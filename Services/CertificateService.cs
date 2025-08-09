using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GCCWebApp.Repositories;
using GreenCircleCertified.ViewModels;

namespace GCCWebApp.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly ApplicationDbContext _context;

        public CertificateService(
            ILogger<CertificateService> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<CertificateListResponse> GetCertificatesAsync(CertificateListRequest request)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .AsQueryable();

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
                {
                    var searchTerm = request.Filters.SearchTerm.ToLower();
                    query = query.Where(c =>
                        c.Company.Name.ToLower().Contains(searchTerm) ||
                        c.Product.Name.ToLower().Contains(searchTerm) ||
                        c.Number.ToLower().Contains(searchTerm));
                }

                if (request.Filters.CertificationTypes.Any())
                {
                    query = query.Where(c => request.Filters.CertificationTypes.Contains(c.CertificationType.Name));
                }

                if (request.Filters.Countries.Any())
                {
                    query = query.Where(c => request.Filters.Countries.Contains(c.Country ?? ""));
                }

                if (request.Filters.Statuses.Any())
                {
                    query = query.Where(c => request.Filters.Statuses.Contains(c.Status));
                }

                // Apply date filters
                if (request.Filters.EffectiveDateStart.HasValue)
                {
                    query = query.Where(c => c.EffectiveDate >= request.Filters.EffectiveDateStart.Value);
                }

                if (request.Filters.EffectiveDateEnd.HasValue)
                {
                    query = query.Where(c => c.EffectiveDate <= request.Filters.EffectiveDateEnd.Value);
                }

                // Apply sorting
                query = request.Sort?.ToLower() switch
                {
                    "newest" => query.OrderByDescending(c => c.EffectiveDate),
                    "expiring" => query.OrderBy(c => c.ExpirationDate),
                    "product-az" => query.OrderBy(c => c.Product.Name),
                    "company-az" => query.OrderBy(c => c.Company.Name),
                    "status" => query.OrderBy(c => c.Status),
                    "type" => query.OrderBy(c => c.CertificationType.Name),
                    "country" => query.OrderBy(c => c.Country),
                    _ => query.OrderBy(c => c.Id)
                };

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var certificates = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(c => new CertificateCardModel
                    {
                        Id = c.Id,
                        CertificateNumber = c.Number,
                        CertificationType = c.CertificationType.Name,
                        CompanyName = c.Company.Name,
                        ProductName = c.Product.Name,
                        ProductCategory = c.Product.Category != null ? c.Product.Category.Name : "",
                        EffectiveDate = c.EffectiveDate,
                        ExpirationDate = c.ExpirationDate,
                        Location = c.Location ?? "",
                        Country = c.Country ?? "",
                        Status = c.Status,
                        BadgeText = GetBadgeText(c.CertificationType.Name),
                        TagType = DetermineTagType(request.UserRole)
                    })
                    .ToListAsync();

                return new CertificateListResponse
                {
                    Certificates = certificates,
                    TotalCount = totalCount,
                    PageSize = request.PageSize,
                    CurrentPage = request.Page,
                    CurrentSort = request.Sort,
                    AppliedFilters = request.Filters,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates");
                return new CertificateListResponse
                {
                    Success = false,
                    Message = "Failed to retrieve certificates",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<CertificateDetailsViewModel> GetCertificateDetailsAsync(int id, string userRole)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                    .Include(c => c.CertificationType)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (certificate == null)
                    return null;

                var viewModel = new CertificateDetailsViewModel
                {
                    Id = certificate.Id,
                    CertificateNumber = certificate.Number,
                    CompanyName = certificate.Company.Name,
                    ProductName = certificate.Product.Name,
                    CertificationType = certificate.CertificationType.Name,
                    ProductCategory = certificate.Product.Category?.Name ?? "",
                    EffectiveDate = certificate.EffectiveDate,
                    ExpirationDate = certificate.ExpirationDate,
                    Location = certificate.Location ?? "",
                    Country = certificate.Country ?? "",
                    CertificateImageUrl = certificate.CertificateFile ?? ""
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate details for ID: {CertificateId}", id);
                return null;
            }
        }

        public async Task<CertificateDownloadModel> GetCertificateDownloadAsync(int id, string userRole)
        {
            try
            {
                var certificate = await GetCertificateDetailsAsync(id, userRole);
                if (certificate == null) return null;

                // For now, return a placeholder PDF
                var pdfContent = GenerateCertificatePdf(certificate);

                return new CertificateDownloadModel
                {
                    CertificateId = id,
                    CertificateNumber = certificate.CertificateNumber,
                    FileName = $"Certificate_{certificate.CertificateNumber}.pdf",
                    FileContent = pdfContent,
                    ContentType = "application/pdf",
                    UserRole = userRole
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing certificate download for ID: {CertificateId}", id);
                return null;
            }
        }

        public async Task<byte[]> ExportCertificatesAsync(CertificateListRequest request, string format)
        {
            try
            {
                var response = await GetCertificatesAsync(request);
                if (!response.Success || !response.Certificates.Any())
                    return null;

                return format.ToLower() switch
                {
                    "csv" => GenerateCsvExport(response.Certificates),
                    "excel" => GenerateExcelExport(response.Certificates),
                    "pdf" => GeneratePdfExport(response.Certificates),
                    _ => GenerateCsvExport(response.Certificates)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates to {Format}", format);
                return null;
            }
        }

        public async Task<CertificateStatistics> GetCertificateStatisticsAsync(string userRole)
        {
            try
            {
                var totalCertificates = await _context.Certificates.CountAsync();
                var activeCertificates = await _context.Certificates.CountAsync(c => c.Status == "active");
                var expiredCertificates = await _context.Certificates.CountAsync(c => c.Status == "expired");
                var pendingCertificates = await _context.Certificates.CountAsync(c => c.Status == "pending");
                
                var expiringSoonDate = DateTime.Now.AddDays(30);
                var expiringSoonCertificates = await _context.Certificates
                    .CountAsync(c => c.ExpirationDate <= expiringSoonDate && c.Status == "active");

                var certificatesByType = await _context.Certificates
                    .GroupBy(c => c.CertificationType.Name)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count);

                var certificatesByCountry = await _context.Certificates
                    .Where(c => !string.IsNullOrEmpty(c.Country))
                    .GroupBy(c => c.Country)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Country!, x => x.Count);

                return new CertificateStatistics
                {
                    TotalCertificates = totalCertificates,
                    ActiveCertificates = activeCertificates,
                    ExpiredCertificates = expiredCertificates,
                    ExpiringSoonCertificates = expiringSoonCertificates,
                    PendingCertificates = pendingCertificates,
                    CertificatesByType = certificatesByType,
                    CertificatesByCountry = certificatesByCountry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate statistics");
                return new CertificateStatistics();
            }
        }

        public async Task<BulkActionResult> PerformBulkActionAsync(BulkActionRequest request)
        {
            try
            {
                var result = new BulkActionResult { Success = true };

                switch (request.Action.ToLower())
                {
                    case "delete":
                        var certificatesToDelete = await _context.Certificates
                            .Where(c => request.CertificateIds.Contains(c.Id))
                            .ToListAsync();
                        
                        _context.Certificates.RemoveRange(certificatesToDelete);
                        await _context.SaveChangesAsync();
                        
                        result.AffectedCount = certificatesToDelete.Count;
                        result.Message = $"Successfully deleted {result.AffectedCount} certificates";
                        break;

                    case "update_status":
                        if (request.Parameters.TryGetValue("status", out var newStatus))
                        {
                            var certificatesToUpdate = await _context.Certificates
                                .Where(c => request.CertificateIds.Contains(c.Id))
                                .ToListAsync();

                            foreach (var cert in certificatesToUpdate)
                            {
                                cert.Status = newStatus;
                                cert.UpdatedAt = DateTime.UtcNow;
                            }

                            await _context.SaveChangesAsync();
                            
                            result.AffectedCount = certificatesToUpdate.Count;
                            result.Message = $"Successfully updated status for {result.AffectedCount} certificates";
                        }
                        else
                        {
                            result.Success = false;
                            result.Message = "Status parameter is required for update_status action";
                        }
                        break;

                    default:
                        result.Success = false;
                        result.Message = "Unknown bulk action";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action: {Action}", request.Action);
                return new BulkActionResult
                {
                    Success = false,
                    Message = "Bulk action failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<List<string>> GetSearchSuggestionsAsync(string term, string type, string userRole)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term)) return new List<string>();

                var suggestions = new List<string>();

                switch (type.ToLower())
                {
                    case "company":
                        suggestions = await _context.Companies
                            .Where(c => c.Name.Contains(term))
                            .Select(c => c.Name)
                            .Take(10)
                            .ToListAsync();
                        break;

                    case "product":
                        suggestions = await _context.Products
                            .Where(p => p.Name.Contains(term))
                            .Select(p => p.Name)
                            .Take(10)
                            .ToListAsync();
                        break;

                    default:
                        suggestions = await _context.CertificationTypes
                            .Where(ct => ct.Name.Contains(term))
                            .Select(ct => ct.Name)
                            .Take(10)
                            .ToListAsync();
                        break;
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
                return new List<string>();
            }
        }

        public async Task<CertificateValidationResult> ValidateCertificateAsync(string certificateNumber)
        {
            try
            {
                var certificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null)
                {
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        Message = "Certificate not found"
                    };
                }

                var isExpired = certificate.ExpirationDate < DateTime.Now;
                var isExpiringSoon = certificate.ExpirationDate <= DateTime.Now.AddDays(30);

                return new CertificateValidationResult
                {
                    IsValid = true,
                    Status = certificate.Status,
                    Message = isExpired ? "Certificate has expired" : isExpiringSoon ? "Certificate is expiring soon" : "Certificate is valid",
                    ExpirationDate = certificate.ExpirationDate,
                    IsExpired = isExpired,
                    IsExpiringSoon = isExpiringSoon && !isExpired
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating certificate: {CertificateNumber}", certificateNumber);
                return new CertificateValidationResult
                {
                    IsValid = false,
                    Message = "Validation failed"
                };
            }
        }

        public async Task<int> CreateCertificateAsync(CertificateEditViewModel certificate, string userRole)
        {
            try
            {
                var newCertificate = new Certificate
                {
                    Number = certificate.Number,
                    Status = certificate.Status,
                    EffectiveDate = certificate.EffectiveDate,
                    ExpirationDate = certificate.ExpirationDate,
                    Location = certificate.Location,
                    Country = certificate.Country,
                    Asin = certificate.Asin,
                    Upc = certificate.Upc,
                    Gtin = certificate.Gtin,
                    Ean = certificate.Ean,
                    CompanyId = certificate.CompanyId,
                    ProductId = certificate.ProductId,
                    CertificationTypeId = certificate.CertificationTypeId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(newCertificate);
                await _context.SaveChangesAsync();

                return newCertificate.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate");
                return 0;
            }
        }

        public async Task<bool> UpdateCertificateAsync(int id, CertificateEditViewModel certificate, string userRole)
        {
            try
            {
                var existingCertificate = await _context.Certificates.FindAsync(id);
                if (existingCertificate == null) return false;

                existingCertificate.Number = certificate.Number;
                existingCertificate.Status = certificate.Status;
                existingCertificate.EffectiveDate = certificate.EffectiveDate;
                existingCertificate.ExpirationDate = certificate.ExpirationDate;
                existingCertificate.Location = certificate.Location;
                existingCertificate.Country = certificate.Country;
                existingCertificate.Asin = certificate.Asin;
                existingCertificate.Upc = certificate.Upc;
                existingCertificate.Gtin = certificate.Gtin;
                existingCertificate.Ean = certificate.Ean;
                existingCertificate.CompanyId = certificate.CompanyId;
                existingCertificate.ProductId = certificate.ProductId;
                existingCertificate.CertificationTypeId = certificate.CertificationTypeId;
                existingCertificate.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate with ID: {CertificateId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteCertificateAsync(int id, string userRole)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null) return false;

                _context.Certificates.Remove(certificate);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate with ID: {CertificateId}", id);
                return false;
            }
        }

        public async Task<CertificateDetailsViewModel> GetCertificateByCertificateNumberAsync(string certificateNumber, string userRole)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .Include(c => c.CustomFieldValues)
                    .ThenInclude(cfv => cfv.Field)
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null) return null;

                return await GetCertificateDetailsAsync(certificate.Id, userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate by number: {CertificateNumber}", certificateNumber);
                return null;
            }
        }

        #region Private Helper Methods

        private string GetBadgeText(string certificationType)
        {
            return certificationType switch
            {
                "Certified Sustainability Facts" => "CSF",
                "Recycled Content" => "RC",
                "Renewable Energy" => "RE",
                "Carbon Footprint" => "CF",
                "Chain of Custody" => "CoC",
                _ => "CERT"
            };
        }

        private string DetermineTagType(string userRole)
        {
            return userRole switch
            {
                "Admin" => "Internal",
                "Analyst" => "Internal",
                _ => "External"
            };
        }

        private byte[] GenerateCertificatePdf(CertificateDetailsViewModel certificate)
        {
            var content = $"Certificate {certificate.CertificateNumber} - {certificate.CompanyName}";
            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        private byte[] GenerateCsvExport(List<CertificateCardModel> certificates)
        {
            var csv = "Certificate Number,Company Name,Product Name,Status,Effective Date,Expiration Date\n";
            foreach (var cert in certificates)
            {
                csv += $"{cert.CertificateNumber},{cert.CompanyName},{cert.ProductName},{cert.Status},{cert.EffectiveDate:yyyy-MM-dd},{cert.ExpirationDate:yyyy-MM-dd}\n";
            }
            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        private byte[] GenerateExcelExport(List<CertificateCardModel> certificates)
        {
            return GenerateCsvExport(certificates);
        }

        private byte[] GeneratePdfExport(List<CertificateCardModel> certificates)
        {
            return System.Text.Encoding.UTF8.GetBytes("PDF Export of Certificates");
        }

        #endregion
    }
}
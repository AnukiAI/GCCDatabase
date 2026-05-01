using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GCCWebApp.Repositories;
using GreenCircleCertified.ViewModels;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCCWebApp.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IProductIdService _productIdService;

        public CertificateService(
            ILogger<CertificateService> logger,
            ApplicationDbContext context,
            IProductIdService productIdService)
        {
            _logger = logger;
            _context = context;
            _productIdService = productIdService;
        }

        public async Task<CertificateListResponse> GetCertificatesAsync(CertificateListRequest request)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .AsQueryable();

                // Apply search filters (includes tag matching)
                if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
                {
                    var searchTerm = request.Filters.SearchTerm.ToLower();
                    query = query.Where(c =>
                        c.Company.Name.ToLower().Contains(searchTerm) ||
                        c.Product.Name.ToLower().Contains(searchTerm) ||
                        c.Number.ToLower().Contains(searchTerm) ||
                        c.Tags.Any(t => t.Name.ToLower().Contains(searchTerm)));
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
                var rawCertificates = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(c => new
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
                        Status = c.Status
                    })
                    .ToListAsync();

                // Apply client-side transformations
                var certificates = rawCertificates.Select(c => new CertificateCardModel
                {
                    Id = c.Id,
                    CertificateNumber = c.CertificateNumber,
                    CertificationType = c.CertificationType,
                    CompanyName = c.CompanyName,
                    ProductName = c.ProductName,
                    ProductCategory = c.ProductCategory,
                    EffectiveDate = c.EffectiveDate,
                    ExpirationDate = c.ExpirationDate,
                    Location = c.Location,
                    Country = c.Country,
                    Status = c.Status,
                    BadgeText = GetBadgeText(c.CertificationType),
                    TagType = DetermineTagType(request.UserRole)
                }).ToList();

                return new CertificateListResponse
                {
                    Certificates = certificates,
                    TotalCount = totalCount,
                    PageSize = request.PageSize,
                    CurrentPage = request.Page,
                    CurrentSort = request.Sort ?? string.Empty,
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

        public async Task<CertificateListResponse> GetCertificatesByPartnerAsync(string partnerName, CertificateListRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving certificates for partner: {PartnerName}", partnerName);

                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .Where(c => c.Partner == partnerName) // Filter by partner
                    .AsQueryable();

                // Apply search filters (includes tag matching)
                if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
                {
                    var searchTerm = request.Filters.SearchTerm.ToLower();
                    query = query.Where(c =>
                        c.Company.Name.ToLower().Contains(searchTerm) ||
                        c.Product.Name.ToLower().Contains(searchTerm) ||
                        c.Number.ToLower().Contains(searchTerm) ||
                        c.Tags.Any(t => t.Name.ToLower().Contains(searchTerm)));
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
                var rawCertificates = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(c => new
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
                        Status = c.Status
                    })
                    .ToListAsync();

                // Apply client-side transformations
                var certificates = rawCertificates.Select(c => new CertificateCardModel
                {
                    Id = c.Id,
                    CertificateNumber = c.CertificateNumber,
                    CertificationType = c.CertificationType,
                    CompanyName = c.CompanyName,
                    ProductName = c.ProductName,
                    ProductCategory = c.ProductCategory,
                    EffectiveDate = c.EffectiveDate,
                    ExpirationDate = c.ExpirationDate,
                    Location = c.Location,
                    Country = c.Country,
                    Status = c.Status,
                    BadgeText = GetBadgeText(c.CertificationType),
                    TagType = DetermineTagType(request.UserRole)
                }).ToList();

                _logger.LogInformation("Retrieved {Count} certificates for partner {PartnerName}",
                    certificates.Count, partnerName);

                return new CertificateListResponse
                {
                    Certificates = certificates,
                    TotalCount = totalCount,
                    PageSize = request.PageSize,
                    CurrentPage = request.Page,
                    CurrentSort = request.Sort ?? string.Empty,
                    AppliedFilters = request.Filters,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates for partner {PartnerName}", partnerName);
                return new CertificateListResponse
                {
                    Success = false,
                    Message = $"Failed to retrieve certificates for partner {partnerName}",
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
                    return null!;

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
                return null!;
            }
        }

        public async Task<CertificateDownloadModel?> GetCertificateDownloadAsync(int id, string userRole)
        {
            try
            {
                var certificate = await GetCertificateDetailsAsync(id, userRole);
                if (certificate == null) return null;

                // Per business rule (2026-04-25): never auto-generate a placeholder PDF.
                // Read the actual uploaded file off disk; if there isn't one, return null so the
                // controller can surface 404. Analysts must upload a real PDF at cert creation.
                var entity = await _context.Certificates.FindAsync(id);
                if (entity == null || string.IsNullOrEmpty(entity.CertificateFile))
                {
                    return null;
                }

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", entity.CertificateFile.TrimStart('~', '/'));
                if (!System.IO.File.Exists(fullPath))
                {
                    return null;
                }

                var fileContent = await System.IO.File.ReadAllBytesAsync(fullPath);

                return new CertificateDownloadModel
                {
                    CertificateId = id,
                    CertificateNumber = certificate.CertificateNumber,
                    FileName = $"Certificate_{certificate.CertificateNumber}.pdf",
                    FileContent = fileContent,
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

        public async Task<byte[]?> ExportCertificatesAsync(CertificateListRequest request, string format)
        {
            try
            {
                var exportRows = await GetCertificatesForExportAsync(request);
                if (!exportRows.Any())
                    return null;

                return format.ToLower() switch
                {
                    "csv" => GenerateCsvExport(exportRows),
                    "excel" => GenerateExcelExport(exportRows),
                    "pdf" => GeneratePdfExportFromRows(exportRows),
                    _ => GenerateCsvExport(exportRows)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates to {Format}", format);
                return null;
            }
        }

        public async Task<byte[]?> ExportCertificatesByIdsAsync(List<string> certificateIds, string format)
        {
            try
            {
                if (certificateIds == null || !certificateIds.Any())
                    return null;

                var exportRows = await GetCertificatesForExportByIdsAsync(certificateIds);
                if (!exportRows.Any())
                    return null;

                return format.ToLower() switch
                {
                    "csv" => GenerateCsvExport(exportRows),
                    "excel" => GenerateExcelExport(exportRows),
                    "pdf" => GeneratePdfExportFromRows(exportRows),
                    _ => GenerateCsvExport(exportRows)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates by IDs to {Format}", format);
                return null;
            }
        }

        /// <summary>
        /// Internal export row with all certificate fields for comprehensive export.
        /// </summary>
        private class CertificateExportRow
        {
            public int Id { get; set; }
            public string CertificateNumber { get; set; } = "";
            public string CertificationType { get; set; } = "";
            public string CertificationSubcategories { get; set; } = "";
            public string Status { get; set; } = "";
            public string CompanyName { get; set; } = "";
            public string CompanyContactEmail { get; set; } = "";
            public string CompanyContactUrl { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string ProductCategory { get; set; } = "";
            public string ProductType { get; set; } = "";
            public string ProductSubcategory { get; set; } = "";
            public string Partner { get; set; } = "";
            public string Pathway { get; set; } = "";
            public string CertificationResults { get; set; } = "";
            public DateTime EffectiveDate { get; set; }
            public DateTime ExpirationDate { get; set; }
            public string Location { get; set; } = "";
            public string Country { get; set; } = "";
            public string ExternalTags { get; set; } = "";
            public string InternalTags { get; set; } = "";
            public string Asin { get; set; } = "";
            public string Upc { get; set; } = "";
            public string Gtin { get; set; } = "";
            public string Ean { get; set; } = "";
        }

        /// <summary>
        /// Queries certificates with all navigation properties for comprehensive export.
        /// </summary>
        private async Task<List<CertificateExportRow>> GetCertificatesForExportAsync(CertificateListRequest request)
        {
            var query = BuildExportQuery();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
            {
                var searchTerm = request.Filters.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Company.Name.ToLower().Contains(searchTerm) ||
                    c.Product.Name.ToLower().Contains(searchTerm) ||
                    c.Number.ToLower().Contains(searchTerm) ||
                    c.Tags.Any(t => t.Name.ToLower().Contains(searchTerm)));
            }
            if (request.Filters.CertificationTypes.Any())
                query = query.Where(c => request.Filters.CertificationTypes.Contains(c.CertificationType.Name));
            if (request.Filters.Countries.Any())
                query = query.Where(c => request.Filters.Countries.Contains(c.Country ?? ""));
            if (request.Filters.Statuses.Any())
                query = query.Where(c => request.Filters.Statuses.Contains(c.Status));

            return await ProjectToExportRows(query);
        }

        /// <summary>
        /// Queries certificates by IDs/numbers with all fields for export.
        /// </summary>
        private async Task<List<CertificateExportRow>> GetCertificatesForExportByIdsAsync(List<string> certificateIds)
        {
            var query = BuildExportQuery()
                .Where(c => certificateIds.Contains(c.Id.ToString()) || certificateIds.Contains(c.Number));

            return await ProjectToExportRows(query);
        }

        private IQueryable<Certificate> BuildExportQuery()
        {
            return _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p.Category)
                .Include(c => c.Product).ThenInclude(p => p.ProductType)
                .Include(c => c.Product).ThenInclude(p => p.ProductSubcategory)
                .Include(c => c.CertificationType)
                .Include(c => c.CertificationTypeSubcategories)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.Pathway)
                .Include(c => c.Tags)
                .Include(c => c.ProductIds)
                .AsQueryable();
        }

        private async Task<List<CertificateExportRow>> ProjectToExportRows(IQueryable<Certificate> query)
        {
            var certs = await query.OrderBy(c => c.Id).ToListAsync();

            return certs.Select(c => new CertificateExportRow
            {
                Id = c.Id,
                CertificateNumber = c.Number,
                CertificationType = c.CertificationType?.Name ?? "",
                CertificationSubcategories = c.CertificationTypeSubcategories != null && c.CertificationTypeSubcategories.Any()
                    ? string.Join(", ", c.CertificationTypeSubcategories.Select(s => s.Name))
                    : "",
                Status = c.Status,
                CompanyName = c.Company?.Name ?? "",
                CompanyContactEmail = c.Company?.Email ?? "",
                CompanyContactUrl = c.Company?.Website ?? "",
                ProductName = c.Product?.Name ?? "",
                ProductCategory = c.ProductCategory?.Name ?? (c.Product?.Category?.Name ?? ""),
                ProductType = c.ProductType?.Name ?? (c.Product?.ProductType?.Name ?? ""),
                ProductSubcategory = c.ProductSubcategory?.Name ?? (c.Product?.ProductSubcategory?.Name ?? ""),
                Partner = c.Partner ?? "None",
                Pathway = c.Pathway?.Name ?? "",
                CertificationResults = c.CertificationResults ?? "",
                EffectiveDate = c.EffectiveDate,
                ExpirationDate = c.ExpirationDate,
                Location = c.Location ?? "",
                Country = c.Country ?? "",
                ExternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "external").Select(t => t.Name)),
                InternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name)),
                Asin = GetProductIdValues(c, "ASIN"),
                Upc = GetProductIdValues(c, "UPC"),
                Gtin = GetProductIdValues(c, "GTIN"),
                Ean = GetProductIdValues(c, "EAN")
            }).ToList();
        }

        private static string GetProductIdValues(Certificate c, string type)
        {
            var fromCollection = c.ProductIds?
                .Where(pid => pid.ProductIdType == type && pid.IsActive)
                .Select(pid => pid.ProductIdValue)
                .ToList();

            if (fromCollection != null && fromCollection.Any())
                return string.Join(", ", fromCollection);

            // Fallback to legacy fields
            return type switch
            {
                "ASIN" => c.Asin ?? "",
                "UPC" => c.Upc ?? "",
                "GTIN" => c.Gtin ?? "",
                "EAN" => c.Ean ?? "",
                _ => ""
            };
        }

        public async Task<CertificateStatistics> GetCertificateStatisticsAsync(string userRole)
        {
            try
            {
                var expiringSoonDate = DateTime.Now.AddDays(30);
                
                // Single query with optimized aggregation to prevent N+1 query pattern
                var certificates = await _context.Certificates
                    .Include(c => c.CertificationType)
                    .Select(c => new 
                    {
                        c.Status,
                        c.ExpirationDate,
                        c.Country,
                        CertificationTypeName = c.CertificationType.Name,
                        IsPublic = CertificateStatusHelper.PublicStatuses.Contains(c.Status)
                    })
                    .ToListAsync();

                // Calculate all statistics in memory from single query result
                var totalCertificates = certificates.Count;
                var activeCertificates = certificates.Count(c => c.IsPublic && c.ExpirationDate >= DateTime.Now);
                var expiredCertificates = certificates.Count(c => c.Status == CertificateStatusHelper.StatusExpired || c.Status == CertificateStatusHelper.StatusExpiredNoAnnualVerification);
                var pendingCertificates = certificates.Count(c => c.Status == CertificateStatusHelper.StatusClientApprovedNotPaid || c.Status == CertificateStatusHelper.StatusDraft);
                var expiringSoonCertificates = 0; // Removed per client request

                var certificatesByType = certificates
                    .Where(c => !string.IsNullOrEmpty(c.CertificationTypeName))
                    .GroupBy(c => c.CertificationTypeName)
                    .ToDictionary(g => g.Key!, g => g.Count());

                var certificatesByCountry = certificates
                    .Where(c => !string.IsNullOrEmpty(c.Country))
                    .GroupBy(c => c.Country)
                    .ToDictionary(g => g.Key!, g => g.Count());

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

                            // CR-015: cascade product-ID statuses on each cert whose status just changed.
                            foreach (var cert in certificatesToUpdate)
                            {
                                await _productIdService.SyncProductIdStatusesForCertificateAsync(cert.Id);
                            }

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
                    EffectiveDate = certificate.EffectiveDate.Value,
                    ExpirationDate = certificate.ExpirationDate.Value,
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

                var previousStatus = existingCertificate.Status;

                existingCertificate.Number = certificate.Number;
                existingCertificate.Status = certificate.Status;
                existingCertificate.EffectiveDate = certificate.EffectiveDate.Value;
                existingCertificate.ExpirationDate = certificate.ExpirationDate.Value;
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

                // CR-015: cascade product-ID statuses if cert status changed.
                if (!string.Equals(previousStatus, certificate.Status, StringComparison.Ordinal))
                {
                    await _productIdService.SyncProductIdStatusesForCertificateAsync(id);
                }

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

        public async Task<CertificateDetailsViewModel?> GetCertificateByCertificateNumberAsync(string certificateNumber, string userRole)
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

        public async Task<Certificate?> GetCertificateEntityAsync(int id)
        {
            return await _context.Certificates.FindAsync(id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        #region Private Helper Methods

        private static string GetBadgeText(string certificationType)
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

        private static string DetermineTagType(string userRole)
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
            try
            {
                // Initialize QuestPDF settings
                QuestPDF.Settings.License = LicenseType.Community;
                
                // Generate individual certificate PDF using QuestPDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        
                        // Header
                        page.Header().Height(100).Background(Colors.Green.Medium).Padding(20).Column(column =>
                        {
                            column.Item().AlignCenter().Text("Green Circle Certified")
                                .FontSize(28).FontColor(Colors.White).Bold();
                            column.Item().AlignCenter().Text("Certificate Document")
                                .FontSize(16).FontColor(Colors.White);
                        });

                        // Content
                        page.Content().PaddingTop(30).Column(column =>
                        {
                            // Certificate Number
                            column.Item().PaddingBottom(20).AlignCenter()
                                .Text($"Certificate #{certificate.CertificateNumber}")
                                .FontSize(24).Bold().FontColor(Colors.Green.Darken2);

                            // Certificate Details
                            column.Item().PaddingBottom(20).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                AddDetailRow(table, "Company:", certificate.CompanyName ?? "");
                                AddDetailRow(table, "Product:", certificate.ProductName ?? "");
                                AddDetailRow(table, "Category:", certificate.ProductCategory ?? "");
                                AddDetailRow(table, "Certification Type:", certificate.CertificationType ?? "");
                                AddDetailRow(table, "Status:", certificate.Status ?? "");
                                AddDetailRow(table, "Effective Date:", certificate.EffectiveDate.ToString("yyyy-MM-dd"));
                                AddDetailRow(table, "Expiration Date:", certificate.ExpirationDate.ToString("yyyy-MM-dd"));
                                if (!string.IsNullOrEmpty(certificate.Location))
                                    AddDetailRow(table, "Location:", $"{certificate.Location}, {certificate.Country}");
                            });

                            // Footer note
                            column.Item().PaddingTop(30).AlignCenter()
                                .Text("This certificate is valid only when verified through the Green Circle Certified database.")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });

                        // Footer
                        page.Footer().AlignCenter().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating individual certificate PDF with QuestPDF");
                
                // Fallback to simple text
                var content = $"Green Circle Certified Certificate\n\n" +
                             $"Certificate Number: {certificate.CertificateNumber}\n" +
                             $"Company: {certificate.CompanyName}\n" +
                             $"Product: {certificate.ProductName}\n" +
                             $"Status: {certificate.Status}\n" +
                             $"Valid: {certificate.EffectiveDate:yyyy-MM-dd} to {certificate.ExpirationDate:yyyy-MM-dd}\n\n" +
                             $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
                
                return System.Text.Encoding.UTF8.GetBytes(content);
            }
        }

        private static void AddDetailRow(dynamic table, string label, string value)
        {
            table.Cell().Border(0.5f).Padding(8).Background(Colors.Grey.Lighten4)
                .Text(label).FontSize(12).Bold();
            table.Cell().Border(0.5f).Padding(8)
                .Text(value).FontSize(12);
        }

        private byte[] GenerateCsvExport(List<CertificateExportRow> certificates)
        {
            try
            {
                var csv = new StringBuilder();

                // Get custom fields and values for all exported certificates
                var certIds = certificates.Select(c => c.Id).ToList();
                var (customFieldNames, customFieldValueMap) = GetCustomFieldDataForExport(certIds);

                // CSV Header — all certificate fields + admin custom fields
                // CSV header text matches the certificate tile UI column labels.
                csv.Append("\"Certificate #\",\"Certification Type\",\"Cert Subcategories\",\"Status\",\"Company\",\"Company Contact Email/URL\",\"Contact URL\",\"Product Name\",\"Product Category\",\"Product Type\",\"Subcategory\",\"Partner\",\"Pathway\",\"Cert Results\",\"Effective Date\",\"Expiration Date\",\"Location\",\"Country\",\"External Tag\",\"Internal Tag\",\"ASIN\",\"UPC\",\"GTIN\",\"EAN\"");
                foreach (var fieldName in customFieldNames)
                {
                    csv.Append($",\"{EscapeCsvField(fieldName)}\"");
                }
                csv.AppendLine();

                // CSV Data with proper escaping
                foreach (var cert in certificates)
                {
                    csv.Append($"\"{EscapeCsvField(cert.CertificateNumber)}\",\"{EscapeCsvField(cert.CertificationType)}\",\"{EscapeCsvField(cert.CertificationSubcategories)}\",\"{EscapeCsvField(cert.Status)}\",\"{EscapeCsvField(cert.CompanyName)}\",\"{EscapeCsvField(cert.CompanyContactEmail)}\",\"{EscapeCsvField(cert.CompanyContactUrl)}\",\"{EscapeCsvField(cert.ProductName)}\",\"{EscapeCsvField(cert.ProductCategory)}\",\"{EscapeCsvField(cert.ProductType)}\",\"{EscapeCsvField(cert.ProductSubcategory)}\",\"{EscapeCsvField(cert.Partner)}\",\"{EscapeCsvField(cert.Pathway)}\",\"{EscapeCsvField(cert.CertificationResults)}\",\"{cert.EffectiveDate:yyyy-MM-dd}\",\"{cert.ExpirationDate:yyyy-MM-dd}\",\"{EscapeCsvField(cert.Location)}\",\"{EscapeCsvField(cert.Country)}\",\"{EscapeCsvField(cert.ExternalTags)}\",\"{EscapeCsvField(cert.InternalTags)}\",\"{EscapeCsvField(cert.Asin)}\",\"{EscapeCsvField(cert.Upc)}\",\"{EscapeCsvField(cert.Gtin)}\",\"{EscapeCsvField(cert.Ean)}\"");

                    // Append custom field values (admin-added fields)
                    customFieldValueMap.TryGetValue(cert.Id, out var certFieldValues);
                    foreach (var fieldName in customFieldNames)
                    {
                        var value = certFieldValues != null && certFieldValues.TryGetValue(fieldName, out var v) ? v : "";
                        csv.Append($",\"{EscapeCsvField(value)}\"");
                    }
                    csv.AppendLine();
                }

                return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV export");
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Escape quotes by doubling them
            return field.Replace("\"", "\"\"");
        }

        /// <summary>
        /// Gets all custom field names and their values for the given certificate IDs.
        /// Returns ordered field names and a map of certificateId -> (fieldName -> value).
        /// </summary>
        private (List<string> FieldNames, Dictionary<int, Dictionary<string, string>> ValueMap) GetCustomFieldDataForExport(List<int> certificateIds)
        {
            try
            {
                // Get all custom field values for these certificates, joined with field names
                var fieldValues = _context.CustomFieldValues
                    .Where(cfv => certificateIds.Contains(cfv.CertificateId))
                    .Include(cfv => cfv.Field)
                    .Select(cfv => new
                    {
                        cfv.CertificateId,
                        FieldName = cfv.Field.Name,
                        cfv.Field.SortOrder,
                        Value = cfv.TextValue ?? ""
                    })
                    .ToList();

                // Get distinct field names in sort order
                var fieldNames = fieldValues
                    .GroupBy(fv => fv.FieldName)
                    .OrderBy(g => g.Min(fv => fv.SortOrder))
                    .Select(g => g.Key)
                    .ToList();

                // Build map: certificateId -> { fieldName -> value }
                var valueMap = fieldValues
                    .GroupBy(fv => fv.CertificateId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToDictionary(fv => fv.FieldName, fv => fv.Value)
                    );

                return (fieldNames, valueMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading custom field data for export");
                return (new List<string>(), new Dictionary<int, Dictionary<string, string>>());
            }
        }

        private byte[] GenerateExcelExport(List<CertificateExportRow> certificates)
        {
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Certificates");

                // Get custom fields and values for all exported certificates
                var certIds = certificates.Select(c => c.Id).ToList();
                var (customFieldNames, customFieldValueMap) = GetCustomFieldDataForExport(certIds);

                // Add headers — all certificate fields + admin custom fields
                var headers = new List<string>
                {
                    "Certificate Number", "Certification Type", "Certification Subcategories", "Status",
                    "Company Name", "Company Contact Email", "Company Contact URL",
                    "Product Name", "Product Category", "Product Type", "Product Subcategory",
                    "Partner Program", "Pathway", "Certification Results",
                    "Effective Date", "Expiration Date", "Location", "Country",
                    "External Tags", "Internal Tags",
                    "ASIN", "UPC", "GTIN", "EAN"
                };
                headers.AddRange(customFieldNames);

                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                }

                // Add data
                int row = 2;
                foreach (var cert in certificates)
                {
                    int col = 1;
                    worksheet.Cell(row, col++).Value = cert.CertificateNumber;
                    worksheet.Cell(row, col++).Value = cert.CertificationType;
                    worksheet.Cell(row, col++).Value = cert.CertificationSubcategories;
                    worksheet.Cell(row, col++).Value = cert.Status;
                    worksheet.Cell(row, col++).Value = cert.CompanyName;
                    worksheet.Cell(row, col++).Value = cert.CompanyContactEmail;
                    worksheet.Cell(row, col++).Value = cert.CompanyContactUrl;
                    worksheet.Cell(row, col++).Value = cert.ProductName;
                    worksheet.Cell(row, col++).Value = cert.ProductCategory;
                    worksheet.Cell(row, col++).Value = cert.ProductType;
                    worksheet.Cell(row, col++).Value = cert.ProductSubcategory;
                    worksheet.Cell(row, col++).Value = cert.Partner;
                    worksheet.Cell(row, col++).Value = cert.Pathway;
                    worksheet.Cell(row, col++).Value = cert.CertificationResults;
                    var effCell = worksheet.Cell(row, col++);
                    effCell.Value = cert.EffectiveDate;
                    effCell.Style.DateFormat.Format = "yyyy-MM-dd";
                    var expCell = worksheet.Cell(row, col++);
                    expCell.Value = cert.ExpirationDate;
                    expCell.Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(row, col++).Value = cert.Location;
                    worksheet.Cell(row, col++).Value = cert.Country;
                    worksheet.Cell(row, col++).Value = cert.ExternalTags;
                    worksheet.Cell(row, col++).Value = cert.InternalTags;
                    worksheet.Cell(row, col++).Value = cert.Asin;
                    worksheet.Cell(row, col++).Value = cert.Upc;
                    worksheet.Cell(row, col++).Value = cert.Gtin;
                    worksheet.Cell(row, col++).Value = cert.Ean;

                    // Append custom field values (admin-added fields)
                    customFieldValueMap.TryGetValue(cert.Id, out var certFieldValues);
                    for (int i = 0; i < customFieldNames.Count; i++)
                    {
                        var value = certFieldValues != null && certFieldValues.TryGetValue(customFieldNames[i], out var v) ? v : "";
                        worksheet.Cell(row, col + i).Value = value;
                    }
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel export");
                // Fallback to CSV
                return GenerateCsvExport(certificates);
            }
        }

        private byte[] GeneratePdfExportFromRows(List<CertificateExportRow> certificates)
        {
            try
            {
                // Initialize QuestPDF settings
                QuestPDF.Settings.License = LicenseType.Community;

                // Generate professional PDF using QuestPDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);

                        // Header
                        page.Header().Height(80).Background(Colors.Green.Medium).Padding(20).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().AlignCenter().Text("Green Circle Certified")
                                    .FontSize(24).FontColor(Colors.White).Bold();
                                column.Item().AlignCenter().Text("Certificate Export Report")
                                    .FontSize(16).FontColor(Colors.White);
                            });
                        });

                        // Content
                        page.Content().PaddingTop(20).Column(column =>
                        {
                            column.Item().PaddingBottom(10).Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(12).FontColor(Colors.Grey.Darken2);

                            column.Item().PaddingBottom(10).Text($"Total Certificates: {certificates.Count}")
                                .FontSize(14).Bold();

                            // Certificate table
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Certificate Number
                                    columns.RelativeColumn(3); // Company Name
                                    columns.RelativeColumn(2); // Product Name
                                    columns.RelativeColumn(1); // Status
                                    columns.RelativeColumn(2); // Valid Period
                                });

                                // Table Header
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Certificate #").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Company").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Product").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Status").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Valid Period").Bold().FontSize(10);
                                });

                                // Table Rows - Limit to 100 for PDF readability
                                foreach (var cert in certificates.Take(100))
                                {
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.CertificateNumber ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.CompanyName ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.ProductName ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.Status ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text($"{cert.EffectiveDate:yyyy-MM-dd} to {cert.ExpirationDate:yyyy-MM-dd}").FontSize(9);
                                }
                            });

                            // Summary
                            if (certificates.Count > 100)
                            {
                                column.Item().PaddingTop(20).Text($"Note: Showing first 100 of {certificates.Count} certificates. For complete data, please use Excel or CSV export.")
                                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                            }
                        });

                        // Footer
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber().FontSize(10);
                            x.Span(" / ").FontSize(10);
                            x.TotalPages().FontSize(10);
                        });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF export with QuestPDF");

                // Fallback to text-based "PDF" if QuestPDF fails
                var fallbackContent = new StringBuilder();
                fallbackContent.AppendLine("Green Circle Certified - Certificate Export Report");
                fallbackContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                fallbackContent.AppendLine("=" + new string('=', 60));
                fallbackContent.AppendLine();

                foreach (var cert in certificates.Take(50))
                {
                    fallbackContent.AppendLine($"Certificate: {cert.CertificateNumber}");
                    fallbackContent.AppendLine($"Company: {cert.CompanyName}");
                    fallbackContent.AppendLine($"Product: {cert.ProductName}");
                    fallbackContent.AppendLine($"Status: {cert.Status}");
                    fallbackContent.AppendLine($"Valid: {cert.EffectiveDate:yyyy-MM-dd} to {cert.ExpirationDate:yyyy-MM-dd}");
                    fallbackContent.AppendLine(new string('-', 40));
                }

                return System.Text.Encoding.UTF8.GetBytes(fallbackContent.ToString());
            }
        }

        #endregion

        #region Advanced Search Methods

        /// <summary>
        /// Advanced search functionality for certificates with multi-criteria support
        /// </summary>
        public async Task<CertificateListResponse> SearchCertificatesAsync(CertificateListRequest searchCriteria)
        {
            return await GetCertificatesAsync(searchCriteria);
        }

        /// <summary>
        /// Paged search functionality for certificates
        /// </summary>
        public async Task<CertificateListResponse> SearchCertificatesPagedAsync(CertificateListRequest searchCriteria)
        {
            return await GetCertificatesAsync(searchCriteria);
        }

        /// <summary>
        /// Apply advanced filters to certificate query
        /// </summary>
        public async Task<CertificateListResponse> ApplyAdvancedFiltersAsync(CertificateListRequest request)
        {
            return await GetCertificatesAsync(request);
        }

        /// <summary>
        /// Save filter configuration for user
        /// </summary>
        public async Task<bool> SaveFilterAsync(string userId, string filterName, CertificateListRequest filterConfig)
        {
            // TODO: Implement saved filters in database
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// Load saved filter configuration for user
        /// </summary>
        public async Task<CertificateListRequest> LoadSavedFilterAsync(string userId, string filterName)
        {
            // TODO: Implement saved filters from database
            await Task.CompletedTask;
            return new CertificateListRequest();
        }

        /// <summary>
        /// Apply dynamic filters to certificate results
        /// </summary>
        public async Task<CertificateListResponse> ApplyDynamicFilterAsync(CertificateListRequest request)
        {
            return await GetCertificatesAsync(request);
        }

        /// <summary>
        /// Export filtered results in various formats
        /// </summary>
        public async Task<byte[]?> ExportFilteredResultsAsync(CertificateListRequest request, string format)
        {
            return await ExportCertificatesAsync(request, format);
        }

        #endregion
    }
}
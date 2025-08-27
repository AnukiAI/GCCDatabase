using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using Asp.Versioning;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Controllers.Api.v1
{
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    public class CertificatesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificatesController> _logger;

        public CertificatesController(ApplicationDbContext context, ILogger<CertificatesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all certificates with optional pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Certificate>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCertificates([FromQuery] PaginationParams parameters)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(parameters.SearchTerm))
                {
                    query = query.Where(c =>
                        c.Number.Contains(parameters.SearchTerm) ||
                        c.Product.Name.Contains(parameters.SearchTerm) ||
                        c.Company.Name.Contains(parameters.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(parameters.SortBy))
                {
                    query = parameters.SortBy.ToLower() switch
                    {
                        "certificatenumber" => parameters.SortDescending ? query.OrderByDescending(c => c.Number) : query.OrderBy(c => c.Number),
                        "issuedate" => parameters.SortDescending ? query.OrderByDescending(c => c.EffectiveDate) : query.OrderBy(c => c.EffectiveDate),
                        "expirationdate" => parameters.SortDescending ? query.OrderByDescending(c => c.ExpirationDate) : query.OrderBy(c => c.ExpirationDate),
                        "productname" => parameters.SortDescending ? query.OrderByDescending(c => c.Product.Name) : query.OrderBy(c => c.Product.Name),
                        "companyname" => parameters.SortDescending ? query.OrderByDescending(c => c.Company.Name) : query.OrderBy(c => c.Company.Name),
                        _ => query.OrderByDescending(c => c.EffectiveDate)
                    };
                }
                else
                {
                    query = query.OrderByDescending(c => c.EffectiveDate);
                }

                var certificates = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                return PaginatedResponse(certificates, totalCount, parameters.PageNumber, parameters.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates");
                return HandleError(ex, "Failed to retrieve certificates");
            }
        }

        /// <summary>
        /// Get a certificate by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StandardApiResponse<Certificate>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCertificate([FromRoute][Range(1, int.MaxValue, ErrorMessage = "Certificate ID must be greater than 0")] int id)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.CustomFieldValues)
                        .ThenInclude(cfv => cfv.Field)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (certificate == null)
                    return NotFoundResponse($"Certificate with ID {id} not found");

                return SuccessResponse(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {CertificateId}", id);
                return HandleError(ex, $"Failed to retrieve certificate {id}");
            }
        }

        /// <summary>
        /// Get certificate by certificate number
        /// </summary>
        [HttpGet("number/{certificateNumber}")]
        [ProducesResponseType(typeof(StandardApiResponse<Certificate>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCertificateByNumber(
            [FromRoute][Required][StringLength(7, MinimumLength = 1, ErrorMessage = "Certificate number must be between 1 and 7 characters")] string certificateNumber)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.CustomFieldValues)
                        .ThenInclude(cfv => cfv.Field)
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null)
                    return NotFoundResponse($"Certificate with number {certificateNumber} not found");

                return SuccessResponse(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate by number {Number}", certificateNumber);
                return HandleError(ex, $"Failed to retrieve certificate {certificateNumber}");
            }
        }

        /// <summary>
        /// Create a new certificate
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Certificate>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCertificate([FromBody] Certificate certificate)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if certificate number already exists
                var existingCert = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.Number == certificate.Number);

                if (existingCert != null)
                    return BadRequestResponse($"Certificate with number {certificate.Number} already exists");

                certificate.CreatedAt = DateTime.UtcNow;
                certificate.UpdatedAt = DateTime.UtcNow;

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();

                var createdCertificate = await _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .FirstOrDefaultAsync(c => c.Id == certificate.Id);

                return CreatedResponse(createdCertificate, $"/api/v1/certificates/{certificate.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate");
                return HandleError(ex, "Failed to create certificate");
            }
        }

        /// <summary>
        /// Update an existing certificate
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(StandardApiResponse<Certificate>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCertificate(int id, [FromBody] Certificate certificate)
        {
            try
            {
                if (id != certificate.Id)
                    return BadRequestResponse("Certificate ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existingCertificate = await _context.Certificates.FindAsync(id);
                if (existingCertificate == null)
                    return NotFoundResponse($"Certificate with ID {id} not found");

                // Check if certificate number is being changed to an existing one
                if (existingCertificate.Number != certificate.Number)
                {
                    var duplicateCert = await _context.Certificates
                        .FirstOrDefaultAsync(c => c.Number == certificate.Number && c.Id != id);

                    if (duplicateCert != null)
                        return BadRequestResponse($"Certificate with number {certificate.Number} already exists");
                }

                existingCertificate.Number = certificate.Number;
                existingCertificate.ProductId = certificate.ProductId;
                existingCertificate.CompanyId = certificate.CompanyId;
                existingCertificate.CertificationTypeId = certificate.CertificationTypeId;
                existingCertificate.EffectiveDate = certificate.EffectiveDate;
                existingCertificate.ExpirationDate = certificate.ExpirationDate;
                existingCertificate.Status = certificate.Status;
                existingCertificate.CertificateFile = certificate.CertificateFile;
                existingCertificate.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedCertificate = await _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .FirstOrDefaultAsync(c => c.Id == id);

                return SuccessResponse(updatedCertificate, "Certificate updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate {CertificateId}", id);
                return HandleError(ex, $"Failed to update certificate {id}");
            }
        }

        /// <summary>
        /// Delete a certificate
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null)
                    return NotFoundResponse($"Certificate with ID {id} not found");

                _context.Certificates.Remove(certificate);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate {CertificateId}", id);
                return HandleError(ex, $"Failed to delete certificate {id}");
            }
        }

        /// <summary>
        /// Search certificates by various criteria
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Certificate>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchCertificates(
            [FromQuery] string term,
            [FromQuery] int? productId,
            [FromQuery] int? companyId,
            [FromQuery] int? certificationTypeId,
            [FromQuery] DateTime? effectiveDateFrom,
            [FromQuery] DateTime? effectiveDateTo,
            [FromQuery] DateTime? expirationDateFrom,
            [FromQuery] DateTime? expirationDateTo,
            [FromQuery] string status,
            [FromQuery] PaginationParams parameters)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(c =>
                        c.Number.Contains(term) ||
                        c.Product.Name.Contains(term) ||
                        c.Company.Name.Contains(term));
                }

                if (productId.HasValue)
                    query = query.Where(c => c.ProductId == productId.Value);

                if (companyId.HasValue)
                    query = query.Where(c => c.CompanyId == companyId.Value);

                if (certificationTypeId.HasValue)
                    query = query.Where(c => c.CertificationTypeId == certificationTypeId.Value);

                if (effectiveDateFrom.HasValue)
                    query = query.Where(c => c.EffectiveDate >= effectiveDateFrom.Value);

                if (effectiveDateTo.HasValue)
                    query = query.Where(c => c.EffectiveDate <= effectiveDateTo.Value);

                if (expirationDateFrom.HasValue)
                    query = query.Where(c => c.ExpirationDate >= expirationDateFrom.Value);

                if (expirationDateTo.HasValue)
                    query = query.Where(c => c.ExpirationDate <= expirationDateTo.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(c => c.Status == status);

                var totalCount = await query.CountAsync();

                var certificates = await query
                    .OrderByDescending(c => c.EffectiveDate)
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                return PaginatedResponse(certificates, totalCount, parameters.PageNumber, parameters.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching certificates");
                return HandleError(ex, "Failed to search certificates");
            }
        }

        /// <summary>
        /// Get certificates expiring within specified days
        /// </summary>
        [HttpGet("expiring")]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Certificate>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExpiringCertificates(
            [FromQuery] int days = 30,
            [FromQuery] PaginationParams? parameters = null)
        {
            try
            {
                parameters ??= new PaginationParams();
                var cutoffDate = DateTime.UtcNow.AddDays(days);

                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Where(c => c.ExpirationDate <= cutoffDate && c.ExpirationDate >= DateTime.UtcNow)
                    .OrderBy(c => c.ExpirationDate);

                var totalCount = await query.CountAsync();

                var certificates = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                var response = new PaginatedResponse<IEnumerable<Certificate>>
                {
                    Success = true,
                    Data = certificates,
                    TotalCount = totalCount,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = totalPages,
                    HasPrevious = parameters.PageNumber > 1,
                    HasNext = parameters.PageNumber < totalPages,
                    Message = $"Certificates expiring within {days} days"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiring certificates");
                return HandleError(ex, "Failed to retrieve expiring certificates");
            }
        }
    }
}
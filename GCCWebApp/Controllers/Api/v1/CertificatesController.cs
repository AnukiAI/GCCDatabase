using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Attributes;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Controllers.Api.v1
{
    /// <summary>
    /// Certificate management API endpoints
    /// Provides CRUD operations for certificates with comprehensive filtering and search capabilities
    /// </summary>
    /// <remarks>
    /// This controller manages all certificate-related operations including:
    /// - Retrieving certificates with pagination and filtering
    /// - Creating new certificates with validation
    /// - Updating existing certificates
    /// - Deleting certificates
    /// - Advanced search capabilities
    ///
    /// All endpoints require authentication via API Key or JWT token.
    /// Rate limits apply based on authentication method.
    /// </remarks>
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [SwaggerTag("Certificates")]
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
        /// Retrieve certificates with pagination and filtering
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of certificates with optional search filtering.
        ///
        /// **Features:**
        /// - Pagination support (configurable page size)
        /// - Search across certificate number, product name, and company name
        /// - Sorting by multiple fields (certificate number, dates, product, company)
        /// - Response caching for improved performance
        ///
        /// **Search Examples:**
        /// - `searchTerm=24-` - Find certificates starting with "24-"
        /// - `searchTerm=Paper` - Find certificates for products containing "Paper"
        /// - `searchTerm=Green` - Find certificates for companies containing "Green"
        ///
        /// **Sorting Options:**
        /// - `sortBy=certificatenumber` - Sort by certificate number
        /// - `sortBy=issuedate` - Sort by effective date
        /// - `sortBy=expirationdate` - Sort by expiration date
        /// - `sortBy=productname` - Sort by product name
        /// - `sortBy=companyname` - Sort by company name
        ///
        /// Add `sortDescending=true` for descending order.
        /// </remarks>
        /// <param name="parameters">Pagination and filtering parameters</param>
        /// <returns>Paginated list of certificates matching the search criteria</returns>
        /// <response code="200">Returns the paginated certificate list</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Authentication required</response>
        /// <response code="429">Rate limit exceeded</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ApiCache(durationInSeconds: CacheProfiles.Medium, varyByQueryParams: true)]
        [SwaggerOperation(
            Summary = "Get certificates with pagination",
            Description = "Retrieve a paginated list of certificates with optional search and sorting",
            OperationId = "GetCertificates",
            Tags = new[] { "Certificates" }
        )]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Certificate>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Search certificates by product ID (ASIN, UPC, GTIN, EAN, etc.)
        /// </summary>
        /// <remarks>
        /// This endpoint allows partners to find certificates by product identifiers such as:
        /// - **ASIN** (Amazon Standard Identification Number) - e.g., "B08XQRT456"
        /// - **UPC** (Universal Product Code) - e.g., "012345678901"
        /// - **GTIN** (Global Trade Item Number) - e.g., "00012345678901"
        /// - **EAN** (European Article Number) - e.g., "5901234567890"
        /// - **ISBN** (International Standard Book Number)
        /// - **SKU** (Stock Keeping Unit)
        /// - **MPN** (Manufacturer Part Number)
        ///
        /// **Examples:**
        ///
        /// Search by ASIN:
        /// ```
        /// GET /api/v1/certificates/search/productid?productId=B08XQRT456&amp;type=ASIN
        /// ```
        ///
        /// Search by UPC:
        /// ```
        /// GET /api/v1/certificates/search/productid?productId=012345678901&amp;type=UPC
        /// ```
        ///
        /// Search across all product ID types:
        /// ```
        /// GET /api/v1/certificates/search/productid?productId=B08XQRT456
        /// ```
        ///
        /// **Response includes:**
        /// - Certificate details (number, status, expiration date)
        /// - Product and company information
        /// - Matched product ID information
        /// - Download URL for certificate PDF
        ///
        /// **Use Cases:**
        /// - Amazon searching for certificates by ASIN
        /// - Wayfair finding certificates by SKU
        /// - Retailers verifying product certifications by UPC
        /// </remarks>
        /// <param name="productId">The product identifier value (e.g., "B08XQRT456", "012345678901")</param>
        /// <param name="type">Optional product ID type filter: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN. If not provided, searches all types.</param>
        /// <returns>List of certificates matching the product ID</returns>
        /// <response code="200">Returns certificates matching the product ID</response>
        /// <response code="400">Invalid product ID or type parameter</response>
        /// <response code="401">Authentication required</response>
        /// <response code="404">No certificates found for the specified product ID</response>
        /// <response code="429">Rate limit exceeded</response>
        [HttpGet("search/productid")]
        [SwaggerOperation(
            Summary = "Search certificates by product ID (ASIN, UPC, GTIN, EAN, etc.)",
            Description = @"Find certificates using product identifiers like ASIN, UPC, GTIN, EAN, ISBN, SKU, or MPN.

**IMPORTANT**: Returns ALL product IDs associated with each certificate, not just the searched ID.
This is crucial for partners who need to see all ASINs/UPCs for a product family.

**Role-Based Filtering**: External users only see certificates they have access to based on:
- Company associations: Certificates from their associated companies
- Partner associations: Certificates where Partner field matches their partnership (Amazon, Wayfair, etc.)
- OR logic: If associated with BOTH companies and partners, shows certificates matching EITHER
- Admin/Analyst: See all certificates (no filtering)",
            OperationId = "SearchCertificatesByProductId",
            Tags = new[] { "Certificates", "Partner API" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchByProductId(
            [FromQuery][Required][StringLength(100, MinimumLength = 1)] string productId,
            [FromQuery] string? type = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productId))
                    return BadRequestResponse("Product ID is required");

                // Valid product ID types
                var validTypes = new[] { "ASIN", "UPC", "GTIN", "EAN", "ISBN", "SKU", "MPN" };

                if (!string.IsNullOrEmpty(type) && !validTypes.Contains(type.ToUpper()))
                    return BadRequestResponse($"Invalid product ID type. Valid types are: {string.Join(", ", validTypes)}");

                // Get user role and associations for filtering
                var userRole = GetCurrentUserRole();
                var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                var userPartnerNames = await GetCurrentUserPartnerNamesAsync();

                // Query certificate product IDs to find matching certificates
                var matchQuery = _context.CertificateProductIds
                    .Include(cpi => cpi.Certificate)
                        .ThenInclude(c => c.Product)
                    .Include(cpi => cpi.Certificate)
                        .ThenInclude(c => c.Company)
                    .Include(cpi => cpi.Certificate)
                        .ThenInclude(c => c.CertificationType)
                    .Where(cpi => cpi.ProductIdValue == productId);

                // Filter by type if specified
                if (!string.IsNullOrEmpty(type))
                {
                    matchQuery = matchQuery.Where(cpi => cpi.ProductIdType.ToUpper() == type.ToUpper());
                }

                var matchedCertificateProductIds = await matchQuery.ToListAsync();

                if (!matchedCertificateProductIds.Any())
                {
                    var message = string.IsNullOrEmpty(type)
                        ? $"No certificates found for product ID: {productId}"
                        : $"No certificates found for product ID: {productId} (type: {type})";

                    return SuccessResponse(new List<object>(), message);
                }

                // Get unique certificate IDs from matches
                var certificateIds = matchedCertificateProductIds
                    .Select(cpi => cpi.CertificateId)
                    .Distinct()
                    .ToList();

                // Now get all certificates with ALL their product IDs
                var certificatesQuery = _context.Certificates
                    .Include(c => c.Product)
                        .ThenInclude(p => p.ProductType)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.ProductIds)
                    .Where(c => certificateIds.Contains(c.Id));

                // Apply role-based filtering for External users
                if (userRole == "External")
                {
                    bool hasCompanies = userCompanyIds.Any();
                    bool hasPartners = userPartnerNames.Any();

                    if (hasCompanies && hasPartners)
                    {
                        // Show certificates from their companies OR matching their partners
                        certificatesQuery = certificatesQuery.Where(c =>
                            userCompanyIds.Contains(c.CompanyId) ||
                            userPartnerNames.Contains(c.Partner ?? "None")
                        );
                    }
                    else if (hasCompanies)
                    {
                        // Only company filtering
                        certificatesQuery = certificatesQuery.Where(c => userCompanyIds.Contains(c.CompanyId));
                    }
                    else if (hasPartners)
                    {
                        // Only partner filtering
                        certificatesQuery = certificatesQuery.Where(c => userPartnerNames.Contains(c.Partner ?? "None"));
                    }
                    else
                    {
                        // No associations - return empty
                        return SuccessResponse(new List<object>(), "No certificates found matching your access permissions");
                    }
                }
                // Admin and Analyst see all matched certificates (no additional filtering)

                var certificates = await certificatesQuery.ToListAsync();

                if (!certificates.Any())
                {
                    return SuccessResponse(new List<object>(), "No certificates found matching your access permissions");
                }

                // Map to response objects with ALL product IDs for each certificate
                var results = certificates.Select(cert => new
                {
                    id = cert.Id,
                    certificateNumber = cert.Number,
                    productName = cert.Product?.Name,
                    productType = cert.Product?.ProductType?.Name,
                    company = cert.Company?.Name,
                    companyId = cert.CompanyId,
                    partner = cert.Partner,
                    status = cert.Status,
                    certificationType = cert.CertificationType?.Name,
                    effectiveDate = cert.EffectiveDate,
                    expirationDate = cert.ExpirationDate,
                    location = cert.Location,
                    country = cert.Country,
                    pdfUrl = $"/api/v1/certificates/{cert.Number}/download",
                    // Return ALL product IDs associated with this certificate
                    productIds = cert.ProductIds
                        .Where(cpi => cpi.Status == "Active") // Only active product IDs
                        .Select(cpi => new
                        {
                            type = cpi.ProductIdType,
                            value = cpi.ProductIdValue,
                            partner = cpi.Partner,
                            status = cpi.Status,
                            isMatchedId = cpi.ProductIdValue == productId &&
                                         (string.IsNullOrEmpty(type) || cpi.ProductIdType.Equals(type, StringComparison.OrdinalIgnoreCase))
                        })
                        .OrderByDescending(pi => pi.isMatchedId) // Put the matched ID first
                        .ThenBy(pi => pi.type)
                        .ThenBy(pi => pi.value)
                        .ToList()
                }).ToList();

                var totalProductIds = results.Sum(r => r.productIds.Count);
                return SuccessResponse(results, $"Found {results.Count} certificate(s) with {totalProductIds} total product IDs for search: {productId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching certificates by product ID {ProductId}", productId);
                return HandleError(ex, "Failed to search certificates by product ID");
            }
        }

        private string GetCurrentUserRole()
        {
            return User?.FindFirst("Role")?.Value ?? "Public";
        }

        private async Task<List<int>> GetCurrentUserCompanyIdsAsync()
        {
            var userEmail = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return new List<int>();

            // Find user and their associated companies
            var user = await _context.Users
                .Include(u => u.Companies)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user?.Companies != null)
            {
                return user.Companies.Select(c => c.Id).ToList();
            }

            return new List<int>();
        }

        private async Task<List<string>> GetCurrentUserPartnerNamesAsync()
        {
            var userEmail = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return new List<string>();

            // Find user and their associated partners
            var user = await _context.Users
                .Include(u => u.Partners)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user?.Partners != null && user.Partners.Any())
            {
                return user.Partners.Select(p => p.Name).ToList();
            }

            return new List<string>();
        }

        /// <summary>
        /// Download certificate PDF file
        /// </summary>
        /// <remarks>
        /// Downloads the certificate PDF file for the specified certificate.
        ///
        /// **Authentication:** Requires API Key or JWT token
        ///
        /// **Parameters:**
        /// - `id`: Can be either the certificate number (e.g., "24-0366") or database ID (e.g., 146)
        ///
        /// **Examples:**
        ///
        /// Download by certificate number:
        /// ```
        /// GET /api/v1/certificates/24-0366/download
        /// ```
        ///
        /// Download by database ID:
        /// ```
        /// GET /api/v1/certificates/146/download
        /// ```
        ///
        /// **Response:**
        /// - Content-Type: application/pdf
        /// - Content-Disposition: attachment; filename="certificate_24-0366.pdf"
        /// - Binary PDF data
        ///
        /// **Use Cases:**
        /// - Downloading certificate PDFs for partner systems
        /// - Archiving certificates for compliance
        /// - Displaying certificates to end users
        ///
        /// **Notes:**
        /// - If an uploaded certificate file exists, it will be returned
        /// - If no uploaded file exists, a dynamically generated PDF will be created
        /// </remarks>
        /// <param name="id">Certificate number (e.g., "24-0366") or database ID (e.g., 146)</param>
        /// <returns>Certificate PDF file</returns>
        /// <response code="200">Returns the certificate PDF file</response>
        /// <response code="400">Invalid certificate ID format</response>
        /// <response code="401">Authentication required</response>
        /// <response code="404">Certificate not found</response>
        /// <response code="500">Error generating or retrieving PDF</response>
        [HttpGet("{id}/download")]
        [SwaggerOperation(
            Summary = "Download certificate PDF",
            Description = "Download the certificate PDF file by certificate number or ID",
            OperationId = "DownloadCertificate",
            Tags = new[] { "Certificates", "Partner API" }
        )]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [Produces("application/pdf")]
        public async Task<IActionResult> DownloadCertificate([FromRoute] string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequestResponse("Certificate ID or number is required");

                Certificate? certificate = null;

                // Try to parse as integer ID first
                if (int.TryParse(id, out int certId))
                {
                    certificate = await _context.Certificates
                        .Include(c => c.Product)
                        .Include(c => c.Company)
                        .Include(c => c.CertificationType)
                        .FirstOrDefaultAsync(c => c.Id == certId);
                }

                // If not found or not an integer, try as certificate number
                if (certificate == null)
                {
                    certificate = await _context.Certificates
                        .Include(c => c.Product)
                        .Include(c => c.Company)
                        .Include(c => c.CertificationType)
                        .FirstOrDefaultAsync(c => c.Number == id);
                }

                if (certificate == null)
                    return NotFoundResponse($"Certificate '{id}' not found");

                // Check if uploaded certificate file exists
                if (!string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    var filePath = Path.Combine("wwwroot", "uploads", "certificates", certificate.CertificateFile);

                    if (System.IO.File.Exists(filePath))
                    {
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        var fileName = $"certificate_{certificate.Number}.pdf";

                        return File(fileBytes, "application/pdf", fileName);
                    }
                }

                // TODO: If no uploaded file exists, generate PDF dynamically
                // For now, return error indicating PDF generation not implemented
                return NotFoundResponse(
                    "Certificate file not found. PDF generation not yet implemented."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate {CertificateId}", id);
                return HandleError(ex, "Failed to download certificate");
            }
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Extensions;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Attributes;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly GCCWebApp.Services.ICertificateService _certificateService;
        // DEF-391: the v1 create and update endpoints wrote straight to the context with no
        // audit row, the same hole DEF-337 closed for delete. Delete already routes through
        // the service; these two now audit here.
        private readonly GCCWebApp.Services.Audit.IAuditService _auditService;

        public CertificatesController(
            ApplicationDbContext context,
            ILogger<CertificatesController> logger,
            IWebHostEnvironment hostingEnvironment,
            GCCWebApp.Services.ICertificateService certificateService,
            GCCWebApp.Services.Audit.IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
            // DEF-337 (completion): deletes route through the service so the API cannot bypass
            // the audit + cascade-capture the UI path performs.
            _certificateService = certificateService;
            // DEF-208: needed to resolve cert files against ContentRootPath/App_Data
            // instead of wwwroot/. Static-file serving for /uploads/ was removed; all
            // partner-API downloads now read from the App_Data location.
            _hostingEnvironment = hostingEnvironment;
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
        /// - Optional product IDs (ASIN, GTIN, MPN, etc.) for partner integrations
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
        ///
        /// **Partner Integration (Novi, Amazon, etc.):**
        /// Add `includeProductIds=true` to include GTIN, MPN, ASIN, UPC, and other product identifiers.
        /// This is essential for partners who need to map certificates to specific product variants.
        ///
        /// **Example for Amazon/Novi:**
        /// ```
        /// GET /api/v1/certificates?includeProductIds=true&amp;pageSize=100
        /// ```
        ///
        /// **Product Identifier Response Structure:**
        /// When `includeProductIds=true`, each certificate includes a `productIdentifiers` array:
        /// ```json
        /// "productIdentifiers": [
        ///   {
        ///     "type": "ASIN",           // Product ID type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
        ///     "value": "B08N5WRWNW",    // The actual product identifier value
        ///     "status": "Active",       // Partner API contract: always "Active" (only active certs/IDs are returned)
        ///     "partner": "Amazon",      // Partner association: Amazon, Novi, None, Other
        ///     "productName": "Sample Product",  // Optional product name for this ID
        ///     "productSku": "SKU123",           // Optional SKU
        ///     "description": "Sample description" // Optional description/notes
        ///   }
        /// ]
        /// ```
        /// </remarks>
        /// <param name="parameters">Pagination and filtering parameters</param>
        /// <param name="includeProductIds">Include product identifiers (ASIN, GTIN, MPN, UPC, etc.) in response. Default: false</param>
        /// <returns>Paginated list of certificates matching the search criteria</returns>
        /// <response code="200">Returns the paginated certificate list</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Authentication required</response>
        /// <response code="429">Rate limit exceeded</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ApiCache(durationInSeconds: CacheProfiles.Medium, varyByQueryParams: true, varyByUser: true)]
        [SwaggerOperation(
            Summary = "Get certificates with pagination",
            Description = "Retrieve a paginated list of certificates with optional search and sorting. Use includeProductIds=true for partner integrations (Novi, Amazon).",
            OperationId = "GetCertificates",
            Tags = new[] { "Certificates" }
        )]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Certificate>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCertificates(
            [FromQuery] PaginationParams parameters,
            [FromQuery] bool includeProductIds = false)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.CertificationTypeSubcategory)
                    .Include(c => c.Pathway)
                    .AsQueryable();

                // Include ProductIds if requested (for partner integrations like Novi)
                if (includeProductIds)
                {
                    query = query.Include(c => c.ProductIds);
                }

                // Apply role-based filtering for Partner API keys and External users
                var userRole = GetCurrentUserRole();
                var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                var userCompanyIds = await GetCurrentUserCompanyIdsAsync();

                if (userRole == "Partner")
                {
                    // Partner API keys can ONLY see certificates with their partner indicator
                    if (userPartnerNames.Any())
                    {
                        query = query.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)));
                        _logger.LogInformation("Partner API filtering applied for partner(s): {Partners}", string.Join(", ", userPartnerNames));
                    }
                    else
                    {
                        _logger.LogWarning("Partner API key used but no partner name found in claims");
                        return PaginatedResponse(new List<object>(), 0, parameters.PageNumber, parameters.PageSize);
                    }

                    // Partner API contract (Amazon Developer Guide §8): only active certificates are returned;
                    // draft, expired, suspended, and revoked certs are excluded.
                    // CR-043: "active" here is BadgeableStatuses, not PublicStatuses - Confidential is
                    // paid, in-force work that must still be badged (client, 2026-09-02). The response
                    // SHAPE is unchanged; only the row set widens.
                    // DEF-358: "active" also means the effective date has been reached. Partner callers are
                    // always reported status="Active", and Amazon does not validate effectiveDate on its side,
                    // so a cert staged ahead of time would be treated as in-force weeks early. Omit it entirely
                    // rather than adding a new status value — the response shape stays frozen.
                    query = query.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status))
                                 .WhereEffectiveDateReached();
                }
                else if (userRole == "External")
                {
                    bool hasCompanies = userCompanyIds.Any();
                    bool hasPartners = userPartnerNames.Any();

                    if (hasCompanies && hasPartners)
                    {
                        query = query.Where(c =>
                            userCompanyIds.Contains(c.CompanyId) ||
                            c.Partners.Any(p => userPartnerNames.Contains(p.Name))
                        );
                    }
                    else if (hasCompanies)
                    {
                        query = query.Where(c => userCompanyIds.Contains(c.CompanyId));
                    }
                    else if (hasPartners)
                    {
                        query = query.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)));
                    }
                    else
                    {
                        return PaginatedResponse(new List<object>(), 0, parameters.PageNumber, parameters.PageSize);
                    }

                    // CR-043: the owning company sees its own IN-FORCE certificates, which now
                    // includes Confidential. Q1 to the client (2026-09-03): "should a company see
                    // its OWN Confidential certificates when signed in?" - answered Yes. Only the
                    // anonymous public database keeps hiding them (PublicStatuses, untouched).
                    query = query.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status));
                }
                // Admin and Analyst see all certificates (no additional filtering)

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

                var pagedQuery = query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize);

                // Partner API contract (Amazon Developer Guide §4): status field is always "Active"
                // for partner callers (only active certs pass the query filter above).
                // Admin/Analyst callers still see the raw internal lifecycle status.
                var isPartnerCaller = userRole == "Partner";

                // Response shape matches Amazon Developer Guide §4 / §5.1 contract exactly.
                // Cert fields: certificateNumber, status, issueDate, expirationDate,
                // certificationCountries, partner, certificationResults, pathway, company,
                // productName, certificationType, amazonCode, issuer, productIds, pdfDownloadUrl.
                if (includeProductIds)
                {
                    var certificatesWithProductIdsRaw = await pagedQuery
                        .Select(c => new
                        {
                            certificateNumber = c.Number,
                            status = isPartnerCaller ? "Active" : c.Status,
                            issueDate = c.EffectiveDate,
                            expirationDate = c.ExpirationDate,
                            certificationCountries = isPartnerCaller ? "All" : c.Country,
                            partner = c.Partner,
                            // CR-171: caller's own associated partner name(s) on this cert (empty for non-members).
                            ownPartner = c.Partners.Where(p => userPartnerNames.Contains(p.Name)).Select(p => p.Name).FirstOrDefault(),
                            // CR-171: ALL associated names for non-partner callers (matrix: company/internal see all).
                            allPartners = c.Partners.Select(p => p.Name).ToList(),
                            certificationResults = c.CertificationResults,
                            pathway = c.Pathway != null ? c.Pathway.Name : null,
                            company = c.Company != null ? c.Company.Name : null,
                            productName = c.Product != null ? c.Product.Name : null,
                            certificationType = c.CertificationType != null ? c.CertificationType.Name : null,
                            amazonCode = c.CertificationType != null ? c.CertificationType.AmazonCode : null,
                            issuer = "GreenCircle Certified",
                            productIds = c.ProductIds
                                // CR-171 Model A: partner callers get untagged (shared) product IDs + those tagged to them.
                                .Where(pi => pi.Status == "Active" &&
                                    (!isPartnerCaller || !pi.Partners.Any() || pi.Partners.Any(p => userPartnerNames.Contains(p.Name))))
                                .Select(pi => new
                                {
                                    type = pi.ProductIdType,
                                    value = pi.ProductIdValue,
                                    status = pi.Status,
                                    partner = pi.Partner,
                                    // caller's own matching tag(s) on this product ID (for the partner-scoped label)
                                    ownPartner = pi.Partners.Where(p => userPartnerNames.Contains(p.Name)).Select(p => p.Name).ToList(),
                                    productName = pi.ProductName,
                                    productSku = pi.ProductSku
                                })
                                .ToList(),
                            pdfDownloadUrl = $"/api/v1/certificates/{c.Number}/download"
                        })
                        .ToListAsync();

                    // CR-045: certificationResults is passed through VERBATIM. This block used to
                    // call FormatCertificationResults to turn a stored "0.17" into "17%"; that
                    // formatter is deleted and must not come back — see Models/Certificate.cs.
                    // CR-171: scope partner output — partner callers see only their own association;
                    // product-ID labels caller-scoped (Model A). Non-partner callers see the primary value.
                    var certificatesWithProductIds = certificatesWithProductIdsRaw.Select(x => new
                    {
                        x.certificateNumber, x.status, x.issueDate, x.expirationDate,
                        x.certificationCountries,
                        partner = isPartnerCaller ? (x.ownPartner ?? "") : string.Join(", ", x.allPartners),
                        certificationResults = x.certificationResults,
                        x.pathway, x.company, x.productName, x.certificationType, x.amazonCode,
                        x.issuer,
                        productIds = x.productIds.Select(pi => new
                        {
                            pi.type, pi.value, pi.status,
                            partner = isPartnerCaller ? string.Join(", ", pi.ownPartner) : pi.partner,
                            pi.productName, pi.productSku
                        }).ToList(),
                        x.pdfDownloadUrl
                    }).ToList();

                    return PaginatedResponse(certificatesWithProductIds, totalCount, parameters.PageNumber, parameters.PageSize);
                }
                else
                {
                    var certificatesRaw = await pagedQuery
                        .Select(c => new
                        {
                            certificateNumber = c.Number,
                            status = isPartnerCaller ? "Active" : c.Status,
                            issueDate = c.EffectiveDate,
                            expirationDate = c.ExpirationDate,
                            certificationCountries = isPartnerCaller ? "All" : c.Country,
                            partner = c.Partner,
                            // CR-171: caller's own associated partner name(s) on this cert (empty for non-members).
                            ownPartner = c.Partners.Where(p => userPartnerNames.Contains(p.Name)).Select(p => p.Name).FirstOrDefault(),
                            // CR-171: ALL associated names for non-partner callers (matrix: company/internal see all).
                            allPartners = c.Partners.Select(p => p.Name).ToList(),
                            certificationResults = c.CertificationResults,
                            pathway = c.Pathway != null ? c.Pathway.Name : null,
                            company = c.Company != null ? c.Company.Name : null,
                            productName = c.Product != null ? c.Product.Name : null,
                            certificationType = c.CertificationType != null ? c.CertificationType.Name : null,
                            amazonCode = c.CertificationType != null ? c.CertificationType.AmazonCode : null,
                            issuer = "GreenCircle Certified",
                            pdfDownloadUrl = $"/api/v1/certificates/{c.Number}/download"
                        })
                        .ToListAsync();

                    // CR-171: partner callers see only their own association; others see all associated names.
                    var certificates = certificatesRaw.Select(x => new
                    {
                        x.certificateNumber, x.status, x.issueDate, x.expirationDate,
                        x.certificationCountries,
                        partner = isPartnerCaller ? (x.ownPartner ?? "") : string.Join(", ", x.allPartners),
                        certificationResults = x.certificationResults,
                        x.pathway, x.company, x.productName, x.certificationType, x.amazonCode,
                        x.issuer, x.pdfDownloadUrl
                    }).ToList();

                    return PaginatedResponse(certificates, totalCount, parameters.PageNumber, parameters.PageSize);
                }
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
                    .Include(c => c.CertificationTypeSubcategory)
                    .Include(c => c.Pathway)
                    .Include(c => c.Partners)
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners)
                    .Include(c => c.CustomFieldValues)
                        .ThenInclude(cfv => cfv.Field)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (certificate == null)
                    return NotFoundResponse($"Certificate with ID {id} not found");

                var isPartnerCaller = GetCurrentUserRole() == "Partner";

                if (isPartnerCaller)
                {
                    // Partner API contract: non-active certs are not visible to partners.
                    // DEF-358: a cert staged ahead of its effective date is not yet in force —
                    // 404 exactly like a non-active one, so the response shape is unchanged.
                    if (!CertificateStatusHelper.BadgeableStatuses.Contains(certificate.Status)
                        || CertificateVisibility.IsNotYetEffective(certificate))
                        return NotFoundResponse($"Certificate with ID {id} not found");

                    // DEF-106: scope by partner. A Partner caller (API key OR Partner-role user)
                    // can only see certs whose Partner matches one of their assigned partners.
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    if (!userPartnerNames.Any() || !certificate.Partners.Any(p => userPartnerNames.Contains(p.Name)))
                        return NotFoundResponse($"Certificate with ID {id} not found");

                    return SuccessResponse(BuildPartnerCertificateDto(certificate, userPartnerNames));
                }

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
            [FromRoute][Required][StringLength(50, MinimumLength = 1, ErrorMessage = "Certificate number must be between 1 and 50 characters")] string certificateNumber)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.CertificationTypeSubcategory)
                    .Include(c => c.Pathway)
                    .Include(c => c.Partners)
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners)
                    .Include(c => c.CustomFieldValues)
                        .ThenInclude(cfv => cfv.Field)
                    .OrderByDescending(c => c.EffectiveDate).ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null)
                    return NotFoundResponse($"Certificate with number {certificateNumber} not found");

                var isPartnerCaller = GetCurrentUserRole() == "Partner";

                if (isPartnerCaller)
                {
                    // Partner API contract: non-active certs are not visible to partners.
                    // DEF-358: not-yet-effective certs 404 the same way.
                    if (!CertificateStatusHelper.BadgeableStatuses.Contains(certificate.Status)
                        || CertificateVisibility.IsNotYetEffective(certificate))
                        return NotFoundResponse($"Certificate with number {certificateNumber} not found");

                    // DEF-106: scope by partner.
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    if (!userPartnerNames.Any() || !certificate.Partners.Any(p => userPartnerNames.Contains(p.Name)))
                        return NotFoundResponse($"Certificate with number {certificateNumber} not found");

                    return SuccessResponse(BuildPartnerCertificateDto(certificate, userPartnerNames));
                }

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

                // DEF-391: audit the create. userId 0 is deliberate — an API-key caller has no
                // Users row, so AuditService resolves the configured service account
                // (Audit:SystemUserEmail) and records the key's name as the actor.
                await AuditApiChangeAsync(
                    AuditAction.Create, certificate.Id, certificate.Number, null, certificate);

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

                // DEF-391: detached snapshot of the stored row BEFORE the assignments below.
                // FindAsync returns a tracked entity, so without this second, no-tracking read
                // the audit diff would compare the mutated object against itself.
                var certificateBeforeUpdate = await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

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

                // DEF-391: audit the field-level diff, after the commit.
                await AuditApiChangeAsync(
                    AuditAction.Update, id, existingCertificate.Number,
                    certificateBeforeUpdate, existingCertificate);

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
        /// DEF-391: one audit call for both v1 write endpoints.
        ///
        /// The acting user is passed as 0 on purpose. An API-key caller has no Users row, so
        /// AuditService falls back to the service account named by Audit:SystemUserEmail; the
        /// key's own name is carried in the username so the row still says WHICH caller did it
        /// (e.g. "API key: Amazon Production"). If that setting is unset the audit is refused
        /// and logged as a gap rather than written against an arbitrary user.
        ///
        /// Never throws: an audit failure must not turn a committed write into a 500 for a
        /// partner. It is logged loudly instead.
        /// </summary>
        private async Task AuditApiChangeAsync(
            string action, int certificateId, string? certificateNumber,
            Certificate? before, Certificate? after)
        {
            try
            {
                var keyName = User?.FindFirst("api_key_name")?.Value
                              ?? User?.FindFirst("partner_name")?.Value
                              ?? User?.Identity?.Name;

                await _auditService.LogEntityChangeAsync(
                    certificateId.ToString(),
                    certificateNumber,
                    action,
                    before,
                    after,
                    0,
                    string.IsNullOrWhiteSpace(keyName) ? "API" : $"API key: {keyName}",
                    reason: null,
                    changeSource: AuditChangeSource.API);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx,
                    "AUDIT GAP: certificate {Id} was changed via the v1 API ({Action}) but the audit row was not written",
                    certificateId, action);
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

                // DEF-337 (completion): this removed the certificate directly from the context,
                // so it wrote no audit row and destroyed all seven cascade child sets silently —
                // the same hole DEF-337 closed on the UI path, still open on the API. Delegating
                // to the service means both paths audit identically and cannot drift.
                if (!await _certificateService.DeleteCertificateAsync(
                        id, userRole: "API", userId: 0, username: null))
                {
                    return HandleError(
                        new InvalidOperationException($"Delete failed for certificate {id}"),
                        $"Failed to delete certificate {id}");
                }

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
        /// <remarks>
        /// DEF-129: Partner callers receive the contract DTO (status="Active",
        /// certificationCountries="All", productIds[] array, no internal IDs, public-status filter
        /// applied). Admin/Analyst still see the full entity for internal dashboards.
        /// </remarks>
        [HttpGet("search")]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchCertificates(
            // DEF-269: term and status are OPTIONAL filters (the body already null-guards both), but
            // as non-nullable reference types .NET 8 binds them as implicitly [Required], so an empty
            // ?term= 400'd. Marked nullable so the documented optional-filter behavior actually works.
            [FromQuery] string? term,
            [FromQuery] int? productId,
            [FromQuery] int? companyId,
            [FromQuery] int? certificationTypeId,
            [FromQuery] DateTime? effectiveDateFrom,
            [FromQuery] DateTime? effectiveDateTo,
            [FromQuery] DateTime? expirationDateFrom,
            [FromQuery] DateTime? expirationDateTo,
            [FromQuery] string? status,
            [FromQuery] PaginationParams parameters)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var isPartnerCaller = userRole == "Partner";
                // CR-171: hoisted to method scope so the partner-scoped DTO mapping below can use it.
                var userPartnerNames = isPartnerCaller ? await GetCurrentUserPartnerNamesAsync() : new List<string>();

                // DEF-129: include nav props the partner DTO needs (Pathway, ProductIds).
                // Company is already included; the DTO projection prevents the circular
                // Company.Certificates serialization that previously caused issues.
                // CR-171: Partners included so BuildPartnerCertificateDto can emit caller-scoped partner.
                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Pathway)
                    .Include(c => c.Partners)
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners)
                    .AsSplitQuery() // CR-171: two collection includes (ProductIds + their Partners) under paging
                    .AsQueryable();

                if (!string.IsNullOrEmpty(term))
                {
                    var lowerTerm = term.ToLower();
                    query = query.Where(c =>
                        c.Number.ToLower().Contains(lowerTerm) ||
                        c.Product.Name.ToLower().Contains(lowerTerm) ||
                        c.Company.Name.ToLower().Contains(lowerTerm) ||
                        c.Tags.Any(t => t.Name.ToLower().Contains(lowerTerm)));
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

                // DEF-269: this is a literal match against the INTERNAL lifecycle status
                // (e.g. "Client Approved - Paid", "Republished"). Partner callers are always
                // reported status="Active" and are already constrained to BadgeableStatuses below, so
                // a partner passing ?status=Active would match zero rows. Honor the filter only for
                // internal callers (Admin/Analyst); ignore it for partners.
                if (!string.IsNullOrEmpty(status) && !isPartnerCaller)
                    query = query.Where(c => c.Status == status);

                if (isPartnerCaller)
                {
                    // Partner API contract (Amazon Developer Guide §4 / §8): partners only see
                    // public-status certs scoped to their partner association.
                    if (!userPartnerNames.Any())
                    {
                        _logger.LogWarning("Partner API key used but no partner name found in claims");
                        return Ok(new PaginatedResponse<IEnumerable<object>>
                        {
                            Success = true,
                            Data = new List<object>(),
                            TotalCount = 0,
                            PageNumber = parameters.PageNumber,
                            PageSize = parameters.PageSize,
                            TotalPages = 0,
                            HasPrevious = false,
                            HasNext = false,
                            Message = "No certificates found matching your access permissions"
                        });
                    }

                    // DEF-358: effective-date lower bound alongside the status + partner scoping.
                    query = query.Where(c =>
                        CertificateStatusHelper.BadgeableStatuses.Contains(c.Status) &&
                        c.Partners.Any(p => userPartnerNames.Contains(p.Name)))
                        .WhereEffectiveDateReached();
                }

                var totalCount = await query.CountAsync();

                var certificates = await query
                    .OrderByDescending(c => c.EffectiveDate)
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                IEnumerable<object> data = isPartnerCaller
                    ? certificates.Select(c => BuildPartnerCertificateDto(c, userPartnerNames)).ToList()
                    : certificates.Cast<object>().ToList();

                return Ok(new PaginatedResponse<IEnumerable<object>>
                {
                    Success = true,
                    Data = data,
                    TotalCount = totalCount,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = totalPages,
                    HasPrevious = parameters.PageNumber > 1,
                    HasNext = parameters.PageNumber < totalPages
                });
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
        /// <remarks>
        /// DEF-124: Partner callers receive the contract DTO (status="Active", certificationCountries="All",
        /// productIds[] array, no internal IDs, public-status filter applied). Admin/Analyst still see the
        /// full entity for internal dashboards.
        /// </remarks>
        [HttpGet("expiring")]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExpiringCertificates(
            [FromQuery] int days = 30,
            [FromQuery] PaginationParams? parameters = null)
        {
            try
            {
                parameters ??= new PaginationParams();
                var cutoffDate = DateTime.UtcNow.AddDays(days);

                var userRole = GetCurrentUserRole();
                var isPartnerCaller = userRole == "Partner";
                // CR-171: hoisted to method scope so the partner-scoped DTO mapping below can use it.
                var userPartnerNames = isPartnerCaller ? await GetCurrentUserPartnerNamesAsync() : new List<string>();

                // DEF-124: include all nav props the partner DTO reads (Company name is in the
                // contract — Amazon expects it). The DTO projection prevents the circular
                // Company.Certificates serialization that previously blew the response to 1GB+.
                // CR-171: Partners included so BuildPartnerCertificateDto can emit caller-scoped partner.
                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Pathway)
                    .Include(c => c.Partners)
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners)
                    .AsSplitQuery() // CR-171: two collection includes under paging
                    .Where(c => c.ExpirationDate <= cutoffDate && c.ExpirationDate >= CertificateStatusHelper.BusinessToday);

                if (isPartnerCaller)
                {
                    // Partner API contract (Amazon Developer Guide §4 / §8): partners only see
                    // public-status certs scoped to their partner association.
                    if (!userPartnerNames.Any())
                    {
                        _logger.LogWarning("Partner API key used but no partner name found in claims");
                        return Ok(new PaginatedResponse<IEnumerable<object>>
                        {
                            Success = true,
                            Data = new List<object>(),
                            TotalCount = 0,
                            PageNumber = parameters.PageNumber,
                            PageSize = parameters.PageSize,
                            TotalPages = 0,
                            HasPrevious = false,
                            HasNext = false,
                            Message = $"No certificates expiring within {days} days matching your access permissions"
                        });
                    }

                    // DEF-358: a not-yet-effective cert is not in force, so it cannot be "expiring soon"
                    // from the partner's perspective either — apply the same lower bound here.
                    query = query.Where(c =>
                        CertificateStatusHelper.BadgeableStatuses.Contains(c.Status) &&
                        c.Partners.Any(p => userPartnerNames.Contains(p.Name)))
                        .WhereEffectiveDateReached();
                }

                query = query.OrderBy(c => c.ExpirationDate);

                var totalCount = await query.CountAsync();

                var certificates = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                IEnumerable<object> data = isPartnerCaller
                    ? certificates.Select(c => BuildPartnerCertificateDto(c, userPartnerNames)).ToList()
                    : certificates.Cast<object>().ToList();

                var response = new PaginatedResponse<IEnumerable<object>>
                {
                    Success = true,
                    Data = data,
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
        /// - ALL product IDs associated with the certificate
        /// - Download URL for certificate PDF
        ///
        /// **Product ID Response Structure:**
        /// Each certificate includes a `productIds` array with ALL associated identifiers:
        /// ```json
        /// "productIds": [
        ///   {
        ///     "type": "ASIN",           // Product ID type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
        ///     "value": "B08N5WRWNW",    // The actual product identifier value
        ///     "status": "Active",       // Partner API contract: always "Active" (only active certs/IDs are returned)
        ///     "partner": "Amazon",      // Partner association: Amazon, Novi, None, Other
        ///     "productName": "Sample Product",  // Optional product name for this ID
        ///     "productSku": "SKU123",           // Optional SKU
        ///     "description": "Sample description", // Optional description/notes
        ///     "isMatchedId": true       // True if this ID matched the search query
        ///   }
        /// ]
        /// ```
        ///
        /// **Use Cases:**
        /// - Amazon searching for certificates by ASIN
        /// - Novi finding certificates by SKU
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
- Partner associations: Certificates where Partner field matches their partnership (Amazon, Novi, etc.)
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
                    .Include(c => c.ProductType)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Pathway)
                    .Include(c => c.Partners) // CR-171: caller-scoped partner output
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners) // CR-171 Model A: per-product partner filtering
                    .AsSplitQuery()
                    .Where(c => certificateIds.Contains(c.Id));

                // Apply role-based filtering for Partner API keys
                if (userRole == "Partner")
                {
                    // Partner API keys can ONLY see certificates with their partner indicator
                    if (userPartnerNames.Any())
                    {
                        certificatesQuery = certificatesQuery.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)));
                        _logger.LogInformation("Partner API filtering applied for partner(s): {Partners}", string.Join(", ", userPartnerNames));
                    }
                    else
                    {
                        // No partner association found - this shouldn't happen with valid Partner API key
                        _logger.LogWarning("Partner API key used but no partner name found in claims");
                        return SuccessResponse(new List<object>(), "No certificates found matching your access permissions");
                    }

                    // Partner API contract (Amazon Developer Guide §8): only active certificates are returned.
                    // DEF-358: plus the effective-date lower bound.
                    certificatesQuery = certificatesQuery.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status))
                                                         .WhereEffectiveDateReached();
                }
                // Apply role-based filtering for External users (logged in via session)
                else if (userRole == "External")
                {
                    bool hasCompanies = userCompanyIds.Any();
                    bool hasPartners = userPartnerNames.Any();

                    if (hasCompanies && hasPartners)
                    {
                        // Show certificates from their companies OR matching their partners
                        certificatesQuery = certificatesQuery.Where(c =>
                            userCompanyIds.Contains(c.CompanyId) ||
                            c.Partners.Any(p => userPartnerNames.Contains(p.Name))
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
                        certificatesQuery = certificatesQuery.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)));
                    }
                    else
                    {
                        // No associations - return empty
                        return SuccessResponse(new List<object>(), "No certificates found matching your access permissions");
                    }

                    // CR-043 Q1 (Yes): the owning company sees its own in-force certificates,
                    // Confidential included. Anonymous public stays on PublicStatuses.
                    certificatesQuery = certificatesQuery.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status));
                }
                // Admin and Analyst see all matched certificates (no additional filtering)

                var certificates = await certificatesQuery.ToListAsync();

                if (!certificates.Any())
                {
                    return SuccessResponse(new List<object>(), "No certificates found matching your access permissions");
                }

                // Partner API contract (Amazon Developer Guide §4): status field is always "Active" for partner callers
                var isPartnerCaller = userRole == "Partner";

                // Map to response objects with ALL product IDs for each certificate
                var results = certificates.Select(cert => new
                {
                    // Response shape matches Amazon Developer Guide §4 / §5.3 contract.
                    certificateNumber = cert.Number,
                    status = isPartnerCaller ? "Active" : cert.Status,
                    issueDate = cert.EffectiveDate,
                    expirationDate = cert.ExpirationDate,
                    certificationCountries = isPartnerCaller ? "All" : cert.Country,
                    // CR-171: partner callers see only their own association; others see the primary.
                    partner = isPartnerCaller
                        ? string.Join(", ", cert.Partners.Where(p => userPartnerNames.Contains(p.Name)).Select(p => p.Name))
                        : cert.Partner,
                    certificationResults = cert.CertificationResults,
                    pathway = cert.Pathway?.Name,
                    company = cert.Company?.Name,
                    productName = cert.Product?.Name,
                    certificationType = cert.CertificationType?.Name,
                    amazonCode = cert.CertificationType?.AmazonCode,
                    issuer = "GreenCircle Certified",
                    productIds = cert.ProductIds
                        // CR-171 Model A: drop co-partner-only product IDs for partner callers.
                        .Where(cpi => cpi.Status == "Active" && ProductIdVisibleToCaller(cpi, isPartnerCaller, userPartnerNames))
                        .Select(cpi => new
                        {
                            type = cpi.ProductIdType,
                            value = cpi.ProductIdValue,
                            status = cpi.Status,
                            partner = ScopedProductIdPartner(cpi, isPartnerCaller, userPartnerNames),
                            productName = cpi.ProductName,
                            productSku = cpi.ProductSku,
                            // isMatchedId flag is documented in §5.3 — marks which ID matched the search
                            isMatchedId = cpi.ProductIdValue == productId &&
                                         (string.IsNullOrEmpty(type) || cpi.ProductIdType.Equals(type, StringComparison.OrdinalIgnoreCase))
                        })
                        .OrderByDescending(pi => pi.isMatchedId) // Put the matched ID first
                        .ThenBy(pi => pi.type)
                        .ThenBy(pi => pi.value)
                        .ToList(),
                    pdfDownloadUrl = $"/api/v1/certificates/{cert.Number}/download"
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
            // Check for role claim (supports both custom "Role" claim and standard ClaimTypes.Role)
            var role = User?.FindFirst("Role")?.Value
                    ?? User?.FindFirst(ClaimTypes.Role)?.Value;

            // Partner API keys get "Partner" role from middleware
            if (role == "Partner")
            {
                return "Partner";
            }

            return role ?? "Public";
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
            // First check if this is a Partner API key request (has partner_name claim directly)
            var partnerNameClaim = User?.FindFirst("partner_name")?.Value;
            if (!string.IsNullOrEmpty(partnerNameClaim))
            {
                // Partner API key - return the partner name from the claim
                return new List<string> { partnerNameClaim };
            }

            // Otherwise, look up user by email (for External users logged in via session)
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
                        .Include(c => c.Partners)
                        .FirstOrDefaultAsync(c => c.Id == certId);
                }

                // If not found or not an integer, try as certificate number
                if (certificate == null)
                {
                    certificate = await _context.Certificates
                        .Include(c => c.Product)
                        .Include(c => c.Company)
                        .Include(c => c.CertificationType)
                        .Include(c => c.Partners)
                        .OrderByDescending(c => c.EffectiveDate).ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync(c => c.Number == id);
                }

                if (certificate == null)
                    return NotFoundResponse($"Certificate '{id}' not found");

                if (GetCurrentUserRole() == "Partner")
                {
                    // Partner API contract: non-active certs are not visible to partners.
                    // DEF-358: not-yet-effective certs 404 the same way — no PDF before the effective date.
                    if (!CertificateStatusHelper.BadgeableStatuses.Contains(certificate.Status)
                        || CertificateVisibility.IsNotYetEffective(certificate))
                        return NotFoundResponse($"Certificate '{id}' not found");

                    // DEF-106: scope by partner.
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    if (!userPartnerNames.Any() || !certificate.Partners.Any(p => userPartnerNames.Contains(p.Name)))
                        return NotFoundResponse($"Certificate '{id}' not found");
                }

                // DEF-208: resolve cert file against the new App_Data base (was wwwroot).
                // Handles both "uploads/certificates/file.pdf" (full relative path) and
                // bare "file.pdf" (just the filename — legacy compatibility). Migration
                // fallback to wwwroot keeps existing files reachable through this
                // endpoint until the one-time server-side Move-Item runs.
                if (!string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    string subPath = certificate.CertificateFile.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
                        ? certificate.CertificateFile.Replace('/', Path.DirectorySeparatorChar)
                        : Path.Combine("uploads", "certificates", certificate.CertificateFile);

                    var newPath = Path.Combine(_hostingEnvironment.ContentRootPath, "App_Data", subPath);
                    var legacyPath = Path.Combine(_hostingEnvironment.WebRootPath ?? "", subPath);
                    var filePath = System.IO.File.Exists(newPath) ? newPath
                                  : System.IO.File.Exists(legacyPath) ? legacyPath
                                  : null;

                    if (filePath != null)
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

        /// <summary>
        /// Get certificates in GS1 JSON-LD format for Amazon integration
        /// </summary>
        /// <remarks>
        /// Returns certificates in GS1 Web Vocabulary JSON-LD format suitable for Amazon Climate Pledge Friendly.
        ///
        /// **GS1 JSON-LD Structure:**
        /// This endpoint returns data following the GS1 Certification Data Model specification:
        /// - `@context`: Points to GS1 certification context file
        /// - `$schema`: Points to GS1 schema for validation
        /// - Nested `products[]` array with GTIN and additional trade item IDs (ASIN, UPC, etc.)
        /// - Nested `certificationInfo` object with certification details
        ///
        /// **Status Mapping:**
        /// - Client Approved (Paid/Confidential/Not Paid), Republished → ACTIVE
        /// - Draft, Hold, Expired → INACTIVE
        ///
        /// **Product Identifier Mapping:**
        /// - UPC/GTIN/EAN values become the `gtin` field (padded to 14 digits)
        /// - ASIN, SKU, MPN, ISBN become `additionalTradeItemId` entries
        ///
        /// **Example Response:**
        /// ```json
        /// {
        ///   "@context": "https://gs1us-technology.github.io/certification-data-model/cert_context.json",
        ///   "$schema": "https://gs1us-technology.github.io/certification-data-model/cert_schema.json",
        ///   "id": "https://greencirclecertified.com/api/v1/certificates/gs1/24-0366",
        ///   "type": "CertificationReport",
        ///   "issueDate": "2024-01-15",
        ///   "issuer": "GreenCircle Certified",
        ///   "products": [
        ///     {
        ///       "id": "https://id.gs1.org/01/00123456789012",
        ///       "type": "Product",
        ///       "gtin": "00123456789012",
        ///       "additionalTradeItemId": [
        ///         { "type": "ASIN", "value": "B08XQRT456", "name": "Recycled Paper Towels - 12 pack" }
        ///       ],
        ///       "name": "Recycled Paper Towels",
        ///       "certificationInfo": {
        ///         "type": "CertificationDetails",
        ///         "agency": "GreenCircle Certified",
        ///         "program": "UL 2809",
        ///         "certificateId": "24-0366",
        ///         "status": "ACTIVE",
        ///         "value": "95.5% recycled content",
        ///         "startDate": "2024-01-15",
        ///         "endDate": "2025-01-15"
        ///       }
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="parameters">Pagination parameters</param>
        /// <returns>Certificates in GS1 JSON-LD format</returns>
        /// <response code="200">Returns the certificates in GS1 format</response>
        /// <response code="401">Authentication required</response>
        [HttpGet("gs1")]
        [SwaggerOperation(
            Summary = "Get certificates in GS1 JSON-LD format",
            Description = "Returns certificates in GS1 Web Vocabulary JSON-LD format for Amazon Climate Pledge Friendly integration.",
            OperationId = "GetCertificatesGS1",
            Tags = new[] { "Certificates", "GS1", "Amazon" }
        )]
        [ProducesResponseType(typeof(GS1CertificationReportListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCertificatesGS1([FromQuery] PaginationParams parameters)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.CertificationTypeSubcategory)
                    .Include(c => c.Pathway)
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners) // CR-171 Model A: per-product partner filtering
                    .AsSplitQuery()
                    .AsQueryable();

                // Apply role-based filtering for Partner API keys (especially important for GS1/Amazon endpoint)
                var userRole = GetCurrentUserRole();
                var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                var userCompanyIds = await GetCurrentUserCompanyIdsAsync();

                if (userRole == "Partner")
                {
                    if (userPartnerNames.Any())
                    {
                        query = query.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)));
                        _logger.LogInformation("GS1 endpoint: Partner API filtering applied for partner(s): {Partners}", string.Join(", ", userPartnerNames));
                    }
                    else
                    {
                        _logger.LogWarning("GS1 endpoint: Partner API key used but no partner name found");
                        return Ok(new GS1CertificationReportListResponse
                        {
                            Success = true,
                            Data = new List<GS1CertificationReport>(),
                            Pagination = new PaginationMetadata { TotalCount = 0 },
                            Message = "No certificates found matching your access permissions"
                        });
                    }

                    // Partner API contract (Amazon Developer Guide §8): only active certificates are returned.
                    // DEF-358: plus the effective-date lower bound.
                    query = query.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status))
                                 .WhereEffectiveDateReached();
                }
                else if (userRole == "External")
                {
                    bool hasCompanies = userCompanyIds.Any();
                    bool hasPartners = userPartnerNames.Any();

                    if (hasCompanies && hasPartners)
                    {
                        query = query.Where(c =>
                            userCompanyIds.Contains(c.CompanyId) ||
                            c.Partners.Any(p => userPartnerNames.Contains(p.Name))
                        );
                    }
                    else if (hasCompanies)
                    {
                        query = query.Where(c => userCompanyIds.Contains(c.CompanyId));
                    }
                    else if (hasPartners)
                    {
                        query = query.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)));
                    }
                    else
                    {
                        return Ok(new GS1CertificationReportListResponse
                        {
                            Success = true,
                            Data = new List<GS1CertificationReport>(),
                            Pagination = new PaginationMetadata { TotalCount = 0 },
                            Message = "No certificates found matching your access permissions"
                        });
                    }

                    // CR-043 Q1 (Yes): the owning company sees its own in-force certificates,
                    // Confidential included. Anonymous public stays on PublicStatuses.
                    query = query.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status));
                }
                // Admin and Analyst see all certificates

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(parameters.SearchTerm))
                {
                    query = query.Where(c =>
                        c.Number.Contains(parameters.SearchTerm) ||
                        c.Product.Name.Contains(parameters.SearchTerm) ||
                        c.Company.Name.Contains(parameters.SearchTerm));
                }

                var totalCount = await query.CountAsync();

                // Order by effective date descending by default
                query = query.OrderByDescending(c => c.EffectiveDate);

                var certificates = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                // Transform to GS1 format (CR-171: product IDs scoped per partner caller)
                var gs1Reports = certificates.Select(cert => MapToGS1Format(cert, Request, userRole == "Partner", userPartnerNames)).ToList();

                var response = new GS1CertificationReportListResponse
                {
                    Success = true,
                    Data = gs1Reports,
                    Pagination = new PaginationMetadata
                    {
                        TotalCount = totalCount,
                        PageNumber = parameters.PageNumber,
                        PageSize = parameters.PageSize,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize),
                        HasPrevious = parameters.PageNumber > 1,
                        HasNext = parameters.PageNumber < (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
                    },
                    Message = $"Found {totalCount} certificate(s) in GS1 format"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates in GS1 format");
                return HandleError(ex, "Failed to retrieve certificates in GS1 format");
            }
        }

        /// <summary>
        /// Get a single certificate in GS1 JSON-LD format
        /// </summary>
        /// <param name="certificateNumber">Certificate number (e.g., "24-0366")</param>
        /// <returns>Single certificate in GS1 JSON-LD format</returns>
        [HttpGet("gs1/{certificateNumber}")]
        [SwaggerOperation(
            Summary = "Get single certificate in GS1 format",
            Description = "Returns a single certificate in GS1 Web Vocabulary JSON-LD format.",
            OperationId = "GetCertificateGS1",
            Tags = new[] { "Certificates", "GS1", "Amazon" }
        )]
        [ProducesResponseType(typeof(GS1CertificationReport), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCertificateGS1(
            [FromRoute][Required] string certificateNumber)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Product)
                    .Include(c => c.Company)
                    .Include(c => c.CertificationType)
                    .Include(c => c.CertificationTypeSubcategory)
                    .Include(c => c.Pathway)
                    .Include(c => c.Partners)
                    .Include(c => c.ProductIds)
                        .ThenInclude(pi => pi.Partners) // CR-171 Model A: per-product partner filtering
                    .AsSplitQuery()
                    .OrderByDescending(c => c.EffectiveDate).ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null)
                    return NotFoundResponse($"Certificate '{certificateNumber}' not found");

                var isPartnerCaller = GetCurrentUserRole() == "Partner";
                var userPartnerNames = isPartnerCaller ? await GetCurrentUserPartnerNamesAsync() : new List<string>();
                if (isPartnerCaller)
                {
                    // Partner API contract: non-active certs are not visible to partners.
                    // DEF-358: not-yet-effective certs 404 the same way (GS1 surface).
                    if (!CertificateStatusHelper.BadgeableStatuses.Contains(certificate.Status)
                        || CertificateVisibility.IsNotYetEffective(certificate))
                        return NotFoundResponse($"Certificate '{certificateNumber}' not found");

                    // DEF-106: scope by partner.
                    if (!userPartnerNames.Any() || !certificate.Partners.Any(p => userPartnerNames.Contains(p.Name)))
                        return NotFoundResponse($"Certificate '{certificateNumber}' not found");
                }

                var gs1Report = MapToGS1Format(certificate, Request, isPartnerCaller, userPartnerNames);
                return Ok(gs1Report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {CertificateNumber} in GS1 format", certificateNumber);
                return HandleError(ex, "Failed to retrieve certificate in GS1 format");
            }
        }

        /// <summary>
        /// Maps a certificate entity to GS1 JSON-LD format
        /// </summary>
        private GS1CertificationReport MapToGS1Format(Certificate cert, HttpRequest request, bool isPartnerCaller, List<string> callerPartnerNames)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            // CR-171 Model A: a partner must not receive a co-partner's product-ID VALUES (GTIN/ASIN/SKU)
            // on a shared cert — drop co-partner-only IDs here too (the GS1 surface had no partner field,
            // but the identifier values themselves are partner data). Requires pi.Partners eager-loaded.
            var productIds = (cert.ProductIds ?? new List<CertificateProductId>())
                .Where(pi => pi.Status == "Active" && ProductIdVisibleToCaller(pi, isPartnerCaller, callerPartnerNames))
                .ToList();

            // Find GTIN from product identifiers (UPC, GTIN, EAN)
            var gtinTypes = new[] { "UPC", "GTIN", "EAN" };
            var gtinId = productIds.FirstOrDefault(pi => gtinTypes.Contains(pi.ProductIdType, StringComparer.OrdinalIgnoreCase));
            var gtin = gtinId?.ProductIdValue;

            // Pad GTIN to 14 digits if found
            if (!string.IsNullOrEmpty(gtin))
            {
                gtin = gtin.PadLeft(14, '0');
            }

            // Collect additional trade item IDs (ASIN, SKU, MPN, ISBN, etc.)
            var additionalIdTypes = new[] { "ASIN", "SKU", "MPN", "ISBN" };
            var additionalIds = productIds
                .Where(pi => additionalIdTypes.Contains(pi.ProductIdType, StringComparer.OrdinalIgnoreCase))
                .Select(pi => new GS1AdditionalTradeItemId
                {
                    Type = pi.ProductIdType,
                    Value = pi.ProductIdValue,
                    Name = pi.ProductName
                })
                .ToList();

            // Map status to GS1 format (ACTIVE or INACTIVE)
            // Approved/live statuses map to ACTIVE; everything else (Draft, Hold, Expired) maps to INACTIVE
            var activeStatuses = new[] { "Active", "Client Approved - Paid", "Client Approved - Confidential", "Client Approved - Not Paid", "Republished" };
            var gs1Status = activeStatuses.Any(s => s.Equals(cert.Status, StringComparison.OrdinalIgnoreCase)) ? "ACTIVE" : "INACTIVE";

            // Format dates as YYYY-MM-DD
            var issueDate = cert.EffectiveDate != default ? cert.EffectiveDate.ToString("yyyy-MM-dd") : null;
            var endDate = cert.ExpirationDate != default ? cert.ExpirationDate.ToString("yyyy-MM-dd") : null;

            // Build GS1 Digital Link URI for product
            var productUri = !string.IsNullOrEmpty(gtin)
                ? $"https://id.gs1.org/01/{gtin}"
                : $"{baseUrl}/api/v1/certificates/gs1/{cert.Number}/product";

            // Build certification URI
            var certUri = $"{baseUrl}/api/v1/certificates/gs1/{cert.Number}";

            return new GS1CertificationReport
            {
                Id = certUri,
                IssueDate = issueDate,
                Issuer = "GreenCircle Certified",
                Products = new List<GS1Product>
                {
                    new GS1Product
                    {
                        Id = productUri,
                        Gtin = gtin,
                        Name = cert.Product?.Name,
                        AdditionalTradeItemId = additionalIds.Any() ? additionalIds : null,
                        CertificationInfo = new GS1CertificationInfo
                        {
                            Agency = "GreenCircle Certified",
                            AgencyURL = "https://www.greencirclecertified.com",
                            Program = cert.CertificationType?.Name,
                            CertificateId = cert.Number,
                            Status = gs1Status,
                            Value = cert.CertificationResults,
                            StartDate = issueDate,
                            EndDate = endDate,
                            // Amazon Developer Guide contract: targetCountries is always "All" for partner responses
                            TargetCountries = new List<string> { "All" },
                            AmazonCode = cert.CertificationType?.AmazonCode,
                            Pathway = cert.Pathway?.Name
                        }
                    }
                }
            };
        }

        // DEF-103: shared partner-cert DTO so single-cert endpoints (by-id, by-number)
        // return the same contract shape as the list endpoint. Status hardcoded to "Active",
        // certificationCountries hardcoded to "All" per Amazon Developer Guide §4 / §8.
        // No internal IDs leaked; productIds[] uses the array shape, not the legacy flat asin/upc/gtin/ean.
        private object BuildPartnerCertificateDto(Certificate c, List<string> callerPartnerNames)
        {
            // CR-171 isolation: this DTO is partner-facing. Emit ONLY the caller's own partner
            // association(s) — never co-partners. c.Partners must be eager-loaded by the caller.
            var ownPartners = (c.Partners ?? new List<Partner>())
                .Where(p => callerPartnerNames.Contains(p.Name))
                .Select(p => p.Name)
                .ToList();
            return new
            {
                certificateNumber = c.Number,
                status = "Active",
                issueDate = c.EffectiveDate,
                expirationDate = c.ExpirationDate,
                certificationCountries = "All",
                partner = string.Join(", ", ownPartners),
                certificationResults = c.CertificationResults,
                pathway = c.Pathway?.Name,
                company = c.Company?.Name,
                productName = c.Product?.Name,
                certificationType = c.CertificationType?.Name,
                amazonCode = c.CertificationType?.AmazonCode,
                issuer = "GreenCircle Certified",
                productIds = (c.ProductIds ?? new List<CertificateProductId>())
                    .Where(pi => pi.Status == "Active" && ProductIdVisibleToCaller(pi, true, callerPartnerNames))
                    .Select(pi => new
                    {
                        type = pi.ProductIdType,
                        value = pi.ProductIdValue,
                        status = pi.Status,
                        // CR-171 Model A: caller-scoped label (own tag(s); blank for untagged shared IDs).
                        partner = ScopedProductIdPartner(pi, true, callerPartnerNames),
                        productName = pi.ProductName,
                        productSku = pi.ProductSku
                    })
                    .ToList(),
                pdfDownloadUrl = $"/api/v1/certificates/{c.Number}/download"
            };
        }

        // CR-171 Model A — a product ID is visible to a partner caller when it is UNTAGGED
        // (no partner tags → shared with all the cert's partners) OR tagged to that caller.
        // Non-partner callers see every product ID. Requires pi.Partners eager-loaded.
        private static bool ProductIdVisibleToCaller(CertificateProductId pi, bool isPartnerCaller, List<string> callerPartnerNames)
            => !isPartnerCaller
               || pi.Partners == null || pi.Partners.Count == 0
               || pi.Partners.Any(p => callerPartnerNames.Contains(p.Name));

        // Partner-facing label for a product ID: the caller's own matching tag(s), or blank for an
        // untagged shared ID. Non-partner callers see the denormalized primary.
        private static string? ScopedProductIdPartner(CertificateProductId pi, bool isPartnerCaller, List<string> callerPartnerNames)
            => isPartnerCaller
               ? string.Join(", ", (pi.Partners ?? new List<Partner>()).Where(p => callerPartnerNames.Contains(p.Name)).Select(p => p.Name))
               : pi.Partner;
    }
}
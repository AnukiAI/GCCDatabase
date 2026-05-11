using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services;
using System.Text.RegularExpressions;

namespace GCCWebApp.Controllers.Api
{
    [Route("api/certificate-tiles")]
    [ApiController]
    public class CertificateTilesApiController : ControllerBase
    {
        private readonly ILogger<CertificateTilesApiController> _logger;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICertificatePdfService _pdfService;
        private readonly ApplicationDbContext _context;
        private readonly bool _showDetailedErrors;

        public CertificateTilesApiController(
            ILogger<CertificateTilesApiController> logger,
            ICertificateRepository certificateRepository,
            IUnitOfWork unitOfWork,
            ICertificatePdfService pdfService,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _logger = logger;
            _certificateRepository = certificateRepository;
            _unitOfWork = unitOfWork;
            _pdfService = pdfService;
            _context = context;
            _showDetailedErrors = configuration.GetValue<bool>("FeatureFlags:ShowDetailedApiErrors", false);
        }

        /// <summary>
        /// Format certificate number to YY-NNNN format
        /// </summary>
        private static string FormatCertificateNumber(string? number, DateTime? effectiveDate)
        {
            if (string.IsNullOrEmpty(number)) return "N/A";

            // If already in correct format (XX-XXXX), return as is
            if (Regex.IsMatch(number, @"^\d{2}-\d{4}$"))
                return number;

            // Get year from effective date or current year
            var year = effectiveDate?.Year.ToString().Substring(2)
                       ?? DateTime.Now.Year.ToString().Substring(2);

            // If certificate number is just digits, format as YY-####
            if (Regex.IsMatch(number, @"^\d+$"))
            {
                var paddedNumber = number.PadLeft(4, '0');
                return $"{year}-{paddedNumber}";
            }

            return number; // Return as-is if we can't format it
        }

        /// <summary>
        /// Get certificates for tile view
        /// GET: /api/certificate-tiles
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCertificates(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? search = null,
            [FromQuery] string? certType = null,
            [FromQuery] string? productCategory = null,
            [FromQuery] string? company = null,
            [FromQuery] string? country = null,
            [FromQuery] string? tags = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 20,
            [FromQuery] string? certTypes = null,
            [FromQuery] string? productCat = null,
            [FromQuery] string? countries = null,
            [FromQuery] string? certTags = null,
            [FromQuery] string? additional = null)
        {
            try
            {
                var finalSort = sortBy ?? sort ?? "cert-number-newest";
                var finalSearchTerm = searchTerm ?? search;

                // Support both page-based and offset-based pagination for backwards compatibility
                var actualOffset = offset > 0 ? offset : (page - 1) * pageSize;
                var actualLimit = limit > 0 ? limit : pageSize;

                var filter = new CertificateSearchFilter
                {
                    SearchTerm = finalSearchTerm,
                    Statuses = CertificateStatusHelper.PublicStatuses.ToList(),
                    ExcludeExpiredByDate = true, // Only show non-expired certs on public view
                    SortBy = MapSortBy(finalSort),
                    SortDescending = finalSort == "cert-number-newest" || finalSort == "company-za" || finalSort == "product-za" || finalSort == "cert-type-za" || finalSort == "location-za" || finalSort == "country-za",
                    Page = Math.Max(1, (actualOffset / actualLimit) + 1),
                    PageSize = actualLimit
                };

                // Parse filter arrays - handle multiple parameter names
                var certTypeFilter = certType ?? certTypes;
                if (!string.IsNullOrEmpty(certTypeFilter))
                {
                    filter.CertificationTypeNames = certTypeFilter.Split('|').ToList();
                }

                var countryFilter = country ?? countries;
                if (!string.IsNullOrEmpty(countryFilter))
                {
                    filter.Countries = countryFilter.Split('|').ToList();
                }

                var companyFilter = company;
                if (!string.IsNullOrEmpty(companyFilter))
                {
                    // Convert company IDs to filter
                    var companyIds = companyFilter.Split('|')
                        .Where(id => int.TryParse(id, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (companyIds.Any())
                    {
                        filter.CompanyIds = companyIds;
                    }
                    else
                    {
                        // If not numeric IDs, treat as company names
                        filter.CompanyNames = companyFilter.Split('|').ToList();
                    }
                }

                var productCategoryFilter = productCategory ?? productCat;
                if (!string.IsNullOrEmpty(productCategoryFilter))
                {
                    filter.ProductCategories = productCategoryFilter.Split('|').ToList();
                }

                var tagsFilter = tags ?? certTags;
                if (!string.IsNullOrEmpty(tagsFilter))
                {
                    // PUBLIC API: Only allow filtering by external tags
                    var requestedTags = tagsFilter.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                    var validExternalTags = await _context.Tags
                        .Where(t => requestedTags.Contains(t.Name) && t.TagType == "external" && t.IsActive)
                        .Select(t => t.Name)
                        .ToListAsync();

                    // If tags were requested but none are valid external tags, use a non-existent tag
                    // to ensure no results are returned (security: don't allow internal tag filtering)
                    if (requestedTags.Any() && !validExternalTags.Any())
                    {
                        filter.Tags = new List<string> { "__INVALID_TAG_FILTER__" };
                    }
                    else
                    {
                        filter.Tags = validExternalTags;
                    }
                }

                if (!string.IsNullOrEmpty(additional))
                {
                    filter.AdditionalFilters = additional.Split('|').ToList();
                }

                var (certificates, totalCount) = await _certificateRepository.GetAdvancedFilteredCertificatesAsync(filter);

                var businessToday = CertificateStatusHelper.BusinessToday;
                var result = certificates.Select(c => new
                {
                    id = c.Id,
                    certificateNumber = FormatCertificateNumber(c.Number, c.EffectiveDate),
                    certificationType = c.CertificationType?.Name ?? "Unknown",
                    companyName = c.Company?.Name ?? "Unknown",
                    productName = c.Product?.Name ?? "Unknown",
                    productCategory = c.ProductCategory != null ? c.ProductCategory.Name : (c.Product?.Category?.Name ?? ""),
                    productType = c.ProductType != null ? c.ProductType.Name : (c.Product?.ProductType != null ? c.Product.ProductType.Name : ""),
                    productSubcategory = c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product?.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : ""),
                    effectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                    expirationDate = c.ExpirationDate.ToString("yyyy-MM-dd"),
                    location = c.Location,
                    country = c.Country,
                    status = c.Status,
                    isExpired = c.ExpirationDate != null && c.ExpirationDate < businessToday,
                    isExpiringSoon = c.ExpirationDate != null && c.ExpirationDate <= businessToday.AddDays(30) && c.ExpirationDate >= businessToday,
                    externalTags = GetCertificateTags(c)
                });

                var response = new
                {
                    success = true,
                    certificates = result,
                    totalCount,
                    loadedCount = actualOffset + result.Count(),
                    hasMore = actualOffset + result.Count() < totalCount,
                    message = (string?)null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates for tile view");

                if (_showDetailedErrors)
                {
                    return BadRequest(new {
                        success = false,
                        message = "Failed to retrieve certificates",
                        error = ex.Message,
                        innerError = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace,
                        certificates = new List<object>(),
                        totalCount = 0
                    });
                }

                return BadRequest(new {
                    success = false,
                    message = "Failed to retrieve certificates",
                    certificates = new List<object>(),
                    totalCount = 0
                });
            }
        }

        /// <summary>
        /// Get filter options for certificate search
        /// GET: /api/certificate-tiles/filters
        /// </summary>
        [HttpGet("filters")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                // Base query for active certificates (public view shows only active/expiring/renewal certificates)
                var businessToday = CertificateStatusHelper.BusinessToday;
                var baseQuery = _context.Certificates
                    .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= businessToday));

                // Strict scoping: only surface filter options that have at least one cert in baseQuery.
                // Picking any dropdown option must always return >= 1 result (no dead clicks).
                var visibleCertTypeIds = await baseQuery
                    .Select(c => c.CertificationTypeId)
                    .Distinct()
                    .ToListAsync();

                var visibleCategoryIds = await baseQuery
                    .Select(c => c.Product.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var visibleProductTypeIds = await baseQuery
                    .Where(c => c.Product.ProductTypeId != null)
                    .Select(c => c.Product.ProductTypeId!.Value)
                    .Distinct()
                    .ToListAsync();

                var visibleSubcategoryIds = await baseQuery
                    .Where(c => c.Product.ProductSubcategoryId != null)
                    .Select(c => c.Product.ProductSubcategoryId!.Value)
                    .Distinct()
                    .ToListAsync();

                // DEF-167: Filter dropdown shows Level-1 (top-level) cert types only.
                // Subcategories should not appear as filter options; certs tagged at Level-2
                // must be repointed at their Level-1 parent via
                // Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql before they will be
                // reachable through this dropdown.
                var hierarchicalCertTypes = await _context.CertificationTypes
                    .Where(ct => ct.IsActive && ct.ParentId == null && visibleCertTypeIds.Contains(ct.Id))
                    .OrderBy(ct => ct.SortOrder)
                    .Select(ct => new { name = ct.Name, level = ct.Level })
                    .ToListAsync();

                // Build hierarchy matching Admin page: Category → Type (ParentId=null) → Subcategory (ParentId!=null)
                var categories = await _context.Set<ProductCategory>()
                    .Where(pc => pc.IsActive)
                    .OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name)
                    .ToListAsync();

                var allTypes = await _context.Set<ProductType>()
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.SortOrder).ThenBy(pt => pt.Name)
                    .ToListAsync();

                var typesByCategory = allTypes.Where(pt => pt.ParentId == null)
                    .GroupBy(pt => pt.CategoryId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var childrenByParent = allTypes.Where(pt => pt.ParentId != null)
                    .GroupBy(pt => pt.ParentId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Trim hierarchy:
                //   - Category: kept ONLY if a public cert's Product.CategoryId points at it.
                //     (DEF-168: the previous `|| c.types.Any()` fallback leaked orphan parent
                //     categories — e.g. a category that admins assigned a Product Type under,
                //     but no Product directly lives in. Find side at
                //     CertificateRepository.GetAdvancedFilteredCertificatesAsync doesn't
                //     traverse Product.ProductType.Category, so the click was a dead-click.)
                //   - Type: kept if directly used OR has a visible subcategory child.
                //     Safe because the find side has parent-cascade on ProductType.Parent.Name.
                var productCategoriesHierarchy = categories
                    .Select(pc => new
                    {
                        id = pc.Id,
                        name = pc.Name,
                        level = 1,
                        types = (typesByCategory.TryGetValue(pc.Id, out var types) ? types : new List<ProductType>())
                            .Select(pt => new
                            {
                                id = pt.Id,
                                name = pt.Name,
                                level = 2,
                                subcategories = (childrenByParent.TryGetValue(pt.Id, out var children) ? children : new List<ProductType>())
                                    .Where(sub => visibleSubcategoryIds.Contains(sub.Id))
                                    .Select(sub => new
                                    {
                                        id = sub.Id,
                                        name = sub.Name,
                                        level = 3
                                    }).ToList()
                            })
                            .Where(t => visibleProductTypeIds.Contains(t.id) || t.subcategories.Any())
                            .ToList()
                    })
                    .Where(c => visibleCategoryIds.Contains(c.id))
                    .ToList();

                var filters = new
                {
                    certificationTypes = hierarchicalCertTypes,

                    companies = await baseQuery
                        .Select(c => new { id = c.Company.Id, name = c.Company.Name })
                        .Distinct()
                        .OrderBy(c => c.name)
                        .ToListAsync(),

                    countries = (await baseQuery
                        .Select(c => c.Country)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .ToListAsync())
                        .OrderBy(c => c == "United States" ? 0 : 1)
                        .ThenBy(c => c)
                        .ToList(),

                    // Hierarchical product categories (Level 1 -> Level 2 -> Level 3)
                    productCategoriesHierarchy,

                    // Keep flat list for backward compatibility
                    productCategories = await baseQuery
                        .Select(c => c.Product.Category.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync(),

                    // Only external tags (APR, LEED, etc.) for the Additional filter
                    additionalTags = await baseQuery
                        .SelectMany(c => c.Tags)
                        .Where(t => t.IsActive && t.TagType == "external")
                        .Select(t => t.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync()
                };

                return Ok(filters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filter options");
                return BadRequest(new { error = "Failed to retrieve filter options" });
            }
        }

        /// <summary>
        /// Get autocomplete suggestions
        /// GET: /api/certificate-tiles/autocomplete
        /// </summary>
        [HttpGet("autocomplete")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAutocompleteSuggestions([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Ok(new { suggestions = new List<object>() });
                }

                var suggestions = new List<object>();

                // Base query: only public-visible certificates (same filter as tile view)
                var businessToday = CertificateStatusHelper.BusinessToday;
                var baseQuery = _context.Certificates
                    .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= businessToday));

                // Search companies that have at least one public certificate
                var companies = await baseQuery
                    .Where(c => c.Company != null && c.Company.Name.Contains(term))
                    .Select(c => c.Company!.Name)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                suggestions.AddRange(companies.Select(name => new
                {
                    value = name,
                    label = name,
                    type = ""
                }));

                // Search products that have at least one public certificate
                var products = await baseQuery
                    .Where(c => c.Product != null && c.Product.Name.Contains(term))
                    .Select(c => c.Product!.Name)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                suggestions.AddRange(products.Select(name => new
                {
                    value = name,
                    label = name,
                    type = ""
                }));

                // Search certificate numbers
                var certificates = await baseQuery
                    .Where(c => c.Number.Contains(term))
                    .Take(5)
                    .Select(c => new { c.Number, CompanyName = c.Company != null ? c.Company.Name : "Unknown" })
                    .ToListAsync();

                suggestions.AddRange(certificates.Select(c => new
                {
                    value = c.Number,
                    label = $"{c.Number} - {c.CompanyName}",
                    type = ""
                }));

                // Search certification types that have at least one public certificate
                var certTypes = await baseQuery
                    .Where(c => c.CertificationType != null && c.CertificationType.Name.Contains(term))
                    .Select(c => c.CertificationType!.Name)
                    .Distinct()
                    .Take(3)
                    .ToListAsync();

                suggestions.AddRange(certTypes.Select(name => new
                {
                    value = name,
                    label = name,
                    type = ""
                }));

                var response = new
                {
                    suggestions = suggestions.Take(15).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving autocomplete suggestions for term: {Term}", term);

                if (_showDetailedErrors)
                {
                    return BadRequest(new {
                        error = "Failed to retrieve suggestions",
                        message = ex.Message,
                        innerError = ex.InnerException?.Message,
                        stackTrace = ex.StackTrace
                    });
                }

                return BadRequest(new { error = "Failed to retrieve suggestions" });
            }
        }

        /// <summary>
        /// Download certificate PDF
        /// GET: /api/certificate-tiles/{id}/download
        /// </summary>
        [HttpGet("{id}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadCertificate(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { error = "Certificate not found" });
                }

                // Per business rule (2026-04-25): never auto-generate a placeholder PDF.
                // Analysts must upload a real certificate file at creation time.
                if (string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    return NotFound(new { error = "Certificate PDF not available" });
                }

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", certificate.CertificateFile.TrimStart('/'));
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound(new { error = "Certificate PDF file not found on server" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var fileName = $"Certificate_{certificate.Number}.pdf";
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate PDF for ID: {Id}", id);
                return BadRequest(new { error = "Failed to download certificate" });
            }
        }

        /// <summary>
        /// Generate shareable link for current search
        /// POST: /api/certificate-tiles/share-link
        /// </summary>
        [HttpPost("share-link")]
        [AllowAnonymous]
        public IActionResult GenerateShareableLink([FromBody] ShareableLinkRequest request)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(request.Search))
                    queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");
                
                if (!string.IsNullOrEmpty(request.CertType))
                    queryParams.Add($"certType={Uri.EscapeDataString(request.CertType)}");
                
                if (!string.IsNullOrEmpty(request.ProductCategory))
                    queryParams.Add($"category={Uri.EscapeDataString(request.ProductCategory)}");
                
                if (!string.IsNullOrEmpty(request.Company))
                    queryParams.Add($"company={Uri.EscapeDataString(request.Company)}");
                
                if (!string.IsNullOrEmpty(request.Country))
                    queryParams.Add($"country={Uri.EscapeDataString(request.Country)}");
                
                if (!string.IsNullOrEmpty(request.SortBy) && request.SortBy != "cert-number-newest")
                    queryParams.Add($"sort={Uri.EscapeDataString(request.SortBy)}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var baseUrl = $"{Request.Scheme}://{Request.Host}/Certificate/CertificateTileView";
                var shareableUrl = $"{baseUrl}{queryString}";

                return Ok(new { url = shareableUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating shareable link");
                return BadRequest(new { error = "Failed to generate shareable link" });
            }
        }

        #region Helper Methods

        private string MapSortBy(string sortBy)
        {
            return sortBy switch
            {
                "newest" => "EffectiveDate",
                "oldest-effective" => "EffectiveDate",
                "expiring" => "ExpirationDate",
                "company-az" => "Company",
                "company-za" => "Company",
                "product-az" => "Product",
                "product-za" => "Product",
                "cert-number-newest" => "Number",
                "cert-number-oldest" => "Number",
                "cert-type" => "CertType",
                "cert-type-za" => "CertType",
                "location-az" => "Location",
                "location-za" => "Location",
                "country" => "Country",
                "country-za" => "Country",
                "product-type" => "ProductType",
                _ => "Number"
            };
        }

        private string? GetCertificateTags(Certificate certificate)
        {
            var tags = new List<string>();

            // Include external tags (public-safe)
            if (certificate.Tags != null)
            {
                tags.AddRange(certificate.Tags
                    .Where(t => t.TagType == "external")
                    .Select(t => t.Name));
            }

            return tags.Any() ? string.Join(", ", tags) : null;
        }

        #endregion
    }

    public class ShareableLinkRequest
    {
        public string? Search { get; set; }
        public string? CertType { get; set; }
        public string? ProductCategory { get; set; }
        public string? Company { get; set; }
        public string? Country { get; set; }
        public string? Tags { get; set; }
        public string? SortBy { get; set; }
    }
}
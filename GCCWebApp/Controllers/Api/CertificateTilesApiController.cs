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
            // DEF-249: default 0 (not 20) so `limit` is a real "not supplied" sentinel — otherwise
            // `actualLimit = limit > 0 ? limit : pageSize` always chose limit and `pageSize` was
            // ignored on the page/pageSize path (every page-based call capped at 20). Same fix as the
            // internal tiles controller. UI sends page+pageSize=20 so its behavior is unchanged.
            [FromQuery] int limit = 0,
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
                // DEF-249: clamp to [1, 200]. Lower bound prevents divide-by-zero in the Page math
                // (line below) when pageSize=0 (now reachable since `limit` defaults to 0). Upper
                // bound is deliberately tighter than the internal admin endpoint because THIS path is
                // PUBLIC/unauthenticated — without it, fixing the pageSize-ignored bug would let an
                // anonymous caller request an unbounded page (the accidental old 20-cap hid this).
                var actualLimit = Math.Clamp(limit > 0 ? limit : pageSize, 1, 200);

                var filter = new CertificateSearchFilter
                {
                    SearchTerm = finalSearchTerm,
                    Statuses = CertificateStatusHelper.PublicStatuses.ToList(),
                    ExcludeExpiredByDate = true, // Only show non-expired certs on public view
                    ExcludeNotYetEffectiveByDate = true, // DEF-358: and not certs staged ahead of their effective date
                    SortBy = MapSortBy(finalSort),
                    SortDescending = finalSort == "cert-number-newest" || finalSort == "company-za" || finalSort == "product-za" || finalSort == "cert-type-za" || finalSort == "location-za" || finalSort == "country-za",
                    // DEF-381: Page is kept only so the value stays meaningful to anything that
                    // reads it; Skip is what actually drives the query. Deriving the offset from
                    // Page here lost the remainder — offset=90/limit=50 became Skip(50), which
                    // re-served rows already shown and made the tail of the list unreachable.
                    Page = Math.Max(1, (actualOffset / actualLimit) + 1),
                    PageSize = actualLimit,
                    Skip = actualOffset
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
                // Base query for active certificates (public view shows only active/expiring/renewal certificates).
                // DEF-358: WherePubliclyLive enforces the FULL window (status + effective + expiration) so the
                // sidebar's "every option returns >= 1 result" guarantee still holds against the listing query.
                var baseQuery = _context.Certificates.WherePubliclyLive();

                // Strict scoping: only surface filter options that have at least one cert in baseQuery.
                // Picking any dropdown option must always return >= 1 result (no dead clicks).
                var visibleCertTypeIds = await baseQuery
                    .Select(c => c.CertificationTypeId)
                    .Distinct()
                    .ToListAsync();

                // DEF-199 (QC #178): use the EFFECTIVE category/type/subcategory at each level
                // (cert-level override if set, otherwise Product-derived) — mirrors the listing
                // predicate at CertificateRepository.cs:339-354 / DEF-192. The previous Product-only
                // projection missed any cert whose category/type/subcategory was set via the
                // cert-level FK override columns and never showed those nodes in the sidebar.
                var visibleCategoryIds = await baseQuery
                    .Select(c => c.ProductCategoryId ?? c.Product.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var visibleProductTypeIds = (await baseQuery
                    .Select(c => c.ProductTypeId ?? c.Product.ProductTypeId)
                    .Where(id => id != null)
                    .Distinct()
                    .ToListAsync())
                    .Select(id => id!.Value)
                    .ToList();

                var visibleSubcategoryIds = (await baseQuery
                    .Select(c => c.ProductSubcategoryId ?? c.Product.ProductSubcategoryId)
                    .Where(id => id != null)
                    .Distinct()
                    .ToListAsync())
                    .Select(id => id!.Value)
                    .ToList();

                // DEF-167: Filter dropdown shows Level-1 (top-level) cert types only.
                // Subcategories should not appear as filter options; certs tagged at Level-2
                // must be repointed at their Level-1 parent via
                // Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql before they will be
                // reachable through this dropdown.
                // QC #124: sort alphabetically by Name. SortOrder column is not manageable from
                // the admin Cert Types page, so its values drift from alpha order over time —
                // newly-added types end up at the top or in arbitrary positions. Alpha sort is
                // predictable for the client and means new admin-added types slot into the right
                // place automatically without any sort-order tuning.
                var hierarchicalCertTypes = await _context.CertificationTypes
                    .Where(ct => ct.IsActive && ct.ParentId == null && visibleCertTypeIds.Contains(ct.Id))
                    .OrderBy(ct => ct.Name)
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

                    // DEF-199 (QC #177): also exclude soft-deleted / inactive companies. A cert
                    // can still reference a Company that was later soft-deleted; the navigation
                    // resolves successfully so the company surfaced in the sidebar even though
                    // the management UI hid it — clicking it appeared to "find no certs" if the
                    // user assumed an active company.
                    companies = await baseQuery
                        .Where(c => c.Company != null && c.Company.IsActive && !c.Company.IsDeleted)
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

                    // DEF-199 (QC #178): flat list resolved from the same visibleCategoryIds set
                    // so cert-level overrides surface here too. Previous projection
                    // (baseQuery.Select(c => c.Product.Category.Name)) ignored overrides.
                    productCategories = await _context.Set<ProductCategory>()
                        .Where(pc => pc.IsActive && visibleCategoryIds.Contains(pc.Id))
                        .OrderBy(pc => pc.Name)
                        .Select(pc => pc.Name)
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

                // Base query: only public-visible certificates (same filter as tile view).
                // DEF-358: autocomplete must not leak the company/product name of a cert that is not
                // yet effective — the suggestion would return zero rows when clicked.
                var baseQuery = _context.Certificates.WherePubliclyLive();

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

        // DEF-362: GET /api/certificate-tiles/{id}/download was REMOVED here, deliberately.
        //
        // It was [AllowAnonymous] and streamed a certificate PDF after nothing more than
        // GetByIdAsync(id) — no status check, no validity-window check, no partner scoping. Any
        // certificate was addressable by integer id, including Client Approved - Confidential,
        // Expired, Draft, and (post-DEF-358) not-yet-effective ones.
        //
        // It leaked nothing only because it was ALSO broken: it built its path with
        // Path.Combine(GetCurrentDirectory(), "wwwroot", ...) — the last hardcoded physical path in
        // any controller — and so bypassed IUploadPathResolver, the single logical→physical mapping
        // DEF-286 established. Uploads moved to FileStorage:StoragePath on 2026-08-13, so it 404'd
        // for all 5,474 migrated PDFs. The bug was the only thing standing between an anonymous
        // caller and every confidential certificate on the system.
        //
        // That is why it is deleted rather than repaired: "fix the 404 by pointing it at the
        // resolver" is the obvious next edit for anyone who finds it, and that single change
        // publishes the whole file store. Nothing called it — the public tile view downloads via
        // /Certificate/Download/{number} and the external service via
        // /api/CertificateTileExternal/{id}/download, both of which gate visibility correctly
        // (verified anonymously against publicly-live / not-yet-effective / confidential / expired
        // certificates, with the files confirmed present on disk).
        //
        // If a tiles-scoped download is ever genuinely needed, gate it with CertificateVisibility
        // AND resolve the path through IUploadPathResolver. Do not restore this method.

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
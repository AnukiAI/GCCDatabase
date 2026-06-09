using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Extensions;
using GCCWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/CertificateTileExternal")]
    [Authorize(Policy = "ExternalAccess")] // Requires External role authentication
    public class CertificateTileExternalApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificateTileExternalApiController> _logger;

        public CertificateTileExternalApiController(
            ApplicationDbContext context,
            ILogger<CertificateTileExternalApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Normalizes a string by removing hyphens, spaces, and special characters for flexible matching.
        /// E.g., "Len-Tex" -> "lentex", "3-Form" -> "3form"
        /// </summary>
        private static string NormalizeForSearch(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input.ToLower(), @"[-\s_]", "");
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
        /// Get certificates for external partner tile view with lazy loading
        /// Only shows certificates relevant to the partner
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCertificateTiles(
            [FromQuery] string? search = null,
            [FromQuery] string? certType = null,
            [FromQuery] string? productCategory = null,
            [FromQuery] string? company = null,
            [FromQuery] string? country = null,
            [FromQuery] string? tags = null,
            [FromQuery] string? partner = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Perf: AsSplitQuery() prevents cartesian on the Tags collection include while
                // keeping all the reference Includes the .Select() projection benefits from. See
                // CertificateTileInternalApiController for full rationale (DEF-166 follow-up).
                var query = _context.Certificates
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.Product.ProductType)
                    .Include(c => c.ProductCategory)
                    .Include(c => c.ProductType)
                    .Include(c => c.ProductSubcategory)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .AsQueryable();

                // Apply role-based filtering (per Design Recommendations - NO OR logic)
                // Partner role: sees certificates tagged with their partner
                // External role: sees certificates from their assigned companies only
                // Admin/Analyst: sees all (handled by policy, but include for completeness)
                var userRole = GetCurrentUserRole();

                if (userRole == "Partner")
                {
                    // Partner users see certificates tagged with their partner
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    if (userPartnerNames.Any())
                    {
                        query = query.Where(c => userPartnerNames.Contains(c.Partner ?? "None"));
                    }
                    else
                    {
                        // Partner user with no partner association - show nothing
                        query = query.Where(c => false);
                    }
                }
                else if (userRole == "External")
                {
                    // External users see ONLY certificates from their assigned companies
                    var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                    if (userCompanyIds.Any())
                    {
                        query = query.Where(c => userCompanyIds.Contains(c.CompanyId));
                    }
                    else
                    {
                        // External user with no company association - show nothing
                        query = query.Where(c => false);
                    }
                }
                // Admin/Analyst see all (no filtering needed - they have full access)

                // Apply additional partner filter if specified (for Partner role users to narrow results)
                if (!string.IsNullOrWhiteSpace(partner) && userRole == "Partner")
                {
                    var partners = partner.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => partners.Contains(c.Partner ?? "None"));
                }

                // Apply search filter with flexible matching (handles hyphens, spaces, capitalization)
                // E.g., "LenTex" matches "Len-Tex", "lentex" matches "Len-Tex", etc.
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    var normalizedSearch = NormalizeForSearch(search);

                    query = query.Where(c =>
                        // Standard search
                        c.Number.ToLower().Contains(searchLower) ||
                        c.CertificationType.Name.ToLower().Contains(searchLower) ||
                        c.Company.Name.ToLower().Contains(searchLower) ||
                        c.Product.Name.ToLower().Contains(searchLower) ||
                        c.Product.Category.Name.ToLower().Contains(searchLower) ||
                        (c.Location != null && c.Location.ToLower().Contains(searchLower)) ||
                        (c.Country != null && c.Country.ToLower().Contains(searchLower)) ||
                        c.Tags.Any(t => t.Name.ToLower().Contains(searchLower)) ||
                        // Flexible matching: compare normalized values (without hyphens/spaces)
                        c.Company.Name.ToLower().Replace("-", "").Replace(" ", "").Contains(normalizedSearch) ||
                        c.Product.Name.ToLower().Replace("-", "").Replace(" ", "").Contains(normalizedSearch) ||
                        (c.Location != null && c.Location.ToLower().Replace("-", "").Replace(" ", "").Contains(normalizedSearch)));
                }

                // Apply certification type filter
                if (!string.IsNullOrWhiteSpace(certType))
                {
                    var types = certType.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => types.Contains(c.CertificationType.Name));
                }

                // DEF-192: Match the EFFECTIVE displayed category/type/subcategory at each level
                // (cert-level override if set, otherwise Product-derived). Previous implementation
                // ORed both sides and could match on a Product-derived value the cert's override
                // hid from display. Display projection at line 240 below; this predicate must
                // mirror it.
                if (!string.IsNullOrWhiteSpace(productCategory))
                {
                    var categories = productCategory.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c =>
                        // Effective Level-1 category
                        (c.ProductCategory != null && categories.Contains(c.ProductCategory.Name)) ||
                        (c.ProductCategory == null && categories.Contains(c.Product.Category.Name)) ||
                        // Effective Level-2 ProductType
                        (c.ProductType != null && categories.Contains(c.ProductType.Name)) ||
                        (c.ProductType == null && c.Product.ProductType != null && categories.Contains(c.Product.ProductType.Name)) ||
                        // Effective Level-3 subcategory
                        (c.ProductSubcategory != null && categories.Contains(c.ProductSubcategory.Name)) ||
                        (c.ProductSubcategory == null && c.Product.ProductSubcategory != null && categories.Contains(c.Product.ProductSubcategory.Name))
                    );
                }

                // Apply company filter — accept numeric IDs OR company names (parity with Public per QC #127)
                if (!string.IsNullOrWhiteSpace(company))
                {
                    var companyTokens = company.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    var companyIds = companyTokens.Where(t => int.TryParse(t, out _)).Select(int.Parse).ToList();
                    if (companyIds.Any())
                    {
                        query = query.Where(c => companyIds.Contains(c.Company.Id));
                    }
                    else
                    {
                        query = query.Where(c => companyTokens.Contains(c.Company.Name));
                    }
                }

                // Apply country filter
                if (!string.IsNullOrWhiteSpace(country))
                {
                    var countries = country.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => countries.Contains(c.Country));
                }

                // Apply tags filter - EXTERNAL TAGS ONLY for external users
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    var tagsList = tags.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => c.Tags.Any(t => tagsList.Contains(t.Name) && t.TagType == "external"));
                }

                // Filter to only show active/valid certificates for external partners
                query = query.Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday));

                // Apply sorting
                query = ApplySorting(query, sortBy);

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination - first fetch raw data
                var rawCertificates = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.Id,
                        RawNumber = c.Number,
                        CertificationType = c.CertificationType.Name,
                        c.EffectiveDate,
                        c.ExpirationDate,
                        CompanyName = c.Company.Name,
                        CompanyEmail = c.Company.Email,
                        CompanyUrl = c.Company.Website,
                        ProductName = c.Product.Name,
                        ProductCategory = c.ProductCategory != null ? c.ProductCategory.Name : c.Product.Category.Name,
                        ProductType = c.ProductType != null ? c.ProductType.Name : (c.Product.ProductType != null ? c.Product.ProductType.Name : ""),
                        ProductSubcategory = c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : ""),
                        c.Location,
                        c.Country,
                        ExternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "external").Select(t => t.Name)),
                        PdfPath = c.CertificateFile,
                        c.Partner,
                        // Status: only PublicStatuses reach this projection (filter at line ~204),
                        // so this is always one of "Client Approved - Paid" / "Republished".
                        // Surfaced in the UI table so the Status column actually has a value.
                        c.Status
                    })
                    .ToListAsync();

                // Format certificate numbers to YY-NNNN format
                var certificates = rawCertificates.Select(c => new
                {
                    c.Id,
                    CertificateNumber = FormatCertificateNumber(c.RawNumber, c.EffectiveDate),
                    c.CertificationType,
                    // Emit as yyyy-MM-dd so the JS parseLocalDate hits the date-only branch.
                    // Raw DateTime would go through UtcDateTimeConverter, which appends Z and shifts the
                    // calendar date back a day in negative-UTC zones (DEF: tile period off-by-one).
                    EffectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                    ExpirationDate = c.ExpirationDate.ToString("yyyy-MM-dd"),
                    c.CompanyName,
                    c.CompanyEmail,
                    c.CompanyUrl,
                    c.ProductName,
                    c.ProductCategory,
                    c.ProductType,
                    c.ProductSubcategory,
                    c.Location,
                    c.Country,
                    c.ExternalTags,
                    c.PdfPath,
                    c.Partner,
                    c.Status
                }).ToList();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    hasMore = page * pageSize < totalCount,
                    certificates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting external certificate tiles");
                return StatusCode(500, new { error = "An error occurred while retrieving certificates" });
            }
        }

        /// <summary>
        /// Search autocomplete suggestions for external view
        /// </summary>
        [HttpGet("autocomplete")]
        public async Task<IActionResult> GetAutocompleteSuggestions([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Ok(new { suggestions = new List<object>() });
            }

            try
            {
                term = term.ToLower();
                var suggestions = new List<object>();

                // Apply role-based filtering for autocomplete
                var userRole = GetCurrentUserRole();
                var baseQuery = _context.Certificates.AsQueryable();

                if (userRole == "Partner")
                {
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    if (userPartnerNames.Any())
                    {
                        baseQuery = baseQuery.Where(c => userPartnerNames.Contains(c.Partner ?? "None"));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(c => false);
                    }
                }
                else if (userRole == "External")
                {
                    var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                    if (userCompanyIds.Any())
                    {
                        baseQuery = baseQuery.Where(c => userCompanyIds.Contains(c.CompanyId));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(c => false);
                    }
                }

                // Only show active certificates in autocomplete
                baseQuery = baseQuery.Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday));

                // Search certificate numbers
                var certNumbers = await baseQuery
                    .Where(c => c.Number.ToLower().Contains(term))
                    .Select(c => new {
                        value = c.Number,
                        label = c.Number,
                        type = "Certificate Number"
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(certNumbers);

                // Search companies (only those with certificates visible to this user based on role)
                var userPartnerNames2 = userRole == "Partner" ? await GetCurrentUserPartnerNamesAsync() : new List<string>();
                var userCompanyIds2 = userRole == "External" ? await GetCurrentUserCompanyIdsAsync() : new List<int>();

                var companies = await _context.Companies
                    .Where(c => c.Name.ToLower().Contains(term) &&
                               c.Certificates.Any(cert =>
                                   (userRole != "Partner" && userRole != "External") ||
                                   (userRole == "Partner" && userPartnerNames2.Contains(cert.Partner ?? "None")) ||
                                   (userRole == "External" && userCompanyIds2.Contains(cert.CompanyId)) &&
                                   CertificateStatusHelper.PublicStatuses.Contains(cert.Status) && (cert.ExpirationDate == null || cert.ExpirationDate >= CertificateStatusHelper.BusinessToday)))
                    .Select(c => new {
                        value = c.Name,
                        label = c.Name,
                        type = "Company"
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(companies);

                // Search products (only those with certificates visible to this user based on role)
                var products = await _context.Products
                    .Where(p => p.Name.ToLower().Contains(term) &&
                               p.Certificates.Any(cert =>
                                   (userRole != "Partner" && userRole != "External") ||
                                   (userRole == "Partner" && userPartnerNames2.Contains(cert.Partner ?? "None")) ||
                                   (userRole == "External" && userCompanyIds2.Contains(cert.CompanyId)) &&
                                   CertificateStatusHelper.PublicStatuses.Contains(cert.Status) && (cert.ExpirationDate == null || cert.ExpirationDate >= CertificateStatusHelper.BusinessToday)))
                    .Select(p => new {
                        value = p.Name,
                        label = p.Name,
                        type = "Product"
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(products);

                // Search certification types
                var certTypes = await _context.CertificationTypes
                    .Where(ct => ct.Name.ToLower().Contains(term))
                    .Select(ct => new { 
                        value = ct.Name, 
                        label = ct.Name, 
                        type = "Certification Type" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(certTypes);

                // Search locations
                var locations = await baseQuery
                    .Where(c => c.Location != null && c.Location.ToLower().Contains(term))
                    .Select(c => c.Location)
                    .Distinct()
                    .Select(l => new { 
                        value = l!, 
                        label = l!, 
                        type = "Location" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(locations);

                return Ok(new { suggestions = suggestions.Take(15) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting autocomplete suggestions for external view");
                return Ok(new { suggestions = new List<object>() });
            }
        }

        /// <summary>
        /// Get filter options for external view (limited to partner-accessible data)
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                // Apply role-based filtering for filter options
                var userRole = GetCurrentUserRole();
                var baseQuery = _context.Certificates.AsQueryable();

                if (userRole == "Partner")
                {
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    if (userPartnerNames.Any())
                    {
                        baseQuery = baseQuery.Where(c => userPartnerNames.Contains(c.Partner ?? "None"));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(c => false);
                    }
                }
                else if (userRole == "External")
                {
                    var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                    if (userCompanyIds.Any())
                    {
                        baseQuery = baseQuery.Where(c => userCompanyIds.Contains(c.CompanyId));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(c => false);
                    }
                }

                // Filter to active certificates only
                baseQuery = baseQuery.Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday));

                // DEF-199 (QC #178): use the EFFECTIVE category/type/subcategory at each level
                // (cert-level override if set, otherwise Product-derived) — mirrors DEF-192's
                // listing predicate. The Product-only projection missed certs whose hierarchy
                // was set via the cert-level FK overrides.
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

                // Build hierarchy matching Admin page: Category → Type (ParentId=null) → Subcategory (ParentId!=null)
                var prodCategories = await _context.Set<ProductCategory>()
                    .Where(pc => pc.IsActive)
                    .OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name)
                    .ToListAsync();

                var allProdTypes = await _context.Set<ProductType>()
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.SortOrder).ThenBy(pt => pt.Name)
                    .ToListAsync();

                var typesByCategory = allProdTypes.Where(pt => pt.ParentId == null)
                    .GroupBy(pt => pt.CategoryId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var childrenByParent = allProdTypes.Where(pt => pt.ParentId != null)
                    .GroupBy(pt => pt.ParentId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Trim hierarchy (DEF-168): Category kept ONLY if a cert's Product.CategoryId
                // directly points at it. Previous `|| c.types.Any()` fallback leaked orphan
                // parent categories (e.g. a category referenced only as the administrative
                // parent of a Product Type) because the find-side in CertificateRepository
                // doesn't traverse Product.ProductType.Category — clicking such a category
                // returned zero. Type-level fallback stays; safe because find side has
                // parent-cascade on ProductType.Parent.Name.
                var productCategoriesHierarchy = prodCategories
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

                // Get unique certification type IDs from active certificates
                var certTypeIds = await baseQuery
                    .Select(c => c.CertificationTypeId)
                    .Distinct()
                    .ToListAsync();

                // DEF-167: Filter dropdown shows Level-1 (top-level) cert types only.
                // Subcategories should not appear as filter options; certs tagged at Level-2
                // must be repointed at their Level-1 parent via
                // Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql before they will be
                // reachable through this dropdown.
                // QC #124: alpha-sort by Name (SortOrder column has no admin UI, so its values
                // are stale config). See CertificateTilesApiController for the rationale.
                var hierarchicalCertTypes = await _context.CertificationTypes
                    .Where(ct => ct.IsActive && ct.ParentId == null && certTypeIds.Contains(ct.Id))
                    .OrderBy(ct => ct.Name)
                    .Select(ct => new { name = ct.Name, level = ct.Level })
                    .ToListAsync();

                var filters = new
                {
                    certificationTypes = hierarchicalCertTypes,

                    productCategoriesHierarchy,

                    // DEF-199 (QC #178): flat lists resolved from the same visible* sets the
                    // hierarchy uses, so cert-level FK overrides surface here too.
                    productCategories = await _context.Set<ProductCategory>()
                        .Where(pc => pc.IsActive && visibleCategoryIds.Contains(pc.Id))
                        .OrderBy(pc => pc.Name)
                        .Select(pc => pc.Name)
                        .ToListAsync(),

                    productTypes = await _context.Set<ProductType>()
                        .Where(pt => pt.IsActive && visibleProductTypeIds.Contains(pt.Id))
                        .OrderBy(pt => pt.Name)
                        .Select(pt => pt.Name)
                        .ToListAsync(),

                    // DEF-199 (QC #177): also exclude soft-deleted / inactive companies so a
                    // cert's stale Company reference can't surface in the dropdown.
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

                    tags = await baseQuery
                        .SelectMany(c => c.Tags)
                        .Where(t => t.IsActive)
                        .Select(t => t.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync(),

                    additionalTags = await baseQuery
                        .SelectMany(c => c.Tags)
                        .Where(t => t.IsActive && t.TagType == "external")
                        .Select(t => t.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync(),

                    // Show available partners (for partner switching if user has access to multiple)
                    partners = await baseQuery
                        .Select(c => c.Partner ?? "None")
                        .Distinct()
                        .OrderBy(p => p)
                        .ToListAsync()
                };

                return Ok(filters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options for external view");
                return StatusCode(500, new { error = "An error occurred while retrieving filter options" });
            }
        }

        /// <summary>
        /// Download certificate PDF (same as public endpoint)
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadCertificate(int id)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);

                if (certificate == null)
                {
                    return NotFound(new { error = "Certificate not found" });
                }

                // Check if user has access to this certificate based on role
                var userRole = GetCurrentUserRole();
                var hasAccess = false;

                if (userRole == "Admin" || userRole == "Analyst")
                {
                    // Admin/Analyst have full access
                    hasAccess = true;
                }
                else if (userRole == "Partner")
                {
                    // Partner users can access certificates tagged with their partner
                    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
                    hasAccess = userPartnerNames.Contains(certificate.Partner ?? "None");
                }
                else if (userRole == "External")
                {
                    // External users can only access certificates from their companies
                    var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                    hasAccess = userCompanyIds.Contains(certificate.CompanyId);
                }

                if (!hasAccess)
                {
                    _logger.LogWarning("User {Email} with role {Role} attempted to download certificate {CertId} without access",
                        User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "unknown",
                        userRole,
                        id);
                    return Forbid("You don't have access to this certificate");
                }

                // Only allow download of active certificates for external users
                if (!CertificateStatusHelper.IsPubliclyVisible(certificate.Status) || (certificate.ExpirationDate != null && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday))
                {
                    return BadRequest(new { error = "Certificate is not available for download" });
                }

                if (string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    return NotFound(new { error = "Certificate PDF not available" });
                }

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", certificate.CertificateFile.TrimStart('/'));
                
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound(new { error = "Certificate PDF file not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var fileName = $"Certificate_{certificate.Number}.pdf";
                
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate {Id}", id);
                return StatusCode(500, new { error = "An error occurred while downloading the certificate" });
            }
        }

        /// <summary>
        /// Get shareable link for filtered results (external view)
        /// </summary>
        [HttpPost("share-link")]
        public IActionResult GetShareableLink([FromBody] ExternalShareLinkRequest request)
        {
            try
            {
                var queryParams = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(request.Search))
                    queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");
                    
                if (!string.IsNullOrWhiteSpace(request.CertType))
                    queryParams.Add($"certType={Uri.EscapeDataString(request.CertType)}");
                    
                if (!string.IsNullOrWhiteSpace(request.ProductCategory))
                    queryParams.Add($"category={Uri.EscapeDataString(request.ProductCategory)}");
                    
                if (!string.IsNullOrWhiteSpace(request.Company))
                    queryParams.Add($"company={Uri.EscapeDataString(request.Company)}");
                    
                if (!string.IsNullOrWhiteSpace(request.Country))
                    queryParams.Add($"country={Uri.EscapeDataString(request.Country)}");
                    
                if (!string.IsNullOrWhiteSpace(request.Tags))
                    queryParams.Add($"tags={Uri.EscapeDataString(request.Tags)}");
                    
                if (!string.IsNullOrWhiteSpace(request.Partner))
                    queryParams.Add($"partner={Uri.EscapeDataString(request.Partner)}");
                    
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                    queryParams.Add($"sort={Uri.EscapeDataString(request.SortBy)}");

                var baseUrl = $"{Request.Scheme}://{Request.Host}/Certificate/CertificateTileViewExternal";
                var shareableUrl = queryParams.Any() 
                    ? $"{baseUrl}?{string.Join("&", queryParams)}"
                    : baseUrl;

                return Ok(new { url = shareableUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating shareable link for external view");
                return StatusCode(500, new { error = "An error occurred while generating the link" });
            }
        }

        /// <summary>
        /// Get the current user's role
        /// </summary>
        private string GetCurrentUserRole()
        {
            return User?.FindFirst("Role")?.Value ?? "Public";
        }

        /// <summary>
        /// Get the partner associated with the current user (for Partner role)
        /// </summary>
        private string? GetUserPartner()
        {
            var partner = User?.FindFirst("partner")?.Value;

            // Log warning if Partner role user has no partner claim
            var role = GetCurrentUserRole();
            if (role == "Partner" && string.IsNullOrEmpty(partner))
            {
                _logger.LogWarning("Partner role user {Email} has no partner claim",
                    User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "unknown");
            }

            return partner;
        }

        /// <summary>
        /// Get company IDs associated with the current user (for External role)
        /// </summary>
        private async Task<List<int>> GetCurrentUserCompanyIdsAsync()
        {
            var userEmail = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return new List<int>();

            var user = await _context.Users
                .Include(u => u.Companies)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user?.Companies != null && user.Companies.Any())
            {
                return user.Companies.Select(c => c.Id).ToList();
            }

            // Log warning if External role user has no company associations
            var role = GetCurrentUserRole();
            if (role == "External")
            {
                _logger.LogWarning("External role user {Email} has no company associations", userEmail);
            }

            return new List<int>();
        }

        /// <summary>
        /// Get partner names associated with the current user (for Partner role from database)
        /// </summary>
        private async Task<List<string>> GetCurrentUserPartnerNamesAsync()
        {
            var userEmail = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return new List<string>();

            var user = await _context.Users
                .Include(u => u.Partners)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user?.Partners != null && user.Partners.Any())
            {
                return user.Partners.Select(p => p.Name).ToList();
            }

            return new List<string>();
        }

        private IQueryable<Certificate> ApplySorting(IQueryable<Certificate> query, string? sortBy)
        {
            // .ThenBy(c => c.Id) tiebreaker on every branch — see Internal controller for full rationale.
            // Without it, ties on the primary sort key produce non-deterministic SQL row order, page 1
            // and page 2 overlap, and the JS dedupe-by-id silently drops rows ("15 of 35" symptom).
            // QC #157 / DEF-(sort-parity): this switch is kept identical to the Internal controller's
            // ApplySorting so the external table sorts every column the header offers. Previously only a
            // subset of branches existed here, so clicking ASIN/UPC/GTIN/EAN/Status/Tag/Subcategory/
            // Contact/Cert-Results/Pathway/Cert-Subcats/Company-URL silently fell through to the default
            // (Effective-Date desc) and the header appeared to do nothing. ID columns use the natural
            // (length-then-value) sort so numeric identifiers order 9 < 10 < 100. Empties always last.
            return sortBy?.ToLower() switch
            {
                "newest" => query.OrderByDescending(c => c.EffectiveDate).ThenBy(c => c.Id),
                "oldest-effective" => query.OrderBy(c => c.EffectiveDate).ThenBy(c => c.Id),
                "expiring" => query.OrderBy(c => c.ExpirationDate).ThenBy(c => c.Id),
                "longest-validity" => query.OrderByDescending(c =>
                    EF.Functions.DateDiffDay(c.EffectiveDate, c.ExpirationDate)).ThenBy(c => c.Id),
                "recently-issued" => query
                    .Where(c => c.EffectiveDate >= DateTime.Now.AddDays(-30))
                    .OrderByDescending(c => c.EffectiveDate).ThenBy(c => c.Id),
                "cert-number-newest" => query.OrderByDescending(c => c.Number).ThenBy(c => c.Id),
                "cert-number-oldest" => query.OrderBy(c => c.Number).ThenBy(c => c.Id),
                "cert-type" => query.OrderByTextEmptiesLast(c => c.CertificationType.Name, false).ThenBy(c => c.Id),
                "cert-type-za" => query.OrderByTextEmptiesLast(c => c.CertificationType.Name, true).ThenBy(c => c.Id),
                "status" => query.OrderBy(c => c.Status).ThenBy(c => c.Id),
                "status-za" => query.OrderByDescending(c => c.Status).ThenBy(c => c.Id),
                "company-az" => query.OrderByTextEmptiesLast(c => c.Company.Name, false).ThenBy(c => c.Id),
                "company-za" => query.OrderByTextEmptiesLast(c => c.Company.Name, true).ThenBy(c => c.Id),
                "product-az" => query.OrderByTextEmptiesLast(c => c.Product.Name, false).ThenBy(c => c.Id),
                "product-za" => query.OrderByTextEmptiesLast(c => c.Product.Name, true).ThenBy(c => c.Id),
                // QC #157: empties always last (consistent across all three tile views). See
                // Extensions/CertificateSortExtensions.cs and the Internal controller's ApplySorting.
                "product-cat" => query.OrderByTextEmptiesLast(c => c.ProductCategory != null ? c.ProductCategory.Name : (c.Product != null && c.Product.Category != null ? c.Product.Category.Name : ""), false).ThenBy(c => c.Id),
                "product-cat-za" => query.OrderByTextEmptiesLast(c => c.ProductCategory != null ? c.ProductCategory.Name : (c.Product != null && c.Product.Category != null ? c.Product.Category.Name : ""), true).ThenBy(c => c.Id),
                "product-type" => query.OrderByTextEmptiesLast(c => c.ProductType != null ? c.ProductType.Name : (c.Product.ProductType != null ? c.Product.ProductType.Name : ""), false).ThenBy(c => c.Id),
                "product-type-za" => query.OrderByTextEmptiesLast(c => c.ProductType != null ? c.ProductType.Name : (c.Product.ProductType != null ? c.Product.ProductType.Name : ""), true).ThenBy(c => c.Id),
                "product-sub" => query.OrderByTextEmptiesLast(c => c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : ""), false).ThenBy(c => c.Id),
                "product-sub-za" => query.OrderByTextEmptiesLast(c => c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : ""), true).ThenBy(c => c.Id),
                "location-az" => query.OrderByTextEmptiesLast(c => c.Location, false).ThenBy(c => c.Id),
                "location-za" => query.OrderByTextEmptiesLast(c => c.Location, true).ThenBy(c => c.Id),
                "country" => query.OrderByTextEmptiesLast(c => c.Country, false).ThenBy(c => c.Id),
                "country-za" => query.OrderByTextEmptiesLast(c => c.Country, true).ThenBy(c => c.Id),
                "ext-tag" => query.OrderByTextEmptiesLast(c => c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).FirstOrDefault(), false).ThenBy(c => c.Id),
                "ext-tag-za" => query.OrderByTextEmptiesLast(c => c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).FirstOrDefault(), true).ThenBy(c => c.Id),
                "partner" => query.OrderByTextEmptiesLast(c => c.Partner, false).ThenBy(c => c.Id),
                "partner-za" => query.OrderByTextEmptiesLast(c => c.Partner, true).ThenBy(c => c.Id),
                // DEF-260: ID columns sort by the COUNT of IDs on the cert (the count-badge value the
                // grid shows), matching the internal Cert List. Count 0 sorts last in both directions.
                // (External does not currently project these cells, so the column renders empty here and
                // this falls back to Id order — kept in parity with the internal controller regardless.)
                "asin" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "ASIN" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "ASIN" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Asin) ? 0 : 1), false).ThenBy(c => c.Id),
                "asin-za" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "ASIN" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "ASIN" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Asin) ? 0 : 1), true).ThenBy(c => c.Id),
                "upc" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "UPC" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "UPC" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Upc) ? 0 : 1), false).ThenBy(c => c.Id),
                "upc-za" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "UPC" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "UPC" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Upc) ? 0 : 1), true).ThenBy(c => c.Id),
                "gtin" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "GTIN" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "GTIN" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Gtin) ? 0 : 1), false).ThenBy(c => c.Id),
                "gtin-za" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "GTIN" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "GTIN" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Gtin) ? 0 : 1), true).ThenBy(c => c.Id),
                "ean" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "EAN" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "EAN" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Ean) ? 0 : 1), false).ThenBy(c => c.Id),
                "ean-za" => query.OrderByCountEmptiesLast(c => c.ProductIds.Count(p => p.ProductIdType == "EAN" && p.IsActive) > 0
                            ? c.ProductIds.Count(p => p.ProductIdType == "EAN" && p.IsActive)
                            : (string.IsNullOrEmpty(c.Ean) ? 0 : 1), true).ThenBy(c => c.Id),
                "contact" => query.OrderByTextEmptiesLast(c => !string.IsNullOrEmpty(c.Company.Email) ? c.Company.Email : c.Company.Website, false).ThenBy(c => c.Id),
                "contact-za" => query.OrderByTextEmptiesLast(c => !string.IsNullOrEmpty(c.Company.Email) ? c.Company.Email : c.Company.Website, true).ThenBy(c => c.Id),
                "cert-results" => query.OrderByNumericNullsLast(c => c.CertificationResultsNumeric, false).ThenBy(c => c.Id),
                "cert-results-za" => query.OrderByNumericNullsLast(c => c.CertificationResultsNumeric, true).ThenBy(c => c.Id),
                "pathway" => query.OrderByTextEmptiesLast(c => c.Pathway != null ? c.Pathway.Name : "", false).ThenBy(c => c.Id),
                "pathway-za" => query.OrderByTextEmptiesLast(c => c.Pathway != null ? c.Pathway.Name : "", true).ThenBy(c => c.Id),
                "cert-sub" => query.OrderByTextEmptiesLast(c => c.CertificationTypeSubcategories.Select(s => s.Name).Min(), false).ThenBy(c => c.Id),
                "cert-sub-za" => query.OrderByTextEmptiesLast(c => c.CertificationTypeSubcategories.Select(s => s.Name).Min(), true).ThenBy(c => c.Id),
                "company-url" => query.OrderByTextEmptiesLast(c => c.Company.Website, false).ThenBy(c => c.Id),
                "company-url-za" => query.OrderByTextEmptiesLast(c => c.Company.Website, true).ThenBy(c => c.Id),
                _ => query.OrderByDescending(c => c.EffectiveDate).ThenBy(c => c.Id)
            };
        }
    }

    public class ExternalShareLinkRequest
    {
        public string? Search { get; set; }
        public string? CertType { get; set; }
        public string? ProductCategory { get; set; }
        public string? Company { get; set; }
        public string? Country { get; set; }
        public string? Tags { get; set; }
        public string? Partner { get; set; }
        public string? SortBy { get; set; }
    }
}
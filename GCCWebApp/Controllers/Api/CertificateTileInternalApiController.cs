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
    [Route("api/certificate-tiles-internal")]
    [Authorize(Policy = "AnalystOrAdmin")]
    public class CertificateTileInternalApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificateTileInternalApiController> _logger;

        public CertificateTileInternalApiController(
            ApplicationDbContext context,
            ILogger<CertificateTileInternalApiController> logger)
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
        /// Get certificates for internal tile view with lazy loading
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCertificateTiles(
            [FromQuery] string? search = null,
            [FromQuery] string? certType = null,
            [FromQuery] string? productCategory = null,
            [FromQuery] string? company = null,
            [FromQuery] string? country = null,
            [FromQuery] string? tags = null,
            [FromQuery] string? status = null,
            [FromQuery] string? partner = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 20)
        {
            try
            {
                // Perf: AsSplitQuery() is the canonical EF Core fix for "many collection Includes +
                // pagination". Without it, the four collection navs (Tags, ProductIds,
                // CertificationTypeSubcategories, CustomFieldValues) cartesian-multiply rows and
                // OFFSET/FETCH then returns fewer than pageSize unique certs (root cause of
                // "15 of 35"). AsSplitQuery makes EF emit one query per collection instead — no
                // cartesian, deterministic pagination. AsNoTracking because this path is read-only.
                //
                // We keep the .Include() calls because the .Select() projection still benefits from
                // the loaded refs being in change-tracker context for the projection translator.
                // (Removing Includes entirely caused the projection to emit 240+ correlated subqueries
                // and time out at 30s — see DEF-166 follow-up 2026-05-08.)
                var query = _context.Certificates
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.Product.ProductType)
                    .Include(c => c.Product.ProductSubcategory)
                    .Include(c => c.ProductCategory)
                    .Include(c => c.ProductType)
                    .Include(c => c.ProductSubcategory)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Pathway)
                    .Include(c => c.CertificationTypeSubcategories)
                    .Include(c => c.Tags)
                    .Include(c => c.ProductIds)
                    .Include(c => c.CustomFieldValues)
                        .ThenInclude(cfv => cfv.Field)
                    .AsQueryable();

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
                // hid from display. Display projection at lines 230-232 below; this predicate
                // must mirror it.
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

                // Apply tags filter
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    var tagsList = tags.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => c.Tags.Any(t => tagsList.Contains(t.Name)));
                }

                // Apply status filter (Internal view specific)
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var statuses = status.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => statuses.Contains(c.Status.ToString()));
                }

                // Apply partner filter (Internal view specific)
                if (!string.IsNullOrWhiteSpace(partner))
                {
                    var partners = partner.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => partners.Contains(c.Partner ?? "None"));
                }

                // Apply sorting
                query = ApplySorting(query, sortBy);

                // Get total count
                var totalCount = await query.CountAsync();

                // Support both offset/limit and page/pageSize for compatibility
                var actualOffset = offset > 0 ? offset : (page - 1) * pageSize;
                var actualLimit = limit > 0 ? limit : pageSize;

                // Apply pagination - first fetch raw data
                var rawCertificates = await query
                    .Skip(actualOffset)
                    .Take(actualLimit)
                    .Select(c => new
                    {
                        c.Id,
                        RawNumber = c.Number,
                        c.EffectiveDate,
                        CertificationType = c.CertificationType.Name,
                        CsfAttributes = c.CertificationType.Description ?? "",
                        c.Status,
                        c.ExpirationDate,
                        CompanyName = c.Company.Name,
                        CompanyContact = !string.IsNullOrEmpty(c.Company.Email) ? c.Company.Email : c.Company.Website ?? "",
                        ProductName = c.Product.Name,
                        ProductCategory = c.ProductCategory != null ? c.ProductCategory.Name : c.Product.Category.Name,
                        ProductType = c.ProductType != null ? c.ProductType.Name : (c.Product.ProductType != null ? c.Product.ProductType.Name : ""),
                        ProductSubcategory = c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : ""),
                        c.Location,
                        c.Country,
                        ExternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "external").Select(t => t.Name)),
                        InternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name)),
                        // Product IDs - get ALL active IDs of each type, comma-separated for display
                        // Fallback to legacy certificate fields if ProductIds collection is empty
                        AsinList = c.ProductIds.Where(pid => pid.ProductIdType == "ASIN" && pid.IsActive).Select(pid => pid.ProductIdValue).ToList(),
                        LegacyAsin = c.Asin ?? "",
                        UpcList = c.ProductIds.Where(pid => pid.ProductIdType == "UPC" && pid.IsActive).Select(pid => pid.ProductIdValue).ToList(),
                        LegacyUpc = c.Upc ?? "",
                        GtinList = c.ProductIds.Where(pid => pid.ProductIdType == "GTIN" && pid.IsActive).Select(pid => pid.ProductIdValue).ToList(),
                        LegacyGtin = c.Gtin ?? "",
                        EanList = c.ProductIds.Where(pid => pid.ProductIdType == "EAN" && pid.IsActive).Select(pid => pid.ProductIdValue).ToList(),
                        LegacyEan = c.Ean ?? "",
                        // Additional product ID types
                        Isbn = c.ProductIds.Where(pid => pid.ProductIdType == "ISBN" && pid.IsActive).Select(pid => pid.ProductIdValue).FirstOrDefault() ?? "",
                        Sku = c.ProductIds.Where(pid => pid.ProductIdType == "SKU" && pid.IsActive).Select(pid => pid.ProductIdValue).FirstOrDefault() ?? "",
                        Mpn = c.ProductIds.Where(pid => pid.ProductIdType == "MPN" && pid.IsActive).Select(pid => pid.ProductIdValue).FirstOrDefault() ?? "",
                        // All product IDs for detailed view
                        AllProductIds = c.ProductIds.Where(pid => pid.IsActive).Select(pid => new {
                            Type = pid.ProductIdType,
                            Value = pid.ProductIdValue,
                            Partner = pid.Partner,
                            ProductName = pid.ProductName
                        }).ToList(),
                        PdfPath = c.CertificateFile,
                        c.Partner,
                        c.CreatedAt,
                        c.UpdatedAt,
                        CertificationResults = c.CertificationResults ?? "",
                        PathwayName = c.Pathway != null ? c.Pathway.Name : "",
                        CertSubcategories = string.Join(", ", c.CertificationTypeSubcategories.Select(s => s.Name)),
                        CompanyUrl = c.Company.Website ?? "",
                        // CR-007: per-cert custom field values keyed by field id.
                        // Values are stored in TextValue regardless of FieldType (per CertificateFieldService).
                        CustomFieldValues = c.CustomFieldValues
                            .Where(cfv => cfv.Field != null && cfv.Field.IsVisible && !string.IsNullOrWhiteSpace(cfv.TextValue))
                            .Select(cfv => new { cfv.FieldId, cfv.TextValue })
                            .ToList()
                    })
                    .ToListAsync();

                // CR-007: fetch all visible custom field definitions for the column toggle
                // Internal table = analyst/admin → show every field marked IsVisible (RoleVisibility is enforced
                // for partner/external surfaces, not the internal admin grid).
                var customFieldDefinitions = await _context.CustomFields
                    .Where(cf => cf.IsVisible)
                    .OrderBy(cf => cf.SortOrder).ThenBy(cf => cf.Name)
                    .Select(cf => new
                    {
                        id = cf.Id,
                        name = cf.Name,
                        section = cf.Section,
                        sortOrder = cf.SortOrder
                    })
                    .ToListAsync();

                // Format certificate numbers to YY-NNNN format and combine product IDs with legacy fallback
                var certificates = rawCertificates.Select(c => new
                {
                    c.Id,
                    CertificateNumber = FormatCertificateNumber(c.RawNumber, c.EffectiveDate),
                    c.CertificationType,
                    c.CsfAttributes,
                    c.Status,
                    // Emit as yyyy-MM-dd so the JS parseLocalDate hits the date-only branch.
                    // Raw DateTime would go through UtcDateTimeConverter, which appends Z and shifts the
                    // calendar date back a day in negative-UTC zones (DEF: tile period off-by-one).
                    EffectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                    ExpirationDate = c.ExpirationDate.ToString("yyyy-MM-dd"),
                    c.CompanyName,
                    c.CompanyContact,
                    c.ProductName,
                    c.ProductCategory,
                    c.ProductType,
                    c.ProductSubcategory,
                    c.Location,
                    c.Country,
                    c.ExternalTags,
                    c.InternalTags,
                    // Combine ProductIds list with legacy field fallback
                    Asin = c.AsinList.Count > 0 ? string.Join(", ", c.AsinList) : c.LegacyAsin,
                    Upc = c.UpcList.Count > 0 ? string.Join(", ", c.UpcList) : c.LegacyUpc,
                    Gtin = c.GtinList.Count > 0 ? string.Join(", ", c.GtinList) : c.LegacyGtin,
                    Ean = c.EanList.Count > 0 ? string.Join(", ", c.EanList) : c.LegacyEan,
                    c.Isbn,
                    c.Sku,
                    c.Mpn,
                    c.AllProductIds,
                    c.PdfPath,
                    c.Partner,
                    c.CreatedAt,
                    c.UpdatedAt,
                    CertificationResults = DisplayFormatters.FormatCertificationResults(c.CertificationResults),
                    c.PathwayName,
                    c.CertSubcategories,
                    c.CompanyUrl,
                    // CR-007: emit a flat object so JS can do `cert.customFields[fieldId]`
                    customFields = c.CustomFieldValues.ToDictionary(v => v.FieldId.ToString(), v => v.TextValue ?? string.Empty)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    certificates,
                    totalCount,
                    loadedCount = actualOffset + certificates.Count(),
                    hasMore = actualOffset + certificates.Count() < totalCount,
                    page = Math.Max(1, (actualOffset / actualLimit) + 1),
                    pageSize = actualLimit,
                    totalPages = (int)Math.Ceiling((double)totalCount / actualLimit),
                    customFieldDefinitions, // CR-007
                    message = (string?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting internal certificate tiles");
                return StatusCode(500, new { error = "An error occurred while retrieving certificates" });
            }
        }

        /// <summary>
        /// Search autocomplete suggestions for internal view
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

                // Search certificate numbers
                var certNumbers = await _context.Certificates
                    .Where(c => c.Number.ToLower().Contains(term))
                    .Select(c => new { 
                        value = c.Number, 
                        label = c.Number, 
                        type = "Certificate Number" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(certNumbers);

                // Search companies
                var companies = await _context.Companies
                    .Where(c => c.Name.ToLower().Contains(term))
                    .Select(c => new { 
                        value = c.Name, 
                        label = c.Name, 
                        type = "Company" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(companies);

                // Search products
                var products = await _context.Products
                    .Where(p => p.Name.ToLower().Contains(term))
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
                var locations = await _context.Certificates
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
                _logger.LogError(ex, "Error getting autocomplete suggestions for internal view");
                return Ok(new { suggestions = new List<object>() });
            }
        }

        /// <summary>
        /// Get filter options for internal view
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                // Build hierarchy matching Admin page: Category → Type (ParentId=null) → Subcategory (ParentId!=null)
                var categories = await _context.ProductCategories
                    .Where(pc => pc.IsActive)
                    .OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name)
                    .ToListAsync();

                var allTypes = await _context.ProductTypes
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.SortOrder).ThenBy(pt => pt.Name)
                    .ToListAsync();

                var typesByCategory = allTypes.Where(pt => pt.ParentId == null)
                    .GroupBy(pt => pt.CategoryId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var childrenByParent = allTypes.Where(pt => pt.ParentId != null)
                    .GroupBy(pt => pt.ParentId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var productCategoriesHierarchy = categories.Select(pc => new
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
                                .Select(sub => new
                                {
                                    id = sub.Id,
                                    name = sub.Name,
                                    level = 3
                                }).ToList()
                        }).ToList()
                }).ToList();

                // Get unique certification type IDs from certificates
                var certTypeIds = await _context.Certificates
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

                    // Keep flat list for backward compatibility
                    productCategories = await _context.Set<ProductCategory>()
                        .Where(pc => pc.IsActive)
                        .OrderBy(pc => pc.Name)
                        .Select(pc => pc.Name)
                        .ToListAsync(),

                    productTypes = await _context.Set<ProductType>()
                        .Where(pt => pt.IsActive)
                        .OrderBy(pt => pt.Name)
                        .Select(pt => pt.Name)
                        .ToListAsync(),

                    companies = await _context.Companies
                        .Where(c => c.IsActive && !c.IsDeleted)
                        .Select(c => new {
                            id = c.Id,
                            name = c.Name,
                            certificateCount = c.Certificates.Count()
                        })
                        .Where(c => c.certificateCount > 0)
                        .OrderBy(c => c.name)
                        .Select(c => new { c.id, c.name })
                        .ToListAsync(),

                    countries = (await _context.Certificates
                        .Where(c => !string.IsNullOrEmpty(c.Country))
                        .Select(c => c.Country!)
                        .Distinct()
                        .ToListAsync())
                        .OrderBy(c => c == "United States" ? 0 : 1)
                        .ThenBy(c => c)
                        .ToList(),

                    // Internal view: provide separate lists for internal and external tags
                    internalTags = await _context.Tags
                        .Where(t => t.IsActive && t.TagType == "internal")
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .ToListAsync(),

                    externalTags = await _context.Tags
                        .Where(t => t.IsActive && t.TagType == "external")
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .ToListAsync(),

                    // Combined tags for backward compatibility
                    tags = await _context.Tags
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .ToListAsync(),

                    // Internal-specific filters
                    statuses = Enum.GetNames(typeof(CertificateStatus)).ToList(),

                    partners = await _context.Certificates
                        .Select(c => c.Partner ?? "None")
                        .Distinct()
                        .OrderBy(p => p)
                        .ToListAsync(),

                    additionalTags = await _context.Tags
                        .Where(t => t.IsActive && t.TagType == "external")
                        .OrderBy(t => t.Name)
                        .Select(t => t.Name)
                        .ToListAsync()
                };

                return Ok(filters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options for internal view");
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
        /// Get shareable link for filtered results (internal view)
        /// </summary>
        [HttpPost("share-link")]
        public IActionResult GetShareableLink([FromBody] InternalShareLinkRequest request)
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
                    
                if (!string.IsNullOrWhiteSpace(request.Status))
                    queryParams.Add($"status={Uri.EscapeDataString(request.Status)}");
                    
                if (!string.IsNullOrWhiteSpace(request.Partner))
                    queryParams.Add($"partner={Uri.EscapeDataString(request.Partner)}");
                    
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                    queryParams.Add($"sort={Uri.EscapeDataString(request.SortBy)}");

                var baseUrl = $"{Request.Scheme}://{Request.Host}/Certificate/CertificateTileViewInternal";
                var shareableUrl = queryParams.Any() 
                    ? $"{baseUrl}?{string.Join("&", queryParams)}"
                    : baseUrl;

                return Ok(new { url = shareableUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating shareable link for internal view");
                return StatusCode(500, new { error = "An error occurred while generating the link" });
            }
        }

        private IQueryable<Certificate> ApplySorting(IQueryable<Certificate> query, string? sortBy)
        {
            // .ThenBy(c => c.Id) tiebreaker on every branch: when the primary sort key has duplicates
            // (e.g. multiple certs share an EffectiveDate), SQL Server's row order is otherwise
            // non-deterministic — page 1 and page 2 then overlap, the JS dedupes-by-id silently drops
            // duplicates, and the user sees fewer total certs than expected ("15 of 35" symptom).
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
                "cert-type" => query.OrderBy(c => c.CertificationType.Name).ThenBy(c => c.Id),
                "cert-type-za" => query.OrderByDescending(c => c.CertificationType.Name).ThenBy(c => c.Id),
                "status" => query.OrderBy(c => c.Status).ThenBy(c => c.Id),
                "status-za" => query.OrderByDescending(c => c.Status).ThenBy(c => c.Id),
                "company-az" => query.OrderBy(c => c.Company.Name).ThenBy(c => c.Id),
                "company-za" => query.OrderByDescending(c => c.Company.Name).ThenBy(c => c.Id),
                "product-az" => query.OrderBy(c => c.Product.Name).ThenBy(c => c.Id),
                "product-za" => query.OrderByDescending(c => c.Product.Name).ThenBy(c => c.Id),
                // DEF-165: Product Category was missing — frontend used to remap productCategory→product-type,
                // so the column appeared to sort but actually sorted by Product Type. Real sort key now wired.
                "product-cat" => query.OrderBy(c => c.ProductCategory != null ? c.ProductCategory.Name : (c.Product != null && c.Product.Category != null ? c.Product.Category.Name : "")).ThenBy(c => c.Id),
                "product-cat-za" => query.OrderByDescending(c => c.ProductCategory != null ? c.ProductCategory.Name : (c.Product != null && c.Product.Category != null ? c.Product.Category.Name : "")).ThenBy(c => c.Id),
                "product-type" => query.OrderBy(c => c.ProductType != null ? c.ProductType.Name : (c.Product.ProductType != null ? c.Product.ProductType.Name : "")).ThenBy(c => c.Id),
                "product-type-za" => query.OrderByDescending(c => c.ProductType != null ? c.ProductType.Name : (c.Product.ProductType != null ? c.Product.ProductType.Name : "")).ThenBy(c => c.Id),
                "product-sub" => query.OrderBy(c => c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : "")).ThenBy(c => c.Id),
                "product-sub-za" => query.OrderByDescending(c => c.ProductSubcategory != null ? c.ProductSubcategory.Name : (c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : "")).ThenBy(c => c.Id),
                "location-az" => query.OrderBy(c => c.Location ?? "").ThenBy(c => c.Id),
                "location-za" => query.OrderByDescending(c => c.Location ?? "").ThenBy(c => c.Id),
                "country" => query.OrderBy(c => c.Country ?? "").ThenBy(c => c.Id),
                "country-za" => query.OrderByDescending(c => c.Country ?? "").ThenBy(c => c.Id),
                // DEF-181: ID-column sort keys must mirror the display logic. The display falls back
                // from `ProductIds` (collection) to the legacy `Certificate.{Asin|Upc|Gtin|Ean}` column
                // when no active ProductIds exist (see line 314 of this file). The old sort comparator
                // only looked at ProductIds, so certs whose only ASIN was stored in the legacy column
                // got sort key "" and bunched together by Id while still displaying real values — Carly's
                // "ASIN/UPC not sorting right" complaint in QC #157.
                //
                // Fix: if any active ProductId exists, sort by Min/First of that subquery; otherwise fall
                // back to the legacy column. Same shape applied to all four ID columns.
                //
                // Note on int-tag/ext-tag/upc: per DEF-165 these three still use FirstOrDefault() instead
                // of .Min() because .Min() (or an inner OrderBy) caused 30s timeouts on the dev DB. Tags
                // have no legacy fallback column (they're collection-only), so the comparator shape is
                // unchanged for them and the existing FirstOrDefault non-determinism remains a perf
                // follow-up. The legacy-fallback fix here applies only to the four ID columns.
                "ext-tag" => query.OrderBy(c => c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).FirstOrDefault() ?? "").ThenBy(c => c.Id),
                "ext-tag-za" => query.OrderByDescending(c => c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).FirstOrDefault() ?? "").ThenBy(c => c.Id),
                "int-tag" => query.OrderBy(c => c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name).FirstOrDefault() ?? "").ThenBy(c => c.Id),
                "int-tag-za" => query.OrderByDescending(c => c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name).FirstOrDefault() ?? "").ThenBy(c => c.Id),
                "partner" => query.OrderBy(c => c.Partner ?? "").ThenBy(c => c.Id),
                "partner-za" => query.OrderByDescending(c => c.Partner ?? "").ThenBy(c => c.Id),
                "asin" => query.OrderBy(c => (c.ProductIds.Any(p => p.ProductIdType == "ASIN" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "ASIN" && p.IsActive).Select(p => p.ProductIdValue).Min()
                            : c.Asin) ?? "").ThenBy(c => c.Id),
                "asin-za" => query.OrderByDescending(c => (c.ProductIds.Any(p => p.ProductIdType == "ASIN" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "ASIN" && p.IsActive).Select(p => p.ProductIdValue).Min()
                            : c.Asin) ?? "").ThenBy(c => c.Id),
                "upc" => query.OrderBy(c => (c.ProductIds.Any(p => p.ProductIdType == "UPC" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "UPC" && p.IsActive).Select(p => p.ProductIdValue).FirstOrDefault()
                            : c.Upc) ?? "").ThenBy(c => c.Id),
                "upc-za" => query.OrderByDescending(c => (c.ProductIds.Any(p => p.ProductIdType == "UPC" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "UPC" && p.IsActive).Select(p => p.ProductIdValue).FirstOrDefault()
                            : c.Upc) ?? "").ThenBy(c => c.Id),
                "gtin" => query.OrderBy(c => (c.ProductIds.Any(p => p.ProductIdType == "GTIN" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "GTIN" && p.IsActive).Select(p => p.ProductIdValue).Min()
                            : c.Gtin) ?? "").ThenBy(c => c.Id),
                "gtin-za" => query.OrderByDescending(c => (c.ProductIds.Any(p => p.ProductIdType == "GTIN" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "GTIN" && p.IsActive).Select(p => p.ProductIdValue).Min()
                            : c.Gtin) ?? "").ThenBy(c => c.Id),
                "ean" => query.OrderBy(c => (c.ProductIds.Any(p => p.ProductIdType == "EAN" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "EAN" && p.IsActive).Select(p => p.ProductIdValue).Min()
                            : c.Ean) ?? "").ThenBy(c => c.Id),
                "ean-za" => query.OrderByDescending(c => (c.ProductIds.Any(p => p.ProductIdType == "EAN" && p.IsActive)
                            ? c.ProductIds.Where(p => p.ProductIdType == "EAN" && p.IsActive).Select(p => p.ProductIdValue).Min()
                            : c.Ean) ?? "").ThenBy(c => c.Id),
                "contact" => query.OrderBy(c => !string.IsNullOrEmpty(c.Company.Email) ? c.Company.Email : c.Company.Website ?? "").ThenBy(c => c.Id),
                "contact-za" => query.OrderByDescending(c => !string.IsNullOrEmpty(c.Company.Email) ? c.Company.Email : c.Company.Website ?? "").ThenBy(c => c.Id),
                // DEF-181: Sort by the persisted computed numeric column instead of the raw text field.
                // Raw values are mixed format ("95%", "100", "0.69", etc.); lex sort produced unintuitive
                // output (e.g. DESC returned "95% > 87% > 100% > 1%"). The computed column normalizes
                // everything to a percent decimal. Non-numeric values land in NULL — default SQL Server
                // NULL placement: NULLs first in ASC, last in DESC, which surfaces populated values where
                // a sorting user is most likely looking.
                "cert-results" => query.OrderBy(c => c.CertificationResultsNumeric).ThenBy(c => c.Id),
                "cert-results-za" => query.OrderByDescending(c => c.CertificationResultsNumeric).ThenBy(c => c.Id),
                "pathway" => query.OrderBy(c => c.Pathway != null ? c.Pathway.Name : "").ThenBy(c => c.Id),
                "pathway-za" => query.OrderByDescending(c => c.Pathway != null ? c.Pathway.Name : "").ThenBy(c => c.Id),
                "cert-sub" => query.OrderBy(c => c.CertificationTypeSubcategories.Select(s => s.Name).Min() ?? "").ThenBy(c => c.Id),
                "cert-sub-za" => query.OrderByDescending(c => c.CertificationTypeSubcategories.Select(s => s.Name).Min() ?? "").ThenBy(c => c.Id),
                "company-url" => query.OrderBy(c => c.Company.Website ?? "").ThenBy(c => c.Id),
                "company-url-za" => query.OrderByDescending(c => c.Company.Website ?? "").ThenBy(c => c.Id),
                "created" => query.OrderByDescending(c => c.CreatedAt).ThenBy(c => c.Id),
                "modified" => query.OrderByDescending(c => c.UpdatedAt).ThenBy(c => c.Id),
                _ => query.OrderByDescending(c => c.EffectiveDate).ThenBy(c => c.Id)
            };
        }
    }

    public class InternalShareLinkRequest
    {
        public string? Search { get; set; }
        public string? CertType { get; set; }
        public string? ProductCategory { get; set; }
        public string? Company { get; set; }
        public string? Country { get; set; }
        public string? Tags { get; set; }
        public string? Status { get; set; }
        public string? Partner { get; set; }
        public string? SortBy { get; set; }
    }
}
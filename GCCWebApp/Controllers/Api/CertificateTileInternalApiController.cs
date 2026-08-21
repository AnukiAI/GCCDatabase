using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Extensions;
using GCCWebApp.Models;
using GCCWebApp.Search;
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
        private readonly GCCWebApp.Services.IUploadPathResolver _uploadPaths;

        public CertificateTileInternalApiController(
            ApplicationDbContext context,
            ILogger<CertificateTileInternalApiController> logger,
            GCCWebApp.Services.IUploadPathResolver uploadPaths)
        {
            _context = context;
            _logger = logger;
            _uploadPaths = uploadPaths;
        }

        // CR-187: search normalization moved to the shared Search.SearchNormalizer, matched against
        // persisted computed NormalizedX columns. The local NormalizeForSearch helper was removed.

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
            // DEF-249: default 0 (not 20) so `limit` is a real "not supplied" sentinel. With the old
            // default of 20, `actualLimit = limit > 0 ? limit : pageSize` (below) ALWAYS chose limit,
            // so `pageSize` was silently ignored on the page/pageSize path — every page-based call was
            // capped at 20 regardless of the requested pageSize ("20 of 35" symptom). The UI sends
            // page+pageSize=20 so it was unaffected; large-pageSize API callers got short pages.
            [FromQuery] int limit = 0)
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

                // CR-187: normalized, user-friendly search. Both sides reduced to a canonical form
                // (lowercase; trademark/punctuation/whitespace stripped) so "Product A" matches
                // "Product™ A", "product-a", "Product:  A", etc. Stored side = persisted computed
                // NormalizedX columns; input side = SearchNormalizer (same rule, cannot drift).
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var nq = SearchNormalizer.Normalize(search);
                    if (!string.IsNullOrEmpty(nq))
                    {
                        // DEF-366: the tag match is resolved as its own semi-join first rather than
                        // sitting in this OR, where SQL Server re-ran it per row of the joined
                        // result. This is the endpoint the internal tile view calls, and the one
                        // that took 7-8s per search after the 5,702-cert load. See
                        // Search/TagSearchMatch.cs.
                        var tagIds = await TagSearchMatch.MatchingCertificateIdsAsync(_context.Certificates, nq);
                        query = query.WhereMatchesSearch(c =>
                            c.NormalizedNumber!.Contains(nq) ||
                            c.CertificationType.NormalizedName!.Contains(nq) ||
                            c.Company.NormalizedName!.Contains(nq) ||
                            c.Product.NormalizedName!.Contains(nq) ||
                            c.Product.Category.NormalizedName!.Contains(nq) ||
                            (c.NormalizedLocation != null && c.NormalizedLocation.Contains(nq)) ||
                            (c.NormalizedCountry != null && c.NormalizedCountry.Contains(nq)), tagIds);
                    }
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
                    query = query.Where(c => c.Partners.Any(p => partners.Contains(p.Name)));
                }

                // Get total count (on the filtered, unordered query — Includes are no-ops for Count).
                var totalCount = await query.CountAsync();

                // Support both offset/limit and page/pageSize for compatibility
                var actualOffset = offset > 0 ? offset : (page - 1) * pageSize;
                // DEF-249: clamp to [1, 500]. Lower bound prevents a divide-by-zero in the page/
                // totalPages math below when a caller passes pageSize=0 (now possible since `limit`
                // defaults to 0). Upper bound caps any single page at 500 rows.
                var actualLimit = Math.Clamp(limit > 0 ? limit : pageSize, 1, 500);

                // DEF-250: paginate to IDs FIRST on an ID-only projection, THEN hydrate that page.
                // ApplySorting's key for ASIN/GTIN/EAN/Cert-Subcats is a correlated .Min() subquery;
                // combined with the AsSplitQuery + 4 collection Includes above, EF replicated that
                // ORDER BY onto every split query and re-joined ~1000 ProductIds for a big-seller
                // cert, hitting the 30s command timeout (intermittent 500s on the ASIN sort).
                // Selecting only c.Id makes EF drop the Includes (no split queries) so the .Min()
                // ORDER BY runs ONCE on a thin query; we then load full data for just this page.
                var pagedIds = await ApplySorting(query, sortBy)
                    .Skip(actualOffset)
                    .Take(actualLimit)
                    .Select(c => c.Id)
                    .ToListAsync();

                // Hydrate the page with full Includes but NO expensive ORDER BY (filter by the page's
                // IDs; AsSplitQuery stitches the collections by PK only now). The user-requested order
                // is restored in memory from pagedIds immediately after the fetch.
                var rawCertificates = await query
                    .Where(c => pagedIds.Contains(c.Id))
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
                        // DEF-261 (QC #157, client 6/10): the tag columns showed multiple tags in
                        // insertion order while the column sorted by an arbitrary FirstOrDefault tag, so
                        // the leading shown value wasn't the sort key — read as "External Tag not sorting."
                        // Order the displayed tags ascending so the lead value == the .Min() sort key,
                        // the same display-vs-sort alignment DEF-248 (ASIN/UPC/GTIN/EAN) and DEF-258
                        // (Cert Subcategories) applied. In-memory string only; no SQL/schema change.
                        ExternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).OrderBy(n => n)),
                        InternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name).OrderBy(n => n)),
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
                        // CR-171: internal (analyst/admin) see ALL associated partner names.
                        PartnerNames = c.Partners.Select(p => p.Name).ToList(),
                        c.CreatedAt,
                        c.UpdatedAt,
                        CertificationResults = c.CertificationResults ?? "",
                        PathwayName = c.Pathway != null ? c.Pathway.Name : "",
                        // QC #157 / DEF-248 pattern: this multi-value cell displayed subcats in
                        // insertion order while the cert-sub column sorts by .Min() (alpha-first),
                        // so the leading shown value didn't match the sort key — read as "Cert
                        // Subcats not sorting." Order ascending so the lead value == the sort key,
                        // same fix already applied to ASIN/UPC/GTIN/EAN above.
                        CertSubcategories = string.Join(", ", c.CertificationTypeSubcategories.Select(s => s.Name).OrderBy(n => n)),
                        CompanyUrl = c.Company.Website ?? "",
                        // CR-007: per-cert custom field values keyed by field id.
                        // Values are stored in TextValue regardless of FieldType (per CertificateFieldService).
                        CustomFieldValues = c.CustomFieldValues
                            .Where(cfv => cfv.Field != null && cfv.Field.IsVisible && !string.IsNullOrWhiteSpace(cfv.TextValue))
                            .Select(cfv => new { cfv.FieldId, cfv.TextValue })
                            .ToList()
                    })
                    .ToListAsync();

                // DEF-250: the hydrate query above filters by ID set and is NOT ordered, so restore
                // the sorted order computed in pagedIds (the page the user actually asked for).
                var idOrder = new Dictionary<int, int>(pagedIds.Count);
                for (int i = 0; i < pagedIds.Count; i++) idOrder[pagedIds[i]] = i;
                rawCertificates = rawCertificates.OrderBy(c => idOrder[c.Id]).ToList();

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
                    // Combine ProductIds list with legacy field fallback.
                    // QC #157 (client, 6/4/2026): multi-value ID cells displayed in insertion order
                    // while the column sorts on the cell's minimum — so the first value the eye
                    // lands on wasn't the value the row was sorted by, reading as "ASIN not sorting."
                    // DEF-259: order the displayed values with the SAME natural order the row sorts by
                    // (length-then-value, matching OrderByNaturalEmptiesLast in ApplySorting) so a
                    // numeric cell reads 2, 10, 100 (not lexical 10, 100, 2) and the leading shown
                    // value == the row's sort key. In-memory, post-materialization — no SQL change.
                    // Single-value cells and the legacy fallback are unaffected.
                    Asin = c.AsinList.Count > 0 ? string.Join(", ", c.AsinList.OrderBy(v => v.Length).ThenBy(v => v)) : c.LegacyAsin,
                    Upc = c.UpcList.Count > 0 ? string.Join(", ", c.UpcList.OrderBy(v => v.Length).ThenBy(v => v)) : c.LegacyUpc,
                    Gtin = c.GtinList.Count > 0 ? string.Join(", ", c.GtinList.OrderBy(v => v.Length).ThenBy(v => v)) : c.LegacyGtin,
                    Ean = c.EanList.Count > 0 ? string.Join(", ", c.EanList.OrderBy(v => v.Length).ThenBy(v => v)) : c.LegacyEan,
                    c.Isbn,
                    c.Sku,
                    c.Mpn,
                    c.AllProductIds,
                    c.PdfPath,
                    // CR-171: all associated partner names (internal view).
                    Partner = string.Join(", ", c.PartnerNames),
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
                // CR-187: normalized autocomplete — matches against persisted computed columns.
                var nq = SearchNormalizer.Normalize(term);
                var suggestions = new List<object>();

                // Search certificate numbers
                var certNumbers = await _context.Certificates
                    .Where(c => c.NormalizedNumber!.Contains(nq))
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
                    .Where(c => c.NormalizedName!.Contains(nq))
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
                    .Where(p => p.NormalizedName!.Contains(nq))
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
                    .Where(ct => ct.NormalizedName!.Contains(nq))
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
                    .Where(c => c.NormalizedLocation != null && c.NormalizedLocation.Contains(nq))
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

                    // CR-171: list all partner names from the M2M collection (the denormalized primary
                    // would miss co-partners on shared certs). Internal view shows every partner.
                    partners = (await _context.Certificates
                        .SelectMany(c => c.Partners)
                        .Select(p => p.Name)
                        .Distinct()
                        .ToListAsync())
                        .OrderBy(p => p)
                        .ToList(),

                    additionalTags = await _context.Tags
                        .Where(t => t.IsActive && (t.TagType == "external" || t.TagType == "internal"))
                        .Select(t => t.Name)
                        .Distinct()
                        .OrderBy(n => n)
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

                // DEF-286 (QC #198): resolve via the shared IUploadPathResolver (configured root →
                // App_Data → wwwroot legacy). Replaces the prior local App_Data/wwwroot logic so this
                // endpoint can never drift from the MVC download again (the DEF-262/264/279 bug class).
                if (!_uploadPaths.TryResolveExisting(certificate.CertificateFile, out var fullPath))
                {
                    _logger.LogWarning(
                        "Certificate PDF missing on disk: cert {Number} (id {Id}) references {Logical} but " +
                        "no file found (resolved {Physical}). Likely the 2026-06-15 upload-store incident; " +
                        "restore from backup.",
                        certificate.Number, id, certificate.CertificateFile,
                        string.IsNullOrEmpty(fullPath) ? "<malformed>" : fullPath);
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
            // QC #157 (client, 5/29/2026): empty/blank values must sort to the END regardless of
            // direction, "consistent across the application." Every TEXT column below goes through
            // OrderByTextEmptiesLast (blanks last in asc AND desc); Cert Results (numeric) goes through
            // OrderByNumericNullsLast. Date / Number / Status(enum) branches keep default ordering —
            // they have no "blank" state. The same two helpers are used by the external + public tile
            // queries so all three views behave identically. See Extensions/CertificateSortExtensions.cs.
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
                // DEF-165: Product Category was missing — frontend used to remap productCategory→product-type,
                // so the column appeared to sort but actually sorted by Product Type. Real sort key now wired.
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
                // DEF-181: ID-column sort keys mirror the display logic — fall back from `ProductIds`
                // (collection) to the legacy `Certificate.{Asin|Upc|Gtin|Ean}` column when no active
                // ProductIds exist. OrderByTextEmptiesLast coalesces a null/blank key to the end.
                // DEF-261 (QC #157, 6/10): ext-tag/int-tag moved off the DEF-165 arbitrary .FirstOrDefault()
                // to .Min() (alphabetical-first tag) so the row sort key matches the now-alphabetically-
                // ordered display cell — same "lead shown value == sort key" rule as DEF-248/DEF-258. The
                // DEF-165 .Min() timeout no longer applies: post-DEF-250 the sort runs on a thin ID-only
                // projection (cert-sub already uses .Min() here at the same cost). Min() over an empty tag
                // set returns NULL → coalesced to "" → still sorts blanks-last in both directions.
                "ext-tag" => query.OrderByTextEmptiesLast(c => c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).Min(), false).ThenBy(c => c.Id),
                "ext-tag-za" => query.OrderByTextEmptiesLast(c => c.Tags.Where(t => t.TagType == "external").Select(t => t.Name).Min(), true).ThenBy(c => c.Id),
                "int-tag" => query.OrderByTextEmptiesLast(c => c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name).Min(), false).ThenBy(c => c.Id),
                "int-tag-za" => query.OrderByTextEmptiesLast(c => c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name).Min(), true).ThenBy(c => c.Id),
                "partner" => query.OrderByTextEmptiesLast(c => c.Partner, false).ThenBy(c => c.Id),
                "partner-za" => query.OrderByTextEmptiesLast(c => c.Partner, true).ThenBy(c => c.Id),
                // DEF-260 (QC #157, supersedes the DEF-256/259 value-sort): the Cert List renders these
                // four columns as a COUNT badge (number of IDs on the cert), not the ID values, so the
                // client expects the column to sort by that visible count ("1000 should be at the top or
                // bottom"). Sort key = count of ACTIVE ProductIds of the type; when a cert has none but
                // carries a legacy single value, count it as 1 so it matches the badge (the display falls
                // back to the legacy column). Count 0 (empty cell, no badge) sorts LAST in both directions
                // via OrderByCountEmptiesLast. Cheap COUNT subquery; runs on the DEF-250 thin projection.
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
                // DEF-181: sort by the persisted computed numeric column (raw text is mixed-format
                // "95%"/"100"/"0.69"). QC #157 consistency: non-numeric → NULL → always LAST in both
                // directions (supersedes DEF-181's original NULLs-first-in-ASC), so populated values lead.
                "cert-results" => query.OrderByNumericNullsLast(c => c.CertificationResultsNumeric, false).ThenBy(c => c.Id),
                "cert-results-za" => query.OrderByNumericNullsLast(c => c.CertificationResultsNumeric, true).ThenBy(c => c.Id),
                "pathway" => query.OrderByTextEmptiesLast(c => c.Pathway != null ? c.Pathway.Name : "", false).ThenBy(c => c.Id),
                "pathway-za" => query.OrderByTextEmptiesLast(c => c.Pathway != null ? c.Pathway.Name : "", true).ThenBy(c => c.Id),
                "cert-sub" => query.OrderByTextEmptiesLast(c => c.CertificationTypeSubcategories.Select(s => s.Name).Min(), false).ThenBy(c => c.Id),
                "cert-sub-za" => query.OrderByTextEmptiesLast(c => c.CertificationTypeSubcategories.Select(s => s.Name).Min(), true).ThenBy(c => c.Id),
                "company-url" => query.OrderByTextEmptiesLast(c => c.Company.Website, false).ThenBy(c => c.Id),
                "company-url-za" => query.OrderByTextEmptiesLast(c => c.Company.Website, true).ThenBy(c => c.Id),
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
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
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.Product.ProductType)
                    .Include(c => c.Product.ProductSubcategory)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .Include(c => c.ProductIds)
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
                    var types = certType.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => types.Contains(c.CertificationType.Name));
                }

                // Apply product category filter
                if (!string.IsNullOrWhiteSpace(productCategory))
                {
                    var categories = productCategory.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => categories.Contains(c.Product.Category.Name));
                }

                // Apply company filter
                if (!string.IsNullOrWhiteSpace(company))
                {
                    var companies = company.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => companies.Contains(c.Company.Id.ToString()));
                }

                // Apply country filter
                if (!string.IsNullOrWhiteSpace(country))
                {
                    var countries = country.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => countries.Contains(c.Country));
                }

                // Apply tags filter
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    var tagsList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => c.Tags.Any(t => tagsList.Contains(t.Name)));
                }

                // Apply status filter (Internal view specific)
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => statuses.Contains(c.Status.ToString()));
                }

                // Apply partner filter (Internal view specific)
                if (!string.IsNullOrWhiteSpace(partner))
                {
                    var partners = partner.Split(',', StringSplitOptions.RemoveEmptyEntries);
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
                        ProductCategory = c.Product.Category.Name,
                        ProductType = c.Product.ProductType != null ? c.Product.ProductType.Name : "",
                        ProductSubcategory = c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : "",
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
                        c.UpdatedAt
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
                    c.EffectiveDate,
                    c.ExpirationDate,
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
                    c.UpdatedAt
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
                // Get hierarchical product categories
                var productCategoriesHierarchy = await _context.Set<ProductCategory>()
                    .Where(pc => pc.IsActive)
                    .Include(pc => pc.ProductTypes.Where(pt => pt.IsActive))
                        .ThenInclude(pt => pt.ProductSubcategories.Where(ps => ps.IsActive))
                    .OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name)
                    .Select(pc => new
                    {
                        id = pc.Id,
                        name = pc.Name,
                        level = 1,
                        types = pc.ProductTypes
                            .OrderBy(pt => pt.SortOrder).ThenBy(pt => pt.Name)
                            .Select(pt => new
                            {
                                id = pt.Id,
                                name = pt.Name,
                                level = 2,
                                subcategories = pt.ProductSubcategories
                                    .OrderBy(ps => ps.SortOrder).ThenBy(ps => ps.Name)
                                    .Select(ps => new
                                    {
                                        id = ps.Id,
                                        name = ps.Name,
                                        level = 3
                                    }).ToList()
                            }).ToList()
                    })
                    .ToListAsync();

                // Get unique certification type IDs from certificates
                var certTypeIds = await _context.Certificates
                    .Select(c => c.CertificationTypeId)
                    .Distinct()
                    .ToListAsync();

                // Get certification types with their hierarchical structure
                var allCertTypes = await _context.CertificationTypes
                    .Where(ct => certTypeIds.Contains(ct.Id) || (ct.Children.Any(child => certTypeIds.Contains(child.Id))))
                    .OrderBy(ct => ct.SortOrder)
                    .Select(ct => new { ct.Id, ct.Name, ct.Level, ct.ParentId, ct.SortOrder })
                    .ToListAsync();

                // Build hierarchical list (parent followed by children)
                var hierarchicalCertTypes = new List<dynamic>();
                foreach (var certType in allCertTypes.Where(ct => ct.ParentId == null))
                {
                    hierarchicalCertTypes.Add(new { name = certType.Name, level = certType.Level });
                    // Add children immediately after parent
                    var children = allCertTypes.Where(ct => ct.ParentId == certType.Id).OrderBy(ct => ct.SortOrder);
                    foreach (var child in children)
                    {
                        hierarchicalCertTypes.Add(new { name = child.Name, level = child.Level });
                    }
                }

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
                            certificateCount = c.Certificates.Count(cert =>
                                cert.Status == "active" ||
                                cert.Status == "expiring_soon" ||
                                cert.Status == "renewal_in_progress")
                        })
                        .Where(c => c.certificateCount > 0)
                        .OrderBy(c => c.name)
                        .Select(c => new { c.id, c.name })
                        .ToListAsync(),

                    countries = await _context.Certificates
                        .Where(c => c.Status == "active" || c.Status == "expiring_soon" || c.Status == "renewal_in_progress")
                        .Where(c => !string.IsNullOrEmpty(c.Country))
                        .Select(c => c.Country!)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToListAsync(),

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
            return sortBy?.ToLower() switch
            {
                "newest" => query.OrderByDescending(c => c.EffectiveDate),
                "oldest-effective" => query.OrderBy(c => c.EffectiveDate),
                "expiring" => query.OrderBy(c => c.ExpirationDate),
                "longest-validity" => query.OrderByDescending(c => 
                    EF.Functions.DateDiffDay(c.EffectiveDate, c.ExpirationDate)),
                "recently-issued" => query
                    .Where(c => c.EffectiveDate >= DateTime.Now.AddDays(-30))
                    .OrderByDescending(c => c.EffectiveDate),
                "cert-number-newest" => query.OrderByDescending(c => c.Number),
                "cert-number-oldest" => query.OrderBy(c => c.Number),
                "cert-type" => query.OrderBy(c => c.CertificationType.Name),
                "cert-type-za" => query.OrderByDescending(c => c.CertificationType.Name),
                "company-az" => query.OrderBy(c => c.Company.Name),
                "company-za" => query.OrderByDescending(c => c.Company.Name),
                "product-az" => query.OrderBy(c => c.Product.Name),
                "product-za" => query.OrderByDescending(c => c.Product.Name),
                "location-az" => query.OrderBy(c => c.Location),
                "location-za" => query.OrderByDescending(c => c.Location),
                "country" => query.OrderBy(c => c.Country),
                "country-za" => query.OrderByDescending(c => c.Country),
                "product-type" => query.OrderBy(c => c.Product.ProductType != null ? c.Product.ProductType.Name : ""),
                "status" => query.OrderBy(c => c.Status),
                "partner" => query.OrderBy(c => c.Partner ?? "None"),
                "created" => query.OrderByDescending(c => c.CreatedAt),
                "modified" => query.OrderByDescending(c => c.UpdatedAt),
                _ => query.OrderByDescending(c => c.EffectiveDate)
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
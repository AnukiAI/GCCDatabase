using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
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
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.Product.ProductType)
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
                    var partners = partner.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => partners.Contains(c.Partner ?? "None"));
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(c =>
                        c.Number.ToLower().Contains(search) ||
                        c.CertificationType.Name.ToLower().Contains(search) ||
                        c.Company.Name.ToLower().Contains(search) ||
                        c.Product.Name.ToLower().Contains(search) ||
                        c.Product.Category.Name.ToLower().Contains(search) ||
                        (c.Location != null && c.Location.ToLower().Contains(search)) ||
                        (c.Country != null && c.Country.ToLower().Contains(search)) ||
                        c.Tags.Any(t => t.Name.ToLower().Contains(search)));
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

                // Filter to only show active/valid certificates for external partners
                query = query.Where(c => c.Status == "active" || c.Status == "expiring_soon" || c.Status == "renewal_in_progress");

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
                        ProductCategory = c.Product.Category.Name,
                        ProductType = c.Product.ProductType != null ? c.Product.ProductType.Name : "",
                        ProductSubcategory = c.Product.ProductSubcategory != null ? c.Product.ProductSubcategory.Name : "",
                        c.Location,
                        c.Country,
                        ExternalTags = string.Join(", ", c.Tags.Select(t => t.Name)),
                        PdfPath = c.CertificateFile,
                        c.Partner
                        // Note: Status and internal fields are not exposed for external view
                    })
                    .ToListAsync();

                // Format certificate numbers to YY-NNNN format
                var certificates = rawCertificates.Select(c => new
                {
                    c.Id,
                    CertificateNumber = FormatCertificateNumber(c.RawNumber, c.EffectiveDate),
                    c.CertificationType,
                    c.EffectiveDate,
                    c.ExpirationDate,
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
                    c.Partner
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
                baseQuery = baseQuery.Where(c => c.Status == "active" || c.Status == "expiring_soon" || c.Status == "renewal_in_progress");

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
                                   (cert.Status == "active" || cert.Status == "expiring_soon" || cert.Status == "renewal_in_progress")))
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
                                   (cert.Status == "active" || cert.Status == "expiring_soon" || cert.Status == "renewal_in_progress")))
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
                baseQuery = baseQuery.Where(c => c.Status == "active" || c.Status == "expiring_soon" || c.Status == "renewal_in_progress");

                // Get hierarchical product categories from active certificates
                var activeCategoryIds = await baseQuery
                    .Select(c => c.Product.CategoryId)
                    .Distinct()
                    .ToListAsync();

                var productCategoriesHierarchy = await _context.Set<ProductCategory>()
                    .Where(pc => pc.IsActive && activeCategoryIds.Contains(pc.Id))
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

                // Get unique certification type IDs from active certificates
                var certTypeIds = await baseQuery
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
                    productCategories = await baseQuery
                        .Select(c => c.Product.Category.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync(),

                    productTypes = await baseQuery
                        .Where(c => c.Product.ProductType != null)
                        .Select(c => c.Product.ProductType!.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync(),

                    companies = await baseQuery
                        .Select(c => new { id = c.Company.Id, name = c.Company.Name })
                        .Distinct()
                        .OrderBy(c => c.name)
                        .ToListAsync(),

                    countries = await baseQuery
                        .Select(c => c.Country)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .OrderBy(c => c)
                        .ToListAsync(),

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
                if (certificate.Status != "active" && certificate.Status != "expiring_soon" && certificate.Status != "renewal_in_progress")
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
                "partner" => query.OrderBy(c => c.Partner ?? "None"),
                _ => query.OrderByDescending(c => c.EffectiveDate)
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
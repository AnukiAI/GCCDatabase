using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/certificate-tiles-external")]
    [Authorize(Policy = "ExternalAccess")]
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

                // Filter by partner access - External users should only see relevant certificates
                var userPartner = GetUserPartner(); // This would need to be implemented based on claims
                if (!string.IsNullOrEmpty(userPartner))
                {
                    query = query.Where(c => c.Partner == userPartner || c.Partner == null); // Show partner-specific and public certificates
                }

                // Apply partner filter if specified
                if (!string.IsNullOrWhiteSpace(partner))
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

                // Apply pagination
                var certificates = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.Id,
                        CertificateNumber = c.Number,
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

                // Apply partner filtering for autocomplete as well
                var userPartner = GetUserPartner();
                var baseQuery = _context.Certificates.AsQueryable();
                if (!string.IsNullOrEmpty(userPartner))
                {
                    baseQuery = baseQuery.Where(c => c.Partner == userPartner || c.Partner == null);
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

                // Search companies (only those with certificates visible to this partner)
                var companies = await _context.Companies
                    .Where(c => c.Name.ToLower().Contains(term) && 
                               c.Certificates.Any(cert => 
                                   (string.IsNullOrEmpty(userPartner) || cert.Partner == userPartner || cert.Partner == null) &&
                                   (cert.Status == "active" || cert.Status == "expiring_soon" || cert.Status == "renewal_in_progress")))
                    .Select(c => new { 
                        value = c.Name, 
                        label = c.Name, 
                        type = "Company" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(companies);

                // Search products (only those with certificates visible to this partner)
                var products = await _context.Products
                    .Where(p => p.Name.ToLower().Contains(term) && 
                               p.Certificates.Any(cert => 
                                   (string.IsNullOrEmpty(userPartner) || cert.Partner == userPartner || cert.Partner == null) &&
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
                var userPartner = GetUserPartner();
                var baseQuery = _context.Certificates.AsQueryable();
                
                // Filter certificates by partner access and active status
                if (!string.IsNullOrEmpty(userPartner))
                {
                    baseQuery = baseQuery.Where(c => c.Partner == userPartner || c.Partner == null);
                }
                baseQuery = baseQuery.Where(c => c.Status == "active" || c.Status == "expiring_soon" || c.Status == "renewal_in_progress");

                var filters = new
                {
                    certificationTypes = await baseQuery
                        .Select(c => c.CertificationType.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToListAsync(),

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

                // Check if user has access to this certificate
                var userPartner = GetUserPartner();
                if (!string.IsNullOrEmpty(userPartner) && 
                    certificate.Partner != userPartner && 
                    certificate.Partner != null)
                {
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
        /// Get the partner associated with the current user
        /// This would need to be implemented based on your authentication system
        /// </summary>
        private string? GetUserPartner()
        {
            // This is a placeholder implementation
            // You would extract the partner information from the user's claims/roles
            // For example:
            // return User.FindFirst("partner")?.Value;
            // or based on email domain or other logic
            
            // For now, return null to show all certificates
            // In actual implementation, you'd determine the partner from user context
            return User.FindFirst("partner")?.Value;
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
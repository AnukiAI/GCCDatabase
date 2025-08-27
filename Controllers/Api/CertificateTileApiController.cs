using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Linq.Dynamic.Core;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/certificate-tiles")]
    public class CertificateTileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificateTileApiController> _logger;

        public CertificateTileApiController(
            ApplicationDbContext context,
            ILogger<CertificateTileApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get certificates for tile view with lazy loading
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCertificateTiles(
            [FromQuery] string? search = null,
            [FromQuery] string? certType = null,
            [FromQuery] string? productCategory = null,
            [FromQuery] string? company = null,
            [FromQuery] string? country = null,
            [FromQuery] string? tags = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(c =>
                        c.CertificateNumber.ToLower().Contains(search) ||
                        c.CertificationType.ToLower().Contains(search) ||
                        c.Company.Name.ToLower().Contains(search) ||
                        c.Product.Name.ToLower().Contains(search) ||
                        c.Product.Category.ToLower().Contains(search) ||
                        c.Location.ToLower().Contains(search) ||
                        c.Country.ToLower().Contains(search) ||
                        c.ExternalTag.ToLower().Contains(search));
                }

                // Apply certification type filter
                if (!string.IsNullOrWhiteSpace(certType))
                {
                    var types = certType.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => types.Contains(c.CertificationType));
                }

                // Apply product category filter
                if (!string.IsNullOrWhiteSpace(productCategory))
                {
                    var categories = productCategory.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    query = query.Where(c => categories.Contains(c.Product.Category));
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
                    query = query.Where(c => tagsList.Contains(c.ExternalTag));
                }

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
                        c.CertificateNumber,
                        c.CertificationType,
                        c.EffectiveDate,
                        c.ExpirationDate,
                        CompanyName = c.Company.Name,
                        CompanyEmail = c.Company.ContactEmail,
                        CompanyUrl = c.Company.ContactUrl,
                        ProductName = c.Product.Name,
                        ProductCategory = c.Product.Category,
                        ProductType = c.Product.Type,
                        ProductSubcategory = c.Product.Subcategory,
                        c.Location,
                        c.Country,
                        c.ExternalTag,
                        c.PdfPath,
                        c.Status
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
                _logger.LogError(ex, "Error getting certificate tiles");
                return StatusCode(500, new { error = "An error occurred while retrieving certificates" });
            }
        }

        /// <summary>
        /// Search autocomplete suggestions
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
                    .Where(c => c.CertificateNumber.ToLower().Contains(term))
                    .Select(c => new { 
                        value = c.CertificateNumber, 
                        label = c.CertificateNumber, 
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
                var certTypes = await _context.Certificates
                    .Where(c => c.CertificationType.ToLower().Contains(term))
                    .Select(c => c.CertificationType)
                    .Distinct()
                    .Select(ct => new { 
                        value = ct, 
                        label = ct, 
                        type = "Certification Type" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(certTypes);

                // Search locations
                var locations = await _context.Certificates
                    .Where(c => c.Location.ToLower().Contains(term))
                    .Select(c => c.Location)
                    .Distinct()
                    .Select(l => new { 
                        value = l, 
                        label = l, 
                        type = "Location" 
                    })
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(locations);

                return Ok(new { suggestions = suggestions.Take(15) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting autocomplete suggestions");
                return Ok(new { suggestions = new List<object>() });
            }
        }

        /// <summary>
        /// Get filter options
        /// </summary>
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var filters = new
                {
                    certificationTypes = await _context.Certificates
                        .Select(c => c.CertificationType)
                        .Where(ct => !string.IsNullOrEmpty(ct))
                        .Distinct()
                        .OrderBy(ct => ct)
                        .ToListAsync(),

                    productCategories = await _context.Products
                        .Select(p => p.Category)
                        .Where(pc => !string.IsNullOrEmpty(pc))
                        .Distinct()
                        .OrderBy(pc => pc)
                        .ToListAsync(),

                    productTypes = await _context.Products
                        .Select(p => p.Type)
                        .Where(pt => !string.IsNullOrEmpty(pt))
                        .Distinct()
                        .OrderBy(pt => pt)
                        .ToListAsync(),

                    companies = await _context.Companies
                        .OrderBy(c => c.Name)
                        .Select(c => new { id = c.Id, name = c.Name })
                        .ToListAsync(),

                    countries = await _context.Certificates
                        .Select(c => c.Country)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .OrderBy(c => c)
                        .ToListAsync(),

                    tags = await _context.Certificates
                        .Select(c => c.ExternalTag)
                        .Where(t => !string.IsNullOrEmpty(t))
                        .Distinct()
                        .OrderBy(t => t)
                        .ToListAsync()
                };

                return Ok(filters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options");
                return StatusCode(500, new { error = "An error occurred while retrieving filter options" });
            }
        }

        /// <summary>
        /// Download certificate PDF
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

                if (string.IsNullOrEmpty(certificate.PdfPath))
                {
                    return NotFound(new { error = "Certificate PDF not available" });
                }

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", certificate.PdfPath.TrimStart('/'));
                
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound(new { error = "Certificate PDF file not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var fileName = $"Certificate_{certificate.CertificateNumber}.pdf";
                
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate {Id}", id);
                return StatusCode(500, new { error = "An error occurred while downloading the certificate" });
            }
        }

        /// <summary>
        /// Get shareable link for filtered results
        /// </summary>
        [HttpPost("share-link")]
        public IActionResult GetShareableLink([FromBody] ShareLinkRequest request)
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
                    
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                    queryParams.Add($"sort={Uri.EscapeDataString(request.SortBy)}");

                var baseUrl = $"{Request.Scheme}://{Request.Host}/Certificate/CertificateTileView";
                var shareableUrl = queryParams.Any() 
                    ? $"{baseUrl}?{string.Join("&", queryParams)}"
                    : baseUrl;

                return Ok(new { url = shareableUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating shareable link");
                return StatusCode(500, new { error = "An error occurred while generating the link" });
            }
        }

        private IQueryable<Certificate> ApplySorting(IQueryable<Certificate> query, string sortBy)
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
                "cert-number-newest" => query.OrderByDescending(c => c.CertificateNumber),
                "cert-number-oldest" => query.OrderBy(c => c.CertificateNumber),
                "cert-type" => query.OrderBy(c => c.CertificationType),
                "cert-type-za" => query.OrderByDescending(c => c.CertificationType),
                "company-az" => query.OrderBy(c => c.Company.Name),
                "company-za" => query.OrderByDescending(c => c.Company.Name),
                "product-az" => query.OrderBy(c => c.Product.Name),
                "product-za" => query.OrderByDescending(c => c.Product.Name),
                "location-az" => query.OrderBy(c => c.Location),
                "location-za" => query.OrderByDescending(c => c.Location),
                "country" => query.OrderBy(c => c.Country),
                "country-za" => query.OrderByDescending(c => c.Country),
                "product-type" => query.OrderBy(c => c.Product.Type),
                _ => query.OrderByDescending(c => c.EffectiveDate)
            };
        }
    }

    public class ShareLinkRequest
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
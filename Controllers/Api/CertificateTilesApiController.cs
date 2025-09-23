using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services;

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

        public CertificateTilesApiController(
            ILogger<CertificateTilesApiController> logger,
            ICertificateRepository certificateRepository,
            IUnitOfWork unitOfWork,
            ICertificatePdfService pdfService)
        {
            _logger = logger;
            _certificateRepository = certificateRepository;
            _unitOfWork = unitOfWork;
            _pdfService = pdfService;
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
            [FromQuery] string? sort = "newest",
            [FromQuery] string? sortBy = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? certTypes = null,
            [FromQuery] string? productCat = null,
            [FromQuery] string? countries = null,
            [FromQuery] string? certTags = null,
            [FromQuery] string? additional = null)
        {
            try
            {
                var finalSort = sort ?? sortBy ?? "newest";
                var finalSearchTerm = searchTerm ?? search;

                var filter = new CertificateSearchFilter
                {
                    SearchTerm = finalSearchTerm,
                    Status = "active", // Only show active certificates for public view
                    SortBy = MapSortBy(finalSort),
                    SortDescending = finalSort == "newest" || finalSort == "cert-number-newest",
                    Page = page,
                    PageSize = pageSize
                };

                // Parse filter arrays - handle multiple parameter names
                var certTypeFilter = certType ?? certTypes;
                if (!string.IsNullOrEmpty(certTypeFilter))
                {
                    filter.CertificationTypeNames = certTypeFilter.Split(',').ToList();
                }

                var countryFilter = country ?? countries;
                if (!string.IsNullOrEmpty(countryFilter))
                {
                    filter.Countries = countryFilter.Split(',').ToList();
                }

                var companyFilter = company;
                if (!string.IsNullOrEmpty(companyFilter))
                {
                    // Convert company IDs to filter
                    var companyIds = companyFilter.Split(',')
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
                        filter.CompanyNames = companyFilter.Split(',').ToList();
                    }
                }

                var productCategoryFilter = productCategory ?? productCat;
                if (!string.IsNullOrEmpty(productCategoryFilter))
                {
                    filter.ProductCategories = productCategoryFilter.Split(',').ToList();
                }

                var tagsFilter = tags ?? certTags;
                if (!string.IsNullOrEmpty(tagsFilter))
                {
                    filter.Tags = tagsFilter.Split(',').ToList();
                }

                if (!string.IsNullOrEmpty(additional))
                {
                    filter.AdditionalFilters = additional.Split(',').ToList();
                }

                var (certificates, totalCount) = await _certificateRepository.GetAdvancedFilteredCertificatesAsync(filter);

                var result = certificates.Select(c => new
                {
                    id = c.Id,
                    certificateNumber = c.Number,
                    certificationType = c.CertificationType?.Name ?? "Unknown",
                    companyName = c.Company?.Name ?? "Unknown",
                    productName = c.Product?.Name ?? "Unknown",
                    productType = c.Product?.Description,
                    effectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                    expirationDate = c.ExpirationDate.ToString("yyyy-MM-dd"),
                    location = c.Location,
                    country = c.Country,
                    status = c.Status,
                    isExpired = c.ExpirationDate < DateTime.UtcNow,
                    isExpiringSoon = c.ExpirationDate < DateTime.UtcNow.AddDays(30) && c.ExpirationDate > DateTime.UtcNow,
                    externalTags = GetCertificateTags(c)
                });

                var response = new
                {
                    success = true,
                    certificates = result,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    currentPage = page,
                    pageSize,
                    hasMore = page < Math.Ceiling(totalCount / (double)pageSize),
                    message = (string?)null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates for tile view");
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
                var certificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();
                var companies = await _unitOfWork.Companies.GetAllAsync();
                var countries = await _certificateRepository.GetDistinctCountriesAsync();

                var response = new
                {
                    certificationTypes = certificationTypes
                        .Where(ct => ct.IsActive)
                        .Select(ct => ct.Name)
                        .OrderBy(name => name)
                        .ToList(),
                    
                    companies = companies
                        .Where(c => c.IsActive)
                        .Select(c => new { id = c.Id, name = c.Name })
                        .OrderBy(c => c.name)
                        .ToList(),
                    
                    countries = countries
                        .Where(c => !string.IsNullOrEmpty(c))
                        .OrderBy(c => c)
                        .ToList()
                };

                return Ok(response);
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

                // Search companies
                var companies = await _unitOfWork.Companies.FindAsync(c => 
                    c.IsActive && c.Name.Contains(term));
                
                suggestions.AddRange(companies.Take(5).Select(c => new
                {
                    value = c.Name,
                    label = c.Name,
                    type = "Company"
                }));

                // Search products
                var products = await _unitOfWork.Products.FindAsync(p => 
                    p.IsActive && p.Name.Contains(term));
                
                suggestions.AddRange(products.Take(5).Select(p => new
                {
                    value = p.Name,
                    label = p.Name,
                    type = "Product"
                }));

                // Search certificate numbers
                var certificates = await _certificateRepository.FindAsync(c => 
                    c.Status == "active" && c.Number.Contains(term));
                
                suggestions.AddRange(certificates.Take(5).Select(c => new
                {
                    value = c.Number,
                    label = $"{c.Number} - {c.Company?.Name ?? "Unknown"}",
                    type = "Certificate"
                }));

                // Search certification types
                var certTypes = await _unitOfWork.CertificationTypes.FindAsync(ct => 
                    ct.IsActive && ct.Name.Contains(term));
                
                suggestions.AddRange(certTypes.Take(3).Select(ct => new
                {
                    value = ct.Name,
                    label = ct.Name,
                    type = "Certification Type"
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

                var fileName = $"Certificate_{certificate.Number}.pdf";
                var fileBytes = await _pdfService.GenerateCertificatePdfAsync(certificate);
                
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
                
                if (!string.IsNullOrEmpty(request.SortBy) && request.SortBy != "newest")
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
                "company-az" => "Company.Name",
                "company-za" => "Company.Name",
                "product-az" => "Product.Name",
                "product-za" => "Product.Name",
                "cert-number-newest" => "Number",
                "cert-number-oldest" => "Number",
                "cert-type" => "CertificationType.Name",
                "cert-type-za" => "CertificationType.Name",
                "location-az" => "Location",
                "location-za" => "Location",
                "country" => "Country",
                "country-za" => "Country",
                _ => "EffectiveDate"
            };
        }

        private string? GetCertificateTags(Certificate certificate)
        {
            var tags = new List<string>();
            
            if (certificate.ExpirationDate < DateTime.UtcNow.AddDays(30))
            {
                tags.Add("Expiring Soon");
            }
            
            if (!string.IsNullOrEmpty(certificate.Partner) && certificate.Partner != "None")
            {
                tags.Add(certificate.Partner);
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
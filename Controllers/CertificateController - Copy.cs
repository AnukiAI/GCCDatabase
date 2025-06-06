using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenCircleCertified.Models;
using GreenCircleCertified.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace GCCWebApp.Controllers
{
    /// <summary>
    /// Controller for Certificate functionality
    /// Handles both basic CRUD operations and advanced certificate list functionality
    /// </summary>
    public class CertificateController : Controller
    {
        private readonly ICertificateService _certificateService;
        private readonly IFilterService _filterService;
        private readonly ILogger<CertificateController> _logger;
        private readonly CertificateListSettings _settings;

        public CertificateController(
            ICertificateService certificateService,
            IFilterService filterService,
            ILogger<CertificateController> logger,
            CertificateListSettings settings)
        {
            _certificateService = certificateService;
            _filterService = filterService;
            _logger = logger;
            _settings = settings;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Main certificates listing page
        /// GET: /Certificate/Certificates
        /// </summary>
        public IActionResult Certificates()
        {
            return View();
        }

        /// <summary>
        /// Create new certificate form
        /// GET: /Certificate/Create
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Certificate details page (basic version)
        /// GET: /Certificate/Details/{id}
        /// </summary>
        public IActionResult Details(int id)
        {
            // Load the certificate by ID here
            return View();
        }

        /// <summary>
        /// Edit certificate form
        /// GET: /Certificate/Edit/{id}
        /// </summary>
        public IActionResult Edit(int id)
        {
            // Load the certificate by ID here
            return View();
        }

        /// <summary>
        /// Certificate tile view
        /// GET: /Certificate/CertificateTileView
        /// </summary>
        public IActionResult CertificateTileView()
        {
            return View();
        }

        /// <summary>
        /// Certificate single view
        /// GET: /Certificate/CertificateSingleView
        /// </summary>
        public IActionResult CertificateSingleView()
        {
            return View();
        }

        /// <summary>
        /// Certificate external view
        /// GET: /Certificate/CertificateExternalView
        /// </summary>
        public IActionResult CertificateExternalView()
        {
            return View();
        }

        /// <summary>
        /// Certificate internal view
        /// GET: /Certificate/CertificateInternalView
        /// </summary>
        public IActionResult CertificateInternalView()
        {
            return View();
        }

        /// <summary>
        /// Alternative tile view 1
        /// GET: /Certificate/CertificateTileViewAltView1
        /// </summary>
        public IActionResult CertificateTileViewAltView1()
        {
            return View();
        }

        /// <summary>
        /// Alternative tile view 2
        /// GET: /Certificate/CertificateTileViewAltView2
        /// </summary>
        public IActionResult CertificateTileViewAltView2()
        {
            return View();
        }

        #endregion

        #region Advanced Certificate List Functionality

        /// <summary>
        /// Main certificate list page with advanced filtering - Public access
        /// GET: /Certificate or /Certificate/Index or /Certificate/List
        /// </summary>
        [HttpGet]
        [Route("Certificate")]
        [Route("Certificate/Index")]
        [Route("Certificate/List")]
        public async Task<IActionResult> Index(CertificateListRequest request = null)
        {
            try
            {
                // Default request if none provided
                request ??= new CertificateListRequest();

                // Set user role based on authentication
                request.UserRole = GetCurrentUserRole();

                // Validate page size
                if (request.PageSize <= 0 || request.PageSize > _settings.MaxPageSize)
                {
                    request.PageSize = _settings.DefaultPageSize;
                }

                // Get certificates with applied filters
                var response = await _certificateService.GetCertificatesAsync(request);

                if (!response.Success)
                {
                    _logger.LogWarning("Failed to retrieve certificates: {Message}", response.Message);
                    return View("Error", new ErrorViewModel { Message = response.Message });
                }

                // Build view model
                var viewModel = new CertificateListViewModel
                {
                    Certificates = response.Certificates,
                    TotalResults = response.TotalCount,
                    CurrentSort = response.CurrentSort,
                    Filters = response.AppliedFilters,
                    Pagination = new PaginationModel
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = response.TotalCount
                    },
                    AvailableFilters = await _filterService.GetAvailableFiltersAsync(request.UserRole)
                };

                // Update sort options with current selection
                foreach (var sortOption in viewModel.SortOptions)
                {
                    sortOption.IsSelected = sortOption.Value == request.Sort;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate list");
                return View("Error", new ErrorViewModel { Message = "An error occurred while loading certificates." });
            }
        }

        /// <summary>
        /// AJAX endpoint for dynamic certificate loading
        /// POST: /Certificate/Search
        /// </summary>
        [HttpPost]
        [Route("Certificate/Search")]
        public async Task<IActionResult> Search([FromBody] CertificateListRequest request)
        {
            try
            {
                request.UserRole = GetCurrentUserRole();

                var response = await _certificateService.GetCertificatesAsync(request);

                if (!response.Success)
                {
                    return Json(new { success = false, message = response.Message });
                }

                // Return partial view for AJAX updates
                var viewModel = new CertificateListViewModel
                {
                    Certificates = response.Certificates,
                    TotalResults = response.TotalCount,
                    Pagination = new PaginationModel
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = response.TotalCount
                    }
                };

                var htmlContent = await this.RenderViewAsync("_CertificateGrid", viewModel.Certificates);

                return Json(new
                {
                    success = true,
                    html = htmlContent,
                    totalResults = response.TotalCount,
                    pagination = viewModel.Pagination.GetPaginationInfo()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching certificates");
                return Json(new { success = false, message = "Search failed. Please try again." });
            }
        }

        /// <summary>
        /// Advanced certificate details page with full data
        /// GET: /Certificate/DetailsAdvanced/{id}
        /// </summary>
        [HttpGet]
        [Route("Certificate/DetailsAdvanced/{id:int}")]
        public async Task<IActionResult> DetailsAdvanced(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var certificate = await _certificateService.GetCertificateDetailsAsync(id, userRole);

                if (certificate == null)
                {
                    return NotFound("Certificate not found.");
                }

                return View(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate details for ID: {CertificateId}", id);
                return View("Error", new ErrorViewModel { Message = "Certificate details could not be loaded." });
            }
        }

        /// <summary>
        /// Download certificate PDF
        /// GET: /Certificate/Download/{id}
        /// </summary>
        [HttpGet]
        [Route("Certificate/Download/{id:int}")]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var downloadModel = await _certificateService.GetCertificateDownloadAsync(id, userRole);

                if (downloadModel == null)
                {
                    return NotFound("Certificate not found or not available for download.");
                }

                // Log download activity
                await _certificateService.LogDownloadActivityAsync(downloadModel);

                return File(downloadModel.FileContent, downloadModel.ContentType, downloadModel.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate ID: {CertificateId}", id);
                return BadRequest("Download failed. Please try again.");
            }
        }

        /// <summary>
        /// Get available filter options for a specific filter type
        /// GET: /Certificate/FilterOptions/{filterType}
        /// </summary>
        [HttpGet]
        [Route("Certificate/FilterOptions/{filterType}")]
        public async Task<IActionResult> GetFilterOptions(string filterType, string searchTerm = "")
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var options = await _filterService.GetFilterOptionsAsync(filterType, searchTerm, userRole);

                return Json(new { success = true, options });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options for: {FilterType}", filterType);
                return Json(new { success = false, message = "Failed to load filter options." });
            }
        }

        /// <summary>
        /// Export certificates to CSV/Excel
        /// POST: /Certificate/Export
        /// </summary>
        [HttpPost]
        [Route("Certificate/Export")]
        [Authorize(Roles = "Analyst,Admin")] // Only authenticated users can export
        public async Task<IActionResult> Export([FromBody] CertificateListRequest request, string format = "csv")
        {
            try
            {
                request.UserRole = GetCurrentUserRole();
                request.PageSize = int.MaxValue; // Get all results for export

                var exportData = await _certificateService.ExportCertificatesAsync(request, format);

                if (exportData == null)
                {
                    return BadRequest("Export failed. Please try again.");
                }

                var contentType = format.ToLower() switch
                {
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "pdf" => "application/pdf",
                    _ => "text/csv"
                };

                var fileName = $"certificates_export_{DateTime.Now:yyyyMMdd_HHmmss}.{format}";

                return File(exportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates");
                return BadRequest("Export failed. Please try again.");
            }
        }

        /// <summary>
        /// Get certificate statistics for dashboard
        /// GET: /Certificate/Statistics
        /// </summary>
        [HttpGet]
        [Route("Certificate/Statistics")]
        [Authorize(Roles = "Analyst,Admin")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var stats = await _certificateService.GetCertificateStatisticsAsync(userRole);

                return Json(new { success = true, statistics = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate statistics");
                return Json(new { success = false, message = "Failed to load statistics." });
            }
        }

        /// <summary>
        /// Bulk operations on certificates (for admin users)
        /// POST: /Certificate/BulkAction
        /// </summary>
        [HttpPost]
        [Route("Certificate/BulkAction")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkAction([FromBody] BulkActionRequest request)
        {
            try
            {
                var result = await _certificateService.PerformBulkActionAsync(request);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message, affectedCount = result.AffectedCount });
                }

                return Json(new { success = false, message = result.Message, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action: {Action}", request.Action);
                return Json(new { success = false, message = "Bulk action failed. Please try again." });
            }
        }

        /// <summary>
        /// Autocomplete endpoint for search suggestions
        /// GET: /Certificate/Autocomplete
        /// </summary>
        [HttpGet]
        [Route("Certificate/Autocomplete")]
        public async Task<IActionResult> Autocomplete(string term, string type = "all")
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var suggestions = await _certificateService.GetSearchSuggestionsAsync(term, type, userRole);

                return Json(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting autocomplete suggestions for term: {Term}", term);
                return Json(new List<string>());
            }
        }

        /// <summary>
        /// Certificate validation endpoint
        /// GET: /Certificate/Validate/{certificateNumber}
        /// </summary>
        [HttpGet]
        [Route("Certificate/Validate/{certificateNumber}")]
        public async Task<IActionResult> ValidateCertificate(string certificateNumber)
        {
            try
            {
                var validation = await _certificateService.ValidateCertificateAsync(certificateNumber);

                return Json(new
                {
                    valid = validation.IsValid,
                    status = validation.Status.ToString(),
                    message = validation.Message,
                    expirationDate = validation.ExpirationDate,
                    isExpired = validation.IsExpired,
                    isExpiringSoon = validation.IsExpiringSoon
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating certificate: {CertificateNumber}", certificateNumber);
                return Json(new { valid = false, message = "Validation failed. Please try again." });
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Error handling page
        /// </summary>
        [Route("Certificate/Error")]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = "An error occurred while processing your request."
            });
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Determine current user role based on authentication context
        /// </summary>
        private UserRole GetCurrentUserRole()
        {
            if (User.IsInRole("Admin"))
                return UserRole.Admin;

            if (User.IsInRole("Analyst"))
                return UserRole.Analyst;

            if (User.IsInRole("External"))
                return UserRole.External;

            return UserRole.Public;
        }

        /// <summary>
        /// Get user email for logging purposes
        /// </summary>
        private string GetCurrentUserEmail()
        {
            return User.Identity?.Name ?? "anonymous";
        }

        /// <summary>
        /// Validate and sanitize search input
        /// </summary>
        private string SanitizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return string.Empty;

            // Remove potentially harmful characters
            searchTerm = searchTerm.Trim();
            searchTerm = System.Text.RegularExpressions.Regex.Replace(searchTerm, @"[<>""']", "");

            // Limit length
            if (searchTerm.Length > 100)
                searchTerm = searchTerm.Substring(0, 100);

            return searchTerm;
        }

        #endregion
    }

    #region Supporting Models

    /// <summary>
    /// Request model for bulk operations
    /// </summary>
    public class BulkActionRequest
    {
        public string Action { get; set; } // "delete", "export", "update_status", etc.
        public List<int> CertificateIds { get; set; } = new List<int>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result model for bulk operations
    /// </summary>
    public class BulkActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int AffectedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Certificate validation result model
    /// </summary>
    public class CertificateValidationResult
    {
        public bool IsValid { get; set; }
        public CertificateStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    /// <summary>
    /// Error view model
    /// </summary>
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string Message { get; set; } = "An error occurred while processing your request.";
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    #endregion

    public IActionResult CertificateTileView()
        {
            // Create some sample data for now
            var certificates = new List<dynamic>
    {
        new {
            Number = "24-1605",
            CompanyName = "ASSA ABLOY Access & Egress Hardware Group",
            ProductName = "900ET Exit Device Trim with EcoFlex®",
            Category = "Construction / Building Products",
            Location = "Berlin, CT, United States",
            EffectiveDate = "7/1/2024",
            ExpirationDate = "6/31/2027"
        }
    };

            return View(certificates);
        }
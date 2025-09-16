using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services;

namespace GCCWebApp.Controllers
{
    /// <summary>
    /// Controller for Certificate functionality
    /// Handles both basic CRUD operations and advanced certificate list functionality
    /// </summary>
    public class CertificateController : Controller
    {
        private readonly ILogger<CertificateController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICertificateRepository _certificateRepository;
        private readonly ICertificatePdfService _pdfService;
        private readonly ICertificateService _certificateService;

        public CertificateController(
            ILogger<CertificateController> logger,
            IUnitOfWork unitOfWork,
            ICertificateRepository certificateRepository,
            ICertificatePdfService pdfService,
            ICertificateService certificateService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _certificateRepository = certificateRepository;
            _pdfService = pdfService;
            _certificateService = certificateService;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Main certificates listing page
        /// GET: /Certificate/Certificates
        /// </summary>
        public async Task<IActionResult> Certificates(int page = 1, int pageSize = 10, string searchTerm = "", int? companyId = null, int? productId = null, string? partner = null)
        {
            try
            {
                var (certificates, totalCount) = await _certificateRepository.GetPagedCertificatesAsync(
                    page, pageSize, searchTerm, companyId, productId, partner);

                var model = new CertificateListViewModelSimple
                {
                    Certificates = certificates,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    SearchTerm = searchTerm,
                    CompanyId = companyId,
                    ProductId = productId,
                    Partner = partner
                };

                // Get companies and products for filter dropdowns
                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificates page");
                return View(new CertificateListViewModelSimple());
            }
        }

        /// <summary>
        /// Certificate tile view - Public access allowed
        /// GET: /Certificate/CertificateTileView
        /// </summary>
        [AllowAnonymous]
        public IActionResult CertificateTileView()
        {
            return View();
        }

        /// <summary>
        /// Certificate single view
        /// GET: /Certificate/CertificateTileViewExternal
        /// </summary>
        [AllowAnonymous]
        public IActionResult CertificateTileViewExternal()
        {
            return View();
        }

        /// <summary>
        /// Certificate internal view for Analysts and Admins
        /// GET: /Certificate/CertificateTileViewInternal
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public IActionResult CertificateTileViewInternal()
        {
            return View();
        }

        /// <summary>
        /// Certificate external view
        /// GET: /Certificate/CertificateExternalView
        /// </summary>
        public IActionResult CertificateExternalView()
        {
            // This will need to be updated to use real data
            return View();
        }

        /// <summary>
        /// Certificate internal view - Only for Admin and Analyst roles
        /// GET: /Certificate/CertificateInternalView
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public IActionResult CertificateInternalView()
        {
            // This will need to be updated to use real data
            return View();
        }

        /// <summary>
        /// Certificate single view by ID or certificate number - Public access allowed
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> CertificateSingleView(string id)
        {
            try
            {
                _logger.LogInformation("CertificateSingleView called with id: {Id}", id);
                
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("CertificateSingleView called with null or empty id");
                    return BadRequest("Certificate ID is required");
                }

                Certificate? certificate = null;

                // Try to parse as integer first (ID)
                if (int.TryParse(id, out int certificateId))
                {
                    _logger.LogInformation("Looking up certificate by ID: {CertificateId}", certificateId);
                    certificate = await _certificateRepository.GetByIdAsync(certificateId);
                }
                else
                {
                    _logger.LogInformation("Looking up certificate by number: {CertificateNumber}", id);
                    // Try to find by certificate number
                    certificate = await _certificateRepository.GetCertificateByNumberAsync(id);
                }

                if (certificate == null)
                {
                    _logger.LogWarning("Certificate not found: {Id}", id);
                    return NotFound($"Certificate {id} not found");
                }

                _logger.LogInformation("Certificate found: {CertificateNumber}, Company: {CompanyName}, Product: {ProductName}", 
                    certificate.Number, certificate.Company?.Name ?? "N/A", certificate.Product?.Name ?? "N/A");

                var model = new CertificateDetailsViewModelSimple
                {
                    Certificate = certificate,
                    CanEdit = CanUserEditCertificate(),
                    CanDelete = CanUserDeleteCertificate(),
                    ShowInternalData = IsInternalUser()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Get certificate details
        /// GET: /Certificate/Details/5
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound();
                }

                var model = new CertificateDetailsViewModelSimple
                {
                    Certificate = certificate,
                    CanEdit = CanUserEditCertificate(),
                    CanDelete = CanUserDeleteCertificate(),
                    ShowInternalData = IsInternalUser()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate details for ID: {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Create new certificate - GET
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new CertificateEditViewModel();
                
                // Load dropdown data
                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();
                ViewBag.CertificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create certificate form");
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Create new certificate - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Create(CertificateEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var certificate = new Certificate
                    {
                        Number = model.Number,
                        Status = model.Status ?? "active",
                        Partner = model.Partner ?? "None",
                        EffectiveDate = model.EffectiveDate,
                        ExpirationDate = model.ExpirationDate,
                        Location = model.Location,
                        Country = model.Country,
                        Asin = model.Asin,
                        Upc = model.Upc,
                        Gtin = model.Gtin,
                        Ean = model.Ean,
                        CompanyId = model.CompanyId,
                        ProductId = model.ProductId,
                        CertificationTypeId = model.CertificationTypeId,
                        CreatedBy = GetCurrentUserId(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _certificateRepository.AddAsync(certificate);
                    await _unitOfWork.CompleteAsync();

                    _logger.LogInformation("Certificate {Number} created by user {UserId}", certificate.Number, GetCurrentUserId());
                    
                    return RedirectToAction(nameof(Details), new { id = certificate.Id });
                }

                // If we got here, something failed, reload the form
                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();
                ViewBag.CertificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate");
                ModelState.AddModelError("", "An error occurred while creating the certificate");
                
                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();
                ViewBag.CertificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();

                return View(model);
            }
        }

        /// <summary>
        /// Edit certificate - GET
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound();
                }

                var model = new CertificateEditViewModel
                {
                    Id = certificate.Id,
                    Number = certificate.Number,
                    Status = certificate.Status,
                    EffectiveDate = certificate.EffectiveDate,
                    ExpirationDate = certificate.ExpirationDate,
                    Location = certificate.Location,
                    Country = certificate.Country,
                    Asin = certificate.Asin,
                    Upc = certificate.Upc,
                    Gtin = certificate.Gtin,
                    Ean = certificate.Ean,
                    CompanyId = certificate.CompanyId,
                    ProductId = certificate.ProductId,
                    CertificationTypeId = certificate.CertificationTypeId
                };

                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();
                ViewBag.CertificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for certificate ID: {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Edit certificate - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Edit(int id, CertificateEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var certificate = await _certificateRepository.GetByIdAsync(id);
                    if (certificate == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    certificate.Number = model.Number;
                    certificate.Status = model.Status ?? "active";
                    certificate.EffectiveDate = model.EffectiveDate;
                    certificate.ExpirationDate = model.ExpirationDate;
                    certificate.Location = model.Location;
                    certificate.Country = model.Country;
                    certificate.Asin = model.Asin;
                    certificate.Upc = model.Upc;
                    certificate.Gtin = model.Gtin;
                    certificate.Ean = model.Ean;
                    certificate.CompanyId = model.CompanyId;
                    certificate.ProductId = model.ProductId;
                    certificate.CertificationTypeId = model.CertificationTypeId;
                    certificate.UpdatedBy = GetCurrentUserId();
                    certificate.UpdatedAt = DateTime.UtcNow;

                    await _certificateRepository.UpdateAsync(certificate);
                    await _unitOfWork.CompleteAsync();

                    _logger.LogInformation("Certificate {Number} updated by user {UserId}", certificate.Number, GetCurrentUserId());

                    return RedirectToAction(nameof(Details), new { id = certificate.Id });
                }

                // If we got here, something failed, reload the form
                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();
                ViewBag.CertificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate ID: {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the certificate");

                ViewBag.Companies = await _unitOfWork.Companies.GetAllAsync();
                ViewBag.Products = await _unitOfWork.Products.GetAllAsync();
                ViewBag.CertificationTypes = await _unitOfWork.CertificationTypes.GetAllAsync();

                return View(model);
            }
        }

        /// <summary>
        /// Delete certificate confirmation - GET
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound();
                }

                var model = new CertificateDetailsViewModelSimple
                {
                    Certificate = certificate,
                    CanEdit = false,
                    CanDelete = true,
                    ShowInternalData = IsInternalUser()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation for certificate ID: {Id}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Delete certificate - POST
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound();
                }

                await _certificateRepository.RemoveAsync(certificate);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Certificate {Number} deleted by user {UserId}", certificate.Number, GetCurrentUserId());

                TempData["SuccessMessage"] = $"Certificate {certificate.Number} has been successfully deleted.";
                return RedirectToAction(nameof(Certificates));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the certificate.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// API endpoint for advanced certificate search
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchApi(
            string? searchTerm = null,
            int? companyId = null,
            int? productId = null,
            int? certificationTypeId = null,
            string? status = null,
            string? location = null,
            string? country = null,
            DateTime? effectiveDateFrom = null,
            DateTime? effectiveDateTo = null,
            DateTime? expirationDateFrom = null,
            DateTime? expirationDateTo = null,
            string? sortBy = "CreatedAt",
            bool sortDescending = true,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var filter = new CertificateSearchFilter
                {
                    SearchTerm = searchTerm,
                    CompanyId = companyId,
                    ProductId = productId,
                    CertificationTypeId = certificationTypeId,
                    Status = status,
                    Location = location,
                    Country = country,
                    EffectiveDateFrom = effectiveDateFrom,
                    EffectiveDateTo = effectiveDateTo,
                    ExpirationDateFrom = expirationDateFrom,
                    ExpirationDateTo = expirationDateTo,
                    SortBy = sortBy,
                    SortDescending = sortDescending,
                    Page = page,
                    PageSize = pageSize
                };

                var (certificates, totalCount) = await _certificateRepository.GetAdvancedFilteredCertificatesAsync(filter);

                var result = new
                {
                    certificates = certificates.Select(c => new
                    {
                        id = c.Id,
                        number = c.Number,
                        status = c.Status,
                        effectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd"),
                        expirationDate = c.ExpirationDate.ToString("yyyy-MM-dd"),
                        company = new { id = c.Company.Id, name = c.Company.Name },
                        product = new { id = c.Product.Id, name = c.Product.Name },
                        certificationType = new { id = c.CertificationType.Id, name = c.CertificationType.Name },
                        location = c.Location,
                        country = c.Country,
                        isExpired = c.ExpirationDate < DateTime.UtcNow,
                        isExpiringSoon = c.ExpirationDate < DateTime.UtcNow.AddDays(30) && c.ExpirationDate > DateTime.UtcNow
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        hasNextPage = page < Math.Ceiling(totalCount / (double)pageSize),
                        hasPreviousPage = page > 1
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchApi");
                return BadRequest("An error occurred while searching certificates");
            }
        }

        /// <summary>
        /// API endpoint for certificate statistics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate statistics");
                return BadRequest("An error occurred while retrieving statistics");
            }
        }

        #region Helper Methods

        private bool CanUserEditCertificate()
        {
            // Use proper authorization policies instead of session-based checking
            return User.HasClaim("Role", "Admin") || User.HasClaim("Role", "Analyst");
        }

        private bool CanUserDeleteCertificate()
        {
            // Use proper authorization policies instead of session-based checking
            return User.HasClaim("Role", "Admin");
        }

        private bool IsInternalUser()
        {
            // Use proper authorization policies instead of session-based checking
            return User.HasClaim("Role", "Admin") || User.HasClaim("Role", "Analyst");
        }

        private string GetUserRole()
        {
            if (User.HasClaim("Role", "Admin"))
                return "Admin";
            if (User.HasClaim("Role", "Analyst"))
                return "Analyst";
            if (User.HasClaim("Role", "External"))
                return "External";
            if (User.HasClaim("Role", "Internal"))
                return "Internal";
            
            return "Public"; // Default role for anonymous users
        }

        private int? GetCurrentUserId()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                    return null;

                // Try to get user ID from different claim types depending on authentication method
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ??
                                  User.FindFirst("sub") ??
                                  User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ??
                                  User.FindFirst("oid"); // Azure AD Object ID

                if (userIdClaim?.Value != null)
                {
                    // For Azure AD B2C, the ID might be a GUID, so we need to map it to our user table
                    // For cookie auth, it should be the actual user ID
                    if (int.TryParse(userIdClaim.Value, out int userId))
                    {
                        return userId;
                    }
                    
                    // If it's a GUID (Azure AD), we would need to look up the user in the database
                    // For now, return a default admin user ID for testing
                    _logger.LogWarning("Could not parse user ID {UserId} as integer, using default", userIdClaim.Value);
                    return 1; // Default to admin user
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
                return null;
            }
        }

        /// <summary>
        /// Download certificate PDF
        /// GET: /Certificate/Download/5
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound();
                }

                // Generate the actual certificate PDF
                var fileName = $"Certificate_{certificate.Number}.pdf";
                var fileBytes = await _pdfService.GenerateCertificatePdfAsync(certificate);
                
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate PDF for ID: {Id}", id);
                return BadRequest("Failed to download certificate");
            }
        }

        /// <summary>
        /// Download certificate PDF by certificate number
        /// GET: /Certificate/Download/24-1605
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> DownloadByNumber(string number)
        {
            try
            {
                var certificate = await _certificateRepository.GetCertificateByNumberAsync(number);
                if (certificate == null)
                {
                    return NotFound();
                }

                return await Download(certificate.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate PDF for number: {Number}", number);
                return BadRequest("Failed to download certificate");
            }
        }

        #endregion

        #region Export Operations

        /// <summary>
        /// Export certificates to various formats (CSV, Excel, PDF)
        /// GET: /Certificate/Export?format=csv&searchTerm=...&filters=...
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> ExportCertificates(
            string format = "csv",
            string? searchTerm = null,
            string? sort = null,
            [FromQuery] List<string>? certificationTypes = null,
            [FromQuery] List<string>? countries = null,
            [FromQuery] List<string>? statuses = null,
            DateTime? effectiveDateStart = null,
            DateTime? effectiveDateEnd = null)
        {
            try
            {
                var userRole = GetUserRole();
                
                var request = new CertificateListRequest
                {
                    Page = 1,
                    PageSize = 10000, // Large page size for export
                    Sort = sort ?? string.Empty,
                    UserRole = userRole,
                    Filters = new CertificateSearchFilters
                    {
                        SearchTerm = searchTerm ?? "",
                        CertificationTypes = certificationTypes ?? new List<string>(),
                        Countries = countries ?? new List<string>(),
                        Statuses = statuses ?? new List<string>(),
                        EffectiveDateStart = effectiveDateStart,
                        EffectiveDateEnd = effectiveDateEnd
                    }
                };

                var exportData = await _certificateService.ExportCertificatesAsync(request, format);
                
                if (exportData == null || exportData.Length == 0)
                {
                    return BadRequest("No data to export or export failed");
                }

                var fileName = $"certificates-{DateTime.Now:yyyy-MM-dd-HHmmss}";
                var contentType = format.ToLower() switch
                {
                    "csv" => "text/csv",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "pdf" => "application/pdf",
                    _ => "text/csv"
                };
                
                var fileExtension = format.ToLower() switch
                {
                    "csv" => "csv",
                    "excel" => "xlsx",
                    "pdf" => "pdf",
                    _ => "csv"
                };

                return File(exportData, contentType, $"{fileName}.{fileExtension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates in format: {Format}", format);
                return BadRequest("Failed to export certificates");
            }
        }

        #endregion

        #endregion
    }
}
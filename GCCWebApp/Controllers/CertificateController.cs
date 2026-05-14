using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services;
using System.IO;
using System.IO.Compression;

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
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IFileUploadService _fileUploadService;
        private readonly ICertificateFieldService _certificateFieldService;
        private readonly GCCWebApp.Services.Audit.IAuditService _auditService;
        private readonly ApplicationDbContext _context;
        private readonly IProductIdService _productIdService;

        public CertificateController(
            ILogger<CertificateController> logger,
            IUnitOfWork unitOfWork,
            ICertificateRepository certificateRepository,
            ICertificatePdfService pdfService,
            ICertificateService certificateService,
            IWebHostEnvironment hostingEnvironment,
            IFileUploadService fileUploadService,
            ICertificateFieldService certificateFieldService,
            GCCWebApp.Services.Audit.IAuditService auditService,
            ApplicationDbContext context,
            IProductIdService productIdService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _certificateRepository = certificateRepository;
            _pdfService = pdfService;
            _certificateService = certificateService;
            _hostingEnvironment = hostingEnvironment;
            _fileUploadService = fileUploadService;
            _certificateFieldService = certificateFieldService;
            _auditService = auditService;
            _context = context;
            _productIdService = productIdService;
        }

        #region Basic CRUD Operations

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
        /// Certificate tile view for External users (partners)
        /// GET: /Certificate/CertificateTileViewExternal
        /// </summary>
        [Authorize(Policy = "ExternalAccess")]
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
        /// Certificate external view - For external partners
        /// GET: /Certificate/CertificateExternalView/24-0366 (by certificate number)
        /// </summary>
        [Authorize(Policy = "ExternalAccess")]
        public async Task<IActionResult> CertificateExternalView(string id)
        {
            try
            {
                _logger.LogInformation("CertificateExternalView called with certificate number: {CertificateNumber}", id);

                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("CertificateExternalView called with null or empty certificate number");
                    return BadRequest("Certificate number is required");
                }

                // Only look up by certificate number (not integer ID)
                var certificate = await _certificateRepository.GetCertificateByNumberAsync(id);

                if (certificate == null)
                {
                    _logger.LogWarning("Certificate not found: {CertificateNumber}", id);
                    return NotFound($"Certificate {id} not found");
                }

                _logger.LogInformation("Certificate found: {CertificateNumber}, Company: {CompanyName}, Product: {ProductName}",
                    certificate.Number, certificate.Company?.Name ?? "N/A", certificate.Product?.Name ?? "N/A");

                // Generate thumbnail if missing (for imported certificates without thumbnails)
                await EnsureCertificateThumbnailAsync(certificate);

                var model = new CertificateDetailsViewModelSimple
                {
                    Certificate = certificate,
                    CanEdit = false,
                    CanDelete = false,
                    ShowInternalData = false
                };

                // Load custom field values for external view
                model.CustomFieldValues = await _certificateFieldService.GetFormattedFieldValuesAsync(certificate.Id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate {CertificateNumber}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Certificate internal view - Only for Admin and Analyst roles
        /// GET: /Certificate/CertificateInternalView/24-0366 (by certificate number)
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> CertificateInternalView(string id)
        {
            try
            {
                _logger.LogInformation("CertificateInternalView called with certificate number: {CertificateNumber}", id);

                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("CertificateInternalView called with null or empty certificate number");
                    return BadRequest("Certificate number is required");
                }

                // Only look up by certificate number (not integer ID)
                var certificate = await _certificateRepository.GetCertificateByNumberAsync(id);

                if (certificate == null)
                {
                    _logger.LogWarning("Certificate not found: {CertificateNumber}", id);
                    return NotFound($"Certificate {id} not found");
                }

                _logger.LogInformation("Certificate found: {CertificateNumber}, Company: {CompanyName}, Product: {ProductName}",
                    certificate.Number, certificate.Company?.Name ?? "N/A", certificate.Product?.Name ?? "N/A");

                // Generate thumbnail if missing (for imported certificates without thumbnails)
                await EnsureCertificateThumbnailAsync(certificate);

                var model = new CertificateDetailsViewModelSimple
                {
                    Certificate = certificate,
                    CanEdit = CanUserEditCertificate(),
                    CanDelete = CanUserDeleteCertificate(),
                    ShowInternalData = true
                };

                // Load custom field values for internal view
                model.CustomFieldValues = await _certificateFieldService.GetFormattedFieldValuesAsync(certificate.Id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate {CertificateNumber}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Certificate single view by certificate number - Public access allowed
        /// GET: /Certificate/CertificateSingleView/24-0366
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> CertificateSingleView(string id)
        {
            try
            {
                _logger.LogInformation("CertificateSingleView called with certificate number: {CertificateNumber}", id);

                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("CertificateSingleView called with null or empty certificate number");
                    return BadRequest("Certificate number is required");
                }

                // Only look up by certificate number (not integer ID) - per requirement
                _logger.LogInformation("Looking up certificate by number: {CertificateNumber}", id);
                var certificate = await _certificateRepository.GetCertificateByNumberAsync(id);

                if (certificate == null)
                {
                    _logger.LogWarning("Certificate not found: {CertificateNumber}", id);
                    return NotFound($"Certificate {id} not found");
                }

                // Block anonymous users from viewing non-public certificates via direct URL.
                // Public view should only show statuses in PublicStatuses with a valid expiration date.
                // Message intentionally distinguishes "not yet published" from "not found" so that a
                // recipient of a prematurely-shared link knows the URL is valid but the certificate
                // is not yet publishable. Cert numbers are sequential, so acknowledging existence
                // does not meaningfully aid enumeration.
                if (User.Identity?.IsAuthenticated != true)
                {
                    var isPublicStatus = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
                    var isExpired = certificate.ExpirationDate != default && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;

                    if (!isPublicStatus || isExpired)
                    {
                        _logger.LogWarning("Anonymous access denied for non-public certificate: {CertificateNumber}, Status: {Status}, Expired: {Expired}", id, certificate.Status, isExpired);
                        return NotFound($"Certificate {id} is not currently available for public viewing.");
                    }
                }

                _logger.LogInformation("Certificate found: {CertificateNumber}, Company: {CompanyName}, Product: {ProductName}",
                    certificate.Number, certificate.Company?.Name ?? "N/A", certificate.Product?.Name ?? "N/A");

                // Generate thumbnail if missing (for imported certificates without thumbnails)
                await EnsureCertificateThumbnailAsync(certificate);

                var model = new CertificateDetailsViewModelSimple
                {
                    Certificate = certificate,
                    CanEdit = CanUserEditCertificate(),
                    CanDelete = CanUserDeleteCertificate(),
                    ShowInternalData = IsInternalUser()
                };

                // Load custom field values for single view
                model.CustomFieldValues = await _certificateFieldService.GetFormattedFieldValuesAsync(certificate.Id);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading certificate {CertificateNumber}", id);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Public verification URL redirect - maps /Certificate/Verify/24-1605 to CertificateSingleView
        /// </summary>
        [AllowAnonymous]
        public IActionResult Verify(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Certificate number is required");

            return RedirectToAction("CertificateSingleView", new { id });
        }

        /// <summary>
        /// Create new certificate - GET
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Auto-generate the next certificate number
                var nextCertNumber = await _certificateRepository.GenerateNextCertificateNumberAsync();

                var model = new CertificateEditViewModel
                {
                    // Auto-populate certificate number (format: YY-XXXX)
                    Number = nextCertNumber,
                    // Dates left blank - analyst must enter manually (QC #133)
                    Status = "Draft" // Default status - aligned with DATA_VALIDATION_RULES.md
                };

                // Load dropdown data
                ViewBag.Companies = (await _unitOfWork.Companies.GetAllAsync()).Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.Name).ToList();
                var allProducts = await _unitOfWork.Products.GetAllAsync();
                ViewBag.Products = allProducts.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.Name).ToList();

                // Load product categories for dropdown
                ViewBag.ProductCategories = await _context.ProductCategories
                    .Where(pc => pc.IsActive).OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name).ToListAsync();

                // Load certification types - separate parent types and all types for cascading dropdown
                var allCertTypes = await _unitOfWork.CertificationTypes.GetAllAsync();
                ViewBag.ParentCertificationTypes = allCertTypes.Where(ct => ct.ParentId == null && ct.IsActive).OrderBy(ct => ct.Name).ToList();
                ViewBag.AllCertificationTypes = allCertTypes.Where(ct => ct.IsActive).Select(ct => new { id = ct.Id, name = ct.Name, parentId = ct.ParentId }).ToList();

                // Load statuses based on user role (database-driven)
                var userRole = GetUserRole();
                ViewBag.Statuses = CertificateStatusHelper.GetStatusesForRole(userRole);

                // Load tags filtered by type
                var allTags = await _unitOfWork.Tags.GetAllAsync();
                ViewBag.ExternalTags = allTags.Where(t => t.TagType == "external" && t.IsActive).ToList();
                ViewBag.InternalTags = allTags.Where(t => t.TagType == "internal" && t.IsActive).ToList();

                // Load pathways
                ViewBag.Pathways = await _context.Pathways.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

                // Load partners from database
                ViewBag.Partners = await _context.Partners.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

                // Load countries
                ViewBag.Countries = CountryList.Countries;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create certificate form");
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Test page for Create form - bypasses browser cache
        /// GET: /Certificate/Create2
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Create2()
        {
            try
            {
                // Auto-generate the next certificate number
                var nextCertNumber = await _certificateRepository.GenerateNextCertificateNumberAsync();

                var model = new CertificateEditViewModel
                {
                    // Auto-populate certificate number (format: YY-XXXX)
                    Number = nextCertNumber,
                    // Dates left blank - analyst must enter manually (QC #133)
                    Status = CertificateStatusHelper.StatusDraft // Default status
                };

                // Load dropdown data
                ViewBag.Companies = (await _unitOfWork.Companies.GetAllAsync()).Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.Name).ToList();
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
        public async Task<IActionResult> Create(
            CertificateEditViewModel model,
            IFormFile? certificateImage,
            IFormFile? certificatePdf,
            string? action,
            string? ExternalTags,
            string? InternalTags,
            string? SubcategoryIds)
        {
            try
            {
                // Debug logging for Product IDs
                _logger.LogInformation("Create POST received. ProductIds count: {Count}", model.ProductIds?.Count ?? 0);
                if (model.ProductIds != null && model.ProductIds.Any())
                {
                    foreach (var pid in model.ProductIds)
                    {
                        _logger.LogInformation("ProductId received: Type={Type}, Value={Value}, CertId={CertId}",
                            pid.ProductIdType, pid.ProductIdValue, pid.CertificateId);
                    }
                }
                else
                {
                    _logger.LogWarning("No ProductIds received in model");
                }

                // DEF-161: Create POST was persisting Product IDs with no format
                // validation — only the /api/productids POST path validated. Run
                // the same checks here so a cert created from /Certificate/Create
                // can't store malformed ASIN/UPC/GTIN/EAN/ISBN.
                if (model.ProductIds != null && model.ProductIds.Any())
                {
                    for (int i = 0; i < model.ProductIds.Count; i++)
                    {
                        var pid = model.ProductIds[i];
                        // Canonicalise so the regex sees uppercase form (matches client save path).
                        pid.ProductIdType = (pid.ProductIdType ?? string.Empty).Trim().ToUpperInvariant();
                        pid.ProductIdValue = Services.ProductIdService.NormalizeValueForType(pid.ProductIdType, pid.ProductIdValue ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(pid.ProductIdType) || string.IsNullOrWhiteSpace(pid.ProductIdValue))
                        {
                            ModelState.AddModelError($"ProductIds[{i}].ProductIdValue", "Product ID type and value are required.");
                            continue;
                        }
                        var ok = await _productIdService.ValidateProductIdAsync(pid.ProductIdType, pid.ProductIdValue);
                        if (!ok)
                        {
                            ModelState.AddModelError(
                                $"ProductIds[{i}].ProductIdValue",
                                $"Invalid {pid.ProductIdType} format: '{pid.ProductIdValue}'.");
                        }
                    }
                }

                // DEF-185: explicit non-nullable FK guards. `[Required]` on `int` does NOT
                // reject 0; without these, a scripted/tampered POST with 0 passes ModelState
                // and crashes at EF with an FK violation (same class as DEF-184).
                if (model.CompanyId <= 0)
                {
                    ModelState.AddModelError(nameof(model.CompanyId), "Company is required. Please select a company.");
                }
                if (model.ProductId <= 0)
                {
                    ModelState.AddModelError(nameof(model.ProductId), "Product is required. Please select a product.");
                }
                if (model.CertificationTypeId <= 0)
                {
                    ModelState.AddModelError(nameof(model.CertificationTypeId), "Certification Type is required. Please select a certification type.");
                }

                if (ModelState.IsValid)
                {
                    var certificate = new Certificate
                    {
                        // DEF-193: Number assigned atomically by AddWithGeneratedNumberAsync below;
                        // model.Number is treated as a preview only so two concurrent Create-form
                        // opens don't clash on the same value at save time.
                        Status = model.Status ?? "Draft",
                        Partner = model.Partner ?? "None",
                        CertificationResults = model.CertificationResults,
                        PathwayId = model.PathwayId,
                        EffectiveDate = model.EffectiveDate.Value,
                        ExpirationDate = model.ExpirationDate.Value,
                        Location = model.Location,
                        Country = model.Country,
                        Asin = model.Asin,
                        Upc = model.Upc,
                        Gtin = model.Gtin,
                        Ean = model.Ean,
                        CompanyId = model.CompanyId,
                        ProductId = model.ProductId,
                        ProductCategoryId = model.ProductCategoryId,
                        ProductTypeId = model.ProductTypeId,
                        ProductSubcategoryId = model.ProductSubcategoryId,
                        CertificationTypeId = model.CertificationTypeId,
                        CertificationTypeSubcategoryId = model.CertificationTypeSubcategoryId,
                        CreatedBy = GetCurrentUserId(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Handle certificate image upload (uses model.Number for the filename — this is
                    // the previewed cert number; if the user lost the race, certificate.Number
                    // assigned at save will differ. The DB CertificateFile column is the canonical
                    // link, so the filename mismatch is cosmetic, not functional.)
                    if (certificateImage != null && certificateImage.Length > 0)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(certificateImage, $"cert_{model.Number}");
                        if (uploadResult.IsSuccess)
                        {
                            certificate.CertificateImagePath = uploadResult.FilePath;
                            _logger.LogInformation("Certificate image uploaded successfully: {FilePath}", uploadResult.FilePath);
                        }
                        else
                        {
                            _logger.LogWarning("Certificate image upload failed: {Errors}", string.Join(", ", uploadResult.Errors));
                            ModelState.AddModelError("certificateImage", string.Join(", ", uploadResult.Errors));
                            await ReloadCreateFormDataAsync();
                            return View(model);
                        }
                    }

                    // Handle PDF certificate upload
                    if (certificatePdf != null && certificatePdf.Length > 0)
                    {
                        var uploadResult = await _fileUploadService.UploadCertificateAsync(certificatePdf, model.Number);
                        if (uploadResult.IsSuccess)
                        {
                            certificate.CertificateFile = uploadResult.FilePath;
                            _logger.LogInformation("Certificate PDF uploaded successfully: {FilePath}", uploadResult.FilePath);

                            // Auto-generate thumbnail from PDF first page
                            var thumbnailPath = await _fileUploadService.GeneratePdfThumbnailAsync(uploadResult.FilePath!, model.Number);
                            if (!string.IsNullOrEmpty(thumbnailPath))
                            {
                                certificate.CertificateImagePath = thumbnailPath;
                                _logger.LogInformation("PDF thumbnail generated: {ThumbnailPath}", thumbnailPath);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Certificate PDF upload failed: {Errors}", string.Join(", ", uploadResult.Errors));
                            ModelState.AddModelError("certificatePdf", string.Join(", ", uploadResult.Errors));
                            await ReloadCreateFormDataAsync();
                            return View(model);
                        }
                    }

                    // DEF-193: atomic save — generates the next cert Number and writes in one
                    // transaction; on UNIQUE-index conflict (race with another save), regenerates
                    // and retries up to 3 attempts. Replaces the prior `AddAsync + CompleteAsync`
                    // pair which trusted the form's pre-filled (race-prone) number.
                    await _certificateRepository.AddWithGeneratedNumberAsync(certificate);

                    // DEF-193 Option A: if the race forced a different number than the preview,
                    // rename the uploaded files so their filenames match the assigned cert Number.
                    // Functional path is the CertificateFile/CertificateImagePath columns; this is
                    // for filename-on-disk consistency for ops audits and manual file browsing.
                    if (!string.IsNullOrEmpty(model.Number)
                        && !string.Equals(certificate.Number, model.Number, StringComparison.Ordinal))
                    {
                        bool anyRenamed = false;
                        var newCertFile = TryRenameUploadedFile(certificate.CertificateFile, model.Number, certificate.Number);
                        if (newCertFile != null) { certificate.CertificateFile = newCertFile; anyRenamed = true; }
                        var newImagePath = TryRenameUploadedFile(certificate.CertificateImagePath, model.Number, certificate.Number);
                        if (newImagePath != null) { certificate.CertificateImagePath = newImagePath; anyRenamed = true; }
                        if (anyRenamed)
                        {
                            await _unitOfWork.CompleteAsync();
                            _logger.LogInformation("DEF-193: race-loser file rename from preview {Preview} to assigned {Number}", model.Number, certificate.Number);
                        }
                    }

                    // Handle Product IDs from Product ID Manager
                    if (model.ProductIds != null && model.ProductIds.Any())
                    {
                        var currentUserId = GetCurrentUserId();
                        var now = DateTime.UtcNow;
                        foreach (var productId in model.ProductIds)
                        {
                            productId.CertificateId = certificate.Id;
                            productId.CreatedBy = currentUserId;
                            productId.CreatedAt = now;
                            productId.UpdatedAt = now;
                            certificate.ProductIds.Add(productId);
                        }
                        await _unitOfWork.CompleteAsync();
                        _logger.LogInformation("Added {Count} product IDs to certificate {Number}", model.ProductIds.Count, certificate.Number);
                    }

                    // Handle Partners (multi-partner support)
                    if (!string.IsNullOrWhiteSpace(model.PartnerIds))
                    {
                        var partnerIdList = model.PartnerIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : 0)
                            .Where(id => id > 0)
                            .ToList();

                        if (partnerIdList.Any())
                        {
                            var partners = await _unitOfWork.Partners.FindAsync(p => partnerIdList.Contains(p.Id) && !p.IsDeleted);
                            var partnerList = partners.ToList();

                            foreach (var partner in partnerList)
                            {
                                certificate.Partners.Add(partner);
                            }
                            await _unitOfWork.CompleteAsync();
                            _logger.LogInformation("Added {Count} partners to certificate {Number}", partnerList.Count, certificate.Number);

                            // Also set the legacy Partner field to the first partner name for backward compatibility
                            if (partnerList.Any())
                            {
                                certificate.Partner = partnerList.First().Name;
                                await _unitOfWork.CompleteAsync();
                            }
                        }
                    }

                    // Handle Tags
                    if (!string.IsNullOrWhiteSpace(ExternalTags) || !string.IsNullOrWhiteSpace(InternalTags))
                    {
                        var tagIds = new List<int>();

                        if (!string.IsNullOrWhiteSpace(ExternalTags))
                        {
                            var externalTagIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(ExternalTags);
                            if (externalTagIds != null) tagIds.AddRange(externalTagIds);
                        }

                        if (!string.IsNullOrWhiteSpace(InternalTags))
                        {
                            var internalTagIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(InternalTags);
                            if (internalTagIds != null) tagIds.AddRange(internalTagIds);
                        }

                        // Load tags and associate with certificate
                        foreach (var tagId in tagIds)
                        {
                            var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
                            if (tag != null)
                            {
                                certificate.Tags.Add(tag);
                            }
                        }
                        await _unitOfWork.CompleteAsync();
                        _logger.LogInformation("Added {Count} tags to certificate {Number}", tagIds.Count, certificate.Number);
                    }

                    // Handle Subcategories (multi-select)
                    if (!string.IsNullOrWhiteSpace(SubcategoryIds))
                    {
                        var subcategoryIdList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(SubcategoryIds);
                        if (subcategoryIdList != null && subcategoryIdList.Any())
                        {
                            foreach (var subcatId in subcategoryIdList)
                            {
                                var subcategory = await _unitOfWork.CertificationTypes.GetByIdAsync(subcatId);
                                // Validate that subcategory belongs to the selected parent type
                                if (subcategory != null && subcategory.ParentId == certificate.CertificationTypeId)
                                {
                                    certificate.CertificationTypeSubcategories.Add(subcategory);
                                }
                            }
                            await _unitOfWork.CompleteAsync();
                            _logger.LogInformation("Added {Count} subcategories to certificate {Number}", subcategoryIdList.Count, certificate.Number);
                        }
                    }

                    // Update company email/website if provided
                    if (model.CompanyEmail != null || model.CompanyWebsite != null)
                    {
                        var company = await _context.Companies.FindAsync(model.CompanyId);
                        if (company != null)
                        {
                            if (model.CompanyEmail != null) company.Email = string.IsNullOrWhiteSpace(model.CompanyEmail) ? null : model.CompanyEmail;
                            if (model.CompanyWebsite != null) company.Website = string.IsNullOrWhiteSpace(model.CompanyWebsite) ? null : model.CompanyWebsite;
                            company.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }

                    // CR-015: cascade product-ID statuses on the freshly-created cert.
                    // Newly-created certs default to Draft so any product IDs added at creation start as Inactive.
                    await _productIdService.SyncProductIdStatusesForCertificateAsync(certificate.Id);

                    _logger.LogInformation("Certificate {Number} created by user {UserId}", certificate.Number, GetCurrentUserId());

                    // Handle "Save & Add Another" action
                    if (action == "saveAndAddAnother")
                    {
                        TempData["SuccessMessage"] = $"Certificate {certificate.Number} created successfully!";
                        return RedirectToAction(nameof(Create));
                    }

                    return RedirectToAction(nameof(CertificateInternalView), new { id = certificate.Number });
                }

                // If we got here, something failed, reload the form
                await ReloadCreateFormDataAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate");
                ModelState.AddModelError("", "An error occurred while creating the certificate");
                await ReloadCreateFormDataAsync();
                return View(model);
            }
        }

        /// <summary>
        /// DEF-193 Option A: rename an uploaded file on disk when the race forced a different
        /// cert Number than the form's preview, so the filename matches the assigned Number.
        /// Returns the new relative path on success, or null if no rename happened (file missing,
        /// filename didn't contain the old number, destination already exists, or I/O error).
        /// </summary>
        private string? TryRenameUploadedFile(string? relativePath, string oldNumber, string newNumber)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(oldNumber) || string.IsNullOrEmpty(newNumber))
                return null;
            try
            {
                var fileName = Path.GetFileName(relativePath);
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains(oldNumber, StringComparison.Ordinal))
                    return null;

                var newFileName = fileName.Replace(oldNumber, newNumber, StringComparison.Ordinal);
                var dir = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";
                var newRelative = string.IsNullOrEmpty(dir) ? newFileName : $"{dir}/{newFileName}";

                var webRoot = _hostingEnvironment.WebRootPath;
                var oldPhysical = Path.Combine(webRoot, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                var newPhysical = Path.Combine(webRoot, newRelative.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (!System.IO.File.Exists(oldPhysical)) return null;
                if (System.IO.File.Exists(newPhysical)) return null; // don't clobber

                System.IO.File.Move(oldPhysical, newPhysical);
                return newRelative;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DEF-193: failed to rename uploaded file {RelativePath} from {Old} to {New}", relativePath, oldNumber, newNumber);
                return null;
            }
        }

        /// <summary>
        /// Helper method to reload form data for Create view
        /// </summary>
        private async Task ReloadCreateFormDataAsync()
        {
            ViewBag.Companies = (await _unitOfWork.Companies.GetAllAsync()).Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.Name).ToList();
            var allProducts = await _unitOfWork.Products.GetAllAsync();
            ViewBag.Products = allProducts.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.Name).ToList();

            // Load product categories for dropdown
            ViewBag.ProductCategories = await _context.ProductCategories
                .Where(pc => pc.IsActive).OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name).ToListAsync();

            // Load certification types - separate parent types and all types for cascading dropdown
            var allCertTypes = await _unitOfWork.CertificationTypes.GetAllAsync();
            ViewBag.ParentCertificationTypes = allCertTypes.Where(ct => ct.ParentId == null && ct.IsActive).OrderBy(ct => ct.Name).ToList();
            ViewBag.AllCertificationTypes = allCertTypes.Where(ct => ct.IsActive).Select(ct => new { id = ct.Id, name = ct.Name, parentId = ct.ParentId }).ToList();

            // Load statuses based on user role (database-driven)
            var userRole = GetUserRole();
            ViewBag.Statuses = CertificateStatusHelper.GetStatusesForRole(userRole);

            var allTags = await _unitOfWork.Tags.GetAllAsync();
            ViewBag.ExternalTags = allTags.Where(t => t.TagType == "external" && t.IsActive).ToList();
            ViewBag.InternalTags = allTags.Where(t => t.TagType == "internal" && t.IsActive).ToList();

            ViewBag.Pathways = await _context.Pathways.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();
        }

        /// <summary>
        /// Edit certificate by certificate number - GET
        /// GET: /Certificate/Edit/24-0366
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Edit(string number)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(number))
                {
                    return BadRequest("Certificate number is required");
                }

                var certificate = await _certificateRepository.GetCertificateByNumberAsync(number);
                if (certificate == null)
                {
                    return NotFound($"Certificate {number} not found");
                }

                // Generate thumbnail if missing (for imported certificates without thumbnails)
                await EnsureCertificateThumbnailAsync(certificate);

                // Get partner IDs as comma-separated string for the view
                var partnerIds = certificate.Partners?.Any() == true
                    ? string.Join(",", certificate.Partners.Select(p => p.Id))
                    : "";

                var model = new CertificateEditViewModel
                {
                    Id = certificate.Id,
                    Number = certificate.Number,
                    Status = certificate.Status,
                    Partner = certificate.Partner,
                    PartnerIds = partnerIds,
                    CertificationResults = certificate.CertificationResults,
                    PathwayId = certificate.PathwayId,
                    EffectiveDate = certificate.EffectiveDate,
                    ExpirationDate = certificate.ExpirationDate,
                    Location = certificate.Location,
                    Country = certificate.Country,
                    Asin = certificate.Asin,
                    Upc = certificate.Upc,
                    Gtin = certificate.Gtin,
                    Ean = certificate.Ean,
                    CertificateImagePath = certificate.CertificateImagePath,
                    CompanyId = certificate.CompanyId,
                    ProductId = certificate.ProductId,
                    ProductCategoryId = certificate.ProductCategoryId,
                    ProductTypeId = certificate.ProductTypeId,
                    ProductSubcategoryId = certificate.ProductSubcategoryId,
                    CertificationTypeId = certificate.CertificationTypeId,
                    CertificationTypeSubcategoryId = certificate.CertificationTypeSubcategoryId,
                    // CR-018: Server-side pagination — only ship the first page to
                    // the Razor partial. The JS Product ID Manager fetches the same
                    // first page from /api/productids/certificate/{id}?page=1 on init
                    // and replaces tbody, so the user sees a fast initial paint instead
                    // of a multi-second server-side render of every row on certs with
                    // 1000+ ASINs. Total count and pagination math come from the API.
                    ProductIds = certificate.ProductIds?.OrderBy(p => p.DisplayOrder ?? int.MaxValue)
                        .ThenBy(p => p.ProductIdType)
                        .ThenBy(p => p.CreatedAt)
                        .Take(50)
                        .ToList() ?? new List<CertificateProductId>(),
                    CustomFieldValues = await _certificateFieldService.GetFieldValuesForCertificateAsync(certificate.Id) ?? new Dictionary<int, string>(),
                    CompanyEmail = certificate.Company?.Email,
                    CompanyWebsite = certificate.Company?.Website
                };

                // Load dropdown data
                ViewBag.Companies = (await _unitOfWork.Companies.GetAllAsync()).Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.Name).ToList();
                var allProducts = await _unitOfWork.Products.GetAllAsync();
                ViewBag.Products = allProducts.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.Name).ToList();

                // Load product categories for dropdown
                ViewBag.ProductCategories = await _context.ProductCategories
                    .Where(pc => pc.IsActive).OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name).ToListAsync();

                // Load certification types - separate parent types and all types for cascading dropdown
                var allCertTypes = await _unitOfWork.CertificationTypes.GetAllAsync();
                ViewBag.ParentCertificationTypes = allCertTypes.Where(ct => ct.ParentId == null && ct.IsActive).OrderBy(ct => ct.Name).ToList();
                ViewBag.AllCertificationTypes = allCertTypes.Where(ct => ct.IsActive).Select(ct => new { id = ct.Id, name = ct.Name, parentId = ct.ParentId }).ToList();

                // Load statuses based on user role (database-driven)
                var userRole = GetUserRole();
                ViewBag.Statuses = CertificateStatusHelper.GetStatusesForRole(userRole);

                // Load all tags filtered by type for the dropdown
                var allTags = await _unitOfWork.Tags.GetAllAsync();
                ViewBag.ExternalTags = allTags.Where(t => t.TagType == "external" && t.IsActive).ToList();
                ViewBag.InternalTags = allTags.Where(t => t.TagType == "internal" && t.IsActive).ToList();

                // Load pathways
                ViewBag.Pathways = await _context.Pathways.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

                // Load partners from database
                ViewBag.Partners = await _context.Partners.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

                // Load existing tags for this certificate
                var existingTags = certificate.Tags?.ToList() ?? new List<Tag>();
                ViewBag.ExistingExternalTags = existingTags.Where(t => t.TagType == "external").Select(t => new { t.Id, t.Name }).ToList();
                ViewBag.ExistingInternalTags = existingTags.Where(t => t.TagType == "internal").Select(t => new { t.Id, t.Name }).ToList();

                // Load existing subcategories for this certificate (multi-select)
                if (certificate.CertificationTypeSubcategories != null && certificate.CertificationTypeSubcategories.Any())
                {
                    ViewBag.ExistingSubcategories = certificate.CertificationTypeSubcategories.Select(s => new { s.Id, s.Name }).ToList();
                }
                else
                {
                    ViewBag.ExistingSubcategories = new List<object>();
                }

                // Load countries
                ViewBag.Countries = CountryList.Countries;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for certificate number: {Number}", number);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Edit certificate - POST
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Edit(
            int id,
            CertificateEditViewModel model,
            IFormFile? certificateImage,
            IFormFile? certificatePdf,
            string? ExternalTags,
            string? InternalTags,
            string? SubcategoryIds)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            try
            {
                // DEF-161: same pre-validation pass as Create POST. Edit was relying on
                // the /api/productids round-trip during the modal save, but a direct
                // form post (or a JS regression) can land lowercase ASIN here too.
                if (model.ProductIds != null && model.ProductIds.Any())
                {
                    for (int i = 0; i < model.ProductIds.Count; i++)
                    {
                        var pid = model.ProductIds[i];
                        pid.ProductIdType = (pid.ProductIdType ?? string.Empty).Trim().ToUpperInvariant();
                        pid.ProductIdValue = Services.ProductIdService.NormalizeValueForType(pid.ProductIdType, pid.ProductIdValue ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(pid.ProductIdType) || string.IsNullOrWhiteSpace(pid.ProductIdValue))
                        {
                            ModelState.AddModelError($"ProductIds[{i}].ProductIdValue", "Product ID type and value are required.");
                            continue;
                        }
                        var ok = await _productIdService.ValidateProductIdAsync(pid.ProductIdType, pid.ProductIdValue);
                        if (!ok)
                        {
                            ModelState.AddModelError(
                                $"ProductIds[{i}].ProductIdValue",
                                $"Invalid {pid.ProductIdType} format: '{pid.ProductIdValue}'.");
                        }
                    }
                }

                // DEF-185: explicit non-nullable FK guards. `[Required]` on `int` does NOT
                // reject 0; without these, a scripted/tampered POST with 0 passes ModelState
                // and crashes at EF with an FK violation (same class as DEF-184).
                if (model.CompanyId <= 0)
                {
                    ModelState.AddModelError(nameof(model.CompanyId), "Company is required. Please select a company.");
                }
                if (model.ProductId <= 0)
                {
                    ModelState.AddModelError(nameof(model.ProductId), "Product is required. Please select a product.");
                }
                if (model.CertificationTypeId <= 0)
                {
                    ModelState.AddModelError(nameof(model.CertificationTypeId), "Certification Type is required. Please select a certification type.");
                }

                if (ModelState.IsValid)
                {
                    var certificate = await _certificateRepository.GetByIdAsync(id);
                    if (certificate == null)
                    {
                        return NotFound();
                    }

                    // Update basic properties
                    certificate.Number = model.Number;
                    certificate.Status = model.Status ?? "Draft";
                    certificate.Partner = model.Partner;
                    certificate.CertificationResults = model.CertificationResults;
                    certificate.PathwayId = model.PathwayId;
                    certificate.EffectiveDate = model.EffectiveDate.Value;
                    certificate.ExpirationDate = model.ExpirationDate.Value;
                    certificate.Location = model.Location;
                    certificate.Country = model.Country;
                    certificate.Asin = model.Asin;
                    certificate.Upc = model.Upc;
                    certificate.Gtin = model.Gtin;
                    certificate.Ean = model.Ean;
                    certificate.CompanyId = model.CompanyId;
                    certificate.ProductId = model.ProductId;
                    certificate.ProductCategoryId = model.ProductCategoryId;
                    certificate.ProductTypeId = model.ProductTypeId;
                    certificate.ProductSubcategoryId = model.ProductSubcategoryId;
                    certificate.CertificationTypeId = model.CertificationTypeId;
                    certificate.CertificationTypeSubcategoryId = model.CertificationTypeSubcategoryId;
                    certificate.UpdatedBy = GetCurrentUserId();
                    certificate.UpdatedAt = DateTime.UtcNow;

                    // Handle certificate image upload
                    if (certificateImage != null && certificateImage.Length > 0)
                    {
                        var uploadResult = await _fileUploadService.UploadImageAsync(certificateImage, $"cert_{model.Number}");
                        if (uploadResult.IsSuccess)
                        {
                            certificate.CertificateImagePath = uploadResult.FilePath;
                            _logger.LogInformation("Certificate image uploaded successfully: {FilePath}", uploadResult.FilePath);
                        }
                        else
                        {
                            _logger.LogWarning("Certificate image upload failed: {Errors}", string.Join(", ", uploadResult.Errors));
                            ModelState.AddModelError("certificateImage", string.Join(", ", uploadResult.Errors));
                            await ReloadEditFormDataAsync(id);
                            return View(model);
                        }
                    }

                    // Handle PDF certificate upload
                    _logger.LogInformation("Edit POST - certificatePdf is {Status}, certificateImage is {ImgStatus}",
                        certificatePdf != null ? $"present ({certificatePdf.Length} bytes, name={certificatePdf.FileName})" : "NULL",
                        certificateImage != null ? $"present ({certificateImage.Length} bytes)" : "NULL");
                    _logger.LogInformation("Edit POST - Current CertificateFile={File}, CertificateImagePath={Image}",
                        certificate.CertificateFile, certificate.CertificateImagePath);

                    if (certificatePdf != null && certificatePdf.Length > 0)
                    {
                        var uploadResult = await _fileUploadService.UploadCertificateAsync(certificatePdf, model.Number);
                        if (uploadResult.IsSuccess)
                        {
                            certificate.CertificateFile = uploadResult.FilePath;
                            _logger.LogInformation("Certificate PDF uploaded successfully: {FilePath}", uploadResult.FilePath);

                            // Auto-generate thumbnail from PDF first page (replaces existing thumbnail)
                            var thumbnailPath = await _fileUploadService.GeneratePdfThumbnailAsync(uploadResult.FilePath!, model.Number);
                            if (!string.IsNullOrEmpty(thumbnailPath))
                            {
                                certificate.CertificateImagePath = thumbnailPath;
                                _logger.LogInformation("PDF thumbnail generated: {ThumbnailPath}", thumbnailPath);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Certificate PDF upload failed: {Errors}", string.Join(", ", uploadResult.Errors));
                            ModelState.AddModelError("certificatePdf", string.Join(", ", uploadResult.Errors));
                            await ReloadEditFormDataAsync(id);
                            return View(model);
                        }
                    }

                    // Handle Product IDs from Product ID Manager
                    // Merge instead of clear-and-readd so CreatedBy/CreatedAt are preserved
                    // on existing rows. Only stamp creator on truly new rows (Id == 0).
                    if (model.ProductIds != null && model.ProductIds.Any())
                    {
                        var currentUserId = GetCurrentUserId();
                        var now = DateTime.UtcNow;

                        var existingById = certificate.ProductIds.ToDictionary(p => p.Id);
                        var submittedIds = new HashSet<int>(
                            model.ProductIds.Where(p => p.Id > 0).Select(p => p.Id));

                        // Remove rows the user deleted in the UI
                        var toRemove = certificate.ProductIds
                            .Where(p => !submittedIds.Contains(p.Id))
                            .ToList();
                        foreach (var pid in toRemove)
                        {
                            certificate.ProductIds.Remove(pid);
                        }

                        foreach (var submitted in model.ProductIds)
                        {
                            if (submitted.Id > 0 && existingById.TryGetValue(submitted.Id, out var existing))
                            {
                                // Update editable fields only — preserve CreatedBy/CreatedAt
                                existing.ProductIdType = submitted.ProductIdType;
                                existing.ProductIdValue = submitted.ProductIdValue;
                                existing.Partner = submitted.Partner;
                                existing.ProductName = submitted.ProductName;
                                existing.ProductSku = submitted.ProductSku;
                                existing.Description = submitted.Description;
                                existing.UpdatedBy = currentUserId;
                                existing.UpdatedAt = now;
                            }
                            else
                            {
                                submitted.Id = 0;
                                submitted.CertificateId = certificate.Id;
                                submitted.CreatedBy = currentUserId;
                                submitted.CreatedAt = now;
                                submitted.UpdatedBy = currentUserId;
                                submitted.UpdatedAt = now;
                                certificate.ProductIds.Add(submitted);
                            }
                        }
                    }

                    // Handle Partners (multi-partner support)
                    if (!string.IsNullOrWhiteSpace(model.PartnerIds))
                    {
                        var partnerIdList = model.PartnerIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(pid => int.TryParse(pid.Trim(), out var parsed) ? parsed : 0)
                            .Where(pid => pid > 0)
                            .ToList();

                        // Clear existing partners
                        certificate.Partners.Clear();

                        if (partnerIdList.Any())
                        {
                            var partners = await _unitOfWork.Partners.FindAsync(p => partnerIdList.Contains(p.Id) && !p.IsDeleted);
                            var partnerList = partners.ToList();

                            foreach (var partner in partnerList)
                            {
                                certificate.Partners.Add(partner);
                            }

                            // Also set the legacy Partner field to the first partner name for backward compatibility
                            certificate.Partner = partnerList.Any() ? partnerList.First().Name : "None";

                            _logger.LogInformation("Updated {Count} partners for certificate {Number}", partnerList.Count, certificate.Number);
                        }
                        else
                        {
                            certificate.Partner = "None";
                        }
                    }
                    else
                    {
                        // No PartnerIds submitted - preserve the Partner dropdown value (set on line 865)
                        // Only clear to "None" if the Partner dropdown was also not set
                        certificate.Partners.Clear();
                        if (string.IsNullOrWhiteSpace(model.Partner) || model.Partner == "None")
                        {
                            certificate.Partner = "None";
                        }
                        // Otherwise keep certificate.Partner as set from model.Partner above
                    }

                    // Handle Tags (External and Internal)
                    if (!string.IsNullOrWhiteSpace(ExternalTags) || !string.IsNullOrWhiteSpace(InternalTags))
                    {
                        var tagIds = new List<int>();

                        if (!string.IsNullOrWhiteSpace(ExternalTags))
                        {
                            var externalTagIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(ExternalTags);
                            if (externalTagIds != null) tagIds.AddRange(externalTagIds);
                        }

                        if (!string.IsNullOrWhiteSpace(InternalTags))
                        {
                            var internalTagIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(InternalTags);
                            if (internalTagIds != null) tagIds.AddRange(internalTagIds);
                        }

                        // Clear existing tags
                        certificate.Tags.Clear();

                        // Associate new tags with certificate
                        foreach (var tagId in tagIds)
                        {
                            var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);
                            if (tag != null)
                            {
                                certificate.Tags.Add(tag);
                            }
                        }
                    }

                    // Handle Subcategories (multi-select)
                    certificate.CertificationTypeSubcategories.Clear();
                    if (!string.IsNullOrWhiteSpace(SubcategoryIds))
                    {
                        var subcategoryIdList = System.Text.Json.JsonSerializer.Deserialize<List<int>>(SubcategoryIds);
                        if (subcategoryIdList != null && subcategoryIdList.Any())
                        {
                            foreach (var subcatId in subcategoryIdList)
                            {
                                var subcategory = await _unitOfWork.CertificationTypes.GetByIdAsync(subcatId);
                                // Validate that subcategory belongs to the selected parent type
                                if (subcategory != null && subcategory.ParentId == certificate.CertificationTypeId)
                                {
                                    certificate.CertificationTypeSubcategories.Add(subcategory);
                                }
                            }
                            _logger.LogInformation("Updated {Count} subcategories for certificate {Number}", subcategoryIdList.Count, certificate.Number);
                        }
                    }

                    // Handle Custom Fields
                    if (model.CustomFieldValues != null && model.CustomFieldValues.Any())
                    {
                        // Get fields for validation
                        var fields = await _certificateFieldService.GetFieldsForCertificateTypeAsync(certificate.CertificationTypeId);

                        // Validate custom field values
                        var (isValid, validationErrors) = await _certificateFieldService.ValidateFieldValuesAsync(model.CustomFieldValues, fields);
                        if (!isValid)
                        {
                            _logger.LogWarning("Custom field validation failed for certificate {Number}: {Errors}",
                                certificate.Number, string.Join(", ", validationErrors));
                            foreach (var error in validationErrors)
                            {
                                ModelState.AddModelError("CustomFieldValues", error);
                            }
                            await ReloadEditFormDataAsync(id);
                            return View(model);
                        }

                        // Save custom field values
                        var fieldsSaved = await _certificateFieldService.SaveFieldValuesAsync(certificate.Id, model.CustomFieldValues);
                        if (!fieldsSaved)
                        {
                            _logger.LogWarning("Failed to save custom field values for certificate {Number}", certificate.Number);
                            ModelState.AddModelError("CustomFieldValues", "Failed to save custom field values");
                            await ReloadEditFormDataAsync(id);
                            return View(model);
                        }

                        _logger.LogInformation("Saved {Count} custom field values for certificate {Number}",
                            model.CustomFieldValues.Count, certificate.Number);
                    }

                    // DEF-171 Option A: save-time auto-expire enforcement. Runs before
                    // the repository.UpdateAsync below so the flip + audit row are part of the
                    // same SaveChanges transaction as the rest of the user's edits.
                    var actingUserId = GetCurrentUserId();
                    var actingUsername = User?.FindFirst("Name")?.Value
                        ?? User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                        ?? (actingUserId.HasValue ? $"User#{actingUserId}" : "System");
                    var (autoExpired, historyRow) = Services.CertificateExpirationService.TryApplySaveTimeAutoExpire(
                        certificate,
                        actingUserId,
                        actingUsername,
                        workflowStep: "Save-Time-Expiration");
                    if (autoExpired && historyRow != null)
                    {
                        _context.CertificateStatusHistory.Add(historyRow);
                    }

                    await _certificateRepository.UpdateAsync(certificate);
                    await _unitOfWork.CompleteAsync();

                    // CR-015: always cascade — picks up status changes AND newly added/replaced product IDs.
                    // Cheap when no-op (only writes rows where stored status drifts from derived value).
                    await _productIdService.SyncProductIdStatusesForCertificateAsync(certificate.Id);

                    // Update company email/website if provided
                    if (model.CompanyEmail != null || model.CompanyWebsite != null)
                    {
                        var companyToUpdate = await _context.Companies.FindAsync(model.CompanyId);
                        if (companyToUpdate != null)
                        {
                            if (model.CompanyEmail != null) companyToUpdate.Email = string.IsNullOrWhiteSpace(model.CompanyEmail) ? null : model.CompanyEmail;
                            if (model.CompanyWebsite != null) companyToUpdate.Website = string.IsNullOrWhiteSpace(model.CompanyWebsite) ? null : model.CompanyWebsite;
                            companyToUpdate.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }

                    _logger.LogInformation("Certificate {Number} updated by user {UserId}", certificate.Number, GetCurrentUserId());

                    TempData["SuccessMessage"] = $"Certificate {certificate.Number} updated successfully!";
                    return RedirectToAction(nameof(CertificateInternalView), new { id = certificate.Number });
                }

                // If we got here, something failed, reload the form
                await ReloadEditFormDataAsync(id);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate ID: {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the certificate");

                await ReloadEditFormDataAsync(id);
                return View(model);
            }
        }

        /// <summary>
        /// Helper method to reload form data for Edit view
        /// </summary>
        private async Task ReloadEditFormDataAsync(int certificateId)
        {
            ViewBag.Companies = (await _unitOfWork.Companies.GetAllAsync()).Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.Name).ToList();
            var allProducts = await _unitOfWork.Products.GetAllAsync();
            ViewBag.Products = allProducts.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.Name).ToList();

            // Load product categories for dropdown
            ViewBag.ProductCategories = await _context.ProductCategories
                .Where(pc => pc.IsActive).OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Name).ToListAsync();

            // Load certification types - separate parent types and all types for cascading dropdown
            var allCertTypes = await _unitOfWork.CertificationTypes.GetAllAsync();
            ViewBag.ParentCertificationTypes = allCertTypes.Where(ct => ct.ParentId == null && ct.IsActive).OrderBy(ct => ct.Name).ToList();
            ViewBag.AllCertificationTypes = allCertTypes.Where(ct => ct.IsActive).Select(ct => new { id = ct.Id, name = ct.Name, parentId = ct.ParentId }).ToList();

            // Load statuses based on user role (database-driven)
            var userRole = GetUserRole();
            ViewBag.Statuses = CertificateStatusHelper.GetStatusesForRole(userRole);

            var allTags = await _unitOfWork.Tags.GetAllAsync();
            ViewBag.ExternalTags = allTags.Where(t => t.TagType == "external" && t.IsActive).ToList();
            ViewBag.InternalTags = allTags.Where(t => t.TagType == "internal" && t.IsActive).ToList();

            ViewBag.Pathways = await _context.Pathways.Where(p => p.IsActive && !p.IsDeleted).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();

            // Load existing tags for the certificate
            var certificate = await _certificateRepository.GetByIdAsync(certificateId);
            if (certificate != null)
            {
                var existingTags = certificate.Tags?.ToList() ?? new List<Tag>();
                ViewBag.ExistingExternalTags = existingTags.Where(t => t.TagType == "external").Select(t => new { t.Id, t.Name }).ToList();
                ViewBag.ExistingInternalTags = existingTags.Where(t => t.TagType == "internal").Select(t => new { t.Id, t.Name }).ToList();
            }
        }

        /// <summary>
        /// Delete certificate confirmation - GET
        /// GET: /Certificate/Delete/24-0366
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string number)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(number))
                {
                    return BadRequest("Certificate number is required");
                }

                var certificate = await _certificateRepository.GetCertificateByNumberAsync(number);
                if (certificate == null)
                {
                    return NotFound($"Certificate {number} not found");
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
                _logger.LogError(ex, "Error loading delete confirmation for certificate number: {Number}", number);
                return RedirectToAction("Error", "Home");
            }
        }

        /// <summary>
        /// Delete certificate - POST
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(string number)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(number))
                {
                    return BadRequest(new { success = false, message = "Certificate number is required" });
                }

                var certificate = await _certificateRepository.GetCertificateByNumberAsync(number);
                if (certificate == null)
                {
                    return NotFound(new { success = false, message = $"Certificate {number} not found" });
                }

                var deletedNumber = certificate.Number;
                await _certificateRepository.RemoveAsync(certificate);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Certificate {Number} deleted by user {UserId}", deletedNumber, GetCurrentUserId());

                return Json(new { success = true, message = $"Certificate {deletedNumber} has been successfully deleted.", redirectUrl = Url.Action(nameof(CertificateTileViewInternal)) });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Error deleting certificate number: {Number}", number);
                return StatusCode(500, new { success = false, message = $"Error deleting certificate: {detail}" });
            }
        }

        /// <summary>
        /// API endpoint for advanced certificate search
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
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
                        isExpired = c.ExpirationDate < CertificateStatusHelper.BusinessToday,
                        isExpiringSoon = c.ExpirationDate <= CertificateStatusHelper.BusinessToday.AddDays(30) && c.ExpirationDate >= CertificateStatusHelper.BusinessToday
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
        [Authorize(Policy = "AnalystOrAdmin")]
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

        /// <summary>
        /// Generates a thumbnail from the PDF if one doesn't exist
        /// </summary>
        private async Task EnsureCertificateThumbnailAsync(Certificate certificate)
        {
            try
            {
                // Check if certificate has a PDF file but no thumbnail
                if (string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    return; // No PDF to generate thumbnail from
                }

                // Check if thumbnail already exists and file is valid
                if (!string.IsNullOrEmpty(certificate.CertificateImagePath))
                {
                    var existingThumbnailPath = Path.Combine(_hostingEnvironment.WebRootPath,
                        certificate.CertificateImagePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(existingThumbnailPath))
                    {
                        return; // Thumbnail already exists
                    }
                }

                // Check if PDF file exists
                var pdfPath = Path.Combine(_hostingEnvironment.WebRootPath,
                    certificate.CertificateFile.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (!System.IO.File.Exists(pdfPath))
                {
                    _logger.LogWarning("PDF file not found for thumbnail generation: {PdfPath}", pdfPath);
                    return;
                }

                // Generate thumbnail from PDF
                _logger.LogInformation("Generating missing thumbnail for certificate {Number}", certificate.Number);
                var thumbnailPath = await _fileUploadService.GeneratePdfThumbnailAsync(certificate.CertificateFile, certificate.Number);

                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    certificate.CertificateImagePath = thumbnailPath;
                    await _certificateRepository.UpdateAsync(certificate);
                    _logger.LogInformation("Thumbnail generated and saved for certificate {Number}: {ThumbnailPath}",
                        certificate.Number, thumbnailPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for certificate {Number}", certificate.Number);
                // Don't throw - thumbnail generation failure shouldn't break the view
            }
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

                    // If it's a GUID (Azure AD), we need to look up the user in the database
                    // SECURITY: Do NOT default to admin user - return null to prevent unauthorized access
                    _logger.LogError("Could not parse user ID {UserId} as integer. Azure AD integration requires database user lookup.", userIdClaim.Value);
                    return null; // Return null instead of defaulting to admin
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
                return null;
            }
        }

        private string GetCurrentUsername()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                    return "Anonymous";

                return User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ??
                      User.FindFirst("name")?.Value ??
                      User.FindFirst("preferred_username")?.Value ??
                      User.Identity?.Name ??
                      "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current username");
                return "Unknown";
            }
        }

        /// <summary>
        /// Download certificate PDF
        /// GET: /Certificate/Download/24-0366
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest("Certificate number is required");
                }

                var certificate = await _certificateRepository.GetCertificateByNumberAsync(id);
                if (certificate == null)
                {
                    return NotFound($"Certificate {id} not found");
                }

                // Block anonymous downloads for non-public certificates. Mirrors the gate in
                // CertificateSingleView so cert numbers (which are sequential) cannot be iterated
                // to harvest Draft / Hold / Suspended / Revoked / Expired PDFs.
                if (User.Identity?.IsAuthenticated != true)
                {
                    var isPublicStatus = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
                    var isExpired = certificate.ExpirationDate != default && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;

                    if (!isPublicStatus || isExpired)
                    {
                        _logger.LogWarning("Anonymous download denied for non-public certificate: {CertificateNumber}, Status: {Status}, Expired: {Expired}", id, certificate.Status, isExpired);
                        return NotFound($"Certificate {id} is not currently available for public download.");
                    }
                }

                // Check if an uploaded certificate file exists
                if (!string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    // Assuming CertificateFile contains a relative path or URL to the uploaded file
                    var filePath = certificate.CertificateFile;

                    // If it's a relative path, construct the full path
                    if (!filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove leading ~ if present
                        if (filePath.StartsWith("~"))
                        {
                            filePath = filePath.Substring(1);
                        }

                        // Remove leading / if present
                        if (filePath.StartsWith("/"))
                        {
                            filePath = filePath.Substring(1);
                        }

                        // Construct full path to wwwroot
                        var webRootPath = _hostingEnvironment.WebRootPath;
                        var fullPath = Path.Combine(webRootPath, filePath.Replace('/', Path.DirectorySeparatorChar));

                        if (System.IO.File.Exists(fullPath))
                        {
                            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                            var fileName = Path.GetFileName(fullPath);
                            var contentType = GetContentType(fullPath);

                            return File(fileBytes, contentType, fileName);
                        }
                    }
                }

                // No uploaded file → no download. Per business rule (2026-04-25): analysts must upload a real
                // PDF when creating a certificate; we never auto-generate a placeholder.
                // Public-safe message only — see QC #168. The internal-process detail (analyst upload workflow)
                // must never reach a public response body since this endpoint is [AllowAnonymous].
                return NotFound($"No certificate PDF has been uploaded for {id}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate for number: {Id}", id);
                return BadRequest("Failed to download certificate");
            }
        }

        /// <summary>
        /// View certificate file for inline display (preview)
        /// GET: /Certificate/ViewCertificateFile/5
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> ViewCertificateFile(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound();
                }

                // Block anonymous inline-view of non-public certificate files. Integer IDs are
                // trivially enumerable, so without this guard any visitor can scrape every cert PDF
                // regardless of Draft / Hold / Expired status.
                if (User.Identity?.IsAuthenticated != true)
                {
                    var isPublicStatus = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
                    var isExpired = certificate.ExpirationDate != default && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;

                    if (!isPublicStatus || isExpired)
                    {
                        _logger.LogWarning("Anonymous file view denied for non-public certificate Id: {CertificateId}, Status: {Status}, Expired: {Expired}", id, certificate.Status, isExpired);
                        return NotFound();
                    }
                }

                // Check if an uploaded certificate file exists
                if (!string.IsNullOrEmpty(certificate.CertificateFile))
                {
                    var filePath = certificate.CertificateFile;

                    // If it's a relative path, construct the full path
                    if (!filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove leading ~ if present
                        if (filePath.StartsWith("~"))
                        {
                            filePath = filePath.Substring(1);
                        }

                        // Remove leading / if present
                        if (filePath.StartsWith("/"))
                        {
                            filePath = filePath.Substring(1);
                        }

                        // Construct full path to wwwroot
                        var webRootPath = _hostingEnvironment.WebRootPath;
                        var fullPath = Path.Combine(webRootPath, filePath.Replace('/', Path.DirectorySeparatorChar));

                        if (System.IO.File.Exists(fullPath))
                        {
                            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                            var contentType = GetContentType(fullPath);

                            // Return file for inline display (not as attachment)
                            return File(fileBytes, contentType);
                        }
                    }
                }

                // If no uploaded file, return placeholder image
                var placeholderPath = Path.Combine(_hostingEnvironment.WebRootPath, "img", "certpreview.png");
                if (System.IO.File.Exists(placeholderPath))
                {
                    var placeholderBytes = await System.IO.File.ReadAllBytesAsync(placeholderPath);
                    return File(placeholderBytes, "image/png");
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing certificate file for ID: {Id}", id);
                return BadRequest("Failed to load certificate file");
            }
        }

        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".tiff" or ".tif" => "image/tiff",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
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

                // Explicit guard so this entry point cannot regress if Download() is ever refactored
                // to drop its own anonymous-status check. DEF-103 showed that trusting downstream
                // helpers to enforce the contract is fragile.
                if (User.Identity?.IsAuthenticated != true)
                {
                    var isPublicStatus = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
                    var isExpired = certificate.ExpirationDate != default && certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;

                    if (!isPublicStatus || isExpired)
                    {
                        _logger.LogWarning("Anonymous download-by-number denied for non-public certificate: {CertificateNumber}, Status: {Status}, Expired: {Expired}", number, certificate.Status, isExpired);
                        return NotFound();
                    }
                }

                return await Download(certificate.Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate PDF for number: {Number}", number);
                return BadRequest("Failed to download certificate");
            }
        }

        /// <summary>
        /// Download multiple selected certificates as a ZIP file
        /// POST: /Certificate/DownloadSelectedZip
        /// </summary>
        /// <param name="certificateIds">List of certificate IDs to download</param>
        /// <returns>ZIP file containing all selected certificate PDFs</returns>
        [HttpPost]
        [Authorize(Policy = "ExternalAccess")]
        public async Task<IActionResult> DownloadSelectedZip([FromBody] List<int> certificateIds)
        {
            if (certificateIds == null || certificateIds.Count == 0)
            {
                return BadRequest("No certificates selected for download");
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var zipFileName = $"certificates_{timestamp}.zip";

                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    int successCount = 0;
                    int failureCount = 0;

                    foreach (var certId in certificateIds)
                    {
                        try
                        {
                            var certificate = await _certificateRepository.GetByIdAsync(certId);
                            if (certificate == null)
                            {
                                _logger.LogWarning("Certificate with ID {CertId} not found, skipping", certId);
                                failureCount++;
                                continue;
                            }

                            // Get the PDF bytes (either from uploaded file or generated)
                            byte[] pdfBytes = null;
                            var fileName = $"Certificate_{certificate.Number}.pdf";

                            // Check if an uploaded certificate file exists
                            if (!string.IsNullOrEmpty(certificate.CertificateFile))
                            {
                                var filePath = certificate.CertificateFile;

                                // If it's a relative path, construct the full path
                                if (!filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Remove leading ~ or / if present
                                    if (filePath.StartsWith("~"))
                                    {
                                        filePath = filePath.Substring(1);
                                    }
                                    if (filePath.StartsWith("/"))
                                    {
                                        filePath = filePath.Substring(1);
                                    }

                                    // Construct full path to wwwroot
                                    var webRootPath = _hostingEnvironment.WebRootPath;
                                    var fullPath = Path.Combine(webRootPath, filePath.Replace('/', Path.DirectorySeparatorChar));

                                    if (System.IO.File.Exists(fullPath))
                                    {
                                        pdfBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                                    }
                                }
                            }

                            // If no uploaded file, try to generate PDF
                            if (pdfBytes == null)
                            {
                                try
                                {
                                    pdfBytes = await _pdfService.GenerateCertificatePdfAsync(certificate);
                                }
                                catch (Exception genEx)
                                {
                                    _logger.LogError(genEx, "Failed to generate PDF for certificate {CertNumber}", certificate.Number);
                                    failureCount++;
                                    continue;
                                }
                            }

                            // Add PDF to ZIP archive
                            if (pdfBytes != null && pdfBytes.Length > 0)
                            {
                                var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                                using var entryStream = entry.Open();
                                await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                                successCount++;
                            }
                            else
                            {
                                _logger.LogWarning("No PDF content available for certificate {CertNumber}", certificate.Number);
                                failureCount++;
                            }
                        }
                        catch (Exception certEx)
                        {
                            _logger.LogError(certEx, "Error processing certificate ID {CertId} for ZIP download", certId);
                            failureCount++;
                        }
                    }

                    // Log summary
                    _logger.LogInformation(
                        "ZIP download completed: {SuccessCount} succeeded, {FailureCount} failed out of {TotalCount} requested",
                        successCount, failureCount, certificateIds.Count);

                    // If no certificates were successfully added, return error
                    if (successCount == 0)
                    {
                        return BadRequest("None of the selected certificates have PDF files available");
                    }
                }

                memoryStream.Position = 0;
                var zipBytes = memoryStream.ToArray();

                // Return ZIP file
                return File(zipBytes, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ZIP file for selected certificates");
                return StatusCode(500, "Failed to create ZIP file. Please try again.");
            }
        }

        #endregion

        #region Export Operations

        /// <summary>
        /// Export certificates to CSV.
        /// GET: /Certificate/ExportCertificates?searchTerm=...&amp;filters=...
        /// </summary>
        /// <remarks>
        /// DEF-128: CSV is the only supported format per CLAUDE.md "CSV ONLY" rule. The legacy
        /// `format` query parameter is accepted for backwards-compatibility but ignored — output is
        /// always CSV regardless. Internal Tag column is included only for Admin/Analyst callers;
        /// External and anonymous callers receive a stripped CSV.
        /// </remarks>
        [AllowAnonymous]
        public async Task<IActionResult> ExportCertificates(
            string format = "csv",
            string? searchTerm = null,
            string? sort = null,
            string? ids = null,
            [FromQuery] List<string>? certificationTypes = null,
            [FromQuery] List<string>? countries = null,
            [FromQuery] List<string>? statuses = null,
            DateTime? effectiveDateStart = null,
            DateTime? effectiveDateEnd = null)
        {
            try
            {
                var userRole = GetUserRole();

                // DEF-128: only Admin/Analyst see Internal Tag column. Anon/External/Partner do not.
                var includeInternalTags = string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(userRole, "Analyst", StringComparison.OrdinalIgnoreCase);

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

                // If specific IDs provided, filter to only those certificates
                byte[]? exportData;
                if (!string.IsNullOrEmpty(ids))
                {
                    var certificateIds = ids.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList();
                    exportData = await _certificateService.ExportCertificatesByIdsAsync(certificateIds, includeInternalTags);
                }
                else
                {
                    exportData = await _certificateService.ExportCertificatesAsync(request, includeInternalTags);
                }

                if (exportData == null || exportData.Length == 0)
                {
                    return BadRequest("No data to export or export failed");
                }

                var fileName = $"certificates-{DateTime.Now:yyyy-MM-dd-HHmmss}.csv";
                return File(exportData, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates");
                return BadRequest("Failed to export certificates");
            }
        }

        #endregion

        #region Import Operations

        /// <summary>
        /// Import certificates from CSV/JSON data
        /// POST: /Certificate/Import
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Import([FromBody] List<CertificateImportDto> certificates)
        {
            if (certificates == null || !certificates.Any())
            {
                return Json(new { success = false, message = "No certificates provided" });
            }

            if (certificates.Count > 100)
            {
                return Json(new { success = false, message = "Maximum 100 certificates can be imported at once" });
            }

            var result = new
            {
                totalProcessed = 0,
                successCount = 0,
                failureCount = 0,
                successful = new List<string>(),
                failed = new List<object>()
            };

            var successfulCerts = new List<string>();
            var failedCerts = new List<object>();
            var userId = GetCurrentUserId();

            foreach (var certDto in certificates)
            {
                try
                {
                    // Validate required fields
                    if (string.IsNullOrEmpty(certDto.CompanyName) || string.IsNullOrEmpty(certDto.ProductName))
                    {
                        failedCerts.Add(new
                        {
                            index = certificates.IndexOf(certDto),
                            identifier = certDto.Number ?? "Unknown",
                            error = "Company Name and Product Name are required"
                        });
                        continue;
                    }

                    // Find or create company
                    var company = await _unitOfWork.Companies.FindFirstOrDefaultAsync(c => c.Name == certDto.CompanyName);
                    if (company == null)
                    {
                        company = new Company
                        {
                            Name = certDto.CompanyName,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Companies.AddAsync(company);
                        await _unitOfWork.CompleteAsync();
                    }

                    // Find or create product
                    var product = await _unitOfWork.Products.FindFirstOrDefaultAsync(p => p.Name == certDto.ProductName);
                    if (product == null)
                    {
                        product = new Product
                        {
                            Name = certDto.ProductName,
                            Description = certDto.Category ?? "",
                            IsActive = true,
                            CompanyId = company.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Products.AddAsync(product);
                        await _unitOfWork.CompleteAsync();
                    }

                    // Find certification type
                    var certType = await _unitOfWork.CertificationTypes.FindFirstOrDefaultAsync(ct => ct.Name == (certDto.CertificationType ?? "CERTIFIED SUSTAINABILITY FACTS"));
                    if (certType == null)
                    {
                        // Use first available certification type
                        var allCertTypes = await _unitOfWork.CertificationTypes.GetAllAsync();
                        certType = allCertTypes.FirstOrDefault();
                        if (certType == null)
                        {
                            failedCerts.Add(new
                            {
                                index = certificates.IndexOf(certDto),
                                identifier = certDto.Number ?? "Unknown",
                                error = "No certification types available in database"
                            });
                            continue;
                        }
                    }

                    // Parse dates
                    DateTime effectiveDate = DateTime.UtcNow;
                    DateTime expirationDate = DateTime.UtcNow.AddYears(1);

                    if (!string.IsNullOrEmpty(certDto.EffectiveDate))
                    {
                        DateTime.TryParse(certDto.EffectiveDate, out effectiveDate);
                    }

                    if (!string.IsNullOrEmpty(certDto.ExpirationDate))
                    {
                        DateTime.TryParse(certDto.ExpirationDate, out expirationDate);
                    }

                    // Generate certificate number if not provided
                    var certNumber = certDto.Number;
                    if (string.IsNullOrEmpty(certNumber))
                    {
                        certNumber = $"IMPORT-{DateTime.Now:yyyyMMdd}-{certificates.IndexOf(certDto) + 1:D4}";
                    }

                    // Check if certificate number already exists
                    var exists = await _certificateRepository.GetCertificateByNumberAsync(certNumber);
                    if (exists != null)
                    {
                        failedCerts.Add(new
                        {
                            index = certificates.IndexOf(certDto),
                            identifier = certNumber,
                            error = $"Certificate {certNumber} already exists"
                        });
                        continue;
                    }

                    // Create certificate
                    var certificate = new Certificate
                    {
                        Number = certNumber,
                        CompanyId = company.Id,
                        ProductId = product.Id,
                        CertificationTypeId = certType.Id,
                        Status = certDto.Status ?? "Active",
                        Partner = "None",
                        EffectiveDate = effectiveDate,
                        ExpirationDate = expirationDate,
                        Location = certDto.Location ?? "Unknown",
                        Country = certDto.Country ?? "Unknown",
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _certificateRepository.AddAsync(certificate);
                    await _unitOfWork.CompleteAsync();

                    // CR-015: cascade for any product IDs created on this import path.
                    await _productIdService.SyncProductIdStatusesForCertificateAsync(certificate.Id);

                    successfulCerts.Add(certNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import certificate at index {Index}", certificates.IndexOf(certDto));
                    failedCerts.Add(new
                    {
                        index = certificates.IndexOf(certDto),
                        identifier = certDto.Number ?? "Unknown",
                        error = "Failed to import certificate: " + ex.Message
                    });
                }
            }

            return Json(new
            {
                success = true,
                message = $"Successfully imported {successfulCerts.Count} of {certificates.Count} certificates",
                data = new
                {
                    totalProcessed = certificates.Count,
                    successCount = successfulCerts.Count,
                    failureCount = failedCerts.Count,
                    successful = successfulCerts,
                    failed = failedCerts
                }
            });
        }

        #endregion

        #region Custom Fields API

        /// <summary>
        /// Gets all custom fields (global, applies to all certificate types)
        /// Filters internal fields for External users
        /// GET: /Certificate/GetCustomFields?certificateTypeId={id}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetCustomFields(int certificateTypeId)
        {
            try
            {
                // Determine user role for field visibility filtering
                // RoleVisibility: Public (all), External (External+), Internal (Analyst/Admin only)
                string userRole = "Public";
                if (User.IsInRole("Admin") || User.IsInRole("Analyst"))
                {
                    userRole = "Admin"; // Admin and Analyst see all fields
                }
                else if (User.IsInRole("External"))
                {
                    userRole = "External"; // External sees Public + External fields
                }

                var fields = await _certificateFieldService.GetFieldsForRoleAsync(userRole);
                return Json(new { success = true, data = fields });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom fields");
                return Json(new { success = false, message = "Error loading custom fields" });
            }
        }

        /// <summary>
        /// Gets custom field values for a specific certificate
        /// GET: /Certificate/GetCustomFieldValues?certificateId={id}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetCustomFieldValues(int certificateId)
        {
            try
            {
                var values = await _certificateFieldService.GetFieldValuesForCertificateAsync(certificateId);
                return Json(new { success = true, data = values });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom field values for certificate {CertificateId}", certificateId);
                return Json(new { success = false, message = "Error loading field values" });
            }
        }

        /// <summary>
        /// Saves custom field values for a certificate
        /// POST: /Certificate/SaveCustomFieldValues
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AnalystOrAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCustomFieldValues([FromBody] SaveCustomFieldValuesRequest request)
        {
            try
            {
                if (request == null || request.CertificateId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                // Get fields for validation
                var certificate = await _certificateRepository.GetByIdAsync(request.CertificateId);
                if (certificate == null)
                {
                    return Json(new { success = false, message = "Certificate not found" });
                }

                var fields = await _certificateFieldService.GetFieldsForCertificateTypeAsync(certificate.CertificationTypeId);

                // Validate field values
                var (isValid, errors) = await _certificateFieldService.ValidateFieldValuesAsync(request.FieldValues, fields);
                if (!isValid)
                {
                    return Json(new { success = false, message = "Validation failed", errors });
                }

                // Save values
                var saved = await _certificateFieldService.SaveFieldValuesAsync(request.CertificateId, request.FieldValues);

                if (saved)
                {
                    return Json(new { success = true, message = "Field values saved successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to save field values" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving custom field values for certificate {CertificateId}", request?.CertificateId);
                return Json(new { success = false, message = "Error saving field values" });
            }
        }

        #endregion

        #region Product Type/Subcategory Cascading Dropdowns

        /// <summary>
        /// Gets all product categories for the Level 1 dropdown
        /// GET: /Certificate/GetProductCategories
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetProductCategories()
        {
            try
            {
                var allCategories = await _context.ProductCategories
                    .Where(pc => pc.IsActive)
                    .OrderBy(pc => pc.SortOrder)
                    .ThenBy(pc => pc.Name)
                    .Select(pc => new { id = pc.Id, name = pc.Name })
                    .ToListAsync();

                return Json(new { success = true, data = allCategories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product categories");
                return Json(new { success = false, message = "Error loading product categories" });
            }
        }

        /// <summary>
        /// Gets product types for cascading dropdown, optionally filtered by category
        /// GET: /Certificate/GetProductTypes?categoryId={id}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetProductTypes(int? categoryId = null)
        {
            try
            {
                var query = _context.ProductTypes
                    .Where(pt => pt.IsActive && pt.ParentId == null); // Only top-level types

                if (categoryId.HasValue)
                {
                    query = query.Where(pt => pt.CategoryId == categoryId.Value);
                }

                var productTypes = await query
                    .OrderBy(pt => pt.SortOrder)
                    .ThenBy(pt => pt.Name)
                    .Select(pt => new { id = pt.Id, name = pt.Name, categoryId = pt.CategoryId })
                    .ToListAsync();

                return Json(new { success = true, data = productTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product types");
                return Json(new { success = false, message = "Error loading product types" });
            }
        }

        /// <summary>
        /// Gets product subcategories (children of a product type) from ProductTypes table
        /// GET: /Certificate/GetProductSubcategories?productTypeId={id}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetProductSubcategories(int productTypeId)
        {
            try
            {
                // Query ProductTypes children (ParentId = productTypeId) instead of empty ProductSubcategories table
                var subcategories = await _context.ProductTypes
                    .Where(pt => pt.ParentId == productTypeId && pt.IsActive)
                    .OrderBy(pt => pt.SortOrder)
                    .ThenBy(pt => pt.Name)
                    .Select(pt => new { id = pt.Id, name = pt.Name })
                    .ToListAsync();

                return Json(new { success = true, data = subcategories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product subcategories for type {ProductTypeId}", productTypeId);
                return Json(new { success = false, message = "Error loading product subcategories" });
            }
        }

        /// <summary>
        /// Gets products filtered by product type and/or subcategory
        /// GET: /Certificate/GetProductsByType?productTypeId={typeId}&subcategoryId={subId}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetProductsByType(int? productTypeId, int? subcategoryId)
        {
            try
            {
                var allProducts = await _unitOfWork.Products.GetAllAsync();
                var query = allProducts.Where(p => p.IsActive && !p.IsDeleted);

                if (subcategoryId.HasValue)
                {
                    query = query.Where(p => p.ProductSubcategoryId == subcategoryId.Value);
                }
                else if (productTypeId.HasValue)
                {
                    query = query.Where(p => p.ProductTypeId == productTypeId.Value);
                }

                var products = query
                    .OrderBy(p => p.Name)
                    .Select(p => new { id = p.Id, name = p.Name })
                    .ToList();

                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by type {ProductTypeId}, subcategory {SubcategoryId}", productTypeId, subcategoryId);
                return Json(new { success = false, message = "Error loading products" });
            }
        }

        /// <summary>
        /// Gets products filtered by company, with category/type/subcategory data for auto-fill
        /// GET: /Certificate/GetProductsByCompany?companyId={id}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> GetProductsByCompany(int companyId)
        {
            try
            {
                // Get company details for email/website pre-population
                var company = await _context.Companies.FindAsync(companyId);

                var products = await _context.Products
                    .Where(p => p.CompanyId == companyId && p.IsActive && !p.IsDeleted)
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        categoryId = p.CategoryId,
                        categoryName = p.Category != null ? p.Category.Name : null,
                        productTypeId = p.ProductTypeId,
                        productTypeName = p.ProductType != null ? p.ProductType.Name : null,
                        productSubcategoryId = p.ProductSubcategoryId,
                        productSubcategoryName = p.ProductSubcategory != null ? p.ProductSubcategory.Name : null
                    })
                    .ToListAsync();

                return Json(new { success = true, data = products, companyEmail = company?.Email ?? "", companyWebsite = company?.Website ?? "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products for company {CompanyId}", companyId);
                return Json(new { success = false, message = "Error loading products" });
            }
        }

        /// <summary>
        /// Search products by name for autocomplete
        /// GET: /Certificate/SearchProducts?query={term}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> SearchProducts(string query)
        {
            try
            {
                var allProducts = await _unitOfWork.Products.GetAllAsync();
                var activeProducts = allProducts.Where(p => p.IsActive && !p.IsDeleted);

                if (string.IsNullOrWhiteSpace(query))
                {
                    // Return top 20 products alphabetically if no query
                    var defaultProducts = activeProducts
                        .OrderBy(p => p.Name)
                        .Take(20)
                        .Select(p => new { id = p.Id, name = p.Name })
                        .ToList();
                    return Json(new { success = true, data = defaultProducts });
                }

                var searchTerm = query.Trim().ToLowerInvariant();

                // Search by name (contains, case-insensitive)
                var matchingProducts = activeProducts
                    .Where(p => p.Name.ToLowerInvariant().Contains(searchTerm))
                    .OrderBy(p =>
                        // Prioritize exact matches, then starts-with, then contains
                        p.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ? 0 :
                        p.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                    .ThenBy(p => p.Name)
                    .Take(20)
                    .Select(p => new { id = p.Id, name = p.Name })
                    .ToList();

                return Json(new { success = true, data = matchingProducts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with query {Query}", query);
                return Json(new { success = false, message = "Error searching products" });
            }
        }

        /// <summary>
        /// Find similar products by name (for duplicate detection)
        /// GET: /Certificate/FindSimilarProducts?name={name}
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> FindSimilarProducts(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var allProducts = await _unitOfWork.Products.GetAllAsync();
                var activeProducts = allProducts.Where(p => p.IsActive && !p.IsDeleted).ToList();

                var searchName = name.Trim().ToLowerInvariant();

                // Check for exact match first
                var exactMatch = activeProducts.FirstOrDefault(p =>
                    p.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

                if (exactMatch != null)
                {
                    return Json(new {
                        success = true,
                        exactMatch = true,
                        data = new[] { new { id = exactMatch.Id, name = exactMatch.Name } }
                    });
                }

                // Find similar products using various fuzzy matching techniques
                var similarProducts = new List<(int Id, string Name, int Score)>();

                foreach (var product in activeProducts)
                {
                    var productName = product.Name.ToLowerInvariant();
                    var score = 0;

                    // Contains match
                    if (productName.Contains(searchName) || searchName.Contains(productName))
                    {
                        score = 70;
                    }
                    // Starts with same word
                    else if (productName.Split(' ').FirstOrDefault() == searchName.Split(' ').FirstOrDefault()
                        && searchName.Split(' ').FirstOrDefault()?.Length > 2)
                    {
                        score = 60;
                    }
                    // Levenshtein-like: check if removing whitespace and comparing
                    else
                    {
                        var cleanSearch = searchName.Replace(" ", "").Replace("-", "").Replace("_", "");
                        var cleanProduct = productName.Replace(" ", "").Replace("-", "").Replace("_", "");

                        if (cleanSearch == cleanProduct)
                        {
                            score = 90; // Same text, different formatting
                        }
                        else if (cleanProduct.Contains(cleanSearch) || cleanSearch.Contains(cleanProduct))
                        {
                            score = 50;
                        }
                    }

                    if (score > 0)
                    {
                        similarProducts.Add((product.Id, product.Name, score));
                    }
                }

                var results = similarProducts
                    .OrderByDescending(p => p.Score)
                    .ThenBy(p => p.Name)
                    .Take(5)
                    .Select(p => new { id = p.Id, name = p.Name, score = p.Score })
                    .ToList();

                return Json(new {
                    success = true,
                    exactMatch = false,
                    data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar products for name {Name}", name);
                return Json(new { success = false, message = "Error finding similar products" });
            }
        }

        #endregion

        #endregion

        #region PDF Upload API

        /// <summary>
        /// POST: /api/certificates/{number}/upload-pdf
        /// Uploads a PDF file to an existing certificate by certificate number.
        /// Designed for automated imports where only the PDF needs to be attached.
        /// </summary>
        [HttpPost]
        [Route("/api/certificates/{number}/upload-pdf")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UploadPdfByNumber(string number, IFormFile pdfFile)
        {
            try
            {
                if (pdfFile == null || pdfFile.Length == 0)
                    return BadRequest(new { success = false, message = "No PDF file provided" });

                if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { success = false, message = "Only PDF files are accepted" });

                if (pdfFile.Length > 20 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "File size must be less than 20MB" });

                var certificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.Number == number);

                if (certificate == null)
                    return NotFound(new { success = false, message = $"Certificate '{number}' not found" });

                var uploadResult = await _fileUploadService.UploadCertificateAsync(pdfFile, number);

                if (!uploadResult.IsSuccess)
                    return BadRequest(new { success = false, message = string.Join(", ", uploadResult.Errors) });

                certificate.CertificateFile = uploadResult.FilePath;

                // Generate thumbnail
                try
                {
                    var pdfPath = Path.Combine(_hostingEnvironment.WebRootPath,
                        uploadResult.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    var thumbnailPath = await _fileUploadService.GeneratePdfThumbnailAsync(pdfPath, number);
                    if (!string.IsNullOrEmpty(thumbnailPath))
                        certificate.CertificateImagePath = thumbnailPath;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate thumbnail for {Number}", number);
                }

                certificate.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("PDF uploaded via API for certificate {Number}", number);

                return Ok(new
                {
                    success = true,
                    message = $"PDF uploaded for certificate {number}",
                    filePath = certificate.CertificateFile,
                    thumbnailPath = certificate.CertificateImagePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload PDF for certificate {Number}", number);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}

// DTO for saving custom field values
public class SaveCustomFieldValuesRequest
{
    public int CertificateId { get; set; }
    public Dictionary<int, string> FieldValues { get; set; } = new Dictionary<int, string>();
}

// DTO for certificate import
public class CertificateImportDto
{
    public string? Number { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? CertificationType { get; set; }
    public string? EffectiveDate { get; set; }
    public string? ExpirationDate { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? Status { get; set; }
}
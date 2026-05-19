using GCCWebApp.Data;
using GCCWebApp.Models.Import;
using GCCWebApp.Services.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GCCWebApp.Controllers
{
    /// <summary>
    /// Admin-only controller for certificate CSV import with preview and comparison
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    public class CertificateImportController : Controller
    {
        private readonly ICertificateImportService _importService;
        private readonly ILogger<CertificateImportController> _logger;
        private readonly ApplicationDbContext _context;

        public CertificateImportController(
            ICertificateImportService importService,
            ILogger<CertificateImportController> logger,
            ApplicationDbContext context)
        {
            _importService = importService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// GET: /CertificateImport
        /// Shows the upload page
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// POST: /CertificateImport/Upload
        /// Uploads CSV file and generates preview
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile csvFile)
        {
            try
            {
                if (csvFile == null || csvFile.Length == 0)
                {
                    TempData["Error"] = "Please select a CSV file to upload";
                    return RedirectToAction(nameof(Index));
                }

                if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Only CSV files are supported";
                    return RedirectToAction(nameof(Index));
                }

                // Limit file size to 10MB
                if (csvFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 10MB";
                    return RedirectToAction(nameof(Index));
                }

                var userId = await GetCurrentUserIdAsync();
                var username = GetCurrentUsername();

                // Generate preview
                var preview = await _importService.PreviewImportAsync(csvFile, userId, username);

                if (preview.HasCriticalErrors)
                {
                    TempData["Error"] = "File could not be processed. Please check the format.";
                    return View("Preview", preview);
                }

                TempData["Success"] = $"File uploaded successfully: {preview.ValidRows} valid rows, {preview.InvalidRows} errors";
                return View("Preview", preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload certificate import file");
                TempData["Error"] = $"Upload failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /CertificateImport/Preview/{importId}
        /// Shows preview of a cached import
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Preview(string importId)
        {
            var preview = await _importService.GetCachedPreviewAsync(importId);

            if (preview == null)
            {
                TempData["Error"] = "Import preview has expired. Please upload the file again.";
                return RedirectToAction(nameof(Index));
            }

            return View(preview);
        }

        /// <summary>
        /// POST: /CertificateImport/Apply
        /// Applies the import after admin approval
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(string importId, string reason)
        {
            try
            {
                var preview = await _importService.GetCachedPreviewAsync(importId);

                if (preview == null)
                {
                    TempData["Error"] = "Import preview has expired. Please upload the file again.";
                    return RedirectToAction(nameof(Index));
                }

                if (!preview.CanApply)
                {
                    TempData["Error"] = "This import cannot be applied due to validation errors.";
                    return View("Preview", preview);
                }

                var userId = await GetCurrentUserIdAsync();
                var username = GetCurrentUsername();

                // Apply the import
                var result = await _importService.ApplyImportAsync(preview, userId, username, reason);

                if (result.Success)
                {
                    TempData["Success"] = $"Import completed successfully! {result.CertificatesCreated} created, {result.CertificatesUpdated} updated.";
                    return RedirectToAction("CertificateTileView", "Certificate");
                }
                else
                {
                    TempData["Error"] = result.Message;
                    if (result.Errors.Any())
                    {
                        TempData["ErrorDetails"] = string.Join("\n", result.Errors);
                    }
                    return View("Preview", preview);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply certificate import: {ImportId}", importId);
                TempData["Error"] = $"Import failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        #region API Endpoints for Modal

        /// <summary>
        /// POST: /api/CertificateImport/Preview
        /// API endpoint for modal - uploads and returns preview as JSON
        /// </summary>
        [HttpPost]
        [Route("/api/CertificateImport/Preview")]
        public async Task<IActionResult> ApiPreview(IFormFile csvFile)
        {
            try
            {
                if (csvFile == null || csvFile.Length == 0)
                {
                    return BadRequest(new { error = "Please select a CSV file to upload" });
                }

                if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "Only CSV files are supported" });
                }

                if (csvFile.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { error = "File size must be less than 10MB" });
                }

                var userId = await GetCurrentUserIdAsync();
                var username = GetCurrentUsername();

                var preview = await _importService.PreviewImportAsync(csvFile, userId, username);

                return Ok(new
                {
                    importId = preview.ImportId,
                    fileName = preview.FileName,
                    totalRows = preview.TotalRows,
                    validRows = preview.ValidRows,
                    invalidRows = preview.InvalidRows,
                    newCertificates = preview.NewCertificates,
                    modifiedCertificates = preview.ModifiedCertificates,
                    unchangedCertificates = preview.UnchangedCertificates,
                    companiesToCreate = preview.CompaniesToCreate,
                    productsToCreate = preview.ProductsToCreate,
                    tagsToCreate = preview.TagsToCreate,
                    canApply = preview.CanApply,
                    hasCriticalErrors = preview.HasCriticalErrors,
                    newRows = preview.NewRows.Select(r => new
                    {
                        rowNumber = r.RowNumber,
                        certificateNumber = r.CertificateNumber,
                        companyName = r.CompanyName,
                        productName = r.ProductName,
                        certificationType = r.CertificationType,
                        status = "New",
                        effectiveDate = r.EffectiveDate,
                        expirationDate = r.ExpirationDate
                    }),
                    modifiedRows = preview.ModifiedRows.Select(r => new
                    {
                        rowNumber = r.RowNumber,
                        certificateNumber = r.CertificateNumber,
                        companyName = r.CompanyName,
                        productName = r.ProductName,
                        certificationType = r.CertificationType,
                        status = "Modified",
                        effectiveDate = r.EffectiveDate,
                        expirationDate = r.ExpirationDate,
                        changes = r.Changes
                    }),
                    errorRows = preview.ErrorRows.Select(e => new
                    {
                        rowNumber = e.RowNumber,
                        certificateNumber = e.CertificateNumber,
                        errorMessages = e.ErrorMessages
                    }),
                    unchangedRows = preview.UnchangedRows.Select(r => new
                    {
                        rowNumber = r.RowNumber,
                        certificateNumber = r.CertificateNumber,
                        companyName = r.CompanyName,
                        productName = r.ProductName,
                        certificationType = r.CertificationType,
                        status = "Duplicate"
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Failed to preview certificate import");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST: /api/CertificateImport/Apply
        /// API endpoint for modal - applies import and returns result as JSON
        /// </summary>
        [HttpPost]
        [Route("/api/CertificateImport/Apply")]
        public async Task<IActionResult> ApiApply([FromBody] ApplyImportRequest request)
        {
            try
            {
                var preview = await _importService.GetCachedPreviewAsync(request.ImportId);

                if (preview == null)
                {
                    return BadRequest(new { success = false, message = "Import preview has expired. Please upload the file again." });
                }

                if (!preview.CanApply)
                {
                    return BadRequest(new { success = false, message = "This import cannot be applied due to validation errors." });
                }

                var userId = await GetCurrentUserIdAsync();
                var username = GetCurrentUsername();

                var result = await _importService.ApplyImportAsync(preview, userId, username, request.Reason);

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    certificatesCreated = result.CertificatesCreated,
                    certificatesUpdated = result.CertificatesUpdated,
                    certificatesFailed = result.CertificatesFailed,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Failed to apply certificate import: {ImportId}", request?.ImportId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class ApplyImportRequest
        {
            public string ImportId { get; set; } = string.Empty;
            public string? Reason { get; set; }
        }

        #endregion

        /// <summary>
        /// GET: /CertificateImport/DownloadTemplate
        /// Downloads a CSV template with headers matching the export columns and the
        /// internal certificate tile-view table labels. Re-importing an exported CSV
        /// works directly; the import service accepts these friendly headers via aliases.
        /// </summary>
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            // Headers only — no sample data, to avoid accidental import of fake data.
            // Column set mirrors Services/CertificateService.cs GenerateCsvExport (with includeInternalTags=true)
            // so the table → export → edit → re-import round-trip works end-to-end.
            // DEF-202 (QC #148): enum-typed columns now carry their allowed values inline so analysts
            // don't have to flip back to the app to look them up. Same pattern as the User Mgmt
            // template (`Status (Active/Inactive)`, `Role (Admin/Analyst/External)`) and the Tags
            // template (`TagType (Internal, External)`). Partner / Pathway / Country / cert taxonomy
            // are intentionally left plain because their value sets are admin-configurable per
            // tenant — listing them here would go stale immediately.
            var statusHint = "Status (Draft / Client Approved - Not Paid / Client Approved - Confidential / Client Approved - Paid / Hold / Expired / Republished / Expired - No Annual Verification — defaults to Draft if blank)";
            var templateCsv = "\"Certificate #\",\"Certification Type\",\"Cert Subcategories\"," +
                              $"\"{statusHint}\"," +
                              "\"Company\",\"Company Email\",\"Company URL\",\"Product Name\",\"Product Category\",\"Product Type\",\"Subcategory\",\"Partner\",\"Pathway\",\"Cert Results\",\"Effective Date\",\"Expiration Date\",\"Location\",\"Country\",\"External Tag\",\"Internal Tag\",\"ASIN\",\"UPC\",\"GTIN\",\"EAN\"";

            var bytes = System.Text.Encoding.UTF8.GetBytes(templateCsv);
            return File(bytes, "text/csv", $"GCC_Certificate_Import_Template.csv");
        }

        /// <summary>
        /// POST: /CertificateImport/Cancel
        /// Cancels an import preview
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(string importId)
        {
            // Preview will expire from cache automatically
            TempData["Info"] = "Import cancelled";
            return RedirectToAction(nameof(Index));
        }

        #region Helper Methods

        private async Task<int> GetCurrentUserIdAsync()
        {
            // Try to get user ID from claim first
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId) && userId > 0)
            {
                return userId;
            }

            // Fall back to looking up user by username
            var username = GetCurrentUsername();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user != null)
            {
                return user.Id;
            }

            // Last resort: get admin user
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            return adminUser?.Id ?? 1;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ??
                   User.FindFirst("name")?.Value ??
                   User.Identity?.Name ??
                   "admin";
        }

        #endregion
    }
}

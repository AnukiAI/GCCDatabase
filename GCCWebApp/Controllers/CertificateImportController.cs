using GCCWebApp.Models.Import;
using GCCWebApp.Services.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CertificateImportController(
            ICertificateImportService importService,
            ILogger<CertificateImportController> logger)
        {
            _importService = importService;
            _logger = logger;
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

                var userId = GetCurrentUserId();
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

                var userId = GetCurrentUserId();
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

        /// <summary>
        /// GET: /CertificateImport/DownloadTemplate
        /// Downloads a CSV template with example data
        /// </summary>
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var templateCsv = @"CertificateNumber,CompanyName,ProductName,CertificationType,Status,EffectiveDate,ExpirationDate,Location,Country,Asin,Upc,Gtin,Ean,Partner
24-0001,Acme Corporation,Widget Pro,FSC,active,2024-01-01,2025-01-01,""Portland, OR"",United States,B08X7N9MRQ,846378985421,00846378985421,0846378985421,None
24-0002,Beta Industries,Gadget Plus,ISO 9001,draft,2024-02-01,2025-02-01,""Berlin, CT"",United States,B09XYZ7890,234567890123,,,None";

            var bytes = System.Text.Encoding.UTF8.GetBytes(templateCsv);
            return File(bytes, "text/csv", $"certificate_import_template_{DateTime.Now:yyyyMMdd}.csv");
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

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Unknown";
        }

        #endregion
    }
}

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
    /// Certificate CSV import with preview and comparison.
    /// Opened to Analyst + Admin per client request (7/10 call): analysts need Import/Template.
    /// </summary>
    [Authorize(Policy = "AnalystOrAdmin")]
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
        public async Task<IActionResult> Apply(string importId, string reason, bool acknowledgeReassignments = false, bool acknowledgeOverwrites = false)
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

                // Company-reassignment guard: when one or more rows would move an existing certificate
                // to a different company (the miskeyed cert-number collision), refuse to apply unless
                // the analyst explicitly acknowledged it on the preview. Enforced here — not just in the
                // view — so the block cannot be bypassed by posting the form directly.
                if (preview.HasCompanyReassignments && !acknowledgeReassignments)
                {
                    TempData["Error"] = $"{preview.CompanyReassignmentCount} row(s) would reassign an existing " +
                        "certificate to a different company. Review the highlighted rows and check the " +
                        "acknowledgement box to confirm before applying.";
                    return View("Preview", preview);
                }

                // Overwrite guard (CR-023 R5): applying over existing certificates is destructive.
                // Require an explicit acknowledgement (enforced here, not just in the view) before we
                // overwrite live records. A safety backup of those records is taken automatically by
                // the service before any change is applied.
                if (preview.WillOverwriteExisting && !acknowledgeOverwrites)
                {
                    TempData["Error"] = $"{preview.OverwriteCount} existing certificate(s) would be overwritten. " +
                        "Check the acknowledgement box to confirm — a backup of their current data is saved " +
                        "automatically before any changes are applied.";
                    return View("Preview", preview);
                }

                var userId = await GetCurrentUserIdAsync();
                var username = GetCurrentUsername();

                // Apply the import
                var result = await _importService.ApplyImportAsync(preview, userId, username, reason);

                if (result.Success)
                {
                    // CR-023 R5: when the import overwrote existing certificates a safety backup was
                    // taken automatically — tell the analyst it exists and name the file.
                    var backupNote = result.BackupCertificateCount > 0
                        ? $" A backup of {result.BackupCertificateCount} overwritten certificate(s) was saved ({result.BackupFileName})."
                        : string.Empty;

                    // DEF-338 (QC #193 follow-up, client 2026-08-04): the analyst DOES get a
                    // confirmation after a successful import — rendered as a non-blocking toast
                    // by the reader view, not as a page banner and not as a blocking modal.
                    //
                    // History: this used to write the shared TempData["Success"], whose only readers
                    // are the CertificateImport views. The redirect target read nothing, and an
                    // *unread* TempData entry is not discarded — it is re-saved on every subsequent
                    // request until some view reads that key — so the message resurfaced later as an
                    // out-of-context "N certificates created" banner on Import Index/Preview/History
                    // (DEF-330). The fix then was to delete the write, which removed the leak but
                    // also left the analyst with no confirmation at all.
                    //
                    // Correct shape: a PRIVATE key whose one and only reader is the redirect target
                    // itself, so it is consumed on the very next request and can never surface
                    // anywhere else. Reader: Views/Certificate/CertificateTileView.cshtml.
                    // Invariant: never a writer without a reader on the redirect target.
                    TempData["ImportSuccessMessage"] = $"Import completed successfully! {result.CertificatesCreated} created, {result.CertificatesUpdated} updated.{backupNote}";
                    return RedirectToAction("CertificateTileView", "Certificate");
                }
                else
                {
                    // DEF-330: "ErrorDetails" had a writer and NO reader anywhere (no view, no
                    // controller) — so it was never displayed, yet an unread TempData entry is
                    // re-saved on every later request. On a large failed import that parks a
                    // multi-KB string in the TempData cookie indefinitely. The same per-row errors
                    // are already rendered on this very page from Model.ErrorRows
                    // (Views/CertificateImport/Preview.cshtml:216-226), so nothing is lost.
                    TempData["Error"] = result.Message;
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
        /// GET: /CertificateImport/History
        /// CR-023: lists past certificate imports (most recent first) with an Undo action.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var batches = await _importService.GetImportHistoryAsync(100);
            return View(batches);
        }

        /// <summary>
        /// POST: /CertificateImport/UndoImport
        /// CR-023: rolls back a completed import batch — restores overwritten certificates to their
        /// pre-import values and removes the certificates the import created. Admin-only (controller
        /// policy) + antiforgery; the destructive confirmation is shown client-side before posting.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoImport(Guid batchId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                var username = GetCurrentUsername();
                var result = await _importService.UndoImportBatchAsync(batchId, userId, username);

                if (result.Success)
                    TempData["Success"] = result.Message;
                else
                    TempData["Error"] = result.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to undo import batch {BatchId}", batchId);
                TempData["Error"] = $"Undo failed: {ex.Message}";
            }
            return RedirectToAction(nameof(History));
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
                    internalTagsToCreate = preview.InternalTagsToCreate,
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

                // Company-reassignment guard (mirrors the MVC Apply action): block until acknowledged.
                if (preview.HasCompanyReassignments && !request.AcknowledgeReassignments)
                {
                    return BadRequest(new
                    {
                        success = false,
                        requiresReassignmentAck = true,
                        companyReassignmentCount = preview.CompanyReassignmentCount,
                        message = $"{preview.CompanyReassignmentCount} row(s) would reassign an existing certificate " +
                            "to a different company. Acknowledge the reassignment to proceed."
                    });
                }

                // Overwrite guard (CR-023 R5; mirrors the MVC Apply action): block until acknowledged.
                if (preview.WillOverwriteExisting && !request.AcknowledgeOverwrites)
                {
                    return BadRequest(new
                    {
                        success = false,
                        requiresOverwriteAck = true,
                        overwriteCount = preview.OverwriteCount,
                        message = $"{preview.OverwriteCount} existing certificate(s) would be overwritten. " +
                            "Acknowledge to proceed — a backup of their current data is saved automatically first."
                    });
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
                    errors = result.Errors,
                    backupFileName = result.BackupFileName,
                    backupCertificateCount = result.BackupCertificateCount
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

            // Set true by the client only after the analyst confirms the company-reassignment warning.
            public bool AcknowledgeReassignments { get; set; }

            // CR-023 R5: set true only after the analyst confirms existing certificates will be
            // overwritten (a safety backup is taken automatically before changes are applied).
            public bool AcknowledgeOverwrites { get; set; }
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
            // template (`TagType (Internal, External)`). Pathway / Country / cert taxonomy
            // are intentionally left plain because their value sets are admin-configurable per
            // tenant — listing them here would go stale immediately.
            // DEF-316 (QC #171): Partner carries a *separator* hint rather than a value list, for the
            // same reason — the partner set is tenant-configurable, but multi-partner certs (CR-171)
            // made the delimiter the thing analysts actually get wrong. Client asked for this exact
            // wording, so don't reword it. SyncCertificatePartnersAsync splits on both ',' and '|',
            // so the previous pipe-based files keep importing; only the guidance changed. The
            // parenthetical is safe for header matching — GetFieldWithMapping falls back to a
            // "Partner " prefix match (see CertificateImportService.cs:129-157).
            // CR-037: Country carries the same separator hint for the same reason — it is
            // multi-value now, and the delimiter is what analysts get wrong. CountryList.ParseMany
            // splits on both ',' and '|', so existing single-country files keep importing
            // unchanged; only the guidance changed.
            var statusHint = "Status (Draft / Client Approved - Not Paid / Client Approved - Confidential / Client Approved - Paid / Hold / Expired / Republished / Expired - No Annual Verification — defaults to Draft if blank)";
            var templateCsv = "\"Certificate #\",\"Certification Type\",\"Cert Subcategories\"," +
                              $"\"{statusHint}\"," +
                              "\"Company\",\"Company Email\",\"Company URL\",\"Product Name\",\"Product Category\",\"Product Type\",\"Subcategory\",\"Partner (separate by comma)\",\"Pathway\",\"Cert Results\",\"Effective Date\",\"Expiration Date\",\"Location\",\"Country (separate by comma)\",\"External Tag\",\"Internal Tag\",\"ASIN\",\"UPC\",\"GTIN\",\"EAN\"";

            // Prepend a UTF-8 BOM so Excel on Windows decodes the file as UTF-8 instead of
            // its default Windows-1252. Without the BOM, the em dash in the Status hint
            // and any future non-ASCII characters render as mojibake (the em dash byte
            // sequence gets shown as garbled Latin-1 characters in Excel).
            var utf8WithBom = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var preamble = utf8WithBom.GetPreamble();
            var body = utf8WithBom.GetBytes(templateCsv);
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
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

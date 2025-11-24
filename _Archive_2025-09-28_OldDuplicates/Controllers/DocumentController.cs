using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Controllers
{
    /// <summary>
    /// Controller for Certificate Document management
    /// Handles document upload, download, preview, and management
    /// </summary>
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            ApplicationDbContext context,
            IFileUploadService fileUploadService,
            ILogger<DocumentController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        /// <summary>
        /// Display upload form for certificate documents
        /// GET: /Document/Upload/{certificateId}
        /// </summary>
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Upload(int certificateId)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == certificateId);

            if (certificate == null)
            {
                return NotFound();
            }

            ViewBag.Certificate = certificate;
            var model = new CertificateDocumentUploadViewModel
            {
                CertificateId = certificateId
            };

            return View(model);
        }

        /// <summary>
        /// Handle document upload
        /// POST: /Document/Upload
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> Upload(CertificateDocumentUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == model.CertificateId);
                ViewBag.Certificate = certificate;
                return View(model);
            }

            try
            {
                var result = await _fileUploadService.UploadCertificateDocumentAsync(
                    model.File,
                    model.CertificateId,
                    model.DocumentType,
                    model.Description,
                    GetCurrentUserId());

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Document uploaded successfully.";
                    return RedirectToAction("Details", "Certificate", new { id = model.CertificateId });
                }
                else
                {
                    ModelState.AddModelError("", result.GetErrorsAsString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for certificate {CertificateId}", model.CertificateId);
                ModelState.AddModelError("", "An error occurred while uploading the document.");
            }

            // If we got here, something failed, reload the form
            var cert = await _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == model.CertificateId);
            ViewBag.Certificate = cert;
            return View(model);
        }

        /// <summary>
        /// Download a document
        /// GET: /Document/Download/{id}
        /// </summary>
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.Set<CertificateDocument>()
                .Include(d => d.Certificate)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            // Check if user has access to this document
            if (!document.IsPublic && !CanUserAccessDocument(document))
            {
                return Forbid();
            }

            try
            {
                var fileBytes = await _fileUploadService.GetFileAsync(document.FilePath);
                if (fileBytes == null)
                {
                    _logger.LogWarning("File not found for document {DocumentId}: {FilePath}", id, document.FilePath);
                    return NotFound("File not found.");
                }

                return File(fileBytes, document.ContentType, document.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", id);
                return StatusCode(500, "An error occurred while downloading the file.");
            }
        }

        /// <summary>
        /// Preview a document (for supported file types)
        /// GET: /Document/Preview/{id}
        /// </summary>
        public async Task<IActionResult> Preview(int id)
        {
            var document = await _context.Set<CertificateDocument>()
                .Include(d => d.Certificate)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            // Check if user has access to this document
            if (!document.IsPublic && !CanUserAccessDocument(document))
            {
                return Forbid();
            }

            try
            {
                var fileBytes = await _fileUploadService.GetFileAsync(document.FilePath);
                if (fileBytes == null)
                {
                    return NotFound("File not found.");
                }

                // Return file for preview - browser will handle based on content type
                return File(fileBytes, document.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing document {DocumentId}", id);
                return StatusCode(500, "An error occurred while previewing the file.");
            }
        }

        /// <summary>
        /// Get documents for a certificate (AJAX)
        /// GET: /Document/GetDocuments/{certificateId}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDocuments(int certificateId)
        {
            try
            {
                var documents = await _context.Set<CertificateDocument>()
                    .Where(d => d.CertificateId == certificateId && d.IsActive)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new
                    {
                        id = d.Id,
                        fileName = d.OriginalFileName,
                        documentType = d.DocumentType,
                        description = d.Description,
                        fileSize = d.FileSize,
                        uploadedAt = d.UploadedAt.ToString("yyyy-MM-dd HH:mm"),
                        isPublic = d.IsPublic,
                        canDownload = d.IsPublic || CanUserAccessDocument(d),
                        downloadUrl = d.IsPublic || CanUserAccessDocument(d) ? Url.Action("Download", new { id = d.Id }) : null,
                        previewUrl = IsPreviewableFile(d.ContentType) && (d.IsPublic || CanUserAccessDocument(d)) ? Url.Action("Preview", new { id = d.Id }) : null
                    })
                    .ToListAsync();

                return Json(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for certificate {CertificateId}", certificateId);
                return BadRequest("An error occurred while retrieving documents.");
            }
        }

        /// <summary>
        /// Delete a document
        /// POST: /Document/Delete/{id}
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var document = await _context.Set<CertificateDocument>()
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (document == null)
                {
                    return NotFound();
                }

                // Mark as inactive instead of deleting
                document.IsActive = false;
                document.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Optionally delete the physical file
                await _fileUploadService.DeleteFileAsync(document.FilePath);

                _logger.LogInformation("Document {DocumentId} deleted by user", id);
                
                return Json(new { success = true, message = "Document deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the document." });
            }
        }

        #region Helper Methods

        private bool CanUserAccessDocument(CertificateDocument document)
        {
            // Check if user has access to this certificate/document
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin" || userRole == "Analyst";
        }

        private bool IsPreviewableFile(string contentType)
        {
            var previewableTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg", 
                "image/png",
                "image/gif",
                "text/plain"
            };

            return previewableTypes.Contains(contentType.ToLower());
        }

        private int? GetCurrentUserId()
        {
            // This would typically get the current user ID from claims or session
            // For now, returning null as we don't have user authentication fully set up
            return null;
        }

        #endregion
    }
}
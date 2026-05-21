using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using GCCWebApp.Models;
using GCCWebApp.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Docnet.Core;
using Docnet.Core.Models;

namespace GCCWebApp.Services
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> UploadCertificateAsync(IFormFile file, string certificateNumber);
        Task<FileUploadResult> UploadImageAsync(IFormFile file, string identifier);
        Task<FileUploadResult> UploadCertificateDocumentAsync(IFormFile file, int certificateId, string documentType, string? description = null, int? uploadedBy = null);
        Task<bool> DeleteFileAsync(string filePath);
        Task<byte[]?> GetFileAsync(string filePath);
        string GetFileUrl(string filePath);
        bool IsValidFileType(IFormFile file, FileType fileType);
        bool IsValidFileSize(IFormFile file, long maxSizeInBytes = 10485760); // 10MB default
        FileValidationResult ValidateFile(IFormFile file, FileUploadConfiguration config);
        Task<string> CalculateFileHashAsync(Stream stream);
        Task<bool> ScanFileForVirusesAsync(string filePath); // Placeholder for virus scanning
        Task<string?> GeneratePdfThumbnailAsync(string pdfFilePath, string certificateNumber);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly string _uploadsPath;
        private readonly string _certificatesPath;
        private readonly string _imagesPath;
        private readonly FileUploadConfiguration _config;

        public FileUploadService(ILogger<FileUploadService> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _logger = logger;
            _environment = environment;
            _context = context;

            // DEF-208: physical upload files now live OUTSIDE the web root so the
            // static-file middleware can't serve them. Logical paths in DB columns
            // (CertificateFile, CertificateImagePath) keep their existing
            // "uploads/.../{filename}" shape — resolved against ContentRootPath/App_Data
            // by CertificateController.StreamUploadedFile.
            //
            // Defensive guard: ContentRootPath must resolve before we build upload paths.
            // Fail loudly so a missing ContentRootPath can't silently land uploads at
            // the application root.
            if (string.IsNullOrEmpty(_environment.ContentRootPath))
            {
                _logger.LogCritical("ContentRootPath is null or empty at FileUploadService init.");
                throw new InvalidOperationException(
                    "ContentRootPath is null or empty. Aborting FileUploadService init to " +
                    "prevent uploads landing at the application root.");
            }

            // Set up upload directories under App_Data (outside wwwroot — see DEF-208).
            _uploadsPath = Path.Combine(_environment.ContentRootPath, "App_Data", "uploads");
            _certificatesPath = Path.Combine(_uploadsPath, "certificates");
            _imagesPath = Path.Combine(_uploadsPath, "images");

            // Ensure directories exist
            Directory.CreateDirectory(_certificatesPath);
            Directory.CreateDirectory(_imagesPath);

            // Initialize configuration with defaults
            _config = new FileUploadConfiguration();
        }

        public async Task<FileUploadResult> UploadCertificateAsync(IFormFile file, string certificateNumber)
        {
            var result = new FileUploadResult();

            try
            {
                // Validate file
                if (!IsValidFileType(file, FileType.Certificate))
                {
                    result.AddError("Invalid file type. Only PDF files are allowed for certificates.");
                    return result;
                }

                if (!IsValidFileSize(file, 20971520)) // 20MB for certificates
                {
                    result.AddError("File size exceeds the maximum limit of 20MB.");
                    return result;
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"{certificateNumber}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}";
                var filePath = Path.Combine(_certificatesPath, fileName);

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    fileName = $"{certificateNumber}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[0..8]{fileExtension}";
                    filePath = Path.Combine(_certificatesPath, fileName);
                }

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                result.FilePath = Path.Combine("uploads", "certificates", fileName).Replace("\\", "/");
                result.FileName = fileName;
                result.FileSize = file.Length;
                result.IsSuccess = true;

                _logger.LogInformation("Certificate file uploaded successfully: {FileName} for certificate {CertificateNumber}", 
                    fileName, certificateNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading certificate file for certificate {CertificateNumber}", certificateNumber);
                result.AddError("An error occurred while uploading the file.");
            }

            return result;
        }

        public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string identifier)
        {
            var result = new FileUploadResult();

            try
            {
                // Validate file
                if (!IsValidFileType(file, FileType.Image))
                {
                    result.AddError("Invalid file type. Only JPG, PNG, and GIF images are allowed.");
                    return result;
                }

                if (!IsValidFileSize(file, 5242880)) // 5MB for images
                {
                    result.AddError("File size exceeds the maximum limit of 5MB.");
                    return result;
                }

                // Validate image dimensions and detect corruption
                ImageValidationResult imageValidation;
                using (var stream = file.OpenReadStream())
                {
                    imageValidation = await ValidateImageAsync(stream);
                }

                if (!imageValidation.IsValid)
                {
                    result.AddError(imageValidation.ErrorMessage ?? "Image validation failed.");
                    return result;
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var fileName = $"{identifier}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}";
                var filePath = Path.Combine(_imagesPath, fileName);

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    fileName = $"{identifier}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[0..8]{fileExtension}";
                    filePath = Path.Combine(_imagesPath, fileName);
                }

                // Save file with optional resizing for large images
                using (var inputStream = file.OpenReadStream())
                {
                    using (var image = await Image.LoadAsync(inputStream))
                    {
                        // Resize if image is too large (max 2000px on longest side)
                        if (image.Width > 2000 || image.Height > 2000)
                        {
                            var resizeOptions = new ResizeOptions
                            {
                                Size = new Size(2000, 2000),
                                Mode = ResizeMode.Max
                            };
                            image.Mutate(x => x.Resize(resizeOptions));
                            _logger.LogInformation("Image resized from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}",
                                imageValidation.Width, imageValidation.Height, image.Width, image.Height);
                        }

                        await image.SaveAsync(filePath);
                    }
                }

                result.FilePath = Path.Combine("uploads", "images", fileName).Replace("\\", "/");
                result.FileName = fileName;
                result.FileSize = new FileInfo(filePath).Length; // Get actual file size after processing
                result.IsSuccess = true;

                _logger.LogInformation("Image file uploaded successfully: {FileName} for {Identifier} (Dimensions: {Width}x{Height})",
                    fileName, identifier, imageValidation.Width, imageValidation.Height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file for {Identifier}", identifier);
                result.AddError("An error occurred while uploading the image.");
            }

            return result;
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }
                
                _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
                await Task.CompletedTask;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<byte[]?> GetFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return null;

                var fullPath = Path.Combine(_environment.WebRootPath, filePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                
                if (File.Exists(fullPath))
                {
                    return await File.ReadAllBytesAsync(fullPath);
                }
                
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                return null;
            }
        }

        public string GetFileUrl(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            return $"/{filePath.Replace("\\", "/")}";
        }

        public bool IsValidFileType(IFormFile file, FileType fileType)
        {
            if (file == null || string.IsNullOrEmpty(file.FileName))
                return false;

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var contentType = file.ContentType.ToLower();

            return fileType switch
            {
                FileType.Certificate => IsValidCertificateFile(fileExtension, contentType),
                FileType.Image => IsValidImageFile(fileExtension, contentType),
                FileType.Document => IsValidDocumentFile(fileExtension, contentType),
                _ => false
            };
        }

        public bool IsValidFileSize(IFormFile file, long maxSizeInBytes = 10485760)
        {
            return file != null && file.Length > 0 && file.Length <= maxSizeInBytes;
        }

        public async Task<FileUploadResult> UploadCertificateDocumentAsync(IFormFile file, int certificateId, string documentType, string? description = null, int? uploadedBy = null)
        {
            var result = new FileUploadResult();

            try
            {
                // Validate file
                var validation = ValidateFile(file, _config);
                if (!validation.IsValid)
                {
                    result.AddError(string.Join("; ", validation.Errors));
                    return result;
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                var safeFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace("-", "_");
                var uniqueFileName = $"{certificateId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeFileName}{fileExtension}";
                var filePath = Path.Combine(_certificatesPath, uniqueFileName);

                // Ensure unique filename
                var counter = 1;
                while (File.Exists(filePath))
                {
                    uniqueFileName = $"{certificateId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeFileName}_{counter}{fileExtension}";
                    filePath = Path.Combine(_certificatesPath, uniqueFileName);
                    counter++;
                }

                string fileHash;
                
                // Save file and calculate hash
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;
                    fileHash = await CalculateFileHashAsync(stream);
                }

                // Save document record to database
                var document = new CertificateDocument
                {
                    FileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FilePath = Path.Combine("uploads", "certificates", uniqueFileName).Replace("\\", "/"),
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FileHash = fileHash,
                    DocumentType = documentType,
                    Description = description,
                    CertificateId = certificateId,
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<CertificateDocument>().Add(document);
                await _context.SaveChangesAsync();

                // Perform virus scan (if enabled)
                if (_config.RequireVirusScanning)
                {
                    var virusScanResult = await ScanFileForVirusesAsync(filePath);
                    document.IsVirusScanned = true;
                    document.PassedVirusScan = virusScanResult;
                    document.VirusScanDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    if (!virusScanResult)
                    {
                        // Delete the file if virus scan fails
                        await DeleteFileAsync(document.FilePath);
                        _context.Set<CertificateDocument>().Remove(document);
                        await _context.SaveChangesAsync();
                        
                        result.AddError("File failed virus scan and has been removed.");
                        return result;
                    }
                }

                result.FilePath = document.FilePath;
                result.FileName = uniqueFileName;
                result.FileSize = file.Length;
                result.FileHash = fileHash;
                result.DocumentId = document.Id;
                result.IsSuccess = true;

                _logger.LogInformation("Certificate document uploaded successfully: {FileName} for certificate {CertificateId}", 
                    uniqueFileName, certificateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading certificate document for certificate {CertificateId}", certificateId);
                result.AddError("An error occurred while uploading the document.");
            }

            return result;
        }

        public FileValidationResult ValidateFile(IFormFile file, FileUploadConfiguration config)
        {
            var result = new FileValidationResult { IsValid = true };

            if (file == null || file.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("No file was uploaded.");
                return result;
            }

            // Check file size
            if (file.Length > config.MaxFileSize)
            {
                result.IsValid = false;
                result.Errors.Add($"File size ({file.Length:N0} bytes) exceeds maximum allowed size ({config.MaxFileSize:N0} bytes).");
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!config.AllowedExtensions.Contains(fileExtension))
            {
                result.IsValid = false;
                result.Errors.Add($"File extension '{fileExtension}' is not allowed. Allowed extensions: {string.Join(", ", config.AllowedExtensions)}");
            }

            // Check for dangerous extensions
            if (config.DangerousExtensions.Contains(fileExtension))
            {
                result.IsValid = false;
                result.Errors.Add($"File extension '{fileExtension}' is potentially dangerous and not allowed.");
            }

            // Check content type
            if (!config.AllowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                result.IsValid = false;
                result.Errors.Add($"Content type '{file.ContentType}' is not allowed.");
            }

            // Additional security checks
            if (string.IsNullOrWhiteSpace(file.FileName) || file.FileName.Contains(".."))
            {
                result.IsValid = false;
                result.Errors.Add("Invalid file name.");
            }

            return result;
        }

        public async Task<string> CalculateFileHashAsync(Stream stream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToHexString(hashBytes).ToLower();
        }

        public async Task<bool> ScanFileForVirusesAsync(string filePath)
        {
            // Placeholder implementation - integrate with actual virus scanning service
            // For now, always return true (no virus detected)
            await Task.Delay(100); // Simulate scanning time

            _logger.LogInformation("Virus scan performed on file: {FilePath} (placeholder implementation)", filePath);
            return true;
        }

        /// <summary>
        /// Generates a thumbnail image from the first page of a PDF file
        /// </summary>
        /// <param name="pdfFilePath">Relative path to the PDF file (from wwwroot)</param>
        /// <param name="certificateNumber">Certificate number for naming the thumbnail</param>
        /// <returns>Relative path to the generated thumbnail, or null if failed</returns>
        public async Task<string?> GeneratePdfThumbnailAsync(string pdfFilePath, string certificateNumber)
        {
            try
            {
                // Get full path to PDF
                var fullPdfPath = Path.Combine(_environment.WebRootPath, pdfFilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (!File.Exists(fullPdfPath))
                {
                    _logger.LogWarning("PDF file not found for thumbnail generation: {PdfPath}", fullPdfPath);
                    return null;
                }

                // Generate unique thumbnail filename
                var thumbnailFileName = $"{certificateNumber}_thumb_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                var thumbnailPath = Path.Combine(_imagesPath, thumbnailFileName);

                // First pass: detect actual PDF page aspect ratio using a small render
                int renderWidth, renderHeight;
                using (var detectReader = DocLib.Instance.GetDocReader(fullPdfPath, new PageDimensions(100, 100)))
                {
                    var pageCount = detectReader.GetPageCount();
                    if (pageCount == 0)
                    {
                        _logger.LogWarning("PDF has no pages: {PdfPath}", fullPdfPath);
                        return null;
                    }
                    using (var detectPage = detectReader.GetPageReader(0))
                    {
                        var pageW = detectPage.GetPageWidth();
                        var pageH = detectPage.GetPageHeight();
                        _logger.LogInformation("PDF page detected dimensions: {W}x{H} for {Path}", pageW, pageH, pdfFilePath);

                        // Scale to ~150 DPI equivalent while preserving aspect ratio
                        // Use 1200px on the longer side, minimum 100px on shorter side
                        if (pageW >= pageH && pageH > 0)
                        {
                            // Landscape
                            renderWidth = 1200;
                            renderHeight = Math.Max(100, (int)(1200.0 * pageH / pageW));
                        }
                        else if (pageW > 0)
                        {
                            // Portrait
                            renderHeight = 1200;
                            renderWidth = Math.Max(100, (int)(1200.0 * pageW / pageH));
                        }
                        else
                        {
                            // Fallback to A4 portrait if detection fails
                            renderWidth = 1240;
                            renderHeight = 1754;
                        }
                    }
                }

                _logger.LogInformation("Rendering PDF thumbnail at {W}x{H} for certificate {Cert}", renderWidth, renderHeight, certificateNumber);

                // Second pass: render at correct aspect ratio
                // Docnet requires dimOne <= dimTwo, so always pass smaller first
                var dimSmall = Math.Min(renderWidth, renderHeight);
                var dimLarge = Math.Max(renderWidth, renderHeight);
                using (var docReader = DocLib.Instance.GetDocReader(fullPdfPath, new PageDimensions(dimSmall, dimLarge)))
                {
                    // Get first page
                    using (var pageReader = docReader.GetPageReader(0))
                    {
                        var width = pageReader.GetPageWidth();
                        var height = pageReader.GetPageHeight();
                        var rawBytes = pageReader.GetImage();

                        if (rawBytes == null || rawBytes.Length == 0)
                        {
                            _logger.LogWarning("Failed to render PDF page to image: {PdfPath}", fullPdfPath);
                            return null;
                        }

                        // Convert BGRA to RGBA and create image
                        // Docnet returns BGRA format, ImageSharp expects RGBA
                        for (int i = 0; i < rawBytes.Length; i += 4)
                        {
                            var temp = rawBytes[i]; // B
                            rawBytes[i] = rawBytes[i + 2]; // R -> B position
                            rawBytes[i + 2] = temp; // B -> R position
                        }

                        // Create image from raw bytes
                        using (var image = Image.LoadPixelData<Rgba32>(rawBytes, width, height))
                        {
                            // Resize to thumbnail size (max 600px on longest side)
                            if (image.Width > 600 || image.Height > 600)
                            {
                                var resizeOptions = new ResizeOptions
                                {
                                    Size = new Size(600, 600),
                                    Mode = ResizeMode.Max // Scales to fit within 600x600 preserving aspect ratio
                                };
                                image.Mutate(x => x.Resize(resizeOptions));
                            }

                            // Save as PNG
                            await image.SaveAsPngAsync(thumbnailPath);
                        }
                    }
                }

                var relativePath = Path.Combine("uploads", "images", thumbnailFileName).Replace("\\", "/");
                _logger.LogInformation("PDF thumbnail generated successfully: {ThumbnailPath} for certificate {CertificateNumber}",
                    relativePath, certificateNumber);

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF thumbnail for certificate {CertificateNumber}", certificateNumber);
                return null;
            }
        }

        /// <summary>
        /// Validates image dimensions and detects corruption
        /// </summary>
        private async Task<ImageValidationResult> ValidateImageAsync(Stream stream)
        {
            var result = new ImageValidationResult { IsValid = true };

            try
            {
                using (var image = await Image.LoadAsync(stream))
                {
                    result.Width = image.Width;
                    result.Height = image.Height;

                    // Check minimum dimensions
                    if (image.Width < 100 || image.Height < 100)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Image dimensions too small. Minimum size is 100x100 pixels. Uploaded: {image.Width}x{image.Height}.";
                        return result;
                    }

                    // Check maximum dimensions
                    if (image.Width > 4000 || image.Height > 4000)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Image dimensions too large. Maximum size is 4000x4000 pixels. Uploaded: {image.Width}x{image.Height}.";
                        return result;
                    }

                    _logger.LogInformation("Image validation passed: {Width}x{Height} pixels", image.Width, image.Height);
                }
            }
            catch (UnknownImageFormatException ex)
            {
                result.IsValid = false;
                result.ErrorMessage = "The uploaded file is not a valid image or is corrupted.";
                _logger.LogWarning(ex, "Image format not recognized or corrupted");
            }
            catch (InvalidImageContentException ex)
            {
                result.IsValid = false;
                result.ErrorMessage = "The image file is corrupted or contains invalid data.";
                _logger.LogWarning(ex, "Invalid image content detected");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = "Failed to validate image. The file may be corrupted.";
                _logger.LogError(ex, "Unexpected error during image validation");
            }

            return result;
        }

        #region Private Helper Methods

        private bool IsValidCertificateFile(string fileExtension, string contentType)
        {
            var validExtensions = new[] { ".pdf" };
            var validContentTypes = new[] { "application/pdf" };

            return validExtensions.Contains(fileExtension) && 
                   validContentTypes.Contains(contentType);
        }

        private bool IsValidImageFile(string fileExtension, string contentType)
        {
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var validContentTypes = new[] 
            { 
                "image/jpeg", "image/jpg", "image/png", "image/gif", 
                "image/bmp", "image/webp" 
            };

            return validExtensions.Contains(fileExtension) && 
                   validContentTypes.Contains(contentType);
        }

        private bool IsValidDocumentFile(string fileExtension, string contentType)
        {
            var validExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf" };
            var validContentTypes = new[] 
            { 
                "application/pdf", "application/msword", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "text/plain", "application/rtf"
            };

            return validExtensions.Contains(fileExtension) && 
                   validContentTypes.Contains(contentType);
        }

        #endregion
    }

    /// <summary>
    /// Represents the result of a file upload operation
    /// </summary>
    public class FileUploadResult
    {
        public bool IsSuccess { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? FileHash { get; set; }
        public int? DocumentId { get; set; }
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        public string GetErrorsAsString()
        {
            return string.Join("; ", Errors);
        }
    }

    /// <summary>
    /// Enumeration of supported file types
    /// </summary>
    public enum FileType
    {
        Certificate,
        Image,
        Document
    }

    /// <summary>
    /// Represents the result of image validation
    /// </summary>
    public class ImageValidationResult
    {
        public bool IsValid { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
using Microsoft.Extensions.Logging;
using System.IO;

namespace GCCWebApp.Services
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> UploadCertificateAsync(IFormFile file, string certificateNumber);
        Task<FileUploadResult> UploadImageAsync(IFormFile file, string identifier);
        Task<bool> DeleteFileAsync(string filePath);
        Task<byte[]?> GetFileAsync(string filePath);
        string GetFileUrl(string filePath);
        bool IsValidFileType(IFormFile file, FileType fileType);
        bool IsValidFileSize(IFormFile file, long maxSizeInBytes = 10485760); // 10MB default
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly ILogger<FileUploadService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsPath;
        private readonly string _certificatesPath;
        private readonly string _imagesPath;

        public FileUploadService(ILogger<FileUploadService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            
            // Set up upload directories
            _uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            _certificatesPath = Path.Combine(_uploadsPath, "certificates");
            _imagesPath = Path.Combine(_uploadsPath, "images");
            
            // Ensure directories exist
            Directory.CreateDirectory(_certificatesPath);
            Directory.CreateDirectory(_imagesPath);
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

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                result.FilePath = Path.Combine("uploads", "images", fileName).Replace("\\", "/");
                result.FileName = fileName;
                result.FileSize = file.Length;
                result.IsSuccess = true;

                _logger.LogInformation("Image file uploaded successfully: {FileName} for {Identifier}", fileName, identifier);
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
}
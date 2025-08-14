using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// View model for file upload form
    /// </summary>
    public class CertificateDocumentUploadViewModel
    {
        public int CertificateId { get; set; }

        [Required(ErrorMessage = "Please select a file to upload")]
        public IFormFile File { get; set; } = null!;

        [MaxLength(50)]
        public string DocumentType { get; set; } = "Certificate";

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsPublic { get; set; } = false;
    }

    /// <summary>
    /// Configuration for file upload constraints
    /// </summary>
    public class FileUploadConfiguration
    {
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB default
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".doc", ".docx" };
        public string[] AllowedContentTypes { get; set; } = 
        {
            "application/pdf",
            "image/jpeg", 
            "image/jpg",
            "image/png", 
            "image/tiff",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        public string UploadDirectory { get; set; } = "uploads/certificates";
        public bool RequireVirusScanning { get; set; } = true;
        public string[] DangerousExtensions { get; set; } = 
        {
            ".exe", ".bat", ".cmd", ".com", ".scr", ".vbs", ".js", ".jar", ".ps1", ".sh"
        };
    }

    /// <summary>
    /// Result of file upload operation
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public string? FileHash { get; set; }
        public int? DocumentId { get; set; }
    }

    /// <summary>
    /// File validation result
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
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
            // Executables
            ".exe", ".msi", ".bat", ".cmd", ".com", ".scr", ".pif", ".application", ".gadget",
            ".msp", ".cpl", ".scf", ".lnk", ".inf", ".reg",
            // Scripts
            ".vbs", ".vbe", ".js", ".jse", ".ws", ".wsf", ".wsc", ".wsh", ".ps1", ".ps2",
            ".psc1", ".psc2", ".msh", ".msh1", ".msh2", ".mshxml", ".msh1xml", ".msh2xml",
            // Shell scripts
            ".sh", ".bash", ".zsh", ".csh", ".tcsh", ".ksh", ".fish",
            // Archives that could contain malicious files
            ".jar", ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2",
            // Web/Server scripts
            ".php", ".asp", ".aspx", ".jsp", ".cgi", ".pl", ".py", ".rb",
            // Database and config files
            ".sql", ".mdb", ".accdb", ".db", ".dbf",
            // Other dangerous formats
            ".hta", ".msc", ".url", ".isu", ".job"
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Represents a document attached to a certificate
    /// </summary>
    public class CertificateDocument
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [MaxLength(64)]
        public string? FileHash { get; set; } // SHA256 hash for integrity

        [MaxLength(50)]
        public string DocumentType { get; set; } = "Certificate"; // Certificate, Supporting, Amendment, etc.

        [MaxLength(500)]
        public string? Description { get; set; }

        public int Version { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public bool IsPublic { get; set; } = false; // Whether document is publicly accessible

        // Security and validation
        public bool IsVirusScanned { get; set; } = false;
        public bool PassedVirusScan { get; set; } = false;
        public DateTime? VirusScanDate { get; set; }

        // Foreign keys
        public int CertificateId { get; set; }
        public int? UploadedBy { get; set; }

        // Audit fields
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Certificate Certificate { get; set; } = null!;
        public User? UploadedByUser { get; set; }
    }
}
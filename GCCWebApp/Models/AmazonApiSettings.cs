using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class AmazonApiSettings
    {
        [Key]
        public int Id { get; set; }

        // Integration Status
        public bool IsEnabled { get; set; } = false;

        public int SyncIntervalHours { get; set; } = 24;

        // Amazon SP-API Credentials
        [Required]
        [StringLength(500)]
        public required string AccessKeyId { get; set; }

        [Required]
        [StringLength(500)]
        public required string SecretAccessKey { get; set; }

        [Required]
        [StringLength(1000)]
        public required string RefreshToken { get; set; }

        [Required]
        [StringLength(10)]
        public required string Region { get; set; } // na, eu, fe

        [Required]
        [StringLength(50)]
        public required string MarketplaceId { get; set; }

        // Sync Options
        public bool SyncProducts { get; set; } = true;

        public bool SyncCertificates { get; set; } = true;

        public bool SyncCompliance { get; set; } = true;

        [StringLength(500)]
        public string? WebhookUrl { get; set; }

        // Status Tracking
        public DateTime? LastSync { get; set; }

        [StringLength(20)]
        public string LastSyncStatus { get; set; } = "idle"; // idle, running, success, error

        public int RecordsSynced { get; set; } = 0;

        [StringLength(1000)]
        public string? LastError { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }
}

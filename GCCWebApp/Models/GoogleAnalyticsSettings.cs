using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class GoogleAnalyticsSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string TrackingId { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = false;

        public string? CustomScript { get; set; }

        public bool AnonymizeIp { get; set; } = true;

        public bool EnhancedLinkAttribution { get; set; } = false;

        public bool DebugMode { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }

        // Navigation property
        public User? UpdatedByUser { get; set; }
    }
}

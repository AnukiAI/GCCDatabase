using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ApiKey
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Key { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [StringLength(50)]
        public string CreatedBy { get; set; }

        public int? RateLimitPerMinute { get; set; } = 60;

        public int? RateLimitPerHour { get; set; } = 1000;

        public string AllowedIPs { get; set; }

        public string AllowedOrigins { get; set; }

        public string Scopes { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }
}
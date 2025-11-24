using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ApiKey
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [Required]
        [StringLength(500)]
        public required string Key { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [StringLength(50)]
        public string? CreatedBy { get; set; }

        public int? RateLimitPerMinute { get; set; } = 60;

        public int? RateLimitPerHour { get; set; } = 1000;

        public int? RateLimitPerDay { get; set; } = 10000;

        public string? AllowedIPs { get; set; }

        public string? AllowedOrigins { get; set; }

        public string? Scopes { get; set; }

        // Security enhancements
        public string? KeyHash { get; set; } // Store hashed version of the key
        
        public int? MaxConcurrentRequests { get; set; } = 10;
        
        public bool RequireTLS { get; set; } = true;
        
        public DateTime? LastSecurityIncident { get; set; }
        
        public int SecurityViolationCount { get; set; } = 0;
        
        public bool IsTemporarilyBlocked { get; set; } = false;
        
        public DateTime? BlockedUntil { get; set; }
        
        public string? UserAgent { get; set; } // Track which client is using this key
        
        // Additional properties needed by ApiKeyService
        public int? RateLimit { get; set; } = 100; // Default rate limit per hour
        
        public int SecurityViolations { get; set; } = 0;
        
        public DateTime? LastViolationAt { get; set; }
        
        public string? LastViolationType { get; set; }
        
        public string? BlockReason { get; set; }
        
        public int RequestCount { get; set; } = 0;
        
        public DateTime? LastRotatedAt { get; set; }
        
        // Business properties
        public string Environment { get; set; } = "Production"; // Production, Staging, Development
        
        public decimal? UsageCostPerRequest { get; set; }
        
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
        
        public bool IsBlocked => IsTemporarilyBlocked && BlockedUntil.HasValue && BlockedUntil.Value > DateTime.UtcNow;
        
        public bool CanBeUsed => IsActive && !IsExpired && !IsBlocked && SecurityViolationCount < 10;
    }
}
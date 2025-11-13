using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Partner-specific API key for accessing GCC API endpoints
    /// Tracks usage and permissions for external partners (Amazon, Wayfair, etc.)
    /// </summary>
    public class PartnerApiKey
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Partner this API key belongs to
        /// </summary>
        [Required]
        [ForeignKey("Partner")]
        public int PartnerId { get; set; }
        public virtual Partner? Partner { get; set; }

        /// <summary>
        /// The actual API key (stored encrypted)
        /// </summary>
        [Required]
        [MaxLength(512)]
        public required string ApiKey { get; set; }

        /// <summary>
        /// Human-readable description/label for this key
        /// </summary>
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this API key is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this API key was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created this API key
        /// </summary>
        [MaxLength(255)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When this API key expires (null = never expires)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// When this API key was last used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Total number of API requests made with this key
        /// </summary>
        public int RequestCount { get; set; } = 0;

        /// <summary>
        /// Rate limit for this partner (requests per hour)
        /// null = no rate limit
        /// </summary>
        public int? RateLimitPerHour { get; set; }

        /// <summary>
        /// JSON array of allowed endpoint patterns
        /// Example: ["/api/v1/certificates/*", "/api/v1/products/*"]
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? AllowedEndpoints { get; set; }

        /// <summary>
        /// JSON object for additional custom permissions
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Permissions { get; set; }

        /// <summary>
        /// Notes about this API key (for admin reference)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }

        /// <summary>
        /// When this API key was revoked (if applicable)
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// User who revoked this API key
        /// </summary>
        [MaxLength(255)]
        public string? RevokedBy { get; set; }
    }
}

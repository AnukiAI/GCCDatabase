using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Number { get; set; } = string.Empty; // Certificate number

        [MaxLength(50)]
        public string Status { get; set; } = "Draft"; // Certificate lifecycle status - aligned with DATA_VALIDATION_RULES.md

        public DateTime EffectiveDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        [MaxLength(175)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public string? Asin { get; set; } // Amazon Standard ID

        public string? Upc { get; set; } // Universal Product Code

        public string? Gtin { get; set; } // Global Trade Item Number

        public string? Ean { get; set; } // European Article Number

        public string? CertificateFile { get; set; } // PDF certificate file path

        [MaxLength(500)]
        public string? CertificateImagePath { get; set; } // Certificate preview image path (PNG/JPG)

        [MaxLength(100)]
        public string? Partner { get; set; } = "None"; // Partner program: Amazon, Novi, None

        [MaxLength(1000)]
        public string? CertificationResults { get; set; } // Optional: Test results/scores for partner requirements (e.g., Amazon threshold verification)

        // Foreign Keys
        public int CompanyId { get; set; }
        public int ProductId { get; set; }

        /// <summary>
        /// Parent certification type (e.g., "Certified Energy Savings"). Required.
        /// </summary>
        public int CertificationTypeId { get; set; }

        /// <summary>
        /// Certification type subcategory (e.g., "Consumer Behavior"). Optional.
        /// Must belong to the selected CertificationType's children.
        /// </summary>
        public int? CertificationTypeSubcategoryId { get; set; }

        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Company Company { get; set; } = null!;
        public Product Product { get; set; } = null!;

        /// <summary>
        /// Parent certification type (required)
        /// </summary>
        public CertificationType CertificationType { get; set; } = null!;

        /// <summary>
        /// Certification type subcategory (optional)
        /// </summary>
        public CertificationType? CertificationTypeSubcategory { get; set; }

        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        // Many-to-many relationships
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Partner> Partners { get; set; } = new List<Partner>(); // Multi-partner support
        public ICollection<CustomFieldValue> CustomFieldValues { get; set; } = new List<CustomFieldValue>();
        
        // Document relationships
        public ICollection<CertificateDocument> Documents { get; set; } = new List<CertificateDocument>();

        // Product ID relationships - supports multiple ASINs, UPCs, etc.
        public ICollection<CertificateProductId> ProductIds { get; set; } = new List<CertificateProductId>();

        // Status history for audit trail
        public ICollection<CertificateStatusHistory> StatusHistory { get; set; } = new List<CertificateStatusHistory>();
    }
}
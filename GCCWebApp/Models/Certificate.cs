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

        /// <summary>
        /// CR-038: marks a MIGRATED certificate that is permitted to share its Number with another
        /// certificate. The client issued some numbers to more than one certificate (137 numbers /
        /// 312 records, 128 of them with genuinely different signed documents), and those numbers
        /// cannot be changed after issue.
        ///
        /// This is a grandfather flag, NOT a general relaxation:
        ///   * the UNIQUE index on Number is FILTERED to `WHERE AllowsDuplicateNumber = 0`, so the
        ///     database still guarantees uniqueness for every ordinary certificate;
        ///   * new certificates are created with 0 and can never take an existing number.
        /// Set to 1 ONLY by the migration loader, and only on the rows that actually participate in
        /// a duplicated number — never on the migration as a whole.
        /// </summary>
        public bool AllowsDuplicateNumber { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Draft"; // Certificate lifecycle status - aligned with DATA_VALIDATION_RULES.md

        public DateTime EffectiveDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        [MaxLength(175)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        // CR-187: persisted computed search-normalized forms (lowercase, trademark/punctuation/
        // whitespace stripped — see Search.SearchNormalizer). Read-only on the DB; SQL maintains
        // them on every write. Used for user-friendly search on certificate number/location/country.
        public string? NormalizedNumber { get; private set; }
        public string? NormalizedLocation { get; private set; }
        public string? NormalizedCountry { get; private set; }

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

        // DEF-181: persisted computed column that normalizes CertificationResults to a numeric percentage
        // so sorting orders values the way users intuitively read them. Raw values are mixed format
        // (e.g. "95%", "100", "0.69") and lex sort produced unintuitive output ("95%" > "100" > "1" > "0.69").
        // Mirrors DisplayFormatters.FormatCertificationResults: contains % → strip and use; [0,1) → ×100;
        // ≥1 → as-is; non-numeric → NULL (sorts as default per direction). Column is read-only on the DB.
        [Column(TypeName = "decimal(18,4)")]
        public decimal? CertificationResultsNumeric { get; private set; }

        public int? PathwayId { get; set; }

        // Foreign Keys
        public int CompanyId { get; set; }
        public int ProductId { get; set; }

        /// <summary>
        /// Product category (e.g., "Plastics", "Paper"). Optional.
        /// Stored on certificate for independent tracking from Product.
        /// </summary>
        public int? ProductCategoryId { get; set; }

        /// <summary>
        /// Product type - Level 1 (e.g., "HDPE", "LDPE"). Optional.
        /// References ProductTypes where ParentId IS NULL.
        /// </summary>
        public int? ProductTypeId { get; set; }

        /// <summary>
        /// Product subcategory - Level 2 child of ProductType. Optional.
        /// References ProductTypes where ParentId IS NOT NULL.
        /// </summary>
        public int? ProductSubcategoryId { get; set; }

        /// <summary>
        /// Parent certification type (e.g., "Certified Energy Savings"). Required.
        /// </summary>
        public int CertificationTypeId { get; set; }

        /// <summary>
        /// Certification type subcategory (e.g., "Consumer Behavior"). Optional.
        /// Must belong to the selected CertificationType's children.
        /// DEPRECATED: Use CertificationTypeSubcategories collection instead for multi-select.
        /// Kept for backwards compatibility during migration.
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
        /// DEPRECATED: Use CertificationTypeSubcategories collection for multi-select.
        /// </summary>
        public CertificationType? CertificationTypeSubcategory { get; set; }

        /// <summary>
        /// Multiple certification type subcategories (optional).
        /// All must belong to the selected CertificationType's children.
        /// </summary>
        public ICollection<CertificationType> CertificationTypeSubcategories { get; set; } = new List<CertificationType>();

        public ProductCategory? ProductCategory { get; set; }
        public ProductType? ProductType { get; set; }
        public ProductType? ProductSubcategory { get; set; }

        public Pathway? Pathway { get; set; }
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        // Many-to-many relationships
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Partner> Partners { get; set; } = new List<Partner>(); // Multi-partner support

        // CR-037: full set of associated countries, primary included. Country above stays the
        // denormalized primary (sort key, CSV column, non-partner API projection).
        public ICollection<CertificateCountry> Countries { get; set; } = new List<CertificateCountry>();
        public ICollection<CustomFieldValue> CustomFieldValues { get; set; } = new List<CustomFieldValue>();
        
        // Document relationships
        public ICollection<CertificateDocument> Documents { get; set; } = new List<CertificateDocument>();

        // Product ID relationships - supports multiple ASINs, UPCs, etc.
        public ICollection<CertificateProductId> ProductIds { get; set; } = new List<CertificateProductId>();

        // Status history for audit trail
        public ICollection<CertificateStatusHistory> StatusHistory { get; set; } = new List<CertificateStatusHistory>();
    }
}
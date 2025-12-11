using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Represents a product identifier (ASIN, UPC, GTIN, EAN) associated with a certificate
    /// Supports multiple product IDs per certificate for Amazon marketplace integration
    /// </summary>
    public class CertificateProductId
    {
        public int Id { get; set; }

        [Required]
        public int CertificateId { get; set; }

        [Required]
        [MaxLength(10)]
        public string ProductIdType { get; set; } = string.Empty; // ASIN, UPC, GTIN, EAN

        [Required]
        [MaxLength(50)]
        public string ProductIdValue { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Pending, Expired

        [MaxLength(50)]
        public string? Partner { get; set; } // Amazon, Novi, None, Other

        [MaxLength(200)]
        public string? ProductName { get; set; } // Optional product name for this specific ID

        [MaxLength(100)]
        public string? ProductSku { get; set; } // Optional SKU

        [MaxLength(500)]
        public string? Description { get; set; } // Optional description or notes

        public bool IsActive { get; set; } = true;

        public bool IsPublic { get; set; } = true; // Whether this product ID is publicly visible

        public int? DisplayOrder { get; set; } // For ordering product IDs in UI

        // Audit fields
        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Certificate Certificate { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
    }

    /// <summary>
    /// Enumeration of supported product ID types
    /// </summary>
    public enum ProductIdType
    {
        ASIN,   // Amazon Standard Identification Number
        UPC,    // Universal Product Code
        GTIN,   // Global Trade Item Number
        EAN,    // European Article Number
        ISBN,   // International Standard Book Number
        SKU,    // Stock Keeping Unit
        MPN,    // Manufacturer Part Number
        Other   // Custom identifier
    }

    /// <summary>
    /// ViewModel for bulk product ID operations
    /// </summary>
    public class BulkProductIdViewModel
    {
        public int CertificateId { get; set; }
        public List<ProductIdImportItem> ProductIds { get; set; } = new();
        public bool OverwriteExisting { get; set; } = false;
        public bool ValidateFormats { get; set; } = true;
        public string? DefaultPartner { get; set; }
        public string? DefaultStatus { get; set; } = "Active";
    }

    /// <summary>
    /// Individual product ID for import operations
    /// </summary>
    public class ProductIdImportItem
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Partner { get; set; }
        public string? Status { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public string? Description { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public bool IsValid => !ValidationErrors.Any();
    }

    /// <summary>
    /// Result of bulk import operation
    /// </summary>
    public class BulkImportResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsSuccess => ErrorCount == 0;
    }
}
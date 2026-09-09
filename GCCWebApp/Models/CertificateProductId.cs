using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

        // Status is auto-derived from parent certificate status (CR-015).
        // Not user-editable: "Active" iff parent cert is in BadgeableStatuses, else "Inactive".
        // CR-043 widened that set to include Confidential — see DeriveStatusFromCert.
        [MaxLength(20)]
        public string Status { get; set; } = "Inactive"; // Auto-set on save; default Inactive until cascade runs.

        [MaxLength(50)]
        public string? Partner { get; set; } // Denormalized PRIMARY partner (first of Partners) — kept for
                                              // display/sort/CSV/back-compat. CR-171: real association is the
                                              // Partners collection below (Model A: API sharing by partner).

        // CR-171 Phase 2: a product ID may be shared with multiple partners. Model A — a partner
        // receives a cert's product IDs that are EITHER untagged (empty collection = visible to all
        // the cert's partners) OR tagged to that partner. Join table: CertificateProductIdPartners.
        [ValidateNever]
        public ICollection<Partner> Partners { get; set; } = new List<Partner>();

        // Widened 200 -> 1000 for migration: retailer product titles in the client ASIN data run up
        // to ~951 chars and are passed to Amazon, so truncation is not acceptable (client directive).
        [MaxLength(1000)]
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

        // Navigation properties (excluded from model binding validation)
        [ValidateNever]
        public Certificate Certificate { get; set; } = null!;
        [ValidateNever]
        public User? CreatedByUser { get; set; }
        [ValidateNever]
        public User? UpdatedByUser { get; set; }

        /// <summary>
        /// Derives the product-ID status from its parent certificate's status (CR-015).
        /// "Active" when the cert is in BadgeableStatuses (Client Approved - Paid, Client Approved -
        /// Confidential, Republished); "Inactive" for every other state (Draft, Hold, Expired, Not
        /// Paid, etc.).
        ///
        /// CR-043 moved this off PublicStatuses. It is not cosmetic: the identifiers on a
        /// Confidential certificate are now emitted by the "Active Product Identifiers" export and
        /// sent to Amazon, so leaving their own Status reading "Inactive" would put a visible
        /// contradiction in front of the client on the Product Identifiers grid — the same row,
        /// labelled inactive, sitting inside the active file.
        ///
        /// Existing rows were written under the old rule and do NOT recompute themselves — this
        /// runs on save. Backfill with Scripts/data-ops/2026-09-08-CR043-productid-status-backfill.sql
        /// in the same release.
        /// </summary>
        public static string DeriveStatusFromCert(string? certStatus)
        {
            return CertificateStatusHelper.BadgeableStatuses.Contains(certStatus ?? string.Empty)
                ? "Active"
                : "Inactive";
        }
    }

    /// <summary>
    /// DEF-121 follow-up: explicit response shape for /api/productids endpoints.
    /// Replaces returning the raw CertificateProductId entity, which transitively
    /// pulled the full User entity for CreatedByUser/UpdatedByUser. The User entity
    /// has accumulated credential-tier fields (PasswordHash, TwoFactorSecret,
    /// RecoveryCodes, SecurityQuestions, AllowedIPs, PasswordResetToken,
    /// PasswordHistory) that must never serialize to API responses. The [JsonIgnore]
    /// stopgap on User.cs already blocks those, but this DTO eliminates the entire
    /// User surface — defense in depth, and durable against new fields being added
    /// to User without remembering to mark them.
    /// </summary>
    public class ProductIdResponseDto
    {
        public int Id { get; set; }
        public int CertificateId { get; set; }
        public string ProductIdType { get; set; } = string.Empty;
        public string ProductIdValue { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Partner { get; set; }
        // CR-171: all associated partner names (multi). The single Partner above is the primary.
        public List<string> Partners { get; set; } = new();
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public int? DisplayOrder { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ProductIdUserDto? CreatedByUser { get; set; }
        public ProductIdUserDto? UpdatedByUser { get; set; }

        public static ProductIdResponseDto FromEntity(CertificateProductId p) => new()
        {
            Id = p.Id,
            CertificateId = p.CertificateId,
            ProductIdType = p.ProductIdType,
            ProductIdValue = p.ProductIdValue,
            Status = p.Status,
            Partner = p.Partner,
            Partners = (p.Partners ?? new List<Partner>()).Select(pp => pp.Name).OrderBy(n => n).ToList(),
            ProductName = p.ProductName,
            ProductSku = p.ProductSku,
            Description = p.Description,
            IsActive = p.IsActive,
            IsPublic = p.IsPublic,
            DisplayOrder = p.DisplayOrder,
            CreatedBy = p.CreatedBy,
            CreatedAt = p.CreatedAt,
            UpdatedBy = p.UpdatedBy,
            UpdatedAt = p.UpdatedAt,
            CreatedByUser = p.CreatedByUser != null
                ? new ProductIdUserDto { Id = p.CreatedByUser.Id, Username = p.CreatedByUser.Username }
                : null,
            UpdatedByUser = p.UpdatedByUser != null
                ? new ProductIdUserDto { Id = p.UpdatedByUser.Id, Username = p.UpdatedByUser.Username }
                : null,
        };
    }

    /// <summary>
    /// Minimal user shape exposed on Product ID responses — just enough for the
    /// Created By / Updated By columns. Never includes credential or contact fields.
    /// </summary>
    public class ProductIdUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
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
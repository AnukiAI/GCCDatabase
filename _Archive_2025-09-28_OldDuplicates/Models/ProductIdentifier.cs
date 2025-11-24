using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ProductIdentifier
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [MaxLength(20)]
        public string IdentifierType { get; set; } = string.Empty; // ASIN, UPC, GTIN, EAN, etc.

        [Required]
        [MaxLength(50)]
        public string IdentifierValue { get; set; } = string.Empty;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Product Product { get; set; } = null!;
    }

    public enum ProductIdentifierType
    {
        ASIN,
        UPC,
        GTIN,
        EAN,
        ISBN,
        ISSN,
        SKU,
        MPN // Manufacturer Part Number
    }
}
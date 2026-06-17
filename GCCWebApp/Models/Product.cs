using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; } = string.Empty;

        // CR-187: persisted computed column holding the search-normalized form of Name
        // (lowercase, trademark/punctuation/whitespace stripped — see Search.SearchNormalizer).
        // Read-only on the DB; SQL maintains it on every write. Used for user-friendly search.
        public string? NormalizedName { get; private set; }

        public string? Description { get; set; }

        // Product identification fields
        [MaxLength(100)]
        public string? Model { get; set; }

        [MaxLength(50)]
        public string? Sku { get; set; }

        [MaxLength(50)]
        public string? Upc { get; set; }

        [MaxLength(50)]
        public string? Asin { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? ProductSubcategoryId { get; set; }
        public int? CompanyId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ProductCategory Category { get; set; } = null!;
        public ProductType? ProductType { get; set; }
        // ProductSubcategory FK targets ProductTypes (Level-2 child rows), matching Certificate.ProductSubcategory.
        // The legacy ProductSubcategories table is unused for Product.
        public ProductType? ProductSubcategory { get; set; }
        public Company? Company { get; set; }

        // Collections
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<ProductIdentifier> ProductIdentifiers { get; set; } = new List<ProductIdentifier>();
    }
}
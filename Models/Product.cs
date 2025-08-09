using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? ProductSubcategoryId { get; set; }
        public int? CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ProductCategory Category { get; set; } = null!;
        public ProductType? ProductType { get; set; }
        public ProductSubcategory? ProductSubcategory { get; set; }
        public Company? Company { get; set; }

        // Collections
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
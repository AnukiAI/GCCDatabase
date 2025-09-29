using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ProductType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        
        public int? ParentId { get; set; }

        public int Level { get; set; } = 0;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ProductCategory Category { get; set; } = null!;
        
        public ProductType? Parent { get; set; }
        public ICollection<ProductType> Children { get; set; } = new List<ProductType>();
        
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<ProductSubcategory> ProductSubcategories { get; set; } = new List<ProductSubcategory>();
    }
}
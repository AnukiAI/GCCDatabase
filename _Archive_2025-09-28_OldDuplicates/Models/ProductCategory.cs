using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();
    }
}
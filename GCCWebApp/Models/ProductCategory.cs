using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        // CR-187: persisted computed search-normalized form of Name (see Search.SearchNormalizer).
        public string? NormalizedName { get; private set; }

        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<ProductType> ProductTypes { get; set; } = new List<ProductType>();
    }
}
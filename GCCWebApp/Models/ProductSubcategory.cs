using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ProductSubcategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public int ProductTypeId { get; set; }

        public string? Description { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ProductType ProductType { get; set; } = null!;
    }
}
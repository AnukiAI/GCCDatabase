using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string TagType { get; set; } = string.Empty; // internal/external

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties for many-to-many relationships
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    // Separate classes for more specific tag types based on ERD
    public class CSFAttribute : Tag
    {
        // Additional properties specific to CSF attributes can be added here
    }

    public class ExternalTag : Tag
    {
        // Constructor to set tag type
        public ExternalTag()
        {
            TagType = "external";
        }
    }

    public class InternalTag : Tag
    {
        // Constructor to set tag type
        public InternalTag()
        {
            TagType = "internal";
        }
    }
}
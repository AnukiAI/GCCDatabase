using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Pathway
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Partner
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Url]
        [MaxLength(500)]
        public string? Website { get; set; }

        [MaxLength(255)]
        public string? ContactInfo { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Company> Companies { get; set; } = new List<Company>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
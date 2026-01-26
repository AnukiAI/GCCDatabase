using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ContactInfo { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Url]
        [MaxLength(500)]
        public string? Website { get; set; }

        [MaxLength(175)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Partner association (for reporting - which partner program this company belongs to)
        public int? PartnerId { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Partner? Partner { get; set; }
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
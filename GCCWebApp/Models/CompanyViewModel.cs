using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class CompanyViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Url]
        [MaxLength(500)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(175)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(255)]
        public string? ContactInfo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
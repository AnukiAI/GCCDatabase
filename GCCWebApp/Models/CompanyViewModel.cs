using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class CompanyViewModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        // QC #181 alignment: same strictness as CertificateEditViewModel.CompanyEmail.
        // Built-in [EmailAddress] is too permissive (accepts "a@b" with no TLD); the
        // previous regex here allowed "a@b.c" (1-char TLD). New pattern requires
        // name@domain.tld with a 2+ char TLD.
        [RegularExpression(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid email address (e.g., name@example.com).")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL (e.g., https://example.com)")]
        [MaxLength(500)]
        public string? Url { get; set; }

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
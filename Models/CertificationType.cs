using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class CertificationType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AmazonCode { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        // Navigation properties
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<CustomField> CustomFields { get; set; } = new List<CustomField>();
    }
}
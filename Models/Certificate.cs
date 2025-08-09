using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(7)]
        public string Number { get; set; } = string.Empty; // Certificate number

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active/expired/pending

        public DateTime EffectiveDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        [MaxLength(175)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public string? Asin { get; set; } // Amazon Standard ID

        public string? Upc { get; set; } // Universal Product Code

        public string? Gtin { get; set; } // Global Trade Item Number

        public string? Ean { get; set; } // European Article Number

        public string? CertificateFile { get; set; } // File path

        // Foreign Keys
        public int CompanyId { get; set; }
        public int ProductId { get; set; }
        public int CertificationTypeId { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Company Company { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public CertificationType CertificationType { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }

        // Many-to-many relationships
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<CustomFieldValue> CustomFieldValues { get; set; } = new List<CustomFieldValue>();
    }
}
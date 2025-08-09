using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class CustomField
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FieldType { get; set; } = string.Empty; // text/number/date/boolean/select

        public bool IsRequired { get; set; } = false;

        public bool IsVisible { get; set; } = true;

        public bool IsPublic { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public string? Options { get; set; } // JSON for select field options

        public string? DefaultValue { get; set; }

        public string? ValidationRules { get; set; } // JSON validation rules

        public int CertificateTypeId { get; set; }

        // Navigation properties
        public CertificationType CertificationType { get; set; } = null!;
        public ICollection<CustomFieldValue> CustomFieldValues { get; set; } = new List<CustomFieldValue>();
    }
}
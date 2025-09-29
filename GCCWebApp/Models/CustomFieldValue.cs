using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    public class CustomFieldValue
    {
        public int Id { get; set; }

        public int CertificateId { get; set; }

        public int FieldId { get; set; }

        public string? TextValue { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? NumberValue { get; set; }

        public DateTime? DateValue { get; set; }

        public bool? BooleanValue { get; set; }

        // Navigation properties
        public Certificate Certificate { get; set; } = null!;
        public CustomField Field { get; set; } = null!;
    }
}
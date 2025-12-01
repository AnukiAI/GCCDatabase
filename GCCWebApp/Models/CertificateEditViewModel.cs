using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// ViewModel for creating and editing certificates
    /// </summary>
    public class CertificateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Certificate Number")]
        [MaxLength(7)]
        public string Number { get; set; } = string.Empty;

        [Display(Name = "Status")]
        [Required]
        public string Status { get; set; } = "active";

        [Display(Name = "Partner Program")]
        [MaxLength(100)]
        public string? Partner { get; set; } = "None";

        [Display(Name = "Certification Results")]
        [MaxLength(1000)]
        public string? CertificationResults { get; set; }

        [Required]
        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        [Required]
        [Display(Name = "Expiration Date")]
        [DataType(DataType.Date)]
        public DateTime ExpirationDate { get; set; }

        [Display(Name = "Location")]
        [MaxLength(175)]
        public string? Location { get; set; }

        [Display(Name = "Country")]
        [MaxLength(100)]
        public string? Country { get; set; }

        [Display(Name = "ASIN")]
        public string? Asin { get; set; }

        [Display(Name = "UPC")]
        public string? Upc { get; set; }

        [Display(Name = "GTIN")]
        public string? Gtin { get; set; }

        [Display(Name = "EAN")]
        public string? Ean { get; set; }

        [Display(Name = "Certificate Image")]
        [MaxLength(500)]
        public string? CertificateImagePath { get; set; }

        /// <summary>
        /// Product IDs associated with this certificate (managed by Product ID Manager)
        /// This is for the new Product ID Manager functionality
        /// </summary>
        public List<CertificateProductId> ProductIds { get; set; } = new List<CertificateProductId>();

        /// <summary>
        /// Custom field values associated with this certificate
        /// Dictionary: Key = CustomField.Id, Value = Field value as string
        /// </summary>
        public Dictionary<int, string> CustomFieldValues { get; set; } = new Dictionary<int, string>();

        [Required]
        [Display(Name = "Company")]
        public int CompanyId { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Certification Type")]
        public int CertificationTypeId { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// ViewModel for creating and editing certificates
    /// </summary>
    public class CertificateEditViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Certificate Number")]
        [MaxLength(50)]
        public string Number { get; set; } = string.Empty;

        [Display(Name = "Status")]
        [Required]
        public string Status { get; set; } = "Draft";

        [Display(Name = "Partner Program")]
        [MaxLength(100)]
        public string? Partner { get; set; } = "None";

        /// <summary>
        /// Partner IDs for multi-partner support
        /// Comma-separated list from the UI, parsed to list of IDs
        /// </summary>
        [Display(Name = "Partners")]
        public string? PartnerIds { get; set; }

        [Display(Name = "Certification Results")]
        [MaxLength(1000)]
        public string? CertificationResults { get; set; }

        [Display(Name = "Pathway")]
        public int? PathwayId { get; set; }

        [Required]
        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveDate { get; set; }

        [Required]
        [Display(Name = "Expiration Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpirationDate { get; set; }

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

        [Display(Name = "Company Email")]
        // QC #181: built-in [EmailAddress] is too permissive (accepts "a@b" with no TLD and
        // some other obviously-bad inputs). Stricter regex requires name@domain.tld with a
        // 2+ character TLD. Regex is emitted as data-val-regex by unobtrusive validation, so
        // it enforces client-side too (the form sets novalidate to disable HTML5 feedback,
        // but jquery.validate still runs). Optional field — null/empty still passes.
        [RegularExpression(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid email address (e.g., name@example.com).")]
        [MaxLength(255)]
        public string? CompanyEmail { get; set; }

        [Display(Name = "Company URL")]
        [MaxLength(500)]
        public string? CompanyWebsite { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Certification Type")]
        public int CertificationTypeId { get; set; }

        [Display(Name = "Certification Type Subcategory")]
        public int? CertificationTypeSubcategoryId { get; set; }

        [Display(Name = "Product Category")]
        public int? ProductCategoryId { get; set; }

        [Display(Name = "Product Type")]
        public int? ProductTypeId { get; set; }

        [Display(Name = "Product Subcategory")]
        public int? ProductSubcategoryId { get; set; }

        /// <summary>
        /// Cross-field rule: a certificate's Expiration Date must be after its Effective Date.
        /// Both fields are independently [Required]; this guards the relationship between them.
        /// Runs server-side on both Create and Edit POST (both gate on ModelState.IsValid). The
        /// client mirrors this in each view's submit handler so the analyst sees it before posting.
        /// Compared on .Date so the time component (always 00:00 for date-only inputs) is ignored.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EffectiveDate.HasValue && ExpirationDate.HasValue &&
                ExpirationDate.Value.Date <= EffectiveDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Expiration Date must be after the Effective Date.",
                    new[] { nameof(ExpirationDate) });
            }
        }
    }
}
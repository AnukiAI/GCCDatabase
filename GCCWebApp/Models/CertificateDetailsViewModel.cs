using System.ComponentModel.DataAnnotations;

namespace GreenCircleCertified.ViewModels
{
    public class CertificateDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Certificate Number")]
        public string CertificateNumber { get; set; } = string.Empty;

        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Certification Type")]
        public string CertificationType { get; set; } = string.Empty;

        [Display(Name = "Product Category")]
        public string ProductCategory { get; set; } = string.Empty;

        [Display(Name = "Product Type")]
        public string ProductType { get; set; } = string.Empty;

        [Display(Name = "Product Subcategory")]
        public string? ProductSubcategory { get; set; }

        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; }

        [Display(Name = "Expiration Date")]
        public DateTime ExpirationDate { get; set; }

        [Display(Name = "Company Contact")]
        public string? CompanyContact { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Certificate Image")]
        public string CertificateImageUrl { get; set; } = string.Empty;

        [Display(Name = "Additional Information")]
        public string? AdditionalInfo { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Last Modified By")]
        public string? LastModifiedBy { get; set; }

        [Display(Name = "Last Modified Date")]
        public DateTime? LastModifiedDate { get; set; }

        // Related collections
        public List<CertificateTag> Tags { get; set; } = new();
        public List<CertificationField> CertificationFields { get; set; } = new();
        public List<CSFAttribute> CSFAttributes { get; set; } = new();
        public List<AuditTrailEntry> AuditTrail { get; set; } = new();
        public List<RelatedDocument> RelatedDocuments { get; set; } = new();
        public List<InternalNote> InternalNotes { get; set; } = new();

        // Computed properties
        public bool IsExpired => ExpirationDate < DateTime.Now;
        public bool IsExpiringSoon => ExpirationDate < DateTime.Now.AddDays(30) && !IsExpired;
        public int DaysUntilExpiration => (int)(ExpirationDate - DateTime.Now).TotalDays;
        public bool IsActive => Status.Equals("active", StringComparison.OrdinalIgnoreCase);
    }
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GCCWebApp.Models.Api.Requests
{
    public class CreateCertificateRequest
    {
        [Required(ErrorMessage = "Certificate number is required")]
        [StringLength(100, ErrorMessage = "Certificate number must not exceed 100 characters")]
        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive number")]
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Company ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Company ID must be a positive number")]
        [JsonPropertyName("companyId")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Certification Type ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Certification Type ID must be a positive number")]
        [JsonPropertyName("certificationTypeId")]
        public int CertificationTypeId { get; set; }

        [Required(ErrorMessage = "Effective date is required")]
        [JsonPropertyName("effectiveDate")]
        public DateTime EffectiveDate { get; set; }

        [JsonPropertyName("expirationDate")]
        public DateTime? ExpirationDate { get; set; }

        [StringLength(50, ErrorMessage = "Status must not exceed 50 characters")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";

        [StringLength(500, ErrorMessage = "Certificate file path must not exceed 500 characters")]
        [JsonPropertyName("certificateFile")]
        public string? CertificateFile { get; set; }

        [JsonPropertyName("customFields")]
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public class UpdateCertificateRequest : CreateCertificateRequest
    {
        [Required(ErrorMessage = "Certificate ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Certificate ID must be a positive number")]
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class CertificateSearchRequest : PaginationParams
    {
        [JsonPropertyName("term")]
        public new string? SearchTerm { get; set; }

        [JsonPropertyName("productId")]
        public int? ProductId { get; set; }

        [JsonPropertyName("companyId")]
        public int? CompanyId { get; set; }

        [JsonPropertyName("certificationTypeId")]
        public int? CertificationTypeId { get; set; }

        [JsonPropertyName("effectiveDateFrom")]
        public DateTime? EffectiveDateFrom { get; set; }

        [JsonPropertyName("effectiveDateTo")]
        public DateTime? EffectiveDateTo { get; set; }

        [JsonPropertyName("expirationDateFrom")]
        public DateTime? ExpirationDateFrom { get; set; }

        [JsonPropertyName("expirationDateTo")]
        public DateTime? ExpirationDateTo { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("includeExpired")]
        public bool IncludeExpired { get; set; } = false;
    }

    public class ExpiringCertificatesRequest : PaginationParams
    {
        [Range(1, 365, ErrorMessage = "Days must be between 1 and 365")]
        [JsonPropertyName("days")]
        public int Days { get; set; } = 30;

        [JsonPropertyName("includeExpired")]
        public bool IncludeExpired { get; set; } = false;
    }
}
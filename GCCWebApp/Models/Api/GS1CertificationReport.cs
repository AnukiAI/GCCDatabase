using System.Text.Json.Serialization;

namespace GCCWebApp.Models.Api
{
    /// <summary>
    /// GS1 JSON-LD Certification Report format for Amazon integration.
    /// Follows GS1 Web Vocabulary specification.
    /// </summary>
    public class GS1CertificationReport
    {
        /// <summary>
        /// JSON-LD context URI pointing to the GS1 certification context file.
        /// </summary>
        [JsonPropertyName("@context")]
        public string Context { get; set; } = "https://gs1us-technology.github.io/certification-data-model/cert_context.json";

        /// <summary>
        /// JSON Schema URI for validation.
        /// </summary>
        [JsonPropertyName("$schema")]
        public string Schema { get; set; } = "https://gs1us-technology.github.io/certification-data-model/cert_schema.json";

        /// <summary>
        /// A globally unique URI describing this certification report document.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Type declaration. Must be "CertificationReport".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "CertificationReport";

        /// <summary>
        /// First date of validity for the certification. Format: YYYY-MM-DD
        /// </summary>
        [JsonPropertyName("issueDate")]
        public string? IssueDate { get; set; }

        /// <summary>
        /// GLN or other identifier of document creator.
        /// </summary>
        [JsonPropertyName("issuer")]
        public string? Issuer { get; set; }

        /// <summary>
        /// A list of one or more certified products.
        /// </summary>
        [JsonPropertyName("products")]
        public List<GS1Product> Products { get; set; } = new();
    }

    /// <summary>
    /// GS1 Product object within a certification report.
    /// </summary>
    public class GS1Product
    {
        /// <summary>
        /// Unique identifier describing the certified product. Must be a GS1 Digital Link URI.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Type declaration. Must be "Product".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Product";

        /// <summary>
        /// A Global Trade Item Number (GTIN). The 14 digit GS1 Identification Key.
        /// </summary>
        [JsonPropertyName("gtin")]
        public string? Gtin { get; set; }

        /// <summary>
        /// Additional product identifiers (ASIN, SKU, etc.)
        /// </summary>
        [JsonPropertyName("additionalTradeItemId")]
        public List<GS1AdditionalTradeItemId>? AdditionalTradeItemId { get; set; }

        /// <summary>
        /// Consumer friendly short description of the product.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// An understandable and useable description of a product.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Certification information for this product.
        /// </summary>
        [JsonPropertyName("certificationInfo")]
        public GS1CertificationInfo CertificationInfo { get; set; } = new();
    }

    /// <summary>
    /// Additional trade item identifier (ASIN, SKU, DUNS, etc.)
    /// Modeled on gs1:additionalOrganizationID structure.
    /// </summary>
    public class GS1AdditionalTradeItemId
    {
        /// <summary>
        /// Type of identifier (ASIN, SKU, UPC, EAN, ISBN, MPN, etc.)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The identifier value.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// GS1 CertificationDetails object containing certification information.
    /// </summary>
    public class GS1CertificationInfo
    {
        /// <summary>
        /// A globally unique URI describing this certification.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Type declaration. Must be "CertificationDetails".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "CertificationDetails";

        /// <summary>
        /// Name of the organisation issuing the certification standard.
        /// </summary>
        [JsonPropertyName("agency")]
        public string Agency { get; set; } = "Green Circle Certified";

        /// <summary>
        /// URL of the certification agency.
        /// </summary>
        [JsonPropertyName("agencyURL")]
        public string? AgencyURL { get; set; }

        /// <summary>
        /// The certification name or umbrella initiative (e.g., TCO Certified, Global Recycled Standard).
        /// </summary>
        [JsonPropertyName("program")]
        public string? Program { get; set; }

        /// <summary>
        /// A reference to a vendor specific identifier for the certificate.
        /// </summary>
        [JsonPropertyName("certificateId")]
        public string? CertificateId { get; set; }

        /// <summary>
        /// Name of the certification standard.
        /// </summary>
        [JsonPropertyName("standard")]
        public string? Standard { get; set; }

        /// <summary>
        /// Current status of the certification: ACTIVE or INACTIVE.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "ACTIVE";

        /// <summary>
        /// The certification's standard value or level (e.g., Gold, Silver, Level 1).
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        /// <summary>
        /// First date of validity for the certification. Format: YYYY-MM-DD
        /// </summary>
        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        /// <summary>
        /// Last date of validity for the certification. Format: YYYY-MM-DD
        /// </summary>
        [JsonPropertyName("endDate")]
        public string? EndDate { get; set; }

        /// <summary>
        /// Countries where the certification applies.
        /// </summary>
        [JsonPropertyName("targetCountries")]
        public List<string>? TargetCountries { get; set; }
    }

    /// <summary>
    /// Response wrapper for GS1 certification reports list.
    /// </summary>
    public class GS1CertificationReportListResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("data")]
        public List<GS1CertificationReport> Data { get; set; } = new();

        [JsonPropertyName("pagination")]
        public PaginationMetadata? Pagination { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";
    }
}

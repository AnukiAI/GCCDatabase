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
        [JsonPropertyOrder(1)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Type declaration. Must be "Product".
        /// </summary>
        [JsonPropertyOrder(2)]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "Product";

        /// <summary>
        /// A Global Trade Item Number (GTIN). The 14 digit GS1 Identification Key.
        /// </summary>
        [JsonPropertyOrder(3)]
        [JsonPropertyName("gtin")]
        public string? Gtin { get; set; }

        /// <summary>
        /// Consumer friendly short description of the product.
        /// </summary>
        [JsonPropertyOrder(4)]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Additional product identifiers (ASIN, SKU, etc.)
        /// </summary>
        [JsonPropertyOrder(5)]
        [JsonPropertyName("additionalTradeItemId")]
        public List<GS1AdditionalTradeItemId>? AdditionalTradeItemId { get; set; }

        /// <summary>
        /// Certification information for this product.
        /// </summary>
        [JsonPropertyOrder(6)]
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

        /// <summary>
        /// Optional product name associated with this specific identifier.
        /// Populated from the import CSV ProductName column or the cert edit UI's per-identifier name field.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
    }

    /// <summary>
    /// GS1 CertificationDetails object containing certification information.
    /// </summary>
    public class GS1CertificationInfo
    {
        /// <summary>
        /// Type declaration. Must be "CertificationDetails".
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "CertificationDetails";

        /// <summary>
        /// Name of the organisation issuing the certification standard.
        /// </summary>
        [JsonPropertyName("agency")]
        public string Agency { get; set; } = "GreenCircle Certified";

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

        /// <summary>
        /// Amazon-specific certification code (e.g., "greencircle_energy_savings").
        /// Custom extension for Climate Pledge Friendly integration.
        /// </summary>
        [JsonPropertyName("amazonCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AmazonCode { get; set; }

        /// <summary>
        /// Certification pathway providing context for results (e.g., "Energy usage reduction").
        /// Used by Amazon CPF program.
        /// </summary>
        [JsonPropertyName("pathway")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Pathway { get; set; }
    }

    /// <summary>
    /// Response wrapper for GS1 certification reports list.
    /// </summary>
    public class GS1CertificationReportListResponse
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("pagination")]
        public PaginationMetadata? Pagination { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("data")]
        public List<GS1CertificationReport> Data { get; set; } = new();

        [JsonPropertyOrder(4)]
        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }
    }
}

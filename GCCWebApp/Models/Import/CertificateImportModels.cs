using System;
using System.Collections.Generic;

namespace GCCWebApp.Models.Import
{
    /// <summary>
    /// Result of certificate import preview showing what will change
    /// </summary>
    public class CertificateImportPreviewResult
    {
        public string ImportId { get; set; } = Guid.NewGuid().ToString(); // Track this import session
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int UploadedByUserId { get; set; }
        public string UploadedByUsername { get; set; } = string.Empty;

        // Summary statistics
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int NewCertificates { get; set; }
        public int ModifiedCertificates { get; set; }
        public int UnchangedCertificates { get; set; }

        // Detailed results
        public List<CertificateImportRow> NewRows { get; set; } = new();
        public List<CertificateImportRow> ModifiedRows { get; set; } = new();
        public List<CertificateImportRow> UnchangedRows { get; set; } = new();
        public List<CertificateImportError> ErrorRows { get; set; } = new();

        // Validation summary
        public bool HasErrors => ErrorRows.Count > 0;
        public bool CanApply => ValidRows > 0 && !HasCriticalErrors;
        public bool HasCriticalErrors { get; set; } // Errors that prevent entire import

        // Entity creation summaries (for auto-creation feature)
        public int CompaniesToCreate { get; set; }
        public int ProductsToCreate { get; set; }
        public int ProductTypesToCreate { get; set; }
        public int TagsToCreate { get; set; }
        public int InternalTagsToCreate { get; set; }

        // Lists of entities to be created (for preview display)
        public List<string> NewCompanyNames { get; set; } = new();
        public List<string> NewProductNames { get; set; } = new();
        public List<string> NewProductTypeNames { get; set; } = new();
        public List<string> NewTagNames { get; set; } = new();
        public List<string> NewInternalTagNames { get; set; } = new();

        // Multi-ASIN and PDF summaries
        public int ProductIdsToCreate { get; set; }
        public int PdfFilesToUpload { get; set; }
    }

    /// <summary>
    /// Represents a single row from the CSV import file
    /// </summary>
    public class CertificateImportRow
    {
        public int RowNumber { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CertificationType { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }
        public string? Asin { get; set; }
        public string? Upc { get; set; }
        public string? Gtin { get; set; }
        public string? Ean { get; set; }
        public string? Partner { get; set; }

        // Company details for auto-creation
        public string? CompanyLocation { get; set; }
        public string? CompanyCountry { get; set; }
        public string? CompanyEmail { get; set; }
        public string? CompanyWebsite { get; set; }
        public bool CompanyWillBeCreated { get; set; }

        // Product details for auto-creation
        public string? ProductTypeName { get; set; }
        public int? ProductTypeId { get; set; }
        public bool ProductWillBeCreated { get; set; }
        public bool ProductTypeWillBeCreated { get; set; }

        // Certification results
        public string? CertificationResults { get; set; }

        // Pathway (optional; SEP/Amazon CPF). Name from CSV, resolved to PathwayId against
        // the seeded Pathways lookup. No auto-create — unmatched names are warned, not blocked.
        public string? Pathway { get; set; }
        public int? PathwayId { get; set; }

        // Tags (comma-separated in CSV, parsed to list) — external
        public List<string> TagNames { get; set; } = new();
        public List<int> TagIds { get; set; } = new();
        public List<string> TagsToCreate { get; set; } = new();

        // Internal tags — parallel pipeline to external. Matched/created with TagType="internal"
        // and attached to the same Certificate.Tags M2M. Kept separate so the creation phase
        // tags them "internal" (the external phase hardcodes "external").
        public List<string> InternalTagNames { get; set; } = new();
        public List<int> InternalTagIds { get; set; } = new();
        public List<string> InternalTagsToCreate { get; set; } = new();

        // Product IDs (for multi-ASIN support - additional rows with same cert number)
        public List<ProductIdEntry> ProductIdEntries { get; set; } = new();

        // PDF file path
        public string? PdfFilePath { get; set; }

        // Metadata
        public ImportRowStatus RowStatus { get; set; }
        public List<string> ValidationMessages { get; set; } = new();
        public List<FieldChange> Changes { get; set; } = new(); // For modified certificates

        // Matched entities (resolved from names)
        public int? CompanyId { get; set; }
        public int? ProductId { get; set; }
        public int? CertificationTypeId { get; set; }
        public int? ExistingCertificateId { get; set; } // If updating existing
    }

    /// <summary>
    /// Represents a product identifier entry to be created during import
    /// </summary>
    public class ProductIdEntry
    {
        public int RowNumber { get; set; }
        public string ProductIdType { get; set; } = string.Empty;
        public string ProductIdValue { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public string? AmazonCode { get; set; }
        public string? Partner { get; set; }
    }

    /// <summary>
    /// Represents a validation error for a row
    /// </summary>
    public class CertificateImportError
    {
        public int RowNumber { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty; // "Validation", "NotFound", "Format"
        public List<string> ErrorMessages { get; set; } = new();
        public string RawData { get; set; } = string.Empty; // Original CSV row for debugging
    }

    /// <summary>
    /// Represents a change to a specific field
    /// </summary>
    public class FieldChange
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool IsSignificant { get; set; } = true; // Some changes like whitespace aren't significant
    }

    /// <summary>
    /// Status of an import row
    /// </summary>
    public enum ImportRowStatus
    {
        New,        // Will create new certificate
        Modified,   // Will update existing certificate
        Unchanged,  // No changes detected
        Error       // Validation failed
    }

    /// <summary>
    /// CSV row as parsed from file
    /// </summary>
    public class CertificateImportCsvRow
    {
        public int RowNumber { get; set; }

        // Certificate fields
        public string CertificateNumber { get; set; } = string.Empty;
        public string CertificationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;      // Maps to CERTIFICATE_LOCATION
        public string Country { get; set; } = string.Empty;       // Maps to CERTIFICATE_COUNTRY
        public string CertificationResults { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;          // Comma-separated tags (external)
        public string InternalTags { get; set; } = string.Empty;  // Comma-separated internal tags

        // Company fields
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLocation { get; set; } = string.Empty;
        public string CompanyCountry { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string CompanyWebsite { get; set; } = string.Empty;

        // Product fields
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;

        // Product identifiers
        public string Asin { get; set; } = string.Empty;
        public string Upc { get; set; } = string.Empty;
        public string Gtin { get; set; } = string.Empty;
        public string Ean { get; set; } = string.Empty;

        // Product ID fields (for multi-ASIN support)
        public string ProductIdType { get; set; } = string.Empty;   // ASIN, UPC, GTIN, EAN, etc.
        public string ProductIdValue { get; set; } = string.Empty;  // The actual identifier value
        public string ProductSku { get; set; } = string.Empty;      // Optional SKU
        public string AmazonCode { get; set; } = string.Empty;      // Amazon certification code

        // PDF file
        public string PdfFilePath { get; set; } = string.Empty;     // Local path to PDF file for upload

        // Partner
        public string Partner { get; set; } = string.Empty;

        // Pathway (optional; resolved to PathwayId during processing)
        public string Pathway { get; set; } = string.Empty;

        public string RawData { get; set; } = string.Empty; // Full CSV line for debugging
    }

    /// <summary>
    /// Request to apply an import after preview approval
    /// </summary>
    public class ApplyImportRequest
    {
        public string ImportId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty; // Admin's reason for import
        public List<int> SelectedRowNumbers { get; set; } = new(); // Empty = apply all valid rows
    }

    /// <summary>
    /// Result of applying an import
    /// </summary>
    public class ApplyImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CertificatesCreated { get; set; }
        public int CertificatesUpdated { get; set; }
        public int CertificatesFailed { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}

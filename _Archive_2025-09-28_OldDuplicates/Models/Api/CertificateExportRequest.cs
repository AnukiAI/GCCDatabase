namespace GCCWebApp.Models.Api
{
    /// <summary>
    /// Request model for exporting certificates in various formats
    /// </summary>
    public class CertificateExportRequest
    {
        /// <summary>
        /// Export format (csv, excel, pdf)
        /// </summary>
        public string Format { get; set; } = "csv";

        /// <summary>
        /// Search term to filter certificates
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Company ID to filter certificates
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// Product ID to filter certificates
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>
        /// User role for access control
        /// </summary>
        public string? UserRole { get; set; }
    }
}
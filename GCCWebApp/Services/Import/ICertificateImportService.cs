using GCCWebApp.Models.Import;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GCCWebApp.Services.Import
{
    /// <summary>
    /// Service for importing certificates from CSV files with preview and validation
    /// </summary>
    public interface ICertificateImportService
    {
        /// <summary>
        /// Parses CSV file and generates preview of changes
        /// Does NOT apply changes - only validates and shows what would happen
        /// </summary>
        Task<CertificateImportPreviewResult> PreviewImportAsync(
            IFormFile csvFile,
            int userId,
            string username
        );

        /// <summary>
        /// Applies an import after admin approval
        /// Creates/updates certificates and logs all changes to audit history
        /// </summary>
        Task<ApplyImportResult> ApplyImportAsync(
            CertificateImportPreviewResult previewResult,
            int userId,
            string username,
            string? reason = null
        );

        /// <summary>
        /// Gets a cached preview result by import ID
        /// Preview results are cached for 30 minutes
        /// </summary>
        Task<CertificateImportPreviewResult?> GetCachedPreviewAsync(string importId);

        /// <summary>
        /// CR-023: lists certificate import batches (most recent first) for the Import History view.
        /// </summary>
        Task<System.Collections.Generic.List<ImportBatch>> GetImportHistoryAsync(int limit = 100);

        /// <summary>
        /// CR-023: undoes (rolls back) a completed certificate import batch — removes the certificates
        /// it created and restores the certificates it overwrote to their pre-import field values,
        /// using the per-row audit trail stamped with this batch id. Idempotent: a batch already
        /// undone is refused.
        /// </summary>
        Task<UndoImportResult> UndoImportBatchAsync(System.Guid batchId, int userId, string username);
    }
}

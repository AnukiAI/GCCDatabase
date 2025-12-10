using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models.Import
{
    /// <summary>
    /// Tracks batch import operations for historical reporting (Phase 2)
    /// Each import operation creates one ImportBatch record
    /// Individual row results are tracked in AuditHistory with matching ImportBatchId
    /// </summary>
    public class ImportBatch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Entity type being imported (Tag, User, Company, Product, etc.)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Original filename that was imported
        /// </summary>
        [MaxLength(255)]
        public string? FileName { get; set; }

        /// <summary>
        /// Total number of rows in the import file
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// Number of new records created
        /// </summary>
        public int NewCount { get; set; }

        /// <summary>
        /// Number of existing records updated
        /// </summary>
        public int ModifiedCount { get; set; }

        /// <summary>
        /// Number of records skipped (user chose not to import)
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Number of unchanged records (already exist with same values)
        /// </summary>
        public int UnchangedCount { get; set; }

        /// <summary>
        /// Number of records that failed to import
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Status of the import batch
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = ImportBatchStatus.Pending;

        /// <summary>
        /// User ID who initiated the import
        /// </summary>
        [Required]
        public int ImportedBy { get; set; }

        /// <summary>
        /// Username at time of import (denormalized for historical accuracy)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ImportedByUsername { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when import was initiated
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when import was completed (null if still processing)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// IP address of the user who initiated the import
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Error message if the entire batch failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Navigation property to User who imported
        /// </summary>
        [ForeignKey("ImportedBy")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Status values for ImportBatch
    /// </summary>
    public static class ImportBatchStatus
    {
        public const string Pending = "PENDING";
        public const string Processing = "PROCESSING";
        public const string Completed = "COMPLETED";
        public const string PartialSuccess = "PARTIAL_SUCCESS";
        public const string Failed = "FAILED";
        public const string Cancelled = "CANCELLED";
    }

    /// <summary>
    /// Generic import preview row (extends base ImportRowStatus)
    /// Uses existing ImportRowStatus enum from CertificateImportModels.cs
    /// </summary>
    public class GenericImportPreviewRow<T> where T : class
    {
        public int RowNumber { get; set; }
        public T Data { get; set; } = null!;
        public T? ExistingData { get; set; }
        public ImportRowStatus Status { get; set; }
        public List<FieldChange> Changes { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool Selected { get; set; } = true; // Default to selected for import
    }

    /// <summary>
    /// Generic result of an import preview operation
    /// </summary>
    public class GenericImportPreviewResult<T> where T : class
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FileName { get; set; }
        public Guid PreviewId { get; set; } = Guid.NewGuid();
        public int TotalRows { get; set; }
        public int NewCount { get; set; }
        public int ModifiedCount { get; set; }
        public int UnchangedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<GenericImportPreviewRow<T>> Rows { get; set; } = new();
    }

    /// <summary>
    /// Request to apply import after preview
    /// </summary>
    public class GenericImportApplyRequest
    {
        public Guid PreviewId { get; set; }
        public List<int> SelectedRowNumbers { get; set; } = new();
        public Dictionary<int, string> RowActions { get; set; } = new(); // RowNumber -> "create", "update", "skip"
    }

    /// <summary>
    /// Result of applying a generic import
    /// </summary>
    public class GenericImportApplyResult
    {
        public bool Success { get; set; }
        public Guid BatchId { get; set; }
        public string? ErrorMessage { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<GenericImportRowError> Errors { get; set; } = new();
    }

    /// <summary>
    /// Error details for a specific row
    /// </summary>
    public class GenericImportRowError
    {
        public int RowNumber { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

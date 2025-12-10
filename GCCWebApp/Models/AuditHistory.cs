using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Generic audit history table for tracking changes to all entities
    /// Tracks CREATE, UPDATE, DELETE operations with field-level change details
    /// </summary>
    public class AuditHistory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Entity type being audited (e.g., "Product", "Company", "Tag", "User", "Certificate")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Primary key of the entity being audited (stored as string for flexibility)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the entity for historical reference (denormalized)
        /// E.g., Product Name, Company Name, Tag Name, Username
        /// </summary>
        [MaxLength(255)]
        public string? EntityName { get; set; }

        /// <summary>
        /// Action performed: CREATE, UPDATE, DELETE, RESTORE
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Specific field that was changed (null for CREATE/DELETE which affect entire entity)
        /// E.g., "Name", "Price", "Status", "Email"
        /// </summary>
        [MaxLength(100)]
        public string? FieldName { get; set; }

        /// <summary>
        /// Previous value before change (null for CREATE)
        /// Stored as JSON string for complex objects
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// New value after change (null for DELETE)
        /// Stored as JSON string for complex objects
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// User ID who made the change (FK to Users table)
        /// </summary>
        [Required]
        public int ChangedBy { get; set; }

        /// <summary>
        /// Username at time of change (denormalized for historical accuracy)
        /// Preserved even if user is later deleted or renamed
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ChangedByUsername { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when change occurred (UTC)
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address of the user who made the change
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string (browser/client information)
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Source of the change: "Web UI", "API", "Import", "Automated", "Migration"
        /// </summary>
        [MaxLength(50)]
        public string? ChangeSource { get; set; }

        /// <summary>
        /// Optional reason provided by user for the change
        /// </summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>
        /// Session ID for correlating multiple changes in same session
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Import batch ID for correlating all records from a single import operation
        /// Used for Phase 2 Import History tracking
        /// </summary>
        public Guid? ImportBatchId { get; set; }

        /// <summary>
        /// Additional metadata stored as JSON
        /// E.g., {"RoleName": "Admin", "BulkImport": true}
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Navigation property to User who made the change
        /// </summary>
        [ForeignKey("ChangedBy")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Enum for common audit actions
    /// </summary>
    public static class AuditAction
    {
        public const string Create = "CREATE";
        public const string Update = "UPDATE";
        public const string Delete = "DELETE";
        public const string Restore = "RESTORE";
        public const string Login = "LOGIN";
        public const string Logout = "LOGOUT";
        public const string PasswordChange = "PASSWORD_CHANGE";
        public const string RoleAssigned = "ROLE_ASSIGNED";
        public const string RoleRemoved = "ROLE_REMOVED";
        public const string StatusChange = "STATUS_CHANGE";
        // Import-specific actions for Phase 2 history tracking
        public const string ImportCreate = "IMPORT_CREATE";
        public const string ImportUpdate = "IMPORT_UPDATE";
        public const string ImportSkipped = "IMPORT_SKIPPED";
        public const string ImportError = "IMPORT_ERROR";
    }

    /// <summary>
    /// Enum for entity types
    /// </summary>
    public static class AuditEntityType
    {
        public const string Certificate = "Certificate";
        public const string Product = "Product";
        public const string Company = "Company";
        public const string Tag = "Tag";
        public const string User = "User";
        public const string Partner = "Partner";
        public const string ApiKey = "ApiKey";
        public const string PartnerApiKey = "PartnerApiKey";
        public const string CertificateType = "CertificateType";
        public const string CertificateField = "CertificateField";
        public const string ProductType = "ProductType";
        public const string Category = "Category";
        public const string Role = "Role";
    }

    /// <summary>
    /// Enum for change sources
    /// </summary>
    public static class AuditChangeSource
    {
        public const string WebUI = "Web UI";
        public const string API = "API";
        public const string Import = "Import";
        public const string Automated = "Automated";
        public const string Migration = "Migration";
        public const string System = "System";
    }
}

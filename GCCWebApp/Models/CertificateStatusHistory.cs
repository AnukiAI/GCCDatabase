using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Tracks all certificate status transitions for audit trail and compliance.
    /// Provides complete history of status changes with timestamps, user info, and reasons.
    /// </summary>
    public class CertificateStatusHistory
    {
        /// <summary>
        /// Primary key
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Certificate
        /// </summary>
        [Required]
        public int CertificateId { get; set; }

        /// <summary>
        /// Certificate number (denormalized for query performance)
        /// </summary>
        /// <remarks>
        /// DEF-314: was MaxLength(7) — sized for exactly "YY-NNNN" — while the source column
        /// Certificate.Number is MaxLength(50). A denormalized copy narrower than its source is
        /// a latent bug: any certificate number longer than 7 chars (e.g. "26-10126", 8 chars)
        /// stores fine on Certificates but overflows here. A status change is the only path that
        /// inserts a row (CertificateController.cs:1336), so SQL Server raised "String or binary
        /// data would be truncated" on save, which the catch at CertificateController.cs:1385
        /// turned into the generic "An error occurred while updating the certificate". Plain
        /// edits were unaffected because they never write a status-history row.
        /// Widened to 50 to match the source. Keep these two in lock-step.
        /// </remarks>
        [MaxLength(50)]
        public string? CertificateNumber { get; set; }

        /// <summary>
        /// Previous status before change
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PreviousStatus { get; set; } = string.Empty;

        /// <summary>
        /// New status after change
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>
        /// Reason for status change
        /// </summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>
        /// Additional notes or comments
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// User who made the status change
        /// </summary>
        [Required]
        public int ChangedBy { get; set; }

        /// <summary>
        /// User name (denormalized for query performance)
        /// </summary>
        [MaxLength(255)]
        public string? ChangedByUsername { get; set; }

        /// <summary>
        /// When the status change occurred
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// IP address of user who made the change
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent of browser/client
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Source of change: Manual, Automated, API, Import
        /// </summary>
        [MaxLength(50)]
        public string? ChangeSource { get; set; }

        /// <summary>
        /// Duration in previous status (in days)
        /// </summary>
        public int? DurationInPreviousStatus { get; set; }

        /// <summary>
        /// Is this an automated transition
        /// </summary>
        public bool IsAutomatedTransition { get; set; }

        /// <summary>
        /// Related workflow step or action
        /// </summary>
        [MaxLength(100)]
        public string? WorkflowStep { get; set; }

        // Navigation properties
        /// <summary>
        /// Related certificate
        /// </summary>
        [ForeignKey(nameof(CertificateId))]
        public virtual Certificate? Certificate { get; set; }

        /// <summary>
        /// User who made the change
        /// </summary>
        [ForeignKey(nameof(ChangedBy))]
        public virtual User? User { get; set; }
    }
}

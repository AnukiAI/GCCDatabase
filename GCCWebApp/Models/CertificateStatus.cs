using System.ComponentModel;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Enumeration of certificate lifecycle status values
    /// Aligned with DATA_VALIDATION_RULES.md - 14 documented states
    /// </summary>
    public enum CertificateStatus
    {
        [Description("Draft")]
        Draft = 0,

        [Description("Pending Review")]
        PendingReview = 1,

        [Description("Under Review")]
        UnderReview = 2,

        [Description("Approved")]
        Approved = 3,

        [Description("Active")]
        Active = 4,

        [Description("Suspended")]
        Suspended = 5,

        [Description("Revoked")]
        Revoked = 6,

        [Description("Expired")]
        Expired = 7,

        [Description("Pending Renewal")]
        PendingRenewal = 8,

        [Description("Renewed")]
        Renewed = 9,

        [Description("Cancelled")]
        Cancelled = 10,

        [Description("Archived")]
        Archived = 11,

        [Description("On Hold")]
        OnHold = 12,

        [Description("Rejected")]
        Rejected = 13
    }

    /// <summary>
    /// Static class for certificate status management and workflow rules
    /// Database-driven status values aligned with DATA_VALIDATION_RULES.md
    /// </summary>
    public static class CertificateStatusHelper
    {
        /// <summary>
        /// Gets all available status values with their display names
        /// These are the 14 documented states from DATA_VALIDATION_RULES.md
        /// </summary>
        public static Dictionary<string, string> GetAllStatuses()
        {
            return new Dictionary<string, string>
            {
                { "Draft", "Draft" },
                { "PendingReview", "Pending Review" },
                { "UnderReview", "Under Review" },
                { "Approved", "Approved" },
                { "Active", "Active" },
                { "Suspended", "Suspended" },
                { "Revoked", "Revoked" },
                { "Expired", "Expired" },
                { "PendingRenewal", "Pending Renewal" },
                { "Renewed", "Renewed" },
                { "Cancelled", "Cancelled" },
                { "Archived", "Archived" },
                { "OnHold", "On Hold" },
                { "Rejected", "Rejected" }
            };
        }

        /// <summary>
        /// Gets status values available for initial certificate creation
        /// </summary>
        public static Dictionary<string, string> GetCreationStatuses()
        {
            return new Dictionary<string, string>
            {
                { "Draft", "Draft" },
                { "PendingReview", "Pending Review" }
            };
        }

        /// <summary>
        /// Gets status values available for different user roles
        /// </summary>
        public static Dictionary<string, string> GetStatusesForRole(string role)
        {
            var allStatuses = GetAllStatuses();

            return role?.ToLower() switch
            {
                "admin" => allStatuses, // Admins can set any status
                "analyst" => allStatuses.Where(s =>
                    s.Key != "Rejected" && s.Key != "Revoked").ToDictionary(k => k.Key, v => v.Value),
                "external" => GetCreationStatuses(), // External users can only create drafts
                _ => GetCreationStatuses()
            };
        }

        /// <summary>
        /// Determines if a status transition is valid based on business rules
        /// Workflow: Draft → PendingReview → UnderReview → Approved → Active
        /// </summary>
        public static bool IsValidTransition(string fromStatus, string toStatus)
        {
            // Define valid status transitions based on business workflow
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Draft", new[] { "PendingReview", "Cancelled" } },
                { "PendingReview", new[] { "UnderReview", "Draft", "Cancelled", "OnHold" } },
                { "UnderReview", new[] { "Approved", "PendingReview", "Rejected", "OnHold" } },
                { "Approved", new[] { "Active", "UnderReview", "OnHold" } },
                { "Active", new[] { "Suspended", "Expired", "PendingRenewal", "Revoked", "Archived" } },
                { "Suspended", new[] { "Active", "Revoked", "Cancelled", "Archived" } },
                { "Revoked", new[] { "Archived" } }, // Near-final state
                { "Expired", new[] { "PendingRenewal", "Archived", "Active" } },
                { "PendingRenewal", new[] { "UnderReview", "Renewed", "Expired", "Cancelled" } },
                { "Renewed", new[] { "Active" } }, // Transitions back to active
                { "Cancelled", new string[] { } }, // Final state - no transitions allowed
                { "Archived", new string[] { } }, // Final state - no transitions allowed
                { "OnHold", new[] { "PendingReview", "UnderReview", "Approved", "Cancelled" } },
                { "Rejected", new[] { "Draft", "Archived" } } // Can restart or archive
            };

            return validTransitions.ContainsKey(fromStatus) &&
                   validTransitions[fromStatus].Contains(toStatus);
        }

        /// <summary>
        /// Gets the display name for a status value
        /// </summary>
        public static string GetDisplayName(string statusValue)
        {
            var statuses = GetAllStatuses();
            return statuses.TryGetValue(statusValue, out var displayName) ? displayName : statusValue;
        }

        /// <summary>
        /// Checks if a status represents an active/valid certificate
        /// </summary>
        public static bool IsActiveStatus(string status)
        {
            return status == "Active" || status == "PendingRenewal" || status == "Renewed";
        }

        /// <summary>
        /// Checks if a status represents a final/terminal state
        /// </summary>
        public static bool IsFinalStatus(string status)
        {
            return status == "Archived" || status == "Cancelled";
        }

        /// <summary>
        /// Checks if a status represents a negative outcome
        /// </summary>
        public static bool IsNegativeStatus(string status)
        {
            return status == "Rejected" || status == "Revoked" || status == "Cancelled";
        }

        /// <summary>
        /// Gets statuses that indicate the certificate needs attention
        /// </summary>
        public static bool NeedsAttention(string status)
        {
            return status == "PendingReview" || status == "UnderReview" ||
                   status == "OnHold" || status == "PendingRenewal" || status == "Expired";
        }
    }
}
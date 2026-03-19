using System.ComponentModel;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Enumeration of certificate lifecycle status values
    /// 8 client-defined states per GCC requirements
    /// </summary>
    public enum CertificateStatus
    {
        [Description("Draft")]
        Draft = 0,

        [Description("Client Approved - Not Paid")]
        ClientApprovedNotPaid = 1,

        [Description("Client Approved - Confidential")]
        ClientApprovedConfidential = 2,

        [Description("Client Approved - Paid")]
        ClientApprovedPaid = 3,

        [Description("Hold")]
        Hold = 4,

        [Description("Expired")]
        Expired = 5,

        [Description("Republished")]
        Republished = 6,

        [Description("Expired - No Annual Verification")]
        ExpiredNoAnnualVerification = 7
    }

    /// <summary>
    /// Static class for certificate status management and workflow rules
    /// </summary>
    public static class CertificateStatusHelper
    {
        // Status string constants used throughout the application
        public const string StatusDraft = "Draft";
        public const string StatusClientApprovedNotPaid = "Client Approved - Not Paid";
        public const string StatusClientApprovedConfidential = "Client Approved - Confidential";
        public const string StatusClientApprovedPaid = "Client Approved - Paid";
        public const string StatusHold = "Hold";
        public const string StatusExpired = "Expired";
        public const string StatusRepublished = "Republished";
        public const string StatusExpiredNoAnnualVerification = "Expired - No Annual Verification";

        /// <summary>
        /// Statuses visible on the public (anonymous) view
        /// </summary>
        public static readonly string[] PublicStatuses = new[]
        {
            StatusClientApprovedPaid,
            StatusRepublished
        };

        /// <summary>
        /// Gets all available status values with their display names
        /// </summary>
        public static Dictionary<string, string> GetAllStatuses()
        {
            return new Dictionary<string, string>
            {
                { StatusDraft, "Draft" },
                { StatusClientApprovedNotPaid, "Client Approved - Not Paid" },
                { StatusClientApprovedConfidential, "Client Approved - Confidential" },
                { StatusClientApprovedPaid, "Client Approved - Paid" },
                { StatusHold, "Hold" },
                { StatusExpired, "Expired" },
                { StatusRepublished, "Republished" },
                { StatusExpiredNoAnnualVerification, "Expired - No Annual Verification" }
            };
        }

        /// <summary>
        /// Gets status values available for initial certificate creation
        /// </summary>
        public static Dictionary<string, string> GetCreationStatuses()
        {
            return new Dictionary<string, string>
            {
                { StatusDraft, "Draft" }
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
                "admin" => allStatuses,
                "analyst" => allStatuses,
                "external" => GetCreationStatuses(),
                _ => GetCreationStatuses()
            };
        }

        /// <summary>
        /// Determines if a status transition is valid based on business rules
        /// </summary>
        public static bool IsValidTransition(string fromStatus, string toStatus)
        {
            var validTransitions = new Dictionary<string, string[]>
            {
                { StatusDraft, new[] { StatusClientApprovedNotPaid, StatusClientApprovedConfidential, StatusClientApprovedPaid, StatusHold } },
                { StatusClientApprovedNotPaid, new[] { StatusClientApprovedPaid, StatusClientApprovedConfidential, StatusHold, StatusExpired } },
                { StatusClientApprovedConfidential, new[] { StatusClientApprovedPaid, StatusHold, StatusExpired } },
                { StatusClientApprovedPaid, new[] { StatusHold, StatusExpired, StatusRepublished, StatusExpiredNoAnnualVerification } },
                { StatusHold, new[] { StatusDraft, StatusClientApprovedNotPaid, StatusClientApprovedConfidential, StatusClientApprovedPaid } },
                { StatusExpired, new[] { StatusRepublished, StatusDraft } },
                { StatusRepublished, new[] { StatusExpired, StatusExpiredNoAnnualVerification, StatusHold } },
                { StatusExpiredNoAnnualVerification, new[] { StatusDraft } }
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
        /// Checks if a status is visible on the public view
        /// </summary>
        public static bool IsPubliclyVisible(string status)
        {
            return status == StatusClientApprovedPaid || status == StatusRepublished;
        }

        /// <summary>
        /// Checks if a status represents an active/valid certificate
        /// </summary>
        public static bool IsActiveStatus(string status)
        {
            return status == StatusClientApprovedPaid || status == StatusRepublished;
        }

        /// <summary>
        /// Checks if a status represents a final/terminal state
        /// </summary>
        public static bool IsFinalStatus(string status)
        {
            return status == StatusExpiredNoAnnualVerification;
        }

        /// <summary>
        /// Checks if a status represents a negative outcome
        /// </summary>
        public static bool IsNegativeStatus(string status)
        {
            return status == StatusExpired || status == StatusExpiredNoAnnualVerification;
        }

        /// <summary>
        /// Gets statuses that indicate the certificate needs attention
        /// </summary>
        public static bool NeedsAttention(string status)
        {
            return status == StatusClientApprovedNotPaid || status == StatusHold || status == StatusExpired;
        }
    }
}

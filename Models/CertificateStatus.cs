using System.ComponentModel;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Enumeration of certificate lifecycle status values
    /// Represents the complete business process from application to completion
    /// </summary>
    public enum CertificateStatus
    {
        [Description("Draft")]
        Draft = 0,

        [Description("Application Submitted")]
        ApplicationSubmitted = 1,

        [Description("Under Review")]
        UnderReview = 2,

        [Description("Technical Review")]
        TechnicalReview = 3,

        [Description("Compliance Verification")]
        ComplianceVerification = 4,

        [Description("Final Approval")]
        FinalApproval = 5,

        [Description("Active")]
        Active = 6,

        [Description("Expiring Soon")]
        ExpiringSoon = 7,

        [Description("Renewal In Progress")]
        RenewalInProgress = 8,

        [Description("Expired")]
        Expired = 9,

        [Description("Suspended")]
        Suspended = 10,

        [Description("Archived")]
        Archived = 11,

        [Description("Cancelled")]
        Cancelled = 12,

        [Description("Client Won't Proceed")]
        ClientDeclined = 13
    }

    /// <summary>
    /// Static class for certificate status management and workflow rules
    /// </summary>
    public static class CertificateStatusHelper
    {
        /// <summary>
        /// Gets all available status values with their display names
        /// </summary>
        public static Dictionary<string, string> GetAllStatuses()
        {
            return new Dictionary<string, string>
            {
                { "draft", "Draft" },
                { "application_submitted", "Application Submitted" },
                { "under_review", "Under Review" },
                { "technical_review", "Technical Review" },
                { "compliance_verification", "Compliance Verification" },
                { "final_approval", "Final Approval" },
                { "active", "Active" },
                { "expiring_soon", "Expiring Soon" },
                { "renewal_in_progress", "Renewal In Progress" },
                { "expired", "Expired" },
                { "suspended", "Suspended" },
                { "archived", "Archived" },
                { "cancelled", "Cancelled" },
                { "client_declined", "Client Won't Proceed" }
            };
        }

        /// <summary>
        /// Gets status values available for initial certificate creation
        /// </summary>
        public static Dictionary<string, string> GetCreationStatuses()
        {
            return new Dictionary<string, string>
            {
                { "draft", "Draft" },
                { "application_submitted", "Application Submitted" }
            };
        }

        /// <summary>
        /// Gets status values available for different user roles
        /// </summary>
        public static Dictionary<string, string> GetStatusesForRole(string role)
        {
            var allStatuses = GetAllStatuses();
            
            return role.ToLower() switch
            {
                "admin" => allStatuses, // Admins can set any status
                "analyst" => allStatuses.Where(s => 
                    s.Key != "client_declined").ToDictionary(k => k.Key, v => v.Value),
                "external" => GetCreationStatuses(), // External users can only create drafts
                _ => GetCreationStatuses()
            };
        }

        /// <summary>
        /// Determines if a status transition is valid based on business rules
        /// </summary>
        public static bool IsValidTransition(string fromStatus, string toStatus)
        {
            // Define valid status transitions based on business workflow
            var validTransitions = new Dictionary<string, string[]>
            {
                { "draft", new[] { "application_submitted", "cancelled" } },
                { "application_submitted", new[] { "under_review", "draft", "cancelled" } },
                { "under_review", new[] { "technical_review", "application_submitted", "client_declined" } },
                { "technical_review", new[] { "compliance_verification", "under_review", "client_declined" } },
                { "compliance_verification", new[] { "final_approval", "technical_review", "client_declined" } },
                { "final_approval", new[] { "active", "compliance_verification" } },
                { "active", new[] { "expiring_soon", "renewal_in_progress", "suspended", "expired", "archived" } },
                { "expiring_soon", new[] { "renewal_in_progress", "expired", "active" } },
                { "renewal_in_progress", new[] { "active", "expired", "expiring_soon" } },
                { "expired", new[] { "renewal_in_progress", "archived", "active" } },
                { "suspended", new[] { "active", "cancelled", "archived" } },
                { "archived", new string[] { } }, // Final state - no transitions allowed
                { "cancelled", new string[] { } }, // Final state - no transitions allowed
                { "client_declined", new string[] { } } // Final state - no transitions allowed
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
            return statuses.ContainsKey(statusValue) ? statuses[statusValue] : statusValue;
        }

        /// <summary>
        /// Checks if a status represents an active/valid certificate
        /// </summary>
        public static bool IsActiveStatus(string status)
        {
            return status == "active" || status == "expiring_soon" || status == "renewal_in_progress";
        }

        /// <summary>
        /// Checks if a status represents a final/terminal state
        /// </summary>
        public static bool IsFinalStatus(string status)
        {
            return status == "archived" || status == "cancelled" || status == "client_declined";
        }
    }
}
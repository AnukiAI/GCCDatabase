using System.ComponentModel;
using System.Text;

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
        // Business timezone: GCC operates on US Eastern Time.
        // Expiration dates are business concepts — "valid through April 3" means April 3 in Eastern Time.
        private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        /// <summary>
        /// Gets today's date in the business timezone (US Eastern).
        /// Use this for all expiration date comparisons instead of DateTime.UtcNow.Date.
        /// </summary>
        public static DateTime BusinessToday => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BusinessTimeZone).Date;

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
        /// CR-043 (client, 2026-09-02, answers confirmed 2026-09-03): statuses that are
        /// COMMERCIALLY IN FORCE and therefore badgeable — the partner/Amazon API, the
        /// product-identifier export, and every logged-in owner surface (Partner and
        /// External/company).
        ///
        /// Differs from <see cref="PublicStatuses"/> by exactly one value: Confidential.
        /// The client's rule is *"the Confidential piece is just for the public DB side to
        /// not show there"* — paid, in-force work that still gets badged, with only the
        /// anonymous public listing suppressed. Before CR-043 a single array answered both
        /// questions, so embargoing a certificate from the public site silently pulled it
        /// out of Amazon's feed as well.
        ///
        /// NEVER point an anonymous/public surface at this array. The pairing is deliberate:
        /// PublicStatuses = "may a stranger see it", BadgeableStatuses = "is it in force".
        /// </summary>
        public static readonly string[] BadgeableStatuses = new[]
        {
            StatusClientApprovedPaid,
            StatusClientApprovedConfidential,
            StatusRepublished
        };

        /// <summary>
        /// Statuses counted as "active" on the INTERNAL dashboard tiles and their exports.
        ///
        /// DEF-223 (Shawn, 2026-05-26) narrowed this from {Paid, Republished, Confidential}
        /// to {Paid, Republished} so "Active" meant the same thing internally and publicly.
        /// CR-043 Q2 (client, 2026-09-03) reverses that premise and restores Confidential:
        /// asked *"should Confidential certificates count as Active in the internal dashboard
        /// counts and their exports?"* the client answered *"Yes as long as they are in the
        /// eff-exp period window."* Confidential is in-force work; only its public listing is
        /// embargoed.
        ///
        /// Equal in value to <see cref="BadgeableStatuses"/> today, and deliberately kept as
        /// a separate array anyway — "counts as active internally" and "is badgeable" are two
        /// questions, and collapsing two questions into one array is the exact fault CR-043
        /// was raised to fix. The window (eff/exp) is applied by each call site, not here.
        /// </summary>
        public static readonly string[] DashboardStatuses = new[]
        {
            StatusClientApprovedPaid,
            StatusClientApprovedConfidential,
            StatusRepublished
        };

        /// <summary>
        /// DEF-220 (QC #180 5/26 follow-up): statuses that count for "Certs Expired Last Month".
        /// A cert is only "expired" if its CURRENT status is Expired or ExpiredNoAnnualVerification.
        /// Republished means the cert was re-published (re-active) with a fresh expiration —
        /// including it in the expired report was wrong. Paid/Confidential with a past
        /// ExpirationDate is a data anomaly (background service should auto-flip to Expired)
        /// and shouldn't be reported as expired either. Draft/Hold/ClientApprovedNotPaid stay
        /// excluded for the same reason as the original DEF-200 fix — never-published certs
        /// never "expire."
        /// </summary>
        public static readonly string[] CurrentlyExpiredStatuses = new[]
        {
            StatusExpired,
            StatusExpiredNoAnnualVerification
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
        /// Returns the display names of every status the given current status may
        /// transition to, per the workflow matrix. Empty when the status is terminal
        /// or unknown. Used to tell the user their valid options inline instead of
        /// making them guess (DEF-305 UX fix for the workflow-transition error).
        /// </summary>
        public static List<string> GetAllowedNextStatusDisplayNames(string fromStatus)
        {
            var result = new List<string>();
            foreach (var status in GetAllStatuses().Keys)
            {
                if (IsValidTransition(fromStatus, status))
                    result.Add(GetDisplayName(status));
            }
            return result;
        }

        /// <summary>
        /// Resolves a free-text status value (e.g. from a CSV import) to its canonical
        /// status string, tolerating case, whitespace, and punctuation differences — so
        /// "Client Approved Paid", "client approved-paid", or "CLIENT APPROVED  PAID" all
        /// resolve to "Client Approved - Paid". Returns false (canonical = "") when the
        /// value matches no known status, so callers can reject it instead of persisting
        /// invalid text. The 8 canonical keys remain distinct after normalization, so there
        /// is no ambiguity (e.g. "Expired" vs "Expired - No Annual Verification").
        /// </summary>
        public static bool TryResolveStatus(string? input, out string canonical)
        {
            canonical = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var target = NormalizeStatusKey(input);
            foreach (var status in GetAllStatuses().Keys)
            {
                if (NormalizeStatusKey(status) == target)
                {
                    canonical = status;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Collapses a status string to a comparison key: lowercase, letters/digits only.
        /// "Client Approved - Paid" and "client approved paid" both → "clientapprovedpaid".
        /// </summary>
        private static string NormalizeStatusKey(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
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
        /// CR-043: in-force check for the badging surfaces — partner/Amazon, the product-identifier
        /// export, and logged-in owner views. The counterpart to <see cref="IsPubliclyVisible"/>,
        /// which answers the anonymous-visibility question and must not be used here.
        /// Status only; callers still apply the effective/expiration window.
        /// </summary>
        public static bool IsBadgeable(string status)
        {
            return status == StatusClientApprovedPaid
                || status == StatusClientApprovedConfidential
                || status == StatusRepublished;
        }

        /// <summary>
        /// Checks if a status represents an active/valid certificate.
        /// Currently unreferenced — <see cref="IsPubliclyVisible"/> and <see cref="IsBadgeable"/>
        /// are the two live predicates. Left in place rather than widened for CR-043 precisely
        /// because "active" is the ambiguous word that let one list do two jobs; a new caller
        /// should pick the specific question it means.
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

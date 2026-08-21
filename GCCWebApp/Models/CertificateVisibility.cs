using System.Linq.Expressions;

namespace GCCWebApp.Models
{
    /// <summary>
    /// DEF-358: single source of truth for "is this certificate live on a PUBLIC-facing surface
    /// right now" — status + the full validity window [EffectiveDate, ExpirationDate], inclusive
    /// on both ends, evaluated in the business timezone (US Eastern, see
    /// <see cref="CertificateStatusHelper.BusinessToday"/>).
    ///
    /// Before DEF-358 every public and partner gate tested status + expiration ONLY, so a cert the
    /// analyst staged early (Client Approved - Paid today, effective next month) published
    /// immediately — on the public database and through the Amazon partner API, which reports it
    /// as status="Active". 9 of 67 publicly-visible certs on the test env were in that state.
    ///
    /// SCOPE (client decision, Shawn 2026-08-17): the not-yet-effective gate applies to
    /// **anonymous public + the partner/Amazon surface only**. Logged-in External (owning company)
    /// users and internal Admin/Analyst users keep seeing their upcoming certs — a company needs to
    /// know its cert is queued, and analysts stage them deliberately. So this class is deliberately
    /// NOT wired into CertificateTileExternalApiController or ExportController.
    ///
    /// The window is enforced at query time, so a cert publishes itself on its effective date with
    /// no background job or status flip required.
    /// </summary>
    public static class CertificateVisibility
    {
        /// <summary>
        /// True when <paramref name="asOf"/> (default: today, business timezone) falls inside the
        /// certificate's validity window.
        ///
        /// Both bounds are INCLUSIVE — a cert is live ON its effective date and remains live
        /// THROUGH its expiration date, matching the pre-existing expiration semantics
        /// ("cert shows through its expiration day, expires the day after",
        /// CertificateExpirationService).
        ///
        /// A <c>default(DateTime)</c> on either bound means "unbounded on that side", preserving the
        /// existing `ExpirationDate != default` convention. That also means legacy/imported rows
        /// with no effective date set are unaffected by this gate.
        /// </summary>
        public static bool IsWithinValidityWindow(DateTime effectiveDate, DateTime expirationDate, DateTime? asOf = null)
        {
            var today = (asOf ?? CertificateStatusHelper.BusinessToday).Date;

            var notYetEffective = effectiveDate != default && effectiveDate.Date > today;
            var expired = expirationDate != default && expirationDate.Date < today;

            return !notYetEffective && !expired;
        }

        /// <summary>
        /// True when the certificate has not yet reached its effective date — i.e. the specific
        /// condition DEF-358 added. Useful for logging and for callers that already test expiry.
        /// </summary>
        public static bool IsNotYetEffective(Certificate certificate, DateTime? asOf = null)
        {
            var today = (asOf ?? CertificateStatusHelper.BusinessToday).Date;
            return certificate.EffectiveDate != default && certificate.EffectiveDate.Date > today;
        }

        /// <summary>
        /// Full public-visibility test for a single loaded certificate: publishable status AND
        /// inside the validity window. This is the in-memory counterpart of
        /// <see cref="PubliclyLiveExpression"/> — the two must always agree.
        /// </summary>
        public static bool IsPubliclyLive(Certificate certificate, DateTime? asOf = null)
        {
            return CertificateStatusHelper.IsPubliclyVisible(certificate.Status)
                   && IsWithinValidityWindow(certificate.EffectiveDate, certificate.ExpirationDate, asOf);
        }

        /// <summary>
        /// EF-translatable predicate for the same rule, for use in IQueryable composition.
        /// <paramref name="asOf"/> is captured as a constant at expression-build time so the
        /// generated SQL parameterizes a single date rather than calling into the helper per row.
        /// </summary>
        public static Expression<Func<Certificate, bool>> PubliclyLiveExpression(DateTime? asOf = null)
        {
            var today = (asOf ?? CertificateStatusHelper.BusinessToday).Date;
            var unset = default(DateTime);

            return c => CertificateStatusHelper.PublicStatuses.Contains(c.Status)
                        && (c.EffectiveDate == unset || c.EffectiveDate <= today)
                        && (c.ExpirationDate == unset || c.ExpirationDate >= today);
        }

        /// <summary>
        /// Just the DEF-358 lower bound, for call sites that already apply their own status and
        /// expiration filters and only need the not-yet-effective rows removed.
        /// </summary>
        public static Expression<Func<Certificate, bool>> EffectiveOnOrBeforeExpression(DateTime? asOf = null)
        {
            var today = (asOf ?? CertificateStatusHelper.BusinessToday).Date;
            var unset = default(DateTime);

            return c => c.EffectiveDate == unset || c.EffectiveDate <= today;
        }

        /// <summary>
        /// Convenience composition: drop certificates that have not yet reached their effective date.
        /// </summary>
        public static IQueryable<Certificate> WhereEffectiveDateReached(this IQueryable<Certificate> query, DateTime? asOf = null)
        {
            return query.Where(EffectiveOnOrBeforeExpression(asOf));
        }

        /// <summary>
        /// Convenience composition: the complete public-visibility rule (status + window).
        /// </summary>
        public static IQueryable<Certificate> WherePubliclyLive(this IQueryable<Certificate> query, DateTime? asOf = null)
        {
            return query.Where(PubliclyLiveExpression(asOf));
        }
    }
}

using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// DEF-358 — certificates staged ahead of their effective date must not appear on the public
    /// database or go out over the partner (Amazon) API until that date arrives.
    ///
    /// Every assertion drives <c>asOf</c> explicitly rather than relying on ambient
    /// <c>DateTime.Now</c>, so the day-before / day-of / day-after boundary is falsifiable instead
    /// of being proved by a proxy that happens to pass today.
    /// </summary>
    public class DEF358_EffectiveDateVisibilityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        // Fixed clock — no ambient date anywhere in this file.
        private static readonly DateTime Today = new DateTime(2026, 8, 17);

        public DEF358_EffectiveDateVisibilityTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
        }

        public void Dispose() => _context.Dispose();

        private static Certificate Cert(
            DateTime effective,
            DateTime expiration,
            string status = CertificateStatusHelper.StatusClientApprovedPaid,
            int id = 1)
        {
            return new Certificate
            {
                Id = id,
                Number = $"26-{1000 + id}",
                Status = status,
                EffectiveDate = effective,
                ExpirationDate = expiration
            };
        }

        // ---------- effective-date lower bound (the DEF-358 defect) ----------

        [Fact]
        public void NotYetEffective_TheDayBefore_IsHiddenFromPublic()
        {
            var cert = Cert(effective: Today.AddDays(1), expiration: Today.AddYears(1));

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeFalse(
                "a cert approved early must stay hidden until its effective date arrives");
            CertificateVisibility.IsNotYetEffective(cert, Today).Should().BeTrue();
        }

        [Fact]
        public void OnTheEffectiveDate_ItselfBecomesVisible()
        {
            var cert = Cert(effective: Today, expiration: Today.AddYears(1));

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeTrue(
                "the lower bound is inclusive — the cert is live ON its effective date");
            CertificateVisibility.IsNotYetEffective(cert, Today).Should().BeFalse();
        }

        [Fact]
        public void AfterTheEffectiveDate_StaysVisible()
        {
            var cert = Cert(effective: Today.AddDays(-30), expiration: Today.AddYears(1));

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeTrue();
        }

        [Fact]
        public void FarFutureEffectiveDate_IsHidden()
        {
            var cert = Cert(effective: Today.AddMonths(2), expiration: Today.AddYears(2));

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeFalse();
        }

        // ---------- expiration upper bound must be unchanged by this fix ----------

        [Fact]
        public void OnTheExpirationDate_IsStillVisible()
        {
            var cert = Cert(effective: Today.AddYears(-1), expiration: Today);

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeTrue(
                "pre-existing semantics: a cert shows THROUGH its expiration day");
        }

        [Fact]
        public void TheDayAfterExpiration_IsHidden()
        {
            var cert = Cert(effective: Today.AddYears(-1), expiration: Today.AddDays(-1));

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeFalse();
        }

        // ---------- rows with no dates set must not regress ----------

        [Fact]
        public void UnsetEffectiveDate_IsTreatedAsUnbounded_NotAsYear1()
        {
            // Legacy/imported rows can carry default(DateTime). They must remain visible —
            // this gate is a new lower bound, not a new required field.
            var cert = Cert(effective: default, expiration: Today.AddYears(1));

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeTrue();
            CertificateVisibility.IsNotYetEffective(cert, Today).Should().BeFalse();
        }

        [Fact]
        public void UnsetExpirationDate_StillMeansNeverExpires()
        {
            var cert = Cert(effective: Today.AddDays(-1), expiration: default);

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeTrue();
        }

        // ---------- status gate is independent of the date gate ----------

        [Theory]
        [InlineData(CertificateStatusHelper.StatusDraft)]
        [InlineData(CertificateStatusHelper.StatusHold)]
        [InlineData(CertificateStatusHelper.StatusClientApprovedNotPaid)]
        [InlineData(CertificateStatusHelper.StatusClientApprovedConfidential)]
        [InlineData(CertificateStatusHelper.StatusExpired)]
        [InlineData(CertificateStatusHelper.StatusExpiredNoAnnualVerification)]
        public void NonPublicStatus_IsHidden_EvenInsideTheWindow(string status)
        {
            var cert = Cert(effective: Today.AddDays(-10), expiration: Today.AddYears(1), status: status);

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeFalse();
        }

        [Theory]
        [InlineData(CertificateStatusHelper.StatusClientApprovedPaid)]
        [InlineData(CertificateStatusHelper.StatusRepublished)]
        public void PublicStatus_IsStillHidden_WhenNotYetEffective(string status)
        {
            var cert = Cert(effective: Today.AddDays(5), expiration: Today.AddYears(1), status: status);

            CertificateVisibility.IsPubliclyLive(cert, Today).Should().BeFalse(
                "both public statuses are subject to the effective-date gate, not just Paid");
        }

        // ---------- the EF predicate and the in-memory helper must agree ----------

        [Fact]
        public async Task QueryPredicate_MatchesInMemoryHelper_AcrossTheWholeBoundary()
        {
            var certs = new[]
            {
                Cert(Today.AddDays(-1), Today.AddYears(1), id: 1),  // effective yesterday  → visible
                Cert(Today,             Today.AddYears(1), id: 2),  // effective today      → visible
                Cert(Today.AddDays(1),  Today.AddYears(1), id: 3),  // effective tomorrow   → hidden
                Cert(Today.AddYears(-1), Today,            id: 4),  // expires today        → visible
                Cert(Today.AddYears(-1), Today.AddDays(-1), id: 5), // expired yesterday    → hidden
                Cert(Today.AddDays(-1), Today.AddYears(1), CertificateStatusHelper.StatusDraft, id: 6), // draft → hidden
            };
            _context.Certificates.AddRange(certs);
            await _context.SaveChangesAsync();

            var fromQuery = await _context.Certificates
                .WherePubliclyLive(Today)
                .Select(c => c.Id)
                .OrderBy(id => id)
                .ToListAsync();

            var fromHelper = certs
                .Where(c => CertificateVisibility.IsPubliclyLive(c, Today))
                .Select(c => c.Id)
                .OrderBy(id => id)
                .ToList();

            fromQuery.Should().Equal(new[] { 1, 2, 4 });
            fromQuery.Should().Equal(fromHelper,
                "the IQueryable predicate and the single-entity helper must never disagree — " +
                "a listing that hides a cert while the detail page shows it is the same defect again");
        }

        [Fact]
        public async Task WhereEffectiveDateReached_AppliesOnlyTheLowerBound()
        {
            // The partner API composes this on top of its own status + partner filters, so it must
            // NOT independently drop expired or non-public rows.
            _context.Certificates.AddRange(
                Cert(Today.AddDays(1), Today.AddYears(1), id: 1),                                        // future  → dropped
                Cert(Today.AddDays(-1), Today.AddDays(-1), id: 2),                                       // expired → kept
                Cert(Today.AddDays(-1), Today.AddYears(1), CertificateStatusHelper.StatusDraft, id: 3)); // draft   → kept
            await _context.SaveChangesAsync();

            var ids = await _context.Certificates
                .WhereEffectiveDateReached(Today)
                .Select(c => c.Id)
                .OrderBy(id => id)
                .ToListAsync();

            ids.Should().Equal(new[] { 2, 3 });
        }
    }
}

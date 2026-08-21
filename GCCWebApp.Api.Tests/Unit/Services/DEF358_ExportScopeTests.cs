using System.Reflection;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// DEF-358 (export half) — `CertificateService.ApplyExportSecurityScope` guards an
    /// **[AllowAnonymous]** endpoint (`CertificateController.ExportCertificates`). Before this fix
    /// it filtered on `PublicStatuses` alone, so an anonymous CSV export handed out certificates
    /// that the public tile view, the certificate detail page, the PDF endpoints and the Amazon
    /// partner feed all withheld.
    ///
    /// Its doc comment said it "mirrors CertificateTileExternalApiController" — and that is exactly
    /// how it drifted. The EXTERNAL tile view is deliberately ungated for the effective date; the
    /// anonymous branch of this method is not external, and had to mirror the PUBLIC view instead.
    ///
    /// The External cases below are **not redundant** — they assert the carve-out is deliberate, so
    /// a later "make it all consistent" refactor cannot silently take visibility away from the
    /// owning company.
    /// </summary>
    public class DEF358_ExportScopeTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private static readonly DateTime Today = DateTime.UtcNow.Date;

        // Ids are meaningful: 1 = live, 2 = not yet effective.
        private const int LiveCertId = 1;
        private const int FutureCertId = 2;

        public DEF358_ExportScopeTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            Seed();
        }

        public void Dispose() => _context.Dispose();

        private void Seed()
        {
            var amazon = new Partner { Id = 1, Name = "Amazon" };
            _context.Partners.Add(amazon);

            _context.Certificates.AddRange(
                new Certificate
                {
                    Id = LiveCertId,
                    Number = "26-LIVE",
                    Status = CertificateStatusHelper.StatusClientApprovedPaid,
                    EffectiveDate = Today.AddDays(-30),
                    ExpirationDate = Today.AddYears(1),
                    CompanyId = 7,
                    Partners = new List<Partner> { amazon }
                },
                new Certificate
                {
                    Id = FutureCertId,
                    Number = "26-FUTURE",
                    Status = CertificateStatusHelper.StatusClientApprovedPaid,
                    EffectiveDate = Today.AddDays(30),   // staged ahead — the DEF-358 case
                    ExpirationDate = Today.AddYears(1),
                    CompanyId = 7,
                    Partners = new List<Partner> { amazon }
                });

            _context.SaveChanges();
        }

        /// <summary>
        /// ApplyExportSecurityScope is private static — invoked by reflection so the test exercises
        /// the real predicate rather than a reimplementation of it. (Same approach the expiration
        /// service tests use for the static throttle.)
        /// </summary>
        private List<int> ScopedIds(string? role, List<string>? partners = null, List<int>? companies = null)
        {
            var method = typeof(CertificateService).GetMethod(
                "ApplyExportSecurityScope",
                BindingFlags.NonPublic | BindingFlags.Static);

            method.Should().NotBeNull(
                "ApplyExportSecurityScope is the single security predicate for CSV export — if it " +
                "was renamed or moved, this test must be repointed, not deleted");

            var request = new CertificateListRequest
            {
                UserRole = role!,
                AllowedPartnerNames = partners ?? new List<string>(),
                AllowedCompanyIds = companies ?? new List<int>()
            };

            var scoped = (IQueryable<Certificate>)method!.Invoke(
                null, new object[] { _context.Certificates.AsQueryable(), request })!;

            return scoped.Select(c => c.Id).OrderBy(id => id).ToList();
        }

        // ---------- the defect: anonymous CSV export ----------

        [Fact]
        public void PublicRole_ExcludesNotYetEffectiveCertificate()
        {
            var ids = ScopedIds("Public");

            ids.Should().Equal(new[] { LiveCertId },
                "the anonymous CSV export must not hand out a certificate the public site withholds");
            ids.Should().NotContain(FutureCertId);
        }

        [Fact]
        public void NullRole_CoalescesToPublic_AndIsGated()
        {
            // `var role = request.UserRole ?? "Public"` — a null role IS gated.
            var ids = ScopedIds(null);

            ids.Should().Equal(new[] { LiveCertId },
                "a null role coalesces to Public and must get the anonymous treatment");
        }

        [Fact]
        public void UnrecognisedRole_FallsThroughUnrestricted_ByDesign()
        {
            // Documents actual behaviour rather than asserting a wish. The if/else chain handles
            // Partner / External / Public and lets everything else reach the unrestricted tail —
            // that tail is precisely how Admin / Analyst / Internal get full access, so an
            // unrecognised role is indistinguishable from an internal one and is fail-OPEN.
            //
            // Not a DEF-358 defect and NOT fixed here: closing it means enumerating the internal
            // roles explicitly, which is a behaviour change for every export caller and needs its
            // own decision. In practice `CertificateController.GetUserRole()` returns "Public" for
            // anonymous callers, so the open path is not reachable from the anonymous endpoint —
            // this test exists so the day someone widens role plumbing, the exposure is already
            // written down.
            var ids = ScopedIds("SomeRoleThatDoesNotExist");

            ids.Should().Equal(new[] { LiveCertId, FutureCertId },
                "unknown roles currently fall through to unrestricted — if this ever starts " +
                "failing, the role chain was changed and that is a security-relevant change");
        }

        // ---------- partner parity with the Amazon API ----------

        [Fact]
        public void PartnerRole_ExcludesNotYetEffectiveCertificate()
        {
            var ids = ScopedIds("Partner", partners: new List<string> { "Amazon" });

            ids.Should().Equal(new[] { LiveCertId },
                "a partner's CSV must match what the partner API returns — the API 404s this cert");
        }

        [Fact]
        public void PartnerRole_WithNoAssociation_GetsNothing()
        {
            ScopedIds("Partner").Should().BeEmpty();
        }

        // ---------- the deliberate carve-out ----------

        [Fact]
        public void ExternalRole_STILL_SEES_ItsOwnNotYetEffectiveCertificate()
        {
            var ids = ScopedIds("External", companies: new List<int> { 7 });

            ids.Should().Contain(FutureCertId,
                "DEF-358 scope decision (Shawn, 2026-08-17): the owning company keeps visibility of " +
                "its own upcoming certificates. If a consistency refactor ever gates External, this " +
                "test must fail loudly rather than the behaviour changing silently");
            ids.Should().Equal(new[] { LiveCertId, FutureCertId });
        }

        [Fact]
        public void AdminRole_IsUnrestricted()
        {
            ScopedIds("Admin").Should().Equal(new[] { LiveCertId, FutureCertId },
                "internal roles see everything — staging certs early is deliberate analyst workflow");
        }
    }
}

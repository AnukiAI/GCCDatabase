using FluentAssertions;
using GCCWebApp.Api.Tests.TestUtilities;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// DEF-337 (completion) — the acting-user fallback for machine-initiated changes.
    ///
    /// Why this exists: the v1 API authenticates with an API key, which has no Users row behind
    /// it, but AuditHistory.ChangedBy is NOT NULL with FK_AuditHistory_Users_ChangedBy. Without
    /// some resolvable user, an API delete physically cannot be audited — so the endpoints were
    /// left writing nothing at all.
    ///
    /// The fix resolves a service account named by configuration (Audit:SystemUserEmail),
    /// currently pointed at the admin account. That makes the audit trail depend on a config
    /// value, and a config value that silently stops resolving would put us straight back to
    /// unaudited deletes with only a log line to notice it. These tests pin all three outcomes.
    /// </summary>
    public class DEF337_AuditSystemUserFallbackTests : IDisposable
    {
        private const string AdminEmail = "swest@anukiconsulting.com";

        private readonly ApplicationDbContext _context;

        public DEF337_AuditSystemUserFallbackTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _context.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                Email = AdminEmail,
                PasswordHash = "x",
                Status = "active",
                TwoFactorMethod = "Email"
            });
            _context.SaveChanges();
        }

        public void Dispose() => _context.Dispose();

        private AuditService CreateService(string? systemUserEmail)
        {
            var settings = new Dictionary<string, string?>();
            if (systemUserEmail != null)
                settings["Audit:SystemUserEmail"] = systemUserEmail;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            // No HttpContext — this is the API-key case: nothing to resolve from claims either.
            return new AuditService(
                _context,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<ILogger<AuditService>>().Object,
                configuration);
        }

        private Task<int> DeleteWithNoActingUser(AuditService service) =>
            service.LogEntityDeleteWithChildrenAsync(
                "500", "26-9500",
                new Certificate { Id = 500, Number = "26-9500", CompanyId = 1, CertificationTypeId = 1 },
                Array.Empty<AuditChildSnapshot>(),
                userId: 0, username: string.Empty,
                reason: "deleted via v1 API", changeSource: AuditChangeSource.API);

        [Fact]
        public async Task WhenConfiguredToAnExistingAccount_TheDeleteIsAudited_AndAttributedToThatAccount()
        {
            var written = await DeleteWithNoActingUser(CreateService(AdminEmail));

            written.Should().Be(1);

            var row = await _context.AuditHistory.SingleAsync();
            row.Action.Should().Be(AuditAction.Delete);
            row.EntityId.Should().Be("500");
            row.ChangedBy.Should().Be(1, "the row must reference a real Users row — ChangedBy is a NOT NULL FK");
            row.ChangedByUsername.Should().Be(AdminEmail,
                "attribution has to name the service account, so a machine-initiated delete is " +
                "distinguishable from that person actually clicking delete");
            row.OldValue.Should().NotBeNullOrWhiteSpace("a delete with no snapshot is not restorable");
        }

        [Fact]
        public async Task WhenNotConfigured_NothingIsWritten_RatherThanAnUnpersistableRow()
        {
            var written = await DeleteWithNoActingUser(CreateService(null));

            written.Should().Be(0);
            (await _context.AuditHistory.CountAsync()).Should().Be(0,
                "writing ChangedBy = 0 would throw on the FK inside SaveChanges and be swallowed — " +
                "the exact silent-loss mode DEF-187 and DEF-337 were both raised for");
        }

        [Fact]
        public async Task WhenConfiguredToAnAccountThatDoesNotExist_NothingIsWritten()
        {
            // The failure mode to guard: someone renames or deactivates the account and the audit
            // trail quietly stops. It must fail closed, not write a row that cannot persist.
            var written = await DeleteWithNoActingUser(CreateService("nobody@anukiconsulting.com"));

            written.Should().Be(0);
            (await _context.AuditHistory.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task AnExplicitActingUserAlwaysWins_OverTheConfiguredServiceAccount()
        {
            _context.Users.Add(new User
            {
                Id = 2,
                Username = "analyst",
                Email = "jane.analyst@anukiconsulting.com",
                PasswordHash = "x",
                Status = "active",
                TwoFactorMethod = "Email"
            });
            await _context.SaveChangesAsync();

            await CreateService(AdminEmail).LogEntityDeleteWithChildrenAsync(
                "500", "26-9500",
                new Certificate { Id = 500, Number = "26-9500", CompanyId = 1, CertificationTypeId = 1 },
                Array.Empty<AuditChildSnapshot>(),
                userId: 2, username: "jane.analyst@anukiconsulting.com",
                reason: "deleted in the UI", changeSource: AuditChangeSource.WebUI);

            var row = await _context.AuditHistory.SingleAsync();
            row.ChangedBy.Should().Be(2,
                "a real signed-in user must never be masked by the service-account fallback");
        }
    }
}

using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// DEF-385: "Trust this device for 30 days".
    ///
    /// These are security guarantees, not behaviour preferences — each test is written so that
    /// REMOVING the guarantee fails it. A trusted device is a 2FA bypass, so the interesting cases
    /// are the ones where it must be REFUSED.
    /// </summary>
    public class TrustedDeviceServiceTests
    {
        private static ApplicationDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ApplicationDbContext(options);
        }

        private static TrustedDeviceService NewService(ApplicationDbContext ctx)
            => new TrustedDeviceService(ctx, NullLogger<TrustedDeviceService>.Instance);

        [Fact]
        public async Task TrustedDevice_IsAccepted_ForTheUserItWasIssuedTo()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var token = await svc.TrustDeviceAsync(userId: 1, deviceLabel: "Chrome", ip: "10.0.0.1");

            (await svc.IsTrustedDeviceAsync(1, token)).Should().BeTrue();
        }

        [Fact]
        public async Task TrustedDevice_IsRefused_ForADifferentUserOnTheSameBrowser()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            // User 1 trusts this browser; user 2 then signs in from the very same browser, so the
            // same cookie is presented. It must not skip user 2's second factor.
            var token = await svc.TrustDeviceAsync(userId: 1, deviceLabel: "Chrome", ip: null);

            (await svc.IsTrustedDeviceAsync(2, token)).Should().BeFalse();
        }

        [Fact]
        public async Task TrustedDevice_IsRefused_OnceExpired()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var token = await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);

            // Expiry is enforced server-side against the stored row — the cookie's own lifetime is a
            // client-side hint and is not what the decision rests on.
            var row = await ctx.Set<TrustedDevice>().SingleAsync();
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await ctx.SaveChangesAsync();

            (await svc.IsTrustedDeviceAsync(1, token)).Should().BeFalse();
        }

        [Fact]
        public async Task TrustedDevice_IsRefused_OnceRevoked()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var token = await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);
            var revoked = await svc.RevokeAllForUserAsync(1, "Password was reset");

            revoked.Should().Be(1);
            (await svc.IsTrustedDeviceAsync(1, token)).Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-real-token")]
        public async Task UnknownOrMissingToken_IsRefused_AndDoesNotThrow(string? token)
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);
            await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);

            (await svc.IsTrustedDeviceAsync(1, token)).Should().BeFalse();
        }

        [Fact]
        public async Task RawToken_IsNeverStored_OnlyItsHash()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var token = await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);
            var row = await ctx.Set<TrustedDevice>().SingleAsync();

            // A database read must not yield a usable cookie.
            row.TokenHash.Should().NotBe(token);
            row.TokenHash.Should().Be(TrustedDeviceService.HashToken(token));
            row.TokenHash.Should().HaveLength(64, "SHA-256 as hex");
        }

        [Fact]
        public async Task EachTrust_GetsAnIndependentToken()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var a = await svc.TrustDeviceAsync(userId: 1, deviceLabel: "laptop", ip: null);
            var b = await svc.TrustDeviceAsync(userId: 1, deviceLabel: "phone", ip: null);

            a.Should().NotBe(b, "two devices must not share a token");
            (await svc.IsTrustedDeviceAsync(1, a)).Should().BeTrue();
            (await svc.IsTrustedDeviceAsync(1, b)).Should().BeTrue();
        }

        [Fact]
        public async Task RevokeAll_RetiresEveryDeviceForThatUserAndNoOtherUsers()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var mineA = await svc.TrustDeviceAsync(userId: 1, deviceLabel: "laptop", ip: null);
            var mineB = await svc.TrustDeviceAsync(userId: 1, deviceLabel: "phone", ip: null);
            var theirs = await svc.TrustDeviceAsync(userId: 2, deviceLabel: "laptop", ip: null);

            var count = await svc.RevokeAllForUserAsync(1, "2FA was disabled");

            count.Should().Be(2);
            (await svc.IsTrustedDeviceAsync(1, mineA)).Should().BeFalse();
            (await svc.IsTrustedDeviceAsync(1, mineB)).Should().BeFalse();
            (await svc.IsTrustedDeviceAsync(2, theirs)).Should().BeTrue("another user's devices are untouched");
        }

        [Fact]
        public async Task RevokedRows_AreKept_SoTheAuditTrailSurvives()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);
            await svc.RevokeAllForUserAsync(1, "Password was reset");

            var row = await ctx.Set<TrustedDevice>().SingleAsync();
            row.RevokedAt.Should().NotBeNull();
            row.RevokedReason.Should().Be("Password was reset");
        }

        [Fact]
        public async Task SuccessfulUse_IsRecorded()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            var token = await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);
            (await ctx.Set<TrustedDevice>().SingleAsync()).LastUsedAt.Should().BeNull();

            await svc.IsTrustedDeviceAsync(1, token);

            (await ctx.Set<TrustedDevice>().SingleAsync()).LastUsedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DefaultTrust_LastsThirtyDays()
        {
            using var ctx = NewContext();
            var svc = NewService(ctx);

            await svc.TrustDeviceAsync(userId: 1, deviceLabel: null, ip: null);

            var row = await ctx.Set<TrustedDevice>().SingleAsync();
            (row.ExpiresAt - row.CreatedAt).TotalDays.Should().BeApproximately(30, 0.01,
                "the checkbox promises 30 days");
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Services
{
    /// <summary>
    /// DEF-385: "Trust this device for 30 days" on the 2FA challenge.
    /// See <see cref="TrustedDevice"/> for the security model.
    /// </summary>
    public interface ITrustedDeviceService
    {
        /// <summary>
        /// Issues a new trust for this user and returns the RAW token to put in the cookie.
        /// The raw value is returned once and never stored.
        /// </summary>
        Task<string> TrustDeviceAsync(int userId, string? deviceLabel, string? ip, int days = TrustedDeviceService.DefaultTrustDays);

        /// <summary>
        /// True when <paramref name="rawToken"/> is a live trust belonging to <paramref name="userId"/>.
        /// Records the use. Any failure — unknown, expired, revoked, or belonging to another user —
        /// returns false and never throws.
        /// </summary>
        Task<bool> IsTrustedDeviceAsync(int userId, string? rawToken);

        /// <summary>Revokes every trust for a user. Used when the password changes or 2FA is turned off.</summary>
        Task<int> RevokeAllForUserAsync(int userId, string reason);
    }

    public class TrustedDeviceService : ITrustedDeviceService
    {
        public const int DefaultTrustDays = 30;

        /// <summary>Cookie name. `GCC.` prefix matches the existing `GCC.TestGate` convention.</summary>
        public const string CookieName = "GCC.TrustedDevice";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<TrustedDeviceService> _logger;

        public TrustedDeviceService(ApplicationDbContext context, ILogger<TrustedDeviceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> TrustDeviceAsync(int userId, string? deviceLabel, string? ip, int days = DefaultTrustDays)
        {
            // 256 bits from a CSPRNG. Base64Url so it is cookie-safe without escaping.
            var raw = Base64UrlToken(32);

            var device = new TrustedDevice
            {
                UserId = userId,
                TokenHash = HashToken(raw),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(days),
                DeviceLabel = Truncate(deviceLabel, 400),
                CreatedFromIp = Truncate(ip, 64)
            };

            _context.Set<TrustedDevice>().Add(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Trusted device created for user {UserId}, expires {ExpiresAt:u}", userId, device.ExpiresAt);
            return raw;
        }

        public async Task<bool> IsTrustedDeviceAsync(int userId, string? rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return false;

            var hash = HashToken(rawToken);
            var now = DateTime.UtcNow;

            // Matched on (hash AND userId) together, so a token issued to one account can never
            // satisfy the check for another account signing in from the same browser.
            var device = await _context.Set<TrustedDevice>()
                .FirstOrDefaultAsync(d => d.TokenHash == hash && d.UserId == userId);

            if (device == null)
            {
                _logger.LogInformation("Trusted-device cookie presented for user {UserId} did not match a stored device", userId);
                return false;
            }

            if (!device.IsActive(now))
            {
                _logger.LogInformation(
                    "Trusted device {DeviceId} for user {UserId} is no longer valid (expires {ExpiresAt:u}, revoked {RevokedAt:u})",
                    device.Id, userId, device.ExpiresAt, device.RevokedAt);
                return false;
            }

            device.LastUsedAt = now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> RevokeAllForUserAsync(int userId, string reason)
        {
            var now = DateTime.UtcNow;
            var active = await _context.Set<TrustedDevice>()
                .Where(d => d.UserId == userId && d.RevokedAt == null)
                .ToListAsync();

            if (active.Count == 0) return 0;

            foreach (var d in active)
            {
                d.RevokedAt = now;
                d.RevokedReason = Truncate(reason, 200);
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Revoked {Count} trusted device(s) for user {UserId}: {Reason}", active.Count, userId, reason);
            return active.Count;
        }

        /// <summary>
        /// SHA-256 hex. Appropriate here and NOT a substitute for password hashing: the input is 256
        /// bits of randomness, so there is nothing to brute-force. Hashing at all is what stops a
        /// database read from yielding a usable cookie.
        /// </summary>
        /// <remarks>
        /// Public so the tests can assert that the stored value really is the hash and not the token
        /// itself. Exposing it leaks nothing: it is a deterministic hash of a value the caller
        /// already holds, and knowing the algorithm does not help anyone guess a 256-bit token.
        /// </remarks>
        public static string HashToken(string rawToken)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken)));
        }

        private static string Base64UrlToken(int bytes)
        {
            var buf = RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToBase64String(buf).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string? Truncate(string? value, int max)
            => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value.Substring(0, max));
    }
}

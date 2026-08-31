using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// DEF-385: one browser that a user has chosen to trust, so it is not challenged for a
    /// second factor again until the trust expires.
    ///
    /// This is a row per device rather than a field on <see cref="User"/> (which is where the rest
    /// of the 2FA state lives) because a person has several devices, each needs its own expiry, and
    /// each has to be revocable on its own. A JSON blob on the user could not do any of that.
    ///
    /// SECURITY MODEL — worth stating plainly, because "remember this device" is easy to get wrong:
    ///
    ///  * This suppresses the SECOND factor only. The password is still required on every sign-in.
    ///    A stolen cookie alone gets an attacker nothing; it only removes the 2FA step for someone
    ///    who ALREADY has the correct password.
    ///  * The raw token never lands in the database. Only its SHA-256 hash is stored, so a database
    ///    read cannot be turned into a usable cookie — the same reasoning as the recovery codes,
    ///    which are hashed by <c>EnhancedTwoFactorService.HashRecoveryCode</c>.
    ///    SHA-256 without a work factor is correct HERE (and would not be for a password): the token
    ///    is 256 bits of cryptographic randomness, so there is no dictionary to attack.
    ///  * Trust is bound to <see cref="UserId"/>. A device trusted for one account does not skip 2FA
    ///    for a different account signing in from the same browser.
    ///  * Expiry is enforced SERVER-SIDE against <see cref="ExpiresAt"/>. The cookie also carries an
    ///    expiry, but a cookie lifetime is a client-side hint the client can ignore, so it is not
    ///    what the decision rests on.
    ///  * Every trusted device for a user is revoked when their password changes or when they turn
    ///    2FA off — the two events that mean "the previous trust decision may no longer be the
    ///    user's own".
    /// </summary>
    public class TrustedDevice
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// SHA-256 (hex) of the random token held in the client's cookie. The raw token is shown to
        /// the browser once, at creation, and is never recoverable from here.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>The authoritative expiry. Checked on every use; the cookie's own expiry is not trusted.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Set each time the device successfully skips a challenge, so stale trusts are visible.</summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>Non-null once revoked. Revoked rows are kept rather than deleted so the audit trail survives.</summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>Why it was revoked (password change, 2FA disabled, user action) — for support and audit.</summary>
        [MaxLength(200)]
        public string? RevokedReason { get; set; }

        /// <summary>
        /// Truncated user-agent, so a person can recognise the entry if we ever surface a
        /// "your trusted devices" screen. Deliberately not used in the trust decision — user-agent
        /// strings are trivially forged and pinning to one would only break on browser updates.
        /// </summary>
        [MaxLength(400)]
        public string? DeviceLabel { get; set; }

        [MaxLength(64)]
        public string? CreatedFromIp { get; set; }

        /// <summary>Usable right now: not revoked and not past its expiry.</summary>
        public bool IsActive(DateTime asOfUtc) => RevokedAt == null && ExpiresAt > asOfUtc;
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GCCWebApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public required string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public required string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? FullName { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // DEF-121: Never serialize credential material in API responses. The C# `required` keyword
        // was dropped here because System.Text.Json cannot reconcile [JsonIgnore] with `required` —
        // deserialization throws "marked required but does not specify a setter" if a User object
        // ever round-trips through JSON (Partner API key auth was hitting this). EF Core still
        // enforces NOT NULL at the DB schema level, and every `new User { ... }` site in the code
        // explicitly assigns this field.
        [JsonIgnore]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public required string Status { get; set; } = "active"; // active/inactive/locked

        public bool IsActive { get; set; } = true;

        public bool IsStaff { get; set; } = false;

        public bool IsSuperuser { get; set; } = false;

        public bool RequirePasswordReset { get; set; } = false;

        public DateTime? LastLogin { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }
        
        // Enhanced security fields
        [JsonIgnore]
        public string? TwoFactorSecret { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;

        public required string TwoFactorMethod { get; set; } = "Email"; // Email, SMS, TOTP, Both

        public string? PhoneNumber { get; set; }

        public bool IsPhoneVerified { get; set; } = false;

        public bool IsEmailVerified { get; set; } = true; // Default to true for existing users

        [JsonIgnore]
        public string? RecoveryCodes { get; set; } // JSON array of backup codes

        public DateTime? LastSecurityAudit { get; set; }

        [JsonIgnore]
        public string? SecurityQuestions { get; set; } // JSON array

        public bool MustChangePassword { get; set; } = false;

        public int MaxConcurrentSessions { get; set; } = 3;

        [JsonIgnore]
        public string? AllowedIPs { get; set; } // Comma-separated list

        [JsonIgnore]
        public string? PasswordResetToken { get; set; }

        [JsonIgnore]
        public DateTime? PasswordResetTokenExpiry { get; set; }

        public bool RequirePasswordChange { get; set; } = false;

        public DateTime? PasswordExpiresAt { get; set; }

        [JsonIgnore]
        public string? PasswordHistory { get; set; } // JSON array of previous password hashes
        
        public int MaxPasswordHistory { get; set; } = 5;
        
        public bool IsLockedOut => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
        
        public bool IsPasswordExpired => PasswordExpiresAt.HasValue && PasswordExpiresAt.Value < DateTime.UtcNow;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // CR-020 (revised 2026-06-05, QC #106): the user's own company / employer, stored as
        // FREE TEXT — not an FK to Companies. Values like "GCC", "Amazon Corporate", "Novi"
        // are valid employers that are not cert companies, so the dropdown was dropped.
        // Distinct from Companies below, which is the data-access scope ("Company Access").
        // Label/display only — never read by any access-scoping path. Optional.
        [MaxLength(255)]
        public string? UserCompany { get; set; }

        // Navigation properties
        public ICollection<Certificate> CreatedCertificates { get; set; } = new List<Certificate>();
        public ICollection<Certificate> UpdatedCertificates { get; set; } = new List<Certificate>();
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<Company> Companies { get; set; } = new List<Company>();
        public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    }
}
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? FullName { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "active"; // active/inactive/locked

        public bool IsActive { get; set; } = true;

        public bool IsStaff { get; set; } = false;

        public bool IsSuperuser { get; set; } = false;

        public bool RequirePasswordReset { get; set; } = false;

        public DateTime? LastLogin { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }
        
        // Enhanced security fields
        public string? TwoFactorSecret { get; set; }
        
        public bool TwoFactorEnabled { get; set; } = false;
        
        public string TwoFactorMethod { get; set; } = "Email"; // Email, SMS, Both
        
        public string? PhoneNumber { get; set; }
        
        public bool IsPhoneVerified { get; set; } = false;
        
        public bool IsEmailVerified { get; set; } = true; // Default to true for existing users
        
        public string? RecoveryCodes { get; set; } // JSON array of backup codes
        
        public DateTime? LastSecurityAudit { get; set; }
        
        public string? SecurityQuestions { get; set; } // JSON array
        
        public bool MustChangePassword { get; set; } = false;
        
        public int MaxConcurrentSessions { get; set; } = 3;
        
        public string? AllowedIPs { get; set; } // Comma-separated list
        
        public bool RequirePasswordChange { get; set; } = false;
        
        public DateTime? PasswordExpiresAt { get; set; }
        
        public string? PasswordHistory { get; set; } // JSON array of previous password hashes
        
        public int MaxPasswordHistory { get; set; } = 5;
        
        public bool IsLockedOut => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
        
        public bool IsPasswordExpired => PasswordExpiresAt.HasValue && PasswordExpiresAt.Value < DateTime.UtcNow;

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Certificate> CreatedCertificates { get; set; } = new List<Certificate>();
        public ICollection<Certificate> UpdatedCertificates { get; set; } = new List<Certificate>();
        public ICollection<Role> Roles { get; set; } = new List<Role>();
        public ICollection<Company> Companies { get; set; } = new List<Company>();
    }
}
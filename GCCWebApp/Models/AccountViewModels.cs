using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or Username")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        // QC #181 alignment: same strict pattern as Admin User/Company ViewModels and
        // CertificateEditViewModel.CompanyEmail. Forgot-password needs a real deliverable
        // email (to receive the reset link), so [EmailAddress]'s "a@b" tolerance is wrong here.
        [RegularExpression(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid email address (e.g., name@example.com).")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        // QC #181 alignment: same strict pattern. The Email here comes from the reset link
        // (carrying the user's account email) and is verified against the stored user record;
        // if a stored email isn't in this format the user couldn't have received the link in
        // the first place, so the constraint is safe. Audit DB users with the SQL in DEF-172
        // before deploying to a new environment.
        [RegularExpression(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid email address (e.g., name@example.com).")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class SetupTotpViewModel
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Display(Name = "Verification Code")]
        public string TotpCode { get; set; } = string.Empty;
    }
}
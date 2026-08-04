using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using QRCoder;
using BCrypt.Net;
using GCCWebApp.Configuration;
using GCCWebApp.Services;
using GCCWebApp.Services.Audit;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly AzureAdB2COptions? _azureAdB2COptions;
        private readonly bool _isAzureAdB2CConfigured;
        private readonly IConfiguration _configuration;
        private readonly IEnhancedTwoFactorService _twoFactorService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ICertificateExpirationService _certificateExpirationService;

        public AccountController(
            ILogger<AccountController> logger,
            IOptions<AzureAdB2COptions>? azureAdB2COptions,
            IConfiguration configuration,
            IEnhancedTwoFactorService twoFactorService,
            IEmailService emailService,
            ApplicationDbContext context,
            IAuditService auditService,
            ICertificateExpirationService certificateExpirationService)
        {
            _logger = logger;
            _azureAdB2COptions = azureAdB2COptions?.Value;
            _configuration = configuration;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
            _context = context;
            _auditService = auditService;
            _certificateExpirationService = certificateExpirationService;

            // Check if Azure AD B2C is properly configured
            _isAzureAdB2CConfigured = _azureAdB2COptions != null &&
                !string.IsNullOrEmpty(_azureAdB2COptions.ClientId) &&
                !_azureAdB2COptions.ClientId.Contains("{") &&
                !string.IsNullOrEmpty(_azureAdB2COptions.TenantId) &&
                !_azureAdB2COptions.TenantId.Contains("{");
        }

        /// <summary>
        /// Initiate sign-in process
        /// </summary>
        [AllowAnonymous]
        public IActionResult SignIn(string? returnUrl = null)
        {
            // If user is already authenticated, redirect to return URL or dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            if (_isAzureAdB2CConfigured && _azureAdB2COptions != null)
            {
                // Use Azure AD B2C authentication
                var redirectUrl = Url.Action(nameof(SignedIn), "Account", new { returnUrl });
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
                
                return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
            }
            else
            {
                // Fall back to local login form
                return RedirectToAction(nameof(Login), new { returnUrl });
            }
        }

        /// <summary>
        /// Handle successful sign-in from Azure AD B2C
        /// </summary>
        [Authorize]
        public IActionResult SignedIn(string? returnUrl = null)
        {
            // Extract user information from claims
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("emails");
            var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(email))
            {
                // Set session variables
                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserName", name ?? email);
                
                // Determine user role based on email domain or other logic
                var userRole = DetermineUserRole(email);
                HttpContext.Session.SetString("UserRole", userRole);

                _logger.LogInformation("User {Email} signed in successfully with role {Role}", email, userRole);
            }

            return RedirectToLocal(returnUrl);
        }

        /// <summary>
        /// Display the login page (fallback for non-Azure AD B2C)
        /// </summary>
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null, string? error = null)
        {
            // If Azure AD B2C is configured, redirect to SignIn
            if (_isAzureAdB2CConfigured)
            {
                return RedirectToAction(nameof(SignIn), new { returnUrl });
            }

            // If user is already authenticated, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            // Handle error messages
            if (!string.IsNullOrEmpty(error))
            {
                switch (error)
                {
                    case "authentication_failed":
                        ViewBag.Error = "Authentication failed. Please check your credentials and try again.";
                        break;
                    case "access_denied":
                        ViewBag.Error = "Access was denied. Please contact your administrator for access.";
                        break;
                    case "session_expired":
                        ViewBag.Error = "Your session has expired due to inactivity. Please log in again.";
                        break;
                    default:
                        ViewBag.Error = error.Contains("session") ? error : "Authentication failed. Please try again.";
                        break;
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            var model = new LoginViewModel();
            return View(model);
        }

        /// <summary>
        /// Handle login form submission (fallback for non-Azure AD B2C)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Simple authentication logic for testing
            var (userRole, canonicalEmail, loginError) = ValidateUserDetailed(model.Email, model.Password);

            if (string.IsNullOrEmpty(userRole))
            {
                // DEF-209: server-side log keeps the specific reason; rendered form
                // shows the generic message returned by ValidateUserDetailed
                // (no "User not found" / "Invalid password" / "Account inactive"
                // differential — same string for every failure mode).
                _logger.LogWarning("Login failed for {Email}: {Reason}", model.Email, loginError);
                ModelState.AddModelError("", loginError);
                return View(model);
            }

            // DEF-113: from this point on use canonicalEmail (the user's actual Email column),
            // never model.Email. Form input may be a username; if we propagate it as the email
            // claim, every downstream lookup of "Users where Email == claim" silently misses
            // and external/role-scoped UIs return empty.
            if (string.IsNullOrEmpty(canonicalEmail)) canonicalEmail = model.Email;

            // Check if user requires password reset
            if (CheckRequirePasswordReset(canonicalEmail))
            {
                _logger.LogInformation("User {Email} requires password reset on login", canonicalEmail);
                TempData["ForceResetEmail"] = canonicalEmail;
                TempData["ForceResetRole"] = userRole;
                TempData["ForceResetReturnUrl"] = returnUrl;
                TempData["ForceResetRememberMe"] = model.RememberMe;
                return RedirectToAction("ForcePasswordReset");
            }

            // Check if 2FA is enabled
            var is2FAEnabled = _configuration.GetValue<bool>("Authentication:Enable2FA", true);

            if (is2FAEnabled)
            {
                try
                {
                    // Get user preferences for 2FA delivery method
                    var preferences = await _twoFactorService.GetUserPreferencesAsync(canonicalEmail);

                    // Generate and send 2FA code using preferred method
                    var code = _twoFactorService.GenerateCode();
                    _twoFactorService.StoreCode(canonicalEmail, code);
                    await _twoFactorService.SendCodeAsync(canonicalEmail, code, preferences.PreferredMethod, preferences.Email, preferences.PhoneNumber);

                    // Store user info temporarily in TempData (canonical email so Verify2FA hits the right user)
                    TempData["2FAEmail"] = canonicalEmail;
                    TempData["2FARole"] = userRole;
                    TempData["ReturnUrl"] = returnUrl;
                    TempData["RememberMe"] = model.RememberMe;
                    // DEF-334: "2FAMethod" write removed - it had ZERO readers anywhere (no view, no
                    // controller). Its siblings above ARE consumed by Verify2FA; this one never was,
                    // so it sat unread in the TempData cookie and was re-saved on every subsequent
                    // request, indefinitely. Same mechanism as DEF-318/DEF-330. If the sign-in screen
                    // should say which method the code went to, that is a UI change - reinstate the
                    // write together with the reader, never on its own.

                    _logger.LogInformation($"2FA code sent to {canonicalEmail} via {preferences.PreferredMethod}");
                    return RedirectToAction("TwoFactorAuthentication");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send 2FA code to {canonicalEmail}");
                    ModelState.AddModelError("", "Unable to send verification code. Please try again or contact support.");
                    return View(model);
                }
            }

            // Direct login without 2FA — use canonical email for all auth claims
            await SignInUser(canonicalEmail, userRole, model.RememberMe);
            return RedirectToLocal(returnUrl);
        }

        /// <summary>
        /// Initiate sign-out process (GET version for direct navigation)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            // If user is not authenticated, just redirect to home
            if (User.Identity?.IsAuthenticated != true)
            {
                _logger.LogInformation("Logout called but user is not authenticated");
                return RedirectToAction("CertificateTileView", "Certificate");
            }
            
            return await PerformLogout();
        }

        /// <summary>
        /// Initiate sign-out process (POST version for form submission)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> LogoutPost()
        {
            // If user is not authenticated, just redirect to home
            if (User.Identity?.IsAuthenticated != true)
            {
                _logger.LogInformation("LogoutPost called but user is not authenticated");
                return RedirectToAction("CertificateTileView", "Certificate");
            }

            return await PerformLogout();
        }

        /// <summary>
        /// Handle GET requests to LogoutPost (e.g. browser navigating directly to the URL).
        /// Redirects to the GET Logout endpoint instead of returning an error.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [Route("Account/LogoutPost")]
        public IActionResult LogoutPostGet()
        {
            return RedirectToAction("Logout");
        }

        /// <summary>
        /// Session expired handler — redirects to login with a message
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SessionExpired()
        {
            return RedirectToAction("Login", new { error = "session_expired" });
        }

        private async Task<IActionResult> PerformLogout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            try
            {
                _logger.LogInformation("User {Email} with role {Role} initiating logout", userEmail, userRole);

                // Clear session data first
                HttpContext.Session.Clear();

                if (_isAzureAdB2CConfigured && User.Identity?.AuthenticationType == OpenIdConnectDefaults.AuthenticationScheme)
                {
                    // Sign out from Azure AD B2C
                    var callbackUrl = Url.Action(nameof(SignedOut), "Account", values: null, protocol: Request.Scheme);
                    return SignOut(
                        new AuthenticationProperties { RedirectUri = callbackUrl },
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        OpenIdConnectDefaults.AuthenticationScheme);
                }
                else
                {
                    // Sign out from cookie authentication (handles all user roles)
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _logger.LogInformation("User {Email} with role {Role} signed out successfully", userEmail, userRole);
                    
                    // Redirect to public certificates page after logout
                    return RedirectToAction("CertificateTileView", "Certificate");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserEmail} with role {UserRole}", userEmail, userRole);
                
                // Even if there's an error, clear session and redirect to public page
                HttpContext.Session.Clear();
                return RedirectToAction("CertificateTileView", "Certificate");
            }
        }

        /// <summary>
        /// Handle post-signout redirect
        /// </summary>
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            // Redirect to public certificates page after sign out
            return RedirectToAction("CertificateTileView", "Certificate");
        }

        /// <summary>
        /// Reset password (Azure AD B2C)
        /// </summary>
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            if (_isAzureAdB2CConfigured && _azureAdB2COptions != null)
            {
                var properties = new AuthenticationProperties();
                properties.Items["policy"] = _azureAdB2COptions.ResetPasswordPolicyId;
                return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
            }
            else
            {
                return RedirectToAction(nameof(ForgotPassword));
            }
        }

        /// <summary>
        /// Edit profile (Azure AD B2C)
        /// </summary>
        [Authorize]
        public IActionResult EditProfile()
        {
            if (_isAzureAdB2CConfigured && _azureAdB2COptions != null)
            {
                var properties = new AuthenticationProperties();
                properties.Items["policy"] = _azureAdB2COptions.EditProfilePolicyId;
                return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
            }
            else
            {
                // Fallback to local profile page if implemented
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// Access denied page
        /// </summary>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Display the forgot password page (fallback)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        /// <summary>
        /// Handle forgot password form submission - generates token and sends reset email
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Always show the same confirmation to prevent email enumeration
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && !u.IsDeleted);

            if (user != null)
            {
                // Generate a secure random token
                var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
                var rawToken = Convert.ToBase64String(tokenBytes);

                // Store hashed token in DB (don't store raw token)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedToken = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawToken)));

                user.PasswordResetToken = hashedToken;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                // Build the reset link with the RAW token (not the hash)
                var urlEncodedToken = System.Net.WebUtility.UrlEncode(rawToken);
                var resetLink = $"{Request.Scheme}://{Request.Host}/Account/ResetPasswordWithToken?token={urlEncodedToken}&email={System.Net.WebUtility.UrlEncode(user.Email)}";

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #4a7c23;'>GreenCircle Certified Database</h1>
                            </div>
                            <h2>Password Reset Request</h2>
                            <p>Hello {user.FullName ?? user.Username},</p>
                            <p>We received a request to reset your password. Click the button below to set a new password:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' style='background-color: #4a7c23; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-size: 16px;'>Reset Password</a>
                            </div>
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #666; font-size: 14px;'>{resetLink}</p>
                            <p><strong>This link expires in 1 hour.</strong></p>
                            <p>If you did not request a password reset, please ignore this email. Your password will remain unchanged.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;' />
                            <p style='color: #999; font-size: 12px;'>This is an automated message from the GCC Database System. Please do not reply to this email.</p>
                        </div>
                    </body>
                    </html>";

                // DEF-209: fire-and-forget the SMTP send so the response time for
                // valid-email matches invalid-email. Before this change, the
                // synchronous send made valid-email responses ~3000ms vs ~30ms for
                // invalid-email — a 100× timing side-channel that defeated the
                // identical-body anti-enumeration design. Background task captures
                // its own exceptions; failures still log to the same _logger.
                var bgEmail = user.Email;
                var bgBody = emailBody;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(bgEmail, "Password Reset - GCC Database", bgBody);
                        _logger.LogInformation("Password reset email sent to {Email}", bgEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send password reset email to {Email}", bgEmail);
                    }
                });
            }
            else
            {
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", model.Email);
            }

            return View("ForgotPasswordConfirmation");
        }

        /// <summary>
        /// Display reset password form after clicking email link
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ResetPasswordWithToken(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Invalid password reset link.";
                return View("ResetPassword");
            }

            // Validate the token. The framework's query-string binder already
            // URL-decodes `token`; a second WebUtility.UrlDecode converts '+'
            // (a valid base64 char) into a space and breaks the hash compare.
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedToken = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token)));

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                !u.IsDeleted &&
                u.PasswordResetToken == hashedToken &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ViewBag.Error = "This password reset link is invalid or has expired. Please request a new one.";
                return View("ResetPassword");
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View("ResetPassword");
        }

        /// <summary>
        /// Handle password reset form submission (from email link)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithToken(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Token = model.Token;
                ViewBag.Email = model.Email;
                ViewBag.Error = "Please correct the errors below.";
                return View("ResetPassword");
            }

            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Token = model.Token;
                ViewBag.Email = model.Email;
                ViewBag.Error = "Passwords do not match.";
                return View("ResetPassword");
            }

            // Validate token. The MVC model binder already URL-decodes form
            // posts; a second WebUtility.UrlDecode converts '+' (a valid
            // base64 char) into a space and breaks the hash compare.
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedToken = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(model.Token)));

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == model.Email &&
                !u.IsDeleted &&
                u.PasswordResetToken == hashedToken &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ViewBag.Error = "This password reset link is invalid or has expired. Please request a new one.";
                return View("ResetPassword");
            }

            // Update the password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.RequirePasswordReset = false;
            user.RequirePasswordChange = false;
            user.MustChangePassword = false;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password successfully reset for {Email}", model.Email);

            return View("PasswordResetSuccess");
        }

        /// <summary>
        /// Display forced password reset page (when RequirePasswordReset flag is set)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForcePasswordReset()
        {
            var email = TempData["ForceResetEmail"]?.ToString();

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Force password reset page accessed without proper session data");
                return RedirectToAction("Login");
            }

            // Keep TempData for POST action
            TempData.Keep();

            ViewBag.Email = email;
            ViewBag.IsForced = true;
            return View("ForcePasswordReset");
        }

        /// <summary>
        /// Handle forced password reset form submission
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForcePasswordReset(string password, string confirmPassword)
        {
            var email = TempData["ForceResetEmail"]?.ToString();
            var role = TempData["ForceResetRole"]?.ToString();
            var returnUrl = TempData["ForceResetReturnUrl"]?.ToString();
            var rememberMe = TempData["ForceResetRememberMe"] as bool? ?? false;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("Force password reset attempted without proper session data");
                return RedirectToAction("Login");
            }

            // Validate passwords
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters long.";
                ViewBag.Email = email;
                ViewBag.IsForced = true;
                TempData["ForceResetEmail"] = email;
                TempData["ForceResetRole"] = role;
                TempData["ForceResetReturnUrl"] = returnUrl;
                TempData["ForceResetRememberMe"] = rememberMe;
                return View("ForcePasswordReset");
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                ViewBag.IsForced = true;
                TempData["ForceResetEmail"] = email;
                TempData["ForceResetRole"] = role;
                TempData["ForceResetReturnUrl"] = returnUrl;
                TempData["ForceResetRememberMe"] = rememberMe;
                return View("ForcePasswordReset");
            }

            try
            {
                // Update password in database
                var user = _context.Users.FirstOrDefault(u => u.Email == email || u.Username == email);
                if (user == null)
                {
                    _logger.LogError("User not found during forced password reset: {Email}", email);
                    return RedirectToAction("Login");
                }

                // DEF-113: pin downstream calls to the user's canonical email so claims/2FA/lookups
                // all resolve correctly even when the user originally typed a username.
                var canonicalEmail = user.Email;

                // Hash and update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.RequirePasswordReset = false;
                _context.SaveChanges();

                _logger.LogInformation("Password successfully reset for user {Email}", canonicalEmail);

                // Check if 2FA is required after password reset
                var is2FAEnabled = _configuration.GetValue<bool>("Authentication:Enable2FA", true);

                if (is2FAEnabled)
                {
                    // Proceed with 2FA flow
                    var preferences = await _twoFactorService.GetUserPreferencesAsync(canonicalEmail);
                    var code = _twoFactorService.GenerateCode();
                    _twoFactorService.StoreCode(canonicalEmail, code);
                    await _twoFactorService.SendCodeAsync(canonicalEmail, code, preferences.PreferredMethod, preferences.Email, preferences.PhoneNumber);

                    TempData["2FAEmail"] = canonicalEmail;
                    TempData["2FARole"] = role;
                    TempData["ReturnUrl"] = returnUrl;
                    TempData["RememberMe"] = rememberMe;
                    // DEF-334: "2FAMethod" write removed - it had ZERO readers anywhere (no view, no
                    // controller). Its siblings above ARE consumed by Verify2FA; this one never was,
                    // so it sat unread in the TempData cookie and was re-saved on every subsequent
                    // request, indefinitely. Same mechanism as DEF-318/DEF-330. If the sign-in screen
                    // should say which method the code went to, that is a UI change - reinstate the
                    // write together with the reader, never on its own.

                    return RedirectToAction("TwoFactorAuthentication");
                }

                // Sign in directly without 2FA — use canonical email
                await SignInUser(canonicalEmail, role, rememberMe);
                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forced password reset for {Email}", email);
                ViewBag.Error = "An error occurred while resetting your password. Please try again.";
                ViewBag.Email = email;
                ViewBag.IsForced = true;
                TempData["ForceResetEmail"] = email;
                TempData["ForceResetRole"] = role;
                TempData["ForceResetReturnUrl"] = returnUrl;
                TempData["ForceResetRememberMe"] = rememberMe;
                return View("ForcePasswordReset");
            }
        }

        /// <summary>
        /// Display the Two-Factor Authentication page
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult TwoFactorAuthentication()
        {
            if (TempData["2FAEmail"] == null)
            {
                _logger.LogWarning("2FA page accessed without proper session data");
                return RedirectToAction("Login");
            }
            
            // Keep the values for the post action
            TempData.Keep();
            return View("2FAAuthenticate");
        }

        /// <summary>
        /// Handle Two-Factor Authentication verification
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2FA(string authCode, bool trustDevice = false)
        {
            var email = TempData["2FAEmail"]?.ToString();
            var role = TempData["2FARole"]?.ToString();
            var returnUrl = TempData["ReturnUrl"]?.ToString();
            var rememberMe = TempData["RememberMe"] as bool? ?? false;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("2FA verification attempted without proper session data");
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(authCode))
            {
                ViewBag.Error = "Please enter a verification code.";
                TempData.Keep();
                return View("2FAAuthenticate");
            }

            // Try TOTP first, then fallback to email/SMS codes, then recovery codes
            var isValidCode = false;
            var normalizedCode = authCode.Replace("-", "").Trim();

            // 6-digit numeric codes: TOTP or email/SMS
            if (normalizedCode.Length == 6 && normalizedCode.All(char.IsDigit))
            {
                // Check if user has TOTP enabled
                var totpValid = await _twoFactorService.ValidateTotpCodeAsync(email, normalizedCode);
                if (totpValid)
                {
                    isValidCode = true;
                    _logger.LogInformation("TOTP verification successful for {Email}", email);
                }
                else
                {
                    // Fallback to email/SMS code validation
                    var emailSmsValid = _twoFactorService.ValidateCode(email, normalizedCode);
                    if (emailSmsValid)
                    {
                        isValidCode = true;
                        _logger.LogInformation("Email/SMS 2FA verification successful for {Email}", email);
                    }
                }
            }

            // 8-char alphanumeric: recovery code
            if (!isValidCode && normalizedCode.Length == 8)
            {
                var recoveryValid = await _twoFactorService.ValidateRecoveryCodeAsync(email, normalizedCode);
                if (recoveryValid)
                {
                    isValidCode = true;
                    _logger.LogInformation("Recovery code used for {Email}", email);
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (user != null)
                    {
                        await _auditService.LogAuditEventAsync(
                            AuditEntityType.User, user.Id.ToString(), user.Username,
                            AuditAction.RecoveryCodeUsed, user.Id, user.Username,
                            "Recovery code used for 2FA verification", changeSource: AuditChangeSource.WebUI);
                    }
                }
            }

            if (isValidCode)
            {
                await SignInUser(email, role, rememberMe || trustDevice);
                return RedirectToLocal(returnUrl);
            }

            // Log failed verification
            var failedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (failedUser != null)
            {
                await _auditService.LogAuditEventAsync(
                    AuditEntityType.User, failedUser.Id.ToString(), failedUser.Username,
                    AuditAction.TwoFactorVerificationFailed, failedUser.Id, failedUser.Username,
                    "2FA verification failed", changeSource: AuditChangeSource.WebUI);
            }

            _logger.LogWarning("2FA verification failed for {Email}", email);
            ViewBag.Error = "Invalid or expired code. Please try again.";
            TempData.Keep(); // Keep for retry
            return View("2FAAuthenticate");
        }

        /// <summary>
        /// Resend 2FA code
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Resend2FA()
        {
            var email = TempData["2FAEmail"]?.ToString();
            
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }
            
            try
            {
                var preferences = await _twoFactorService.GetUserPreferencesAsync(email);
                var success = await _twoFactorService.ResendCodeAsync(email, preferences);
                if (success)
                {
                    TempData.Keep(); // Keep session data
                    return Json(new { success = true, message = $"Verification code sent successfully via {preferences.PreferredMethod}." });
                }
                else
                {
                    return Json(new { success = false, message = "Please wait before requesting another code." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to resend 2FA code to {email}");
                return Json(new { success = false, message = "Failed to send code. Please try again." });
            }
        }

        /// <summary>
        /// Display 2FA preferences page
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TwoFactorPreferences()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            var preferences = await _twoFactorService.GetUserPreferencesAsync(userEmail);
            return View(preferences);
        }

        /// <summary>
        /// Save 2FA preferences
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorPreferences(TwoFactorPreferences model)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            model.UserId = userEmail;

            try
            {
                // If phone number is provided and method includes SMS, send verification
                if (!string.IsNullOrEmpty(model.PhoneNumber) && 
                    (model.PreferredMethod == TwoFactorDeliveryMethod.SMS || model.PreferredMethod == TwoFactorDeliveryMethod.Both))
                {
                    if (!model.IsPhoneVerified)
                    {
                        await _twoFactorService.SendVerificationCodeAsync(userEmail, model.PhoneNumber, TwoFactorDeliveryMethod.SMS);
                        TempData["PhoneVerificationSent"] = "Verification code sent to your phone. Please check your messages.";
                        TempData["TempPhoneNumber"] = model.PhoneNumber;
                        TempData["TempPreferredMethod"] = model.PreferredMethod.ToString();
                        return RedirectToAction("VerifyPhone");
                    }
                }

                await _twoFactorService.SaveUserPreferencesAsync(model);
                TempData["SuccessMessage"] = "Your 2FA preferences have been saved successfully.";
                return RedirectToAction("TwoFactorPreferences");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save 2FA preferences for {userEmail}");
                ModelState.AddModelError("", "Failed to save preferences. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Phone verification page
        /// </summary>
        [Authorize]
        [HttpGet]
        public IActionResult VerifyPhone()
        {
            if (TempData["PhoneVerificationSent"] == null)
            {
                return RedirectToAction("TwoFactorPreferences");
            }

            ViewBag.Message = TempData["PhoneVerificationSent"];
            TempData.Keep();
            return View();
        }

        /// <summary>
        /// Verify phone number
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(string verificationCode)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
            var phoneNumber = TempData["TempPhoneNumber"]?.ToString();
            var preferredMethod = TempData["TempPreferredMethod"]?.ToString();

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("TwoFactorPreferences");
            }

            if (string.IsNullOrWhiteSpace(verificationCode) || verificationCode.Length != 6)
            {
                ViewBag.Error = "Please enter a valid 6-digit verification code.";
                TempData.Keep();
                return View();
            }

            try
            {
                var isVerified = await _twoFactorService.VerifyPhoneAsync(userEmail, phoneNumber, verificationCode);
                if (isVerified)
                {
                    // Now save the preferences with verified phone
                    var preferences = await _twoFactorService.GetUserPreferencesAsync(userEmail);
                    preferences.PhoneNumber = phoneNumber;
                    preferences.IsPhoneVerified = true;
                    
                    if (Enum.TryParse<TwoFactorDeliveryMethod>(preferredMethod, out var method))
                    {
                        preferences.PreferredMethod = method;
                    }

                    await _twoFactorService.SaveUserPreferencesAsync(preferences);
                    TempData["SuccessMessage"] = "Phone number verified and 2FA preferences saved successfully.";
                    return RedirectToAction("TwoFactorPreferences");
                }
                else
                {
                    ViewBag.Error = "Invalid or expired verification code. Please try again.";
                    TempData.Keep();
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to verify phone number for {userEmail}");
                ViewBag.Error = "Failed to verify phone number. Please try again.";
                TempData.Keep();
                return View();
            }
        }

        #region Helper Methods

        private string DetermineUserRole(string email)
        {
            // Simple role determination logic based on email
            // In production, this would query your user database
            
            if (email.EndsWith("@greencirclecertified.com", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }
            else if (email.EndsWith("@contractor.com", StringComparison.OrdinalIgnoreCase) ||
                     email.EndsWith("@analyst.com", StringComparison.OrdinalIgnoreCase))
            {
                return "Analyst";
            }
            else if (email.Contains("partner", StringComparison.OrdinalIgnoreCase) ||
                     email.EndsWith("@amazon.com", StringComparison.OrdinalIgnoreCase))
            {
                return "External";
            }
            else
            {
                return "Public";
            }
        }

        private string ValidateUser(string email, string password)
        {
            var (role, _, _) = ValidateUserDetailed(email, password);
            return role;
        }

        // Returns: (Role, CanonicalEmail, Error). CanonicalEmail is the user's actual Email column
        // value — distinct from the form input (which can be a username). Callers MUST use the
        // canonical email for downstream claims/TempData/lookups; otherwise role-scoped queries
        // (e.g. external user → company → certificates) silently return zero rows when the user
        // logs in with a username instead of their full email. (DEF-113, 2026-04-26.)
        // DEF-210 lockout policy
        private const int MaxFailedLoginAttempts = 5;
        private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);

        // DEF-209: every failure path returns the same generic message. Diagnostics
        // (which step actually failed) stay in the server log for ops, but never
        // reach the rendered form. Eliminates email-enumeration via differential
        // error strings ("User not found" vs "Invalid password" vs "Account inactive").
        private const string GenericLoginError = "Invalid email or password.";

        private (string Role, string Email, string Error) ValidateUserDetailed(string email, string password)
        {
            try
            {
                // Query database for user by email OR username
                var user = _context.Users
                    .Include(u => u.Roles)
                    .FirstOrDefault(u => u.Email == email || u.Username == email);

                // User not found
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: User not found for email/username: {Email}", email);
                    return (string.Empty, string.Empty, GenericLoginError);
                }

                // DEF-210: account lockout gate — check BEFORE password verify so a
                // locked account can't be probed for password correctness. Failed
                // attempts during lockout window do NOT increment the counter
                // (otherwise an attacker who waits out the timer always finds the
                // counter at MaxFailedLoginAttempts and re-locks instantly on first
                // try). When the lockout naturally expires the counter implicitly
                // resets via the next successful login OR the user gets a clean 5
                // attempts; legitimate user gets a reasonable recovery path.
                if (user.IsLockedOut)
                {
                    _logger.LogWarning(
                        "Login attempt rejected: account {Email} is locked until {LockedUntil} (attempts={Attempts})",
                        user.Email, user.LockedUntil, user.FailedLoginAttempts);
                    return (string.Empty, string.Empty, GenericLoginError);
                }

                // Check if user is active.
                // DEF-326: the Status comparison was ordinal, so a row stored as "Active" (any
                // casing other than all-lower) locked the account out of login while the admin
                // screen showed it as Active — the app writes this column lowercase, but imports
                // and direct SQL do not have to.
                if (!user.IsActive || user.IsDeleted ||
                    !string.Equals(user.Status?.Trim(), "active", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Login attempt failed: User {Email} is not active (IsActive={IsActive}, IsDeleted={IsDeleted}, Status={Status})",
                        email, user.IsActive, user.IsDeleted, user.Status);
                    return (string.Empty, string.Empty, GenericLoginError);
                }

                // Verify password using BCrypt
                bool passwordValid;
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                catch (Exception bcryptEx)
                {
                    _logger.LogError(bcryptEx, "BCrypt verification threw exception for user {Email}, hash length={Len}",
                        email, user.PasswordHash?.Length);
                    return (string.Empty, string.Empty, GenericLoginError);
                }

                if (!passwordValid)
                {
                    // DEF-210: increment failure counter and lock if threshold reached.
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                    {
                        user.LockedUntil = DateTime.UtcNow.Add(LoginLockoutDuration);
                        _logger.LogWarning(
                            "Account {Email} locked until {Until} after {Count} consecutive failed login attempts",
                            user.Email, user.LockedUntil, user.FailedLoginAttempts);
                    }
                    user.UpdatedAt = DateTime.UtcNow;
                    _context.SaveChanges();

                    _logger.LogWarning(
                        "Login attempt failed: Invalid password for user: {Email} (attempts={Attempts}/{Max})",
                        email, user.FailedLoginAttempts, MaxFailedLoginAttempts);
                    return (string.Empty, string.Empty, GenericLoginError);
                }

                // Get user's primary role
                var role = user.Roles.FirstOrDefault()?.Name ?? "Public";

                _logger.LogInformation("User {Email} (input: {Input}) successfully authenticated with role {Role}", user.Email, email, role);

                // DEF-210: clear lockout state + update last-login timestamp.
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();

                return (role, user.Email, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {Email}", email);
                return (string.Empty, string.Empty, GenericLoginError);
            }
        }

        /// <summary>
        /// Check if user requires password reset on next login
        /// </summary>
        private bool CheckRequirePasswordReset(string email)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email || u.Username == email);
                return user?.RequirePasswordReset ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password reset requirement for {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Clear the password reset requirement for a user
        /// </summary>
        private bool ClearRequirePasswordReset(string email)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email || u.Username == email);
                if (user != null)
                {
                    user.RequirePasswordReset = false;
                    _context.SaveChanges();
                    _logger.LogInformation("Password reset requirement cleared for user {Email}", email);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing password reset requirement for {Email}", email);
                return false;
            }
        }

        private async Task SignInUser(string email, string role, bool rememberMe)
        {
            // Look up the actual DB user so claims carry the real Id and Username.
            // Audit fields (CreatedBy/UpdatedBy on Certificate, CertificateProductId, etc.)
            // are int FKs to Users.Id. Every controller's GetCurrentUserId() helper parses
            // ClaimTypes.NameIdentifier as int — if we stamp a Guid here, every parse fails
            // and audit data is silently lost. Use the user's Id from the DB.
            var user = _context.Users.FirstOrDefault(u => u.Email == email || u.Username == email);
            var nameIdentifier = user != null ? user.Id.ToString() : Guid.NewGuid().ToString();
            var displayName = user?.Username ?? email;

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role), // Standard role claim for [Authorize(Roles = "...")]
                new Claim("Email", email),
                new Claim("Name", displayName),
                new Claim("Role", role), // Custom role claim for policies
                new Claim("RememberMe", rememberMe ? "true" : "false"), // Read by client to skip idle auto-logout
                new Claim(ClaimTypes.NameIdentifier, nameIdentifier)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"User {email} signed in with role {role}");

            // Set session variables
            HttpContext.Session.SetString("UserEmail", email);
            HttpContext.Session.SetString("UserName", email);
            HttpContext.Session.SetString("UserRole", role);

            // Expire overdue certificates (throttled to once per 24 hours, safe to call every login)
            try
            {
                await _certificateExpirationService.CheckAndExpireAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Certificate expiration check failed during login");
            }
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Role-based landing pages for authenticated users
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole switch
            {
                "Public" => RedirectToAction("CertificateTileView", "Certificate"),
                "External" => RedirectToAction("CertificateTileViewExternal", "Certificate"),
                "Partner" => RedirectToAction("CertificateTileViewExternal", "Certificate"),
                "Analyst" => RedirectToAction("CertificateTileViewInternal", "Certificate"),
                "Admin" => RedirectToAction("CertificateTileViewInternal", "Certificate"),
                _ => RedirectToAction("CertificateTileView", "Certificate")
            };
        }

        #endregion

        #region TOTP Methods

    /// <summary>
    /// Setup TOTP (Time-based One-Time Password) for Microsoft Authenticator
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult SetupTotp()
    {
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login");
        }

        try
        {
            // Generate a random secret for TOTP without requiring database user
            var secret = GenerateBase32Secret();
            
            // Store in session temporarily
            HttpContext.Session.SetString($"TotpSecret_{userEmail}", secret);
            
            // Generate QR code URI
            var issuer = "GCC Database";
            var label = $"{issuer}:{userEmail}";
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
            
            ViewBag.TotpSecret = secret;
            ViewBag.QrCodeUri = qrCodeUri;
            ViewBag.UserEmail = userEmail;
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to setup TOTP for user {userEmail}");
            TempData["ErrorMessage"] = "Failed to setup authenticator. Please try again.";
            return RedirectToAction("TwoFactorPreferences");
        }
    }

    private string GenerateBase32Secret(int length = 32)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var random = new Random();
        var secret = new char[length];
        
        for (int i = 0; i < length; i++)
        {
            secret[i] = base32Chars[random.Next(base32Chars.Length)];
        }
        
        return new string(secret);
    }

    /// <summary>
    /// Generate QR code image for TOTP setup
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult GenerateQrCode()
    {
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return BadRequest("User not authenticated");
        }

        try
        {
            // Get the secret from session
            var secret = HttpContext.Session.GetString($"TotpSecret_{userEmail}");
            if (string.IsNullOrEmpty(secret))
            {
                // Generate new if not found
                secret = GenerateBase32Secret();
                HttpContext.Session.SetString($"TotpSecret_{userEmail}", secret);
            }
            
            // Generate QR code
            var issuer = "GCC Database";
            var label = $"{issuer}:{userEmail}";
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
            
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCoder.QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(10);
            
            return File(qrCodeImage, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to generate QR code for user {userEmail}");
            return BadRequest("Failed to generate QR code");
        }
    }

    /// <summary>
    /// Verify and enable TOTP
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTotp(string totpCode)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login");
        }

        if (string.IsNullOrEmpty(totpCode) || totpCode.Length != 6)
        {
            TempData["ErrorMessage"] = "Please enter a valid 6-digit code from your authenticator app.";
            return RedirectToAction("SetupTotp");
        }

        try
        {
            var success = await _twoFactorService.EnableTotpAsync(userEmail, totpCode);
            if (success)
            {
                // Generate recovery codes
                var recoveryCodes = await _twoFactorService.GenerateRecoveryCodesAsync(userEmail);
                TempData["RecoveryCodes"] = System.Text.Json.JsonSerializer.Serialize(recoveryCodes);

                // Audit logging
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user != null)
                {
                    await _auditService.LogAuditEventAsync(
                        AuditEntityType.User, user.Id.ToString(), user.Username,
                        AuditAction.TotpEnabled, user.Id, user.Username,
                        "User enabled TOTP authenticator", changeSource: AuditChangeSource.WebUI);
                    await _auditService.LogAuditEventAsync(
                        AuditEntityType.User, user.Id.ToString(), user.Username,
                        AuditAction.RecoveryCodesGenerated, user.Id, user.Username,
                        "Recovery codes generated during TOTP setup", changeSource: AuditChangeSource.WebUI);
                }

                return RedirectToAction("ShowRecoveryCodes");
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid code. Please check your authenticator app and try again.";
                return RedirectToAction("SetupTotp");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable TOTP for user {UserEmail}", userEmail);
            TempData["ErrorMessage"] = "Failed to enable authenticator. Please try again.";
            return RedirectToAction("SetupTotp");
        }
    }

    /// <summary>
    /// Disable TOTP for user
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTotp()
    {
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login");
        }

        try
        {
            var success = await _twoFactorService.DisableTotpAsync(userEmail);
            if (success)
            {
                TempData["SuccessMessage"] = "Microsoft Authenticator has been disabled for your account.";
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user != null)
                {
                    await _auditService.LogAuditEventAsync(
                        AuditEntityType.User, user.Id.ToString(), user.Username,
                        AuditAction.TotpDisabled, user.Id, user.Username,
                        "User disabled TOTP authenticator", changeSource: AuditChangeSource.WebUI);
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to disable authenticator.";
            }

            return RedirectToAction("TwoFactorPreferences");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable TOTP for user {UserEmail}", userEmail);
            TempData["ErrorMessage"] = "Failed to disable authenticator. Please try again.";
            return RedirectToAction("TwoFactorPreferences");
        }
    }

    /// <summary>
    /// Show recovery codes after TOTP setup (one-time display)
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult ShowRecoveryCodes()
    {
        var codesJson = TempData["RecoveryCodes"]?.ToString();
        if (string.IsNullOrEmpty(codesJson))
        {
            return RedirectToAction("TwoFactorPreferences");
        }
        var codes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(codesJson);
        ViewBag.RecoveryCodes = codes;
        return View();
    }

    /// <summary>
    /// Regenerate recovery codes for authenticated user
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateRecoveryCodes()
    {
        var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(userEmail))
            return RedirectToAction("Login");

        var codes = await _twoFactorService.GenerateRecoveryCodesAsync(userEmail);
        TempData["RecoveryCodes"] = System.Text.Json.JsonSerializer.Serialize(codes);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user != null)
        {
            await _auditService.LogAuditEventAsync(
                AuditEntityType.User, user.Id.ToString(), user.Username,
                AuditAction.RecoveryCodesGenerated, user.Id, user.Username,
                "Recovery codes regenerated by user", changeSource: AuditChangeSource.WebUI);
        }

        return RedirectToAction("ShowRecoveryCodes");
    }

    /// <summary>
    /// Validate TOTP code during login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateTotp(string totpCode)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(totpCode))
        {
            ViewBag.ErrorMessage = "Please enter a valid code from your authenticator app.";
            return View("TwoFactorAuthentication");
        }

        try
        {
            var isValid = await _twoFactorService.ValidateTotpCodeAsync(userEmail, totpCode);
            
            if (isValid)
            {
                // Complete login process
                HttpContext.Session.SetString("Is2FAVerified", "true");
                
                var returnUrl = HttpContext.Session.GetString("ReturnUrl");
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    HttpContext.Session.Remove("ReturnUrl");
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid authenticator code. Please try again.";
                return View("TwoFactorAuthentication");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"TOTP validation failed for user {userEmail}");
            ViewBag.ErrorMessage = "Authentication failed. Please try again.";
            return View("TwoFactorAuthentication");
        }
    }

    #endregion

    #region Diagnostic Endpoints

    /// <summary>
    /// DIAGNOSTIC: Test authentication flow - Development environment only
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> TestAuth(string username = "admin", string password = "TestPass123!")
    {
        // SECURITY: Only allow in Development environment
        var env = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (env == null || !env.IsDevelopment())
        {
            return NotFound(); // Hide endpoint in non-development environments
        }

        try
        {
            var result = new
            {
                TestingUser = username,
                Step1_UserQuery = "Checking...",
                Step2_UserFound = false,
                Step3_UserActive = false,
                Step4_PasswordVerified = false,
                Step5_RolesLoaded = false,
                Step6_RoleName = "",
                UserDetails = (object?)null,
                RoleDetails = (object?)null,
                ErrorMessage = ""
            };

            // Step 1: Query user
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == username || u.Username == username);

            if (user == null)
            {
                return Ok(new
                {
                    result.TestingUser,
                    Step1_UserQuery = "Complete",
                    Step2_UserFound = false,
                    ErrorMessage = $"User '{username}' not found in database"
                });
            }

            // Step 2: User found
            var userFound = true;

            // Step 3: Check if active
            // DEF-326: case-insensitive, matching the real login check above — this diagnostic
            // path must not report "active" differently from the gate it is meant to mirror.
            var isActive = user.IsActive && !user.IsDeleted &&
                string.Equals(user.Status?.Trim(), "active", StringComparison.OrdinalIgnoreCase);

            // Step 4: Verify password
            bool passwordVerified = false;
            try
            {
                passwordVerified = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    result.TestingUser,
                    Step1_UserQuery = "Complete",
                    Step2_UserFound = userFound,
                    Step3_UserActive = isActive,
                    Step4_PasswordVerified = false,
                    ErrorMessage = $"BCrypt verification failed: {ex.Message}"
                });
            }

            // Step 5: Check roles
            var rolesLoaded = user.Roles != null && user.Roles.Any();
            var roleName = user.Roles.FirstOrDefault()?.Name ?? "No role assigned";

            return Ok(new
            {
                result.TestingUser,
                Step1_UserQuery = "Complete",
                Step2_UserFound = userFound,
                Step3_UserActive = isActive,
                Step4_PasswordVerified = passwordVerified,
                Step5_RolesLoaded = rolesLoaded,
                Step6_RoleName = roleName,
                UserDetails = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.IsActive,
                    user.IsDeleted,
                    user.Status,
                    PasswordHashLength = user.PasswordHash?.Length ?? 0,
                    RoleCount = user.Roles?.Count ?? 0
                },
                RoleDetails = user.Roles?.Select(r => new { r.Id, r.Name }).ToList(),
                Success = userFound && isActive && passwordVerified && rolesLoaded
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                ErrorMessage = $"Exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }

    #endregion
}
}
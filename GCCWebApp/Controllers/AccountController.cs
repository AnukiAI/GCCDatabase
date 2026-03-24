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
            var userRole = ValidateUser(model.Email, model.Password);

            if (string.IsNullOrEmpty(userRole))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            // Check if user requires password reset
            if (CheckRequirePasswordReset(model.Email))
            {
                _logger.LogInformation("User {Email} requires password reset on login", model.Email);
                TempData["ForceResetEmail"] = model.Email;
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
                    var preferences = await _twoFactorService.GetUserPreferencesAsync(model.Email);
                    
                    // Generate and send 2FA code using preferred method
                    var code = _twoFactorService.GenerateCode();
                    _twoFactorService.StoreCode(model.Email, code);
                    await _twoFactorService.SendCodeAsync(model.Email, code, preferences.PreferredMethod, preferences.Email, preferences.PhoneNumber);
                    
                    // Store user info temporarily in TempData
                    TempData["2FAEmail"] = model.Email;
                    TempData["2FARole"] = userRole;
                    TempData["ReturnUrl"] = returnUrl;
                    TempData["RememberMe"] = model.RememberMe;
                    TempData["2FAMethod"] = preferences.PreferredMethod.ToString();
                    
                    _logger.LogInformation($"2FA code sent to {model.Email} via {preferences.PreferredMethod}");
                    return RedirectToAction("TwoFactorAuthentication");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send 2FA code to {model.Email}");
                    ModelState.AddModelError("", "Unable to send verification code. Please try again or contact support.");
                    return View(model);
                }
            }

            // Direct login without 2FA
            await SignInUser(model.Email, userRole, model.RememberMe);
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
        /// Handle forgot password form submission (fallback)
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

            // In a real application, you would send a password reset email
            _logger.LogInformation($"Password reset requested for {model.Email}");
            
            // For demo purposes, just show a success message
            ViewBag.Message = "If an account with that email address exists, you will receive a password reset link.";
            
            // Add await for async completion
            await Task.CompletedTask;
            
            return View("ForgotPasswordConfirmation");
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

                // Hash and update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.RequirePasswordReset = false;
                _context.SaveChanges();

                _logger.LogInformation("Password successfully reset for user {Email}", email);

                // Check if 2FA is required after password reset
                var is2FAEnabled = _configuration.GetValue<bool>("Authentication:Enable2FA", true);

                if (is2FAEnabled)
                {
                    // Proceed with 2FA flow
                    var preferences = await _twoFactorService.GetUserPreferencesAsync(email);
                    var code = _twoFactorService.GenerateCode();
                    _twoFactorService.StoreCode(email, code);
                    await _twoFactorService.SendCodeAsync(email, code, preferences.PreferredMethod, preferences.Email, preferences.PhoneNumber);

                    TempData["2FAEmail"] = email;
                    TempData["2FARole"] = role;
                    TempData["ReturnUrl"] = returnUrl;
                    TempData["RememberMe"] = rememberMe;
                    TempData["2FAMethod"] = preferences.PreferredMethod.ToString();

                    return RedirectToAction("TwoFactorAuthentication");
                }

                // Sign in directly without 2FA
                await SignInUser(email, role, rememberMe);
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
                    return string.Empty;
                }

                // Check if user is active
                if (!user.IsActive || user.IsDeleted || user.Status != "active")
                {
                    _logger.LogWarning("Login attempt failed: User {Email} is not active (IsActive={IsActive}, IsDeleted={IsDeleted}, Status={Status})",
                        email, user.IsActive, user.IsDeleted, user.Status);
                    return string.Empty;
                }

                // Verify password using BCrypt
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.LogWarning("Login attempt failed: Invalid password for user: {Email}", email);
                    return string.Empty;
                }

                // Get user's primary role
                var role = user.Roles.FirstOrDefault()?.Name ?? "Public";

                _logger.LogInformation("User {Email} successfully authenticated with role {Role}", email, role);

                // Update last login timestamp
                user.LastLoginAt = DateTime.UtcNow;
                _context.SaveChanges();

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {Email}", email);
                return string.Empty;
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
            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role), // Standard role claim for [Authorize(Roles = "...")]
                new Claim("Email", email),
                new Claim("Name", email), // Using email as name for simplicity
                new Claim("Role", role), // Custom role claim for policies
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
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
                "Analyst" => RedirectToAction("Index", "Dashboard"),
                "Admin" => RedirectToAction("Index", "Dashboard"),
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
            var isActive = user.IsActive && !user.IsDeleted && user.Status == "active";

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
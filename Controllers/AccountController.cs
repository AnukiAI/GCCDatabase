using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using GCCWebApp.Configuration;
using GCCWebApp.Services;

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

        public AccountController(
            ILogger<AccountController> logger, 
            IOptions<AzureAdB2COptions>? azureAdB2COptions, 
            IConfiguration configuration,
            IEnhancedTwoFactorService twoFactorService,
            IEmailService emailService)
        {
            _logger = logger;
            _azureAdB2COptions = azureAdB2COptions?.Value;
            _configuration = configuration;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
            
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
                    default:
                        ViewBag.Error = "Authentication failed. Please try again.";
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
        public async Task<IActionResult> Logout()
        {
            return await PerformLogout();
        }

        /// <summary>
        /// Initiate sign-out process (POST version for form submission)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutPost()
        {
            return await PerformLogout();
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
            
            if (string.IsNullOrWhiteSpace(authCode) || authCode.Length != 6)
            {
                ViewBag.Error = "Please enter a valid 6-digit code.";
                TempData.Keep(); // Keep for retry
                return View("2FAAuthenticate");
            }
            
            if (_twoFactorService.ValidateCode(email, authCode))
            {
                _logger.LogInformation($"2FA verification successful for {email}");
                await SignInUser(email, role, rememberMe || trustDevice);
                return RedirectToLocal(returnUrl);
            }
            
            _logger.LogWarning($"2FA verification failed for {email}");
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
            // Check if test mode is enabled
            var isTestMode = _configuration.GetValue<bool>("Authentication:EnableTestMode");
            
            if (isTestMode)
            {
                // Load test users from configuration
                var testUsersSection = _configuration.GetSection("Authentication:TestUsers");
                foreach (var userSection in testUsersSection.GetChildren())
                {
                    var testEmail = userSection["Email"];
                    var testPassword = userSection["Password"];
                    var testRole = userSection["Role"];
                    
                    if (testEmail?.Equals(email, StringComparison.OrdinalIgnoreCase) == true && testPassword == password)
                    {
                        _logger.LogInformation("Test user {Email} authenticated with role {Role}", email, testRole);
                        return testRole ?? "Public";
                    }
                }
            }
            
            // Fallback to hardcoded test users for backward compatibility
            var testUsers = new Dictionary<string, (string password, string role)>
            {
                { "admin@greencirclecertified.com", ("admin123", "Admin") },
                { "analyst@greencirclecertified.com", ("analyst123", "Analyst") },
                { "partner@amazon.com", ("partner123", "External") },
                { "user@test.com", ("user123", "Public") }
            };

            if (testUsers.TryGetValue(email.ToLower(), out var userData) && userData.password == password)
            {
                return userData.role;
            }

            return string.Empty; // Invalid credentials
        }

        private async Task SignInUser(string email, string role, bool rememberMe)
        {
            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("Email", email),
                new Claim("Name", email), // Using email as name for simplicity
                new Claim("Role", role),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
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
    }

    #region View Models

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
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
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}
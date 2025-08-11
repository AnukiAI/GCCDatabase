using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using GCCWebApp.Configuration;

namespace GCCWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly AzureAdB2COptions? _azureAdB2COptions;
        private readonly bool _isAzureAdB2CConfigured;

        public AccountController(ILogger<AccountController> logger, IOptions<AzureAdB2COptions>? azureAdB2COptions)
        {
            _logger = logger;
            _azureAdB2COptions = azureAdB2COptions?.Value;
            
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

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Email),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim("Email", model.Email),
                new Claim("Name", model.Email), // Using email as name for simplicity
                new Claim("Role", userRole),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"User {model.Email} signed in with role {userRole}");

            // Set session variables
            HttpContext.Session.SetString("UserEmail", model.Email);
            HttpContext.Session.SetString("UserName", model.Email);
            HttpContext.Session.SetString("UserRole", userRole);

            return RedirectToLocal(returnUrl);
        }

        /// <summary>
        /// Initiate sign-out process
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            // Clear session data
            HttpContext.Session.Clear();

            if (_isAzureAdB2CConfigured)
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
                // Sign out from cookie authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation($"User {userEmail} signed out");
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// Handle post-signout redirect
        /// </summary>
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return RedirectToAction(nameof(Login));
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
            return View("ForgotPasswordConfirmation");
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
            // Simple validation for testing - in production, validate against your user store
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
                "External" => RedirectToAction("Index", "Dashboard"),
                "Analyst" or "Admin" => RedirectToAction("Index", "Dashboard"),
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
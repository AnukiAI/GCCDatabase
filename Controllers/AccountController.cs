using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace YourProjectName.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Display the login page
        /// </summary>
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null, string error = null)
        {
            // If user is already authenticated, redirect to dashboard
            if (User.Identity.IsAuthenticated)
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
        /// Handle login form submission (for testing)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Simple authentication logic for testing
            // In production, this would validate against your user store
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

            // Redirect to intended page or dashboard
            return RedirectToLocal(returnUrl);
        }

        /// <summary>
        /// Handle user sign out
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            // Clear session data
            HttpContext.Session.Clear();

            // Sign out from cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation($"User {userEmail} signed out");

            return RedirectToAction("Login");
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
        /// Admin-only user creation page (for testing)
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateUserViewModel());
        }

        /// <summary>
        /// Create a new user (Admin only) - simplified for testing
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // In a real application, you would save this to a database
                _logger.LogInformation($"User {model.Email} would be created with role {model.Role}");

                TempData["Success"] = $"User {model.Email} created successfully with role {model.Role}.";
                return RedirectToAction("UserManagement", "Admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user {model.Email}");
                ModelState.AddModelError("", "An error occurred while creating the user.");
                return View(model);
            }
        }

        /// <summary>
        /// Quick login buttons for testing different roles
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> QuickLogin(string role)
        {
            var email = $"{role.ToLower()}@test.com";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("Email", email),
                new Claim("Name", $"Test {role}"),
                new Claim("Role", role),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"Quick login as {role} - {email}");

            return RedirectToAction("Dashboard", "Dashboard");
        }

        #region Private Helper Methods

        /// <summary>
        /// Simple user validation for testing
        /// Returns the user's role if valid, null if invalid
        /// </summary>
        private string ValidateUser(string email, string password)
        {
            // Simple hardcoded users for testing
            // In production, this would check against your user store/database
            var testUsers = new Dictionary<string, string>
            {
                { "admin@test.com", "Admin" },
                { "analyst@test.com", "Analyst" },
                { "external@test.com", "External" },
                { "user@test.com", "External" }
            };

            // For testing, accept any password that's not empty
            if (string.IsNullOrEmpty(password))
                return null;

            return testUsers.TryGetValue(email.ToLower(), out var role) ? role : null;
        }

        /// <summary>
        /// Safely redirect to local URL
        /// </summary>
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }

    #region View Models

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; } = false;
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display name is required")]
        [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "User Role")]
        public string Role { get; set; } = "External";

        [Display(Name = "Send Welcome Email")]
        public bool SendWelcomeEmail { get; set; } = true;
    }

    #endregion
}
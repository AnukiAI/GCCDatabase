# GCC Database Application - Security Guide

## Table of Contents
1. [Overview](#overview)
2. [Security Architecture](#security-architecture)
3. [Authentication and Authorization](#authentication-and-authorization)
4. [Data Protection](#data-protection)
5. [Input Validation](#input-validation)
6. [Session Management](#session-management)
7. [API Security](#api-security)
8. [Database Security](#database-security)
9. [Infrastructure Security](#infrastructure-security)
10. [Security Monitoring](#security-monitoring)
11. [Compliance](#compliance)
12. [Security Testing](#security-testing)
13. [Incident Response](#incident-response)
14. [Security Checklist](#security-checklist)

## Overview

This guide outlines comprehensive security measures implemented in the GCC Database Web Application to protect against various threats and ensure data confidentiality, integrity, and availability.

### Security Principles
- **Defense in Depth**: Multiple layers of security controls
- **Principle of Least Privilege**: Minimal access rights
- **Zero Trust**: Verify everything, trust nothing
- **Secure by Default**: Security-first design approach
- **Continuous Monitoring**: Real-time threat detection

### Threat Model ✅ **UPDATED**
| Threat | Impact | Likelihood | Mitigation |
|---------|--------|------------|------------|
| SQL Injection | High | Medium | Parameterized queries, ORM, ValidationService |
| XSS Attacks | High | Medium | Input sanitization, CSP, Input validation |
| CSRF Attacks | Medium | Medium | Anti-forgery tokens, API authentication |
| Data Breaches | High | Low | Encryption, access controls, validation |
| Authentication Bypass | High | Low | MFA, strong authentication, JWT tokens |
| Privilege Escalation | High | Low | Role-based access control, API authorization |
| File Upload Attacks | High | Medium | **NEW**: File type validation, size limits, virus scanning |
| Path Traversal | Medium | Low | **NEW**: Secure file naming, path validation |
| Business Logic Bypass | Medium | Medium | **NEW**: Comprehensive validation service |

## Security Architecture

### Security Layers
```
┌─────────────────────────────────────────┐
│              End Users                   │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│          Web Application Firewall       │
│          (Azure Front Door)             │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│           Load Balancer                 │
│         (SSL Termination)               │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│        Application Layer                │
│    (Authentication & Authorization)     │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│          Database Layer                 │
│      (Encryption & Access Control)     │
└─────────────────────────────────────────┘
```

### Security Components
```csharp
// Security middleware pipeline configuration
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts(); // HTTP Strict Transport Security
    }

    app.UseHttpsRedirection(); // Force HTTPS
    app.UseSecurityHeaders(); // Custom security headers
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgeryToken(); // CSRF protection
    
    app.UseRouting();
    app.UseEndpoints(endpoints => { /* ... */ });
}

// Security headers middleware
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Security headers
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");
        
        await next(context);
    }
}
```

## Authentication and Authorization

The application implements **dual-mode authentication** with automatic detection and fallback:

### Development Authentication (Cookie-based)
```csharp
// Automatic fallback to cookie authentication in development
// Located in Program.cs - auto-detection based on Azure AD B2C configuration

var azureAdB2CConfig = builder.Configuration.GetSection("AzureAdB2C").Get<AzureAdB2COptions>();
var isAzureAdB2CConfigured = azureAdB2CConfig != null && 
    !string.IsNullOrEmpty(azureAdB2CConfig.ClientId) && 
    !azureAdB2CConfig.ClientId.Contains("{");

if (isAzureAdB2CConfigured)
{
    // Production: Azure AD B2C Authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options);
            options.Authority = azureAdB2CConfig!.SignUpSignInAuthority;
        });
}
else
{
    // Development: Cookie Authentication
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        });
}
```

### Role-Based Authorization Policies
```csharp
// Authorization policies supporting both authentication modes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("Role", "Admin"));

    options.AddPolicy("AnalystOrAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.HasClaim("Role", "Admin") ||
                  context.User.HasClaim("Role", "Analyst")));

    options.AddPolicy("ExternalAccess", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.HasClaim("Role", "External") ||
                  context.User.HasClaim("Role", "Admin") ||
                  context.User.HasClaim("Role", "Analyst")));
});
```

### Dynamic Role Assignment
The application includes intelligent role assignment based on email domains:
```csharp
// Role determination logic in AccountController
private string DetermineUserRole(string email)
{
    if (email.EndsWith("@greencirclecertified.com", StringComparison.OrdinalIgnoreCase))
        return "Admin";
    else if (email.EndsWith("@contractor.com", StringComparison.OrdinalIgnoreCase) ||
             email.EndsWith("@analyst.com", StringComparison.OrdinalIgnoreCase))
        return "Analyst";
    else if (email.Contains("partner", StringComparison.OrdinalIgnoreCase) ||
             email.EndsWith("@amazon.com", StringComparison.OrdinalIgnoreCase))
        return "External";
    else
        return "Public";
}
```

### Role-Based Access Control (RBAC)
```csharp
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var userRoles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (userRoles.Any(role => requirement.AllowedRoles.Contains(role)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    [Authorize(Policy = "AnalystOrAdmin")]
    public async Task<IActionResult> ViewCertificates()
    {
        // Only Analysts and Administrators can access
        return View();
    }
}
```

### Multi-Factor Authentication (MFA)
```csharp
public class MfaRequiredAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var mfaClaim = user.FindFirst("mfa_verified");
        if (mfaClaim == null || !bool.Parse(mfaClaim.Value))
        {
            // Redirect to MFA challenge
            context.Result = new RedirectToActionResult(
                "Challenge", "Mfa", null);
        }
    }
}
```

## Data Protection

### Encryption at Rest
```csharp
// Database encryption configuration
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure sensitive fields for encryption
        modelBuilder.Entity<User>()
            .Property(u => u.PersonalData)
            .HasConversion(
                v => EncryptionHelper.Encrypt(v),
                v => EncryptionHelper.Decrypt(v));
    }
}

public static class EncryptionHelper
{
    private static readonly byte[] Key = Convert.FromBase64String(
        Environment.GetEnvironmentVariable("ENCRYPTION_KEY"));

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var writer = new StreamWriter(cs);

        writer.Write(plainText);
        writer.Close();

        var encrypted = ms.ToArray();
        var result = new byte[aes.IV.Length + encrypted.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        var buffer = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Key;

        var iv = new byte[16];
        var encrypted = new byte[buffer.Length - 16];
        Array.Copy(buffer, 0, iv, 0, 16);
        Array.Copy(buffer, 16, encrypted, 0, encrypted.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encrypted);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }
}
```

### Encryption in Transit
```csharp
// HTTPS configuration
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    });

    services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });

    // Configure TLS
    services.Configure<KestrelServerOptions>(options =>
    {
        options.ConfigureHttpsDefaults(https =>
        {
            https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
        });
    });
}
```

### Password Security
```csharp
public class PasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        var hashBytes = new byte[SaltSize + KeySize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(key, 0, hashBytes, SaltSize, KeySize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashBytes = Convert.FromBase64String(hash);

        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        for (int i = 0; i < KeySize; i++)
        {
            if (hashBytes[i + SaltSize] != key[i])
                return false;
        }

        return true;
    }

    public bool IsPasswordStrong(string password)
    {
        return password.Length >= 12 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
    }
}
```

## Input Validation ✅ **ENHANCED**

### Enhanced Validation Service ✅ **NEW**
The application now includes a comprehensive validation service that implements business logic validation beyond basic data annotations:

```csharp
namespace GCCWebApp.Services
{
    public class ValidationService : IValidationService
    {
        public async Task<ValidationResult> ValidateCertificateAsync(Certificate certificate)
        {
            var result = new ValidationResult();
            
            // Certificate number uniqueness validation
            if (await _context.Certificates.AnyAsync(c => 
                c.Number == certificate.Number && c.Id != certificate.Id))
            {
                result.AddError($"Certificate number '{certificate.Number}' is already in use.");
            }
            
            // Business rule: Certificate validity period cannot exceed 5 years
            if (certificate.ExpirationDate > certificate.EffectiveDate.AddYears(5))
            {
                result.AddError("Certificate validity period cannot exceed 5 years.");
            }
            
            // Product code format validation (ASIN, UPC, GTIN, EAN)
            if (!string.IsNullOrEmpty(certificate.Upc) && !IsValidUpc(certificate.Upc))
            {
                result.AddError("UPC format is invalid. Must be 12 digits.");
            }
            
            // Entity relationship validation
            if (!await _context.Companies.AnyAsync(c => c.Id == certificate.CompanyId))
            {
                result.AddError("Selected company does not exist.");
            }
            
            return result;
        }

        private bool IsValidUpc(string upc) =>
            !string.IsNullOrEmpty(upc) && upc.Length == 12 && upc.All(char.IsDigit);
    }
}
```

### Data Annotation Validation
```csharp
public class CertificateCreateModel
{
    [Required(ErrorMessage = "Certificate number is required")]
    [StringLength(50, MinimumLength = 5)]
    [RegularExpression(@"^[A-Z0-9\-]+$", 
        ErrorMessage = "Certificate number can only contain uppercase letters, numbers, and hyphens")]
    public string Number { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int CompanyId { get; set; }

    [DataType(DataType.Date)]
    [FutureDate(ErrorMessage = "Effective date must be in the future")]
    public DateTime EffectiveDate { get; set; }
}

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value is DateTime date)
        {
            return date > DateTime.Now;
        }
        return false;
    }
}
```

### File Upload Security ✅ **NEW**
A comprehensive file upload security system has been implemented to prevent various file-based attacks:

```csharp
namespace GCCWebApp.Services
{
    public class FileUploadService : IFileUploadService
    {
        public async Task<FileUploadResult> UploadCertificateAsync(IFormFile file, string certificateNumber)
        {
            var result = new FileUploadResult();

            // 1. File type validation - Both extension and MIME type
            if (!IsValidFileType(file, FileType.Certificate))
            {
                result.AddError("Invalid file type. Only PDF files are allowed for certificates.");
                return result;
            }

            // 2. File size validation (20MB limit for certificates)
            if (!IsValidFileSize(file, 20971520))
            {
                result.AddError("File size exceeds the maximum limit of 20MB.");
                return result;
            }

            // 3. Secure filename generation (prevents path traversal)
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{certificateNumber}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{fileExtension}";
            var filePath = Path.Combine(_certificatesPath, fileName);

            // 4. Double-check file doesn't exist (collision prevention)
            if (File.Exists(filePath))
            {
                fileName = $"{certificateNumber}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}[0..8]{fileExtension}";
                filePath = Path.Combine(_certificatesPath, fileName);
            }

            // 5. Save file with restricted permissions
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResult { IsSuccess = true, FilePath = filePath };
        }

        private bool IsValidFileType(IFormFile file, FileType fileType)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var contentType = file.ContentType.ToLower();

            return fileType switch
            {
                FileType.Certificate => IsValidCertificateFile(fileExtension, contentType),
                FileType.Image => IsValidImageFile(fileExtension, contentType),
                _ => false
            };
        }

        private bool IsValidCertificateFile(string extension, string contentType)
        {
            var validExtensions = new[] { ".pdf" };
            var validContentTypes = new[] { "application/pdf" };
            
            return validExtensions.Contains(extension) && validContentTypes.Contains(contentType);
        }
    }
}
```

**File Upload Security Features:**
- **Double Validation**: Both file extension and MIME type verification
- **Size Limits**: Configurable per file type (20MB certificates, 5MB images)  
- **Path Traversal Prevention**: Secure filename generation with timestamps
- **Storage Isolation**: Separate directories for different file types
- **Collision Prevention**: Automatic unique naming for duplicate uploads
- **Permission Control**: Files saved with minimal required permissions

### SQL Injection Prevention
```csharp
public class CertificateRepository
{
    public async Task<IEnumerable<Certificate>> SearchAsync(string searchTerm)
    {
        // Use parameterized queries to prevent SQL injection
        const string sql = @"
            SELECT * FROM Certificates 
            WHERE CertificateNumber LIKE @SearchTerm 
               OR CompanyName LIKE @SearchTerm";

        return await _context.Database
            .SqlQueryRaw<Certificate>(sql, 
                new SqlParameter("@SearchTerm", $"%{searchTerm}%"))
            .ToListAsync();
    }

    // Entity Framework automatically parameterizes LINQ queries
    public async Task<Certificate> GetByNumberAsync(string number)
    {
        return await _context.Certificates
            .FirstOrDefaultAsync(c => c.Number == number);
    }
}
```

### XSS Prevention
```csharp
public class XssProtectionService
{
    private static readonly Regex HtmlTagRegex = 
        new(@"<[^>]+>", RegexOptions.Compiled);

    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove HTML tags
        var sanitized = HtmlTagRegex.Replace(input, string.Empty);
        
        // HTML encode
        return WebUtility.HtmlEncode(sanitized);
    }

    public string SanitizeForJavaScript(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

// Usage in controllers
[HttpPost]
public async Task<IActionResult> CreateCertificate(CertificateCreateModel model)
{
    // Input is automatically validated by model binding
    if (!ModelState.IsValid)
        return View(model);

    // Additional sanitization for display fields
    model.Description = _xssProtection.SanitizeHtml(model.Description);
    
    await _certificateService.CreateAsync(model);
    return RedirectToAction("Index");
}
```

## Session Management

### Secure Session Configuration
The application implements comprehensive session security:

```csharp
// Session configuration in Program.cs
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "GCC.Session";
});

// Authentication cookie configuration (for cookie auth mode)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "GCC.Authentication";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
```

### Session State Management
```csharp
// Secure session handling in AccountController
[HttpPost]
public async Task<IActionResult> Login(LoginModel model)
{
    // After successful authentication, set secure session data
    HttpContext.Session.SetString("UserEmail", user.Email);
    HttpContext.Session.SetString("UserName", user.Name);
    HttpContext.Session.SetString("UserRole", userRole);
    HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString());
    
    // Create authentication claims
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("Role", userRole),
        new Claim("Name", user.Name)
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
}
```

### Session Timeout Handling
```javascript
// Client-side session timeout warning
class SessionManager {
    constructor() {
        this.warningTime = 25 * 60 * 1000; // 25 minutes
        this.logoutTime = 30 * 60 * 1000; // 30 minutes
        this.warningTimer = null;
        this.logoutTimer = null;
        this.init();
    }

    init() {
        this.resetTimers();
        document.addEventListener('click', () => this.resetTimers());
        document.addEventListener('keypress', () => this.resetTimers());
    }

    resetTimers() {
        clearTimeout(this.warningTimer);
        clearTimeout(this.logoutTimer);

        this.warningTimer = setTimeout(() => {
            this.showWarning();
        }, this.warningTime);

        this.logoutTimer = setTimeout(() => {
            this.logout();
        }, this.logoutTime);
    }

    showWarning() {
        if (confirm('Your session will expire in 5 minutes. Click OK to extend.')) {
            // Make a request to extend session
            fetch('/api/session/extend', { method: 'POST' })
                .then(() => this.resetTimers());
        }
    }

    logout() {
        window.location.href = '/Account/Logout';
    }
}

new SessionManager();
```

## API Security

### API Authentication
```csharp
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [RequirePermission("certificates.read")]
    public async Task<ActionResult<IEnumerable<Certificate>>> GetCertificates(
        [FromQuery] CertificateSearchModel search)
    {
        // Rate limiting check
        if (!await _rateLimiter.TryAcquireAsync(HttpContext.User.Identity.Name))
        {
            return StatusCode(429, "Rate limit exceeded");
        }

        var certificates = await _certificateService.SearchAsync(search);
        return Ok(certificates);
    }
}

public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        
        if (!user.HasClaim("permissions", _permission))
        {
            context.Result = new ForbidResult();
        }
    }
}
```

### API Rate Limiting
```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var key = GetClientKey(context);
        var limit = GetRateLimit(context);
        
        if (!IsRequestAllowed(key, limit))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }

    private bool IsRequestAllowed(string key, RateLimit limit)
    {
        var requests = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();
        var window = DateTime.UtcNow.AddMinutes(-limit.WindowMinutes);
        
        requests.RemoveAll(r => r < window);
        
        if (requests.Count >= limit.MaxRequests)
            return false;

        requests.Add(DateTime.UtcNow);
        _cache.Set(key, requests, TimeSpan.FromMinutes(limit.WindowMinutes));
        
        return true;
    }
}
```

## Database Security

### Connection Security
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<AppDbContext>(options =>
    {
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        
        // Use connection string with integrated security or encrypted passwords
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    });

    // Configure connection pooling
    services.AddDbContextPool<AppDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    }, poolSize: 128);
}
```

### Row-Level Security
```sql
-- Create security function
CREATE FUNCTION security.fn_UserAccessPredicate(@UserId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS AccessResult
WHERE 
    @UserId = CAST(SESSION_CONTEXT(N'UserId') AS INT)
    OR IS_ROLEMEMBER('db_owner') = 1
    OR IS_ROLEMEMBER('Admin') = 1;

-- Apply security policy
CREATE SECURITY POLICY security.UserDataFilter
ADD FILTER PREDICATE security.fn_UserAccessPredicate(UserId)
ON dbo.UserData,
ADD BLOCK PREDICATE security.fn_UserAccessPredicate(UserId)
ON dbo.UserData AFTER INSERT
WITH (STATE = ON);
```

## Infrastructure Security

### Network Security
```yaml
# Azure Network Security Group rules
apiVersion: network.azure.com/v1
kind: NetworkSecurityGroup
metadata:
  name: gcc-web-nsg
rules:
  - name: AllowHTTPS
    protocol: TCP
    sourcePortRange: "*"
    destinationPortRange: "443"
    access: Allow
    priority: 1000
    direction: Inbound
    
  - name: DenyHTTP
    protocol: TCP
    sourcePortRange: "*"
    destinationPortRange: "80"
    access: Deny
    priority: 1100
    direction: Inbound
    
  - name: AllowSSH
    protocol: TCP
    sourceAddressPrefix: "10.0.0.0/8"
    destinationPortRange: "22"
    access: Allow
    priority: 1200
    direction: Inbound
```

### Firewall Configuration
```bash
# Azure Application Gateway Web Application Firewall
az network application-gateway waf-config set \
  --resource-group gcc-prod-rg \
  --gateway-name gcc-app-gateway \
  --enabled true \
  --firewall-mode Prevention \
  --rule-set-type OWASP \
  --rule-set-version 3.2

# Custom WAF rules
az network application-gateway waf-rule create \
  --resource-group gcc-prod-rg \
  --gateway-name gcc-app-gateway \
  --name "BlockSQLInjection" \
  --priority 100 \
  --action Block \
  --rule-type MatchRule \
  --conditions @sql-injection-rules.json
```

## Security Monitoring

### Security Event Logging
```csharp
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public void LogAuthenticationFailure(string username, string ipAddress, string reason)
    {
        _logger.LogWarning("Authentication failure for user {Username} from IP {IpAddress}: {Reason}",
            username, ipAddress, reason);
    }

    public void LogAuthorizationFailure(string username, string resource, string action)
    {
        _logger.LogWarning("Authorization failure for user {Username} accessing {Resource} with action {Action}",
            username, resource, action);
    }

    public void LogDataAccess(string username, string resource, string operation)
    {
        _logger.LogInformation("Data access by user {Username} on {Resource} with operation {Operation}",
            username, resource, operation);
    }

    public void LogSecurityThreat(string threatType, string details, string ipAddress)
    {
        _logger.LogError("Security threat detected: {ThreatType} from IP {IpAddress}. Details: {Details}",
            threatType, ipAddress, details);
    }
}
```

### Intrusion Detection
```csharp
public class IntrusionDetectionService
{
    public async Task DetectSuspiciousActivity(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var userAgent = context.Request.Headers["User-Agent"];
        
        // Check for suspicious patterns
        if (IsSuspiciousUserAgent(userAgent))
        {
            await RecordThreat(ipAddress, "Suspicious User Agent", userAgent);
        }

        if (await IsRapidRequests(ipAddress))
        {
            await RecordThreat(ipAddress, "Rapid Requests", "Too many requests in short time");
        }

        if (ContainsSqlInjectionPatterns(context.Request.QueryString.Value))
        {
            await RecordThreat(ipAddress, "SQL Injection Attempt", 
                context.Request.QueryString.Value);
        }
    }

    private bool IsSuspiciousUserAgent(string userAgent)
    {
        var suspiciousPatterns = new[]
        {
            "sqlmap", "nikto", "nmap", "masscan", "zap", "burp"
        };

        return suspiciousPatterns.Any(pattern => 
            userAgent?.ToLower().Contains(pattern) == true);
    }
}
```

## Compliance

### GDPR Compliance
```csharp
public class GdprService
{
    public async Task<byte[]> ExportUserData(int userId)
    {
        var userData = await GetUserData(userId);
        var export = new
        {
            PersonalInfo = userData.PersonalInfo,
            Certificates = userData.Certificates,
            ActivityLog = userData.ActivityLog.Where(a => 
                a.Timestamp > DateTime.UtcNow.AddYears(-2))
        };

        return JsonSerializer.SerializeToUtf8Bytes(export, 
            new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task DeleteUserData(int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Anonymize rather than delete for audit trail
            var user = await _context.Users.FindAsync(userId);
            user.Email = $"deleted-user-{userId}@anonymous.com";
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.IsDeleted = true;
            user.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### SOX Compliance
```csharp
public class AuditTrailService
{
    public async Task LogDataChange(string tableName, int recordId, 
        string oldValues, string newValues, string changedBy)
    {
        var auditLog = new AuditLog
        {
            TableName = tableName,
            RecordId = recordId,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedBy = changedBy,
            ChangeDate = DateTime.UtcNow,
            ChangeType = DetermineChangeType(oldValues, newValues)
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditTrail(
        string tableName, int recordId)
    {
        return await _context.AuditLogs
            .Where(a => a.TableName == tableName && a.RecordId == recordId)
            .OrderByDescending(a => a.ChangeDate)
            .ToListAsync();
    }
}
```

## Security Testing

### Automated Security Testing
```csharp
[TestClass]
public class SecurityTests
{
    [TestMethod]
    public async Task Api_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = new HttpClient();
        var response = await client.GetAsync("https://localhost:5001/api/certificates");
        
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Login_WithSqlInjection_DoesNotAuthenticate()
    {
        var client = new HttpClient();
        var payload = new
        {
            Username = "admin'; DROP TABLE Users; --",
            Password = "password"
        };

        var response = await client.PostAsJsonAsync(
            "https://localhost:5001/api/auth/login", payload);
        
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public void PasswordHashing_ProducesSecureHash()
    {
        var service = new PasswordService();
        var password = "TestPassword123!";
        
        var hash1 = service.HashPassword(password);
        var hash2 = service.HashPassword(password);
        
        // Hashes should be different due to salt
        Assert.AreNotEqual(hash1, hash2);
        
        // Both should verify correctly
        Assert.IsTrue(service.VerifyPassword(password, hash1));
        Assert.IsTrue(service.VerifyPassword(password, hash2));
    }
}
```

### Penetration Testing Checklist
- [ ] SQL injection testing
- [ ] Cross-site scripting (XSS) testing  
- [ ] Cross-site request forgery (CSRF) testing
- [ ] Authentication bypass attempts
- [ ] Authorization testing
- [ ] Session management testing
- [ ] Input validation testing
- [ ] Error handling testing
- [ ] File upload security testing
- [ ] API security testing

## Incident Response

### Security Incident Response Plan
1. **Detection**: Automated monitoring alerts
2. **Containment**: Isolate affected systems
3. **Eradication**: Remove threat and vulnerabilities
4. **Recovery**: Restore systems to normal operation
5. **Lessons Learned**: Post-incident review

### Incident Response Procedures
```csharp
public class IncidentResponseService
{
    public async Task HandleSecurityIncident(SecurityIncident incident)
    {
        // Log the incident
        _logger.LogCritical("Security incident detected: {Type} - {Description}",
            incident.Type, incident.Description);

        // Notify security team
        await _notificationService.NotifySecurityTeam(incident);

        // Take immediate action based on severity
        switch (incident.Severity)
        {
            case Severity.Critical:
                await DisableAffectedAccounts(incident.AffectedUsers);
                await BlockSuspiciousIpAddresses(incident.SourceIpAddresses);
                break;
                
            case Severity.High:
                await RequirePasswordReset(incident.AffectedUsers);
                await IncreasedMonitoring(incident.AffectedSystems);
                break;
        }

        // Create incident ticket
        await _ticketingService.CreateIncidentTicket(incident);
    }

    private async Task DisableAffectedAccounts(IEnumerable<string> usernames)
    {
        foreach (var username in usernames)
        {
            var user = await _userService.GetByUsernameAsync(username);
            if (user != null)
            {
                user.IsActive = false;
                user.LockoutEnd = DateTime.UtcNow.AddHours(24);
                await _userService.UpdateAsync(user);
            }
        }
    }
}
```

## Security Checklist

### Development Security Checklist
- [ ] All inputs validated and sanitized
- [ ] Parameterized queries used throughout
- [ ] Authentication implemented on all protected endpoints
- [ ] Authorization checks in place
- [ ] Sensitive data encrypted at rest and in transit
- [ ] Security headers configured
- [ ] Error handling doesn't expose sensitive information
- [ ] Logging includes security events
- [ ] Dependencies regularly updated
- [ ] Security testing integrated into CI/CD

### Deployment Security Checklist
- [ ] HTTPS enforced across all endpoints
- [ ] Firewall rules configured
- [ ] WAF enabled and configured
- [ ] Database access restricted
- [ ] Monitoring and alerting configured
- [ ] Backup encryption enabled
- [ ] Access controls implemented
- [ ] Security patches applied
- [ ] Compliance requirements met
- [ ] Incident response plan in place

### Operational Security Checklist
- [ ] Regular security assessments conducted
- [ ] Vulnerability scans performed
- [ ] Access reviews completed quarterly
- [ ] Security training up to date
- [ ] Incident response plan tested
- [ ] Backup and recovery procedures tested
- [ ] Security metrics monitored
- [ ] Compliance audits passed
- [ ] Third-party security assessments completed
- [ ] Security documentation updated

## Support

For security support:
- **Security Team**: security@gccdatabase.com
- **Incident Reporting**: incidents@gccdatabase.com
- **Security Hotline**: +1-555-0199
- **Documentation**: /Documentation/Security_Guide.md

## Version History

- **v1.0.0** - Initial security guide
- Last Updated: August 2024
# Non-Azure Deployment Configuration Guide
## For Plesk with MariaDB/PostgreSQL and Email-Based 2FA

## 1. DATABASE CONFIGURATION

### Option A: MariaDB (MySQL Compatible)

#### Install Required NuGet Package:
```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.2
```

#### Update appsettings.Test.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=gcc_database;User=gcc_user;Password=YOUR_PASSWORD;",
    "MariaDbConnection": "Server=localhost;Port=3306;Database=gcc_database;User=gcc_user;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  }
}
```

### Option B: PostgreSQL

#### Install Required NuGet Package:
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.10
```

#### Update appsettings.Test.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=gcc_database;Username=gcc_user;Password=YOUR_PASSWORD",
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=gcc_database;Username=gcc_user;Password=YOUR_PASSWORD"
  }
}
```

## 2. PROGRAM.CS MODIFICATIONS

Replace the database configuration section in Program.cs:

```csharp
// ===== DATABASE CONFIGURATION =====
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "sqlite";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (dbProvider.ToLower())
    {
        case "mariadb":
        case "mysql":
            var mariaDbConnection = builder.Configuration.GetConnectionString("MariaDbConnection") 
                ?? builder.Configuration.GetConnectionString("DefaultConnection");
            var mariaDbVersion = new MariaDbServerVersion(new Version(10, 11, 0));
            options.UseMySql(mariaDbConnection, mariaDbVersion);
            break;
            
        case "postgresql":
        case "postgres":
            var postgresConnection = builder.Configuration.GetConnectionString("PostgreSqlConnection") 
                ?? builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(postgresConnection);
            break;
            
        default: // sqlite
            options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
            break;
    }
});
```

## 3. TWO-FACTOR AUTHENTICATION (Without Azure)

### Simple Email-Based 2FA Implementation

#### Create Services/TwoFactorService.cs:
```csharp
using System.Security.Cryptography;

namespace GCCWebApp.Services
{
    public interface ITwoFactorService
    {
        string GenerateCode();
        bool ValidateCode(string userId, string code);
        Task SendCodeByEmailAsync(string email, string code);
        void StoreCode(string userId, string code);
    }

    public class TwoFactorService : ITwoFactorService
    {
        private readonly ILogger<TwoFactorService> _logger;
        private readonly IEmailService _emailService;
        // In production, use a cache service or database
        private static readonly Dictionary<string, (string code, DateTime expiry)> _codes = new();

        public TwoFactorService(ILogger<TwoFactorService> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public string GenerateCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
            return code.ToString("D6");
        }

        public void StoreCode(string userId, string code)
        {
            _codes[userId] = (code, DateTime.UtcNow.AddMinutes(5));
            _logger.LogInformation($"2FA code stored for user {userId}");
        }

        public bool ValidateCode(string userId, string code)
        {
            if (!_codes.ContainsKey(userId))
                return false;

            var (storedCode, expiry) = _codes[userId];
            
            if (DateTime.UtcNow > expiry)
            {
                _codes.Remove(userId);
                return false;
            }

            if (storedCode == code)
            {
                _codes.Remove(userId);
                return true;
            }

            return false;
        }

        public async Task SendCodeByEmailAsync(string email, string code)
        {
            var subject = "Your GCC Database 2FA Code";
            var body = $@"
                <h2>Two-Factor Authentication Code</h2>
                <p>Your verification code is: <strong>{code}</strong></p>
                <p>This code will expire in 5 minutes.</p>
                <p>If you didn't request this code, please ignore this email.</p>
            ";

            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
}
```

#### Create Services/EmailService.cs:
```csharp
using System.Net;
using System.Net.Mail;

namespace GCCWebApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            
            using var client = new SmtpClient
            {
                Host = smtpSettings["Host"],
                Port = int.Parse(smtpSettings["Port"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
                Credentials = new NetworkCredential(
                    smtpSettings["Username"],
                    smtpSettings["Password"]
                )
            };

            var message = new MailMessage
            {
                From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            
            message.To.Add(to);

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                throw;
            }
        }
    }
}
```

## 4. EMAIL CONFIGURATION

Add to appsettings.Test.json:
```json
{
  "SmtpSettings": {
    "Host": "smtp.your-provider.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@domain.com",
    "Password": "your-email-password",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "GCC Database"
  },
  "DatabaseProvider": "mariadb",
  "Authentication": {
    "Enable2FA": true,
    "EnableTestMode": false,
    "RequireEmailConfirmation": true
  }
}
```

## 5. UPDATE ACCOUNT CONTROLLER

In AccountController.cs, update the Login method:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    // Validate user credentials
    var userRole = ValidateUser(model.Email, model.Password);
    
    if (string.IsNullOrEmpty(userRole))
    {
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    // Check if 2FA is enabled
    var is2FAEnabled = _configuration.GetValue<bool>("Authentication:Enable2FA");
    
    if (is2FAEnabled)
    {
        // Generate and send 2FA code
        var code = _twoFactorService.GenerateCode();
        _twoFactorService.StoreCode(model.Email, code);
        await _twoFactorService.SendCodeByEmailAsync(model.Email, code);
        
        // Store user info temporarily
        TempData["2FAEmail"] = model.Email;
        TempData["2FARole"] = userRole;
        TempData["ReturnUrl"] = returnUrl;
        
        return RedirectToAction("TwoFactorAuthentication");
    }

    // Direct login without 2FA
    await SignInUser(model.Email, userRole, model.RememberMe);
    return RedirectToLocal(returnUrl);
}

[HttpGet]
public IActionResult TwoFactorAuthentication()
{
    if (TempData["2FAEmail"] == null)
    {
        return RedirectToAction("Login");
    }
    
    // Keep the values for the post action
    TempData.Keep();
    return View("2FAAuthenticate");
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Verify2FA(string authCode, bool trustDevice)
{
    var email = TempData["2FAEmail"]?.ToString();
    var role = TempData["2FARole"]?.ToString();
    var returnUrl = TempData["ReturnUrl"]?.ToString();
    
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
    {
        return RedirectToAction("Login");
    }
    
    if (_twoFactorService.ValidateCode(email, authCode))
    {
        await SignInUser(email, role, trustDevice);
        return RedirectToLocal(returnUrl);
    }
    
    ViewBag.Error = "Invalid or expired code. Please try again.";
    TempData.Keep(); // Keep for retry
    return View("2FAAuthenticate");
}
```

## 6. REGISTER SERVICES IN PROGRAM.CS

Add these services:
```csharp
// Email and 2FA services
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<ITwoFactorService, TwoFactorService>();
```

## 7. DATABASE MIGRATION

### For MariaDB:
```bash
# Create migration
dotnet ef migrations add InitialMariaDB --context ApplicationDbContext

# Update database
dotnet ef database update --context ApplicationDbContext
```

### For PostgreSQL:
```bash
# Create migration
dotnet ef migrations add InitialPostgreSQL --context ApplicationDbContext

# Update database
dotnet ef database update --context ApplicationDbContext
```

## 8. PLESK SETUP SPECIFICS

### Create Database in Plesk:

1. **For MariaDB:**
   - Go to "Databases" in Plesk
   - Click "Add Database"
   - Database name: `gcc_database`
   - Create database user: `gcc_user`
   - Set strong password
   - Grant all privileges

2. **For PostgreSQL:**
   - Similar process but select PostgreSQL
   - Note the port (usually 5432)
   - Create user and database

### Environment Variables in Plesk:
Add these in "ASP.NET Settings":
- `ASPNETCORE_ENVIRONMENT`: Test
- `DatabaseProvider`: mariadb (or postgresql)

## 9. TESTING THE SETUP

1. **Test Database Connection:**
```csharp
// Add a test endpoint in a controller
[HttpGet]
public async Task<IActionResult> TestDb()
{
    try
    {
        var count = await _context.Users.CountAsync();
        return Ok($"Database connected. User count: {count}");
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Database error: {ex.Message}");
    }
}
```

2. **Test Email Service:**
```csharp
[HttpGet]
public async Task<IActionResult> TestEmail()
{
    try
    {
        await _emailService.SendEmailAsync(
            "test@example.com", 
            "Test Email", 
            "This is a test email from GCC Database"
        );
        return Ok("Email sent successfully");
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Email error: {ex.Message}");
    }
}
```

## 10. COMMON SMTP PROVIDERS

### Gmail:
- Host: smtp.gmail.com
- Port: 587
- EnableSsl: true
- Note: Need app-specific password

### SendGrid:
- Host: smtp.sendgrid.net
- Port: 587
- EnableSsl: true
- Username: apikey
- Password: YOUR_SENDGRID_API_KEY

### Local Mail Server (for testing):
- Host: localhost
- Port: 25
- EnableSsl: false

## SECURITY NOTES

1. **Store sensitive data securely:**
   - Use Plesk's environment variables for passwords
   - Never commit credentials to git

2. **For production:**
   - Use a proper cache service (Redis) for 2FA codes
   - Implement rate limiting on 2FA attempts
   - Add CAPTCHA after failed attempts
   - Use secure session management

3. **Database security:**
   - Use strong passwords
   - Limit database user permissions
   - Enable SSL for database connections
   - Regular backups

---
Last Updated: August 21, 2025
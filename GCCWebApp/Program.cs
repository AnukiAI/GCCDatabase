using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using MySql.EntityFrameworkCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreRateLimit;
using GCCWebApp.Configuration;
using GCCWebApp.Data;
using GCCWebApp.Repositories;
using GCCWebApp.Services;
using GCCWebApp.Authentication;
using GCCWebApp.Services.Security;
using GCCWebApp.Middleware;
using GCCWebApp.Extensions;
using GCCWebApp.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ===== SECURE CONFIGURATION SETUP =====
// Configure Azure Key Vault and secure configuration management
builder.Configuration.AddAzureKeyVaultIfAvailable(builder.Environment);

// Server-only secrets overlay (DEF-263).
//
// Every other config file the app reads ships inside the deploy zip, so a secret written into one
// on the server is destroyed by the next extract — and destroyed SILENTLY, because empty SMTP
// credentials just fall back to MockEmailService, which logs and delivers nothing. That is how
// email stayed dead here: the setting was applied, then a deploy quietly reverted it.
//
// appsettings.Secrets.json is deliberately NOT in the zip (deploy.ps1 aborts if it appears in
// staging) and NOT in git (.gitignore). Extracting a zip adds and overwrites but never deletes, so
// a file that is absent from the zip survives every deploy untouched. It is registered last, so it
// overrides appsettings.json and appsettings.{Environment}.json.
//
// optional: true keeps developer machines and CI working without it.
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// Re-register environment variables so they still outrank the overlay above. CreateBuilder already
// added them, but anything appended after a provider outranks it, and an env var set on the box is
// the more deliberate act of the two - it should win, not lose to a file.
builder.Configuration.AddEnvironmentVariables();

// Cookie Secure-flag policy. Test/Prod are HTTPS-only (IIS in-process hosting +
// the web.config HTTP→HTTPS redirect rule + HSTS), so all cookies must carry the
// Secure attribute — Always removes any dependence on the app's perceived request
// scheme. Development runs on plain-HTTP localhost, where Secure cookies would
// never be sent, so relax to SameAsRequest there only. (Replaces the earlier
// SameAsRequest-everywhere "TEMPORARY FIX" TODOs on the session/auth/antiforgery
// cookies.)
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// ===== SESSION CONFIGURATION =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Secure on HTTPS-only Test/Prod; relaxed only on Development (plain-HTTP localhost).
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "GCC.Session";
});

// ===== DISTRIBUTED CACHE =====
builder.Services.AddDistributedMemoryCache();

// ===== DATA PROTECTION =====
// Keys persist on the file system (not the user profile) so they survive app-pool recycles.
//
// DEF-363 — the location is now CONFIGURABLE and should point OUTSIDE the content root.
// It used to be hardcoded to <content root>\DataProtectionKeys. Under IIS that is actively
// harmful: IIS watches the application directory, and the key ring is WRITTEN there on first
// start (and Directory.CreateDirectory below touches it on every start). The resulting change
// notification shuts the app down — and with hostingModel="inprocess" a stopped app cannot
// restart in the same worker process, so every subsequent request returns HTTP 500 until the
// pool is recycled. Observed on gccwebapp.com 2026-08-18: the app started, served /__gate 200,
// warmed its queries, then logged "Application is shutting down..." and 500'd thereafter.
//
// It only looked stable before because the key files already existed, so nothing was written.
// Deleting them (correctly — they were leaked) exposed the latent fault.
//
// Set DataProtection:KeyPath to somewhere outside the deployed folder. A relative value is
// resolved against the content root, so "..\\DataProtectionKeys" puts it alongside the app
// folder rather than inside it. Leaving it unset preserves the old in-folder behaviour.
//
// DEF-395 (2026-09-02) — KeyPath ACTUALLY WORKS AS OF THIS FIX. It previously did nothing:
// SecurityServicesExtensions.AddDataProtection ran later (Program.cs:487) and re-registered a
// hardcoded <content root>\DataProtectionKeys, overriding whatever was resolved here, while the
// CreateDirectory call below still threw on an un-writable path. Setting KeyPath could therefore
// only ever crash startup and never move the keys. The extension no longer registers file-system
// storage; this block is now the single owner of it.
//
// Because the setting is live again, the directory creation is guarded. An un-writable KeyPath
// (the 2026-08-18 outage: the app-pool identity could not write to the vhost root) must NOT take
// the site down with HTTP 500.30 on every request. It falls back to the in-folder default — the
// known-good behaviour — and says so loudly, because a silent fallback would put the key ring
// somewhere the operator did not intend.
var configuredKeyPath = builder.Configuration["DataProtection:KeyPath"];
var inFolderKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys");
var dataProtectionPath = string.IsNullOrWhiteSpace(configuredKeyPath)
    ? inFolderKeyPath
    : (Path.IsPathRooted(configuredKeyPath)
        ? configuredKeyPath
        : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredKeyPath)));

try
{
    Directory.CreateDirectory(dataProtectionPath);
}
catch (Exception ex)
{
    Console.WriteLine(
        $"ERROR: DataProtection:KeyPath '{dataProtectionPath}' is not usable ({ex.GetType().Name}: {ex.Message}). " +
        $"Falling back to '{inFolderKeyPath}'. Grant the app-pool identity write access to the configured " +
        "path, or unset DataProtection:KeyPath to silence this. Keys written to the in-folder default are " +
        "inside the directory IIS watches (see the DEF-363 note above).");
    dataProtectionPath = inFolderKeyPath;
    try
    {
        Directory.CreateDirectory(dataProtectionPath);
    }
    catch (Exception fallbackEx)
    {
        // Neither location is writable. Let this one throw: continuing would hand every user a
        // key ring that lives only in memory, so every app-pool recycle would silently sign
        // everyone out and invalidate every antiforgery token. Failing at startup is louder.
        Console.WriteLine($"FATAL: fallback DataProtection path '{dataProtectionPath}' is also unusable.");
        throw new InvalidOperationException(
            $"No writable DataProtection key path. Tried '{configuredKeyPath}' and '{inFolderKeyPath}'.",
            fallbackEx);
    }
}

var dataProtectionBuilder = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Pin the key-ring discriminator to a stable name. Without this it derives from the app's own
// path, so moving the app (or the key folder) silently invalidates every cookie. The config key
// already existed but was never read.
var dataProtectionAppName = builder.Configuration["DataProtection:ApplicationName"];
if (!string.IsNullOrWhiteSpace(dataProtectionAppName))
{
    dataProtectionBuilder.SetApplicationName(dataProtectionAppName);
}

Console.WriteLine($"DataProtection keys will be stored in: {dataProtectionPath}");

// ===== DATABASE CONFIGURATION =====
// Support for multiple database providers based on connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("SqliteConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    // Detect database type from connection string
    if (connectionString.Contains("Server=tcp:") ||
        (connectionString.Contains("Server=") && (connectionString.Contains("database.windows.net") ||
         connectionString.Contains("mssqllocaldb") || connectionString.Contains("sqlexpress") ||
         connectionString.Contains("Integrated Security") || connectionString.Contains("Trusted_Connection") ||
         (connectionString.Contains("Database=") && connectionString.Contains("TrustServerCertificate")))))
    {
        // SQL Server / Azure SQL Database connection string format
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                // DEF-350: was a hardcoded 30. The certificate-import preview scales with
                // database size and at go-live volume (5,405 certs) already runs close to that
                // ceiling, failing with "Execution Timeout Expired" — which reads as a flaky
                // import rather than a limit being hit. Default is unchanged at 30s; set
                // Database:CommandTimeoutSeconds (or Database__CommandTimeoutSeconds) to raise it
                // for a migration window. This is a mitigation, not a fix: the underlying cost
                // of the preview path is still unexplained.
                sqlServerOptions.CommandTimeout(
                    builder.Configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 30);
            });
        });
        Console.WriteLine("Using SQL Server database provider");

        // Say WHICH database, not just which provider.
        //
        // Two sites now run this same bundle on the same Plesk platform — gccwebapp.com against
        // gccwebap_certificates (real client data) and test.gccwebapp.com against gccwebap_test.
        // The ONLY thing pointing the test site away from production data is its own
        // appsettings.Secrets.json overlay, which is deliberately absent from the deploy zip.
        // So the failure mode is: overlay missing, malformed, or the key misspelled -> the app
        // falls straight back to the connection string in appsettings.Production.json and the
        // test site starts writing to live client records. Nothing errors. Nothing looks wrong.
        // Every page works. That is exactly the shape of the SMTP failure DEF-263 chased for
        // weeks, and the fix is the same one: make the live value observable at startup.
        //
        // Password is never printed — SqlConnectionStringBuilder is parsed for Data Source,
        // Initial Catalog and User ID only. Wrapped in try/catch because a malformed connection
        // string must not be able to take the site down on startup (the DEF-395 KeyPath lesson:
        // a diagnostic that can crash the process is worse than no diagnostic).
        try
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            Console.WriteLine($"  DB server  : {csb.DataSource}");
            Console.WriteLine($"  DB catalog : {csb.InitialCatalog}");
            Console.WriteLine($"  DB user    : {csb.UserID}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  WARNING: could not parse the SQL connection string to report its target ({ex.GetType().Name}). " +
                              "The app will still try to connect; verify the target another way before trusting this site.");
        }
    }
    else if (connectionString.Contains("Host=") || connectionString.Contains("postgres"))
    {
        // PostgreSQL / Azure Database for PostgreSQL connection string format
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });
        });
        Console.WriteLine("Using PostgreSQL database provider");
    }
    else if (connectionString.Contains("Server=") && connectionString.Contains("Port=") &&
        connectionString.Contains("Database=") && (connectionString.Contains("User=") || connectionString.Contains("Uid=")))
    {
        // MySQL/MariaDB connection string format
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySQL(connectionString, mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });
        Console.WriteLine("Using MySQL/MariaDB database provider");
    }
    else
    {
        // FAIL-FAST: Unrecognized connection string format
        var errorMessage = $"Invalid or unrecognized database connection string format.\n" +
            $"Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...\n\n" +
            $"REQUIRED: SQL Server connection string must contain:\n" +
            $"  - 'Server=' or 'Server=tcp:'\n" +
            $"  - 'Database='\n" +
            $"  - Either 'Integrated Security' OR 'User Id=' and 'Password='\n\n" +
            $"Example: Server=SERVERNAME;Database=DBNAME;User Id=USER;Password=PASS;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=true;\n\n" +
            $"IMPORTANT: SQLite and other database providers are not supported in this application.\n" +
            $"Only SQL Server is allowed for data storage.";

        Console.Error.WriteLine($"❌ CRITICAL DATABASE CONFIGURATION ERROR\n{errorMessage}");
        throw new InvalidOperationException(errorMessage);
    }
}
else
{
    // FAIL-FAST: No connection string configured
    var errorMessage = "No database connection string configured.\n\n" +
        "REQUIRED: 'DefaultConnection' must be configured in appsettings.json\n\n" +
        "Example configuration:\n" +
        "\"ConnectionStrings\": {\n" +
        "  \"DefaultConnection\": \"Server=SERVERNAME;Database=DBNAME;User Id=USER;Password=PASS;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=true;\"\n" +
        "}\n\n" +
        "IMPORTANT: Only SQL Server is supported. SQLite and other database providers are not allowed.";

    Console.Error.WriteLine($"❌ CRITICAL DATABASE CONFIGURATION ERROR\n{errorMessage}");
    throw new InvalidOperationException(errorMessage);
}

// ===== AZURE AD B2C AUTHENTICATION =====
var azureAdB2CSection = builder.Configuration.GetSection("AzureAdB2C");
builder.Services.Configure<AzureAdB2COptions>(azureAdB2CSection);

// Check if Azure AD B2C is properly configured
var azureAdB2CConfig = azureAdB2CSection.Get<AzureAdB2COptions>();
var isAzureAdB2CConfigured = azureAdB2CConfig != null && 
    !string.IsNullOrEmpty(azureAdB2CConfig.ClientId) && 
    !azureAdB2CConfig.ClientId.Contains("{") &&
    !string.IsNullOrEmpty(azureAdB2CConfig.TenantId) &&
    !azureAdB2CConfig.TenantId.Contains("{");

if (isAzureAdB2CConfigured)
{
    // Configure Azure AD B2C authentication
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options);
            options.Authority = azureAdB2CConfig!.SignUpSignInAuthority;
            options.SignedOutCallbackPath = azureAdB2CConfig.SignedOutCallbackPath;

            // Configure events for B2C specific scenarios
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context =>
                {
                    // For API requests, return JSON instead of redirecting
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        context.HandleResponse();
                        return context.Response.WriteAsync("{\"success\":false,\"message\":\"Authentication required. Please log in.\",\"errorCode\":\"UNAUTHORIZED\"}");
                    }

                    // Handle password reset policy
                    if (context.Properties.Items.ContainsKey("policy") &&
                        context.Properties.Items["policy"] == azureAdB2CConfig.ResetPasswordPolicyId)
                    {
                        context.ProtocolMessage.IssuerAddress = azureAdB2CConfig.ResetPasswordAuthority + "/oauth2/v2.0/authorize";
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    // Handle password reset flow
                    if (context.Exception.Message.Contains("AADB2C90118"))
                    {
                        // Redirect to password reset policy
                        context.Response.Redirect($"/Account/ResetPassword");
                        context.HandleResponse();
                    }
                    return Task.CompletedTask;
                }
            };
        });

    // Configure cookie events for B2C to handle API requests
    builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"success\":false,\"message\":\"Authentication required. Please log in.\",\"errorCode\":\"UNAUTHORIZED\"}");
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"success\":false,\"message\":\"Access denied. You do not have permission to perform this action.\",\"errorCode\":\"FORBIDDEN\"}");
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
}
else
{
    // Fallback to cookie authentication for development
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
            options.Cookie.Name = "GCC.Auth";
            options.Cookie.HttpOnly = true;
            // Secure on HTTPS-only Test/Prod; relaxed only on Development (plain-HTTP localhost).
            options.Cookie.SecurePolicy = cookieSecurePolicy;

            // Handle API requests - return JSON instead of redirecting to login page
            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    // Return 401 for API, Export, and Dashboard AJAX requests instead of redirecting to login page
                    if (context.Request.Path.StartsWithSegments("/api") ||
                        context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Path.StartsWithSegments("/Export") ||
                        context.Request.Path.StartsWithSegments("/Dashboard/Download"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"success\":false,\"message\":\"Authentication required. Please log in.\",\"errorCode\":\"UNAUTHORIZED\"}");
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") ||
                        context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Path.StartsWithSegments("/Export") ||
                        context.Request.Path.StartsWithSegments("/Dashboard/Download"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"success\":false,\"message\":\"Access denied. You do not have permission to perform this action.\",\"errorCode\":\"FORBIDDEN\"}");
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                }
            };
        });
}

// ===== API AUTHENTICATION =====
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, opts => { })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtKey = Environment.GetEnvironmentVariable("GCC_JWT_SECRET") ?? 
                     builder.Configuration["Jwt:Key"] ?? 
                     throw new InvalidOperationException("JWT signing key must be configured via GCC_JWT_SECRET environment variable or Jwt:Key configuration");
        
        // Validate JWT key strength
        if (jwtKey.Length < 64)
            throw new InvalidOperationException("JWT signing key must be at least 64 characters long for security");
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer must be configured"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience must be configured"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            // Enhanced security settings
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            RequireAudience = true
        };
    });

// ===== AUTHORIZATION POLICIES =====
builder.Services.AddAuthorization(options =>
{
    // Role-based policies for testing - simplified but functional
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
                  context.User.HasClaim("Role", "Partner") ||
                  context.User.HasClaim("Role", "Admin") ||
                  context.User.HasClaim("Role", "Analyst")));

    options.AddPolicy("AuthenticatedUsers", policy =>
        policy.RequireAuthenticatedUser());

    // Default policy requires authentication
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ===== CSRF PROTECTION =====
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    // Secure on HTTPS-only Test/Prod; relaxed only on Development (plain-HTTP localhost).
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SuppressXFrameOptionsHeader = false;
});

// ===== API VERSIONING =====
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.QueryStringApiVersionReader("version"),
        new Asp.Versioning.HeaderApiVersionReader("X-Version"),
        new Asp.Versioning.MediaTypeApiVersionReader("ver"));
}).AddApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// ===== RATE LIMITING =====
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ===== API ENHANCEMENTS =====
// Custom API rate limiting with more granular control
builder.Services.AddApiRateLimiting(options =>
{
    options.AnonymousLimit = 100;
    options.AuthenticatedLimit = 500;
    options.ApiKeyLimit = 1000;
    options.PartnerApiKeyLimit = 200;
    options.AdminLimit = 5000;
});

// API Health Checks
builder.Services.AddApiHealthChecks();

// ===== CORS CONFIGURATION =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000", "https://localhost:3001" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===== PERFORMANCE OPTIMIZATION =====
builder.Services.AddPerformanceOptimization(builder.Configuration);
builder.Services.AddBundlingAndMinification(builder.Environment);

// ===== SECURITY SERVICES =====
// Security services are configured via SecurityServicesExtensions
builder.Services.ConfigureSecurityServices(builder.Configuration);

// ===== SWAGGER CONFIGURATION =====
builder.Services.AddSwaggerConfiguration();

// ===== MVC & RAZOR PAGES CONFIGURATION =====
builder.Services.AddControllersWithViews(options =>
{
    // Require authentication globally for all controllers except those marked with [AllowAnonymous]
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization for API responses
    options.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
    // Handle circular references in navigation properties (Company -> Certificates -> Company)
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    // Emit DateTime values as ISO 8601 UTC with 'Z' suffix (matches Amazon Developer Guide format)
    options.JsonSerializerOptions.Converters.Add(new GCCWebApp.Extensions.UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new GCCWebApp.Extensions.UtcNullableDateTimeConverter());
});

builder.Services.AddRazorPages();

// ===== REPOSITORY PATTERN =====
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// ===== APPLICATION SERVICES =====
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IFilterService, FilterService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
// DEF-286 (QC #198): single source of truth for the upload root + logical→physical
// path resolution. Singleton — config + hosting env are fixed for the process lifetime.
builder.Services.AddSingleton<IUploadPathResolver, UploadPathResolver>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICertificateFieldService, CertificateFieldService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IPartnerApiKeyService, PartnerApiKeyService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICertificatePdfService, CertificatePdfService>();
builder.Services.AddScoped<ICertificateWorkflowService, CertificateWorkflowService>();
builder.Services.AddScoped<IProductIdService, ProductIdService>();

// ===== AUDIT SERVICES =====
builder.Services.AddScoped<GCCWebApp.Services.Audit.IAuditService, GCCWebApp.Services.Audit.AuditService>();

// ===== IMPORT SERVICES =====
builder.Services.AddScoped<GCCWebApp.Services.Import.ICertificateImportService, GCCWebApp.Services.Import.CertificateImportService>();

// ===== PERFORMANCE MONITORING =====
builder.Services.AddScoped<GCCWebApp.Services.Performance.IPerformanceMonitoringService, GCCWebApp.Services.Performance.PerformanceMonitoringService>();

// ===== TWO-FACTOR AUTHENTICATION SERVICES =====
// Register email service based on SMTP configuration
builder.Services.AddScoped<IEmailService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<EmailService>>();
    var smtpSection = configuration.GetSection("SmtpSettings");
    
    // Check if SMTP is properly configured
    var host = smtpSection["Host"];
    var username = smtpSection["Username"];
    var password = smtpSection["Password"];
    
    if (!string.IsNullOrWhiteSpace(host) && 
        !string.IsNullOrWhiteSpace(username) && 
        !string.IsNullOrWhiteSpace(password))
    {
        return new EmailService(configuration, logger);
    }
    else
    {
        var mockLogger = serviceProvider.GetRequiredService<ILogger<MockEmailService>>();
        return new MockEmailService(mockLogger);
    }
});

// SMS Services
builder.Services.AddHttpClient<TwilioSmsService>();
builder.Services.AddHttpClient<TextBeltSmsService>();
builder.Services.AddScoped<TwilioSmsService>();
builder.Services.AddScoped<TextBeltSmsService>();
builder.Services.AddScoped<MockSmsService>();

// SMS Service Factory
builder.Services.AddScoped<ISmsService>(serviceProvider => 
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var smsProvider = configuration.GetValue<string>("SmsSettings:Provider")?.ToLower() ?? "mock";

    return smsProvider switch
    {
        "twilio" => serviceProvider.GetRequiredService<TwilioSmsService>(),
        "textbelt" => serviceProvider.GetRequiredService<TextBeltSmsService>(),
        _ => serviceProvider.GetRequiredService<MockSmsService>()
    };
});

// 2FA Services
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IEnhancedTwoFactorService, EnhancedTwoFactorService>();
// DEF-385: "Trust this device for 30 days" on the 2FA challenge.
builder.Services.AddScoped<ITrustedDeviceService, TrustedDeviceService>();

// ===== SECURITY SERVICES =====
builder.Services.AddSecureConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddHostedService<ConfigurationValidationService>();

// ===== ETL SERVICES =====
builder.Services.AddScoped<GCCWebApp.Services.ETL.IETLService, GCCWebApp.Services.ETL.ETLService>();
builder.Services.AddScoped<GCCWebApp.Services.ETL.IDataExtractor, GCCWebApp.Services.ETL.DataExtractor>();
builder.Services.AddScoped<GCCWebApp.Services.ETL.IDataTransformer, GCCWebApp.Services.ETL.DataTransformer>();
builder.Services.AddScoped<GCCWebApp.Services.ETL.IDataLoader, GCCWebApp.Services.ETL.DataLoader>();
builder.Services.AddScoped<GCCWebApp.Services.ETL.IETLMonitor, GCCWebApp.Services.ETL.ETLMonitor>();
builder.Services.AddHostedService<GCCWebApp.Services.ETL.ETLSchedulerService>();

// ===== CERTIFICATE EXPIRATION SERVICE =====
// Scoped service implements the sweep. Two triggers:
//   1. Login (AccountController.SignInUser) — throttled to once per 24h
//   2. CertificateExpirationBackgroundService — periodic timer, runs whether anyone is logged in
//      or not. Interval via Jobs:CertificateExpiration:IntervalMinutes (default 60).
builder.Services.AddScoped<GCCWebApp.Services.ICertificateExpirationService, GCCWebApp.Services.CertificateExpirationService>();
builder.Services.AddHostedService<GCCWebApp.Services.CertificateExpirationBackgroundService>();

// ===== EF QUERY WARM-UP (DEF-234) =====
// Fires one Take(1) public-tile query at startup so the first real request doesn't pay
// the ~23s EF compile + SQL plan cost. Background task; non-fatal on failure.
builder.Services.AddHostedService<GCCWebApp.Services.EfQueryWarmupService>();

// ===== BASIC SERVICES =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ===== BUILD APPLICATION =====
var app = builder.Build();

// ===== DATA-OPS COMMANDS (argv branch) =====
// Migration plan §10. Runs with the full DI container but WITHOUT the middleware pipeline or a
// listening port, then exits — so it can never be reached over HTTP, only from a console.
// `dotnet run -- link-pdfs --manifest <path> --dry-run`
if (GCCWebApp.Services.DataOps.PdfLinkCommand.Matches(args))
{
    return await GCCWebApp.Services.DataOps.PdfLinkCommand.RunAsync(app, args);
}

// ===== STARTUP LOGGING (before middleware) =====
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("GreenCircle Certified Database application starting up");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

// Email transport banner (DEF-263).
//
// Falling back to MockEmailService is completely silent at runtime: 2FA codes and password-reset
// links are logged and dropped, while the UI still reports success (ForgotPassword deliberately
// returns an identical response either way — DEF-209 anti-enumeration). That combination hid a dead
// mail path here for months. Stating the choice once at startup makes it checkable in stdout.
{
    var smtp = app.Configuration.GetSection("SmtpSettings");
    var smtpConfigured = !string.IsNullOrWhiteSpace(smtp["Host"])
                      && !string.IsNullOrWhiteSpace(smtp["Username"])
                      && !string.IsNullOrWhiteSpace(smtp["Password"]);

    if (smtpConfigured)
    {
        // Never log Password. Username is logged because identifying the sending mailbox is the
        // first thing needed when delivery fails.
        logger.LogInformation(
            "Email: LIVE via {Host}:{Port} ({Security}) as {Username}, from {FromEmail}. " +
            "2FA codes and password-reset links will be delivered.",
            smtp["Host"], smtp["Port"], smtp["Security"] ?? "auto-detect from port",
            smtp["Username"], smtp["FromEmail"]);
    }
    else
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(smtp["Host"])) missing.Add("Host");
        if (string.IsNullOrWhiteSpace(smtp["Username"])) missing.Add("Username");
        if (string.IsNullOrWhiteSpace(smtp["Password"])) missing.Add("Password");

        logger.LogWarning(
            "Email: DISABLED (MockEmailService) — SmtpSettings missing {Missing}. Messages are " +
            "written to this log and NOT delivered, so two-factor authentication and password " +
            "reset will not work. Anyone who enables email 2FA will be locked out. " +
            "Fix by creating appsettings.Secrets.json beside GCCWebApp.dll " +
            "(see appsettings.Secrets.template.json) and recycling the app pool.",
            string.Join(", ", missing));
    }
}

// ===== MIDDLEWARE PIPELINE CONFIGURATION =====

// Performance optimization middleware (early in pipeline)
app.UsePerformanceOptimization(app.Environment);

// Security headers middleware (should be first after performance)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Request/Response logging middleware (early in pipeline for comprehensive logging)
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// API Exception handling middleware (structured API error responses)
app.UseApiExceptionHandling();

// Global exception handling middleware (handles all exceptions with structured logging)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// NOTE: API rate limiting is deliberately NOT registered here. It must run *after*
// authentication so it can read the caller's identity and per-partner claims — see
// the registration below UseAuthentication()/PartnerApiKeyAuthenticationMiddleware.
// Registering it at this point throttles every API caller as anonymous (DEF-345).

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// QC #71: Handle 400/401/403 status codes gracefully instead of showing "This page isn't working"
// For browser requests, redirect expired/unauthorized sessions to the login page.
// API requests already return JSON via cookie event handlers above.
app.UseStatusCodePages(context =>
{
    var response = context.HttpContext.Response;
    var request = context.HttpContext.Request;

    // Skip API requests — they already get JSON responses from cookie auth events
    if (request.Path.StartsWithSegments("/api") ||
        request.Headers["X-Requested-With"] == "XMLHttpRequest")
    {
        return Task.CompletedTask;
    }

    // For 400/401/403 on browser page requests, redirect to login with session_expired message
    if (response.StatusCode == 400 || response.StatusCode == 401 || response.StatusCode == 403)
    {
        response.Redirect("/Account/Login?error=session_expired");
    }

    return Task.CompletedTask;
});

// HTTPS redirection - skip in Test environment to avoid "Failed to determine https port" errors
if (!app.Environment.IsEnvironment("Test"))
{
    app.UseHttpsRedirection();
}

// CORS (must be before routing)
app.UseCors("ApiCorsPolicy");

// Rate limiting (before routing) - disabled in development
if (!app.Environment.IsDevelopment())
{
    app.UseIpRateLimiting();
}

// Test environment access gate (HTTP Basic Auth) — opt-in via config.
// Config: TestEnvironmentGate:Enabled + Username + Password.
// Skips /api/* so Partner API key authentication still works.
// Must sit AFTER UseStatusCodePages so we can disable its 401→login redirect
// for gate-issued 401s, and BEFORE UseStaticFiles so /css, /js, /img aren't public.
app.UseMiddleware<TestEnvironmentGateMiddleware>();

// DEF-208: block /uploads/* before UseStaticFiles so even if wwwroot/uploads/
// still has files from a prior build, they can't be served directly. All cert
// PDF + thumbnail access flows through CertificateController.ViewCertificateFile /
// ViewCertificateImage now (which apply the DEF-204 role+status+ownership gate).
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        return;
    }
    await next();
});

// Static files with cache control headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 7 days in production, 0 in development
        // asp-append-version will handle cache busting when files change
        var cacheDuration = app.Environment.IsProduction() ? 604800 : 0;
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={cacheDuration}");

        // Add ETag for better caching validation
        ctx.Context.Response.Headers.Append("ETag", $"\"{DateTime.UtcNow.Ticks}\"");
    }
});

// Routing
app.UseRouting();

// Session management (before authentication)
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();

// Partner API Key Authentication (validates X-API-Key header for partner requests)
app.UseMiddleware<PartnerApiKeyAuthenticationMiddleware>();

// API Rate Limiting middleware (custom implementation for APIs).
// DEF-345: must sit below UseAuthentication() and the partner key middleware. It resolves the
// caller's limit and counter bucket from claims (partner_rate_limit / partner_id / api_key_id /
// role), all of which are unset until the two middleware above have run. Placed before
// UseAuthorization() so throttling still applies to callers who authenticate but are then denied.
app.UseApiRateLimiting();

app.UseAuthorization();

// API Authentication Middleware - ensures API endpoints return JSON for auth failures instead of HTML redirects
app.UseApiAuthenticationMiddleware();

// Role middleware - set session roles based on claims
app.Use(async (context, next) =>
{
    // Only process session for pages that need it
    if (context.Request.Path.HasValue && !context.Request.Path.Value.StartsWith("/api"))
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Get role from claims and store in session
            var roleClaim = context.User.FindFirst("Role")?.Value;
            if (!string.IsNullOrEmpty(roleClaim))
            {
                context.Session.SetString("UserRole", roleClaim);
            }

            // Get other user info
            var emailClaim = context.User.FindFirst("Email")?.Value;
            var nameClaim = context.User.FindFirst("Name")?.Value;

            if (!string.IsNullOrEmpty(emailClaim))
                context.Session.SetString("UserEmail", emailClaim);

            if (!string.IsNullOrEmpty(nameClaim))
                context.Session.SetString("UserName", nameClaim);
        }
        else
        {
            // Set default role for unauthenticated users
            context.Session.SetString("UserRole", "Public");
        }
    }

    await next();
});

// ===== SWAGGER CONFIGURATION =====
// Development: Public access (no authentication required)
// Production: Role-based access (handled in SwaggerExtensions.cs)
var apiVersionDescriptionProvider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
app.UseSwaggerConfiguration(apiVersionDescriptionProvider);

// ===== REDOC CONFIGURATION (Client-Friendly Documentation) =====
// ReDoc provides clean, read-only API documentation for partners

// Authenticated endpoint to serve the OpenAPI spec for ReDoc
app.MapGet("/api-docs/openapi.json", (HttpContext context, Swashbuckle.AspNetCore.Swagger.ISwaggerProvider swaggerProvider) =>
{
    try
    {
        var swagger = swaggerProvider.GetSwagger("partner-data");
        context.Response.ContentType = "application/json";

        using var stringWriter = new StringWriter();
        var jsonWriter = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(stringWriter);
        swagger.SerializeAsV3(jsonWriter);
        return Results.Content(stringWriter.ToString(), "application/json");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unable to load API specification: {ex.Message}");
    }
})
.WithName("OpenApiSpec")
.RequireAuthorization();

app.MapGet("/api-docs", async (HttpContext context) =>
{
    var html = @"<!DOCTYPE html>
<html>
<head>
    <title>GCC Partner API Documentation</title>
    <meta charset=""utf-8""/>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <link href=""https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700"" rel=""stylesheet"">
    <style>
        body { margin: 0; padding: 0; }
        .topbar { background: #1a5f2a; padding: 15px 20px; color: white; }
        .topbar h1 { margin: 0; font-family: 'Montserrat', sans-serif; font-size: 20px; }
        .topbar p { margin: 5px 0 0 0; font-size: 14px; opacity: 0.9; }
    </style>
</head>
<body>
    <div class=""topbar"">
        <h1>Green Circle Certified - Partner API</h1>
        <p>API Documentation for Amazon, Novi, Wayfair, and other integration partners</p>
    </div>
    <redoc spec-url='/api-docs/openapi.json'
           hide-download-button=""true""
           theme='{
             ""colors"": { ""primary"": { ""main"": ""#1a5f2a"" } },
             ""typography"": { ""fontFamily"": ""Roboto, sans-serif"" }
           }'>
    </redoc>
    <script src=""https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js""></script>
</body>
</html>";
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(html);
})
.WithName("ApiDocs")
.RequireAuthorization();

// ===== DEDICATED PARTNER API DOCUMENTATION ROUTE =====
// Route for partners to access their specific API documentation
app.MapGet("/partner/api-docs", (HttpContext context) =>
{
    // Redirect to ReDoc for cleaner partner experience
    context.Response.Redirect("/api-docs");
    return Task.CompletedTask;
})
.WithName("PartnerApiDocs")
.WithMetadata(new Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute(302));

// ===== ROUTE MAPPING =====

// Certificate number edit route (must come before default)
app.MapControllerRoute(
    name: "certificate_edit_by_number",
    pattern: "Certificate/Edit/{number}",
    defaults: new { controller = "Certificate", action = "Edit" },
    constraints: new { number = @"^[0-9]+-[0-9A-Za-z]+$" });

// Certificate number delete route (must come before default)
app.MapControllerRoute(
    name: "certificate_delete_by_number",
    pattern: "Certificate/Delete/{number}",
    defaults: new { controller = "Certificate", action = "Delete" },
    constraints: new { number = @"^[0-9]+-[0-9A-Za-z]+$" });

// Certificate pre-save thumbnail preview route (must come before default).
// The action parameter is named {fileName}, but the default route only supplies an
// {id} token — so without this the segment binds to id and fileName stays null, and
// PreviewThumbnail returns NotFound (the broken-image preview on the Create page
// before save; Edit worked because ViewCertificateImage binds {id}). DEF-208 flow.
app.MapControllerRoute(
    name: "certificate_preview_thumbnail",
    pattern: "Certificate/PreviewThumbnail/{fileName}",
    defaults: new { controller = "Certificate", action = "PreviewThumbnail" });

// Dashboard route - explicit mapping for /Dashboard to Dashboard/Index
app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard",
    defaults: new { controller = "Dashboard", action = "Index" });

// CertificateImport route - explicit mapping for /CertificateImport to Index
app.MapControllerRoute(
    name: "certificate_import",
    pattern: "CertificateImport",
    defaults: new { controller = "CertificateImport", action = "Index" });

// Default MVC route - goes to public certificates view
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Certificate}/{action=CertificateTileView}/{id?}");

// Area routes (if you have admin areas)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Razor Pages
app.MapRazorPages();

// Health Checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

logger.LogInformation("Application started successfully");

// ===== VALIDATE SECURE CONFIGURATION =====
await app.Services.ValidateSecureConfigurationAsync();

// ===== DATABASE INITIALIZATION =====
// Environment-specific database initialization strategies
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (app.Environment.IsDevelopment())
        {
            // Development: apply migrations and seed the essential baseline data on a fresh
            // DB (Roles/Permissions/Users/API keys). Idempotent — no-ops when the DB already
            // has Roles, so existing dev databases are untouched. ISS-001.
            await SeedData.InitializeAsync(context, seedEssentialData: true);
            logger.LogInformation("✅ Development database migrations applied successfully (essential baseline data seeded on fresh DBs; ISS-001)");
        }
        else if (app.Environment.EnvironmentName == "Testing")
        {
            // Testing: Use EnsureCreated for InMemory databases (no migrations needed)
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("✅ Testing database created successfully");
        }
        else if (app.Environment.IsStaging())
        {
            // Staging: MANUAL MIGRATION MANAGEMENT - Do not auto-migrate.
            // Schema is applied out-of-band by the deploy process (the generated
            // migration SQL, e.g. migrations/apply-migrations.sql, run at deploy time).
            logger.LogInformation("🔧 Staging environment - skipping automatic migrations (manual migration management enabled)");

            // Validate database connectivity
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogCritical("🚨 CRITICAL: Staging database connection failed!");
                throw new InvalidOperationException("Staging database is not accessible");
            }

            logger.LogInformation("✅ Staging database connectivity verified");
        }
        else if (app.Environment.IsProduction())
        {
            // Validate database connectivity first
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogCritical("🚨 CRITICAL: Production database connection failed!");
                throw new InvalidOperationException("Production database is not accessible");
            }
            logger.LogInformation("✅ Production database connectivity verified");

            // Auto-migrate on startup (DEF-252 / recurrence of DEF-239).
            // gccwebapp.com is a single-tenant pre-launch TEST env that runs under the
            // Production environment name. Two login outages (DEF-239, DEF-252) were caused
            // by deploying code whose EF migration was never run on the DB by hand — the
            // schema fell behind the model and every Users query threw. Applying pending
            // migrations at startup closes that gap permanently, and EF runs them with
            // QUOTED_IDENTIFIER ON (avoiding the Msg 1934 half-apply the hand-run SQL hit).
            // Gated by Database:AutoMigrateOnStartup (default true) so a future real
            // production cutover can revert to manual management by setting it false.
            var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup", true);
            if (autoMigrate)
            {
                var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
                if (pending.Count > 0)
                {
                    logger.LogInformation("🔧 Production auto-migrate: applying {Count} pending migration(s): {Migrations}",
                        pending.Count, string.Join(", ", pending));
                    await context.Database.MigrateAsync();
                    logger.LogInformation("✅ Production database migrations applied successfully ({Count} applied)", pending.Count);
                }
                else
                {
                    logger.LogInformation("✅ Production database schema up to date (no pending migrations)");
                }
            }
            else
            {
                logger.LogInformation("🔧 Production environment - auto-migrate disabled (Database:AutoMigrateOnStartup=false); manual migration management");
            }
        }
        else
        {
            // Unknown environment - MANUAL MIGRATION MANAGEMENT
            logger.LogWarning("⚠️  Unknown environment '{Environment}' - skipping automatic migrations (manual migration management enabled)", app.Environment.EnvironmentName);

            // Validate database connectivity
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("⚠️  Database connection failed in unknown environment");
            }
        }
    }
    catch (Exception ex)
    {
        var errorMessage = $"Critical database initialization error in {app.Environment.EnvironmentName} environment";
        logger.LogCritical(ex, errorMessage);

        if (!app.Environment.IsDevelopment())
        {
            // In production/staging, database issues should prevent startup
            throw new InvalidOperationException($"{errorMessage}: {ex.Message}", ex);
        }

        // In development, log error but don't crash (allows debugging)
        logger.LogError("Development environment - continuing startup despite database error");
    }
}

// ===== RUN APPLICATION =====
app.Run();

// The entry point returns int because the data-ops argv branch above exits with a status code.
// app.Run() blocks until shutdown, so this is only reached on a clean stop.
return 0;

// Make the implicit Program class accessible to test projects
public partial class Program { }
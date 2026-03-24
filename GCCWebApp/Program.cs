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

// ===== SESSION CONFIGURATION =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // TEMPORARY FIX: Allow cookies without valid SSL certificate
    // TODO: Install proper SSL certificate and change back to CookieSecurePolicy.Always
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "GCC.Session";
});

// ===== DISTRIBUTED CACHE =====
builder.Services.AddDistributedMemoryCache();

// ===== DATA PROTECTION =====
// Configure keys to persist in application folder (not user profile)
// This ensures keys survive deployments and app pool recycles
var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

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
                sqlServerOptions.CommandTimeout(30);
            });
        });
        Console.WriteLine("Using SQL Server database provider");
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
            // TEMPORARY FIX: Allow cookies without valid SSL certificate
            // TODO: Install proper SSL certificate and change back to CookieSecurePolicy.Always
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

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
    // Use SameAsRequest for container deployments behind reverse proxy
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ICertificateFieldService, CertificateFieldService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IPartnerApiKeyService, PartnerApiKeyService>();
builder.Services.AddScoped<IAmazonApiService, AmazonApiService>();
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
// Registered as scoped — triggered on login or page load, throttled to once per 24 hours
builder.Services.AddScoped<GCCWebApp.Services.ICertificateExpirationService, GCCWebApp.Services.CertificateExpirationService>();

// ===== BASIC SERVICES =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ===== BUILD APPLICATION =====
var app = builder.Build();

// ===== STARTUP LOGGING (before middleware) =====
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("GreenCircle Certified Database application starting up");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

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

// API Rate Limiting middleware (custom implementation for APIs)
app.UseApiRateLimiting();

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

// Public endpoint to serve the OpenAPI spec (allows anonymous access for ReDoc)
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
.AllowAnonymous();

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
.AllowAnonymous();

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
            // Development: Apply migrations automatically (data seeding via SQL scripts only)
            await SeedData.InitializeAsync(context);
            logger.LogInformation("✅ Development database migrations applied successfully (use SQL scripts in Database_Scripts/ to seed data)");
        }
        else if (app.Environment.EnvironmentName == "Testing")
        {
            // Testing: Use EnsureCreated for InMemory databases (no migrations needed)
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("✅ Testing database created successfully");
        }
        else if (app.Environment.IsStaging())
        {
            // Staging: MANUAL MIGRATION MANAGEMENT - Do not auto-migrate
            // Database schema must be managed via SQL scripts (00_CREATE_SCHEMA.sql, FULL_TEST_DATA_SEED.sql)
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
            // Production: MANUAL MIGRATION MANAGEMENT - Do not auto-migrate
            // Database schema must be managed via SQL scripts (00_CREATE_SCHEMA.sql, FULL_TEST_DATA_SEED.sql)
            logger.LogInformation("🔧 Production environment - skipping automatic migrations (manual migration management enabled)");

            // Validate database connectivity
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogCritical("🚨 CRITICAL: Production database connection failed!");
                throw new InvalidOperationException("Production database is not accessible");
            }

            logger.LogInformation("✅ Production database connectivity verified");
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

// Make the implicit Program class accessible to test projects
public partial class Program { }
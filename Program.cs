using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "GCC.Session";
});

// ===== DISTRIBUTED CACHE =====
builder.Services.AddDistributedMemoryCache();

// ===== DATABASE CONFIGURATION =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));

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
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
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
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
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
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICertificatePdfService, CertificatePdfService>();
builder.Services.AddScoped<ICertificateWorkflowService, CertificateWorkflowService>();

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

// Global exception handling middleware (handles all exceptions with structured logging)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

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

// HTTPS redirection
app.UseHttpsRedirection();

// CORS (must be before routing)
app.UseCors("ApiCorsPolicy");

// Rate limiting (before routing) - disabled in development
if (!app.Environment.IsDevelopment())
{
    app.UseIpRateLimiting();
}

// Static files (already configured in UsePerformanceOptimization)

// Routing
app.UseRouting();

// Session management (before authentication)
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

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
if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
    app.UseSwaggerConfiguration(apiVersionDescriptionProvider);
}

// ===== ROUTE MAPPING =====

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

logger.LogInformation("Application started successfully");

// ===== VALIDATE SECURE CONFIGURATION =====
await app.Services.ValidateSecureConfigurationAsync();

// ===== SEED DATABASE =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedData.InitializeAsync(context);
        logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred seeding the database");
    }
}

// ===== RUN APPLICATION =====
app.Run();

// Make the implicit Program class accessible to test projects
public partial class Program { }
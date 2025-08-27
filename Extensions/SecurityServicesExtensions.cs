using Azure.Identity;
using GCCWebApp.Middleware;
using GCCWebApp.Services.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Security.Cryptography.X509Certificates;

namespace GCCWebApp.Extensions;

/// <summary>
/// Extension methods for configuring security services and middleware
/// </summary>
public static class SecurityServicesExtensions
{
    /// <summary>
    /// Configures comprehensive security services in the service collection
    /// </summary>
    public static IServiceCollection ConfigureSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSecurityServices(configuration);
    }

    /// <summary>
    /// Adds comprehensive security services to the service collection
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register security services
        services.AddScoped<IKeyVaultService, KeyVaultService>();
        services.AddScoped<ISecurityMonitoringService, SecurityMonitoringService>();
        services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();

        // Configure data protection
        services.AddDataProtection(configuration);

        // Configure certificate services
        services.AddCertificateServices(configuration);

        // Configure session security
        services.AddSessionSecurity(configuration);

        // Configure CORS with security
        services.AddSecureCors(configuration);

        // Configure forwarded headers for reverse proxy scenarios
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            
            // Only trust proxies from known networks in production
            var trustedProxies = configuration.GetSection("Security:TrustedProxies").Get<string[]>();
            if (trustedProxies?.Any() == true)
            {
                foreach (var proxy in trustedProxies)
                {
                    if (System.Net.IPAddress.TryParse(proxy, out var ipAddress))
                    {
                        options.KnownProxies.Add(ipAddress);
                    }
                }
            }
        });

        return services;
    }

    /// <summary>
    /// Configures data protection with Azure Key Vault or local storage
    /// </summary>
    private static IServiceCollection AddDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName(configuration.GetValue<string>("DataProtection:ApplicationName", "GCC-WebApp"));

        // Configure key lifetime
        var keyLifetime = configuration.GetValue<string>("DataProtection:KeyLifetime");
        if (!string.IsNullOrEmpty(keyLifetime) && TimeSpan.TryParse(keyLifetime, out var lifetime))
        {
            dataProtectionBuilder.SetDefaultKeyLifetime(lifetime);
        }

        // Configure key storage
        var persistToAzure = configuration.GetValue<bool>("DataProtection:PersistKeysToAzureBlobStorage", false);
        if (persistToAzure)
        {
            var connectionString = configuration.GetValue<string>("DataProtection:AzureBlobStorageConnectionString");
            var containerName = configuration.GetValue<string>("DataProtection:AzureBlobStorageContainerName");
            
            if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(containerName))
            {
                dataProtectionBuilder.PersistKeysToAzureBlobStorage(connectionString, containerName, "keys.xml");
            }
        }
        else
        {
            // Use local file system in development
            var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys");
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        return services;
    }

    /// <summary>
    /// Configures certificate services for SSL/TLS
    /// </summary>
    private static IServiceCollection AddCertificateServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure certificate loading from configuration
        services.Configure<CertificateOptions>(configuration.GetSection("Certificates"));

        // Add certificate selector service
        services.AddScoped<ICertificateSelector, CertificateSelector>();

        return services;
    }

    /// <summary>
    /// Configures session security settings
    /// </summary>
    private static IServiceCollection AddSessionSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(configuration.GetValue<int>("Session:TimeoutMinutes", 30));
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Name = configuration.GetValue<string>("Session:CookieName", "__Host-SessionId");
        });

        return services;
    }

    /// <summary>
    /// Configures CORS with security considerations
    /// </summary>
    private static IServiceCollection AddSecureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                
                if (allowedOrigins.Any())
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    // In development, allow localhost
                    var isDevelopment = configuration.GetValue<bool>("FeatureFlags:EnableDetailedErrorMessages", false);
                    if (isDevelopment)
                    {
                        policy.SetIsOriginAllowed(origin => 
                            new Uri(origin).Host == "localhost" || 
                            new Uri(origin).Host == "127.0.0.1");
                    }
                }

                policy.WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }

    /// <summary>
    /// Configures the security pipeline middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityPipeline(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Use forwarded headers (must be early in pipeline)
        app.UseForwardedHeaders();

        // Use security headers middleware
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Use HSTS in production
        var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Force HTTPS redirection
        var requireHttps = configuration.GetValue<bool>("FeatureFlags:RequireHttpsForAllRequests", true);
        if (requireHttps)
        {
            app.UseHttpsRedirection();
        }

        // Use CORS
        var enableCorsRestrictions = configuration.GetValue<bool>("FeatureFlags:EnableCorsRestrictions", true);
        if (enableCorsRestrictions)
        {
            app.UseCors();
        }

        return app;
    }
}

/// <summary>
/// Certificate configuration options
/// </summary>
public class CertificateOptions
{
    public CertificateSource Default { get; set; } = new();
}

public class CertificateSource
{
    public string Source { get; set; } = "Store"; // Store, File, or KeyVault
    public string? StoreLocation { get; set; }
    public string? StoreName { get; set; }
    public string? Subject { get; set; }
    public string? FilePath { get; set; }
    public string? Password { get; set; }
    public string? KeyVaultUrl { get; set; }
    public string? CertificateName { get; set; }
}

/// <summary>
/// Service for selecting SSL certificates
/// </summary>
public interface ICertificateSelector
{
    X509Certificate2? GetCertificate(string? serverName = null);
    Task<X509Certificate2?> GetCertificateAsync(string? serverName = null);
}

public class CertificateSelector : ICertificateSelector
{
    private readonly ILogger<CertificateSelector> _logger;
    private readonly CertificateOptions _options;
    private readonly IKeyVaultService _keyVaultService;

    public CertificateSelector(
        ILogger<CertificateSelector> logger,
        Microsoft.Extensions.Options.IOptions<CertificateOptions> options,
        IKeyVaultService keyVaultService)
    {
        _logger = logger;
        _options = options.Value;
        _keyVaultService = keyVaultService;
    }

    public X509Certificate2? GetCertificate(string? serverName = null)
    {
        return GetCertificateAsync(serverName).GetAwaiter().GetResult();
    }

    public async Task<X509Certificate2?> GetCertificateAsync(string? serverName = null)
    {
        try
        {
            var source = _options.Default;

            return source.Source?.ToLowerInvariant() switch
            {
                "store" => LoadFromStore(source),
                "file" => LoadFromFile(source),
                "keyvault" => await LoadFromKeyVaultAsync(source),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load SSL certificate");
            return null;
        }
    }

    private X509Certificate2? LoadFromStore(CertificateSource source)
    {
        if (string.IsNullOrEmpty(source.Subject))
        {
            _logger.LogWarning("Certificate subject is not specified for store source");
            return null;
        }

        var storeLocation = Enum.TryParse<StoreLocation>(source.StoreLocation, out var loc) ? loc : StoreLocation.CurrentUser;
        var storeName = Enum.TryParse<StoreName>(source.StoreName, out var name) ? name : StoreName.My;

        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, source.Subject, false);
        
        if (certificates.Count == 0)
        {
            _logger.LogWarning("No certificate found with subject '{Subject}'", source.Subject);
            return null;
        }

        // Get the certificate with the latest expiration date
        var certificate = certificates.OrderByDescending(c => c.NotAfter).First();
        
        _logger.LogInformation("Loaded certificate from store: {Subject}, expires {NotAfter}", 
            certificate.Subject, certificate.NotAfter);

        return certificate;
    }

    private X509Certificate2? LoadFromFile(CertificateSource source)
    {
        if (string.IsNullOrEmpty(source.FilePath) || !File.Exists(source.FilePath))
        {
            _logger.LogWarning("Certificate file not found: {FilePath}", source.FilePath);
            return null;
        }

        var certificate = string.IsNullOrEmpty(source.Password) 
            ? new X509Certificate2(source.FilePath)
            : new X509Certificate2(source.FilePath, source.Password);

        _logger.LogInformation("Loaded certificate from file: {Subject}, expires {NotAfter}", 
            certificate.Subject, certificate.NotAfter);

        return certificate;
    }

    private async Task<X509Certificate2?> LoadFromKeyVaultAsync(CertificateSource source)
    {
        if (string.IsNullOrEmpty(source.CertificateName))
        {
            _logger.LogWarning("Certificate name is not specified for KeyVault source");
            return null;
        }

        // This would require additional Azure Key Vault certificate client implementation
        _logger.LogWarning("Key Vault certificate loading not yet implemented");
        await Task.CompletedTask;
        return null;
    }
}
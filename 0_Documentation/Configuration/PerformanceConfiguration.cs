using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;
using GCCWebApp.Services.Performance;
using System.IO.Compression;

namespace GCCWebApp.Configuration;

/// <summary>
/// Configuration for performance optimization features
/// </summary>
public static class PerformanceConfiguration
{
    /// <summary>
    /// Configure response compression and bundling
    /// </summary>
    public static IServiceCollection AddPerformanceOptimization(this IServiceCollection services, IConfiguration configuration)
    {
        // Response compression
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/javascript",
                "text/css",
                "text/html",
                "text/plain",
                "text/xml",
                "application/xml",
                "image/svg+xml",
                "application/font-woff",
                "application/font-woff2"
            });
        });

        // Configure compression levels
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // Memory caching
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = configuration.GetValue<long>("Performance:MemoryCache:SizeLimit", 100_000_000); // 100MB default
            options.CompactionPercentage = configuration.GetValue<double>("Performance:MemoryCache:CompactionPercentage", 0.25);
        });

        // Distributed caching (in-memory for now, can be extended to Redis)
        services.AddDistributedMemoryCache();

        // Performance services
        services.AddScoped<IResponseCachingService, ResponseCachingService>();
        services.AddScoped<IDatabaseOptimizationService, DatabaseOptimizationService>();

        // Output caching
        services.AddOutputCache(options =>
        {
            // Default policy
            options.AddBasePolicy(builder => 
                builder.Expire(TimeSpan.FromMinutes(10)));

            // Specific policies for different content types
            options.AddPolicy("StaticFiles", builder =>
                builder.Expire(TimeSpan.FromDays(1))
                       .SetVaryByHeader("Accept-Encoding"));

            options.AddPolicy("CertificatesList", builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                       .SetVaryByQuery("page", "search", "company", "product"));

            options.AddPolicy("CertificateDetails", builder =>
                builder.Expire(TimeSpan.FromMinutes(15))
                       .SetVaryByRouteValue("id", "number"));

            options.AddPolicy("CompanyDetails", builder =>
                builder.Expire(TimeSpan.FromMinutes(30))
                       .SetVaryByRouteValue("name"));

            options.AddPolicy("ProductsList", builder =>
                builder.Expire(TimeSpan.FromMinutes(20))
                       .SetVaryByQuery("category", "company"));

            options.AddPolicy("PartnersList", builder =>
                builder.Expire(TimeSpan.FromHours(1))
                       .SetVaryByQuery("country", "type"));
        });

        return services;
    }

    /// <summary>
    /// Configure the application pipeline for performance optimization
    /// </summary>
    public static IApplicationBuilder UsePerformanceOptimization(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Response compression (early in pipeline)
        app.UseResponseCompression();

        // Output caching (before static files)
        app.UseOutputCache();

        // Static file caching with proper headers
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Cache static files for 1 year
                var headers = ctx.Context.Response.Headers;
                headers.Append("Cache-Control", "public,max-age=31536000,immutable");
                headers.Append("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
            }
        });

        return app;
    }

    /// <summary>
    /// Configure bundling and minification
    /// </summary>
    public static IServiceCollection AddBundlingAndMinification(this IServiceCollection services, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            // In production, we would typically use a build process for bundling
            // This is a simplified in-memory approach for demonstration
            services.AddSingleton<IBundleService, BundleService>();
        }

        return services;
    }
}

/// <summary>
/// Service for bundling and minifying CSS and JavaScript files
/// </summary>
public interface IBundleService
{
    Task<string> GetCssBundleAsync(string bundleName);
    Task<string> GetJsBundleAsync(string bundleName);
    Task InvalidateBundleAsync(string bundleName);
}

public class BundleService : IBundleService
{
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BundleService> _logger;
    private readonly Dictionary<string, string[]> _cssBundles;
    private readonly Dictionary<string, string[]> _jsBundles;

    public BundleService(IMemoryCache cache, IWebHostEnvironment env, ILogger<BundleService> logger)
    {
        _cache = cache;
        _env = env;
        _logger = logger;

        // Define bundle configurations
        _cssBundles = new Dictionary<string, string[]>
        {
            ["main"] = new[] { "css/main.css", "css/certificates.css" },
            ["admin"] = new[] { "css/main.css", "css/admin.css" },
            ["external"] = new[] { "css/main.css", "css/certificatesexternal.css" }
        };

        _jsBundles = new Dictionary<string, string[]>
        {
            ["main"] = new[] { "js/main.js", "js/certificates.js" },
            ["admin"] = new[] { "js/main.js", "js/admin.js" },
            ["external"] = new[] { "js/main.js", "js/certificatesexternal.js" }
        };
    }

    public async Task<string> GetCssBundleAsync(string bundleName)
    {
        var cacheKey = $"css_bundle_{bundleName}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedBundle) && cachedBundle != null)
        {
            return cachedBundle;
        }

        if (!_cssBundles.TryGetValue(bundleName, out var files))
        {
            _logger.LogWarning("CSS bundle '{BundleName}' not found", bundleName);
            return string.Empty;
        }

        var bundledContent = await BundleCssFilesAsync(files);
        
        // Cache for 1 hour
        _cache.Set(cacheKey, bundledContent, TimeSpan.FromHours(1));
        
        return bundledContent;
    }

    public async Task<string> GetJsBundleAsync(string bundleName)
    {
        var cacheKey = $"js_bundle_{bundleName}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedBundle) && cachedBundle != null)
        {
            return cachedBundle;
        }

        if (!_jsBundles.TryGetValue(bundleName, out var files))
        {
            _logger.LogWarning("JS bundle '{BundleName}' not found", bundleName);
            return string.Empty;
        }

        var bundledContent = await BundleJsFilesAsync(files);
        
        // Cache for 1 hour
        _cache.Set(cacheKey, bundledContent, TimeSpan.FromHours(1));
        
        return bundledContent;
    }

    public Task InvalidateBundleAsync(string bundleName)
    {
        _cache.Remove($"css_bundle_{bundleName}");
        _cache.Remove($"js_bundle_{bundleName}");
        _logger.LogInformation("Invalidated bundle cache for: {BundleName}", bundleName);
        return Task.CompletedTask;
    }

    private async Task<string> BundleCssFilesAsync(string[] files)
    {
        var bundledContent = new System.Text.StringBuilder();

        foreach (var file in files)
        {
            var filePath = Path.Combine(_env.WebRootPath, file);
            
            if (File.Exists(filePath))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var minifiedContent = MinifyCss(content);
                    bundledContent.AppendLine(minifiedContent);
                    
                    _logger.LogDebug("Bundled CSS file: {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading CSS file: {File}", file);
                }
            }
            else
            {
                _logger.LogWarning("CSS file not found: {File}", file);
            }
        }

        return bundledContent.ToString();
    }

    private async Task<string> BundleJsFilesAsync(string[] files)
    {
        var bundledContent = new System.Text.StringBuilder();

        foreach (var file in files)
        {
            var filePath = Path.Combine(_env.WebRootPath, file);
            
            if (File.Exists(filePath))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var minifiedContent = MinifyJs(content);
                    bundledContent.AppendLine(minifiedContent);
                    
                    _logger.LogDebug("Bundled JS file: {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading JS file: {File}", file);
                }
            }
            else
            {
                _logger.LogWarning("JS file not found: {File}", file);
            }
        }

        return bundledContent.ToString();
    }

    private string MinifyCss(string css)
    {
        // Basic CSS minification - remove comments, whitespace, etc.
        // In production, you'd use a proper minification library
        return css
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Replace("  ", " ")
            .Replace(" {", "{")
            .Replace("{ ", "{")
            .Replace(" }", "}")
            .Replace("} ", "}")
            .Replace("; ", ";")
            .Replace(": ", ":")
            .Trim();
    }

    private string MinifyJs(string js)
    {
        // Basic JS minification - remove comments and excess whitespace
        // In production, you'd use a proper minification library like UglifyJS
        return js
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\t", " ")
            .Replace("  ", " ")
            .Trim();
    }
}
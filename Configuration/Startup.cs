using GreenCircleCertified.Models;
using GreenCircleCertified.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static GreenCircleCertified.Services.Interfaces;

namespace GCCWebApp.Configuration
{
    /// <summary>
    /// Startup configuration for the GCC Web Application
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configure services for dependency injection
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services
            services.AddControllersWithViews();
            services.AddMemoryCache();
            services.AddLogging();

            // Add application settings
            var certificateSettings = new CertificateListSettings();
            Configuration.GetSection("CertificateSettings").Bind(certificateSettings);
            services.AddSingleton(certificateSettings);

            // Register application services
            RegisterApplicationServices(services);

            // Add authentication and authorization if needed
            // services.AddAuthentication()...
            // services.AddAuthorization()...

            // Add health checks
            services.AddHealthChecks();

            // Add API versioning if needed
            // services.AddApiVersioning();

            // Add Swagger for API documentation
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "GreenCircle Certified API",
                    Version = "v1",
                    Description = "API for GreenCircle Certified Database"
                });
            });
        }

        /// <summary>
        /// Register application-specific services
        /// </summary>
        private void RegisterApplicationServices(IServiceCollection services)
        {
            // Core services
            services.AddScoped<ICertificateService, EnhancedCertificateService>();
            services.AddScoped<IFilterService, FilterService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();

            // Additional services can be added here
            // services.AddScoped<IReportService, ReportService>();
            // services.AddScoped<INotificationService, NotificationService>();
        }

        /// <summary>
        /// Configure the HTTP request pipeline
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            logger.LogInformation("Configuring application pipeline for environment: {Environment}", env.EnvironmentName);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GreenCircle Certified API v1"));
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Add authentication and authorization middleware if configured
            // app.UseAuthentication();
            // app.UseAuthorization();

            // Add health check endpoint
            app.UseHealthChecks("/health");

            // Configure endpoints
            app.UseEndpoints(endpoints =>
            {
                // Certificate routes
                endpoints.MapControllerRoute(
                    name: "certificate-list",
                    pattern: "certificates",
                    defaults: new { controller = "Certificate", action = "Index" });

                endpoints.MapControllerRoute(
                    name: "certificate-details",
                    pattern: "certificates/{id:int}",
                    defaults: new { controller = "Certificate", action = "DetailsAdvanced" });

                endpoints.MapControllerRoute(
                    name: "certificate-download",
                    pattern: "certificates/{id:int}/download",
                    defaults: new { controller = "Certificate", action = "Download" });

                // Default route
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // API routes
                endpoints.MapControllers();
            });

            logger.LogInformation("Application pipeline configuration completed");
        }
    }

    /// <summary>
    /// Extension methods for service registration
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Add GreenCircle Certified services to the DI container
        /// </summary>
        public static IServiceCollection AddGreenCircleServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Load settings from configuration
            var settings = new CertificateListSettings();
            configuration.GetSection("CertificateSettings").Bind(settings);
            services.AddSingleton(settings);

            // Register core services
            services.AddScoped<ICertificateService, EnhancedCertificateService>();
            services.AddScoped<IFilterService, FilterService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }

        /// <summary>
        /// Add caching services with Redis support (optional)
        /// </summary>
        public static IServiceCollection AddGreenCircleCaching(this IServiceCollection services, IConfiguration configuration)
        {
            var useRedis = configuration.GetValue<bool>("Caching:UseRedis");

            if (useRedis)
            {
                var connectionString = configuration.GetConnectionString("Redis");
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = connectionString;
                });
                // services.AddScoped<ICacheService, RedisCacheService>();
            }
            else
            {
                services.AddMemoryCache();
                services.AddScoped<ICacheService, CacheService>();
            }

            return services;
        }

        /// <summary>
        /// Add email services with different providers
        /// </summary>
        public static IServiceCollection AddGreenCircleEmail(this IServiceCollection services, IConfiguration configuration)
        {
            var emailProvider = configuration.GetValue<string>("Email:Provider");

            switch (emailProvider?.ToLower())
            {
                case "sendgrid":
                    // services.AddScoped<IEmailService, SendGridEmailService>();
                    break;
                case "smtp":
                    // services.AddScoped<IEmailService, SmtpEmailService>();
                    break;
                default:
                    services.AddScoped<IEmailService, EmailService>(); // Mock service
                    break;
            }

            return services;
        }
    }

    /// <summary>
    /// Program class for .NET 6+ minimal hosting model
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app, app.Environment);

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add framework services
            services.AddControllersWithViews();
            services.AddRazorPages();

            // Add GreenCircle services
            services.AddGreenCircleServices(configuration);
            services.AddGreenCircleCaching(configuration);
            services.AddGreenCircleEmail(configuration);

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                if (configuration.GetValue<bool>("Logging:EnableFile"))
                {
                    // Add file logging if configured
                    // builder.AddFile(configuration.GetSection("Logging:File"));
                }
            });

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<CertificateHealthCheck>("certificate-service");

            // Add API documentation
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "GreenCircle Certified API",
                    Version = "v1",
                    Description = "API for managing and browsing certified sustainable products",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "GreenCircle Certified",
                        Email = "support@greencirclecertified.com",
                        Url = new Uri("https://www.greencirclecertified.com")
                    }
                });

                // Include XML comments if available
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });
        }

        private static void ConfigurePipeline(WebApplication app, IWebHostEnvironment env)
        {
            // Configure the HTTP request pipeline
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GreenCircle Certified API v1");
                    c.RoutePrefix = "api-docs";
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Add authentication and authorization if configured
            // app.UseAuthentication();
            // app.UseAuthorization();

            // Add health checks
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/ready");

            // Map controller routes
            app.MapControllerRoute(
                name: "certificate-api",
                pattern: "api/certificates/{action=Index}",
                defaults: new { controller = "Certificate" });

            app.MapControllerRoute(
                name: "certificate-public",
                pattern: "certificates",
                defaults: new { controller = "Certificate", action = "Index" });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();
        }
    }

    /// <summary>
    /// Health check for certificate service
    /// </summary>
    public class CertificateHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly ICertificateService _certificateService;
        private readonly ILogger<CertificateHealthCheck> _logger;

        public CertificateHealthCheck(ICertificateService certificateService, ILogger<CertificateHealthCheck> logger)
        {
            _certificateService = certificateService;
            _logger = logger;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Test certificate service by getting statistics
                var stats = await _certificateService.GetCertificateStatisticsAsync(UserRole.Public);

                if (stats != null && stats.TotalCertificates >= 0)
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                        $"Certificate service is healthy. Total certificates: {stats.TotalCertificates}");
                }

                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    "Certificate service returned invalid statistics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for certificate service");
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Certificate service is unhealthy", ex);
            }
        }
    }
}

// Configuration example for appsettings.json
/*
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GreenCircleCertified;Trusted_Connection=true;MultipleActiveResultSets=true",
    "Redis": "localhost:6379"
  },
  "CertificateSettings": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100,
    "AllowedPageSizes": [10, 20, 50, 100],
    "DefaultSort": "default",
    "EnablePublicAccess": true,
    "EnableDownloads": true,
    "EnableSearch": true,
    "EnableFilters": true,
    "CacheExpirationMinutes": 15,
    "EnableAuditLogging": true,
    "EnableEmailNotifications": false
  },
  "Caching": {
    "UseRedis": false,
    "DefaultExpirationMinutes": 30
  },
  "Email": {
    "Provider": "mock",
    "SendGridApiKey": "",
    "SmtpHost": "",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@greencirclecertified.com",
    "FromName": "GreenCircle Certified"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "GreenCircleCertified": "Debug"
    },
    "EnableFile": false,
    "File": {
      "Path": "logs/gcc-{Date}.txt",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  },
  "AllowedHosts": "*"
}
*/
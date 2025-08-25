using GCCWebApp.Services.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GCCWebApp.Extensions
{
    /// <summary>
    /// Extensions for configuring secure configuration management
    /// </summary>
    public static class SecureConfigurationExtensions
    {
        /// <summary>
        /// Adds secure configuration management with Azure Key Vault support
        /// </summary>
        public static IServiceCollection AddSecureConfiguration(
            this IServiceCollection services, 
            IConfiguration configuration, 
            IWebHostEnvironment environment)
        {
            // Register the secure configuration service
            services.AddSingleton<ISecureConfigurationService, SecureConfigurationService>();

            // Add health check for configuration validation
            services.AddHealthChecks()
                .AddCheck<SecureConfigurationHealthCheck>("secure-configuration");

            return services;
        }

        /// <summary>
        /// Configures Azure Key Vault integration for the configuration builder
        /// Note: Requires Azure.Extensions.AspNetCore.Configuration.Secrets NuGet package
        /// </summary>
        public static IConfigurationBuilder AddAzureKeyVaultIfAvailable(
            this IConfigurationBuilder builder,
            IWebHostEnvironment environment)
        {
            Console.WriteLine("🔐 Azure Key Vault integration available but requires additional NuGet packages");
            Console.WriteLine("   Install: Azure.Extensions.AspNetCore.Configuration.Secrets");
            Console.WriteLine("   Install: Azure.Identity");
            Console.WriteLine("   See: Documentation/06 Training/Secure_Configuration_Guide.md");
            
            return builder;
        }

        /// <summary>
        /// Validates secure configuration during application startup
        /// </summary>
        public static async Task<IServiceProvider> ValidateSecureConfigurationAsync(this IServiceProvider serviceProvider)
        {
            var secureConfig = serviceProvider.GetRequiredService<ISecureConfigurationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<SecureConfigurationService>>();

            try
            {
                logger.LogInformation("🔍 Starting secure configuration validation...");
                
                // Log current configuration status
                secureConfig.LogConfigurationStatus();
                
                // Perform validation
                var isValid = await secureConfig.ValidateConfigurationAsync();
                
                if (isValid)
                {
                    logger.LogInformation("✅ Secure configuration validation passed");
                }
                else
                {
                    logger.LogWarning("⚠️  Secure configuration validation completed with warnings");
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "🚨 CRITICAL: Secure configuration validation failed");
                throw;
            }

            return serviceProvider;
        }
    }

    /// <summary>
    /// Placeholder for Key Vault secret manager - requires Azure packages to implement
    /// </summary>
    public class PrefixKeyVaultSecretManager
    {
        public PrefixKeyVaultSecretManager(string prefix)
        {
            // Placeholder implementation
        }
    }

    /// <summary>
    /// Health check for secure configuration validation
    /// </summary>
    public class SecureConfigurationHealthCheck : IHealthCheck
    {
        private readonly ISecureConfigurationService _secureConfigService;
        private readonly ILogger<SecureConfigurationHealthCheck> _logger;

        public SecureConfigurationHealthCheck(
            ISecureConfigurationService secureConfigService,
            ILogger<SecureConfigurationHealthCheck> logger)
        {
            _secureConfigService = secureConfigService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isValid = await _secureConfigService.ValidateConfigurationAsync();
                
                if (isValid)
                {
                    return HealthCheckResult.Healthy("All required secrets are properly configured");
                }
                
                // In development, warnings are acceptable
                if (!_secureConfigService.IsProductionEnvironment)
                {
                    return HealthCheckResult.Degraded("Some secrets are using placeholders in development environment");
                }
                
                return HealthCheckResult.Unhealthy("Required secrets are missing or misconfigured in production");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for secure configuration");
                return HealthCheckResult.Unhealthy("Configuration validation failed", ex);
            }
        }
    }

    /// <summary>
    /// Startup validation service to ensure configuration is secure before accepting requests
    /// </summary>
    public class ConfigurationValidationService : IHostedService
    {
        private readonly ISecureConfigurationService _secureConfigService;
        private readonly ILogger<ConfigurationValidationService> _logger;

        public ConfigurationValidationService(
            ISecureConfigurationService secureConfigService,
            ILogger<ConfigurationValidationService> logger)
        {
            _secureConfigService = secureConfigService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔐 Starting configuration validation service...");

            try
            {
                await _secureConfigService.ValidateConfigurationAsync();
                _logger.LogInformation("✅ Configuration validation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "🚨 STARTUP FAILURE: Configuration validation failed");
                
                // In production, configuration failures should prevent startup
                if (_secureConfigService.IsProductionEnvironment)
                {
                    throw new InvalidOperationException("Application cannot start with invalid configuration in production", ex);
                }
                
                _logger.LogWarning("Allowing startup in non-production environment despite configuration issues");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
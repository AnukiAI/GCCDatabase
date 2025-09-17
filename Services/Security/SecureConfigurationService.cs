using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace GCCWebApp.Services.Security
{
    /// <summary>
    /// Service for secure configuration management with support for Azure Key Vault,
    /// environment variables, and development fallbacks
    /// </summary>
    public interface ISecureConfigurationService
    {
        string GetConnectionString(string name);
        string GetSecret(string key);
        bool IsProductionEnvironment { get; }
        Task<bool> ValidateConfigurationAsync();
        void LogConfigurationStatus();
    }

    public class SecureConfigurationService : ISecureConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SecureConfigurationService> _logger;
        private readonly Dictionary<string, string> _requiredSecrets;
        private readonly Dictionary<string, string> _optionalSecrets;
        private readonly Dictionary<string, string> _environmentMappings;

        public bool IsProductionEnvironment => _environment.IsProduction();

        public SecureConfigurationService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<SecureConfigurationService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            
            _requiredSecrets = new Dictionary<string, string>
            {
                ["JWT_SECRET_KEY"] = "Jwt:Key",
                ["DATABASE_CONNECTION_STRING"] = "ConnectionStrings:DefaultConnection"
            };

            // Optional secrets - only validated if features are enabled
            _optionalSecrets = new Dictionary<string, string>
            {
                ["AZURE_AD_B2C_CLIENT_SECRET"] = "AzureAdB2C:ClientSecret",
                ["AZURE_AD_CLIENT_SECRET"] = "AzureAd:ClientSecret",
                ["AZURE_BLOB_CONNECTION_STRING"] = "DataProtection:AzureBlobStorageConnectionString",
                ["APPLICATION_INSIGHTS_CONNECTION_STRING"] = "ApplicationInsights:ConnectionString",
                ["SMTP_PASSWORD"] = "EmailConfiguration:Password"
            };

            _environmentMappings = new Dictionary<string, string>
            {
                ["{JWT_SECRET_KEY}"] = "GCC_JWT_SECRET_KEY",
                ["{JWT_SECRET_KEY_256_BITS_MINIMUM}"] = "GCC_JWT_SECRET_KEY",
                ["{AZURE_AD_B2C_CLIENT_SECRET}"] = "GCC_AZURE_AD_B2C_CLIENT_SECRET",
                ["{AZURE_AD_CLIENT_SECRET}"] = "GCC_AZURE_AD_CLIENT_SECRET",
                ["{PRODUCTION_DATABASE_CONNECTION_STRING}"] = "GCC_DATABASE_CONNECTION_STRING",
                ["{PRODUCTION_SQLITE_CONNECTION_STRING}"] = "GCC_SQLITE_CONNECTION_STRING",
                ["{AZURE_BLOB_CONNECTION_STRING}"] = "GCC_AZURE_BLOB_CONNECTION_STRING",
                ["{APPLICATION_INSIGHTS_CONNECTION_STRING}"] = "GCC_APPLICATION_INSIGHTS_CONNECTION_STRING",
                ["{SMTP_PASSWORD}"] = "GCC_SMTP_PASSWORD",
                ["{client-secret}"] = "GCC_AZURE_AD_B2C_CLIENT_SECRET",
                ["{client-id}"] = "GCC_AZURE_AD_B2C_CLIENT_ID",
                ["{tenant-id}"] = "GCC_AZURE_AD_B2C_TENANT_ID",
                ["{tenant-name}"] = "GCC_AZURE_AD_B2C_TENANT_NAME"
            };
        }

        public string GetConnectionString(string name)
        {
            var connectionString = _configuration.GetConnectionString(name);
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string '{ConnectionStringName}' not found", name);
                throw new InvalidOperationException($"Connection string '{name}' is required but not configured");
            }

            // Check if it's a placeholder and resolve it
            if (IsPlaceholder(connectionString))
            {
                connectionString = ResolvePlaceholder(connectionString, $"Connection string for {name}");
            }

            // Validate connection string doesn't contain sensitive info in logs
            var sanitizedConnectionString = SanitizeConnectionString(connectionString);
            _logger.LogDebug("Retrieved connection string for {ConnectionStringName}: {SanitizedConnectionString}", 
                name, sanitizedConnectionString);

            return connectionString;
        }

        public string GetSecret(string key)
        {
            var value = _configuration[key];
            
            if (string.IsNullOrEmpty(value))
            {
                _logger.LogError("Configuration key '{ConfigKey}' not found", key);
                throw new InvalidOperationException($"Configuration key '{key}' is required but not configured");
            }

            // Check if it's a placeholder and resolve it
            if (IsPlaceholder(value))
            {
                value = ResolvePlaceholder(value, key);
            }

            // Validate secret strength for JWT keys
            if (key.Contains("Jwt:Key", StringComparison.OrdinalIgnoreCase))
            {
                ValidateJwtKeyStrength(value);
            }

            _logger.LogDebug("Retrieved secret for key: {ConfigKey}", key);
            return value;
        }

        public async Task<bool> ValidateConfigurationAsync()
        {
            var isValid = true;
            var missingSecrets = new List<string>();

            _logger.LogInformation("Starting configuration validation...");

            foreach (var requiredSecret in _requiredSecrets)
            {
                try
                {
                    var value = GetSecret(requiredSecret.Value);
                    
                    if (IsPlaceholder(value))
                    {
                        missingSecrets.Add(requiredSecret.Key);
                        isValid = false;
                        _logger.LogError("Required secret '{SecretKey}' contains placeholder: {Value}", 
                            requiredSecret.Key, value);
                    }
                    else
                    {
                        _logger.LogDebug("✓ Secret '{SecretKey}' is properly configured", requiredSecret.Key);
                    }
                }
                catch (Exception ex)
                {
                    missingSecrets.Add(requiredSecret.Key);
                    isValid = false;
                    _logger.LogError(ex, "Failed to retrieve required secret '{SecretKey}'", requiredSecret.Key);
                }
            }

            // Check optional secrets (warn but don't fail)
            var missingOptionalSecrets = new List<string>();
            foreach (var optionalSecret in _optionalSecrets)
            {
                try
                {
                    var value = GetSecret(optionalSecret.Value);

                    if (IsPlaceholder(value))
                    {
                        missingOptionalSecrets.Add(optionalSecret.Key);
                        _logger.LogWarning("Optional secret '{SecretKey}' contains placeholder: {Value}",
                            optionalSecret.Key, value);
                    }
                    else
                    {
                        _logger.LogDebug("✓ Optional secret '{SecretKey}' is properly configured", optionalSecret.Key);
                    }
                }
                catch (Exception)
                {
                    missingOptionalSecrets.Add(optionalSecret.Key);
                    _logger.LogWarning("Optional secret '{SecretKey}' is not configured (this is OK)", optionalSecret.Key);
                }
            }

            if (missingOptionalSecrets.Any())
            {
                _logger.LogInformation("ℹ️  Optional services not configured: {MissingOptionalSecrets}",
                    string.Join(", ", missingOptionalSecrets));
            }

            if (!isValid && IsProductionEnvironment)
            {
                _logger.LogCritical("⚠️  CRITICAL: Missing required secrets in production: {MissingSecrets}", 
                    string.Join(", ", missingSecrets));
                
                // In production, this should cause the application to fail to start
                throw new InvalidOperationException(
                    $"Production deployment missing critical secrets: {string.Join(", ", missingSecrets)}");
            }
            else if (!isValid)
            {
                _logger.LogWarning("⚠️  Missing secrets in development environment: {MissingSecrets}", 
                    string.Join(", ", missingSecrets));
            }
            else
            {
                _logger.LogInformation("✓ All required secrets are properly configured");
            }

            await Task.CompletedTask;
            return isValid;
        }

        public void LogConfigurationStatus()
        {
            _logger.LogInformation("=== Configuration Security Status ===");
            _logger.LogInformation("Environment: {Environment}", _environment.EnvironmentName);
            _logger.LogInformation("Production Mode: {IsProduction}", IsProductionEnvironment);
            
            var secretsWithPlaceholders = 0;
            var totalSecrets = _requiredSecrets.Count;

            foreach (var secret in _requiredSecrets)
            {
                try
                {
                    var value = _configuration[secret.Value];
                    if (IsPlaceholder(value))
                    {
                        secretsWithPlaceholders++;
                        _logger.LogWarning("⚠️  Placeholder detected for: {SecretKey}", secret.Key);
                    }
                    else
                    {
                        _logger.LogInformation("✓ Configured: {SecretKey}", secret.Key);
                    }
                }
                catch
                {
                    secretsWithPlaceholders++;
                    _logger.LogError("✗ Missing: {SecretKey}", secret.Key);
                }
            }

            _logger.LogInformation("Secrets Status: {Configured}/{Total} configured properly", 
                totalSecrets - secretsWithPlaceholders, totalSecrets);
            
            if (secretsWithPlaceholders > 0 && IsProductionEnvironment)
            {
                _logger.LogCritical("🚨 SECURITY ALERT: {Count} secrets using placeholders in PRODUCTION!", 
                    secretsWithPlaceholders);
            }
            
            _logger.LogInformation("=====================================");
        }

        private bool IsPlaceholder(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Check for common placeholder patterns
            return value.StartsWith('{') && value.EndsWith('}') ||
                   value.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("TODO", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                   value.Length < 10; // Assume very short values are placeholders for secrets
        }

        private string ResolvePlaceholder(string placeholder, string context)
        {
            _logger.LogDebug("Resolving placeholder for: {Context}", context);

            // Try to resolve from environment mappings first
            if (_environmentMappings.TryGetValue(placeholder, out var envVarName))
            {
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                if (!string.IsNullOrEmpty(envValue))
                {
                    _logger.LogInformation("✓ Resolved placeholder from environment variable: {EnvVar}", envVarName);
                    return envValue;
                }
                else
                {
                    _logger.LogWarning("Environment variable '{EnvVar}' not found for placeholder: {Placeholder}", 
                        envVarName, placeholder);
                }
            }

            // Try Azure Key Vault if available (this would require additional setup)
            // var keyVaultValue = await ResolveFromKeyVaultAsync(placeholder);
            // if (keyVaultValue != null) return keyVaultValue;

            // Development fallbacks for specific secrets
            if (_environment.IsDevelopment())
            {
                return GenerateDevelopmentFallback(placeholder, context);
            }

            _logger.LogError("Unable to resolve placeholder: {Placeholder} for context: {Context}", 
                placeholder, context);
            throw new InvalidOperationException(
                $"Unable to resolve configuration placeholder '{placeholder}' for {context}");
        }

        private string GenerateDevelopmentFallback(string placeholder, string context)
        {
            _logger.LogWarning("🔧 Generating development fallback for: {Placeholder}", placeholder);

            return placeholder.ToUpperInvariant() switch
            {
                "{JWT_SECRET_KEY}" or "{JWT_SECRET_KEY_256_BITS_MINIMUM}" => 
                    "DEV_JWT_SECRET_KEY_THIS_IS_FOR_DEVELOPMENT_ONLY_AND_SHOULD_NOT_BE_USED_IN_PRODUCTION_ENVIRONMENT_123456789",
                "{AZURE_AD_B2C_CLIENT_SECRET}" or "{AZURE_AD_CLIENT_SECRET}" or "{CLIENT-SECRET}" => 
                    "dev-client-secret-placeholder-for-development-only",
                "{CLIENT-ID}" => "dev-client-id-placeholder",
                "{TENANT-ID}" => "dev-tenant-id-placeholder", 
                "{TENANT-NAME}" => "dev-tenant",
                _ when context.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) => 
                    "Data Source=gcc_database_dev.db",
                _ => placeholder // Keep original placeholder if no fallback available
            };
        }

        private void ValidateJwtKeyStrength(string jwtKey)
        {
            if (jwtKey.Length < 64)
            {
                var message = $"JWT key must be at least 64 characters long for security. Current length: {jwtKey.Length}";
                _logger.LogError("🔒 JWT Key Security Issue: {Message}", message);
                
                if (IsProductionEnvironment)
                {
                    throw new InvalidOperationException(message);
                }
                else
                {
                    _logger.LogWarning("JWT key length insufficient for production use");
                }
            }
            else
            {
                _logger.LogDebug("✓ JWT key meets minimum length requirements");
            }
        }

        private string SanitizeConnectionString(string connectionString)
        {
            // Remove sensitive information from connection strings for logging
            var patterns = new[]
            {
                @"Password=([^;]+)",
                @"pwd=([^;]+)",
                @"User Id=([^;]+)",
                @"uid=([^;]+)"
            };

            var sanitized = connectionString;
            foreach (var pattern in patterns)
            {
                sanitized = Regex.Replace(sanitized, pattern, m => 
                    $"{m.Value.Split('=')[0]}=***", RegexOptions.IgnoreCase);
            }

            return sanitized;
        }
    }
}
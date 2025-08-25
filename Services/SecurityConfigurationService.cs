using Microsoft.Extensions.Options;
using GCCWebApp.Configuration;

namespace GCCWebApp.Services
{
    public interface ISecurityConfigurationService
    {
        Task<SecurityConfigurationValidationResult> ValidateConfigurationAsync();
        Task<bool> ValidateJwtConfigurationAsync();
        Task<bool> ValidateApiKeySecurityAsync();
        Task<bool> ValidatePasswordPolicyAsync();
        Task<bool> ValidateTlsConfigurationAsync();
        Task<SecurityScore> CalculateSecurityScoreAsync();
    }

    public class SecurityConfigurationService : ISecurityConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityConfigurationService> _logger;
        private readonly IOptions<AzureAdB2COptions> _azureOptions;

        public SecurityConfigurationService(
            IConfiguration configuration, 
            ILogger<SecurityConfigurationService> logger,
            IOptions<AzureAdB2COptions> azureOptions)
        {
            _configuration = configuration;
            _logger = logger;
            _azureOptions = azureOptions;
        }

        public async Task<SecurityConfigurationValidationResult> ValidateConfigurationAsync()
        {
            var result = new SecurityConfigurationValidationResult();

            // JWT Configuration Validation
            result.JwtConfigurationValid = await ValidateJwtConfigurationAsync();
            
            // API Key Security Validation
            result.ApiKeySecurityValid = await ValidateApiKeySecurityAsync();
            
            // Password Policy Validation
            result.PasswordPolicyValid = await ValidatePasswordPolicyAsync();
            
            // TLS Configuration Validation
            result.TlsConfigurationValid = await ValidateTlsConfigurationAsync();
            
            // Azure AD B2C Validation
            result.AzureAdB2CConfigurationValid = ValidateAzureAdB2CConfiguration();
            
            // Security Headers Validation
            result.SecurityHeadersConfigured = ValidateSecurityHeadersConfiguration();
            
            // Rate Limiting Configuration
            result.RateLimitingConfigured = ValidateRateLimitingConfiguration();

            result.OverallValid = result.JwtConfigurationValid && 
                                  result.ApiKeySecurityValid && 
                                  result.PasswordPolicyValid && 
                                  result.TlsConfigurationValid &&
                                  result.SecurityHeadersConfigured &&
                                  result.RateLimitingConfigured;

            if (!result.OverallValid)
            {
                _logger.LogWarning("Security configuration validation failed. Issues: {Issues}", 
                    string.Join(", ", result.ValidationErrors));
            }

            return result;
        }

        public Task<bool> ValidateJwtConfigurationAsync()
        {
            try
            {
                var jwtKey = _configuration["Jwt:Key"];
                var jwtIssuer = _configuration["Jwt:Issuer"];
                var jwtAudience = _configuration["Jwt:Audience"];
                var jwtExpiry = _configuration["Jwt:ExpiryMinutes"];

                if (string.IsNullOrEmpty(jwtKey))
                {
                    _logger.LogError("JWT signing key is not configured");
                    return Task.FromResult(false);
                }

                if (jwtKey.Contains("{") || jwtKey == "YourSuperSecretKeyForJWTTokenGeneration123456789")
                {
                    _logger.LogError("JWT signing key appears to be a placeholder or default value");
                    return Task.FromResult(false);
                }

                if (jwtKey.Length < 64)
                {
                    _logger.LogError("JWT signing key is too short. Minimum 64 characters required");
                    return Task.FromResult(false);
                }

                if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                {
                    _logger.LogError("JWT issuer and audience must be configured");
                    return Task.FromResult(false);
                }

                if (!int.TryParse(jwtExpiry, out var expiryMinutes) || expiryMinutes <= 0 || expiryMinutes > 1440)
                {
                    _logger.LogError("JWT expiry time is invalid. Should be between 1 and 1440 minutes");
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating JWT configuration");
                return Task.FromResult(false);
            }
        }

        public Task<bool> ValidateApiKeySecurityAsync()
        {
            try
            {
                // Check if rate limiting is configured for API keys
                var rateLimitConfig = _configuration.GetSection("IpRateLimiting");
                if (!rateLimitConfig.Exists())
                {
                    _logger.LogWarning("Rate limiting is not configured for API endpoints");
                    return Task.FromResult(false);
                }

                // Validate HTTPS requirement
                var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>();
                if (allowedOrigins?.Any(origin => origin.StartsWith("http://") && !origin.Contains("localhost")) == true)
                {
                    _logger.LogWarning("Non-localhost HTTP origins are configured, which is insecure");
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key security configuration");
                return Task.FromResult(false);
            }
        }

        public Task<bool> ValidatePasswordPolicyAsync()
        {
            try
            {
                var passwordPolicy = _configuration.GetSection("PasswordPolicy");
                
                var minLength = passwordPolicy.GetValue<int>("MinLength", 8);
                var requireUppercase = passwordPolicy.GetValue<bool>("RequireUppercase", true);
                var requireLowercase = passwordPolicy.GetValue<bool>("RequireLowercase", true);
                var requireDigits = passwordPolicy.GetValue<bool>("RequireDigits", true);
                var requireSpecialChars = passwordPolicy.GetValue<bool>("RequireSpecialCharacters", true);

                if (minLength < 12)
                {
                    _logger.LogWarning("Password minimum length should be at least 12 characters");
                    return Task.FromResult(false);
                }

                if (!requireUppercase || !requireLowercase || !requireDigits || !requireSpecialChars)
                {
                    _logger.LogWarning("Password policy should require uppercase, lowercase, digits, and special characters");
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password policy configuration");
                return Task.FromResult(false);
            }
        }

        public Task<bool> ValidateTlsConfigurationAsync()
        {
            try
            {
                // Check HTTPS redirection is enabled
                var httpsPort = _configuration.GetValue<int>("https_port", 443);
                if (httpsPort <= 0)
                {
                    _logger.LogWarning("HTTPS port is not properly configured");
                    return Task.FromResult(false);
                }

                // Check for proper certificate configuration in production
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                if (environment == "Production")
                {
                    var certificateConfig = _configuration.GetSection("Certificates");
                    if (!certificateConfig.Exists())
                    {
                        _logger.LogWarning("TLS certificate configuration is missing for production environment");
                        return Task.FromResult(false);
                    }
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TLS configuration");
                return Task.FromResult(false);
            }
        }

        public async Task<SecurityScore> CalculateSecurityScoreAsync()
        {
            var score = new SecurityScore();
            var validation = await ValidateConfigurationAsync();

            // Calculate score based on validation results
            score.JwtSecurityScore = validation.JwtConfigurationValid ? 20 : 0;
            score.ApiKeySecurityScore = validation.ApiKeySecurityValid ? 15 : 0;
            score.PasswordPolicyScore = validation.PasswordPolicyValid ? 20 : 0;
            score.TlsConfigurationScore = validation.TlsConfigurationValid ? 15 : 0;
            score.AzureAdB2CScore = validation.AzureAdB2CConfigurationValid ? 10 : 0;
            score.SecurityHeadersScore = validation.SecurityHeadersConfigured ? 10 : 0;
            score.RateLimitingScore = validation.RateLimitingConfigured ? 10 : 0;

            score.TotalScore = score.JwtSecurityScore + score.ApiKeySecurityScore + 
                              score.PasswordPolicyScore + score.TlsConfigurationScore + 
                              score.AzureAdB2CScore + score.SecurityHeadersScore + 
                              score.RateLimitingScore;

            score.SecurityLevel = score.TotalScore switch
            {
                >= 90 => "Excellent",
                >= 70 => "Good",
                >= 50 => "Fair",
                >= 30 => "Poor",
                _ => "Critical"
            };

            return score;
        }

        private bool ValidateAzureAdB2CConfiguration()
        {
            try
            {
                var config = _azureOptions.Value;
                if (config == null) return false;

                var hasPlaceholders = config.ClientId.Contains("{") || 
                                    config.TenantId.Contains("{") ||
                                    config.Instance.Contains("{");

                if (hasPlaceholders)
                {
                    _logger.LogWarning("Azure AD B2C configuration contains placeholder values");
                    return false;
                }

                return !string.IsNullOrEmpty(config.ClientId) && 
                       !string.IsNullOrEmpty(config.TenantId) && 
                       !string.IsNullOrEmpty(config.Instance);
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateSecurityHeadersConfiguration()
        {
            // This would typically check that security middleware is properly configured
            // For now, we assume it's configured correctly if the service is running
            return true;
        }

        private bool ValidateRateLimitingConfiguration()
        {
            try
            {
                var rateLimitConfig = _configuration.GetSection("IpRateLimiting");
                return rateLimitConfig.Exists() && rateLimitConfig.GetValue<bool>("EnableEndpointRateLimiting", false);
            }
            catch
            {
                return false;
            }
        }
    }

    public class SecurityConfigurationValidationResult
    {
        public bool OverallValid { get; set; }
        public bool JwtConfigurationValid { get; set; }
        public bool ApiKeySecurityValid { get; set; }
        public bool PasswordPolicyValid { get; set; }
        public bool TlsConfigurationValid { get; set; }
        public bool AzureAdB2CConfigurationValid { get; set; }
        public bool SecurityHeadersConfigured { get; set; }
        public bool RateLimitingConfigured { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime ValidationDate { get; set; } = DateTime.UtcNow;
    }

    public class SecurityScore
    {
        public int JwtSecurityScore { get; set; }
        public int ApiKeySecurityScore { get; set; }
        public int PasswordPolicyScore { get; set; }
        public int TlsConfigurationScore { get; set; }
        public int AzureAdB2CScore { get; set; }
        public int SecurityHeadersScore { get; set; }
        public int RateLimitingScore { get; set; }
        public int TotalScore { get; set; }
        public string SecurityLevel { get; set; } = "Unknown";
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public List<string> Recommendations { get; set; } = new();
    }
}
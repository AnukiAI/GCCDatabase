using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;

namespace GCCWebApp.Services.Security;

/// <summary>
/// Service for managing Azure Key Vault operations and secrets management
/// </summary>
public interface IKeyVaultService
{
    Task<string?> GetSecretAsync(string secretName);
    Task<bool> SetSecretAsync(string secretName, string secretValue);
    Task<bool> DeleteSecretAsync(string secretName);
    Task<bool> IsKeyVaultAvailableAsync();
    Task<Dictionary<string, string>> GetAllSecretsAsync();
    Task RotateJwtSigningKeyAsync();
}

public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient? _secretClient;
    private readonly ILogger<KeyVaultService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly bool _isKeyVaultEnabled;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public KeyVaultService(
        ILogger<KeyVaultService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;

        var keyVaultUrl = configuration["KeyVault:VaultUrl"];
        _isKeyVaultEnabled = !string.IsNullOrEmpty(keyVaultUrl) && 
                           !keyVaultUrl.Contains("{") && 
                           !keyVaultUrl.Equals("https://your-keyvault.vault.azure.net/", StringComparison.OrdinalIgnoreCase);

        if (_isKeyVaultEnabled)
        {
            try
            {
                var credential = new DefaultAzureCredential();
                _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
                _logger.LogInformation("Azure Key Vault client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault client");
                _isKeyVaultEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning("Azure Key Vault is not configured or disabled");
        }
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        if (!_isKeyVaultEnabled || _secretClient == null)
        {
            _logger.LogWarning("Key Vault is not available, falling back to configuration");
            return GetFallbackSecret(secretName);
        }

        var cacheKey = $"keyvault_secret_{secretName}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedSecret))
        {
            return cachedSecret;
        }

        try
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            var secretValue = secret.Value.Value;
            
            // Cache the secret for a short period
            _cache.Set(cacheKey, secretValue, _cacheExpiration);
            
            _logger.LogDebug("Successfully retrieved secret from Key Vault: {SecretName}", secretName);
            return secretValue;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret {SecretName} not found in Key Vault, falling back to configuration", secretName);
            return GetFallbackSecret(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            return GetFallbackSecret(secretName);
        }
    }

    public async Task<bool> SetSecretAsync(string secretName, string secretValue)
    {
        if (!_isKeyVaultEnabled || _secretClient == null)
        {
            _logger.LogWarning("Key Vault is not available, cannot set secret");
            return false;
        }

        try
        {
            await _secretClient.SetSecretAsync(secretName, secretValue);
            
            // Invalidate cache
            var cacheKey = $"keyvault_secret_{secretName}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Successfully set secret in Key Vault: {SecretName}", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret {SecretName} in Key Vault", secretName);
            return false;
        }
    }

    public async Task<bool> DeleteSecretAsync(string secretName)
    {
        if (!_isKeyVaultEnabled || _secretClient == null)
        {
            _logger.LogWarning("Key Vault is not available, cannot delete secret");
            return false;
        }

        try
        {
            await _secretClient.StartDeleteSecretAsync(secretName);
            
            // Invalidate cache
            var cacheKey = $"keyvault_secret_{secretName}";
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("Successfully deleted secret from Key Vault: {SecretName}", secretName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {SecretName} from Key Vault", secretName);
            return false;
        }
    }

    public async Task<bool> IsKeyVaultAvailableAsync()
    {
        if (!_isKeyVaultEnabled || _secretClient == null)
        {
            return false;
        }

        try
        {
            // Try to get a test secret to verify connectivity
            await foreach (var page in _secretClient.GetPropertiesOfSecretsAsync().AsPages())
            {
                // If we can enumerate at least one page, the connection is working
                break;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetAllSecretsAsync()
    {
        var secrets = new Dictionary<string, string>();

        if (!_isKeyVaultEnabled || _secretClient == null)
        {
            _logger.LogWarning("Key Vault is not available");
            return secrets;
        }

        try
        {
            await foreach (var secretProperty in _secretClient.GetPropertiesOfSecretsAsync())
            {
                if (secretProperty.Enabled == true)
                {
                    try
                    {
                        var secret = await _secretClient.GetSecretAsync(secretProperty.Name);
                        secrets[secretProperty.Name] = secret.Value.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to retrieve secret {SecretName}", secretProperty.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secrets from Key Vault");
        }

        return secrets;
    }

    public async Task RotateJwtSigningKeyAsync()
    {
        if (!_isKeyVaultEnabled || _secretClient == null)
        {
            _logger.LogWarning("Key Vault is not available, cannot rotate JWT signing key");
            return;
        }

        try
        {
            // Generate a new secure JWT signing key
            var newKey = GenerateSecureJwtKey();
            
            // Store the new key in Key Vault
            await SetSecretAsync("jwt-signing-key", newKey);
            
            // Log the rotation (but not the key itself)
            _logger.LogInformation("JWT signing key rotated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate JWT signing key");
        }
    }

    private string? GetFallbackSecret(string secretName)
    {
        // Map Key Vault secret names to configuration keys
        var configKey = secretName switch
        {
            "jwt-signing-key" => "Jwt:Key",
            "database-connection-string" => "ConnectionStrings:DefaultConnection",
            "azure-ad-client-secret" => "AzureAdB2C:ClientSecret",
            _ => null
        };

        if (configKey != null)
        {
            return _configuration[configKey];
        }

        return null;
    }

    private static string GenerateSecureJwtKey()
    {
        // Generate a cryptographically secure 256-bit key
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var keyBytes = new byte[64]; // 512 bits for extra security
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }
}
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface IApiKeyService
    {
        Task<ApiKey> CreateApiKeyAsync(string name, string description, string createdBy, DateTime? expiresAt = null);
        Task<ApiKey> ValidateApiKeyAsync(string key);
        Task<bool> RevokeApiKeyAsync(int id);
        Task<IEnumerable<ApiKey>> GetAllApiKeysAsync();
        Task<ApiKey> GetApiKeyByIdAsync(int id);
        Task UpdateLastUsedAsync(string key);
        bool IsValidOrigin(ApiKey apiKey, string origin);
        bool IsValidIP(ApiKey apiKey, string ipAddress);
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(ApplicationDbContext context, ILogger<ApiKeyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiKey> CreateApiKeyAsync(string name, string description, string createdBy, DateTime? expiresAt = null)
        {
            var apiKey = new ApiKey
            {
                Name = name,
                Key = GenerateApiKey(),
                Description = description,
                CreatedBy = createdBy,
                ExpiresAt = expiresAt ?? DateTime.UtcNow.AddYears(1),
                IsActive = true
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"API Key created for {name} by {createdBy}");
            return apiKey;
        }

        public async Task<ApiKey> ValidateApiKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == key && k.IsActive);

            if (apiKey == null)
                return null;

            if (apiKey.IsExpired)
            {
                _logger.LogWarning($"Expired API key used: {apiKey.Name}");
                return null;
            }

            return apiKey;
        }

        public async Task<bool> RevokeApiKeyAsync(int id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null)
                return false;

            apiKey.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"API Key revoked: {apiKey.Name}");
            return true;
        }

        public async Task<IEnumerable<ApiKey>> GetAllApiKeysAsync()
        {
            return await _context.ApiKeys
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<ApiKey> GetApiKeyByIdAsync(int id)
        {
            return await _context.ApiKeys.FindAsync(id);
        }

        public async Task UpdateLastUsedAsync(string key)
        {
            var apiKey = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == key);

            if (apiKey != null)
            {
                apiKey.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public bool IsValidOrigin(ApiKey apiKey, string origin)
        {
            if (string.IsNullOrEmpty(apiKey.AllowedOrigins))
                return true;

            var allowedOrigins = apiKey.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return allowedOrigins.Any(o => o.Trim().Equals(origin, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsValidIP(ApiKey apiKey, string ipAddress)
        {
            if (string.IsNullOrEmpty(apiKey.AllowedIPs))
                return true;

            var allowedIPs = apiKey.AllowedIPs.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return allowedIPs.Any(ip => ip.Trim().Equals(ipAddress, StringComparison.OrdinalIgnoreCase));
        }

        private string GenerateApiKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
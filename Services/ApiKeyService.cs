using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface IApiKeyService
    {
        Task<ApiKey> CreateApiKeyAsync(string name, string description, string createdBy, DateTime? expiresAt = null);
        Task<ApiKey?> ValidateApiKeyAsync(string key);
        Task<bool> RevokeApiKeyAsync(int id);
        Task<IEnumerable<ApiKey>> GetAllApiKeysAsync();
        Task<ApiKey?> GetApiKeyByIdAsync(int id);
        Task UpdateLastUsedAsync(string key);
        bool IsValidOrigin(ApiKey apiKey, string origin);
        bool IsValidIP(ApiKey apiKey, string ipAddress);
        
        // Enhanced security methods
        Task RecordSecurityViolationAsync(int apiKeyId, string violationType, string details);
        Task<bool> IsRateLimitExceededAsync(int apiKeyId, string endpoint);
        Task<bool> BlockApiKeyAsync(int id, TimeSpan duration, string reason);
        Task<bool> UnblockApiKeyAsync(int id);
        Task<Dictionary<string, object>> GetApiKeyUsageStatsAsync(int apiKeyId, DateTime fromDate);
        Task<bool> RotateApiKeyAsync(int id);
        Task<bool> ValidateApiKeyStrengthAsync(string key);
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

        public async Task<ApiKey?> ValidateApiKeyAsync(string key)
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

        public async Task<ApiKey?> GetApiKeyByIdAsync(int id)
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

        public async Task RecordSecurityViolationAsync(int apiKeyId, string violationType, string details)
        {
            var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
            if (apiKey != null)
            {
                apiKey.SecurityViolations++;
                apiKey.LastViolationAt = DateTime.UtcNow;
                apiKey.LastViolationType = violationType;
                
                _logger.LogWarning($"Security violation recorded for API Key {apiKey.Name}: {violationType} - {details}");
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsRateLimitExceededAsync(int apiKeyId, string endpoint)
        {
            var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
            if (apiKey == null) return true;

            // Check if temporarily blocked
            if (apiKey.IsTemporarilyBlocked && apiKey.BlockedUntil > DateTime.UtcNow)
            {
                return true;
            }

            // Reset temporary block if expired
            if (apiKey.IsTemporarilyBlocked && apiKey.BlockedUntil <= DateTime.UtcNow)
            {
                apiKey.IsTemporarilyBlocked = false;
                apiKey.BlockedUntil = null;
                apiKey.BlockReason = null;
                await _context.SaveChangesAsync();
            }

            // Apply enhanced rate limits if security violations detected
            var baseRateLimit = apiKey.RateLimit ?? 100; // Default 100 requests per hour
            if (apiKey.SecurityViolations > 0)
            {
                baseRateLimit = Math.Max(10, baseRateLimit / (apiKey.SecurityViolations * 2));
            }

            // Simple hourly rate limiting (in production, use Redis or similar)
            var hourAgo = DateTime.UtcNow.AddHours(-1);
            // Note: This is a simplified implementation. In production, implement proper distributed rate limiting
            
            return false; // Simplified for now
        }

        public async Task<bool> BlockApiKeyAsync(int id, TimeSpan duration, string reason)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return false;

            apiKey.IsTemporarilyBlocked = true;
            apiKey.BlockedUntil = DateTime.UtcNow.Add(duration);
            apiKey.BlockReason = reason;

            _logger.LogWarning($"API Key {apiKey.Name} blocked for {duration.TotalMinutes} minutes. Reason: {reason}");
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnblockApiKeyAsync(int id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return false;

            apiKey.IsTemporarilyBlocked = false;
            apiKey.BlockedUntil = null;
            apiKey.BlockReason = null;

            _logger.LogInformation($"API Key {apiKey.Name} unblocked");
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, object>> GetApiKeyUsageStatsAsync(int apiKeyId, DateTime fromDate)
        {
            var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
            if (apiKey == null) return new Dictionary<string, object>();

            return new Dictionary<string, object>
            {
                ["ApiKeyName"] = apiKey.Name,
                ["TotalRequests"] = apiKey.RequestCount,
                ["SecurityViolations"] = apiKey.SecurityViolations,
                ["LastUsed"] = apiKey.LastUsedAt,
                ["IsActive"] = apiKey.IsActive,
                ["IsBlocked"] = apiKey.IsTemporarilyBlocked,
                ["BlockedUntil"] = apiKey.BlockedUntil,
                ["LastViolation"] = apiKey.LastViolationAt,
                ["CreatedAt"] = apiKey.CreatedAt,
                ["ExpiresAt"] = apiKey.ExpiresAt
            };
        }

        public async Task<bool> RotateApiKeyAsync(int id)
        {
            var apiKey = await _context.ApiKeys.FindAsync(id);
            if (apiKey == null) return false;

            var oldKey = apiKey.Key;
            apiKey.Key = GenerateApiKey();
            apiKey.LastRotatedAt = DateTime.UtcNow;

            _logger.LogInformation($"API Key rotated for {apiKey.Name}");
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ValidateApiKeyStrengthAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return Task.FromResult(false);
            
            // Check minimum length (should be at least 32 characters for base64 encoded 24+ bytes)
            if (key.Length < 32) return Task.FromResult(false);
            
            // Check for valid base64url characters
            var validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
            if (!key.All(c => validChars.Contains(c))) return Task.FromResult(false);
            
            // Check entropy (simplified check)
            var uniqueChars = key.Distinct().Count();
            if (uniqueChars < 16) return Task.FromResult(false); // Should have reasonable character diversity
            
            return Task.FromResult(true);
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
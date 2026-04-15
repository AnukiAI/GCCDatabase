using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Security.Cryptography;
using System.Text;

namespace GCCWebApp.Services
{
    public interface IPartnerApiKeyService
    {
        Task<List<PartnerApiKey>> GetAllApiKeysAsync();
        Task<List<PartnerApiKey>> GetApiKeysByPartnerAsync(int partnerId);
        Task<PartnerApiKey?> GetApiKeyByIdAsync(int id);
        Task<PartnerApiKey?> GetApiKeyByValueAsync(string apiKey);
        Task<PartnerApiKey> GenerateApiKeyAsync(int partnerId, string? description, DateTime? expiresAt, string createdBy);
        Task<bool> RevokeApiKeyAsync(int id, string revokedBy);
        Task<(bool Success, string ErrorMessage)> DeleteApiKeyAsync(int id);
        Task<bool> ValidateApiKeyAsync(string apiKey);
        Task UpdateLastUsedAsync(string apiKey);
        Task<Dictionary<string, object>> GetUsageStatsAsync(int partnerId);
    }

    public class PartnerApiKeyService : IPartnerApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnerApiKeyService> _logger;

        public PartnerApiKeyService(ApplicationDbContext context, ILogger<PartnerApiKeyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PartnerApiKey>> GetAllApiKeysAsync()
        {
            try
            {
                return await _context.PartnerApiKeys
                    .Include(k => k.Partner)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all partner API keys");
                throw;
            }
        }

        public async Task<List<PartnerApiKey>> GetApiKeysByPartnerAsync(int partnerId)
        {
            try
            {
                return await _context.PartnerApiKeys
                    .Where(k => k.PartnerId == partnerId)
                    .Include(k => k.Partner)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for partner {PartnerId}", partnerId);
                throw;
            }
        }

        public async Task<PartnerApiKey?> GetApiKeyByIdAsync(int id)
        {
            try
            {
                return await _context.PartnerApiKeys
                    .Include(k => k.Partner)
                    .FirstOrDefaultAsync(k => k.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key {Id}", id);
                throw;
            }
        }

        public async Task<PartnerApiKey?> GetApiKeyByValueAsync(string apiKey)
        {
            try
            {
                return await _context.PartnerApiKeys
                    .Include(k => k.Partner)
                    .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key by value");
                return null;
            }
        }

        public async Task<PartnerApiKey> GenerateApiKeyAsync(int partnerId, string? description, DateTime? expiresAt, string createdBy)
        {
            try
            {
                // Verify partner exists
                var partner = await _context.Partners.FindAsync(partnerId);
                if (partner == null)
                {
                    throw new ArgumentException($"Partner with ID {partnerId} not found");
                }

                // Generate a secure API key
                var apiKey = GenerateSecureApiKey();

                var partnerApiKey = new PartnerApiKey
                {
                    PartnerId = partnerId,
                    ApiKey = apiKey,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    ExpiresAt = expiresAt,
                    RequestCount = 0
                };

                await _context.PartnerApiKeys.AddAsync(partnerApiKey);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated API key for partner {PartnerId} ({PartnerName}) by {CreatedBy}",
                    partnerId, partner.Name, createdBy);

                // Reload with partner included
                return await _context.PartnerApiKeys
                    .Include(k => k.Partner)
                    .FirstAsync(k => k.Id == partnerApiKey.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for partner {PartnerId}", partnerId);
                throw;
            }
        }

        public async Task<bool> RevokeApiKeyAsync(int id, string revokedBy)
        {
            try
            {
                var apiKey = await _context.PartnerApiKeys.FindAsync(id);
                if (apiKey == null)
                {
                    _logger.LogWarning("Attempted to revoke non-existent API key {Id}", id);
                    return false;
                }

                apiKey.IsActive = false;
                apiKey.RevokedAt = DateTime.UtcNow;
                apiKey.RevokedBy = revokedBy;

                _context.PartnerApiKeys.Update(apiKey);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Revoked API key {Id} for partner {PartnerId} by {RevokedBy}",
                    id, apiKey.PartnerId, revokedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key {Id}", id);
                throw;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteApiKeyAsync(int id)
        {
            try
            {
                var apiKey = await _context.PartnerApiKeys.FindAsync(id);
                if (apiKey == null)
                {
                    return (false, "API key not found");
                }

                if (apiKey.IsActive)
                {
                    return (false, "Cannot delete an active API key. Revoke it first.");
                }

                _context.PartnerApiKeys.Remove(apiKey);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted revoked API key {Id} for partner {PartnerId}", id, apiKey.PartnerId);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key {Id}", id);
                throw;
            }
        }

        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            try
            {
                var key = await _context.PartnerApiKeys
                    .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.IsActive);

                if (key == null)
                {
                    return false;
                }

                // Check if expired
                if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("API key {Id} is expired", key.Id);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return false;
            }
        }

        public async Task UpdateLastUsedAsync(string apiKey)
        {
            try
            {
                var key = await _context.PartnerApiKeys
                    .FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.IsActive);

                if (key != null)
                {
                    key.LastUsedAt = DateTime.UtcNow;
                    key.RequestCount++;

                    _context.PartnerApiKeys.Update(key);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used timestamp for API key");
                // Don't throw - this is not critical
            }
        }

        public async Task<Dictionary<string, object>> GetUsageStatsAsync(int partnerId)
        {
            try
            {
                var apiKeys = await GetApiKeysByPartnerAsync(partnerId);

                var stats = new Dictionary<string, object>
                {
                    ["TotalApiKeys"] = apiKeys.Count,
                    ["ActiveApiKeys"] = apiKeys.Count(k => k.IsActive),
                    ["RevokedApiKeys"] = apiKeys.Count(k => !k.IsActive),
                    ["TotalRequests"] = apiKeys.Sum(k => k.RequestCount),
                    ["LastUsed"] = apiKeys
                        .Where(k => k.LastUsedAt.HasValue)
                        .OrderByDescending(k => k.LastUsedAt)
                        .FirstOrDefault()?.LastUsedAt,
                    ["MostActiveKey"] = apiKeys
                        .OrderByDescending(k => k.RequestCount)
                        .FirstOrDefault()?.Description ?? "N/A"
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage stats for partner {PartnerId}", partnerId);
                throw;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure API key
        /// Format: gcc_partner_[64 character hex string]
        /// </summary>
        private string GenerateSecureApiKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32]; // 256 bits
                rng.GetBytes(bytes);
                var hex = Convert.ToHexString(bytes).ToLowerInvariant();
                return $"gcc_partner_{hex}";
            }
        }
    }
}

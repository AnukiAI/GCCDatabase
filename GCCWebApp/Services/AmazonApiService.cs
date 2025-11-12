using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface IAmazonApiService
    {
        Task<AmazonApiSettings?> GetSettingsAsync();
        Task<AmazonApiSettings> SaveSettingsAsync(AmazonApiSettings settings, string updatedBy);
        Task<bool> TestConnectionAsync(AmazonApiSettings settings);
        Task<(bool success, string message, int recordsProcessed)> TriggerSyncAsync(string triggeredBy);
        Task<List<AmazonSyncHistory>> GetSyncHistoryAsync(int count = 10);
        Task LogSyncHistoryAsync(AmazonSyncHistory history);
    }

    public class AmazonApiService : IAmazonApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AmazonApiService> _logger;

        public AmazonApiService(ApplicationDbContext context, ILogger<AmazonApiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AmazonApiSettings?> GetSettingsAsync()
        {
            try
            {
                // There should only be one settings record
                return await _context.AmazonApiSettings.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Amazon API settings");
                throw;
            }
        }

        public async Task<AmazonApiSettings> SaveSettingsAsync(AmazonApiSettings settings, string updatedBy)
        {
            try
            {
                var existing = await _context.AmazonApiSettings.FirstOrDefaultAsync();

                if (existing != null)
                {
                    // Update existing settings
                    existing.IsEnabled = settings.IsEnabled;
                    existing.SyncIntervalHours = settings.SyncIntervalHours;
                    existing.AccessKeyId = settings.AccessKeyId;
                    existing.SecretAccessKey = settings.SecretAccessKey;
                    existing.RefreshToken = settings.RefreshToken;
                    existing.Region = settings.Region;
                    existing.MarketplaceId = settings.MarketplaceId;
                    existing.SyncProducts = settings.SyncProducts;
                    existing.SyncCertificates = settings.SyncCertificates;
                    existing.SyncCompliance = settings.SyncCompliance;
                    existing.WebhookUrl = settings.WebhookUrl;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = updatedBy;

                    _context.AmazonApiSettings.Update(existing);
                }
                else
                {
                    // Create new settings
                    settings.CreatedAt = DateTime.UtcNow;
                    settings.UpdatedAt = DateTime.UtcNow;
                    settings.UpdatedBy = updatedBy;
                    await _context.AmazonApiSettings.AddAsync(settings);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Amazon API settings saved by {UpdatedBy}", updatedBy);

                return existing ?? settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Amazon API settings");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(AmazonApiSettings settings)
        {
            try
            {
                _logger.LogInformation("Testing Amazon API connection for marketplace {MarketplaceId}", settings.MarketplaceId);

                // TODO: Implement actual Amazon SP-API connection test
                // This would involve:
                // 1. Getting an access token using the refresh token
                // 2. Making a simple API call (e.g., get seller info)
                // 3. Validating the response

                // For now, basic validation
                if (string.IsNullOrWhiteSpace(settings.AccessKeyId) ||
                    string.IsNullOrWhiteSpace(settings.SecretAccessKey) ||
                    string.IsNullOrWhiteSpace(settings.RefreshToken))
                {
                    return false;
                }

                // Simulate API call delay
                await Task.Delay(1000);

                // Placeholder: return true if credentials are present
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Amazon API connection");
                return false;
            }
        }

        public async Task<(bool success, string message, int recordsProcessed)> TriggerSyncAsync(string triggeredBy)
        {
            var startTime = DateTime.UtcNow;
            var recordsProcessed = 0;
            var recordsSucceeded = 0;
            var recordsFailed = 0;

            try
            {
                var settings = await GetSettingsAsync();

                if (settings == null || !settings.IsEnabled)
                {
                    var message = "Amazon API integration is not enabled";
                    _logger.LogWarning(message);
                    return (false, message, 0);
                }

                _logger.LogInformation("Starting Amazon sync triggered by {TriggeredBy}", triggeredBy);

                // Update status to running
                settings.LastSyncStatus = "running";
                settings.UpdatedAt = DateTime.UtcNow;
                _context.AmazonApiSettings.Update(settings);
                await _context.SaveChangesAsync();

                // TODO: Implement actual sync logic
                // This would involve:
                // 1. Get certificates that need syncing
                // 2. Format data for Amazon SP-API
                // 3. Upload certificates/compliance data
                // 4. Handle responses and errors

                // Placeholder: Simulate sync operation
                await Task.Delay(2000);

                // For demo, get count of all certificates
                recordsProcessed = await _context.Certificates.CountAsync();
                recordsSucceeded = recordsProcessed;

                // Update settings with sync results
                settings.LastSync = DateTime.UtcNow;
                settings.LastSyncStatus = "success";
                settings.RecordsSynced += recordsProcessed;
                settings.LastError = null;
                settings.UpdatedAt = DateTime.UtcNow;
                _context.AmazonApiSettings.Update(settings);

                // Log sync history
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                await LogSyncHistoryAsync(new AmazonSyncHistory
                {
                    Timestamp = startTime,
                    Type = string.IsNullOrEmpty(triggeredBy) ? "Auto" : "Manual",
                    RecordsProcessed = recordsProcessed,
                    RecordsSucceeded = recordsSucceeded,
                    RecordsFailed = recordsFailed,
                    Status = "success",
                    DurationSeconds = duration,
                    Message = $"Successfully synced {recordsSucceeded} certificates",
                    TriggeredBy = triggeredBy
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Amazon sync completed successfully. Processed: {RecordsProcessed}", recordsProcessed);

                return (true, $"Sync completed successfully. Processed {recordsProcessed} records.", recordsProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Amazon sync");

                // Update settings with error status
                var settings = await GetSettingsAsync();
                if (settings != null)
                {
                    settings.LastSync = DateTime.UtcNow;
                    settings.LastSyncStatus = "error";
                    settings.LastError = ex.Message;
                    settings.UpdatedAt = DateTime.UtcNow;
                    _context.AmazonApiSettings.Update(settings);
                }

                // Log sync history with error
                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                await LogSyncHistoryAsync(new AmazonSyncHistory
                {
                    Timestamp = startTime,
                    Type = string.IsNullOrEmpty(triggeredBy) ? "Auto" : "Manual",
                    RecordsProcessed = recordsProcessed,
                    RecordsSucceeded = recordsSucceeded,
                    RecordsFailed = recordsFailed,
                    Status = "error",
                    DurationSeconds = duration,
                    Message = "Sync failed with error",
                    ErrorDetails = ex.Message,
                    TriggeredBy = triggeredBy
                });

                await _context.SaveChangesAsync();

                return (false, $"Sync failed: {ex.Message}", recordsProcessed);
            }
        }

        public async Task<List<AmazonSyncHistory>> GetSyncHistoryAsync(int count = 10)
        {
            try
            {
                return await _context.AmazonSyncHistory
                    .OrderByDescending(h => h.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Amazon sync history");
                throw;
            }
        }

        public async Task LogSyncHistoryAsync(AmazonSyncHistory history)
        {
            try
            {
                await _context.AmazonSyncHistory.AddAsync(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging Amazon sync history");
                // Don't throw - logging failure shouldn't break the sync
            }
        }
    }
}

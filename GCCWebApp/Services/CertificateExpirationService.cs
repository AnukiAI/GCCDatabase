using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface ICertificateExpirationService
    {
        /// <summary>
        /// Checks and expires overdue certificates. Throttled to run at most once per day.
        /// Safe to call on every login — skips if already ran today.
        /// </summary>
        Task CheckAndExpireAsync();
    }

    /// <summary>
    /// Service that automatically expires certificates whose expiration date has passed.
    /// Triggered on user login, throttled to run at most once per 24 hours.
    /// Logs all changes to both CertificateStatusHistory and AuditHistory tables.
    /// </summary>
    public class CertificateExpirationService : ICertificateExpirationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificateExpirationService> _logger;

        // Static so the throttle persists across scoped instances for the app's lifetime
        private static DateTime _lastRunUtc = DateTime.MinValue;
        private static readonly object _lock = new();

        public CertificateExpirationService(
            ApplicationDbContext context,
            ILogger<CertificateExpirationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CheckAndExpireAsync()
        {
            // Throttle: only run once per 24 hours
            lock (_lock)
            {
                if ((DateTime.UtcNow - _lastRunUtc).TotalHours < 24)
                {
                    return;
                }
                _lastRunUtc = DateTime.UtcNow;
            }

            try
            {
                await ExpireOverdueCertificatesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in certificate expiration check");
                // Reset so it retries on next login
                lock (_lock) { _lastRunUtc = DateTime.MinValue; }
            }
        }

        private async Task ExpireOverdueCertificatesAsync()
        {
            var now = DateTime.UtcNow;

            var expiredStatuses = new[]
            {
                CertificateStatusHelper.StatusExpired,
                CertificateStatusHelper.StatusExpiredNoAnnualVerification
            };

            var certsToExpire = await _context.Certificates
                .Where(c => c.ExpirationDate < now
                    && !expiredStatuses.Contains(c.Status))
                .ToListAsync();

            if (certsToExpire.Count == 0)
            {
                _logger.LogInformation("Certificate expiration check complete. No certificates to expire.");
                return;
            }

            _logger.LogInformation("Found {Count} certificates past expiration date. Updating statuses...", certsToExpire.Count);

            foreach (var cert in certsToExpire)
            {
                var previousStatus = cert.Status;
                cert.Status = CertificateStatusHelper.StatusExpired;
                cert.UpdatedAt = now;

                // Log to CertificateStatusHistory
                _context.CertificateStatusHistory.Add(new CertificateStatusHistory
                {
                    CertificateId = cert.Id,
                    CertificateNumber = cert.Number,
                    PreviousStatus = previousStatus,
                    NewStatus = CertificateStatusHelper.StatusExpired,
                    Reason = "Certificate expiration date has passed",
                    Notes = $"Auto-expired by system. Expiration date: {cert.ExpirationDate:yyyy-MM-dd}",
                    ChangedBy = 1,
                    ChangedByUsername = "System",
                    ChangedAt = now,
                    ChangeSource = AuditChangeSource.Automated,
                    IsAutomatedTransition = true,
                    WorkflowStep = "Auto-Expiration"
                });

                // Log to AuditHistory
                _context.AuditHistory.Add(new AuditHistory
                {
                    EntityType = AuditEntityType.Certificate,
                    EntityId = cert.Id.ToString(),
                    EntityName = cert.Number,
                    Action = AuditAction.StatusChange,
                    FieldName = "Status",
                    OldValue = previousStatus,
                    NewValue = CertificateStatusHelper.StatusExpired,
                    ChangedBy = 1,
                    ChangedByUsername = "System",
                    ChangedAt = now,
                    ChangeSource = AuditChangeSource.Automated,
                    Reason = "Certificate expiration date has passed"
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully expired {Count} certificates", certsToExpire.Count);
        }
    }
}

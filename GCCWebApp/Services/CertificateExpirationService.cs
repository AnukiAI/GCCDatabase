using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface ICertificateExpirationService
    {
        /// <summary>
        /// Checks and expires overdue certificates.
        /// When bypassThrottle=false (default), skips if already ran in the last 24h — safe for
        /// login-triggered calls that may fire many times per day.
        /// When bypassThrottle=true, runs unconditionally — used by the background timer,
        /// which is its own throttle.
        /// </summary>
        Task CheckAndExpireAsync(bool bypassThrottle = false);
    }

    /// <summary>
    /// Service that automatically expires certificates whose expiration date has passed.
    /// Triggered on user login, throttled to run at most once per 24 hours.
    /// Logs all changes to both CertificateStatusHistory and AuditHistory tables.
    /// </summary>
    public class CertificateExpirationService : ICertificateExpirationService
    {
        /// <summary>
        /// Save-time enforcement helper (DEF-171 Option A): if <paramref name="cert"/> has an
        /// expiration date in the past and its Status isn't already an expired-flavor, this
        /// mutates Status to Expired and returns a pre-built audit row that the caller should
        /// add to its DbContext (via _context.CertificateStatusHistory.Add(row)) before
        /// SaveChanges. Pure on the cert mutation side, no DB access here — keeps the helper
        /// safe to call from any controller/service whose own DbContext owns the save.
        /// Returns (false, null) when no flip was needed.
        /// </summary>
        public static (bool AutoExpired, CertificateStatusHistory? HistoryRow) TryApplySaveTimeAutoExpire(
            Certificate cert,
            int? actingUserId,
            string actingUsername,
            string workflowStep)
        {
            var today = DateTime.UtcNow.Date;
            var expiredStatuses = new[]
            {
                CertificateStatusHelper.StatusExpired,
                CertificateStatusHelper.StatusExpiredNoAnnualVerification
            };

            if (cert.ExpirationDate.Date >= today || expiredStatuses.Contains(cert.Status))
            {
                return (false, null);
            }

            var statusBefore = cert.Status;
            cert.Status = CertificateStatusHelper.StatusExpired;

            var row = new CertificateStatusHistory
            {
                CertificateId = cert.Id,
                CertificateNumber = cert.Number,
                PreviousStatus = statusBefore,
                NewStatus = CertificateStatusHelper.StatusExpired,
                Reason = "Save-time auto-expire: expiration date is in the past",
                Notes = $"Caller submitted Status='{statusBefore}' with ExpirationDate={cert.ExpirationDate:yyyy-MM-dd}; system overrode to Expired.",
                ChangedBy = actingUserId ?? 1,
                ChangedByUsername = actingUsername,
                ChangedAt = DateTime.UtcNow,
                ChangeSource = AuditChangeSource.Automated,
                IsAutomatedTransition = true,
                WorkflowStep = workflowStep
            };

            return (true, row);
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificateExpirationService> _logger;
        private readonly IProductIdService _productIdService;

        // Static so the throttle persists across scoped instances for the app's lifetime
        private static DateTime _lastRunUtc = DateTime.MinValue;
        private static readonly object _lock = new();

        public CertificateExpirationService(
            ApplicationDbContext context,
            ILogger<CertificateExpirationService> logger,
            IProductIdService productIdService)
        {
            _context = context;
            _logger = logger;
            _productIdService = productIdService;
        }

        public async Task CheckAndExpireAsync(bool bypassThrottle = false)
        {
            // Throttle (login-triggered path only): skip if a sweep ran in the last 24h.
            // The background timer passes bypassThrottle=true because the timer itself is the cadence.
            if (!bypassThrottle)
            {
                lock (_lock)
                {
                    if ((DateTime.UtcNow - _lastRunUtc).TotalHours < 24)
                    {
                        return;
                    }
                    _lastRunUtc = DateTime.UtcNow;
                }
            }

            try
            {
                await ExpireOverdueCertificatesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in certificate expiration check");
                if (!bypassThrottle)
                {
                    // Reset so the next login retries.
                    lock (_lock) { _lastRunUtc = DateTime.MinValue; }
                }
            }
        }

        private async Task ExpireOverdueCertificatesAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var expiredStatuses = new[]
            {
                CertificateStatusHelper.StatusExpired,
                CertificateStatusHelper.StatusExpiredNoAnnualVerification
            };

            // Expire certs whose expiration date is before today (cert shows through its expiration day, expires the day after)
            var certsToExpire = await _context.Certificates
                .Where(c => c.ExpirationDate < today
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

            // CR-015: cascade Inactive status to all child product IDs on the just-expired certs.
            foreach (var cert in certsToExpire)
            {
                await _productIdService.SyncProductIdStatusesForCertificateAsync(cert.Id);
            }

            _logger.LogInformation("Successfully expired {Count} certificates", certsToExpire.Count);
        }
    }
}

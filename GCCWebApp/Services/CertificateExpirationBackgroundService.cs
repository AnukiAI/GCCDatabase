using Microsoft.Extensions.Hosting;

namespace GCCWebApp.Services
{
    /// <summary>
    /// Runs the certificate expiration sweep on a schedule, independent of user logins.
    ///
    /// Default cadence: once per day at 00:00 UTC. Cert expiration is a date-boundary event —
    /// a cert valid through 2026-05-15 becomes expired the instant the UTC date ticks over to
    /// 2026-05-16 — so flipping statuses at midnight UTC aligns the sweep with the moment
    /// expiration actually happens.
    ///
    /// Override for testing: set <c>Jobs:CertificateExpiration:IntervalMinutes</c> in appsettings
    /// to a positive integer. The service then uses a fixed interval instead of midnight, so a
    /// dev/test environment can verify the BG sweep path without waiting until UTC midnight.
    ///
    /// On every start (cold boot or app-pool recycle) the service runs one immediate sweep to
    /// catch certs that aged into expiration while the app was down.
    ///
    /// Save-time auto-expire enforcement in <see cref="CertificateService.UpdateCertificateAsync"/>
    /// handles the manual-edit case at the moment of save, so this background sweep is only
    /// strictly necessary for certs that age past their expiration date without anyone touching
    /// them.
    /// </summary>
    public class CertificateExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CertificateExpirationBackgroundService> _logger;
        private readonly TimeSpan? _intervalOverride;
        private readonly TimeSpan _startupDelay;

        public CertificateExpirationBackgroundService(
            IServiceProvider services,
            ILogger<CertificateExpirationBackgroundService> logger,
            IConfiguration configuration)
        {
            _services = services;
            _logger = logger;

            var minutes = configuration.GetValue<int?>("Jobs:CertificateExpiration:IntervalMinutes");
            _intervalOverride = (minutes.HasValue && minutes.Value > 0)
                ? TimeSpan.FromMinutes(minutes.Value)
                : null;

            var startupSeconds = configuration.GetValue<int?>("Jobs:CertificateExpiration:StartupDelaySeconds") ?? 30;
            if (startupSeconds < 0) startupSeconds = 0;
            _startupDelay = TimeSpan.FromSeconds(startupSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mode = _intervalOverride.HasValue
                ? $"Interval={_intervalOverride.Value}"
                : "Daily at 00:00 UTC";
            _logger.LogInformation(
                "CertificateExpirationBackgroundService starting. Mode={Mode}, StartupDelay={StartupDelay}",
                mode, _startupDelay);

            if (_startupDelay > TimeSpan.Zero)
            {
                try { await Task.Delay(_startupDelay, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }

            // Startup catch-up: handle certs that expired during the app-pool's idle window.
            await RunSweepOnce(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = ComputeNextDelay();
                _logger.LogInformation(
                    "CertificateExpiration next sweep scheduled at {NextUtc:yyyy-MM-dd HH:mm:ss} UTC (in {Delay})",
                    DateTime.UtcNow + delay, delay);

                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { break; }

                await RunSweepOnce(stoppingToken);
            }

            _logger.LogInformation("CertificateExpirationBackgroundService stopped");
        }

        private TimeSpan ComputeNextDelay()
        {
            if (_intervalOverride.HasValue) return _intervalOverride.Value;

            // Next 00:00 UTC. Always strictly in the future so we never re-fire instantly
            // after a midnight sweep (Date.AddDays(1) guarantees > now).
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            return nextMidnight - now;
        }

        private async Task RunSweepOnce(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                using var scope = _services.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<ICertificateExpirationService>();
                await svc.CheckAndExpireAsync(bypassThrottle: true);
            }
            catch (Exception ex)
            {
                // Isolated per-iteration: log and keep scheduling. One bad run must not kill the loop.
                _logger.LogError(ex, "CertificateExpirationBackgroundService sweep iteration failed");
            }
        }
    }
}

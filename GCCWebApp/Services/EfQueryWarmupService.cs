using GCCWebApp.Models;
using GCCWebApp.Repositories;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace GCCWebApp.Services
{
    /// <summary>
    /// DEF-234: warm the EF Core query plan + SQL Server query cache for the public tile
    /// cert-list query during app startup, so the first real user (or smoke run) doesn't
    /// pay the cold-compile cost.
    ///
    /// Measured before this fix: first call to /api/certificate-tiles took 23.1s, then
    /// dropped to &lt;200ms on subsequent calls. The 23s window intermittently tripped the
    /// browser-side AbortSignal.timeout(30000) when other page-load fetches competed for
    /// the same DB connection, surfacing as RED in the Tier 1 smoke suite (4/12 cells).
    ///
    /// This service fires one small (Take 1) query through the same repository method the
    /// public tile API uses, which forces EF to compile the LINQ → SQL plan, hits SQL with
    /// the same shape so the engine caches its plan, and warms the connection pool.
    /// Subsequent calls are warm-path fast.
    ///
    /// Fail-safe: any exception during warm-up is logged at Warning and swallowed. Warm-up
    /// is an optimization, not a correctness requirement — a failure here must not prevent
    /// the app from starting.
    /// </summary>
    public class EfQueryWarmupService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EfQueryWarmupService> _logger;

        public EfQueryWarmupService(IServiceProvider services, ILogger<EfQueryWarmupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Run on a background task so we don't block the host startup pipeline.
            // The first incoming request will be served by the time this completes;
            // even partially-warmed caches are better than fully cold.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();

                    var filter = new CertificateSearchFilter
                    {
                        Statuses = CertificateStatusHelper.PublicStatuses.ToList(),
                        ExcludeExpiredByDate = true,
                        ExcludeNotYetEffectiveByDate = true, // DEF-358: match the public tile query shape we're warming
                        Page = 1,
                        PageSize = 1, // minimum — we just need EF to compile and SQL to plan
                        SortBy = "number",
                        SortDescending = true
                    };

                    var sw = Stopwatch.StartNew();
                    var (_, totalCount) = await repo.GetAdvancedFilteredCertificatesAsync(filter);
                    sw.Stop();

                    _logger.LogInformation(
                        "EfQueryWarmupService: public tile query warmed in {ElapsedMs}ms (totalCount={TotalCount})",
                        sw.ElapsedMilliseconds, totalCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "EfQueryWarmupService: warm-up failed (non-fatal). First public tile request will pay cold-start cost.");
                }
            }, cancellationToken);

            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

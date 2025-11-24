using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GCCWebApp.Services.ETL
{
    public class ETLSchedulerService : BackgroundService
    {
        private readonly ILogger<ETLSchedulerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;

        public ETLSchedulerService(
            ILogger<ETLSchedulerService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ETL Scheduler Service starting...");

            // Run ETL metrics check every hour instead of every 5 minutes to reduce log noise
            _timer = new Timer(async _ => await CheckETLMetrics(), null, TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));

            await Task.CompletedTask;
        }

        private async Task CheckETLMetrics()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var etlMonitor = scope.ServiceProvider.GetRequiredService<IETLMonitor>();

                    // Get metrics for the last hour
                    var toDate = DateTime.UtcNow;
                    var fromDate = toDate.AddHours(-1);

                    var metrics = await etlMonitor.GetETLMetricsAsync(fromDate, toDate);

                    // Only log if there's actual activity or this is a daily summary (at midnight)
                    if (metrics.TotalJobsRun > 0 || DateTime.UtcNow.Hour == 0)
                    {
                        _logger.LogInformation(
                            "ETL metrics: {TotalJobs} jobs, {Successful} successful, {Failed} failed",
                            metrics.TotalJobsRun,
                            metrics.SuccessfulJobs,
                            metrics.FailedJobs);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ETL metrics");
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Services.ETL
{
    public class ETLMonitor : IETLMonitor
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ETLMonitor> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ETLMonitor(ApplicationDbContext context, ILogger<ETLMonitor> logger, IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<ETLMetrics> GetETLMetricsAsync(DateTime fromDate, DateTime toDate)
        {
            _logger.LogInformation("Retrieving ETL metrics from {FromDate} to {ToDate}", fromDate, toDate);

            try
            {
                // Get ETL log entries for the specified period
                var logEntries = await _context.Set<ETLLogEntry>()
                    .Where(le => le.Timestamp >= fromDate && le.Timestamp <= toDate)
                    .ToListAsync();

                var jobIds = logEntries.Select(le => le.JobId).Distinct().ToList();

                var metrics = new ETLMetrics
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalJobsRun = jobIds.Count,
                    SuccessfulJobs = logEntries.Count(le => le.Category == "JobComplete"),
                    FailedJobs = logEntries.Count(le => le.Category == "JobError")
                };

                // Calculate performance metrics
                var jobCompletions = logEntries.Where(le => le.Category == "JobComplete").ToList();
                if (jobCompletions.Any())
                {
                    var totalRecords = jobCompletions
                        .Where(jc => jc.Properties.ContainsKey("RecordsProcessed"))
                        .Sum(jc => Convert.ToInt64(jc.Properties["RecordsProcessed"]));
                    
                    metrics.TotalRecordsProcessed = totalRecords;

                    var totalDuration = jobCompletions
                        .Where(jc => jc.Properties.ContainsKey("Duration"))
                        .Sum(jc => Convert.ToDouble(jc.Properties["Duration"]));
                    
                    if (jobCompletions.Count > 0)
                    {
                        metrics.AverageJobDuration = TimeSpan.FromMinutes(totalDuration / jobCompletions.Count);
                    }
                }

                // Calculate data quality metrics
                await CalculateDataQualityMetrics(metrics, fromDate, toDate);

                // Calculate performance metrics
                await CalculatePerformanceMetrics(metrics, fromDate, toDate);

                _logger.LogInformation("ETL metrics calculated: {TotalJobs} jobs, {SuccessfulJobs} successful, {FailedJobs} failed", 
                    metrics.TotalJobsRun, metrics.SuccessfulJobs, metrics.FailedJobs);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ETL metrics");
                throw;
            }
        }

        public async Task LogETLEventAsync(ETLLogEntry logEntry)
        {
            try
            {
                _context.Set<ETLLogEntry>().Add(logEntry);
                await _context.SaveChangesAsync();

                _logger.LogDebug("ETL event logged: {Category} - {Message}", logEntry.Category, logEntry.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log ETL event: {Message}", logEntry.Message);
                throw;
            }
        }

        public async Task<List<ETLAlert>> GetActiveAlertsAsync()
        {
            try
            {
                var activeAlerts = await _context.Set<ETLAlert>()
                    .Where(a => !a.IsResolved)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} active ETL alerts", activeAlerts.Count);

                return activeAlerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active ETL alerts");
                throw;
            }
        }

        public async Task CreateAlertAsync(ETLAlert alert)
        {
            try
            {
                _context.Set<ETLAlert>().Add(alert);
                await _context.SaveChangesAsync();

                _logger.LogWarning("ETL Alert created: {AlertType} - {Message}", alert.AlertType, alert.Message);

                // Send notifications based on severity
                await SendAlertNotificationAsync(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ETL alert: {Message}", alert.Message);
                throw;
            }
        }

        private async Task CalculateDataQualityMetrics(ETLMetrics metrics, DateTime fromDate, DateTime toDate)
        {
            try
            {
                // Get data quality issues from ETL transformations
                var qualityIssues = await _context.Set<ETLDataQualityIssue>()
                    .Where(dqi => dqi.DetectedAt >= fromDate && dqi.DetectedAt <= toDate)
                    .GroupBy(dqi => dqi.IssueType)
                    .Select(g => new { IssueType = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var issue in qualityIssues)
                {
                    metrics.DataQualityMetrics[issue.IssueType] = issue.Count;
                }

                // Calculate overall data quality score
                var totalRecords = metrics.TotalRecordsProcessed;
                var totalIssues = qualityIssues.Sum(i => i.Count);
                
                if (totalRecords > 0)
                {
                    var qualityScore = Math.Max(0, (double)(totalRecords - totalIssues) / totalRecords * 100);
                    metrics.DataQualityMetrics["OverallQualityScore"] = Math.Round(qualityScore, 2);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating data quality metrics");
            }
        }

        private async Task CalculatePerformanceMetrics(ETLMetrics metrics, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var performanceMetrics = new List<ETLPerformanceMetric>();

                // Calculate throughput (records per minute)
                if (metrics.AverageJobDuration.TotalMinutes > 0 && metrics.TotalRecordsProcessed > 0)
                {
                    var throughput = metrics.TotalRecordsProcessed / metrics.AverageJobDuration.TotalMinutes;
                    performanceMetrics.Add(new ETLPerformanceMetric
                    {
                        MetricName = "RecordsPerMinute",
                        Value = Math.Round(throughput, 2),
                        Unit = "records/min"
                    });
                }

                // Calculate success rate
                if (metrics.TotalJobsRun > 0)
                {
                    var successRate = (double)metrics.SuccessfulJobs / metrics.TotalJobsRun * 100;
                    performanceMetrics.Add(new ETLPerformanceMetric
                    {
                        MetricName = "SuccessRate",
                        Value = Math.Round(successRate, 2),
                        Unit = "percentage"
                    });
                }

                // Calculate resource utilization (simplified)
                performanceMetrics.Add(new ETLPerformanceMetric
                {
                    MetricName = "AverageMemoryUsage",
                    Value = await GetAverageMemoryUsageAsync(fromDate, toDate),
                    Unit = "MB"
                });

                performanceMetrics.Add(new ETLPerformanceMetric
                {
                    MetricName = "AverageCpuUsage",
                    Value = await GetAverageCpuUsageAsync(fromDate, toDate),
                    Unit = "percentage"
                });

                metrics.PerformanceMetrics = performanceMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance metrics");
            }
        }

        private async Task<double> GetAverageMemoryUsageAsync(DateTime fromDate, DateTime toDate)
        {
            // This would typically integrate with system monitoring tools
            // For now, return a simulated value
            return await Task.FromResult(512.0); // MB
        }

        private async Task<double> GetAverageCpuUsageAsync(DateTime fromDate, DateTime toDate)
        {
            // This would typically integrate with system monitoring tools
            // For now, return a simulated value
            return await Task.FromResult(65.0); // percentage
        }

        private async Task SendAlertNotificationAsync(ETLAlert alert)
        {
            try
            {
                switch (alert.Severity)
                {
                    case ETLSeverity.Critical:
                        await SendCriticalAlertNotificationAsync(alert);
                        break;
                    case ETLSeverity.Error:
                        await SendErrorAlertNotificationAsync(alert);
                        break;
                    case ETLSeverity.Warning:
                        await SendWarningAlertNotificationAsync(alert);
                        break;
                    default:
                        _logger.LogInformation("Info alert created: {Message}", alert.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert notification for alert: {AlertId}", alert.Id);
            }
        }

        private async Task SendCriticalAlertNotificationAsync(ETLAlert alert)
        {
            // Send immediate notifications (email, SMS, Slack, etc.)
            _logger.LogError("CRITICAL ETL ALERT: {Message}", alert.Message);
            
            // Here you would integrate with notification services
            // Example: Email service, Slack webhook, Teams notification, etc.
            
            await Task.CompletedTask;
        }

        private async Task SendErrorAlertNotificationAsync(ETLAlert alert)
        {
            // Send error notifications
            _logger.LogError("ETL ERROR ALERT: {Message}", alert.Message);
            
            await Task.CompletedTask;
        }

        private async Task SendWarningAlertNotificationAsync(ETLAlert alert)
        {
            // Send warning notifications
            _logger.LogWarning("ETL WARNING ALERT: {Message}", alert.Message);
            
            await Task.CompletedTask;
        }
    }

    // Background service for ETL monitoring and scheduling
    public class ETLSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ETLSchedulerService> _logger;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _metricCollectionTimer;

        public ETLSchedulerService(IServiceProvider serviceProvider, ILogger<ETLSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Set up periodic health checks (every 5 minutes)
            _healthCheckTimer = new Timer(CheckETLHealth, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            
            // Set up metric collection (every 15 minutes)
            _metricCollectionTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunScheduledETLJobs(stoppingToken);
                    await CheckForExpiredJobs(stoppingToken);
                    await CleanupOldLogs(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ETL scheduler service");
                }

                // Wait for 1 minute before next check
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task RunScheduledETLJobs(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var etlService = scope.ServiceProvider.GetRequiredService<IETLService>();

            try
            {
                // Get active ETL configurations with cron schedules
                var scheduledConfigs = await context.Set<ETLConfiguration>()
                    .Where(c => c.IsActive && !string.IsNullOrEmpty(c.CronSchedule))
                    .ToListAsync(cancellationToken);

                foreach (var config in scheduledConfigs)
                {
                    if (ShouldRunJob(config))
                    {
                        _logger.LogInformation("Starting scheduled ETL job: {JobName}", config.Name);
                        
                        // Run ETL job in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await etlService.RunFullETLPipelineAsync(config);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Scheduled ETL job failed: {JobName}", config.Name);
                            }
                        }, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking scheduled ETL jobs");
            }
        }

        private async Task CheckForExpiredJobs(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var etlService = scope.ServiceProvider.GetRequiredService<IETLService>();
            var monitor = scope.ServiceProvider.GetRequiredService<IETLMonitor>();

            try
            {
                var activeJobs = await etlService.GetJobStatusesAsync();
                var expiredJobs = activeJobs.Where(job => 
                    job.Status == ETLStatus.Running && 
                    job.StartTime < DateTime.UtcNow.AddHours(-2)) // Jobs running for more than 2 hours
                    .ToList();

                foreach (var expiredJob in expiredJobs)
                {
                    _logger.LogWarning("ETL job {JobId} has been running for {Duration} hours", 
                        expiredJob.JobId, (DateTime.UtcNow - expiredJob.StartTime).TotalHours);

                    await monitor.CreateAlertAsync(new ETLAlert
                    {
                        AlertType = "LongRunningJob",
                        Message = $"ETL job {expiredJob.JobName} has been running for more than 2 hours",
                        Severity = ETLSeverity.Warning,
                        Context = new Dictionary<string, object>
                        {
                            ["JobId"] = expiredJob.JobId,
                            ["JobName"] = expiredJob.JobName,
                            ["StartTime"] = expiredJob.StartTime,
                            ["Duration"] = (DateTime.UtcNow - expiredJob.StartTime).TotalHours
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for expired jobs");
            }
        }

        private async Task CleanupOldLogs(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep logs for 30 days

                // Clean up old log entries
                var oldLogs = await context.Set<ETLLogEntry>()
                    .Where(le => le.Timestamp < cutoffDate)
                    .ToListAsync(cancellationToken);

                if (oldLogs.Count > 0)
                {
                    context.Set<ETLLogEntry>().RemoveRange(oldLogs);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} old ETL log entries", oldLogs.Count);
                }

                // Clean up resolved alerts older than 7 days
                var oldAlerts = await context.Set<ETLAlert>()
                    .Where(a => a.IsResolved && a.ResolvedAt < DateTime.UtcNow.AddDays(-7))
                    .ToListAsync(cancellationToken);

                if (oldAlerts.Count > 0)
                {
                    context.Set<ETLAlert>().RemoveRange(oldAlerts);
                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Cleaned up {Count} old resolved ETL alerts", oldAlerts.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ETL log cleanup");
            }
        }

        private void CheckETLHealth(object state)
        {
            _ = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<IETLMonitor>();

                try
                {
                    var recentMetrics = await monitor.GetETLMetricsAsync(
                        DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

                    // Check for concerning patterns
                    if (recentMetrics.FailedJobs > recentMetrics.SuccessfulJobs && recentMetrics.TotalJobsRun > 0)
                    {
                        await monitor.CreateAlertAsync(new ETLAlert
                        {
                            AlertType = "HighFailureRate",
                            Message = $"ETL failure rate is high: {recentMetrics.FailedJobs}/{recentMetrics.TotalJobsRun} jobs failed in the last hour",
                            Severity = ETLSeverity.Warning
                        });
                    }

                    _logger.LogDebug("ETL health check completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during ETL health check");
                }
            });
        }

        private void CollectMetrics(object state)
        {
            _ = Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<IETLMonitor>();

                try
                {
                    var metrics = await monitor.GetETLMetricsAsync(
                        DateTime.UtcNow.AddMinutes(-15), DateTime.UtcNow);

                    // Store metrics for later analysis
                    // This could be sent to a time-series database, monitoring system, etc.
                    
                    _logger.LogDebug("ETL metrics collected: {TotalJobs} jobs, {TotalRecords} records", 
                        metrics.TotalJobsRun, metrics.TotalRecordsProcessed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during metric collection");
                }
            });
        }

        private bool ShouldRunJob(ETLConfiguration config)
        {
            if (string.IsNullOrEmpty(config.CronSchedule))
                return false;

            // Simplified cron check - in a real implementation, you'd use a proper cron parser
            // For demo purposes, assume daily job at specific hour
            if (config.CronSchedule == "0 2 * * *") // Daily at 2 AM
            {
                var now = DateTime.Now;
                var lastRun = config.LastRunTime ?? DateTime.MinValue;
                
                return now.Hour == 2 && now.Minute < 5 && 
                       (now.Date > lastRun.Date || lastRun == DateTime.MinValue);
            }

            return false;
        }

        public override void Dispose()
        {
            _healthCheckTimer?.Dispose();
            _metricCollectionTimer?.Dispose();
            base.Dispose();
        }
    }
}
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
}

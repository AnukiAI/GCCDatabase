using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GCCWebApp.Services.ETL;
using GCCWebApp.Models;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ETLController : ControllerBase
    {
        private readonly IETLService _etlService;
        private readonly IETLMonitor _etlMonitor;
        private readonly ILogger<ETLController> _logger;

        public ETLController(IETLService etlService, IETLMonitor etlMonitor, ILogger<ETLController> logger)
        {
            _etlService = etlService;
            _etlMonitor = etlMonitor;
            _logger = logger;
        }

        /// <summary>
        /// Run a full ETL pipeline
        /// </summary>
        [HttpPost("run-full")]
        public async Task<ActionResult<ETLJobResult>> RunFullETL([FromBody] ETLConfiguration config)
        {
            try
            {
                _logger.LogInformation("Starting full ETL pipeline: {ConfigName}", config.Name);
                var result = await _etlService.RunFullETLPipelineAsync(config);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running full ETL pipeline");
                return StatusCode(500, new { error = "Failed to run ETL pipeline", details = ex.Message });
            }
        }

        /// <summary>
        /// Run an incremental ETL pipeline
        /// </summary>
        [HttpPost("run-incremental")]
        public async Task<ActionResult<ETLJobResult>> RunIncrementalETL([FromBody] ETLConfiguration config)
        {
            try
            {
                _logger.LogInformation("Starting incremental ETL pipeline: {ConfigName}", config.Name);
                var result = await _etlService.RunIncrementalETLAsync(config);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running incremental ETL pipeline");
                return StatusCode(500, new { error = "Failed to run incremental ETL pipeline", details = ex.Message });
            }
        }

        /// <summary>
        /// Get status of all ETL jobs
        /// </summary>
        [HttpGet("jobs/status")]
        public async Task<ActionResult<List<ETLJobStatus>>> GetJobStatuses()
        {
            try
            {
                var statuses = await _etlService.GetJobStatusesAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job statuses");
                return StatusCode(500, new { error = "Failed to retrieve job statuses", details = ex.Message });
            }
        }

        /// <summary>
        /// Cancel a running ETL job
        /// </summary>
        [HttpPost("jobs/{jobId}/cancel")]
        public async Task<ActionResult> CancelJob(Guid jobId)
        {
            try
            {
                var result = await _etlService.CancelJobAsync(jobId);
                if (result)
                {
                    return Ok(new { message = "Job cancelled successfully" });
                }
                return NotFound(new { error = "Job not found or already completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
                return StatusCode(500, new { error = "Failed to cancel job", details = ex.Message });
            }
        }

        /// <summary>
        /// Extract data only
        /// </summary>
        [HttpPost("extract")]
        public async Task<ActionResult<ETLJobResult>> ExtractData([FromBody] ETLConfiguration config)
        {
            try
            {
                var result = await _etlService.ExtractDataAsync(config);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data extraction");
                return StatusCode(500, new { error = "Data extraction failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Get ETL metrics for a date range
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<ETLMetrics>> GetMetrics(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                if (toDate == default)
                    toDate = DateTime.UtcNow;
                
                if (fromDate == default)
                    fromDate = DateTime.UtcNow.AddDays(-7);

                var metrics = await _etlMonitor.GetETLMetricsAsync(fromDate, toDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ETL metrics");
                return StatusCode(500, new { error = "Failed to retrieve metrics", details = ex.Message });
            }
        }

        /// <summary>
        /// Get active alerts
        /// </summary>
        [HttpGet("alerts")]
        public async Task<ActionResult<List<ETLAlert>>> GetActiveAlerts()
        {
            try
            {
                var alerts = await _etlMonitor.GetActiveAlertsAsync();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active alerts");
                return StatusCode(500, new { error = "Failed to retrieve alerts", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a custom alert
        /// </summary>
        [HttpPost("alerts")]
        public async Task<ActionResult> CreateAlert([FromBody] ETLAlert alert)
        {
            try
            {
                await _etlMonitor.CreateAlertAsync(alert);
                return Ok(new { message = "Alert created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, new { error = "Failed to create alert", details = ex.Message });
            }
        }

        /// <summary>
        /// Export certificate data to file
        /// </summary>
        [HttpPost("export")]
        public async Task<ActionResult> ExportData([FromBody] ExportRequest request)
        {
            try
            {
                var config = new ETLConfiguration
                {
                    Name = "Data Export",
                    DataSources = request.DataSources,
                    Transformations = new List<string> { "validate", "enrich" },
                    Destinations = new List<string> { $"file:{request.FilePath}:{request.Format}" }
                };

                var result = await _etlService.RunFullETLPipelineAsync(config);
                
                if (result.Status == ETLStatus.Completed)
                {
                    return Ok(new { message = "Data exported successfully", filePath = request.FilePath });
                }
                else
                {
                    return StatusCode(500, new { error = "Export failed", details = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                return StatusCode(500, new { error = "Export failed", details = ex.Message });
            }
        }

        /// <summary>
        /// Get ETL dashboard data
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ETLDashboardData>> GetDashboardData()
        {
            try
            {
                var recentMetrics = await _etlMonitor.GetETLMetricsAsync(
                    DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
                
                var activeJobs = await _etlService.GetJobStatusesAsync();
                var activeAlerts = await _etlMonitor.GetActiveAlertsAsync();

                var dashboardData = new ETLDashboardData
                {
                    TotalJobsLast7Days = recentMetrics.TotalJobsRun,
                    SuccessfulJobsLast7Days = recentMetrics.SuccessfulJobs,
                    FailedJobsLast7Days = recentMetrics.FailedJobs,
                    TotalRecordsProcessedLast7Days = recentMetrics.TotalRecordsProcessed,
                    AverageJobDuration = recentMetrics.AverageJobDuration,
                    ActiveJobsCount = activeJobs.Count(j => j.Status == ETLStatus.Running),
                    ActiveAlertsCount = activeAlerts.Count,
                    DataQualityScore = recentMetrics.DataQualityMetrics.GetValueOrDefault("OverallQualityScore", 0),
                    LastRunTime = activeJobs.OrderByDescending(j => j.StartTime).FirstOrDefault()?.StartTime,
                    SystemHealth = CalculateSystemHealth(recentMetrics, activeAlerts.Count)
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, new { error = "Failed to retrieve dashboard data", details = ex.Message });
            }
        }

        /// <summary>
        /// Validate ETL configuration
        /// </summary>
        [HttpPost("validate-config")]
        public async Task<ActionResult<ConfigValidationResult>> ValidateConfiguration([FromBody] ETLConfiguration config)
        {
            try
            {
                var validationResult = new ConfigValidationResult
                {
                    IsValid = true,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                };

                // Validate configuration
                if (string.IsNullOrWhiteSpace(config.Name))
                {
                    validationResult.Errors.Add("Configuration name is required");
                    validationResult.IsValid = false;
                }

                if (!config.DataSources.Any())
                {
                    validationResult.Errors.Add("At least one data source must be specified");
                    validationResult.IsValid = false;
                }

                if (!config.Destinations.Any())
                {
                    validationResult.Warnings.Add("No destinations specified - data will not be loaded anywhere");
                }

                // Validate cron schedule if provided
                if (!string.IsNullOrEmpty(config.CronSchedule))
                {
                    if (!IsValidCronExpression(config.CronSchedule))
                    {
                        validationResult.Errors.Add("Invalid cron expression");
                        validationResult.IsValid = false;
                    }
                }

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                return StatusCode(500, new { error = "Configuration validation failed", details = ex.Message });
            }
        }

        private string CalculateSystemHealth(ETLMetrics metrics, int activeAlerts)
        {
            if (activeAlerts > 0 && metrics.FailedJobs > metrics.SuccessfulJobs)
                return "Critical";
            
            if (activeAlerts > 2 || metrics.FailedJobs > 0)
                return "Warning";
            
            if (metrics.TotalJobsRun > 0 && metrics.SuccessfulJobs == metrics.TotalJobsRun)
                return "Healthy";
            
            return "Unknown";
        }

        private bool IsValidCronExpression(string cronExpression)
        {
            // Simplified cron validation - in production, use a proper cron library
            var parts = cronExpression.Split(' ');
            return parts.Length == 5; // minute hour day month dayOfWeek
        }
    }

    public class ExportRequest
    {
        public List<string> DataSources { get; set; } = new();
        public string FilePath { get; set; } = string.Empty;
        public string Format { get; set; } = "json";
    }

    public class ConfigValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class ETLDashboardData
    {
        public int TotalJobsLast7Days { get; set; }
        public int SuccessfulJobsLast7Days { get; set; }
        public int FailedJobsLast7Days { get; set; }
        public long TotalRecordsProcessedLast7Days { get; set; }
        public TimeSpan AverageJobDuration { get; set; }
        public int ActiveJobsCount { get; set; }
        public int ActiveAlertsCount { get; set; }
        public double DataQualityScore { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string SystemHealth { get; set; } = "Unknown";
    }
}
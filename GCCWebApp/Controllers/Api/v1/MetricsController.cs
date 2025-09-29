using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Models.Api;
using GCCWebApp.Services.Performance;
using Asp.Versioning;

namespace GCCWebApp.Controllers.Api.v1;

[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
public class MetricsController : BaseApiController
{
    private readonly IPerformanceMonitoringService _performanceMonitoring;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IPerformanceMonitoringService performanceMonitoring,
        ILogger<MetricsController> logger)
    {
        _performanceMonitoring = performanceMonitoring;
        _logger = logger;
    }

    /// <summary>
    /// Get current performance metrics
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StandardApiResponse<PerformanceMetrics>), StatusCodes.Status200OK)]
    public IActionResult GetMetrics()
    {
        try
        {
            var metrics = _performanceMonitoring.GetMetrics();
            return SuccessResponse(metrics, "Performance metrics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return HandleError(ex, "Failed to retrieve performance metrics");
        }
    }

    /// <summary>
    /// Get performance metrics summary
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetMetricsSummary()
    {
        try
        {
            var metrics = _performanceMonitoring.GetMetrics();
            
            var summary = new
            {
                TotalOperations = metrics.Counters.Values.Where(c => c.Count > 0).Sum(c => c.Count),
                AverageResponseTime = metrics.Counters
                    .Where(kv => kv.Key.EndsWith("_duration_ms"))
                    .Select(kv => kv.Value.AverageValue)
                    .DefaultIfEmpty(0)
                    .Average(),
                SlowestOperations = metrics.Counters
                    .Where(kv => kv.Key.EndsWith("_duration_ms"))
                    .OrderByDescending(kv => kv.Value.MaxValue)
                    .Take(10)
                    .Select(kv => new { Operation = kv.Key, MaxDurationMs = kv.Value.MaxValue }),
                ErrorCounts = metrics.Counters
                    .Where(kv => kv.Key.EndsWith("_error_count"))
                    .ToDictionary(kv => kv.Key, kv => kv.Value.Count),
                RecentMetricsCount = metrics.RecentMetrics.Length,
                CollectedAt = metrics.CollectedAt
            };

            return SuccessResponse(summary, "Performance metrics summary retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics summary");
            return HandleError(ex, "Failed to retrieve performance metrics summary");
        }
    }

    /// <summary>
    /// Reset performance metrics
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult ResetMetrics()
    {
        try
        {
            _performanceMonitoring.ResetMetrics();
            return SuccessResponse(new { ResetAt = DateTime.UtcNow }, "Performance metrics reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting performance metrics");
            return HandleError(ex, "Failed to reset performance metrics");
        }
    }

    /// <summary>
    /// Record a custom metric
    /// </summary>
    [HttpPost("record")]
    [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RecordMetric([FromBody] RecordMetricRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse(ModelStateToApiErrors());

            _performanceMonitoring.RecordMetric(request.Name, request.Value, request.Tags);
            
            return SuccessResponse(new { RecordedAt = DateTime.UtcNow }, 
                $"Metric '{request.Name}' recorded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording custom metric {MetricName}", request?.Name);
            return HandleError(ex, "Failed to record custom metric");
        }
    }
}

public class RecordMetricRequest
{
    public required string Name { get; set; }
    public double Value { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}
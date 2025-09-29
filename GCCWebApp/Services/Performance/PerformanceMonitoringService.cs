using System.Collections.Concurrent;
using System.Diagnostics;
using GCCWebApp.Services.Security;

namespace GCCWebApp.Services.Performance;

public interface IPerformanceMonitoringService
{
    Task<T> MonitorAsync<T>(string operation, Func<Task<T>> action, object? metadata = null);
    Task MonitorAsync(string operation, Func<Task> action, object? metadata = null);
    void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null);
    PerformanceMetrics GetMetrics();
    void ResetMetrics();
}

public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly IStructuredLoggingService _structuredLogger;
    private readonly ConcurrentDictionary<string, PerformanceCounter> _counters;
    private readonly ConcurrentQueue<PerformanceMetric> _recentMetrics;
    private readonly int _maxRecentMetrics = 1000;

    public PerformanceMonitoringService(
        ILogger<PerformanceMonitoringService> logger,
        IStructuredLoggingService structuredLogger)
    {
        _logger = logger;
        _structuredLogger = structuredLogger;
        _counters = new ConcurrentDictionary<string, PerformanceCounter>();
        _recentMetrics = new ConcurrentQueue<PerformanceMetric>();
    }

    public async Task<T> MonitorAsync<T>(string operation, Func<Task<T>> action, object? metadata = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;
        
        try
        {
            var result = await action();
            stopwatch.Stop();
            
            await RecordSuccessfulOperation(operation, stopwatch.Elapsed, metadata);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordFailedOperation(operation, stopwatch.Elapsed, ex, metadata);
            throw;
        }
    }

    public async Task MonitorAsync(string operation, Func<Task> action, object? metadata = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await action();
            stopwatch.Stop();
            
            await RecordSuccessfulOperation(operation, stopwatch.Elapsed, metadata);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordFailedOperation(operation, stopwatch.Elapsed, ex, metadata);
            throw;
        }
    }

    public void RecordMetric(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var counter = _counters.GetOrAdd(metricName, _ => new PerformanceCounter());
        counter.RecordValue(value);
        
        var metric = new PerformanceMetric
        {
            Name = metricName,
            Value = value,
            Timestamp = DateTime.UtcNow,
            Tags = tags ?? new Dictionary<string, string>()
        };
        
        _recentMetrics.Enqueue(metric);
        
        // Keep recent metrics queue size manageable
        while (_recentMetrics.Count > _maxRecentMetrics)
        {
            _recentMetrics.TryDequeue(out _);
        }
        
        // Log significant metrics
        if (ShouldLogMetric(metricName, value))
        {
            _logger.LogInformation("Performance Metric: {MetricName} = {Value} {Tags}",
                metricName, value, tags != null ? string.Join(", ", tags.Select(kv => $"{kv.Key}={kv.Value}")) : "");
        }
    }

    public PerformanceMetrics GetMetrics()
    {
        var counters = _counters.ToDictionary(
            kv => kv.Key,
            kv => new PerformanceCounterSnapshot
            {
                Count = kv.Value.Count,
                TotalValue = kv.Value.TotalValue,
                MinValue = kv.Value.MinValue,
                MaxValue = kv.Value.MaxValue,
                AverageValue = kv.Value.AverageValue,
                LastUpdated = kv.Value.LastUpdated
            });

        var recentMetrics = _recentMetrics.ToArray();
        
        return new PerformanceMetrics
        {
            Counters = counters,
            RecentMetrics = recentMetrics,
            CollectedAt = DateTime.UtcNow
        };
    }

    public void ResetMetrics()
    {
        _counters.Clear();
        while (_recentMetrics.TryDequeue(out _)) { }
        _logger.LogInformation("Performance metrics reset");
    }

    private async Task RecordSuccessfulOperation(string operation, TimeSpan duration, object? metadata)
    {
        var durationMs = duration.TotalMilliseconds;
        
        // Record performance metrics
        RecordMetric($"{operation}_duration_ms", durationMs);
        RecordMetric($"{operation}_success_count", 1);
        RecordMetric("total_operations_count", 1);
        
        // Log to structured logging service
        await _structuredLogger.LogPerformanceMetricAsync(operation, duration, metadata);
        
        // Log slow operations
        if (duration.TotalSeconds > 5)
        {
            _logger.LogWarning("Slow operation detected: {Operation} took {DurationMs}ms", 
                operation, durationMs);
        }
        else if (duration.TotalSeconds > 1)
        {
            _logger.LogInformation("Operation performance: {Operation} took {DurationMs}ms", 
                operation, durationMs);
        }
    }

    private async Task RecordFailedOperation(string operation, TimeSpan duration, Exception exception, object? metadata)
    {
        var durationMs = duration.TotalMilliseconds;
        
        // Record failure metrics
        RecordMetric($"{operation}_duration_ms", durationMs);
        RecordMetric($"{operation}_error_count", 1);
        RecordMetric("total_errors_count", 1);
        
        // Log to structured logging service
        await _structuredLogger.LogExceptionAsync(
            exception, 
            $"Performance monitored operation: {operation}",
            null,
            new { Duration = duration, Metadata = metadata });
        
        _logger.LogError(exception, "Operation failed: {Operation} failed after {DurationMs}ms", 
            operation, durationMs);
    }

    private bool ShouldLogMetric(string metricName, double value)
    {
        // Log duration metrics if they're significant
        if (metricName.EndsWith("_duration_ms") && value > 1000) // > 1 second
            return true;
        
        // Log error counts
        if (metricName.EndsWith("_error_count"))
            return true;
        
        // Log specific business metrics
        var importantMetrics = new[]
        {
            "database_query_duration_ms",
            "api_request_duration_ms",
            "file_upload_duration_ms",
            "certificate_processing_duration_ms",
            "authentication_duration_ms"
        };
        
        return importantMetrics.Any(m => metricName.StartsWith(m));
    }
}

public class PerformanceCounter
{
    private readonly object _lock = new();
    private long _count;
    private double _totalValue;
    private double _minValue = double.MaxValue;
    private double _maxValue = double.MinValue;
    private DateTime _lastUpdated;

    public long Count => _count;
    public double TotalValue => _totalValue;
    public double MinValue => _minValue == double.MaxValue ? 0 : _minValue;
    public double MaxValue => _maxValue == double.MinValue ? 0 : _maxValue;
    public double AverageValue => _count > 0 ? _totalValue / _count : 0;
    public DateTime LastUpdated => _lastUpdated;

    public void RecordValue(double value)
    {
        lock (_lock)
        {
            _count++;
            _totalValue += value;
            _minValue = Math.Min(_minValue, value);
            _maxValue = Math.Max(_maxValue, value);
            _lastUpdated = DateTime.UtcNow;
        }
    }
}

public class PerformanceMetric
{
    public required string Name { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class PerformanceMetrics
{
    public Dictionary<string, PerformanceCounterSnapshot> Counters { get; set; } = new();
    public PerformanceMetric[] RecentMetrics { get; set; } = Array.Empty<PerformanceMetric>();
    public DateTime CollectedAt { get; set; }
}

public class PerformanceCounterSnapshot
{
    public long Count { get; set; }
    public double TotalValue { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double AverageValue { get; set; }
    public DateTime LastUpdated { get; set; }
}
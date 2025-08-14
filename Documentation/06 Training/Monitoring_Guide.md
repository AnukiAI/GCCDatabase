# GCC Database Application - Monitoring and Logging Guide

## Table of Contents
1. [Overview](#overview)
2. [Monitoring Strategy](#monitoring-strategy)
3. [Application Performance Monitoring](#application-performance-monitoring)
4. [Infrastructure Monitoring](#infrastructure-monitoring)
5. [Database Monitoring](#database-monitoring)
6. [Logging Architecture](#logging-architecture)
7. [Alerting and Notifications](#alerting-and-notifications)
8. [Metrics and KPIs](#metrics-and-kpis)
9. [Dashboards](#dashboards)
10. [Log Analysis](#log-analysis)
11. [Troubleshooting](#troubleshooting)
12. [Best Practices](#best-practices)

## Overview

This guide provides comprehensive monitoring and logging strategies for the GCC Database Web Application to ensure optimal performance, reliability, and quick issue resolution.

### Monitoring Objectives
- **Proactive Issue Detection**: Identify problems before users are affected
- **Performance Optimization**: Track and improve application performance
- **Security Monitoring**: Detect and respond to security threats
- **Business Intelligence**: Understand user behavior and system usage
- **Compliance**: Meet audit and regulatory requirements

### Key Components
- Application Performance Monitoring (APM)
- Infrastructure monitoring
- Database performance monitoring
- Centralized logging
- Real-time alerting
- Custom dashboards

## Monitoring Strategy

### Monitoring Layers
```
┌─────────────────────────────────────────┐
│             User Experience             │
│        (Synthetic & Real User)          │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│            Application Layer            │
│    (Performance, Errors, Business)     │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│           Infrastructure Layer          │
│      (CPU, Memory, Network, Disk)      │
└─────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────┐
│            Database Layer               │
│    (Queries, Connections, Storage)     │
└─────────────────────────────────────────┘
```

### Monitoring Philosophy
- **Four Golden Signals**: Latency, Traffic, Errors, Saturation
- **RED Method**: Rate, Errors, Duration
- **USE Method**: Utilization, Saturation, Errors
- **SLI/SLO Framework**: Service Level Indicators/Objectives

## Application Performance Monitoring

### Application Insights Integration
```csharp
// Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add Application Insights
    services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = Configuration.GetConnectionString("ApplicationInsights");
        options.EnableRequestTrackingTelemetryModule = true;
        options.EnableEventCounterCollectionModule = true;
        options.EnableDependencyTrackingTelemetryModule = true;
        options.EnablePerformanceCounterCollectionModule = true;
    });

    // Add custom telemetry initializers
    services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
    services.AddSingleton<ITelemetryProcessor, FilterTelemetryProcessor>();
}

public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ISupportProperties properties)
        {
            properties.Properties["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            properties.Properties["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
```

### Custom Metrics Tracking
```csharp
public class MetricsService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(TelemetryClient telemetryClient, ILogger<MetricsService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackCertificateCreated(string certificateType, string companyName)
    {
        _telemetryClient.TrackEvent("CertificateCreated", new Dictionary<string, string>
        {
            ["CertificateType"] = certificateType,
            ["CompanyName"] = companyName,
            ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });

        _telemetryClient.GetMetric("Certificates.Created").TrackValue(1);
    }

    public void TrackUserLogin(string username, bool successful, string method)
    {
        _telemetryClient.TrackEvent("UserLogin", new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Successful"] = successful.ToString(),
            ["Method"] = method
        });

        var metric = successful ? "Login.Success" : "Login.Failure";
        _telemetryClient.GetMetric(metric).TrackValue(1);
    }

    public void TrackDatabaseQuery(string queryName, TimeSpan duration, bool successful)
    {
        _telemetryClient.TrackDependency("SQL", queryName, DateTime.UtcNow.Subtract(duration), 
            duration, successful);

        _telemetryClient.GetMetric("Database.Query.Duration").TrackValue(duration.TotalMilliseconds);
    }

    public void TrackBusinessMetric(string metricName, double value, IDictionary<string, string> properties = null)
    {
        _telemetryClient.GetMetric(metricName).TrackValue(value);
        
        if (properties != null)
        {
            _telemetryClient.TrackEvent($"{metricName}.Tracked", properties);
        }
    }
}
```

### Performance Counters
```csharp
public class PerformanceMetrics
{
    private readonly TelemetryClient _telemetryClient;
    private readonly Timer _timer;

    public PerformanceMetrics(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
        _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void CollectMetrics(object state)
    {
        // CPU Usage
        var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _telemetryClient.GetMetric("System.CPU.Usage").TrackValue(cpuCounter.NextValue());

        // Memory Usage
        var memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        _telemetryClient.GetMetric("System.Memory.Available").TrackValue(memoryCounter.NextValue());

        // GC Statistics
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);
        
        _telemetryClient.GetMetric("GC.Gen0.Collections").TrackValue(gen0Collections);
        _telemetryClient.GetMetric("GC.Gen1.Collections").TrackValue(gen1Collections);
        _telemetryClient.GetMetric("GC.Gen2.Collections").TrackValue(gen2Collections);

        // Thread Pool
        ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);
        _telemetryClient.GetMetric("ThreadPool.WorkerThreads.Available").TrackValue(workerThreads);
        _telemetryClient.GetMetric("ThreadPool.IOThreads.Available").TrackValue(ioThreads);
    }
}
```

## Infrastructure Monitoring

### Azure Monitor Configuration
```json
{
  "AzureMonitor": {
    "WorkspaceId": "your-workspace-id",
    "PrimaryKey": "your-primary-key",
    "LogAnalytics": {
      "Enabled": true,
      "Categories": [
        "ApplicationInsights",
        "AzureDiagnostics",
        "ContainerInsights"
      ]
    }
  }
}
```

### Health Checks
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database")
        .AddUrlGroup(new Uri("https://api.external-service.com/health"), "external-api")
        .AddCheck<RedisHealthCheck>("redis")
        .AddCheck<DiskSpaceHealthCheck>("disk-space")
        .AddApplicationInsightsPublisher();

    services.Configure<HealthCheckPublisherOptions>(options =>
    {
        options.Delay = TimeSpan.FromSeconds(10);
        options.Period = TimeSpan.FromMinutes(1);
    });
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();
            
            return HealthCheckResult.Healthy("Redis connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}

public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
        var results = new List<string>();
        var isHealthy = true;

        foreach (var drive in drives)
        {
            var freeSpacePercentage = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
            
            if (freeSpacePercentage < 10) // Less than 10% free space
            {
                isHealthy = false;
                results.Add($"{drive.Name}: {freeSpacePercentage:F1}% free (Critical)");
            }
            else if (freeSpacePercentage < 20) // Less than 20% free space
            {
                results.Add($"{drive.Name}: {freeSpacePercentage:F1}% free (Warning)");
            }
        }

        return Task.FromResult(isHealthy
            ? HealthCheckResult.Healthy("Disk space is adequate", new { Details = results })
            : HealthCheckResult.Degraded("Disk space is low", new { Details = results }));
    }
}
```

### Container Monitoring
```yaml
# Docker Compose with monitoring
version: '3.8'
services:
  web:
    image: gcc-database:latest
    labels:
      - "prometheus.io/scrape=true"
      - "prometheus.io/port=5000"
      - "prometheus.io/path=/metrics"
    
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      
  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-storage:/var/lib/grafana
      
volumes:
  grafana-storage:
```

## Database Monitoring

### SQL Server Monitoring
```csharp
public class DatabaseMonitoringService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly TelemetryClient _telemetryClient;

    public async Task MonitorDatabasePerformance()
    {
        using var context = _dbContextFactory.CreateDbContext();

        // Monitor connection pool
        var connectionState = context.Database.GetConnectionString();
        _telemetryClient.TrackTrace($"Database connection: {connectionState}");

        // Monitor active connections
        var activeConnections = await GetActiveConnectionsAsync(context);
        _telemetryClient.GetMetric("Database.ActiveConnections").TrackValue(activeConnections);

        // Monitor query performance
        await MonitorSlowQueries(context);

        // Monitor deadlocks
        await MonitorDeadlocks(context);

        // Monitor index fragmentation
        await MonitorIndexFragmentation(context);
    }

    private async Task<int> GetActiveConnectionsAsync(AppDbContext context)
    {
        var query = @"
            SELECT COUNT(*) 
            FROM sys.dm_exec_sessions 
            WHERE is_user_process = 1 AND status = 'running'";

        return await context.Database.SqlQueryRaw<int>(query).FirstAsync();
    }

    private async Task MonitorSlowQueries(AppDbContext context)
    {
        var query = @"
            SELECT TOP 10
                qs.total_elapsed_time / qs.execution_count AS avg_elapsed_time,
                qs.execution_count,
                SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
                    CASE qs.statement_end_offset
                        WHEN -1 THEN DATALENGTH(qt.text)
                        ELSE qs.statement_end_offset
                    END - qs.statement_start_offset)/2 + 1) AS query_text
            FROM sys.dm_exec_query_stats AS qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS qt
            WHERE qs.total_elapsed_time / qs.execution_count > 1000000 -- 1 second
            ORDER BY avg_elapsed_time DESC";

        // Execute and log slow queries
        // Implementation details omitted for brevity
    }

    private async Task MonitorDeadlocks(AppDbContext context)
    {
        // Query for deadlock information from extended events or error log
        var query = @"
            SELECT 
                deadlock_time,
                victim_query,
                blocking_query
            FROM deadlock_monitoring_view
            WHERE deadlock_time > DATEADD(MINUTE, -5, GETDATE())";

        // Log deadlock information
        // Implementation details omitted for brevity
    }
}
```

### Database Alerts
```sql
-- Create alerts for database monitoring
EXEC msdb.dbo.sp_add_alert
    @name = N'GCC_Database_High_CPU',
    @message_id = 0,
    @severity = 0,
    @performance_condition = N'SQLServer:Resource Pool Stats|CPU usage %|default|>|80',
    @notification_message = N'Database CPU usage is above 80%';

EXEC msdb.dbo.sp_add_alert
    @name = N'GCC_Database_Deadlock',
    @message_id = 1205, -- Deadlock error
    @severity = 0,
    @notification_message = N'Deadlock detected in GCC Database';

EXEC msdb.dbo.sp_add_alert
    @name = N'GCC_Database_Log_Full',
    @message_id = 9002, -- Transaction log full
    @severity = 0,
    @notification_message = N'Transaction log is full in GCC Database';
```

## Logging Architecture

### Structured Logging with Serilog
```csharp
public void ConfigureServices(IServiceCollection services)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/gcc-.log", 
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30)
        .WriteTo.ApplicationInsights(
            Configuration.GetConnectionString("ApplicationInsights"),
            TelemetryConverter.Traces)
        .CreateLogger();

    services.AddLogging(builder => builder.AddSerilog());
}
```

### Logging Middleware
```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["UserId"] = context.User.Identity?.Name,
            ["UserIP"] = context.Connection.RemoteIpAddress?.ToString()
        }))
        {
            _logger.LogInformation("Request started: {Method} {Path} {QueryString}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Request completed: {Method} {Path} {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
```

### Application Logging Patterns
```csharp
public class CertificateService
{
    private readonly ILogger<CertificateService> _logger;

    public async Task<Certificate> CreateAsync(Certificate certificate)
    {
        using var activity = Activity.StartActivity("CreateCertificate");
        
        _logger.LogInformation("Creating certificate {CertificateNumber} for company {CompanyId}",
            certificate.Number, certificate.CompanyId);

        try
        {
            // Validate certificate
            if (await _repository.ExistsAsync(c => c.Number == certificate.Number))
            {
                _logger.LogWarning("Certificate creation failed: Duplicate number {CertificateNumber}",
                    certificate.Number);
                throw new DuplicateException($"Certificate {certificate.Number} already exists");
            }

            var result = await _repository.AddAsync(certificate);
            
            _logger.LogInformation("Certificate created successfully: {CertificateId} {CertificateNumber}",
                result.Id, result.Number);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create certificate {CertificateNumber}: {ErrorMessage}",
                certificate.Number, ex.Message);
            throw;
        }
    }
}
```

## Alerting and Notifications

### Alert Configuration
```csharp
public class AlertingService
{
    public async Task ConfigureAlerts()
    {
        var alerts = new[]
        {
            new AlertRule
            {
                Name = "High Error Rate",
                Condition = "requests/failed > 50 in 5 minutes",
                Action = NotifyOncall | CreateIncident,
                Severity = AlertSeverity.Critical
            },
            new AlertRule
            {
                Name = "Slow Response Time",
                Condition = "requests/duration > 5000ms for 90th percentile in 10 minutes",
                Action = NotifyDevelopers,
                Severity = AlertSeverity.Warning
            },
            new AlertRule
            {
                Name = "Database Connection Failure",
                Condition = "health_check/database = false",
                Action = NotifyOncall | NotifyDBA,
                Severity = AlertSeverity.Critical
            }
        };

        foreach (var alert in alerts)
        {
            await CreateAlert(alert);
        }
    }
}

public class NotificationService
{
    public async Task SendAlert(AlertRule rule, AlertTrigger trigger)
    {
        var message = new AlertMessage
        {
            Title = $"[{rule.Severity}] {rule.Name}",
            Description = trigger.Description,
            Timestamp = trigger.Timestamp,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            RunbookUrl = GetRunbookUrl(rule.Name)
        };

        if (rule.Action.HasFlag(NotifyOncall))
        {
            await SendPagerDutyAlert(message);
        }

        if (rule.Action.HasFlag(NotifyDevelopers))
        {
            await SendSlackAlert(message, "#development");
        }

        if (rule.Action.HasFlag(NotifyDBA))
        {
            await SendEmailAlert(message, "dba@company.com");
        }
    }
}
```

### Azure Monitor Alerts
```json
{
  "type": "Microsoft.Insights/metricAlerts",
  "apiVersion": "2018-03-01",
  "name": "GCC-High-CPU-Alert",
  "properties": {
    "description": "Alert when CPU usage exceeds 80%",
    "severity": 2,
    "enabled": true,
    "scopes": [
      "/subscriptions/{subscription-id}/resourceGroups/gcc-prod-rg/providers/Microsoft.Web/sites/gcc-database-app"
    ],
    "evaluationFrequency": "PT1M",
    "windowSize": "PT5M",
    "criteria": {
      "odata.type": "Microsoft.Azure.Monitor.MultipleResourceMultipleMetricCriteria",
      "allOf": [
        {
          "name": "High CPU Usage",
          "metricName": "CpuPercentage",
          "operator": "GreaterThan",
          "threshold": 80,
          "timeAggregation": "Average"
        }
      ]
    },
    "actions": [
      {
        "actionGroupId": "/subscriptions/{subscription-id}/resourceGroups/gcc-prod-rg/providers/Microsoft.Insights/actionGroups/GCC-AlertActionGroup"
      }
    ]
  }
}
```

## Metrics and KPIs

### Application Metrics
```csharp
public class ApplicationMetrics
{
    public static readonly string[] CoreMetrics = {
        "Requests.Total",
        "Requests.Failed",
        "Requests.Duration",
        "Certificates.Created",
        "Certificates.Updated",
        "Users.Active",
        "Database.Queries.Duration",
        "Cache.HitRate"
    };

    public static readonly Dictionary<string, string> BusinessMetrics = new()
    {
        ["Certificates.Created.Daily"] = "Number of certificates created per day",
        ["Companies.Active"] = "Number of active companies",
        ["Users.LoginRate"] = "Daily user login rate",
        ["Search.Queries"] = "Number of search queries per hour",
        ["API.Calls"] = "API calls per minute",
        ["Data.Export.Requests"] = "Data export requests per day"
    };
}

public class MetricsCollector
{
    private readonly TelemetryClient _telemetryClient;

    public void CollectBusinessMetrics()
    {
        var dailyCertificates = GetDailyCertificateCount();
        _telemetryClient.GetMetric("Business.Certificates.Daily").TrackValue(dailyCertificates);

        var activeUsers = GetActiveUserCount();
        _telemetryClient.GetMetric("Business.Users.Active").TrackValue(activeUsers);

        var searchVolume = GetSearchVolume();
        _telemetryClient.GetMetric("Business.Search.Volume").TrackValue(searchVolume);
    }
}
```

### SLA Metrics
```csharp
public class SlaMetrics
{
    public static readonly Dictionary<string, SlaTarget> Targets = new()
    {
        ["Availability"] = new SlaTarget { Target = 99.9, Unit = "%" },
        ["ResponseTime"] = new SlaTarget { Target = 2000, Unit = "ms" },
        ["ErrorRate"] = new SlaTarget { Target = 1, Unit = "%" },
        ["Throughput"] = new SlaTarget { Target = 1000, Unit = "req/min" }
    };

    public async Task<SlaReport> GenerateReport(DateTime from, DateTime to)
    {
        var availability = await CalculateAvailability(from, to);
        var responseTime = await CalculateAverageResponseTime(from, to);
        var errorRate = await CalculateErrorRate(from, to);
        var throughput = await CalculateAverageThroughput(from, to);

        return new SlaReport
        {
            Period = $"{from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
            Metrics = new Dictionary<string, SlaMetric>
            {
                ["Availability"] = new() { Value = availability, Target = 99.9, Met = availability >= 99.9 },
                ["ResponseTime"] = new() { Value = responseTime, Target = 2000, Met = responseTime <= 2000 },
                ["ErrorRate"] = new() { Value = errorRate, Target = 1, Met = errorRate <= 1 },
                ["Throughput"] = new() { Value = throughput, Target = 1000, Met = throughput >= 1000 }
            }
        };
    }
}
```

## Dashboards

### Grafana Dashboard Configuration
```json
{
  "dashboard": {
    "title": "GCC Database Application",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])",
            "legendFormat": "{{method}} {{status}}"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "Error Rate",
        "type": "stat",
        "targets": [
          {
            "expr": "rate(http_requests_total{status=~\"5..\"}[5m]) / rate(http_requests_total[5m]) * 100",
            "legendFormat": "Error Rate %"
          }
        ]
      },
      {
        "title": "Active Users",
        "type": "stat",
        "targets": [
          {
            "expr": "active_users_total",
            "legendFormat": "Active Users"
          }
        ]
      }
    ]
  }
}
```

### Application Insights Workbook
```json
{
  "version": "Notebook/1.0",
  "items": [
    {
      "type": 3,
      "content": {
        "version": "KqlItem/1.0",
        "query": "requests\n| where timestamp > ago(1h)\n| summarize count() by bin(timestamp, 5m), resultCode\n| render columnchart",
        "size": 0,
        "title": "Request Rate by Status Code"
      }
    },
    {
      "type": 3,
      "content": {
        "version": "KqlItem/1.0",
        "query": "requests\n| where timestamp > ago(1h)\n| summarize avg(duration) by bin(timestamp, 5m)\n| render linechart",
        "size": 0,
        "title": "Average Response Time"
      }
    },
    {
      "type": 3,
      "content": {
        "version": "KqlItem/1.0",
        "query": "exceptions\n| where timestamp > ago(1h)\n| summarize count() by type\n| order by count_ desc\n| take 10",
        "size": 0,
        "title": "Top Exceptions"
      }
    }
  ]
}
```

## Log Analysis

### Kusto Queries (Application Insights)
```kql
-- Request analysis
requests
| where timestamp > ago(1h)
| summarize 
    RequestCount = count(),
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    ErrorCount = countif(success == false)
by bin(timestamp, 5m)
| order by timestamp desc

-- Error analysis
exceptions
| where timestamp > ago(24h)
| extend ErrorMessage = tostring(details[0].message)
| summarize 
    Count = count(),
    FirstOccurrence = min(timestamp),
    LastOccurrence = max(timestamp)
by type, ErrorMessage
| order by Count desc

-- User activity analysis
customEvents
| where timestamp > ago(7d)
| where name == "UserLogin"
| extend Username = tostring(customDimensions["Username"])
| summarize 
    LoginCount = count(),
    LastLogin = max(timestamp)
by Username
| order by LoginCount desc

-- Performance troubleshooting
dependencies
| where timestamp > ago(1h)
| where type == "SQL"
| where duration > 5000 -- Queries taking more than 5 seconds
| project timestamp, name, duration, success, data
| order by duration desc
```

### ELK Stack Configuration
```yaml
# Elasticsearch configuration
elasticsearch:
  cluster.name: gcc-logs
  node.name: gcc-node-1
  network.host: 0.0.0.0
  discovery.type: single-node
  xpack.security.enabled: false

# Logstash pipeline
input {
  beats {
    port => 5044
  }
}

filter {
  if [fields][service] == "gcc-web" {
    grok {
      match => { "message" => "%{TIMESTAMP_ISO8601:timestamp} \[%{LOGLEVEL:level}\] %{GREEDYDATA:message}" }
    }
    
    date {
      match => [ "timestamp", "yyyy-MM-dd HH:mm:ss.SSS" ]
    }
  }
}

output {
  elasticsearch {
    hosts => ["elasticsearch:9200"]
    index => "gcc-logs-%{+YYYY.MM.dd}"
  }
}

# Kibana dashboards
kibana:
  server.host: "0.0.0.0"
  elasticsearch.hosts: ["http://elasticsearch:9200"]
```

## Troubleshooting

### Common Monitoring Issues

#### High Memory Usage
```csharp
public class MemoryDiagnostics
{
    public static void LogMemoryUsage(ILogger logger)
    {
        var gcInfo = GC.GetTotalMemory(false);
        var workingSet = Environment.WorkingSet;
        
        logger.LogInformation("Memory Usage - GC: {GCMemory} bytes, Working Set: {WorkingSet} bytes",
            gcInfo, workingSet);

        for (int gen = 0; gen <= 2; gen++)
        {
            var collections = GC.CollectionCount(gen);
            logger.LogInformation("GC Gen {Generation}: {Collections} collections", gen, collections);
        }
    }
}
```

#### Performance Issues
```csharp
public class PerformanceDiagnostics
{
    public static async Task DiagnoseSlowRequests(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Add diagnostic headers
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            context.Response.Headers.Add("X-Response-Time", stopwatch.ElapsedMilliseconds.ToString());
            return Task.CompletedTask;
        });
    }
}
```

### Monitoring Health Check
```csharp
public class MonitoringHealthCheck : IHealthCheck
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<MonitoringHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if telemetry is working
            _telemetryClient.TrackEvent("HealthCheck.Monitoring");
            
            // Check log aggregation
            _logger.LogInformation("Monitoring health check executed");
            
            return HealthCheckResult.Healthy("Monitoring systems are functional");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Monitoring systems are not working", ex);
        }
    }
}
```

## Best Practices

### Monitoring Best Practices
1. **Monitor Everything**: Application, infrastructure, business metrics
2. **Set Meaningful Alerts**: Avoid alert fatigue with relevant thresholds
3. **Use Structured Logging**: Consistent log formats for better analysis
4. **Implement Circuit Breakers**: Prevent cascade failures
5. **Monitor User Experience**: Real user monitoring and synthetic tests

### Logging Best Practices
1. **Log Levels**: Use appropriate levels (Debug, Info, Warn, Error, Fatal)
2. **Contextual Information**: Include relevant context in log messages
3. **Avoid Sensitive Data**: Never log passwords, credit cards, etc.
4. **Performance Aware**: Logging shouldn't impact performance
5. **Centralized Logging**: Aggregate logs from all sources

### Alert Best Practices
1. **Actionable Alerts**: Every alert should require human action
2. **Clear Runbooks**: Include troubleshooting steps with alerts
3. **Escalation Procedures**: Define who to contact and when
4. **Test Alerts**: Regularly verify alert configurations work
5. **Post-Incident Reviews**: Learn from every incident

## Support

For monitoring support:
- **DevOps Team**: devops@gccdatabase.com
- **On-Call**: +1-555-0100
- **Monitoring Dashboard**: https://monitoring.gccdatabase.com
- **Documentation**: /Documentation/Monitoring_Guide.md

## Version History

- **v1.0.0** - Initial monitoring guide
- Last Updated: August 2024
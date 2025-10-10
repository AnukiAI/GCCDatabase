# Performance Monitoring Plan - GCC Database Solution

## Overview
This document defines the comprehensive performance monitoring strategy for the GCC Database Solution, establishing monitoring frameworks, performance baselines, alerting thresholds, and optimization procedures to ensure optimal system performance and user experience.

## Performance Objectives

### Performance Targets

#### Application Performance
| Metric | Target | Warning Threshold | Critical Threshold |
|--------|--------|------------------|-------------------|
| Page Load Time | < 2 seconds | > 3 seconds | > 5 seconds |
| API Response Time | < 500ms | > 1 second | > 2 seconds |
| Database Query Time | < 100ms | > 500ms | > 1 second |
| Certificate Search | < 1 second | > 2 seconds | > 5 seconds |
| PDF Generation | < 3 seconds | > 5 seconds | > 10 seconds |

#### System Performance
| Metric | Target | Warning Threshold | Critical Threshold |
|--------|--------|------------------|-------------------|
| CPU Utilization | < 70% | > 80% | > 90% |
| Memory Usage | < 75% | > 85% | > 95% |
| Disk I/O Wait | < 10% | > 20% | > 40% |
| Network Utilization | < 60% | > 75% | > 90% |
| Disk Space Usage | < 80% | > 85% | > 95% |

#### Business Performance
| Metric | Target | Warning Threshold | Critical Threshold |
|--------|--------|------------------|-------------------|
| User Session Duration | > 5 minutes | < 2 minutes | < 1 minute |
| Certificate Creation Rate | > 100/day | < 50/day | < 25/day |
| Search Success Rate | > 95% | < 90% | < 85% |
| API Success Rate | > 99% | < 97% | < 95% |
| User Error Rate | < 1% | > 2% | > 5% |

### Service Level Agreements (SLA)

#### Availability SLA
- **Production Uptime**: 99.5% (4.38 hours downtime/month)
- **Planned Maintenance**: < 2 hours/month
- **Unplanned Downtime**: < 2 hours/month
- **Database Availability**: 99.9%

#### Performance SLA
- **95th Percentile Response Time**: < 2 seconds
- **Mean Response Time**: < 1 second
- **Time to First Byte**: < 500ms
- **Concurrent User Support**: 500 users

## Performance Monitoring Framework

### Application Performance Monitoring (APM)

#### ASP.NET Core Performance Instrumentation
```csharp
// Performance monitoring middleware
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly DiagnosticSource _diagnosticSource;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        try
        {
            context.Items["RequestId"] = requestId;
            context.Items["StartTime"] = DateTime.UtcNow;

            // Track incoming request
            _metrics.Counter("gcc_http_requests_total")
                .WithTag("method", context.Request.Method)
                .WithTag("endpoint", GetEndpointName(context))
                .Increment();

            await _next(context);

            // Track successful response
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalMilliseconds;

            _metrics.Histogram("gcc_http_request_duration_ms")
                .WithTag("method", context.Request.Method)
                .WithTag("endpoint", GetEndpointName(context))
                .WithTag("status_code", context.Response.StatusCode.ToString())
                .Observe(duration);

            // Log slow requests
            if (duration > 2000)
            {
                _logger.LogWarning("Slow request detected: {RequestId} - {Method} {Path} took {Duration}ms",
                    requestId, context.Request.Method, context.Request.Path, duration);
            }

            // Track user experience metrics
            TrackUserExperienceMetrics(context, duration);

        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _metrics.Counter("gcc_http_requests_errors_total")
                .WithTag("method", context.Request.Method)
                .WithTag("endpoint", GetEndpointName(context))
                .WithTag("exception", ex.GetType().Name)
                .Increment();

            _logger.LogError(ex, "Request failed: {RequestId} - {Method} {Path}",
                requestId, context.Request.Method, context.Request.Path);

            throw;
        }
    }

    private void TrackUserExperienceMetrics(HttpContext context, double duration)
    {
        var endpoint = GetEndpointName(context);

        // Track Core Web Vitals equivalent metrics
        if (IsPageRequest(context))
        {
            _metrics.Histogram("gcc_page_load_time_ms")
                .WithTag("page", endpoint)
                .Observe(duration);

            // Simulate LCP (Largest Contentful Paint) tracking
            var lcp = duration * 1.2; // Estimate based on server response time
            _metrics.Histogram("gcc_largest_contentful_paint_ms")
                .WithTag("page", endpoint)
                .Observe(lcp);
        }

        // Track API performance
        if (IsApiRequest(context))
        {
            _metrics.Histogram("gcc_api_response_time_ms")
                .WithTag("endpoint", endpoint)
                .Observe(duration);
        }
    }
}
```

#### Database Performance Monitoring
```csharp
// Database performance interceptor
public class DatabasePerformanceInterceptor : DbCommandInterceptor
{
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<DatabasePerformanceInterceptor> _logger;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        eventData.Context.Items["CommandStartTime"] = DateTime.UtcNow;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        TrackQueryPerformance(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void TrackQueryPerformance(DbCommand command, CommandExecutedEventData eventData)
    {
        var startTime = (DateTime)eventData.Context.Items["CommandStartTime"];
        var duration = DateTime.UtcNow - startTime;

        var queryType = GetQueryType(command.CommandText);
        var tableName = ExtractTableName(command.CommandText);

        _metrics.Histogram("gcc_database_query_duration_ms")
            .WithTag("query_type", queryType)
            .WithTag("table", tableName)
            .Observe(duration.TotalMilliseconds);

        // Log slow queries
        if (duration.TotalMilliseconds > 1000)
        {
            _logger.LogWarning("Slow query detected: {Duration}ms - {Query}",
                duration.TotalMilliseconds, command.CommandText);

            // Store slow query details for analysis
            _metrics.Counter("gcc_slow_queries_total")
                .WithTag("query_type", queryType)
                .WithTag("table", tableName)
                .Increment();
        }
    }

    private string GetQueryType(string sql)
    {
        var trimmed = sql.TrimStart().ToUpper();
        if (trimmed.StartsWith("SELECT")) return "SELECT";
        if (trimmed.StartsWith("INSERT")) return "INSERT";
        if (trimmed.StartsWith("UPDATE")) return "UPDATE";
        if (trimmed.StartsWith("DELETE")) return "DELETE";
        return "OTHER";
    }
}
```

### Infrastructure Performance Monitoring

#### System Metrics Collection
```bash
#!/bin/bash
# System performance metrics collection script

METRICS_FILE="/tmp/gcc-metrics-$(date +%Y%m%d-%H%M%S).json"

echo "Collecting system performance metrics..."

# CPU metrics
CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
LOAD_AVERAGE=$(uptime | awk -F'load average:' '{print $2}' | awk '{print $1}' | tr -d ',')

# Memory metrics
MEM_TOTAL=$(free -m | awk 'NR==2{printf "%.0f", $2}')
MEM_USED=$(free -m | awk 'NR==2{printf "%.0f", $3}')
MEM_USAGE=$(free -m | awk 'NR==2{printf "%.1f", $3*100/$2}')

# Disk metrics
DISK_USAGE=$(df -h / | awk 'NR==2 {print $5}' | tr -d '%')
DISK_IOWAIT=$(iostat -c 1 2 | tail -1 | awk '{print $4}')

# Network metrics
RX_BYTES=$(cat /proc/net/dev | grep eth0 | awk '{print $2}')
TX_BYTES=$(cat /proc/net/dev | grep eth0 | awk '{print $10}')

# Application-specific metrics
APP_PROCESSES=$(pgrep -c dotnet)
APP_MEMORY=$(ps -aux | grep dotnet | awk '{sum+=$6} END {printf "%.0f", sum/1024}')

# Database metrics
if command -v sqlcmd &> /dev/null; then
    DB_CONNECTIONS=$(sqlcmd -S localhost -E -Q "SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1" -h -1)
    DB_CPU=$(sqlcmd -S localhost -E -Q "SELECT AVG(cpu_percent) FROM sys.dm_db_resource_stats WHERE end_time > DATEADD(minute, -5, GETUTCDATE())" -h -1)
else
    DB_CONNECTIONS="N/A"
    DB_CPU="N/A"
fi

# Create JSON metrics
cat > "$METRICS_FILE" << EOF
{
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
    "system": {
        "cpu_usage_percent": $CPU_USAGE,
        "load_average": $LOAD_AVERAGE,
        "memory_total_mb": $MEM_TOTAL,
        "memory_used_mb": $MEM_USED,
        "memory_usage_percent": $MEM_USAGE,
        "disk_usage_percent": $DISK_USAGE,
        "disk_iowait_percent": $DISK_IOWAIT
    },
    "network": {
        "rx_bytes": $RX_BYTES,
        "tx_bytes": $TX_BYTES
    },
    "application": {
        "process_count": $APP_PROCESSES,
        "memory_usage_mb": $APP_MEMORY
    },
    "database": {
        "active_connections": "$DB_CONNECTIONS",
        "cpu_percent": "$DB_CPU"
    }
}
EOF

# Send metrics to monitoring system
if command -v curl &> /dev/null; then
    curl -X POST -H "Content-Type: application/json" \
         -d @"$METRICS_FILE" \
         http://localhost:8080/api/metrics/system 2>/dev/null || true
fi

echo "Metrics collected: $METRICS_FILE"
```

#### Prometheus Integration
```yaml
# prometheus.yml configuration for GCC monitoring
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "gcc_performance_rules.yml"

scrape_configs:
  - job_name: 'gcc-webapp'
    static_configs:
      - targets: ['localhost:8080']
    metrics_path: '/metrics'
    scrape_interval: 10s

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['localhost:9100']

  - job_name: 'sql-server-exporter'
    static_configs:
      - targets: ['localhost:9399']

  - job_name: 'blackbox-exporter'
    static_configs:
      - targets: ['localhost:9115']
    params:
      module: [http_2xx]
      target: ['http://localhost:8080/health']

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093
```

### Performance Baselines and Benchmarking

#### Load Testing Framework
```csharp
// Performance testing service
public class PerformanceTestingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PerformanceTestingService> _logger;

    public async Task<PerformanceTestResult> RunPerformanceTest(PerformanceTestConfig config)
    {
        var results = new List<RequestResult>();
        var tasks = new List<Task>();

        var startTime = DateTime.UtcNow;

        // Simulate concurrent users
        for (int i = 0; i < config.ConcurrentUsers; i++)
        {
            tasks.Add(SimulateUser(config, results, i));
        }

        await Task.WhenAll(tasks);

        var endTime = DateTime.UtcNow;

        return AnalyzeResults(results, startTime, endTime, config);
    }

    private async Task SimulateUser(PerformanceTestConfig config, List<RequestResult> results, int userId)
    {
        var random = new Random();

        for (int i = 0; i < config.RequestsPerUser; i++)
        {
            var scenario = config.Scenarios[random.Next(config.Scenarios.Count)];
            var result = await ExecuteScenario(scenario, userId, i);

            lock (results)
            {
                results.Add(result);
            }

            // Think time between requests
            if (config.ThinkTimeMs > 0)
            {
                await Task.Delay(random.Next(config.ThinkTimeMs / 2, config.ThinkTimeMs * 2));
            }
        }
    }

    private async Task<RequestResult> ExecuteScenario(TestScenario scenario, int userId, int iteration)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new RequestResult
        {
            UserId = userId,
            Iteration = iteration,
            ScenarioName = scenario.Name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            var response = await _httpClient.SendAsync(scenario.Request);
            stopwatch.Stop();

            result.Duration = stopwatch.Elapsed;
            result.StatusCode = (int)response.StatusCode;
            result.Success = response.IsSuccessStatusCode;
            result.ResponseSize = response.Content.Headers.ContentLength ?? 0;

            // Validate response content if specified
            if (scenario.ExpectedContent != null)
            {
                var content = await response.Content.ReadAsStringAsync();
                result.ContentValid = content.Contains(scenario.ExpectedContent);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Error = ex.Message;
            return result;
        }
    }

    private PerformanceTestResult AnalyzeResults(List<RequestResult> results, DateTime startTime, DateTime endTime, PerformanceTestConfig config)
    {
        var successfulRequests = results.Where(r => r.Success).ToList();
        var durations = successfulRequests.Select(r => r.Duration.TotalMilliseconds).OrderBy(d => d).ToList();

        return new PerformanceTestResult
        {
            TestConfig = config,
            StartTime = startTime,
            EndTime = endTime,
            TotalDuration = endTime - startTime,
            TotalRequests = results.Count,
            SuccessfulRequests = successfulRequests.Count,
            FailedRequests = results.Count - successfulRequests.Count,
            SuccessRate = (double)successfulRequests.Count / results.Count * 100,
            AverageResponseTime = durations.Any() ? durations.Average() : 0,
            MedianResponseTime = durations.Any() ? GetPercentile(durations, 0.5) : 0,
            P95ResponseTime = durations.Any() ? GetPercentile(durations, 0.95) : 0,
            P99ResponseTime = durations.Any() ? GetPercentile(durations, 0.99) : 0,
            MinResponseTime = durations.Any() ? durations.Min() : 0,
            MaxResponseTime = durations.Any() ? durations.Max() : 0,
            RequestsPerSecond = results.Count / (endTime - startTime).TotalSeconds,
            ErrorsByType = results.Where(r => !r.Success)
                .GroupBy(r => r.Error ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}
```

#### Performance Test Scenarios
```json
{
  "performance_test_config": {
    "concurrent_users": 50,
    "requests_per_user": 20,
    "think_time_ms": 1000,
    "scenarios": [
      {
        "name": "home_page_load",
        "weight": 20,
        "request": {
          "method": "GET",
          "url": "/",
          "headers": {
            "User-Agent": "PerformanceTest/1.0"
          }
        },
        "expected_content": "Certificate Database"
      },
      {
        "name": "certificate_search",
        "weight": 30,
        "request": {
          "method": "GET",
          "url": "/Certificate/CertificateTileView?search=eco",
          "headers": {
            "User-Agent": "PerformanceTest/1.0"
          }
        },
        "expected_content": "certificates"
      },
      {
        "name": "certificate_details",
        "weight": 25,
        "request": {
          "method": "GET",
          "url": "/Certificate/Details/1",
          "headers": {
            "User-Agent": "PerformanceTest/1.0"
          }
        },
        "expected_content": "Certificate Details"
      },
      {
        "name": "api_certificates",
        "weight": 15,
        "request": {
          "method": "GET",
          "url": "/api/CertificateTileExternal/tiles",
          "headers": {
            "User-Agent": "PerformanceTest/1.0",
            "Accept": "application/json"
          }
        },
        "expected_content": "certificates"
      },
      {
        "name": "pdf_download",
        "weight": 10,
        "request": {
          "method": "GET",
          "url": "/Certificate/Download/1",
          "headers": {
            "User-Agent": "PerformanceTest/1.0"
          }
        },
        "expected_content_type": "application/pdf"
      }
    ]
  }
}
```

### Real User Monitoring (RUM)

#### Client-Side Performance Tracking
```javascript
// Real User Monitoring (RUM) implementation
class GCCPerformanceMonitor {
    constructor() {
        this.sessionId = this.generateSessionId();
        this.pageLoadStart = performance.now();
        this.metrics = [];

        this.initializeMonitoring();
    }

    initializeMonitoring() {
        // Track page load performance
        window.addEventListener('load', () => {
            this.trackPageLoad();
        });

        // Track user interactions
        document.addEventListener('click', (e) => {
            this.trackUserInteraction(e);
        });

        // Track AJAX requests
        this.interceptXHR();

        // Track Core Web Vitals
        this.trackCoreWebVitals();

        // Send metrics periodically
        setInterval(() => {
            this.sendMetrics();
        }, 30000);
    }

    trackPageLoad() {
        const navigation = performance.getEntriesByType('navigation')[0];
        const paint = performance.getEntriesByType('paint');

        const metrics = {
            type: 'page_load',
            url: window.location.pathname,
            session_id: this.sessionId,
            timestamp: Date.now(),
            metrics: {
                dns_lookup: navigation.domainLookupEnd - navigation.domainLookupStart,
                tcp_connect: navigation.connectEnd - navigation.connectStart,
                ssl_handshake: navigation.secureConnectionStart > 0 ?
                    navigation.connectEnd - navigation.secureConnectionStart : 0,
                request_time: navigation.responseStart - navigation.requestStart,
                response_time: navigation.responseEnd - navigation.responseStart,
                dom_processing: navigation.domComplete - navigation.domLoading,
                page_load_time: navigation.loadEventEnd - navigation.navigationStart,
                first_paint: paint.find(p => p.name === 'first-paint')?.startTime || 0,
                first_contentful_paint: paint.find(p => p.name === 'first-contentful-paint')?.startTime || 0
            }
        };

        this.metrics.push(metrics);
    }

    trackCoreWebVitals() {
        // Largest Contentful Paint (LCP)
        new PerformanceObserver((entryList) => {
            const entries = entryList.getEntries();
            const lastEntry = entries[entries.length - 1];

            this.metrics.push({
                type: 'lcp',
                value: lastEntry.startTime,
                url: window.location.pathname,
                session_id: this.sessionId,
                timestamp: Date.now()
            });
        }).observe({ entryTypes: ['largest-contentful-paint'] });

        // First Input Delay (FID)
        new PerformanceObserver((entryList) => {
            const entries = entryList.getEntries();
            entries.forEach(entry => {
                this.metrics.push({
                    type: 'fid',
                    value: entry.processingStart - entry.startTime,
                    url: window.location.pathname,
                    session_id: this.sessionId,
                    timestamp: Date.now()
                });
            });
        }).observe({ entryTypes: ['first-input'] });

        // Cumulative Layout Shift (CLS)
        let clsValue = 0;
        new PerformanceObserver((entryList) => {
            const entries = entryList.getEntries();
            entries.forEach(entry => {
                if (!entry.hadRecentInput) {
                    clsValue += entry.value;
                }
            });

            this.metrics.push({
                type: 'cls',
                value: clsValue,
                url: window.location.pathname,
                session_id: this.sessionId,
                timestamp: Date.now()
            });
        }).observe({ entryTypes: ['layout-shift'] });
    }

    trackUserInteraction(event) {
        const startTime = performance.now();

        // Track time to next paint after interaction
        requestAnimationFrame(() => {
            const duration = performance.now() - startTime;

            this.metrics.push({
                type: 'interaction',
                element: event.target.tagName,
                element_id: event.target.id,
                interaction_type: 'click',
                response_time: duration,
                url: window.location.pathname,
                session_id: this.sessionId,
                timestamp: Date.now()
            });
        });
    }

    interceptXHR() {
        const originalOpen = XMLHttpRequest.prototype.open;
        const originalSend = XMLHttpRequest.prototype.send;

        XMLHttpRequest.prototype.open = function(method, url) {
            this._method = method;
            this._url = url;
            this._startTime = performance.now();
            return originalOpen.apply(this, arguments);
        };

        XMLHttpRequest.prototype.send = function() {
            this.addEventListener('loadend', () => {
                const duration = performance.now() - this._startTime;

                window.gccMonitor.metrics.push({
                    type: 'xhr',
                    method: this._method,
                    url: this._url,
                    status: this.status,
                    duration: duration,
                    session_id: window.gccMonitor.sessionId,
                    timestamp: Date.now()
                });
            });

            return originalSend.apply(this, arguments);
        };
    }

    sendMetrics() {
        if (this.metrics.length === 0) return;

        const metricsToSend = [...this.metrics];
        this.metrics = [];

        fetch('/api/performance/rum', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                session_id: this.sessionId,
                user_agent: navigator.userAgent,
                metrics: metricsToSend
            })
        }).catch(error => {
            console.error('Failed to send performance metrics:', error);
            // Re-add failed metrics for retry
            this.metrics.unshift(...metricsToSend);
        });
    }

    generateSessionId() {
        return 'session_' + Math.random().toString(36).substr(2, 9) + '_' + Date.now();
    }
}

// Initialize monitoring
window.addEventListener('DOMContentLoaded', () => {
    window.gccMonitor = new GCCPerformanceMonitor();
});
```

## Performance Analysis and Reporting

### Performance Dashboard
```html
<!DOCTYPE html>
<html>
<head>
    <title>GCC Performance Dashboard</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        .dashboard { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 20px; }
        .metric-card { border: 1px solid #ddd; padding: 20px; border-radius: 5px; }
        .metric-value { font-size: 2em; font-weight: bold; }
        .metric-good { color: #28a745; }
        .metric-warning { color: #ffc107; }
        .metric-bad { color: #dc3545; }
    </style>
</head>
<body>
    <h1>GCC Performance Dashboard</h1>

    <div class="dashboard">
        <!-- Application Performance -->
        <div class="metric-card">
            <h3>Application Performance</h3>
            <div>Average Response Time: <span id="avg-response-time" class="metric-value">--</span>ms</div>
            <div>95th Percentile: <span id="p95-response-time" class="metric-value">--</span>ms</div>
            <canvas id="response-time-chart" width="300" height="200"></canvas>
        </div>

        <!-- Database Performance -->
        <div class="metric-card">
            <h3>Database Performance</h3>
            <div>Query Time: <span id="avg-query-time" class="metric-value">--</span>ms</div>
            <div>Active Connections: <span id="db-connections" class="metric-value">--</span></div>
            <canvas id="db-performance-chart" width="300" height="200"></canvas>
        </div>

        <!-- System Resources -->
        <div class="metric-card">
            <h3>System Resources</h3>
            <div>CPU Usage: <span id="cpu-usage" class="metric-value">--</span>%</div>
            <div>Memory Usage: <span id="memory-usage" class="metric-value">--</span>%</div>
            <canvas id="system-resources-chart" width="300" height="200"></canvas>
        </div>

        <!-- User Experience -->
        <div class="metric-card">
            <h3>User Experience</h3>
            <div>Page Load Time: <span id="page-load-time" class="metric-value">--</span>ms</div>
            <div>Error Rate: <span id="error-rate" class="metric-value">--</span>%</div>
            <canvas id="user-experience-chart" width="300" height="200"></canvas>
        </div>

        <!-- Business Metrics -->
        <div class="metric-card">
            <h3>Business Performance</h3>
            <div>Throughput: <span id="throughput" class="metric-value">--</span> req/sec</div>
            <div>Active Users: <span id="active-users" class="metric-value">--</span></div>
            <canvas id="business-metrics-chart" width="300" height="200"></canvas>
        </div>

        <!-- Core Web Vitals -->
        <div class="metric-card">
            <h3>Core Web Vitals</h3>
            <div>LCP: <span id="lcp" class="metric-value">--</span>ms</div>
            <div>FID: <span id="fid" class="metric-value">--</span>ms</div>
            <div>CLS: <span id="cls" class="metric-value">--</span></div>
        </div>
    </div>

    <script>
        // Dashboard update logic
        async function updateDashboard() {
            try {
                const response = await fetch('/api/performance/dashboard');
                const data = await response.json();

                // Update metrics
                updateMetric('avg-response-time', data.avgResponseTime, 1000, 2000);
                updateMetric('p95-response-time', data.p95ResponseTime, 2000, 5000);
                updateMetric('avg-query-time', data.avgQueryTime, 100, 500);
                updateMetric('db-connections', data.dbConnections, 50, 80);
                updateMetric('cpu-usage', data.cpuUsage, 70, 90);
                updateMetric('memory-usage', data.memoryUsage, 75, 90);
                updateMetric('page-load-time', data.pageLoadTime, 2000, 5000);
                updateMetric('error-rate', data.errorRate, 1, 5);
                updateMetric('throughput', data.throughput);
                updateMetric('active-users', data.activeUsers);
                updateMetric('lcp', data.lcp, 2500, 4000);
                updateMetric('fid', data.fid, 100, 300);
                updateMetric('cls', data.cls, 0.1, 0.25);

                // Update charts
                updateCharts(data);

            } catch (error) {
                console.error('Failed to update dashboard:', error);
            }
        }

        function updateMetric(elementId, value, warningThreshold, criticalThreshold) {
            const element = document.getElementById(elementId);
            if (!element) return;

            element.textContent = typeof value === 'number' ? value.toFixed(1) : value;

            // Apply color coding
            element.className = 'metric-value';
            if (criticalThreshold && value > criticalThreshold) {
                element.classList.add('metric-bad');
            } else if (warningThreshold && value > warningThreshold) {
                element.classList.add('metric-warning');
            } else {
                element.classList.add('metric-good');
            }
        }

        function updateCharts(data) {
            // Implementation of chart updates
            // This would integrate with Chart.js to show trends
        }

        // Update every 30 seconds
        setInterval(updateDashboard, 30000);
        updateDashboard(); // Initial load
    </script>
</body>
</html>
```

### Automated Performance Reports
```csharp
public class PerformanceReportingService
{
    public async Task<PerformanceReport> GenerateWeeklyReport()
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-7);

        var report = new PerformanceReport
        {
            ReportPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            GeneratedAt = DateTime.UtcNow,
            ApplicationPerformance = await GetApplicationPerformanceMetrics(startDate, endDate),
            DatabasePerformance = await GetDatabasePerformanceMetrics(startDate, endDate),
            InfrastructurePerformance = await GetInfrastructureMetrics(startDate, endDate),
            UserExperience = await GetUserExperienceMetrics(startDate, endDate),
            SlaCompliance = await CalculateSlaCompliance(startDate, endDate),
            PerformanceGoals = await EvaluatePerformanceGoals(startDate, endDate),
            Recommendations = await GenerateRecommendations(startDate, endDate)
        };

        return report;
    }

    private async Task<ApplicationPerformanceMetrics> GetApplicationPerformanceMetrics(DateTime start, DateTime end)
    {
        var responseTimeData = await _metricsRepository.GetResponseTimeMetrics(start, end);
        var throughputData = await _metricsRepository.GetThroughputMetrics(start, end);
        var errorData = await _metricsRepository.GetErrorMetrics(start, end);

        return new ApplicationPerformanceMetrics
        {
            AverageResponseTime = responseTimeData.Average(),
            P95ResponseTime = Percentile(responseTimeData, 0.95),
            P99ResponseTime = Percentile(responseTimeData, 0.99),
            TotalRequests = throughputData.Sum(),
            AverageThroughput = throughputData.Average(),
            PeakThroughput = throughputData.Max(),
            ErrorRate = errorData.Average(),
            AvailabilityPercentage = CalculateAvailability(start, end),
            SlowRequestsCount = responseTimeData.Count(r => r > 2000),
            ResponseTimeDistribution = CalculateResponseTimeDistribution(responseTimeData)
        };
    }

    private async Task<List<PerformanceRecommendation>> GenerateRecommendations(DateTime start, DateTime end)
    {
        var recommendations = new List<PerformanceRecommendation>();

        // Analyze performance patterns and generate recommendations
        var slowQueries = await _metricsRepository.GetSlowQueries(start, end);
        if (slowQueries.Any())
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Category = "Database",
                Priority = "High",
                Issue = $"Found {slowQueries.Count} slow queries",
                Recommendation = "Optimize slow queries and add missing indexes",
                Impact = "Reduce response time by 20-40%",
                ImplementationEffort = "Medium"
            });
        }

        var highCpuPeriods = await _metricsRepository.GetHighCpuPeriods(start, end, 80);
        if (highCpuPeriods.Any())
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Category = "Infrastructure",
                Priority = "Medium",
                Issue = $"CPU usage exceeded 80% for {highCpuPeriods.Count} periods",
                Recommendation = "Consider scaling up or optimizing CPU-intensive operations",
                Impact = "Improve system stability and response times",
                ImplementationEffort = "Low to Medium"
            });
        }

        return recommendations;
    }
}
```

## Performance Optimization Procedures

### Performance Optimization Workflow
```markdown
## Performance Optimization Process

### 1. Performance Issue Detection
- [ ] Monitor alerts triggered
- [ ] Performance baselines exceeded
- [ ] User complaints received
- [ ] Proactive performance review

### 2. Initial Assessment (15 minutes)
- [ ] Verify performance degradation
- [ ] Identify affected components
- [ ] Assess user impact
- [ ] Determine urgency level

### 3. Data Collection (30 minutes)
- [ ] Gather performance metrics
- [ ] Collect system resource data
- [ ] Review application logs
- [ ] Analyze database performance
- [ ] Check external dependencies

### 4. Root Cause Analysis (60 minutes)
- [ ] Correlate performance data
- [ ] Identify bottlenecks
- [ ] Review recent changes
- [ ] Analyze code and queries
- [ ] Test hypotheses

### 5. Optimization Implementation (Variable)
- [ ] Develop optimization plan
- [ ] Implement fixes
- [ ] Test improvements
- [ ] Deploy optimizations
- [ ] Monitor results

### 6. Validation and Documentation (30 minutes)
- [ ] Verify performance improvement
- [ ] Update documentation
- [ ] Share learnings with team
- [ ] Update monitoring thresholds
```

### Common Performance Optimizations

#### Database Query Optimization
```sql
-- Query performance analysis
SELECT TOP 10
    qs.execution_count,
    qs.total_worker_time / qs.execution_count as avg_cpu_time,
    qs.total_elapsed_time / qs.execution_count as avg_elapsed_time,
    qs.total_logical_reads / qs.execution_count as avg_logical_reads,
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2)+1) as query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qs.total_elapsed_time / qs.execution_count > 1000  -- Queries taking > 1 second on average
ORDER BY qs.total_worker_time / qs.execution_count DESC;

-- Index optimization recommendations
SELECT
    migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) AS improvement_measure,
    'CREATE INDEX [IX_' + OBJECT_NAME(mid.object_id) + '_' +
    REPLACE(REPLACE(REPLACE(ISNULL(mid.equality_columns,''),', ','_'),'[',''),']','') + ']' +
    ' ON ' + mid.statement + ' (' + ISNULL (mid.equality_columns,'') +
    CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL
         THEN ',' ELSE '' END + ISNULL (mid.inequality_columns, '') + ')' +
    ISNULL (' INCLUDE (' + mid.included_columns + ')', '') AS create_index_statement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) > 10
ORDER BY improvement_measure DESC;
```

#### Application Code Optimization
```csharp
// Performance optimization examples
public class OptimizedCertificateService
{
    // Cache frequently accessed data
    private readonly IMemoryCache _cache;
    private readonly ILogger<OptimizedCertificateService> _logger;

    public async Task<List<CertificateDto>> GetCertificatesOptimized(SearchCriteria criteria)
    {
        // Use caching for expensive queries
        var cacheKey = $"certificates_{criteria.GetHashCode()}";
        if (_cache.TryGetValue(cacheKey, out List<CertificateDto> cachedResult))
        {
            return cachedResult;
        }

        // Use projection to minimize data transfer
        var query = _context.Certificates
            .Where(BuildWhereClause(criteria))
            .Select(c => new CertificateDto
            {
                Id = c.Id,
                Name = c.Name,
                CompanyName = c.Company.Name,
                Status = c.Status,
                ExpirationDate = c.ExpirationDate
            });

        // Use async enumeration for large datasets
        var results = await query.ToListAsync();

        // Cache results with appropriate expiration
        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(15));

        return results;
    }

    // Optimize bulk operations
    public async Task<BulkUpdateResult> BulkUpdateCertificates(List<CertificateUpdateDto> updates)
    {
        var result = new BulkUpdateResult();

        // Process in batches to avoid memory issues
        const int batchSize = 100;
        for (int i = 0; i < updates.Count; i += batchSize)
        {
            var batch = updates.Skip(i).Take(batchSize);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var update in batch)
                {
                    var certificate = await _context.Certificates.FindAsync(update.Id);
                    if (certificate != null)
                    {
                        // Update properties
                        certificate.Status = update.Status;
                        certificate.LastModifiedDate = DateTime.UtcNow;
                        result.UpdatedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Batch update failed for batch starting at {Index}", i);
                result.ErrorCount += batch.Count();
            }
        }

        return result;
    }
}
```

## Continuous Performance Improvement

### Performance Review Process
```markdown
## Monthly Performance Review

### Performance Metrics Analysis
- [ ] Review SLA compliance
- [ ] Analyze performance trends
- [ ] Identify degradation patterns
- [ ] Compare against baselines

### Capacity Planning Review
- [ ] Assess resource utilization trends
- [ ] Forecast capacity needs
- [ ] Plan infrastructure scaling
- [ ] Update capacity thresholds

### Optimization Opportunities
- [ ] Identify performance bottlenecks
- [ ] Prioritize optimization efforts
- [ ] Plan optimization sprints
- [ ] Update performance budgets

### Process Improvements
- [ ] Review monitoring effectiveness
- [ ] Update alerting thresholds
- [ ] Enhance performance testing
- [ ] Improve documentation
```

### Performance Budget Management
```json
{
  "performance_budgets": {
    "page_load_time": {
      "target": 2000,
      "warning": 3000,
      "critical": 5000,
      "unit": "milliseconds"
    },
    "api_response_time": {
      "target": 500,
      "warning": 1000,
      "critical": 2000,
      "unit": "milliseconds"
    },
    "database_query_time": {
      "target": 100,
      "warning": 500,
      "critical": 1000,
      "unit": "milliseconds"
    },
    "bundle_size": {
      "target": 250,
      "warning": 500,
      "critical": 1000,
      "unit": "kilobytes"
    },
    "core_web_vitals": {
      "lcp": {
        "good": 2500,
        "needs_improvement": 4000,
        "poor": 4000
      },
      "fid": {
        "good": 100,
        "needs_improvement": 300,
        "poor": 300
      },
      "cls": {
        "good": 0.1,
        "needs_improvement": 0.25,
        "poor": 0.25
      }
    }
  }
}
```

---

This comprehensive performance monitoring plan ensures optimal system performance through continuous monitoring, proactive optimization, and data-driven decision making for the GCC Database Solution.

---
*Last Updated: September 17, 2025*
# Capacity Planning & Scaling Guide - GCC Database Solution

## Overview
This document provides comprehensive guidance for capacity planning and scaling strategies for the GCC Database Solution. It covers resource forecasting, scaling procedures, performance optimization, and automated scaling mechanisms to ensure optimal system performance as demand grows.

## Current System Baseline

### Current Architecture Overview
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Load Balancer │───▶│   Web App (x2)  │───▶│   Database      │
│   (nginx)       │    │   ASP.NET Core  │    │   SQL Server    │
│   CPU: 2 cores  │    │   CPU: 4 cores  │    │   CPU: 8 cores  │
│   RAM: 4 GB     │    │   RAM: 8 GB     │    │   RAM: 32 GB    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       ▼                       │
         │              ┌─────────────────┐              │
         │              │   Redis Cache   │              │
         │              │   CPU: 2 cores  │              │
         │              │   RAM: 8 GB     │              │
         │              └─────────────────┘              │
         │                                               │
         ▼                                               ▼
┌─────────────────┐                            ┌─────────────────┐
│   File Storage  │                            │   Backup Store  │
│   Azure Blob    │                            │   Azure Backup  │
│   100 GB        │                            │   500 GB        │
└─────────────────┘                            └─────────────────┘
```

### Current Performance Metrics

#### Application Metrics (Current Load)
| Metric | Current Value | Capacity |
|--------|---------------|----------|
| Concurrent Users | 50 | 200 |
| Requests/Second | 25 | 100 |
| Average Response Time | 800ms | < 2000ms |
| Peak Response Time | 1500ms | < 5000ms |
| CPU Utilization | 35% | 80% |
| Memory Usage | 60% | 85% |
| Database Connections | 15 | 100 |

#### Database Metrics
| Metric | Current Value | Capacity |
|--------|---------------|----------|
| Database Size | 2.5 GB | 100 GB |
| Transaction Log Size | 500 MB | 10 GB |
| Queries/Second | 150 | 1000 |
| Average Query Time | 45ms | 100ms |
| Concurrent Connections | 25 | 200 |
| Index Fragmentation | 8% | 30% |

## Capacity Planning Framework

### Growth Projections

#### User Growth Forecast
```
Year 1 (Current): 500 registered users, 50 concurrent
Year 2: 1,500 registered users, 150 concurrent
Year 3: 3,000 registered users, 300 concurrent
Year 4: 5,000 registered users, 500 concurrent
Year 5: 8,000 registered users, 800 concurrent
```

#### Data Growth Forecast
```
Current Database Size: 2.5 GB
Projected Growth Rate: 100% per year

Year 1: 2.5 GB
Year 2: 5.0 GB (2x certificates + companies)
Year 3: 10.0 GB (Partner expansion + API usage)
Year 4: 20.0 GB (International expansion)
Year 5: 40.0 GB (Full feature set + integrations)
```

#### Transaction Volume Forecast
```
Current: 1,000 transactions/day
Year 1: 2,500 transactions/day
Year 2: 6,000 transactions/day
Year 3: 15,000 transactions/day
Year 4: 35,000 transactions/day
Year 5: 75,000 transactions/day
```

### Capacity Planning Models

#### Little's Law Application
```csharp
// Capacity planning calculations using Little's Law
public class CapacityPlanningCalculator
{
    public CapacityMetrics CalculateCapacityRequirements(WorkloadForecast forecast)
    {
        // Little's Law: L = λ × W
        // L = Average number of requests in system
        // λ = Arrival rate (requests per second)
        // W = Average time in system (response time)

        var averageSystemTime = forecast.AverageResponseTime / 1000.0; // Convert to seconds
        var arrivalRate = forecast.RequestsPerSecond;
        var averageRequestsInSystem = arrivalRate * averageSystemTime;

        // Calculate resource requirements
        var cpuUtilization = CalculateCpuUtilization(arrivalRate, forecast.CpuTimePerRequest);
        var memoryRequirement = CalculateMemoryRequirement(forecast.ConcurrentUsers, forecast.MemoryPerUser);
        var databaseConnections = CalculateDatabaseConnections(arrivalRate, forecast.DatabaseTimePerRequest);

        return new CapacityMetrics
        {
            AverageRequestsInSystem = averageRequestsInSystem,
            RequiredCpuCores = Math.Ceiling(cpuUtilization / 0.8), // Target 80% utilization
            RequiredMemoryGB = Math.Ceiling(memoryRequirement / 0.85), // Target 85% utilization
            RequiredDatabaseConnections = Math.Ceiling(databaseConnections * 1.5), // 50% buffer
            EstimatedMaxThroughput = CalculateMaxThroughput(cpuUtilization, memoryRequirement)
        };
    }

    private double CalculateCpuUtilization(double requestsPerSecond, double cpuTimePerRequest)
    {
        // CPU utilization = (requests/sec) × (CPU time per request)
        return requestsPerSecond * cpuTimePerRequest;
    }

    private double CalculateMemoryRequirement(int concurrentUsers, double memoryPerUser)
    {
        // Base application memory + (concurrent users × memory per user)
        var baseMemory = 2.0; // GB for base application
        var userMemory = concurrentUsers * memoryPerUser / 1024.0; // Convert MB to GB
        return baseMemory + userMemory;
    }

    private double CalculateDatabaseConnections(double requestsPerSecond, double dbTimePerRequest)
    {
        // Average database connections = (requests/sec) × (DB time per request)
        return requestsPerSecond * dbTimePerRequest;
    }
}
```

#### Performance Testing for Capacity Planning
```csharp
public class CapacityTestingService
{
    public async Task<CapacityTestResult> RunCapacityTest(CapacityTestConfiguration config)
    {
        var results = new List<LoadTestResult>();

        // Test different load levels to find breaking points
        for (int load = config.StartLoad; load <= config.MaxLoad; load += config.LoadIncrement)
        {
            var testConfig = new PerformanceTestConfig
            {
                ConcurrentUsers = load,
                RequestsPerUser = config.RequestsPerUser,
                TestDuration = config.TestDuration
            };

            var result = await RunLoadTest(testConfig);
            results.Add(result);

            // Stop if we hit critical thresholds
            if (result.AverageResponseTime > config.MaxAcceptableResponseTime ||
                result.ErrorRate > config.MaxAcceptableErrorRate)
            {
                break;
            }

            // Wait between tests to allow system recovery
            await Task.Delay(config.RecoveryTime);
        }

        return AnalyzeCapacityResults(results, config);
    }

    private CapacityTestResult AnalyzeCapacityResults(List<LoadTestResult> results, CapacityTestConfiguration config)
    {
        // Find capacity limits
        var maxStableLoad = results
            .Where(r => r.ErrorRate <= config.MaxAcceptableErrorRate &&
                       r.AverageResponseTime <= config.MaxAcceptableResponseTime)
            .OrderByDescending(r => r.ConcurrentUsers)
            .FirstOrDefault();

        var breakingPoint = results
            .Where(r => r.ErrorRate > config.MaxAcceptableErrorRate ||
                       r.AverageResponseTime > config.MaxAcceptableResponseTime)
            .OrderBy(r => r.ConcurrentUsers)
            .FirstOrDefault();

        return new CapacityTestResult
        {
            MaxStableLoad = maxStableLoad?.ConcurrentUsers ?? 0,
            BreakingPoint = breakingPoint?.ConcurrentUsers ?? config.MaxLoad,
            RecommendedCapacity = (int)(maxStableLoad?.ConcurrentUsers * 0.8 ?? 0), // 80% of max stable
            PerformanceProfile = GeneratePerformanceProfile(results),
            ResourceUtilization = GenerateResourceUtilizationProfile(results),
            ScalingRecommendations = GenerateScalingRecommendations(results)
        };
    }
}
```

## Scaling Strategies

### Horizontal Scaling (Scale Out)

#### Application Server Scaling
```yaml
# Docker Swarm scaling configuration
version: '3.8'
services:
  gcc-webapp:
    image: gcc-webapp:latest
    deploy:
      replicas: 2
      update_config:
        parallelism: 1
        delay: 30s
        failure_action: rollback
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
      resources:
        limits:
          cpus: '2.0'
          memory: 4G
        reservations:
          cpus: '1.0'
          memory: 2G
    networks:
      - gcc-network
    depends_on:
      - database
      - redis

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    deploy:
      replicas: 2
      placement:
        constraints:
          - node.role == manager
```

#### Kubernetes Horizontal Pod Autoscaler
```yaml
# HPA configuration for automatic scaling
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: gcc-webapp-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: gcc-webapp
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  - type: Pods
    pods:
      metric:
        name: response_time_p95
      target:
        type: AverageValue
        averageValue: "2000"
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 10
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
      - type: Pods
        value: 2
        periodSeconds: 60
      selectPolicy: Max
```

#### Load Balancer Configuration
```nginx
# nginx load balancer configuration
upstream gcc_backend {
    least_conn;
    server app1:8080 max_fails=3 fail_timeout=30s;
    server app2:8080 max_fails=3 fail_timeout=30s;
    server app3:8080 max_fails=3 fail_timeout=30s backup;
}

server {
    listen 80;
    server_name gcc.production.com;

    location / {
        proxy_pass http://gcc_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Health check
        proxy_next_upstream error timeout invalid_header http_500 http_502 http_503 http_504;
        proxy_connect_timeout 5s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    location /health {
        access_log off;
        return 200 "healthy\n";
    }
}
```

### Vertical Scaling (Scale Up)

#### Database Server Scaling Plan
```sql
-- Database scaling assessment queries
-- Current resource utilization
SELECT
    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1) as current_connections,
    (SELECT @@MAX_CONNECTIONS) as max_connections,
    (SELECT physical_memory_kb/1024/1024 FROM sys.dm_os_sys_info) as total_memory_gb,
    (SELECT available_physical_memory_kb/1024/1024 FROM sys.dm_os_sys_memory) as available_memory_gb,
    (SELECT cpu_count FROM sys.dm_os_sys_info) as cpu_count;

-- Performance counters for scaling decisions
SELECT
    object_name,
    counter_name,
    cntr_value,
    cntr_type
FROM sys.dm_os_performance_counters
WHERE object_name LIKE '%SQL Server:Memory Manager%'
   OR object_name LIKE '%SQL Server:Buffer Manager%'
   OR object_name LIKE '%SQL Server:Databases%'
ORDER BY object_name, counter_name;

-- Database growth analysis
SELECT
    DB_NAME() as database_name,
    SUM(size * 8.0 / 1024) as current_size_mb,
    SUM(max_size * 8.0 / 1024) as max_size_mb,
    AVG(growth * 8.0 / 1024) as avg_growth_mb
FROM sys.database_files
GROUP BY database_id;
```

#### Vertical Scaling Decision Matrix
```csharp
public class VerticalScalingAdvisor
{
    public ScalingRecommendation AnalyzeVerticalScalingNeeds(SystemMetrics metrics)
    {
        var recommendation = new ScalingRecommendation();

        // CPU Analysis
        if (metrics.CpuUtilization > 80)
        {
            recommendation.CpuAction = ScalingAction.ScaleUp;
            recommendation.RecommendedCpuCores = Math.Ceiling(metrics.CurrentCpuCores * 1.5);
            recommendation.Priority = Priority.High;
        }
        else if (metrics.CpuUtilization < 30 && metrics.CurrentCpuCores > 2)
        {
            recommendation.CpuAction = ScalingAction.ScaleDown;
            recommendation.RecommendedCpuCores = Math.Max(2, metrics.CurrentCpuCores / 2);
            recommendation.Priority = Priority.Low;
        }

        // Memory Analysis
        if (metrics.MemoryUtilization > 85)
        {
            recommendation.MemoryAction = ScalingAction.ScaleUp;
            recommendation.RecommendedMemoryGB = Math.Ceiling(metrics.CurrentMemoryGB * 1.5);
            recommendation.Priority = Math.Max(recommendation.Priority, Priority.High);
        }
        else if (metrics.MemoryUtilization < 40 && metrics.CurrentMemoryGB > 4)
        {
            recommendation.MemoryAction = ScalingAction.ScaleDown;
            recommendation.RecommendedMemoryGB = Math.Max(4, metrics.CurrentMemoryGB * 0.75);
        }

        // Storage Analysis
        if (metrics.StorageUtilization > 80)
        {
            recommendation.StorageAction = ScalingAction.ScaleUp;
            recommendation.RecommendedStorageGB = metrics.CurrentStorageGB * 2;
            recommendation.Priority = Math.Max(recommendation.Priority, Priority.Medium);
        }

        // Cost-benefit analysis
        recommendation.EstimatedCost = CalculateScalingCost(recommendation);
        recommendation.EstimatedBenefit = CalculatePerformanceBenefit(recommendation, metrics);

        return recommendation;
    }

    private decimal CalculateScalingCost(ScalingRecommendation recommendation)
    {
        // Calculate monthly cost difference
        var cpuCostPerCore = 50; // Monthly cost per CPU core
        var memoryCostPerGB = 10; // Monthly cost per GB RAM
        var storageCostPerGB = 0.5m; // Monthly cost per GB storage

        var cpuCostDelta = (recommendation.RecommendedCpuCores - recommendation.CurrentCpuCores) * cpuCostPerCore;
        var memoryCostDelta = (recommendation.RecommendedMemoryGB - recommendation.CurrentMemoryGB) * memoryCostPerGB;
        var storageCostDelta = (recommendation.RecommendedStorageGB - recommendation.CurrentStorageGB) * storageCostPerGB;

        return cpuCostDelta + memoryCostDelta + storageCostDelta;
    }
}
```

### Auto-Scaling Implementation

#### Application Auto-Scaling Service
```csharp
public class AutoScalingService : IHostedService
{
    private readonly ILogger<AutoScalingService> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly IContainerOrchestrator _orchestrator;
    private Timer _scalingTimer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _scalingTimer = new Timer(PerformScalingAnalysis, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Auto-scaling service started");
    }

    private async void PerformScalingAnalysis(object state)
    {
        try
        {
            var metrics = await CollectCurrentMetrics();
            var scalingDecision = AnalyzeScalingNeeds(metrics);

            if (scalingDecision.ShouldScale)
            {
                await ExecuteScalingAction(scalingDecision);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-scaling analysis");
        }
    }

    private async Task<CurrentMetrics> CollectCurrentMetrics()
    {
        return new CurrentMetrics
        {
            CpuUtilization = await _metrics.GetAverageCpuUtilization(TimeSpan.FromMinutes(5)),
            MemoryUtilization = await _metrics.GetAverageMemoryUtilization(TimeSpan.FromMinutes(5)),
            ResponseTime = await _metrics.GetAverageResponseTime(TimeSpan.FromMinutes(5)),
            RequestRate = await _metrics.GetRequestRate(TimeSpan.FromMinutes(1)),
            ErrorRate = await _metrics.GetErrorRate(TimeSpan.FromMinutes(5)),
            ActiveConnections = await _metrics.GetActiveConnections(),
            CurrentReplicas = await _orchestrator.GetCurrentReplicaCount()
        };
    }

    private ScalingDecision AnalyzeScalingNeeds(CurrentMetrics metrics)
    {
        var decision = new ScalingDecision();

        // Scale up conditions
        if (metrics.CpuUtilization > 70 || metrics.MemoryUtilization > 80 || metrics.ResponseTime > 2000)
        {
            if (metrics.CurrentReplicas < 10) // Max replicas limit
            {
                decision.ShouldScale = true;
                decision.Action = ScalingAction.ScaleUp;
                decision.TargetReplicas = Math.Min(10, metrics.CurrentReplicas + 1);
                decision.Reason = $"High resource utilization: CPU={metrics.CpuUtilization}%, Memory={metrics.MemoryUtilization}%, ResponseTime={metrics.ResponseTime}ms";
            }
        }
        // Scale down conditions
        else if (metrics.CpuUtilization < 30 && metrics.MemoryUtilization < 50 && metrics.ResponseTime < 1000)
        {
            if (metrics.CurrentReplicas > 2) // Min replicas limit
            {
                decision.ShouldScale = true;
                decision.Action = ScalingAction.ScaleDown;
                decision.TargetReplicas = Math.Max(2, metrics.CurrentReplicas - 1);
                decision.Reason = $"Low resource utilization: CPU={metrics.CpuUtilization}%, Memory={metrics.MemoryUtilization}%";
            }
        }

        return decision;
    }

    private async Task ExecuteScalingAction(ScalingDecision decision)
    {
        _logger.LogInformation("Executing scaling action: {Action} to {Replicas} replicas. Reason: {Reason}",
            decision.Action, decision.TargetReplicas, decision.Reason);

        try
        {
            await _orchestrator.ScaleToReplicas(decision.TargetReplicas);

            // Log scaling event for monitoring
            _metrics.Counter("gcc_scaling_events_total")
                .WithTag("action", decision.Action.ToString())
                .WithTag("reason", "auto")
                .Increment();

            _logger.LogInformation("Scaling action completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute scaling action");

            _metrics.Counter("gcc_scaling_failures_total")
                .WithTag("action", decision.Action.ToString())
                .Increment();
        }
    }
}
```

## Database Scaling Strategies

### Database Scaling Approaches

#### Read Replica Configuration
```sql
-- Set up read replica for read scaling
-- Primary database configuration
ALTER DATABASE GCCDatabase SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE GCCDatabase SET READ_COMMITTED_SNAPSHOT ON;

-- Create read-only user for replica connections
CREATE LOGIN [gcc_reader] WITH PASSWORD = 'SecurePassword123!';
CREATE USER [gcc_reader] FOR LOGIN [gcc_reader];
ALTER ROLE [db_datareader] ADD MEMBER [gcc_reader];

-- Monitor replica lag
SELECT
    database_name,
    last_commit_time,
    last_commit_time_utc,
    commit_sequence_num
FROM sys.dm_hadr_database_replica_states
WHERE database_id = DB_ID('GCCDatabase');
```

#### Database Connection String Strategy
```csharp
public class DatabaseConnectionManager
{
    private readonly string _primaryConnectionString;
    private readonly List<string> _readOnlyConnectionStrings;
    private readonly ILogger<DatabaseConnectionManager> _logger;
    private int _readOnlyIndex = 0;

    public string GetConnectionString(QueryType queryType)
    {
        switch (queryType)
        {
            case QueryType.Write:
            case QueryType.ReadConsistent:
                return _primaryConnectionString;

            case QueryType.ReadEventual:
                return GetNextReadOnlyConnection();

            default:
                return _primaryConnectionString;
        }
    }

    private string GetNextReadOnlyConnection()
    {
        if (_readOnlyConnectionStrings.Count == 0)
        {
            _logger.LogWarning("No read-only connections available, falling back to primary");
            return _primaryConnectionString;
        }

        var connectionString = _readOnlyConnectionStrings[_readOnlyIndex % _readOnlyConnectionStrings.Count];
        _readOnlyIndex++;
        return connectionString;
    }

    // Usage in repositories
    public async Task<List<Certificate>> GetCertificatesAsync(SearchCriteria criteria)
    {
        var connectionString = GetConnectionString(QueryType.ReadEventual);
        using var context = new ApplicationDbContext(connectionString);

        return await context.Certificates
            .Where(c => /* search criteria */)
            .ToListAsync();
    }
}
```

#### Database Partitioning Strategy
```sql
-- Partition certificates by date for better performance
-- Create partition function
CREATE PARTITION FUNCTION CertificateDatePartition (datetime2)
AS RANGE RIGHT FOR VALUES
('2024-01-01', '2024-04-01', '2024-07-01', '2024-10-01', '2025-01-01');

-- Create partition scheme
CREATE PARTITION SCHEME CertificateDateScheme
AS PARTITION CertificateDatePartition
TO ([PRIMARY], [Q1_2024], [Q2_2024], [Q3_2024], [Q4_2024], [Q1_2025]);

-- Create partitioned table
CREATE TABLE Certificates_Partitioned (
    Id int IDENTITY(1,1),
    Name nvarchar(255) NOT NULL,
    CompanyId int NOT NULL,
    CreatedDate datetime2 NOT NULL,
    ExpirationDate datetime2 NOT NULL,
    Status nvarchar(50) NOT NULL,
    -- Other columns...

    CONSTRAINT PK_Certificates_Partitioned
    PRIMARY KEY CLUSTERED (Id, CreatedDate)
) ON CertificateDateScheme(CreatedDate);

-- Monitor partition usage
SELECT
    p.partition_number,
    p.rows,
    rv.value as range_boundary,
    fg.name as filegroup_name
FROM sys.partitions p
INNER JOIN sys.partition_schemes ps ON p.partition_id = ps.data_space_id
INNER JOIN sys.partition_range_values rv ON ps.function_id = rv.function_id
    AND p.partition_number = rv.boundary_id + 1
INNER JOIN sys.filegroups fg ON p.partition_id = fg.data_space_id
WHERE p.object_id = OBJECT_ID('Certificates_Partitioned')
ORDER BY p.partition_number;
```

### Cache Scaling Strategy

#### Redis Cluster Configuration
```yaml
# Redis cluster for scaling cache layer
version: '3.8'
services:
  redis-master:
    image: redis:7-alpine
    command: redis-server --port 6379 --cluster-enabled yes --cluster-config-file nodes.conf --cluster-node-timeout 5000 --appendonly yes
    ports:
      - "6379:6379"
      - "16379:16379"
    volumes:
      - redis-master-data:/data

  redis-slave-1:
    image: redis:7-alpine
    command: redis-server --port 6379 --cluster-enabled yes --cluster-config-file nodes.conf --cluster-node-timeout 5000 --appendonly yes
    ports:
      - "6380:6379"
      - "16380:16379"
    volumes:
      - redis-slave1-data:/data

  redis-slave-2:
    image: redis:7-alpine
    command: redis-server --port 6379 --cluster-enabled yes --cluster-config-file nodes.conf --cluster-node-timeout 5000 --appendonly yes
    ports:
      - "6381:6379"
      - "16381:16379"
    volumes:
      - redis-slave2-data:/data

volumes:
  redis-master-data:
  redis-slave1-data:
  redis-slave2-data:
```

#### Distributed Caching Implementation
```csharp
public class DistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly CacheSettings _settings;

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null)
    {
        var cachedValue = await _cache.GetStringAsync(key);

        if (cachedValue != null)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(cachedValue);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached value for key: {Key}", key);
            }
        }

        var item = await getItem();

        if (item != null)
        {
            var serializedItem = JsonSerializer.Serialize(item);
            var options = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromMinutes(_settings.DefaultSlidingExpirationMinutes));
            }

            await _cache.SetStringAsync(key, serializedItem, options);
        }

        return item;
    }

    // Cache invalidation strategies
    public async Task InvalidatePatternAsync(string pattern)
    {
        // Implementation depends on cache provider
        // For Redis, use SCAN command to find matching keys
        var keys = await FindKeysByPatternAsync(pattern);

        var tasks = keys.Select(key => _cache.RemoveAsync(key));
        await Task.WhenAll(tasks);
    }

    // Cache warming for frequently accessed data
    public async Task WarmCacheAsync()
    {
        _logger.LogInformation("Starting cache warming process");

        // Warm certificate data
        await WarmCertificateCache();

        // Warm company data
        await WarmCompanyCache();

        // Warm product data
        await WarmProductCache();

        _logger.LogInformation("Cache warming completed");
    }

    private async Task WarmCertificateCache()
    {
        // Cache popular search queries
        var popularSearches = await GetPopularSearchQueries();

        foreach (var search in popularSearches)
        {
            var cacheKey = $"certificates:search:{search.GetHashCode()}";
            await GetOrSetAsync(cacheKey,
                () => _certificateService.SearchCertificatesAsync(search),
                TimeSpan.FromHours(1));
        }
    }
}
```

## Monitoring and Alerting for Scaling

### Capacity Monitoring Dashboard
```csharp
public class CapacityMonitoringService
{
    public async Task<CapacityStatus> GetCurrentCapacityStatus()
    {
        var status = new CapacityStatus();

        // Application capacity
        status.ApplicationCapacity = new ComponentCapacity
        {
            CurrentLoad = await GetCurrentApplicationLoad(),
            MaxCapacity = await GetMaxApplicationCapacity(),
            UtilizationPercentage = await GetApplicationUtilization(),
            TimeToCapacity = await EstimateTimeToCapacity("application"),
            ScalingRecommendation = await GetApplicationScalingRecommendation()
        };

        // Database capacity
        status.DatabaseCapacity = new ComponentCapacity
        {
            CurrentLoad = await GetCurrentDatabaseLoad(),
            MaxCapacity = await GetMaxDatabaseCapacity(),
            UtilizationPercentage = await GetDatabaseUtilization(),
            TimeToCapacity = await EstimateTimeToCapacity("database"),
            ScalingRecommendation = await GetDatabaseScalingRecommendation()
        };

        // Storage capacity
        status.StorageCapacity = new ComponentCapacity
        {
            CurrentLoad = await GetCurrentStorageUsage(),
            MaxCapacity = await GetMaxStorageCapacity(),
            UtilizationPercentage = await GetStorageUtilization(),
            TimeToCapacity = await EstimateTimeToCapacity("storage"),
            ScalingRecommendation = await GetStorageScalingRecommendation()
        };

        return status;
    }

    private async Task<TimeSpan> EstimateTimeToCapacity(string component)
    {
        // Get historical growth data
        var growthData = await _metricsRepository.GetGrowthTrends(component, TimeSpan.FromDays(30));

        if (growthData.Count < 7) // Need at least a week of data
            return TimeSpan.MaxValue;

        // Calculate linear growth rate
        var growthRate = CalculateLinearGrowthRate(growthData);

        if (growthRate <= 0) // No growth or negative growth
            return TimeSpan.MaxValue;

        var currentUtilization = await GetCurrentUtilization(component);
        var remainingCapacity = 100 - currentUtilization;

        // Time to reach 90% capacity
        var daysToCapacity = (90 - currentUtilization) / growthRate;

        return TimeSpan.FromDays(Math.Max(0, daysToCapacity));
    }
}
```

### Scaling Alerts Configuration
```yaml
# Prometheus alerting rules for scaling
groups:
- name: capacity-alerts
  rules:
  - alert: ApplicationNearCapacity
    expr: gcc_application_utilization > 80
    for: 10m
    labels:
      severity: warning
      component: application
    annotations:
      summary: "Application approaching capacity limits"
      description: "Application utilization is {{ $value }}%"
      action: "Consider scaling application instances"

  - alert: DatabaseNearCapacity
    expr: gcc_database_utilization > 85
    for: 5m
    labels:
      severity: critical
      component: database
    annotations:
      summary: "Database approaching capacity limits"
      description: "Database utilization is {{ $value }}%"
      action: "Scale database resources immediately"

  - alert: StorageNearCapacity
    expr: gcc_storage_utilization > 90
    for: 1m
    labels:
      severity: critical
      component: storage
    annotations:
      summary: "Storage space critically low"
      description: "Storage utilization is {{ $value }}%"
      action: "Expand storage immediately"

  - alert: ResponseTimeDegrading
    expr: histogram_quantile(0.95, rate(gcc_request_duration_seconds_bucket[5m])) > 3
    for: 15m
    labels:
      severity: warning
      component: performance
    annotations:
      summary: "Application response time degrading"
      description: "95th percentile response time is {{ $value }}s"
      action: "Check for capacity constraints"

  - alert: PredictedCapacityReached
    expr: gcc_time_to_capacity_days < 7
    for: 1h
    labels:
      severity: warning
      component: planning
    annotations:
      summary: "Capacity will be reached soon"
      description: "Estimated {{ $value }} days until capacity limit"
      action: "Plan scaling activities"
```

## Cost Optimization Strategies

### Resource Right-Sizing
```csharp
public class CostOptimizationService
{
    public async Task<CostOptimizationReport> AnalyzeCostOptimization()
    {
        var report = new CostOptimizationReport();

        // Analyze oversized resources
        report.OversizedResources = await FindOversizedResources();

        // Analyze undersized resources
        report.UndersizedResources = await FindUndersizedResources();

        // Analyze unused resources
        report.UnusedResources = await FindUnusedResources();

        // Calculate potential savings
        report.PotentialMonthlySavings = CalculatePotentialSavings(report);

        // Generate recommendations
        report.Recommendations = await GenerateOptimizationRecommendations(report);

        return report;
    }

    private async Task<List<ResourceOptimization>> FindOversizedResources()
    {
        var optimizations = new List<ResourceOptimization>();

        // Check CPU utilization
        var cpuUtilization = await _metricsRepository.GetAverageCpuUtilization(TimeSpan.FromDays(30));
        if (cpuUtilization < 30)
        {
            optimizations.Add(new ResourceOptimization
            {
                ResourceType = "CPU",
                CurrentSize = await GetCurrentCpuAllocation(),
                RecommendedSize = Math.Max(2, await GetCurrentCpuAllocation() / 2),
                UtilizationPercentage = cpuUtilization,
                PotentialSavings = CalculateCpuSavings(await GetCurrentCpuAllocation() / 2),
                ConfidenceLevel = cpuUtilization < 20 ? 0.9 : 0.7
            });
        }

        // Check memory utilization
        var memoryUtilization = await _metricsRepository.GetAverageMemoryUtilization(TimeSpan.FromDays(30));
        if (memoryUtilization < 40)
        {
            optimizations.Add(new ResourceOptimization
            {
                ResourceType = "Memory",
                CurrentSize = await GetCurrentMemoryAllocation(),
                RecommendedSize = Math.Max(4, await GetCurrentMemoryAllocation() * 0.75),
                UtilizationPercentage = memoryUtilization,
                PotentialSavings = CalculateMemorySavings(await GetCurrentMemoryAllocation() * 0.25),
                ConfidenceLevel = memoryUtilization < 30 ? 0.9 : 0.7
            });
        }

        return optimizations;
    }

    private async Task<List<ScheduledScalingRecommendation>> GenerateScheduledScalingPlan()
    {
        var recommendations = new List<ScheduledScalingRecommendation>();

        // Analyze usage patterns
        var usagePatterns = await _metricsRepository.GetUsagePatterns(TimeSpan.FromDays(90));

        // Find low usage periods
        var lowUsagePeriods = usagePatterns
            .Where(p => p.AverageUtilization < 40)
            .GroupBy(p => new { p.DayOfWeek, p.Hour })
            .Select(g => new
            {
                DayOfWeek = g.Key.DayOfWeek,
                Hour = g.Key.Hour,
                AverageUtilization = g.Average(p => p.AverageUtilization)
            })
            .Where(p => p.AverageUtilization < 30)
            .ToList();

        foreach (var period in lowUsagePeriods)
        {
            recommendations.Add(new ScheduledScalingRecommendation
            {
                DayOfWeek = period.DayOfWeek,
                Hour = period.Hour,
                Action = ScalingAction.ScaleDown,
                RecommendedInstances = Math.Max(1, await GetCurrentInstanceCount() / 2),
                ExpectedSavings = CalculateHourlySavings(await GetCurrentInstanceCount() / 2),
                Confidence = 0.8
            });
        }

        return recommendations;
    }
}
```

### Reserved Capacity Planning
```csharp
public class ReservedCapacityPlanner
{
    public async Task<ReservedCapacityPlan> GenerateReservedCapacityPlan()
    {
        var plan = new ReservedCapacityPlan();

        // Analyze baseline capacity needs
        var baselineMetrics = await AnalyzeBaselineCapacity();

        // Calculate reserved instance recommendations
        plan.ComputeReservations = await CalculateComputeReservations(baselineMetrics);
        plan.DatabaseReservations = await CalculateDatabaseReservations(baselineMetrics);
        plan.StorageReservations = await CalculateStorageReservations(baselineMetrics);

        // Calculate cost savings
        plan.TotalMonthlySavings = plan.ComputeReservations.Sum(r => r.MonthlySavings) +
                                  plan.DatabaseReservations.Sum(r => r.MonthlySavings) +
                                  plan.StorageReservations.Sum(r => r.MonthlySavings);

        plan.PaybackPeriodMonths = CalculatePaybackPeriod(plan);

        return plan;
    }

    private async Task<BaselineCapacityMetrics> AnalyzeBaselineCapacity()
    {
        // Analyze 90-day historical data to determine baseline needs
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-90);

        var dailyMetrics = await _metricsRepository.GetDailyMetrics(startDate, endDate);

        return new BaselineCapacityMetrics
        {
            MinimumCpuCores = dailyMetrics.Min(m => m.CpuCores),
            AverageCpuCores = dailyMetrics.Average(m => m.CpuCores),
            P95CpuCores = Percentile(dailyMetrics.Select(m => m.CpuCores), 0.95),

            MinimumMemoryGB = dailyMetrics.Min(m => m.MemoryGB),
            AverageMemoryGB = dailyMetrics.Average(m => m.MemoryGB),
            P95MemoryGB = Percentile(dailyMetrics.Select(m => m.MemoryGB), 0.95),

            MinimumStorageGB = dailyMetrics.Min(m => m.StorageGB),
            AverageStorageGB = dailyMetrics.Average(m => m.StorageGB),
            ProjectedStorageGB = ProjectStorageGrowth(dailyMetrics.Select(m => m.StorageGB))
        };
    }
}
```

## Implementation Roadmap

### Phase 1: Foundation (Months 1-2)
```markdown
## Phase 1: Monitoring and Baseline Setup

### Week 1-2: Monitoring Infrastructure
- [ ] Deploy Prometheus and Grafana
- [ ] Configure application metrics collection
- [ ] Set up database performance monitoring
- [ ] Create capacity monitoring dashboard

### Week 3-4: Baseline Establishment
- [ ] Collect 2 weeks of baseline metrics
- [ ] Establish performance baselines
- [ ] Document current capacity limits
- [ ] Create capacity forecasting models

### Week 5-6: Alert Configuration
- [ ] Configure capacity alerting
- [ ] Set up automated scaling triggers
- [ ] Test alert notification systems
- [ ] Document escalation procedures

### Week 7-8: Initial Optimization
- [ ] Implement basic auto-scaling
- [ ] Optimize database queries
- [ ] Configure caching layer
- [ ] Test scaling procedures
```

### Phase 2: Horizontal Scaling (Months 3-4)
```markdown
## Phase 2: Application Layer Scaling

### Week 9-10: Load Balancer Setup
- [ ] Configure nginx load balancer
- [ ] Set up health checks
- [ ] Implement session affinity
- [ ] Test failover scenarios

### Week 11-12: Container Orchestration
- [ ] Deploy Kubernetes cluster
- [ ] Configure Horizontal Pod Autoscaler
- [ ] Implement rolling deployments
- [ ] Set up service mesh (optional)

### Week 13-14: Database Read Scaling
- [ ] Set up read replicas
- [ ] Implement read/write splitting
- [ ] Configure connection pooling
- [ ] Test replica lag monitoring

### Week 15-16: Testing and Validation
- [ ] Conduct load testing
- [ ] Validate auto-scaling behavior
- [ ] Test disaster recovery
- [ ] Document procedures
```

### Phase 3: Advanced Scaling (Months 5-6)
```markdown
## Phase 3: Advanced Scaling Features

### Week 17-18: Predictive Scaling
- [ ] Implement predictive algorithms
- [ ] Configure scheduled scaling
- [ ] Set up machine learning models
- [ ] Test prediction accuracy

### Week 19-20: Database Scaling
- [ ] Implement database partitioning
- [ ] Set up database clustering
- [ ] Configure backup scaling
- [ ] Test failover procedures

### Week 21-22: Cost Optimization
- [ ] Implement resource right-sizing
- [ ] Configure scheduled scaling
- [ ] Set up reserved capacity
- [ ] Optimize storage costs

### Week 23-24: Final Testing
- [ ] Conduct end-to-end testing
- [ ] Validate all scaling scenarios
- [ ] Performance benchmark testing
- [ ] Documentation completion
```

---

This comprehensive capacity planning and scaling guide provides the framework for ensuring the GCC Database Solution can handle growth efficiently while maintaining performance and controlling costs.

---
*Last Updated: September 17, 2025*
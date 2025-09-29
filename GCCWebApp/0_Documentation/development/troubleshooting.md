# GCC Database Application - Troubleshooting Guide

## Table of Contents
1. [Overview](#overview)
2. [General Troubleshooting Steps](#general-troubleshooting-steps)
3. [Authentication Issues](#authentication-issues)
4. [Database Connection Problems](#database-connection-problems)
5. [Performance Issues](#performance-issues)
6. [Application Errors](#application-errors)
7. [Deployment Issues](#deployment-issues)
8. [API Problems](#api-problems)
9. [User Interface Issues](#user-interface-issues)
10. [Security-Related Issues](#security-related-issues)
11. [Monitoring and Logging Problems](#monitoring-and-logging-problems)
12. [Infrastructure Issues](#infrastructure-issues)
13. [Data Issues](#data-issues)
14. [Common Error Codes](#common-error-codes)
15. [Diagnostic Tools](#diagnostic-tools)
16. [Escalation Procedures](#escalation-procedures)

## Overview

This guide provides systematic troubleshooting procedures for common issues encountered in the GCC Database Web Application. Follow the step-by-step instructions to diagnose and resolve problems efficiently.

### Before You Start
1. **Check System Status**: Visit https://status.gccdatabase.com
2. **Review Recent Changes**: Check deployment logs and recent configuration changes
3. **Gather Information**: Collect error messages, timestamps, and user details
4. **Check Logs**: Review application and system logs for relevant entries

## General Troubleshooting Steps

### 1. Quick Health Check
```bash
# Check application health
curl https://localhost:5001/health
curl https://localhost:5001/health/ready

# Check specific components
curl https://localhost:5001/health/live
```

### 2. Log Analysis
```bash
# Check recent application logs
tail -f /var/log/gcc-app/gcc-.log

# Search for specific errors
grep -i "error\|exception\|failed" /var/log/gcc-app/gcc-*.log | tail -20

# Check system logs (Linux)
journalctl -u gcc-app -f

# Check Windows Event Logs (PowerShell)
Get-EventLog -LogName Application -Source "GCC Database" -Newest 10
```

### 3. Resource Monitoring
```bash
# Check CPU and memory usage
top -p $(pgrep -d',' -f gcc-app)

# Check disk space
df -h

# Check network connections
netstat -tulpn | grep :5001

# Windows equivalent
tasklist /FI "IMAGENAME eq GCCWebApp.exe"
```

## Authentication Issues

### Problem: Users Cannot Log In

**Symptoms:**
- Login page shows "Invalid credentials" for valid users
- Login redirects to error page
- Authentication timeouts

**Troubleshooting Steps:**

1. **Check Azure AD B2C Configuration**
```bash
# Verify environment variables
echo $AzureAd__TenantId
echo $AzureAd__ClientId

# Test Azure AD endpoint
curl "https://login.microsoftonline.com/common/discovery/instance?authorization_endpoint=https://login.microsoftonline.com/common/oauth2/authorize&api-version=1.1"
```

2. **Verify Application Registration**
- Check redirect URIs in Azure AD
- Verify client secret hasn't expired
- Confirm required permissions are granted

3. **Check Authentication Logs**
```csharp
// Add detailed logging to authentication events
services.Configure<OpenIdConnectOptions>(options =>
{
    options.Events = new OpenIdConnectEvents
    {
        OnRemoteFailure = context =>
        {
            logger.LogError("Authentication failed: {Error} - {ErrorDescription}",
                context.Failure?.Message, context.Properties?.Items);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            logger.LogInformation("Token validated for user: {User}",
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});
```

**Solutions:**
- Update client secret if expired
- Verify redirect URIs match exactly
- Check for clock skew between servers
- Ensure HTTPS is properly configured

### Problem: Session Timeouts

**Symptoms:**
- Users logged out unexpectedly
- "Session expired" messages
- Frequent re-authentication requests

**Solutions:**
```csharp
// Adjust session configuration
services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Increase session timeout
    options.SlidingExpiration = true; // Reset timeout on activity
    options.Cookie.SameSite = SameSiteMode.Lax; // Adjust for cross-site scenarios
});
```

## Database Connection Problems

### Problem: Cannot Connect to Database

**Symptoms:**
- "A network-related or instance-specific error occurred"
- Connection timeout errors
- Application startup failures

**Troubleshooting Steps:**

1. **Test Database Connectivity**
```bash
# Test SQL Server connection
sqlcmd -S server -U username -P password -Q "SELECT 1"

# Test from application server
telnet database-server 1433

# Check connection string
dotnet ef database update --dry-run
```

2. **Check Database Server Status**
```sql
-- Check SQL Server status
SELECT @@SERVERNAME, @@VERSION, GETDATE();

-- Check active connections
SELECT 
    DB_NAME(dbid) as DatabaseName,
    COUNT(dbid) as ConnectionCount
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid;

-- Check blocking processes
SELECT 
    r.session_id,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.command
FROM sys.dm_exec_requests r
WHERE r.blocking_session_id <> 0;
```

3. **Verify Connection Pool**
```csharp
// Add connection pool monitoring
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging(); // Only in development
});
```

**Solutions:**
- Verify firewall settings allow database connections
- Check SQL Server service status
- Validate connection string parameters
- Increase connection timeout values
- Review connection pool settings

### Problem: Database Deadlocks

**Symptoms:**
- "Transaction was deadlocked" errors
- Slow database operations
- Database timeout errors

**Diagnosis:**
```sql
-- Enable deadlock monitoring
DBCC TRACEON(1204, 1222, -1);

-- Query deadlock information
SELECT 
    t1.resource_type,
    t1.database_id,
    t1.resource_associated_entity_id,
    t1.request_mode,
    t1.request_session_id,
    t2.blocking_session_id
FROM sys.dm_tran_locks as t1
INNER JOIN sys.dm_os_waiting_tasks as t2
ON t1.lock_owner_address = t2.resource_address;
```

**Solutions:**
- Implement retry logic with exponential backoff
- Optimize query performance
- Reduce transaction scope
- Add proper indexes

## Performance Issues

### Problem: Slow Application Response

**Symptoms:**
- Page load times exceed 5 seconds
- API responses are slow
- Users report sluggish interface

**Troubleshooting Steps:**

1. **Application Performance Analysis**
```csharp
// Add performance monitoring middleware
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await _next(context);
        
        stopwatch.Stop();
        
        if (stopwatch.ElapsedMilliseconds > 2000) // Log slow requests
        {
            _logger.LogWarning("Slow request: {Method} {Path} took {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
```

2. **Database Performance Analysis**
```sql
-- Find slow queries
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
ORDER BY avg_elapsed_time DESC;

-- Check index usage
SELECT 
    o.name AS table_name,
    i.name AS index_name,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
INNER JOIN sys.objects o ON i.object_id = o.object_id
WHERE s.database_id = DB_ID()
ORDER BY s.user_seeks + s.user_scans + s.user_lookups DESC;
```

3. **Memory and CPU Analysis**
```bash
# Check memory usage
free -h
ps aux --sort=-%mem | head

# Check CPU usage
top -o +%CPU
iostat -x 1 5

# Windows equivalent
Get-Counter "\Memory\Available MBytes"
Get-Counter "\Processor(_Total)\% Processor Time"
```

**Solutions:**
- Implement caching strategies
- Optimize database queries
- Add database indexes
- Scale up/out infrastructure
- Enable compression

### Problem: High Memory Usage

**Symptoms:**
- OutOfMemoryException errors
- Application becomes unresponsive
- Frequent garbage collection

**Diagnosis:**
```csharp
// Memory diagnostic utility
public class MemoryDiagnostics
{
    public static void LogMemoryStats(ILogger logger)
    {
        var totalMemory = GC.GetTotalMemory(false);
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);
        
        logger.LogInformation("Memory Stats: Total={TotalBytes}, Gen0={Gen0}, Gen1={Gen1}, Gen2={Gen2}",
            totalMemory, gen0Collections, gen1Collections, gen2Collections);
    }
}
```

**Solutions:**
- Implement proper disposal patterns
- Review caching strategies
- Optimize large object allocations
- Consider memory profiling tools

## Application Errors

### Problem: Unhandled Exceptions

**Symptoms:**
- HTTP 500 Internal Server Error
- Application crashes
- Generic error pages displayed

**Troubleshooting:**

1. **Check Exception Logs**
```bash
# Search for exceptions in logs
grep -A 5 -B 5 "Exception\|Error" /var/log/gcc-app/*.log

# Application Insights query
exceptions
| where timestamp > ago(1h)
| order by timestamp desc
| take 50
```

2. **Add Detailed Error Handling**
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                if (feature?.Error != null)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(feature.Error, "Unhandled exception occurred");
                }

                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("An error occurred. Please try again.");
            });
        });
    }
}
```

### Problem: Model Validation Errors

**Symptoms:**
- Forms not submitting
- Validation error messages not displaying
- HTTP 400 Bad Request errors

**Solutions:**
```csharp
// Enhanced model validation
[HttpPost]
public async Task<IActionResult> Create(CertificateModel model)
{
    if (!ModelState.IsValid)
    {
        var errors = ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });
            
        _logger.LogWarning("Model validation failed: {@ValidationErrors}", errors);
        
        return View(model);
    }

    // Process valid model
    return RedirectToAction("Index");
}
```

## Deployment Issues

### Problem: Deployment Failures

**Symptoms:**
- Build pipeline failures
- Application won't start after deployment
- Configuration errors

**Azure App Service Troubleshooting:**
```bash
# Check deployment logs
az webapp log tail --name gcc-database-app --resource-group gcc-prod-rg

# Download deployment package
az webapp deployment source show --name gcc-database-app --resource-group gcc-prod-rg

# Check application settings
az webapp config appsettings list --name gcc-database-app --resource-group gcc-prod-rg
```

**Docker Troubleshooting:**
```bash
# Check container status
docker ps -a

# View container logs
docker logs gcc-app --tail 50 --follow

# Execute commands in container
docker exec -it gcc-app /bin/bash

# Check container resource usage
docker stats gcc-app
```

**IIS Troubleshooting:**
```powershell
# Check application pool status
Get-IISAppPool -Name "GCCAppPool"

# Check failed request logs
Get-ChildItem "C:\inetpub\logs\FailedReqLogFiles" | Sort LastWriteTime -Descending | Select -First 5

# Check event logs
Get-EventLog -LogName System -Source "Microsoft-Windows-WAS" -Newest 10
```

## API Problems

### Problem: API Authentication Failures

**Symptoms:**
- HTTP 401 Unauthorized responses
- Bearer token rejected
- API key authentication fails

**Troubleshooting:**
```bash
# Test API with curl
curl -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Content-Type: application/json" \
     https://api.gccdatabase.com/api/v1/certificates

# Decode JWT token
echo "YOUR_JWT_TOKEN" | cut -d. -f2 | base64 -d | jq .

# Test API key authentication
curl -H "X-API-Key: YOUR_API_KEY" \
     https://api.gccdatabase.com/api/v1/certificates
```

### Problem: API Rate Limiting

**Symptoms:**
- HTTP 429 Too Many Requests
- Rate limit exceeded messages
- Throttled responses

**Solutions:**
```csharp
// Implement exponential backoff
public class ApiRetryHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i <= maxRetries; i++)
        {
            var response = await base.SendAsync(request, cancellationToken);
            
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                return response;

            if (i == maxRetries)
                return response;

            await Task.Delay(delay, cancellationToken);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
        }

        return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
    }
}
```

## User Interface Issues

### Problem: Pages Not Loading

**Symptoms:**
- Blank pages
- JavaScript errors
- CSS not loading

**Browser Troubleshooting:**
```javascript
// Check for JavaScript errors
console.log('Checking for errors...');
window.onerror = function(msg, url, line, col, error) {
    console.error('Error: ', msg, ' at ', url, ':', line);
    return false;
};

// Check resource loading
window.addEventListener('load', function() {
    var resources = performance.getEntriesByType('resource');
    resources.forEach(function(resource) {
        if (resource.responseEnd - resource.requestStart > 2000) {
            console.warn('Slow resource:', resource.name, resource.responseEnd - resource.requestStart, 'ms');
        }
    });
});
```

### Problem: Form Submission Issues

**Symptoms:**
- Forms not submitting
- Data not saving
- CSRF validation errors

**Solutions:**
```html
<!-- Ensure CSRF tokens are included -->
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()
    <!-- Form fields -->
</form>
```

```csharp
// Check CSRF configuration
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

## Security-Related Issues

### Problem: SSL Certificate Issues

**Symptoms:**
- "Certificate not trusted" warnings
- HTTPS connection failures
- Mixed content warnings

**Diagnosis:**
```bash
# Check certificate details
openssl s_client -connect www.gccdatabase.com:443 -servername www.gccdatabase.com

# Check certificate expiration
echo | openssl s_client -connect www.gccdatabase.com:443 2>/dev/null | openssl x509 -noout -dates

# Verify certificate chain
curl -vI https://www.gccdatabase.com
```

**Solutions:**
- Renew expired certificates
- Install intermediate certificates
- Update certificate bindings
- Configure proper SSL/TLS versions

### Problem: CORS Issues

**Symptoms:**
- Cross-origin request blocked
- Preflight request failures
- Missing CORS headers

**Solutions:**
```csharp
services.AddCors(options =>
{
    options.AddPolicy("GCCPolicy",
        builder =>
        {
            builder
                .WithOrigins("https://www.gccdatabase.com", "https://app.gccdatabase.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

app.UseCors("GCCPolicy");
```

## Monitoring and Logging Problems

### Problem: Missing Logs

**Symptoms:**
- No recent log entries
- Application Insights not receiving data
- Log files not rotating

**Solutions:**
```csharp
// Verify logging configuration
public void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.AddDebug();
        builder.AddApplicationInsights();
        
        if (Environment.IsDevelopment())
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        }
    });
}

// Test logging manually
_logger.LogInformation("Test log message at {Timestamp}", DateTime.UtcNow);
```

### Problem: High Log Volume

**Symptoms:**
- Disk space filling up with logs
- Performance impact from excessive logging
- Log analysis difficulties

**Solutions:**
- Adjust log levels in production
- Implement log sampling
- Configure log retention policies
- Use structured logging filters

## Infrastructure Issues

### Problem: High CPU Usage

**Symptoms:**
- Application becomes slow
- Response timeouts
- Server becomes unresponsive

**Diagnosis:**
```bash
# Identify CPU-intensive processes
top -o +%CPU
ps aux --sort=-%cpu | head -10

# Check application threads
dotnet-counters ps
dotnet-counters monitor --process-id PID
```

**Solutions:**
- Scale up server resources
- Optimize CPU-intensive operations
- Implement caching
- Profile application performance

### Problem: Network Connectivity

**Symptoms:**
- Intermittent connection failures
- DNS resolution issues
- Firewall blocking

**Diagnosis:**
```bash
# Test network connectivity
ping database-server
telnet database-server 1433
nslookup database-server

# Check firewall rules
iptables -L
ufw status

# Test specific ports
nc -zv database-server 1433
```

## Data Issues

### Problem: Data Corruption

**Symptoms:**
- Invalid data in database
- Constraint violations
- Missing related records

**Diagnosis:**
```sql
-- Check data integrity
DBCC CHECKDB('GCCDatabase');

-- Find orphaned records
SELECT c.* 
FROM Certificates c
LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
WHERE co.CompanyId IS NULL;

-- Check constraint violations
SELECT * FROM sys.check_constraints WHERE is_disabled = 1;
```

### Problem: Data Synchronization Issues

**Symptoms:**
- Stale data displayed
- Cache inconsistencies
- Replication lag

**Solutions:**
```csharp
// Implement cache invalidation
public class CacheService
{
    public async Task InvalidateCacheAsync(string pattern)
    {
        var keys = await _cache.GetKeysAsync(pattern);
        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key);
        }
    }
}
```

## Common Error Codes

### HTTP Status Codes
| Code | Description | Common Causes | Solutions |
|------|-------------|---------------|-----------|
| 400 | Bad Request | Invalid input data, malformed JSON | Validate request format and data |
| 401 | Unauthorized | Missing/invalid authentication | Check authentication tokens |
| 403 | Forbidden | Insufficient permissions | Verify user roles and permissions |
| 404 | Not Found | Resource doesn't exist | Check URLs and resource IDs |
| 409 | Conflict | Duplicate data, concurrency issues | Handle conflicts gracefully |
| 429 | Too Many Requests | Rate limit exceeded | Implement retry logic |
| 500 | Internal Server Error | Application errors | Check logs for exceptions |
| 502 | Bad Gateway | Upstream service issues | Check dependent services |
| 503 | Service Unavailable | Service down/overloaded | Check service status |

### Application-Specific Error Codes
| Code | Description | Cause | Solution |
|------|-------------|-------|---------|
| GCC001 | Certificate validation failed | Invalid certificate data | Check certificate fields |
| GCC002 | Company not found | Invalid company ID | Verify company exists |
| GCC003 | Duplicate certificate number | Certificate already exists | Use unique certificate number |
| GCC004 | Database connection failed | Network/authentication issue | Check connection string |
| GCC005 | File upload failed | File size/format issue | Check file constraints |

## Diagnostic Tools

### Application Diagnostic Commands
```bash
# .NET diagnostic tools
dotnet --info
dotnet --list-runtimes
dotnet --list-sdks

# Process information
dotnet-counters ps
dotnet-counters monitor --process-id PID

# Memory dumps
dotnet-dump collect -p PID

# Performance traces
dotnet-trace collect -p PID --duration 00:00:30
```

### Database Diagnostic Queries
```sql
-- Current database activity
SELECT 
    s.session_id,
    s.login_name,
    s.status,
    r.command,
    r.wait_type,
    r.wait_time
FROM sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
WHERE s.is_user_process = 1;

-- Lock information
SELECT 
    resource_type,
    resource_description,
    request_mode,
    request_status,
    request_session_id
FROM sys.dm_tran_locks
WHERE resource_database_id = DB_ID();

-- Performance counters
SELECT 
    object_name,
    counter_name,
    cntr_value
FROM sys.dm_os_performance_counters
WHERE object_name LIKE '%Database%'
   OR object_name LIKE '%Memory%'
   OR object_name LIKE '%CPU%';
```

### Log Analysis Tools
```bash
# Analyze logs with common tools
grep -i "error\|exception\|failed" application.log | wc -l
awk '/ERROR/ {print $1, $2, $NF}' application.log
sed -n '/2024-08-08 10:00/,/2024-08-08 11:00/p' application.log

# JSON log parsing
cat application.log | jq 'select(.Level == "Error")'
```

## Escalation Procedures

### Level 1 Support (Help Desk)
- **Scope**: User account issues, basic functionality questions
- **Response Time**: 4 hours
- **Contact**: support@gccdatabase.com

### Level 2 Support (Technical Team)
- **Scope**: Application bugs, configuration issues, moderate performance problems
- **Response Time**: 2 hours (business hours)
- **Contact**: technical-support@gccdatabase.com

### Level 3 Support (Development Team)
- **Scope**: Critical bugs, security issues, major performance problems
- **Response Time**: 1 hour (24/7 for critical)
- **Contact**: dev-team@gccdatabase.com

### Critical Incident Escalation
- **Triggers**: System down, security breach, data loss
- **Process**:
  1. Immediate notification to on-call engineer
  2. Incident commander assignment
  3. War room activation
  4. Stakeholder communication
- **Contact**: emergency@gccdatabase.com or +1-555-0199

### Escalation Matrix
| Issue Severity | Response Time | Escalation Path |
|----------------|---------------|-----------------|
| Critical (P1) | 15 minutes | On-call → Dev Lead → CTO |
| High (P2) | 1 hour | Tech Support → Dev Team |
| Medium (P3) | 4 hours | Help Desk → Tech Support |
| Low (P4) | 24 hours | Help Desk |

## Support Resources

### Documentation
- **Knowledge Base**: https://docs.gccdatabase.com
- **API Documentation**: https://api-docs.gccdatabase.com
- **Status Page**: https://status.gccdatabase.com
- **Community Forum**: https://community.gccdatabase.com

### Contact Information
- **General Support**: support@gccdatabase.com
- **Technical Issues**: technical@gccdatabase.com
- **Security Incidents**: security@gccdatabase.com
- **Emergency Hotline**: +1-555-0199

### Internal Tools
- **Monitoring Dashboard**: https://monitoring.gccdatabase.com
- **Log Analytics**: https://logs.gccdatabase.com
- **Deployment Pipeline**: https://ci-cd.gccdatabase.com
- **Error Tracking**: https://errors.gccdatabase.com

## Version History

- **v1.0.0** - Initial troubleshooting guide
- Last Updated: August 2024
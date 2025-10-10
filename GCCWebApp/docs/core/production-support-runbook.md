# Production Support Runbook - GCC Database Solution

## Overview
This runbook provides comprehensive operational procedures for supporting the GCC Database Solution in production. It includes troubleshooting guides, escalation procedures, maintenance tasks, and emergency response protocols.

## Quick Reference

### Emergency Contacts
```
Primary On-Call: [Phone] [Email]
Secondary On-Call: [Phone] [Email]
CTO: [Phone] [Email]
Database Administrator: [Phone] [Email]
Security Lead: [Phone] [Email]
```

### Critical System URLs
```
Production Application: https://gcc.production.com
Health Check: https://gcc.production.com/health
Monitoring Dashboard: https://monitor.gcc.production.com
Log Aggregation: https://logs.gcc.production.com
Admin Panel: https://gcc.production.com/Admin
```

### Service Status Page
```
Status Dashboard: https://status.gcc.production.com
Twitter Updates: @GCCStatus
Slack Channel: #gcc-production-alerts
```

## System Architecture Overview

### Application Components
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Load Balancer │───▶│   Web App (x3)  │───▶│   Database      │
│   (nginx)       │    │   ASP.NET Core  │    │   SQL Server    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       ▼                       │
         │              ┌─────────────────┐              │
         │              │   Redis Cache   │              │
         │              └─────────────────┘              │
         │                                               │
         ▼                                               ▼
┌─────────────────┐                            ┌─────────────────┐
│   File Storage  │                            │   Backup Store  │
│   Azure Blob    │                            │   Azure Backup  │
└─────────────────┘                            └─────────────────┘
```

### Key Services and Ports
```
Service                Port    Protocol   Health Check
--------------------- ------- ---------- -------------------------
Web Application       8080    HTTP/HTTPS /health
Database              1433    SQL        SQL connection test
Redis Cache           6379    TCP        PING command
Load Balancer         80/443  HTTP/HTTPS nginx status
File Storage          443     HTTPS      Azure Blob health
```

## Service Management

### Application Service Control

#### Windows Service (IIS)
```powershell
# Check service status
Get-Service -Name "GCCWebApp"
Get-Process -Name "w3wp" | Where-Object {$_.ProcessName -eq "w3wp"}

# Start/Stop/Restart service
Start-Service -Name "GCCWebApp"
Stop-Service -Name "GCCWebApp"
Restart-Service -Name "GCCWebApp"

# IIS Application Pool management
Import-Module WebAdministration
Get-IISAppPool -Name "GCCWebApp"
Start-WebAppPool -Name "GCCWebApp"
Stop-WebAppPool -Name "GCCWebApp"
Restart-WebAppPool -Name "GCCWebApp"
```

#### Docker Container Management
```bash
# Check container status
docker ps | grep gcc-webapp
docker logs gcc-webapp --tail 100

# Container operations
docker stop gcc-webapp
docker start gcc-webapp
docker restart gcc-webapp

# Health check
docker exec gcc-webapp curl -f http://localhost:8080/health
```

#### Kubernetes Deployment
```bash
# Check pod status
kubectl get pods -n gcc-production -l app=gcc-webapp

# View logs
kubectl logs -n gcc-production -l app=gcc-webapp --tail=100

# Restart deployment
kubectl rollout restart deployment/gcc-webapp -n gcc-production

# Scale deployment
kubectl scale deployment gcc-webapp --replicas=5 -n gcc-production
```

### Database Service Management

#### SQL Server Management
```sql
-- Check database status
SELECT name, state_desc, user_access_desc
FROM sys.databases
WHERE name = 'GCCDatabase';

-- Check active connections
SELECT
    DB_NAME(dbid) as Database_Name,
    COUNT(dbid) as Connection_Count
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid, DB_NAME(dbid);

-- Kill blocking processes
SELECT
    session_id,
    blocking_session_id,
    wait_type,
    wait_time,
    last_wait_type
FROM sys.dm_exec_requests
WHERE blocking_session_id != 0;

-- Kill specific session (if needed)
KILL [session_id];
```

```powershell
# SQL Server Service Control
Get-Service -Name "MSSQLSERVER"
Start-Service -Name "MSSQLSERVER"
Stop-Service -Name "MSSQLSERVER"
Restart-Service -Name "MSSQLSERVER"

# Check SQL Server error log
Get-EventLog -LogName Application -Source "MSSQLSERVER" -Newest 50
```

### Cache Service Management

#### Redis Management
```bash
# Connect to Redis
redis-cli

# Check Redis status
redis-cli ping

# Monitor Redis
redis-cli monitor

# Check memory usage
redis-cli info memory

# Clear cache (emergency only)
redis-cli flushall

# Check specific cache keys
redis-cli keys "gcc:*"
```

## Troubleshooting Guide

### Application Issues

#### Issue: Application Not Responding
**Symptoms**: HTTP 500 errors, timeouts, no response

**Immediate Actions**:
1. Check application health endpoint
2. Review recent application logs
3. Verify database connectivity
4. Check system resources

**Diagnostic Steps**:
```bash
# Check health endpoint
curl -I https://gcc.production.com/health

# Check application logs
tail -f /var/log/gcc-webapp/application.log

# Check system resources
top
df -h
free -m

# Check network connectivity
netstat -tulpn | grep :8080
```

**Resolution Steps**:
```bash
# Restart application
sudo systemctl restart gcc-webapp

# If Docker:
docker restart gcc-webapp

# If Kubernetes:
kubectl rollout restart deployment/gcc-webapp -n gcc-production

# Verify resolution
curl https://gcc.production.com/health
```

#### Issue: Slow Response Times
**Symptoms**: Page load times > 3 seconds, API timeouts

**Diagnostic Steps**:
```sql
-- Check for blocking queries
SELECT
    r.session_id,
    r.blocking_session_id,
    t.text as sql_text,
    r.wait_type,
    r.wait_time,
    r.cpu_time,
    r.logical_reads
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50
ORDER BY r.cpu_time DESC;

-- Check index usage
SELECT
    OBJECT_NAME(s.object_id) as TableName,
    i.name as IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE OBJECT_NAME(s.object_id) LIKE '%Certificate%'
ORDER BY s.user_seeks + s.user_scans + s.user_lookups DESC;
```

**Resolution Steps**:
1. Identify slow queries and optimize
2. Check for missing indexes
3. Clear application cache
4. Restart application pools
5. Scale application instances if needed

#### Issue: Memory Leaks
**Symptoms**: Increasing memory usage, eventual OutOfMemory exceptions

**Diagnostic Steps**:
```bash
# Check memory usage
ps aux | grep dotnet
top -p $(pgrep dotnet)

# Monitor garbage collection
dotnet-counters monitor --process-id $(pgrep dotnet) --counters System.Runtime

# Generate memory dump
dotnet-dump collect -p $(pgrep dotnet)
```

**Resolution Steps**:
1. Restart application
2. Review recent code changes
3. Analyze memory dump
4. Implement memory optimization

### Database Issues

#### Issue: Database Connection Failures
**Symptoms**: SQL connection timeout errors, login failures

**Diagnostic Steps**:
```sql
-- Check connection limits
SELECT
    @@MAX_CONNECTIONS as Max_Connections,
    COUNT(*) as Current_Connections
FROM sys.dm_exec_sessions;

-- Check for deadlocks
SELECT
    creation_time,
    deadlock_graph
FROM sys.dm_xe_sessions s
JOIN sys.dm_xe_session_events e ON s.address = e.event_session_address
WHERE s.name = 'system_health';
```

**Resolution Steps**:
```sql
-- Kill idle connections
DECLARE @kill varchar(8000) = '';
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'
FROM sys.dm_exec_sessions
WHERE program_name = '.Net SqlClient Data Provider'
AND last_request_end_time < DATEADD(minute, -30, GETDATE());

EXEC(@kill);
```

#### Issue: Database Locks and Blocking
**Symptoms**: Application timeouts, slow queries

**Diagnostic Steps**:
```sql
-- Identify blocking chains
WITH BlockingChain AS (
    SELECT
        session_id,
        blocking_session_id,
        wait_type,
        wait_time,
        wait_resource,
        sql_handle
    FROM sys.dm_exec_requests
    WHERE blocking_session_id != 0
)
SELECT
    bc.*,
    st.text as blocked_query,
    bs.text as blocking_query
FROM BlockingChain bc
CROSS APPLY sys.dm_exec_sql_text(bc.sql_handle) st
CROSS APPLY sys.dm_exec_sql_text(
        (SELECT sql_handle FROM sys.dm_exec_requests WHERE session_id = bc.blocking_session_id)
    ) bs;
```

**Resolution Steps**:
1. Identify root blocker
2. Kill blocking session if necessary
3. Optimize problematic queries
4. Review transaction isolation levels

### Network and Connectivity Issues

#### Issue: Load Balancer Problems
**Symptoms**: Intermittent 502/503 errors, uneven traffic distribution

**Diagnostic Steps**:
```bash
# Check nginx status
sudo nginx -t
sudo systemctl status nginx

# Check upstream health
curl -H "Host: internal-health" http://backend-server:8080/health

# Review nginx logs
tail -f /var/log/nginx/error.log
tail -f /var/log/nginx/access.log
```

**Resolution Steps**:
```bash
# Reload nginx configuration
sudo nginx -s reload

# Restart nginx
sudo systemctl restart nginx

# Check backend connectivity
for server in server1 server2 server3; do
    curl -s http://$server:8080/health
done
```

#### Issue: SSL Certificate Problems
**Symptoms**: Certificate warnings, HTTPS failures

**Diagnostic Steps**:
```bash
# Check certificate expiration
openssl x509 -in /etc/ssl/certs/gcc.crt -noout -dates

# Test SSL connectivity
openssl s_client -connect gcc.production.com:443

# Check certificate chain
curl -vI https://gcc.production.com
```

**Resolution Steps**:
1. Renew SSL certificate
2. Update certificate in load balancer
3. Restart web services
4. Verify SSL configuration

## Maintenance Procedures

### Daily Maintenance Tasks

#### Automated Health Checks
```bash
#!/bin/bash
# daily-health-check.sh

# Check application health
echo "Checking application health..."
curl -f https://gcc.production.com/health || echo "ALERT: Health check failed"

# Check disk space
echo "Checking disk space..."
df -h | awk '$5 > 80 { print "WARNING: " $0 }'

# Check memory usage
echo "Checking memory usage..."
free -m | awk 'NR==2{printf "Memory Usage: %s/%sMB (%.2f%%)\n", $3,$2,$3*100/$2 }'

# Check database connections
echo "Checking database connections..."
sqlcmd -S localhost -E -Q "SELECT COUNT(*) as ActiveConnections FROM sys.dm_exec_sessions WHERE is_user_process = 1"

# Check error logs
echo "Checking for errors in last 24 hours..."
grep -i error /var/log/gcc-webapp/application.log | grep "$(date '+%Y-%m-%d')" | wc -l

echo "Daily health check completed at $(date)"
```

#### Log Rotation and Cleanup
```bash
#!/bin/bash
# log-cleanup.sh

# Archive old logs
find /var/log/gcc-webapp/ -name "*.log" -mtime +7 -exec gzip {} \;

# Delete old compressed logs
find /var/log/gcc-webapp/ -name "*.log.gz" -mtime +30 -delete

# Clean up temporary files
find /tmp -name "gcc-*" -mtime +1 -delete

# Database log cleanup
sqlcmd -S localhost -E -Q "
    BACKUP LOG GCCDatabase TO DISK = 'NUL';
    DBCC SHRINKFILE('GCCDatabase_Log', 1);
"
```

### Weekly Maintenance Tasks

#### Performance Monitoring
```sql
-- Weekly performance review
SELECT
    OBJECT_NAME(object_id) as TableName,
    index_id,
    avg_fragmentation_in_percent,
    page_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED')
WHERE avg_fragmentation_in_percent > 30
AND page_count > 1000
ORDER BY avg_fragmentation_in_percent DESC;

-- Index maintenance
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql = @sql +
    CASE
        WHEN avg_fragmentation_in_percent > 30 THEN
            'ALTER INDEX ALL ON ' + OBJECT_NAME(object_id) + ' REBUILD;' + CHAR(13)
        WHEN avg_fragmentation_in_percent > 10 THEN
            'ALTER INDEX ALL ON ' + OBJECT_NAME(object_id) + ' REORGANIZE;' + CHAR(13)
        ELSE ''
    END
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED')
WHERE avg_fragmentation_in_percent > 10;

EXEC sp_executesql @sql;
```

#### Security Updates
```bash
#!/bin/bash
# weekly-security-updates.sh

# Check for security updates
apt list --upgradable | grep -i security

# Update system packages
sudo apt update && sudo apt upgrade -y

# Update Docker images
docker pull gcc-webapp:latest

# Scan for vulnerabilities
docker scan gcc-webapp:latest

# Update SSL certificates if needed
certbot renew --quiet
```

### Monthly Maintenance Tasks

#### Database Maintenance
```sql
-- Monthly database maintenance
-- Update statistics
UPDATE STATISTICS Certificates;
UPDATE STATISTICS Companies;
UPDATE STATISTICS Products;

-- Check database integrity
DBCC CHECKDB('GCCDatabase') WITH NO_INFOMSGS;

-- Backup database
BACKUP DATABASE GCCDatabase
TO DISK = 'C:\Backups\GCCDatabase_Monthly_' + FORMAT(GETDATE(), 'yyyy-MM-dd') + '.bak'
WITH COMPRESSION, CHECKSUM;

-- Archive old audit logs
DELETE FROM AuditLogs WHERE CreatedDate < DATEADD(month, -6, GETDATE());
```

#### Capacity Planning Review
```bash
# Monthly capacity review
echo "=== Monthly Capacity Report ===" > /tmp/capacity-report.txt
echo "Date: $(date)" >> /tmp/capacity-report.txt
echo "" >> /tmp/capacity-report.txt

# Disk usage trends
echo "Disk Usage:" >> /tmp/capacity-report.txt
df -h >> /tmp/capacity-report.txt
echo "" >> /tmp/capacity-report.txt

# Memory usage trends
echo "Memory Usage:" >> /tmp/capacity-report.txt
free -h >> /tmp/capacity-report.txt
echo "" >> /tmp/capacity-report.txt

# Database growth
echo "Database Size:" >> /tmp/capacity-report.txt
sqlcmd -S localhost -E -Q "
    SELECT
        name,
        size * 8 / 1024 as SizeMB,
        max_size * 8 / 1024 as MaxSizeMB
    FROM sys.database_files
" >> /tmp/capacity-report.txt

# Send report
mail -s "Monthly Capacity Report" admin@gcc.com < /tmp/capacity-report.txt
```

## Backup and Recovery Procedures

### Database Backup Verification
```sql
-- Verify backup integrity
RESTORE VERIFYONLY FROM DISK = 'C:\Backups\GCCDatabase_Latest.bak';

-- Check backup history
SELECT
    bs.database_name,
    bs.backup_start_date,
    bs.backup_finish_date,
    bs.backup_size/1024/1024 as BackupSizeMB,
    bmf.physical_device_name
FROM msdb.dbo.backupset bs
INNER JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
WHERE bs.database_name = 'GCCDatabase'
ORDER BY bs.backup_start_date DESC;
```

### Application Configuration Backup
```bash
#!/bin/bash
# backup-configuration.sh

BACKUP_DIR="/backup/config/$(date +%Y-%m-%d)"
mkdir -p $BACKUP_DIR

# Backup application configuration
cp /app/appsettings.Production.json $BACKUP_DIR/
cp /app/web.config $BACKUP_DIR/

# Backup nginx configuration
cp /etc/nginx/sites-available/gcc $BACKUP_DIR/

# Backup SSL certificates
cp -r /etc/ssl/certs/gcc* $BACKUP_DIR/

# Create archive
tar -czf /backup/config/gcc-config-$(date +%Y-%m-%d).tar.gz -C /backup/config $(date +%Y-%m-%d)

echo "Configuration backup completed: /backup/config/gcc-config-$(date +%Y-%m-%d).tar.gz"
```

## Monitoring and Alerting

### Health Check Endpoints
```csharp
// Health check implementation
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

// Custom health checks
public class CacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        try
        {
            var testKey = "health-check-" + Guid.NewGuid();
            await _cache.SetStringAsync(testKey, "test", TimeSpan.FromSeconds(10));
            var result = await _cache.GetStringAsync(testKey);
            await _cache.RemoveAsync(testKey);

            return result == "test"
                ? HealthCheckResult.Healthy("Cache is responsive")
                : HealthCheckResult.Degraded("Cache test failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache connection failed", ex);
        }
    }
}
```

### Alert Thresholds
```yaml
# Alert configuration
alerts:
  response_time:
    warning: 1000ms
    critical: 3000ms

  error_rate:
    warning: 1%
    critical: 5%

  disk_usage:
    warning: 80%
    critical: 90%

  memory_usage:
    warning: 85%
    critical: 95%

  database_connections:
    warning: 80%
    critical: 95%
```

### Monitoring Queries
```sql
-- Performance monitoring queries
-- Top resource-consuming queries
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
ORDER BY qs.total_worker_time / qs.execution_count DESC;

-- Wait statistics
SELECT
    wait_type,
    wait_time_ms,
    waiting_tasks_count,
    signal_wait_time_ms,
    wait_time_ms - signal_wait_time_ms as resource_wait_time_ms
FROM sys.dm_os_wait_stats
WHERE wait_time_ms > 0
AND wait_type NOT IN ('CLR_SEMAPHORE', 'LAZYWRITER_SLEEP', 'RESOURCE_QUEUE',
                     'SLEEP_TASK', 'SLEEP_SYSTEMTASK', 'SQLTRACE_WAIT', 'WAITFOR')
ORDER BY wait_time_ms DESC;
```

## Emergency Procedures

### Immediate Response Checklist
```markdown
## EMERGENCY RESPONSE CHECKLIST

### STEP 1: ASSESS SITUATION (0-5 minutes)
- [ ] Identify severity level (P1-Critical, P2-High, P3-Medium, P4-Low)
- [ ] Check system status dashboard
- [ ] Verify customer impact
- [ ] Notify on-call manager if P1/P2

### STEP 2: INITIAL RESPONSE (5-15 minutes)
- [ ] Join emergency bridge call (if P1)
- [ ] Begin status page updates
- [ ] Start collecting diagnostic information
- [ ] Implement immediate mitigation if available

### STEP 3: DIAGNOSIS (15-30 minutes)
- [ ] Review application logs
- [ ] Check database performance
- [ ] Verify infrastructure status
- [ ] Identify root cause

### STEP 4: RESOLUTION (30+ minutes)
- [ ] Implement fix or rollback
- [ ] Verify system stability
- [ ] Update status page
- [ ] Communicate resolution to stakeholders

### STEP 5: POST-INCIDENT (24-48 hours)
- [ ] Schedule post-incident review
- [ ] Document lessons learned
- [ ] Implement preventive measures
- [ ] Update runbook procedures
```

### Severity Level Definitions
```markdown
## P1 - CRITICAL (15 minute response)
- Total system outage
- Data corruption or loss
- Security breach
- Payment processing failure

## P2 - HIGH (30 minute response)
- Significant feature outage
- Major performance degradation
- Authentication issues
- Database connectivity problems

## P3 - MEDIUM (4 hour response)
- Minor feature issues
- Moderate performance issues
- Non-critical third-party failures

## P4 - LOW (24 hour response)
- Cosmetic issues
- Documentation problems
- Minor enhancement requests
```

### Rollback Procedures
```bash
#!/bin/bash
# emergency-rollback.sh

# Get current version
CURRENT_VERSION=$(docker ps --format "table {{.Image}}" | grep gcc-webapp | cut -d: -f2)
echo "Current version: $CURRENT_VERSION"

# List available rollback versions
echo "Available versions for rollback:"
docker images gcc-webapp --format "table {{.Tag}}\t{{.CreatedAt}}" | head -5

# Prompt for rollback version
read -p "Enter version to rollback to: " ROLLBACK_VERSION

# Confirm rollback
read -p "Are you sure you want to rollback to $ROLLBACK_VERSION? (yes/no): " CONFIRM

if [ "$CONFIRM" = "yes" ]; then
    echo "Starting rollback to $ROLLBACK_VERSION..."

    # Stop current container
    docker stop gcc-webapp

    # Remove current container
    docker rm gcc-webapp

    # Start rollback version
    docker run -d --name gcc-webapp -p 80:8080 gcc-webapp:$ROLLBACK_VERSION

    # Verify rollback
    sleep 30
    curl -f http://localhost/health && echo "Rollback successful" || echo "Rollback failed"

    # Update load balancer health check
    # nginx reload or health check update

else
    echo "Rollback cancelled"
fi
```

## Performance Optimization

### Database Performance Tuning
```sql
-- Index optimization recommendations
SELECT
    migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) AS improvement_measure,
    'CREATE INDEX [IX_' + OBJECT_NAME(mid.object_id) + '_' + REPLACE(REPLACE(REPLACE(ISNULL(mid.equality_columns,''),', ','_'),'[',''),']','') + ']' +
    ' ON ' + mid.statement + ' (' + ISNULL (mid.equality_columns,'') +
    CASE WHEN mid.equality_columns IS NOT NULL AND mid.inequality_columns IS NOT NULL THEN ',' ELSE '' END +
    ISNULL (mid.inequality_columns, '') + ')' +
    ISNULL (' INCLUDE (' + mid.included_columns + ')', '') AS create_index_statement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
WHERE migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) * (migs.user_seeks + migs.user_scans) > 10
ORDER BY improvement_measure DESC;

-- Query optimization
-- Enable query store
ALTER DATABASE GCCDatabase SET QUERY_STORE = ON;
ALTER DATABASE GCCDatabase SET QUERY_STORE (OPERATION_MODE = READ_WRITE);

-- Review top resource consuming queries
SELECT TOP 10
    qsq.query_id,
    qsqt.query_sql_text,
    qsrs.avg_duration / 1000 as avg_duration_ms,
    qsrs.avg_cpu_time / 1000 as avg_cpu_time_ms,
    qsrs.avg_logical_io_reads,
    qsrs.count_executions
FROM sys.query_store_query qsq
INNER JOIN sys.query_store_query_text qsqt ON qsq.query_text_id = qsqt.query_text_id
INNER JOIN sys.query_store_runtime_stats qsrs ON qsq.query_id = qsrs.query_id
ORDER BY qsrs.avg_duration DESC;
```

### Application Performance Tuning
```csharp
// Connection pooling optimization
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3);
    });
}, ServiceLifetime.Scoped);

// Memory caching
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // MB
    options.CompactionPercentage = 0.25;
});

// Response caching
services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
    options.UseCaseSensitivePaths = false;
});

// Background services for performance
services.AddHostedService<CacheWarmupService>();
services.AddHostedService<PerformanceMonitoringService>();
```

## Security Procedures

### Security Incident Response
```markdown
## SECURITY INCIDENT RESPONSE

### IMMEDIATE ACTIONS (0-30 minutes)
1. **Isolate affected systems**
   - Disconnect from network if necessary
   - Block suspicious IP addresses
   - Disable compromised user accounts

2. **Preserve evidence**
   - Take system snapshots
   - Preserve log files
   - Document all actions taken

3. **Notify stakeholders**
   - Security team
   - Legal counsel (if required)
   - Regulatory bodies (if required)

### INVESTIGATION (30 minutes - 24 hours)
1. **Analyze logs and evidence**
2. **Determine scope of breach**
3. **Identify attack vectors**
4. **Assess data impact**

### CONTAINMENT AND RECOVERY (24-72 hours)
1. **Apply security patches**
2. **Restore from clean backups**
3. **Reset all credentials**
4. **Implement additional monitoring**

### POST-INCIDENT (1-2 weeks)
1. **Conduct lessons learned session**
2. **Update security procedures**
3. **Implement preventive measures**
4. **Provide user/customer notifications**
```

### Security Monitoring Commands
```bash
# Check for suspicious login attempts
grep "Failed password" /var/log/auth.log | tail -20

# Monitor network connections
netstat -tuln | grep LISTEN
ss -tuln | grep :80

# Check for unauthorized processes
ps aux | grep -v "^root\|^www-data\|^gcc"

# Review file permissions
find /app -type f -perm /o+w -ls

# Check for recent file modifications
find /app -type f -mtime -1 -ls
```

## Disaster Recovery Procedures

### Recovery Time Objectives (RTO)
```
Component               RTO        RPO
Application            30 min     15 min
Database              60 min     5 min
File Storage          120 min    60 min
Full System           4 hours    30 min
```

### Disaster Recovery Steps
```markdown
## DISASTER RECOVERY PROCEDURE

### STEP 1: ASSESSMENT (0-30 minutes)
- [ ] Determine scope of disaster
- [ ] Activate disaster recovery team
- [ ] Notify stakeholders
- [ ] Begin documentation

### STEP 2: INFRASTRUCTURE RECOVERY (30-120 minutes)
- [ ] Provision new infrastructure
- [ ] Restore network connectivity
- [ ] Deploy application containers
- [ ] Verify security configurations

### STEP 3: DATA RECOVERY (60-180 minutes)
- [ ] Restore database from backup
- [ ] Verify data integrity
- [ ] Restore file storage
- [ ] Test data accessibility

### STEP 4: APPLICATION RECOVERY (120-240 minutes)
- [ ] Deploy application code
- [ ] Update configuration
- [ ] Test critical functionality
- [ ] Enable monitoring

### STEP 5: VALIDATION (240-300 minutes)
- [ ] Run full system tests
- [ ] Verify user access
- [ ] Check integrations
- [ ] Update DNS/routing

### STEP 6: COMMUNICATION (300+ minutes)
- [ ] Notify users of restoration
- [ ] Update status page
- [ ] Schedule post-mortem
- [ ] Document lessons learned
```

---

This production support runbook provides comprehensive guidance for maintaining, troubleshooting, and supporting the GCC Database Solution in production environments. Regular updates and team training ensure effective incident response and system reliability.

---
*Last Updated: September 17, 2025*
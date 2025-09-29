# GCC Database Solution - Administrator Guide

## Table of Contents

1. [System Administration Overview](#system-administration-overview)
2. [User Management](#user-management)
3. [Role and Permission Management](#role-and-permission-management)
4. [System Configuration](#system-configuration)
5. [Database Administration](#database-administration)
6. [API Management](#api-management)
7. [Security Administration](#security-administration)
8. [Monitoring and Performance](#monitoring-and-performance)
9. [Backup and Recovery](#backup-and-recovery)
10. [Deployment and Updates](#deployment-and-updates)
11. [Troubleshooting and Support](#troubleshooting-and-support)
12. [Audit and Compliance](#audit-and-compliance)

---

## System Administration Overview

### Administrator Responsibilities

**Primary Responsibilities:**
- User account management and role assignments
- System configuration and maintenance
- Security policy enforcement
- Performance monitoring and optimization
- Data backup and recovery procedures
- System updates and deployments
- Compliance and audit support

**Access Requirements:**
- Administrator role in the system
- Secure administrator workstation
- Two-factor authentication enabled
- VPN access (if required)
- Emergency contact information updated

### Admin Panel Access

1. **Login** with administrator credentials
2. **Navigate** to `/Admin` or click "Admin Panel" in the navigation
3. **Available Admin Sections:**
   - User Management
   - Role Management
   - Company Management
   - Product Management
   - Partner Management
   - Certificate Types
   - System Settings
   - Audit Logs
   - API Key Management

### Critical Admin Guidelines

**Security Best Practices:**
- ⚠️ **Admin and primary user accounts cannot be deleted** (system requirement)
- Always use two-factor authentication
- Regular password changes (every 90 days)
- Log out when leaving workstation
- Review audit logs regularly
- Never share administrator credentials

**Change Management:**
- Document all configuration changes
- Test changes in development environment first
- Schedule maintenance during off-peak hours
- Notify users of planned maintenance
- Have rollback procedures ready

---

## User Management

### Creating User Accounts

1. **Navigate** to Admin → User Management
2. **Click** "Create New User"
3. **Fill out** user information:
   - First Name, Last Name
   - Email Address (must be unique)
   - Phone Number
   - Company/Organization
   - Job Title
4. **Assign** initial role (can be changed later)
5. **Set** account preferences:
   - Email notifications enabled/disabled
   - Account expiration date (if temporary)
   - Force password change on first login
6. **Send** account creation email to user

### Managing Existing Users

**View User List:**
- All users displayed with key information
- Search and filter capabilities
- Sort by name, email, role, last login
- Export user list to Excel/CSV

**Edit User Account:**
1. **Click** "Edit" next to user name
2. **Modify** user information as needed
3. **Update** role assignments
4. **Set** account status (Active/Inactive/Suspended)
5. **Save** changes

**User Account Actions:**
- **Reset Password:** Send password reset email
- **Disable Account:** Temporarily disable access
- **Suspend Account:** Lock account pending investigation
- **Delete Account:** ⚠️ **Not available for Admin and primary users**
- **View Activity:** See user's recent system activity
- **Impersonate User:** Access system as the user (audit logged)

### Bulk User Management

**Import Users from CSV:**
1. **Download** user import template
2. **Fill out** template with user data
3. **Upload** CSV file via Admin panel
4. **Review** import preview
5. **Confirm** and process import

**Bulk Updates:**
- Select multiple users with checkboxes
- Apply bulk actions (role changes, email notifications, etc.)
- Export selected user data

### User Account Security

**Account Lockout Policies:**
- Failed login attempts: 5 attempts
- Lockout duration: 15 minutes
- Manual unlock available to administrators

**Password Policies:**
- Minimum 8 characters
- Must include uppercase, lowercase, number, special character
- Cannot reuse last 5 passwords
- Password expiration: 90 days (configurable)

**Session Management:**
- Maximum session duration: 24 hours
- Idle timeout: 8 hours
- Concurrent session limit: 3 sessions per user
- Force logout capability for administrators

---

## Role and Permission Management

### Available Roles

**Admin Role:**
- Full system access
- User management capabilities
- System configuration access
- ⚠️ **Cannot be deleted** (system requirement)
- All certificate management functions
- API key management
- Audit log access

**Analyst Role:**
- Certificate creation and editing
- Company and product management
- Dashboard and reporting access
- Export capabilities
- Cannot manage users or system settings

**External Role:**
- Limited access to partner-specific certificates
- Search and view capabilities
- Download permissions
- No administrative functions

**Internal Role:**
- Broader certificate access than External
- Internal system features
- Reporting capabilities
- No certificate management

### Role Assignment

**Assign Roles to Users:**
1. **Go to** Admin → User Management
2. **Select** user to modify
3. **Click** "Edit Roles"
4. **Select** appropriate role(s)
5. **Save** changes

**Role-Based Access Matrix:**

| Feature | Admin | Analyst | Internal | External |
|---------|-------|---------|----------|----------|
| View Public Certificates | ✓ | ✓ | ✓ | ✓ |
| View All Certificates | ✓ | ✓ | ✓ | ❌ |
| Create Certificates | ✓ | ✓ | ❌ | ❌ |
| Edit Certificates | ✓ | ✓ | ❌ | ❌ |
| Delete Certificates | ✓ | ❌ | ❌ | ❌ |
| User Management | ✓ | ❌ | ❌ | ❌ |
| System Settings | ✓ | ❌ | ❌ | ❌ |
| API Key Management | ✓ | ❌ | ❌ | ❌ |
| Audit Logs | ✓ | ❌ | ❌ | ❌ |
| Dashboard Access | ✓ | ✓ | ✓ | ❌ |
| Export Functions | ✓ | ✓ | ✓ | ❌ |

### Custom Role Creation

**Creating Custom Roles:**
1. **Navigate** to Admin → Role Management
2. **Click** "Create Custom Role"
3. **Define** role properties:
   - Role name and description
   - Permission set
   - Access restrictions
4. **Test** role with test user account
5. **Deploy** to production

**Permission Categories:**
- Certificate Management
- User Administration
- System Configuration
- API Access
- Reporting and Analytics
- Data Export
- Audit and Compliance

---

## System Configuration

### Application Settings

**Access Configuration:**
- Navigate to Admin → System Settings
- Modify configuration values
- Apply changes (may require restart)

**Key Configuration Areas:**

**Authentication Settings:**
```json
{
  "Authentication": {
    "EnableAzureB2C": true,
    "CookieAuthFallback": true,
    "SessionTimeoutMinutes": 480,
    "RequireTwoFactor": false,
    "PasswordExpirationDays": 90
  }
}
```

**Feature Flags:**
```json
{
  "Features": {
    "EnableLazyLoading": false,
    "EnableAutocomplete": false,
    "EnableApiRateLimiting": true,
    "EnablePartnerFiltering": true,
    "EnableBulkOperations": true
  }
}
```

**Email Settings:**
```json
{
  "Email": {
    "SMTPServer": "smtp.server.com",
    "SMTPPort": 587,
    "EnableSSL": true,
    "FromAddress": "noreply@greencirclecertified.com",
    "FromDisplayName": "GCC Database System"
  }
}
```

### Database Configuration

**Connection Strings:**
- Development: SQLite database
- Production: SQL Server with connection pooling
- Backup: Azure SQL or on-premises backup

**Performance Settings:**
```json
{
  "Database": {
    "CommandTimeoutSeconds": 30,
    "ConnectionPoolSize": 100,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "EnableSensitiveDataLogging": false
  }
}
```

### Logging Configuration

**Log Levels:**
- Debug: Development only
- Information: Standard operations
- Warning: Non-critical issues
- Error: Application errors
- Critical: System failures

**Log Destinations:**
- File system: `/logs/` directory
- Database: AuditLogs table
- Azure Application Insights (if configured)
- Email alerts for critical errors

### Cache Configuration

**Memory Caching:**
```json
{
  "Caching": {
    "DefaultExpirationMinutes": 30,
    "MaxCacheSizeMB": 512,
    "EnableDistributedCaching": true,
    "RedisConnectionString": "localhost:6379"
  }
}
```

**Cache Keys Used:**
- `gcc:certificates:{id}` - Individual certificates
- `gcc:search:{hash}` - Search results
- `gcc:companies` - Company list
- `gcc:products` - Product catalog

---

## Database Administration

### Database Management Tasks

**Regular Maintenance:**
- Index optimization and rebuilding
- Statistics updates
- Transaction log management
- Backup verification
- Performance monitoring

**Database Schema:**

**Key Tables:**
- `Certificates` - Main certificate records
- `Companies` - Company information
- `Products` - Product catalog
- `Users` - User accounts
- `ApiKeys` - API key management
- `AuditLogs` - System audit trail
- `CertificateStatus` - Status tracking

### Database Migrations

**Running Migrations:**
```bash
# Check current migration status
dotnet ef migrations list

# Apply pending migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add [MigrationName]

# Rollback to specific migration
dotnet ef database update [MigrationName]
```

**Migration Best Practices:**
- Always backup database before migrations
- Test migrations in development first
- Review migration SQL before applying
- Have rollback plan ready
- Document schema changes

### Database Performance Monitoring

**Key Performance Indicators:**
- Query execution times
- Index usage statistics
- Lock contention
- Database size growth
- Connection pool utilization

**Performance Queries:**
```sql
-- Check slow queries
SELECT TOP 10
    total_elapsed_time / execution_count as avg_time,
    text,
    execution_count
FROM sys.dm_exec_query_stats
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
ORDER BY avg_time DESC;

-- Check index usage
SELECT
    OBJECT_NAME(s.object_id) as table_name,
    i.name as index_name,
    s.user_seeks,
    s.user_scans,
    s.user_lookups
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id
AND s.index_id = i.index_id;

-- Check database size
SELECT
    name,
    size * 8 / 1024 as size_mb,
    max_size * 8 / 1024 as max_size_mb
FROM sys.database_files;
```

### Database Security

**Access Control:**
- Application service account with minimal permissions
- Separate accounts for different environments
- No direct user access to production database
- Encrypted connections required

**Data Encryption:**
- Transparent Data Encryption (TDE) enabled
- Encrypted backups
- SSL/TLS for all connections
- Sensitive data masking in non-production

---

## API Management

### API Key Management

**Creating API Keys:**
1. **Navigate** to Admin → API Key Management
2. **Click** "Create New API Key"
3. **Fill out** API key details:
   - Key name and description
   - Associated user or organization
   - Expiration date
   - Rate limiting settings
   - IP restrictions (optional)
4. **Set** permissions and scope
5. **Generate** and securely share key

**API Key Permissions:**
- Read-only access to public certificates
- Full certificate management (for internal systems)
- User management (admin keys only)
- Export capabilities
- Analytics access

### Rate Limiting

**Default Rate Limits:**
- Public API: 100 requests/minute
- Authenticated API: 500 requests/minute
- Partner APIs: 1000 requests/hour
- Admin APIs: No limit (monitored)

**Configuring Rate Limits:**
```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "DefaultRateLimit": 100,
    "RateLimitPeriodMinutes": 1,
    "EnableIpRateLimiting": true,
    "EnableApiKeyRateLimiting": true
  }
}
```

### API Monitoring

**Metrics Tracked:**
- Request volume by endpoint
- Response times
- Error rates
- Rate limit violations
- API key usage patterns

**API Health Checks:**
- `/health` - Basic system health
- `/health/detailed` - Comprehensive health check
- `/api/health` - API-specific health status

### Partner API Configuration

**Amazon Integration:**
- Dedicated API endpoint: `/api/CertificateTileExternal`
- Automatic partner filtering
- Enhanced rate limits
- IP whitelisting capability
- Custom authentication headers

**API Documentation:**
- Swagger/OpenAPI available at `/swagger`
- Postman collections provided
- Code samples in multiple languages
- Integration testing guidelines

---

## Security Administration

### Security Policies

**Authentication Requirements:**
- Strong password policy enforced
- Two-factor authentication recommended
- Account lockout after failed attempts
- Session management controls

**Authorization Framework:**
- Role-based access control (RBAC)
- Principle of least privilege
- Regular access reviews
- Segregation of duties

### Security Monitoring

**Security Events Monitored:**
- Failed login attempts
- Privilege escalations
- Data access patterns
- API key usage
- Administrative actions

**Security Logs:**
```sql
-- Recent failed logins
SELECT TOP 50
    UserId,
    Email,
    Action,
    Details,
    IpAddress,
    Timestamp
FROM AuditLogs
WHERE Action = 'LoginFailed'
AND Timestamp > DATEADD(day, -7, GETDATE())
ORDER BY Timestamp DESC;

-- Administrative actions
SELECT TOP 100
    UserId,
    Action,
    Details,
    IpAddress,
    Timestamp
FROM AuditLogs
WHERE Action IN ('UserCreated', 'UserDeleted', 'RoleAssigned', 'SettingsChanged')
ORDER BY Timestamp DESC;
```

### Incident Response

**Security Incident Procedures:**
1. **Immediate Actions:**
   - Isolate affected systems
   - Preserve evidence
   - Notify security team
   - Begin incident log

2. **Investigation:**
   - Analyze audit logs
   - Determine scope of incident
   - Identify attack vectors
   - Document findings

3. **Containment:**
   - Apply security patches
   - Reset compromised credentials
   - Update security configurations
   - Monitor for continued threats

4. **Recovery:**
   - Restore from clean backups
   - Verify system integrity
   - Implement additional controls
   - Update procedures

### Compliance Requirements

**Data Protection:**
- Personal data encryption
- Access logging and monitoring
- Data retention policies
- Right to be forgotten compliance

**Audit Requirements:**
- Complete audit trail
- Immutable log storage
- Regular compliance reviews
- Third-party security assessments

---

## Monitoring and Performance

### System Monitoring

**Key Metrics to Monitor:**
- Application response times
- Server resource utilization (CPU, memory, disk)
- Database performance
- Network connectivity
- Error rates and exceptions

**Monitoring Tools Integration:**
- Application Insights (Azure)
- Custom performance counters
- Health check endpoints
- Log aggregation systems

### Performance Optimization

**Application Performance:**
- Connection pooling optimization
- Caching strategies
- Lazy loading implementation
- Query optimization

**Database Performance:**
- Index maintenance
- Query plan optimization
- Statistics updates
- Partitioning strategies

### Alerting Configuration

**Critical Alerts:**
- Application downtime
- Database connectivity failures
- High error rates
- Security incidents
- Resource exhaustion

**Alert Thresholds:**
```json
{
  "Alerts": {
    "ResponseTimeWarning": 1000,
    "ResponseTimeCritical": 3000,
    "ErrorRateWarning": 1,
    "ErrorRateCritical": 5,
    "DiskUsageWarning": 80,
    "DiskUsageCritical": 90
  }
}
```

### Performance Reports

**Weekly Performance Report:**
- Average response times
- Peak usage periods
- Error summary
- Resource utilization trends
- Capacity planning recommendations

**Monthly Capacity Planning:**
- Growth trends analysis
- Infrastructure scaling recommendations
- Cost optimization opportunities
- Performance benchmark comparisons

---

## Backup and Recovery

### Backup Procedures

**Database Backups:**
- Full backup: Daily at 2 AM
- Differential backup: Every 6 hours
- Transaction log backup: Every 15 minutes
- Retention: 30 days online, 1 year archived

**Application Backups:**
- Configuration files
- Custom certificates
- Log files
- Uploaded documents

**Backup Commands:**
```sql
-- Full database backup
BACKUP DATABASE GCCDatabase
TO DISK = 'C:\Backups\GCCDatabase_Full.bak'
WITH COMPRESSION, CHECKSUM;

-- Verify backup
RESTORE VERIFYONLY
FROM DISK = 'C:\Backups\GCCDatabase_Full.bak';
```

### Recovery Procedures

**Database Recovery:**
1. **Assess** damage and recovery requirements
2. **Stop** application services
3. **Restore** database from backup
4. **Apply** transaction log backups (if needed)
5. **Verify** data integrity
6. **Restart** application services
7. **Monitor** system performance

**Point-in-Time Recovery:**
```sql
-- Restore to specific point in time
RESTORE DATABASE GCCDatabase
FROM DISK = 'C:\Backups\GCCDatabase_Full.bak'
WITH REPLACE, NORECOVERY;

RESTORE LOG GCCDatabase
FROM DISK = 'C:\Backups\GCCDatabase_Log.trn'
WITH STOPAT = '2025-09-17 14:30:00';
```

### Disaster Recovery

**Recovery Objectives:**
- Recovery Time Objective (RTO): 4 hours
- Recovery Point Objective (RPO): 15 minutes
- Maximum tolerable downtime: 8 hours

**DR Site Requirements:**
- Identical infrastructure configuration
- Current backup copies
- Network connectivity
- Updated recovery procedures

**DR Testing:**
- Quarterly recovery tests
- Annual full DR exercise
- Documentation updates
- Staff training

---

## Deployment and Updates

### Application Deployment

**Deployment Environments:**
- Development: Local development and testing
- Staging: Pre-production testing
- Production: Live system

**Deployment Process:**
1. **Code Review:** All changes peer-reviewed
2. **Testing:** Comprehensive testing in staging
3. **Approval:** Change control approval
4. **Deployment:** Automated deployment pipeline
5. **Verification:** Post-deployment testing
6. **Monitoring:** Enhanced monitoring during/after deployment

### Database Updates

**Migration Deployment:**
```bash
# Backup production database
sqlcmd -Q "BACKUP DATABASE GCCDatabase TO DISK = 'C:\Backups\PreMigration.bak'"

# Apply migrations
dotnet ef database update

# Verify migration success
dotnet ef migrations list
```

**Rollback Procedures:**
```bash
# Rollback to previous migration
dotnet ef database update [PreviousMigrationName]

# Restore from backup if needed
sqlcmd -Q "RESTORE DATABASE GCCDatabase FROM DISK = 'C:\Backups\PreMigration.bak' WITH REPLACE"
```

### Version Management

**Version Numbering:**
- Major.Minor.Patch format
- Major: Breaking changes
- Minor: New features
- Patch: Bug fixes

**Release Documentation:**
- Change log maintenance
- Breaking change notifications
- Migration guides
- Configuration updates required

### Automated Deployment

**CI/CD Pipeline:**
```yaml
# Azure DevOps Pipeline Example
trigger:
  branches:
    include:
    - master

variables:
  buildConfiguration: 'Release'

stages:
- stage: Build
  jobs:
  - job: BuildApp
    steps:
    - task: DotNetCoreCLI@2
      inputs:
        command: 'build'
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'

- stage: Deploy
  jobs:
  - deployment: DeployToProduction
    environment: production
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: 'production-subscription'
              appName: 'gcc-webapp'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

---

## Troubleshooting and Support

### Common Issues

**Application Won't Start:**
1. Check configuration files
2. Verify database connectivity
3. Review error logs
4. Check port availability
5. Validate SSL certificates

**Performance Issues:**
1. Monitor resource usage
2. Check database performance
3. Review recent changes
4. Analyze user activity patterns
5. Check for memory leaks

**Database Issues:**
1. Check connection strings
2. Verify SQL Server service status
3. Review error logs
4. Check disk space
5. Monitor active connections

### Diagnostic Tools

**System Diagnostics:**
```bash
# Check application health
curl http://localhost:5101/health

# Monitor resource usage
Get-Process -Name "dotnet" | Select-Object CPU,WorkingSet,VirtualMemorySize

# Check database connectivity
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

**Log Analysis:**
```bash
# Application errors
grep -i "error" /var/log/gcc-webapp/application.log | tail -50

# Failed logins
grep -i "login failed" /var/log/gcc-webapp/security.log | tail -20

# Performance issues
grep -i "timeout\|slow" /var/log/gcc-webapp/application.log
```

### Support Escalation

**Level 1 Support:**
- Basic troubleshooting
- Password resets
- Account lockouts
- General user questions

**Level 2 Support:**
- Application configuration issues
- Performance problems
- Data integrity concerns
- Integration problems

**Level 3 Support:**
- Database administration
- Security incidents
- System architecture changes
- Emergency response

**Emergency Procedures:**
- 24/7 on-call rotation
- Emergency contact tree
- Escalation timelines
- Communication protocols

---

## Audit and Compliance

### Audit Trail Management

**Audit Log Categories:**
- User authentication events
- Data access and modifications
- Administrative actions
- API usage
- System configuration changes

**Audit Log Retention:**
- Real-time logs: 90 days in database
- Archived logs: 7 years in secure storage
- Critical events: Permanent retention
- Regular log exports for compliance

### Compliance Reporting

**Required Reports:**
- User access reports
- Data modification reports
- Security incident reports
- System availability reports
- Change management reports

**Automated Reporting:**
```sql
-- Monthly user activity report
SELECT
    u.Email,
    COUNT(*) as LoginCount,
    MAX(al.Timestamp) as LastLogin,
    STRING_AGG(DISTINCT al.Action, ', ') as Actions
FROM Users u
LEFT JOIN AuditLogs al ON u.Id = al.UserId
WHERE al.Timestamp >= DATEADD(month, -1, GETDATE())
GROUP BY u.Email
ORDER BY LoginCount DESC;

-- Certificate modification audit
SELECT
    c.CertificateNumber,
    u.Email as ModifiedBy,
    al.Action,
    al.Details,
    al.Timestamp
FROM AuditLogs al
INNER JOIN Users u ON al.UserId = u.Id
INNER JOIN Certificates c ON al.EntityId = c.Id
WHERE al.EntityType = 'Certificate'
AND al.Timestamp >= DATEADD(month, -1, GETDATE())
ORDER BY al.Timestamp DESC;
```

### Compliance Checklist

**Monthly Compliance Review:**
- [ ] Review user access rights
- [ ] Verify backup integrity
- [ ] Check security log for anomalies
- [ ] Update risk assessments
- [ ] Review and test incident response procedures
- [ ] Validate change management documentation
- [ ] Confirm compliance training completion

**Quarterly Compliance Review:**
- [ ] Conduct security assessment
- [ ] Review and update policies
- [ ] Test disaster recovery procedures
- [ ] Audit user accounts and roles
- [ ] Review vendor security assessments
- [ ] Update compliance documentation

**Annual Compliance Review:**
- [ ] Full security audit
- [ ] Penetration testing
- [ ] Business continuity testing
- [ ] Compliance gap analysis
- [ ] Policy and procedure updates
- [ ] Staff security training
- [ ] Third-party risk assessment

---

## Emergency Contacts

**Primary On-Call:** [Phone] [Email]
**Secondary On-Call:** [Phone] [Email]
**Database Administrator:** [Phone] [Email]
**Security Lead:** [Phone] [Email]
**CTO/Technical Lead:** [Phone] [Email]

**External Contacts:**
- Hosting Provider Support: [Phone] [Email]
- Database Vendor Support: [Phone] [Email]
- Security Incident Response: [Phone] [Email]

---

## Document Information

**Administrator Guide Version:** 1.0
**Last Updated:** September 17, 2025
**Next Review Date:** December 17, 2025
**Document Owner:** GCC Technical Team
**Approval:** [Approval signature and date]

---

## Document History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-09-17 | Initial administrator guide creation | GCC Development Team |

---

*This document contains sensitive system information. Distribution is restricted to authorized administrators only.*
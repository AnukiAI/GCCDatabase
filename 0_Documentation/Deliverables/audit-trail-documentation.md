# Audit Trail Documentation - GCC Web Application

## Document Control
- **Version:** 1.0
- **Date:** September 17, 2025
- **Classification:** Confidential
- **Owner:** GreenCircle Certified

## Executive Summary
This document describes the audit trail implementation in the GCC Web Application, including logging mechanisms, data retention, analysis procedures, and compliance requirements.

## Audit Trail Architecture

### System Components
```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
├─────────────────────────────────────────────────────────────┤
│  Controllers  │  Services  │  Repositories  │  Middleware   │
├─────────────────────────────────────────────────────────────┤
│                    Audit Logging Layer                       │
├──────────────┬──────────────┬───────────────┬──────────────┤
│  User Actions│ System Events│ Security Events│ Data Changes │
├──────────────┴──────────────┴───────────────┴──────────────┤
│                    Database Storage                          │
├─────────────────────────────────────────────────────────────┤
│              AuditLogs Table │ Serilog Logs                 │
└─────────────────────────────────────────────────────────────┘
```

## Audit Events Captured

### 1. Authentication Events
| Event Type | Description | Data Captured | Retention |
|------------|-------------|---------------|-----------|
| User Login | Successful authentication | Username, IP, Timestamp, Method | 1 year |
| Failed Login | Authentication failure | Username, IP, Timestamp, Reason | 1 year |
| User Logout | Session termination | Username, Session Duration | 1 year |
| Password Change | Credential update | Username, Timestamp | 1 year |
| Account Lockout | Security lockout | Username, IP, Reason | 2 years |
| 2FA Events | Two-factor authentication | Username, Method, Result | 1 year |
| Session Timeout | Automatic logout | Username, Last Activity | 6 months |

### 2. Authorization Events
| Event Type | Description | Data Captured | Retention |
|------------|-------------|---------------|-----------|
| Access Granted | Successful authorization | User, Resource, Action | 1 year |
| Access Denied | Authorization failure | User, Resource, Reason | 1 year |
| Role Assignment | User role changes | User, Old Role, New Role, Admin | 3 years |
| Permission Change | Permission modifications | User, Permission, Action | 3 years |
| Privilege Escalation | Elevated access | User, Original Role, Escalated Role | 3 years |

### 3. Data Modification Events
| Event Type | Description | Data Captured | Retention |
|------------|-------------|---------------|-----------|
| Certificate Created | New certificate | User, Certificate ID, Data | 7 years |
| Certificate Updated | Certificate modification | User, Certificate ID, Changes | 7 years |
| Certificate Deleted | Certificate removal | User, Certificate ID, Reason | 7 years |
| Status Change | Workflow transitions | User, Certificate ID, Old Status, New Status | 7 years |
| Bulk Operations | Mass updates | User, Operation, Affected Records | 3 years |
| Data Export | Data extraction | User, Export Type, Records Count | 2 years |
| Data Import | Data ingestion | User, Import Type, Records Count | 2 years |

### 4. Administrative Events
| Event Type | Description | Data Captured | Retention |
|------------|-------------|---------------|-----------|
| User Creation | New user account | Admin, New User, Roles | 3 years |
| User Modification | User account changes | Admin, User, Changes | 3 years |
| User Deletion | Account removal | Admin, User, Reason | 5 years |
| System Configuration | Settings changes | Admin, Setting, Old Value, New Value | 3 years |
| API Key Generation | API access creation | Admin, Key Name, Permissions | 3 years |
| API Key Revocation | API access removal | Admin, Key Name, Reason | 3 years |

### 5. Security Events
| Event Type | Description | Data Captured | Retention |
|------------|-------------|---------------|-----------|
| Suspicious Activity | Anomaly detection | User, Activity, Risk Score | 3 years |
| Rate Limit Exceeded | Throttling triggered | IP, Endpoint, Request Count | 1 year |
| Invalid Input | Validation failure | User, Input Type, Value | 6 months |
| SQL Injection Attempt | Security threat | IP, Query, Timestamp | 5 years |
| XSS Attempt | Script injection | IP, Payload, Target | 5 years |
| CSRF Token Failure | Token mismatch | User, Token, Endpoint | 1 year |
| File Upload | Document submission | User, File Name, Size, Type | 2 years |

### 6. System Events
| Event Type | Description | Data Captured | Retention |
|------------|-------------|---------------|-----------|
| Application Start | System initialization | Version, Environment, Config | 1 year |
| Application Stop | System shutdown | Reason, Duration | 1 year |
| Database Connection | DB connectivity | Connection String (masked), Result | 6 months |
| External API Call | Third-party integration | Service, Endpoint, Result | 1 year |
| Error Occurrence | System errors | Error Type, Stack Trace, Context | 1 year |
| Performance Warning | Slow operations | Operation, Duration, Threshold | 6 months |

## Database Schema

### AuditLogs Table Structure
```sql
CREATE TABLE [dbo].[AuditLogs] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NULL,
    [UserName] NVARCHAR(256) NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NULL,
    [EntityId] NVARCHAR(100) NULL,
    [OldValues] NVARCHAR(MAX) NULL,
    [NewValues] NVARCHAR(MAX) NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IpAddress] NVARCHAR(45) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [Result] NVARCHAR(50) NULL,
    [Duration] INT NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [AdditionalData] NVARCHAR(MAX) NULL,
    INDEX IX_AuditLogs_UserId (UserId),
    INDEX IX_AuditLogs_Timestamp (Timestamp),
    INDEX IX_AuditLogs_Action (Action),
    INDEX IX_AuditLogs_EntityType_EntityId (EntityType, EntityId)
);
```

### Log Entry Format
```json
{
  "id": 12345,
  "userId": "user-guid",
  "userName": "john.doe@example.com",
  "action": "CertificateUpdated",
  "entityType": "Certificate",
  "entityId": "CERT-2025-001",
  "oldValues": {
    "status": "Draft",
    "modifiedDate": "2025-09-16T10:00:00Z"
  },
  "newValues": {
    "status": "Submitted",
    "modifiedDate": "2025-09-17T14:30:00Z"
  },
  "timestamp": "2025-09-17T14:30:00Z",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "result": "Success",
  "duration": 250,
  "additionalData": {
    "sessionId": "session-123",
    "requestId": "req-456"
  }
}
```

## Implementation Details

### Audit Service Implementation
```csharp
public interface IAuditService
{
    Task LogAsync(AuditLogEntry entry);
    Task LogUserActionAsync(string action, object data = null);
    Task LogDataChangeAsync(string entity, string id, object oldValue, object newValue);
    Task LogSecurityEventAsync(string eventType, string details, string severity);
    Task<IEnumerable<AuditLogEntry>> GetAuditLogsAsync(AuditLogQuery query);
}
```

### Middleware Integration
```csharp
public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Capture request details
        var requestInfo = CaptureRequestInfo(context);

        // Process request
        await _next(context);

        // Log based on response
        await LogRequestAsync(requestInfo, context.Response);
    }
}
```

### Database Trigger Example
```sql
CREATE TRIGGER tr_Certificate_Audit
ON Certificates
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @Action VARCHAR(10)
    SET @Action =
        CASE
            WHEN EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted) THEN 'UPDATE'
            WHEN EXISTS(SELECT * FROM inserted) THEN 'INSERT'
            ELSE 'DELETE'
        END

    INSERT INTO AuditLogs (Action, EntityType, EntityId, OldValues, NewValues, Timestamp)
    SELECT
        @Action,
        'Certificate',
        COALESCE(i.Id, d.Id),
        (SELECT * FROM deleted FOR JSON AUTO),
        (SELECT * FROM inserted FOR JSON AUTO),
        GETUTCDATE()
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.Id = d.Id
END
```

## Audit Log Analysis

### Regular Analysis Tasks

#### Daily Reviews
- Failed login attempts > 5 per user
- Unauthorized access attempts
- Error rates > threshold
- Suspicious activity patterns

#### Weekly Reviews
- User activity summaries
- Data modification trends
- API usage patterns
- Performance metrics

#### Monthly Reviews
- Compliance audit trail completeness
- Security incident analysis
- User behavior analytics
- System health metrics

### Analysis Queries

#### Security Analysis
```sql
-- Detect brute force attempts
SELECT
    IpAddress,
    COUNT(*) as FailedAttempts,
    MIN(Timestamp) as FirstAttempt,
    MAX(Timestamp) as LastAttempt
FROM AuditLogs
WHERE Action = 'LoginFailed'
    AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY IpAddress
HAVING COUNT(*) > 5
ORDER BY FailedAttempts DESC;

-- Identify privilege escalation
SELECT
    UserId,
    UserName,
    Action,
    OldValues,
    NewValues,
    Timestamp
FROM AuditLogs
WHERE Action IN ('RoleAssignment', 'PermissionGrant')
    AND JSON_VALUE(NewValues, '$.role') IN ('Admin', 'Analyst')
ORDER BY Timestamp DESC;
```

#### Compliance Reporting
```sql
-- Certificate lifecycle audit trail
SELECT
    EntityId as CertificateId,
    Action,
    UserName,
    JSON_VALUE(OldValues, '$.status') as OldStatus,
    JSON_VALUE(NewValues, '$.status') as NewStatus,
    Timestamp
FROM AuditLogs
WHERE EntityType = 'Certificate'
    AND EntityId = @CertificateId
ORDER BY Timestamp;

-- Data retention compliance check
SELECT
    YEAR(Timestamp) as Year,
    COUNT(*) as LogCount,
    MIN(Timestamp) as OldestLog,
    MAX(Timestamp) as NewestLog
FROM AuditLogs
GROUP BY YEAR(Timestamp)
ORDER BY Year;
```

## Retention and Archival

### Retention Policy
| Log Type | Active Storage | Archive Storage | Total Retention |
|----------|---------------|-----------------|-----------------|
| Security Events | 1 year | 4 years | 5 years |
| Certificate Audit | 2 years | 5 years | 7 years |
| User Activity | 6 months | 6 months | 1 year |
| System Events | 3 months | 9 months | 1 year |
| Performance Logs | 1 month | 5 months | 6 months |

### Archival Process
1. **Monthly Archive Job**
   - Move logs older than active period to archive tables
   - Compress archived data
   - Update indices

2. **Archive Storage**
   ```sql
   CREATE TABLE AuditLogs_Archive (
       -- Same structure as AuditLogs
       -- Partitioned by year
   ) ON YearlyPartitionScheme(Timestamp);
   ```

3. **Purge Process**
   - Delete logs exceeding total retention
   - Generate compliance report before deletion
   - Maintain deletion audit trail

## Access Controls

### Who Can Access Audit Logs

| Role | Access Level | Permissions |
|------|--------------|-------------|
| System Admin | Full Access | View, Export, Analyze, Configure |
| Compliance Officer | Read Access | View, Export, Analyze |
| Security Analyst | Security Logs | View security events, Analyze |
| Auditor | Read-Only | View, Export for audit period |
| Developer | Limited | View own actions only |
| Regular User | None | No direct access |

### Access Implementation
```csharp
[Authorize(Roles = "Admin,ComplianceOfficer,Auditor")]
public class AuditController : Controller
{
    [HttpGet]
    public async Task<IActionResult> ViewLogs(AuditLogQuery query)
    {
        // Apply role-based filtering
        if (!User.IsInRole("Admin"))
        {
            query.ApplySecurityFilter(User);
        }

        var logs = await _auditService.GetAuditLogsAsync(query);
        return View(logs);
    }
}
```

## Reporting and Alerts

### Automated Reports
| Report Type | Frequency | Recipients | Content |
|------------|-----------|------------|---------|
| Security Summary | Daily | Security Team | Failed logins, threats detected |
| Compliance Report | Weekly | Compliance Officer | Audit trail completeness |
| User Activity | Monthly | Management | User statistics, trends |
| System Health | Weekly | IT Team | Errors, performance metrics |
| Certificate Audit | Monthly | Compliance | Certificate lifecycle compliance |

### Alert Conditions
| Alert Type | Trigger | Notification Method | Recipients |
|------------|---------|-------------------|------------|
| Multiple Failed Logins | 5+ failures in 10 min | Email, SMS | Security Team |
| Privilege Escalation | Admin role assigned | Email | Security, Compliance |
| Data Breach Attempt | SQL injection detected | Immediate Email, SMS | Security, CTO |
| Audit Log Tampering | Direct DB modification | Immediate Alert | Security, Admin |
| High Error Rate | 50+ errors/hour | Email | Development Team |
| Certificate Deletion | Any deletion | Email | Compliance Officer |

## Compliance Requirements

### Regulatory Compliance
- **GDPR Article 30**: Records of processing activities
- **GDPR Article 33**: Breach notification within 72 hours
- **ISO 27001**: A.12.4 Logging and monitoring
- **SOC 2**: Monitoring of system controls
- **PCI DSS 10**: Track and monitor all access (N/A if no payment data)

### Audit Log Integrity
1. **Immutability**: Audit logs cannot be modified or deleted
2. **Cryptographic Signing**: Digital signatures for log entries
3. **Timestamp Integrity**: NTP-synchronized timestamps
4. **Chain of Custody**: Continuous log sequence numbering
5. **Segregation of Duties**: Separate audit log database access

## Monitoring Dashboard

### Key Metrics
- Total events logged (last 24h, 7d, 30d)
- Failed authentication attempts
- Data modifications by user
- API usage by endpoint
- Error rate trends
- Response time percentiles

### Visualization Requirements
- Real-time event stream
- Geographic access map
- User activity heat map
- Trend analysis charts
- Anomaly detection alerts

## Incident Response Procedures

### When Suspicious Activity Detected
1. **Immediate Actions**
   - Isolate affected account/system
   - Preserve audit logs
   - Notify security team

2. **Investigation**
   - Analyze audit trail
   - Correlate events
   - Identify impact scope

3. **Response**
   - Contain threat
   - Implement fixes
   - Update security rules

4. **Post-Incident**
   - Generate incident report
   - Update procedures
   - Compliance notification if required

## Testing and Validation

### Audit System Testing
- Verify all events are logged correctly
- Test log retention and archival
- Validate alert triggers
- Confirm access controls
- Test report generation
- Verify log integrity

### Compliance Validation
- Annual audit trail review
- Quarterly retention verification
- Monthly completeness checks
- Weekly security event analysis
- Daily monitoring validation

## Appendices

### A. Event Type Codes
```
AUTH-001: User Login Success
AUTH-002: User Login Failure
AUTH-003: User Logout
DATA-001: Create Operation
DATA-002: Read Operation
DATA-003: Update Operation
DATA-004: Delete Operation
SEC-001: Security Threat Detected
SEC-002: Access Violation
SYS-001: System Start
SYS-002: System Stop
SYS-003: System Error
```

### B. Severity Levels
- **Critical**: Immediate action required
- **High**: Response within 1 hour
- **Medium**: Response within 24 hours
- **Low**: Regular review cycle
- **Info**: Informational only

### C. Related Documents
- Security Architecture Document
- Incident Response Plan
- Data Retention Policy
- Compliance Matrix

---
*This document is confidential and proprietary to GreenCircle Certified. Last updated: September 17, 2025*
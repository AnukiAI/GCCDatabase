# Disaster Recovery Plan
## GCC Database Solution

### Document Information
- **Version:** 1.0
- **Date:** September 17, 2025
- **Author:** Development Team
- **Classification:** Internal
- **Review Date:** December 17, 2025

---

## 1. Executive Summary

### 1.1 Purpose
This Disaster Recovery Plan (DRP) provides comprehensive procedures for the recovery of the GCC Database Solution in the event of system failures, data loss, security incidents, or natural disasters. The plan ensures business continuity and minimal service disruption.

### 1.2 Scope
This plan covers:
- Application infrastructure recovery
- Database recovery and backup restoration
- Authentication service recovery
- Third-party integration restoration
- Communication procedures and responsibilities

### 1.3 Recovery Objectives

| **Metric** | **Target** | **Maximum Acceptable** |
|------------|------------|------------------------|
| Recovery Time Objective (RTO) | 2 hours | 4 hours |
| Recovery Point Objective (RPO) | 15 minutes | 1 hour |
| Maximum Tolerable Downtime (MTD) | 4 hours | 8 hours |
| Service Level Recovery | 99% within 2 hours | 95% within 4 hours |

---

## 2. Risk Assessment & Business Impact Analysis

### 2.1 Critical Business Functions

#### High Priority (RTO: 2 hours)
- **Certificate Lookup Service**: Public certificate search and verification
- **Partner API Access**: Amazon/Wayfair integration endpoints
- **User Authentication**: Login and access management
- **Certificate Downloads**: PDF generation and delivery

#### Medium Priority (RTO: 4 hours)
- **Certificate Management**: CRUD operations for certificates
- **Admin Functions**: User management, role assignments
- **Analytics Dashboard**: Reporting and metrics
- **Audit Trail**: Security and compliance logging

#### Low Priority (RTO: 8 hours)
- **ETL Operations**: Data import/export processes
- **Development Environment**: Testing and staging systems
- **Documentation Access**: Internal knowledge base
- **Training Materials**: User guides and tutorials

### 2.2 Disaster Scenarios

#### Category 1: Infrastructure Failures
- **Application Server Failure**: Hardware, OS, or application crashes
- **Database Server Failure**: SQL Server corruption or unavailability
- **Network Outages**: Connectivity issues, DNS failures
- **Storage Failures**: Disk corruption, file system errors

#### Category 2: Security Incidents
- **Cyber Attacks**: DDoS, malware, ransomware
- **Data Breaches**: Unauthorized access, data exfiltration
- **Account Compromises**: Admin or service account breaches
- **API Abuse**: Rate limiting failures, unauthorized access

#### Category 3: Environmental Disasters
- **Natural Disasters**: Fire, flood, earthquake, severe weather
- **Power Outages**: Extended electrical failures
- **Facility Damage**: Building damage or inaccessibility
- **Provider Outages**: Cloud service provider failures

#### Category 4: Human Errors
- **Accidental Deletions**: Database, file, or configuration deletions
- **Configuration Errors**: Misconfigurations causing service failures
- **Deployment Failures**: Failed releases causing system instability
- **Security Misconfigurations**: Unauthorized access due to config errors

---

## 3. Recovery Team Structure

### 3.1 Disaster Recovery Team Roles

#### Primary Response Team
```
┌─────────────────────────────────────────────────────────────┐
│                    INCIDENT COMMANDER                       │
│                    (Project Manager)                        │
│    • Overall coordination and decision making               │
│    • Stakeholder communication                              │
│    • Resource allocation                                    │
└─────────────────────────────────────────────────────────────┘
                                    │
                ┌───────────────────┼───────────────────┐
                │                   │                   │
┌───────────────────────┐ ┌───────────────────────┐ ┌───────────────────────┐
│   TECHNICAL LEAD      │ │   DATABASE ADMIN      │ │  SECURITY OFFICER     │
│   (Lead Developer)    │ │   (Data Specialist)   │ │  (Security Lead)      │
│                       │ │                       │ │                       │
│ • System recovery     │ │ • Database restoration│ │ • Security assessment │
│ • Application fixes   │ │ • Data integrity      │ │ • Access control      │
│ • Code deployment     │ │ • Backup management   │ │ • Incident analysis   │
└───────────────────────┘ └───────────────────────┘ └───────────────────────┘
                │                   │                   │
┌───────────────────────┐ ┌───────────────────────┐ ┌───────────────────────┐
│  OPERATIONS TEAM      │ │  COMMUNICATIONS       │ │   BUSINESS LIAISON    │
│  (DevOps/Support)     │ │  (Marketing/PR)       │ │  (Business Analyst)   │
│                       │ │                       │ │                       │
│ • Infrastructure      │ │ • User communication  │ │ • Business impact     │
│ • Monitoring          │ │ • Partner notification│ │ • Priority decisions  │
│ • Network recovery    │ │ • Status updates      │ │ • Requirement changes │
└───────────────────────┘ └───────────────────────┘ └───────────────────────┘
```

#### Secondary Support Team
- **Azure/Cloud Support**: External vendor support contacts
- **Legal Counsel**: For security incidents and compliance
- **External Consultants**: Specialized recovery expertise
- **Management Team**: Executive decision making for major incidents

### 3.2 Contact Information

#### Primary Contacts
```
Role                 | Name              | Phone           | Email                    | Backup
--------------------|-------------------|-----------------|--------------------------|------------------
Incident Commander  | [Project Manager] | +1-555-0001    | pm@gcc.com              | [Deputy PM]
Technical Lead      | [Lead Developer]  | +1-555-0002    | tech@gcc.com            | [Sr Developer]
Database Admin      | [DBA]            | +1-555-0003    | dba@gcc.com             | [Jr DBA]
Security Officer    | [Security Lead]   | +1-555-0004    | security@gcc.com        | [Security Analyst]
```

#### Escalation Contacts
```
Level | Title              | Contact          | Conditions
------|-------------------|------------------|---------------------------
1     | Team Lead         | +1-555-0010     | Initial incident response
2     | Department Head   | +1-555-0020     | RTO > 2 hours
3     | VP Technology     | +1-555-0030     | RTO > 4 hours or security
4     | CEO/President     | +1-555-0040     | Business-critical impact
```

---

## 4. Backup Strategy & Data Protection

### 4.1 Database Backup Architecture

#### Production Database Backups
```sql
-- Full Database Backup (Daily at 2:00 AM UTC)
BACKUP DATABASE [GCCDatabase]
TO DISK = 'D:\Backups\GCCDatabase_Full_YYYYMMDD.bak'
WITH COMPRESSION, CHECKSUM, INIT
GO

-- Differential Backup (Every 6 hours)
BACKUP DATABASE [GCCDatabase]
TO DISK = 'D:\Backups\GCCDatabase_Diff_YYYYMMDDHH.bak'
WITH DIFFERENTIAL, COMPRESSION, CHECKSUM, INIT
GO

-- Transaction Log Backup (Every 15 minutes)
BACKUP LOG [GCCDatabase]
TO DISK = 'D:\Backups\GCCDatabase_Log_YYYYMMDDHHMM.trn'
WITH COMPRESSION, CHECKSUM, INIT
GO
```

#### Backup Locations & Retention
```
Backup Type     | Location              | Retention | Frequency    | Encryption
----------------|----------------------|-----------|--------------|------------
Full Backup     | Local: D:\Backups\   | 30 days   | Daily        | AES-256
Full Backup     | Azure Blob Storage   | 90 days   | Daily        | AES-256
Differential    | Local: D:\Backups\   | 7 days    | 6 hours      | AES-256
Differential    | Azure Blob Storage   | 30 days   | 6 hours      | AES-256
Transaction Log | Local: D:\Backups\   | 3 days    | 15 minutes   | AES-256
Transaction Log | Azure Blob Storage   | 7 days    | 15 minutes   | AES-256
```

### 4.2 Application Backup Components

#### Code Repository
- **Primary**: GitHub repository with full version history
- **Mirror**: Azure DevOps Git repository (hourly sync)
- **Local**: Development team local repositories

#### Configuration Files
- **Environment Variables**: Stored in Azure Key Vault with daily snapshots
- **Application Configs**: Version controlled in Git repository
- **SSL Certificates**: Backed up in Azure Key Vault with auto-renewal

#### Static Assets & Files
- **User Uploads**: Daily backup to Azure Blob Storage
- **Generated PDFs**: Archived to cold storage after 30 days
- **Application Logs**: Retained for 90 days in Azure Log Analytics

### 4.3 Backup Verification Procedures

#### Daily Verification (Automated)
```powershell
# Daily backup verification script
$BackupPath = "D:\Backups\"
$LatestBackup = Get-ChildItem $BackupPath -Filter "*_Full_*.bak" |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1

# Verify backup integrity
RESTORE VERIFYONLY FROM DISK = $LatestBackup.FullName

# Test restore to isolated environment
RESTORE DATABASE [GCCDatabase_Test] FROM DISK = $LatestBackup.FullName
WITH REPLACE, STATS = 10
```

#### Weekly Recovery Testing
- Restore full backup to test environment
- Verify application functionality
- Test authentication systems
- Validate data integrity
- Document any issues or gaps

---

## 5. Recovery Procedures

### 5.1 Application Server Recovery

#### Scenario A: Application Process Failure
```bash
# Step 1: Immediate response (5 minutes)
# Check application status
curl -f http://localhost:5101/health || echo "Application down"

# Step 2: Service restart attempt
sudo systemctl restart gcc-webapp
sudo systemctl status gcc-webapp

# Step 3: Container recovery (if using Docker)
docker ps | grep gcc-webapp
docker restart gcc-webapp || docker run -d --name gcc-webapp gcc-webapp:latest

# Step 4: Verify recovery
curl -f http://localhost:5101/health
curl -f http://localhost:5101/api/health/detailed
```

#### Scenario B: Server Hardware Failure
```bash
# Step 1: Activate standby server (15 minutes)
# Switch DNS to standby server
az network dns record-set a update \
  --resource-group gcc-rg \
  --zone-name greencirclecertified.com \
  --name www --set aRecords[0].ipv4Address=<STANDBY_IP>

# Step 2: Deploy application to standby
docker pull gccregistry.azurecr.io/gcc-webapp:latest
docker run -d --name gcc-webapp -p 80:8080 \
  -e ConnectionStrings__DefaultConnection="<STANDBY_DB_CONNECTION>" \
  gccregistry.azurecr.io/gcc-webapp:latest

# Step 3: Update load balancer configuration
az network lb rule update \
  --resource-group gcc-rg \
  --lb-name gcc-loadbalancer \
  --name http-rule \
  --backend-pool-name standby-pool
```

### 5.2 Database Recovery Procedures

#### Point-in-Time Recovery
```sql
-- Step 1: Restore full backup
RESTORE DATABASE [GCCDatabase]
FROM DISK = 'D:\Backups\GCCDatabase_Full_20250917.bak'
WITH NORECOVERY, REPLACE, STATS = 10
GO

-- Step 2: Restore differential backup
RESTORE DATABASE [GCCDatabase]
FROM DISK = 'D:\Backups\GCCDatabase_Diff_20250917_18.bak'
WITH NORECOVERY, STATS = 10
GO

-- Step 3: Restore transaction logs (up to failure point)
RESTORE LOG [GCCDatabase]
FROM DISK = 'D:\Backups\GCCDatabase_Log_20250917_1830.trn'
WITH RECOVERY, STOPAT = '2025-09-17 18:35:00', STATS = 10
GO

-- Step 4: Verify database integrity
DBCC CHECKDB('GCCDatabase') WITH NO_INFOMSGS
GO

-- Step 5: Update statistics and rebuild indexes
UPDATE STATISTICS [GCCDatabase].[dbo].[Certificates] WITH FULLSCAN
GO
```

#### Database Corruption Recovery
```sql
-- Step 1: Emergency repairs for minor corruption
ALTER DATABASE [GCCDatabase] SET EMERGENCY
GO
DBCC CHECKDB('GCCDatabase', REPAIR_ALLOW_DATA_LOSS) WITH NO_INFOMSGS
GO
ALTER DATABASE [GCCDatabase] SET ONLINE
GO

-- Step 2: If major corruption, restore from backup
-- Use Point-in-Time Recovery procedure above

-- Step 3: Data validation after recovery
SELECT COUNT(*) FROM Certificates
SELECT COUNT(*) FROM Users
SELECT COUNT(*) FROM AuditLogs

-- Step 4: Check application connectivity
-- Run application health checks
```

### 5.3 Security Incident Response

#### Suspected Data Breach
```bash
# Step 1: Immediate containment (5 minutes)
# Disable affected user accounts
az ad user update --id compromised-user@gcc.com --account-enabled false

# Revoke API keys
curl -X DELETE http://localhost:5101/api/apikeys/suspicious-key \
  -H "Authorization: Bearer <ADMIN_TOKEN>"

# Step 2: Network isolation (15 minutes)
# Block suspicious IP addresses
az network nsg rule create \
  --resource-group gcc-rg \
  --nsg-name gcc-security-group \
  --name block-suspicious-ip \
  --priority 100 \
  --source-address-prefixes <SUSPICIOUS_IP> \
  --access Deny

# Step 3: Evidence preservation
# Capture current logs
docker logs gcc-webapp > incident-logs-$(date +%Y%m%d-%H%M).txt
# Backup current database state
# Create forensic image of affected systems
```

#### Ransomware Attack
```bash
# Step 1: Immediate isolation (2 minutes)
# Disconnect from network
sudo ip link set eth0 down
# Stop all services
sudo systemctl stop gcc-webapp nginx

# Step 2: Assessment and communication (15 minutes)
# Notify incident commander
# Contact law enforcement if required
# Document attack vector and scope

# Step 3: Recovery from clean backups
# Verify backup integrity (not compromised)
# Restore from known-good backup point
# Rebuild systems from scratch if necessary
```

### 5.4 Communication Procedures

#### Internal Communication Matrix
```
Incident Severity | Notification Time | Recipients                    | Method
------------------|------------------|-------------------------------|------------------
Low              | 4 hours          | Technical Team               | Email, Slack
Medium           | 1 hour           | Technical Team + Management  | Email, Phone
High             | 15 minutes       | All Stakeholders             | Phone, SMS, Email
Critical         | 5 minutes        | All + Emergency Contacts     | Phone, SMS
```

#### External Communication Templates

**Partner Notification (Amazon/Wayfair)**
```
Subject: GCC API Service Status Update - [INCIDENT_ID]

Dear [PARTNER_NAME] Team,

We are currently experiencing [BRIEF_DESCRIPTION] affecting our API services.

Impact: [SPECIFIC_IMPACT_ON_PARTNER]
Expected Resolution: [ETA]
Alternative Access: [WORKAROUND_IF_AVAILABLE]

We will provide updates every [FREQUENCY] until resolution.

Contact: support@greencirclecertified.com
Phone: +1-555-GCC-HELP

Thank you for your patience.
GCC Technical Team
```

**Public Status Page Update**
```
[TIMESTAMP] - Service Degradation
We are investigating reports of slow response times on our certificate lookup service.
Impact: Certificate searches may take longer than usual
Status: Investigating
Updates: We will provide an update in 30 minutes

[TIMESTAMP] - Service Restored
The issue has been resolved. All services are operating normally.
Root Cause: [BRIEF_TECHNICAL_EXPLANATION]
Prevention: [STEPS_TAKEN_TO_PREVENT_RECURRENCE]
```

---

## 6. Recovery Testing & Validation

### 6.1 Disaster Recovery Testing Schedule

#### Quarterly Full DR Tests
```
Test Type           | Frequency | Duration | Scope                  | Success Criteria
--------------------|-----------|----------|------------------------|------------------
Full System Recovery| Quarterly | 8 hours  | Complete environment   | RTO < 4 hours
Database Recovery   | Monthly   | 4 hours  | Database only          | RPO < 1 hour
Application Recovery| Monthly   | 2 hours  | App servers only       | Service restored
Security Response  | Monthly   | 2 hours  | Simulated incidents    | Response < 15 min
Partner API Recovery| Quarterly | 4 hours  | External integrations  | API fully functional
```

#### Testing Procedures Documentation
```bash
# DR Test Execution Checklist
# Pre-test preparation
□ Schedule maintenance window
□ Notify stakeholders
□ Prepare test environment
□ Document baseline metrics

# Test execution
□ Simulate disaster scenario
□ Execute recovery procedures
□ Record time measurements
□ Validate system functionality
□ Test user authentication
□ Verify data integrity
□ Check partner integrations

# Post-test activities
□ Document lessons learned
□ Update procedures if needed
□ Communicate results to team
□ Schedule follow-up actions
```

### 6.2 Recovery Validation Checklist

#### System Functionality Validation
```bash
# Database connectivity
sqlcmd -S <SERVER> -d GCCDatabase -Q "SELECT COUNT(*) FROM Certificates"

# Application health
curl -f http://localhost:5101/health
curl -f http://localhost:5101/api/health/detailed

# Authentication systems
curl -X POST http://localhost:5101/api/auth/test \
  -H "Content-Type: application/json" \
  -d '{"username":"test@gcc.com","password":"test"}'

# Partner API endpoints
curl -H "X-API-Key: test-key" http://localhost:5101/api/CertificateTileExternal/tiles

# File upload/download
curl -f http://localhost:5101/Certificate/Download/1

# Search functionality
curl "http://localhost:5101/api/certificates/search?q=test"
```

#### Performance Validation
```bash
# Response time testing
ab -n 1000 -c 10 http://localhost:5101/
ab -n 100 -c 5 http://localhost:5101/api/certificates

# Database performance
sqlcmd -S <SERVER> -d GCCDatabase -Q "
SET STATISTICS TIME ON
SELECT * FROM Certificates WHERE Status = 'Active'
SET STATISTICS TIME OFF"

# Memory and CPU usage
docker stats gcc-webapp
```

#### Data Integrity Validation
```sql
-- Record counts
SELECT 'Certificates' as TableName, COUNT(*) as RecordCount FROM Certificates
UNION ALL
SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM Users
UNION ALL
SELECT 'AuditLogs' as TableName, COUNT(*) as RecordCount FROM AuditLogs

-- Data consistency checks
SELECT COUNT(*) as OrphanedRecords
FROM Certificates c
LEFT JOIN Companies co ON c.CompanyId = co.Id
WHERE co.Id IS NULL

-- Recent activity verification
SELECT TOP 10 * FROM AuditLogs ORDER BY Timestamp DESC
```

---

## 7. Business Continuity Procedures

### 7.1 Operational Workarounds

#### Manual Certificate Verification Process
When the automated system is unavailable:

1. **Phone/Email Requests**
   - Customers call +1-555-GCC-CERT
   - Verify certificate by company name and product
   - Email PDF certificate copy within 4 hours
   - Log all manual requests for later system update

2. **Partner Integrations (Amazon/Wayfair)**
   - Provide daily CSV export via secure FTP
   - Manual API responses for critical requests
   - Phone support for urgent verifications
   - Batch processing when system restored

3. **Certificate Creation**
   - Use offline certificate template
   - Manual data entry when system restored
   - Temporary certificate numbers (C-TEMP-xxxxx)
   - Priority processing for time-sensitive certificates

### 7.2 Resource Scaling During Recovery

#### Personnel Allocation
```
Recovery Phase | Duration | Team Size | Primary Activities
---------------|----------|-----------|-------------------
Immediate      | 0-2 hrs  | 5 people  | Assessment, containment
Active Recovery| 2-8 hrs  | 8 people  | System restoration
Validation     | 8-12 hrs | 6 people  | Testing, verification
Normal Ops     | 12+ hrs  | 3 people  | Monitoring, support
```

#### Resource Requirements
- **Technical**: 2 senior developers, 1 DBA, 1 systems admin
- **Communication**: 1 project manager, 1 customer support lead
- **Business**: 1 business analyst for priority decisions
- **External**: Cloud support, vendor assistance as needed

### 7.3 Supply Chain & Vendor Dependencies

#### Critical Vendor Contacts
```
Vendor              | Service           | Contact              | SLA
--------------------|-------------------|---------------------|----------
Microsoft Azure     | Cloud Platform    | +1-800-MSFT-HELP   | 4 hours
GitHub              | Code Repository   | support@github.com  | 24 hours
SSL Certificate     | Security Certs    | support@ssl.com     | 2 hours
Email Service       | SMTP Delivery     | support@email.com   | 1 hour
```

#### Vendor Escalation Procedures
1. **Standard Support**: Use normal support channels
2. **Severity 2**: Request manager escalation
3. **Severity 1**: Invoke emergency support contract
4. **Business Critical**: Engage vendor executive contacts

---

## 8. Post-Incident Procedures

### 8.1 Post-Incident Review Process

#### Immediate Post-Incident (within 2 hours of resolution)
```
Activity                     | Responsible      | Timeline | Deliverable
----------------------------|------------------|----------|------------------
Service restoration confirm | Technical Lead   | 30 min   | System status report
Stakeholder notification    | Incident Cmdr    | 1 hour   | Resolution notice
Initial timeline creation   | Technical Lead   | 2 hours  | Incident timeline
Customer communication     | Communications   | 2 hours  | Public status update
```

#### Formal Post-Incident Review (within 5 business days)
```
Phase              | Activities                           | Duration | Participants
-------------------|--------------------------------------|----------|------------------
Data Collection    | Gather logs, metrics, timelines     | 2 days   | Technical team
Analysis           | Root cause analysis, gap assessment | 2 days   | Full DR team
Documentation      | Write incident report, lessons learned| 1 day    | Incident commander
Presentation       | Review with stakeholders            | 1 hour   | Management team
```

### 8.2 Incident Report Template

#### Executive Summary
```markdown
## Incident Report: [INCIDENT_ID]

**Date/Time**: [START_TIME] - [END_TIME] UTC
**Duration**: [TOTAL_DURATION]
**Severity**: [LOW/MEDIUM/HIGH/CRITICAL]
**Services Affected**: [LIST_OF_SERVICES]
**Customer Impact**: [DESCRIPTION]
**Root Cause**: [BRIEF_DESCRIPTION]

### Timeline Summary
- [TIME]: Incident detected
- [TIME]: Response team assembled
- [TIME]: Root cause identified
- [TIME]: Fix implemented
- [TIME]: Service restored
- [TIME]: Post-incident review completed
```

#### Detailed Analysis
```markdown
### Root Cause Analysis
[DETAILED_TECHNICAL_EXPLANATION]

### Contributing Factors
1. [FACTOR_1]
2. [FACTOR_2]
3. [FACTOR_3]

### What Went Well
- [POSITIVE_ASPECT_1]
- [POSITIVE_ASPECT_2]

### What Needs Improvement
- [IMPROVEMENT_AREA_1]
- [IMPROVEMENT_AREA_2]

### Action Items
| Action | Owner | Due Date | Priority |
|--------|-------|----------|----------|
| [ACTION_1] | [OWNER] | [DATE] | High |
| [ACTION_2] | [OWNER] | [DATE] | Medium |
```

### 8.3 Continuous Improvement Process

#### Lessons Learned Database
Maintain centralized repository of:
- Incident reports and analyses
- Recovery procedure updates
- Training material improvements
- Tool and process enhancements

#### Regular Plan Updates
```
Update Type        | Frequency | Trigger                    | Approval Required
-------------------|-----------|----------------------------|------------------
Technical Updates  | Monthly   | System changes            | Technical Lead
Process Updates    | Quarterly | Lessons learned           | DR Team
Contact Updates    | Monthly   | Personnel changes         | HR + DR Team
Full Plan Review   | Annually  | Scheduled review          | Management
Emergency Updates  | As needed | Critical gaps identified  | Incident Commander
```

#### Training and Awareness Program
- **Monthly**: DR team training on new procedures
- **Quarterly**: Tabletop exercises with management
- **Semi-annually**: Full DR simulation with all staff
- **Annually**: Third-party DR assessment and recommendations

---

## 9. Plan Maintenance & Updates

### 9.1 Document Control

#### Version History
```
Version | Date       | Changes                           | Approved By
--------|------------|-----------------------------------|------------------
1.0     | 2025-09-17| Initial disaster recovery plan    | [Project Manager]
```

#### Review Schedule
```
Review Type        | Frequency | Next Due    | Responsible
-------------------|-----------|-------------|------------------
Technical Review   | Monthly   | 2025-10-17  | Technical Lead
Business Review    | Quarterly | 2025-12-17  | Business Analyst
Management Review  | Annually  | 2026-09-17  | Project Manager
Emergency Review   | As needed | N/A         | Incident Commander
```

### 9.2 Change Management Integration

#### Plan Update Procedures
1. **Identify Need**: Incident review, system changes, or scheduled review
2. **Draft Changes**: Technical team drafts proposed modifications
3. **Impact Assessment**: Evaluate changes using standard impact assessment template
4. **Approval Process**: Follow change management approval workflow
5. **Implementation**: Update plan and notify all stakeholders
6. **Validation**: Test updated procedures during next DR exercise

#### Change Documentation Requirements
- Change request ID and description
- Business justification for changes
- Technical impact assessment
- Testing and validation plan
- Communication and training plan
- Rollback procedures if needed

### 9.3 Stakeholder Communication

#### Plan Distribution
```
Stakeholder Group    | Distribution Method | Update Frequency | Access Level
--------------------|--------------------|-----------------|--------------
DR Team Members     | Email + Portal     | Immediate       | Full access
Management Team     | Email summary      | Quarterly       | Executive summary
All Staff           | Company portal     | Quarterly       | General awareness
External Partners   | Secure portal      | As needed       | Relevant sections
Vendors/Contractors | Direct delivery    | As needed       | Limited access
```

#### Training Requirements
```
Role                | Training Required              | Frequency | Format
--------------------|--------------------------------|-----------|------------------
DR Team            | Full plan + hands-on exercises | Monthly   | In-person
Technical Staff    | Technical procedures           | Quarterly | Online + practice
Management         | Plan overview + decision making| Annually  | Tabletop exercise
All Employees      | Basic awareness                | Annually  | Online training
New Hires          | Role-specific training         | On hire   | Orientation
```

---

## 10. Compliance & Audit Requirements

### 10.1 Regulatory Compliance

#### Data Protection (GDPR)
- **Data Recovery**: Ensure personal data recovery meets GDPR requirements
- **Breach Notification**: 72-hour notification requirement for data breaches
- **Data Minimization**: Recovery procedures only restore necessary data
- **Audit Trail**: Maintain complete audit trail of all recovery activities

#### SOC 2 Type II Requirements
- **Availability**: Demonstrate system availability commitments
- **Security**: Document security controls during recovery
- **Processing Integrity**: Ensure data integrity throughout recovery
- **Confidentiality**: Maintain data confidentiality during incidents

### 10.2 Audit Trail Requirements

#### Recovery Activity Logging
```sql
-- Audit log structure for DR activities
CREATE TABLE DRActivityLog (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IncidentId NVARCHAR(50) NOT NULL,
    ActivityType NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    PerformedBy NVARCHAR(100) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    SystemsAffected NVARCHAR(MAX),
    Result NVARCHAR(50) NOT NULL,
    Notes NVARCHAR(MAX)
);
```

#### Required Documentation
- Incident detection and notification records
- Recovery action timeline with responsible parties
- System verification and validation results
- Business impact assessment and measurements
- Communications sent to stakeholders
- Lessons learned and improvement actions

### 10.3 External Audit Support

#### Audit Preparation
- Maintain current DR plan and test results
- Document all DR exercises and outcomes
- Keep records of plan updates and approvals
- Provide evidence of team training and competency
- Demonstrate regular testing and validation

#### Audit Evidence Package
```
Document Type              | Location                    | Retention Period
--------------------------|----------------------------|------------------
DR Plan (current version) | Document management system | 7 years
DR Test Results           | Test results database      | 3 years
Incident Reports          | Incident management system | 5 years
Training Records          | HR training system         | 3 years
Communication Records     | Email archive system       | 2 years
Vendor Agreements         | Contract management system | Life of contract + 7 years
```

---

## 11. Appendices

### Appendix A: Emergency Contact List
```
CRITICAL HOTLINE: +1-555-DR-EMERGENCY (375-363-749)

24/7 Emergency Contacts:
┌─────────────────────────────────────────────────────────────────┐
│ INCIDENT COMMANDER (Primary)                                    │
│ Name: [Project Manager Name]                                    │
│ Mobile: +1-555-0001                                            │
│ Email: pm@greencirclecertified.com                             │
│ Home: +1-555-0011                                              │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ TECHNICAL LEAD (Primary)                                        │
│ Name: [Technical Lead Name]                                     │
│ Mobile: +1-555-0002                                            │
│ Email: tech@greencirclecertified.com                           │
│ Backup: [Senior Developer] +1-555-0012                         │
└─────────────────────────────────────────────────────────────────┘

Management Escalation:
- Department Head: +1-555-0020
- VP Technology: +1-555-0030
- CEO/President: +1-555-0040

External Emergency Services:
- Local Emergency: 911
- FBI Cyber Crime: +1-855-292-3937
- Azure Support: +1-800-642-7676
- Legal Counsel: +1-555-0050
```

### Appendix B: System Recovery Scripts

#### Database Recovery Script
```sql
-- GCC Database Recovery Script
-- Usage: Execute in order during database recovery

-- Step 1: Verify database state
SELECT
    name,
    state_desc,
    create_date,
    collation_name
FROM sys.databases
WHERE name = 'GCCDatabase';

-- Step 2: Check for active connections
SELECT
    session_id,
    login_name,
    host_name,
    program_name,
    last_request_end_time
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('GCCDatabase');

-- Step 3: Set database to single user mode (if needed)
-- ALTER DATABASE [GCCDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Step 4: Restore database (replace with actual backup file)
-- RESTORE DATABASE [GCCDatabase]
-- FROM DISK = 'C:\Backups\GCCDatabase_Latest.bak'
-- WITH REPLACE, STATS = 10;

-- Step 5: Verify table structures and data
SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
ORDER BY TABLE_SCHEMA, TABLE_NAME;

-- Step 6: Check critical table row counts
SELECT 'Certificates' as TableName, COUNT(*) as RowCount FROM Certificates
UNION ALL
SELECT 'Users' as TableName, COUNT(*) as RowCount FROM Users
UNION ALL
SELECT 'Companies' as TableName, COUNT(*) as RowCount FROM Companies
UNION ALL
SELECT 'AuditLogs' as TableName, COUNT(*) as RowCount FROM AuditLogs;

-- Step 7: Verify database integrity
DBCC CHECKDB('GCCDatabase') WITH NO_INFOMSGS;

-- Step 8: Update statistics
EXEC sp_updatestats;

-- Step 9: Set database back to multi-user mode
-- ALTER DATABASE [GCCDatabase] SET MULTI_USER;

-- Step 10: Test application connectivity
-- Run application health check endpoint
```

#### Application Recovery Script
```bash
#!/bin/bash
# GCC Application Recovery Script
# Usage: ./gcc-app-recovery.sh [environment]

set -e  # Exit on any error

ENVIRONMENT=${1:-production}
APP_NAME="gcc-webapp"
HEALTH_ENDPOINT="http://localhost:5101/health"
MAX_WAIT_TIME=300  # 5 minutes

echo "Starting GCC Application Recovery for $ENVIRONMENT environment..."
echo "Timestamp: $(date -u)"

# Step 1: Stop existing application processes
echo "Step 1: Stopping existing application processes..."
docker stop $APP_NAME 2>/dev/null || echo "No running container found"
docker rm $APP_NAME 2>/dev/null || echo "No container to remove"

# Kill any remaining processes
pkill -f "GCCWebApp.dll" 2>/dev/null || echo "No processes to kill"

# Step 2: Pull latest application image
echo "Step 2: Pulling latest application image..."
docker pull gccregistry.azurecr.io/gcc-webapp:$ENVIRONMENT

# Step 3: Start application container
echo "Step 3: Starting application container..."
if [ "$ENVIRONMENT" = "production" ]; then
    docker run -d \
        --name $APP_NAME \
        -p 5101:8080 \
        -e ASPNETCORE_ENVIRONMENT=Production \
        -e ConnectionStrings__DefaultConnection="$PROD_CONNECTION_STRING" \
        -e Jwt__Key="$JWT_SECRET_KEY" \
        --restart unless-stopped \
        gccregistry.azurecr.io/gcc-webapp:production
else
    docker run -d \
        --name $APP_NAME \
        -p 5101:8080 \
        -e ASPNETCORE_ENVIRONMENT=Development \
        -e ConnectionStrings__DefaultConnection="Data Source=gcc_database.db" \
        gccregistry.azurecr.io/gcc-webapp:development
fi

# Step 4: Wait for application to start
echo "Step 4: Waiting for application to start..."
WAIT_TIME=0
while [ $WAIT_TIME -lt $MAX_WAIT_TIME ]; do
    if curl -f $HEALTH_ENDPOINT >/dev/null 2>&1; then
        echo "Application is healthy!"
        break
    fi
    echo "Waiting for application... ($WAIT_TIME seconds)"
    sleep 10
    WAIT_TIME=$((WAIT_TIME + 10))
done

if [ $WAIT_TIME -ge $MAX_WAIT_TIME ]; then
    echo "ERROR: Application failed to start within $MAX_WAIT_TIME seconds"
    echo "Container logs:"
    docker logs $APP_NAME
    exit 1
fi

# Step 5: Validate application functionality
echo "Step 5: Validating application functionality..."

# Test endpoints
ENDPOINTS=(
    "/health"
    "/api/health/detailed"
    "/Certificate/CertificateTileView"
    "/api/CertificateTileExternal/tiles"
)

for endpoint in "${ENDPOINTS[@]}"; do
    echo "Testing endpoint: $endpoint"
    if curl -f "http://localhost:5101$endpoint" >/dev/null 2>&1; then
        echo "✓ $endpoint - OK"
    else
        echo "✗ $endpoint - FAILED"
        echo "WARNING: Endpoint validation failed"
    fi
done

# Step 6: Notify stakeholders
echo "Step 6: Application recovery completed successfully"
echo "Application URL: http://localhost:5101"
echo "Health Check: $HEALTH_ENDPOINT"
echo "Recovery completed at: $(date -u)"

# Optional: Send notification email
# echo "GCC Application recovered successfully at $(date -u)" | \
#   mail -s "GCC Application Recovery Complete" admin@greencirclecertified.com
```

#### Network Recovery Script
```bash
#!/bin/bash
# GCC Network Recovery Script
# Usage: ./gcc-network-recovery.sh

echo "Starting GCC Network Recovery..."

# Step 1: Check basic network connectivity
echo "Step 1: Checking network connectivity..."
ping -c 4 8.8.8.8 && echo "Internet connectivity: OK" || echo "Internet connectivity: FAILED"
ping -c 4 google.com && echo "DNS resolution: OK" || echo "DNS resolution: FAILED"

# Step 2: Check firewall rules
echo "Step 2: Checking firewall configuration..."
if command -v ufw >/dev/null 2>&1; then
    ufw status verbose
elif command -v firewall-cmd >/dev/null 2>&1; then
    firewall-cmd --list-all
else
    echo "No supported firewall found"
fi

# Step 3: Check and restart network services
echo "Step 3: Restarting network services..."
systemctl restart networking || systemctl restart network
systemctl restart systemd-resolved

# Step 4: Verify port accessibility
echo "Step 4: Checking port accessibility..."
PORTS=(80 443 5101 5432 1433)
for port in "${PORTS[@]}"; do
    if netstat -ln | grep ":$port " >/dev/null; then
        echo "✓ Port $port is listening"
    else
        echo "✗ Port $port is not accessible"
    fi
done

# Step 5: Test application endpoints
echo "Step 5: Testing application endpoints..."
curl -I http://localhost:5101/health 2>/dev/null | head -1

# Step 6: Check SSL certificate (if HTTPS)
echo "Step 6: Checking SSL certificate..."
if openssl s_client -connect localhost:443 -servername greencirclecertified.com </dev/null 2>/dev/null | openssl x509 -noout -dates 2>/dev/null; then
    echo "SSL certificate is valid"
else
    echo "SSL certificate check failed or HTTPS not configured"
fi

echo "Network recovery check completed"
```

### Appendix C: Vendor Contact Information

#### Cloud Services
```
Microsoft Azure Support
├── General Support: +1-800-642-7676
├── Emergency: +1-800-865-9408
├── Portal: https://portal.azure.com
├── Account Manager: [Azure Account Rep]
├── Technical Account Manager: [Azure TAM]
└── Contract Number: [Azure Contract ID]

GitHub Support
├── Enterprise Support: support@github.com
├── Emergency: Via GitHub Enterprise Portal
├── Account Manager: [GitHub Account Rep]
├── Technical Contact: [GitHub Technical Contact]
└── Contract Number: [GitHub Enterprise Agreement]
```

#### Security Services
```
SSL Certificate Provider
├── Support: +1-555-SSL-HELP
├── Emergency Renewal: support-emergency@ssl-provider.com
├── Account: [SSL Account Number]
└── Renewal Contact: [SSL Renewal Contact]

Security Monitoring
├── SOC: +1-555-SOC-2417
├── Emergency: soc-emergency@security-provider.com
├── Account Manager: [Security Account Rep]
└── Contract: [Security Monitoring Agreement]
```

#### Database Services
```
SQL Server Support (if hosted)
├── Support: +1-555-SQL-HELP
├── DBA Services: dba@sql-hosting-provider.com
├── Emergency: +1-555-SQL-911
└── Account: [SQL Hosting Account]

Backup Services
├── Support: +1-555-BACKUP-1
├── Recovery Services: recovery@backup-provider.com
├── Emergency Restore: +1-555-BACKUP-911
└── Account: [Backup Service Account]
```

### Appendix D: Testing Checklists

#### Quarterly DR Test Checklist
```
Pre-Test Preparation (1 week before)
□ Schedule test window with stakeholders
□ Notify all team members of test date/time
□ Prepare test environment
□ Backup current production state
□ Document baseline metrics
□ Prepare communication templates
□ Set up monitoring and logging

Test Execution Day
□ Start time recorded: ___________
□ Incident scenario initiated
□ DR team assembled and roles assigned
□ Communication procedures tested
□ Recovery procedures executed step-by-step
□ Database recovery validated
□ Application recovery validated
□ Network connectivity verified
□ Security controls tested
□ Partner integrations validated
□ End-user functionality verified
□ Performance benchmarks measured
□ End time recorded: ___________

Post-Test Activities (within 48 hours)
□ Test results documented
□ Recovery time objectives measured
□ Recovery point objectives validated
□ Issues and gaps identified
□ Lessons learned captured
□ Improvement recommendations documented
□ Plan updates scheduled
□ Stakeholder summary prepared
□ Next test date scheduled
□ Action items assigned with due dates
```

#### Security Incident Response Checklist
```
Immediate Response (0-15 minutes)
□ Incident detected and verified
□ Incident commander notified
□ Initial assessment completed
□ Containment measures initiated
□ Evidence preservation started
□ Initial stakeholder notification sent

Investigation Phase (15 minutes - 2 hours)
□ Security team assembled
□ Forensic analysis initiated
□ Scope and impact assessed
□ Attack vector identified
□ Affected systems identified
□ Data breach assessment completed
□ Law enforcement notification (if required)

Recovery Phase (2-8 hours)
□ Systems isolated and cleaned
□ Security patches applied
□ Access controls updated
□ Monitoring enhanced
□ Systems restored from clean backups
□ Functionality validated
□ Security controls verified

Post-Incident (within 72 hours)
□ Final impact assessment completed
□ Regulatory notifications submitted
□ Customer/partner communications sent
□ Incident report drafted
□ Lessons learned session conducted
□ Security improvements implemented
□ Process updates documented
```

---

## Document Approval

### Approval Matrix
```
Role                    | Name                | Signature | Date
------------------------|--------------------|-----------|---------
Project Manager         | [PM Name]          |           |
Technical Lead          | [Tech Lead Name]   |           |
Security Officer        | [Security Name]    |           |
Database Administrator  | [DBA Name]         |           |
Business Analyst        | [BA Name]          |           |
```

### Distribution List
- All Disaster Recovery Team Members
- Department Heads
- Executive Management
- External Audit Firm
- Key Vendor Contacts (restricted sections)

---

**Document Control:**
- **Classification:** Internal Use
- **Retention Period:** 7 years
- **Next Review Date:** December 17, 2025
- **Document Owner:** Project Manager
- **Technical Owner:** Technical Lead

---

*This Disaster Recovery Plan is a living document that must be updated regularly to reflect changes in technology, processes, and organizational structure. All team members are responsible for reporting gaps or improvements to ensure the plan remains current and effective.*
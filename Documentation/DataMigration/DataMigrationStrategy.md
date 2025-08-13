# GCC Database Solution - Data Migration Strategy Document

## Table of Contents
1. [Executive Overview](#executive-overview)
2. [Migration Architecture](#migration-architecture)
3. [Data Migration Methodology](#data-migration-methodology)
4. [Technical Implementation Strategy](#technical-implementation-strategy)
5. [Data Quality Framework](#data-quality-framework)
6. [Security and Compliance](#security-and-compliance)
7. [Performance Optimization](#performance-optimization)
8. [Testing Strategy](#testing-strategy)
9. [Deployment Strategy](#deployment-strategy)
10. [Monitoring and Maintenance](#monitoring-and-maintenance)

## Executive Overview

### Strategic Objectives
The GCC Database migration strategy aims to:
- Seamlessly transition from legacy systems to modern ASP.NET Core architecture
- Ensure zero data loss and maintain 100% data integrity
- Minimize business disruption with targeted < 4-hour migration window
- Establish scalable infrastructure for future growth
- Implement comprehensive audit and compliance tracking

### Scope Definition
**In Scope:**
- All certificate data and metadata
- User accounts and role assignments
- Product and company information
- File assets (PDFs, images)
- Historical audit data
- System configurations

**Out of Scope:**
- Legacy system decommissioning (separate project)
- Third-party integrations (Phase 2)
- Historical log files older than 2 years

### Success Metrics
| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Data Accuracy | 99.99% | Automated validation scripts |
| Migration Duration | < 4 hours | Time tracking during execution |
| System Availability | 99.9% post-migration | Monitoring tools |
| User Satisfaction | > 90% | Post-migration survey |
| Performance | < 2s page load | Application performance monitoring |

## Migration Architecture

### System Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Source Systems                           │
├─────────────────────────────────────────────────────────────┤
│  Legacy Database  │  File Storage  │  Configuration Files   │
└────────┬──────────┴───────┬────────┴────────┬──────────────┘
         │                  │                  │
         ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│                  ETL Pipeline (SSIS/Custom)                  │
├─────────────────────────────────────────────────────────────┤
│  Extract  │  Transform  │  Validate  │  Load  │  Verify     │
└────────┬──────────────────────────────────────┬─────────────┘
         │                                      │
         ▼                                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    Target Systems                            │
├─────────────────────────────────────────────────────────────┤
│  SQL Server LocalDB  │  Azure Blob Storage  │  App Config   │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow Architecture

```
Source Data → Extraction Layer → Staging Database → 
Transformation Engine → Validation Layer → Target Database
```

### Technology Stack
- **Database**: SQL Server LocalDB (Development) / SQL Server (Production)
- **ETL Tools**: SQL Server Integration Services (SSIS) / Custom C# utilities
- **Validation**: Custom validation framework with automated testing
- **Monitoring**: Application Insights / Custom dashboard
- **Backup**: SQL Server native backup with Azure Backup integration

## Data Migration Methodology

### Migration Approach: Hybrid Strategy

**Big Bang + Phased Hybrid Model**
- Core data (certificates, companies, products): Big Bang migration
- User data and configurations: Phased migration
- File assets: Parallel migration with verification

### Migration Phases

#### Phase 0: Discovery and Assessment (2 weeks)
```
Week 1: Data Profiling
├── Analyze source data quality
├── Document data lineage
├── Identify data relationships
└── Create data dictionary

Week 2: Gap Analysis
├── Map source to target schemas
├── Identify transformation requirements
├── Document business rules
└── Define validation criteria
```

#### Phase 1: Design and Development (3 weeks)
```
Week 1: Architecture Design
├── Design ETL workflows
├── Create mapping documents
├── Define error handling
└── Design rollback procedures

Week 2-3: Development
├── Build extraction scripts
├── Develop transformation logic
├── Create validation routines
└── Implement monitoring
```

#### Phase 2: Testing and Validation (2 weeks)
```
Week 1: Unit and Integration Testing
├── Test individual components
├── Validate transformations
├── Test error handling
└── Performance benchmarking

Week 2: UAT and Performance Testing
├── User acceptance testing
├── Load testing
├── Security testing
└── Disaster recovery testing
```

#### Phase 3: Production Migration (1 weekend)
```
Friday Evening:
├── Final backup
├── System freeze
└── Pre-migration checks

Saturday:
├── Execute migration
├── Validate data
└── System testing

Sunday:
├── Final validation
├── Go-live decision
└── System activation
```

## Technical Implementation Strategy

### Database Schema Migration

#### Schema Versioning Strategy
```sql
-- Version control for database schema
CREATE TABLE SchemaVersion (
    VersionID INT PRIMARY KEY IDENTITY,
    Version VARCHAR(20) NOT NULL,
    AppliedDate DATETIME DEFAULT GETDATE(),
    AppliedBy VARCHAR(100),
    Description VARCHAR(500)
);
```

#### Key Migration Tables
```sql
-- Migration tracking table
CREATE TABLE MigrationLog (
    MigrationID INT PRIMARY KEY IDENTITY,
    SourceTable VARCHAR(100),
    TargetTable VARCHAR(100),
    RecordsProcessed INT,
    RecordsSucceeded INT,
    RecordsFailed INT,
    StartTime DATETIME,
    EndTime DATETIME,
    Status VARCHAR(50),
    ErrorDetails NVARCHAR(MAX)
);

-- Data validation results
CREATE TABLE ValidationResults (
    ValidationID INT PRIMARY KEY IDENTITY,
    MigrationID INT FOREIGN KEY REFERENCES MigrationLog(MigrationID),
    ValidationRule VARCHAR(200),
    PassedCount INT,
    FailedCount INT,
    FailedRecordIDs NVARCHAR(MAX),
    ExecutionTime DATETIME DEFAULT GETDATE()
);
```

### ETL Process Implementation

#### Certificate Migration Process
```sql
-- Step 1: Extract certificates with transformations
WITH CertificateTransform AS (
    SELECT 
        CertificateNumber,
        CertificateType,
        CompanyName,
        ProductName,
        CASE 
            WHEN ExpirationDate < GETDATE() THEN 'Expired'
            WHEN ExpirationDate < DATEADD(day, 30, GETDATE()) THEN 'Expiring'
            ELSE 'Active'
        END AS Status,
        EffectiveDate,
        ExpirationDate,
        Location,
        Country,
        -- Parse comma-separated ASINs if present
        STRING_SPLIT(ASINList, ',') AS ASINs,
        -- Parse comma-separated UPCs if present
        STRING_SPLIT(UPCList, ',') AS UPCs
    FROM SourceCertificates
)
-- Step 2: Insert into target with validation
INSERT INTO Certificates (
    Number, Type, CompanyID, ProductID, Status, 
    EffectiveDate, ExpirationDate, Location, Country
)
SELECT 
    ct.CertificateNumber,
    ct.CertificateType,
    c.CompanyID,
    p.ProductID,
    ct.Status,
    ct.EffectiveDate,
    ct.ExpirationDate,
    ct.Location,
    ct.Country
FROM CertificateTransform ct
INNER JOIN Companies c ON ct.CompanyName = c.Name
INNER JOIN Products p ON ct.ProductName = p.Name
WHERE NOT EXISTS (
    SELECT 1 FROM Certificates 
    WHERE Number = ct.CertificateNumber
);
```

#### User Migration with Role Mapping
```sql
-- Map users to new role structure
INSERT INTO AspNetUsers (Id, UserName, Email, EmailConfirmed, NormalizedUserName, NormalizedEmail)
SELECT 
    NEWID(),
    Username,
    Email,
    1,
    UPPER(Username),
    UPPER(Email)
FROM SourceUsers;

-- Assign roles based on business rules
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 
    u.Id,
    CASE 
        WHEN su.UserType = 'Administrator' THEN (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')
        WHEN su.UserType = 'Internal' THEN (SELECT Id FROM AspNetRoles WHERE Name = 'GCCUser')
        WHEN su.UserType = 'External' THEN (SELECT Id FROM AspNetRoles WHERE Name = 'ExternalUser')
        ELSE (SELECT Id FROM AspNetRoles WHERE Name = 'Public')
    END
FROM AspNetUsers u
INNER JOIN SourceUsers su ON u.UserName = su.Username;
```

### File Migration Strategy

#### PDF Migration Process
```csharp
public class FileMigrationService
{
    private readonly string _sourcePath;
    private readonly string _targetPath;
    private readonly ILogger<FileMigrationService> _logger;
    
    public async Task<MigrationResult> MigrateCertificatePDFs()
    {
        var result = new MigrationResult();
        var files = Directory.GetFiles(_sourcePath, "*.pdf");
        
        foreach (var sourceFile in files)
        {
            try
            {
                // Extract certificate number from filename
                var certificateNumber = ExtractCertificateNumber(sourceFile);
                
                // Generate new standardized filename
                var newFileName = GenerateStandardFileName(certificateNumber);
                var targetFile = Path.Combine(_targetPath, newFileName);
                
                // Copy with verification
                await CopyWithVerification(sourceFile, targetFile);
                
                // Update database with new file location
                await UpdateCertificateFilePath(certificateNumber, targetFile);
                
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to migrate file: {sourceFile}");
                result.FailedFiles.Add(sourceFile);
            }
        }
        
        return result;
    }
    
    private async Task CopyWithVerification(string source, string target)
    {
        // Copy file
        File.Copy(source, target, overwrite: false);
        
        // Verify checksum
        var sourceHash = await ComputeFileHash(source);
        var targetHash = await ComputeFileHash(target);
        
        if (sourceHash != targetHash)
        {
            File.Delete(target);
            throw new InvalidOperationException("File verification failed");
        }
    }
}
```

## Data Quality Framework

### Data Quality Dimensions

| Dimension | Description | Measurement | Target |
|-----------|-------------|-------------|--------|
| Completeness | All required fields populated | % of non-null required fields | > 99% |
| Accuracy | Data reflects reality | Manual sampling validation | > 98% |
| Consistency | Data aligns across systems | Cross-reference checks | 100% |
| Timeliness | Data is current | Age of most recent update | < 24 hours |
| Validity | Data conforms to rules | Business rule validation | 100% |
| Uniqueness | No duplicate records | Duplicate detection algorithms | 0 duplicates |

### Data Validation Rules

#### Critical Validations
```sql
-- Certificate validation rules
CREATE PROCEDURE ValidateCertificates
AS
BEGIN
    -- Rule 1: All certificates must have unique numbers
    INSERT INTO ValidationResults (ValidationRule, FailedCount, FailedRecordIDs)
    SELECT 
        'Unique Certificate Numbers',
        COUNT(*) - COUNT(DISTINCT Number),
        STRING_AGG(CAST(CertificateID AS VARCHAR), ',')
    FROM Certificates
    GROUP BY Number
    HAVING COUNT(*) > 1;
    
    -- Rule 2: Expiration date must be after effective date
    INSERT INTO ValidationResults (ValidationRule, FailedCount, FailedRecordIDs)
    SELECT 
        'Valid Date Range',
        COUNT(*),
        STRING_AGG(CAST(CertificateID AS VARCHAR), ',')
    FROM Certificates
    WHERE ExpirationDate <= EffectiveDate;
    
    -- Rule 3: All certificates must have associated company and product
    INSERT INTO ValidationResults (ValidationRule, FailedCount, FailedRecordIDs)
    SELECT 
        'Required Relationships',
        COUNT(*),
        STRING_AGG(CAST(CertificateID AS VARCHAR), ',')
    FROM Certificates
    WHERE CompanyID IS NULL OR ProductID IS NULL;
END
```

### Data Cleansing Strategy

#### Automated Cleansing Rules
1. **Standardize text fields**: Trim whitespace, consistent casing
2. **Normalize dates**: Convert to UTC, validate ranges
3. **Deduplicate records**: Merge duplicates with audit trail
4. **Fix referential integrity**: Map orphaned records or flag for review
5. **Standardize locations**: Geocode and validate addresses

## Security and Compliance

### Security Requirements

#### Data Protection
- **Encryption at Rest**: SQL Server TDE enabled
- **Encryption in Transit**: TLS 1.2+ for all connections
- **Access Control**: Role-based access with principle of least privilege
- **Audit Logging**: Comprehensive audit trail for all data changes

#### Compliance Considerations
- **GDPR**: Personal data handling and right to erasure
- **SOC 2**: Security controls and audit requirements
- **Industry Standards**: Follow OWASP guidelines for web security

### Security Implementation
```csharp
public class SecureDataMigration
{
    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;
    
    public async Task MigrateSensitiveData(SensitiveData data)
    {
        try
        {
            // Encrypt sensitive fields
            data.Email = await _encryption.Encrypt(data.Email);
            data.Phone = await _encryption.Encrypt(data.Phone);
            
            // Audit the migration
            await _audit.LogMigration(new AuditEntry
            {
                Action = "DataMigration",
                Entity = "User",
                EntityId = data.Id,
                Timestamp = DateTime.UtcNow,
                User = "MigrationService"
            });
            
            // Perform migration
            await MigrateToTarget(data);
        }
        catch (Exception ex)
        {
            await _audit.LogError(ex);
            throw;
        }
    }
}
```

## Performance Optimization

### Performance Targets
| Operation | Target | Measurement |
|-----------|--------|-------------|
| Bulk Insert | 10,000 records/sec | Rows per second |
| Data Validation | < 5 minutes for 1M records | Execution time |
| Index Rebuild | < 30 minutes | Rebuild duration |
| Full Backup | < 15 minutes | Backup time |

### Optimization Strategies

#### Database Optimization
```sql
-- Disable indexes during bulk insert
ALTER INDEX ALL ON Certificates DISABLE;

-- Bulk insert with minimal logging
BULK INSERT Certificates
FROM 'certificates.csv'
WITH (
    TABLOCK,
    ROWS_PER_BATCH = 10000,
    BATCHSIZE = 100000
);

-- Rebuild indexes after migration
ALTER INDEX ALL ON Certificates REBUILD WITH (ONLINE = ON);

-- Update statistics
UPDATE STATISTICS Certificates WITH FULLSCAN;
```

#### Application-Level Optimization
```csharp
public class BulkMigrationService
{
    public async Task BulkMigrate<T>(IEnumerable<T> records) where T : class
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var bulkCopy = new SqlBulkCopy(connection)
        {
            BatchSize = 10000,
            BulkCopyTimeout = 600,
            EnableStreaming = true
        };
        
        // Map columns
        foreach (var property in typeof(T).GetProperties())
        {
            bulkCopy.ColumnMappings.Add(property.Name, property.Name);
        }
        
        // Convert to DataTable for bulk copy
        var dataTable = ConvertToDataTable(records);
        
        // Execute bulk insert
        await bulkCopy.WriteToServerAsync(dataTable);
    }
}
```

## Testing Strategy

### Test Phases

#### Unit Testing
- Individual transformation functions
- Validation rules
- Error handling routines
- Data cleansing functions

#### Integration Testing
- End-to-end data flow
- System integrations
- File migrations
- User authentication

#### Performance Testing
```yaml
Load Testing Scenarios:
  - Scenario: Concurrent User Load
    Users: 100
    Duration: 30 minutes
    Operations:
      - Search certificates
      - View certificate details
      - Export data
      - Upload files
    
  - Scenario: Data Migration Load
    Records: 1,000,000
    Target Time: 60 minutes
    Validation: 100% accuracy
```

#### User Acceptance Testing
- All user roles tested
- Critical business workflows
- Report generation
- Data export/import

### Test Data Strategy
```sql
-- Generate test data for migration testing
CREATE PROCEDURE GenerateTestData
    @RecordCount INT
AS
BEGIN
    DECLARE @i INT = 1;
    
    WHILE @i <= @RecordCount
    BEGIN
        INSERT INTO TestCertificates (
            Number, Type, CompanyName, ProductName,
            EffectiveDate, ExpirationDate
        )
        VALUES (
            CONCAT('TEST-', RIGHT('00000' + CAST(@i AS VARCHAR), 5)),
            CASE @i % 3 
                WHEN 0 THEN 'Type A'
                WHEN 1 THEN 'Type B'
                ELSE 'Type C'
            END,
            CONCAT('Company ', @i % 100),
            CONCAT('Product ', @i % 50),
            DATEADD(day, -365, GETDATE()),
            DATEADD(day, 365, GETDATE())
        );
        
        SET @i = @i + 1;
    END
END
```

## Deployment Strategy

### Environment Strategy

| Environment | Purpose | Data Volume | Refresh Cycle |
|-------------|---------|-------------|---------------|
| Development | Development and testing | 10% sample | Weekly |
| Staging | Pre-production validation | 100% copy | Before each release |
| Production | Live system | Full data | N/A |
| DR | Disaster recovery | Full replica | Real-time sync |

### Deployment Checklist

#### Pre-Deployment
- [ ] All tests passing (unit, integration, performance)
- [ ] Security scan completed
- [ ] Backup verified and tested
- [ ] Rollback procedures documented
- [ ] Stakeholder approval obtained
- [ ] Communication sent to users
- [ ] Maintenance window scheduled

#### During Deployment
- [ ] System backup completed
- [ ] Database migration executed
- [ ] File migration completed
- [ ] Configuration updated
- [ ] Smoke tests passed
- [ ] User access verified
- [ ] Performance benchmarks met

#### Post-Deployment
- [ ] System monitoring active
- [ ] User feedback collected
- [ ] Performance metrics validated
- [ ] Security audit completed
- [ ] Documentation updated
- [ ] Lessons learned documented

### Rollback Strategy

#### Immediate Rollback (< 30 minutes)
```sql
-- Quick rollback procedure
CREATE PROCEDURE EmergencyRollback
AS
BEGIN
    -- Restore database from backup
    RESTORE DATABASE GCCDatabase 
    FROM DISK = 'C:\Backups\GCC_PreMigration.bak'
    WITH REPLACE, NORECOVERY;
    
    -- Restore transaction logs
    RESTORE LOG GCCDatabase 
    FROM DISK = 'C:\Backups\GCC_PreMigration.trn'
    WITH RECOVERY;
    
    -- Revert application to previous version
    EXEC RevertApplicationVersion;
END
```

## Monitoring and Maintenance

### Key Performance Indicators (KPIs)

| KPI | Target | Alert Threshold | Action |
|-----|--------|-----------------|--------|
| Database Response Time | < 100ms | > 500ms | Investigate query performance |
| Page Load Time | < 2s | > 3s | Check application performance |
| Error Rate | < 0.1% | > 1% | Review error logs |
| User Sessions | N/A | < 10 active | Check system availability |
| Data Sync Lag | < 5 min | > 15 min | Check replication status |

### Monitoring Implementation
```csharp
public class MigrationMonitor
{
    private readonly ILogger<MigrationMonitor> _logger;
    private readonly IMetricsCollector _metrics;
    
    public async Task MonitorMigration()
    {
        // Track migration progress
        _metrics.TrackMetric("MigrationProgress", GetProgressPercentage());
        
        // Monitor data quality
        var qualityScore = await CalculateDataQuality();
        _metrics.TrackMetric("DataQuality", qualityScore);
        
        // Check system health
        var health = await CheckSystemHealth();
        if (!health.IsHealthy)
        {
            _logger.LogWarning($"System health check failed: {health.Message}");
            await SendAlert(health);
        }
        
        // Log performance metrics
        _metrics.TrackMetric("RecordsPerSecond", GetMigrationSpeed());
        _metrics.TrackMetric("ErrorRate", GetErrorRate());
    }
}
```

### Maintenance Schedule

#### Daily Tasks
- Review error logs
- Check backup completion
- Monitor performance metrics
- Validate data synchronization

#### Weekly Tasks
- Analyze performance trends
- Review security logs
- Update documentation
- Test disaster recovery

#### Monthly Tasks
- Full system backup verification
- Security audit
- Performance optimization review
- Capacity planning assessment

## Appendices

### A. SQL Migration Scripts

#### Create Migration Database
```sql
-- Create staging database for migration
CREATE DATABASE GCC_Migration_Staging;
GO

USE GCC_Migration_Staging;
GO

-- Create schema matching target structure
CREATE SCHEMA Migration;
GO

-- Create tracking tables
CREATE TABLE Migration.ExecutionLog (
    LogID INT IDENTITY PRIMARY KEY,
    Phase VARCHAR(50),
    StartTime DATETIME,
    EndTime DATETIME,
    Status VARCHAR(20),
    RecordsProcessed INT,
    ErrorCount INT,
    Details NVARCHAR(MAX)
);
```

### B. PowerShell Migration Scripts

```powershell
# Migration execution script
param(
    [string]$SourceServer,
    [string]$TargetServer,
    [string]$ConfigFile
)

# Load configuration
$config = Get-Content $ConfigFile | ConvertFrom-Json

# Execute migration phases
foreach ($phase in $config.Phases) {
    Write-Host "Executing phase: $($phase.Name)"
    
    try {
        # Run migration stored procedure
        Invoke-Sqlcmd -ServerInstance $TargetServer `
                      -Database "GCC_Migration" `
                      -Query "EXEC Migration.$($phase.Procedure)"
        
        Write-Host "Phase completed successfully"
    }
    catch {
        Write-Error "Phase failed: $_"
        
        # Execute rollback if configured
        if ($phase.RollbackOnError) {
            Invoke-Sqlcmd -ServerInstance $TargetServer `
                          -Database "GCC_Migration" `
                          -Query "EXEC Migration.Rollback"
        }
        
        throw
    }
}
```

### C. Validation Queries

```sql
-- Comprehensive data validation suite
CREATE PROCEDURE RunFullValidation
AS
BEGIN
    DECLARE @Results TABLE (
        TestName VARCHAR(100),
        Result VARCHAR(20),
        Details NVARCHAR(MAX)
    );
    
    -- Test 1: Record counts match
    INSERT INTO @Results
    SELECT 'Record Count Validation',
           CASE WHEN 
               (SELECT COUNT(*) FROM SourceDB.dbo.Certificates) = 
               (SELECT COUNT(*) FROM TargetDB.dbo.Certificates)
           THEN 'PASS' ELSE 'FAIL' END,
           CONCAT('Source: ', (SELECT COUNT(*) FROM SourceDB.dbo.Certificates),
                  ' Target: ', (SELECT COUNT(*) FROM TargetDB.dbo.Certificates));
    
    -- Test 2: No orphaned records
    INSERT INTO @Results
    SELECT 'Referential Integrity',
           CASE WHEN NOT EXISTS (
               SELECT 1 FROM Certificates c
               LEFT JOIN Companies co ON c.CompanyID = co.CompanyID
               WHERE co.CompanyID IS NULL
           ) THEN 'PASS' ELSE 'FAIL' END,
           'Check for orphaned certificate records';
    
    -- Test 3: Data quality metrics
    INSERT INTO @Results
    SELECT 'Data Quality Score',
           CASE WHEN (
               SELECT AVG(CAST(QualityScore AS FLOAT))
               FROM (
                   SELECT CASE 
                       WHEN LEN(Number) > 0 THEN 1 ELSE 0 END +
                       CASE WHEN CompanyID IS NOT NULL THEN 1 ELSE 0 END +
                       CASE WHEN ProductID IS NOT NULL THEN 1 ELSE 0 END +
                       CASE WHEN EffectiveDate IS NOT NULL THEN 1 ELSE 0 END +
                       CASE WHEN ExpirationDate IS NOT NULL THEN 1 ELSE 0 END
                       AS QualityScore
                   FROM Certificates
               ) q
           ) / 5.0 > 0.95 THEN 'PASS' ELSE 'FAIL' END,
           'Average data quality score across all records';
    
    SELECT * FROM @Results;
END
```

### D. Emergency Contacts

| Role | Name | Phone | Email | Escalation |
|------|------|-------|-------|------------|
| Technical Lead | [Name] | [Phone] | [Email] | Primary |
| Database Admin | [Name] | [Phone] | [Email] | Primary |
| Project Manager | [Name] | [Phone] | [Email] | Secondary |
| Business Owner | [Name] | [Phone] | [Email] | Secondary |
| Microsoft Support | N/A | 1-800-MICROSOFT | [Support Portal] | Tertiary |

## Document Version Control

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-07 | Migration Team | Initial strategy document |
| | | | |

## Sign-offs

| Stakeholder | Role | Signature | Date |
|-------------|------|-----------|------|
| | Technical Lead | | |
| | Business Owner | | |
| | Security Officer | | |
| | Project Manager | | |

---

*This document is confidential and proprietary to GreenCircle Certified. Distribution is limited to authorized personnel only.*
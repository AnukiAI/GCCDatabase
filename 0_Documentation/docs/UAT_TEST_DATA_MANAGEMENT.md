# UAT Test Data Management Strategy
## GCC Web Application - User Acceptance Testing

**Document Version:** 1.0  
**Last Updated:** August 2025  
**Data Owner:** UAT Lead  
**Approval:** Project Manager, Product Owner

---

## 🎯 **Test Data Management Objectives**

### **Primary Goals:**
✅ **Data Integrity:** Ensure consistent, accurate test data across all UAT cycles  
✅ **Environment Stability:** Maintain predictable data states for reliable testing  
✅ **Security Compliance:** Protect sensitive data and maintain privacy standards  
✅ **Test Coverage:** Provide comprehensive data scenarios for thorough testing  
✅ **Efficiency:** Enable rapid data setup and reset capabilities  

### **Success Criteria:**
- 100% test scenarios have required data available
- <15 minutes for complete data refresh
- Zero data privacy violations
- >95% data consistency across test cycles
- Full traceability of data changes

---

## 📊 **Data Categories & Requirements**

### **🏢 Master Data**

#### **User Accounts & Roles**
```sql
-- Core test users with defined roles
Users Required:
• Admin Users: 2 accounts (primary + backup)
• Analyst Users: 3 accounts (different experience levels)
• External Users: 5 accounts (different companies)
• Internal Users: 2 accounts (different departments)
• Public Users: 1 account (guest access)

Role Combinations:
• Multi-role users (Admin + Analyst)
• Disabled/locked accounts
• New users (never logged in)
• Expired password accounts
```

#### **Company & Organization Data**
```sql
-- Representative company profiles
Companies Required:
• Large Enterprise: Amazon, Microsoft (>10,000 employees)
• Mid-size: Regional manufacturers (1,000-10,000 employees)
• Small Business: Local companies (<1,000 employees)
• Government: Federal, state, local entities
• Non-profit: Environmental organizations
• International: Companies from different countries

Company Variations:
• Active vs inactive companies
• Different certification levels
• Various contact information formats
• Multiple address types
```

#### **Product & Certificate Data**
```sql
-- Diverse product portfolio
Products Required:
• Electronics: Computers, phones, appliances
• Automotive: Components, systems, vehicles
• Industrial: Manufacturing equipment, tools
• Consumer Goods: Household items, clothing
• Chemicals: Materials, compounds, treatments

Certificate Variations:
• Active certificates (various expiration dates)
• Expired certificates
• Pending approval certificates
• Rejected/withdrawn certificates
• Different certification types and standards
```

### **🔄 Transactional Data**

#### **Certificate Lifecycle Data**
```sql
-- Complete workflow scenarios
Certificate States:
• Draft certificates (work in progress)
• Submitted for review
• Under review (different stages)
• Approved and active
• Expired (various time periods)
• Renewed certificates
• Cancelled/withdrawn certificates

Workflow Scenarios:
• Standard approval flow
• Expedited processing
• Rejection and resubmission
• Amendment requests
• Bulk operations
```

#### **Audit & History Data**
```sql
-- Comprehensive audit trails
Audit Records:
• User login/logout activities
• Certificate creation/modification
• Administrative actions
• System configuration changes
• Data export activities
• Failed authentication attempts

Time-based Scenarios:
• Recent activity (last 24 hours)
• Weekly patterns (last 7 days)
• Monthly trends (last 30 days)
• Historical data (6+ months old)
```

### **🎛️ Configuration Data**

#### **System Settings**
```sql
-- Various system configurations
Settings Variations:
• Different notification preferences
• Various time zone configurations
• Multiple language settings
• Security policy variations
• Feature flag combinations

Administrative Data:
• System announcements
• Maintenance schedules
• Configuration backups
• Performance metrics
• Error logs
```

---

## 🏗️ **Data Environment Architecture**

### **Environment Data Flow**
```
┌─────────────────────┐
│   Production Data   │ (Anonymized Source)
└─────────┬───────────┘
          │ (Weekly Sync)
┌─────────▼───────────┐
│   Master Test DB    │ (Sanitized Repository)
└─────────┬───────────┘
          │ (On-demand Refresh)
┌─────────▼───────────┐
│   UAT Test DB       │ (Active Testing)
└─────────────────────┘
```

### **Data Refresh Strategy**

#### **Daily Refresh (Light)**
```sql
-- Reset transactional data only
BEGIN TRANSACTION;
  -- Clear session data
  DELETE FROM UserSessions WHERE CreatedAt < DATEADD(hour, -24, GETDATE());
  
  -- Reset test certificates to known state
  UPDATE Certificates 
  SET Status = 'Active', ModifiedAt = GETDATE()
  WHERE CertificateId IN (SELECT CertificateId FROM TestDataCertificates);
  
  -- Clear audit logs from testing
  DELETE FROM AuditLogs WHERE CreatedAt > DATEADD(hour, -8, GETDATE())
    AND UserId IN (SELECT UserId FROM TestDataUsers);
COMMIT;
```

#### **Weekly Refresh (Complete)**
```sql
-- Full database reset and reseed
-- 1. Backup current state
BACKUP DATABASE GCC_UAT TO DISK = 'C:\Backups\GCC_UAT_Weekly_Backup.bak';

-- 2. Drop and recreate
DROP DATABASE GCC_UAT;
CREATE DATABASE GCC_UAT;

-- 3. Restore from master template
RESTORE DATABASE GCC_UAT FROM DISK = 'C:\Templates\GCC_Master_Template.bak';

-- 4. Run seed data scripts
EXEC sp_executesql @TestDataScript;
```

#### **On-Demand Refresh (Scenario-based)**
```sql
-- Refresh specific data scenarios
-- Example: Reset certificate workflow data
UPDATE Certificates 
SET Status = 'Draft', 
    ReviewStage = NULL,
    ApprovalDate = NULL
WHERE TestScenario = 'WorkflowTesting';

-- Reset related workflow data
DELETE FROM CertificateReviews 
WHERE CertificateId IN (
  SELECT CertificateId FROM Certificates WHERE TestScenario = 'WorkflowTesting'
);
```

---

## 🔐 **Data Security & Privacy**

### **Data Sanitization Process**

#### **Personal Data Anonymization**
```sql
-- Anonymize personal information
UPDATE Users SET
  FirstName = CONCAT('TestUser', UserId),
  LastName = CONCAT('LastName', UserId),
  Email = CONCAT('testuser', UserId, '@example.com'),
  Phone = CONCAT('555-0100-', RIGHT('000' + CAST(UserId AS VARCHAR), 3)),
  Address = CONCAT(UserId, ' Test Street'),
  City = 'Test City',
  PostalCode = RIGHT('00000' + CAST(UserId AS VARCHAR), 5)
WHERE Environment = 'UAT';

-- Anonymize company data
UPDATE Companies SET
  CompanyName = CONCAT('Test Company ', CompanyId),
  ContactEmail = CONCAT('contact', CompanyId, '@testcompany.com'),
  Phone = CONCAT('555-0200-', RIGHT('000' + CAST(CompanyId AS VARCHAR), 3)),
  Address = CONCAT(CompanyId, ' Business Avenue')
WHERE Environment = 'UAT';
```

#### **Sensitive Data Removal**
```sql
-- Remove or mask sensitive information
-- Credit card numbers
UPDATE PaymentMethods SET
  CardNumber = '4111111111111111',  -- Test card number
  CVV = '123',
  ExpirationDate = '12/25'
WHERE Environment = 'UAT';

-- Financial data
UPDATE InvoiceDetails SET
  Amount = ROUND(Amount * 0.1, 2),  -- Scale down amounts
  TaxAmount = ROUND(TaxAmount * 0.1, 2)
WHERE Environment = 'UAT';
```

### **Access Control**

#### **Data Access Matrix**
| **Role** | **Read Access** | **Write Access** | **Delete Access** | **Export Access** |
|----------|----------------|------------------|-------------------|-------------------|
| **UAT Lead** | All data | Test data only | Test data only | Sanitized reports |
| **UAT Tester** | Assigned test data | Test results | Own test results | Test evidence |
| **Developer** | Debug data | Bug fixes only | No access | Debug logs |
| **DBA** | System data | Schema changes | Backup/restore | System reports |

#### **Data Retention Policies**
- **Test Results:** Retained for 90 days after UAT completion
- **Personal Data:** Anonymized after each refresh cycle
- **Audit Logs:** Retained for compliance requirements (6 months)
- **Backup Data:** Monthly backups for 1 year

---

## 📋 **Test Data Scenarios**

### **Functional Testing Data**

#### **User Authentication Scenarios**
```sql
-- Valid login scenarios
INSERT INTO TestUsers (Username, Role, Status, Scenario)
VALUES 
  ('admin.test', 'Admin', 'Active', 'ValidLogin'),
  ('analyst.new', 'Analyst', 'Active', 'NewUserLogin'),
  ('external.partner', 'External', 'Active', 'ExternalLogin');

-- Invalid login scenarios  
INSERT INTO TestUsers (Username, Role, Status, Scenario)
VALUES 
  ('locked.user', 'Analyst', 'Locked', 'LockedAccount'),
  ('expired.user', 'External', 'Expired', 'ExpiredAccount'),
  ('disabled.user', 'Admin', 'Disabled', 'DisabledAccount');
```

#### **Certificate Management Scenarios**
```sql
-- Certificate workflow scenarios
INSERT INTO TestCertificates (Number, Status, Company, Scenario)
VALUES 
  ('CERT-001', 'Draft', 'Test Company A', 'DraftCertificate'),
  ('CERT-002', 'Under Review', 'Test Company B', 'ReviewProcess'),
  ('CERT-003', 'Approved', 'Test Company C', 'ActiveCertificate'),
  ('CERT-004', 'Expired', 'Test Company D', 'ExpiredCertificate'),
  ('CERT-005', 'Rejected', 'Test Company E', 'RejectedCertificate');
```

### **Performance Testing Data**

#### **High Volume Data Sets**
```sql
-- Large dataset for performance testing
-- Generate 10,000 certificates
DECLARE @Counter INT = 1;
WHILE @Counter <= 10000
BEGIN
  INSERT INTO TestCertificates (Number, Status, Company, CreatedDate, Scenario)
  VALUES 
    (CONCAT('PERF-', FORMAT(@Counter, '00000')), 
     'Active', 
     CONCAT('Performance Test Company ', (@Counter % 100) + 1),
     DATEADD(day, -(@Counter % 365), GETDATE()),
     'PerformanceTest');
  SET @Counter = @Counter + 1;
END;
```

#### **Search and Filter Test Data**
```sql
-- Complex search scenarios
INSERT INTO TestCertificates (Number, Status, Company, ProductType, ExpirationDate, Scenario)
VALUES 
  ('SEARCH-001', 'Active', 'Electronics Corp', 'Laptop', '2025-12-31', 'SearchTest'),
  ('SEARCH-002', 'Active', 'Auto Parts Inc', 'Brake System', '2024-06-30', 'SearchTest'),
  ('SEARCH-003', 'Expired', 'Green Energy Ltd', 'Solar Panel', '2024-01-15', 'SearchTest'),
  ('SEARCH-004', 'Active', 'Chemical Solutions', 'Eco-Cleaner', '2026-03-20', 'SearchTest');
```

### **Security Testing Data**

#### **SQL Injection Test Data**
```sql
-- Test data for security validation
INSERT INTO TestUsers (Username, Email, Scenario, Notes)
VALUES 
  ('test.injection', 'test@example.com', 'SQLInjection', 'Safe test account for SQL injection testing'),
  ('xss.test', 'xss@example.com', 'XSSTesting', 'Account for XSS vulnerability testing'),
  ('auth.bypass', 'bypass@example.com', 'AuthBypass', 'Account for authorization testing');
```

---

## 🛠️ **Data Management Tools & Scripts**

### **Data Setup Scripts**

#### **Complete Environment Setup**
```bash
#!/bin/bash
# setup-uat-data.sh - Complete UAT data environment setup

echo "Starting UAT Data Setup..."

# 1. Backup existing data
echo "Creating backup..."
sqlcmd -S localhost -d GCC_UAT -Q "BACKUP DATABASE GCC_UAT TO DISK = 'C:\Backups\GCC_UAT_$(Get-Date -Format yyyyMMdd).bak'"

# 2. Clear existing test data
echo "Clearing existing test data..."
sqlcmd -S localhost -d GCC_UAT -i "scripts/clear-test-data.sql"

# 3. Load master data
echo "Loading master data..."
sqlcmd -S localhost -d GCC_UAT -i "scripts/load-master-data.sql"

# 4. Load transactional data
echo "Loading transactional data..."
sqlcmd -S localhost -d GCC_UAT -i "scripts/load-transaction-data.sql"

# 5. Load specific test scenarios
echo "Loading test scenarios..."
sqlcmd -S localhost -d GCC_UAT -i "scripts/load-test-scenarios.sql"

# 6. Verify data integrity
echo "Verifying data integrity..."
sqlcmd -S localhost -d GCC_UAT -i "scripts/verify-data.sql"

echo "UAT Data Setup Complete!"
```

#### **Quick Data Reset**
```bash
#!/bin/bash
# quick-reset.sh - Quick data reset for daily testing

echo "Performing quick data reset..."

# Reset only transactional data
sqlcmd -S localhost -d GCC_UAT -Q "
UPDATE Certificates 
SET Status = 'Active', 
    ModifiedAt = GETDATE() 
WHERE TestScenario IS NOT NULL;

DELETE FROM AuditLogs 
WHERE CreatedAt > DATEADD(hour, -8, GETDATE());

UPDATE Users 
SET LastLoginDate = NULL 
WHERE Email LIKE '%test%';
"

echo "Quick reset complete!"
```

### **Data Validation Scripts**

#### **Data Integrity Validation**
```sql
-- validate-data.sql - Comprehensive data validation
-- Check for required test users
SELECT 'User Validation' AS CheckType,
       CASE 
         WHEN COUNT(*) >= 13 THEN 'PASS' 
         ELSE 'FAIL - Missing test users' 
       END AS Status,
       COUNT(*) AS ActualCount,
       13 AS RequiredCount
FROM Users 
WHERE Email LIKE '%test%' OR Email LIKE '%example.com';

-- Check for required test certificates
SELECT 'Certificate Validation' AS CheckType,
       CASE 
         WHEN COUNT(*) >= 100 THEN 'PASS' 
         ELSE 'FAIL - Insufficient test certificates' 
       END AS Status,
       COUNT(*) AS ActualCount,
       100 AS RequiredCount
FROM Certificates 
WHERE TestScenario IS NOT NULL;

-- Check for required companies
SELECT 'Company Validation' AS CheckType,
       CASE 
         WHEN COUNT(*) >= 20 THEN 'PASS' 
         ELSE 'FAIL - Insufficient test companies' 
       END AS Status,
       COUNT(*) AS ActualCount,
       20 AS RequiredCount
FROM Companies 
WHERE CompanyName LIKE 'Test Company%';
```

### **Data Monitoring Dashboard**

#### **Data Health Check Query**
```sql
-- data-health-dashboard.sql
-- Real-time data status for UAT team

SELECT 
  'Data Health Dashboard' AS Title,
  GETDATE() AS CheckTime;

-- User account status
SELECT 
  'User Accounts' AS Category,
  Role,
  COUNT(*) AS Count,
  COUNT(CASE WHEN Status = 'Active' THEN 1 END) AS Active,
  COUNT(CASE WHEN Status = 'Locked' THEN 1 END) AS Locked
FROM Users 
WHERE Email LIKE '%test%' OR Email LIKE '%example.com'
GROUP BY Role;

-- Certificate distribution
SELECT 
  'Certificates' AS Category,
  Status,
  COUNT(*) AS Count,
  MIN(CreatedDate) AS OldestDate,
  MAX(CreatedDate) AS NewestDate
FROM Certificates 
WHERE TestScenario IS NOT NULL
GROUP BY Status;

-- Data freshness check
SELECT 
  'Data Freshness' AS Category,
  'Last Refresh' AS Metric,
  MAX(CreatedDate) AS LastUpdate,
  DATEDIFF(hour, MAX(CreatedDate), GETDATE()) AS HoursSinceUpdate
FROM TestDataRefreshLog;
```

---

## 📊 **Data Lifecycle Management**

### **Refresh Schedule**

#### **Daily Operations (Automated)**
```
6:00 AM - Data health check
6:15 AM - Quick transactional reset
6:30 AM - User session cleanup
7:00 AM - Generate daily data report
7:30 AM - Notify UAT team of data status
```

#### **Weekly Operations (Scheduled)**
```
Sunday 2:00 AM - Complete database backup
Sunday 2:30 AM - Full data refresh from master
Sunday 3:00 AM - Load fresh test scenarios
Sunday 3:30 AM - Validate all data sets
Sunday 4:00 AM - Generate weekly data report
```

#### **Monthly Operations (Manual)**
```
First Saturday - Master data template update
Second Saturday - Archive old test data
Third Saturday - Security audit of test data
Fourth Saturday - Performance optimization
```

### **Data Versioning**

#### **Version Control Strategy**
```
Data Templates:
• v1.0 - Initial UAT setup
• v1.1 - Additional security test data
• v1.2 - Performance testing datasets
• v1.3 - Edge case scenarios

Change Management:
• All data changes tracked in TestDataChangeLog
• Version increments for significant changes
• Rollback capability to previous versions
• Impact assessment for data changes
```

---

## 🚨 **Troubleshooting Guide**

### **Common Data Issues**

#### **Issue 1: Authentication Failures**
```sql
-- Diagnosis
SELECT Username, Status, LastLoginAttempt, FailedLoginCount
FROM Users 
WHERE Email LIKE '%test%' AND Status != 'Active';

-- Resolution
UPDATE Users 
SET Status = 'Active', 
    FailedLoginCount = 0,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword('TestPass123!')
WHERE Email LIKE '%test%';
```

#### **Issue 2: Missing Test Data**
```sql
-- Diagnosis
SELECT TestScenario, COUNT(*) as Count
FROM Certificates 
WHERE TestScenario IS NOT NULL
GROUP BY TestScenario
ORDER BY Count DESC;

-- Resolution
EXEC LoadTestScenarioData @ScenarioName = 'MissingScenario';
```

#### **Issue 3: Data Corruption**
```sql
-- Diagnosis
DBCC CHECKDB('GCC_UAT') WITH NO_INFOMSGS;

-- Resolution (if corruption found)
RESTORE DATABASE GCC_UAT 
FROM DISK = 'C:\Backups\GCC_UAT_LastKnownGood.bak' 
WITH REPLACE;
```

---

## ✅ **Data Quality Metrics**

### **Quality KPIs**
- **Data Completeness:** >98% of required test scenarios have data
- **Data Accuracy:** <1% data validation failures
- **Data Freshness:** Data refreshed within last 24 hours
- **Performance:** <5 seconds for data reset operations
- **Security:** Zero privacy violations in test data

### **Daily Quality Report Template**
```
UAT Data Quality Report - [Date]

Data Completeness: [XX/XX scenarios] (XX%)
Data Accuracy: [XX validation checks passed] (XX%)
Data Freshness: [Last refresh: XX hours ago]
Performance: [Reset time: XX seconds]
Security Status: [No issues/Issues found]

Top 3 Data Issues:
1. [Issue description and status]
2. [Issue description and status]  
3. [Issue description and status]

Recommendations:
• [Action item 1]
• [Action item 2]
• [Action item 3]
```

---

This comprehensive test data management strategy ensures reliable, consistent, and secure data availability throughout the UAT lifecycle, supporting effective testing and quality assurance.
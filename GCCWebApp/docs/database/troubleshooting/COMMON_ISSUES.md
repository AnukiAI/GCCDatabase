# Common Database Issues

**Last Updated:** November 17, 2025

Quick solutions to frequently encountered database problems.

---

## No Certificates Displaying on Website

### Symptom
- Website loads successfully
- Shows "0 certificates"
- Data exists in database

### Diagnostic Steps

**1. Verify data exists:**
```sql
SELECT COUNT(*) as TotalCerts,
       SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) as ActiveCerts
FROM Certificates;
```

Expected: At least some Active certificates

**2. Check table schema:**
```sql
SELECT SCHEMA_NAME(schema_id) as Schema, name as TableName
FROM sys.tables
WHERE name = 'Certificates';
```

Expected: `dbo` | `Certificates`

**If wrong schema** → See [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md)

**3. Verify application restarted:**
```bash
# IIS
Recycle application pool

# Or
Touch web.config to force restart
```

**4. Check connection string:**
```json
// appsettings.Production.json
"DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;..."
```

**See Also:** [Zero Certificates Issue](ZERO_CERTIFICATES_ISSUE.md) for detailed troubleshooting

---

## Login Failed for User

### Symptom
```
Login failed for user 'GCC_adminxyz123'
```

### Solutions

**1. Verify password:**
```bash
# Test connection
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -Q "SELECT 1"
```

**2. Check user exists:**
```sql
-- Connect as admin
SELECT name, type_desc
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';
```

**3. Grant database access:**
```sql
-- If user exists but can't access database
CREATE USER GCC_adminxyz123 FOR LOGIN GCC_adminxyz123;
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
```

**4. Verify login exists:**
```sql
-- At server level
SELECT name, type_desc
FROM sys.server_principals
WHERE name = 'GCC_adminxyz123';

-- If missing, create login
CREATE LOGIN GCC_adminxyz123 WITH PASSWORD = 'Q3Ez4e!gE_47KNi';
```

---

## Cannot Connect to Server

### Symptom
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server
```

### Solutions

**1. Test network connectivity:**
```bash
# Ping server
ping 68.71.129.120

# Test SQL port
Test-NetConnection -ComputerName 68.71.129.120 -Port 1533
```

**2. Verify SQL Server is running:**
```bash
# Windows
Get-Service MSSQLSERVER

# Or check remotely
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -Q "SELECT @@VERSION"
```

**3. Check firewall:**
- Port 1533 must be open
- Allow from your IP address
- Contact hosting provider if needed

**4. Verify connection string format:**
```
Server=68.71.129.120,1533  ✅ (comma before port)
Server=68.71.129.120:1533  ❌ (colon is wrong)
```

---

## Invalid Object Name 'dbo.Certificates'

### Symptom
```
Invalid object name 'dbo.Certificates'
```

### Cause
Tables created in wrong schema (e.g., `GCC_adminxyz123.Certificates` instead of `dbo.Certificates`)

### Solution

**1. Verify schema:**
```sql
SELECT SCHEMA_NAME(schema_id), name
FROM sys.tables
ORDER BY SCHEMA_NAME(schema_id), name;
```

**If tables are in wrong schema:**

```sql
-- 1. Fix user schema
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;

-- 2. Drop and recreate tables (use rebuild scripts)
-- See: docs/database/deployment/QUICK_START.md
```

**See:** [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md)

---

## Migration Errors

### Symptom
```
dotnet ef database update
Error: The database table already exists
```

### Solution

**Option 1: Clear migrations and reapply**
```sql
-- Delete migration history
DELETE FROM __EFMigrationsHistory;

-- Then reapply
dotnet ef database update
```

**Option 2: Use SQL scripts instead**
```bash
# Run schema script directly
cd DB_REBUILD_PACKAGE_WITH_DATA
sqlcmd ... -i "02_CREATE_SCHEMA.sql"
```

**Option 3: Generate idempotent script**
```bash
dotnet ef migrations script --idempotent -o schema.sql
```

---

## IDENTITY_INSERT Errors

### Symptom
```
Cannot insert explicit value for identity column when IDENTITY_INSERT is OFF
```

### Cause
Trying to insert specific ID values into auto-increment columns

### Solution

**Check current state:**
```sql
-- See if IDENTITY_INSERT is on for any table
SELECT name
FROM sys.tables
WHERE OBJECTPROPERTY(object_id, 'TableHasIdentity') = 1;
```

**Turn off IDENTITY_INSERT:**
```sql
SET IDENTITY_INSERT dbo.Certificates OFF;
SET IDENTITY_INSERT dbo.Companies OFF;
SET IDENTITY_INSERT dbo.Products OFF;
```

**Or restart SQL Server** to clear all IDENTITY_INSERT states.

---

## Foreign Key Constraint Violations

### Symptom
```
The INSERT statement conflicted with the FOREIGN KEY constraint
```

### Cause
Trying to insert data that references non-existent parent records

### Solution

**1. Check what's missing:**
```sql
-- Example: Certificate references non-existent Company
SELECT DISTINCT CompanyId
FROM Certificates
WHERE CompanyId NOT IN (SELECT Id FROM Companies);
```

**2. Load data in correct order:**
```
1. Lookup tables (CertificationTypes, Partners, etc.)
2. Companies
3. Products
4. Certificates
5. Users
```

**3. Use seed scripts** which load in correct order:
```bash
DB_REBUILD_PACKAGE_WITH_DATA/03_SEED_DATA_WITH_CERTIFICATES.sql
```

---

## Database Locks / Timeout

### Symptom
```
Timeout expired. The timeout period elapsed prior to completion of the operation
```

### Solutions

**1. Check for blocking:**
```sql
-- See what's blocking
SELECT blocking_session_id, wait_type, wait_time, wait_resource
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;
```

**2. Kill blocking process:**
```sql
-- Find blocking session
SELECT session_id, status, command, wait_type
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;

-- Kill it (use with caution)
KILL 52;  -- Replace with actual session_id
```

**3. Increase timeout in connection string:**
```
Server=...;Connection Timeout=300;...
```

---

## Duplicate Key Violations

### Symptom
```
Violation of PRIMARY KEY constraint. Cannot insert duplicate key
```

### Solutions

**1. Check existing data:**
```sql
SELECT * FROM Certificates WHERE Number = '24-0001';
```

**2. Delete duplicates:**
```sql
-- Be careful with this!
DELETE FROM Certificates WHERE Number = '24-0001';
```

**3. Use MERGE or UPDATE instead:**
```sql
IF EXISTS (SELECT 1 FROM Certificates WHERE Number = '24-0001')
    UPDATE Certificates SET ... WHERE Number = '24-0001'
ELSE
    INSERT INTO Certificates ...
```

---

## Application Can't See New Data

### Symptom
- Data inserted successfully
- Queries in SSMS return data
- Website shows old/no data

### Solutions

**1. Restart application:**
```bash
# IIS
Recycle application pool

# Or
touch web.config
```

**2. Clear application cache:**
```csharp
// Check if caching is enabled
// May need to clear Redis or memory cache
```

**3. Hard refresh browser:**
```
Ctrl + Shift + R (Chrome/Firefox)
Ctrl + F5 (Edge)
```

**4. Verify connection string** points to correct database

---

## Permission Denied Errors

### Symptom
```
The SELECT permission was denied on the object 'Certificates'
```

### Solution

**Grant permissions:**
```sql
-- Grant full access
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;

-- Or grant specific permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO GCC_adminxyz123;
```

**Verify permissions:**
```sql
SELECT
    dp.name as UserName,
    STRING_AGG(r.name, ', ') as Roles
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
WHERE dp.name = 'GCC_adminxyz123'
GROUP BY dp.name;
```

---

## Corrupted Migration History

### Symptom
```
The migration '20250918212035_AddCertificateProductIds' has already been applied
```

### Solution

**1. Check migration history:**
```sql
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

**2. Remove corrupted migration:**
```sql
DELETE FROM __EFMigrationsHistory
WHERE MigrationId = 'problematic_migration_id';
```

**3. Reapply migrations:**
```bash
dotnet ef database update
```

**4. Or use SQL scripts** to bypass migrations entirely

---

## Database Not Found

### Symptom
```
Cannot open database "gccwebap_certificates". Login failed.
```

### Solution

**1. Verify database exists:**
```sql
-- Connect to master database
USE master;
GO

SELECT name FROM sys.databases
WHERE name = 'gccwebap_certificates';
```

**2. Create database if missing:**
```sql
CREATE DATABASE gccwebap_certificates;
GO
```

**3. Verify connection string** has correct database name

---

## Diagnostic Queries

### Check Database Health
```sql
-- Database size
SELECT
    name,
    size * 8 / 1024 as SizeMB,
    max_size
FROM sys.database_files;

-- Table sizes
SELECT
    t.name as TableName,
    p.rows as RowCount,
    SUM(a.total_pages) * 8 / 1024 as TotalSpaceMB
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0
GROUP BY t.name, p.rows
ORDER BY TotalSpaceMB DESC;
```

### Check Active Connections
```sql
SELECT
    session_id,
    login_name,
    host_name,
    program_name,
    status,
    cpu_time,
    memory_usage
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('gccwebap_certificates');
```

### Check Recent Queries
```sql
SELECT TOP 10
    qs.execution_count,
    qs.total_elapsed_time / 1000000.0 as TotalSeconds,
    SUBSTRING(qt.text, 1, 500) as QueryText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
WHERE qt.text LIKE '%Certificates%'
ORDER BY qs.last_execution_time DESC;
```

---

## Quick Reference Commands

```sql
-- Verify schema
SELECT SCHEMA_NAME(schema_id), name FROM sys.tables;

-- Count records
SELECT 'Certificates' as Table, COUNT(*) FROM Certificates
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL SELECT 'Products', COUNT(*) FROM Products;

-- Check user
SELECT name, default_schema_name FROM sys.database_principals WHERE name = 'GCC_adminxyz123';

-- Test connection
SELECT @@VERSION, @@SERVERNAME, DB_NAME();

-- Restart connection pool
-- (Close all connections and reconnect)
```

---

## See Also

- [Zero Certificates Issue](ZERO_CERTIFICATES_ISSUE.md) - Detailed troubleshooting
- [Critical Schema Requirement](../reference/CRITICAL_SCHEMA_REQUIREMENT.md) - Schema issues
- [Connection Strings](../reference/CONNECTION_STRINGS.md) - Connection problems
- [Quick Start Guide](../deployment/QUICK_START.md) - Setup issues

---

## Getting Help

If none of these solutions work:

1. **Check application logs** in `/logs` folder
2. **Review SQL Server error logs**
3. **Verify environment configuration**
4. **Compare with working environment** (dev vs prod)
5. **See deployment guides** for step-by-step instructions

**For persistent issues, contact:** swest@anukiconsulting.com

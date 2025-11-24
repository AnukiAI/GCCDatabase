# Production Database Deployment Checklist

## **CRITICAL REQUIREMENT: DBO Schema**

❗ **ALL database tables MUST be in `dbo` schema** ❗

Entity Framework **WILL NOT WORK** if tables are under user schema (e.g., `GCC_adminxyz123.Certificates`)

### Why This Matters
- **Symptom**: Database has data but website shows nothing
- **Root Cause**: EF Core looks for `dbo.Certificates`, but tables are actually `GCC_adminxyz123.Certificates`
- **Result**: Application cannot find any data, behaves as if database is empty

---

## Pre-Deployment Checklist

### ✅ 1. Database User Configuration

**BEFORE creating any tables**, verify user configuration:

```sql
-- Connect to database
USE gccwebap_certificates;
GO

-- Check user's default schema
SELECT
    name as UserName,
    default_schema_name as DefaultSchema,
    CASE
        WHEN default_schema_name = 'dbo' THEN '✓ CORRECT'
        ELSE '✗ WRONG - MUST BE dbo'
    END as Status
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';

-- Check user is db_owner
SELECT
    CASE
        WHEN IS_ROLEMEMBER('db_owner', 'GCC_adminxyz123') = 1
        THEN '✓ User is db_owner'
        ELSE '✗ User is NOT db_owner - ADD TO ROLE'
    END as RoleStatus;
```

**Expected Results:**
- ✅ DefaultSchema: `dbo`
- ✅ RoleStatus: User is db_owner

**If NOT correct, run:**
```sql
-- Set default schema to dbo
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;

-- Add to db_owner role
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
```

### ✅ 2. Verify Clean Database

**BEFORE running schema scripts:**

```sql
-- Should return 0 rows (empty database)
SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
```

**If tables already exist with WRONG schema:**
- 🔴 **STOP!** Do not proceed
- 📋 Run `REBUILD_PRODUCTION_DBO.cmd` to start fresh
- ✅ Then continue with deployment

### ✅ 3. Connection String Verification

**In `appsettings.Production.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  },
  "DatabaseProvider": "SqlServer"
}
```

**Verify:**
- ✅ Server: `tcp:68.71.129.120,1533`
- ✅ Database: `gccwebap_certificates`
- ✅ User: `GCC_adminxyz123`
- ✅ DatabaseProvider: `SqlServer` (NOT `Sqlite` or `Postgres`)

---

## Deployment Steps

### Step 1: Setup Database User (ONE TIME ONLY)

```bash
cd DatabaseScripts
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -Q "ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo; ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;"
```

### Step 2: Verify Configuration

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 07_Check_Schema_Owner.sql
```

**Expected Output:**
```
SchemaName    TableCount    Status
----------    ----------    ------
dbo           0             CORRECT
```

**If you see user schema (GCC_adminxyz123) with tables:**
- 🔴 Tables are in WRONG schema
- 📋 Run `REBUILD_PRODUCTION_DBO.cmd`

### Step 3: Apply Database Schema

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 00_Complete_Schema.sql
```

### Step 4: Verify Schema Created Correctly

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -Q "SELECT SCHEMA_NAME(schema_id) as SchemaName, COUNT(*) as TableCount FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)"
```

**CRITICAL CHECK:**
```
SchemaName    TableCount
----------    ----------
dbo           40+
```

**MUST NOT show:**
```
SchemaName              TableCount
------------------      ----------
GCC_adminxyz123         40+        ← WRONG! REBUILD DATABASE!
```

### Step 5: Seed Initial Data

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 04_Quick_Fix_Minimal_Data.sql
```

### Step 6: Verify Data with Correct Status

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 05_Check_Certificate_Status.sql
```

**Expected:**
- ✅ At least some certificates with Status = `active`
- ✅ No certificates with Status = `NULL` or invalid values

**If needed, fix status:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 06_Fix_Certificate_Status.sql
```

---

## Post-Deployment Verification

### ✅ Database Level

```sql
-- 1. Verify schema ownership
SELECT SCHEMA_NAME(schema_id), name
FROM sys.tables
ORDER BY SCHEMA_NAME(schema_id), name;

-- Should ALL show: dbo | TableName

-- 2. Verify row counts
SELECT 'Roles' as TableName, COUNT(*) FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates;

-- 3. Verify certificate status distribution
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status;

-- Should show 'active' certificates that will display on public site

-- 4. Verify certificates that WILL display
SELECT COUNT(*) as VisibleCertificates
FROM Certificates
WHERE Status IN ('active', 'expiring_soon', 'renewal_in_progress');

-- Should be > 0
```

### ✅ Application Level

**Set environment:**
```bash
set ASPNETCORE_ENVIRONMENT=Production
```

**Start application:**
```bash
cd GCCWebApp
dotnet run --urls "http://localhost:5101"
```

**Check logs for:**
- ✅ "Using SQL Server" (not SQLite or Postgres)
- ✅ Connection string shows correct server
- ✅ No EF Core exceptions
- ✅ Migrations applied successfully

### ✅ API Level

**Test API endpoint:**
```bash
curl http://localhost:5101/api/certificate-tiles
```

**Expected Response:**
```json
{
  "totalCount": 10,
  "results": [
    {
      "id": 1,
      "certificateNumber": "24-1605",
      "status": "active",
      ...
    }
  ]
}
```

**If `totalCount: 0` despite database having certificates:**
- 🔴 Check certificate status values (must be `active`, `expiring_soon`, or `renewal_in_progress`)
- 🔴 Check schema ownership (tables must be `dbo.TableName`)

### ✅ Website Level

1. **Navigate to:** `/Certificate/CertificateTileView`
2. **Should see:** Certificate tiles displayed
3. **Filters:** Should populate with data from database
4. **Search:** Should return results
5. **Download:** Certificate PDFs should download

**If website shows 0 certificates:**
1. ❌ Check browser console (F12) for JavaScript errors
2. ❌ Check Network tab for failed API calls
3. ❌ Hard refresh (Ctrl+F5) to clear cache
4. ❌ Verify API endpoint returns data (test with curl/Postman)

---

## Common Issues & Fixes

### ❌ Issue: Tables Created Under User Schema

**Symptom:**
```sql
SELECT SCHEMA_NAME(schema_id), name FROM sys.tables;
-- Shows: GCC_adminxyz123 | Certificates (WRONG!)
```

**Fix:**
```bash
cd DatabaseScripts
REBUILD_PRODUCTION_DBO.cmd
```

This will:
1. Drop database
2. Recreate with proper user configuration
3. Apply schema (tables created as `dbo.TableName`)
4. Seed data

### ❌ Issue: Certificates Not Displaying

**Symptom:** Database has 80 certificates, website shows 0

**Diagnosis:**
```sql
-- Check status values
SELECT Status, COUNT(*) FROM Certificates GROUP BY Status;

-- Check how many WILL display
SELECT COUNT(*) FROM Certificates
WHERE Status IN ('active', 'expiring_soon', 'renewal_in_progress');
```

**Fix:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 06_Fix_Certificate_Status.sql
```

### ❌ Issue: EF Core Can't Find Tables

**Symptom:** Application logs show "Invalid object name 'Certificates'"

**Diagnosis:**
```sql
-- Check schema ownership
SELECT SCHEMA_NAME(schema_id), name FROM sys.tables WHERE name = 'Certificates';
```

**If shows:** `GCC_adminxyz123 | Certificates`
- 🔴 **WRONG SCHEMA**
- 📋 Rebuild database with `REBUILD_PRODUCTION_DBO.cmd`

**If shows:** `dbo | Certificates`
- ✅ Schema is correct
- Check connection string in `appsettings.Production.json`
- Verify application is using Production environment

---

## Quick Reference Commands

```bash
# Check schema ownership
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -Q "SELECT SCHEMA_NAME(schema_id), name FROM sys.tables"

# Check row counts
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -Q "SELECT 'Certificates' as T, COUNT(*) as Count FROM Certificates"

# Check certificate status
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -Q "SELECT Status, COUNT(*) FROM Certificates GROUP BY Status"

# Rebuild database (if schema is wrong)
cd DatabaseScripts
REBUILD_PRODUCTION_DBO.cmd
```

---

## Files Reference

| File | Purpose |
|------|---------|
| `07_Check_Schema_Owner.sql` | ✅ Check if tables are in dbo schema |
| `08_Rebuild_Database_DBO_Schema.sql` | 🔄 Rebuild database with dbo schema |
| `REBUILD_PRODUCTION_DBO.cmd` | 🚀 One-click rebuild (recommended) |
| `00_Complete_Schema.sql` | 📋 Create all tables |
| `04_Quick_Fix_Minimal_Data.sql` | 📊 Seed initial data |
| `05_Check_Certificate_Status.sql` | 🔍 Check certificate status values |
| `06_Fix_Certificate_Status.sql` | 🔧 Fix certificate status to 'active' |

---

## Success Criteria

✅ **Database Level:**
- All tables in `dbo` schema
- User `GCC_adminxyz123` has default schema = `dbo`
- User is member of `db_owner` role
- Tables exist and have data
- Certificates have status = `active` (or `expiring_soon`)

✅ **Application Level:**
- Application starts without errors
- Uses SQL Server (not SQLite)
- Connects to production database
- EF Core can read tables

✅ **Website Level:**
- `/Certificate/CertificateTileView` displays certificates
- Filters work and show data
- Search returns results
- Download works

---

**Last Updated:** 2025-11-02
**Critical Requirement Added:** DBO Schema Ownership

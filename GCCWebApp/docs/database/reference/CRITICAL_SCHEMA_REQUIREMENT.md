# Critical Schema Requirement

**Last Updated:** November 17, 2025
**Priority:** CRITICAL - READ BEFORE DEPLOYMENT

---

## The Critical Requirement

**ALL DATABASE TABLES MUST BE IN THE `dbo` SCHEMA**

Entity Framework requires tables to be named `dbo.TableName` (not `username.TableName`).

**This is the #1 cause of production deployment failures.**

---

## The Problem

When you create tables in SQL Server without setting a default schema, SQL Server creates them in the **user's schema** (e.g., `GCC_adminxyz123.Certificates`) instead of the **dbo schema** (`dbo.Certificates`).

**Result:** Entity Framework cannot find the tables.

**Symptoms:**
- Website loads but shows 0 certificates
- Error: "Invalid object name 'dbo.Certificates'"
- Data exists in database but application can't see it
- Queries work in SSMS but fail in application

---

## Why This Happens

SQL Server behavior:
```sql
-- User created WITHOUT default schema
CREATE USER GCC_adminxyz123 WITH PASSWORD = 'password';

-- User's default schema is their username
-- Any table they create goes into GCC_adminxyz123 schema
CREATE TABLE Certificates (...);
-- Creates: GCC_adminxyz123.Certificates ❌

-- Entity Framework looks for: dbo.Certificates ✅
-- MISMATCH! Table not found.
```

---

## The Fix

**BEFORE creating any tables:**

```sql
-- Set user's default schema to dbo
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
```

**Then create tables:**
```sql
-- Now creates: dbo.Certificates ✅
CREATE TABLE Certificates (...);
```

---

## Verification

### Check User's Default Schema

```sql
SELECT name, default_schema_name
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';
```

**Expected result:**
```
name                default_schema_name
---------------     -------------------
GCC_adminxyz123     dbo
```

**If default_schema_name is NULL or equals the username:**
→ Fix immediately before creating tables!

### Check Table Schema Ownership

```sql
SELECT
    SCHEMA_NAME(schema_id) as SchemaName,
    name as TableName
FROM sys.tables
ORDER BY SchemaName, TableName;
```

**Expected result:**
```
SchemaName  TableName
----------  ---------
dbo         ApiKeys
dbo         Certificates
dbo         Companies
dbo         Products
...
```

**If you see `GCC_adminxyz123` as SchemaName:**
→ Tables were created in wrong schema! See recovery below.

### Quick Count Check

```sql
SELECT SCHEMA_NAME(schema_id) as SchemaName, COUNT(*) as TableCount
FROM sys.tables
GROUP BY SCHEMA_NAME(schema_id);
```

**Expected result:**
```
SchemaName          TableCount
-----------         ----------
dbo                 35
```

**If you see multiple schemas or wrong schema:**
→ Tables need to be recreated!

---

## Recovery: Tables in Wrong Schema

If tables were created in the wrong schema, they must be **recreated**.

**You cannot easily transfer tables between schemas in SQL Server when there are foreign key constraints.**

### Option 1: Drop and Recreate (Recommended)

```bash
# Step 1: Drop all tables
cd DB_REBUILD_PACKAGE_WITH_DATA
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "01_DROP_ALL_TABLES.sql"

# Step 2: Fix user schema
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;"

# Step 3: Recreate schema
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "02_CREATE_SCHEMA.sql"

# Step 4: Reload data
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "03_SEED_DATA_WITH_CERTIFICATES.sql"
```

### Option 2: Use Entity Framework Migrations

```bash
# Fix user schema first
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;

# Drop all tables
cd DatabaseScripts
sqlcmd ... -i "DROP_ALL_TABLES.sql"

# Let EF recreate tables in correct schema
cd ../GCCWebApp
dotnet ef database update --connection "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;TrustServerCertificate=true;Encrypt=false"
```

---

## Prevention

### For New Deployments

**ALWAYS verify user schema BEFORE creating tables:**

```sql
-- 1. Check current schema
SELECT default_schema_name
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';

-- 2. If not 'dbo', fix it
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;

-- 3. Verify fix
SELECT default_schema_name
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';
-- Must show: dbo
```

### Use Pre-Deployment Checklist

Print and follow: [Pre-Deployment Checklist](../deployment/PRE_DEPLOYMENT_CHECKLIST.md)

Includes mandatory schema verification steps.

---

## Why Entity Framework Requires dbo Schema

Entity Framework Core has this behavior by default:

1. **No schema specified in model:** EF assumes `dbo` schema
2. **SQL Server default:** New users get their username as default schema
3. **Result:** Mismatch between EF expectation and SQL Server reality

**You can override this in EF with:**
```csharp
modelBuilder.HasDefaultSchema("GCC_adminxyz123");
```

**But this is NOT recommended because:**
- Violates SQL Server best practices
- Makes database harder to manage
- Breaks tooling that expects dbo schema
- Complicates migrations and backups

**Best practice:** Always use `dbo` schema.

---

## Production User Configuration

The production user should be configured as:

```sql
-- User must have these permissions
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;

-- User must have dbo as default schema
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;

-- Verify configuration
SELECT
    dp.name as UserName,
    dp.default_schema_name as DefaultSchema,
    STRING_AGG(r.name, ', ') as Roles
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
WHERE dp.name = 'GCC_adminxyz123'
GROUP BY dp.name, dp.default_schema_name;
```

**Expected result:**
```
UserName            DefaultSchema  Roles
-----------------   -------------  --------
GCC_adminxyz123     dbo            db_owner
```

---

## Decision Tree

### "Certificates not displaying on website"

```
1. Does data exist in database?
   SELECT COUNT(*) FROM Certificates;

   NO → Load data (see Quick Start Guide)
   YES → Continue to #2

2. What schema are tables in?
   SELECT SCHEMA_NAME(schema_id), name FROM sys.tables WHERE name = 'Certificates';

   Shows "GCC_adminxyz123" → WRONG SCHEMA! Fix user schema and recreate tables
   Shows "dbo" → Correct schema, check other issues

3. Is application pointing to correct database?
   Check connection string in appsettings.Production.json

4. Has application restarted since data load?
   IIS recycle application pool
```

---

## See Also

- [Quick Start Guide](../deployment/QUICK_START.md) - Includes schema verification
- [Zero Certificates Issue](../troubleshooting/ZERO_CERTIFICATES_ISSUE.md) - Specific troubleshooting
- [Pre-Deployment Checklist](../deployment/PRE_DEPLOYMENT_CHECKLIST.md) - Prevention checklist
- [Common Issues](../troubleshooting/COMMON_ISSUES.md) - Other common problems

---

## Summary

✅ **DO:**
- Set `DEFAULT_SCHEMA = dbo` BEFORE creating tables
- Verify schema AFTER creating tables
- Use pre-deployment checklist
- Let Entity Framework migrations create tables (they use dbo)

❌ **DON'T:**
- Create tables without checking user schema first
- Assume tables are in dbo schema
- Try to transfer tables between schemas (recreate instead)
- Skip schema verification steps

**This one configuration prevents 90% of production deployment issues.**

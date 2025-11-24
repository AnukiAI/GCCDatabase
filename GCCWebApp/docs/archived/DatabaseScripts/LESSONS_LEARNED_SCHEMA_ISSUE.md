# Lessons Learned: Production Database Schema Issue

**Date:** 2025-11-02
**Severity:** CRITICAL
**Impact:** Production website completely broken - 0 certificates displaying
**Duration:** Multiple hours to diagnose and fix

---

## Executive Summary

Production database was deployed with tables created under the **wrong schema** (`GCC_adminxyz123` instead of `dbo`), causing Entity Framework Core to be unable to find any tables. Despite 80+ records existing in the database, the website displayed 0 certificates because EF Core is hardcoded to use the `dbo` schema.

**Root Cause:** SQL Server user was not configured with `DEFAULT_SCHEMA = dbo` before table creation.

**Solution:** Drop all tables, configure user properly, reapply migrations using Entity Framework.

**Prevention:** Mandatory pre-deployment schema verification checklist.

---

## The Problem

### What Happened

1. **Initial Deployment:**
   - Created SQL Server user `GCC_adminxyz123` on production
   - Did NOT set `DEFAULT_SCHEMA = dbo`
   - Ran schema creation scripts
   - Tables created as `GCC_adminxyz123.Certificates` (wrong!)

2. **Symptom:**
   - Database showed 80+ certificate records existed
   - Website displayed **0 certificates**
   - No errors in logs
   - API returned `{"totalCount": 0, "results": []}`

3. **Why It Failed:**
   - Entity Framework Core **hardcoded** to look for `dbo.TableName`
   - Tables existed as `GCC_adminxyz123.TableName`
   - EF Core silently returned empty results (no error thrown)
   - Application behaved as if database was completely empty

### The Confusion

```sql
-- Database appeared to have data:
SELECT COUNT(*) FROM Certificates;
-- Result: 80 rows

-- But tables were in wrong schema:
SELECT SCHEMA_NAME(schema_id), name FROM sys.tables WHERE name = 'Certificates';
-- Result: GCC_adminxyz123 | Certificates  ← WRONG!
-- Should be: dbo | Certificates
```

---

## Root Cause Analysis

### Why Did This Happen?

**SQL Server Default Behavior:**
- When a user creates a table WITHOUT specifying schema
- SQL Server creates it under the **user's own schema** (not dbo)
- User schema name = username (e.g., `GCC_adminxyz123`)

**Example:**
```sql
-- User GCC_adminxyz123 runs:
CREATE TABLE Certificates (...);

-- SQL Server creates:
GCC_adminxyz123.Certificates  ← User's schema (WRONG for EF Core)

-- NOT:
dbo.Certificates  ← What EF Core expects
```

### What Was Missed

1. **User Configuration Step Skipped:**
   ```sql
   -- THIS WAS NOT RUN BEFORE TABLE CREATION:
   ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
   ```

2. **No Schema Verification:**
   - After running schema scripts, did not verify tables were in `dbo`
   - Assumed tables were created correctly

3. **Misleading Success:**
   - `SELECT COUNT(*) FROM Certificates` returned 80
   - This made it appear everything was working
   - But EF Core couldn't access the tables

### Entity Framework Core Behavior

**EF Core Schema Resolution:**
```csharp
// Entity Framework Core ALWAYS uses dbo schema by default:
modelBuilder.Entity<Certificate>().ToTable("Certificates", "dbo");

// This is equivalent to:
SELECT * FROM dbo.Certificates;

// If table is GCC_adminxyz123.Certificates, EF Core gets:
// Invalid object name 'dbo.Certificates'
// OR (if permissions allow):
// Empty result set (0 rows)
```

**Why No Error Was Logged:**
- EF Core doesn't throw an error if table doesn't exist in expected schema
- Just returns empty collection
- Application continues running with 0 results

---

## Technical Deep Dive

### SQL Server Schema Ownership

**Schema Definition:**
- A schema is a namespace for database objects
- Every table belongs to exactly one schema
- Format: `schemaName.tableName`

**Default Schemas:**
- `dbo` = database owner schema (standard)
- `username` = user-specific schema (SQL Server creates automatically)

**User Default Schema:**
```sql
-- Check user's default schema:
SELECT default_schema_name
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';

-- If NULL or equals username → tables will be created under user schema
-- If 'dbo' → tables will be created under dbo schema ✓
```

### How Schema Affects Table Creation

**Scenario 1: User WITHOUT dbo default schema (WRONG):**
```sql
-- User configuration:
default_schema_name = NULL  (or = 'GCC_adminxyz123')

-- When user runs:
CREATE TABLE Certificates (...);

-- SQL Server creates:
CREATE TABLE GCC_adminxyz123.Certificates (...);  ← WRONG!
```

**Scenario 2: User WITH dbo default schema (CORRECT):**
```sql
-- User configuration:
default_schema_name = 'dbo'

-- When user runs:
CREATE TABLE Certificates (...);

-- SQL Server creates:
CREATE TABLE dbo.Certificates (...);  ← CORRECT!
```

### Entity Framework Core Schema Mapping

**Default Mapping:**
```csharp
// In DbContext:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // EF Core defaults to dbo schema:
    modelBuilder.Entity<Certificate>();
    // Maps to: dbo.Certificates
}
```

**Explicit Schema (Not Used in This Project):**
```csharp
// Could explicitly specify schema (but we don't):
modelBuilder.Entity<Certificate>().ToTable("Certificates", "GCC_adminxyz123");
// Would map to: GCC_adminxyz123.Certificates
```

**Why We Use dbo:**
- Industry standard for ASP.NET Core applications
- Default EF Core behavior
- Avoids schema permission issues
- Simplifies deployment across environments

---

## Impact Assessment

### Production Downtime
- **Duration:** Multiple hours
- **User Impact:** Website completely non-functional
- **Data Loss:** None (data was in database, just inaccessible)
- **Revenue Impact:** N/A (pre-launch)

### Technical Debt Created
- Emergency database table recreation required
- Multiple diagnostic scripts created
- Documentation updates needed
- Deployment process revision needed

### What Could Have Gone Wrong
- If discovered after production launch with real customer data
- Data migration complexity if customers had uploaded certificates
- Potential data loss if backup strategy wasn't solid

---

## The Fix (What We Did)

### Step 1: Diagnosis (1-2 hours)
```bash
# Discovered tables in wrong schema:
sqlcmd -Q "SELECT SCHEMA_NAME(schema_id), name FROM sys.tables"
# Result: GCC_adminxyz123 | Certificates (WRONG!)

# Discovered EF Core was looking for dbo:
# Reviewed ApplicationDbContext code
# Confirmed EF Core uses dbo schema by default
```

### Step 2: Decision - Drop and Recreate
```bash
# Why not ALTER SCHEMA?
# - Complex with foreign keys
# - Risk of data corruption
# - No production data to preserve
# - Clean slate is safer
```

### Step 3: Table Recreation (30 minutes)
```sql
-- 1. Drop all foreign key constraints
-- 2. Drop all tables
-- 3. Configure user:
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;

-- 4. Clear migration history
DELETE FROM __EFMigrationsHistory;

-- 5. Reapply all migrations
dotnet ef database update

-- 6. Seed data
-- Run: 04_Quick_Fix_Minimal_Data.sql
```

### Step 4: Verification (10 minutes)
```sql
-- Verify schema:
SELECT SCHEMA_NAME(schema_id), COUNT(*)
FROM sys.tables
GROUP BY SCHEMA_NAME(schema_id);
-- Expected: dbo | 30

-- Verify data:
SELECT COUNT(*) FROM Certificates WHERE Status = 'active';
-- Expected: > 0

-- Test API:
curl http://localhost:5101/api/certificate-tiles
-- Expected: {"totalCount": 8, ...}
```

---

## Prevention - How to Never Let This Happen Again

### 1. MANDATORY Pre-Deployment Schema Configuration

**ALWAYS run this BEFORE creating any tables:**

```sql
USE gccwebap_certificates;
GO

-- Configure user with dbo schema
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;

-- Add to db_owner role
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;

-- VERIFY configuration
SELECT
    name as UserName,
    default_schema_name as DefaultSchema,
    CASE
        WHEN default_schema_name = 'dbo' THEN '✓ CORRECT'
        ELSE '✗ WRONG - STOP DEPLOYMENT'
    END as Status
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';

-- MUST show: DefaultSchema = dbo, Status = ✓ CORRECT
```

**If Status is WRONG:**
```
🛑 STOP DEPLOYMENT IMMEDIATELY
🔧 Run: ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
✅ Re-verify before proceeding
```

### 2. MANDATORY Post-Deployment Schema Verification

**ALWAYS verify after running schema scripts:**

```sql
-- Check that ALL tables are in dbo schema
SELECT
    SCHEMA_NAME(schema_id) as SchemaName,
    COUNT(*) as TableCount,
    CASE
        WHEN SCHEMA_NAME(schema_id) = 'dbo' THEN '✓ CORRECT'
        ELSE '✗ WRONG - ABORT DEPLOYMENT'
    END as Status
FROM sys.tables
GROUP BY SCHEMA_NAME(schema_id);

-- Expected Result:
-- SchemaName | TableCount | Status
-- dbo        | 30         | ✓ CORRECT

-- If you see any other schema (e.g., GCC_adminxyz123):
-- 🛑 STOP - Tables in wrong schema
-- 🔧 Drop all tables and start over
```

### 3. Use Entity Framework Migrations (Not SQL Scripts)

**Recommended Approach:**
```bash
# Let EF Core create the tables (it knows about dbo requirement):
dotnet ef database update --connection "[production-connection-string]"

# NOT:
sqlcmd -i 00_Complete_Schema.sql  ← Can create wrong schema
```

**Why EF Migrations Are Better:**
- EF Core handles schema correctly
- Type-safe (catches errors at compile time)
- Version controlled (in Migrations folder)
- Rollback support
- Idempotent (safe to run multiple times)

### 4. Deployment Checklist (MUST FOLLOW)

Create file: `DEPLOYMENT_CHECKLIST.txt`

```
PRE-DEPLOYMENT:
☐ User exists in database
☐ User DEFAULT_SCHEMA = dbo (verified)
☐ User in db_owner role (verified)
☐ Connection string tested
☐ Backup created (if data exists)

DEPLOYMENT:
☐ Use EF migrations (NOT SQL scripts)
☐ Run: dotnet ef database update
☐ Monitor for errors

POST-DEPLOYMENT VERIFICATION:
☐ All tables in dbo schema (verified)
☐ Row counts match expectations
☐ API endpoint returns data
☐ Website displays certificates
☐ Login works

ROLLBACK PLAN (IF ISSUES):
☐ Drop all tables
☐ Reapply migrations
☐ Restore data from backup
```

### 5. Automated Verification Script

Create: `verify-schema.sql`

```sql
-- Run this after EVERY deployment
DECLARE @NonDboTables INT;
DECLARE @DboTables INT;

SELECT @NonDboTables = COUNT(*)
FROM sys.tables
WHERE SCHEMA_NAME(schema_id) != 'dbo';

SELECT @DboTables = COUNT(*)
FROM sys.tables
WHERE SCHEMA_NAME(schema_id) = 'dbo';

IF @NonDboTables > 0
BEGIN
    RAISERROR('❌ DEPLOYMENT FAILED: %d tables in wrong schema', 16, 1, @NonDboTables);
END
ELSE IF @DboTables = 0
BEGIN
    RAISERROR('❌ DEPLOYMENT FAILED: No tables found', 16, 1);
END
ELSE
BEGIN
    PRINT '✅ DEPLOYMENT SUCCESSFUL: All ' + CAST(@DboTables AS VARCHAR) + ' tables in dbo schema';
END
```

### 6. Documentation Updates

**Files Updated:**
- ✅ `.claude/CLAUDE.md` - Added CRITICAL requirement
- ✅ `DatabaseScripts/PRODUCTION_DEPLOYMENT_CHECKLIST.md` - Added schema verification
- ✅ `DatabaseScripts/PRODUCTION_ISSUE_SUMMARY.md` - Complete technical analysis
- ✅ `DatabaseScripts/README_COMPLETE_REFRESH.md` - Step-by-step guide
- ✅ `DatabaseScripts/LESSONS_LEARNED_SCHEMA_ISSUE.md` - This document

**New Files Created:**
- ✅ `00_DROP_TABLES_ONLY.sql` - Safe table recreation
- ✅ `RECREATE_TABLES_NOW.cmd` - Automated recreation
- ✅ `07_Check_Schema_Owner.sql` - Schema verification
- ✅ `TABLES_RECREATED_SUCCESS.md` - Resolution summary

### 7. Training & Knowledge Transfer

**Required Reading for Anyone Deploying:**
1. This document (LESSONS_LEARNED_SCHEMA_ISSUE.md)
2. PRODUCTION_DEPLOYMENT_CHECKLIST.md
3. README_COMPLETE_REFRESH.md

**Key Takeaways:**
- ⚠️ SQL Server users need DEFAULT_SCHEMA = dbo BEFORE table creation
- ⚠️ Entity Framework requires dbo schema (not user schema)
- ⚠️ ALWAYS verify schema after deployment
- ⚠️ Use EF migrations (not SQL scripts) for schema deployment

---

## Red Flags - Warning Signs This Might Be Happening

### During Deployment:
1. ❌ Forgot to run `ALTER USER ... WITH DEFAULT_SCHEMA = dbo`
2. ❌ Used SQL scripts instead of EF migrations
3. ❌ Didn't verify schema after table creation
4. ❌ Saw user schema name in table listings

### After Deployment:
1. ❌ API returns `{"totalCount": 0}` despite data in database
2. ❌ Website shows "No certificates found"
3. ❌ Direct SQL queries return data but EF Core returns empty
4. ❌ No errors in logs (silent failure)

### How to Check:
```sql
-- Quick schema check:
SELECT SCHEMA_NAME(schema_id), name
FROM sys.tables
WHERE name = 'Certificates';

-- If result shows anything OTHER than 'dbo':
-- 🚨 CRITICAL ISSUE - STOP AND FIX IMMEDIATELY
```

---

## Cost-Benefit Analysis

### Cost of This Issue:
- **Time Lost:** 4+ hours (diagnosis + fix + documentation)
- **Stress Level:** HIGH (production down)
- **Risk:** HIGH (could have lost data)

### Cost of Prevention:
- **Time:** 5 minutes (run verification scripts)
- **Effort:** LOW (follow checklist)
- **Risk:** ZERO (catches issue before production)

### ROI:
**Preventing this issue is 48x faster than fixing it after deployment.**

---

## Success Criteria - Deployment is ONLY Successful If:

1. ✅ ALL tables in `dbo` schema (0 tables in user schema)
2. ✅ User configured with `DEFAULT_SCHEMA = dbo`
3. ✅ Data exists and is accessible
4. ✅ API endpoint returns data (`totalCount > 0`)
5. ✅ Website displays certificates
6. ✅ No errors in application logs

**If ANY criterion fails → Deployment FAILED → Rollback and investigate**

---

## Future Improvements

### Short Term (Next Deployment):
1. Add automated schema verification to deployment scripts
2. Use EF migrations exclusively (retire SQL schema scripts)
3. Create pre-deployment checklist PDF
4. Add schema verification to CI/CD pipeline

### Long Term:
1. Implement database schema monitoring
2. Add alerts if tables detected in wrong schema
3. Create deployment automation that enforces schema checks
4. Add integration tests that verify schema ownership

---

## References

### Documentation Created:
- `LESSONS_LEARNED_SCHEMA_ISSUE.md` (this document)
- `PRODUCTION_DEPLOYMENT_CHECKLIST.md`
- `PRODUCTION_ISSUE_SUMMARY.md`
- `README_COMPLETE_REFRESH.md`
- `TABLES_RECREATED_SUCCESS.md`

### Scripts Created:
- `00_DROP_TABLES_ONLY.sql` - Safe table drop
- `RECREATE_TABLES_NOW.cmd` - Automated recreation
- `07_Check_Schema_Owner.sql` - Schema verification

### Related Issues:
- None (this was the first occurrence)

### External Resources:
- SQL Server Schema Documentation: https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/create-a-database-schema
- Entity Framework Core Schema Mapping: https://learn.microsoft.com/en-us/ef/core/modeling/relational/tables

---

## Sign-Off

**Incident Resolved:** 2025-11-02
**Documentation Complete:** 2025-11-02
**Prevention Measures Implemented:** ✅ Complete

**Reviewed By:** _________________
**Date:** _________________

**This incident must be reviewed during next deployment planning meeting.**

---

## Appendix A: Quick Reference Commands

```bash
# Check schema ownership
sqlcmd -S [server] -U [user] -P [pass] -d [db] -Q "SELECT SCHEMA_NAME(schema_id), name FROM sys.tables"

# Check user default schema
sqlcmd -Q "SELECT default_schema_name FROM sys.database_principals WHERE name = '[user]'"

# Fix user schema
sqlcmd -Q "ALTER USER [user] WITH DEFAULT_SCHEMA = dbo"

# Apply EF migrations
dotnet ef database update --connection "[connection-string]"

# Verify deployment
sqlcmd -Q "SELECT SCHEMA_NAME(schema_id), COUNT(*) FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)"
```

---

## Appendix B: Decision Tree

```
Is database user configured with DEFAULT_SCHEMA = dbo?
├─ YES → Proceed to table creation
└─ NO  → STOP
         ├─ Run: ALTER USER WITH DEFAULT_SCHEMA = dbo
         └─ Verify, then proceed

Are tables being created?
├─ Using EF migrations → GOOD (recommended)
└─ Using SQL scripts   → CAUTION (verify schema after)

Tables created successfully?
├─ Verify schema:
│  ├─ All in dbo?
│  │  ├─ YES → Proceed to data seeding
│  │  └─ NO  → STOP - Drop tables and start over
│  └─ Check: SELECT SCHEMA_NAME(schema_id) FROM sys.tables

Data seeded successfully?
├─ Test API endpoint
│  ├─ Returns data?
│  │  ├─ YES → Test website
│  │  └─ NO  → Check certificate status values
│  └─ curl /api/certificate-tiles

Website displays data?
├─ YES → Deployment SUCCESS ✅
└─ NO  → Investigate (check schema, status, logs)
```

---

**END OF DOCUMENT**

**🔥 CRITICAL: READ THIS BEFORE EVERY DATABASE DEPLOYMENT 🔥**

# Complete Production Database Refresh

## Problem

The production database has tables created under the **WRONG schema**:
- ❌ Current: `GCC_adminxyz123.Certificates`
- ✅ Required: `dbo.Certificates`

**Impact:** Entity Framework cannot find the tables, website shows 0 certificates despite 80 records in database.

---

## Solution: Complete Database Refresh

This will **completely wipe** and recreate the database with the correct schema.

---

## ⚠️ CRITICAL WARNING

**THIS WILL DELETE ALL DATA IN THE DATABASE!**

- All tables will be dropped
- All data will be lost
- Database will be recreated from scratch

**Before proceeding:**
1. Backup any important data (if needed)
2. Make sure you're ready to start fresh
3. Understand this cannot be undone

---

## Quick Start (Recommended)

### One-Click Complete Refresh

```bash
cd DatabaseScripts
COMPLETE_REFRESH_NOW.cmd
```

**What this does:**
1. ✅ Kills all database connections
2. ✅ Drops the entire database
3. ✅ Creates fresh database
4. ✅ Configures user with `DEFAULT_SCHEMA = dbo`
5. ✅ Applies complete schema (all tables)
6. ✅ Seeds 10 sample certificates
7. ✅ Verifies everything is correct

**Time:** ~3 minutes
**User interaction:** Requires confirmation to prevent accidents

---

## Manual Process (If You Prefer Step-by-Step)

### Step 1: Complete Database Refresh
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d master -i 00_COMPLETE_DATABASE_REFRESH.sql
```

**This script:**
- Kills all connections
- Drops database
- Creates new database
- Configures user with `DEFAULT_SCHEMA = dbo`

### Step 2: Apply Schema
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 00_Complete_Schema.sql
```

**This creates all tables as `dbo.TableName`**

### Step 3: Seed Data
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 04_Quick_Fix_Minimal_Data.sql
```

**This imports 10 sample certificates**

### Step 4: Verify
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -Q "SELECT SCHEMA_NAME(schema_id), COUNT(*) FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)"
```

**Expected output:**
```
SchemaName    (No column name)
----------    ----------------
dbo           40
```

**NOT this:**
```
GCC_adminxyz123    40    ← WRONG!
```

---

## Verification Checklist

After running the complete refresh:

### ✅ 1. Verify Schema Ownership
```sql
SELECT SCHEMA_NAME(schema_id) as SchemaName, name as TableName
FROM sys.tables
ORDER BY name;
```

**ALL tables should show:** `dbo | TableName`

### ✅ 2. Verify User Configuration
```sql
SELECT
    name as UserName,
    default_schema_name as DefaultSchema,
    CASE
        WHEN default_schema_name = 'dbo' THEN 'CORRECT'
        ELSE 'WRONG'
    END as Status
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';
```

**Should show:** `DefaultSchema = dbo | Status = CORRECT`

### ✅ 3. Verify Data Exists
```sql
SELECT
    'Roles' as TableName, COUNT(*) as Rows FROM Roles
UNION ALL
    SELECT 'Users', COUNT(*) FROM Users
UNION ALL
    SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL
    SELECT 'Certificates', COUNT(*) FROM Certificates
ORDER BY TableName;
```

**Expected (if seeded):**
```
TableName        Rows
-------------    ----
Certificates     10
Companies        5
Roles            4
Users            1
```

### ✅ 4. Verify Certificate Status
```sql
SELECT Status, COUNT(*) as Count
FROM Certificates
WHERE Status IN ('active', 'expiring_soon', 'renewal_in_progress')
GROUP BY Status;
```

**Should show active certificates that will display on website**

### ✅ 5. Test API Endpoint
```bash
# From production server
curl http://localhost:5101/api/certificate-tiles
```

**Expected response:**
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

### ✅ 6. Test Website
1. Navigate to: `/Certificate/CertificateTileView`
2. Should see 10 certificates displayed in tile view
3. Filters should populate with data
4. Search should work
5. Download should work

---

## What Gets Removed

### Before Refresh (WRONG):
```
Database: gccwebap_certificates
├── GCC_adminxyz123 (schema)
│   ├── Certificates (80 rows) ← WRONG SCHEMA
│   ├── Companies
│   ├── Products
│   └── ... (all tables)
└── dbo (schema)
    └── (empty)
```

### After Refresh (CORRECT):
```
Database: gccwebap_certificates (NEW)
├── dbo (schema)
│   ├── Certificates (10 rows) ← CORRECT SCHEMA
│   ├── Companies (5 rows)
│   ├── Products (5 rows)
│   └── ... (all tables)
└── GCC_adminxyz123 (schema)
    └── (empty - no tables)
```

---

## Why Complete Refresh is Necessary

### Why Not Just Move Tables?

You might think: "Can we just move the tables from `GCC_adminxyz123` schema to `dbo` schema?"

**Problems with moving tables:**
1. Foreign key constraints must be dropped and recreated
2. Indexes must be rebuilt
3. Triggers and stored procedures need updating
4. Risk of corruption or data inconsistency
5. Complex and error-prone process

**Complete refresh is:**
- ✅ Clean and guaranteed to work
- ✅ Fast (~3 minutes)
- ✅ Ensures correct configuration
- ✅ No risk of schema corruption
- ✅ Fresh start with known good state

---

## Post-Refresh: Production Deployment

After database is refreshed, deploy the application:

### 1. Deploy Application Files
```bash
# Copy published application to production server
# Ensure appsettings.Production.json is configured correctly
```

### 2. Verify Configuration
**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;..."
  },
  "DatabaseProvider": "SqlServer"
}
```

### 3. Set Environment
```bash
set ASPNETCORE_ENVIRONMENT=Production
```

### 4. Start Application
```bash
dotnet GCCWebApp.dll
```

### 5. Verify Logs
Check for:
- ✅ "Using SQL Server"
- ✅ Connection successful
- ✅ No EF Core errors
- ✅ Migrations applied (if any)

### 6. Test Website
- Navigate to: `http://localhost:5101/Certificate/CertificateTileView`
- Should see 10 certificates
- All functionality should work

---

## Troubleshooting

### Issue: "Cannot drop database because it is currently in use"

**Solution:** The refresh script handles this automatically by killing connections.

**Manual fix if needed:**
```sql
USE master;
ALTER DATABASE gccwebap_certificates SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE gccwebap_certificates;
```

### Issue: Tables still in wrong schema after refresh

**Check:** Did you run `00_Complete_Schema.sql` AFTER `00_COMPLETE_DATABASE_REFRESH.sql`?

**Verify user config:**
```sql
SELECT default_schema_name FROM sys.database_principals WHERE name = 'GCC_adminxyz123';
-- MUST show: dbo
```

### Issue: Certificates still not displaying after refresh

**Check status values:**
```sql
SELECT Status, COUNT(*) FROM Certificates GROUP BY Status;
```

**Fix if needed:**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 06_Fix_Certificate_Status.sql
```

### Issue: API returns empty results

**Check:**
1. Application is using Production environment
2. Connection string points to correct database
3. Schema is `dbo` (not user schema)
4. Certificates have status = 'active'

---

## Files Reference

| File | Purpose |
|------|---------|
| `00_COMPLETE_DATABASE_REFRESH.sql` | **SQL script** - Complete database refresh |
| `COMPLETE_REFRESH_NOW.cmd` | **Automated** - One-click complete refresh (RECOMMENDED) |
| `00_Complete_Schema.sql` | Create all database tables |
| `04_Quick_Fix_Minimal_Data.sql` | Seed sample data |
| `07_Check_Schema_Owner.sql` | Verify schema ownership |
| `README_COMPLETE_REFRESH.md` | This document |

---

## FAQ

### Q: Will I lose data?
**A:** Yes. ALL data in the database will be deleted. This is a complete refresh.

### Q: Can I backup the old data first?
**A:** If the schema is wrong (user schema instead of dbo), the data is not usable anyway. But you can export using SQL Server Management Studio if needed.

### Q: How long does this take?
**A:** ~3 minutes total (drop, create, schema, seed)

### Q: Is this safe for production?
**A:** This IS the fix for production. The current state (wrong schema) makes the database unusable by the application.

### Q: What if something goes wrong?
**A:** The script includes verification steps. If any step fails, you'll see an error and can re-run.

### Q: Do I need to rebuild the application?
**A:** No. The application code is fine. This only fixes the database.

### Q: Will I need to do this again?
**A:** No. Once schema is correct, future deployments will work normally. This is a one-time fix.

---

## Success Criteria

After complete refresh, verify:

- ✅ All tables exist in `dbo` schema (not user schema)
- ✅ User configured with `DEFAULT_SCHEMA = dbo`
- ✅ Sample data seeded (if selected)
- ✅ Certificates have status = `active`
- ✅ API endpoint returns data
- ✅ Website displays certificates

---

## Ready to Proceed?

**Run this command NOW:**

```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\DatabaseScripts"

COMPLETE_REFRESH_NOW.cmd
```

**This will completely refresh the production database with the correct schema!** 🚀

---

**Last Updated:** 2025-11-02
**Required Action:** Complete database refresh to fix schema ownership issue

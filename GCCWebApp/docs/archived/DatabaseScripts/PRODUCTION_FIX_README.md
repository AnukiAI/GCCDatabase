# Production Database Fix - Certificate Display Issue

## Problem Summary

**Symptoms:**
- Production website at AccuWebHosting shows no certificates
- Database schema exists (all tables created)
- But all tables are EMPTY (0 rows)

**Root Cause:**
The seed script `DataMigration/01_Seed_Production_Data.sql` has critical flaws:
1. ❌ **Invalid Column**: Roles INSERT uses `IsDefault` column (doesn't exist, should be `IsSystem`)
2. ❌ **Missing Data**: Companies, Partners, CertificationTypes, Products, Users, Tags sections are EMPTY
3. ❌ **FK Violations**: Certificates try to reference non-existent CompanyId/ProductId/CertificationTypeId

## Solution Overview

You have 3 options (ranked by speed):

### **Option 1: Quick Fix - Minimal Data (FASTEST - 5 minutes)**
Import just enough data to get the site working and show SOME certificates.

```bash
# From DatabaseScripts folder
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 04_Quick_Fix_Minimal_Data.sql
```

This will import:
- 4 Roles (Admin, Analyst, External, Public)
- 1 Admin user
- 3 Partners (Amazon, Wayfair, Other)
- 5 Companies
- 5 CertificationTypes
- 5 Products
- 10 Sample Certificates

### **Option 2: Use Application Seeding (RECOMMENDED - 10 minutes)**
Let the ASP.NET application seed the database automatically:

```bash
# 1. Set environment to Production
$env:ASPNETCORE_ENVIRONMENT = "Production"

# 2. Run application (it will auto-seed on startup)
cd GCCWebApp
dotnet run

# The application will:
# - Detect empty database
# - Seed default Roles
# - Create admin user
# - Seed Partners
# - Import CertificationTypes from config
```

**Note**: This seeds DEFAULT data only. To import your development certificates, use Option 3.

### **Option 3: Full Data Migration (COMPLETE - 30 minutes)**
Import ALL data from development database (178 certificates, 28 companies, etc.)

```bash
# From DataMigration folder
cd ..\DataMigration

# Run migration PowerShell script
.\migrate-to-production.ps1

# OR use Python script to generate full seed SQL
python generate-seed-data.py
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i migration-data.sql
```

## Verification Steps

After running ANY of the above options:

### 1. Run Diagnostic Script
```bash
cd DatabaseScripts
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 01_Diagnose_Production.sql
```

### 2. Expected Results
**Minimal (Option 1):**
- Roles: 4
- Companies: 5
- CertificationTypes: 5
- Products: 5
- Certificates: 10

**Application Seed (Option 2):**
- Roles: 4
- Users: 1 (admin)
- Partners: 3
- CertificationTypes: 22

**Full Migration (Option 3):**
- Roles: 4
- Users: 14
- Companies: 28
- Partners: 3
- CertificationTypes: 22
- Products: 39
- Certificates: 178

### 3. Test Website
1. Navigate to: http://[your-production-url]/Certificate/CertificateTileView
2. Should see certificates displayed in tiles
3. Filters should populate with actual data
4. Search should work

## What Went Wrong?

The original `01_Seed_Production_Data.sql` was generated incorrectly:

**Bad SQL (Line 17):**
```sql
-- ❌ WRONG - IsDefault column doesn't exist in schema
INSERT INTO [Roles] ([Id], [Name], [Description], [IsActive], [IsDefault], ...)
```

**Good SQL (Fixed):**
```sql
-- ✓ CORRECT - Use IsSystem column instead
INSERT INTO [Roles] ([Id], [Name], [Description], [IsActive], [IsSystem], ...)
```

**Missing Sections (Lines 28-70):**
```sql
-- ❌ EMPTY - No INSERT statements!
SET IDENTITY_INSERT [Companies] ON;
SET IDENTITY_INSERT [Companies] OFF;
```

## Files in This Folder

| File | Purpose |
|------|---------|
| `00_Complete_Schema.sql` | Database schema (already applied ✓) |
| `01_Diagnose_Production.sql` | Check current production state |
| `02_Generate_Seed_From_CSV.ps1` | Generate seed SQL from CSV files |
| `03_Complete_Seed_Data.sql` | Generated complete seed script |
| `04_Quick_Fix_Minimal_Data.sql` | **USE THIS** for fastest fix |
| `PRODUCTION_FIX_README.md` | This file |

## Quick Start (TL;DR)

**I just want the site to work NOW:**

```bash
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\DatabaseScripts"

# Run the quick fix
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 04_Quick_Fix_Minimal_Data.sql

# Verify it worked
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -d gccwebap_certificates -i 01_Diagnose_Production.sql
```

Then go to your production website - certificates should now display!

## Need Help?

If you encounter any errors:

1. **Check connection**: `sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -Q "SELECT @@VERSION"`
2. **Check database exists**: `sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P Q3Ez4e!gE_47KNi -Q "SELECT name FROM sys.databases"`
3. **Review deployment log**: Check `DataMigration/deployment-log.txt` for previous errors
4. **Re-run diagnostics**: `01_Diagnose_Production.sql` shows current state

## Next Steps After Fix

Once data is loaded:

1. ✅ Verify website displays certificates
2. ✅ Test search and filters
3. ✅ Create test admin user and login
4. ✅ Test certificate download functionality
5. ✅ Review application logs for any runtime errors

---

**Status**: Schema ✓ | Data ✗ → **ACTION REQUIRED: Run Option 1 or 2**

# Production Database Rebuild Guide

## Overview

This guide walks you through completely rebuilding the production database from scratch, including:
1. Dropping all tables and schema
2. Recreating schema from Entity Framework migrations
3. Seeding with test data

**⚠️ WARNING**: This is a **DESTRUCTIVE** operation that will delete ALL data!

## When to Use This

✅ **Good Use Cases:**
- Production test environment needs fresh data
- Database schema is corrupted
- Testing the complete database setup process
- Fixing schema ownership issues (dbo vs username)
- Starting with a clean slate for testing

❌ **NEVER Use When:**
- Production has real customer data
- You need to preserve existing data
- You're just adding/updating a few records

## Prerequisites

### Required Files (all in `Database_Scripts/` folder)

1. **DROP_ALL_TABLES_SAFE.sql** - Safely drops all tables
2. **schema_migrations.sql** - Recreates schema from EF migrations (generated)
3. **FULL_TEST_DATA_SEED.sql** - Seeds test data (15 users, 28 companies, 43 products, 25 certificates)

### Connection Details

```
Server: 68.71.129.120,1533
Database: gccwebap_certificates
Username: GCC_adminxyz123
Password: Q3Ez4e!gE_47KNi
```

### Required Tools

- **sqlcmd** (SQL Server command-line tool)
- **dotnet CLI** (for generating migration script)

## Step-by-Step Instructions

### Step 1: Generate Fresh Schema Migration Script

**Why**: Ensures schema script matches your current Entity Framework migrations.

```bash
# Navigate to project directory
cd "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

# Generate idempotent migration script
dotnet ef migrations script --idempotent > schema_migrations_temp.sql
```

**Fix the generated script** (add required SET statements):

```bash
# Create final schema script with proper settings
echo "-- Required SET options for SQL Server
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
SET ANSI_WARNINGS ON;
GO
" > schema_migrations.sql

# Append generated schema (skip build output lines)
tail -n +4 schema_migrations_temp.sql >> schema_migrations.sql

# Clean up temp file
rm schema_migrations_temp.sql
```

### Step 2: Drop All Tables

**What it does**: Safely removes all tables, foreign keys, and migrations history.

```bash
cd Database_Scripts

sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -i DROP_ALL_TABLES_SAFE.sql
```

**Expected Output:**
```
============================================
Dropping All Tables (Safe Method)
============================================

Step 1: Dropping all foreign key constraints...
✓ All foreign key constraints dropped

Step 2: Dropping all tables...
✓ All tables dropped

Step 3: Dropping migrations history...
✓ Migrations history dropped

============================================
SUCCESS: All tables dropped
============================================
```

**Troubleshooting:**
- If you get "Cannot drop object" errors, the script will handle them automatically
- If script hangs, check network connectivity to server

### Step 3: Recreate Schema from Migrations

**What it does**: Creates all 35 tables with proper structure, indexes, and constraints.

```bash
cd ..

sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -i schema_migrations.sql
```

**Expected Output:**
- No errors (silence is success!)
- Script runs for ~30-60 seconds

**Verify tables were created:**

```bash
sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -Q "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
```

**Expected**: TableCount = 35

**Common Issues:**

1. **"CREATE INDEX failed... QUOTED_IDENTIFIER"**
   - **Cause**: Missing SET statements at beginning of script
   - **Fix**: Ensure schema_migrations.sql starts with SET ANSI_NULLS ON, etc.

2. **"Foreign key references invalid table"**
   - **Cause**: Tables being created in wrong order
   - **Fix**: Regenerate schema_migrations.sql using `dotnet ef migrations script`

3. **Only 3 tables created (AmazonApiSettings, AmazonSyncHistory, __EFMigrationsHistory)**
   - **Cause**: Idempotent script found existing migration records
   - **Fix**: Drop __EFMigrationsHistory table and re-run script

### Step 4: Seed Test Data

**What it does**: Populates database with:
- 15 test users (admin, analyst, external, etc.)
- 28 companies (ASSA ABLOY, 3M, Interface, etc.)
- 43 products
- 25 certificates
- Roles, permissions, tags, API keys

```bash
cd Database_Scripts

sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -i FULL_TEST_DATA_SEED.sql
```

**Expected Output:**
```
============================================
GCC Complete Test Data Seeding
Started at: 2025-11-17 15:54:12
============================================

1. Seeding Product Categories...
✓ 6 Product Categories created

2. Seeding Certification Types...
✓ 5 Certification Types created

3. Seeding Partners...
✓ 3 Partners created

4. Seeding Companies...
✓ 28 Companies created

5. Seeding Product Types...
✓ 19 Product Types created

6. Seeding Products...
✓ 43 Products created

7. Seeding Roles...
✓ 4 Roles created

8. Seeding Permissions...
✓ 6 Permissions created

9. Assigning permissions to roles...
✓ Permissions assigned to all roles

10. Seeding Users...
✓ 15 Users created

11. Assigning roles to users...
✓ User-Role assignments created

12. Seeding base certificates...
✓ 25 Base Certificates created

13. Skipping additional test certificates...
✓ Additional certificate generation skipped

14. Seeding Tags...
✓ 10 Tags created

15. Seeding API Keys...
✓ 3 API Keys created


============================================
SEEDING COMPLETE
============================================

Database Statistics:
EntityType         Count
------------------ -----------
ApiKeys                      3
Certificates                25
CertificationTypes           5
Companies                   28
Partners                     3
Permissions                  6
ProductCategories            6
Products                    43
ProductTypes                19
Roles                        4
Tags                        10
Users                       15

Test User Credentials:
Admin: admin@gcc.com / TestPass123!
Analyst: analyst@gcc.com / TestPass123!
External: partner@amazon.com / TestPass123!

Completed at: 2025-11-17 15:54:24
============================================
```

**Common Issues:**

1. **"Cannot insert NULL into column 'UpdatedAt' / 'IsDeleted' / 'RequireTLS'"**
   - **Cause**: Seed script missing columns that were added to schema later
   - **Fix**: Update INSERT statements to include all required columns
   - **Columns to check**:
     - Companies: `IsDeleted`, `CreatedAt`, `UpdatedAt`
     - Products: `IsDeleted`, `ProductTypeId`, `CreatedAt`, `UpdatedAt`
     - ApiKeys: `RequireTLS`, `SecurityViolationCount`, `IsTemporarilyBlocked`, `SecurityViolations`, `RequestCount`

2. **"Duplicate key violation on 'IX_Certificates_Number'"**
   - **Cause**: Running seed script multiple times without cleaning first
   - **Fix**: Run DROP_ALL_TABLES_SAFE.sql first, then start over

3. **"INSERT failed... QUOTED_IDENTIFIER"**
   - **Cause**: Missing SET statements at beginning of seed script
   - **Fix**: Ensure FULL_TEST_DATA_SEED.sql includes:
     ```sql
     SET NOCOUNT ON;
     GO
     SET QUOTED_IDENTIFIER ON;
     GO
     SET ANSI_NULLS ON;
     GO
     ```

### Step 5: Verify Rebuild Success

**Check table count:**

```bash
sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -Q "SELECT name FROM sys.tables WHERE type = 'U' ORDER BY name"
```

**Expected**: 35 tables listed

**Check data:**

```bash
sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -Q "SELECT COUNT(*) as UserCount FROM Users; SELECT COUNT(*) as CertCount FROM Certificates; SELECT COUNT(*) as ProductCount FROM Products;"
```

**Expected**:
- UserCount: 15
- CertCount: 25
- ProductCount: 43

**Test login:**

1. Navigate to https://gccwebapp.com/Account/Login
2. Login with:
   - Username: `admin`
   - Password: `TestPass123!`
3. Navigate to https://gccwebapp.com/Admin/UserManagement
4. Verify 15 users are displayed

## Complete Rebuild Script (All Steps Combined)

Create a file: `REBUILD_COMPLETE_PRODUCTION.cmd`

```batch
@echo off
REM ============================================
REM Complete Production Database Rebuild
REM ============================================
REM Run this script to rebuild database from scratch
REM ============================================

echo ============================================
echo GCC DATABASE COMPLETE REBUILD
echo ============================================
echo.
echo ⚠️  WARNING: This will DELETE ALL DATA! ⚠️
echo.
echo Server: 68.71.129.120,1533
echo Database: gccwebap_certificates
echo.
echo Press Ctrl+C to CANCEL, or
pause

SET SERVER=68.71.129.120,1533
SET DATABASE=gccwebap_certificates
SET USERNAME=GCC_adminxyz123
SET PASSWORD=Q3Ez4e!gE_47KNi

echo.
echo ============================================
echo Step 1/4: Generating fresh schema script...
echo ============================================
cd ..
dotnet ef migrations script --idempotent > schema_migrations_temp.sql

REM Add SET statements
echo -- Required SET options for SQL Server > schema_migrations.sql
echo SET ANSI_NULLS ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo SET QUOTED_IDENTIFIER ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo SET ANSI_PADDING ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo SET ANSI_WARNINGS ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo. >> schema_migrations.sql

REM Remove build output lines and append to final script
more +3 schema_migrations_temp.sql >> schema_migrations.sql
del schema_migrations_temp.sql

echo ✓ Schema script generated

echo.
echo ============================================
echo Step 2/4: Dropping all tables...
echo ============================================
cd Database_Scripts
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i DROP_ALL_TABLES_SAFE.sql

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Drop tables failed!
    pause
    exit /b 1
)

echo.
echo ============================================
echo Step 3/4: Recreating schema...
echo ============================================
cd ..
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i schema_migrations.sql

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Schema creation failed!
    pause
    exit /b 1
)

echo.
echo ============================================
echo Step 4/4: Seeding test data...
echo ============================================
cd Database_Scripts
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i FULL_TEST_DATA_SEED.sql

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Data seeding failed!
    pause
    exit /b 1
)

echo.
echo ============================================
echo ✅ REBUILD COMPLETE!
echo ============================================
echo.
echo Next Steps:
echo   1. Test login: https://gccwebapp.com/Account/Login
echo      Username: admin
echo      Password: TestPass123!
echo.
echo   2. Verify users: https://gccwebapp.com/Admin/UserManagement
echo      Should show 15 users
echo.
echo   3. Check products: https://gccwebapp.com/Product/Management
echo      Should show 43 products
echo.
pause
```

## Quick Reference Commands

### List all tables
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT name FROM sys.tables ORDER BY name"
```

### Count records in key tables
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Users' as [Table], COUNT(*) as [Count] FROM Users UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Companies', COUNT(*) FROM Companies"
```

### Check schema ownership
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT SCHEMA_NAME(schema_id) as SchemaName, name as TableName FROM sys.tables ORDER BY SchemaName, TableName"
```

**Expected**: All tables should show `dbo` as SchemaName (NOT `GCC_adminxyz123`)

## Test User Credentials

After rebuild, you can login with any of these accounts:

| Username | Email | Role | Password |
|----------|-------|------|----------|
| admin | admin@gcc.com | Admin | TestPass123! |
| analyst | analyst@gcc.com | Analyst | TestPass123! |
| external | external@company.com | External | TestPass123! |
| testuser | test@gcc.com | Public | TestPass123! |
| sarah.johnson | sarah.johnson@gcc.com | Analyst | TestPass123! |
| mike.chen | mike.chen@gcc.com | Analyst | TestPass123! |
| lisa.rodriguez | lisa.rodriguez@gcc.com | External | TestPass123! |
| david.kim | david.kim@gcc.com | External | TestPass123! |
| emma.wilson | emma.wilson@gcc.com | External | TestPass123! |
| alex.martinez | alex.martinez@gcc.com | External | TestPass123! |
| jennifer.lee | jennifer.lee@gcc.com | Analyst | TestPass123! |
| robert.taylor | robert.taylor@gcc.com | Analyst | TestPass123! |
| maria.garcia | maria.garcia@gcc.com | External | TestPass123! |
| public.user | public.user@gcc.com | Public | TestPass123! |

## Backup Before Rebuild (IMPORTANT!)

If production has ANY data you might need later:

```bash
# Backup entire database
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -Q "BACKUP DATABASE gccwebap_certificates TO DISK = 'C:\Backups\gccwebapp_backup_YYYYMMDD.bak'"
```

**Note**: You may need to adjust backup path based on server permissions.

## Rollback (If Something Goes Wrong)

If rebuild fails partway through:

1. **Re-run from Step 1** - The scripts are designed to be re-runnable
2. **Check error messages** - Look for specific column/table names in errors
3. **Fix seed script** - Update INSERT statements to include missing columns
4. **Regenerate schema** - Run `dotnet ef migrations script` again

## Related Documentation

- **Database Setup**: `docs/database/deployment/QUICK_START.md`
- **Schema Reference**: `docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md`
- **Connection Strings**: `docs/database/reference/CONNECTION_STRINGS.md`
- **Troubleshooting**: `docs/database/troubleshooting/COMMON_ISSUES.md`

## Support

If you encounter issues:

1. Check error messages in console output
2. Verify connection to database server
3. Ensure user `GCC_adminxyz123` has `db_owner` role
4. Check that `DEFAULT_SCHEMA` is set to `dbo` for the user
5. Review troubleshooting section above

---

**Last Updated**: 2025-11-17
**Tested On**: Production Test Environment (68.71.129.120,1533)
**Status**: ✅ Verified Working

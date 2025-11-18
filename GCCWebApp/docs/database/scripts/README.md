# Database Scripts Overview

**Last Updated:** November 17, 2025

This directory contains documentation for database scripts located throughout the project.

---

## Script Locations

### Rebuild Scripts

**Location:** `DB_REBUILD_PACKAGE_WITH_DATA/`

**Files:**
- `01_DROP_ALL_TABLES.sql` - Drop all existing tables
- `02_CREATE_SCHEMA.sql` - Create complete database schema (35+ tables)
- `03_SEED_DATA_WITH_CERTIFICATES.sql` - Load test data (175+ certificates)

**Usage:**
```bash
cd DB_REBUILD_PACKAGE_WITH_DATA

# Execute in order using SSMS or sqlcmd
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "01_DROP_ALL_TABLES.sql"
sqlcmd ... -i "02_CREATE_SCHEMA.sql"
sqlcmd ... -i "03_SEED_DATA_WITH_CERTIFICATES.sql"
```

**See:** [Quick Start Guide](../deployment/QUICK_START.md)

---

### Sync Scripts

**Location:** Root directory

**Files:**
- `export_dev_to_production_scripts_v2.sql` - SQL script to generate export
- `export_from_dev.cmd` - Windows batch script to export dev data
- `import_to_production.cmd` - Windows batch script to import to production
- `sync_dev_to_production_complete.cmd` - One-click full sync

**Usage:**
```bash
# Two-step process
export_from_dev.cmd          # Creates PRODUCTION_DATA_FROM_DEV_v2.sql
import_to_production.cmd     # Imports to production

# Or one-click
sync_dev_to_production_complete.cmd
```

**Output Files:**
- `PRODUCTION_DATA_FROM_DEV_v2.sql` - Generated INSERT statements
- `import_results.log` - Import execution log

**See:** [Sync Dev to Prod](../deployment/SYNC_DEV_TO_PROD.md)

---

### Diagnostic Scripts

**Location:** `DatabaseScripts/` (legacy, will be consolidated)

**Useful scripts:**

**Check Schema Ownership:**
```sql
-- 07_Check_Schema_Owner.sql
SELECT SCHEMA_NAME(schema_id) as Schema, name as TableName
FROM sys.tables
ORDER BY Schema, TableName;
```

**Check Certificate Status:**
```sql
-- 05_Check_Certificate_Status.sql
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status;
```

**Fix Certificate Status:**
```sql
-- 06_Fix_Certificate_Status.sql
UPDATE Certificates
SET Status = 'Active'
WHERE Status IN ('draft', 'pending', 'approved');
```

---

### Legacy Scripts

**Location:** `Database_Scripts/` (old, outdated)

**Status:** ⚠️ DO NOT USE - Contains outdated information

These scripts reference old database names and configurations:
- Old database name: `gccwebap_gccdb` (should be `gccwebap_certificates`)
- Old server configurations
- Outdated schema

**Use instead:**
- `DB_REBUILD_PACKAGE_WITH_DATA/` for rebuilds
- `docs/database/` for current documentation

---

## Script Usage Guidelines

### Running SQL Scripts

**Using SSMS:**
1. Open SQL Server Management Studio
2. Connect to target database
3. File → Open → Select script
4. Execute (F5)

**Using sqlcmd:**
```bash
sqlcmd -S SERVER -U USER -P "PASSWORD" -d DATABASE -i "script.sql"
```

**Using sqlcmd with output log:**
```bash
sqlcmd -S SERVER -U USER -P "PASSWORD" -d DATABASE -i "script.sql" -o "output.log"
```

### Connection Strings

**Development:**
```bash
-S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev
```

**Production:**
```bash
-S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates
```

**See:** [Connection Strings](../reference/CONNECTION_STRINGS.md)

---

## Common Script Tasks

### Complete Database Rebuild

```bash
cd DB_REBUILD_PACKAGE_WITH_DATA

# 1. Drop all tables
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "01_DROP_ALL_TABLES.sql"

# 2. Create schema
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "02_CREATE_SCHEMA.sql"

# 3. Seed data
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "03_SEED_DATA_WITH_CERTIFICATES.sql"
```

**Time:** 5-10 minutes

### Sync Dev to Production

```bash
# 1. Export from dev
export_from_dev.cmd

# 2. Import to production
import_to_production.cmd

# 3. Restart application
# (IIS recycle application pool)
```

**Time:** 10-15 minutes

### Verify Schema

```sql
-- Check user default schema
SELECT name, default_schema_name
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';
-- Must show: dbo

-- Check table schemas
SELECT SCHEMA_NAME(schema_id), COUNT(*)
FROM sys.tables
GROUP BY SCHEMA_NAME(schema_id);
-- Must show: dbo | 35+
```

### Verify Data

```sql
-- Count records
SELECT 'Certificates' as Entity, COUNT(*) as Count FROM Certificates
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Users', COUNT(*) FROM Users;

-- Check active certificates
SELECT COUNT(*) as ActiveCertificates
FROM Certificates
WHERE Status = 'Active' AND IsPublic = 1;
```

---

## Script Safety

### Before Running Destructive Scripts

**Always:**
1. Backup database
2. Verify connection string (don't run on wrong server!)
3. Read script contents first
4. Test on dev environment first

**Destructive scripts:**
- `01_DROP_ALL_TABLES.sql` - Deletes all tables
- `00_COMPLETE_DATABASE_REFRESH.sql` - Deletes entire database

### Transaction Wrapping

Most scripts use transactions:
```sql
BEGIN TRANSACTION;
-- Operations here
COMMIT;
-- Or ROLLBACK on error
```

**Manual execution:**
```sql
BEGIN TRANSACTION;

-- Run your commands

-- If successful:
COMMIT;

-- If error:
ROLLBACK;
```

---

## Troubleshooting Scripts

### "File not found"

**Check working directory:**
```bash
cd /path/to/GCCWebApp/GCCWebApp
dir DB_REBUILD_PACKAGE_WITH_DATA
```

**Use full path:**
```bash
sqlcmd ... -i "C:\Full\Path\To\Script.sql"
```

### "Syntax error"

**Ensure correct SQL Server compatibility:**
- Scripts target SQL Server 2016+
- Don't run on SQLite, PostgreSQL, etc.

**Check encoding:**
- Use UTF-8 or ANSI
- No BOM (Byte Order Mark)

### "Permission denied"

**Verify user has permissions:**
```sql
-- User must be db_owner
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
```

### Script hangs / timeout

**Increase timeout:**
```bash
sqlcmd ... -t 300  # 300 seconds timeout
```

**Check for locks:**
```sql
SELECT blocking_session_id, wait_type
FROM sys.dm_exec_requests
WHERE blocking_session_id <> 0;
```

---

## See Also

- [Quick Start Guide](../deployment/QUICK_START.md) - Using rebuild scripts
- [Sync Dev to Prod](../deployment/SYNC_DEV_TO_PROD.md) - Using sync scripts
- [Common Issues](../troubleshooting/COMMON_ISSUES.md) - Script troubleshooting
- [Connection Strings](../reference/CONNECTION_STRINGS.md) - Connection details

---

## Script Maintenance

### Updating Scripts

**Schema changes:**
```bash
# Regenerate schema from EF migrations
dotnet ef migrations script --idempotent -o Database_Scripts/SCHEMA_COMPLETE.sql
```

**Data export:**
```bash
# Update export script with new tables/columns
# Edit: export_dev_to_production_scripts_v2.sql
```

### Version Control

**Do commit:**
- SQL scripts (`.sql`)
- Batch scripts (`.cmd`)
- Documentation

**Don't commit:**
- Generated data files (`.sql` in root)
- Log files (`.log`)
- Passwords in scripts

---

**For more information, see the main [Database Documentation](../README.md)**

# GCC Database Deployment Scripts

## 🔥 CRITICAL REQUIREMENT - READ FIRST 🔥

**⚠️ SCHEMA OWNERSHIP REQUIREMENT ⚠️**

**ALL database tables MUST be created in the `dbo` schema.**

Entity Framework Core **REQUIRES** `dbo.TableName` format. If tables are created under user schema (e.g., `GCC_adminxyz123.Certificates`), the application **WILL NOT WORK** - even though data exists in the database.

**BEFORE creating ANY tables, you MUST configure the database user:**

```sql
USE gccwebap_certificates;
GO

-- CRITICAL: Set user default schema to dbo
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
GO

-- Add to db_owner role
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
GO

-- VERIFY configuration (MUST show dbo):
SELECT default_schema_name FROM sys.database_principals WHERE name = 'GCC_adminxyz123';
GO
```

**If you skip this step, all tables will be created in the wrong schema and the application will fail.**

See `LESSONS_LEARNED_SCHEMA_ISSUE.md` for complete details on why this is critical.

---

## Overview
This folder contains SQL scripts for deploying the GCC Web Application database schema.

## Files

### 00_Complete_Schema.sql
- **Description**: Idempotent script containing all database migrations
- **Purpose**: Creates complete database schema from scratch or updates existing database
- **Safe to run**: Yes - includes `IF NOT EXISTS` checks, won't drop existing data
- **Run on**: Fresh database OR existing database to apply new migrations

## Deployment Instructions

### For NEW Database (First Time Setup)

1. **Create Database**:
   ```sql
   CREATE DATABASE gccwebap_certificates;
   GO
   USE gccwebap_certificates;
   GO
   ```

2. **Create Database User** (if needed):
   ```sql
   CREATE LOGIN GCC_adminxyz123 WITH PASSWORD = 'YourSecurePassword';
   GO
   USE gccwebap_certificates;
   GO
   CREATE USER GCC_adminxyz123 FOR LOGIN GCC_adminxyz123;
   GO

   -- ⚠️ CRITICAL: Set default schema to dbo (REQUIRED for Entity Framework)
   ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
   GO

   -- Add to db_owner role
   ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
   GO

   -- ✅ VERIFY user configuration (MUST show dbo):
   SELECT
       name as UserName,
       default_schema_name as DefaultSchema,
       CASE
           WHEN default_schema_name = 'dbo' THEN '✓ CORRECT'
           ELSE '✗ WRONG - FIX BEFORE PROCEEDING'
       END as Status
   FROM sys.database_principals
   WHERE name = 'GCC_adminxyz123';
   GO
   ```

   **If Status shows WRONG, STOP and fix before continuing!**

3. **Run Schema Script**:
   - Open `00_Complete_Schema.sql` in SQL Server Management Studio (SSMS)
   - Ensure you're connected to the correct database
   - Execute the script (F5)

   OR using command line:
   ```bash
   sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -i 00_Complete_Schema.sql
   ```

4. **Verify Installation**:
   ```sql
   -- ✅ CRITICAL: Verify ALL tables are in dbo schema
   SELECT
       SCHEMA_NAME(schema_id) as SchemaName,
       COUNT(*) as TableCount,
       CASE
           WHEN SCHEMA_NAME(schema_id) = 'dbo' THEN '✓ CORRECT'
           ELSE '✗ WRONG SCHEMA - ABORT DEPLOYMENT'
       END as Status
   FROM sys.tables
   GROUP BY SCHEMA_NAME(schema_id);
   GO

   -- Expected Result: SchemaName = dbo, Status = ✓ CORRECT
   -- If you see any other schema (e.g., GCC_adminxyz123):
   -- 🛑 STOP - Tables in wrong schema - See LESSONS_LEARNED_SCHEMA_ISSUE.md

   -- Check that tables were created
   SELECT TABLE_NAME
   FROM INFORMATION_SCHEMA.TABLES
   WHERE TABLE_TYPE = 'BASE TABLE'
   ORDER BY TABLE_NAME;

   -- Check migrations history
   SELECT * FROM __EFMigrationsHistory
   ORDER BY MigrationId;
   ```

### For EXISTING Database (Update/Migration)

1. **Backup Database** (CRITICAL):
   ```sql
   BACKUP DATABASE gccwebap_certificates
   TO DISK = 'C:\Backups\gccwebap_certificates_backup_before_migration.bak'
   WITH FORMAT, NAME = 'Pre-Migration Backup';
   ```

2. **Run Schema Script**:
   ```bash
   sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password] -d gccwebap_certificates -i 00_Complete_Schema.sql
   ```

3. **Verify Update**:
   ```sql
   -- Check latest migrations
   SELECT TOP 5 * FROM __EFMigrationsHistory
   ORDER BY MigrationId DESC;
   ```

## Production Environment (AccuWebHosting)

**Server**: 68.71.129.120,1533
**Database**: gccwebap_certificates
**Username**: GCC_adminxyz123

### Connection String for Web App
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=false;Encrypt=true;"
  }
}
```

## Key Tables Created

- **Certificates** - Main certificate records
- **Companies** - Company/organization data
- **Products** - Product information
- **CertificateProductIds** - ASIN/UPC/GTIN mappings
- **Users** - User accounts
- **Roles** - Role definitions (Admin, Analyst, External)
- **UserRoles** - User-to-role mappings
- **Partners** - Partner entities (Amazon, Wayfair, Other)
- **Tags** - Certificate tagging system
- **CertificateTags** - Certificate-to-tag mappings
- **ApiKeys** - API authentication keys
- **AuditLogs** - System audit trail

## Important Notes

1. **🔥 SCHEMA REQUIREMENT (CRITICAL)**: User MUST have `DEFAULT_SCHEMA = dbo` before creating tables
   - See top of this document for configuration steps
   - See `LESSONS_LEARNED_SCHEMA_ISSUE.md` for why this is critical
   - **ALWAYS verify schema after deployment**

2. **Idempotent Script**: Can be run multiple times safely

3. **No Data Loss**: Creates tables/columns but doesn't drop existing data

4. **Migration History**: Updates `__EFMigrationsHistory` automatically

5. **Seed Data**: Application seeds default data on first run

6. **Always Backup**: Backup production database before migrations

7. **Recommended**: Use Entity Framework migrations (`dotnet ef database update`) instead of SQL scripts

## Troubleshooting

### ❌ Tables Created in Wrong Schema (Most Common Issue)

**Symptom**: Application shows 0 data despite database having records

**Check**:
```sql
SELECT SCHEMA_NAME(schema_id), name
FROM sys.tables
WHERE name = 'Certificates';
```

**If result shows anything OTHER than `dbo`:**
```
🛑 CRITICAL ISSUE - Tables in wrong schema
```

**Fix**: See `LESSONS_LEARNED_SCHEMA_ISSUE.md` for complete resolution steps

**Quick Fix**:
1. Drop all tables (use `00_DROP_TABLES_ONLY.sql`)
2. Configure user: `ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo`
3. Reapply migrations: `dotnet ef database update`

### Permission Denied
```sql
USE gccwebap_certificates;
GO
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
GO
```

### Cannot connect to remote server
- Verify firewall allows port 1533
- Test connection: `sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [password]`

### Application Shows 0 Certificates Despite Data in Database
**Causes**:
1. ❌ Tables in wrong schema (see above)
2. ❌ Certificate status not 'active' (see `06_Fix_Certificate_Status.sql`)
3. ❌ Connection string pointing to wrong database

**Diagnostic Scripts**:
- `07_Check_Schema_Owner.sql` - Check schema ownership
- `05_Check_Certificate_Status.sql` - Check certificate status values

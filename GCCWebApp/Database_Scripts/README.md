# GCC Web Application - Database Scripts

## Overview
This directory contains SQL Server migration scripts for the GCC Web Application database.

**Target Database:** MS SQL Server 2016+
**Database Name:** gccwebap_gccdb
**Server:** 68.71.129.120,1533

---

## Files

### 1. `01_GCCWebApp_Complete_Schema.sql`
- **Purpose:** Complete database schema from all migrations
- **Type:** Idempotent (can be run multiple times safely)
- **Size:** 1,150 lines
- **Generated:** October 7, 2025
- **Entity Framework Version:** 8.0.11

**What it creates:**
- 25+ tables for certificates, products, companies, users, roles, permissions
- Product ID management (100s of ASINs per certificate)
- ETL configuration and logging tables
- API key management
- Custom fields and tags
- All indexes and foreign keys
- Migration history tracking

### 2. `02_Backup_Rollback.sql`
- **Purpose:** Backup and rollback procedures
- **Usage:** Run BEFORE applying schema changes

### 3. `03_Data_Seed.sql` (Optional)
- **Purpose:** Insert default data (roles, permissions, admin user)
- **Note:** Application auto-seeds on first run, so this is optional

---

## Deployment Instructions

### Option 1: Automatic (Recommended)
Let the application run migrations automatically:

1. Deploy application files to Plesk
2. Configure connection string in `appsettings.Production.json`
3. Start application - it will create all tables automatically

### Option 2: Manual SQL Execution (For Review/Control)

#### Step 1: Review the Script
```sql
-- Open 01_GCCWebApp_Complete_Schema.sql in SQL Server Management Studio
-- Review the schema before execution
```

#### Step 2: Connect to Database
```bash
# Using sqlcmd
sqlcmd -S 68.71.129.120,1533 -U adminGCC -P 'YOUR_PASSWORD' -d gccwebap_gccdb

# Or use SQL Server Management Studio (SSMS)
```

#### Step 3: Execute Schema Script
```sql
-- In SSMS or sqlcmd
:r 01_GCCWebApp_Complete_Schema.sql
GO
```

#### Step 4: Verify Tables Created
```sql
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

Expected tables:
- Certificates
- CertificateProductIds (ASINs)
- Companies
- Products
- Users
- Roles
- Permissions
- ApiKeys
- Partners
- And 15+ more...

#### Step 5: Verify Migration History
```sql
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
```

Should show 12 migrations from `20250808131552_InitialCreate` to `20251006162626_AddCertificateImagePath`.

---

## Key Database Tables

### Core Business Tables
- **Certificates** - Main certificate records with 14-state workflow
- **CertificateProductIds** - Multiple ASINs/product IDs per certificate
- **Companies** - Certificate holders
- **Products** - Product catalog
- **CertificationTypes** - Types of certifications

### User Management
- **Users** - Application users with 2FA support
- **Roles** - Admin, Analyst, External roles
- **Permissions** - Granular permissions system
- **Partners** - Partner organizations (Amazon, Wayfair)

### API & Integration
- **ApiKeys** - API authentication tokens
- **ETLConfigurations** - Data import configurations
- **ETLLogEntries** - Import history logs

### Metadata
- **CustomFields** - Flexible custom fields per certificate type
- **Tags** - Tagging system
- **ProductCategories** - Product categorization

---

## Important Notes

### Data Types
The generated script uses these SQL Server data types:
- `INTEGER` = INT (SQL Server alias)
- `TEXT` = NVARCHAR(MAX)
- `decimal(18,4)` = DECIMAL(18,4)

SQL Server will accept these as-is.

### Indexes
The script creates:
- **Unique indexes** on Number, Email, Username, API keys
- **Performance indexes** on Status, dates, foreign keys
- **Composite indexes** for complex queries (certificate search)

### Foreign Keys
- Cascade delete enabled for dependent records
- SET NULL for audit fields (CreatedBy, UpdatedBy)

### Transaction Safety
Each migration is wrapped in:
```sql
BEGIN TRANSACTION;
-- Migration operations
COMMIT;
```

If any step fails, the entire migration rolls back.

---

## Rollback Procedures

### Full Rollback (Delete All Tables)
```sql
-- WARNING: This deletes ALL data!
-- Only use in development/test environments

-- Drop all foreign keys first
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'

-- Drop all tables
EXEC sp_MSforeachtable 'DROP TABLE ?'
```

### Rollback to Specific Migration
Not directly supported. You would need to:
1. Backup database
2. Drop all tables
3. Run migrations up to desired point

---

## Verification Queries

### Check Table Count
```sql
SELECT COUNT(*) AS TableCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
-- Expected: 25-30 tables
```

### Check Certificate Table Structure
```sql
EXEC sp_columns 'Certificates';
```

### Check Indexes
```sql
SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.is_unique,
    i.type_desc
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = 'Certificates'
ORDER BY t.name, i.name;
```

### Check Foreign Keys
```sql
SELECT
    OBJECT_NAME(f.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME(f.referenced_object_id) AS ReferencedTable,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys f
INNER JOIN sys.foreign_key_columns fc ON f.object_id = fc.constraint_object_id
WHERE OBJECT_NAME(f.parent_object_id) = 'Certificates';
```

---

## Troubleshooting

### Script Fails with "Table already exists"
The script checks for `__EFMigrationsHistory` but individual migrations don't check for existing tables.

**Solution:** Either:
- Drop all tables and re-run
- Or let the application run migrations (Option 1)

### "Cannot insert duplicate key" in __EFMigrationsHistory
Migration was partially applied.

**Solution:**
```sql
-- Check what migrations are recorded
SELECT * FROM __EFMigrationsHistory;

-- If needed, delete partial migration
DELETE FROM __EFMigrationsHistory
WHERE MigrationId = 'problematic_migration_id';
```

### Permission Denied Errors
User `adminGCC` needs these permissions:
```sql
-- Grant necessary permissions
USE gccwebap_gccdb;
ALTER ROLE db_ddladmin ADD MEMBER adminGCC;
ALTER ROLE db_datareader ADD MEMBER adminGCC;
ALTER ROLE db_datawriter ADD MEMBER adminGCC;
GO
```

---

## Database Size Estimates

**Initial Schema (Empty):** ~10 MB
**With 1,000 Certificates:** ~50 MB
**With 10,000 Certificates + 100,000 ASINs:** ~500 MB
**With 100,000 Certificates:** ~2-5 GB (depending on documents/images)

**Indexes add:** ~20-30% overhead

---

## Backup Recommendations

### Before First Deployment
```sql
-- No backup needed (empty database)
```

### Before Schema Updates
```sql
-- Full backup
BACKUP DATABASE gccwebap_gccdb
TO DISK = 'C:\Backups\gccwebap_gccdb_pre_update.bak'
WITH COMPRESSION, INIT;
```

### Regular Backups (After Going Live)
- **Full Backup:** Daily
- **Differential Backup:** Every 4 hours
- **Transaction Log Backup:** Every 15 minutes (if using Full recovery model)

---

## Performance Tuning

### After Importing Large Datasets
```sql
-- Update statistics
UPDATE STATISTICS Certificates WITH FULLSCAN;
UPDATE STATISTICS CertificateProductIds WITH FULLSCAN;

-- Rebuild indexes
ALTER INDEX ALL ON Certificates REBUILD;
ALTER INDEX ALL ON CertificateProductIds REBUILD;
```

### Monitor Index Usage
```sql
SELECT
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.dm_db_index_usage_stats s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE database_id = DB_ID()
ORDER BY user_seeks + user_scans + user_lookups DESC;
```

---

## Support

**Connection Details:**
- Server: 68.71.129.120,1533
- Database: gccwebap_gccdb
- User: adminGCC

**For Issues:**
1. Check application logs in Plesk
2. Check SQL Server error log
3. Verify connection string in appsettings.Production.json
4. Ensure firewall allows port 1533

---

**Last Updated:** October 7, 2025
**EF Core Version:** 8.0.11
**SQL Server Compatibility:** 2016+

-- =====================================================================
-- GCC Web Application - Backup and Rollback Procedures
-- =====================================================================
-- Database: gccwebap_gccdb
-- Server: 68.71.129.120,1533
-- Created: October 7, 2025
-- =====================================================================

USE gccwebap_gccdb;
GO

-- =====================================================================
-- SECTION 1: PRE-DEPLOYMENT BACKUP
-- =====================================================================

-- Full database backup before schema changes
PRINT 'Creating full database backup...';

DECLARE @BackupPath NVARCHAR(500);
DECLARE @BackupFileName NVARCHAR(500);
DECLARE @Timestamp VARCHAR(20);

-- Generate timestamp for backup file
SET @Timestamp = CONVERT(VARCHAR(20), GETDATE(), 112) + '_' +
                 REPLACE(CONVERT(VARCHAR(20), GETDATE(), 108), ':', '');

SET @BackupFileName = 'gccwebap_gccdb_backup_' + @Timestamp + '.bak';
SET @BackupPath = 'C:\Backups\' + @BackupFileName;  -- Adjust path as needed

PRINT 'Backup file: ' + @BackupPath;

-- Create backup directory if needed (requires xp_cmdshell enabled)
-- EXEC xp_cmdshell 'mkdir C:\Backups', NO_OUTPUT;

BACKUP DATABASE gccwebap_gccdb
TO DISK = @BackupPath
WITH
    COMPRESSION,
    INIT,
    NAME = 'GCC WebApp Full Backup',
    DESCRIPTION = 'Pre-deployment backup for schema update';

PRINT 'Backup completed successfully';
GO

-- =====================================================================
-- SECTION 2: VERIFY BACKUP
-- =====================================================================

PRINT 'Verifying backup...';

-- Get the latest backup information
SELECT TOP 1
    database_name,
    backup_start_date,
    backup_finish_date,
    backup_size / 1024 / 1024 AS backup_size_mb,
    compressed_backup_size / 1024 / 1024 AS compressed_size_mb,
    type_desc AS backup_type
FROM msdb.dbo.backupset
WHERE database_name = 'gccwebap_gccdb'
ORDER BY backup_start_date DESC;

PRINT 'Backup verification complete';
GO

-- =====================================================================
-- SECTION 3: CREATE ROLLBACK SCRIPT (Before Schema Changes)
-- =====================================================================

PRINT 'Generating rollback information...';

-- Save current schema state
IF OBJECT_ID('tempdb..#SchemaSnapshot') IS NOT NULL
    DROP TABLE #SchemaSnapshot;

SELECT
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
INTO #SchemaSnapshot
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';

PRINT 'Schema snapshot created';

-- Display current table count
SELECT
    COUNT(*) AS CurrentTableCount,
    GETDATE() AS SnapshotTime
FROM #SchemaSnapshot;
GO

-- =====================================================================
-- SECTION 4: ROLLBACK PROCEDURES
-- =====================================================================

-- *** WARNING: The following rollback procedures are DESTRUCTIVE ***
-- *** Only use in development/test environments or emergencies ***
-- *** ALWAYS backup before rollback ***

-- ---------------------------------------------------------------------
-- ROLLBACK OPTION 1: Complete Database Rollback (DESTRUCTIVE)
-- ---------------------------------------------------------------------
/*
-- Uncomment to execute complete rollback

PRINT 'WARNING: This will delete ALL tables and data!';
PRINT 'Press Ctrl+C to cancel within 10 seconds...';
WAITFOR DELAY '00:00:10';

USE gccwebap_gccdb;
GO

-- Disable all constraints
PRINT 'Disabling constraints...';
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Drop all foreign key constraints
PRINT 'Dropping foreign keys...';
DECLARE @DropFK NVARCHAR(MAX) = '';
SELECT @DropFK += 'ALTER TABLE [' + OBJECT_SCHEMA_NAME(parent_object_id) +
                  '].[' + OBJECT_NAME(parent_object_id) +
                  '] DROP CONSTRAINT [' + name + '];'
FROM sys.foreign_keys;
EXEC sp_executesql @DropFK;

-- Drop all tables
PRINT 'Dropping all tables...';
EXEC sp_MSforeachtable 'DROP TABLE ?';

PRINT 'Complete rollback finished';
PRINT 'Database is now empty and ready for fresh schema deployment';
GO
*/

-- ---------------------------------------------------------------------
-- ROLLBACK OPTION 2: Restore from Backup (SAFE)
-- ---------------------------------------------------------------------
/*
-- Uncomment and modify to restore from backup

USE master;
GO

-- Set database to single user mode
ALTER DATABASE gccwebap_gccdb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Restore from backup
RESTORE DATABASE gccwebap_gccdb
FROM DISK = 'C:\Backups\gccwebap_gccdb_backup_YYYYMMDD_HHMMSS.bak'
WITH REPLACE, RECOVERY;

-- Set database back to multi-user
ALTER DATABASE gccwebap_gccdb SET MULTI_USER;

PRINT 'Database restored from backup successfully';
GO
*/

-- ---------------------------------------------------------------------
-- ROLLBACK OPTION 3: Drop Specific Migration (Selective)
-- ---------------------------------------------------------------------
/*
-- Uncomment to remove a specific migration from history
-- Use this if a migration partially completed

USE gccwebap_gccdb;
GO

-- View current migrations
SELECT
    MigrationId,
    ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;

-- Remove specific migration (replace with actual MigrationId)
DELETE FROM __EFMigrationsHistory
WHERE MigrationId = '20251006162626_AddCertificateImagePath';

PRINT 'Migration removed from history';
PRINT 'Application will attempt to re-run this migration on next startup';
GO
*/

-- =====================================================================
-- SECTION 5: POST-DEPLOYMENT VERIFICATION
-- =====================================================================

PRINT 'Post-deployment verification queries...';
PRINT 'Run these AFTER schema deployment to verify success:';
PRINT '';
PRINT '-- 1. Check table count';
PRINT 'SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = ''BASE TABLE'';';
PRINT '';
PRINT '-- 2. Check migration history';
PRINT 'SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;';
PRINT '';
PRINT '-- 3. Check key tables exist';
PRINT 'SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN (''Certificates'', ''CertificateProductIds'', ''Users'', ''Companies'', ''Products'') ORDER BY TABLE_NAME;';
PRINT '';
PRINT '-- 4. Check indexes on Certificates table';
PRINT 'SELECT name, type_desc, is_unique FROM sys.indexes WHERE object_id = OBJECT_ID(''Certificates'') ORDER BY name;';
GO

-- =====================================================================
-- SECTION 6: EMERGENCY CONTACT INFO
-- =====================================================================

PRINT '';
PRINT '========================================';
PRINT 'EMERGENCY ROLLBACK CONTACT INFO';
PRINT '========================================';
PRINT 'Database: gccwebap_gccdb';
PRINT 'Server: 68.71.129.120,1533';
PRINT 'User: adminGCC';
PRINT '';
PRINT 'Backup Location: C:\Backups\';
PRINT 'Log Location: C:\Program Files\Microsoft SQL Server\....\LOG\';
PRINT '';
PRINT 'If deployment fails:';
PRINT '1. Check SQL Server error log';
PRINT '2. Review __EFMigrationsHistory table';
PRINT '3. Restore from most recent backup (see ROLLBACK OPTION 2)';
PRINT '4. Contact database administrator';
PRINT '========================================';
GO

-- =====================================================================
-- SECTION 7: UTILITY FUNCTIONS
-- =====================================================================

-- Function to check database size
PRINT 'Checking database size...';

SELECT
    DB_NAME() AS DatabaseName,
    SUM(CAST(size AS BIGINT) * 8 / 1024) AS SizeMB,
    SUM(CAST(size AS BIGINT) * 8 / 1024 / 1024) AS SizeGB,
    GETDATE() AS CheckTime
FROM sys.database_files;
GO

-- Function to check connection count
PRINT 'Checking active connections...';

SELECT
    DB_NAME(database_id) AS DatabaseName,
    COUNT(*) AS ConnectionCount
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('gccwebap_gccdb')
GROUP BY database_id;
GO

-- Function to check last backup
PRINT 'Checking last backup...';

SELECT TOP 1
    database_name,
    backup_start_date,
    backup_finish_date,
    DATEDIFF(HOUR, backup_finish_date, GETDATE()) AS HoursSinceBackup,
    type_desc AS BackupType,
    backup_size / 1024 / 1024 AS BackupSizeMB
FROM msdb.dbo.backupset
WHERE database_name = 'gccwebap_gccdb'
ORDER BY backup_start_date DESC;
GO

PRINT '';
PRINT '========================================';
PRINT 'Backup and Rollback Script Complete';
PRINT 'Review output above before proceeding';
PRINT '========================================';
GO

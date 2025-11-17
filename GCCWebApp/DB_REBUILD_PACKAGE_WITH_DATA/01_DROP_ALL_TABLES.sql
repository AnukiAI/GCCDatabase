-- ============================================
-- DROP ALL TABLES - SQL Server Clean Start
-- ============================================
-- Run this FIRST to clean your database
-- WARNING: This deletes ALL data and tables!
-- ============================================

USE gccwebap_certificates;
GO

PRINT '========================================';
PRINT 'Starting database cleanup...';
PRINT '========================================';
PRINT '';

-- Disable all constraints
PRINT 'Step 1: Disabling all foreign key constraints...';
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
PRINT 'Done.';
PRINT '';

-- Drop all foreign key constraints
PRINT 'Step 2: Dropping all foreign key constraints...';
DECLARE @dropFK NVARCHAR(MAX) = N'';
SELECT @dropFK += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
    + '.' + QUOTENAME(OBJECT_NAME(parent_object_id))
    + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;

IF LEN(@dropFK) > 0
BEGIN
    EXEC sp_executesql @dropFK;
    PRINT 'All foreign keys dropped.';
END
ELSE
    PRINT 'No foreign keys found.';
PRINT '';

-- Drop all tables
PRINT 'Step 3: Dropping all tables...';
DECLARE @dropTables NVARCHAR(MAX) = N'';
SELECT @dropTables += 'DROP TABLE ' + QUOTENAME(TABLE_SCHEMA) + '.' 
    + QUOTENAME(TABLE_NAME) + ';' + CHAR(13)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

IF LEN(@dropTables) > 0
BEGIN
    EXEC sp_executesql @dropTables;
    PRINT 'All tables dropped.';
END
ELSE
    PRINT 'No tables found.';
PRINT '';

-- Verify cleanup
PRINT '========================================';
PRINT 'Verification: Remaining tables';
PRINT '========================================';
SELECT COUNT(*) AS RemainingTables 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';

PRINT '';
PRINT '========================================';
PRINT 'Database cleanup complete!';
PRINT 'Database is now empty and ready for schema creation.';
PRINT '========================================';
GO

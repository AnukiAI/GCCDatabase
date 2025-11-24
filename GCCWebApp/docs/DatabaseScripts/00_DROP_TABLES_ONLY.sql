-- =========================================
-- DROP ALL TABLES (Keep Database)
-- =========================================
-- This script will:
-- 1. Drop all existing tables
-- 2. Configure user with dbo schema
-- 3. Ready for schema redeployment
-- =========================================

PRINT '=========================================';
PRINT 'DROP ALL TABLES - KEEP DATABASE';
PRINT '=========================================';
PRINT '';
PRINT 'This will DROP all tables but KEEP the database';
PRINT '';

USE gccwebap_certificates;
GO

PRINT '';
PRINT 'Step 1: Dropping all foreign key constraints...';
PRINT '-----------------------------------------';

-- Drop all foreign key constraints first
DECLARE @SQL NVARCHAR(MAX) = N'';

SELECT @SQL = @SQL +
    'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
    ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys;

IF @SQL != ''
BEGIN
    EXEC sp_executesql @SQL;
    PRINT '✓ All foreign key constraints dropped';
END
ELSE
BEGIN
    PRINT '✓ No foreign key constraints found';
END
GO

PRINT '';
PRINT 'Step 2: Dropping all tables...';
PRINT '-----------------------------------------';

-- Drop all tables
DECLARE @SQL NVARCHAR(MAX) = N'';

SELECT @SQL = @SQL +
    'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.tables;

IF @SQL != ''
BEGIN
    EXEC sp_executesql @SQL;
    PRINT '✓ All tables dropped';
END
ELSE
BEGIN
    PRINT '✓ No tables found';
END
GO

PRINT '';
PRINT 'Step 3: Configuring user with dbo schema...';
PRINT '-----------------------------------------';

-- Configure user with dbo schema
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'GCC_adminxyz123')
BEGIN
    ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
    PRINT '✓ User default schema set to dbo';
END
ELSE
BEGIN
    -- Create user if it doesn't exist
    CREATE USER GCC_adminxyz123 FOR LOGIN GCC_adminxyz123;
    ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
    PRINT '✓ User created and default schema set to dbo';
END
GO

-- Add to db_owner role
IF IS_ROLEMEMBER('db_owner', 'GCC_adminxyz123') = 0
BEGIN
    ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
    PRINT '✓ User added to db_owner role';
END
ELSE
BEGIN
    PRINT '✓ User already in db_owner role';
END
GO

PRINT '';
PRINT 'Step 4: Verifying configuration...';
PRINT '-----------------------------------------';

-- Verify configuration
SELECT
    'User: ' + dp.name as Configuration,
    'Default Schema: ' + dp.default_schema_name as Setting,
    CASE
        WHEN dp.default_schema_name = 'dbo' THEN '✓ CORRECT'
        ELSE '✗ WRONG - Should be dbo'
    END as Status
FROM sys.database_principals dp
WHERE dp.name = 'GCC_adminxyz123';

-- Verify no tables exist
SELECT
    'Table count: ' + CAST(COUNT(*) AS NVARCHAR(10)) as TableCheck,
    CASE
        WHEN COUNT(*) = 0 THEN '✓ Database is clean'
        ELSE '✗ Tables still exist (unexpected)'
    END as Status
FROM sys.tables;

PRINT '';
PRINT '=========================================';
PRINT 'TABLES DROPPED - DATABASE READY';
PRINT '=========================================';
PRINT '';
PRINT 'Database Configuration:';
PRINT '  ✓ Database: gccwebap_certificates (still exists)';
PRINT '  ✓ User: GCC_adminxyz123';
PRINT '  ✓ Default Schema: dbo';
PRINT '  ✓ Role: db_owner';
PRINT '  ✓ Tables: 0 (all removed)';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Run: 00_Complete_Schema.sql';
PRINT '  2. Run: 04_Quick_Fix_Minimal_Data.sql';
PRINT '  3. Verify tables are in dbo schema';
PRINT '';
PRINT 'All new tables will be created as dbo.TableName!';
PRINT '=========================================';
GO

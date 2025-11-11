-- =========================================
-- COMPLETE DATABASE REFRESH
-- =========================================
-- This script will:
-- 1. KILL all connections to the database
-- 2. DROP the database completely
-- 3. CREATE a fresh database
-- 4. Configure the user with dbo schema
-- 5. Ready for schema deployment
-- =========================================

PRINT '========================================='
PRINT 'GCC PRODUCTION DATABASE - COMPLETE REFRESH'
PRINT '========================================='
PRINT ''
PRINT 'This will COMPLETELY DESTROY the existing database!'
PRINT 'ALL DATA WILL BE LOST!'
PRINT ''
PRINT 'Waiting 5 seconds... Press Ctrl+C to cancel'
GO

WAITFOR DELAY '00:00:05';
GO

USE master;
GO

DECLARE @DatabaseName NVARCHAR(128) = N'gccwebap_certificates';
DECLARE @SQL NVARCHAR(MAX);

PRINT ''
PRINT 'Step 1: Killing all connections to database...'
PRINT '-----------------------------------------'

-- Kill all connections
IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName)
BEGIN
    SET @SQL = N'
    DECLARE @kill varchar(8000) = '''';
    SELECT @kill = @kill + ''KILL '' + CONVERT(varchar(5), session_id) + '';''
    FROM sys.dm_exec_sessions
    WHERE database_id = DB_ID(''' + @DatabaseName + ''')
    AND session_id != @@SPID;

    EXEC(@kill);
    ';

    EXEC sp_executesql @SQL;
    PRINT '✓ All connections killed';
END
ELSE
BEGIN
    PRINT '✓ Database does not exist yet';
END
GO

PRINT ''
PRINT 'Step 2: Dropping existing database...'
PRINT '-----------------------------------------'

DECLARE @DatabaseName NVARCHAR(128) = N'gccwebap_certificates';

IF EXISTS (SELECT name FROM sys.databases WHERE name = @DatabaseName)
BEGIN
    -- Set to single user to force drop
    DECLARE @SQL NVARCHAR(MAX);
    SET @SQL = N'ALTER DATABASE [' + @DatabaseName + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
    EXEC sp_executesql @SQL;

    -- Drop the database
    SET @SQL = N'DROP DATABASE [' + @DatabaseName + N'];';
    EXEC sp_executesql @SQL;

    PRINT '✓ Database dropped successfully';
END
ELSE
BEGIN
    PRINT '✓ Database did not exist';
END
GO

PRINT ''
PRINT 'Step 3: Creating fresh database...'
PRINT '-----------------------------------------'

CREATE DATABASE gccwebap_certificates
COLLATE SQL_Latin1_General_CP1_CI_AS;

PRINT '✓ Database created';
GO

PRINT ''
PRINT 'Step 4: Configuring database user...'
PRINT '-----------------------------------------'

USE gccwebap_certificates;
GO

-- Create user from login if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'GCC_adminxyz123')
BEGIN
    CREATE USER GCC_adminxyz123 FOR LOGIN GCC_adminxyz123;
    PRINT '✓ User created from login';
END
ELSE
BEGIN
    PRINT '✓ User already exists';
END
GO

-- Set default schema to dbo (CRITICAL!)
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
PRINT '✓ Default schema set to dbo';
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

PRINT ''
PRINT 'Step 5: Verifying configuration...'
PRINT '-----------------------------------------'

SELECT
    'User: ' + dp.name as Configuration,
    'Default Schema: ' + dp.default_schema_name as Setting,
    CASE
        WHEN dp.default_schema_name = 'dbo' THEN '✓ CORRECT'
        ELSE '✗ WRONG - Should be dbo'
    END as Status
FROM sys.database_principals dp
WHERE dp.name = 'GCC_adminxyz123';

SELECT
    'db_owner membership: ' +
    CASE
        WHEN IS_ROLEMEMBER('db_owner', 'GCC_adminxyz123') = 1 THEN '✓ YES'
        ELSE '✗ NO'
    END as RoleStatus;

-- Check for any existing tables (should be 0)
SELECT
    'Table count: ' + CAST(COUNT(*) AS NVARCHAR(10)) as TableCheck,
    CASE
        WHEN COUNT(*) = 0 THEN '✓ Database is clean'
        ELSE '✗ Tables already exist (unexpected)'
    END as Status
FROM sys.tables;

PRINT ''
PRINT '========================================='
PRINT 'DATABASE REFRESH COMPLETE!'
PRINT '========================================='
PRINT ''
PRINT 'Database Configuration:'
PRINT '  ✓ Database: gccwebap_certificates'
PRINT '  ✓ User: GCC_adminxyz123'
PRINT '  ✓ Default Schema: dbo'
PRINT '  ✓ Role: db_owner'
PRINT '  ✓ Tables: 0 (clean database)'
PRINT ''
PRINT 'Next Steps:'
PRINT '  1. Run: 00_Complete_Schema.sql'
PRINT '  2. Run: 04_Quick_Fix_Minimal_Data.sql'
PRINT '  3. Verify tables are in dbo schema'
PRINT ''
PRINT 'All tables will now be created as dbo.TableName!'
PRINT '========================================='
GO

-- =========================================
-- REBUILD PRODUCTION DATABASE WITH DBO SCHEMA
-- =========================================
-- CRITICAL: This drops and recreates the database
-- BACKUP your data before running this!
-- =========================================

USE master;
GO

PRINT '========================================='
PRINT 'GCC DATABASE REBUILD - DBO SCHEMA FIX'
PRINT '========================================='
PRINT ''
PRINT 'WARNING: This will DROP and RECREATE the database'
PRINT 'Make sure you have backed up any important data!'
PRINT ''
PRINT 'Press Ctrl+C to cancel within 10 seconds...'
GO

-- Wait 10 seconds
WAITFOR DELAY '00:00:10';
GO

PRINT 'Proceeding with rebuild...'
PRINT ''
GO

-- =========================================
-- STEP 1: Drop existing database if it exists
-- =========================================
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'gccwebap_certificates')
BEGIN
    PRINT 'Dropping existing database...'

    -- Kill all connections first
    ALTER DATABASE gccwebap_certificates SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

    DROP DATABASE gccwebap_certificates;
    PRINT '✓ Database dropped'
END
GO

-- =========================================
-- STEP 2: Create fresh database
-- =========================================
PRINT 'Creating new database...'
CREATE DATABASE gccwebap_certificates
COLLATE SQL_Latin1_General_CP1_CI_AS;
GO

PRINT '✓ Database created'
PRINT ''
GO

USE gccwebap_certificates;
GO

-- =========================================
-- STEP 3: Configure user with dbo schema
-- =========================================
PRINT 'Configuring database user...'

-- Create user from login
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'GCC_adminxyz123')
BEGIN
    CREATE USER GCC_adminxyz123 FOR LOGIN GCC_adminxyz123;
    PRINT '✓ User created'
END

-- Make user db_owner (this ensures dbo schema by default)
ALTER ROLE db_owner ADD MEMBER GCC_adminxyz123;
PRINT '✓ User added to db_owner role'

-- CRITICAL: Set default schema to dbo
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
PRINT '✓ Default schema set to dbo'
GO

-- =========================================
-- STEP 4: Verify Configuration
-- =========================================
PRINT ''
PRINT 'Verifying configuration...'

SELECT
    'User: ' + name as Info,
    'Default Schema: ' + default_schema_name as Schema,
    CASE
        WHEN default_schema_name = 'dbo' THEN '✓ CORRECT'
        ELSE '✗ WRONG'
    END as Status
FROM sys.database_principals
WHERE name = 'GCC_adminxyz123';

-- Check db_owner membership
IF IS_ROLEMEMBER('db_owner', 'GCC_adminxyz123') = 1
    PRINT '✓ User is db_owner'
ELSE
    PRINT '✗ User is NOT db_owner (PROBLEM!)'
GO

PRINT ''
PRINT '========================================='
PRINT 'Database rebuild complete!'
PRINT '========================================='
PRINT ''
PRINT 'Next steps:'
PRINT '  1. Run: 00_Complete_Schema.sql (create tables)'
PRINT '  2. Run: 04_Quick_Fix_Minimal_Data.sql (seed data)'
PRINT '  3. Verify tables are in dbo schema'
PRINT ''
PRINT 'IMPORTANT: All tables will now be created as dbo.TableName'
PRINT 'Entity Framework will be able to find them!'
PRINT '========================================='
GO

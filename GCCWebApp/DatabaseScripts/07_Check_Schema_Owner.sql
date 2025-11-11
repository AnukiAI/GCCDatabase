-- =========================================
-- Check Schema Ownership in Production
-- =========================================
-- This will show if tables are under wrong schema
-- (should be dbo, not username)
-- =========================================

USE gccwebap_certificates;
GO

PRINT '========================================='
PRINT 'DATABASE SCHEMA ANALYSIS'
PRINT '========================================='
PRINT ''

-- 1. Show all schemas in database
PRINT 'All Schemas in Database:'
PRINT '-----------------------------------------'
SELECT
    s.name as SchemaName,
    COUNT(t.name) as TableCount,
    CASE
        WHEN s.name = 'dbo' THEN 'CORRECT'
        ELSE 'WRONG - Should be dbo'
    END as Status
FROM sys.schemas s
LEFT JOIN sys.tables t ON s.schema_id = t.schema_id
GROUP BY s.name
ORDER BY COUNT(t.name) DESC;

PRINT ''
PRINT '========================================='
PRINT 'Tables by Schema:'
PRINT '-----------------------------------------'
SELECT
    SCHEMA_NAME(schema_id) as SchemaName,
    name as TableName,
    CASE
        WHEN SCHEMA_NAME(schema_id) = 'dbo' THEN 'OK'
        ELSE 'WRONG SCHEMA!'
    END as Status
FROM sys.tables
ORDER BY SCHEMA_NAME(schema_id), name;

PRINT ''
PRINT '========================================='
PRINT 'DIAGNOSIS:'
PRINT '-----------------------------------------'
PRINT 'Tables MUST be in "dbo" schema for Entity Framework to work.'
PRINT ''
PRINT 'If tables are under user schema (like GCC_adminxyz123),'
PRINT 'the application CANNOT find them.'
PRINT ''
PRINT 'This happens when:'
PRINT '  1. User does not have dbo as default schema'
PRINT '  2. Tables created without explicit dbo. prefix'
PRINT '  3. User is not db_owner role member'
PRINT ''
PRINT 'FIX: Rebuild database with proper schema configuration'
PRINT '========================================='
GO

-- Show default schema for user
PRINT ''
PRINT 'Default Schema for Current User:'
SELECT
    name as UserName,
    default_schema_name as DefaultSchema,
    CASE
        WHEN default_schema_name = 'dbo' THEN 'CORRECT'
        ELSE 'WRONG - Should be dbo'
    END as Status
FROM sys.database_principals
WHERE name = USER_NAME();
GO

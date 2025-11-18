-- VERIFY PRODUCTION DATABASE SCHEMA

-- 1. Check ALL tables with their schema
SELECT
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS TableName
FROM sys.tables
ORDER BY SchemaName, TableName;

-- 2. Specifically look for UserRoles in ANY schema
SELECT
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS TableName
FROM sys.tables
WHERE name LIKE '%UserRole%' OR name LIKE '%User%' OR name LIKE '%Role%';

-- 3. Check migration history
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;

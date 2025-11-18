-- CHECK WHAT TABLES EXIST IN PRODUCTION DATABASE

-- 1. List all tables
SELECT
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS TableName,
    create_date AS CreatedDate
FROM sys.tables
ORDER BY SchemaName, TableName;

-- 2. Check if critical authentication tables exist
SELECT
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
        THEN 'EXISTS' ELSE 'MISSING' END AS Users_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles')
        THEN 'EXISTS' ELSE 'MISSING' END AS Roles_Table,
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserRoles')
        THEN 'EXISTS' ELSE 'MISSING' END AS UserRoles_Table;

-- 3. If Roles table exists, show what roles are defined
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles')
BEGIN
    SELECT * FROM Roles ORDER BY Id;
END
ELSE
BEGIN
    SELECT 'Roles table does not exist' AS ErrorMessage;
END

-- 4. Check which migration was last applied
SELECT TOP 10
    MigrationId,
    ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId DESC;

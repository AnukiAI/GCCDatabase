-- Check UserRoles table schema
USE gccwebap_certificates;
GO

-- Get column names
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'UserRoles'
ORDER BY ORDINAL_POSITION;
GO

-- Show sample data
SELECT TOP 5 * FROM UserRoles;
GO

-- ============================================
-- GCC Production Database Validation Script
-- Server: 68.71.129.120,1533
-- Database: gccwebap_certificates
-- User: GCC_adminxyz123
-- ============================================
-- Run this script after migrations to validate database state
-- All checks should pass for successful deployment
-- ============================================

PRINT '============================================';
PRINT 'GCC Production Database Validation';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

-- ============================================
-- 1. DATABASE CONNECTION TEST
-- ============================================
PRINT '1. Testing database connection...';
SELECT
    DB_NAME() AS DatabaseName,
    @@VERSION AS SQLVersion,
    GETDATE() AS CurrentTime;
PRINT '✓ Connection successful';
PRINT '';

-- ============================================
-- 2. TABLE COUNT VERIFICATION
-- ============================================
PRINT '2. Verifying table count...';
DECLARE @TableCount INT;
SELECT @TableCount = COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';

PRINT 'Total tables: ' + CAST(@TableCount AS VARCHAR);
IF @TableCount >= 25
    PRINT '✓ Table count is good (expected 25+)';
ELSE
    PRINT '✗ WARNING: Expected at least 25 tables, found ' + CAST(@TableCount AS VARCHAR);
PRINT '';

-- ============================================
-- 3. MIGRATION HISTORY CHECK
-- ============================================
PRINT '3. Checking migration history...';
SELECT
    MigrationId,
    ProductVersion,
    CASE
        WHEN MigrationId = '20251014002347_FixCompanyNameIndexForSqlServer'
        THEN '✓ SQL Server Fix Applied'
        ELSE ''
    END AS Note
FROM __EFMigrationsHistory
ORDER BY MigrationId;

DECLARE @MigrationCount INT;
SELECT @MigrationCount = COUNT(*) FROM __EFMigrationsHistory;
PRINT 'Total migrations applied: ' + CAST(@MigrationCount AS VARCHAR);
IF @MigrationCount >= 13
    PRINT '✓ All migrations applied';
ELSE
    PRINT '✗ WARNING: Expected at least 13 migrations';
PRINT '';

-- ============================================
-- 4. ROW COUNT VERIFICATION
-- ============================================
PRINT '4. Verifying row counts...';
SELECT
    'Certificates' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END AS Status
FROM Certificates
UNION ALL
SELECT 'Users', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM Users
UNION ALL
SELECT 'Companies', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM Companies
UNION ALL
SELECT 'Products', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM Products
UNION ALL
SELECT 'ProductCategories', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM ProductCategories
UNION ALL
SELECT 'CertificationTypes', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM CertificationTypes
UNION ALL
SELECT 'Partners', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM Partners
UNION ALL
SELECT 'Roles', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM Roles
UNION ALL
SELECT 'Permissions', COUNT(*),
    CASE WHEN COUNT(*) >= 1 THEN '✓' ELSE '✗' END FROM Permissions
ORDER BY TableName;
PRINT '';

-- ============================================
-- 5. CRITICAL INDEXES VERIFICATION
-- ============================================
PRINT '5. Verifying critical unique indexes...';
SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    i.name AS IndexName,
    CASE
        WHEN ty.name = 'nvarchar' AND c.max_length = -1 THEN '✗ MAX LENGTH - WILL FAIL!'
        WHEN ty.name = 'nvarchar' AND c.max_length > 0 THEN '✓ Fixed'
        ELSE '✓ OK'
    END AS Status
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE i.is_unique = 1
  AND t.is_ms_shipped = 0
  AND c.name IN ('Name', 'Username', 'Email', 'Codename', 'KeyHash', 'Number')
ORDER BY t.name, c.name;
PRINT '';

-- ============================================
-- 6. DATA INTEGRITY CHECKS
-- ============================================
PRINT '6. Checking data integrity...';

-- Orphaned certificates check
DECLARE @OrphanedCerts INT;
SELECT @OrphanedCerts = COUNT(*)
FROM Certificates c
WHERE NOT EXISTS (SELECT 1 FROM Companies co WHERE co.Id = c.CompanyId)
   OR NOT EXISTS (SELECT 1 FROM Products p WHERE p.Id = c.ProductId);

PRINT 'Orphaned certificates: ' + CAST(@OrphanedCerts AS VARCHAR);
IF @OrphanedCerts = 0
    PRINT '✓ No orphaned certificates';
ELSE
    PRINT '✗ WARNING: Found ' + CAST(@OrphanedCerts AS VARCHAR) + ' orphaned certificates';

-- Invalid date checks
DECLARE @InvalidDates INT;
SELECT @InvalidDates = COUNT(*)
FROM Certificates
WHERE ExpirationDate <= EffectiveDate;

PRINT 'Invalid expiration dates: ' + CAST(@InvalidDates AS VARCHAR);
IF @InvalidDates = 0
    PRINT '✓ All dates are valid';
ELSE
    PRINT '✗ WARNING: Found ' + CAST(@InvalidDates AS VARCHAR) + ' certificates with invalid dates';
PRINT '';

-- ============================================
-- 7. FOREIGN KEY VERIFICATION
-- ============================================
PRINT '7. Verifying foreign key constraints...';
SELECT
    COUNT(*) AS ForeignKeyCount,
    CASE WHEN COUNT(*) >= 20 THEN '✓ Foreign keys exist' ELSE '✗ Missing FKs' END AS Status
FROM sys.foreign_keys
WHERE is_disabled = 0;
PRINT '';

-- ============================================
-- 8. USER AND ROLE VERIFICATION
-- ============================================
PRINT '8. Verifying users and roles...';
SELECT
    r.Name AS RoleName,
    COUNT(ru.UserId) AS UserCount,
    CASE WHEN COUNT(ru.UserId) >= 1 THEN '✓' ELSE '✗' END AS Status
FROM Roles r
LEFT JOIN RoleUser ru ON r.Id = ru.RolesId
GROUP BY r.Id, r.Name
ORDER BY r.Name;
PRINT '';

-- Admin user check
PRINT 'Checking for admin user...';
IF EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@gcc.com' OR Email = 'admin@greencirclecertified.com')
    PRINT '✓ Admin user exists';
ELSE
    PRINT '✗ WARNING: No admin user found!';
PRINT '';

-- ============================================
-- 9. CERTIFICATE STATUS BREAKDOWN
-- ============================================
PRINT '9. Certificate status breakdown:';
SELECT
    Status,
    COUNT(*) AS Count,
    CASE WHEN COUNT(*) > 0 THEN '✓' ELSE '○' END AS HasData
FROM Certificates
GROUP BY Status
ORDER BY Status;
PRINT '';

-- ============================================
-- 10. DATABASE SIZE AND PERFORMANCE
-- ============================================
PRINT '10. Database size and performance:';
SELECT
    DB_NAME() AS DatabaseName,
    SUM(CAST(size AS BIGINT) * 8) / 1024 AS SizeMB,
    CASE
        WHEN SUM(CAST(size AS BIGINT) * 8) / 1024 < 2048 THEN '✓ Size OK'
        ELSE '⚠ Large database'
    END AS Status
FROM sys.database_files;
PRINT '';

-- ============================================
-- 11. FINAL SUMMARY
-- ============================================
PRINT '============================================';
PRINT 'VALIDATION COMPLETE';
PRINT '============================================';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';
PRINT 'Check the results above:';
PRINT '- All ✓ marks indicate passing tests';
PRINT '- Any ✗ marks indicate issues that need attention';
PRINT '- Any ⚠ marks indicate warnings';
PRINT '';
PRINT 'If all critical tests pass, database is ready for use!';
PRINT '============================================';

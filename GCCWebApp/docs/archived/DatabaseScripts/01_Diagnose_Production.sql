-- =========================================
-- GCC Production Database Diagnostic Script
-- Run this to check current production data state
-- =========================================

USE gccwebap_certificates;
GO

PRINT '========================================='
PRINT 'GCC PRODUCTION DATABASE DIAGNOSTIC'
PRINT '========================================='
PRINT ''

-- Check table row counts
PRINT 'TABLE ROW COUNTS:'
PRINT '-----------------------------------------'
SELECT 'Roles' as TableName, COUNT(*) as RowCount FROM Roles
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL
SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL
SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL
SELECT 'ProductCategories', COUNT(*) FROM ProductCategories
UNION ALL
SELECT 'ProductTypes', COUNT(*) FROM ProductTypes
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL
SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL
SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds
ORDER BY TableName;

PRINT ''
PRINT '=========================================';
PRINT 'MIGRATION HISTORY:'
PRINT '-----------------------------------------'
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;

PRINT ''
PRINT '=========================================';
PRINT 'SCHEMA CHECK - Missing Columns:'
PRINT '-----------------------------------------'

-- Check if Roles table has IsDefault column (it shouldn't)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Roles' AND COLUMN_NAME = 'IsDefault')
BEGIN
    PRINT '✓ Roles.IsDefault column does NOT exist (correct - this was the error)'
END
ELSE
BEGIN
    PRINT '✗ Roles.IsDefault column EXISTS (unexpected)'
END

-- Check if Roles table has IsSystem column (it should)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME = 'Roles' AND COLUMN_NAME = 'IsSystem')
BEGIN
    PRINT '✓ Roles.IsSystem column exists (correct)'
END
ELSE
BEGIN
    PRINT '✗ Roles.IsSystem column is MISSING (problem)'
END

PRINT ''
PRINT '=========================================';
PRINT 'SAMPLE DATA CHECK:'
PRINT '-----------------------------------------'

IF (SELECT COUNT(*) FROM Roles) > 0
BEGIN
    PRINT 'Sample Roles:'
    SELECT TOP 5 Id, Name, IsSystem, IsActive FROM Roles
END
ELSE
BEGIN
    PRINT '⚠ No Roles found in database'
END

IF (SELECT COUNT(*) FROM Companies) > 0
BEGIN
    PRINT 'Sample Companies:'
    SELECT TOP 5 Id, Name, Country FROM Companies
END
ELSE
BEGIN
    PRINT '⚠ No Companies found in database'
END

IF (SELECT COUNT(*) FROM CertificationTypes) > 0
BEGIN
    PRINT 'Sample CertificationTypes:'
    SELECT TOP 5 Id, Name, AmazonCode FROM CertificationTypes
END
ELSE
BEGIN
    PRINT '⚠ No CertificationTypes found in database'
END

IF (SELECT COUNT(*) FROM Certificates) > 0
BEGIN
    PRINT 'Sample Certificates:'
    SELECT TOP 5 Id, Number, Status, CompanyId, ProductId, CertificationTypeId FROM Certificates
END
ELSE
BEGIN
    PRINT '⚠ No Certificates found in database'
END

PRINT ''
PRINT '=========================================';
PRINT 'EXPECTED vs ACTUAL:'
PRINT '-----------------------------------------'
PRINT 'EXPECTED (from development):'
PRINT '  Roles: 4'
PRINT '  Users: 14'
PRINT '  Companies: 28'
PRINT '  Partners: 3'
PRINT '  CertificationTypes: 22'
PRINT '  Products: 39'
PRINT '  Certificates: 178'
PRINT ''
PRINT 'Run this script to see ACTUAL counts above'
PRINT '========================================='

-- Azure SQL Database Migration Verification Script
-- Run this after importing the database to Azure SQL
-- Compare results with source database at AccuWebHosting

-- ============================================
-- 1. TABLE COUNT VERIFICATION
-- ============================================
SELECT
    COUNT(*) AS TotalTables,
    'Should match source database table count' AS Note
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';

-- ============================================
-- 2. LIST ALL TABLES
-- ============================================
SELECT
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- ============================================
-- 3. ROW COUNT VERIFICATION
-- ============================================
SELECT
    'Certificates' AS TableName,
    COUNT(*) AS RowCount
FROM Certificates
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds
UNION ALL
SELECT 'ApiKeys', COUNT(*) FROM ApiKeys
UNION ALL
SELECT 'Partners', COUNT(*) FROM Partners
ORDER BY TableName;

-- ============================================
-- 4. VERIFY ADMIN USER EXISTS
-- ============================================
SELECT
    Id,
    Email,
    UserName,
    EmailConfirmed,
    LockoutEnabled,
    AccessFailedCount
FROM Users
WHERE Email = 'admin@greencirclecertified.com';

-- Should return 1 row with admin user

-- ============================================
-- 5. VERIFY USER ROLES
-- ============================================
SELECT
    r.Name AS RoleName,
    COUNT(ur.UserId) AS UserCount
FROM AspNetRoles r
LEFT JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
GROUP BY r.Name
ORDER BY r.Name;

-- Should show Admin, Analyst, External roles

-- ============================================
-- 6. VERIFY CERTIFICATES DATA
-- ============================================
SELECT
    CertificateNumber,
    CompanyName,
    ProductName,
    Status,
    Partner,
    CreatedDate,
    ModifiedDate
FROM Certificates
ORDER BY CreatedDate DESC;

-- Verify sample certificates exist

-- ============================================
-- 7. VERIFY PRODUCT IDs (ASINs)
-- ============================================
SELECT
    c.CertificateNumber,
    cp.ProductIdType,
    cp.ProductIdValue,
    cp.IsVerified
FROM CertificateProductIds cp
INNER JOIN Certificates c ON cp.CertificateId = c.Id
ORDER BY c.CertificateNumber, cp.ProductIdType;

-- Verify ASINs and other product IDs are migrated

-- ============================================
-- 8. VERIFY INDEXES
-- ============================================
SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.is_primary_key AS IsPrimaryKey
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.is_ms_shipped = 0
ORDER BY t.name, i.name;

-- ============================================
-- 9. VERIFY FOREIGN KEYS
-- ============================================
SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
ORDER BY TableName, ForeignKeyName;

-- ============================================
-- 10. DATABASE SIZE
-- ============================================
SELECT
    DB_NAME() AS DatabaseName,
    SUM(size) * 8 / 1024 AS SizeMB,
    'Azure SQL Basic allows 2 GB (2048 MB)' AS Note
FROM sys.database_files;

-- ============================================
-- 11. VERIFY MIGRATION HISTORY (EF Core)
-- ============================================
SELECT
    MigrationId,
    ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;

-- Should show all 12 migrations:
-- - 20250808131552_InitialCreate
-- - 20250808134211_UpdatedDatabaseSchema
-- - 20250808143700_InitialMigration
-- - 20250808160522_AdminEnhancements
-- - 20250810151650_UpdateETLConfiguration
-- - 20250811142726_AddProductIdentifiers
-- - 20250813031456_FixModelIssues
-- - 20250827043046_AddPerformanceIndexes
-- - 20250827182202_AddPartnerFieldToCertificate
-- - 20250909010923_AddPartnerEntity
-- - 20250918212035_AddCertificateProductIds
-- - 20251006162626_AddCertificateImagePath

-- ============================================
-- 12. CHECK FOR ORPHANED RECORDS
-- ============================================

-- Certificates without Companies (should be 0)
SELECT COUNT(*) AS OrphanedCertificatesWithoutCompany
FROM Certificates c
WHERE NOT EXISTS (
    SELECT 1 FROM Companies co WHERE co.Id = c.CompanyId
);

-- Certificates without Products (should be 0 if products are required)
SELECT COUNT(*) AS OrphanedCertificatesWithoutProduct
FROM Certificates c
WHERE NOT EXISTS (
    SELECT 1 FROM Products p WHERE p.Id = c.ProductId
);

-- Product IDs without Certificates (should be 0)
SELECT COUNT(*) AS OrphanedProductIds
FROM CertificateProductIds cp
WHERE NOT EXISTS (
    SELECT 1 FROM Certificates c WHERE c.Id = cp.CertificateId
);

-- ============================================
-- 13. VERIFY PARTNER DATA
-- ============================================
SELECT
    Id,
    PartnerName,
    IsActive,
    RequiresCertificateApproval
FROM Partners
ORDER BY PartnerName;

-- Should show Amazon, Wayfair, and None partners

-- ============================================
-- 14. VERIFY API KEYS
-- ============================================
SELECT
    KeyName,
    IsActive,
    ExpirationDate,
    RateLimitPerHour,
    CreatedDate
FROM ApiKeys
ORDER BY CreatedDate DESC;

-- ============================================
-- 15. FINAL SUMMARY
-- ============================================
SELECT
    'Migration Verification Complete' AS Status,
    GETDATE() AS VerifiedAt,
    DB_NAME() AS DatabaseName,
    @@VERSION AS SQLVersion;

-- ============================================
-- COMPARISON CHECKLIST
-- ============================================
/*
Run this script on BOTH databases and compare results:

1. Source (AccuWebHosting):
   Server: 68.71.129.120,1533
   Database: gccwebap_gccdb

2. Target (Azure SQL):
   Server: [your-server].database.windows.net
   Database: gccwebapp-db

COMPARE:
✓ Table count matches
✓ Row counts match for all tables
✓ Admin user exists
✓ All roles exist
✓ Sample certificates visible
✓ Product IDs (ASINs) migrated
✓ Indexes exist
✓ Foreign keys exist
✓ All 12 migrations recorded
✓ No orphaned records
✓ Partners exist
✓ API keys migrated

If all checks pass → Migration successful ✓
If any checks fail → Review migration and re-import
*/

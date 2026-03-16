-- ============================================
-- CLEAN ALL DATA - Remove all data but keep schema
-- ============================================
-- This script removes all data from tables while preserving:
-- - Table structures
-- - Indexes
-- - Constraints
-- - Stored procedures
-- - Views
--
-- Use this before running FULL_TEST_DATA_SEED.sql for a clean start
-- ============================================

PRINT '============================================';
PRINT 'GCC Database Data Cleanup';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';
PRINT 'WARNING: This will delete ALL DATA but preserve schema';
PRINT 'Press Ctrl+C within 5 seconds to cancel...';
WAITFOR DELAY '00:00:05';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY
    -- Disable all foreign key constraints temporarily
    PRINT 'Disabling foreign key constraints...';
    EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
    PRINT '✓ Constraints disabled';
    PRINT '';

    -- Delete data in reverse dependency order (child tables first)
    PRINT '1. Cleaning junction/relationship tables...';
    DELETE FROM dbo.CertificateTags;
    PRINT '   ✓ CertificateTags cleared';

    DELETE FROM dbo.CertificateProductIds;
    PRINT '   ✓ CertificateProductIds cleared';

    DELETE FROM dbo.ProductPartners;
    PRINT '   ✓ ProductPartners cleared';

    DELETE FROM dbo.CompanyPartners;
    PRINT '   ✓ CompanyPartners cleared';

    DELETE FROM dbo.UserPartners;
    PRINT '   ✓ UserPartners cleared';

    DELETE FROM dbo.UserCompanies;
    PRINT '   ✓ UserCompanies cleared';

    DELETE FROM dbo.RolePermissions;
    PRINT '   ✓ RolePermissions cleared';

    DELETE FROM dbo.RoleUser;
    PRINT '   ✓ RoleUser cleared';
    PRINT '';

    PRINT '2. Cleaning operational tables...';
    DELETE FROM dbo.ApiKeys;
    PRINT '   ✓ ApiKeys cleared';

    DELETE FROM dbo.AuditLogs;
    PRINT '   ✓ AuditLogs cleared';

    DELETE FROM dbo.CertificateStatusHistory;
    PRINT '   ✓ CertificateStatusHistory cleared';

    DELETE FROM dbo.Certificates;
    PRINT '   ✓ Certificates cleared';
    PRINT '';

    PRINT '3. Cleaning master data tables...';
    DELETE FROM dbo.Products;
    PRINT '   ✓ Products cleared';

    DELETE FROM dbo.Companies;
    PRINT '   ✓ Companies cleared';

    DELETE FROM dbo.Users;
    PRINT '   ✓ Users cleared';

    DELETE FROM dbo.Tags;
    PRINT '   ✓ Tags cleared';
    PRINT '';

    PRINT '4. Cleaning reference data (will be reseeded)...';
    DELETE FROM dbo.Partners;
    PRINT '   ✓ Partners cleared';

    DELETE FROM dbo.CSFAttributes;
    PRINT '   ✓ CSFAttributes cleared';

    DELETE FROM dbo.CertificateFields;
    PRINT '   ✓ CertificateFields cleared';

    DELETE FROM dbo.CertificationTypes;
    PRINT '   ✓ CertificationTypes cleared';

    DELETE FROM dbo.ProductTypes;
    PRINT '   ✓ ProductTypes cleared';

    DELETE FROM dbo.ProductCategories;
    PRINT '   ✓ ProductCategories cleared';

    DELETE FROM dbo.Permissions;
    PRINT '   ✓ Permissions cleared';

    DELETE FROM dbo.Roles;
    PRINT '   ✓ Roles cleared';
    PRINT '';

    -- Reset identity seeds to start from 1
    PRINT '5. Resetting identity seeds...';
    DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Roles', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Permissions', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Companies', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Products', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Certificates', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Tags', RESEED, 0);
    DBCC CHECKIDENT ('dbo.Partners', RESEED, 0);
    DBCC CHECKIDENT ('dbo.ProductCategories', RESEED, 0);
    DBCC CHECKIDENT ('dbo.ProductTypes', RESEED, 0);
    DBCC CHECKIDENT ('dbo.CertificationTypes', RESEED, 0);
    DBCC CHECKIDENT ('dbo.CertificateFields', RESEED, 0);
    DBCC CHECKIDENT ('dbo.CSFAttributes', RESEED, 0);
    DBCC CHECKIDENT ('dbo.ApiKeys', RESEED, 0);
    DBCC CHECKIDENT ('dbo.AuditLogs', RESEED, 0);
    PRINT '   ✓ Identity seeds reset';
    PRINT '';

    -- Re-enable all foreign key constraints
    PRINT '6. Re-enabling foreign key constraints...';
    EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
    PRINT '   ✓ Constraints re-enabled';
    PRINT '';

    COMMIT TRANSACTION;

    PRINT '============================================';
    PRINT 'SUCCESS: All data cleaned successfully';
    PRINT 'Database is ready for reseeding';
    PRINT 'Run: FULL_TEST_DATA_SEED.sql';
    PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
    PRINT '============================================';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED DURING CLEANUP';
    PRINT '============================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR);
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR);
    PRINT '============================================';
    PRINT 'Transaction rolled back - no changes made';
    PRINT '============================================';

    -- Re-enable constraints even on error
    EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
END CATCH;

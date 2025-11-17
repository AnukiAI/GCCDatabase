-- ============================================
-- Clear Test Data and Reseed with Full Dev Data
-- ============================================
-- This script:
-- 1. Clears the 22 test certificates we seeded earlier
-- 2. Prepares for the full 178-certificate dev dataset
-- ============================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'Clearing Test Data from Production';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- Delete test certificates (22 certs)
PRINT 'Deleting test certificates...';
DELETE FROM Certificates WHERE Id BETWEEN 1 AND 22;
PRINT '✓ Test certificates deleted';

-- Delete test products (10 products)
PRINT 'Deleting test products...';
DELETE FROM Products WHERE Id BETWEEN 1 AND 10;
PRINT '✓ Test products deleted';

-- Delete test companies (10 companies)
PRINT 'Deleting test companies...';
DELETE FROM Companies WHERE Id BETWEEN 1 AND 10;
PRINT '✓ Test companies deleted';

COMMIT TRANSACTION;
PRINT '';
PRINT '============================================';
PRINT 'Test data cleared successfully';
PRINT 'Ready to run FULL_TEST_DATA_SEED_PRODUCTION.sql';
PRINT '============================================';

END TRY
BEGIN CATCH
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH

GO

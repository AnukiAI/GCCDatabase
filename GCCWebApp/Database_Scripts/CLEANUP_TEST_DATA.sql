-- =============================================
-- CLEANUP_TEST_DATA.sql
-- Removes test/duplicate data from production database
-- Date: 2026-01-08
-- Issues: ISSUE-005, ISSUE-006
-- =============================================

USE [gccwebap_certificates]; -- Change to your database name if different
GO

SET NOCOUNT ON;
GO

PRINT '=========================================';
PRINT 'Starting Test Data Cleanup...';
PRINT '=========================================';
PRINT '';

-- =============================================
-- ISSUE-005: Remove "test3" Partner (if exists)
-- =============================================
PRINT 'ISSUE-005: Checking for "test3" partner...';

IF EXISTS (SELECT 1 FROM dbo.Partners WHERE Name = 'test3')
BEGIN
    -- First check if any certificates reference this partner
    DECLARE @Test3PartnerId INT = (SELECT Id FROM dbo.Partners WHERE Name = 'test3');
    DECLARE @CertCount INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE PartnerId = @Test3PartnerId);

    IF @CertCount > 0
    BEGIN
        PRINT '  WARNING: Partner "test3" has ' + CAST(@CertCount AS VARCHAR) + ' associated certificates.';
        PRINT '  Reassigning to "Other" partner before deletion...';

        DECLARE @OtherPartnerId INT = (SELECT Id FROM dbo.Partners WHERE Name = 'Other');
        IF @OtherPartnerId IS NOT NULL
        BEGIN
            UPDATE dbo.Certificates SET PartnerId = @OtherPartnerId WHERE PartnerId = @Test3PartnerId;
            PRINT '  ✓ Reassigned ' + CAST(@CertCount AS VARCHAR) + ' certificates to "Other" partner.';
        END
    END

    -- Check for PartnerApiKeys
    DELETE FROM dbo.PartnerApiKeys WHERE PartnerId = @Test3PartnerId;
    PRINT '  ✓ Removed any API keys for "test3" partner.';

    -- Delete the partner
    DELETE FROM dbo.Partners WHERE Name = 'test3';
    PRINT '  ✓ Deleted "test3" partner.';
END
ELSE
BEGIN
    PRINT '  - Partner "test3" not found (may already be cleaned up).';
END

PRINT '';

-- =============================================
-- ISSUE-006: Check for Duplicate Vantage Vinyl Types
-- =============================================
PRINT 'ISSUE-006: Checking for duplicate Vantage Vinyl types...';

-- List all Vantage Vinyl related entries
SELECT Id, Name, AmazonCode, Description, IsActive, SortOrder
FROM dbo.CertificationTypes
WHERE Name LIKE '%Vantage%' OR Name LIKE '%Vinyl%'
ORDER BY Name;

-- Count duplicates
DECLARE @VantageCount INT = (SELECT COUNT(*) FROM dbo.CertificationTypes WHERE Name LIKE '%Vantage Vinyl%');

IF @VantageCount > 1
BEGIN
    PRINT '  WARNING: Found ' + CAST(@VantageCount AS VARCHAR) + ' Vantage Vinyl entries.';
    PRINT '  Review the entries above and decide which to keep.';
    PRINT '  ';
    PRINT '  To remove duplicates, uncomment and run:';
    PRINT '  -- DELETE FROM dbo.CertificationTypes WHERE Id = [ID_TO_REMOVE];';
    PRINT '  ';
    PRINT '  Recommended: Keep the entry with proper trademark symbol (+Vantage Vinyl™ Verified)';
END
ELSE IF @VantageCount = 1
BEGIN
    PRINT '  ✓ Only one Vantage Vinyl entry found - no duplicates.';
END
ELSE
BEGIN
    PRINT '  - No Vantage Vinyl entries found.';
END

PRINT '';

-- =============================================
-- Additional: List any other test/placeholder data
-- =============================================
PRINT 'Additional Cleanup Check:';

-- Check for test partners
PRINT '  Partners with "test" in name:';
SELECT Id, Name, Description, IsActive FROM dbo.Partners WHERE Name LIKE '%test%';

-- Check for placeholder data
PRINT '  Partners with empty description:';
SELECT Id, Name, ISNULL(Description, '(null)') as Description FROM dbo.Partners WHERE Description IS NULL OR Description = '';

PRINT '';
PRINT '=========================================';
PRINT 'Test Data Cleanup Complete!';
PRINT '=========================================';
GO

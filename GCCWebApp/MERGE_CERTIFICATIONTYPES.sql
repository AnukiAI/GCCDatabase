-- ===============================================================================
-- CertificationTypes Merge: Conservative Approach (Option A)
-- Production keeps IDs 1-10, Dev's new types get IDs 11-27
-- CRITICAL: IDs 6-10 are used by 125 certificates - CANNOT be deleted/modified
-- ===============================================================================

USE gccwebap_certificates;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Safety check: Verify we have exactly 10 types before merge
    DECLARE @CurrentCount INT = (SELECT COUNT(*) FROM CertificationTypes);
    
    IF @CurrentCount <> 10
    BEGIN
        RAISERROR('Production has unexpected count. Expected 10, found %d. Merge aborted.', 16, 1, @CurrentCount);
        RETURN;
    END

    PRINT 'Pre-merge count: 10 CertificationTypes';
    PRINT 'Adding 17 new CertificationTypes from Dev (IDs 11-27)...';
    PRINT '';

    SET IDENTITY_INSERT CertificationTypes ON;

    -- Insert 17 new Dev types that don't exist in Production
    INSERT INTO CertificationTypes (Id, Name, Description, Level, ParentId, SortOrder, IsActive) VALUES
    (11, 'Carbon Footprint Reduction', 'Products with reduced carbon emissions', 1, NULL, 11, 1),
    (12, 'Certified Energy Savings', 'Energy efficiency certification', 1, NULL, 12, 1),
    (13, 'Certified Environmental Facts', 'Environmental impact certification', 1, NULL, 13, 1),
    (14, 'Certified Water Savings', 'Water conservation certification', 1, NULL, 14, 1),
    (15, 'Closed Loop Product', 'Products designed for circular economy', 1, NULL, 15, 1),
    (16, 'Dematerialization', 'Products with reduced material usage', 1, NULL, 16, 1),
    (17, 'Greenhouse Gas (GHG) Verification', 'Verified greenhouse gas reduction', 1, NULL, 17, 1),
    (18, 'GreenCircle Audited', 'Third-party audited certification', 1, NULL, 18, 1),
    (19, 'Life Cycle Assessment (LCA) Optimized', 'Optimized environmental impact across product lifecycle', 1, NULL, 19, 1),
    (20, 'Manufacturer Inventory Verification', 'Verified manufacturer inventory practices', 1, NULL, 20, 1),
    (21, 'Recyclable Material', 'Products made from recyclable materials', 1, NULL, 21, 1),
    (22, 'Renewable Resource Content', 'Products from renewable resources', 1, NULL, 22, 1),
    (23, 'Sustainable Energy Practices', 'Sustainable energy manufacturing practices', 1, NULL, 23, 1),
    (24, 'Sustainable Manufacturing Practices', 'Environmentally responsible manufacturing', 1, NULL, 24, 1),
    (25, 'Waste System Audit', 'Audited waste management systems', 1, NULL, 25, 1),
    (26, '+Vantage Vinyl™ Verified', 'Verified Vantage Vinyl certification', 1, NULL, 26, 1),
    (27, 'Preloved', 'Refurbished or pre-owned sustainable products', 1, NULL, 27, 1);

    SET IDENTITY_INSERT CertificationTypes OFF;

    -- Verify final count
    DECLARE @FinalCount INT = (SELECT COUNT(*) FROM CertificationTypes);

    IF @FinalCount = 27
    BEGIN
        COMMIT TRANSACTION;
        
        PRINT '';
        PRINT '========================================';
        PRINT 'SUCCESS: Merge Complete';
        PRINT '========================================';
        PRINT 'Total CertificationTypes: ' + CAST(@FinalCount AS VARCHAR);
        PRINT '  - Preserved (IDs 1-10):  10 types';
        PRINT '  - New from Dev (11-27):  17 types';
        PRINT '';
        PRINT 'Production types 6-10 preserved (used by 125 certificates)';
    END
    ELSE
    BEGIN
        RAISERROR('Unexpected final count. Expected 27, found %d. Rolling back.', 16, 1, @FinalCount);
        ROLLBACK TRANSACTION;
    END

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    PRINT 'Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    
    THROW;
END CATCH;
GO

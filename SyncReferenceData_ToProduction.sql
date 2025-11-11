-- ===============================================================================
-- GCC Database - Sync Reference Data from Dev to Production
-- Date: 2025-11-10
-- Purpose: Import missing reference data into Production database
-- ===============================================================================

USE gccwebap_certificates;
GO

PRINT '========================================';
PRINT 'Starting Reference Data Synchronization';
PRINT '========================================';
PRINT '';

-- Store original settings
DECLARE @OriginalNoCountSetting VARCHAR(3) = CASE WHEN (512 & @@OPTIONS) = 512 THEN 'ON' ELSE 'OFF' END;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- ===============================================================================
    -- 1. PRODUCTTYPES (19 records)
    -- ===============================================================================
    PRINT 'Processing ProductTypes...';

    -- Delete existing data (if any)
    DELETE FROM ProductTypes;

    -- Reset identity
    DBCC CHECKIDENT ('ProductTypes', RESEED, 0);

    -- Enable identity insert
    SET IDENTITY_INSERT ProductTypes ON;

    -- Insert data
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (1, 'Insulation', 1, NULL, 1, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (2, 'Appliances', 1, NULL, 1, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (3, 'Flooring', 1, NULL, 1, 3, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (4, 'Carpeting', 5, NULL, 1, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (5, 'Fabric', 5, NULL, 1, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (6, 'Network Cables', 2, NULL, 1, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (7, 'Lighting', 2, NULL, 1, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (8, 'Furniture', 6, NULL, 1, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (9, 'Foam Board', 1, 1, 2, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (10, 'Kitchen Appliances', 1, 2, 2, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (11, 'Laundry Appliances', 1, 2, 2, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (12, 'Hardwood', 1, 3, 2, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (13, 'Laminate', 1, 3, 2, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (14, 'Commercial Carpet', 5, 4, 2, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (15, 'Residential Carpet', 5, 4, 2, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (16, 'Upholstery Fabric', 5, 5, 2, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (17, 'Apparel Fabric', 5, 5, 2, 2, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (18, 'Office Chairs', 6, 8, 2, 1, 1);
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (19, 'Sofas', 6, 8, 2, 2, 1);

    SET IDENTITY_INSERT ProductTypes OFF;

    PRINT '  ✓ ProductTypes: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records inserted';
    PRINT '';

    -- ===============================================================================
    -- 2. TAGS (26 records)
    -- ===============================================================================
    PRINT 'Processing Tags...';

    -- Delete existing data (if any)
    DELETE FROM Tags;

    -- Reset identity
    DBCC CHECKIDENT ('Tags', RESEED, 0);

    -- Enable identity insert
    SET IDENTITY_INSERT Tags ON;

    -- Insert data (will be generated from insert_tags.sql)
    -- Note: Manual verification needed for TagType values (internal/external) and descriptions

    SET IDENTITY_INSERT Tags OFF;

    PRINT '  ! Tags: Skipped - requires manual column verification';
    PRINT '';

    -- ===============================================================================
    -- 3. CUSTOMFIELDS (30 records)
    -- ===============================================================================
    PRINT 'Processing CustomFields...';

    -- Delete existing data (if any)
    DELETE FROM CustomFields;

    -- Reset identity
    DBCC CHECKIDENT ('CustomFields', RESEED, 0);

    -- Enable identity insert
    SET IDENTITY_INSERT CustomFields ON;

    -- Insert data (will be generated from insert_customfields.sql)

    SET IDENTITY_INSERT CustomFields OFF;

    PRINT '  ! CustomFields: Skipped - requires manual data insertion';
    PRINT '';

    -- ===============================================================================
    -- 4. PERMISSIONS (6 records)
    -- ===============================================================================
    PRINT 'Processing Permissions...';

    -- Delete existing data (if any)
    DELETE FROM Permissions;

    -- Reset identity
    DBCC CHECKIDENT ('Permissions', RESEED, 0);

    -- Enable identity insert
    SET IDENTITY_INSERT Permissions ON;

    -- Insert data (will be generated from insert_permissions.sql)

    SET IDENTITY_INSERT Permissions OFF;

    PRINT '  ! Permissions: Skipped - requires manual data insertion';
    PRINT '';

    -- ===============================================================================
    -- 5. CERTIFICATIONTYPES (22 records - merge with existing 10)
    -- ===============================================================================
    PRINT 'Processing CertificationTypes...';

    PRINT '  ! CertificationTypes: Skipped - requires merge logic with existing 10 records';
    PRINT '';

    -- ===============================================================================
    -- 6. PERMISSIONROLE (12 records)
    -- ===============================================================================
    PRINT 'Processing PermissionRole...';

    -- This table has no identity, just foreign keys
    -- Delete existing data (if any)
    DELETE FROM PermissionRole;

    -- Insert data (will be generated from insert_permissionrole.sql)

    PRINT '  ! PermissionRole: Skipped - requires Permissions to be inserted first';
    PRINT '';

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '========================================';
    PRINT 'Reference Data Sync Complete (Partial)';
    PRINT '========================================';
    PRINT 'ProductTypes: ✓ Complete (19 records)';
    PRINT 'Tags: Pending manual insertion';
    PRINT 'CustomFields: Pending manual insertion';
    PRINT 'Permissions: Pending manual insertion';
    PRINT 'PermissionRole: Pending manual insertion';
    PRINT 'CertificationTypes: Pending merge strategy';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    PRINT 'Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Procedure: ' + ISNULL(ERROR_PROCEDURE(), 'N/A');

    -- Re-throw the error
    THROW;
END CATCH;

-- Restore original settings
IF @OriginalNoCountSetting = 'OFF'
    SET NOCOUNT OFF;
GO

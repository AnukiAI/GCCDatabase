-- ===================================================================================
-- GCC Database - Complete Reference Data Sync: Dev -> Production
-- Date: 2025-11-10
-- Database: gccwebap_certificates (Production)
-- ===================================================================================
--  
-- This script synchronizes reference data from Dev to Production:
--   ✓ ProductTypes (19 records)
--   ✓ Tags (26 records)
--   ✓ Permissions (6 records)
--   ✓ PermissionRole (12 records)
--   ⚠ CertificationTypes (manual merge required - 10 existing, 12 new)
--   ⚠ CustomFields (requires separate BCP import - data truncated)
--
-- WARNING: This will DELETE and REPLACE existing data in these tables!
-- ===================================================================================

USE gccwebap_certificates;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '==========================================================';
PRINT 'Starting Reference Data Synchronization';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '==========================================================';
PRINT '';

BEGIN TRY
    BEGIN TRANSACTION SyncReferenceData;

    -- ================================================================
    -- 1. PRODUCTTYPES (19 records)
    -- ================================================================
    PRINT '1. ProductTypes: Deleting existing data...';
    DELETE FROM ProductTypes;
    DBCC CHECKIDENT ('ProductTypes', RESEED, 0);
    
    PRINT '   ProductTypes: Inserting 19 records...';
    SET IDENTITY_INSERT ProductTypes ON;
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (1, 'Insulation', 1, NULL, 1, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (2, 'Appliances', 1, NULL, 1, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (3, 'Flooring', 1, NULL, 1, 3, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (4, 'Carpeting', 5, NULL, 1, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (5, 'Fabric', 5, NULL, 1, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (6, 'Network Cables', 2, NULL, 1, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (7, 'Lighting', 2, NULL, 1, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (8, 'Furniture', 6, NULL, 1, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (9, 'Foam Board', 1, CAST(1 AS INT), 2, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (10, 'Kitchen Appliances', 1, CAST(2 AS INT), 2, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (11, 'Laundry Appliances', 1, CAST(2 AS INT), 2, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (12, 'Hardwood', 1, CAST(3 AS INT), 2, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (13, 'Laminate', 1, CAST(3 AS INT), 2, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (14, 'Commercial Carpet', 5, CAST(4 AS INT), 2, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (15, 'Residential Carpet', 5, CAST(4 AS INT), 2, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (16, 'Upholstery Fabric', 5, CAST(5 AS INT), 2, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (17, 'Apparel Fabric', 5, CAST(5 AS INT), 2, 2, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (18, 'Office Chairs', 6, CAST(8 AS INT), 2, 1, 1);
INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (19, 'Tables', 6, CAST(8 AS INT), 2, 2, 1);

(19 rows affected)
    SET IDENTITY_INSERT ProductTypes OFF;
    PRINT '   ✓ ProductTypes: Complete';
    PRINT '';

    -- ================================================================
    -- 2. TAGS (26 records)
    -- ================================================================
    PRINT '2. Tags: Deleting existing data...';
    DELETE FROM Tags;
    DBCC CHECKIDENT ('Tags', RESEED, 0);
    
    PRINT '   Tags: Inserting 26 records...';
    SET IDENTITY_INSERT Tags ON;
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (1, 'APR', 'external', 'APR Environmental Product Declaration', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (2, 'LEED', 'external', 'Contributes to LEED certification', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (3, 'High Priority Client', 'internal', 'Important client certification', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (4, 'Q4 Review', 'internal', 'Scheduled for quarterly review', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (5, 'Environmental Facts', 'internal', 'Environmental impact data', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (6, 'Recycled Content', 'internal', 'Contains recycled materials', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (7, 'Carbon Neutral', 'internal', 'Carbon neutral product', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (8, 'Energy Star', 'internal', 'Energy Star certified product', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (9, 'Cradle to Cradle', 'internal', 'Cradle to Cradle certified', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (10, 'Forest Stewardship Council', 'internal', 'FSC certified materials', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (11, 'GREENGUARD', 'internal', 'Low chemical emissions', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (12, 'Bio-Based', 'internal', 'Made from renewable biological resources', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (13, 'Renewable Energy', 'internal', 'Manufactured using renewable energy', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (14, 'Circular Economy', 'internal', 'Designed for circular economy principles', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (15, 'Water Stewardship', 'internal', 'Responsible water usage in manufacturing', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (16, 'Fair Trade', 'internal', 'Fair trade certified materials', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (17, 'Urgent Review', 'internal', 'Requires immediate attention', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (18, 'Annual Audit', 'internal', 'Scheduled for annual audit', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (19, 'Customer Request', 'internal', 'Generated from customer inquiry', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (20, 'Regulatory Compliance', 'internal', 'Required for regulatory compliance', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (21, 'Market Expansion', 'internal', 'Supporting new market entry', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (22, 'Partnership Program', 'internal', 'Part of strategic partnership', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (23, 'Innovation Project', 'internal', 'Supporting innovation initiative', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (24, 'Cost Optimization', 'internal', 'Cost reduction initiative', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (25, 'Quality Improvement', 'internal', 'Quality enhancement program', 1);
INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (26, 'Digital Transformation', 'internal', 'Digital transformation project', 1);

(26 rows affected)
    SET IDENTITY_INSERT Tags OFF;
    PRINT '   ✓ Tags: Complete';
    PRINT '';

    -- ================================================================
    -- 3. PERMISSIONS (6 records)
    -- ================================================================
    PRINT '3. Permissions: Deleting existing data...';
    DELETE FROM Permissions;
    DBCC CHECKIDENT ('Permissions', RESEED, 0);
    
    PRINT '   Permissions: Inserting 6 records...';
    SET IDENTITY_INSERT Permissions ON;
INSERT INTO Permissions (Id, Name, Description, Category, IsActive) VALUES (1, 'View Certificates', 'Can view certificates', 'Certificate', 1);
INSERT INTO Permissions (Id, Name, Description, Category, IsActive) VALUES (2, 'Create Certificates', 'Can create new certificates', 'Certificate', 1);
INSERT INTO Permissions (Id, Name, Description, Category, IsActive) VALUES (3, 'Edit Certificates', 'Can edit existing certificates', 'Certificate', 1);
INSERT INTO Permissions (Id, Name, Description, Category, IsActive) VALUES (4, 'Delete Certificates', 'Can delete certificates', 'Certificate', 1);
INSERT INTO Permissions (Id, Name, Description, Category, IsActive) VALUES (5, 'Manage Users', 'Can manage user accounts', 'User', 1);
INSERT INTO Permissions (Id, Name, Description, Category, IsActive) VALUES (6, 'View Reports', 'Can view system reports', 'Report', 1);

(6 rows affected)
    SET IDENTITY_INSERT Permissions OFF;
    PRINT '   ✓ Permissions: Complete';
    PRINT '';

    -- ================================================================
    -- 4. PERMISSIONROLE (12 records)
    -- ================================================================
    PRINT '4. PermissionRole: Deleting existing data...';
    DELETE FROM PermissionRole;
    
    PRINT '   PermissionRole: Inserting 12 records...';
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 2);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 3);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 4);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (2, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (2, 2);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (3, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (3, 2);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (4, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (5, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (6, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (6, 3);

(12 rows affected)
    PRINT '   ✓ PermissionRole: Complete';
    PRINT '';

    COMMIT TRANSACTION SyncReferenceData;

    PRINT '';
    PRINT '==========================================================';
    PRINT 'SYNC COMPLETED SUCCESSFULLY!';
    PRINT '==========================================================';
    PRINT '';
    PRINT 'Summary:';
    PRINT '  ✓ ProductTypes: 19 records';
    PRINT '  ✓ Tags: 26 records';
    PRINT '  ✓ Permissions: 6 records';
    PRINT '  ✓ PermissionRole: 12 records';
    PRINT '';
    PRINT 'Still TODO:';
    PRINT '  ⚠ CertificationTypes: Requires manual review/merge';
    PRINT '     (Production has 10, Dev has 22)';
    PRINT '  ⚠ CustomFields: Requires separate BCP export/import';
    PRINT '     (30 records with complex data)';
    PRINT '';
    PRINT 'Next Steps:';
    PRINT '  1. Verify record counts match';
    PRINT '  2. Test application functionality';
    PRINT '  3. Review CertificationTypes merge strategy';
    PRINT '';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION SyncReferenceData;

    PRINT '';
    PRINT '==========================================================';
    PRINT 'ERROR - TRANSACTION ROLLED BACK';
    PRINT '==========================================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Error Procedure: ' + ISNULL(ERROR_PROCEDURE(), 'N/A');
    
    THROW;
END CATCH;

SET NOCOUNT OFF;
GO

PRINT '';
PRINT 'Script execution complete. Review output above for results.';
GO

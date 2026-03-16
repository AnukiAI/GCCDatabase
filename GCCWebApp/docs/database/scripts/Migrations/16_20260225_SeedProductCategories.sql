SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Migration: 16_20260225_SeedProductCategories
-- Description: Seeds production Product Categories and Product Types from client-provided
--              spreadsheet "Copy of product categories and csf attributes.xlsx"
--              Replaces test/mixed data with 8 categories, 45 types, and 6 subcategories.
-- Author: Claude Code
-- Date: 2026-02-25
-- Source: Product Category tab from client Excel file

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '================================================================';
    PRINT 'Migration 16: Seed Product Categories & Types from Client Data';
    PRINT '================================================================';
    PRINT '';

    -- =====================================================
    -- STEP 1: Clear ProductTypeId on Products (nullable column)
    -- =====================================================
    PRINT '1. Clearing ProductTypeId references on Products...';

    UPDATE dbo.Products SET ProductTypeId = NULL WHERE ProductTypeId IS NOT NULL;
    PRINT '   Done.';
    PRINT '';

    -- =====================================================
    -- STEP 2: Delete ALL existing Product Types
    --         (children first to respect FK constraints)
    -- =====================================================
    PRINT '2. Removing all existing Product Types...';

    -- Delete deepest children first (Level 2+)
    WHILE EXISTS (SELECT 1 FROM dbo.ProductTypes WHERE ParentId IS NOT NULL
                  AND Id NOT IN (SELECT DISTINCT ParentId FROM dbo.ProductTypes WHERE ParentId IS NOT NULL))
    BEGIN
        DELETE FROM dbo.ProductTypes
        WHERE ParentId IS NOT NULL
        AND Id NOT IN (SELECT DISTINCT ParentId FROM dbo.ProductTypes WHERE ParentId IS NOT NULL);
    END

    -- Delete remaining children
    DELETE FROM dbo.ProductTypes WHERE ParentId IS NOT NULL;
    -- Delete root types
    DELETE FROM dbo.ProductTypes;

    DECLARE @deletedTypes INT = @@ROWCOUNT;
    PRINT '   All product types removed.';
    PRINT '';

    -- =====================================================
    -- STEP 3: Ensure all 8 Product Categories exist
    --         Categories 1-6 may already exist, add 7-8
    --         Products.CategoryId is NOT NULL so we keep
    --         existing categories in place.
    -- =====================================================
    PRINT '3. Setting up Product Categories...';

    -- Update existing categories to match client data
    UPDATE dbo.ProductCategories SET Name = 'Chemicals and Raw Materials',
        Description = 'Chemical compounds, metals, minerals, and plastic resins', SortOrder = 1
        WHERE Id = 1;
    UPDATE dbo.ProductCategories SET Name = 'Clothing, Apparel, and Fabrics',
        Description = 'Apparel, accessories, jewelry, and textile products', SortOrder = 2
        WHERE Id = 2;
    UPDATE dbo.ProductCategories SET Name = 'Construction / Building Products',
        Description = 'Building materials, fixtures, and construction supplies', SortOrder = 3
        WHERE Id = 3;
    UPDATE dbo.ProductCategories SET Name = 'Consumer Products',
        Description = 'Household goods, appliances, and consumer items', SortOrder = 4
        WHERE Id = 4;
    UPDATE dbo.ProductCategories SET Name = 'Electronics, Computers, and Accessories',
        Description = 'Electronic devices, components, and accessories', SortOrder = 5
        WHERE Id = 5;
    UPDATE dbo.ProductCategories SET Name = 'Food and Beverage',
        Description = 'Food, beverages, and flavoring products', SortOrder = 6
        WHERE Id = 6;

    -- Add Healthcare and Pharmaceutical if not exists
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductCategories WHERE Id = 7)
    BEGIN
        SET IDENTITY_INSERT dbo.ProductCategories ON;
        INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive)
        VALUES (7, 'Healthcare and Pharmaceutical', 'Medical devices and pharmaceutical products', 7, 1);
        SET IDENTITY_INSERT dbo.ProductCategories OFF;
        PRINT '   Added: Healthcare and Pharmaceutical (Id: 7)';
    END
    ELSE
    BEGIN
        UPDATE dbo.ProductCategories SET Name = 'Healthcare and Pharmaceutical',
            Description = 'Medical devices and pharmaceutical products', SortOrder = 7
            WHERE Id = 7;
    END

    -- Add Packaging if not exists
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductCategories WHERE Id = 8)
    BEGIN
        SET IDENTITY_INSERT dbo.ProductCategories ON;
        INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive)
        VALUES (8, 'Packaging', 'Paper, plastic, and other packaging materials', 8, 1);
        SET IDENTITY_INSERT dbo.ProductCategories OFF;
        PRINT '   Added: Packaging (Id: 8)';
    END
    ELSE
    BEGIN
        UPDATE dbo.ProductCategories SET Name = 'Packaging',
            Description = 'Paper, plastic, and other packaging materials', SortOrder = 8
            WHERE Id = 8;
    END

    -- Delete any extra categories beyond Id 8 that are not referenced by products
    DELETE FROM dbo.ProductCategories
    WHERE Id > 8
    AND Id NOT IN (SELECT DISTINCT CategoryId FROM dbo.Products);

    PRINT '   8 Product Categories configured.';
    PRINT '';

    -- =====================================================
    -- STEP 4: Insert all Product Types (Level 1)
    -- =====================================================
    PRINT '4. Inserting Level 1 Product Types...';

    SET IDENTITY_INSERT dbo.ProductTypes ON;

    -- Chemicals and Raw Materials (CategoryId: 1)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (1,  'Additives',                            1, 1, 1, 1, NULL),
        (2,  'Metal Alloys',                         1, 1, 2, 1, NULL),
        (3,  'Minerals',                             1, 1, 3, 1, NULL),
        (4,  'Plastics',                             1, 1, 4, 1, NULL);

    -- Clothing, Apparel, and Fabrics (CategoryId: 2)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (5,  'Apparel',                              2, 1, 1, 1, NULL),
        (6,  'Bags and Accessories',                 2, 1, 2, 1, NULL),
        (7,  'Jewelry',                              2, 1, 3, 1, NULL);

    -- Construction / Building Products (CategoryId: 3)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (8,  'Adhesives and Sealants',               3, 1, 1,  1, NULL),
        (9,  'Ceramics and Tile',                    3, 1, 2,  1, NULL),
        (10, 'Coatings',                             3, 1, 3,  1, NULL),
        (11, 'Concrete and Concrete Materials',      3, 1, 4,  1, NULL),
        (12, 'Doors, Door Hardware, and Accessories', 3, 1, 5,  1, NULL),
        (13, 'Fiberglass Mats',                      3, 1, 6,  1, NULL),
        (14, 'Flooring',                             3, 1, 7,  1, NULL),
        (15, 'Grout and Mortar',                     3, 1, 8,  1, NULL),
        (16, 'HVAC',                                 3, 1, 9,  1, NULL),
        (17, 'Insulation',                           3, 1, 10, 1, NULL),
        (18, 'Moisture Control',                     3, 1, 11, 1, NULL),
        (19, 'Plumbing and Accessories',             3, 1, 12, 1, NULL),
        (20, 'Roofing',                              3, 1, 13, 1, NULL),
        (21, 'Wallcoverings and Interior Wall Finishes', 3, 1, 14, 1, NULL),
        (22, 'Windows, Parts, and Accessories',      3, 1, 15, 1, NULL),
        (23, 'Wiring and Cable',                     3, 1, 16, 1, NULL);

    -- Consumer Products (CategoryId: 4)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (24, 'Appliances',                           4, 1, 1,  1, NULL),
        (25, 'Baby Products',                        4, 1, 2,  1, NULL),
        (26, 'Cleaning Supplies',                    4, 1, 3,  1, NULL),
        (27, 'Furniture',                            4, 1, 4,  1, NULL),
        (28, 'Health and Beauty',                    4, 1, 5,  1, NULL),
        (29, 'Home Goods',                           4, 1, 6,  1, NULL),
        (30, 'Office Supplies (non-electronic)',     4, 1, 7,  1, NULL),
        (31, 'Personal Protective Equipment',        4, 1, 8,  1, NULL),
        (32, 'Pet Products',                         4, 1, 9,  1, NULL),
        (33, 'Security',                             4, 1, 10, 1, NULL);

    -- Electronics, Computers, and Accessories (CategoryId: 5)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (34, 'Card Reader',                          5, 1, 1, 1, NULL),
        (35, 'Electronic Accessories',               5, 1, 2, 1, NULL),
        (36, 'Microelectronics',                     5, 1, 3, 1, NULL),
        (37, 'Printers and Printer Cartridges',      5, 1, 4, 1, NULL);

    -- Food and Beverage (CategoryId: 6)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (38, 'Animal Products',                      6, 1, 1, 1, NULL),
        (39, 'Beverages',                            6, 1, 2, 1, NULL),
        (40, 'Food',                                 6, 1, 3, 1, NULL),
        (41, 'Fragrance and Flavor',                 6, 1, 4, 1, NULL);

    -- Healthcare and Pharmaceutical (CategoryId: 7)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (42, 'Medical Devices',                      7, 1, 1, 1, NULL);

    -- Packaging (CategoryId: 8)
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (43, 'Paper',                                8, 1, 1, 1, NULL),
        (44, 'Plastic',                              8, 1, 2, 1, NULL),
        (45, 'Other',                                8, 1, 3, 1, NULL);

    PRINT '   Inserted 45 Level 1 product types.';
    PRINT '';

    -- =====================================================
    -- STEP 5: Insert Level 2 subcategories (under Plastics)
    -- =====================================================
    PRINT '5. Inserting Level 2 Subcategories (under Plastics)...';

    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES
        (46, 'HDPE',    1, 2, 1, 1, 4),
        (47, 'LDPE',    1, 2, 2, 1, 4),
        (48, 'LLDPE',   1, 2, 3, 1, 4),
        (49, 'PET',     1, 2, 4, 1, 4),
        (50, 'PP',      1, 2, 5, 1, 4),
        (51, 'PVC',     1, 2, 6, 1, 4);

    SET IDENTITY_INSERT dbo.ProductTypes OFF;

    PRINT '   Inserted 6 Level 2 subcategories.';
    PRINT '';

    -- =====================================================
    -- STEP 6: Reseed identity columns
    -- =====================================================
    PRINT '6. Reseeding identity columns...';

    DBCC CHECKIDENT ('dbo.ProductCategories', RESEED, 8);
    DBCC CHECKIDENT ('dbo.ProductTypes', RESEED, 51);

    PRINT '   Done.';
    PRINT '';

    -- =====================================================
    -- STEP 7: Verification
    -- =====================================================
    PRINT '7. Verification...';

    DECLARE @catCount INT = (SELECT COUNT(*) FROM dbo.ProductCategories);
    DECLARE @typeL1Count INT = (SELECT COUNT(*) FROM dbo.ProductTypes WHERE ParentId IS NULL);
    DECLARE @typeL2Count INT = (SELECT COUNT(*) FROM dbo.ProductTypes WHERE ParentId IS NOT NULL);
    DECLARE @totalTypes INT = (SELECT COUNT(*) FROM dbo.ProductTypes);
    DECLARE @orphanProducts INT = (SELECT COUNT(*) FROM dbo.Products WHERE ProductTypeId IS NOT NULL);

    PRINT '   Product Categories: ' + CAST(@catCount AS VARCHAR) + ' (expected 8)';
    PRINT '   Product Types L1:   ' + CAST(@typeL1Count AS VARCHAR) + ' (expected 45)';
    PRINT '   Product Types L2:   ' + CAST(@typeL2Count AS VARCHAR) + ' (expected 6)';
    PRINT '   Total Types:        ' + CAST(@totalTypes AS VARCHAR) + ' (expected 51)';
    PRINT '   Products w/ type:   ' + CAST(@orphanProducts AS VARCHAR) + ' (expected 0 - need re-mapping)';
    PRINT '';

    IF @catCount = 8 AND @totalTypes = 51
    BEGIN
        PRINT '================================================================';
        PRINT 'Migration 16 COMPLETE - All data verified.';
        PRINT '================================================================';
    END
    ELSE
    BEGIN
        PRINT '*** WARNING: Counts do not match expected values! ***';
    END

    PRINT '';
    PRINT 'NOTE: All Products now have ProductTypeId = NULL.';
    PRINT '      Re-map products to new types via Admin UI.';

    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '*** MIGRATION FAILED - ROLLED BACK ***';
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT 'Line:  ' + CAST(ERROR_LINE() AS VARCHAR);
END CATCH;

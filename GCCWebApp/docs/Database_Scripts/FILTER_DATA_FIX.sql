-- ============================================
-- FILTER DATA FIX SCRIPT
-- Updates existing data to match design requirements
-- Safe for use with existing FK relationships
-- ============================================

PRINT '============================================';
PRINT 'FILTER DATA FIX SCRIPT';
PRINT 'Starting at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

-- ============================================
-- 1. Fix Certification Types
-- ============================================
PRINT '1. Fixing Certification Types...';

-- Remove duplicate +Vantage entry
DELETE FROM dbo.CertificationTypes WHERE Name = '+Vantage VinylT Verified';
PRINT '  Removed duplicate +Vantage entry';

-- Ensure Waste System Audit has correct hierarchy
DECLARE @WasteDiversionId INT;
SELECT @WasteDiversionId = Id FROM dbo.CertificationTypes WHERE Name = 'Waste Diversion from Landfill';

UPDATE dbo.CertificationTypes
SET [Level] = 2, ParentId = @WasteDiversionId
WHERE Name = 'Waste System Audit';

-- Ensure all other types are Level 1
UPDATE dbo.CertificationTypes
SET [Level] = 1, ParentId = NULL
WHERE Name != 'Waste System Audit';

PRINT '  Certification Types hierarchy fixed';
PRINT '';

-- ============================================
-- 2. Update Product Categories to match design
-- ============================================
PRINT '2. Updating Product Categories...';

-- Update existing categories to match design names
UPDATE dbo.ProductCategories SET Name = 'Chemicals and Raw Materials', Description = 'Chemical products and raw materials' WHERE Id = 1;
UPDATE dbo.ProductCategories SET Name = 'Clothing, Apparel, and Fabrics', Description = 'Clothing, apparel, and fabric products' WHERE Id = 2;
UPDATE dbo.ProductCategories SET Name = 'Construction / Building Products', Description = 'Construction and building products' WHERE Id = 3;
UPDATE dbo.ProductCategories SET Name = 'Consumer Products', Description = 'Consumer products' WHERE Id = 4;
UPDATE dbo.ProductCategories SET Name = 'Electronics, Computers, and Accessories', Description = 'Electronics, computers, and accessories' WHERE Id = 5;
UPDATE dbo.ProductCategories SET Name = 'Food and Beverage', Description = 'Food and beverage products' WHERE Id = 6;

PRINT '  Product Categories renamed to match design';
PRINT '';

-- ============================================
-- 3. Update/Insert Product Types for each category
-- ============================================
PRINT '3. Fixing Product Types...';

-- First, update existing product types to correct categories
-- Chemicals and Raw Materials (CategoryId = 1) - was Building Materials
UPDATE dbo.ProductTypes SET Name = 'Additives', CategoryId = 1, SortOrder = 1 WHERE Id = 1;
UPDATE dbo.ProductTypes SET Name = 'Metal Alloys', CategoryId = 1, SortOrder = 2 WHERE Id = 2;
UPDATE dbo.ProductTypes SET Name = 'Minerals', CategoryId = 1, SortOrder = 3 WHERE Id = 3;
UPDATE dbo.ProductTypes SET Name = 'Plastics', CategoryId = 1, SortOrder = 4 WHERE Id = 9;

-- Clothing, Apparel, and Fabrics (CategoryId = 2) - was Electronics/Electrical
UPDATE dbo.ProductTypes SET Name = 'Apparel', CategoryId = 2, SortOrder = 1 WHERE Id = 4;
UPDATE dbo.ProductTypes SET Name = 'Bags and Accessories', CategoryId = 2, SortOrder = 2 WHERE Id = 5;
UPDATE dbo.ProductTypes SET Name = 'Jewelry', CategoryId = 2, SortOrder = 3 WHERE Id = 6;

-- Construction / Building Products (CategoryId = 3) - was Materials/Plastics
UPDATE dbo.ProductTypes SET Name = 'Adhesives and Sealants', CategoryId = 3, SortOrder = 1 WHERE Id = 7;
UPDATE dbo.ProductTypes SET Name = 'Ceramics and Tile', CategoryId = 3, SortOrder = 2 WHERE Id = 8;
UPDATE dbo.ProductTypes SET Name = 'Coatings', CategoryId = 3, SortOrder = 3 WHERE Id = 10;
UPDATE dbo.ProductTypes SET Name = 'Concrete and Concrete Materials', CategoryId = 3, SortOrder = 4 WHERE Id = 11;
UPDATE dbo.ProductTypes SET Name = 'Doors, Door Hardware, and Accessories', CategoryId = 3, SortOrder = 5 WHERE Id = 12;
UPDATE dbo.ProductTypes SET Name = 'Fiberglass Mats', CategoryId = 3, SortOrder = 6 WHERE Id = 13;
UPDATE dbo.ProductTypes SET Name = 'Flooring', CategoryId = 3, SortOrder = 7 WHERE Id = 14;
UPDATE dbo.ProductTypes SET Name = 'Grout and Mortar', CategoryId = 3, SortOrder = 8 WHERE Id = 15;
UPDATE dbo.ProductTypes SET Name = 'HVAC', CategoryId = 3, SortOrder = 9 WHERE Id = 16;
UPDATE dbo.ProductTypes SET Name = 'Insulation', CategoryId = 3, SortOrder = 10 WHERE Id = 17;
UPDATE dbo.ProductTypes SET Name = 'Moisture Control', CategoryId = 3, SortOrder = 11 WHERE Id = 18;
UPDATE dbo.ProductTypes SET Name = 'Plumbing and Accessories', CategoryId = 3, SortOrder = 12 WHERE Id = 19;
UPDATE dbo.ProductTypes SET Name = 'Roofing', CategoryId = 3, SortOrder = 13 WHERE Id = 24;
UPDATE dbo.ProductTypes SET Name = 'Wallcoverings and Interior Wall Finishes', CategoryId = 3, SortOrder = 14 WHERE Id = 25;
UPDATE dbo.ProductTypes SET Name = 'Windows, Parts, and Accessories', CategoryId = 3, SortOrder = 15 WHERE Id = 26;
UPDATE dbo.ProductTypes SET Name = 'Wiring and Cable', CategoryId = 3, SortOrder = 16 WHERE Id = 27;

-- Consumer Products (CategoryId = 4) - was Paper Products
UPDATE dbo.ProductTypes SET Name = 'Appliances', CategoryId = 4, SortOrder = 1 WHERE Id = 28;
UPDATE dbo.ProductTypes SET Name = 'Baby Products', CategoryId = 4, SortOrder = 2 WHERE Id = 29;
UPDATE dbo.ProductTypes SET Name = 'Cleaning Supplies', CategoryId = 4, SortOrder = 3 WHERE Id = 30;
UPDATE dbo.ProductTypes SET Name = 'Furniture', CategoryId = 4, SortOrder = 4 WHERE Id = 31;
UPDATE dbo.ProductTypes SET Name = 'Health and Beauty', CategoryId = 4, SortOrder = 5 WHERE Id = 32;
UPDATE dbo.ProductTypes SET Name = 'Home Goods', CategoryId = 4, SortOrder = 6 WHERE Id = 33;
UPDATE dbo.ProductTypes SET Name = 'Office Supplies (non-electronic)', CategoryId = 4, SortOrder = 7 WHERE Id = 34;
UPDATE dbo.ProductTypes SET Name = 'Personal Protective Equipment', CategoryId = 4, SortOrder = 8 WHERE Id = 35;
UPDATE dbo.ProductTypes SET Name = 'Pet Products', CategoryId = 4, SortOrder = 9 WHERE Id = 36;
UPDATE dbo.ProductTypes SET Name = 'Security', CategoryId = 4, SortOrder = 10 WHERE Id = 37;

-- Electronics, Computers, and Accessories (CategoryId = 5) - was Textiles
UPDATE dbo.ProductTypes SET Name = 'Card Reader', CategoryId = 5, SortOrder = 1 WHERE Id = 38;
UPDATE dbo.ProductTypes SET Name = 'Electronic Accessories', CategoryId = 5, SortOrder = 2 WHERE Id = 39;
UPDATE dbo.ProductTypes SET Name = 'Microelectronics', CategoryId = 5, SortOrder = 3 WHERE Id = 40;
UPDATE dbo.ProductTypes SET Name = 'Printers and Printer Cartridges', CategoryId = 5, SortOrder = 4 WHERE Id = 41;

-- Food and Beverage (CategoryId = 6) - was Consumer Products
-- Check if we have product types with higher IDs, otherwise insert
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes WHERE Name = 'Animal Products')
BEGIN
    INSERT INTO dbo.ProductTypes (Name, CategoryId, [Level], SortOrder, IsActive) VALUES ('Animal Products', 6, 0, 1, 1);
END
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes WHERE Name = 'Beverages')
BEGIN
    INSERT INTO dbo.ProductTypes (Name, CategoryId, [Level], SortOrder, IsActive) VALUES ('Beverages', 6, 0, 2, 1);
END
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes WHERE Name = 'Food')
BEGIN
    INSERT INTO dbo.ProductTypes (Name, CategoryId, [Level], SortOrder, IsActive) VALUES ('Food', 6, 0, 3, 1);
END
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes WHERE Name = 'Fragrance and Flavor')
BEGIN
    INSERT INTO dbo.ProductTypes (Name, CategoryId, [Level], SortOrder, IsActive) VALUES ('Fragrance and Flavor', 6, 0, 4, 1);
END

PRINT '  Product Types updated/inserted';
PRINT '';

-- ============================================
-- 4. Add Product Subcategories for Plastics
-- ============================================
PRINT '4. Adding Product Subcategories...';

-- Get Plastics ProductTypeId
DECLARE @PlasticsId INT;
SELECT @PlasticsId = Id FROM dbo.ProductTypes WHERE Name = 'Plastics';

IF @PlasticsId IS NOT NULL
BEGIN
    -- Insert subcategories if they don't exist
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductSubcategories WHERE Name = 'HDPE' AND ProductTypeId = @PlasticsId)
        INSERT INTO dbo.ProductSubcategories (Name, ProductTypeId, Description, SortOrder, IsActive) VALUES ('HDPE', @PlasticsId, 'High-Density Polyethylene', 1, 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductSubcategories WHERE Name = 'LDPE' AND ProductTypeId = @PlasticsId)
        INSERT INTO dbo.ProductSubcategories (Name, ProductTypeId, Description, SortOrder, IsActive) VALUES ('LDPE', @PlasticsId, 'Low-Density Polyethylene', 2, 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductSubcategories WHERE Name = 'LLDPE' AND ProductTypeId = @PlasticsId)
        INSERT INTO dbo.ProductSubcategories (Name, ProductTypeId, Description, SortOrder, IsActive) VALUES ('LLDPE', @PlasticsId, 'Linear Low-Density Polyethylene', 3, 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductSubcategories WHERE Name = 'PET' AND ProductTypeId = @PlasticsId)
        INSERT INTO dbo.ProductSubcategories (Name, ProductTypeId, Description, SortOrder, IsActive) VALUES ('PET', @PlasticsId, 'Polyethylene Terephthalate', 4, 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductSubcategories WHERE Name = 'PP' AND ProductTypeId = @PlasticsId)
        INSERT INTO dbo.ProductSubcategories (Name, ProductTypeId, Description, SortOrder, IsActive) VALUES ('PP', @PlasticsId, 'Polypropylene', 5, 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.ProductSubcategories WHERE Name = 'PVC' AND ProductTypeId = @PlasticsId)
        INSERT INTO dbo.ProductSubcategories (Name, ProductTypeId, Description, SortOrder, IsActive) VALUES ('PVC', @PlasticsId, 'Polyvinyl Chloride', 6, 1);

    PRINT '  Plastics subcategories added';
END
ELSE
BEGIN
    PRINT '  Warning: Plastics ProductType not found';
END

PRINT '';

-- ============================================
-- Summary
-- ============================================
PRINT '============================================';
PRINT 'FILTER DATA FIX COMPLETE';
PRINT '============================================';
PRINT '';

DECLARE @CertTypeCount INT, @CatCount INT, @TypeCount INT, @SubcatCount INT;
SELECT @CertTypeCount = COUNT(*) FROM dbo.CertificationTypes;
SELECT @CatCount = COUNT(*) FROM dbo.ProductCategories;
SELECT @TypeCount = COUNT(*) FROM dbo.ProductTypes;
SELECT @SubcatCount = COUNT(*) FROM dbo.ProductSubcategories;

PRINT 'Summary:';
PRINT '  - Certification Types: ' + CAST(@CertTypeCount AS VARCHAR) + ' records';
PRINT '  - Product Categories: ' + CAST(@CatCount AS VARCHAR) + ' records';
PRINT '  - Product Types: ' + CAST(@TypeCount AS VARCHAR) + ' records';
PRINT '  - Product Subcategories: ' + CAST(@SubcatCount AS VARCHAR) + ' records';
PRINT '';

-- Verify hierarchy
PRINT 'Verification - Certification Type Hierarchy:';
SELECT Name, [Level], ParentId FROM dbo.CertificationTypes WHERE Name IN ('Waste Diversion from Landfill', 'Waste System Audit') ORDER BY SortOrder;

PRINT '';
PRINT 'Verification - Product Categories:';
SELECT Id, Name FROM dbo.ProductCategories ORDER BY SortOrder;

PRINT '';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);

GO

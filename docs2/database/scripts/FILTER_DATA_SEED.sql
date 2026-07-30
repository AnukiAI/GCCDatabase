-- ============================================
-- FILTER DATA SEED SCRIPT
-- Seeds CertificationTypes, ProductCategories, ProductTypes, ProductSubcategories
-- Based on Design Requirements Document
-- Safe for use with existing data (uses MERGE pattern)
-- ============================================

PRINT '============================================';
PRINT 'FILTER DATA SEED SCRIPT';
PRINT 'Starting at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

-- ============================================
-- 1. Certification Types (Level 1 and Level 2)
-- Per design: "Waste System Audit" is Level 2 under "Waste Diversion from Landfill"
-- ============================================
PRINT '1. Seeding Certification Types...';

-- Use MERGE to insert or update certification types
MERGE INTO dbo.CertificationTypes AS target
USING (VALUES
    (1, 'Biobased', 'BIO', 'Biobased content certification', 1, 1, 1),
    (2, 'Biodegradable', 'BDG', 'Biodegradable product certification', 0, 2, 1),
    (3, 'Carbon Footprint Reduction', 'CFR', 'Carbon footprint reduction certification', 0, 3, 1),
    (4, 'Certified Energy Savings', 'CES', 'Energy savings certification', 1, 4, 1),
    (5, 'Certified Environmental Facts', 'CEF', 'Environmental impact certification', 1, 5, 1),
    (6, 'Certified Sustainability Facts', 'CSF', 'Comprehensive sustainability certification', 1, 6, 1),
    (7, 'Certified Water Savings', 'CWS', 'Water savings certification', 1, 7, 1),
    (8, 'Closed Loop Product', 'CLP', 'Closed loop product certification', 1, 8, 1),
    (9, 'Dematerialization', 'DEM', 'Dematerialization certification', 1, 9, 1),
    (10, 'Greenhouse Gas (GHG) Verification', 'GHG', 'GHG verification', 1, 10, 1),
    (11, 'GreenCircle Audited', 'GCA', 'GreenCircle audited certification', 1, 11, 1),
    (12, 'Life Cycle Assessment (LCA) Optimized', 'LCA', 'LCA optimized certification', 1, 12, 1),
    (13, 'Manufacturer Inventory Verification', 'MIV', 'Manufacturer inventory verification', 1, 13, 1),
    (14, 'Recyclable Material', 'RCM', 'Recyclable material certification', 1, 14, 1),
    (15, 'Recycled Content', 'RC', 'Recycled content certification', 1, 15, 1),
    (16, 'Renewable Resource Content', 'RRC', 'Renewable resource content', 1, 16, 1),
    (17, 'Sustainable Energy Practices', 'SEP', 'Sustainable energy practices', 0, 17, 1),
    (18, 'Sustainable Manufacturing Practices', 'SMP', 'Sustainable manufacturing practices', 0, 18, 1),
    (19, 'Waste Diversion from Landfill', 'WDL', 'Waste diversion from landfill certification', 1, 19, 1),
    (20, 'Waste System Audit', 'WSA', 'Waste system audit (Level 2 under Waste Diversion)', 1, 20, 2),
    (21, '+Vantage Vinyl Verified', 'VVV', '+Vantage Vinyl verified certification', 1, 21, 1),
    (22, 'Preloved', 'PRL', 'Preloved certification', 0, 22, 1)
) AS source (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level])
ON target.Name = source.Name
WHEN MATCHED THEN
    UPDATE SET
        AmazonCode = source.AmazonCode,
        Description = source.Description,
        IsActive = source.IsActive,
        SortOrder = source.SortOrder,
        [Level] = source.[Level]
WHEN NOT MATCHED THEN
    INSERT (Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (source.Name, source.AmazonCode, source.Description, source.IsActive, source.SortOrder, source.[Level], NULL);

-- Set Waste System Audit ParentId to Waste Diversion from Landfill
DECLARE @WasteDiversionId INT;
SELECT @WasteDiversionId = Id FROM dbo.CertificationTypes WHERE Name = 'Waste Diversion from Landfill';

UPDATE dbo.CertificationTypes
SET ParentId = @WasteDiversionId
WHERE Name = 'Waste System Audit';

-- Ensure all other types have NULL ParentId and Level 1 (except Waste System Audit)
UPDATE dbo.CertificationTypes
SET ParentId = NULL, [Level] = 1
WHERE Name != 'Waste System Audit';

UPDATE dbo.CertificationTypes
SET [Level] = 2
WHERE Name = 'Waste System Audit';

PRINT '  Certification Types seeded/updated';
PRINT '';

-- ============================================
-- 2. Product Categories (Level 1)
-- ============================================
PRINT '2. Seeding Product Categories...';

-- First, clear subcategories and types to avoid FK conflicts
DELETE FROM dbo.ProductSubcategories;
DELETE FROM dbo.ProductTypes;
DELETE FROM dbo.ProductCategories;

-- Reset identity seeds
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.ProductCategories'))
    DBCC CHECKIDENT ('dbo.ProductCategories', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.ProductTypes'))
    DBCC CHECKIDENT ('dbo.ProductTypes', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.ProductSubcategories'))
    DBCC CHECKIDENT ('dbo.ProductSubcategories', RESEED, 0);

SET IDENTITY_INSERT dbo.ProductCategories ON;

INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive)
VALUES
    (1, 'Chemicals and Raw Materials', 'Chemical products and raw materials', 1, 1),
    (2, 'Clothing, Apparel, and Fabrics', 'Clothing, apparel, and fabric products', 2, 1),
    (3, 'Construction / Building Products', 'Construction and building products', 3, 1),
    (4, 'Consumer Products', 'Consumer products', 4, 1),
    (5, 'Electronics, Computers, and Accessories', 'Electronics, computers, and accessories', 5, 1),
    (6, 'Food and Beverage', 'Food and beverage products', 6, 1);

SET IDENTITY_INSERT dbo.ProductCategories OFF;
PRINT '  6 Product Categories created';
PRINT '';

-- ============================================
-- 3. Product Types (Level 2)
-- ============================================
PRINT '3. Seeding Product Types...';

SET IDENTITY_INSERT dbo.ProductTypes ON;

-- Chemicals and Raw Materials (CategoryId = 1)
INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
VALUES
    (1, 'Additives', 1, NULL, 0, 1, 1),
    (2, 'Metal Alloys', 1, NULL, 0, 2, 1),
    (3, 'Minerals', 1, NULL, 0, 3, 1),
    (4, 'Plastics', 1, NULL, 0, 4, 1);

-- Clothing, Apparel, and Fabrics (CategoryId = 2)
INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
VALUES
    (5, 'Apparel', 2, NULL, 0, 1, 1),
    (6, 'Bags and Accessories', 2, NULL, 0, 2, 1),
    (7, 'Jewelry', 2, NULL, 0, 3, 1);

-- Construction / Building Products (CategoryId = 3)
INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
VALUES
    (8, 'Adhesives and Sealants', 3, NULL, 0, 1, 1),
    (9, 'Ceramics and Tile', 3, NULL, 0, 2, 1),
    (10, 'Coatings', 3, NULL, 0, 3, 1),
    (11, 'Concrete and Concrete Materials', 3, NULL, 0, 4, 1),
    (12, 'Doors, Door Hardware, and Accessories', 3, NULL, 0, 5, 1),
    (13, 'Fiberglass Mats', 3, NULL, 0, 6, 1),
    (14, 'Flooring', 3, NULL, 0, 7, 1),
    (15, 'Grout and Mortar', 3, NULL, 0, 8, 1),
    (16, 'HVAC', 3, NULL, 0, 9, 1),
    (17, 'Insulation', 3, NULL, 0, 10, 1),
    (18, 'Moisture Control', 3, NULL, 0, 11, 1),
    (19, 'Plumbing and Accessories', 3, NULL, 0, 12, 1),
    (20, 'Roofing', 3, NULL, 0, 13, 1),
    (21, 'Wallcoverings and Interior Wall Finishes', 3, NULL, 0, 14, 1),
    (22, 'Windows, Parts, and Accessories', 3, NULL, 0, 15, 1),
    (23, 'Wiring and Cable', 3, NULL, 0, 16, 1);

-- Consumer Products (CategoryId = 4)
INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
VALUES
    (24, 'Appliances', 4, NULL, 0, 1, 1),
    (25, 'Baby Products', 4, NULL, 0, 2, 1),
    (26, 'Cleaning Supplies', 4, NULL, 0, 3, 1),
    (27, 'Furniture', 4, NULL, 0, 4, 1),
    (28, 'Health and Beauty', 4, NULL, 0, 5, 1),
    (29, 'Home Goods', 4, NULL, 0, 6, 1),
    (30, 'Office Supplies (non-electronic)', 4, NULL, 0, 7, 1),
    (31, 'Personal Protective Equipment', 4, NULL, 0, 8, 1),
    (32, 'Pet Products', 4, NULL, 0, 9, 1),
    (33, 'Security', 4, NULL, 0, 10, 1);

-- Electronics, Computers, and Accessories (CategoryId = 5)
INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
VALUES
    (34, 'Card Reader', 5, NULL, 0, 1, 1),
    (35, 'Electronic Accessories', 5, NULL, 0, 2, 1),
    (36, 'Microelectronics', 5, NULL, 0, 3, 1),
    (37, 'Printers and Printer Cartridges', 5, NULL, 0, 4, 1);

-- Food and Beverage (CategoryId = 6)
INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
VALUES
    (38, 'Animal Products', 6, NULL, 0, 1, 1),
    (39, 'Beverages', 6, NULL, 0, 2, 1),
    (40, 'Food', 6, NULL, 0, 3, 1),
    (41, 'Fragrance and Flavor', 6, NULL, 0, 4, 1);

SET IDENTITY_INSERT dbo.ProductTypes OFF;
PRINT '  41 Product Types created';
PRINT '';

-- ============================================
-- 4. Product Subcategories (Level 3)
-- Only Plastics has subcategories per design
-- ============================================
PRINT '4. Seeding Product Subcategories...';

SET IDENTITY_INSERT dbo.ProductSubcategories ON;

-- Plastics subcategories (ProductTypeId = 4)
INSERT INTO dbo.ProductSubcategories (Id, Name, ProductTypeId, Description, SortOrder, IsActive)
VALUES
    (1, 'HDPE', 4, 'High-Density Polyethylene', 1, 1),
    (2, 'LDPE', 4, 'Low-Density Polyethylene', 2, 1),
    (3, 'LLDPE', 4, 'Linear Low-Density Polyethylene', 3, 1),
    (4, 'PET', 4, 'Polyethylene Terephthalate', 4, 1),
    (5, 'PP', 4, 'Polypropylene', 5, 1),
    (6, 'PVC', 4, 'Polyvinyl Chloride', 6, 1);

SET IDENTITY_INSERT dbo.ProductSubcategories OFF;
PRINT '  6 Product Subcategories created (under Plastics)';
PRINT '';

-- ============================================
-- Summary
-- ============================================
PRINT '============================================';
PRINT 'FILTER DATA SEED COMPLETE';
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
PRINT 'Certification Type Hierarchy Verification:';
SELECT Name, [Level], ParentId FROM dbo.CertificationTypes WHERE Name IN ('Waste Diversion from Landfill', 'Waste System Audit') ORDER BY SortOrder;

PRINT '';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);

GO

-- ============================================
-- GCC Client Test Data Seeding Script
-- ============================================
-- This script CLEANS and seeds client-provided test data
--
-- IMPORTANT: This script DELETES ALL EXISTING CERTIFICATES before seeding!
--
-- Source: Client-provided sample data (January 2026)
-- Total Certificates: 22
-- Total Companies: 21
--
-- Database: SQL Server
-- Safe to run multiple times (idempotent with cleanup)
-- ============================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'GCC Client Test Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- CLEANUP EXISTING DATA
-- ============================================
PRINT 'STEP 0: Cleaning existing certificate data...';
PRINT '';

-- Delete certificate dependencies first
PRINT '0.1. Cleaning certificate dependencies...';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateProductIds')
    DELETE FROM dbo.CertificateProductIds;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateTags')
    DELETE FROM dbo.CertificateTags;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateStatusHistory')
    DELETE FROM dbo.CertificateStatusHistory;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomFieldValues')
    DELETE FROM dbo.CustomFieldValues;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateDocuments')
    DELETE FROM dbo.CertificateDocuments;
PRINT '  Certificate dependencies cleaned';

-- Delete certificates
PRINT '0.2. Cleaning certificates...';
DELETE FROM dbo.Certificates;
PRINT '  Certificates cleaned';

-- Delete products (will recreate with client data)
PRINT '0.3. Cleaning products...';
DELETE FROM dbo.Products;
PRINT '  Products cleaned';

-- Delete companies (will recreate with client data)
PRINT '0.4. Cleaning companies...';
DELETE FROM dbo.Companies;
PRINT '  Companies cleaned';

-- Delete product types (will recreate)
PRINT '0.5. Cleaning product types...';
DELETE FROM dbo.ProductTypes;
PRINT '  Product types cleaned';

-- Delete certification types (will recreate)
PRINT '0.6. Cleaning certification types...';
DELETE FROM dbo.CertificationTypes;
PRINT '  Certification types cleaned';

PRINT '';
PRINT 'Cleanup completed';
PRINT '';

-- ============================================
-- 1. Product Categories (keep existing or create minimal set)
-- ============================================
PRINT '1. Ensuring Product Categories exist...';

IF NOT EXISTS (SELECT 1 FROM dbo.ProductCategories WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.ProductCategories ON;
    INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive)
    VALUES (1, 'General', 'General product category', 1, 1);
    SET IDENTITY_INSERT dbo.ProductCategories OFF;
    PRINT '  Created General category';
END
ELSE
    PRINT '  Product Categories already exist';
PRINT '';

-- ============================================
-- 2. Certification Types (10 types from client data)
-- ============================================
PRINT '2. Seeding Certification Types...';

SET IDENTITY_INSERT dbo.CertificationTypes ON;

INSERT INTO dbo.CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
VALUES
    (1, 'Recycled Content', 'RC', 'Recycled content certification', 1, 1, 1, NULL),
    (2, 'GreenCircle Audited', 'GCA', 'GreenCircle audited certification', 1, 2, 1, NULL),
    (3, 'Certified Environmental Facts', 'CEF', 'Environmental impact certification', 1, 3, 1, NULL),
    (4, 'Waste Diversion from Landfill', 'WDL', 'Waste diversion certification', 1, 4, 1, NULL),
    (5, 'Carbon Footprint Reduction', 'CFR', 'Carbon footprint reduction certification', 1, 5, 1, NULL),
    (6, 'Material Ingredient Reporting', 'MIR', 'Material composition reporting', 1, 6, 1, NULL),
    (7, 'Certified Sustainability Facts', 'CSF', 'Comprehensive sustainability certification', 1, 7, 1, NULL),
    (8, 'LCA Optimized', 'LCA', 'Life cycle assessment optimized', 1, 8, 1, NULL),
    (9, 'Certified Energy Savings', 'CES', 'Energy efficiency certification', 1, 9, 1, NULL),
    (10, 'Closed Loop Product', 'CLP', 'Closed loop product certification', 1, 10, 1, NULL);

SET IDENTITY_INSERT dbo.CertificationTypes OFF;
PRINT '  10 Certification Types created';
PRINT '';

-- ============================================
-- 3. Product Types (from client data)
-- ============================================
PRINT '3. Seeding Product Types...';

SET IDENTITY_INSERT dbo.ProductTypes ON;

INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId)
VALUES
    (1, 'Packaging', 1, 1, 1, 1, NULL),
    (2, 'General', 1, 1, 2, 1, NULL),
    (3, 'Wipes', 1, 1, 3, 1, NULL),
    (4, 'Consumer Goods', 1, 1, 4, 1, NULL),
    (5, 'Polymer Modified Mortars', 1, 1, 5, 1, NULL),
    (6, 'Consumer product', 1, 1, 6, 1, NULL),
    (7, 'Food', 1, 1, 7, 1, NULL),
    (8, 'Plastic', 1, 1, 8, 1, NULL),
    (9, 'Asphalt sealer', 1, 1, 9, 1, NULL),
    (10, 'Pork Products', 1, 1, 10, 1, NULL),
    (11, 'Plastic Resins', 1, 1, 11, 1, NULL),
    (12, 'Door', 1, 1, 12, 1, NULL),
    (13, 'Card Reader', 1, 1, 13, 1, NULL),
    (14, 'Plastic film', 1, 1, 14, 1, NULL),
    (15, 'Consumer Products', 1, 1, 15, 1, NULL);

SET IDENTITY_INSERT dbo.ProductTypes OFF;
PRINT '  15 Product Types created';
PRINT '';

-- ============================================
-- 4. Companies (21 companies from client data)
-- ============================================
PRINT '4. Seeding Companies...';

SET IDENTITY_INSERT dbo.Companies ON;

INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES
    (1, 'Sofidel America Corp.', NULL, 'Horsham, PA', 'United States', 1, 0, GETDATE(), GETDATE()),
    (2, 'IB Roof Systems', NULL, NULL, 'United States', 1, 0, GETDATE(), GETDATE()),
    (3, 'DUDE Wipes', NULL, 'Springdale, AR', 'United States', 1, 0, GETDATE(), GETDATE()),
    (4, 'Cox Automotive', NULL, 'Riverside, CA', 'United States', 1, 0, GETDATE(), GETDATE()),
    (5, 'Procter & Gamble', NULL, 'Gyongyos, Hungary', 'Hungary', 1, 0, GETDATE(), GETDATE()),
    (6, 'Philips', NULL, 'Batam, Indonesia', 'Indonesia', 1, 0, GETDATE(), GETDATE()),
    (7, 'MAPEI', NULL, 'Deerfield Beach, FL', 'United States', 1, 0, GETDATE(), GETDATE()),
    (8, 'HanesBrands Inc.', NULL, 'El Salvador', 'El Salvador', 1, 0, GETDATE(), GETDATE()),
    (9, 'General Mills, Inc.', NULL, 'Buffalo, NY', 'United States', 1, 0, GETDATE(), GETDATE()),
    (10, 'Cox Communications', NULL, NULL, 'United States', 1, 0, GETDATE(), GETDATE()),
    (11, 'Glad', NULL, 'Amherst, VA', 'United States', 1, 0, GETDATE(), GETDATE()),
    (12, 'Asphalt System, Inc.', NULL, 'Salt Lake City, UT', 'United States', 1, 0, GETDATE(), GETDATE()),
    (13, 'Copper Development Association', NULL, NULL, 'United States', 1, 0, GETDATE(), GETDATE()),
    (14, 'Greenfield Natural Meat Co.', NULL, 'Manitoba, Canada', 'Canada', 1, 0, GETDATE(), GETDATE()),
    (15, 'Cox Enterprises', NULL, 'Atlanta, GA', 'United States', 1, 0, GETDATE(), GETDATE()),
    (16, 'Sika Corporation', NULL, 'Lyndhurst, NJ', 'United States', 1, 0, GETDATE(), GETDATE()),
    (17, 'Everrank Inc.', NULL, 'Tijuana, MX', 'Mexico', 1, 0, GETDATE(), GETDATE()),
    (18, 'Gunnebo Entrance Control Ltd.', NULL, 'United Kingdom', 'United Kingdom', 1, 0, GETDATE(), GETDATE()),
    (19, 'HID Global', NULL, 'Cardiff, Wales, United Kingdom', 'United Kingdom', 1, 0, GETDATE(), GETDATE()),
    (20, 'SPR Packaging LLC', NULL, 'Rockwall, TX', 'United States', 1, 0, GETDATE(), GETDATE()),
    (21, 'Clover Imaging Group', NULL, 'Ottawa, IL', 'United States', 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT dbo.Companies OFF;
PRINT '  21 Companies created';
PRINT '';

-- ============================================
-- 5. Products (from client data + Not Specified placeholder)
-- ============================================
PRINT '5. Seeding Products...';

SET IDENTITY_INSERT dbo.Products ON;

INSERT INTO dbo.Products (Id, Name, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES
    -- Product ID 1: Not Specified (placeholder for blank products)
    (1, 'Not Specified', 1, 2, NULL, 1, 0, GETDATE(), GETDATE()),
    -- Actual products from client data
    (2, 'Plastic Film Packaging', 1, 1, 1, 1, 0, GETDATE(), GETDATE()),
    (3, '+Vantage Vinyl Verified', 1, 2, 2, 1, 0, GETDATE(), GETDATE()),
    (4, 'DUDE Wipes Flushable Wipes', 1, 3, 3, 1, 0, GETDATE(), GETDATE()),
    (5, 'Electric Toothbrush Handles', 1, 4, 5, 1, 0, GETDATE(), GETDATE()),
    (6, 'Bottles', 1, 2, 6, 1, 0, GETDATE(), GETDATE()),
    (7, 'Ker 111 Gray & White', 1, 5, 7, 1, 0, GETDATE(), GETDATE()),
    (8, 'Textiles', 1, 6, 8, 1, 0, GETDATE(), GETDATE()),
    (9, 'Various Lucky Charms Cereals', 1, 7, 9, 1, 0, GETDATE(), GETDATE()),
    (10, 'Drawstring Bags', 1, 8, 11, 1, 0, GETDATE(), GETDATE()),
    (11, 'GSB-88', 1, 9, 12, 1, 0, GETDATE(), GETDATE()),
    (12, 'Select Greenfield Natural Meat Co. Pork Products Sold in the United States', 1, 10, 14, 1, 0, GETDATE(), GETDATE()),
    (13, 'Sika Injection-310 US', 1, 2, 16, 1, 0, GETDATE(), GETDATE()),
    (14, 'PET Clear PCR Thermoform FDA Resin', 1, 11, 17, 1, 0, GETDATE(), GETDATE()),
    (15, 'GlasStileS', 1, 12, 18, 1, 0, GETDATE(), GETDATE()),
    (16, 'HID Signo readers', 1, 13, 19, 1, 0, GETDATE(), GETDATE()),
    (17, 'SOF001-132245KirkPCR', 1, 14, 20, 1, 0, GETDATE(), GETDATE()),
    (18, 'Remanufactured Ink Cartridges', 1, 15, 21, 1, 0, GETDATE(), GETDATE()),
    (19, 'Remanufactured Laser Cartridges', 1, 15, 21, 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT dbo.Products OFF;
PRINT '  19 Products created (including Not Specified placeholder)';
PRINT '';

-- ============================================
-- 6. Certificates (22 certificates from client data)
-- ============================================
PRINT '6. Seeding Certificates...';

SET IDENTITY_INSERT dbo.Certificates ON;

INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, CompanyId, ProductId, CertificationTypeId, Partner, CreatedAt, UpdatedAt)
VALUES
    -- 23-0358: Sofidel America Corp. - Plastic Film Packaging - Recycled Content
    (1, '23-0358', 'active', '2023-06-01', '2099-12-31', 'Horsham, PA', 1, 2, 1, 'None', GETDATE(), GETDATE()),

    -- 23-0375: IB Roof Systems - +Vantage Vinyl Verified - GreenCircle Audited
    (2, '23-0375', 'active', '2023-06-01', '2026-05-31', NULL, 2, 3, 2, 'None', GETDATE(), GETDATE()),

    -- 23-0753: DUDE Wipes - DUDE Wipes Flushable Wipes - Certified Environmental Facts
    (3, '23-0753', 'active', '2023-10-01', '2026-09-30', 'Springdale, AR', 3, 4, 3, 'None', GETDATE(), GETDATE()),

    -- 23-0923: Cox Automotive - Not Specified - Waste Diversion from Landfill
    (4, '23-0923', 'active', '2023-11-01', '2026-10-31', 'Riverside, CA', 4, 1, 4, 'None', GETDATE(), GETDATE()),

    -- 23-0925: Procter & Gamble - Electric Toothbrush Handles - Carbon Footprint Reduction
    (5, '23-0925', 'active', '2023-11-01', '2026-10-31', 'Gyongyos, Hungary and Marktheidenfeld, Germany', 5, 5, 5, 'None', GETDATE(), GETDATE()),

    -- 23-1048: Philips - Bottles - Certified Environmental Facts
    (6, '23-1048', 'active', '2023-11-01', '2026-10-31', 'Batam, Indonesia', 6, 6, 3, 'None', GETDATE(), GETDATE()),

    -- 24-0028: MAPEI - Ker 111 Gray & White - Material Ingredient Reporting
    (7, '24-0028', 'active', '2024-03-01', '2026-02-28', 'Deerfield Beach, FL', 7, 7, 6, 'None', GETDATE(), GETDATE()),

    -- 24-0298: HanesBrands Inc. - Textiles - Carbon Footprint Reduction
    (8, '24-0298', 'active', '2024-04-08', '2027-04-07', 'El Salvador', 8, 8, 5, 'None', GETDATE(), GETDATE()),

    -- 24-0304: General Mills, Inc. - Various Lucky Charms Cereals - Certified Environmental Facts
    (9, '24-0304', 'active', '2024-04-01', '2027-03-31', 'Buffalo, NY; Cedar Rapids, IA', 9, 9, 3, 'None', GETDATE(), GETDATE()),

    -- 24-0306: Cox Communications - Not Specified - GreenCircle Audited
    (10, '24-0306', 'active', '2023-11-01', '2026-10-31', NULL, 10, 1, 2, 'None', GETDATE(), GETDATE()),

    -- 24-0321: Glad - Drawstring Bags - Carbon Footprint Reduction
    (11, '24-0321', 'active', '2024-06-01', '2027-05-31', 'Amherst, VA & Rogers, AR', 11, 10, 5, 'None', GETDATE(), GETDATE()),

    -- 24-0347: Asphalt System, Inc. - GSB-88 - LCA Optimized
    (12, '24-0347', 'active', '2024-06-01', '2027-05-31', 'Salt Lake City, UT', 12, 11, 8, 'None', GETDATE(), GETDATE()),

    -- 24-0353: Copper Development Association - Not Specified - Recycled Content (missing dates - using 2025 defaults)
    (13, '24-0353', 'active', '2025-01-01', '2025-12-31', NULL, 13, 1, 1, 'None', GETDATE(), GETDATE()),

    -- 24-0485: Greenfield Natural Meat Co. - Pork Products - Certified Sustainability Facts
    (14, '24-0485', 'active', '2024-06-01', '2027-05-31', 'Manitoba, Ontario, Saskatchewan, and Quebec, Canada', 14, 12, 7, 'None', GETDATE(), GETDATE()),

    -- 24-0550: Cox Enterprises - Not Specified - Waste Diversion from Landfill
    (15, '24-0550', 'active', '2024-07-01', '2027-06-30', 'Atlanta, GA', 15, 1, 4, 'None', GETDATE(), GETDATE()),

    -- 24-1597: Sika Corporation - Sika Injection-310 US - Material Ingredient Reporting
    (16, '24-1597', 'active', '2024-07-01', '2026-06-30', 'Lyndhurst, NJ', 16, 13, 6, 'None', GETDATE(), GETDATE()),

    -- 25-0123: Everrank Inc. - PET Clear PCR Thermoform FDA Resin - Recycled Content
    (17, '25-0123', 'active', '2025-02-01', '2026-01-31', 'Tijuana, MX', 17, 14, 1, 'None', GETDATE(), GETDATE()),

    -- 25-0124: Gunnebo Entrance Control Ltd. - GlasStileS - Certified Energy Savings
    (18, '25-0124', 'active', '2025-04-14', '2026-04-13', 'United Kingdom', 18, 15, 9, 'None', GETDATE(), GETDATE()),

    -- 25-0245: HID Global - HID Signo readers - Certified Energy Savings
    (19, '25-0245', 'active', '2025-07-01', '2026-06-30', 'Cardiff, Wales, United Kingdom', 19, 16, 9, 'None', GETDATE(), GETDATE()),

    -- 25-0246: SPR Packaging LLC - SOF001-132245KirkPCR - Recycled Content
    (20, '25-0246', 'active', '2025-05-01', '2026-04-30', 'Rockwall, TX', 20, 17, 1, 'None', GETDATE(), GETDATE()),

    -- 25-8484: Clover Imaging Group - Remanufactured Ink Cartridges - Closed Loop Product
    (21, '25-8484', 'active', '2025-12-01', '2026-11-30', 'Ottawa, IL', 21, 18, 10, 'None', GETDATE(), GETDATE()),

    -- 25-8485: Clover Imaging Group - Remanufactured Laser Cartridges - Closed Loop Product
    (22, '25-8485', 'active', '2025-12-01', '2026-11-30', 'Ottawa, IL', 21, 19, 10, 'None', GETDATE(), GETDATE());

SET IDENTITY_INSERT dbo.Certificates OFF;
PRINT '  22 Certificates created';
PRINT '';

-- ============================================
-- COMMIT TRANSACTION
-- ============================================
COMMIT TRANSACTION;

PRINT '============================================';
PRINT 'Client Test Data Seeding COMPLETED';
PRINT 'Finished at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - Certification Types: 10';
PRINT '  - Product Types: 15';
PRINT '  - Companies: 21';
PRINT '  - Products: 19';
PRINT '  - Certificates: 22';
PRINT '';

END TRY
BEGIN CATCH
    -- Rollback on error
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED - TRANSACTION ROLLED BACK';
    PRINT '============================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '============================================';

    -- Re-throw the error
    THROW;
END CATCH;
GO

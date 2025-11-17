-- ============================================
-- GCC Production Certificate Data Seeding
-- ============================================
-- Fixed for production SQL Server compatibility
-- Includes proper timestamps and all required fields
-- ============================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'GCC Production Certificate Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- 1. Companies (Required for Certificates)
-- ============================================
PRINT '1. Seeding Companies...';

IF NOT EXISTS (SELECT 1 FROM dbo.Companies)
BEGIN
    SET IDENTITY_INSERT dbo.Companies ON;

    INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt)
    VALUES
        (1, 'CertainTeed', 'info@certainteed.com', 'Malvern, PA', 'United States', 1, 0, 'https://www.certainteed.com', GETDATE(), GETDATE()),
        (2, 'Owens Corning', 'contact@owenscorning.com', 'Toledo, OH', 'United States', 1, 0, 'https://www.owenscorning.com', GETDATE(), GETDATE()),
        (3, 'Johns Manville', 'info@jm.com', 'Denver, CO', 'United States', 1, 0, 'https://www.jm.com', GETDATE(), GETDATE()),
        (4, 'Interface', 'contact@interface.com', 'Atlanta, GA', 'United States', 1, 0, 'https://www.interface.com', GETDATE(), GETDATE()),
        (5, 'Shaw Industries', 'info@shawinc.com', 'Dalton, GA', 'United States', 1, 0, 'https://www.shawfloors.com', GETDATE(), GETDATE()),
        (6, 'Mohawk Industries', 'contact@mohawkind.com', 'Calhoun, GA', 'United States', 1, 0, 'https://www.mohawkind.com', GETDATE(), GETDATE()),
        (7, 'Philips Lighting', 'info@signify.com', 'Eindhoven', 'Netherlands', 1, 0, 'https://www.signify.com', GETDATE(), GETDATE()),
        (8, 'Procter & Gamble', 'contact@pg.com', 'Cincinnati, OH', 'United States', 1, 0, 'https://www.pg.com', GETDATE(), GETDATE()),
        (9, 'Unilever', 'info@unilever.com', 'London', 'United Kingdom', 1, 0, 'https://www.unilever.com', GETDATE(), GETDATE()),
        (10, 'Herman Miller', 'contact@hermanmiller.com', 'Zeeland, MI', 'United States', 1, 0, 'https://www.hermanmiller.com', GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Companies OFF;
    PRINT '✓ 10 Companies created';
END
ELSE
    PRINT '○ Companies already exist, skipping';
PRINT '';

-- ============================================
-- 2. Products (Required for Certificates)
-- ============================================
PRINT '2. Seeding Products...';

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    DECLARE @BuildingMaterialsCategoryId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Building Materials');
    DECLARE @TextilesCategoryId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Textiles');
    DECLARE @ElectronicsCategoryId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Electronics / Electrical');
    DECLARE @ConsumerCategoryId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Consumer Products');

    SET IDENTITY_INSERT dbo.Products ON;

    INSERT INTO dbo.Products (Id, Name, Description, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (1, 'EcoTouch Insulation', 'Sustainable fiberglass insulation', @BuildingMaterialsCategoryId, 2, 1, 0, GETDATE(), GETDATE()),
        (2, 'Sustainable Insulation Batts', 'Recycled content insulation', @BuildingMaterialsCategoryId, 1, 1, 0, GETDATE(), GETDATE()),
        (3, 'Climate Pro Insulation', 'High R-value eco insulation', @BuildingMaterialsCategoryId, 3, 1, 0, GETDATE(), GETDATE()),
        (4, 'TacTiles Carpet Tiles', 'Modular carpet with recycled content', @TextilesCategoryId, 4, 1, 0, GETDATE(), GETDATE()),
        (5, 'EcoWorx Carpet', 'PVC-free carpet backing', @TextilesCategoryId, 5, 1, 0, GETDATE(), GETDATE()),
        (6, 'SmartStrand Carpet', 'Bio-based carpet fiber', @TextilesCategoryId, 6, 1, 0, GETDATE(), GETDATE()),
        (7, 'LED Tube Lights', 'Energy efficient LED lighting', @ElectronicsCategoryId, 7, 1, 0, GETDATE(), GETDATE()),
        (8, 'Tide Purclean Detergent', 'Plant-based laundry detergent', @ConsumerCategoryId, 8, 1, 0, GETDATE(), GETDATE()),
        (9, 'Seventh Generation Cleaner', 'Eco-friendly cleaning products', @ConsumerCategoryId, 9, 1, 0, GETDATE(), GETDATE()),
        (10, 'Aeron Office Chair', 'Sustainable office furniture', @ConsumerCategoryId, 10, 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Products OFF;
    PRINT '✓ 10 Products created';
END
ELSE
    PRINT '○ Products already exist, skipping';
PRINT '';

-- ============================================
-- 3. Certificates (Main Data)
-- ============================================
PRINT '3. Seeding Certificates...';

IF NOT EXISTS (SELECT 1 FROM dbo.Certificates)
BEGIN
    DECLARE @CEF_TypeId INT = (SELECT Id FROM CertificationTypes WHERE AmazonCode = 'CEF');
    DECLARE @CSF_TypeId INT = (SELECT Id FROM CertificationTypes WHERE AmazonCode = 'CSF');
    DECLARE @RC_TypeId INT = (SELECT Id FROM CertificationTypes WHERE AmazonCode = 'RC');

    SET IDENTITY_INSERT dbo.Certificates ON;

    -- Active Certificates for Public Viewing
    INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country,
                                   CompanyId, ProductId, CertificationTypeId, Partner,
                                   CreatedAt, UpdatedAt)
    VALUES
        -- CertainTeed Products
        (1, '24-0001', 'Active', '2024-01-15', '2027-01-15', 'Malvern, PA', 'United States',
         1, 2, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (2, '24-0002', 'Active', '2024-02-01', '2027-02-01', 'Malvern, PA', 'United States',
         1, 2, @CSF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (3, '24-0003', 'Active', '2024-02-15', '2027-02-15', 'Malvern, PA', 'United States',
         1, 2, @RC_TypeId, 'None', GETDATE(), GETDATE()),

        -- Owens Corning Products
        (4, '24-0010', 'Active', '2024-01-20', '2027-01-20', 'Toledo, OH', 'United States',
         2, 1, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (5, '24-0011', 'Active', '2024-03-01', '2027-03-01', 'Toledo, OH', 'United States',
         2, 1, @CSF_TypeId, 'Wayfair', GETDATE(), GETDATE()),
        (6, '24-0012', 'Active', '2024-03-15', '2027-03-15', 'Toledo, OH', 'United States',
         2, 1, @RC_TypeId, 'None', GETDATE(), GETDATE()),

        -- Johns Manville Products
        (7, '24-0020', 'Active', '2024-02-10', '2027-02-10', 'Denver, CO', 'United States',
         3, 3, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (8, '24-0021', 'Active', '2024-04-01', '2027-04-01', 'Denver, CO', 'United States',
         3, 3, @CSF_TypeId, 'None', GETDATE(), GETDATE()),

        -- Interface Carpet Tiles
        (9, '24-0100', 'Active', '2024-01-05', '2027-01-05', 'Atlanta, GA', 'United States',
         4, 4, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (10, '24-0101', 'Active', '2024-02-20', '2027-02-20', 'Atlanta, GA', 'United States',
          4, 4, @CSF_TypeId, 'Wayfair', GETDATE(), GETDATE()),
        (11, '24-0102', 'Active', '2024-03-10', '2027-03-10', 'Atlanta, GA', 'United States',
          4, 4, @RC_TypeId, 'None', GETDATE(), GETDATE()),

        -- Shaw Industries Carpet
        (12, '24-0110', 'Active', '2024-01-25', '2027-01-25', 'Dalton, GA', 'United States',
          5, 5, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (13, '24-0111', 'Active', '2024-03-05', '2027-03-05', 'Dalton, GA', 'United States',
          5, 5, @CSF_TypeId, 'Amazon', GETDATE(), GETDATE()),

        -- Mohawk Industries
        (14, '24-0120', 'Active', '2024-02-05', '2027-02-05', 'Calhoun, GA', 'United States',
          6, 6, @CEF_TypeId, 'Wayfair', GETDATE(), GETDATE()),
        (15, '24-0121', 'Active', '2024-04-10', '2027-04-10', 'Calhoun, GA', 'United States',
          6, 6, @RC_TypeId, 'None', GETDATE(), GETDATE()),

        -- Philips Lighting
        (16, '24-0200', 'Active', '2024-01-10', '2027-01-10', 'Eindhoven', 'Netherlands',
          7, 7, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (17, '24-0201', 'Active', '2024-03-20', '2027-03-20', 'Eindhoven', 'Netherlands',
          7, 7, @CSF_TypeId, 'None', GETDATE(), GETDATE()),

        -- P&G Products
        (18, '24-0300', 'Active', '2024-02-25', '2027-02-25', 'Cincinnati, OH', 'United States',
          8, 8, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),
        (19, '24-0301', 'Active', '2024-04-15', '2027-04-15', 'Cincinnati, OH', 'United States',
          8, 8, @RC_TypeId, 'None', GETDATE(), GETDATE()),

        -- Unilever Products
        (20, '24-0400', 'Active', '2024-01-30', '2027-01-30', 'London', 'United Kingdom',
          9, 9, @CEF_TypeId, 'Amazon', GETDATE(), GETDATE()),

        -- Herman Miller Furniture
        (21, '24-0500', 'Active', '2024-03-25', '2027-03-25', 'Zeeland, MI', 'United States',
          10, 10, @CEF_TypeId, 'Wayfair', GETDATE(), GETDATE()),
        (22, '24-0501', 'Active', '2024-04-20', '2027-04-20', 'Zeeland, MI', 'United States',
          10, 10, @CSF_TypeId, 'Wayfair', GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Certificates OFF;
    PRINT '✓ 22 Active Certificates created for public viewing';
END
ELSE
    PRINT '○ Certificates already exist, skipping';
PRINT '';

-- ============================================
-- 4. Commit Transaction
-- ============================================
PRINT 'Committing changes...';
COMMIT TRANSACTION;
PRINT '✓ Transaction committed successfully';
PRINT '';

-- ============================================
-- 5. Verification
-- ============================================
PRINT '============================================';
PRINT 'VERIFICATION';
PRINT '============================================';

SELECT 'Companies' AS EntityType, COUNT(*) AS Count FROM Companies
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL
SELECT 'Active Certificates', COUNT(*) FROM Certificates WHERE Status = 'Active';

PRINT '';
PRINT '============================================';
PRINT 'CERTIFICATE DATA SEEDING COMPLETE';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Restart your GCC Web Application (IIS recycle or touch web.config)';
PRINT '2. Visit https://gccwebapp.com/ to see certificate tiles';
PRINT '3. You should now see 22 active certificates displaying';
PRINT '============================================';

END TRY

BEGIN CATCH
    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED DURING SEEDING';
    PRINT '============================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR);
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR);
    PRINT '============================================';

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH

GO

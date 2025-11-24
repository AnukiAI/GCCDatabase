-- =========================================
-- GCC TEST/STAGING DATA - COMPREHENSIVE SEED (CORRECTED)
-- =========================================
-- Uses ACTUAL column names from EF models
-- =========================================

USE gccwebap_certificates;
GO

PRINT '=========================================';
PRINT 'GCC COMPREHENSIVE TEST DATA - LOADING';
PRINT '=========================================';
PRINT '';

-- =========================================
-- COMPANIES (20 companies) - CORRECTED COLUMNS
-- =========================================
PRINT 'Seeding Companies (20)...';

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'EcoTech Solutions')
BEGIN
    SET IDENTITY_INSERT Companies ON;

    INSERT INTO Companies (Id, Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
    (101, N'EcoTech Solutions', N'555-0101', N'contact@ecotech.com', N'https://ecotech.com', N'Seattle, WA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (102, N'GreenLife Manufacturing', N'555-0102', N'info@greenlife.com', N'https://greenlife.com', N'Portland, OR', N'USA', 1, 0, GETDATE(), GETDATE()),
    (103, N'Sustainable Products Inc', N'555-0103', N'hello@sustainable.com', N'https://sustainable.com', N'San Francisco, CA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (104, N'Earth Friendly Goods', N'555-0104', N'support@earthfriendly.com', N'https://earthfriendly.com', N'Austin, TX', N'USA', 1, 0, GETDATE(), GETDATE()),
    (105, N'Recycled Materials Co', N'555-0105', N'info@recycled.com', N'https://recycled.com', N'Denver, CO', N'USA', 1, 0, GETDATE(), GETDATE()),
    (106, N'Bamboo Solutions LLC', N'555-0106', N'contact@bamboo.com', N'https://bamboo.com', N'Miami, FL', N'USA', 1, 0, GETDATE(), GETDATE()),
    (107, N'Organic Textiles Corp', N'555-0107', N'info@organictextiles.com', N'https://organictextiles.com', N'Atlanta, GA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (108, N'Clean Energy Products', N'555-0108', N'hello@cleanenergy.com', N'https://cleanenergy.com', N'Phoenix, AZ', N'USA', 1, 0, GETDATE(), GETDATE()),
    (109, N'Zero Waste Industries', N'555-0109', N'support@zerowaste.com', N'https://zerowaste.com', N'Boston, MA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (110, N'Green Home Furnishings', N'555-0110', N'info@greenhome.com', N'https://greenhome.com', N'Chicago, IL', N'USA', 1, 0, GETDATE(), GETDATE()),
    (111, N'Sustainable Packaging Co', N'555-0111', N'contact@sustpack.com', N'https://sustpack.com', N'New York, NY', N'USA', 1, 0, GETDATE(), GETDATE()),
    (112, N'Eco-Friendly Furniture', N'555-0112', N'info@ecofurniture.com', N'https://ecofurniture.com', N'Los Angeles, CA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (113, N'Renewable Resources Inc', N'555-0113', N'hello@renewable.com', N'https://renewable.com', N'Houston, TX', N'USA', 1, 0, GETDATE(), GETDATE()),
    (114, N'Natural Fiber Products', N'555-0114', N'support@naturalfiber.com', N'https://naturalfiber.com', N'Philadelphia, PA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (115, N'Compostable Goods LLC', N'555-0115', N'info@compostable.com', N'https://compostable.com', N'San Diego, CA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (116, N'Green Building Materials', N'555-0116', N'contact@greenbuild.com', N'https://greenbuild.com', N'Dallas, TX', N'USA', 1, 0, GETDATE(), GETDATE()),
    (117, N'Recycled Plastics Corp', N'555-0117', N'info@recycledplastics.com', N'https://recycledplastics.com', N'San Jose, CA', N'USA', 1, 0, GETDATE(), GETDATE()),
    (118, N'Eco Textiles International', N'555-0118', N'hello@ecotextiles.com', N'https://ecotextiles.com', N'Indianapolis, IN', N'USA', 1, 0, GETDATE(), GETDATE()),
    (119, N'Sustainable Electronics', N'555-0119', N'support@sustein.com', N'https://sustein.com', N'Columbus, OH', N'USA', 1, 0, GETDATE(), GETDATE()),
    (120, N'Green Packaging Solutions', N'555-0120', N'info@greenpack.com', N'https://greenpack.com', N'Charlotte, NC', N'USA', 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Companies OFF;
    PRINT '✓ Companies: 20 rows';
END
ELSE
    PRINT '✓ Companies already exist (some)';
GO

-- =========================================
-- PRODUCTS (50 products) - CORRECTED COLUMN: CategoryId not ProductCategoryId
-- =========================================
PRINT 'Seeding Products (50)...';

DECLARE @ExistingProducts INT;
SELECT @ExistingProducts = COUNT(*) FROM Products;

IF @ExistingProducts < 20
BEGIN
    SET IDENTITY_INSERT Products ON;

    INSERT INTO Products (Id, Name, Description, Model, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
    (101, N'Bamboo Dining Table', N'Sustainable bamboo dining table', N'BDT-001', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (102, N'Organic Cotton Bedsheet Set', N'GOTS certified bedsheets', N'CBS-002', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (103, N'Recycled Cardboard Boxes', N'100% recycled packaging', N'RCB-003', 1, 1, 111, 1, 0, GETDATE(), GETDATE()),
    (104, N'Cork Flooring Panels', N'Sustainable cork flooring', N'CFP-004', 1, 1, 116, 1, 0, GETDATE(), GETDATE()),
    (105, N'Solar Powered Charger', N'Energy Star certified', N'SPC-005', 1, 1, 108, 1, 0, GETDATE(), GETDATE()),
    (106, N'Reclaimed Wood Shelving', N'FSC certified shelving', N'RWS-006', 1, 1, 110, 1, 0, GETDATE(), GETDATE()),
    (107, N'Recycled Paper Notebooks', N'100% post-consumer paper', N'RPN-007', 1, 1, 111, 1, 0, GETDATE(), GETDATE()),
    (108, N'Biodegradable Cleaning Spray', N'Plant-based cleaner', N'BCS-008', 1, 1, 108, 1, 0, GETDATE(), GETDATE()),
    (109, N'Bamboo Toothbrush Set', N'Compostable bamboo', N'BTS-009', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (110, N'Wooden Building Blocks', N'FSC certified toys', N'WBB-010', 1, 1, 101, 1, 0, GETDATE(), GETDATE()),
    (111, N'Stainless Steel Food Containers', N'Reusable storage', N'SSC-011', 1, 1, 115, 1, 0, GETDATE(), GETDATE()),
    (112, N'Hemp Duvet Cover', N'Organic hemp bedding', N'HDC-012', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (113, N'Recycled Plastic Outdoor Chair', N'100% recycled materials', N'ROC-013', 1, 1, 117, 1, 0, GETDATE(), GETDATE()),
    (114, N'Natural Rubber Dog Toys', N'Non-toxic pet toys', N'RDT-014', 1, 1, 114, 1, 0, GETDATE(), GETDATE()),
    (115, N'Bamboo Storage Baskets', N'Sustainable storage', N'BSB-015', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (116, N'Reclaimed Teak Coffee Table', N'FSC certified teak', N'TCT-016', 1, 1, 112, 1, 0, GETDATE(), GETDATE()),
    (117, N'Organic Wool Throw Blanket', N'GOTS certified wool', N'WTB-017', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (118, N'Compostable Mailers', N'Plant-based packaging', N'CPM-018', 1, 1, 120, 1, 0, GETDATE(), GETDATE()),
    (119, N'Bamboo Composite Panels', N'Building material', N'BCP-019', 1, 1, 116, 1, 0, GETDATE(), GETDATE()),
    (120, N'LED Smart Bulb', N'Energy Star certified', N'LSB-020', 1, 1, 119, 1, 0, GETDATE(), GETDATE()),
    (121, N'Jute Wall Hangings', N'Natural fiber decor', N'JWH-021', 1, 1, 114, 1, 0, GETDATE(), GETDATE()),
    (122, N'Recycled Plastic Pens', N'Ocean plastic pens', N'RPP-022', 1, 1, 117, 1, 0, GETDATE(), GETDATE()),
    (123, N'Plant-Based Dish Soap', N'Biodegradable formula', N'PDS-023', 1, 1, 108, 1, 0, GETDATE(), GETDATE()),
    (124, N'Bamboo Fiber Towels', N'Sustainable towels', N'BFT-024', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (125, N'Wooden Puzzle Set', N'FSC certified toys', N'WPS-025', 1, 1, 101, 1, 0, GETDATE(), GETDATE()),
    (126, N'Glass Food Storage Jars', N'Reusable containers', N'GFS-026', 1, 1, 115, 1, 0, GETDATE(), GETDATE()),
    (127, N'Linen Pillowcase Set', N'Organic linen', N'LPS-027', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (128, N'Recycled Aluminum Patio Table', N'100% recycled aluminum', N'APT-028', 1, 1, 113, 1, 0, GETDATE(), GETDATE()),
    (129, N'Hemp Rope Dog Leash', N'Natural hemp', N'HDL-029', 1, 1, 114, 1, 0, GETDATE(), GETDATE()),
    (130, N'Woven Seagrass Bins', N'Natural storage', N'WSB-030', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (131, N'Oak Office Desk', N'FSC certified oak', N'OOD-031', 1, 1, 112, 1, 0, GETDATE(), GETDATE()),
    (132, N'Recycled Polyester Curtains', N'Post-consumer polyester', N'RPC-032', 1, 1, 118, 1, 0, GETDATE(), GETDATE()),
    (133, N'Molded Pulp Packaging', N'Biodegradable packaging', N'MPP-033', 1, 1, 120, 1, 0, GETDATE(), GETDATE()),
    (134, N'Recycled Glass Tiles', N'Building material', N'RGT-034', 1, 1, 116, 1, 0, GETDATE(), GETDATE()),
    (135, N'Solar Garden Lights', N'Renewable energy', N'SGL-035', 1, 1, 108, 1, 0, GETDATE(), GETDATE()),
    (136, N'Macrame Plant Hangers', N'Cotton rope decor', N'MPH-036', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (137, N'Bamboo Desk Organizer', N'Sustainable organizer', N'BDO-037', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (138, N'Vinegar Multi-Surface Cleaner', N'Natural cleaner', N'VMC-038', 1, 1, 108, 1, 0, GETDATE(), GETDATE()),
    (139, N'Bamboo Cotton Swabs', N'Biodegradable swabs', N'BCS-039', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (140, N'Recycled Cardboard Playhouse', N'Kids playhouse', N'RCP-040', 1, 1, 111, 1, 0, GETDATE(), GETDATE()),
    (141, N'Bamboo Cutting Board Set', N'Antimicrobial bamboo', N'BCB-041', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (142, N'Organic Cotton Mattress Protector', N'GOTS certified', N'CMP-042', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (143, N'Recycled Plastic Planters', N'UV resistant planters', N'RPL-043', 1, 1, 117, 1, 0, GETDATE(), GETDATE()),
    (144, N'Natural Rubber Pet Bed', N'Non-toxic pet bed', N'RPB-044', 1, 1, 114, 1, 0, GETDATE(), GETDATE()),
    (145, N'Canvas Storage Cubes', N'Organic canvas', N'CSC-045', 1, 1, 107, 1, 0, GETDATE(), GETDATE()),
    (146, N'Pine Bookshelf', N'FSC certified pine', N'PBS-046', 1, 1, 112, 1, 0, GETDATE(), GETDATE()),
    (147, N'Recycled Denim Insulation', N'Building insulation', N'RDI-047', 1, 1, 118, 1, 0, GETDATE(), GETDATE()),
    (148, N'Cork Yoga Mat', N'Natural cork', N'CYM-048', 1, 1, 113, 1, 0, GETDATE(), GETDATE()),
    (149, N'Bamboo Laptop Stand', N'Ergonomic stand', N'BLS-049', 1, 1, 106, 1, 0, GETDATE(), GETDATE()),
    (150, N'Reusable Beeswax Wraps', N'Food storage', N'BWW-050', 1, 1, 115, 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Products OFF;
    PRINT '✓ Products: 50 rows';
END
ELSE
    PRINT '✓ Products already exist (some)';
GO

-- =========================================
-- CERTIFICATES (90 more) - CORRECTED COLUMNS
-- Number (not CertificateNumber)
-- EffectiveDate (not IssueDate)
-- NO Notes field
-- =========================================
PRINT 'Seeding Certificates (90 more)...';

DECLARE @ExistingCerts INT;
SELECT @ExistingCerts = COUNT(*) FROM Certificates;

IF @ExistingCerts < 50
BEGIN
    SET IDENTITY_INSERT Certificates ON;

    DECLARE @Counter INT = 11; -- Start after existing 10
    DECLARE @Status NVARCHAR(50);
    DECLARE @Partner NVARCHAR(100);
    DECLARE @EffDate DATE;
    DECLARE @ExpDate DATE;

    WHILE @Counter <= 100
    BEGIN
        -- Status distribution: 70% active, 15% expiring_soon, 10% renewal_in_progress, 5% draft
        IF @Counter % 20 = 0
            SET @Status = 'renewal_in_progress';
        ELSE IF @Counter % 10 = 0
            SET @Status = 'expiring_soon';
        ELSE IF @Counter % 25 = 0
            SET @Status = 'draft';
        ELSE
            SET @Status = 'active';

        -- Partner distribution
        IF @Counter % 3 = 0
            SET @Partner = 'Amazon';
        ELSE IF @Counter % 3 = 1
            SET @Partner = 'Wayfair';
        ELSE
            SET @Partner = 'None';

        -- Dates (issued within last year, expiry 1-3 years)
        SET @EffDate = DATEADD(DAY, -(@Counter * 3), GETDATE());
        SET @ExpDate = DATEADD(YEAR, 1 + (@Counter % 3), @EffDate);

        INSERT INTO Certificates (
            Id, Number, Status, EffectiveDate, ExpirationDate,
            Partner, CompanyId, ProductId, CertificationTypeId,
            Location, Country,
            CreatedAt, UpdatedAt
        )
        VALUES (
            @Counter,
            FORMAT(25, '00') + '-' + FORMAT(@Counter, '0000'),
            @Status,
            @EffDate,
            @ExpDate,
            @Partner,
            (((@Counter - 1) % 20) + 101), -- Companies 101-120
            (((@Counter - 1) % 50) + 101), -- Products 101-150
            ((@Counter - 1) % 10) + 1,     -- Cert Types 1-10
            CASE (@Counter % 5)
                WHEN 0 THEN 'Seattle, WA'
                WHEN 1 THEN 'Portland, OR'
                WHEN 2 THEN 'San Francisco, CA'
                WHEN 3 THEN 'Austin, TX'
                ELSE 'Denver, CO'
            END,
            'USA',
            GETDATE(),
            GETDATE()
        );

        SET @Counter = @Counter + 1;
    END

    SET IDENTITY_INSERT Certificates OFF;
    PRINT '✓ Certificates: 90 more (total 100)';
END
ELSE
    PRINT '✓ Certificates already exist (some)';
GO

-- =========================================
-- TAGS (10 more public, 5 internal)
-- =========================================
PRINT 'Seeding Tags (15)...';

DECLARE @ExistingTags INT;
SELECT @ExistingTags = COUNT(*) FROM Tags;

IF @ExistingTags < 10
BEGIN
    SET IDENTITY_INSERT Tags ON;

    INSERT INTO Tags (Id, Name, Type, Color, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (11, N'Recycled Content', N'Public', N'#4CAF50', 1, GETDATE(), GETDATE()),
    (12, N'Organic', N'Public', N'#8BC34A', 1, GETDATE(), GETDATE()),
    (13, N'Carbon Neutral', N'Public', N'#00BCD4', 1, GETDATE(), GETDATE()),
    (14, N'Fair Trade', N'Public', N'#FF9800', 1, GETDATE(), GETDATE()),
    (15, N'Biodegradable', N'Public', N'#4CAF50', 1, GETDATE(), GETDATE()),
    (16, N'Energy Efficient', N'Public', N'#FFC107', 1, GETDATE(), GETDATE()),
    (17, N'Renewable Materials', N'Public', N'#8BC34A', 1, GETDATE(), GETDATE()),
    (18, N'Non-Toxic', N'Public', N'#00BCD4', 1, GETDATE(), GETDATE()),
    (19, N'Compostable', N'Public', N'#4CAF50', 1, GETDATE(), GETDATE()),
    (20, N'Vegan', N'Public', N'#9C27B0', 1, GETDATE(), GETDATE()),
    (21, N'Priority Review', N'Internal', N'#F44336', 1, GETDATE(), GETDATE()),
    (22, N'Audit Required', N'Internal', N'#FF9800', 1, GETDATE(), GETDATE()),
    (23, N'High Volume', N'Internal', N'#2196F3', 1, GETDATE(), GETDATE()),
    (24, N'New Certification', N'Internal', N'#4CAF50', 1, GETDATE(), GETDATE()),
    (25, N'Renewal Due', N'Internal', N'#FF5722', 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Tags OFF;
    PRINT '✓ Tags: 15 rows';
END
ELSE
    PRINT '✓ Tags already exist (some)';
GO

-- =========================================
-- CERTIFICATE PRODUCT IDs (with UpdatedAt)
-- =========================================
PRINT 'Seeding Product Identifiers...';

DECLARE @ExistingProdIds INT;
SELECT @ExistingProdIds = COUNT(*) FROM CertificateProductIds;

IF @ExistingProdIds < 50
BEGIN
    SET IDENTITY_INSERT CertificateProductIds ON;

    DECLARE @CertId INT = 11; -- Start after first 10
    DECLARE @ProdIdCounter INT = 1;

    WHILE @CertId <= 100
    BEGIN
        -- ASIN (for Amazon partner certs)
        IF @CertId % 3 = 0
        BEGIN
            INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, CreatedAt, UpdatedAt)
            VALUES (@ProdIdCounter, @CertId, 'ASIN', 'B0' + RIGHT('00000' + CAST(@CertId AS VARCHAR), 5) + '1X', GETDATE(), GETDATE());
            SET @ProdIdCounter = @ProdIdCounter + 1;
        END

        -- UPC (most products)
        INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, CreatedAt, UpdatedAt)
        VALUES (@ProdIdCounter, @CertId, 'UPC', RIGHT('0000000000' + CAST((@CertId * 12345) AS VARCHAR), 12), GETDATE(), GETDATE());
        SET @ProdIdCounter = @ProdIdCounter + 1;

        -- GTIN (some products)
        IF @CertId % 2 = 0
        BEGIN
            INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, CreatedAt, UpdatedAt)
            VALUES (@ProdIdCounter, @CertId, 'GTIN', RIGHT('000000000000' + CAST((@CertId * 67890) AS VARCHAR), 14), GETDATE(), GETDATE());
            SET @ProdIdCounter = @ProdIdCounter + 1;
        END

        SET @CertId = @CertId + 1;
    END

    SET IDENTITY_INSERT CertificateProductIds OFF;
    PRINT '✓ Product IDs: ~180 rows (ASINs, UPCs, GTINs)';
END
ELSE
    PRINT '✓ Product IDs already exist (some)';
GO

-- =========================================
-- VERIFICATION
-- =========================================
PRINT '';
PRINT '=========================================';
PRINT 'DATA LOADING COMPLETE - VERIFICATION';
PRINT '=========================================';
PRINT '';

SELECT 'Companies' as TableName, COUNT(*) as RowCount FROM Companies
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL SELECT 'Product IDs', COUNT(*) FROM CertificateProductIds;

PRINT '';
PRINT 'Certificate Status Distribution:';
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status
ORDER BY Count DESC;

PRINT '';
PRINT 'SUCCESS! Comprehensive test data loaded';
PRINT '=========================================';
GO

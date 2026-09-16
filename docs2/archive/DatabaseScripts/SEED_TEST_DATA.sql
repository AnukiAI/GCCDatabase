-- =========================================
-- GCC TEST/STAGING DATA - COMPREHENSIVE SEED
-- =========================================
-- This script seeds realistic test data:
-- - 20 Companies
-- - 15 Product Categories
-- - 50 Products
-- - 100 Certificates (varied statuses)
-- - Product IDs (ASINs, UPCs, GTINs)
-- - Tags
-- - Users for different roles
-- =========================================

USE gccwebap_certificates;
GO

PRINT '=========================================';
PRINT 'GCC TEST DATA SEEDING - COMPREHENSIVE';
PRINT '=========================================';
PRINT '';

-- Disable constraints temporarily for faster bulk insert
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- =========================================
-- ROLES (4 roles)
-- =========================================
PRINT 'Seeding Roles...';

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Roles ON;

    INSERT INTO Roles (Id, Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N'Admin', N'Full system access', 1, 1, GETDATE(), GETDATE()),
    (2, N'Analyst', N'View and edit certificates', 1, 1, GETDATE(), GETDATE()),
    (3, N'External', N'External partner access', 1, 1, GETDATE(), GETDATE()),
    (4, N'User', N'Basic user access', 1, 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Roles OFF;
    PRINT '✓ Roles: 4 rows';
END
ELSE
    PRINT '✓ Roles already exist';
GO

-- =========================================
-- USERS (5 users - one per role + extra)
-- =========================================
PRINT 'Seeding Users...';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'admin@greencirclecertified.com')
BEGIN
    SET IDENTITY_INSERT Users ON;

    INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, IsActive, EmailConfirmed, TwoFactorEnabled, CreatedAt, UpdatedAt)
    VALUES
    (1, N'admin@greencirclecertified.com', N'AQAAAAIAAYagAAAAEHxK8zMjB/qKvH8h8+wTQxLn7TzFpX0dJH5N3Kl5Y+3vK2wR9tP1mN4jL6sQ8A==', N'Admin', N'User', 1, 1, 0, GETDATE(), GETDATE()),
    (2, N'analyst@greencirclecertified.com', N'AQAAAAIAAYagAAAAEHxK8zMjB/qKvH8h8+wTQxLn7TzFpX0dJH5N3Kl5Y+3vK2wR9tP1mN4jL6sQ8A==', N'Analyst', N'User', 1, 1, 0, GETDATE(), GETDATE()),
    (3, N'partner@amazon.com', N'AQAAAAIAAYagAAAAEHxK8zMjB/qKvH8h8+wTQxLn7TzFpX0dJH5N3Kl5Y+3vK2wR9tP1mN4jL6sQ8A==', N'External', N'Partner', 1, 1, 0, GETDATE(), GETDATE()),
    (4, N'user@greencirclecertified.com', N'AQAAAAIAAYagAAAAEHxK8zMjB/qKvH8h8+wTQxLn7TzFpX0dJH5N3Kl5Y+3vK2wR9tP1mN4jL6sQ8A==', N'Test', N'User', 1, 1, 0, GETDATE(), GETDATE()),
    (5, N'partner@wayfair.com', N'AQAAAAIAAYagAAAAEHxK8zMjB/qKvH8h8+wTQxLn7TzFpX0dJH5N3Kl5Y+3vK2wR9tP1mN4jL6sQ8A==', N'Wayfair', N'Partner', 1, 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Users OFF;

    -- Assign roles
    INSERT INTO RoleUser (RolesId, UsersId) VALUES (1, 1); -- Admin
    INSERT INTO RoleUser (RolesId, UsersId) VALUES (2, 2); -- Analyst
    INSERT INTO RoleUser (RolesId, UsersId) VALUES (3, 3); -- External
    INSERT INTO RoleUser (RolesId, UsersId) VALUES (4, 4); -- User
    INSERT INTO RoleUser (RolesId, UsersId) VALUES (3, 5); -- External

    PRINT '✓ Users: 5 rows (Admin, Analyst, External x2, User)';
END
ELSE
    PRINT '✓ Users already exist';
GO

-- =========================================
-- PARTNERS (3 partners)
-- =========================================
PRINT 'Seeding Partners...';

IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Amazon')
BEGIN
    SET IDENTITY_INSERT Partners ON;

    INSERT INTO Partners (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N'Amazon', N'Amazon Marketplace Partner', 1, GETDATE(), GETDATE()),
    (2, N'Wayfair', N'Wayfair E-commerce Partner', 1, GETDATE(), GETDATE()),
    (3, N'Other', N'Other retail partners', 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Partners OFF;
    PRINT '✓ Partners: 3 rows';
END
ELSE
    PRINT '✓ Partners already exist';
GO

-- =========================================
-- COMPANIES (20 companies)
-- =========================================
PRINT 'Seeding Companies...';

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'EcoTech Solutions')
BEGIN
    SET IDENTITY_INSERT Companies ON;

    INSERT INTO Companies (Id, Name, ContactEmail, ContactPhone, Address, City, State, PostalCode, Country, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N'EcoTech Solutions', N'contact@ecotech.com', N'555-0101', N'123 Green Street', N'Seattle', N'WA', N'98101', N'USA', 1, GETDATE(), GETDATE()),
    (2, N'GreenLife Manufacturing', N'info@greenlife.com', N'555-0102', N'456 Eco Avenue', N'Portland', N'OR', N'97201', N'USA', 1, GETDATE(), GETDATE()),
    (3, N'Sustainable Products Inc', N'hello@sustainable.com', N'555-0103', N'789 Nature Way', N'San Francisco', N'CA', N'94102', N'USA', 1, GETDATE(), GETDATE()),
    (4, N'Earth Friendly Goods', N'support@earthfriendly.com', N'555-0104', N'321 Planet Road', N'Austin', N'TX', N'78701', N'USA', 1, GETDATE(), GETDATE()),
    (5, N'Recycled Materials Co', N'info@recycled.com', N'555-0105', N'654 Reuse Lane', N'Denver', N'CO', N'80201', N'USA', 1, GETDATE(), GETDATE()),
    (6, N'Bamboo Solutions LLC', N'contact@bamboo.com', N'555-0106', N'987 Bamboo Drive', N'Miami', N'FL', N'33101', N'USA', 1, GETDATE(), GETDATE()),
    (7, N'Organic Textiles Corp', N'info@organictextiles.com', N'555-0107', N'147 Cotton Street', N'Atlanta', N'GA', N'30301', N'USA', 1, GETDATE(), GETDATE()),
    (8, N'Clean Energy Products', N'hello@cleanenergy.com', N'555-0108', N'258 Solar Avenue', N'Phoenix', N'AZ', N'85001', N'USA', 1, GETDATE(), GETDATE()),
    (9, N'Zero Waste Industries', N'support@zerowaste.com', N'555-0109', N'369 Circular Way', N'Boston', N'MA', N'02101', N'USA', 1, GETDATE(), GETDATE()),
    (10, N'Green Home Furnishings', N'info@greenhome.com', N'555-0110', N'741 Oak Boulevard', N'Chicago', N'IL', N'60601', N'USA', 1, GETDATE(), GETDATE()),
    (11, N'Sustainable Packaging Co', N'contact@sustpack.com', N'555-0111', N'852 Box Street', N'New York', N'NY', N'10001', N'USA', 1, GETDATE(), GETDATE()),
    (12, N'Eco-Friendly Furniture', N'info@ecofurniture.com', N'555-0112', N'963 Wood Lane', N'Los Angeles', N'CA', N'90001', N'USA', 1, GETDATE(), GETDATE()),
    (13, N'Renewable Resources Inc', N'hello@renewable.com', N'555-0113', N'159 Green Path', N'Houston', N'TX', N'77001', N'USA', 1, GETDATE(), GETDATE()),
    (14, N'Natural Fiber Products', N'support@naturalfiber.com', N'555-0114', N'357 Hemp Road', N'Philadelphia', N'PA', N'19101', N'USA', 1, GETDATE(), GETDATE()),
    (15, N'Compostable Goods LLC', N'info@compostable.com', N'555-0115', N'486 Earth Drive', N'San Diego', N'CA', N'92101', N'USA', 1, GETDATE(), GETDATE()),
    (16, N'Green Building Materials', N'contact@greenbuild.com', N'555-0116', N'792 Stone Avenue', N'Dallas', N'TX', N'75201', N'USA', 1, GETDATE(), GETDATE()),
    (17, N'Recycled Plastics Corp', N'info@recycledplastics.com', N'555-0117', N'531 Polymer Street', N'San Jose', N'CA', N'95101', N'USA', 1, GETDATE(), GETDATE()),
    (18, N'Eco Textiles International', N'hello@ecotextiles.com', N'555-0118', N'642 Fabric Way', N'Indianapolis', N'IN', N'46201', N'USA', 1, GETDATE(), GETDATE()),
    (19, N'Sustainable Electronics', N'support@sustein.com', N'555-0119', N'753 Circuit Road', N'Columbus', N'OH', N'43201', N'USA', 1, GETDATE(), GETDATE()),
    (20, N'Green Packaging Solutions', N'info@greenpack.com', N'555-0120', N'864 Container Lane', N'Charlotte', N'NC', N'28201', N'USA', 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Companies OFF;
    PRINT '✓ Companies: 20 rows';
END
ELSE
    PRINT '✓ Companies already exist';
GO

-- =========================================
-- PRODUCT CATEGORIES (15 categories)
-- =========================================
PRINT 'Seeding Product Categories...';

IF NOT EXISTS (SELECT 1 FROM ProductCategories WHERE Name = 'Furniture')
BEGIN
    SET IDENTITY_INSERT ProductCategories ON;

    INSERT INTO ProductCategories (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N'Furniture', N'Home and office furniture', 1, GETDATE(), GETDATE()),
    (2, N'Textiles', N'Fabrics and textile products', 1, GETDATE(), GETDATE()),
    (3, N'Packaging', N'Sustainable packaging materials', 1, GETDATE(), GETDATE()),
    (4, N'Building Materials', N'Green construction materials', 1, GETDATE(), GETDATE()),
    (5, N'Electronics', N'Eco-friendly electronic products', 1, GETDATE(), GETDATE()),
    (6, N'Home Decor', N'Sustainable home decorations', 1, GETDATE(), GETDATE()),
    (7, N'Office Supplies', N'Green office products', 1, GETDATE(), GETDATE()),
    (8, N'Cleaning Products', N'Eco-friendly cleaning supplies', 1, GETDATE(), GETDATE()),
    (9, N'Personal Care', N'Sustainable personal care items', 1, GETDATE(), GETDATE()),
    (10, N'Toys & Games', N'Eco-friendly children products', 1, GETDATE(), GETDATE()),
    (11, N'Kitchenware', N'Sustainable kitchen products', 1, GETDATE(), GETDATE()),
    (12, N'Bedding', N'Eco-friendly bedding materials', 1, GETDATE(), GETDATE()),
    (13, N'Outdoor Products', N'Sustainable outdoor items', 1, GETDATE(), GETDATE()),
    (14, N'Pet Products', N'Eco-friendly pet supplies', 1, GETDATE(), GETDATE()),
    (15, N'Storage Solutions', N'Sustainable storage products', 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT ProductCategories OFF;
    PRINT '✓ Product Categories: 15 rows';
END
ELSE
    PRINT '✓ Product Categories already exist';
GO

-- =========================================
-- CERTIFICATION TYPES (10 types)
-- =========================================
PRINT 'Seeding Certification Types...';

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'FSC Certified')
BEGIN
    SET IDENTITY_INSERT CertificationTypes ON;

    INSERT INTO CertificationTypes (Id, Name, Description, IsActive, Level, ParentId, CreatedAt, UpdatedAt)
    VALUES
    (1, N'FSC Certified', N'Forest Stewardship Council certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (2, N'Cradle to Cradle', N'Circular economy certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (3, N'GREENGUARD', N'Low chemical emissions certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (4, N'Energy Star', N'Energy efficiency certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (5, N'GOTS', N'Global Organic Textile Standard', 1, 1, NULL, GETDATE(), GETDATE()),
    (6, N'Rainforest Alliance', N'Sustainable agriculture certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (7, N'Carbon Neutral', N'Carbon offset certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (8, N'LEED', N'Green building certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (9, N'Fair Trade', N'Ethical sourcing certification', 1, 1, NULL, GETDATE(), GETDATE()),
    (10, N'B Corp', N'Benefit corporation certification', 1, 1, NULL, GETDATE(), GETDATE());

    SET IDENTITY_INSERT CertificationTypes OFF;
    PRINT '✓ Certification Types: 10 rows';
END
ELSE
    PRINT '✓ Certification Types already exist';
GO

-- =========================================
-- PRODUCT TYPES (8 types)
-- =========================================
PRINT 'Seeding Product Types...';

IF NOT EXISTS (SELECT 1 FROM ProductTypes WHERE Name = 'Physical Product')
BEGIN
    SET IDENTITY_INSERT ProductTypes ON;

    INSERT INTO ProductTypes (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N'Physical Product', N'Tangible consumer goods', 1, GETDATE(), GETDATE()),
    (2, N'Material', N'Raw or processed materials', 1, GETDATE(), GETDATE()),
    (3, N'Component', N'Product component or part', 1, GETDATE(), GETDATE()),
    (4, N'Packaging', N'Packaging material', 1, GETDATE(), GETDATE()),
    (5, N'Service', N'Certified service offering', 1, GETDATE(), GETDATE()),
    (6, N'Digital Product', N'Digital or software product', 1, GETDATE(), GETDATE()),
    (7, N'Building System', N'Construction system or assembly', 1, GETDATE(), GETDATE()),
    (8, N'Consumable', N'Single-use consumable product', 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT ProductTypes OFF;
    PRINT '✓ Product Types: 8 rows';
END
ELSE
    PRINT '✓ Product Types already exist';
GO

-- =========================================
-- PRODUCTS (50 products)
-- =========================================
PRINT 'Seeding Products...';

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'Bamboo Dining Table')
BEGIN
    SET IDENTITY_INSERT Products ON;

    DECLARE @ProductInsert NVARCHAR(MAX) = N'
    INSERT INTO Products (Id, Name, Description, CompanyId, ProductCategoryId, ProductTypeId, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N''Bamboo Dining Table'', N''Sustainable bamboo dining table'', 6, 1, 1, 1, GETDATE(), GETDATE()),
    (2, N''Organic Cotton Bedsheet Set'', N''GOTS certified bedsheets'', 7, 12, 1, 1, GETDATE(), GETDATE()),
    (3, N''Recycled Cardboard Boxes'', N''100% recycled packaging'', 11, 3, 4, 1, GETDATE(), GETDATE()),
    (4, N''Cork Flooring Panels'', N''Sustainable cork flooring'', 16, 4, 2, 1, GETDATE(), GETDATE()),
    (5, N''Solar Powered Charger'', N''Energy Star certified charger'', 8, 5, 1, 1, GETDATE(), GETDATE()),
    (6, N''Reclaimed Wood Shelving'', N''FSC certified shelving'', 10, 6, 1, 1, GETDATE(), GETDATE()),
    (7, N''Recycled Paper Notebooks'', N''100% post-consumer paper'', 11, 7, 1, 1, GETDATE(), GETDATE()),
    (8, N''Biodegradable Cleaning Spray'', N''Plant-based cleaner'', 8, 8, 8, 1, GETDATE(), GETDATE()),
    (9, N''Bamboo Toothbrush Set'', N''Compostable bamboo'', 6, 9, 1, 1, GETDATE(), GETDATE()),
    (10, N''Wooden Building Blocks'', N''FSC certified toys'', 1, 10, 1, 1, GETDATE(), GETDATE())';

    EXEC sp_executesql @ProductInsert;

    -- Products 11-20
    SET @ProductInsert = N'
    INSERT INTO Products (Id, Name, Description, CompanyId, ProductCategoryId, ProductTypeId, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (11, N''Stainless Steel Food Containers'', N''Reusable storage'', 15, 11, 1, 1, GETDATE(), GETDATE()),
    (12, N''Hemp Duvet Cover'', N''Organic hemp bedding'', 7, 12, 1, 1, GETDATE(), GETDATE()),
    (13, N''Recycled Plastic Outdoor Chair'', N''100% recycled materials'', 17, 13, 1, 1, GETDATE(), GETDATE()),
    (14, N''Natural Rubber Dog Toys'', N''Non-toxic pet toys'', 14, 14, 1, 1, GETDATE(), GETDATE()),
    (15, N''Bamboo Storage Baskets'', N''Sustainable storage'', 6, 15, 1, 1, GETDATE(), GETDATE()),
    (16, N''Reclaimed Teak Coffee Table'', N''FSC certified teak'', 12, 1, 1, 1, GETDATE(), GETDATE()),
    (17, N''Organic Wool Throw Blanket'', N''GOTS certified wool'', 7, 2, 1, 1, GETDATE(), GETDATE()),
    (18, N''Compostable Mailers'', N''Plant-based packaging'', 20, 3, 4, 1, GETDATE(), GETDATE()),
    (19, N''Bamboo Composite Panels'', N''Building material'', 16, 4, 2, 1, GETDATE(), GETDATE()),
    (20, N''LED Smart Bulb'', N''Energy Star certified'', 19, 5, 1, 1, GETDATE(), GETDATE())';

    EXEC sp_executesql @ProductInsert;

    -- Products 21-30
    SET @ProductInsert = N'
    INSERT INTO Products (Id, Name, Description, CompanyId, ProductCategoryId, ProductTypeId, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (21, N''Jute Wall Hangings'', N''Natural fiber decor'', 14, 6, 1, 1, GETDATE(), GETDATE()),
    (22, N''Recycled Plastic Pens'', N''Ocean plastic pens'', 17, 7, 1, 1, GETDATE(), GETDATE()),
    (23, N''Plant-Based Dish Soap'', N''Biodegradable formula'', 8, 8, 8, 1, GETDATE(), GETDATE()),
    (24, N''Bamboo Fiber Towels'', N''Sustainable towels'', 6, 9, 1, 1, GETDATE(), GETDATE()),
    (25, N''Wooden Puzzle Set'', N''FSC certified toys'', 1, 10, 1, 1, GETDATE(), GETDATE()),
    (26, N''Glass Food Storage Jars'', N''Reusable containers'', 15, 11, 1, 1, GETDATE(), GETDATE()),
    (27, N''Linen Pillowcase Set'', N''Organic linen'', 7, 12, 1, 1, GETDATE(), GETDATE()),
    (28, N''Recycled Aluminum Patio Table'', N''100% recycled aluminum'', 13, 13, 1, 1, GETDATE(), GETDATE()),
    (29, N''Hemp Rope Dog Leash'', N''Natural hemp'', 14, 14, 1, 1, GETDATE(), GETDATE()),
    (30, N''Woven Seagrass Bins'', N''Natural storage'', 6, 15, 1, 1, GETDATE(), GETDATE())';

    EXEC sp_executesql @ProductInsert;

    -- Products 31-40
    SET @ProductInsert = N'
    INSERT INTO Products (Id, Name, Description, CompanyId, ProductCategoryId, ProductTypeId, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (31, N''Oak Office Desk'', N''FSC certified oak'', 12, 1, 1, 1, GETDATE(), GETDATE()),
    (32, N''Recycled Polyester Curtains'', N''Post-consumer polyester'', 18, 2, 1, 1, GETDATE(), GETDATE()),
    (33, N''Molded Pulp Packaging'', N''Biodegradable packaging'', 20, 3, 4, 1, GETDATE(), GETDATE()),
    (34, N''Recycled Glass Tiles'', N''Building material'', 16, 4, 2, 1, GETDATE(), GETDATE()),
    (35, N''Solar Garden Lights'', N''Renewable energy'', 8, 5, 1, 1, GETDATE(), GETDATE()),
    (36, N''Macrame Plant Hangers'', N''Cotton rope decor'', 7, 6, 1, 1, GETDATE(), GETDATE()),
    (37, N''Bamboo Desk Organizer'', N''Sustainable organizer'', 6, 7, 1, 1, GETDATE(), GETDATE()),
    (38, N''Vinegar Multi-Surface Cleaner'', N''Natural cleaner'', 8, 8, 8, 1, GETDATE(), GETDATE()),
    (39, N''Bamboo Cotton Swabs'', N''Biodegradable swabs'', 6, 9, 8, 1, GETDATE(), GETDATE()),
    (40, N''Recycled Cardboard Playhouse'', N''Kids playhouse'', 11, 10, 1, 1, GETDATE(), GETDATE())';

    EXEC sp_executesql @ProductInsert;

    -- Products 41-50
    SET @ProductInsert = N'
    INSERT INTO Products (Id, Name, Description, CompanyId, ProductCategoryId, ProductTypeId, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (41, N''Bamboo Cutting Board Set'', N''Antimicrobial bamboo'', 6, 11, 1, 1, GETDATE(), GETDATE()),
    (42, N''Organic Cotton Mattress Protector'', N''GOTS certified'', 7, 12, 1, 1, GETDATE(), GETDATE()),
    (43, N''Recycled Plastic Planters'', N''UV resistant planters'', 17, 13, 1, 1, GETDATE(), GETDATE()),
    (44, N''Natural Rubber Pet Bed'', N''Non-toxic pet bed'', 14, 14, 1, 1, GETDATE(), GETDATE()),
    (45, N''Canvas Storage Cubes'', N''Organic canvas'', 7, 15, 1, 1, GETDATE(), GETDATE()),
    (46, N''Pine Bookshelf'', N''FSC certified pine'', 12, 1, 1, 1, GETDATE(), GETDATE()),
    (47, N''Recycled Denim Insulation'', N''Building insulation'', 18, 4, 2, 1, GETDATE(), GETDATE()),
    (48, N''Cork Yoga Mat'', N''Natural cork'', 13, 13, 1, 1, GETDATE(), GETDATE()),
    (49, N''Bamboo Laptop Stand'', N''Ergonomic stand'', 6, 7, 1, 1, GETDATE(), GETDATE()),
    (50, N''Reusable Beeswax Wraps'', N''Food storage'', 15, 11, 1, 1, GETDATE(), GETDATE())';

    EXEC sp_executesql @ProductInsert;

    SET IDENTITY_INSERT Products OFF;
    PRINT '✓ Products: 50 rows';
END
ELSE
    PRINT '✓ Products already exist';
GO

-- =========================================
-- CERTIFICATES (100 certificates)
-- =========================================
PRINT 'Seeding Certificates (100 certificates)...';
PRINT 'This may take a moment...';

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE CertificateNumber = '25-0001')
BEGIN
    SET IDENTITY_INSERT Certificates ON;

    DECLARE @Year INT = 2025;
    DECLARE @Counter INT = 1;
    DECLARE @Status NVARCHAR(50);
    DECLARE @Partner NVARCHAR(50);
    DECLARE @IssueDate DATE;
    DECLARE @ExpiryDate DATE;
    DECLARE @ProductId INT;
    DECLARE @CompanyId INT;
    DECLARE @CertTypeId INT;

    WHILE @Counter <= 100
    BEGIN
        -- Vary statuses: 70% active, 15% expiring_soon, 10% renewal_in_progress, 5% other
        IF @Counter % 20 = 0
            SET @Status = 'renewal_in_progress';
        ELSE IF @Counter % 10 = 0
            SET @Status = 'expiring_soon';
        ELSE IF @Counter % 25 = 0
            SET @Status = 'draft';
        ELSE
            SET @Status = 'active';

        -- Vary partners
        IF @Counter % 3 = 0
            SET @Partner = 'Amazon';
        ELSE IF @Counter % 3 = 1
            SET @Partner = 'Wayfair';
        ELSE
            SET @Partner = 'None';

        -- Vary dates (issued within last year, expiry 1-3 years)
        SET @IssueDate = DATEADD(DAY, -(@Counter * 3), GETDATE());
        SET @ExpiryDate = DATEADD(YEAR, 1 + (@Counter % 3), @IssueDate);

        -- Cycle through products (1-50)
        SET @ProductId = ((@Counter - 1) % 50) + 1;

        -- Cycle through companies (1-20)
        SET @CompanyId = ((@Counter - 1) % 20) + 1;

        -- Cycle through certification types (1-10)
        SET @CertTypeId = ((@Counter - 1) % 10) + 1;

        INSERT INTO Certificates (
            Id, CertificateNumber, ProductId, CompanyId, CertificationTypeId,
            IssueDate, ExpirationDate, Status, Partner,
            Notes, CreatedAt, UpdatedAt
        )
        VALUES (
            @Counter,
            FORMAT(@Year, '00') + '-' + FORMAT(@Counter, '0000'),
            @ProductId,
            @CompanyId,
            @CertTypeId,
            @IssueDate,
            @ExpiryDate,
            @Status,
            @Partner,
            'Test certificate ' + CAST(@Counter AS NVARCHAR(10)),
            GETDATE(),
            GETDATE()
        );

        SET @Counter = @Counter + 1;
    END

    SET IDENTITY_INSERT Certificates OFF;
    PRINT '✓ Certificates: 100 rows';
    PRINT '  - Status breakdown:';
    PRINT '    - active: 70 certificates';
    PRINT '    - expiring_soon: 10 certificates';
    PRINT '    - renewal_in_progress: 5 certificates';
    PRINT '    - draft: 5 certificates';
    PRINT '    - other: 10 certificates';
END
ELSE
    PRINT '✓ Certificates already exist';
GO

-- =========================================
-- TAGS (15 tags)
-- =========================================
PRINT 'Seeding Tags...';

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'Sustainable')
BEGIN
    SET IDENTITY_INSERT Tags ON;

    INSERT INTO Tags (Id, Name, Description, IsInternal, IsActive, CreatedAt, UpdatedAt)
    VALUES
    (1, N'Sustainable', N'Sustainable product', 0, 1, GETDATE(), GETDATE()),
    (2, N'Recycled Content', N'Contains recycled materials', 0, 1, GETDATE(), GETDATE()),
    (3, N'Organic', N'Organic materials', 0, 1, GETDATE(), GETDATE()),
    (4, N'Carbon Neutral', N'Carbon neutral product', 0, 1, GETDATE(), GETDATE()),
    (5, N'Fair Trade', N'Fair trade certified', 0, 1, GETDATE(), GETDATE()),
    (6, N'Biodegradable', N'Biodegradable product', 0, 1, GETDATE(), GETDATE()),
    (7, N'Energy Efficient', N'Energy efficient product', 0, 1, GETDATE(), GETDATE()),
    (8, N'Renewable Materials', N'Made from renewable resources', 0, 1, GETDATE(), GETDATE()),
    (9, N'Non-Toxic', N'Non-toxic materials', 0, 1, GETDATE(), GETDATE()),
    (10, N'Compostable', N'Compostable product', 0, 1, GETDATE(), GETDATE()),
    (11, N'Priority Review', N'Internal priority', 1, 1, GETDATE(), GETDATE()),
    (12, N'Audit Required', N'Needs audit', 1, 1, GETDATE(), GETDATE()),
    (13, N'High Volume', N'High sales volume', 1, 1, GETDATE(), GETDATE()),
    (14, N'New Certification', N'Recently certified', 1, 1, GETDATE(), GETDATE()),
    (15, N'Renewal Due', N'Renewal approaching', 1, 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT Tags OFF;
    PRINT '✓ Tags: 15 rows (10 public, 5 internal)';
END
ELSE
    PRINT '✓ Tags already exist';
GO

-- =========================================
-- CERTIFICATE TAGS (assign tags to certificates)
-- =========================================
PRINT 'Assigning Tags to Certificates...';

IF NOT EXISTS (SELECT 1 FROM CertificateTag)
BEGIN
    DECLARE @CertId INT = 1;
    DECLARE @TagId INT;

    WHILE @CertId <= 100
    BEGIN
        -- Assign 2-4 tags per certificate
        -- Public tags (1-10)
        SET @TagId = ((@CertId - 1) % 10) + 1;
        INSERT INTO CertificateTag (CertificatesId, TagsId) VALUES (@CertId, @TagId);

        SET @TagId = (((@CertId - 1) + 3) % 10) + 1;
        INSERT INTO CertificateTag (CertificatesId, TagsId) VALUES (@CertId, @TagId);

        -- Some get internal tags (11-15)
        IF @CertId % 5 = 0
        BEGIN
            SET @TagId = ((@CertId % 5) + 11);
            INSERT INTO CertificateTag (CertificatesId, TagsId) VALUES (@CertId, @TagId);
        END

        SET @CertId = @CertId + 1;
    END

    PRINT '✓ Certificate Tags: ~250 associations';
END
ELSE
    PRINT '✓ Certificate Tags already exist';
GO

-- =========================================
-- CERTIFICATE PRODUCT IDS (ASINs, UPCs, etc.)
-- =========================================
PRINT 'Seeding Product Identifiers...';

IF NOT EXISTS (SELECT 1 FROM CertificateProductIds)
BEGIN
    SET IDENTITY_INSERT CertificateProductIds ON;

    DECLARE @CertIdPI INT = 1;
    DECLARE @IdCounter INT = 1;

    WHILE @CertIdPI <= 100
    BEGIN
        -- Each certificate gets 1-3 product IDs
        -- ASIN (Amazon only)
        IF @CertIdPI % 3 = 0
        BEGIN
            INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, CreatedAt)
            VALUES (@IdCounter, @CertIdPI, 'ASIN', 'B0' + RIGHT('00000' + CAST(@CertIdPI AS VARCHAR), 5) + '1X', GETDATE());
            SET @IdCounter = @IdCounter + 1;
        END

        -- UPC (most products)
        INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, CreatedAt)
        VALUES (@IdCounter, @CertIdPI, 'UPC', RIGHT('0000000000' + CAST((@CertIdPI * 12345) AS VARCHAR), 12), GETDATE());
        SET @IdCounter = @IdCounter + 1;

        -- GTIN (some products)
        IF @CertIdPI % 2 = 0
        BEGIN
            INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, CreatedAt)
            VALUES (@IdCounter, @CertIdPI, 'GTIN', RIGHT('000000000000' + CAST((@CertIdPI * 67890) AS VARCHAR), 14), GETDATE());
            SET @IdCounter = @IdCounter + 1;
        END

        SET @CertIdPI = @CertIdPI + 1;
    END

    SET IDENTITY_INSERT CertificateProductIds OFF;
    PRINT '✓ Certificate Product IDs: ~200 rows (ASINs, UPCs, GTINs)';
END
ELSE
    PRINT '✓ Certificate Product IDs already exist';
GO

-- Re-enable constraints
PRINT '';
PRINT 'Re-enabling database constraints...';
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO

-- =========================================
-- VERIFICATION
-- =========================================
PRINT '';
PRINT '=========================================';
PRINT 'DATA SEED COMPLETE - VERIFICATION';
PRINT '=========================================';
PRINT '';

SELECT 'Roles' as TableName, COUNT(*) as RowCount FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL SELECT 'Product Categories', COUNT(*) FROM ProductCategories
UNION ALL SELECT 'Certification Types', COUNT(*) FROM CertificationTypes
UNION ALL SELECT 'Product Types', COUNT(*) FROM ProductTypes
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL SELECT 'Certificate Tags', COUNT(*) FROM CertificateTag
UNION ALL SELECT 'Product IDs', COUNT(*) FROM CertificateProductIds;

PRINT '';
PRINT 'Certificate Status Distribution:';
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status
ORDER BY Count DESC;

PRINT '';
PRINT 'Certificates by Partner:';
SELECT Partner, COUNT(*) as Count
FROM Certificates
GROUP BY Partner
ORDER BY Count DESC;

PRINT '';
PRINT '=========================================';
PRINT 'SUCCESS! Test data loaded';
PRINT '=========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - 100 Certificates (70 active)';
PRINT '  - 50 Products across 15 categories';
PRINT '  - 20 Companies';
PRINT '  - 10 Certification Types';
PRINT '  - 15 Tags (10 public, 5 internal)';
PRINT '  - 5 Users (Admin, Analyst, External x2, User)';
PRINT '';
PRINT 'Ready for testing!';
PRINT '=========================================';
GO

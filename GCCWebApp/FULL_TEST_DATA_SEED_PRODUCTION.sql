-- ============================================
-- GCC Complete Test Data Seeding Script
-- ============================================
-- This script seeds COMPLETE test data for development/test environments
-- Includes: 175+ certificates, 28 companies, 43 products, 15 users
-- Based on: Data/SeedData.cs
-- Database: SQL Server
-- Safe to run multiple times (idempotent)
-- ============================================

SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'GCC Complete Test Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- 1. Product Categories
-- ============================================
PRINT '1. Seeding Product Categories...';

IF NOT EXISTS (SELECT 1 FROM dbo.ProductCategories)
BEGIN
    SET IDENTITY_INSERT dbo.ProductCategories ON;

    INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive)
    VALUES
        (1, 'Building Materials', 'Building materials and construction products', 1, 1),
        (2, 'Electronics / Electrical', 'Electronic and electrical products', 2, 1),
        (3, 'Materials / Plastics', 'Plastic materials and products', 3, 1),
        (4, 'Paper Products', 'Paper and paper-based products', 4, 1),
        (5, 'Textiles', 'Textile materials', 5, 1),
        (6, 'Consumer Products', 'Consumer goods and products', 6, 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.ProductCategories OFF;
    PRINT 'âœ“ 6 Product Categories created';
END
ELSE
    PRINT 'â—‹ Product Categories already exist, skipping';
PRINT '';

-- ============================================
-- 2. Certification Types
-- ============================================
PRINT '2. Seeding Certification Types...';

IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes)
BEGIN
    SET IDENTITY_INSERT dbo.CertificationTypes ON;

    INSERT INTO dbo.CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder)
    VALUES
        (1, 'Certified Environmental Facts', 'CEF', 'Environmental impact certification', 1, 1),
        (2, 'Certified Sustainability Facts', 'CSF', 'Comprehensive sustainability certification', 1, 2),
        (3, 'Recycled Content', 'RC', 'Recycled content certification', 1, 3),
        (4, 'Energy Savings', 'ES', 'Energy efficiency certification', 1, 4),
        (5, 'Material Ingredient Reporting', 'MIR', 'Material composition reporting', 1, 5);

    SET IDENTITY_INSERT dbo.CertificationTypes OFF;
    PRINT 'âœ“ 5 Certification Types created';
END
ELSE
    PRINT 'â—‹ Certification Types already exist, skipping';
PRINT '';

-- ============================================
-- 3. Partners
-- ============================================
PRINT '3. Seeding Partners...';

IF NOT EXISTS (SELECT 1 FROM dbo.Partners)
BEGIN
    SET IDENTITY_INSERT dbo.Partners ON;

    INSERT INTO dbo.Partners (Id, Name, Description, Website, SortOrder, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (1, 'Amazon', 'Global e-commerce marketplace specializing in certified sustainable products',
         'https://www.amazon.com', 1, 1, 0, GETDATE(), GETDATE()),
        (2, 'Wayfair', 'Online furniture and home goods retailer focused on sustainable product offerings',
         'https://www.wayfair.com', 2, 1, 0, GETDATE(), GETDATE()),
        (3, 'Other', 'Other retail partners and general partner category',
         NULL, 3, 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Partners OFF;
    PRINT 'âœ“ 3 Partners created';
END
ELSE
    PRINT 'â—‹ Partners already exist, skipping';
PRINT '';

-- ============================================
-- 4. Companies (28 companies)
-- ============================================
PRINT '4. Seeding Companies...';

IF NOT EXISTS (SELECT 1 FROM dbo.Companies)
BEGIN
    SET IDENTITY_INSERT dbo.Companies ON;

    INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt)
    VALUES
        (1, 'ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division', 'sustainability@assaabloy.com', 'Berlin, CT', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (2, 'Green Products Inc. - Environmental Division', 'contact@greenproducts.com', 'Portland, OR', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (3, 'Acme Corporation', 'info@acme.com', 'New York, NY', 'United States', 1, 0, '', GETDATE(), GETDATE()),
        (4, 'Globex Industries', 'sales@globex.com', 'Chicago, IL', 'United States', 1, 0, '', GETDATE(), GETDATE()),
        (5, 'Stark Enterprises', 'hello@stark.com', 'Malibu, CA', 'United States', 1, 0, '', GETDATE(), GETDATE()),
        (6, 'Wayne Technologies', 'contact@waynetech.com', 'Gotham, NY', 'United States', 1, 0, '', GETDATE(), GETDATE()),
        (7, 'EcoFlex Manufacturing', 'info@ecoflex.com', 'Austin, TX', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (8, 'GAF Materials Corporation', 'green@gaf.com', 'Cedar City, UT', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (9, '3M Company', 'sustainability@3m.com', 'St. Paul, MN', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (10, 'Shaw Industries Group', 'green@shawinc.com', 'Dalton, GA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (11, 'Mohawk Industries', 'sustainability@mohawkind.com', 'Calhoun, GA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (12, 'Armstrong World Industries', 'green@armstrong.com', 'Lancaster, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (13, 'Owens Corning', 'sustainability@owenscorning.com', 'Toledo, OH', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (14, 'CertainTeed Corporation', 'sustainability@certainteed.com', 'Malvern, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (15, 'Johns Manville', 'environmental@jm.com', 'Denver, CO', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (16, 'Kingspan Insulation', 'green@kingspan.com', 'Atlanta, GA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (17, 'Hunter Douglas', 'sustainability@hunterdouglas.com', 'Pearl River, NY', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (18, 'Steelcase Inc.', 'sustainability@steelcase.com', 'Grand Rapids, MI', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (19, 'Herman Miller Inc.', 'green@hermanmiller.com', 'Zeeland, MI', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (20, 'Knoll Inc.', 'sustainability@knoll.com', 'East Greenville, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (21, 'Teknion Corporation', 'environmental@teknion.com', 'Toronto, ON', 'Canada', 1, 0, NULL, GETDATE(), GETDATE()),
        (22, 'Forbo Flooring Systems', 'sustainability@forbo.com', 'Hazleton, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (23, 'Tarkett USA Inc.', 'green@tarkett.com', 'Solon, OH', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (24, 'Milliken & Company', 'sustainability@milliken.com', 'Spartanburg, SC', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (25, 'USG Corporation', 'environmental@usg.com', 'Chicago, IL', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (26, 'Lutron Electronics', 'sustainability@lutron.com', 'Coopersburg, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (27, 'Philips Lighting', 'green@philips.com', 'Somerset, NJ', 'United States', 1, 0, NULL, GETDATE(), GETDATE()),
        (28, 'Cree Lighting', 'sustainability@cree.com', 'Durham, NC', 'United States', 1, NULL);

    SET IDENTITY_INSERT dbo.Companies OFF;
    PRINT 'âœ“ 28 Companies created';
END
ELSE
    PRINT 'â—‹ Companies already exist, skipping';
PRINT '';

-- ============================================
-- 5. Product Types
-- ============================================
PRINT '5. Seeding Product Types...';

IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes)
BEGIN
    SET IDENTITY_INSERT dbo.ProductTypes ON;

    -- Parent types
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId)
    VALUES
        (1, 'Insulation', 1, 1, 1, 1, NULL),
        (2, 'Appliances', 1, 1, 2, 1, NULL),
        (3, 'Flooring', 1, 1, 3, 1, NULL),
        (4, 'Carpeting', 5, 1, 1, 1, NULL),
        (5, 'Fabric', 5, 1, 2, 1, NULL),
        (6, 'Network Cables', 2, 1, 1, 1, NULL),
        (7, 'Lighting', 2, 1, 2, 1, NULL),
        (8, 'Furniture', 6, 1, 1, 1, NULL);

    -- Sub types
    INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId)
    VALUES
        (9, 'Foam Board', 1, 2, 1, 1, 1),
        (10, 'Kitchen Appliances', 1, 2, 1, 1, 2),
        (11, 'Laundry Appliances', 1, 2, 2, 1, 2),
        (12, 'Hardwood', 1, 2, 1, 1, 3),
        (13, 'Laminate', 1, 2, 2, 1, 3),
        (14, 'Commercial Carpet', 5, 2, 1, 1, 4),
        (15, 'Residential Carpet', 5, 2, 2, 1, 4),
        (16, 'Upholstery Fabric', 5, 2, 1, 1, 5),
        (17, 'Apparel Fabric', 5, 2, 2, 1, 5),
        (18, 'Office Chairs', 6, 2, 1, 1, 8),
        (19, 'Tables', 6, 2, 2, 1, 8);

    SET IDENTITY_INSERT dbo.ProductTypes OFF;
    PRINT 'âœ“ 19 Product Types created';
END
ELSE
    PRINT 'â—‹ Product Types already exist, skipping';
PRINT '';

-- ============================================
-- 6. Products (43 products)
-- ============================================
PRINT '6. Seeding Products...';

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;

    INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (1, '900ET Exit Device Trim with EcoFlexÂ®', 'Exit device trim with sustainable EcoFlex technology', 1, 1, 1, 1),
        (2, 'Standard Exit Device - Model 9800', 'Commercial grade exit device', 1, 1, 1, 1),
        (3, 'Heavy-Duty Exit Hardware - Series 2000', 'Heavy-duty commercial exit hardware', 1, 1, 1, 1),
        (4, 'Commercial Door Closer - DC300', 'Commercial grade door closing mechanism', 1, 1, 1, 1),
        (5, 'Security Exit Device - EX700', 'High-security exit device system', 1, 1, 1, 1),
        (6, 'Reusable Water Bottle - Stainless Steel', 'Eco-friendly stainless steel water bottle', 6, NULL, 2, 1),
        (7, 'Bamboo Coffee Cup with Lid', 'Sustainable bamboo coffee cup', 6, NULL, 2, 1),
        (8, 'Glass Storage Container Set', 'Reusable glass storage containers', 6, NULL, 2, 1),
        (9, 'Organic Cotton Tote Bag', 'Sustainable organic cotton tote bag', 5, NULL, 2, 1),
        (10, '10Gain XP Category 6A Cable', 'High-performance network cable', 2, 6, 4, 1),
        (11, 'Carbon Negative Carpet Tile', 'Sustainable carpet tiles', 5, 4, 5, 1),
        (12, '3M VHB Tape', 'Very High Bond structural glazing tape', 1, NULL, 9, 1),
        (13, 'Shaw Contract Carpet Tile', 'Commercial carpet tile with recycled content', 5, 4, 10, 1),
        (14, 'Mohawk Hardwood Flooring', 'Sustainable hardwood flooring', 1, 3, 11, 1),
        (15, 'Armstrong Ceiling Tiles', 'Mineral fiber ceiling tiles', 1, NULL, 12, 1),
        (16, 'Owens Corning Fiberglas Insulation', 'Pink fiberglass insulation batts', 1, 1, 13, 1),
        (17, 'LED Light Fixtures', 'Energy-efficient LED lighting', 2, 7, 9, 1),
        (18, 'Office Chair', 'Ergonomic office seating', 6, 8, 10, 1),
        (19, 'CertainTeed Fiber Glass Insulation', 'Sustainable fiberglass insulation with recycled content', 1, 1, 14, 1),
        (20, 'Johns Manville Mineral Wool Insulation', 'Fire-resistant mineral wool insulation', 1, 1, 15, 1),
        (21, 'Kingspan Kooltherm K5 External Wall Board', 'High-performance insulation board', 1, 1, 16, 1),
        (22, 'Hunter Douglas Architectural Window Treatments', 'Energy-efficient window coverings', 1, NULL, 17, 1),
        (23, 'Steelcase Think Ergonomic Office Chair', 'Sustainable office seating with recycled materials', 6, 8, 18, 1),
        (24, 'Herman Miller Aeron Chair', 'Iconic ergonomic chair with sustainable design', 6, 8, 19, 1),
        (25, 'Knoll ReGeneration Chair', 'Made from recycled materials', 6, 8, 20, 1),
        (26, 'Teknion District Chair', 'Sustainable office seating solution', 6, 8, 21, 1),
        (27, 'Forbo Marmoleum Flooring', 'Natural linoleum flooring with low environmental impact', 1, 3, 22, 1),
        (28, 'Tarkett iQ Surface Flooring', 'Sustainable homogeneous vinyl flooring', 1, 3, 23, 1),
        (29, 'Milliken Modular Carpet', 'Carbon neutral carpet tiles', 5, 4, 24, 1),
        (30, 'USG Sheetrock Brand Gypsum Panels', 'Sustainable drywall with recycled content', 1, NULL, 25, 1),
        (31, 'Lutron Caseta Smart Dimmer Switch', 'Energy-saving smart lighting control', 2, 7, 26, 1),
        (32, 'Philips LED Tube Light', 'Energy-efficient LED lighting solution', 2, 7, 27, 1),
        (33, 'Cree LED High Bay Fixture', 'Industrial LED lighting with smart controls', 2, 7, 28, 1),
        (34, 'Interface Net-Works Carpet Tiles', 'Made from recycled fishing nets', 5, 4, 5, 1),
        (35, '3M Scotchshield Ultra Safety & Security Window Film', 'Window protection film', 1, NULL, 9, 1),
        (36, 'Shaw EcoWorx Carpet Backing', 'Recyclable carpet backing system', 5, 4, 10, 1),
        (37, 'Armstrong Clean Room Ceiling Panels', 'Specialized ceiling tiles for clean environments', 1, NULL, 12, 1),
        (38, 'GAF TPO Roofing Membrane', 'Single-ply roofing membrane', 1, NULL, 8, 1),
        (39, 'Superior Essex PowerWise Cable', 'Energy-efficient data center cable', 2, 6, 4, 1, 0, GETDATE(), GETDATE());

    -- Add more products to reach 43
    INSERT INTO dbo.Products (Id, Name, Description, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (40, 'EcoFlex Sustainable Packaging', 'Biodegradable packaging materials', 3, 7, 1),
        (41, 'Green Paper Products - Recycled Copy Paper', 'Office paper with 100% recycled content', 4, 2, 1),
        (42, 'Sustainable Wood Furniture Collection', 'FSC-certified wood furniture', 6, 18, 1),
        (43, 'Energy Star Certified Appliances', 'High-efficiency home appliances', 1, 3, 1, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Products OFF;
    PRINT 'âœ“ 43 Products created';
END
ELSE
    PRINT 'â—‹ Products already exist, skipping';
PRINT '';

-- ============================================
-- 7. Roles
-- ============================================
PRINT '7. Seeding Roles...';

IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    SET IDENTITY_INSERT dbo.Roles ON;

    INSERT INTO dbo.Roles (Id, Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES
        (1, 'Admin', 'Full system access', 1, 1, GETDATE(), GETDATE()),
        (2, 'Analyst', 'Certificate management access', 1, 1, GETDATE(), GETDATE()),
        (3, 'External', 'External user access', 1, 1, GETDATE(), GETDATE()),
        (4, 'Public', 'Public read-only access', 1, 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Roles OFF;
    PRINT 'âœ“ 4 Roles created';
END
ELSE
    PRINT 'â—‹ Roles already exist, skipping';
PRINT '';

-- ============================================
-- 8. Permissions
-- ============================================
PRINT '8. Seeding Permissions...';

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions)
BEGIN
    SET IDENTITY_INSERT dbo.Permissions ON;

    INSERT INTO dbo.Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES
        (1, 'View Certificates', 'certificate_view', 'Certificate', 'Can view certificates', 1, GETDATE()),
        (2, 'Create Certificates', 'certificate_create', 'Certificate', 'Can create new certificates', 1, GETDATE()),
        (3, 'Edit Certificates', 'certificate_edit', 'Certificate', 'Can edit existing certificates', 1, GETDATE()),
        (4, 'Delete Certificates', 'certificate_delete', 'Certificate', 'Can delete certificates', 1, GETDATE()),
        (5, 'Manage Users', 'user_manage', 'User', 'Can manage user accounts', 1, GETDATE()),
        (6, 'View Reports', 'report_view', 'Report', 'Can view system reports', 1, GETDATE());

    SET IDENTITY_INSERT dbo.Permissions OFF;
    PRINT 'âœ“ 6 Permissions created';
END
ELSE
    PRINT 'â—‹ Permissions already exist, skipping';
PRINT '';

-- ============================================
-- 9. Role-Permission Assignments
-- ============================================
PRINT '9. Assigning permissions to roles...';

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionRole)
BEGIN
    -- Admin gets all permissions
    INSERT INTO dbo.PermissionRole (RolesId, PermissionsId)
    SELECT 1, Id FROM dbo.Permissions;

    -- Analyst gets certificate management (except delete)
    INSERT INTO dbo.PermissionRole (RolesId, PermissionsId)
    SELECT 2, Id
    FROM dbo.Permissions
    WHERE Codename IN ('certificate_view', 'certificate_create', 'certificate_edit', 'report_view');

    -- External gets view and reports
    INSERT INTO dbo.PermissionRole (RolesId, PermissionsId)
    SELECT 3, Id
    FROM dbo.Permissions
    WHERE Codename IN ('certificate_view', 'report_view');

    -- Public gets only view
    INSERT INTO dbo.PermissionRole (RolesId, PermissionsId)
    SELECT 4, Id
    FROM dbo.Permissions
    WHERE Codename = 'certificate_view';

    PRINT 'âœ“ Permissions assigned to all roles';
END
ELSE
    PRINT 'â—‹ Role-Permission assignments already exist, skipping';
PRINT '';

-- ============================================
-- 10. Users (15 test users)
-- ============================================
PRINT '10. Seeding Users...';

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    SET IDENTITY_INSERT dbo.Users ON;

    -- Note: Password hash is for "TestPass123!" using BCrypt
    DECLARE @PasswordHash NVARCHAR(500) = '$2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m';

    INSERT INTO dbo.Users (Id, Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod, IsActive, IsStaff, IsSuperuser, CreatedAt, UpdatedAt)
    VALUES
        (1, 'admin', 'admin@gcc.com', 'Admin', 'Admin User', @PasswordHash, 'active', 'Email', 1, 1, 1, GETDATE(), GETDATE()),
        (2, 'analyst', 'analyst@gcc.com', 'Jane', 'Jane Analyst', @PasswordHash, 'active', 'Email', 1, 1, 0, GETDATE(), GETDATE()),
        (3, 'external', 'external@company.com', 'External', 'External User', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (4, 'testuser', 'test@gcc.com', 'Test', 'Test User', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (5, 'sarah.johnson', 'sarah.johnson@gcc.com', 'Sarah', 'Sarah Johnson', @PasswordHash, 'active', 'Email', 1, 1, 0, GETDATE(), GETDATE()),
        (6, 'mike.chen', 'mike.chen@gcc.com', 'Mike', 'Mike Chen', @PasswordHash, 'active', 'Email', 1, 1, 0, GETDATE(), GETDATE()),
        (7, 'lisa.rodriguez', 'lisa.rodriguez@assaabloy.com', 'Lisa', 'Lisa Rodriguez', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (8, 'david.kim', 'david.kim@3m.com', 'David', 'David Kim', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (9, 'emma.wilson', 'emma.wilson@interface.com', 'Emma', 'Emma Wilson', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (10, 'alex.martinez', 'alex.martinez@steelcase.com', 'Alex', 'Alex Martinez', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (11, 'jennifer.lee', 'jennifer.lee@gcc.com', 'Jennifer', 'Jennifer Lee', @PasswordHash, 'active', 'Email', 1, 1, 0, GETDATE(), GETDATE()),
        (12, 'robert.taylor', 'robert.taylor@gcc.com', 'Robert', 'Robert Taylor', @PasswordHash, 'active', 'Email', 1, 1, 0, GETDATE(), GETDATE()),
        (13, 'maria.garcia', 'maria.garcia@mohawkind.com', 'Maria', 'Maria Garcia', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (14, 'public.user', 'public@example.com', 'Public', 'Public User', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE()),
        (15, 'partner', 'partner@amazon.com', 'Partner', 'Amazon Partner', @PasswordHash, 'active', 'Email', 1, 0, 0, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Users OFF;
    PRINT 'âœ“ 15 Users created';
END
ELSE
    PRINT 'â—‹ Users already exist, skipping';
PRINT '';

-- ============================================
-- 11. User-Role Assignments
-- ============================================
PRINT '11. Assigning roles to users...';

IF NOT EXISTS (SELECT 1 FROM dbo.RoleUser)
BEGIN
    -- Admin users
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (1, 1, 0, GETDATE(), GETDATE());  -- admin@gcc.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (1, 12); -- robert.taylor@gcc.com

    -- Analyst users
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (2, 2);  -- analyst@gcc.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (2, 5);  -- sarah.johnson@gcc.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (2, 6);  -- mike.chen@gcc.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (2, 11); -- jennifer.lee@gcc.com

    -- External users
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 3);  -- external@company.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 7);  -- lisa.rodriguez@assaabloy.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 8);  -- david.kim@3m.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 9);  -- emma.wilson@interface.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 10); -- alex.martinez@steelcase.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 13); -- maria.garcia@mohawkind.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 15); -- partner@amazon.com

    -- Public users
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (4, 4);  -- test@gcc.com
    INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (4, 14); -- public@example.com

    PRINT 'âœ“ User-Role assignments created';
END
ELSE
    PRINT 'â—‹ User-Role assignments already exist, skipping';
PRINT '';

-- ============================================
-- 12. Base Certificates (25 seed certificates)
-- ============================================
PRINT '12. Seeding base certificates...';

IF NOT EXISTS (SELECT 1 FROM dbo.Certificates)
BEGIN
    SET IDENTITY_INSERT dbo.Certificates ON;

    INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Asin, Upc, Gtin, Ean, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES
        -- Primary example from mockup
        (1, '24-1605', 'active', '2024-07-01', '2027-06-30', 'Berlin, CT', 'United States', 'B08X7N9MRQ', '846378985421', '00846378985421', '0846378985421', 1, 1, 1, '2023-01-01', '2023-04-15'),
        (2, '24-1606', 'active', '2024-09-01', '2027-08-31', 'Portland, OR', 'United States', NULL, '846378985422', '00846378985422', '0846378985422', 2, 6, 2, '2023-01-01', '2023-12-31'),
        (3, '24-1607', 'active', '2024-08-01', '2027-07-31', 'Berlin, CT', 'United States', NULL, NULL, NULL, NULL, 1, 2, 2, '2023-01-01', '2023-04-15'),
        (4, '24-1608', 'draft', '2025-01-01', '2028-12-31', 'Berlin, CT', 'United States', NULL, NULL, NULL, NULL, 1, 3, 1, GETDATE(), GETDATE()),
        (5, '24-1609', 'expired', '2021-01-01', '2024-01-01', 'Berlin, CT', 'United States', NULL, NULL, NULL, NULL, 1, 4, 3, '2021-01-01', '2024-01-01'),
        (6, '24-5712', 'active', '2024-11-25', '2027-11-24', 'Ashdown, AR', 'United States', NULL, '456789012345', NULL, NULL, 3, 3, 1, GETDATE(), GETDATE()),
        (7, '24-5633', 'active', '2024-10-01', '2027-09-30', 'Hoisington, KS', 'United States', 'B09XYZ7890', '234567890123', NULL, NULL, 4, 4, 1, GETDATE(), GETDATE()),
        (8, '24-2108', 'active', '2024-10-01', '2027-09-30', 'Dalton, GA', 'United States', 'B09DEF1234', '345678901234', NULL, NULL, 5, 5, 3, GETDATE(), GETDATE()),
        (9, '24-6201', 'active', '2024-12-01', '2027-11-30', 'St. Paul, MN', 'United States', 'B10ABC1234', '456789012346', NULL, NULL, 9, 6, 2, GETDATE(), GETDATE()),
        (10, '24-6385', 'active', '2024-11-15', '2025-11-14', 'Dalton, GA', 'United States', NULL, '567890123457', NULL, NULL, 10, 7, 1, GETDATE(), GETDATE()),
        (11, '24-7892', 'pending', '2025-01-01', '2028-12-31', 'Calhoun, GA', 'United States', 'B11XYZ5678', '678901234568', NULL, NULL, 11, 8, 3, GETDATE(), GETDATE()),
        (12, '24-8134', 'active', '2024-09-15', '2027-09-14', 'Lancaster, PA', 'United States', NULL, '789012345679', NULL, NULL, 12, 9, 4, GETDATE(), GETDATE()),
        (13, '24-9567', 'expired', '2021-06-01', '2024-05-31', 'Toledo, OH', 'United States', 'B12PQR9012', '890123456780', NULL, NULL, 13, 10, 1, GETDATE(), GETDATE()),
        (14, '25-0001', 'active', '2025-01-01', '2028-12-31', 'Malvern, PA', 'United States', 'B25CER0001', '125478963014', '01254789630142', '1254789630142', 14, 19, 1, GETDATE(), GETDATE()),
        (15, '25-0002', 'active', '2024-08-15', '2027-08-14', 'Denver, CO', 'United States', 'B25JOH0002', '147852369025', NULL, NULL, 15, 20, 4, GETDATE(), GETDATE()),
        (16, '25-0003', 'pending', '2025-03-01', '2028-02-28', 'Atlanta, GA', 'United States', NULL, '963741852036', NULL, NULL, 16, 21, 5, GETDATE(), GETDATE()),
        (17, '24-9876', 'active', '2024-07-01', '2026-06-30', 'Pearl River, NY', 'United States', 'B24HUN9876', '852741963047', NULL, NULL, 17, 22, 2, GETDATE(), GETDATE()),
        (18, '24-8765', 'active', '2024-06-01', '2027-05-31', 'Grand Rapids, MI', 'United States', 'B24STE8765', '741852963058', '07418529630589', NULL, 18, 23, 3, GETDATE(), GETDATE()),
        (19, '24-7654', 'active', '2024-05-15', '2027-05-14', 'Zeeland, MI', 'United States', 'B24HER7654', '630741852069', NULL, NULL, 19, 24, 1, GETDATE(), GETDATE()),
        (20, '24-6543', 'suspended', '2024-04-01', '2027-03-31', 'East Greenville, PA', 'United States', NULL, '529630741870', NULL, NULL, 20, 25, 2, GETDATE(), GETDATE()),
        (21, '24-5432', 'active', '2024-03-01', '2026-02-28', 'Toronto, ON', 'Canada', 'B24TEK5432', '418529630781', NULL, NULL, 21, 26, 4, GETDATE(), GETDATE()),
        (22, '24-4321', 'active', '2024-02-01', '2027-01-31', 'Hazleton, PA', 'United States', NULL, '307418529692', NULL, NULL, 22, 27, 5, GETDATE(), GETDATE()),
        (23, '24-3210', 'expiring_soon', '2022-01-01', '2025-03-31', 'Solon, OH', 'United States', 'B24TAR3210', '196307418503', NULL, NULL, 23, 28, 1, GETDATE(), GETDATE()),
        (24, '24-2109', 'active', '2024-01-01', '2026-12-31', 'Spartanburg, SC', 'United States', NULL, '085296307414', NULL, NULL, 24, 29, 3, GETDATE(), GETDATE()),
        (25, '23-9999', 'expired', '2020-12-01', '2023-11-30', 'Chicago, IL', 'United States', 'B23USG9999', '741963085225', NULL, NULL, 25, 30, 2, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.Certificates OFF;
    PRINT 'âœ“ 25 Base Certificates created';
END
ELSE
    PRINT 'â—‹ Certificates already exist, skipping';
PRINT '';

-- ============================================
-- 13. Generate Additional Certificates (150 more)
-- ============================================
PRINT '13. Generating additional test certificates...';

DECLARE @CertCount INT = (SELECT COUNT(*) FROM dbo.Certificates);

IF @CertCount < 175
BEGIN
    DECLARE @i INT = 26;
    DECLARE @MaxCerts INT = 175;
    DECLARE @CertNumber VARCHAR(20);
    DECLARE @Status VARCHAR(20);
    DECLARE @EffectiveDate DATE;
    DECLARE @ExpirationDate DATE;
    DECLARE @Location VARCHAR(100);
    DECLARE @CompanyId INT;
    DECLARE @ProductId INT;
    DECLARE @CertTypeId INT;

    DECLARE @Locations TABLE (Location VARCHAR(100), Country VARCHAR(50));
    INSERT INTO @Locations VALUES
        ('New York, NY', 'United States'), ('Los Angeles, CA', 'United States'),
        ('Chicago, IL', 'United States'), ('Houston, TX', 'United States'),
        ('Phoenix, AZ', 'United States'), ('Philadelphia, PA', 'United States'),
        ('San Antonio, TX', 'United States'), ('San Diego, CA', 'United States'),
        ('Dallas, TX', 'United States'), ('Austin, TX', 'United States'),
        ('Seattle, WA', 'United States'), ('Denver, CO', 'United States'),
        ('Boston, MA', 'United States'), ('Portland, OR', 'United States'),
        ('Atlanta, GA', 'United States'), ('Miami, FL', 'United States');

    WHILE @i <= @MaxCerts
    BEGIN
        -- Generate certificate number (format: YY-NNNN, max 7 chars)
        SET @CertNumber = CONCAT((24 + (@i % 2)), '-', RIGHT('0000' + CAST((5000 + @i) AS VARCHAR), 4));

        -- Random dates
        SET @EffectiveDate = DATEADD(DAY, -365 + (@i % 730), GETDATE());
        SET @ExpirationDate = DATEADD(YEAR, 1 + (@i % 3), @EffectiveDate);

        -- Determine status based on dates
        IF @ExpirationDate < GETDATE()
            SET @Status = 'expired';
        ELSE IF @ExpirationDate < DATEADD(MONTH, 3, GETDATE())
            SET @Status = 'expiring_soon';
        ELSE IF @EffectiveDate > GETDATE()
            SET @Status = CASE WHEN @i % 2 = 0 THEN 'draft' ELSE 'pending' END;
        ELSE
            SET @Status = 'active';

        -- Random company, product, cert type
        SET @CompanyId = 1 + (@i % 28);
        SET @ProductId = 1 + (@i % 43);
        SET @CertTypeId = 1 + (@i % 5);

        -- Get random location
        SELECT TOP 1 @Location = Location
        FROM @Locations
        ORDER BY NEWID();

        INSERT INTO dbo.Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
        VALUES (@CertNumber, @Status, @EffectiveDate, @ExpirationDate, @Location, 'United States', @CompanyId, @ProductId, @CertTypeId, DATEADD(DAY, -30, @EffectiveDate), DATEADD(DAY, -1, GETDATE()));

        SET @i = @i + 1;
    END;

    PRINT 'âœ“ ' + CAST(@MaxCerts - 25 AS VARCHAR) + ' Additional Certificates generated';
END
ELSE
    PRINT 'â—‹ Additional certificates already exist, skipping';
PRINT '';

-- ============================================
-- 14. Tags
-- ============================================
PRINT '14. Seeding Tags...';

IF NOT EXISTS (SELECT 1 FROM dbo.Tags)
BEGIN
    SET IDENTITY_INSERT dbo.Tags ON;

    INSERT INTO dbo.Tags (Id, Name, TagType, Description, IsActive)
    VALUES
        (1, 'Environmental Facts', 'external', 'Environmental impact data', 1),
        (2, 'Recycled Content', 'external', 'Contains recycled materials', 1),
        (3, 'Carbon Neutral', 'external', 'Carbon neutral product', 1),
        (4, 'High Priority Client', 'internal', 'Important client certification', 1),
        (5, 'Q4 Review', 'internal', 'Scheduled for quarterly review', 1),
        (6, 'Energy Star', 'external', 'Energy Star certified product', 1),
        (7, 'LEED Points', 'external', 'Contributes to LEED certification', 1),
        (8, 'Cradle to Cradle', 'external', 'Cradle to Cradle certified', 1),
        (9, 'Forest Stewardship Council', 'external', 'FSC certified materials', 1),
        (10, 'GREENGUARD', 'external', 'Low chemical emissions', 1);

    SET IDENTITY_INSERT dbo.Tags OFF;
    PRINT 'âœ“ 10 Tags created';
END
ELSE
    PRINT 'â—‹ Tags already exist, skipping';
PRINT '';

-- ============================================
-- 15. API Keys
-- ============================================
PRINT '15. Seeding API Keys...';

IF NOT EXISTS (SELECT 1 FROM dbo.ApiKeys)
BEGIN
    SET IDENTITY_INSERT dbo.ApiKeys ON;

    INSERT INTO dbo.ApiKeys (Id, Name, [Key], KeyHash, Description, CreatedBy, IsActive, CreatedAt, LastUsedAt, ExpiresAt, Environment, Scopes, RateLimitPerHour)
    VALUES
        (1, 'Integration Test Key', 'gcc_test_key_1234567890abcdef', 'test_key_hash_1234567890abcdef',
         'API key for integration testing', 'admin', 1, GETDATE(), GETDATE(), DATEADD(YEAR, 1, GETDATE()), 'Development', 'read,write,admin', 10000),
        (2, 'Partner API Access', 'gcc_partner_key_abcdef1234567890', 'partner_key_hash_abcdef1234567890',
         'API access for external partners', 'partner', 1, GETDATE(), DATEADD(DAY, -5, GETDATE()), DATEADD(MONTH, 6, GETDATE()), 'Production', 'read', 1000),
        (3, 'Mobile App Key', 'gcc_mobile_key_fedcba0987654321', 'mobile_key_hash_fedcba0987654321',
         'API key for mobile application', 'analyst', 1, DATEADD(DAY, -30, GETDATE()), DATEADD(HOUR, -2, GETDATE()), DATEADD(MONTH, 3, GETDATE()), 'Production', 'read,write', 5000);

    SET IDENTITY_INSERT dbo.ApiKeys OFF;
    PRINT 'âœ“ 3 API Keys created';
END
ELSE
    PRINT 'â—‹ API Keys already exist, skipping';
PRINT '';

-- Commit transaction
COMMIT TRANSACTION;

PRINT '';
PRINT '============================================';
PRINT 'SEEDING COMPLETE';
PRINT '============================================';

-- Generate statistics
PRINT '';
PRINT 'Database Statistics:';
SELECT 'ProductCategories' AS EntityType, COUNT(*) AS Count FROM dbo.ProductCategories
UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM dbo.CertificationTypes
UNION ALL SELECT 'Partners', COUNT(*) FROM dbo.Partners
UNION ALL SELECT 'Companies', COUNT(*) FROM dbo.Companies
UNION ALL SELECT 'ProductTypes', COUNT(*) FROM dbo.ProductTypes
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM dbo.Certificates
UNION ALL SELECT 'Tags', COUNT(*) FROM dbo.Tags
UNION ALL SELECT 'Roles', COUNT(*) FROM dbo.Roles
UNION ALL SELECT 'Permissions', COUNT(*) FROM dbo.Permissions
UNION ALL SELECT 'Users', COUNT(*) FROM dbo.Users
UNION ALL SELECT 'ApiKeys', COUNT(*) FROM dbo.ApiKeys
ORDER BY EntityType;

PRINT '';
PRINT 'Test User Credentials:';
PRINT 'Admin: admin@gcc.com / TestPass123!';
PRINT 'Analyst: analyst@gcc.com / TestPass123!';
PRINT 'External: partner@amazon.com / TestPass123!';
PRINT '';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED DURING SEEDING';
    PRINT '============================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR);
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR);
    PRINT '============================================';

    -- Re-throw the error
    THROW;
END CATCH;

GO


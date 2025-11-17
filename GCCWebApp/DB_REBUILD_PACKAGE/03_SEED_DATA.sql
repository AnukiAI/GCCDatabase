-- ============================================
-- GCC Production Essential Data Seeding Script
-- ============================================
-- This script seeds ONLY essential data required for production
-- Does NOT include test users or test certificates
-- Safe to run in production environment
-- ============================================

PRINT '============================================';
PRINT 'GCC Production Essential Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

-- ============================================
-- 1. Product Categories (Required for app)
-- ============================================
PRINT '1. Seeding Product Categories...';

IF NOT EXISTS (SELECT 1 FROM ProductCategories)
BEGIN
    INSERT INTO ProductCategories (Name, Description, SortOrder, IsActive)
    VALUES
        ('Building Materials', 'Building materials and construction products', 1, 1),
        ('Electronics / Electrical', 'Electronic and electrical products', 2, 1),
        ('Materials / Plastics', 'Plastic materials and products', 3, 1),
        ('Paper Products', 'Paper and paper-based products', 4, 1),
        ('Textiles', 'Textile materials', 5, 1),
        ('Consumer Products', 'Consumer goods and products', 6, 1);

    PRINT '✓ 6 Product Categories created';
END
ELSE
    PRINT '○ Product Categories already exist, skipping';
PRINT '';

-- ============================================
-- 2. Certification Types (Required for app)
-- ============================================
PRINT '2. Seeding Certification Types...';

IF NOT EXISTS (SELECT 1 FROM CertificationTypes)
BEGIN
    INSERT INTO CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder)
    VALUES
        ('Certified Environmental Facts', 'CEF', 'Environmental impact certification', 1, 1),
        ('Certified Sustainability Facts', 'CSF', 'Comprehensive sustainability certification', 1, 2),
        ('Recycled Content', 'RC', 'Recycled content certification', 1, 3),
        ('Energy Savings', 'ES', 'Energy efficiency certification', 1, 4),
        ('Material Ingredient Reporting', 'MIR', 'Material composition reporting', 1, 5);

    PRINT '✓ 5 Certification Types created';
END
ELSE
    PRINT '○ Certification Types already exist, skipping';
PRINT '';

-- ============================================
-- 3. Partners (Required for app)
-- ============================================
PRINT '3. Seeding Partners...';

IF NOT EXISTS (SELECT 1 FROM Partners)
BEGIN
    INSERT INTO Partners (Name, Description, Website, SortOrder, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        ('Amazon', 'Global e-commerce marketplace specializing in certified sustainable products',
         'https://www.amazon.com', 1, 1, 0, GETDATE(), GETDATE()),
        ('Wayfair', 'Online furniture and home goods retailer focused on sustainable product offerings',
         'https://www.wayfair.com', 2, 1, 0, GETDATE(), GETDATE()),
        ('Other', 'Other retail partners and general partner category',
         NULL, 3, 1, 0, GETDATE(), GETDATE());

    PRINT '✓ 3 Partners created';
END
ELSE
    PRINT '○ Partners already exist, skipping';
PRINT '';

-- ============================================
-- 4. Roles (Required for app)
-- ============================================
PRINT '4. Seeding Roles...';

IF NOT EXISTS (SELECT 1 FROM Roles)
BEGIN
    INSERT INTO Roles (Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES
        ('Admin', 'Full system access', 1, 1, GETDATE(), GETDATE()),
        ('Analyst', 'Certificate management access', 1, 1, GETDATE(), GETDATE()),
        ('External', 'External user access', 1, 1, GETDATE(), GETDATE()),
        ('Public', 'Public read-only access', 1, 1, GETDATE(), GETDATE());

    PRINT '✓ 4 Roles created';
END
ELSE
    PRINT '○ Roles already exist, skipping';
PRINT '';

-- ============================================
-- 5. Permissions (Required for app)
-- ============================================
PRINT '5. Seeding Permissions...';

IF NOT EXISTS (SELECT 1 FROM Permissions)
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES
        ('View Certificates', 'certificate_view', 'Certificate', 'Can view certificates', 1, GETDATE()),
        ('Create Certificates', 'certificate_create', 'Certificate', 'Can create new certificates', 1, GETDATE()),
        ('Edit Certificates', 'certificate_edit', 'Certificate', 'Can edit existing certificates', 1, GETDATE()),
        ('Delete Certificates', 'certificate_delete', 'Certificate', 'Can delete certificates', 1, GETDATE()),
        ('Manage Users', 'user_manage', 'User', 'Can manage user accounts', 1, GETDATE()),
        ('View Reports', 'report_view', 'Report', 'Can view system reports', 1, GETDATE());

    PRINT '✓ 6 Permissions created';
END
ELSE
    PRINT '○ Permissions already exist, skipping';
PRINT '';

-- ============================================
-- 6. Role-Permission Assignments
-- ============================================
PRINT '6. Assigning permissions to roles...';

IF NOT EXISTS (SELECT 1 FROM PermissionRole)
BEGIN
    DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin');
    DECLARE @AnalystRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Analyst');
    DECLARE @ExternalRoleId INT = (SELECT Id FROM Roles WHERE Name = 'External');
    DECLARE @PublicRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Public');

    -- Admin gets all permissions
    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @AdminRoleId, Id FROM Permissions;

    -- Analyst gets certificate management (except delete)
    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @AnalystRoleId, Id
    FROM Permissions
    WHERE Codename IN ('certificate_view', 'certificate_create', 'certificate_edit', 'report_view');

    -- External gets view and reports
    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @ExternalRoleId, Id
    FROM Permissions
    WHERE Codename IN ('certificate_view', 'report_view');

    -- Public gets only view
    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @PublicRoleId, Id
    FROM Permissions
    WHERE Codename = 'certificate_view';

    PRINT '✓ Permissions assigned to all roles';
END
ELSE
    PRINT '○ Role-Permission assignments already exist, skipping';
PRINT '';

-- ============================================
-- 7. Product Types (Basic structure)
-- ============================================
PRINT '7. Seeding basic Product Types...';

IF NOT EXISTS (SELECT 1 FROM ProductTypes)
BEGIN
    DECLARE @BuildingMaterialsId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Building Materials');
    DECLARE @ElectronicsId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Electronics / Electrical');
    DECLARE @TextilesId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Textiles');
    DECLARE @ConsumerProductsId INT = (SELECT Id FROM ProductCategories WHERE Name = 'Consumer Products');

    INSERT INTO ProductTypes (Name, CategoryId, Level, SortOrder, IsActive)
    VALUES
        ('Insulation', @BuildingMaterialsId, 1, 1, 1),
        ('Flooring', @BuildingMaterialsId, 1, 2, 1),
        ('Lighting', @ElectronicsId, 1, 1, 1),
        ('Carpeting', @TextilesId, 1, 1, 1),
        ('Furniture', @ConsumerProductsId, 1, 1, 1);

    PRINT '✓ 5 basic Product Types created';
END
ELSE
    PRINT '○ Product Types already exist, skipping';
PRINT '';

-- ============================================
-- 8. Commit Transaction
-- ============================================
PRINT 'Committing changes...';
COMMIT TRANSACTION;
PRINT '✓ Transaction committed successfully';
PRINT '';

-- ============================================
-- 9. Verification
-- ============================================
PRINT '============================================';
PRINT 'VERIFICATION';
PRINT '============================================';

SELECT 'ProductCategories' AS EntityType, COUNT(*) AS Count FROM ProductCategories
UNION ALL
SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL
SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL
SELECT 'Roles', COUNT(*) FROM Roles
UNION ALL
SELECT 'Permissions', COUNT(*) FROM Permissions
UNION ALL
SELECT 'Role-Permission Assignments', COUNT(*) FROM PermissionRole
UNION ALL
SELECT 'ProductTypes', COUNT(*) FROM ProductTypes;

PRINT '';
PRINT '============================================';
PRINT 'ESSENTIAL DATA SEEDING COMPLETE';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Create your production admin user via application UI';
PRINT '2. Start adding companies, products, and certificates';
PRINT '3. The application is now ready for production use';
PRINT '============================================';

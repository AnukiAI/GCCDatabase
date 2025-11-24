-- =========================================
-- GCC PRODUCTION QUICK FIX - Minimal Data
-- =========================================
-- Purpose: Get production website working ASAP with minimal sample data
-- This imports just enough data to display certificates on the website
--
-- Contains:
--   - 4 Roles
--   - 1 Admin User
--   - 3 Partners
--   - 5 Companies
--   - 5 CertificationTypes (most common ones)
--   - 5 Products
--   - 10 Sample Certificates
--
-- Run time: ~10 seconds
-- =========================================

USE gccwebap_certificates;
GO

PRINT '========================================='
PRINT 'GCC PRODUCTION QUICK FIX - Starting...'
PRINT '========================================='
GO

-- Disable constraints temporarily
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';
GO


-- =========================================
-- 1. ROLES
-- =========================================
PRINT 'Importing Roles...';

-- Clear existing roles if any
DELETE FROM RoleUser;
DELETE FROM PermissionRole;
DELETE FROM Roles;

SET IDENTITY_INSERT [Roles] ON;

INSERT INTO [Roles] ([Id], [Name], [Description], [IsSystem], [IsActive], [CreatedAt], [UpdatedAt])
VALUES
(1, N'Admin', N'Full system access', 1, 1, GETDATE(), GETDATE()),
(2, N'Analyst', N'Certificate management access', 1, 1, GETDATE(), GETDATE()),
(3, N'External', N'External user access', 1, 1, GETDATE(), GETDATE()),
(4, N'Public', N'Public read-only access', 1, 1, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Roles] OFF;
PRINT '✓ Roles: 4 rows';
GO


-- =========================================
-- 2. USERS
-- =========================================
PRINT 'Importing Users...';

-- Clear existing users if any
DELETE FROM RoleUser;
DELETE FROM CompanyUser;
DELETE FROM PartnerUser;
DELETE FROM Users;

SET IDENTITY_INSERT [Users] ON;

-- Admin user (password: Admin123! - BCrypt hashed)
INSERT INTO [Users] ([Id], [Username], [Email], [Name], [FullName], [PasswordHash], [Status], [IsActive], [IsStaff], [IsSuperuser],
                      [RequirePasswordReset], [TwoFactorEnabled], [TwoFactorMethod], [IsPhoneVerified], [IsEmailVerified],
                      [MustChangePassword], [RequirePasswordChange], [MaxConcurrentSessions], [MaxPasswordHistory],
                      [FailedLoginAttempts], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(1, N'admin@greencirclecertified.com', N'admin@greencirclecertified.com', N'System Admin', N'System Administrator',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', -- Admin123!
 N'active', 1, 1, 1, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Users] OFF;

-- Assign Admin role to admin user
INSERT INTO RoleUser (RolesId, UsersId) VALUES (1, 1);

PRINT '✓ Users: 1 row (admin)';
GO


-- =========================================
-- 3. PARTNERS
-- =========================================
PRINT 'Importing Partners...';

-- Clear existing partners if any
DELETE FROM PartnerUser;
DELETE FROM Partners;

SET IDENTITY_INSERT [Partners] ON;

INSERT INTO [Partners] ([Id], [Name], [Description], [Website], [ContactInfo], [Email], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
VALUES
(1, N'Amazon', N'Amazon marketplace partnership', N'https://www.amazon.com', NULL, NULL, 1, 0, 1, GETDATE(), GETDATE()),
(2, N'Wayfair', N'Wayfair marketplace partnership', N'https://www.wayfair.com', NULL, NULL, 1, 0, 2, GETDATE(), GETDATE()),
(3, N'Other', N'Other partnerships', NULL, NULL, NULL, 1, 0, 3, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Partners] OFF;
PRINT '✓ Partners: 3 rows';
GO


-- =========================================
-- 4. COMPANIES
-- =========================================
PRINT 'Importing Companies...';

-- Clear existing companies if any
DELETE FROM CompanyUser;
DELETE FROM Certificates;
DELETE FROM Products;
DELETE FROM Companies;

SET IDENTITY_INSERT [Companies] ON;

INSERT INTO [Companies] ([Id], [Name], [ContactInfo], [Email], [Website], [Location], [Country], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(1, N'ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division', N'sustainability@assaabloy.com', NULL, NULL, N'Berlin, CT', N'United States', 1, 0, GETDATE(), GETDATE()),
(2, N'Shaw Industries Group', N'green@shawinc.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(3, N'Mohawk Industries', N'sustainability@mohawkind.com', NULL, NULL, N'Calhoun, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(4, N'3M Company', N'sustainability@3m.com', NULL, NULL, N'St. Paul, MN', N'United States', 1, 0, GETDATE(), GETDATE()),
(5, N'Interface, Inc.', N'sustainability@interface.com', NULL, NULL, N'Atlanta, GA', N'United States', 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Companies] OFF;
PRINT '✓ Companies: 5 rows';
GO


-- =========================================
-- 5. PRODUCT CATEGORIES (Required for Products)
-- =========================================
PRINT 'Importing Product Categories...';

DELETE FROM Products;
DELETE FROM ProductTypes;
DELETE FROM ProductCategories;

SET IDENTITY_INSERT [ProductCategories] ON;

INSERT INTO [ProductCategories] ([Id], [Name], [Description], [SortOrder], [IsActive])
VALUES
(1, N'Building Materials', N'Construction and building materials', 1, 1),
(2, N'Electronics', N'Electronic devices and components', 2, 1),
(3, N'Furniture', N'Office and commercial furniture', 3, 1),
(4, N'Textiles', N'Carpets, fabrics, and floor coverings', 4, 1),
(5, N'Packaging', N'Sustainable packaging materials', 5, 1);

SET IDENTITY_INSERT [ProductCategories] OFF;
PRINT '✓ Product Categories: 5 rows';
GO


-- =========================================
-- 6. CERTIFICATION TYPES (Most Common)
-- =========================================
PRINT 'Importing Certification Types...';

DELETE FROM Certificates;
DELETE FROM CustomFieldValues;
DELETE FROM CustomFields;
DELETE FROM CertificationTypes;

SET IDENTITY_INSERT [CertificationTypes] ON;

INSERT INTO [CertificationTypes] ([Id], [Name], [AmazonCode], [Description], [IsActive], [SortOrder], [Level], [ParentId])
VALUES
(1, N'Biobased', N'BB', N'Products derived from renewable biological resources', 1, 1, 1, NULL),
(2, N'Biodegradable', N'BD', N'Products that break down naturally in the environment', 1, 2, 1, NULL),
(3, N'Recycled Content', N'RC', N'Products containing recycled materials', 1, 3, 1, NULL),
(4, N'Certified Sustainability Facts', N'CSF', N'Comprehensive sustainability certification', 1, 4, 1, NULL),
(5, N'Waste Diversion from Landfill', N'WDL', N'Waste reduction and diversion practices', 1, 5, 1, NULL);

SET IDENTITY_INSERT [CertificationTypes] OFF;
PRINT '✓ Certification Types: 5 rows';
GO


-- =========================================
-- 7. PRODUCTS
-- =========================================
PRINT 'Importing Products...';

DELETE FROM CertificateProductIds;
DELETE FROM Certificates;
DELETE FROM Products;

SET IDENTITY_INSERT [Products] ON;

INSERT INTO [Products] ([Id], [Name], [Description], [Model], [Sku], [Upc], [Asin], [CategoryId], [ProductTypeId], [ProductSubcategoryId], [CompanyId], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(1, N'900ET Exit Device Trim with EcoFlex', N'Exit device trim with sustainable EchoFlex technology', NULL, NULL, NULL, NULL, 1, NULL, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(2, N'EcoWorx Carpet Tile', N'Sustainable commercial carpet tile', NULL, NULL, NULL, NULL, 4, NULL, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(3, N'Airo Carpet Tile with Recycled Content', N'Commercial carpet with post-consumer recycled content', NULL, NULL, NULL, NULL, 4, NULL, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(4, N'VHB Structural Glazing Tape', N'High-performance building adhesive', NULL, NULL, NULL, NULL, 1, NULL, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(5, N'Carbon Negative Modular Carpet', N'Net-zero carbon impact flooring', NULL, NULL, NULL, NULL, 4, NULL, NULL, 5, 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Products] OFF;
PRINT '✓ Products: 5 rows';
GO


-- =========================================
-- 8. CERTIFICATES (Sample Data)
-- =========================================
PRINT 'Importing Certificates...';

DELETE FROM CertificateTag;
DELETE FROM CertificateProductIds;
DELETE FROM CustomFieldValues;
DELETE FROM CertificateDocuments;
DELETE FROM Certificates;

SET IDENTITY_INSERT [Certificates] ON;

INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country], [Asin], [Upc], [Gtin], [Ean], [CertificateFile], [CertificateImagePath], [Partner], [CompanyId], [ProductId], [CertificationTypeId], [CreatedBy], [UpdatedBy], [CreatedAt], [UpdatedAt])
VALUES
(1, N'24-1605', N'active', '2024-07-01', '2027-06-30', N'Berlin, CT', N'United States', N'B08X7N9MRQ', N'846378985421', N'00846378985421', N'0846378985421', NULL, NULL, N'None', 1, 1, 1, NULL, NULL, GETDATE(), GETDATE()),
(2, N'24-5712', N'active', '2024-11-25', '2027-11-24', N'Dalton, GA', N'United States', NULL, N'456789012345', NULL, NULL, NULL, NULL, N'None', 2, 2, 3, NULL, NULL, GETDATE(), GETDATE()),
(3, N'24-6201', N'active', '2024-12-01', '2027-11-30', N'Calhoun, GA', N'United States', N'B10ABC1234', N'456789012346', NULL, NULL, NULL, NULL, N'None', 3, 3, 4, NULL, NULL, GETDATE(), GETDATE()),
(4, N'24-2108', N'active', '2024-10-01', '2027-09-30', N'St. Paul, MN', N'United States', N'B09DEF1234', N'345678901234', NULL, NULL, NULL, NULL, N'None', 4, 4, 2, NULL, NULL, GETDATE(), GETDATE()),
(5, N'24-8134', N'active', '2024-09-15', '2027-09-14', N'Atlanta, GA', N'United States', NULL, N'789012345679', NULL, NULL, NULL, NULL, N'None', 5, 5, 5, NULL, NULL, GETDATE(), GETDATE()),
(6, N'25-0001', N'active', '2025-01-01', '2028-12-31', N'Berlin, CT', N'United States', N'B25CER0001', N'125478963014', N'01254789630142', N'1254789630142', NULL, NULL, N'None', 1, 1, 1, NULL, NULL, GETDATE(), GETDATE()),
(7, N'24-1607', N'active', '2024-08-01', '2027-07-31', N'Dalton, GA', N'United States', NULL, NULL, NULL, NULL, NULL, NULL, N'None', 2, 2, 2, NULL, NULL, GETDATE(), GETDATE()),
(8, N'24-1608', N'draft', '2025-01-01', '2028-12-31', N'Calhoun, GA', N'United States', NULL, NULL, NULL, NULL, NULL, NULL, N'None', 3, 3, 1, NULL, NULL, GETDATE(), GETDATE()),
(9, N'24-1609', N'expired', '2021-01-01', '2024-01-01', N'St. Paul, MN', N'United States', NULL, NULL, NULL, NULL, NULL, NULL, N'None', 4, 4, 3, NULL, NULL, GETDATE(), GETDATE()),
(10, N'25-0002', N'active', '2024-08-15', '2027-08-14', N'Atlanta, GA', N'United States', N'B25JOH0002', N'147852369025', NULL, NULL, NULL, NULL, N'None', 5, 5, 4, NULL, NULL, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Certificates] OFF;
PRINT '✓ Certificates: 10 rows';
GO


-- =========================================
-- Re-enable constraints
-- =========================================
PRINT 'Re-enabling database constraints...';
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all';
GO


-- =========================================
-- VERIFICATION
-- =========================================
PRINT ''
PRINT '========================================='
PRINT 'QUICK FIX COMPLETE - Verification:'
PRINT '========================================='

SELECT 'Roles' as TableName, COUNT(*) as RowCount FROM Roles
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL
SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL
SELECT 'ProductCategories', COUNT(*) FROM ProductCategories
UNION ALL
SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Certificates', COUNT(*) FROM Certificates
ORDER BY TableName;

PRINT ''
PRINT '========================================='
PRINT 'SUCCESS! Production database now has:'
PRINT '  - 4 Roles'
PRINT '  - 1 Admin user (admin@greencirclecertified.com / Admin123!)'
PRINT '  - 3 Partners'
PRINT '  - 5 Companies'
PRINT '  - 5 Product Categories'
PRINT '  - 5 Certification Types'
PRINT '  - 5 Products'
PRINT '  - 10 Sample Certificates'
PRINT ''
PRINT 'Next steps:'
PRINT '  1. Visit your production website'
PRINT '  2. Navigate to /Certificate/CertificateTileView'
PRINT '  3. You should see 10 certificates displayed'
PRINT '  4. Login with admin@greencirclecertified.com / Admin123!'
PRINT ''
PRINT 'To import FULL dataset (178 certificates):'
PRINT '  Run the migrate-to-production.ps1 script'
PRINT '========================================='
GO

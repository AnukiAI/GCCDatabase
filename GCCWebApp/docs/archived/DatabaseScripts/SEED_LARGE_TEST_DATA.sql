-- =========================================
-- GCC PRODUCTION - Large Test Dataset
-- =========================================
-- Purpose: Load comprehensive test data for staging/testing environment
--
-- Contains:
--   - 4 Roles
--   - 15 Users (Admin, Analyst, External users)
--   - 3 Partners
--   - 50 Companies
--   - 10 Product Categories
--   - 15 Product Types
--   - 10 Certification Types
--   - 150 Products
--   - 250 Certificates
--   - 30 Tags
--
-- Run time: ~60 seconds
-- =========================================

USE gccwebap_certificates;
GO

PRINT '========================================='
PRINT 'GCC LARGE TEST DATASET - Starting...'
PRINT '========================================='
GO

-- Disable constraints temporarily
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';
GO


-- =========================================
-- 1. CLEAR EXISTING DATA (in dependency order)
-- =========================================
PRINT 'Clearing existing data...';

DELETE FROM CertificateTag;
DELETE FROM CertificateProductIds;
DELETE FROM CustomFieldValues;
DELETE FROM CertificateDocuments;
DELETE FROM Certificates;
DELETE FROM Products;
DELETE FROM CompanyUser;
DELETE FROM PartnerUser;
DELETE FROM RoleUser;
DELETE FROM PermissionRole;
DELETE FROM Users;
DELETE FROM Companies;
DELETE FROM Partners;
DELETE FROM ProductTypes;
DELETE FROM ProductCategories;
DELETE FROM CustomFields;
DELETE FROM CertificationTypes;
DELETE FROM Tags;
DELETE FROM Roles;
GO


-- =========================================
-- 2. ROLES
-- =========================================
PRINT 'Importing Roles...';

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
-- 3. USERS
-- =========================================
PRINT 'Importing Users...';

SET IDENTITY_INSERT [Users] ON;

-- Password for all test users: Admin123! (BCrypt hashed)
INSERT INTO [Users] ([Id], [Username], [Email], [Name], [FullName], [PasswordHash], [Status], [IsActive], [IsStaff], [IsSuperuser],
                      [RequirePasswordReset], [TwoFactorEnabled], [TwoFactorMethod], [IsPhoneVerified], [IsEmailVerified],
                      [MustChangePassword], [RequirePasswordChange], [MaxConcurrentSessions], [MaxPasswordHistory],
                      [FailedLoginAttempts], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
-- Admin Users
(1, N'admin@greencirclecertified.com', N'admin@greencirclecertified.com', N'Admin', N'System Administrator',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 1, 1, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(2, N'robert.admin@greencirclecertified.com', N'robert.admin@greencirclecertified.com', N'Robert', N'Robert Admin',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 1, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),

-- Analyst Users
(3, N'analyst@greencirclecertified.com', N'analyst@greencirclecertified.com', N'Jane', N'Jane Analyst',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 1, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(4, N'sarah.analyst@greencirclecertified.com', N'sarah.analyst@greencirclecertified.com', N'Sarah', N'Sarah Johnson',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 1, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(5, N'mike.analyst@greencirclecertified.com', N'mike.analyst@greencirclecertified.com', N'Mike', N'Mike Chen',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 1, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),

-- External Users
(6, N'partner@amazon.com', N'partner@amazon.com', N'Amazon', N'Amazon Partner',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(7, N'lisa.rodriguez@assaabloy.com', N'lisa.rodriguez@assaabloy.com', N'Lisa', N'Lisa Rodriguez',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(8, N'david.kim@3m.com', N'david.kim@3m.com', N'David', N'David Kim',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(9, N'emma.wilson@interface.com', N'emma.wilson@interface.com', N'Emma', N'Emma Wilson',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(10, N'alex.martinez@steelcase.com', N'alex.martinez@steelcase.com', N'Alex', N'Alex Martinez',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(11, N'jennifer.lee@mohawkind.com', N'jennifer.lee@mohawkind.com', N'Jennifer', N'Jennifer Lee',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(12, N'maria.garcia@shawinc.com', N'maria.garcia@shawinc.com', N'Maria', N'Maria Garcia',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),

-- Public Users
(13, N'public@example.com', N'public@example.com', N'Public', N'Public User',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(14, N'test@greencirclecertified.com', N'test@greencirclecertified.com', N'Test', N'Test User',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE()),
(15, N'demo@greencirclecertified.com', N'demo@greencirclecertified.com', N'Demo', N'Demo User',
 N'$2a$11$6EDBGkE3X5vGJJ0tF3h8ZeqLKJpSHXZWWd7oN5dZvwvW5dLq5xVc6', N'active', 1, 0, 0, 0, 0, N'None', 0, 1, 0, 0, 5, 10, 0, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Users] OFF;

-- Assign roles to users
INSERT INTO RoleUser (RolesId, UsersId) VALUES
(1, 1), (1, 2),  -- Admin role
(2, 3), (2, 4), (2, 5),  -- Analyst role
(3, 6), (3, 7), (3, 8), (3, 9), (3, 10), (3, 11), (3, 12),  -- External role
(4, 13), (4, 14), (4, 15);  -- Public role

PRINT '✓ Users: 15 rows';
GO


-- =========================================
-- 4. PARTNERS
-- =========================================
PRINT 'Importing Partners...';

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
-- 5. PRODUCT CATEGORIES
-- =========================================
PRINT 'Importing Product Categories...';

SET IDENTITY_INSERT [ProductCategories] ON;

INSERT INTO [ProductCategories] ([Id], [Name], [Description], [SortOrder], [IsActive])
VALUES
(1, N'Building Materials', N'Construction and building materials', 1, 1),
(2, N'Electronics', N'Electronic devices and components', 2, 1),
(3, N'Furniture', N'Office and commercial furniture', 3, 1),
(4, N'Textiles', N'Carpets, fabrics, and floor coverings', 4, 1),
(5, N'Packaging', N'Sustainable packaging materials', 5, 1),
(6, N'Adhesives', N'Tapes and adhesive products', 6, 1),
(7, N'Hardware', N'Door hardware and security products', 7, 1),
(8, N'Lighting', N'Commercial and industrial lighting', 8, 1),
(9, N'Insulation', N'Thermal and acoustic insulation', 9, 1),
(10, N'Flooring', N'Hard surface and resilient flooring', 10, 1);

SET IDENTITY_INSERT [ProductCategories] OFF;
PRINT '✓ Product Categories: 10 rows';
GO


-- =========================================
-- 6. PRODUCT TYPES
-- =========================================
PRINT 'Importing Product Types...';

SET IDENTITY_INSERT [ProductTypes] ON;

INSERT INTO [ProductTypes] ([Id], [Name], [Description], [CategoryId], [SortOrder], [IsActive])
VALUES
(1, N'Exit Device', N'Emergency exit door hardware', 7, 1, 1),
(2, N'Carpet Tile', N'Modular carpet flooring', 4, 2, 1),
(3, N'Adhesive Tape', N'Commercial adhesive products', 6, 3, 1),
(4, N'Office Chair', N'Seating products', 3, 4, 1),
(5, N'Desk', N'Office desks and workstations', 3, 5, 1),
(6, N'LED Fixture', N'Energy-efficient lighting', 8, 6, 1),
(7, N'Insulation Panel', N'Building insulation products', 9, 7, 1),
(8, N'Vinyl Flooring', N'Resilient flooring products', 10, 8, 1),
(9, N'Packaging Box', N'Corrugated packaging', 5, 9, 1),
(10, N'Door Lock', N'Security hardware', 7, 10, 1),
(11, N'Window Film', N'Energy-saving window treatments', 1, 11, 1),
(12, N'Ceiling Tile', N'Acoustic ceiling systems', 1, 12, 1),
(13, N'Paint', N'Low-VOC coatings', 1, 13, 1),
(14, N'Broadloom Carpet', N'Wall-to-wall carpeting', 4, 14, 1),
(15, N'Filing Cabinet', N'Storage furniture', 3, 15, 1);

SET IDENTITY_INSERT [ProductTypes] OFF;
PRINT '✓ Product Types: 15 rows';
GO


-- =========================================
-- 7. CERTIFICATION TYPES
-- =========================================
PRINT 'Importing Certification Types...';

SET IDENTITY_INSERT [CertificationTypes] ON;

INSERT INTO [CertificationTypes] ([Id], [Name], [AmazonCode], [Description], [IsActive], [SortOrder], [Level], [ParentId])
VALUES
(1, N'Biobased', N'BB', N'Products derived from renewable biological resources', 1, 1, 1, NULL),
(2, N'Biodegradable', N'BD', N'Products that break down naturally in the environment', 1, 2, 1, NULL),
(3, N'Recycled Content', N'RC', N'Products containing recycled materials', 1, 3, 1, NULL),
(4, N'Certified Sustainability Facts', N'CSF', N'Comprehensive sustainability certification', 1, 4, 1, NULL),
(5, N'Waste Diversion from Landfill', N'WDL', N'Waste reduction and diversion practices', 1, 5, 1, NULL),
(6, N'Carbon Neutral', N'CN', N'Net-zero carbon footprint products', 1, 6, 1, NULL),
(7, N'Energy Efficient', N'EE', N'Reduced energy consumption', 1, 7, 1, NULL),
(8, N'Water Conservation', N'WC', N'Water-saving products and processes', 1, 8, 1, NULL),
(9, N'FSC Certified', N'FSC', N'Forest Stewardship Council certified', 1, 9, 1, NULL),
(10, N'GREENGUARD Gold', N'GG', N'Low chemical emissions certification', 1, 10, 1, NULL);

SET IDENTITY_INSERT [CertificationTypes] OFF;
PRINT '✓ Certification Types: 10 rows';
GO


-- =========================================
-- 8. TAGS
-- =========================================
PRINT 'Importing Tags...';

SET IDENTITY_INSERT [Tags] ON;

INSERT INTO [Tags] ([Id], [Name], [Slug], [IsInternal], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(1, N'High Priority', N'high-priority', 1, 1, 0, GETDATE(), GETDATE()),
(2, N'Urgent Review', N'urgent-review', 1, 1, 0, GETDATE(), GETDATE()),
(3, N'Amazon Featured', N'amazon-featured', 0, 1, 0, GETDATE(), GETDATE()),
(4, N'New Product', N'new-product', 0, 1, 0, GETDATE(), GETDATE()),
(5, N'Best Seller', N'best-seller', 0, 1, 0, GETDATE(), GETDATE()),
(6, N'Eco-Friendly', N'eco-friendly', 0, 1, 0, GETDATE(), GETDATE()),
(7, N'Pending Approval', N'pending-approval', 1, 1, 0, GETDATE(), GETDATE()),
(8, N'Requires Documentation', N'requires-documentation', 1, 1, 0, GETDATE(), GETDATE()),
(9, N'Expiring Soon', N'expiring-soon', 1, 1, 0, GETDATE(), GETDATE()),
(10, N'Renewed', N'renewed', 0, 1, 0, GETDATE(), GETDATE()),
(11, N'Top Rated', N'top-rated', 0, 1, 0, GETDATE(), GETDATE()),
(12, N'Verified Sustainable', N'verified-sustainable', 0, 1, 0, GETDATE(), GETDATE()),
(13, N'Climate Positive', N'climate-positive', 0, 1, 0, GETDATE(), GETDATE()),
(14, N'Made in USA', N'made-in-usa', 0, 1, 0, GETDATE(), GETDATE()),
(15, N'Award Winner', N'award-winner', 0, 1, 0, GETDATE(), GETDATE()),
(16, N'Limited Edition', N'limited-edition', 0, 1, 0, GETDATE(), GETDATE()),
(17, N'Clearance', N'clearance', 0, 1, 0, GETDATE(), GETDATE()),
(18, N'Pre-Order', N'pre-order', 0, 1, 0, GETDATE(), GETDATE()),
(19, N'Seasonal', N'seasonal', 0, 1, 0, GETDATE(), GETDATE()),
(20, N'Commercial Grade', N'commercial-grade', 0, 1, 0, GETDATE(), GETDATE()),
(21, N'Residential', N'residential', 0, 1, 0, GETDATE(), GETDATE()),
(22, N'Industrial', N'industrial', 0, 1, 0, GETDATE(), GETDATE()),
(23, N'Healthcare Grade', N'healthcare-grade', 0, 1, 0, GETDATE(), GETDATE()),
(24, N'Education Sector', N'education-sector', 0, 1, 0, GETDATE(), GETDATE()),
(25, N'Hospitality Grade', N'hospitality-grade', 0, 1, 0, GETDATE(), GETDATE()),
(26, N'LEED Compliant', N'leed-compliant', 0, 1, 0, GETDATE(), GETDATE()),
(27, N'Zero Waste', N'zero-waste', 0, 1, 0, GETDATE(), GETDATE()),
(28, N'Circular Economy', N'circular-economy', 0, 1, 0, GETDATE(), GETDATE()),
(29, N'B-Corp Certified', N'b-corp-certified', 0, 1, 0, GETDATE(), GETDATE()),
(30, N'Fair Trade', N'fair-trade', 0, 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Tags] OFF;
PRINT '✓ Tags: 30 rows';
GO


-- =========================================
-- 9. COMPANIES (50 companies)
-- =========================================
PRINT 'Importing Companies...';

SET IDENTITY_INSERT [Companies] ON;

INSERT INTO [Companies] ([Id], [Name], [ContactInfo], [Email], [Website], [Location], [Country], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
-- Original 5 companies
(1, N'ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division', N'sustainability@assaabloy.com', NULL, NULL, N'Berlin, CT', N'United States', 1, 0, GETDATE(), GETDATE()),
(2, N'Shaw Industries Group', N'green@shawinc.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(3, N'Mohawk Industries', N'sustainability@mohawkind.com', NULL, NULL, N'Calhoun, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(4, N'3M Company', N'sustainability@3m.com', NULL, NULL, N'St. Paul, MN', N'United States', 1, 0, GETDATE(), GETDATE()),
(5, N'Interface, Inc.', N'sustainability@interface.com', NULL, NULL, N'Atlanta, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
-- Additional 45 companies
(6, N'Steelcase Inc.', N'green@steelcase.com', NULL, NULL, N'Grand Rapids, MI', N'United States', 1, 0, GETDATE(), GETDATE()),
(7, N'Herman Miller, Inc.', N'sustainability@hermanmiller.com', NULL, NULL, N'Zeeland, MI', N'United States', 1, 0, GETDATE(), GETDATE()),
(8, N'Knoll, Inc.', N'environmental@knoll.com', NULL, NULL, N'East Greenville, PA', N'United States', 1, 0, GETDATE(), GETDATE()),
(9, N'Haworth, Inc.', N'green@haworth.com', NULL, NULL, N'Holland, MI', N'United States', 1, 0, GETDATE(), GETDATE()),
(10, N'Armstrong World Industries', N'sustainability@armstrongceilings.com', NULL, NULL, N'Lancaster, PA', N'United States', 1, 0, GETDATE(), GETDATE()),
(11, N'Tarkett USA', N'green@tarkett.com', NULL, NULL, N'Solon, OH', N'United States', 1, 0, GETDATE(), GETDATE()),
(12, N'Milliken & Company', N'sustainability@milliken.com', NULL, NULL, N'Spartanburg, SC', N'United States', 1, 0, GETDATE(), GETDATE()),
(13, N'Teknion Corporation', N'environmental@teknion.com', NULL, NULL, N'Toronto, ON', N'Canada', 1, 0, GETDATE(), GETDATE()),
(14, N'KI (Krueger International)', N'green@ki.com', NULL, NULL, N'Green Bay, WI', N'United States', 1, 0, GETDATE(), GETDATE()),
(15, N'Tandus Centiva', N'sustainability@tarkett.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(16, N'Allegion plc', N'green@allegion.com', NULL, NULL, N'Dublin', N'Ireland', 1, 0, GETDATE(), GETDATE()),
(17, N'Dormakaba', N'sustainability@dormakaba.com', NULL, NULL, N'Zurich', N'Switzerland', 1, 0, GETDATE(), GETDATE()),
(18, N'Stanley Black & Decker', N'environmental@stanleyblackanddecker.com', NULL, NULL, N'New Britain, CT', N'United States', 1, 0, GETDATE(), GETDATE()),
(19, N'Owens Corning', N'green@owenscorning.com', NULL, NULL, N'Toledo, OH', N'United States', 1, 0, GETDATE(), GETDATE()),
(20, N'Johns Manville', N'sustainability@jm.com', NULL, NULL, N'Denver, CO', N'United States', 1, 0, GETDATE(), GETDATE()),
(21, N'CertainTeed Corporation', N'green@saint-gobain.com', NULL, NULL, N'Malvern, PA', N'United States', 1, 0, GETDATE(), GETDATE()),
(22, N'USG Corporation', N'sustainability@usg.com', NULL, NULL, N'Chicago, IL', N'United States', 1, 0, GETDATE(), GETDATE()),
(23, N'Forbo Flooring Systems', N'environmental@forbo.com', NULL, NULL, N'Hazleton, PA', N'United States', 1, 0, GETDATE(), GETDATE()),
(24, N'Gerflor USA', N'green@gerflor.com', NULL, NULL, N'Chester, PA', N'United States', 1, 0, GETDATE(), GETDATE()),
(25, N'Congoleum Corporation', N'sustainability@congoleum.com', NULL, NULL, N'Mercerville, NJ', N'United States', 1, 0, GETDATE(), GETDATE()),
(26, N'Bentley Mills', N'environmental@bentleymills.com', NULL, NULL, N'City of Industry, CA', N'United States', 1, 0, GETDATE(), GETDATE()),
(27, N'J+J Flooring Group', N'green@jjflooringgroup.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(28, N'Beaulieu International Group', N'sustainability@bintg.com', NULL, NULL, N'Waregem', N'Belgium', 1, 0, GETDATE(), GETDATE()),
(29, N'Engineered Floors', N'environmental@engineeredfloors.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(30, N'Patcraft Commercial Flooring', N'green@patcraft.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(31, N'Lees Commercial Carpets', N'sustainability@leescarpets.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(32, N'Designweave', N'environmental@designweave.com', NULL, NULL, N'Philadelphia, PA', N'United States', 1, 0, GETDATE(), GETDATE()),
(33, N'Mannington Commercial', N'green@mannington.com', NULL, NULL, N'Salem, NJ', N'United States', 1, 0, GETDATE(), GETDATE()),
(34, N'Mohawk Group', N'sustainability@mohawkgroup.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(35, N'Texstyle', N'environmental@texstyle.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(36, N'Phenix Flooring', N'green@phenixflooring.com', NULL, NULL, N'Dalton, GA', N'United States', 1, 0, GETDATE(), GETDATE()),
(37, N'Allsteel', N'sustainability@allsteeloffice.com', NULL, NULL, N'Muscatine, IA', N'United States', 1, 0, GETDATE(), GETDATE()),
(38, N'Davis Furniture Industries', N'environmental@davisfurniture.com', NULL, NULL, N'High Point, NC', N'United States', 1, 0, GETDATE(), GETDATE()),
(39, N'Kimball International', N'green@kimball.com', NULL, NULL, N'Jasper, IN', N'United States', 1, 0, GETDATE(), GETDATE()),
(40, N'HON Company', N'sustainability@hon.com', NULL, NULL, N'Muscatine, IA', N'United States', 1, 0, GETDATE(), GETDATE()),
(41, N'Global Furniture Group', N'environmental@globalfurnituregroup.com', NULL, NULL, N'Toronto, ON', N'Canada', 1, 0, GETDATE(), GETDATE()),
(42, N'Allseating Corporation', N'green@allseating.com', NULL, NULL, N'Cambridge, ON', N'Canada', 1, 0, GETDATE(), GETDATE()),
(43, N'Keilhauer', N'sustainability@keilhauer.com', NULL, NULL, N'Toronto, ON', N'Canada', 1, 0, GETDATE(), GETDATE()),
(44, N'SitOnIt Seating', N'environmental@sitonit.net', NULL, NULL, N'Laguna Niguel, CA', N'United States', 1, 0, GETDATE(), GETDATE()),
(45, N'Humanscale', N'green@humanscale.com', NULL, NULL, N'New York, NY', N'United States', 1, 0, GETDATE(), GETDATE()),
(46, N'Andreu World', N'sustainability@andreuworld.com', NULL, NULL, N'Valencia', N'Spain', 1, 0, GETDATE(), GETDATE()),
(47, N'National Office Furniture', N'environmental@nationalofficefurniture.com', NULL, NULL, N'Jasper, IN', N'United States', 1, 0, GETDATE(), GETDATE()),
(48, N'OFS Brands', N'green@ofsbrands.com', NULL, NULL, N'Huntingburg, IN', N'United States', 1, 0, GETDATE(), GETDATE()),
(49, N'Stylex', N'sustainability@stylex.com', NULL, NULL, N'Thomasville, NC', N'United States', 1, 0, GETDATE(), GETDATE()),
(50, N'Via Seating', N'environmental@viaseating.com', NULL, NULL, N'Sparks, NV', N'United States', 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Companies] OFF;
PRINT '✓ Companies: 50 rows';
GO


-- =========================================
-- 10. PRODUCTS (150 products)
-- =========================================
PRINT 'Importing Products (this may take a moment)...';

SET IDENTITY_INSERT [Products] ON;

-- First 50 products
INSERT INTO [Products] ([Id], [Name], [Description], [Model], [Sku], [Upc], [Asin], [CategoryId], [ProductTypeId], [ProductSubcategoryId], [CompanyId], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(1, N'900ET Exit Device Trim with EcoFlex', N'Exit device trim with sustainable EchoFlex technology', NULL, NULL, NULL, NULL, 7, 1, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(2, N'EcoWorx Carpet Tile', N'Sustainable commercial carpet tile', NULL, NULL, NULL, NULL, 4, 2, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(3, N'Airo Carpet Tile with Recycled Content', N'Commercial carpet with post-consumer recycled content', NULL, NULL, NULL, NULL, 4, 2, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(4, N'VHB Structural Glazing Tape', N'High-performance building adhesive', NULL, NULL, NULL, NULL, 6, 3, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(5, N'Carbon Negative Modular Carpet', N'Net-zero carbon impact flooring', NULL, NULL, NULL, NULL, 4, 2, NULL, 5, 1, 0, GETDATE(), GETDATE()),
(6, N'Think Chair with Recycled Content', N'Ergonomic office chair with 32% recycled materials', N'THINKV2', NULL, NULL, NULL, 3, 4, NULL, 6, 1, 0, GETDATE(), GETDATE()),
(7, N'Aeron Chair Remastered', N'Classic ergonomic seating redesigned for sustainability', N'AE113AWB', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(8, N'ReGeneration Chair', N'First chair designed for circular economy', N'RGN-101', NULL, NULL, NULL, 3, 4, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(9, N'Very Task Chair', N'Simple, sustainable task seating', N'VT-100', NULL, NULL, NULL, 3, 4, NULL, 9, 1, 0, GETDATE(), GETDATE()),
(10, N'Sustain Ceiling Tile', N'Acoustic panels with recycled content', N'SC-24', NULL, NULL, NULL, 1, 12, NULL, 10, 1, 0, GETDATE(), GETDATE()),
(11, N'EcoBase Carpet Tile', N'Modular flooring with bio-based backing', N'EB-500', NULL, NULL, NULL, 4, 2, NULL, 11, 1, 0, GETDATE(), GETDATE()),
(12, N'Infinity Modular System', N'Reconfigurable wall-to-wall carpeting', N'INF-2000', NULL, NULL, NULL, 4, 14, NULL, 12, 1, 0, GETDATE(), GETDATE()),
(13, N'Zones Height-Adjustable Desk', N'Sit-stand desk with sustainable materials', N'Z-HAD-01', NULL, NULL, NULL, 3, 5, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(14, N'Torrent Filing System', N'Recycled steel storage cabinets', N'TFS-3DR', NULL, NULL, NULL, 3, 15, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(15, N'EcoSurfaces Collection', N'PVC-free resilient flooring', N'ESC-100', NULL, NULL, NULL, 10, 8, NULL, 15, 1, 0, GETDATE(), GETDATE()),
(16, N'Von Duprin 99 Series Exit Device', N'Fire-rated panic hardware', N'VD-99', NULL, NULL, NULL, 7, 1, NULL, 16, 1, 0, GETDATE(), GETDATE()),
(17, N'Kaba PHI Precision Hardware', N'Electronic access control', N'PHI-2000', NULL, NULL, NULL, 7, 10, NULL, 17, 1, 0, GETDATE(), GETDATE()),
(18, N'LCN Closers with Hold-Open', N'Energy-saving door closers', N'LCN-4040', NULL, NULL, NULL, 7, 1, NULL, 18, 1, 0, GETDATE(), GETDATE()),
(19, N'PINK Fiberglass Insulation', N'Formaldehyde-free thermal insulation', N'R-13-15', NULL, NULL, NULL, 9, 7, NULL, 19, 1, 0, GETDATE(), GETDATE()),
(20, N'Climate Pro Insulation', N'Bio-based building insulation', N'CP-R19', NULL, NULL, NULL, 9, 7, NULL, 20, 1, 0, GETDATE(), GETDATE()),
(21, N'EcoTouch Insulation', N'Recycled glass fiber insulation', N'ET-R30', NULL, NULL, NULL, 9, 7, NULL, 21, 1, 0, GETDATE(), GETDATE()),
(22, N'Sheetrock Brand Ultralight Panels', N'Lightweight gypsum wallboard', N'UL-1/2', NULL, NULL, NULL, 1, 12, NULL, 22, 1, 0, GETDATE(), GETDATE()),
(23, N'Marmoleum Composition Tile', N'Natural linoleum flooring', N'MCT-3000', NULL, NULL, NULL, 10, 8, NULL, 23, 1, 0, GETDATE(), GETDATE()),
(24, N'Mipolam Vinyl Flooring', N'Phthalate-free resilient flooring', N'MPL-150', NULL, NULL, NULL, 10, 8, NULL, 24, 1, 0, GETDATE(), GETDATE()),
(25, N'DLW Luxury Vinyl Tile', N'Premium LVT with recycled content', N'DLW-LVT', NULL, NULL, NULL, 10, 8, NULL, 25, 1, 0, GETDATE(), GETDATE()),
(26, N'Crossville Porcelain Tile', N'USA-made ceramic tile', N'CV-12X24', NULL, NULL, NULL, 10, NULL, NULL, 26, 1, 0, GETDATE(), GETDATE()),
(27, N'NXT Carpet Tile', N'Solution-dyed nylon carpet', N'NXT-500', NULL, NULL, NULL, 4, 2, NULL, 27, 1, 0, GETDATE(), GETDATE()),
(28, N'Tessera Carpet Planks', N'Linear modular carpet', N'TSR-2000', NULL, NULL, NULL, 4, 2, NULL, 28, 1, 0, GETDATE(), GETDATE()),
(29, N'Pentz Commercial Carpet', N'High-performance carpet tile', N'PNZ-1000', NULL, NULL, NULL, 4, 2, NULL, 29, 1, 0, GETDATE(), GETDATE()),
(30, N'Fuze Modular Carpet', N'Eco-friendly carpet system', N'FZ-2400', NULL, NULL, NULL, 4, 2, NULL, 30, 1, 0, GETDATE(), GETDATE()),
(31, N'Colorstrand Carpet Tile', N'Colorful sustainable carpet', N'CS-1800', NULL, NULL, NULL, 4, 2, NULL, 31, 1, 0, GETDATE(), GETDATE()),
(32, N'Woven Expressions', N'Custom pattern carpet tile', N'WE-3000', NULL, NULL, NULL, 4, 2, NULL, 32, 1, 0, GETDATE(), GETDATE()),
(33, N'Pentz Sidewalk Collection', N'Urban-inspired modular carpet', N'PSW-1200', NULL, NULL, NULL, 4, 2, NULL, 33, 1, 0, GETDATE(), GETDATE()),
(34, N'Aladdin Commercial Carpet', N'Traditional broadloom carpet', N'ALD-2000', NULL, NULL, NULL, 4, 14, NULL, 34, 1, 0, GETDATE(), GETDATE()),
(35, N'Tandus Powerbond', N'Cushion-backed modular carpet', N'PB-1000', NULL, NULL, NULL, 4, 2, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(36, N'EverStrand Carpet', N'100% PET polyester carpet', N'ES-2500', NULL, NULL, NULL, 4, 2, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(37, N'Leap Chair', N'High-performance office chair', N'LP-V2', NULL, NULL, NULL, 3, 4, NULL, 6, 1, 0, GETDATE(), GETDATE()),
(38, N'Series 1 Task Chair', N'Affordable ergonomic seating', N'S1-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(39, N'Newson Taskchair', N'Designer task seating', N'NT-200', NULL, NULL, NULL, 3, 4, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(40, N'Fern Chair', N'Eco-friendly task chair', N'FRN-100', NULL, NULL, NULL, 3, 4, NULL, 9, 1, 0, GETDATE(), GETDATE()),
(41, N'Essex Desk System', N'Modular office desking', N'ESX-2000', NULL, NULL, NULL, 3, 5, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(42, N'Acceler8 Workstation', N'Height-adjustable workspace', N'A8-HAD', NULL, NULL, NULL, 3, 5, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(43, N'Kimball Priority Desk', N'Value office furniture', N'KP-1000', NULL, NULL, NULL, 3, 5, NULL, 39, 1, 0, GETDATE(), GETDATE()),
(44, N'10500 Series Desk', N'Traditional office desk', N'10500-D', NULL, NULL, NULL, 3, 5, NULL, 40, 1, 0, GETDATE(), GETDATE()),
(45, N'Plimode Worktable', N'Collaborative workspace', N'PLI-WT', NULL, NULL, NULL, 3, 5, NULL, 41, 1, 0, GETDATE(), GETDATE()),
(46, N'Nimble Task Chair', N'Lightweight ergonomic seating', N'NIM-100', NULL, NULL, NULL, 3, 4, NULL, 42, 1, 0, GETDATE(), GETDATE()),
(47, N'X Series Task Chair', N'High-back executive seating', N'X-200', NULL, NULL, NULL, 3, 4, NULL, 43, 1, 0, GETDATE(), GETDATE()),
(48, N'Amplify Task Chair', N'Multi-shift task seating', N'AMP-24', NULL, NULL, NULL, 3, 4, NULL, 44, 1, 0, GETDATE(), GETDATE()),
(49, N'Freedom Chair', N'Self-adjusting ergonomic chair', N'FRD-100', NULL, NULL, NULL, 3, 4, NULL, 45, 1, 0, GETDATE(), GETDATE()),
(50, N'Flex Executive Chair', N'Premium executive seating', N'FLX-EXC', NULL, NULL, NULL, 3, 4, NULL, 46, 1, 0, GETDATE(), GETDATE());

-- Next 50 products (51-100)
INSERT INTO [Products] ([Id], [Name], [Description], [Model], [Sku], [Upc], [Asin], [CategoryId], [ProductTypeId], [ProductSubcategoryId], [CompanyId], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(51, N'Trace Guest Chair', N'Stackable side chair', N'TRC-STK', NULL, NULL, NULL, 3, 4, NULL, 47, 1, 0, GETDATE(), GETDATE()),
(52, N'Staks Guest Seating', N'Multipurpose stacking chair', N'STK-400', NULL, NULL, NULL, 3, 4, NULL, 48, 1, 0, GETDATE(), GETDATE()),
(53, N'Grafton Guest Chair', N'Contemporary side seating', N'GRF-100', NULL, NULL, NULL, 3, 4, NULL, 49, 1, 0, GETDATE(), GETDATE()),
(54, N'Bounce Guest Chair', N'Flexible guest seating', N'BNC-200', NULL, NULL, NULL, 3, 4, NULL, 50, 1, 0, GETDATE(), GETDATE()),
(55, N'900 Series Exit Device', N'Rim exit device', N'900-RIM', NULL, NULL, NULL, 7, 1, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(56, N'800 Series Exit Device', N'Vertical rod exit device', N'800-VR', NULL, NULL, NULL, 7, 1, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(57, N'700 Series Exit Device', N'Mortise exit device', N'700-MOR', NULL, NULL, NULL, 7, 1, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(58, N'5000 Series Lock', N'Cylindrical lock set', N'5000-CYL', NULL, NULL, NULL, 7, 10, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(59, N'6000 Series Mortise Lock', N'Heavy-duty mortise lock', N'6000-MOR', NULL, NULL, NULL, 7, 10, NULL, 1, 1, 0, GETDATE(), GETDATE()),
(60, N'3M Window Film', N'Energy-saving window treatment', N'NV35', NULL, NULL, NULL, 1, 11, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(61, N'Prestige Window Film', N'Daylight-redirecting film', N'PR70', NULL, NULL, NULL, 1, 11, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(62, N'Night Vision Film', N'Privacy window film', N'NV15', NULL, NULL, NULL, 1, 11, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(63, N'Scotch Performance Masking Tape', N'Industrial masking tape', N'PM-2050', NULL, NULL, NULL, 6, 3, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(64, N'Scotch Box Sealing Tape', N'Packaging tape', N'3750-CLR', NULL, NULL, NULL, 6, 3, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(65, N'VHB Tape 5952', N'High-strength bonding tape', N'VHB-5952', NULL, NULL, NULL, 6, 3, NULL, 4, 1, 0, GETDATE(), GETDATE()),
(66, N'Philadelphia Commercial Carpet', N'Broadloom commercial carpet', N'PC-2000', NULL, NULL, NULL, 4, 14, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(67, N'Shaw Contract Carpet Tile', N'Modular carpet system', N'SCT-1500', NULL, NULL, NULL, 4, 2, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(68, N'Patcraft Artisan Carpet', N'Designer carpet tile', N'PAT-ART', NULL, NULL, NULL, 4, 2, NULL, 30, 1, 0, GETDATE(), GETDATE()),
(69, N'Queen Commercial Carpet', N'High-performance broadloom', N'QCC-3000', NULL, NULL, NULL, 4, 14, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(70, N'Tuftex Carpet', N'Residential and commercial carpet', N'TFX-2500', NULL, NULL, NULL, 4, 14, NULL, 2, 1, 0, GETDATE(), GETDATE()),
(71, N'SmartStrand Silk Reserve', N'Stain-resistant carpet', N'SSR-1000', NULL, NULL, NULL, 4, 14, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(72, N'Karastan Carpet', N'Luxury carpet products', N'KAR-2000', NULL, NULL, NULL, 4, 14, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(73, N'Durkan Carpet', N'Hospitality carpet', N'DRK-H100', NULL, NULL, NULL, 4, 14, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(74, N'Lees Carpet', N'Commercial broadloom', N'LS-C2000', NULL, NULL, NULL, 4, 14, NULL, 3, 1, 0, GETDATE(), GETDATE()),
(75, N'Interface Urban Retreat', N'Biophilic carpet tile', N'IUR-2000', NULL, NULL, NULL, 4, 2, NULL, 5, 1, 0, GETDATE(), GETDATE()),
(76, N'Interface Composure', N'Calming carpet design', N'IC-1500', NULL, NULL, NULL, 4, 2, NULL, 5, 1, 0, GETDATE(), GETDATE()),
(77, N'Interface Detour', N'Geometric carpet tile', N'ID-1000', NULL, NULL, NULL, 4, 2, NULL, 5, 1, 0, GETDATE(), GETDATE()),
(78, N'Interface On Line', N'Linear carpet design', N'IOL-2500', NULL, NULL, NULL, 4, 2, NULL, 5, 1, 0, GETDATE(), GETDATE()),
(79, N'Interface Walk the Plank', N'Wood-look carpet plank', N'IWP-3000', NULL, NULL, NULL, 4, 2, NULL, 5, 1, 0, GETDATE(), GETDATE()),
(80, N'Think Office Desk', N'Sit-stand workstation', N'THK-DSK', NULL, NULL, NULL, 3, 5, NULL, 6, 1, 0, GETDATE(), GETDATE()),
(81, N'Migration SE Desk', N'Height-adjustable desk', N'MIG-SE', NULL, NULL, NULL, 3, 5, NULL, 6, 1, 0, GETDATE(), GETDATE()),
(82, N'Elective Elements Desk', N'Modular desk system', N'EE-DSK', NULL, NULL, NULL, 3, 5, NULL, 6, 1, 0, GETDATE(), GETDATE()),
(83, N'Answer Panel System', N'Cubicle workstations', N'ANS-PNL', NULL, NULL, NULL, 3, 5, NULL, 6, 1, 0, GETDATE(), GETDATE()),
(84, N'Canvas Office Landscape', N'Open plan office system', N'CVS-OLS', NULL, NULL, NULL, 3, 5, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(85, N'Everywhere Tables', N'Multi-use tables', N'EVR-TBL', NULL, NULL, NULL, 3, 5, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(86, N'Memo Chair', N'Training room seating', N'MEM-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(87, N'Sayl Chair', N'Eco-friendly office chair', N'SAY-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(88, N'Verus Chair', N'TriFlex back task chair', N'VER-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(89, N'Caper Chair', N'Multipurpose stacking chair', N'CAP-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(90, N'Mirra 2 Chair', N'Ergonomic task chair', N'MR2-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(91, N'Cosm Chair', N'Auto-harmonic tilt chair', N'CSM-100', NULL, NULL, NULL, 3, 4, NULL, 7, 1, 0, GETDATE(), GETDATE()),
(92, N'Life Chair', N'Tri-panel back task seating', N'LIF-100', NULL, NULL, NULL, 3, 4, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(93, N'Chadwick Chair', N'Executive seating', N'CHD-100', NULL, NULL, NULL, 3, 4, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(94, N'Barcelona Chair', N'Iconic lounge seating', N'BAR-100', NULL, NULL, NULL, 3, 4, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(95, N'Dividends Horizon Panel System', N'Office cubicles', N'DH-PNL', NULL, NULL, NULL, 3, 5, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(96, N'Reff Profiles Desk', N'Height-adjustable desk', N'RP-DSK', NULL, NULL, NULL, 3, 5, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(97, N'Calibrate Series Storage', N'Office storage cabinets', N'CAL-STG', NULL, NULL, NULL, 3, 15, NULL, 8, 1, 0, GETDATE(), GETDATE()),
(98, N'Tone Height-Adjustable Desk', N'Sit-stand desk', N'TON-HAD', NULL, NULL, NULL, 3, 5, NULL, 9, 1, 0, GETDATE(), GETDATE()),
(99, N'Motivate Tables', N'Collaborative tables', N'MOT-TBL', NULL, NULL, NULL, 3, 5, NULL, 9, 1, 0, GETDATE(), GETDATE()),
(100, N'Answer Beam Seating', N'Bench seating system', N'ANS-BM', NULL, NULL, NULL, 3, 4, NULL, 9, 1, 0, GETDATE(), GETDATE());

-- Final 50 products (101-150)
INSERT INTO [Products] ([Id], [Name], [Description], [Model], [Sku], [Upc], [Asin], [CategoryId], [ProductTypeId], [ProductSubcategoryId], [CompanyId], [IsActive], [IsDeleted], [CreatedAt], [UpdatedAt])
VALUES
(101, N'Dune Ceiling System', N'Acoustical ceiling panels', N'DUN-24', NULL, NULL, NULL, 1, 12, NULL, 10, 1, 0, GETDATE(), GETDATE()),
(102, N'Calla Ceiling Tile', N'High-NRC ceiling tile', N'CAL-24', NULL, NULL, NULL, 1, 12, NULL, 10, 1, 0, GETDATE(), GETDATE()),
(103, N'Optima Ceiling Panel', N'Commercial ceiling system', N'OPT-24', NULL, NULL, NULL, 1, 12, NULL, 10, 1, 0, GETDATE(), GETDATE()),
(104, N'Ultima Health Zone Tile', N'Healthcare ceiling', N'UHZ-24', NULL, NULL, NULL, 1, 12, NULL, 10, 1, 0, GETDATE(), GETDATE()),
(105, N'Cirrus Ceiling Cloud', N'Suspended acoustical cloud', N'CIR-CLD', NULL, NULL, NULL, 1, 12, NULL, 10, 1, 0, GETDATE(), GETDATE()),
(106, N'ID Square Edge Carpet Tile', N'Premium modular carpet', N'IDS-500', NULL, NULL, NULL, 4, 2, NULL, 11, 1, 0, GETDATE(), GETDATE()),
(107, N'ProtectPlus 2 Carpet', N'Moisture barrier carpet', N'PP2-1000', NULL, NULL, NULL, 4, 2, NULL, 11, 1, 0, GETDATE(), GETDATE()),
(108, N'Ethos Modular Carpet', N'Sustainable carpet tile', N'ETH-2000', NULL, NULL, NULL, 4, 2, NULL, 11, 1, 0, GETDATE(), GETDATE()),
(109, N'Infinity PureColor Carpet', N'Cleanable broadloom', N'IPC-1500', NULL, NULL, NULL, 4, 14, NULL, 11, 1, 0, GETDATE(), GETDATE()),
(110, N'EcoSmart Vinyl Flooring', N'Phthalate-free LVT', N'ES-LVT', NULL, NULL, NULL, 10, 8, NULL, 11, 1, 0, GETDATE(), GETDATE()),
(111, N'Antron Lumena Fiber', N'High-performance nylon', N'ALM-FBR', NULL, NULL, NULL, 4, NULL, NULL, 12, 1, 0, GETDATE(), GETDATE()),
(112, N'ColorWorks Carpet Tile', N'Custom dyed carpet', N'CW-2000', NULL, NULL, NULL, 4, 2, NULL, 12, 1, 0, GETDATE(), GETDATE()),
(113, N'Antron Legacy Carpet', N'Recycled content carpet', N'ALG-1500', NULL, NULL, NULL, 4, 2, NULL, 12, 1, 0, GETDATE(), GETDATE()),
(114, N'StrataWorx Vinyl Tile', N'Dimensional vinyl', N'STW-VCT', NULL, NULL, NULL, 10, 8, NULL, 12, 1, 0, GETDATE(), GETDATE()),
(115, N'Blueprint Carpet Tile', N'Architectural carpet design', N'BLP-1000', NULL, NULL, NULL, 4, 2, NULL, 12, 1, 0, GETDATE(), GETDATE()),
(116, N'Collaborative Panel System', N'Team workspace', N'COL-PNL', NULL, NULL, NULL, 3, 5, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(117, N'Expansion Desking System', N'Modular office desks', N'EXP-DSK', NULL, NULL, NULL, 3, 5, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(118, N'Respond Task Chair', N'Ergonomic office chair', N'RES-100', NULL, NULL, NULL, 3, 4, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(119, N'Boulevard Seating', N'Lounge and occasional seating', N'BLV-100', NULL, NULL, NULL, 3, 4, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(120, N'Sidemark Storage', N'Filing and storage', N'SDM-STG', NULL, NULL, NULL, 3, 15, NULL, 13, 1, 0, GETDATE(), GETDATE()),
(121, N'Enwork Cressida Chair', N'Multi-purpose seating', N'CRS-100', NULL, NULL, NULL, 3, 4, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(122, N'Learning Commons Table', N'Educational furniture', N'LCT-1000', NULL, NULL, NULL, 3, 5, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(123, N'Intuitiv Height-Adjustable Desk', N'Sit-stand workstation', N'INT-HAD', NULL, NULL, NULL, 3, 5, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(124, N'Odeon Task Chair', N'Ergonomic task seating', N'ODE-100', NULL, NULL, NULL, 3, 4, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(125, N'Reflect Filing Cabinet', N'Metal storage cabinet', N'RFL-FC', NULL, NULL, NULL, 3, 15, NULL, 14, 1, 0, GETDATE(), GETDATE()),
(126, N'Powerbond Plus Carpet', N'Cushion-backed carpet', N'PBP-2000', NULL, NULL, NULL, 4, 2, NULL, 15, 1, 0, GETDATE(), GETDATE()),
(127, N'Contour Carpet Tile', N'Textured modular carpet', N'CNT-1500', NULL, NULL, NULL, 4, 2, NULL, 15, 1, 0, GETDATE(), GETDATE()),
(128, N'Assurance Carpet', N'Guaranteed carpet performance', N'ASR-1000', NULL, NULL, NULL, 4, 14, NULL, 15, 1, 0, GETDATE(), GETDATE()),
(129, N'Majestic Carpet Tile', N'Luxury carpet tile', N'MAJ-3000', NULL, NULL, NULL, 4, 2, NULL, 15, 1, 0, GETDATE(), GETDATE()),
(130, N'Eclipse Resilient Flooring', N'Rubber flooring', N'ECL-RF', NULL, NULL, NULL, 10, 8, NULL, 15, 1, 0, GETDATE(), GETDATE()),
(131, N'Prestige 24/7 Lock', N'Heavy-duty cylindrical lock', N'P247-CYL', NULL, NULL, NULL, 7, 10, NULL, 16, 1, 0, GETDATE(), GETDATE()),
(132, N'Falcon T Series Lock', N'Grade 1 cylindrical lock', N'FL-T100', NULL, NULL, NULL, 7, 10, NULL, 16, 1, 0, GETDATE(), GETDATE()),
(133, N'Schlage ND Series Lock', N'Commercial door lock', N'ND-80PD', NULL, NULL, NULL, 7, 10, NULL, 16, 1, 0, GETDATE(), GETDATE()),
(134, N'Von Duprin QEL Exit Device', N'Quiet electric latch', N'QEL-99', NULL, NULL, NULL, 7, 1, NULL, 16, 1, 0, GETDATE(), GETDATE()),
(135, N'LCN 4111 Series Closer', N'Door closer with hold-open', N'LCN-4111', NULL, NULL, NULL, 7, 1, NULL, 16, 1, 0, GETDATE(), GETDATE()),
(136, N'Dorma TS93 Door Closer', N'Universal door closer', N'TS93', NULL, NULL, NULL, 7, 1, NULL, 17, 1, 0, GETDATE(), GETDATE()),
(137, N'Kaba E-Plex 5000 Lock', N'Electronic pushbutton lock', N'EP5000', NULL, NULL, NULL, 7, 10, NULL, 17, 1, 0, GETDATE(), GETDATE()),
(138, N'Best Access Systems Lock', N'Interchangeable core lock', N'BAS-IC', NULL, NULL, NULL, 7, 10, NULL, 17, 1, 0, GETDATE(), GETDATE()),
(139, N'Dorma RTS88 Transom Closer', N'Overhead concealed closer', N'RTS88', NULL, NULL, NULL, 7, 1, NULL, 17, 1, 0, GETDATE(), GETDATE()),
(140, N'Saflok Electronic Lock', N'Hotel electronic lock', N'SFL-EL', NULL, NULL, NULL, 7, 10, NULL, 17, 1, 0, GETDATE(), GETDATE()),
(141, N'FiberGlass Insulation Batts', N'Thermal insulation rolls', N'FG-R19', NULL, NULL, NULL, 9, 7, NULL, 19, 1, 0, GETDATE(), GETDATE()),
(142, N'Foamular XPS Insulation', N'Extruded polystyrene insulation', N'FOM-R5', NULL, NULL, NULL, 9, 7, NULL, 19, 1, 0, GETDATE(), GETDATE()),
(143, N'Thermafiber Mineral Wool', N'Fire-resistant insulation', N'TF-R15', NULL, NULL, NULL, 9, 7, NULL, 19, 1, 0, GETDATE(), GETDATE()),
(144, N'703 Fiberglas Panels', N'Acoustical insulation boards', N'703-2IN', NULL, NULL, NULL, 9, 7, NULL, 19, 1, 0, GETDATE(), GETDATE()),
(145, N'PINK Next Gen Insulation', N'Formaldehyde-free batts', N'PNG-R13', NULL, NULL, NULL, 9, 7, NULL, 19, 1, 0, GETDATE(), GETDATE()),
(146, N'ComfortTherm Insulation', N'Bio-based insulation', N'CT-R21', NULL, NULL, NULL, 9, 7, NULL, 20, 1, 0, GETDATE(), GETDATE()),
(147, N'Spider Insulation', N'Spray-applied insulation', N'SPD-R19', NULL, NULL, NULL, 9, 7, NULL, 20, 1, 0, GETDATE(), GETDATE()),
(148, N'ClimateControl Insulation', N'Multi-functional insulation', N'CC-R25', NULL, NULL, NULL, 9, 7, NULL, 20, 1, 0, GETDATE(), GETDATE()),
(149, N'TouchStone Mineral Wool', N'Stone wool insulation', N'TST-MW', NULL, NULL, NULL, 9, 7, NULL, 20, 1, 0, GETDATE(), GETDATE()),
(150, N'FormPlus Insulation Board', N'Rigid insulation board', N'FP-R10', NULL, NULL, NULL, 9, 7, NULL, 20, 1, 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT [Products] OFF;
PRINT '✓ Products: 150 rows';
GO


-- =========================================
-- 11. CERTIFICATES (250 certificates)
-- =========================================
PRINT 'Importing Certificates (this will take a minute)...';

SET IDENTITY_INSERT [Certificates] ON;

-- Generate 250 certificates with variety in status, dates, companies, products, and cert types
-- Status distribution: ~70% active, ~10% draft, ~10% expired, ~5% pending, ~5% withdrawn
-- This is a sampling - in production you'd generate all 250

-- Active certificates (1-175)
-- Sample of first 50 active certificates
DECLARE @i INT = 1;
WHILE @i <= 50
BEGIN
    INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country],
                                  [Asin], [Upc], [Gtin], [Ean], [CertificateFile], [CertificateImagePath], [Partner],
                                  [CompanyId], [ProductId], [CertificationTypeId], [CreatedBy], [UpdatedBy], [CreatedAt], [UpdatedAt])
    VALUES
    (@i,
     CONCAT('25-', RIGHT('0000' + CAST(@i AS VARCHAR), 4)),
     'active',
     DATEADD(DAY, -(@i * 3), GETDATE()),
     DATEADD(YEAR, 3, DATEADD(DAY, -(@i * 3), GETDATE())),
     CASE (@i % 10)
         WHEN 0 THEN 'Berlin, CT'
         WHEN 1 THEN 'Dalton, GA'
         WHEN 2 THEN 'Grand Rapids, MI'
         WHEN 3 THEN 'Zeeland, MI'
         WHEN 4 THEN 'Atlanta, GA'
         WHEN 5 THEN 'Toronto, ON'
         WHEN 6 THEN 'New York, NY'
         WHEN 7 THEN 'Chicago, IL'
         WHEN 8 THEN 'Los Angeles, CA'
         ELSE 'Seattle, WA'
     END,
     'United States',
     CASE WHEN @i % 3 = 0 THEN CONCAT('B0', RIGHT('00000' + CAST(@i AS VARCHAR), 5)) ELSE NULL END,
     CASE WHEN @i % 2 = 0 THEN CONCAT(ABS(CHECKSUM(NEWID()) % 900000000 + 100000000), RIGHT('0000' + CAST(@i AS VARCHAR), 4)) ELSE NULL END,
     NULL,
     NULL,
     NULL,
     NULL,
     CASE WHEN @i % 5 = 0 THEN 'Amazon' WHEN @i % 7 = 0 THEN 'Wayfair' ELSE 'None' END,
     ((@i - 1) % 50) + 1,  -- CompanyId 1-50
     ((@i - 1) % 150) + 1, -- ProductId 1-150
     ((@i - 1) % 10) + 1,  -- CertificationTypeId 1-10
     NULL,
     NULL,
     DATEADD(DAY, -(@i * 3 + 10), GETDATE()),
     DATEADD(DAY, -5, GETDATE()));

    SET @i = @i + 1;
END

-- Continue with more active certificates (51-175) - simplified bulk insert
SET @i = 51;
WHILE @i <= 175
BEGIN
    INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country],
                                  [Partner], [CompanyId], [ProductId], [CertificationTypeId], [CreatedAt], [UpdatedAt])
    VALUES
    (@i,
     CONCAT('25-', RIGHT('0000' + CAST(@i AS VARCHAR), 4)),
     'active',
     DATEADD(DAY, -(@i * 2), GETDATE()),
     DATEADD(YEAR, 3, DATEADD(DAY, -(@i * 2), GETDATE())),
     CASE (@i % 5) WHEN 0 THEN 'New York, NY' WHEN 1 THEN 'Los Angeles, CA' WHEN 2 THEN 'Chicago, IL' WHEN 3 THEN 'Houston, TX' ELSE 'Phoenix, AZ' END,
     'United States',
     CASE WHEN @i % 4 = 0 THEN 'Amazon' WHEN @i % 6 = 0 THEN 'Wayfair' ELSE 'None' END,
     ((@i - 1) % 50) + 1,
     ((@i - 1) % 150) + 1,
     ((@i - 1) % 10) + 1,
     DATEADD(DAY, -(@i * 2 + 15), GETDATE()),
     DATEADD(DAY, -3, GETDATE()));

    SET @i = @i + 1;
END

-- Draft certificates (176-200)
SET @i = 176;
WHILE @i <= 200
BEGIN
    INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country],
                                  [Partner], [CompanyId], [ProductId], [CertificationTypeId], [CreatedAt], [UpdatedAt])
    VALUES
    (@i,
     CONCAT('25-', RIGHT('0000' + CAST(@i AS VARCHAR), 4)),
     'draft',
     DATEADD(DAY, 30, GETDATE()),
     DATEADD(YEAR, 3, DATEADD(DAY, 30, GETDATE())),
     'Pending Location',
     'United States',
     'None',
     ((@i - 1) % 50) + 1,
     ((@i - 1) % 150) + 1,
     ((@i - 1) % 10) + 1,
     DATEADD(DAY, -10, GETDATE()),
     DATEADD(DAY, -1, GETDATE()));

    SET @i = @i + 1;
END

-- Expired certificates (201-225)
SET @i = 201;
WHILE @i <= 225
BEGIN
    INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country],
                                  [Partner], [CompanyId], [ProductId], [CertificationTypeId], [CreatedAt], [UpdatedAt])
    VALUES
    (@i,
     CONCAT('24-', RIGHT('0000' + CAST(@i - 200 AS VARCHAR), 4)),
     'expired',
     DATEADD(YEAR, -4, GETDATE()),
     DATEADD(YEAR, -1, DATEADD(DAY, -30, GETDATE())),
     CASE (@i % 4) WHEN 0 THEN 'Atlanta, GA' WHEN 1 THEN 'Boston, MA' WHEN 2 THEN 'Denver, CO' ELSE 'Miami, FL' END,
     'United States',
     'None',
     ((@i - 1) % 50) + 1,
     ((@i - 1) % 150) + 1,
     ((@i - 1) % 10) + 1,
     DATEADD(YEAR, -4, DATEADD(DAY, -20, GETDATE())),
     DATEADD(DAY, -60, GETDATE()));

    SET @i = @i + 1;
END

-- Pending certificates (226-238)
SET @i = 226;
WHILE @i <= 238
BEGIN
    INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country],
                                  [Partner], [CompanyId], [ProductId], [CertificationTypeId], [CreatedAt], [UpdatedAt])
    VALUES
    (@i,
     CONCAT('25-', RIGHT('0000' + CAST(@i AS VARCHAR), 4)),
     'pending_review',
     GETDATE(),
     DATEADD(YEAR, 3, GETDATE()),
     'Under Review',
     'United States',
     'None',
     ((@i - 1) % 50) + 1,
     ((@i - 1) % 150) + 1,
     ((@i - 1) % 10) + 1,
     DATEADD(DAY, -5, GETDATE()),
     GETDATE());

    SET @i = @i + 1;
END

-- Withdrawn certificates (239-250)
SET @i = 239;
WHILE @i <= 250
BEGIN
    INSERT INTO [Certificates] ([Id], [Number], [Status], [EffectiveDate], [ExpirationDate], [Location], [Country],
                                  [Partner], [CompanyId], [ProductId], [CertificationTypeId], [CreatedAt], [UpdatedAt])
    VALUES
    (@i,
     CONCAT('24-', RIGHT('0000' + CAST(@i - 200 AS VARCHAR), 4)),
     'withdrawn',
     DATEADD(YEAR, -2, GETDATE()),
     DATEADD(YEAR, 1, DATEADD(DAY, -90, GETDATE())),
     'Withdrawn',
     'United States',
     'None',
     ((@i - 1) % 50) + 1,
     ((@i - 1) % 150) + 1,
     ((@i - 1) % 10) + 1,
     DATEADD(YEAR, -2, DATEADD(DAY, -15, GETDATE())),
     DATEADD(DAY, -90, GETDATE()));

    SET @i = @i + 1;
END

SET IDENTITY_INSERT [Certificates] OFF;
PRINT '✓ Certificates: 250 rows';
GO


-- =========================================
-- 12. CERTIFICATE TAGS (sample associations)
-- =========================================
PRINT 'Linking certificates to tags...';

-- Link some active certificates to public tags
INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT TOP 30 c.Id, 3 FROM Certificates c WHERE c.Status = 'active' ORDER BY NEWID();  -- Amazon Featured

INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT TOP 25 c.Id, 4 FROM Certificates c WHERE c.Status = 'active' ORDER BY NEWID();  -- New Product

INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT TOP 40 c.Id, 6 FROM Certificates c WHERE c.Status = 'active' ORDER BY NEWID();  -- Eco-Friendly

INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT TOP 20 c.Id, 5 FROM Certificates c WHERE c.Status = 'active' ORDER BY NEWID();  -- Best Seller

-- Link draft certificates to internal tags
INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT c.Id, 7 FROM Certificates c WHERE c.Status = 'draft';  -- Pending Approval

-- Link some certificates to specialty tags
INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT TOP 15 c.Id, 14 FROM Certificates c WHERE c.Country = 'United States' AND c.Status = 'active' ORDER BY NEWID();  -- Made in USA

INSERT INTO CertificateTag (CertificatesId, TagsId)
SELECT TOP 10 c.Id, 26 FROM Certificates c WHERE c.Status = 'active' ORDER BY NEWID();  -- LEED Compliant

PRINT '✓ Certificate-Tag associations created';
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
PRINT 'LARGE TEST DATASET - Verification:'
PRINT '========================================='

SELECT 'Roles' as TableName, COUNT(*) as RowCount FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL SELECT 'ProductCategories', COUNT(*) FROM ProductCategories
UNION ALL SELECT 'ProductTypes', COUNT(*) FROM ProductTypes
UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL SELECT 'CertificateTag', COUNT(*) FROM CertificateTag
ORDER BY TableName;

PRINT ''
PRINT '========================================='
PRINT 'Certificate Status Breakdown:'
PRINT '========================================='
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status
ORDER BY Status;

PRINT ''
PRINT '========================================='
PRINT 'SUCCESS! Database loaded with:'
PRINT '  - 4 Roles'
PRINT '  - 15 Users (2 Admin, 3 Analyst, 7 External, 3 Public)'
PRINT '  - 3 Partners'
PRINT '  - 50 Companies'
PRINT '  - 10 Product Categories'
PRINT '  - 15 Product Types'
PRINT '  - 10 Certification Types'
PRINT '  - 30 Tags (public + internal)'
PRINT '  - 150 Products'
PRINT '  - 250 Certificates'
PRINT ''
PRINT 'Login credentials (all passwords: Admin123!):'
PRINT '  - Admin: admin@greencirclecertified.com'
PRINT '  - Analyst: analyst@greencirclecertified.com'
PRINT '  - External: partner@amazon.com'
PRINT ''
PRINT 'Next steps:'
PRINT '  1. Visit /Certificate/CertificateTileView'
PRINT '  2. Verify ~175 active certificates display'
PRINT '  3. Test filters, search, and downloads'
PRINT '========================================='
GO

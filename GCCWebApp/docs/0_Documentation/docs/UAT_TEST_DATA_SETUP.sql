-- ============================================================================
-- GCC Database Solution - UAT Test Data Setup Script
-- Version: 1.0 | Date: 2026-02-19
-- Purpose: Creates pre-condition test data for COMPREHENSIVE_UAT_GUIDE.md
-- ============================================================================
-- USAGE: Run this script against the UAT database BEFORE executing test cases.
-- WARNING: This script uses INSERT with explicit IDs. Run on a clean/empty DB
--          or adjust IDs to avoid conflicts with existing data.
-- ============================================================================

SET NOCOUNT ON;
PRINT '=== GCC UAT Test Data Setup Starting ===';

-- ============================================================================
-- SECTION 1: PARTNERS (Pre-requisite for companies and certificates)
-- ============================================================================
PRINT 'Creating Partners...';

SET IDENTITY_INSERT Partners ON;

IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Amazon')
    INSERT INTO Partners (Id, Name, Description, Website, Email, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (1, 'Amazon', 'Amazon partner integration', 'https://www.amazon.com', 'partner@amazon.com', 1, 0, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Novi')
    INSERT INTO Partners (Id, Name, Description, Website, Email, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (2, 'Novi', 'Novi Connect partner', 'https://www.noviconnect.com', 'partner@noviconnect.com', 1, 0, 2, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Other')
    INSERT INTO Partners (Id, Name, Description, Website, Email, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (3, 'Other', 'Other partners', NULL, NULL, 1, 0, 3, GETUTCDATE(), GETUTCDATE());

SET IDENTITY_INSERT Partners OFF;
PRINT '  Partners created: Amazon, Novi, Other';

-- ============================================================================
-- SECTION 2: COMPANIES (Pre-requisite for products, certificates, external users)
-- ============================================================================
PRINT 'Creating Companies...';

SET IDENTITY_INSERT Companies ON;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'EcoProduct Co')
    INSERT INTO Companies (Id, Name, ContactInfo, Email, Website, Location, Country, PartnerId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (1, 'EcoProduct Co', 'John Smith', 'contact@ecoproduct.com', 'https://ecoproduct.com', 'Seattle, WA', 'United States', 1, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'GreenTech Industries')
    INSERT INTO Companies (Id, Name, ContactInfo, Email, Website, Location, Country, PartnerId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (2, 'GreenTech Industries', 'Jane Doe', 'info@greentech.com', 'https://greentech.com', 'Portland, OR', 'United States', 2, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'TestCo UAT')
    INSERT INTO Companies (Id, Name, ContactInfo, Email, Website, Location, Country, PartnerId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (3, 'TestCo UAT', 'Test Contact', 'test@testco.com', 'https://testco.com', 'Portland, OR', 'United States', NULL, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'OtherCo')
    INSERT INTO Companies (Id, Name, ContactInfo, Email, Website, Location, Country, PartnerId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (4, 'OtherCo', 'Other Contact', 'info@otherco.com', 'https://otherco.com', 'Denver, CO', 'United States', NULL, 1, 0, GETUTCDATE(), GETUTCDATE());

SET IDENTITY_INSERT Companies OFF;
PRINT '  Companies created: EcoProduct Co, GreenTech Industries, TestCo UAT, OtherCo';

-- ============================================================================
-- SECTION 3: PRODUCT CATEGORIES & TYPES
-- ============================================================================
PRINT 'Creating Product Categories and Types...';

SET IDENTITY_INSERT ProductCategories ON;

IF NOT EXISTS (SELECT 1 FROM ProductCategories WHERE Name = 'Consumer Goods')
    INSERT INTO ProductCategories (Id, Name, Description, SortOrder, IsActive)
    VALUES (1, 'Consumer Goods', 'Consumer product category', 1, 1);

IF NOT EXISTS (SELECT 1 FROM ProductCategories WHERE Name = 'Industrial')
    INSERT INTO ProductCategories (Id, Name, Description, SortOrder, IsActive)
    VALUES (2, 'Industrial', 'Industrial product category', 2, 1);

SET IDENTITY_INSERT ProductCategories OFF;

SET IDENTITY_INSERT ProductTypes ON;

IF NOT EXISTS (SELECT 1 FROM ProductTypes WHERE Name = 'Paper Products' AND CategoryId = 1)
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive)
    VALUES (1, 'Paper Products', 1, NULL, 0, 1, 1);

IF NOT EXISTS (SELECT 1 FROM ProductTypes WHERE Name = 'Cleaning Products' AND CategoryId = 1)
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive)
    VALUES (2, 'Cleaning Products', 1, NULL, 0, 2, 1);

IF NOT EXISTS (SELECT 1 FROM ProductTypes WHERE Name = 'Water Treatment' AND CategoryId = 2)
    INSERT INTO ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive)
    VALUES (3, 'Water Treatment', 2, NULL, 0, 1, 1);

SET IDENTITY_INSERT ProductTypes OFF;

-- ============================================================================
-- SECTION 4: PRODUCTS (Pre-requisite for certificates)
-- ============================================================================
PRINT 'Creating Products...';

SET IDENTITY_INSERT Products ON;

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'Recycled Paper Towels')
    INSERT INTO Products (Id, Name, Description, Model, Sku, Upc, Asin, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (1, 'Recycled Paper Towels', '100% recycled paper towels', 'RPT-100', 'SKU-RPT100', '846378985421', 'B08N5WRWNW', 1, 1, 1, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'Eco Cleaning Spray')
    INSERT INTO Products (Id, Name, Description, Model, Sku, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (2, 'Eco Cleaning Spray', 'Biodegradable cleaning solution', 'ECS-200', 'SKU-ECS200', 1, 2, 1, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'Solar Panel Module')
    INSERT INTO Products (Id, Name, Description, Model, Sku, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (3, 'Solar Panel Module', 'High-efficiency solar panel', 'SPM-300', 'SKU-SPM300', 2, 3, 2, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'EcoWidget Pro')
    INSERT INTO Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (4, 'EcoWidget Pro', 'TestCo flagship product', 1, 1, 3, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'EcoWidget Lite')
    INSERT INTO Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (5, 'EcoWidget Lite', 'TestCo budget product', 1, 1, 3, 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'OtherCo Product')
    INSERT INTO Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (6, 'OtherCo Product', 'Product from different company', 1, 2, 4, 1, 0, GETUTCDATE(), GETUTCDATE());

SET IDENTITY_INSERT Products OFF;
PRINT '  Products created: 6 products across 4 companies';

-- ============================================================================
-- SECTION 5: CERTIFICATION TYPES (Parent/Child hierarchy)
-- ============================================================================
PRINT 'Creating Certification Types...';

SET IDENTITY_INSERT CertificationTypes ON;

-- Parent types
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'UL 2809' AND ParentId IS NULL)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (1, 'UL 2809', 'UL2809', 'Environmental claim validation for recycled content', 1, 1, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'SCS Global' AND ParentId IS NULL)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (2, 'SCS Global', 'SCSGLOBAL', 'SCS Global Services certification', 1, 2, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Environmental Compliance' AND ParentId IS NULL)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (3, 'Environmental Compliance', 'ENVCOMP', 'Environmental compliance standards', 1, 3, 1, NULL);

-- Subcategories (children)
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Level 1' AND ParentId = 1)
    INSERT INTO CertificationTypes (Id, Name, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (4, 'Level 1', 'Minimum recycled content threshold', 1, 1, 2, 1);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Level 2' AND ParentId = 1)
    INSERT INTO CertificationTypes (Id, Name, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (5, 'Level 2', 'Enhanced recycled content verification', 1, 2, 2, 1);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Indoor Advantage Gold' AND ParentId = 2)
    INSERT INTO CertificationTypes (Id, Name, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (6, 'Indoor Advantage Gold', 'Low VOC emissions for indoor air quality', 1, 1, 2, 2);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Water Conservation Level 1' AND ParentId = 3)
    INSERT INTO CertificationTypes (Id, Name, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (7, 'Water Conservation Level 1', 'Basic water conservation standard', 1, 1, 2, 3);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Water Conservation Level 2' AND ParentId = 3)
    INSERT INTO CertificationTypes (Id, Name, Description, IsActive, SortOrder, Level, ParentId)
    VALUES (8, 'Water Conservation Level 2', 'Advanced water conservation standard', 1, 2, 2, 3);

SET IDENTITY_INSERT CertificationTypes OFF;
PRINT '  Certification Types created: 3 parents with 5 subcategories';

-- ============================================================================
-- SECTION 6: CUSTOM FIELDS (Linked to certification types)
-- ============================================================================
PRINT 'Creating Custom Fields...';

SET IDENTITY_INSERT CustomFields ON;

IF NOT EXISTS (SELECT 1 FROM CustomFields WHERE Name = 'Testing Lab' AND CertificateTypeId = 3)
    INSERT INTO CustomFields (Id, Name, FieldType, CertificateTypeId, IsRequired, IsVisible, RoleVisibility, SortOrder)
    VALUES (1, 'Testing Lab', 'Text', 3, 1, 1, 'Public', 1);

IF NOT EXISTS (SELECT 1 FROM CustomFields WHERE Name = 'Compliance Score' AND CertificateTypeId = 3)
    INSERT INTO CustomFields (Id, Name, FieldType, CertificateTypeId, IsRequired, IsVisible, RoleVisibility, SortOrder)
    VALUES (2, 'Compliance Score', 'Number', 3, 0, 1, 'Public', 2);

IF NOT EXISTS (SELECT 1 FROM CustomFields WHERE Name = 'Recycled Content %' AND CertificateTypeId = 1)
    INSERT INTO CustomFields (Id, Name, FieldType, CertificateTypeId, IsRequired, IsVisible, RoleVisibility, SortOrder)
    VALUES (3, 'Recycled Content %', 'Number', 1, 0, 1, 'Public', 1);

SET IDENTITY_INSERT CustomFields OFF;
PRINT '  Custom Fields created: Testing Lab, Compliance Score, Recycled Content %';

-- ============================================================================
-- SECTION 7: TAGS (Internal and External)
-- ============================================================================
PRINT 'Creating Tags...';

SET IDENTITY_INSERT Tags ON;

-- External tags (visible to all users)
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'Sustainable')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (1, 'Sustainable', 'external', 'Sustainability certification', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'Verified')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (2, 'Verified', 'external', 'Third-party verified', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'Water Positive')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (3, 'Water Positive', 'external', 'Water positive initiative', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'Eco-Friendly')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (4, 'Eco-Friendly', 'external', 'Eco-friendly product', 1);

-- Internal tags (visible to analyst/admin only)
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'Priority')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (5, 'Priority', 'internal', 'Priority review flag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'FastTrack')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (6, 'FastTrack', 'internal', 'Fast-track processing', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Name = 'NeedsFollowUp')
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive)
    VALUES (7, 'NeedsFollowUp', 'internal', 'Requires follow-up action', 1);

SET IDENTITY_INSERT Tags OFF;
PRINT '  Tags created: 4 external, 3 internal';

-- ============================================================================
-- SECTION 8: PATHWAYS
-- ============================================================================
PRINT 'Creating Pathways...';

SET IDENTITY_INSERT Pathways ON;

IF NOT EXISTS (SELECT 1 FROM Pathways WHERE Name = 'Standard Certification')
    INSERT INTO Pathways (Id, Name, IsActive, SortOrder, IsDeleted)
    VALUES (1, 'Standard Certification', 1, 1, 0);

IF NOT EXISTS (SELECT 1 FROM Pathways WHERE Name = 'Express Certification')
    INSERT INTO Pathways (Id, Name, IsActive, SortOrder, IsDeleted)
    VALUES (2, 'Express Certification', 1, 2, 0);

SET IDENTITY_INSERT Pathways OFF;
PRINT '  Pathways created: Standard, Express';

-- ============================================================================
-- SECTION 9: CERTIFICATES (Various statuses for testing)
-- Pre-requisite: Companies, Products, CertificationTypes must exist
-- ============================================================================
PRINT 'Creating Certificates...';

SET IDENTITY_INSERT Certificates ON;

-- Certificate A: Active, TestCo, Amazon partner, has tags and product IDs
-- Used in: E2E Scenario 9 (Search & Discovery Cross-Role)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0001')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CertificationResults, CompanyId, ProductId, CertificationTypeId, CertificationTypeSubcategoryId, CreatedAt, UpdatedAt)
    VALUES (1, '26-0001', 'Active', '2026-01-15', '2027-01-15', 'Seattle, WA', 'United States', 'Amazon', '95.5% recycled content', 3, 4, 1, 5, GETUTCDATE(), GETUTCDATE());

-- Certificate B: Active, OtherCo, No partner
-- Used in: E2E Scenario 9 (cross-company isolation test)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0002')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CertificationTypeSubcategoryId, CreatedAt, UpdatedAt)
    VALUES (2, '26-0002', 'Active', '2026-02-01', '2027-02-01', 'Denver, CO', 'United States', 'None', 4, 6, 2, 6, GETUTCDATE(), GETUTCDATE());

-- Certificate C: Draft, TestCo (should NOT be visible to public/external)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0003')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES (3, '26-0003', 'Draft', '2026-03-01', '2027-03-01', 'Portland, OR', 'United States', 'None', 3, 5, 1, GETUTCDATE(), GETUTCDATE());

-- Certificate D: Archived, TestCo (final state - no transitions)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0004')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CertificationTypeSubcategoryId, CreatedAt, UpdatedAt)
    VALUES (4, '26-0004', 'Archived', '2025-01-01', '2026-01-01', 'Seattle, WA', 'United States', 'Amazon', 3, 4, 1, 4, GETUTCDATE(), GETUTCDATE());

-- Certificate E: Active, EcoProduct Co (for general testing)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0005')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CertificationResults, CompanyId, ProductId, CertificationTypeId, CertificationTypeSubcategoryId, CreatedAt, UpdatedAt)
    VALUES (5, '26-0005', 'Active', '2026-01-01', '2027-06-01', 'Seattle, WA', 'United States', 'Amazon', '92% recycled content', 1, 1, 1, 5, GETUTCDATE(), GETUTCDATE());

-- Certificate F: Expired (for renewal lifecycle testing)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0006')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES (6, '26-0006', 'Expired', '2025-01-01', '2026-01-15', 'Portland, OR', 'United States', 'Novi', 2, 3, 2, GETUTCDATE(), GETUTCDATE());

-- Certificate G: PendingReview (for workflow testing)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0007')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CertificationTypeSubcategoryId, CreatedAt, UpdatedAt)
    VALUES (7, '26-0007', 'PendingReview', '2026-02-01', '2027-02-01', 'Denver, CO', 'United States', 'None', 1, 2, 3, 7, GETUTCDATE(), GETUTCDATE());

-- Certificate H: Active, about to expire (for renewal auto-transition testing)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0008')
    INSERT INTO Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES (8, '26-0008', 'Active', '2025-03-01', DATEADD(DAY, 25, GETUTCDATE()), 'Seattle, WA', 'United States', 'Amazon', 1, 1, 1, GETUTCDATE(), GETUTCDATE());

SET IDENTITY_INSERT Certificates OFF;
PRINT '  Certificates created: 8 certs (Active, Draft, Archived, Expired, PendingReview)';

-- ============================================================================
-- SECTION 10: CERTIFICATE TAGS (Many-to-Many associations)
-- ============================================================================
PRINT 'Creating Certificate-Tag associations...';

-- Cert A (26-0001): Sustainable (external) + Priority (internal)
IF NOT EXISTS (SELECT 1 FROM CertificateTags WHERE CertificatesId = 1 AND TagsId = 1)
    INSERT INTO CertificateTags (CertificatesId, TagsId) VALUES (1, 1);
IF NOT EXISTS (SELECT 1 FROM CertificateTags WHERE CertificatesId = 1 AND TagsId = 5)
    INSERT INTO CertificateTags (CertificatesId, TagsId) VALUES (1, 5);

-- Cert B (26-0002): Verified (external)
IF NOT EXISTS (SELECT 1 FROM CertificateTags WHERE CertificatesId = 2 AND TagsId = 2)
    INSERT INTO CertificateTags (CertificatesId, TagsId) VALUES (2, 2);

-- Cert E (26-0005): Eco-Friendly (external) + FastTrack (internal)
IF NOT EXISTS (SELECT 1 FROM CertificateTags WHERE CertificatesId = 5 AND TagsId = 4)
    INSERT INTO CertificateTags (CertificatesId, TagsId) VALUES (5, 4);
IF NOT EXISTS (SELECT 1 FROM CertificateTags WHERE CertificatesId = 5 AND TagsId = 6)
    INSERT INTO CertificateTags (CertificatesId, TagsId) VALUES (5, 6);

PRINT '  Certificate-Tag associations created';

-- ============================================================================
-- SECTION 11: CERTIFICATE PRODUCT IDS
-- ============================================================================
PRINT 'Creating Certificate Product IDs...';

SET IDENTITY_INSERT CertificateProductIds ON;

-- Cert A (26-0001): ASIN + UPC
IF NOT EXISTS (SELECT 1 FROM CertificateProductIds WHERE CertificateId = 1 AND ProductIdType = 'ASIN')
    INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, Status, Partner, ProductName, ProductSku, IsActive, IsPublic, CreatedAt, UpdatedAt)
    VALUES (1, 1, 'ASIN', 'B08X7N9MRQ', 'Active', 'Amazon', 'EcoWidget Pro', 'SKU-EWP100', 1, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM CertificateProductIds WHERE CertificateId = 1 AND ProductIdType = 'UPC')
    INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, Status, Partner, ProductName, ProductSku, IsActive, IsPublic, CreatedAt, UpdatedAt)
    VALUES (2, 1, 'UPC', '846378985421', 'Active', 'None', 'EcoWidget Pro', 'SKU-EWP100', 1, 1, GETUTCDATE(), GETUTCDATE());

-- Cert E (26-0005): ASIN + GTIN
IF NOT EXISTS (SELECT 1 FROM CertificateProductIds WHERE CertificateId = 5 AND ProductIdType = 'ASIN')
    INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, Status, Partner, ProductName, ProductSku, IsActive, IsPublic, CreatedAt, UpdatedAt)
    VALUES (3, 5, 'ASIN', 'B08N5WRWNW', 'Active', 'Amazon', 'Recycled Paper Towels', 'SKU-RPT100', 1, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM CertificateProductIds WHERE CertificateId = 5 AND ProductIdType = 'GTIN')
    INSERT INTO CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue, Status, Partner, ProductName, IsActive, IsPublic, CreatedAt, UpdatedAt)
    VALUES (4, 5, 'GTIN', '00846378985421', 'Active', 'Novi', 'Recycled Paper Towels', 1, 1, GETUTCDATE(), GETUTCDATE());

SET IDENTITY_INSERT CertificateProductIds OFF;
PRINT '  Certificate Product IDs created: 4 IDs (ASIN, UPC, GTIN)';

-- ============================================================================
-- SECTION 12: ROLES & PERMISSIONS
-- Note: These may already exist from initial migration. Using IF NOT EXISTS.
-- ============================================================================
PRINT 'Verifying Roles...';

SET IDENTITY_INSERT Roles ON;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    INSERT INTO Roles (Id, Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES (1, 'Admin', 'Full system administrator', 1, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Analyst')
    INSERT INTO Roles (Id, Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES (2, 'Analyst', 'Certificate analyst - create/edit/manage certificates', 1, 1, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'External')
    INSERT INTO Roles (Id, Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES (3, 'External', 'External client - read-only company-scoped access', 1, 1, GETUTCDATE(), GETUTCDATE());

SET IDENTITY_INSERT Roles OFF;
PRINT '  Roles verified: Admin, Analyst, External';

-- ============================================================================
-- SECTION 13: TEST USERS
-- Note: Password hashes are for "TestPass123!" - adjust for your auth system.
-- The actual hash depends on your IPasswordHasher implementation.
-- You may need to create these users through the application UI instead.
-- ============================================================================
PRINT 'Verifying Test Users...';
PRINT '  NOTE: Users should be created through the application UI to ensure';
PRINT '  proper password hashing. Use these credentials:';
PRINT '    admin / TestPass123! (Admin role)';
PRINT '    analyst / TestPass123! (Analyst role)';
PRINT '    external / TestPass123! (External role, Company: TestCo UAT)';
PRINT '';
PRINT '  If users already exist, verify UserCompanies association:';

-- Associate external user with TestCo UAT (Company Id 3)
-- First find the external user ID
DECLARE @ExternalUserId INT;
SELECT @ExternalUserId = Id FROM Users WHERE Username = 'external';

IF @ExternalUserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM UserCompanies WHERE UserId = @ExternalUserId AND CompaniesId = 3)
        INSERT INTO UserCompanies (UserId, CompaniesId) VALUES (@ExternalUserId, 3);
    PRINT '  External user associated with TestCo UAT (Company Id 3)';
END
ELSE
BEGIN
    PRINT '  WARNING: External user not found. Create via UI: external / TestPass123! / External role / TestCo UAT';
END

-- ============================================================================
-- SECTION 14: CERTIFICATE STATUS HISTORY (Audit trail for existing certs)
-- ============================================================================
PRINT 'Creating Certificate Status History...';

DECLARE @AdminUserId INT;
SELECT @AdminUserId = Id FROM Users WHERE Username = 'admin';

IF @AdminUserId IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CertificateStatusHistory ON;

    -- Cert D (26-0004) lifecycle: Draft → Active → Expired → Archived
    IF NOT EXISTS (SELECT 1 FROM CertificateStatusHistory WHERE CertificateId = 4 AND NewStatus = 'Active')
        INSERT INTO CertificateStatusHistory (Id, CertificateId, CertificateNumber, PreviousStatus, NewStatus, ChangedBy, ChangedByUsername, ChangedAt, ChangeSource, IsAutomatedTransition)
        VALUES (1, 4, '26-0004', 'Draft', 'Active', @AdminUserId, 'admin', '2025-01-15', 'Manual', 0);

    IF NOT EXISTS (SELECT 1 FROM CertificateStatusHistory WHERE CertificateId = 4 AND NewStatus = 'Expired')
        INSERT INTO CertificateStatusHistory (Id, CertificateId, CertificateNumber, PreviousStatus, NewStatus, ChangedBy, ChangedByUsername, ChangedAt, ChangeSource, IsAutomatedTransition)
        VALUES (2, 4, '26-0004', 'Active', 'Expired', @AdminUserId, 'admin', '2026-01-01', 'System', 1);

    IF NOT EXISTS (SELECT 1 FROM CertificateStatusHistory WHERE CertificateId = 4 AND NewStatus = 'Archived')
        INSERT INTO CertificateStatusHistory (Id, CertificateId, CertificateNumber, PreviousStatus, NewStatus, ChangedBy, ChangedByUsername, ChangedAt, ChangeSource, IsAutomatedTransition)
        VALUES (3, 4, '26-0004', 'Expired', 'Archived', @AdminUserId, 'admin', '2026-01-10', 'Manual', 0);

    SET IDENTITY_INSERT CertificateStatusHistory OFF;
    PRINT '  Status history created for Certificate 26-0004';
END
ELSE
BEGIN
    PRINT '  WARNING: Admin user not found. Status history skipped.';
END

-- ============================================================================
-- SECTION 15: VERIFICATION QUERIES
-- Run these to verify test data was created correctly
-- ============================================================================
PRINT '';
PRINT '=== VERIFICATION ===';

PRINT 'Partners:';
SELECT Id, Name, IsActive FROM Partners ORDER BY Id;

PRINT 'Companies:';
SELECT Id, Name, Location, Country, IsActive FROM Companies ORDER BY Id;

PRINT 'Products:';
SELECT p.Id, p.Name, c.Name AS Company, p.IsActive
FROM Products p
LEFT JOIN Companies c ON p.CompanyId = c.Id
ORDER BY p.Id;

PRINT 'Certification Types (Hierarchy):';
SELECT ct.Id, ct.Name, ct.Level, p.Name AS ParentName, ct.AmazonCode
FROM CertificationTypes ct
LEFT JOIN CertificationTypes p ON ct.ParentId = p.Id
ORDER BY COALESCE(ct.ParentId, ct.Id), ct.Level, ct.SortOrder;

PRINT 'Tags:';
SELECT Id, Name, TagType, IsActive FROM Tags ORDER BY TagType, Id;

PRINT 'Certificates:';
SELECT c.Id, c.Number, c.Status, co.Name AS Company, p.Name AS Product, c.Partner,
       c.EffectiveDate, c.ExpirationDate
FROM Certificates c
JOIN Companies co ON c.CompanyId = co.Id
JOIN Products p ON c.ProductId = p.Id
ORDER BY c.Id;

PRINT 'Certificate Tags:';
SELECT c.Number, t.Name AS TagName, t.TagType
FROM CertificateTags ct
JOIN Certificates c ON ct.CertificatesId = c.Id
JOIN Tags t ON ct.TagsId = t.Id
ORDER BY c.Number, t.TagType;

PRINT 'Certificate Product IDs:';
SELECT c.Number, cp.ProductIdType, cp.ProductIdValue, cp.Status, cp.Partner
FROM CertificateProductIds cp
JOIN Certificates c ON cp.CertificateId = c.Id
ORDER BY c.Number, cp.ProductIdType;

PRINT 'Users:';
SELECT u.Id, u.Username, u.Email, u.IsActive, r.Name AS RoleName
FROM Users u
LEFT JOIN UserRoles ur ON u.Id = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.Id
ORDER BY u.Id;

PRINT 'User-Company Associations:';
SELECT u.Username, c.Name AS Company
FROM UserCompanies uc
JOIN Users u ON uc.UserId = u.Id
JOIN Companies c ON uc.CompaniesId = c.Id;

PRINT '';
PRINT '=== GCC UAT Test Data Setup Complete ===';
PRINT '';
PRINT 'SUMMARY OF TEST DATA:';
PRINT '  Partners:            3 (Amazon, Novi, Other)';
PRINT '  Companies:           4 (EcoProduct Co, GreenTech Industries, TestCo UAT, OtherCo)';
PRINT '  Product Categories:  2 (Consumer Goods, Industrial)';
PRINT '  Product Types:       3 (Paper Products, Cleaning Products, Water Treatment)';
PRINT '  Products:            6 (across 4 companies)';
PRINT '  Certification Types: 3 parents + 5 subcategories';
PRINT '  Custom Fields:       3 (Testing Lab, Compliance Score, Recycled Content %)';
PRINT '  Tags:                7 (4 external, 3 internal)';
PRINT '  Pathways:            2 (Standard, Express)';
PRINT '  Certificates:        8 (Active x3, Draft, Archived, Expired, PendingReview, Active-expiring)';
PRINT '  Product IDs:         4 (ASIN x2, UPC, GTIN)';
PRINT '';
PRINT 'E2E SCENARIO PRE-CONDITIONS MET:';
PRINT '  Scenario 1 (Full Lifecycle):     Companies + Products + CertTypes + Tags ready';
PRINT '  Scenario 2 (Company Onboarding): Run fresh (creates its own data)';
PRINT '  Scenario 3 (Partner API):        3 Active Amazon certs with product IDs ready';
PRINT '  Scenario 4 (Day 1 Setup):        Run fresh (creates its own data)';
PRINT '  Scenario 5 (Renewal):            Cert 26-0008 expiring in ~25 days ready';
PRINT '  Scenario 6 (Rejection):          Run fresh (creates its own data)';
PRINT '  Scenario 7 (Bulk Ops):           Templates available, import page accessible';
PRINT '  Scenario 8 (User Lifecycle):     Run fresh (creates its own users)';
PRINT '  Scenario 9 (Cross-Role Search):  Certs A-D ready with correct company/status/tags';
PRINT '';

SET NOCOUNT OFF;

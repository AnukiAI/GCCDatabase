-- ============================================================================
-- SETUP EXTERNAL TEST USERS + TEST CERTIFICATES FOR VISIBILITY TESTING
--
-- Creates 4 external test users with different company associations:
--   1. ext-amazon     / TestPass123!  → Company with Amazon partner certs
--   2. ext-novi       / TestPass123!  → Company with Novi partner certs
--   3. ext-nopartner  / TestPass123!  → Company with no partner certs
--   4. ext-none       / TestPass123!  → NO company association (should see 0)
--
-- Also creates test companies, products, and certificates for each scenario.
-- Idempotent - safe to run multiple times.
-- ============================================================================

PRINT '=== Setting Up External Test Users + Certificates for Visibility Testing ===';
PRINT '';

-- BCrypt hash for TestPass123! (same as other test users)
DECLARE @PasswordHash NVARCHAR(MAX) = '$2a$11$rBEMOGcoUhf.g7MiBYMh.eOMsXhXoJrXRomaYCEQhj.7Bq8xSJvMK';
DECLARE @Now DATETIME2 = GETUTCDATE();

-- Get the External role ID
DECLARE @ExternalRoleId INT;
SELECT @ExternalRoleId = Id FROM Roles WHERE Name = 'External';

IF @ExternalRoleId IS NULL
BEGIN
    PRINT 'ERROR: External role not found!';
    RETURN;
END
PRINT 'External Role ID: ' + CAST(@ExternalRoleId AS VARCHAR);

-- Get a valid CertificationType
DECLARE @CertTypeId INT;
SELECT TOP 1 @CertTypeId = Id FROM CertificationTypes WHERE IsActive = 1;
IF @CertTypeId IS NULL
BEGIN
    PRINT 'ERROR: No active CertificationType found!';
    RETURN;
END
PRINT 'Using CertificationType ID: ' + CAST(@CertTypeId AS VARCHAR);

-- ============================================================================
-- STEP 1: Create test companies
-- ============================================================================
PRINT '';
PRINT '--- Creating Test Companies ---';

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-AmazonCo')
    INSERT INTO Companies (Name, ContactInfo, Email, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest-AmazonCo', 'Amazon Test Contact', 'amazon@exttest.com', 'Seattle, WA', 'United States', 1, 0, @Now, @Now);
DECLARE @AmazonCoId INT = (SELECT Id FROM Companies WHERE Name = 'ExtTest-AmazonCo');
PRINT 'ExtTest-AmazonCo ID: ' + CAST(@AmazonCoId AS VARCHAR);

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-NoviCo')
    INSERT INTO Companies (Name, ContactInfo, Email, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest-NoviCo', 'Novi Test Contact', 'novi@exttest.com', 'Portland, OR', 'United States', 1, 0, @Now, @Now);
DECLARE @NoviCoId INT = (SELECT Id FROM Companies WHERE Name = 'ExtTest-NoviCo');
PRINT 'ExtTest-NoviCo ID: ' + CAST(@NoviCoId AS VARCHAR);

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-NoPartnerCo')
    INSERT INTO Companies (Name, ContactInfo, Email, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest-NoPartnerCo', 'No Partner Contact', 'nopartner@exttest.com', 'Denver, CO', 'United States', 1, 0, @Now, @Now);
DECLARE @NoPartnerCoId INT = (SELECT Id FROM Companies WHERE Name = 'ExtTest-NoPartnerCo');
PRINT 'ExtTest-NoPartnerCo ID: ' + CAST(@NoPartnerCoId AS VARCHAR);

-- ============================================================================
-- STEP 2: Create test products (one per company)
-- ============================================================================
PRINT '';
PRINT '--- Creating Test Products ---';

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ExtTest Amazon Product')
    INSERT INTO Products (Name, Description, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest Amazon Product', 'Test product for Amazon partner', @AmazonCoId, 1, 0, @Now, @Now);
DECLARE @AmazonProdId INT = (SELECT Id FROM Products WHERE Name = 'ExtTest Amazon Product');

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ExtTest Novi Product')
    INSERT INTO Products (Name, Description, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest Novi Product', 'Test product for Novi partner', @NoviCoId, 1, 0, @Now, @Now);
DECLARE @NoviProdId INT = (SELECT Id FROM Products WHERE Name = 'ExtTest Novi Product');

IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ExtTest NoPartner Product')
    INSERT INTO Products (Name, Description, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest NoPartner Product', 'Test product no partner', @NoPartnerCoId, 1, 0, @Now, @Now);
DECLARE @NoPartnerProdId INT = (SELECT Id FROM Products WHERE Name = 'ExtTest NoPartner Product');

PRINT 'Products created';

-- ============================================================================
-- STEP 3: Create test certificates
-- ============================================================================
PRINT '';
PRINT '--- Creating Test Certificates ---';

-- Amazon partner certs (3 active, 1 draft)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-001')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-001', 'active', DATEADD(MONTH, -6, @Now), DATEADD(MONTH, 6, @Now), 'Seattle, WA', 'United States', 'Amazon', @AmazonCoId, @AmazonProdId, @CertTypeId, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-002')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-002', 'active', DATEADD(MONTH, -3, @Now), DATEADD(MONTH, 9, @Now), 'Chicago, IL', 'United States', 'Amazon', @AmazonCoId, @AmazonProdId, @CertTypeId, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-003')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-003', 'active', DATEADD(MONTH, -1, @Now), DATEADD(MONTH, 11, @Now), 'New York, NY', 'United States', 'Amazon', @AmazonCoId, @AmazonProdId, @CertTypeId, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-DRAFT')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-DRAFT', 'draft', DATEADD(MONTH, -1, @Now), DATEADD(MONTH, 11, @Now), 'Boston, MA', 'United States', 'Amazon', @AmazonCoId, @AmazonProdId, @CertTypeId, @Now, @Now);

-- Novi partner certs (2 active, 1 expired)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NOVI-001')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NOVI-001', 'active', DATEADD(MONTH, -4, @Now), DATEADD(MONTH, 8, @Now), 'Portland, OR', 'United States', 'Novi', @NoviCoId, @NoviProdId, @CertTypeId, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NOVI-002')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NOVI-002', 'active', DATEADD(MONTH, -2, @Now), DATEADD(MONTH, 10, @Now), 'San Francisco, CA', 'United States', 'Novi', @NoviCoId, @NoviProdId, @CertTypeId, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NOVI-EXPIRED')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NOVI-EXPIRED', 'expired', DATEADD(MONTH, -12, @Now), DATEADD(MONTH, -1, @Now), 'Portland, OR', 'United States', 'Novi', @NoviCoId, @NoviProdId, @CertTypeId, @Now, @Now);

-- No partner certs (2 active)
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NP-001')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NP-001', 'active', DATEADD(MONTH, -5, @Now), DATEADD(MONTH, 7, @Now), 'Denver, CO', 'United States', 'None', @NoPartnerCoId, @NoPartnerProdId, @CertTypeId, @Now, @Now);

IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NP-002')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NP-002', 'active', DATEADD(MONTH, -3, @Now), DATEADD(MONTH, 9, @Now), 'Austin, TX', 'United States', NULL, @NoPartnerCoId, @NoPartnerProdId, @CertTypeId, @Now, @Now);

PRINT 'Certificates created (Amazon: 3 active + 1 draft, Novi: 2 active + 1 expired, NoPartner: 2 active)';

-- ============================================================================
-- STEP 4: Create external test users
-- ============================================================================
PRINT '';
PRINT '--- Creating External Test Users ---';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-amazon')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, CreatedAt, UpdatedAt, LoginAttempts, IsLocked)
    VALUES ('ext-amazon', 'ext-amazon@test.com', @PasswordHash, 'active', 1, @Now, @Now, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-novi')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, CreatedAt, UpdatedAt, LoginAttempts, IsLocked)
    VALUES ('ext-novi', 'ext-novi@test.com', @PasswordHash, 'active', 1, @Now, @Now, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-nopartner')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, CreatedAt, UpdatedAt, LoginAttempts, IsLocked)
    VALUES ('ext-nopartner', 'ext-nopartner@test.com', @PasswordHash, 'active', 1, @Now, @Now, 0, 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-none')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, CreatedAt, UpdatedAt, LoginAttempts, IsLocked)
    VALUES ('ext-none', 'ext-none@test.com', @PasswordHash, 'active', 1, @Now, @Now, 0, 0);

DECLARE @User1Id INT = (SELECT Id FROM Users WHERE Username = 'ext-amazon');
DECLARE @User2Id INT = (SELECT Id FROM Users WHERE Username = 'ext-novi');
DECLARE @User3Id INT = (SELECT Id FROM Users WHERE Username = 'ext-nopartner');
DECLARE @User4Id INT = (SELECT Id FROM Users WHERE Username = 'ext-none');

PRINT 'User IDs - ext-amazon: ' + CAST(@User1Id AS VARCHAR) + ', ext-novi: ' + CAST(@User2Id AS VARCHAR) + ', ext-nopartner: ' + CAST(@User3Id AS VARCHAR) + ', ext-none: ' + CAST(@User4Id AS VARCHAR);

-- ============================================================================
-- STEP 5: Assign External role
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @User1Id AND RoleId = @ExternalRoleId)
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@User1Id, @ExternalRoleId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @User2Id AND RoleId = @ExternalRoleId)
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@User2Id, @ExternalRoleId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @User3Id AND RoleId = @ExternalRoleId)
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@User3Id, @ExternalRoleId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @User4Id AND RoleId = @ExternalRoleId)
    INSERT INTO UserRoles (UserId, RoleId) VALUES (@User4Id, @ExternalRoleId);

PRINT 'Assigned External role to all 4 users';

-- ============================================================================
-- STEP 6: Associate users with companies (ext-none gets NO association)
-- ============================================================================
PRINT '';
PRINT '--- Associating Users with Companies ---';

IF NOT EXISTS (SELECT 1 FROM UserCompanies WHERE UserId = @User1Id AND CompaniesId = @AmazonCoId)
    INSERT INTO UserCompanies (UserId, CompaniesId) VALUES (@User1Id, @AmazonCoId);
PRINT 'ext-amazon → ExtTest-AmazonCo';

IF NOT EXISTS (SELECT 1 FROM UserCompanies WHERE UserId = @User2Id AND CompaniesId = @NoviCoId)
    INSERT INTO UserCompanies (UserId, CompaniesId) VALUES (@User2Id, @NoviCoId);
PRINT 'ext-novi → ExtTest-NoviCo';

IF NOT EXISTS (SELECT 1 FROM UserCompanies WHERE UserId = @User3Id AND CompaniesId = @NoPartnerCoId)
    INSERT INTO UserCompanies (UserId, CompaniesId) VALUES (@User3Id, @NoPartnerCoId);
PRINT 'ext-nopartner → ExtTest-NoPartnerCo';

PRINT 'ext-none → NO company (should see 0 certificates)';

-- ============================================================================
-- VERIFICATION
-- ============================================================================
PRINT '';
PRINT '=== VERIFICATION ===';

SELECT
    u.Username,
    u.Email,
    r.Name AS [Role],
    co.Name AS [Company],
    (SELECT COUNT(*) FROM Certificates c WHERE c.CompanyId = co.Id AND c.Status = 'active') AS [ActiveCerts],
    (SELECT COUNT(*) FROM Certificates c WHERE c.CompanyId = co.Id) AS [TotalCerts]
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UserId
JOIN Roles r ON ur.RoleId = r.Id
LEFT JOIN UserCompanies uc ON u.Id = uc.UserId
LEFT JOIN Companies co ON uc.CompaniesId = co.Id
WHERE u.Username IN ('ext-amazon', 'ext-novi', 'ext-nopartner', 'ext-none')
ORDER BY u.Username;

SELECT
    c.Number, c.Status, c.Partner, co.Name AS Company
FROM Certificates c
JOIN Companies co ON c.CompanyId = co.Id
WHERE c.Number LIKE 'EXT-%'
ORDER BY c.Number;

PRINT '';
PRINT '=== TEST CREDENTIALS (all use TestPass123!) ===';
PRINT 'ext-amazon     → ExtTest-AmazonCo  → Should see 3 active Amazon partner certs';
PRINT 'ext-novi       → ExtTest-NoviCo     → Should see 2 active Novi partner certs';
PRINT 'ext-nopartner  → ExtTest-NoPartnerCo → Should see 2 active non-partner certs';
PRINT 'ext-none       → NO company          → Should see 0 certificates';
PRINT '';
PRINT '=== SETUP COMPLETE ===';

DECLARE @PasswordHash NVARCHAR(MAX);
SELECT @PasswordHash = PasswordHash FROM Users WHERE Username = 'admin';
DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @ExternalRoleId INT;
SELECT @ExternalRoleId = Id FROM Roles WHERE Name = 'External';

-- Create users
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-amazon')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, IsDeleted, IsStaff, IsSuperuser, RequirePasswordReset, FailedLoginAttempts, TwoFactorEnabled, TwoFactorMethod, IsPhoneVerified, IsEmailVerified, MustChangePassword, MaxConcurrentSessions, RequirePasswordChange, MaxPasswordHistory, CreatedAt, UpdatedAt)
    VALUES ('ext-amazon', 'ext-amazon@test.com', @PasswordHash, 'active', 1, 0, 0, 0, 0, 0, 0, 'Email', 0, 0, 0, 1, 0, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-novi')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, IsDeleted, IsStaff, IsSuperuser, RequirePasswordReset, FailedLoginAttempts, TwoFactorEnabled, TwoFactorMethod, IsPhoneVerified, IsEmailVerified, MustChangePassword, MaxConcurrentSessions, RequirePasswordChange, MaxPasswordHistory, CreatedAt, UpdatedAt)
    VALUES ('ext-novi', 'ext-novi@test.com', @PasswordHash, 'active', 1, 0, 0, 0, 0, 0, 0, 'Email', 0, 0, 0, 1, 0, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-nopartner')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, IsDeleted, IsStaff, IsSuperuser, RequirePasswordReset, FailedLoginAttempts, TwoFactorEnabled, TwoFactorMethod, IsPhoneVerified, IsEmailVerified, MustChangePassword, MaxConcurrentSessions, RequirePasswordChange, MaxPasswordHistory, CreatedAt, UpdatedAt)
    VALUES ('ext-nopartner', 'ext-nopartner@test.com', @PasswordHash, 'active', 1, 0, 0, 0, 0, 0, 0, 'Email', 0, 0, 0, 1, 0, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-none')
    INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, IsDeleted, IsStaff, IsSuperuser, RequirePasswordReset, FailedLoginAttempts, TwoFactorEnabled, TwoFactorMethod, IsPhoneVerified, IsEmailVerified, MustChangePassword, MaxConcurrentSessions, RequirePasswordChange, MaxPasswordHistory, CreatedAt, UpdatedAt)
    VALUES ('ext-none', 'ext-none@test.com', @PasswordHash, 'active', 1, 0, 0, 0, 0, 0, 0, 'Email', 0, 0, 0, 1, 0, 0, @Now, @Now);

DECLARE @U1 INT = (SELECT Id FROM Users WHERE Username = 'ext-amazon');
DECLARE @U2 INT = (SELECT Id FROM Users WHERE Username = 'ext-novi');
DECLARE @U3 INT = (SELECT Id FROM Users WHERE Username = 'ext-nopartner');
DECLARE @U4 INT = (SELECT Id FROM Users WHERE Username = 'ext-none');

-- Assign External role
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UsersId = @U1 AND RolesId = @ExternalRoleId)
    INSERT INTO UserRoles (UsersId, RolesId) VALUES (@U1, @ExternalRoleId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UsersId = @U2 AND RolesId = @ExternalRoleId)
    INSERT INTO UserRoles (UsersId, RolesId) VALUES (@U2, @ExternalRoleId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UsersId = @U3 AND RolesId = @ExternalRoleId)
    INSERT INTO UserRoles (UsersId, RolesId) VALUES (@U3, @ExternalRoleId);
IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UsersId = @U4 AND RolesId = @ExternalRoleId)
    INSERT INTO UserRoles (UsersId, RolesId) VALUES (@U4, @ExternalRoleId);

-- Companies
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-AmazonCo')
    INSERT INTO Companies (Name, ContactInfo, Email, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest-AmazonCo', 'Amazon Test Contact', 'amazon@exttest.com', 'Seattle, WA', 'United States', 1, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-NoviCo')
    INSERT INTO Companies (Name, ContactInfo, Email, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest-NoviCo', 'Novi Test Contact', 'novi@exttest.com', 'Portland, OR', 'United States', 1, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-NoPartnerCo')
    INSERT INTO Companies (Name, ContactInfo, Email, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest-NoPartnerCo', 'No Partner Contact', 'nopartner@exttest.com', 'Denver, CO', 'United States', 1, 0, @Now, @Now);

DECLARE @AC INT = (SELECT Id FROM Companies WHERE Name = 'ExtTest-AmazonCo');
DECLARE @NC INT = (SELECT Id FROM Companies WHERE Name = 'ExtTest-NoviCo');
DECLARE @NPC INT = (SELECT Id FROM Companies WHERE Name = 'ExtTest-NoPartnerCo');

-- User-Company associations
IF NOT EXISTS (SELECT 1 FROM CompanyUser WHERE UsersId = @U1 AND CompaniesId = @AC)
    INSERT INTO CompanyUser (UsersId, CompaniesId) VALUES (@U1, @AC);
IF NOT EXISTS (SELECT 1 FROM CompanyUser WHERE UsersId = @U2 AND CompaniesId = @NC)
    INSERT INTO CompanyUser (UsersId, CompaniesId) VALUES (@U2, @NC);
IF NOT EXISTS (SELECT 1 FROM CompanyUser WHERE UsersId = @U3 AND CompaniesId = @NPC)
    INSERT INTO CompanyUser (UsersId, CompaniesId) VALUES (@U3, @NPC);

-- Products
DECLARE @CatId INT;
SELECT TOP 1 @CatId = Id FROM ProductCategories WHERE IsActive = 1;
IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ExtTest Amazon Product')
    INSERT INTO Products (Name, Description, CompanyId, CategoryId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest Amazon Product', 'Test product for Amazon partner', @AC, @CatId, 1, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ExtTest Novi Product')
    INSERT INTO Products (Name, Description, CompanyId, CategoryId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest Novi Product', 'Test product for Novi partner', @NC, @CatId, 1, 0, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'ExtTest NoPartner Product')
    INSERT INTO Products (Name, Description, CompanyId, CategoryId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('ExtTest NoPartner Product', 'Test product no partner', @NPC, @CatId, 1, 0, @Now, @Now);

DECLARE @AP INT = (SELECT Id FROM Products WHERE Name = 'ExtTest Amazon Product');
DECLARE @NP INT = (SELECT Id FROM Products WHERE Name = 'ExtTest Novi Product');
DECLARE @NPP INT = (SELECT Id FROM Products WHERE Name = 'ExtTest NoPartner Product');
DECLARE @CT INT;
SELECT TOP 1 @CT = Id FROM CertificationTypes WHERE IsActive = 1;

-- Certificates
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-001')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-001', 'active', DATEADD(MONTH,-6,@Now), DATEADD(MONTH,6,@Now), 'Seattle, WA', 'United States', 'Amazon', @AC, @AP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-002')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-002', 'active', DATEADD(MONTH,-3,@Now), DATEADD(MONTH,9,@Now), 'Chicago, IL', 'United States', 'Amazon', @AC, @AP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-003')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-003', 'active', DATEADD(MONTH,-1,@Now), DATEADD(MONTH,11,@Now), 'New York, NY', 'United States', 'Amazon', @AC, @AP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-AMZ-DRAFT')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-AMZ-DRAFT', 'draft', DATEADD(MONTH,-1,@Now), DATEADD(MONTH,11,@Now), 'Boston, MA', 'United States', 'Amazon', @AC, @AP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NOVI-001')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NOVI-001', 'active', DATEADD(MONTH,-4,@Now), DATEADD(MONTH,8,@Now), 'Portland, OR', 'United States', 'Novi', @NC, @NP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NOVI-002')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NOVI-002', 'active', DATEADD(MONTH,-2,@Now), DATEADD(MONTH,10,@Now), 'San Francisco, CA', 'United States', 'Novi', @NC, @NP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NOVI-EXPIRED')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NOVI-EXPIRED', 'expired', DATEADD(MONTH,-12,@Now), DATEADD(MONTH,-1,@Now), 'Portland, OR', 'United States', 'Novi', @NC, @NP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NP-001')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NP-001', 'active', DATEADD(MONTH,-5,@Now), DATEADD(MONTH,7,@Now), 'Denver, CO', 'United States', 'None', @NPC, @NPP, @CT, @Now, @Now);
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Number = 'EXT-NP-002')
    INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country, Partner, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt)
    VALUES ('EXT-NP-002', 'active', DATEADD(MONTH,-3,@Now), DATEADD(MONTH,9,@Now), 'Austin, TX', 'United States', NULL, @NPC, @NPP, @CT, @Now, @Now);

-- Verify
PRINT 'Users:';
SELECT u.Username, r.Name AS Role, c.Name AS Company
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UsersId
JOIN Roles r ON ur.RolesId = r.Id
LEFT JOIN CompanyUser cu ON u.Id = cu.UsersId
LEFT JOIN Companies c ON cu.CompaniesId = c.Id
WHERE u.Username LIKE 'ext-%'
ORDER BY u.Username;

PRINT 'Certificates:';
SELECT Number, Status, Partner FROM Certificates WHERE Number LIKE 'EXT-%' ORDER BY Number;

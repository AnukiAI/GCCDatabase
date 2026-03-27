-- Add missing external test accounts to Prod DB
-- Safe: checks for existing records before inserting
-- Password: TestPass123! (same bcrypt hash as Dev)
-- Date: 2026-03-26

BEGIN TRANSACTION;

-- 1. Create test companies
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-AmazonCo')
INSERT INTO Companies (Name, Country, Email, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ExtTest-AmazonCo', 'United States', 'amazon@exttest.com', 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-NoviCo')
INSERT INTO Companies (Name, Country, Email, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ExtTest-NoviCo', 'United States', 'novi@exttest.com', 1, 0, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Name = 'ExtTest-NoPartnerCo')
INSERT INTO Companies (Name, Country, Email, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ExtTest-NoPartnerCo', 'United States', 'nopartner@exttest.com', 1, 0, GETUTCDATE(), GETUTCDATE());

-- 2. Create test users (password = TestPass123!)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-amazon')
INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, TwoFactorMethod, CreatedAt, UpdatedAt)
VALUES ('ext-amazon', 'ext-amazon@test.com',
  '$2a$11$iZ.Z.LUeIw4WPrxWRAnMgOkFVvKRoFrP2gdLPhiCljfSMFZ0MaNmm',
  'active', 1, 'Email', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-novi')
INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, TwoFactorMethod, CreatedAt, UpdatedAt)
VALUES ('ext-novi', 'ext-novi@test.com',
  '$2a$11$iZ.Z.LUeIw4WPrxWRAnMgOkFVvKRoFrP2gdLPhiCljfSMFZ0MaNmm',
  'active', 1, 'Email', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-nopartner')
INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, TwoFactorMethod, CreatedAt, UpdatedAt)
VALUES ('ext-nopartner', 'ext-nopartner@test.com',
  '$2a$11$iZ.Z.LUeIw4WPrxWRAnMgOkFVvKRoFrP2gdLPhiCljfSMFZ0MaNmm',
  'active', 1, 'Email', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'ext-none')
INSERT INTO Users (Username, Email, PasswordHash, Status, IsActive, TwoFactorMethod, CreatedAt, UpdatedAt)
VALUES ('ext-none', 'ext-none@test.com',
  '$2a$11$iZ.Z.LUeIw4WPrxWRAnMgOkFVvKRoFrP2gdLPhiCljfSMFZ0MaNmm',
  'active', 1, 'Email', GETUTCDATE(), GETUTCDATE());

-- 3. Assign External role (RoleId = 3) to each test user
INSERT INTO UserRoles (UsersId, RolesId)
SELECT u.Id, 3 FROM Users u
WHERE u.Username IN ('ext-amazon','ext-novi','ext-none','ext-nopartner')
  AND NOT EXISTS (SELECT 1 FROM UserRoles ur WHERE ur.UsersId = u.Id AND ur.RolesId = 3);

-- 4. Link users to their test companies
INSERT INTO CompanyUser (UsersId, CompaniesId)
SELECT u.Id, c.Id FROM Users u, Companies c
WHERE u.Username = 'ext-amazon' AND c.Name = 'ExtTest-AmazonCo'
  AND NOT EXISTS (SELECT 1 FROM CompanyUser cu WHERE cu.UsersId = u.Id AND cu.CompaniesId = c.Id);

INSERT INTO CompanyUser (UsersId, CompaniesId)
SELECT u.Id, c.Id FROM Users u, Companies c
WHERE u.Username = 'ext-novi' AND c.Name = 'ExtTest-NoviCo'
  AND NOT EXISTS (SELECT 1 FROM CompanyUser cu WHERE cu.UsersId = u.Id AND cu.CompaniesId = c.Id);

INSERT INTO CompanyUser (UsersId, CompaniesId)
SELECT u.Id, c.Id FROM Users u, Companies c
WHERE u.Username = 'ext-nopartner' AND c.Name = 'ExtTest-NoPartnerCo'
  AND NOT EXISTS (SELECT 1 FROM CompanyUser cu WHERE cu.UsersId = u.Id AND cu.CompaniesId = c.Id);

-- ext-none intentionally has NO company association

COMMIT;

-- 5. Verify
SELECT '=== Inserted Users ===' AS [Check];
SELECT u.Username, u.Email, r.Name AS Role, c.Name AS Company
FROM Users u
JOIN UserRoles ur ON u.Id = ur.UsersId
JOIN Roles r ON ur.RolesId = r.Id
LEFT JOIN CompanyUser cu ON u.Id = cu.UsersId
LEFT JOIN Companies c ON cu.CompaniesId = c.Id
WHERE u.Username IN ('ext-amazon','ext-novi','ext-none','ext-nopartner')
ORDER BY u.Username;

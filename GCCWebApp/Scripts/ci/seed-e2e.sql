-- CI E2E business-data seed (Phase 5, docs/qa/regression-e2e-strategy.md §8).
--
-- Runs AFTER the app boots in a fresh CI DB (schema auto-migrated, SeedData has seeded login users
-- admin/analyst/external + the Roles incl. 'Partner'). SeedData/migrations do NOT create Partners,
-- ProductCategories, CertificationTypes, Companies, Products, or Certificates — so this seed is
-- SELF-SUFFICIENT and creates everything the deploy-gate security + CRUD gates need.
-- (Amazon contract-diff stays a LOCAL gate — it needs the frozen real certs, not reproduced here.)
--
-- All data is synthetic + fixture-marked:
--   Users:      partner@amazon.com, perry.novi@…, pam.wayfair@…, pip.other@…, pru.multi@…  (role Partner)
--   Partners:   Amazon / Novi / Other / Wayfair  (created by NAME if absent — never assumes ids 1-4)
--   Companies:  ZZ_CI_ExternalCo, ZZ_CI_OtherCo   ·  Products: ZZ_CI_Product_*
--   Certs:      98-9001..98-9104 (98- prefix avoids collision with real YY-NNNN data)
-- Partner passwords reuse SeedData's `admin` PasswordHash (TestPass123!) — no hardcoded credential.
--
-- Idempotent + transactional. Run:
--   sqlcmd -C -S <server> -U <user> -P <pw> -d <db> -i Scripts/ci/seed-e2e.sql

SET QUOTED_IDENTIFIER ON;   -- required: tables have filtered indexes / computed columns
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @effRecent datetime2 = DATEADD(day, -30, @now);
DECLARE @expFuture datetime2 = DATEADD(year,  2, @now);

DECLARE @emails TABLE (Email nvarchar(256), FullName nvarchar(256));
INSERT INTO @emails VALUES
 ('partner@amazon.com',      'CI Amazon Partner'),
 ('perry.novi@example.com',  'CI Novi Partner'),
 ('pam.wayfair@example.com', 'CI Wayfair Partner'),
 ('pip.other@example.com',   'CI Other Partner'),
 ('pru.multi@example.com',   'CI Multi Partner');

-- ===================== cleanup any prior run (FK-safe order) =====================
DELETE cp FROM CertificatePartners cp JOIN Certificates c ON c.Id = cp.CertificatesId WHERE c.Number LIKE '98-9%';
IF OBJECT_ID('CertificateStatusHistory') IS NOT NULL
  DELETE FROM CertificateStatusHistory WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Number LIKE '98-9%');
DELETE FROM Certificates WHERE Number LIKE '98-9%';
DELETE FROM Products  WHERE Name LIKE 'ZZ_CI_%';
DELETE cu FROM CompanyUser cu JOIN Companies co ON co.Id = cu.CompaniesId WHERE co.Name LIKE 'ZZ_CI_%';
DELETE FROM Companies WHERE Name LIKE 'ZZ_CI_%';
DELETE pu FROM PartnerUser pu JOIN Users u ON u.Id = pu.UsersId WHERE u.Email IN (SELECT Email FROM @emails);
DELETE ur FROM UserRoles ur   JOIN Users u ON u.Id = ur.UsersId WHERE u.Email IN (SELECT Email FROM @emails);
DELETE FROM Users WHERE Email IN (SELECT Email FROM @emails);

-- ===================== prerequisites from SeedData =====================
DECLARE @pwHash        nvarchar(500) = (SELECT TOP 1 PasswordHash FROM Users WHERE Username = 'admin');
DECLARE @partnerRoleId int           = (SELECT TOP 1 Id FROM Roles WHERE Name = 'Partner');
DECLARE @extUserId     int           = (SELECT TOP 1 Id FROM Users WHERE Username = 'external');
IF @pwHash IS NULL OR @partnerRoleId IS NULL OR @extUserId IS NULL
BEGIN
  RAISERROR('seed-e2e: prerequisite missing (admin pw / Partner role / external user). Did SeedData run?', 16, 1);
  ROLLBACK TRAN; RETURN;
END

-- ===================== reference data (create if absent, by name) =====================
IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name='Amazon')  INSERT INTO Partners (Name,IsActive,IsDeleted,SortOrder,CreatedAt,UpdatedAt) VALUES ('Amazon', 1,0,1,@now,@now);
IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name='Novi')    INSERT INTO Partners (Name,IsActive,IsDeleted,SortOrder,CreatedAt,UpdatedAt) VALUES ('Novi',   1,0,2,@now,@now);
IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name='Other')   INSERT INTO Partners (Name,IsActive,IsDeleted,SortOrder,CreatedAt,UpdatedAt) VALUES ('Other',  1,0,3,@now,@now);
IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name='Wayfair') INSERT INTO Partners (Name,IsActive,IsDeleted,SortOrder,CreatedAt,UpdatedAt) VALUES ('Wayfair',1,0,4,@now,@now);

IF NOT EXISTS (SELECT 1 FROM ProductCategories  WHERE IsActive=1) INSERT INTO ProductCategories  (Name,SortOrder,IsActive)       VALUES ('ZZ_CI_Category', 1, 1);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE IsActive=1) INSERT INTO CertificationTypes (Name,IsActive,SortOrder,Level) VALUES ('ZZ_CI_CertType', 1, 1, 1);

DECLARE @catId      int = (SELECT TOP 1 Id FROM ProductCategories  WHERE IsActive=1 ORDER BY Id);
DECLARE @certTypeId int = (SELECT TOP 1 Id FROM CertificationTypes WHERE IsActive=1 ORDER BY Id);

-- ===================== partner login users =====================
INSERT INTO Users
  (Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod,
   IsActive, IsDeleted, IsStaff, IsSuperuser, RequirePasswordReset, TwoFactorEnabled,
   IsPhoneVerified, IsEmailVerified, MustChangePassword, RequirePasswordChange,
   FailedLoginAttempts, MaxConcurrentSessions, MaxPasswordHistory, CreatedAt, UpdatedAt)
SELECT e.Email, e.Email, NULL, e.FullName, @pwHash, 'active', 'Email',
       1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 3, 5, @now, @now
FROM @emails e;

INSERT INTO UserRoles (RolesId, UsersId)
SELECT @partnerRoleId, u.Id FROM Users u WHERE u.Email IN (SELECT Email FROM @emails);

-- partner associations (map by NAME; pru.multi -> Amazon + Novi)
INSERT INTO PartnerUser (PartnersId, UsersId)
SELECT p.Id, u.Id
FROM Users u
JOIN (VALUES
  ('partner@amazon.com',      'Amazon'),
  ('perry.novi@example.com',  'Novi'),
  ('pam.wayfair@example.com', 'Wayfair'),
  ('pip.other@example.com',   'Other'),
  ('pru.multi@example.com',   'Amazon'),
  ('pru.multi@example.com',   'Novi')
) m(email, pname) ON u.Email = m.email
JOIN Partners p ON p.Name = m.pname;

-- ===================== companies + external-user association =====================
INSERT INTO Companies (Name, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES ('ZZ_CI_ExternalCo', 1, 0, @now, @now);
DECLARE @extCo int = SCOPE_IDENTITY();
INSERT INTO Companies (Name, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES ('ZZ_CI_OtherCo', 1, 0, @now, @now);
DECLARE @otherCo int = SCOPE_IDENTITY();

IF NOT EXISTS (SELECT 1 FROM CompanyUser WHERE CompaniesId = @extCo AND UsersId = @extUserId)
  INSERT INTO CompanyUser (CompaniesId, UsersId) VALUES (@extCo, @extUserId);

-- ===================== products (one per company) =====================
INSERT INTO Products (Name, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ZZ_CI_Product_Ext', @catId, @extCo, 1, 0, @now, @now);
DECLARE @extProd int = SCOPE_IDENTITY();
INSERT INTO Products (Name, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ZZ_CI_Product_Other', @catId, @otherCo, 1, 0, @now, @now);
DECLARE @otherProd int = SCOPE_IDENTITY();

-- ===================== certificates =====================
-- Paid+future / Draft+future so the per-minute expiration sweep never mutates them.
INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt) VALUES
 ('98-9001', 'Client Approved - Paid', @effRecent, @expFuture, @extCo,   @extProd,   @certTypeId, @now, @now), -- external OWN public (detail A1/A4, idor)
 ('98-9002', 'Client Approved - Paid', @effRecent, @expFuture, @otherCo, @otherProd, @certTypeId, @now, @now), -- OTHER company public (external must 404 — detail A3)
 ('98-9003', 'Draft',                  @effRecent, @expFuture, @otherCo, @otherProd, @certTypeId, @now, @now), -- non-public (anon must 404 — detail A6)
 ('98-9101', 'Client Approved - Paid', @effRecent, @expFuture, @otherCo, @otherProd, @certTypeId, @now, @now), -- Amazon partner cert
 ('98-9102', 'Client Approved - Paid', @effRecent, @expFuture, @otherCo, @otherProd, @certTypeId, @now, @now), -- Novi
 ('98-9103', 'Client Approved - Paid', @effRecent, @expFuture, @otherCo, @otherProd, @certTypeId, @now, @now), -- Wayfair
 ('98-9104', 'Client Approved - Paid', @effRecent, @expFuture, @otherCo, @otherProd, @certTypeId, @now, @now); -- Other

-- tag partner certs (CertificatePartners M2M, by partner NAME) so each partner sees ONLY their own
INSERT INTO CertificatePartners (CertificatesId, PartnersId)
SELECT c.Id, p.Id
FROM Certificates c
JOIN (VALUES ('98-9101','Amazon'), ('98-9102','Novi'), ('98-9103','Wayfair'), ('98-9104','Other')) m(num, pname) ON c.Number = m.num
JOIN Partners p ON p.Name = m.pname;

COMMIT;

SELECT 'seed-e2e applied' AS Result,
       (SELECT COUNT(*) FROM Users WHERE Email IN (SELECT Email FROM @emails)) AS PartnerUsers,
       (SELECT COUNT(*) FROM Certificates WHERE Number LIKE '98-9%')           AS Certs,
       (SELECT COUNT(*) FROM CertificatePartners cp JOIN Certificates c ON c.Id=cp.CertificatesId WHERE c.Number LIKE '98-91%') AS PartnerTags,
       @extCo AS ExtCompanyId, @otherCo AS OtherCompanyId;

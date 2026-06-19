-- E2E Smoke Tier 2 — cert workflow-state visibility fixtures.
--
-- Seeds 9 certificates that exercise the per-view state-visibility matrix:
--   - 8 under the EXTERNAL test user's own company (Henkel, id 53) — one per state.
--   - 1 "Client Approved - Paid" under a NEW isolated company ZZ_SmokeFixture_OtherCo
--     that the external user is NOT associated with (tests other-company exclusion).
--
-- Cert numbers 99-9001..99-9009 (the 99- prefix won't collide with real YY-NNNN data and
-- still satisfies the YY-NNNN format the views expect, so each is searchable via ?search=).
--
-- DATE STRATEGY (critical): the per-minute CertificateExpirationService sweep flips any cert
-- with ExpirationDate < today whose status is NOT already Expired/ExpiredNoAnnual to "Expired".
-- So the 6 non-expired-status fixtures get a FUTURE expiration (sweep skips on date); the 2
-- already-expired-status fixtures get a PAST expiration (sweep skips on status). Nothing the
-- sweep can mutate.
--
-- Idempotent: re-running first removes any prior 99-90xx fixtures + ZZ_SmokeFixture rows.
-- Run:  sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P <pw> -C -i seed-state-fixtures.sql
-- (Dev server runs Development env → GCCDatabase_Dev.)

SET QUOTED_IDENTIFIER ON;   -- required: tables have filtered indexes / computed columns
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- ===== cleanup any prior run (FK-safe order) =====
IF OBJECT_ID('CertificateStatusHistory') IS NOT NULL
    DELETE FROM CertificateStatusHistory WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Number LIKE '99-90%');
DELETE FROM Certificates WHERE Number LIKE '99-90%';
DELETE FROM Products     WHERE Name LIKE 'ZZ_SmokeFixture%';
DELETE FROM Companies    WHERE Name LIKE 'ZZ_SmokeFixture%';

-- The external test user's OWN company, derived from the actual CompanyUser association
-- (the smoke logs in as username 'external'). Was hardcoded 53 ("Henkel"), but ids drift:
-- 53 is now "Domtar" and the 'external' user maps to KAO. Hardcoding broke the own-company
-- visibility expectations (fixtures landed under a company the user doesn't belong to, so
-- External correctly hid them). Derive it so the matrix stays valid as data shifts.
DECLARE @ownCompanyId int = (
    SELECT TOP 1 cu.CompaniesId
    FROM CompanyUser cu
    JOIN Users u ON u.Id = cu.UsersId
    WHERE u.Username = 'external'
    ORDER BY cu.CompaniesId);
-- Pick any active CertificationType that actually exists in THIS db. (Was hardcoded id 1;
-- ids 1-2 have since been removed from the dev db, so a literal breaks the FK. The matrix
-- tests visibility by status/company/date, not by cert type, so any valid active id works.)
DECLARE @certTypeId   int = (SELECT TOP 1 Id FROM CertificationTypes WHERE IsActive = 1 ORDER BY Id);
DECLARE @catId        int = (SELECT TOP 1 Id FROM ProductCategories WHERE IsActive = 1 ORDER BY Id);

DECLARE @now       datetime2 = SYSUTCDATETIME();
DECLARE @effRecent datetime2 = DATEADD(day,  -30, @now);
DECLARE @expFuture datetime2 = DATEADD(year,   2, @now);  -- non-expired states
DECLARE @effOld    datetime2 = DATEADD(year,  -1, @now);
DECLARE @expPast   datetime2 = DATEADD(day,  -10, @now);  -- already-expired states

-- ===== isolated "other" company (external user NOT associated) =====
INSERT INTO Companies (Name, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ZZ_SmokeFixture_OtherCo', 1, 0, @now, @now);
DECLARE @otherCompanyId int = SCOPE_IDENTITY();

-- ===== fixture products (one per company) =====
INSERT INTO Products (Name, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ZZ_SmokeFixture_Product_Own', @catId, @ownCompanyId, 1, 0, @now, @now);
DECLARE @ownProductId int = SCOPE_IDENTITY();

INSERT INTO Products (Name, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
VALUES ('ZZ_SmokeFixture_Product_Other', @catId, @otherCompanyId, 1, 0, @now, @now);
DECLARE @otherProductId int = SCOPE_IDENTITY();

-- ===== 8 state fixtures under own company (Henkel) =====
INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt) VALUES
 ('99-9001', 'Draft',                            @effRecent, @expFuture, @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9002', 'Client Approved - Not Paid',       @effRecent, @expFuture, @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9003', 'Client Approved - Confidential',   @effRecent, @expFuture, @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9004', 'Client Approved - Paid',           @effRecent, @expFuture, @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9005', 'Hold',                             @effRecent, @expFuture, @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9006', 'Expired',                          @effOld,    @expPast,   @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9007', 'Republished',                      @effRecent, @expFuture, @ownCompanyId, @ownProductId, @certTypeId, @now, @now),
 ('99-9008', 'Expired - No Annual Verification', @effOld,    @expPast,   @ownCompanyId, @ownProductId, @certTypeId, @now, @now);

-- ===== other-company Paid fixture (external must NOT see it) =====
INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, CompanyId, ProductId, CertificationTypeId, CreatedAt, UpdatedAt) VALUES
 ('99-9009', 'Client Approved - Paid',           @effRecent, @expFuture, @otherCompanyId, @otherProductId, @certTypeId, @now, @now);

COMMIT;
SELECT 'seeded' AS Result,
       (SELECT COUNT(*) FROM Certificates WHERE Number LIKE '99-90%') AS FixtureCerts,
       @ownCompanyId AS OwnCompanyId,
       @otherCompanyId AS OtherCompanyId,
       @catId AS CategoryId;

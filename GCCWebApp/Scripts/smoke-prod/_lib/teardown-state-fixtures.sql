-- E2E Smoke Tier 2 — remove all cert workflow-state fixtures seeded by
-- seed-state-fixtures.sql. FK-safe order. Idempotent.
--
-- Run:  sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P <pw> -C -i teardown-state-fixtures.sql

SET QUOTED_IDENTIFIER ON;   -- required: tables have filtered indexes / computed columns
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID('CertificateStatusHistory') IS NOT NULL
    DELETE FROM CertificateStatusHistory WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Number LIKE '99-90%');
DELETE FROM Certificates WHERE Number LIKE '99-90%';
DELETE FROM Products     WHERE Name LIKE 'ZZ_SmokeFixture%';
DELETE FROM Companies    WHERE Name LIKE 'ZZ_SmokeFixture%';

COMMIT;
SELECT 'torn down' AS Result,
       (SELECT COUNT(*) FROM Certificates WHERE Number LIKE '99-90%') AS RemainingFixtureCerts;

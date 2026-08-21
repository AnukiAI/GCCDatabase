/*==============================================================================
  GO-LIVE STEP 1 — purge test data ahead of the real cert migration
  ------------------------------------------------------------------------------
  Client instruction (ReadMe.txt, 2026-08-12):
      "All test certs/ASINs should be removed before uploading as we are
       preparing for go-live."
      Remove: certs, companies, products, fake users (except key test accounts)
      DO NOT remove: Cert Types, Product Cat/Type/Subcat, Tags, Pathways, Partners

  Shawn's additional keeps (2026-08-13): PartnerApiKeys, ApiKeys,
  GoogleAnalyticsSettings, and the GCC admin account.

  RUN WITH sqlcmd -I  (QUOTED_IDENTIFIER ON) — the CR-187 persisted computed
  columns make statements fail otherwise. See [[reference_testenv_sql_delete_gotchas]].

      Report only:  sqlcmd -S <srv> -d <db> -I -v Execute=0 -i <thisfile>
      Execute:      sqlcmd -S <srv> -d <db> -I -v Execute=1 -i <thisfile>

  -v Execute is REQUIRED and has no in-script default, on purpose. An earlier
  revision carried ":setvar Execute 0" as a safety default -- but sqlcmd applies
  :setvar AFTER -v, so the script silently ignored "-v Execute=1" and reported
  when it was told to delete. A missing -v now fails loudly instead of guessing.

  ONE script rather than the usual IDENTIFY/REMEDIATE pair, deliberately: the
  report and the delete must share the same keep-list and the same WHERE clauses.
  Two files drift, and a drifted purge deletes the wrong rows.

  ------------------------------------------------------------------------------
  DELETE ORDER IS NOT FREE CHOICE. Certificates cascades to its 7 children, but
  AuditHistory.ChangedBy, Certificates.CreatedBy/UpdatedBy,
  CertificateStatusHistory, CertificateProductIds and GoogleAnalyticsSettings.UpdatedBy
  all reference Users with NO_ACTION. Deleting users first FAILS. Required order:

      Certificates -> Products -> Companies -> AuditHistory -> ImportBatches -> Users

  Everything runs in ONE transaction: a partial purge leaves the DB in a state
  no rollback plan covers.
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Execute bit = CAST('$(Execute)' AS bit);

/*------------------------------------------------------------------ keep-list --
  Users that survive the purge. Edit HERE, nowhere else.

  swest@anukiconsulting.com is MANDATORY, not a preference: it is
  Audit:SystemUserEmail in appsettings.json. If it is missing on the target,
  every audited write fails — the Gate 10 / DEF-337 failure mode.

  REVISED 2026-08-17 — three client-side accounts added. The earlier two-entry
  list would have deleted the CLIENT'S OWN ADMIN. Enumerating all 86 accounts
  the purge removes (not sampling them) showed 22 already soft-deleted, 64 live,
  and only 6 ever logged in — three of those six belonging to the client, not to
  us. A deleted account cannot be recovered by the client; we would have to
  recreate it with a new password.

  A keep-list errs safe in one direction only: keeping an account that could have
  been deleted is reversible in seconds, deleting one that was needed is not.
  When in doubt, add the address.

  Everything NOT listed here is our own test scaffolding — @anukiconsulting.com
  seeds, @example.com import-validation fixtures, qc108-*@gcc-test.local rows,
  and never-used duplicate carly@ accounts on throwaway domains (gcc5/6/7.com,
  admin.com, gcccc.com). Verified against the live test database 2026-08-17.
-------------------------------------------------------------------------------*/
DECLARE @KeepUsers TABLE (Email nvarchar(256) PRIMARY KEY);
INSERT INTO @KeepUsers (Email) VALUES
    (N'swest@anukiconsulting.com'),          -- MANDATORY: Audit:SystemUserEmail
    (N'admin@greencirclecertified.com'),     -- GCC admin       (Shawn, 2026-08-13)
    (N'carly@greencirclecertified.com'),     -- CLIENT ADMIN    (Shawn, 2026-08-17) last login 08-14
    (N'analyst@greencirclecertified.com'),   -- client analyst  (Shawn, 2026-08-17) last login 08-07
    (N'partner@amazon.com');                 -- Amazon partner  (Shawn, 2026-08-17) last login 08-12

/*--------------------------------------------------------------- pre-flight --*/
IF NOT EXISTS (SELECT 1 FROM dbo.Users u JOIN @KeepUsers k ON k.Email = u.Email
               WHERE u.Email = N'swest@anukiconsulting.com')
BEGIN
    RAISERROR('ABORT: the Audit:SystemUserEmail account (swest@anukiconsulting.com) is not present in dbo.Users. Purging would break every audited write on this database.', 16, 1);
    RETURN;
END;

PRINT '================ KEEP-LIST (users that survive) ================';
SELECT u.Id, u.Username, u.Email, u.IsActive
FROM   dbo.Users u JOIN @KeepUsers k ON k.Email = u.Email
ORDER BY u.Id;

PRINT '';
PRINT '=== keep-list entries with NO matching user row (typos / already gone) ===';
SELECT k.Email AS UnmatchedKeepEntry
FROM   @KeepUsers k
WHERE  NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Email = k.Email);

/*----------------------------------------------------------------- scope --*/
PRINT '';
PRINT '================ PURGE SCOPE ================';
SELECT 'Certificates'                 AS TableName, COUNT(*) AS RowsToDelete, 'direct' AS Mode FROM dbo.Certificates
UNION ALL SELECT 'CertificateProductIds',           COUNT(*), 'cascade' FROM dbo.CertificateProductIds
UNION ALL SELECT 'CertificateProductIdPartners',    COUNT(*), 'cascade' FROM dbo.CertificateProductIdPartners
UNION ALL SELECT 'CertificatePartners',             COUNT(*), 'cascade' FROM dbo.CertificatePartners
UNION ALL SELECT 'CertificateStatusHistory',        COUNT(*), 'cascade' FROM dbo.CertificateStatusHistory
UNION ALL SELECT 'CertificateTag (join)',           COUNT(*), 'cascade' FROM dbo.CertificateTag
UNION ALL SELECT 'CertCertTypeSubcats (join)',      COUNT(*), 'cascade' FROM dbo.CertificateCertificationTypeSubcategories
UNION ALL SELECT 'CertificateDocuments',            COUNT(*), 'cascade' FROM dbo.CertificateDocuments
UNION ALL SELECT 'CustomFieldValues',               COUNT(*), 'cascade' FROM dbo.CustomFieldValues
UNION ALL SELECT 'Products',                        COUNT(*), 'direct'  FROM dbo.Products
UNION ALL SELECT 'ProductIdentifiers',              COUNT(*), 'cascade' FROM dbo.ProductIdentifiers
UNION ALL SELECT 'ProductTag (join)',               COUNT(*), 'cascade' FROM dbo.ProductTag
UNION ALL SELECT 'Companies',                       COUNT(*), 'direct'  FROM dbo.Companies
UNION ALL SELECT 'CompanyUser (join)',              COUNT(*), 'cascade' FROM dbo.CompanyUser
UNION ALL SELECT 'AuditHistory',                    COUNT(*), 'direct'  FROM dbo.AuditHistory
UNION ALL SELECT 'ImportBatches',                   COUNT(*), 'direct'  FROM dbo.ImportBatches
UNION ALL SELECT 'Users (fake)',
                 (SELECT COUNT(*) FROM dbo.Users u WHERE NOT EXISTS (SELECT 1 FROM @KeepUsers k WHERE k.Email = u.Email)),
                 'direct'
UNION ALL SELECT 'UserRoles (join)',                COUNT(*), 'cascade' FROM dbo.UserRoles
UNION ALL SELECT 'PartnerUser (join)',              COUNT(*), 'cascade' FROM dbo.PartnerUser;

PRINT '';
PRINT '================ PRESERVED (must be UNCHANGED after) ================';
SELECT 'CertificationTypes' AS TableName, COUNT(*) AS RowsKept, 'client explicit' AS Why FROM dbo.CertificationTypes
UNION ALL SELECT 'ProductCategories',     COUNT(*), 'client explicit' FROM dbo.ProductCategories
UNION ALL SELECT 'ProductTypes',          COUNT(*), 'client explicit' FROM dbo.ProductTypes
UNION ALL SELECT 'ProductSubcategories',  COUNT(*), 'client explicit (EMPTY - see plan 12.3)' FROM dbo.ProductSubcategories
UNION ALL SELECT 'Tags',                  COUNT(*), 'client explicit' FROM dbo.Tags
UNION ALL SELECT 'Pathways',              COUNT(*), 'client explicit' FROM dbo.Pathways
UNION ALL SELECT 'Partners',              COUNT(*), 'client explicit' FROM dbo.Partners
UNION ALL SELECT 'PartnerApiKeys',        COUNT(*), 'Shawn 08-13 (Amazon live key)' FROM dbo.PartnerApiKeys
UNION ALL SELECT 'ApiKeys',               COUNT(*), 'Shawn 08-13' FROM dbo.ApiKeys
UNION ALL SELECT 'GoogleAnalyticsSettings', COUNT(*), 'Shawn 08-13' FROM dbo.GoogleAnalyticsSettings
UNION ALL SELECT 'Roles',                 COUNT(*), 'needed for GCC step 3' FROM dbo.Roles
UNION ALL SELECT 'Permissions',           COUNT(*), 'needed for GCC step 3' FROM dbo.Permissions
UNION ALL SELECT 'PermissionRole',        COUNT(*), 'needed for GCC step 3' FROM dbo.PermissionRole
UNION ALL SELECT 'CustomFields',          COUNT(*), 'config, not test data' FROM dbo.CustomFields
UNION ALL SELECT 'Certificates_bak_CR019', COUNT(*), 'CR-019 backup table' FROM dbo.Certificates_bak_CR019_20260608
UNION ALL SELECT 'Products_bak_CR019',    COUNT(*), 'CR-019 backup table' FROM dbo.Products_bak_CR019_20260608
UNION ALL SELECT '__EFMigrationsHistory', COUNT(*), 'schema version - NEVER touch' FROM dbo.__EFMigrationsHistory;

IF @Execute = 0
BEGIN
    PRINT '';
    PRINT '*** REPORT ONLY - nothing deleted. Re-run with -v Execute=1 to apply. ***';
    RETURN;
END;

/*----------------------------------------------------------------- execute --*/
PRINT '';
PRINT '================ EXECUTING PURGE ================';

BEGIN TRANSACTION;

    DELETE FROM dbo.Certificates;          PRINT '  Certificates      deleted: ' + CAST(@@ROWCOUNT AS varchar(20)) + ' (+ 7 cascaded child tables)';
    DELETE FROM dbo.Products;              PRINT '  Products          deleted: ' + CAST(@@ROWCOUNT AS varchar(20));
    DELETE FROM dbo.Companies;             PRINT '  Companies         deleted: ' + CAST(@@ROWCOUNT AS varchar(20)) + ' (+ CompanyUser cascaded)';
    DELETE FROM dbo.AuditHistory;          PRINT '  AuditHistory      deleted: ' + CAST(@@ROWCOUNT AS varchar(20));
    DELETE FROM dbo.ImportBatches;         PRINT '  ImportBatches     deleted: ' + CAST(@@ROWCOUNT AS varchar(20));

    DELETE u
    FROM   dbo.Users u
    WHERE  NOT EXISTS (SELECT 1 FROM @KeepUsers k WHERE k.Email = u.Email);
    PRINT '  Users             deleted: ' + CAST(@@ROWCOUNT AS varchar(20)) + ' (+ UserRoles / PartnerUser / CompanyUser cascaded)';

    /* Reseed IDENTITY so the migration's first insert starts from 1 rather than
       continuing the test data's numbering. Certificate.Number is the business
       key, but low surrogate Ids keep the migrated set legible.
       NB: RESEED to 0 on an empty table => next Id is 1. */
    DBCC CHECKIDENT ('dbo.Certificates',   RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Products',       RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Companies',      RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.AuditHistory',   RESEED, 0) WITH NO_INFOMSGS;
    PRINT '  IDENTITY reseeded: Certificates, Products, Companies, AuditHistory';

    /*-------------------------------------------------- post-purge assertions --*/
    DECLARE @err nvarchar(500) = NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'swest@anukiconsulting.com')
        SET @err = 'Audit:SystemUserEmail account did not survive the purge.';
    IF (SELECT COUNT(*) FROM dbo.Certificates) <> 0
        SET @err = 'Certificates not empty after purge.';
    IF (SELECT COUNT(*) FROM dbo.CertificateProductIds) <> 0
        SET @err = 'CertificateProductIds not empty - cascade did not fire.';
    IF (SELECT COUNT(*) FROM dbo.CertificationTypes) = 0
        SET @err = 'CertificationTypes was emptied - PRESERVED table was touched.';
    IF (SELECT COUNT(*) FROM dbo.Partners) = 0
        SET @err = 'Partners was emptied - PRESERVED table was touched.';
    IF (SELECT COUNT(*) FROM dbo.PartnerApiKeys) = 0
        SET @err = 'PartnerApiKeys was emptied - Amazon live key destroyed.';
    IF (SELECT COUNT(*) FROM dbo.Pathways) = 0
        SET @err = 'Pathways was emptied - PRESERVED table was touched.';

    IF @err IS NOT NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('PURGE ROLLED BACK: %s', 16, 1, @err);
        RETURN;
    END;

COMMIT TRANSACTION;

PRINT '';
PRINT '================ POST-PURGE STATE ================';
SELECT 'Certificates' AS TableName, COUNT(*) AS Rows FROM dbo.Certificates
UNION ALL SELECT 'Products',             COUNT(*) FROM dbo.Products
UNION ALL SELECT 'Companies',            COUNT(*) FROM dbo.Companies
UNION ALL SELECT 'Users',                COUNT(*) FROM dbo.Users
UNION ALL SELECT 'AuditHistory',         COUNT(*) FROM dbo.AuditHistory
UNION ALL SELECT '--- preserved ---',    NULL
UNION ALL SELECT 'CertificationTypes',   COUNT(*) FROM dbo.CertificationTypes
UNION ALL SELECT 'ProductCategories',    COUNT(*) FROM dbo.ProductCategories
UNION ALL SELECT 'ProductTypes',         COUNT(*) FROM dbo.ProductTypes
UNION ALL SELECT 'Tags',                 COUNT(*) FROM dbo.Tags
UNION ALL SELECT 'Pathways',             COUNT(*) FROM dbo.Pathways
UNION ALL SELECT 'Partners',             COUNT(*) FROM dbo.Partners
UNION ALL SELECT 'PartnerApiKeys',       COUNT(*) FROM dbo.PartnerApiKeys
UNION ALL SELECT 'ApiKeys',              COUNT(*) FROM dbo.ApiKeys
UNION ALL SELECT 'GoogleAnalyticsSettings', COUNT(*) FROM dbo.GoogleAnalyticsSettings
UNION ALL SELECT 'Roles',                COUNT(*) FROM dbo.Roles
UNION ALL SELECT 'Permissions',          COUNT(*) FROM dbo.Permissions;

PRINT '';
PRINT 'PURGE COMPLETE.';

/*==============================================================================
  CR-038 — allow duplicate certificate numbers for MIGRATED history only
  ------------------------------------------------------------------------------
  Matches EF migration 20260814195931_CR038_AllowDuplicateCertificateNumbers.
  Test and prod NEVER auto-migrate (Gate 7 / DEF-239), so this must be run by
  hand in the same deploy step as the DLL that expects it.

  WHAT IT DOES
    1. adds Certificates.AllowsDuplicateNumber  (bit NOT NULL DEFAULT 0)
    2. replaces the UNIQUE index on Number with a FILTERED unique index:
         UNIQUE (Number) WHERE AllowsDuplicateNumber = 0

  WHY A FILTER RATHER THAN DROPPING UNIQUENESS
  The client issued 137 certificate numbers to more than one certificate (312
  records; 128 of those numbers have genuinely different signed documents) and an
  issued number cannot be changed. Simply dropping the unique index would remove
  the guarantee for ALL 5,000+ ordinary certificates and leave uniqueness resting
  on application code — and on the main MVC create path there was no application
  check at all, only this index. The filter keeps the database guarantee for every
  non-exempt certificate and exempts only the grandfathered rows.

  ⚠️ THE INDEX NAME IS LOAD-BEARING — DO NOT RENAME IT.
  CertificateRepository.IsUniqueNumberViolation matches the SQL error text on the
  literal string 'IX_Certificates_Number' to drive DEF-193's retry (two analysts
  submitting the Create form at the same moment). Rename the index and that retry
  silently stops firing, with no error anywhere.

  ⚠️ WHAT THE FILTER CANNOT DO
  It cannot see exempt rows, so it does NOT stop a NEW certificate being given a
  number that a grandfathered record already uses. That case is covered in code by
  ICertificateRepository.IsCertificateNumberAvailableAsync (Edit path) and
  ValidationService.ValidateCertificateNumberUniquenessAsync (API path). Deploying
  this script WITHOUT that DLL leaves the gap open.

  Idempotent. Run with:  sqlcmd -S <srv> -d <db> -I -i <thisfile>
  (-I required: CR-187 persisted computed columns.)
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*-------------------------------------------------------------- pre-flight --*/
-- Creating a UNIQUE filtered index fails if the non-exempt rows already contain
-- duplicates. Report them rather than failing halfway.
IF EXISTS (
    SELECT 1 FROM dbo.Certificates c
    WHERE NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Certificates') AND name = 'AllowsDuplicateNumber')
      AND 1 = 0)
    PRINT '';

BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('dbo.Certificates') AND name = 'AllowsDuplicateNumber')
    BEGIN
        ALTER TABLE dbo.Certificates
            ADD AllowsDuplicateNumber bit NOT NULL CONSTRAINT DF_Certificates_AllowsDuplicateNumber DEFAULT(0);
        PRINT '  added Certificates.AllowsDuplicateNumber (bit NOT NULL default 0)';
    END
    ELSE PRINT '  AllowsDuplicateNumber already present';

COMMIT TRANSACTION;
GO

-- Separate batch: the index filter references a column added above.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @dups int = (
    SELECT COUNT(*) FROM (
        SELECT Number FROM dbo.Certificates
        WHERE AllowsDuplicateNumber = 0
        GROUP BY Number HAVING COUNT(*) > 1) x);

IF @dups > 0
BEGIN
    SELECT Number, COUNT(*) AS Records
    FROM dbo.Certificates WHERE AllowsDuplicateNumber = 0
    GROUP BY Number HAVING COUNT(*) > 1 ORDER BY COUNT(*) DESC;

    RAISERROR('ABORT: %d certificate number(s) are duplicated among NON-exempt rows. Flag the migrated duplicates (AllowsDuplicateNumber = 1) before creating the filtered unique index.', 16, 1, @dups);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE object_id = OBJECT_ID('dbo.Certificates') AND name = 'IX_Certificates_Number'
             AND filter_definition IS NULL)
BEGIN
    DROP INDEX IX_Certificates_Number ON dbo.Certificates;
    PRINT '  dropped the unfiltered unique index';
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.Certificates') AND name = 'IX_Certificates_Number')
BEGIN
    CREATE UNIQUE INDEX IX_Certificates_Number
        ON dbo.Certificates(Number)
        WHERE AllowsDuplicateNumber = 0;
    PRINT '  created FILTERED unique index IX_Certificates_Number';
END
ELSE PRINT '  filtered index already present';

/*------------------------------------------------------------- assertions --*/
DECLARE @err nvarchar(300) = NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.Certificates') AND name = 'AllowsDuplicateNumber')
    SET @err = 'AllowsDuplicateNumber column missing.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.Certificates')
                 AND name = 'IX_Certificates_Number'
                 AND is_unique = 1
                 AND filter_definition IS NOT NULL)
    SET @err = 'IX_Certificates_Number is not a filtered UNIQUE index.';

IF @err IS NOT NULL BEGIN RAISERROR('FAILED: %s', 16, 1, @err); RETURN; END;

PRINT '';
SELECT i.name AS IndexName, i.is_unique AS IsUnique, i.filter_definition AS Filter
FROM   sys.indexes i
WHERE  i.object_id = OBJECT_ID('dbo.Certificates') AND i.name = 'IX_Certificates_Number';

SELECT COUNT(*) AS TotalCertificates,
       SUM(CASE WHEN AllowsDuplicateNumber = 1 THEN 1 ELSE 0 END) AS GrandfatheredExempt
FROM   dbo.Certificates;

PRINT 'CR-038 schema change complete.';

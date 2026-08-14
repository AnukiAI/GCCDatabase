/*==============================================================================
  MIGRATION PREREQ P3b — widen dbo.Products.Name  nvarchar(255) -> nvarchar(1000)
  ------------------------------------------------------------------------------
  WHY THIS EXISTS: P3 WIDENED THE WRONG COLUMN (or rather, only one of two)

  Plan §2 P3 records "client confirmed 'widen, don't truncate'" and widened
  CertificateProductId.ProductName 200 -> 1000 (migration
  20260624142128_WidenCertificateProductIdProductName). That is the product name
  on the PRODUCT-ID row.

  There is a SECOND column holding a product name — dbo.Products.Name, the
  product entity itself — and it was left at nvarchar(255). The go-live load
  fails on it:

      Import failed: String or binary data would be truncated in table
      'GCCDatabase_Dev.dbo.Products', column 'Name'. Truncated value:
      '10Gain(r) XP Category 6A - CMR, 10Gain(r) XP Category 6A - CMP, ...'

  Found by actually running the transform output through the importer on dev
  (2026-08-13). The PREVIEW passed clean — 1,006/1,006 and 4,063/4,077 valid —
  because preview does not exercise the INSERT. Only APPLY surfaced it, and it
  failed the ENTIRE file: 0 certificates created, both times.

  Scope: 13 rows exceed 255 chars (3 on Active, 10 on Expired-1), longest 710.

  Widening to 1000 matches the sibling column rather than picking a new number,
  and honours the same client decision: widen, don't truncate.
  cf. [[feedback_denormalized_copy_width_mismatch]] (DEF-314) — the same family
  of bug: two columns carry the same value, only one got resized.

  ------------------------------------------------------------------------------
  THIS IS A SCHEMA CHANGE. It needs an EF migration for the model
  (Models/Product.cs MaxLength + a migration) so a fresh DB is born correct, AND
  this script run against any environment that does not auto-migrate — prod
  never does (Gate 7, DEF-239). This script alone is NOT the whole fix.

  ------------------------------------------------------------------------------
  Name cannot simply be ALTERed: a persisted computed column (CR-187
  NormalizedName) and two indexes depend on it. Order is drop-dependents ->
  alter -> recreate. The computed definition is captured from the catalogue at
  runtime rather than hardcoded, so this cannot drift from whatever CR-187
  actually deployed.

      sqlcmd -S <srv> -d <db> -I -i <thisfile>       (-I required: CR-187)
==============================================================================*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @curLen int = (SELECT max_length / 2 FROM sys.columns
                       WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'Name');

PRINT 'dbo.Products.Name is currently nvarchar(' + CAST(@curLen AS varchar(10)) + ')';

IF @curLen >= 1000
BEGIN
    PRINT 'Products.Name already >= 1000 — skipping to the AuditHistory step.';
    GOTO AuditStep;
END;

/* Capture the CR-187 computed definition before dropping it. */
DECLARE @def nvarchar(max) = (SELECT definition FROM sys.computed_columns
                              WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'NormalizedName');
DECLARE @persisted bit = (SELECT is_persisted FROM sys.computed_columns
                          WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'NormalizedName');

IF @def IS NULL
BEGIN
    RAISERROR('ABORT: could not read the NormalizedName computed definition. Refusing to drop what cannot be rebuilt.', 16, 1);
    RETURN;
END;

PRINT 'captured NormalizedName definition (' + CAST(LEN(@def) AS varchar(10)) + ' chars, persisted=' + CAST(@persisted AS varchar(1)) + ')';

BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_NormalizedName' AND object_id=OBJECT_ID('dbo.Products'))
    BEGIN DROP INDEX [IX_Products_NormalizedName] ON dbo.Products; PRINT '  dropped IX_Products_NormalizedName'; END;

    IF EXISTS (SELECT 1 FROM sys.computed_columns WHERE object_id=OBJECT_ID('dbo.Products') AND name='NormalizedName')
    BEGIN ALTER TABLE dbo.Products DROP COLUMN [NormalizedName]; PRINT '  dropped computed column NormalizedName'; END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_Name' AND object_id=OBJECT_ID('dbo.Products'))
    BEGIN DROP INDEX [IX_Products_Name] ON dbo.Products; PRINT '  dropped IX_Products_Name'; END;

    /* NOT NULL preserved — Name is NOT NULL today and the model requires it. */
    ALTER TABLE dbo.Products ALTER COLUMN [Name] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL;
    PRINT '  altered Name -> nvarchar(1000) NOT NULL';

    DECLARE @sql nvarchar(max) =
        N'ALTER TABLE dbo.Products ADD [NormalizedName] AS ' + @def +
        CASE WHEN @persisted = 1 THEN N' PERSISTED' ELSE N'' END + N';';
    EXEC sp_executesql @sql;
    PRINT '  recreated computed column NormalizedName';

    CREATE NONCLUSTERED INDEX [IX_Products_NormalizedName] ON dbo.Products ([NormalizedName]);
    CREATE NONCLUSTERED INDEX [IX_Products_Name] ON dbo.Products ([Name]);
    PRINT '  recreated both indexes';

    /*------------------------------------------------------------ assertions --*/
    DECLARE @err nvarchar(400) = NULL;

    IF (SELECT max_length/2 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Products') AND name='Name') <> 1000
        SET @err = 'Name is not nvarchar(1000) after the alter.';
    IF NOT EXISTS (SELECT 1 FROM sys.computed_columns WHERE object_id=OBJECT_ID('dbo.Products') AND name='NormalizedName')
        SET @err = 'NormalizedName was not recreated.';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_Name' AND object_id=OBJECT_ID('dbo.Products'))
        SET @err = 'IX_Products_Name was not recreated.';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_NormalizedName' AND object_id=OBJECT_ID('dbo.Products'))
        SET @err = 'IX_Products_NormalizedName was not recreated.';
    /* CR-187 must still normalise — a rebuilt computed column that yields NULL
       for a populated Name would silently break fuzzy search. */
    IF EXISTS (SELECT 1 FROM dbo.Products WHERE Name IS NOT NULL AND NormalizedName IS NULL)
        SET @err = 'NormalizedName is NULL for rows with a Name — the rebuilt definition is wrong.';

    IF @err IS NOT NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('ROLLED BACK: %s', 16, 1, @err);
        RETURN;
    END;

COMMIT TRANSACTION;

/*==============================================================================
  STEP 2 — dbo.AuditHistory.EntityName  nvarchar(255) -> nvarchar(1000)
  ------------------------------------------------------------------------------
  Widening Products.Name alone was NOT enough. Re-running the load moved the
  identical truncation one hop downstream:

      Import failed: String or binary data would be truncated in table
      'dbo.AuditHistory', column 'EntityName'.

  AuditHistory keeps a DENORMALIZED copy of the entity's name, and it was still
  nvarchar(255) while its source is now nvarchar(1000). That is DEF-314 exactly
  ([[feedback_denormalized_copy_width_mismatch]]): the copy must never be
  narrower than the source it copies.

  Widen to 1000 to MATCH Products.Name, rather than to some number that merely
  clears today's data — the whole point is that the two stay in step.

  Checked the rest of the chain in one pass rather than discovering it hop by
  hop. Everything else clears the go-live source maxima:
      Companies.Name              255  vs max  95   OK
      Certificates.Location       175  vs max 140   OK
      Certificates.Country        100  vs max  28   OK
      Certificates.CertificationResults 1000 vs 23  OK
      CertificateProductIds.ProductName 1000 (P3)   OK
  No index and no computed column depends on EntityName, so this is a plain
  ALTER — no drop/recreate dance.
==============================================================================*/
AuditStep:

DECLARE @auditLen int = (SELECT max_length / 2 FROM sys.columns
                         WHERE object_id = OBJECT_ID('dbo.AuditHistory') AND name = 'EntityName');

PRINT '';
PRINT 'dbo.AuditHistory.EntityName is currently nvarchar(' + CAST(@auditLen AS varchar(10)) + ')';

IF @auditLen < 1000
BEGIN
    ALTER TABLE dbo.AuditHistory ALTER COLUMN [EntityName] nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL;
    PRINT '  altered AuditHistory.EntityName -> nvarchar(1000) NULL';

    IF (SELECT max_length/2 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.AuditHistory') AND name='EntityName') <> 1000
    BEGIN
        RAISERROR('AuditHistory.EntityName did not widen.', 16, 1);
        RETURN;
    END;
END
ELSE PRINT '  already >= 1000, nothing to do.';

PRINT '';
SELECT 'Products' AS TableName, c.name AS ColumnName, c.max_length/2 AS NCharLen, c.is_nullable
FROM   sys.columns c
WHERE  c.object_id = OBJECT_ID('dbo.Products') AND c.name IN ('Name','NormalizedName')
UNION ALL
SELECT 'AuditHistory', c.name, c.max_length/2, c.is_nullable
FROM   sys.columns c
WHERE  c.object_id = OBJECT_ID('dbo.AuditHistory') AND c.name = 'EntityName';

PRINT 'DONE.';
PRINT '';
PRINT '*** LATENT LIMIT, STATED DELIBERATELY ***';
PRINT 'IX_Products_Name is a nonclustered index on Name. SQL Server caps a';
PRINT 'nonclustered key at 1700 bytes = 850 nvarchar chars, so nvarchar(1000)';
PRINT 'exceeds it on paper. CREATE INDEX succeeds with a warning and only an';
PRINT 'actual insert over 850 chars fails. Longest name in the go-live source is';
PRINT '710, so the load is safe — but a future 851+ char product name WILL fail';
PRINT 'to insert. If that becomes real, move Name to an INCLUDE column or drop';
PRINT 'IX_Products_Name; do not narrow the column back.';

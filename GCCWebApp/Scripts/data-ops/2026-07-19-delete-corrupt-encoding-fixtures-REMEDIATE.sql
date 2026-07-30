/* =============================================================================
   Delete CORRUPT encoding test-fixtures  —  TEST ENV, transactional, dry-run-able
   -----------------------------------------------------------------------------
   Origin : Client report 2026-07-17 (cert 26-9648 exported "BurtÃ¢â‚¬â„¢s Bees").
            Root cause = a Unicode edge-case QA battery ("Test Edge Product 00NN")
            was loaded into the test env with UTF-8 mis-decoded as Windows-1252,
            baking mojibake into name fields. App code + real records are clean
            (dev DB verified clean; see observations_log.md 2026-07-17).
            Detection: 2026-07-17-mojibake-encoding-detection-IDENTIFY.sql.

   Scope (Shawn 2026-07-19): DELETE the CORRUPT rows ONLY. The ~130 clean
   "Test Edge Product" fixtures (apostrophe/comma/space/long-name tests) are KEPT.

   Deletes:
     - Certificates whose Company OR Product is mojibake-corrupt  (~46; incl. 26-9648)
         -> all cert children CASCADE (CertificateProductIds incl. the 43 corrupt
            ProductNames, StatusHistory, Documents, Partners, Tags, CustomFieldValues)
     - the ~47 corrupt Products                                    (ProductTag / ProductIdentifiers CASCADE)
     - the 5 corrupt Companies (incl. the duplicate "Burt's Bees" Id 288)
   Preserves:
     - the real "Burt's Bees" (Company Id 62) and every genuine record
     - clean Test Edge fixtures (their CompanyId is NULLed only if it pointed at a
       deleted corrupt company; the row itself survives)

   SAFETY:
     * SET XACT_ABORT ON — any error aborts the whole transaction (no partial delete).
     * Guard: aborts if a target cert is NOT part of the Test Edge fixture set.
     * Dry-run by default. Pass the sqlcmd var Commit=0 to preview (ROLLBACK),
       Commit=1 to apply (COMMIT). Example:
         sqlcmd ... -v Commit=0 -i <thisfile>     (preview)
         sqlcmd ... -v Commit=1 -i <thisfile>     (apply)
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Required: Companies/Products/Certificates carry persisted computed NormalizedName
-- columns (CR-187). DML against them needs these ON (sqlcmd defaults QUOTED_IDENTIFIER OFF).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Commit bit = $(Commit);

IF OBJECT_ID('tempdb..#cc') IS NOT NULL DROP TABLE #cc;
IF OBJECT_ID('tempdb..#cp') IS NOT NULL DROP TABLE #cp;
IF OBJECT_ID('tempdb..#tc') IS NOT NULL DROP TABLE #tc;

BEGIN TRAN;

/* 1. Target sets --------------------------------------------------------------
   mojibake := value carries Ã, Â or â under binary (accent-sensitive) collation. */
SELECT Id INTO #cc FROM dbo.Companies
WHERE (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%';

SELECT Id INTO #cp FROM dbo.Products
WHERE (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%';

SELECT Id INTO #tc FROM dbo.Certificates
WHERE CompanyId IN (SELECT Id FROM #cc)
   OR ProductId IN (SELECT Id FROM #cp);

PRINT '--- Pre-delete target counts ---';
SELECT (SELECT COUNT(*) FROM #cc) AS CorruptCompanies,
       (SELECT COUNT(*) FROM #cp) AS CorruptProducts,
       (SELECT COUNT(*) FROM #tc) AS TargetCerts,
       (SELECT COUNT(*) FROM dbo.CertificateProductIds
         WHERE CertificateId IN (SELECT Id FROM #tc)) AS CascadedProductIds;

/* 2. Guard against over-delete: every target cert must belong to the Test Edge
      fixture set (its product is "Test Edge Product %" OR it is in-scope only
      because its Company is corrupt). If any other cert slipped in, ABORT. */
IF EXISTS (
    SELECT 1
    FROM dbo.Certificates c
    JOIN dbo.Products p ON p.Id = c.ProductId
    WHERE c.Id IN (SELECT Id FROM #tc)
      AND p.Name NOT LIKE N'Test Edge Product%'
      AND c.CompanyId NOT IN (SELECT Id FROM #cc)
)
BEGIN
    ROLLBACK TRAN;
    RAISERROR('ABORT: a target certificate is NOT part of the Test Edge fixture set. Nothing deleted.', 16, 1);
    RETURN;
END

/* 3. Detach surviving CLEAN products from corrupt companies (Products.CompanyId
      is NO_ACTION). Corrupt products are deleted in step 5, so exclude them. */
UPDATE dbo.Products
   SET CompanyId = NULL
WHERE CompanyId IN (SELECT Id FROM #cc)
  AND Id NOT IN (SELECT Id FROM #cp);

/* 4. Delete target certs — cascades all cert children (incl. the corrupt ProductIds). */
DELETE FROM dbo.Certificates WHERE Id IN (SELECT Id FROM #tc);

/* 5. Delete the corrupt products (now unreferenced by any cert). */
DELETE FROM dbo.Products WHERE Id IN (SELECT Id FROM #cp);

/* 6. Delete the corrupt companies (now unreferenced by cert or product). */
DELETE FROM dbo.Companies WHERE Id IN (SELECT Id FROM #cc);

/* 7. Post-delete verification: mojibake must be 0 across all three columns. */
PRINT '--- Post-delete mojibake remaining (expect 0 / 0 / 0) ---';
SELECT 'Companies.Name' AS Col, COUNT(*) AS RemainingMojibake FROM dbo.Companies
WHERE (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'
UNION ALL
SELECT 'Products.Name', COUNT(*) FROM dbo.Products
WHERE (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
   OR (CAST(Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'
UNION ALL
SELECT 'CertificateProductIds.ProductName', COUNT(*) FROM dbo.CertificateProductIds
WHERE (CAST(ProductName AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
   OR (CAST(ProductName AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
   OR (CAST(ProductName AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%';

/* 8. Confirm the REAL "Burt's Bees" (Id 62) is untouched and correct. */
PRINT '--- Real Burt''s Bees (Id 62) should remain, U+2019 intact ---';
SELECT Id, Name, CONVERT(varchar(200), CONVERT(varbinary(200), Name), 2) AS HexBytes
FROM dbo.Companies WHERE Id = 62;

IF @Commit = 1
BEGIN
    COMMIT TRAN;
    PRINT '=== COMMITTED — corrupt encoding fixtures deleted. ===';
END
ELSE
BEGIN
    ROLLBACK TRAN;
    PRINT '=== DRY RUN (rolled back). Re-run with -v Commit=1 to apply. ===';
END

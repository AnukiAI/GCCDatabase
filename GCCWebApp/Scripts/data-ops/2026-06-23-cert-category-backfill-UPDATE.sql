/* ============================================================================
   CERTIFICATE PRODUCT-CATEGORY BACKFILL — UPDATE
   QC #189/#195 / DEF-278. Sets each certificate's own ProductCategoryId
   (CR-186 cert-level field) from its linked product's CategoryId, but ONLY
   where the cert currently has NO category of its own (ProductCategoryId IS NULL).

   WHY THIS IS NEEDED (deploy coupling — read before running):
     DEF-278 changes the cert DETAIL views (Internal/External/Single) to display
     the certificate's OWN category only — i.e. `cert.ProductCategory?.Name ?? "—"`
     — and REMOVES the old fallback to the linked product's category. Going
     forward, new certs set ProductCategoryId (manual create auto-fills it; import
     now honors the template's Product Category column). But certs created BEFORE
     this fix (esp. every imported cert — the old import never set ProductCategoryId)
     have ProductCategoryId = NULL, so after the display change they would show "—"
     even though their product has a real category. This backfill restores their
     displayed category. ==> The display change (code) and this backfill (data)
     MUST DEPLOY TOGETHER. If the code ships without this, existing certs lose
     their category on the detail view until this runs.

   SCOPE / SAFETY
     - Touches ONLY rows where Certificates.ProductCategoryId IS NULL (idempotent:
       re-running changes nothing once populated; never overwrites a chosen value).
     - A genuinely "blank on purpose" cert created AFTER this runs stays NULL and
       correctly shows "—" — this one-time pass only fixes the historical NULLs.
     - Backup table created FIRST and persists regardless of rollback (reversible
       even after COMMIT: restore from Certificates_bak_DEF278_*).
     - Runs inside a transaction that ROLLS BACK by default. Review the verification
       output, then comment ROLLBACK / uncomment COMMIT and re-run to apply.
     - Requires a DB backup + CR sign-off before COMMIT per project data-change
       rules ([[feedback_data_changes_require_cr]]). CR-186-adjacent.
   ============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;

/* --- 1) Pre-flight counts (read-only) --- */
DECLARE @nullCerts INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE ProductCategoryId IS NULL);
DECLARE @fixable INT = (
    SELECT COUNT(*)
    FROM dbo.Certificates c
    JOIN dbo.Products p ON p.Id = c.ProductId
    WHERE c.ProductCategoryId IS NULL AND p.CategoryId IS NOT NULL
);
PRINT 'Certs with NULL ProductCategoryId           = ' + CAST(@nullCerts AS VARCHAR(10));
PRINT 'Of those, backfillable from product category = ' + CAST(@fixable AS VARCHAR(10));

/* --- 2) BACKUP the exact rows about to change (persists outside the transaction) --- */
IF OBJECT_ID('dbo.Certificates_bak_DEF278_20260623') IS NOT NULL DROP TABLE dbo.Certificates_bak_DEF278_20260623;
SELECT c.*
INTO dbo.Certificates_bak_DEF278_20260623
FROM dbo.Certificates c
JOIN dbo.Products p ON p.Id = c.ProductId
WHERE c.ProductCategoryId IS NULL AND p.CategoryId IS NOT NULL;
PRINT 'Backup rows -> Certificates_bak_DEF278_20260623 = '
    + CAST((SELECT COUNT(*) FROM dbo.Certificates_bak_DEF278_20260623) AS VARCHAR(10));

BEGIN TRAN;

/* --- 3) Backfill cert.ProductCategoryId from the linked product's category --- */
UPDATE c
SET c.ProductCategoryId = p.CategoryId
FROM dbo.Certificates c
JOIN dbo.Products p ON p.Id = c.ProductId
WHERE c.ProductCategoryId IS NULL AND p.CategoryId IS NOT NULL;
PRINT 'Rows updated = ' + CAST(@@ROWCOUNT AS VARCHAR(10));

/* --- 4) Verify (runs before rollback/commit) --- */
SELECT TOP 50 c.Number, cat.Name AS BackfilledCategory, p.Name AS ProductName
FROM dbo.Certificates c
JOIN dbo.Products p ON p.Id = c.ProductId
LEFT JOIN dbo.ProductCategories cat ON cat.Id = c.ProductCategoryId
WHERE c.Id IN (SELECT Id FROM dbo.Certificates_bak_DEF278_20260623)
ORDER BY c.Number;

DECLARE @stillNull INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE ProductCategoryId IS NULL);
PRINT 'Certs STILL NULL after backfill = ' + CAST(@stillNull AS VARCHAR(10))
    + '  (expected: only certs whose product has no category, if any)';

/* --- 5) DEFAULT = ROLLBACK. Review output, then comment ROLLBACK / uncomment COMMIT, re-run. --- */
ROLLBACK TRAN;
-- COMMIT TRAN;
GO

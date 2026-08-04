/* ============================================================================
   Restore Products 100 + 101 ('Test Carly') from the AuditHistory snapshot
   ----------------------------------------------------------------------------
   Authorized by : Shawn West, 2026-08-04
   CR            : CR-019 (same data-ops authorization as the QC #242 cleanup)
   Target        : TEST ENVIRONMENT (gccwebapp.com -> gccwebap_certificates)
   Source rows   : AuditHistory.Id 2335 (product 100), 2336 (product 101)

   WHY
   ---
   During QC #242 follow-up the two products 'Test Carly' (company 29 = "TEST")
   were deleted via the admin UI, intending instead to remove the 'Carly Test'
   product CATEGORY — which had already been removed earlier the same day by
   2026-08-04-qc242-carly-test-cleanup.sql. Restoring them to undo the mistake.

   WHAT IS RESTORED  (corrected 2026-08-04 — see note)
   ---------------------------------------------------
   The snapshot showed only 8 keys, which initially looked like truncation. It
   is not. AuditService serializes with
       DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
   (Services/Audit/AuditService.cs:491), so NULL fields are OMITTED by design:
   an absent key means NULL, not "not captured."

   These two products therefore genuinely had ProductTypeId, ProductSubcategoryId,
   Sku, Upc, Asin, Description and Model all NULL. **This restore is faithful,
   not lossy.** Do not "improve" it by guessing values for the absent keys.

   Two real caveats remain:
     * UpdatedAt is excluded from every snapshot by AuditSkipFields
       (AuditService.cs:414) and is NOT NULL here, so it is set to CreatedAt.
       The original modification time is not recoverable.
     * NormalizedName is a PERSISTED COMPUTED column — it must NOT be inserted;
       SQL Server recomputes it (verified below: 'testcarly', matching the audit).

   Child rows (ProductIdentifiers, ProductTag) cascade on delete and are NOT
   audited anywhere — a genuine gap, logged as DEF-337. Verified none existed
   for these two products, so nothing was lost here.

   USAGE
   -----
     Dry run (rolls back):  sqlcmd ... -I -v DoCommit=0 -i <this file>
     Commit:                sqlcmd ... -I -v DoCommit=1 -i <this file>
   ============================================================================ */

SET QUOTED_IDENTIFIER ON;   -- required: Products has a computed NormalizedName
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DoCommit bit = $(DoCommit);

BEGIN TRY
    BEGIN TRANSACTION;

    /* ---- Guard 1: the Ids must still be free (never silently overwrite) ---- */
    IF EXISTS (SELECT 1 FROM Products WHERE Id IN (100,101))
        THROW 52001, 'ABORT: product 100 or 101 already exists — nothing restored.', 1;

    /* ---- Guard 2: the audit source rows must be present and intact ---- */
    IF (SELECT COUNT(*) FROM AuditHistory
        WHERE Id IN (2335,2336) AND EntityType='Product' AND Action='DELETE') <> 2
        THROW 52002, 'ABORT: expected both audit snapshot rows 2335 and 2336.', 1;

    /* ---- Guard 3: the FK targets must still exist ---- */
    IF NOT EXISTS (SELECT 1 FROM ProductCategories WHERE Id = 1)
        THROW 52003, 'ABORT: category 1 is missing.', 1;
    IF NOT EXISTS (SELECT 1 FROM Companies WHERE Id = 29)
        THROW 52004, 'ABORT: company 29 is missing.', 1;

    PRINT 'All 3 guards passed.';

    /* ---- Restore, values taken verbatim from the audit JSON ---- */
    SET IDENTITY_INSERT Products ON;

    INSERT INTO Products (Id, Name, CategoryId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
      (100, 'Test Carly', 1, 29, 1, 0, '2026-04-03T18:57:14.9536273', '2026-04-03T18:57:14.9536273'),
      (101, 'Test Carly', 1, 29, 1, 0, '2026-04-03T18:57:15.3864757', '2026-04-03T18:57:15.3864757');

    DECLARE @Rows int = @@ROWCOUNT;   -- capture immediately; PRINT would reset it
    SET IDENTITY_INSERT Products OFF;

    PRINT CONCAT('Products restored : ', @Rows, ' (expected 2)');
    IF @Rows <> 2 THROW 52005, 'ABORT: insert count was not 2.', 1;

    /* ---- Verify, including that the computed column recomputed correctly ---- */
    PRINT '--- Restored rows ---';
    SELECT Id, Name, NormalizedName, CategoryId, CompanyId, ProductTypeId, IsActive, IsDeleted, CreatedAt
    FROM Products WHERE Id IN (100,101) ORDER BY Id;

    IF EXISTS (SELECT 1 FROM Products WHERE Id IN (100,101) AND NormalizedName <> 'testcarly')
        THROW 52006, 'ABORT: NormalizedName did not recompute to the audited value.', 1;

    PRINT '--- Totals ---';
    SELECT (SELECT COUNT(*) FROM Products)     AS TotalProducts,
           (SELECT COUNT(*) FROM Certificates) AS TotalCerts;

    IF @DoCommit = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT '*** COMMITTED ***';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT '*** DRY RUN — ROLLED BACK, nothing changed ***';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '*** ABORTED — ROLLED BACK ***';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH

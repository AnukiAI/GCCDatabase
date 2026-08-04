/* ============================================================================
   QC #242 test-data cleanup — "Carly Test" category + "Test Type" product type
   ----------------------------------------------------------------------------
   Authorized by : Shawn West, 2026-08-04
   CR            : CR-019 ("deletes confirmed 'Carly Test' data")
   Target        : TEST ENVIRONMENT (gccwebapp.com -> gccwebap_certificates)
   Related       : DEF-323 (count under-report, fixed), DEF-324, CR-032 (closed)

   WHY THIS EXISTS
   ---------------
   QC #242 was NOT a code defect. DeleteProductCategoryAsync /
   DeleteProductTypeAsync refused correctly: four live test products really did
   reference the two records the client was trying to remove. The client could
   not find them because the products carry names that do not read as test data
   ("Best Product Ever v2"), and — for the category — because the Usage column
   under-reported the count to 0 (that display bug is DEF-323).

   WHAT IS DELETED (6 rows, all verified free of real references 2026-08-04)
   ------------------------------------------------------------------------
     Products         140  'Best Product Ever v2'    -> ProductTypeId 86
     Products         141  'Best Product Ever v3(TM)'-> ProductTypeId 86
     Products         389  'Carly Test'              -> CategoryId 9
     Products         392  'Test'                    -> CategoryId 9
     ProductTypes      86  'Test Type'               (under category 1, REAL)
     ProductCategories  9  'Carly Test'

   NOT TOUCHED: category 1 'Chemicals and Raw Materials' is a real category and
   is only the *parent* of type 86. It stays.

   SAFETY
   ------
   Every delete is guarded by a precondition check that THROWs (forcing
   rollback) if anything real has attached itself since the survey. Note that
   FK_Products_ProductCategories_CategoryId is ON DELETE CASCADE, so deleting
   category 9 would silently take its products with it — this script deletes
   products explicitly first so nothing is removed implicitly.

   USAGE
   -----
     Dry run (rolls back):  sqlcmd ... -v DoCommit=0 -i <this file>
     Commit:                sqlcmd ... -v DoCommit=1 -i <this file>
   ============================================================================ */

/* REQUIRED: Products/ProductCategories/ProductTypes carry persisted computed
   NormalizedName columns (CR-187). Any DELETE against them fails with Msg 1934
   unless QUOTED_IDENTIFIER is ON — sqlcmd leaves it OFF by default, so this
   must be set here (and/or sqlcmd invoked with -I). */
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DoCommit bit = $(DoCommit);

BEGIN TRY
    BEGIN TRANSACTION;

    /* ---- Guard 1: no certificate may reference any of the 4 products ---- */
    IF EXISTS (SELECT 1 FROM Certificates WHERE ProductId IN (140,141,389,392))
        THROW 51001, 'ABORT: a certificate references one of the products marked for deletion.', 1;

    /* ---- Guard 2: no product identifiers on those products ---- */
    IF EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId IN (140,141,389,392))
        THROW 51002, 'ABORT: product identifiers exist on a product marked for deletion.', 1;

    /* ---- Guard 3: no certificate may reference category 9 or type 86 ---- */
    IF EXISTS (SELECT 1 FROM Certificates
               WHERE ProductCategoryId = 9 OR ProductTypeId = 86 OR ProductSubcategoryId = 86)
        THROW 51003, 'ABORT: a certificate references category 9 or type 86.', 1;

    /* ---- Guard 4: type 86 must have no child types ---- */
    IF EXISTS (SELECT 1 FROM ProductTypes WHERE ParentId = 86)
        THROW 51004, 'ABORT: type 86 has child types.', 1;

    /* ---- Guard 5: category 9 must have no product types under it ---- */
    IF EXISTS (SELECT 1 FROM ProductTypes WHERE CategoryId = 9)
        THROW 51005, 'ABORT: category 9 has product types under it.', 1;

    /* ---- Guard 6: the 6 target rows must be exactly what we surveyed ---- */
    IF (SELECT COUNT(*) FROM Products WHERE Id IN (140,141,389,392)) <> 4
        THROW 51006, 'ABORT: expected exactly 4 target products.', 1;
    IF NOT EXISTS (SELECT 1 FROM ProductCategories WHERE Id = 9 AND Name = 'Carly Test')
        THROW 51007, 'ABORT: category 9 is not "Carly Test".', 1;
    IF NOT EXISTS (SELECT 1 FROM ProductTypes WHERE Id = 86 AND Name = 'Test Type')
        THROW 51008, 'ABORT: type 86 is not "Test Type".', 1;

    /* ---- Guard 7: no OTHER product may still sit under cat 9 / type 86 ---- */
    IF EXISTS (SELECT 1 FROM Products
               WHERE Id NOT IN (140,141,389,392)
                 AND (CategoryId = 9 OR ProductTypeId = 86 OR ProductSubcategoryId = 86))
        THROW 51009, 'ABORT: an unsurveyed product still references category 9 or type 86.', 1;

    PRINT 'All 7 guards passed.';

    /* ---- Deletes, children first ---- */
    /* NOTE: @@ROWCOUNT must be captured into a variable IMMEDIATELY after each
       DELETE. Any intervening statement — including PRINT — resets it to that
       statement's own row count, which silently turns the assertions below into
       "0 <> 4" and aborts a perfectly good run. */
    DECLARE @Rows int;

    DELETE FROM ProductTag WHERE ProductsId IN (140,141,389,392);
    SET @Rows = @@ROWCOUNT;
    PRINT CONCAT('ProductTag rows deleted     : ', @Rows);

    DELETE FROM Products WHERE Id IN (140,141,389,392);
    SET @Rows = @@ROWCOUNT;
    PRINT CONCAT('Products deleted            : ', @Rows, ' (expected 4)');
    IF @Rows <> 4 THROW 51010, 'ABORT: product delete count was not 4.', 1;

    DELETE FROM ProductTypes WHERE Id = 86;
    SET @Rows = @@ROWCOUNT;
    PRINT CONCAT('ProductTypes deleted        : ', @Rows, ' (expected 1)');
    IF @Rows <> 1 THROW 51011, 'ABORT: type delete count was not 1.', 1;

    DELETE FROM ProductCategories WHERE Id = 9;
    SET @Rows = @@ROWCOUNT;
    PRINT CONCAT('ProductCategories deleted   : ', @Rows, ' (expected 1)');
    IF @Rows <> 1 THROW 51012, 'ABORT: category delete count was not 1.', 1;

    /* ---- Post-state verification ---- */
    PRINT '--- Post-state (all must be 0) ---';
    SELECT
      (SELECT COUNT(*) FROM Products          WHERE Id IN (140,141,389,392)) AS ProductsRemaining,
      (SELECT COUNT(*) FROM ProductTypes      WHERE Id = 86)                 AS TypeRemaining,
      (SELECT COUNT(*) FROM ProductCategories WHERE Id = 9)                  AS CategoryRemaining;

    PRINT '--- Collateral check: real data intact ---';
    SELECT
      (SELECT COUNT(*) FROM Certificates)                         AS TotalCerts,
      (SELECT COUNT(*) FROM Products)                             AS TotalProducts,
      (SELECT COUNT(*) FROM ProductCategories)                    AS TotalCategories,
      (SELECT COUNT(*) FROM ProductTypes)                         AS TotalTypes,
      (SELECT COUNT(*) FROM ProductCategories WHERE Id = 1)       AS ChemicalsCategoryIntact;

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

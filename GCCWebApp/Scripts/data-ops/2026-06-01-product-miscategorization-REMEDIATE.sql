/* ============================================================================
   PRODUCT MIS-CATEGORIZATION — REMEDIATION (GUARDED, WRITES DATA)
   Companion to 2026-06-01-product-miscategorization-IDENTIFY.sql (run that FIRST).
   Data-ops pass for DEF-231 fallout (QC #186). This is a DATA change on business
   records, so per project policy it requires:

     1. A formal CR / written go-ahead from GCC for the specific rows touched.
     2. A FULL DB BACKUP taken immediately before execution.
     3. The IDENTIFY script reviewed, and the suspect lists triaged by a human
        into (a) test junk to delete and (b) real records to re-categorize.

   WHY THIS IS NOT A ONE-CLICK "FIX": the analyst's INTENDED category/type/sub
   was overwritten by the bug and is not stored anywhere. There is no correct
   value to restore automatically. Real records must be corrected to values a
   human supplies (ideally just re-edited in the now-fixed UI). This script only:
     - PART A: deletes confirmed TEST data (Carly Test), and
     - PART B: applies human-supplied correct values to a confirmed worklist.

   SAFETY MODEL
     - Everything runs inside ONE transaction that ENDS IN ROLLBACK by default.
     - Run it, read the PRINTed before/after counts, confirm they match the
       IDENTIFY output, THEN change the final `ROLLBACK` to `COMMIT` and re-run.
     - Target: SQL Server. Same instance the client tests — treat as production.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;   -- any error aborts the whole transaction

BEGIN TRAN;

/* ===========================================================================
   PART A — DELETE CONFIRMED TEST DATA (Carly Test)  [QC #170 thread 1]
   Tune @TestNameLike to the exact test names confirmed in IDENTIFY SECTION 3.
   Leave @DoPartA = 0 to skip.
   Deletion order respects FKs: clear cert references -> products -> child
   ProductTypes -> top-level ProductTypes -> categories.
   =========================================================================== */
DECLARE @DoPartA      BIT           = 0;                 -- set 1 to run Part A
DECLARE @TestNameLike NVARCHAR(100) = N'%carly%test%';   -- MUST match IDENTIFY

IF @DoPartA = 1
BEGIN
    PRINT '--- PART A: deleting test data matching ' + @TestNameLike + ' ---';

    -- target sets
    DECLARE @TestCats TABLE (Id INT PRIMARY KEY);
    DECLARE @TestTypes TABLE (Id INT PRIMARY KEY);
    DECLARE @TestProducts TABLE (Id INT PRIMARY KEY);

    INSERT @TestCats  SELECT Id FROM ProductCategories WHERE Name LIKE @TestNameLike;
    -- types/subcats either named like the test OR sitting under a test category
    INSERT @TestTypes SELECT Id FROM ProductTypes
                      WHERE Name LIKE @TestNameLike
                         OR CategoryId IN (SELECT Id FROM @TestCats);
    INSERT @TestProducts SELECT Id FROM Products
                      WHERE ISNULL(IsDeleted,0)=0
                        AND ( CategoryId IN (SELECT Id FROM @TestCats)
                           OR ProductTypeId IN (SELECT Id FROM @TestTypes)
                           OR ProductSubcategoryId IN (SELECT Id FROM @TestTypes)
                           OR Name LIKE @TestNameLike );

    PRINT 'Test categories : ' + CAST((SELECT COUNT(*) FROM @TestCats)     AS VARCHAR(10));
    PRINT 'Test types/subs : ' + CAST((SELECT COUNT(*) FROM @TestTypes)    AS VARCHAR(10));
    PRINT 'Test products   : ' + CAST((SELECT COUNT(*) FROM @TestProducts) AS VARCHAR(10));

    /* GUARD: refuse to proceed if a NON-test certificate (real business record)
       points at any of these test entities. If this fires, stop and triage —
       a real cert is tied to test data and needs a manual decision. */
    IF EXISTS (
        SELECT 1 FROM Certificates c
        WHERE  c.ProductId           IN (SELECT Id FROM @TestProducts)
            OR c.ProductCategoryId   IN (SELECT Id FROM @TestCats)
            OR c.ProductTypeId       IN (SELECT Id FROM @TestTypes)
            OR c.ProductSubcategoryId IN (SELECT Id FROM @TestTypes)
    )
    BEGIN
        PRINT '!! A certificate references the test data. Listing them; NOT deleting.';
        SELECT 'CERT REFERENCING TEST DATA' AS Warn, c.Id, c.Number, c.Status, c.ProductId,
               c.ProductCategoryId, c.ProductTypeId, c.ProductSubcategoryId
        FROM   Certificates c
        WHERE  c.ProductId            IN (SELECT Id FROM @TestProducts)
            OR c.ProductCategoryId    IN (SELECT Id FROM @TestCats)
            OR c.ProductTypeId        IN (SELECT Id FROM @TestTypes)
            OR c.ProductSubcategoryId IN (SELECT Id FROM @TestTypes);
        -- Decide per row whether to null these FKs before deleting. Example (commented):
        -- UPDATE Certificates SET ProductCategoryId = NULL
        --   WHERE Id = <certId> AND ProductCategoryId IN (SELECT Id FROM @TestCats);
    END
    ELSE
    BEGIN
        DELETE FROM Products     WHERE Id IN (SELECT Id FROM @TestProducts);
        PRINT 'Deleted products: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

        -- child types first (ParentId NOT NULL), then top-level
        DELETE FROM ProductTypes WHERE Id IN (SELECT Id FROM @TestTypes) AND ParentId IS NOT NULL;
        PRINT 'Deleted child types: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
        DELETE FROM ProductTypes WHERE Id IN (SELECT Id FROM @TestTypes) AND ParentId IS NULL;
        PRINT 'Deleted top-level types: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

        DELETE FROM ProductCategories WHERE Id IN (SELECT Id FROM @TestCats);
        PRINT 'Deleted categories: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    END
END
ELSE PRINT '--- PART A skipped (@DoPartA = 0) ---';

/* ===========================================================================
   PART B — CORRECT CONFIRMED REAL RECORDS (human-supplied values only)
   For genuine business products/certs that were mis-categorized. There is NO
   automatic source of truth, so a human fills in the correct IDs below, one row
   per ProductId. The fix also pushes the same correct values onto the
   certificate(s) that reference each product (they were auto-filled from the
   product). Leave the VALUES list empty to skip.

   Get the correct CategoryId / TypeId / SubId from /Admin/ProductTypes or:
     SELECT Id, Name, ParentId, CategoryId, [Level] FROM ProductTypes ORDER BY CategoryId, [Level];
   Use NULL for a level the product shouldn't set.

   *** Often the cleanest path for real records is to re-edit them in the now-
       fixed UI (audited, no SQL). Use PART B only for genuine bulk volume. ***
   =========================================================================== */
DECLARE @Corrections TABLE (
    ProductId           INT PRIMARY KEY,
    CorrectCategoryId   INT     NOT NULL,
    CorrectTypeId       INT     NULL,
    CorrectSubId        INT     NULL
);

-- >>> FILL IN after triage. Example rows (commented out):
-- INSERT @Corrections (ProductId, CorrectCategoryId, CorrectTypeId, CorrectSubId) VALUES
--   (123, 1, 4, 48),    -- product 123 -> Chemicals / Plastics / LLDPE
--   (124, 1, 4, NULL);  -- product 124 -> Chemicals / Plastics (no subcategory)

IF EXISTS (SELECT 1 FROM @Corrections)
BEGIN
    PRINT '--- PART B: applying ' + CAST((SELECT COUNT(*) FROM @Corrections) AS VARCHAR(10)) + ' product corrections ---';

    /* Integrity guards before writing: every supplied id must exist and be
       internally consistent (type is top-level under the category; sub is a
       child of that type). XACT_ABORT rolls everything back if a guard raises. */
    IF EXISTS (SELECT 1 FROM @Corrections x
               WHERE NOT EXISTS (SELECT 1 FROM ProductCategories pc WHERE pc.Id = x.CorrectCategoryId))
        THROW 50001, 'A CorrectCategoryId does not exist in ProductCategories.', 1;

    IF EXISTS (SELECT 1 FROM @Corrections x WHERE x.CorrectTypeId IS NOT NULL AND NOT EXISTS (
                 SELECT 1 FROM ProductTypes pt
                 WHERE pt.Id = x.CorrectTypeId AND pt.ParentId IS NULL AND pt.CategoryId = x.CorrectCategoryId))
        THROW 50002, 'A CorrectTypeId is not a top-level type under its CorrectCategoryId.', 1;

    IF EXISTS (SELECT 1 FROM @Corrections x WHERE x.CorrectSubId IS NOT NULL AND NOT EXISTS (
                 SELECT 1 FROM ProductTypes sub
                 WHERE sub.Id = x.CorrectSubId AND sub.ParentId = x.CorrectTypeId))
        THROW 50003, 'A CorrectSubId is not a child of its CorrectTypeId.', 1;

    -- before snapshot
    SELECT 'BEFORE' AS Stage, p.Id, p.Name, p.CategoryId, p.ProductTypeId, p.ProductSubcategoryId
    FROM Products p JOIN @Corrections x ON x.ProductId = p.Id;

    UPDATE p
       SET p.CategoryId           = x.CorrectCategoryId,
           p.ProductTypeId        = x.CorrectTypeId,
           p.ProductSubcategoryId = x.CorrectSubId
    FROM Products p
    JOIN @Corrections x ON x.ProductId = p.Id;
    PRINT 'Products updated: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- Cascade to the certificates that reference these products AND currently
    -- carry the corrupted (default-category / null) signature, i.e. the ones
    -- that were auto-filled from the bad product. Certs an analyst has since
    -- corrected by hand are left untouched.
    UPDATE c
       SET c.ProductCategoryId    = x.CorrectCategoryId,
           c.ProductTypeId        = x.CorrectTypeId,
           c.ProductSubcategoryId = x.CorrectSubId
    FROM Certificates c
    JOIN @Corrections x ON x.ProductId = c.ProductId
    WHERE c.ProductTypeId IS NULL AND c.ProductSubcategoryId IS NULL;
    PRINT 'Certificates updated: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

    -- after snapshot
    SELECT 'AFTER' AS Stage, p.Id, p.Name, p.CategoryId, p.ProductTypeId, p.ProductSubcategoryId
    FROM Products p JOIN @Corrections x ON x.ProductId = p.Id;
END
ELSE PRINT '--- PART B skipped (no corrections supplied) ---';

/* ===========================================================================
   COMMIT GATE
   Review the PRINTed counts + BEFORE/AFTER above. They MUST match what you
   expect from IDENTIFY. Only then change ROLLBACK -> COMMIT and re-run.
   =========================================================================== */
PRINT '*** Transaction will ROLLBACK. Change to COMMIT once verified. ***';
ROLLBACK TRAN;   -- <-- change to COMMIT TRAN after verifying the output
GO

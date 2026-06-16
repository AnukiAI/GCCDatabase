/* ============================================================================
   CR-186 SPLIT — PRODUCT SPLIT BY (TYPE, SUBCATEGORY) — REMEDIATE (WRITES DATA)
   Date: 2026-06-15
   Companion to 2026-06-15-product-split-by-type-subcat-IDENTIFY.sql (run that FIRST).

   CLIENT DECISION (2026-06-15): a Product Management line item = (Company, Name,
   Category, Product Type, Subcategory). Products whose certs span multiple
   (Type, Subcategory) combos are SPLIT into one record per combo; certs are
   re-pointed to the matching record. Going forward the app does this on every cert
   create/edit (CertificateController.ResolveProductForCertAsync); this is the
   one-time catch-up for existing rows. Supersedes the withdrawn single-value backfill.

   WHAT IT DOES
     1. Each product KEEPS its primary combo (most certs) — sets Type/Subcategory on it.
     2. For every OTHER combo, creates a NEW product record cloning the base product's
        identity-neutral fields (Name/Company/Category/Description/SKU/Model).
     3. Re-points each certificate to the product record matching its own (Type, Subcat).
     Single-combo / no-cert products are only updated in step 1 (no new records).

   WHAT IT DOES NOT DO: no deletes, no schema change, no file/upload changes. Product-
   level Identifiers/Tags stay on the original (primary) record (flagged default).

   POLICY: live business-data change => written GCC go-ahead on the reviewed IDENTIFY
   worklist + FULL DB BACKUP immediately before running ([[feedback_data_changes_require_cr]]).

   SAFETY MODEL: ONE transaction, ENDS IN ROLLBACK by default. Run it, read the PRINTed
   counts, confirm they match IDENTIFY, THEN change ROLLBACK TRAN -> COMMIT TRAN and re-run.
   Target: SQL Server (treat as production).
   ============================================================================ */

-- QUOTED_IDENTIFIER/ANSI_NULLS ON required for INSERT into Products (computed/indexed
-- column → error 1934 otherwise). Must be set at batch top, before any DML.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @ProductsBefore INT = (SELECT COUNT(*) FROM Products WHERE IsDeleted = 0);
PRINT 'Products (not deleted) BEFORE: ' + CAST(@ProductsBefore AS VARCHAR(10));

-- ---- Stage: base product attributes x its cert combos, ranked (rn=1 = primary/kept)
IF OBJECT_ID('tempdb..#ranked') IS NOT NULL DROP TABLE #ranked;
SELECT  bp.Id AS OrigProductId, bp.Name, bp.Description, bp.Model, bp.Sku,
        bp.CompanyId, bp.CategoryId, bp.IsActive,
        cc.ProductTypeId, cc.ProductSubcategoryId, cc.CertCount,
        ROW_NUMBER() OVER (PARTITION BY cc.ProductId
                           ORDER BY cc.CertCount DESC,
                                    CASE WHEN cc.ProductTypeId IS NULL THEN 1 ELSE 0 END,
                                    cc.ProductTypeId, cc.ProductSubcategoryId) AS rn
INTO    #ranked
FROM   (SELECT ProductId, ProductTypeId, ProductSubcategoryId, COUNT(*) AS CertCount
        FROM Certificates GROUP BY ProductId, ProductTypeId, ProductSubcategoryId) cc
JOIN    Products bp ON bp.Id = cc.ProductId AND bp.IsDeleted = 0;

-- ---- Step 1: set the PRIMARY combo's Type/Subcategory onto the existing product row.
-- (Updated unconditionally for every product that has certs — idempotent; avoids the
--  SQL Server 2022-only IS DISTINCT FROM so it runs on older prod instances too.)
UPDATE p
SET p.ProductTypeId       = r.ProductTypeId,
    p.ProductSubcategoryId = r.ProductSubcategoryId,
    p.UpdatedAt            = SYSUTCDATETIME()
FROM Products p
JOIN #ranked r ON r.OrigProductId = p.Id AND r.rn = 1
WHERE p.IsDeleted = 0;
DECLARE @PrimarySet INT = @@ROWCOUNT;
PRINT 'Primary-combo products updated (Type/Subcat set): ' + CAST(@PrimarySet AS VARCHAR(10));

-- ---- Steps 2+3: create a record per NON-primary combo and re-point its certs
DECLARE @NewRecords INT = 0, @CertsRepointed INT = 0, @NewId INT;
DECLARE @OrigId INT, @Name NVARCHAR(255), @Desc NVARCHAR(MAX), @Model NVARCHAR(100),
        @Sku NVARCHAR(50), @Co INT, @Cat INT, @Type INT, @Sub INT, @Active BIT;

DECLARE split_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT OrigProductId, Name, Description, Model, Sku, CompanyId, CategoryId, ProductTypeId, ProductSubcategoryId, IsActive
    FROM #ranked WHERE rn > 1;
OPEN split_cur;
FETCH NEXT FROM split_cur INTO @OrigId,@Name,@Desc,@Model,@Sku,@Co,@Cat,@Type,@Sub,@Active;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO Products (Name, Description, Model, Sku, CompanyId, CategoryId,
                          ProductTypeId, ProductSubcategoryId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (@Name, @Desc, @Model, @Sku, @Co, @Cat, @Type, @Sub, @Active, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @NewId = SCOPE_IDENTITY();
    SET @NewRecords += 1;

    UPDATE Certificates
    SET ProductId = @NewId
    WHERE ProductId = @OrigId
      AND ( (ProductTypeId = @Type) OR (ProductTypeId IS NULL AND @Type IS NULL) )
      AND ( (ProductSubcategoryId = @Sub) OR (ProductSubcategoryId IS NULL AND @Sub IS NULL) );
    SET @CertsRepointed += @@ROWCOUNT;

    FETCH NEXT FROM split_cur INTO @OrigId,@Name,@Desc,@Model,@Sku,@Co,@Cat,@Type,@Sub,@Active;
END
CLOSE split_cur; DEALLOCATE split_cur;

PRINT 'New product records created (non-primary combos): ' + CAST(@NewRecords AS VARCHAR(10));
PRINT 'Certificates re-pointed to new records:           ' + CAST(@CertsRepointed AS VARCHAR(10));

DECLARE @ProductsAfter INT = (SELECT COUNT(*) FROM Products WHERE IsDeleted = 0);
PRINT 'Products (not deleted) AFTER:  ' + CAST(@ProductsAfter AS VARCHAR(10))
    + '  (expected = before + new records)';

-- ---- Read-back: line items for any product NAME that now has >1 record (the splits)
SELECT p.Id AS ProductId, p.Name, p.ProductTypeId, pt.Name AS ProductTypeName,
       p.ProductSubcategoryId, sub.Name AS ProductSubcategoryName,
       (SELECT COUNT(*) FROM Certificates c WHERE c.ProductId = p.Id) AS CertCount
FROM   Products p
LEFT JOIN ProductTypes pt  ON pt.Id  = p.ProductTypeId
LEFT JOIN ProductTypes sub ON sub.Id = p.ProductSubcategoryId
WHERE  p.IsDeleted = 0
  AND  p.Name IN (SELECT Name FROM #ranked WHERE rn > 1)
ORDER BY p.Name, p.Id;

DROP TABLE #ranked;

/* DEFAULT = ROLLBACK. Verify the counts above against IDENTIFY, then flip to COMMIT and re-run. */
ROLLBACK TRAN;
-- COMMIT TRAN;
PRINT 'ROLLBACK (dry run). Change ROLLBACK TRAN -> COMMIT TRAN to apply.';

/* ============================================================================
   "CARLY TEST" RE-CATEGORIZATION — UPDATE (NOT DELETE / NOT DEACTIVATE)
   CR-019 / DEF-231 fallout cleanup. Re-files the 2 products and 4 certificates
   that sit under the default "Carly Test" category (Id 9) onto their correct
   category, recovered from the linked record where one exists. Per Shawn
   (2026-06-08): UPDATE the records, do NOT delete them, and LEAVE the
   "Carly Test" category in place (it should display fine once data is correct).

   Exact Ids verified read-only against prod 2026-06-08:
     Categories: 1=Chemicals and Raw Materials, 4=Consumer Products,
                 5=Electronics Computers and Accessories, 9=Carly Test (untouched)
     Type 4=Plastics, Subcategory 85=PS

   MAPPING (each target's source in parentheses)
     Product 133 "Test 2 Product"  cat 9 -> 1, type ->4 Plastics, sub ->85 PS  (copied from cert 26-0168, DEF-243-corrected)
     Product 131 "Eero test"       cat 9 -> 5 Electronics                       (chosen: Eero = Amazon Wi-Fi router)
     Cert    26-0159               cat 9 -> 4 Consumer Products                 (matches its product 116)
     Cert    26-0161               cat 9 -> 4 Consumer Products                 (matches its product 117)
     Cert    26-0163               cat 9 -> 4 Consumer Products                 (matches its product 127)
     Cert    26-0167 "Eero test"   cat 9 -> 5 Electronics                       (matches product 131's new category)

   SAFETY
     - Backup tables are created FIRST and persist regardless of rollback, so the
       change is fully reversible even after COMMIT (restore from *_bak_CR019_*).
     - All UPDATEs run inside a transaction that ROLLS BACK by default. Review the
       verification output, then change the final ROLLBACK to COMMIT and re-run.
     - Targets are explicit Ids / cert Numbers — no pattern matching.
     - Requires a DB backup + CR sign-off before COMMIT per project data-change rules.
   ============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;

/* --- 1) BACKUP the exact affected rows (persist outside the transaction) --- */
IF OBJECT_ID('dbo.Products_bak_CR019_20260608')     IS NOT NULL DROP TABLE dbo.Products_bak_CR019_20260608;
SELECT * INTO dbo.Products_bak_CR019_20260608     FROM dbo.Products      WHERE Id IN (131,133);
IF OBJECT_ID('dbo.Certificates_bak_CR019_20260608') IS NOT NULL DROP TABLE dbo.Certificates_bak_CR019_20260608;
SELECT * INTO dbo.Certificates_bak_CR019_20260608 FROM dbo.Certificates  WHERE Number IN ('26-0159','26-0161','26-0163','26-0167');
DECLARE @bakP INT = (SELECT COUNT(*) FROM dbo.Products_bak_CR019_20260608);
DECLARE @bakC INT = (SELECT COUNT(*) FROM dbo.Certificates_bak_CR019_20260608);
PRINT 'Backup rows -> Products_bak_CR019_20260608=' + CAST(@bakP AS VARCHAR(10))
    + ', Certificates_bak_CR019_20260608='          + CAST(@bakC AS VARCHAR(10));

BEGIN TRAN;

/* --- 2) Products mis-filed under Carly Test --- */
UPDATE dbo.Products SET CategoryId = 1, ProductTypeId = 4, ProductSubcategoryId = 85 WHERE Id = 133;  -- Test 2 Product -> Chemicals/Plastics/PS
UPDATE dbo.Products SET CategoryId = 5                                                WHERE Id = 131;  -- Eero test -> Electronics

/* --- 3) Certificates whose saved category is Carly Test (category only; products carry no type/sub) --- */
UPDATE dbo.Certificates SET ProductCategoryId = 4 WHERE Number IN ('26-0159','26-0161','26-0163');     -- -> Consumer Products
UPDATE dbo.Certificates SET ProductCategoryId = 5 WHERE Number = '26-0167';                            -- Eero -> Electronics

/* --- 4) Verify (runs before rollback/commit) --- */
SELECT 'PRODUCT' AS Kind, p.Id, p.Name, cat.Name AS NewCategory, tp.Name AS NewType, sb.Name AS NewSubcat
FROM dbo.Products p
LEFT JOIN dbo.ProductCategories cat ON cat.Id = p.CategoryId
LEFT JOIN dbo.ProductTypes      tp  ON tp.Id  = p.ProductTypeId
LEFT JOIN dbo.ProductTypes      sb  ON sb.Id  = p.ProductSubcategoryId
WHERE p.Id IN (131,133);

SELECT 'CERT' AS Kind, c.Number, cat.Name AS NewCategory
FROM dbo.Certificates c
LEFT JOIN dbo.ProductCategories cat ON cat.Id = c.ProductCategoryId
WHERE c.Number IN ('26-0159','26-0161','26-0163','26-0167');

/* --- 5) DEFAULT = ROLLBACK. Review the output, then comment ROLLBACK and uncomment COMMIT, re-run to apply. --- */
-- ROLLBACK TRAN;
COMMIT TRAN;
GO

/* ============================================================================
   PRODUCT MIS-CATEGORIZATION — IDENTIFICATION (READ-ONLY)
   Data-ops pass for the DEF-231 fallout (QC #186). NOT a defect fix — DEF-231
   is the code fix; this addresses rows already corrupted before that deploys.

   WHAT HAPPENED
   -------------
   The cert form's inline "Add New product" (saveInlineProduct) created products
   under the FIRST category returned by /Product/GetCategories (ordered by
   SortOrder) with NO product type / subcategory, ignoring the analyst's form
   selections. Selecting that product then auto-filled the (wrong) category onto
   the certificate too. So both the Product row AND the referencing Certificate's
   Product* FKs carry the wrong category and null type/subcategory.

   IMPORTANT: the analyst's INTENDED category/type/subcategory is NOT stored
   anywhere — it was overwritten. This script therefore only *identifies* suspect
   rows for human review. It does NOT and CANNOT auto-derive the correct values.

   FK MAP (verified against ApplicationDbContext, 2026-06-01)
     Products.CategoryId            -> ProductCategories.Id
     Products.ProductTypeId         -> ProductTypes.Id  (ParentId IS NULL  = top-level type)
     Products.ProductSubcategoryId  -> ProductTypes.Id  (ParentId IS NOT NULL = child)
     Certificates.ProductCategoryId / ProductTypeId / ProductSubcategoryId  (same targets)

   SAFETY
     - READ-ONLY. No INSERT/UPDATE/DELETE. Safe to run on the live/test DB.
     - This DB is the same instance the client tests (gccwebapp -> prod DB).
       Do not run the companion REMEDIATE script without a backup + CR approval.
     - Target: SQL Server. Run each section and review the output.
   ============================================================================ */

SET NOCOUNT ON;

/* ---------------------------------------------------------------------------
   SECTION 0 — The "default" category the bug used.
   /Product/GetCategories does: WHERE IsActive = 1 ORDER BY SortOrder.
   EF applies no explicit tiebreak; we add Id for determinism. CONFIRM this row
   matches what the app actually serves first (open /Admin/ProductTypes or call
   GET /Product/GetCategories and check element [0]).
   --------------------------------------------------------------------------- */
DECLARE @DefaultCategoryId INT;
SELECT TOP (1) @DefaultCategoryId = Id
FROM   ProductCategories
WHERE  IsActive = 1
ORDER  BY SortOrder, Id;

SELECT 'DEFAULT CATEGORY (bug target)' AS Note,
       pc.Id, pc.Name, pc.SortOrder, pc.IsActive
FROM   ProductCategories pc
WHERE  pc.Id = @DefaultCategoryId;

/* ---------------------------------------------------------------------------
   SECTION 1 — Suspect PRODUCTS (corruption fingerprint).
   Fingerprint = sits under the default category AND has neither type nor
   subcategory. This is NECESSARY but not SUFFICIENT: a product legitimately
   created as "category only" under the default category will also match.
   Review the list; the high-confidence subset is in SECTION 2.
   --------------------------------------------------------------------------- */
SELECT 'SUSPECT PRODUCTS (default cat + null type + null sub)' AS Bucket,
       p.Id              AS ProductId,
       p.Name            AS ProductName,
       p.CategoryId,
       cat.Name          AS CategoryName,
       p.ProductTypeId,
       p.ProductSubcategoryId,
       p.CompanyId,
       co.Name           AS CompanyName,
       p.CreatedAt,
       (SELECT COUNT(*) FROM Certificates c WHERE c.ProductId = p.Id) AS CertRefCount
FROM   Products p
JOIN   ProductCategories cat ON cat.Id = p.CategoryId
LEFT   JOIN Companies co     ON co.Id = p.CompanyId
WHERE  ISNULL(p.IsDeleted, 0) = 0
  AND  p.CategoryId           = @DefaultCategoryId
  AND  p.ProductTypeId        IS NULL
  AND  p.ProductSubcategoryId IS NULL
ORDER  BY p.CreatedAt DESC;

/* ---------------------------------------------------------------------------
   SECTION 2 — HIGH-CONFIDENCE: suspect product AND a certificate that mirrors
   the same default-category / null-type signature. The quick-add path is the
   only flow that produces BOTH the product and its cert in this exact state,
   so these are the strongest candidates for the DEF-231 bug specifically.
   --------------------------------------------------------------------------- */
SELECT 'HIGH-CONFIDENCE (product + cert both show default cat, no type/sub)' AS Bucket,
       c.Id              AS CertificateId,
       c.Number          AS CertNumber,
       c.Status,
       c.CreatedAt       AS CertCreatedAt,
       p.Id              AS ProductId,
       p.Name            AS ProductName,
       cat.Name          AS CategoryName,
       co.Name           AS CompanyName
FROM   Certificates c
JOIN   Products p            ON p.Id  = c.ProductId
JOIN   ProductCategories cat ON cat.Id = p.CategoryId
LEFT   JOIN Companies co     ON co.Id = c.CompanyId
WHERE  ISNULL(p.IsDeleted, 0) = 0
  AND  p.CategoryId           = @DefaultCategoryId
  AND  p.ProductTypeId        IS NULL
  AND  p.ProductSubcategoryId IS NULL
  AND  c.ProductCategoryId    = @DefaultCategoryId
  AND  c.ProductTypeId        IS NULL
  AND  c.ProductSubcategoryId IS NULL
ORDER  BY c.CreatedAt DESC;

/* ---------------------------------------------------------------------------
   SECTION 3 — CARLY TEST data (explicit client request to remove — QC #170
   thread 1). Adjust the LIKE patterns to match the actual test names you see.
   Lists the test categories/types plus any products + cert references that
   would block their deletion.
   --------------------------------------------------------------------------- */
DECLARE @TestNameLike NVARCHAR(100) = N'%carly%test%';   -- tune as needed

SELECT 'TEST CATEGORIES' AS Bucket, pc.Id, pc.Name, pc.SortOrder,
       (SELECT COUNT(*) FROM Products    p WHERE p.CategoryId         = pc.Id AND ISNULL(p.IsDeleted,0)=0) AS ProductsUnder,
       (SELECT COUNT(*) FROM Certificates c WHERE c.ProductCategoryId = pc.Id)                              AS CertsRefCategory,
       (SELECT COUNT(*) FROM ProductTypes pt WHERE pt.CategoryId      = pc.Id)                              AS TypesUnder
FROM   ProductCategories pc
WHERE  pc.Name LIKE @TestNameLike;

SELECT 'TEST PRODUCT TYPES / SUBCATS' AS Bucket, pt.Id, pt.Name, pt.ParentId, pt.CategoryId, pt.[Level],
       (SELECT COUNT(*) FROM Products    p WHERE p.ProductTypeId        = pt.Id OR p.ProductSubcategoryId = pt.Id) AS ProductsRef,
       (SELECT COUNT(*) FROM Certificates c WHERE c.ProductTypeId       = pt.Id OR c.ProductSubcategoryId = pt.Id) AS CertsRef
FROM   ProductTypes pt
WHERE  pt.Name LIKE @TestNameLike;

SELECT 'TEST PRODUCTS' AS Bucket, p.Id, p.Name, p.CategoryId, cat.Name AS CategoryName, p.CreatedAt,
       (SELECT COUNT(*) FROM Certificates c WHERE c.ProductId = p.Id) AS CertRefCount
FROM   Products p
JOIN   ProductCategories cat ON cat.Id = p.CategoryId
WHERE  ISNULL(p.IsDeleted,0) = 0
  AND (p.Name LIKE @TestNameLike OR cat.Name LIKE @TestNameLike);

/* ---------------------------------------------------------------------------
   SECTION 4 — Summary counts (paste into the data-ops ticket / CR).
   --------------------------------------------------------------------------- */
SELECT
  (SELECT COUNT(*) FROM Products p
     WHERE ISNULL(p.IsDeleted,0)=0 AND p.CategoryId=@DefaultCategoryId
       AND p.ProductTypeId IS NULL AND p.ProductSubcategoryId IS NULL)               AS SuspectProducts,
  (SELECT COUNT(*) FROM Certificates c JOIN Products p ON p.Id=c.ProductId
     WHERE ISNULL(p.IsDeleted,0)=0 AND p.CategoryId=@DefaultCategoryId
       AND p.ProductTypeId IS NULL AND p.ProductSubcategoryId IS NULL
       AND c.ProductCategoryId=@DefaultCategoryId AND c.ProductTypeId IS NULL
       AND c.ProductSubcategoryId IS NULL)                                            AS HighConfidenceCerts,
  (SELECT COUNT(*) FROM ProductCategories pc WHERE pc.Name LIKE @TestNameLike)        AS TestCategories,
  (SELECT COUNT(*) FROM ProductTypes pt      WHERE pt.Name LIKE @TestNameLike)        AS TestTypes;
GO

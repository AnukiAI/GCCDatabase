/* ============================================================================
   QC #243 diagnostic — "Product Types with no Product Category"
   Client report (7/30/26): "When I was going in to remove some of the product
   cat test options I found several that had Prod Types but not Prod Category.
   Not sure how this would be possible given you need the category to then
   define the associated types." (Image 108.)

   READ-ONLY. No INSERT/UPDATE/DELETE. Safe to run against the test environment
   or production.

   Why this file exists instead of a fix: the condition does NOT reproduce in
   the dev database, and `ProductTypes.CategoryId` is NOT NULL, so a product
   type with a literally-absent category cannot exist. Whatever the client saw
   has another cause. Query 3 is the leading candidate. Run all four and send
   back the output before anyone writes a fix.
   ============================================================================ */

/* ---------------------------------------------------------------------------
   1. Is CategoryId really NOT NULL on this database?
      Establishes whether the schema here matches dev. If IS_NULLABLE = YES,
      the environments have drifted and that alone could explain the report.
   --------------------------------------------------------------------------- */
SELECT 'Q1 schema' AS Check_, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ProductTypes' AND COLUMN_NAME = 'CategoryId';


/* ---------------------------------------------------------------------------
   2. Broken FK: CategoryId points at a ProductCategory row that no longer
      exists. Possible if a category was hard-deleted without an enforced FK.
      These types would render with a blank category.
   --------------------------------------------------------------------------- */
SELECT 'Q2 dangling FK' AS Check_, pt.Id, pt.Name, pt.Level, pt.ParentId, pt.CategoryId, pt.IsActive
FROM ProductTypes pt
LEFT JOIN ProductCategories pc ON pc.Id = pt.CategoryId
WHERE pc.Id IS NULL
ORDER BY pt.Level, pt.Id;


/* ---------------------------------------------------------------------------
   3. *** LEADING CANDIDATE *** Active product types whose category is INACTIVE.
      AdminService.GetProductTypesAsync builds the tree by iterating only
      categories where IsActive = 1, then attaching types to them. A type whose
      category has been deactivated is attached to nothing, so it vanishes from
      the management screen entirely — while still existing, still holding
      products/certificates, and still blocking deletes. That is a very good
      match for "Prod Types but not Prod Category".
   --------------------------------------------------------------------------- */
SELECT 'Q3 type under inactive category' AS Check_,
       pt.Id, pt.Name, pt.Level, pt.ParentId, pt.IsActive AS TypeActive,
       pc.Id AS CategoryId, pc.Name AS CategoryName, pc.IsActive AS CategoryActive,
       (SELECT COUNT(*) FROM Products  p WHERE p.ProductTypeId = pt.Id OR p.ProductSubcategoryId = pt.Id) AS ProductRefs,
       (SELECT COUNT(*) FROM Certificates c WHERE c.ProductTypeId = pt.Id OR c.ProductSubcategoryId = pt.Id) AS CertRefs
FROM ProductTypes pt
JOIN ProductCategories pc ON pc.Id = pt.CategoryId
WHERE pt.IsActive = 1 AND pc.IsActive = 0
ORDER BY pc.Name, pt.Level, pt.Name;


/* ---------------------------------------------------------------------------
   4. Hierarchy shape. Level is used inconsistently in the dev data: BOTH 0 and
      1 appear as "top-level type with no parent". The tree builder groups by
      ParentId and ignores Level, so this is currently harmless — but if this
      environment has a different distribution (e.g. non-zero Level rows with a
      NULL ParentId that the UI expects to be nested), that could also present
      as a type detached from its category.
   --------------------------------------------------------------------------- */
SELECT 'Q4 shape' AS Check_,
       pt.Level,
       COUNT(*)                                                   AS RowCount_,
       SUM(CASE WHEN pt.ParentId IS NULL THEN 1 ELSE 0 END)       AS WithNoParent,
       SUM(CASE WHEN pt.ParentId IS NOT NULL THEN 1 ELSE 0 END)   AS WithParent,
       SUM(CASE WHEN pt.IsActive = 0 THEN 1 ELSE 0 END)           AS Inactive
FROM ProductTypes pt
GROUP BY pt.Level
ORDER BY pt.Level;

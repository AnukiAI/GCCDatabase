/* ============================================================================
   CR-186 SPLIT — PRODUCT SPLIT BY (TYPE, SUBCATEGORY) — IDENTIFY (READ-ONLY)
   Date: 2026-06-15
   Run this FIRST. Writes NOTHING. Companion REMEDIATE script performs the split.

   CLIENT DECISION (2026-06-15, Shawn): a Product Management line item is keyed by
   (Company, Name, Category, Product Type, Subcategory). If a product's certificates
   span more than one (Type, Subcategory) combo, that product must become SEPARATE
   line items — one product record per combo — each with its own certificate count.

   WHAT THE SPLIT WILL DO (see REMEDIATE):
     - For each existing product whose certs use K distinct (Type, Subcategory) combos:
         * the product KEEPS its primary combo (the one with the most certs);
         * K-1 NEW product records are created (cloning Name/Company/Category/
           Description/SKU/Model), one per other combo;
         * each certificate is re-pointed to the record matching its own (Type, Subcat).
     - Products with a single combo (or zero certs) are NOT split — they just get that
       combo's Type/Subcategory set on the existing row. Certs with no Type/Subcategory
       form a valid (blank) combo.

   This IDENTIFY output is the worklist GCC reviews before REMEDIATE runs.
   Live business-data change => requires written GCC go-ahead + full DB backup
   before REMEDIATE ([[feedback_data_changes_require_cr]]). Target: SQL Server.
   Supersedes the withdrawn single-value backfill (2026-06-15-product-type-subcat-backfill-*).
   ============================================================================ */

SET NOCOUNT ON;

-- distinct (product, type, subcategory) combos that actually have certificates
IF OBJECT_ID('tempdb..#combos') IS NOT NULL DROP TABLE #combos;
SELECT c.ProductId, c.ProductTypeId, c.ProductSubcategoryId, COUNT(*) AS CertCount
INTO   #combos
FROM   Certificates c
GROUP BY c.ProductId, c.ProductTypeId, c.ProductSubcategoryId;

-- products whose certs span >1 combo (these split)
IF OBJECT_ID('tempdb..#splitting') IS NOT NULL DROP TABLE #splitting;
SELECT ProductId, COUNT(*) AS ComboCount
INTO   #splitting
FROM   #combos
GROUP BY ProductId
HAVING COUNT(*) > 1;

/* SECTION 1 — headline counts ------------------------------------------------ */
SELECT
    (SELECT COUNT(*) FROM Products WHERE IsDeleted = 0)                         AS TotalProducts,
    (SELECT COUNT(DISTINCT ProductId) FROM #combos)                            AS ProductsWithCerts,
    (SELECT COUNT(*) FROM #splitting)                                          AS ProductsThatSplit,
    (SELECT ISNULL(SUM(ComboCount - 1), 0) FROM #splitting)                    AS NewRecordsToCreate,
    (SELECT COUNT(*) FROM Products p
       WHERE p.IsDeleted = 0
         AND NOT EXISTS (SELECT 1 FROM #combos c WHERE c.ProductId = p.Id))    AS ProductsWithNoCerts_Untouched;

/* SECTION 2 — split worklist: each splitting product's combos + cert counts ---
   The combo with the most certs (top of each group) is the one the existing
   product row KEEPS; the rest become new records. */
SELECT  p.Id   AS ProductId,
        p.Name AS ProductName,
        cc.ProductTypeId,
        pt.Name  AS ProductTypeName,
        cc.ProductSubcategoryId,
        sub.Name AS ProductSubcategoryName,
        cc.CertCount,
        ROW_NUMBER() OVER (PARTITION BY cc.ProductId
                           ORDER BY cc.CertCount DESC,
                                    CASE WHEN cc.ProductTypeId IS NULL THEN 1 ELSE 0 END,
                                    cc.ProductTypeId, cc.ProductSubcategoryId) AS ComboRank_1IsKept
FROM    #combos cc
JOIN    #splitting s ON s.ProductId = cc.ProductId
JOIN    Products p   ON p.Id = cc.ProductId AND p.IsDeleted = 0
LEFT JOIN ProductTypes pt  ON pt.Id  = cc.ProductTypeId
LEFT JOIN ProductTypes sub ON sub.Id = cc.ProductSubcategoryId
ORDER BY p.Id, ComboRank_1IsKept;

/* SECTION 3 — single-combo / no-split products that just get Type/Subcat set --- */
SELECT  p.Id AS ProductId, p.Name AS ProductName,
        cc.ProductTypeId, pt.Name AS ProductTypeName,
        cc.ProductSubcategoryId, sub.Name AS ProductSubcategoryName, cc.CertCount
FROM    #combos cc
JOIN    Products p ON p.Id = cc.ProductId AND p.IsDeleted = 0
LEFT JOIN ProductTypes pt  ON pt.Id  = cc.ProductTypeId
LEFT JOIN ProductTypes sub ON sub.Id = cc.ProductSubcategoryId
WHERE   cc.ProductId NOT IN (SELECT ProductId FROM #splitting)
ORDER BY p.Id;

DROP TABLE #combos; DROP TABLE #splitting;

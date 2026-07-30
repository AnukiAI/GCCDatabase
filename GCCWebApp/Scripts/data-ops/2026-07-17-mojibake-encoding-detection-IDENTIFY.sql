/* =============================================================================
   Mojibake (character-encoding corruption) DETECTION  —  READ ONLY, NO CHANGES
   -----------------------------------------------------------------------------
   Origin : Client report 2026-07-17 — cert 26-9648 company exports as
            "BurtÃ¢â‚¬â„¢s Bees" instead of "Burt's Bees".
   Finding: The app's import/export code is UTF-8-clean (see observations_log.md
            2026-07-17). The corruption is baked into the STORED value — a curly
            apostrophe ' (U+2019, UTF-8 bytes E2 80 99) was decoded as Windows-1252
            during external source-data prep, before import.
                correct        Burt's Bees
                single moji.   Burtâ€™s Bees      (UTF-8 read as 1252 once)
                double moji.   BurtÃ¢â‚¬â„¢s Bees  (read as 1252 twice — reported)

   Purpose: NON-DESTRUCTIVE sweep to size the blast radius — every row/column
            across the schema whose stored text carries a mojibake signature.
            SELECT-only. Nothing is updated. Review the output, then a separate
            CR + REMEDIATE script performs the targeted fix (real records →
            data-change-requires-a-CR rule).

   How mojibake is detected (and why collation matters):
     UTF-8-read-as-1252 corruption ALWAYS introduces one of these lead chars:
        Ã  (U+00C3)  lead of C3-xx sequences (à á â ã ä ...)  — in ~all accent moji
        Â  (U+00C2)  lead of C2-xx sequences (nbsp © ® ° ...) — the classic "Â "
        â  (U+00E2)  lead of E2-xx sequences (smart quotes, — , ' , " , ™ , € , …)
     Legitimately-encoded accents store as their OWN single codepoint (é, ñ, ü) and
     do NOT contain Ã/Â/â, so this catches corruption without flagging real accents.

     CRITICAL: the default DB collation (SQL_Latin1_General_CP1_CI_AS) is accent-
     INSENSITIVE, so  LIKE N'%Ã%'  would match every 'a'/'A' and return the whole
     table. Every comparison below is forced to  COLLATE Latin1_General_BIN2
     (binary = exact codepoint) so only the true mojibake chars match.

   Caveat / triage: the WHERE is broad (Ã OR Â OR â) for full coverage. The
     MarkerFound column tells you WHICH signature hit so you can triage:
        contains Ã (esp. with â/€/™)      -> almost certainly corrupt (double-encoded)
        contains â€ / â‚¬ / â„¢            -> corrupt smart-quote/dash/™/€ (single)
        LONE  â  with no other marker      -> REVIEW (could be a real "â", e.g. a
                                              French word) before including in a fix.
     HexPreview shows the UTF-16 bytes of the stored value as confirmation.

   Run: SQLCMD / SSMS against the target DB (test env or a restored copy). Read-only.
   ============================================================================= */

SET NOCOUNT ON;

-- Reusable pieces (documentation only; inlined below so the script is copy-paste safe):
--   value  := CAST(<col> AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
--   hit    := value LIKE N'%Ã%' OR value LIKE N'%Â%' OR value LIKE N'%â%'


/* -----------------------------------------------------------------------------
   SECTION 0 — Direct confirmation of the reported row(s)
   Shows the stored company name(s) matching "Burt" plus the raw bytes.
   -------------------------------------------------------------------------- */
PRINT '=== SECTION 0: reported row (Burt*) ===';
SELECT
    co.Id,
    co.Name                                                              AS StoredName,
    CONVERT(varchar(400), CONVERT(varbinary(400), co.Name), 2)           AS HexBytes_UTF16LE
FROM dbo.Companies AS co
WHERE co.Name LIKE N'%Burt%';


/* -----------------------------------------------------------------------------
   SECTION 1 — Full schema sweep (one flat, actionable grid)
   One row per (table, column, record) that carries a mojibake signature.
   Normalized* computed columns are intentionally EXCLUDED: they are derived from
   their base column and regenerate automatically once the base column is fixed.
   -------------------------------------------------------------------------- */
PRINT '=== SECTION 1: full mojibake sweep ===';

WITH hits AS (

    /* Companies -------------------------------------------------------------- */
    SELECT 'Companies' AS TableName, v.ColumnName, c.Id AS RowId,
           CAST(c.Name AS NVARCHAR(255)) AS RowLabel, v.BadValue
    FROM dbo.Companies c
    CROSS APPLY (VALUES
        ('Name',        CAST(c.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('ContactInfo', CAST(c.ContactInfo AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Location',    CAST(c.Location    AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Country',     CAST(c.Country     AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* Products --------------------------------------------------------------- */
    SELECT 'Products', v.ColumnName, p.Id,
           CAST(p.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.Products p
    CROSS APPLY (VALUES
        ('Name',        CAST(p.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(p.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Model',       CAST(p.Model       AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Sku',         CAST(p.Sku         AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* Certificates ----------------------------------------------------------- */
    SELECT 'Certificates', v.ColumnName, ce.Id,
           CAST(ce.Number AS NVARCHAR(255)), v.BadValue
    FROM dbo.Certificates ce
    CROSS APPLY (VALUES
        ('Location',            CAST(ce.Location            AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Country',             CAST(ce.Country             AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('CertificationResults',CAST(ce.CertificationResults AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* CertificateProductIds  (ProductName = retailer titles, highest risk) ---- */
    SELECT 'CertificateProductIds', v.ColumnName, pid.Id,
           CAST(ce.Number AS NVARCHAR(255)), v.BadValue          -- label by parent cert #
    FROM dbo.CertificateProductIds pid
    LEFT JOIN dbo.Certificates ce ON ce.Id = pid.CertificateId
    CROSS APPLY (VALUES
        ('ProductName', CAST(pid.ProductName AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('ProductSku',  CAST(pid.ProductSku  AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pid.Description  AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* CertificationTypes ----------------------------------------------------- */
    SELECT 'CertificationTypes', v.ColumnName, ct.Id,
           CAST(ct.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.CertificationTypes ct
    CROSS APPLY (VALUES
        ('Name',        CAST(ct.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(ct.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('AmazonCode',  CAST(ct.AmazonCode  AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* ProductCategories ------------------------------------------------------ */
    SELECT 'ProductCategories', v.ColumnName, pc.Id,
           CAST(pc.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.ProductCategories pc
    CROSS APPLY (VALUES
        ('Name',        CAST(pc.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pc.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* ProductTypes ----------------------------------------------------------- */
    SELECT 'ProductTypes', 'Name', pt.Id,
           CAST(pt.Name AS NVARCHAR(255)),
           CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
    FROM dbo.ProductTypes pt
    WHERE (CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
       OR (CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
       OR (CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'

    UNION ALL
    /* ProductSubcategories --------------------------------------------------- */
    SELECT 'ProductSubcategories', v.ColumnName, ps.Id,
           CAST(ps.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.ProductSubcategories ps
    CROSS APPLY (VALUES
        ('Name',        CAST(ps.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(ps.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* Pathways --------------------------------------------------------------- */
    SELECT 'Pathways', v.ColumnName, pw.Id,
           CAST(pw.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.Pathways pw
    CROSS APPLY (VALUES
        ('Name',        CAST(pw.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pw.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* Partners --------------------------------------------------------------- */
    SELECT 'Partners', v.ColumnName, pn.Id,
           CAST(pn.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.Partners pn
    CROSS APPLY (VALUES
        ('Name',        CAST(pn.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pn.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('ContactInfo', CAST(pn.ContactInfo AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* Tags ------------------------------------------------------------------- */
    SELECT 'Tags', v.ColumnName, tg.Id,
           CAST(tg.Name AS NVARCHAR(255)), v.BadValue
    FROM dbo.Tags tg
    CROSS APPLY (VALUES
        ('Name',        CAST(tg.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(tg.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'

    UNION ALL
    /* CustomFieldValues (free-text values entered per certificate) ------------ */
    SELECT 'CustomFieldValues', 'TextValue', cfv.Id,
           CAST(cfv.Id AS NVARCHAR(255)),
           CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
    FROM dbo.CustomFieldValues cfv
    WHERE (CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
       OR (CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
       OR (CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'

    UNION ALL
    /* CustomFields (definition labels) --------------------------------------- */
    SELECT 'CustomFields', 'Name', cf.Id,
           CAST(cf.Name AS NVARCHAR(255)),
           CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
    FROM dbo.CustomFields cf
    WHERE (CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
       OR (CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
       OR (CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'
)
SELECT
    h.TableName,
    h.ColumnName,
    h.RowId,
    h.RowLabel,
    h.BadValue,
    /* which signature(s) fired — for triage */
    STUFF(
        CASE WHEN h.BadValue LIKE N'%Ã%'   THEN N' | Ã'   ELSE N'' END +
        CASE WHEN h.BadValue LIKE N'%Â%'   THEN N' | Â'   ELSE N'' END +
        CASE WHEN h.BadValue LIKE N'%â€%'  THEN N' | â€'  ELSE N'' END +
        CASE WHEN h.BadValue LIKE N'%â‚¬%' THEN N' | â‚¬' ELSE N'' END +
        CASE WHEN h.BadValue LIKE N'%â„¢%' THEN N' | â„¢' ELSE N'' END +
        CASE WHEN h.BadValue LIKE N'%â%'
              AND h.BadValue NOT LIKE N'%â€%'
              AND h.BadValue NOT LIKE N'%â‚¬%'
              AND h.BadValue NOT LIKE N'%â„¢%' THEN N' | â(lone-REVIEW)' ELSE N'' END
    , 1, 3, N'')                                            AS MarkerFound,
    /* double-encoded (Ã + â together) is the highest-confidence corruption */
    CASE WHEN h.BadValue LIKE N'%Ã%' AND h.BadValue LIKE N'%â%'
         THEN 'double-encoded'
         WHEN h.BadValue LIKE N'%Ã%' OR h.BadValue LIKE N'%Â%'
           OR h.BadValue LIKE N'%â€%' OR h.BadValue LIKE N'%â‚¬%' OR h.BadValue LIKE N'%â„¢%'
         THEN 'single-encoded'
         ELSE 'review' END                                 AS Severity,
    CONVERT(varchar(400), CONVERT(varbinary(400), h.BadValue), 2) AS HexPreview_UTF16LE
FROM hits h
ORDER BY
    CASE WHEN h.BadValue LIKE N'%Ã%' AND h.BadValue LIKE N'%â%' THEN 0 ELSE 1 END, -- double first
    h.TableName, h.ColumnName, h.RowId;


/* -----------------------------------------------------------------------------
   SECTION 2 — Summary counts per table + column (blast-radius sizing)
   -------------------------------------------------------------------------- */
PRINT '=== SECTION 2: summary (rows affected per table/column) ===';

WITH hits AS (
    SELECT 'Companies' AS TableName, v.ColumnName, v.BadValue
    FROM dbo.Companies c
    CROSS APPLY (VALUES
        ('Name',        CAST(c.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('ContactInfo', CAST(c.ContactInfo AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Location',    CAST(c.Location    AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Country',     CAST(c.Country     AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'Products', v.ColumnName, v.BadValue
    FROM dbo.Products p
    CROSS APPLY (VALUES
        ('Name',        CAST(p.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(p.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Model',       CAST(p.Model       AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Sku',         CAST(p.Sku         AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'Certificates', v.ColumnName, v.BadValue
    FROM dbo.Certificates ce
    CROSS APPLY (VALUES
        ('Location',            CAST(ce.Location             AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Country',             CAST(ce.Country              AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('CertificationResults',CAST(ce.CertificationResults AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'CertificateProductIds', v.ColumnName, v.BadValue
    FROM dbo.CertificateProductIds pid
    CROSS APPLY (VALUES
        ('ProductName', CAST(pid.ProductName AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('ProductSku',  CAST(pid.ProductSku  AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pid.Description  AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'CertificationTypes', v.ColumnName, v.BadValue
    FROM dbo.CertificationTypes ct
    CROSS APPLY (VALUES
        ('Name',        CAST(ct.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(ct.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('AmazonCode',  CAST(ct.AmazonCode  AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'ProductCategories', v.ColumnName, v.BadValue
    FROM dbo.ProductCategories pc
    CROSS APPLY (VALUES
        ('Name',        CAST(pc.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pc.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'ProductTypes', 'Name', CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
    FROM dbo.ProductTypes pt
    WHERE (CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
       OR (CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
       OR (CAST(pt.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'
    UNION ALL
    SELECT 'ProductSubcategories', v.ColumnName, v.BadValue
    FROM dbo.ProductSubcategories ps
    CROSS APPLY (VALUES
        ('Name',        CAST(ps.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(ps.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'Pathways', v.ColumnName, v.BadValue
    FROM dbo.Pathways pw
    CROSS APPLY (VALUES
        ('Name',        CAST(pw.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pw.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'Partners', v.ColumnName, v.BadValue
    FROM dbo.Partners pn
    CROSS APPLY (VALUES
        ('Name',        CAST(pn.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(pn.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('ContactInfo', CAST(pn.ContactInfo AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'Tags', v.ColumnName, v.BadValue
    FROM dbo.Tags tg
    CROSS APPLY (VALUES
        ('Name',        CAST(tg.Name        AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2),
        ('Description', CAST(tg.Description AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2)
    ) v(ColumnName, BadValue)
    WHERE v.BadValue LIKE N'%Ã%' OR v.BadValue LIKE N'%Â%' OR v.BadValue LIKE N'%â%'
    UNION ALL
    SELECT 'CustomFieldValues', 'TextValue', CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
    FROM dbo.CustomFieldValues cfv
    WHERE (CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
       OR (CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
       OR (CAST(cfv.TextValue AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'
    UNION ALL
    SELECT 'CustomFields', 'Name', CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2
    FROM dbo.CustomFields cf
    WHERE (CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Ã%'
       OR (CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%Â%'
       OR (CAST(cf.Name AS NVARCHAR(MAX)) COLLATE Latin1_General_BIN2) LIKE N'%â%'
)
SELECT
    h.TableName,
    h.ColumnName,
    COUNT(*)                                                            AS RowsAffected,
    SUM(CASE WHEN h.BadValue LIKE N'%Ã%' AND h.BadValue LIKE N'%â%' THEN 1 ELSE 0 END) AS DoubleEncoded,
    SUM(CASE WHEN NOT (h.BadValue LIKE N'%Ã%' AND h.BadValue LIKE N'%â%') THEN 1 ELSE 0 END) AS SingleOrReview
FROM hits h
GROUP BY h.TableName, h.ColumnName
ORDER BY RowsAffected DESC, h.TableName, h.ColumnName;

PRINT '=== END. READ-ONLY: nothing was modified. ===';

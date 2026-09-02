/* ============================================================================
   Normalise Certificates.CertificationResults to explicit "XX%" strings   CR-042
   ----------------------------------------------------------------------------
   WHY. The field is free text. The client's source workbooks recorded results in
   Excel cells carrying the number format '0%', so the cell DISPLAYED "23%" while
   HOLDING 0.23. The migration read the true cell value (correct for a numeric
   cell), so the database stores 0.23 and the app renders "23%" via
   Extensions/DisplayFormatters.FormatCertificationResults.

   Nothing is broken -- but the stored value and the published value are written in
   different dialects, and the display rule has to GUESS which dialect a bare number
   is in. That guess is right for every value in today's data and wrong the first
   time someone records a result that is not a percentage. Writing the percentage
   explicitly removes the guess for every existing row, and matches the 158 ASIN
   certificates the client already corrected by hand on 2026-08-26.

   THIS CHANGES NO DISPLAYED VALUE AND NO SORT ORDER.
     * Display: FormatCertificationResults returns a %-bearing string unchanged, so
       "23%" renders exactly as 0.23 did.
     * Sort: the persisted computed column CertificationResultsNumeric strips '%'
       before casting -- verified from sys.computed_columns 2026-09-02 -- so 0.23
       and "23%" both compute to 23. The assertion below proves this per row rather
       than trusting it.

   RULE -- mirrors FormatCertificationResults exactly:
     contains '%'      -> untouched (159 rows)
     non-numeric       -> untouched (28 rows: Silver, Gold, +Vantage Vinyl Verified)
     numeric in [0,1]  -> x100, append '%'   (0.23 -> "23%", 1 -> "100%"  <- DEF-383)
     numeric > 1       -> append '%'         (none exist today)

   Rows whose percentage would NOT be a whole number are deliberately SKIPPED
   rather than rounded, and reported at the end. Today there are none (verified by
   enumeration, not sampling), but silently rounding a future 0.235 would be a
   fabrication of the same class DEF-353 exists to prevent.

   RUN WITH: sqlcmd -S <server> -d <database> -I -b -i this-file.sql
   -I (QUOTED_IDENTIFIER ON) is REQUIRED -- CR-187 persisted computed columns.
   Idempotent: re-running is a no-op, because every rewritten row now contains '%'.
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @errmsg nvarchar(400);

-- Snapshot every populated row's sort value BEFORE, so the assertion can compare per row.
IF OBJECT_ID('tempdb..#Before') IS NOT NULL DROP TABLE #Before;
SELECT Id,
       CertificationResults        AS OldText,
       CertificationResultsNumeric AS OldNumeric
INTO   #Before
FROM   dbo.Certificates
WHERE  CertificationResults IS NOT NULL AND LTRIM(RTRIM(CertificationResults)) <> '';

DECLARE @populated int = (SELECT COUNT(*) FROM #Before);
PRINT 'populated rows before: ' + CAST(@populated AS varchar(10));

BEGIN TRANSACTION;

UPDATE c
SET    CertificationResults =
         CONVERT(varchar(20),
           CONVERT(int,
             CASE WHEN TRY_CONVERT(decimal(18,6), c.CertificationResults) BETWEEN 0 AND 1
                  THEN TRY_CONVERT(decimal(18,6), c.CertificationResults) * 100
                  ELSE TRY_CONVERT(decimal(18,6), c.CertificationResults)
             END)) + '%'
FROM   dbo.Certificates c
WHERE  c.CertificationResults IS NOT NULL
  AND  LTRIM(RTRIM(c.CertificationResults)) <> ''
  AND  c.CertificationResults NOT LIKE '%[%]%'
  AND  TRY_CONVERT(decimal(18,6), c.CertificationResults) IS NOT NULL
       -- only whole percentages; anything else is left for a human
  AND  (CASE WHEN TRY_CONVERT(decimal(18,6), c.CertificationResults) BETWEEN 0 AND 1
             THEN TRY_CONVERT(decimal(18,6), c.CertificationResults) * 100
             ELSE TRY_CONVERT(decimal(18,6), c.CertificationResults)
        END)
       = ROUND(CASE WHEN TRY_CONVERT(decimal(18,6), c.CertificationResults) BETWEEN 0 AND 1
                    THEN TRY_CONVERT(decimal(18,6), c.CertificationResults) * 100
                    ELSE TRY_CONVERT(decimal(18,6), c.CertificationResults)
               END, 0);

DECLARE @updated int = @@ROWCOUNT;
PRINT 'rows rewritten       : ' + CAST(@updated AS varchar(10));

----------------------------------------------------------------- assertions --
-- 1. No row lost or gained.
IF (SELECT COUNT(*) FROM dbo.Certificates
    WHERE CertificationResults IS NOT NULL AND LTRIM(RTRIM(CertificationResults)) <> '') <> @populated
BEGIN
    SET @errmsg = 'ABORT: the populated-row count changed.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

-- 2. THE IMPORTANT ONE: the sort value must be identical for every single row.
--    If the computed column disagreed with the C# formatter, this catches it here
--    rather than as silently mis-sorted certificates in production (DEF-383).
IF EXISTS (SELECT 1
           FROM   #Before b
           JOIN   dbo.Certificates c ON c.Id = b.Id
           WHERE  ISNULL(c.CertificationResultsNumeric, -999999) <> ISNULL(b.OldNumeric, -999999))
BEGIN
    SET @errmsg = 'ABORT: CertificationResultsNumeric changed for at least one row - '
                + 'display and sort would disagree. Nothing committed.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

-- 3. Nothing numeric-and-bare should remain.
DECLARE @leftover int = (SELECT COUNT(*) FROM dbo.Certificates
                         WHERE CertificationResults IS NOT NULL
                           AND LTRIM(RTRIM(CertificationResults)) <> ''
                           AND CertificationResults NOT LIKE '%[%]%'
                           AND TRY_CONVERT(decimal(18,6), CertificationResults) IS NOT NULL);
PRINT 'bare numerics left   : ' + CAST(@leftover AS varchar(10)) + '  (non-whole percentages, skipped on purpose)';
IF @leftover > 0
    SELECT Id, CertificationResults AS skipped_value
    FROM   dbo.Certificates
    WHERE  CertificationResults IS NOT NULL
      AND  CertificationResults NOT LIKE '%[%]%'
      AND  TRY_CONVERT(decimal(18,6), CertificationResults) IS NOT NULL;

COMMIT TRANSACTION;

PRINT '';
PRINT 'COMMITTED. Post-state:';
SELECT CASE WHEN CertificationResults LIKE '%[%]%' THEN 'percentage string'
            ELSE 'non-numeric text' END AS shape,
       COUNT(*) AS certs
FROM   dbo.Certificates
WHERE  CertificationResults IS NOT NULL AND LTRIM(RTRIM(CertificationResults)) <> ''
GROUP BY CASE WHEN CertificationResults LIKE '%[%]%' THEN 'percentage string'
              ELSE 'non-numeric text' END;

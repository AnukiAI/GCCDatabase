/* ============================================================================
   Set CertificationResults = 'Multiple' on the three GAF renewals     CR-044
   ----------------------------------------------------------------------------
   WHY. The 2026-09-03 batch-2 load treated 25-0497 / 25-8163 / 25-8164 as
   in-place RENEWALS and, by design, moved only Status / EffectiveDate /
   ExpirationDate / Location. The client's sheet also carries a Certification
   Results value for all three -- 'Multiple' -- and it was never written. Found
   by 2026-09-04-certresults-audit.py (verdict DB_BLANK, 3 rows).

   NOT A DEFECT. The loader did exactly what it documented; the field was simply
   outside the four it moved. Correct code + a value that never landed = data ops
   under the CR that authorised the load.

   THE VALUE COMES FROM THE CLIENT, NOT FROM US. 'Multiple' is the literal cell
   contents of 'Certification Results (not needed for CSF, CEF, LCA, GCC Audited,
   MIR, SMP)' on 'Final Upload List 9.3.26' for each of the three rows, read
   2026-09-04. It is a TEXT cell, so the sheet's '0%' number format does not apply
   and the displayed value is the string itself -- nothing is being converted.
   'Multiple' is already an established stored value: 26-0412 carries it today.

   NOTHING ELSE MOVES. Dates, status, location and country were verified 2026-09-04
   to already match the workbook exactly for all three certs; they are not touched.

   SCOPE: exactly 3 rows, and the script aborts if it is anything but 3.
   Idempotent: re-running is a no-op because the WHERE clause requires an empty
   field. Reversible: see the UNDO block at the foot of this file.

   RUN WITH: sqlcmd -S <server> -d <database> -I -b -i this-file.sql
   -I (QUOTED_IDENTIFIER ON) is REQUIRED -- CR-187 persisted computed columns.
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @errmsg nvarchar(400);

DECLARE @targets TABLE (Number nvarchar(50) PRIMARY KEY, Val nvarchar(50));
INSERT INTO @targets (Number, Val) VALUES
    (N'25-0497', N'Multiple'),
    (N'25-8163', N'Multiple'),
    (N'25-8164', N'Multiple');

--------------------------------------------------------------- pre-flight --
PRINT 'BEFORE:';
SELECT c.Number, c.Status, c.EffectiveDate, c.ExpirationDate,
       ISNULL(c.CertificationResults, N'(null)') AS CertificationResults,
       ISNULL(CONVERT(varchar(20), c.CertificationResultsNumeric), '(null)') AS SortValue
FROM   dbo.Certificates c
JOIN   @targets t ON t.Number = c.Number
ORDER  BY c.Number;

-- 1. All three must exist, exactly once each.
DECLARE @found int = (SELECT COUNT(*) FROM dbo.Certificates c JOIN @targets t ON t.Number = c.Number);
IF @found <> 3
BEGIN
    SET @errmsg = 'ABORT: expected exactly 3 matching certificates, found '
                + CAST(@found AS varchar(10)) + '. Nothing changed.';
    RAISERROR(@errmsg, 16, 1); RETURN;
END;

-- 2. Refuse to overwrite a value somebody has already put there.
IF EXISTS (SELECT 1 FROM dbo.Certificates c JOIN @targets t ON t.Number = c.Number
           WHERE c.CertificationResults IS NOT NULL
             AND LTRIM(RTRIM(c.CertificationResults)) <> N'')
BEGIN
    PRINT 'NOTHING TO DO: at least one row already has a Certification Results value.';
    PRINT 'This script only fills an EMPTY field. Re-run is a no-op by design.';
    RETURN;
END;

------------------------------------------------------------------ update --
BEGIN TRANSACTION;

UPDATE c
SET    c.CertificationResults = t.Val,
       c.UpdatedAt            = SYSUTCDATETIME()
FROM   dbo.Certificates c
JOIN   @targets t ON t.Number = c.Number
WHERE  c.CertificationResults IS NULL
   OR  LTRIM(RTRIM(c.CertificationResults)) = N'';

DECLARE @updated int = @@ROWCOUNT;
PRINT '';
PRINT 'rows updated: ' + CAST(@updated AS varchar(10));

IF @updated <> 3
BEGIN
    SET @errmsg = 'ABORT: updated ' + CAST(@updated AS varchar(10))
                + ' rows, expected 3. Rolled back.';
    RAISERROR(@errmsg, 16, 1); ROLLBACK TRANSACTION; RETURN;
END;

--------------------------------------------------------------- assertions --
-- 3. Every target now holds exactly the client's string.
IF EXISTS (SELECT 1 FROM dbo.Certificates c JOIN @targets t ON t.Number = c.Number
           WHERE ISNULL(c.CertificationResults, N'') <> t.Val)
BEGIN
    SET @errmsg = 'ABORT: a target row does not hold the expected value. Rolled back.';
    RAISERROR(@errmsg, 16, 1); ROLLBACK TRANSACTION; RETURN;
END;

-- 4. 'Multiple' is non-numeric, so the sort column MUST stay NULL. If it did not,
--    the computed column and the display rule would disagree (DEF-383's lesson).
IF EXISTS (SELECT 1 FROM dbo.Certificates c JOIN @targets t ON t.Number = c.Number
           WHERE c.CertificationResultsNumeric IS NOT NULL)
BEGIN
    SET @errmsg = 'ABORT: a non-numeric value produced a non-NULL sort value. Rolled back.';
    RAISERROR(@errmsg, 16, 1); ROLLBACK TRANSACTION; RETURN;
END;

-- 5. Blast radius: nothing outside the three rows may have been touched.
DECLARE @otherBlank int = (SELECT COUNT(*) FROM dbo.Certificates
                           WHERE Number NOT IN (SELECT Number FROM @targets)
                             AND CertificationResults = N'Multiple');
PRINT 'other certs holding ''Multiple'' (unchanged, informational): '
      + CAST(@otherBlank AS varchar(10));

COMMIT TRANSACTION;

PRINT '';
PRINT 'COMMITTED. AFTER:';
SELECT c.Number, c.Status, c.EffectiveDate, c.ExpirationDate,
       c.CertificationResults,
       ISNULL(CONVERT(varchar(20), c.CertificationResultsNumeric), '(null)') AS SortValue,
       c.UpdatedAt
FROM   dbo.Certificates c
JOIN   @targets t ON t.Number = c.Number
ORDER  BY c.Number;

/* ---------------------------------------------------------------- UNDO ----
   Restores the pre-change state (field empty). Run only if this change is
   being reversed; it does NOT touch dates, status or location.

   UPDATE dbo.Certificates
   SET    CertificationResults = NULL, UpdatedAt = SYSUTCDATETIME()
   WHERE  Number IN (N'25-0497', N'25-8163', N'25-8164')
     AND  CertificationResults = N'Multiple';
---------------------------------------------------------------------------- */

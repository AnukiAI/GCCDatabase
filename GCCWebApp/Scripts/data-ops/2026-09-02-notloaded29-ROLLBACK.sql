/* ============================================================================
   ROLLBACK for the "Not Loaded (29)" top-up load        CR-041 (expanded scope)
   ----------------------------------------------------------------------------
   Undoes the load run against gccwebap_certificates on 2026-09-02 13:21:10 UTC,
   which inserted 29 certificates, 24 products and 9 companies.

   The generator writes NO AuditHistory and NO ImportBatches row (decision: Shawn,
   2026-08-13), so there is no UndoImport for this load. This script is the undo.
   The belt-and-braces alternative is the logical backup taken immediately before:
       Scripts/data-ops/_backups/20260902-pre-notloaded29/

   Every row this load created carries the SAME CreatedAt stamp, which is the key
   used here -- narrower and safer than an Id range on its own, but BOTH are
   asserted so a coincidental match cannot widen the delete.

     Certificates : Id 5711-5739   (29)
     Products     : Id 3441-3464   (24)
     Companies    : Id  302- 310    (9)

   RUN WITH: sqlcmd -S <server> -d <database> -I -b -i this-file.sql
   -I (QUOTED_IDENTIFIER ON) is REQUIRED -- CR-187 persisted computed columns.

   Every FK referencing Certificates is ON DELETE CASCADE (verified 2026-09-02 on
   TEST), so no explicit child deletes are needed. NOTE: CertificateCountries does
   NOT exist on TEST -- CR-037 is still only in the working tree -- so it is
   deliberately not named here; naming it would fail with "invalid object name"
   at exactly the moment this script is needed.
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @stamp datetime2(7) = '2026-09-02T13:21:10.4362886';
DECLARE @errmsg nvarchar(400);
DECLARE @n int;

BEGIN TRANSACTION;

------------------------------------------------------------------ preflight --
SELECT @n = COUNT(*) FROM dbo.Certificates WHERE CreatedAt = @stamp AND Id BETWEEN 5711 AND 5739;
IF @n <> 29
BEGIN
    SET @errmsg = 'ABORT: expected 29 certificates from this load, found ' + CAST(@n AS nvarchar(10))
                + '. Refusing to delete a set that is not the one this script was written for.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

-- Nothing outside the load may share the stamp.
SELECT @n = COUNT(*) FROM dbo.Certificates WHERE CreatedAt = @stamp AND Id NOT BETWEEN 5711 AND 5739;
IF @n > 0
BEGIN
    SET @errmsg = 'ABORT: ' + CAST(@n AS nvarchar(10)) + ' certificate(s) share the load stamp but sit outside Id 5711-5739.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

--------------------------------------------------------------------- delete --
DELETE FROM dbo.Certificates WHERE CreatedAt = @stamp AND Id BETWEEN 5711 AND 5739;
PRINT 'certificates deleted: ' + CAST(@@ROWCOUNT AS varchar(10)) + '  (expected 29)';

-- Products created by the load, only if nothing else came to reference them.
DELETE p FROM dbo.Products p
WHERE  p.CreatedAt = @stamp AND p.Id BETWEEN 3441 AND 3464
  AND  NOT EXISTS (SELECT 1 FROM dbo.Certificates c WHERE c.ProductId = p.Id);
PRINT 'products deleted    : ' + CAST(@@ROWCOUNT AS varchar(10)) + '  (expected 24)';

-- Companies created by the load, only if now unreferenced.
DELETE co FROM dbo.Companies co
WHERE  co.CreatedAt = @stamp AND co.Id BETWEEN 302 AND 310
  AND  NOT EXISTS (SELECT 1 FROM dbo.Certificates c WHERE c.CompanyId = co.Id)
  AND  NOT EXISTS (SELECT 1 FROM dbo.Products p  WHERE p.CompanyId  = co.Id);
PRINT 'companies deleted   : ' + CAST(@@ROWCOUNT AS varchar(10)) + '  (expected 9)';

----------------------------------------------------------------- assertions --
SELECT @n = COUNT(*) FROM dbo.Certificates;
PRINT 'certificates now: ' + CAST(@n AS varchar(10)) + '  (expected 5702)';
IF @n <> 5702
BEGIN
    SET @errmsg = 'ABORT: certificate count is ' + CAST(@n AS nvarchar(10))
                + ', expected 5702. Other writes happened since the load; rolling back so nothing is lost.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

COMMIT TRANSACTION;
PRINT 'ROLLBACK COMPLETE - database returned to its pre-load state (5,702 certificates).';

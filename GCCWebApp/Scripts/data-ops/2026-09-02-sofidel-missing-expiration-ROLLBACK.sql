/*==============================================================================
  ROLLBACK for the 2026-09-02 Sofidel load (23-0358, 25-0351)
  ------------------------------------------------------------------------------
  The migration SQL generator writes NO AuditHistory and NO ImportBatches row, so
  there is no UndoImport for that load path. This file is the undo.

  Safe to run ONLY against a database whose pre-load state was:
      Certificates 5,702 rows   MAX(Id) = 5702
      Products                  MAX(Id) = 3439
  captured on TEST (gccwebap_certificates) 2026-09-02 08:08 before the load.
  The guards below re-assert that, so running this against any other state aborts
  instead of deleting someone else's rows.

  RUN WITH:  sqlcmd -S <server> -d <database> -I -b -i this-file.sql
==============================================================================*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @errmsg nvarchar(400);

BEGIN TRANSACTION;

--------------------------------------------------------------------- guards --
-- Only the two certificates this load created may be removed, and only if they
-- really are new rows (Id above the pre-load high-water mark).
IF EXISTS (SELECT 1 FROM dbo.Certificates
           WHERE Number IN ('23-0358','25-0351') AND Id <= 5702)
BEGIN
    SET @errmsg = 'ABORT: a certificate numbered 23-0358 or 25-0351 exists at an Id <= 5702, so it predates this load. Refusing to delete.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

DECLARE @certs int = (SELECT COUNT(*) FROM dbo.Certificates WHERE Number IN ('23-0358','25-0351'));
PRINT 'certificates matched for removal: ' + CAST(@certs AS varchar(10));

------------------------------------------------------------------- children --
-- No explicit child deletes. Every FK referencing Certificates is ON DELETE
-- CASCADE -- verified against this database 2026-09-02:
--   CertificateCertificationTypeSubcategories, CertificateDocuments,
--   CertificatePartners, CertificateProductIds, CertificateStatusHistory,
--   CertificateTag, CustomFieldValues
-- so the parent delete below removes them.
--
-- Deliberately NOT referencing CertificateCountries: that table is CR-037 and
-- does not exist on TEST yet (the migration is still only in the working tree).
-- Naming it here would make this rollback fail with "invalid object name" at the
-- exact moment it is needed. If CR-037 ships before this is run, its FK will be
-- CASCADE like the rest and still needs no line here.

-------------------------------------------------------------- certificates --
DELETE FROM dbo.Certificates WHERE Number IN ('23-0358','25-0351');
PRINT 'certificates deleted: ' + CAST(@@ROWCOUNT AS varchar(10));

------------------------------------------------------------------ products --
-- The load created the Sofidel "Plastic Film Packaging" product. Remove it only
-- if it is above the pre-load high-water mark AND nothing else now points at it.
DELETE p
FROM dbo.Products p
JOIN dbo.Companies co ON co.Id = p.CompanyId
WHERE co.Name LIKE '%Sofidel%'
  AND p.Name = 'Plastic Film Packaging'
  AND p.Id > 3439
  AND NOT EXISTS (SELECT 1 FROM dbo.Certificates c WHERE c.ProductId = p.Id);
PRINT 'products deleted: ' + CAST(@@ROWCOUNT AS varchar(10));

----------------------------------------------------------------- assertions --
DECLARE @remaining int = (SELECT COUNT(*) FROM dbo.Certificates WHERE Number IN ('23-0358','25-0351'));
IF @remaining <> 0
BEGIN
    SET @errmsg = 'ABORT: ' + CAST(@remaining AS nvarchar(10)) + ' target certificate(s) still present after delete.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

DECLARE @total int = (SELECT COUNT(*) FROM dbo.Certificates);
PRINT 'certificates now: ' + CAST(@total AS varchar(10)) + '  (expected 5702)';
IF @total <> 5702
BEGIN
    SET @errmsg = 'ABORT: certificate count is ' + CAST(@total AS nvarchar(10)) + ', expected 5702. Other writes happened since the load; rolling back so nothing is lost.';
    RAISERROR(@errmsg,16,1); ROLLBACK TRANSACTION; RETURN;
END;

COMMIT TRANSACTION;
PRINT 'ROLLBACK COMPLETE - database returned to the pre-load 5,702-certificate state.';

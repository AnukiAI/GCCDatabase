/*
================================================================================
  DEF-338 / QC #193 — remove the import-toast verification test data
================================================================================
  Date:     2026-08-04
  Author:   Anuki
  Database: GCCDatabase_Dev  (DEV ONLY — do NOT run against GCCDatabase or prod)
  Purpose:  Deletes the throwaway certificates created while verifying the
            "Import completed successfully!" toast end to end (DEF-338).

  These rows were created BY the verification, not by the client. They are the
  only artifacts of that test:

      26-9501  DEF338 Toast Test Widget A
      26-9502  DEF338 Toast Test Widget B
      26-9503  DEF338 Toast Test Widget C   (leak-check run)

  All seven Certificates FK children are ON DELETE CASCADE
  (CertificateCertificationTypeSubcategories, CertificateDocuments,
   CertificatePartners, CertificateProductIds, CertificateStatusHistory,
   CertificateTag, CustomFieldValues), so deleting the certificate rows removes
   their children automatically. The three test Products are deleted explicitly
   afterwards because Products is the PARENT side of that relationship.

  Run with sqlcmd -I (CR-187 persisted computed columns require QUOTED_IDENTIFIER
  ON — see reference_testenv_sql_delete_gotchas).

      sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P '<pw>' -I ^
             -i Scripts/data-ops/2026-08-04-def338-import-toast-test-cleanup.sql

  Safe to skip entirely — this is dev test data, harmless if left in place.
================================================================================
*/

SET NOCOUNT ON;

DECLARE @certNumbers TABLE (Number nvarchar(50) PRIMARY KEY);
INSERT INTO @certNumbers (Number) VALUES ('26-9501'), ('26-9502'), ('26-9503');

DECLARE @productNames TABLE (Name nvarchar(200) PRIMARY KEY);
INSERT INTO @productNames (Name) VALUES
    ('DEF338 Toast Test Widget A'),
    ('DEF338 Toast Test Widget B'),
    ('DEF338 Toast Test Widget C');

PRINT '--- BEFORE ---';
SELECT c.Id, c.Number, c.Status, p.Name AS ProductName
FROM   Certificates c
LEFT   JOIN Products p ON p.Id = c.ProductId
WHERE  c.Number IN (SELECT Number FROM @certNumbers);

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @productIds TABLE (Id int PRIMARY KEY);
    INSERT INTO @productIds (Id)
    SELECT DISTINCT p.Id
    FROM   Products p
    WHERE  p.Name IN (SELECT Name FROM @productNames);

    -- Certificates first; the seven child tables cascade.
    DELETE FROM Certificates
    WHERE  Number IN (SELECT Number FROM @certNumbers);
    PRINT 'Certificates deleted: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' (expected 3)';

    -- Guard: never delete a product still referenced by a certificate we did not create.
    DELETE FROM Products
    WHERE  Id IN (SELECT Id FROM @productIds)
      AND  NOT EXISTS (SELECT 1 FROM Certificates c WHERE c.ProductId = Products.Id);
    PRINT 'Products deleted: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' (expected 3)';

    COMMIT TRANSACTION;
    PRINT 'COMMITTED.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ROLLED BACK — ' + ERROR_MESSAGE();
END CATCH

PRINT '--- AFTER (both counts must be 0) ---';
SELECT 'CertsRemaining'  AS Check_, COUNT(*) AS Cnt FROM Certificates WHERE Number IN (SELECT Number FROM @certNumbers)
UNION ALL
SELECT 'ProductsRemaining', COUNT(*) FROM Products WHERE Name IN (SELECT Name FROM @productNames);

/* CR-056 rollback - removes certificate 26-0234 added by 2026-09-30-cr056-load-26-0234.py.
   Guarded: aborts unless exactly ONE 26-0234 exists and it was created on 2026-09-30 (UTC).
   (CreatedBy is an int user Id, so the load leaves it NULL like the batch loads did.)
   The uploaded PDF/preview files stay in App_Data\uploads (harmless orphans).
   sqlcmd -C -S tcp:68.71.129.120,1533 -d gccwebap_certificates -U <user> -P <pw> -I -b -i 2026-09-30-cr056-ROLLBACK.sql */
SET XACT_ABORT ON;
SELECT DB_NAME() AS db;
BEGIN TRAN;
DECLARE @id int, @n int;
SELECT @n = COUNT(*), @id = MAX(Id) FROM Certificates WHERE Number = '26-0234' AND CAST(CreatedAt AS date) IN ('2026-09-30','2026-10-01');
IF @n <> 1 OR (SELECT COUNT(*) FROM Certificates WHERE Number = '26-0234') <> 1
    THROW 50010, 'CR-056 rollback: expected exactly one CR-056 row for 26-0234 - nothing deleted', 1;
DELETE FROM CertificateTag           WHERE CertificatesId = @id;
DELETE FROM CertificateCountries     WHERE CertificateId  = @id;
DELETE FROM CertificateDocuments     WHERE CertificateId  = @id;
DELETE FROM CertificateStatusHistory WHERE CertificateId  = @id;
DELETE FROM Certificates             WHERE Id = @id;
COMMIT;
SELECT COUNT(*) AS remaining_26_0234 FROM Certificates WHERE Number = '26-0234';

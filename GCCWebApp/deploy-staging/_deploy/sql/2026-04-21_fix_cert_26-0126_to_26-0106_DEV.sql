-- =============================================================================
-- Data Correction: Cert 26-0126 should be 26-0106 (DEV)
-- Date: 2026-04-21
-- Author: Shawn West / Anuki Consulting
-- =============================================================================
-- BEFORE STATE (Dev):
--   Id=51  Number=26-0106  CertificateFile=NULL        (correct number, no PDF)
--   Id=54  Number=26-0126  CertificateFile=26-0126.pdf (wrong number, has PDF + 1 ASIN)
--
-- Duplicate was created when PDF uploaded with wrong cert number.
-- Fix: merge Id=54's PDF + ASIN into Id=51, delete Id=54.
-- =============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;

-- 1. Move the ASIN (and any other product IDs) from cert 54 → cert 51
UPDATE CertificateProductIds
SET CertificateId = 51, UpdatedAt = SYSUTCDATETIME()
WHERE CertificateId = 54;

-- 2. Update cert 51 to point to the (soon-to-be-renamed) PDF
UPDATE Certificates
SET CertificateFile = 'uploads/certificates/26-0106_20260420_163333.pdf',
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = 51 AND Number = '26-0106';

-- 3. Safety check: cert 54 should now have zero dependent rows
DECLARE @Orphans INT = (
    SELECT COUNT(*) FROM CertificateProductIds WHERE CertificateId = 54
) + (
    SELECT COUNT(*) FROM CertificateTag WHERE CertificatesId = 54
) + (
    SELECT COUNT(*) FROM CertificatePartners WHERE CertificatesId = 54
) + (
    SELECT COUNT(*) FROM CertificateDocuments WHERE CertificateId = 54
) + (
    SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId = 54
);

IF @Orphans > 0
BEGIN
    RAISERROR('Cert Id=54 still has %d dependent rows; aborting.', 16, 1, @Orphans);
    ROLLBACK;
    RETURN;
END;

-- 4. Delete the duplicate cert
DELETE FROM Certificates WHERE Id = 54 AND Number = '26-0126';

-- 5. Verify post-state
SELECT Id, Number, CertificateFile, Status FROM Certificates WHERE Number IN ('26-0126','26-0106');
SELECT CertificateId, Id, ProductIdType, ProductIdValue FROM CertificateProductIds WHERE CertificateId IN (51, 54);

COMMIT;
PRINT 'Dev merge complete.';

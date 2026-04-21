-- =============================================================================
-- Data Correction: Cert 26-0126 should be 26-0106 (TEST/PROD - gccwebapp.com)
-- Date: 2026-04-21
-- Author: Shawn West / Anuki Consulting
-- Server: tcp:68.71.129.120,1533  DB: gccwebap_certificates
-- =============================================================================
-- BEFORE STATE (Test/Prod):
--   Id=68  Number=26-0126  CertificateFile=uploads/certificates/26-0126_20260420_173717.pdf
--
-- No duplicate here — simple rename of Number + CertificateFile path.
-- The PDF file on the server must be renamed separately:
--   26-0126_20260420_173717.pdf → 26-0106_20260420_173717.pdf
-- =============================================================================

SET XACT_ABORT ON;
BEGIN TRAN;

-- Pre-check: confirm the single expected row exists
IF NOT EXISTS (SELECT 1 FROM Certificates WHERE Id = 68 AND Number = '26-0126')
BEGIN
    RAISERROR('Expected row Id=68 Number=26-0126 not found. Aborting.', 16, 1);
    ROLLBACK;
    RETURN;
END;

IF EXISTS (SELECT 1 FROM Certificates WHERE Number = '26-0106')
BEGIN
    RAISERROR('Cert 26-0106 already exists in Test/Prod. Manual review required.', 16, 1);
    ROLLBACK;
    RETURN;
END;

-- Rename cert number and update file path
UPDATE Certificates
SET Number = '26-0106',
    CertificateFile = 'uploads/certificates/26-0106_20260420_173717.pdf',
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = 68 AND Number = '26-0126';

-- Verify post-state
SELECT Id, Number, CertificateFile, Status FROM Certificates WHERE Number IN ('26-0126','26-0106');
SELECT CertificateId, Id, ProductIdType, ProductIdValue FROM CertificateProductIds WHERE CertificateId = 68;

COMMIT;
PRINT 'Test/Prod rename complete. Remember to rename the PDF on the server filesystem.';

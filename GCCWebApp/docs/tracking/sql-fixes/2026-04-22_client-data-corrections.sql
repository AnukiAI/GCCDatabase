-- =============================================================================
-- Client data corrections — 2026-04-22
-- Source: Kao/Amazon Devices/Sonos spreadsheet provided by Shawn
-- =============================================================================
-- Scope:
--   (1) CertificationTypes.AmazonCode — short codes → Amazon long-form strings
--       for 3 types confirmed in client data (SEP/RC/CES).
--   (2) Cert 26-0151 — CertificationResults "1" → "100", PathwayId → 3.
--
-- Confirmed from client data:
--   26-0151: 100%, "Renewable electricity usage"
--   26-0106: 69% (already correct), pathway blank (already NULL)
--   26-0157: 17% (already correct), pathway blank (already NULL)
--   26-0146 (partial): amazonCode long-form (covered by CT update)
--
-- NOT in this script (no client data yet):
--   26-0146 / 26-0147 / 26-0150 — pathway & certResults beyond what we have
--   Other 15 CertificationType AmazonCodes — awaiting client mapping
-- =============================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- 1. CertificationType AmazonCode long-form (applies to all certs tagged with these types)
UPDATE CertificationTypes SET AmazonCode = 'greencircle_sustainable_energy_practices'
WHERE Id = 17 AND Name = 'Sustainable Energy Practices' AND AmazonCode = 'SEP';

UPDATE CertificationTypes SET AmazonCode = 'greencircle_recycled_content'
WHERE Id = 15 AND Name = 'Recycled Content' AND AmazonCode = 'RC';

UPDATE CertificationTypes SET AmazonCode = 'greencircle_energy_savings'
WHERE Id = 4 AND Name = 'Certified Energy Savings' AND AmazonCode = 'CES';

-- 2. Cert 26-0151 data corrections
UPDATE Certificates
SET CertificationResults = '100',
    PathwayId = 3,  -- Renewable electricity usage
    UpdatedAt = SYSUTCDATETIME()
WHERE Number = '26-0151';

-- 3. Verification
SELECT Id, Name, AmazonCode FROM CertificationTypes WHERE Id IN (4, 15, 17) ORDER BY Id;

SELECT c.Id, c.Number, c.CertificationResults, c.PathwayId, p.Name AS PathwayName
FROM Certificates c LEFT JOIN Pathways p ON c.PathwayId = p.Id
WHERE c.Number = '26-0151';

COMMIT;
PRINT 'Corrections applied.';

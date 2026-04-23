-- QC #123: Deactivate duplicate CertificationType rows
-- Database QC Log 4.22.26 — Carly flagged public filter /api/certificate-tiles/filters
-- returning duplicate certification type names.
--
-- Root cause: 10 CertificationType records exist with duplicate names (same name, different Id).
-- All certificates reference the root (ParentId IS NULL) version. The children/dup records
-- (ParentId=7 "Certified Sustainability Facts", plus two under ParentId=45/47) have
-- ZERO cert references.
--
-- Verification query (run first on prod to confirm same state):
-- SELECT ct.Id, ct.Name, ct.ParentId, COUNT(c.Id) AS CertCount
-- FROM CertificationTypes ct
-- LEFT JOIN Certificates c ON c.CertificationTypeId = ct.Id
-- WHERE ct.Id IN (49,50,53,54,55,59,65,71,73,78)
-- GROUP BY ct.Id, ct.Name, ct.ParentId
-- ORDER BY ct.Name, ct.Id;
--
-- Expected: all 10 rows return CertCount = 0 before applying the UPDATE.

SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

UPDATE CertificationTypes
SET IsActive = 0
WHERE Id IN (49, 50, 53, 54, 55, 59, 65, 71, 73, 78);

-- Expected: 10 rows affected
PRINT CONCAT('Rows deactivated: ', @@ROWCOUNT);

-- Sanity check — confirm no active duplicates remain
SELECT Name, COUNT(*) AS Cnt
FROM CertificationTypes
WHERE IsActive = 1
GROUP BY Name
HAVING COUNT(*) > 1;
-- Expected: 0 rows

COMMIT TRANSACTION;
GO

-- ROLLBACK (if needed):
-- UPDATE CertificationTypes SET IsActive = 1 WHERE Id IN (49,50,53,54,55,59,65,71,73,78);

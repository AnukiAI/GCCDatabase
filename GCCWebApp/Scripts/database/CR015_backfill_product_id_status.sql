-- ============================================================================
-- CR-015 — Backfill CertificateProductId.Status from parent Certificate.Status
-- ============================================================================
-- Rule: Product ID status is auto-derived from the parent cert's status.
--   "Active"   when parent cert.Status IN ('Client Approved - Paid','Republished')
--   "Inactive" otherwise (Draft / Hold / Expired / Confidential / Not Paid /
--              Expired - No Annual Verification)
--
-- Run on each environment ONCE after deploying the CR-015 application code.
-- Idempotent — re-running is a no-op once data is already in sync.
--
-- Pre-flight (recommended):
--   SELECT c.Status, p.Status, COUNT(*) AS Rows
--   FROM CertificateProductIds p JOIN Certificates c ON p.CertificateId = c.Id
--   GROUP BY c.Status, p.Status
--   ORDER BY c.Status, p.Status;
--
-- Post-flight verification:
--   SELECT TOP 100 c.Number, c.Status AS CertStatus, p.ProductIdType, p.ProductIdValue, p.Status AS PidStatus
--   FROM CertificateProductIds p JOIN Certificates c ON p.CertificateId = c.Id
--   WHERE p.Status NOT IN ('Active','Inactive')
--      OR (c.Status IN ('Client Approved - Paid','Republished') AND p.Status <> 'Active')
--      OR (c.Status NOT IN ('Client Approved - Paid','Republished') AND p.Status <> 'Inactive');
--   -- Expected: 0 rows after backfill.
-- ============================================================================

SET NOCOUNT ON;

BEGIN TRANSACTION CR015_Backfill;

-- 1) Active product IDs: parent cert is in a public/active status
UPDATE p
SET    p.Status    = 'Active',
       p.UpdatedAt = SYSUTCDATETIME()
FROM   CertificateProductIds p
JOIN   Certificates c ON p.CertificateId = c.Id
WHERE  c.Status IN ('Client Approved - Paid', 'Republished')
  AND  p.Status <> 'Active';

DECLARE @ActiveRows INT = @@ROWCOUNT;

-- 2) Inactive product IDs: parent cert is in any other status
UPDATE p
SET    p.Status    = 'Inactive',
       p.UpdatedAt = SYSUTCDATETIME()
FROM   CertificateProductIds p
JOIN   Certificates c ON p.CertificateId = c.Id
WHERE  c.Status NOT IN ('Client Approved - Paid', 'Republished')
  AND  p.Status <> 'Inactive';

DECLARE @InactiveRows INT = @@ROWCOUNT;

PRINT CONCAT('CR-015 backfill: ', @ActiveRows, ' rows -> Active, ', @InactiveRows, ' rows -> Inactive.');

COMMIT TRANSACTION CR015_Backfill;

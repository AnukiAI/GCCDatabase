-- ============================================================================
-- CR-043 — Product-identifier status backfill (DATA, runs AFTER schema migrations)
-- ----------------------------------------------------------------------------
-- CR-043 widened the "this certificate is in force" rule from PublicStatuses
-- {Paid, Republished} to BadgeableStatuses {Paid, Confidential, Republished}.
--
-- CertificateProductIds.Status is DERIVED from the parent certificate's status
-- (CR-015) but is written ON SAVE, not computed by the database. So every
-- identifier row saved under the old rule still reads 'Inactive' even though its
-- certificate is now in force.
--
-- REQUIRED in the same deploy step as the code. Without it those identifiers ship
-- inside the "Active Product Identifiers" export and go to Amazon while the
-- Product Identifiers grid labels each one Inactive — a contradiction visible to
-- the client on the screen they use.
--
-- Idempotent: the WHERE clause excludes rows already 'Active', so a re-run
-- updates 0 rows. Safe to run before or after the app pool recycle.
--
-- Deliberately NO effective/expiration window here — DeriveStatusFromCert has
-- none either. Status follows the parent certificate's STATUS alone; the window
-- is applied at query time by the export and the partner API. A window in the
-- stored value would go stale every midnight.
--
-- Scope on the target measured 2026-09-08: 420 rows across 13 certificates.
-- The standalone version with dry-run, before/after enumeration and abort
-- assertions is Scripts/data-ops/2026-09-08-CR043-productid-status-backfill.sql;
-- use that one for manual runs. This copy is the automated deploy path.
-- ============================================================================

UPDATE  pid
SET     pid.Status    = N'Active',
        pid.UpdatedAt = SYSUTCDATETIME()
FROM    CertificateProductIds pid
INNER JOIN Certificates c ON c.Id = pid.CertificateId
WHERE   c.Status = N'Client Approved - Confidential'
  AND   pid.Status <> N'Active';

-- Guard against the reverse mistake: identifiers attached to a certificate that is
-- NOT badgeable must never read 'Active'. This is not a no-op safety net — it is
-- the assertion that proves the UPDATE above did not spill through its JOIN, which
-- a row count alone could never show.
IF EXISTS (
    SELECT 1
    FROM   CertificateProductIds pid
    INNER JOIN Certificates c ON c.Id = pid.CertificateId
    WHERE  c.Status NOT IN (N'Client Approved - Paid',
                            N'Client Approved - Confidential',
                            N'Republished')
      AND  pid.Status = N'Active'
)
BEGIN
    RAISERROR (N'CR-043 backfill: product identifiers on non-badgeable certificates are marked Active. Investigate before serving traffic.', 16, 1);
END

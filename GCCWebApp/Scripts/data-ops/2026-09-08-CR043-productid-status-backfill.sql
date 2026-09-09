/* ============================================================================
   CR-043 — backfill CertificateProductIds.Status for Confidential certificates
   ----------------------------------------------------------------------------
   WHY THIS EXISTS
   CertificateProductId.Status is derived from the parent certificate's status
   (CR-015) and is written ON SAVE, not computed by the database. CR-043 widened
   the "Active" rule from PublicStatuses to BadgeableStatuses (adding
   Client Approved - Confidential), so every product-identifier row that was
   saved under the old rule still reads 'Inactive'.

   Left alone, those identifiers appear in the "Active Product Identifiers"
   export and go to Amazon while their own Status column says Inactive — the
   contradiction lands on the client's Product Identifiers grid.

   RUN THIS IN THE SAME RELEASE AS THE CR-043 CODE. Order does not matter
   (the code change does not depend on the data, and vice versa), but a gap
   between them is the window in which the two disagree.

   SAFETY
   - Transactional. Runs as a dry run by default and ROLLS BACK.
     Set @Commit = 1 to persist.
   - Touches ONE column on rows whose parent certificate is Confidential.
     Never touches Certificates, never deletes anything.
   - Idempotent: re-running after a commit updates 0 rows.
   - Prints before/after counts and the affected certificate list, not a
     summary line — enumerate, don't sample.

   USAGE
     sqlcmd -C -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P <pw> -I -b ^
            -i Scripts/data-ops/2026-09-08-CR043-productid-status-backfill.sql
   (add -v Commit=1 semantics by editing @Commit below; kept as a literal so a
   stray variable cannot silently commit)
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;   -- <<< set to 1 to persist

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. Before picture — every affected certificate, listed, not counted
-- ---------------------------------------------------------------------------
PRINT '--- BEFORE: product-identifier rows on Confidential certificates ---';

SELECT  c.Number                AS CertificateNumber,
        c.Status                AS CertificateStatus,
        c.EffectiveDate,
        c.ExpirationDate,
        pid.Status              AS ProductIdStatus,
        COUNT(*)                AS RowsFound
FROM    CertificateProductIds pid
JOIN    Certificates c ON c.Id = pid.CertificateId
WHERE   c.Status = N'Client Approved - Confidential'
GROUP BY c.Number, c.Status, c.EffectiveDate, c.ExpirationDate, pid.Status
ORDER BY c.Number, pid.Status;

DECLARE @ToUpdate int =
(
    SELECT COUNT(*)
    FROM   CertificateProductIds pid
    JOIN   Certificates c ON c.Id = pid.CertificateId
    WHERE  c.Status = N'Client Approved - Confidential'
      AND  pid.Status <> N'Active'
);

PRINT CONCAT('Rows that will move Inactive -> Active: ', @ToUpdate);

-- ---------------------------------------------------------------------------
-- 2. The update
--
--    Deliberately keyed on the CERTIFICATE status, not on pid.Status, so it
--    expresses the same rule the code does (DeriveStatusFromCert) rather than
--    a one-off correction. Any row already 'Active' is filtered out so the
--    row count printed above is the row count actually changed.
--
--    Note it does NOT apply the effective/expiration window: neither does
--    DeriveStatusFromCert. Status follows the parent certificate's status
--    alone; the window is applied at query time by the export and the API
--    (WhereEffectiveDateReached + the expiration sweep). Adding a window here
--    would make the stored value drift out of date every midnight.
-- ---------------------------------------------------------------------------
UPDATE  pid
SET     pid.Status    = N'Active',
        pid.UpdatedAt = SYSUTCDATETIME()
FROM    CertificateProductIds pid
JOIN    Certificates c ON c.Id = pid.CertificateId
WHERE   c.Status = N'Client Approved - Confidential'
  AND   pid.Status <> N'Active';

DECLARE @Updated int = @@ROWCOUNT;
PRINT CONCAT('Rows updated: ', @Updated);

IF @Updated <> @ToUpdate
BEGIN
    PRINT 'ABORT: updated row count does not match the pre-count.';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- ---------------------------------------------------------------------------
-- 3. Assertions that must hold before this is allowed to commit
-- ---------------------------------------------------------------------------

-- 3a. No Confidential certificate is left with an Inactive identifier.
IF EXISTS (
    SELECT 1
    FROM   CertificateProductIds pid
    JOIN   Certificates c ON c.Id = pid.CertificateId
    WHERE  c.Status = N'Client Approved - Confidential'
      AND  pid.Status <> N'Active'
)
BEGIN
    PRINT 'ABORT: Confidential certificates still hold non-Active identifiers.';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- 3b. NOTHING outside Confidential changed. Every other status must still
--     agree with DeriveStatusFromCert. This is the assertion that proves the
--     UPDATE's JOIN did not spill — equal counts would not have proved it.
IF EXISTS (
    SELECT 1
    FROM   CertificateProductIds pid
    JOIN   Certificates c ON c.Id = pid.CertificateId
    WHERE  c.Status NOT IN (N'Client Approved - Paid',
                            N'Client Approved - Confidential',
                            N'Republished')
      AND  pid.Status = N'Active'
)
BEGIN
    PRINT 'ABORT: identifiers on non-badgeable certificates are marked Active.';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- ---------------------------------------------------------------------------
-- 4. After picture
-- ---------------------------------------------------------------------------
PRINT '--- AFTER: product-identifier status by certificate status ---';

SELECT  c.Status        AS CertificateStatus,
        pid.Status      AS ProductIdStatus,
        COUNT(*)        AS RowsFound
FROM    CertificateProductIds pid
JOIN    Certificates c ON c.Id = pid.CertificateId
GROUP BY c.Status, pid.Status
ORDER BY c.Status, pid.Status;

-- ---------------------------------------------------------------------------
-- 5. Commit gate
-- ---------------------------------------------------------------------------
IF @Commit = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'COMMITTED.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'DRY RUN — rolled back. Set @Commit = 1 to persist.';
END

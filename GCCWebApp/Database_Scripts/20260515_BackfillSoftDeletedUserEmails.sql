-- Data backfill: soft-deleted Users from before the 5/12 deploy.
--
-- Not a code defect — the deployed DeleteUserAsync (DEF-137 fix, 2026-05-12)
-- correctly suffixes Email/Username with "__deleted__<id>__<yyyyMMddHHmmss>"
-- on every new soft delete so the unique slot frees up. The issue is historical
-- data: users deleted BEFORE 5/12 never got that rewrite, so their original
-- Email/Username still occupies the unfiltered UNIQUE index slot
-- (Migrations\20251015162223_InitialCreate_SqlServer_Final.cs
-- IX_Users_Email / IX_Users_Username) and blocks any re-create with the same
-- address. This one-time script reconciles those rows with the new convention.
--
-- What this script does:
--   1. Diagnostic SELECT  — counts and lists the soft-deleted rows that will be touched.
--   2. Backfill UPDATE    — applies the suffix using DeletedAt where available,
--                           else falls back to CreatedAt, else GETUTCDATE().
--   3. Verification SELECT — re-runs the count to confirm zero rows remain.
--
-- Idempotency:
--   The WHERE clause excludes any row whose Email already contains the marker,
--   so re-running this script after the first run is a no-op.
--
-- Audit:
--   Original email values are preserved in AuditHistory CREATE/DELETE rows for
--   each affected user (per AuditService snapshots). This script does NOT
--   modify AuditHistory.
--
-- How to run:
--   1. Open in SSMS against the GCC production database
--   2. Run Step 1 (diagnostic) and review the rows
--   3. Run Step 2 inside the transaction; either COMMIT or ROLLBACK
--   4. Run Step 3 to verify

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- =============================================================================
-- STEP 1 — DIAGNOSTIC (read-only)
-- =============================================================================
PRINT '--- Step 1: Diagnostic ---';

DECLARE @AffectedCount INT;
SELECT @AffectedCount = COUNT(*)
FROM dbo.Users
WHERE IsDeleted = 1
  AND Email NOT LIKE '%__deleted__%'
  AND Email NOT LIKE '%__cleanup__%';

PRINT 'Soft-deleted users with unsuffixed Email: ' + CAST(@AffectedCount AS NVARCHAR(10));

SELECT
    Id,
    Email AS CurrentEmail,
    Username AS CurrentUsername,
    DeletedAt,
    CreatedAt
FROM dbo.Users
WHERE IsDeleted = 1
  AND Email NOT LIKE '%__deleted__%'
  AND Email NOT LIKE '%__cleanup__%'
ORDER BY DeletedAt DESC, Id DESC;

-- =============================================================================
-- STEP 2 — BACKFILL (transactional; COMMIT or ROLLBACK at the bottom)
-- =============================================================================
PRINT '--- Step 2: Backfill ---';

BEGIN TRANSACTION;

-- Compute the suffix per row: __deleted__<id>__<yyyyMMddHHmmss>
-- DeletedAt is used when set; otherwise CreatedAt; otherwise current UTC.
UPDATE u
SET
    Email    = u.Email    + '__deleted__' + CAST(u.Id AS NVARCHAR(10)) + '__' +
               FORMAT(COALESCE(u.DeletedAt, u.CreatedAt, GETUTCDATE()), 'yyyyMMddHHmmss'),
    Username = u.Username + '__deleted__' + CAST(u.Id AS NVARCHAR(10)) + '__' +
               FORMAT(COALESCE(u.DeletedAt, u.CreatedAt, GETUTCDATE()), 'yyyyMMddHHmmss')
FROM dbo.Users u
WHERE u.IsDeleted = 1
  AND u.Email NOT LIKE '%__deleted__%'
  AND u.Email NOT LIKE '%__cleanup__%';

DECLARE @RowsUpdated INT = @@ROWCOUNT;
PRINT 'Rows updated: ' + CAST(@RowsUpdated AS NVARCHAR(10));

-- Sanity check inside the transaction — make sure no unfiltered unique conflicts
-- were created.  If two soft-deleted users share the same Id+timestamp (cannot
-- happen — Id is the PK), this would surface here.
IF EXISTS (
    SELECT Email, COUNT(*) AS c
    FROM dbo.Users
    GROUP BY Email
    HAVING COUNT(*) > 1
)
BEGIN
    PRINT 'ERROR: duplicate Email values detected after backfill. Rolling back.';
    ROLLBACK TRANSACTION;
END
ELSE IF EXISTS (
    SELECT Username, COUNT(*) AS c
    FROM dbo.Users
    GROUP BY Username
    HAVING COUNT(*) > 1
)
BEGIN
    PRINT 'ERROR: duplicate Username values detected after backfill. Rolling back.';
    ROLLBACK TRANSACTION;
END
ELSE
BEGIN
    PRINT 'Sanity checks passed. Committing.';
    COMMIT TRANSACTION;
END

-- =============================================================================
-- STEP 3 — VERIFICATION
-- =============================================================================
PRINT '--- Step 3: Verification ---';

SELECT @AffectedCount = COUNT(*)
FROM dbo.Users
WHERE IsDeleted = 1
  AND Email NOT LIKE '%__deleted__%'
  AND Email NOT LIKE '%__cleanup__%';

PRINT 'Remaining unsuffixed soft-deleted users (should be 0): ' + CAST(@AffectedCount AS NVARCHAR(10));

-- Spot-check a few rewritten rows
SELECT TOP 10
    Id,
    Email,
    Username,
    IsDeleted,
    DeletedAt
FROM dbo.Users
WHERE IsDeleted = 1
ORDER BY DeletedAt DESC, Id DESC;

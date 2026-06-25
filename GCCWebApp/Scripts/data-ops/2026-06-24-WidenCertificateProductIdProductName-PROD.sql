-- =============================================================================
-- Gate 7 (DEF-239): prod never auto-migrates. Run this WITH the deploy that carries
-- migration 20260624142128_WidenCertificateProductIdProductName.
--
-- What it does: widens CertificateProductIds.ProductName from nvarchar(200) to
-- nvarchar(1000) (nullable). Needed because imported/long product-identifier names
-- exceed 200 chars and would otherwise fail to insert/update.
--
-- Idempotent + safe to re-run: only alters the column if it isn't already 1000-wide,
-- and only stamps __EFMigrationsHistory if the migration isn't already recorded.
-- Wrapped in a transaction. NON-destructive (widening only; no data loss).
-- =============================================================================

-- DEF-252 hardening: set both session options ON before the transaction so a future
-- run can't half-apply (QUOTED_IDENTIFIER matters if a filtered index/computed column
-- is ever involved; XACT_ABORT rolls the whole batch back on any error).
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

-- 1) Widen the column only if it is not already nvarchar(1000).
--    sys.columns.max_length for nvarchar = 2 bytes/char, so nvarchar(1000) => 2000.
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    JOIN sys.objects o ON o.object_id = c.object_id
    WHERE o.name = N'CertificateProductIds'
      AND c.name = N'ProductName'
      AND c.max_length <> 2000
)
BEGIN
    ALTER TABLE [CertificateProductIds]
        ALTER COLUMN [ProductName] nvarchar(1000) NULL;
    PRINT 'ProductName widened to nvarchar(1000).';
END
ELSE
BEGIN
    PRINT 'ProductName already nvarchar(1000) (or table/column missing) - no change.';
END;
GO

-- 2) Record the migration so EF considers it applied (prod does not auto-migrate).
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624142128_WidenCertificateProductIdProductName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260624142128_WidenCertificateProductIdProductName', N'8.0.11');
    PRINT 'Migration history row inserted.';
END
ELSE
BEGIN
    PRINT 'Migration history row already present - no change.';
END;
GO

COMMIT;
GO

-- Gate 7 post-deploy verification (active). Expect: type_name=nvarchar, max_length=2000
-- (nvarchar(1000) = 2 bytes/char), is_nullable=1, and migration_recorded=1.
SELECT
    c.name              AS column_name,
    t.name              AS type_name,
    c.max_length        AS max_length_bytes,
    c.is_nullable       AS is_nullable,
    CASE WHEN EXISTS (
        SELECT 1 FROM [__EFMigrationsHistory]
        WHERE [MigrationId] = N'20260624142128_WidenCertificateProductIdProductName'
    ) THEN 1 ELSE 0 END AS migration_recorded
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'CertificateProductIds') AND c.name = N'ProductName';
GO

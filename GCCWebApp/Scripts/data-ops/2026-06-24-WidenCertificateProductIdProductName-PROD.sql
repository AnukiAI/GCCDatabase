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

-- Verify (optional):
-- SELECT c.name, t.name AS type_name, c.max_length, c.is_nullable
-- FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
-- WHERE c.object_id = OBJECT_ID(N'CertificateProductIds') AND c.name = N'ProductName';

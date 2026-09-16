-- Schema hardening: convert Users.Email and Users.Username unique indexes to
-- filtered unique indexes (WHERE [IsDeleted] = 0).
--
-- Why:
--   After the DEF-137 fix (deployed 2026-05-12) DeleteUserAsync suffixes
--   Email/Username with __deleted__<id>__<ts> on every soft delete to free the
--   unique slot. That works, but it depends on the suffix logic never regressing.
--   This change excludes soft-deleted rows from the uniqueness constraint at
--   the schema level, so the suffix becomes a nice-to-have for audit
--   reconstruction rather than load-bearing for the create-after-delete path.
--   Same pattern already in use on Pathways.Name
--   (Migrations\20260213005208_FixPathwayUniqueIndexForSoftDelete.cs).
--
-- When to run:
--   Bundle alongside the next app deploy that includes commit containing
--   Migrations\20260515185538_FixUserUniqueIndexesForSoftDelete.cs. Order does
--   not matter — the new app code is compatible with both index shapes.
--
-- Pre-req:
--   Database_Scripts\20260515_BackfillSoftDeletedUserEmails.sql has already
--   been run against this DB (already done on prod 2026-05-15). The new
--   filtered indexes don't require the backfill to have happened, but running
--   in this order keeps the DB consistent with the suffix convention the new
--   code writes.
--
-- Idempotent — script is guarded by __EFMigrationsHistory check, safe to re-run.
-- ============================================================================

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515185538_FixUserUniqueIndexesForSoftDelete'
)
BEGIN
    DROP INDEX [IX_Users_Email] ON [Users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515185538_FixUserUniqueIndexesForSoftDelete'
)
BEGIN
    DROP INDEX [IX_Users_Username] ON [Users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515185538_FixUserUniqueIndexesForSoftDelete'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515185538_FixUserUniqueIndexesForSoftDelete'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260515185538_FixUserUniqueIndexesForSoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260515185538_FixUserUniqueIndexesForSoftDelete', N'8.0.11');
END;
GO

COMMIT;
GO


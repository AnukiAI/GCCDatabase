-- Applies EF migration 20260326025056_AddPasswordResetTokenToUser to prod.
-- Adds two nullable columns to [Users] for the forgot-password token flow:
--   PasswordResetToken         nvarchar(max)
--   PasswordResetTokenExpiry   datetime2
--
-- Code paths: AccountController forgot-password / reset-password endpoints.
-- Not currently blocking the site (Forgot Password is gated on SMTP being
-- configured anyway, which it isn't). Apply to catch up the schema with the
-- migration history before any future deploy that exercises this flow.
--
-- Idempotent: each column add and the history-row insert are guarded.
-- Transactional with XACT_ABORT so any failure rolls everything back.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Users]')
      AND name = N'PasswordResetToken'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetToken] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Users]')
      AND name = N'PasswordResetTokenExpiry'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetTokenExpiry] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326025056_AddPasswordResetTokenToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260326025056_AddPasswordResetTokenToUser', '8.0.11');
END;

COMMIT TRANSACTION;

-- Verification
SELECT
  (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = N'PasswordResetToken')        AS TokenColExists,
  (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[Users]') AND name = N'PasswordResetTokenExpiry')  AS ExpiryColExists,
  (SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260326025056_AddPasswordResetTokenToUser') AS HistoryRowExists;

-- REQUIRED session settings (DEF-252 / recurrence of DEF-239):
--   QUOTED_IDENTIFIER ON  — the backfill UPDATE touches [Users], which carries a
--                           filtered unique index (WHERE [IsDeleted]=0). With this OFF
--                           (sqlcmd -Q/-i default) the UPDATE fails with Msg 1934 and,
--                           because the error is non-fatal, the rest of the script still
--                           commits — the column is created but left UN-BACKFILLED.
--   XACT_ABORT ON         — make ANY error abort the whole transaction atomically so the
--                           script either fully applies or leaves prod untouched. Without
--                           it a mid-script failure half-applies (exactly what DEF-252 hit).
-- If you run via sqlcmd, also pass -I (sets QUOTED_IDENTIFIER ON for the session).
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605131035_ChangeUserCompanyToFreeText'
)
BEGIN
    ALTER TABLE [Users] ADD [UserCompany] nvarchar(255) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605131035_ChangeUserCompanyToFreeText'
)
BEGIN

                    UPDATE u
                    SET u.UserCompany = c.Name
                    FROM [Users] u
                    INNER JOIN [Companies] c ON u.UserCompanyId = c.Id
                    WHERE u.UserCompanyId IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605131035_ChangeUserCompanyToFreeText'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Companies_UserCompanyId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605131035_ChangeUserCompanyToFreeText'
)
BEGIN
    DROP INDEX [IX_Users_UserCompanyId] ON [Users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605131035_ChangeUserCompanyToFreeText'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UserCompanyId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] DROP COLUMN [UserCompanyId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260605131035_ChangeUserCompanyToFreeText'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260605131035_ChangeUserCompanyToFreeText', N'8.0.11');
END;
GO

COMMIT;
GO


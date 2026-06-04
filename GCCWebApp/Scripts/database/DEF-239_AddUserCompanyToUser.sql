BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603135743_AddUserCompanyToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [UserCompanyId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603135743_AddUserCompanyToUser'
)
BEGIN
    CREATE INDEX [IX_Users_UserCompanyId] ON [Users] ([UserCompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603135743_AddUserCompanyToUser'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Companies_UserCompanyId] FOREIGN KEY ([UserCompanyId]) REFERENCES [Companies] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260603135743_AddUserCompanyToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260603135743_AddUserCompanyToUser', N'8.0.11');
END;
GO

COMMIT;
GO


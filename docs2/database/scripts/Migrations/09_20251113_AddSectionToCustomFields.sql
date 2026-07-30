BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113171055_AddSectionToCustomFields'
)
BEGIN
    ALTER TABLE [CustomFields] ADD [Section] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113171055_AddSectionToCustomFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113171055_AddSectionToCustomFields', N'8.0.11');
END;
GO

COMMIT;
GO


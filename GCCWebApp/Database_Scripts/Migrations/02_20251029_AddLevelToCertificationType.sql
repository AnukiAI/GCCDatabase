BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029003704_AddLevelToCertificationType'
)
BEGIN
    ALTER TABLE [CertificationTypes] ADD [Level] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029003704_AddLevelToCertificationType'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'UsageCostPerRequest');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ApiKeys] ALTER COLUMN [UsageCostPerRequest] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029003704_AddLevelToCertificationType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029003704_AddLevelToCertificationType', N'8.0.11');
END;
GO

COMMIT;
GO


BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113161816_AddGoogleAnalyticsSettings'
)
BEGIN
    CREATE TABLE [GoogleAnalyticsSettings] (
        [Id] int NOT NULL IDENTITY,
        [TrackingId] nvarchar(20) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [CustomScript] TEXT NULL,
        [AnonymizeIp] bit NOT NULL,
        [EnhancedLinkAttribution] bit NOT NULL,
        [DebugMode] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_GoogleAnalyticsSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoogleAnalyticsSettings_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113161816_AddGoogleAnalyticsSettings'
)
BEGIN
    CREATE INDEX [IX_GoogleAnalyticsSettings_UpdatedBy] ON [GoogleAnalyticsSettings] ([UpdatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113161816_AddGoogleAnalyticsSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113161816_AddGoogleAnalyticsSettings', N'8.0.11');
END;
GO

COMMIT;
GO


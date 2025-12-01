BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111164931_AddAmazonApiIntegration'
)
BEGIN
    CREATE TABLE [AmazonApiSettings] (
        [Id] int NOT NULL IDENTITY,
        [IsEnabled] bit NOT NULL,
        [SyncIntervalHours] int NOT NULL,
        [AccessKeyId] nvarchar(500) NOT NULL,
        [SecretAccessKey] nvarchar(500) NOT NULL,
        [RefreshToken] nvarchar(1000) NOT NULL,
        [Region] nvarchar(10) NOT NULL,
        [MarketplaceId] nvarchar(50) NOT NULL,
        [SyncProducts] bit NOT NULL,
        [SyncCertificates] bit NOT NULL,
        [SyncCompliance] bit NOT NULL,
        [WebhookUrl] nvarchar(500) NULL,
        [LastSync] datetime2 NULL,
        [LastSyncStatus] nvarchar(20) NOT NULL,
        [RecordsSynced] int NOT NULL,
        [LastError] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_AmazonApiSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111164931_AddAmazonApiIntegration'
)
BEGIN
    CREATE TABLE [AmazonSyncHistory] (
        [Id] int NOT NULL IDENTITY,
        [Timestamp] datetime2 NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [RecordsProcessed] int NOT NULL,
        [RecordsSucceeded] int NOT NULL,
        [RecordsFailed] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [DurationSeconds] float NOT NULL,
        [Message] nvarchar(1000) NULL,
        [ErrorDetails] nvarchar(4000) NULL,
        [TriggeredBy] nvarchar(100) NULL,
        CONSTRAINT [PK_AmazonSyncHistory] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111164931_AddAmazonApiIntegration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251111164931_AddAmazonApiIntegration', N'8.0.11');
END;
GO

COMMIT;
GO


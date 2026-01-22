BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112144629_AddPartnerApiKeys'
)
BEGIN
    CREATE TABLE [PartnerApiKeys] (
        [Id] int NOT NULL IDENTITY,
        [PartnerId] int NOT NULL,
        [ApiKey] nvarchar(512) NOT NULL,
        [Description] nvarchar(255) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(255) NULL,
        [ExpiresAt] datetime2 NULL,
        [LastUsedAt] datetime2 NULL,
        [RequestCount] int NOT NULL,
        [RateLimitPerHour] int NULL,
        [AllowedEndpoints] nvarchar(max) NULL,
        [Permissions] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedBy] nvarchar(255) NULL,
        CONSTRAINT [PK_PartnerApiKeys] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartnerApiKeys_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112144629_AddPartnerApiKeys'
)
BEGIN
    CREATE INDEX [IX_PartnerApiKeys_PartnerId] ON [PartnerApiKeys] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112144629_AddPartnerApiKeys'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112144629_AddPartnerApiKeys', N'8.0.11');
END;
GO

COMMIT;
GO


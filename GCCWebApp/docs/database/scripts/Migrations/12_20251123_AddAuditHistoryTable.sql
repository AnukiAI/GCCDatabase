BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE TABLE [AuditHistory] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(50) NOT NULL,
        [EntityName] nvarchar(255) NULL,
        [Action] nvarchar(20) NOT NULL,
        [FieldName] nvarchar(100) NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [ChangedBy] int NOT NULL,
        [ChangedByUsername] nvarchar(255) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [ChangeSource] nvarchar(50) NULL,
        [Reason] nvarchar(1000) NULL,
        [SessionId] nvarchar(100) NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditHistory_Users_ChangedBy] FOREIGN KEY ([ChangedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_Action] ON [AuditHistory] ([Action]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedAt] ON [AuditHistory] ([ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedAt_EntityType] ON [AuditHistory] ([ChangedAt], [EntityType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedBy] ON [AuditHistory] ([ChangedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedBy_ChangedAt] ON [AuditHistory] ([ChangedBy], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityId] ON [AuditHistory] ([EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityType] ON [AuditHistory] ([EntityType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityType_Action] ON [AuditHistory] ([EntityType], [Action]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityType_EntityId_ChangedAt] ON [AuditHistory] ([EntityType], [EntityId], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251123161550_AddAuditHistoryTable', N'8.0.11');
END;
GO

COMMIT;
GO


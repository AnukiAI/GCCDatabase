BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    ALTER TABLE [Certificates] ADD [PathwayId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    CREATE TABLE [Pathways] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Pathways] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    CREATE INDEX [IX_Certificates_PathwayId] ON [Certificates] ([PathwayId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    CREATE INDEX [IX_Pathways_IsActive] ON [Pathways] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    CREATE INDEX [IX_Pathways_IsActive_SortOrder] ON [Pathways] ([IsActive], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Pathways_Name] ON [Pathways] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    CREATE INDEX [IX_Pathways_SortOrder] ON [Pathways] ([SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_Pathways_PathwayId] FOREIGN KEY ([PathwayId]) REFERENCES [Pathways] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Name', N'Description', N'IsActive', N'IsDeleted', N'SortOrder', N'CreatedAt', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Pathways]'))
        SET IDENTITY_INSERT [Pathways] ON;
    EXEC(N'INSERT INTO [Pathways] ([Name], [Description], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
    VALUES (N''Energy usage reduction'', NULL, CAST(1 AS bit), CAST(0 AS bit), 1, ''2026-02-13T00:55:48.9744072Z'', ''2026-02-13T00:55:48.9744079Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Name', N'Description', N'IsActive', N'IsDeleted', N'SortOrder', N'CreatedAt', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Pathways]'))
        SET IDENTITY_INSERT [Pathways] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Name', N'Description', N'IsActive', N'IsDeleted', N'SortOrder', N'CreatedAt', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Pathways]'))
        SET IDENTITY_INSERT [Pathways] ON;
    EXEC(N'INSERT INTO [Pathways] ([Name], [Description], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
    VALUES (N''Onsite fossil fuel usage reduction'', NULL, CAST(1 AS bit), CAST(0 AS bit), 2, ''2026-02-13T00:55:48.9765666Z'', ''2026-02-13T00:55:48.9765675Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Name', N'Description', N'IsActive', N'IsDeleted', N'SortOrder', N'CreatedAt', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Pathways]'))
        SET IDENTITY_INSERT [Pathways] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Name', N'Description', N'IsActive', N'IsDeleted', N'SortOrder', N'CreatedAt', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Pathways]'))
        SET IDENTITY_INSERT [Pathways] ON;
    EXEC(N'INSERT INTO [Pathways] ([Name], [Description], [IsActive], [IsDeleted], [SortOrder], [CreatedAt], [UpdatedAt])
    VALUES (N''Renewable electricity usage'', NULL, CAST(1 AS bit), CAST(0 AS bit), 3, ''2026-02-13T00:55:48.9765698Z'', ''2026-02-13T00:55:48.9765698Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Name', N'Description', N'IsActive', N'IsDeleted', N'SortOrder', N'CreatedAt', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Pathways]'))
        SET IDENTITY_INSERT [Pathways] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213000922_AddPathwayTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213000922_AddPathwayTable', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213005208_FixPathwayUniqueIndexForSoftDelete'
)
BEGIN
    DROP INDEX [IX_Pathways_Name] ON [Pathways];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213005208_FixPathwayUniqueIndexForSoftDelete'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pathways_Name] ON [Pathways] ([Name]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213005208_FixPathwayUniqueIndexForSoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213005208_FixPathwayUniqueIndexForSoftDelete', N'8.0.11');
END;
GO

COMMIT;
GO


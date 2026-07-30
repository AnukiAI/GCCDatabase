BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE TABLE [CertificateStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [CertificateId] int NOT NULL,
        [CertificateNumber] nvarchar(7) NULL,
        [PreviousStatus] nvarchar(50) NOT NULL,
        [NewStatus] nvarchar(50) NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [Notes] nvarchar(max) NULL,
        [ChangedBy] int NOT NULL,
        [ChangedByUsername] nvarchar(255) NULL,
        [ChangedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [ChangeSource] nvarchar(50) NULL,
        [DurationInPreviousStatus] int NULL,
        [IsAutomatedTransition] bit NOT NULL,
        [WorkflowStep] nvarchar(100) NULL,
        CONSTRAINT [PK_CertificateStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateStatusHistory_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateStatusHistory_Users_ChangedBy] FOREIGN KEY ([ChangedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_CertificateId] ON [CertificateStatusHistory] ([CertificateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_CertificateId_ChangedAt] ON [CertificateStatusHistory] ([CertificateId], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_CertificateId_NewStatus] ON [CertificateStatusHistory] ([CertificateId], [NewStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_ChangedAt] ON [CertificateStatusHistory] ([ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_ChangedAt_NewStatus] ON [CertificateStatusHistory] ([ChangedAt], [NewStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_ChangedBy] ON [CertificateStatusHistory] ([ChangedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_NewStatus] ON [CertificateStatusHistory] ([NewStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_PreviousStatus] ON [CertificateStatusHistory] ([PreviousStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251117211135_AddCertificateStatusHistory', N'8.0.11');
END;
GO

COMMIT;
GO


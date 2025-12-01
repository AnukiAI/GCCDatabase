BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    ALTER TABLE [CertificationTypes] ADD [ParentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    CREATE INDEX [IX_CertificationTypes_ParentId] ON [CertificationTypes] ([ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    ALTER TABLE [CertificationTypes] ADD CONSTRAINT [FK_CertificationTypes_CertificationTypes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [CertificationTypes] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN

                    UPDATE CertificationTypes
                    SET ParentId = (SELECT Id FROM CertificationTypes WHERE Name = 'Waste Diversion from Landfill')
                    WHERE Name = 'Waste System Audit';
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029011232_AddParentIdToCertificationType', N'8.0.11');
END;
GO

COMMIT;
GO


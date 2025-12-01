BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126212053_AddCertificationResultsToCertificate'
)
BEGIN
    ALTER TABLE [Certificates] ADD [CertificationResults] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126212053_AddCertificationResultsToCertificate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251126212053_AddCertificationResultsToCertificate', N'8.0.11');
END;
GO

COMMIT;
GO


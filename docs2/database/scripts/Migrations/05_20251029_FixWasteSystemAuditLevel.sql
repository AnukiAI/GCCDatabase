BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029163311_FixWasteSystemAuditLevel'
)
BEGIN
    UPDATE CertificationTypes SET Level = 1 WHERE Name = 'Waste System Audit' AND Level = 2;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029163311_FixWasteSystemAuditLevel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029163311_FixWasteSystemAuditLevel', N'8.0.11');
END;
GO

COMMIT;
GO


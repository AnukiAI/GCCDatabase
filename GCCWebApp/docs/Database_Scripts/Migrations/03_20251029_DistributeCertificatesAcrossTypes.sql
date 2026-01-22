BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029005907_DistributeCertificatesAcrossTypes'
)
BEGIN

                    -- Update certificates to distribute across all certification types
                    WITH NumberedCertificates AS (
                        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) as RowNum
                        FROM Certificates
                    )
                    UPDATE c
                    SET CertificationTypeId = 1 + ((nc.RowNum - 1) % 22)
                    FROM Certificates c
                    INNER JOIN NumberedCertificates nc ON c.Id = nc.Id;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029005907_DistributeCertificatesAcrossTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029005907_DistributeCertificatesAcrossTypes', N'8.0.11');
END;
GO

COMMIT;
GO


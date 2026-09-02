BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901164220_CR037_MultiCountryCertificates'
)
BEGIN
    CREATE TABLE [CertificateCountries] (
        [Id] int NOT NULL IDENTITY,
        [CertificateId] int NOT NULL,
        [Country] nvarchar(100) NOT NULL,
        [NormalizedCountry] AS LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Country], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N'')) PERSISTED,
        CONSTRAINT [PK_CertificateCountries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateCountries_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901164220_CR037_MultiCountryCertificates'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CertificateCountries_CertificateId_Country] ON [CertificateCountries] ([CertificateId], [Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901164220_CR037_MultiCountryCertificates'
)
BEGIN
    CREATE INDEX [IX_CertificateCountries_Country] ON [CertificateCountries] ([Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901164220_CR037_MultiCountryCertificates'
)
BEGIN
    CREATE INDEX [IX_CertificateCountries_NormalizedCountry] ON [CertificateCountries] ([NormalizedCountry]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901164220_CR037_MultiCountryCertificates'
)
BEGIN

                    INSERT INTO [CertificateCountries] ([CertificateId], [Country])
                    SELECT c.[Id], LTRIM(RTRIM(c.[Country]))
                    FROM [Certificates] c
                    WHERE c.[Country] IS NOT NULL
                      AND NULLIF(LTRIM(RTRIM(c.[Country])), '') IS NOT NULL
                      AND NOT EXISTS (
                            SELECT 1 FROM [CertificateCountries] cc
                            WHERE cc.[CertificateId] = c.[Id]
                              AND cc.[Country] = LTRIM(RTRIM(c.[Country]))
                      );
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901164220_CR037_MultiCountryCertificates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901164220_CR037_MultiCountryCertificates', N'8.0.11');
END;
GO

COMMIT;
GO


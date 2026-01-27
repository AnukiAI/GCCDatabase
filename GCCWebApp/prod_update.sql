BEGIN TRANSACTION;
GO

ALTER TABLE [Companies] ADD [PartnerId] int NULL;
GO

DROP INDEX [IX_Certificates_Number] ON [Certificates];
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Certificates]') AND [c].[name] = N'Number');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Certificates] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Certificates] ALTER COLUMN [Number] nvarchar(50) NOT NULL;
CREATE UNIQUE INDEX [IX_Certificates_Number] ON [Certificates] ([Number]);
GO

CREATE TABLE [CertificatePartners] (
    [CertificatesId] int NOT NULL,
    [PartnersId] int NOT NULL,
    CONSTRAINT [PK_CertificatePartners] PRIMARY KEY ([CertificatesId], [PartnersId]),
    CONSTRAINT [FK_CertificatePartners_Certificates_CertificatesId] FOREIGN KEY ([CertificatesId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CertificatePartners_Partners_PartnersId] FOREIGN KEY ([PartnersId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Companies_PartnerId] ON [Companies] ([PartnerId]);
GO

CREATE INDEX [IX_CertificatePartners_PartnersId] ON [CertificatePartners] ([PartnersId]);
GO

ALTER TABLE [Companies] ADD CONSTRAINT [FK_Companies_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260127002958_AddPartnerIdToCompanies', N'8.0.11');
GO

COMMIT;
GO


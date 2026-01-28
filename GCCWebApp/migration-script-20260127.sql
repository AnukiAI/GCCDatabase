BEGIN TRANSACTION;
GO

CREATE TABLE [CertificateCertificationTypeSubcategories] (
    [CertificateId] int NOT NULL,
    [CertificationTypeSubcategoriesId] int NOT NULL,
    CONSTRAINT [PK_CertificateCertificationTypeSubcategories] PRIMARY KEY ([CertificateId], [CertificationTypeSubcategoriesId]),
    CONSTRAINT [FK_CertificateCertificationTypeSubcategories_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CertificateCertificationTypeSubcategories_CertificationTypes_CertificationTypeSubcategoriesId] FOREIGN KEY ([CertificationTypeSubcategoriesId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_CertificateCertificationTypeSubcategories_CertificationTypeSubcategoriesId] ON [CertificateCertificationTypeSubcategories] ([CertificationTypeSubcategoriesId]);
GO


                INSERT INTO CertificateCertificationTypeSubcategories (CertificateId, CertificationTypeSubcategoriesId)
                SELECT Id, CertificationTypeSubcategoryId
                FROM Certificates
                WHERE CertificationTypeSubcategoryId IS NOT NULL
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260127183149_AddMultipleSubcategoriesToCertificate', N'8.0.11');
GO

COMMIT;
GO


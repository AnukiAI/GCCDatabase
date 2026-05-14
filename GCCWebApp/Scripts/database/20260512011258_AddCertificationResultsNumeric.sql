-- DEF-187/prod-deploy follow-up 2026-05-13
-- Applies EF migration 20260512011258_AddCertificationResultsNumeric to prod.
-- Code shipped in GCCWebApp-deploy-20260513-0027.zip references this column;
-- absence caused /api/certificate-tiles to throw "Invalid column name 'CertificationResultsNumeric'".

-- QUOTED_IDENTIFIER and ANSI_NULLS must be ON for indexed computed columns.
-- sqlcmd defaults QUOTED_IDENTIFIER to OFF, which caused error 1934 on first run.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Certificates]')
      AND name = N'CertificationResultsNumeric'
)
BEGIN
    ALTER TABLE [Certificates] ADD [CertificationResultsNumeric] AS (
        CASE
          WHEN [CertificationResults] IS NULL OR [CertificationResults] = N'' THEN NULL
          WHEN [CertificationResults] LIKE N'%[%]%'
            THEN TRY_CAST(REPLACE([CertificationResults], N'%', N'') AS DECIMAL(18,4))
          WHEN TRY_CAST([CertificationResults] AS DECIMAL(18,4)) < 1
            THEN TRY_CAST([CertificationResults] AS DECIMAL(18,4)) * 100
          ELSE TRY_CAST([CertificationResults] AS DECIMAL(18,4))
        END
    ) PERSISTED;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[Certificates]')
      AND name = N'IX_Certificates_CertificationResultsNumeric'
)
BEGIN
    CREATE INDEX [IX_Certificates_CertificationResultsNumeric]
      ON [Certificates] ([CertificationResultsNumeric]);
END;

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512011258_AddCertificationResultsNumeric'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260512011258_AddCertificationResultsNumeric', '8.0.11');
END;

COMMIT TRANSACTION;

-- Verification
SELECT
  (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[Certificates]') AND name = N'CertificationResultsNumeric') AS ColExists,
  (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Certificates]') AND name = N'IX_Certificates_CertificationResultsNumeric') AS IdxExists,
  (SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260512011258_AddCertificationResultsNumeric') AS HistoryRowExists;

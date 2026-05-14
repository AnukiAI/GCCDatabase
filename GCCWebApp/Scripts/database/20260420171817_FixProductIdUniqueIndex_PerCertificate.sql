-- Applies EF migration 20260420171817_FixProductIdUniqueIndex_PerCertificate to prod.
-- Changes the uniqueness scope on [CertificateProductIds]:
--   BEFORE: (ProductIdType, ProductIdValue)              unique globally
--   AFTER:  (CertificateId, ProductIdType, ProductIdValue) unique per certificate
--
-- Business rationale: the same product identifier (e.g., an ASIN) can legitimately
-- appear on multiple certificates (one product, multiple cert types). The old
-- global-unique index blocked that. New index keeps duplicates out *within* a
-- certificate but allows the same Type+Value pair across different certs.
--
-- Safety:
--   * The CREATE UNIQUE INDEX with (CertificateId, Type, Value) is strictly LESS
--     restrictive than the old (Type, Value) global unique. If the old index
--     succeeded (it did — it's currently on prod), the new index cannot fail on
--     duplicate violations.
--   * Idempotent: each step is guarded by an existence check.
--   * Transactional with XACT_ABORT.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[CertificateProductIds]')
      AND name = N'IX_CertificateProductIds_ProductIdType_ProductIdValue'
)
BEGIN
    DROP INDEX [IX_CertificateProductIds_ProductIdType_ProductIdValue]
        ON [CertificateProductIds];
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[CertificateProductIds]')
      AND name = N'IX_CertificateProductIds_CertificateId_ProductIdType_ProductIdValue'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CertificateProductIds_CertificateId_ProductIdType_ProductIdValue]
        ON [CertificateProductIds] ([CertificateId], [ProductIdType], [ProductIdValue]);
END;

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260420171817_FixProductIdUniqueIndex_PerCertificate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260420171817_FixProductIdUniqueIndex_PerCertificate', '8.0.11');
END;

COMMIT TRANSACTION;

-- Verification
SELECT
  (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'[CertificateProductIds]')
     AND name = N'IX_CertificateProductIds_ProductIdType_ProductIdValue')                                 AS OldIdxStillThere_ShouldBe0,
  (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'[CertificateProductIds]')
     AND name = N'IX_CertificateProductIds_CertificateId_ProductIdType_ProductIdValue')                   AS NewIdxExists_ShouldBe1,
  (SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260420171817_FixProductIdUniqueIndex_PerCertificate') AS HistoryRowExists_ShouldBe1;

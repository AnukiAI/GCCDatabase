-- CR-171 Phase 2 (product-ID-level multi-partner, Model A) — prod SQL for the deploy (Gate 7).
-- Prod does NOT auto-migrate. Run BOTH steps in the deploy:
--   (1) create the CertificateProductIdPartners join table (mirrors migration 20260617200729),
--   (2) backfill it from the denormalized CertificateProductId.Partner string.
-- Idempotent — safe to re-run.

-- (1) Join table (EF skip-nav shadow keys: CertificateProductIdId, PartnersId).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateProductIdPartners')
BEGIN
    CREATE TABLE CertificateProductIdPartners (
        CertificateProductIdId int NOT NULL,
        PartnersId            int NOT NULL,
        CONSTRAINT PK_CertificateProductIdPartners PRIMARY KEY (CertificateProductIdId, PartnersId),
        CONSTRAINT FK_CertificateProductIdPartners_CertificateProductIds_CertificateProductIdId
            FOREIGN KEY (CertificateProductIdId) REFERENCES CertificateProductIds (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CertificateProductIdPartners_Partners_PartnersId
            FOREIGN KEY (PartnersId) REFERENCES Partners (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_CertificateProductIdPartners_PartnersId
        ON CertificateProductIdPartners (PartnersId);

    -- Register the migration so EF does not try to re-create it (only if applying SQL, not EF update).
    IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260617200729_AddProductIdPartnersM2M')
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260617200729_AddProductIdPartnersM2M', '9.0.0');
END;

-- (2) Backfill from the denormalized single Partner string (Model A: a tagged product ID is
-- visible only to that partner; untagged stays shared with all the cert's partners).
INSERT INTO CertificateProductIdPartners (CertificateProductIdId, PartnersId)
SELECT pid.Id, p.Id
FROM CertificateProductIds pid
INNER JOIN Partners p
        ON p.Name = pid.Partner
       AND p.IsDeleted = 0
WHERE pid.Partner IS NOT NULL
  AND pid.Partner <> ''
  AND pid.Partner <> 'None'
  AND NOT EXISTS (
        SELECT 1 FROM CertificateProductIdPartners x
        WHERE x.CertificateProductIdId = pid.Id AND x.PartnersId = p.Id
  );

-- ============================================================================
-- CR-171 — Multi-partner backfill (DATA, runs AFTER schema migrations)
-- ----------------------------------------------------------------------------
-- The EF migration script (apply-migrations.sql) creates the join tables
-- (CertificatePartners already existed; CertificateProductIdPartners added by
-- migration 20260617200729_AddProductIdPartnersM2M). This script POPULATES them
-- from the denormalized single Partner string. REQUIRED in the same deploy step:
-- once access filtering reads the M2M collections, any row not present here goes
-- invisible to its partner logins/API keys.
--
-- Idempotent (NOT EXISTS guards) — safe to re-run. The deploy bundler appends
-- every Scripts/database/post-deploy/*.sql to migrations/apply-migrations.sql,
-- so this runs automatically right after the schema is brought up to date.
-- ============================================================================

-- 1) Certificate-level partners
INSERT INTO CertificatePartners (CertificatesId, PartnersId)
SELECT c.Id, p.Id
FROM Certificates c
INNER JOIN Partners p ON p.Name = c.Partner AND p.IsDeleted = 0
WHERE c.Partner IS NOT NULL AND c.Partner <> '' AND c.Partner <> 'None'
  AND NOT EXISTS (
        SELECT 1 FROM CertificatePartners cp
        WHERE cp.CertificatesId = c.Id AND cp.PartnersId = p.Id);

-- 2) Product-ID-level partners (Model A: a tagged product ID is restricted to that
--    partner; untagged stays shared with all the cert's partners)
INSERT INTO CertificateProductIdPartners (CertificateProductIdId, PartnersId)
SELECT pid.Id, p.Id
FROM CertificateProductIds pid
INNER JOIN Partners p ON p.Name = pid.Partner AND p.IsDeleted = 0
WHERE pid.Partner IS NOT NULL AND pid.Partner <> '' AND pid.Partner <> 'None'
  AND NOT EXISTS (
        SELECT 1 FROM CertificateProductIdPartners x
        WHERE x.CertificateProductIdId = pid.Id AND x.PartnersId = p.Id);
GO

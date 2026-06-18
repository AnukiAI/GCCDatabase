-- CR-171 Phase 1 backfill — populate the CertificatePartners (M2M) join table from the
-- denormalized Certificate.Partner string. REQUIRED with the deploy (Gate 7): once access
-- filtering reads the collection instead of the single string, any cert not present in the
-- join table becomes invisible to its partner logins/API keys. Prod does NOT auto-migrate,
-- so run this in the same deploy step. Idempotent — safe to re-run (NOT EXISTS guard).
--
-- Join-table columns are EF skip-navigation shadow keys: CertificatesId, PartnersId.

INSERT INTO CertificatePartners (CertificatesId, PartnersId)
SELECT c.Id, p.Id
FROM Certificates c
INNER JOIN Partners p
        ON p.Name = c.Partner
       AND p.IsDeleted = 0
WHERE c.Partner IS NOT NULL
  AND c.Partner <> ''
  AND c.Partner <> 'None'
  AND NOT EXISTS (
        SELECT 1
        FROM CertificatePartners cp
        WHERE cp.CertificatesId = c.Id
          AND cp.PartnersId = p.Id
  );

-- Verification (run after): rows that have a real denorm Partner but NO join row.
-- Expect 0. A non-zero count means the denorm Partner value has no matching Partners.Name.
-- SELECT c.Id, c.Number, c.Partner
-- FROM Certificates c
-- WHERE c.Partner IS NOT NULL AND c.Partner <> '' AND c.Partner <> 'None'
--   AND NOT EXISTS (SELECT 1 FROM CertificatePartners cp
--                   INNER JOIN Partners p ON p.Id = cp.PartnersId
--                   WHERE cp.CertificatesId = c.Id AND p.Name = c.Partner);

-- DEF-167 — Audit and remediate certificates whose CertificationTypeId points at a Level-2 (subcategory) row.
--
-- Context: client QC #169 reported that the public cert-tile filter shows CSF subcategories as their own
-- filter checkboxes. Code fix (DEF-167) drops subcategories from the dropdown so only Level-1 parents show.
-- That makes any cert currently tagged at Level-2 unreachable via the filter.
--
-- This script does two things:
--   1. AUDIT (always safe to run): lists every cert with CertificationTypeId at Level=2, plus the parent
--      Level-1 row the cert should be re-pointed at.
--   2. REMEDIATE (commented out by default — CR-gated): repoints those certs to their Level-2's Level-1
--      parent, and inserts the original Level-2 reference into CertificateCertificationTypeSubcategories so
--      the subcategory information is preserved on the cert detail view.
--
-- BUSINESS-RECORD UPDATE: per project rule, this changes business records and MUST go through a CR before
-- the UPDATE block is uncommented and run on prod. The CR should cite the audit row count and the list of
-- affected cert Numbers from the audit query.
--
-- Dev DB state at scoping time (2026-05-11): 41 certs, all tagged at Level 1, 0 affected. No dev run needed.

-- =====================================================================
-- 1) AUDIT — run this first. No writes.
-- =====================================================================

SELECT
    c.Id                       AS CertId,
    c.Number                   AS CertNumber,
    c.CertificationTypeId      AS CurrentTypeId,
    ct.Name                    AS CurrentTypeName,
    ct.[Level]                 AS CurrentTypeLevel,
    ct.ParentId                AS ProposedNewTypeId,
    p.Name                     AS ProposedNewTypeName,
    p.[Level]                  AS ProposedNewTypeLevel
FROM Certificates c
JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
LEFT JOIN CertificationTypes p ON ct.ParentId = p.Id
WHERE ct.[Level] = 2
ORDER BY p.Name, ct.Name, c.Number;

-- Row count for CR write-up:
SELECT COUNT(*) AS AffectedCertCount
FROM Certificates c
JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.[Level] = 2;

-- =====================================================================
-- 2) REMEDIATE — DO NOT RUN WITHOUT CR APPROVAL.
--    Repoints CertificationTypeId to the parent Level-1, then preserves the
--    original Level-2 reference in CertificateCertificationTypeSubcategories.
-- =====================================================================

/*
BEGIN TRANSACTION;

-- 2a) Preserve the original Level-2 reference as a subcategory association.
--     Skip rows that already have the (CertificateId, CertificationTypeId) pair to avoid PK violation.
INSERT INTO CertificateCertificationTypeSubcategories (CertificateId, CertificationTypeId)
SELECT c.Id, c.CertificationTypeId
FROM Certificates c
JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.[Level] = 2
  AND NOT EXISTS (
      SELECT 1
      FROM CertificateCertificationTypeSubcategories x
      WHERE x.CertificateId = c.Id
        AND x.CertificationTypeId = c.CertificationTypeId
  );

-- 2b) Repoint the primary CertificationTypeId at the parent Level-1.
UPDATE c
SET    c.CertificationTypeId = ct.ParentId
FROM   Certificates c
JOIN   CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE  ct.[Level] = 2
  AND  ct.ParentId IS NOT NULL;

-- 2c) Post-condition check: zero Level-2 primary tags remaining.
SELECT 'Post-remediation L2 tag count (should be 0)' AS Check_,
       COUNT(*) AS Cnt
FROM Certificates c
JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.[Level] = 2;

-- Confirm row counts, then either COMMIT or ROLLBACK.
-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
*/

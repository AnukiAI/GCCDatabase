-- =============================================================================
-- CR-021 — Certified Environmental Facts -> Amazon long-form code
-- Date: 2026-06-11
-- Source: Migration analysis of client sample ASIN list (6.1.26). 7,904 of 14,183
--         ASIN rows carry amazonCode = 'greencircle_cef'. The DB still holds the
--         seeded short code 'CEF' on CertificationType Id 5 (only SEP/RC/CES were
--         converted to long-form on 2026-04-22; CEF was left "awaiting client mapping").
--
-- Effect: certs of type 'Certified Environmental Facts' passed to Amazon currently
--         send 'CEF', which Amazon does not recognize. This sets the long-form code
--         so the badge is applied. No schema change, no migration, no API shape change.
--
-- PRECONDITION: run the read-only PRE-CHECK first and confirm Id 5 / Name /
--               current AmazonCode before running the UPDATE. Requires GCC approval
--               (live reference-data change) per the data-change policy.
-- =============================================================================

-- ---- PRE-CHECK (read-only — safe to run anytime) ----------------------------
SELECT Id, Name, AmazonCode
FROM   CertificationTypes
WHERE  Id = 5;
-- Expect: Id=5, Name='Certified Environmental Facts', AmazonCode='CEF' (or NULL).
-- If Name differs, STOP and reconcile before proceeding.

-- ---- UPDATE (guarded, idempotent) -------------------------------------------
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE CertificationTypes
SET    AmazonCode = 'greencircle_cef'
WHERE  Id = 5
  AND  Name = 'Certified Environmental Facts'
  AND  ISNULL(AmazonCode, '') <> 'greencircle_cef';   -- no-op if already correct

-- ---- VERIFICATION -----------------------------------------------------------
SELECT Id, Name, AmazonCode
FROM   CertificationTypes
WHERE  Id = 5;
-- Expect: AmazonCode = 'greencircle_cef'.

COMMIT;
PRINT 'CR-021 applied: Certified Environmental Facts AmazonCode = greencircle_cef.';

-- ---- ROLLBACK (manual, if needed) -------------------------------------------
-- UPDATE CertificationTypes SET AmazonCode = 'CEF'
-- WHERE Id = 5 AND Name = 'Certified Environmental Facts';

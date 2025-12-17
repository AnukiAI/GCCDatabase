-- =============================================
-- Data Migration Script: Certification Type Subcategory
-- =============================================
-- Purpose: Migrate existing certificates that point to child certification types
--          to use the new CertificationTypeSubcategoryId field
--
-- Before running:
-- 1. Backup your database
-- 2. Run the EF migration: dotnet ef database update
-- 3. Then run this script
--
-- Logic:
-- - Certificates pointing to a PARENT type (ParentId IS NULL): No change needed
-- - Certificates pointing to a CHILD type (ParentId IS NOT NULL):
--   Move CertificationTypeId to CertificationTypeSubcategoryId,
--   Set CertificationTypeId to the parent type
-- =============================================

-- Set the database context (adjust for your environment)
-- USE GCCDatabase_Dev;  -- Development
-- USE gccwebap_certificates;  -- Production

-- =============================================
-- STEP 1: Preview the changes (SELECT only - safe to run)
-- =============================================
PRINT '=== PREVIEW: Certificates that will be migrated ==='

SELECT
    c.Id AS CertificateId,
    c.Number AS CertificateNumber,
    c.CertificationTypeId AS CurrentTypeId,
    ct.Name AS CurrentTypeName,
    ct.ParentId AS CurrentTypeParentId,
    parent.Name AS ParentTypeName,
    'Will move to SubcategoryId' AS Action
FROM Certificates c
INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
LEFT JOIN CertificationTypes parent ON ct.ParentId = parent.Id
WHERE ct.ParentId IS NOT NULL
ORDER BY c.Number;

-- Count affected records
SELECT COUNT(*) AS CertificatesToMigrate
FROM Certificates c
INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.ParentId IS NOT NULL;

-- =============================================
-- STEP 2: Perform the migration (UPDATE)
-- =============================================
PRINT '=== MIGRATING: Moving child types to SubcategoryId ==='

BEGIN TRANSACTION;

-- Update certificates that point to child types
UPDATE c
SET
    c.CertificationTypeSubcategoryId = c.CertificationTypeId,  -- Move current to subcategory
    c.CertificationTypeId = ct.ParentId                        -- Set to parent type
FROM Certificates c
INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE ct.ParentId IS NOT NULL;

-- Check how many rows were affected
PRINT CONCAT('Rows updated: ', @@ROWCOUNT);

-- =============================================
-- STEP 3: Verify the migration
-- =============================================
PRINT '=== VERIFICATION: Check migrated certificates ==='

SELECT
    c.Id AS CertificateId,
    c.Number AS CertificateNumber,
    c.CertificationTypeId AS NewTypeId,
    parentType.Name AS TypeName,
    c.CertificationTypeSubcategoryId AS NewSubcategoryId,
    subType.Name AS SubcategoryName
FROM Certificates c
INNER JOIN CertificationTypes parentType ON c.CertificationTypeId = parentType.Id
LEFT JOIN CertificationTypes subType ON c.CertificationTypeSubcategoryId = subType.Id
WHERE c.CertificationTypeSubcategoryId IS NOT NULL
ORDER BY c.Number;

-- Verify data integrity: SubcategoryId's parent should match CertificationTypeId
SELECT
    c.Id AS CertificateId,
    c.Number AS CertificateNumber,
    c.CertificationTypeId,
    c.CertificationTypeSubcategoryId,
    subType.ParentId AS SubcategoryParentId,
    CASE
        WHEN c.CertificationTypeId = subType.ParentId THEN 'OK'
        ELSE 'ERROR - Parent mismatch!'
    END AS IntegrityCheck
FROM Certificates c
INNER JOIN CertificationTypes subType ON c.CertificationTypeSubcategoryId = subType.Id
ORDER BY c.Number;

-- =============================================
-- STEP 4: Commit or Rollback
-- =============================================
-- Uncomment ONE of the following:

-- If everything looks correct:
COMMIT TRANSACTION;
PRINT '=== MIGRATION COMPLETE ==='

-- If something went wrong:
-- ROLLBACK TRANSACTION;
-- PRINT '=== MIGRATION ROLLED BACK ==='

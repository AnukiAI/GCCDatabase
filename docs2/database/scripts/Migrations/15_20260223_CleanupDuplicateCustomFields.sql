-- ============================================================================
-- Migration 15: Cleanup Duplicate Custom Fields
-- Date: 2026-02-23
-- Purpose: Remove custom fields that duplicate data already captured by
--          hard-coded fields on the Certificate, Company, and Product models.
--          Update remaining fields with proper section names.
-- ============================================================================
--
-- CONTEXT:
-- The CustomFields table was seeded with 30 fields, but ~12 of them duplicate
-- data already on the certificate form:
--   - Company info (name, email, phone, address) -> Company entity via FK
--   - Product info (brand, model, SKU, dimensions, weight, color) -> Product entity via FK
--   - Country -> Certificate.Country field
--   - Certificate Visibility -> Certificate.Status + RoleVisibility system
--
-- The remaining 18 fields are genuinely unique (environmental metrics, testing
-- data, compliance standards, record management) and should be kept.
--
-- SAFE TO RUN: This script checks for existing values before deleting.
--              Fields with data will be flagged but NOT deleted.
-- ============================================================================

PRINT '=== Migration 15: Cleanup Duplicate Custom Fields ===';
PRINT '';

-- Step 1: Identify fields to remove (duplicates of hard-coded form fields)
-- IDs based on seed data in SYNC_PROD_DATA.sql
DECLARE @FieldsToRemove TABLE (Id INT, Name NVARCHAR(100), Reason NVARCHAR(200));

INSERT INTO @FieldsToRemove VALUES
(1,  'Certificate Holder Company Name',  'Duplicate: Certificate -> Company.Name via FK'),
(2,  'Certificate Holder Contact Email',  'Duplicate: Certificate -> Company.Email via FK'),
(3,  'Certificate Holder Phone Number',   'Duplicate: Certificate -> Company.Phone via FK'),
(4,  'Certificate Holder Address',         'Duplicate: Certificate -> Company.Address via FK'),
(5,  'Product Brand Name',                'Duplicate: Product entity has its own attributes'),
(6,  'Product Model Number',              'Duplicate: Product entity has its own attributes'),
(7,  'Product SKU',                       'Duplicate: Product entity has its own attributes'),
(8,  'Product Dimensions',                'Duplicate: Product entity has its own attributes'),
(9,  'Product Weight',                    'Duplicate: Product entity has its own attributes'),
(10, 'Product Color Options',             'Duplicate: Product entity has its own attributes'),
(13, 'Manufacturing Country',             'Duplicate: Certificate.Country field'),
(28, 'Certificate Visibility',            'Duplicate: Certificate.Status + RoleVisibility system');

PRINT 'Fields marked for removal:';
SELECT Id, Name, Reason FROM @FieldsToRemove ORDER BY Id;

-- Step 2: Check for existing values before deleting
DECLARE @FieldsWithData TABLE (FieldId INT, Name NVARCHAR(100), ValueCount INT);

INSERT INTO @FieldsWithData
SELECT ftr.Id, ftr.Name, COUNT(cfv.Id)
FROM @FieldsToRemove ftr
LEFT JOIN CustomFieldValues cfv ON cfv.FieldId = ftr.Id
GROUP BY ftr.Id, ftr.Name
HAVING COUNT(cfv.Id) > 0;

IF EXISTS (SELECT 1 FROM @FieldsWithData)
BEGIN
    PRINT '';
    PRINT '*** WARNING: The following fields have existing data and will NOT be deleted ***';
    SELECT FieldId, Name, ValueCount AS 'Values Found' FROM @FieldsWithData;
    PRINT '*** Review these values before running with FORCE mode ***';
    PRINT '';
END

-- Step 3: Delete field values for fields being removed (only those without data)
DELETE cfv
FROM CustomFieldValues cfv
INNER JOIN @FieldsToRemove ftr ON cfv.FieldId = ftr.Id
WHERE ftr.Id NOT IN (SELECT FieldId FROM @FieldsWithData);

PRINT 'Deleted orphan CustomFieldValues for duplicate fields.';

-- Step 4: Delete the duplicate custom fields (only those without data)
DELETE cf
FROM CustomFields cf
INNER JOIN @FieldsToRemove ftr ON cf.Id = ftr.Id
WHERE ftr.Id NOT IN (SELECT FieldId FROM @FieldsWithData);

DECLARE @DeletedCount INT = @@ROWCOUNT;
PRINT CONCAT('Deleted ', @DeletedCount, ' duplicate custom fields.');

-- Step 5: Update section names on remaining fields to match new categories
-- Environmental Metrics (IDs 15-20)
UPDATE CustomFields SET Section = 'Environmental Metrics'
WHERE Id IN (15, 17, 18, 19, 20)
  AND EXISTS (SELECT 1 FROM CustomFields WHERE Id = 15);

-- Testing & Compliance (IDs 16, 21-24)
UPDATE CustomFields SET Section = 'Testing & Compliance'
WHERE Id IN (16, 21, 22, 23, 24)
  AND EXISTS (SELECT 1 FROM CustomFields WHERE Id = 16);

-- Record Management (IDs 25-27, 29-30)
UPDATE CustomFields SET Section = 'Record Management'
WHERE Id IN (25, 26, 27, 29, 30)
  AND EXISTS (SELECT 1 FROM CustomFields WHERE Id = 25);

-- Manufacturing fields stay as Additional Information (IDs 11, 12, 14)
UPDATE CustomFields SET Section = 'Additional Information'
WHERE Id IN (11, 12, 14)
  AND EXISTS (SELECT 1 FROM CustomFields WHERE Id = 11);

PRINT 'Updated section names on remaining fields.';

-- Step 6: Re-sequence sort orders for remaining fields
;WITH Reordered AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY
        CASE Section
            WHEN 'Additional Information' THEN 1
            WHEN 'Environmental Metrics' THEN 2
            WHEN 'Testing & Compliance' THEN 3
            WHEN 'Record Management' THEN 4
            ELSE 5
        END, SortOrder
    ) AS NewOrder
    FROM CustomFields
)
UPDATE cf
SET cf.SortOrder = r.NewOrder
FROM CustomFields cf
INNER JOIN Reordered r ON cf.Id = r.Id;

PRINT 'Re-sequenced sort orders.';

-- Step 7: Summary
PRINT '';
PRINT '=== Summary ===';
SELECT Section, COUNT(*) AS FieldCount
FROM CustomFields
GROUP BY Section
ORDER BY
    CASE Section
        WHEN 'Additional Information' THEN 1
        WHEN 'Environmental Metrics' THEN 2
        WHEN 'Testing & Compliance' THEN 3
        WHEN 'Record Management' THEN 4
        ELSE 5
    END;

PRINT '';
PRINT 'Remaining fields:';
SELECT Id, Name, FieldType, Section, SortOrder
FROM CustomFields
ORDER BY SortOrder;

PRINT '';
PRINT '=== Migration 15 Complete ===';

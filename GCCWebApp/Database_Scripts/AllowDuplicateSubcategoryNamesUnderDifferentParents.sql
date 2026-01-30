-- Migration: AllowDuplicateSubcategoryNamesUnderDifferentParents
-- Date: 2026-01-30
-- Description: Changes the unique constraint on CertificationTypes.Name to allow
--              subcategories to have the same name under different parent types.
--
-- Example: "Baby Products" can now exist under both "Made Safe" AND "EWG Verified"
--
-- Implementation Notes:
-- - SQL Server treats NULL as a unique value in indexes, so we need TWO indexes:
--   1. A filtered unique index for subcategories (ParentId IS NOT NULL)
--   2. A filtered unique index for top-level types (ParentId IS NULL)

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Drop the existing unique index on Name only
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CertificationTypes_Name' AND object_id = OBJECT_ID('CertificationTypes'))
    BEGIN
        DROP INDEX [IX_CertificationTypes_Name] ON [CertificationTypes];
        PRINT 'Dropped existing unique index IX_CertificationTypes_Name';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_CertificationTypes_Name does not exist - skipping drop';
    END

    -- Step 2: Create unique index for subcategories (same name allowed under different parents)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CertificationTypes_Name_ParentId' AND object_id = OBJECT_ID('CertificationTypes'))
    BEGIN
        CREATE UNIQUE INDEX [IX_CertificationTypes_Name_ParentId]
        ON [CertificationTypes] ([Name], [ParentId])
        WHERE [ParentId] IS NOT NULL;

        PRINT 'Created composite unique index IX_CertificationTypes_Name_ParentId for subcategories';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_CertificationTypes_Name_ParentId already exists - skipping create';
    END

    -- Step 3: Create unique index for top-level types (no duplicate names at root level)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CertificationTypes_Name_TopLevel' AND object_id = OBJECT_ID('CertificationTypes'))
    BEGIN
        CREATE UNIQUE INDEX [IX_CertificationTypes_Name_TopLevel]
        ON [CertificationTypes] ([Name])
        WHERE [ParentId] IS NULL;

        PRINT 'Created unique index IX_CertificationTypes_Name_TopLevel for top-level types';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_CertificationTypes_Name_TopLevel already exists - skipping create';
    END

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '=== Migration Complete ===';
    PRINT 'Subcategories can now have duplicate names under different parent types.';
    PRINT 'Top-level certificate types still require unique names.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

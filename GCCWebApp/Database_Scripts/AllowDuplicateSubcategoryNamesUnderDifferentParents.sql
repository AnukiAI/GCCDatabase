-- Migration: AllowDuplicateSubcategoryNamesUnderDifferentParents
-- Date: 2026-01-30
-- Description: Changes unique constraints on CertificationTypes and ProductTypes to allow
--              subcategories/children to have the same name under different parent types.
--
-- Business Rules:
--   - Same name + Same parent = BLOCKED (duplicate)
--   - Same name + Different parent = ALLOWED
--
-- Examples:
--   - "Baby Products" can exist under both "Made Safe" AND "EWG Verified"
--   - "Electronics" can exist under both "Category A" AND "Category B"

BEGIN TRANSACTION;

BEGIN TRY

    -- =============================================
    -- PART 1: CertificationTypes
    -- =============================================

    PRINT '=== Updating CertificationTypes indexes ===';

    -- Step 1a: Drop the existing unique index on Name only
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CertificationTypes_Name' AND object_id = OBJECT_ID('CertificationTypes'))
    BEGIN
        DROP INDEX [IX_CertificationTypes_Name] ON [CertificationTypes];
        PRINT 'Dropped existing unique index IX_CertificationTypes_Name';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_CertificationTypes_Name does not exist - skipping drop';
    END

    -- Step 1b: Create unique index for subcategories (same name allowed under different parents)
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

    -- Step 1c: Create unique index for top-level types (no duplicate names at root level)
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

    -- =============================================
    -- PART 2: ProductTypes
    -- =============================================

    PRINT '';
    PRINT '=== Updating ProductTypes indexes ===';

    -- Step 2a: Check for and remove any duplicate ProductTypes before creating constraint
    -- (This keeps the first entry by ID and removes later duplicates)
    DECLARE @DuplicateCount INT;

    WITH CTE AS (
        SELECT Id, Name, ParentId,
               ROW_NUMBER() OVER (PARTITION BY Name, ParentId ORDER BY Id) AS RowNum
        FROM ProductTypes
    )
    DELETE FROM CTE WHERE RowNum > 1;

    SET @DuplicateCount = @@ROWCOUNT;
    IF @DuplicateCount > 0
        PRINT 'Cleaned up ' + CAST(@DuplicateCount AS VARCHAR(10)) + ' duplicate ProductTypes entries';
    ELSE
        PRINT 'No duplicate ProductTypes found';

    -- Step 2b: Create unique index for child items (same name allowed under different parents)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductTypes_Name_ParentId' AND object_id = OBJECT_ID('ProductTypes'))
    BEGIN
        CREATE UNIQUE INDEX [IX_ProductTypes_Name_ParentId]
        ON [ProductTypes] ([Name], [ParentId])
        WHERE [ParentId] IS NOT NULL;

        PRINT 'Created composite unique index IX_ProductTypes_Name_ParentId for child items';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_ProductTypes_Name_ParentId already exists - skipping create';
    END

    -- Step 2c: Create unique index for top-level items (no duplicate names at root level)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductTypes_Name_TopLevel' AND object_id = OBJECT_ID('ProductTypes'))
    BEGIN
        CREATE UNIQUE INDEX [IX_ProductTypes_Name_TopLevel]
        ON [ProductTypes] ([Name])
        WHERE [ParentId] IS NULL;

        PRINT 'Created unique index IX_ProductTypes_Name_TopLevel for top-level items';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_ProductTypes_Name_TopLevel already exists - skipping create';
    END

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '=== Migration Complete ===';
    PRINT 'CertificationTypes: Subcategories can have duplicate names under different parent types.';
    PRINT 'ProductTypes: Children can have duplicate names under different parent items.';
    PRINT 'Top-level items still require unique names in both tables.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH

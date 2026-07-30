SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Migration: 19_20260320_UpdateCertificationTypes
-- Description: Updates parent certification types and subcategories per client QC feedback (3/9/2026)
--   1. Add missing parent types: Carbon Footprint Reduction, Sustainable Energy Practices,
--      Sustainable Manufacturing Practices, Product Optimization: Carbon Footprint Reduction
--   2. Remove (deactivate) parent types: Renewable Resource Content, Biobased
--   3. Rename: Manufacturer Inventory Verification -> Material Ingredient Reporting
--   4. Add new subcategories under appropriate parents:
--      - Waste Diversion Event (under Waste Diversion from Landfill parent or CSF)
--      - ISO 14001 Certified, Verified Declare Label, Verified HPD (under CSF)
--   5. Ensure subcategory sort orders are alphabetical
-- Author: Claude Code
-- Date: 2026-03-20
-- Source: Client QC Log (Carly, 3/9/2026)

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '================================================================';
    PRINT 'Migration 19: Update Certification Types & Subcategories        ';
    PRINT '================================================================';
    PRINT '';

    -- =====================================================
    -- STEP 1: Add missing PARENT certification types
    -- =====================================================
    PRINT '1. Adding missing parent certification types...';

    IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Carbon Footprint Reduction' AND (ParentId IS NULL OR Level = 1))
    BEGIN
        INSERT INTO dbo.CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
        VALUES ('Carbon Footprint Reduction', 'CFR', 'Carbon footprint reduction certification', 1, 10, 1, NULL);
        PRINT '   + Added: Carbon Footprint Reduction';
    END
    ELSE
        PRINT '   o Carbon Footprint Reduction already exists';

    IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Sustainable Energy Practices' AND (ParentId IS NULL OR Level = 1))
    BEGIN
        INSERT INTO dbo.CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
        VALUES ('Sustainable Energy Practices', 'SEP', 'Sustainable energy practices certification', 1, 11, 1, NULL);
        PRINT '   + Added: Sustainable Energy Practices';
    END
    ELSE
        PRINT '   o Sustainable Energy Practices already exists';

    IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Sustainable Manufacturing Practices' AND (ParentId IS NULL OR Level = 1))
    BEGIN
        INSERT INTO dbo.CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
        VALUES ('Sustainable Manufacturing Practices', 'SMP', 'Sustainable manufacturing practices certification', 1, 12, 1, NULL);
        PRINT '   + Added: Sustainable Manufacturing Practices';
    END
    ELSE
        PRINT '   o Sustainable Manufacturing Practices already exists';

    IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Product Optimization: Carbon Footprint Reduction' AND (ParentId IS NULL OR Level = 1))
    BEGIN
        INSERT INTO dbo.CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
        VALUES ('Product Optimization: Carbon Footprint Reduction', 'POCFR', 'Product optimization carbon footprint reduction certification', 1, 13, 1, NULL);
        PRINT '   + Added: Product Optimization: Carbon Footprint Reduction';
    END
    ELSE
        PRINT '   o Product Optimization: Carbon Footprint Reduction already exists';

    PRINT '';

    -- =====================================================
    -- STEP 2: Deactivate parent types to remove
    -- =====================================================
    PRINT '2. Deactivating unwanted parent certification types...';

    IF EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Renewable Resource Content' AND IsActive = 1)
    BEGIN
        UPDATE dbo.CertificationTypes SET IsActive = 0 WHERE Name = 'Renewable Resource Content';
        PRINT '   - Deactivated: Renewable Resource Content';
    END
    ELSE
        PRINT '   o Renewable Resource Content not found or already inactive';

    IF EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Biobased' AND IsActive = 1 AND (ParentId IS NULL OR Level = 1))
    BEGIN
        UPDATE dbo.CertificationTypes SET IsActive = 0 WHERE Name = 'Biobased' AND (ParentId IS NULL OR Level = 1);
        PRINT '   - Deactivated: Biobased (parent type only, not subcategory)';
    END
    ELSE
        PRINT '   o Biobased parent type not found or already inactive';

    PRINT '';

    -- =====================================================
    -- STEP 3: Rename Manufacturer Inventory Verification
    -- =====================================================
    PRINT '3. Renaming certification types...';

    IF EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Manufacturer Inventory Verification')
    BEGIN
        -- Check if Material Ingredient Reporting already exists as a separate entry
        IF EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Material Ingredient Reporting' AND (ParentId IS NULL OR Level = 1))
        BEGIN
            -- Both exist: deactivate the old name
            UPDATE dbo.CertificationTypes SET IsActive = 0 WHERE Name = 'Manufacturer Inventory Verification';
            PRINT '   ~ Deactivated "Manufacturer Inventory Verification" (Material Ingredient Reporting already exists)';
        END
        ELSE
        BEGIN
            -- Only old name exists: rename it
            UPDATE dbo.CertificationTypes SET Name = 'Material Ingredient Reporting' WHERE Name = 'Manufacturer Inventory Verification';
            PRINT '   ~ Renamed: Manufacturer Inventory Verification -> Material Ingredient Reporting';
        END
    END
    ELSE
        PRINT '   o Manufacturer Inventory Verification not found (may already be named correctly)';

    PRINT '';

    -- =====================================================
    -- STEP 4: Add new subcategories under CSF
    --         (Certified Sustainability Facts)
    -- =====================================================
    PRINT '4. Adding new subcategories...';

    -- Find CSF parent ID dynamically
    DECLARE @csfId INT = (SELECT TOP 1 Id FROM dbo.CertificationTypes WHERE Name = 'Certified Sustainability Facts' AND (ParentId IS NULL OR Level = 1));

    IF @csfId IS NULL
    BEGIN
        PRINT '   WARNING: Certified Sustainability Facts parent type not found! Skipping subcategory inserts.';
    END
    ELSE
    BEGIN
        -- Add Waste Diversion Event (under CSF, related to Waste Diversion from Landfill)
        IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Waste Diversion Event' AND ParentId = @csfId)
        BEGIN
            INSERT INTO dbo.CertificationTypes (Name, ParentId, Level, IsActive, SortOrder)
            VALUES ('Waste Diversion Event', @csfId, 2, 1, 100);
            PRINT '   + Added subcategory: Waste Diversion Event (under CSF)';
        END
        ELSE
            PRINT '   o Waste Diversion Event already exists';

        -- Add ISO 14001 Certified
        IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'ISO 14001 Certified' AND ParentId = @csfId)
        BEGIN
            INSERT INTO dbo.CertificationTypes (Name, ParentId, Level, IsActive, SortOrder)
            VALUES ('ISO 14001 Certified', @csfId, 2, 1, 100);
            PRINT '   + Added subcategory: ISO 14001 Certified (under CSF)';
        END
        ELSE
            PRINT '   o ISO 14001 Certified already exists';

        -- Add Verified Declare Label
        IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Verified Declare Label' AND ParentId = @csfId)
        BEGIN
            INSERT INTO dbo.CertificationTypes (Name, ParentId, Level, IsActive, SortOrder)
            VALUES ('Verified Declare Label', @csfId, 2, 1, 100);
            PRINT '   + Added subcategory: Verified Declare Label (under CSF)';
        END
        ELSE
            PRINT '   o Verified Declare Label already exists';

        -- Add Verified HPD
        IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = 'Verified HPD' AND ParentId = @csfId)
        BEGIN
            INSERT INTO dbo.CertificationTypes (Name, ParentId, Level, IsActive, SortOrder)
            VALUES ('Verified HPD', @csfId, 2, 1, 100);
            PRINT '   + Added subcategory: Verified HPD (under CSF)';
        END
        ELSE
            PRINT '   o Verified HPD already exists';

        -- =====================================================
        -- STEP 5: Re-number sort orders alphabetically for CSF subcategories
        -- =====================================================
        PRINT '';
        PRINT '5. Updating subcategory sort orders to alphabetical...';

        ;WITH Numbered AS (
            SELECT Id, Name, ROW_NUMBER() OVER (ORDER BY Name) AS NewSortOrder
            FROM dbo.CertificationTypes
            WHERE ParentId = @csfId AND IsActive = 1
        )
        UPDATE ct
        SET ct.SortOrder = n.NewSortOrder
        FROM dbo.CertificationTypes ct
        INNER JOIN Numbered n ON ct.Id = n.Id;

        PRINT '   Updated sort orders for all CSF subcategories (alphabetical).';
    END

    PRINT '';
    PRINT '================================================================';
    PRINT 'Migration 19 complete. Summary:';
    PRINT '================================================================';

    -- Print final state
    PRINT '';
    PRINT 'Active Parent Certification Types:';
    SELECT Id, Name, AmazonCode, SortOrder
    FROM dbo.CertificationTypes
    WHERE (ParentId IS NULL OR Level = 1) AND IsActive = 1
    ORDER BY Name;

    IF @csfId IS NOT NULL
    BEGIN
        PRINT '';
        PRINT 'CSF Subcategories (alphabetical):';
        SELECT Id, Name, SortOrder
        FROM dbo.CertificationTypes
        WHERE ParentId = @csfId AND IsActive = 1
        ORDER BY Name;
    END

    COMMIT TRANSACTION;
    PRINT '';
    PRINT 'Transaction committed successfully.';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    PRINT 'Transaction rolled back.';
    THROW;
END CATCH
GO

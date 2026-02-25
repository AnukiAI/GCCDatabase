SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Migration: 17_20260225_SeedCSFSubcategories
-- Description: Adds 38 CSF attributes as Level 2 subcategories under
--              "Certified Sustainability Facts" (CertificationTypes Id: 7).
--              These were previously stored as tags — now properly placed
--              in the certification type hierarchy.
-- Author: Claude Code
-- Date: 2026-02-25
-- Source: CSF Attributes tab from client Excel file

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '================================================================';
    PRINT 'Migration 17: Seed CSF Subcategories under Certified            ';
    PRINT '              Sustainability Facts (Id: 7)                      ';
    PRINT '================================================================';
    PRINT '';

    -- Verify parent exists
    IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Id = 7 AND Name = 'Certified Sustainability Facts')
    BEGIN
        RAISERROR('Parent certification type "Certified Sustainability Facts" (Id: 7) not found!', 16, 1);
        RETURN;
    END

    -- =====================================================
    -- STEP 1: Remove any existing CSF subcategories
    --         (in case this is re-run)
    -- =====================================================
    PRINT '1. Clearing existing CSF subcategories...';

    -- Check for certificates referencing CSF subcategories before deleting
    DECLARE @certCount INT = (
        SELECT COUNT(*) FROM dbo.Certificates
        WHERE CertificationTypeSubcategoryId IN (
            SELECT Id FROM dbo.CertificationTypes WHERE ParentId = 7
        )
    );

    IF @certCount > 0
    BEGIN
        PRINT '   WARNING: ' + CAST(@certCount AS VARCHAR) + ' certificates reference CSF subcategories.';
        PRINT '   Clearing their subcategory references...';
        UPDATE dbo.Certificates
        SET CertificationTypeSubcategoryId = NULL
        WHERE CertificationTypeSubcategoryId IN (
            SELECT Id FROM dbo.CertificationTypes WHERE ParentId = 7
        );
    END

    DELETE FROM dbo.CertificationTypes WHERE ParentId = 7;
    PRINT '   Done.';
    PRINT '';

    -- =====================================================
    -- STEP 2: Insert 38 CSF subcategories
    -- =====================================================
    PRINT '2. Inserting 38 CSF subcategories...';

    INSERT INTO dbo.CertificationTypes (Name, ParentId, Level, IsActive, SortOrder)
    VALUES
        -- Material & Content
        ('Biobased Content',                                          7, 2, 1, 1),
        ('Closed Loop',                                               7, 2, 1, 2),
        ('Dematerialization',                                         7, 2, 1, 3),
        ('Recycled Content',                                          7, 2, 1, 4),
        ('Recyclable Material',                                       7, 2, 1, 5),
        ('Take Back Recycling Program',                               7, 2, 1, 6),

        -- Waste Reduction
        ('Total Waste Reduction',                                     7, 2, 1, 7),
        ('Waste to Landfill Reduction',                               7, 2, 1, 8),
        ('Waste Diversion from Landfill',                             7, 2, 1, 9),
        ('Zero Waste to Landfill',                                    7, 2, 1, 10),

        -- Disclosure & Transparency
        ('Food Transparency Disclosure',                              7, 2, 1, 11),
        ('Low-Emitting Materials Disclosure',                         7, 2, 1, 12),
        ('Material Ingredient Reporting Disclosure',                  7, 2, 1, 13),
        ('Facility Employee Safety Programs',                         7, 2, 1, 14),

        -- Energy & Carbon
        ('Certified Energy Savings',                                  7, 2, 1, 15),
        ('Embodied Carbon Disclosures',                               7, 2, 1, 16),
        ('Environmental Product Declaration',                         7, 2, 1, 17),
        ('Product Carbon Footprint',                                  7, 2, 1, 18),
        ('Facility Carbon Emissions Reduction',                       7, 2, 1, 19),
        ('Facility Carbon Neutral (Scope 1 and Scope 2)',             7, 2, 1, 20),
        ('Life Cycle Assessment (LCA) Optimized',                     7, 2, 1, 21),
        ('Product Optimization: Carbon Footprint Reduction',          7, 2, 1, 22),

        -- Sustainable Energy
        ('Sustainable Energy Practices',                              7, 2, 1, 23),
        ('Facility Energy Usage Reduction',                           7, 2, 1, 24),
        ('Carbon Free or Renewable Energy Usage',                     7, 2, 1, 25),
        ('Carbon Free or Renewable Energy Usage Increase',            7, 2, 1, 26),
        ('Onsite Fossil Fuel Usage Reduction',                        7, 2, 1, 27),

        -- Water & Environmental
        ('Certified Water Savings',                                   7, 2, 1, 28),
        ('Commitment to Animal Welfare',                              7, 2, 1, 29),
        ('Commitment to Land Protection and Preservation',            7, 2, 1, 30),
        ('Facility Environmental Protection and Management Program',  7, 2, 1, 31),
        ('Facility Water Usage Reduction',                            7, 2, 1, 32),
        ('Sustainable Sourcing',                                      7, 2, 1, 33),

        -- Social Responsibility
        ('Community Stewardship',                                     7, 2, 1, 34),
        ('Diversity and Inclusion Program',                           7, 2, 1, 35),
        ('Equity in Employment',                                      7, 2, 1, 36),
        ('Ethical Supply Chain Policies',                              7, 2, 1, 37),
        ('Ethical Business Practices',                                7, 2, 1, 38);

    PRINT '   Inserted 38 CSF subcategories.';
    PRINT '';

    -- =====================================================
    -- STEP 3: Clean up any remaining CSF tags
    -- =====================================================
    PRINT '3. Cleaning up CSF tags (if any remain)...';

    DECLARE @csfTagCount INT = (SELECT COUNT(*) FROM dbo.Tags WHERE TagType = 'csf');

    IF @csfTagCount > 0
    BEGIN
        -- Remove certificate-tag associations first
        DELETE FROM dbo.CertificateTag
        WHERE TagsId IN (SELECT Id FROM dbo.Tags WHERE TagType = 'csf');

        DELETE FROM dbo.Tags WHERE TagType = 'csf';
        PRINT '   Removed ' + CAST(@csfTagCount AS VARCHAR) + ' CSF tags.';
    END
    ELSE
    BEGIN
        PRINT '   No CSF tags found (already cleaned up).';
    END
    PRINT '';

    -- =====================================================
    -- STEP 4: Verification
    -- =====================================================
    PRINT '4. Verification...';

    DECLARE @subcatCount INT = (SELECT COUNT(*) FROM dbo.CertificationTypes WHERE ParentId = 7);
    DECLARE @remainingCsfTags INT = (SELECT COUNT(*) FROM dbo.Tags WHERE TagType = 'csf');

    PRINT '   CSF Subcategories: ' + CAST(@subcatCount AS VARCHAR) + ' (expected 38)';
    PRINT '   Remaining CSF tags: ' + CAST(@remainingCsfTags AS VARCHAR) + ' (expected 0)';
    PRINT '';

    IF @subcatCount = 38 AND @remainingCsfTags = 0
    BEGIN
        PRINT '================================================================';
        PRINT 'Migration 17 COMPLETE - All CSF subcategories created.';
        PRINT '================================================================';
    END
    ELSE
    BEGIN
        PRINT '*** WARNING: Counts do not match expected values! ***';
    END

    COMMIT TRANSACTION;

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '*** MIGRATION FAILED - ROLLED BACK ***';
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT 'Line:  ' + CAST(ERROR_LINE() AS VARCHAR);
END CATCH;

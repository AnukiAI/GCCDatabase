-- ============================================
-- UPDATE CSF Attributes Script
-- ============================================
-- Safely clears existing CSF attributes and loads new ones
-- Handles foreign key relationships in CertificateTag junction table
-- Run this against DEV and PRODUCTION databases
-- Database: SQL Server
-- ============================================

SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'UPDATE CSF Attributes';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- Step 1: Show current state
-- ============================================
PRINT 'Current CSF Attributes count:';
SELECT COUNT(*) AS CurrentCSFCount FROM dbo.Tags WHERE TagType = 'csf';

PRINT '';
PRINT 'Current CSF Attribute assignments to certificates:';
SELECT COUNT(*) AS CertificateCSFAssignments
FROM dbo.CertificateTag ct
INNER JOIN dbo.Tags t ON ct.TagsId = t.Id
WHERE t.TagType = 'csf';

-- ============================================
-- Step 2: Remove CSF attribute assignments from certificates
-- ============================================
PRINT '';
PRINT 'Removing CSF attribute assignments from CertificateTag junction table...';

DELETE ct
FROM dbo.CertificateTag ct
INNER JOIN dbo.Tags t ON ct.TagsId = t.Id
WHERE t.TagType = 'csf';

PRINT '✓ Removed ' + CAST(@@ROWCOUNT AS VARCHAR) + ' CSF attribute assignments from certificates';

-- ============================================
-- Step 3: Delete existing CSF attributes
-- ============================================
PRINT '';
PRINT 'Deleting existing CSF attributes from Tags table...';

DELETE FROM dbo.Tags WHERE TagType = 'csf';

PRINT '✓ Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' existing CSF attributes';

-- ============================================
-- Step 3b: Delete conflicting internal/external tags that match CSF names
-- ============================================
PRINT '';
PRINT 'Removing conflicting tags with same names as CSF attributes...';

-- Delete the internal "Recycled Content" tag if it exists and is not assigned to certificates
DELETE t FROM dbo.Tags t
WHERE t.Name = 'Recycled Content'
  AND t.TagType != 'csf'
  AND NOT EXISTS (SELECT 1 FROM dbo.CertificateTag ct WHERE ct.TagsId = t.Id);

PRINT '✓ Removed ' + CAST(@@ROWCOUNT AS VARCHAR) + ' conflicting tags';

-- ============================================
-- Step 4: Insert new CSF attributes
-- ============================================
PRINT '';
PRINT 'Inserting new CSF attributes...';

SET IDENTITY_INSERT dbo.Tags ON;

INSERT INTO dbo.Tags (Id, Name, TagType, Description, IsActive)
VALUES
    -- Material & Content Attributes
    (1001, 'Biobased Content', 'csf', '', 1),
    (1002, 'Closed Loop', 'csf', '', 1),
    (1003, 'Dematerialization', 'csf', '', 1),
    (1004, 'Recycled Content', 'csf', '', 1),
    (1005, 'Recyclable Material', 'csf', '', 1),
    (1006, 'Take Back Recycling Program', 'csf', '', 1),

    -- Waste Reduction Attributes
    (1007, 'Total Waste Reduction', 'csf', '', 1),
    (1008, 'Waste to Landfill Reduction', 'csf', '', 1),
    (1009, 'Waste Diversion from Landfill', 'csf', '', 1),
    (1010, 'Zero Waste to Landfill', 'csf', '', 1),

    -- Disclosure & Transparency Attributes
    (1011, 'Food Transparency Disclosure', 'csf', '', 1),
    (1012, 'Low-Emitting Materials Disclosure', 'csf', '', 1),
    (1013, 'Material Ingredient Reporting Disclosure', 'csf', '', 1),
    (1014, 'Facility Employee Safety Programs', 'csf', '', 1),

    -- Energy & Carbon Attributes
    (1015, 'Certified Energy Savings', 'csf', '', 1),
    (1016, 'Embodied Carbon Disclosures', 'csf', '', 1),
    (1017, 'Environmental Product Declaration', 'csf', '', 1),
    (1018, 'Product Carbon Footprint', 'csf', '', 1),
    (1019, 'Facility Carbon Emissions Reduction', 'csf', '', 1),
    (1020, 'Facility Carbon Neutral (Scope 1 and Scope 2)', 'csf', '', 1),
    (1021, 'Life Cycle Assessment (LCA) Optimized', 'csf', '', 1),
    (1022, 'Product Optimization: Carbon Footprint Reduction', 'csf', '', 1),

    -- Sustainable Energy Attributes
    (1023, 'Sustainable Energy Practices', 'csf', '', 1),
    (1024, 'Facility Energy Usage Reduction', 'csf', '', 1),
    (1025, 'Carbon Free or Renewable Energy Usage', 'csf', '', 1),
    (1026, 'Carbon Free or Renewable Energy Usage Increase', 'csf', '', 1),
    (1027, 'Onsite Fossil Fuel Usage Reduction', 'csf', '', 1),

    -- Water & Environmental Attributes
    (1028, 'Certified Water Savings', 'csf', '', 1),
    (1029, 'Commitment to Animal Welfare', 'csf', '', 1),
    (1030, 'Commitment to Land Protection and Preservation', 'csf', '', 1),
    (1031, 'Facility Environmental Protection and Management Program', 'csf', '', 1),
    (1032, 'Facility Water Usage Reduction', 'csf', '', 1),
    (1033, 'Sustainable Sourcing', 'csf', '', 1),

    -- Social Responsibility Attributes
    (1034, 'Community Stewardship', 'csf', '', 1),
    (1035, 'Diversity and Inclusion Program', 'csf', '', 1),
    (1036, 'Equity in Employment', 'csf', '', 1),
    (1037, 'Ethical Supply Chain Policies', 'csf', '', 1),
    (1038, 'Ethical Business Practices', 'csf', '', 1);

SET IDENTITY_INSERT dbo.Tags OFF;

PRINT '✓ Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' new CSF attributes';

-- ============================================
-- Step 5: Verify results
-- ============================================
PRINT '';
PRINT '============================================';
PRINT 'VERIFICATION';
PRINT '============================================';

PRINT '';
PRINT 'New CSF Attributes loaded:';
SELECT Id, Name, TagType, IsActive FROM dbo.Tags WHERE TagType = 'csf' ORDER BY Id;

PRINT '';
PRINT 'Summary:';
SELECT
    COUNT(*) AS TotalCSFAttributes,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveAttributes
FROM dbo.Tags
WHERE TagType = 'csf';

-- Commit transaction
COMMIT TRANSACTION;

PRINT '';
PRINT '============================================';
PRINT 'CSF ATTRIBUTES UPDATE COMPLETE';
PRINT '============================================';
PRINT '';
PRINT 'Categories:';
PRINT '  - Material & Content: 6 attributes (1001-1006)';
PRINT '  - Waste Reduction: 4 attributes (1007-1010)';
PRINT '  - Disclosure & Transparency: 4 attributes (1011-1014)';
PRINT '  - Energy & Carbon: 8 attributes (1015-1022)';
PRINT '  - Sustainable Energy: 5 attributes (1023-1027)';
PRINT '  - Water & Environmental: 6 attributes (1028-1033)';
PRINT '  - Social Responsibility: 5 attributes (1034-1038)';
PRINT '  - TOTAL: 38 attributes';
PRINT '';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED DURING UPDATE';
    PRINT '============================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR);
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR);
    PRINT '============================================';

    -- Re-throw the error
    THROW;
END CATCH;

GO

-- ============================================
-- CSF Attributes Data Seeding Script
-- ============================================
-- Seeds Certified Sustainability Facts (CSF) attributes into Tags table
-- CSF Attributes are stored as Tags with TagType = 'csf'
-- Safe to run multiple times (idempotent)
-- Database: SQL Server
-- ============================================

SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'CSF Attributes Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- CSF Attributes (stored in Tags table)
-- ============================================
PRINT 'Seeding CSF Attributes into Tags table...';

-- Check if table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tags')
BEGIN
    PRINT '❌ ERROR: Tags table does not exist';
    PRINT '   Please run database migrations first: dotnet ef database update';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- CSF Attributes are stored in Tags table with TagType = 'csf'
-- First remove any CSF attribute assignments from CertificateTag junction table
DELETE ct
FROM dbo.CertificateTag ct
INNER JOIN dbo.Tags t ON ct.TagsId = t.Id
WHERE t.TagType = 'csf';
PRINT '✓ Removed CSF attribute assignments from certificates';

-- Then delete existing CSF attributes to ensure clean state
DELETE FROM dbo.Tags WHERE TagType = 'csf';
PRINT '✓ Cleared existing CSF Attributes';

SET IDENTITY_INSERT dbo.Tags ON;

INSERT INTO dbo.Tags (Id, Name, TagType, Description, IsActive)
VALUES
    -- Material & Content Attributes
    (1001, 'CSF: Biobased Content', 'csf', 'Product contains biobased or bio-derived materials', 1),
    (1002, 'CSF: Closed Loop', 'csf', 'Product participates in closed loop manufacturing system', 1),
    (1003, 'CSF: Dematerialization', 'csf', 'Product designed with reduced material usage', 1),
    (1004, 'CSF: Recycled Content', 'csf', 'Product contains recycled materials', 1),
    (1005, 'CSF: Recyclable Material', 'csf', 'Product materials can be recycled', 1),
    (1006, 'CSF: Take Back Recycling Program', 'csf', 'Manufacturer offers take-back recycling program', 1),

    -- Waste Reduction Attributes
    (1007, 'CSF: Total Waste Reduction', 'csf', 'Product or process demonstrates total waste reduction', 1),
    (1008, 'CSF: Waste to Landfill Reduction', 'csf', 'Product or process reduces waste sent to landfill', 1),
    (1009, 'CSF: Waste Diversion from Landfill', 'csf', 'Product or process diverts waste from landfill', 1),
    (1010, 'CSF: Zero Waste to Landfill', 'csf', 'Product or facility achieves zero waste to landfill', 1),

    -- Disclosure & Transparency Attributes
    (1011, 'CSF: Food Transparency Disclosure', 'csf', 'Product provides food ingredient transparency', 1),
    (1012, 'CSF: Low-Emitting Materials Disclosure', 'csf', 'Product discloses low-emitting materials information', 1),
    (1013, 'CSF: Material Ingredient Reporting', 'csf', 'Product provides material ingredient reporting', 1),
    (1014, 'CSF: Facility Employee Safety Programs', 'csf', 'Facility maintains employee safety programs', 1),

    -- Energy & Carbon Attributes
    (1015, 'CSF: Certified Energy Savings', 'csf', 'Product demonstrates certified energy savings', 1),
    (1016, 'CSF: Embodied Carbon Disclosures', 'csf', 'Product discloses embodied carbon information', 1),
    (1017, 'CSF: Environmental Product Declaration', 'csf', 'Product has Environmental Product Declaration (EPD)', 1),
    (1018, 'CSF: Product Carbon Footprint', 'csf', 'Product carbon footprint has been measured and disclosed', 1),
    (1019, 'CSF: Facility Carbon Emissions Reduction', 'csf', 'Facility demonstrates carbon emissions reduction', 1),
    (1020, 'CSF: Facility Carbon Neutral', 'csf', 'Facility is carbon neutral for Scope 1 and Scope 2 emissions', 1),
    (1021, 'CSF: Life Cycle Assessment Optimized', 'csf', 'Product optimized through Life Cycle Assessment', 1),
    (1022, 'CSF: Carbon Footprint Reduction', 'csf', 'Product optimized for carbon footprint reduction', 1),

    -- Sustainable Energy Attributes
    (1023, 'CSF: Sustainable Energy Practices', 'csf', 'Facility or product uses sustainable energy practices', 1),
    (1024, 'CSF: Facility Energy Usage Reduction', 'csf', 'Facility demonstrates energy usage reduction', 1),
    (1025, 'CSF: Carbon Free or Renewable Energy', 'csf', 'Facility uses carbon free or renewable energy', 1),
    (1026, 'CSF: Renewable Energy Usage Increase', 'csf', 'Facility demonstrates increase in carbon free or renewable energy usage', 1),
    (1027, 'CSF: Onsite Fossil Fuel Reduction', 'csf', 'Facility demonstrates reduction in onsite fossil fuel usage', 1),

    -- Water & Environmental Attributes
    (1028, 'CSF: Certified Water Savings', 'csf', 'Product demonstrates certified water savings', 1),
    (1029, 'CSF: Commitment to Animal Welfare', 'csf', 'Product or company demonstrates commitment to animal welfare', 1),
    (1030, 'CSF: Land Protection and Preservation', 'csf', 'Company demonstrates commitment to land protection and preservation', 1),
    (1031, 'CSF: Environmental Management Program', 'csf', 'Facility maintains environmental protection and management program', 1),
    (1032, 'CSF: Facility Water Usage Reduction', 'csf', 'Facility demonstrates water usage reduction', 1),
    (1033, 'CSF: Sustainable Sourcing', 'csf', 'Product uses sustainably sourced materials', 1),

    -- Social Responsibility Attributes
    (1034, 'CSF: Community Stewardship', 'csf', 'Company demonstrates community stewardship', 1),
    (1035, 'CSF: Diversity and Inclusion Program', 'csf', 'Company maintains diversity and inclusion program', 1),
    (1036, 'CSF: Equity in Employment', 'csf', 'Company demonstrates equity in employment practices', 1),
    (1037, 'CSF: Ethical Supply Chain Policies', 'csf', 'Company maintains ethical supply chain policies', 1),
    (1038, 'CSF: Ethical Business Practices', 'csf', 'Company demonstrates ethical business practices', 1);

SET IDENTITY_INSERT dbo.Tags OFF;

DECLARE @AttributeCount INT = @@ROWCOUNT;
PRINT '✓ ' + CAST(@AttributeCount AS VARCHAR) + ' CSF Attributes created in Tags table';

PRINT '';

-- Commit transaction
COMMIT TRANSACTION;

PRINT '';
PRINT '============================================';
PRINT 'CSF ATTRIBUTES SEEDING COMPLETE';
PRINT '============================================';
PRINT '';
PRINT 'Summary:';
SELECT
    COUNT(*) AS TotalCSFAttributes,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveAttributes
FROM dbo.Tags
WHERE TagType = 'csf';

PRINT '';
PRINT 'CSF Attribute Categories:';
PRINT '  - Material & Content: 6 attributes';
PRINT '  - Waste Reduction: 4 attributes';
PRINT '  - Disclosure & Transparency: 4 attributes';
PRINT '  - Energy & Carbon: 8 attributes';
PRINT '  - Sustainable Energy: 5 attributes';
PRINT '  - Water & Environmental: 6 attributes';
PRINT '  - Social Responsibility: 5 attributes';
PRINT '  - TOTAL: 38 attributes';

PRINT '';
PRINT 'These attributes support the Certified Sustainability Facts (CSF) program,';
PRINT 'providing comprehensive sustainability data for certified products.';
PRINT 'All CSF attributes are stored in the Tags table with TagType = ''csf''.';

PRINT '';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED DURING SEEDING';
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

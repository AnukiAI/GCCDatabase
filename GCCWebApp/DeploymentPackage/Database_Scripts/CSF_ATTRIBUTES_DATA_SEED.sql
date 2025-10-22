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
IF NOT EXISTS (SELECT 1 FROM dbo.Tags WHERE TagType = 'csf')
BEGIN
    SET IDENTITY_INSERT dbo.Tags ON;

    INSERT INTO dbo.Tags (Id, Name, TagType, Description, IsActive)
    VALUES
        -- Environmental Impact Attributes
        (1001, 'Carbon Footprint', 'csf', 'Total greenhouse gas emissions in kg CO2 equivalent per unit', 1),
        (1002, 'Water Consumption', 'csf', 'Total water used in production in liters per unit', 1),
        (1003, 'Energy Consumption', 'csf', 'Total energy used in manufacturing in kWh per unit', 1),
        (1004, 'Waste Generated', 'csf', 'Total waste produced during manufacturing in kg per unit', 1),
        (1005, 'Air Quality Impact', 'csf', 'VOC and particulate emissions rating (1-10 scale)', 1),

        -- Material Content Attributes
        (1006, 'Recycled Content Percentage', 'csf', 'Percentage of recycled materials by weight (0-100%)', 1),
        (1007, 'Post-Consumer Recycled Content', 'csf', 'Percentage of post-consumer recycled materials (0-100%)', 1),
        (1008, 'Bio-Based Content Percentage', 'csf', 'Percentage of bio-based or renewable materials (0-100%)', 1),
        (1009, 'Hazardous Material Free', 'csf', 'Indicates product is free of hazardous substances (Yes/No)', 1),
        (1010, 'Heavy Metals Content', 'csf', 'Contains lead, cadmium, mercury, or other heavy metals (Yes/No/PPM level)', 1),

        -- Chemical Safety Attributes
        (1011, 'VOC Emissions Level', 'csf', 'Volatile organic compound emissions in g/L', 1),
        (1012, 'Formaldehyde Free', 'csf', 'Product contains no added formaldehyde (Yes/No)', 1),
        (1013, 'Phthalate Free', 'csf', 'Product is free of phthalates (Yes/No)', 1),
        (1014, 'BPA Free', 'csf', 'Product contains no Bisphenol A (Yes/No)', 1),
        (1015, 'Flame Retardant Free', 'csf', 'Product contains no halogenated flame retardants (Yes/No)', 1),

        -- Energy & Efficiency Attributes
        (1016, 'Energy Efficiency Rating', 'csf', 'Energy efficiency classification (A+++ to G scale)', 1),
        (1017, 'Annual Energy Cost', 'csf', 'Estimated annual energy cost in USD', 1),
        (1018, 'Standby Power Consumption', 'csf', 'Power consumption in standby mode in watts', 1),
        (1019, 'Operational Efficiency', 'csf', 'Percentage efficiency during normal operation (0-100%)', 1),
        (1020, 'Energy Star Qualified', 'csf', 'Meets Energy Star program requirements (Yes/No)', 1),

        -- Product Lifecycle Attributes
        (1021, 'Expected Product Lifespan', 'csf', 'Expected usable life in years', 1),
        (1022, 'Recyclability Percentage', 'csf', 'Percentage of product that can be recycled at end of life (0-100%)', 1),
        (1023, 'Biodegradable Components', 'csf', 'Percentage of biodegradable components (0-100%)', 1),
        (1024, 'Disassembly Rating', 'csf', 'Ease of disassembly for recycling (1-10 scale)', 1),
        (1025, 'Circular Economy Compatibility', 'csf', 'Product designed for circular economy principles (Yes/No)', 1),

        -- Social Responsibility Attributes
        (1026, 'Fair Trade Certified', 'csf', 'Product meets fair trade standards (Yes/No)', 1),
        (1027, 'Conflict-Free Materials', 'csf', 'Uses only conflict-free sourced materials (Yes/No)', 1),
        (1028, 'Labor Standards Compliance', 'csf', 'Manufactured under fair labor conditions (Yes/No)', 1),
        (1029, 'Local Sourcing Percentage', 'csf', 'Percentage of materials sourced within 500 miles (0-100%)', 1),
        (1030, 'Community Impact Score', 'csf', 'Positive community impact rating (1-10 scale)', 1),

        -- Certification & Standards Attributes
        (1031, 'FSC Certified', 'csf', 'Forest Stewardship Council certified (Yes/No)', 1),
        (1032, 'PEFC Certified', 'csf', 'Programme for the Endorsement of Forest Certification (Yes/No)', 1),
        (1033, 'SFI Certified', 'csf', 'Sustainable Forestry Initiative certified (Yes/No)', 1),
        (1034, 'LEED Contribution Points', 'csf', 'Number of LEED points this product contributes (0-10)', 1),
        (1035, 'GREENGUARD Certified', 'csf', 'GREENGUARD certification level (None/Certified/Gold)', 1),
        (1036, 'Cradle to Cradle Level', 'csf', 'Cradle to Cradle certification level (Basic/Bronze/Silver/Gold/Platinum)', 1),
        (1037, 'ISO 14001 Compliant', 'csf', 'Manufactured in ISO 14001 certified facility (Yes/No)', 1),
        (1038, 'Carbon Neutral Certified', 'csf', 'Product is carbon neutral certified (Yes/No)', 1),

        -- Packaging Attributes
        (1039, 'Packaging Recycled Content', 'csf', 'Percentage of recycled content in packaging (0-100%)', 1),
        (1040, 'Packaging Recyclability', 'csf', 'Percentage of packaging that is recyclable (0-100%)', 1),
        (1041, 'Minimal Packaging Design', 'csf', 'Uses minimal/optimized packaging (Yes/No)', 1),
        (1042, 'Plastic-Free Packaging', 'csf', 'Packaging contains no plastic materials (Yes/No)', 1),

        -- Transportation & Logistics Attributes
        (1043, 'Transportation Carbon Impact', 'csf', 'CO2 emissions from transportation in kg', 1),
        (1044, 'Local Manufacturing', 'csf', 'Manufactured within regional market (Yes/No)', 1),
        (1045, 'Shipping Distance', 'csf', 'Average shipping distance to point of sale in km', 1),

        -- Indoor Environmental Quality Attributes
        (1046, 'Indoor Air Quality Certified', 'csf', 'Certified for low indoor air pollution (Yes/No)', 1),
        (1047, 'Low Emission Product', 'csf', 'Meets California Section 01350 standards (Yes/No)', 1),
        (1048, 'Odor-Free Certification', 'csf', 'Product has minimal odor emissions (Yes/No)', 1),
        (1049, 'Antimicrobial Properties', 'csf', 'Product has natural antimicrobial properties (Yes/No)', 1),
        (1050, 'Allergen-Free Certification', 'csf', 'Product is certified allergen-free (Yes/No)', 1);

    SET IDENTITY_INSERT dbo.Tags OFF;

    DECLARE @AttributeCount INT = @@ROWCOUNT;
    PRINT '✓ ' + CAST(@AttributeCount AS VARCHAR) + ' CSF Attributes created in Tags table';
END
ELSE
BEGIN
    PRINT '○ CSF Attributes already exist, skipping';
END

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
PRINT '  - Environmental Impact: 5 attributes';
PRINT '  - Material Content: 5 attributes';
PRINT '  - Chemical Safety: 5 attributes';
PRINT '  - Energy & Efficiency: 5 attributes';
PRINT '  - Product Lifecycle: 5 attributes';
PRINT '  - Social Responsibility: 5 attributes';
PRINT '  - Certification & Standards: 8 attributes';
PRINT '  - Packaging: 4 attributes';
PRINT '  - Transportation & Logistics: 3 attributes';
PRINT '  - Indoor Environmental Quality: 5 attributes';

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

-- ============================================
-- Certificate Fields Data Seeding Script
-- ============================================
-- Seeds dynamic certificate field definitions
-- Safe to run multiple times (idempotent)
-- Database: SQL Server
-- ============================================

SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'Certificate Fields Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- Certificate Fields
-- ============================================
PRINT 'Seeding Certificate Fields...';

-- Check if table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificateFields')
BEGIN
    PRINT '❌ ERROR: CertificateFields table does not exist';
    PRINT '   Please run database migrations first: dotnet ef database update';
    ROLLBACK TRANSACTION;
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.CertificateFields)
BEGIN
    SET IDENTITY_INSERT dbo.CertificateFields ON;

    INSERT INTO dbo.CertificateFields (Id, Name, FieldType, IsRequired, IsVisible, SortOrder, Options, DefaultValue, ValidationRules, IsActive, CreatedAt, UpdatedAt)
    VALUES
        -- Basic Information Fields
        (1, 'Certificate Number', 'text', 1, 1, 1, NULL, NULL, '{"maxLength":20,"pattern":"^[0-9]{2}-[0-9]{4}$"}', 1, GETDATE(), GETDATE()),
        (2, 'Effective Date', 'date', 1, 1, 2, NULL, NULL, '{"minDate":"2020-01-01"}', 1, GETDATE(), GETDATE()),
        (3, 'Expiration Date', 'date', 1, 1, 3, NULL, NULL, '{"minDate":"effectiveDate"}', 1, GETDATE(), GETDATE()),
        (4, 'Certificate Status', 'dropdown', 1, 1, 4, 'active,draft,pending,suspended,expired,expiring_soon', 'draft', NULL, 1, GETDATE(), GETDATE()),

        -- Company & Product Fields
        (5, 'Company Name', 'text', 1, 1, 5, NULL, NULL, '{"maxLength":255}', 1, GETDATE(), GETDATE()),
        (6, 'Product Name', 'text', 1, 1, 6, NULL, NULL, '{"maxLength":255}', 1, GETDATE(), GETDATE()),
        (7, 'Product Category', 'dropdown', 0, 1, 7, 'Building Materials,Electronics / Electrical,Materials / Plastics,Paper Products,Textiles,Consumer Products', NULL, NULL, 1, GETDATE(), GETDATE()),

        -- Location Fields
        (8, 'Manufacturing Location', 'text', 0, 1, 8, NULL, NULL, '{"maxLength":255}', 1, GETDATE(), GETDATE()),
        (9, 'Country of Origin', 'text', 0, 1, 9, NULL, 'United States', '{"maxLength":100}', 1, GETDATE(), GETDATE()),

        -- Product Identifiers
        (10, 'ASIN', 'text', 0, 1, 10, NULL, NULL, '{"maxLength":10,"pattern":"^B[0-9A-Z]{9}$"}', 1, GETDATE(), GETDATE()),
        (11, 'UPC', 'text', 0, 1, 11, NULL, NULL, '{"maxLength":12,"pattern":"^[0-9]{12}$"}', 1, GETDATE(), GETDATE()),
        (12, 'GTIN', 'text', 0, 1, 12, NULL, NULL, '{"maxLength":14,"pattern":"^[0-9]{14}$"}', 1, GETDATE(), GETDATE()),
        (13, 'EAN', 'text', 0, 1, 13, NULL, NULL, '{"maxLength":13,"pattern":"^[0-9]{13}$"}', 1, GETDATE(), GETDATE()),
        (14, 'SKU', 'text', 0, 1, 14, NULL, NULL, '{"maxLength":50}', 1, GETDATE(), GETDATE()),

        -- Certification Details
        (15, 'Certification Type', 'dropdown', 1, 1, 15, 'CEF,CSF,Recycled Content,Energy Savings,Material Ingredient Reporting', NULL, NULL, 1, GETDATE(), GETDATE()),
        (16, 'Certification Body', 'text', 0, 1, 16, NULL, 'Green Circle Certified', '{"maxLength":255}', 1, GETDATE(), GETDATE()),
        (17, 'Auditor Name', 'text', 0, 1, 17, NULL, NULL, '{"maxLength":255}', 1, GETDATE(), GETDATE()),
        (18, 'Audit Date', 'date', 0, 1, 18, NULL, NULL, NULL, 1, GETDATE(), GETDATE()),

        -- Environmental Data
        (19, 'Carbon Footprint (kg CO2e)', 'number', 0, 1, 19, NULL, NULL, '{"min":0,"precision":2}', 1, GETDATE(), GETDATE()),
        (20, 'Recycled Content Percentage', 'number', 0, 1, 20, NULL, NULL, '{"min":0,"max":100,"precision":1}', 1, GETDATE(), GETDATE()),
        (21, 'VOC Emissions (g/L)', 'number', 0, 1, 21, NULL, NULL, '{"min":0,"precision":2}', 1, GETDATE(), GETDATE()),
        (22, 'Energy Efficiency Rating', 'dropdown', 0, 1, 22, 'A+++,A++,A+,A,B,C,D,E,F,G', NULL, NULL, 1, GETDATE(), GETDATE()),

        -- Compliance & Standards
        (23, 'Meets FSC Standards', 'boolean', 0, 1, 23, NULL, 'false', NULL, 1, GETDATE(), GETDATE()),
        (24, 'LEED Eligible', 'boolean', 0, 1, 24, NULL, 'false', NULL, 1, GETDATE(), GETDATE()),
        (25, 'Energy Star Certified', 'boolean', 0, 1, 25, NULL, 'false', NULL, 1, GETDATE(), GETDATE()),
        (26, 'Cradle to Cradle Certified', 'boolean', 0, 1, 26, NULL, 'false', NULL, 1, GETDATE(), GETDATE()),

        -- Additional Information
        (27, 'Certificate Notes', 'textarea', 0, 1, 27, NULL, NULL, '{"maxLength":2000}', 1, GETDATE(), GETDATE()),
        (28, 'Internal Reference', 'text', 0, 1, 28, NULL, NULL, '{"maxLength":100}', 1, GETDATE(), GETDATE()),
        (29, 'Document URL', 'text', 0, 1, 29, NULL, NULL, '{"maxLength":500,"pattern":"^https?://.*"}', 1, GETDATE(), GETDATE()),
        (30, 'Tags', 'text', 0, 1, 30, NULL, NULL, '{"maxLength":500}', 1, GETDATE(), GETDATE());

    SET IDENTITY_INSERT dbo.CertificateFields OFF;

    DECLARE @FieldCount INT = @@ROWCOUNT;
    PRINT '✓ ' + CAST(@FieldCount AS VARCHAR) + ' Certificate Fields created';
END
ELSE
BEGIN
    PRINT '○ Certificate Fields already exist, skipping';
END

PRINT '';

-- Commit transaction
COMMIT TRANSACTION;

PRINT '';
PRINT '============================================';
PRINT 'CERTIFICATE FIELDS SEEDING COMPLETE';
PRINT '============================================';
PRINT '';
PRINT 'Summary:';
SELECT
    COUNT(*) AS TotalFields,
    SUM(CASE WHEN IsRequired = 1 THEN 1 ELSE 0 END) AS RequiredFields,
    SUM(CASE WHEN IsVisible = 1 THEN 1 ELSE 0 END) AS VisibleFields,
    COUNT(DISTINCT FieldType) AS UniqueFieldTypes
FROM dbo.CertificateFields;

PRINT '';
PRINT 'Field Types:';
SELECT FieldType, COUNT(*) AS Count
FROM dbo.CertificateFields
GROUP BY FieldType
ORDER BY COUNT(*) DESC;

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

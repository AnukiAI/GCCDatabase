USE gccwebap_certificates;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT 'Clearing existing CustomFields...';
    DELETE FROM CustomFields;
    DBCC CHECKIDENT ('CustomFields', RESEED, 0);
    
    PRINT 'Importing CustomFields data...';
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (1, N'Certificate Holder Company Name', N'text', 1, 1, 1, 1, N
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (2, N'Certificate Holder Contact Email', N'email', 1, 1, 1, 2,
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (3, N'Certificate Holder Phone Number', N'phone', 0, 1, 1, 3, 
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (4, N'Certificate Holder Address', N'textarea', 0, 1, 1, 4, N'
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (5, N'Product Brand Name', N'text', 0, 1, 1, 5, N'', N'', N'ma
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (6, N'Product Model Number', N'text', 0, 1, 1, 6, N'', N'', N'
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (7, N'Product SKU', N'text', 0, 1, 1, 7, N'', N'', N'maxLength
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (8, N'Product Dimensions', N'text', 0, 1, 1, 8, N'', N'', N'ma
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (9, N'Product Weight', N'number', 0, 1, 1, 9, N'', N'', N'nume
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (10, N'Product Color Options', N'text', 0, 1, 1, 10, N'', N'',
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (11, N'Manufacturing Facility Name', N'text', 0, 1, 1, 11, N''
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (12, N'Manufacturing Facility Location', N'text', 0, 1, 1, 12,
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (13, N'Manufacturing Country', N'select', 0, 1, 1, 13, N'Unite
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (14, N'Manufacturing Date', N'date', 0, 1, 1, 14, N'', N'', N'
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (15, N'Environmental Impact Statement', N'textarea', 0, 1, 1, 
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (16, N'Third-Party Certifications', N'multiselect', 0, 1, 1, 1
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (17, N'Carbon Footprint (kg CO2e)', N'number', 0, 1, 1, 17, N'
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (18, N'Water Usage (liters)', N'number', 0, 1, 1, 18, N'', N''
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (19, N'Energy Consumption (kWh)', N'number', 0, 1, 1, 19, N'',
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (20, N'Recycled Content Percentage', N'number', 0, 1, 1, 20, N
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (21, N'Test Report Number', N'text', 0, 1, 1, 21, N'', N'', N'
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (22, N'Test Laboratory Name', N'text', 0, 1, 1, 22, N'', N'', 
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (23, N'Test Date', N'date', 0, 1, 1, 23, N'', N'', N'date', 1)
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (24, N'Compliance Standards', N'multiselect', 0, 1, 1, 24, N'I
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (25, N'Special Notes', N'textarea', 0, 1, 1, 25, N'', N'', N'm
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (26, N'Internal Reference Number', N'text', 0, 1, 1, 26, N'', 
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (27, N'Priority Level', N'select', 0, 1, 1, 27, N'Low,Normal,H
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (28, N'Certificate Visibility', N'select', 0, 1, 1, 28, N'Publ
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (29, N'Renewal Reminder Date', N'date', 0, 1, 1, 29, N'', N'',
SET IDENTITY_INSERT CustomFields ON; INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId) VALUES (30, N'Assigned Analyst', N'text', 0, 1, 1, 30, N'', N'', N'ma

    SET IDENTITY_INSERT CustomFields OFF;
    
    COMMIT TRANSACTION;
    
    PRINT 'SUCCESS: CustomFields imported';
    PRINT 'Total records: ';
    SELECT COUNT(*) AS CustomFieldsCount FROM CustomFields;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

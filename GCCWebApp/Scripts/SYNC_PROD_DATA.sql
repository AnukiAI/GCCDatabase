-- =============================================
-- PRODUCTION DATABASE SYNC SCRIPT
-- Generated: 2025-12-04
-- Source: Development Database (ANUKI01)
-- Target: Production Database (68.71.129.120,1533)
-- =============================================

-- PART 1: Apply Missing Migrations Schema Changes
-- =============================================

-- Migration: 20251123161550_AddAuditHistoryTable
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditHistory')
BEGIN
    CREATE TABLE [dbo].[AuditHistory] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [Changes] nvarchar(max) NULL,
        [UserId] int NULL,
        [UserName] nvarchar(256) NULL,
        [Timestamp] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_AuditHistory] PRIMARY KEY ([Id])
    );
    PRINT 'Created AuditHistory table';
END
GO

-- Migration: 20251126212053_AddCertificationResultsToCertificate
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Certificates') AND name = 'CertificationResults')
BEGIN
    ALTER TABLE [dbo].[Certificates] ADD [CertificationResults] nvarchar(max) NULL;
    PRINT 'Added CertificationResults column to Certificates';
END
GO

-- Record the migrations
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251123161550_AddAuditHistoryTable')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251123161550_AddAuditHistoryTable', '9.0.0');
    PRINT 'Recorded migration: 20251123161550_AddAuditHistoryTable';
END
GO

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251126212053_AddCertificationResultsToCertificate')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251126212053_AddCertificationResultsToCertificate', '9.0.0');
    PRINT 'Recorded migration: 20251126212053_AddCertificationResultsToCertificate';
END
GO

-- PART 2: Sync CertificationTypes
-- =============================================
PRINT 'Syncing CertificationTypes...';

-- First, update existing records to match dev
UPDATE CertificationTypes SET Name = 'Biobased', [Level] = 1 WHERE Id = 1;
UPDATE CertificationTypes SET Name = 'Biodegradable', [Level] = 1 WHERE Id = 2;
UPDATE CertificationTypes SET Name = 'Carbon Footprint Reduction', [Level] = 1 WHERE Id = 3;
UPDATE CertificationTypes SET Name = 'Certified Energy Savings', [Level] = 1 WHERE Id = 4;
UPDATE CertificationTypes SET Name = 'Certified Environmental Facts', [Level] = 1 WHERE Id = 5;

-- Insert missing CertificationTypes (IDs 6-22)
SET IDENTITY_INSERT CertificationTypes ON;

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 6)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (6, 'Certified Sustainability Facts', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 7)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (7, 'Certified Water Savings', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 8)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (8, 'Closed Loop Product', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 9)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (9, 'Dematerialization', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 10)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (10, 'Greenhouse Gas (GHG) Verification', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 11)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (11, 'GreenCircle Audited', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 12)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (12, 'Life Cycle Assessment (LCA) Optimized', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 13)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (13, 'Manufacturer Inventory Verification', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 14)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (14, 'Recyclable Material', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 15)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (15, 'Recycled Content', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 16)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (16, 'Renewable Resource Content', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 17)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (17, 'Sustainable Energy Practices', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 18)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (18, 'Sustainable Manufacturing Practices', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 19)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (19, 'Waste Diversion from Landfill', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 20)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (20, 'Waste System Audit', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 21)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (21, '+Vantage VinylT Verified', 1, 1, NULL);
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 22)
    INSERT INTO CertificationTypes (Id, Name, [Level], IsActive, ParentId) VALUES (22, 'Preloved', 1, 1, NULL);

SET IDENTITY_INSERT CertificationTypes OFF;
PRINT 'CertificationTypes synced: 22 records';
GO

-- PART 3: Sync Tags
-- =============================================
PRINT 'Syncing Tags...';

-- Update existing tags names to match dev
UPDATE Tags SET Name = 'APR' WHERE Id = 1;
UPDATE Tags SET Name = 'LEED' WHERE Id = 2;
UPDATE Tags SET Name = 'High Priority Client' WHERE Id = 3;
UPDATE Tags SET Name = 'Q4 Review' WHERE Id = 4;
UPDATE Tags SET Name = 'Environmental Facts' WHERE Id = 5;
UPDATE Tags SET Name = 'Recycled Content' WHERE Id = 6;
UPDATE Tags SET Name = 'Carbon Neutral' WHERE Id = 7;
UPDATE Tags SET Name = 'Energy Star' WHERE Id = 8;
UPDATE Tags SET Name = 'Cradle to Cradle' WHERE Id = 9;
UPDATE Tags SET Name = 'Forest Stewardship Council' WHERE Id = 10;

-- Insert missing tags (IDs 11-26)
SET IDENTITY_INSERT Tags ON;

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 11)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (11, 'GREENGUARD', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 12)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (12, 'Bio-Based', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 13)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (13, 'Renewable Energy', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 14)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (14, 'Circular Economy', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 15)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (15, 'Water Stewardship', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 16)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (16, 'Fair Trade', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 17)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (17, 'Urgent Review', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 18)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (18, 'Annual Audit', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 19)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (19, 'Customer Request', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 20)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (20, 'Regulatory Compliance', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 21)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (21, 'Market Expansion', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 22)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (22, 'Partnership Program', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 23)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (23, 'Innovation Project', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 24)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (24, 'Cost Optimization', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 25)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (25, 'Quality Improvement', 1);
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 26)
    INSERT INTO Tags (Id, Name, IsActive) VALUES (26, 'Digital Transformation', 1);

SET IDENTITY_INSERT Tags OFF;
PRINT 'Tags synced: 26 records';
GO

-- PART 4: Sync CustomFields
-- =============================================
PRINT 'Syncing CustomFields...';

-- Delete existing custom fields (if any) and insert fresh
DELETE FROM CustomFields;

SET IDENTITY_INSERT CustomFields ON;

INSERT INTO CustomFields (Id, Name, FieldType, IsRequired, IsVisible, IsPublic, SortOrder, Options, DefaultValue, ValidationRules, CertificateTypeId, Section) VALUES
(1, 'Certificate Holder Company Name', 'text', 1, 1, 1, 1, '', '', 'maxLength:255', 1, NULL),
(2, 'Certificate Holder Contact Email', 'email', 1, 1, 1, 2, '', '', 'email,maxLength:255', 1, NULL),
(3, 'Certificate Holder Phone Number', 'phone', 0, 1, 1, 3, '', '', 'phone,maxLength:20', 1, NULL),
(4, 'Certificate Holder Address', 'textarea', 0, 1, 1, 4, '', '', 'maxLength:500', 1, NULL),
(5, 'Product Brand Name', 'text', 0, 1, 1, 5, '', '', 'maxLength:255', 1, NULL),
(6, 'Product Model Number', 'text', 0, 1, 1, 6, '', '', 'maxLength:100', 1, NULL),
(7, 'Product SKU', 'text', 0, 1, 1, 7, '', '', 'maxLength:100', 1, NULL),
(8, 'Product Dimensions', 'text', 0, 1, 1, 8, '', '', 'maxLength:100', 1, NULL),
(9, 'Product Weight', 'number', 0, 1, 1, 9, '', '', 'numeric,min:0', 1, NULL),
(10, 'Product Color Options', 'text', 0, 1, 1, 10, '', '', 'maxLength:255', 1, NULL),
(11, 'Manufacturing Facility Name', 'text', 0, 1, 1, 11, '', '', 'maxLength:255', 1, NULL),
(12, 'Manufacturing Facility Location', 'text', 0, 1, 1, 12, '', '', 'maxLength:255', 1, NULL),
(13, 'Manufacturing Country', 'select', 0, 1, 1, 13, 'United States,Canada,Mexico,China,India,Germany,United Kingdom,France,Italy,Japan,Other', 'United States', '', 1, NULL),
(14, 'Manufacturing Date', 'date', 0, 1, 1, 14, '', '', 'date', 1, NULL),
(15, 'Environmental Impact Statement', 'textarea', 0, 1, 1, 15, '', '', 'maxLength:1000', 1, NULL),
(16, 'Third-Party Certifications', 'multiselect', 0, 1, 1, 16, 'LEED,Energy Star,GREENGUARD,Cradle to Cradle,FSC,PEFC,SFI,Carbon Neutral,ISO 14001,Other', '', '', 1, NULL),
(17, 'Carbon Footprint (kg CO2e)', 'number', 0, 1, 1, 17, '', '', 'numeric,min:0', 1, NULL),
(18, 'Water Usage (liters)', 'number', 0, 1, 1, 18, '', '', 'numeric,min:0', 1, NULL),
(19, 'Energy Consumption (kWh)', 'number', 0, 1, 1, 19, '', '', 'numeric,min:0', 1, NULL),
(20, 'Recycled Content Percentage', 'number', 0, 1, 1, 20, '', '', 'numeric,min:0,max:100', 1, NULL),
(21, 'Test Report Number', 'text', 0, 1, 1, 21, '', '', 'maxLength:100', 1, NULL),
(22, 'Test Laboratory Name', 'text', 0, 1, 1, 22, '', '', 'maxLength:255', 1, NULL),
(23, 'Test Date', 'date', 0, 1, 1, 23, '', '', 'date', 1, NULL),
(24, 'Compliance Standards', 'multiselect', 0, 1, 1, 24, 'ISO 14024,ISO 14025,ISO 14040,ASTM Standards,ANSI Standards,UL Standards,Other', '', '', 1, NULL),
(25, 'Special Notes', 'textarea', 0, 1, 1, 25, '', '', 'maxLength:2000', 1, NULL),
(26, 'Internal Reference Number', 'text', 0, 1, 1, 26, '', '', 'maxLength:100', 1, NULL),
(27, 'Priority Level', 'select', 0, 1, 1, 27, 'Low,Normal,High,Urgent', 'Normal', '', 1, NULL),
(28, 'Certificate Visibility', 'select', 0, 1, 1, 28, 'Public,Partner Only,Internal Only,Confidential', 'Public', '', 1, NULL),
(29, 'Renewal Reminder Date', 'date', 0, 1, 1, 29, '', '', 'date', 1, NULL),
(30, 'Assigned Analyst', 'text', 0, 1, 1, 30, '', '', 'maxLength:255', 1, NULL);

SET IDENTITY_INSERT CustomFields OFF;
PRINT 'CustomFields synced: 30 records';
GO

-- PART 5: Sync PartnerApiKeys
-- =============================================
PRINT 'Syncing PartnerApiKeys...';

-- Insert Partner API Keys if they don't exist
SET IDENTITY_INSERT PartnerApiKeys ON;

IF NOT EXISTS (SELECT 1 FROM PartnerApiKeys WHERE Id = 1)
    INSERT INTO PartnerApiKeys (Id, PartnerId, ApiKey, Description, IsActive, CreatedAt, CreatedBy, ExpiresAt, LastUsedAt, RequestCount, RateLimitPerHour, AllowedEndpoints, Permissions, Notes, RevokedAt, RevokedBy)
    VALUES (1, 1, 'gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7', 'get cert Info', 1, '2025-11-13 05:40:55.499', 'admin@greencirclecertified.com', '2026-11-13 19:42:00.000', NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM PartnerApiKeys WHERE Id = 2)
    INSERT INTO PartnerApiKeys (Id, PartnerId, ApiKey, Description, IsActive, CreatedAt, CreatedBy, ExpiresAt, LastUsedAt, RequestCount, RateLimitPerHour, AllowedEndpoints, Permissions, Notes, RevokedAt, RevokedBy)
    VALUES (2, 1, 'gcc_partner_b451fb3b31c645e9dd900c3c7b2f6250fbb7480bbb4510d602efc5e52102fc4d', 'get cert Info 2', 1, '2025-11-13 05:54:36.825', 'admin@greencirclecertified.com', NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL);

IF NOT EXISTS (SELECT 1 FROM PartnerApiKeys WHERE Id = 3)
    INSERT INTO PartnerApiKeys (Id, PartnerId, ApiKey, Description, IsActive, CreatedAt, CreatedBy, ExpiresAt, LastUsedAt, RequestCount, RateLimitPerHour, AllowedEndpoints, Permissions, Notes, RevokedAt, RevokedBy)
    VALUES (3, 2, 'gcc_partner_8c147b7e4e1dfb11900883446f79c2ed861c0d72bd133114ff6e2aa87904c9d6', 'test', 1, '2025-11-13 05:55:14.763', 'admin@greencirclecertified.com', '2025-11-27 05:55:00.000', NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL);

SET IDENTITY_INSERT PartnerApiKeys OFF;
PRINT 'PartnerApiKeys synced: 3 records';
GO

-- PART 6: Verification Queries
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'VERIFICATION - Record Counts After Sync:';
PRINT '=============================================';

SELECT 'CertificationTypes' AS TableName, COUNT(*) AS RecordCount FROM CertificationTypes
UNION ALL
SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL
SELECT 'CustomFields', COUNT(*) FROM CustomFields
UNION ALL
SELECT 'PartnerApiKeys', COUNT(*) FROM PartnerApiKeys
UNION ALL
SELECT 'AuditHistory', COUNT(*) FROM AuditHistory
UNION ALL
SELECT '__EFMigrationsHistory', COUNT(*) FROM __EFMigrationsHistory;

PRINT '';
PRINT 'Database sync completed successfully!';
GO

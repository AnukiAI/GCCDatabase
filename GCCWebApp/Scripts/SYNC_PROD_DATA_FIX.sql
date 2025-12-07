-- =============================================
-- PRODUCTION DATABASE SYNC SCRIPT - PART 2
-- Fix CertificationTypes and Tags
-- Generated: 2025-12-04
-- =============================================

-- PART 1: Sync CertificationTypes (with required columns)
-- Columns: Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId
-- =============================================
PRINT 'Syncing CertificationTypes...';

SET IDENTITY_INSERT CertificationTypes ON;

-- Insert missing CertificationTypes (IDs 6-22) with all required columns
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 6)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (6, 'Certified Sustainability Facts', 'CSF', 'Sustainability certification', 1, 6, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 7)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (7, 'Certified Water Savings', 'CWS', 'Water savings certification', 1, 7, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 8)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (8, 'Closed Loop Product', 'CLP', 'Closed loop product certification', 1, 8, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 9)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (9, 'Dematerialization', 'DEM', 'Dematerialization certification', 1, 9, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 10)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (10, 'Greenhouse Gas (GHG) Verification', 'GHG', 'GHG verification certification', 1, 10, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 11)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (11, 'GreenCircle Audited', 'GCA', 'GreenCircle audit certification', 1, 11, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 12)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (12, 'Life Cycle Assessment (LCA) Optimized', 'LCA', 'LCA optimized certification', 1, 12, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 13)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (13, 'Manufacturer Inventory Verification', 'MIV', 'Manufacturer inventory verification', 1, 13, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 14)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (14, 'Recyclable Material', 'RCM', 'Recyclable material certification', 1, 14, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 15)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (15, 'Recycled Content', 'RCC', 'Recycled content certification', 1, 15, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 16)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (16, 'Renewable Resource Content', 'RRC', 'Renewable resource certification', 1, 16, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 17)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (17, 'Sustainable Energy Practices', 'SEP', 'Sustainable energy practices', 1, 17, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 18)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (18, 'Sustainable Manufacturing Practices', 'SMP', 'Sustainable manufacturing practices', 1, 18, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 19)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (19, 'Waste Diversion from Landfill', 'WDL', 'Waste diversion certification', 1, 19, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 20)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (20, 'Waste System Audit', 'WSA', 'Waste system audit certification', 1, 20, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 21)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (21, '+Vantage VinylT Verified', 'VTV', 'Vantage Vinyl verified', 1, 21, 1, NULL);

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Id = 22)
    INSERT INTO CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, [Level], ParentId)
    VALUES (22, 'Preloved', 'PRE', 'Preloved certification', 1, 22, 1, NULL);

SET IDENTITY_INSERT CertificationTypes OFF;
PRINT 'CertificationTypes sync complete';
GO

-- PART 2: Sync Tags (with required columns)
-- Columns: Id, Name, TagType, Description, IsActive
-- =============================================
PRINT 'Syncing Tags...';

SET IDENTITY_INSERT Tags ON;

-- Insert missing tags (IDs 11-26) with all required columns
IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 11)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (11, 'GREENGUARD', 'external', 'GREENGUARD certification tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 12)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (12, 'Bio-Based', 'external', 'Bio-based materials tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 13)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (13, 'Renewable Energy', 'external', 'Renewable energy tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 14)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (14, 'Circular Economy', 'external', 'Circular economy tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 15)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (15, 'Water Stewardship', 'external', 'Water stewardship tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 16)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (16, 'Fair Trade', 'external', 'Fair trade tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 17)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (17, 'Urgent Review', 'internal', 'Urgent review internal tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 18)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (18, 'Annual Audit', 'internal', 'Annual audit internal tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 19)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (19, 'Customer Request', 'internal', 'Customer request internal tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 20)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (20, 'Regulatory Compliance', 'external', 'Regulatory compliance tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 21)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (21, 'Market Expansion', 'internal', 'Market expansion internal tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 22)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (22, 'Partnership Program', 'internal', 'Partnership program tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 23)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (23, 'Innovation Project', 'internal', 'Innovation project tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 24)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (24, 'Cost Optimization', 'internal', 'Cost optimization tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 25)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (25, 'Quality Improvement', 'internal', 'Quality improvement tag', 1);

IF NOT EXISTS (SELECT 1 FROM Tags WHERE Id = 26)
    INSERT INTO Tags (Id, Name, TagType, Description, IsActive) VALUES (26, 'Digital Transformation', 'internal', 'Digital transformation tag', 1);

SET IDENTITY_INSERT Tags OFF;
PRINT 'Tags sync complete';
GO

-- PART 3: Verification
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
PRINT 'Final sync completed successfully!';
GO

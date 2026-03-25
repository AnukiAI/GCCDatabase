SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- ============================================================================
-- PRODUCTION DEPLOYMENT v2.0.0 — All data scripts (#18-#21)
-- Run as single batch to avoid connection throttling
-- Date: 2026-03-24
-- ============================================================================

PRINT '=== PRE-CHECK: Current State ===';
PRINT '';

PRINT 'Certificate Statuses:';
SELECT Status, COUNT(*) AS Cnt FROM Certificates GROUP BY Status ORDER BY Status;

PRINT 'Permission Descriptions (before):';
SELECT Name, Description FROM Permissions WHERE Name IN ('Manage Workflow','View Audit Logs','Manage Tags') ORDER BY Name;

PRINT 'Product Categories Count:';
SELECT COUNT(*) AS ProductCatCount FROM ProductCategories;

PRINT '';
PRINT '============================================================================';
PRINT 'SCRIPT #18: Update Status Types (14-state -> 8 client-defined)';
PRINT '============================================================================';

-- Only run if old statuses still exist
IF EXISTS (SELECT 1 FROM Certificates WHERE Status IN ('Active','active','Renewed','renewal_in_progress','expiring_soon','Approved','PendingReview','UnderReview','PendingRenewal','Suspended','OnHold','Revoked','Cancelled','Archived','Rejected'))
BEGIN
    BEGIN TRANSACTION;

    UPDATE Certificates SET Status = 'Client Approved - Paid'
    WHERE Status IN ('Active', 'active', 'Renewed', 'renewal_in_progress', 'expiring_soon');
    PRINT '  Updated Active/Renewed -> Client Approved - Paid: ' + CAST(@@ROWCOUNT AS VARCHAR);

    UPDATE Certificates SET Status = 'Client Approved - Not Paid'
    WHERE Status IN ('Approved');
    PRINT '  Updated Approved -> Client Approved - Not Paid: ' + CAST(@@ROWCOUNT AS VARCHAR);

    UPDATE Certificates SET Status = 'Draft'
    WHERE Status IN ('PendingReview', 'UnderReview', 'PendingRenewal', 'Rejected');
    PRINT '  Updated PendingReview/UnderReview/Rejected -> Draft: ' + CAST(@@ROWCOUNT AS VARCHAR);

    UPDATE Certificates SET Status = 'Hold'
    WHERE Status IN ('Suspended', 'OnHold');
    PRINT '  Updated Suspended/OnHold -> Hold: ' + CAST(@@ROWCOUNT AS VARCHAR);

    UPDATE Certificates SET Status = 'Expired'
    WHERE Status IN ('Expired', 'expired', 'Revoked');
    PRINT '  Updated Expired/Revoked -> Expired: ' + CAST(@@ROWCOUNT AS VARCHAR);

    UPDATE Certificates SET Status = 'Expired - No Annual Verification'
    WHERE Status IN ('Cancelled', 'Archived');
    PRINT '  Updated Cancelled/Archived -> Expired - No Annual Verification: ' + CAST(@@ROWCOUNT AS VARCHAR);

    COMMIT;
    PRINT '#18 APPLIED';
END
ELSE
    PRINT '#18 SKIPPED (no old statuses found)';

PRINT '';
SELECT Status, COUNT(*) AS Cnt FROM Certificates GROUP BY Status ORDER BY Status;

PRINT '';
PRINT '============================================================================';
PRINT 'SCRIPT #19: Update Certification Types';
PRINT '============================================================================';

-- Add missing parent types
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Carbon Footprint Reduction' AND (ParentId IS NULL OR Level = 1))
BEGIN
    INSERT INTO CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES ('Carbon Footprint Reduction', 'CFR', 'Carbon footprint reduction certification', 1, 10, 1, NULL);
    PRINT '  + Added: Carbon Footprint Reduction';
END
ELSE PRINT '  o Carbon Footprint Reduction exists';

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Sustainable Energy Practices' AND (ParentId IS NULL OR Level = 1))
BEGIN
    INSERT INTO CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES ('Sustainable Energy Practices', 'SEP', 'Sustainable energy practices certification', 1, 11, 1, NULL);
    PRINT '  + Added: Sustainable Energy Practices';
END
ELSE PRINT '  o Sustainable Energy Practices exists';

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Sustainable Manufacturing Practices' AND (ParentId IS NULL OR Level = 1))
BEGIN
    INSERT INTO CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES ('Sustainable Manufacturing Practices', 'SMP', 'Sustainable manufacturing practices certification', 1, 12, 1, NULL);
    PRINT '  + Added: Sustainable Manufacturing Practices';
END
ELSE PRINT '  o Sustainable Manufacturing Practices exists';

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Product Optimization: Carbon Footprint Reduction' AND (ParentId IS NULL OR Level = 1))
BEGIN
    INSERT INTO CertificationTypes (Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId)
    VALUES ('Product Optimization: Carbon Footprint Reduction', 'POCFR', 'Product optimization carbon footprint reduction certification', 1, 13, 1, NULL);
    PRINT '  + Added: Product Optimization: Carbon Footprint Reduction';
END
ELSE PRINT '  o Product Optimization: Carbon Footprint Reduction exists';

-- Deactivate removed types
IF EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Renewable Resource Content' AND IsActive = 1)
BEGIN
    UPDATE CertificationTypes SET IsActive = 0 WHERE Name = 'Renewable Resource Content';
    PRINT '  - Deactivated: Renewable Resource Content';
END

IF EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Biobased' AND IsActive = 1 AND (ParentId IS NULL OR Level = 1))
BEGIN
    UPDATE CertificationTypes SET IsActive = 0 WHERE Name = 'Biobased' AND (ParentId IS NULL OR Level = 1);
    PRINT '  - Deactivated: Biobased (parent only)';
END

-- Rename
IF EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Manufacturer Inventory Verification')
BEGIN
    IF EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Material Ingredient Reporting' AND (ParentId IS NULL OR Level = 1))
        UPDATE CertificationTypes SET IsActive = 0 WHERE Name = 'Manufacturer Inventory Verification';
    ELSE
        UPDATE CertificationTypes SET Name = 'Material Ingredient Reporting' WHERE Name = 'Manufacturer Inventory Verification';
    PRINT '  ~ Renamed: Manufacturer Inventory Verification -> Material Ingredient Reporting';
END

-- Add CSF subcategories
DECLARE @csfId INT = (SELECT TOP 1 Id FROM CertificationTypes WHERE Name = 'Certified Sustainability Facts' AND (ParentId IS NULL OR Level = 1));
IF @csfId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Waste Diversion Event' AND ParentId = @csfId)
        INSERT INTO CertificationTypes (Name, ParentId, Level, IsActive, SortOrder) VALUES ('Waste Diversion Event', @csfId, 2, 1, 100);
    IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'ISO 14001 Certified' AND ParentId = @csfId)
        INSERT INTO CertificationTypes (Name, ParentId, Level, IsActive, SortOrder) VALUES ('ISO 14001 Certified', @csfId, 2, 1, 100);
    IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Verified Declare Label' AND ParentId = @csfId)
        INSERT INTO CertificationTypes (Name, ParentId, Level, IsActive, SortOrder) VALUES ('Verified Declare Label', @csfId, 2, 1, 100);
    IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Verified HPD' AND ParentId = @csfId)
        INSERT INTO CertificationTypes (Name, ParentId, Level, IsActive, SortOrder) VALUES ('Verified HPD', @csfId, 2, 1, 100);

    -- Re-sort alphabetically
    ;WITH Numbered AS (
        SELECT Id, Name, ROW_NUMBER() OVER (ORDER BY Name) AS NewSortOrder
        FROM CertificationTypes WHERE ParentId = @csfId AND IsActive = 1
    )
    UPDATE ct SET ct.SortOrder = n.NewSortOrder
    FROM CertificationTypes ct INNER JOIN Numbered n ON ct.Id = n.Id;

    PRINT '  CSF subcategories updated';
END
ELSE PRINT '  WARNING: CSF parent not found';

PRINT '#19 APPLIED';

PRINT '';
PRINT '============================================================================';
PRINT 'SCRIPT #20: Update Partner Descriptions + Wayfair';
PRINT '============================================================================';

UPDATE Partners SET Description = 'Novi Connect is an AI shopping optimization platform', UpdatedAt = GETDATE()
WHERE Name = 'Novi';
PRINT '  Updated Novi description: ' + CAST(@@ROWCOUNT AS VARCHAR);

IF NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Wayfair')
BEGIN
    UPDATE Partners SET SortOrder = SortOrder + 1, UpdatedAt = GETDATE() WHERE Name = 'Other';
    INSERT INTO Partners (Name, Description, Website, Email, ContactInfo, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES ('Wayfair', 'Online furniture and home goods retailer focused on sustainable product offerings', 'https://www.wayfair.com', 'partner@wayfair.com', 'Wayfair Sustainable Products', 1, 0, 3, GETDATE(), GETDATE());
    PRINT '  + Inserted Wayfair';
END
ELSE
BEGIN
    -- Wayfair exists but may need description
    UPDATE Partners SET Description = 'Online furniture and home goods retailer focused on sustainable product offerings', UpdatedAt = GETDATE()
    WHERE Name = 'Wayfair' AND (Description IS NULL OR Description = '');
    PRINT '  o Wayfair exists, updated description if empty: ' + CAST(@@ROWCOUNT AS VARCHAR);
END

PRINT '#20 APPLIED';

PRINT '';
PRINT '============================================================================';
PRINT 'SCRIPT #21: Update Permission Descriptions (QC #105)';
PRINT '============================================================================';

UPDATE Permissions SET Description = 'Manage certificate workflow state transitions' WHERE Name = 'Manage Workflow';
PRINT '  Updated Manage Workflow: ' + CAST(@@ROWCOUNT AS VARCHAR);

UPDATE Permissions SET Description = 'System activity history' WHERE Name = 'View Audit Logs';
PRINT '  Updated View Audit Logs: ' + CAST(@@ROWCOUNT AS VARCHAR);

UPDATE Permissions SET Description = 'Classification tags (internal and external)' WHERE Name = 'Manage Tags';
PRINT '  Updated Manage Tags: ' + CAST(@@ROWCOUNT AS VARCHAR);

PRINT '#21 APPLIED';

PRINT '';
PRINT '============================================================================';
PRINT 'POST-CHECK: Final State';
PRINT '============================================================================';

PRINT 'Certificate Statuses:';
SELECT Status, COUNT(*) AS Cnt FROM Certificates GROUP BY Status ORDER BY Status;

PRINT 'Partners:';
SELECT Name, LEFT(Description, 60) AS Description FROM Partners ORDER BY SortOrder;

PRINT 'Updated Permissions:';
SELECT Name, Description FROM Permissions WHERE Name IN ('Manage Workflow','View Audit Logs','Manage Tags') ORDER BY Name;

PRINT '';
PRINT '============================================================================';
PRINT 'ALL SCRIPTS COMPLETE';
PRINT '============================================================================';

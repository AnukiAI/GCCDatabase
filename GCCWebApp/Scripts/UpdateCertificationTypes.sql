-- QC #45: Fix missing/incorrect certification types
-- Run against production database
-- Date: 2026-04-13

BEGIN TRANSACTION;

-- 1. Add missing parent certification types (only if they don't exist)
IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Carbon Footprint Reduction' AND ParentId IS NULL)
BEGIN
    INSERT INTO CertificationTypes (Name, [Level], IsActive, ParentId)
    VALUES ('Carbon Footprint Reduction', 1, 1, NULL);
    PRINT 'Added: Carbon Footprint Reduction';
END

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Sustainable Energy Practices' AND ParentId IS NULL)
BEGIN
    INSERT INTO CertificationTypes (Name, [Level], IsActive, ParentId)
    VALUES ('Sustainable Energy Practices', 1, 1, NULL);
    PRINT 'Added: Sustainable Energy Practices';
END

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Sustainable Manufacturing Practices' AND ParentId IS NULL)
BEGIN
    INSERT INTO CertificationTypes (Name, [Level], IsActive, ParentId)
    VALUES ('Sustainable Manufacturing Practices', 1, 1, NULL);
    PRINT 'Added: Sustainable Manufacturing Practices';
END

IF NOT EXISTS (SELECT 1 FROM CertificationTypes WHERE Name = 'Product Optimization: Carbon Footprint Reduction' AND ParentId IS NULL)
BEGIN
    INSERT INTO CertificationTypes (Name, [Level], IsActive, ParentId)
    VALUES ('Product Optimization: Carbon Footprint Reduction', 1, 1, NULL);
    PRINT 'Added: Product Optimization: Carbon Footprint Reduction';
END

-- 2. Deactivate unwanted certification types
UPDATE CertificationTypes SET IsActive = 0 WHERE Name = 'Renewable Resource Content' AND ParentId IS NULL;
PRINT 'Deactivated: Renewable Resource Content';

UPDATE CertificationTypes SET IsActive = 0 WHERE Name = 'Biobased' AND ParentId IS NULL;
PRINT 'Deactivated: Biobased';

-- 3. Rename Manufacturer Inventory Verification to Material Ingredient Reporting
UPDATE CertificationTypes SET Name = 'Material Ingredient Reporting' WHERE Name = 'Manufacturer Inventory Verification' AND ParentId IS NULL;
PRINT 'Renamed: Manufacturer Inventory Verification -> Material Ingredient Reporting';

COMMIT TRANSACTION;

-- Verify results
SELECT Id, Name, [Level], IsActive, ParentId
FROM CertificationTypes
WHERE ParentId IS NULL
ORDER BY Name;

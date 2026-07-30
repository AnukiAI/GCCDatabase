-- ============================================================================
-- Migration: Update Partner Descriptions + Add Wayfair
-- Date: 2026-03-24
-- QC #110: Change Novi description, add Wayfair as new partner
-- ============================================================================

PRINT '============================================================================';
PRINT 'MIGRATION: Update Partner Descriptions + Add Wayfair';
PRINT '============================================================================';

-- Step 1: Update Novi description
PRINT '';
PRINT 'Step 1: Updating Novi description...';

UPDATE dbo.Partners
SET
    Description = 'Novi Connect is an AI shopping optimization platform',
    UpdatedAt = GETDATE()
WHERE Name = 'Novi';

IF @@ROWCOUNT > 0
    PRINT '  ✓ Updated Novi description';
ELSE
    PRINT '  - No Novi partner found to update';

-- Step 2: Add Wayfair partner
PRINT '';
PRINT 'Step 2: Adding Wayfair partner...';

IF NOT EXISTS (SELECT 1 FROM dbo.Partners WHERE Name = 'Wayfair')
BEGIN
    -- Shift Other's SortOrder to make room for Wayfair
    UPDATE dbo.Partners
    SET SortOrder = SortOrder + 1, UpdatedAt = GETDATE()
    WHERE Name = 'Other';

    INSERT INTO dbo.Partners (Name, Description, Website, Email, ContactInfo, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (
        'Wayfair',
        'Online furniture and home goods retailer focused on sustainable product offerings',
        'https://www.wayfair.com',
        'partner@wayfair.com',
        'Wayfair Sustainable Products',
        1, -- IsActive
        0, -- IsDeleted
        3, -- SortOrder (after Novi=2, before Other=4)
        GETDATE(),
        GETDATE()
    );
    PRINT '  ✓ Inserted Partner: Wayfair';
END
ELSE
    PRINT '  - Wayfair partner already exists';

-- Verification
PRINT '';
PRINT '============================================================================';
PRINT 'VERIFICATION';
PRINT '============================================================================';
SELECT Id, Name, Description, SortOrder FROM dbo.Partners ORDER BY SortOrder;

PRINT '';
PRINT '============================================================================';
PRINT 'MIGRATION COMPLETE';
PRINT '============================================================================';

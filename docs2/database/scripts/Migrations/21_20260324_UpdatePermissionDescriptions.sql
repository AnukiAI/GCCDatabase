-- ============================================================================
-- Migration: Update Permission Descriptions
-- Date: 2026-03-24
-- QC #105: Fix permission descriptions for Manage Workflow, View Audit Logs,
--          and Manage Tags
-- ============================================================================

PRINT '============================================================================';
PRINT 'MIGRATION: Update Permission Descriptions (QC #105)';
PRINT '============================================================================';

-- Step 1: Update Manage Workflow description
PRINT '';
PRINT 'Step 1: Updating Manage Workflow description...';

UPDATE dbo.Permissions
SET Description = 'Manage certificate workflow state transitions'
WHERE Name = 'Manage Workflow';

IF @@ROWCOUNT > 0
    PRINT '  Updated: Manage Workflow';
ELSE
    PRINT '  - Manage Workflow permission not found';

-- Step 2: Update View Audit Logs description
PRINT '';
PRINT 'Step 2: Updating View Audit Logs description...';

UPDATE dbo.Permissions
SET Description = 'System activity history'
WHERE Name = 'View Audit Logs';

IF @@ROWCOUNT > 0
    PRINT '  Updated: View Audit Logs';
ELSE
    PRINT '  - View Audit Logs permission not found';

-- Step 3: Update Manage Tags description
PRINT '';
PRINT 'Step 3: Updating Manage Tags description...';

UPDATE dbo.Permissions
SET Description = 'Classification tags (internal and external)'
WHERE Name = 'Manage Tags';

IF @@ROWCOUNT > 0
    PRINT '  Updated: Manage Tags';
ELSE
    PRINT '  - Manage Tags permission not found';

-- Verification
PRINT '';
PRINT '============================================================================';
PRINT 'VERIFICATION';
PRINT '============================================================================';
SELECT Id, Name, Description FROM dbo.Permissions ORDER BY Name;

PRINT '';
PRINT '============================================================================';
PRINT 'MIGRATION COMPLETE';
PRINT '============================================================================';

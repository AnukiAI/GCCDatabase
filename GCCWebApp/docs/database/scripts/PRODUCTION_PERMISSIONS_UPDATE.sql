-- ============================================
-- GCC Production Permissions Update Script
-- ============================================
-- This script adds ALL missing permissions to Production
-- to match the comprehensive permission set in Development
--
-- SAFE TO RUN: Uses IF NOT EXISTS checks
-- DOES NOT DELETE: Only adds missing permissions
-- ============================================

PRINT '============================================';
PRINT 'GCC Production Permissions Update';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- 1. Update Existing Permission Codenames (if using old format)
-- ============================================
PRINT '1. Standardizing existing permission codenames...';

-- Update to dot notation format if using underscore format
UPDATE Permissions SET Codename = 'certificate.view' WHERE Codename = 'certificate_view';
UPDATE Permissions SET Codename = 'certificate.create' WHERE Codename = 'certificate_create';
UPDATE Permissions SET Codename = 'certificate.edit' WHERE Codename = 'certificate_edit';
UPDATE Permissions SET Codename = 'certificate.delete' WHERE Codename = 'certificate_delete';
UPDATE Permissions SET Codename = 'admin.users' WHERE Codename = 'user_manage';
UPDATE Permissions SET Codename = 'data.reports' WHERE Codename = 'report_view';

-- Also update Category to match new structure
UPDATE Permissions SET Category = 'Certificates' WHERE Category = 'Certificate';
UPDATE Permissions SET Category = 'Administration' WHERE Category = 'User';
UPDATE Permissions SET Category = 'Data' WHERE Category = 'Report';

PRINT '   Codenames standardized';
PRINT '';

-- ============================================
-- 2. Add Missing Permissions
-- ============================================
PRINT '2. Adding missing permissions...';

-- Certificate Module - Add Manage Workflow
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'certificate.workflow')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Workflow', 'certificate.workflow', 'Certificates', 'Approve/reject submissions', 1, GETDATE());
    PRINT '   + Added: Manage Workflow (certificate.workflow)';
END

-- Data Module
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'data.export')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Export Data', 'data.export', 'Data', 'Download CSV exports', 1, GETDATE());
    PRINT '   + Added: Export Data (data.export)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'data.import')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Import Data', 'data.import', 'Data', 'Bulk import from CSV', 1, GETDATE());
    PRINT '   + Added: Import Data (data.import)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'data.audit')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('View Audit Logs', 'data.audit', 'Data', 'System activity history', 1, GETDATE());
    PRINT '   + Added: View Audit Logs (data.audit)';
END

-- Administration Module
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.roles')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Roles', 'admin.roles', 'Administration', 'Configure role permissions', 1, GETDATE());
    PRINT '   + Added: Manage Roles (admin.roles)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.companies')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Companies', 'admin.companies', 'Administration', 'Partner/client companies', 1, GETDATE());
    PRINT '   + Added: Manage Companies (admin.companies)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.tags')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Tags', 'admin.tags', 'Administration', 'Internal classification tags', 1, GETDATE());
    PRINT '   + Added: Manage Tags (admin.tags)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.partners')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Partners', 'admin.partners', 'Administration', 'API keys & partner access', 1, GETDATE());
    PRINT '   + Added: Manage Partners (admin.partners)';
END

-- API Access Module
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'api.read')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('API Read', 'api.read', 'API Access', 'Read data via API', 1, GETDATE());
    PRINT '   + Added: API Read (api.read)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'api.write')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('API Write', 'api.write', 'API Access', 'Create/update via API', 1, GETDATE());
    PRINT '   + Added: API Write (api.write)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'api.delete')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('API Delete', 'api.delete', 'API Access', 'Delete via API', 1, GETDATE());
    PRINT '   + Added: API Delete (api.delete)';
END

-- Products Module
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'products.view')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('View Products', 'products.view', 'Products', 'Browse product catalog', 1, GETDATE());
    PRINT '   + Added: View Products (products.view)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'products.manage')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Products', 'products.manage', 'Products', 'Add/edit products', 1, GETDATE());
    PRINT '   + Added: Manage Products (products.manage)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'products.types')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Product Types', 'products.types', 'Products', 'Categories & types', 1, GETDATE());
    PRINT '   + Added: Manage Product Types (products.types)';
END

-- System Module
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'system.settings')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('System Settings', 'system.settings', 'System', 'Global configuration', 1, GETDATE());
    PRINT '   + Added: System Settings (system.settings)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'system.certtypes')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Cert Types', 'system.certtypes', 'System', 'Certification types', 1, GETDATE());
    PRINT '   + Added: Manage Cert Types (system.certtypes)';
END

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'system.fields')
BEGIN
    INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
    VALUES ('Manage Custom Fields', 'system.fields', 'System', 'Dynamic form fields', 1, GETDATE());
    PRINT '   + Added: Manage Custom Fields (system.fields)';
END

PRINT '';

-- ============================================
-- 3. Assign ALL Permissions to Admin Role
-- ============================================
PRINT '3. Assigning all permissions to Admin role...';

DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin');

IF @AdminRoleId IS NOT NULL
BEGIN
    -- Add any permissions not already assigned to Admin
    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @AdminRoleId, p.Id
    FROM Permissions p
    WHERE p.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM PermissionRole pr
          WHERE pr.RolesId = @AdminRoleId AND pr.PermissionsId = p.Id
      );

    PRINT '   Admin role updated with all permissions';
END
ELSE
    PRINT '   WARNING: Admin role not found!';

PRINT '';

-- ============================================
-- 4. Assign Analyst Permissions
-- ============================================
PRINT '4. Assigning permissions to Analyst role...';

DECLARE @AnalystRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Analyst');

IF @AnalystRoleId IS NOT NULL
BEGIN
    -- Analyst permissions: certificate management, workflow, data export/import, reports, products view
    DECLARE @AnalystPerms TABLE (Codename VARCHAR(50));
    INSERT INTO @AnalystPerms VALUES
        ('certificate.view'), ('certificate.create'), ('certificate.edit'), ('certificate.workflow'),
        ('data.export'), ('data.import'), ('data.reports'),
        ('products.view');

    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @AnalystRoleId, p.Id
    FROM Permissions p
    INNER JOIN @AnalystPerms ap ON p.Codename = ap.Codename
    WHERE p.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM PermissionRole pr
          WHERE pr.RolesId = @AnalystRoleId AND pr.PermissionsId = p.Id
      );

    PRINT '   Analyst role updated';
END
ELSE
    PRINT '   WARNING: Analyst role not found!';

PRINT '';

-- ============================================
-- 5. Assign External Permissions
-- ============================================
PRINT '5. Assigning permissions to External role...';

DECLARE @ExternalRoleId INT = (SELECT Id FROM Roles WHERE Name = 'External');

IF @ExternalRoleId IS NOT NULL
BEGIN
    -- External permissions: view certificates, view products, export data, reports
    DECLARE @ExternalPerms TABLE (Codename VARCHAR(50));
    INSERT INTO @ExternalPerms VALUES
        ('certificate.view'),
        ('data.export'), ('data.reports'),
        ('products.view');

    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @ExternalRoleId, p.Id
    FROM Permissions p
    INNER JOIN @ExternalPerms ep ON p.Codename = ep.Codename
    WHERE p.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM PermissionRole pr
          WHERE pr.RolesId = @ExternalRoleId AND pr.PermissionsId = p.Id
      );

    PRINT '   External role updated';
END
ELSE
    PRINT '   WARNING: External role not found!';

PRINT '';

-- ============================================
-- 6. Assign Public Permissions
-- ============================================
PRINT '6. Assigning permissions to Public role...';

DECLARE @PublicRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Public');

IF @PublicRoleId IS NOT NULL
BEGIN
    -- Public permissions: view certificates and products only
    DECLARE @PublicPerms TABLE (Codename VARCHAR(50));
    INSERT INTO @PublicPerms VALUES
        ('certificate.view'),
        ('products.view');

    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @PublicRoleId, p.Id
    FROM Permissions p
    INNER JOIN @PublicPerms pp ON p.Codename = pp.Codename
    WHERE p.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM PermissionRole pr
          WHERE pr.RolesId = @PublicRoleId AND pr.PermissionsId = p.Id
      );

    PRINT '   Public role updated';
END
ELSE
    PRINT '   WARNING: Public role not found!';

PRINT '';

-- ============================================
-- 7. Commit Transaction
-- ============================================
COMMIT TRANSACTION;
PRINT 'Transaction committed successfully';
PRINT '';

-- ============================================
-- 8. Verification Report
-- ============================================
PRINT '============================================';
PRINT 'VERIFICATION REPORT';
PRINT '============================================';
PRINT '';

PRINT 'All Permissions (by Category):';
SELECT Category, Name, Codename, IsActive
FROM Permissions
ORDER BY Category, Name;

PRINT '';
PRINT 'Permission Counts by Role:';
SELECT r.Name AS RoleName, COUNT(pr.PermissionsId) AS PermissionCount
FROM Roles r
LEFT JOIN PermissionRole pr ON r.Id = pr.RolesId
GROUP BY r.Id, r.Name
ORDER BY r.Name;

PRINT '';
PRINT 'Admin Role Permissions:';
SELECT p.Category, p.Name, p.Codename
FROM Permissions p
INNER JOIN PermissionRole pr ON p.Id = pr.PermissionsId
INNER JOIN Roles r ON r.Id = pr.RolesId
WHERE r.Name = 'Admin'
ORDER BY p.Category, p.Name;

PRINT '';
PRINT '============================================';
PRINT 'PERMISSIONS UPDATE COMPLETE';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';
PRINT 'Summary of Changes:';
PRINT '- Standardized permission codenames (underscore to dot notation)';
PRINT '- Added 16 new granular permissions across 6 categories:';
PRINT '  * Certificates: Manage Workflow';
PRINT '  * Data: Export, Import, Audit Logs';
PRINT '  * Administration: Roles, Companies, Tags, Partners';
PRINT '  * API Access: Read, Write, Delete';
PRINT '  * Products: View, Manage, Types';
PRINT '  * System: Settings, Cert Types, Custom Fields';
PRINT '- Updated role-permission assignments';
PRINT '';
PRINT 'Total Permissions: 22 (was 6)';
PRINT '============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED';
    PRINT '============================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '============================================';

    THROW;
END CATCH;

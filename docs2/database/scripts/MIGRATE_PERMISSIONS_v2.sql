-- =====================================================
-- PERMISSION MIGRATION SCRIPT v2.0
-- Migrates from 6 basic permissions to 24 comprehensive permissions
-- Run this script to update an existing database
-- =====================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting Permission Migration v2.0...';
    PRINT '======================================';

    -- Step 1: Backup existing role-permission mappings
    PRINT 'Step 1: Creating backup of existing role-permission mappings...';

    IF OBJECT_ID('tempdb..#PermissionRoleBackup') IS NOT NULL DROP TABLE #PermissionRoleBackup;
    SELECT rp.* INTO #PermissionRoleBackup FROM PermissionRole rp;
    PRINT '  - Backed up ' + CAST(@@ROWCOUNT AS VARCHAR) + ' role-permission mappings';

    -- Step 2: Clear existing role-permission mappings
    PRINT 'Step 2: Clearing existing role-permission mappings...';
    DELETE FROM PermissionRole;
    PRINT '  - Cleared existing mappings';

    -- Step 3: Update existing permissions with new codenames (preserve IDs where possible)
    PRINT 'Step 3: Updating existing permissions...';

    -- Update View Certificates
    UPDATE Permissions SET Codename = 'certificate.view', Category = 'Certificates', Description = 'Browse certificate list'
    WHERE Codename = 'certificate_view' OR Name = 'View Certificates';

    -- Update Create Certificates
    UPDATE Permissions SET Codename = 'certificate.create', Category = 'Certificates', Description = 'Add new certificates'
    WHERE Codename = 'certificate_create' OR Name = 'Create Certificates';

    -- Update Edit Certificates
    UPDATE Permissions SET Codename = 'certificate.edit', Category = 'Certificates', Description = 'Modify existing certificates'
    WHERE Codename = 'certificate_edit' OR Name = 'Edit Certificates';

    -- Update Delete Certificates
    UPDATE Permissions SET Codename = 'certificate.delete', Category = 'Certificates', Description = 'Remove certificates'
    WHERE Codename = 'certificate_delete' OR Name = 'Delete Certificates';

    -- Update Manage Users
    UPDATE Permissions SET Codename = 'admin.users', Category = 'Administration', Description = 'Create/edit user accounts'
    WHERE Codename = 'user_manage' OR Name = 'Manage Users';

    -- Update View Reports (move to Data category)
    UPDATE Permissions SET Codename = 'data.reports', Category = 'Data', Description = 'Access analytics reports'
    WHERE Codename = 'report_view' OR Name = 'View Reports';

    PRINT '  - Updated existing permissions';

    -- Step 4: Insert new permissions
    PRINT 'Step 4: Inserting new permissions...';

    -- Certificate Module - Add Workflow permission
    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'certificate.workflow')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Workflow', 'certificate.workflow', 'Certificates', 'Approve/reject submissions', 1, GETUTCDATE());

    -- Data Module
    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'data.export')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Export Data', 'data.export', 'Data', 'Download CSV exports', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'data.import')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Import Data', 'data.import', 'Data', 'Bulk import from CSV', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'data.audit')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('View Audit Logs', 'data.audit', 'Data', 'System activity history', 1, GETUTCDATE());

    -- Administration Module
    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.roles')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Roles', 'admin.roles', 'Administration', 'Configure role permissions', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.companies')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Companies', 'admin.companies', 'Administration', 'Partner/client companies', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.tags')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Tags', 'admin.tags', 'Administration', 'Internal classification tags', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'admin.partners')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Partners', 'admin.partners', 'Administration', 'API keys & partner access', 1, GETUTCDATE());

    -- API Access Module
    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'api.read')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('API Read', 'api.read', 'API Access', 'Read data via API', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'api.write')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('API Write', 'api.write', 'API Access', 'Create/update via API', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'api.delete')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('API Delete', 'api.delete', 'API Access', 'Delete via API', 1, GETUTCDATE());

    -- Products Module
    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'products.view')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('View Products', 'products.view', 'Products', 'Browse product catalog', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'products.manage')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Products', 'products.manage', 'Products', 'Add/edit products', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'products.types')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Product Types', 'products.types', 'Products', 'Categories & types', 1, GETUTCDATE());

    -- System Module
    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'system.settings')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('System Settings', 'system.settings', 'System', 'Global configuration', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'system.certtypes')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Cert Types', 'system.certtypes', 'System', 'Certification types', 1, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Codename = 'system.fields')
        INSERT INTO Permissions (Name, Codename, Category, Description, IsActive, CreatedAt)
        VALUES ('Manage Custom Fields', 'system.fields', 'System', 'Dynamic form fields', 1, GETUTCDATE());

    PRINT '  - Inserted new permissions';

    -- Step 5: Reassign permissions to roles
    PRINT 'Step 5: Reassigning permissions to roles...';

    -- Get role IDs
    DECLARE @AdminRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Admin');
    DECLARE @AnalystRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Analyst');
    DECLARE @ExternalRoleId INT = (SELECT Id FROM Roles WHERE Name = 'External');
    DECLARE @PublicRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Public');

    -- Admin gets ALL permissions
    IF @AdminRoleId IS NOT NULL
    BEGIN
        INSERT INTO PermissionRole (RolesId, PermissionsId)
        SELECT @AdminRoleId, Id FROM Permissions WHERE IsActive = 1;
        PRINT '  - Admin role: All permissions assigned';
    END

    -- Analyst gets certificate management, data, and products view
    IF @AnalystRoleId IS NOT NULL
    BEGIN
        INSERT INTO PermissionRole (RolesId, PermissionsId)
        SELECT @AnalystRoleId, Id FROM Permissions
        WHERE Codename IN (
            'certificate.view', 'certificate.create', 'certificate.edit', 'certificate.workflow',
            'data.export', 'data.import', 'data.reports',
            'products.view'
        );
        PRINT '  - Analyst role: Certificate, data, and products permissions assigned';
    END

    -- External gets view certificates, products, export, reports
    IF @ExternalRoleId IS NOT NULL
    BEGIN
        INSERT INTO PermissionRole (RolesId, PermissionsId)
        SELECT @ExternalRoleId, Id FROM Permissions
        WHERE Codename IN (
            'certificate.view',
            'data.export', 'data.reports',
            'products.view'
        );
        PRINT '  - External role: View and export permissions assigned';
    END

    -- Public gets only view certificates and products
    IF @PublicRoleId IS NOT NULL
    BEGIN
        INSERT INTO PermissionRole (RolesId, PermissionsId)
        SELECT @PublicRoleId, Id FROM Permissions
        WHERE Codename IN ('certificate.view', 'products.view');
        PRINT '  - Public role: View-only permissions assigned';
    END

    -- Step 6: Summary
    PRINT '';
    PRINT '======================================';
    PRINT 'Migration Summary:';
    PRINT '======================================';

    SELECT
        'Total Permissions' AS Metric,
        CAST(COUNT(*) AS VARCHAR) AS Value
    FROM Permissions WHERE IsActive = 1
    UNION ALL
    SELECT
        'Total Role-Permission Mappings',
        CAST(COUNT(*) AS VARCHAR)
    FROM PermissionRole;

    PRINT '';
    PRINT 'Permissions by Category:';
    SELECT Category, COUNT(*) AS PermissionCount
    FROM Permissions
    WHERE IsActive = 1
    GROUP BY Category
    ORDER BY Category;

    COMMIT TRANSACTION;
    PRINT '';
    PRINT 'Migration completed successfully!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: Migration failed!';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    THROW;
END CATCH

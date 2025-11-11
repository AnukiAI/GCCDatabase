USE gccwebap_certificates;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Permissions (with all required columns)
    DELETE FROM Permissions;
    DBCC CHECKIDENT ('Permissions', RESEED, 0);
    SET IDENTITY_INSERT Permissions ON;
INSERT INTO Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt) VALUES (1, 'View Certificates', 'certificate_view', 'Certificate', 'Can view certificates', 1, GETDATE());
INSERT INTO Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt) VALUES (2, 'Create Certificates', 'certificate_create', 'Certificate', 'Can create new certificates', 1, GETDATE());
INSERT INTO Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt) VALUES (3, 'Edit Certificates', 'certificate_edit', 'Certificate', 'Can edit existing certificates', 1, GETDATE());
INSERT INTO Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt) VALUES (4, 'Delete Certificates', 'certificate_delete', 'Certificate', 'Can delete certificates', 1, GETDATE());
INSERT INTO Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt) VALUES (5, 'Manage Users', 'user_manage', 'User', 'Can manage user accounts', 1, GETDATE());
INSERT INTO Permissions (Id, Name, Codename, Category, Description, IsActive, CreatedAt) VALUES (6, 'View Reports', 'report_view', 'Report', 'Can view system reports', 1, GETDATE());
    SET IDENTITY_INSERT Permissions OFF;

    -- PermissionRole
    DELETE FROM PermissionRole;
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 2);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 3);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (1, 4);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (2, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (2, 2);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (3, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (3, 2);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (4, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (5, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (6, 1);
INSERT INTO PermissionRole (PermissionsId, RolesId) VALUES (6, 3);

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: Permissions and PermissionRole synchronized';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

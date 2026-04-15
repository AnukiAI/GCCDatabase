-- Add Partner role to existing database
-- Run this on any environment where the role doesn't already exist

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Partner')
BEGIN
    INSERT INTO Roles (Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt)
    VALUES ('Partner', 'Partner organization access - scoped to assigned partner data', 1, 1, GETUTCDATE(), GETUTCDATE());

    DECLARE @partnerRoleId INT = SCOPE_IDENTITY();

    -- Assign same permissions as External role (certificate.view, data.export, data.reports, products.view)
    INSERT INTO PermissionRole (RolesId, PermissionsId)
    SELECT @partnerRoleId, p.Id
    FROM Permissions p
    WHERE p.Codename IN ('certificate.view', 'data.export', 'data.reports', 'products.view');

    PRINT 'Partner role created with ID: ' + CAST(@partnerRoleId AS VARCHAR(10));
END
ELSE
BEGIN
    PRINT 'Partner role already exists, skipping.';
END

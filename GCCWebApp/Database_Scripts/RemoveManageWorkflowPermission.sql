-- Remove the "Manage Workflow" permission (certificate.workflow).
-- 2026-05-01: client requested removal. Permission was never enforced by any
-- controller/service code; workflow transitions are gated by role-based policies.
-- See SeedData.cs comment + memory note 2026-03-24 (QC #105).
--
-- Safe to run multiple times (idempotent).

DECLARE @permId INT = (SELECT Id FROM Permissions WHERE Codename = 'certificate.workflow');

IF @permId IS NOT NULL
BEGIN
    -- Remove role assignments first (FK)
    DELETE FROM PermissionRole WHERE PermissionsId = @permId;

    -- Remove the permission row
    DELETE FROM Permissions WHERE Id = @permId;

    PRINT 'Removed permission "Manage Workflow" (Id=' + CAST(@permId AS VARCHAR(10)) + ')';
END
ELSE
BEGIN
    PRINT 'Permission "Manage Workflow" not found, skipping (already removed).';
END

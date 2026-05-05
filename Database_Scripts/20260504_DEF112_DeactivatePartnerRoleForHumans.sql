-- DEF-112: Web-user "Partner" role and Partner-API-key "Partner" role share the same code path
--
-- Resolution: zero humans have the Partner role in prod (verified via /Admin/GetUsers on
-- gccwebapp.com 2026-05-04). The Partner role only ever fires from the API-key middleware,
-- which injects ClaimTypes.Role = "Partner" directly without consulting the Roles table.
--
-- This script soft-deactivates the Partner row in the Roles table so it stops appearing in
-- the User Management role dropdown (GetRolesListAsync filters Where(r => r.IsActive)).
-- Future admins cannot accidentally assign the role to a human user.
--
-- Zero impact on Partner API auth — middleware injects the claim directly, doesn't read the row.
-- Zero impact on existing users — none are assigned to the Partner role.
-- Fully reversible: re-run with IsActive = 1 if a future requirement needs human Partner users.
--
-- Run this once on every environment (dev, prod) after the deploy lands.

DECLARE @PartnerRoleId INT;
SELECT @PartnerRoleId = Id FROM Roles WHERE Name = 'Partner';

IF @PartnerRoleId IS NULL
BEGIN
    PRINT 'Partner role not found — no action needed.';
    RETURN;
END

-- Snapshot current state for audit
SELECT 'BEFORE' AS Phase, Id, Name, IsActive, Description FROM Roles WHERE Id = @PartnerRoleId;

-- Verify no human users currently have the Partner role (defense in depth)
DECLARE @PartnerHumanCount INT;
-- EF many-to-many table uses default pluralized FK names: RolesId, UsersId
SELECT @PartnerHumanCount = COUNT(*)
FROM UserRoles ur
INNER JOIN Users u ON ur.UsersId = u.Id
WHERE ur.RolesId = @PartnerRoleId AND u.IsDeleted = 0;

IF @PartnerHumanCount > 0
BEGIN
    PRINT CONCAT('ABORT: ', @PartnerHumanCount, ' human users currently have Partner role. Reassign them before running this script.');
    RETURN;
END

-- Soft-deactivate
UPDATE Roles
SET IsActive = 0,
    Description = 'API-key-only role injected by PartnerApiKeyAuthenticationMiddleware. Not assignable to web users (DEF-112 2026-05-04).'
WHERE Id = @PartnerRoleId;

-- Verify
SELECT 'AFTER' AS Phase, Id, Name, IsActive, Description FROM Roles WHERE Id = @PartnerRoleId;

PRINT 'DEF-112: Partner role soft-deactivated. /Admin/UserManagement role dropdown will no longer offer it.';

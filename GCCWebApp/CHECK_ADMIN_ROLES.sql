-- Check if admin user has roles assigned in production
-- This is critical for authentication to work

PRINT '======================================'
PRINT 'ADMIN USER ROLE ASSIGNMENT CHECK'
PRINT '======================================'
PRINT ''

-- Check Roles table
PRINT '--- Available Roles ---'
SELECT Id, Name, NormalizedName, Description
FROM Roles
ORDER BY Id;

PRINT ''
PRINT '--- Admin User Details ---'
SELECT Id, Username, Email, IsActive, Status
FROM Users
WHERE Username = 'admin';

PRINT ''
PRINT '--- Admin User Role Assignments ---'
SELECT
    u.Id AS UserId,
    u.Username,
    u.Email,
    r.Id AS RoleId,
    r.Name AS RoleName
FROM Users u
LEFT JOIN UserRoles ur ON u.Id = ur.UsersId
LEFT JOIN Roles r ON ur.RolesId = r.Id
WHERE u.Username = 'admin';

PRINT ''
PRINT '======================================'
PRINT 'EXPECTED RESULT:'
PRINT 'Admin user should have RoleId = 1 (Admin)'
PRINT ''
PRINT 'If no roles are assigned, run this:'
PRINT ''
PRINT '-- Assign Admin role to admin user'
PRINT 'INSERT INTO UserRoles (RolesId, UsersId)'
PRINT 'SELECT 1, u.Id'
PRINT 'FROM Users u'
PRINT 'WHERE u.Username = ''admin'''
PRINT 'AND NOT EXISTS ('
PRINT '    SELECT 1 FROM UserRoles ur'
PRINT '    WHERE ur.UsersId = u.Id AND ur.RolesId = 1'
PRINT ');'
PRINT '======================================'

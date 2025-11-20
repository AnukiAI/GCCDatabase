-- ========================================================================
-- ADD UAT TEST USERS TO PRODUCTION DATABASE
-- ========================================================================
-- Generated: 2025-11-19
-- Purpose: Add UAT-specific test accounts while preserving existing dev accounts
-- Database: gccwebap_certificates on 68.71.129.120,1533
--
-- UAT Credentials:
--   Admin:    admin@greencirclecertified.com / Admin123!
--   Analyst:  analyst@greencirclecertified.com / Analyst123!
--   External: partner@amazon.com / Partner123!
-- ========================================================================

USE gccwebap_certificates;
GO

-- Display existing users BEFORE changes
PRINT '========== EXISTING USERS BEFORE CHANGES ==========';
SELECT Id, Username, Email, Status, IsActive, IsDeleted
FROM dbo.Users
ORDER BY Username;
GO

-- Get Role IDs (we'll need these for user-role assignments)
DECLARE @AdminRoleId INT, @AnalystRoleId INT, @ExternalRoleId INT;

SELECT @AdminRoleId = Id FROM dbo.Roles WHERE Name = 'Admin';
SELECT @AnalystRoleId = Id FROM dbo.Roles WHERE Name = 'Analyst';
SELECT @ExternalRoleId = Id FROM dbo.Roles WHERE Name = 'External';

PRINT 'Role IDs:';
PRINT '  Admin: ' + CAST(@AdminRoleId AS VARCHAR);
PRINT '  Analyst: ' + CAST(@AnalystRoleId AS VARCHAR);
PRINT '  External: ' + CAST(@ExternalRoleId AS VARCHAR);
GO

-- ========================================================================
-- 1. ADD UAT ADMIN USER
-- ========================================================================

-- Check if UAT admin already exists
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@greencirclecertified.com')
BEGIN
    PRINT 'Adding UAT Admin user: admin@greencirclecertified.com';

    INSERT INTO dbo.Users (Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'admin.uat',
        'admin@greencirclecertified.com',
        'Admin UAT',
        'Admin UAT User',
        '$2a$11$UoNvHJFDMjtDHPmZwTJAuuzkVbJ6IcAklsAwRwHFQsagn1lF4r6zO', -- Admin123!
        'active',
        'Email',
        1,
        0,
        GETUTCDATE(),
        GETUTCDATE()
    );

    -- Assign Admin role
    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    DECLARE @AdminRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'Admin');

    INSERT INTO dbo.UserRoles (UserId, RoleId)
    VALUES (@AdminUserId, @AdminRoleId);

    PRINT 'UAT Admin user created successfully';
END
ELSE
BEGIN
    PRINT 'UAT Admin user already exists - updating password';
    UPDATE dbo.Users
    SET PasswordHash = '$2a$11$UoNvHJFDMjtDHPmZwTJAuuzkVbJ6IcAklsAwRwHFQsagn1lF4r6zO',
        IsActive = 1,
        IsDeleted = 0,
        Status = 'active',
        UpdatedAt = GETUTCDATE()
    WHERE Email = 'admin@greencirclecertified.com';
END
GO

-- ========================================================================
-- 2. ADD UAT ANALYST USER
-- ========================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'analyst@greencirclecertified.com')
BEGIN
    PRINT 'Adding UAT Analyst user: analyst@greencirclecertified.com';

    INSERT INTO dbo.Users (Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'analyst.uat',
        'analyst@greencirclecertified.com',
        'Analyst UAT',
        'Analyst UAT User',
        '$2a$11$Q.npkkcCJhA3NPmHfDhlAu3pXfQ/GQfdMSWnz9Jv5DKmDUD2XD6vO', -- Analyst123!
        'active',
        'Email',
        1,
        0,
        GETUTCDATE(),
        GETUTCDATE()
    );

    -- Assign Analyst role
    DECLARE @AnalystUserId INT = SCOPE_IDENTITY();
    DECLARE @AnalystRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'Analyst');

    INSERT INTO dbo.UserRoles (UserId, RoleId)
    VALUES (@AnalystUserId, @AnalystRoleId);

    PRINT 'UAT Analyst user created successfully';
END
ELSE
BEGIN
    PRINT 'UAT Analyst user already exists - updating password';
    UPDATE dbo.Users
    SET PasswordHash = '$2a$11$Q.npkkcCJhA3NPmHfDhlAu3pXfQ/GQfdMSWnz9Jv5DKmDUD2XD6vO',
        IsActive = 1,
        IsDeleted = 0,
        Status = 'active',
        UpdatedAt = GETUTCDATE()
    WHERE Email = 'analyst@greencirclecertified.com';
END
GO

-- ========================================================================
-- 3. ADD UAT EXTERNAL USER (Partner)
-- ========================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'partner@amazon.com')
BEGIN
    PRINT 'Adding UAT External user: partner@amazon.com';

    INSERT INTO dbo.Users (Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (
        'partner.amazon',
        'partner@amazon.com',
        'Amazon Partner',
        'Amazon Partner UAT User',
        '$2a$11$1PXM1s0bRhhOPzEf8.sArOhTiVL0ugcDhq3/erNGR2IOligjM79SW', -- Partner123!
        'active',
        'Email',
        1,
        0,
        GETUTCDATE(),
        GETUTCDATE()
    );

    -- Assign External role
    DECLARE @ExternalUserId INT = SCOPE_IDENTITY();
    DECLARE @ExternalRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'External');

    INSERT INTO dbo.UserRoles (UserId, RoleId)
    VALUES (@ExternalUserId, @ExternalRoleId);

    PRINT 'UAT External user created successfully';
END
ELSE
BEGIN
    PRINT 'UAT External user already exists - updating password';
    UPDATE dbo.Users
    SET PasswordHash = '$2a$11$1PXM1s0bRhhOPzEf8.sArOhTiVL0ugcDhq3/erNGR2IOligjM79SW',
        IsActive = 1,
        IsDeleted = 0,
        Status = 'active',
        UpdatedAt = GETUTCDATE()
    WHERE Email = 'partner@amazon.com';
END
GO

-- Display all users AFTER changes
PRINT '';
PRINT '========== ALL USERS AFTER CHANGES ==========';
SELECT
    u.Id,
    u.Username,
    u.Email,
    r.Name AS Role,
    u.Status,
    u.IsActive,
    u.IsDeleted
FROM dbo.Users u
LEFT JOIN dbo.UserRoles ur ON u.Id = ur.UserId
LEFT JOIN dbo.Roles r ON ur.RoleId = r.Id
ORDER BY u.Username;
GO

PRINT '';
PRINT '========================================';
PRINT 'UAT Users Added Successfully!';
PRINT '========================================';
PRINT '';
PRINT 'UAT Test Credentials (https://gccwebapp.com):';
PRINT '  Admin:    admin@greencirclecertified.com / Admin123!';
PRINT '  Analyst:  analyst@greencirclecertified.com / Analyst123!';
PRINT '  External: partner@amazon.com / Partner123!';
PRINT '';
PRINT 'Dev Test Credentials (for system testing):';
PRINT '  Admin:    username: admin / TestPass123!';
PRINT '  Analyst:  username: analyst / TestPass123!';
PRINT '  External: username: external / TestPass123!';
PRINT '';
PRINT '========================================';
GO

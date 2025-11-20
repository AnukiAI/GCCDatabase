-- ========================================================================
-- ADD UAT TEST USERS TO PRODUCTION DATABASE (V3 - All Required Fields)
-- ========================================================================
-- Generated: 2025-11-19 (Fixed ALL required columns)
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

-- ========================================================================
-- 1. ADD UAT ADMIN USER
-- ========================================================================

DECLARE @AdminUserId INT;
DECLARE @AdminRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'Admin');

-- Check if UAT admin already exists
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@greencirclecertified.com')
BEGIN
    PRINT 'Adding UAT Admin user: admin@greencirclecertified.com';

    INSERT INTO dbo.Users (
        Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod,
        IsActive, IsDeleted, IsStaff, IsSuperuser,
        RequirePasswordReset, TwoFactorEnabled, IsPhoneVerified, IsEmailVerified,
        MustChangePassword, RequirePasswordChange, FailedLoginAttempts,
        MaxConcurrentSessions, MaxPasswordHistory,
        CreatedAt, UpdatedAt
    )
    VALUES (
        'admin.uat',
        'admin@greencirclecertified.com',
        'Admin UAT',
        'Admin UAT User',
        '$2a$11$UoNvHJFDMjtDHPmZwTJAuuzkVbJ6IcAklsAwRwHFQsagn1lF4r6zO', -- Admin123!
        'active',
        'Email',
        1, -- IsActive
        0, -- IsDeleted
        0, -- IsStaff
        0, -- IsSuperuser
        0, -- RequirePasswordReset
        0, -- TwoFactorEnabled
        0, -- IsPhoneVerified
        1, -- IsEmailVerified
        0, -- MustChangePassword
        0, -- RequirePasswordChange
        0, -- FailedLoginAttempts
        3, -- MaxConcurrentSessions
        5, -- MaxPasswordHistory
        GETUTCDATE(),
        GETUTCDATE()
    );

    SET @AdminUserId = SCOPE_IDENTITY();

    -- Assign Admin role
    IF @AdminUserId IS NOT NULL
    BEGIN
        INSERT INTO dbo.UserRoles (UsersId, RolesId)
        VALUES (@AdminUserId, @AdminRoleId);
        PRINT 'UAT Admin user created successfully with ID: ' + CAST(@AdminUserId AS VARCHAR);
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Failed to create UAT Admin user';
    END
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

    -- Ensure role is assigned
    DECLARE @ExistingAdminId INT = (SELECT Id FROM dbo.Users WHERE Email = 'admin@greencirclecertified.com');
    IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UsersId = @ExistingAdminId AND RolesId = @AdminRoleId)
    BEGIN
        INSERT INTO dbo.UserRoles (UsersId, RolesId)
        VALUES (@ExistingAdminId, @AdminRoleId);
        PRINT 'Admin role assigned to existing user';
    END
END
GO

-- ========================================================================
-- 2. ADD UAT ANALYST USER
-- ========================================================================

DECLARE @AnalystUserId INT;
DECLARE @AnalystRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'Analyst');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'analyst@greencirclecertified.com')
BEGIN
    PRINT 'Adding UAT Analyst user: analyst@greencirclecertified.com';

    INSERT INTO dbo.Users (
        Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod,
        IsActive, IsDeleted, IsStaff, IsSuperuser,
        RequirePasswordReset, TwoFactorEnabled, IsPhoneVerified, IsEmailVerified,
        MustChangePassword, RequirePasswordChange, FailedLoginAttempts,
        MaxConcurrentSessions, MaxPasswordHistory,
        CreatedAt, UpdatedAt
    )
    VALUES (
        'analyst.uat',
        'analyst@greencirclecertified.com',
        'Analyst UAT',
        'Analyst UAT User',
        '$2a$11$Q.npkkcCJhA3NPmHfDhlAu3pXfQ/GQfdMSWnz9Jv5DKmDUD2XD6vO', -- Analyst123!
        'active',
        'Email',
        1, -- IsActive
        0, -- IsDeleted
        0, -- IsStaff
        0, -- IsSuperuser
        0, -- RequirePasswordReset
        0, -- TwoFactorEnabled
        0, -- IsPhoneVerified
        1, -- IsEmailVerified
        0, -- MustChangePassword
        0, -- RequirePasswordChange
        0, -- FailedLoginAttempts
        3, -- MaxConcurrentSessions
        5, -- MaxPasswordHistory
        GETUTCDATE(),
        GETUTCDATE()
    );

    SET @AnalystUserId = SCOPE_IDENTITY();

    -- Assign Analyst role
    IF @AnalystUserId IS NOT NULL
    BEGIN
        INSERT INTO dbo.UserRoles (UsersId, RolesId)
        VALUES (@AnalystUserId, @AnalystRoleId);
        PRINT 'UAT Analyst user created successfully with ID: ' + CAST(@AnalystUserId AS VARCHAR);
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Failed to create UAT Analyst user';
    END
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

    -- Ensure role is assigned
    DECLARE @ExistingAnalystId INT = (SELECT Id FROM dbo.Users WHERE Email = 'analyst@greencirclecertified.com');
    IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UsersId = @ExistingAnalystId AND RolesId = @AnalystRoleId)
    BEGIN
        INSERT INTO dbo.UserRoles (UsersId, RolesId)
        VALUES (@ExistingAnalystId, @AnalystRoleId);
        PRINT 'Analyst role assigned to existing user';
    END
END
GO

-- ========================================================================
-- 3. ADD UAT EXTERNAL USER (Partner)
-- ========================================================================

DECLARE @ExternalUserId INT;
DECLARE @ExternalRoleId INT = (SELECT Id FROM dbo.Roles WHERE Name = 'External');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'partner@amazon.com')
BEGIN
    PRINT 'Adding UAT External user: partner@amazon.com';

    INSERT INTO dbo.Users (
        Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod,
        IsActive, IsDeleted, IsStaff, IsSuperuser,
        RequirePasswordReset, TwoFactorEnabled, IsPhoneVerified, IsEmailVerified,
        MustChangePassword, RequirePasswordChange, FailedLoginAttempts,
        MaxConcurrentSessions, MaxPasswordHistory,
        CreatedAt, UpdatedAt
    )
    VALUES (
        'partner.amazon',
        'partner@amazon.com',
        'Amazon Partner',
        'Amazon Partner UAT User',
        '$2a$11$1PXM1s0bRhhOPzEf8.sArOhTiVL0ugcDhq3/erNGR2IOligjM79SW', -- Partner123!
        'active',
        'Email',
        1, -- IsActive
        0, -- IsDeleted
        0, -- IsStaff
        0, -- IsSuperuser
        0, -- RequirePasswordReset
        0, -- TwoFactorEnabled
        0, -- IsPhoneVerified
        1, -- IsEmailVerified
        0, -- MustChangePassword
        0, -- RequirePasswordChange
        0, -- FailedLoginAttempts
        3, -- MaxConcurrentSessions
        5, -- MaxPasswordHistory
        GETUTCDATE(),
        GETUTCDATE()
    );

    SET @ExternalUserId = SCOPE_IDENTITY();

    -- Assign External role
    IF @ExternalUserId IS NOT NULL
    BEGIN
        INSERT INTO dbo.UserRoles (UsersId, RolesId)
        VALUES (@ExternalUserId, @ExternalRoleId);
        PRINT 'UAT External user created successfully with ID: ' + CAST(@ExternalUserId AS VARCHAR);
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Failed to create UAT External user';
    END
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

    -- Ensure role is assigned
    DECLARE @ExistingExternalId INT = (SELECT Id FROM dbo.Users WHERE Email = 'partner@amazon.com');
    IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UsersId = @ExistingExternalId AND RolesId = @ExternalRoleId)
    BEGIN
        INSERT INTO dbo.UserRoles (UsersId, RolesId)
        VALUES (@ExistingExternalId, @ExternalRoleId);
        PRINT 'External role assigned to existing user';
    END
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
LEFT JOIN dbo.UserRoles ur ON u.Id = ur.UsersId
LEFT JOIN dbo.Roles r ON ur.RolesId = r.Id
ORDER BY u.Username;
GO

PRINT '';
PRINT '========================================';
PRINT 'UAT Users Setup Complete!';
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
PRINT 'Test all credentials at: https://gccwebapp.com/Account/Login';
PRINT '========================================';
GO

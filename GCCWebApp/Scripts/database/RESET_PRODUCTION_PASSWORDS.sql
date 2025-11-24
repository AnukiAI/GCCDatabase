-- ========================================================================
-- RESET PRODUCTION USER PASSWORDS
-- ========================================================================
-- Generated: 2025-11-19
-- Purpose: Reset all user passwords to "TestPass123!" for production database
-- Database: gccwebap_certificates on 68.71.129.120,1533
--
-- IMPORTANT: Run this script ONLY on the production database if users cannot log in
-- ========================================================================

USE gccwebap_certificates;
GO

-- Display current user status BEFORE update
PRINT '========== BEFORE UPDATE ==========';
SELECT Id, Username, Email,
       LEFT(PasswordHash, 20) + '...' as PasswordHash_Preview,
       Status, IsActive, IsDeleted, LastLoginAt
FROM dbo.Users
ORDER BY Username;
GO

-- Reset ALL user passwords to: TestPass123!
-- BCrypt Hash: $2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa
UPDATE dbo.Users
SET PasswordHash = '$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa'
WHERE IsDeleted = 0;  -- Only update non-deleted users
GO

-- Display updated user status AFTER update
PRINT '========== AFTER UPDATE ==========';
SELECT Id, Username, Email,
       LEFT(PasswordHash, 20) + '...' as PasswordHash_Preview,
       Status, IsActive, IsDeleted, LastLoginAt
FROM dbo.Users
ORDER BY Username;
GO

PRINT '';
PRINT '========================================';
PRINT 'Password Reset Complete!';
PRINT '========================================';
PRINT 'All user passwords have been reset to: TestPass123!';
PRINT '';
PRINT 'Test Credentials:';
PRINT '  Username: admin     Password: TestPass123!';
PRINT '  Username: analyst   Password: TestPass123!';
PRINT '  Username: external  Password: TestPass123!';
PRINT '';
PRINT 'Login URL: https://gccwebapp.com/Account/Login';
PRINT '========================================';
GO

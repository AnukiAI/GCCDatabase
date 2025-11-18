SET IDENTITY_INSERT dbo.Users ON;

DECLARE @PasswordHash NVARCHAR(500) = '$2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m';

INSERT INTO dbo.Users (Id, Username, Email, Name, FullName, PasswordHash, Status, TwoFactorMethod, IsActive, IsStaff, IsSuperuser, IsDeleted, RequirePasswordReset, TwoFactorEnabled, IsEmailVerified, IsPhoneVerified, FailedLoginAttempts, MaxConcurrentSessions, MustChangePassword, RequirePasswordChange, MaxPasswordHistory, CreatedAt, UpdatedAt)
VALUES
    (1, 'admin', 'admin@gcc.com', 'Admin', 'Admin User', @PasswordHash, 'active', 'Email', 1, 1, 1, 0, 0, 0, 1, 0, 0, 3, 0, 0, 5, GETDATE(), GETDATE()),
    (2, 'analyst', 'analyst@gcc.com', 'Jane', 'Jane Analyst', @PasswordHash, 'active', 'Email', 1, 1, 0, 0, 0, 0, 1, 0, 0, 3, 0, 0, 5, GETDATE(), GETDATE()),
    (3, 'external', 'external@company.com', 'External', 'External User', @PasswordHash, 'active', 'Email', 1, 0, 0, 0, 0, 0, 1, 0, 0, 3, 0, 0, 5, GETDATE(), GETDATE());

SET IDENTITY_INSERT dbo.Users OFF;

-- Assign roles
INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (1, 1);  -- admin -> Admin role
INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (2, 2);  -- analyst -> Analyst role
INSERT INTO dbo.RoleUser (RolesId, UsersId) VALUES (3, 3);  -- external -> External role

SELECT Username, Email FROM Users;

-- Reset admin password to: TestPass123!
-- Generated on: 2025-11-19 16:04:50
-- BCrypt hash: 

UPDATE dbo.Users
SET PasswordHash = ''
WHERE Username = 'admin';

-- Verify the update
SELECT Id, Username, Email, PasswordHash, Status, IsActive, IsDeleted
FROM dbo.Users
WHERE Username = 'admin';

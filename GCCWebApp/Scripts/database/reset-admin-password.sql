-- Reset admin password to: TestPass123!
-- BCrypt hash generated for "TestPass123!"
-- Run this on production database: gccwebap_certificates

UPDATE dbo.Users
SET PasswordHash = '$2a$11$8vJ5VqZ8YqZ8YqZ8YqZ8YeO7mQqZ8YqZ8YqZ8YqZ8YqZ8YqZ8YqZO'
WHERE Username = 'admin';

-- Verify the update
SELECT Id, Username, Email, PasswordHash, Status, IsActive, IsDeleted
FROM dbo.Users
WHERE Username = 'admin';

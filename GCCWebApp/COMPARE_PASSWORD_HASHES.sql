-- Compare dev and production password hashes
-- This script checks if admin passwords match between environments

PRINT '======================================'
PRINT 'PASSWORD HASH COMPARISON'
PRINT '======================================'
PRINT ''

-- Show admin user password hash
SELECT
    Id,
    Username,
    Email,
    PasswordHash,
    IsActive,
    Status,
    LastLoginAt
FROM Users
WHERE Username = 'admin';

PRINT ''
PRINT '======================================'
PRINT 'EXPECTED HASH (from dev):'
PRINT '$2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m'
PRINT ''
PRINT 'This hash works with password: TestPass123!'
PRINT '======================================'
PRINT ''

-- If hash doesn't match, show the update statement
PRINT 'If hash does not match, run this update:'
PRINT ''
PRINT 'UPDATE Users'
PRINT 'SET PasswordHash = ''$2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m'''
PRINT 'WHERE Username = ''admin'';'
PRINT ''

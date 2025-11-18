-- PRODUCTION LOGIN DIAGNOSTICS
-- Run these queries on the production database to diagnose login issues

-- 1. CHECK IF USERS TABLE EXISTS
SELECT
    CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
    THEN 'Users table EXISTS'
    ELSE 'Users table DOES NOT EXIST - Run migrations!'
    END AS TableStatus;

-- 2. COUNT USERS IN DATABASE
SELECT COUNT(*) AS TotalUsers FROM Users;

-- 3. LIST ALL USERS WITH THEIR STATUS
SELECT
    Id,
    Username,
    Email,
    Name,
    FullName,
    IsActive,
    IsDeleted,
    Status,
    CreatedAt,
    LastLoginAt,
    -- Show first 20 chars of password hash to verify it exists
    LEFT(PasswordHash, 20) + '...' AS PasswordHashPreview
FROM Users
ORDER BY Id;

-- 4. CHECK IF ADMIN USER EXISTS
SELECT
    Id,
    Username,
    Email,
    IsActive,
    IsDeleted,
    Status,
    LEFT(PasswordHash, 50) + '...' AS PasswordHashPreview
FROM Users
WHERE Username = 'admin' OR Email = 'swest@anukiconsulting.com';

-- 5. CHECK USER ROLES
SELECT
    u.Username,
    u.Email,
    r.Name AS RoleName
FROM Users u
LEFT JOIN UserRoles ur ON u.Id = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.Id
WHERE u.Username = 'admin' OR u.Email = 'swest@anukiconsulting.com';

-- 6. VERIFY ROLES TABLE
SELECT * FROM Roles ORDER BY Id;

-- 7. CHECK DATABASE SCHEMA OWNERSHIP
SELECT
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS TableName
FROM sys.tables
WHERE name IN ('Users', 'Roles', 'UserRoles', 'Certificates')
ORDER BY SchemaName, TableName;

-- EXPECTED RESULTS:
-- - All tables should be in 'dbo' schema (NOT GCC_adminxyz123 schema)
-- - Admin user should exist with Username='admin', Email='swest@anukiconsulting.com'
-- - Admin user should have IsActive=1, IsDeleted=0, Status='active'
-- - Admin user should have a role assigned
-- - Password hash should start with '$2a$11$' (BCrypt format)

-- IF USERS TABLE DOESN'T EXIST:
-- Run: dotnet ef database update --connection "Server=tcp:68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"

-- IF NO USERS EXIST:
-- Run the FULL_TEST_DATA_SEED.sql script to populate test data

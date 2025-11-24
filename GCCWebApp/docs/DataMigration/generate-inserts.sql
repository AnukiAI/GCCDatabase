-- Generate INSERT statements for migration
-- Run this on ANUKI01 development database

SET NOCOUNT ON;

-- Helper to escape single quotes
DECLARE @sql NVARCHAR(MAX) = '';

PRINT '-- ========================================';
PRINT '-- GCC Database Migration Script';
PRINT '-- Generated: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '-- Source: ANUKI01 / GCCDatabase_Dev';
PRINT '-- Target: Production';
PRINT '-- ========================================';
PRINT '';
PRINT '-- Disable constraints temporarily';
PRINT 'EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";';
PRINT 'GO';
PRINT '';

-- 1. Roles
PRINT '-- ========================================';
PRINT '-- 1. Roles';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Roles] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Roles] (Id, Name, NormalizedName, Description, ConcurrencyStamp, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    '''' + REPLACE(NormalizedName, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(ConcurrencyStamp, '''', '''''') + '''', 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''');'
FROM Roles
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Roles] OFF;';
PRINT 'GO';
PRINT '';

-- 2. Companies
PRINT '-- ========================================';
PRINT '-- 2. Companies';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Companies] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Companies] (Id, Name, ContactName, ContactEmail, ContactPhone, Address, City, State, ZipCode, Country, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(ContactName, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(ContactEmail, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(ContactPhone, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Address, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(City, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(State, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(ZipCode, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Country, '''', '''''') + '''', 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''', ' +
    ISNULL(CAST(CreatedBy AS NVARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(UpdatedBy AS NVARCHAR), 'NULL') + ');'
FROM Companies
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Companies] OFF;';
PRINT 'GO';
PRINT '';

-- 3. Partners
PRINT '-- ========================================';
PRINT '-- 3. Partners';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Partners] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Partners] (Id, Name, IsActive, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    CAST(IsActive AS NVARCHAR) + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''');'
FROM Partners
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Partners] OFF;';
PRINT 'GO';
PRINT '';

-- 4. CertificationTypes
PRINT '-- ========================================';
PRINT '-- 4. CertificationTypes';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [CertificationTypes] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [CertificationTypes] (Id, Name, Description, Level, ParentId, IsActive, DisplayOrder, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(Level AS NVARCHAR) + ', ' +
    ISNULL(CAST(ParentId AS NVARCHAR), 'NULL') + ', ' +
    CAST(IsActive AS NVARCHAR) + ', ' +
    CAST(DisplayOrder AS NVARCHAR) + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''');'
FROM CertificationTypes
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [CertificationTypes] OFF;';
PRINT 'GO';
PRINT '';

-- 5. Products
PRINT '-- ========================================';
PRINT '-- 5. Products';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Products] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Products] (Id, Name, Description, Sku, CompanyId, ProductType, Category, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Sku, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CompanyId AS NVARCHAR) + ', ' +
    ISNULL('''' + REPLACE(ProductType, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Category, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(IsActive AS NVARCHAR) + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''', ' +
    ISNULL(CAST(CreatedBy AS NVARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(UpdatedBy AS NVARCHAR), 'NULL') + ');'
FROM Products
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Products] OFF;';
PRINT 'GO';
PRINT '';

-- 6. Users
PRINT '-- ========================================';
PRINT '-- 6. Users';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Users] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Users] (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, IsActive, LastLoginDate, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    ISNULL('''' + REPLACE(UserName, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(NormalizedUserName, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Email, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(NormalizedEmail, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(EmailConfirmed AS NVARCHAR) + ', ' +
    ISNULL('''' + REPLACE(PasswordHash, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(SecurityStamp, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(ConcurrencyStamp, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(PhoneNumber, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(PhoneNumberConfirmed AS NVARCHAR) + ', ' +
    CAST(TwoFactorEnabled AS NVARCHAR) + ', ' +
    ISNULL('''' + CONVERT(VARCHAR, LockoutEnd, 120) + '''', 'NULL') + ', ' +
    CAST(LockoutEnabled AS NVARCHAR) + ', ' +
    CAST(AccessFailedCount AS NVARCHAR) + ', ' +
    ISNULL('''' + REPLACE(FirstName, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(LastName, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(IsActive AS NVARCHAR) + ', ' +
    ISNULL('''' + CONVERT(VARCHAR, LastLoginDate, 120) + '''', 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''');'
FROM Users
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Users] OFF;';
PRINT 'GO';
PRINT '';

-- 7. Tags
PRINT '-- ========================================';
PRINT '-- 7. Tags';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Tags] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Tags] (Id, Name, Description, IsInternal, Color, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(IsInternal AS NVARCHAR) + ', ' +
    ISNULL('''' + REPLACE(Color, '''', '''''') + '''', 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''');'
FROM Tags
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Tags] OFF;';
PRINT 'GO';
PRINT '';

-- 8. Certificates
PRINT '-- ========================================';
PRINT '-- 8. Certificates (Main Data)';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [Certificates] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [Certificates] (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, Asin, Upc, Gtin, Ean, CertificateFile, CertificateImagePath, Partner, CompanyId, ProductId, CertificationTypeId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(Number, '''', '''''') + ''', ' +
    '''' + REPLACE(Status, '''', '''''') + ''', ' +
    '''' + CONVERT(VARCHAR, EffectiveDate, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, ExpirationDate, 120) + ''', ' +
    ISNULL('''' + REPLACE(Location, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Country, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Asin, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Upc, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Gtin, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Ean, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(CertificateFile, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(CertificateImagePath, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Partner, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CompanyId AS NVARCHAR) + ', ' +
    CAST(ProductId AS NVARCHAR) + ', ' +
    CAST(CertificationTypeId AS NVARCHAR) + ', ' +
    ISNULL(CAST(CreatedBy AS NVARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(UpdatedBy AS NVARCHAR), 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, UpdatedAt, 120) + ''');'
FROM Certificates
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [Certificates] OFF;';
PRINT 'GO';
PRINT '';

-- 9. ApiKeys
PRINT '-- ========================================';
PRINT '-- 9. ApiKeys';
PRINT '-- ========================================';
PRINT 'SET IDENTITY_INSERT [ApiKeys] ON;';
PRINT 'GO';
SELECT 'INSERT INTO [ApiKeys] (Id, KeyValue, Description, UserId, IsActive, ExpiresAt, CreatedAt) VALUES (' +
    CAST(Id AS NVARCHAR) + ', ' +
    '''' + REPLACE(KeyValue, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(UserId AS NVARCHAR) + ', ' +
    CAST(IsActive AS NVARCHAR) + ', ' +
    ISNULL('''' + CONVERT(VARCHAR, ExpiresAt, 120) + '''', 'NULL') + ', ' +
    '''' + CONVERT(VARCHAR, CreatedAt, 120) + ''');'
FROM ApiKeys
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT [ApiKeys] OFF;';
PRINT 'GO';
PRINT '';

PRINT '-- Enable constraints';
PRINT 'EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";';
PRINT 'GO';
PRINT '';
PRINT '-- Migration script generation complete';

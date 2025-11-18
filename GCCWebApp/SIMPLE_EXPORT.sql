-- Simple export script - only core tables
USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

PRINT '-- Companies';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
SELECT 'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(ContactInfo, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Location, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Country, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CAST(IsActive AS INT) AS VARCHAR) + ', ' +
    CAST(CAST(IsDeleted AS INT) AS VARCHAR) + ', ' +
    ISNULL('''' + REPLACE(Website, '''', '''''') + '''', 'NULL') + ', ' +
    'GETDATE(), GETDATE());'
FROM Companies
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT '';

PRINT '-- Products';
PRINT 'SET IDENTITY_INSERT dbo.Products ON;';
SELECT 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL(CAST(CategoryId AS VARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(ProductTypeId AS VARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(CompanyId AS VARCHAR), 'NULL') + ', ' +
    CAST(CAST(IsActive AS INT) AS VARCHAR) + ', ' +
    CAST(CAST(IsDeleted AS INT) AS VARCHAR) + ', ' +
    'GETDATE(), GETDATE());'
FROM Products
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT dbo.Products OFF;';
PRINT '';

PRINT '-- Certificates (Active only)';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
SELECT 'INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + Number + ''', ' +
    '''' + Status + ''', ' +
    '''' + CONVERT(VARCHAR, EffectiveDate, 120) + ''', ' +
    '''' + CONVERT(VARCHAR, ExpirationDate, 120) + ''', ' +
    ISNULL('''' + REPLACE(Location, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Country, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CompanyId AS VARCHAR) + ', ' +
    CAST(ProductId AS VARCHAR) + ', ' +
    CAST(CertificationTypeId AS VARCHAR) + ', ' +
    ISNULL('''' + Partner + '''', 'NULL') + ', ' +
    ISNULL('''' + Asin + '''', 'NULL') + ', ' +
    ISNULL('''' + Upc + '''', 'NULL') + ', ' +
    ISNULL('''' + Gtin + '''', 'NULL') + ', ' +
    ISNULL('''' + Ean + '''', 'NULL') + ', ' +
    'GETDATE(), GETDATE());'
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT '';

PRINT '-- CertificateProductIds';
PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds ON;';
SELECT 'INSERT INTO dbo.CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    CAST(CertificateId AS VARCHAR) + ', ' +
    '''' + ProductIdType + ''', ' +
    '''' + REPLACE(ProductIdValue, '''', '''''') + ''');'
FROM CertificateProductIds
WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
ORDER BY Id;
PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds OFF;';
PRINT '';

PRINT '-- Summary:';
SELECT 'Companies' as Entity, COUNT(*) as Count FROM Companies
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates (Active)', COUNT(*) FROM Certificates WHERE Status = 'Active'
UNION ALL SELECT 'ProductIDs', COUNT(*) FROM CertificateProductIds WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active');

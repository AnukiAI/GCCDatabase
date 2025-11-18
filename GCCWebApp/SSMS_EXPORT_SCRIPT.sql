-- ============================================
-- Run this in SSMS on ANUKI01 / GCCDatabase_Dev
-- Copy the results and run on production
-- ============================================

USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

-- Get counts first
DECLARE @CompanyCount INT, @ProductCount INT, @CertCount INT;
SELECT @CompanyCount = COUNT(*) FROM Companies;
SELECT @ProductCount = COUNT(*) FROM Products;
SELECT @CertCount = COUNT(*) FROM Certificates WHERE Status = 'Active';

-- ============================================
-- CLEAR PRODUCTION DATA (Run this on production FIRST)
-- ============================================
PRINT '-- ============================================';
PRINT '-- STEP 1: Run this on PRODUCTION first to clear data:';
PRINT '-- ============================================';
PRINT 'DELETE FROM CertificateProductIds;';
PRINT 'DELETE FROM Certificates;';
PRINT 'DELETE FROM Products;';
PRINT 'DELETE FROM Companies;';
PRINT 'GO';
PRINT '';
PRINT '-- ============================================';
PRINT '-- STEP 2: Then run the INSERT statements below:';
PRINT '-- ============================================';
PRINT '';

-- ============================================
-- COMPANIES
-- ============================================
PRINT '';
PRINT '-- Companies (' + CAST(@CompanyCount AS VARCHAR) + ')';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
PRINT 'GO';

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

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- PRODUCTS
-- ============================================
PRINT '';
PRINT '-- Products (' + CAST(@ProductCount AS VARCHAR) + ')';
PRINT 'SET IDENTITY_INSERT dbo.Products ON;';
PRINT 'GO';

SELECT 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(CAST(Description AS NVARCHAR(MAX)), '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL(CAST(CategoryId AS VARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(ProductTypeId AS VARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(CompanyId AS VARCHAR), 'NULL') + ', ' +
    CAST(CAST(IsActive AS INT) AS VARCHAR) + ', ' +
    CAST(CAST(IsDeleted AS INT) AS VARCHAR) + ', ' +
    'GETDATE(), GETDATE());'
FROM Products
ORDER BY Id;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Products OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- CERTIFICATES (Active only)
-- ============================================
PRINT '';
PRINT '-- Certificates - Active only (' + CAST(@CertCount AS VARCHAR) + ')';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
PRINT 'GO';

SELECT 'INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + Number + ''', ' +
    '''' + Status + ''', ' +
    '''' + CONVERT(VARCHAR(23), EffectiveDate, 121) + ''', ' +
    '''' + CONVERT(VARCHAR(23), ExpirationDate, 121) + ''', ' +
    ISNULL('''' + REPLACE(Location, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Country, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CompanyId AS VARCHAR) + ', ' +
    CAST(ProductId AS VARCHAR) + ', ' +
    CAST(CertificationTypeId AS VARCHAR) + ', ' +
    ISNULL('''' + Partner + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Asin, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Upc, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Gtin, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Ean, '''', '''''') + '''', 'NULL') + ', ' +
    'GETDATE(), GETDATE());'
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- CERTIFICATE PRODUCT IDs
-- ============================================
DECLARE @ProductIdCount INT;
SELECT @ProductIdCount = COUNT(*)
FROM CertificateProductIds
WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active');

IF @ProductIdCount > 0
BEGIN
    PRINT '';
    PRINT '-- Certificate Product IDs (' + CAST(@ProductIdCount AS VARCHAR) + ')';
    PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds ON;';
    PRINT 'GO';

    SELECT 'INSERT INTO dbo.CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue) VALUES (' +
        CAST(Id AS VARCHAR) + ', ' +
        CAST(CertificateId AS VARCHAR) + ', ' +
        '''' + ProductIdType + ''', ' +
        '''' + REPLACE(ProductIdValue, '''', '''''') + ''');'
    FROM CertificateProductIds
    WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
    ORDER BY Id;

    PRINT 'GO';
    PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds OFF;';
    PRINT 'GO';
    PRINT '';
END

-- ============================================
-- SUMMARY
-- ============================================
PRINT '';
PRINT '-- ============================================';
PRINT '-- IMPORT COMPLETE';
PRINT '-- ============================================';
PRINT '-- Verify counts:';
PRINT 'SELECT ''Companies'' as Entity, COUNT(*) as Count FROM Companies';
PRINT 'UNION ALL SELECT ''Products'', COUNT(*) FROM Products';
PRINT 'UNION ALL SELECT ''Certificates'', COUNT(*) FROM Certificates';
PRINT 'UNION ALL SELECT ''ProductIDs'', COUNT(*) FROM CertificateProductIds;';
PRINT 'GO';
PRINT '';
PRINT '-- Expected counts from dev:';

SELECT 'Companies' as Entity, COUNT(*) as Count FROM Companies
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Certificates (Active)', COUNT(*) FROM Certificates WHERE Status = 'Active'
UNION ALL SELECT 'ProductIDs', COUNT(*) FROM CertificateProductIds WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
ORDER BY Entity;

PRINT '';
PRINT '-- ============================================';
PRINT '-- DONE! Now restart IIS application pool';
PRINT '-- ============================================';

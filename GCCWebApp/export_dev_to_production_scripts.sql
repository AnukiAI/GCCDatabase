-- ============================================
-- Export Dev Data to Production-Compatible Scripts
-- ============================================
-- Run this in SSMS connected to ANUKI01 / GCCDatabase_Dev
-- This will generate INSERT scripts you can run on production
-- ============================================

USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

-- ============================================
-- Step 1: Export Companies
-- ============================================
PRINT '-- ============================================';
PRINT '-- COMPANIES DATA (Production Compatible)';
PRINT '-- ============================================';
PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
PRINT '';

DECLARE @CompanySql NVARCHAR(MAX) = '';

SELECT @CompanySql = @CompanySql +
    'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(ContactInfo, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Location, '''', '''''') + '''', 'NULL') + ', ' +
    ISNULL('''' + REPLACE(Country, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CAST(IsActive AS INT) AS VARCHAR) + ', ' +
    CAST(CAST(IsDeleted AS INT) AS VARCHAR) + ', ' +
    ISNULL('''' + REPLACE(Website, '''', '''''') + '''', 'NULL') + ', ' +
    'GETDATE(), GETDATE());' + CHAR(13) + CHAR(10)
FROM Companies
ORDER BY Id;

PRINT @CompanySql;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT '';

-- ============================================
-- Step 2: Export Products
-- ============================================
PRINT '-- ============================================';
PRINT '-- PRODUCTS DATA (Production Compatible)';
PRINT '-- ============================================';
PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Products ON;';
PRINT '';

DECLARE @ProductSql NVARCHAR(MAX) = '';

SELECT @ProductSql = @ProductSql +
    'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
    CAST(Id AS VARCHAR) + ', ' +
    '''' + REPLACE(Name, '''', '''''') + ''', ' +
    ISNULL('''' + REPLACE(Description, '''', '''''') + '''', 'NULL') + ', ' +
    CAST(CategoryId AS VARCHAR) + ', ' +
    ISNULL(CAST(ProductTypeId AS VARCHAR), 'NULL') + ', ' +
    ISNULL(CAST(CompanyId AS VARCHAR), 'NULL') + ', ' +
    CAST(CAST(IsActive AS INT) AS VARCHAR) + ', ' +
    CAST(CAST(IsDeleted AS INT) AS VARCHAR) + ', ' +
    'GETDATE(), GETDATE());' + CHAR(13) + CHAR(10)
FROM Products
ORDER BY Id;

PRINT @ProductSql;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Products OFF;';
PRINT '';

-- ============================================
-- Step 3: Export Active Certificates
-- ============================================
PRINT '-- ============================================';
PRINT '-- CERTIFICATES DATA (Active Only - Production Compatible)';
PRINT '-- ============================================';
PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
PRINT '';

DECLARE @CertSql NVARCHAR(MAX) = '';

SELECT @CertSql = @CertSql +
    'INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (' +
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
    'GETDATE(), GETDATE());' + CHAR(13) + CHAR(10)
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;

PRINT @CertSql;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT '';

PRINT '-- ============================================';
PRINT '-- EXPORT COMPLETE';
PRINT '-- Copy all output and save as: PRODUCTION_DATA_FROM_DEV.sql';
PRINT '-- Then run on production database';
PRINT '-- ============================================';

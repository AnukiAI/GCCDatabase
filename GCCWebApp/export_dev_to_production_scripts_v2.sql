-- ============================================
-- Export Dev Data to Production-Compatible Scripts v2
-- ============================================
USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

PRINT '-- ============================================';
PRINT '-- COMPANIES DATA (Production Compatible)';
PRINT '-- ============================================';
PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
PRINT '';

-- Export Companies one by one
DECLARE @Id INT, @Name NVARCHAR(500), @ContactInfo NVARCHAR(500), @Location NVARCHAR(200);
DECLARE @Country NVARCHAR(100), @IsActive BIT, @IsDeleted BIT, @Website NVARCHAR(500);

DECLARE company_cursor CURSOR FOR
SELECT Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website
FROM Companies
ORDER BY Id;

OPEN company_cursor;
FETCH NEXT FROM company_cursor INTO @Id, @Name, @ContactInfo, @Location, @Country, @IsActive, @IsDeleted, @Website;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@Id AS VARCHAR) + ', ' +
        '''' + REPLACE(@Name, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@ContactInfo, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Location, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Country, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(CAST(@IsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@IsDeleted AS INT) AS VARCHAR) + ', ' +
        ISNULL('''' + REPLACE(@Website, '''', '''''') + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    FETCH NEXT FROM company_cursor INTO @Id, @Name, @ContactInfo, @Location, @Country, @IsActive, @IsDeleted, @Website;
END;

CLOSE company_cursor;
DEALLOCATE company_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT '';

-- Export Products
PRINT '-- ============================================';
PRINT '-- PRODUCTS DATA (Production Compatible)';
PRINT '-- ============================================';
PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Products ON;';
PRINT '';

DECLARE @ProdId INT, @ProdName NVARCHAR(500), @Description NVARCHAR(MAX), @CategoryId INT;
DECLARE @ProductTypeId INT, @CompanyId INT, @ProdIsActive BIT, @ProdIsDeleted BIT;

DECLARE product_cursor CURSOR FOR
SELECT Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted
FROM Products
ORDER BY Id;

OPEN product_cursor;
FETCH NEXT FROM product_cursor INTO @ProdId, @ProdName, @Description, @CategoryId, @ProductTypeId, @CompanyId, @ProdIsActive, @ProdIsDeleted;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@ProdId AS VARCHAR) + ', ' +
        '''' + REPLACE(@ProdName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@Description, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(@CategoryId AS VARCHAR) + ', ' +
        ISNULL(CAST(@ProductTypeId AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@CompanyId AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@ProdIsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@ProdIsDeleted AS INT) AS VARCHAR) + ', ' +
        'GETDATE(), GETDATE());';

    FETCH NEXT FROM product_cursor INTO @ProdId, @ProdName, @Description, @CategoryId, @ProductTypeId, @CompanyId, @ProdIsActive, @ProdIsDeleted;
END;

CLOSE product_cursor;
DEALLOCATE product_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Products OFF;';
PRINT '';

-- Export Certificates
PRINT '-- ============================================';
PRINT '-- CERTIFICATES DATA (Active Only - Production Compatible)';
PRINT '-- ============================================';
PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
PRINT '';

DECLARE @CertId INT, @Number NVARCHAR(100), @Status NVARCHAR(50), @EffectiveDate DATETIME, @ExpirationDate DATETIME;
DECLARE @CertLocation NVARCHAR(200), @CertCountry NVARCHAR(100), @CertCompanyId INT, @ProductId INT, @CertificationTypeId INT;
DECLARE @Partner NVARCHAR(100), @Asin NVARCHAR(100), @Upc NVARCHAR(100), @Gtin NVARCHAR(100), @Ean NVARCHAR(100);

DECLARE cert_cursor CURSOR FOR
SELECT Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;

OPEN cert_cursor;
FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate, @CertLocation, @CertCountry, @CertCompanyId, @ProductId, @CertificationTypeId, @Partner, @Asin, @Upc, @Gtin, @Ean;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@CertId AS VARCHAR) + ', ' +
        '''' + @Number + ''', ' +
        '''' + @Status + ''', ' +
        '''' + CONVERT(VARCHAR, @EffectiveDate, 120) + ''', ' +
        '''' + CONVERT(VARCHAR, @ExpirationDate, 120) + ''', ' +
        ISNULL('''' + REPLACE(@CertLocation, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@CertCountry, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(@CertCompanyId AS VARCHAR) + ', ' +
        CAST(@ProductId AS VARCHAR) + ', ' +
        CAST(@CertificationTypeId AS VARCHAR) + ', ' +
        ISNULL('''' + @Partner + '''', 'NULL') + ', ' +
        ISNULL('''' + @Asin + '''', 'NULL') + ', ' +
        ISNULL('''' + @Upc + '''', 'NULL') + ', ' +
        ISNULL('''' + @Gtin + '''', 'NULL') + ', ' +
        ISNULL('''' + @Ean + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate, @CertLocation, @CertCountry, @CertCompanyId, @ProductId, @CertificationTypeId, @Partner, @Asin, @Upc, @Gtin, @Ean;
END;

CLOSE cert_cursor;
DEALLOCATE cert_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT '';

PRINT '-- ============================================';
PRINT '-- EXPORT COMPLETE';
PRINT '-- ============================================';

-- Export script using cursor and PRINT (no truncation)
USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

PRINT '-- ============================================';
PRINT '-- STEP 1: Clear production data first';
PRINT '-- ============================================';
PRINT 'DELETE FROM CertificateProductIds;';
PRINT 'DELETE FROM Certificates;';
PRINT 'DELETE FROM Products;';
PRINT 'DELETE FROM Companies;';
PRINT 'GO';
PRINT '';
PRINT '-- ============================================';
PRINT '-- STEP 2: Insert data';
PRINT '-- ============================================';
PRINT '';

-- Companies
PRINT '-- Companies';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
PRINT 'GO';

DECLARE @Id INT, @Name NVARCHAR(500), @ContactInfo NVARCHAR(500), @Location NVARCHAR(500);
DECLARE @Country NVARCHAR(200), @IsActive BIT, @IsDeleted BIT, @Website NVARCHAR(500);
DECLARE @Sql NVARCHAR(MAX);

DECLARE company_cursor CURSOR FOR
SELECT Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website
FROM Companies
ORDER BY Id;

OPEN company_cursor;
FETCH NEXT FROM company_cursor INTO @Id, @Name, @ContactInfo, @Location, @Country, @IsActive, @IsDeleted, @Website;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@Id AS VARCHAR) + ', ' +
        '''' + REPLACE(@Name, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@ContactInfo, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Location, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Country, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(CAST(@IsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@IsDeleted AS INT) AS VARCHAR) + ', ' +
        ISNULL('''' + REPLACE(@Website, '''', '''''') + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    PRINT @Sql;
    FETCH NEXT FROM company_cursor INTO @Id, @Name, @ContactInfo, @Location, @Country, @IsActive, @IsDeleted, @Website;
END;

CLOSE company_cursor;
DEALLOCATE company_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT 'GO';
PRINT '';

-- Products
PRINT '-- Products';
PRINT 'SET IDENTITY_INSERT dbo.Products ON;';
PRINT 'GO';

DECLARE @ProductId INT, @ProductName NVARCHAR(500), @Description NVARCHAR(MAX);
DECLARE @CategoryId INT, @ProductTypeId INT, @CompanyId INT;
DECLARE @ProdIsActive BIT, @ProdIsDeleted BIT;

DECLARE product_cursor CURSOR FOR
SELECT Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted
FROM Products
ORDER BY Id;

OPEN product_cursor;
FETCH NEXT FROM product_cursor INTO @ProductId, @ProductName, @Description, @CategoryId, @ProductTypeId, @CompanyId, @ProdIsActive, @ProdIsDeleted;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@ProductId AS VARCHAR) + ', ' +
        '''' + REPLACE(@ProductName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(CAST(@Description AS NVARCHAR(MAX)), '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL(CAST(@CategoryId AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@ProductTypeId AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@CompanyId AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@ProdIsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@ProdIsDeleted AS INT) AS VARCHAR) + ', ' +
        'GETDATE(), GETDATE());';

    PRINT @Sql;
    FETCH NEXT FROM product_cursor INTO @ProductId, @ProductName, @Description, @CategoryId, @ProductTypeId, @CompanyId, @ProdIsActive, @ProdIsDeleted;
END;

CLOSE product_cursor;
DEALLOCATE product_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Products OFF;';
PRINT 'GO';
PRINT '';

-- Certificates (Active only)
PRINT '-- Certificates (Active only)';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
PRINT 'GO';

DECLARE @CertId INT, @Number NVARCHAR(100), @Status NVARCHAR(50);
DECLARE @EffectiveDate DATETIME, @ExpirationDate DATETIME;
DECLARE @CertLocation NVARCHAR(500), @CertCountry NVARCHAR(200);
DECLARE @CertCompanyId INT, @CertProductId INT, @CertificationTypeId INT;
DECLARE @Partner NVARCHAR(100), @Asin NVARCHAR(100), @Upc NVARCHAR(100);
DECLARE @Gtin NVARCHAR(100), @Ean NVARCHAR(100);

DECLARE cert_cursor CURSOR FOR
SELECT Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country,
       CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;

OPEN cert_cursor;
FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate,
    @CertLocation, @CertCountry, @CertCompanyId, @CertProductId, @CertificationTypeId,
    @Partner, @Asin, @Upc, @Gtin, @Ean;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@CertId AS VARCHAR) + ', ' +
        '''' + @Number + ''', ' +
        '''' + @Status + ''', ' +
        '''' + CONVERT(VARCHAR(23), @EffectiveDate, 121) + ''', ' +
        '''' + CONVERT(VARCHAR(23), @ExpirationDate, 121) + ''', ' +
        ISNULL('''' + REPLACE(@CertLocation, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@CertCountry, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(@CertCompanyId AS VARCHAR) + ', ' +
        CAST(@CertProductId AS VARCHAR) + ', ' +
        CAST(@CertificationTypeId AS VARCHAR) + ', ' +
        ISNULL('''' + @Partner + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Asin, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Upc, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Gtin, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Ean, '''', '''''') + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    PRINT @Sql;
    FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate,
        @CertLocation, @CertCountry, @CertCompanyId, @CertProductId, @CertificationTypeId,
        @Partner, @Asin, @Upc, @Gtin, @Ean;
END;

CLOSE cert_cursor;
DEALLOCATE cert_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT 'GO';
PRINT '';

-- CertificateProductIds
DECLARE @ProdIdCount INT;
SELECT @ProdIdCount = COUNT(*)
FROM CertificateProductIds
WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active');

IF @ProdIdCount > 0
BEGIN
    PRINT '-- CertificateProductIds';
    PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds ON;';
    PRINT 'GO';

    DECLARE @ProdIdId INT, @ProdIdCertId INT, @ProdIdType NVARCHAR(50), @ProdIdValue NVARCHAR(100);

    DECLARE prodid_cursor CURSOR FOR
    SELECT Id, CertificateId, ProductIdType, ProductIdValue
    FROM CertificateProductIds
    WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
    ORDER BY Id;

    OPEN prodid_cursor;
    FETCH NEXT FROM prodid_cursor INTO @ProdIdId, @ProdIdCertId, @ProdIdType, @ProdIdValue;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Sql = 'INSERT INTO dbo.CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue) VALUES (' +
            CAST(@ProdIdId AS VARCHAR) + ', ' +
            CAST(@ProdIdCertId AS VARCHAR) + ', ' +
            '''' + @ProdIdType + ''', ' +
            '''' + REPLACE(@ProdIdValue, '''', '''''') + ''');';

        PRINT @Sql;
        FETCH NEXT FROM prodid_cursor INTO @ProdIdId, @ProdIdCertId, @ProdIdType, @ProdIdValue;
    END;

    CLOSE prodid_cursor;
    DEALLOCATE prodid_cursor;

    PRINT 'GO';
    PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds OFF;';
    PRINT 'GO';
    PRINT '';
END;

PRINT '';
PRINT '-- ============================================';
PRINT '-- DONE! Verify counts:';
PRINT '-- ============================================';
PRINT 'SELECT ''Companies'' as Entity, COUNT(*) as Count FROM Companies';
PRINT 'UNION ALL SELECT ''Products'', COUNT(*) FROM Products';
PRINT 'UNION ALL SELECT ''Certificates'', COUNT(*) FROM Certificates';
PRINT 'UNION ALL SELECT ''ProductIDs'', COUNT(*) FROM CertificateProductIds;';
PRINT 'GO';

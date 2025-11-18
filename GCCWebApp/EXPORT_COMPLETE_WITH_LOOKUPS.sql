-- Complete export script including ALL lookup tables
-- Run this on ANUKI01/GCCDatabase_Dev
USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

PRINT '-- ============================================';
PRINT '-- STEP 1: Clear production data first';
PRINT '-- ============================================';
PRINT 'DELETE FROM CertificateProductIds;';
PRINT 'DELETE FROM Certificates;';
PRINT 'DELETE FROM Products;';
PRINT 'DELETE FROM ProductTypes;';
PRINT 'DELETE FROM Companies;';
PRINT 'DELETE FROM ProductCategories;';
PRINT 'DELETE FROM CertificationTypes;';
PRINT 'GO';
PRINT '';
PRINT '-- ============================================';
PRINT '-- STEP 2: Insert lookup tables';
PRINT '-- ============================================';
PRINT '';

-- ============================================
-- PRODUCT CATEGORIES
-- ============================================
PRINT '-- ProductCategories';
PRINT 'SET IDENTITY_INSERT dbo.ProductCategories ON;';
PRINT 'GO';

DECLARE @Id INT, @Name NVARCHAR(255), @Description NVARCHAR(MAX);
DECLARE @SortOrder INT, @IsActive BIT;
DECLARE @Sql NVARCHAR(MAX);

DECLARE category_cursor CURSOR FOR
SELECT Id, Name, Description, SortOrder, IsActive
FROM ProductCategories
ORDER BY Id;

OPEN category_cursor;
FETCH NEXT FROM category_cursor INTO @Id, @Name, @Description, @SortOrder, @IsActive;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive) VALUES (' +
        CAST(@Id AS VARCHAR) + ', ' +
        '''' + REPLACE(@Name, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(CAST(@Description AS NVARCHAR(MAX)), '''', '''''') + '''', 'NULL') + ', ' +
        CAST(@SortOrder AS VARCHAR) + ', ' +
        CAST(CAST(@IsActive AS INT) AS VARCHAR) + ');';

    PRINT @Sql;
    FETCH NEXT FROM category_cursor INTO @Id, @Name, @Description, @SortOrder, @IsActive;
END;

CLOSE category_cursor;
DEALLOCATE category_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.ProductCategories OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- PRODUCT TYPES
-- ============================================
PRINT '-- ProductTypes';
PRINT 'SET IDENTITY_INSERT dbo.ProductTypes ON;';
PRINT 'GO';

DECLARE @TypeId INT, @TypeName NVARCHAR(255), @CategoryId INT;
DECLARE @ParentId INT, @Level INT, @TypeSortOrder INT, @TypeIsActive BIT;

DECLARE type_cursor CURSOR FOR
SELECT Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive
FROM ProductTypes
ORDER BY Id;

OPEN type_cursor;
FETCH NEXT FROM type_cursor INTO @TypeId, @TypeName, @CategoryId, @ParentId, @Level, @TypeSortOrder, @TypeIsActive;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, ParentId, Level, SortOrder, IsActive) VALUES (' +
        CAST(@TypeId AS VARCHAR) + ', ' +
        '''' + REPLACE(@TypeName, '''', '''''') + ''', ' +
        CAST(@CategoryId AS VARCHAR) + ', ' +
        ISNULL(CAST(@ParentId AS VARCHAR), 'NULL') + ', ' +
        CAST(@Level AS VARCHAR) + ', ' +
        CAST(@TypeSortOrder AS VARCHAR) + ', ' +
        CAST(CAST(@TypeIsActive AS INT) AS VARCHAR) + ');';

    PRINT @Sql;
    FETCH NEXT FROM type_cursor INTO @TypeId, @TypeName, @CategoryId, @ParentId, @Level, @TypeSortOrder, @TypeIsActive;
END;

CLOSE type_cursor;
DEALLOCATE type_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.ProductTypes OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- CERTIFICATION TYPES
-- ============================================
PRINT '-- CertificationTypes';
PRINT 'SET IDENTITY_INSERT dbo.CertificationTypes ON;';
PRINT 'GO';

DECLARE @CertTypeId INT, @CertTypeName NVARCHAR(255), @AmazonCode NVARCHAR(255);
DECLARE @CertTypeDescription NVARCHAR(MAX), @CertTypeIsActive BIT;
DECLARE @CertTypeSortOrder INT, @CertTypeLevel INT, @CertTypeParentId INT;

DECLARE certtype_cursor CURSOR FOR
SELECT Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId
FROM CertificationTypes
ORDER BY Id;

OPEN certtype_cursor;
FETCH NEXT FROM certtype_cursor INTO @CertTypeId, @CertTypeName, @AmazonCode, @CertTypeDescription, @CertTypeIsActive, @CertTypeSortOrder, @CertTypeLevel, @CertTypeParentId;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.CertificationTypes (Id, Name, AmazonCode, Description, IsActive, SortOrder, Level, ParentId) VALUES (' +
        CAST(@CertTypeId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CertTypeName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@AmazonCode, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(CAST(@CertTypeDescription AS NVARCHAR(MAX)), '''', '''''') + '''', 'NULL') + ', ' +
        CAST(CAST(@CertTypeIsActive AS INT) AS VARCHAR) + ', ' +
        CAST(@CertTypeSortOrder AS VARCHAR) + ', ' +
        CAST(@CertTypeLevel AS VARCHAR) + ', ' +
        ISNULL(CAST(@CertTypeParentId AS VARCHAR), 'NULL') + ');';

    PRINT @Sql;
    FETCH NEXT FROM certtype_cursor INTO @CertTypeId, @CertTypeName, @AmazonCode, @CertTypeDescription, @CertTypeIsActive, @CertTypeSortOrder, @CertTypeLevel, @CertTypeParentId;
END;

CLOSE certtype_cursor;
DEALLOCATE certtype_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.CertificationTypes OFF;';
PRINT 'GO';
PRINT '';

PRINT '-- ============================================';
PRINT '-- STEP 3: Insert data tables';
PRINT '-- ============================================';
PRINT '';

-- ============================================
-- COMPANIES
-- ============================================
PRINT '-- Companies';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
PRINT 'GO';

DECLARE @CompanyId INT, @CompanyName NVARCHAR(500), @ContactInfo NVARCHAR(500), @Location NVARCHAR(500);
DECLARE @Country NVARCHAR(200), @CompIsActive BIT, @CompIsDeleted BIT, @Website NVARCHAR(500);

DECLARE company_cursor CURSOR FOR
SELECT Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website
FROM Companies
ORDER BY Id;

OPEN company_cursor;
FETCH NEXT FROM company_cursor INTO @CompanyId, @CompanyName, @ContactInfo, @Location, @Country, @CompIsActive, @CompIsDeleted, @Website;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@CompanyId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CompanyName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@ContactInfo, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Location, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Country, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(CAST(@CompIsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@CompIsDeleted AS INT) AS VARCHAR) + ', ' +
        ISNULL('''' + REPLACE(@Website, '''', '''''') + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    PRINT @Sql;
    FETCH NEXT FROM company_cursor INTO @CompanyId, @CompanyName, @ContactInfo, @Location, @Country, @CompIsActive, @CompIsDeleted, @Website;
END;

CLOSE company_cursor;
DEALLOCATE company_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- PRODUCTS
-- ============================================
PRINT '-- Products';
PRINT 'SET IDENTITY_INSERT dbo.Products ON;';
PRINT 'GO';

DECLARE @ProductId INT, @ProductName NVARCHAR(500), @ProdDescription NVARCHAR(MAX);
DECLARE @ProdCategoryId INT, @ProductTypeId INT, @ProdCompanyId INT;
DECLARE @ProdIsActive BIT, @ProdIsDeleted BIT;

DECLARE product_cursor CURSOR FOR
SELECT Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted
FROM Products
ORDER BY Id;

OPEN product_cursor;
FETCH NEXT FROM product_cursor INTO @ProductId, @ProductName, @ProdDescription, @ProdCategoryId, @ProductTypeId, @ProdCompanyId, @ProdIsActive, @ProdIsDeleted;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = 'INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@ProductId AS VARCHAR) + ', ' +
        '''' + REPLACE(@ProductName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(CAST(@ProdDescription AS NVARCHAR(MAX)), '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL(CAST(@ProdCategoryId AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@ProductTypeId AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@ProdCompanyId AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@ProdIsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@ProdIsDeleted AS INT) AS VARCHAR) + ', ' +
        'GETDATE(), GETDATE());';

    PRINT @Sql;
    FETCH NEXT FROM product_cursor INTO @ProductId, @ProductName, @ProdDescription, @ProdCategoryId, @ProductTypeId, @ProdCompanyId, @ProdIsActive, @ProdIsDeleted;
END;

CLOSE product_cursor;
DEALLOCATE product_cursor;

PRINT 'GO';
PRINT 'SET IDENTITY_INSERT dbo.Products OFF;';
PRINT 'GO';
PRINT '';

-- ============================================
-- CERTIFICATES (Active only)
-- ============================================
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

-- ============================================
-- CERTIFICATE PRODUCT IDs
-- ============================================
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
PRINT 'SELECT ''ProductCategories'' as Entity, COUNT(*) as Count FROM ProductCategories';
PRINT 'UNION ALL SELECT ''ProductTypes'', COUNT(*) FROM ProductTypes';
PRINT 'UNION ALL SELECT ''CertificationTypes'', COUNT(*) FROM CertificationTypes';
PRINT 'UNION ALL SELECT ''Companies'', COUNT(*) FROM Companies';
PRINT 'UNION ALL SELECT ''Products'', COUNT(*) FROM Products';
PRINT 'UNION ALL SELECT ''Certificates'', COUNT(*) FROM Certificates';
PRINT 'UNION ALL SELECT ''ProductIDs'', COUNT(*) FROM CertificateProductIds;';
PRINT 'GO';

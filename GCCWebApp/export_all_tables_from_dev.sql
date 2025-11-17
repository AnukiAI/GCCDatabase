-- ============================================
-- COMPLETE DATABASE EXPORT - Dev to Production
-- ============================================
-- Exports ALL tables in correct dependency order
-- Run on: ANUKI01 / GCCDatabase_Dev
-- ============================================

USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

PRINT '-- ============================================';
PRINT '-- GCC DATABASE COMPLETE EXPORT';
PRINT '-- Generated: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '-- Source: ANUKI01 / GCCDatabase_Dev';
PRINT '-- Target: Production / gccwebap_certificates';
PRINT '-- ============================================';
PRINT '';

-- ============================================
-- SECTION 1: LOOKUP TABLES (No dependencies)
-- ============================================

-- ProductCategories
PRINT '-- ============================================';
PRINT '-- PRODUCT CATEGORIES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.ProductCategories ON;';
PRINT '';

DECLARE @CatId INT, @CatName NVARCHAR(200), @CatIsActive BIT;
DECLARE cat_cursor CURSOR FOR SELECT Id, Name, IsActive FROM ProductCategories ORDER BY Id;
OPEN cat_cursor;
FETCH NEXT FROM cat_cursor INTO @CatId, @CatName, @CatIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.ProductCategories (Id, Name, IsActive) VALUES (' +
        CAST(@CatId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CatName, '''', '''''') + ''', ' +
        CAST(CAST(@CatIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM cat_cursor INTO @CatId, @CatName, @CatIsActive;
END;
CLOSE cat_cursor;
DEALLOCATE cat_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.ProductCategories OFF;';
PRINT '';

-- ProductTypes
PRINT '-- ============================================';
PRINT '-- PRODUCT TYPES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.ProductTypes ON;';
PRINT '';

DECLARE @TypeId INT, @TypeName NVARCHAR(200), @TypeIsActive BIT;
DECLARE type_cursor CURSOR FOR SELECT Id, Name, IsActive FROM ProductTypes ORDER BY Id;
OPEN type_cursor;
FETCH NEXT FROM type_cursor INTO @TypeId, @TypeName, @TypeIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.ProductTypes (Id, Name, IsActive) VALUES (' +
        CAST(@TypeId AS VARCHAR) + ', ' +
        '''' + REPLACE(@TypeName, '''', '''''') + ''', ' +
        CAST(CAST(@TypeIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM type_cursor INTO @TypeId, @TypeName, @TypeIsActive;
END;
CLOSE type_cursor;
DEALLOCATE type_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.ProductTypes OFF;';
PRINT '';

-- CertificationTypes
PRINT '-- ============================================';
PRINT '-- CERTIFICATION TYPES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.CertificationTypes ON;';
PRINT '';

DECLARE @CertTypeId INT, @CertTypeName NVARCHAR(200), @CertTypeDesc NVARCHAR(MAX), @CertTypeIsActive BIT;
DECLARE certtype_cursor CURSOR FOR SELECT Id, Name, Description, IsActive FROM CertificationTypes ORDER BY Id;
OPEN certtype_cursor;
FETCH NEXT FROM certtype_cursor INTO @CertTypeId, @CertTypeName, @CertTypeDesc, @CertTypeIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.CertificationTypes (Id, Name, Description, IsActive) VALUES (' +
        CAST(@CertTypeId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CertTypeName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@CertTypeDesc, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(CAST(@CertTypeIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM certtype_cursor INTO @CertTypeId, @CertTypeName, @CertTypeDesc, @CertTypeIsActive;
END;
CLOSE certtype_cursor;
DEALLOCATE certtype_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.CertificationTypes OFF;';
PRINT '';

-- CertificateFields
PRINT '-- ============================================';
PRINT '-- CERTIFICATE FIELDS';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.CertificateFields ON;';
PRINT '';

DECLARE @FieldId INT, @FieldName NVARCHAR(200), @FieldIsActive BIT;
DECLARE field_cursor CURSOR FOR SELECT Id, Name, IsActive FROM CertificateFields ORDER BY Id;
OPEN field_cursor;
FETCH NEXT FROM field_cursor INTO @FieldId, @FieldName, @FieldIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.CertificateFields (Id, Name, IsActive) VALUES (' +
        CAST(@FieldId AS VARCHAR) + ', ' +
        '''' + REPLACE(@FieldName, '''', '''''') + ''', ' +
        CAST(CAST(@FieldIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM field_cursor INTO @FieldId, @FieldName, @FieldIsActive;
END;
CLOSE field_cursor;
DEALLOCATE field_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.CertificateFields OFF;';
PRINT '';

-- Partners
PRINT '-- ============================================';
PRINT '-- PARTNERS';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Partners ON;';
PRINT '';

DECLARE @PartnerId INT, @PartnerName NVARCHAR(200), @PartnerIsActive BIT;
DECLARE partner_cursor CURSOR FOR SELECT Id, Name, IsActive FROM Partners ORDER BY Id;
OPEN partner_cursor;
FETCH NEXT FROM partner_cursor INTO @PartnerId, @PartnerName, @PartnerIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Partners (Id, Name, IsActive) VALUES (' +
        CAST(@PartnerId AS VARCHAR) + ', ' +
        '''' + REPLACE(@PartnerName, '''', '''''') + ''', ' +
        CAST(CAST(@PartnerIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM partner_cursor INTO @PartnerId, @PartnerName, @PartnerIsActive;
END;
CLOSE partner_cursor;
DEALLOCATE partner_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Partners OFF;';
PRINT '';

-- Tags
PRINT '-- ============================================';
PRINT '-- TAGS';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Tags ON;';
PRINT '';

DECLARE @TagId INT, @TagName NVARCHAR(100), @TagIsInternal BIT, @TagIsActive BIT;
DECLARE tag_cursor CURSOR FOR SELECT Id, Name, IsInternal, IsActive FROM Tags ORDER BY Id;
OPEN tag_cursor;
FETCH NEXT FROM tag_cursor INTO @TagId, @TagName, @TagIsInternal, @TagIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Tags (Id, Name, IsInternal, IsActive) VALUES (' +
        CAST(@TagId AS VARCHAR) + ', ' +
        '''' + REPLACE(@TagName, '''', '''''') + ''', ' +
        CAST(CAST(@TagIsInternal AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@TagIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM tag_cursor INTO @TagId, @TagName, @TagIsInternal, @TagIsActive;
END;
CLOSE tag_cursor;
DEALLOCATE tag_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Tags OFF;';
PRINT '';

-- ============================================
-- SECTION 2: COMPANIES (Referenced by Products)
-- ============================================

PRINT '-- ============================================';
PRINT '-- COMPANIES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Companies ON;';
PRINT '';

DECLARE @CompId INT, @CompName NVARCHAR(500), @ContactInfo NVARCHAR(500), @Location NVARCHAR(200);
DECLARE @Country NVARCHAR(100), @IsActive BIT, @IsDeleted BIT, @Website NVARCHAR(500);

DECLARE company_cursor CURSOR FOR
SELECT Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website
FROM Companies
ORDER BY Id;

OPEN company_cursor;
FETCH NEXT FROM company_cursor INTO @CompId, @CompName, @ContactInfo, @Location, @Country, @IsActive, @IsDeleted, @Website;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@CompId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CompName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@ContactInfo, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Location, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@Country, '''', '''''') + '''', 'NULL') + ', ' +
        CAST(CAST(@IsActive AS INT) AS VARCHAR) + ', ' +
        CAST(CAST(@IsDeleted AS INT) AS VARCHAR) + ', ' +
        ISNULL('''' + REPLACE(@Website, '''', '''''') + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    FETCH NEXT FROM company_cursor INTO @CompId, @CompName, @ContactInfo, @Location, @Country, @IsActive, @IsDeleted, @Website;
END;

CLOSE company_cursor;
DEALLOCATE company_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Companies OFF;';
PRINT '';

-- ============================================
-- SECTION 3: PRODUCTS (Referenced by Certificates)
-- ============================================

PRINT '-- ============================================';
PRINT '-- PRODUCTS';
PRINT '-- ============================================';
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

-- ============================================
-- SECTION 4: CERTIFICATES (Main data)
-- ============================================

PRINT '-- ============================================';
PRINT '-- CERTIFICATES (Active Only)';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
PRINT '';

DECLARE @CertId INT, @Number NVARCHAR(100), @Status NVARCHAR(50), @EffectiveDate DATETIME, @ExpirationDate DATETIME;
DECLARE @CertLocation NVARCHAR(200), @CertCountry NVARCHAR(100), @CertCompanyId INT, @CertProductId INT, @CertificationTypeId INT;
DECLARE @Partner NVARCHAR(100), @Asin NVARCHAR(100), @Upc NVARCHAR(100), @Gtin NVARCHAR(100), @Ean NVARCHAR(100);

DECLARE cert_cursor CURSOR FOR
SELECT Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;

OPEN cert_cursor;
FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate, @CertLocation, @CertCountry, @CertCompanyId, @CertProductId, @CertificationTypeId, @Partner, @Asin, @Upc, @Gtin, @Ean;

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
        CAST(@CertProductId AS VARCHAR) + ', ' +
        CAST(@CertificationTypeId AS VARCHAR) + ', ' +
        ISNULL('''' + @Partner + '''', 'NULL') + ', ' +
        ISNULL('''' + @Asin + '''', 'NULL') + ', ' +
        ISNULL('''' + @Upc + '''', 'NULL') + ', ' +
        ISNULL('''' + @Gtin + '''', 'NULL') + ', ' +
        ISNULL('''' + @Ean + '''', 'NULL') + ', ' +
        'GETDATE(), GETDATE());';

    FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate, @CertLocation, @CertCountry, @CertCompanyId, @CertProductId, @CertificationTypeId, @Partner, @Asin, @Upc, @Gtin, @Ean;
END;

CLOSE cert_cursor;
DEALLOCATE cert_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT '';

PRINT '-- ============================================';
PRINT '-- EXPORT COMPLETE';
PRINT '-- Tables exported:';
PRINT '--   - ProductCategories';
PRINT '--   - ProductTypes';
PRINT '--   - CertificationTypes';
PRINT '--   - CertificateFields';
PRINT '--   - Partners';
PRINT '--   - Tags';
PRINT '--   - Companies (28)';
PRINT '--   - Products (39)';
PRINT '--   - Certificates (80 Active)';
PRINT '-- ============================================';

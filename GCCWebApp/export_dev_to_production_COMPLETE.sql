-- ============================================
-- COMPLETE DATABASE EXPORT - Dev to Production v3
-- ============================================
-- Exports ALL business data for 100% dev/prod match
-- Run on: ANUKI01 / GCCDatabase_Dev
-- Target: 68.71.129.120,1533 / gccwebap_certificates
-- ============================================
-- EXCLUDES: Users, Roles, Permissions, ApiKeys (keep production separate)
-- INCLUDES: All certificate data, product IDs, custom fields, tags
-- ============================================

USE GCCDatabase_Dev;
GO

SET NOCOUNT ON;

PRINT '-- ============================================';
PRINT '-- GCC DATABASE COMPLETE EXPORT v3';
PRINT '-- Generated: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '-- Source: ANUKI01 / GCCDatabase_Dev';
PRINT '-- Target: Production / gccwebap_certificates';
PRINT '-- ============================================';
PRINT '';
PRINT '-- IMPORTANT: Run this on production AFTER clearing existing data:';
PRINT '-- DELETE FROM CertificateProductIds;';
PRINT '-- DELETE FROM CertificateTags;';
PRINT '-- DELETE FROM CustomFieldValues;';
PRINT '-- DELETE FROM Certificates;';
PRINT '-- DELETE FROM Products;';
PRINT '-- DELETE FROM ProductIdentifiers;';
PRINT '-- DELETE FROM ProductSubcategories;';
PRINT '-- DELETE FROM Companies;';
PRINT '-- DELETE FROM Tags;';
PRINT '-- ============================================';
PRINT '';

-- ============================================
-- SECTION 1: LOOKUP TABLES (No dependencies)
-- ============================================

-- ProductCategories
PRINT '-- ============================================';
PRINT '-- 1. PRODUCT CATEGORIES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.ProductCategories ON;';
PRINT '';

DECLARE @CatId INT, @CatName NVARCHAR(200), @CatDesc NVARCHAR(MAX), @CatSort INT, @CatIsActive BIT;
DECLARE cat_cursor CURSOR FOR
SELECT Id, Name, Description, SortOrder, IsActive
FROM ProductCategories
ORDER BY Id;

OPEN cat_cursor;
FETCH NEXT FROM cat_cursor INTO @CatId, @CatName, @CatDesc, @CatSort, @CatIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.ProductCategories (Id, Name, Description, SortOrder, IsActive) VALUES (' +
        CAST(@CatId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CatName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@CatDesc, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL(CAST(@CatSort AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@CatIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM cat_cursor INTO @CatId, @CatName, @CatDesc, @CatSort, @CatIsActive;
END;
CLOSE cat_cursor;
DEALLOCATE cat_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.ProductCategories OFF;';
PRINT '';

-- ProductTypes
PRINT '-- ============================================';
PRINT '-- 2. PRODUCT TYPES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.ProductTypes ON;';
PRINT '';

DECLARE @TypeId INT, @TypeName NVARCHAR(200), @TypeCatId INT, @TypeLevel INT, @TypeSort INT, @TypeIsActive BIT, @TypeParentId INT;
DECLARE type_cursor CURSOR FOR
SELECT Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId
FROM ProductTypes
ORDER BY Id;

OPEN type_cursor;
FETCH NEXT FROM type_cursor INTO @TypeId, @TypeName, @TypeCatId, @TypeLevel, @TypeSort, @TypeIsActive, @TypeParentId;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.ProductTypes (Id, Name, CategoryId, Level, SortOrder, IsActive, ParentId) VALUES (' +
        CAST(@TypeId AS VARCHAR) + ', ' +
        '''' + REPLACE(@TypeName, '''', '''''') + ''', ' +
        ISNULL(CAST(@TypeCatId AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@TypeLevel AS VARCHAR), 'NULL') + ', ' +
        ISNULL(CAST(@TypeSort AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@TypeIsActive AS INT) AS VARCHAR) + ', ' +
        ISNULL(CAST(@TypeParentId AS VARCHAR), 'NULL') + ');';
    FETCH NEXT FROM type_cursor INTO @TypeId, @TypeName, @TypeCatId, @TypeLevel, @TypeSort, @TypeIsActive, @TypeParentId;
END;
CLOSE type_cursor;
DEALLOCATE type_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.ProductTypes OFF;';
PRINT '';

-- ProductSubcategories
PRINT '-- ============================================';
PRINT '-- 3. PRODUCT SUBCATEGORIES';
PRINT '-- ============================================';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductSubcategories')
BEGIN
    PRINT 'SET IDENTITY_INSERT dbo.ProductSubcategories ON;';
    PRINT '';

    DECLARE @SubcatId INT, @SubcatName NVARCHAR(200), @SubcatCatId INT, @SubcatIsActive BIT;
    DECLARE subcat_cursor CURSOR FOR
    SELECT Id, Name, CategoryId, IsActive
    FROM ProductSubcategories
    ORDER BY Id;

    OPEN subcat_cursor;
    FETCH NEXT FROM subcat_cursor INTO @SubcatId, @SubcatName, @SubcatCatId, @SubcatIsActive;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'INSERT INTO dbo.ProductSubcategories (Id, Name, CategoryId, IsActive) VALUES (' +
            CAST(@SubcatId AS VARCHAR) + ', ' +
            '''' + REPLACE(@SubcatName, '''', '''''') + ''', ' +
            CAST(@SubcatCatId AS VARCHAR) + ', ' +
            CAST(CAST(@SubcatIsActive AS INT) AS VARCHAR) + ');';
        FETCH NEXT FROM subcat_cursor INTO @SubcatId, @SubcatName, @SubcatCatId, @SubcatIsActive;
    END;
    CLOSE subcat_cursor;
    DEALLOCATE subcat_cursor;

    PRINT '';
    PRINT 'SET IDENTITY_INSERT dbo.ProductSubcategories OFF;';
    PRINT '';
END

-- CertificationTypes
PRINT '-- ============================================';
PRINT '-- 4. CERTIFICATION TYPES';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.CertificationTypes ON;';
PRINT '';

DECLARE @CertTypeId INT, @CertTypeName NVARCHAR(200), @CertTypeCode NVARCHAR(50), @CertTypeDesc NVARCHAR(MAX), @CertTypeSort INT, @CertTypeIsActive BIT;
DECLARE certtype_cursor CURSOR FOR
SELECT Id, Name, AmazonCode, Description, SortOrder, IsActive
FROM CertificationTypes
ORDER BY Id;

OPEN certtype_cursor;
FETCH NEXT FROM certtype_cursor INTO @CertTypeId, @CertTypeName, @CertTypeCode, @CertTypeDesc, @CertTypeSort, @CertTypeIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.CertificationTypes (Id, Name, AmazonCode, Description, SortOrder, IsActive) VALUES (' +
        CAST(@CertTypeId AS VARCHAR) + ', ' +
        '''' + REPLACE(@CertTypeName, '''', '''''') + ''', ' +
        ISNULL('''' + @CertTypeCode + '''', 'NULL') + ', ' +
        ISNULL('''' + REPLACE(@CertTypeDesc, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL(CAST(@CertTypeSort AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@CertTypeIsActive AS INT) AS VARCHAR) + ');';
    FETCH NEXT FROM certtype_cursor INTO @CertTypeId, @CertTypeName, @CertTypeCode, @CertTypeDesc, @CertTypeSort, @CertTypeIsActive;
END;
CLOSE certtype_cursor;
DEALLOCATE certtype_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.CertificationTypes OFF;';
PRINT '';

-- Partners
PRINT '-- ============================================';
PRINT '-- 5. PARTNERS';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Partners ON;';
PRINT '';

DECLARE @PartnerId INT, @PartnerName NVARCHAR(200), @PartnerDesc NVARCHAR(MAX), @PartnerWeb NVARCHAR(500), @PartnerSort INT, @PartnerIsActive BIT;
DECLARE partner_cursor CURSOR FOR
SELECT Id, Name, Description, Website, SortOrder, IsActive
FROM Partners
ORDER BY Id;

OPEN partner_cursor;
FETCH NEXT FROM partner_cursor INTO @PartnerId, @PartnerName, @PartnerDesc, @PartnerWeb, @PartnerSort, @PartnerIsActive;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Partners (Id, Name, Description, Website, SortOrder, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (' +
        CAST(@PartnerId AS VARCHAR) + ', ' +
        '''' + REPLACE(@PartnerName, '''', '''''') + ''', ' +
        ISNULL('''' + REPLACE(@PartnerDesc, '''', '''''') + '''', 'NULL') + ', ' +
        ISNULL('''' + @PartnerWeb + '''', 'NULL') + ', ' +
        ISNULL(CAST(@PartnerSort AS VARCHAR), 'NULL') + ', ' +
        CAST(CAST(@PartnerIsActive AS INT) AS VARCHAR) + ', 0, GETDATE(), GETDATE());';
    FETCH NEXT FROM partner_cursor INTO @PartnerId, @PartnerName, @PartnerDesc, @PartnerWeb, @PartnerSort, @PartnerIsActive;
END;
CLOSE partner_cursor;
DEALLOCATE partner_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Partners OFF;';
PRINT '';

-- Tags
PRINT '-- ============================================';
PRINT '-- 6. TAGS';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Tags ON;';
PRINT '';

DECLARE @TagId INT, @TagName NVARCHAR(100), @TagIsInternal BIT, @TagIsActive BIT;
DECLARE tag_cursor CURSOR FOR
SELECT Id, Name, IsInternal, IsActive
FROM Tags
ORDER BY Id;

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

-- CustomFields
PRINT '-- ============================================';
PRINT '-- 7. CUSTOM FIELDS';
PRINT '-- ============================================';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomFields')
BEGIN
    PRINT 'SET IDENTITY_INSERT dbo.CustomFields ON;';
    PRINT '';

    DECLARE @CFId INT, @CFName NVARCHAR(200), @CFType NVARCHAR(50), @CFIsRequired BIT, @CFIsActive BIT, @CFCertTypeId INT;
    DECLARE cf_cursor CURSOR FOR
    SELECT Id, Name, FieldType, IsRequired, IsActive, CertificationTypeId
    FROM CustomFields
    ORDER BY Id;

    OPEN cf_cursor;
    FETCH NEXT FROM cf_cursor INTO @CFId, @CFName, @CFType, @CFIsRequired, @CFIsActive, @CFCertTypeId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'INSERT INTO dbo.CustomFields (Id, Name, FieldType, IsRequired, IsActive, CertificationTypeId) VALUES (' +
            CAST(@CFId AS VARCHAR) + ', ' +
            '''' + REPLACE(@CFName, '''', '''''') + ''', ' +
            '''' + @CFType + ''', ' +
            CAST(CAST(@CFIsRequired AS INT) AS VARCHAR) + ', ' +
            CAST(CAST(@CFIsActive AS INT) AS VARCHAR) + ', ' +
            ISNULL(CAST(@CFCertTypeId AS VARCHAR), 'NULL') + ');';
        FETCH NEXT FROM cf_cursor INTO @CFId, @CFName, @CFType, @CFIsRequired, @CFIsActive, @CFCertTypeId;
    END;
    CLOSE cf_cursor;
    DEALLOCATE cf_cursor;

    PRINT '';
    PRINT 'SET IDENTITY_INSERT dbo.CustomFields OFF;';
    PRINT '';
END

-- ============================================
-- SECTION 2: COMPANIES
-- ============================================

PRINT '-- ============================================';
PRINT '-- 8. COMPANIES';
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
-- SECTION 3: PRODUCTS
-- ============================================

PRINT '-- ============================================';
PRINT '-- 9. PRODUCTS';
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
        ISNULL(CAST(@CategoryId AS VARCHAR), 'NULL') + ', ' +
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

-- ProductIdentifiers
PRINT '-- ============================================';
PRINT '-- 10. PRODUCT IDENTIFIERS';
PRINT '-- ============================================';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductIdentifiers')
BEGIN
    PRINT 'SET IDENTITY_INSERT dbo.ProductIdentifiers ON;';
    PRINT '';

    DECLARE @ProdIdId INT, @ProdIdProdId INT, @ProdIdType NVARCHAR(50), @ProdIdValue NVARCHAR(100);
    DECLARE prodid_cursor CURSOR FOR
    SELECT Id, ProductId, IdentifierType, IdentifierValue
    FROM ProductIdentifiers
    ORDER BY Id;

    OPEN prodid_cursor;
    FETCH NEXT FROM prodid_cursor INTO @ProdIdId, @ProdIdProdId, @ProdIdType, @ProdIdValue;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'INSERT INTO dbo.ProductIdentifiers (Id, ProductId, IdentifierType, IdentifierValue) VALUES (' +
            CAST(@ProdIdId AS VARCHAR) + ', ' +
            CAST(@ProdIdProdId AS VARCHAR) + ', ' +
            '''' + @ProdIdType + ''', ' +
            '''' + REPLACE(@ProdIdValue, '''', '''''') + ''');';
        FETCH NEXT FROM prodid_cursor INTO @ProdIdId, @ProdIdProdId, @ProdIdType, @ProdIdValue;
    END;
    CLOSE prodid_cursor;
    DEALLOCATE prodid_cursor;

    PRINT '';
    PRINT 'SET IDENTITY_INSERT dbo.ProductIdentifiers OFF;';
    PRINT '';
END

-- ============================================
-- SECTION 4: CERTIFICATES
-- ============================================

PRINT '-- ============================================';
PRINT '-- 11. CERTIFICATES (Active Only)';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.Certificates ON;';
PRINT '';

DECLARE @CertId INT, @Number NVARCHAR(100), @Status NVARCHAR(50), @EffectiveDate DATETIME, @ExpirationDate DATETIME;
DECLARE @CertLocation NVARCHAR(200), @CertCountry NVARCHAR(100), @CertCompanyId INT, @CertProductId INT, @CertificationTypeId INT;
DECLARE @Partner NVARCHAR(100), @IsPublic BIT;

DECLARE cert_cursor CURSOR FOR
SELECT Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, IsPublic
FROM Certificates
WHERE Status = 'Active'
ORDER BY Id;

OPEN cert_cursor;
FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate, @CertLocation, @CertCountry, @CertCompanyId, @CertProductId, @CertificationTypeId, @Partner, @IsPublic;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, IsPublic, CreatedAt, UpdatedAt) VALUES (' +
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
        CAST(CAST(@IsPublic AS INT) AS VARCHAR) + ', ' +
        'GETDATE(), GETDATE());';

    FETCH NEXT FROM cert_cursor INTO @CertId, @Number, @Status, @EffectiveDate, @ExpirationDate, @CertLocation, @CertCountry, @CertCompanyId, @CertProductId, @CertificationTypeId, @Partner, @IsPublic;
END;

CLOSE cert_cursor;
DEALLOCATE cert_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.Certificates OFF;';
PRINT '';

-- ============================================
-- SECTION 5: CERTIFICATE PRODUCT IDs (CRITICAL!)
-- ============================================

PRINT '-- ============================================';
PRINT '-- 12. CERTIFICATE PRODUCT IDs (ASINs, UPCs, etc.)';
PRINT '-- ============================================';
PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds ON;';
PRINT '';

DECLARE @CertProdId INT, @CertProdCertId INT, @CertProdType NVARCHAR(50), @CertProdValue NVARCHAR(200);
DECLARE certprod_cursor CURSOR FOR
SELECT Id, CertificateId, ProductIdType, ProductIdValue
FROM CertificateProductIds
WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
ORDER BY Id;

OPEN certprod_cursor;
FETCH NEXT FROM certprod_cursor INTO @CertProdId, @CertProdCertId, @CertProdType, @CertProdValue;
WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'INSERT INTO dbo.CertificateProductIds (Id, CertificateId, ProductIdType, ProductIdValue) VALUES (' +
        CAST(@CertProdId AS VARCHAR) + ', ' +
        CAST(@CertProdCertId AS VARCHAR) + ', ' +
        '''' + @CertProdType + ''', ' +
        '''' + REPLACE(@CertProdValue, '''', '''''') + ''');';
    FETCH NEXT FROM certprod_cursor INTO @CertProdId, @CertProdCertId, @CertProdType, @CertProdValue;
END;
CLOSE certprod_cursor;
DEALLOCATE certprod_cursor;

PRINT '';
PRINT 'SET IDENTITY_INSERT dbo.CertificateProductIds OFF;';
PRINT '';

-- ============================================
-- SECTION 6: CERTIFICATE TAGS (Join Table)
-- ============================================

PRINT '-- ============================================';
PRINT '-- 13. CERTIFICATE TAGS (Associations)';
PRINT '-- ============================================';
PRINT '';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateTags')
BEGIN
    DECLARE @CertTagCertId INT, @CertTagTagId INT;
    DECLARE certtag_cursor CURSOR FOR
    SELECT CertificateId, TagId
    FROM CertificateTags
    WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
    ORDER BY CertificateId, TagId;

    OPEN certtag_cursor;
    FETCH NEXT FROM certtag_cursor INTO @CertTagCertId, @CertTagTagId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'INSERT INTO dbo.CertificateTags (CertificateId, TagId) VALUES (' +
            CAST(@CertTagCertId AS VARCHAR) + ', ' +
            CAST(@CertTagTagId AS VARCHAR) + ');';
        FETCH NEXT FROM certtag_cursor INTO @CertTagCertId, @CertTagTagId;
    END;
    CLOSE certtag_cursor;
    DEALLOCATE certtag_cursor;
END
PRINT '';

-- ============================================
-- SECTION 7: CUSTOM FIELD VALUES
-- ============================================

PRINT '-- ============================================';
PRINT '-- 14. CUSTOM FIELD VALUES';
PRINT '-- ============================================';
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomFieldValues')
BEGIN
    PRINT 'SET IDENTITY_INSERT dbo.CustomFieldValues ON;';
    PRINT '';

    DECLARE @CFVId INT, @CFVCertId INT, @CFVFieldId INT, @CFVValue NVARCHAR(MAX);
    DECLARE cfv_cursor CURSOR FOR
    SELECT Id, CertificateId, CustomFieldId, Value
    FROM CustomFieldValues
    WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Status = 'Active')
    ORDER BY Id;

    OPEN cfv_cursor;
    FETCH NEXT FROM cfv_cursor INTO @CFVId, @CFVCertId, @CFVFieldId, @CFVValue;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'INSERT INTO dbo.CustomFieldValues (Id, CertificateId, CustomFieldId, Value) VALUES (' +
            CAST(@CFVId AS VARCHAR) + ', ' +
            CAST(@CFVCertId AS VARCHAR) + ', ' +
            CAST(@CFVFieldId AS VARCHAR) + ', ' +
            ISNULL('''' + REPLACE(@CFVValue, '''', '''''') + '''', 'NULL') + ');';
        FETCH NEXT FROM cfv_cursor INTO @CFVId, @CFVCertId, @CFVFieldId, @CFVValue;
    END;
    CLOSE cfv_cursor;
    DEALLOCATE cfv_cursor;

    PRINT '';
    PRINT 'SET IDENTITY_INSERT dbo.CustomFieldValues OFF;';
    PRINT '';
END

-- ============================================
-- SUMMARY
-- ============================================

PRINT '-- ============================================';
PRINT '-- EXPORT COMPLETE - Summary:';
PRINT '-- ============================================';
PRINT '';

-- Count records
DECLARE @CompanyCount INT, @ProductCount INT, @CertCount INT, @ProdIdCount INT, @TagCount INT;
DECLARE @CatCount INT, @TypeCount INT, @CertTypeCount INT, @PartnerCount INT;

SELECT @CompanyCount = COUNT(*) FROM Companies;
SELECT @ProductCount = COUNT(*) FROM Products;
SELECT @CertCount = COUNT(*) FROM Certificates WHERE Status = 'Active';
SELECT @TagCount = COUNT(*) FROM Tags;
SELECT @CatCount = COUNT(*) FROM ProductCategories;
SELECT @TypeCount = COUNT(*) FROM ProductTypes;
SELECT @CertTypeCount = COUNT(*) FROM CertificationTypes;
SELECT @PartnerCount = COUNT(*) FROM Partners;

-- Count product IDs for active certificates only
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CertificateProductIds')
    SELECT @ProdIdCount = COUNT(*)
    FROM CertificateProductIds cpi
    INNER JOIN Certificates c ON cpi.CertificateId = c.Id
    WHERE c.Status = 'Active';
ELSE
    SET @ProdIdCount = 0;

PRINT '-- Lookup Tables:';
PRINT '--   ProductCategories: ' + CAST(@CatCount AS VARCHAR);
PRINT '--   ProductTypes: ' + CAST(@TypeCount AS VARCHAR);
PRINT '--   CertificationTypes: ' + CAST(@CertTypeCount AS VARCHAR);
PRINT '--   Partners: ' + CAST(@PartnerCount AS VARCHAR);
PRINT '--   Tags: ' + CAST(@TagCount AS VARCHAR);
PRINT '';
PRINT '-- Business Data:';
PRINT '--   Companies: ' + CAST(@CompanyCount AS VARCHAR);
PRINT '--   Products: ' + CAST(@ProductCount AS VARCHAR);
PRINT '--   Certificates (Active): ' + CAST(@CertCount AS VARCHAR);
PRINT '--   Product IDs (ASINs/UPCs): ' + CAST(@ProdIdCount AS VARCHAR);
PRINT '';
PRINT '-- IMPORTANT: This exports ALL data needed for 100% dev/prod match';
PRINT '-- EXCLUDES: Users, Roles, Permissions, ApiKeys (security)';
PRINT '-- ============================================';

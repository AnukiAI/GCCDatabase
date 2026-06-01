-- DEF-226 reproduction setup. Idempotent: re-running cleans up first.
-- Tests the hypothesis that a cert tagged at CATEGORY-FK only (no Type-FK)
-- makes the parent-category row block delete while the child-Type row
-- (if any) reads 0 -- contradicting count vs delete behavior.
--
-- Setup:
--   - Create ProductCategory "DEF226-CatOnly" (no child types)
--   - Tag Cert Id=2 (26-0001) with ProductCategoryId only, ProductTypeId=NULL
--
-- Compare with qc170-setup.sql which tagged BOTH category AND type.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- Reset cert 2 first (in case prior run left it tagged)
    UPDATE Certificates
       SET ProductCategoryId = 1, ProductTypeId = NULL, ProductSubcategoryId = NULL
     WHERE Id = 2;

    -- Drop any existing test rows
    DELETE FROM ProductTypes WHERE Name IN ('DEF226-CatOnly');
    DELETE FROM ProductCategories WHERE Name = 'DEF226-CatOnly';

    DECLARE @CatId INT;
    INSERT INTO ProductCategories (Name, IsActive, SortOrder)
    VALUES ('DEF226-CatOnly', 1, 999);
    SET @CatId = SCOPE_IDENTITY();

    -- Tag cert 2 (26-0001) to this category ONLY -- no Type, no Subcategory
    UPDATE Certificates
       SET ProductCategoryId = @CatId, ProductTypeId = NULL, ProductSubcategoryId = NULL
     WHERE Id = 2;

    SELECT
        @CatId  AS DEF226CategoryId,
        (SELECT Number FROM Certificates WHERE Id = 2) AS TaggedCertNumber,
        (SELECT ProductCategoryId FROM Certificates WHERE Id = 2) AS CertProductCategoryId,
        (SELECT ProductTypeId FROM Certificates WHERE Id = 2) AS CertProductTypeId;

    COMMIT TRANSACTION;
    PRINT 'def226-setup OK';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

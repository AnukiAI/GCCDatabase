-- QC #170 reproduction setup. Idempotent: re-running cleans up first.
-- Creates ProductCategory "Carly Test" + ProductType "Carly Test Type" and
-- tags Certificate Id=2 (26-0001) at those rows.

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
    DELETE FROM ProductTypes WHERE Name IN ('Carly Test Type', 'Carly Test');
    DELETE FROM ProductCategories WHERE Name = 'Carly Test';

    DECLARE @CatId INT;
    INSERT INTO ProductCategories (Name, IsActive, SortOrder)
    VALUES ('Carly Test', 1, 999);
    SET @CatId = SCOPE_IDENTITY();

    DECLARE @TypeId INT;
    INSERT INTO ProductTypes (Name, CategoryId, ParentId, [Level], SortOrder, IsActive)
    VALUES ('Carly Test Type', @CatId, NULL, 1, 999, 1);
    SET @TypeId = SCOPE_IDENTITY();

    -- Tag cert 2 (26-0001) to the test rows
    UPDATE Certificates
       SET ProductCategoryId = @CatId, ProductTypeId = @TypeId, ProductSubcategoryId = NULL
     WHERE Id = 2;

    SELECT
        @CatId  AS CarlyTestCategoryId,
        @TypeId AS CarlyTestTypeId,
        (SELECT Number FROM Certificates WHERE Id = 2) AS TaggedCertNumber;

    COMMIT TRANSACTION;
    PRINT 'qc170-setup OK';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

-- DEF-226 cleanup. Restores cert 2 (26-0001) and removes test category.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE Certificates
       SET ProductCategoryId = 1, ProductTypeId = NULL, ProductSubcategoryId = NULL
     WHERE Id = 2;

    DELETE FROM ProductCategories WHERE Name = 'DEF226-CatOnly';

    COMMIT TRANSACTION;
    PRINT 'def226-cleanup OK';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

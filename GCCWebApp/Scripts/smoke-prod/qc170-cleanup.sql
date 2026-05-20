-- QC #170 reproduction cleanup. Restores cert 2 (26-0001) and removes the
-- Carly Test rows.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE Certificates
       SET ProductCategoryId = 1, ProductTypeId = NULL, ProductSubcategoryId = NULL
     WHERE Id = 2;

    DELETE FROM ProductTypes WHERE Name IN ('Carly Test Type', 'Carly Test');
    DELETE FROM ProductCategories WHERE Name = 'Carly Test';

    COMMIT TRANSACTION;
    PRINT 'qc170-cleanup OK';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

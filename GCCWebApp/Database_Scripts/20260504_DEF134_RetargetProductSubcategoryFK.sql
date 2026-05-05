-- DEF-134 — Retarget Product.ProductSubcategoryId FK from ProductSubcategories → ProductTypes.
-- Generated from EF migration 20260504204044_RetargetProductSubcategoryFkToProductTypes.
-- Safe to run on a DB that already has all ProductSubcategoryId values NULL (verify first).
--
-- PRE-FLIGHT CHECK (run this first on prod):
--   SELECT COUNT(*) AS NonNullSubcatRows FROM Products WHERE ProductSubcategoryId IS NOT NULL;
-- If 0 → safe to apply.
-- If >0 → those values point at ProductSubcategories.Id (legacy table). Either NULL them out
--         or map them to a matching ProductTypes child row before applying, or this migration
--         will fail when SQL Server validates the new FK.

BEGIN TRANSACTION;
GO

ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_ProductSubcategories_ProductSubcategoryId];
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ProductTypes_ProductSubcategoryId] FOREIGN KEY ([ProductSubcategoryId]) REFERENCES [ProductTypes] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504204044_RetargetProductSubcategoryFkToProductTypes', N'8.0.11');
GO

COMMIT;
GO

-- ============================================
-- DROP ALL TABLES - Safe Version
-- ============================================
-- Drops all foreign keys first, then tables
-- ============================================

PRINT '============================================';
PRINT 'Dropping All Tables (Safe Method)';
PRINT '============================================';
PRINT '';

BEGIN TRY
    -- Step 1: Drop all foreign key constraints
    PRINT 'Step 1: Dropping all foreign key constraints...';

    DECLARE @sql NVARCHAR(MAX) = N'';

    SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
        + '.' + QUOTENAME(OBJECT_NAME(parent_object_id))
        + ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.foreign_keys;

    EXEC sp_executesql @sql;
    PRINT '✓ All foreign key constraints dropped';
    PRINT '';

    -- Step 2: Drop all tables
    PRINT 'Step 2: Dropping all tables...';

    SET @sql = N'';

    SELECT @sql += N'DROP TABLE ' + QUOTENAME(TABLE_SCHEMA) + '.'
        + QUOTENAME(TABLE_NAME) + ';' + CHAR(13)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_TYPE = 'BASE TABLE'
        AND TABLE_NAME != '__EFMigrationsHistory';

    EXEC sp_executesql @sql;
    PRINT '✓ All tables dropped';
    PRINT '';

    -- Step 3: Drop migrations history table last
    PRINT 'Step 3: Dropping migrations history...';
    IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL
        DROP TABLE dbo.__EFMigrationsHistory;
    PRINT '✓ Migrations history dropped';
    PRINT '';

    PRINT '============================================';
    PRINT 'SUCCESS: All tables dropped';
    PRINT '============================================';

END TRY
BEGIN CATCH
    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR DURING DROP';
    PRINT '============================================';
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT 'Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '============================================';
END CATCH;

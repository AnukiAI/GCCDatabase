-- Fix missing PartnerId column on Companies table
-- Production database: gccwebap_certificates

-- 1. Add PartnerId column to Companies (if not exists)
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Companies]')
    AND name = 'PartnerId'
)
BEGIN
    ALTER TABLE [Companies] ADD [PartnerId] int NULL;
    PRINT 'Added PartnerId column to Companies table';
END
ELSE
BEGIN
    PRINT 'PartnerId column already exists on Companies table';
END
GO

-- 2. Add index on PartnerId (if not exists)
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[Companies]')
    AND name = 'IX_Companies_PartnerId'
)
BEGIN
    CREATE INDEX [IX_Companies_PartnerId] ON [Companies] ([PartnerId]);
    PRINT 'Created IX_Companies_PartnerId index';
END
ELSE
BEGIN
    PRINT 'IX_Companies_PartnerId index already exists';
END
GO

-- 3. Add foreign key constraint (if not exists)
IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_Companies_Partners_PartnerId'
)
BEGIN
    ALTER TABLE [Companies] ADD CONSTRAINT [FK_Companies_Partners_PartnerId]
        FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE SET NULL;
    PRINT 'Created FK_Companies_Partners_PartnerId foreign key';
END
ELSE
BEGIN
    PRINT 'FK_Companies_Partners_PartnerId foreign key already exists';
END
GO

-- 4. Update Certificate Number column length (if needed)
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Certificates'
    AND COLUMN_NAME = 'Number'
    AND CHARACTER_MAXIMUM_LENGTH < 50
)
BEGIN
    -- Drop and recreate the unique index
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_Number')
    BEGIN
        DROP INDEX [IX_Certificates_Number] ON [Certificates];
        PRINT 'Dropped IX_Certificates_Number index';
    END

    -- Alter the column
    ALTER TABLE [Certificates] ALTER COLUMN [Number] nvarchar(50) NOT NULL;
    PRINT 'Altered Certificates.Number to nvarchar(50)';

    -- Recreate the unique index
    CREATE UNIQUE INDEX [IX_Certificates_Number] ON [Certificates] ([Number]);
    PRINT 'Recreated IX_Certificates_Number index';
END
ELSE
BEGIN
    PRINT 'Certificates.Number already has sufficient length';
END
GO

-- 5. Record the migration in __EFMigrationsHistory (if not exists)
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260127002958_AddPartnerIdToCompanies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260127002958_AddPartnerIdToCompanies', N'8.0.11');
    PRINT 'Recorded migration in __EFMigrationsHistory';
END
ELSE
BEGIN
    PRINT 'Migration already recorded in __EFMigrationsHistory';
END
GO

PRINT 'Migration completed successfully!';
GO

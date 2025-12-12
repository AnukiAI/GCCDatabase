-- ============================================================
-- Migration: 20251208051905_AddImportBatchAndAuditImportBatchId
-- Date: December 8, 2025
-- Description: Adds ImportBatches table and ImportBatchId to AuditHistory
-- Apply to: Production (gccwebap_certificates on 68.71.129.120,1533)
-- ============================================================

-- First, check if migration has already been applied
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20251208051905_AddImportBatchAndAuditImportBatchId')
BEGIN
    PRINT 'Applying migration: 20251208051905_AddImportBatchAndAuditImportBatchId';

    -- Add ImportBatchId column to AuditHistory table
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AuditHistory]') AND name = 'ImportBatchId')
    BEGIN
        ALTER TABLE [dbo].[AuditHistory] ADD [ImportBatchId] uniqueidentifier NULL;
        PRINT 'Added ImportBatchId column to AuditHistory';
    END
    ELSE
    BEGIN
        PRINT 'ImportBatchId column already exists in AuditHistory';
    END

    -- Create ImportBatches table
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ImportBatches' AND schema_id = SCHEMA_ID('dbo'))
    BEGIN
        CREATE TABLE [dbo].[ImportBatches] (
            [Id] uniqueidentifier NOT NULL,
            [EntityType] nvarchar(100) NOT NULL,
            [FileName] nvarchar(255) NULL,
            [TotalRows] int NOT NULL,
            [NewCount] int NOT NULL,
            [ModifiedCount] int NOT NULL,
            [SkippedCount] int NOT NULL,
            [UnchangedCount] int NOT NULL,
            [ErrorCount] int NOT NULL,
            [Status] nvarchar(20) NOT NULL,
            [ImportedBy] int NOT NULL,
            [ImportedByUsername] nvarchar(255) NOT NULL,
            [CreatedAt] datetime2 NOT NULL,
            [CompletedAt] datetime2 NULL,
            [IpAddress] nvarchar(50) NULL,
            [ErrorMessage] nvarchar(max) NULL,
            CONSTRAINT [PK_ImportBatches] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_ImportBatches_Users_ImportedBy] FOREIGN KEY ([ImportedBy]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
        );
        PRINT 'Created ImportBatches table';
    END
    ELSE
    BEGIN
        PRINT 'ImportBatches table already exists';
    END

    -- Create index on ImportedBy
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ImportBatches_ImportedBy' AND object_id = OBJECT_ID(N'[dbo].[ImportBatches]'))
    BEGIN
        CREATE INDEX [IX_ImportBatches_ImportedBy] ON [dbo].[ImportBatches] ([ImportedBy]);
        PRINT 'Created index IX_ImportBatches_ImportedBy';
    END
    ELSE
    BEGIN
        PRINT 'Index IX_ImportBatches_ImportedBy already exists';
    END

    -- Record migration in EF migrations history
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251208051905_AddImportBatchAndAuditImportBatchId', N'9.0.0');

    PRINT 'Migration 20251208051905_AddImportBatchAndAuditImportBatchId applied successfully';
END
ELSE
BEGIN
    PRINT 'Migration 20251208051905_AddImportBatchAndAuditImportBatchId has already been applied';
END
GO

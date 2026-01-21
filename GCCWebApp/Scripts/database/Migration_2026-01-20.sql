-- ============================================================================
-- GCCWebApp Database Migration Script
-- Date: 2026-01-20
-- Migrations: 3 (December 2025 - January 2026)
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT 'Starting GCCWebApp Database Migration...';
PRINT '';

-- ============================================================================
-- Migration 1: AddImportBatchAndAuditImportBatchId (Dec 8, 2025)
-- Adds import batch tracking for CSV imports
-- ============================================================================

PRINT 'Migration 1: AddImportBatchAndAuditImportBatchId';

-- Check if ImportBatches table already exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportBatches')
BEGIN
    PRINT '  Creating ImportBatches table...';

    CREATE TABLE ImportBatches (
        Id uniqueidentifier NOT NULL,
        EntityType nvarchar(100) NOT NULL,
        FileName nvarchar(255) NULL,
        TotalRows int NOT NULL,
        NewCount int NOT NULL,
        ModifiedCount int NOT NULL,
        SkippedCount int NOT NULL,
        UnchangedCount int NOT NULL,
        ErrorCount int NOT NULL,
        Status nvarchar(20) NOT NULL,
        ImportedBy int NOT NULL,
        ImportedByUsername nvarchar(255) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        CompletedAt datetime2 NULL,
        IpAddress nvarchar(50) NULL,
        ErrorMessage nvarchar(max) NULL,
        CONSTRAINT PK_ImportBatches PRIMARY KEY (Id),
        CONSTRAINT FK_ImportBatches_Users_ImportedBy FOREIGN KEY (ImportedBy) REFERENCES Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ImportBatches_ImportedBy ON ImportBatches(ImportedBy);

    PRINT '  ImportBatches table created successfully.';
END
ELSE
BEGIN
    PRINT '  ImportBatches table already exists - skipping.';
END

-- Add ImportBatchId column to AuditHistory if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AuditHistory') AND name = 'ImportBatchId')
BEGIN
    PRINT '  Adding ImportBatchId column to AuditHistory...';
    ALTER TABLE AuditHistory ADD ImportBatchId uniqueidentifier NULL;
    PRINT '  ImportBatchId column added successfully.';
END
ELSE
BEGIN
    PRINT '  ImportBatchId column already exists - skipping.';
END

PRINT 'Migration 1 complete.';
PRINT '';

-- ============================================================================
-- Migration 2: AddCertificationTypeSubcategoryId (Dec 16, 2025)
-- Adds subcategory support for certificates
-- ============================================================================

PRINT 'Migration 2: AddCertificationTypeSubcategoryId';

-- Add CertificationTypeSubcategoryId column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Certificates') AND name = 'CertificationTypeSubcategoryId')
BEGIN
    PRINT '  Adding CertificationTypeSubcategoryId column to Certificates...';
    ALTER TABLE Certificates ADD CertificationTypeSubcategoryId int NULL;

    PRINT '  Creating index IX_Certificates_CertificationTypeSubcategoryId...';
    CREATE INDEX IX_Certificates_CertificationTypeSubcategoryId ON Certificates(CertificationTypeSubcategoryId);

    PRINT '  Adding foreign key constraint...';
    ALTER TABLE Certificates ADD CONSTRAINT FK_Certificates_CertificationTypes_CertificationTypeSubcategoryId
        FOREIGN KEY (CertificationTypeSubcategoryId) REFERENCES CertificationTypes(Id) ON DELETE NO ACTION;

    PRINT '  CertificationTypeSubcategoryId added successfully.';
END
ELSE
BEGIN
    PRINT '  CertificationTypeSubcategoryId column already exists - skipping.';
END

PRINT 'Migration 2 complete.';
PRINT '';

-- ============================================================================
-- Migration 3: AddRoleVisibilityToCustomFields (Dec 22, 2025)
-- Adds role-based visibility control for custom fields
-- ============================================================================

PRINT 'Migration 3: AddRoleVisibilityToCustomFields';

-- Add RoleVisibility column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomFields') AND name = 'RoleVisibility')
BEGIN
    PRINT '  Adding RoleVisibility column to CustomFields...';
    ALTER TABLE CustomFields ADD RoleVisibility nvarchar(20) NOT NULL CONSTRAINT DF_CustomFields_RoleVisibility DEFAULT '';
    PRINT '  RoleVisibility column added successfully.';
END
ELSE
BEGIN
    PRINT '  RoleVisibility column already exists - skipping.';
END

PRINT 'Migration 3 complete.';
PRINT '';

-- ============================================================================
-- Update EF Migrations History (if table exists)
-- ============================================================================

IF EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    PRINT 'Updating EF Migrations History...';

    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251208051905_AddImportBatchAndAuditImportBatchId')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251208051905_AddImportBatchAndAuditImportBatchId', '9.0.0');
        PRINT '  Added migration record: 20251208051905_AddImportBatchAndAuditImportBatchId';
    END

    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251216221555_AddCertificationTypeSubcategoryId')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251216221555_AddCertificationTypeSubcategoryId', '9.0.0');
        PRINT '  Added migration record: 20251216221555_AddCertificationTypeSubcategoryId';
    END

    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251222200135_AddRoleVisibilityToCustomFields')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251222200135_AddRoleVisibilityToCustomFields', '9.0.0');
        PRINT '  Added migration record: 20251222200135_AddRoleVisibilityToCustomFields';
    END
END

COMMIT TRANSACTION;

PRINT '';
PRINT '============================================================================';
PRINT 'All migrations completed successfully!';
PRINT '============================================================================';

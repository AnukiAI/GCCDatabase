-- ============================================
-- GCC Database Schema Creation Script
-- Generated from: 20251015162223_InitialCreate_SqlServer_Final
-- Purpose: Creates all database tables and indexes
-- Target: SQL Server 2019+
-- Database: gccwebap_certificates (Production) or GCCDatabase_Dev (Development)
-- ============================================
-- IMPORTANT: Run this script BEFORE running FULL_TEST_DATA_SEED.sql
-- ============================================

SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'GCC Database Schema Creation';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

-- ============================================
-- Cleanup: Drop tables from wrong schemas
-- ============================================
PRINT '0. Cleaning up tables in non-dbo schemas...';

DECLARE @SchemaCleanupSql NVARCHAR(MAX) = '';
DECLARE @DroppedCount INT = 0;

-- Find all GCC-related tables NOT in dbo schema
SELECT @SchemaCleanupSql = @SchemaCleanupSql +
    'DROP TABLE [' + s.name + '].[' + t.name + ']; ' + CHAR(13) + CHAR(10),
    @DroppedCount = @DroppedCount + 1
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name != 'dbo'
    AND t.name IN (
        'ApiKeys', 'CertificateAggregates', 'CertificationTypes', 'Companies',
        'ETLAlerts', 'ETLConfigurations', 'ETLDataQualityIssues', 'ETLLogEntries',
        'Partners', 'Permissions', 'ProductCategories', 'Roles', 'Tags', 'Users',
        'CustomFields', 'ProductTypes', 'PermissionRole', 'CompanyUser',
        'PartnerUser', 'RoleUser', 'ProductSubcategories',
        'Products', 'Certificates', 'ProductIdentifiers', 'ProductTag',
        'CertificateDocuments', 'CertificateProductIds', 'CertificateTag', 'CustomFieldValues'
    )
ORDER BY
    CASE t.name
        -- Drop in reverse dependency order (deepest dependencies first)
        WHEN 'CustomFieldValues' THEN 1
        WHEN 'CertificateTag' THEN 2
        WHEN 'CertificateProductIds' THEN 3
        WHEN 'CertificateDocuments' THEN 4
        WHEN 'ProductTag' THEN 5
        WHEN 'ProductIdentifiers' THEN 6
        WHEN 'Certificates' THEN 7
        WHEN 'Products' THEN 8
        WHEN 'ProductSubcategories' THEN 9
        WHEN 'RoleUser' THEN 10
        WHEN 'PartnerUser' THEN 11
        WHEN 'CompanyUser' THEN 12
        WHEN 'PermissionRole' THEN 13
        WHEN 'ProductTypes' THEN 14
        WHEN 'CustomFields' THEN 15
        ELSE 16
    END;

IF LEN(@SchemaCleanupSql) > 0
BEGIN
    PRINT '  Dropping ' + CAST(@DroppedCount AS VARCHAR) + ' tables from non-dbo schemas...';
    EXEC sp_executesql @SchemaCleanupSql;
    PRINT '  Cleanup complete!';
END
ELSE
    PRINT '  No tables found in non-dbo schemas';

PRINT '';

-- ============================================
-- Phase 1: Create Independent Tables (No Foreign Keys)
-- ============================================

PRINT '1. Creating independent tables...';

-- ApiKeys
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ApiKeys] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Key] NVARCHAR(500) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [LastUsedAt] DATETIME2 NULL,
        [ExpiresAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(50) NULL,
        [RateLimitPerMinute] INT NULL,
        [RateLimitPerHour] INT NULL,
        [RateLimitPerDay] INT NULL,
        [AllowedIPs] NVARCHAR(MAX) NULL,
        [AllowedOrigins] NVARCHAR(MAX) NULL,
        [Scopes] NVARCHAR(MAX) NULL,
        [KeyHash] NVARCHAR(450) NULL,
        [MaxConcurrentRequests] INT NULL,
        [RequireTLS] BIT NOT NULL DEFAULT 0,
        [LastSecurityIncident] DATETIME2 NULL,
        [SecurityViolationCount] INT NOT NULL DEFAULT 0,
        [IsTemporarilyBlocked] BIT NOT NULL DEFAULT 0,
        [BlockedUntil] DATETIME2 NULL,
        [UserAgent] NVARCHAR(MAX) NULL,
        [RateLimit] INT NULL,
        [SecurityViolations] INT NOT NULL DEFAULT 0,
        [LastViolationAt] DATETIME2 NULL,
        [LastViolationType] NVARCHAR(MAX) NULL,
        [BlockReason] NVARCHAR(MAX) NULL,
        [RequestCount] INT NOT NULL DEFAULT 0,
        [LastRotatedAt] DATETIME2 NULL,
        [Environment] NVARCHAR(MAX) NOT NULL DEFAULT 'Production',
        [UsageCostPerRequest] DECIMAL(18,2) NULL,
        CONSTRAINT [PK_ApiKeys] PRIMARY KEY ([Id])
    );
    PRINT '  - ApiKeys table created';
END
ELSE
    PRINT '  - ApiKeys table already exists';

-- CertificateAggregates
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificateAggregates' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CertificateAggregates] (
        [Period] NVARCHAR(450) NOT NULL,
        [PeriodDate] DATETIME2 NOT NULL,
        [TotalCertificates] INT NOT NULL DEFAULT 0,
        [ActiveCertificates] INT NOT NULL DEFAULT 0,
        [ExpiredCertificates] INT NOT NULL DEFAULT 0,
        [ExpiringThisMonth] INT NOT NULL DEFAULT 0,
        [CertificatesByType] TEXT NOT NULL,
        [CertificatesByCountry] TEXT NOT NULL,
        [CertificatesByCompany] TEXT NOT NULL,
        [AverageValidityPeriod] FLOAT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_CertificateAggregates] PRIMARY KEY ([Period], [PeriodDate])
    );
    PRINT '  - CertificateAggregates table created';
END
ELSE
    PRINT '  - CertificateAggregates table already exists';

-- CertificationTypes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificationTypes' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CertificationTypes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [AmazonCode] NVARCHAR(255) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [SortOrder] INT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_CertificationTypes] PRIMARY KEY ([Id])
    );
    PRINT '  - CertificationTypes table created';
END
ELSE
    PRINT '  - CertificationTypes table already exists';

-- Companies
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Companies' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Companies] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [ContactInfo] NVARCHAR(255) NULL,
        [Email] NVARCHAR(255) NULL,
        [Website] NVARCHAR(500) NULL,
        [Location] NVARCHAR(175) NULL,
        [Country] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
    PRINT '  - Companies table created';
END
ELSE
    PRINT '  - Companies table already exists';

-- ETLAlerts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ETLAlerts' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ETLAlerts] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [AlertType] NVARCHAR(MAX) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Severity] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [IsResolved] BIT NOT NULL DEFAULT 0,
        [ResolvedAt] DATETIME2 NULL,
        [Context] TEXT NOT NULL,
        CONSTRAINT [PK_ETLAlerts] PRIMARY KEY ([Id])
    );
    PRINT '  - ETLAlerts table created';
END
ELSE
    PRINT '  - ETLAlerts table already exists';

-- ETLConfigurations
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ETLConfigurations' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ETLConfigurations] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(450) NOT NULL,
        [JobType] INT NOT NULL,
        [IsIncremental] BIT NOT NULL DEFAULT 0,
        [LastRunTime] DATETIME2 NULL,
        [DataSources] NVARCHAR(MAX) NOT NULL,
        [Transformations] NVARCHAR(MAX) NOT NULL,
        [Destinations] NVARCHAR(MAX) NOT NULL,
        [Settings] TEXT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CronSchedule] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ETLConfigurations] PRIMARY KEY ([Id])
    );
    PRINT '  - ETLConfigurations table created';
END
ELSE
    PRINT '  - ETLConfigurations table already exists';

-- ETLDataQualityIssues
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ETLDataQualityIssues' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ETLDataQualityIssues] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [IssueType] NVARCHAR(450) NOT NULL,
        [Description] NVARCHAR(MAX) NOT NULL,
        [FieldName] NVARCHAR(MAX) NOT NULL,
        [RecordId] NVARCHAR(MAX) NULL,
        [ExpectedValue] TEXT NULL,
        [ActualValue] TEXT NULL,
        [Severity] INT NOT NULL,
        [DetectedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ETLDataQualityIssues] PRIMARY KEY ([Id])
    );
    PRINT '  - ETLDataQualityIssues table created';
END
ELSE
    PRINT '  - ETLDataQualityIssues table already exists';

-- ETLLogEntries
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ETLLogEntries' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ETLLogEntries] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [JobId] UNIQUEIDENTIFIER NOT NULL,
        [Timestamp] DATETIME2 NOT NULL,
        [LogLevel] NVARCHAR(MAX) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(MAX) NULL,
        [Properties] TEXT NOT NULL,
        CONSTRAINT [PK_ETLLogEntries] PRIMARY KEY ([Id])
    );
    PRINT '  - ETLLogEntries table created';
END
ELSE
    PRINT '  - ETLLogEntries table already exists';

-- Partners
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Partners' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Partners] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Website] NVARCHAR(500) NULL,
        [ContactInfo] NVARCHAR(255) NULL,
        [Email] NVARCHAR(255) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
    );
    PRINT '  - Partners table created';
END
ELSE
    PRINT '  - Partners table already exists';

-- Permissions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Permissions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Permissions] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Codename] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(100) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
    PRINT '  - Permissions table created';
END
ELSE
    PRINT '  - Permissions table already exists';

-- ProductCategories
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductCategories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProductCategories] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
    );
    PRINT '  - ProductCategories table created';
END
ELSE
    PRINT '  - ProductCategories table already exists';

-- Roles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsSystem] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
    PRINT '  - Roles table created';
END
ELSE
    PRINT '  - Roles table already exists';

-- Tags
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tags' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Tags] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [TagType] NVARCHAR(20) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
    PRINT '  - Tags table created';
END
ELSE
    PRINT '  - Tags table already exists';

-- Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Username] NVARCHAR(255) NOT NULL,
        [Email] NVARCHAR(255) NOT NULL,
        [Name] NVARCHAR(100) NULL,
        [FullName] NVARCHAR(255) NULL,
        [LastLoginAt] DATETIME2 NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [PasswordHash] NVARCHAR(255) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active',
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsStaff] BIT NOT NULL DEFAULT 0,
        [IsSuperuser] BIT NOT NULL DEFAULT 0,
        [RequirePasswordReset] BIT NOT NULL DEFAULT 0,
        [LastLogin] DATETIME2 NULL,
        [PasswordChangedAt] DATETIME2 NULL,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LockedUntil] DATETIME2 NULL,
        [TwoFactorSecret] NVARCHAR(MAX) NULL,
        [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
        [TwoFactorMethod] NVARCHAR(MAX) NOT NULL DEFAULT 'None',
        [PhoneNumber] NVARCHAR(MAX) NULL,
        [IsPhoneVerified] BIT NOT NULL DEFAULT 0,
        [IsEmailVerified] BIT NOT NULL DEFAULT 0,
        [RecoveryCodes] NVARCHAR(MAX) NULL,
        [LastSecurityAudit] DATETIME2 NULL,
        [SecurityQuestions] NVARCHAR(MAX) NULL,
        [MustChangePassword] BIT NOT NULL DEFAULT 0,
        [MaxConcurrentSessions] INT NOT NULL DEFAULT 5,
        [AllowedIPs] NVARCHAR(MAX) NULL,
        [RequirePasswordChange] BIT NOT NULL DEFAULT 0,
        [PasswordExpiresAt] DATETIME2 NULL,
        [PasswordHistory] NVARCHAR(MAX) NULL,
        [MaxPasswordHistory] INT NOT NULL DEFAULT 5,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
    PRINT '  - Users table created';
END
ELSE
    PRINT '  - Users table already exists';

PRINT '';
PRINT '2. Creating dependent tables (Level 1)...';

-- CustomFields (depends on CertificationTypes)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomFields' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CustomFields] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [FieldType] NVARCHAR(50) NOT NULL,
        [IsRequired] BIT NOT NULL DEFAULT 0,
        [IsVisible] BIT NOT NULL DEFAULT 1,
        [IsPublic] BIT NOT NULL DEFAULT 1,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [Options] NVARCHAR(MAX) NULL,
        [DefaultValue] NVARCHAR(MAX) NULL,
        [ValidationRules] NVARCHAR(MAX) NULL,
        [CertificateTypeId] INT NOT NULL,
        CONSTRAINT [PK_CustomFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomFields_CertificationTypes_CertificateTypeId]
            FOREIGN KEY ([CertificateTypeId]) REFERENCES [CertificationTypes]([Id]) ON DELETE CASCADE
    );
    PRINT '  - CustomFields table created';
END
ELSE
    PRINT '  - CustomFields table already exists';

-- ProductTypes (depends on ProductCategories)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductTypes' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProductTypes] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [CategoryId] INT NOT NULL,
        [ParentId] INT NULL,
        [Level] INT NOT NULL DEFAULT 0,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ProductTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductTypes_ProductCategories_CategoryId]
            FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductTypes_ProductTypes_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [ProductTypes]([Id])
    );
    PRINT '  - ProductTypes table created';
END
ELSE
    PRINT '  - ProductTypes table already exists';

-- PermissionRole (join table for Permissions and Roles)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PermissionRole' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[PermissionRole] (
        [PermissionsId] INT NOT NULL,
        [RolesId] INT NOT NULL,
        CONSTRAINT [PK_PermissionRole] PRIMARY KEY ([PermissionsId], [RolesId]),
        CONSTRAINT [FK_PermissionRole_Permissions_PermissionsId]
            FOREIGN KEY ([PermissionsId]) REFERENCES [Permissions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PermissionRole_Roles_RolesId]
            FOREIGN KEY ([RolesId]) REFERENCES [Roles]([Id]) ON DELETE CASCADE
    );
    PRINT '  - PermissionRole table created';
END
ELSE
    PRINT '  - PermissionRole table already exists';

-- CompanyUser (join table for Companies and Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanyUser' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CompanyUser] (
        [CompaniesId] INT NOT NULL,
        [UsersId] INT NOT NULL,
        CONSTRAINT [PK_CompanyUser] PRIMARY KEY ([CompaniesId], [UsersId]),
        CONSTRAINT [FK_CompanyUser_Companies_CompaniesId]
            FOREIGN KEY ([CompaniesId]) REFERENCES [Companies]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CompanyUser_Users_UsersId]
            FOREIGN KEY ([UsersId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );
    PRINT '  - CompanyUser table created';
END
ELSE
    PRINT '  - CompanyUser table already exists';

-- PartnerUser (join table for Partners and Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PartnerUser' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[PartnerUser] (
        [PartnersId] INT NOT NULL,
        [UsersId] INT NOT NULL,
        CONSTRAINT [PK_PartnerUser] PRIMARY KEY ([PartnersId], [UsersId]),
        CONSTRAINT [FK_PartnerUser_Partners_PartnersId]
            FOREIGN KEY ([PartnersId]) REFERENCES [Partners]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PartnerUser_Users_UsersId]
            FOREIGN KEY ([UsersId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );
    PRINT '  - PartnerUser table created';
END
ELSE
    PRINT '  - PartnerUser table already exists';

-- RoleUser (join table for Roles and Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleUser' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[RoleUser] (
        [RolesId] INT NOT NULL,
        [UsersId] INT NOT NULL,
        CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
        CONSTRAINT [FK_RoleUser_Roles_RolesId]
            FOREIGN KEY ([RolesId]) REFERENCES [Roles]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleUser_Users_UsersId]
            FOREIGN KEY ([UsersId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );
    PRINT '  - RoleUser table created';
END
ELSE
    PRINT '  - RoleUser table already exists';

-- ProductSubcategories (depends on ProductTypes)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductSubcategories' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProductSubcategories] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [ProductTypeId] INT NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ProductSubcategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductSubcategories_ProductTypes_ProductTypeId]
            FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes]([Id]) ON DELETE CASCADE
    );
    PRINT '  - ProductSubcategories table created';
END
ELSE
    PRINT '  - ProductSubcategories table already exists';

PRINT '';
PRINT '3. Creating dependent tables (Level 2)...';

-- Products (depends on Companies, ProductCategories, ProductTypes, ProductSubcategories)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Model] NVARCHAR(100) NULL,
        [Sku] NVARCHAR(50) NULL,
        [Upc] NVARCHAR(50) NULL,
        [Asin] NVARCHAR(50) NULL,
        [CategoryId] INT NOT NULL,
        [ProductTypeId] INT NULL,
        [ProductSubcategoryId] INT NULL,
        [CompanyId] INT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Companies_CompanyId]
            FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]),
        CONSTRAINT [FK_Products_ProductCategories_CategoryId]
            FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Products_ProductSubcategories_ProductSubcategoryId]
            FOREIGN KEY ([ProductSubcategoryId]) REFERENCES [ProductSubcategories]([Id]),
        CONSTRAINT [FK_Products_ProductTypes_ProductTypeId]
            FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes]([Id])
    );
    PRINT '  - Products table created';
END
ELSE
    PRINT '  - Products table already exists';

PRINT '';
PRINT '4. Creating dependent tables (Level 3)...';

-- Certificates (depends on Companies, Products, CertificationTypes, Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Certificates' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Certificates] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Number] NVARCHAR(7) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',
        [EffectiveDate] DATETIME2 NOT NULL,
        [ExpirationDate] DATETIME2 NOT NULL,
        [Location] NVARCHAR(175) NULL,
        [Country] NVARCHAR(100) NULL,
        [Asin] NVARCHAR(MAX) NULL,
        [Upc] NVARCHAR(MAX) NULL,
        [Gtin] NVARCHAR(MAX) NULL,
        [Ean] NVARCHAR(MAX) NULL,
        [CertificateFile] NVARCHAR(MAX) NULL,
        [CertificateImagePath] NVARCHAR(500) NULL,
        [Partner] NVARCHAR(100) NULL,
        [CompanyId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [CertificationTypeId] INT NOT NULL,
        [CreatedBy] INT NULL,
        [UpdatedBy] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Certificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Certificates_CertificationTypes_CertificationTypeId]
            FOREIGN KEY ([CertificationTypeId]) REFERENCES [CertificationTypes]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Certificates_Companies_CompanyId]
            FOREIGN KEY ([CompanyId]) REFERENCES [Companies]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Certificates_Products_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Certificates_Users_CreatedBy]
            FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id]),
        CONSTRAINT [FK_Certificates_Users_UpdatedBy]
            FOREIGN KEY ([UpdatedBy]) REFERENCES [Users]([Id])
    );
    PRINT '  - Certificates table created';
END
ELSE
    PRINT '  - Certificates table already exists';

-- ProductIdentifiers (depends on Products)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductIdentifiers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProductIdentifiers] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProductId] INT NOT NULL,
        [IdentifierType] NVARCHAR(20) NOT NULL,
        [IdentifierValue] NVARCHAR(50) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_ProductIdentifiers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductIdentifiers_Products_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE CASCADE
    );
    PRINT '  - ProductIdentifiers table created';
END
ELSE
    PRINT '  - ProductIdentifiers table already exists';

-- ProductTag (join table for Products and Tags)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductTag' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[ProductTag] (
        [ProductsId] INT NOT NULL,
        [TagsId] INT NOT NULL,
        CONSTRAINT [PK_ProductTag] PRIMARY KEY ([ProductsId], [TagsId]),
        CONSTRAINT [FK_ProductTag_Products_ProductsId]
            FOREIGN KEY ([ProductsId]) REFERENCES [Products]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductTag_Tags_TagsId]
            FOREIGN KEY ([TagsId]) REFERENCES [Tags]([Id]) ON DELETE CASCADE
    );
    PRINT '  - ProductTag table created';
END
ELSE
    PRINT '  - ProductTag table already exists';

PRINT '';
PRINT '5. Creating dependent tables (Level 4)...';

-- CertificateDocuments (depends on Certificates, Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificateDocuments' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CertificateDocuments] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [OriginalFileName] NVARCHAR(255) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [ContentType] NVARCHAR(100) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [FileHash] NVARCHAR(64) NULL,
        [DocumentType] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [Version] INT NOT NULL DEFAULT 1,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsPublic] BIT NOT NULL DEFAULT 1,
        [IsVirusScanned] BIT NOT NULL DEFAULT 0,
        [PassedVirusScan] BIT NOT NULL DEFAULT 0,
        [VirusScanDate] DATETIME2 NULL,
        [CertificateId] INT NOT NULL,
        [UploadedBy] INT NULL,
        [UploadedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_CertificateDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateDocuments_Certificates_CertificateId]
            FOREIGN KEY ([CertificateId]) REFERENCES [Certificates]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateDocuments_Users_UploadedBy]
            FOREIGN KEY ([UploadedBy]) REFERENCES [Users]([Id])
    );
    PRINT '  - CertificateDocuments table created';
END
ELSE
    PRINT '  - CertificateDocuments table already exists';

-- CertificateProductIds (depends on Certificates, Users)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificateProductIds' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CertificateProductIds] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CertificateId] INT NOT NULL,
        [ProductIdType] NVARCHAR(10) NOT NULL,
        [ProductIdValue] NVARCHAR(50) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active',
        [Partner] NVARCHAR(50) NULL,
        [ProductName] NVARCHAR(200) NULL,
        [ProductSku] NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsPublic] BIT NOT NULL DEFAULT 1,
        [DisplayOrder] INT NULL,
        [CreatedBy] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedBy] INT NULL,
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_CertificateProductIds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateProductIds_Certificates_CertificateId]
            FOREIGN KEY ([CertificateId]) REFERENCES [Certificates]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateProductIds_Users_CreatedBy]
            FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id]),
        CONSTRAINT [FK_CertificateProductIds_Users_UpdatedBy]
            FOREIGN KEY ([UpdatedBy]) REFERENCES [Users]([Id])
    );
    PRINT '  - CertificateProductIds table created';
END
ELSE
    PRINT '  - CertificateProductIds table already exists';

-- CertificateTag (join table for Certificates and Tags)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificateTag' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CertificateTag] (
        [CertificatesId] INT NOT NULL,
        [TagsId] INT NOT NULL,
        CONSTRAINT [PK_CertificateTag] PRIMARY KEY ([CertificatesId], [TagsId]),
        CONSTRAINT [FK_CertificateTag_Certificates_CertificatesId]
            FOREIGN KEY ([CertificatesId]) REFERENCES [Certificates]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateTag_Tags_TagsId]
            FOREIGN KEY ([TagsId]) REFERENCES [Tags]([Id]) ON DELETE CASCADE
    );
    PRINT '  - CertificateTag table created';
END
ELSE
    PRINT '  - CertificateTag table already exists';

-- CustomFieldValues (depends on Certificates, CustomFields)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomFieldValues' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[CustomFieldValues] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [CertificateId] INT NOT NULL,
        [FieldId] INT NOT NULL,
        [TextValue] NVARCHAR(MAX) NULL,
        [NumberValue] DECIMAL(18,4) NULL,
        [DateValue] DATETIME2 NULL,
        [BooleanValue] BIT NULL,
        CONSTRAINT [PK_CustomFieldValues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomFieldValues_Certificates_CertificateId]
            FOREIGN KEY ([CertificateId]) REFERENCES [Certificates]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomFieldValues_CustomFields_FieldId]
            FOREIGN KEY ([FieldId]) REFERENCES [CustomFields]([Id]) ON DELETE CASCADE
    );
    PRINT '  - CustomFieldValues table created';
END
ELSE
    PRINT '  - CustomFieldValues table already exists';

PRINT '';
PRINT '============================================';
PRINT '6. Creating indexes...';
PRINT '============================================';

-- ApiKeys Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_CreatedAt' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE INDEX [IX_ApiKeys_CreatedAt] ON [ApiKeys]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_CreatedBy' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE INDEX [IX_ApiKeys_CreatedBy] ON [ApiKeys]([CreatedBy]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_CreatedBy_IsActive' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE INDEX [IX_ApiKeys_CreatedBy_IsActive] ON [ApiKeys]([CreatedBy], [IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_ExpiresAt' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE INDEX [IX_ApiKeys_ExpiresAt] ON [ApiKeys]([ExpiresAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_IsActive' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE INDEX [IX_ApiKeys_IsActive] ON [ApiKeys]([IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_KeyHash' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE UNIQUE INDEX [IX_ApiKeys_KeyHash] ON [ApiKeys]([KeyHash]) WHERE [KeyHash] IS NOT NULL;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_KeyHash_IsActive_ExpiresAt' AND object_id = OBJECT_ID('dbo.ApiKeys'))
    CREATE INDEX [IX_ApiKeys_KeyHash_IsActive_ExpiresAt] ON [ApiKeys]([KeyHash], [IsActive], [ExpiresAt]);

-- CertificateAggregates Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateAggregates_PeriodDate' AND object_id = OBJECT_ID('dbo.CertificateAggregates'))
    CREATE INDEX [IX_CertificateAggregates_PeriodDate] ON [CertificateAggregates]([PeriodDate]);

-- CertificateDocuments Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateDocuments_CertificateId' AND object_id = OBJECT_ID('dbo.CertificateDocuments'))
    CREATE INDEX [IX_CertificateDocuments_CertificateId] ON [CertificateDocuments]([CertificateId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateDocuments_FileHash' AND object_id = OBJECT_ID('dbo.CertificateDocuments'))
    CREATE INDEX [IX_CertificateDocuments_FileHash] ON [CertificateDocuments]([FileHash]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateDocuments_UploadedBy' AND object_id = OBJECT_ID('dbo.CertificateDocuments'))
    CREATE INDEX [IX_CertificateDocuments_UploadedBy] ON [CertificateDocuments]([UploadedBy]);

-- CertificateProductIds Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_CertificateId' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_CertificateId] ON [CertificateProductIds]([CertificateId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_CertificateId_IsActive' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_CertificateId_IsActive] ON [CertificateProductIds]([CertificateId], [IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_CertificateId_ProductIdType' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_CertificateId_ProductIdType] ON [CertificateProductIds]([CertificateId], [ProductIdType]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_CreatedAt' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_CreatedAt] ON [CertificateProductIds]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_CreatedBy' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_CreatedBy] ON [CertificateProductIds]([CreatedBy]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_IsActive' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_IsActive] ON [CertificateProductIds]([IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_Partner' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_Partner] ON [CertificateProductIds]([Partner]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_Partner_Status_IsActive' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_Partner_Status_IsActive] ON [CertificateProductIds]([Partner], [Status], [IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_ProductIdType' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_ProductIdType] ON [CertificateProductIds]([ProductIdType]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_ProductIdType_ProductIdValue' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE UNIQUE INDEX [IX_CertificateProductIds_ProductIdType_ProductIdValue] ON [CertificateProductIds]([ProductIdType], [ProductIdValue]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_ProductIdValue' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_ProductIdValue] ON [CertificateProductIds]([ProductIdValue]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_Status' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_Status] ON [CertificateProductIds]([Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_Status_IsActive_CreatedAt' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_Status_IsActive_CreatedAt] ON [CertificateProductIds]([Status], [IsActive], [CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateProductIds_UpdatedBy' AND object_id = OBJECT_ID('dbo.CertificateProductIds'))
    CREATE INDEX [IX_CertificateProductIds_UpdatedBy] ON [CertificateProductIds]([UpdatedBy]);

-- Certificates Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_CertificationTypeId' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_CertificationTypeId] ON [Certificates]([CertificationTypeId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_CertificationTypeId_Status' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_CertificationTypeId_Status] ON [Certificates]([CertificationTypeId], [Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_CompanyId' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_CompanyId] ON [Certificates]([CompanyId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_CompanyId_Status' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_CompanyId_Status] ON [Certificates]([CompanyId], [Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_Country' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_Country] ON [Certificates]([Country]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_Country_Status' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_Country_Status] ON [Certificates]([Country], [Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_CreatedAt' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_CreatedAt] ON [Certificates]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_CreatedBy' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_CreatedBy] ON [Certificates]([CreatedBy]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_EffectiveDate' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_EffectiveDate] ON [Certificates]([EffectiveDate]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_ExpirationDate' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_ExpirationDate] ON [Certificates]([ExpirationDate]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_ExpirationDate_Status_CompanyId' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_ExpirationDate_Status_CompanyId] ON [Certificates]([ExpirationDate], [Status], [CompanyId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_Number' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE UNIQUE INDEX [IX_Certificates_Number] ON [Certificates]([Number]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_ProductId' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_ProductId] ON [Certificates]([ProductId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_ProductId_Status' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_ProductId_Status] ON [Certificates]([ProductId], [Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_Status' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_Status] ON [Certificates]([Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_Status_ExpirationDate' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_Status_ExpirationDate] ON [Certificates]([Status], [ExpirationDate]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Certificates_UpdatedBy' AND object_id = OBJECT_ID('dbo.Certificates'))
    CREATE INDEX [IX_Certificates_UpdatedBy] ON [Certificates]([UpdatedBy]);

-- CertificateTag Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificateTag_TagsId' AND object_id = OBJECT_ID('dbo.CertificateTag'))
    CREATE INDEX [IX_CertificateTag_TagsId] ON [CertificateTag]([TagsId]);

-- CertificationTypes Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CertificationTypes_Name' AND object_id = OBJECT_ID('dbo.CertificationTypes'))
    CREATE UNIQUE INDEX [IX_CertificationTypes_Name] ON [CertificationTypes]([Name]);

-- Companies Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_Country' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE INDEX [IX_Companies_Country] ON [Companies]([Country]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_Country_IsActive' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE INDEX [IX_Companies_Country_IsActive] ON [Companies]([Country], [IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_CreatedAt' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE INDEX [IX_Companies_CreatedAt] ON [Companies]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_IsActive' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE INDEX [IX_Companies_IsActive] ON [Companies]([IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_Name' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE UNIQUE INDEX [IX_Companies_Name] ON [Companies]([Name]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_Name_Country' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE INDEX [IX_Companies_Name_Country] ON [Companies]([Name], [Country]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Companies_Website' AND object_id = OBJECT_ID('dbo.Companies'))
    CREATE INDEX [IX_Companies_Website] ON [Companies]([Website]);

-- CompanyUser Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CompanyUser_UsersId' AND object_id = OBJECT_ID('dbo.CompanyUser'))
    CREATE INDEX [IX_CompanyUser_UsersId] ON [CompanyUser]([UsersId]);

-- CustomFields Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomFields_CertificateTypeId' AND object_id = OBJECT_ID('dbo.CustomFields'))
    CREATE INDEX [IX_CustomFields_CertificateTypeId] ON [CustomFields]([CertificateTypeId]);

-- CustomFieldValues Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomFieldValues_CertificateId' AND object_id = OBJECT_ID('dbo.CustomFieldValues'))
    CREATE INDEX [IX_CustomFieldValues_CertificateId] ON [CustomFieldValues]([CertificateId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomFieldValues_FieldId' AND object_id = OBJECT_ID('dbo.CustomFieldValues'))
    CREATE INDEX [IX_CustomFieldValues_FieldId] ON [CustomFieldValues]([FieldId]);

-- ETLAlerts Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLAlerts_CreatedAt' AND object_id = OBJECT_ID('dbo.ETLAlerts'))
    CREATE INDEX [IX_ETLAlerts_CreatedAt] ON [ETLAlerts]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLAlerts_IsResolved' AND object_id = OBJECT_ID('dbo.ETLAlerts'))
    CREATE INDEX [IX_ETLAlerts_IsResolved] ON [ETLAlerts]([IsResolved]);

-- ETLConfigurations Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLConfigurations_Name' AND object_id = OBJECT_ID('dbo.ETLConfigurations'))
    CREATE INDEX [IX_ETLConfigurations_Name] ON [ETLConfigurations]([Name]);

-- ETLDataQualityIssues Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLDataQualityIssues_DetectedAt' AND object_id = OBJECT_ID('dbo.ETLDataQualityIssues'))
    CREATE INDEX [IX_ETLDataQualityIssues_DetectedAt] ON [ETLDataQualityIssues]([DetectedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLDataQualityIssues_IssueType' AND object_id = OBJECT_ID('dbo.ETLDataQualityIssues'))
    CREATE INDEX [IX_ETLDataQualityIssues_IssueType] ON [ETLDataQualityIssues]([IssueType]);

-- ETLLogEntries Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLLogEntries_JobId' AND object_id = OBJECT_ID('dbo.ETLLogEntries'))
    CREATE INDEX [IX_ETLLogEntries_JobId] ON [ETLLogEntries]([JobId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ETLLogEntries_Timestamp' AND object_id = OBJECT_ID('dbo.ETLLogEntries'))
    CREATE INDEX [IX_ETLLogEntries_Timestamp] ON [ETLLogEntries]([Timestamp]);

-- Partners Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Partners_CreatedAt' AND object_id = OBJECT_ID('dbo.Partners'))
    CREATE INDEX [IX_Partners_CreatedAt] ON [Partners]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Partners_IsActive' AND object_id = OBJECT_ID('dbo.Partners'))
    CREATE INDEX [IX_Partners_IsActive] ON [Partners]([IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Partners_IsActive_SortOrder' AND object_id = OBJECT_ID('dbo.Partners'))
    CREATE INDEX [IX_Partners_IsActive_SortOrder] ON [Partners]([IsActive], [SortOrder]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Partners_Name' AND object_id = OBJECT_ID('dbo.Partners'))
    CREATE UNIQUE INDEX [IX_Partners_Name] ON [Partners]([Name]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Partners_SortOrder' AND object_id = OBJECT_ID('dbo.Partners'))
    CREATE INDEX [IX_Partners_SortOrder] ON [Partners]([SortOrder]);

-- PartnerUser Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PartnerUser_UsersId' AND object_id = OBJECT_ID('dbo.PartnerUser'))
    CREATE INDEX [IX_PartnerUser_UsersId] ON [PartnerUser]([UsersId]);

-- PermissionRole Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PermissionRole_RolesId' AND object_id = OBJECT_ID('dbo.PermissionRole'))
    CREATE INDEX [IX_PermissionRole_RolesId] ON [PermissionRole]([RolesId]);

-- Permissions Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Permissions_Codename' AND object_id = OBJECT_ID('dbo.Permissions'))
    CREATE UNIQUE INDEX [IX_Permissions_Codename] ON [Permissions]([Codename]);

-- ProductCategories Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductCategories_Name' AND object_id = OBJECT_ID('dbo.ProductCategories'))
    CREATE UNIQUE INDEX [IX_ProductCategories_Name] ON [ProductCategories]([Name]);

-- ProductIdentifiers Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductIdentifiers_ProductId_IdentifierType_IdentifierValue' AND object_id = OBJECT_ID('dbo.ProductIdentifiers'))
    CREATE UNIQUE INDEX [IX_ProductIdentifiers_ProductId_IdentifierType_IdentifierValue] ON [ProductIdentifiers]([ProductId], [IdentifierType], [IdentifierValue]);

-- Products Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_CategoryId] ON [Products]([CategoryId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId_CompanyId' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_CategoryId_CompanyId] ON [Products]([CategoryId], [CompanyId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CompanyId' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_CompanyId] ON [Products]([CompanyId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CompanyId_IsActive' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_CompanyId_IsActive] ON [Products]([CompanyId], [IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CreatedAt' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_CreatedAt] ON [Products]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_IsActive' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_IsActive] ON [Products]([IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Name' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_Name] ON [Products]([Name]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_ProductSubcategoryId' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_ProductSubcategoryId] ON [Products]([ProductSubcategoryId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_ProductTypeId' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_ProductTypeId] ON [Products]([ProductTypeId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_ProductTypeId_IsActive' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX [IX_Products_ProductTypeId_IsActive] ON [Products]([ProductTypeId], [IsActive]);

-- ProductSubcategories Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductSubcategories_ProductTypeId' AND object_id = OBJECT_ID('dbo.ProductSubcategories'))
    CREATE INDEX [IX_ProductSubcategories_ProductTypeId] ON [ProductSubcategories]([ProductTypeId]);

-- ProductTag Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductTag_TagsId' AND object_id = OBJECT_ID('dbo.ProductTag'))
    CREATE INDEX [IX_ProductTag_TagsId] ON [ProductTag]([TagsId]);

-- ProductTypes Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductTypes_CategoryId' AND object_id = OBJECT_ID('dbo.ProductTypes'))
    CREATE INDEX [IX_ProductTypes_CategoryId] ON [ProductTypes]([CategoryId]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductTypes_ParentId' AND object_id = OBJECT_ID('dbo.ProductTypes'))
    CREATE INDEX [IX_ProductTypes_ParentId] ON [ProductTypes]([ParentId]);

-- Roles Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Roles_Name' AND object_id = OBJECT_ID('dbo.Roles'))
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles]([Name]);

-- RoleUser Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RoleUser_UsersId' AND object_id = OBJECT_ID('dbo.RoleUser'))
    CREATE INDEX [IX_RoleUser_UsersId] ON [RoleUser]([UsersId]);

-- Tags Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tags_Name' AND object_id = OBJECT_ID('dbo.Tags'))
    CREATE UNIQUE INDEX [IX_Tags_Name] ON [Tags]([Name]);

-- Users Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_CreatedAt' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_CreatedAt] ON [Users]([CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users]([Email]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_IsActive' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_IsActive] ON [Users]([IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_IsActive_CreatedAt' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_IsActive_CreatedAt] ON [Users]([IsActive], [CreatedAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_LastLoginAt' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_LastLoginAt] ON [Users]([LastLoginAt]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Status' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_Status] ON [Users]([Status]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Status_IsActive' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_Status_IsActive] ON [Users]([Status], [IsActive]);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users]([Username]);

PRINT 'All indexes created successfully';
PRINT '';

-- ============================================
-- Summary
-- ============================================
PRINT '============================================';
PRINT 'Schema creation complete!';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';
PRINT 'Tables created: 28';
PRINT '  - Independent: 13 (ApiKeys, CertificateAggregates, CertificationTypes, Companies, ETL tables, Partners, Permissions, ProductCategories, Roles, Tags, Users)';
PRINT '  - Level 1 dependencies: 7 (CustomFields, ProductTypes, join tables)';
PRINT '  - Level 2 dependencies: 1 (Products)';
PRINT '  - Level 3 dependencies: 3 (Certificates, ProductIdentifiers, ProductTag)';
PRINT '  - Level 4 dependencies: 4 (CertificateDocuments, CertificateProductIds, CertificateTag, CustomFieldValues)';
PRINT '';
PRINT 'Indexes created: 100+ performance indexes';
PRINT '';
PRINT 'NEXT STEP: Run FULL_TEST_DATA_SEED.sql to populate data';
PRINT '============================================';
GO

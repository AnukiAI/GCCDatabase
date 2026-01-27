IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ApiKeys] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Key] nvarchar(500) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastUsedAt] datetime2 NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedBy] nvarchar(50) NULL,
        [RateLimitPerMinute] int NULL,
        [RateLimitPerHour] int NULL,
        [RateLimitPerDay] int NULL,
        [AllowedIPs] nvarchar(max) NULL,
        [AllowedOrigins] nvarchar(max) NULL,
        [Scopes] nvarchar(max) NULL,
        [KeyHash] nvarchar(450) NULL,
        [MaxConcurrentRequests] int NULL,
        [RequireTLS] bit NOT NULL,
        [LastSecurityIncident] datetime2 NULL,
        [SecurityViolationCount] int NOT NULL,
        [IsTemporarilyBlocked] bit NOT NULL,
        [BlockedUntil] datetime2 NULL,
        [UserAgent] nvarchar(max) NULL,
        [RateLimit] int NULL,
        [SecurityViolations] int NOT NULL,
        [LastViolationAt] datetime2 NULL,
        [LastViolationType] nvarchar(max) NULL,
        [BlockReason] nvarchar(max) NULL,
        [RequestCount] int NOT NULL,
        [LastRotatedAt] datetime2 NULL,
        [Environment] nvarchar(max) NOT NULL,
        [UsageCostPerRequest] decimal(18,2) NULL,
        CONSTRAINT [PK_ApiKeys] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CertificateAggregates] (
        [Period] nvarchar(450) NOT NULL,
        [PeriodDate] datetime2 NOT NULL,
        [TotalCertificates] int NOT NULL,
        [ActiveCertificates] int NOT NULL,
        [ExpiredCertificates] int NOT NULL,
        [ExpiringThisMonth] int NOT NULL,
        [CertificatesByType] TEXT NOT NULL,
        [CertificatesByCountry] TEXT NOT NULL,
        [CertificatesByCompany] TEXT NOT NULL,
        [AverageValidityPeriod] float NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CertificateAggregates] PRIMARY KEY ([Period], [PeriodDate])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CertificationTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [AmazonCode] nvarchar(255) NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_CertificationTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Companies] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [ContactInfo] nvarchar(255) NULL,
        [Email] nvarchar(255) NULL,
        [Website] nvarchar(500) NULL,
        [Location] nvarchar(175) NULL,
        [Country] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ETLAlerts] (
        [Id] uniqueidentifier NOT NULL,
        [AlertType] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Severity] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsResolved] bit NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [Context] TEXT NOT NULL,
        CONSTRAINT [PK_ETLAlerts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ETLConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [JobType] int NOT NULL,
        [IsIncremental] bit NOT NULL,
        [LastRunTime] datetime2 NULL,
        [DataSources] nvarchar(max) NOT NULL,
        [Transformations] nvarchar(max) NOT NULL,
        [Destinations] nvarchar(max) NOT NULL,
        [Settings] TEXT NOT NULL,
        [IsActive] bit NOT NULL,
        [CronSchedule] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ETLConfigurations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ETLDataQualityIssues] (
        [Id] uniqueidentifier NOT NULL,
        [IssueType] nvarchar(450) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [FieldName] nvarchar(max) NOT NULL,
        [RecordId] nvarchar(max) NULL,
        [ExpectedValue] TEXT NULL,
        [ActualValue] TEXT NULL,
        [Severity] int NOT NULL,
        [DetectedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ETLDataQualityIssues] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ETLLogEntries] (
        [Id] uniqueidentifier NOT NULL,
        [JobId] uniqueidentifier NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [LogLevel] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NULL,
        [Properties] TEXT NOT NULL,
        CONSTRAINT [PK_ETLLogEntries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Partners] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Website] nvarchar(500) NULL,
        [ContactInfo] nvarchar(255) NULL,
        [Email] nvarchar(255) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Codename] nvarchar(100) NOT NULL,
        [Category] nvarchar(100) NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ProductCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsSystem] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Tags] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [TagType] nvarchar(20) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(255) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Name] nvarchar(100) NULL,
        [FullName] nvarchar(255) NULL,
        [LastLoginAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsStaff] bit NOT NULL,
        [IsSuperuser] bit NOT NULL,
        [RequirePasswordReset] bit NOT NULL,
        [LastLogin] datetime2 NULL,
        [PasswordChangedAt] datetime2 NULL,
        [FailedLoginAttempts] int NOT NULL,
        [LockedUntil] datetime2 NULL,
        [TwoFactorSecret] nvarchar(max) NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [TwoFactorMethod] nvarchar(max) NOT NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [IsPhoneVerified] bit NOT NULL,
        [IsEmailVerified] bit NOT NULL,
        [RecoveryCodes] nvarchar(max) NULL,
        [LastSecurityAudit] datetime2 NULL,
        [SecurityQuestions] nvarchar(max) NULL,
        [MustChangePassword] bit NOT NULL,
        [MaxConcurrentSessions] int NOT NULL,
        [AllowedIPs] nvarchar(max) NULL,
        [RequirePasswordChange] bit NOT NULL,
        [PasswordExpiresAt] datetime2 NULL,
        [PasswordHistory] nvarchar(max) NULL,
        [MaxPasswordHistory] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CustomFields] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [FieldType] nvarchar(50) NOT NULL,
        [IsRequired] bit NOT NULL,
        [IsVisible] bit NOT NULL,
        [IsPublic] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Options] nvarchar(max) NULL,
        [DefaultValue] nvarchar(max) NULL,
        [ValidationRules] nvarchar(max) NULL,
        [CertificateTypeId] int NOT NULL,
        CONSTRAINT [PK_CustomFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomFields_CertificationTypes_CertificateTypeId] FOREIGN KEY ([CertificateTypeId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ProductTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [CategoryId] int NOT NULL,
        [ParentId] int NULL,
        [Level] int NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ProductTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductTypes_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductTypes_ProductTypes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ProductTypes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [PermissionRole] (
        [PermissionsId] int NOT NULL,
        [RolesId] int NOT NULL,
        CONSTRAINT [PK_PermissionRole] PRIMARY KEY ([PermissionsId], [RolesId]),
        CONSTRAINT [FK_PermissionRole_Permissions_PermissionsId] FOREIGN KEY ([PermissionsId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PermissionRole_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CompanyUser] (
        [CompaniesId] int NOT NULL,
        [UsersId] int NOT NULL,
        CONSTRAINT [PK_CompanyUser] PRIMARY KEY ([CompaniesId], [UsersId]),
        CONSTRAINT [FK_CompanyUser_Companies_CompaniesId] FOREIGN KEY ([CompaniesId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CompanyUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [PartnerUser] (
        [PartnersId] int NOT NULL,
        [UsersId] int NOT NULL,
        CONSTRAINT [PK_PartnerUser] PRIMARY KEY ([PartnersId], [UsersId]),
        CONSTRAINT [FK_PartnerUser_Partners_PartnersId] FOREIGN KEY ([PartnersId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PartnerUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [RoleUser] (
        [RolesId] int NOT NULL,
        [UsersId] int NOT NULL,
        CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
        CONSTRAINT [FK_RoleUser_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ProductSubcategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [ProductTypeId] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ProductSubcategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductSubcategories_ProductTypes_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(255) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Model] nvarchar(100) NULL,
        [Sku] nvarchar(50) NULL,
        [Upc] nvarchar(50) NULL,
        [Asin] nvarchar(50) NULL,
        [CategoryId] int NOT NULL,
        [ProductTypeId] int NULL,
        [ProductSubcategoryId] int NULL,
        [CompanyId] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]),
        CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Products_ProductSubcategories_ProductSubcategoryId] FOREIGN KEY ([ProductSubcategoryId]) REFERENCES [ProductSubcategories] ([Id]),
        CONSTRAINT [FK_Products_ProductTypes_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [Certificates] (
        [Id] int NOT NULL IDENTITY,
        [Number] nvarchar(7) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [ExpirationDate] datetime2 NOT NULL,
        [Location] nvarchar(175) NULL,
        [Country] nvarchar(100) NULL,
        [Asin] nvarchar(max) NULL,
        [Upc] nvarchar(max) NULL,
        [Gtin] nvarchar(max) NULL,
        [Ean] nvarchar(max) NULL,
        [CertificateFile] nvarchar(max) NULL,
        [CertificateImagePath] nvarchar(500) NULL,
        [Partner] nvarchar(100) NULL,
        [CompanyId] int NOT NULL,
        [ProductId] int NOT NULL,
        [CertificationTypeId] int NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Certificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Certificates_CertificationTypes_CertificationTypeId] FOREIGN KEY ([CertificationTypeId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Certificates_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Certificates_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Certificates_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Certificates_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ProductIdentifiers] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [IdentifierType] nvarchar(20) NOT NULL,
        [IdentifierValue] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductIdentifiers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductIdentifiers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [ProductTag] (
        [ProductsId] int NOT NULL,
        [TagsId] int NOT NULL,
        CONSTRAINT [PK_ProductTag] PRIMARY KEY ([ProductsId], [TagsId]),
        CONSTRAINT [FK_ProductTag_Products_ProductsId] FOREIGN KEY ([ProductsId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductTag_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CertificateDocuments] (
        [Id] int NOT NULL IDENTITY,
        [FileName] nvarchar(255) NOT NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSize] bigint NOT NULL,
        [FileHash] nvarchar(64) NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Version] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsPublic] bit NOT NULL,
        [IsVirusScanned] bit NOT NULL,
        [PassedVirusScan] bit NOT NULL,
        [VirusScanDate] datetime2 NULL,
        [CertificateId] int NOT NULL,
        [UploadedBy] int NULL,
        [UploadedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CertificateDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateDocuments_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateDocuments_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CertificateProductIds] (
        [Id] int NOT NULL IDENTITY,
        [CertificateId] int NOT NULL,
        [ProductIdType] nvarchar(10) NOT NULL,
        [ProductIdValue] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Partner] nvarchar(50) NULL,
        [ProductName] nvarchar(200) NULL,
        [ProductSku] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [IsPublic] bit NOT NULL,
        [DisplayOrder] int NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CertificateProductIds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateProductIds_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateProductIds_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_CertificateProductIds_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CertificateTag] (
        [CertificatesId] int NOT NULL,
        [TagsId] int NOT NULL,
        CONSTRAINT [PK_CertificateTag] PRIMARY KEY ([CertificatesId], [TagsId]),
        CONSTRAINT [FK_CertificateTag_Certificates_CertificatesId] FOREIGN KEY ([CertificatesId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateTag_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE TABLE [CustomFieldValues] (
        [Id] int NOT NULL IDENTITY,
        [CertificateId] int NOT NULL,
        [FieldId] int NOT NULL,
        [TextValue] nvarchar(max) NULL,
        [NumberValue] decimal(18,4) NULL,
        [DateValue] datetime2 NULL,
        [BooleanValue] bit NULL,
        CONSTRAINT [PK_CustomFieldValues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomFieldValues_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomFieldValues_CustomFields_FieldId] FOREIGN KEY ([FieldId]) REFERENCES [CustomFields] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ApiKeys_CreatedAt] ON [ApiKeys] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ApiKeys_CreatedBy] ON [ApiKeys] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ApiKeys_CreatedBy_IsActive] ON [ApiKeys] ([CreatedBy], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ApiKeys_ExpiresAt] ON [ApiKeys] ([ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ApiKeys_IsActive] ON [ApiKeys] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ApiKeys_KeyHash] ON [ApiKeys] ([KeyHash]) WHERE [KeyHash] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ApiKeys_KeyHash_IsActive_ExpiresAt] ON [ApiKeys] ([KeyHash], [IsActive], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateAggregates_PeriodDate] ON [CertificateAggregates] ([PeriodDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateDocuments_CertificateId] ON [CertificateDocuments] ([CertificateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateDocuments_FileHash] ON [CertificateDocuments] ([FileHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateDocuments_UploadedBy] ON [CertificateDocuments] ([UploadedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_CertificateId] ON [CertificateProductIds] ([CertificateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_CertificateId_IsActive] ON [CertificateProductIds] ([CertificateId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_CertificateId_ProductIdType] ON [CertificateProductIds] ([CertificateId], [ProductIdType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_CreatedAt] ON [CertificateProductIds] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_CreatedBy] ON [CertificateProductIds] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_IsActive] ON [CertificateProductIds] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_Partner] ON [CertificateProductIds] ([Partner]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_Partner_Status_IsActive] ON [CertificateProductIds] ([Partner], [Status], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_ProductIdType] ON [CertificateProductIds] ([ProductIdType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CertificateProductIds_ProductIdType_ProductIdValue] ON [CertificateProductIds] ([ProductIdType], [ProductIdValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_ProductIdValue] ON [CertificateProductIds] ([ProductIdValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_Status] ON [CertificateProductIds] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_Status_IsActive_CreatedAt] ON [CertificateProductIds] ([Status], [IsActive], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateProductIds_UpdatedBy] ON [CertificateProductIds] ([UpdatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_CertificationTypeId] ON [Certificates] ([CertificationTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_CertificationTypeId_Status] ON [Certificates] ([CertificationTypeId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_CompanyId] ON [Certificates] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_CompanyId_Status] ON [Certificates] ([CompanyId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_Country] ON [Certificates] ([Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_Country_Status] ON [Certificates] ([Country], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_CreatedAt] ON [Certificates] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_CreatedBy] ON [Certificates] ([CreatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_EffectiveDate] ON [Certificates] ([EffectiveDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_ExpirationDate] ON [Certificates] ([ExpirationDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_ExpirationDate_Status_CompanyId] ON [Certificates] ([ExpirationDate], [Status], [CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Certificates_Number] ON [Certificates] ([Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_ProductId] ON [Certificates] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_ProductId_Status] ON [Certificates] ([ProductId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_Status] ON [Certificates] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_Status_ExpirationDate] ON [Certificates] ([Status], [ExpirationDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Certificates_UpdatedBy] ON [Certificates] ([UpdatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CertificateTag_TagsId] ON [CertificateTag] ([TagsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CertificationTypes_Name] ON [CertificationTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Companies_Country] ON [Companies] ([Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Companies_Country_IsActive] ON [Companies] ([Country], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Companies_CreatedAt] ON [Companies] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Companies_IsActive] ON [Companies] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Companies_Name] ON [Companies] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Companies_Name_Country] ON [Companies] ([Name], [Country]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Companies_Website] ON [Companies] ([Website]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CompanyUser_UsersId] ON [CompanyUser] ([UsersId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CustomFields_CertificateTypeId] ON [CustomFields] ([CertificateTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CustomFieldValues_CertificateId] ON [CustomFieldValues] ([CertificateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_CustomFieldValues_FieldId] ON [CustomFieldValues] ([FieldId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLAlerts_CreatedAt] ON [ETLAlerts] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLAlerts_IsResolved] ON [ETLAlerts] ([IsResolved]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLConfigurations_Name] ON [ETLConfigurations] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLDataQualityIssues_DetectedAt] ON [ETLDataQualityIssues] ([DetectedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLDataQualityIssues_IssueType] ON [ETLDataQualityIssues] ([IssueType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLLogEntries_JobId] ON [ETLLogEntries] ([JobId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ETLLogEntries_Timestamp] ON [ETLLogEntries] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Partners_CreatedAt] ON [Partners] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Partners_IsActive] ON [Partners] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Partners_IsActive_SortOrder] ON [Partners] ([IsActive], [SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Partners_Name] ON [Partners] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Partners_SortOrder] ON [Partners] ([SortOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_PartnerUser_UsersId] ON [PartnerUser] ([UsersId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_PermissionRole_RolesId] ON [PermissionRole] ([RolesId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Codename] ON [Permissions] ([Codename]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductCategories_Name] ON [ProductCategories] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductIdentifiers_ProductId_IdentifierType_IdentifierValue] ON [ProductIdentifiers] ([ProductId], [IdentifierType], [IdentifierValue]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId_CompanyId] ON [Products] ([CategoryId], [CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_CompanyId] ON [Products] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_CompanyId_IsActive] ON [Products] ([CompanyId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_CreatedAt] ON [Products] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_IsActive] ON [Products] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_ProductSubcategoryId] ON [Products] ([ProductSubcategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_ProductTypeId] ON [Products] ([ProductTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Products_ProductTypeId_IsActive] ON [Products] ([ProductTypeId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ProductSubcategories_ProductTypeId] ON [ProductSubcategories] ([ProductTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ProductTag_TagsId] ON [ProductTag] ([TagsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ProductTypes_CategoryId] ON [ProductTypes] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_ProductTypes_ParentId] ON [ProductTypes] ([ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_RoleUser_UsersId] ON [RoleUser] ([UsersId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tags_Name] ON [Tags] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Users_IsActive_CreatedAt] ON [Users] ([IsActive], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Users_LastLoginAt] ON [Users] ([LastLoginAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Users_Status] ON [Users] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE INDEX [IX_Users_Status_IsActive] ON [Users] ([Status], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251015162223_InitialCreate_SqlServer_Final'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251015162223_InitialCreate_SqlServer_Final', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029003704_AddLevelToCertificationType'
)
BEGIN
    ALTER TABLE [CertificationTypes] ADD [Level] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029003704_AddLevelToCertificationType'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'UsageCostPerRequest');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [ApiKeys] ALTER COLUMN [UsageCostPerRequest] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029003704_AddLevelToCertificationType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029003704_AddLevelToCertificationType', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029005907_DistributeCertificatesAcrossTypes'
)
BEGIN

                    -- Update certificates to distribute across all certification types
                    WITH NumberedCertificates AS (
                        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) as RowNum
                        FROM Certificates
                    )
                    UPDATE c
                    SET CertificationTypeId = 1 + ((nc.RowNum - 1) % 22)
                    FROM Certificates c
                    INNER JOIN NumberedCertificates nc ON c.Id = nc.Id;
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029005907_DistributeCertificatesAcrossTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029005907_DistributeCertificatesAcrossTypes', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    ALTER TABLE [CertificationTypes] ADD [ParentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    CREATE INDEX [IX_CertificationTypes_ParentId] ON [CertificationTypes] ([ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    ALTER TABLE [CertificationTypes] ADD CONSTRAINT [FK_CertificationTypes_CertificationTypes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [CertificationTypes] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN

                    UPDATE CertificationTypes
                    SET ParentId = (SELECT Id FROM CertificationTypes WHERE Name = 'Waste Diversion from Landfill')
                    WHERE Name = 'Waste System Audit';
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029011232_AddParentIdToCertificationType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029011232_AddParentIdToCertificationType', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029163311_FixWasteSystemAuditLevel'
)
BEGIN
    UPDATE CertificationTypes SET Level = 1 WHERE Name = 'Waste System Audit' AND Level = 2;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251029163311_FixWasteSystemAuditLevel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251029163311_FixWasteSystemAuditLevel', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111164931_AddAmazonApiIntegration'
)
BEGIN
    CREATE TABLE [AmazonApiSettings] (
        [Id] int NOT NULL IDENTITY,
        [IsEnabled] bit NOT NULL,
        [SyncIntervalHours] int NOT NULL,
        [AccessKeyId] nvarchar(500) NOT NULL,
        [SecretAccessKey] nvarchar(500) NOT NULL,
        [RefreshToken] nvarchar(1000) NOT NULL,
        [Region] nvarchar(10) NOT NULL,
        [MarketplaceId] nvarchar(50) NOT NULL,
        [SyncProducts] bit NOT NULL,
        [SyncCertificates] bit NOT NULL,
        [SyncCompliance] bit NOT NULL,
        [WebhookUrl] nvarchar(500) NULL,
        [LastSync] datetime2 NULL,
        [LastSyncStatus] nvarchar(20) NOT NULL,
        [RecordsSynced] int NOT NULL,
        [LastError] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_AmazonApiSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111164931_AddAmazonApiIntegration'
)
BEGIN
    CREATE TABLE [AmazonSyncHistory] (
        [Id] int NOT NULL IDENTITY,
        [Timestamp] datetime2 NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [RecordsProcessed] int NOT NULL,
        [RecordsSucceeded] int NOT NULL,
        [RecordsFailed] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [DurationSeconds] float NOT NULL,
        [Message] nvarchar(1000) NULL,
        [ErrorDetails] nvarchar(4000) NULL,
        [TriggeredBy] nvarchar(100) NULL,
        CONSTRAINT [PK_AmazonSyncHistory] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251111164931_AddAmazonApiIntegration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251111164931_AddAmazonApiIntegration', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112144629_AddPartnerApiKeys'
)
BEGIN
    CREATE TABLE [PartnerApiKeys] (
        [Id] int NOT NULL IDENTITY,
        [PartnerId] int NOT NULL,
        [ApiKey] nvarchar(512) NOT NULL,
        [Description] nvarchar(255) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(255) NULL,
        [ExpiresAt] datetime2 NULL,
        [LastUsedAt] datetime2 NULL,
        [RequestCount] int NOT NULL,
        [RateLimitPerHour] int NULL,
        [AllowedEndpoints] nvarchar(max) NULL,
        [Permissions] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedBy] nvarchar(255) NULL,
        CONSTRAINT [PK_PartnerApiKeys] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PartnerApiKeys_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112144629_AddPartnerApiKeys'
)
BEGIN
    CREATE INDEX [IX_PartnerApiKeys_PartnerId] ON [PartnerApiKeys] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251112144629_AddPartnerApiKeys'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251112144629_AddPartnerApiKeys', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113161816_AddGoogleAnalyticsSettings'
)
BEGIN
    CREATE TABLE [GoogleAnalyticsSettings] (
        [Id] int NOT NULL IDENTITY,
        [TrackingId] nvarchar(20) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [CustomScript] TEXT NULL,
        [AnonymizeIp] bit NOT NULL,
        [EnhancedLinkAttribution] bit NOT NULL,
        [DebugMode] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_GoogleAnalyticsSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GoogleAnalyticsSettings_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113161816_AddGoogleAnalyticsSettings'
)
BEGIN
    CREATE INDEX [IX_GoogleAnalyticsSettings_UpdatedBy] ON [GoogleAnalyticsSettings] ([UpdatedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113161816_AddGoogleAnalyticsSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113161816_AddGoogleAnalyticsSettings', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113171055_AddSectionToCustomFields'
)
BEGIN
    ALTER TABLE [CustomFields] ADD [Section] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251113171055_AddSectionToCustomFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251113171055_AddSectionToCustomFields', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE TABLE [CertificateStatusHistory] (
        [Id] int NOT NULL IDENTITY,
        [CertificateId] int NOT NULL,
        [CertificateNumber] nvarchar(7) NULL,
        [PreviousStatus] nvarchar(50) NOT NULL,
        [NewStatus] nvarchar(50) NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [Notes] nvarchar(max) NULL,
        [ChangedBy] int NOT NULL,
        [ChangedByUsername] nvarchar(255) NULL,
        [ChangedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [ChangeSource] nvarchar(50) NULL,
        [DurationInPreviousStatus] int NULL,
        [IsAutomatedTransition] bit NOT NULL,
        [WorkflowStep] nvarchar(100) NULL,
        CONSTRAINT [PK_CertificateStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CertificateStatusHistory_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CertificateStatusHistory_Users_ChangedBy] FOREIGN KEY ([ChangedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_CertificateId] ON [CertificateStatusHistory] ([CertificateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_CertificateId_ChangedAt] ON [CertificateStatusHistory] ([CertificateId], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_CertificateId_NewStatus] ON [CertificateStatusHistory] ([CertificateId], [NewStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_ChangedAt] ON [CertificateStatusHistory] ([ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_ChangedAt_NewStatus] ON [CertificateStatusHistory] ([ChangedAt], [NewStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_ChangedBy] ON [CertificateStatusHistory] ([ChangedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_NewStatus] ON [CertificateStatusHistory] ([NewStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    CREATE INDEX [IX_CertificateStatusHistory_PreviousStatus] ON [CertificateStatusHistory] ([PreviousStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251117211135_AddCertificateStatusHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251117211135_AddCertificateStatusHistory', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [RoleUser] DROP CONSTRAINT [FK_RoleUser_Roles_RolesId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [RoleUser] DROP CONSTRAINT [FK_RoleUser_Users_UsersId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [RoleUser] DROP CONSTRAINT [PK_RoleUser];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    EXEC sp_rename N'[RoleUser]', N'UserRoles';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    EXEC sp_rename N'[UserRoles].[IX_RoleUser_UsersId]', N'IX_UserRoles_UsersId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [UserRoles] ADD CONSTRAINT [PK_UserRoles] PRIMARY KEY ([RolesId], [UsersId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    ALTER TABLE [UserRoles] ADD CONSTRAINT [FK_UserRoles_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118214329_RenameRoleUserToUserRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118214329_RenameRoleUserToUserRoles', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE TABLE [AuditHistory] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(50) NOT NULL,
        [EntityName] nvarchar(255) NULL,
        [Action] nvarchar(20) NOT NULL,
        [FieldName] nvarchar(100) NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [ChangedBy] int NOT NULL,
        [ChangedByUsername] nvarchar(255) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [ChangeSource] nvarchar(50) NULL,
        [Reason] nvarchar(1000) NULL,
        [SessionId] nvarchar(100) NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditHistory_Users_ChangedBy] FOREIGN KEY ([ChangedBy]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_Action] ON [AuditHistory] ([Action]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedAt] ON [AuditHistory] ([ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedAt_EntityType] ON [AuditHistory] ([ChangedAt], [EntityType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedBy] ON [AuditHistory] ([ChangedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_ChangedBy_ChangedAt] ON [AuditHistory] ([ChangedBy], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityId] ON [AuditHistory] ([EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityType] ON [AuditHistory] ([EntityType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityType_Action] ON [AuditHistory] ([EntityType], [Action]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    CREATE INDEX [IX_AuditHistory_EntityType_EntityId_ChangedAt] ON [AuditHistory] ([EntityType], [EntityId], [ChangedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251123161550_AddAuditHistoryTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251123161550_AddAuditHistoryTable', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126212053_AddCertificationResultsToCertificate'
)
BEGIN
    ALTER TABLE [Certificates] ADD [CertificationResults] nvarchar(1000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251126212053_AddCertificationResultsToCertificate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251126212053_AddCertificationResultsToCertificate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208051905_AddImportBatchAndAuditImportBatchId'
)
BEGIN
    ALTER TABLE [AuditHistory] ADD [ImportBatchId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208051905_AddImportBatchAndAuditImportBatchId'
)
BEGIN
    CREATE TABLE [ImportBatches] (
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
        CONSTRAINT [FK_ImportBatches_Users_ImportedBy] FOREIGN KEY ([ImportedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208051905_AddImportBatchAndAuditImportBatchId'
)
BEGIN
    CREATE INDEX [IX_ImportBatches_ImportedBy] ON [ImportBatches] ([ImportedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251208051905_AddImportBatchAndAuditImportBatchId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251208051905_AddImportBatchAndAuditImportBatchId', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216221555_AddCertificationTypeSubcategoryId'
)
BEGIN
    ALTER TABLE [Certificates] ADD [CertificationTypeSubcategoryId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216221555_AddCertificationTypeSubcategoryId'
)
BEGIN
    CREATE INDEX [IX_Certificates_CertificationTypeSubcategoryId] ON [Certificates] ([CertificationTypeSubcategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216221555_AddCertificationTypeSubcategoryId'
)
BEGIN
    ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_CertificationTypes_CertificationTypeSubcategoryId] FOREIGN KEY ([CertificationTypeSubcategoryId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251216221555_AddCertificationTypeSubcategoryId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251216221555_AddCertificationTypeSubcategoryId', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222200135_AddRoleVisibilityToCustomFields'
)
BEGIN
    ALTER TABLE [CustomFields] ADD [RoleVisibility] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251222200135_AddRoleVisibilityToCustomFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251222200135_AddRoleVisibilityToCustomFields', N'8.0.11');
END;
GO

COMMIT;
GO


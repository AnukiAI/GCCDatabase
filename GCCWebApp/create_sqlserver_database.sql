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
GO


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
GO


CREATE TABLE [CertificationTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [AmazonCode] nvarchar(255) NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_CertificationTypes] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


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
GO


CREATE TABLE [ProductCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(max) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
);
GO


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
GO


CREATE TABLE [Tags] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [TagType] nvarchar(20) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
);
GO


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
GO


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
GO


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
GO


CREATE TABLE [PermissionRole] (
    [PermissionsId] int NOT NULL,
    [RolesId] int NOT NULL,
    CONSTRAINT [PK_PermissionRole] PRIMARY KEY ([PermissionsId], [RolesId]),
    CONSTRAINT [FK_PermissionRole_Permissions_PermissionsId] FOREIGN KEY ([PermissionsId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PermissionRole_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CompanyUser] (
    [CompaniesId] int NOT NULL,
    [UsersId] int NOT NULL,
    CONSTRAINT [PK_CompanyUser] PRIMARY KEY ([CompaniesId], [UsersId]),
    CONSTRAINT [FK_CompanyUser_Companies_CompaniesId] FOREIGN KEY ([CompaniesId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CompanyUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PartnerUser] (
    [PartnersId] int NOT NULL,
    [UsersId] int NOT NULL,
    CONSTRAINT [PK_PartnerUser] PRIMARY KEY ([PartnersId], [UsersId]),
    CONSTRAINT [FK_PartnerUser_Partners_PartnersId] FOREIGN KEY ([PartnersId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PartnerUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RoleUser] (
    [RolesId] int NOT NULL,
    [UsersId] int NOT NULL,
    CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
    CONSTRAINT [FK_RoleUser_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


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
GO


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
GO


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
    CONSTRAINT [FK_Certificates_CertificationTypes_CertificationTypeId] FOREIGN KEY ([CertificationTypeId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Certificates_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Certificates_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Certificates_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Certificates_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


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
GO


CREATE TABLE [ProductTag] (
    [ProductsId] int NOT NULL,
    [TagsId] int NOT NULL,
    CONSTRAINT [PK_ProductTag] PRIMARY KEY ([ProductsId], [TagsId]),
    CONSTRAINT [FK_ProductTag_Products_ProductsId] FOREIGN KEY ([ProductsId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductTag_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
);
GO


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
    CONSTRAINT [FK_CertificateDocuments_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


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
    CONSTRAINT [FK_CertificateProductIds_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CertificateProductIds_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [CertificateTag] (
    [CertificatesId] int NOT NULL,
    [TagsId] int NOT NULL,
    CONSTRAINT [PK_CertificateTag] PRIMARY KEY ([CertificatesId], [TagsId]),
    CONSTRAINT [FK_CertificateTag_Certificates_CertificatesId] FOREIGN KEY ([CertificatesId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CertificateTag_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
);
GO


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
GO


CREATE INDEX [IX_ApiKeys_CreatedAt] ON [ApiKeys] ([CreatedAt]);
GO


CREATE INDEX [IX_ApiKeys_CreatedBy] ON [ApiKeys] ([CreatedBy]);
GO


CREATE INDEX [IX_ApiKeys_CreatedBy_IsActive] ON [ApiKeys] ([CreatedBy], [IsActive]);
GO


CREATE INDEX [IX_ApiKeys_ExpiresAt] ON [ApiKeys] ([ExpiresAt]);
GO


CREATE INDEX [IX_ApiKeys_IsActive] ON [ApiKeys] ([IsActive]);
GO


CREATE UNIQUE INDEX [IX_ApiKeys_KeyHash] ON [ApiKeys] ([KeyHash]) WHERE [KeyHash] IS NOT NULL;
GO


CREATE INDEX [IX_ApiKeys_KeyHash_IsActive_ExpiresAt] ON [ApiKeys] ([KeyHash], [IsActive], [ExpiresAt]);
GO


CREATE INDEX [IX_CertificateAggregates_PeriodDate] ON [CertificateAggregates] ([PeriodDate]);
GO


CREATE INDEX [IX_CertificateDocuments_CertificateId] ON [CertificateDocuments] ([CertificateId]);
GO


CREATE INDEX [IX_CertificateDocuments_FileHash] ON [CertificateDocuments] ([FileHash]);
GO


CREATE INDEX [IX_CertificateDocuments_UploadedBy] ON [CertificateDocuments] ([UploadedBy]);
GO


CREATE INDEX [IX_CertificateProductIds_CertificateId] ON [CertificateProductIds] ([CertificateId]);
GO


CREATE INDEX [IX_CertificateProductIds_CertificateId_IsActive] ON [CertificateProductIds] ([CertificateId], [IsActive]);
GO


CREATE INDEX [IX_CertificateProductIds_CertificateId_ProductIdType] ON [CertificateProductIds] ([CertificateId], [ProductIdType]);
GO


CREATE INDEX [IX_CertificateProductIds_CreatedAt] ON [CertificateProductIds] ([CreatedAt]);
GO


CREATE INDEX [IX_CertificateProductIds_CreatedBy] ON [CertificateProductIds] ([CreatedBy]);
GO


CREATE INDEX [IX_CertificateProductIds_IsActive] ON [CertificateProductIds] ([IsActive]);
GO


CREATE INDEX [IX_CertificateProductIds_Partner] ON [CertificateProductIds] ([Partner]);
GO


CREATE INDEX [IX_CertificateProductIds_Partner_Status_IsActive] ON [CertificateProductIds] ([Partner], [Status], [IsActive]);
GO


CREATE INDEX [IX_CertificateProductIds_ProductIdType] ON [CertificateProductIds] ([ProductIdType]);
GO


CREATE UNIQUE INDEX [IX_CertificateProductIds_ProductIdType_ProductIdValue] ON [CertificateProductIds] ([ProductIdType], [ProductIdValue]);
GO


CREATE INDEX [IX_CertificateProductIds_ProductIdValue] ON [CertificateProductIds] ([ProductIdValue]);
GO


CREATE INDEX [IX_CertificateProductIds_Status] ON [CertificateProductIds] ([Status]);
GO


CREATE INDEX [IX_CertificateProductIds_Status_IsActive_CreatedAt] ON [CertificateProductIds] ([Status], [IsActive], [CreatedAt]);
GO


CREATE INDEX [IX_CertificateProductIds_UpdatedBy] ON [CertificateProductIds] ([UpdatedBy]);
GO


CREATE INDEX [IX_Certificates_CertificationTypeId] ON [Certificates] ([CertificationTypeId]);
GO


CREATE INDEX [IX_Certificates_CertificationTypeId_Status] ON [Certificates] ([CertificationTypeId], [Status]);
GO


CREATE INDEX [IX_Certificates_CompanyId] ON [Certificates] ([CompanyId]);
GO


CREATE INDEX [IX_Certificates_CompanyId_Status] ON [Certificates] ([CompanyId], [Status]);
GO


CREATE INDEX [IX_Certificates_Country] ON [Certificates] ([Country]);
GO


CREATE INDEX [IX_Certificates_Country_Status] ON [Certificates] ([Country], [Status]);
GO


CREATE INDEX [IX_Certificates_CreatedAt] ON [Certificates] ([CreatedAt]);
GO


CREATE INDEX [IX_Certificates_CreatedBy] ON [Certificates] ([CreatedBy]);
GO


CREATE INDEX [IX_Certificates_EffectiveDate] ON [Certificates] ([EffectiveDate]);
GO


CREATE INDEX [IX_Certificates_ExpirationDate] ON [Certificates] ([ExpirationDate]);
GO


CREATE INDEX [IX_Certificates_ExpirationDate_Status_CompanyId] ON [Certificates] ([ExpirationDate], [Status], [CompanyId]);
GO


CREATE UNIQUE INDEX [IX_Certificates_Number] ON [Certificates] ([Number]);
GO


CREATE INDEX [IX_Certificates_ProductId] ON [Certificates] ([ProductId]);
GO


CREATE INDEX [IX_Certificates_ProductId_Status] ON [Certificates] ([ProductId], [Status]);
GO


CREATE INDEX [IX_Certificates_Status] ON [Certificates] ([Status]);
GO


CREATE INDEX [IX_Certificates_Status_ExpirationDate] ON [Certificates] ([Status], [ExpirationDate]);
GO


CREATE INDEX [IX_Certificates_UpdatedBy] ON [Certificates] ([UpdatedBy]);
GO


CREATE INDEX [IX_CertificateTag_TagsId] ON [CertificateTag] ([TagsId]);
GO


CREATE UNIQUE INDEX [IX_CertificationTypes_Name] ON [CertificationTypes] ([Name]);
GO


CREATE INDEX [IX_Companies_Country] ON [Companies] ([Country]);
GO


CREATE INDEX [IX_Companies_Country_IsActive] ON [Companies] ([Country], [IsActive]);
GO


CREATE INDEX [IX_Companies_CreatedAt] ON [Companies] ([CreatedAt]);
GO


CREATE INDEX [IX_Companies_IsActive] ON [Companies] ([IsActive]);
GO


CREATE UNIQUE INDEX [IX_Companies_Name] ON [Companies] ([Name]);
GO


CREATE INDEX [IX_Companies_Name_Country] ON [Companies] ([Name], [Country]);
GO


CREATE INDEX [IX_Companies_Website] ON [Companies] ([Website]);
GO


CREATE INDEX [IX_CompanyUser_UsersId] ON [CompanyUser] ([UsersId]);
GO


CREATE INDEX [IX_CustomFields_CertificateTypeId] ON [CustomFields] ([CertificateTypeId]);
GO


CREATE INDEX [IX_CustomFieldValues_CertificateId] ON [CustomFieldValues] ([CertificateId]);
GO


CREATE INDEX [IX_CustomFieldValues_FieldId] ON [CustomFieldValues] ([FieldId]);
GO


CREATE INDEX [IX_ETLAlerts_CreatedAt] ON [ETLAlerts] ([CreatedAt]);
GO


CREATE INDEX [IX_ETLAlerts_IsResolved] ON [ETLAlerts] ([IsResolved]);
GO


CREATE INDEX [IX_ETLConfigurations_Name] ON [ETLConfigurations] ([Name]);
GO


CREATE INDEX [IX_ETLDataQualityIssues_DetectedAt] ON [ETLDataQualityIssues] ([DetectedAt]);
GO


CREATE INDEX [IX_ETLDataQualityIssues_IssueType] ON [ETLDataQualityIssues] ([IssueType]);
GO


CREATE INDEX [IX_ETLLogEntries_JobId] ON [ETLLogEntries] ([JobId]);
GO


CREATE INDEX [IX_ETLLogEntries_Timestamp] ON [ETLLogEntries] ([Timestamp]);
GO


CREATE INDEX [IX_Partners_CreatedAt] ON [Partners] ([CreatedAt]);
GO


CREATE INDEX [IX_Partners_IsActive] ON [Partners] ([IsActive]);
GO


CREATE INDEX [IX_Partners_IsActive_SortOrder] ON [Partners] ([IsActive], [SortOrder]);
GO


CREATE UNIQUE INDEX [IX_Partners_Name] ON [Partners] ([Name]);
GO


CREATE INDEX [IX_Partners_SortOrder] ON [Partners] ([SortOrder]);
GO


CREATE INDEX [IX_PartnerUser_UsersId] ON [PartnerUser] ([UsersId]);
GO


CREATE INDEX [IX_PermissionRole_RolesId] ON [PermissionRole] ([RolesId]);
GO


CREATE UNIQUE INDEX [IX_Permissions_Codename] ON [Permissions] ([Codename]);
GO


CREATE UNIQUE INDEX [IX_ProductCategories_Name] ON [ProductCategories] ([Name]);
GO


CREATE UNIQUE INDEX [IX_ProductIdentifiers_ProductId_IdentifierType_IdentifierValue] ON [ProductIdentifiers] ([ProductId], [IdentifierType], [IdentifierValue]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE INDEX [IX_Products_CategoryId_CompanyId] ON [Products] ([CategoryId], [CompanyId]);
GO


CREATE INDEX [IX_Products_CompanyId] ON [Products] ([CompanyId]);
GO


CREATE INDEX [IX_Products_CompanyId_IsActive] ON [Products] ([CompanyId], [IsActive]);
GO


CREATE INDEX [IX_Products_CreatedAt] ON [Products] ([CreatedAt]);
GO


CREATE INDEX [IX_Products_IsActive] ON [Products] ([IsActive]);
GO


CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
GO


CREATE INDEX [IX_Products_ProductSubcategoryId] ON [Products] ([ProductSubcategoryId]);
GO


CREATE INDEX [IX_Products_ProductTypeId] ON [Products] ([ProductTypeId]);
GO


CREATE INDEX [IX_Products_ProductTypeId_IsActive] ON [Products] ([ProductTypeId], [IsActive]);
GO


CREATE INDEX [IX_ProductSubcategories_ProductTypeId] ON [ProductSubcategories] ([ProductTypeId]);
GO


CREATE INDEX [IX_ProductTag_TagsId] ON [ProductTag] ([TagsId]);
GO


CREATE INDEX [IX_ProductTypes_CategoryId] ON [ProductTypes] ([CategoryId]);
GO


CREATE INDEX [IX_ProductTypes_ParentId] ON [ProductTypes] ([ParentId]);
GO


CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
GO


CREATE INDEX [IX_RoleUser_UsersId] ON [RoleUser] ([UsersId]);
GO


CREATE UNIQUE INDEX [IX_Tags_Name] ON [Tags] ([Name]);
GO


CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
GO


CREATE INDEX [IX_Users_IsActive_CreatedAt] ON [Users] ([IsActive], [CreatedAt]);
GO


CREATE INDEX [IX_Users_LastLoginAt] ON [Users] ([LastLoginAt]);
GO


CREATE INDEX [IX_Users_Status] ON [Users] ([Status]);
GO


CREATE INDEX [IX_Users_Status_IsActive] ON [Users] ([Status], [IsActive]);
GO


CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO



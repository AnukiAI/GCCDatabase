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

CREATE TABLE [Certificates] (
    [Id] INTEGER NOT NULL,
    [Number] TEXT NOT NULL,
    [Type] TEXT NOT NULL,
    [Company] TEXT NULL,
    [Product] TEXT NULL,
    [Category] TEXT NULL,
    [EffectiveDate] TEXT NOT NULL,
    [ExpirationDate] TEXT NOT NULL,
    [Location] TEXT NULL,
    [Country] TEXT NULL,
    CONSTRAINT [PK_Certificates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Companies] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Location] TEXT NULL,
    [Country] TEXT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Products] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Description] TEXT NULL,
    [Category] TEXT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250808131552_InitialCreate', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Category');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Products] DROP COLUMN [Category];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Certificates]') AND [c].[name] = N'Category');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Certificates] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Certificates] DROP COLUMN [Category];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Certificates]') AND [c].[name] = N'Company');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Certificates] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Certificates] DROP COLUMN [Company];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Certificates]') AND [c].[name] = N'Product');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Certificates] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Certificates] DROP COLUMN [Product];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Certificates]') AND [c].[name] = N'Type');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Certificates] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Certificates] DROP COLUMN [Type];
GO

ALTER TABLE [Products] ADD [CategoryId] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [Products] ADD [CompanyId] INTEGER NULL;
GO

ALTER TABLE [Products] ADD [CreatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Products] ADD [IsActive] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Products] ADD [ProductSubcategoryId] INTEGER NULL;
GO

ALTER TABLE [Products] ADD [ProductTypeId] INTEGER NULL;
GO

ALTER TABLE [Products] ADD [UpdatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Companies] ADD [ContactInfo] TEXT NULL;
GO

ALTER TABLE [Companies] ADD [CreatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Companies] ADD [IsActive] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Companies] ADD [UpdatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Certificates] ADD [Asin] TEXT NULL;
GO

ALTER TABLE [Certificates] ADD [CertificateFile] TEXT NULL;
GO

ALTER TABLE [Certificates] ADD [CertificationTypeId] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [Certificates] ADD [CompanyId] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [Certificates] ADD [CreatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Certificates] ADD [CreatedBy] INTEGER NULL;
GO

ALTER TABLE [Certificates] ADD [Ean] TEXT NULL;
GO

ALTER TABLE [Certificates] ADD [Gtin] TEXT NULL;
GO

ALTER TABLE [Certificates] ADD [ProductId] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [Certificates] ADD [Status] TEXT NOT NULL DEFAULT '';
GO

ALTER TABLE [Certificates] ADD [Upc] TEXT NULL;
GO

ALTER TABLE [Certificates] ADD [UpdatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Certificates] ADD [UpdatedBy] INTEGER NULL;
GO

CREATE TABLE [CertificationTypes] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [AmazonCode] TEXT NULL,
    [Description] TEXT NULL,
    [IsActive] INTEGER NOT NULL,
    [SortOrder] INTEGER NOT NULL,
    CONSTRAINT [PK_CertificationTypes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Permissions] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Codename] TEXT NOT NULL,
    [Category] TEXT NULL,
    [Description] TEXT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ProductCategories] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Description] TEXT NULL,
    [SortOrder] INTEGER NOT NULL,
    [IsActive] INTEGER NOT NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Roles] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Description] TEXT NULL,
    [IsSystem] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Tags] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [TagType] TEXT NOT NULL,
    [Description] TEXT NULL,
    [IsActive] INTEGER NOT NULL,
    CONSTRAINT [PK_Tags] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] INTEGER NOT NULL,
    [Username] TEXT NOT NULL,
    [Email] TEXT NOT NULL,
    [Name] TEXT NULL,
    [PasswordHash] TEXT NOT NULL,
    [Status] TEXT NOT NULL,
    [IsActive] INTEGER NOT NULL,
    [IsStaff] INTEGER NOT NULL,
    [IsSuperuser] INTEGER NOT NULL,
    [RequirePasswordReset] INTEGER NOT NULL,
    [LastLogin] TEXT NULL,
    [PasswordChangedAt] TEXT NULL,
    [FailedLoginAttempts] INTEGER NOT NULL,
    [LockedUntil] TEXT NULL,
    [CreatedAt] TEXT NOT NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CustomFields] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [FieldType] TEXT NOT NULL,
    [IsRequired] INTEGER NOT NULL,
    [IsVisible] INTEGER NOT NULL,
    [IsPublic] INTEGER NOT NULL,
    [SortOrder] INTEGER NOT NULL,
    [Options] TEXT NULL,
    [DefaultValue] TEXT NULL,
    [ValidationRules] TEXT NULL,
    [CertificateTypeId] INTEGER NOT NULL,
    CONSTRAINT [PK_CustomFields] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomFields_CertificationTypes_CertificateTypeId] FOREIGN KEY ([CertificateTypeId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductTypes] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [CategoryId] INTEGER NOT NULL,
    [ParentId] INTEGER NULL,
    [Level] INTEGER NOT NULL,
    [SortOrder] INTEGER NOT NULL,
    [IsActive] INTEGER NOT NULL,
    CONSTRAINT [PK_ProductTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductTypes_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductTypes_ProductTypes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ProductTypes] ([Id])
);
GO

CREATE TABLE [PermissionRole] (
    [PermissionsId] INTEGER NOT NULL,
    [RolesId] INTEGER NOT NULL,
    CONSTRAINT [PK_PermissionRole] PRIMARY KEY ([PermissionsId], [RolesId]),
    CONSTRAINT [FK_PermissionRole_Permissions_PermissionsId] FOREIGN KEY ([PermissionsId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PermissionRole_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CertificateTag] (
    [CertificatesId] INTEGER NOT NULL,
    [TagsId] INTEGER NOT NULL,
    CONSTRAINT [PK_CertificateTag] PRIMARY KEY ([CertificatesId], [TagsId]),
    CONSTRAINT [FK_CertificateTag_Certificates_CertificatesId] FOREIGN KEY ([CertificatesId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CertificateTag_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductTag] (
    [ProductsId] INTEGER NOT NULL,
    [TagsId] INTEGER NOT NULL,
    CONSTRAINT [PK_ProductTag] PRIMARY KEY ([ProductsId], [TagsId]),
    CONSTRAINT [FK_ProductTag_Products_ProductsId] FOREIGN KEY ([ProductsId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductTag_Tags_TagsId] FOREIGN KEY ([TagsId]) REFERENCES [Tags] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CompanyUser] (
    [CompaniesId] INTEGER NOT NULL,
    [UsersId] INTEGER NOT NULL,
    CONSTRAINT [PK_CompanyUser] PRIMARY KEY ([CompaniesId], [UsersId]),
    CONSTRAINT [FK_CompanyUser_Companies_CompaniesId] FOREIGN KEY ([CompaniesId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CompanyUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RoleUser] (
    [RolesId] INTEGER NOT NULL,
    [UsersId] INTEGER NOT NULL,
    CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
    CONSTRAINT [FK_RoleUser_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CustomFieldValues] (
    [Id] INTEGER NOT NULL,
    [CertificateId] INTEGER NOT NULL,
    [FieldId] INTEGER NOT NULL,
    [TextValue] TEXT NULL,
    [NumberValue] decimal(18,4) NULL,
    [DateValue] TEXT NULL,
    [BooleanValue] INTEGER NULL,
    CONSTRAINT [PK_CustomFieldValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CustomFieldValues_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CustomFieldValues_CustomFields_FieldId] FOREIGN KEY ([FieldId]) REFERENCES [CustomFields] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductSubcategories] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [ProductTypeId] INTEGER NOT NULL,
    [Description] TEXT NULL,
    [SortOrder] INTEGER NOT NULL,
    [IsActive] INTEGER NOT NULL,
    CONSTRAINT [PK_ProductSubcategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductSubcategories_ProductTypes_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO

CREATE INDEX [IX_Products_CompanyId] ON [Products] ([CompanyId]);
GO

CREATE INDEX [IX_Products_ProductSubcategoryId] ON [Products] ([ProductSubcategoryId]);
GO

CREATE INDEX [IX_Products_ProductTypeId] ON [Products] ([ProductTypeId]);
GO

CREATE UNIQUE INDEX [IX_Companies_Name] ON [Companies] ([Name]);
GO

CREATE INDEX [IX_Certificates_CertificationTypeId] ON [Certificates] ([CertificationTypeId]);
GO

CREATE INDEX [IX_Certificates_CompanyId] ON [Certificates] ([CompanyId]);
GO

CREATE INDEX [IX_Certificates_CreatedBy] ON [Certificates] ([CreatedBy]);
GO

CREATE UNIQUE INDEX [IX_Certificates_Number] ON [Certificates] ([Number]);
GO

CREATE INDEX [IX_Certificates_ProductId] ON [Certificates] ([ProductId]);
GO

CREATE INDEX [IX_Certificates_UpdatedBy] ON [Certificates] ([UpdatedBy]);
GO

CREATE INDEX [IX_CertificateTag_TagsId] ON [CertificateTag] ([TagsId]);
GO

CREATE UNIQUE INDEX [IX_CertificationTypes_Name] ON [CertificationTypes] ([Name]);
GO

CREATE INDEX [IX_CompanyUser_UsersId] ON [CompanyUser] ([UsersId]);
GO

CREATE INDEX [IX_CustomFields_CertificateTypeId] ON [CustomFields] ([CertificateTypeId]);
GO

CREATE INDEX [IX_CustomFieldValues_CertificateId] ON [CustomFieldValues] ([CertificateId]);
GO

CREATE INDEX [IX_CustomFieldValues_FieldId] ON [CustomFieldValues] ([FieldId]);
GO

CREATE INDEX [IX_PermissionRole_RolesId] ON [PermissionRole] ([RolesId]);
GO

CREATE UNIQUE INDEX [IX_Permissions_Codename] ON [Permissions] ([Codename]);
GO

CREATE UNIQUE INDEX [IX_ProductCategories_Name] ON [ProductCategories] ([Name]);
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

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO

ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_CertificationTypes_CertificationTypeId] FOREIGN KEY ([CertificationTypeId]) REFERENCES [CertificationTypes] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]);
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE;
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ProductSubcategories_ProductSubcategoryId] FOREIGN KEY ([ProductSubcategoryId]) REFERENCES [ProductSubcategories] ([Id]);
GO

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ProductTypes_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250808134211_UpdatedDatabaseSchema', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250808143700_InitialMigration', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [DeletedAt] TEXT NULL;
GO

ALTER TABLE [Users] ADD [FullName] TEXT NULL;
GO

ALTER TABLE [Users] ADD [IsDeleted] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Users] ADD [LastLoginAt] TEXT NULL;
GO

ALTER TABLE [Roles] ADD [IsActive] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Permissions] ADD [CreatedAt] TEXT NOT NULL DEFAULT '0001-01-01 00:00:00';
GO

ALTER TABLE [Permissions] ADD [IsActive] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250808160522_AdminEnhancements', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Products] ADD [Asin] TEXT NULL;
GO

ALTER TABLE [Products] ADD [Model] TEXT NULL;
GO

ALTER TABLE [Products] ADD [Sku] TEXT NULL;
GO

ALTER TABLE [Products] ADD [Upc] TEXT NULL;
GO

CREATE TABLE [CertificateAggregates] (
    [Period] TEXT NOT NULL,
    [PeriodDate] TEXT NOT NULL,
    [TotalCertificates] INTEGER NOT NULL,
    [ActiveCertificates] INTEGER NOT NULL,
    [ExpiredCertificates] INTEGER NOT NULL,
    [ExpiringThisMonth] INTEGER NOT NULL,
    [CertificatesByType] TEXT NOT NULL,
    [CertificatesByCountry] TEXT NOT NULL,
    [CertificatesByCompany] TEXT NOT NULL,
    [AverageValidityPeriod] REAL NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_CertificateAggregates] PRIMARY KEY ([Period], [PeriodDate])
);
GO

CREATE TABLE [ETLAlerts] (
    [Id] TEXT NOT NULL,
    [AlertType] TEXT NOT NULL,
    [Message] TEXT NOT NULL,
    [Severity] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    [IsResolved] INTEGER NOT NULL,
    [ResolvedAt] TEXT NULL,
    [Context] TEXT NOT NULL,
    CONSTRAINT [PK_ETLAlerts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ETLConfigurations] (
    [Id] TEXT NOT NULL,
    [Name] TEXT NOT NULL,
    [JobType] INTEGER NOT NULL,
    [IsIncremental] INTEGER NOT NULL,
    [LastRunTime] TEXT NULL,
    [DataSources] TEXT NOT NULL,
    [Transformations] TEXT NOT NULL,
    [Destinations] TEXT NOT NULL,
    [Settings] TEXT NOT NULL,
    [IsActive] INTEGER NOT NULL,
    [CronSchedule] TEXT NULL,
    [CreatedAt] TEXT NOT NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_ETLConfigurations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ETLDataQualityIssues] (
    [Id] TEXT NOT NULL,
    [IssueType] TEXT NOT NULL,
    [Description] TEXT NOT NULL,
    [FieldName] TEXT NOT NULL,
    [RecordId] TEXT NULL,
    [ExpectedValue] TEXT NULL,
    [ActualValue] TEXT NULL,
    [Severity] INTEGER NOT NULL,
    [DetectedAt] TEXT NOT NULL,
    CONSTRAINT [PK_ETLDataQualityIssues] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ETLLogEntries] (
    [Id] TEXT NOT NULL,
    [JobId] TEXT NOT NULL,
    [Timestamp] TEXT NOT NULL,
    [LogLevel] TEXT NOT NULL,
    [Message] TEXT NOT NULL,
    [Category] TEXT NULL,
    [Properties] TEXT NOT NULL,
    CONSTRAINT [PK_ETLLogEntries] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_CertificateAggregates_PeriodDate] ON [CertificateAggregates] ([PeriodDate]);
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250810151650_UpdateETLConfiguration', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ProductIdentifiers] (
    [Id] INTEGER NOT NULL,
    [ProductId] INTEGER NOT NULL,
    [IdentifierType] TEXT NOT NULL,
    [IdentifierValue] TEXT NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_ProductIdentifiers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductIdentifiers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_ProductIdentifiers_ProductId_IdentifierType_IdentifierValue] ON [ProductIdentifiers] ([ProductId], [IdentifierType], [IdentifierValue]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250811142726_AddProductIdentifiers', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ApiKeys] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Key] TEXT NOT NULL,
    [Description] TEXT NOT NULL,
    [IsActive] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    [LastUsedAt] TEXT NULL,
    [ExpiresAt] TEXT NULL,
    [CreatedBy] TEXT NOT NULL,
    [RateLimitPerMinute] INTEGER NULL,
    [RateLimitPerHour] INTEGER NULL,
    [AllowedIPs] TEXT NOT NULL,
    [AllowedOrigins] TEXT NOT NULL,
    [Scopes] TEXT NOT NULL,
    CONSTRAINT [PK_ApiKeys] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CertificateDocuments] (
    [Id] INTEGER NOT NULL,
    [FileName] TEXT NOT NULL,
    [OriginalFileName] TEXT NOT NULL,
    [FilePath] TEXT NOT NULL,
    [ContentType] TEXT NOT NULL,
    [FileSize] INTEGER NOT NULL,
    [FileHash] TEXT NULL,
    [DocumentType] TEXT NOT NULL,
    [Description] TEXT NULL,
    [Version] INTEGER NOT NULL,
    [IsActive] INTEGER NOT NULL,
    [IsPublic] INTEGER NOT NULL,
    [IsVirusScanned] INTEGER NOT NULL,
    [PassedVirusScan] INTEGER NOT NULL,
    [VirusScanDate] TEXT NULL,
    [CertificateId] INTEGER NOT NULL,
    [UploadedBy] INTEGER NULL,
    [UploadedAt] TEXT NOT NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_CertificateDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CertificateDocuments_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CertificateDocuments_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_CertificateDocuments_CertificateId] ON [CertificateDocuments] ([CertificateId]);
GO

CREATE INDEX [IX_CertificateDocuments_FileHash] ON [CertificateDocuments] ([FileHash]);
GO

CREATE INDEX [IX_CertificateDocuments_UploadedBy] ON [CertificateDocuments] ([UploadedBy]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250813031456_FixModelIssues', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [AllowedIPs] TEXT NULL;
GO

ALTER TABLE [Users] ADD [IsEmailVerified] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Users] ADD [IsPhoneVerified] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Users] ADD [LastSecurityAudit] TEXT NULL;
GO

ALTER TABLE [Users] ADD [MaxConcurrentSessions] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [Users] ADD [MaxPasswordHistory] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [Users] ADD [MustChangePassword] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Users] ADD [PasswordExpiresAt] TEXT NULL;
GO

ALTER TABLE [Users] ADD [PasswordHistory] TEXT NULL;
GO

ALTER TABLE [Users] ADD [PhoneNumber] TEXT NULL;
GO

ALTER TABLE [Users] ADD [RecoveryCodes] TEXT NULL;
GO

ALTER TABLE [Users] ADD [RequirePasswordChange] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Users] ADD [SecurityQuestions] TEXT NULL;
GO

ALTER TABLE [Users] ADD [TwoFactorEnabled] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Users] ADD [TwoFactorMethod] TEXT NOT NULL DEFAULT '';
GO

ALTER TABLE [Users] ADD [TwoFactorSecret] TEXT NULL;
GO

ALTER TABLE [Products] ADD [IsDeleted] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Companies] ADD [Email] TEXT NULL;
GO

ALTER TABLE [Companies] ADD [IsDeleted] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [Companies] ADD [Website] TEXT NULL;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'Scopes');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [ApiKeys] ALTER COLUMN [Scopes] TEXT NULL;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'Description');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [ApiKeys] ALTER COLUMN [Description] TEXT NULL;
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'CreatedBy');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [ApiKeys] ALTER COLUMN [CreatedBy] TEXT NULL;
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'AllowedOrigins');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [ApiKeys] ALTER COLUMN [AllowedOrigins] TEXT NULL;
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ApiKeys]') AND [c].[name] = N'AllowedIPs');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ApiKeys] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [ApiKeys] ALTER COLUMN [AllowedIPs] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [BlockReason] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [BlockedUntil] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [Environment] TEXT NOT NULL DEFAULT '';
GO

ALTER TABLE [ApiKeys] ADD [IsTemporarilyBlocked] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [ApiKeys] ADD [KeyHash] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [LastRotatedAt] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [LastSecurityIncident] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [LastViolationAt] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [LastViolationType] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [MaxConcurrentRequests] INTEGER NULL;
GO

ALTER TABLE [ApiKeys] ADD [RateLimit] INTEGER NULL;
GO

ALTER TABLE [ApiKeys] ADD [RateLimitPerDay] INTEGER NULL;
GO

ALTER TABLE [ApiKeys] ADD [RequestCount] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [ApiKeys] ADD [RequireTLS] INTEGER NOT NULL DEFAULT CAST(0 AS INTEGER);
GO

ALTER TABLE [ApiKeys] ADD [SecurityViolationCount] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [ApiKeys] ADD [SecurityViolations] INTEGER NOT NULL DEFAULT 0;
GO

ALTER TABLE [ApiKeys] ADD [UsageCostPerRequest] TEXT NULL;
GO

ALTER TABLE [ApiKeys] ADD [UserAgent] TEXT NULL;
GO

CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);
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

CREATE INDEX [IX_Products_CategoryId_CompanyId] ON [Products] ([CategoryId], [CompanyId]);
GO

CREATE INDEX [IX_Products_CompanyId_IsActive] ON [Products] ([CompanyId], [IsActive]);
GO

CREATE INDEX [IX_Products_CreatedAt] ON [Products] ([CreatedAt]);
GO

CREATE INDEX [IX_Products_IsActive] ON [Products] ([IsActive]);
GO

CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
GO

CREATE INDEX [IX_Products_ProductTypeId_IsActive] ON [Products] ([ProductTypeId], [IsActive]);
GO

CREATE INDEX [IX_Companies_Country] ON [Companies] ([Country]);
GO

CREATE INDEX [IX_Companies_Country_IsActive] ON [Companies] ([Country], [IsActive]);
GO

CREATE INDEX [IX_Companies_CreatedAt] ON [Companies] ([CreatedAt]);
GO

CREATE INDEX [IX_Companies_IsActive] ON [Companies] ([IsActive]);
GO

CREATE INDEX [IX_Companies_Name_Country] ON [Companies] ([Name], [Country]);
GO

CREATE INDEX [IX_Companies_Website] ON [Companies] ([Website]);
GO

CREATE INDEX [IX_Certificates_CertificationTypeId_Status] ON [Certificates] ([CertificationTypeId], [Status]);
GO

CREATE INDEX [IX_Certificates_CompanyId_Status] ON [Certificates] ([CompanyId], [Status]);
GO

CREATE INDEX [IX_Certificates_Country] ON [Certificates] ([Country]);
GO

CREATE INDEX [IX_Certificates_Country_Status] ON [Certificates] ([Country], [Status]);
GO

CREATE INDEX [IX_Certificates_CreatedAt] ON [Certificates] ([CreatedAt]);
GO

CREATE INDEX [IX_Certificates_EffectiveDate] ON [Certificates] ([EffectiveDate]);
GO

CREATE INDEX [IX_Certificates_ExpirationDate] ON [Certificates] ([ExpirationDate]);
GO

CREATE INDEX [IX_Certificates_ExpirationDate_Status_CompanyId] ON [Certificates] ([ExpirationDate], [Status], [CompanyId]);
GO

CREATE INDEX [IX_Certificates_ProductId_Status] ON [Certificates] ([ProductId], [Status]);
GO

CREATE INDEX [IX_Certificates_Status] ON [Certificates] ([Status]);
GO

CREATE INDEX [IX_Certificates_Status_ExpirationDate] ON [Certificates] ([Status], [ExpirationDate]);
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

CREATE UNIQUE INDEX [IX_ApiKeys_KeyHash] ON [ApiKeys] ([KeyHash]);
GO

CREATE INDEX [IX_ApiKeys_KeyHash_IsActive_ExpiresAt] ON [ApiKeys] ([KeyHash], [IsActive], [ExpiresAt]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250827043046_AddPerformanceIndexes', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Certificates] ADD [Partner] TEXT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250827182202_AddPartnerFieldToCertificate', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Partners] (
    [Id] INTEGER NOT NULL,
    [Name] TEXT NOT NULL,
    [Description] TEXT NULL,
    [Website] TEXT NULL,
    [ContactInfo] TEXT NULL,
    [Email] TEXT NULL,
    [IsActive] INTEGER NOT NULL,
    [IsDeleted] INTEGER NOT NULL,
    [SortOrder] INTEGER NOT NULL,
    [CreatedAt] TEXT NOT NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_Partners] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PartnerUser] (
    [PartnersId] INTEGER NOT NULL,
    [UsersId] INTEGER NOT NULL,
    CONSTRAINT [PK_PartnerUser] PRIMARY KEY ([PartnersId], [UsersId]),
    CONSTRAINT [FK_PartnerUser_Partners_PartnersId] FOREIGN KEY ([PartnersId]) REFERENCES [Partners] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PartnerUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250909010923_AddPartnerEntity', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [CertificateProductIds] (
    [Id] INTEGER NOT NULL,
    [CertificateId] INTEGER NOT NULL,
    [ProductIdType] TEXT NOT NULL,
    [ProductIdValue] TEXT NOT NULL,
    [Status] TEXT NOT NULL,
    [Partner] TEXT NULL,
    [ProductName] TEXT NULL,
    [ProductSku] TEXT NULL,
    [Description] TEXT NULL,
    [IsActive] INTEGER NOT NULL,
    [IsPublic] INTEGER NOT NULL,
    [DisplayOrder] INTEGER NULL,
    [CreatedBy] INTEGER NULL,
    [CreatedAt] TEXT NOT NULL,
    [UpdatedBy] INTEGER NULL,
    [UpdatedAt] TEXT NOT NULL,
    CONSTRAINT [PK_CertificateProductIds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CertificateProductIds_Certificates_CertificateId] FOREIGN KEY ([CertificateId]) REFERENCES [Certificates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CertificateProductIds_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_CertificateProductIds_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
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

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250918212035_AddCertificateProductIds', N'8.0.11');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Certificates] ADD [CertificateImagePath] TEXT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251006162626_AddCertificateImagePath', N'8.0.11');
GO

COMMIT;
GO


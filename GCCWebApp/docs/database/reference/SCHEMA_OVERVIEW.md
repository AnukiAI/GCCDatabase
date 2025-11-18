# GCC Database Schema Overview

**Last Updated:** 2025-11-17
**Database Version:** SQL Server 2019+
**EF Core Version:** 9.0.9
**Total Tables:** 34+ (27 primary entities + 6 junction tables + aggregate tables)

---

## Table of Contents

1. [Core Certificate Management](#core-certificate-management)
2. [Product Management](#product-management)
3. [Organization Management](#organization-management)
4. [User & Authentication](#user--authentication)
5. [Tags & Attributes](#tags--attributes)
6. [API & Integration](#api--integration)
7. [Settings](#settings)
8. [ETL & Data Processing](#etl--data-processing)
9. [Junction Tables](#junction-tables)
10. [Indexes & Performance](#indexes--performance)

---

## Core Certificate Management

### Certificates
**Primary Table:** Certificate management with 14-state workflow

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Number | nvarchar(7) | Unique, Required | Certificate number (e.g., "24-0366") |
| Status | nvarchar(50) | Required | 14-state workflow status |
| EffectiveDate | datetime2 | Required | Certificate start date |
| ExpirationDate | datetime2 | Required | Certificate end date |
| Location | nvarchar(max) | Nullable | Certificate location |
| Country | nvarchar(max) | Nullable | Country of certification |
| Partner | nvarchar(100) | Nullable | Partner: Amazon, Wayfair, None |
| CertificateFile | nvarchar(max) | Nullable | Uploaded certificate file path |
| CertificateImagePath | nvarchar(max) | Nullable | Certificate preview image path |
| CompanyId | int | FK | Foreign key to Companies |
| ProductId | int | FK | Foreign key to Products |
| CertificationTypeId | int | FK | Foreign key to CertificationTypes |
| CreatedBy | int | FK | Foreign key to Users |
| UpdatedBy | int | FK | Foreign key to Users |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on Number
- INDEX on (Status, ExpirationDate)
- INDEX on (CompanyId, Status)
- INDEX on (ProductId, Status)
- INDEX on (Country, Status)
- INDEX on (CertificationTypeId, Status)
- INDEX on (ExpirationDate, Status, CompanyId)

**Relationships:**
- Certificate → Company (Many-to-One)
- Certificate → Product (Many-to-One)
- Certificate → CertificationType (Many-to-One)
- Certificate → User (CreatedBy, Many-to-One)
- Certificate → User (UpdatedBy, Many-to-One)
- Certificate → CertificateProductId (One-to-Many, Cascade Delete)
- Certificate → CertificateDocument (One-to-Many, Cascade Delete)
- Certificate → CustomFieldValue (One-to-Many)
- Certificate → Tag (Many-to-Many via CertificateTag)

---

### CertificateProductIds
**Purpose:** Multiple product identifiers (ASINs, UPCs, GTINs, etc.) per certificate

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| CertificateId | int | FK, Required | Foreign key to Certificates |
| ProductIdType | nvarchar(10) | Required | Type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| ProductIdValue | nvarchar(50) | Required | Product identifier value |
| Status | nvarchar(max) | Nullable | Product ID status |
| Partner | nvarchar(max) | Nullable | Associated partner |
| ProductName | nvarchar(max) | Nullable | Product name |
| ProductSku | nvarchar(max) | Nullable | Product SKU |
| Description | nvarchar(max) | Nullable | Product description |
| IsActive | bit | Required, Default: 1 | Active flag |
| IsPublic | bit | Required, Default: 1 | Public visibility flag |
| DisplayOrder | int | Required, Default: 0 | Sort order |
| CreatedBy | int | FK | Foreign key to Users |
| UpdatedBy | int | FK | Foreign key to Users |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on (ProductIdType, ProductIdValue)
- INDEX on (CertificateId, ProductIdType)
- INDEX on (Partner, Status, IsActive)
- INDEX on (CertificateId, IsActive)
- INDEX on (Status, IsActive, CreatedAt)

**Relationships:**
- CertificateProductId → Certificate (Many-to-One, Cascade Delete)
- CertificateProductId → User (CreatedBy/UpdatedBy, Many-to-One)

---

### CertificateDocuments
**Purpose:** File attachments for certificates (PDFs, images, etc.)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| CertificateId | int | FK, Required | Foreign key to Certificates |
| FileName | nvarchar(max) | Required | Stored file name |
| OriginalFileName | nvarchar(max) | Required | Original uploaded file name |
| FilePath | nvarchar(max) | Required | File storage path |
| ContentType | nvarchar(max) | Required | MIME type |
| FileSize | bigint | Required | File size in bytes |
| FileHash | nvarchar(max) | Nullable | SHA256 hash for integrity |
| DocumentType | nvarchar(max) | Nullable | Document type/category |
| Description | nvarchar(max) | Nullable | Document description |
| Version | int | Required, Default: 1 | Document version |
| IsActive | bit | Required, Default: 1 | Active flag |
| IsPublic | bit | Required, Default: 0 | Public visibility |
| IsVirusScanned | bit | Required, Default: 0 | Virus scan status |
| PassedVirusScan | bit | Nullable | Scan result |
| VirusScanDate | datetime2 | Nullable | Scan timestamp |
| UploadedBy | int | FK | Foreign key to Users |
| UploadedAt | datetime2 | Required | Upload timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- INDEX on CertificateId
- INDEX on FileHash

**Relationships:**
- CertificateDocument → Certificate (Many-to-One, Cascade Delete)
- CertificateDocument → User (UploadedBy, Many-to-One)

---

### CertificationTypes
**Purpose:** Certificate type taxonomy with hierarchical support

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Unique, Required | Certification type name |
| AmazonCode | nvarchar(max) | Nullable | Amazon-specific code |
| Description | nvarchar(max) | Nullable | Type description |
| IsActive | bit | Required, Default: 1 | Active flag |
| SortOrder | int | Required, Default: 0 | Display order |
| Level | int | Required, Default: 1 | Hierarchy level (1=parent, 2=child) |
| ParentId | int | FK, Nullable | Self-referencing parent ID |

**Indexes:**
- UNIQUE INDEX on Name

**Relationships:**
- CertificationType → CertificationType (Self-referencing Parent-Child)
- CertificationType → Certificate (One-to-Many)
- CertificationType → CustomField (One-to-Many)

---

### CustomFields
**Purpose:** Dynamic custom fields per certification type

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(100) | Required | Field name |
| FieldType | nvarchar(max) | Required | Type: text, number, date, boolean, select |
| IsRequired | bit | Required, Default: 0 | Required flag |
| IsVisible | bit | Required, Default: 1 | Visibility flag |
| IsPublic | bit | Required, Default: 0 | Public visibility |
| SortOrder | int | Required, Default: 0 | Display order |
| Options | nvarchar(max) | Nullable | JSON options for select fields |
| DefaultValue | nvarchar(max) | Nullable | Default value |
| ValidationRules | nvarchar(max) | Nullable | JSON validation rules |
| Section | nvarchar(100) | Nullable | Field section/grouping |
| CertificateTypeId | int | FK, Required | Foreign key to CertificationTypes |

**Relationships:**
- CustomField → CertificationType (Many-to-One)
- CustomField → CustomFieldValue (One-to-Many)

---

### CustomFieldValues
**Purpose:** Store values for custom fields on certificates

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| CertificateId | int | FK, Required | Foreign key to Certificates |
| FieldId | int | FK, Required | Foreign key to CustomFields |
| TextValue | nvarchar(max) | Nullable | Text field value |
| NumberValue | decimal(18,4) | Nullable | Numeric field value |
| DateValue | datetime2 | Nullable | Date field value |
| BooleanValue | bit | Nullable | Boolean field value |

**Relationships:**
- CustomFieldValue → Certificate (Many-to-One)
- CustomFieldValue → CustomField (Many-to-One)

---

## Product Management

### Products
**Primary Table:** Product catalog

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Required | Product name |
| Description | nvarchar(max) | Nullable | Product description |
| Model | nvarchar(max) | Nullable | Model number |
| Sku | nvarchar(max) | Nullable | Stock keeping unit |
| Upc | nvarchar(max) | Nullable | Universal product code |
| Asin | nvarchar(max) | Nullable | Amazon standard ID |
| CategoryId | int | FK, Nullable | Foreign key to ProductCategories |
| ProductTypeId | int | FK, Nullable | Foreign key to ProductTypes |
| ProductSubcategoryId | int | FK, Nullable | Foreign key to ProductSubcategories |
| CompanyId | int | FK, Nullable | Foreign key to Companies |
| IsActive | bit | Required, Default: 1 | Active flag |
| IsDeleted | bit | Required, Default: 0 | Soft delete flag |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- INDEX on Name
- INDEX on CategoryId
- INDEX on ProductTypeId
- INDEX on CompanyId
- INDEX on IsActive
- INDEX on CreatedAt
- INDEX on (CategoryId, CompanyId)
- INDEX on (ProductTypeId, IsActive)
- INDEX on (CompanyId, IsActive)

**Relationships:**
- Product → ProductCategory (Many-to-One)
- Product → ProductType (Many-to-One)
- Product → ProductSubcategory (Many-to-One)
- Product → Company (Many-to-One)
- Product → Certificate (One-to-Many)
- Product → ProductIdentifier (One-to-Many, Cascade Delete)
- Product → Tag (Many-to-Many via ProductTag)

---

### ProductCategories
**Purpose:** Top-level product categorization

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Unique, Required | Category name |
| Description | nvarchar(max) | Nullable | Category description |
| SortOrder | int | Required, Default: 0 | Display order |
| IsActive | bit | Required, Default: 1 | Active flag |

**Indexes:**
- UNIQUE INDEX on Name

**Relationships:**
- ProductCategory → Product (One-to-Many)
- ProductCategory → ProductType (One-to-Many)

---

### ProductTypes
**Purpose:** Product type taxonomy with hierarchy

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Required | Product type name |
| CategoryId | int | FK, Nullable | Foreign key to ProductCategories |
| ParentId | int | FK, Nullable | Self-referencing parent ID |
| Level | int | Required | Hierarchy level |
| SortOrder | int | Required, Default: 0 | Display order |
| IsActive | bit | Required, Default: 1 | Active flag |

**Relationships:**
- ProductType → ProductCategory (Many-to-One)
- ProductType → ProductType (Self-referencing Parent-Child)
- ProductType → Product (One-to-Many)
- ProductType → ProductSubcategory (One-to-Many)

---

### ProductSubcategories
**Purpose:** Subcategory classification within product types

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Required | Subcategory name |
| ProductTypeId | int | FK, Required | Foreign key to ProductTypes |
| Description | nvarchar(max) | Nullable | Subcategory description |
| SortOrder | int | Required, Default: 0 | Display order |
| IsActive | bit | Required, Default: 1 | Active flag |

**Relationships:**
- ProductSubcategory → ProductType (Many-to-One)
- ProductSubcategory → Product (One-to-Many)

---

### ProductIdentifiers
**Purpose:** Multiple identifier types per product

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| ProductId | int | FK, Required | Foreign key to Products |
| IdentifierType | nvarchar(20) | Required | Type: ASIN, UPC, GTIN, etc. |
| IdentifierValue | nvarchar(50) | Required | Identifier value |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on (ProductId, IdentifierType, IdentifierValue)

**Relationships:**
- ProductIdentifier → Product (Many-to-One, Cascade Delete)

---

## Organization Management

### Companies
**Purpose:** Company/manufacturer information

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Unique, Required | Company name |
| ContactInfo | nvarchar(max) | Nullable | Contact information |
| Email | nvarchar(max) | Nullable | Email address |
| Website | nvarchar(max) | Nullable | Website URL |
| Location | nvarchar(max) | Nullable | Physical location |
| Country | nvarchar(max) | Nullable | Country |
| IsActive | bit | Required, Default: 1 | Active flag |
| IsDeleted | bit | Required, Default: 0 | Soft delete flag |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on Name
- INDEX on (Country, IsActive)
- INDEX on (Name, Country)

**Relationships:**
- Company → Certificate (One-to-Many)
- Company → Product (One-to-Many)
- Company → User (Many-to-Many via CompanyUser)

---

### Partners
**Purpose:** Partner organizations (Amazon, Wayfair, etc.)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(100) | Unique, Required | Partner name |
| Description | nvarchar(max) | Nullable | Partner description |
| Website | nvarchar(max) | Nullable | Website URL |
| ContactInfo | nvarchar(max) | Nullable | Contact information |
| Email | nvarchar(max) | Nullable | Email address |
| IsActive | bit | Required, Default: 1 | Active flag |
| IsDeleted | bit | Required, Default: 0 | Soft delete flag |
| SortOrder | int | Required, Default: 0 | Display order |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on Name
- INDEX on IsActive
- INDEX on SortOrder
- INDEX on CreatedAt
- INDEX on (IsActive, SortOrder)

**Relationships:**
- Partner → PartnerApiKey (One-to-Many)
- Partner → User (Many-to-Many via UserPartner)

---

## User & Authentication

### Users
**Purpose:** User accounts with authentication and security features

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Username | nvarchar(255) | Unique, Required | Login username |
| Email | nvarchar(255) | Unique, Required | Email address |
| Name | nvarchar(max) | Nullable | Display name |
| FullName | nvarchar(max) | Nullable | Full legal name |
| PasswordHash | nvarchar(max) | Required | Hashed password |
| Status | nvarchar(max) | Nullable | Account status |
| IsActive | bit | Required, Default: 1 | Active flag |
| IsStaff | bit | Required, Default: 0 | Staff flag |
| IsSuperuser | bit | Required, Default: 0 | Superuser flag |
| RequirePasswordReset | bit | Required, Default: 0 | Force password reset |
| PasswordChangedAt | datetime2 | Nullable | Last password change |
| FailedLoginAttempts | int | Required, Default: 0 | Failed login counter |
| LockedUntil | datetime2 | Nullable | Account lockout end time |
| TwoFactorSecret | nvarchar(max) | Nullable | 2FA secret key |
| TwoFactorEnabled | bit | Required, Default: 0 | 2FA enabled flag |
| TwoFactorMethod | nvarchar(max) | Nullable | 2FA method: Email/SMS/TOTP/Both |
| PhoneNumber | nvarchar(max) | Nullable | Phone number for SMS 2FA |
| IsPhoneVerified | bit | Required, Default: 0 | Phone verification status |
| IsEmailVerified | bit | Required, Default: 0 | Email verification status |
| RecoveryCodes | nvarchar(max) | Nullable | JSON recovery codes |
| LastSecurityAudit | datetime2 | Nullable | Last security audit date |
| SecurityQuestions | nvarchar(max) | Nullable | JSON security questions |
| MustChangePassword | bit | Required, Default: 0 | Force password change flag |
| MaxConcurrentSessions | int | Nullable | Session limit |
| AllowedIPs | nvarchar(max) | Nullable | IP whitelist |
| RequirePasswordChange | bit | Required, Default: 0 | Password change required |
| PasswordExpiresAt | datetime2 | Nullable | Password expiration date |
| PasswordHistory | nvarchar(max) | Nullable | JSON password history |
| MaxPasswordHistory | int | Nullable | Password history limit |
| LastLoginAt | datetime2 | Nullable | Last login timestamp |
| IsDeleted | bit | Required, Default: 0 | Soft delete flag |
| DeletedAt | datetime2 | Nullable | Deletion timestamp |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on Username
- UNIQUE INDEX on Email
- INDEX on Status
- INDEX on IsActive
- INDEX on CreatedAt
- INDEX on LastLoginAt
- INDEX on (Status, IsActive)
- INDEX on (IsActive, CreatedAt)

**Relationships:**
- User → Certificate (One-to-Many as CreatedBy/UpdatedBy)
- User → CertificateProductId (One-to-Many as CreatedBy/UpdatedBy)
- User → CertificateDocument (One-to-Many as UploadedBy)
- User → ApiKey (One-to-Many as CreatedBy)
- User → Role (Many-to-Many via UserRole)
- User → Company (Many-to-Many via CompanyUser)
- User → Partner (Many-to-Many via UserPartner)

---

### Roles
**Purpose:** Role definitions for RBAC

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(50) | Unique, Required | Role name: Admin, Analyst, External, Internal, Public |
| Description | nvarchar(max) | Nullable | Role description |
| IsSystem | bit | Required, Default: 0 | System role flag |
| IsActive | bit | Required, Default: 1 | Active flag |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- UNIQUE INDEX on Name

**Relationships:**
- Role → User (Many-to-Many via UserRole)
- Role → Permission (Many-to-Many via RolePermission)

---

### Permissions
**Purpose:** Granular permission definitions

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(255) | Required | Permission name |
| Codename | nvarchar(100) | Unique, Required | Permission code |
| Category | nvarchar(max) | Nullable | Permission category |
| Description | nvarchar(max) | Nullable | Permission description |
| IsActive | bit | Required, Default: 1 | Active flag |
| CreatedAt | datetime2 | Required | Creation timestamp |

**Indexes:**
- UNIQUE INDEX on Codename

**Relationships:**
- Permission → Role (Many-to-Many via RolePermission)

---

## Tags & Attributes

### Tags
**Purpose:** Internal and external tags for certificates and products

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(50) | Unique, Required | Tag name |
| TagType | nvarchar(max) | Nullable | Type: internal, external |
| Description | nvarchar(max) | Nullable | Tag description |
| IsActive | bit | Required, Default: 1 | Active flag |

**Indexes:**
- UNIQUE INDEX on Name

**Relationships:**
- Tag → Certificate (Many-to-Many via CertificateTag)
- Tag → Product (Many-to-Many via ProductTag)

---

## API & Integration

### ApiKeys
**Purpose:** API key management with rate limiting and security

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Name | nvarchar(100) | Required | API key name |
| Key | nvarchar(500) | Required | API key value |
| KeyHash | nvarchar(450) | Unique | Hashed API key |
| Description | nvarchar(max) | Nullable | Key description |
| IsActive | bit | Required, Default: 1 | Active flag |
| RateLimitPerMinute | int | Required, Default: 60 | Rate limit per minute |
| RateLimitPerHour | int | Required, Default: 1000 | Rate limit per hour |
| RateLimitPerDay | int | Required, Default: 10000 | Rate limit per day |
| AllowedIPs | nvarchar(max) | Nullable | IP whitelist |
| AllowedOrigins | nvarchar(max) | Nullable | CORS origins |
| Scopes | nvarchar(max) | Nullable | API scopes |
| MaxConcurrentRequests | int | Required, Default: 10 | Concurrent request limit |
| RequireTLS | bit | Required, Default: 1 | Require HTTPS |
| SecurityViolationCount | int | Required, Default: 0 | Violation counter |
| IsTemporarilyBlocked | bit | Required, Default: 0 | Temporary block flag |
| BlockedUntil | datetime2 | Nullable | Block expiration |
| LastSecurityIncident | datetime2 | Nullable | Last security incident |
| UserAgent | nvarchar(max) | Nullable | User agent string |
| RateLimit | int | Required, Default: 100 | Legacy rate limit |
| SecurityViolations | int | Nullable | Security violation count |
| LastViolationAt | datetime2 | Nullable | Last violation timestamp |
| LastViolationType | nvarchar(max) | Nullable | Last violation type |
| BlockReason | nvarchar(max) | Nullable | Block reason |
| RequestCount | int | Required, Default: 0 | Total request count |
| LastRotatedAt | datetime2 | Nullable | Last key rotation |
| Environment | nvarchar(max) | Nullable | Environment: Production/Staging/Development |
| UsageCostPerRequest | decimal(18,6) | Nullable | Cost per API request |
| CreatedBy | int | FK | Foreign key to Users |
| CreatedAt | datetime2 | Required | Creation timestamp |
| LastUsedAt | datetime2 | Nullable | Last usage timestamp |
| ExpiresAt | datetime2 | Nullable | Expiration timestamp |

**Indexes:**
- UNIQUE INDEX on KeyHash
- INDEX on (KeyHash, IsActive, ExpiresAt)
- INDEX on (CreatedBy, IsActive)

**Relationships:**
- ApiKey → User (Many-to-One as CreatedBy)

---

### PartnerApiKeys
**Purpose:** Partner-specific API keys for external integration

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| PartnerId | int | FK, Required | Foreign key to Partners |
| ApiKey | nvarchar(512) | Required | API key value |
| Description | nvarchar(max) | Nullable | Key description |
| IsActive | bit | Required, Default: 1 | Active flag |
| RateLimitPerHour | int | Nullable | Rate limit per hour |
| AllowedEndpoints | nvarchar(max) | Nullable | JSON allowed endpoints |
| Permissions | nvarchar(max) | Nullable | JSON permissions |
| Notes | nvarchar(max) | Nullable | Admin notes |
| RequestCount | int | Required, Default: 0 | Total request count |
| CreatedBy | int | Nullable | Created by user ID |
| CreatedAt | datetime2 | Required | Creation timestamp |
| LastUsedAt | datetime2 | Nullable | Last usage timestamp |
| ExpiresAt | datetime2 | Nullable | Expiration timestamp |
| RevokedAt | datetime2 | Nullable | Revocation timestamp |
| RevokedBy | int | Nullable | Revoked by user ID |

**Relationships:**
- PartnerApiKey → Partner (Many-to-One)

---

### AmazonApiSettings
**Purpose:** Amazon API integration configuration

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| IsEnabled | bit | Required, Default: 0 | Integration enabled flag |
| SyncIntervalHours | int | Required, Default: 24 | Sync frequency in hours |
| AccessKeyId | nvarchar(500) | Nullable | Amazon access key ID |
| SecretAccessKey | nvarchar(500) | Nullable | Amazon secret access key |
| RefreshToken | nvarchar(1000) | Nullable | Amazon refresh token |
| Region | nvarchar(10) | Nullable | AWS region |
| MarketplaceId | nvarchar(50) | Nullable | Amazon marketplace ID |
| SyncProducts | bit | Required, Default: 0 | Sync products flag |
| SyncCertificates | bit | Required, Default: 0 | Sync certificates flag |
| SyncCompliance | bit | Required, Default: 0 | Sync compliance flag |
| WebhookUrl | nvarchar(max) | Nullable | Webhook URL |
| LastSync | datetime2 | Nullable | Last sync timestamp |
| LastSyncStatus | nvarchar(max) | Nullable | Status: idle/running/success/error |
| RecordsSynced | int | Nullable | Records synced in last run |
| LastError | nvarchar(max) | Nullable | Last error message |
| UpdatedBy | int | FK | Foreign key to Users |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Relationships:**
- AmazonApiSettings → User (Many-to-One as UpdatedBy)
- AmazonApiSettings → AmazonSyncHistory (One-to-Many)

---

### AmazonSyncHistory
**Purpose:** Amazon sync execution logs

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| Timestamp | datetime2 | Required | Sync execution time |
| Type | nvarchar(max) | Nullable | Type: Auto/Manual |
| RecordsProcessed | int | Required, Default: 0 | Records processed |
| RecordsSucceeded | int | Required, Default: 0 | Successful records |
| RecordsFailed | int | Required, Default: 0 | Failed records |
| Status | nvarchar(max) | Nullable | Status: success/error/partial |
| DurationSeconds | int | Required, Default: 0 | Execution duration |
| Message | nvarchar(max) | Nullable | Status message |
| ErrorDetails | nvarchar(max) | Nullable | Error details |
| TriggeredBy | nvarchar(max) | Nullable | User who triggered sync |

**Relationships:**
- AmazonSyncHistory → AmazonApiSettings (Many-to-One implied)

---

## Settings

### GoogleAnalyticsSettings
**Purpose:** Google Analytics integration configuration

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Primary key |
| TrackingId | nvarchar(20) | Required | GA tracking ID (e.g., UA-XXXXXXXX-X) |
| IsEnabled | bit | Required, Default: 0 | Tracking enabled flag |
| CustomScript | nvarchar(max) | Nullable | Custom tracking script |
| AnonymizeIp | bit | Required, Default: 1 | IP anonymization flag |
| EnhancedLinkAttribution | bit | Required, Default: 0 | Enhanced link attribution |
| DebugMode | bit | Required, Default: 0 | Debug mode flag |
| UpdatedBy | int | FK | Foreign key to Users |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Relationships:**
- GoogleAnalyticsSettings → User (Many-to-One as UpdatedBy)

---

## ETL & Data Processing

### ETLConfiguration
**Purpose:** ETL job configuration and scheduling

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Primary key (GUID) |
| Name | nvarchar(max) | Nullable | Job name |
| JobType | nvarchar(max) | Nullable | Type: Full/Incremental/RealTime/OnDemand/Scheduled |
| IsIncremental | bit | Required | Incremental flag |
| LastRunTime | datetime2 | Nullable | Last execution time |
| DataSources | nvarchar(max) | Nullable | JSON data sources |
| Transformations | nvarchar(max) | Nullable | JSON transformations |
| Destinations | nvarchar(max) | Nullable | JSON destinations |
| Settings | nvarchar(max) | Nullable | JSON settings |
| IsActive | bit | Required | Active flag |
| CronSchedule | nvarchar(max) | Nullable | Cron schedule expression |
| CreatedAt | datetime2 | Required | Creation timestamp |
| UpdatedAt | datetime2 | Required | Last update timestamp |

**Indexes:**
- INDEX on Name

**Relationships:**
- ETLConfiguration → ETLLogEntry (One-to-Many)
- ETLConfiguration → ETLAlert (One-to-Many)
- ETLConfiguration → ETLDataQualityIssue (One-to-Many)

---

### ETLLogEntry
**Purpose:** ETL execution logs

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Primary key (GUID) |
| JobId | uniqueidentifier | Required | Foreign key to ETLConfiguration |
| Timestamp | datetime2 | Required | Log timestamp |
| LogLevel | nvarchar(max) | Nullable | Level: Info/Warning/Error |
| Message | nvarchar(max) | Nullable | Log message |
| Category | nvarchar(max) | Nullable | Log category |
| Properties | nvarchar(max) | Nullable | JSON properties |

**Indexes:**
- INDEX on JobId
- INDEX on Timestamp

**Relationships:**
- ETLLogEntry → ETLConfiguration (Many-to-One via JobId)

---

### ETLAlert
**Purpose:** ETL alerts and notifications

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Primary key (GUID) |
| AlertType | nvarchar(max) | Nullable | Alert type |
| Message | nvarchar(max) | Nullable | Alert message |
| Severity | nvarchar(max) | Nullable | Severity: Info/Warning/Error/Critical |
| CreatedAt | datetime2 | Required | Alert creation time |
| IsResolved | bit | Required | Resolution status |
| ResolvedAt | datetime2 | Nullable | Resolution timestamp |
| Context | nvarchar(max) | Nullable | JSON context |

**Indexes:**
- INDEX on CreatedAt
- INDEX on IsResolved

---

### ETLDataQualityIssue
**Purpose:** Data quality issue tracking

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Primary key (GUID) |
| IssueType | nvarchar(max) | Nullable | Issue type |
| Description | nvarchar(max) | Nullable | Issue description |
| FieldName | nvarchar(max) | Nullable | Affected field |
| RecordId | nvarchar(max) | Nullable | Affected record ID |
| ExpectedValue | nvarchar(max) | Nullable | JSON expected value |
| ActualValue | nvarchar(max) | Nullable | JSON actual value |
| Severity | nvarchar(max) | Nullable | Issue severity |
| DetectedAt | datetime2 | Required | Detection timestamp |

**Indexes:**
- INDEX on DetectedAt
- INDEX on IssueType

---

### CertificateAggregateData
**Purpose:** Pre-aggregated certificate metrics for dashboard performance

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Period | nvarchar(450) | PK (Composite) | Period type: Daily/Weekly/Monthly |
| PeriodDate | datetime2 | PK (Composite) | Period date |
| TotalCertificates | int | Required | Total certificate count |
| ActiveCertificates | int | Required | Active certificate count |
| ExpiredCertificates | int | Required | Expired certificate count |
| ExpiringThisMonth | int | Required | Expiring this month count |
| CertificatesByType | nvarchar(max) | Nullable | JSON breakdown by type |
| CertificatesByCountry | nvarchar(max) | Nullable | JSON breakdown by country |
| CertificatesByCompany | nvarchar(max) | Nullable | JSON breakdown by company |
| AverageValidityPeriod | decimal(18,2) | Required | Average validity in days |
| CreatedAt | datetime2 | Required | Creation timestamp |

**Indexes:**
- PRIMARY KEY on (Period, PeriodDate)
- INDEX on PeriodDate

---

## Junction Tables

### CertificateTag
**Purpose:** Many-to-many relationship between Certificates and Tags

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| CertificateId | int | PK (Composite), FK | Foreign key to Certificates |
| TagId | int | PK (Composite), FK | Foreign key to Tags |

**Relationships:**
- CertificateTag → Certificate (Many-to-One)
- CertificateTag → Tag (Many-to-One)

---

### ProductTag
**Purpose:** Many-to-many relationship between Products and Tags

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ProductId | int | PK (Composite), FK | Foreign key to Products |
| TagId | int | PK (Composite), FK | Foreign key to Tags |

**Relationships:**
- ProductTag → Product (Many-to-One)
- ProductTag → Tag (Many-to-One)

---

### UserRole
**Purpose:** Many-to-many relationship between Users and Roles

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| UserId | int | PK (Composite), FK | Foreign key to Users |
| RoleId | int | PK (Composite), FK | Foreign key to Roles |

**Relationships:**
- UserRole → User (Many-to-One)
- UserRole → Role (Many-to-One)

---

### RolePermission
**Purpose:** Many-to-many relationship between Roles and Permissions

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| RoleId | int | PK (Composite), FK | Foreign key to Roles |
| PermissionId | int | PK (Composite), FK | Foreign key to Permissions |

**Relationships:**
- RolePermission → Role (Many-to-One)
- RolePermission → Permission (Many-to-One)

---

### CompanyUser
**Purpose:** Many-to-many relationship between Companies and Users (for External user filtering)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| CompanyId | int | PK (Composite), FK | Foreign key to Companies |
| UserId | int | PK (Composite), FK | Foreign key to Users |

**Relationships:**
- CompanyUser → Company (Many-to-One)
- CompanyUser → User (Many-to-One)

---

### UserPartner
**Purpose:** Many-to-many relationship between Users and Partners (for External user filtering)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| UserId | int | PK (Composite), FK | Foreign key to Users |
| PartnerId | int | PK (Composite), FK | Foreign key to Partners |

**Relationships:**
- UserPartner → User (Many-to-One)
- UserPartner → Partner (Many-to-One)

---

## Indexes & Performance

### Critical Composite Indexes (20+)

**Certificate Performance Indexes:**
```sql
CREATE INDEX IX_Certificates_Status_ExpirationDate ON Certificates(Status, ExpirationDate);
CREATE INDEX IX_Certificates_CompanyId_Status ON Certificates(CompanyId, Status);
CREATE INDEX IX_Certificates_ProductId_Status ON Certificates(ProductId, Status);
CREATE INDEX IX_Certificates_Country_Status ON Certificates(Country, Status);
CREATE INDEX IX_Certificates_CertificationTypeId_Status ON Certificates(CertificationTypeId, Status);
CREATE INDEX IX_Certificates_ExpirationDate_Status_CompanyId ON Certificates(ExpirationDate, Status, CompanyId);
```

**CertificateProductId Indexes:**
```sql
CREATE UNIQUE INDEX IX_CertificateProductIds_ProductIdType_ProductIdValue
    ON CertificateProductIds(ProductIdType, ProductIdValue);
CREATE INDEX IX_CertificateProductIds_CertificateId_ProductIdType
    ON CertificateProductIds(CertificateId, ProductIdType);
CREATE INDEX IX_CertificateProductIds_Partner_Status_IsActive
    ON CertificateProductIds(Partner, Status, IsActive);
CREATE INDEX IX_CertificateProductIds_Status_IsActive_CreatedAt
    ON CertificateProductIds(Status, IsActive, CreatedAt);
```

**Product Indexes:**
```sql
CREATE INDEX IX_Products_CategoryId_CompanyId ON Products(CategoryId, CompanyId);
CREATE INDEX IX_Products_ProductTypeId_IsActive ON Products(ProductTypeId, IsActive);
CREATE INDEX IX_Products_CompanyId_IsActive ON Products(CompanyId, IsActive);
```

**User & Security Indexes:**
```sql
CREATE INDEX IX_Users_Status_IsActive ON Users(Status, IsActive);
CREATE INDEX IX_Users_IsActive_CreatedAt ON Users(IsActive, CreatedAt);
CREATE INDEX IX_ApiKeys_KeyHash_IsActive_ExpiresAt ON ApiKeys(KeyHash, IsActive, ExpiresAt);
CREATE INDEX IX_ApiKeys_CreatedBy_IsActive ON ApiKeys(CreatedBy, IsActive);
```

**Company Indexes:**
```sql
CREATE INDEX IX_Companies_Country_IsActive ON Companies(Country, IsActive);
CREATE INDEX IX_Companies_Name_Country ON Companies(Name, Country);
```

**Partner Indexes:**
```sql
CREATE INDEX IX_Partners_IsActive_SortOrder ON Partners(IsActive, SortOrder);
```

---

## Schema Ownership Requirement

**CRITICAL:** All tables MUST be in the `dbo` schema (not user-owned schemas).

**Verification:**
```sql
SELECT SCHEMA_NAME(schema_id) AS SchemaName, name AS TableName
FROM sys.tables
WHERE type = 'U'
ORDER BY SchemaName, TableName;
```

**Expected:** All tables should show `dbo` as SchemaName.

**User Configuration:**
```sql
ALTER USER GCC_adminxyz123 WITH DEFAULT_SCHEMA = dbo;
EXEC sp_addrolemember 'db_owner', 'GCC_adminxyz123';
```

See: [CRITICAL_SCHEMA_REQUIREMENT.md](CRITICAL_SCHEMA_REQUIREMENT.md)

---

## Summary

- **Total Tables:** 34+ (27 primary + 6 junction + aggregate tables)
- **Total Indexes:** 50+ (14 unique, 20+ composite, 16+ single-column)
- **Relationships:** 15 one-to-many, 6 many-to-many, 2 self-referencing
- **Key Features:** 14-state workflow, RBAC, 2FA, API rate limiting, ETL pipeline
- **Data Types:** int, uniqueidentifier, nvarchar, datetime2, bit, decimal, bigint
- **JSON Storage:** 15+ fields using JSON for complex objects
- **Audit Tracking:** CreatedAt/UpdatedAt on all entities, CreatedBy/UpdatedBy on sensitive entities
- **Soft Deletion:** IsDeleted + DeletedAt on Company, Product, Partner, User

---

**Last Updated:** 2025-11-17
**Documentation Status:** ✅ Complete (100% up-to-date)

For detailed relationship diagrams, see [ERD_DIAGRAM.md](../ERD_DIAGRAM.md)

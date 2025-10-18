# GCC Web Application - Database Schema Documentation

**Database:** gccwebap_gccdb
**Server:** 68.71.129.120,1533
**DBMS:** Microsoft SQL Server 2016+
**Generated:** October 7, 2025

---

## Table of Contents
1. [Core Business Tables](#core-business-tables)
2. [User Management](#user-management)
3. [Product Management](#product-management)
4. [API & Integration](#api--integration)
5. [ETL & Data Quality](#etl--data-quality)
6. [Metadata & Configuration](#metadata--configuration)
7. [Relationships & Foreign Keys](#relationships--foreign-keys)
8. [Indexes & Performance](#indexes--performance)

---

## Core Business Tables

### Certificates
**Primary table for certificate records** - Supports 14-state workflow

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key (auto-increment) |
| Number | NVARCHAR | Unique certificate number |
| Status | NVARCHAR | Workflow status (Active, Expired, Pending, etc.) |
| CompanyId | INT | Foreign key to Companies |
| ProductId | INT | Foreign key to Products |
| CertificationTypeId | INT | Foreign key to CertificationTypes |
| EffectiveDate | DATETIME | Start date of certification |
| ExpirationDate | DATETIME | End date of certification |
| Country | NVARCHAR | Country of certification |
| Location | NVARCHAR | Specific location/facility |
| Partner | NVARCHAR | Partner name (Amazon, Wayfair, None) |
| CertificateFile | NVARCHAR | PDF file path |
| CertificateImagePath | NVARCHAR | Optional image/thumbnail |
| CreatedAt | DATETIME | Creation timestamp |
| CreatedBy | INT | User who created (nullable FK) |
| UpdatedAt | DATETIME | Last update timestamp |
| UpdatedBy | INT | User who updated (nullable FK) |

**Indexes:**
- UNIQUE: Number
- Standard: Status, ExpirationDate, EffectiveDate, Country, CompanyId, ProductId, CertificationTypeId, CreatedAt
- Composite: (Status, ExpirationDate), (CompanyId, Status), (ProductId, Status), (Country, Status)

**Relationships:**
- ONE Company has MANY Certificates
- ONE Product has MANY Certificates
- ONE CertificationType has MANY Certificates
- ONE User (CreatedBy) has MANY Certificates
- ONE User (UpdatedBy) has MANY Certificates

---

### CertificateProductIds
**Supports 100s of ASINs per certificate** - Added in migration `20250918212035`

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| CertificateId | INT | Foreign key to Certificates |
| ProductIdType | NVARCHAR | Type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| ProductIdValue | NVARCHAR | The actual product ID |
| Status | NVARCHAR | Active, Inactive, Pending |
| Partner | NVARCHAR | Associated partner |
| ProductName | NVARCHAR | Optional product name |
| ProductSku | NVARCHAR | Optional SKU |
| Description | NVARCHAR | Optional description |
| IsActive | BIT | Active flag |
| IsPublic | BIT | Publicly visible |
| DisplayOrder | INT | Sort order (nullable) |
| CreatedBy | INT | User FK (nullable) |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedBy | INT | User FK (nullable) |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- UNIQUE: (ProductIdType, ProductIdValue)
- Standard: CertificateId, ProductIdType, ProductIdValue, Status, Partner, IsActive, CreatedAt
- Composite: (CertificateId, IsActive), (CertificateId, ProductIdType), (Partner, Status, IsActive), (Status, IsActive, CreatedAt)

**Key Features:**
- Bulk CSV import/export
- Search by ASIN/UPC across all certificates
- Partner-specific filtering

---

### CertificationTypes
**Defines types of certifications**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Type name (UNIQUE) |
| AmazonCode | NVARCHAR | Amazon-specific code |
| Description | NVARCHAR | Description |
| IsActive | BIT | Active flag |
| SortOrder | INT | Display order |

**Examples:** GreenCircle Certified Recoverable, GreenCircle Certified Renewable

---

### Companies
**Certificate holders / manufacturers**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Company name (UNIQUE) |
| Location | NVARCHAR | Address/location |
| Country | NVARCHAR | Country |
| ContactInfo | NVARCHAR | Contact details |
| Email | NVARCHAR | Email address |
| Website | NVARCHAR | Company website |
| IsActive | BIT | Active flag |
| IsDeleted | BIT | Soft delete flag |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- UNIQUE: Name
- Standard: Country, IsActive, CreatedAt, Website
- Composite: (Name, Country), (Country, IsActive)

---

### Products
**Product catalog**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Product name |
| Description | NVARCHAR | Product description |
| Model | NVARCHAR | Model number |
| Sku | NVARCHAR | SKU |
| Asin | NVARCHAR | Amazon ASIN (legacy - use CertificateProductIds) |
| Upc | NVARCHAR | UPC (legacy) |
| CategoryId | INT | FK to ProductCategories |
| ProductTypeId | INT | FK to ProductTypes (nullable) |
| ProductSubcategoryId | INT | FK to ProductSubcategories (nullable) |
| CompanyId | INT | FK to Companies (nullable) |
| IsActive | BIT | Active flag |
| IsDeleted | BIT | Soft delete flag |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- Standard: Name, CategoryId, CompanyId, ProductTypeId, ProductSubcategoryId, IsActive, CreatedAt
- Composite: (CategoryId, CompanyId), (CompanyId, IsActive), (ProductTypeId, IsActive)

---

## User Management

### Users
**Application users with 2FA support**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Username | NVARCHAR | Username (UNIQUE) |
| Email | NVARCHAR | Email (UNIQUE) |
| Name | NVARCHAR | Display name |
| FullName | NVARCHAR | Full legal name |
| PasswordHash | NVARCHAR | Hashed password |
| Status | NVARCHAR | Active, Inactive, Locked |
| IsActive | BIT | Active flag |
| IsStaff | BIT | Staff member flag |
| IsSuperuser | BIT | Superuser flag |
| IsDeleted | BIT | Soft delete flag |
| RequirePasswordReset | BIT | Force password change |
| RequirePasswordChange | BIT | Password change needed |
| MustChangePassword | BIT | Must change password |
| TwoFactorEnabled | BIT | 2FA enabled |
| TwoFactorMethod | NVARCHAR | 2FA method (SMS, Email, TOTP) |
| TwoFactorSecret | NVARCHAR | 2FA secret key (encrypted) |
| PhoneNumber | NVARCHAR | Phone for SMS 2FA |
| IsPhoneVerified | BIT | Phone verified |
| IsEmailVerified | BIT | Email verified |
| LastLogin | DATETIME | Last login timestamp |
| LastLoginAt | DATETIME | Last login timestamp (duplicate) |
| PasswordChangedAt | DATETIME | Password change date |
| PasswordExpiresAt | DATETIME | Password expiration |
| PasswordHistory | NVARCHAR | JSON of previous password hashes |
| MaxPasswordHistory | INT | Max password history count |
| FailedLoginAttempts | INT | Failed login counter |
| LockedUntil | DATETIME | Account locked until |
| MaxConcurrentSessions | INT | Max concurrent sessions allowed |
| AllowedIPs | NVARCHAR | Whitelisted IP addresses |
| RecoveryCodes | NVARCHAR | 2FA recovery codes |
| SecurityQuestions | NVARCHAR | Security Q&A (JSON) |
| LastSecurityAudit | DATETIME | Last security audit date |
| DeletedAt | DATETIME | Soft delete timestamp |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- UNIQUE: Username, Email
- Standard: Status, IsActive, LastLoginAt, CreatedAt
- Composite: (Status, IsActive), (IsActive, CreatedAt)

**Security Features:**
- Password history tracking
- Account lockout after failed attempts
- 2FA with multiple methods (TOTP, SMS, Email)
- IP whitelisting
- Session management

---

### Roles
**User roles (Admin, Analyst, External)**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Role name (UNIQUE) |
| Description | NVARCHAR | Role description |
| IsSystem | BIT | System role (cannot delete) |
| IsActive | BIT | Active flag |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Default Roles:**
- **Admin** - Full CRUD on all resources
- **Analyst** - Read/Write, no Admin pages
- **External** - Read-only public access

**Relationships:**
- MANY Roles have MANY Permissions (RolePermission junction)
- MANY Roles have MANY Users (RoleUser junction)

---

### Permissions
**Granular permission system**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Permission name |
| Codename | NVARCHAR | Unique code (UNIQUE) |
| Category | NVARCHAR | Category/group |
| Description | NVARCHAR | Description |
| IsActive | BIT | Active flag |
| CreatedAt | DATETIME | Creation timestamp |

**Indexes:**
- UNIQUE: Codename

**Examples:**
- `certificate.create`
- `certificate.update`
- `certificate.delete`
- `user.manage`
- `role.manage`

---

### Partners
**Partner organizations (Amazon, Wayfair)**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Partner name (UNIQUE) |
| Description | NVARCHAR | Description |
| Website | NVARCHAR | Partner website |
| ContactInfo | NVARCHAR | Contact details |
| Email | NVARCHAR | Partner email |
| IsActive | BIT | Active flag |
| IsDeleted | BIT | Soft delete flag |
| SortOrder | INT | Display order |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- UNIQUE: Name
- Standard: IsActive, SortOrder, CreatedAt
- Composite: (IsActive, SortOrder)

**Relationships:**
- MANY Partners have MANY Users (PartnerUser junction)

---

## Product Management

### ProductCategories
**Top-level product categories**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Category name (UNIQUE) |
| Description | NVARCHAR | Description |
| SortOrder | INT | Display order |
| IsActive | BIT | Active flag |

---

### ProductTypes
**Hierarchical product types (up to 3 levels)**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Type name |
| CategoryId | INT | FK to ProductCategories |
| ParentId | INT | FK to ProductTypes (self-referencing) |
| Level | INT | Hierarchy level (0, 1, 2) |
| SortOrder | INT | Display order |
| IsActive | BIT | Active flag |

**Indexes:**
- Standard: CategoryId, ParentId

---

### ProductSubcategories
**Product subcategories**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Subcategory name |
| ProductTypeId | INT | FK to ProductTypes |
| Description | NVARCHAR | Description |
| SortOrder | INT | Display order |
| IsActive | BIT | Active flag |

---

### ProductIdentifiers
**Legacy product identifiers table** (superseded by CertificateProductIds)

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| ProductId | INT | FK to Products |
| IdentifierType | NVARCHAR | ASIN, UPC, etc. |
| IdentifierValue | NVARCHAR | Identifier value |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- UNIQUE: (ProductId, IdentifierType, IdentifierValue)

---

## API & Integration

### ApiKeys
**API authentication tokens with rate limiting**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | API key name |
| Key | NVARCHAR | API key (plain text - should be hashed) |
| KeyHash | NVARCHAR | Hashed API key (UNIQUE) |
| Description | NVARCHAR | Description |
| Environment | NVARCHAR | Development, Staging, Production |
| IsActive | BIT | Active flag |
| CreatedAt | DATETIME | Creation timestamp |
| CreatedBy | NVARCHAR | Creator username |
| LastUsedAt | DATETIME | Last usage timestamp |
| LastRotatedAt | DATETIME | Last key rotation |
| ExpiresAt | DATETIME | Expiration date |
| RateLimit | INT | Requests per minute (deprecated) |
| RateLimitPerMinute | INT | Requests per minute |
| RateLimitPerHour | INT | Requests per hour |
| RateLimitPerDay | INT | Requests per day |
| MaxConcurrentRequests | INT | Max concurrent requests |
| RequestCount | INT | Total request count |
| AllowedIPs | NVARCHAR | Whitelisted IP addresses (JSON array) |
| AllowedOrigins | NVARCHAR | Whitelisted origins (JSON array) |
| Scopes | NVARCHAR | Permitted scopes (JSON array) |
| RequireTLS | BIT | Require HTTPS |
| IsTemporarilyBlocked | BIT | Temporarily blocked flag |
| BlockedUntil | DATETIME | Blocked until timestamp |
| BlockReason | NVARCHAR | Reason for blocking |
| SecurityViolations | INT | Violation counter |
| SecurityViolationCount | INT | Violation counter (duplicate) |
| LastViolationType | NVARCHAR | Last violation type |
| LastViolationAt | DATETIME | Last violation timestamp |
| LastSecurityIncident | NVARCHAR | Last security incident details |
| UserAgent | NVARCHAR | Expected User-Agent |
| UsageCostPerRequest | NVARCHAR | Cost tracking |

**Indexes:**
- UNIQUE: KeyHash
- Standard: IsActive, CreatedBy, CreatedAt, ExpiresAt
- Composite: (CreatedBy, IsActive), (KeyHash, IsActive, ExpiresAt)

**Security Features:**
- Rate limiting (per minute/hour/day)
- IP whitelisting
- Origin whitelisting
- Scope-based permissions
- Automatic blocking after violations
- Key rotation tracking

---

## ETL & Data Quality

### ETLConfigurations
**ETL job configurations for data import**

| Column | Type | Description |
|--------|------|-------------|
| Id | NVARCHAR (GUID) | Primary key |
| Name | NVARCHAR | Configuration name |
| JobType | INT | Job type enum |
| IsIncremental | BIT | Incremental or full load |
| LastRunTime | DATETIME | Last execution time |
| DataSources | NVARCHAR (JSON) | Data source configurations |
| Transformations | NVARCHAR (JSON) | Transformation rules |
| Destinations | NVARCHAR (JSON) | Destination configurations |
| Settings | NVARCHAR (JSON) | Additional settings |
| IsActive | BIT | Active flag |
| CronSchedule | NVARCHAR | Cron schedule expression |
| CreatedAt | DATETIME | Creation timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- Standard: Name

---

### ETLLogEntries
**ETL execution logs**

| Column | Type | Description |
|--------|------|-------------|
| Id | NVARCHAR (GUID) | Primary key |
| JobId | NVARCHAR | Job identifier |
| Timestamp | DATETIME | Log timestamp |
| LogLevel | NVARCHAR | Debug, Info, Warning, Error |
| Message | NVARCHAR | Log message |
| Category | NVARCHAR | Log category |
| Properties | NVARCHAR (JSON) | Additional properties |

**Indexes:**
- Standard: JobId, Timestamp

---

### ETLAlerts
**ETL alerts and notifications**

| Column | Type | Description |
|--------|------|-------------|
| Id | NVARCHAR (GUID) | Primary key |
| AlertType | NVARCHAR | Alert type |
| Message | NVARCHAR | Alert message |
| Severity | INT | Severity level |
| CreatedAt | DATETIME | Creation timestamp |
| IsResolved | BIT | Resolution flag |
| ResolvedAt | DATETIME | Resolution timestamp |
| Context | NVARCHAR (JSON) | Alert context |

**Indexes:**
- Standard: CreatedAt, IsResolved

---

### ETLDataQualityIssues
**Data quality issue tracking**

| Column | Type | Description |
|--------|------|-------------|
| Id | NVARCHAR (GUID) | Primary key |
| IssueType | NVARCHAR | Issue type |
| Description | NVARCHAR | Issue description |
| FieldName | NVARCHAR | Affected field |
| RecordId | NVARCHAR | Affected record ID |
| ExpectedValue | NVARCHAR | Expected value |
| ActualValue | NVARCHAR | Actual value |
| Severity | INT | Severity level |
| DetectedAt | DATETIME | Detection timestamp |

**Indexes:**
- Standard: IssueType, DetectedAt

---

### CertificateAggregates
**Pre-computed aggregates for dashboard**

| Column | Type | Description |
|--------|------|-------------|
| Period | NVARCHAR | Period type (Daily, Monthly, Yearly) |
| PeriodDate | DATETIME | Period date |
| TotalCertificates | INT | Total certificate count |
| ActiveCertificates | INT | Active count |
| ExpiredCertificates | INT | Expired count |
| ExpiringThisMonth | INT | Expiring soon count |
| CertificatesByType | NVARCHAR (JSON) | Count by type |
| CertificatesByCountry | NVARCHAR (JSON) | Count by country |
| CertificatesByCompany | NVARCHAR (JSON) | Count by company |
| AverageValidityPeriod | FLOAT | Average validity in days |
| CreatedAt | DATETIME | Creation timestamp |

**Indexes:**
- PRIMARY KEY: (Period, PeriodDate)
- Standard: PeriodDate

---

## Metadata & Configuration

### Tags
**Flexible tagging system**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Tag name (UNIQUE) |
| TagType | NVARCHAR | Tag type/category |
| Description | NVARCHAR | Description |
| IsActive | BIT | Active flag |

**Relationships:**
- MANY Tags have MANY Certificates (CertificateTag junction)
- MANY Tags have MANY Products (ProductTag junction)

---

### CustomFields
**Dynamic custom fields per certification type**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Name | NVARCHAR | Field name |
| FieldType | NVARCHAR | Text, Number, Date, Boolean, Select |
| IsRequired | BIT | Required flag |
| IsVisible | BIT | Visible flag |
| IsPublic | BIT | Publicly visible |
| SortOrder | INT | Display order |
| Options | NVARCHAR (JSON) | Options for Select type |
| DefaultValue | NVARCHAR | Default value |
| ValidationRules | NVARCHAR (JSON) | Validation rules |
| CertificateTypeId | INT | FK to CertificationTypes |

**Indexes:**
- Standard: CertificateTypeId

---

### CustomFieldValues
**Custom field values for certificates**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| CertificateId | INT | FK to Certificates |
| FieldId | INT | FK to CustomFields |
| TextValue | NVARCHAR | Text value |
| NumberValue | DECIMAL(18,4) | Numeric value |
| DateValue | DATETIME | Date value |
| BooleanValue | BIT | Boolean value |

**Indexes:**
- Standard: CertificateId, FieldId

---

### CertificateDocuments
**File attachments for certificates**

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| FileName | NVARCHAR | Stored filename |
| OriginalFileName | NVARCHAR | Original filename |
| FilePath | NVARCHAR | File storage path |
| ContentType | NVARCHAR | MIME type |
| FileSize | INT | File size in bytes |
| FileHash | NVARCHAR | SHA256 hash for deduplication |
| DocumentType | NVARCHAR | PDF, Image, Report, etc. |
| Description | NVARCHAR | Description |
| Version | INT | Version number |
| IsActive | BIT | Active flag |
| IsPublic | BIT | Publicly accessible |
| IsVirusScanned | BIT | Virus scan flag |
| PassedVirusScan | BIT | Virus scan result |
| VirusScanDate | DATETIME | Scan timestamp |
| CertificateId | INT | FK to Certificates |
| UploadedBy | INT | FK to Users (nullable) |
| UploadedAt | DATETIME | Upload timestamp |
| UpdatedAt | DATETIME | Update timestamp |

**Indexes:**
- Standard: CertificateId, FileHash, UploadedBy

---

## Relationships & Foreign Keys

### Certificate Relationships
```
Certificate (1) ────> (N) CertificateProductIds  [100s of ASINs]
Certificate (N) ────> (1) Company
Certificate (N) ────> (1) Product
Certificate (N) ────> (1) CertificationType
Certificate (1) ────> (N) CertificateDocuments
Certificate (1) ────> (N) CustomFieldValues
Certificate (N) ────> (N) Tags  [CertificateTag junction]
Certificate (N) ────> (1) User (CreatedBy)  [SET NULL on delete]
Certificate (N) ────> (1) User (UpdatedBy)  [SET NULL on delete]
```

### User Relationships
```
User (N) ────> (N) Roles  [RoleUser junction]
User (N) ────> (N) Companies  [CompanyUser junction]
User (N) ────> (N) Partners  [PartnerUser junction]
User (1) ────> (N) Certificates (as CreatedBy)
User (1) ────> (N) Certificates (as UpdatedBy)
User (1) ────> (N) CertificateProductIds (as CreatedBy)
User (1) ────> (N) CertificateProductIds (as UpdatedBy)
User (1) ────> (N) CertificateDocuments (as UploadedBy)
```

### Role & Permission Relationships
```
Role (N) ────> (N) Permissions  [PermissionRole junction]
Role (N) ────> (N) Users  [RoleUser junction]
```

### Product Relationships
```
Product (N) ────> (1) ProductCategory
Product (N) ────> (1) ProductType
Product (N) ────> (1) ProductSubcategory
Product (N) ────> (1) Company
Product (1) ────> (N) ProductIdentifiers  [Legacy]
Product (N) ────> (N) Tags  [ProductTag junction]
Product (1) ────> (N) Certificates
```

---

## Indexes & Performance

### Critical Performance Indexes

#### Certificate Search Performance
```sql
-- Single column indexes
IX_Certificates_Status
IX_Certificates_ExpirationDate
IX_Certificates_Country
IX_Certificates_CompanyId
IX_Certificates_ProductId
IX_Certificates_CertificationTypeId
IX_Certificates_CreatedAt

-- Composite indexes for complex queries
IX_Certificates_Status_ExpirationDate
IX_Certificates_CompanyId_Status
IX_Certificates_ProductId_Status
IX_Certificates_Country_Status
IX_Certificates_CertificationTypeId_Status
IX_Certificates_ExpirationDate_Status_CompanyId
```

#### ASIN/Product ID Lookup
```sql
-- Unique constraint for data integrity
IX_CertificateProductIds_ProductIdType_ProductIdValue (UNIQUE)

-- Fast lookups
IX_CertificateProductIds_ProductIdValue
IX_CertificateProductIds_CertificateId
IX_CertificateProductIds_Partner_Status_IsActive
```

#### User & Authentication
```sql
IX_Users_Username (UNIQUE)
IX_Users_Email (UNIQUE)
IX_Users_Status_IsActive
IX_Users_LastLoginAt
IX_ApiKeys_KeyHash (UNIQUE)
IX_ApiKeys_KeyHash_IsActive_ExpiresAt
```

### Index Maintenance Recommendations

**Weekly:**
```sql
UPDATE STATISTICS Certificates WITH FULLSCAN;
UPDATE STATISTICS CertificateProductIds WITH FULLSCAN;
```

**Monthly:**
```sql
ALTER INDEX ALL ON Certificates REBUILD;
ALTER INDEX ALL ON CertificateProductIds REBUILD;
```

---

## Migration History

| MigrationId | Description | Date |
|-------------|-------------|------|
| 20250808131552_InitialCreate | Initial schema | Aug 8, 2025 |
| 20250808134211_UpdatedDatabaseSchema | Schema refinements | Aug 8, 2025 |
| 20250808143700_InitialMigration | Baseline migration | Aug 8, 2025 |
| 20250808160522_AdminEnhancements | User management enhancements | Aug 8, 2025 |
| 20250810151650_UpdateETLConfiguration | ETL tables added | Aug 10, 2025 |
| 20250811142726_AddProductIdentifiers | Product identifiers table | Aug 11, 2025 |
| 20250813031456_FixModelIssues | Bug fixes and ApiKeys | Aug 13, 2025 |
| 20250827043046_AddPerformanceIndexes | Performance optimization | Aug 27, 2025 |
| 20250827182202_AddPartnerFieldToCertificate | Partner field added | Aug 27, 2025 |
| 20250909010923_AddPartnerEntity | Partner entity and relationships | Sep 9, 2025 |
| 20250918212035_AddCertificateProductIds | **Multiple ASINs support** | Sep 18, 2025 |
| 20251006162626_AddCertificateImagePath | Certificate image path | Oct 6, 2025 |

---

## Data Volume Estimates

**Per Record Storage:**
- Certificate: ~1 KB
- CertificateProductId: ~500 bytes
- CertificateDocument: ~5 KB (metadata only, files stored separately)
- User: ~2 KB
- Company: ~1 KB
- Product: ~1 KB

**Example Scenarios:**

| Scenario | Certificate Count | Product IDs | Documents | Est. DB Size |
|----------|-------------------|-------------|-----------|--------------|
| Small | 1,000 | 5,000 | 2,000 | 50 MB |
| Medium | 10,000 | 100,000 | 20,000 | 500 MB |
| Large | 50,000 | 500,000 | 100,000 | 2 GB |
| Enterprise | 100,000 | 1,000,000 | 200,000 | 5 GB |

*Indexes add approximately 20-30% overhead*

---

## Security Considerations

1. **Sensitive Data:**
   - `Users.PasswordHash` - Bcrypt hashed
   - `Users.TwoFactorSecret` - Should be encrypted at rest
   - `ApiKeys.Key` - Should be hashed, not plain text
   - `Users.RecoveryCodes` - Should be encrypted

2. **Audit Fields:**
   - Most tables have CreatedAt/UpdatedAt for audit trail
   - CreatedBy/UpdatedBy link to Users table
   - Soft delete flags (IsDeleted) for data retention

3. **Row-Level Security:**
   - External users: Filter by Partner field
   - Analyst users: Full read/write except Admin pages
   - Admin users: Full access

4. **Data Retention:**
   - Soft deletes preserve history
   - Consider implementing purge jobs for old data
   - GDPR compliance: User data deletion procedures needed

---

## Query Optimization Tips

### Fast Certificate Lookups
```sql
-- By Number (uses unique index)
SELECT * FROM Certificates WHERE Number = '24-0366';

-- Active certificates expiring soon (uses composite index)
SELECT * FROM Certificates
WHERE Status = 'Active' AND ExpirationDate BETWEEN GETDATE() AND DATEADD(MONTH, 1, GETDATE());

-- By Company and Status (uses composite index)
SELECT * FROM Certificates
WHERE CompanyId = 123 AND Status = 'Active';
```

### ASIN Lookups (Across All Certificates)
```sql
-- Find certificate by ASIN (uses unique index)
SELECT c.*
FROM Certificates c
INNER JOIN CertificateProductIds cpi ON c.Id = cpi.CertificateId
WHERE cpi.ProductIdType = 'ASIN' AND cpi.ProductIdValue = 'B08N5WRWNW'
AND cpi.IsActive = 1;
```

### Dashboard Aggregates
```sql
-- Use pre-computed aggregates when possible
SELECT * FROM CertificateAggregates
WHERE Period = 'Monthly' AND PeriodDate >= DATEADD(MONTH, -6, GETDATE());

-- Or compute on-the-fly with indexes
SELECT
    Status,
    COUNT(*) AS Count
FROM Certificates
WHERE CreatedAt >= DATEADD(MONTH, -1, GETDATE())
GROUP BY Status;
```

---

**Document Version:** 1.0
**Last Updated:** October 7, 2025
**Database Version:** EF Core 8.0.11

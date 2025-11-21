# GCC Database Solution - Field Mapping Reference Guide

**Version:** 1.0
**Date:** November 21, 2025
**Purpose:** Client reference for mapping existing data fields to GCC database schema

## Table of Contents

1. [How to Use This Guide](#how-to-use-this-guide)
2. [Certificate Fields](#certificate-fields)
3. [Company Fields](#company-fields)
4. [Product Fields](#product-fields)
5. [User Fields](#user-fields)
6. [Product ID Fields](#product-id-fields)
7. [Reference Data Fields](#reference-data-fields)
8. [Custom Fields](#custom-fields)
9. [Common Field Transformations](#common-field-transformations)
10. [Appendix: Data Type Reference](#appendix-data-type-reference)

---

## How to Use This Guide

### Purpose

This guide helps you map fields from your existing data source (database, spreadsheet, or legacy system) to the GCC database schema. For each entity (Certificate, Company, Product, etc.), you'll find:

1. **GCC Field Name** - The field name in the new GCC database
2. **Data Type** - The expected data type and constraints
3. **Required?** - Whether the field must have a value
4. **Description** - What the field represents
5. **Common Source Names** - Typical field names in legacy systems
6. **Example Values** - Sample data to illustrate the format
7. **Validation Rules** - Data quality requirements
8. **Transformation Notes** - How to convert from common legacy formats

### Mapping Workflow

1. **Identify Your Source Fields**
   - List all fields in your current data source
   - Document the current field names and sample data

2. **Map to GCC Schema**
   - Find corresponding GCC field using "Common Source Names" column
   - Note any fields that don't have a direct match (these may need custom fields)

3. **Plan Transformations**
   - Document any data format changes needed (dates, status codes, etc.)
   - Identify data cleansing requirements (duplicates, missing values, etc.)

4. **Create Mapping Document**
   - Use provided Excel template: `docs/database/Field_Mapping_Worksheet.xlsx`
   - Fill in: Source Field → GCC Field → Transformation Required

5. **Validate Sample Data**
   - Test transformation logic on 10-20 sample records
   - Verify results match GCC validation rules

---

## Certificate Fields

### Core Certificate Information

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **Id** | Integer | Auto | Primary key (auto-generated) | ID, CertificateID, PK | 1, 2, 3 | System-generated, do not import |
| **Number** | String(7) | Yes | Unique certificate number | CertificateNumber, CertNumber, Cert#, Number | 24-0001, 24-0366, 25-0100 | Format: YY-NNNN (e.g., 24-0001), must be unique |
| **Status** | String(50) | Yes | Certificate lifecycle status | Status, State, CertStatus, LifecycleStatus | active, draft, expired, issued | Must be one of 14 valid values (see Status Values table) |
| **EffectiveDate** | Date | Yes | Certificate start date | EffectiveDate, StartDate, IssueDate, ValidFrom, DateIssued | 2024-01-15, 2025-03-01 | Format: YYYY-MM-DD, must be before ExpirationDate |
| **ExpirationDate** | Date | Yes | Certificate end date | ExpirationDate, EndDate, ValidUntil, ExpDate, Expiry | 2025-01-14, 2026-02-28 | Format: YYYY-MM-DD, must be after EffectiveDate |
| **Location** | String(175) | No | Physical location | Location, Address, Site, Facility, Plant | "Seattle, WA", "123 Main St, Portland, OR" | Max 175 characters |
| **Country** | String(100) | No | Country code or name | Country, CountryCode, Nation | US, CA, GB, United States | Prefer ISO 3166-1 alpha-2 (US, CA, GB) |
| **Partner** | String(100) | No | Partner program | Partner, Program, Marketplace, Channel | Amazon, Wayfair, None, Other | Must be: Amazon, Wayfair, None, or Other |

### Legacy Certificate IDs (Deprecated - Now Use CertificateProductId Table)

These fields are deprecated in favor of the new `CertificateProductId` table which supports multiple product IDs per certificate.

| GCC Field | Data Type | Required | Description | Common Source Names | Migration Notes |
|-----------|-----------|----------|-------------|---------------------|-----------------|
| **Asin** | String | No | DEPRECATED - Use CertificateProductId | ASIN, AmazonASIN, ProductASIN | Migrate to CertificateProductId table with ProductIdType='ASIN' |
| **Upc** | String | No | DEPRECATED - Use CertificateProductId | UPC, UniversalProductCode | Migrate to CertificateProductId table with ProductIdType='UPC' |
| **Gtin** | String | No | DEPRECATED - Use CertificateProductId | GTIN, GlobalTradeItemNumber | Migrate to CertificateProductId table with ProductIdType='GTIN' |
| **Ean** | String | No | DEPRECATED - Use CertificateProductId | EAN, EuropeanArticleNumber | Migrate to CertificateProductId table with ProductIdType='EAN' |

**Migration Path:**
```csv
# Old: Single ASIN in Certificate table
Certificate.Asin = "B08N5WRWNW"

# New: Multiple product IDs in CertificateProductId table
CertificateProductId:
  CertificateId=1, ProductIdType=ASIN, ProductIdValue=B08N5WRWNW
  CertificateId=1, ProductIdType=ASIN, ProductIdValue=B08N5WRWNY
  CertificateId=1, ProductIdType=UPC, ProductIdValue=012345678905
```

### Foreign Keys (Relationships)

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **CompanyId** | Integer | Yes | Reference to Company | CompanyID, ManufacturerID, ClientID, HolderID | 1, 5, 12 | Must reference existing Company.Id |
| **ProductId** | Integer | Yes | Reference to Product | ProductID, ItemID, ProductRef | 1, 10, 25 | Must reference existing Product.Id |
| **CertificationTypeId** | Integer | Yes | Reference to CertificationType | CertTypeID, TypeID, CertificationID | 1, 2, 3 | Must reference existing CertificationType.Id |
| **CreatedBy** | Integer | No | User who created certificate | CreatedBy, CreatedByUserID, AuthorID | 1, 5 | Must reference existing User.Id (optional) |
| **UpdatedBy** | Integer | No | User who last updated | UpdatedBy, ModifiedBy, LastEditedBy | 1, 5 | Must reference existing User.Id (optional) |

### File References

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **CertificateFile** | String(500) | No | Path to PDF certificate | CertificateFile, PDFPath, FilePath, DocumentPath | certificates/24-0001.pdf | Relative path from wwwroot/uploads/ |
| **CertificateImagePath** | String(500) | No | Path to preview image | ImagePath, ThumbnailPath, PreviewImage | certificates/24-0001.png | Relative path from wwwroot/uploads/ |

### Audit Fields (System-Managed)

| GCC Field | Data Type | Required | Description | Common Source Names | Migration Notes |
|-----------|-----------|----------|-------------|---------------------|-----------------|
| **CreatedAt** | DateTime | Auto | Creation timestamp | CreatedDate, DateCreated, Created | System sets on insert, can override during migration |
| **UpdatedAt** | DateTime | Auto | Last update timestamp | ModifiedDate, LastModified, Updated | System sets on update, can override during migration |

### Certificate Status Values (14-State Workflow)

Your legacy status values must be mapped to one of these 14 GCC status values:

| GCC Status | Description | Typical Legacy Mappings |
|------------|-------------|------------------------|
| **draft** | Certificate being created | Draft, New, Created, InProgress |
| **pending_review** | Awaiting review | PendingReview, Pending, AwaitingReview, Submitted |
| **under_review** | Currently being reviewed | UnderReview, InReview, Reviewing, BeingReviewed |
| **pending_approval** | Awaiting approval | PendingApproval, AwaitingApproval, ReadyForApproval |
| **approved** | Approved but not issued | Approved, Accepted, Authorized |
| **issued** | Certificate issued to client | Issued, Published, Delivered, Sent |
| **active** | Currently valid certificate | Active, Valid, Current, Live, InEffect |
| **expiring_soon** | Expires within 90 days | ExpiringSoon, NearExpiration, AboutToExpire |
| **renewal_requested** | Renewal in progress | RenewalRequested, Renewing, RenewalInProgress |
| **suspended** | Temporarily suspended | Suspended, OnHold, Paused, Halted |
| **revoked** | Permanently revoked | Revoked, Withdrawn, Cancelled, Terminated |
| **expired** | Past expiration date | Expired, PastDue, NoLongerValid, Lapsed |
| **archived** | Historical record | Archived, Historical, OldRecord, Closed |
| **cancelled** | Cancelled before issue | Cancelled, Rejected, Denied, Aborted |

**Mapping Examples:**
```
Legacy: "Valid" → GCC: "active"
Legacy: "Pending" → GCC: "pending_review"
Legacy: "Cancelled" → GCC: "cancelled"
Legacy: "Expired" → GCC: "expired"
Legacy: "In Review" → GCC: "under_review"
```

---

## Company Fields

### Core Company Information

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **Id** | Integer | Auto | Primary key (auto-generated) | ID, CompanyID, ClientID, PK | 1, 2, 3 | System-generated, do not import |
| **Name** | String(255) | Yes | Company legal name | Name, CompanyName, BusinessName, LegalName, Organization | "Acme Corporation", "Global Flooring Solutions Ltd" | Max 255 chars, must be unique (case-insensitive) |
| **ContactInfo** | String(255) | No | Contact person or info | Contact, ContactPerson, ContactName, PrimaryContact | "John Smith", "Jane Doe, VP Operations" | Max 255 chars |
| **Email** | String(255) | No | Company email | Email, ContactEmail, CompanyEmail, BusinessEmail | contact@acmecorp.com, info@company.com | Must be valid email format if provided |
| **Website** | String(500) | No | Company website URL | Website, URL, CompanyURL, WebAddress | https://www.acmecorp.com, http://example.com | Must start with http:// or https:// |
| **Location** | String(175) | No | Physical location | Location, Address, City, Site, Headquarters | "Seattle, WA", "123 Main St, Portland, OR" | Max 175 chars, free-text |
| **Country** | String(100) | No | Country code or name | Country, CountryCode, Nation, Region | US, CA, GB, United States, Canada | Prefer ISO 3166-1 alpha-2 (US, CA, GB) |
| **IsActive** | Boolean | No | Active status flag | IsActive, Active, Status, Enabled | true, false, 1, 0 | Default: true |
| **IsDeleted** | Boolean | No | Soft delete flag | IsDeleted, Deleted, Removed | false, 0 | Default: false (don't import deleted records) |

### Audit Fields

| GCC Field | Data Type | Required | Description | Migration Notes |
|-----------|-----------|----------|-------------|-----------------|
| **CreatedAt** | DateTime | Auto | Creation timestamp | System sets on insert, can override |
| **UpdatedAt** | DateTime | Auto | Last update timestamp | System sets on update, can override |

### Common Company Data Issues

| Issue | Example | Fix |
|-------|---------|-----|
| **Duplicate names** | "Acme Corp", "ACME Corporation", "acme corp." | Standardize to one canonical name: "Acme Corporation" |
| **Missing country** | Location: "Seattle, WA" but Country: NULL | Extract country from location or set default |
| **Invalid email** | "john@company" (missing TLD) | Add .com or correct domain |
| **Invalid URL** | "www.company.com" (missing protocol) | Add https:// prefix |
| **Inactive companies** | Companies no longer in business | Set IsActive = false instead of deleting |

---

## Product Fields

### Core Product Information

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **Id** | Integer | Auto | Primary key (auto-generated) | ID, ProductID, ItemID, PK | 1, 2, 3 | System-generated, do not import |
| **Name** | String(255) | Yes | Product name | Name, ProductName, ItemName, Title, Description | "Oak Hardwood Flooring", "Bamboo Engineered Flooring" | Max 255 chars, required |
| **Description** | Text | No | Product description | Description, Details, FullDescription, LongDescription | "Premium solid oak hardwood flooring 3/4 inch thick" | Unlimited length |
| **Model** | String(100) | No | Product model number | Model, ModelNumber, ModelCode, PartNumber | "OAK-HW-750", "BAMB-ENG-500" | Max 100 chars |
| **Sku** | String(50) | No | Stock keeping unit | SKU, StockCode, ItemCode, ProductCode | "OAK-HW-750-NAT", "BAMB-500" | Max 50 chars |
| **Upc** | String(50) | No | Universal product code | UPC, UniversalProductCode, Barcode | "012345678905", "639382000393" | 12 digits if provided |
| **Asin** | String(50) | No | Amazon standard ID | ASIN, AmazonASIN, AmazonID | "B08N5WRWNW", "B07XYZ1234" | 10 alphanumeric chars if provided |

### Foreign Keys (Relationships)

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **CategoryId** | Integer | Yes | Reference to ProductCategory | CategoryID, Category, ProductCategoryID | 1, 2, 3 | Must reference existing ProductCategory.Id |
| **ProductTypeId** | Integer | No | Reference to ProductType | TypeID, ProductTypeID, Type | 1, 2, 3 | Must reference existing ProductType.Id if provided |
| **ProductSubcategoryId** | Integer | No | Reference to ProductSubcategory | SubcategoryID, Subcategory | 1, 2, 3 | Must reference existing ProductSubcategory.Id if provided |
| **CompanyId** | Integer | No | Reference to Company (manufacturer) | CompanyID, ManufacturerID, MakerID | 5, 12 | Must reference existing Company.Id if provided |

### Status Fields

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **IsActive** | Boolean | No | Active status flag | IsActive, Active, Status, Enabled | true, false, 1, 0 | Default: true |
| **IsDeleted** | Boolean | No | Soft delete flag | IsDeleted, Deleted, Removed | false, 0 | Default: false |

### Audit Fields

| GCC Field | Data Type | Required | Description | Migration Notes |
|-----------|-----------|----------|-------------|-----------------|
| **CreatedAt** | DateTime | Auto | Creation timestamp | System sets on insert |
| **UpdatedAt** | DateTime | Auto | Last update timestamp | System sets on update |

### Product Category Mapping

Map your existing product categories to GCC ProductCategory.Id values:

| Your Category | GCC CategoryId | GCC Category Name |
|---------------|----------------|-------------------|
| Flooring / Wood Flooring / Hardwood | 1 | Flooring |
| Furniture / Tables / Chairs | 2 | Furniture |
| Building / Construction / Materials | 3 | Building Materials |
| Packaging / Boxes / Containers | 4 | Packaging |
| Textiles / Fabrics / Cloth | 5 | Textiles |

*Note: Actual category IDs will depend on your reference data import in Phase 1.*

---

## User Fields

### Core User Information

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **Id** | Integer | Auto | Primary key (auto-generated) | ID, UserID, PK | 1, 2, 3 | System-generated |
| **Username** | String(255) | Yes | Unique username | Username, Login, UserName, LoginName | admin, analyst, jsmith | Max 255 chars, must be unique (case-insensitive) |
| **Email** | String(255) | Yes | Email address | Email, EmailAddress, UserEmail | admin@example.com, john.smith@company.com | Must be valid email format, must be unique |
| **Name** | String(100) | No | Short name or first name | Name, FirstName, GivenName | John, Jane, Admin | Max 100 chars |
| **FullName** | String(255) | No | Full display name | FullName, DisplayName, RealName | "John Smith", "Jane Doe" | Max 255 chars |
| **PasswordHash** | String(255) | Yes | BCrypt password hash | Password, PasswordHash, Hash | $2a$11$... | Must be BCrypt hash (use HashGenerator tool) |
| **Status** | String(20) | Yes | Account status | Status, AccountStatus, State | active, inactive, locked | Must be: active, inactive, or locked |
| **IsActive** | Boolean | No | Active flag | IsActive, Active, Enabled | true, false, 1, 0 | Default: true |

### Security Fields

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **TwoFactorEnabled** | Boolean | No | 2FA enabled flag | TwoFactorEnabled, 2FA, MFAEnabled | true, false | Default: false |
| **TwoFactorMethod** | String | No | 2FA delivery method | TwoFactorMethod, 2FAMethod, MFAMethod | Email, SMS, TOTP, Both | Default: Email |
| **PhoneNumber** | String(20) | No | Phone for SMS 2FA | Phone, PhoneNumber, Mobile, CellPhone | +1-555-555-5555 | Required if TwoFactorMethod includes SMS |
| **IsPhoneVerified** | Boolean | No | Phone verified flag | PhoneVerified, MobileVerified | true, false | Default: false |
| **IsEmailVerified** | Boolean | No | Email verified flag | EmailVerified, Verified | true, false | Default: true |

### Role Assignment

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **Roles** | String | Yes | Comma-separated role names | Role, Roles, UserRole, Permissions | Admin, Analyst, "Admin,Analyst" | Must reference existing role names |

**Role Values:**
- `Admin` - Full system access, can manage users and roles
- `Analyst` - Read/write access, no admin functions
- `External` - Limited access to associated data only
- `Public` - Public view-only access

**Multiple Roles:**
```csv
# Single role
Roles: "Admin"

# Multiple roles (comma-separated)
Roles: "Admin,Analyst"
Roles: "External,Public"
```

### Audit Fields

| GCC Field | Data Type | Required | Description | Migration Notes |
|-----------|-----------|----------|-------------|-----------------|
| **CreatedAt** | DateTime | Auto | Account creation date | Can override during migration |
| **UpdatedAt** | DateTime | Auto | Last update timestamp | System updates automatically |
| **LastLoginAt** | DateTime | No | Last login timestamp | Can import from legacy system |

### Password Hash Requirements

**CRITICAL:** Passwords must be BCrypt hashed before import. Never import plain-text passwords.

**Hash Generation:**
```bash
# Use provided hash generator tool
cd Scripts/HashGenerator
dotnet run -- "TestPass123!"

# Output:
$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa
```

**CSV Example:**
```csv
Username,Email,PasswordHash,Status,Roles
admin,admin@example.com,$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa,active,Admin
analyst,analyst@example.com,$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa,active,Analyst
```

---

## Product ID Fields

The CertificateProductId entity supports multiple product identifiers (ASINs, UPCs, GTINs, etc.) per certificate.

### Core Product ID Information

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values | Validation Rules |
|-----------|-----------|----------|-------------|---------------------|----------------|------------------|
| **Id** | Integer | Auto | Primary key (auto-generated) | ID, ProductIDID, PK | 1, 2, 3 | System-generated |
| **CertificateId** | Integer | Yes | Reference to Certificate | CertificateID, CertID | 1, 5, 10 | Must reference existing Certificate.Id |
| **ProductIdType** | String(10) | Yes | Type of identifier | Type, IDType, IdentifierType | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN | Must be one of supported types |
| **ProductIdValue** | String(50) | Yes | Identifier value | Value, ID, Code, Number | B08N5WRWNW, 012345678905 | Format must match type |
| **Status** | String(20) | No | Product ID status | Status, State | Active, Inactive, Pending, Expired | Default: Active |
| **Partner** | String(50) | No | Associated partner | Partner, Marketplace | Amazon, Wayfair, None, Other | Optional partner association |

### Optional Metadata

| GCC Field | Data Type | Required | Description | Common Source Names | Example Values |
|-----------|-----------|----------|-------------|---------------------|----------------|
| **ProductName** | String(200) | No | Product name for this ID | ProductName, Name, Title | "Oak Hardwood Flooring - Natural" |
| **ProductSku** | String(100) | No | SKU for this ID | SKU, StockCode | "OAK-HW-750-NAT" |
| **Description** | String(500) | No | Additional notes | Description, Notes, Comments | "Primary Amazon listing" |
| **IsActive** | Boolean | No | Active flag | IsActive, Active | true, false (default: true) |
| **IsPublic** | Boolean | No | Public visibility flag | IsPublic, Public, Visible | true, false (default: true) |
| **DisplayOrder** | Integer | No | Sort sequence | DisplayOrder, SortOrder, Sequence | 1, 2, 3 |

### Product ID Types and Formats

| Type | Description | Format | Example | Validation Regex |
|------|-------------|--------|---------|------------------|
| **ASIN** | Amazon Standard ID | 10 alphanumeric | B08N5WRWNW | ^[A-Z0-9]{10}$ |
| **UPC** | Universal Product Code | 12 digits | 012345678905 | ^\d{12}$ |
| **GTIN** | Global Trade Item Number | 8, 12, 13, or 14 digits | 00012345678905 | ^\d{8,14}$ |
| **EAN** | European Article Number | 13 digits | 5901234123457 | ^\d{13}$ |
| **ISBN** | International Book Number | 10 or 13 digits (with hyphens) | 978-0-596-52068-7 | ^\d{9}[\dX]$ or ^\d{13}$ |
| **SKU** | Stock Keeping Unit | Varies by company | ABC-12345-M | No strict format |
| **MPN** | Manufacturer Part Number | Varies by manufacturer | MFR-9876-BLK | No strict format |
| **Other** | Custom identifier | Any format | CUSTOM-ID-001 | No strict format |

### Migration from Legacy Single Product IDs

If your legacy system stores product IDs directly on the certificate (e.g., Certificate.Asin, Certificate.Upc), migrate them to the CertificateProductId table:

**Legacy Data:**
```sql
Certificate:
  Id=1, Number='24-0001', Asin='B08N5WRWNW', Upc='012345678905', Gtin='00012345678905'
```

**New Data:**
```sql
Certificate:
  Id=1, Number='24-0001' (Asin, Upc, Gtin fields now NULL/deprecated)

CertificateProductId:
  Id=1, CertificateId=1, ProductIdType='ASIN', ProductIdValue='B08N5WRWNW', DisplayOrder=1
  Id=2, CertificateId=1, ProductIdType='UPC', ProductIdValue='012345678905', DisplayOrder=2
  Id=3, CertificateId=1, ProductIdType='GTIN', ProductIdValue='00012345678905', DisplayOrder=3
```

---

## Reference Data Fields

### Product Categories

| GCC Field | Data Type | Required | Description | Example Values |
|-----------|-----------|----------|-------------|----------------|
| **Id** | Integer | Auto | Primary key | 1, 2, 3 |
| **Name** | String(255) | Yes | Category name | Flooring, Furniture, Building Materials |
| **Description** | Text | No | Category description | "Flooring products and materials" |
| **SortOrder** | Integer | No | Display sequence | 1, 2, 3 |
| **IsActive** | Boolean | No | Active flag | true, false |

### Certification Types

| GCC Field | Data Type | Required | Description | Example Values |
|-----------|-----------|----------|-------------|----------------|
| **Id** | Integer | Auto | Primary key | 1, 2, 3 |
| **Name** | String(255) | Yes | Certification type name | GreenCircle Certified, FSC Certified, CARB Compliant |
| **AmazonCode** | String(255) | No | Amazon partner code | GCC, FSC, CARB |
| **Description** | Text | No | Type description | "Primary GreenCircle certification" |
| **Level** | Integer | No | Hierarchy level (1=parent, 2+=child) | 1, 2 |
| **ParentId** | Integer | No | Parent certification type | NULL, 1, 2 |
| **SortOrder** | Integer | No | Display sequence | 1, 2, 3 |
| **IsActive** | Boolean | No | Active flag | true, false |

### Partners

| GCC Field | Data Type | Required | Description | Example Values |
|-----------|-----------|----------|-------------|----------------|
| **Id** | Integer | Auto | Primary key | 1, 2, 3 |
| **Name** | String(255) | Yes | Partner name | Amazon, Wayfair, None, Other |
| **Description** | Text | No | Partner description | "Amazon Marketplace" |
| **ContactEmail** | String(255) | No | Partner contact email | partner@amazon.com |
| **ContactPhone** | String(20) | No | Partner contact phone | +1-555-555-5555 |
| **IsActive** | Boolean | No | Active flag | true, false |
| **SortOrder** | Integer | No | Display sequence | 1, 2, 3 |

### Tags

| GCC Field | Data Type | Required | Description | Example Values |
|-----------|-----------|----------|-------------|----------------|
| **Id** | Integer | Auto | Primary key | 1, 2, 3 |
| **Name** | String(100) | Yes | Tag name | Renewable, Sustainable, FSC Certified |
| **IsInternal** | Boolean | No | Internal-only flag | true (hidden from External), false (public) |
| **IsActive** | Boolean | No | Active flag | true, false |

---

## Custom Fields

Custom fields allow you to extend the certificate schema for industry-specific or client-specific requirements.

### Custom Field Definitions

| GCC Field | Data Type | Required | Description | Example Values |
|-----------|-----------|----------|-------------|----------------|
| **Id** | Integer | Auto | Primary key | 1, 2, 3 |
| **Name** | String(100) | Yes | Field label | "FSC Chain of Custody ID", "CARB Compliance Number" |
| **FieldType** | String(20) | Yes | Data type | Text, Number, Date, Boolean, Select |
| **CertificateTypeId** | Integer | No | Applies to specific cert type | 1, 2 (NULL = applies to all types) |
| **IsRequired** | Boolean | No | Required field flag | true, false |
| **DefaultValue** | Text | No | Pre-filled value | "", "2024-01-01" |
| **ValidationRules** | Text | No | Regex validation pattern | ^[A-Z]{3}-[0-9]{6}$ |
| **SortOrder** | Integer | No | Display sequence | 1, 2, 3 |
| **IsActive** | Boolean | No | Active flag | true, false |

### Custom Field Values

| GCC Field | Data Type | Required | Description | Example Values |
|-----------|-----------|----------|-------------|----------------|
| **Id** | Integer | Auto | Primary key | 1, 2, 3 |
| **CertificateId** | Integer | Yes | Reference to Certificate | 1, 5, 10 |
| **FieldId** | Integer | Yes | Reference to CustomField | 1, 2, 3 |
| **Value** | Text | Yes | Field value | "FSC-123456", "2024-03-15", "true" |

### Custom Field Use Cases

1. **Industry-Specific Requirements**
   - FSC Chain of Custody ID (Forestry)
   - CARB Compliance Number (California regulations)
   - Test Report Number (Quality assurance)

2. **Client-Specific Tracking**
   - Internal reference numbers
   - Project codes
   - Approval timestamps

3. **Extended Metadata**
   - Inspector name
   - Lab name
   - Certification body

---

## Common Field Transformations

### Date Format Conversions

**Target Format:** YYYY-MM-DD (ISO 8601)

| Source Format | Example | Transformation | Result |
|---------------|---------|----------------|--------|
| MM/DD/YYYY | 03/15/2024 | Reorder to YYYY-MM-DD | 2024-03-15 |
| DD/MM/YYYY | 15/03/2024 | Reorder to YYYY-MM-DD | 2024-03-15 |
| YYYY/MM/DD | 2024/03/15 | Replace / with - | 2024-03-15 |
| Month DD, YYYY | March 15, 2024 | Parse and format | 2024-03-15 |
| Excel serial | 45369 | Convert serial to date | 2024-03-15 |

**Excel Formula:**
```excel
=TEXT(A1,"YYYY-MM-DD")
```

**SQL Server:**
```sql
CONVERT(VARCHAR, DateField, 23) AS FormattedDate
```

### Boolean Conversions

**Target Format:** true or false (lowercase)

| Source Value | Transformation | Result |
|--------------|----------------|--------|
| 1, Y, Yes, TRUE, T | Convert to boolean true | true |
| 0, N, No, FALSE, F | Convert to boolean false | false |
| NULL, Empty | Default to false | false |

### Status Code Mappings

Map legacy status codes to GCC 14-state workflow (see Certificate Status Values table above).

**SQL Transformation Example:**
```sql
SELECT
  CertificateNumber AS Number,
  CASE
    WHEN OldStatus = 'Valid' THEN 'active'
    WHEN OldStatus = 'Pending' THEN 'pending_review'
    WHEN OldStatus = 'Cancelled' THEN 'cancelled'
    WHEN OldStatus = 'Expired' THEN 'expired'
    ELSE 'draft'
  END AS Status
FROM LegacyCertificates;
```

### Country Code Standardization

**Target Format:** ISO 3166-1 alpha-2 (2-letter codes)

| Source Value | Transformation | Result |
|--------------|----------------|--------|
| United States, USA, U.S.A. | Standardize to ISO code | US |
| Canada, CAN | Standardize to ISO code | CA |
| United Kingdom, UK, Great Britain | Standardize to ISO code | GB |
| Germany, Deutschland, DEU | Standardize to ISO code | DE |

**SQL Transformation Example:**
```sql
SELECT
  CASE
    WHEN Country IN ('United States', 'USA', 'U.S.A.') THEN 'US'
    WHEN Country IN ('Canada', 'CAN') THEN 'CA'
    WHEN Country IN ('United Kingdom', 'UK', 'Great Britain') THEN 'GB'
    ELSE Country
  END AS Country
FROM Companies;
```

### Text Cleaning

| Issue | Example | Fix |
|-------|---------|-----|
| Extra whitespace | "  Acme Corp  " | Trim: "Acme Corp" |
| Smart quotes | "Company "ABC"" | Replace: "Company \"ABC\"" |
| Em dashes | "Product—Description" | Replace: "Product - Description" |
| Line breaks | "Line 1\r\nLine 2" | Replace with space: "Line 1 Line 2" |
| HTML entities | "&amp;", "&lt;", "&gt;" | Decode: "&", "<", ">" |
| Special characters | ©, ®, ™ | Replace or remove |

### Email Standardization

| Issue | Example | Fix |
|-------|---------|-----|
| Uppercase | "JOHN@COMPANY.COM" | Lowercase: "john@company.com" |
| Spaces | "john @ company.com" | Remove spaces: "john@company.com" |
| Missing TLD | "john@company" | Add domain: "john@company.com" |
| Invalid format | "john.company.com" | Add @: "john@company.com" |

### Phone Number Standardization

**Target Format:** E.164 international format: +1-555-555-5555

| Source Format | Example | Transformation | Result |
|---------------|---------|----------------|--------|
| (555) 555-5555 | (555) 555-5555 | Add country code | +1-555-555-5555 |
| 555.555.5555 | 555.555.5555 | Add country code, replace dots | +1-555-555-5555 |
| +1 555 555 5555 | +1 555 555 5555 | Replace spaces with hyphens | +1-555-555-5555 |
| 5555555555 | 5555555555 | Add country code and format | +1-555-555-5555 |

---

## Appendix: Data Type Reference

### GCC Data Types

| Data Type | Description | Max Length | Example Values | CSV Format |
|-----------|-------------|------------|----------------|-----------|
| **Integer** | Whole number | N/A | 1, 42, 1000 | 1, 42, 1000 |
| **String(N)** | Text with max length N | N characters | "Hello", "Acme Corp" | Hello, "Acme Corp" |
| **Text** | Unlimited text | Unlimited | "Long description..." | "Long description..." |
| **Date** | Calendar date | N/A | 2024-03-15 | 2024-03-15 |
| **DateTime** | Date and time | N/A | 2024-03-15 10:30:00 | 2024-03-15 10:30:00 |
| **Boolean** | True/false flag | N/A | true, false | true, false |
| **Decimal** | Decimal number | Varies | 99.99, 1234.5678 | 99.99, 1234.5678 |
| **Email** | Email address | 255 characters | user@example.com | user@example.com |
| **URL** | Web address | 500 characters | https://example.com | https://example.com |

### Required vs Optional

- **REQUIRED** - Field must have a non-NULL, non-empty value
- **Optional** - Field can be NULL or empty
- **Auto** - System-generated, do not import

### Unique Constraints

These fields must have unique values across all records:

| Entity | Unique Fields |
|--------|---------------|
| Certificate | Number |
| Company | Name |
| User | Username, Email |
| ProductCategory | Name |
| CertificationType | Name |
| Tag | Name |
| CertificateProductId | ProductIdType + ProductIdValue (combination) |

---

## Support

For questions about field mappings or data transformations:

- **Email:** swest@anukiconsulting.com
- **Documentation:** `docs/database/DATA_MIGRATION_STRATEGY.md`
- **CSV Templates:** `docs/database/migration_templates/`
- **Tools:** `Scripts/DataValidator/`, `Scripts/HashGenerator/`

---

**END OF DOCUMENT**

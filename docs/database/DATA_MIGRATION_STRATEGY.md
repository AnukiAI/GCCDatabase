# GCC Database Solution - Data Migration Strategy

**Version:** 1.0
**Date:** November 21, 2025
**Status:** Ready for Client Review

## Table of Contents

1. [Overview](#overview)
2. [Migration Approach](#migration-approach)
3. [Entity Dependencies](#entity-dependencies)
4. [Migration Phases](#migration-phases)
5. [Data Validation Rules](#data-validation-rules)
6. [CSV Template Usage](#csv-template-usage)
7. [Common Migration Scenarios](#common-migration-scenarios)
8. [Rollback Strategy](#rollback-strategy)
9. [Quality Assurance](#quality-assurance)

---

## Overview

This document outlines the comprehensive data migration strategy for the GCC Database Solution. It provides the client with a structured approach to review their current data and ensure all fields are properly accounted for during the import process.

### Key Objectives

1. **Zero Data Loss** - All current data fields must be mapped and preserved
2. **Data Integrity** - Maintain all relationships between entities
3. **Validation** - Ensure data quality through automated validation rules
4. **Traceability** - Track all data transformations and migrations
5. **Rollback Capability** - Support safe rollback if issues are detected

### Database Technologies

- **Development:** SQL Server (ANUKI01) - `GCCDatabase_Dev`
- **Production:** SQL Server (AccuWebHosting) - `gccwebap_certificates`
- **Alternative:** SQLite (for Docker/portable deployments)

---

## Migration Approach

### Pre-Migration Phase

#### 1. Data Inventory & Assessment

**Action Items:**
- [ ] Export current database schema (tables, columns, data types)
- [ ] Document all existing data fields and their current usage
- [ ] Identify custom fields or extensions to standard schema
- [ ] Count records in each table for capacity planning
- [ ] Identify data quality issues (duplicates, missing values, invalid formats)

**Tools Provided:**
```bash
# Export current schema
sqlcmd -S [YOUR_SERVER] -d [YOUR_DATABASE] -Q "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS ORDER BY TABLE_NAME, ORDINAL_POSITION" -o schema_current.txt

# Get record counts
sqlcmd -S [YOUR_SERVER] -d [YOUR_DATABASE] -i Scripts/GET_TABLE_COUNTS.sql -o table_counts.txt
```

#### 2. Field Mapping Review

Use the **Field Mapping Reference Guide** (see `FIELD_MAPPING_REFERENCE.md`) to:
- Map each current database field to the new GCC schema
- Identify unmapped fields that need custom field configuration
- Document data transformations required (e.g., date format changes, status code mappings)

#### 3. Data Cleansing

**Common Issues to Address:**
- **Duplicate Records:** Identify and merge duplicate companies, products, users
- **Missing Required Fields:** Populate default values or source missing data
- **Invalid Formats:** Standardize phone numbers, emails, dates, country codes
- **Orphaned Records:** Remove or reassign records with missing foreign keys
- **Inconsistent Naming:** Standardize company names, product names, certification types

---

## Entity Dependencies

Understanding entity dependencies is critical for proper migration order. Data must be imported in dependency order to maintain referential integrity.

### Dependency Hierarchy (Import Order)

```
Level 1 (No Dependencies - Import First):
├── Roles
├── Permissions
├── ProductCategories
├── CertificationTypes
└── Partners

Level 2 (Depends on Level 1):
├── Users (depends on Roles)
├── ProductTypes (depends on ProductCategories)
├── ProductSubcategories (depends on ProductTypes)
├── Companies
├── CustomFields (depends on CertificationTypes)
└── Tags

Level 3 (Depends on Level 1-2):
├── Products (depends on ProductCategories, ProductTypes, ProductSubcategories, Companies)
└── User-Role Assignments (depends on Users, Roles)

Level 4 (Depends on Level 1-3):
├── Certificates (depends on Companies, Products, CertificationTypes, Users)
├── User-Company Associations (depends on Users, Companies)
└── User-Partner Associations (depends on Users, Partners)

Level 5 (Depends on Certificates):
├── CertificateProductIds (depends on Certificates)
├── CustomFieldValues (depends on Certificates, CustomFields)
├── CertificateDocuments (depends on Certificates)
├── Certificate-Tag Associations (depends on Certificates, Tags)
├── Product-Tag Associations (depends on Products, Tags)
└── CertificateStatusHistory (depends on Certificates, Users)

Level 6 (Optional/Advanced):
├── ApiKeys (depends on Users)
├── PartnerApiKeys (depends on Partners)
└── ETL Configurations
```

### Critical Foreign Key Relationships

| Child Entity | Foreign Key Field | Parent Entity | Required? | Notes |
|--------------|-------------------|---------------|-----------|-------|
| Certificate | CompanyId | Company | Yes | Company must exist before certificate |
| Certificate | ProductId | Product | Yes | Product must exist before certificate |
| Certificate | CertificationTypeId | CertificationType | Yes | Cert type must exist before certificate |
| Certificate | CreatedBy | User | No | User who created the certificate |
| Product | CategoryId | ProductCategory | Yes | Category must exist before product |
| Product | ProductTypeId | ProductType | No | Optional product type classification |
| Product | CompanyId | Company | No | Optional company association |
| CertificateProductId | CertificateId | Certificate | Yes | Certificate must exist first |
| CustomFieldValue | CertificateId | Certificate | Yes | Certificate must exist first |
| CustomFieldValue | FieldId | CustomField | Yes | Custom field definition must exist |

---

## Migration Phases

### Phase 1: Foundation Data (Reference Tables)

**Objective:** Establish core reference data that other entities depend on.

**Entities:**
1. Roles (Admin, Analyst, External, Public)
2. Permissions (view_certificate, edit_certificate, etc.)
3. ProductCategories (Flooring, Furniture, Building Materials, etc.)
4. CertificationTypes (GreenCircle Certified, FSC, CARB, etc.)
5. Partners (Amazon, Wayfair, None, Other)

**Import Method:**
- Use SQL INSERT scripts for system-critical data (Roles, Permissions, Partners)
- Use CSV import for client-specific data (ProductCategories, CertificationTypes)

**CSV Templates:**
- `templates/01_roles_template.csv`
- `templates/01_product_categories_template.csv`
- `templates/01_certification_types_template.csv`
- `templates/01_partners_template.csv`

**Validation Checks:**
- [ ] All required roles exist (Admin, Analyst, External, Public)
- [ ] All product categories have unique names
- [ ] All certification types have unique names
- [ ] Partners include Amazon, Wayfair, None as minimum

**Success Criteria:**
- All foundation tables populated
- No duplicate names in reference tables
- All IsActive flags set correctly

---

### Phase 2: Master Data (Companies, Products, Users)

**Objective:** Import core business entities.

#### 2.1 Companies

**CSV Template:** `templates/02_companies_template.csv`

**Required Fields:**
- `Name` (required, unique, max 255 chars)
- `Country` (recommended, max 100 chars)
- `IsActive` (default: true)

**Optional Fields:**
- `ContactInfo`, `Email`, `Website`, `Location`

**Validation Rules:**
- Unique company names (case-insensitive)
- Valid email format if provided
- Valid URL format for website if provided
- Country codes should follow ISO 3166-1 alpha-2 (US, CA, GB, etc.)

**Common Transformations:**
```csv
# Example: Standardizing company names
Current: "acme corp.", "ACME Corporation", "Acme Corp"
Standardized: "Acme Corporation"

# Example: Standardizing countries
Current: "USA", "United States", "U.S.A."
Standardized: "US"
```

#### 2.2 Users

**CSV Template:** `templates/02_users_template.csv`

**Required Fields:**
- `Username` (required, unique, max 255 chars)
- `Email` (required, unique, valid email format)
- `PasswordHash` (required - use BCrypt hash generator provided)
- `Status` (active/inactive/locked, default: active)
- `Roles` (comma-separated: Admin, Analyst, External, Public)

**Optional Fields:**
- `Name`, `FullName`, `PhoneNumber`, `TwoFactorEnabled`, `TwoFactorMethod`

**Security Notes:**
- **CRITICAL:** Passwords must be BCrypt hashed before import
- Use provided `Scripts/HashGenerator/` tool to generate secure hashes
- Never store plain-text passwords in CSV files
- Default password policy: 8+ chars, uppercase, lowercase, number, symbol

**Validation Rules:**
- Unique usernames (case-insensitive)
- Unique emails (case-insensitive)
- Valid email format
- At least one user must have Admin role
- Status must be one of: active, inactive, locked

#### 2.3 Products

**CSV Template:** `templates/02_products_template.csv`

**Required Fields:**
- `Name` (required, max 255 chars)
- `CategoryId` (required, must reference existing ProductCategory)

**Optional Fields:**
- `Description`, `Model`, `Sku`, `Upc`, `Asin`, `ProductTypeId`, `ProductSubcategoryId`, `CompanyId`, `IsActive`

**Validation Rules:**
- CategoryId must reference valid ProductCategory.Id
- ProductTypeId must reference valid ProductType.Id (if provided)
- CompanyId must reference valid Company.Id (if provided)
- Product identifiers (SKU, UPC, ASIN) should be unique if provided

**Common Transformations:**
```csv
# Example: Product category mapping
Current Category: "Wood Flooring"
New CategoryId: 1 (lookup from ProductCategories table)

# Example: Company association
Current: "Manufacturer: Acme Corporation"
New CompanyId: 5 (lookup from Companies table)
```

---

### Phase 3: Certificates (Core Data)

**Objective:** Import certificate records with all required relationships.

**CSV Template:** `templates/03_certificates_template.csv`

**Required Fields:**
- `Number` (required, unique, max 7 chars, format: YY-NNNN, e.g., 24-0001)
- `Status` (required, see status values below)
- `EffectiveDate` (required, format: YYYY-MM-DD)
- `ExpirationDate` (required, format: YYYY-MM-DD)
- `CompanyId` (required, must reference existing Company)
- `ProductId` (required, must reference existing Product)
- `CertificationTypeId` (required, must reference existing CertificationType)

**Optional Fields:**
- `Location`, `Country`, `Partner`, `CertificateFile`, `CertificateImagePath`, `CreatedBy`, `Tags` (comma-separated)

**Certificate Number Format:**
- **Pattern:** `YY-NNNN` where YY = 2-digit year, NNNN = 4-digit sequential number
- **Examples:** `24-0001`, `24-0366`, `25-0001`
- **Validation:** Must be unique, match regex pattern `^\d{2}-\d{4}$`

**Certificate Status Values (14-State Workflow):**

| Status | Description | Typical Use |
|--------|-------------|-------------|
| `draft` | Certificate being created | Initial state |
| `pending_review` | Awaiting review | After draft completion |
| `under_review` | Currently being reviewed | Active review process |
| `pending_approval` | Awaiting approval | After review passes |
| `approved` | Approved but not issued | Approval granted |
| `issued` | Certificate issued to client | Active certificate |
| `active` | Currently valid certificate | Public-facing certificates |
| `expiring_soon` | Expires within 90 days | Auto-flagged by system |
| `renewal_requested` | Renewal in progress | Client requested renewal |
| `suspended` | Temporarily suspended | Issues found |
| `revoked` | Permanently revoked | Serious violations |
| `expired` | Past expiration date | Auto-flagged by system |
| `archived` | Historical record | No longer active |
| `cancelled` | Cancelled before issue | Cancelled during creation |

**Partner Values:**
- `Amazon` - Amazon marketplace certificates
- `Wayfair` - Wayfair marketplace certificates
- `None` - No partner program
- `Other` - Other partners

**Validation Rules:**
- Certificate number must be unique and match format YY-NNNN
- EffectiveDate must be before ExpirationDate
- Status must be one of the 14 valid values
- CompanyId, ProductId, CertificationTypeId must reference existing records
- Country should follow ISO 3166-1 alpha-2 if provided
- Partner must be one of: Amazon, Wayfair, None, Other

**Data Quality Checks:**
```sql
-- Check for invalid date ranges
SELECT Number, EffectiveDate, ExpirationDate
FROM Certificates
WHERE EffectiveDate >= ExpirationDate;

-- Check for missing required foreign keys
SELECT Number FROM Certificates WHERE CompanyId IS NULL OR ProductId IS NULL OR CertificationTypeId IS NULL;

-- Check for invalid status values
SELECT Number, Status FROM Certificates
WHERE Status NOT IN ('draft', 'pending_review', 'under_review', 'pending_approval',
                      'approved', 'issued', 'active', 'expiring_soon', 'renewal_requested',
                      'suspended', 'revoked', 'expired', 'archived', 'cancelled');
```

---

### Phase 4: Certificate Product IDs (Multiple Product Identifiers)

**Objective:** Import multiple product identifiers (ASINs, UPCs, GTINs, etc.) per certificate.

**CSV Template:** `templates/04_certificate_product_ids_template.csv`

**Required Fields:**
- `CertificateId` (required, must reference existing Certificate)
- `ProductIdType` (required, one of: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN, Other)
- `ProductIdValue` (required, max 50 chars)

**Optional Fields:**
- `Status` (Active, Inactive, Pending, Expired - default: Active)
- `Partner` (Amazon, Wayfair, None, Other)
- `ProductName`, `ProductSku`, `Description`, `IsActive`, `IsPublic`, `DisplayOrder`

**Product ID Type Details:**

| Type | Description | Format | Example |
|------|-------------|--------|---------|
| ASIN | Amazon Standard ID | 10 alphanumeric | B08N5WRWNW |
| UPC | Universal Product Code | 12 digits | 012345678905 |
| GTIN | Global Trade Item Number | 8/12/13/14 digits | 00012345678905 |
| EAN | European Article Number | 13 digits | 5901234123457 |
| ISBN | International Book Number | 10 or 13 digits | 978-0-596-52068-7 |
| SKU | Stock Keeping Unit | Varies | ABC-12345-M |
| MPN | Manufacturer Part Number | Varies | MFR-9876-BLK |
| Other | Custom identifier | Varies | CUSTOM-ID-001 |

**Validation Rules:**
- CertificateId must reference valid Certificate.Id
- ProductIdType must be one of the supported types
- ProductIdValue format must match type (e.g., ASIN = 10 chars, UPC = 12 digits)
- Combination of ProductIdType + ProductIdValue must be unique across all certificates
- Status must be one of: Active, Inactive, Pending, Expired

**Format Validation Examples:**
```csv
# ASIN validation (10 alphanumeric characters)
Valid: B08N5WRWNW, B07XYZ1234
Invalid: B08N5WRWN (too short), B08N5WRWNW1 (too long)

# UPC validation (12 digits)
Valid: 012345678905, 639382000393
Invalid: 01234567890 (too short), 0123456789012 (too long), ABC123456789 (non-numeric)

# GTIN validation (8, 12, 13, or 14 digits)
Valid: 12345678, 012345678905, 5901234123457, 00012345678905
Invalid: 1234567 (too short), ABC12345 (non-numeric)
```

**Bulk Import Support:**
- System supports importing hundreds of ASINs per certificate
- Use DisplayOrder field to control display sequence (1, 2, 3, ...)
- Set IsPublic = false to hide product IDs from public view
- Set IsActive = false to soft-delete without removing from database

---

### Phase 5: Extended Data (Custom Fields, Documents, Tags)

#### 5.1 Custom Fields & Values

**CSV Templates:**
- `templates/05_custom_fields_template.csv` (field definitions)
- `templates/05_custom_field_values_template.csv` (field values)

**Custom Field Definition:**
- `Name` (required, max 100 chars)
- `FieldType` (Text, Number, Date, Boolean, Select)
- `CertificateTypeId` (optional - field applies to specific cert type)
- `IsRequired`, `DefaultValue`, `ValidationRules`, `SortOrder`

**Custom Field Values:**
- `CertificateId` (required)
- `FieldId` (required)
- `Value` (required)

**Use Cases:**
- Additional certification metadata not in standard schema
- Industry-specific requirements (e.g., FSC Chain of Custody ID)
- Client-specific tracking fields

#### 5.2 Tags (Classification)

**CSV Template:** `templates/05_tags_template.csv`

**Fields:**
- `Name` (required, unique, max 100 chars)
- `IsInternal` (true/false - internal tags hidden from External users)
- `IsActive` (true/false)

**Certificate-Tag Associations:**
- Import via `Tags` column in certificates CSV (comma-separated)
- Example: `Tags: "Renewable,Sustainable,FSC Certified"`

**Tag Types:**
- **Public Tags:** Visible to all users (e.g., "Renewable", "Recycled Content")
- **Internal Tags:** Visible only to Admin/Analyst (e.g., "Priority Review", "Legal Hold")

#### 5.3 Certificate Documents

**CSV Template:** `templates/05_certificate_documents_template.csv`

**Fields:**
- `CertificateId` (required)
- `DocumentType` (Certificate, Supporting, Test Report, Photo, Other)
- `FileName` (required)
- `FilePath` (required - relative to uploads directory)
- `FileSize` (bytes)
- `FileHash` (SHA-256 hash for integrity)
- `UploadedBy` (User.Id)

**Document Migration Strategy:**
1. Copy physical files to `wwwroot/uploads/certificates/` directory
2. Calculate SHA-256 hash for each file: `Get-FileHash -Algorithm SHA256 file.pdf`
3. Import document metadata via CSV
4. System validates file existence and hash on import

**Supported File Types:**
- **Certificates:** PDF (primary), PNG, JPG (images)
- **Supporting Docs:** PDF, DOCX, XLSX, PNG, JPG, ZIP
- **Max File Size:** 100 MB (configurable in appsettings.json)

---

## Data Validation Rules

### Automated Validation (Pre-Import)

The system provides automated validation tools to check CSV files before import:

```bash
# Validate CSV files
dotnet run --project Scripts/DataValidator validate --file companies.csv --entity Company
dotnet run --project Scripts/DataValidator validate --file certificates.csv --entity Certificate

# Validate entire migration package
dotnet run --project Scripts/DataValidator validate-all --directory migration_data/
```

### Validation Categories

#### 1. Required Field Validation
- All required fields must have non-null, non-empty values
- Required fields identified in CSV templates with `[REQUIRED]` marker

#### 2. Format Validation
- **Dates:** Must be in YYYY-MM-DD format (ISO 8601)
- **Emails:** Must match regex `^[^@]+@[^@]+\.[^@]+$`
- **URLs:** Must start with http:// or https://
- **Certificate Numbers:** Must match `^\d{2}-\d{4}$` (e.g., 24-0001)
- **Phone Numbers:** Recommended format: +1-555-555-5555 (E.164)
- **Country Codes:** ISO 3166-1 alpha-2 (US, CA, GB, etc.)

#### 3. Referential Integrity Validation
- Foreign key values must reference existing parent records
- Example: Product.CompanyId must reference existing Company.Id

#### 4. Uniqueness Validation
- Fields with unique constraints must have no duplicates
- Examples: Certificate.Number, User.Username, User.Email, Company.Name

#### 5. Business Logic Validation
- Certificate EffectiveDate < ExpirationDate
- User must have at least one role
- Active certificates must have valid status
- Product identifiers must follow type-specific formats

#### 6. Data Range Validation
- Dates should be within reasonable ranges (not year 1900 or 2099+)
- Numeric values within expected ranges
- String lengths not exceeding max length constraints

### Validation Report Format

```
=== Validation Report ===
File: companies.csv
Entity: Company
Date: 2025-11-21 10:30:00

ERRORS (3):
  Row 15, Column 'Name': Required field is empty
  Row 23, Column 'Email': Invalid email format 'john@company' (missing TLD)
  Row 45, Column 'Name': Duplicate value 'Acme Corporation' (also in row 12)

WARNINGS (2):
  Row 8, Column 'Country': Value 'USA' should be 'US' (ISO 3166-1 alpha-2)
  Row 19, Column 'Website': Missing protocol (should be 'https://example.com')

SUMMARY:
  Total Rows: 50
  Valid Rows: 47
  Errors: 3
  Warnings: 2
  Status: FAILED (errors must be fixed before import)
```

---

## CSV Template Usage

### Template File Locations

All CSV templates are located in:
```
GCCWebApp/docs/database/migration_templates/
```

### Template Structure

Each CSV template includes:
1. **Header Row:** Column names matching database field names
2. **Data Type Row:** Shows expected data type for each column
3. **Example Rows:** 2-3 example rows with realistic sample data
4. **Notes Row:** Special instructions or validation rules

### Template Naming Convention

```
[Phase]_[Entity]_template.csv

Examples:
01_roles_template.csv
02_companies_template.csv
03_certificates_template.csv
04_certificate_product_ids_template.csv
05_tags_template.csv
```

### Using Templates

#### Step 1: Download Template
```bash
# Download all templates
git clone [repository] && cd GCCWebApp/docs/database/migration_templates/

# Or download individual template from repository
```

#### Step 2: Populate Template
1. Open template in Excel, Google Sheets, or text editor
2. **Delete the data type row and notes row** (keep only header row)
3. Add your data rows, following format examples
4. **Save as CSV (UTF-8 encoding)**

**IMPORTANT:**
- Use UTF-8 encoding to support international characters
- Escape commas in data values with double quotes: `"Acme, Inc."`
- Escape quotes by doubling them: `"He said ""hello"""`
- Use `\n` for line breaks within fields
- Leave optional fields blank (empty string) if not applicable

#### Step 3: Validate Template
```bash
# Validate before import
dotnet run --project Scripts/DataValidator validate --file your_companies.csv --entity Company
```

#### Step 4: Import Data
```bash
# Import via admin interface
# Navigate to: https://gccwebapp.com/Admin/DataImport
# Upload CSV file
# Review validation results
# Confirm import

# Or import via command line
dotnet run --project Scripts/DataImporter import --file your_companies.csv --entity Company
```

### Common Template Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Import fails with "Invalid CSV format" | Wrong encoding | Save as UTF-8, not ANSI or UTF-16 |
| Commas break columns | Unescaped commas in data | Wrap values in quotes: `"Acme, Inc."` |
| Dates not recognized | Wrong format | Use YYYY-MM-DD, not MM/DD/YYYY |
| Foreign key errors | Parent record doesn't exist | Import parent entities first |
| Duplicate errors | Records already exist | Check for existing data, use update mode |
| Special characters garbled | Wrong encoding | Ensure UTF-8 encoding |

---

## Common Migration Scenarios

### Scenario 1: Fresh Database (New Installation)

**Situation:** Client has no existing database, starting fresh with new data.

**Steps:**
1. Use provided seed data script: `dotnet run --seed-data`
2. Import client-specific reference data (ProductCategories, CertificationTypes)
3. Import Companies, Products, Users
4. Import Certificates
5. Import Product IDs and extended data

**Timeline:** 1-2 hours for small dataset (<1000 records), 4-8 hours for large dataset (10,000+ records)

### Scenario 2: Migration from Spreadsheets (Excel/Google Sheets)

**Situation:** Client currently manages certificates in Excel or Google Sheets.

**Steps:**
1. Export Excel data to CSV format (UTF-8 encoding)
2. Map Excel columns to GCC database fields using Field Mapping Guide
3. Clean data (remove duplicates, standardize formats)
4. Use CSV templates to structure data correctly
5. Validate CSV files using validation tools
6. Import in dependency order (Phase 1 → Phase 5)

**Common Excel → CSV Issues:**
- Excel date formats (convert to YYYY-MM-DD)
- Leading zeros removed (e.g., UPC codes) - format as text in Excel
- Special characters (smart quotes, em dashes) - replace with standard characters
- Formula results instead of values - copy/paste as values only

### Scenario 3: Migration from Legacy Database (SQL Server/MySQL/PostgreSQL)

**Situation:** Client has existing database with different schema.

**Steps:**
1. Export legacy database schema and create field mapping document
2. Write SQL transformation queries to map old schema to new schema
3. Export transformed data to CSV files
4. Validate CSV files
5. Import via CSV templates or direct SQL INSERT (for large datasets)

**SQL Export Examples:**
```sql
-- Export Companies from legacy database
SELECT
  Name,
  ContactPerson AS ContactInfo,
  Email,
  Website,
  CONCAT(City, ', ', State) AS Location,
  CountryCode AS Country,
  IsActive = CASE WHEN Status = 'Active' THEN 1 ELSE 0 END
FROM LegacyCompanies
WHERE IsDeleted = 0;

-- Export Certificates with field transformations
SELECT
  CertNumber AS Number,
  CASE
    WHEN Status = 'Valid' THEN 'active'
    WHEN Status = 'Pending' THEN 'pending_review'
    WHEN Status = 'Cancelled' THEN 'cancelled'
    ELSE 'draft'
  END AS Status,
  CONVERT(VARCHAR, StartDate, 23) AS EffectiveDate,  -- YYYY-MM-DD
  CONVERT(VARCHAR, EndDate, 23) AS ExpirationDate,
  CompanyID AS CompanyId,
  ProductID AS ProductId,
  CertTypeID AS CertificationTypeId
FROM LegacyCertificates;
```

### Scenario 4: Partial Migration (Specific Entities Only)

**Situation:** Client wants to import only certain entities (e.g., new products) without full migration.

**Steps:**
1. Identify entities to import
2. Ensure all dependent entities already exist (e.g., ProductCategories before Products)
3. Export existing IDs from database for foreign key references
4. Prepare CSV files with foreign key values
5. Import using "append" mode (don't overwrite existing data)

**Example:** Importing new products for existing company
```csv
Name,Description,CategoryId,CompanyId,IsActive
"New Product A","Description",1,5,true
"New Product B","Description",2,5,true
```

### Scenario 5: Data Update/Refresh (Overwrite Existing)

**Situation:** Client wants to update existing records with new data.

**Steps:**
1. Export current database to CSV (for backup)
2. Prepare updated CSV files with same IDs
3. Use "update" mode in import tool
4. System matches by ID or unique field (e.g., Certificate.Number)
5. Updates existing records, creates new records if not found

**Update Mode Behavior:**
- Match by primary ID if provided
- Match by unique field (Certificate.Number, User.Email) if ID not provided
- Update all fields from CSV (NULL values will overwrite existing data)
- Preserve audit fields (CreatedAt, CreatedBy) but update UpdatedAt, UpdatedBy

---

## Rollback Strategy

### Pre-Migration Backup

**CRITICAL:** Always create full database backup before migration.

```bash
# SQL Server backup
sqlcmd -S [SERVER] -Q "BACKUP DATABASE gccwebap_certificates TO DISK='C:\Backups\pre_migration_backup.bak'"

# Or use SSMS GUI: Right-click database → Tasks → Back Up

# SQLite backup
cp gcc_database.db gcc_database_backup_$(date +%Y%m%d).db
```

### Migration Transaction Log

The system logs all migration operations:
- **ETLLogEntries** table - Detailed operation log
- **ETLDataQualityIssues** table - Data quality problems detected
- **ETLAlerts** table - Critical issues requiring attention

### Rollback Procedures

#### Option 1: Full Database Restore (Recommended)
```bash
# Restore SQL Server backup
sqlcmd -S [SERVER] -Q "RESTORE DATABASE gccwebap_certificates FROM DISK='C:\Backups\pre_migration_backup.bak' WITH REPLACE"
```

#### Option 2: Incremental Rollback (Undo Specific Import)
```sql
-- Find migration batch ID
SELECT DISTINCT JobId, Timestamp, Message
FROM ETLLogEntries
WHERE Message LIKE '%Import started%'
ORDER BY Timestamp DESC;

-- Delete records created in specific batch
DELETE FROM CertificateProductIds WHERE CreatedAt >= '2025-11-21 10:00:00';
DELETE FROM Certificates WHERE CreatedAt >= '2025-11-21 10:00:00';

-- Verify deletion
SELECT COUNT(*) FROM Certificates WHERE CreatedAt >= '2025-11-21 10:00:00';
```

#### Option 3: Soft Delete (Mark as Inactive)
```sql
-- Mark imported records as inactive instead of deleting
UPDATE Certificates SET IsActive = 0, UpdatedAt = GETDATE()
WHERE CreatedAt >= '2025-11-21 10:00:00';

-- Verify
SELECT COUNT(*) FROM Certificates WHERE IsActive = 0;
```

### Rollback Checklist

After rollback:
- [ ] Verify database restored successfully
- [ ] Check record counts match pre-migration counts
- [ ] Test application functionality (login, view certificates, search)
- [ ] Review ETL logs to understand what went wrong
- [ ] Fix data issues identified
- [ ] Plan corrective actions before re-attempting migration

---

## Quality Assurance

### Pre-Import QA Checklist

**Data Quality:**
- [ ] All required fields populated
- [ ] No duplicate records in unique fields
- [ ] All foreign keys reference existing records
- [ ] Date ranges valid (EffectiveDate < ExpirationDate)
- [ ] Formats match validation rules (emails, URLs, phone numbers)
- [ ] Certificate numbers follow YY-NNNN pattern
- [ ] Status values match allowed values
- [ ] Product ID formats valid for their type (ASIN, UPC, etc.)

**CSV File Quality:**
- [ ] UTF-8 encoding (supports international characters)
- [ ] Header row matches template exactly
- [ ] No extra columns or missing columns
- [ ] Commas escaped properly ("Acme, Inc.")
- [ ] Quotes escaped properly ("He said ""hello""")
- [ ] File size reasonable (under 50 MB per file)
- [ ] Line endings consistent (Windows CRLF or Unix LF)

**Business Logic:**
- [ ] At least one Admin user exists
- [ ] All companies have unique names
- [ ] All products assigned to valid categories
- [ ] All certificates have valid company, product, and cert type
- [ ] Public-facing certificates have status "active" or "expiring_soon"

### Post-Import QA Checklist

**Database Integrity:**
- [ ] Record counts match expected (compare to source data)
- [ ] No orphaned records (foreign keys all valid)
- [ ] Unique constraints enforced (no duplicates)
- [ ] Audit fields populated (CreatedAt, UpdatedAt)
- [ ] Relationships intact (certificates → companies, products, etc.)

**Application Testing:**
- [ ] Login with test users (Admin, Analyst, External)
- [ ] View certificate list (public, internal, external views)
- [ ] Search certificates by number, company, product
- [ ] Filter certificates by status, partner, country
- [ ] Download certificate PDFs
- [ ] View certificate details
- [ ] Edit certificate (Admin/Analyst only)
- [ ] Export data to CSV

**Data Validation Queries:**
```sql
-- Verify record counts
SELECT 'Companies' AS Entity, COUNT(*) AS Count FROM Companies
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL
SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds;

-- Check for orphaned certificates (invalid foreign keys)
SELECT c.Number, c.CompanyId, c.ProductId, c.CertificationTypeId
FROM Certificates c
LEFT JOIN Companies co ON c.CompanyId = co.Id
LEFT JOIN Products p ON c.ProductId = p.Id
LEFT JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
WHERE co.Id IS NULL OR p.Id IS NULL OR ct.Id IS NULL;

-- Check for duplicate certificate numbers
SELECT Number, COUNT(*) AS Count
FROM Certificates
GROUP BY Number
HAVING COUNT(*) > 1;

-- Check for invalid status values
SELECT DISTINCT Status FROM Certificates
WHERE Status NOT IN ('draft', 'pending_review', 'under_review', 'pending_approval',
                      'approved', 'issued', 'active', 'expiring_soon', 'renewal_requested',
                      'suspended', 'revoked', 'expired', 'archived', 'cancelled');

-- Check for date range issues
SELECT Number, EffectiveDate, ExpirationDate
FROM Certificates
WHERE EffectiveDate >= ExpirationDate;

-- Check for missing required user roles
SELECT u.Username, COUNT(r.Id) AS RoleCount
FROM Users u
LEFT JOIN UserRoles ur ON u.Id = ur.UsersId
LEFT JOIN Roles r ON ur.RolesId = r.Id
GROUP BY u.Id, u.Username
HAVING COUNT(r.Id) = 0;
```

### Performance Testing

After large data imports, verify system performance:

```sql
-- Test query performance (should complete in < 1 second)
SET STATISTICS TIME ON;

-- Search certificates
SELECT * FROM Certificates WHERE Number LIKE '24-%';

-- Join query (certificates with company and product)
SELECT c.Number, co.Name AS Company, p.Name AS Product
FROM Certificates c
INNER JOIN Companies co ON c.CompanyId = co.Id
INNER JOIN Products p ON c.ProductId = p.Id
WHERE c.Status = 'active';

-- Count by status
SELECT Status, COUNT(*) AS Count
FROM Certificates
GROUP BY Status;

SET STATISTICS TIME OFF;
```

**Performance Benchmarks:**
- Simple queries (< 1 second for 10,000 records)
- Complex joins (< 3 seconds for 10,000 records)
- Aggregations (< 2 seconds for 10,000 records)
- Page load (< 2 seconds for certificate list)

If performance is slow:
1. Check indexes are created (run `dotnet ef migrations list`)
2. Update database statistics: `EXEC sp_updatestats;`
3. Rebuild indexes: `ALTER INDEX ALL ON Certificates REBUILD;`
4. Verify query execution plans in SSMS

---

## Support & Troubleshooting

### Common Issues

#### Issue: CSV Import Fails with "Invalid Format"
**Cause:** Wrong encoding or special characters
**Solution:** Save CSV as UTF-8, escape commas and quotes properly

#### Issue: Foreign Key Constraint Violation
**Cause:** Referencing non-existent parent record
**Solution:** Import parent entities first (see dependency hierarchy), verify IDs exist

#### Issue: Duplicate Key Error
**Cause:** Trying to insert duplicate unique value (certificate number, email, etc.)
**Solution:** Check for existing records, use update mode instead of insert

#### Issue: Date Format Error
**Cause:** Wrong date format (MM/DD/YYYY instead of YYYY-MM-DD)
**Solution:** Convert dates to ISO 8601 format (YYYY-MM-DD)

#### Issue: Import Succeeds but Data Not Visible
**Cause:** Records marked as inactive or deleted
**Solution:** Check `IsActive` and `IsDeleted` flags, update if needed

### Getting Help

**Documentation:**
- Field Mapping Reference: `docs/database/FIELD_MAPPING_REFERENCE.md`
- Database Schema: `docs/database/reference/SCHEMA_DOCUMENTATION.md`
- Connection Strings: `docs/database/reference/CONNECTION_STRINGS.md`
- Troubleshooting: `docs/database/troubleshooting/COMMON_ISSUES.md`

**Tools:**
- Data Validator: `Scripts/DataValidator/`
- Data Importer: `Scripts/DataImporter/`
- Hash Generator: `Scripts/HashGenerator/`
- SQL Scripts: `Scripts/database/`

**Contact:**
- Email: swest@anukiconsulting.com
- Project Repository: [GitHub URL]

---

## Appendices

### Appendix A: Database Schema Diagram

See `docs/database/diagrams/SCHEMA_ERD.md` for complete Entity-Relationship Diagram.

### Appendix B: CSV Template Index

| Template | Entity | Phase | Dependencies |
|----------|--------|-------|--------------|
| 01_roles_template.csv | Roles | 1 | None |
| 01_product_categories_template.csv | ProductCategories | 1 | None |
| 01_certification_types_template.csv | CertificationTypes | 1 | None |
| 01_partners_template.csv | Partners | 1 | None |
| 02_companies_template.csv | Companies | 2 | None |
| 02_users_template.csv | Users | 2 | Roles |
| 02_products_template.csv | Products | 2 | ProductCategories, Companies |
| 03_certificates_template.csv | Certificates | 3 | Companies, Products, CertificationTypes, Users |
| 04_certificate_product_ids_template.csv | CertificateProductIds | 4 | Certificates |
| 05_custom_fields_template.csv | CustomFields | 5 | CertificationTypes |
| 05_custom_field_values_template.csv | CustomFieldValues | 5 | Certificates, CustomFields |
| 05_tags_template.csv | Tags | 5 | None |
| 05_certificate_documents_template.csv | CertificateDocuments | 5 | Certificates, Users |

### Appendix C: Status Code Mappings (Legacy → New)

Common legacy status values mapped to GCC 14-state workflow:

| Legacy Status | GCC Status | Notes |
|---------------|------------|-------|
| New, Created, Draft | draft | Initial state |
| Pending, Under Review | pending_review | Awaiting review |
| In Review, Reviewing | under_review | Active review |
| Approved, Accepted | approved | Approved but not issued |
| Issued, Published | issued | Issued to client |
| Active, Valid, Current | active | Currently valid |
| Expiring, Near Expiration | expiring_soon | Within 90 days of expiration |
| Renewing, Renewal | renewal_requested | Renewal in progress |
| Suspended, On Hold | suspended | Temporarily suspended |
| Revoked, Withdrawn | revoked | Permanently revoked |
| Expired, Past Due | expired | Past expiration date |
| Archived, Historical | archived | Historical record |
| Cancelled, Rejected | cancelled | Cancelled before issue |

### Appendix D: Country Code Reference (ISO 3166-1 Alpha-2)

Common country codes for certificate location data:

| Code | Country |
|------|---------|
| US | United States |
| CA | Canada |
| MX | Mexico |
| GB | United Kingdom |
| DE | Germany |
| FR | France |
| CN | China |
| JP | Japan |
| IN | India |
| BR | Brazil |
| AU | Australia |
| NZ | New Zealand |

Full list: https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2

---

## Document Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-21 | Claude Code | Initial comprehensive migration strategy document |

---

**END OF DOCUMENT**

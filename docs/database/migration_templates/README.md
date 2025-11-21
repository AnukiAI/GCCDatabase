# GCC Database - CSV Import Templates

**Version:** 1.0
**Date:** November 21, 2025
**Purpose:** CSV templates for importing data into GCC Database Solution

## Overview

This directory contains CSV templates for importing data into the GCC database. Each template corresponds to a specific database entity and includes:

- **Header Row:** Column names matching database field names
- **Data Type Row:** Expected data types and constraints
- **Example Rows:** 2-3 sample records with realistic data
- **Notes Row:** Special instructions and validation rules

## Quick Start

### 1. Review Migration Documentation

Before using these templates, read the comprehensive migration guides:

- **[Data Migration Strategy](../DATA_MIGRATION_STRATEGY.md)** - Complete migration approach and process
- **[Field Mapping Reference](../FIELD_MAPPING_REFERENCE.md)** - Field-by-field mapping guide

### 2. Download Template

Select the appropriate template for your import:

```bash
# Download template from this directory
# Example: 02_companies_template.csv
```

### 3. Prepare Your Data

1. **Open Template** - Open in Excel, Google Sheets, or text editor
2. **Delete Example Rows** - Remove data type row, example rows, and notes row (keep only header)
3. **Add Your Data** - Fill in your data following the format examples
4. **Save as UTF-8 CSV** - Critical: Must be UTF-8 encoding for international characters

### 4. Validate Your CSV

```bash
# Use validation tool before import
dotnet run --project Scripts/DataValidator validate --file your_data.csv --entity Company
```

### 5. Import Data

```bash
# Via admin web interface
https://gccwebapp.com/Admin/DataImport

# Or via command line
dotnet run --project Scripts/DataImporter import --file your_data.csv --entity Company
```

## Template Index

### Phase 1: Foundation Data (Reference Tables)

Import these first as they have no dependencies:

| Template | Entity | Description | Dependencies |
|----------|--------|-------------|--------------|
| [01_roles_template.csv](01_roles_template.csv) | Roles | User roles (Admin, Analyst, External, Public) | None - Pre-seeded |
| [01_product_categories_template.csv](01_product_categories_template.csv) | ProductCategories | Product categories (Flooring, Furniture, etc.) | None |
| [01_certification_types_template.csv](01_certification_types_template.csv) | CertificationTypes | Certification types (GreenCircle, FSC, CARB, etc.) | None |
| [01_partners_template.csv](01_partners_template.csv) | Partners | Partners (Amazon, Wayfair, None, Other) | None - Core entities only |

### Phase 2: Master Data (Companies, Products, Users)

Import after Phase 1:

| Template | Entity | Description | Dependencies |
|----------|--------|-------------|--------------|
| [02_companies_template.csv](02_companies_template.csv) | Companies | Certificate holders and manufacturers | None |
| [02_users_template.csv](02_users_template.csv) | Users | System users with roles and authentication | Roles |
| [02_products_template.csv](02_products_template.csv) | Products | Products being certified | ProductCategories, Companies (optional) |

### Phase 3: Certificates (Core Data)

Import after Phase 2:

| Template | Entity | Description | Dependencies |
|----------|--------|-------------|--------------|
| [03_certificates_template.csv](03_certificates_template.csv) | Certificates | Certificates with 14-state workflow | Companies, Products, CertificationTypes, Users |

### Phase 4: Certificate Product IDs

Import after Phase 3:

| Template | Entity | Description | Dependencies |
|----------|--------|-------------|--------------|
| [04_certificate_product_ids_template.csv](04_certificate_product_ids_template.csv) | CertificateProductIds | Multiple product IDs per certificate (ASIN, UPC, GTIN, etc.) | Certificates |

### Phase 5: Extended Data (Custom Fields, Documents, Tags)

Import after Phases 1-4:

| Template | Entity | Description | Dependencies |
|----------|--------|-------------|--------------|
| [05_tags_template.csv](05_tags_template.csv) | Tags | Classification tags (public and internal) | None |
| [05_custom_fields_template.csv](05_custom_fields_template.csv) | CustomFields | Custom field definitions | CertificationTypes (optional) |
| [05_custom_field_values_template.csv](05_custom_field_values_template.csv) | CustomFieldValues | Custom field values for certificates | Certificates, CustomFields |
| [05_certificate_documents_template.csv](05_certificate_documents_template.csv) | CertificateDocuments | Certificate PDFs and supporting files | Certificates, Users |

## Template Structure

Each template follows this structure:

```csv
FieldName1,FieldName2,FieldName3
[DATA_TYPE CONSTRAINTS],[DATA_TYPE CONSTRAINTS],[DATA_TYPE CONSTRAINTS]
Example Value 1,Example Value 2,Example Value 3
Example Value 4,Example Value 5,Example Value 6
[NOTES] Instructions and validation rules for this entity...
```

**Before importing:**
1. Delete the data type row (row 2)
2. Delete the example rows (rows 3-4)
3. Delete the notes row (last row)
4. Keep only the header row and your data rows

## CSV Formatting Rules

### UTF-8 Encoding

**Critical:** All CSV files must be saved with UTF-8 encoding to support international characters.

**How to save as UTF-8:**
- **Excel:** Save As → CSV UTF-8 (Comma delimited) (*.csv)
- **Google Sheets:** File → Download → Comma-separated values (.csv, current sheet)
- **Notepad++:** Encoding → Convert to UTF-8
- **VS Code:** Save with Encoding → UTF-8

### Escaping Special Characters

| Character | How to Escape | Example |
|-----------|---------------|---------|
| **Comma** | Wrap in quotes | `"Acme, Inc."` |
| **Quote** | Double the quote | `"He said ""hello"""` |
| **Line break** | Use \n | `"Line 1\nLine 2"` |
| **Leading/trailing spaces** | Wrap in quotes | `"  Acme Corp  "` |

### Required vs Optional Fields

- **[REQUIRED]** in data type row = Field must have a value
- No [REQUIRED] marker = Field is optional (can be empty)
- **[AUTO]** or system-generated fields = Do not include in CSV (e.g., Id, CreatedAt)

### Data Type Notation

| Notation | Meaning | Example |
|----------|---------|---------|
| `[TEXT(255)]` | String, max 255 characters | "Company Name" |
| `[INTEGER]` | Whole number | 1, 42, 1000 |
| `[DATE]` | Date in YYYY-MM-DD format | 2024-03-15 |
| `[BOOLEAN]` | true or false (lowercase) | true, false |
| `[EMAIL(255)]` | Valid email address | user@example.com |
| `[URL(500)]` | Valid URL starting with http:// or https:// | https://example.com |

## Common Issues & Solutions

### Issue: Import fails with "Invalid CSV format"

**Cause:** Wrong encoding (ANSI instead of UTF-8)
**Solution:** Re-save file as UTF-8 (see Encoding section above)

### Issue: Commas in data break column alignment

**Cause:** Unescaped commas in data values
**Solution:** Wrap values containing commas in double quotes: `"Acme, Inc."`

### Issue: Dates not recognized

**Cause:** Wrong date format (MM/DD/YYYY instead of YYYY-MM-DD)
**Solution:** Convert all dates to ISO 8601 format: YYYY-MM-DD

### Issue: Foreign key constraint violation

**Cause:** Referencing non-existent parent record (e.g., CompanyId=999 but Company.Id=999 doesn't exist)
**Solution:** Import parent entities first (see dependency order above)

### Issue: Duplicate key error

**Cause:** Trying to insert duplicate unique value (e.g., duplicate certificate number)
**Solution:** Check for duplicates in your data, ensure uniqueness for constrained fields

### Issue: Import succeeds but data not visible

**Cause:** Records marked as inactive (IsActive=false) or deleted (IsDeleted=true)
**Solution:** Set IsActive=true and IsDeleted=false for all records you want visible

## Example: Importing Companies

### Step 1: Download Template

Download `02_companies_template.csv` from this directory.

### Step 2: Open and Clean Template

Original template:
```csv
Name,ContactInfo,Email,Website,Location,Country,IsActive
[TEXT(255) REQUIRED],[TEXT(255)],[EMAIL(255)],[URL(500)],[TEXT(175)],[TEXT(100)],[BOOLEAN]
"Acme Corporation",John Smith,contact@acmecorp.com,https://www.acmecorp.com,"Seattle, WA",US,true
"Global Flooring Solutions Ltd",Jane Doe,info@globalflooring.com,https://www.globalflooring.com,"Toronto, ON",CA,true
[NOTES] ...
```

After cleaning (ready for your data):
```csv
Name,ContactInfo,Email,Website,Location,Country,IsActive
```

### Step 3: Add Your Data

```csv
Name,ContactInfo,Email,Website,Location,Country,IsActive
"Your Company A",John Smith,contact@companya.com,https://www.companya.com,"New York, NY",US,true
"Your Company B",Jane Doe,info@companyb.com,https://www.companyb.com,"Vancouver, BC",CA,true
```

### Step 4: Save as UTF-8 CSV

File → Save As → CSV UTF-8 (*.csv) → Save as `my_companies.csv`

### Step 5: Validate

```bash
dotnet run --project Scripts/DataValidator validate --file my_companies.csv --entity Company
```

Expected output:
```
=== Validation Report ===
File: my_companies.csv
Entity: Company

SUMMARY:
  Total Rows: 2
  Valid Rows: 2
  Errors: 0
  Warnings: 0
  Status: PASSED
```

### Step 6: Import

Upload via admin interface:
1. Navigate to: https://gccwebapp.com/Admin/DataImport
2. Select Entity: Company
3. Choose File: my_companies.csv
4. Click "Validate"
5. Review validation results
6. Click "Import" if validation passes

## Validation Tools

### Command-Line Validator

```bash
# Validate single file
dotnet run --project Scripts/DataValidator validate --file your_data.csv --entity Company

# Validate all files in directory
dotnet run --project Scripts/DataValidator validate-all --directory migration_data/

# Output detailed error report
dotnet run --project Scripts/DataValidator validate --file your_data.csv --entity Company --output errors.txt
```

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

## Additional Tools

### Password Hash Generator

For User imports, passwords must be BCrypt hashed:

```bash
cd Scripts/HashGenerator
dotnet run -- "YourPassword123!"

Output: $2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa
```

### SQL Scripts

Useful SQL scripts for data migration:

- `Scripts/GET_TABLE_COUNTS.sql` - Get record counts for all tables
- `Scripts/EXPORT_SCHEMA.sql` - Export current database schema
- `Scripts/VALIDATE_RELATIONSHIPS.sql` - Check foreign key integrity

## Support

### Documentation

- **Migration Strategy:** [../DATA_MIGRATION_STRATEGY.md](../DATA_MIGRATION_STRATEGY.md)
- **Field Mapping Guide:** [../FIELD_MAPPING_REFERENCE.md](../FIELD_MAPPING_REFERENCE.md)
- **Database Schema:** [../reference/SCHEMA_DOCUMENTATION.md](../reference/SCHEMA_DOCUMENTATION.md)
- **Connection Strings:** [../reference/CONNECTION_STRINGS.md](../reference/CONNECTION_STRINGS.md)
- **Troubleshooting:** [../troubleshooting/COMMON_ISSUES.md](../troubleshooting/COMMON_ISSUES.md)

### Contact

- **Email:** swest@anukiconsulting.com
- **Project Repository:** [GitHub URL]

---

**Last Updated:** November 21, 2025
**Version:** 1.0


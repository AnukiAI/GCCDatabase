# GCC Data Migration Guide

## Overview

This guide explains how to prepare data for migration into the GCC Certificate Management System. The client should populate the CSV templates in the order specified below.

## Data Import Order (Dependencies)

Data must be imported in this order due to foreign key relationships:

```
1. Certification Types       (no dependencies)
2. Product Categories        (no dependencies)
3. Product Types             (depends on: Product Categories)
3a. Product Subcategories    (depends on: Product Types) - OPTIONAL
4. Companies                 (no dependencies)
5. Products                  (depends on: Categories, Types, Subcategories, Companies)
6. Tags                      (no dependencies) - OPTIONAL
7. Certificates              (depends on: Companies, Products, Certification Types)
8. Certificate Product IDs   (depends on: Certificates) - FOR AMAZON INTEGRATION
```

---

## CSV Templates

### 1. Certification Types (`01_certification_types.csv`)

The types of certifications your organization issues.

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 255 | Certification type name | Recycled Content |
| AMAZON_CODE | No | 255 | Amazon partner code | RC |
| DESCRIPTION | No | - | Description of cert type | Recycled content certification |
| PARENT_NAME | No | 255 | Parent type name (for subcategories) | |

**Current System Values:**
- Recycled Content
- GreenCircle Audited
- Certified Environmental Facts
- Waste Diversion from Landfill
- Carbon Footprint Reduction
- Material Ingredient Reporting
- Certified Sustainability Facts
- LCA Optimized
- Certified Energy Savings
- Closed Loop Product

---

### 2. Product Categories (`02_product_categories.csv`)

High-level groupings for products.

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 255 | Category name | Building Materials |
| DESCRIPTION | No | - | Description | Construction products |
| SORT_ORDER | No | - | Display order (1, 2, 3...) | 1 |

---

### 3. Product Types (`03_product_types.csv`)

Specific product types within categories.

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 255 | Product type name | Insulation |
| CATEGORY_NAME | **Yes** | 255 | Must match a Product Category | Building Materials |
| PARENT_NAME | No | 255 | Parent type (for sub-types) | |
| SORT_ORDER | No | - | Display order | 1 |

---

### 3a. Product Subcategories (`03a_product_subcategories.csv`) - OPTIONAL

Third level of product hierarchy (Category > Type > Subcategory).

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 255 | Subcategory name | Foam Board |
| PRODUCT_TYPE_NAME | **Yes** | 255 | Must match a Product Type | Insulation |
| DESCRIPTION | No | - | Description | Rigid foam insulation boards |
| SORT_ORDER | No | - | Display order | 1 |

---

### 4. Companies (`04_companies.csv`)

Organizations that hold certificates.

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 255 | Company name | Sofidel America Corp. |
| CONTACT_INFO | No | 255 | Contact person/dept | John Smith, Sustainability |
| EMAIL | No | 255 | Contact email | sustainability@company.com |
| WEBSITE | No | 500 | Company website URL | https://www.company.com |
| LOCATION | No | 175 | City, State/Region | Horsham, PA |
| COUNTRY | No | 100 | Country name | United States |

---

### 5. Products (`05_products.csv`)

Products that are certified.

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 255 | Product name | Plastic Film Packaging |
| DESCRIPTION | No | - | Product description | Recyclable packaging film |
| COMPANY_NAME | No | 255 | Owning company (must match Companies) | Sofidel America Corp. |
| CATEGORY_NAME | **Yes** | 255 | Must match a Product Category | Materials / Plastics |
| PRODUCT_TYPE_NAME | No | 255 | Must match a Product Type | Packaging |
| MODEL | No | 100 | Model number | PKG-2000 |
| SKU | No | 50 | Stock keeping unit | SKU-12345 |
| UPC | No | 50 | Universal Product Code | 012345678901 |
| ASIN | No | 50 | Amazon Standard ID | B08XYZ1234 |

---

### 6. Tags (`06_tags.csv`) - Optional

Tags for filtering and categorization.

| Column | Required | Max Length | Description | Example |
|--------|----------|------------|-------------|---------|
| NAME | **Yes** | 50 | Tag name | APR |
| TAG_TYPE | **Yes** | 20 | `external` or `internal` | external |
| DESCRIPTION | No | - | Tag description | Association of Plastic Recyclers |

**Tag Types:**
- `external` - Visible to all users (APR, LEED, FSC, etc.)
- `internal` - Only visible to analysts/admins

---

### 7. Certificates (`07_certificates.csv`)

The main certificate data.

| Column | Required | Max Length | Valid Values | Example |
|--------|----------|------------|--------------|---------|
| CERTIFICATE_NUMBER | **Yes** | 7 | Format: YY-NNNN | 23-0358 |
| COMPANY_NAME | **Yes** | 255 | Must match Companies | Sofidel America Corp. |
| PRODUCT_NAME | **Yes** | 255 | Must match Products | Plastic Film Packaging |
| CERTIFICATION_TYPE | **Yes** | 255 | Must match Certification Types | Recycled Content |
| EFFECTIVE_DATE | **Yes** | - | Format: M/D/YYYY | 6/1/2023 |
| EXPIRATION_DATE | **Yes** | - | Format: M/D/YYYY or blank for perpetual | 5/31/2026 |
| LOCATION | No | 175 | Manufacturing/facility location | Horsham, PA |
| COUNTRY | No | 100 | Country name | United States |
| STATUS | No | 50 | See status values below | active |
| PARTNER | No | 100 | Amazon, Novi, or None | None |
| CERTIFICATION_RESULTS | No | 1000 | Test results/scores | 95% recycled content |
| TAGS | No | - | Comma-separated tag names | APR, LEED |
| ASIN | No | - | Amazon Standard ID | B08XYZ1234 |
| UPC | No | - | Universal Product Code | 012345678901 |
| GTIN | No | - | Global Trade Item Number | 00012345678905 |
| EAN | No | - | European Article Number | 5012345678900 |

**Valid Status Values:**
| Status | Description |
|--------|-------------|
| `active` | **Use this for live certificates** |
| `draft` | Work in progress |
| `pending_review` | Awaiting review |
| `expiring_soon` | Near expiration |
| `expired` | Past expiration date |
| `renewal_in_progress` | Being renewed |
| `suspended` | Temporarily inactive |
| `revoked` | Permanently revoked |

**Note:** For certificates to appear on the public tile view, they must have status `active`, `expiring_soon`, or `renewal_in_progress`.

---

### 8. Certificate Product IDs (`08_certificate_product_ids.csv`) - FOR AMAZON

Multiple product identifiers per certificate for Amazon/partner marketplace integration.

| Column | Required | Max Length | Valid Values | Example |
|--------|----------|------------|--------------|---------|
| CERTIFICATE_NUMBER | **Yes** | 7 | Must match a Certificate | 23-0358 |
| PRODUCT_ID_TYPE | **Yes** | 10 | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN | ASIN |
| PRODUCT_ID_VALUE | **Yes** | 50 | The actual ID value | B08XYZ1234 |
| PARTNER | No | 50 | Amazon, Novi, None, Other | Amazon |
| STATUS | No | 20 | Active, Inactive, Pending, Expired | Active |
| PRODUCT_NAME | No | 200 | Product name for this specific ID | Plastic Film Roll - 100ft |
| PRODUCT_SKU | No | 100 | SKU for this product variant | SKU-PF100 |
| DESCRIPTION | No | 500 | Notes about this product ID | Main Amazon listing |

**Product ID Types:**
- `ASIN` - Amazon Standard Identification Number (10 chars, starts with B0)
- `UPC` - Universal Product Code (12 digits)
- `GTIN` - Global Trade Item Number (14 digits)
- `EAN` - European Article Number (13 digits)
- `ISBN` - International Standard Book Number
- `SKU` - Stock Keeping Unit
- `MPN` - Manufacturer Part Number

**Use Case:** A single certificate can have multiple product IDs. For example, if a company has the same certified product listed with different sizes on Amazon, each ASIN would be a separate row.

---

## Data Validation Rules

### Certificate Number Format
- Must be 7 characters: `YY-NNNN`
- YY = 2-digit year
- NNNN = 4-digit sequence number
- Example: `23-0358`, `24-1597`, `25-0123`

### Date Formats
- Accepted: `M/D/YYYY`, `MM/DD/YYYY`, `YYYY-MM-DD`
- For perpetual certificates (no expiration): leave EXPIRATION_DATE blank or use `2099-12-31`

### Country Values
Please use consistent country names:
- `United States` (not USA, US, U.S.)
- `Canada`
- `United Kingdom` (not UK, U.K.)
- `Mexico`
- etc.

### Required vs Optional Fields
- Fields marked **Yes** in Required column must have values
- If PRODUCT_NAME is blank, system will use "Not Specified"
- If EXPIRATION_DATE is blank, system will use 2099-12-31

---

## Sample Data

See the sample CSV files in this folder for examples of properly formatted data.

---

## Questions?

Contact the development team for assistance with data formatting or migration issues.

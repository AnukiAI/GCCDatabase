# Import Test Templates

This folder contains CSV files for testing the import functionality across all admin pages.

## Test Files Summary

### Company Import (`/Company/Index`)
| File | Purpose | Expected Results |
|------|---------|------------------|
| `company_positive.csv` | Valid data only | All 10 rows should import successfully |
| `company_negative.csv` | Invalid data | All rows should show errors/warnings |
| `company_boundary.csv` | Edge cases | Mix of valid (boundary) and invalid (exceeded) |
| `company_special.csv` | Special characters | Tests CSV parsing with quotes, commas, etc. |
| `company_mixed.csv` | Mixed valid/invalid | Some pass, some fail - good for preview testing |

### User Import (`/Admin/UserManagement`)
| File | Purpose | Expected Results |
|------|---------|------------------|
| `user_positive.csv` | Valid users | All 10 rows should import successfully |
| `user_negative.csv` | Invalid users | All rows should show errors |
| `user_boundary.csv` | Edge cases | Tests name/email length limits |
| `user_mixed.csv` | Mixed data | Some valid, some invalid |

### Tag Import (`/Admin/Tags`)
| File | Purpose | Expected Results |
|------|---------|------------------|
| `tag_positive.csv` | Valid tags | All 12 rows should import (internal/external) |
| `tag_negative.csv` | Invalid tags | Tests missing fields, invalid types |
| `tag_boundary.csv` | Edge cases | Tests 50 char name limit |
| `tag_mixed.csv` | Mixed data | Some valid, some invalid |

### Product Import (`/Admin/ProductManagement`)
| File | Purpose | Expected Results |
|------|---------|------------------|
| `product_positive.csv` | Valid products | All 10 rows should import |
| `product_negative.csv` | Invalid products | Tests FK refs, missing fields |
| `product_boundary.csv` | Edge cases | Tests name/description limits |
| `product_mixed.csv` | Mixed data | Some valid, some invalid |

---

## Validation Rules Reference

### Company Fields
| Field | Required | Max Length | Format | Validation |
|-------|----------|------------|--------|------------|
| Name | **YES** | 255 | Text | Must not be empty, unique |
| Email | No | 255 | Email | `^[^@\s]+@[^@\s]+\.[^@\s]+$` |
| URL | No | 500 | URL | Must start with http:// or https:// |

### User Fields
| Field | Required | Max Length | Format | Validation |
|-------|----------|------------|--------|------------|
| Name | **YES** | 100 | Text | Display name |
| Email | **YES** | 255 | Email | Must be unique, valid email format |
| Role | **YES** | - | Enum | Admin, Analyst, External (case-sensitive) |
| Status | No | 20 | Enum | active, inactive (defaults to active) |

### Tag Fields
| Field | Required | Max Length | Format | Validation |
|-------|----------|------------|--------|------------|
| Name | **YES** | 50 | Text | Must be unique (case-insensitive) |
| Type | **YES** | 20 | Enum | internal, external (lowercase only) |
| Description | No | - | Text | Optional description |

### Product Fields
| Field | Required | Max Length | Format | Validation |
|-------|----------|------------|--------|------------|
| Name | **YES** | 100 | Text | Product name |
| Description | No | 500 | Text | Product description |
| CompanyName | No | - | Text | Must match existing company name |
| ProductTypeName | No | - | Text | Must match existing product type |

---

## Test Categories Explained

### 1. Positive Tests (`*_positive.csv`)
- **Purpose**: Verify that valid data imports correctly
- **Expected**: All rows should import without errors
- **Tests**: Standard valid data, optional fields empty, various valid formats

### 2. Negative Tests (`*_negative.csv`)
- **Purpose**: Verify that invalid data is rejected
- **Expected**: All rows should show validation errors
- **Tests**:
  - Missing required fields
  - Invalid email formats
  - Invalid URL formats
  - Invalid enum values (roles, types)
  - SQL injection attempts
  - XSS attack attempts
  - Invalid foreign key references

### 3. Boundary Tests (`*_boundary.csv`)
- **Purpose**: Test field length limits and edge cases
- **Expected**: Mix of success (at boundary) and failure (exceeding)
- **Tests**:
  - Minimum length (1-2 chars)
  - Exact maximum length
  - Exceeding maximum length
  - Unicode characters
  - Numbers only
  - Special characters in names

### 4. Special Character Tests (`*_special.csv`)
- **Purpose**: Test CSV parsing edge cases
- **Expected**: Tests proper handling of:
  - Commas in quoted fields
  - Double quotes (escaped as `""`)
  - Newlines in fields
  - Ampersands, HTML entities
  - Various special characters

### 5. Mixed Tests (`*_mixed.csv`)
- **Purpose**: Realistic test with both valid and invalid data
- **Expected**: Good rows import, bad rows show errors
- **Use Case**: Best for testing the import preview modal UI

---

## How to Test

### Step 1: Navigate to Import Page
- Company: http://localhost:5101/Company/Index
- Users: http://localhost:5101/Admin/UserManagement
- Tags: http://localhost:5101/Admin/Tags
- Products: http://localhost:5101/Admin/ProductManagement

### Step 2: Click Import Button
Look for the "Import" button in the action bar.

### Step 3: Select Test File
Choose the appropriate CSV file from this folder.

### Step 4: Review Import Preview
The modal should show:
- **New** (green): Records that will be created
- **Modified** (blue): Records that differ from existing
- **Unchanged** (gray): Records that match existing
- **Error** (red): Records with validation errors

### Step 5: Verify Actions
- Check that error messages are clear and helpful
- Verify row counts match expected results
- Test Apply Import, Keep Current, Skip actions

### Step 6: Apply Import
Click "Apply Import" and verify:
- Success notifications appear
- Data is correctly saved
- Error handling works properly

---

## Security Test Cases

The negative test files include security test cases:

| Attack Type | Test Data | Expected Behavior |
|-------------|-----------|-------------------|
| SQL Injection | `'; DROP TABLE;--` | Should be escaped/rejected |
| XSS | `<script>alert(1)</script>` | Should be HTML encoded |
| Path Traversal | `../../../etc/passwd` | Should be rejected |
| Email Injection | Headers in email | Should be rejected |

---

## Known Validation Edge Cases

1. **Email validation** is strict - requires format `user@domain.tld`
2. **URL validation** requires protocol (http:// or https://)
3. **Tag types** are case-sensitive (must be lowercase)
4. **Role names** are case-sensitive (Admin, Analyst, External)
5. **Company/Product references** must match existing records exactly

---

## File Locations

```
TestData/ImportTemplates/
├── README.md                 (this file)
├── company_positive.csv
├── company_negative.csv
├── company_boundary.csv
├── company_special.csv
├── company_mixed.csv
├── user_positive.csv
├── user_negative.csv
├── user_boundary.csv
├── user_mixed.csv
├── tag_positive.csv
├── tag_negative.csv
├── tag_boundary.csv
├── tag_mixed.csv
├── product_positive.csv
├── product_negative.csv
├── product_boundary.csv
└── product_mixed.csv
```

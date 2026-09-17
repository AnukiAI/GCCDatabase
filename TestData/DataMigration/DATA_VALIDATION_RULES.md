# GCC WebApp Data Validation Rules Reference

## Purpose
Complete reference of all validation rules enforced during data import. Use this to pre-validate source data.

---

## 1. Company Validation Rules

### Field Specifications

| Field | Type | Required | Max Length | Format | Default |
|-------|------|----------|------------|--------|---------|
| Name | string | YES | 255 | Any text | - |
| Email | string | No | 255 | Email format | NULL |
| Url | string | No | 500 | URL format | NULL |

### Validation Details

#### Name (REQUIRED)
```
Rule: Cannot be null, empty, or whitespace
Regex: .{1,255}
Examples:
  VALID: "Acme Corporation", "A", "Company with 255 chars..."
  INVALID: "", "   ", null
Error: "Company name is required"
```

#### Email (Optional)
```
Rule: If provided, must be valid email format
Regex: ^[^@\s]+@[^@\s]+\.[^@\s]+$
Examples:
  VALID: "contact@company.com", "user.name+tag@domain.co.uk", null, ""
  INVALID: "invalid", "@domain.com", "user@", "user@.com"
Error: "Invalid email address"
```

#### Url (Optional)
```
Rule: If provided, must be valid URL with protocol
Regex: ^https?://.*
Examples:
  VALID: "https://www.company.com", "http://company.com", null, ""
  INVALID: "www.company.com", "company.com", "ftp://company.com"
Error: "Invalid URL format (must start with http:// or https://)"
```

### Business Rules
- Duplicate company names: Allowed (but not recommended)
- Case sensitivity: Case-insensitive matching for lookups
- Whitespace: Trimmed on import

---

## 2. User Validation Rules

### Field Specifications

| Field | Type | Required | Max Length | Format | Default |
|-------|------|----------|------------|--------|---------|
| Name | string | YES | 100 | Any text | - |
| Email | string | YES | 255 | Email format | - |
| Role | enum | YES | 20 | Admin/Analyst/External | - |
| Status | enum | No | 20 | Active/Inactive | Active |
| Company | string | No | 255 | Must exist | NULL |
| Password | string | No | 100 | Min 6 chars | Auto-generated |
| RequirePasswordReset | bool | No | - | true/false | true |

### Validation Details

#### Name (REQUIRED)
```
Rule: Cannot be null, empty, or whitespace
Regex: .{1,100}
Examples:
  VALID: "John Doe", "J", "Name with exactly 100 characters..."
  INVALID: "", "   ", null, "Name exceeding 100 characters..."
Error: "User name is required"
```

#### Email (REQUIRED)
```
Rule: Must be valid email, must be unique in system
Regex: ^[^@\s]+@[^@\s]+\.[^@\s]+$
Unique: Yes - duplicate emails rejected
Examples:
  VALID: "user@domain.com" (if not already in system)
  INVALID: "invalid", "user@existing.com" (if already exists)
Error: "Invalid email address" or "Email already exists"
```

#### Role (REQUIRED)
```
Rule: Must be one of allowed values (case-insensitive)
Allowed: "Admin", "Analyst", "External"
Examples:
  VALID: "Admin", "admin", "ADMIN", "Analyst", "External"
  INVALID: "SuperUser", "Administrator", "Guest", "User"
Error: "Invalid role. Must be Admin, Analyst, or External"
```

#### Status (Optional)
```
Rule: If provided, must be one of allowed values
Allowed: "Active", "Inactive"
Default: "Active"
Examples:
  VALID: "Active", "active", "Inactive", "", null
  INVALID: "Enabled", "Disabled", "1", "0"
Error: "Invalid status. Must be Active or Inactive"
```

#### Company (Optional)
```
Rule: If provided, must match existing company name
Lookup: Case-insensitive match against Company.Name
Examples:
  VALID: "Acme Corp" (if exists), "", null
  INVALID: "NonExistent Company XYZ"
Error: "Company 'X' not found in system"
```

### Business Rules
- Email uniqueness: Enforced (critical)
- Password: Auto-generated if not provided, forced reset on first login
- Role hierarchy: Admin > Analyst > External

---

## 3. Tag Validation Rules

### Field Specifications

| Field | Type | Required | Max Length | Format | Default |
|-------|------|----------|------------|--------|---------|
| Name | string | YES | 50 | Any text | - |
| TagType | enum | YES | 20 | internal/external | - |
| Description | string | No | 500 | Any text | NULL |

### Validation Details

#### Name (REQUIRED)
```
Rule: Cannot be null, empty, or whitespace
Regex: .{1,50}
Unique: Yes - duplicate tag names rejected
Examples:
  VALID: "Priority", "P", "Tag with exactly 50 characters..."
  INVALID: "", "   ", null, "Tag exceeding 50 characters..."
Error: "Tag name is required"
```

#### TagType (REQUIRED)
```
Rule: Must be one of allowed values (case-insensitive)
Allowed: "internal", "external"
Examples:
  VALID: "internal", "Internal", "INTERNAL", "external", "External"
  INVALID: "public", "private", "both", "Internal Tag"
Error: "Invalid tag type. Must be 'internal' or 'external'"
```

#### Description (Optional)
```
Rule: If provided, max 500 characters
Examples:
  VALID: "Description text", "", null
  INVALID: "Text exceeding 500 characters..."
Error: "Description exceeds 500 characters"
```

### Business Rules
- Tag name uniqueness: Enforced across all tag types
- Internal tags: Only visible to Admin/Analyst
- External tags: Visible to all authenticated users

---

## 4. Product Validation Rules

### Field Specifications

| Field | Type | Required | Max Length | Format | Default |
|-------|------|----------|------------|--------|---------|
| Name | string | YES | 100 | Any text | - |
| Description | string | No | 500 | Any text | NULL |
| Company | string | No | 255 | Must exist | NULL |
| ProductType | string | No | 100 | Must exist | NULL |
| Status | enum | No | 20 | Active/Inactive/Draft | Active |

### Validation Details

#### Name (REQUIRED)
```
Rule: Cannot be null, empty, or whitespace
Regex: .{1,100}
Examples:
  VALID: "Widget Pro 2000", "W", "Product name 100 chars..."
  INVALID: "", "   ", null
Error: "Product name is required"
```

#### Description (Optional)
```
Rule: If provided, max 500 characters
Examples:
  VALID: "Product description", "", null
  INVALID: "Text exceeding 500 characters..."
Error: "Description exceeds 500 characters"
```

#### Company (Optional - Recommended)
```
Rule: If provided, must match existing company name
Lookup: Case-insensitive match against Company.Name
Examples:
  VALID: "Acme Corp" (if exists), "", null
  INVALID: "NonExistent Company XYZ"
Error: "Company 'X' not found in system"
```

#### ProductType (Optional)
```
Rule: If provided, must match existing product type name
Lookup: Case-insensitive match against ProductType.Name
Examples:
  VALID: "Electronics" (if exists), "", null
  INVALID: "NonExistent Type"
Error: "Product type 'X' not found in system"
```

#### Status (Optional)
```
Rule: If provided, must be one of allowed values
Allowed: "Active", "Inactive", "Draft"
Default: "Active"
Examples:
  VALID: "Active", "Inactive", "Draft", "", null
  INVALID: "Enabled", "Published", "Pending"
Error: "Invalid status. Must be Active, Inactive, or Draft"
```

### Business Rules
- Product uniqueness: Name + Company combination should be unique
- Orphaned products: Allowed but not recommended (no company association)

---

## 5. Certificate Validation Rules (Reference)

### Field Specifications

| Field | Type | Required | Max Length | Format | Default |
|-------|------|----------|------------|--------|---------|
| CertificateNumber | string | YES | 50 | Unique | Auto-gen |
| Company | FK | YES | - | Must exist | - |
| Product | FK | No | - | Must exist | NULL |
| Partner | enum | No | 50 | Amazon/Wayfair/None | None |
| IssueDate | date | No | - | YYYY-MM-DD | Today |
| ExpiryDate | date | No | - | YYYY-MM-DD | NULL |
| Status | enum | YES | 20 | Workflow states | Draft |
| CertificationResults | string | No | 500 | Any text | NULL |

### Validation Details

#### CertificateNumber (REQUIRED)
```
Rule: Unique, auto-generated if not provided
Format: YY-NNNN (e.g., "24-0001")
Unique: Yes - duplicates rejected
```

#### Partner (Optional)
```
Allowed: "Amazon", "Wayfair", "None"
Default: "None"
Case-sensitive: No
```

#### Date Fields
```
Format: ISO 8601 (YYYY-MM-DD)
Rule: ExpiryDate must be >= IssueDate if both provided
Examples:
  VALID: "2025-12-31", "2025-01-01"
  INVALID: "12/31/2025", "31-12-2025", "December 31, 2025"
```

#### Status (Workflow States)
```
Allowed Values (14 states):
  "Draft", "PendingReview", "UnderReview", "Approved",
  "Active", "Suspended", "Revoked", "Expired",
  "PendingRenewal", "Renewed", "Cancelled", "Archived",
  "OnHold", "Rejected"

Valid Transitions: See CertificateWorkflowService for state machine
```

---

## 6. Cross-Entity Validation Rules

### Import Order Dependencies

```
REQUIRED ORDER:
1. Companies (no dependencies)
2. Product Types (no dependencies)
3. Tags (no dependencies)
4. Products (depends on: Companies, ProductTypes)
5. Users (depends on: Companies - optional)
6. Certificates (depends on: Companies, Products)
7. CertificateProductIds (depends on: Certificates)
```

### Referential Integrity Matrix

| Source Entity | Target Entity | FK Field | On Missing |
|---------------|---------------|----------|------------|
| Product | Company | CompanyId | Import fails for row |
| Product | ProductType | ProductTypeId | Import fails for row |
| User | Company | CompanyId | Set to NULL |
| Certificate | Company | CompanyId | Import fails for row |
| Certificate | Product | ProductId | Set to NULL |

---

## 7. Common Validation Errors & Fixes

### Error: "X not found in system"

**Cause**: Referenced entity doesn't exist
**Fix**:
1. Import reference data first (Companies, ProductTypes)
2. Check spelling/case of reference value
3. Create missing reference entity manually

### Error: "X already exists"

**Cause**: Duplicate unique constraint violation
**Fix**:
1. Find existing record in system
2. Decide: Update existing or skip import
3. Remove duplicate from source file

### Error: "Invalid format for X"

**Cause**: Value doesn't match expected pattern
**Fix**:
1. Check field format requirements above
2. Transform data to match pattern
3. Use header normalization (automatic)

### Error: "X exceeds maximum length"

**Cause**: Value too long for database field
**Fix**:
1. Truncate value to max length
2. Summarize content
3. Store overflow in notes field

### Error: "Required field X is empty"

**Cause**: Mandatory field has no value
**Fix**:
1. Provide value in source data
2. Define default value strategy
3. Reject record if no reasonable default

---

## 8. Validation Regex Patterns

```javascript
// Email
const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

// URL (with protocol)
const urlPattern = /^https?:\/\/.+/i;

// Date (ISO 8601)
const datePattern = /^\d{4}-\d{2}-\d{2}$/;

// Certificate Number
const certNumberPattern = /^\d{2}-\d{4}$/;

// Phone (US format, flexible)
const phonePattern = /^[\d\s\-\(\)\+\.]+$/;

// Alphanumeric with spaces
const alphaNumPattern = /^[a-zA-Z0-9\s]+$/;

// SQL Injection detection
const sqlInjectionPattern = /('|--|;|\bDROP\b|\bDELETE\b|\bINSERT\b|\bUPDATE\b|\bSELECT\b)/i;

// XSS detection
const xssPattern = /<script|javascript:|on\w+=/i;
```

---

## 9. Validation Implementation Location

| Entity | Frontend Validation | Backend Validation |
|--------|--------------------|--------------------|
| Company | `companyManagement.js:540-566` | `CompanyController.cs:63-70` |
| User | `userManagement.js:723-766` | `AdminController.cs` |
| Tag | `tags.js:1105-1127` | `AdminController.cs` |
| Product | `productManagement.js:1140-1171` | `AdminController.cs` |

---

*Last Updated: December 10, 2025*

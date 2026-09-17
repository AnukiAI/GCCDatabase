# UAT Import Testing Guide

## GCC Web Application - Bulk Import Functionality

**Document Version**: 1.0
**Last Updated**: December 2025
**Target Audience**: UAT Testers, QA Team, Business Analysts

---

## Table of Contents

1. [Overview](#overview)
2. [Test Environment Setup](#test-environment-setup)
3. [Test Data Location](#test-data-location)
4. [Test Execution Guide](#test-execution-guide)
5. [Test Scenarios](#test-scenarios)
6. [Expected Results Matrix](#expected-results-matrix)
7. [Defect Reporting](#defect-reporting)
8. [Appendix](#appendix)

---

## Overview

This guide provides step-by-step instructions for UAT testing of the bulk import functionality across the GCC Web Application admin pages. The import feature allows administrators to upload CSV files to create or update records in bulk.

### Features Under Test

| Feature | Admin Page | URL |
|---------|-----------|-----|
| Company Import | Company Management | `/Company/Index` |
| User Import | User Management | `/Admin/UserManagement` |
| Tag Import | Tag Management | `/Admin/Tags` |
| Product Import | Product Management | `/Admin/ProductManagement` |

### Pre-Requisites

- Valid Admin or Analyst login credentials
- Access to test environment (localhost:5101 or production URL)
- Test data CSV files (provided in this folder)

---

## Test Environment Setup

### Step 1: Access the Application

**Development Environment:**
```
URL: http://localhost:5101
```

**Production Environment:**
```
URL: https://gccwebapp.com
```

### Step 2: Login with Test Credentials

| Role | Username | Password | Access Level |
|------|----------|----------|--------------|
| Admin | admin | TestPass123! | Full access to all imports |
| Analyst | analyst | TestPass123! | Limited import access |

### Step 3: Verify Test Data Files

Ensure you have access to the test data folder:
```
Location: GCCWebApp\TestData\ImportTemplates\
```

---

## Test Data Location

### File Path
```
C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp\TestData\ImportTemplates\
```

### Available Test Files

| Entity | Positive | Negative | Boundary | Mixed | Special |
|--------|----------|----------|----------|-------|---------|
| Company | company_positive.csv | company_negative.csv | company_boundary.csv | company_mixed.csv | company_special.csv |
| User | user_positive.csv | user_negative.csv | user_boundary.csv | user_mixed.csv | - |
| Tag | tag_positive.csv | tag_negative.csv | tag_boundary.csv | tag_mixed.csv | - |
| Product | product_positive.csv | product_negative.csv | product_boundary.csv | product_mixed.csv | - |

---

## Test Execution Guide

### How to Perform an Import Test

#### Step 1: Navigate to Admin Page

1. Login to the application
2. Go to the appropriate admin page:
   - **Companies**: Click "Companies" in navigation OR go to `/Company/Index`
   - **Users**: Go to Admin > User Management OR `/Admin/UserManagement`
   - **Tags**: Go to Admin > Tags OR `/Admin/Tags`
   - **Products**: Go to Admin > Product Management OR `/Admin/ProductManagement`

#### Step 2: Initiate Import

1. Locate the **Import** button in the action toolbar (top-right area)
2. Click the **Import** button
3. A file selection dialog will appear

#### Step 3: Select Test File

1. Navigate to the test data folder
2. Select the appropriate CSV file for your test scenario
3. Click **Open**

#### Step 4: Review Import Preview

The Import Preview Modal will display with the following information:

| Status | Color | Meaning |
|--------|-------|---------|
| **New** | Green | Record will be created (does not exist) |
| **Modified** | Blue | Record exists and will be updated |
| **Unchanged** | Gray | Record exists and matches exactly |
| **Error** | Red | Validation failed - will not import |

**Verify:**
- [ ] Row counts are correct
- [ ] Status colors are appropriate
- [ ] Error messages are clear and actionable
- [ ] Preview data matches CSV content

#### Step 5: Execute Import

For each row, you can choose:
- **Apply Import**: Import the row
- **Keep Current**: Skip and keep existing data
- **Skip**: Do not import this row

After reviewing:
1. Click **Apply Import** button to import valid rows
2. Or click **Cancel** to abort the entire import

#### Step 6: Verify Results

After import completes:
- [ ] Success notification appears (toast message)
- [ ] Data grid refreshes with new/updated records
- [ ] Error rows show appropriate error messages
- [ ] No duplicate records created

---

## Test Scenarios

### Scenario 1: Positive Testing (Happy Path)

**Objective**: Verify all valid data imports successfully

**Test Files**: `*_positive.csv`

| Test Case | File | Steps | Expected Result |
|-----------|------|-------|-----------------|
| TC-IMP-001 | company_positive.csv | Import all 10 companies | All 10 rows show "New" status, import succeeds |
| TC-IMP-002 | user_positive.csv | Import all 10 users | All 10 rows show "New" status, import succeeds |
| TC-IMP-003 | tag_positive.csv | Import all 12 tags | All 12 rows show "New" status, import succeeds |
| TC-IMP-004 | product_positive.csv | Import all 10 products | All 10 rows show "New" status, import succeeds |

**Sample Data (company_positive.csv):**
```csv
Name,Email,URL
Acme Corporation,info@acmecorp.com,https://www.acmecorp.com
Green Products Inc,contact@greenproducts.org,https://greenproducts.org
Sustainable Materials Co,,
```

---

### Scenario 2: Negative Testing (Error Handling)

**Objective**: Verify invalid data is rejected with clear error messages

**Test Files**: `*_negative.csv`

| Test Case | File | Invalid Data Type | Expected Error |
|-----------|------|-------------------|----------------|
| TC-IMP-005 | company_negative.csv | Missing required name | "Name is required" |
| TC-IMP-006 | company_negative.csv | Invalid email format | "Invalid email format" |
| TC-IMP-007 | company_negative.csv | Invalid URL format | "Invalid URL format" |
| TC-IMP-008 | user_negative.csv | Invalid role name | "Invalid role" |
| TC-IMP-009 | user_negative.csv | Duplicate email | "Email already exists" |
| TC-IMP-010 | tag_negative.csv | Invalid tag type | "Type must be internal or external" |

**What to Verify:**
- [ ] All invalid rows show "Error" status (red)
- [ ] Error messages are specific and helpful
- [ ] Valid rows in same file can still be imported
- [ ] No partial data saved for error rows

---

### Scenario 3: Boundary Testing (Edge Cases)

**Objective**: Verify field length limits and special cases

**Test Files**: `*_boundary.csv`

| Test Case | Field | Test | Expected Result |
|-----------|-------|------|-----------------|
| TC-IMP-011 | Company Name | 255 chars (max) | Should import successfully |
| TC-IMP-012 | Company Name | 256 chars (over max) | Should show error |
| TC-IMP-013 | User Name | 1 char (min) | Should import successfully |
| TC-IMP-014 | User Email | 255 chars | Should import successfully |
| TC-IMP-015 | Tag Name | 50 chars (max) | Should import successfully |
| TC-IMP-016 | Tag Name | 51 chars (over max) | Should show error |

---

### Scenario 4: Mixed Data Testing (Realistic)

**Objective**: Test real-world scenario with mix of valid and invalid data

**Test Files**: `*_mixed.csv`

**Sample Data (company_mixed.csv):**
```csv
Name,Email,URL
Valid Company One,valid1@company.com,https://valid1.com      <- Should import
,missing.name@test.com,https://noname.com                    <- Error: missing name
Valid Company Two,invalid-email,https://valid2.com           <- Error: invalid email
Valid Company Three,valid3@company.com,not-a-url             <- Error: invalid URL
"Company With, Comma",comma@company.com,https://comma.com    <- Should import
```

| Test Case | File | Expected Valid | Expected Errors |
|-----------|------|----------------|-----------------|
| TC-IMP-017 | company_mixed.csv | 5 rows | 4 rows |
| TC-IMP-018 | user_mixed.csv | 5 rows | 5 rows |
| TC-IMP-019 | tag_mixed.csv | 6 rows | 6 rows |
| TC-IMP-020 | product_mixed.csv | 5 rows | 5 rows |

---

### Scenario 5: Security Testing

**Objective**: Verify protection against malicious input

**Test Files**: Included in `*_negative.csv` files

| Test Case | Attack Type | Test Input | Expected Behavior |
|-----------|-------------|------------|-------------------|
| TC-IMP-021 | SQL Injection | `'; DROP TABLE;--` | Input rejected or escaped |
| TC-IMP-022 | XSS Attack | `<script>alert(1)</script>` | HTML encoded, not executed |
| TC-IMP-023 | Path Traversal | `../../../etc/passwd` | Input rejected |
| TC-IMP-024 | Email Header Injection | `test@test.com\nBCC:hacker@evil.com` | Input rejected |

**Verification Steps:**
1. Import file containing attack strings
2. Verify error messages appear (do not import)
3. If any import succeeds, verify data is safely escaped
4. Check browser console for any JavaScript execution (XSS)
5. Verify database integrity after test

---

### Scenario 6: Duplicate Handling

**Objective**: Verify behavior when importing existing records

| Test Case | Scenario | Expected Result |
|-----------|----------|-----------------|
| TC-IMP-025 | Import same company twice | Second import shows "Unchanged" |
| TC-IMP-026 | Import existing with different data | Shows "Modified" with diff |
| TC-IMP-027 | Import with same email, different name | Shows "Modified" |

---

## Expected Results Matrix

### Company Import Results

| File | Total Rows | New | Modified | Unchanged | Errors |
|------|------------|-----|----------|-----------|--------|
| company_positive.csv | 10 | 10 | 0 | 0 | 0 |
| company_negative.csv | 10 | 0 | 0 | 0 | 10 |
| company_boundary.csv | 10 | 5 | 0 | 0 | 5 |
| company_mixed.csv | 9 | 5 | 0 | 0 | 4 |
| company_special.csv | 10 | 10 | 0 | 0 | 0 |

### User Import Results

| File | Total Rows | New | Modified | Unchanged | Errors |
|------|------------|-----|----------|-----------|--------|
| user_positive.csv | 10 | 10 | 0 | 0 | 0 |
| user_negative.csv | 10 | 0 | 0 | 0 | 10 |
| user_boundary.csv | 10 | 5 | 0 | 0 | 5 |
| user_mixed.csv | 10 | 5 | 0 | 0 | 5 |

### Tag Import Results

| File | Total Rows | New | Modified | Unchanged | Errors |
|------|------------|-----|----------|-----------|--------|
| tag_positive.csv | 12 | 12 | 0 | 0 | 0 |
| tag_negative.csv | 10 | 0 | 0 | 0 | 10 |
| tag_boundary.csv | 10 | 5 | 0 | 0 | 5 |
| tag_mixed.csv | 12 | 6 | 0 | 0 | 6 |

### Product Import Results

| File | Total Rows | New | Modified | Unchanged | Errors |
|------|------------|-----|----------|-----------|--------|
| product_positive.csv | 10 | 10 | 0 | 0 | 0 |
| product_negative.csv | 10 | 0 | 0 | 0 | 10 |
| product_boundary.csv | 10 | 5 | 0 | 0 | 5 |
| product_mixed.csv | 10 | 5 | 0 | 0 | 5 |

---

## Defect Reporting

### When to Log a Defect

Log a defect when:
- Import succeeds but data is incorrect
- Import fails for valid data
- Invalid data imports without error
- Error messages are unclear or missing
- UI does not display correct status colors
- Security test cases are not properly handled
- Application crashes or shows unexpected behavior

### Defect Template

```
Title: [IMP] Brief description of issue

Environment: [Development/Production]
Test Case: TC-IMP-XXX
Test File: [filename.csv]

Steps to Reproduce:
1. Navigate to [page]
2. Click Import
3. Select [test file]
4. [Additional steps]

Expected Result:
[What should happen]

Actual Result:
[What actually happened]

Evidence:
[Screenshots, error messages, console logs]

Severity: [Critical/High/Medium/Low]
Priority: [P1/P2/P3/P4]
```

---

## Appendix

### A. Validation Rules Quick Reference

| Entity | Field | Required | Max Length | Format |
|--------|-------|----------|------------|--------|
| Company | Name | Yes | 255 | Text |
| Company | Email | No | 255 | email@domain.tld |
| Company | URL | No | 500 | http(s)://... |
| User | Name | Yes | 100 | Text |
| User | Email | Yes | 255 | email@domain.tld (unique) |
| User | Role | Yes | - | Admin, Analyst, External |
| User | Status | No | 20 | active, inactive |
| Tag | Name | Yes | 50 | Text (unique) |
| Tag | Type | Yes | 20 | internal, external |
| Product | Name | Yes | 100 | Text |
| Product | Description | No | 500 | Text |

### B. Role Permissions for Import

| Role | Company | User | Tag | Product |
|------|---------|------|-----|---------|
| Admin | Full | Full | Full | Full |
| Analyst | Full | View Only | Full | Full |
| External | No Access | No Access | No Access | No Access |

### C. Test Execution Checklist

Before Testing:
- [ ] Application is running and accessible
- [ ] Test credentials are working
- [ ] Test data files are available
- [ ] Database is in known state (consider backup)

During Testing:
- [ ] Document actual results for each test case
- [ ] Capture screenshots of errors
- [ ] Note any unexpected behavior
- [ ] Verify data in database if needed

After Testing:
- [ ] Log all defects found
- [ ] Update test results matrix
- [ ] Clean up test data if required
- [ ] Report test completion status

### D. Contact Information

For questions about this guide or test data:
- Technical Lead: [Contact Info]
- QA Lead: [Contact Info]
- Project Manager: [Contact Info]

---

**End of Document**

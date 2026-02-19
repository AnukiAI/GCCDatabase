# GCC Database Solution - Comprehensive UAT Testing Guide

**Version:** 3.0
**Date:** February 19, 2026
**Application:** GCC Web Application (ASP.NET Core 8.0)
**Environment:** UAT / System Test

---

## Table of Contents

**Part A: Feature-Level Testing (Modules 1-10)**
1. [Overview & Purpose](#1-overview--purpose)
2. [Test Credentials & Setup](#2-test-credentials--setup)
3. [Test Case Format & Status Key](#3-test-case-format--status-key)
4. [Module 1: Authentication & Account Management](#4-module-1-authentication--account-management)
5. [Module 2: Public User Flows](#5-module-2-public-user-flows)
6. [Module 3: External User Flows](#6-module-3-external-user-flows)
7. [Module 4: Analyst User Flows](#7-module-4-analyst-user-flows)
8. [Module 5: Admin User Flows](#8-module-5-admin-user-flows)
9. [Module 6: Certificate 14-State Workflow](#9-module-6-certificate-14-state-workflow)
10. [Module 7: Partner API Flows](#10-module-7-partner-api-flows)
11. [Module 8: Cross-Role Permission Matrix Validation](#11-module-8-cross-role-permission-matrix-validation)
12. [Module 9: Data Export & Import](#12-module-9-data-export--import)
13. [Module 10: Edge Cases & Negative Testing](#13-module-10-edge-cases--negative-testing)

**Part B: End-to-End Business Scenarios (Modules 11-19)**
14. [Module 11: E2E Scenario 1 - Certificate Full Lifecycle](#14-module-11-e2e-scenario-1---certificate-full-lifecycle)
15. [Module 12: E2E Scenario 2 - New Company Onboarding](#15-module-12-e2e-scenario-2---new-company-onboarding)
16. [Module 13: E2E Scenario 3 - Partner API Integration](#16-module-13-e2e-scenario-3---partner-api-integration)
17. [Module 14: E2E Scenario 4 - Admin System Setup (Day 1)](#17-module-14-e2e-scenario-4---admin-system-setup-day-1)
18. [Module 15: E2E Scenario 5 - Certificate Renewal Lifecycle](#18-module-15-e2e-scenario-5---certificate-renewal-lifecycle)
19. [Module 16: E2E Scenario 6 - Rejection & Recovery Flow](#19-module-16-e2e-scenario-6---rejection--recovery-flow)
20. [Module 17: E2E Scenario 7 - Bulk Operations Workflow](#20-module-17-e2e-scenario-7---bulk-operations-workflow)
21. [Module 18: E2E Scenario 8 - User Access Lifecycle](#21-module-18-e2e-scenario-8---user-access-lifecycle)
22. [Module 19: E2E Scenario 9 - Search & Discovery Cross-Role](#22-module-19-e2e-scenario-9---search--discovery-cross-role)

**Part B2: Accessibility Testing (Module 20)**
23. [Module 20: Accessibility Testing (WCAG 2.1 AA)](#23-module-20-accessibility-testing-wcag-21-aa)

**Part C: Traceability & Reporting**
24. [Requirements Traceability Matrix](#24-requirements-traceability-matrix)
25. [Defect Reporting Template](#25-defect-reporting-template)
26. [Test Execution Summary](#26-test-execution-summary)

---

## 1. Overview & Purpose

This guide provides a **complete, end-to-end** User Acceptance Testing plan for the GCC Database Solution. It covers:

- **All 6 user roles**: Public, External Client, Partner, Analyst, Admin, API Integration
- **All application pages**: 30+ pages across authentication, certificates, admin, and API
- **14-state certificate workflow**: Every status transition with role-based access
- **Permission verification**: What each role can and cannot do
- **Requirements validation**: Mapping test cases back to documented requirements

### How to Use This Guide

1. **Before starting**: Set up test accounts per Section 2
2. **Execute by module**: Work through Modules 1-10 in order
3. **Record results**: Use Pass/Fail/Blocked columns in each test table
4. **Log defects**: Use the template in Section 25
5. **Track progress**: Update the summary in Section 26

---

## 2. Test Credentials & Setup

### Test Accounts

| Username | Password | Role | Company | Purpose |
|----------|----------|------|---------|---------|
| admin | TestPass123! | Administrator | N/A | Full system access |
| analyst | TestPass123! | Analyst | N/A | Certificate management |
| external | TestPass123! | External Client | (Assigned Company) | Company-scoped view |
| (no account) | N/A | Public | N/A | Unauthenticated access |

### Environment URLs

| Environment | URL |
|-------------|-----|
| Local Dev | http://localhost:5101 |
| UAT | (TBD - configure per deployment) |

### Pre-Test Checklist

- [ ] Application is running and accessible
- [ ] **Run `UAT_TEST_DATA_SETUP.sql`** against UAT database (creates all required seed data)
- [ ] Database is seeded with test data (4 companies, 6 products, 8 certificates, 7 tags)
- [ ] 8 certificates exist in various statuses (Active x3, Draft, Archived, Expired, PendingReview, Active-expiring)
- [ ] 4 companies exist with products (EcoProduct Co, GreenTech Industries, TestCo UAT, OtherCo)
- [ ] External user is associated with TestCo UAT (Company Id 3)
- [ ] PDF test file available (< 10MB) for upload testing
- [ ] CSV test files available for import testing
- [ ] Browser DevTools available for API/network inspection
- [ ] For accessibility testing: Screen reader installed (NVDA/JAWS/VoiceOver)

---

## 3. Test Case Format & Status Key

### Test Case Format

Each test case includes:
- **ID**: Unique identifier (e.g., TC-AUTH-001)
- **Description**: What is being tested
- **Pre-conditions**: Setup required before test
- **Steps**: Numbered actions to perform
- **Expected Result**: What should happen
- **Status**: Pass / Fail / Blocked / Not Run

### Status Key

| Status | Icon | Meaning |
|--------|------|---------|
| Pass | :white_check_mark: | Test passed as expected |
| Fail | :x: | Test did not produce expected result |
| Blocked | :no_entry: | Cannot execute due to dependency |
| Not Run | :black_square_button: | Not yet executed |

---

## 4. Module 1: Authentication & Account Management

### 1.1 Login Flow

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-AUTH-001 | Valid admin login | 1. Navigate to /Account/Login 2. Enter admin/TestPass123! 3. Click Sign In | Redirect to Dashboard or Internal Tile View | |
| TC-AUTH-002 | Valid analyst login | 1. Navigate to /Account/Login 2. Enter analyst/TestPass123! 3. Click Sign In | Redirect to Internal Tile View | |
| TC-AUTH-003 | Valid external login | 1. Navigate to /Account/Login 2. Enter external/TestPass123! 3. Click Sign In | Redirect to External Tile View | |
| TC-AUTH-004 | Invalid password | 1. Navigate to /Account/Login 2. Enter admin/WrongPassword 3. Click Sign In | Error message displayed, no redirect | |
| TC-AUTH-005 | Invalid username | 1. Navigate to /Account/Login 2. Enter nonexistent/TestPass123! 3. Click Sign In | Error message displayed | |
| TC-AUTH-006 | Empty fields submission | 1. Navigate to /Account/Login 2. Leave both fields empty 3. Click Sign In | Validation errors shown for required fields | |
| TC-AUTH-007 | Account lockout | 1. Enter wrong password 5+ times for same account | Account locked message displayed | |
| TC-AUTH-008 | Remember Me functionality | 1. Login with Remember Me checked 2. Close browser 3. Reopen and navigate to app | User remains authenticated | |

### 1.2 Two-Factor Authentication (2FA)

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-AUTH-009 | Enable 2FA | 1. Login as admin 2. Navigate to Account > Two Factor Preferences 3. Enable 2FA 4. Scan QR code with authenticator app 5. Enter TOTP code | 2FA enabled, recovery codes displayed | |
| TC-AUTH-010 | Login with 2FA enabled | 1. Login with username/password 2. Enter TOTP code from authenticator | Successful login after 2FA verification | |
| TC-AUTH-011 | Invalid 2FA code | 1. Login with username/password 2. Enter wrong TOTP code | Error message, not authenticated | |
| TC-AUTH-012 | Disable 2FA (Admin) | 1. Login as admin 2. Navigate to User Management 3. Select user with 2FA 4. Click Disable 2FA | 2FA disabled for target user | |

### 1.3 Password Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-AUTH-013 | Forgot password flow | 1. Click Forgot Password on login 2. Enter email 3. Check email for reset link 4. Set new password | Password reset successful, can login with new password | |
| TC-AUTH-014 | Force password reset | 1. Admin flags user for password reset 2. User logs in 3. User is redirected to password reset | User must set new password before accessing system | |

### 1.4 Session & Authorization

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-AUTH-015 | Session timeout | 1. Login 2. Leave idle beyond session timeout 3. Attempt action | Redirect to login page | |
| TC-AUTH-016 | Access denied - External to Admin | 1. Login as external 2. Navigate directly to /Admin/UserManagement | Access Denied page or redirect to appropriate view | |
| TC-AUTH-017 | Access denied - Analyst to Admin | 1. Login as analyst 2. Navigate directly to /Admin/UserManagement | Access Denied page or redirect | |
| TC-AUTH-018 | Logout | 1. Login as any user 2. Click Logout | Redirected to login page, session cleared | |

---

## 5. Module 2: Public User Flows

**Role**: Unauthenticated user (no login required)

### 2.1 Public Certificate Tile View

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-PUB-001 | Access public tile view | 1. Navigate to /Certificate/CertificateTileView (no login) | Page loads with certificate tiles visible | |
| TC-PUB-002 | Infinite scroll loading | 1. View tile view 2. Scroll to bottom of page | Additional certificates load automatically (no pagination buttons) | |
| TC-PUB-003 | Search by certificate number | 1. Type certificate number in search bar (e.g., "24-") | Autocomplete suggestions appear, results filter | |
| TC-PUB-004 | Search by company name | 1. Type company name in search bar | Autocomplete suggestions with categorized results | |
| TC-PUB-005 | Filter by certification type | 1. Open filter panel 2. Select a certification type checkbox | Tiles filter to show only selected type | |
| TC-PUB-006 | Filter by status | 1. Open filter panel 2. Select "Active" status | Only Active certificates shown | |
| TC-PUB-007 | Multiple filters combined | 1. Select certification type filter 2. Select status filter 3. Enter search text | Results match ALL applied filters | |
| TC-PUB-008 | Clear all filters | 1. Apply multiple filters 2. Click clear/reset | All certificates shown, filters cleared | |
| TC-PUB-009 | Shareable URL | 1. Apply filters 2. Copy URL from browser 3. Open in new incognito window | Same filters applied, same results shown | |
| TC-PUB-010 | No edit/create buttons visible | 1. View tile view as public user | No Add, Edit, or Delete buttons anywhere | |
| TC-PUB-011 | No internal tags visible | 1. View certificate tiles | Internal tag column/data is NOT shown | |
| TC-PUB-012 | No navigation menu | 1. View page as unauthenticated user | Only public tile view accessible, no admin/management nav links | |
| TC-PUB-013a | Draft certificates NOT visible | 1. Ensure a Draft certificate exists 2. View public tile view 3. Search for it by number | Certificate NOT shown (only Active certificates visible to public) | |
| TC-PUB-013b | Archived certificates NOT visible | 1. Ensure an Archived certificate exists 2. View public tile view | Certificate NOT shown | |
| TC-PUB-013c | Filter options from database | 1. Open filter panel on public tile view | Filter options (certification types, statuses) are dynamically loaded from database, not hardcoded values | |

### 2.2 Public Certificate Details

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-PUB-013 | View single certificate | 1. Click on a certificate tile | Certificate detail page loads with: Certificate Number, Company, Product, Status, Dates, Certification Type | |
| TC-PUB-014 | Certificate preview image | 1. View certificate details | PDF thumbnail preview shown (or "Certificate Unavailable" placeholder) | |
| TC-PUB-015 | Download certificate PDF | 1. View certificate with PDF uploaded 2. Click Download button | PDF file downloads | |
| TC-PUB-016 | No edit buttons on details | 1. View certificate detail page as public | No Edit or Delete buttons visible | |
| TC-PUB-017 | Certificate number in URL | 1. View certificate details | URL uses certificate number (e.g., /Certificate/CertificateSingleView/24-0366), NOT database ID | |

---

## 6. Module 3: External User Flows

**Role**: External Client (login: external / TestPass123!)
**Key Rule**: Can ONLY see data for their associated company/partner

### 3.1 External Certificate Tile View

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-EXT-001 | Access external tile view | 1. Login as external 2. Verify redirect to /Certificate/CertificateTileViewExternal | External tile view loads | |
| TC-EXT-002 | Only own company data visible | 1. View tile view 2. Check all visible certificates | ALL certificates belong to the external user's associated company only | |
| TC-EXT-003 | Cannot see other company data | 1. Search for a certificate from a different company | No results returned for other companies | |
| TC-EXT-004 | No Add Certificate button | 1. View external tile view | No "Add" or "Create" button visible | |
| TC-EXT-005 | No Edit buttons | 1. View tiles | No Edit button/icon on any certificate | |
| TC-EXT-006 | No Delete buttons | 1. View tiles | No Delete button/icon on any certificate | |
| TC-EXT-007 | No internal tags visible | 1. View certificate tiles | Internal Tag column is NOT displayed | |
| TC-EXT-008 | External tags visible | 1. View certificate with external tags | External tags ARE displayed | |
| TC-EXT-009 | Search within own data | 1. Use search bar 2. Enter text | Search works but only within company's certificates | |
| TC-EXT-010 | Filter within own data | 1. Apply certification type filter | Filters work within company's data only | |
| TC-EXT-011 | Infinite scroll | 1. Scroll to bottom | More certificates from own company load | |
| TC-EXT-012 | Bulk ZIP download | 1. Select multiple certificates 2. Click download | ZIP file downloads with selected certificate PDFs | |

### 3.2 External Certificate Details

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-EXT-013 | View own certificate detail | 1. Click on a certificate tile | Detail view shows certificate data (read-only) | |
| TC-EXT-014 | No edit/delete on details | 1. View certificate detail page | No Edit or Delete buttons | |
| TC-EXT-015 | Cannot access other company cert via URL | 1. Manually enter URL for a certificate from another company | Access denied or certificate not found | |

### 3.3 External Product Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-EXT-016 | Access product management | 1. Navigate to /Product/Management | Page loads with products | |
| TC-EXT-017 | Only own company products | 1. View product list | Only products for external user's company shown | |
| TC-EXT-018 | No Actions column | 1. View product table | Actions column (Edit/Delete) is NOT visible | |
| TC-EXT-019 | No Add Product button | 1. View product page | No Add/Create button visible | |
| TC-EXT-020 | Product data is read-only | 1. Try to interact with product data | No inline editing available | |

### 3.4 External Navigation Verification

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-EXT-021 | Navigation menu items | 1. Check main navigation menu | Only see: Certificates, Products (NO Admin, NO Companies, NO Dashboard) | |
| TC-EXT-022 | Cannot access admin pages | 1. Navigate directly to /Admin/UserManagement | Access denied | |
| TC-EXT-023 | Cannot access certificate create | 1. Navigate directly to /Certificate/Create | Access denied | |
| TC-EXT-024 | Cannot access company management | 1. Navigate directly to /Company/Index | Access denied | |

---

## 7. Module 4: Analyst User Flows

**Role**: Analyst (login: analyst / TestPass123!)
**Key Rule**: Can create/edit certificates, manage workflow, but NO admin or delete access

### 4.1 Internal Certificate Tile View

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-001 | Access internal tile view | 1. Login as analyst | Internal tile view loads at /Certificate/CertificateTileViewInternal | |
| TC-ANL-002 | See all certificates | 1. View tile view | Certificates from ALL companies visible (not scoped) | |
| TC-ANL-003 | Internal tags visible | 1. View tiles | Internal tags column IS visible | |
| TC-ANL-004 | Add Certificate button visible | 1. View tile view | Add/Create New button IS visible | |
| TC-ANL-005 | Edit button visible | 1. View certificate tile | Edit button/icon IS visible on each tile | |
| TC-ANL-006 | Search across all data | 1. Search for certificate | Results span all companies and types | |
| TC-ANL-007 | All filters available | 1. Open filter panel | Can filter by: Certification Type, Status, Company, Partner, Tags | |
| TC-ANL-008 | Shareable URL with filters | 1. Apply filters 2. Copy URL 3. Share with another analyst | Same filters/results when opened | |
| TC-ANL-009 | Bulk ZIP download | 1. Select multiple certificates 2. Download | ZIP file with PDFs downloads | |

### 4.2 Certificate Create Flow

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-010 | Navigate to create page | 1. Click Add/Create New from tile view | /Certificate/Create page loads | |
| TC-ANL-011 | Auto-generated certificate number | 1. View create form | Certificate Number field pre-populated with next sequential number (YY-NNNN format) | |
| TC-ANL-012 | Required field validation | 1. Leave all fields empty 2. Click Save | Validation errors shown for required fields | |
| TC-ANL-013 | Select company | 1. Select a company from dropdown | Company selected, associated products may update | |
| TC-ANL-014 | Select product | 1. Select a product from dropdown | Product selected | |
| TC-ANL-015 | Select certification type | 1. Select parent certification type | Subcategory dropdown populates with child types | |
| TC-ANL-016 | Select subcategory | 1. Select subcategory from populated dropdown | Subcategory selected, custom fields may load | |
| TC-ANL-017 | Custom fields load dynamically | 1. Select a certification type that has custom fields | Custom field inputs appear below cert type selection | |
| TC-ANL-018 | Set effective date | 1. Enter or pick effective date | Date accepted in correct format | |
| TC-ANL-019 | Set expiration date | 1. Enter expiration date (after effective date) | Date accepted | |
| TC-ANL-020 | Expiration before effective date | 1. Set expiration date BEFORE effective date | Validation error shown | |
| TC-ANL-021 | Upload PDF certificate | 1. Click upload zone or drag PDF file 2. Verify preview | File name shown, preview generated, success toast | |
| TC-ANL-022 | Upload invalid file type | 1. Try to upload a .docx or .jpg file | Error: Invalid file type | |
| TC-ANL-023 | Upload oversized file | 1. Try to upload PDF > 10MB | Error: File too large | |
| TC-ANL-024 | Add external tags | 1. Select external tags from dropdown | Tag badges appear, can be removed with X | |
| TC-ANL-025 | Add internal tags | 1. Select internal tags from dropdown | Tag badges appear | |
| TC-ANL-026 | Add product identifiers | 1. Click Add Product ID 2. Fill in Type (ASIN), Value, Status 3. Save | Product ID appears in table below form | |
| TC-ANL-027 | Add multiple product IDs | 1. Add 3+ product IDs of different types (ASIN, UPC, GTIN) | All appear in product ID table | |
| TC-ANL-028 | Edit product identifier | 1. Click edit on a product ID row 2. Modify value 3. Save | Updated value shown in table | |
| TC-ANL-029 | Delete product identifier | 1. Click delete on a product ID row 2. Confirm | Product ID removed from table | |
| TC-ANL-030 | Import product IDs via CSV | 1. Click Import on product ID section 2. Upload CSV | Product IDs populated from CSV | |
| TC-ANL-031 | Enter Certification Results | 1. Enter text in Certification Results field (optional) | Text accepted (up to 1000 chars) | |
| TC-ANL-032 | Set partner | 1. Select partner (Amazon/Novi/Other/None) | Partner set | |
| TC-ANL-033 | Set location and country | 1. Enter location and country | Fields populated | |
| TC-ANL-034 | Analyst available statuses | 1. Check Status dropdown options | Should see 12 of 14 statuses (all except Rejected and Revoked): Draft, Pending Review, Under Review, Approved, Active, Suspended, Expired, Pending Renewal, Renewed, Cancelled, Archived, On Hold | |
| TC-ANL-035 | Save certificate | 1. Fill all required fields 2. Click Save | Certificate created, redirect to details page, success notification | |
| TC-ANL-036 | Save & Add Another | 1. Fill all required fields 2. Click Save & Add Another | Certificate created, redirect back to blank Create form, success message | |
| TC-ANL-037 | Unsaved changes warning | 1. Fill some fields 2. Try to navigate away without saving | Warning prompt about unsaved changes | |

### 4.3 Certificate Edit Flow

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-038 | Open edit page | 1. From tile view, click Edit on a certificate | Edit page loads with all current data pre-populated | |
| TC-ANL-039 | All fields pre-populated | 1. Verify all fields on edit page | Company, Product, Type, Subcategory, Dates, Tags, Product IDs, Custom Fields all pre-filled | |
| TC-ANL-040 | Edit company | 1. Change company dropdown | Company updated | |
| TC-ANL-041 | Edit certification type | 1. Change certification type | Subcategory and custom fields update | |
| TC-ANL-042 | Add new tag on edit | 1. Add a tag that wasn't previously assigned | Tag badge appears | |
| TC-ANL-043 | Remove existing tag | 1. Click X on an existing tag badge | Tag removed | |
| TC-ANL-044 | Replace PDF | 1. Upload new PDF on edit page | New file shown, old replaced | |
| TC-ANL-045 | Change status on edit | 1. Change status dropdown | Status updated (within allowed transitions) | |
| TC-ANL-046 | Save edited certificate | 1. Make changes 2. Click Save Changes | Changes saved, redirect to details, success notification | |
| TC-ANL-047 | Certificate number unchanged | 1. Open edit 2. Check certificate number field | Certificate number cannot be changed on edit | |

### 4.4 Certificate Details View (Internal)

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-048 | View certificate details | 1. Click on certificate from tile view | Detail page shows ALL fields including internal tags | |
| TC-ANL-049 | PDF preview and download | 1. View details of cert with PDF | Thumbnail preview visible, Download button works | |
| TC-ANL-050 | Click-to-zoom preview | 1. Click on thumbnail preview | Full-size modal opens with certificate image | |
| TC-ANL-051 | Edit button on details | 1. View details page | Edit button visible and functional | |
| TC-ANL-052 | Certificate number displayed (not ID) | 1. View details page | Page shows Certificate Number (e.g., 24-0366), NOT database ID | |

### 4.5 Analyst Navigation & Restrictions

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-053 | Navigation menu | 1. Check navigation | See: Certificates, Dashboard, Companies & Products (NOT Administration) | |
| TC-ANL-054 | Cannot delete certificates | 1. Check tile view and details | No Delete button/option visible for analyst | |
| TC-ANL-055 | Cannot access admin pages | 1. Navigate to /Admin/UserManagement directly | Access denied | |
| TC-ANL-056 | Can access Dashboard | 1. Click Dashboard in nav | Dashboard page loads with statistics | |
| TC-ANL-057 | Can access Companies | 1. Navigate to /Company/Index | Company list loads | |
| TC-ANL-058 | Can manage products | 1. Navigate to /Product/Management | Product page with CRUD operations | |

### 4.6 Certificate Import Access (Analyst - DENIED)

**Note**: Certificate Import is Admin-only. Analysts CANNOT access the bulk import page.

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-059 | Analyst cannot access certificate import | 1. Login as analyst 2. Navigate directly to /CertificateImport/Index | Access denied / redirect (CertificateImportController has AdminOnly policy) | |
| TC-ANL-060 | No import menu item for analyst | 1. Check navigation menu as analyst | Certificate Import link NOT visible in nav | |

### 4.7 Additional Analyst Validations

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ANL-061 | Cascading company-product dropdown | 1. On Create form, select a Company 2. Open Product dropdown | Product dropdown filters to show only products linked to selected company | |
| TC-ANL-062 | Company change resets product | 1. Select Company A and Product A-1 2. Change to Company B | Product dropdown resets/filters to Company B products only | |
| TC-ANL-063 | Database-driven filter options | 1. Open filter panel on internal tile view | All filter options (certification types, statuses, companies, tags) are loaded from database, not hardcoded | |

---

## 8. Module 5: Admin User Flows

**Role**: Administrator (login: admin / TestPass123!)
**Key Rule**: Full system access including all management, delete, and configuration

### 5.1 Admin Dashboard

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-001 | Access admin dashboard | 1. Login as admin | Dashboard loads with system statistics | |
| TC-ADM-002 | Statistics cards display | 1. View dashboard | Cards show: Total Certificates, Active, Expired, Companies, Users, etc. | |
| TC-ADM-003 | Stat card downloads | 1. Click download on a stat card | Data downloads as CSV | |

### 5.2 User Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-004 | Access user management | 1. Navigate to Admin > User Management | User list table loads | |
| TC-ADM-005 | Search users | 1. Type in search box | Table filters in real-time | |
| TC-ADM-006 | Filter by role | 1. Select role from filter dropdown | Only users with selected role shown | |
| TC-ADM-007 | Filter by status | 1. Select Active/Inactive filter | Users filtered by status | |
| TC-ADM-008 | Filter by company | 1. Select company from filter | Users filtered by company association | |
| TC-ADM-009 | Create new user | 1. Click Add New 2. Fill: Username, Email, Password, Role, Company (optional) 3. Save | User created, appears in table, success toast | |
| TC-ADM-010 | Create user - duplicate email | 1. Try to create user with existing email | Error: Email must be unique | |
| TC-ADM-011 | Create user - duplicate username | 1. Try to create user with existing username | Error: Username must be unique | |
| TC-ADM-012 | Edit existing user | 1. Click Edit on a user 2. Change role 3. Save | User updated, success toast | |
| TC-ADM-013 | Delete user | 1. Click Delete on a user 2. Confirm in modal | Confirmation modal appears FIRST, then user deleted, success toast | |
| TC-ADM-014 | Cannot delete admin user | 1. Try to delete the admin user | Operation prevented, error message | |
| TC-ADM-015 | Reset user 2FA | 1. Click Reset 2FA on user with 2FA enabled | 2FA reset for user | |
| TC-ADM-016 | Export users CSV | 1. Click Export | CSV file downloads with all user data | |
| TC-ADM-017 | Download user import template | 1. Click Template | CSV template downloads with correct headers | |
| TC-ADM-018 | Import users from CSV | 1. Click Import 2. Upload valid CSV | Users imported, success notification | |

### 5.3 Role Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-019 | Access role management | 1. Navigate to Admin > Role Management | Role list with permission matrix loads | |
| TC-ADM-020 | View role permissions | 1. Click on a role | Permission checkboxes displayed by category (6 categories, 24 permissions) | |
| TC-ADM-021 | Create custom role | 1. Click Add Role 2. Enter Name/Description 3. Select permissions 4. Save | Role created with selected permissions | |
| TC-ADM-022 | Edit role permissions | 1. Click Edit on role 2. Change permissions 3. Save | Permissions updated | |
| TC-ADM-023 | Delete custom role | 1. Click Delete on custom role 2. Confirm | Role deleted (confirmation modal before, toast after) | |
| TC-ADM-024 | Cannot delete Admin role | 1. Try to delete Admin role | Operation prevented | |
| TC-ADM-025 | Select All permissions | 1. Edit role 2. Click Select All | All permissions checked | |
| TC-ADM-026 | Clear All permissions | 1. Edit role 2. Click Clear All | All permissions unchecked | |
| TC-ADM-027 | Export roles CSV | 1. Click Export | Roles CSV downloads | |

### 5.4 Certificate Type Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-028 | Access certificate types | 1. Navigate to Admin > Data Management > Certificate Types | Certificate type hierarchy loads | |
| TC-ADM-029 | View parent/child hierarchy | 1. View certificate types list | Parent types shown with associated subcategories | |
| TC-ADM-030 | Create parent cert type | 1. Click Add 2. Enter Name, Description 3. Save | New parent certification type created | |
| TC-ADM-031 | Create subcategory | 1. Click Add subcategory under a parent 2. Enter Name 3. Save | Subcategory created under parent | |
| TC-ADM-032 | Edit cert type (Amazon Code) | 1. Edit a cert type 2. Set AmazonCode field 3. Save | Amazon Code saved for API mapping | |
| TC-ADM-033 | Delete cert type | 1. Delete a cert type with no certificates 2. Confirm | Type deleted | |
| TC-ADM-034 | Help modal | 1. Click Help button | Help modal displays hierarchy explanation | |

### 5.5 Certificate Field Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-035 | Access certificate fields | 1. Navigate to Admin > Certificate Fields | Custom fields list loads | |
| TC-ADM-036 | Create text field | 1. Add new field: Type=Text, Name, Required/Optional 2. Save | Text field created | |
| TC-ADM-037 | Create number field | 1. Add new field: Type=Number 2. Save | Number field created | |
| TC-ADM-038 | Create date field | 1. Add new field: Type=Date 2. Save | Date field created | |
| TC-ADM-039 | Create dropdown field | 1. Add new field: Type=Select, add options 2. Save | Dropdown field created with options | |
| TC-ADM-040 | Associate field with cert type | 1. Set certification type association on field | Field appears when that cert type is selected on Create/Edit | |
| TC-ADM-041 | Edit custom field | 1. Edit field name/type 2. Save | Field updated | |
| TC-ADM-042 | Delete custom field | 1. Delete field 2. Confirm | Field deleted (confirmation modal, then toast) | |

### 5.6 Tag Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-043 | Access tag management | 1. Navigate to Admin > Tags | Tag list loads | |
| TC-ADM-044 | Filter by tag type | 1. Filter by Internal/External | Tags filtered by type | |
| TC-ADM-045 | Create external tag | 1. Add new tag: Name, Type=External, Active 2. Save | External tag created (visible to external users) | |
| TC-ADM-046 | Create internal tag | 1. Add new tag: Name, Type=Internal, Active 2. Save | Internal tag created (NOT visible to external users) | |
| TC-ADM-047 | Edit tag | 1. Edit tag name/status 2. Save | Tag updated | |
| TC-ADM-048 | Delete tag | 1. Delete tag 2. Confirm | Tag deleted (confirmation modal, then toast) | |
| TC-ADM-049 | Export tags CSV | 1. Click Export | Tags CSV downloads | |
| TC-ADM-050 | Download tag template | 1. Click Template | Import template CSV downloads | |
| TC-ADM-051 | Import tags CSV | 1. Click Import 2. Upload CSV | Tags imported | |

### 5.7 Partner Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-052 | Access partners | 1. Navigate to Admin > Partners | Partner list loads | |
| TC-ADM-053 | Core partners exist | 1. View list | Amazon, Novi, Other partners present | |
| TC-ADM-054 | Create partner | 1. Click Add 2. Enter partner details 3. Save | Partner created | |
| TC-ADM-055 | Edit partner | 1. Edit partner details 2. Save | Partner updated | |
| TC-ADM-056 | Delete partner | 1. Delete partner 2. Confirm | Partner deleted | |
| TC-ADM-057 | Export partners CSV | 1. Click Export | Partners CSV downloads | |
| TC-ADM-058 | No Template/Import buttons | 1. View Partners page | NO Template button, NO Import button (Export + Add only) | |

### 5.8 Product Type Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-059 | Access product types | 1. Navigate to Admin > Product Types | Product type list loads | |
| TC-ADM-060 | Create product type | 1. Add new product type 2. Save | Product type created | |
| TC-ADM-061 | Edit product type | 1. Edit product type 2. Save | Product type updated | |
| TC-ADM-062 | Delete product type | 1. Delete product type 2. Confirm | Product type deleted | |

### 5.9 CSF Attributes

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-063 | Access CSF attributes | 1. Navigate to Admin > CSF Attributes | CSF attribute list loads | |
| TC-ADM-064 | CRUD operations | 1. Create, Read, Update, Delete CSF attributes | All CRUD operations work | |

### 5.10 Pathway Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-065 | Access pathways | 1. Navigate to Admin > Pathways | Pathway list loads | |
| TC-ADM-066 | Create pathway | 1. Add new pathway with cert type association 2. Save | Pathway created | |
| TC-ADM-067 | Edit pathway | 1. Edit pathway 2. Save | Pathway updated | |
| TC-ADM-068 | Delete pathway | 1. Delete pathway 2. Confirm | Pathway deleted | |

### 5.11 Partner API Management

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-069 | Access Partner API page | 1. Navigate to Admin > API Management > Partner API | API key management loads | |
| TC-ADM-070 | Generate API key | 1. Generate new API key for partner | Key generated, displayed | |
| TC-ADM-071 | View API usage | 1. Check usage statistics for partner | Usage stats displayed | |
| TC-ADM-072 | Revoke API key | 1. Revoke existing API key | Key revoked, no longer functional | |

### 5.12 Google Analytics Configuration

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-073 | Access GA settings | 1. Navigate to Admin > Configuration > Google Analytics | GA settings page loads | |
| TC-ADM-074 | Enable/Disable tracking | 1. Toggle GA tracking on/off 2. Save | Setting persisted | |

### 5.13 Certificate Import (Admin Only)

**Note**: Certificate Import requires AdminOnly policy. Analysts and External users cannot access this page.

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-075 | Access certificate import | 1. Login as admin 2. Navigate to /CertificateImport/Index | Import page loads | |
| TC-ADM-076 | Download import template | 1. Click Template button | CSV template file downloads with correct column headers | |
| TC-ADM-077 | Upload valid CSV | 1. Upload CSV with valid certificate data | Preview page shows parsed data | |
| TC-ADM-078 | Preview import data | 1. Review preview table | Shows what will be created/updated with validation status per row | |
| TC-ADM-079 | Apply import | 1. Click Apply on preview | Certificates created/updated, success notification with count | |
| TC-ADM-080 | Cancel import | 1. Click Cancel on preview | No changes made, return to import page | |
| TC-ADM-081 | Upload invalid CSV format | 1. Upload CSV with wrong column names | Error message about invalid format | |

### 5.14 Admin-Specific Certificate Operations

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-082 | Admin can delete certificates | 1. View certificate 2. Click Delete 3. Confirm modal | Confirmation modal, then certificate deleted, success toast | |
| TC-ADM-083 | Admin sees all 14 statuses | 1. Open Create/Edit certificate 2. Check Status dropdown | All 14 statuses available: Draft, Pending Review, Under Review, Approved, Active, Suspended, Revoked, Expired, Pending Renewal, Renewed, Cancelled, Archived, On Hold, Rejected | |
| TC-ADM-084 | Delete notification pattern | 1. Click Delete on any entity | Confirmation modal BEFORE delete, Toast notification AFTER success (NOT native alert/confirm) | |

### 5.15 Delete with Dependencies

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-085 | Delete company with certificates | 1. Try to delete a company that has certificates assigned | Error or warning: cannot delete company with existing certificates | |
| TC-ADM-086 | Delete product with certificates | 1. Try to delete a product that has certificates assigned | Error or warning: cannot delete product with existing certificates | |
| TC-ADM-087 | Delete cert type with certificates | 1. Try to delete a certification type that has certificates | Error or warning: cannot delete cert type with existing certificates | |
| TC-ADM-088 | Delete tag with assignments | 1. Try to delete a tag assigned to certificates | Tag deleted or warning about existing assignments | |
| TC-ADM-089 | Delete role with users assigned | 1. Try to delete a role that has users | Error: cannot delete role with assigned users | |

### 5.16 Duplicate Prevention

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-ADM-090 | Duplicate tag name prevention | 1. Create tag "TestTag" 2. Try to create another tag named "TestTag" | Error: Tag name must be unique (or similar) | |
| TC-ADM-091 | Duplicate company name prevention | 1. Create company "TestCo" 2. Try to create another "TestCo" | Error or warning about duplicate name | |
| TC-ADM-092 | Duplicate cert type name prevention | 1. Create cert type with existing name | Error or handled gracefully | |

---

## 9. Module 6: Certificate 14-State Workflow

### 6.1 Workflow State Definitions

| # | State | Display Name | Description | Final? |
|---|-------|-------------|-------------|--------|
| 1 | Draft | Draft | Initial state, not yet submitted | No |
| 2 | PendingReview | Pending Review | Awaiting initial review | No |
| 3 | UnderReview | Under Review | Currently being reviewed | No |
| 4 | Approved | Approved | Approved, ready for activation | No |
| 5 | Active | Active | Currently active and valid | No |
| 6 | Suspended | Suspended | Temporarily suspended | No |
| 7 | Revoked | Revoked | Certificate revoked (near-final) | Near-final |
| 8 | Expired | Expired | Past expiration date | No |
| 9 | PendingRenewal | Pending Renewal | Within 30 days of expiration | No |
| 10 | Renewed | Renewed | Renewed, will transition to Active | No |
| 11 | Cancelled | Cancelled | Cancelled permanently | **Yes (Final)** |
| 12 | Archived | Archived | Archived permanently | **Yes (Final)** |
| 13 | OnHold | On Hold | Temporarily on hold | No |
| 14 | Rejected | Rejected | Review rejected | No |

### 6.2 Valid State Transitions Test Matrix

Test each transition by changing status on the Edit page. Verify invalid transitions are NOT available in the dropdown.

| ID | From State | To State | Valid? | Role | Steps | Expected Result | Status |
|----|-----------|----------|--------|------|-------|-----------------|--------|
| TC-WF-001 | Draft | Pending Review | Yes | Analyst/Admin | Edit cert in Draft, change to Pending Review, Save | Status changes to Pending Review | |
| TC-WF-002 | Draft | Cancelled | Yes | Admin | Edit cert in Draft, change to Cancelled | Status changes to Cancelled | |
| TC-WF-003 | Draft | Active | **NO** | Any | Check dropdown for Draft cert | Active should NOT be available from Draft | |
| TC-WF-004 | Pending Review | Under Review | Yes | Analyst/Admin | Change Pending Review to Under Review | Status updated | |
| TC-WF-005 | Pending Review | Draft | Yes | Analyst/Admin | Return to Draft from Pending Review | Status reverted | |
| TC-WF-006 | Pending Review | Cancelled | Yes | Admin | Cancel from Pending Review | Cancelled | |
| TC-WF-007 | Pending Review | On Hold | Yes | Admin | Put Pending Review on hold | Status = On Hold | |
| TC-WF-008 | Under Review | Approved | Yes | Analyst/Admin | Approve from Under Review | Status = Approved | |
| TC-WF-009 | Under Review | Pending Review | Yes | Analyst/Admin | Send back for review | Status = Pending Review | |
| TC-WF-010 | Under Review | Rejected | Yes | Admin | Reject from Under Review | Status = Rejected | |
| TC-WF-011 | Under Review | On Hold | Yes | Admin | Hold from Under Review | Status = On Hold | |
| TC-WF-012 | Approved | Active | Yes | Analyst/Admin | Activate from Approved | Status = Active | |
| TC-WF-013 | Approved | Under Review | Yes | Analyst/Admin | Send back from Approved | Status = Under Review | |
| TC-WF-014 | Approved | On Hold | Yes | Admin | Hold from Approved | Status = On Hold | |
| TC-WF-015 | Active | Suspended | Yes | Admin | Suspend active cert | Status = Suspended | |
| TC-WF-016 | Active | Expired | Yes | Admin/System | Expire active cert | Status = Expired | |
| TC-WF-017 | Active | Pending Renewal | Yes | Admin/System | Mark for renewal (or auto 30-day) | Status = Pending Renewal | |
| TC-WF-018 | Active | Revoked | Yes | Admin | Revoke active cert | Status = Revoked | |
| TC-WF-019 | Active | Archived | Yes | Admin | Archive active cert | Status = Archived | |
| TC-WF-020 | Suspended | Active | Yes | Admin | Reactivate suspended cert | Status = Active | |
| TC-WF-021 | Suspended | Revoked | Yes | Admin | Revoke suspended cert | Status = Revoked | |
| TC-WF-022 | Suspended | Cancelled | Yes | Admin | Cancel suspended cert | Status = Cancelled | |
| TC-WF-023 | Suspended | Archived | Yes | Admin | Archive suspended cert | Status = Archived | |
| TC-WF-024 | Revoked | Archived | Yes | Admin | Archive revoked cert | Status = Archived | |
| TC-WF-025 | Revoked | Active | **NO** | Any | Check dropdown for Revoked cert | Active NOT available (Revoked is near-final) | |
| TC-WF-026 | Expired | Pending Renewal | Yes | Analyst/Admin | Renew expired cert | Status = Pending Renewal | |
| TC-WF-027 | Expired | Archived | Yes | Admin | Archive expired cert | Status = Archived | |
| TC-WF-028 | Expired | Active | Yes | Admin | Reactivate expired cert | Status = Active | |
| TC-WF-029 | Pending Renewal | Under Review | Yes | Analyst/Admin | Submit renewal for review | Status = Under Review | |
| TC-WF-030 | Pending Renewal | Renewed | Yes | Analyst/Admin | Mark as renewed | Status = Renewed | |
| TC-WF-031 | Pending Renewal | Expired | Yes | Admin/System | Let renewal expire | Status = Expired | |
| TC-WF-032 | Pending Renewal | Cancelled | Yes | Admin | Cancel from renewal | Status = Cancelled | |
| TC-WF-033 | Renewed | Active | Yes | Analyst/Admin | Activate renewed cert | Status = Active | |
| TC-WF-034 | Cancelled | ANY | **NO** | Any | Check dropdown for Cancelled cert | NO transitions available (Final state) | |
| TC-WF-035 | Archived | ANY | **NO** | Any | Check dropdown for Archived cert | NO transitions available (Final state) | |
| TC-WF-036 | On Hold | Pending Review | Yes | Admin | Resume on-hold cert to review | Status = Pending Review | |
| TC-WF-037 | On Hold | Under Review | Yes | Admin | Resume to review | Status = Under Review | |
| TC-WF-038 | On Hold | Approved | Yes | Admin | Resume to approved | Status = Approved | |
| TC-WF-039 | On Hold | Cancelled | Yes | Admin | Cancel from hold | Status = Cancelled | |
| TC-WF-040 | Rejected | Draft | Yes | Analyst/Admin | Restart rejected cert | Status = Draft | |
| TC-WF-041 | Rejected | Archived | Yes | Admin | Archive rejected cert | Status = Archived | |

### 6.3 Role-Based Status Access

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-WF-042 | Admin sees all 14 statuses | 1. Login as admin 2. Edit certificate 3. Check status dropdown | All 14 statuses visible | |
| TC-WF-043 | Analyst limited statuses | 1. Login as analyst 2. Edit certificate 3. Check status dropdown | Should see 12 of 14 statuses (all except Rejected and Revoked). Verify: Draft, Pending Review, Under Review, Approved, Active, Suspended, Expired, Pending Renewal, Renewed, Cancelled, Archived, On Hold | |
| TC-WF-044 | External cannot change status | 1. Login as external 2. Verify no edit access | Cannot edit certificates at all | |

### 6.4 Automated Workflow

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-WF-045 | Auto Pending Renewal (30 days) | 1. Create cert with expiration 30 days from now 2. Set status to Active 3. Wait for background job (or trigger manually) | Status auto-changes to Pending Renewal | |
| TC-WF-046 | Auto Expired | 1. Create cert with past expiration date 2. Set to Active 3. Trigger expiration check | Status auto-changes to Expired | |
| TC-WF-047 | Status history audit trail | 1. Change a cert through multiple statuses 2. Check audit log | Each transition logged with: user, timestamp, from/to status | |

---

## 10. Module 7: Partner API Flows

**Tested using curl, PowerShell, or API testing tools (Postman, etc.)**

### 7.1 API Authentication

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-API-001 | Valid API key auth | 1. Send GET /api/v1/certificates with valid X-API-Key header | 200 OK with certificate data | |
| TC-API-002 | Missing API key | 1. Send GET /api/v1/certificates without X-API-Key | 401 Unauthorized | |
| TC-API-003 | Invalid API key | 1. Send GET /api/v1/certificates with wrong key | 401 Unauthorized | |
| TC-API-004 | Revoked API key | 1. Use a previously revoked API key | 401 Unauthorized | |

### 7.2 Certificate Endpoints

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-API-005 | List certificates (paginated) | GET /api/v1/certificates?pageSize=10&pageNumber=1 | JSON with pagination metadata + data array | |
| TC-API-006 | Get by ID | GET /api/v1/certificates/{id} | Single certificate JSON | |
| TC-API-007 | Get by certificate number | GET /api/v1/certificates/number/{number} | Single certificate by number | |
| TC-API-008 | Include product IDs | GET /api/v1/certificates?includeProductIds=true | Response includes productIds array | |
| TC-API-009 | Filter by status | GET /api/v1/certificates?status=Active | Only Active certificates returned | |
| TC-API-010 | Filter by company | GET /api/v1/certificates?companyId={id} | Only specified company's certs | |
| TC-API-011 | Filter by partner | GET /api/v1/certificates?partnerId={id} | Only specified partner's certs | |
| TC-API-012 | Search by product ID | GET /api/v1/certificates/search/productid?productId={ASIN}&type=ASIN | Certificates matching product ID | |
| TC-API-013 | Download PDF | GET /api/v1/certificates/{number}/download | PDF file returned | |
| TC-API-014 | Non-existent certificate | GET /api/v1/certificates/999999 | 404 Not Found | |

### 7.3 API Response Format

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-API-015 | Pagination metadata | 1. Request list endpoint | Response contains: totalCount, pageNumber, pageSize, totalPages, hasPrevious, hasNext | |
| TC-API-016 | Certificate fields | 1. Get single certificate | Response contains: certificateNumber, status, issueDate, expirationDate, company, productName, certificationType, certificationTypeSubcategory, issuer, certificationResults, partner | |
| TC-API-017 | Issuer always "Green Circle Certified" | 1. Check issuer field on any cert | issuer = "Green Circle Certified" (static value) | |
| TC-API-018 | Product ID format | 1. Get cert with includeProductIds=true | Each productId has: type, value, status, partner, productName, productSku, description | |
| TC-API-019 | Date format ISO 8601 | 1. Check date fields | Dates in format: YYYY-MM-DDTHH:mm:ssZ | |

### 7.4 Rate Limiting

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-API-020 | Rate limit enforced | 1. Send rapid requests exceeding rate limit | 429 Too Many Requests after limit exceeded | |
| TC-API-021 | Rate limit headers | 1. Check response headers | Headers include rate limit info (remaining, reset) | |

---

## 11. Module 8: Cross-Role Permission Matrix Validation

### 8.1 Page Access by Role

Test each URL directly in the browser. Verify correct access or denial.

| ID | URL | Public | External | Analyst | Admin | Status |
|----|-----|--------|----------|---------|-------|--------|
| TC-PERM-001 | /Certificate/CertificateTileView | Allowed | Allowed | Allowed | Allowed | |
| TC-PERM-002 | /Certificate/CertificateTileViewExternal | Denied | Allowed | Allowed | Allowed | |
| TC-PERM-003 | /Certificate/CertificateTileViewInternal | Denied | Denied | Allowed | Allowed | |
| TC-PERM-004 | /Certificate/Create | Denied | Denied | Allowed | Allowed | |
| TC-PERM-005 | /Certificate/Edit/{number} | Denied | Denied | Allowed | Allowed | |
| TC-PERM-006 | /Dashboard/Index | Denied | Denied | Allowed | Allowed | |
| TC-PERM-007 | /Company/Index | Denied | Denied | Allowed | Allowed | |
| TC-PERM-008 | /Product/Management | Denied | Allowed (Read-only) | Allowed | Allowed | |
| TC-PERM-009 | /Admin/UserManagement | Denied | Denied | Denied | Allowed | |
| TC-PERM-010 | /Admin/RoleManagement | Denied | Denied | Denied | Allowed | |
| TC-PERM-011 | /Admin/CertificateTypes | Denied | Denied | Denied | Allowed | |
| TC-PERM-012 | /Admin/CertificateFields | Denied | Denied | Denied | Allowed | |
| TC-PERM-013 | /Admin/Tags | Denied | Denied | Denied | Allowed | |
| TC-PERM-014 | /Admin/Partners | Denied | Denied | Denied | Allowed | |
| TC-PERM-015 | /Admin/Pathways | Denied | Denied | Denied | Allowed | |
| TC-PERM-016 | /Admin/ProductTypes | Denied | Denied | Denied | Allowed | |
| TC-PERM-017 | /Admin/PartnerAPI | Denied | Denied | Denied | Allowed | |
| TC-PERM-018 | /Admin/GoogleAnalytics | Denied | Denied | Denied | Allowed | |
| TC-PERM-019 | /CertificateImport/Index | Denied | Denied | **Denied** | Allowed | |

### 8.2 Feature Access by Role

| ID | Feature | Public | External | Analyst | Admin | Status |
|----|---------|--------|----------|---------|-------|--------|
| TC-PERM-020 | View certificates | Yes (public only) | Yes (own company) | Yes (all) | Yes (all) | |
| TC-PERM-021 | Create certificates | No | No | Yes | Yes | |
| TC-PERM-022 | Edit certificates | No | No | Yes | Yes | |
| TC-PERM-023 | Delete certificates | No | No | No | Yes | |
| TC-PERM-024 | Manage workflow | No | No | Yes (limited) | Yes (full) | |
| TC-PERM-025 | Export data (CSV) | No | Yes (own data) | Yes | Yes | |
| TC-PERM-026 | Import data (CSV) - Certificate Import | No | No | **No (Admin only)** | Yes | |
| TC-PERM-026a | Import data (CSV) - Product IDs within cert form | No | No | Yes | Yes | |
| TC-PERM-027 | View internal tags | No | No | Yes | Yes | |
| TC-PERM-028 | View external tags | No | Yes | Yes | Yes | |
| TC-PERM-029 | Manage users | No | No | No | Yes | |
| TC-PERM-030 | Manage roles | No | No | No | Yes | |
| TC-PERM-031 | Manage companies | No | No | No | Yes | |
| TC-PERM-032 | Manage tags | No | No | No | Yes | |
| TC-PERM-033 | Manage partners | No | No | No | Yes | |
| TC-PERM-034 | API access | No | No | No | Yes | |
| TC-PERM-035 | View products | No | Yes (own company) | Yes | Yes | |
| TC-PERM-036 | Manage products | No | No | No | Yes | |
| TC-PERM-037 | System settings | No | No | No | Yes | |

### 8.3 UI Element Visibility by Role

| ID | UI Element | Public | External | Analyst | Admin | Status |
|----|-----------|--------|----------|---------|-------|--------|
| TC-PERM-038 | Navigation: Administration menu | Hidden | Hidden | Hidden | Visible | |
| TC-PERM-039 | Navigation: Dashboard | Hidden | Hidden | Visible | Visible | |
| TC-PERM-040 | Navigation: Companies & Products | Hidden | Products only | Both | Both | |
| TC-PERM-041 | Add Certificate button | Hidden | Hidden | Visible | Visible | |
| TC-PERM-042 | Edit Certificate button | Hidden | Hidden | Visible | Visible | |
| TC-PERM-043 | Delete Certificate button | Hidden | Hidden | Hidden | Visible | |
| TC-PERM-044 | Internal Tags column | Hidden | Hidden | Visible | Visible | |
| TC-PERM-045 | Product Actions column | Hidden | Hidden | Visible | Visible | |
| TC-PERM-046 | Import/Export buttons (Admin pages) | Hidden | Hidden | Hidden | Visible | |

---

## 12. Module 9: Data Export & Import

### 9.1 CSV Export

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-EXP-001 | Export certificates | 1. Navigate to cert view (Analyst+) 2. Click Export | CSV file downloads with certificate data | |
| TC-EXP-002 | Export users | 1. Admin > User Management 2. Click Export | CSV downloads with user data | |
| TC-EXP-003 | Export roles | 1. Admin > Role Management 2. Click Export | CSV downloads with role data | |
| TC-EXP-004 | Export tags | 1. Admin > Tags 2. Click Export | CSV downloads with tag data | |
| TC-EXP-005 | Export partners | 1. Admin > Partners 2. Click Export | CSV downloads with partner data | |
| TC-EXP-006 | Export products | 1. Product Management 2. Click Export | CSV downloads with product data | |
| TC-EXP-007 | Export companies | 1. Company Management 2. Click Export | CSV downloads with company data | |
| TC-EXP-008 | CSV format only | 1. Check any export | File is .csv format (NOT .xlsx, .json, or .pdf) | |
| TC-EXP-009 | Export with filters applied | 1. Apply filters 2. Click Export | Only filtered data exported | |
| TC-EXP-010 | External export own data only | 1. Login as external 2. Export | Only own company's data in export | |

### 9.2 CSV Import

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-IMP-001 | Download template - Users | 1. Admin > User Management 2. Click Template | CSV template with correct column headers | |
| TC-IMP-002 | Download template - Tags | 1. Admin > Tags 2. Click Template | CSV template downloads | |
| TC-IMP-003 | Download template - Companies | 1. Company page 2. Click Template | CSV template downloads | |
| TC-IMP-004 | Import valid CSV | 1. Click Import 2. Upload properly formatted CSV | Data imported successfully | |
| TC-IMP-005 | Import with validation errors | 1. Upload CSV with invalid data (missing required, bad format) | Error report showing which rows failed and why | |
| TC-IMP-006 | Import duplicate records | 1. Upload CSV with data that already exists | Handled gracefully (update or skip with message) | |
| TC-IMP-007 | Partners page - No Import | 1. Go to Partners page | Import button does NOT exist | |
| TC-IMP-008 | Partners page - No Template | 1. Go to Partners page | Template button does NOT exist | |

---

## 13. Module 10: Edge Cases & Negative Testing

### 10.1 Form Validation

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-NEG-001 | Certificate - empty required fields | 1. Create cert with no fields filled 2. Submit | Validation errors for all required fields | |
| TC-NEG-002 | Certificate - duplicate number | 1. Create cert with existing certificate number | Error: Certificate number must be unique | |
| TC-NEG-003 | User - empty email | 1. Create user with no email 2. Submit | Validation error for email | |
| TC-NEG-004 | User - invalid email format | 1. Create user with "notanemail" 2. Submit | Validation error for email format | |
| TC-NEG-005 | User - duplicate email | 1. Create user with existing email | Error: Email must be unique | |
| TC-NEG-006 | Role - empty name | 1. Create role with no name | Validation error | |
| TC-NEG-007 | Tag - empty name | 1. Create tag with no name | Validation error | |
| TC-NEG-008 | Company - empty name | 1. Create company with no name | Validation error | |
| TC-NEG-009 | Product ID - invalid type | 1. Add product ID without selecting type | Validation error | |
| TC-NEG-010 | Product ID - empty value | 1. Add product ID without value | Validation error | |

### 10.2 Notification Pattern Validation

**Critical: NO native browser alert(), confirm(), or prompt() should appear anywhere in the system.**

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-NEG-011 | Delete cert notification | 1. Delete a certificate | AdminCore.showConfirmation() modal BEFORE, AdminCore.showNotification() toast AFTER | |
| TC-NEG-012 | Delete user notification | 1. Delete a user | Confirmation modal BEFORE, toast AFTER | |
| TC-NEG-013 | Delete tag notification | 1. Delete a tag | Confirmation modal BEFORE, toast AFTER | |
| TC-NEG-014 | Delete role notification | 1. Delete a role | Confirmation modal BEFORE, toast AFTER | |
| TC-NEG-015 | Create success notification | 1. Create any entity | Toast notification for success (no modal) | |
| TC-NEG-016 | Edit success notification | 1. Edit any entity | Toast notification for success | |
| TC-NEG-017 | No native alerts anywhere | 1. Use every feature in the system | NEVER see a native browser alert/confirm/prompt dialog | |

### 10.3 Data Boundary Testing

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-NEG-018 | Certification Results max length | 1. Enter 1001+ characters in Certification Results | Truncated or rejected at 1000 chars | |
| TC-NEG-019 | Certificate number format | 1. Verify auto-generated number format | Format: YY-NNNN (e.g., 24-0366) | |
| TC-NEG-020 | Very long company name | 1. Create company with 255+ character name | Handled gracefully (truncated or rejected) | |
| TC-NEG-021 | Special characters in fields | 1. Enter <script>alert('xss')</script> in text fields | HTML encoded, no script execution (XSS prevention) | |
| TC-NEG-022 | SQL injection attempt | 1. Enter ' OR 1=1 -- in search fields | No SQL injection, treated as literal text | |
| TC-NEG-023 | Large CSV import | 1. Import CSV with 1000+ rows | Handled gracefully (may show progress) | |

### 10.4 Browser & Responsiveness

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-NEG-024 | Minimum font size | 1. Inspect any page text | All text >= 16px | |
| TC-NEG-025 | Mobile responsive tile view | 1. Resize browser to mobile width | Tile view adapts, remains usable | |
| TC-NEG-026 | Admin page responsive | 1. Resize admin page to tablet width | Table scrolls or adapts | |

---

---

# PART B: END-TO-END BUSINESS SCENARIOS

These scenarios simulate real-world business journeys that cross multiple roles, pages, and data dependencies. Each scenario must be executed **in sequence** - steps build on each other. Failed steps block subsequent steps in the same scenario.

---

## 14. Module 11: E2E Scenario 1 - Certificate Full Lifecycle

**Business Story**: A new certification is created by an Analyst, reviewed and approved by an Admin, made visible to the public and partners, viewed by an External client, and eventually expires and gets archived.

**Roles Involved**: Admin, Analyst, External, Public, Partner (API)
**Estimated Time**: 30-45 minutes
**Data Created**: 1 certificate through all lifecycle stages

### Pre-conditions
- Admin, Analyst, and External accounts exist
- At least 1 company and 1 product exist
- At least 1 certification type with subcategory exists
- External user is associated with the company being tested
- Partner API key exists for testing

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| 1 | E2E-LC-001 | Analyst | Login as analyst | /Account/Login | Redirected to internal tile view or dashboard | |
| 2 | E2E-LC-002 | Analyst | Click "Create Certificate" | /Certificate/Create | Create form loads, certificate number auto-generated (YY-NNNN) | |
| 3 | E2E-LC-003 | Analyst | Select Company from dropdown | Create form | Company selected, products may filter | |
| 4 | E2E-LC-004 | Analyst | Select Product from dropdown | Create form | Product selected | |
| 5 | E2E-LC-005 | Analyst | Select Certification Type (parent) | Create form | Subcategory dropdown populates | |
| 6 | E2E-LC-006 | Analyst | Select Subcategory | Create form | Custom fields load dynamically if configured | |
| 7 | E2E-LC-007 | Analyst | Fill custom fields (if any appeared) | Create form | Fields accept valid input | |
| 8 | E2E-LC-008 | Analyst | Set Effective Date = today | Create form | Date accepted | |
| 9 | E2E-LC-009 | Analyst | Set Expiration Date = 1 year from today | Create form | Date accepted (after effective date) | |
| 10 | E2E-LC-010 | Analyst | Set Location and Country | Create form | Fields populated | |
| 11 | E2E-LC-011 | Analyst | Select Partner = "Amazon" | Create form | Partner set | |
| 12 | E2E-LC-012 | Analyst | Enter Certification Results = "95.5% recycled content" | Create form | Text accepted in optional field | |
| 13 | E2E-LC-013 | Analyst | Upload PDF certificate (valid < 10MB) | Create form | File preview generated, file name shown, success toast | |
| 14 | E2E-LC-014 | Analyst | Add External Tag (e.g., "Sustainable") | Create form | Tag badge appears | |
| 15 | E2E-LC-015 | Analyst | Add Internal Tag (e.g., "PriorityReview") | Create form | Tag badge appears | |
| 16 | E2E-LC-016 | Analyst | Add Product ID: Type=ASIN, Value=B08X7N9MRQ | Create form | Product ID appears in table, format validated | |
| 17 | E2E-LC-017 | Analyst | Add Product ID: Type=UPC, Value=846378985421 | Create form | Second product ID in table | |
| 18 | E2E-LC-018 | Analyst | Set Status = "Draft" | Create form | Status set to Draft | |
| 19 | E2E-LC-019 | Analyst | Click Save | POST /Certificate/Create | Certificate created, redirected to Details page, success toast. **Record the Certificate Number for remaining steps.** | |
| 20 | E2E-LC-020 | Analyst | Verify Details page shows all entered data | /Certificate/Details/{number} | All fields match: Company, Product, Type, Subcategory, Dates, Location, Partner, Cert Results, PDF preview, Tags (both internal & external), Product IDs (ASIN + UPC) | |
| 21 | E2E-LC-021 | Analyst | Verify Certificate Number displayed (NOT database ID) | Details page | Shows YY-NNNN format in heading and URL | |
| 22 | E2E-LC-022 | Public | Open incognito browser, go to public tile view | /Certificate/CertificateTileView | Page loads | |
| 23 | E2E-LC-023 | Public | Search for the new certificate number | Public tile view | Certificate NOT found (status is Draft, not visible to public) | |
| 24 | E2E-LC-024 | Analyst | Edit the certificate, change Status to "Pending Review" | /Certificate/Edit/{number} | Status updated, save successful | |
| 25 | E2E-LC-025 | Admin | Login as admin | /Account/Login | Dashboard loads | |
| 26 | E2E-LC-026 | Admin | Navigate to internal tile view, find the certificate | /Certificate/CertificateTileViewInternal | Certificate visible with "Pending Review" status badge | |
| 27 | E2E-LC-027 | Admin | Edit the certificate, change Status to "Under Review" | /Certificate/Edit/{number} | Status = Under Review | |
| 28 | E2E-LC-028 | Admin | Edit again, change Status to "Approved" | /Certificate/Edit/{number} | Status = Approved | |
| 29 | E2E-LC-029 | Admin | Edit again, change Status to "Active" | /Certificate/Edit/{number} | Status = Active. This is the key transition - certificate is now LIVE. | |
| 30 | E2E-LC-030 | Public | Refresh public tile view, search for certificate | /Certificate/CertificateTileView | Certificate NOW appears (status = Active) | |
| 31 | E2E-LC-031 | Public | Click on the certificate tile | /Certificate/CertificateSingleView/{number} | Detail view shows: Certificate Number, Company, Product, Dates, Type, Location. External tags visible ("Sustainable"). Internal tags NOT visible. No Edit/Delete buttons. | |
| 32 | E2E-LC-032 | Public | Click Download PDF | Details page | PDF file downloads successfully | |
| 33 | E2E-LC-033 | Public | Verify Certificate Number in URL (not ID) | Browser URL bar | URL shows /Certificate/CertificateSingleView/XX-XXXX | |
| 34 | E2E-LC-034 | External | Login as external user (associated with same company) | /Account/Login | External tile view loads | |
| 35 | E2E-LC-035 | External | Find the certificate in external tile view | /Certificate/CertificateTileViewExternal | Certificate visible (same company). External tags shown. Internal tags ("PriorityReview") NOT shown. No Edit/Delete buttons. | |
| 36 | E2E-LC-036 | External | Click on certificate for details | External detail view | Read-only view. Certification Results field visible. No Edit/Delete buttons. | |
| 37 | E2E-LC-037 | Partner API | GET /api/v1/certificates?includeProductIds=true (with X-API-Key) | API endpoint | Certificate appears in response with: certificateNumber, status=Active, company, certificationResults="95.5% recycled content", productIds array with ASIN and UPC entries | |
| 38 | E2E-LC-038 | Partner API | GET /api/v1/certificates/search/productid?productId=B08X7N9MRQ&type=ASIN | API endpoint | Certificate returned by ASIN search | |
| 39 | E2E-LC-039 | Partner API | GET /api/v1/certificates/{number}/download | API endpoint | PDF file returned | |
| 40 | E2E-LC-040 | Admin | Edit certificate, change Status to "Suspended" | /Certificate/Edit/{number} | Status = Suspended | |
| 41 | E2E-LC-041 | Public | Refresh public tile view, search for certificate | /Certificate/CertificateTileView | Certificate NO LONGER visible (Suspended not public) | |
| 42 | E2E-LC-042 | Admin | Edit certificate, change Status back to "Active" | /Certificate/Edit/{number} | Status = Active, certificate reappears in public view | |
| 43 | E2E-LC-043 | Admin | Edit certificate, change Status to "Expired" | /Certificate/Edit/{number} | Status = Expired | |
| 44 | E2E-LC-044 | Admin | Edit certificate, change Status to "Archived" | /Certificate/Edit/{number} | Status = Archived (FINAL state) | |
| 45 | E2E-LC-045 | Admin | Try to edit Archived certificate status | /Certificate/Edit/{number} | No status transitions available. Status dropdown empty or disabled. Certificate is permanently archived. | |
| 46 | E2E-LC-046 | Public | Search for archived certificate | /Certificate/CertificateTileView | Certificate NOT visible (Archived not public) | |

### Verification Checklist
- [ ] Certificate created with all fields populated
- [ ] Workflow progressed: Draft → Pending Review → Under Review → Approved → Active
- [ ] Public visibility activated only when Active
- [ ] External user saw only external tags, not internal
- [ ] Partner API returned certificate with product IDs and certification results
- [ ] Suspension hid certificate from public, reactivation restored it
- [ ] Archived certificate reached final state with no further transitions
- [ ] Certificate number (not ID) used throughout all views and URLs

---

## 15. Module 12: E2E Scenario 2 - New Company Onboarding

**Business Story**: A new client company is set up in the system. Products are created under the company. An external user account is created and associated with the company. The external user logs in and can only see their company's data.

**Roles Involved**: Admin, External (new user)
**Estimated Time**: 20-30 minutes
**Data Created**: 1 company, 2 products, 1 external user, 1 certificate

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| 1 | E2E-CO-001 | Admin | Login as admin | /Account/Login | Dashboard loads | |
| 2 | E2E-CO-002 | Admin | Navigate to Company Management | /Company/Index | Company list loads | |
| 3 | E2E-CO-003 | Admin | Click Add New Company | Company form | Add form/modal opens | |
| 4 | E2E-CO-004 | Admin | Enter: Name="TestCo UAT", Location="Portland, OR", Country="United States", Email="test@testco.com", Website="testco.com" | Company form | Fields accepted | |
| 5 | E2E-CO-005 | Admin | Save company | Company form | Company created, appears in list, success toast | |
| 6 | E2E-CO-006 | Admin | Navigate to Product Management | /Product/Management | Product list loads | |
| 7 | E2E-CO-007 | Admin | Click Add New Product | Product form | Add form opens | |
| 8 | E2E-CO-008 | Admin | Enter: Name="EcoWidget Pro", Company="TestCo UAT", Category, Type, Subcategory | Product form | Cascading dropdowns work, fields populated | |
| 9 | E2E-CO-009 | Admin | Save first product | Product form | Product created, success toast | |
| 10 | E2E-CO-010 | Admin | Create second product: Name="EcoWidget Lite", same company | Product form | Second product created | |
| 11 | E2E-CO-011 | Admin | Navigate to User Management | /Admin/UserManagement | User list loads | |
| 12 | E2E-CO-012 | Admin | Click Add New User | User form | Add form opens | |
| 13 | E2E-CO-013 | Admin | Enter: Name="TestCo User", Email="uat-testco@example.com", Role="External", Company="TestCo UAT", Password="UATTest123!" | User form | Fields accepted, External role selected shows Company field | |
| 14 | E2E-CO-014 | Admin | Save user | User form | User created, success toast | |
| 15 | E2E-CO-015 | Admin | Create a certificate for TestCo UAT: Company="TestCo UAT", Product="EcoWidget Pro", Status=Active | /Certificate/Create | Certificate created and saved as Active | |
| 16 | E2E-CO-016 | Admin | Create a second certificate for a DIFFERENT company, Status=Active | /Certificate/Create | Second certificate from different company | |
| 17 | E2E-CO-017 | Admin | Logout | Logout | Session ended | |
| 18 | E2E-CO-018 | External (new) | Login as "uat-testco@example.com" / "UATTest123!" | /Account/Login | Redirected to External Tile View (or forced password reset first) | |
| 19 | E2E-CO-019 | External (new) | View certificate tile list | /Certificate/CertificateTileViewExternal | ONLY the TestCo UAT certificate visible. The other company's cert is NOT shown. | |
| 20 | E2E-CO-020 | External (new) | Search for the other company's certificate by number | Search bar | No results. Cannot see other company's data. | |
| 21 | E2E-CO-021 | External (new) | Navigate to Products | /Product/Management | ONLY "EcoWidget Pro" and "EcoWidget Lite" visible. No other company products. | |
| 22 | E2E-CO-022 | External (new) | Verify no Actions column on products | Product page | No Edit/Delete buttons. Read-only view. | |
| 23 | E2E-CO-023 | External (new) | Verify no Admin menu items | Navigation | No "Administration" menu. Only Certificates and Products in nav. | |
| 24 | E2E-CO-024 | External (new) | Try to navigate directly to /Admin/UserManagement | Browser URL bar | Access denied / redirect | |
| 25 | E2E-CO-025 | External (new) | Try to navigate directly to /Certificate/Create | Browser URL bar | Access denied / redirect | |
| 26 | E2E-CO-026 | External (new) | Export certificates | Export button | CSV contains ONLY TestCo UAT certificates | |

### Verification Checklist
- [ ] Company created with all fields
- [ ] Products created and linked to company
- [ ] External user created and associated with company
- [ ] External user sees ONLY their company's data (certificates, products)
- [ ] External user cannot access admin pages, create/edit certs, or see other companies
- [ ] Data isolation is complete and verified

---

## 16. Module 13: E2E Scenario 3 - Partner API Integration

**Business Story**: An admin configures a new partner (Amazon), generates an API key, and the partner uses the API to discover certificates, search by product ID, and download certificate PDFs. Rate limiting is verified.

**Roles Involved**: Admin, Partner (API consumer)
**Estimated Time**: 20-30 minutes
**Tool Required**: curl, PowerShell, or Postman

### Pre-conditions
- At least 3 certificates exist with Partner="Amazon" and Status=Active
- At least 1 certificate has ASIN product identifiers

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| 1 | E2E-API-001 | Admin | Login and navigate to Partner API Management | /Admin/PartnerAPI | API management page loads | |
| 2 | E2E-API-002 | Admin | Generate new API key for "Amazon" partner | API management | Key generated, displayed once. **Copy and save the key for remaining steps.** | |
| 3 | E2E-API-003 | Partner | Test authentication: GET /api/v1/certificates with X-API-Key header | API | 200 OK with JSON response | |
| 4 | E2E-API-004 | Partner | Test NO authentication: GET /api/v1/certificates without key | API | 401 Unauthorized | |
| 5 | E2E-API-005 | Partner | List certificates page 1: GET /api/v1/certificates?pageNumber=1&pageSize=5 | API | Response includes: pagination.totalCount, pagination.pageSize=5, data array with <=5 certs | |
| 6 | E2E-API-006 | Partner | Verify pagination: check hasNext=true, request page 2 | API | Page 2 returns next set, hasPrevious=true | |
| 7 | E2E-API-007 | Partner | Get single certificate by number: GET /api/v1/certificates/number/{number} | API | Single certificate object with all fields | |
| 8 | E2E-API-008 | Partner | Verify response fields: certificateNumber, status, issueDate, expirationDate, company, productName, certificationType, certificationTypeSubcategory, issuer, certificationResults, partner | API | All fields present. issuer = "Green Circle Certified". certificationResults populated. | |
| 9 | E2E-API-009 | Partner | Request with product IDs: GET /api/v1/certificates?includeProductIds=true | API | productIds array populated with type, value, status, partner, productName, productSku | |
| 10 | E2E-API-010 | Partner | Search by ASIN: GET /api/v1/certificates/search/productid?productId={ASIN}&type=ASIN | API | Certificate(s) with matching ASIN returned | |
| 11 | E2E-API-011 | Partner | Search by UPC: GET /api/v1/certificates/search/productid?productId={UPC}&type=UPC | API | Certificate(s) with matching UPC returned | |
| 12 | E2E-API-012 | Partner | Filter by status: GET /api/v1/certificates?status=Active | API | Only Active certificates returned | |
| 13 | E2E-API-013 | Partner | Download PDF: GET /api/v1/certificates/{number}/download | API | PDF file returned with correct content-type | |
| 14 | E2E-API-014 | Partner | Request non-existent certificate: GET /api/v1/certificates/99-9999 | API | 404 Not Found with error message | |
| 15 | E2E-API-015 | Partner | Verify date format in responses | API | All dates in ISO 8601: YYYY-MM-DDTHH:mm:ssZ | |
| 16 | E2E-API-016 | Partner | Send rapid requests (exceed rate limit) | API | Eventually returns 429 Too Many Requests with rate limit headers | |
| 17 | E2E-API-017 | Admin | Revoke the API key | /Admin/PartnerAPI | Key marked as revoked | |
| 18 | E2E-API-018 | Partner | Attempt request with revoked key | API | 401 Unauthorized (key no longer valid) | |

### Verification Checklist
- [ ] API key generation and authentication works
- [ ] All certificate fields returned correctly including certificationResults
- [ ] Product ID search by ASIN and UPC works
- [ ] PDF download via API works
- [ ] Pagination metadata accurate
- [ ] Rate limiting enforced
- [ ] Revoked key properly rejected

---

## 17. Module 14: E2E Scenario 4 - Admin System Setup (Day 1)

**Business Story**: A brand-new system is being configured. Admin sets up all master data in the correct dependency order, then an Analyst creates a certificate using all the configured data. This validates the entire admin setup feeds correctly into certificate creation.

**Roles Involved**: Admin, Analyst
**Estimated Time**: 30-40 minutes
**Purpose**: Validates data dependency chain

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| **Phase 1: Master Data Setup (Admin)** | | | | | | |
| 1 | E2E-SETUP-001 | Admin | Login as admin | /Account/Login | Dashboard loads | |
| 2 | E2E-SETUP-002 | Admin | Create Certification Type: Parent="Environmental Compliance" | /Admin/CertificateTypes | Parent type created | |
| 3 | E2E-SETUP-003 | Admin | Create Subcategory under parent: "Water Conservation Level 1" | /Admin/CertificateTypes | Child type created under parent | |
| 4 | E2E-SETUP-004 | Admin | Create Subcategory: "Water Conservation Level 2" | /Admin/CertificateTypes | Second child type created | |
| 5 | E2E-SETUP-005 | Admin | Set AmazonCode on parent cert type (for API mapping) | /Admin/CertificateTypes | AmazonCode saved | |
| 6 | E2E-SETUP-006 | Admin | Create Custom Field: Name="Testing Lab", Type=Text, Required, associated with "Environmental Compliance" | /Admin/CertificateFields | Custom field created and linked to cert type | |
| 7 | E2E-SETUP-007 | Admin | Create Custom Field: Name="Compliance Score", Type=Number, Optional, associated with "Environmental Compliance" | /Admin/CertificateFields | Second custom field created | |
| 8 | E2E-SETUP-008 | Admin | Create External Tag: "Water Positive" | /Admin/Tags | External tag created | |
| 9 | E2E-SETUP-009 | Admin | Create Internal Tag: "FastTrack" | /Admin/Tags | Internal tag created | |
| 10 | E2E-SETUP-010 | Admin | Create Partner: "WaterMark Inc" (or verify Amazon/Novi exist) | /Admin/Partners | Partner created/verified | |
| 11 | E2E-SETUP-011 | Admin | Create Product Type hierarchy: Category="Industrial" > Type="Water Treatment" > Sub="Filtration Systems" | /Admin/ProductTypes | Three-level hierarchy created | |
| 12 | E2E-SETUP-012 | Admin | Create Company: "AquaTech Solutions" | /Company/Index | Company created | |
| 13 | E2E-SETUP-013 | Admin | Create Product: "AquaFilter Pro", Company="AquaTech Solutions", Category="Industrial" | /Product/Management | Product created and linked | |
| 14 | E2E-SETUP-014 | Admin | Logout | Logout | Session ended | |
| **Phase 2: Certificate Creation Using Setup Data (Analyst)** | | | | | | |
| 15 | E2E-SETUP-015 | Analyst | Login as analyst | /Account/Login | Internal view loads | |
| 16 | E2E-SETUP-016 | Analyst | Navigate to Create Certificate | /Certificate/Create | Form loads | |
| 17 | E2E-SETUP-017 | Analyst | Select Company = "AquaTech Solutions" | Create form | Company available in dropdown | |
| 18 | E2E-SETUP-018 | Analyst | Select Product = "AquaFilter Pro" | Create form | Product available (linked to company) | |
| 19 | E2E-SETUP-019 | Analyst | Select Certification Type = "Environmental Compliance" | Create form | Subcategory dropdown populates with "Water Conservation Level 1" and "Level 2" | |
| 20 | E2E-SETUP-020 | Analyst | Select Subcategory = "Water Conservation Level 1" | Create form | Custom fields appear: "Testing Lab" (required text) and "Compliance Score" (optional number) | |
| 21 | E2E-SETUP-021 | Analyst | Fill "Testing Lab" = "National Water Testing Institute" | Create form | Required field accepts input | |
| 22 | E2E-SETUP-022 | Analyst | Fill "Compliance Score" = "94.7" | Create form | Optional number field accepts input | |
| 23 | E2E-SETUP-023 | Analyst | Try to Save WITHOUT filling "Testing Lab" (clear it) | Create form | Validation error: "Testing Lab is required" (custom field validation works) | |
| 24 | E2E-SETUP-024 | Analyst | Fill "Testing Lab" back in. Add External Tag "Water Positive" | Create form | Tag from admin setup appears in dropdown and selected | |
| 25 | E2E-SETUP-025 | Analyst | Add Internal Tag "FastTrack" | Create form | Internal tag available and selected | |
| 26 | E2E-SETUP-026 | Analyst | Set dates, location, partner. Click Save. | Create form | Certificate created with ALL admin-configured data | |
| 27 | E2E-SETUP-027 | Analyst | View Details page | /Certificate/Details/{number} | Verify: Cert Type="Environmental Compliance", Subcategory="Water Conservation Level 1", Custom fields shown with values, Tags shown, Company/Product correct | |

### Verification Checklist
- [ ] Certification type hierarchy (parent/child) created and usable in cert form
- [ ] Custom fields loaded dynamically when correct cert type selected
- [ ] Required custom field validation enforced
- [ ] Tags (internal and external) created in admin appear in cert form dropdowns
- [ ] Product type hierarchy created and products linked correctly
- [ ] Company → Product → Certificate chain works end-to-end
- [ ] All admin-configured data correctly flows into certificate creation

---

## 18. Module 15: E2E Scenario 5 - Certificate Renewal Lifecycle

**Business Story**: An active certificate approaches its expiration date. The system automatically transitions it to Pending Renewal. An Analyst submits it for review, it gets renewed, and the certificate returns to Active status.

**Roles Involved**: Admin, Analyst, System (automated)
**Estimated Time**: 20 minutes (manual) or depends on background job schedule

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| 1 | E2E-REN-001 | Admin | Create certificate with Status=Active, Expiration Date = today + 25 days | /Certificate/Create | Certificate created as Active with near-future expiration | |
| 2 | E2E-REN-002 | System | Trigger expiration check (background job or manual trigger) | Background service | Certificate auto-transitions from Active → Pending Renewal (within 30-day window) | |
| 3 | E2E-REN-003 | Analyst | View certificate in internal tile view | /Certificate/CertificateTileViewInternal | Certificate shows "Pending Renewal" status badge | |
| 4 | E2E-REN-004 | Analyst | Verify "Expiring Soon" indicator on tile | Tile view | Visual indicator showing cert is expiring soon | |
| 5 | E2E-REN-005 | Analyst | Edit certificate: Change status to "Under Review" | /Certificate/Edit/{number} | Status = Under Review (renewal review in progress) | |
| 6 | E2E-REN-006 | Admin | Review and change status to "Renewed" | /Certificate/Edit/{number} | Status = Renewed | |
| 7 | E2E-REN-007 | Admin | Change status from Renewed to "Active" | /Certificate/Edit/{number} | Status = Active (certificate is now renewed and active) | |
| 8 | E2E-REN-008 | Admin | Verify the expiration date was updated (if applicable) or note it | Details page | Expiration date should be updated for renewed cert | |
| 9 | E2E-REN-009 | Public | Verify certificate visible in public view | /Certificate/CertificateTileView | Certificate appears as Active | |
| 10 | E2E-REN-010 | Admin | Check status history / audit trail | Admin or Details page | Full history: Active → Pending Renewal (system) → Under Review → Renewed → Active | |

### Alternative Path: Expiration Without Renewal

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| 11 | E2E-REN-011 | Admin | Create certificate with Status=Active, Expiration Date = yesterday | /Certificate/Create | Certificate created | |
| 12 | E2E-REN-012 | System | Trigger expiration check | Background service | Certificate auto-transitions from Active → Expired | |
| 13 | E2E-REN-013 | Public | Search for expired certificate | /Certificate/CertificateTileView | Certificate NOT visible (Expired not public) | |
| 14 | E2E-REN-014 | Admin | Archive expired certificate | /Certificate/Edit/{number} | Status = Archived (final state) | |

### Verification Checklist
- [ ] Automated Pending Renewal transition triggered within 30-day window
- [ ] Renewal workflow: Pending Renewal → Under Review → Renewed → Active
- [ ] Expired certificates auto-transition correctly
- [ ] Audit trail captures automated system transitions
- [ ] Public visibility matches certificate status

---

## 19. Module 16: E2E Scenario 6 - Rejection & Recovery Flow

**Business Story**: An Analyst creates a certificate that gets rejected during review. The Analyst corrects issues and resubmits. The certificate eventually gets approved and activated.

**Roles Involved**: Analyst, Admin
**Estimated Time**: 15-20 minutes

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| 1 | E2E-REJ-001 | Analyst | Create certificate with intentionally incomplete data (e.g., missing PDF) | /Certificate/Create | Certificate created in Draft status | |
| 2 | E2E-REJ-002 | Analyst | Change status to Pending Review | /Certificate/Edit/{number} | Status = Pending Review | |
| 3 | E2E-REJ-003 | Admin | Review certificate, identify issues | /Certificate/Details/{number} | Admin sees the certificate is incomplete | |
| 4 | E2E-REJ-004 | Admin | Change status from Pending Review → Under Review | /Certificate/Edit/{number} | Status = Under Review | |
| 5 | E2E-REJ-005 | Admin | Reject: Change status to "Rejected" | /Certificate/Edit/{number} | Status = Rejected | |
| 6 | E2E-REJ-006 | Analyst | View rejected certificate in internal tile view | Tile view | Certificate shows "Rejected" status | |
| 7 | E2E-REJ-007 | Analyst | Verify available transitions from Rejected | /Certificate/Edit/{number} | Can go to: Draft or Archived (NOT Active or other states) | |
| 8 | E2E-REJ-008 | Analyst | Change status back to "Draft" | /Certificate/Edit/{number} | Status = Draft (restart workflow) | |
| 9 | E2E-REJ-009 | Analyst | Fix the certificate: Upload PDF, add missing data | /Certificate/Edit/{number} | Certificate now complete with all required data | |
| 10 | E2E-REJ-010 | Analyst | Resubmit: Change status to "Pending Review" | /Certificate/Edit/{number} | Status = Pending Review (second attempt) | |
| 11 | E2E-REJ-011 | Admin | Review again: Pending Review → Under Review → Approved | /Certificate/Edit/{number} | Status = Approved | |
| 12 | E2E-REJ-012 | Admin | Activate: Approved → Active | /Certificate/Edit/{number} | Status = Active. Certificate is now live. | |
| 13 | E2E-REJ-013 | Public | Verify certificate now appears in public view | /Certificate/CertificateTileView | Certificate visible | |
| 14 | E2E-REJ-014 | Admin | Check full audit trail | Details or Admin | History: Draft → PendingReview → UnderReview → Rejected → Draft → PendingReview → UnderReview → Approved → Active | |

### Verification Checklist
- [ ] Rejected status available only from Under Review
- [ ] Rejected can return to Draft (not directly to Active)
- [ ] Analyst can fix and resubmit after rejection
- [ ] Full audit trail preserved across rejection cycle
- [ ] Certificate eventually reaches Active and is publicly visible

---

## 20. Module 17: E2E Scenario 7 - Bulk Operations Workflow

**Business Story**: Admin downloads import templates, prepares bulk data, imports via CSV, previews changes, applies the import, then exports the data to verify. Covers the full template → import → verify → export cycle.

**Roles Involved**: Admin
**Estimated Time**: 25-35 minutes
**Files Needed**: CSV editor (Excel, Google Sheets, or text editor)

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| **Phase 1: Template Download** | | | | | | |
| 1 | E2E-BULK-001 | Admin | Navigate to User Management, click Template | /Admin/UserManagement | CSV template downloads with headers: Name, Email, Role, Password, CompanyIds | |
| 2 | E2E-BULK-002 | Admin | Navigate to Tags, click Template | /Admin/Tags | Tag import template downloads | |
| 3 | E2E-BULK-003 | Admin | Navigate to Companies, click Template | /Company/Index | Company import template downloads | |
| 4 | E2E-BULK-004 | Admin | Navigate to Certificate Import, click Template | /CertificateImport/Index | Certificate import template downloads with correct column headers | |
| 5 | E2E-BULK-005 | Admin | Verify all templates are CSV format (not xlsx) | Downloaded files | All files are .csv format | |
| **Phase 2: Prepare & Import Companies** | | | | | | |
| 6 | E2E-BULK-006 | Admin | Fill company template with 3 test companies | CSV editor | Data prepared: BulkCo-A, BulkCo-B, BulkCo-C | |
| 7 | E2E-BULK-007 | Admin | Import companies CSV | /Company/Index → Import | Companies imported successfully, toast notification | |
| 8 | E2E-BULK-008 | Admin | Verify all 3 companies in list | /Company/Index | BulkCo-A, BulkCo-B, BulkCo-C all visible | |
| **Phase 3: Import Tags** | | | | | | |
| 9 | E2E-BULK-009 | Admin | Fill tag template: 2 external + 1 internal tags | CSV editor | Tags prepared | |
| 10 | E2E-BULK-010 | Admin | Import tags CSV | /Admin/Tags → Import | Tags imported | |
| 11 | E2E-BULK-011 | Admin | Filter by type to verify | /Admin/Tags | External and internal tags correctly typed | |
| **Phase 4: Bulk Certificate Import** | | | | | | |
| 12 | E2E-BULK-012 | Admin | Fill certificate template with 5 certificates across the 3 companies, including product IDs and tags | CSV editor | CSV prepared with: CERTIFICATE_NUMBER, COMPANY_NAME, PRODUCT_NAME, CERTIFICATION_TYPE, dates, ASIN, UPC columns | |
| 13 | E2E-BULK-013 | Admin | Upload certificate CSV | /CertificateImport/Index | File accepted, redirected to preview | |
| 14 | E2E-BULK-014 | Admin | Review import preview | /CertificateImport/Preview | Preview shows: X new, Y modified, Z errors. Each row has validation status. | |
| 15 | E2E-BULK-015 | Admin | Verify auto-created entities | Preview page | If products didn't exist, preview shows they will be auto-created | |
| 16 | E2E-BULK-016 | Admin | Click Apply | Preview page | Import executed. Success notification with counts. | |
| 17 | E2E-BULK-017 | Admin | Navigate to Internal Tile View | /Certificate/CertificateTileViewInternal | All 5 imported certificates visible | |
| **Phase 5: Export & Verify** | | | | | | |
| 18 | E2E-BULK-018 | Admin | Export certificates from tile view | Export button | CSV downloads | |
| 19 | E2E-BULK-019 | Admin | Open exported CSV, verify imported data present | CSV editor | All 5 certificates in export with correct company, product, type, dates | |
| 20 | E2E-BULK-020 | Admin | Export companies | /Company/Index → Export | All 3 bulk companies in export | |
| 21 | E2E-BULK-021 | Admin | Export tags | /Admin/Tags → Export | Bulk-imported tags in export | |
| **Phase 6: Negative Import Testing** | | | | | | |
| 22 | E2E-BULK-022 | Admin | Upload CSV with duplicate certificate numbers (existing in system) | /CertificateImport/Index | Preview shows "Modified" or "Duplicate" status, not error crash | |
| 23 | E2E-BULK-023 | Admin | Upload CSV with invalid data (bad dates, missing required) | /CertificateImport/Index | Preview shows error rows with specific error messages per row | |
| 24 | E2E-BULK-024 | Admin | Cancel import of invalid data | Preview page | No changes applied, return to import page | |

### Verification Checklist
- [ ] Templates download as CSV format for all entity types
- [ ] Companies imported successfully via CSV
- [ ] Tags imported with correct type (internal/external)
- [ ] Certificate bulk import with preview before applying
- [ ] Auto-creation of missing companies/products during cert import
- [ ] Export data matches imported data (round-trip verification)
- [ ] Invalid data handled gracefully with row-level error messages
- [ ] Cancel import makes no changes

---

## 21. Module 18: E2E Scenario 8 - User Access Lifecycle

**Business Story**: An admin creates a new Analyst user account. The user logs in for the first time, may be required to reset password, sets up 2FA, works normally, and eventually the admin disables the account.

**Roles Involved**: Admin, Analyst (new user)
**Estimated Time**: 20 minutes

### Scenario Steps

| Step | ID | Role | Action | Page/Endpoint | Expected Result | Status |
|------|-----|------|--------|---------------|-----------------|--------|
| **Phase 1: Account Creation (Admin)** | | | | | | |
| 1 | E2E-USER-001 | Admin | Login as admin | /Account/Login | Dashboard loads | |
| 2 | E2E-USER-002 | Admin | Navigate to User Management | /Admin/UserManagement | User list loads | |
| 3 | E2E-USER-003 | Admin | Create user: Name="UAT Analyst", Email="uat-analyst@example.com", Role="Analyst", Password="Initial123!" | User form | User created, success toast | |
| 4 | E2E-USER-004 | Admin | Verify user appears in user list with Analyst role | User list | User visible with correct role | |
| 5 | E2E-USER-005 | Admin | Logout | Logout | Session ended | |
| **Phase 2: First Login (New Analyst)** | | | | | | |
| 6 | E2E-USER-006 | Analyst (new) | Login as uat-analyst@example.com / Initial123! | /Account/Login | Login succeeds (may redirect to force password reset if flag set) | |
| 7 | E2E-USER-007 | Analyst (new) | If force password reset: Enter new password "NewSecure456!" | /Account/ForcePasswordReset | Password changed, redirected to main app | |
| 8 | E2E-USER-008 | Analyst (new) | If 2FA required: Complete 2FA setup | /Account/TwoFactorPreferences | 2FA configured | |
| 9 | E2E-USER-009 | Analyst (new) | Verify landing on correct page (Dashboard or Internal Tile View) | Dashboard or Tile View | Analyst-appropriate view loads | |
| 10 | E2E-USER-010 | Analyst (new) | Verify navigation menu shows Analyst items | Navigation | See: Certificates, Dashboard, Companies & Products. NOT see: Administration | |
| **Phase 3: Normal Analyst Operations** | | | | | | |
| 11 | E2E-USER-011 | Analyst (new) | Create a certificate | /Certificate/Create | Can create certificates | |
| 12 | E2E-USER-012 | Analyst (new) | Edit the certificate | /Certificate/Edit/{number} | Can edit certificates | |
| 13 | E2E-USER-013 | Analyst (new) | Try to delete a certificate | Tile view / Details | NO delete button visible (Analyst cannot delete) | |
| 14 | E2E-USER-014 | Analyst (new) | View Dashboard statistics | /Dashboard/Index | Dashboard loads with certificate stats | |
| 15 | E2E-USER-015 | Analyst (new) | Access Company Management | /Company/Index | Company list loads | |
| 16 | E2E-USER-016 | Analyst (new) | Try to access /Admin/UserManagement | Browser URL | Access denied | |
| 17 | E2E-USER-017 | Analyst (new) | Logout | Logout | Session ended | |
| **Phase 4: Account Modification (Admin)** | | | | | | |
| 18 | E2E-USER-018 | Admin | Login as admin | /Account/Login | Dashboard loads | |
| 19 | E2E-USER-019 | Admin | Edit user: Change role from Analyst to External | /Admin/UserManagement | Role changed, success toast | |
| 20 | E2E-USER-020 | Analyst→External | Login as uat-analyst@example.com | /Account/Login | Redirected to External Tile View (NOT Dashboard) | |
| 21 | E2E-USER-021 | Now External | Verify navigation reflects External role | Navigation | Only Certificates and Products. No Dashboard, no Admin. | |
| 22 | E2E-USER-022 | Now External | Try to create a certificate | /Certificate/Create | Access denied (External cannot create) | |
| 23 | E2E-USER-023 | Now External | Logout | Logout | Session ended | |
| **Phase 5: Account Deactivation (Admin)** | | | | | | |
| 24 | E2E-USER-024 | Admin | Login, navigate to User Management | /Admin/UserManagement | User list loads | |
| 25 | E2E-USER-025 | Admin | Deactivate user (set inactive or delete) | User form | User deactivated/deleted | |
| 26 | E2E-USER-026 | Deactivated | Try to login as deactivated user | /Account/Login | Login fails with appropriate message | |

### Verification Checklist
- [ ] User creation with role assignment works
- [ ] First login with force password reset works
- [ ] 2FA setup flow completes
- [ ] Analyst permissions correct (create/edit but not delete certs)
- [ ] Role change takes effect on next login (Analyst → External)
- [ ] External permissions enforced after role change
- [ ] Deactivated user cannot login

---

## 22. Module 19: E2E Scenario 9 - Search & Discovery Cross-Role

**Business Story**: The same set of certificates is viewed from all 4 perspectives (Public, External, Analyst, Admin) to verify each role sees the correct data, filters, and UI elements. This is the ultimate cross-role verification.

**Roles Involved**: Public, External, Analyst, Admin
**Estimated Time**: 25-30 minutes

### Pre-conditions
- Certificate "A" exists: Status=Active, Partner=Amazon, Company=TestCo, ExternalTag="Sustainable", InternalTag="Priority", has ASIN product ID
- Certificate "B" exists: Status=Active, Partner=None, Company=OtherCo, ExternalTag="Verified"
- Certificate "C" exists: Status=Draft, Company=TestCo
- Certificate "D" exists: Status=Archived, Company=TestCo
- External user is associated with "TestCo"

### Scenario Steps

| Step | ID | Role | Action | Expected Result | Status |
|------|-----|------|--------|-----------------|--------|
| **Public View** | | | | | |
| 1 | E2E-SEARCH-001 | Public | Navigate to /Certificate/CertificateTileView | Cert A and B visible. Cert C (Draft) and D (Archived) NOT visible. | |
| 2 | E2E-SEARCH-002 | Public | Check filter panel | External tags shown ("Sustainable", "Verified"). NO internal tags ("Priority"). | |
| 3 | E2E-SEARCH-003 | Public | Filter by tag "Sustainable" | Only Cert A shown | |
| 4 | E2E-SEARCH-004 | Public | Clear filter, search by ASIN | Cert A found via autocomplete | |
| 5 | E2E-SEARCH-005 | Public | Check Cert A tile details | No Edit/Delete buttons. No internal tags visible. Certificate Number shown (not ID). | |
| 6 | E2E-SEARCH-006 | Public | Copy shareable URL with "Sustainable" filter | URL contains search parameters | |
| 7 | E2E-SEARCH-007 | Public | Open shareable URL in new incognito window | Same filtered results displayed | |
| **External View (TestCo user)** | | | | | |
| 8 | E2E-SEARCH-008 | External | Login as external (TestCo) | External tile view loads | |
| 9 | E2E-SEARCH-009 | External | View certificate list | Cert A visible (TestCo, Active). Cert B NOT visible (OtherCo). Cert C NOT visible (Draft). Cert D NOT visible (Archived). | |
| 10 | E2E-SEARCH-010 | External | Check filter panel | External tags only. No internal tags. Company filter may be hidden (only one company). | |
| 11 | E2E-SEARCH-011 | External | Search for Cert B by number | No results (different company) | |
| 12 | E2E-SEARCH-012 | External | View Cert A details | External tags visible ("Sustainable"). Internal tags ("Priority") NOT visible. No Edit/Delete. | |
| 13 | E2E-SEARCH-013 | External | Check Products page | Only TestCo products. No OtherCo products. No Actions column. | |
| **Analyst View** | | | | | |
| 14 | E2E-SEARCH-014 | Analyst | Login as analyst | Internal tile view loads | |
| 15 | E2E-SEARCH-015 | Analyst | View certificate list | ALL 4 certificates visible (A, B, C, D) across ALL statuses and companies | |
| 16 | E2E-SEARCH-016 | Analyst | Check filter panel | Both external AND internal tags in filter panel. All companies. All statuses. | |
| 17 | E2E-SEARCH-017 | Analyst | Filter by internal tag "Priority" | Only Cert A shown | |
| 18 | E2E-SEARCH-018 | Analyst | Filter by status "Draft" | Only Cert C shown | |
| 19 | E2E-SEARCH-019 | Analyst | View Cert A details | ALL data visible: external tags, internal tags, all fields. Edit button present. No Delete button. | |
| 20 | E2E-SEARCH-020 | Analyst | Click Edit on Cert A | Edit page loads with all data pre-populated | |
| **Admin View** | | | | | |
| 21 | E2E-SEARCH-021 | Admin | Login as admin | Dashboard or internal tile view | |
| 22 | E2E-SEARCH-022 | Admin | View certificate list | ALL 4 certificates visible, all statuses, all companies | |
| 23 | E2E-SEARCH-023 | Admin | View Cert A details | ALL data visible. Edit AND Delete buttons present. | |
| 24 | E2E-SEARCH-024 | Admin | Filter by status "Archived" | Only Cert D shown | |
| 25 | E2E-SEARCH-025 | Admin | Check status dropdown options on Edit for Cert C (Draft) | All valid transitions from Draft available (Pending Review, Cancelled) | |
| 26 | E2E-SEARCH-026 | Admin | Check status dropdown for Cert D (Archived) | No transitions available (final state) | |

### Cross-Role Comparison Summary

| Aspect | Public | External (TestCo) | Analyst | Admin |
|--------|--------|-------------------|---------|-------|
| **Cert A (Active, TestCo)** | Visible | Visible | Visible | Visible |
| **Cert B (Active, OtherCo)** | Visible | **NOT visible** | Visible | Visible |
| **Cert C (Draft, TestCo)** | **NOT visible** | **NOT visible** | Visible | Visible |
| **Cert D (Archived, TestCo)** | **NOT visible** | **NOT visible** | Visible | Visible |
| **Internal tags shown** | No | No | Yes | Yes |
| **External tags shown** | Yes | Yes | Yes | Yes |
| **Edit button** | No | No | Yes | Yes |
| **Delete button** | No | No | No | Yes |
| **Filter by internal tag** | No | No | Yes | Yes |
| **Filter by all companies** | Yes | No (own only) | Yes | Yes |
| **Filter by all statuses** | No (Active only) | No (Active only) | Yes (all 14) | Yes (all 14) |

### Verification Checklist
- [ ] Public sees only Active certificates, no internal tags, no edit buttons
- [ ] External sees only own company's Active certificates, no internal tags
- [ ] Analyst sees all certificates, all tags, can edit but not delete
- [ ] Admin sees everything, can edit and delete
- [ ] Company-scoped data isolation verified for External role
- [ ] Status-based visibility verified (Draft/Archived hidden from Public/External)
- [ ] Shareable URLs work across sessions
- [ ] Same certificate viewed from 4 roles shows appropriate data per role

---

# PART B2: ACCESSIBILITY TESTING

---

## 23. Module 20: Accessibility Testing (WCAG 2.1 AA)

**Standard**: WCAG 2.1 Level AA Compliance
**Tools Required**: Screen reader (NVDA/JAWS/VoiceOver), keyboard only, browser zoom, color contrast analyzer
**Applies to**: All pages across all roles

### 20.1 Keyboard Navigation

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-001 | Tab order - Login page | 1. Navigate to /Account/Login 2. Press Tab repeatedly | Focus moves in logical order: Username → Password → Remember Me → Sign In → Forgot Password | |
| TC-A11Y-002 | Tab order - Public tile view | 1. Navigate to public tile view 2. Tab through elements | Focus order: Skip link → Search bar → Filter toggles → Certificate tiles → Load more | |
| TC-A11Y-003 | Tab order - Admin pages | 1. Navigate to any Admin page 2. Tab through | Focus order: Nav menu → Action buttons (Template, Import, Export, Add) → Search → Filters → Table rows → Pagination | |
| TC-A11Y-004 | Skip navigation link | 1. Press Tab on any page | First focusable element is "Skip to main content" link. Pressing Enter skips nav. | |
| TC-A11Y-005 | Enter/Space activates buttons | 1. Tab to any button 2. Press Enter or Space | Button activates (same as click) | |
| TC-A11Y-006 | Escape closes modals | 1. Open any modal (delete confirmation, help, etc.) 2. Press Escape | Modal closes, focus returns to trigger element | |
| TC-A11Y-007 | Arrow keys in dropdowns | 1. Tab to a dropdown (status filter, role select) 2. Use Up/Down arrows | Options navigate via arrow keys | |
| TC-A11Y-008 | Focus trap in modals | 1. Open a confirmation modal 2. Tab through all elements 3. Continue tabbing | Focus stays within modal (does not tab to background elements) | |

### 20.2 Screen Reader Compatibility

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-009 | Page title announced | 1. Navigate to each page with screen reader on | Screen reader announces page title (from ViewData["Title"]) | |
| TC-A11Y-010 | Heading hierarchy | 1. Use screen reader heading list on any page | Single H1 (page title from layout), logical H2/H3 subsections, no skipped levels | |
| TC-A11Y-011 | Form labels on login | 1. Tab to username/password fields with screen reader | Fields announced with their labels: "Username", "Password" | |
| TC-A11Y-012 | Form labels on certificate create | 1. Tab through Create Certificate form with screen reader | All fields have associated labels: Company, Product, Certification Type, Dates, etc. | |
| TC-A11Y-013 | Table headers announced | 1. Navigate admin table with screen reader | Screen reader announces column headers when navigating cells | |
| TC-A11Y-014 | Button purposes announced | 1. Focus on action buttons (Edit, Delete, Export) | Screen reader announces button purpose (not just "button") | |
| TC-A11Y-015 | Status badge alt text | 1. Navigate to certificate tile with screen reader | Status badge text announced (e.g., "Status: Active") | |
| TC-A11Y-016 | Image alt text | 1. Navigate to certificate detail with thumbnail | Image has alt text "Certificate preview" or descriptive equivalent | |
| TC-A11Y-017 | sr-only labels for search/filter | 1. Focus on search input and filter dropdowns | admin-sr-only labels are read by screen reader (e.g., "Search items", "Filter by status") | |

### 20.3 Color Contrast & Visual

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-018 | Text contrast ratio | 1. Use contrast checker on body text vs background | Contrast ratio >= 4.5:1 for normal text (16px), >= 3:1 for large text (18px+) | |
| TC-A11Y-019 | Button contrast | 1. Check primary button (.btn--primary) text vs background | Contrast ratio >= 4.5:1 | |
| TC-A11Y-020 | Status badge contrast | 1. Check Active (#92C83E), Draft (#FFC107), Expired (#FF5252) badge text vs background | All badge text/background combos meet >= 4.5:1 contrast | |
| TC-A11Y-021 | Link visibility | 1. Inspect links on any page | Links distinguishable from surrounding text by more than just color (underline or bold) | |
| TC-A11Y-022 | Focus indicator visible | 1. Tab through elements | Visible focus ring/outline on every focusable element (no outline:none that hides focus) | |
| TC-A11Y-023 | Information not by color alone | 1. Check validation errors, status indicators | Errors have icon + text (not just red color). Statuses have label text (not just colored dot). | |
| TC-A11Y-024 | 16px minimum font | 1. Inspect all text on every page | No text smaller than 16px (per CLAUDE.md requirement) | |

### 20.4 ARIA Attributes

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-025 | aria-label on search inputs | 1. Inspect search input fields | Each search has aria-label (e.g., "Search items", "Search certificates") | |
| TC-A11Y-026 | aria-label on filter selects | 1. Inspect filter dropdowns | Each filter has aria-label (e.g., "Filter by status", "Filter by role") | |
| TC-A11Y-027 | aria-expanded on collapsible panels | 1. Inspect filter panel toggle button | aria-expanded="true" when open, "false" when closed | |
| TC-A11Y-028 | aria-hidden on decorative icons | 1. Inspect Font Awesome icons (<i class="fas fa-*">) | Decorative icons have aria-hidden="true" | |
| TC-A11Y-029 | role="alert" on notifications | 1. Trigger a toast notification 2. Inspect element | Notification container has role="alert" or aria-live="polite" | |
| TC-A11Y-030 | role="dialog" on modals | 1. Open confirmation modal 2. Inspect | Modal has role="dialog" and aria-labelledby pointing to modal title | |
| TC-A11Y-031 | aria-invalid on validation errors | 1. Submit form with empty required fields 2. Inspect invalid field | Field has aria-invalid="true" and associated error message via aria-describedby | |

### 20.5 Form Accessibility

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-032 | Required fields indicated | 1. View certificate create form | Required fields have visual indicator (*) AND aria-required="true" | |
| TC-A11Y-033 | Error messages linked to fields | 1. Submit with validation errors | Each error message is associated with its field (via aria-describedby or adjacent label) | |
| TC-A11Y-034 | Autocomplete accessible | 1. Type in search bar to trigger autocomplete 2. Use arrow keys | Suggestions navigable via keyboard. Selected item announced by screen reader. aria-autocomplete present. | |
| TC-A11Y-035 | File upload accessible | 1. Tab to file upload zone on certificate create | Upload zone focusable, activatable via Enter/Space. Screen reader announces "Upload PDF" or equivalent. | |
| TC-A11Y-036 | Date picker accessible | 1. Tab to date fields 2. Try to enter date via keyboard | Date can be entered via keyboard typing (YYYY-MM-DD). If date picker widget exists, it's keyboard navigable. | |
| TC-A11Y-037 | Tag selection accessible | 1. Tab to tag dropdown/selector 2. Use keyboard to select | Tags selectable via keyboard. Selected tags removable via keyboard (X button focusable). | |
| TC-A11Y-038 | Checkbox filters accessible | 1. Tab to filter checkboxes 2. Press Space | Checkboxes toggle via Space key. Label associated with checkbox. | |

### 20.6 Modal & Dialog Accessibility

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-039 | Delete confirmation modal | 1. Click Delete 2. Inspect modal with screen reader | Modal has title, descriptive text, and clearly labeled Confirm/Cancel buttons. Focus trapped inside. | |
| TC-A11Y-040 | Help modal accessible | 1. Open Help modal on cert type page | Content readable by screen reader, close button accessible, Escape key works. | |
| TC-A11Y-041 | Modal focus management | 1. Open any modal | Focus moves to first focusable element in modal. On close, focus returns to trigger button. | |
| TC-A11Y-042 | Toast notification announced | 1. Trigger success action (save, delete) | Toast notification announced by screen reader via aria-live region | |

### 20.7 Responsive & Zoom Accessibility

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-043 | 200% zoom usable | 1. Zoom browser to 200% 2. Navigate tile view | Content reflows, no horizontal scroll on main content. All functions accessible. | |
| TC-A11Y-044 | 200% zoom admin tables | 1. Zoom browser to 200% on admin page | Table scrollable or reformatted. No content cut off. | |
| TC-A11Y-045 | Touch target size (mobile) | 1. View on mobile device or responsive mode | All interactive elements >= 44x44px touch target | |
| TC-A11Y-046 | Text resizing | 1. Use browser text-only zoom to 200% | Text resizes without breaking layout. No text truncated. | |

### 20.8 Data Table Accessibility

| ID | Description | Steps | Expected Result | Status |
|----|-------------|-------|-----------------|--------|
| TC-A11Y-047 | Admin table has <th> headers | 1. Inspect admin table HTML | Table uses proper <thead>/<th> elements, not styled <td> | |
| TC-A11Y-048 | Table has caption or aria-label | 1. Inspect table element | Table has <caption> or aria-label describing its content (e.g., "Certificate list") | |
| TC-A11Y-049 | Sortable columns announced | 1. If columns are sortable, tab to header | Screen reader announces sort state (e.g., "sorted ascending") | |
| TC-A11Y-050 | Empty table state | 1. Apply filter that returns no results | Accessible message displayed: "No results found" (not just empty table body) | |

---

# PART C: TRACEABILITY & REPORTING

---

## 24. Requirements Traceability Matrix

### Core Requirements Mapping

| Requirement | Description | Test Cases | Verified? |
|-------------|-------------|------------|-----------|
| REQ-AUTH-01 | Role-based access control (6 roles) | TC-PERM-001 to TC-PERM-046 | |
| REQ-AUTH-02 | Two-factor authentication | TC-AUTH-009 to TC-AUTH-012 | |
| REQ-AUTH-03 | Session management & timeout | TC-AUTH-015, TC-AUTH-018 | |
| REQ-CERT-01 | Certificate CRUD operations | TC-ANL-010 to TC-ANL-047 | |
| REQ-CERT-02 | 14-state workflow | TC-WF-001 to TC-WF-047 | |
| REQ-CERT-03 | Certificate number format (YY-NNNN) | TC-ANL-011, TC-NEG-019, TC-PUB-017 | |
| REQ-CERT-04 | PDF upload with preview | TC-ANL-021 to TC-ANL-023, TC-ANL-049 | |
| REQ-CERT-05 | Product identifier management | TC-ANL-026 to TC-ANL-030 | |
| REQ-CERT-06 | Tag management (internal/external) | TC-ANL-024 to TC-ANL-025, TC-ADM-043 to TC-ADM-051 | |
| REQ-CERT-07 | Custom fields per cert type | TC-ANL-017, TC-ADM-035 to TC-ADM-042 | |
| REQ-CERT-08 | Certification Results field (optional) | TC-ANL-031 | |
| REQ-CERT-09 | Certificate number displayed (not ID) | TC-ANL-052, TC-PUB-017 | |
| REQ-VIEW-01 | Public tile view with infinite scroll | TC-PUB-001 to TC-PUB-012 | |
| REQ-VIEW-02 | External tile view (company-scoped) | TC-EXT-001 to TC-EXT-012 | |
| REQ-VIEW-03 | Internal tile view (all data) | TC-ANL-001 to TC-ANL-009 | |
| REQ-VIEW-04 | Autocomplete search | TC-PUB-003, TC-PUB-004 | |
| REQ-VIEW-05 | Shareable URLs | TC-PUB-009, TC-ANL-008 | |
| REQ-VIEW-06 | Database-driven filters | TC-PUB-005 to TC-PUB-008 | |
| REQ-EXT-01 | External sees only own company | TC-EXT-002, TC-EXT-003, TC-EXT-017 | |
| REQ-EXT-02 | External cannot see internal tags | TC-EXT-007, TC-PUB-011 | |
| REQ-EXT-03 | External no Add/Edit/Delete | TC-EXT-004 to TC-EXT-006, TC-EXT-018 to TC-EXT-019 | |
| REQ-ADM-01 | User management (CRUD + import) | TC-ADM-004 to TC-ADM-018 | |
| REQ-ADM-02 | Role management with permissions | TC-ADM-019 to TC-ADM-027 | |
| REQ-ADM-03 | Certificate type hierarchy | TC-ADM-028 to TC-ADM-034 | |
| REQ-ADM-04 | Partner management (core entities) | TC-ADM-052 to TC-ADM-058 | |
| REQ-ADM-05 | Partners - export/add only (no import) | TC-ADM-058, TC-IMP-007, TC-IMP-008 | |
| REQ-ADM-06 | Admin role cannot be deleted | TC-ADM-024 | |
| REQ-ADM-07 | Unique emails per user | TC-ADM-010 | |
| REQ-EXP-01 | CSV export only (no Excel/JSON/PDF) | TC-EXP-008 | |
| REQ-IMP-01 | CSV import with templates | TC-IMP-001 to TC-IMP-006 | |
| REQ-API-01 | Partner API with key authentication | TC-API-001 to TC-API-004 | |
| REQ-API-02 | Certificate REST endpoints | TC-API-005 to TC-API-014 | |
| REQ-API-03 | Product ID search via API | TC-API-012 | |
| REQ-API-04 | Rate limiting | TC-API-020, TC-API-021 | |
| REQ-API-05 | GS1 JSON-LD format | TC-API-016 | |
| REQ-UI-01 | No native alert/confirm/prompt | TC-NEG-011 to TC-NEG-017 | |
| REQ-UI-02 | Delete: confirm modal + toast | TC-NEG-011 to TC-NEG-014, TC-ADM-077 | |
| REQ-UI-03 | 16px minimum font size | TC-NEG-024 | |
| REQ-SEC-01 | XSS prevention | TC-NEG-021 | |
| REQ-SEC-02 | SQL injection prevention | TC-NEG-022 | |
| REQ-A11Y-01 | Keyboard navigation (all pages) | TC-A11Y-001 to TC-A11Y-008 | |
| REQ-A11Y-02 | Screen reader compatibility | TC-A11Y-009 to TC-A11Y-017 | |
| REQ-A11Y-03 | Color contrast >= 4.5:1 | TC-A11Y-018 to TC-A11Y-024 | |
| REQ-A11Y-04 | ARIA attributes on interactive elements | TC-A11Y-025 to TC-A11Y-031 | |
| REQ-A11Y-05 | Accessible forms | TC-A11Y-032 to TC-A11Y-038 | |
| REQ-A11Y-06 | Modal/dialog accessibility | TC-A11Y-039 to TC-A11Y-042 | |
| REQ-A11Y-07 | 200% zoom usable | TC-A11Y-043 to TC-A11Y-046 | |
| REQ-A11Y-08 | Data table accessibility | TC-A11Y-047 to TC-A11Y-050 | |
| REQ-ADM-08 | Certificate Import Admin-only | TC-ANL-059, TC-PERM-019, TC-ADM-075 to TC-ADM-081 | |
| REQ-ADM-09 | Delete with dependency protection | TC-ADM-085 to TC-ADM-089 | |
| REQ-ADM-10 | Duplicate entity prevention | TC-ADM-090 to TC-ADM-092 | |

### End-to-End Scenario Coverage

| Requirement | E2E Scenario | Steps |
|-------------|-------------|-------|
| Full certificate lifecycle (create → active → archived) | E2E Scenario 1 (Module 11) | E2E-LC-001 to E2E-LC-046 |
| Company onboarding & data isolation | E2E Scenario 2 (Module 12) | E2E-CO-001 to E2E-CO-026 |
| Partner API integration end-to-end | E2E Scenario 3 (Module 13) | E2E-API-001 to E2E-API-018 |
| Admin system setup feeds into cert creation | E2E Scenario 4 (Module 14) | E2E-SETUP-001 to E2E-SETUP-027 |
| Automated renewal & expiration lifecycle | E2E Scenario 5 (Module 15) | E2E-REN-001 to E2E-REN-014 |
| Rejection, correction, and re-approval | E2E Scenario 6 (Module 16) | E2E-REJ-001 to E2E-REJ-014 |
| Bulk template → import → verify → export | E2E Scenario 7 (Module 17) | E2E-BULK-001 to E2E-BULK-024 |
| User account creation → usage → deactivation | E2E Scenario 8 (Module 18) | E2E-USER-001 to E2E-USER-026 |
| Same data viewed across all 4 roles | E2E Scenario 9 (Module 19) | E2E-SEARCH-001 to E2E-SEARCH-026 |

---

## 25. Defect Reporting Template

When a test fails, log a defect using this template:

```
### Defect Report

**Defect ID:** DEF-XXX
**Date:** YYYY-MM-DD
**Tester:** [Name]
**Test Case ID:** TC-XXX-XXX
**Severity:** Critical / High / Medium / Low
**Priority:** P1 / P2 / P3 / P4

**Summary:**
[One-line description of the defect]

**Environment:**
- Browser: [Chrome/Firefox/Edge + version]
- URL: [Environment URL]
- Role: [Admin/Analyst/External/Public]

**Steps to Reproduce:**
1. [Step 1]
2. [Step 2]
3. [Step 3]

**Expected Result:**
[What should have happened]

**Actual Result:**
[What actually happened]

**Screenshots/Evidence:**
[Attach screenshots or console errors]

**Additional Notes:**
[Any context, workarounds, or related defects]
```

---

## 26. Test Execution Summary

### Part A: Feature-Level Testing

| Module | Total Tests | Pass | Fail | Blocked | Not Run | % Complete |
|--------|------------|------|------|---------|---------|------------|
| 1. Authentication | 18 | | | | 18 | 0% |
| 2. Public User | 20 | | | | 20 | 0% |
| 3. External User | 24 | | | | 24 | 0% |
| 4. Analyst User | 63 | | | | 63 | 0% |
| 5. Admin User | 92 | | | | 92 | 0% |
| 6. Workflow (14 States) | 47 | | | | 47 | 0% |
| 7. Partner API | 21 | | | | 21 | 0% |
| 8. Permissions Matrix | 47 | | | | 47 | 0% |
| 9. Export/Import | 18 | | | | 18 | 0% |
| 10. Edge Cases & Negative | 26 | | | | 26 | 0% |
| **Part A Subtotal** | **376** | | | | **376** | **0%** |

### Part B: End-to-End Business Scenarios

| Module | Total Steps | Pass | Fail | Blocked | Not Run | % Complete |
|--------|------------|------|------|---------|---------|------------|
| 11. E2E: Certificate Full Lifecycle | 46 | | | | 46 | 0% |
| 12. E2E: New Company Onboarding | 26 | | | | 26 | 0% |
| 13. E2E: Partner API Integration | 18 | | | | 18 | 0% |
| 14. E2E: Admin System Setup (Day 1) | 27 | | | | 27 | 0% |
| 15. E2E: Certificate Renewal Lifecycle | 14 | | | | 14 | 0% |
| 16. E2E: Rejection & Recovery Flow | 14 | | | | 14 | 0% |
| 17. E2E: Bulk Operations Workflow | 24 | | | | 24 | 0% |
| 18. E2E: User Access Lifecycle | 26 | | | | 26 | 0% |
| 19. E2E: Search & Discovery Cross-Role | 26 | | | | 26 | 0% |
| **Part B Subtotal** | **221** | | | | **221** | **0%** |

### Part B2: Accessibility Testing

| Module | Total Tests | Pass | Fail | Blocked | Not Run | % Complete |
|--------|------------|------|------|---------|---------|------------|
| 20. Accessibility (WCAG 2.1 AA) | 50 | | | | 50 | 0% |
| **Part B2 Subtotal** | **50** | | | | **50** | **0%** |

### Combined Total

| Category | Total | Pass | Fail | Blocked | Not Run | % Complete |
|----------|-------|------|------|---------|---------|------------|
| Part A: Feature Tests | 376 | | | | 376 | 0% |
| Part B: E2E Scenarios | 221 | | | | 221 | 0% |
| Part B2: Accessibility | 50 | | | | 50 | 0% |
| **GRAND TOTAL** | **647** | | | | **647** | **0%** |

### Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| QA Lead | | | |
| Product Owner | | | |
| Project Manager | | | |
| Client Representative | | | |

---

### Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-19 | AI-Assisted Generation | Initial feature-level guide - 359 test cases across 10 modules |
| 2.0 | 2026-02-19 | AI-Assisted Generation | Added 9 end-to-end business scenarios (221 steps), bringing total to 580 test cases/steps across 19 modules |
| 3.0 | 2026-02-19 | AI-Assisted Generation | Gap fixes: corrected analyst statuses (12 of 14, excludes Rejected/Revoked), moved Certificate Import to Admin-only module, added dependency/duplicate tests, added Draft visibility tests. Added Module 20: Accessibility Testing (50 WCAG 2.1 AA tests). Added test data setup script. Total: 647 tests across 20 modules. |

---

*This guide covers the complete GCC Database Solution application. All test cases should be executed per role before UAT sign-off.*

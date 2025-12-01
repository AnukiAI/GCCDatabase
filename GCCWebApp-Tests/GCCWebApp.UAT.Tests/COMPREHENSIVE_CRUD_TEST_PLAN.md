# Comprehensive CRUD Test Plan for GCC Web Application

## Executive Summary

This plan expands CRUD test coverage from the current 7 entity types to comprehensive coverage of ALL admin management pages, plus Certificate operations, role-based access control, and edge cases.

## Current Coverage (7 Existing Test Files)

| Entity | Tests | Coverage |
|--------|-------|----------|
| Tag Management | 16 | CREATE (5), READ (4), UPDATE (2), DELETE (3), EXPORT (2) |
| User Management | 18 | CREATE (4), READ (5), UPDATE (4), DELETE (3), EXPORT (2) |
| Company Management | 12 | CREATE (3), READ (3), UPDATE (2), DELETE (2), EXPORT (2) |
| Product Management | 12 | CREATE (3), READ (4), UPDATE (2), DELETE (2), EXPORT (1) |
| Certificate Types | 8 | CREATE (2), READ (2), UPDATE (1), DELETE (2), EXPORT (1) |
| Certificate Fields | 8 | CREATE (2), READ (2), UPDATE (1), DELETE (2), EXPORT (1) |
| Partners | 9 | CREATE (3), READ (2), UPDATE (1), DELETE (2), EXPORT (1) |
| **TOTAL** | **83** | Basic CRUD coverage |

## Gap Analysis - Missing Test Coverage

### 1. Missing Admin Page Tests

| Admin Page | URL | Priority | Status |
|------------|-----|----------|--------|
| Role Management | /Admin/RoleManagement | HIGH | **MISSING** |
| Product Types | /Admin/ProductTypes | HIGH | **MISSING** |
| CSF Attributes | /Admin/CSFAttributes | MEDIUM | **MISSING** |
| Partner API | /Admin/PartnerAPI | HIGH | **MISSING** |
| GCC API | /Admin/GCCAPI | MEDIUM | **MISSING** |
| Google Analytics | /Admin/GoogleAnalytics | LOW | **MISSING** |
| Admin Dashboard | /Admin/Index | LOW | **MISSING** |

### 2. Missing Certificate Tests

| Test Area | Priority | Status |
|-----------|----------|--------|
| Certificate Create Flow | HIGH | **MISSING** |
| Certificate Edit Flow | HIGH | **MISSING** |
| Certificate View (Single) | MEDIUM | **MISSING** |
| Certificate Delete (with workflow) | HIGH | **MISSING** |
| Certificate Workflow States (14 states) | HIGH | **MISSING** |
| Certificate Download | MEDIUM | **MISSING** |
| Certificate Export (CSV/Excel) | MEDIUM | **MISSING** |
| Product ID Management (ASINs) | HIGH | **MISSING** |

### 3. Missing Validation Tests

| Test Type | Entities Affected | Priority |
|-----------|-------------------|----------|
| Required Field Validation | All | HIGH |
| Email Format Validation | Users | HIGH |
| Duplicate Name Prevention | Tags, Products, Companies | HIGH |
| Password Strength Validation | Users | MEDIUM |
| Date Format Validation | Certificates | MEDIUM |

### 4. Missing Role-Based Access Tests

| Test Scenario | Priority |
|---------------|----------|
| Admin can access all admin pages | HIGH |
| Analyst cannot access admin pages | HIGH |
| External cannot access admin pages | HIGH |
| External sees filtered data only | HIGH |
| Role permissions are enforced | HIGH |

### 5. Missing Import/Export Tests

| Entity | Import | Export | Template |
|--------|--------|--------|----------|
| Users | Need Tests | Need Tests | Need Tests |
| Tags | Need Tests | Need Tests | Need Tests |
| Products | Need Tests | Need Tests | Need Tests |
| Companies | Need Tests | Need Tests | Need Tests |

---

## Proposed New Test Files

### Phase 1: Missing Admin Pages (High Priority)

1. **RoleManagementCrudTests.cs** (~15 tests)
   - CREATE: Add new role, add role with permissions, cancel add
   - READ: View roles list, view permissions list
   - UPDATE: Edit role, modify permissions, cancel edit
   - DELETE: Delete role, system role protection (Admin cannot delete)
   - PERMISSIONS: Assign/revoke permissions, verify permission categories

2. **ProductTypesCrudTests.cs** (~12 tests)
   - CREATE: Add category, add type, add subcategory
   - READ: View hierarchy (tree view), view table view, search, expand/collapse
   - UPDATE: Edit category, edit type, move in hierarchy
   - DELETE: Delete category, delete type, cascade protection
   - EXPORT: Export to CSV

3. **PartnerApiCrudTests.cs** (~14 tests)
   - CREATE: Generate API key for partner
   - READ: View partners grid, view partner details, view usage stats, view access logs
   - UPDATE: Update API key permissions, configure rate limits
   - DELETE: Revoke API key
   - NAVIGATION: Click partner card, switch between partners

4. **GccApiCrudTests.cs** (~10 tests)
   - CREATE: Generate API key
   - READ: View API info, view available endpoints, view key list
   - UPDATE: Update key permissions
   - DELETE: Revoke/delete API key
   - NAVIGATION: Access Swagger documentation

### Phase 2: Certificate Operations (High Priority)

5. **CertificateCrudTests.cs** (~25 tests)
   - CREATE: Create new certificate, fill required fields, select company/product
   - READ: View certificate list (tile view), view single certificate, search, filter
   - UPDATE: Edit certificate, update status, modify fields
   - DELETE: Delete certificate, confirm deletion, cancel deletion
   - WORKFLOW: Transition through states (Draft -> Active, Active -> Expired, etc.)
   - DOWNLOAD: Download PDF, download via tile view

6. **ProductIdCrudTests.cs** (~12 tests)
   - CREATE: Add single ASIN, add multiple ASINs, add UPC/GTIN/EAN
   - READ: View product IDs on certificate, view in modal
   - UPDATE: Edit product ID, change type
   - DELETE: Remove product ID, bulk delete
   - IMPORT: CSV import of product IDs
   - EXPORT: CSV export of product IDs

### Phase 3: Validation & Edge Cases (Medium Priority)

7. **ValidationTests.cs** (~20 tests)
   - User: Required email, valid email format, unique email, password strength
   - Tag: Required name, unique name per type, valid type selection
   - Company: Required name, unique name
   - Product: Required name, valid company selection
   - Certificate: Required fields, valid date formats, valid status transitions

8. **EdgeCaseTests.cs** (~15 tests)
   - Empty tables handling
   - Maximum length inputs
   - Special characters in names
   - Concurrent edit handling
   - Session timeout during edit
   - Network error during save

### Phase 4: Role-Based Access Control (High Priority)

9. **RoleAccessControlTests.cs** (~20 tests)
   - Admin access to all pages
   - Analyst blocked from admin pages
   - External blocked from admin pages
   - External sees filtered product data
   - External cannot see internal tags
   - External cannot add/edit certificates
   - Role-specific UI elements (buttons visible/hidden)

### Phase 5: Import/Export Operations (Medium Priority)

10. **ImportExportTests.cs** (~18 tests)
    - CSV export for each entity type
    - Template download for each entity type
    - CSV import with valid data
    - CSV import with invalid data (error handling)
    - Large file import
    - Export with filters applied

### Phase 6: Additional Admin Pages (Low Priority)

11. **CSFAttributesCrudTests.cs** (~8 tests)
    - Basic CRUD operations
    - View attributes list
    - Add/Edit/Delete attributes

12. **GoogleAnalyticsCrudTests.cs** (~6 tests)
    - READ: View current settings
    - UPDATE: Update tracking ID, toggle enable/disable
    - VALIDATION: Invalid tracking ID format

13. **AdminDashboardTests.cs** (~5 tests)
    - READ: View dashboard, verify statistics cards
    - NAVIGATION: Quick action links work correctly

---

## Test Summary After Implementation

| Category | Tests | Priority |
|----------|-------|----------|
| Existing CRUD Tests | 83 | Done |
| Role Management | 15 | High |
| Product Types | 12 | High |
| Partner API | 14 | High |
| GCC API | 10 | Medium |
| Certificate CRUD | 25 | High |
| Product ID CRUD | 12 | High |
| Validation Tests | 20 | Medium |
| Edge Case Tests | 15 | Medium |
| Role Access Control | 20 | High |
| Import/Export Tests | 18 | Medium |
| CSF Attributes | 8 | Low |
| Google Analytics | 6 | Low |
| Admin Dashboard | 5 | Low |
| **TOTAL NEW** | **180** | - |
| **GRAND TOTAL** | **263** | - |

---

## Implementation Order

### Sprint 1 (Immediate - High Priority)
1. RoleManagementCrudTests.cs
2. CertificateCrudTests.cs
3. RoleAccessControlTests.cs
4. PartnerApiCrudTests.cs

### Sprint 2 (Next - High Priority)
5. ProductTypesCrudTests.cs
6. ProductIdCrudTests.cs
7. ValidationTests.cs

### Sprint 3 (Medium Priority)
8. GccApiCrudTests.cs
9. ImportExportTests.cs
10. EdgeCaseTests.cs

### Sprint 4 (Low Priority)
11. CSFAttributesCrudTests.cs
12. GoogleAnalyticsCrudTests.cs
13. AdminDashboardTests.cs

---

## Test Categories for Runner

```powershell
# New test filter categories
run-production-tests crud-roles       # Role Management CRUD
run-production-tests crud-prodtypes   # Product Types CRUD
run-production-tests crud-partnerapi  # Partner API CRUD
run-production-tests crud-gccapi      # GCC API CRUD
run-production-tests crud-certs       # Certificate CRUD
run-production-tests crud-productids  # Product ID CRUD
run-production-tests validation       # Validation tests
run-production-tests edge-cases       # Edge case tests
run-production-tests access-control   # Role access control tests
run-production-tests import-export    # Import/Export tests
```

---

## Questions for Clarification

Before proceeding, please confirm:

1. **Certificate Workflow Testing**: Should tests verify ALL 14 workflow states, or focus on common transitions (Draft->Active, Active->Expired)?

2. **Data Cleanup**: Should tests clean up created test data after completion, or leave it for manual inspection?

3. **Production Safety**: For production testing, should we:
   - Only test READ operations?
   - Create/Delete test data with specific prefixes (e.g., "AutoTest_")?
   - Use a dedicated test account with limited permissions?

4. **Partner API Testing**: The Partner API management requires actual API key generation. Should we:
   - Test only UI interactions?
   - Actually generate and then revoke keys?

5. **Priority Confirmation**: Is the proposed implementation order (Sprints 1-4) acceptable, or should priorities be adjusted?

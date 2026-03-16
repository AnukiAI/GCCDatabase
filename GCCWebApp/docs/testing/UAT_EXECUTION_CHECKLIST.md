# GCC Database Solution - UAT Test Execution Checklist

**Date:** February 19, 2026
**Tester:** ____________________
**Environment:** ____________________
**Build/Version:** ____________________

---

## Instructions

1. Execute tests in priority order (P1 first, then P2, P3, P4)
2. Mark each test: PASS / FAIL / BLOCKED / SKIP
3. For FAILs, note the defect ID in the Notes column
4. Each section has a sign-off line at the bottom
5. Estimated total time: 6-8 hours for full execution

---

## Pre-Test Setup Checklist

- [ ] Application running and accessible at: ____________________
- [ ] `UAT_TEST_DATA_SETUP.sql` executed against test database
- [ ] Test accounts verified: admin / analyst / external (all TestPass123!)
- [ ] 8 seed certificates exist (Active x3, Draft, Archived, Expired, PendingReview, Active-expiring)
- [ ] 4 companies, 6 products, 7 tags exist
- [ ] External user associated with TestCo UAT (Company ID 3)
- [ ] PDF test file ready (< 10MB)
- [ ] CSV test files ready for import testing
- [ ] API testing tool available (curl/Postman)
- [ ] Browser DevTools accessible

---

## PRIORITY 1 - CRITICAL (Execute First)

### P1-A: Login & Authentication (15 min)

| # | Test | Role | Result | Notes |
|---|------|------|--------|-------|
| 1 | Login as admin (admin/TestPass123!) | Admin | | |
| 2 | Login as analyst (analyst/TestPass123!) | Analyst | | |
| 3 | Login as external (external/TestPass123!) | External | | |
| 4 | Invalid password rejected | Any | | |
| 5 | Empty fields show validation errors | Any | | |
| 6 | Logout clears session | Any | | |

**Sign-off: ________ Date: ________**

### P1-B: Role-Based Access Control (20 min)

Test each URL as each role. Enter PASS if correct, FAIL if wrong access.

| # | URL | Public | External | Analyst | Admin |
|---|-----|--------|----------|---------|-------|
| 1 | /Certificate/CertificateTileView | Allow | Allow | Allow | Allow |
| 2 | /Certificate/CertificateTileViewExternal | Deny | Allow | Allow | Allow |
| 3 | /Certificate/CertificateTileViewInternal | Deny | Deny | Allow | Allow |
| 4 | /Certificate/Create | Deny | Deny | Allow | Allow |
| 5 | /Admin/UserManagement | Deny | Deny | Deny | Allow |
| 6 | /Admin/RoleManagement | Deny | Deny | Deny | Allow |
| 7 | /Admin/Tags | Deny | Deny | Deny | Allow |
| 8 | /CertificateImport/Index | Deny | Deny | Deny | Allow |
| 9 | /Dashboard/Index | Deny | Deny | Allow | Allow |

**Sign-off: ________ Date: ________**

### P1-C: External User Data Isolation (15 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Login as external - only own company certs visible | | |
| 2 | Search for other company's cert number - NOT found | | |
| 3 | Products page shows only own company products | | |
| 4 | No Add/Edit/Delete buttons visible anywhere | | |
| 5 | No internal tags visible on any certificate | | |
| 6 | No Administration menu in navigation | | |
| 7 | Direct URL to /Certificate/Create = access denied | | |
| 8 | Direct URL to /Admin/UserManagement = access denied | | |

**Sign-off: ________ Date: ________**

### P1-D: Certificate CRUD (25 min)

| # | Test | Role | Result | Notes |
|---|------|------|--------|-------|
| 1 | Create cert - auto-generated number (YY-NNNN format) | Analyst | | |
| 2 | Select company, product cascades correctly | Analyst | | |
| 3 | Select cert type, subcategory populates | Analyst | | |
| 4 | Custom fields load when cert type selected | Analyst | | |
| 5 | Upload PDF (valid < 10MB) - preview generated | Analyst | | |
| 6 | Upload invalid file type (.docx) - rejected | Analyst | | |
| 7 | Add external + internal tags | Analyst | | |
| 8 | Add product identifiers (ASIN, UPC) | Analyst | | |
| 9 | Enter Certification Results (optional) | Analyst | | |
| 10 | Save - success toast, redirect to details | Analyst | | |
| 11 | Details shows Certificate Number (NOT database ID) | Analyst | | |
| 12 | Edit - all fields pre-populated | Analyst | | |
| 13 | Save changes - success notification | Analyst | | |
| 14 | Admin can delete cert (confirm modal + toast) | Admin | | |
| 15 | Required field validation works on empty submit | Analyst | | |

**Sign-off: ________ Date: ________**

### P1-E: 14-State Workflow - Key Transitions (20 min)

| # | From | To | Valid? | Role | Result | Notes |
|---|------|----|--------|------|--------|-------|
| 1 | Draft | Pending Review | Yes | Analyst | | |
| 2 | Draft | Active | **NO** | Any | | Verify NOT in dropdown |
| 3 | Pending Review | Under Review | Yes | Analyst | | |
| 4 | Under Review | Approved | Yes | Analyst | | |
| 5 | Under Review | Rejected | Yes | Admin | | |
| 6 | Approved | Active | Yes | Analyst | | |
| 7 | Active | Suspended | Yes | Admin | | |
| 8 | Active | Revoked | Yes | Admin | | |
| 9 | Revoked | Active | **NO** | Any | | Near-final state |
| 10 | Cancelled | ANY | **NO** | Any | | Final state |
| 11 | Archived | ANY | **NO** | Any | | Final state |
| 12 | Analyst sees 12 statuses (NOT Rejected/Revoked) | | Analyst | | |
| 13 | Admin sees all 14 statuses | | Admin | | |

**Sign-off: ________ Date: ________**

### P1-F: Notification Pattern (10 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Delete cert: confirmation MODAL before, TOAST after | | |
| 2 | Delete user: confirmation MODAL before, TOAST after | | |
| 3 | Create entity: toast notification (no modal) | | |
| 4 | Edit entity: toast notification (no modal) | | |
| 5 | **NO native browser alert/confirm/prompt ANYWHERE** | | |

**Sign-off: ________ Date: ________**

### P1-G: Security Quick Check (5 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Enter `<script>alert('xss')</script>` in text field | | No script execution |
| 2 | Enter `' OR 1=1 --` in search | | No SQL injection |
| 3 | CSV export only (.csv, NOT .xlsx/.json/.pdf) | | |

**Sign-off: ________ Date: ________**

---

## PRIORITY 2 - HIGH (Execute Second)

### P2-A: Partner API Verification (20 min)

**API Key:** ____________________ (generate from Admin > Partner API)

| # | Test | Method | Result | Notes |
|---|------|--------|--------|-------|
| 1 | Valid API key returns 200 | GET /api/v1/certificates + X-API-Key | | |
| 2 | Missing API key returns 401 | GET /api/v1/certificates (no key) | | |
| 3 | Invalid API key returns 401 | GET /api/v1/certificates + wrong key | | |
| 4 | Pagination works (pageSize, totalCount) | ?pageSize=5&pageNumber=1 | | |
| 5 | Get by certificate number | /number/{number} | | |
| 6 | Product IDs included | ?includeProductIds=true | | |
| 7 | Search by ASIN | /search/productid?productId={ASIN}&type=ASIN | | |
| 8 | Download PDF | /{number}/download | | |
| 9 | issuer = "Green Circle Certified" | Check any response | | |
| 10 | Dates in ISO 8601 format | Check any response | | |
| 11 | Revoked key returns 401 | Revoke key, then request | | |

**Sign-off: ________ Date: ________**

### P2-B: Two-Factor Authentication (10 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Enable 2FA - QR code displayed | | |
| 2 | Login with valid TOTP code | | |
| 3 | Invalid TOTP code rejected | | |
| 4 | Admin can disable user's 2FA | | |

**Sign-off: ________ Date: ________**

### P2-C: Admin Data Management (20 min)

| # | Test | Page | Result | Notes |
|---|------|------|--------|-------|
| 1 | Create/edit/delete certificate type | Admin > Cert Types | | |
| 2 | Create subcategory under parent | Admin > Cert Types | | |
| 3 | Create/edit/delete tags (internal + external) | Admin > Tags | | |
| 4 | Create/edit/delete partner | Admin > Partners | | |
| 5 | Partners page: NO Template/Import buttons | Admin > Partners | | |
| 6 | Create/edit/delete product type | Admin > Product Types | | |
| 7 | Create/edit/delete custom field | Admin > Cert Fields | | |
| 8 | Associate custom field with cert type | Admin > Cert Fields | | |
| 9 | Cannot delete Admin role | Admin > Roles | | |
| 10 | Cannot delete admin user | Admin > Users | | |
| 11 | Unique email enforced on user creation | Admin > Users | | |

**Sign-off: ________ Date: ________**

### P2-D: Export/Import Cycle (15 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Download user import template (CSV) | | |
| 2 | Download tag import template (CSV) | | |
| 3 | Download certificate import template (CSV) | | |
| 4 | Import valid CSV - data imported | | |
| 5 | Import invalid CSV - row-level errors shown | | |
| 6 | Export certificates - CSV downloads | | |
| 7 | Export with filters applied - only filtered data | | |
| 8 | External user export - only own company data | | |

**Sign-off: ________ Date: ________**

### P2-E: Tile View Features (15 min)

| # | Test | View | Result | Notes |
|---|------|------|--------|-------|
| 1 | Infinite scroll loads more certificates | Public | | |
| 2 | Search autocomplete with categorized results | Public | | |
| 3 | Filter by certification type | Public | | |
| 4 | Filter by status | Public | | |
| 5 | Multiple filters combined | Public | | |
| 6 | Clear all filters | Public | | |
| 7 | Shareable URL preserves filters | Public | | |
| 8 | Draft certs NOT visible to public | Public | | |
| 9 | Archived certs NOT visible to public | Public | | |
| 10 | Bulk ZIP download works | Analyst | | |

**Sign-off: ________ Date: ________**

---

## PRIORITY 3 - MEDIUM (Execute Third)

### P3-A: Edge Cases & Validation (15 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Cert Results max 1000 chars enforced | | |
| 2 | Expiration before effective date = validation error | | |
| 3 | Duplicate certificate number prevented | | |
| 4 | Very long company name handled | | |
| 5 | Large CSV import (100+ rows) handled | | |
| 6 | Delete company with certificates = error/warning | | |
| 7 | Delete product with certificates = error/warning | | |
| 8 | Delete role with assigned users = error | | |
| 9 | Duplicate tag name prevented | | |

**Sign-off: ________ Date: ________**

### P3-B: Certification Results Visibility (10 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Cert Results visible in internal tile view column | | |
| 2 | Cert Results included in CSV export | | |
| 3 | Cert Results visible on certificate detail page | | |

**Sign-off: ________ Date: ________**

### P3-C: UI Standards Check (10 min)

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | All text >= 16px font size | | |
| 2 | Mobile responsive tile view (resize browser) | | |
| 3 | Admin tables responsive at tablet width | | |
| 4 | Single H1 per page (from layout, not view) | | |

**Sign-off: ________ Date: ________**

---

## PRIORITY 4 - LOW / FUTURE (Execute if Time Permits)

### P4-A: Amazon Scope Change Features [PENDING]

*Skip this section if Pathway/Amazon Code features are not yet built.*

| # | Test | Result | Notes |
|---|------|--------|-------|
| 1 | Pathway dropdown on cert create (scoped by cert type) | | |
| 2 | Pathway hidden when no pathways configured for type | | |
| 3 | General pathway visible for all cert types | | |
| 4 | Amazon Code in API response (standard JSON) | | |
| 5 | Amazon Code in API response (GS1 JSON-LD) | | |
| 6 | Pathway in API response (standard JSON) | | |
| 7 | Pathway in API response (GS1 JSON-LD) | | |
| 8 | Soft warning: Amazon cert + empty Cert Results | | |

**Sign-off: ________ Date: ________**

### P4-B: Accessibility Quick Check (20 min)

| # | Test | Tool | Result | Notes |
|---|------|------|--------|-------|
| 1 | Tab order logical on login page | Keyboard | | |
| 2 | Tab order logical on tile view | Keyboard | | |
| 3 | Enter/Space activates all buttons | Keyboard | | |
| 4 | Escape closes all modals | Keyboard | | |
| 5 | Focus indicator visible on all elements | Keyboard | | |
| 6 | Skip navigation link present | Keyboard | | |
| 7 | Form fields have labels (screen reader) | Screen reader | | |
| 8 | Status badges have text (not color only) | Visual | | |
| 9 | Color contrast >= 4.5:1 on body text | Contrast checker | | |
| 10 | 200% zoom - no content cut off | Browser zoom | | |

**Sign-off: ________ Date: ________**

---

## END-TO-END SCENARIOS (Execute After Feature Tests)

### E2E-1: Certificate Full Lifecycle (30-45 min)

| # | Step | Role | Result | Notes |
|---|------|------|--------|-------|
| 1 | Create cert as Draft with all fields | Analyst | | |
| 2 | Cert NOT visible in public view (Draft) | Public | | |
| 3 | Change to Pending Review -> Under Review -> Approved | Admin | | |
| 4 | Change to Active | Admin | | |
| 5 | Cert NOW visible in public view | Public | | |
| 6 | External user (same company) sees cert, no internal tags | External | | |
| 7 | API returns cert with all fields | API | | |
| 8 | Suspend cert - disappears from public | Admin | | |
| 9 | Reactivate - reappears | Admin | | |
| 10 | Archive cert (final state, no more transitions) | Admin | | |

**Sign-off: ________ Date: ________**

### E2E-2: Company Onboarding & Data Isolation (20-30 min)

| # | Step | Role | Result | Notes |
|---|------|------|--------|-------|
| 1 | Create new company | Admin | | |
| 2 | Create products under company | Admin | | |
| 3 | Create external user linked to company | Admin | | |
| 4 | Create active cert for new company | Admin | | |
| 5 | Login as new external user | External | | |
| 6 | ONLY sees own company's certs and products | External | | |
| 7 | Cannot see other company's data | External | | |
| 8 | Cannot access admin/create pages | External | | |

**Sign-off: ________ Date: ________**

### E2E-3: Cross-Role Comparison (25-30 min)

*Use the same set of certificates and verify from each role perspective.*

| Aspect | Public | External | Analyst | Admin | Pass? |
|--------|--------|----------|---------|-------|-------|
| Active cert from TestCo visible? | Yes | Yes | Yes | Yes | |
| Active cert from OtherCo visible? | Yes | **No** | Yes | Yes | |
| Draft cert visible? | **No** | **No** | Yes | Yes | |
| Archived cert visible? | **No** | **No** | Yes | Yes | |
| Internal tags visible? | No | No | Yes | Yes | |
| Edit button present? | No | No | Yes | Yes | |
| Delete button present? | No | No | No | Yes | |

**Sign-off: ________ Date: ________**

---

## Final Summary

| Section | Total Tests | Pass | Fail | Blocked | Skip |
|---------|-----------|------|------|---------|------|
| P1 - Critical | 63 | | | | |
| P2 - High | 49 | | | | |
| P3 - Medium | 16 | | | | |
| P4 - Low/Future | 18 | | | | |
| E2E Scenarios | 25 | | | | |
| **TOTAL** | **171** | | | | |

### Overall UAT Result: PASS / FAIL

**Tester Signature:** _________________________ **Date:** _____________

**Reviewer Signature:** ________________________ **Date:** _____________

---

*Note: This checklist is a condensed version of the full 662-test COMPREHENSIVE_UAT_GUIDE.md. For detailed steps, expected results, and additional scenarios, refer to the full guide.*

*Generated: February 19, 2026 | GCC Database Solution - PointSolve Technology*

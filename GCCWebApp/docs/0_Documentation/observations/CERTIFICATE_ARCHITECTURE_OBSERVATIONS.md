# Certificate Architecture Deep Analysis - Observations Report

**Date:** December 11, 2025
**Analyst:** Claude Code
**Scope:** Certificate Create, Edit, Admin, and Tile View Pages
**Status:** RESOLVED - Status alignment completed

## Resolution Summary (December 11, 2025)

The critical status mismatch issue has been **FIXED**. Changes made:

| File | Change |
|------|--------|
| `Models/CertificateStatus.cs` | Replaced enum with 14 documented states from DATA_VALIDATION_RULES.md |
| `Models/Certificate.cs` | Changed default status from "active" to "Draft" |
| `Views/Certificate/Create.cshtml` | Dynamic status dropdown via ViewBag.Statuses |
| `Views/Certificate/Edit.cshtml` | Dynamic status dropdown via ViewBag.Statuses |
| `Controllers/CertificateController.cs` | Added role-based status loading |
| `Services/CertificateWorkflowService.cs` | Updated to PascalCase status values |

**Build Status:** Succeeded (0 errors, 0 warnings)

---

---

## Executive Summary

The certificate management system has significant architectural and usability gaps between different views. The **Admin/Management pages** (Create, Edit) are **NOT aligned** with:
1. The 14-state workflow system defined in `CertificateStatus.cs`
2. The `CertificateWorkflowService` business rules
3. Modern UX best practices for enterprise forms
4. The tile view presentation patterns

**Key Finding:** Users can set ANY status value in Create/Edit forms, completely bypassing the sophisticated 14-state workflow system that was implemented. This creates data integrity issues and workflow inconsistencies.

---

## 1. CRITICAL STATUS MISMATCH

### The Problem (Severity: HIGH)

**Create.cshtml (Lines 126-134)** and **Edit.cshtml (Lines 127-136)** have HARD-CODED status options:

```html
<!-- What the forms show -->
<option value="Draft">Draft</option>
<option value="Client Approved - Not Paid">Client Approved - Not Paid</option>
<option value="Client Approved - Confidential">Client Approved - Confidential</option>
<option value="Client Approved - Paid">Client Approved - Paid</option>
<option value="Hold">Hold</option>
<option value="Active" selected>Active</option>
```

**CertificateStatus.cs (Lines 9-52)** defines the ACTUAL workflow:

```csharp
// What the system expects (14 states)
Draft, ApplicationSubmitted, UnderReview, TechnicalReview,
ComplianceVerification, FinalApproval, Active, ExpiringSoon,
RenewalInProgress, Expired, Suspended, Archived, Cancelled, ClientDeclined
```

**CertificateStatusHelper.cs (Lines 86-93)** - What creation SHOULD allow:

```csharp
public static Dictionary<string, string> GetCreationStatuses()
{
    return new Dictionary<string, string>
    {
        { "draft", "Draft" },
        { "application_submitted", "Application Submitted" }
    };
}
```

### Impact

| Issue | Consequence |
|-------|-------------|
| Form values don't match enum | Invalid data stored in database |
| Workflow rules bypassed | Certificates skip required review steps |
| Inconsistent casing | "Draft" vs "draft" - string comparison fails |
| Missing statuses | 8 workflow states not available in forms |
| Extra statuses | "Client Approved - Not Paid" doesn't exist in enum |

### Recommendation

Replace hard-coded status dropdowns with dynamic loading from `CertificateStatusHelper`:

```csharp
// Controller should pass
ViewBag.Statuses = CertificateStatusHelper.GetStatusesForRole(User.GetUserRole());
```

---

## 2. WORKFLOW SERVICE NOT INTEGRATED WITH FORMS

### The Problem (Severity: HIGH)

The sophisticated `CertificateWorkflowService` (312 lines) implements:
- Valid transition rules (line 115-138)
- Role-based status restrictions (line 98-110)
- Audit trail logging (line 202-253)
- Business logic hooks (line 258-310)

**But the Create/Edit forms do NOT call this service!**

**CertificateController.cs** Edit POST action:
- Directly sets `certificate.Status = model.Status`
- No validation against `IsValidTransition()`
- No call to `TransitionCertificateStatusAsync()`
- No audit trail for status changes through forms

### Impact

1. **No Workflow Enforcement**: User can change "Active" to "Draft" (violates workflow)
2. **No Audit Trail**: Manual status changes not logged to `CertificateStatusHistory`
3. **No Business Logic**: Partner notifications, PDF generation not triggered
4. **No Role Validation**: External user might set "Final Approval" if they access form

### Recommendation

Modify Edit POST to use workflow service:

```csharp
if (model.Status != existingCertificate.Status)
{
    var result = await _workflowService.TransitionCertificateStatusAsync(
        certificate.Id, model.Status, GetCurrentUserId());

    if (!result.Success)
    {
        ModelState.AddModelError("Status", result.ErrorMessage);
        return View(model);
    }
}
```

---

## 3. CREATE/EDIT FORM USABILITY ISSUES

### 3.1 Form Layout (Single Page Overload)

**Current State:**
- Create: 393 lines, 15+ fields on one page
- Edit: 461 lines with inline JavaScript
- Sidebar cluttered with tags + preview
- No clear visual hierarchy

**Best Practice Violations:**
| Violation | Location | Issue |
|-----------|----------|-------|
| No form sections | Create.cshtml:58 | All fields in single grid |
| No progress indicator | Both | Users don't know completion % |
| No save drafts | Create.cshtml | Data lost on navigation |
| No inline validation | Both | Errors only on submit |
| No character counters | CertificationResults | 1000 char limit invisible |

### 3.2 Field Organization Issues

**Current Order (Create.cshtml):**
1. Company (line 61)
2. Certification # (line 76)
3. Certification Type (line 82)
4. Product Name (line 97)
5. Dates (line 112-122)
6. Status (line 124)
7. Partner (line 138)
8. Certification Results (line 149)
9. Location/Country (line 156-173)

**Problem:** Status is in the middle of basic info - users may accidentally change it.

**Recommended Order:**
1. **Section: Identity** - Company, Product, Cert #, Cert Type
2. **Section: Dates** - Effective, Expiration (with validation)
3. **Section: Classification** - Status, Partner
4. **Section: Details** - Location, Country, Results
5. **Section: Attachments** - PDF, Product IDs, Tags

### 3.3 Date Validation Gap

**Create.cshtml (Lines 113-122):**
```html
<input asp-for="EffectiveDate" type="date" required />
<input asp-for="ExpirationDate" type="date" required />
```

**JavaScript (certificate-create.js:344-366):**
```javascript
// Date validation exists but...
if (expiration <= effective) {
    expirationDate.setCustomValidity('Expiration date must be after effective date');
}
```

**Issue:**
- Uses `<=` instead of `<` (same date should be invalid)
- No minimum date (can set dates in the past)
- No maximum date range (10-year certificate?)
- Server-side validation missing

### 3.4 Partner Conditional Fields

**Requirement (from CLAUDE.md):**
> Amazon talked about us passing this to ensure the cert meets their threshold

**Current Implementation:**
- Partner dropdown has: None, Amazon, Wayfair, Other
- CertificationResults is always optional
- No conditional requirement when Partner = Amazon

**Should Be:**
- When Partner = "Amazon" or "Wayfair": CertificationResults becomes REQUIRED
- Show helper text explaining partner requirements
- Validate minimum content length

---

## 4. TILE VIEW VS FORM DISCONNECT

### 4.1 Action Button Inconsistency

**CertificateTileViewInternal.cshtml:**
- Edit button visible for Admin/Analyst
- Download button visible for all
- 21-column grid including Internal Tags

**Create/Edit Forms:**
- No visual consistency with tile view
- Different color scheme (no green headers)
- Different button styling

### 4.2 Status Display Mismatch

**Tile Views show:**
- Status badges with colors (Active: green, Draft: yellow, Expired: red)
- snake_case values: "active", "draft", "expired"

**Forms show:**
- Plain dropdown with Title Case: "Active", "Draft", "Expired"
- No color coding in status selection

### 4.3 Navigation Flow Problems

**Current Flow:**
```
Tile View → Edit → [Save] → Details Page → [?] → Tile View
```

**Expected Flow:**
```
Tile View → Edit → [Save] → Back to Tile View (with success toast)
```

**Issue:** After edit, user is dumped on Details page, must manually navigate back.

---

## 5. JAVASCRIPT ARCHITECTURE ISSUES

### 5.1 Custom Modal vs AdminCore

**certificate-create.js (Lines 11-69):**
```javascript
const ModalSystem = {
    showConfirmation: function(options) {
        // Custom modal implementation
        const modalHtml = `<div class="modal-overlay">...`;
    }
}
```

**Requirement (from CLAUDE.md):**
> ALL feedback must use AdminCore.showConfirmation() and AdminCore.showNotification()

**Violation:** Custom `ModalSystem` is used instead of `AdminCore`, creating:
- Inconsistent styling across app
- Different keyboard handling
- Different backdrop behavior
- Maintenance burden (two modal systems)

### 5.2 Missing AdminCore Integration

**certificate-create.js Line 77-81:**
```javascript
showNotification: function(message, type = 'info') {
    if (typeof AdminCore !== 'undefined' && AdminCore.showNotification) {
        AdminCore.showNotification(message, type);
        return;
    }
    // Falls back to custom implementation
}
```

**Issue:** Conditional check is good, but `showConfirmation` does NOT check for AdminCore.

---

## 6. DATA INTEGRITY RISKS

### 6.1 Certificate Number Format

**Model expectation:** `XX-XXXX` (e.g., "24-0001")
**Form input:** `maxlength="7"` only
**Validation:** NONE for format

**Risk:** Users can enter "ABCDEFG", "1234567", or any 7-char string.

**Solution:**
```javascript
// Add pattern validation
<input pattern="[0-9]{2}-[0-9]{4}" />
```

### 6.2 Required Field Enforcement

**Create Form Required Fields:**
- Company, Number, CertificationType, Product - OK
- EffectiveDate, ExpirationDate - OK
- Status - REQUIRED but wrong values
- Partner - NOT required (should be?)
- PDF Upload - Required on Create, Optional on Edit

**Issue:** PDF marked `required` in Create but model `CertificatePdfPath` is nullable.

### 6.3 Tag Handling

**Create.cshtml (Lines 194-235):**
- Tags stored in hidden inputs as JSON arrays
- No server-side validation of tag IDs
- Could inject invalid tag IDs

---

## 7. ACCESSIBILITY GAPS

### 7.1 Form Accessibility

| Element | Issue | Fix |
|---------|-------|-----|
| Status dropdown | No `aria-describedby` | Add description |
| Date inputs | No format hint | Add "MM/DD/YYYY" placeholder |
| Tag buttons | No ARIA labels | Add `aria-label="Remove tag"` |
| PDF upload zone | Good! Has keyboard handler | Keep |

### 7.2 Error Messaging

**Current:** Validation errors shown in red `<span>` elements
**Missing:**
- No `role="alert"` on error container
- No focus management (errors don't scroll into view)
- No screen reader announcement

---

## 8. PERFORMANCE CONCERNS

### 8.1 Form Load Time

**Create.cshtml loads:**
- Companies dropdown (ViewBag.Companies)
- Products dropdown (ViewBag.Products) - ALL products
- CertificationTypes dropdown
- External Tags
- Internal Tags
- _ProductIdManager partial
- 4 JavaScript files

**Issue:** If there are 1000+ products, dropdown becomes unusable.

**Solution:** Replace with autocomplete/search input.

### 8.2 Custom Fields Lazy Loading

**Good pattern (Lines 310-323):**
```javascript
$('#CertificationTypeId').change(function() {
    certificateCustomFields.loadFieldsForType(typeId);
});
```

Custom fields load on-demand - efficient.

---

## 9. SECURITY OBSERVATIONS

### 9.1 Authorization Gaps

**Create.cshtml (Line 5):**
```razor
ViewData["BodyClass"] = "role-admin";
```

This sets body class but doesn't enforce authorization. Controller should have:
```csharp
[Authorize(Policy = "AnalystOrAdmin")]
public IActionResult Create() { ... }
```

### 9.2 Delete Confirmation

**Edit.cshtml (Lines 299-304):**
```razor
@if (User.IsInRole("Admin"))
{
    <button onclick="window.CertificateEdit.confirmDelete(@Model.Id)">Delete</button>
}
```

**Good:** Admin-only delete
**Risk:** Client-side role check, ensure server-side validation exists

---

## 10. RECOMMENDED IMPROVEMENTS

### Priority 0: Critical (Do Immediately)

1. **Fix Status Dropdown** - Use `CertificateStatusHelper` values
2. **Integrate Workflow Service** - Validate transitions, log changes
3. **Server-Side Validation** - Add model validation for status transitions
4. **Fix Modal Usage** - Replace custom modals with `AdminCore`

### Priority 1: High (This Sprint)

5. **Multi-Step Form Wizard** - Break 15-field form into logical steps
6. **Conditional Required Fields** - CertificationResults for Amazon/Wayfair
7. **Date Validation Enhancement** - Add min/max dates, server validation
8. **Certificate Number Format** - Add regex pattern validation

### Priority 2: Medium (Next Sprint)

9. **Product Autocomplete** - Replace dropdown with search
10. **Form Draft Saving** - Auto-save to localStorage
11. **Navigation Flow** - Return to tile view after save
12. **Progress Indicator** - Show form completion percentage

### Priority 3: Low (Backlog)

13. **Character Counters** - Add to CertificationResults
14. **Inline Validation** - Real-time field validation
15. **Status Color Picker** - Match tile view badge colors
16. **Accessibility Audit** - WCAG 2.1 AA compliance

---

## 11. COMPARISON MATRIX

| Feature | Create Form | Edit Form | Tile View | Workflow Service |
|---------|-------------|-----------|-----------|------------------|
| Status Values | 6 hard-coded | 7 hard-coded | 14 dynamic | 14 enum values |
| Status Format | Title Case | Title Case | snake_case | snake_case |
| Transition Rules | NONE | NONE | N/A | Full validation |
| Audit Logging | NONE | NONE | N/A | Full history |
| Role Filtering | NONE | Admin check for delete | Role-based columns | Full role support |
| AdminCore Usage | Partial | Partial | Full | N/A |

---

## 12. EFFORT ESTIMATION

| Fix | Complexity | Impact | Estimate |
|-----|------------|--------|----------|
| Status dropdown fix | Low | High | 2-4 hours |
| Workflow integration | Medium | Critical | 8-16 hours |
| Multi-step wizard | High | High | 24-40 hours |
| AdminCore migration | Low | Medium | 4-8 hours |
| Date validation | Low | Medium | 2-4 hours |
| Total P0/P1 | - | - | 40-72 hours |

---

## Appendix A: File References

| File | Line Numbers | Issue |
|------|--------------|-------|
| `Views/Certificate/Create.cshtml` | 126-134 | Wrong status values |
| `Views/Certificate/Edit.cshtml` | 127-136 | Wrong status values |
| `Models/CertificateStatus.cs` | 9-52 | Correct enum |
| `Models/CertificateStatus.cs` | 86-93 | Creation statuses |
| `Services/CertificateWorkflowService.cs` | 115-138 | Transition rules |
| `wwwroot/js/certificate-create.js` | 11-69 | Custom modal |
| `wwwroot/js/certificate-create.js` | 344-366 | Date validation |

---

## Appendix B: Design Mockup Recommendations

A modern certificate management form should have:

1. **Stepper Header** - Shows current step (1/4, 2/4, etc.)
2. **Section Cards** - Group related fields with clear headings
3. **Status Workflow Visual** - Show current status in a visual pipeline
4. **Sidebar Summary** - Show certificate preview as form is filled
5. **Action Bar** - Fixed bottom bar with Save, Cancel, Next buttons
6. **Validation Feedback** - Inline errors with icons and colors

---

*Document Version: 1.0*
*Generated by Claude Code Deep Analysis*

---

## Observation Entry: February 5, 2026

### Amazon API Expanded Requirements - Scope Change Assessment

**Context:** Amazon (Kyle Bowman) requested expanded API fields to move from binary yes/no certification to analog percentage-based data. Carly Povilaitis raised concerns about complexity, especially with SEP certifications having multiple pathways.

**Key Findings:**
1. The system already has ~70% of the foundation built (CertificationResults, GS1 endpoint, AmazonCode on CertificationType, partner filtering, product IDs)
2. The PRIMARY gap is a **Pathway** entity - a new admin-controlled dropdown that gives context to the percentage in CertificationResults
3. CertificationType Subcategory already works (Parent/Child hierarchy in DB)
4. GS1 compliance is ALREADY DONE - major differentiator
5. API currently returns 20 fields; Amazon needs ~21-22 (only Pathway and AmazonCode are missing from response)

**Risks Identified:**
- Scope creep from 8 simultaneous recommendations (phased approach recommended)
- Carly explicitly warned against cert-type-specific fields
- Validation must be soft (warnings) initially, not hard (blocking)
- Existing data must not be retroactively invalidated

**Decision Needed:**
- Answers to stakeholder questions in Section 7 of scope change document
- Phase approval before any development begins

**Document Created:** `docs/0_Documentation/docs/SCOPE_CHANGE_Amazon_API_Requirements_2026-02-05.md`

**Changes Requested vs. Requirements Alignment:**
| Recommendation | Aligns with Original Requirements? | New Scope? |
|---------------|-----------------------------------|------------|
| Pathway field | NO - entirely new Amazon request | YES |
| Subcategory improvements | Partially - architecture exists | MINOR |
| Expand API to 18+ fields | YES - was planned | NO |
| GS1 structures | YES - already built | NO |
| UI for new fields | Partially - CertResults was planned | MINOR |
| API filtering enhancements | YES - natural extension | MINOR |
| Export/view updates | YES - expected evolution | MINOR |
| Validation layer | NO - new Amazon-driven requirement | YES |

**Bottom Line:** 2 items are genuinely NEW scope (Pathway + Validation). The rest is exposing/connecting what already exists. This is manageable if phased correctly.

### Revision: February 5, 2026 (v2.0)

**Scope change document revised to leaner approach after critical review.**

Key changes from v1.0 to v2.0:
- Removed inflated REC-3 ("expand to 18+ fields") -- API already returns 20 fields, only 2 are missing
- Removed REC-4 (GS1 structures) -- already fully implemented, no work needed
- Removed REC-6 over-engineering ("filter by required vs optional fields") -- Amazon filters on their side
- Removed REC-8 forced numeric validation -- breaks legitimate data formats like "UL 2809 Level 2"
- Consolidated 8 recommendations into 3 focused priorities
- Added 3 Pathway design options (A: Lookup Table, B: Free Text, C: Extend Hierarchy) with pros/cons
- Added client-facing questions table to gate development decisions
- Document reformatted for client delivery with approval section

**Design Recommendation:** Option A (Standalone Pathway Lookup Table with optional cert-type scoping) balances flexibility with data consistency. Allows admin to manage values, scopes pathways to relevant cert types, and avoids free-text inconsistency that would degrade API quality for Amazon.

---

### Phase 1 Implementation - Data Visibility (February 5, 2026)

**Phase 1 of the Amazon API scope change has been implemented.** No database schema changes required. All changes are additive.

#### Changes Made

| File | Change |
|------|--------|
| `Models/CertificateViewModels.cs` | Added `CertificationResults` and `CertificationTypeSubcategory` properties to `CertificateCardModel` |
| `Controllers/Api/CertificateTileInternalApiController.cs` | Added `CertificationResults` and `CertificationTypeSubcategory` to raw query and formatted projections |
| `Controllers/Api/CertificateTileExternalApiController.cs` | Added `CertificationResults` and `CertificationTypeSubcategory` to raw query and formatted projections |
| `Views/Certificate/CertificateTileViewInternal.cshtml` | Added "Cert Subcategory" and "Cert Results" grid header columns after Partner |
| `Views/Certificate/CertificateTileViewExternal.cshtml` | Added "CSF Attributes" header (alignment fix), "Cert Subcategory" and "Cert Results" headers |
| `wwwroot/js/certificate-tile-view-internal.js` | Added `certificationResults` and `certificationTypeSubcategory` variables and grid-cells in `createTableRow()` |
| `wwwroot/js/certificateTileViewExternal.js` | Added `certificationResults` and `certificationTypeSubcategory` variables and grid-cells in `createTableRow()` |
| `wwwroot/css/pages/certificate-tile-internal.css` | Updated grid to 24 columns (was 22), min-width 2785px |
| `wwwroot/css/pages/certificate-tile-external.css` | Updated grid to 24 columns (was 21), min-width 2775px, fixed CSF Attributes column alignment |
| `Services/CertificateService.cs` | Added fields to both query projections and CSV/Excel export methods |
| `Controllers/Api/v1/CertificatesController.cs` | Added `amazonCode` to standard, includeProductIds, and search projections |
| `Models/Api/GS1CertificationReport.cs` | Added `AmazonCode` property to `GS1CertificationInfo` (null-excluded for non-Amazon certs) |

#### Observations

- **Pre-existing bug found:** External tile view JS (`certificateTileViewExternal.js`) had a `csfAttributes` cell in `createTableRow()` but no matching header in the CSHTML or CSS column. This caused column misalignment. Fixed by adding the missing "CSF Attributes" header and CSS column.
- **Subcategory fallback:** Both the collection (`CertificationTypeSubcategories`) and deprecated single FK (`CertificationTypeSubcategory`) are queried with fallback logic, ensuring certificates created before the multi-select feature still show their subcategory data.
- **No breaking changes:** All additions are new optional fields. Existing API consumers are unaffected.

---

### Amazon API Change Order - Email Thread Analysis (February 5, 2026)

#### Context

Change order document created for Amazon's expanded CPF requirements. Email thread between Kyle (Amazon), Carly (GreenCircle), and Dan (internal) shaped the final scope.

#### Key Confirmations from Email Thread

| Item | Source | Detail |
|------|--------|--------|
| Amazon will use `certificationResults` | Kyle (Amazon) | Two purposes: (1) customer-facing display of quantitative values, (2) internal CPF threshold filtering |
| Amazon wants energy saving % and recycled content % | Kyle (Amazon) | Moving from binary "certified = eligible" to analog "certified with X% savings" |
| Amazon filters on their side | Kyle (Amazon) | "Filtering which products are eligible for CPF on our side" -- they consume raw data and apply their own thresholds |
| Pathway field is needed | Carly (GreenCircle) | For SEP certs, same % can relate to different methodologies (Design Efficiency vs. Consumer Behavior) |
| Admin-managed dropdown for Pathway | Carly (GreenCircle) | "Pathway field with dropdowns (that Admin can control)" |
| Avoid cert-type-specific fields | Carly (GreenCircle) | "Things are always changing" -- generic approach required |
| Certs only tagged for Amazon if they meet thresholds | Carly (GreenCircle) | Non-qualifying certs never appear in Amazon's API results |

#### Scope Evolution

- **Original proposal:** 8 changes, including Pathway (speculative at the time)
- **After Dan's feedback:** Questioned whether CertResults was necessary -- valid concern, but Amazon confirmed need
- **After Amazon's response:** Pathway appeared unnecessary (Amazon didn't ask for it)
- **After Carly's response:** Pathway brought back -- needed for SEP methodology context
- **Final scope:** All 8 changes confirmed, with 5 pending questions for GreenCircle before Changes #5-8 can proceed

#### Risk Observation

The "building features Amazon does not consume" risk from the original document has been significantly reduced. Amazon explicitly confirmed how they will use `certificationResults`, and GreenCircle independently confirmed Pathway is needed for their own data quality. Both parties have aligned interests.

#### Documents Updated

- `docs/0_Documentation/docs/PHASE1_IMPLEMENTATION_SUMMARY.md` -- Markdown change order (updated with confirmations)
- `docs/0_Documentation/docs/PROPOSED_CHANGES_Amazon_API_2026-02-05.html` -- HTML change order for PDF export (updated with confirmations)

---

### Observation Entry: February 19, 2026

#### Comprehensive UAT Guide Created - Testing Gap Analysis

**Context:** User requested review of existing user guides, testing documentation, and user flow coverage across all roles. Comprehensive audit performed.

**What Existed Before:**
- Role Management Help Guide (good coverage of RBAC)
- UAT Credentials Guide (good)
- Certificate Create/Edit Flow (developer-focused, Mermaid diagrams)
- Import Test Guide (20+ test cases, import-only)
- API Testing Plan (100+ test IDs, partner API only)
- Partner API Testing Guide (40+ tests)
- Pre/Post Deployment Checklists

**Gaps Identified:**
1. NO unified UAT guide covering all roles end-to-end
2. NO step-by-step test scripts for Admin CRUD operations (Users, Roles, Tags, Partners, etc.)
3. NO External user test guide (data isolation verification missing)
4. NO 14-state workflow transition test matrix
5. NO cross-role permission verification matrix
6. NO end-to-end business scenario tests (multi-role journeys)
7. NO Certificate Tile View validation guide per role
8. NO CSV Export test coverage across entities
9. NO notification pattern validation tests (alert vs. AdminCore)
10. NO requirements traceability matrix

**Document Created:** `docs/0_Documentation/docs/COMPREHENSIVE_UAT_GUIDE.md` (v2.0)
- **Part A:** 359 feature-level test cases across 10 modules (Auth, Public, External, Analyst, Admin, Workflow, API, Permissions, Export/Import, Edge Cases)
- **Part B:** 221 end-to-end scenario steps across 9 business journeys:
  - E2E Scenario 1: Certificate Full Lifecycle (Admin→Analyst→Public→External→API, 46 steps)
  - E2E Scenario 2: New Company Onboarding (26 steps)
  - E2E Scenario 3: Partner API Integration (18 steps)
  - E2E Scenario 4: Admin System Setup Day 1 (27 steps)
  - E2E Scenario 5: Certificate Renewal Lifecycle (14 steps)
  - E2E Scenario 6: Rejection & Recovery Flow (14 steps)
  - E2E Scenario 7: Bulk Operations Workflow (24 steps)
  - E2E Scenario 8: User Access Lifecycle (26 steps)
  - E2E Scenario 9: Search & Discovery Cross-Role (26 steps)
- **Part C:** Requirements Traceability Matrix + Defect Template + Execution Summary
- **Total:** 580 test cases/steps across 19 modules

**Key Insight:** The existing docs were developer/technical-focused. What was missing was a document a stakeholder, QA tester, or client could pick up and systematically validate the entire system. This guide fills that gap.

### Observation Entry: February 19, 2026 (continued)

#### UAT Guide v3.0 - Gap Fixes, Accessibility Module, Test Data Script

**Context:** Detailed section-by-section walkthrough of the UAT guide revealed several inaccuracies and gaps. Code research confirmed the issues. Three updates applied:

**Gap Fixes Applied:**
1. **Analyst Status Dropdown (TC-ANL-034, TC-WF-043):** Guide incorrectly stated analysts see "Draft, Active, Expired, Pending Review". Code research of `CertificateStatusHelper.GetStatusesForRole()` confirmed analysts see **12 of 14 statuses** (all except Rejected and Revoked). Fixed.
2. **Certificate Import Access (TC-ANL-059-065 → TC-ADM-075-081):** Guide placed Certificate Import under Analyst module. Code research showed `CertificateImportController` has `[Authorize(Policy = "AdminOnly")]` at class level. Moved entire section to Admin module. Updated TC-PERM-019 from Analyst=Allowed to Analyst=**Denied**.
3. **Missing Test Cases Added:**
   - TC-PUB-013a/b/c: Draft/Archived not visible to public + database-driven filters
   - TC-ANL-061/62/63: Cascading company→product dropdown + filter validation
   - TC-ADM-085-089: Delete entity with dependencies protection
   - TC-ADM-090-092: Duplicate entity prevention
4. **Section Reference Fix:** "How to Use" pointed to old section numbers (15/16) instead of correct (25/26).

**Accessibility Module Added (Module 20):**
- 50 test cases (TC-A11Y-001 to TC-A11Y-050) covering WCAG 2.1 Level AA
- 8 subsections: Keyboard Nav, Screen Reader, Color Contrast, ARIA, Forms, Modals, Responsive/Zoom, Data Tables
- Based on actual codebase inspection of ARIA attributes, admin-sr-only labels, and button classes

**Test Data Setup Script Created:**
- `docs/0_Documentation/docs/UAT_TEST_DATA_SETUP.sql`
- Creates all pre-condition data for E2E scenarios: 3 partners, 4 companies, 6 products, 8 certificates (various statuses), 7 tags, 3 cert types with 5 subcategories, 3 custom fields, product IDs, status history
- Idempotent (uses IF NOT EXISTS for all inserts)
- Includes verification queries

**Updated Totals:** v3.0 = **647 test cases** across 20 modules (Part A: 376, Part B: 221, Part B2: 50 accessibility)

**Risk Note:** The analyst status error could have led to incorrect pass/fail judgments during UAT. The Certificate Import access error would have wasted testing time (analysts would get Access Denied). Both caught through code verification before testing began - validates the importance of code-verified test cases.

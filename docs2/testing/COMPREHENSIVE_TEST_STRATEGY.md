# GCC Database Solution — Comprehensive Test Strategy

**Version:** 1.0
**Date:** 2026-03-23
**Author:** Anuki Consulting
**Status:** Active

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current State Assessment](#2-current-state-assessment)
3. [Test Strategy](#3-test-strategy)
4. [Test Categories](#4-test-categories)
5. [Detailed Test Cases](#5-detailed-test-cases)
6. [High-Risk Areas](#6-high-risk-areas)
7. [Automation vs Manual Recommendations](#7-automation-vs-manual-recommendations)
8. [Priority Execution Order](#8-priority-execution-order)
9. [Test Infrastructure Fixes](#9-test-infrastructure-fixes)

---

## 1. Executive Summary

After 12+ months of development, the GCC platform has reached 93% feature completeness but only 74.9% test pass rate (721/962). The client UAT on 3/13/2026 surfaced 80+ defects. Key gaps exist in:

- **Role-based access enforcement** — policies exist but edge cases are untested
- **Certificate workflow transitions** — 14-state machine with invalid/conflicting state paths unexplored
- **Error handling and validation** — happy-path tested, failure modes largely ignored
- **E2E and accessibility** — 42.9% and 31.5% pass rates due to infrastructure issues
- **API authentication** — 12+ tests failing with 401 errors

This strategy addresses those gaps with 300+ test cases organized by risk, automatable vs manual, and execution priority.

---

## 2. Current State Assessment

### 2.1 Test Project Health

| Project | Tests | Pass Rate | Blocker |
|---------|-------|-----------|---------|
| GCCWebApp.Tests | 784 | 79.8% | 106 failures need triage |
| GCCWebApp.Api.Tests | 81 | 63.0% | Auth setup broken |
| GCCWebApp.Security.Tests | 29 | 72.4% | 8 failures |
| GCCWebApp.E2E.Tests | 14 | 42.9% | App not running on expected port |
| GCCWebApp.Accessibility.Tests | 54 | 31.5% | App not running on port 7130 |
| GCCWebApp.UAT.Tests | — | BUILD FAIL | Logger URI config error |
| GCCWebApp.RoleBasedUI.Tests | — | Unknown | Needs verification |
| GCCWebApp.BDD.Tests | — | Unknown | Needs verification |

### 2.2 Known Unresolved Issues

| ID | Issue | Severity |
|----|-------|----------|
| ISSUE-001 | API auth tests return 401 (MultiPartnerApiIntegrationTests) | High |
| ISSUE-006 | E2E tests — net::ERR_CONNECTION_REFUSED (port mismatch) | Medium |
| ISSUE-007 | Accessibility tests — same port mismatch | Medium |
| ISSUE-008 | UAT tests build error (logger URI) | Low |
| CLIENT-QC | 80+ open defects from 3/13/2026 UAT | High |

### 2.3 Coverage Gaps

| Area | Current Coverage | Gap |
|------|-----------------|-----|
| Certificate CRUD happy path | Good | — |
| Certificate workflow edge cases | Minimal | Invalid transitions, concurrent edits, expired cert handling |
| Role-based data isolation | Partial | External user seeing internal data, boundary conditions |
| File upload validation | Basic | Malformed PDFs, oversized files, concurrent uploads |
| Custom field rendering | None | Dynamic fields per cert type, visibility per role |
| API rate limiting | None | Burst behavior, limit reset, concurrent keys |
| Error recovery | None | Database failures, file system errors, partial saves |
| Browser compatibility | None | Mobile views, infinite scroll, autocomplete |

---

## 3. Test Strategy

### 3.1 Testing Pyramid

```
                    ┌─────────┐
                    │  Manual  │  ← Exploratory, usability, visual
                    │  (15%)   │
                ┌───┴─────────┴───┐
                │   E2E / UI      │  ← Playwright: critical user journeys
                │   (15%)         │
            ┌───┴─────────────────┴───┐
            │  Integration Tests      │  ← API, DB, auth, workflow
            │  (30%)                  │
        ┌───┴─────────────────────────┴───┐
        │  Unit Tests                     │  ← Services, validators, helpers
        │  (40%)                          │
        └─────────────────────────────────┘
```

### 3.2 Guiding Principles

1. **Fix infrastructure before writing new tests.** E2E and Accessibility suites are dead weight at 43% and 32%. Fix the port/startup issues first.
2. **Test the boundaries, not just the center.** Every role boundary, every state transition edge, every validation limit.
3. **Prioritize by blast radius.** A broken role check affects every user. A CSS glitch affects one page.
4. **Automate the repeatable, manually test the subjective.** Usability, visual layout, and exploratory testing stay manual.
5. **Regression-first.** Every client QC defect that gets fixed must have a regression test before the fix is marked done.

### 3.3 Test Data Strategy

| Dataset | Purpose | Location |
|---------|---------|----------|
| Seed SQL | Baseline certificates, users, companies | `docs/testing/UAT_TEST_DATA_SETUP.sql` |
| Edge-case fixtures | Expired certs, locked users, max-length fields | New: `docs/testing/EDGE_CASE_TEST_DATA.sql` |
| File fixtures | Valid PDF, corrupt PDF, oversized PDF, non-PDF | New: `docs/testing/test-files/` |
| API fixtures | Valid/invalid/expired API keys | Seeded in test setup |

### 3.4 Environment Requirements

| Environment | URL | Purpose |
|-------------|-----|---------|
| Local Dev | `http://localhost:5101` | Unit + integration tests |
| E2E Target | `https://localhost:7001` | Playwright E2E tests |
| Accessibility Target | `http://localhost:7130` | axe-core accessibility tests |
| Production | `greencirclecertified.com` | Smoke tests only |

---

## 4. Test Categories

### Category 1: Authentication & Session Management
### Category 2: Role-Based Access Control (RBAC)
### Category 3: Certificate CRUD & Validation
### Category 4: 14-State Workflow Engine
### Category 5: File Upload & Document Management
### Category 6: Certificate Views (Public / External / Internal)
### Category 7: Custom Fields & Dynamic Forms
### Category 8: Product & Company Cascading
### Category 9: Search, Filter, Sort & Pagination
### Category 10: API Endpoints & Partner Integration
### Category 11: Admin Management Pages
### Category 12: Error Handling & Recovery
### Category 13: Usability & UI Consistency
### Category 14: Accessibility (WCAG 2.1 AA)
### Category 15: Performance & Load
### Category 16: Security

---

## 5. Detailed Test Cases

---

### CAT-01: Authentication & Session Management

#### 01-001: Login — Valid credentials per role
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to `/Account/Login` | Login form renders |
| 2 | Enter `admin` / `TestPass123!` | Redirect to Dashboard |
| 3 | Repeat for `analyst`, `external` | Each redirects to role-appropriate landing |

**Type:** Automated (E2E)
**Priority:** P1

#### 01-002: Login — Invalid password
| Step | Action | Expected |
|------|--------|----------|
| 1 | Enter valid username, wrong password | Error message displayed |
| 2 | Verify no redirect occurs | Stays on login page |
| 3 | Verify failed login attempt counter increments | `FailedLoginAttempts` +1 in DB |

**Type:** Automated (Integration)
**Priority:** P1

#### 01-003: Account lockout after N failed attempts
| Step | Action | Expected |
|------|--------|----------|
| 1 | Submit wrong password N times | Account locked (`LockedUntil` set) |
| 2 | Attempt with correct password while locked | Access denied with lockout message |
| 3 | Wait for lockout to expire | Login succeeds |

**Type:** Automated (Integration)
**Priority:** P1

#### 01-004: "Remember Me" cookie persistence
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login with Remember Me checked | Cookie expiry = 14 days |
| 2 | Login without Remember Me | Cookie expiry = 30 minutes |
| 3 | Close browser with Remember Me, reopen | Session persists |
| 4 | Close browser without Remember Me, reopen | Session expired, login required |

**Type:** Automated (E2E)
**Priority:** P1

#### 01-005: Session expiration and sliding window
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login, idle for 30+ minutes | Session expires, redirect to login |
| 2 | Login, interact at 25 minutes | Session extended another 30 min |

**Type:** Manual
**Priority:** P2

#### 01-006: Concurrent session enforcement
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login on Browser A | Session established |
| 2 | Login same user on Browser B | Behavior depends on `MaxConcurrentSessions` |
| 3 | If max=1, verify Browser A session invalidated | Redirect to login on next action |

**Type:** Manual
**Priority:** P2

#### 01-007: Two-factor authentication — all methods
| Step | Action | Expected |
|------|--------|----------|
| 1 | Enable 2FA (Email) → login | Email code prompt appears |
| 2 | Enter valid code | Login succeeds |
| 3 | Enter expired/invalid code | Access denied |
| 4 | Repeat for SMS, TOTP, Both | Each method functions correctly |
| 5 | Use recovery code | Login succeeds, code consumed |

**Type:** Automated (Integration) + Manual (SMS/Email delivery)
**Priority:** P1

#### 01-008: Password expiration enforcement
| Step | Action | Expected |
|------|--------|----------|
| 1 | Set `PasswordExpiresAt` to past date | Login redirects to password change |
| 2 | Set `RequirePasswordReset = true` | Login redirects to password change |
| 3 | After password change | Normal access restored |

**Type:** Automated (Integration)
**Priority:** P2

#### 01-009: Azure AD B2C authentication flow
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click "Sign in with Azure AD B2C" | Redirect to B2C login page |
| 2 | Complete Azure login | Callback to `/Account/SignedIn`, claims mapped |
| 3 | Trigger password reset (AADB2C90118) | Redirect to B2C password reset policy |

**Type:** Manual (requires Azure environment)
**Priority:** P1

#### 01-010: Login — SQL injection attempt
| Step | Action | Expected |
|------|--------|----------|
| 1 | Enter `' OR 1=1 --` as username | Login fails, no error leak |
| 2 | Enter `'; DROP TABLE Users; --` | Login fails, database intact |

**Type:** Automated (Security)
**Priority:** P1

#### 01-011: Login — XSS attempt
| Step | Action | Expected |
|------|--------|----------|
| 1 | Enter `<script>alert('xss')</script>` as username | Input sanitized, no script execution |
| 2 | Verify error message does not reflect raw input | HTML-encoded output |

**Type:** Automated (Security)
**Priority:** P1

#### 01-012: IP whitelist enforcement
| Step | Action | Expected |
|------|--------|----------|
| 1 | Set `AllowedIPs` for user to specific IP | Login from that IP succeeds |
| 2 | Login from different IP | Access denied |
| 3 | Set `AllowedIPs` to empty/null | Login from any IP succeeds |

**Type:** Automated (Integration)
**Priority:** P3

---

### CAT-02: Role-Based Access Control (RBAC)

#### 02-001: Admin — full access verification
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as admin | Access to all routes |
| 2 | Navigate to `/Admin/Index` | 200 OK |
| 3 | Navigate to `/Certificate/Create` | 200 OK |
| 4 | Navigate to `/Certificate/CertificateTileViewInternal` | 200 OK |
| 5 | Navigate to `/Admin/UserManagement` | 200 OK |

**Type:** Automated (E2E)
**Priority:** P1

#### 02-002: Analyst — permitted and denied routes
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as analyst | Dashboard loads |
| 2 | Navigate to `/Certificate/Create` | 200 OK |
| 3 | Navigate to `/Certificate/Edit/{id}` | 200 OK |
| 4 | Navigate to `/Admin/UserManagement` | 403 Forbidden |
| 5 | Navigate to `/Admin/Index` | 403 Forbidden |

**Type:** Automated (E2E)
**Priority:** P1

#### 02-003: External user — restricted to assigned company only
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as external user assigned to Company A | Dashboard shows Company A data only |
| 2 | Navigate to `/Certificate/Create` | 403 Forbidden (no Add button visible) |
| 3 | Navigate to `/Certificate/Edit/{id}` | 403 Forbidden (no Edit button visible) |
| 4 | Navigate to `/Certificate/Delete/{id}` | 403 Forbidden |
| 5 | Access `/Product/Management` | Only Company A products shown |
| 6 | Attempt direct URL to Company B certificate | 403 or filtered out |

**Type:** Automated (E2E + Integration)
**Priority:** P1 — CRITICAL

#### 02-004: External user — internal tags hidden
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as external | Navigate to certificate detail |
| 2 | Certificate has both internal and external tags | Only external tags visible |
| 3 | Inspect page source | No internal tag data in HTML |

**Type:** Automated (E2E)
**Priority:** P1

#### 02-005: External user — no Actions column on Product Management
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as external | Navigate to `/Product/Management` |
| 2 | Verify table columns | Actions column absent |
| 3 | Verify no Add/Edit/Delete buttons | None present |

**Type:** Automated (E2E)
**Priority:** P1

#### 02-006: Unauthenticated — public access boundaries
| Step | Action | Expected |
|------|--------|----------|
| 1 | No login | Navigate to `/Certificate/CertificateTileView` | 200 OK |
| 2 | Navigate to `/Certificate/CertificateSingleView/{id}` | 200 OK (only active certs) |
| 3 | Navigate to `/Certificate/Create` | Redirect to login |
| 4 | Navigate to `/Admin/Index` | Redirect to login |
| 5 | Navigate to `/Dashboard` | Redirect to login |
| 6 | Only `ClientApprovedPaid` and `Republished` certs visible | No Draft/Hold/Expired certs shown |

**Type:** Automated (E2E)
**Priority:** P1

#### 02-007: Role escalation — direct URL manipulation
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as external | Copy admin URL |
| 2 | Paste `/Admin/UserManagement` directly | 403 Forbidden |
| 3 | Attempt POST to `/Admin/CreateUser` via curl/Postman | 403 Forbidden |
| 4 | Attempt POST to `/Certificate/Create` | 403 Forbidden |
| 5 | Modify cookie role claim value | Session invalidated or 403 |

**Type:** Automated (Security + Integration)
**Priority:** P1 — CRITICAL

#### 02-008: Role-specific dashboard content
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as admin | Dashboard shows all stats, all companies |
| 2 | Login as analyst | Dashboard shows all stats, all companies |
| 3 | Login as external | Dashboard shows only assigned company stats |

**Type:** Automated (E2E)
**Priority:** P2

#### 02-009: Custom field visibility by role
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create custom field with `RoleVisibility = Internal` | |
| 2 | View certificate as admin | Field visible |
| 3 | View certificate as analyst | Field visible |
| 4 | View certificate as external | Field NOT visible |
| 5 | View certificate as public (anonymous) | Field NOT visible |
| 6 | Repeat for `RoleVisibility = External` | Visible to external+, not public |
| 7 | Repeat for `RoleVisibility = Public` | Visible to all |

**Type:** Automated (Integration + E2E)
**Priority:** P1

#### 02-010: Anti-forgery token enforcement on POST
| Step | Action | Expected |
|------|--------|----------|
| 1 | Submit POST to `/Certificate/Create` without anti-forgery token | 400 Bad Request |
| 2 | Submit POST with expired token | 400 Bad Request |
| 3 | Submit POST with token from different session | 400 Bad Request |

**Type:** Automated (Security)
**Priority:** P1

---

### CAT-03: Certificate CRUD & Validation

#### 03-001: Create certificate — all required fields
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to `/Certificate/Create` | Form loads with all fields |
| 2 | Fill: Company, Product, CertificationType, EffectiveDate, ExpirationDate | |
| 3 | Upload valid PDF | |
| 4 | Submit | Certificate created, redirect to details |
| 5 | Verify certificate number auto-generated (format YY-XXXX) | |
| 6 | Verify status = Draft | |

**Type:** Automated (E2E)
**Priority:** P1

#### 03-002: Create certificate — missing required fields
| Step | Action | Expected |
|------|--------|----------|
| 1 | Submit form with no Company | Validation error on Company |
| 2 | Submit with no Product | Validation error on Product |
| 3 | Submit with no CertificationType | Validation error |
| 4 | Submit with no dates | Validation error |
| 5 | Verify form retains entered values after validation failure | Fields preserved |

**Type:** Automated (E2E)
**Priority:** P1

#### 03-003: Create certificate — date validation
| Step | Action | Expected |
|------|--------|----------|
| 1 | Set ExpirationDate before EffectiveDate | Validation error |
| 2 | Set ExpirationDate = EffectiveDate | Validation error (must be at least 1 day apart) |
| 3 | Set EffectiveDate far in past (2000-01-01) | Accepted (valid historical cert) |
| 4 | Set ExpirationDate far in future (2099-12-31) | Accepted |
| 5 | Leave EffectiveDate blank, fill ExpirationDate | Validation error |

**Type:** Automated (Integration + E2E)
**Priority:** P1

#### 03-004: Edit certificate — field persistence
| Step | Action | Expected |
|------|--------|----------|
| 1 | Open existing certificate for edit | All fields pre-populated |
| 2 | Change Company | Product dropdown reloads for new company |
| 3 | Change only CertificationType | Save succeeds, other fields unchanged |
| 4 | Verify certificate number unchanged after edit | Number immutable |
| 5 | Verify audit trail records the change | AuditHistory entry created |

**Type:** Automated (E2E + Integration)
**Priority:** P1

#### 03-005: Edit certificate — concurrent edit conflict
| Step | Action | Expected |
|------|--------|----------|
| 1 | User A opens Edit for cert X | Form loads |
| 2 | User B opens Edit for same cert X | Form loads |
| 3 | User A saves changes | Succeeds |
| 4 | User B saves changes | Either: concurrency error shown, OR last-write-wins with audit trail |

**Type:** Manual
**Priority:** P2

#### 03-006: Delete certificate — confirmation and soft delete
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click Delete on certificate | `AdminCore.showConfirmation()` modal appears |
| 2 | Cancel confirmation | Certificate still exists |
| 3 | Confirm deletion | `AdminCore.showNotification()` toast, certificate removed from list |
| 4 | Verify NO native `alert()` or `confirm()` used | Only AdminCore modals |

**Type:** Automated (E2E)
**Priority:** P1

#### 03-007: Delete certificate — cascade protection
| Step | Action | Expected |
|------|--------|----------|
| 1 | Delete certificate with associated ProductIds | ProductIds also removed or orphaned gracefully |
| 2 | Delete certificate with StatusHistory entries | History preserved or cleaned |
| 3 | Delete certificate with CustomFieldValues | Values cleaned up |

**Type:** Automated (Integration)
**Priority:** P2

#### 03-008: Certificate number uniqueness
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create two certificates in rapid succession | Both get unique numbers |
| 2 | Attempt to manually set duplicate number (if possible) | Validation error |

**Type:** Automated (Integration)
**Priority:** P1

#### 03-009: Certification Results field (optional)
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create certificate without Certification Results | Succeeds (field optional) |
| 2 | Create certificate with Certification Results filled | Value saved and displayed |
| 3 | Edit to add Certification Results | Value persists |
| 4 | Edit to clear Certification Results | Clears to null/empty |

**Type:** Automated (E2E)
**Priority:** P2

#### 03-010: Certificate — max length / boundary values
| Step | Action | Expected |
|------|--------|----------|
| 1 | Enter 50-char certificate number (max) | Accepted |
| 2 | Enter 51-char certificate number | Rejected |
| 3 | Enter extremely long company name | Truncated or rejected at MaxLength |
| 4 | Enter special characters in text fields | Properly escaped, no XSS |
| 5 | Enter Unicode / emoji in text fields | Handled gracefully |

**Type:** Automated (Integration)
**Priority:** P2

---

### CAT-04: 14-State Workflow Engine

#### 04-001: Valid transitions from Draft
| Step | Action | Expected |
|------|--------|----------|
| 1 | Start with Draft certificate | |
| 2 | Transition to ClientApprovedNotPaid | Succeeds |
| 3 | Transition to ClientApprovedConfidential | Succeeds |
| 4 | Transition to ClientApprovedPaid | Succeeds |
| 5 | Transition to Hold | Succeeds |
| 6 | Verify StatusHistory entry created | OldStatus=Draft, NewStatus=target, ChangedBy, ChangedAt |

**Type:** Automated (Integration)
**Priority:** P1

#### 04-002: Invalid transitions — every illegal path
| Step | Action | Expected |
|------|--------|----------|
| 1 | Draft → Expired | Rejected |
| 2 | Draft → Republished | Rejected |
| 3 | Draft → ExpiredNoAnnualVerification | Rejected |
| 4 | ClientApprovedPaid → Draft | Rejected |
| 5 | ClientApprovedPaid → ClientApprovedNotPaid | Rejected |
| 6 | Expired → ClientApprovedPaid | Rejected |
| 7 | ExpiredNoAnnualVerification → ClientApprovedPaid | Rejected |
| 8 | ExpiredNoAnnualVerification → Republished | Rejected |

**Type:** Automated (Unit + Integration)
**Priority:** P1 — CRITICAL

#### 04-003: Transition matrix — exhaustive
All 64 possible state pairs (8 × 8) tested. Valid transitions accept, invalid transitions reject.

| From \ To | Draft | NotPaid | Confid | Paid | Hold | Expired | Repub | ExpNoVerif |
|-----------|-------|---------|--------|------|------|---------|-------|------------|
| Draft | — | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ |
| NotPaid | ✗ | — | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Confid | ✗ | ✗ | — | ✓ | ✓ | ✓ | ✗ | ✗ |
| Paid | ✗ | ✗ | ✗ | — | ✓ | ✓ | ✓ | ✓ |
| Hold | ✓ | ✓ | ✓ | ✓ | — | ✗ | ✗ | ✗ |
| Expired | ✓ | ✗ | ✗ | ✗ | ✗ | — | ✓ | ✗ |
| Repub | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | — | ✓ |
| ExpNoVerif | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | — |

**Type:** Automated (Unit — parameterized test with all 56 off-diagonal pairs)
**Priority:** P1

#### 04-004: Auto-expiration of certificates
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create cert with ExpirationDate = yesterday | Status auto-transitions to Expired |
| 2 | Create cert with ExpirationDate = 29 days from now | Flagged in "NeedsAttention" |
| 3 | Run `CheckAndUpdateExpiringCertificatesAsync()` | Expired certs transitioned, history logged |
| 4 | Verify publicly visible cert expires | Removed from public tile view |

**Type:** Automated (Integration)
**Priority:** P1

#### 04-005: Status visibility per role
| Step | Action | Expected |
|------|--------|----------|
| 1 | Public (anonymous) tile view | Only ClientApprovedPaid + Republished visible |
| 2 | External user tile view | Only ClientApprovedPaid + Republished visible |
| 3 | Analyst/Admin tile view | All statuses visible |
| 4 | Direct URL to Draft cert as anonymous | 404 or access denied |
| 5 | Direct URL to Hold cert as external | Access denied |

**Type:** Automated (E2E + Integration)
**Priority:** P1 — CRITICAL

#### 04-006: Available status dropdown per current state
| Step | Action | Expected |
|------|--------|----------|
| 1 | Open Edit for Draft cert | Status dropdown shows: NotPaid, Confid, Paid, Hold |
| 2 | Open Edit for Expired cert | Status dropdown shows: Draft, Republished |
| 3 | Open Edit for ExpiredNoAnnualVerification | Status dropdown shows: Draft only |
| 4 | Verify dropdown never shows current status as option | Current status excluded |

**Type:** Automated (E2E)
**Priority:** P1

#### 04-007: Workflow audit trail integrity
| Step | Action | Expected |
|------|--------|----------|
| 1 | Transition cert through 4 states | 4 StatusHistory entries created |
| 2 | Each entry has: OldStatus, NewStatus, ChangedBy (user ID), ChangedAt (timestamp) | All fields populated |
| 3 | ChangedAt timestamps are sequential | No time travel |
| 4 | ChangedBy matches authenticated user | Not null, not system |

**Type:** Automated (Integration)
**Priority:** P2

---

### CAT-05: File Upload & Document Management

#### 05-001: PDF upload — valid file
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload valid PDF under 20MB | File accepted |
| 2 | Verify file saved to `/wwwroot/uploads/certificates/` | File exists |
| 3 | Verify thumbnail auto-generated | PNG thumbnail created |
| 4 | Verify filename format: `{certNumber}_{timestamp}.pdf` | Correct format |

**Type:** Automated (Integration)
**Priority:** P1

#### 05-002: PDF upload — invalid file types
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload .exe file | Rejected with error |
| 2 | Upload .docx file | Rejected with error |
| 3 | Upload .pdf file with wrong MIME (renamed .exe) | Rejected (content-type check) |
| 4 | Upload .jpg file to PDF upload field | Rejected |

**Type:** Automated (Integration + Security)
**Priority:** P1

#### 05-003: PDF upload — size limits
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload PDF exactly 20MB | Accepted |
| 2 | Upload PDF 20.1MB | Rejected with clear error message |
| 3 | Upload 0-byte PDF | Rejected |
| 4 | Upload image exactly 10MB | Accepted |
| 5 | Upload image 10.1MB | Rejected |

**Type:** Automated (Integration)
**Priority:** P1

#### 05-004: PDF upload — replace existing
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload PDF for cert with existing PDF | New PDF replaces old |
| 2 | Verify old file cleaned up or archived | No orphaned files |
| 3 | Verify thumbnail regenerated | New thumbnail matches new PDF |

**Type:** Automated (Integration)
**Priority:** P2

#### 05-005: Image upload — validation
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload PNG thumbnail | Accepted |
| 2 | Upload JPG thumbnail | Accepted |
| 3 | Upload GIF | Rejected |
| 4 | Upload SVG (potential XSS vector) | Rejected |

**Type:** Automated (Integration + Security)
**Priority:** P1

#### 05-006: File download — access control
| Step | Action | Expected |
|------|--------|----------|
| 1 | Download PDF for active cert as anonymous | Allowed (public cert) |
| 2 | Download PDF for Draft cert as anonymous | Denied |
| 3 | Download PDF for Draft cert as analyst | Allowed |
| 4 | Download PDF via API without API key | 401 Unauthorized |

**Type:** Automated (Integration)
**Priority:** P1

#### 05-007: Concurrent file uploads
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload 5 PDFs simultaneously for different certs | All succeed |
| 2 | Upload same filename twice simultaneously | Unique names generated, no collision |

**Type:** Automated (Integration)
**Priority:** P3

---

### CAT-06: Certificate Views (Public / External / Internal)

#### 06-001: Public tile view — correct data visibility
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to `/Certificate/CertificateTileView` (no login) | Page loads |
| 2 | Only ClientApprovedPaid and Republished certs shown | No Draft/Hold/Expired |
| 3 | Certificate number displayed (not database ID) | Format: YY-XXXX |
| 4 | Company name, Product name, Cert type, dates shown | All visible |
| 5 | No internal tags visible | Only external tags |
| 6 | No Edit/Delete actions | No action buttons |

**Type:** Automated (E2E)
**Priority:** P1

#### 06-002: External tile view — company-scoped
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as external user (Company A) | |
| 2 | Navigate to `/Certificate/CertificateTileViewExternal` | Only Company A certs shown |
| 3 | No certificates from Company B/C visible | Filtered correctly |
| 4 | No Add/Edit/Delete buttons | Read-only |

**Type:** Automated (E2E)
**Priority:** P1 — CRITICAL

#### 06-003: Internal tile view — all data
| Step | Action | Expected |
|------|--------|----------|
| 1 | Login as analyst | Navigate to internal tile view |
| 2 | All statuses visible | Including Draft, Hold, Expired |
| 3 | Internal tags visible | Both internal + external tags |
| 4 | Edit/Delete actions available | Action buttons present |

**Type:** Automated (E2E)
**Priority:** P1

#### 06-004: Infinite scroll
| Step | Action | Expected |
|------|--------|----------|
| 1 | Load tile view with 50+ certificates | First page loads (default page size) |
| 2 | Scroll to bottom | Next batch auto-loads |
| 3 | Continue scrolling | Loads until all certs shown |
| 4 | Verify no duplicates across pages | Each cert appears once |
| 5 | Apply filter, then scroll | Filtered results paginate correctly |

**Type:** Automated (E2E)
**Priority:** P2

#### 06-005: Single certificate view — field display
| Step | Action | Expected |
|------|--------|----------|
| 1 | View `/Certificate/CertificateSingleView/{number}` | All public fields shown |
| 2 | Company email empty | Shows "—" (dash), not "Available upon request" |
| 3 | Company URL empty | Shows "—" (dash), not "Not provided" |
| 4 | Product Type empty | Shows "—" (dash) |
| 5 | Certificate PDF available | Download link present |

**Type:** Automated (E2E)
**Priority:** P1

#### 06-006: Shareable certificate URLs
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to `/Certificate/Verify/{certNumber}` | Redirects to single view |
| 2 | Share URL to unauthenticated user | Page loads (if cert is active) |
| 3 | Share URL for Draft cert | 404 or access denied |

**Type:** Automated (E2E)
**Priority:** P2

#### 06-007: Certificate views — display certificate number, not ID
| Step | Action | Expected |
|------|--------|----------|
| 1 | All table views | "Certificate Number" column shows YY-XXXX format |
| 2 | All tile views | Certificate number displayed on tile |
| 3 | Detail views | Certificate number in header |
| 4 | No database ID visible to user anywhere | ID never shown |

**Type:** Automated (E2E)
**Priority:** P1 — REGRESSION (from client QC)

---

### CAT-07: Custom Fields & Dynamic Forms

#### 07-001: Custom fields render per certification type
| Step | Action | Expected |
|------|--------|----------|
| 1 | Select CertificationType A on Create form | Custom fields for A appear |
| 2 | Switch to CertificationType B | Custom fields change to B's fields |
| 3 | Switch back to A | A's fields reappear (values cleared) |

**Type:** Automated (E2E)
**Priority:** P2

#### 07-002: Required custom fields block submission
| Step | Action | Expected |
|------|--------|----------|
| 1 | CertType has required custom field | Field rendered with required indicator |
| 2 | Submit without filling required custom field | Validation error |
| 3 | Fill field and resubmit | Succeeds |

**Type:** Automated (E2E)
**Priority:** P2

#### 07-003: Custom field types — all types render and save
| Step | Action | Expected |
|------|--------|----------|
| 1 | Text field — enter text | Saves and displays correctly |
| 2 | Number field — enter number | Saves as number, validates non-numeric input |
| 3 | Date field — pick date | Saves date, displays formatted |
| 4 | Boolean field — toggle | Saves true/false |
| 5 | Select field — choose option | Options from JSON, saves selected value |

**Type:** Automated (Integration + E2E)
**Priority:** P2

#### 07-004: Custom field values persist through edit
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create cert with custom field values | Values saved |
| 2 | Open Edit | Custom field values pre-populated |
| 3 | Change one value, save | Changed value updated, others unchanged |

**Type:** Automated (E2E)
**Priority:** P2

---

### CAT-08: Product & Company Cascading

#### 08-001: Company → Product cascade on Create
| Step | Action | Expected |
|------|--------|----------|
| 1 | Select Company A | Product dropdown loads with Company A products |
| 2 | Select different Company B | Product dropdown refreshes with Company B products |
| 3 | Previous product selection cleared | Product field reset |
| 4 | Category/Type/Subcategory fields cleared | All cascade fields reset |

**Type:** Automated (E2E)
**Priority:** P1

#### 08-002: Product → Category/Type/Subcategory auto-fill
| Step | Action | Expected |
|------|--------|----------|
| 1 | Select product that has category/type/subcategory | All three fields auto-populate |
| 2 | Select product with category only | Category fills, type/subcategory remain empty |
| 3 | Change product | Fields update to new product's values |

**Type:** Automated (E2E)
**Priority:** P1

#### 08-003: "Add New Product" inline modal
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click "Add New Product" on Create form | Modal opens with 6 fields |
| 2 | Fill: Name, Company, Category (required), Type, Subcategory, Description | |
| 3 | Save | Modal closes, product selected in main form |
| 4 | Category/Type/Subcategory auto-filled on main form | Cascade values populated |
| 5 | Submit without Category in modal | Validation error (Category required) |

**Type:** Automated (E2E)
**Priority:** P1

#### 08-004: Edit form — pre-populated cascading fields
| Step | Action | Expected |
|------|--------|----------|
| 1 | Open Edit for cert with Company A, Product X | Company A selected, Product X pre-selected |
| 2 | Product dropdown contains Company A's products | Filtered correctly |
| 3 | Category/Type/Subcategory pre-populated | Match Product X's values |
| 4 | Change Company to B | Products reload, Product X cleared |

**Type:** Automated (E2E)
**Priority:** P1

#### 08-005: Autocomplete text input behavior
| Step | Action | Expected |
|------|--------|----------|
| 1 | Type partial product name | Autocomplete suggestions appear |
| 2 | Select suggestion | Product field populated |
| 3 | Change company | Autocomplete text input clears |
| 4 | Type name with no matches | "No results" indication |

**Type:** Automated (E2E)
**Priority:** P2

---

### CAT-09: Search, Filter, Sort & Pagination

#### 09-001: Search by certificate number
| Step | Action | Expected |
|------|--------|----------|
| 1 | Search "26-0023" | Matching cert(s) returned |
| 2 | Search partial "26-00" | All certs starting with 26-00 |
| 3 | Search non-existent number | Empty results with message |

**Type:** Automated (E2E)
**Priority:** P1

#### 09-002: Search by product name, company name, tags
| Step | Action | Expected |
|------|--------|----------|
| 1 | Search by product name | Matching certs returned |
| 2 | Search by company name | Matching certs returned |
| 3 | Search by tag name | Matching certs returned |

**Type:** Automated (E2E)
**Priority:** P1

#### 09-003: Filter by certification type
| Step | Action | Expected |
|------|--------|----------|
| 1 | Select CertificationType filter | Only matching certs shown |
| 2 | Clear filter | All certs shown |
| 3 | Combine with search | Both filters applied (AND logic) |

**Type:** Automated (E2E)
**Priority:** P1

#### 09-004: Filter by status (role-dependent)
| Step | Action | Expected |
|------|--------|----------|
| 1 | As admin, filter by Draft | Draft certs shown |
| 2 | As external, attempt filter by Draft | Filter option not available |
| 3 | As public, filter by status | Only Paid/Republished options |

**Type:** Automated (E2E)
**Priority:** P1

#### 09-005: Sort — all options
| Step | Action | Expected |
|------|--------|----------|
| 1 | Sort by Newest | Most recent EffectiveDate first |
| 2 | Sort by Expiring Soon | Nearest ExpirationDate first |
| 3 | Sort by Product A-Z | Alphabetical by product name |
| 4 | Sort by Company A-Z | Alphabetical by company name |
| 5 | Sort by Status | Grouped by status |
| 6 | Sort by Type | Grouped by certification type |
| 7 | Sort by Country | Grouped by country |

**Type:** Automated (E2E)
**Priority:** P2

#### 09-006: Filter + Sort + Search combination
| Step | Action | Expected |
|------|--------|----------|
| 1 | Apply search term + CertType filter + Sort by Newest | All three applied simultaneously |
| 2 | Clear search, keep filter and sort | Filter + sort still active |
| 3 | Apply country filter on top | Three filters active |
| 4 | Clear all | Returns to default view |

**Type:** Automated (E2E)
**Priority:** P2

#### 09-007: Empty state handling
| Step | Action | Expected |
|------|--------|----------|
| 1 | Search term that matches nothing | "No certificates found" message |
| 2 | Filter combination with no results | Clear message, not blank page |
| 3 | External user with no assigned certs | Empty state message |

**Type:** Automated (E2E)
**Priority:** P2

---

### CAT-10: API Endpoints & Partner Integration

#### 10-001: API authentication — API key
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET `/api/v1/certificates` with valid X-API-Key | 200 OK |
| 2 | GET without API key | 401 Unauthorized |
| 3 | GET with invalid API key | 401 Unauthorized |
| 4 | GET with expired API key | 401 Unauthorized |
| 5 | GET with revoked API key | 401 Unauthorized |

**Type:** Automated (Integration)
**Priority:** P1

#### 10-002: API authentication — JWT Bearer
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET with valid Bearer token | 200 OK |
| 2 | GET with expired token | 401 Unauthorized |
| 3 | GET with malformed token | 401 Unauthorized |
| 4 | GET with token signed by wrong key | 401 Unauthorized |

**Type:** Automated (Integration)
**Priority:** P1

#### 10-003: API rate limiting
| Step | Action | Expected |
|------|--------|----------|
| 1 | Standard API key: 1001st request in an hour | 429 Too Many Requests |
| 2 | Partner API key: 2001st request in an hour | 429 Too Many Requests |
| 3 | Verify rate limit headers in response | X-RateLimit-Remaining, X-RateLimit-Reset |
| 4 | Wait for reset period | Requests succeed again |

**Type:** Automated (Integration)
**Priority:** P2

#### 10-004: API — GET /api/v1/certificates
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET with no params | Paginated list, default page 1 |
| 2 | GET with `?pageNumber=2&pageSize=10` | Correct page returned |
| 3 | GET with `?status=ClientApprovedPaid` | Filtered results |
| 4 | GET with `?companyId=1&productId=2` | Combined filters |
| 5 | GET with `?includeProductIds=true` | Product IDs included in response |
| 6 | Verify `StandardApiResponse` format | success, message, data, requestId, timestamp |

**Type:** Automated (Integration)
**Priority:** P1

#### 10-005: API — GET /api/v1/certificates/{id}
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET with valid ID | Certificate data returned |
| 2 | GET with non-existent ID | 404 Not Found |
| 3 | GET with string/invalid ID | 400 Bad Request |

**Type:** Automated (Integration)
**Priority:** P1

#### 10-006: API — certificate PDF download
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET `/api/v1/certificates/{id}/download` | PDF file returned |
| 2 | GET for cert with no PDF | 404 Not Found |
| 3 | Content-Type header = `application/pdf` | Correct MIME type |

**Type:** Automated (Integration)
**Priority:** P1

#### 10-007: API — search by product ID
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET `/api/v1/certificates/search/productid?type=ASIN&value=B001234` | Matching certs |
| 2 | Search with non-existent ASIN | Empty results |
| 3 | Search by UPC, GTIN, EAN | Each type works |

**Type:** Automated (Integration)
**Priority:** P1

#### 10-008: API — HTTPS enforcement
| Step | Action | Expected |
|------|--------|----------|
| 1 | HTTP request in production | 403 or redirect to HTTPS |
| 2 | HTTPS request in production | Succeeds |
| 3 | HTTP request in development | Succeeds (HTTPS not enforced) |

**Type:** Automated (Integration)
**Priority:** P2

#### 10-009: Partner API — Amazon integration
| Step | Action | Expected |
|------|--------|----------|
| 1 | Authenticate with partner API key (gcc_partner_...) | 200 OK |
| 2 | Rate limit at 2000/hour | Higher than standard keys |
| 3 | Cert data includes Amazon-specific fields | AmazonCode in CertificationType |

**Type:** Automated (Integration)
**Priority:** P2

#### 10-010: API — response format consistency
| Step | Action | Expected |
|------|--------|----------|
| 1 | All success responses | `{ success: true, data: ..., message: ... }` |
| 2 | All error responses | `{ success: false, errors: [...], message: ... }` |
| 3 | All responses include requestId | Unique per request |
| 4 | All responses include timestamp | ISO 8601 format |

**Type:** Automated (Integration)
**Priority:** P2

---

### CAT-11: Admin Management Pages

#### 11-001: User management CRUD
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to `/Admin/UserManagement` as admin | User list loads |
| 2 | Create new user with all fields | User created, appears in list |
| 3 | Edit user role | Role updated |
| 4 | Deactivate user | User can no longer login |
| 5 | Attempt to delete Admin user | Prevented (admin user cannot be deleted) |
| 6 | Verify unique email enforcement | Duplicate email rejected |

**Type:** Automated (E2E)
**Priority:** P1

#### 11-002: Admin pages — CSV export only
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click Export on any admin page | CSV file downloaded |
| 2 | Verify no Excel/JSON/PDF export options | Only CSV available |
| 3 | Verify `ExportUtils.exportToCSV()` used | No other export method |
| 4 | CSV content matches displayed data | Column headers + data rows correct |

**Type:** Automated (E2E)
**Priority:** P1

#### 11-003: Admin pages — UI consistency
| Step | Action | Expected |
|------|--------|----------|
| 1 | All admin pages | Action buttons (Export, Add) RIGHT-ALIGNED in header |
| 2 | All admin pages | Stats bar RIGHT-ALIGNED below search |
| 3 | All admin pages | Search bar 100% width |
| 4 | All admin pages | Use `.admin-container` layout |
| 5 | All admin pages | Title from `ViewData["Title"]`, no inline `<h1>` |
| 6 | All admin pages | 16px minimum font size |

**Type:** Automated (E2E) + Manual (visual)
**Priority:** P2

#### 11-004: Certificate Type management — hierarchical
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create parent CertificationType (Level 1) | Created |
| 2 | Create child CertificationType (Level 2) under parent | Created with ParentId |
| 3 | Edit child type | ParentId preserved |
| 4 | Delete parent with children | Prevented or cascaded |

**Type:** Automated (Integration)
**Priority:** P2

#### 11-005: Tag management — type enforcement
| Step | Action | Expected |
|------|--------|----------|
| 1 | Create tag with type "external" | Visible in public views |
| 2 | Create tag with type "internal" | Visible only to analyst/admin |
| 3 | Assign both tag types to certificate | Both saved |
| 4 | View as external user | Only external tags visible |

**Type:** Automated (Integration + E2E)
**Priority:** P1

#### 11-006: Partner management
| Step | Action | Expected |
|------|--------|----------|
| 1 | Navigate to Partners page | Export + Add buttons only |
| 2 | No Template/Import buttons | Verified absent |
| 3 | Create new partner | Partner created |
| 4 | Export partners | CSV download |

**Type:** Automated (E2E)
**Priority:** P2

---

### CAT-12: Error Handling & Recovery

#### 12-001: Database connection failure
| Step | Action | Expected |
|------|--------|----------|
| 1 | Simulate database unavailability | Graceful error page (not stack trace) |
| 2 | Web request during DB outage | 500 error with user-friendly message |
| 3 | API request during DB outage | JSON error response with error code |
| 4 | Database recovers | Application resumes without restart |

**Type:** Manual (infrastructure)
**Priority:** P2

#### 12-002: File system errors
| Step | Action | Expected |
|------|--------|----------|
| 1 | Upload PDF when upload directory is full | Error message, no crash |
| 2 | Download PDF when file is missing from disk | 404 with message, not stack trace |
| 3 | Generate thumbnail when PDF is corrupt | Graceful failure, cert still saved |

**Type:** Manual
**Priority:** P2

#### 12-003: Global exception handling — web vs API
| Step | Action | Expected |
|------|--------|----------|
| 1 | Trigger unhandled exception in MVC controller | HTML error page (not raw stack trace) |
| 2 | Trigger unhandled exception in API controller | JSON error response |
| 3 | Production mode | No stack traces leaked |
| 4 | Development mode | Stack traces shown for debugging |

**Type:** Automated (Integration)
**Priority:** P1

#### 12-004: Validation failure — form data preservation
| Step | Action | Expected |
|------|--------|----------|
| 1 | Fill Create form, trigger validation error | Form re-renders with all entered values preserved |
| 2 | File upload included | File may need re-upload (noted in UI) |
| 3 | Cascading dropdowns retain selection | Company/Product/Category still selected |

**Type:** Automated (E2E)
**Priority:** P2

#### 12-005: Network timeout handling
| Step | Action | Expected |
|------|--------|----------|
| 1 | AJAX call to load products times out | User sees error, can retry |
| 2 | Infinite scroll request fails | Error message, scroll still functional |
| 3 | File upload connection lost mid-upload | Error message, partial file cleaned up |

**Type:** Manual
**Priority:** P3

#### 12-006: Duplicate submission prevention
| Step | Action | Expected |
|------|--------|----------|
| 1 | Double-click Submit on Create form | Only one certificate created |
| 2 | Submit, press Back, Submit again | Duplicate prevented or warned |

**Type:** Manual
**Priority:** P2

---

### CAT-13: Usability & UI Consistency

#### 13-001: No native JavaScript dialogs
| Step | Action | Expected |
|------|--------|----------|
| 1 | Audit entire codebase for `alert(`, `confirm(`, `prompt(` | Zero instances |
| 2 | All confirmations use `AdminCore.showConfirmation()` | Modal pattern |
| 3 | All notifications use `AdminCore.showNotification()` | Toast pattern |

**Type:** Automated (static analysis / grep)
**Priority:** P1 — CRITICAL RULE

#### 13-002: Delete pattern — modal then toast
| Step | Action | Expected |
|------|--------|----------|
| 1 | Click Delete on any entity | Confirmation modal appears |
| 2 | Modal has clear title, message, Cancel + Confirm buttons | Proper layout |
| 3 | Modal backdrop: `rgba(0,0,0,0.85)` + blur | Correct styling |
| 4 | Cancel | Modal closes, no action |
| 5 | Confirm | Entity deleted, success toast appears |

**Type:** Automated (E2E)
**Priority:** P1

#### 13-003: Button hover states
| Step | Action | Expected |
|------|--------|----------|
| 1 | Hover over action buttons | Transparent background (NO grey background) |
| 2 | Verify across all pages | Consistent hover behavior |

**Type:** Manual (visual)
**Priority:** P3

#### 13-004: Empty field display — dash convention
| Step | Action | Expected |
|------|--------|----------|
| 1 | Company with no email | Displays "—" |
| 2 | Company with no URL | Displays "—" |
| 3 | Product with no type | Displays "—" |
| 4 | Consistent across all 4 detail views | SingleView, Details, InternalView, ExternalView |

**Type:** Automated (E2E)
**Priority:** P1 — REGRESSION

#### 13-005: Responsive design — mobile
| Step | Action | Expected |
|------|--------|----------|
| 1 | View tile page at 375px width | Cards stack vertically |
| 2 | View admin pages at 768px width | Table scrollable or responsive |
| 3 | Navigation menu on mobile | Hamburger menu or similar |
| 4 | Forms on mobile | Full-width inputs, readable labels |

**Type:** Manual
**Priority:** P3

#### 13-006: Font size minimum 16px
| Step | Action | Expected |
|------|--------|----------|
| 1 | Audit CSS for any font-size below 16px | Zero instances |
| 2 | Check computed styles on key pages | All text >= 16px |

**Type:** Automated (static analysis + E2E)
**Priority:** P2

---

### CAT-14: Accessibility (WCAG 2.1 AA)

#### 14-001: axe-core automated audit
| Step | Action | Expected |
|------|--------|----------|
| 1 | Run axe-core on all public pages | Zero critical/serious violations |
| 2 | Run axe-core on admin pages | Zero critical/serious violations |
| 3 | Run axe-core on login page | Zero violations |

**Type:** Automated (Accessibility test suite — fix port issue first)
**Priority:** P2

#### 14-002: Keyboard navigation
| Step | Action | Expected |
|------|--------|----------|
| 1 | Tab through login form | All fields reachable |
| 2 | Tab through Create certificate form | All fields reachable in logical order |
| 3 | Tab through tile view | Cards navigable |
| 4 | Enter/Space to activate buttons | All interactive elements respond |
| 5 | Escape to close modals | Modal dismisses |

**Type:** Manual
**Priority:** P2

#### 14-003: Screen reader compatibility
| Step | Action | Expected |
|------|--------|----------|
| 1 | `aria-live` on dynamic regions (tile loading) | Screen reader announces new content |
| 2 | `aria-label` on icon-only buttons | Descriptive labels |
| 3 | Form labels associated with inputs | `for`/`id` pairs or `aria-labelledby` |
| 4 | Heading hierarchy (h1 → h2 → h3) | No skipped levels |

**Type:** Manual
**Priority:** P2

#### 14-004: Color contrast
| Step | Action | Expected |
|------|--------|----------|
| 1 | Check all text/background combinations | 4.5:1 ratio minimum (AA) |
| 2 | Check button text contrast | Meets AA |
| 3 | Check error message contrast | Visible and distinct |

**Type:** Automated (axe-core) + Manual
**Priority:** P2

---

### CAT-15: Performance & Load

#### 15-001: Page load times
| Step | Action | Expected |
|------|--------|----------|
| 1 | Homepage / tile view | < 3 seconds |
| 2 | Admin dashboard | < 3 seconds |
| 3 | Certificate Create form | < 2 seconds |
| 4 | Certificate Edit form | < 3 seconds |

**Type:** Automated (Performance tests)
**Priority:** P3

#### 15-002: API response times
| Step | Action | Expected |
|------|--------|----------|
| 1 | GET /api/v1/certificates (first page) | < 500ms |
| 2 | GET /api/v1/certificates with filters | < 1 second |
| 3 | GET /api/v1/certificates/{id} | < 200ms |
| 4 | POST /api/v1/certificates (create) | < 2 seconds |

**Type:** Automated (Performance tests)
**Priority:** P3

#### 15-003: Database query performance
| Step | Action | Expected |
|------|--------|----------|
| 1 | Certificate list with 10,000 records | < 1 second query time |
| 2 | No N+1 query patterns | Verify with EF query logging |
| 3 | All indexed columns used in WHERE clauses | Query plan verification |

**Type:** Automated (Integration with query logging)
**Priority:** P3

#### 15-004: Concurrent user load
| Step | Action | Expected |
|------|--------|----------|
| 1 | 50 simultaneous users browsing tiles | Response times < 5 seconds |
| 2 | 10 simultaneous certificate creates | All succeed, no conflicts |
| 3 | 100 simultaneous API requests | Within rate limits, all processed |

**Type:** Automated (Performance tests / k6 or similar)
**Priority:** P3

---

### CAT-16: Security

#### 16-001: SQL injection — all input points
| Step | Action | Expected |
|------|--------|----------|
| 1 | Search box: `' OR 1=1 --` | No data leak |
| 2 | URL parameter: `/Certificate/Edit/' OR 1=1` | 400 or 404 |
| 3 | API query: `?companyId=1 OR 1=1` | Parameterized query, no injection |
| 4 | Form fields: Company name with SQL | Escaped, no injection |

**Type:** Automated (Security tests)
**Priority:** P1

#### 16-002: XSS — stored and reflected
| Step | Action | Expected |
|------|--------|----------|
| 1 | Enter `<script>alert('xss')</script>` in certificate fields | Stored as text, rendered escaped |
| 2 | Enter XSS in search box | Not reflected in page |
| 3 | Enter XSS in company name | Displayed as literal text |
| 4 | SVG upload with embedded script | File rejected |

**Type:** Automated (Security tests)
**Priority:** P1

#### 16-003: CSRF protection
| Step | Action | Expected |
|------|--------|----------|
| 1 | POST without anti-forgery token | 400 Bad Request |
| 2 | POST with cross-origin anti-forgery token | 400 Bad Request |
| 3 | All POST forms include `@Html.AntiForgeryToken()` | Verified in source |

**Type:** Automated (Security tests)
**Priority:** P1

#### 16-004: Security headers
| Step | Action | Expected |
|------|--------|----------|
| 1 | Check `X-Content-Type-Options: nosniff` | Present |
| 2 | Check `X-Frame-Options: DENY` or CSP frame-ancestors | Present |
| 3 | Check `Strict-Transport-Security` (production) | Present |
| 4 | Check no `Server` header leak | Absent or generic |

**Type:** Automated (Security tests)
**Priority:** P2

#### 16-005: Path traversal — file download
| Step | Action | Expected |
|------|--------|----------|
| 1 | Request `/Certificate/Download?path=../../web.config` | Denied |
| 2 | Request with encoded path traversal (`%2e%2e`) | Denied |
| 3 | Request non-existent file path | 404, no directory listing |

**Type:** Automated (Security tests)
**Priority:** P1

#### 16-006: Sensitive data exposure
| Step | Action | Expected |
|------|--------|----------|
| 1 | API error response | No stack traces in production |
| 2 | API response | No password hashes, no API key values |
| 3 | Page source | No connection strings, no secrets |
| 4 | Cookie flags | HttpOnly, Secure (production) |

**Type:** Automated (Security tests)
**Priority:** P1

---

## 6. High-Risk Areas

### Risk Matrix

| # | Area | Risk Level | Why | Impact if Missed |
|---|------|-----------|-----|------------------|
| 1 | **External user data isolation** | CRITICAL | External users must ONLY see their assigned company data. Failure = data breach | Client data exposed to unauthorized parties |
| 2 | **Workflow state transitions** | CRITICAL | Invalid transitions could make certificates publicly visible prematurely or irreversibly stuck | Unapproved certs shown to public, compliance violation |
| 3 | **Role escalation via URL manipulation** | HIGH | Policies exist but direct URL access by lower-privileged users may bypass UI restrictions | Unauthorized CRUD operations |
| 4 | **File upload security** | HIGH | Malicious file uploads (disguised executables, XSS in SVG) could compromise server | Server compromise, XSS attacks |
| 5 | **API authentication bypass** | HIGH | 12+ tests already failing with 401 — indicates auth setup fragility | Unauthorized API access to all certificate data |
| 6 | **Certificate number vs ID display** | MEDIUM | Client QC flagged this — ID shown instead of cert number in some views | Client confidence loss, incorrect data references |
| 7 | **Auto-expiration timing** | MEDIUM | Certs must transition to Expired automatically — if missed, expired certs stay public | Compliance violation, showing expired certifications |
| 8 | **Concurrent edits** | MEDIUM | No optimistic concurrency detected — last write wins silently | Data loss without user awareness |
| 9 | **Custom field role visibility** | MEDIUM | Internal custom fields visible to external users = information leak | Confidential assessment data exposed |
| 10 | **CSV import validation** | MEDIUM | Bulk import with bad data could corrupt database | Mass data quality issues |

### Top 5 Failure Scenarios to Test First

1. **External user accesses another company's certificate via direct URL** — test 02-003
2. **Certificate transitions from Draft directly to Republished** (invalid) — test 04-002
3. **Analyst navigates to admin-only URL** — test 02-002
4. **Upload .exe renamed to .pdf** — test 05-002
5. **API request without authentication gets data** — test 10-001

---

## 7. Automation vs Manual Recommendations

### Automate (80% of test cases)

| Category | Tool | Why Automate |
|----------|------|-------------|
| Authentication (01-001 through 01-003, 01-010, 01-011) | xUnit + WebApplicationFactory | Repeatable, regression-critical |
| RBAC enforcement (all 02-xxx) | xUnit + Playwright | High risk, must pass on every build |
| Certificate CRUD validation (03-001 through 03-010) | xUnit + Playwright | Core functionality, frequent changes |
| Workflow transitions (all 04-xxx) | xUnit (parameterized) | 64 state pairs, tedious to test manually |
| File upload validation (05-001 through 05-006) | xUnit integration tests | Security-critical, repeatable |
| API endpoints (all 10-xxx) | xUnit + HttpClient | Already partially covered, extend |
| Security (all 16-xxx) | xUnit security tests | Must never regress |
| Search/Filter/Sort (09-001 through 09-006) | Playwright | Functional, repeatable |
| Static analysis (13-001, 13-006) | Grep / custom script | One-time setup, runs in CI |

### Keep Manual (20% of test cases)

| Category | Why Manual |
|----------|-----------|
| Session expiration (01-005, 01-006) | Requires real time passage, browser state |
| Azure AD B2C (01-009) | External identity provider, environment-specific |
| Concurrent edits (03-005) | Multi-user timing, hard to automate reliably |
| Database/file system failures (12-001, 12-002) | Infrastructure simulation |
| Responsive design (13-005) | Visual judgment needed |
| Keyboard navigation (14-002) | Subjective flow assessment |
| Screen reader (14-003) | Requires assistive technology |
| Button hover states (13-003) | Visual judgment |
| Duplicate submission (12-006) | Timing-sensitive |

### CI Pipeline Integration

```
┌─────────────────────────────────────────────┐
│  PR Build Pipeline                          │
├─────────────────────────────────────────────┤
│  Stage 1: Build + Static Analysis           │
│  ├── dotnet build                           │
│  ├── Grep for alert()/confirm()/prompt()    │
│  └── CSS audit (font-size < 16px)           │
│                                             │
│  Stage 2: Unit Tests                        │
│  ├── GCCWebApp.Tests (services, models)     │
│  └── Workflow transition matrix             │
│                                             │
│  Stage 3: Integration Tests                 │
│  ├── GCCWebApp.Api.Tests                    │
│  ├── GCCWebApp.Security.Tests               │
│  └── GCCWebApp.RoleBasedUI.Tests            │
│                                             │
│  Stage 4: E2E Tests (nightly / pre-release) │
│  ├── GCCWebApp.E2E.Tests (Playwright)       │
│  └── GCCWebApp.Accessibility.Tests          │
└─────────────────────────────────────────────┘
```

---

## 8. Priority Execution Order

### Phase 1: Infrastructure Fix (Days 1-2)
**Goal:** Get all test suites running before writing new tests.

| Task | Effort | Impact |
|------|--------|--------|
| Fix E2E test port (7001 → app actual port or use WebApplicationFactory) | 2 hours | Unlocks 14 tests |
| Fix Accessibility test port (7130 → correct port) | 1 hour | Unlocks 54 tests |
| Fix UAT test build (logger URI config) | 30 min | Unlocks UAT suite |
| Fix API auth test setup (401 errors) | 4 hours | Unlocks 12+ tests |
| Triage 106 failing GCCWebApp.Tests | 4 hours | Understand true coverage |

### Phase 2: Critical Security & RBAC (Days 3-5)
**Goal:** Ensure no data leaks or unauthorized access.

| Priority | Tests | Count |
|----------|-------|-------|
| P1 | 02-003: External user company isolation | 1 (multi-step) |
| P1 | 02-007: Role escalation via URL | 1 |
| P1 | 02-004: Internal tags hidden from external | 1 |
| P1 | 02-009: Custom field visibility | 1 |
| P1 | 04-002, 04-003: Invalid workflow transitions | 2 (56 sub-cases) |
| P1 | 04-005: Status visibility per role | 1 |
| P1 | 16-001 through 16-006: Security suite | 6 |

### Phase 3: Core Workflow & CRUD (Days 6-8)
**Goal:** Validate all certificate operations and state machine.

| Priority | Tests | Count |
|----------|-------|-------|
| P1 | 03-001 through 03-008: Certificate CRUD | 8 |
| P1 | 04-001, 04-004, 04-006: Workflow happy + edge paths | 3 |
| P1 | 05-001 through 05-006: File upload validation | 6 |
| P1 | 06-001 through 06-007: View correctness | 7 |

### Phase 4: API & Integration (Days 9-10)
**Goal:** Validate all API endpoints with auth, rate limiting, and partner access.

| Priority | Tests | Count |
|----------|-------|-------|
| P1 | 10-001 through 10-007: API core | 7 |
| P2 | 10-008 through 10-010: API extended | 3 |
| P1 | 01-001 through 01-004: Auth core | 4 |

### Phase 5: Usability, Search, Admin (Days 11-13)
**Goal:** Validate user-facing quality and admin functions.

| Priority | Tests | Count |
|----------|-------|-------|
| P1 | 13-001, 13-002: No native dialogs, delete pattern | 2 |
| P1 | 13-004: Dash convention for empty fields | 1 |
| P1 | 09-001 through 09-004: Search/filter | 4 |
| P1 | 11-001, 11-002: User mgmt, CSV export | 2 |
| P2 | 08-001 through 08-005: Cascading fields | 5 |
| P2 | 07-001 through 07-004: Custom fields | 4 |

### Phase 6: Edge Cases, Performance, Accessibility (Days 14-16)
**Goal:** Harden for production readiness.

| Priority | Tests | Count |
|----------|-------|-------|
| P2 | 12-001 through 12-006: Error handling | 6 |
| P2 | 14-001 through 14-004: Accessibility | 4 |
| P2 | 01-005 through 01-008: Auth extended | 4 |
| P3 | 15-001 through 15-004: Performance | 4 |
| P3 | 13-003, 13-005: Visual/responsive | 2 |

### Ongoing: Regression Gate
Every client QC defect fix MUST include a regression test before closing the defect. Tag these tests with `[Trait("Regression", "QC-{defectId}")]` so they can be run as a targeted suite.

---

## 9. Test Infrastructure Fixes

### Fix 1: E2E Tests — WebApplicationFactory
Replace external app dependency with in-process test server:

```csharp
// GCCWebApp.E2E.Tests/CustomWebApplicationFactory.cs
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Replace DB with in-memory or test database
            // Seed test data
        });
    }
}
```

### Fix 2: Accessibility Tests — Same Pattern
Use `WebApplicationFactory` or configure test base URL from environment variable:

```csharp
var baseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL")
    ?? "http://localhost:5101";
```

### Fix 3: UAT Tests Build Error
Fix logger URI configuration:
```
// Change from:
dotnet test --logger "verbosity=detailed"
// To:
dotnet test --logger "console;verbosity=detailed"
```

### Fix 4: API Auth Tests
Ensure test setup creates valid API keys in the test database and configures the `ApiKeyAuthenticationHandler` to use the test DB context.

---

## Appendix: Test Case Summary

| Category | Tests | P1 | P2 | P3 | Automated | Manual |
|----------|-------|----|----|----|-----------|--------|
| 01 - Auth & Sessions | 12 | 6 | 4 | 2 | 9 | 3 |
| 02 - RBAC | 10 | 7 | 3 | 0 | 9 | 1 |
| 03 - Certificate CRUD | 10 | 6 | 4 | 0 | 9 | 1 |
| 04 - Workflow | 7 | 5 | 2 | 0 | 7 | 0 |
| 05 - File Upload | 7 | 5 | 1 | 1 | 7 | 0 |
| 06 - Views | 7 | 5 | 2 | 0 | 7 | 0 |
| 07 - Custom Fields | 4 | 0 | 4 | 0 | 4 | 0 |
| 08 - Cascading | 5 | 3 | 2 | 0 | 5 | 0 |
| 09 - Search/Filter | 7 | 3 | 4 | 0 | 7 | 0 |
| 10 - API | 10 | 5 | 5 | 0 | 10 | 0 |
| 11 - Admin | 6 | 3 | 3 | 0 | 5 | 1 |
| 12 - Error Handling | 6 | 1 | 3 | 2 | 2 | 4 |
| 13 - Usability | 6 | 3 | 1 | 2 | 3 | 3 |
| 14 - Accessibility | 4 | 0 | 4 | 0 | 1 | 3 |
| 15 - Performance | 4 | 0 | 0 | 4 | 4 | 0 |
| 16 - Security | 6 | 5 | 1 | 0 | 6 | 0 |
| **TOTAL** | **111** | **57** | **43** | **11** | **95** | **16** |

**Automation target: 86% (95/111 test cases)**
**P1 coverage: 51% of all tests — execute first**

---

*Document maintained by Anuki Consulting. Update after each sprint/QC cycle.*

# Defect Register

**Project:** GCC Database Solution
**Created:** 2026-03-23
**Last Updated:** 2026-03-25

## Status Definitions

| Status | Meaning |
|--------|---------|
| New | Backfilled from QC log — not yet reviewed or triaged |
| Open | Identified, not yet started |
| In-Progress | Actively being worked on |
| Fixed | Code fix applied, awaiting verification |
| Verified | Fix confirmed working by tester |
| Closed | Verified and accepted — no further action |
| Won't-Fix | Intentionally not fixing (requires justification) |

## Priority Definitions

| Priority | Meaning |
|----------|---------|
| Critical | Blocks launch or causes data loss/security risk |
| High | Major functionality broken, no workaround |
| Medium | Functionality impaired but workaround exists |
| Low | Cosmetic or minor inconvenience |

---

## Defect Summary

| Priority | New | Open | In-Progress | Fixed | Verified | Closed | Won't-Fix | Total |
|----------|-----|------|-------------|-------|----------|--------|-----------|-------|
| Critical | 0 | 0 | 0 | 5 | 0 | 8 | 0 | 13 |
| High | 15 | 4 | 0 | 6 | 0 | 3 | 0 | 28 |
| Medium | 16 | 8 | 0 | 5 | 0 | 3 | 0 | 32 |
| Low | 5 | 3 | 0 | 4 | 0 | 0 | 0 | 12 |
| **Total** | **36** | **15** | **0** | **20** | **0** | **14** | **0** | **85** |

> Update this summary table whenever defect statuses change.

---

## Defect Log

### DEF-001 — Expired/Draft/Hold/Confidential certs showing on public view

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Replaced 14-state status system with 8 client-defined statuses. Public view now only shows "Client Approved - Paid" and "Republished" where expiration date has not passed. Applied across 25+ files. |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `024d209` |

---

### DEF-002 — Auto-expire: DB must change status to Expired when expiration date passes

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | CertificateExpirationService existed but was never called. Wired it into AccountController.SignInUser() so it runs on every login, throttled to once per 24 hours. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | Pending commit |

---

### DEF-003 — Sort/filter not working on public view

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Async loadFilterOptions() was not awaited, so setupUrlParameters() tried to check boxes that didn't exist yet. Fixed initialization order across all 3 tile views (public, external, internal). |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | Pending commit |

---

### DEF-004 — Mobile/tablet broken: filters and hamburger menu non-functional

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Three root causes: (1) toggleFilter() used inline styles conflicting with CSS media queries — changed to CSS class toggle. (2) External/internal views had mobile filter button hardcoded display:none — removed. (3) External/internal JS had no mobile toggle handler — added. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | Pending commit |

---

### DEF-005 — Pagination missing across multiple pages

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Created AdminPagination utility (admin-pagination.js). Integrated into User Management and Product Management pages with 25 items per page. Added pagination CSS to admin-tables.css. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | Pending commit |

---

### DEF-006 — Product not tied to company in cert creation

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | CR-002 |
| Resolution | Added company→product cascading. Product dropdown loads products for selected company. Autocomplete clears on company change. Applied to Create, Edit, and inline modal. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | `1ed2f12` |

---

### DEF-007 — Dashboard exports dumping code instead of data

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Auth redirect was sending login page HTML as CSV when session expired. Extended OnRedirectToLogin/OnRedirectToAccessDenied in Program.cs to return 401/403 for /Export, /Dashboard/Download, and AJAX requests. Added Content-Type validation in dashboard.js. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | Pending commit |

---

### DEF-008 — Date format in CSV export not recognized as dates

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed date formatting in CSV exports |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `55174a3` |

---

### DEF-009 — Session timeout clears unsaved work with no warning

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Added localStorage auto-save every 30s for certificate forms. On page load, checks for recovered data and shows restore/discard banner. Session timeout handler saves form data before auto-logout and notifies user. Recovery data expires after 2 hours. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | Pending commit |

> **Note:** Priority downgraded from Critical to Medium — session timeout warning existed but form data backup was missing. Now both are in place.

---

### DEF-010 — User creation failing in Admin

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed user creation flow in admin |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `55174a3` |

---

### DEF-011 — Cert types missing from dropdown

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed dropdown population for cert types |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `55174a3` |

---

### DEF-012 — Countries list only has 5 entries

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Populated full countries list |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `55174a3` |

---

### DEF-013 — Company autocomplete not working

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Critical |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed autocomplete binding and search logic |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `d29395c` |

---

### DEF-014 — Product ID Manager edit freezes

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed Product ID Manager edit functionality |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `8461993` |

---

### DEF-015 — CSV import for large numbers (SKUs) fails due to scientific notation

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-016 — Product Management missing Product Type/Subcategory columns

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Added product type and subcategory columns to Product Management view |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `d29395c` |

---

### DEF-017 — Partner added in admin not appearing in cert creation dropdown

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-018 — Product types always assigned to "Chemicals and Raw Materials"

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed category assignment logic in product creation/edit |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `d29395c` |

---

### DEF-019 — Cert type deletion not protected when certs are tied to it

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-020 — Shareable URLs not preserving search/filter state

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-021 — Remove "Expiring Soon" tag globally

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Removed Expiring Soon from all sort options, tags, dashboard, exports, and backend services as part of status system overhaul |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `024d209` |

---

### DEF-022 — Sort By options cleanup for public view

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-023 — Remove extra Sign In dropdown step

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-024 — Company name not wrapping on mobile

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-025 — Download button alignment on cert detail page

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-026 — Packaging/Paper/Plastic miscategorized under Chemicals

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-027 — APR/LEED filter not working

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-028 — Character limit clarity for company/product names

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-029 — Default cert preview image removal

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-030 — Login text updates needed

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-031 — Remember me not working consistently

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Auth cookie was hardcoded to 30-min expiration regardless of Remember Me. Changed to 14-day expiration when rememberMe is true. |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | `bc5d619` |

---

### DEF-032 — Cert subcategories additions needed

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-033 — Pathway field label update

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-034 — Status types update needed

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | CR-003 |
| Resolution | Replaced entire 14-state system with 8 client-defined statuses |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `024d209` |

---

### DEF-035 — Breadcrumbs broken links

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | All `href="#"` breadcrumb links fixed across 19 views. "Administration" links to Admin Dashboard. Category labels changed to non-clickable `<span>`. Current page items changed to `<span class="current">`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-036 — Nav appearing behind search bar

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-037 — Upload PDF click target (words only, not icon)

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-038 — Font size adjustments in cert listing

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-039 — Sort arrows overlapping column names

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Reduced sort icon size from 16px to 10px, added `white-space: nowrap` on `<th>`, reduced header padding. Added `min-width` on all columns. Files: `product-management.css`, `Management.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-040 — Remove "Search:" label from filter tags

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-041 — Template export cleanup (remove sample data)

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Open |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| Resolution | — |
| Resolved Date | — |
| Verified By | — |
| Git Ref | — |

---

### DEF-042 — Help text/example updates in admin pages

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Updated Help modal in Product Types to remove all hardcoded fake examples. Hierarchy levels now use generic labels. Product assignment example uses generic flow. File: `ProductTypes.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-043 — Blue box descriptions removal in admin popups

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Changed info hint text and field descriptions in Add/Edit modals to generic language. Removed fake examples. Inline examples now dynamically pulled from actual loaded hierarchy data, labeled "Existing". File: `product-types.js`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-044 — Company Email/URL showing "Available upon request" / "Not provided" instead of dash

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Changed placeholders to display dash (—) across all 4 detail views |
| Resolved Date | 2026-03-23 |
| Verified By | Pending |
| Git Ref | `5337f1f` |

---

### DEF-045 — Tag spacing too tight against card actions bar

| Field | Value |
|-------|-------|
| Source | Internal-Test |
| Reported By | Steve |
| Reported Date | 2026-03-23 |
| Priority | Low |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Added 8px bottom margin to .certificate-tags inside tile cards |
| Resolved Date | 2026-03-23 |
| Verified By | Steve |
| Git Ref | `c0c006c` |

---

### DEF-046 — Password reset enforcement not working

| Field | Value |
|-------|-------|
| Source | Internal-Test |
| Reported By | Steve |
| Reported Date | 2026-03-19 |
| Priority | High |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Users with RequirePasswordReset=true now forced to change password on login |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `a88e3d4` |

---

### DEF-047 — User Management checkbox not displaying as proper square

| Field | Value |
|-------|-------|
| Source | Internal-Test |
| Reported By | Steve |
| Reported Date | 2026-03-19 |
| Priority | Low |
| Status | Closed |
| Assigned To | Claude |
| Related CR/ISS | — |
| Resolution | Fixed checkbox rendering in User Management |
| Resolved Date | 2026-03-19 |
| Verified By | Pending |
| Git Ref | `a88e3d4` |

---

## Backfilled from QC Log — 2026-03-23

> The following 49 defects (DEF-048 through DEF-096) were extracted from `Database QC Log_3.13.26.xlsx` rows that were not previously captured in this register. All are marked **New** status pending triage.

---

### DEF-048 — Cert list view missing fields, needs scroll bar + frozen headers

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Amanda/Dan |
| Reported Date | 2025-11-04 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R16 |
| Description | In list form some details/fields are missing (Products & Company name). Should have all certificate details. Needs scroll bar on top and frozen column headers. |

---

### DEF-049 — Remove filter cleared / Sort By complete popup notifications

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R29 |
| Description | Should not need popup that filters were cleared or that Sort By was complete. Especially if adding filter tags at top as discussed on 3/6 call. See image 5. |

---

### DEF-050 — "General Product" showing incorrectly, causing filter issues

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R44 |
| Description | What does "General Product" mean? Where is this pulled from — what dropdown option? Don't think it is correct and it's causing filtering issues. See image 21. |

---

### DEF-051 — GCC logo should always link back to cert homepage

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R45 |
| Description | Cert detail page opens in new tab (good), but no way to get back to cert homepage. The GCC logo in header should always link back to the cert homepage — for all pages and login types. |

---

### DEF-052 — No error message on incorrect login credentials

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R47 |
| Description | No error message is appearing when an incorrect email/password combo is entered on the Sign In page. |

---

### DEF-053 — Add "Return to GreenCircle database" link from Sign In page

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R48 |
| Description | There should be a way to get back to the public homepage from the Sign In page. Add link: "Return to GreenCircle database >" |

---

### DEF-054 — Error when selecting Certification Type on cert creation

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R51 |
| Description | Getting error when selecting Certification Type in cert creation form. |

---

### DEF-055 — Mobile cert creation page distorted and non-functional

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R55 |
| Description | Text/boxes get distorted on mobile view for cert creation and it's not functional. Low priority — don't think we will be creating certs from mobile. See image 10. |

---

### DEF-056 — Random characters appearing in names

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R56 |
| Description | Random characters showing in names. Was it inputted this way or another issue? Anuki to confirm. See image 11. |

---

### DEF-057 — Remove "All Categories" and "All Types" from dropdowns

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R58 |
| Description | Remove "All Categories" as an option under Product Category. Remove "All Types" from Product Type. This creates filtering issues when sorting. |

---

### DEF-058 — Unclear error message during cert creation (image 13)

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R62 |
| Description | "What does this mean?" — unclear error message shown during cert creation. See image 13. |

---

### DEF-059 — Cert image cutoff and button misalignment after creation

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R65 |
| Description | After creating a cert, the cert image is getting cut off on the confirmation page. Buttons are also not aligned correctly. See image 14. |

---

### DEF-060 — Status should only show for logged-in users

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R66 |
| Description | Make sure Status only shows for logged in people — should not be visible on public views. |

---

### DEF-061 — Company Email/URL fields missing from cert creation form

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R67 |
| Description | Never saw a field for Company Email/URL to fill out in cert creation but it showed up on preview. Add fields to creation form. |

---

### DEF-062 — Arrow blocking text on cert creation page

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-09 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R68 |
| Description | Fix arrow blocking text on cert creation page. See image 15. |

---

### DEF-063 — Duplicate cert showing in analyst listing

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R69 |
| Description | Cert 26-0005 is showing twice in the analyst cert listing. There should only ever be one listing per cert number. |

---

### DEF-064 — Analyst cert listing page missing pagination

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-005 |
| QC Log Row | R70 |
| Description | Similar to public page — how many will appear on a page? There are no page numbers to navigate on the analyst cert listing. |

---

### DEF-065 — Dashboard statistics inaccurate

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R72 |
| Description | Dashboard numbers not accurate: 154 companies with active certs when only 21 active certs exist — likely counting all companies not just those with active certs. Same logic issue for product categories (60), product types (93), certs published this year (24). Certs published last year stays 0 even after changing a cert's effective year to 2025. |

---

### DEF-066 — Dashboard expired count wrong

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R73 |
| Description | Under # Certs by Status, Expired says 3 but none have that status. Make counts match the status types we are keeping. Expiring Soon is not a status type and timeline not defined. |

---

### DEF-067 — Dashboard shows "No product type data available"

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R74 |
| Description | Dashboard says "No product type data available" when there should be product type data. |

---

### DEF-068 — Dashboard Top 5 Product Categories showing cert types instead

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R75 |
| Description | "Top 5 Product Categories by Active Cert Count" is showing certificate types (Recycled Content, etc.) instead of actual product categories. Data source is wrong. |

---

### DEF-069 — Auto-logout shows error page instead of login redirect

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-009 |
| QC Log Row | R76 |
| Description | Instead of redirecting to login screen on auto-logout, shows "This page isn't working" error at /Account/LogoutPost. Need graceful redirect. Also confirm: what is the auto-logout inactivity time? |

---

### DEF-070 — Dashboard export count mismatch

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-007 |
| QC Log Row | R78 |
| Description | Total Active Certs shows 21 (correct) but the export only listed 19 results. Export count does not match displayed count. Other exports not verified — Anuki needs to check all. |

---

### DEF-071 — Cert export missing many fields

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R82 |
| Description | Certificate listing "Export All to Excel" does not include every field: missing product type, cert subcategory, product subcategory, cert results, pathway, cert notes, etc. Must also include admin-added custom fields. |

---

### DEF-072 — Product ID Manager search not working

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R84 |
| Description | Search bar does not appear to work under Product ID Manager. |

---

### DEF-073 — Cert details should open in new tab from listing page

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R88 |
| Description | When opening certificate details via arrow icon on the cert listing page, it should open in a new tab. |

---

### DEF-074 — Move Company Contact Email/URL to last row in cert table

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R89 |
| Description | Move Company Contact Email/URL to last row in the certificate listing table. |

---

### DEF-075 — Horizontal scroll UX in cert listing needs improvement

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R91 |
| Description | Need to find a better way to scroll horizontally in list — UX is hard. Referenced Wrike as example where you can scroll in table with mouse vs having to use scroll bar specifically. |
| Resolution | Added `word-wrap: break-word` and `overflow-wrap` on `<td>`. Added horizontal scroll (`overflow-x: auto`) on table container with `min-width: 1100px`. File: `product-management.css`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-076 — Status green box styling inconsistent in cert listing

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R92 |
| Description | Under Status column, some "Active" values show in green box, others without. Be consistent — either all with or all without. See image 26. |

---

### DEF-077 — Product Management font size consideration

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-038 |
| QC Log Row | R94 |
| Description | Similar to cert listing page request — could make font smaller in Product Management table to help with wrapping. Not required but consideration. |

---

### DEF-078 — Product Management pagination needed

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | DEF-005 |
| QC Log Row | R95 |
| Description | Product Management page will get very long with lots of data. Need to stop at 50 and go to different page with page numbers at bottom. |
| Resolution | Added page-number pagination at 50 rows per page. Previous/Next arrows, numbered buttons with ellipsis, resets to page 1 on filter/search. Files: `Management.cshtml`, `product-management.css`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-079 — Remove Status/SKU/Model Number from Add Product popup

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R97 |
| Description | Add New Product popup should not have Status, SKU, or Model Number fields. Remove them. |
| Resolution | Removed Status dropdown, SKU input, and Model Number input from modal completely. `saveProduct` sends `isActive: true` directly; `sku` and `model` omitted. File: `Management.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-080 — Product Type/Subcategory not appearing in Add Product popup

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R98 |
| Description | No options are appearing for Product Type and Product Subcategory in the Add New Product popup on Product Management page. |
| Resolution | Added cascade logic: Category selection filters Product Type dropdown (client-side from preloaded data). Product Type selection fetches Subcategory options via `/Certificate/GetProductSubcategories` API. `productSubcategoryId` now included in save payload. File: `Management.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-081 — Product export missing Subcategory field

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R99 |
| Description | Product export should include all fields available at product level — currently missing Subcategory. |
| Resolution | Export CSV headers and data rows now include Product Type and Subcategory columns. File: `Management.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-082 — Product Management sorting not working for all columns

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R100 |
| Description | Sorting ascending/descending not working for Company and Category columns (or only works 1 time and won't toggle). Certificates and Identifiers columns do not sort at all. |
| Resolution | Root cause: `data-sort` attribute values didn't match JS property names. Added `sortMap` to translate. Added numeric sorting for Certificates and Identifiers columns. Fixed sort icon reset on column change. File: `Management.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-083 — Company required validation missing in Add Product popup

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R102 |
| Description | When adding a new product, Company has to be filled in — cannot be left blank. Add required validation. |

---

### DEF-084 — Product delete not working + unclear error message

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-019 |
| QC Log Row | R103 |
| Description | Got error when trying to delete a product — trash can functionality not working. Products tied to certificates should show clear error that they cannot be deleted. Even products with 0 certificates won't delete. Error message needs to be more clear. See image 29. |

---

### DEF-085 — Product Identifier Import only importing 1 of 2 products

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R104 |
| Description | Went to add 2 products from Excel and only 1 added to page. Ensure import working correctly. |

---

### DEF-086 — Change "Verification URL" label to "Database Listing URL"

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-12 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R105 |
| Description | On cert detail page, change "Verification URL" to "Database Listing URL". See image 30. |

---

### DEF-087 — Product export includes sample data rows

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-041 |
| QC Log Row | R107 |
| Description | Product Management export is including 2 rows of sample/test data. This will cause issues — remove test data from export. |

---

### DEF-088 — Product Template missing required fields

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R108 |
| Description | Product Management Template must include all the fields from the Add New Product popup. Right now it only has a few of the fields. |

---

### DEF-089 — Role page: clarify Manage Workflow, Audit Logs, update Tag description

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R110 |
| Description | What is "Manage Workflow" option — what access is this? Where do you see Audit Logs? Update "Manage Tags" description to "Classification tags (internal and external)". |
| Resolution | Updated "Manage Workflow" description to "Manage certificate workflow state transitions". Updated "Manage Tags" to "Classification tags (internal and external)". Note: Manage Workflow permission exists in seed data but is NOT enforced by any controller. Audit Logs page does not exist — permission is placeholder. File: `SeedData.cs`. SQL UPDATE needed for live DB. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-090 — Add New User popup missing Company field

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | — |
| QC Log Row | R111 |
| Description | "Add New User" popup is missing Company field (field exists in export but not in creation form). |
| Resolution | Company field was conditionally hidden, only shown for External role. Fixed to show for all roles except Partner. Help text updated. Files: `userManagement.js`, `UserManagement.cshtml`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-091 — User Template remove sample data

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | DEF-041 |
| QC Log Row | R113 |
| Description | Remove sample data from User Management Template export. |
| Resolution | Template CSV now outputs headers only with no sample "John Doe" row. File: `userManagement.js`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-092 — User CSV import not working

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | High |
| Status | Fixed |
| Assigned To | Claude |
| Related CR/ISS | DEF-090, DEF-091 |
| QC Log Row | R114 |
| Description | Tried to import user through CSV file and got failure notice. Import functionality not working. |
| Resolution | Root cause same as DEF-090/DEF-091 — `CreateUserAsync` caught all exceptions and returned `false`. Changed return type to `(bool Success, string ErrorMessage)` tuple, added duplicate email check. Import error messages now include actual failure reason. Files: `AdminService.cs`, `IAdminService.cs`, `AdminController.cs`. |
| Resolved Date | 2026-03-24 |
| Verified By | Pending |
| Git Ref | `786dc55` |

---

### DEF-093 — Partner API descriptions update (Novi/Wayfair swap)

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Low |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R115 |
| Description | Change Novi description to: "Novi Connect is an AI shopping optimization platform." Move current Novi description to Wayfair: "Online furniture and home goods retailer focused on sustainable product offerings." |

---

### DEF-094 — Certificate Field Management section headers confusion

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | — |
| QC Log Row | R123 |
| Description | What do the different Section options connect to? All added fields appear under "Additional Information" section regardless of selected section. Don't think we need section headers — all should go under Additional Information. |

---

### DEF-095 — Custom fields auto-add to exports and table views

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-071 |
| QC Log Row | R124 |
| Description | Confirm that as new custom fields are added they automatically appear in the full export. Does it get added to any of the table views? |

---

### DEF-096 — Tags Template remove sample data

| Field | Value |
|-------|-------|
| Source | UAT |
| Reported By | Carly |
| Reported Date | 2026-03-13 |
| Priority | Medium |
| Status | New |
| Assigned To | Unassigned |
| Related CR/ISS | DEF-041 |
| QC Log Row | R125 |
| Description | Remove sample data from Tags Template export. |

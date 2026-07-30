# Change Request Register

**Project:** GCC Database Solution
**Created:** 2026-03-23
**Last Updated:** 2026-04-24 (CR-007 added)

## Status Definitions

| Status | Meaning |
|--------|---------|
| Submitted | Requested, awaiting review |
| Under-Review | Being assessed for impact and effort |
| Approved | Accepted, ready for implementation |
| In-Progress | Actively being implemented |
| Completed | Delivered and verified |
| Rejected | Not accepted (requires justification) |
| Deferred | Accepted but postponed (requires target date) |

## Type Definitions

| Type | Meaning |
|------|---------|
| Enhancement | Improvement to existing functionality |
| New-Requirement | Entirely new capability |
| Scope-Change | Modification to agreed project scope |
| Modification | Change to existing requirement or behavior |

---

## Change Request Summary

| Status | Count |
|--------|-------|
| Submitted | 0 |
| Under-Review | 0 |
| Approved | 0 |
| In-Progress | 4 |
| Completed | 3 |
| Rejected | 0 |
| Deferred | 0 |
| **Total** | **7** |

---

## Change Request Log

### CR-001 — Add Certification Results field to certificate upload

| Field | Value |
|-------|-------|
| Requested By | Carly (GCC) |
| Request Date | 2026-02-05 |
| Type | New-Requirement |
| Priority | Medium |
| Status | In-Progress |
| Assigned To | Claude |
| Estimated Effort | 2 hrs |
| Actual Effort | — |
| Impact Assessment | Add optional free-text field to Create/Edit forms. Display on detail views. Include in CSV export. Amazon may require this field for CPF threshold validation. |
| Approval | Carly / 2026-02-05 |
| Related DEF/ISS | — |
| Resolution | — |
| Completed Date | — |
| Git Ref | — |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-02-05 | Submitted — Amazon wants threshold data passthrough | Carly |
| 2026-02-05 | Approved — scope change document created | Steve |
| 2026-03-23 | Added to tracking register | Claude |

---

### CR-002 — Tie products to companies with cascading dropdowns

| Field | Value |
|-------|-------|
| Requested By | Carly (GCC) |
| Request Date | 2026-03-13 |
| Type | Enhancement |
| Priority | High |
| Status | Completed |
| Assigned To | Claude |
| Estimated Effort | 4 hrs |
| Actual Effort | 4 hrs |
| Impact Assessment | Product dropdown must filter by selected company. Category/type/subcategory must cascade from product selection. Affects Create, Edit, and inline Add Product modal. |
| Approval | Steve / 2026-03-13 |
| Related DEF/ISS | DEF-006 |
| Resolution | Added company→product→category cascading across Create, Edit, and inline modal. 5 files changed. |
| Completed Date | 2026-03-23 |
| Git Ref | `1ed2f12` |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-03-13 | Submitted via QC log — product not tied to company | Carly |
| 2026-03-13 | Approved | Steve |
| 2026-03-23 | Implemented — cascading dropdowns across all cert forms | Claude |
| 2026-03-23 | Completed | Claude |

---

### CR-003 — Replace 14-state status system with 8 client-defined statuses

| Field | Value |
|-------|-------|
| Requested By | Carly (GCC) |
| Request Date | 2026-03-13 |
| Type | Scope-Change |
| Priority | Critical |
| Status | Completed |
| Assigned To | Claude |
| Estimated Effort | 6 hrs |
| Actual Effort | 6 hrs |
| Impact Assessment | Complete overhaul of certificate status system. Affects 25+ files: all API controllers, forms, dashboard, exports, JavaScript, validation, import, filter, ETL. Requires SQL migration script. Public view visibility rules change. |
| Approval | Steve / 2026-03-19 |
| Related DEF/ISS | DEF-001, DEF-021, DEF-034 |
| Resolution | Replaced 14-state system with 8 statuses. Public view shows only "Client Approved - Paid" and "Republished" with valid dates. SQL migration script created. |
| Completed Date | 2026-03-19 |
| Git Ref | `024d209` |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-03-13 | Submitted — QC log items about wrong statuses and expired certs showing | Carly |
| 2026-03-19 | Approved and implemented — 25+ files, SQL migration | Claude |
| 2026-03-19 | Completed | Claude |

---

### CR-004 — Amazon API scope expansion (Certification Data Enhancements)

| Field | Value |
|-------|-------|
| Requested By | Kyle Bowman (Amazon) via Carly |
| Request Date | 2026-02-05 |
| Type | Scope-Change |
| Priority | Medium |
| Status | Completed |
| Assigned To | Claude |
| Estimated Effort | 8 hrs |
| Actual Effort | 8 hrs |
| Impact Assessment | Amazon wants richer certification data display and CPF filtering by percentage values. Scope change document prepared. Most capabilities already existed (Certification Results field, type hierarchy, Amazon codes, GS1 API, partner filtering). |
| Approval | Steve / 2026-02-05 |
| Related DEF/ISS | — |
| Resolution | Scope change document created and delivered. System assessment showed most capabilities already built. Certification Results field (CR-001) is the remaining piece. |
| Completed Date | 2026-02-05 |
| Git Ref | — |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-02-05 | Submitted — Amazon expanded CPF integration requirements | Carly |
| 2026-02-05 | Scope change document created and approved | Steve |
| 2026-02-05 | Assessment completed — most capabilities already built | Claude |

---

### CR-005 — Add desktop Hide/Show Filters control to public certificate listing

| Field | Value |
|-------|-------|
| Requested By | Shawn |
| Request Date | 2026-04-24 |
| Type | Enhancement |
| Priority | Low |
| Status | In-Progress |
| Assigned To | Claude |
| Estimated Effort | 0.5 hr |
| Actual Effort | 0.5 hr |
| Impact Assessment | Public-facing `/Certificate/CertificateTileView` was missing the desktop "Hide Filters" / "Show Filters" sidebar collapse controls that already exist on the Internal (analyst) and External (logged-in partner) views. The mobile "Show Filters & Search" toggle was present, but desktop public users had no way to collapse the filter sidebar to widen the tile grid. Identified during SIT screenshot review (2026-04-24). Scope: 1 markup file + 1 JS file. No CSS changes (existing `.collapsed` / `.sidebar-collapsed` rules in shared `certificatetileview.css` cover the new buttons). |
| Approval | Shawn / 2026-04-24 |
| Related DEF/ISS | DEF-098, DEF-099 (sibling work on the internal/external sidebar controls) |
| Resolution | Markup added to `Views/Certificate/CertificateTileView.cshtml` — `sidebar-toggle-tab` (Show Filters edge tab) and `sidebar-collapse-btn` (Hide Filters button), with sidebar `<div>` upgraded to `<aside id="filterSidebar">`. JS added to `wwwroot/js/certificate-tile-view.js` — `setupSidebarToggle()`, `collapseSidebar()`, `expandSidebar()` matching internal/external behavior. Wired into `initialize()`. |
| Completed Date | TBD (pending deploy) |
| Git Ref | TBD |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-04-24 | Identified during SIT screenshot review — public view missing desktop sidebar collapse | Shawn / Claude |
| 2026-04-24 | Approved as CR-005 | Shawn |
| 2026-04-24 | Implemented + locally validated | Claude |

---

### CR-006 — Replace "Data not available" placeholder with em dash on certificate detail views

| Field | Value |
|-------|-------|
| Requested By | Carly (GCC) via Shawn |
| Request Date | 2026-04-24 |
| Type | Modification |
| Priority | Low |
| Status | In-Progress |
| Assigned To | Claude |
| Estimated Effort | 0.25 hr |
| Actual Effort | 0.25 hr |
| Impact Assessment | Cosmetic placeholder change on the three certificate detail views (`CertificateSingleView` public, `CertificateExternalView` partner, `CertificateInternalView` analyst/admin). Six fields per view show "Data not available" when the underlying value is null: Product Category, Product Type, Product Subcategory, Location, Company Email, Company URL. Replaced visible text with em dash (—, U+2014) while keeping the `<span class="no-data">` wrapper so existing dimmed-text styling still applies. 18 instances total across 3 files. No data model, controller, or API changes — display-only. Out of scope: tile-card listings (`'N/A'` placeholders in JS), PDF export, partner API responses, server log messages — left as-is pending separate confirmation. |
| Approval | Shawn / 2026-04-24 |
| Related DEF/ISS | — |
| Resolution | `Views/Certificate/CertificateSingleView.cshtml` (6), `Views/Certificate/CertificateExternalView.cshtml` (6), `Views/Certificate/CertificateInternalView.cshtml` (6) — each `<span class='no-data'>Data not available</span>` → `<span class='no-data'>—</span>`. Razor views require app restart to pick up. |
| Completed Date | TBD (pending deploy) |
| Git Ref | TBD |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-04-24 | Submitted — client (Carly) wants em dash instead of "Data not available" placeholder text on detail pages | Shawn |
| 2026-04-24 | Scoped to 3 cert detail views, 18 instances. Tile listings, PDF, partner API explicitly excluded | Claude |
| 2026-04-24 | Approved as CR-006 | Shawn |
| 2026-04-24 | Implemented + locally validated | Claude |
| 2026-04-25 | Extension — same 3 cert detail views had OTHER missing-data placeholders ("Unknown Company", "Unknown Product", "Unknown Location", "Unknown Country", "Unknown" for dates/status, "N/A" for cert number) that CR-006 didn't sweep. Replaced all with "—" for consistency. ~18 additional instances across the 3 files. | Claude |

---

### CR-007 — Append admin-managed Custom Fields as columns on Internal cert table view

| Field | Value |
|-------|-------|
| Requested By | Shawn |
| Request Date | 2026-04-24 |
| Type | Enhancement |
| Priority | Medium |
| Status | In-Progress |
| Assigned To | Claude |
| Estimated Effort | 2 hrs |
| Actual Effort | TBD |
| Impact Assessment | Internal cert listing's List/Table view (`CertificateTileViewInternal.cshtml`) renders 24 hardcoded columns (Cert#, Type, Status, Dates, Company, Product, Cat/Type/Subcat, Location, Country, Tags, Partner, ASIN/UPC/GTIN/EAN, Contact, Cert Results, Pathway, Cert Subcats, Contact URL). The admin-managed Custom Fields (created via `/Admin/CertificateFields`) DO render on the cert detail page (`CertificateInternalView`) but are completely absent from the table — the API doesn't return them, the column toggle doesn't list them, the row renderer doesn't output them. Scope: append all `IsVisible=true` custom fields as new columns at the end of the table, in `SortOrder`. Default OFF in the column toggle (so 30+ fields don't blow up the table on first load). All custom fields go under a single "Custom Fields" divider (sorted by `SortOrder`) — do NOT sub-group by the `CustomField.Section` property; that column is legacy. Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `Views/Certificate/CertificateTileViewInternal.cshtml`, `wwwroot/js/certificate-tile-view-internal.js`. **Explicitly out of scope** (separate CRs if Carly wants them): server-side `/Certificate/ExportCertificates` Excel/CSV export, sorting/filtering by custom-field columns, Partner API custom field exposure (deferred to CR-008 — needs Carly + Amazon contract review). |
| Approval | Shawn / 2026-04-24 |
| Related DEF/ISS | — |
| Resolution | TBD |
| Completed Date | TBD |
| Git Ref | TBD |

#### History
| Date | Action | By |
|------|--------|----|
| 2026-04-24 | Identified — admin-managed Custom Fields render on cert detail but not in the Internal table list view | Shawn |
| 2026-04-24 | Confirmed scope: internal table view only; Partner API split to CR-008 | Shawn |
| 2026-04-24 | Approved as CR-007 | Shawn |
| 2026-04-24 | Implemented + locally validated (1 visible cf in local DB; render path + toggle + persistence all confirmed) | Claude |
| 2026-04-25 | Refinement: collapsed dropdown to single "Custom Fields" divider (don't sub-group by `Section`) per Shawn | Claude |
| 2026-04-25 | Readability fixes on the table: cells wrap (no more `nowrap`/ellipsis truncation), Status badge wraps (so "Client Approved - Paid" reads in full instead of "CLIENT APPR"), Actions column widened 90→110px, action icons stay centered when rows grow tall. CSS only — `wwwroot/css/certificatetileview.css` + `wwwroot/css/pages/certificate-tile-internal.css` | Claude |

---

## Template — Copy for New Change Requests

```
### CR-XXX — [Title]

| Field | Value |
|-------|-------|
| Requested By | [Name] |
| Request Date | YYYY-MM-DD |
| Type | [Enhancement/New-Requirement/Scope-Change/Modification] |
| Priority | [Critical/High/Medium/Low] |
| Status | Submitted |
| Assigned To | Unassigned |
| Estimated Effort | — |
| Actual Effort | — |
| Impact Assessment | [What areas are affected] |
| Approval | — |
| Related DEF/ISS | — |
| Resolution | — |
| Completed Date | — |
| Git Ref | — |

#### History
| Date | Action | By |
|------|--------|----|
| YYYY-MM-DD | Submitted — [reason] | [Name] |
```

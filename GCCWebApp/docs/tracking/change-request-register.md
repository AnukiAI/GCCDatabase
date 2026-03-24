# Change Request Register

**Project:** GCC Database Solution
**Created:** 2026-03-23
**Last Updated:** 2026-03-23

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
| In-Progress | 1 |
| Completed | 3 |
| Rejected | 0 |
| Deferred | 0 |
| **Total** | **4** |

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

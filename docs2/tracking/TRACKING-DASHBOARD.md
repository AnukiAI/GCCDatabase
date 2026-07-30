# GCC Project Tracking Dashboard

**Project:** GCC Database Solution
**Last Updated:** 2026-03-25
**Prepared By:** Anuki Consulting

---

## At a Glance

| Metric | Value |
|--------|-------|
| Total Defects | 96 |
| New (backfilled, untriaged) | 36 |
| Open Defects | 15 |
| Fixed (Pending Verification) | 34 |
| Closed (Verified) | 14 |
| Defect Close Rate | 50% (fixed + closed of total) |
| Open Issues | 2 |
| Change Requests (Total) | 4 |
| Change Requests (In-Progress) | 1 |
| Total Effort Logged | 44.5 hrs |

---

## Defect Burndown

```
Full QC Log (3/13/2026):   121 line items (20 Amanda/Dan Nov '25 + 101 Carly Mar '26)
Tracked in register:       96 defects (47 original + 49 backfilled 3/23)
Fixed/Closed to date:      48 (14 fixed on 3/24)
New (untriaged):           36
Remaining open:            15

New (untriaged) by Priority:
  High:     15   ████████████████████████████████████████████████████████████
  Medium:   16   ████████████████████████████████████████████████████████████████
  Low:       5   ████████████████████

Open (previously triaged) by Priority:
  Critical:  0   (all 5 critical fixed 3/23)
  High:      4   ████████████████
  Medium:    8   ████████████████████████████████
  Low:       3   ████████████
```

---

## Critical Open Items

**All critical defects have been fixed as of 2026-03-23.** Pending verification.

| ID | Title | Status | Fixed Date |
|----|-------|--------|------------|
| DEF-002 | Auto-expire certs | Fixed | 2026-03-23 |
| DEF-003 | Sort/filter on public view | Fixed | 2026-03-23 |
| DEF-004 | Mobile/tablet filters | Fixed | 2026-03-23 |
| DEF-005 | Pagination missing | Fixed | 2026-03-23 |
| DEF-007 | Dashboard exports dumping code | Fixed | 2026-03-23 |
| DEF-009 | Session timeout loses work | Fixed | 2026-03-23 |

---

## Change Requests In Flight

| ID | Title | Status | Priority |
|----|-------|--------|----------|
| CR-001 | Add Certification Results field | In-Progress | Medium |

---

## Open Issues

| ID | Title | Category | Priority |
|----|-------|----------|----------|
| ISS-001 | Test suites lack user journey/E2E/edge coverage | Process | High |
| ISS-002 | Single tester for full application UAT | Operational | Medium |

---

## Recent Activity (Last 7 Days)

| Date | Key Activity | Items |
|------|-------------|-------|
| 2026-03-24 | **14 defects fixed (QC Sprint 1).** Product Mgmt: pagination, sort arrows, text wrapping, sorting, cascading dropdowns, export subcategory, remove Status/SKU/Model. Admin: breadcrumbs (19 views), role descriptions, Add User Company field, user template/import. Product Types: blue box descriptions, help examples. Deployed v2.0.0. | DEF-035, DEF-039, DEF-042, DEF-043, DEF-075, DEF-078–082, DEF-089–092 |
| 2026-03-23 | **6 critical defects fixed.** Backfilled 49 missing QC log items (DEF-048–DEF-096) as New status. Plus cascading dropdowns, Remember Me, placeholders. | DEF-002–009, DEF-031, DEF-044–045, DEF-048–096, CR-002 |
| 2026-03-19 | 8-state status overhaul, batch QC fixes, password reset, checkbox | CR-003, DEF-001, DEF-008–DEF-018, DEF-021, DEF-034, DEF-046, DEF-047 |

---

## Tracker Index

| Tracker | File | Items |
|---------|------|-------|
| Defect Register | [defect-register.md](defect-register.md) | 96 items (36 new, 15 open, 20 fixed, 14 closed) |
| Issue Register | [issue-register.md](issue-register.md) | 2 items |
| Change Request Register | [change-request-register.md](change-request-register.md) | 4 items |
| Progress Log | [progress-log.md](progress-log.md) | 4 date entries |
| Effort Log | [effort-log.md](effort-log.md) | 44.5 hrs logged |

---

## New Items — Backfilled from QC Log (36 remaining, pending triage)

### High Priority (15)

| ID | QC Row | Page | Description |
|----|--------|------|-------------|
| DEF-051 | R45 | Public view | GCC logo should link to cert homepage |
| DEF-052 | R47 | Sign In | No error message on bad login |
| DEF-054 | R51 | Create cert | Error when selecting Certification Type |
| DEF-060 | R66 | Public view | Status showing to non-logged-in users |
| DEF-061 | R67 | Create cert | Company Email/URL fields missing |
| DEF-063 | R69 | Analyst listing | Duplicate cert showing |
| DEF-064 | R70 | Analyst listing | Pagination missing |
| DEF-065 | R72 | Dashboard | Statistics inaccurate |
| DEF-069 | R76 | Login | Auto-logout shows error page |
| DEF-070 | R78 | Dashboard export | Export count mismatch |
| DEF-071 | R82 | Cert export | Missing many fields |
| DEF-084 | R103 | Product Mgmt | Delete not working |
| DEF-085 | R104 | Product IDs | Import only adding 1 of 2 |
| DEF-087 | R107 | Product export | Includes sample data |
| DEF-088 | R108 | Product Template | Missing fields |

### Medium Priority (16)

| ID | QC Row | Page | Description |
|----|--------|------|-------------|
| DEF-048 | R16 | Cert list | Missing fields, needs scroll bar + frozen headers |
| DEF-050 | R44 | Public view | "General Product" causing filter issues |
| DEF-053 | R48 | Sign In | Add "Return to database" link |
| DEF-056 | R56 | Global | Random characters in names |
| DEF-057 | R58 | Create cert | Remove "All Categories"/"All Types" from dropdowns |
| DEF-058 | R62 | Create cert | Unclear error message |
| DEF-059 | R65 | Post-creation | Cert image cutoff, buttons misaligned |
| DEF-066 | R73 | Dashboard | Expired count wrong |
| DEF-067 | R74 | Dashboard | "No product type data available" |
| DEF-068 | R75 | Dashboard | Showing cert types as product categories |
| DEF-072 | R84 | Product IDs | Search not working |
| DEF-083 | R102 | Product Mgmt | Company required validation missing |
| DEF-094 | R123 | Cert Fields | Section headers confusion |
| DEF-095 | R124 | Cert Fields | Custom fields auto-add to exports? |
| DEF-096 | R125 | Tags | Template remove sample data |

### Low Priority (5)

| ID | QC Row | Page | Description |
|----|--------|------|-------------|
| DEF-049 | R29 | Public view | Remove filter/sort popup notifications |
| DEF-055 | R55 | Create cert | Mobile creation distorted |
| DEF-062 | R68 | Create cert | Arrow blocking text |
| DEF-073 | R88 | Cert listing | Open details in new tab |
| DEF-074 | R89 | Cert listing | Move Email/URL to last row |
| DEF-076 | R92 | Cert listing | Status green box inconsistent |
| DEF-077 | R94 | Product Mgmt | Font size consideration |
| DEF-086 | R105 | Cert detail | Change "Verification URL" to "Database Listing URL" |
| DEF-093 | R115 | Partners | API descriptions update (Novi/Wayfair) |

---

## Verification Backlog

> These items have been fixed but need testing confirmation (`Verified By: Pending`).

| ID | Title | Fixed Date |
|----|-------|------------|
| DEF-001 | Expired/Draft/Hold certs on public view | 2026-03-19 |
| DEF-002 | Auto-expire certs on login | 2026-03-23 |
| DEF-003 | Sort/filter initialization order | 2026-03-23 |
| DEF-004 | Mobile/tablet filters and toggle | 2026-03-23 |
| DEF-005 | Admin pagination (User/Product Mgmt) | 2026-03-23 |
| DEF-007 | Dashboard exports auth redirect | 2026-03-23 |
| DEF-008 | CSV export date format | 2026-03-19 |
| DEF-009 | Session timeout form data backup | 2026-03-23 |
| DEF-010 | User creation in Admin | 2026-03-19 |
| DEF-011 | Cert types missing from dropdown | 2026-03-19 |
| DEF-012 | Countries list only 5 entries | 2026-03-19 |
| DEF-013 | Company autocomplete | 2026-03-19 |
| DEF-014 | Product ID Manager edit freezes | 2026-03-19 |
| DEF-016 | Product Mgmt missing columns | 2026-03-19 |
| DEF-018 | Product types always Chemicals | 2026-03-19 |
| DEF-021 | Expiring Soon tag removal | 2026-03-19 |
| DEF-031 | Remember Me inconsistent | 2026-03-23 |
| DEF-034 | Status types update | 2026-03-19 |
| DEF-035 | Breadcrumbs broken links (19 views) | 2026-03-24 |
| DEF-039 | Sort arrows overlapping column names | 2026-03-24 |
| DEF-042 | Help text/example updates in admin | 2026-03-24 |
| DEF-043 | Blue box descriptions in admin popups | 2026-03-24 |
| DEF-044 | Email/URL placeholders | 2026-03-23 |
| DEF-046 | Password reset enforcement | 2026-03-19 |
| DEF-047 | User Mgmt checkbox | 2026-03-19 |
| DEF-075 | Horizontal scroll UX in Product Mgmt | 2026-03-24 |
| DEF-078 | Product Mgmt pagination (50/page) | 2026-03-24 |
| DEF-079 | Remove Status/SKU/Model from Add Product | 2026-03-24 |
| DEF-080 | Product Type/Subcategory in Add Product | 2026-03-24 |
| DEF-081 | Product export includes Subcategory | 2026-03-24 |
| DEF-082 | Product Mgmt sorting all columns | 2026-03-24 |
| DEF-089 | Role permission descriptions clarified | 2026-03-24 |
| DEF-090 | Add User Company field for all roles | 2026-03-24 |
| DEF-091 | User template sample data removed | 2026-03-24 |
| DEF-092 | User CSV import with error messages | 2026-03-24 |

---

## Maintenance Notes

- Update this dashboard whenever defect/issue/CR statuses change
- Regenerate summary counts from the individual registers
- Review verification backlog weekly — items pending verification need tester sign-off
- Effort log running totals should match the summary here

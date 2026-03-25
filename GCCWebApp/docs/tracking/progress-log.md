# Progress Log

**Project:** GCC Database Solution
**Created:** 2026-03-23
**Last Updated:** 2026-03-25

> Entries are reverse-chronological (newest first). Each entry records what was done, which tracked items were addressed, and the outcome.

---

## 2026-03-24

| Activity | Items Addressed | Outcome | Performed By |
|----------|-----------------|---------|--------------|
| Fixed Product Mgmt sort arrows overlapping | DEF-039 | Reduced icon size to 10px, added `white-space: nowrap`, `min-width` on columns. Files: `product-management.css`, `Management.cshtml` | Claude |
| Fixed Product Mgmt text wrapping/horizontal scroll | DEF-075 | Added `word-wrap`, `overflow-wrap`, horizontal scroll with `min-width: 1100px`. File: `product-management.css` | Claude |
| Added Product Mgmt pagination | DEF-078 | 50 rows per page, numbered buttons with ellipsis, resets on filter/search. Files: `Management.cshtml`, `product-management.css` | Claude |
| Added Product Type/Subcategory columns | DEF-080 | Added `ProductSubcategoryName` to API response and view model. Two new sortable columns. Files: `ProductController.cs`, `Management.cshtml` | Claude |
| Removed Status/SKU/Model from Add Product | DEF-079 | Removed 3 fields completely from modal. `isActive: true` sent directly. File: `Management.cshtml` | Claude |
| Fixed Product Type/Subcategory cascade in Add Product | DEF-080 | Category→Type client-side filter, Type→Subcategory via API fetch. File: `Management.cshtml` | Claude |
| Fixed Product export to include Subcategory | DEF-081 | Added Product Type and Subcategory columns to CSV export. File: `Management.cshtml` | Claude |
| Fixed Product Mgmt sorting all columns | DEF-082 | Added `sortMap` for property name translation. Numeric sort for Certificates/Identifiers. File: `Management.cshtml` | Claude |
| Fixed breadcrumbs across 19 views | DEF-035 | All `href="#"` links fixed. Admin→Dashboard, categories→non-clickable `<span>`, current page→`<span class="current">`. 19 .cshtml files | Claude |
| Updated role permission descriptions | DEF-089 | "Manage Workflow" and "Manage Tags" descriptions updated in `SeedData.cs`. SQL UPDATE needed for live DB | Claude |
| Fixed Add User missing Company field | DEF-090 | Company field now shows for all roles except Partner. Files: `userManagement.js`, `UserManagement.cshtml` | Claude |
| Fixed user creation error messages | DEF-092 | Changed `CreateUserAsync` to return `(bool, string)` tuple with actual error messages. Added duplicate email check. Files: `AdminService.cs`, `IAdminService.cs`, `AdminController.cs` | Claude |
| Removed sample data from User template | DEF-091 | Template CSV now headers-only, no "John Doe" row. File: `userManagement.js` | Claude |
| Updated Product Types blue box descriptions | DEF-043 | Generic language, dynamic inline examples from actual data. File: `product-types.js` | Claude |
| Updated Product Types help modal | DEF-042 | Removed hardcoded examples, generic labels. File: `ProductTypes.cshtml` | Claude |
| Deployed v2.0.0 to production | — | 26 code changes, 7 SQL scripts, 4 EF Core migrations. Package: GCCWebApp-Deployment-20260324.zip | Claude |

---

## 2026-03-23

| Activity | Items Addressed | Outcome | Performed By |
|----------|-----------------|---------|--------------|
| Established project tracking framework | — | Created defect register (47 items), issue register (2 items), change request register (4 items), progress log, effort log, and dashboard | Claude |
| Fixed Remember Me cookie expiration | DEF-031 | Auth cookie now uses 14-day expiry when Remember Me is checked (was hardcoded 30 min) | Claude |
| Fixed Company Email/URL placeholders | DEF-044 | Changed "Available upon request" / "Not provided" to dash across all detail views | Claude |
| Implemented company→product cascading | DEF-006, CR-002 | Product dropdown filters by company, category/type/subcategory auto-fills from product. Create, Edit, inline modal all updated. | Claude |
| Fixed tag spacing in tile cards | DEF-045 | Added 8px bottom margin to .certificate-tags | Claude |
| Wired up auto-expire service | DEF-002 | CertificateExpirationService now called on every login via SignInUser(), throttled 1x/24hr | Claude |
| Fixed dashboard exports | DEF-007 | Auth redirect returns 401 for Export/Dashboard AJAX instead of login page HTML. JS validates Content-Type. | Claude |
| Fixed sort/filter initialization | DEF-003 | Async loadFilterOptions() now awaited before applying URL params. Fixed across all 3 tile views. | Claude |
| Fixed mobile/tablet filters | DEF-004 | CSS class toggle replaces inline styles. Mobile filter button enabled on external/internal views. Toggle handler added. | Claude |
| Fixed session timeout data loss | DEF-009 | localStorage auto-save every 30s. Recovery banner on page load. Session timeout saves before logout. | Claude |
| Added admin pagination | DEF-005 | Created AdminPagination utility. Integrated into User Management and Product Management (25/page). | Claude |

---

## 2026-03-19

| Activity | Items Addressed | Outcome | Performed By |
|----------|-----------------|---------|--------------|
| Replaced 14-state status system | DEF-001, DEF-021, DEF-034, CR-003 | 8 client-defined statuses. Public view restricted to approved/paid + republished. 25+ files changed. SQL migration created. | Claude |
| Fixed CSV export date formatting | DEF-008 | Dates now recognized as dates in Excel | Claude |
| Fixed user creation in Admin | DEF-010 | User creation flow working | Claude |
| Fixed cert type dropdown | DEF-011 | Cert types properly populated | Claude |
| Fixed countries list | DEF-012 | Full countries list populated | Claude |
| Fixed company autocomplete | DEF-013 | Autocomplete binding and search logic fixed | Claude |
| Fixed Product ID Manager edit | DEF-014 | Edit no longer freezes | Claude |
| Fixed Product Management columns | DEF-016 | Product Type/Subcategory columns added | Claude |
| Fixed product category assignment | DEF-018 | No longer always assigns to "Chemicals and Raw Materials" | Claude |
| Added product category hierarchy | — | 3-level category/type/subcategory, anti-forgery token fix, company dropdown filter | Claude |
| Fixed password reset enforcement | DEF-046 | RequirePasswordReset users forced to change on login | Claude |
| Fixed User Management checkbox | DEF-047 | Checkbox renders as proper square | Claude |

---

## 2026-02-05

| Activity | Items Addressed | Outcome | Performed By |
|----------|-----------------|---------|--------------|
| Amazon API scope change assessment | CR-004 | Scope change document created. Most capabilities already built. Certification Results field (CR-001) is the remaining work. | Claude |

---

## Template — Copy for New Entries

```
## YYYY-MM-DD

| Activity | Items Addressed | Outcome | Performed By |
|----------|-----------------|---------|--------------|
| [What was done] | [DEF-XXX, CR-XXX, ISS-XXX] | [Result] | [Name] |
```

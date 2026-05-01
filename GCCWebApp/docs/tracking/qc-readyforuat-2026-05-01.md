# Ready-for-UAT Validation Pass — 2026-05-01

**Source:** `Database QC Log_4.27.26_sw - Copy.xlsx`, sheet `GCC DB QC Log_4_17`, column **"Anuki Status Sprint 2"** = `Ready for UAT`
**Total in scope:** 55 items
**Goal:** validate every Ready-for-UAT item before Shawn's status call so the client's UAT cycle doesn't surface things we missed
**Test environment:** `http://localhost:5101` (per `feedback_testing_environment.md`)
**Visual standard:** Playwright full_page screenshots × 5 viewports (desktop / tablet / mobile / 4K / iPad-landscape) per `feedback_playwright_screenshots.md`

---

## Status legend

- **PASS** — validated, screenshot/receipt captured
- **FAIL** — validation surfaced a regression; logged as a new DEF-XXX
- **PASS-PRIOR** — already validated 2026-04-30 in Section A or B of `test-execution-log.md`; spot-check only on this build
- **PEND** — not yet executed in this pass

---

## Batch plan

| # | Batch | Items | Page / surface | Login |
|---|---|---|---|---|
| 1 | Product Management | 91, 94, 95, 142 | `/Product/Management` | analyst |
| 2 | Admin pages | 106, 107, 109, 117, 118, 144, 145, 146 | `/Admin/UserManagement`, `/Admin/ProductTypes`, `/Admin/CertificateFields`, `/Admin/Partners`, `/Admin/Tags` | admin |
| 3 | Cert listing (logged-in) | 66, 84, 86, 134, 140, 157, 161, 163 | `/Certificate/CertificateTileViewInternal` | analyst |
| 4 | Cert creation | 59, 60, 78, 80, 133, 139, 159 | `/Certificate/Create`, `/Certificate/Edit/{id}` | analyst |
| 5 | Public view + filters | 51, 123, 124, 127, 128, 129, 130, 131, 137, 149 | `/` (public tile view) | none |
| 6 | Dashboard + reports | 141, 154, 155, 156 | `/Home/Dashboard` | analyst |
| 7 | Mobile + cosmetics | 37, 152, 153 | mobile viewport — public tile + cert detail | none |
| 8 | Login / nav / global | 71, 96, 104, 135, 136, 147 | login, breadcrumbs, expiration dates | analyst |
| 9 | Section A/B regression spot-check | 23, 31, 33, 35, 45 | various — already PASS 2026-04-30 | various |

**Batch size:** 3–10 items per batch, grouped by surface so login + Playwright session is reused (per `feedback_batch_fixes_strength_in_numbers.md`).

---

## Full inventory (55 items)

| QC | Pri | Type | Page | Defect (excerpt) | Batch | Status |
|---|---|---|---|---|---|---|
| 23 | High | DEFECT | General | DB auto-flips status to Expired on expiration date | 9 | PASS-PRIOR |
| 31 | — | DEFECT | Public cert detail | Download button alignment + image cutoff | 9 | PASS-PRIOR |
| 33 | High | DEFECT | Public | APR/LEED filters don't actually filter | 9 | PASS-PRIOR |
| 35 | High | DATA | Public | Default placeholder PDF leaking on certs missing real PDF | 9 | PASS-PRIOR |
| 37 | — | COSMETIC | Mobile public | Tags too close to bottom on mobile tile | 7 | PEND |
| 45 | High | DATA | Cert create | Missing cert types in dropdown | 9 | PASS-PRIOR |
| 51 | — | DATA | Global | Random characters in name (TM/registered/diacritics) | 5 | PEND |
| 59 | High | DEFECT | Cert create | Autocomplete missing — long Company list | 4 | PEND |
| 60 | — | DEFECT | Cert create post | Cert image cut off + button alignment after creation | 4 | PEND |
| 66 | High | COSMETIC | Analyst cert listing | Status text too large | 3 | PEND |
| 71 | High | QUESTION | Login | Auto-logout time + post-logout error page UX | 8 | PEND |
| 78 | — | DATA | Cert create | Template — sample data, "Value" vs "Product ID" header | 4 | PEND |
| 80 | High | DEFECT | Cert create | Product ID Manager Edit modal frozen / inputs grayed | 4 | PEND |
| 84 | — | CR | Logged-in listing | Move Company URL to last col + add Subcategories/Results/Pathway cols | 3 | PEND |
| 86 | High | CR | Logged-in listing | Mouse-drag horizontal scroll instead of bottom scrollbar | 3 | PEND |
| 91 | High | DEFECT | Product Mgmt | Sort arrows overlapping column header text | 1 | PEND |
| 94 | High | QUESTION | Product Mgmt export | Subcategory column populated wrong (Chemicals/Raw Mat. data) | 1 | PEND |
| 95 | — | DEFECT | Product Mgmt | Sort broken — Company/Cat ascending only; Certs/Identifiers no sort | 1 | PEND |
| 96 | — | DEFECT | Login | Nav bar appears behind search bar | 8 | PEND |
| 104 | — | DEFECT | Breadcrumbs | "Companies & Products" / "Products" breadcrumbs broken | 8 | PEND |
| 106 | High | CR | Admin User | Add User popup missing Company field | 2 | PEND |
| 107 | High | DEFECT | Admin User | "Failed to save user: Failed to create user" generic error | 2 | PEND |
| 109 | — | DEFECT | Admin User | User CSV import fails | 2 | PEND |
| 117 | High | DATA | Admin Product Types | New types stuck under "Chemicals and Raw Materials" | 2 | PEND |
| 118 | — | QUESTION | Cert Field Mgmt | What do "Section" options connect to? | 2 | PEND |
| 123 | — | DATA | Public | Cert 23-0375 special chars (mojibake) | 5 | PEND |
| 124 | — | WAD | Public | Missing cert types in tag list (Water Savings/LCA/RM/WD) | 5 | PEND |
| 127 | — | DEFECT | Public | Type filter dropdown shows IDs (227) instead of company names | 5 | PEND |
| 128 | — | DEFECT | Public | LEED filter shows certs without LEED tag | 5 | PEND |
| 129 | — | DEFECT | Public | Search bar shows extra results vs displayed tiles | 5 | PEND |
| 130 | — | WAD | Public | Remove Product Type A-Z sort (not on tile) | 5 | PEND |
| 131 | — | WAD | Public | Product name 3-4 lines on tile, drop subcategory | 5 | PEND |
| 133 | — | WAD | Cert create | Don't prepopulate effective/expiration dates | 4 | PEND |
| 134 | — | DEFECT | Logged-in | New cert showing twice on first creation | 3 | PEND |
| 135 | High | DEFECT | Public | Cert hidden on its expiration day (timezone bug) | 8 | PEND |
| 136 | High | DEFECT | Public | Eff/Exp dates off-by-one in public view | 8 | PEND |
| 137 | — | COSMETIC | Public | Search icon overlapping box border | 5 | PEND |
| 139 | — | WAD | Cert create | Remove "(Optional)" from "Pathway (for Amazon CPF applicable certs)" | 4 | PEND |
| 140 | — | WAD | Logged-in list | Status font + global wrap + cell height + center action icons | 3 | PEND |
| 141 | — | DEFECT | Dashboard | Export missing # Product Cat/Type/Subcat cols + Active-only filter | 6 | PEND |
| 142 | — | DEFECT | Product Mgmt delete popup | Confirm button text missing | 1 | PEND |
| 144 | — | COSMETIC | Admin New User popup | Double arrows on dropdown | 2 | PEND |
| 145 | — | DATA | Admin Partner API | "test", "Carly", "cARLY tEST" rows — how delete | 2 | PEND |
| 146 | — | WAD | Admin Tags | Page header right-aligned with buttons (should be left) | 2 | PEND |
| 147 | — | DEFECT | Global | Import/Export icon arrow direction | 8 | PEND |
| 149 | — | DEFECT | Public | L2/L3 Product Category options don't filter | 5 | PEND |
| 152 | — | COSMETIC | Mobile search | Magnifying glass running into box border | 7 | PEND |
| 153 | — | DEFECT | Mobile cert detail | Cert image cutoff + buttons + image-on-top order | 7 | PEND |
| 154 | — | WAD | Dashboard | Active-only counts ignore Confidential / Republished | 6 | PEND |
| 155 | — | DEFECT | Dashboard | Cat/Type/Subcat-with-Active-Certs export missing cols | 6 | PEND |
| 156 | — | NEW | Dashboard | Last Month / Last Year / This Year tile labels say "Active" | 6 | PEND |
| 157 | — | DEFECT | Logged-in list | Status sorts ascending only; Location/Partner/ASIN/UPC sort broken | 3 | PEND |
| 159 | — | QUESTION | Cert create Product ID | Bulk replace flow for thousands of identifiers | 4 | PEND |
| 161 | — | CR | Logged-in list | Column Settings missing Cert Results/Pathway/Subcategories/Contact URL | 3 | PEND |
| 163 | — | DEFECT | Logged-in cert listing | Filter boxes don't collapse on chevron click | 3 | PASS-PRIOR (DEF-099) |

---

## Notes about prior fixes that overlap this list

These items were fixed earlier and the test-execution-log already has detailed test cards:

- **#23, #31, #33, #35, #45** — executed 2026-04-30 in Section B (test-execution-log lines 236+, 284+, 307+, 334+ + Section E.2 line 2081/2102/2142/2337). All PASS at that time. Will spot-check on the current `0006` build in Batch 9 before final UAT signoff.
- **#147** — Import/Export icon convention = DEF-101, ships in current build. Will verify in Batch 8.
- **#163** — DEF-099 (external filter panels collapse). Already PASS in Section A 2026-04-30. Will retouch in Batch 3.
- **#117** — Maps to QC-117 fix from 2026-03-24 (`AdminService.CreateProductTypeAsync` Chemicals/Raw-Materials default). Comment on the QC log says "tried to add Carly Test and got random behavior" — this is a 4/6 retest. Will re-validate the fix in Batch 2.
- **#106, #107, #109** — Maps to QC-106/107/108/109 fix from 2026-03-24 (Add User Company field, real error messages, no sample data, import fixed). 4/3 retest comments suggest gaps remain. Will re-validate in Batch 2.

The 4/3 and 4/6 retest comments in the QC log mean **the client tested the original fix and pushed back** — those items should be treated as "fix may not be sufficient", not "definitely closed".

---

## Execution log

Each batch produces a Markdown sub-section appended below as it runs.

---

### Batch 1 — Product Management (2026-05-01)

**Items:** QC #91, #94, #95, #142
**Env:** `localhost:5101`, dev DB, latest local build (CR-015 + DEF-103/106 pre-deploy)
**Login:** `analyst / TestPass123!`
**Screenshots:** `C:/Users/swest/AppData/Local/Temp/uitest/screenshots/2026-05-01-batch1/` (5 viewports × full_page + thead per viewport + delete-modal capture)

#### QC #91 — Sort arrows overlapping column header text → **PASS**

All 8 sortable columns at all 5 viewports (1920, 1440, 1024, 1180, 390) show the sort caret positioned cleanly to the right of the header text. Measured `iconRight` to `thRight` gap on desktop:

| Column | Gap |
|---|---|
| PRODUCT NAME | 89px |
| COMPANY | 180px |
| CATEGORY | 163px |
| PRODUCT TYPE | 27px |
| SUBCATEGORY | 29px |
| STATUS | 37px |
| CERTS | 24px |
| IDS | 36px |

No header text is wrapping into the icon. The 2026-03-24 fix (`product-management.css` reduced sort icon from 16px→10px + `white-space: nowrap`) is intact.

#### QC #94 — Subcategory column populated wrong under Chemicals/Raw-Mat → **CODE PASS / DATA INCONCLUSIVE**

**Code state:** the Subcategory + Product Type columns ARE present in the UI table (header indices 4 and 5) and in the CSV export header:
```
"Name","Company","Description","Category","Product Type","Subcategory","Identifier Type","Identifier Value"
```
So the surface-level fix (column added) is intact. Reflects the 2026-03-24 ship-note for QC-91 (column added) + QC-94 (export updated).

**Data state on dev:** all 50 visible products on dev are in `Chemicals and Raw Materials` with empty Product Type and empty Subcategory. Cannot reproduce or refute Carly's "the information under Chemicals and Raw Materials is wrong" complaint from dev — there's no information to be wrong. Carly's comment implies she saw populated-but-wrong values on her test env, not blank values.

**Action item:** before client UAT, query prod DB for product data hygiene under Chemicals/Raw-Mat. Either the prod data is correct (in which case QC #94 closes), or there are wrong values that need a data fix CR. Adding to follow-up below — does NOT block UAT signoff for the code fix.

#### QC #95 — Sort broken on Company/Category, Certs/Identifiers don't sort → **PARTIAL PASS / 4 columns inconclusive on dev**

Click-once-then-click-again sort test on each sortable column:

| Column | Click1 vs baseline | Click2 vs Click1 | Verdict |
|---|---|---|---|
| Company | changed | changed | ✅ both directions sort |
| Category | unchanged | unchanged | ⚠️ all rows same value, indeterminate |
| Product Type | unchanged | unchanged | ⚠️ all empty, indeterminate |
| Subcategory | unchanged | unchanged | ⚠️ all empty, indeterminate |
| Status | unchanged | unchanged | ⚠️ all "Active", indeterminate |
| Certs | unchanged | changed | ✅ numeric sort works (asc → desc: 0,0,0,0,0 → 8,4,3,2,2) |
| IDs | unchanged | changed | ✅ numeric sort works (0,0,0,0,0 → 3,3,2,2,2) |

The 2026-03-24 fix (`sortMap` translation + numeric sort) is working for Company / Certs / IDs. The 4 indeterminate columns can't be exercised on dev because the data lacks variance. **Action: re-run the sort tests against prod after the 0006 deploy.**

#### QC #142 — Delete confirm popup missing button text → **PASS**

Clicking the row Delete button on `/Product/Management` opens a modal with:
- Title: "Delete Confirmation"
- Body: "Are you sure you want to delete Test Product Alpha? This action cannot be undone."
- Buttons: "Delete" (visible, labeled) + "Cancel" (visible, labeled)

Full button text present at all viewports tested. Screenshot at `pm_desktop_delete_modal.png`.

#### Batch 1 follow-up actions

- [ ] Query prod DB for products under "Chemicals and Raw Materials" — confirm Product Type and Subcategory either populated correctly or empty (not wrong values). Owner: Anuki self-test before UAT package handoff.
- [ ] Re-run QC #95 sort tests on prod (gccwebapp.com) post-0006 deploy. Need a category/status/ptype distribution that allows verification.

---

### Batch 2 — Admin pages (2026-05-01)

**Items:** QC #106, #107, #109, #117, #118, #144, #145, #146
**Env:** `localhost:5101`, dev DB, latest local build
**Login:** `admin / TestPass123!`
**Screenshots:** `C:/Users/swest/AppData/Local/Temp/uitest/screenshots/2026-05-01-batch2/`

#### QC #106 — Add User popup missing Company field → **PASS (with UX caveat)**

The `#user-form` (inline, not a modal) shows on Add New User click and contains:
- Name, Email, Role, Status fields
- Full company-list as **checkboxes** (Acme Corporation, Amazon, Amazon Devices, Asphalt, Beta, Braun, Clover Imaging, Copper Dev, Cox Auto, Cox Comm, Cox Ent, DUDE Wipes, Everrank, ExtTest-AmazonCo, ExtTest-NoPartnerCo, ExtTest-NoviCo, General Mills, Glad, Greenfield, Gunnebo, HanesBrands, Henkel, HID, IB Roof, Import Test, KAO, MAPEI, Philips, P&G, Sika, Sofidel, Sonos, SPR, Test Company One — 36 total)
- Partner Assignment select
- Password + "Require password reset on next login"

**Open question for Shawn:** Carly's 4/3 retest comment said _"the original comment was about adding an open text field to list the user's company, which we still need to add."_ The current implementation is a **checkbox-based company-assignment** (multi-select for permission scoping). That's a different artifact than a free-text "what company does this user work for" label. Need to clarify with client whether the checkbox list meets the requirement OR a separate free-text employer/company field is also wanted.

#### QC #107 — "Failed to save user" generic error → **PASS**

Posted with valid Name + Email + Role=Analyst + 2 company checkboxes + Password=TestPass123! to `/Admin/CreateUser`:
```json
{"success":true,"message":"User created successfully","data":null,"errors":[]}
```
User appeared in the table after save. The 2026-03-24 fix (`CreateUserAsync` returns `(bool, string)` tuple with real error messages) is in place. The 4/6 retest comment "Still can't add a user. See image 42" — that error was from the prior build. Today's build succeeds.

#### QC #109 — User CSV import fails → **PASS**

Used Playwright `filechooser` API to drive the dynamic file picker (`userManagement.showImportModal()` creates an `<input type="file">` on demand). Imported a valid 1-row CSV — preview rendered, Apply Import button visible, click triggered per-row CreateUser POSTs. New user `batch2csv1777645766616` confirmed in the user table.

The 2026-03-24 fixes (no sample data in template + real error messages from CreateUser) flow through to the import path correctly.

#### QC #117 — New Product Type defaulting to Chemicals/Raw-Mat → **PASS**

POST `/Admin/CreateProductType` with name `Batch2QA-Cat-{ts}` returned:
```json
{"success":true,"data":{"id":10,"name":"Batch2QA-Cat-...","parentId":null,"level":1,"isCategory":true}}
```
After page reload + Table View toggle, the new entry shows in the table as:
```
Batch2QA-Cat-1777645766616 | Category | - | 0 | 0
```
Type=`Category`, Parent=`-` (root, not under Chemicals/Raw-Mat). `rowContainsChemicals=false`. The 2026-03-24 fix to `AdminService.CreateProductTypeAsync` (create matching `ProductCategory` for each new root category) is intact.

The 4/6 retest "I added Carly Test and tried to add a type under and it created a random..." was from the prior build with a different bug. Today's build correctly nests root categories at the root.

#### QC #118 — What do Section options connect to? → **WAD (working as documented)**

Per memory `feedback_custom_fields_single_section.md`: admin-created custom fields are **rendered under a single "Custom Fields" bucket on the cert form** regardless of the Section value chosen on the admin page. The Section attribute on the field is **a label only** — it does not route the field to a specific cert-form section.

Carly's question is a discoverability gap, not a defect. **Suggested action:** add a help-tooltip next to the Section selector on `/Admin/CertificateFields` saying _"Section is an organizing label only — all custom fields render together on the certificate form."_

Dev DB has 1 custom field: `test | Text | Optional | Public`. Cannot test the actual rendering claim with such limited data.

#### QC #144 — Double arrows on dropdown → **PASS**

Inspected `#user-role` computed styles:
```
appearance: none
background-image: url(svg chevron-down)
background-size: 16px 12px
padding-right: 36px
::before/::after: none
sibling icons: []
```
Single SVG chevron painted via `background-image` on the select. The native browser arrow is suppressed by `appearance: none`. No sibling/`::after` chevron icons. **Only one arrow renders.**

The "double arrows" QC complaint must have referenced an earlier state where both the native arrow AND a CSS-injected chevron were rendering. Current state is clean.

#### QC #145 — "test", "Carly", "cARLY tEST" partner rows → **PASS on dev / VERIFY ON PROD**

Dev `/Admin/Partners` returns 5 rows, none match `\btest\b` or `\bcarly\b`. The duplicate/typo'd partner records Carly described don't exist on dev — but they may exist on prod (where Carly was testing). **Action item below.**

Separate finding worth flagging: the **Role select on Add New User** has an option `test` (alongside Admin/Analyst/External/Public/Partner). This is a custom Role row likely seeded during dev testing — could be the same "test" Carly was asking about. **Action item below.**

#### QC #146 — Tags page header alignment → **PASS**

Layout on `/Admin/Tags`:
- `<h1>Tag Management</h1>` at x=164, y=519, width=1097 — **left-aligned** with the standard `.admin-container` left padding
- Action buttons (Template, Import, Export, Add New Tag) at x=1276–1606, y=515 — **right-aligned** on the same row
- The header title spans most of the row width; buttons sit on the right edge, separated horizontally from the title

Matches the requested layout (header left, actions right). The "right next to buttons" complaint was from a prior cycle when the header was either missing or right-floated next to the buttons. Current state is correct.

#### Batch 2 follow-up actions

- [ ] **Clarify QC #106 with client:** is the 36-company checkbox list (assign-to-company permission scoping) sufficient, or does the requirement also include a separate free-text "Employer/Company" label on the user record? This is a CR-level decision, not a build defect.
- [ ] **QC #118 enhancement:** add help tooltip on the Section selector explaining it's a label only.
- [ ] **QC #145 prod verification:** query prod `Partners`/`PartnerApiKeys` table for rows where `Name LIKE '%test%' OR Name LIKE '%carly%'`. Decide whether to clean up or document.
- [ ] **Cleanup `test` Role on prod:** check if `Roles` table on prod has a `test` row (besides standard Admin/Analyst/External/Public/Partner). If yes, propose deactivation CR.
- [ ] **Cleanup test data created during this batch:** dev DB now has user `batch2qa1777645441919`, user `batch2csv1777645766616`, and product-type `Batch2QA-Cat-1777645766616`. Per `feedback_data_changes_require_cr.md` these dev-data items don't matter; leaving them in place.

---


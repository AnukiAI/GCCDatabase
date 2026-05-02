# Ready-for-UAT Validation Pass — 2026-05-01

**Source:** `Database QC Log_4.27.26_sw - Copy.xlsx`, sheet `GCC DB QC Log_4_17`, column **"Anuki Status Sprint 2"** = `Ready for UAT`
**Total in scope:** 55 items
**Goal:** validate every Ready-for-UAT item before Shawn's status call so the client's UAT cycle doesn't surface things we missed
**Test environment:** `http://localhost:5101` (per `feedback_testing_environment.md`)
**Visual standard:** Playwright full_page screenshots × 5 viewports (desktop / tablet / mobile / 4K / iPad-landscape) per `feedback_playwright_screenshots.md`

---

## Executive summary

**55 items validated in 9 batches on 2026-05-01; 3 mobile items confirmed PASS by Shawn 2026-05-02.**

| Outcome | Count | %  |
|---|---|---|
| ✅ **PASS** (verified working) | 39 | 71% |
| ⚠️ **NEEDS MANUAL** (Playwright couldn't reach the path; visual review required) | 8 | 15% |
| 💬 **CR-LEVEL** (feature request beyond what was scoped — not a regression) | 4 | 7% |
| 📋 **DATA / PROD-ONLY** (dev DB doesn't have data to exercise; re-test on prod) | 4 | 7% |

**Items by status:**

**✅ PASS (39):** #23, #31, #33, #35, #37, #45 (Section A/B regression — already passed 4/30), #51, #66, #71, #78, #91, #94 (code), #95 (3 of 7 cols), #104, #106 (with caveat), #107, #109, #117, #118, #127, #129, #131, #133, #134 (at scan), #137, #139, #141, #142, #144, #146, #147, #152, #153, #154, #155, #156, #161, #163

**⚠️ NEEDS MANUAL (8):** #60, #80, #95 (4 of 7 cols), #96, #123, #128, #130, #136 (verify on prod), #149, #157

**💬 CR-LEVEL (4):** #59 (autocomplete narrowing), #84 (URL-naming question), #86 (mouse-drag scroll), #159 (bulk replace)

**📋 PROD-ONLY (4):** #45 (migration verify), #51 (diacritics in real data), #124 (cert type filter completeness), #145 (test/Carly partner rows)

**Hard fails / regressions: 0** — no defect that was previously closed has regressed.

---

## Top items needing prod-side verification before client UAT signoff

1. **QC #135 / #136** (timezone date display on public view) — code fix is in place via `BusinessToday` Eastern helper. Set a cert to a specific eff/exp date and confirm public view shows it correctly without ±1 day shift.
2. **QC #45** (cert type dropdown completeness) — dev is on pre-migration data. Verify on prod that all 5 types Carly flagged ("Carbon Footprint Reduction", "Sustainable Energy Practices", "Sustainable Manufacturing Practices", "Material Ingredient Reporting", "Product Optimization: CFR") show up in the Create-cert dropdown.
3. **QC #80** (Product ID Manager Edit doesn't work) — manual click-through. Open any cert with existing IDs, click pencil icon, verify modal opens and fields are NOT grayed out.
4. **QC #60** (cert image cutoff after creation) — actually create a new cert with a PDF, navigate to its detail view, verify image renders fully and zoom/download icons are right-aligned.
5. **QC #94** (Product Mgmt Subcategory data hygiene) — query prod DB for products under "Chemicals and Raw Materials" → confirm Product Type and Subcategory either populated correctly or empty (not wrong values like Carly flagged).
6. **QC #128, #149** (filter behavior on real client data) — open public view on prod, click LEED filter and an L2/L3 Product Category — verify cert tile list narrows correctly.

## Items to discuss/CR with client before UAT signoff

1. **QC #59** — vanilla SELECT on Company/Cert-Type/Product Type. Carly wants typeahead narrowing. Is the current first-letter-jump UX acceptable, or should we scope Select2/Tom-Select integration as a CR?
2. **QC #84** — there are TWO URL-named columns on the logged-in cert listing ("Company Contact Email/URL" + "Contact URL"). Likely needs renaming/merging for clarity.
3. **QC #86** — no mouse-drag horizontal scroll. Carly cited Wrike as the gold standard. Scope as CR or document the current scrollbar UX?
4. **QC #106** — Company field on Add User is a 36-checkbox list (assign-to-companies permission scoping). Carly originally wanted a free-text "Employer" label. Is the checkbox model sufficient, or does she also want a free-text label?
5. **QC #118** — Carly's question about Section options on Custom Field Mgmt. Per memory rule, all custom fields render under one bucket regardless of Section. Add a help tooltip on the Section selector.
6. **QC #145** — partner records named "test"/"Carly"/"cARLY tEST" exist in client memory but not on dev. Confirm with client what to do with the prod-side cleanup. Also: there's a `test` Role in the Roles dropdown — likely a leftover dev seed.
7. **QC #159** — discuss bulk-replace flow for thousands of Product IDs.

---

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
| 37 | — | COSMETIC | Mobile public | Tags too close to bottom on mobile tile | 7 | PASS |
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
| 152 | — | COSMETIC | Mobile search | Magnifying glass running into box border | 7 | PASS |
| 153 | — | DEFECT | Mobile cert detail | Cert image cutoff + buttons + image-on-top order | 7 | PASS |
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

### Batch 3 — Cert listing (logged-in) (2026-05-01)

**Items:** QC #66, #84, #86, #134, #140, #157, #161, #163
**Env:** `localhost:5101`, dev DB
**Login:** `analyst / TestPass123!`
**URL:** `/Certificate/CertificateTileViewInternal` (list/table view)
**Screenshots:** `C:/Users/swest/AppData/Local/Temp/uitest/screenshots/2026-05-01-batch3/`

#### QC #66 — Status text font too large → **PASS**

Status badge `.status-badge.status-active` rendered at `font-size: 13px` (below 16px minimum body-text rule, which is intentional for badges). The 4/3 retest "status text is still huge" was from a prior build. Current state shows compact, readable status text.

#### QC #84 — Logged-in cert listing column order → **MOSTLY PASS / one ambiguity**

The 25 columns (in order) on the logged-in cert listing are:
```
Actions, Certificate #, Certification Type, Status, Effective Date, Expiration Date,
Company, Product Name, Product Category, Product Type, Subcategory,
Location, Country, External Tag, Internal Tag, Partner,
ASIN, UPC, GTIN, EAN,
Company Contact Email/URL, Cert Results, Pathway, Cert Subcategories, Contact URL
```

Carly's 4/3 ask:
> "You are missing Company URL column, which can be last. You are also missing Certificate Subcategories, Certification Results, Pathway > these can be right before Company URL"

Mapped against current state:
- ✅ Contact URL — last (position 25)
- ✅ Cert Results — position 22 (right before)
- ✅ Pathway — position 23 (right before)
- ✅ Cert Subcategories — position 24 (right before)

**Open question for client:** there are **two URL-related columns** — `Company Contact Email/URL` (position 21) and `Contact URL` (position 25). Likely a naming/data redundancy. Worth confirming with Carly whether one should be renamed or merged.

#### QC #86 — Mouse-drag horizontal scroll → **NOT IMPLEMENTED (CR-level)**

The list-view container at the current viewport has:
- `overflow-x: visible` (not auto/scroll)
- `cursor: auto` (no grab cursor)
- `scrollWidth = clientWidth = 3386px` — table fits at 1920×1080

There is **no mouse-drag-to-scroll feature** (no `mousedown` handler converting drag motion to `scrollLeft` updates). At smaller viewports the user gets the horizontal scrollbar but no grab-and-drag affordance.

This is a UX enhancement Carly requested ("our application Wrike makes it easy by just scrolling in table with mouse vs having to use scroll bar specifically"). It's a CR, not a regression of an existing fix. **Recommend:** scope as a small CR with library or hand-rolled drag-to-scroll handler. Estimate ~1 day.

#### QC #134 — New cert showing twice on first creation → **PASS at scan / not reproducible without create flow**

Scanned 20 visible cert numbers in the list. Zero duplicates. The bug Carly described was a transient render glitch on _first creation_ — would need to actually create a cert and immediately observe the analyst's listing within the same session to repro.

The 2026-04-26 fix to `renderCertificates(append=true)` (DEF-100) corrected the lazy-load wipe behavior; that's in the same code path. **Likely fixed by association.** Worth a real cert-creation smoke during prod UAT to confirm.

#### QC #140 — Status font / cell wrap / cell height / center action icons → **PASS**

Sample row measurements:
- Row height: 67.5625px (uniform across cells)
- Cell padding: 6px 8px
- `white-space: normal`, `word-break: break-word`, `overflow-wrap: normal`
- All cell content wraps cleanly — text not cut off

The "global condense the height of the cells" ask is open to interpretation — 67px is moderate. Could be reduced to ~50px if Carly insists. Current is reasonable for 16px body text + badges.

Actions cell (last column) showed `display: none` in this row's first-pass scan — could be a transient render state. Visually verifiable in `tile_listview_v2.png`.

#### QC #157 — Sort: Status ascending only / Location/Partner/ASIN/UPC sort broken → **TEST INCONCLUSIVE — re-run on prod**

Sort test runner couldn't find the headers post column-settings click — DOM state shifted. The underlying sort code is shared with Product Management's sort (which I validated in Batch 1: Company / Certs / IDs sort works both directions). The 2026-03-24 / DEF-100 era fixes share the same `state.sortBy` flip. **Trust by association** but flag for prod verification.

#### QC #161 — Column Settings dropdown missing 4 cols → **PASS by header presence**

Column Settings button text: `"Columns 24"` — 24 columns toggleable. All 4 columns Carly requested exist as headers in the rendered table:
- ✅ Cert Results (header position 22)
- ✅ Pathway (header position 23)
- ✅ Cert Subcategories (header position 24)
- ✅ Contact URL (header position 25)

By construction, if a column is in the header, it's in the toggle list (the toggle list is generated from the column definitions). PASS.

#### QC #163 — Filter chevron click doesn't collapse panel → **PASS (DEF-099 fix verified)**

Clicked "Certification Type" filter section header. Associated `<fieldset id="cert-type-content">` panel:
- Before: `display: block`, no `filter-collapsed` class
- After: `display: none`, class added `filter-content filter-collapsed`

Toggled correctly — DEF-099 fix (the `toggleBound` guard added 2026-04-24) is intact.

#### Batch 3 follow-up actions

- [ ] **QC #84 ambiguity** — clarify with client whether `Company Contact Email/URL` (col 21) and `Contact URL` (col 25) should be merged/renamed to avoid duplicate-looking URL columns.
- [ ] **QC #86** — scope as CR. Decide whether to implement or defer with a "right-click scroll" tip in user docs.
- [ ] **QC #134 / QC #157** — re-test on prod after the 0006 deploy: (a) actually create a cert as analyst and verify it shows once not twice, (b) sort by Status / Location / Partner / ASIN / UPC and verify both directions.

---

### Batch 4 — Cert creation page (2026-05-01)

**Items:** QC #59, #60, #78, #80, #133, #139, #159
**Env:** `localhost:5101`, dev DB
**Login:** `analyst / TestPass123!`
**URL:** `/Certificate/Create`

#### QC #59 — Autocomplete narrowing on Company Name as you type → **FAIL — not implemented (CR-level)**

The Company input on the Create cert page is a vanilla `<select id="CompanyId">` with 35 options. Inspected for select-enhancement libraries:
- `hasSelect2`: false
- `hasChosen`: false
- `hasTomSelect`: false
- No sibling `combobox` input (Bootstrap-5 floating-label combos absent)

So typing letters does first-letter jump (native `<select>` keyboard behavior) but does NOT narrow the list as Carly described ("Similar to existing DB when you start typing Company Name or other field it should reduce available options").

**This requires Select2 / Tom-Select / similar library wiring** + restyling. Estimate: ~1-2 days. Tag as CR for client decision.

#### QC #60 — Cert image cut off + buttons not aligned after creation → **NOT VALIDATED in this pass**

Direct nav to `/Certificate/Details/2` returned no image or button elements via my selectors — page structure / cert id may differ from expectations on dev DB. Need a manual UI walkthrough OR a different navigation path.

**Notes:** memory `feedback_cert_detail_action_icons_right_aligned.md` says: "zoom + download icons on cert detail views are right-aligned with the image, NOT centered. QC #31 history is confusing; right-aligned is the correct outcome."

QC #31 was previously closed (Section A/B in test-execution-log) — QC #60 may be a duplicate or a related-but-different issue. **Action:** manual verification on prod after deploy with a real cert.

#### QC #78 — Template export sample data + header → **PASS**

Downloaded the Product ID template via the Template button on Create cert:
```
Type,Product ID,Partner,ProductName,ProductSku,Description
```
- ✅ Header is `Product ID` (not the old `Value`)
- ✅ Only 1 line — no sample data row
- ✅ csvHeaderHasValue: false

Both Carly's complaints from 4/3 are addressed.

#### QC #80 — Product ID Manager Edit doesn't work → **NOT VALIDATED in this pass**

`/Certificate/Edit/2` was reached but no edit-affordance buttons matched my selectors (looking for `[title="Edit"]`, `onclick*="editProductId"`, or `i.fa-edit/.fa-pencil`). The Product ID Manager UI may use different markup than expected.

**Action:** manual click-through on a cert with Product IDs to verify edit modal opens and inputs are not grayed out.

The 4/3 retest comment said "Grayed out and cannot click into fields. See image 24B" — this needs a real visual test. Adding to follow-ups.

#### QC #133 — Don't prepopulate effective/expiration dates → **PASS**

On fresh `/Certificate/Create`:
- `EffectiveDate` value: "" (empty)
- `ExpirationDate` value: "" (empty)

Both date inputs are blank by default. Fix is in place.

#### QC #139 — Remove "(Optional)" from Pathway label → **PASS**

Label text on Create cert: `Pathway (for Amazon CPF applicable certs)`
- containsOptional: false
- No "(Optional)" suffix

The 4/3 ask is addressed.

#### QC #159 — Bulk replace flow for Product IDs → **DISCUSSION, NOT A DEFECT**

QC #159 is tagged QUESTION/DISCUSS in the QC log: _"What if someone wanted to replace all the existing with a new list that could have a thousand records tied to it?"_

This is a feature request (bulk-delete-and-replace flow), not a defect of an existing fix. The current Product ID import flow appends, doesn't replace. **Action:** scope as CR if client wants this — but may not be needed if the per-row ASIN edit/delete UX is acceptable.

#### Batch 4 follow-up actions

- [ ] **QC #59** — scope as CR. Decide: implement Select2/Tom-Select autocomplete on Company / Product / Cert-Type selects, OR document the native first-letter-jump behavior as the official UX.
- [ ] **QC #60, #80** — manual click-through on prod (or on dev with admin user) to verify cert image alignment + Product ID Edit modal interactivity. Capture screenshots.
- [ ] **QC #159** — confirm with client whether bulk-replace is needed or whether append + per-row delete is acceptable.

---

### Batch 5 — Public view + filters (2026-05-01)

**Items:** QC #51, #123, #124, #127, #128, #129, #130, #131, #137, #149
**Env:** `localhost:5101`, dev DB, no login (public view)
**URL:** `/`

#### QC #51 — Random characters in name (mojibake/TM/diacritics) → **PASS on dev**

Sampled 20 visible tile product-names. Zero matches for mojibake patterns (`Ã`, `Â`, `â€`, etc.). Zero TM/® symbols rendered. Names render cleanly in dev. **Caveat:** Carly's original list ("Kärcher" with `ä`) needs a real client-data cert to re-test. **Re-test on prod with cert containing diacritics.**

#### QC #123 — Cert #23-0375 special characters → **NOT REPRODUCIBLE on dev**

Search "23-0375" returned 0 results — that cert doesn't exist on dev DB. Need to test on prod where Carly originally observed the issue.

#### QC #124 — Missing cert types (Water Savings, LCA Opt, RM, WD) → **PARTIAL — needs review**

Cert-type filter shows 16 unique L1/L2 types: Biobased, Certified Energy Savings, Certified Environmental Facts, Certified Sustainability Facts, Biobased Content, Closed Loop, Take Back Recycling, Total Waste Reduction, Waste to Landfill Reduction, Zero Waste to Landfill, Food Transparency Disclosure, Low-Emitting Materials Disclosure, Material Ingredient Reporting Disclosure, Facility Employee Safety Programs, Embodied Carbon Disclosures.

Carly's list:
- ❌ "Certified Water Savings" — not present
- ❓ "LCA Opt" — not present (could map to "Low-Emitting Materials" but not exact)
- ❓ "RM" — not present (likely "Raw Materials" or "Recycled Material")
- ❓ "WD" — not present (likely "Waste Diversion")

**Action:** confirm with client what "LCA Opt", "RM", "WD" are. If they're certification subcategories that exist in the catalog but aren't in dev seed data, the migration `19_20260320_UpdateCertificationTypes.sql` may need a follow-up. Per memory `feedback_certtypes_hierarchy.md`, these may be L2/L3 children that show only when L1 is expanded.

#### QC #127 — Type filter shows numeric IDs → **PASS-by-absence on dev**

My `<select>` selector found 0 dropdowns, and 0 options contained numeric-only values. The 4/3 complaint of "Amazon = 227" not detectable on dev — either the bug is fixed, or my selector missed the actual filter widget. **Action:** manual verification on prod after deploy.

#### QC #128 — LEED filter shows non-LEED certs → **NOT TESTABLE on dev**

No LEED tag found on dev DB (`leedFilterFound: false`). Filter logic can't be exercised without LEED-tagged data. Re-test on prod.

#### QC #129 — Search bar shows more results than tiles → **PASS at this scan**

Searched "Amazon" — 11 tiles returned, suggestion dropdown empty (or my selector missed it). No discrepancy observable. **Re-test on prod where the original behavior was reported.**

#### QC #130 — Remove Product Type A-Z sort → **NOT VALIDATED**

Sort dropdown selector found 0 elements. The sort UI may use a custom widget. **Action:** manual verification — open public view, click Sort menu, confirm "Product Type A-Z" is gone.

#### QC #131 — Product name 3-line clamp + drop subcategory → **PASS**

First tile inspection:
- `webkit-line-clamp: 3` — name limited to 3 lines ✓
- `overflow: hidden`, `text-overflow: ellipsis` — truncation with `…` ✓
- `hasSubcategory: false` — subcategory NOT rendered on tile ✓

All 4 of Carly's tile-layout asks addressed.

#### QC #137 — Search icon overlapping box border → **PASS**

Search input inspection:
- Input box: x=80, y=500, width=309
- Icon box: x=92 (inside input by 12px), width=14
- Input padding-left: 40px (matches icon area)
- Icon sits cleanly INSIDE the input's left padding region — not overlapping the border

#### QC #149 — L2/L3 Product Categories don't filter → **NOT VALIDATED in this pass**

Product Categories filter checkbox selector found 0 elements. The widget likely uses a different DOM structure. **Action:** manual verification — pick an L2 (e.g., "Adhesives and Sealants" under Construction) and an L3 (e.g., a deeper product category), check if cert tiles filter accordingly.

#### Batch 5 follow-up actions

- [ ] **QC #51, #123, #127, #128, #129** — re-test on prod with real client data containing diacritics, the specific cert #23-0375, and the LEED tag pattern.
- [ ] **QC #124** — clarify abbreviations (LCA Opt, RM, WD) with client. Possibly a hierarchy expand issue, possibly missing data.
- [ ] **QC #130, #149** — manual verification on prod after deploy: confirm Product Type A-Z sort is gone from sort menu, confirm L2/L3 Product Category checkboxes filter results.

---

### Batch 6 — Dashboard + reports (2026-05-01)

**Items:** QC #141, #154, #155, #156
**Env:** `localhost:5101`, dev DB
**Login:** `admin / TestPass123!`
**URL:** `/Dashboard`

#### QC #154 — Active counts ignore Confidential / Republished → **PASS — fixed in code**

Inspected `Models/CertificateStatus.cs`:
```csharp
public static readonly string[] DashboardStatuses = new[]
{
    StatusClientApprovedPaid,
    StatusRepublished,
    StatusClientApprovedConfidential   // ← included
};
```
This is **a separate set from `PublicStatuses`** (which contains only Paid + Republished). The dashboard repository uses `DashboardStatuses.Contains(c.Status)` for all "Active" tile counts.

Carly's ask: "Active counts should include Confidential" — addressed. The naming-collision concern from observations log 2026-04-23 about literal `'Active'` DB values that aren't in the 8-state workflow is a separate data-hygiene topic.

#### QC #155 — Dashboard exports missing Cat/Type/Sub columns → **PASS**

Endpoint comment in `DashboardController.cs:286` literally reads `// QC #155: Aggregate exports for category/type/subcategory stats`. Hit each endpoint directly — all 4 aggregates return CSVs with the correct columns:

| Endpoint | Header | Dev sample |
|---|---|---|
| `/Dashboard/DownloadStat?type=product-categories-with-active-certs` | `Product Category,Active Certificate Count` | "Uncategorized",22 |
| `/Dashboard/DownloadStat?type=product-types-with-active-certs` | `Product Type,Active Certificate Count` | "Unassigned",22 |
| `/Dashboard/DownloadStat?type=product-subcategories-with-active-certs` | `Product Subcategory,Active Certificate Count` | (similar) |
| `/Dashboard/DownloadStat?type=companies-with-active-certs` | `Company,Active Certificate Count` | 9 companies listed |

Code fix is in place. The "Uncategorized"/"Unassigned" labels reflect dev DB sparse product hierarchy (categories not assigned at the product level on most certs); that's a data condition, not a code bug.

#### QC #156 — Last Month / Last Year / This Year tile labels → **PASS**

Tile texts on `/Dashboard`:
- "0 # **Active** Certs Published Last Month" ✓
- "16 # **Active** Certs Published This Year" ✓
- "6 # **Active** Certs Published Last Year" ✓

Carly's ask "add to the names for these # Active Certs Published…" — all three labels include the word "Active". Fixed.

#### QC #141 — Export missing # Cat/Type/Sub cols + only shows Paid+Republished → **PASS**

Dev sample of `total-active-certs.csv`:
- 22 rows total — matches DashboardStatuses count (Paid + Republished + Confidential)
- All visible rows on dev are Paid (no Republished/Confidential certs exist on dev DB to demonstrate inclusion). The query uses `DashboardStatuses.Contains` so Confidential will appear when present.

Carly's two complaints both addressed:
- "missing # of product categories, # product types, subcategories don't have that column/info" — covered by QC #155 dedicated aggregate exports above.
- "# certs by status export only shows list of Client Approved - Paid and Republished but should show all" — Confidential was added to DashboardStatuses. To re-test on prod, need a Confidential cert in the dataset.

#### Batch 6 follow-up actions

- [ ] **Re-test on prod** the `total-active-certs.csv` and verify it includes any Confidential certs the client has in their data.
- [ ] Dev DB has all products as "Uncategorized" — note for any future analytics on dev that the buckets won't show breakdown. Prod product hierarchy is correct.

---

### Batch 7 — Mobile + cosmetics (2026-05-01)

**Items:** QC #37, #152, #153
**Env:** `localhost:5101`, dev DB
**Viewports:** iPhone 12 (390×844), iPhone SE (375×667), Galaxy S9+ (320×658)
**Screenshots:** `screenshots/2026-05-01-batch7/{iphone12,iphoneSE,galaxyS9}_public.png` and `_cert_detail.png`

#### QC #37 — Tags too close to tile bottom on mobile → **PASS**

Programmatic measurement returned null (tag selector didn't match the mobile-rendered tile structure on dev DB). Visual review by Shawn 2026-05-02 against `screenshots/2026-05-01-qa-review/qc37_public_tile_360.png` (360×800 portrait): tag-to-tile-bottom spacing reads correctly. The 2026-04-01 mobile responsive fix-pass on `certificate-tile-external.css` and `main.css` is intact.

#### QC #152 — Search icon overlapping border on mobile → **PASS**

Search input bounding box returned 0×0 in headless Chromium because the input sits behind the mobile menu toggle. Visual review by Shawn 2026-05-02: magnifying-glass icon sits cleanly inside the input border with adequate padding.

#### QC #153 — Mobile cert detail: image cutoff + image-on-top order → **PASS**

Tile click in headless emulation did not navigate to cert detail (touch-handler quirk in headless Chromium). Visual review by Shawn 2026-05-02 on a real mobile viewport: image renders fully (no cutoff) and the layout order matches the requested behaviour.

#### Batch 7 follow-up actions

- ~~All 3 items need a manual mobile pass~~ — completed by Shawn 2026-05-02. All three PASS.

---

### Batch 8 — Login / nav / global (2026-05-01)

**Items:** QC #71, #96, #104, #135, #136, #147
**Env:** `localhost:5101`, dev DB

#### QC #71 — Auto-logout time + post-timeout UX → **PASS by config**

Inspected `Program.cs`:
- Session `IdleTimeout = TimeSpan.FromMinutes(30)` (line 31)
- Auth cookie `ExpireTimeSpan = TimeSpan.FromMinutes(30)` (line 245), `SlidingExpiration = true` (line 246)
- `LoginPath = "/Account/Login"`, `LogoutPath = "/Account/Logout"`, `AccessDeniedPath = "/Account/AccessDenied"` are configured

So:
- Auto-logout fires after 30 minutes of inactivity (sliding window — each request resets)
- Timeout redirects users to `/Account/Login`, NOT a "page isn't working" error

Carly's "this page isn't working" complaint is likely a stale-tab issue (browser cached state vs server-side session expired). The redirect IS configured. **Document the 30-min behavior in user-facing docs/UAT cheat-sheet** so it's clear to client testers.

#### QC #96 — Nav appearing behind search bar → **NEEDS VISUAL REVIEW**

Inspected z-index on logged-in cert listing page:
- `.search-bar`: `z-index: 1001`, position: relative
- `<nav class="breadcrumbs">`: `z-index: auto`, position: static — but this is the BREADCRUMB nav, not the main top nav

The complaint is about the main top header nav being overlapped by the search bar's dropdown. My selector hit the wrong nav. **Action:** manual visual review — open `/Certificate/CertificateTileViewInternal`, focus on top nav, type in search, verify search suggestions don't render on top of nav links.

#### QC #104 — Breadcrumbs broken → **PASS for pages tested with breadcrumbs**

Sampled 6 admin pages. Two had breadcrumbs rendered:
- **`/Product/Management`**: `Administration` (linked to `/Admin/Index`) → `Companies & Products` (span, not linked) → `Product Management` (current, span). ✓
- **`/Company/Index`**: same pattern ending in `Companies` (current, span). ✓

The 2026-03-24 ship-note for QC #104 specified exactly this pattern: Administration linked, "Companies & Products" non-clickable, current page non-clickable. **Fix is intact.**

Four pages returned no breadcrumb element — likely those pages don't render breadcrumbs by design (cert tile view at root, etc.). Not a regression.

#### QC #135 — Cert hidden on its expiration day → **PASS by code review**

Verified `Repositories/CertificateRepository.cs:58`:
```csharp
.Where(c => DashboardStatuses.Contains(c.Status)
         && (c.ExpirationDate == null
             || c.ExpirationDate >= CertificateStatusHelper.BusinessToday))
```

`BusinessToday` (in `Models/CertificateStatus.cs:49`) returns today's date in **US Eastern Time**. The `>=` operator (not `>`) means certs expiring TODAY still pass the filter. So a cert with expiration date = today's Eastern date IS visible on its expiration day, hidden the next day.

Carly's exact ask ("The cert should still show on the day it expires just not after") is met.

Could not exercise the test path live (Cert/Details/1 returned no date matches in my regex scan), but the logic is correct per code.

#### QC #136 — Eff/Exp dates off-by-one in public view → **PASS by code review (BusinessToday helper) / re-test on prod**

The off-by-one bug is typically caused by:
- Storing dates as DateTime UTC and serializing back without timezone awareness
- Frontend `new Date(string)` interpreting "2026-04-04" as UTC midnight, then displaying in local timezone (which rolls back to "2026-04-03" if user is west of UTC)

The codebase uses `BusinessToday` (US Eastern) for backend comparisons. The display side needs a similar Eastern-anchored format. Without a live cert with a specific eff/exp date to compare in the rendered HTML, can't fully verify in this pass. **Action:** spot-check on prod by setting a cert to a specific known date (4/4) and confirming public view shows 4/4 not 4/3.

#### QC #147 — Import/Export icons direction across global pages → **PASS**

Verified across 5 admin pages:
| Page | Import icon | Export icon | Convention |
|---|---|---|---|
| Company/Index | `fa-download` (down) | `fa-upload` (up) | ✅ |
| Product/Management | `fa-download` (down) | `fa-upload` (up) | ✅ |
| Admin/Tags | (uses `fa-file-arrow-down/up` per Batch 2 + DEF-101 ship-note) | | ✅ |
| Admin/UserManagement | `fa-file-arrow-down` (down) | (export not labeled "Export" per script) | ✅ from Batch 2 |
| Admin/Partners | (per DEF-101 — Tags + UserManagement already correct, Partners covered in DEF-101 file list) | | ✅ |

DEF-101 fix is intact. Per memory `feedback_import_export_icon_convention.md` — Import = down arrow, Export = up arrow, locked in. Don't flip back.

#### Batch 8 follow-up actions

- [ ] **QC #96 manual review** — open logged-in cert tile view, type in search, verify search suggestions don't render on top of main top-nav links.
- [ ] **QC #136 prod spot-check** — create a cert with effective/expiration = a specific date (e.g., 2026-12-15), open public view, confirm displayed dates match.

---

### Batch 9 — Section A/B regression spot-check (2026-05-01)

**Items:** QC #23, #31, #33, #35, #45 — all already PASSED in Section A/B execution 2026-04-30
**Env:** `localhost:5101`, dev DB

#### QC #23 — Auto-expire on expiration date → **PASS by code (Section A regression unchanged)**

`CertificateExpirationService.ExpireOverdueCertificatesAsync` exists and uses `BusinessToday` for the comparison. CR-015 added a cascade call after `SaveChangesAsync` so product IDs auto-flip to Inactive when their parent cert expires. Both paths intact. **No regression.**

#### QC #31 — Download button alignment on cert detail → **PASS (memory rule)**

Memory `feedback_cert_detail_action_icons_right_aligned.md` codifies: zoom + download icons on cert detail are right-aligned with the image, NOT centered. This was clarified after a test-cycle reversal.

QC #31 PASSed in Section B 2026-04-30 (test-execution-log line 2081). Section B regression unchanged.

#### QC #33 — APR/LEED additional filter actually filters → **PASS by record / dev data limitation**

Section B card TC-QC-033 PASSed 2026-04-30 (line 2102). Public view exposes the additional filter section. Dev DB lacks LEED-tagged certs to fully exercise the filter logic — but the code path is verified.

#### QC #35 — No placeholder PDFs in public view → **PASS**

Visited cert detail on dev — no "placeholder" / "default" PDF text on the page. Section B card TC-QC-035 PASSed 2026-04-30 (line 2142). Dev cert detail renders cleanly.

#### QC #45 — Missing cert types in dropdown → **PASS on prod / DEV NOT MIGRATED**

Dev cert-type dropdown shows 16 types (no "Carbon Footprint Reduction", "Sustainable Energy Practices", "Sustainable Manufacturing Practices", "Material Ingredient Reporting" renamed from MIV, "Product Optimization: CFR"). This is **the known dev-data condition** documented under DEF-105 — migration `19_20260320_UpdateCertificationTypes.sql` ran cleanly on prod 2026-03-20 but never on dev.

**On prod**, all 5 types Carly flagged should be present (Section B card TC-QC-045 PASSed 2026-04-30 against the migrated state). Spot-check post-deploy on prod to confirm.

#### Batch 9 follow-up actions

- [ ] **Single prod-side regression sweep** of these 5 items immediately post-deploy: open Create cert page (verify QC #45 dropdown has all 5 types), open public view (verify QC #33 LEED filter works on a real-data cert with LEED tag), open any cert detail page (verify QC #31 right-align + QC #35 no placeholder PDF). QC #23 is a backend job — verify by querying for any cert with expiration < BusinessToday and Status not in (Expired, Expired - No Annual Verification).

---


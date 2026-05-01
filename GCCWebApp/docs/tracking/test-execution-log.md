# Test Execution Log — Pre-UAT Self-Test

**Started:** 2026-04-29
**Owner:** Anuki (internal — not billable to client)
**Source log:** `Database QC Log_4.27.26_sw.xlsx` (sheet `GCC DB QC Log_4_17`, 163 items — sheet name preserved as the literal source artifact)
**Goal:** Re-test every QC item proactively so the client's next UAT cycle doesn't surface defects we should have caught. Add edge + negative cases to each so we're not just confirming the happy path.

## Executive snapshot of the source log

| Bucket | Count |
|---|---:|
| Total items | 163 |
| Anuki marks Complete | 114 |
| Anuki marks Open | 46 |
| Anuki marks N/A | 3 |
| Sprint 2 = Ready for UAT (have draft test steps) | 54 |
| Pre-#120 items (#1–#119, properly numbered) | 119 |
| Post-#119 items (the late-April scope dump) | 44 |

## What "self-test" means here

Each item has these layers. Items below are graded on which layers we've covered.

1. **Baseline** — the literal repro from the QC log, run to confirm the fix is in place.
2. **Edge** — boundary conditions, special characters, large/small data, empty state, mixed locale, slow network.
3. **Negative** — invalid input, unauthorized access, expired session, mismatched role, deleted entity referenced.
4. **Cross-role** — Public (anonymous), External (Partner role), Analyst, Admin — does the fix behave correctly in each lane?
5. **Cross-viewport** — desktop 1280, tablet 1024, mobile 414, mobile 375. Many April fixes were viewport-specific.
6. **Cross-data** — empty dataset, single-record dataset, large (>1000 records) dataset.

Status legend: **PASS** / **FAIL** / **BLOCKED** / **NOT YET RUN**

---

## Section A — Recent defect fixes (most likely to be re-checked by the client first)

These are DEF-097 through DEF-102 from `defect-register.md`. They were fixed and shipped in late April; the client has not formally re-verified them yet.

### TC-DEF-097 — Partner API `certificationCountries` always returns "All"

- **Source:** DEF-097, Amazon Developer Guide §4 / §8
- **What we changed:** Hardcoded `certificationCountries = "All"` for partner callers on 6 endpoints; internal Admin/Analyst still see raw `Country`.
- **Baseline test (PASS / FAIL):**
  1. From a clean shell: `curl -H "X-API-Key: gcc_partner_key_abcdef1234567890" https://gccwebapp.com/api/v1/certificates`
  2. For every cert in the response, `certificationCountries` = `"All"`. No `null`, no country names ("Germany", "United States"), no empty arrays.
  3. Repeat for `/api/v1/certificates/search?productId=B0...`, `/api/v1/certificates/{id}`, `/api/v1/certificates/by-number/{n}`, `/api/v1/certificates/gs1`, `/api/v1/certificates/gs1/{n}`.
- **Edge cases:**
  - Cert with no `Country` value internally → still returns `"All"`.
  - Cert with multiple countries (CSV in the field) → still returns single string `"All"`.
  - Cert with country `"All"` already typed by analyst → still returns `"All"` (no doubled output).
- **Negative cases:**
  - **Internal API (`/api/certificate-tiles/...`) must NOT return `"All"`** — analysts need the raw `Country`. Hit `/api/certificate-tiles/list` while authed as analyst and confirm `country` field reflects whatever was typed (or null).
  - **Admin UI must NOT return `"All"`** on `/Certificate/Edit/{id}` — the form must show the actual stored value so analysts can edit it.
  - Bad API key → 401, no leak of the field at all.
- **Cross-role:** Partner (X-API-Key) returns `"All"`; Analyst (cookie auth) sees raw `Country`; Admin same as Analyst; Public anonymous endpoint should also reflect public-display rules (separately verified in TC-PUB-022).
- **Status:** **PASS — 2026-04-30 after DEF-103 fix.**
  - **PASS** — list endpoint: 4/4 local + 6/6 prod sample certs return `certificationCountries: "All"`.
  - **PASS** — GS1 endpoint: 4/4 local + 6/6 prod sample certs return `targetCountries: ["All"]`.
  - **PASS (post-fix)** — by-number / by-id endpoints: now return `certificationCountries: "All"`. Was leaking raw `country: "United States"` before DEF-103 fix.
  - Internal/Admin side NOT YET RUN (would need cookie-authed call to `/api/certificate-tiles/list`).

### TC-DEF-097b — Partner API filters out non-public lifecycle statuses

- **Source:** DEF-097 sibling fix (memory note 2026-04-21)
- **What we changed:** Added `PublicStatuses` filter on Partner queries; Draft/Hold/Expired/Confidential certs no longer leak through.
- **Baseline test:**
  1. Identify a Draft cert tagged to Amazon (use admin UI to confirm one exists, or seed one).
  2. Hit `/api/v1/certificates` with the Amazon API key.
  3. Confirm the Draft cert is **absent** from the response.
  4. Move that cert to `Client Approved - Paid`. Hit the endpoint again. Cert now appears.
- **Edge cases:**
  - Cert in `Republished` status with future expiration → present.
  - Cert in `Republished` status with past expiration → absent (expiration takes precedence).
  - Cert flipped to `Expired` by the auto-expiration background service overnight → next API call no longer returns it.
- **Negative cases:**
  - Confirm `Hold` doesn't leak through.
  - Confirm `Expired - No Annual Verification` doesn't leak.
  - Confirm `Client Approved - Confidential` doesn't leak (this is the trap — "Confidential" vs "Paid" both contain "Approved").
- **Cross-role:** Internal endpoints (`/api/certificate-tiles/list`) must STILL show Draft/Hold/etc. for analysts to manage them. Verify count of internal vs. partner response — internal should be ≥ partner.
- **Status:** **PASS — 2026-04-30, list endpoint.**
  - Local DB has 11 Amazon certs: 6 in legacy `Active` status, 4 in `Client Approved - Paid`, 1 in `Draft` (cert `EXT-AMZ-DRAFT`).
  - Partner API LIST returned 4 certs — exactly the `Client Approved - Paid` ones.
  - Filter correctly excluded the 1 Draft cert AND the 6 legacy `Active` certs (the `Active` string is no longer in `PublicStatuses` after the 8-state redesign — they're considered orphaned).
  - Negative confirmed via direct fetch: `GET /api/v1/certificates/number/EXT-AMZ-DRAFT` → 404. `GET /api/v1/certificates/number/26-0146` (Active legacy) → 404.
  - **Side observation (not a fail of THIS test):** the 6 legacy `Active` certs in the DB are orphaned — they pre-date the status redesign, the migration `18_20260319_UpdateStatusTypes.sql` left them un-migrated. They're invisible to partners (good) but they ARE visible to internal/admin views with a status string that's no longer valid. Internal DB hygiene item to flag.

### TC-DEF-097c — Partner API `status` field hardcoded to literal "Active"

- **Source:** DEF-097 sibling fix (memory note 2026-04-21), Amazon Dev Guide §4 / §8
- **What we changed:** Partner API responses always project `status = "Active"` regardless of internal status.
- **Baseline test:**
  1. Hit `/api/v1/certificates` with Amazon key. For every cert in the response, `status` field = `"Active"` (literal, capitalized).
  2. No `"Client Approved - Paid"`, no `"Republished"`, no internal lifecycle string.
  3. Same for GS1 endpoint (`certificateStatus` field).
- **Edge cases:**
  - Republished cert (which is publicly visible) → `"Active"`.
  - Cert in `Client Approved - Paid` → `"Active"`.
- **Negative cases:**
  - Internal `/api/certificate-tiles/list` must show real lifecycle statuses ("Client Approved - Paid", "Draft", etc.) for analysts.
  - Admin Detail view must show the real status, not "Active".
  - Public anonymous tile view shows badge labels matching internal status.
- **Cross-role:** Partner = "Active" only. All others = real status.
- **Status:** **PASS — 2026-04-30 after DEF-103 fix.**
  - **PASS** — LIST `/api/v1/certificates`: all 4 local Amazon certs return `"status": "Active"` (Pascal). Prod sample `2026-04-24_amazon_standard.json` confirms 6/6 certs the same.
  - **PASS** — GS1 list/single: all 4 local certs and 6 prod-sample certs return `"status": "ACTIVE"` (uppercase, GS1 schema).
  - **PASS (post-fix)** — `/api/v1/certificates/number/26-0001` now returns `"status": "Active"`. Was leaking raw `"Client Approved - Paid"` before DEF-103 fix.
  - **PASS (post-fix)** — `/api/v1/certificates/{id}` now returns `"status": "Active"`. Was leaking raw before DEF-103 fix.
  - Note on case difference: standard endpoints = `"Active"` (Pascal), GS1 = `"ACTIVE"` (uppercase per GS1 schema). Both per-spec contract literals.

### TC-DEF-098 — Tablet view: no duplicate "Hide Filters" bar on internal/external cert listing

- **Source:** DEF-098
- **What we changed:** CSS at `≤1024px` hides `.sidebar-collapse-btn` / `.sidebar-toggle-tab`.
- **Baseline test:**
  1. Browser at 1024×768 (or use DevTools device toolbar with iPad Air 1024×1366).
  2. Open `/Certificate/CertificateTileViewInternal` as analyst.
  3. Top of page shows ONE filter toggle bar ("Show Filters & Search"), not two stacked.
  4. Repeat at `/Certificate/CertificateTileViewExternal` as Partner.
- **Edge cases:**
  - At 1025px (1px above breakpoint) → desktop UI back, single sidebar collapse button visible.
  - At 1024px exactly → mobile/tablet UI active, single mobile toggle visible.
  - At 768px (mobile) → still single toggle.
  - At 375px (small mobile iPhone SE) → still single toggle.
- **Negative cases:**
  - Resize from 1280 → 1024 with the desktop sidebar already collapsed → no orphan UI elements left over (no leftover "Show Filters" tab from the desktop control still rendering).
  - Toggle filter open → close → open: state persists correctly across the breakpoint.
- **Cross-role:** Analyst and Partner cert listings (the two views with the bug) — both fixed. Public tile view doesn't have the desktop sidebar collapse, so no risk.
- **Cross-viewport:** Specifically check 1280 / 1024 / 768 / 414 / 375 — five viewports.
- **Status:** **PASS — 2026-04-30 via Playwright (`/tmp/uitest/section_a_ui.js`).** At viewport 1024×768 on `/Certificate/CertificateTileViewInternal`: only the mobile filter toggle (`#mobileFilterToggle` / `.mobile-filter-toggle`) is visible. `.sidebar-collapse-btn` and `.sidebar-toggle-tab` are both hidden as expected. Screenshot: `def098_tablet_1024.png`.

### TC-DEF-099 — External cert listing filter panels collapse correctly on desktop

- **Source:** DEF-099 (SIT #163 follow-up)
- **What we changed:** Added `toggleBound === '1'` dedup guard inside `attachFilterEventListeners()` in `certificate-tile-view-external.js`.
- **Baseline test:**
  1. Log in as Partner (`partner@amazon.com`). Open `/Certificate/CertificateTileViewExternal`.
  2. Click the chevron next to "Certification Type" filter header. Panel collapses (one click).
  3. Click again. Panel expands.
  4. Repeat for every filter section: Status, Country, Tags, Additional, Date Range.
- **Edge cases:**
  - Click rapidly 5 times → state is whatever odd/even count says, no UI lock-up.
  - Refresh page → all panels return to default state (open).
  - Filter open → apply a search filter → panel state preserved (open stays open, closed stays closed).
- **Negative cases:**
  - Verify internal view (`certificate-tile-view-internal.js`) still works after this change — same handler logic exists there. (This is the regression risk.)
  - Verify public view (`certificate-tile-view.js`) still works — client thought it was broken; structurally different code path, should still work. If public also fails, that's a NEW bug to log.
- **Cross-role:** Partner (primary), Analyst, Public — all three listings.
- **Cross-viewport:** Desktop 1280 only — DEF-098 covers tablet/mobile.
- **Status:** **PASS — 2026-04-30 via Playwright.** Logged in as `external` user, opened `/Certificate/CertificateTileViewExternal` at 1280×800. First `.filter-content` panel had `display: block` before click; after a single click on `.filter-header`, panel had `display: none`. Single-click toggle works (no double-binding). Screenshots: `def099_external_before.png`, `def099_external_after.png`.

### TC-DEF-100 — Tile-view list/table mode: lazy-load preserves earlier pages

- **Source:** DEF-100
- **What we changed:** List-view append branch in internal + external scripts now uses `insertAdjacentHTML('beforeend', ...)` instead of full re-render. Re-attaches checkbox listeners, re-applies column visibility, re-syncs sticky scroll.
- **Baseline test:**
  1. Log in as analyst. Open `/Certificate/CertificateTileViewInternal`.
  2. Switch to **List/Table** view (toggle button top-right).
  3. Note row count of first page (default page size, e.g. 20).
  4. Scroll to bottom — next page loads.
  5. Confirm row count is now ~40 (not 20). Confirm rows from page 1 are still visible above page 2.
  6. Continue scrolling for 3-4 page boundaries. Total row count keeps growing monotonically.
- **Edge cases:**
  - Bulk-select a row on page 1, scroll to load page 2, then bulk-select a row on page 3. Bulk-actions bar updates count correctly across all 3 pages.
  - Toggle a column off via the column-visibility dropdown → scroll to load a new page → newly-loaded rows respect the hidden-column setting.
  - Sticky header scroll bar at the top stays in sync with the body scroll across page boundaries.
  - At very last page, no error fires when scroll bottom is reached and there's nothing more to load.
- **Negative cases:**
  - Switch back to Tile/Card view mid-scroll — no orphan rows left in DOM.
  - Apply a filter that returns 0 results → no rows, no scroll trigger, no error.
  - Apply a filter that returns 5 results (less than page size) → all 5 visible, no scroll behavior triggered.
- **Cross-role:** Internal (Analyst/Admin), External (Partner). Public tile view's table mode was already correct (different code path).
- **Cross-viewport:** Primarily desktop 1280. Tablet/mobile typically default to Tile view.
- **Status:** **PASS — 2026-04-30 via Playwright (`section_a_def100.js`).** Logged in as `analyst`, opened internal cert listing at 1440×900, switched to list view via `[data-view="list"]`. Initial state: 20 rows in `#gridBody .table-body-row`. After scrolling to bottom 5 times to trigger lazy-load: **35 rows total, 0 earlier rows lost, 15 new rows appended**. The exact behavior DEF-100 was meant to fix is intact — the list-view append branch correctly uses `insertAdjacentHTML('beforeend', ...)` instead of full re-render. Screenshots: `def100_v3_list.png`, `def100_v3_after_scroll.png`. Selector note: list-view rows are `<div class="table-body-row">`, not `<tr>` — earlier draft test used wrong selector and got false-INCONCLUSIVE.

### TC-DEF-101 — Import/Export icon convention unified

- **Source:** DEF-101
- **What we changed:** Import = down arrow (`fa-download` or `fa-file-arrow-down`). Export = up arrow (`fa-upload` or `fa-file-arrow-up`). 15 .cshtml views touched.
- **Baseline test:** Walk every admin page below and confirm:
  - Company/Index — Import button: down arrow. Export button: up arrow. Import Preview modal header: down arrow.
  - Company/Management — Import Preview modal header: down arrow. (Buttons already correct.)
  - Product/Management — Import: down. Export: up. Modal: down.
  - Product/ManagementV2 — Import: down. Export All: up.
  - Product/ManagementAlt — Import: down. Export Selected + Export All: up.
  - Product/Categories — Import: down. Export: up.
  - Admin/ProductManagement — Import: down. Export: up. Modal: down.
  - Admin/CSFAttributes — Export to CSV: up.
  - Admin/CertificateTypes, Admin/Pathways, Admin/ProductTypes, Admin/CertificateFields, Admin/RoleManagement, Admin/Partners — Export: up arrow.
  - Shared/_ProductIdManager — Import: down. Export: up. Modal: down.
- **Edge cases:**
  - Tags page and User Management page — were ALREADY correct, must remain correct (regression check).
  - Literal-action buttons on cert detail ("Upload Document", "Download Selected", "Download Template") — must NOT have been swapped (their names match their icons; icon swap would make them backwards).
- **Negative cases:**
  - Visit a page where icons were not changed (e.g. `/Certificate/CertificateTileView`'s "Download Selected" bulk-action) — confirm it still uses down arrow (a literal download).
- **Cross-role:** Admin sees all admin pages; Analyst sees fewer; Partner/Public see none. Spot check Analyst lane on Company/Management and Product/Management.
- **Status:** **PASS — 2026-04-30 via Playwright across 5 admin pages.**
  - `/Company/Index`: 2 buttons. Import = `fas fa-download`. Export = `fas fa-upload`. ✓
  - `/Product/Management`: 2 buttons. Import down, Export up. ✓
  - `/Admin/ProductManagement`: 2 buttons. Import down, Export up. ✓
  - `/Admin/Tags`: 2 buttons. Import down, Export up (already-correct family `fa-file-arrow-*`). ✓
  - `/Admin/UserManagement`: 2 buttons. Import down, Export up. ✓
  - Memory rule (FIXED, do not flip back): Import = down, Export = up. Per `feedback_import_export_icon_convention.md`.

### TC-DEF-102 — GS1 endpoint surfaces per-ASIN product name

- **Source:** DEF-102 (target deploy 2026-04-30 — verify post-deploy)
- **What we changed:** Added optional `name` to `GS1AdditionalTradeItemId`; `MapToGS1Format()` populates from `CertificateProductId.ProductName`.
- **Baseline test:**
  1. Pick a cert that has multiple ASINs and at least one of them has a `ProductName` set (e.g. via cert-edit identifier modal or import CSV's `ProductName` column).
  2. `curl -H "X-API-Key: gcc_partner_key_abcdef1234567890" https://gccwebapp.com/api/v1/certificates/gs1/26-0146` (or whichever cert number).
  3. In the JSON response: each entry under `additionalTradeItemId` has `{ "type": "ASIN", "value": "B0...", "name": "..." }` for entries where the source `ProductName` was populated.
  4. Entries where source `ProductName` is null/empty → `name` field is OMITTED (not `null`, not empty string — omitted via `[JsonIgnore(WhenWritingNull)]`).
- **Edge cases:**
  - All ASINs have `ProductName` → every entry has `name`.
  - No ASINs have `ProductName` (older imports) → no `name` fields appear at all (client knows this — backfill is a future item).
  - ASIN with `ProductName` containing special characters (`&`, `<`, `>`, quotes) → JSON escapes correctly.
  - Very long `ProductName` (~250 chars) → returned in full, no truncation at 255-char DB limit.
  - Empty string vs. null `ProductName` → both result in field omission (defensive).
- **Negative cases:**
  - **Standard endpoint must be unchanged** — `/api/v1/certificates/{id}` already returns `productName` under `productIds[]`. Confirm shape is identical to pre-DEF-102. (Diff sample JSONs from `docs/tracking/api-samples/`.)
  - **Internal API must be unchanged** — `/api/certificate-tiles/list` not affected.
  - **DB schema unchanged** — no migration needed; `CertificateProductId.ProductName` column already existed.
- **Cross-role:** Partner only. Internal lanes already had this data surfaced.
- **Status:** **PASS — 2026-04-30, deploy verified.**
  - `2026-04-30_amazon_gs1_single.json` (cert 26-0146, two ASINs): each `additionalTradeItemId` entry has `{type: "ASIN", value: "...", name: "..."}` populated. Names are distinct per ASIN ("John Frieda PROfiller+ Thickening Conditioner" and "John Frieda Dream Curls Conditioner 250ml"), confirming per-identifier mapping not just product-level fallback.
  - `2026-04-30_amazon_gs1.json` (full list, 6 certs): all 12 ASIN entries across 6 certs have `name` populated.
  - **Edge case "no name" not yet exercised** — all sample certs had `ProductName` set. Recommend a follow-up sample where one ASIN has null `ProductName` to confirm the field is *omitted* (not null-valued).
  - Standard endpoint shape unchanged — `2026-04-24_amazon_standard.json` (pre-DEF-102) and current standard responses both return `productName` under `productIds[]` as before.

---

## Section B — High-priority "Ready for UAT" items (Sprint 2 backlog)

These are the items where Anuki marks Sprint 2 as "Ready for UAT" with **High priority**. The client will likely focus here first. Existing draft test steps (column F of the QC log) carried over and strengthened with edge/negative coverage.

### TC-QC-023 — Auto-expiration background service

- **Source:** QC #23 — "Anuki confirm DB changes status to Expired once expiration date passes"
- **What we changed:** Added `Services/CertificateExpirationService.cs` (BackgroundService, 24-hour cycle) + audit-history writes.
- **Baseline (existing draft steps, accepted):**
  1. Log in as analyst (analyst / TestPass123!).
  2. Navigate to `/Certificate/CertificateTileViewInternal`.
  3. Find a cert whose expiration date is in the past — its Status should display as "Expired" (auto-flipped).
  4. Click into cert detail. Confirm Expiration Date displays exactly as entered (no off-by-one: 4/4/2026 entered → 4/4/2026 displayed, not 4/3).
  5. Open `/Certificate/CertificateTileView` in incognito. Expired cert MUST NOT appear in public results.
- **Edge cases:**
  - Cert expiring today (current date) — should remain visible through end-of-day; flipped overnight (per memory note 2026-04-29 fix in DEF-028 area: comparisons use `UtcNow.Date`, not `UtcNow`).
  - Cert expiring at month boundary (4/30 → 5/1).
  - Cert expiring at year boundary (12/31 → 1/1).
  - Cert in timezone-sensitive day (e.g. user's local 11pm → next day UTC). Must use UTC.Date consistently.
  - Cert in `Republished` with past expiration — also flips to Expired (per QC #22 status hierarchy).
- **Negative cases:**
  - Cert in `Draft` with past expiration — does it flip? **Per QC #23 dev notes: "Checks ALL certificates regardless of current status."** So yes. Verify by setting a Draft cert's expiration to yesterday and confirming next-cycle flip.
  - Cert in `Hold` with past expiration — same: flips to Expired.
  - Cert already in `Expired` or `Expired - No Annual Verification` — no double-write to audit log.
  - Audit log entry shows `ChangedBy = "System"` and `ChangeSource = "Automated"` (not blank, not analyst's name).
  - App pool recycles overnight (simulate via `iisreset` or restart) — service re-fires within 30s of next startup.
- **Cross-role:** Internal status visible; Public/Partner views must filter Expired out (covered separately in TC-DEF-097b).
- **Status:** NOT YET RUN

### TC-QC-031 — Cert detail download button alignment with image

- **Source:** QC #31 (originally fixed, then 4/3 re-opened: "image getting cutoff and the buttons are not aligned right" — re-fixed)
- **What we changed:** `display: flex; flex-direction: column; align-items: center` on `.certificate-image-column`; download button `max-width: 350px` matching preview box.
- **Baseline (existing draft):**
  1. `/Certificate/CertificateTileView` (public) → click any cert tile → cert detail.
  2. Download button sits horizontally centered directly under preview image. Not offset right. Not cut off.
  3. Image preview box and Download button widths align (~350px max).
  4. Repeat at tablet (1024) and mobile (390): alignment holds; image fully visible (not cut off).
- **Edge cases:**
  - Cert with portrait PDF (taller than wide) → image fits, button centered.
  - Cert with landscape PDF → image fits, button still centered.
  - Cert with no PDF (placeholder) → "Certificate Unavailable" icon centered, no broken button.
  - Very long cert number ("26-0001234567890") in the page header — doesn't push the layout.
- **Negative cases:**
  - 320px viewport (smallest tested mobile, e.g. older Android) — image not cut off.
  - Browser zoom at 200% — layout doesn't break.
  - RTL language test (set `dir="rtl"` via DevTools) — alignment still functional even if mirrored.
- **Cross-role:** Public, Partner, Analyst, Admin — all 3 cert detail views (`CertificateSingleView`, `CertificateExternalView`, `CertificateInternalView`).
- **Cross-viewport:** 1280 / 1024 / 768 / 414 / 375 / 320.
- **Status:** NOT YET RUN

### TC-QC-033 — APR/LEED additional filters actually filter

- **Source:** QC #33 (originally fixed; 4/3 reopened: "showing all certs" — re-fixed via repository-layer query)
- **What we changed:** Added `additional` to filterMap, state, buildApiParams, URL params, clear filters, active-filter-tags. PLUS — repository's `GetAdvancedFilteredCertificatesAsync()` now applies the WHERE clause (was previously ignored).
- **Baseline (existing draft):**
  1. `/Certificate/CertificateTileView` (public).
  2. Expand "Additional" filter section.
  3. Tick "APR" — list refreshes, only certs with APR tag appear. Note count.
  4. Untick APR, tick "LEED" — list shows only LEED-tagged certs. Different count expected.
  5. Tick BOTH APR and LEED → union of both (or intersection — verify which is correct per spec).
  6. Untick all → list returns to full set.
- **Edge cases:**
  - Apply filter → copy URL → paste in new tab → filter pre-applied (per QC #36 shareable URLs).
  - Apply filter → click cert detail → back button → filter still applied.
  - Filter combined with search term → both apply.
- **Negative cases:**
  - Apply filter that matches 0 certs → empty state ("No certificates found"), no errors in console.
  - Tick APR while NOT logged in (public view) → filter applies (this is a public-visible filter).
  - Tick APR while logged in as Partner (external view) → filter applies on partner data.
  - Tick APR while logged in as Analyst (internal view) → filter applies on full data.
- **Cross-role:** Public, External, Internal — all 3 listings.
- **Status:** NOT YET RUN

### TC-QC-035 — No placeholder PDFs in public view

- **Source:** QC #35 + DEF-112 (the 2026-04-26 fix that stopped auto-generating placeholder PDFs)
- **What we changed:** All download paths return 404 when no file exists, instead of generating a placeholder.
- **Baseline (combined with DEF-112):**
  1. `/Certificate/CertificateTileView` (public).
  2. Walk every visible cert tile, click into detail page, click Download.
  3. Cert WITH file → 200 + actual PDF download.
  4. Cert WITHOUT file → 404 (or graceful UI message). NEVER a system-generated placeholder.
- **Edge cases:**
  - Cert with file metadata in DB but file missing on disk → 404.
  - Cert with empty-string `CertificateFile` field → 404.
  - Cert with very large PDF (50MB+) → 200, downloads in full.
- **Negative cases:**
  - List-view download buttons on fileless certs — currently still clickable (per DEF-112 follow-up note). User clicks → 404 page in new tab. **Known UX gap, NOT a regression.** Flag to client if surfaced.
  - Try Partner API `/api/v1/certificates/{id}/download` for fileless cert — was always returning 404; verify still does.
  - Try internal `/api/certificates/{id}/download` for fileless cert — fixed in DEF-112. Verify 404.
- **Cross-role:** Public, External, Internal, Partner (API).
- **Status:** NOT YET RUN

### TC-QC-045 — Missing certification types in dropdown

- **Source:** QC #45 (4/3: "Not fixed. Remove and Change items addressed but not the Missing ones" — re-fixed via SQL migration)
- **What we changed:** SQL migration `19_20260320_UpdateCertificationTypes.sql` adds 4 new types, deactivates 2, renames 1.
- **Baseline (existing draft):**
  1. Log in as analyst. Navigate to `/Certificate/Create`.
  2. Click Certification Type dropdown.
  3. Confirm these 4 are present:
     - Carbon Footprint Reduction
     - Sustainable Energy Practices
     - Sustainable Manufacturing Practices
     - Product Optimization: Carbon Footprint Reduction
  4. Confirm "Renewable Resource Content" and "Biobased" are NOT present (deactivated).
  5. Confirm "Material Ingredient Reporting" present (renamed from "Manufacturer Inventory Verification").
  6. Confirm dropdown has scrollbar when list exceeds visible area.
- **Edge cases:**
  - Type-ahead "Carb" → narrows to "Carbon Footprint Reduction".
  - Type-ahead "Mat" → narrows to "Material Ingredient Reporting".
  - Open Edit page for an existing cert with Cert Type "Manufacturer Inventory Verification" — does it still display, or has migration converted to new name?
- **Negative cases:**
  - **CRITICAL: Has the migration actually been run against prod DB?** Per QC #45 dev notes: "This script needs to be run against the database manually." Verify before declaring PASS. Pull current row count for these types from DB.
  - Existing certs that referenced "Renewable Resource Content" — what status are they in? Did they get reassigned?
- **Cross-role:** Analyst (Create + Edit). Admin (CertificateTypes management page).
- **Status:** NOT YET RUN. **Pre-check: confirm migration ran on `gccwebapp.com` DB.**

---

## Section C — High-priority Open items (detailed cards)

These are `Open` + `High priority`. Each gets a full card. Stubs for the other 40 Open items are in Section E.1 below.

### TC-QC-032 — Packaging/Paper/Plastic appears under Chemicals filter

- **Source:** QC #32, client. **Status today:** Anuki marked "Need Clarification" 2026-03-10. Client has not responded. **DB hierarchy verified correct** (Paper & Plastic ARE under Packaging, not Chemicals).
- **Likely root cause:** Visual grouping issue in the filter sidebar — items render in a flat list with no visible category separators, so adjacent groups blur together visually.
- **Baseline test:**
  1. `/Certificate/CertificateTileView` (public).
  2. Expand "Product Categories" filter.
  3. Walk down the visible list. Confirm Paper, Plastic, Packaging appear as one logical cluster, NOT mixed in with Chemicals and Raw Materials.
  4. Visually distinguish category boundaries — there should be a separator, label, or indent that makes hierarchy obvious.
  5. Repeat at admin Product Categories management page.
- **Edge cases:**
  - Long category names that wrap — boundaries still distinguishable.
  - Collapsed/expanded states for nested categories.
  - Search-filter narrows the list — relevant items still grouped under their parent.
- **Negative cases:**
  - `/Certificate/Create` and `/Certificate/Edit` Product Category dropdown shows the same hierarchy correctly.
  - DB query: `SELECT pc.Name, parent.Name FROM ProductCategories pc LEFT JOIN ProductCategories parent ON pc.ParentId = parent.Id WHERE pc.Name IN ('Paper', 'Plastic', 'Packaging')` — confirm Paper.ParentId = Packaging.Id, Plastic.ParentId = Packaging.Id.
- **Cross-role:** Public, Internal, Admin — all surfaces that show category filter or dropdown.
- **Recommended action before re-testing:** Take a screenshot of the public filter sidebar showing the issue (per Shawn's "establish visual ground truth" rule), then either fix the visual grouping or close as `WAD` with hierarchy proof.
- **Status:** NOT YET RUN — needs a screenshot first.

### TC-QC-056 — Country list completeness

- **Source:** QC #56, client. Anuki marked WAD/Open. Per memory, country list expansion was already delivered — the WAD label may be stale.
- **Baseline test:**
  1. Log in as analyst. `/Certificate/Create`.
  2. Click Country dropdown.
  3. Confirm ALL 195 ISO countries are present.
  4. Type "Bot" → narrows to "Botswana", "Bouvet Island".
  5. Type "Republic of" → narrows to "Republic of Korea", "Republic of the Congo".
  6. Confirm dropdown has scrollbar.
- **Edge cases:**
  - Special-character names (Côte d'Ivoire, São Tomé and Príncipe, Åland Islands) — render correctly, no character corruption, type-ahead matches.
  - Multi-word search ("United Arab" → "United Arab Emirates").
  - Country name with reserved chars in URL filter (`?country=United%20States`).
- **Negative cases:**
  - On Edit page for an existing cert, saved country still shows in dropdown (no orphan values).
  - Public tile view filter sidebar — all 195 filterable.
  - **Partner API `certificationCountries`** — separate concern; this returns "All" per DEF-097, NOT the full list. Confirm cross-rule.
- **Cross-role:** Analyst (Create/Edit), Admin (Country reference data if exists), Public (filter).
- **Status:** NOT YET RUN. **Pre-check: query DB `SELECT COUNT(*) FROM Countries`. Expected ~195 (or ~249 with territories — confirm with client which standard).**

### TC-QC-135 — Cert hidden on its expiration day (timezone bug)

- **Source:** QC #135, client. Sprint 2: Ready for UAT.
- **What we changed:** Created `CertificateStatusHelper.BusinessToday` (US Eastern Time). Replaced UTC-based comparisons across 9 files. The previous `AddDays(-1)` workaround overshot by ~14 hours.
- **Baseline (existing draft, accepted):**
  1. Log in as analyst. Edit a cert with status "Client Approved - Paid"; set Expiration Date to TODAY (US Eastern). Save.
  2. `/Certificate/CertificateTileView` (public, anonymous) — cert IS visible today.
  3. Day after expiration → cert NOT visible on public view.
  4. Confirm comparison uses US Eastern, not UTC (no premature dropoff after 8pm EDT).
- **Edge cases:**
  - **Test at 9pm EDT specifically** — UTC has rolled to next day, but business date is still today. Cert must be visible.
  - Cert expiring at midnight Eastern (12:00 am) — visible through that calendar day.
  - Daylight Saving transition day (March / November) — `BusinessToday` correct on both sides.
  - User's browser in PST → cert visibility driven by server's Eastern clock, not browser.
- **Negative cases:**
  - Browser local timezone shifted to UTC (DevTools → Sensors → Timezone) → cert visibility unaffected (server-side rule).
  - Cert expiring tomorrow Eastern — visible today.
  - Cert expired 5 days ago — not visible.
  - Auto-expiration background service flips status to Expired on the calendar day AFTER expiration, not on it.
- **Cross-role:** Public, External, Internal — all surfaces. Internal sees Expired too; public/external must hide.
- **Status:** NOT YET RUN. **Tip: use `Set-TimeZone "Pacific Standard Time"` on a test box to simulate non-Eastern user.**

### TC-QC-136 — Date display off-by-one in public/external/internal views

- **Source:** QC #136, client. Sprint 2: Ready for UAT.
- **What we changed:** Public view already had `parseLocalDate()`; external + internal tile views were missing it. Added across `certificate-tile-view-external.js`, `certificate-tile-view-internal.js`, server-side `CertificateController.cs`. Standardized expiration check.
- **Baseline (existing draft, strengthened):**
  1. Create new cert with **Effective = 4/4/2026, Expiration = 4/4/2027**. Save.
  2. `/Certificate/CertificateTileView` (public) — find cert.
  3. Detail view — Effective shows **4/4/2026**, Expiration shows **4/4/2027** (NOT 4/3 anywhere).
  4. Repeat on internal listing tile + table mode, external listing tile + table mode.
  5. CSV export — dates match.
  6. Cert visible on expiration day; drops off the next.
- **Edge cases:**
  - Different entry formats (4/4/2026 vs 04/04/2026 vs 2026-04-04) — all display consistently.
  - Year boundary: cert with effective 12/31/2025, expiration 12/31/2026 — neither shifts to 12/30.
  - Leap year: cert with effective 2/29/2024 — displays 2/29/2024.
  - Browser timezone: UTC, EST, PST, JST, AEDT — date displays the SAME on all.
- **Negative cases:**
  - Browser timezone Tokyo (JST, UTC+9) via DevTools Sensors → date still shows as entered.
  - Browser timezone Honolulu (HST, UTC-10) → date still shows as entered.
  - System clock manipulated forward 24 hours (browser-side only) → date display unchanged.
- **Cross-role / Cross-viewport:** All 4 cert views (public single, partner external, internal admin, list mode in each). Desktop + mobile.
- **Status:** NOT YET RUN. **Pair with QC #135 — run together.**

### TC-QC-143 — Partner Role (CR-008, approved 2026-04-29)

- **Source:** QC #143, client. Scoped as **CR-008**, approved 2026-04-29 alongside the rest of the CR bundle.
- **What's needed:** New `Partner` role type, partner-scoped data filtering, admin UI to assign Partner = Amazon (or Novi, Other) to a user, auth claim or middleware that scopes API responses to assigned partner. Pairs with CR-013 (partner-scoping mechanism is shared).
- **Baseline test (post-implementation):**
  1. Log in as admin. Open `/Admin/UserManagement`. Create new user with role = `Partner`, assigned partner = Amazon.
  2. Confirm role appears in the role dropdown alongside Admin / Analyst / External.
  3. Sign in as that user (or impersonate). Open `/Certificate/CertificateTileViewExternal`.
  4. Confirm only Amazon-tagged certs appear in the listing.
  5. Confirm cert detail pages for non-Amazon certs return 403 / not found when accessed by direct URL.
- **Edge cases:**
  - User assigned to multiple partners (Amazon + Novi) — sees union.
  - User assigned to Partner with zero certs — empty state.
  - Existing External-role users — migration plan needed (auto-assign to Amazon? Or admin reassigns manually?).
- **Negative cases:**
  - Partner-role user attempts admin action → 403.
  - Partner-role user calls Partner API with X-API-Key intended for a DIFFERENT partner → 401/403.
  - Partner-role user attempts to view internal tagging UI → blocked.
  - Partner-role assigned to no partner → safe default (sees nothing, not everything).
- **Cross-role:** Admin (assigns), Partner (uses), Analyst (unaffected), External (the role being deprecated/migrated FROM).
- **Status:** **NOT YET RUN — implementation in progress per CR-008.** Update card and run once delivered.

---

## Section C-2 — Other Open items (lighter cards)

These are items 50, 51, 60, 104, 123-134 (excl 135/136), 137-142, 144-149 (excl 143), 152-163 — Open without High priority. Stubs in Section E.1; flagging here the ones with known risk:

- **QC-050 / QC-051** — DEFECT/DATA Open: certificate types data + cert type dropdown. Linked to QC #45 (cert type SQL migration). If migration didn't run on prod, these all FAIL together.
- **QC-104** — DEFECT Open: per memory 2026-03-24, breadcrumb fixes shipped. Verify breadcrumb on every admin page is correct (not "#" links).
- **QC-148** — CR Open: Excel template hint headers. Confirm every admin page's CSV/Excel template has parenthetical hints (e.g. "Status (Active, Inactive)").
- **QC-150 / QC-151** — Forgot Password / Duo MFA. **CR-009 / CR-010 approved 2026-04-29.** Test cards to be authored once implementation is in flight (need B2C plan tier confirmation first to know what's actually being built).
- **QC-158** — ASIN scale UX. **CR-011 approved 2026-04-29.** Test card to be authored once design lands (paginated vs collapsible vs lazy-load is a design decision, not yet made).

For all other unprioritized Opens, the stub card in Section E.1 stands. Run them with the same 6-layer framework after High-priority items pass.

---

## Section D — Items already marked Complete (regression-only re-test)

For items 1-119 marked Complete by Anuki and verified by the client, test cards are short:
- **Regression check only** — confirm original fix still works after subsequent commits.
- **Run as part of a sweep** — bulk-run via Playwright at the end of the cycle.
- Full stub cards in Section E.2 below (114 items).

The recommended sweep approach:
1. Run existing Playwright suites (`UAT`, `RoleBasedUI`, `MobileUITests`) against `localhost:5101` or `gccwebapp.com`. Most Section D items have at least one assertion in those suites already.
2. For items with no Playwright coverage, do a manual smoke check (load the page, exercise the original repro, confirm pass).
3. Track each Section E.2 stub PASS/FAIL after the sweep — open a new DEF-XXX for any FAIL.

---

## How we run this

1. **Tonight (2026-04-29):** Author Section A (DEF-097 through DEF-102) and Section B (high-priority Ready-for-UAT). DONE for the listed items above.
2. **Next session:** Continue Section C high-priority opens.
3. **After that:** Sweep Section D (regression check on closed items) — likely a Playwright-driven script, not manual.
4. **Run the actual tests:** Section A first (closest to deploy), then B, then C. Mark each test PASS/FAIL with screenshot evidence in `docs/tracking/screenshots/test-execution/`.
5. **Surface findings:** Any FAIL becomes a new DEF-XXX in `defect-register.md`. New defects discovered during edge/negative testing get logged before the client sees them.

## Notes for self

- Don't pad. If a test step is one line, it's one line. Don't write "verify the page loads correctly" when "page loads in <2s on cold cache" is the actual check.
- Run on `localhost:5101` first (per Shawn's testing-environment rule). Live verification only after local pass.
- Use Playwright with full-page screenshots at 5 viewports per `feedback_playwright_screenshots.md`.
- Don't open new defects against memory rules that say "FIXED, do not flip back" (e.g. import/export icons).

---

## Section E - Full Stubbed Coverage (all 163 items, generated from QC log)

Every item from Database QC Log_4.27.26_sw.xlsx gets a card here. Items already covered in Sections A-B-C-D in this file have their detailed cards above; the version here is a stub for completeness. The stub carries:
- Defect text from column J of the QC log
- Existing draft test steps (column F) where present
- A standard edge/negative/cross-role/cross-viewport block to fill in during execution

Items are grouped by Anuki status: Open first (need fix or clarification), then Ready for UAT (Sprint 2 fix waiting for client verification), then Complete (regression sweep).

### E.1 - Open items (Anuki not yet marked complete)

Count: 46

### TC-QC-032 [DATA / Pri: High / Status: Open]
- **Page:** Public view + Admin
- **Raised by:** client
- **Sprint 2:** none
- **Last test result:** Need Clarification
- **Defect:** Under Product Categories filter Packaging/Paper/Plastic should not be under Chemicals and Raw Materials -- Packaging is it's own section

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-050 [DEFECT / Pri: Low / Status: Open]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** Dev/SIT Testing
- **Defect:** I don’t think we will be creating certs from mobile view so this is low priority but text/boxes get distorted and it's not functional
- **Fix summary (4/10 SP2 comment):** |   Create + Edit pages mobile fixes: |                                                                           ●  1. Preview contanger overflow — Replaced inline width: 350px; height: |   350px with CSS class .certificate-create-preview using max-width: 350px;   aspect-ratio: 8.5/11. On mobile it scales to max-width: 100% so it fits ─ |   the screen.                                                               2. Font sizes below 16px minimum — Removed inline font-size: 13px, 11px, |   12px on sidebar help text, preview placeholder text, and PDF upload hint.   All now use 16px via CSS classes. |   3. Sidebar section headers — Replaced inline flex styles with reusable |   .sidebar-section-header class. |   4. Button font size — Fixed admin-btn from 14px to 16px on mobile |   breakpoints. |   5. Both Create.cshtml and Edit.cshtml updated consistently. |  |   The layout already stacks to single column at 1024px (sidebar below form) |    and single-column form fields at 768px. Want to run locally and test?

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create on mobile viewport (390x844) | 3. Verify: certificate preview box scales to fit screen width (does NOT overflow horizontally) | 4. Verify: all field labels and helper text render at 16px or larger (no tiny text) | 5. Verify: form fields stack to single column (not crammed side-by-side) | 6. Try entering data in 3-4 fields and uploading a PDF — interaction must work | 7. Repeat on /Certificate/Edit/{id} for an existing cert |  | PASS: Create + Edit pages are usable on mobile — no horizontal overflow, no <16px text, fields stackable, all interactions functional.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-051 [DATA / Pri: - / Status: Open]
- **Page:** Global
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Last test result:** Need clarification
- **Defect:** Why are there random characters in name -- was it inputted this way or another issue? Anuki to confirm |  | 4/3 Reply: Some will have TM or registered trademark symbols. One client is called Kärcher (Latin small letter A with diaeresis/umlaut), use of ampersand (&), etc.
- **Fix summary (4/10 SP2 comment):** QC #: 51 |  ⏵Defect: Random characters in names |   Fix Summary: Not a code defect — confirmed as legitimate Unicode (™, ®, ä, &). Fixed UTF-8       encoding gap in 4 CSV import StreamReaders (CertificateImportService, ProductController, ProductIdService, AdminController) to prevent future corruption.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Browse cert tiles and look for company/product names with special characters: ™, ®, ä, ü, &, etc. | 3. They should render as the actual symbol (e.g. "Kärcher", "Sika®", "HID Signo™"), NOT as garbled characters (â€¢, Ã¤, etc.) | 4. Log in as analyst, navigate to /Admin/UserManagement, click Import on a CSV with UTF-8 encoded special characters in a name field | 5. After import, verify the name imports cleanly with the special character intact (not corrupted) |  | PASS: All special characters in existing names display correctly; CSV import preserves UTF-8 encoding without corruption.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-056 [WAD / Pri: High / Status: Open]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** All countries need to be available in the list. There are only 5 listed

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-059 [DEFECT / Pri: High / Status: Open]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Last test result:** Development in Progress
- **Defect:** Similar to existing DB when you start typing Company Name or other field it should reduce available options -- otherwise the list will become massively long to scroll through
- **Fix summary (4/10 SP2 comment):** QC #: 59 |   Defect: Type-ahead autocomplete on cert create/edit dropdowns |   Fix Summary: Added search/filter input to Company and Product custom dropdowns in |     certificate-inline-create.js. Added SearchableSelect to Certification Type and Product |     Category on both Create and Edit pages. Country already had it. All 5 key dropdowns now have  type-ahead.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create | 3. Company dropdown — click to open, type a few letters (e.g. "amaz"). The list narrows to matching companies. Select one. | 4. Product dropdown — click to open (after a Company is selected), type a few letters from a known product. The list narrows. Select one. | 5. Country dropdown — click to open, type "uni". List narrows to United States, United Kingdom, etc. Select one. | 6. Certification Type dropdown — click to open, type the first 2-3 letters of a known cert type (e.g. "rec" for Recycled Content). List narrows. Select one. | 7. Product Category dropdown — click to open, type a few letters (e.g. "chem"). List narrows. Select one. | 8. Repeat steps 3-7 on /Certificate/Edit/{id} for an existing cert — same five dropdowns should also have a type-ahead search box at the top. |  | PASS criteria: each of the 5 dropdowns shows a search input at the top, narrows options as you type, and lets you select a filtered result without scrolling the full list.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-060 [DEFECT / Pri: - / Status: Open]
- **Page:** Create cert page - post creation
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Last test result:** Testing ongoing
- **Defect:** Cert image getting cut off. Got to this page after creating cert. The buttons are also not aligned correctly
- **Fix summary (4/10 SP2 comment):** QC #60 is marked as fixed from our previous CSS normalization work — the certificate preview boxes |   now use aspect-ratio: 8.5/11 with object-fit: contain instead of the old square 350px container. The |   button alignment was also fixed (right-aligned with justify-content: flex-end).

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create | 3. Fill required fields (Company, Product, Cert Type, dates, etc.) and upload a PDF | 4. Click Save — you'll land on the post-creation cert detail page | 5. Verify: the certificate preview image displays the full uploaded PDF (not cut off — uses 8.5x11 aspect ratio) | 6. Verify: action buttons (Download, Edit, Delete) are right-aligned and clearly readable | 7. Repeat at tablet (1024) and mobile (390) viewports |  | PASS: Cert image displays in full without cropping, buttons are aligned correctly across desktop/tablet/mobile.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-104 [DEFECT / Pri: - / Status: Open]
- **Page:** Login - Breadcrumbs
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Last test result:** review
- **Defect:** Companies & Products and Products in breadcrumbs are either not going anywhere or going to error page. I don't think they are actual different pages so they just should not be hyperlinked but Anuki confirm |  | Same with the Administration breadcrumb under admin |  | 4/3: Somewhat fixed. Now there is Administration that is clickable in breadcrumbs, it goes to a dashboard page only accessible this way. This page should be accessible as a Dashboard listed option in dropdown since you can't get to it any other way
- **Fix summary (4/10 SP2 comment):** QC #104 is verified as fixed. Here's the review summary: |  |   What was fixed (confirmed across all 24 breadcrumb views):                                  ─ |                                                                                               ─  1. No href="#" in breadcrumbs — zero instances found in any breadcrumb section. The remaining   href="#" hits are all in non-breadcrumb elements (clear-filters buttons, sort options, 2FA |   links, nav dropdowns) which are correct/expected usage. |   2. "Administration" links to Admin Dashboard — all Admin views use <a |   href="@Url.Action("Index", "Admin")">Administration</a> which resolves to a real page. |   3. Category labels are non-clickable <span>s — "Data Management", "API Management", |   "Companies & Products" are all rendered as plain <span> elements, not links. |   4. Current page is non-clickable — all views use <span class="current">Page Name</span> for |   the final breadcrumb item. |   5. Certificate Create/Edit have proper navigation chains: Home -> Administration -> |   Certificate Management (links to internal tile view) -> current page. |  |   No issues found. QC #104 can be marked Fixed/Closed.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate through these pages and inspect breadcrumbs: |    - /Admin (Administration → clickable, lands on Admin Dashboard) |    - /Admin/UserManagement (Administration → Data Management [non-clickable span] → User Management [non-clickable]) |    - /Product/Management (Home → Companies & Products [non-clickable span] → Product Management [non-clickable]) |    - /Certificate/Create (Home → Administration → Certificate Management [links to internal tile view] → Create) | 3. Click each clickable breadcrumb item — they should land on real pages (no 404) | 4. Hover over non-clickable items — should NOT show a pointer cursor or be styled as a link |  | PASS: All breadcrumb hyperlinks resolve to real pages; category labels and current-page items render as non-clickable spans.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-123 [DATA / Pri: - / Status: Open]
- **Page:** public view
- **Raised by:** Alice
- **Sprint 2:** Ready for UAT
- **Defect:** Certificate #23-0375 is showing special characters, this should not be the case
- **Fix summary (4/10 SP2 comment):** What's stored in the database (Product Id 62) is the corrupted version where the ™ (trademark) symbol got split |     into three garbage characters (â€¢ / Unicode 226, 8222, 162). |  |     The SQL fix script being created will correct this to the proper ™ character. Same pattern applies to the ® |     (registered) symbol on the other 4 affected products (GSB-88®, Sika® Injection-310 US, HID Signo™ readers, M3 |     CLIQ® mortise cylinder).

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Find Certificate #23-0375 and open its detail page | 3. Verify the product name renders the ™ symbol correctly (NOT garbled characters like â€¢ or three random chars) | 4. Check the other 4 affected products (GSB-88®, Sika® Injection-310 US, HID Signo™ readers) for proper ® and ™ rendering | 5. (Pre-deploy: SQL fix script must be run against production to repair the corrupted database values) |  | PASS: All 5 affected products display proper ™ / ® symbols on public view; no garbled character sequences remain.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-124 [WAD / Pri: - / Status: Open]
- **Page:** public view
- **Raised by:** Alice
- **Sprint 2:** Ready for UAT
- **Defect:** Missing Certified Water Savings, LCA Opt, RM, WD as a certification type |  | CP NOTE: Anuki please confirm is it only showing certs with active listing for sorting? Is that why they aren't there?
- **Fix summary (4/10 SP2 comment):** Issue observed. The public view filter only shows certification types that have at least one active, non-expired certificate (Client Approved - Paid or Republished). If those types aren't showing, their certificates are either in a non-public status or expired |  | fix: Certification types filter now shows all configured types from the admin page, |   not just those with active certificates. "Certified Water Savings", "LCA Opt", "RM", "WD" |   will all appear if they exist in Admin > Certificate Types. |  | client to confirm functionality is accurate?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-127 [DEFECT / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** For the filter boxes that appear at top (see image). I don't think you need identifier (Type/Company/etc). |  | + | Also when I select any Company it's giving a number not actual company name (for example Amazon = 227). The companies are not appearing correctly
- **Fix summary (4/10 SP2 comment):** 1. Company filter showing numbers — Checkbox value changed from company.id to company.name.    The backend already handle scompagytnam s (fasls through to CompanyNamesnfiltrs path). Chips |   now show "Amazon" instead of "227". |   2. Type labels on filter chips — Removed the <span class="filter-tag-label"> prefix ("Type:",   "Company:", "Search:", etc.) from all filter chips. Chips now show just the selected value |   with an X to remove.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Apply a Company filter (e.g. "Amazon") via the sidebar | 3. Look at the active filter chip at the top of the results | 4. Chip should display "Amazon" (the company NAME) — NOT "227" or any numeric ID | 5. Chip should show only the value — NO label prefix like "Type:", "Company:", "Search:" | 6. Repeat with a Type and a Search term — chips show only the values, no label prefixes |  | PASS: Filter chips show human-readable values (company name, not ID) and have no "Type:"/"Company:"/"Search:" prefixes.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-128 [DEFECT / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** When I select LEED under filtering it's showing certs that don't have the LEED tag  (it's not filtering out any certs therefore not working). Same for other tag, not working
- **Fix summary (4/10 SP2 comment):** e |   - Root cause: JS sent ?additional=LEED but the deployed API on gccwebapp.com only |   recognizes the ?tags= parameter                                                             - Verified on gccwebapp.com: ?additional=LEED → 18 results (broken), ?tags=LEED → 1 result   (works)                                                                                    - Fix: certificate-tile-view.js now sends additional filters through the tags parameter     - Cache buster updated to v=20260415

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Note the total certificate count | 3. Open Additional filter section, tick LEED — list refreshes; only certs with LEED tag remain (count drops) | 4. Browse 2-3 results and confirm each actually has the LEED tag visible on the tile | 5. Untick LEED, tick APR — only APR-tagged certs remain | 6. Verify network request: filter sends `?tags=LEED` (NOT `?additional=LEED`) |  | PASS: LEED and APR filters actually filter the result set; only certs with the selected tag appear.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-129 [DEFECT / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Search bar shows more results  when typing then are available on public DB. There is only 1 Amazon tile and the company name is Amazon.  |  | It looks like it's adding Company, Product, etc. to the end of the name which I don't think is needed.  | Also, why is it showing Amazon Devices Company in search dropdown when that is not active cert I can see on public side. Public DB should only show what is actively available on it to prevent confusion. |  | When I type Amazon as a search term its showing 2, 1 is right and the other is Dude Wipes which is not accurate.
- **Fix summary (4/10 SP2 comment):** QC #129 (Search autocomplete showing more results than available) — Root cause: autocomplete |   endpoint queried ALL active companies/products regardless of public certificate status. |   "Amazon Devices Company" and "Dude Wipes" appeared because they were active entities but had |   no public-visible certs. Fix: Changed all four autocomplete searches (companies, products, |   cert numbers, cert types) to query through the same baseQuery filter used by the tile view |   (PublicStatuses + non-expired). Only entities linked to visible public certificates now |   appear in suggestions. Also removed type labels ("Company", "Product", "Certification Type") |   from dropdown items per client's feedback. Files: CertificateTilesApiController.cs, |   certificate-tile-view.js.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Click the search bar, type "amaz" slowly | 3. Autocomplete should show ONLY entities (companies, products, cert types, cert numbers) that have a publicly visible cert | 4. Specifically: "Amazon Devices Company" should NOT appear (active company but no public cert) | 5. "Dude Wipes" should NOT appear (active but no public cert) | 6. Suggestions should NOT have "Company"/"Product"/"Type" suffixes appended to entity names |  | PASS: Autocomplete only suggests entities with at least one public cert; suggestion labels are clean (no type suffix).

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-130 [WAD / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** You can remove Product Type A-Z Sort By option because the product type does not show on the tile
- **Fix summary (4/10 SP2 comment):** Removed Product Type A-Z sort option from CertificateTileView.cshtml sort dropdown.   File: CertificateTileView.cshtml.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Click the "Sort By" dropdown | 3. Verify "Product Type A-Z" is NOT in the list (was removed because product type doesn't show on the tile) | 4. Other sort options (Default, Cert Number, Company A-Z, etc.) remain functional |  | PASS: Product Type A-Z sort option is absent from the dropdown; remaining sort options still work.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-131 [WAD / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Per conversation on 4/2 call please make it that product name shows up to 3 lines (4 max) on tile and remove product subcategory (eg, Home Goods). Show .... when it hits limit |  | Would also remove 1 line from the company name in the tile with ... for longer ones |  | Trying to get the tile sizes a little smaller to reduce some of the open white space
- **Fix summary (4/10 SP2 comment):** Product subcategory removed from all three tile views (public, internal, external). The   product name  and company name  CSS applies across all of them as well.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Find tiles with very long product names (multi-line) | 3. Product name should clamp to 3 lines (max 4) with "..." truncation | 4. Find tiles with very long company names — clamps to 1 line with "..." | 5. Verify product subcategory (e.g. "Home Goods") is NOT shown on the tile | 6. Repeat on internal (/Certificate/CertificateTileViewInternal) and external (/Certificate/CertificateTileViewExternal) tile views — same clamping behavior |  | PASS: Product name 3-4 line clamp, company name 1-line clamp, subcategory hidden — across all three tile views.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-132 [DEFECT / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Dev
- **Defect:** Not all countries are showing for active certs under filter. For example Germany and Canada are missing |  | You mentioned on the 4/2 call it would only show ones for active certs but it's not functioning this way
- **Fix summary (4/10 SP2 comment):** QC #132 fix: United States now sorts to the top of the country filter list, with remaining |   countries in A-Z order. The filter itself is working as designed — only showing countries      that have active (Client Approved - Paid / Republished), non-expired certificates.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Open the Country filter list | 3. Verify "United States" sorts to the top (priority pinned) | 4. Remaining countries sort A-Z below it | 5. Verify only countries that have at least one ACTIVE (Client Approved - Paid / Republished, non-expired) cert appear in the list | 6. (Note: Germany / Canada absence is expected if no active certs exist for those countries) |  | PASS: United States pinned at top; other countries A-Z; only countries with active certs are listed.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-133 [WAD / Pri: - / Status: Open]
- **Page:** Cert creation
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Do not prepopulate effective and expiration dates. This could cause issues when adding new certs
- **Fix summary (4/10 SP2 comment):** #133 fix complete. Here's what changed:                                                                                                                                                                                           ─ |  |  ⏵1. CertificateEditViewModel.cs — Changed EffectiveDate and ExpirationDate from DateTime to DateTime? (nullable) so the form can render with empty date fields |   2. CertificateController.cs — Removed DateTime.Today and DateTime.Today.AddYears(2) defaults from both Create() and Create2() GET actions. Used .Value on POST actions since [Required] validation ensures values are present before    saving |   3. CertificateService.cs — Used .Value when mapping from the nullable ViewModel to the non-nullable entity model (Create and Update methods) |  |   The date fields will now show blank when creating a new certificate — the analyst must manually enter both dates. Both fields are still required, so the form won't submit without them.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create | 3. Verify Effective Date field is EMPTY by default (no pre-populated value) | 4. Verify Expiration Date field is EMPTY by default | 5. Submit the form WITHOUT entering dates — get a validation message that dates are required (or whichever is applicable) | 6. Edit an existing cert — Effective and Expiration dates show the saved values (not blank) |  | PASS: Create form opens with both date fields empty; existing cert edit still shows saved dates.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-134 [DEFECT / Pri: - / Status: Open]
- **Page:** Login
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** I created a new cert 26-0006. It's showing twice under the Analyst login (sometimes but not all the time but def upon first creation)
- **Fix summary (4/10 SP2 comment):** QC #134 (Certificate showing twice under Analyst login) — Root cause: race condition between |   initial page load and infinite scroll observer. loadInitialCertificates() was not awaited |   before setupInfiniteScroll() was called. If the page had few certs, the scroll sentinel was |   already visible when the observer was set up, triggering a second load (page 2) while page 1 |   was still rendering — or immediately after, before sort order stabilized for newly created |   certs. Fix applied to all three tile views: |  |   1. await initial load before setting up infinite scroll — prevents the observer from firing |   during the first load |   2. Deduplicate by cert ID on append — if a cert appears in both page 1 and page 2 (possible |   with newly created certs shifting pagination boundaries), duplicates are filtered out |                                                                                                 Files: certificate-tile-view-internal.js, certificate-tile-view.js, |   certificate-tile-view-external.js.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Create a new test certificate (e.g. cert number 26-XXXX) — save | 3. Navigate to /Certificate/CertificateTileViewInternal | 4. Verify the new cert appears EXACTLY ONCE in the list (not duplicated) | 5. Scroll to bottom — infinite scroll continues to load older certs | 6. Refresh the page — cert still appears once | 7. Repeat 2-3 times with different new certs |  | PASS: Newly-created certs appear once and only once on the internal listing — no duplicates from infinite-scroll race condition.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-135 [DEFECT / Pri: High / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** I updated Test Company 3.9 to have an expiration date of the day I was testing and it would not show on public DB (even with status as Client Approved - Paid),  | The cert should still show on the day it expires just not after that date
- **Fix summary (4/10 SP2 comment):** Issue: Certificates with an expiration date of "today" were not showing on the public |   database, even with status "Client Approved - Paid." The cert should remain visible on its |   expiration day, just not after. |  |   Root Cause: All expiration date comparisons used DateTime.UtcNow.Date, which is 4-5 hours |   ahead of US Eastern Time. After ~8pm EDT, UTC rolls to the next calendar day, causing certs |   to disappear while still valid on the business day. A previous AddDays(-1) workaround was |   inconsistent and overshot by ~14 hours. |  |   Fix: Created CertificateStatusHelper.BusinessToday — returns today's date in US Eastern Time |   (client's business timezone). Replaced all scattered UTC-based expiration comparisons across 9 |   files with this single property. Expiration dates are now evaluated as business concepts in |   the business timezone, not UTC.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Edit any cert with status "Client Approved - Paid"; set Expiration Date to TODAY's date. Save. | 3. Open /Certificate/CertificateTileView (public, anonymous) and verify the cert IS visible today | 4. Wait for the next calendar day (or simulate by setting Expiration to yesterday) | 5. Verify the cert is NO LONGER visible on public view the day AFTER expiration | 6. Confirm comparison uses local (US Eastern) date, not UTC (no premature dropoff after 8pm EDT) |  | PASS: Cert remains visible on its expiration day, drops off the next day; date logic uses local timezone, not UTC.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-136 [DEFECT / Pri: High / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** The system seems to be changing the eff/exp dates by 1 day in public view. I put 4/4 and in public view it says 4/3. Same for eff date. THIS CANNOT HAPPEN THEY NEED TO BE ACCURATE. See image. |  | Also, the DB should show the cert through the expiration date. And the day after it gets removed and changed to 'Expired' status. This is not happening now, it's removing the cert if it expired 4/3 when it should be expiring/not showing come 4/4
- **Fix summary (4/10 SP2 comment):** Issue: |   Effective and Expiration dates displayed one day earlier than entered across all |   public-facing certificate views. For example, a certificate with an expiration date of 4/4 |    would display as 4/3. This also affected when certificates were removed from public view |   — they were disappearing on the expiration date instead of remaining visible through that |   day and expiring the following day. |  |   Root Cause: |   A timezone conversion bug in the browser. The system stores dates without a time component |    (e.g., "2026-04-04"). When the browser interprets this, it assumes midnight UTC. For |   users in US timezones (4–10 hours behind UTC), this rolls the displayed date back one day. |    The public tile view had a fix for this, but the external and internal tile views did |   not. |  |   A secondary issue was found in the server-side expiration check that could also cause |   premature expiration when the server's UTC clock was ahead of the user's local timezone. |  |   What Was Fixed: |   - External certificate tile view — dates now display correctly |   - Internal certificate tile view — dates now display correctly |   - Server-side expiration checks — standardized to keep certificates visible through their |   full expiration day across all US timezones |   - Certificate detail page (direct URL access) — same timezone buffer applied |  |   How to Verify: |   1. Open the public certificate tile view |   2. Find a certificate with a known effective or expiration date |   3. Confirm the dates shown match exactly what was entered — no off-by-one shift |   4. On a certificate's expiration date, confirm it is still visible in the public view |   5. The day after the expiration date, confirm it is no longer visible |  |   Files Changed: |   - certificate-tile-view-external.js |   - certificate-tile-view-internal.js |   - CertificateController.cs

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Create a new cert with Effective Date = 4/4/2026, Expiration Date = 4/4/2027. Save. | 3. Open /Certificate/CertificateTileView (public) — find the cert | 4. Click into detail view — Effective shows 4/4/2026, Expiration shows 4/4/2027 (NOT 4/3/2026 / 4/3/2027) | 5. Repeat on internal listing, external listing, and CSV export — same dates everywhere | 6. Cert remains visible on the expiration date itself; drops off the next day |  | PASS: Dates display exactly as entered across every public-facing surface; no off-by-one shift; cert visible through expiration day.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-137 [COSMETIC / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Search icon overlapping box border
- **Fix summary (4/10 SP2 comment):** Search icon overlapping box border |                                                                                                  - wwwroot/css/main.css: Moved search icon from left: 15px to 12px, set explicit font-size:     14px, added pointer-events: none. Reduced input left padding from 40px to 36px to tighten the   gap.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Locate the search bar near the top | 3. Magnifying glass icon should sit cleanly INSIDE the search input box — NOT touching or overlapping the box border | 4. Cursor should NOT change to pointer over the icon (icon is non-interactive) | 5. Click into the search bar and type — text starts to the right of the icon with proper spacing |  | PASS: Search icon is positioned inside the box with clear margin from the border; not click-blocking; input padding leaves room for typed text.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-138 [COSMETIC / Pri: - / Status: Open]
- **Page:** Public tile view - mobile
- **Raised by:** client
- **Sprint 2:** Dev
- **Defect:** Give more spacing below external tag. See image
- **Fix summary (4/10 SP2 comment):** Mobile spacing below external tag |  |   - wwwroot/css/certificateTileViewExternal.css: Added mobile media query rule for |   .certificate-card .certificate-tags with margin-bottom: 20px and padding-bottom: 14px.

**Baseline test:**

1. Open /Certificate/CertificateTileViewExternal (external user, log in as external / TestPass123!) on mobile viewport (390x844) | 2. Find a tile with the External tag (small label on the card) | 3. Verify clear vertical spacing (~20px margin-bottom + 14px padding) below the External tag — it doesn't touch the bottom of the card | 4. Repeat at iPhone SE (375x667) |  | PASS: External tag has visible breathing room (>=20px) below it on mobile, doesn't crowd the card bottom edge.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-139 [WAD / Pri: - / Status: Open]
- **Page:** Cert creation
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Remove word (Optional) after  | Pathway (for Amazon CPF applicable certs) |  | It is optional but not if it’s a pathway that requires so just remove word optional to avoid confusion
- **Fix summary (4/10 SP2 comment):** Remove "(Optional)" from Pathway field |  |   - Views/Certificate/Create.cshtml: Removed <span class="text-muted">(Optional)</span> from |   Pathway label. |   - Views/Certificate/Edit.cshtml: Same removal on the Edit form.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create | 3. Locate the Pathway field label | 4. Confirm the label reads simply "Pathway" — NO trailing "(Optional)" text | 5. Repeat on /Certificate/Edit/{id} |  | PASS: Pathway label is plain "Pathway" on both Create and Edit forms — no "(Optional)" suffix.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-140 [WAD / Pri: - / Status: Open]
- **Page:** Login - list view
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Reduce the font size of the Status text |  | Gloabl wrap text when it doesn't fit in a column (right now text is cut off and can't read) | Global condense the height of the cells  |  | Middle align the icons in Actions column
- **Fix summary (4/10 SP2 comment):** List view styling (status font, text wrap, cell height, actions) |  |   - wwwroot/css/certificatetileview.css: |     - Table body cells: Changed white-space: nowrap to normal with word-wrap: break-word so |   text wraps instead of being cut off |     - Reduced cell padding from 6px 10px to 4px 8px and min-height from 32px to 28px |   (condensed) |     - Reduced header padding from 8px 10px to 6px 8px |     - Reduced status badge font from 16px to 12px in grid view, 12px to 11px in table body

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal — switch to List view | 3. Verify Status column text is at REDUCED font size (smaller than before — fits better in narrow columns) | 4. Verify cell text WRAPS when too long (no horizontal cutoff with "...") | 5. Verify cells have CONDENSED row height (less vertical padding than before) | 6. Verify Actions column icons are vertically MIDDLE-aligned within the cell |  | PASS: Status font reduced, text wraps in all cells, row height condensed, Action icons middle-aligned.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-141 [DEFECT / Pri: - / Status: Open]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** The export for # of product categories, # product types, subcategories don't have that column/info and need to |  | # certs by status export only shows list of Client Approved - Paid and Republished but should show all  |  | Top 10 product types and Top 5 product categories don't have these fields in export
- **Fix summary (4/10 SP2 comment):** Dashboard export issues |  |   - wwwroot/js/dashboard.js: Changed stat download URL from /Export/DownloadStat to |   /Dashboard/DownloadStat so product category/type/subcategory exports use the detailed |   breakdown method. |   - Controllers/DashboardController.cs: |     - GetTableData("cert-status"): Now returns all 8 statuses (Draft, Client Approved - Not |   Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished, |   Expired - No Annual Verification) instead of just Active/Expired. |     - GetTableData("product-types") and GetTableData("product-categories"): Now return summary |   data (Name + Count) matching the on-screen tables instead of raw certificate lists. |     - GenerateTableCsv: Rewritten to output proper summary CSV (Name, Count) with |   context-specific headers instead of full certificate details.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Dashboard | 3. For "# Certs by Status" widget — click Download. CSV should include ALL 8 statuses (Draft, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Republished, Hold, Expired, plus any others) — not just Paid + Republished | 4. For "# Product Categories with Active Certs" — Download. CSV should be a detailed breakdown (each category name + its active cert count), not a single number | 5. Same for "# Product Types" and "# Product Subcategories" — each downloads detail rows with names + counts | 6. For "Top 10 Product Types" and "Top 5 Product Categories" — Download. CSV includes the relevant detail columns |  | PASS: All Dashboard widget exports include the detailed breakdown data (names + counts) and all status types — not summary-only.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-142 [DEFECT / Pri: - / Status: Open]
- **Page:** Product Managerment - deleting product popup
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Button text missing. See image 40
- **Fix summary (4/10 SP2 comment):** Delete product popup missing button text |  |   - wwwroot/js/admin/admin-core.js: Added explicit inline style="color:#ffffff;font-size:16px;" |    and type="button" to confirmation modal buttons to prevent CSS overrides from hiding button |   text.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Product/Management | 3. Click the delete (trash) icon on any product | 4. Confirmation modal opens — verify both "Delete" and "Cancel" (or similar) buttons display VISIBLE WHITE TEXT (not blank/missing labels) | 5. Buttons must be clickable — type="button" set so they don't accidentally submit forms | 6. Repeat on other admin pages with delete confirmations (Users, Certificate Types, Tags, etc.) |  | PASS: Confirmation modal buttons always show their text labels in white at 16px; no blank buttons.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-143 [NEW / Pri: High / Status: Open]
- **Page:** Admin - Role/User (for Partner)
- **Raised by:** client
- **Sprint 2:** Document, and scope functrionality for clarity and send to client for review
- **Defect:** Partner Role type is now missing. How would we create one of these for Amazon and then users so they can only see Partner = Amazon certs?
- **Fix summary (4/10 SP2 comment):** #143 — Partner Role type missing: |   The system doesn't have a "Partner" role — it has Admin, Analyst, External. Partners (Amazon, Novi, |   Other) are entities, not roles. To scope API users to only see Amazon certs, we'd need a new feature: |    either a Partner role type with partner-based data filtering, or API key scoping by partner. |  | Fix: Add Partner to user managemnet that will be setup by admin on account creation.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-144 [COSMETIC / Pri: - / Status: Open]
- **Page:** Admin - New User popup
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Showing double arrows
- **Fix summary (4/10 SP2 comment):** #144 — "ng double arrows 41" — too terse to act on. Needs clarification from client on what |   page/component this refers to.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-145 [DATA / Pri: - / Status: Open]
- **Page:** Admin - Partner API Access
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** How did test, [name redacted] and [name redacted] tEST get added? How do they get deleted?
- **Fix summary (4/10 SP2 comment):** - Revoke — immediately disables the key but keeps the record for audit trail. This is critical action for security. |   - Delete — permanently removes the record. Typically only allowed on keys that are already     revoked, and usually with a confirmation modal. |  |  Behavior: Active keys show revoke button (ban icon). Revoked keys show delete button (trash |   icon). Attempting to delete an active key via API returns 400 error.

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/PartnerAPI (Partner API Access management) | 3. Identify "test", "client" and "client tEST" entries (or any other test keys) | 4. Verify Active keys show a Revoke button (ban icon) | 5. Click Revoke on a key — confirmation appears, key flips to Revoked status | 6. Revoked keys now show a Delete button (trash icon) | 7. Click Delete on a revoked key — key permanently removed | 8. Try clicking Delete on an Active key — denied with a clear message that you must Revoke first |  | PASS: Active keys show Revoke only; Revoked keys show Delete; Active keys cannot be directly deleted; both actions audit-trail correctly.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-146 [WAD / Pri: - / Status: Open]
- **Page:** Admin - Tags
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** The page header should be left aligned - right now it is right next to buttons
- **Fix summary (4/10 SP2 comment):** #146 — Tags page header right-aligned: |   Root cause: tags.css had justify-content: flex-end on the .admin-header, overriding the standard |   space-between layout. This pushed the page title ("Tag Management") to the right, next to the action |   buttons instead of left-aligned. Fix: Changed to justify-content: space-between to match all other |   admin pages. |  |   This QC item appears to already be resolved — the CSS already has justify-content:            space-between on .admin-header. The QC description itself describes the root cause and the e |   fix that was applied. |  |   Can you confirm this is already fixed when you look at the Tags page in the browser? If it |   still looks wrong, I'll need a screenshot to see what's happening.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-147 [DEFECT / Pri: - / Status: Open]
- **Page:** Global - Login pages where present
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Your arrows are going the wrong direction for import and export icons and need updated
- **Fix summary (4/10 SP2 comment):** All Import/Export buttons across the entire app are now using:  | Import = fa-file-import (arrow going into a document) | Export = fa-file-export (arrow going out of a document)

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Walk through every page that has Import/Export buttons (Certificate listing, Product Management, User Management, Tags, Cert Types, etc.) | 3. Verify icon for IMPORT shows arrow going INTO a document (fa-file-import — arrow points down/into doc) | 4. Verify icon for EXPORT shows arrow going OUT of a document (fa-file-export — arrow points up/out of doc) | 5. Hover tooltip should match the action |  | PASS: Every Import button uses fa-file-import; every Export button uses fa-file-export — consistent across all pages.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-148 [CR / Pri: - / Status: Open]
- **Page:** Global - Excel Templates
- **Raised by:** client
- **Sprint 2:** Dev
- **Defect:** For standard fields in Excel template can you include in header name what options are where appropriate: |  | For example | Status (Active, Inactive)
- **Fix summary (4/10 SP2 comment):** Change Request: Template header updates: |  Products (productManagement.js): Status → Status (Active, Inactive)                           |  Tags (tags.js): TagType → TagType (Internal, External) |  Users (userManagement.js): Already had hints from QC #108 — no header changes needed

**Baseline test:**

1. Log in as analyst or admin | 2. Download Excel/CSV templates from these pages and inspect the column headers: |    - /Product/Management → Status header reads "Status (Active, Inactive)" |    - /Admin/Tags → TagType header reads "TagType (Internal, External)" |    - /Admin/UserManagement → headers already include hints from QC #108 (no change needed here) | 3. Each header should clearly indicate the valid value options inline |  | PASS: Templates include valid-value hints in the header for Status (Products), TagType (Tags), and any other constrained-value fields.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-149 [DEFECT / Pri: - / Status: Open]
- **Page:** Public
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** The second and third level product options in the Product Categories box do not filter certs being shown
- **Fix summary (4/10 SP2 comment):** #149 — Public view Level 2/3 product category filters not working: |   Root cause: The filter hierarchy shows three levels from ProductCategory (Level 1) and ProductType |   table (Level 2 root types, Level 3 child types). But the repository query had two problems: (1) No |   parent cascade — selecting a Level 2 parent type didn't match certificates assigned to its child |   types. (2) The Product.ProductSubcategory navigation points to a different table (ProductSubcategory) |    than the hierarchy's Level 3 items (which come from ProductType children). Fix: Added parent |   traversal checks (ProductType.Parent.Name) so selecting a parent type also matches its children, |   added null-safe checks on c.Product, and included both the ProductSubcategory table and ProductType |   child entities in the matching logic.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Open Product Categories filter — expand to show 3 levels (Level 1 categories, Level 2 root types, Level 3 child types) | 3. Tick a Level 2 (parent) type — list should filter to certs assigned to that parent OR any of its child types (cascade) | 4. Tick a Level 3 (child) type only — list filters to certs assigned to that specific child | 5. Tick a Level 1 category — list filters to all certs under that category tree | 6. Untick all — full set returns |  | PASS: Level 1, 2, AND 3 product category filters all actually narrow the cert results, with cascading parent→child behavior on Level 2.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-150 [NEW / Pri: - / Status: Open]
- **Page:** Sign In
- **Raised by:** client
- **Sprint 2:** Needs Email Configuration
- **Defect:** Anuki confirm forgot password works. We will need individual logins at some point to see emails and test
- **Fix summary (4/10 SP2 comment):** #150 — Forgot password verification: |   Anuki needs to confirm forgot password works in Azure AD B2C. This requires individual test logins |   with real email addresses. Not a code defect — it's a verification task for the auth configuration.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-151 [CR / Pri: - / Status: Open]
- **Page:** Login - Two-Factor Auth Settings
- **Raised by:** client
- **Sprint 2:** work with team to discuss changes to security.
- **Defect:** MS Auth is ok - but is Duo also an option? That is more popular here
- **Fix summary (4/10 SP2 comment):** #151 — Duo MFA option: |   Azure AD B2C supports Duo as an MFA option through custom policies or a Duo integration. This would |   be a configuration change in the B2C tenant, not application code. Need to confirm if client's B2C plan |   tier supports custom identity providers for MFA.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-152 [COSMETIC / Pri: - / Status: Open]
- **Page:** Mobile Search
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Magnifying glass running into line border of box
- **Fix summary (4/10 SP2 comment):** #152 — Mobile search magnifying glass overlapping border: |   The search icon was positioned at left: 15px with input padding of only 35px on the left. On mobile, |   the bold icon (font-weight 900) crowded against the input border. Fix: Added mobile-specific CSS |   increasing input left padding to 40px, adjusting icon position to left: 12px, and reducing icon |   font-size to 14px for better spacing within the narrower mobile input.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) on mobile viewport (390x844 iPhone 12) | 2. Look at the search input box at the top | 3. Magnifying glass icon must sit cleanly inside the box with clear margin from the LEFT border — NOT touching or overlapping the border | 4. Icon font size is reduced (~14px) so bold weight doesn't crowd the box | 5. Tap into search and type — text starts well to the right of the icon |  | PASS: Search icon doesn't overlap input border on mobile; clear visual gap; typing space adequate.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-153 [DEFECT / Pri: - / Status: Open]
- **Page:** Mobile Cert Details View
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** The cert image is getting cutoff similar to desktop and buttons not aligning |  | Also, can you move the image to the bottom after the content vs first since once sized it will be hard to read so at least they can see other content first
- **Fix summary (4/10 SP2 comment):** #153 — Mobile cert details image cutoff and wrong order: |   Two issues: (1) Image was displayed ABOVE content on mobile (order: -1), but client wants it below so |   users see certificate details first. Changed to order: 1. (2) Action buttons (zoom, download, |   edit/delete) were right-aligned on mobile, not fitting well on narrow screens. Added justify-content: |    center for download action icons and internal action buttons on mobile.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) on mobile viewport (390x844) | 2. Click into any cert tile to open cert detail page | 3. Verify the cert content (company name, status, fields, tags) appears FIRST, scrollable | 4. The cert IMAGE should appear at the BOTTOM (below content), NOT the top | 5. Image displays in full (not cut off) using aspect-ratio sizing | 6. Action buttons (zoom, download) below the image are CENTER-aligned (not right-aligned crammed against the screen edge) |  | PASS: On mobile cert detail, content shows above the image; image displays in full; action buttons center-aligned.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-154 [WAD / Pri: - / Status: Open]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** The reports under Dashboard are not taking into account all 'Active' cert status types.  |  | Client Approved - Paid  (seems to be counting this) | Client Approved - Confidential (does not show on public side but is still considered an 'active' cert for reporting - is currently filtering these out in reports) | Republished (seems to be counting this) |
- **Fix summary (4/10 SP2 comment):** QC #154 — Dashboard not counting Confidential certs                                                   - Added DashboardStatuses array (Client Approved - Paid + Republished + Client Approved - |   Confidential) in CertificateStatus.cs |   - Updated all dashboard stat queries in CertificateRepository.cs to use DashboardStatuses instead of   PublicStatuses      ✗ |   - PublicStatuses unchanged — Confidential certs remain hidden from public tile views

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Dashboard | 3. Stats that count "active certs" should now include all 3 dashboard statuses: Client Approved - Paid + Republished + Client Approved - Confidential | 4. Verify the dashboard count is HIGHER than the public-facing cert count (because Confidential is counted internally but not shown publicly) | 5. Cross-check via /Certificate/CertificateTileViewInternal filtered by these 3 statuses — count matches the dashboard widget | 6. Public view still EXCLUDES Confidential (verify on /Certificate/CertificateTileView) |  | PASS: Dashboard counts include Confidential; public view does not. Numbers reconcile.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-155 [DEFECT / Pri: - / Status: Open]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** # Product Categories with Active Certs, # Product Types with Active Certs, # Product Type Subcategories with Active Certs |  | Export does not have product cat/type/sub columns. Need to make sure the data being pulled is included in export for data review
- **Fix summary (4/10 SP2 comment):** QC #155 — Product cat/type/sub exports missing detail columns |   - Added GetProductSubcategoryStatisticsAsync() to repository |   - Modified DashboardController.DownloadStat() — when downloading product-categories, product-types, |   or product-subcategories, it now exports a detailed CSV with each name and its active cert count |   (instead of a single-row count)

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Dashboard | 3. For "# Product Categories with Active Certs" — click Download | 4. CSV should have rows: each category name + its active cert count (multi-row breakdown) | 5. Repeat for "# Product Types with Active Certs" and "# Product Type Subcategories with Active Certs" | 6. All three downloads should be detail breakdowns, NOT a single-row summary |  | PASS: Each of the three product cat/type/subcat exports returns a detailed multi-row CSV with names and active cert counts.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-156 [NEW / Pri: - / Status: Open]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** # Certs Published Last Month, # Certs Published Last Year, # Certs Published This Year |  | It seems to only be counting the still 'active' status certs. That's fine just add to the names for these # Active Certs…...
- **Fix summary (4/10 SP2 comment):** QC #156 — Published stat labels unclear |   - Renamed 3 labels in Dashboard/Index.cshtml: |     - # Certs Published Last Month → # Active Certs Published Last Month |     - # Certs Published This Year → # Active Certs Published This Year |     - # Certs Published Last Year → # Active Certs Published Last Year

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Dashboard | 3. Verify the following labels read EXACTLY: |    - "# Active Certs Published Last Month" (was "# Certs Published Last Month") |    - "# Active Certs Published This Year" (was "# Certs Published This Year") |    - "# Active Certs Published Last Year" (was "# Certs Published Last Year") | 4. Cross-check the count by counting active certs published in the named period |  | PASS: All three labels include "Active" prefix to clarify scope; counts match active cert filter.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-157 [DEFECT / Pri: - / Status: Open]
- **Page:** Login - Cert List View
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Status only sorts ascending  |  | Location is not sorting right for ascending or descending (not following alphabetical order) |  | Check that Internal Tag is sorting right, seems off |  | Partner, ASIN, UPC not sorting right
- **Fix summary (4/10 SP2 comment):** all columns now have both ascending and descending sort  ─ |   support.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal — switch to List view | 3. Click each column header below — confirm BOTH ascending AND descending sort works on each: |    - Status (toggles A→Z and Z→A) |    - Location (alphabetical, both directions) |    - Internal Tag (alphabetical, both directions) |    - Partner (alphabetical, both directions) |    - ASIN (correct alphanumeric order, both directions) |    - UPC (correct numeric order, both directions) | 4. Click between columns — sort icon resets on inactive column |  | PASS: All 6 listed columns sort cleanly in both directions following correct alphabetical/numeric order.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-158 [CR / Pri: - / Status: Open]
- **Page:** Login - Internal Cert Detail View
- **Raised by:** client
- **Sprint 2:** Review Changes
- **Defect:** What will happen when there are hundreds or thousands of ASINs tied to cert
- **Fix summary (4/10 SP2 comment):** Options to consider: |   - Quick fix: Add a collapsible section that shows first 20 ASINs with a "Show all X" expand |   button. Low effort, solves the immediate UX problem. |   - Medium fix: Add server-side pagination to the product ID API (?page=1&pageSize=50) with |   load-more in the UI. More robust but touches more code. |   - Full fix: Virtual scrolling — overkill for this use case. |  |  |   My recommendation: the collapsible approach with a count badge. It's what users expect and |   matches the pattern already used in the table view (ASIN count badges). |  | The system currently loads ALL product IDs at once with no limits at every layer: |   - Detail view: Simple @foreach with no scrollable container — 1000 ASINs = 1000 divs stacking |    vertically |   - Edit view: Has a 400px scrollable container but renders all rows in DOM |   - API: No pagination parameters, returns everything |   - Database: .Include(c => c.ProductIds) with no .Take() limit |  |   It won't crash, but becomes sluggish above ~500 items and unusable above 1000. The quickest |   fix would be a collapsible section showing first 20 with "Show all X" on the detail view, and |    server-side pagination on the API for the edit view. Want me to scope that as a formal CR or |    tackle it now?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-159 [QUESTION / Pri: - / Status: Open]
- **Page:** Cert Creation Page - Product Identifier Section
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** DISCUSS: I see how someone would edit or add to the Product list individually. |  | What if someone wanted to replace all the existing with a new list that could have a thousand records tied to it? So it would remove those not included and add those new and keep existing still on list? |  | I tried a version of this and got an error. There were 2 listed from a prior import. I tried to import a doc with only 1 product.
- **Fix summary (4/10 SP2 comment):** QC #159 — Product ID bulk import errors and no replace option: Two bugs fixed. (1) |   GetValidationErrorsAsync() in ProductIdService.cs called CheckDuplicateAsync() without |   passing certificateId, causing a global duplicate check across all certificates instead of |   scoping to the current one — fixed by adding certificateId parameter and passing it through. |   (2) The API's overwriteExisting parameter was accepted but never used — implemented full |   replace logic: when enabled, existing active product IDs not present in the import file are |   deactivated before new ones are added. Duplicates that already exist on the certificate are |   silently skipped in overwrite mode. Added "Replace all existing" checkbox to the import |   preview modal in _ProductIdManager.cshtml. Client-side applyImport() in product-id-manager.js |    updated to delete existing IDs not in the import when toggle is checked.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Edit/{id} | 3. Open Product ID Manager → click Bulk Import | 4. Upload a CSV with hundreds of Product IDs (some duplicate within THIS cert, some appearing on OTHER certs) | 5. Verify duplicates within this cert are flagged; duplicates from other certs are ALLOWED (scope is per-certificate, not global) | 6. Test "Replace All" / overwriteExisting option (if exposed) — uploads new list, removes old IDs not in new list, keeps existing IDs that are in new list, adds new IDs |  | PASS: Validation scopes duplicate checks per-cert (not global); bulk import handles 1000+ rows; replace-all option works as described (delete + add + keep).

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-160 [QUESTION / Pri: - / Status: Open]
- **Page:** Cert Detail Page
- **Raised by:** client
- **Sprint 2:** Clarification provided, no defect
- **Defect:** Will the Database Listing URLs change when we have a production URL? |  | Right now for example it has: | https://gccwebapp.com/Certificate/Verify/26-0004
- **Fix summary (4/10 SP2 comment):** Defect: Database Listing URLs — will they change for production? |   Fix Summary: Not a defect. Verification URLs are dynamically built using |     Context.Request.Scheme + Context.Request.Host in views. They auto-adapt to any domain. No |     code change needed.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-161 [CR / Pri: - / Status: Open]
- **Page:** Login - Cert List View
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Columng Settings dropdown missing: | Certification Results | Pathway | Certificate Subcategories | Contact URL
- **Fix summary (4/10 SP2 comment):** QC #161 — Cert List Column Dropdown Missing Fields: Fixed. Four fields were missing from the internal certificate listing table: Certification Results, Pathway, Certificate Subcategories, and Contact URL. The API controller |   (CertificateTileInternalApiController.cs) was updated to include Pathway and CertificationTypeSubcategories navigation properties and return four new fields in the response. The view was updated with column toggle switches under a   new "Additional" section in the column picker dropdown, along with matching sticky header cells. The JavaScript row rendering was updated to extract and display the new data, and CSS column widths were added. These four columns append to the end of the table and can be toggled on/off like all other columns.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal — switch to List view | 3. Open Column Settings dropdown | 4. Verify these 4 fields are PRESENT and toggleable: |    - Certification Results |    - Pathway |    - Certificate Subcategories |    - Contact URL | 5. Toggle each ON — column appears in the table with data populated for at least one row | 6. Export to CSV — verify all 4 columns appear in the export |  | PASS: All 4 fields are in the column settings dropdown, render correctly when enabled, and export to CSV.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-162 [QUESTION / Pri: - / Status: Open]
- **Page:** Cert List Export
- **Raised by:** client
- **Sprint 2:** Clarification provided, no defect
- **Defect:** For the ASIN/UPC/etc columns when doing a full export --- will it only list the ones with active status - since it appears just in comma seperated list in cell?
- **Fix summary (4/10 SP2 comment):** Defect: Cert List Export — ASIN/UPC columns, Active only? |   Fix Summary: Not a defect. CertificateService.cs GetProductIdValues() already filters by |     pid.IsActive. Only Active-status product identifiers appear in the comma-separated export |     cells.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-163 [DEFECT / Pri: - / Status: Open]
- **Page:** Login Cert Listing
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** You cannot collapse the filter boxes. It appears to be working on public view but not when you hit the down arrow next to the filter box name like Certification Type when logged in
- **Fix summary (4/10 SP2 comment):** Defect: Filter boxes won't collapse on logged-in cert listing (works on public view) |   Fix Summary: Fixed. certificate-tile-view-internal.js and certificate-tile-view-external.js |     were missing toggleFilter() function and .filter-header click event listeners that existed |     in the public certificate-tile-view.js. Added both to internal and external JS files. CSS |     rule .filter-collapsed already existed in shared stylesheet.

**Baseline test:**

1. Log in (e.g., external / TestPass123! or analyst / TestPass123!)                      ─ |   2. Navigate to the logged-in cert listing — /Certificate/CertificateTileViewExternal        (external role) or /Certificate/CertificateTileView (analyst)                            ─ |   3. DEF-099 test: Click the chevron next to a filter name like "Certification Type" — the    panel should collapse on the first click, expand on the second. Repeat for a couple         filters.                                                                                    4. DEF-098 test: Resize browser to tablet width (≤1024px, e.g., iPad portrait 768px or      landscape 1024px) using DevTools device mode — you should see only one "Hide Filters" bar,   not two stacked.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### E.2 - Items already marked Complete (regression-sweep candidates)

Count: 114. For these, the test card is shorter - confirm the original fix still works after subsequent commits. Many of these can be batch-validated via the existing Playwright suites (RoleBasedUI, MobileUITests, UAT).

### TC-QC-001 [WAD / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** All fields seem crammed together. Can we get them to all line up, add some spacing, make more user friendly. Seems like something could get missed with it all so close together.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-002 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Theres no easy way to add a new company or new product from the add certificate button. Do we need separate pages for product and company? any reason we can't do all this from the same dashboard? See wireframes

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-003 [DEFECT / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Effective Date & Expiration Date - set dates to make it easier and not start at year 001

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-004 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Should location be a required field?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-005 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Is partner the right word for the field?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-006 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** What are certificate tags - I thought we were using tags for retailers and such. But the DB shows retailers as 'partners' and that also has tags?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-007 [DEFECT / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Upload PDF -  You have to click the words to upload and not the PDF picture

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-008 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** General walk through on page. Product IDs, ASINs

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-009 [DEFECT / Pri: - / Status: Complete]
- **Page:** Certificates
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** On actual certificate - download button, view details button and edit button do not work. In list form as well.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-010 [WAD / Pri: - / Status: Complete]
- **Page:** Certificates
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Margins on both sides are very large - can we remove the margins so website uses the full page and make the dashboard/ list larger and easier to use?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-011 [CR / Pri: - / Status: Complete]
- **Page:** Certificates list
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** In the list form some details/fields are missing. Products & company name - this should have all certificate details.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-012 [CR / Pri: - / Status: Complete]
- **Page:** Certificates
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Can we have an option to hide the left hand navigation (filters)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-013 [DEFECT / Pri: - / Status: Complete]
- **Page:** Companies & Products
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Can't add new companies and products, the import not working

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-014 [QUESTION / Pri: - / Status: Complete]
- **Page:** General
- **Raised by:** Amanda/Dan
- **Sprint 2:** none
- **Defect:** Without real data in the database it is difficult to do a full review. Need to do a more thorough review with sample data

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-015 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Dan
- **Sprint 2:** none
- **Defect:** It looks like we are asked to add a single ASIN to attach to a product cert. How do we do a 1-many relationship - looks like we should be able to import CSV file of many id's for a product but right now unable to test

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-016 [DEFECT / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Dan
- **Sprint 2:** none
- **Defect:** i tried to go through and actually create a cert but it wont let me. need to review when it is live. cert # should be auto created.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-017 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Dan
- **Sprint 2:** none
- **Defect:** it asks to upload a cert preview image... this should be auto generated based on the cert PDF, by the website - we will not be uploading preview images.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-018 [Question / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Dan
- **Sprint 2:** none
- **Defect:** there are no internal and external tags

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-019 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Dan
- **Sprint 2:** none
- **Defect:** do we import csv's for product IDs here?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-020 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** Dan
- **Sprint 2:** none
- **Defect:** or here?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-021 [WAD / Pri: - / Status: Complete]
- **Page:** Global
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Remove 'Expiring Soon' tag globally

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-022 [WAD / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Any expired certs (Cert Expiration Date) should NOT appear in public database, neither should those marked with following statuses. See image 4. Anuki please check / fix |  | Status Types that DO NOT show on public view:  | DRAFT | Client Approved - Not Paid | Client Approved - Confidential | Hold | Expired | Expired - No Annual Verification |  | Status types that DO show on public view: Client Approved - Paid, Republished (so long as expiration date has not passed) |  | Right now you are using an 'Active' status to show what is live but that's not a status type we had

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-023 [DEFECT / Pri: High / Status: Complete]
- **Page:** General Functionality
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Anuki confirm that the DB changes the status to Expired once the expiration date has passed - this is key requirement |  | 4/3/26: See issue 142
- **Fix summary (4/10 SP2 comment):** Fixed. Two related bugs: (1) Dates off by 1 day — JavaScript parsed date-only strings ("2026-04-04") as UTC midnight, then toLocaleDateString() shifted to the user's local timezone, showing the previous day. Added

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal | 3. Find a cert whose expiration date is in the past — its Status should display as "Expired" (auto-flipped by the system) | 4. Click into the cert detail. Confirm the Expiration Date displays exactly as entered (no off-by-one — e.g. if entered 4/4/2026, it shows 4/4/2026, not 4/3/2026) | 5. Navigate to /Certificate/CertificateTileView (open in incognito/anonymous). The expired cert must NOT appear in public results. |  | PASS: Expired cert shows "Expired" status internally, is hidden from public view, and dates render with no off-by-one shift.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-024 [WAD / Pri: - / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Should not need popup that filters were cleared or that Sort By was complete (for any of them). Especially if we are adding them at top like we discussed on 3/6 call. See image 5
- **Fix summary (4/10 SP2 comment):** ▎ parseLocalDate() helper across 4 JS files that parses dates as local midnight. (2) Cert hidden on expiration day — Backend compared ExpirationDate against DateTime.UtcNow (current time), so a cert expiring today was   ▎ hidden partway through the day. Changed all comparisons to use DateTime.UtcNow.Date (today at midnight) across 8 C# files so certs remain visible through their full expiration day. Expiration service also updated to   ▎ mark certs expired the day after their expiration date, not during it.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-025 [WAD / Pri: - / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** In the Sort By area remove the following options in public view, we don't need (plus some are not working): | Newest Effective Date, Oldest Effective Date, Expiring Soon, Longest Validity Period, Recently Issued (Last 30 Days), Most Popular Companies, Relevance Score, Status |  | Move Product Type A-Z under Alphabetical section (changed mind on 4/3 Issue #136 to just remove this because we don't show on tiles so it's confusing to have)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-026 [DEFECT / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** None of the Sort By items (that we would be keeping) are working

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-027 [DEFECT / Pri: High / Status: Complete]
- **Page:** Public view - tablet/mobile
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Filters and hamburger menu don't work at tablet/mobile size. See image 6

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-028 [WAD / Pri: - / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Remove this extra step and click (see image). When Sign In is selected it should just go to Sign In page not have this dropdown to select again. See image 7

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-029 [QUESTION / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** Working As Designed
- **Defect:** Not all the certs are showing. Will it be 20 max on page? Then we need page listing on bottom to view others (with number of pages people can click on) |  | 4/6: Need to confirm once we have more data

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-030 [DEFECT / Pri: High / Status: Complete]
- **Page:** Public view - tablet/mobile
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Company name is not wrapping correctly on mobile view. See image 16

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-031 [DEFECT / Pri: - / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** On cert detail page download button not aligned with graphic |  | 4/3: Looks worse, see image 17B - image getting cutoff and the buttons are not aligned right
- **Fix summary (4/10 SP2 comment):** Fixed. Download button was centering within the full image column (400-600px) while the preview box was only 350px wide, causing misalignment. Added display: flex; flex-direction: column; align-items: center to |   ▎ .certificate-image-column so both the preview box and download button center-align together. Constrained download button to max-width: 350px to match preview box width. |  |

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) in browser | 2. Click any cert tile to open cert detail page | 3. Visually confirm the Download button sits horizontally centered directly under the certificate preview image (not offset right or cut off) | 4. Image preview box and Download button widths align (both ~350px max) | 5. Repeat at tablet (1024) and mobile (390) widths — alignment should hold; image must not be cut off |  | PASS: Download button is centered under the preview image at all viewports, and the preview image is fully visible (not cut off).

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-033 [DEFECT / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** When selecting APR / LEED under Additional filter options no sorting happened - not working |  |  | 4/3: Still not fixed, seeing certs show when tags are not listed on the cert (showing all certs). This sorting is broken
- **Fix summary (4/10 SP2 comment):** QC #33 (APR/LEED filters) - The frontend sends the additional filter |   correctly, but the repository never applies it to the query. The |   AdditionalFilters property is ignored in |   GetAdvancedFilteredCertificatesAsync() — so all certs come back            regardless of tag selection. |  |  QC #33 — APR/LEED Additional Filters |   - CertificateRepository.cs:286 — Added AdditionalFilters WHERE clause to● |   the query. The filter was received by the API but never applied to the  ─ |   database query. Now certs are filtered by matching tag names.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Expand "Additional" filter section | 3. Tick "APR" — list should refresh and show ONLY certs with the APR tag. Note total count. | 4. Untick APR, tick "LEED" — list should refresh and show ONLY certs with the LEED tag. Different count expected. | 5. Tick BOTH APR and LEED — should show certs with either tag (union behavior) | 6. Untick all — list returns to full set |  | PASS: Each Additional tag actually filters the list. Cert count changes per selection. No certs without the selected tag appear in results.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-034 [CR / Pri: - / Status: Complete]
- **Page:** General
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Anuki can you confirm what the character limit for display is for Company Name, Product Name and anything else relevant |  | The tiles are very high and I'm debating if we reduce by 1 or 2 lines worth of product name text but I'd like to know how much can fit now

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-035 [DATA / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Last test result:** Test Data Only.
- **Defect:** All certs uploaded should have a PDF/visual.  | Please remove the default page you have (there is certain info we would not want public) |  | 4/3: Please remove the default PDF that is showing I don’t want to run risk that this goes live if a cert is added without PDF
- **Fix summary (4/10 SP2 comment):** QC #35 (Default PDF) - Test data issue, no code fix needed. Data cleanup before go-live.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) | 2. Walk through every visible cert tile and click into detail page | 3. Each should display the actual uploaded PDF/visual for that cert — not the placeholder "default" PDF page | 4. (Pre-launch data cleanup item: confirm with client that no test/placeholder PDFs remain in production data) |  | PASS: No cert in public view shows a generic placeholder PDF; every cert has its real, intended visual.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-036 [DEFECT / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** We need to be able to have a public link that has the search results for a company name. Similar to current DB - so clients can link to our DB with theirs already filtered. This does not appear to be working when copy the URL into browser to go to filtered view (it's showing all certs) |  | https://gccwebapp.com/?search=ASSA+ABLOY

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-037 [COSMETIC / Pri: - / Status: Complete]
- **Page:** Public view - mobile
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Tags too close to bottom. See image 19 |  | 4/3: Still an issue on mobile see image 19B
- **Fix summary (4/10 SP2 comment):** QC #37 (Tags too close on mobile) - Has 20px total bottom spacing but no   mobile-specific override. On narrow cards it still looks cramped.  |  |  QC #37 — Tags too close to bottom on mobile                               - certificatetileview.css:627 — Increased base tag spacing (margin |   12→16px, padding 8→12px) and added mobile breakpoint (margin 20px,         padding 14px) for more breathing room on smaller screens.

**Baseline test:**

1. Open /Certificate/CertificateTileView (public) on mobile viewport (390x844 iPhone 12) | 2. Scroll to any cert tile that has tags (small pill labels like "Recycled Content", "FSC", etc.) | 3. Visually verify clear breathing room between the tag pills and the tile's bottom action bar (download/view icons) | 4. Repeat at 375x667 (iPhone SE) |  | PASS: Tags have at least 16-20px of vertical space below them on mobile — they do not touch or overlap the bottom action icons.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-038 [WAD / Pri: - / Status: Complete]
- **Page:** Public view / General
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** When no company email or URL is provided can you have them both just have a dash (similar to what you do for Product Type when it's blank) |  | Right now one says Available upon request and other Not provided

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-039 [CR / Pri: - / Status: Complete]
- **Page:** Public view / General
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** What does 'General Product' mean? Where is this pulled - what dropdown option? Don't think it is correct and it's causing filtering issues

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-040 [CR / Pri: High / Status: Complete]
- **Page:** Public view
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** When you are on the Cert detail page it opens a new tab which is good. But there is no way to get back to the cert homepage from that tab if you wanted or in general |  | The client logo at the top in header should always link back to the cert homepage - for all pages and login types

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-041 [WAD / Pri: - / Status: Complete]
- **Page:** Sign In
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Replace "Access your..." text with | Login is restricted—public sign-up is not available. |  | NOTE: Will we have a username -- isn't it just going to be email they can use? If username will not be accepted then remove its mention

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-042 [DEFECT / Pri: High / Status: Complete]
- **Page:** Sign In
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** No error message is appearing when an incorrect email/password combo is entered

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-043 [CR / Pri: - / Status: Complete]
- **Page:** Sign In
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** There should be a way to get back to the public homepage from the Sign In page. Add link | Return to GreenCircle database >

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-044 [DEFECT / Pri: - / Status: Complete]
- **Page:** Sign In
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Remember me' does not appear to be working all the time, please check

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-045 [DATA / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Not all cert types are appearing in dropdown (also make sure it has scroll bar) | Missing: Carbon Footprint Reduction, Sustainable Energy Practices, Sustainable Manufacturing Practices, Product Optimization: Carbon Footprint Reduction |  | Remove: Renewable Resource Content, Biobased |  | Change: Manufacturer Inventory Verification to Material Ingredient Reporting |  | 4/3: Not fixed. Remove and Change items are addressed but not the Missing ones
- **Fix summary (4/10 SP2 comment):** QC #45 (Missing cert types) - The SQL migration script                     (19_20260320_UpdateCertificationTypes.sql) was never created. The 4        missing types are not in the database.   |  |  QC #45 — Missing certification types |   - Created Scripts/UpdateCertificationTypes.sql — Adds 4 missing parent |   types (Carbon Footprint Reduction, Sustainable Energy Practices, |   Sustainable Manufacturing Practices, Product Optimization: Carbon |   Footprint Reduction), deactivates 2 (Renewable Resource Content, |   Biobased), renames 1 (Manufacturer Inventory Verification → Material |   Ingredient Reporting). This script needs to be run against the database |   manually.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create | 3. Click the Certification Type dropdown | 4. Type-ahead and confirm these 4 types are present (added in this fix): |    - Carbon Footprint Reduction |    - Sustainable Energy Practices |    - Sustainable Manufacturing Practices |    - Product Optimization: Carbon Footprint Reduction | 5. Confirm "Renewable Resource Content" and "Biobased" are NOT in the list (deactivated) | 6. Confirm the dropdown has a scrollbar when the list is longer than the visible area |  | PASS: 4 new cert types present, 2 deactivated types absent, dropdown scrolls.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-046 [DEFECT / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Getting error when selecting Certification Type

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-047 [DATA / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Add Waste Diversion Event under Certificate Subcategoies for Waste Diversion from Landfill

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-048 [WAD / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Keep Certificate Subcategories options in alphabetical order

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-049 [Data / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Under Certificate Subcategories |  | Add: ISO 14001 Certified, Verified Declare Label, Verified HPD

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-052 [DEFECT / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Product Name options need to be tied to the account only, not displaying all products this will become way too long of a list. Based on the added product the category, type, and subcategory would then be filled out  |  | This was in the wires and discussed functionality similar to current DB. When they select Company the existing Products should appear in dropdown or they can add new. When they add new they need to tie the category, type, and subcategory accordingly. When you select an already entered product they prefill. You will need to create some popup to complete the remaining fields not just enter product name in text box |  | Adding a product name/type/subcat is also not working in general

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-053 [WAD / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Remove All Categories as an option under Product Category | Remove All Types from Product Type | This creates filtering issues when sorting

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-054 [WAD / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Update Status types to only these: | DRAFT | Client Approved - Not Paid | Client Approved - Confidential | Client Approved - Paid | Hold | Expired | Republished | Expired - No Annual Verification |  | See row 27 based on functionality.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-055 [WAD / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Update Pathway field label to say  | Pathway (for Amazon CPF applicable certs)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-057 [QUESTION / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** What does this mean? See image 13

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-058 [WAD / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** no change, working as designed.
- **Last test result:** Need clarification
- **Defect:** I was testing to add a cert. I did have it open awhile but it never completely timed me out. I went to fill in everything and when I hit submit it cleared everything out and never saved info

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-061 [WAD / Pri: - / Status: Complete]
- **Page:** Create cert page - post creation
- **Raised by:** client
- **Sprint 2:** none
- **Last test result:** Working As Designed.
- **Defect:** Make sure Status only shows for logged in people

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-062 [DEFECT / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** I never saw a field for Company Email/URL to fill out in cert creation but it showed up on preview - add fields

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-063 [COSMETIC / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Fix arrow, blocking text

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-064 [DEFECT / Pri: - / Status: Complete]
- **Page:** Analyst - Cert listing page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** The cert I created 26-0005 is showing twice in the list. There should ever only be one listing per cert number

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-065 [QUESTION / Pri: High / Status: Complete]
- **Page:** Analyst - Cert listing page
- **Raised by:** client
- **Sprint 2:** Pending due to lack of data to test functionality.
- **Defect:** Similar to public page how many will appear on a page? There are no numbers to navigate |  | 4/6: Need to confirm once we have more data
- **Fix summary (4/10 SP2 comment):** The infinite scroll is working correctly in both environments. There is currently limited data. Pending more data for validations

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-066 [COSMETIC / Pri: High / Status: Complete]
- **Page:** Analyst - Cert listing page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Make status text smaller font size since this is internal anyway to fit better with long product names |  | 4/3: Not fixed status text is still huge see image 22B
- **Fix summary (4/10 SP2 comment):** The previous fix correctly set font-size: 13px on .certificate-card .status-badge, .status-label, .partner-label, and .partner-badge — but those are all <span> elements inside the .status-row div. The .status-row div itself had no   CSS rule, so it inherited from .card-header which sets: |   - font-size: 16px |   - text-transform: uppercase |  |   Any text nodes or whitespace between spans rendered at the inherited 16px uppercase, and the overall line height was based on 16px, making the status area look oversized. |  |   Fix: Added a .certificate-card .status-row rule that sets font-size: 13px and text-transform: none at the container level, so everything inside — spans, text nodes, whitespace — all render at the smaller size. The individual span |   rules remain as reinforcement.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal | 3. Locate cert tiles with long product names (multiple words wrapping) | 4. Visually verify the status badges and labels (e.g. "Client Approved - Paid", partner badges) render at 13px (smaller than the 16px previously used) | 5. Status text fits within the card header without overflowing or pushing other elements out of place | 6. The .status-row div text is also reduced (no leftover 16px text from card-header inheritance) |  | PASS: Status badges/labels are 13px on internal tile cards and fit cleanly even with long product names — no oversized text or layout overflow.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-067 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Some numbers in the dashboard are not accurate based on current data. Not sure how this information is pulling.  |  | 21 active certs - correct as of time of pull | 154 companies with active certs -- how could this be more than number of certs? I think you are taking the number of companies in the system but not with ACTIVE certs | Same logic applies for product categories (60), product types (93), certs published this year (24)  |  | I changed a cert with 'active' and cert effective year in 2025 and the number for cert published last year remained 0 - so not working

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-068 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Not sure how Expired is getting calculated under # Certs by Status -- it says 3 but none have that status. Make these counts match the status types we are keeping. (Expiring soon is not one -- and the timeline is not qualified anywhere either)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-069 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** It says 'No product type data available' - when there should be

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-070 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - Dashboard
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Top 5 Product Categories by Active Cert Count is not showing the correct data. The information in the table are certificate types (Recycled Content, etc.). These are not product categories

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-071 [QUESTION / Pri: High / Status: Complete]
- **Page:** Login
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Is there an auto logout based on inactivity? What is the time? I worry it will delete work people are doing  |  | Also, instead of taking you back to login screen it gives a 'This page isn't working' which is not an experience we want - https://gccwebapp.com/Account/LogoutPost |  | 4/6: I am getting 'This page isn't working HTP ERROR 400' after I had it open awhile. Need confirmation on how it's expected to function and timeout duration in different scenarios
- **Fix summary (4/10 SP2 comment):** Session timeout behavior is now: |   - 30 min inactivity: Client-side timer shows warning at 25 min, logs out at 30 min via form |   submit |   - Server cookie expires first (e.g., browser tab was suspended): Page navigation → redirects |   to login with "Your session has expired" message. AJAX calls → same redirect via fetch |   interceptor. |   - No more "This page isn't working" — all expired session paths lead to the login page with a |    clear message. |  |   Ready for the next QC issue.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) at /Account/Login | 2. Open a new tab and directly visit /Account/LogoutPost (a GET request to a previously POST-only endpoint) | 3. You should be redirected gracefully to /Account/Login — NOT see a "This page isn't working" 400/405 error | 4. Log in again, then leave the tab idle for the configured session timeout duration (check appsettings for the timeout value) | 5. After timeout, click any link or refresh — you should land on the login page with a "Session expired" message, NOT a Chrome error page |  | PASS: Logout via GET works, session timeout returns to login with a clear message — no raw 400/405 error pages.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-072 [DEFECT / Pri: High / Status: Complete]
- **Page:** Data Export
- **Raised by:** client
- **Sprint 2:** none
- **Last test result:** Review
- **Defect:** The way the dates are exporting to Excel the format is not recognizing as a date. This will be problematic when we try and do sorting/manipulation. Usually when it's in a date format it would show breakdown by year/month in the filter box.  |  | Even when I change the cell type to date it does not work

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-073 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - Dashboard - Data Export
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** When I went to export the data for Total Active Certs - the total active is 21 (right) but the export only listed 19 results  |  | I did not check all others -- Anuki needs to make sure accurate

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-074 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - Dashboard - Data Export
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Download Expiring Certs Report (Next 3 Months) --- this exports a bunch of code not actual data

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-075 [Duplicate / Pri: - / Status: Complete]
- **Page:** Login - Dashboard - Data Export
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Download Current Active Certs Report --- this exports a bunch of code not actual data

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-076 [Duplicate / Pri: - / Status: Complete]
- **Page:** Login - Dashboard - Data Export
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Download Expiring Certs Report (Next 30 Days) --- this exports a bunch of code not actual data

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-077 [CR / Pri: High / Status: Complete]
- **Page:** Data Export
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** On Certificate listing page when selecting 'Export All to Excel' it does not include every field associated with a certificate and needs to (e.g., missing product type, cert subcategory, product subcategory, cert results, pathway, cert notes, etc.) |  | It needs to be able to add Admin added new fields too
- **Fix summary (4/10 SP2 comment):** new fields that were requested during development, needed added to the export.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-078 [DATA / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Remove the sample data in 'Template' only include row headers |  | 4/3: The export has Value instead of Product ID. They should match to avoid confusion
- **Fix summary (4/10 SP2 comment):** Problem: The Product ID export and template CSV used "Value" as the column        header, while the UI calls it "Product ID" — they should match to avoid |   confusion. |  |   Changes (3 files): |   1. product-id-manager.js:1222 — Template download header: Value → Product ID |   2. ProductIdService.cs:369 — Export CSV header: Value → Product ID |   3. ProductIdApiController.cs:378 — Sample CSV endpoint header: Value → Product |   ID |   4. product-id-manager.js:782 — Import parser now also accepts product id (with |   space) as a valid column header, so re-importing exported files works seamlessly

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Create | 3. Open Product ID Manager (cog/edit icon next to Products section) | 4. Click "Download Template" | 5. Open the downloaded CSV. It should: |    - Contain ONLY headers (no sample data rows) |    - Use "Product ID" as the column header (NOT "Value") | 6. Click "Export" on existing Product IDs and verify the export CSV also uses "Product ID" header (matches template) |  | PASS: Template has headers only, both template and export use "Product ID" column header consistently.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-079 [DEFECT / Pri: - / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Search bar does not appear to work under Product ID Manager

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-080 [DEFECT / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Edit option under Product ID Manager does not work - nothing editable, freezes  |  | 4/3: Not fixed. Grayed out and cannot click into fields. See image 24B
- **Fix summary (4/10 SP2 comment):** Root cause: new bootstrap.Modal() was called every time Add or Edit was clicked. Bootstrap 5─ |   creates a new backdrop overlay each time new bootstrap.Modal() is invoked on an element that   already has a modal instance. After the first modal interaction, subsequent opens stacked an |   extra backdrop on top of the modal dialog — making fields appear grayed out and unclickable.  |   Fix: Changed all three new bootstrap.Modal(...) calls to |   bootstrap.Modal.getOrCreateInstance(...) in product-id-manager.js (lines 311, 322, 336). This |    reuses the existing modal instance instead of creating duplicate backdrops. |  |   This affects both the Create and Edit certificate pages, but was most visible on Create |   because that's where users would Add a product ID first (creating the initial modal instance) |    and then try to Edit it (triggering the duplicate backdrop).

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/Edit/{id} for any existing cert | 3. Open Product ID Manager | 4. Click "Add" — modal opens, fill in fields, save (modal closes cleanly) | 5. Click "Edit" on an existing Product ID row — modal opens, fields are EDITABLE (not grayed out / unclickable) | 6. Modify a value, save — change persists | 7. Open Add modal again — fields still work, no stacked overlay/backdrop | 8. Repeat Edit/Add several times to confirm no stale modal instances |  | PASS: Edit and Add modals open with fully interactive fields every time, no grayed-out backdrop overlay accumulation.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-081 [DEFECT / Pri: High / Status: Complete]
- **Page:** Create cert page
- **Raised by:** client
- **Sprint 2:** none
- **Last test result:** Review
- **Defect:** Under Product ID Manager I don't think importing SKUs or long lists in CSV works, it through an error cause the CSV changes it to 1.23E+12

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-082 [COSMETIC / Pri: - / Status: Complete]
- **Page:** Login - cert listing page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Make the font sizes in list view smaller (similar to size you have in Cert # and dates)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-083 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - cert listing page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** When opening the certificate details via arrow icon - open in new tab

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-084 [CR / Pri: - / Status: Complete]
- **Page:** Login - cert listing page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Move Company Contact Email/URL to last row in table |  | 4/3: You are missing Company URL column, which can be last. You are also missing Certificate Subcategories, Certification Results, Pathway > these can be right before Company Email towards the end |  | Company Contact Email column is also getting cut off. See image 38
- **Fix summary (4/10 SP2 comment):** Already in place (not missing): |   - Company URL column (tc-companyurl) - present in headers, body rows, column      toggle, and API |   - Certificate Subcategories column (tc-certsub) - present |   - Certification Results column (tc-certresults) - present |   - Pathway column (tc-pathway) - present |   - All data is loaded via .Include() and returned in the API response |   - CSV export includes all these columns |  |   Fix applied: |   - Company Contact Email column width: increased from 150px to 220px in |   certificate-tile-internal.css to prevent text cutoff |  |   client may have been testing before these columns were deployed, or her browser |   had cached column visibility settings hiding them. The columns are all |   functional and data-populated. If she's still not seeing them, she may need to |   click the "Columns" button and hit "Show All" to reset her column toggle |   preferences (stored in localStorage).

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal — switch to List view | 3. Open Column Settings — verify these columns are available (and ON if appropriate): |    - Company URL (last position) |    - Company Contact Email (next-to-last) |    - Certificate Subcategories |    - Certification Results |    - Pathway | 4. Enable all and confirm data populates correctly in each column for at least one row | 5. Export to CSV — verify all listed columns appear in the export with data |  | PASS: All listed columns are present in column toggle, list view, and CSV export with correct data.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-085 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login - cert listing page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Expiration Date is missing sort arrow in header row | Arrow for Effective Date is running into line

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-086 [CR / Pri: High / Status: Complete]
- **Page:** Login - cert listing page
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Last test result:** Review
- **Defect:** As discussed on call need to find a better way to scroll horizontally in list - UX is hard - our application Wrike makes it easy by just scrolling in table with mouse vs having to use scroll bar specifically |  | 4/3: The above was fixed, however, now there is no ability to scroll vertifically to see more results. When I scroll all the way down and back up I am only left with 5 rows to see (it works to start then doesn't)
- **Fix summary (4/10 SP2 comment):** Root cause: The wheel event handler (line 1758) was calling e.preventDefault() on every     ─ |   vertical mouse scroll when the cursor was over the table body. Since the table has horizontal   overflow (wide columns), the condition was always true, and all vertical page scrolling was   blocked whenever the mouse hovered over the table. This made it impossible to scroll down to─ |   see more rows or trigger infinite scroll to load additional results.                        ─ |                                                                                                 Fix: Changed the wheel handler to only intercept scrolling when the Shift key is held |   (e.shiftKey). This gives users two ways to scroll horizontally: |   1. Sticky scrollbar at the bottom (already in place, works as before) |   2. Shift + mouse wheel when hovering over the table (power-user shortcut) |  |   Normal vertical page scrolling now works normally, so users can scroll through all |   certificate rows and trigger infinite scroll to load more results.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal — switch to List view | 3. Position cursor over the table body | 4. Use mouse wheel — vertical scroll should move the PAGE up/down (NOT just hijack to horizontal table scroll) | 5. Use Shift+wheel (or trackpad horizontal swipe) — table should scroll horizontally | 6. Verify you can scroll all the way down the page to see every row |  | PASS: Vertical mouse scroll works normally for page scrolling even when over the table; horizontal scroll still available via Shift+wheel or scrollbar.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-087 [WAD / Pri: - / Status: Complete]
- **Page:** Login - cert listing page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Under Status some Active show in green box - others without. Just be consistent, either is fine

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-088 [COSMETIC / Pri: - / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Sort arrows overlapping or close to column name, increase width of cell

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-089 [COSMETIC / Pri: - / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Similar to the certs listing page request about making font smaller in table - you could do that here too, just to help with wrapping (not required but consideration)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-090 [CR / Pri: High / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** This page will get VERY long and have a lot of data - you will need to have it stop at 50 and go to different page with numbers at bottom

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-091 [DEFECT / Pri: High / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Columns missing for Product Type, Product Subcategory |  | 4/3: Columns are added but the sort arrows are overlapping text. See image 39
- **Fix summary (4/10 SP2 comment):** Root cause: The <th> sort icons have two conflicting CSS classes — sort-icon and |   admin-sort-icon: |   - admin-tables.css defines .admin-sort-icon with position: absolute; right: 8px; top: 50%; |   transform: translateY(-50%) — designed for absolute-positioned icons |   - product-management.css defines #product-table th .sort-icon with display: inline-block; |   margin-left: 4px — designed for inline icons |  |   Both classes apply to the same <span>, and the absolute positioning wins, pulling the arrow |   out of the text flow and stacking it on top of the column name. |  |   Fix: Added position: static; transform: none; to the #product-table th .sort-icon rule. This |   overrides the absolute positioning from admin-sort-icon and keeps the sort arrows inline next |    to the header text. Also removed position: relative from #product-table th since it's no |   longer needed as a positioning context.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Product/Management | 3. Verify the table includes columns: Product Type, Subcategory (in addition to existing columns) | 4. Click each column header to sort — ascending then descending | 5. Verify the sort arrow icons do NOT overlap the column header text (icons are inline, sized appropriately, not absolutely positioned over text) | 6. Export to CSV — verify Product Type and Subcategory columns are in the export |  | PASS: Both columns visible, sortable, arrows render without overlap, exports include the new columns.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-092 [WAD / Pri: - / Status: Complete]
- **Page:** Product Management - Add New Product popup
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Should not need Status, SKU, or Model Number

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-093 [DEFECT / Pri: High / Status: Complete]
- **Page:** Product Management - Add New Product popup
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** No options are appearing for Product Type and Product Subcategory

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-094 [QUESTION / Pri: High / Status: Complete]
- **Page:** Product Management - Export
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Should include all fields available at product level - missing Subcategory |  | 4/3: The subcategory field is there but the information under Chemicals and Raw Materials is wrong.  | Product Type level = Additives, Metal Alloys, Minerals, Plastics | Subcategory = HDPE, LDPE, LLDPE, PET, PP, PVC (right now you have all these subs under product type level which is wrong)
- **Fix summary (4/10 SP2 comment):** This is a data issue, not a code issue. Here's what's happening: |  |   Root Cause |  |   The product type hierarchy is a 3-level self-referencing structure:              - Level 1 (Category): e.g., "Chemicals and Raw Materials" |   - Level 2 (Product Type): e.g., "Additives", "Metal Alloys", "Minerals",          "Plastics" |   - Level 3 (Subcategory): e.g., HDPE, LDPE, LLDPE, PET, PP, PVC |  |   The problem is that HDPE, LDPE, LLDPE, PET, PP, PVC have ParentId = NULL in the |   database, which puts them at Level 2 (Product Type) instead of Level 3 |   (Subcategory) under "Plastics." |  |   Fix Options |  |   1. Admin UI — Someone with Admin access can use the Product Types page to move |   these items under the correct parent (e.g., drag/reassign HDPE under "Plastics") |   2. SQL Script — Run an UPDATE to set the correct ParentId: |   -- Find the parent "Plastics" ID first |   -- Then UPDATE ProductTypes SET ParentId = @PlasticsId WHERE Name IN |   ('HDPE','LDPE','LLDPE','PET','PP','PVC')

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-095 [DEFECT / Pri: - / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Sorting ascending/descending is not working for Company and Category columns (or if it does work it only does it 1 time and won't change again) |  | Certificates and Identifiers columns do not sort at all |  | 4/3: Ascending/descending sorting is not working on columns on this page
- **Fix summary (4/10 SP2 comment):** QC #95 — Sorting ascending/descending not working on Product Management columns               |   Root cause: Two competing sort click handlers were attached to the same <th> elements:        |   1. Inline script (Management.cshtml line 771): The correct handler — uses sortMap to |   translate data-sort attributes to actual JS property names (company → companyName), handles |   numeric sorts for Certificates/Identifiers, and calls populateTable() to re-render. |   2. productManagement.js (line 47): A competing handler — attaches its own handleSort() to all |    [data-sort] elements. This handler has its own separate sort state (this.currentSort), uses |   raw data-sort values as property names (no sortMap), and critically calls updateSortIcons() |   which overwrites all sort icons on the page. |  |   When a column header was clicked, both handlers fired. The inline script sorted and rendered |   correctly, then productManagement.js overwrote the sort icons with its own state. Since the |   two handlers had independent direction tracking, the icons appeared stuck and sorting seemed |   to "only work once." |  |   Fix: Removed the competing sort handler registration from productManagement.js. That file's |   sort logic was designed for a products-table-body element that doesn't exist on this page |   (its renderProducts() exits early), so the sort was dead code — except for updateSortIcons() |   which was actively breaking the real sort's icon display.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Product/Management | 3. Click the Company column header — list sorts ascending. Click again — list sorts descending. Confirm both directions work. | 4. Repeat for Category column — both ascending and descending sort works | 5. Click Certificates column header — sorts numerically (by certificate count), both directions | 6. Click Identifiers column — sorts numerically (by total identifier count), both directions | 7. Switch sort columns several times — sort icon resets correctly on the inactive columns |  | PASS: All four columns (Company, Category, Certificates, Identifiers) sort both ascending AND descending; numeric columns sort by number not string.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-096 [DEFECT / Pri: - / Status: Complete]
- **Page:** Login
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Nav is appearing behind search bar and should always be top layer |  | 4/3: Still incorrect, see image 28
- **Fix summary (4/10 SP2 comment):** Both fixes from the QC #96 description are already in the code. The header has the position:   relative that makes the z-index take effect, and the .nav-wrapper has z-index: 1000 so nav  ─ |   dropdowns stack above page content.

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Certificate/CertificateTileViewInternal (or any page with the search bar near the top) | 3. Hover over a top-nav menu item to open its dropdown | 4. The dropdown menu must render IN FRONT OF the search bar and any page content below the header (not behind/clipped) | 5. Repeat on Dashboard and Admin pages — nav dropdowns always on top | 6. Confirm at desktop, tablet, and mobile widths |  | PASS: Top navigation dropdowns always render above all page content (search bar, tiles, tables) at every viewport.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-097 [DEFECT / Pri: - / Status: Complete]
- **Page:** Product Management - Add New Product popup
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** When adding a new product Company has to be filled in, cannot be left blank

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-098 [DEFECT / Pri: - / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** I got an error when trying to delete a product. (they cannot be deleted from trash can - functionality is not working) |  | If a product is tied to a certificate in the database then it should have an error that it cannot be deleted. But even when I went to delete one that had 0 for certificate it would not delete. Make the error message more clear. Also ensure this functionality is in place

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-099 [DEFECT / Pri: High / Status: Complete]
- **Page:** Create cert page - Product Identifier Import
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** I went to add 2 products from Excel and only 1 added to page. Ensure import working correctly |  | 4/6: Worked when adding 2 products via import

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-100 [WAD / Pri: - / Status: Complete]
- **Page:** Cert Detail Page
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Change Verification URL to Database Listing URL:

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-101 [WAD / Pri: - / Status: Complete]
- **Page:** Product Management
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** You don't need the word 'Search:' in these filter tags to save on space

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-102 [DATA / Pri: High / Status: Complete]
- **Page:** Product Management - Export
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Remove the test data from export (it's including 2 rows of sample data this will cause issue) |  |

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-103 [DEFECT / Pri: High / Status: Complete]
- **Page:** Product Management - Template
- **Raised by:** client
- **Sprint 2:** Dev/SIT Testing
- **Defect:** Template must include all the fields we keep on the Add New Product popup (see edits in prior issue). Right now it only has a few of the fields  |  | 4/3: The export headers don't match the fields on DB - you need to align to avoid confusion - Product Name, Category (missing), Subcategory (missing). Remove Status
- **Fix summary (4/10 SP2 comment):** Root cause: The export CSV had different columns than the import template: |   - Template: Name, Company, Description, Category, Product Type, Subcategory |   - Export: Name, Company, Category, Product Type, Subcategory, Status, Identifier Type, |   Identifier Value |  |   Mismatches: Export included Status (removed from Add Product per QC #92), was missing |   Description, and column order differed from the template. |  |   Fix: Updated the export function to align with the template and Add Product modal: |   - Removed Status column |   - Added Description column |   - Column order now matches: Name, Company, Description, Category, Product Type, Subcategory, |   Identifier Type, Identifier Value

**Baseline test:**

1. Log in as analyst (analyst / TestPass123!) | 2. Navigate to /Product/Management | 3. Click "Download Template" — open the downloaded CSV | 4. Verify headers EXACTLY: Name, Company, Description, Category, Product Type, Subcategory (no Status, no SKU, no Model) | 5. Verify NO sample data rows (headers only) | 6. Click "Export" on existing products — open the exported CSV | 7. Verify export headers MATCH the template headers exactly (same names, same order) |  | PASS: Template and Export CSVs have identical column headers in the same order, no sample data, no obsolete fields (Status/SKU/Model).

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-105 [QUESTION / Pri: - / Status: Complete]
- **Page:** Admin - Role
- **Raised by:** client
- **Sprint 2:** Dev
- **Last test result:** Review with client
- **Defect:** What is Manage Workflow option mean? What access is this? |  | Where do you see Audit Logs? |  | Update Manage Tags description to Classification tags (internal and external)
- **Fix summary (4/10 SP2 comment):** QC #105 — Admin Roles: Three Items for Discussion |  |   1. "Manage Workflow" Permission |  |   - Current state: Description updated to "Manage certificate workflow state |   transitions" |   - Reality: This permission exists in the database but is not enforced anywhere |   in code. The 14-state certificate workflow is controlled by role-based policies |   (Admin, Analyst), not by this individual permission toggle. |   - Question for client: Do you want this permission to actually control workflow |   access, or should we remove it to avoid confusion? |  |   2. Audit Logs |  |   - Current state: The app is already logging audit data to two tables: |     - AuditHistory — tracks all user actions (creates, updates, deletes) with IP, |   user agent, session info |     - CertificateStatusHistory — tracks every certificate status change with who, |   when, and duration |   - Reality: The data exists and is queryable via SQL, but there is no UI page to |   view it. |   - Question for client: Do you want an Audit Logs admin page built? If so, this |   would be a Change Request with defined scope. |  |   3. "Manage Tags" Description |  |   - Status: Done. Updated to "Classification tags (internal and external)."

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-106 [CR / Pri: High / Status: Complete]
- **Page:** Admin - User
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Add New User' popup is missing Company field (in export but not in creation box) |  | 4/3: The original comment was about adding an open text field to list the user's company, which we still need to add.  |  | I am fine with you adding the new Company Access setting but we need a check all option at top because internal users will get every one. Also, I would put them in alphabetical order by company name. |  | How would you then represent these company selections in the user list table?
- **Fix summary (4/10 SP2 comment):** All three fixes are applied to wwwroot/js/userManagement.js: |  |   1. Check All — Added a "Check All" checkbox at the top of the company list, styled with a      bottom border separator and bold text. It toggles all company checkboxes on/off, and its |   state auto-syncs when individual boxes are toggled. |   2. Alphabetical order — Companies are now sorted by name (.localeCompare()) in both the        filter dropdown and the modal checkbox list.                                                   3. Save for all roles — Changed from role === 'External' to role !== 'Partner', so Admin and   Analyst users also have their company selections saved. External still requires at least one |   selection; Admin/Analyst selections are optional. |  |   Also handled edge cases: |   - Check All resets when opening "Add New User" modal |   - Check All syncs correctly when editing an existing user (reflects if all companies are |   already selected)

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/UserManagement | 3. Click "Add New User" | 4. Verify the modal includes a Company field for ALL roles (Admin, Analyst, External, Public) — NOT only when External is selected | 5. Switch role to Partner — Company field is replaced by Partner Assignment (intentional) | 6. The Company field includes: |    - "Check All" checkbox at the top of the company list |    - Companies sorted alphabetically (case-insensitive) | 7. Save a new user with multiple companies selected — companies persist on next edit |  | PASS: Company field always visible (except for Partner role), Check All works, alphabetical order preserved, multi-company assignment saves.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-107 [DEFECT / Pri: High / Status: Complete]
- **Page:** Admin - User
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Got an error that 'Failed to save user: Failed to create user' when I tried to add one. Not working |  | 4/6: Still can't add a user. See image 42 for current error
- **Fix summary (4/10 SP2 comment):** QC #107 is already resolved. The fix wraps the BeginTransactionAsync() calls inside           _context.Database.CreateExecutionStrategy().ExecuteAsync(), which is the correct pattern for |   SQL Server's retrying execution strategy. The duplicate email check was also added before the   transaction.

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/UserManagement → Add New User | 3. Try creating a user with an EMAIL that already exists in the system — should get a clear error message: "Email already exists" (or similar) — NOT generic "Failed to create user" | 4. Try creating a user with a fresh unique email and required fields — succeeds, user appears in the list | 5. Try creating multiple users in quick succession — no transaction/retry errors |  | PASS: Duplicate email gives a clear specific error; valid creates succeed without "user-initiated transactions" errors.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-108 [DATA / Pri: - / Status: Complete]
- **Page:** Admin - User
- **Raised by:** client
- **Sprint 2:** SIT Testing
- **Defect:** Remove sample data in Template export |  | 4/6: In the template header of RequirePasswordReset -- what option would they include? (Yes/No)? |  | Same for Status list (Active/Inactive) |  | Call it User Company |  | Assuming there is no way to then select indvidiual companies someone has access to?
- **Fix summary (4/10 SP2 comment):** QC #108 — User template and export alignment per client's 4/6 feedback |  |   Issues identified from client's feedback: |   1. RequirePasswordReset — unclear what values to enter |   2. Status — unclear what values to enter |   3. Company → should be called User Company |   4. Template column order didn't match import parser at all |  |   Root cause: Template headers (Name, Email, Company, Role, Status, Password, |   RequirePasswordReset) were completely out of sync with the import parser (Name, Email, Role, |   Password, CompanyIds). Different column order, different field names, and the parser used |   internal IDs for companies instead of names. |  |   Fixes: |  |   File: userManagement.js — template |   Change: Headers now include valid-value hints: Role (Admin/Analyst/External), Status |     (Active/Inactive), RequirePasswordReset (Yes/No), User Company (semicolon-separated if |     multiple) |   ──────────────────────────────────────── |   File: userManagement.js — export |   Change: Renamed Company → User Company, removed ID column, multi-company now |     semicolon-separated to match template |   ──────────────────────────────────────── |   File: AdminController.cs — import parser |   Change: Updated to match new column order: Name, Email, Role, Password, Status, |     RequirePasswordReset, User Company. Resolves company names to IDs (not raw IDs). Parses |     Yes/No for RequirePasswordReset. Defaults: Status=Active, RequirePasswordReset=Yes

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/UserManagement | 3. Click "Download Template" | 4. Open the CSV and verify: |    - NO sample row (headers only) |    - Header names match the import parser exactly |    - "User Company" header (NOT just "Company") |    - RequirePasswordReset column hint shows valid values (Yes/No) |    - Status column hint shows valid values (Active/Inactive) | 5. Click "Export" — verify export headers match template headers exactly |  | PASS: Template has headers only with clear value hints, "User Company" naming, and import/export columns align.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-109 [DEFECT / Pri: - / Status: Complete]
- **Page:** Admin - User
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** Tried to import user through file and got failure notice, not working |  | 4/6: Still can't upload. See image 43 for message
- **Fix summary (4/10 SP2 comment):** Root cause: CreateUserAsync and UpdateUserAsync in AdminService.cs used explicit transactions   (BeginTransactionAsync) wrapped in CreateExecutionStrategy().ExecuteAsync(). While this is |   theoretically the correct EF Core pattern, it was triggering the |   SqlServerRetryingExecutionStrategy error: "does not support user-initiated transactions". |  |   Fix: Removed the explicit BeginTransactionAsync / CommitAsync / RollbackAsync and the |   CreateExecutionStrategy wrapper from both methods. This is safe because: |   - EF Core's SaveChangesAsync() is already atomic — all tracked changes are committed in a |   single database transaction internally |   - The old code had two SaveChangesAsync calls in CreateUserAsync (first to get the user ID, |   then to save relationships) — consolidated into a single call since EF Core auto-generates |   the ID on save |   - Audit logging is in a try/catch and non-critical — it won't roll back the user creation if |   it fails |  |   Both CreateUserAsync and UpdateUserAsync were fixed with the same approach.

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/UserManagement → click Import | 3. Upload a properly formatted CSV (use the template downloaded in #108) with 2-3 valid rows | 4. Import should succeed — users appear in list | 5. Try uploading a CSV with intentional errors (duplicate email, invalid role) — notification shows up to 3 specific error details (NOT just "Import failed: X errors") | 6. Verify the Company column from CSV is honored (assigned company shows on imported user's profile) |  | PASS: Valid CSV imports successfully; bad rows produce specific actionable error messages; Company column is read and applied.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-110 [WAD / Pri: - / Status: Complete]
- **Page:** Admin - Partner API Access
- **Raised by:** client
- **Sprint 2:** none
- **Last test result:** review
- **Defect:** Change description for Novi to: Novi Connect is an AI shopping optimization platform |  | Put what you have in for Novi under Wayfair: Online furniture and home goods retailer focused on sustainable product offerings |

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-111 [WAD / Pri: - / Status: Complete]
- **Page:** Admin - Certificate Types
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** In Add Certificate Type popup delete description language in blue box. It's not needed (and info is not correct) |  | Under Examples in this box change to: Recycled Content, Certified Energy Savings, etc.

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-112 [WAD / Pri: - / Status: Complete]
- **Page:** Admin - Certificate Types
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** In Add Subcategory popup delete the description language in blue box. It's not need (and info is not correct) |  | Under Examples in this box change to: Attributes under multi-attribute label

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-113 [CR / Pri: High / Status: Complete]
- **Page:** Admin - Certificate Types
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Do not allow the deletion of a Certificate Type if it has any Certificates tied to it

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-114 [WAD / Pri: - / Status: Complete]
- **Page:** Admin - Certificate Types
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Under Help update Language: |  | (e.g., "USDA Organic"….) to (e.g., Certified Sustainability Facts) | (e.g., "100% Organic"…) to (e.g., Recycled Content, Closed Loop) |  | Based on these examples update the examples in sections below

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-115 [WAD / Pri: - / Status: Complete]
- **Page:** Admin - Product  Types
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Under Edit Category box remove blue box description, info not right

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-116 [CR / Pri: - / Status: Complete]
- **Page:** Admin - Product  Types
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Update the Help examples to how we actually have them broken down -- the example you use are not accurate ones and will cause confusion

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-117 [DATA / Pri: High / Status: Complete]
- **Page:** Admin - Product  Types
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** When adding a new one it appears to keep throwing it under Chemicals and Raw Materials, which is not accurate |  | 4/6: This is not working right. I added [name redacted] Test and tried to add a type under and it created a random new section with Adhesives and Sealants, etc. under it. Functionality is not working right and I think it messed up listings which will need fixed. See image 44 |  | I also could not delete it please fix!
- **Fix summary (4/10 SP2 comment):** 1. Create category — creates in ProductCategories table, returns isCategory: true |   2. Create type under category — correctly uses CategoryId from parent category, creates in ProductTypes table |   3. Delete type — removes from ProductTypes table                                                                                                                                                                                     ─ |   4. Delete category — removes from ProductCategories table                                                                                                                                                                              5. Delete protection — prevents deleting categories with children                                                                                                                                                                     ─ |  |   The fix is working correctly. You can test it in the browser at http://localhost:5101 — log in as swest@anukiconsulting.com / TestPass123!, go to Admin > Product Types, and try adding/deleting items.      |  | QC #117 (Admin Product Types: adding items creates wrong hierarchy, delete fails) — Root cause: Fundamental data model mismatch — GetProductTypesAsync returned root nodes from ProductCategories table (with ProductCategory.Id), |   but all write operations (create/update/delete) looked up IDs in ProductTypes table. A ProductCategory.Id = 5 matched a completely different ProductType.Id = 5, causing items to be created under wrong parents. Delete failed |   because category IDs weren't found in the ProductTypes table. Fix: (1) Added IsCategory flag to ProductTypeViewModel. (2) GetProductTypesAsync sets IsCategory = true on root nodes. (3) CreateProductTypeAsync rewritten with 3 |   branches: create category, create type under category, create subcategory under type. (4) Added DeleteProductCategoryAsync for deleting from ProductCategories table. (5) All JS action buttons now pass isCategory/parentIsCategory |   through the full CRUD chain. Files: AdminModels.cs, AdminService.cs, IAdminService.cs, AdminController.cs, product-types.js. Tested: create category, create type under it, delete type, delete category, delete protection for  categories with children — all pass.

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/ProductTypes | 3. Click "Add Category" — create a new top-level category (e.g. "client Test Category") | 4. Verify it appears as a NEW root category (NOT nested under "Chemicals and Raw Materials" or any other existing category) | 5. Click the new category, then Add Type under it — create "Test Type" | 6. Verify the new type lands under YOUR new category, not a random different parent | 7. Delete the test type and category — both removed cleanly |  | PASS: New root categories render as their own top-level node; types created under a category attach to the correct parent (not defaulted to "Chemicals and Raw Materials").

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-118 [QUESTION / Pri: - / Status: Complete]
- **Page:** Certificate Field Management
- **Raised by:** client
- **Sprint 2:** Ready for UAT
- **Defect:** What do the different Section options connect to? It appears whenever I add it just goes down to Additional Information section with header in creation creation page -- so then why these different section names and what do they correlate to? |  | I don't think we need section headers and they can all just go under Additional Information  |  | 4/6: Getting error message when trying to add could not test. See image 45
- **Fix summary (4/10 SP2 comment):** Original fix (remove section headers from Create/Edit forms): DONE                            - Section column remains in admin table for internal organization |   - All custom fields render flat under "Additional Fields" on cert forms |  |   client's 4/6 follow-up (image 45 — error when adding a field): Fixed |  |   Root cause: CreateCertificateFieldAsync in AdminService.cs hardcoded CertificateTypeId = 1. |   If the production database's first CertificationType record doesn't have Id = 1 (which |   happens when identity columns don't start at 1, or the table was reseeded), the foreign key |   constraint fails, producing an error on save. |  |   Fix: Replaced the hardcoded CertificateTypeId = 1 with a dynamic lookup using |   _context.CertificationTypes.FirstOrDefaultAsync() — the same approach the seed data uses. |   Added a guard returning a clear error message if no certificate types exist. |  |   Also fixed in this session (QC #109 same root pattern): Removed explicit |   BeginTransactionAsync / CreateExecutionStrategy from CreateUserAsync and UpdateUserAsync — |   these were causing the SqlServerRetryingExecutionStrategy error that client reported in image |   43.

**Baseline test:**

1. Log in as admin (admin / TestPass123!) | 2. Navigate to /Admin/CertificateFields | 3. Add a new custom field — verify Section column is still present in admin table for internal organization | 4. Navigate to /Certificate/Create | 5. Verify all custom fields render flat under a single "Additional Fields" section — NO sub-section headers | 6. Same on /Certificate/Edit/{id} | 7. From admin, add another custom field assigned to a different Section — verify it still renders flat with all the others on Create/Edit (no extra section header) |  | PASS: Custom fields display as a single flat list under "Additional Fields" on Create/Edit; Section column visible in admin table only; adding new fields no longer errors.

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-119 [CR / Pri: - / Status: Complete]
- **Page:** Certificate Field Management
- **Raised by:** client
- **Sprint 2:** Test that custom, certificat fields. Validate that it shows up on the table view in certs.
- **Last test result:** Review
- **Defect:** Anuki -- confirm that as we add new fields it would be added automatically to the full export too?  |  | Does it get added to any of the table views?
- **Fix summary (4/10 SP2 comment):** |   1. Are new custom fields added automatically to exports? |  |   - Yes. Exports dynamically query all custom fields that have values and append them as additional columns after the standard 15 columns. When an admin creates a field and an analyst fills in a value, it appears in the next export automatically — no code |    changes needed. |  |  2. Do custom fields appear in any views? |  |   - Yes — on the Internal Certificate Detail View. Custom fields are displayed dynamically, grouped by section. When |   an admin creates a field and an analyst fills in a value, it shows up automatically. |   - No — not in the tile view tables. Those show a fixed set of columns (cert number, company, product, type, status, |    dates). |   - Question for client: Do you want custom fields added to the tile view tables as well? Append to the end of the table?

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-120 [DATA / Pri: - / Status: Complete]
- **Page:** Tags
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** Remove sample data in Template export

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-121 [DEFECT / Pri: High / Status: Complete]
- **Page:** Partners
- **Raised by:** client
- **Sprint 2:** none
- **Defect:** I added a new Partner and it does not appear in dropdown on Cert creation page

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### E.3 - N/A items

### TC-QC-122 [WAD / Pri: - / Status: N/A]
- **Page:** public view
- **Raised by:** Alice
- **Sprint 2:** none
- **Defect:** DO NOT EDIT - CORRECT AS IS PER CP -- IT WILL DEPEND ON WHAT SORT THEY HAVE ON AND BY DEFAULT IT'S COMPANY NAME | When selecting multiple certification types (CEF, CFR, CES), same certification types are not grouped together. Like certification types should be grouped together (Ex. all CEFs should be grouped together before CES are introduced)

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-125 [N/A / Pri: - / Status: N/A]
- **Page:** public view
- **Raised by:** Alice
- **Sprint 2:** none
- **Defect:** Would GPS be added under the "additional" drop down? |  | CP NOTE: Yes Alice it would be an external tag we add. No change for Anuki

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN

### TC-QC-126 [N/A / Pri: - / Status: N/A]
- **Page:** public view
- **Raised by:** Alice
- **Sprint 2:** none
- **Defect:** There can be an option to view certifcates in a grid or list POV |  | CP NOTE: I don't know what this means. The list view is for login users only. No Anuki change required

**Baseline test:**
- TODO: author test steps (no draft in QC log column F)

**Edge cases:**
- TODO during execution

**Negative cases:**
- TODO during execution

**Cross-role / Cross-viewport:** TODO during execution

**Status:** NOT YET RUN


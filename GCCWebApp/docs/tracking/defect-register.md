# GCC Defect Register

Canonical log of defects, bug fixes, and small corrective changes. Every QC fix gets a DEF-XXX entry here so we have a single audit trail.

Numbering continues from the pre-existing register (last known DEF-096 as of 2026-03-23). If an older version of this file is recovered from OneDrive or another location, merge entries by ID.

## Columns

- **ID** — `DEF-XXX`, monotonically increasing
- **Date** — when the fix was made (YYYY-MM-DD)
- **Severity** — Critical / High / Medium / Low
- **Status** — Open / In Progress / Fixed / Deployed / Closed
- **Title** — short summary
- **Details** — root cause + fix + files touched
- **Deploy** — which zip/build this shipped in (once deployed)

---

## Open / Recent

### DEF-161 — Lowercase ASIN rejected on /Certificate/Create; Create POST persisted unvalidated Product IDs

- **Date:** 2026-05-07
- **Severity:** High (go-live blocker — primary cert authoring path; data-integrity hole on save)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — "https://gccwebapp.com/Certificate/Create — validation issues. I'm unable to enter a product ID that meets validation requirements."

**Symptom:** In the Product Identifier modal on the Certificate Create form, typing a real Amazon ASIN in lowercase (e.g. `b08n5wrwnw`) was rejected with "ASIN must be exactly 10 alphanumeric characters" with no clue that the issue was case. The user had no way to know to retype in upper case, since the official Amazon ASIN format is uppercase but lowercase entries are common when users paste from URLs or notes. Compounding this: even when a value did pass the client check, the server-side `CertificateController.Create` POST persisted Product IDs **without ever calling `ProductIdService.ValidateProductIdAsync`** — only the Edit-time `/api/productids` round-trip ran format validation. So a JS bypass or client regression would silently store malformed Product IDs.

**Root cause:**
1. `wwwroot/js/product-id-manager.js:861` — ASIN regex was `/^[A-Z0-9]{10}$/` (case-sensitive). All other type regexes that involve letters (`SKU`, `MPN`) already used the `i` flag; ASIN was the lone exception.
2. `Services/ProductIdService.cs:712` (`ValidateAsin`) — same case-sensitive regex on the server. The `type.ToUpper()` call in `ValidateProductIdAsync` only normalised the type, not the value.
3. `Controllers/CertificateController.cs` Create POST (line 480-495) — added each `model.ProductIds` entry directly to `certificate.ProductIds` and saved via `_unitOfWork`, skipping `_productIdService.ValidateProductIdAsync` entirely. The Edit POST had the same gap.

**Fix:**
- **Client (`wwwroot/js/product-id-manager.js`):**
  - ASIN regex now `/^[A-Z0-9]{10}$/i`.
  - New `normalizeProductIdValue(type, value)` uppercases the value for the seven typed-code formats (ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN); `OTHER` stays freeform.
  - `handleFormSubmit` runs the value through the normaliser before pushing to in-memory list / posting to API.
- **Server (`Services/ProductIdService.cs`):**
  - New `public static NormalizeValueForType(string? type, string? value)` helper mirrors the client rule.
  - `ValidateProductIdAsync` normalises the value before regex match, so direct API calls with lowercase ASIN now pass.
  - `CreateProductIdAsync` and `UpdateProductIdAsync` canonicalise type + value at the top of the method, so the DB never stores a lowercase ASIN regardless of caller.
  - `ImportProductIdsAsync` (CSV bulk path) normalises `ProductIdValue` on insert.
- **Server (`Controllers/CertificateController.cs`):**
  - Both Create POST and Edit POST run a pre-validation pass over `model.ProductIds`: canonicalise type + value via `ProductIdService.NormalizeValueForType`, then `_productIdService.ValidateProductIdAsync` each row, adding `ModelState` errors keyed `ProductIds[i].ProductIdValue` for invalid entries. Submission falls back to the same View with errors surfaced inline.

**Files changed (4):**
- `wwwroot/js/product-id-manager.js`
- `Services/ProductIdService.cs`
- `Controllers/CertificateController.cs`
- `docs/tracking/defect-register.md` (this entry)

**Verification (localhost, http://localhost:5101):**
1. **Validation function truth table** (`verify-isbn.mjs`): Before fix — `asin_lower=false`, `asin_mixed=false`. After fix — `asin_lower=true`, `asin_mixed=true`, all other types unchanged.
2. **Server `/api/productids/validate`** (`verify-server.mjs`): lowercase ASIN now returns `data: true`; mixed-case + lowercase type returns `data: true`; genuinely bad ASIN (`TOO_SHORT`) still returns `data: false`.
3. **End-to-end Create** (`e2e-create.mjs`): Form filled with company, product, cert type, dates, tiny PDF, and a Product ID with **lowercase** value `b08abc1234`. Submit succeeded; redirect to `/Certificate/CertificateInternalView/26-0158`. Direct SQL confirmation: `SELECT ProductIdType, ProductIdValue FROM CertificateProductIds WHERE CertificateId IN (SELECT Id FROM Certificates WHERE Number='26-0158')` returned `ASIN | B08ABC1234` — value canonicalised on save, not stored as the lowercase form the user typed.
4. **Genuinely-bad inputs still rejected:** `B08N5WR` (short), `12345` (UPC short), `1234567` (GTIN short), `1234567890123` (ISBN bad prefix), `AB` (SKU short), 60-char MPN.

**Audit table (severity ranked):**

| # | Defect | Where | Severity | Status |
|---|--------|-------|----------|--------|
| A | ASIN regex case-sensitive (client) | product-id-manager.js:861 | High | Fixed |
| B | ASIN regex case-sensitive (server) | ProductIdService.cs:712 | High | Fixed (via NormalizeValueForType pre-pass) |
| C | Create POST persists Product IDs without format validation | CertificateController.cs Create POST | High | Fixed (pre-validation pass added) |
| D | Edit POST persists Product IDs without format validation (same gap) | CertificateController.cs Edit POST | High | Fixed (same pre-validation pass) |
| E | UPC/EAN client-side missing checksum (length-only) | product-id-manager.js:862,864 | Medium | **Deferred** — server now enforces on Create + Edit + API + bulk import; client length check is fast feedback. Adding checksum to client is a UX-only enhancement and risks blocking valid UPCs the user has already verified externally. |
| F | Bulk CSV import didn't normalise value | ProductIdService.cs:511 | Medium | Fixed (uses NormalizeValueForType) |

**Deploy:** Pending. Restart required (server-side changes).

---

### DEF-160 — Login does not route users to their role-appropriate certificates page

- **Date:** 2026-05-07
- **Severity:** Medium (functional — every authenticated session lands on a generic page; correct page is one extra click away)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — testing login flow on `gccwebapp.com`. Reported "When you login, for example external, partner should go to external certificates. Analyst and Admin should go to internal certificates. Currently, it just goes to certificates."

**Symptom:** Successful login dropped users on a generic certs page regardless of role. Specifically: External landed on the External tile view (correct), Partner landed on the public `CertificateTileView` (wrong — should be External tile view), Analyst and Admin landed on the legacy Dashboard `Index` (wrong — should be Internal tile view).

**Root cause:** Two switch tables were out of sync with the intended landing-page mapping:
1. `Controllers/AccountController.cs` `RedirectToLocal` — had no `"Partner"` case, so Partner fell through to the default (`CertificateTileView`, public). Analyst and Admin cases routed to `Dashboard/Index` rather than `CertificateTileViewInternal`.
2. `Controllers/HomeController.cs` `Index` — same issue. No Partner case, Analyst/Admin routed to Dashboard.

**Fix:** Updated both switch tables. Final mapping for authenticated roles:
- `Public` → `CertificateTileView`
- `External` → `CertificateTileViewExternal`
- `Partner` → `CertificateTileViewExternal`
- `Analyst` → `CertificateTileViewInternal`
- `Admin` → `CertificateTileViewInternal`

Auth policies are already correct — `ExternalAccess` permits External/Partner/Admin/Analyst, `AnalystOrAdmin` permits Admin/Analyst — so the redirect targets are reachable by their respective roles. The `Url.IsLocalUrl(returnUrl)` short-circuit in `RedirectToLocal` is preserved, so deep-link returns after session timeout still work.

**Files:** `Controllers/AccountController.cs`, `Controllers/HomeController.cs`.

**Verification (pending — please QC after deploy):** Log in fresh (no `returnUrl`) as each role. External and Partner should land on `/Certificate/CertificateTileViewExternal`. Analyst and Admin should land on `/Certificate/CertificateTileViewInternal`. With a `returnUrl` (deep-link forced through login), users still return to the original URL.

---

### DEF-159 — Tablet: "Template" import button stretches to 100% width on the row beneath the other action buttons (Internal cert tile view)

- **Date:** 2026-05-07
- **Severity:** Medium (visible layout — primary cert listing page, Admin role, tablet portrait)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — viewing `https://gccwebapp.com/Certificate/CertificateTileViewInternal` on tablet (≤768px). Action button row above the tile grid was wrapping the trailing "Template" button onto its own line and the wrapped button was inflating to fill 100% of the row width while every other button remained content-sized.

**Symptom:** The Internal tile view shows five action buttons in `.export-options` for Admin: Add Certificate / Download Selected / Export All to CSV / Import / Template. At ~768px viewport width, four buttons fit on row 1 and "Template" wraps onto row 2 alone — but instead of staying at its content width (~110px), it stretched to 100% of the container. External tile view was unaffected because that view has only two buttons in `.export-options` and they fit on a single row even at tablet width.

**Root cause:** `wwwroot/css/main.css` had a single `@media (max-width: 768px)` block that applied both `.export-options { justify-content: stretch; }` and `.export-options .btn { flex: 1; min-width: 140px; }`. The `flex: 1` on a flex item that wraps onto a row by itself causes that item to grow to the full available row width — exactly the observed behavior. The intent of `flex: 1` was to make buttons full-width on phones (single column layout, larger tap targets), which is fine at phone widths where every button wraps; the bug only surfaces at tablet widths where exactly one button wraps alone.

**Fix:** Split the rule by breakpoint in `wwwroot/css/main.css`:
- Tablet `≤768px`: keep `min-width: 140px` on the buttons but drop `flex: 1` and `justify-content: stretch`. Wrapped buttons now stay at content width.
- Phone `≤480px`: re-introduce `flex: 1` and `justify-content: stretch` for the full-width tap-target behavior on phones.

This preserves the phone UX while removing the tablet inflation. Internal view's "Template" button now sits at content width on row 2 instead of spanning full width. External view is unchanged by the fix because its two buttons still fit on row 1 at tablet width.

**Files:** `wwwroot/css/main.css` only (one media-query block split into two).

**Verification (pending — please QC after deploy):** Open `/Certificate/CertificateTileViewInternal` as Admin on iPad portrait (~768px). Action button row should show Add Certificate / Download Selected / Export All to CSV on row 1, and Import + Template both at content width on row 2 (or all on row 1 at slightly wider tablets). On phone (~375px) buttons should still stack full-width as before.

---

### DEF-158 — External tile view sends Partner/External users to AccessDenied when clicking a cert tile

- **Date:** 2026-05-07
- **Severity:** **High** (functional break — Partner role cannot view any cert detail page from the tile view)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — logged in as Partner role on `gccwebapp.com`, clicked into cert 26-0157, landed on `/Account/AccessDenied?ReturnUrl=%2FCertificate%2FCertificateInternalView%2F26-0157`. Same break for any External user.

**Symptom:** Both External and Partner roles can reach the External tile view (`/Certificate/CertificateTileViewExternal`) per the `ExternalAccess` policy (External + Partner + Admin + Analyst). But clicking a tile opens `/Certificate/CertificateInternalView/{number}` in a new tab — and that route is gated by `AnalystOrAdmin` policy. Partner + External fail the policy check and get redirected to AccessDenied.

**Root cause:** `wwwroot/js/certificate-tile-view-external.js` had hardcoded the wrong route in two places (lines 1146 and 1155):
```js
window.open(`/Certificate/CertificateInternalView/${encodeURIComponent(certificateNumber)}`, '_blank');
```
Both `viewCertificate(num)` (button click / context menu) and `viewCertificateDetails(num)` (tile click) pointed at the Internal route. The External route (`CertificateExternalView`) is the correct target — it's gated by `ExternalAccess`, which all four roles satisfy.

There is also a stale orphan file `wwwroot/js/certificateTileViewExternal.js` (camelCase) that already had the correct external route. It is not loaded by any view. Did not touch it as part of this fix.

**Fix:** Changed both `window.open(...)` calls in `wwwroot/js/certificate-tile-view-external.js` to point at `/Certificate/CertificateExternalView/{number}`. The Internal tile view's JS (`certificate-tile-view-internal.js`) is unchanged — it still routes to InternalView correctly for Admin/Analyst.

**Files:** `wwwroot/js/certificate-tile-view-external.js` only.

**Verification:** Reproduced Shawn's exact symptom locally. Logged in as External user (same `ExternalAccess` policy as Partner), navigated to External tile view, called `window.viewCertificate('26-0146')` and `window.viewCertificateDetails('26-0146')` with `window.open` stubbed — both returned `/Certificate/CertificateExternalView/26-0146` post-fix (was `CertificateInternalView` pre-fix). Direct GET on `/Certificate/CertificateInternalView/26-0146` as External user reproduced the AccessDenied redirect Shawn hit; direct GET on `/Certificate/CertificateExternalView/26-0146` returns 200.

**Note on dev DB:** No Partner-role users exist in the local dev DB (Roles table has Partner at id 7 but UserRoles has no rows for it). Verified via External which shares the same `ExternalAccess` policy and the same JS path.

---

### DEF-157 — Cert detail view forces portrait box on landscape cert images (squished/letterboxed thumbnails)

- **Date:** 2026-05-07
- **Severity:** Medium (visible layout — primary cert detail page)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — viewing cert 26-0157 on `gccwebapp.com` desktop, the cert preview was showing in a tall portrait container with the landscape cert image squished into a tiny letterboxed strip surrounded by whitespace. "Why did this break?"

**Symptom:** GCC cert thumbnails come in both portrait (Letter 8.5×11) and landscape orientations. The `.certificate-preview-box` rule in `wwwroot/css/main.css` was applying `aspect-ratio: 8.5 / 11` to every preview box. With `object-fit: contain` on the inner img, that meant: portrait certs filled the box correctly, but landscape certs (e.g., 26-0157's 600×464 thumbnail, 26-0146's 600×462 thumbnail) were scaled down to fit the box width and centered vertically with huge whitespace top and bottom.

**Why "did this break":** The `aspect-ratio: 8.5 / 11` constraint and the matching base `.certificate-preview-box img` rules were added to main.css recently to fix DEF-156 (image rendering at natural pixel size and overflowing the column on tablet landscape). The first fix solved the overflow but introduced the new shape constraint, which is wrong for landscape certs. Net effect on tablet landscape:

| State | Portrait certs | Landscape certs |
|---|---|---|
| Before portrait-box rule | OK | Image overflowed column ❌ |
| After portrait-box rule (current live) | OK | Image squished into tiny letterbox ❌ |

**Root cause:** Forced portrait `aspect-ratio` is wrong for the dataset — cert thumbnails do not all have the same orientation. A rigid container shape can never simultaneously match both. Earlier today's "DEF-156 fix" today was a no-op duplicate against rules already deployed live (verified by pulling `https://gccwebapp.com/css/main.css` — 133,563 bytes match, rules already present).

**Fix:** Removed `aspect-ratio: 8.5 / 11`, `display: flex`, and `align-items/justify-content: center` from `.certificate-preview-box`. Switched img from `position: absolute; width:100%; height:100%; object-fit: contain` to `position: relative; display: block; width:100%; height: auto`. Container now sizes itself to the natural aspect ratio of whatever image loads. Added `min-height: 200px` so the no-PDF placeholder still has somewhere to sit.

**Files:** `wwwroot/css/main.css` only.

**Verification:** Reproduced and re-tested cert 26-0146 (600×462 landscape thumbnail) on localhost at desktop (1440×900), tablet landscape (1024×768), tablet portrait (768×1024), and mobile (390×844) via Playwright. Pre-fix: container was 8.5/11 portrait shape (e.g., 316×471 at tablet landscape), image rendered at 314×244 in the middle with letterbox whitespace. Post-fix: container = image natural aspect (e.g., 316×244), image fills container, 0 overflowing elements at every viewport. Screenshots: `C:\Users\swest\AppData\Local\Temp\uitest-pidsort\screenshots\fix-26-0146-*.png`.

---

### DEF-156 — Certificate image overflows viewport on tablet landscape (1024px) in cert detail views

- **Date:** 2026-05-07
- **Severity:** Medium (visible layout break on a primary tablet viewport)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — `https://gccwebapp.com/Certificate/CertificateInternalView/26-0146` on tablet landscape, certificate preview image was rendering outside the card and not wrapping.

**Symptom:** On the cert detail page (Internal AND External views) at tablet landscape ~1024px, the bare `<img>` inside the right-column preview box rendered at its natural pixel size (~612px wide) and overflowed the column / viewport. Layout was correct on mobile (≤768px) and on full desktop, which masked the bug.

**Root cause:** `wwwroot/css/certificates.css` defines the base `.certificate-preview-box` and `.certificate-preview-box img` rules (`aspect-ratio: 8.5/11`, `overflow: hidden`, `position: absolute; width: 100%; height: 100%; object-fit: contain`), but `Views/Shared/_Layout.cshtml` only links `main.css`. The certificates.css bundle is referenced in `appsettings.Performance.json` but never linked in the layout. main.css only had a `≤768px` `@media` override for the preview-box img — at any viewport above 768px the img was unstyled and rendered at its intrinsic pixel size.

**Fix:** Added the missing base `.certificate-preview-box`, `.certificate-preview-box img`, and `.preview-placeholder` rules to `wwwroot/css/main.css` (above the existing tablet `@media (max-width: 1024px) and (min-width: 769px)` block), so they apply at every viewport. Did NOT add the `<link>` to certificates.css to avoid pulling in unrelated rules.

**Files:** `wwwroot/css/main.css` only.

**Verification:** Played through `/Certificate/CertificateInternalView/26-0146` and `/Certificate/CertificateExternalView/26-0146` at 768 (portrait), 1024 (landscape), 1180 (iPad Air landscape), 1280 (small desktop) via Playwright. Pre-fix: 1 element overflowing at 1024px (the bare `<img>`, width 612, right edge at x=1252). Post-fix: 0 overflowing elements at every tested viewport. Image now sits in the aspect-ratio box with `object-fit: contain`. Screenshots in `C:\Users\swest\AppData\Local\Temp\uitest-pidsort\screenshots\26-0146-*.png`.

---

### DEF-155 — Database Listing URL overflows viewport on mobile in cert detail views

- **Date:** 2026-05-07
- **Severity:** Low (cosmetic / mobile layout)
- **Status:** Fixed locally (pending deploy)
- **Reporter:** Shawn — reviewing `https://gccwebapp.com/Certificate/CertificateInternalView/26-0146` on mobile; the rendered Database Listing URL anchor (`https://gccwebapp.com/Certificate/Verify/26-0146`) extended past the right edge of the card.

**Symptom:** Long URL displayed as anchor text has no natural break points; on narrow viewports it stretched the container and pushed content off-screen.

**Root cause:** Anchor element had no wrapping rule. URLs without spaces or hyphens won't wrap by default — they need explicit `word-break` or `overflow-wrap` to break mid-string.

**Fix:** Added inline `word-break: break-all; overflow-wrap: anywhere;` to the verify-URL anchor in both `Views/Certificate/CertificateInternalView.cshtml` and `Views/Certificate/CertificateExternalView.cshtml`.

**Files:** `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`.

---

### DEF-154 — "Replace all existing" toggle silently dropped when import has no "new" rows; partial imports could deactivate matching rows

- **Date:** 2026-05-07
- **Severity:** **High** (data integrity — could deactivate hundreds of matching rows in the partial-import case)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — re-imported the 1000-row CSV against cert 26-0023 with "Replace all existing" checked; the lone stray row that wasn't in the file was NOT deactivated.

**Symptom:** On the cert Edit page's CSV import preview, checking "Replace all existing" was supposed to deactivate any existing product ID on the cert that wasn't in the import file. After the DEF-153 fix made dup-detection see all pages, re-importing the same file classified all matching rows as "modified" or "unchanged" with zero "new" — and the Replace-all logic silently did nothing.

**Root cause:** Two failure modes in `wwwroot/js/product-id-manager.js` `applyImport()`:
1. **All rows unchanged.** `selectedRows.length === 0` (unchanged are auto-deselected), so the function returned with "No rows selected for import" before even reading the toggle. Replace-all toggle ignored entirely.
2. **All rows modified, zero new.** `creates = selectedRows.filter(r => r.action !== 'update')` was empty, so the bulk endpoint was never called — the `overwriteExisting` flag never reached the server, and the server's deactivation loop never ran.
3. **Partial — some new + some modified.** This was the worst case. The bulk POST only sent "creates" (new rows), so the server's `importKeys` set contained ONLY those new keys. The server's deactivation loop then marked every existing row whose key wasn't in `importKeys` — including the modified rows the user wanted to keep — as Inactive. PUT updates ran afterwards on rows that were now deactivated.

**Fix:**
- When `overwriteExisting` is checked, the import set is now ALL non-error rows in the import file (regardless of per-row selection). The toggle implies "the file is the new source of truth," and per-row checkboxes lose meaning in that mode. The empty-selectedRows guard now returns a different error message in this branch ("Import file has no valid rows. Replace-all needs at least one valid row to define the new state.") instead of "No rows selected for import."
- The bulk endpoint payload now includes EVERY row in the import set (including updates) when `overwriteExisting` is true, not just `creates`. This makes the server's `importKeys` set complete — the deactivation loop only deactivates rows that are truly missing from the file. The server's bulk handler already silently skips dup keys in overwrite mode (counted as success, no insert), so sending matching rows is harmless on the insert path.
- PUT updates for "modified" rows still run individually after the bulk call, to apply field changes (the bulk endpoint silently skips dup keys without applying field updates).

**Files:** `wwwroot/js/product-id-manager.js` only.

**Verification:** Verified via DB inspection. Reset cert 26-0023 to 1001 active rows (1000 ASINs from import file + 1 stray row `1321321212`). Imported the 1000-row file with "Replace all existing" checked. After the apply: `SELECT COUNT(*) WHERE IsActive = 1` returns **1000** (was 1001). The stray row is now Inactive. Pre-fix behavior: would still be 1001 active.

**Out of scope (for future CR if needed):** The bulk endpoint's "silent skip on dup-key in overwrite mode" semantics mean field-level changes on matching rows aren't applied via the bulk path. This CR routes those through the existing PUT-individually loop, which works but emits N requests for N modified rows. If we ever see Replace-all imports with thousands of field changes hitting the rate limiter, we should change the server-side bulk endpoint to update fields on dup-key in overwrite mode (one round-trip, not N). Not blocking launch — current scale is fine.

---

### DEF-153 — Import preview falsely classifies existing rows as "new" after CR-018 server-paging shipped

- **Date:** 2026-05-07
- **Severity:** High (data integrity — would create duplicate rows on re-import)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — re-imported `product-ids-sample-1000-asins.csv` after the first import committed, expecting all 1000 rows to flag as "Unchanged"; instead "a few were tagged unchanged, but the majority said new."

**Symptom:** On the cert Edit page, the CSV import preview compared each imported row against `this.productIds` (the JS in-memory state) to classify it as new / modified / unchanged. After CR-018 introduced server-side pagination, `this.productIds` only holds the currently visible page (≤ 50 rows). So on a cert with 1001 ASINs, the dup-detection only saw the 50 rows on page 1 — every imported value not on that page was flagged "new" even though it already existed in the DB. Clicking Apply Import in that state would have created 950+ duplicate rows.

**Root cause:** `wwwroot/js/product-id-manager.js` `buildImportPreview()` does `this.productIds.find(p => …)` to look up an existing row matching the import's type+value. That semantic was correct when `this.productIds` held the full list (pre-CR-018), but is wrong post-CR-018 because the array is now scoped to one page.

**Fix:** When in server-paged mode (`serverPaged && certificateId`), `buildImportPreview` now pre-fetches the full list once via the existing un-paged endpoint `GET /api/productids/certificate/{id}` (no `page` param → returns all rows in `data[]`), builds a `Map` keyed by `${type.toUpperCase()}:${value.toUpperCase()}`, and looks up each import row in that map instead of scanning `this.productIds`. Falls back to the original in-memory scan if the pre-fetch fails or in non-paged mode (Create page).

**Files:** `wwwroot/js/product-id-manager.js`.

**Verification:** Playwright probe (`test-def153-import-dedup.mjs`) seeds 100 import items whose values match existing rows on cert 26-0023 (most are NOT on page 1). Pre-fix: all 100 would be "new". Post-fix: 0 new, 100 modified. 5/5 assertions pass.

**Out of scope:** The pre-fetch loads the full list into memory in the JS — fine for 1000 rows, may need re-thinking at 100,000+. If we hit that scale, switch to a server-side `lookup-keys` endpoint that returns just `{type, value}` pairs (smaller payload). Not blocking launch.

---

### DEF-152 — Product Identifier pagination buttons didn't match Product Management's pagination styling

- **Date:** 2026-05-06
- **Severity:** Low (visual / UI consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on cert Create / Edit pages while comparing against `/Product/Management`.

**Symptom:** The Product Identifier pagination on cert Create / Edit pages rendered as five inline-styled text buttons — `[First]` `[Prev]` `Page X of Y` `[Next]` `[Last]` — with hand-rolled CSS in the JS template (border, padding, font-size, background all written as inline `style="…"`). The Product Management page (and the rest of the admin app) uses chevron Prev / numbered pages with ellipsis / chevron Next, styled via the canonical `.pagination-btn` and `.pagination-controls` classes. Two visibly different paginations on the same admin app — Shawn called this out as a clear consistency break.

**Root cause:** `wwwroot/js/product-id-manager.js` `renderPagination()` was authored before the admin pagination convention (`.pagination-btn` / `.pagination-controls` defined in `wwwroot/css/product-management.css:1556-1601`) was established, and was never retrofitted. The "First / Last" navigation also wasn't in the canonical pattern — Product Management only ships chevron Prev + numbered + chevron Next.

**Fix:**
- `wwwroot/js/product-id-manager.js` `renderPagination()` rewritten to mirror `Views/Product/Management.cshtml:740-793` — chevron-left Previous button, numbered page buttons (max 7 visible with `...` ellipsis at edges), chevron-right Next button, all using `class="pagination-btn"` (with `active` / `disabled` modifiers). Active page also gets `aria-current="page"`. Container wrapper uses `pid-pagination-wrapper` (left: "Showing N-M of T" info, right: `.pagination-controls` button row).
- `wwwroot/css/product-id-manager.css`: Appended scoped pagination styles under `.product-id-manager .pagination-*` mirroring `wwwroot/css/product-management.css:1556-1601`. Active button hardcoded to `#002B76` rather than `var(--blue-dark)` because the var is overridden to `#002876` in some parent contexts; matching the rendered output of Product Management is the goal, so the literal wins.

**Files:** `wwwroot/js/product-id-manager.js`, `wwwroot/css/product-id-manager.css`.

**Verification:** Playwright test at `C:\Users\swest\AppData\Local\Temp\uitest-pidsort\test-pagination.mjs` — 20/20 pass. Pushed 51 in-memory product IDs to force pagination, then asserted: wrapper class, button class structure, no inline background styles, chevron-left Prev (disabled on p1), chevron-right Next (disabled on last page), `.active` + `aria-current="page"` on current page, "Showing N-M of T" info text updates on page change, computed background `rgb(0, 43, 118)` matches Product Management exactly, computed dimensions (36px min-width, 44px rendered height) match Product Management exactly.

---

### DEF-151 — "Unsaved form data recovered from HH:MM" banner hidden pending Phase 2 rework

- **Date:** 2026-05-06
- **Severity:** Low (UX clean-up)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed the false-positive banner on the cert Create page during DEF-150 testing.

**Symptom:** The Create / Edit cert pages displayed a yellow recovery banner reading *"Unsaved form data recovered from HH:MM. Would you like to restore it?"* with [Restore] / [Discard] buttons. Banner fired even on freshly opened pages where the user had no real prior session — cascading dropdowns (Company → Product, Category → Type → Subcategory, Cert Type → Subcategories) retrigger `change` events on initial load and the unsaved-state tracker reads those as edits.

**Root cause (current banner has multiple problems):**
- False positives: `UnsavedChanges.rebaselineUnsavedChanges()` runs on a `setTimeout(..., 1000)` after page load, but the recovery check (`checkForRecovery()`) runs earlier in the same flow and sees the cascade-driven values as "differences" against the saved payload.
- Partial restore: the restore handler explicitly skips `__RequestVerificationToken`, `CertificateFile`, and `ThumbnailFile` (`certificate-create.js:766`). A user who clicks Restore gets text fields back but no cert PDF, no thumbnail. They'd then click Save and either fail validation (PDF required on Create) or persist a half-edit.
- No coverage of product IDs, custom field values, or tags — the recovery payload only includes standard form fields.
- Time-only timestamp without a date.

**Decision:** Hide the banner now, rebuild it as a trustworthy experience in Phase 2. The auto-save infrastructure stays — only the recovery prompt is suppressed — so the saved data is still there for Phase 2 to consume cleanly.

**Fix:**
- `wwwroot/js/certificate-create.js`: Added a `SHOW_RECOVERY_PROMPT: false` kill switch on the `UnsavedChanges` module. `checkForRecovery()` short-circuits when the flag is off but still GCs entries older than 2 hours from `localStorage` so storage doesn't leak indefinitely. Corrupt JSON entries are also cleared rather than crashing on parse.

**Files:** `wwwroot/js/certificate-create.js`.

**Phase 2 follow-up:** CR-017 — Unsaved-form recovery banner UX rework. See `docs/tracking/CR-017-form-recovery-ux.md` for full Phase 2 scope (false-positive elimination, full-surface payload, all-or-nothing restore, better copy, same-cert scoping).

**Verification:** Reload `/Certificate/Create` or `/Certificate/Edit/{id}`. Banner should not appear under any condition. Open DevTools → Application → localStorage to confirm auto-save entries still write (key is set in `UnsavedChanges.storageKey`).

---

### DEF-150 — Product Identifier table missing column sort that other admin tables have

- **Date:** 2026-05-06
- **Severity:** Low (UX consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on cert Edit page after the DEF-149 hint shipped.

**Symptom:** The Product Identifier table on `/Certificate/Create` and `/Certificate/Edit/{id}` had non-clickable column headers (`<th width="80">Type</th>` etc.) with no sort affordance. Every other admin table in the app — Product Management, Tags, Pathways, User Management, Companies — supports click-to-sort with visual chevron icons (`▼` default, `▲ / ▼` per direction). Inconsistent behavior at the same UI layer.

**Root cause:** The Product ID Manager partial (`Views/Shared/_ProductIdManager.cshtml`) was authored before the `admin-table-th--sortable` convention was established and never retrofitted. The JS (`wwwroot/js/product-id-manager.js`) only handled filter + search; sort state didn't exist.

**Fix:**
- `Views/Shared/_ProductIdManager.cshtml`: Each column header (Type, Product ID, Product Name, SKU, Partner, Created, Created By) now uses `data-sort="…"` + `class="admin-table-th--sortable"` + `<span class="sort-icon admin-sort-icon">▼</span>`. Mirrors `Views/Product/Management.cshtml:106-113`. Actions column intentionally not sortable.
- `wwwroot/js/product-id-manager.js`:
  - Added `sortColumn` / `sortDirection` constructor state.
  - Added `initializeSortableHeaders()` (called from `init()`) — click toggles asc/desc on same column, switches and resets to asc on different column. Updates icons and `active-sort` class.
  - Added `applySort()` — alpha sort (locale-aware) for Type / Value / Name / SKU / Partner / Created By; numeric timestamp sort for Created. `createdBy` resolves through `createdByUser.username` first, falling back to the raw `createdBy` field for in-memory items that haven't been persisted yet.
  - Wired into `renderTable()` so every render path picks up the active sort (load from API, in-memory add/edit/delete on Create page, filter/search, import-apply). Idempotent when no column is selected — preserves server order.

**Files:** `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`.

**Verification:** Reload `/Certificate/Edit/{any-id}`. Click column headers to confirm sort behavior matches Product Management. Type a search query — sort should persist. Add a new product ID — table re-renders honoring active sort.

---

### DEF-149 — Edit page: Product ID save semantics differ from Create page (no UX signal)

- **Date:** 2026-05-06
- **Severity:** Low (UX clarity)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — surfaced while auditing orphan-record risk between Create and Edit.

**Symptom:** The Product ID Manager on `/Certificate/Create` holds Add/Edit/Delete/Import in browser memory and only commits on cert Save (single transaction with the parent cert). On `/Certificate/Edit/{id}`, the same UI commits each operation immediately to the DB via `/api/productids/*` endpoints, so clicking Cancel on the cert form does NOT revert product ID changes. Two save semantics behind the same UI surface.

**Root cause:** `wwwroot/js/product-id-manager.js` branches on `certificateId === 0`: zero → in-memory mode (Create), positive → immediate-API mode (Edit). No visual indicator of which mode is active.

**Decision:** Keep immediate-save behavior on Edit. The "fix a wrong ASIN quickly" workflow benefits from immediate persistence — an analyst who spots a bad identifier shouldn't have to commit unrelated cert-form changes to remove it. Refactoring to deferred-save (option #2) was started and reverted after Shawn flagged the use case. Instead, surface the behavior with a UI hint (option #1).

**Fix:** Added an info note on `Views/Certificate/Edit.cshtml` directly above the Product ID Manager, styled with the app's blue accent (#002B76), 16px, with `fa-info-circle` icon. Text reads: *"Add, edit, and delete actions on product identifiers save immediately and apply even if you cancel the certificate edit. Use this if you need to quickly correct a wrong identifier."*

**Orphan-risk note (verified during this audit, not a defect):** `CertificateProductId.CertificateId` is NOT NULL with `OnDelete(DeleteBehavior.Cascade)` (`ApplicationDbContextModelSnapshot.cs:2143-2149`). Schema makes FK orphans impossible — deleting a cert cascades to its product IDs.

**Files:** `Views/Certificate/Edit.cshtml`.

**Out of scope:** Edit submit handler still sends a hardcoded `Status: 'Active'` in hidden inputs (`Edit.cshtml:862`), which contradicts CR-015 ("Status follows cert"). Harmless because the controller's merge logic (`CertificateController.cs:869-911`) doesn't update Status from the submission, but it's stale code. Not addressing in this fix to keep scope tight.

**Verification:** Reload `/Certificate/Edit/{any-id}` and confirm the blue note renders above the Product ID Manager. Behavior unchanged.

---

### DEF-148 — Product ID Manager: "Product ID" column rendered in monospace and the row count was center-aligned, both inconsistent with the rest of the app

- **Date:** 2026-05-06
- **Severity:** Low (visual / UI consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on `/Certificate/Edit/26-0146`, two related sub-items raised back-to-back.

**Symptom 1 — column font:** The "Product ID" column in the Product Identifiers section rendered values in `'Courier New', monospace` at `font-weight: 500`. Every adjacent column (Type, Product Name, SKU, Partner, Created, Created By) inherited the default proportional Open Sans, so the values column visually popped out as bolder typewriter text against the rest of the row.

**Symptom 2 — count alignment:** The "Showing N product ID(s)" pagination caption beneath the table was centered. The rest of the app right-aligns count captions next to their tables (admin tables, cert tile views, etc.).

**Root cause:**
- `wwwroot/css/product-id-manager.css:215` — the `.product-id-value` class carried `font-family: 'Courier New', monospace; font-weight: 500;` overrides on top of the truncation properties. Likely a leftover from when the column was treated like a code/identifier display; no other column in the app uses monospace.
- `Views/Shared/_ProductIdManager.cshtml:161` — the pagination wrapper used `class="pagination-info text-center text-muted mt-2"` (Bootstrap `text-center`).

**Fix:**
- CSS: dropped the `font-family` and `font-weight` declarations from `.product-id-value`; kept the truncation properties (`max-width`, `overflow`, `text-overflow`, `white-space`).
- Razor: `text-center` → `text-end`.

**Files:** `wwwroot/css/product-id-manager.css`, `Views/Shared/_ProductIdManager.cshtml`.

**Out of scope (intentional):** The Import Preview modal's preview row still inlines `font-family: monospace` on the `productIdValue` cell (`wwwroot/js/product-id-manager.js:1025`). That's a separate modal not visible on the cert edit page; leaving it for now since the user only flagged the main table — can revisit if it shows up in QC.

**Verification:** Static asset changes; reload `/Certificate/Edit/26-0146` to confirm.

---

### DEF-147 — Product ID Manager action icons (Edit/Delete) didn't match the app standard; trash icon wasn't red

- **Date:** 2026-05-05
- **Severity:** Low (visual / UI consistency)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on prod cert `https://gccwebapp.com/Certificate/Edit/26-0160`.

**Symptom:** Action icons in the Product ID Manager rendered as Bootstrap outlined pill buttons (`btn btn-outline-primary` / `btn btn-outline-danger`) — square borders, larger footprint, and the delete icon used the muted Bootstrap danger outline rather than the bright red the rest of the app uses for destructive actions. Inconsistent with the canonical `admin-inline-action-btn--edit/--delete` pattern used on Pathways, Partners, Tags, etc.

**Root cause:** Two render sites diverged from the standard:
- `Views/Shared/_ProductIdManager.cshtml` rendered server-side rows with `btn btn-outline-primary` / `btn btn-outline-danger` inside a `btn-group btn-group-sm`.
- `wwwroot/js/product-id-manager.js` (`getProductIdRowHtml`) rendered client-side rows with the same legacy classes.

In addition, the canonical CSS `wwwroot/css/admin/admin-inline-editing.css` (which defines `.admin-inline-action-btn--edit` as `#00AEEF` blue and `.admin-inline-action-btn--delete` as `#FF5252` red) was not linked from either of the host pages (`Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`).

**Fix:**
- Replaced the action button markup at both render sites with the standard `<div class="admin-inline-actions">` wrapper plus `<button class="admin-inline-action-btn admin-inline-action-btn--edit">` / `--delete`. Kept the existing `btn-edit-product-id` / `btn-delete-product-id` hooks alongside the new classes so the JS event handlers continue to bind. Added `aria-label` per the app's accessibility convention.
- Linked `~/css/admin/admin-inline-editing.css` from both Create.cshtml and Edit.cshtml.

**Files:** `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`.

**Verification (localhost:5101 / `GCCDatabase_Dev`):** Playwright script `C:\Users\swest\AppData\Local\Temp\uitest\test-pid-icons.js` opened cert `26-0147` Edit page as `analyst`, scrolled to Product ID Manager, screenshotted (`pid-icons-after.png`), and read computed styles. **Edit icon `rgb(0, 174, 239)` (= `#00AEEF`)**, **Delete icon `rgb(255, 82, 82)` (= `#FF5252`)**, **zero remaining `btn-outline-*` action classes** on the page. Visual confirms the standard inline-icon footprint, edit blue, trash red.

---

### DEF-146 — Product ID Manager "Created By" was overwritten on every cert edit (showed "System" or current editor instead of the original creator)

- **Date:** 2026-05-05
- **Severity:** Medium (data-integrity / audit-trail)
- **Status:** Fixed locally (pending UAT / deploy)
- **Reporter:** Shawn — observed on prod cert `https://gccwebapp.com/Certificate/Edit/26-0147`, "Created by" cells showed `System` instead of the user that entered them.

**Symptom:** Product ID Manager's "Created By" column on the cert Edit page showed `System` (or, after any edit, the editor's username) instead of the user that originally added the row.

**Root cause:** `CertificateController.Edit (POST)` used a clear-and-readd pattern for the `ProductIds` collection (`certificate.ProductIds.Clear()` followed by `certificate.ProductIds.Add(productId)`), and stamped `CreatedBy = currentUserId` / `CreatedAt = now` on every row in the loop — overwriting the original creator on existing rows on every cert edit. Compounding factor: the Edit form's hidden `ProductIds[i].*` inputs (`Views/Certificate/Edit.cshtml`) don't include `CreatedBy`, so the model arrived with `CreatedBy = null`, and if `GetCurrentUserId()` ever returned null (e.g., Azure AD GUID claim that couldn't be mapped to an int user ID), the row landed with `CreatedBy = NULL`. The frontend `wwwroot/js/product-id-manager.js:244-245` falls back to literal `"System"` when `createdByUser` is null, producing the visible defect.

**Fix:** Replaced the clear-and-readd in `Controllers/CertificateController.cs` (Edit POST, Product ID handling block) with a merge: build a `Dictionary<int, CertificateProductId>` of existing rows by Id; remove rows whose Id is not in the submitted set; for each submitted row, if `Id > 0` and matches an existing row, update only the editable fields (Type, Value, Partner, ProductName, ProductSku, Description) and stamp `UpdatedBy/UpdatedAt` — preserving the original `CreatedBy/CreatedAt`; if `Id == 0`, treat as new and stamp `CreatedBy = currentUserId, CreatedAt = now` plus the same Updated fields.

**Files:** `Controllers/CertificateController.cs` (Edit POST, lines ~866-910).

**Out of scope (intentional):** No data backfill on existing NULL-CreatedBy rows (e.g., prod 26-0147). Per [feedback_data_changes_require_cr.md], data UPDATE on real cert records needs a formal CR; flagged for the client if they want historical creator attribution restored.

**Verification (localhost:5101 against `GCCDatabase_Dev`):** Playwright script at `C:\Users\swest\AppData\Local\Temp\uitest\test-pid-createdby.js` exercised three scenarios:
- Scenario A: cert `26-0025` has `CreatedBy = NULL` on its PIDs. Logged in as `analyst`, opened Edit, clicked Save with no PID changes. Expected: NULL preserved (not overwritten to `analyst`). **PASS.**
- Scenario B: cert `26-0147` has `CreatedBy = admin` on its PIDs. Logged in as `analyst`, opened Edit, clicked Save with no PID changes. Expected: `admin` preserved. **PASS.**
- Scenario C: cert `26-0147`, logged in as `analyst`, used the Product ID Manager to add a brand-new SKU PID, then saved. Expected: new row `CreatedBy = analyst`, existing rows still `CreatedBy = admin`. **PASS.**

---

### DEF-145 — Cert tile filters: Public + External filter dropdowns listed Cert Types and Product Categories with no matching visible certs (dead clicks)

- **Date:** 2026-05-05
- **Severity:** Low (functional — empty result on click; not data-loss)
- **Status:** Fixed (pending UAT / deploy)
- **Reporter:** Anuki internal QA pass — surfaced during DEF-144 audit; fixed after Shawn explicit approval ("yes do it for Public and External")

**Symptom:** On `/Certificate/CertificateTileView` (Public) and `/Certificate/CertificateTileViewExternal` (External), the filter sidebar's Cert Types and Product Categories dropdowns listed every admin-configured option, regardless of whether any cert in the user's visible cert pool actually used that option. Picking such an option produced an empty tile grid — silent zero-state, indistinguishable from a broken filter.

**Root cause:** Both endpoints pulled the cert types and product hierarchy directly from `_context.CertificationTypes` and `_context.ProductCategories`/`_context.ProductTypes` admin tables, with only an `IsActive` filter — no relationship back to the controller's `baseQuery` (the user-visible cert set). External view already had this scoping for `certificationTypes` via `certTypeIds`, but neither view scoped the product hierarchy.

**Fix:**
- `Controllers/Api/CertificateTilesApiController.cs` (Public):
  - Added `visibleCertTypeIds`, `visibleCategoryIds`, `visibleProductTypeIds`, `visibleSubcategoryIds` derived from `baseQuery`.
  - `certificationTypes` query now requires `visibleCertTypeIds.Contains(ct.Id) || ct.Children.Any(child => visibleCertTypeIds.Contains(child.Id))`.
  - `productCategoriesHierarchy` builder trims subcategories to those in `visibleSubcategoryIds`, keeps types if used directly OR if any visible subcat exists under them, and keeps categories if used directly OR if any visible type/subcat exists under them.
- `Controllers/Api/CertificateTileExternalApiController.cs` (External):
  - Same `productCategoriesHierarchy` scoping using the role-aware `baseQuery` (which already factors in user's company/partner scope).
  - `certificationTypes` was already correctly scoped — left unchanged.
- Internal view (`CertificateTileInternalApiController.cs`) intentionally unchanged. Admin/analyst should see the full taxonomy regardless of cert state, since they need to assign types/categories during cert creation.

Net effect: every dropdown option in Public and External now corresponds to at least one cert the user can actually click into.

**Tradeoff:** When admin adds a new cert type or product category but no public cert has been approved with it yet, it will not appear in the Public/External filter sidebar until an approved cert references it. This is intentional — favors filter signal over taxonomy completeness. Internal view still shows everything for admin operations.

**Test plan:**
1. Public view at `/Certificate/CertificateTileView` while logged out. Open Cert Types and Product Categories filters. Pick any option — should always return ≥1 cert.
2. External view at `/Certificate/CertificateTileViewExternal` as `external` user. Same assertion.
3. Internal view at `/Certificate/CertificateTileViewInternal` as `analyst` — confirm full taxonomy still shows (regression check).
4. Admin: add a new "Test Category" via `/Admin/ProductTypes` with no certs assigned. Reload Public view — confirm "Test Category" does NOT appear in filter sidebar. Reload Internal view — confirm it DOES appear.

**Found by:** During DEF-144 country-sort audit, code-survey of all three filter endpoints surfaced the "filters can include options with zero matching certs" UX gap. Confirmed with Shawn before implementing.

---

### DEF-144 — Cert tile filters: Countries dropdown didn't show "United States" first on Internal & External views

- **Date:** 2026-05-05
- **Severity:** Low (cosmetic — full country list is correct, just wrong sort order)
- **Status:** Fixed (pending UAT / deploy)
- **Reporter:** Anuki internal QA pass — Shawn directive during 2026-05-05 retest cycle

**Symptom:** The Countries filter on `/Certificate/CertificateTileViewInternal` and `/Certificate/CertificateTileViewExternal` listed all countries strict A-Z. Per requirement, "United States" must be pinned to the top of the list, then the rest A-Z.

**Root cause:** The two controllers were using a plain database `OrderBy(c => c)` for the countries filter. The Public-view controller already had the correct pattern (custom in-memory sort with US weighted to 0); Internal and External were missed when that pattern was added.

**Fix:**
- `Controllers/Api/CertificateTileInternalApiController.cs:526` — changed `countries = await ... .OrderBy(c => c).ToListAsync()` to materialize first via `.ToListAsync()`, then `.OrderBy(c => c == "United States" ? 0 : 1).ThenBy(c => c).ToList()`.
- `Controllers/Api/CertificateTileExternalApiController.cs:549` — same change.
- Public controller already correct — no change needed.

The materialize-then-sort pattern matches `CertificateTilesApiController.cs:322` exactly so future maintainers see one shape.

**Test plan:**
1. Log in as `analyst`. Open `/Certificate/CertificateTileViewInternal`. Open the Countries filter dropdown. Confirm "United States" appears first, all other countries A-Z below it.
2. Log in as `external`. Open `/Certificate/CertificateTileViewExternal`. Open Countries filter. Same assertion.
3. Public view (`/Certificate/CertificateTileView`) — regression check. Countries filter should still have US first (was always correct, but verify).

**Found by:** Code-survey of all three tile filter endpoints. While reviewing for the broader "filters reflect what the user can see" question, the country-sort discrepancy was the single objective fix; remaining visibility-scoping questions (e.g., filter sidebar showing cert types that have no public-visible certs) are flagged in the observations log for Shawn's CR call rather than fixed unilaterally.

---

### DEF-143 — Cert detail views load `js/certificates.js` (a list/grid initializer); page hits non-existent `/api/certificates` and throws `initializeSearch` TypeError on every load

- **Date:** 2026-05-05
- **Severity:** Low (functionally harmless — page renders correctly, download button still works because each detail view defines its own `window.downloadCertificate` inline; but two console errors fire on every detail-page navigation, including for unauthenticated public visitors hitting cert links from search engines)
- **Status:** **Fixed (pending deploy)** — removed dead `<script>` tag from 3 detail views
- **Reporter:** Internal QC continued retest, 2026-05-05. Surfaced in `session4-batch.js` test that loaded the public cert detail page for an arbitrary cert number from the live tile API.
- **Symptom:** Loading any of `/Certificate/CertificateSingleView/{certificateNumber}`, `/Certificate/CertificateInternalView/{…}`, or `/Certificate/CertificateExternalView/{…}` produces these console errors:
  1. `Failed to load resource: the server responded with a status of 404 ()` for `/api/certificates?pageSize=100&page=1`
  2. `Error loading certificates from API: Error: HTTP error! status: 404 at loadCertificatesFromAPI (.../js/certificates.js:78:23)`
  3. `TypeError: Cannot read properties of undefined (reading 'initializeSearch') at initializeCertificateSearch (.../js/certificates.js:122:25)` — cascading TypeError because the previous throw left state half-initialized
- **Root cause:** All three single-cert detail views (`CertificateSingleView.cshtml:233`, `CertificateInternalView.cshtml:336`, `CertificateExternalView.cshtml:277`) include `<script src="~/js/certificates.js"></script>`. That script is a *list/grid/search/filter* initializer for a multi-cert table view — it auto-runs `initializeCertificates()` on DOMContentLoaded, which calls `loadCertificatesFromAPI()` → `fetch('/api/certificates?pageSize=100&page=1')`. That endpoint returns 404 on the deployed build. Looking at controllers: `Controllers/Api/CertificatesApiController.cs` declares `[Route("api/[controller]")]` which should resolve to `/api/certificates`, but the route is actually 404 (also `api/etl` from `ETLController.cs` returns 404). Best guess: the legacy un-versioned `/api/certificates` and `/api/etl` controllers were either never registered in the active route table or were disabled in favor of the v1 versioned controllers (`/api/v1/certificates` returns 401 / works as expected with API key). Either way, `certificates.js` calls a route that doesn't exist.
- **Why this isn't worse:** All 3 detail views define their own `window.downloadCertificate` inline, and `openImageZoom` / `closeImageZoom` are also defined inline in the same scripts. The detail views genuinely don't need `certificates.js` for any functionality — there is no certificates grid, no search input, no filter UI on a single-cert detail page. The `<script>` tag is a dead include that produces noise on every page load with zero functional benefit.
- **Fix:** Remove the `<script src="~/js/certificates.js"></script>` line from all three detail views. Each view's inline scripts handle download / zoom / etc. independently. No other JS dependency exists between the detail views and `certificates.js`. Three single-line deletions.
- **Out of scope (separate decisions):**
  - Whether `wwwroot/js/certificates.js` itself should be deleted from the codebase. After this fix it is no longer referenced by any view (verified by grep). But deleting unreferenced JS is a cleanup decision, not a defect.
  - Whether the legacy `/api/certificates` and `/api/etl` routes should be re-enabled or the controllers deleted. The v1 versioned route at `/api/v1/certificates` is the supported Partner API surface today. If anything still relies on the unversioned routes, that would need to be migrated. Punted to a separate cleanup decision.
- **Files:** `Views/Certificate/CertificateSingleView.cshtml`, `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\session4-batch.js`

---

### DEF-142 — Partner-role web user has no navigation after login; cannot access Certificates or Products views

- **Date:** 2026-05-05
- **Severity:** Medium — Partner web users are completely locked out of the UI; only the API-key path works
- **Status:** **Fixed (pending deploy)** — built clean locally; dev DB updated; needs Plesk deploy
- **Reporter:** Shawn (internal QC), 2026-05-05: created testuser5 with Partner role on gccwebapp.com, logged in, saw no nav links.
- **Root cause:** Three-part gap. (1) `Views/Shared/_Layout.cshtml:171` only renders the External-style nav for `userRole == "External"` — Partner falls through to no nav. (2) `Program.cs` `ExternalAccess` policy only authorizes Admin/Analyst/External Role claims — even if the nav linked to `/Certificate/CertificateTileViewExternal`, Partner would 403. (3) `Data/SeedData.cs:97` had Partner seeded with `IsActive=false` (per the prior DEF-112 Path 4 decision to eliminate web-user Partner role). The dev DB row reflected that flip, so the User Management role dropdown didn't even offer Partner on dev. On prod the deactivation SQL was never run, so the dropdown DID offer it — which is how testuser5 got assigned in the first place.
- **Why this reverses DEF-112's resolution:** DEF-112 (resolved 2026-05-04) chose Path 4 (eliminate web-user Partner) on the assumption no human Partner users were needed — Amazon's UAT users could become External users with company scoping. Shawn confirmed 2026-05-05 that the actual requirement is the opposite: Partner web users DO exist, mirror External access pattern, but filter by partner instead of by company. So a Partner-role login should see the same Certificates + Products nav an External user sees, with results filtered to certs tagged with their assigned partner (e.g., all Amazon staff see all Amazon-tagged certs). The cert/product filtering plumbing for this case was already fully built (`Controllers/Api/CertificateTileExternalApiController.cs:98-110`, `Controllers/ProductController.cs:72-83 + 184-190`, Edit User modal Partner Assignment dropdown, `User.Partners` many-to-many). Only the policy/nav/active-flag wiring was missing.
- **Fix (4 changes):**
  1. `Program.cs` — `ExternalAccess` policy now also accepts `"Partner"` Role claim, so Partner web users can hit `/Certificate/CertificateTileViewExternal` and `/Product/Management`.
  2. `Views/Shared/_Layout.cshtml:171` — nav branch fires for `userRole == "External" || userRole == "Partner"`. Both roles render the Certificates + Products nav. Comment updated.
  3. `Data/SeedData.cs:94-97` — Partner role now seeds with `IsActive=true` and a description that reflects the dual-purpose use (web users + API keys both flow through this role; web path filters by `User.Partners`, API path is scoped at the controller level by middleware).
  4. **Dev DB** — `UPDATE Roles SET IsActive = 1, Description = '...' WHERE Name = 'Partner'`. Verified: BEFORE IsActive=0 → AFTER IsActive=1. Prod DB needs no DB change because the deactivation SQL was never run there.
- **Architectural caveat (carried forward from DEF-112):** The dual meaning of `userRole == "Partner"` (human user vs API-key request) still shares the same string in cert-listing controllers. Functionally the two flow through different controllers and don't collide — the External tile API gates by session userId → `User.Partners`, and the `/api/v1/certificates*` Partner API path is scoped by middleware before the role check fires. But audit-trail confusion remains a future hardening item (the DEF-112 Path 1 rename to `PartnerUser` or Path 2 explicit branch split). Not in scope for this fix.
- **Verification plan:**
  - Local: log in as a Partner-role user with `User.Partners` containing "Amazon"; confirm nav shows Certificates + Products; confirm `/Certificate/CertificateTileViewExternal` lists only Amazon-tagged certs; confirm `/Product/Management` lists only products with Amazon-tagged certs.
  - After Plesk deploy: re-test Shawn's testuser5 scenario on gccwebapp.com (assign a partner via the Edit User modal first if not already assigned, then log in).
- **Files:** `Program.cs`, `Views/Shared/_Layout.cshtml`, `Data/SeedData.cs`, dev DB Roles table

---

### DEF-141 — `/Product/Management` analyst page fires `/Admin/GetProducts` on every load, producing silent SyntaxError; wasted server round-trip + console noise

- **Date:** 2026-05-05
- **Severity:** Low (functionally harmless — page renders correctly; but wastes a round-trip per page load and produces a misleading console error every time)
- **Status:** **Fixed (pending deploy)** — gate added in `wwwroot/js/admin/productManagement.js`
- **Reporter:** Internal QC continued retest (Shawn-driven, 2026-05-05). Surfaced via multi-page smoke run (`multi-page-smoke.js`).
- **Symptom:** Every load of `/Product/Management` as an analyst produces `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON` in the console. The page renders correctly because the inline Razor JS handles its own `loadProducts()` against `/Product/GetProducts` (the analyst-allowed endpoint, fixed under DEF-111). But a *second* load is also firing.
- **Root cause:** `Views/Product/Management.cshtml` includes `~/js/admin/productManagement.js` solely for the import-modal helper methods (the inline view delegates `addProductBtn` import-click handling to `window.productManagement.handleImportClick()`). That script auto-instantiates `new ProductManagement()` on DOMContentLoaded, whose `init()` unconditionally calls `loadProducts()` → `fetch('/Admin/GetProducts')`. The analyst lacks Admin policy, so the framework redirects `/Admin/GetProducts` to `/Account/AccessDenied` (HTTP 200, `Content-Type: text/html`). `response.ok === true`; the unguarded `response.json()` then chokes on the leading `<` of the HTML page. The catch logs the SyntaxError and triggers a "Error connecting to server" toast which is visible briefly. Net effect: a wasted authorization round-trip on every load + console-error noise + a confusing toast that contradicts the page's actually-working state.
- **Why DEF-111's fix didn't catch this:** DEF-111 added the `response.redirected || !contentType.includes('application/json')` guard to the *inline* `loadProducts()` in `Views/Product/Management.cshtml`. The duplicate fetch comes from a *different* file — `wwwroot/js/admin/productManagement.js` — which wasn't in scope. Both functions are named `loadProducts`, which masked the duplication.
- **Fix:** Single 4-line gate in `productManagement.js` `init()`: only call `loadProducts()` / `renderProducts()` / `updateProductCount()` when `document.getElementById('products-table-body')` exists. That element only lives on `Views/Admin/ProductManagement.cshtml` (the page this class actually owns). On `/Product/Management` (which uses `id="product-table"` not `products-table-body`) the class still instantiates and binds its event handlers — the inline view explicitly delegates import-modal handling to `window.productManagement.handleImportClick()` — but skips the data-load that was hitting the wrong endpoint.
- **Verification plan:** After deploy, re-run `multi-page-smoke.js` and confirm zero console errors on `/Product/Management` for the analyst role. The Admin role's `/Admin/ProductManagement` should still load products as before (still hits `/Admin/GetProducts`, but admin has access).
- **Adjacent findings (not fixed under this defect):**
  - **Design-guide drift on success notifications.** `wwwroot/js/admin/admin-core.js` line 121 routes ALL `showNotification` calls (including `'success'`) to centered alertdialog modals that require an OK click. Project `.claude/CLAUDE.md` "User Notifications" section says "Success: Toast notification only". Either the design guide should be updated to reflect the modal-everywhere choice, or admin-core needs a separate transient-toast path for success. Not fixing without a decision — the modal-everywhere choice was deliberate per the inline code comment ("Route ALL notifications to centered modals for consistency"), so this is a doc-vs-code reconciliation, not an obvious bug.
  - **`<h1>` count = 2** on Dashboard, Product Management, and Company Index per Playwright smoke. CLAUDE.md says "Use `ViewData[\"Title\"]` (no `<h1>` in views)". Visual + accessibility audit needed before yanking duplicates — one of them might be the layout-supplied page heading and the other a duplicate inside the view.
  - **`/Admin/CertificationTypes` timed out at 30s** for analyst role during smoke. Should redirect to AccessDenied promptly. Likely a slow query or a misconfigured `[Authorize]` on a heavy endpoint that runs before the redirect. Worth a per-page timing/profiling pass.
- **Files:** `wwwroot/js/admin/productManagement.js`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\check-def111.js`, `multi-page-smoke.js`, `product-mgmt-batch.js`

---

### DEF-140 — Login: "Remember Me" silently overridden by client-side 30-min idle auto-logout

- **Date:** 2026-05-05
- **Severity:** Medium — defeats the entire feature; user perceives Remember Me as broken
- **Status:** **Fixed (pending deploy)** — built clean locally; needs Plesk deploy + manual verification
- **Reporter:** Shawn (internal QC), 2026-05-05: "Testing: Remember Me on the login does not appear to be working" — confirmed scenario is leaving a tab open and idling.
- **Root cause:** Two-layer problem. Server-side, the prior fix (commit `bc5d619`, 2026-03-20) correctly issues a 14-day persistent auth cookie when Remember Me is checked, and the session re-hydration middleware in `Program.cs` repopulates `Session.UserRole`/`UserEmail`/`UserName` from claims on each request, so browser-close-and-reopen works. But `wwwroot/js/auth.js:67-113` sets a hard-coded 30-minute idle timer that calls `document.getElementById('logoutForm').submit()` regardless of the user's Remember Me preference. So a Remember-Me user idling on an authenticated tab gets force-logged-out by client JS even though the server cookie is still valid for 14 days.
- **Fix (3 files):**
  1. `Controllers/AccountController.cs` `SignInUser` — added a `RememberMe` claim (`"true"`/`"false"`) to the auth ticket so downstream pages can read the preference without a separate cookie or DB lookup.
  2. `Views/Shared/_Layout.cshtml` — emit `<meta name="x-remember-me" content="..." />` based on `User.FindFirst("RememberMe")` so client JS can read the active state.
  3. `wwwroot/js/auth.js` — read the meta tag; when content is `"true"`, skip both the 5-min warning toast and the 30-min auto-logout entirely. Server-side cookie expiration (14 days) is the source of truth; the existing 401-fetch-interceptor at line 142 still redirects to login if the server says the session has expired.
- **Decision (Shawn, 2026-05-05):** Remember Me checkbox stays **unchecked by default** on a fresh visit to `/Account/Login`, including after explicit logout. Pre-checking on shared computers is a security footgun, and most secure-app conventions default unchecked.
- **Out of scope:** The Azure AD B2C path (`OnTokenValidated` in `AccountController.cs` line ~100) issues its own claims and doesn't go through `SignInUser`. AAD B2C is configured but not the active auth scheme today, so its claim set wasn't touched. If/when AAD B2C is enabled in production, the same `RememberMe` claim plumbing will need to be added there.
- **Files:** `Controllers/AccountController.cs`, `Views/Shared/_Layout.cshtml`, `wwwroot/js/auth.js`

---

### DEF-139 — Product Mgmt: "Add New Product" subcategory dropdown shows empty placeholder — user can't tell if it's loading, empty, or stale

- **Date:** 2026-05-05
- **Severity:** Low-Medium — UX/feedback gap; not data-loss. Affects every Add-New / Edit Product cascade.
- **Status:** **Fixed (pending deploy)** — code change in `Views/Product/Management.cshtml`
- **Reporter:** Shawn (continuation of internal QC retest), 2026-05-05: "Add new Product: Subcategory is not loading the first time Product Type is select and there are subcategory" — reproduced on https://gccwebapp.com/Product/Management.
- **Investigation:** Programmatically exercised the cascade against the live deployed page (Playwright, multiple scenarios: first-pick, rapid swap, close+reopen, kbd interaction) — every scenario loaded the correct subcategories from `/Certificate/GetProductSubcategories?productTypeId={id}`. Deployed JS matches source. So a deterministic "doesn't load" reproduction wasn't possible from automation, which points at one of: (a) browser cache holding pre-cascade JS, (b) slow-network race where the dropdown is briefly empty between change-event and fetch-completion and the user reads it as "didn't load", or (c) ambiguity — the placeholder reads "Select Subcategory" both while loading and when the type genuinely has no subs, so the user assumes the loading case.
- **Fix (`Views/Product/Management.cshtml` `populateSubcategoryDropdown`):** three changes, all defensive:
  1. **Loading state** — set `<option>Loading subcategories...</option>` immediately on type change, before await. Now the user always sees explicit feedback that a fetch is in flight.
  2. **Empty state** — when fetch returns success+empty, render `<option>No subcategories available</option>` instead of leaving the generic "Select Subcategory" placeholder. Matches the `Views/Certificate/Create.cshtml` pattern (line 585) so behavior is consistent across the two cascades.
  3. **Request-token guard** — module-scope `_subcategoryFetchSeq` counter. Each call captures `mySeq = ++_subcategoryFetchSeq`; after each await, bail if `mySeq !== _subcategoryFetchSeq`. Prevents a slow, earlier fetch from clobbering the dropdown after the user has switched to a different type. Closes a real (low-probability) race even if the user-visible symptom turns out to be cache.
- **Files:** `Views/Product/Management.cshtml`
- **Out of scope:** Same cascade exists on `Views/Admin/ProductManagement.cshtml` and `Views/Certificate/{Create,Edit}.cshtml`. Cert pages already show "No subcategories available". The Admin/ProductManagement page wasn't named in the report; left untouched pending evidence the same UX gap matters there.

---

### DEF-138 — QC #91: Product Mgmt save silently nulled Sku and Model fields on every update — client called it "a big issue"

- **Date:** 2026-05-05
- **Severity:** Medium-High — affected every product update through the Product Mgmt modal; existing Sku and Model values were silently destroyed
- **Status:** **Fixed (pending deploy)** — verified live on `localhost:5101`
- **Reporter:** Client retest 5/1 on QC #91: "There was no product type and subcat filled in from list. So I added to 25-8760 and 25-8777. But they then are not showing in list at all. What is going on. Also, another issue: When I went back into 25-8760 it keeps clearing out Product Name and Product Type even after saving edit (but it kept prod cat and subcat). What is going on? This is a big issue!"
- **Root cause:** A prior sprint's QC #92 fix removed the Sku, Model, and Status inputs from the Add/Edit Product modal (per memory `2026-03-24` observation). The frontend `saveProduct` JS payload at `Views/Product/Management.cshtml:928-937` no longer includes `sku` or `model`. But `Controllers/ProductController.cs:386-387` (`SaveProduct` action, update branch) still unconditionally assigned `product.Sku = model.Sku` and `product.Model = model.Model`. Since the JS payload omits both fields, JSON deserialization sets `model.Sku` and `model.Model` to `null`. The server then overwrites the existing database values with `null` on every single product update. Sku/Model values populated by CSV import — or any other path that bypasses the modal — were silently destroyed by any subsequent edit.
- **Why the client perceived it as "Product Name and Product Type cleared":** The Product Mgmt table renders each row's Product cell as two stacked lines: the product Name on top, and `SKU: ${product.sku || 'N/A'}` immediately below it (`Views/Product/Management.cshtml:667-671`). Cert and Product numbers like "25-8760" and "25-8777" referenced in the client's complaint look like they were SKU values. After the first save, those SKU strings flipped to "N/A" in the table; the client identified the products by their SKU values and reported them as "not showing in list at all" and "Product Name cleared." The actual Product Name (`product.name`) was never affected. Verified by live test against the dev DB: the Name field, ProductType, ProductSubcategory, CompanyId, and CategoryId were all correctly preserved across save; only Sku and Model were nulled.
- **Fix:** Changed the SaveProduct update branch to only assign Sku/Model when the model carries a non-null value:
  ```csharp
  if (model.Sku != null) product.Sku = model.Sku;
  if (model.Model != null) product.Model = model.Model;
  ```
  This preserves existing values when the modal omits the fields, while still allowing CSV import (or any future call site that does send the fields) to update them. The Create branch is unchanged because new products legitimately start with whatever Sku/Model the modal supplies (which is null today, but that's a separate UX decision about whether to surface those inputs).
- **Live verification (2026-05-05 on `localhost:5101`):**
  - Test 1 (the bug): seeded a product with Sku=`TEST-SKU-<ts>` and Model=`TEST-MODEL-<ts>`, then submitted the exact JSON payload the modal would send (no `sku`, no `model`). On the **un-patched** build, both fields flipped to `null` after save. On the **patched** build, both fields are preserved verbatim.
  - Test 2: saving with a newly-set ProductTypeId works as before — the type and the joined ProductTypeName both flow through to the list correctly. So the second part of the client's complaint ("they're not showing in list at all" referring to type/subcat after save) was not a separate persistence bug; it was the same SKU-display-N/A symptom misread.
- **Out of scope for this fix (related but separate decisions):**
  - Whether to put Sku and Model inputs back into the Add/Edit Product modal. QC #92 explicitly removed them. If the client wants them back, it's a UX scope question — needs a decision before code change.
  - Whether to drop the `SKU: ...` line from the product list cell entirely so future SKU values can't visually disappear and confuse the user. This is a polish question, not a defect.
- **Files:** `Controllers/ProductController.cs`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\qc91-test.js`

---

### DEF-137 — QC #107: Add User flow had two latent bugs — RequirePasswordReset silently dropped + soft-deleted email blocked re-create

- **Date:** 2026-05-04
- **Severity:** Medium — Admin/Analyst-side workflow defect; client retest at 5/1 pushed back on QC #107 with 5 sub-issues
- **Status:** **Fixed (pending deploy)** — verified live on `localhost:5101`
- **Reporter:** Client retest 5/1 on QC log row #107: "I still cannot add a user if I enter a password AND I have the checkbox checked... I tried to create another with my email and i was getting error message that a user with that email already exists"
- **Prior-sprint context (why this resurfaced):** The Sprint 2 4/10 dev note on this row claimed `"QC #107 is already resolved. The fix wraps the BeginTransactionAsync() calls inside _context.Database.CreateExecutionStrategy().ExecuteAsync(), which is the correct pattern for SQL Server transient retries"` plus added a duplicate-email pre-check so users got a real error instead of "Invalid data provided". Both pieces of that sprint's work were real, but:
  - The `BeginTransactionAsync` + `CreateExecutionStrategy().ExecuteAsync()` wrapper is **no longer in the current code** — `CreateUserAsync` was later collapsed to a single `await _context.SaveChangesAsync()` (line 167-168 carries the comment `// Single SaveChangesAsync — EF Core tracks all changes and commits atomically`). The simplification is valid (EF Core does an implicit transaction) but the prior sprint's rationale is moot in the current shape.
  - The duplicate-email pre-check (line 108-113) landed, but didn't filter `!u.IsDeleted`. So the surface the client hit at 5/1 — *delete a user, recreate with the same email* — was outside the prior fix's coverage, and the unfiltered DB UNIQUE indexes on `Email` + `Username` would have rejected the INSERT anyway.
  - The `RequirePasswordReset` checkbox-to-entity assignment was never added in any prior sprint — the field has been silently dropped on Create since the form was wired up.
  - The Sprint 2 "Ready for UAT" verdict was technically true for the surface the dev tested (you now see a real error message), but did not address the soft-delete reuse case or the checkbox flag — exactly the two cases the client retested. Same trailing-comment blind spot pattern that surfaced across 13 other rows in the 5/4 Sprint 3 reconciliation; same remediation now codified in the validation prompt template's Phase 4 pre-write checklist.
- **Sub-issue A — soft-deleted email blocked re-create:** `AdminService.CreateUserAsync` line 108-113 checked for duplicate emails via `_context.Users.FirstOrDefault(u.Email == email)` without filtering `!u.IsDeleted`. Soft-deleted users (set by `DeleteUserAsync` at line 322 — flips `IsDeleted=true`, leaves Email/Username intact) blocked re-create with the same email. Worse: even after fixing the C# check, `ApplicationDbContext.cs:374-375` defines unfiltered UNIQUE indexes on both `Email` and `Username`, so the DB itself rejects the INSERT (`SaveChanges` throws "An error occurred while saving the entity changes").
  - **Fix part 1:** Added `&& !u.IsDeleted` to the duplicate-email guard so legitimate re-create attempts pass the C# check.
  - **Fix part 2:** Modified `DeleteUserAsync` to suffix the soft-deleted user's `Email` and `Username` with `__deleted__<id>__<timestamp>` before SaveChanges. This frees the unique slot at the DB level. The audit log captures the original email in `oldUser` snapshot so the original value is preserved and reconstructable.
  - **Why suffix vs filtered index:** Filtered unique indexes (`WHERE IsDeleted = 0`) are the cleaner long-term fix but require an EF migration + DB schema change. The suffix approach is zero-migration and works within the existing schema.
- **Sub-issue B — RequirePasswordReset checkbox silently dropped on Create:** `AdminService.CreateUserAsync` lines 117-127 built the `User` entity setting `Status`, `TwoFactorMethod`, `IsActive`, `CreatedAt` but **never assigned `model.RequirePasswordReset` to `user.RequirePasswordReset`**. The checkbox state had zero effect on the created user. Per `Models/User.cs:50` the field defaults to `false`, so users always logged in without forced reset regardless of the checkbox.
  - **Fix:** Added `RequirePasswordReset = model.RequirePasswordReset` to the User initializer.
- **Sub-issue E — New External user couldn't log in (red herring):** Live test confirmed External users log in fine after creation. Carly's 5/1 complaint was likely a side-effect of Bug B — she set the checkbox expecting forced reset but got a normal login that confused her, OR she'd previously hit the soft-deleted-email block from a prior Carly Test user. Both root causes addressed by A + B.
- **Live verification (2026-05-04 on `localhost:5101`):**
  - Test A: created user → deleted → re-created with same email → both creates returned `{success: true}`. Soft-deleted row has email `qc107A_<ts>@example.com__deleted__<id>__<yyyyMMddHHmmss>`; live row has clean email.
  - Test B: created user with `requirePasswordReset: true`, logged out, logged in as that user → redirected to `/Account/ForcePasswordReset`. Flag now honored.
  - Test E: created External user with `requirePasswordReset: false`, logged out, logged in as External user → landed on `/Certificate/CertificateTileViewExternal`. Login works.
- **Out of scope for this DEF (still open from QC #107 — track separately):**
  - Sub-issue C — Password field clears when re-opening user. Working as designed (security best practice — never display stored password); needs UX helper "Leave blank to keep current password" + don't reset typed password on save. Defer to Sprint 3 polish or new ticket.
  - Sub-issue D — Company Access not limiting Admin/Analyst data. Working as designed per Carly's own 5/1 quote: "Admin and Analyst will never be limited to companies." External-side scoping is implemented at `ProductController.cs:120,125,300` and `CertificateTileExternalApiController.cs:347,354,371`. No code change needed; verify in client UAT.
- **Files:** `Services/AdminService.cs`
- **Test artifact:** `C:\Users\swest\AppData\Local\Temp\uitest\qc107-test.js`

---

### DEF-136 — Product Management: post-save success path threw `notificationQueue is not defined`, surfacing a fake error toast on a successful save

- **Date:** 2026-05-04
- **Severity:** Medium — every successful Add or Edit Product showed an error toast saying "Error saving product" even though the row had been persisted. Verified against prod test DB: products 119/120/121 (created during Shawn's UAT minutes earlier) all saved correctly with `ProductTypeId=4`, `ProductSubcategoryId=46`, while he was watching error toasts for each one.
- **Status:** **Fixed (pending deploy)** — verified DLL contents post-publish; old `showNotification(message, 'success');` literal is no longer in the compiled view.
- **Reporter:** Shawn — pasted browser console showing `ReferenceError: notificationQueue is not defined at showNotification (Management:1653:13) at HTMLFormElement.saveProduct (Management:1206:25)`.
- **Root cause:** `Views/Product/Management.cshtml` had a stale legacy `showNotification` function that referenced a `notificationQueue` global that was never declared at the top of the file (likely the residue of an unfinished in-page notification system; the auto-refresh scaffolding around it is also dead code that's never wired up). The post-save success branch in `saveProduct` called both `showNotification(message, 'success')` and `showToast(message)` redundantly. The first call threw a `ReferenceError`. The outer `catch` in `saveProduct` swallowed the error and called `showToast('Error saving product', 'error')` — which routes through `AdminCore.showNotification` (the working notification system) and rendered a misleading red toast.
- **Fix:** Dropped the broken `showNotification(message, 'success');` call from the success branch. The next line (`showToast(message)`) already routes through `AdminCore.showNotification` for the success toast, so functionality is preserved with no replacement needed.
- **Out of scope:** The dead in-page notification queue + auto-refresh scaffolding (functions `showNotification`, `processNotificationQueue`, `displayNotification`, `getNotificationIcon`, `closeNotification`, `removeNotification`, `initializeRealTimeUpdates`, `toggleAutoRefresh`, `startAutoRefresh`, `stopAutoRefresh`, `updateLastRefreshIndicator`, `createNotificationContainer`, `handleKeyboardShortcuts`) is left in place. Removing it is a polish / cleanup ticket. Same pattern likely exists in other admin views and could be refactored to a shared helper if it becomes a problem.
- **Files:** `Views/Product/Management.cshtml`.

---

### DEF-135 — CSP: web.config script-src missing Google domains, blocking admin-enabled Google Analytics

- **Date:** 2026-05-04
- **Severity:** Medium — GA was enabled by an admin via `/Admin/GoogleAnalytics` (DB row `GoogleAnalyticsSettings.IsEnabled = 1`, TrackingId `G-16J12CPD8S`), the layout injected the `gtag.js` script tag, but the browser blocked the load with `script-src` violation. Zero analytics signals were being collected on the test env.
- **Status:** **Fixed (pending deploy)** — included in the 2026-05-04 17:45 zip.
- **Reporter:** Shawn — pasted browser console showing `Loading the script 'https://www.googletagmanager.com/gtag/js?id=G-16J12CPD8S' violates the following Content Security Policy directive: "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com"`.
- **Root cause:** Two CSP sources are emitted on every response — the IIS-level header in `web.config` and the app-level header from `Middleware/SecurityHeadersMiddleware.cs`. The middleware version already lists `https://www.googletagmanager.com` and `https://www.google-analytics.com` in `script-src` and the relevant `connect-src` hosts. The `web.config` version did not. Browsers intersect multiple CSP headers, so the more-restrictive `web.config` value wins and blocks GA.
- **Fix:** `web.config` `script-src` extended with `https://www.googletagmanager.com https://www.google-analytics.com`. `connect-src` extended with the GA4 beacon hosts (`*.google-analytics.com`, `analytics.google.com`, `www.google.com`, `stats.g.doubleclick.net`, `www.googletagmanager.com`) — same set the middleware already allows. The two CSP sources now agree.
- **Per-environment scoping:** The DB toggle `GoogleAnalyticsSettings.IsEnabled` is the actual on/off switch — the layout only injects the script when it's true. CSP changes ship the same web.config to every environment, but real prod can keep `IsEnabled=false` and GA never fires there even though CSP allows it. Site is deindex-pending (Bing + Google tickets submitted by Shawn 2026-05-04) so analytics on the gated test env doesn't leak into search.
- **Followup not done here:** The dual-CSP architecture (web.config + middleware) is fragile — drift between the two has now bitten us once. Consider removing the web.config CSP entirely and letting the middleware be the single source of truth in a later cleanup pass. Out of scope.
- **Files:** `web.config`.

---

### DEF-134 — DATA-LOSS: Product Management Edit silently nulls Type/Subcategory; Subcategory FK pointed at the wrong table

- **Date:** 2026-05-04
- **Severity:** High — every product in dev DB has `ProductTypeId = NULL` and `ProductSubcategoryId = NULL` (51/51) despite the columns existing on the model. QC #91 (added "Product Type" + "Subcategory" columns to the management table) was marked Ready for UAT but the columns are blank because the data isn't being persisted.
- **Status:** **Fixed (pending deploy)** — verified end-to-end on `localhost:5101` via Playwright; new product saved with Plastics/HDPE; edit modal pre-fills all four dropdowns.
- **Reporter:** Shawn — "the product and product subcategories don't have any data even though the user entered it on a product"
- **Two independent root causes:**
  1. **`editProduct()` only set name + description.** `Views/Product/Management.cshtml:824-836` left Company, Category, Product Type, Subcategory dropdowns empty when the modal opened in Edit mode. When the user hit Save without manually re-picking each one, `ProductController.SaveProduct` blindly overwrote all four fields with `null`. Every Edit was a silent wipe.
  2. **`Product.ProductSubcategoryId` FK pointed at the wrong table.** The model FKed subcategory to the `ProductSubcategories` table (`ApplicationDbContext.cs:203-205`), but `ProductSubcategories` has 0 rows and the cascade endpoint `/Certificate/GetProductSubcategories` returns Level-2 children from the `ProductTypes` table. So if a user picked a subcategory in the modal, EF rejected the save with FK violation → exception caught → `Success=false` → user saw a generic error toast. `Certificate.ProductSubcategory` already correctly used `ProductType?` (`Certificate.cs:113`); `Product` was the inconsistent one.
- **Fix:**
  - **Model:** `Product.ProductSubcategory` retyped from `ProductSubcategory?` → `ProductType?` (`Models/Product.cs`). Removed unused `Products` collection from `Models/ProductSubcategory.cs`.
  - **DbContext:** `Product → ProductSubcategory` relationship reconfigured to `WithMany().OnDelete(Restrict).IsRequired(false)` targeting the `ProductTypes` table (`Data/ApplicationDbContext.cs`).
  - **Migration:** `20260504204044_RetargetProductSubcategoryFkToProductTypes.cs` — drops `FK_Products_ProductSubcategories_ProductSubcategoryId`, adds `FK_Products_ProductTypes_ProductSubcategoryId`. No data conversion needed (all values were NULL). Reversible.
  - **API surface:** `ProductController.GetProducts` now returns `companyId`, `categoryId`, `productTypeId`, `productSubcategoryId` alongside the existing `*Name` fields. `ProductListViewModel` extended with the four ID properties.
  - **Edit pre-fill:** `editProduct()` made async; pre-fills Company + Category + Type + Subcategory from the loaded product, awaiting the subcategory cascade fetch before showing the modal.
  - **Import lookup:** `ImportProducts` subcategory lookup repointed from the empty `ProductSubcategories` table to `ProductTypes` Level-2 children (filter: `Name == subcategoryName && ParentId == productTypeId`). Product type lookup also now scoped to Level-1 (`ParentId == null`) to prevent accidentally matching a child type with the same name.
- **Verified live on `localhost:5101`:**
  - Add Product modal: picked Chemicals → Plastics → HDPE, saved. DB row shows `ProductTypeId=4, ProductSubcategoryId=46`. Both columns render in the management table.
  - Edit Product modal: opening an existing product pre-fills all four dropdowns to the saved values (company=1, cat=1, type=4, sub=46).
  - Migration applied to dev DB; FK now reads `FK_Products_ProductTypes_ProductSubcategoryId` per `sys.foreign_keys`.
- **Out of scope (not changed):**
  - Existing 51 products in dev DB still have `NULL` for both fields — backfill is a data action, not a code fix; would need a CR if client wants legacy products tagged. Per `feedback_data_changes_require_cr.md`.
  - The `ProductSubcategories` table is now orphaned for Product but is still referenced by `UnitOfWork`/`IUnitOfWork` and EF DbSet. Left in place to keep the surface area minimal; can be dropped in a later cleanup.
  - QC #91 stays Ready-for-UAT — the column-rendering work it shipped was correct; this defect is the orthogonal save-path bug behind the empty cells.
- **Files:** `Models/Product.cs`, `Models/ProductSubcategory.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProductController.cs`, `Views/Product/Management.cshtml`, `Migrations/20260504204044_RetargetProductSubcategoryFkToProductTypes.cs` (+ `.Designer.cs`, snapshot updated).
- **Followup polish (same UAT cycle, 2026-05-04 17:45 zip):** `ProductController.GetProductTypes` was returning every active `ProductTypes` row regardless of `ParentId`, so the modal's Product Type dropdown showed Level-1 roots + Level-2 children mixed (and on prod, also Level-0 legacy "Construction" roots). The Subcategory cascade then re-displayed the same Level-2 entries as if they were direct subcategories of themselves. Fix: added `pt.ParentId == null` to the `GetProductTypes` filter so only root types reach the dropdown. Subcategory cascade is unchanged (already correctly returns direct children of the selected type).
- **Migration applied to prod test DB:** 2026-05-04 — pre-flight confirmed 0 non-null `ProductSubcategoryId` rows and 100 total products before apply; post-apply, `sys.foreign_keys` shows `FK_Products_ProductTypes_ProductSubcategoryId` correctly targets `ProductTypes`. Migration logged in `__EFMigrationsHistory`. New saves verified working — products 119, 120, 121 created during UAT all persisted with `ProductTypeId=4` (Plastics) and `ProductSubcategoryId=46` (HDPE).

---

### DEF-133 — UI: mojibake (`â–¶`, `â€¢`, `â€"`, `â†'`) baked into 4 Razor views — visible to users on Product Management

- **Date:** 2026-05-03
- **Severity:** Medium — visible cosmetic corruption on Product Management page (`▶` row-expand triangle, `•` last-updated bullet) and Admin > Product Management (`—` em-dash placeholder for empty Parent Identifiers)
- **Status:** **Fixed (pending deploy)**
- **Reporter:** Shawn — "why am I seeing special characters. â–¶"
- **Root cause:** Source files were UTF-8 with BOM, but at some point each was opened in an editor that interpreted them as Windows-1252 and re-saved as UTF-8 — re-encoding the multibyte sequences as their Windows-1252 visual aliases. The bad bytes were then literally checked in. Same family of bug as the prior `Database_Scripts/FixMojibakeProductNames.sql` (which corrected `™` → `â„¢` and `®` → `Â®` in product names on prod 2026-04-14).
  - `▶` (U+25B6, UTF-8 `E2 96 B6`) → `â–¶`
  - `•` (U+2022, UTF-8 `E2 80 A2`) → `â€¢`
  - `—` (U+2014, UTF-8 `E2 80 94`) → `â€"`
  - `→` (U+2192, UTF-8 `E2 86 92`) → `â†'`
- **Fix:** Restored the 7 corrupted glyphs in 4 view files. Verified clean: `grep "â"` against `Views/` returns zero matches; dev DB product names have zero remaining mojibake (already fixed by 2026-04-14 SQL script).
- **User-visible instances corrected:**
  - `Views/Product/Management.cshtml:647` — row-expand triangle in product list
  - `Views/Product/Management.cshtml:1497` — collapsed-state triangle assignment
  - `Views/Product/Management.cshtml:1361` — last-updated bullet separator
  - `Views/Admin/ProductManagement.cshtml:82` — em-dash empty-state for Parent Identifiers cell
- **Comment-only instances (not user-visible, cleaned for hygiene):**
  - `Views/Product/Management.cshtml:527` — JS comment
  - `Views/Certificate/Edit.cshtml:499` — JS comment
  - `Views/Certificate/Create.cshtml:447` — JS comment
- **Out of scope (intentionally not touched):**
  - `deploy-out/**` — regenerates on next `dotnet publish`
  - `docs/tracking/test-execution-log.md`, `qc-readyforuat-2026-05-01.md`, `qc-manual-tests-2026-05-01.md`, `qc-sit-screenshots-2026-04-24/index.html` — internal docs, not served to users
  - 30+ `.cs` controller/service files lack a UTF-8 BOM but contain only ASCII high-byte content in legitimate string literals; no rendering bug, no action.
- **Prevention recommendation (NOT implemented — proposing for tracking):** add a pre-commit hook or CI check that greps `^Views/.*\.cshtml$` for the canonical mojibake byte sequences (`â„`, `â€`, `â†`, `â–`, `Â®`, `Â `) and fails the commit. Cheap to wire up; would have caught this at the moment of corruption.
- **Files:** `Views/Product/Management.cshtml`, `Views/Admin/ProductManagement.cshtml`, `Views/Certificate/Edit.cshtml`, `Views/Certificate/Create.cshtml`

---

### DEF-132 — REGRESSION (caught + fixed same-cycle): DEF-121 [JsonIgnore] + C# `required` keyword incompatibility broke Partner API key authentication

- **Date:** 2026-05-03
- **Severity:** Medium — caught + fixed within minutes during cycle 4 audit; never deployed
- **Status:** **Fixed (pending deploy)**
- **Reporter:** Cycle 4 audit run discovered Partner API was returning 500 on `/api/v1/certificates/search` with log message `"Error during Partner API key authentication"` and inner exception `JsonPropertyInfo 'passwordHash' defined in type 'GCCWebApp.Models.User' is marked required but does not specify a setter`.
- **Root cause:** The DEF-121 stopgap added `[JsonIgnore]` on `User.PasswordHash`, but the property was also declared with the C# `required` keyword (`public required string PasswordHash { get; set; }`). System.Text.Json cannot reconcile the two: `[JsonIgnore]` removes the property from JSON deserialization, but `required` still expects the JSON to provide it. When the Partner API key auth path round-tripped a User object through JSON (likely cache/session), deserialization threw.
- **Fix:** Dropped the `required` keyword from `PasswordHash` in `Models/User.cs`. Initialization safety preserved by EF Core's NOT NULL DB constraint and by every `new User { ... }` site explicitly setting the field (verified via grep).
- **Verified:** Live curl with Amazon partner key on `localhost:5101` — `/api/v1/certificates` and `/api/v1/certificates/search` both return 200 with valid contract DTOs. No 500s.
- **Lesson for followup ticket (DEF-121 DTO refactor):** the `[JsonIgnore]` stopgap is cleaner with non-required nullable string properties. The DTO refactor (option 1 of original DEF-121) is now even more attractive because it sidesteps this kind of attribute-vs-keyword conflict entirely.
- **Files:** `Models/User.cs`

---

### DEF-131 — SECURITY: Partner API keys could POST/PUT/DELETE certificates via `/api/v1/certificates/bulk/*` (privilege escalation — read-only contract violated)

- **Date:** 2026-05-03
- **Severity:** **CRITICAL (S1)** — privilege escalation; partners are contractually read-only consumers, but they could create/update/delete certificates
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 4
- **Reporter:** Cycle 4 audit of all v1 controllers reachable by `[Authorize(AuthenticationSchemes = "ApiKey")]`
- **Problem:** `Controllers/Api/v1/BulkCertificatesController.cs` was decorated with `[Authorize(AuthenticationSchemes = "ApiKey")]` at the class level (line 15). This permitted any valid Partner API key to call:
  - `POST /api/v1/certificates/bulk/create` (line 28) — accepts up to 100 `Certificate` entities, persists them via `_context.SaveChangesAsync()`
  - `PUT /api/v1/certificates/bulk/update` (line 128) — updates existing certs in batch
  - `DELETE /api/v1/certificates/bulk` (line 228) — deletes by ID list
  - `POST /api/v1/certificates/bulk/validate` (line 280)
- **Live evidence (before fix):** `curl -X POST -H "X-API-Key: <amazon partner key>" -d '[]' http://localhost:5101/api/v1/certificates/bulk/create` → HTTP 400 with body `"No certificates provided"` — meaning **the auth check passed and only the empty-array branch rejected**. A real payload would have been processed.
- **Why this got through:** Copy-pasted controller-level auth attribute from `CertificatesController` (which legitimately accepts ApiKey for read endpoints). Whoever added the bulk controller didn't differentiate read vs mutation auth requirements.
- **Contract source:** Per CLAUDE.md `External User Restrictions`, External users (closest human analogue to Partner role) have no Add/Edit cert buttons. Per memory `feedback_amazon_api_immutable.md`, Amazon Partner API contract is read-only. Per the prompt's role table, Partner uses `X-API-Key` header for **read** access to `/api/v1/*`.
- **Fix:** Changed class-level attribute from `[Authorize(AuthenticationSchemes = "ApiKey")]` to `[Authorize(Policy = "AnalystOrAdmin")]`. This forces cookie-based auth (default scheme) and rejects any caller not in the Analyst or Admin role. Partner API keys are now blocked.
- **Verified live (post-fix curl, Amazon partner key):**
  - `POST /api/v1/certificates/bulk/create` → HTTP 403 `"Access denied. You do not have permission to perform this action."`
  - `PUT /api/v1/certificates/bulk/update` → HTTP 403
  - `DELETE /api/v1/certificates/bulk` → HTTP 403
  - Sanity: `GET /api/v1/certificates` (Partner read endpoint) → HTTP 200 (unaffected)
- **Internal impact assessment:** `grep -r 'api/v1/certificates/bulk\|BulkCertificatesController'` found ZERO callers in the repo. No frontend JS, no service code, no test code references these endpoints. The lockdown is functionally invisible to the existing app — there is no internal feature that uses the bulk routes. If Admin/Analyst tooling needs them in future, they can authenticate via cookie just like every other admin route.
- **Files:** `Controllers/Api/v1/BulkCertificatesController.cs`

---

### DEF-130 — SECURITY+DATA-EXPOSURE: `/api/v1/products` was Partner-API-key authorized at the class level — exposing GET data leak AND POST/PUT/DELETE mutation methods to partners

- **Date:** 2026-05-03 (reported), 2026-05-04 (fixed via lockdown, severity upgraded after method audit, deployed)
- **Severity:** **CRITICAL (S1)** — upgraded 2026-05-04 from S3 after a smoke against prod confirmed the controller exposes `[HttpPost]` (line 207), `[HttpPut("{id}")]` (line 241), and `[HttpDelete("{id}")]` (line 292) in addition to the GET routes (lines 82, 171, 318). With the pre-fix class-level `[Authorize(AuthenticationSchemes = "ApiKey")]`, any valid partner API key (e.g., the live Amazon prod key) could **create, update, or delete products** via the API — the same privilege-escalation class as DEF-131. The 2026-05-04 attribute swap closes all four HTTP methods at once.
- **Status:** **Deployed 2026-05-04** — option 1 (lockdown) shipped per Shawn's call. Verified live on gccwebapp.com: real Amazon partner key gets HTTP 403 ("Access denied. You do not have permission to perform this action.") on `GET /api/v1/products`. Anonymous returns 401. Test partner key (id=7) was generated, used for verification, and revoked in the same minute.
- **Reporter:** Cycle 4 audit of v1 controllers reachable by Partner API key
- **Problem:** `Controllers/Api/v1/ProductsController.cs` was decorated with `[Authorize(AuthenticationSchemes = "ApiKey")]` at line 29 — Partner API keys could hit it. The list endpoint returned raw `Product` entities including:
  - Internal IDs (`id`, `categoryId`, `companyId`, `productTypeId`, `productSubcategoryId`)
  - `isActive`, `isDeleted` flags (internal lifecycle state)
  - Nested `category` object with `description`, `sortOrder`, `products: [null]` (circular nav reference)
  - Nested empty `productType`, `productSubcategory`, `company` references
  - Nested `certificates: [null]` array (circular)
- **Live evidence (Partner key → `GET /api/v1/products?pageSize=1`):** Returned 51 products (all of them — no partner scoping) with full internal structure visible.
- **Contract:** Products endpoint is NOT in the Amazon Developer Guide §4 or §8 contract list. The contracted partner endpoints are `/api/v1/certificates*` only. Two reasonable fix paths existed:
  1. **Drop Partner access entirely:** change auth to `[Authorize(Policy = "AnalystOrAdmin")]` (same as DEF-131 fix). Clean answer if Amazon doesn't consume it.
  2. **Add a partner DTO projection:** if Amazon does consume the products endpoint, project to a clean `ProductPartnerDto` with only public-safe fields and drop the nested entity navigation. Would require Carly→Kyle sign-off per `feedback_amazon_api_immutable.md`.
- **Decision rationale (2026-05-04):** Production usage check — confirmed via dev DB that Amazon's API key has `AllowedEndpoints = NULL` (no endpoint restriction in middleware), so partners CAN structurally hit the route, but `/api/v1/products` is not part of the Amazon Developer Guide contract and so isn't bound by the contract-immutability rule. The aggregate `PartnerApiKey.RequestCount` doesn't break down by endpoint, and Application Insights is disabled in prod (`EnableApplicationInsights: false`), leaving stdout logs as the only behavioral source — Shawn opted to ship the lockdown immediately (fully reversible in minutes) and pull prod stdout logs in parallel. If logs come back showing Amazon usage, revert + ship option 2 as a follow-up CR.
- **Fix (2026-05-04):** Single-attribute swap on `Controllers/Api/v1/ProductsController.cs:29` — `[Authorize(AuthenticationSchemes = "ApiKey")]` → `[Authorize(Policy = "AnalystOrAdmin")]`. Same pattern as DEF-131. Class-level only; no method-level overrides exist. XML doc-comment updated to reflect the new access posture and reference DEF-130 for the rationale.
- **Internal-consumer impact analysis:** Grep across `*.cs`, `*.cshtml`, `*.js`, `*.html` for `api/v1/products` returned only (a) the controller's own `CreatedResponse` self-reference, (b) a code comment in `PartnerApiKeyAuthenticationMiddleware`, (c) an example string in `PartnerApiController` doc, (d) the `PartnerApiKey.AllowedEndpoints` example, and (e) the `Views/Admin/GCCAPI.cshtml` admin docs page (which still works for Admin/Analyst cookie auth). Zero JS or backend code consumes this endpoint internally — lockdown has zero functional impact on the internal app.
- **Followup if logs show partner calls:** Revert the attribute, write `Models/Api/ProductPartnerDto.cs` with only public-safe fields (id, name, description, productIds[]), update controller methods to project, and route the change as a Partner API contract change requiring Carly→Kyle sign-off.
- **Files:** `Controllers/Api/v1/ProductsController.cs`

---

### DEF-129 — SECURITY/CONTRACT: Partner API `/api/v1/certificates/search` leaks raw entity (same bug class as DEF-103/DEF-124)

- **Date:** 2026-05-03
- **Severity:** **CRITICAL (S1)** — Partner API contract violation per `feedback_amazon_api_immutable.md` and prompt's S1 trigger list
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 4 of the QA pass
- **Reporter:** Cycle 4 audit of `[HttpGet]` actions in `CertificatesController` after DEF-124 fix
- **Problem:** Identical bug class to DEF-124, found in the same controller. `GetExpiringCertificates` was projected in cycle 3, but `SearchCertificates` (line 549) was still returning the raw `Certificate` entity directly via `PaginatedResponse(certificates, ...)`. No DTO projection, no `PublicStatuses` filter, no role check.
- **Live evidence (before fix):** `curl -H "X-API-Key: <amazon>" "/api/v1/certificates/search?term=Widget&status=Client%20Approved%20-%20Paid&pageSize=2"` returned a Certificate entity with `id: 2`, `status: "Client Approved - Paid"` (raw), `country: "United States"` (raw), flat `asin/upc/gtin/ean`, internal IDs `companyId/productId/*Id`, plus a nested `company` entity with `email`, `contactInfo`, and a nested `products` array containing more circular references.
- **Why this got through:** DEF-103 (April 21) was supposed to cover all 6 partner endpoints but missed `/expiring`, `/{id}`, `/number/{n}`. The April 30 self-test caught the latter two. The May 3 cycle 1 caught `/expiring`. The May 3 cycle 4 audit caught `/search` — the fourth from the same scope-creep gap. **A controller-wide grep is now the right test.**
- **Fix (cycle 4):** Same pattern as DEF-124. `SearchCertificates`:
  1. Detects `userRole == "Partner"` via `GetCurrentUserRole()`
  2. Includes `Pathway` and `ProductIds` (DTO requires them)
  3. For partner callers: filters on `PublicStatuses.Contains(Status) && partnerNames.Contains(Partner)`. Empty paginated response if no partner association.
  4. Projects via shared `BuildPartnerCertificateDto` helper. Admin/Analyst still get the raw entity.
  5. Changed return type signature on the `PaginatedResponse` call so it accepts both projected DTOs and raw entities.
- **Verified live (post-fix):** `curl ".../search?term=Widget&status=Client%20Approved%20-%20Paid"` now returns `{certificateNumber: "26-0001", status: "Active", certificationCountries: "All", company: "Acme Corporation", productIds: [], pdfDownloadUrl: ...}` — no internal IDs, no flat product fields, no nested entities.
- **Followup ticket recommended:** rather than chasing endpoint-by-endpoint, write a controller-level unit test that asserts every `[HttpGet]` in `CertificatesController` returns the contract DTO when caller is `Partner`. Catches future regressions automatically.
- **Files:** `Controllers/Api/v1/CertificatesController.cs`

---

### DEF-128 — DATA-EXPOSURE/FUNCTIONAL: `/Certificate/ExportCertificates` is `[AllowAnonymous]`, supports Excel/PDF (rule says CSV-only), and PDF format returns broken file

- **Date:** 2026-05-03
- **Severity:** **HIGH (S2)** — driven by Internal Tag exposure to anonymous callers (per CLAUDE.md, even External users cannot view internal tags; this endpoint exposes them to **anyone**)
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 3 of the QA pass
- **Reporter:** Internal QA pass cycle 2 (full-spectrum-test prompt)
- **Composite finding — 4 sub-issues in one endpoint:**

  **(a) Anonymous Internal Tag exposure — S2:**
  - `Controllers/CertificateController.cs:1708` — `ExportCertificates` is `[AllowAnonymous]`
  - Live test: `curl http://localhost:5101/Certificate/ExportCertificates?format=csv` (no cookie, no key) returns a 24-column CSV
  - Column 20 is `"Internal Tag"`, populated with values like `"Sustainable"`, `"Eco-Friendly"`, `"Verified"` for real certs
  - Per CLAUDE.md `External User Restrictions`: "CANNOT view/edit internal tags". Anonymous tier is below External, so this clearly violates the intent.
  - Other sensitive columns leaked: `"Company Contact Email/URL"` (e.g., `contact@acme.com`), `"Status"` raw lifecycle (e.g., `"Client Approved - Paid"` instead of public-facing literal), `"ASIN"`, `"UPC"`, `"GTIN"`, `"EAN"`.

  **(b) Excel format support violates "CSV ONLY" rule — S3:**
  - CLAUDE.md Critical Rule: "**CSV ONLY** - No Excel/JSON/PDF from admin pages. Use `ExportUtils.exportToCSV()` directly"
  - Endpoint accepts `format=excel`, returns real `Microsoft Excel 2007+` (.xlsx), MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, ~12KB
  - Both internal AND external tile views send `format=excel` (`certificate-tile-view-external.js:1397`, `certificate-tile-view-internal.js`)

  **(c) PDF format returns broken file — S3 functional:**
  - `curl ".../ExportCertificates?format=pdf"` returns HTTP 200 OK, `Content-Type: application/pdf`, ~7.8KB
  - But `file` reports the body as `ASCII text, with CRLF line terminators` — this is **not a real PDF** (no `%PDF-` magic bytes)
  - User experience: clicking "Export to PDF" downloads a file that no PDF reader can open
  - Probably the legacy CSV stream getting served with a PDF MIME, or the export service returns a stub. Either way, broken on the wire.

  **(d) Button label says "Export All to Excel" — S5 polish:**
  - `Views/Certificate/CertificateTileViewExternal.cshtml:184` and `CertificateTileViewInternal.cshtml:249`:
    `<i class="fas fa-file-excel"></i> Export All to Excel`
  - aria-label: `"Export all visible certificates to Excel file"`
  - This contradicts "CSV ONLY" — the user believes they're getting Excel, the rule says CSV.

- **Recommended fix (in priority order):**
  1. **Add `[Authorize]` to `ExportCertificates`** (or remove the endpoint and route through `ExportUtils.exportToCSV()` per the rule). Anonymous callers should not be able to bulk-pull cert data with internal tags.
  2. **Drop the Internal Tag column** from any export accessible to External or anonymous callers. (For Admin/Analyst, fine to keep.)
  3. **Force `format=csv`** server-side, ignore other format values (or 400 them). Update both tile-view JS files to send `format=csv` and update button label/icon to match (`<i class="fas fa-file-csv"></i> Export All to CSV`).
  4. **Project the Status field** through `CertificateStatusHelper.PublicStatuses` mapping when caller is anonymous/External (same DEF-103 pattern).
  5. **Remove the `pdf` branch** from the `format` switch entirely — it's broken and not used.
- **Files:** `Controllers/CertificateController.cs`, `wwwroot/js/certificate-tile-view-external.js`, `wwwroot/js/certificate-tile-view-internal.js`, `Views/Certificate/CertificateTileViewExternal.cshtml`, `Views/Certificate/CertificateTileViewInternal.cshtml`, `Services/CertificateService.cs` (ExportCertificatesAsync — confirm CSV-only output regardless of format param).
- **Test evidence (cycle 2, 2026-05-03):** Curl outputs captured in test-execution-log Section H.
- **Fix (2026-05-03 cycle 3):** All four sub-issues addressed in one batch.
  1. **Service interface (`Services/IServices.cs`):** dropped `string format` param, added `bool includeInternalTags = false`. Same for `ExportCertificatesAsync`, `ExportCertificatesByIdsAsync`, `ExportFilteredResultsAsync`. Default `false` — most restrictive.
  2. **Service implementation (`Services/CertificateService.cs`):** dropped the `format.ToLower() switch` (no more Excel / PDF branches). Always calls `GenerateCsvExport`. The `GenerateExcelExport` and `GeneratePdfExportFromRows` methods are retained as dead code for now; cleanup is a followup ticket.
  3. **`GenerateCsvExport`:** added `bool includeInternalTags` parameter. When false, the `"Internal Tag"` column is dropped from both the header row and every data row. Column count goes from 24 → 23 for anon/External callers.
  4. **MVC controller (`Controllers/CertificateController.cs:1709`):** kept `[AllowAnonymous]` (public tile view legitimately needs export), but: hardcoded `Content-Type: text/csv` and `.csv` extension; computes `includeInternalTags = (userRole == "Admin" || userRole == "Analyst")` so anon/External/Partner do NOT get the column.
  5. **API controller (`Controllers/Api/CertificatesApiController.cs`):** dropped format switch, hardcoded CSV. Endpoint is `[Authorize(Policy = "AnalystOrAdmin")]` so always passes `includeInternalTags: true`.
  6. **Views (`CertificateTileViewExternal.cshtml`, `CertificateTileViewInternal.cshtml`):** "Export All to Excel" → "Export All to CSV"; `fa-file-excel` → `fa-file-csv`; aria-label updated.
  7. **JS (`certificate-tile-view-external.js`, `certificate-tile-view-internal.js`):** all `params.append('format', 'excel')` → `'csv'`; toast text "Exporting … to Excel" → "to CSV".
  8. **Tests (`GCCWebApp.Api.Tests/Unit/Services/CertificateServiceTests.cs`):** updated 8 call sites to use new signature; added 2 new tests covering the include-internal-tags-false behavior (DEF-128 regression coverage).
- **Verified live (curl on `localhost:5101`, 2026-05-03 — anonymous, no API key, no cookie):**
  ```
  GET /Certificate/ExportCertificates?format=excel  → text/csv (CSV ASCII, was Microsoft Excel 2007+)
  GET /Certificate/ExportCertificates?format=pdf    → text/csv (CSV ASCII, was broken PDF stub)
  GET /Certificate/ExportCertificates?format=csv    → 23 columns (was 24)
    Header: "Certificate #",…,"External Tag","ASIN",…  ← no Internal Tag between External Tag and ASIN
    First data row: …,"Verified","B08X7N9MRQ",…       ← Internal Tag value "Sustainable" gone
  ```

---

### DEF-127 — POLISH: Import/Export icon convention drifted on Product/Company pages

- **Date:** 2026-05-03 (reported), 2026-05-04 (fixed + deployed)
- **Severity:** Low (S5 polish)
- **Status:** **Deployed 2026-05-04** — verified live on gccwebapp.com: 4 affected pages (`/Admin/ProductManagement`, `/Company/Index`, `/Product/Management`, `/Product/Categories`) all show `fa-file-arrow-down`/`fa-file-arrow-up`; zero residual `fa-download`/`fa-upload` occurrences anywhere on those pages.
- **Reporter:** Internal QA pass cycle 2 (visual sweep)
- **Symptom:** DEF-101 standardized Import = down arrow, Export = up arrow across admin pages. The convention is honored everywhere, but the icon class names diverge:
  - **`/Admin/UserManagement` and `/Admin/Tags`** use `fa-file-arrow-down` / `fa-file-arrow-up` (file-with-arrow icons)
  - **`/Admin/ProductManagement` and `/Company/Index`** use generic `fa-download` / `fa-upload` (tray icons)
- **Why it's polish-tier:** Both classes are visually down/up arrows so the DEF-101 spec ("Import = down arrow, Export = up arrow") is technically satisfied. Visual consistency is just slightly off — paged-side-by-side, the icons look different. No user is going to be confused, but design QC will notice.
- **Fix (2026-05-04):** Aligned Import/Export buttons on the four drifted pages to use `fa-file-arrow-down` / `fa-file-arrow-up`. Also updated the Import Preview modal headers on the same pages so the icon stays consistent inside the modal.
- **Files:** `Views/Product/Management.cshtml` (lines 39, 42, 141), `Views/Company/Index.cshtml` (lines 38, 41, 96), `Views/Admin/ProductManagement.cshtml` (lines 37, 40, 116), `Views/Product/Categories.cshtml` (lines 18, 21).
- **Verification:** Source-side grep confirms both icon classes present in all four files; localhost runtime verify pending — Razor runtime compilation isn't configured in this project, so the change reaches the browser only after the next dev server restart / publish.
- **Out of scope (intentionally not touched):** Admin pages with Export-only buttons (`CertificateFields`, `CertificateTypes`, `Partners`, `Pathways`, `RoleManagement`, `ProductTypes`, `CSFAttributes`) currently use `fa-upload` for Export. Convention-wise they satisfy DEF-101 ("Export = up arrow"); standardizing them to `fa-file-arrow-up` is a separate polish ticket if design QC flags it. `Views/Product/ManagementAlt.cshtml` and `ManagementV2.cshtml` are alt views (not on sitemap) — left alone.

---

### DEF-126 — Add/Edit Product ID modal: stray ⚠ triangle on initial open + still cramped at 900px

- **Date:** 2026-05-03
- **Severity:** Medium — visual / UX bug; not blocking but looks broken on every modal open
- **Status:** **Fixed local** — pending deploy
- **Reporter:** Shawn (`http://localhost:5101/Certificate/Edit/26-0147`, "Edit Product ID and Add Product ID is still not wide enough and getting triangle caution icon, validation issues?")
- **Symptom:** Opening the Add Product ID (or Edit Product ID) modal on Certificate Create / Edit shows a small red ⚠ triangle below the empty Product ID input *before the user has touched anything*. Modal also still feels cramped on a 1920px desktop monitor.
- **Root cause — triangle:**
  - `wwwroot/css/accessibility-enhancements-low-priority.css` lines 422–427 add `content: '\26A0 '` (⚠) as a `::before` pseudo-element to **every** `.invalid-feedback` div globally — regardless of whether the field is in an error state.
  - The Product ID modal markup pre-renders `<div class="invalid-feedback" id="valueValidation"></div>` as an empty placeholder ready for JS to fill with an error message.
  - `wwwroot/css/product-id-manager.css` overrides Bootstrap's default `display: none` on `.invalid-feedback` with `display: block` so it always reserves vertical space.
  - Combined: the empty placeholder div is forced visible, and the global pseudo-element paints the ⚠ even with no error to display.
- **Root cause — width:** DEF-119 had bumped `#productIdModal .modal-dialog max-width` from 650px to 900px. On a 1920px desktop the modal still occupies under half the viewport and the two-column form looks pinched.
- **Fix:**
  1. `accessibility-enhancements-low-priority.css`: changed selector from `.invalid-feedback::before` → `.invalid-feedback:not(:empty)::before` (and same for `.error` and `.field-validation-error`). The ⚠ now only renders when the feedback element actually contains an error message.
  2. `product-id-manager.css`: bumped `#productIdModal .modal-dialog max-width` from 900px → 1140px (matches Bootstrap's modal-xl).
- **Files:** `wwwroot/css/accessibility-enhancements-low-priority.css`, `wwwroot/css/product-id-manager.css`
- **Verified locally (2026-05-03):** Playwright on `http://localhost:5101/Certificate/Edit/26-0147`, viewport 1920×1080, admin user.
  - **Fresh Add modal:** no triangle below empty Product ID input. DOM check confirmed `valueValidation` div has empty `textContent` and `getComputedStyle(...,'::before').content === 'none'`.
  - **Invalid input (typed "B08N5WRWN" with ASIN selected):** red triangle ⚠ now renders alongside the message "ASIN must be exactly 10 alphanumeric characters" — accessibility behavior preserved for actual errors.
  - **Modal width:** dialog bounding box now `1140 × ~990` (was `900 × 992`), centered at `x=390` on a 1920px viewport — visibly more breathing room around all form fields.
- **Regression risk:** the `:not(:empty)` selector change is global. Any other form on the site that relied on the ⚠ pseudo-element appearing on a *deliberately empty* `.invalid-feedback` div (and the empty div being meaningful as a visual indicator) would lose the icon. Practical impact: zero — `.invalid-feedback` is always paired with a message in Bootstrap-style validation; an empty one is always a placeholder.

---

### DEF-125 — Polish: 404 vs 302 on Admin routes leaks which controller actions exist

- **Date:** 2026-05-03
- **Severity:** Low (Polish / S5) — minor reconnaissance leak; not a real attack vector
- **Status:** **Open** — defer; not worth a fix unless touched anyway
- **Reporter:** Internal QA pass (cycle 1 of full-spectrum-test prompt)
- **Symptom:** Anonymous request to a non-existent admin action returns HTTP 404, while a real admin action returns HTTP 302 (redirect to login). This lets an attacker enumerate which admin actions exist without authenticating.
- **Evidence (anon):**
  - `/Admin/Index` → 302 (exists, redirects to login)
  - `/Admin/UserManagement` → 302 (exists)
  - `/Admin/Tags` → 302 (exists)
  - `/Admin/Dashboard` → 404 (action does not exist)
  - `/Admin/Roles` → 404 (action does not exist)
- **Why minor:** The set of admin action names is deterministic and would be visible to anyone who logs in once. It's also obvious from URL conventions (UserManagement, Tags, etc.). Treating it as a security issue would mean adding `[AllowAnonymous]` 302 catch-alls just to spoof the response code, which adds complexity for negligible benefit.
- **Recommendation:** Leave open; revisit only if a security review specifically flags it.
- **Files:** N/A

---

### DEF-124 — SECURITY/CONTRACT: Partner API `/api/v1/certificates/expiring` leaks raw entity (status, internal IDs, file paths, no public-status filter)

- **Date:** 2026-05-03
- **Severity:** **CRITICAL (S1)** — Partner API contract violation per `feedback_amazon_api_immutable.md` and prompt's S1 trigger list
- **Status:** **Fixed (pending deploy)** — same-day fix in cycle 3 of the QA pass
- **Reporter:** Internal QA pass (cycle 1 of full-spectrum-test prompt) — found while regressing DEF-103 across all Partner API endpoints
- **Problem:** `GET /api/v1/certificates/expiring?days=N` is reachable via Partner API key (controller-level `[Authorize(AuthenticationSchemes = "ApiKey")]` at `CertificatesController.cs:33`). The endpoint returns the raw `Certificate` entity instead of the contract DTO. Same root-cause class as DEF-103, but `/expiring` was missed when the fix was applied to `/{id}` and `/number/{n}`.
- **Evidence (live `/api/v1/certificates/expiring?days=365` with active Amazon partner key):**
  ```json
  {
    "data": [{
      "id": 38,                                 // ← INTERNAL ID LEAK
      "number": "EXT-AMZ-001",
      "status": "Client Approved - Paid",       // ← INTERNAL LIFECYCLE STATUS LEAK (should be "Active")
      "country": "United States",               // ← RAW COUNTRY LEAK (should be "All")
      "asin": null, "upc": null,                // ← FLAT FIELDS (should be productIds[] array)
      "gtin": null, "ean": null,
      "certificateFile": null,                  // ← INTERNAL FILE PATH LEAK
      "certificateImagePath": null,
      "pathwayId": null,
      "companyId": 61,                          // ← INTERNAL COMPANY ID LEAK
      "productId": 84,                          // ← INTERNAL PRODUCT ID LEAK
      "productCategoryId": null,
      "productTypeId": null,
      "productSubcategoryId": null,
      "certificationTypeId": 1,                 // ← INTERNAL TYPE ID LEAK
      "certificationTypeSubcategoryId": null,
      "createdBy": null, "updatedBy": null,
      "createdAt": "...", "updatedAt": "...",
      "company": { "id": 61, "...": "...",
                   "certificates": [ /* full nested entity tree */ ]
      }
    }]
  }
  ```
- **No public-status filter:** `EXT-AMZ-001` is in legacy lifecycle status `"Client Approved - Paid"`. If any cert tagged `Amazon` is in `Draft`, `Submitted`, `Hold`, or `Expired`, it would also leak through this endpoint to the partner — violating the same Amazon Developer Guide §4 + §8 rule that DEF-103 was about.
- **Circular nav references:** Because `Company` is `Include`d and `Company` has a back-reference to `Certificates`, the JSON serializer produces a deeply nested response. Live test response was 1GB+ before being truncated. This is also a **DoS amplification risk** (any partner key holder can demand 1GB responses).
- **Root cause (`Controllers/Api/v1/CertificatesController.cs:627-674`):**
  - Line 657: `Data = certificates` — returns raw `List<Certificate>`, no projection
  - No `where (PublicStatuses.Contains(c.Status))` filter
  - No partner-role branch to apply `BuildPartnerCertificateDto` helper
  - `Include(c => c.Company)` triggers circular serialization
- **Fix (recommended):**
  1. Add `PublicStatuses` filter when caller is Partner: mirror lines 826-843 of the same controller (used in `SearchByProductId`).
  2. Add partner branch that projects via `BuildPartnerCertificateDto` (the helper added in 2026-04-30 DEF-103 fix).
  3. Add `Include(Pathway)` and `Include(ProductIds)` to the query (so the DTO has the data it needs).
  4. Drop `Include(Company)` from the partner-facing path (DTO only needs `Company.Name`); keep it for analyst/admin if they ever call this endpoint.
  5. Remove `id`, `companyId`, `productId`, `*Id`, `certificateFile`, `certificateImagePath`, `createdBy`, `updatedBy` from the response — these are internal-only.
- **Why this got through 2026-04-21 fix:** DEF-103's commit message claimed "all 6 partner endpoints fixed" but the actual fix only touched list, GS1 list, GS1 single, and missed `/expiring`, `/{id}`, `/number/{n}`. The 2026-04-30 self-test caught `/{id}` + `/number/{n}` and fixed them. `/expiring` wasn't in scope of either pass — slipped through.
- **Audit needed:** Re-grep all `[HttpGet]` actions in `CertificatesController` and confirm every one returns a projected DTO when `userRole == "Partner"`. Also audit all other API v1 controllers (`ProductsController`, `CompaniesController`) for the same pattern.
- **Customer impact:** Amazon's Climate Pledge Friendly integration consumes `/api/v1/*`. Of the 7 contracted endpoints, `/expiring` is documented in Swagger and is reachable. If Amazon scripts hit it, they'd see internal lifecycle statuses (e.g., `"Submitted"`, `"Hold"`) which violates the contract literal `"Active"`. They might either reject the response (breaking their integration) or, worse, accept it and then question why our app exposes "draft" certifications externally.
- **Fix (2026-05-03 cycle 3):** Mirrored the `SearchByProductId` pattern in the same controller. `GetExpiringCertificates`:
  1. Detects `userRole == "Partner"` via `GetCurrentUserRole()`.
  2. Includes `Pathway` and `ProductIds` (so the contract DTO has the data it needs); kept `Company` include because the DTO projection prevents the circular Company→Certificates serialization that previously blew the response to 1GB+.
  3. For partner callers: filters on `PublicStatuses.Contains(Status) && partnerNames.Contains(Partner)`. Returns empty paginated response if the API key has no partner association.
  4. Projects via shared `BuildPartnerCertificateDto` helper — same DTO shape as list/by-id/by-number endpoints. Anonymous/Admin/Analyst still get the raw entity for internal dashboards.
- **Verified live (curl on `localhost:5101`, 2026-05-03):**
  ```
  GET /api/v1/certificates/expiring?days=365 (Amazon partner key)
  → 4 items (only PublicStatuses survived filter)
  → status: "Active" (was "Client Approved - Paid")
  → certificationCountries: "All" (was "United States")
  → company: "ExtTest-AmazonCo" / "Acme Corporation" (now populated, not null)
  → productIds: [] (was flat asin/upc/gtin/ean fields)
  → No internal IDs, no certificateFile/certificateImagePath leak
  → Response size ~1KB (was 1GB+ on circular nesting)
  ```
- **Files:** `Controllers/Api/v1/CertificatesController.cs`

---

### DEF-123 — Dashboard: aggregate stat CSV exports show "Uncategorized" / "Unassigned" instead of real names; counts disagreed with tiles

- **Date:** 2026-05-03
- **Severity:** Medium (broken reporting; downloads were silently useless for product categories/types/subcategories)
- **Status:** Fixed (pending deploy)
- **Reporter:** Shawn — follow-up to DEF-122 ("test the export data is matching")
- **Symptom:**
  - `/Dashboard/DownloadStat?type=product-categories-with-active-certs` returned `"Uncategorized",22` instead of the real category name
  - Same for `product-types-with-active-certs` → `"Unassigned",22`
  - Same for `product-subcategories-with-active-certs` → `"Unassigned",22`
  - `Total Product Types: 1` in the CSV header disagreed with the tile showing `0` (the "Unassigned" fallback bucket was being counted as if it were a real type)
- **Root causes (two):**
  1. **Missing eager loads.** `GetActiveCertificatesAsync()` only included `Company`, `Product`, and `CertificationType`. The controller's GroupBy expressions read `c.Product?.Category?.Name`, `c.ProductType?.Name`, `c.ProductSubcategory?.Name` — none of those nav properties were Included, so they all evaluated to null at runtime → fell to the `?? "Uncategorized"` / `?? "Unassigned"` fallback strings.
  2. **GroupBy ignored the Product linkage path.** Controller grouped by cert-direct nav only (`c.ProductType?.Name`), ignoring the `c.Product.ProductType.Name` path. The tile counting logic in `GetCertificateStatisticsAsync` correctly considers both paths, so tile and CSV used different definitions even before the null-fallback issue.
  3. **"Unassigned"/"Uncategorized" treated as a real group.** Group count included these fallback buckets, so `Total Product Types: N` could overstate by 1 if any cert was untyped.
- **Fix:**
  1. `Repositories/CertificateRepository.cs` — `GetActiveCertificatesAsync()` now `.ThenInclude`s `Product.Category`, `Product.ProductType`, `Product.ProductSubcategory`, and includes the cert-direct `ProductCategory`, `ProductType`, `ProductSubcategory` navs as well.
  2. `Controllers/DashboardController.cs` `GenerateAggregatedStatCsv` — GroupBy now uses Product linkage preferred, cert-direct fallback (matching tile semantics), and filters out null/empty group keys via `.Where(g => !string.IsNullOrWhiteSpace(g.Key))`. CSV reports only real types; if there are zero typed certs, the report shows `Total: 0` with no rows.
- **Validation (dev DB, 22 active certs):**
  | Stat | Tile | CSV Total | CSV rows | Match |
  |---|---|---|---|---|
  | Product Categories | 1 | 1 | "Chemicals and Raw Materials",22 | ✅ |
  | Product Types | 0 | 0 | (empty) | ✅ |
  | Product Subcategories | 0 | 0 | (empty) | ✅ |
  | Companies | 9 | 9 | 9 named rows summing to 22 | ✅ |
- **Files touched:**
  - `Repositories/CertificateRepository.cs`
  - `Controllers/DashboardController.cs`
- **Deploy:** Pending — same build as DEF-122
- **Notes:** `GetActiveCertificatesAsync()` is also called from `GetCertificatesForStat` and `GetCertificatesForTable` for non-aggregate downloads (active cert lists, top-companies/types/categories tables). Adding ThenIncludes is incremental cost (no new joins for callers that don't read the nav, just one query with more JOINs); no behavior regression in those code paths.

---

### DEF-122 — Dashboard: tile #3 label "# Product Categories with Active Certs" was bound to certification-types count

- **Date:** 2026-05-02
- **Severity:** Medium (visible to internal users; misleading reporting metric)
- **Status:** Fixed (pending deploy)
- **Reporter:** Shawn — "are the numbers correct" audit of `/Dashboard`
- **Symptom:** Dashboard tile #3 displayed the count of distinct **certification types** under a "Product Categories" label. Dashboard had no tile for actual product categories.
- **Root cause:** `Views/Dashboard/Index.cshtml:58` bound the value to `Model.TotalCertificationTypes` (which the controller fills from `Statistics["CertTypesWithActiveCerts"]`). The repository never computed a `ProductCategoriesWithActiveCerts` stat at all — the key didn't exist.
- **Fix:**
  1. `Repositories/CertificateRepository.cs` — added `stats["ProductCategoriesWithActiveCerts"]` distinct-count query, mirroring the two-query EF pattern already used for ProductTypes / Subcategories (handles certs linked via `Product.Category` and certs linked directly via `c.ProductCategory`).
  2. `Views/Dashboard/Index.cshtml` (line 58) — rebound to `Model.Statistics.GetValueOrDefault("ProductCategoriesWithActiveCerts", 0)`.
- **Validation (dev DB):** OLD value = 4 (cert types — wrong); NEW value = 1 ("Chemicals and Raw Materials" — matches Top-5 Product Categories table at the bottom of the same page).
- **Files touched:**
  - `Repositories/CertificateRepository.cs`
  - `Views/Dashboard/Index.cshtml`
- **Deploy:** Pending — next build after 2026-05-01 zip
- **Notes:** Tile's download button (`data-stat-type="product-categories-with-active-certs"`) was already correctly mapped to the right CSV — was just sitting on a tile showing the wrong number. `DashboardViewModel.TotalCertificationTypes` left in place (no other consumers found, but kept for safety; can remove in a future cleanup).

---

### DEF-121 — SECURITY: `/api/productids/certificate/{id}` leaks password hashes and 2FA secrets in CreatedByUser / UpdatedByUser

- **Date:** 2026-05-01 (filed) → 2026-05-03 stopgap → 2026-05-03 DTO refactor (this entry)
- **Severity:** **HIGH** (information disclosure — credentials exposure)
- **Status:** **Fixed (stopgap + DTO refactor)** — pending deploy. Both layers of defense in place:
  1. `[JsonIgnore]` on 8 sensitive `User` fields (2026-05-03 stopgap, still in place).
  2. `ProductIdResponseDto` projection now removes the `User` entity surface entirely from `/api/productids/*` responses (2026-05-03 cycle 4 — this fix).
- **Reporter:** Found incidentally while diagnosing DEF-120 (Created By column rendering)
- **Problem:** `GET /api/productids/certificate/{certificateId}` returns the full `CertificateProductId` entity with `Include(p => p.CreatedByUser)` and `Include(p => p.UpdatedByUser)` (`Services/ProductIdService.cs:65-66`). Default JSON serialization emits the entire User object, including:
  - `passwordHash` (BCrypt hash — recoverable to plaintext via offline attack if leaked)
  - `twoFactorSecret` (TOTP seed — bypasses 2FA entirely if leaked)
  - `recoveryCodes`
  - `securityQuestions`
  - `passwordResetToken` / `passwordResetTokenExpiry`
  - `allowedIPs` (IP allowlist — useful for attacker reconnaissance)
  - `passwordHistory`
  - Email + last-login metadata
- **Authorization:** Endpoint requires `[Authorize(Policy = "AnalystOrAdmin")]`. The leak is not anonymous — it's only visible to authenticated Analyst+ users. But: (1) any Analyst account compromise immediately yields all admin password hashes; (2) any Analyst can capture all admin 2FA secrets and bypass MFA on any account; (3) Partner API users do NOT hit this endpoint, so contract is unaffected.
- **Other endpoints potentially affected (audit needed):** Any controller that returns `CertificateProductId` with the same `Include` pattern (e.g. `GetProductId(int id)`, `CreateProductId`, `UpdateProductId`). The cert tile internal/external APIs may also leak via `Certificate.CreatedByUser` includes — needs verification.
- **Recommended fixes (pick one or combine):**
  1. **DTO projection (preferred):** introduce `ProductIdResponseDto` with only `{ id, certificateId, productIdType, productIdValue, status, partner, productName, productSku, description, isActive, isPublic, displayOrder, createdAt, createdBy, createdByUserName, updatedAt, updatedBy, updatedByUserName }`. Map in controller before returning. Eliminates the entire User-entity surface.
  2. **`[JsonIgnore]` on User secrets:** mark `passwordHash`, `twoFactorSecret`, `recoveryCodes`, `securityQuestions`, `passwordResetToken`, `passwordHistory`, `allowedIPs` with `[JsonIgnore]` on `Models/User.cs`. Application-wide protection but couples the model to serialization concerns.
  3. **Both:** belt-and-suspenders.
- **Not fixed in this commit:** flagged as standalone defect. Needs Shawn's call on which approach + audit scope.
- **Why this got through:** Pattern of `.Include(...).Returning(entity)` is convenient and is used in many places. No DTO layer enforced. Code review didn't catch it because the User entity grew over time (new security fields added) without revisiting which endpoints serialize it.
- **2026-05-03 verification (cycle 1 QA pass):** Re-confirmed unfixed after 2 days. Code evidence:
  - `Models/User.cs:31` — `PasswordHash` still has no `[JsonIgnore]`
  - `Models/User.cs:53` — `TwoFactorSecret` still has no `[JsonIgnore]`
  - `Services/ProductIdService.cs:65-66, 74-75` — still uses `Include(CreatedByUser)` / `Include(UpdatedByUser)` and returns the entity directly
  - **Aging risk:** Open HIGH-severity credential leak sitting unaddressed for 2+ days. Every day this stays open is a window where any Analyst account compromise yields the admin password hash + 2FA secret (full account takeover). Recommend bumping severity to **CRITICAL** and either (a) putting a quick `[JsonIgnore]` block on `User.cs` today as a stopgap and following up with the DTO refactor, or (b) doing the full DTO refactor now.
- **2026-05-03 STOPGAP FIX SHIPPED LOCAL** (cycle 3 QA pass): Applied option 2 (`[JsonIgnore]` on User secrets). Status now **Fixed (stopgap, pending deploy)** — DTO refactor still recommended as a followup ticket but the immediate credential exposure is closed.
  - `Models/User.cs` — added `using System.Text.Json.Serialization;` and `[JsonIgnore]` on: `PasswordHash`, `TwoFactorSecret`, `RecoveryCodes`, `SecurityQuestions`, `AllowedIPs`, `PasswordResetToken`, `PasswordResetTokenExpiry`, `PasswordHistory`. Eight fields total.
  - **Why this is safe:** No code in the repo deserializes JSON → `User` entity (`grep -E '\[FromBody\].*User\b|JsonSerializer\.Deserialize<User>|FromJson<User>'` → no matches). All update flows go through ViewModels (`UserCreateViewModel`, `UserEditViewModel`) and `AdminService`. EF Core reads from DB, not JSON. The `[JsonIgnore]` only affects API response serialization, not internal flows.
  - **Verified runtime:** `/Admin/GetUsers` already uses `UserViewModel` projection — its output never had secrets. The `/api/productids/certificate/{id}` endpoint that DEF-121 originally flagged still uses `Include(CreatedByUser)`/`Include(UpdatedByUser)` patterns, but with `[JsonIgnore]` the serialized output now omits all 8 sensitive fields regardless. Dev DB has zero CertificateProductId rows so couldn't run a live curl reproduction; proceeded on code evidence.
  - **Followup ticket needed:** DTO refactor (option 1 from this entry's "Recommended fixes") to remove the User entity surface entirely. The `[JsonIgnore]` stopgap protects against accidental serialization of the existing fields, but if a new sensitive field is added to `User` in future, the next dev has to remember to mark it. The DTO approach is the durable fix.
  - **Files:** `Models/User.cs`
- **2026-05-03 DTO REFACTOR SHIPPED LOCAL** (cycle 4 QA pass — followup to the stopgap above):
  - Added `ProductIdResponseDto` and `ProductIdUserDto` to `Models/CertificateProductId.cs`. The DTO mirrors every public field on `CertificateProductId` and exposes only `{ id, username }` for `CreatedByUser` and `UpdatedByUser` — no email, no role, no audit fields, and no surface area for any future credential field added to `User`.
  - Updated all four read/write endpoints on `Controllers/Api/ProductIdApiController.cs` (`GetProductIdsByCertificate`, `GetProductId`, `CreateProductId`, `UpdateProductId`) to project entities via `ProductIdResponseDto.FromEntity(...)` before returning. `ApiResponse<CertificateProductId>` and `ApiResponse<List<CertificateProductId>>` are now `ApiResponse<ProductIdResponseDto>` / `ApiResponse<List<ProductIdResponseDto>>`.
  - `CreateProductId` and `UpdateProductId` now re-fetch the saved entity via `GetProductIdAsync(id)` (which has `Include(CreatedByUser).Include(UpdatedByUser)`) so the returned DTO has populated user nav data for the immediate row render.
  - **Service layer untouched.** `IProductIdService` still returns `CertificateProductId` entities — internal callers (e.g., `ExportToCsvAsync`, the cascade in `SyncProductIdStatusesForCertificateAsync`) keep their existing typed access. Only the controller boundary maps to DTO.
  - **JS frontend impact: zero.** The DTO preserves the exact field names the JS consumer reads: `productId.id`, `productId.productIdType`, `productId.productIdValue`, `productId.partner`, `productId.productName`, `productId.productSku`, `productId.description`, `productId.createdAt`, `productId.createdBy`, `productId.createdByUser.username`. Verified live (see below).
  - **Build:** 0 errors, 57 warnings (all pre-existing, unrelated to this change).
  - **Verified live (2026-05-03, `localhost:5101`, admin cookie):**
    - `GET /api/productids/certificate/48` (cert 26-0147, 2 product IDs) → returns 18 fields per row. `createdByUser` is `{ id, username }` only. Forbidden-fields scan: `passwordHash`, `twoFactorSecret`, `recoveryCodes`, `securityQuestions`, `allowedIPs`, `passwordResetToken`, `passwordHistory` — none present anywhere in the JSON.
    - `GET /api/productids/7` → same DTO shape, same forbidden-fields scan passes.
    - `PUT /api/productids/7` → DTO returned, forbidden-fields scan passes, status 200.
    - `GET /Certificate/Edit/26-0147` → table renders "admin" in Created By column for both rows; clicking Edit opens modal with all fields pre-populated from the DTO (Type=ASIN, Value=B0BRNPBZJ8, Partner=Amazon, ProductName populated, Description populated).
  - **Files:** `Models/CertificateProductId.cs`, `Controllers/Api/ProductIdApiController.cs`

---

### DEF-120 — Certificate Edit: Product Identifiers "Created By" column shows user ID instead of username

- **Date:** 2026-05-01
- **Severity:** Medium (functional bug — operators see meaningless numeric ID instead of human-readable username)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (`http://localhost:5101/Certificate/Edit/26-0150`, "Under product Identifiers shows 1 under Create By. This should be populated by the user name")
- **Root cause:** `wwwroot/js/product-id-manager.js:244` rendered the cell with `productId.createdBy` — that's the numeric foreign-key column from `CertificateProductId.CreatedBy` (e.g. `1` for the admin user). The Razor partial (`Views/Shared/_ProductIdManager.cshtml:118-119`) correctly reads `productId.CreatedByUser?.Username`, but the JS cell renderer (used after the page hydrates) wasn't using the same navigation property.
- **Fix:** Updated the cell render to read `(productId.createdByUser && productId.createdByUser.username) || 'System'`. The API already includes the navigation property (`Services/ProductIdService.cs:65 .Include(p => p.CreatedByUser)`), so no API change needed.
- **Files touched:** `wwwroot/js/product-id-manager.js` (single template literal at lines 244-246)
- **Verified locally (2026-05-01):** Playwright on `localhost:5101/Certificate/Edit/26-0150`. API returns `createdByUser.username: "admin"`. Rendered cells show `["admin", "admin", "admin"]` instead of the previous `["1", "1", "1"]`.

---

### DEF-119 — Certificate Create / Edit: Add Product ID modal too narrow on desktop, form collapses to single column

- **Date:** 2026-05-01
- **Severity:** Low (cosmetic — form still works, just wastes desktop screen real estate)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (`Screenshot_1-5-2026_175147_localhost.jpeg`, "Add Product ID Modal width is too narrow for desktop view")
- **Root cause:** Two stacked modal-width caps in `wwwroot/css/product-id-manager.css`:
  1. `#productIdModal .modal-dialog { max-width: 650px }` — capped the product modal at 650 px regardless of `modal-lg` class on the dialog (Bootstrap modal-lg defaults to 800 px). At 650 px, the form's `col-md-6` columns (Type | Product ID) still rendered side-by-side, but with no breathing room — and the description textarea looked cramped.
  2. `.modal-dialog { max-width: 600px }` — a bare global selector that capped EVERY Bootstrap modal on Certificate Create / Edit (the only views loading this CSS) to 600 px. The specific rule on `#productIdModal` already won, so this was a stealth leak affecting any other modal that happened to render on those pages.
- **Fix:** Bumped the productIdModal cap to 900 px (a touch wider than modal-lg's 800 px to give the description textarea more room) and removed the bare global `.modal-dialog { max-width: 600px }` rule entirely. Other modals on Create / Edit now get their default Bootstrap widths.
- **Files touched:** `wwwroot/css/product-id-manager.css` (two rules)
- **Verified locally (2026-05-01):** Playwright at 1280×1080 and 1920×1080.
  - 1280: dialog 900 px wide, 174 px margin each side. 2-column form renders with comfortable spacing.
  - 1920: dialog 900 px wide, 494 px margin each side. Centered nicely on the wider screen.
  - Visual confirms: Type | Product ID side-by-side at col-md-6, Partner / Product Name / SKU / Description full-width below, footer with Cancel + Save Product ID buttons properly spaced.
- **Coverage:** The `_ProductIdManager` partial is shared between Certificate Create and Certificate Edit, so both pages benefit from the single CSS edit.

---

### DEF-118 — Certificate Create: Product ID Manager export missing Created and Created By columns

- **Date:** 2026-05-01
- **Severity:** Low (data column gap in export — UI table shows Created / Created By, export omitted them)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn ("Product ID Manager Export does not have Created and Created By" on `/Certificate/Create`)
- **Root cause:** The Create page uses an in-memory CSV builder (`exportInMemoryProductIds` in `wwwroot/js/product-id-manager.js`) because there's no certificate ID yet to hit the server export endpoint. That builder only included `Type, Product ID, Partner, ProductName, ProductSku, Description` — missing the Created and Created By columns that the on-screen table shows. The header naming also drifted from the table ("ProductName" vs "Product Name", "ProductSku" vs "SKU") in violation of the DEF-110 standard ("export headers match table column labels").
- **Fix:** Updated `exportInMemoryProductIds` to emit `Type, Product ID, Product Name, SKU, Partner, Created, Created By, Description` — column order matches the on-screen table; Description trails since it's saved data but not a table column. Created formatted as `yyyy-MM-dd` from `pid.createdAt` (blank for new in-memory items), Created By reads `pid.createdByUser?.username || pid.createdBy || ''`. Round-trip safe: importer matches header names case-insensitively and ignores unknown headers, so adding columns doesn't break import.
- **Files touched:** `wwwroot/js/product-id-manager.js` (single function: `exportInMemoryProductIds`)
- **Verified locally (2026-05-01):** Playwright on `localhost:5101/Certificate/Create`. Added one row via Add Product ID modal (ASIN/B0ABCDEFGH/Amazon/Test Product/SKU-001/A test row). Captured exported CSV via Blob interception:
  ```
  Type,Product ID,Product Name,SKU,Partner,Created,Created By,Description
  "ASIN","B0ABCDEFGH","Test Product","SKU-001","Amazon","","","A test row"
  ```
  Created and Created By appear, blank as expected for a new unsaved certificate.
- **Note for follow-up (NOT changed):** The Edit-page server export (`Services/ProductIdService.cs:418`) currently emits `Type,Product ID,Status,Partner,ProductName,ProductSku,Description,CreatedAt,CreatedBy` — it still includes the legacy `Status` column (CR-015 says auto-derived, should be removed) and uses non-table-aligned header names. Two surfaces, two header conventions — worth a follow-up CR to align the server export to the same column shape as the in-memory export. Did not change in this fix per "only what was asked."

---

### DEF-117 — Cert tile views (Internal + External): Column Settings dropdown spills below viewport on mobile portrait — bottom of column list unreachable

- **Date:** 2026-05-01
- **Severity:** Medium (functional impact: ~half of column toggles unreachable on mobile portrait — users can't hide/show columns past the visible area)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (`Screenshot_1-5-2026_172446_localhost.jpeg`, mobile portrait, "column setting drop down display is off and unable to see the bottom of the sort list")
- **Root cause:** The mobile JS positioner (`positionDropdownForMobile` in `certificate-tile-view-internal.js`) set the dropdown's `top` to `toggleBtn.bottom + 4`, but did NOT adjust `max-height` to match available space below. The CSS hard-coded `max-height: calc(100vh - 120px)` (≈680 px on a 800 px viewport). With the toggle button at y≈423 in the viewport, the 680-px-tall dropdown extended to y≈1111 — 311 px past the 800 px viewport bottom. The dropdown was scrollable internally (`overflow-y: auto`, content was 1208 px tall), but the user could only see the top ~370 px because the rest was below the fold. The external view didn't have any positioner at all — it relied on default `position: absolute; top: 100%` so the dropdown opened below the button anchored to the document, also overflowing.
- **Fix:** Rewrote `positionDropdownForMobile` to dynamically size `max-height` to the actual available space, and to flip ABOVE the button when there's more room above than below:
  ```js
  const below = vpH - r.bottom - margin;
  const above = r.top - margin;
  if (below >= above) {
      // open below: top = btn.bottom+4, max-height = below
  } else {
      // flip above: bottom = vpH - btn.top + 4, max-height = above
  }
  ```
  Floor of 200 px applied so the dropdown is always usable even on tiny viewports. External view got the same logic added (it didn't have one before — same code, same JS structure).
- **Files touched:** `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`
- **CSS unchanged:** existing `@media (max-width: 768px)` rule with `position: fixed` still applies. JS now overrides `max-height` and (when flipping) the `top`/`bottom` properties.
- **Verified locally (2026-05-01):** Playwright at 360×800.
  - Button mid-viewport (scrollY=503, btnTop=375): opens below, `max-height: 364 px`, `dd.bottom: 796 px` (4 px margin from viewport bottom). `ddBelowViewport: false`. Internal scroll active for 1208 px content.
  - Button near top of viewport (scrollY=1542, btnTop=45): opens below, `max-height: 695 px`, dd extends from 101→796 — uses nearly the full viewport. Visual confirms full Column Settings list reachable via internal scroll. CORE FIELDS header visible, COMPANY & PRODUCT section visible just below.
- **Coverage:** Internal and external cert tile views both fixed. Shared CSS (no change needed).

---

### DEF-116 — Certificate Create / Edit: Product Identifier filter row overflows mobile viewport

- **Date:** 2026-05-01
- **Severity:** Medium (filter controls partially / fully off-screen on phones — Search input was completely invisible at 360 px)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn ("Product Identifier section — controls are not contained in the view on mobile")
- **Root cause:** The filter row in `Views/Shared/_ProductIdManager.cshtml` used inline styles `display: flex; flex-wrap: nowrap` plus fixed pixel widths (`min-width: 110px`, `min-width: 120px`, `width: 200px`). At a 360 px portrait viewport the available container width was 190 px, but the filter row's intrinsic width was 449 px — so the partner dropdown was clipped ("All Partne...") and the search input was entirely off-screen. Inline styles also made the row impossible to override with media queries.
- **Fix:** Moved layout into a CSS class (`.product-manager-filter-row`) and switched from `nowrap` to `wrap`. Type and Partner dropdowns keep their min-width but no longer force a single line. Search uses `flex: 1 1 200px` — grows to fill remaining space at desktop/tablet widths, wraps to its own line on phones. Also added a scroll-shadow background gradient on `.product-ids-table-container` so users get a visual cue that the data table extends horizontally beyond the viewport (the table still requires `min-width: 800px` to keep its 8 columns readable).
- **Files touched:**
  - `Views/Shared/_ProductIdManager.cshtml` — replaced inline `style=""` attributes with CSS classes (`.product-manager-filter-row`, `.pid-filter-type`, `.pid-filter-partner`, `.pid-filter-search`)
  - `wwwroot/css/product-id-manager.css` — added the new flex-wrap rule + scroll-shadow background on the table container
- **Coverage:** The `_ProductIdManager` partial is shared across `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`, and any other view that includes it — single source-of-truth fix.
- **Verified locally (2026-05-01):** Playwright at 360 / 768 / 1024 px viewports.
  - 360 px (mobile): inner filter row went from `scrollWidth=449/clientWidth=190 (overflow=259 px)` to `scrollWidth=190/clientWidth=190 (zero overflow)`. All three controls now fully visible on three wrapped rows. Visual confirmed.
  - 768 px (tablet): all three controls fit on one row; search expands to 311 px to fill remaining space.
  - 1024 px (desktop): single-row layout intact; search expands to 491 px.

---

### DEF-115 — Cert tile views (Internal + External): "Columns" toggle button left-aligns on narrow viewports instead of right-aligning

- **Date:** 2026-05-01
- **Severity:** Low (cosmetic alignment, no functional break)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn ("most likely external too. Columns should be right aligned.")
- **Root cause:** `.table-toolbar-row` is a flexbox with `flex-wrap: wrap` and `justify-content: space-between`. At narrow widths (~360 px portrait phone), the "Showing N certificates" left text plus the "Columns" right button don't fit on one line, so flex wraps the right element to its own line. `space-between` only distributes items WITHIN a flex line — when the right element is alone on line 2, it falls to the line's flex-start position (left edge) and reads as left-aligned.
- **Fix:** Added `margin-left: auto` to `.table-toolbar-right`. On a wrapped flex line containing only this item, the auto margin expands to fill the line's spare space and pushes the button to the right edge. Desktop behavior is unchanged because `space-between` already produced the same outcome there.
- **Files touched:** `wwwroot/css/certificatetileview.css` (single declaration on `.table-toolbar-right`).
- **Coverage:** Same selectors and same stylesheet are shared between `Views/Certificate/CertificateTileViewInternal.cshtml` and `Views/Certificate/CertificateTileViewExternal.cshtml`, so a single CSS edit fixes both.
- **Verified locally (2026-05-01):** Playwright sweep across 5 viewports (360, 768, 900, 1024, 1280). At every width, button now sits 16 px from the row's right edge. At 360 px the button x-position moved from 56 → 172 (left edge → right edge of available line). Visual screenshot confirms toolbar reads "Showing 20 of 40 certificates" on line 1 and right-aligned "Columns" button on line 2.

---

### DEF-114 — Dashboard mobile portrait: column 2 of stats tables clipped, headers/values truncated

- **Date:** 2026-05-01
- **Severity:** Medium (visible data loss on mobile portrait — column 2 numbers were cropped at the right edge of the section)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (screenshot `Screenshot_1-5-2026_164437_localhost.jpeg`, "data on dashboard tables are not showing all the data, the formatting is cutting off text in mobile portrait view")
- **Root cause:** At 360 px viewport, the standard admin shell ate ~50 px of horizontal space per side: `.admin-container { margin: 10px; padding: 15px }` + `.admin-body-content { padding: 24px }`. The `.tables-container` was rendered at only **216 px wide**, while the inner `<table>` measured 232–263 px depending on the second-column header label. `.table-section { overflow: hidden }` then **silently clipped** the right edge — that's why the headers and values appeared cut off in the screenshot.
- **First attempt (rejected):** Added `overflow-wrap: anywhere; word-break: break-word;` to cell text. This caused catastrophic character-by-character vertical stacking (one letter per line) — confirmed via Playwright screenshot. Reverted.
- **Second attempt (rejected):** Capped `th:last-child { max-width: 130px }` to force column 2 narrower. Treated the symptom (table wider than container) without fixing the cause (container too narrow).
- **Final fix:** Scoped admin-shell padding/margin reduction to a new `.dashboard-page` class on the dashboard's `.admin-container` element. At ≤480 px, dashboard-only shell padding drops from 10/15/24 → 4/6/4 per side, returning ~46 px of horizontal space to the tables-container. Sections now measure 274 px wide and tables fit cleanly at 272 px.
- **Files touched:**
  - `Views/Dashboard/Index.cshtml` — added `.dashboard-page` class to the root `.admin-container` div (line 14)
  - `wwwroot/css/pages/dashboard.css` — inside `@media (max-width: 480px)`: removed the `th:last-child max-width` kludge, added `.dashboard-page.admin-container` and `.dashboard-page .admin-body-content` padding/margin overrides
- **Verified locally (2026-05-01):** Playwright at 360×800 mobile viewport. `tables-container` width 216 → 274 (+58 px). All 5 tables: `tableScrollWidth === tableClientWidth = 272`, no clipping. `documentElement.scrollWidth === viewport (360)`, no horizontal scroll. Visual screenshot confirms all column 2 headers fully visible ("TOTAL ACTIVE CERTS", "# OF ACTIVE CERTIFICATES", etc.) and all numeric values readable.
- **Scoping note:** The `.dashboard-page` class isolates the padding cut to the Dashboard view only. Other admin pages (`UserManagement`, `Tags`, etc.) keep their existing 10/15/24 shell padding — none of those pages have the same column-2 truncation problem because their tables either scroll horizontally (DEF-110 user-management) or use card layouts.

---

### DEF-113 — Remove `/Admin/Index` admin dashboard page (client doesn't want it)

- **Date:** 2026-05-01
- **Severity:** Low (feature removal, no functional bug)
- **Status:** **Fixed** — pending deploy
- **Reporter:** Shawn (client preference: client doesn't want a standalone admin dashboard)
- **Change:**
  - Deleted `Views/Admin/Index.cshtml`
  - `AdminController.Index()` now redirects to `UserManagement` so any old bookmarks land somewhere useful instead of 404
  - Replaced `<a href="@Url.Action("Index", "Admin")">Administration</a>` with non-clickable `<span>Administration</span>` in 18 view breadcrumbs (matches the "Companies & Products" pattern from DEF-104)
- **Files touched:** `Controllers/AdminController.cs`, `Views/Admin/Index.cshtml` (deleted), 18 breadcrumb-bearing views: `Views/Admin/{CertificateFields,CertificateTypes,CreateUser,CSFAttributes,GCCAPI,GoogleAnalytics,PartnerAPI,Partners,Pathways,ProductManagement,ProductTypes,RoleManagement,Tags,UserManagement}.cshtml`, `Views/Certificate/{Create,Edit}.cshtml`, `Views/Company/Index.cshtml`, `Views/Product/Management.cshtml`
- **Verified locally (2026-05-01):** `/Admin/Index` redirects to `/Admin/UserManagement`. `/Admin` (no action) returns 404 (MVC default routing). All 18 breadcrumbs render "Administration" as a plain span. Login flow unaffected — Admin role still routes to `Dashboard/Index` (different page) on login per existing `HomeController` and `AccountController` redirects.
- **Note:** `_adminService.GetDashboardDataAsync()` is now an orphan method — left in place this cycle, can be removed in a future cleanup.

---

### DEF-112 — Partner role: web-user "Partner" vs Partner API "Partner" share the same code path with different identities — needs an architectural decision

> **2026-05-05 REVERSAL:** Path 4 ("eliminate web-user Partner role") was reversed by Shawn after testuser5 testing surfaced no nav for Partner-role users. The actual requirement is Partner web users DO exist and mirror External access pattern, filtered by partner instead of by company. See **DEF-142** for the new resolution. The Roles.IsActive=false flip was reversed on dev DB (back to IsActive=true). Prod DB flip was never run, so no prod rollback needed. The `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` script referenced below was never actually committed to the repo. Architectural concern (dual-meaning of `userRole == "Partner"` string) remains as a future hardening item — see DEF-142 caveat.

- **Date:** 2026-05-01 (reported), 2026-05-04 (resolved via Path 4), 2026-05-05 (Path 4 reversed → Path 2-equivalent in DEF-142)
- **Severity:** Medium (no current bug, but the dual meaning makes future changes risky and the model is ambiguous to operators)
- **Status:** **Fixed (source + dev DB) — pending prod SQL run.** Path 4 selected after confirming on 2026-05-04 that the prod DB has zero human users with the Partner role (verified via `GET /Admin/GetUsers` → role distribution: Admin / Analyst / External / Public only; no `"role": "Partner"` matches across all 23 users).
- **Resolution chosen — Path 4 (eliminate web-user Partner role):** The Partner role exists in the Roles table (id=6, IsActive=true) but is only ever assigned by `PartnerApiKeyAuthenticationMiddleware`, which injects `ClaimTypes.Role = "Partner"` directly without consulting the table. Soft-deactivating the row (`IsActive = false`) hides it from the User Management role dropdown (`GetRolesListAsync` filters on `IsActive`) and prevents future admins from accidentally creating human Partner users. Zero impact on Partner API auth (middleware doesn't read the row), zero impact on existing users (none are assigned). Fully reversible.
- **Files touched (2026-05-04):**
  - `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` — idempotent script. Sets `Roles.IsActive = 0` on the Partner row + updates the description. Includes a defense-in-depth pre-check that aborts if any human users have the Partner role. Uses EF many-to-many table FK names `UserRoles.RolesId` / `UserRoles.UsersId`. Run on dev DB 2026-05-04: BEFORE→AFTER snapshot confirms IsActive=1→0.
  - `Data/SeedData.cs:94` — Partner role definition now sets `IsActive = false` and the new description for forward consistency on fresh DBs. Comment block above the line explains the source-of-truth.
  - `Middleware/PartnerApiKeyAuthenticationMiddleware.cs` (around line 132) — added a comment block at the claim-injection site marking it as the SINGLE SOURCE OF TRUTH for the "Partner" role claim and pointing to DEF-112.
- **What this does NOT change:** the existing `userRole == "Partner"` checks in `CertificateTileExternalApiController.cs` and `Controllers/Api/v1/CertificatesController.cs` are left in place — they're correct. They only ever fire for API-key callers since no humans have the role. The dual-meaning concern from the original report is moot once the role is unassignable to humans. If a future requirement needs human Partner users, this is reversible: re-run the SQL with `IsActive = 1` and reapply `IsActive = true` in SeedData.
- **Prod SQL run pending (Shawn):** `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` needs to run against the prod DB. Idempotent and safe — has the human-count abort guard. Verified on dev 2026-05-04.
- **Original problem statement (preserved):** Two completely different identities both flow through `if (userRole == "Partner")` in cert-listing controllers, and the codebase doesn't distinguish them at the policy/audit layer:
- **Reporter:** Surfaced while explaining how Partner role gets applied during the Edit User modal review (2026-05-01)
- **Problem:** Two completely different identities both flow through `if (userRole == "Partner")` in cert-listing controllers, and the codebase doesn't distinguish them at the policy/audit layer:
  1. **Web-user Partner role** — a human user logs in with email/password (cookie session). `User.Partners` collection is populated via the Edit User modal (Partner Assignment dropdown). They see the web UI filtered by `userPartnerNames.Contains(c.Partner ?? "None")`.
  2. **Partner API key Partner role** — Amazon's automated systems hit `/api/v1/certificates*` with `X-API-Key`. Middleware assigns role="Partner" for the request lifetime. Same filter branch fires.
- **Where the conflation lives:**
  - `Controllers/Api/CertificateTileExternalApiController.cs:98-110` — web-user partner filter
  - `Controllers/Api/v1/CertificatesController.cs:943` — Partner API key middleware comment "Partner API keys get 'Partner' role from middleware"
  - `Controllers/Api/v1/CertificatesController.cs:993-995` — same `user.Partners.Any()` lookup, but for an API key context that doesn't have a user record
  - `Models/User.cs:102` — `User.Partners` is a many-to-many collection; modal saves a single value but the model supports multi
- **Why this matters:**
  - Audit trail: a human Partner user editing their own data and an Amazon API call look identical in claims-based logs.
  - Brittle filter expansion: any future filter change has to be reasoned about under both identities. Hard to test.
  - Onboarding confusion: the modal has Partner Assignment for web users, but most "Partner" semantics in stakeholder conversations refer to the API contract — not human users.
  - Memory: `feedback_amazon_api_immutable.md` says Partner API contract is locked; web-user role logic shares code paths with that locked contract, so an internal-app refactor could accidentally break the contract.
- **Resolution paths to consider (DECISION NEEDED, not picking one):**
  1. **Rename web-user role** to `PartnerUser` or `BrandPartner` and keep `Partner` as the API-key-only role. Cleanest separation; requires DB migration on `Roles` table + UI label updates.
  2. **Split the filter branch** into two: `userRole == "PartnerApiKey"` vs `userRole == "Partner"`, with the API-key path deriving partner names from the key's metadata, not `User.Partners`.
  3. **Document the dual meaning** in code comments + add a `[Display(Name="Partner User")]` on the role and leave the wire format alone. Lowest-effort but doesn't fix the audit-trail confusion.
  4. **Eliminate the web-user Partner role** entirely if no human user actually needs it — Amazon's UAT users could become External users with company scoping instead.
- **Open questions for Shawn:**
  - Are there *currently* any human users with Partner role in production, or is it only the API-key path that's used?
  - Does Carly/the client expect Partner-role *humans* to be able to log in (e.g., Amazon staff with portal logins)?
  - If yes, should they see their *partner's* certs only, or *their company's* certs + their partner's certs?
- **Effort estimate:** Path 3 (~2 hrs documentation), Path 1/2 (~6-10 hrs code + migration + tests), Path 4 (~4 hrs code + data migration).
- **Files to touch (any path):** `Controllers/Api/CertificateTileExternalApiController.cs`, `Controllers/Api/v1/CertificatesController.cs`, `Controllers/ProductController.cs`, `Models/User.cs`, `Views/Admin/UserManagement.cshtml`, `wwwroot/js/userManagement.js` + migration if Path 1.

---

### DEF-111 — `/Product/Management` "Error loading products: SyntaxError: Unexpected token '<'" — fetch parses HTML auth-redirect as JSON

- **Date:** 2026-05-01 (reported), 2026-05-04 (fixed + deployed)
- **Severity:** Medium (silent functional failure; the user sees an empty/broken product list with no actionable error)
- **Status:** **Deployed 2026-05-04** — verified live on gccwebapp.com: served `/Product/Management` HTML contains the `response.redirected || !contentType.includes('application/json')` guard and the `AdminCore.showNotification('Your session has expired. Reloading…', 'error')` toast call.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Logged in as analyst, navigating to `/Product/Management` produces this console error: `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON`. The page renders but the products table is empty / not populated. No visible error toast or banner — the user has no idea anything went wrong.
- **Root cause confirmed:** The JS in `Views/Product/Management.cshtml` `loadProducts()` was calling `response.json()` after only checking `response.ok`. When the server-side cookie session expires mid-page-life, an AJAX `fetch('/Product/GetProducts')` is auto-redirected by the browser to `/Account/Login`, which returns HTTP 200 with `Content-Type: text/html` (the login page). `response.ok` is `true`, so `.json()` runs and chokes on the leading `<` of `<!DOCTYPE`. The `catch` block then logs to `console.error` only — no toast — so the user sees an empty table and no actionable feedback.
- **Verified guard premise (2026-05-04):** A live `fetch('/Account/Login')` from the running app sets `response.redirected: true` and `Content-Type: text/html; charset=utf-8` — so a `(redirected || !contentType.includes('application/json'))` check correctly identifies the auth-redirect case.
- **Fix (2026-05-04):** Updated `loadProducts()` in `Views/Product/Management.cshtml` to:
  1. Read `response.headers.get('content-type')` and check for `application/json` before calling `.json()`. If the response was redirected or the content type isn't JSON, surface `AdminCore.showNotification('Your session has expired. Reloading…', 'error')` and `window.location.reload()` after 1500ms — giving the user a real signal and re-entering the auth flow cleanly.
  2. Replace silent `console.error` calls in the non-OK and `catch` branches with visible `AdminCore.showNotification(..., 'error')` toasts — so any future fetch failure (network, 5xx, etc.) is surfaced to the user instead of masked.
- **Scope decision:** Only `loadProducts()` was modified. The same pattern applies to four other fetch sites in this file (`loadInitialData`, save/edit/delete handlers) and to several other admin pages — those are out of scope for this defect. If the auth-redirect symptom is reported elsewhere, fix per-site or refactor into a shared helper. Per `feedback_only_requested_changes.md`, not expanding scope here.
- **Files:** `Views/Product/Management.cshtml`
- **Verification:** Source-side `loadProducts()` now contains the `response.redirected || !contentType.includes('application/json')` guard and `AdminCore.showNotification` calls. Localhost runtime verify pending — Razor runtime compilation isn't configured, so the change reaches the browser only after the next dev server restart / publish.
- **Found by:** `fullspectrum_out/console-errors-rollup.json` (2026-05-01 verification run for DEF-107/108 fix).

### DEF-110 — CSV export headers don't match UI table column labels (12 export surfaces)

- **Date:** 2026-05-01
- **Severity:** Medium (analyst confusion; data is correct but headers diverge from screen)
- **Status:** **Fixed (local)** — JS/Razor live on localhost:5101; C# changes in `CertificateService.cs` and `AdminService.cs` need server restart to take effect
- **Reporter:** Shawn — surfaced via export audit, instruction "the exports should match what is showing in the UI"
- **Affected exports & fixes:**
  1. **User Management** (`wwwroot/js/userManagement.js`) — was `Name, Email, Role, Status, User Company, Last Login`; now `Name, Company, Email, Role, Status, Last Login, 2FA` (matches UI; supersedes QC #108 "User Company" naming — import template still uses "User Company" for round-trip).
  2. **Company Management** (`wwwroot/js/admin/companyManagement.js`) — dropped `Id`, `Name`→`Company Name`, `Website`→`URL`.
  3. **Admin/ProductManagement** (`Views/Admin/ProductManagement.cshtml` inline) — was `Name, Description, SKU, Model, Status`; now `Product Name, Parent Identifiers, Status` (matches UI; SKU/Model/Description dropped).
  4. **Product/Management** (`Views/Product/Management.cshtml` inline) — now `Product Name, Company, Category, Product Type, Subcategory, Status, Certs, IDs`. Added Subcategory + Status; identifiers consolidated to one `IDs` column to match UI summary cell. Note: import template kept its own column set (round-trip with template intentional).
  5. **Pathways** (`Views/Admin/Pathways.cshtml` inline) — was `Name, Description, Active, Sort Order`; now `Pathway Name` only (matches UI table).
  6. **Tags** (`wwwroot/js/tags.js`) — dropped `ID` column; remaining `Tag Name, Tag Type, Description` matches UI.
  7. **Certificate Types** (`wwwroot/js/admin/certificate-types.js`) — dropped `ID`; `Certificate Count`→`Certificates`.
  8. **Certificate Fields** (`wwwroot/js/admin/certificate-fields.js`) — dropped `ID`/`Sort Order`; `Name`→`Field Name`, `Field Type`→`Control Type`, `Visible`→`Visibility`. Now matches UI exactly.
  9. **Product Types** (`wwwroot/js/product-types.js`) — replaced `ExportUtils.showExportModal` (CSV/JSON/Excel chooser, violated CSV-ONLY rule) with `exportToCSV` direct. Dropped `ID`. `Hierarchy Level`→`Level`.
  10. **Certificate tile export** (`Services/CertificateService.cs:1025`, ⚠ NEEDS RESTART) — header text now matches tile UI: `Certificate Number`→`Certificate #`, `Company Name`→`Company`, `Product Subcategory`→`Subcategory`, `Partner Program`→`Partner`, `Certification Results`→`Cert Results`, `Certification Subcategories`→`Cert Subcategories`, `External Tags`/`Internal Tags`→singular, `Company Contact Email`/`Company Contact URL`→`Company Contact Email/URL`/`Contact URL`. Column order preserved.
  11. **Server-side admin exports** (`Services/AdminService.cs:1983`, ⚠ NEEDS RESTART) — Title-cased all headers and dropped `Id`/internal fields:
      - users: `Name, Company, Email, Role, Status, Last Login`
      - roles: `Role Name, Description, Permissions, Users`
      - certificate-types: `Name, Description, Active, Certificates`
      - certificate-fields: `Field Name, Control Type, Required, Visibility, Notes`
      - partners: `Partner Name, Description, Website, Active, Users, Created Date`
      - product-types: `Name, Type, Parent, Level, Sort Order, Product Count`
- **Side fixes shipped in same change:**
  - **Duplicate-handler bug**: `wwwroot/js/admin/productManagement.js:31` was auto-registering on `#export-btn` from the class constructor while inline handlers in both `Admin/ProductManagement.cshtml` and `Product/Management.cshtml` also registered — clicking Export downloaded **two** CSVs per click. Removed class-level auto-registration; class method `exportProducts()` retained for explicit callers.
  - **CSV-ONLY rule violation**: `product-types.js` was the only admin page using `ExportUtils.showExportModal` (lets user pick CSV/JSON/Excel). Replaced with direct `exportToCSV` call.
- **Known carry-overs (not changed):**
  - User import template still uses "User Company" header (per Carly QC #108) — round-trip with import preserved.
  - Product import template (`Views/Product/Management.cshtml downloadImportTemplate`) still uses its own column set — intentional, separate from export.
  - Cert export column **order** unchanged in `CertificateService.cs` — only header **text** reworded; reordering would ripple into all consumers.
- **Files touched (10):** `wwwroot/js/userManagement.js`, `wwwroot/js/admin/companyManagement.js`, `wwwroot/js/admin/productManagement.js`, `wwwroot/js/tags.js`, `wwwroot/js/admin/certificate-types.js`, `wwwroot/js/admin/certificate-fields.js`, `wwwroot/js/product-types.js`, `Views/Admin/ProductManagement.cshtml`, `Views/Admin/Pathways.cshtml`, `Views/Product/Management.cshtml`, `Services/CertificateService.cs`, `Services/AdminService.cs`.
- **Verification:** JS files served by running dev server confirmed via `Invoke-WebRequest` to contain new content. Live click-through verification not performed; recommend Playwright spot-check after server restart.

---

### DEF-109 — Tile listings render Effective/Expiration date one day earlier than the DB value

- **Date:** 2026-05-01
- **Severity:** High (visible to analysts and partners; displayed date does not match entered date)
- **Status:** **Closed** — deployed and validated by Shawn on 2026-05-01
- **Reporter:** Shawn (live test on cert 26-0160)
- **Symptom:** Edit form shows the correct date (e.g. `5/10/2026`), but the same cert on `/Certificate/CertificateTileViewInternal` and `/Certificate/CertificateTileViewExternal` renders one day earlier (`5/9/2026`). Public tile view (`/Certificate/CertificateTileView`) was correct.
- **Root cause:** Global `UtcDateTimeConverter` (`Extensions/UtcDateTimeConverter.cs`, registered in `Program.cs:443-444`) serializes every `DateTime` as `"yyyy-MM-ddTHH:mm:ssZ"` — including calendar-only dates whose `Kind` is `Unspecified`. The `Z` suffix tells the browser the value is UTC midnight; in any negative-UTC timezone the browser then converts to local and `toLocaleDateString` renders the previous calendar day. The internal/external tile APIs were emitting raw `c.EffectiveDate` / `c.ExpirationDate` and went through the converter. The public tile API was already pre-formatting with `.ToString("yyyy-MM-dd")`, which is why only public was correct. Server-rendered Razor views (Edit form, `CertificateInternalView`, `CertificateExternalView`, `CertificateSingleView`) were also correct because they format server-side and never round-trip through JS.
- **Why the converter exists:** Per its header comment, it was added so the **Partner API** (`/api/v1/certificates*`) emits `"2024-01-15T00:00:00Z"` to match Amazon's documented format. Removing it globally would break the Partner API contract (`feedback_amazon_api_immutable.md`), so the fix mirrors the public API's existing pre-formatting pattern in the two tile APIs only.
- **Fix:** In the in-memory projection of both `Controllers/Api/CertificateTileInternalApiController.cs` and `Controllers/Api/CertificateTileExternalApiController.cs`, format the two date fields explicitly: `EffectiveDate = c.EffectiveDate.ToString("yyyy-MM-dd")` and `ExpirationDate = c.ExpirationDate.ToString("yyyy-MM-dd")`. JS `parseLocalDate` then matches its date-only regex branch and constructs a local `Date` — no UTC drift. Partner API output is unchanged.
- **Files:** `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.
- **Verification:** Playwright run on `http://localhost:5101` (`C:\Users\swest\AppData\Local\Temp\uitest\verify_def_104_dates.js`, viewport timezone `America/New_York`):
  - Internal API: 3 sampled certs all returned dates as `yyyy-MM-dd` (e.g. `26-0023 eff=2027-07-08 exp=2028-07-07`).
  - External API: same shape (e.g. `26-0025 eff=2026-02-25 exp=2028-02-25`).
  - Internal tile card 1 rendered `7/8/2027` — matches the API's `2027-07-08` exactly (no off-by-one).
  - Build clean (0 errors, 49 warnings unchanged).
- **Cross-reference:** Same class of bug as the previously-shipped QC #23 fix, but a different surface (those were the cert detail view; this is the tile list APIs). The `UtcDateTimeConverter` was added later for Partner API compliance and re-introduced the drift on the JSON-API surfaces it touches.
- **Deploy:** `GCCWebApp-test-2026-05-01-1051.zip` (bundled with DEF-103, DEF-106, Manage Workflow seed removal). Validated live by Shawn 2026-05-01.

### DEF-108 — `Cross-Origin-Embedder-Policy: require-corp` blocks Google Analytics and other cross-origin embeds on every page

- **Date:** 2026-05-01
- **Severity:** Medium (analytics blackout + console-error noise on every page load; no functional break for the app itself)
- **Status:** **Fixed (local) — pending deploy.** Verified on `localhost:5101` 2026-05-01: console errors dropped from 112 to 0 GA-related errors after both header changes.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Browser console on every authed and unauthed page shows:
  - `Failed to load resource: net::ERR_BLOCKED_BY_RESPONSE.NotSameOriginAfterDefaultedToSameOriginByCoep` (5x on `/Account/AccessDenied` alone)
  - `Connecting to 'https://analytics.google.com/g/collect?...' was blocked due to "require-corp" being set as Cross-Origin-Embedder-Policy` (2x per cert tile page load)
  - `Fetch API cannot load https://analytics.google.com/g/collect?...` (paired with each block)
- **Root cause:** `Middleware/SecurityHeadersMiddleware.cs` line 102 sets `Cross-Origin-Embedder-Policy: require-corp`. This policy mandates that every cross-origin embedded resource must respond with `Cross-Origin-Resource-Policy: cross-origin`. Google Analytics endpoints (`analytics.google.com`, `www.google.com/g/collect`) and `googletagmanager.com` do NOT set CORP, so they're blocked by the browser. Same root cause produces the AccessDenied resource block.
- **Why it matters:**
  1. GA is silently NOT recording any page views in dev OR test env (and probably in prod too — needs verification post-deploy). Whatever data the client thinks they're getting from GA, they're not.
  2. Console-error noise makes real defects harder to spot during QA.
  3. COEP is only valuable when the app uses `SharedArrayBuffer` / cross-origin isolation features. This codebase does not.
- **Sibling effect:** `Cross-Origin-Resource-Policy: same-origin` (line 99) is fine — only affects how OUR resources can be embedded by other origins. Left in place.
- **Fix applied (two-part):**
  1. `Middleware/SecurityHeadersMiddleware.cs` — changed `Cross-Origin-Embedder-Policy` from `require-corp` to `unsafe-none`. This unblocked the cross-origin script load.
  2. Same file — extended `connect-src` in CSP from `'self' https://api.greencirclecertified.com https://www.google-analytics.com https://www.googletagmanager.com https://cdn.jsdelivr.net` to additionally include `https://*.google-analytics.com https://analytics.google.com https://www.google.com https://stats.g.doubleclick.net`. GA4 fires beacons to all of these endpoints (not just `www.google-analytics.com`); without them in connect-src, the unblocked scripts could load but their fetches were CSP-rejected.
- **Verification on `localhost:5101` (2026-05-01):** Re-ran `fullspectrum_ui.js`. Console errors total: **112 → 1** (-99%). The single remaining is DEF-111 (`/Product/Management` SyntaxError, unrelated). All `analytics.google.com`, `www.google.com/g/collect`, `stats.g.doubleclick.net` fetches succeed.
- **Files touched:** `Middleware/SecurityHeadersMiddleware.cs` (CSP `connect-src` line, Permissions/Feature-Policy block, COEP value)
- **Found by:** `fullspectrum_out/console-errors-rollup.json` produced by the 2026-05-01 full-spectrum sweep.
- **Deploy:** Pending — bundles cleanly with DEF-107.

### DEF-107 — Duplicate `Feature-Policy` + `Permissions-Policy` headers on every response (browser warning 16x per session)

- **Date:** 2026-05-01
- **Severity:** Low (warning only; no functional break)
- **Status:** **Fixed (local) — pending deploy.** Verified on `localhost:5101` 2026-05-01: the 16x warning entry no longer appears in `console-errors-rollup.json`.
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Every page load (login, dashboard, public tile view, internal tile view, external tile view, all admin pages) emits this Chromium console warning: `Error with Feature-Policy header: Some features are specified in both Feature-Policy and Permissions-Policy header: camera, microphone, geolocation, payment, usb. Values defined in Permissions-Policy will be used.` Total warnings logged across the sweep: 16 across 11 distinct page URLs.
- **Root cause:** `Middleware/SecurityHeadersMiddleware.cs` lines 91 and 95 emit BOTH headers with overlapping directives. The comment on line 94 says "for older browsers" — but every browser that supports the cert tile view JS (ES2017+ async/await, fetch, etc.) supports `Permissions-Policy`. Sending both is redundant and produces the warning.
- **Why it matters:**
  1. Console-error noise during QA — masks real defects.
  2. The redundancy implies older-browser support that the rest of the codebase doesn't actually deliver.
- **Proposed fix (one-line):** Remove the `Feature-Policy` `Append` call (lines 94–96) in `Middleware/SecurityHeadersMiddleware.cs`. Keep `Permissions-Policy`.
- **Files touched (proposed):** `Middleware/SecurityHeadersMiddleware.cs` lines 94–96
- **Verification (when fixed):** Re-run `fullspectrum_ui.js`. The 16x warning entry should drop out of `console-errors-rollup.json`.
- **Found by:** `fullspectrum_out/console-errors-rollup.json` produced by the 2026-05-01 full-spectrum sweep.
- **Deploy:** Open

### DEF-097 — Partner API `certificationCountries` must always be "All"
- **Date:** 2026-04-24
- **Severity:** Medium
- **Status:** Deployed — verified live 2026-04-24
- **Reporter:** Shawn (client confirmation pending at time of fix)
- **Contract:** Amazon Developer Guide — partner responses should always report `All` for country scope, regardless of what the cert's Country field holds internally.
- **Root cause:** `Controllers/Api/v1/CertificatesController.cs` projected `certificationCountries = c.Country` (and GS1 `TargetCountries = new List<string> { cert.Country }`) — so whatever analysts typed into the cert's Country field (e.g. "Germany") leaked into Amazon's API response. Most certs had blank Country → `null` in the response. Inconsistent output.
- **Fix:** Gated the projection on `isPartnerCaller`. Partner API callers now always see `"All"`; internal Admin/Analyst views still see the raw `c.Country` value for their own workflows.
- **Files changed:**
  - `Controllers/Api/v1/CertificatesController.cs` lines 241, 287, 868 (standard endpoints)
  - `Controllers/Api/v1/CertificatesController.cs` line ~1406 (GS1 `MapToGS1Format` helper — hardcoded `["All"]`)
- **Samples updated:**
  - `docs/tracking/api-samples/2026-04-24_amazon_standard.json`
  - `docs/tracking/api-samples/2026-04-24_amazon_gs1.json`
- **Deploy:** `GCCWebApp-test-2026-04-24-1100.zip`

### DEF-098 — Tablet view: duplicate "Hide Filters" buttons on logged-in cert listing
- **Date:** 2026-04-24
- **Severity:** Low
- **Status:** Fixed — pending deploy
- **Reporter:** Shawn (QC re-review of prior SIT item #163)
- **Symptom:** On tablet width (≤1024px), the internal/external certificate tile view showed two stacked blue bars at the top — "Hide Filters" (desktop sidebar-collapse button) and "Hide Filters & Search" (mobile toggle). Both functional, both mounted, confusing UI.
- **Root cause:** `.sidebar-collapse-btn` is `display: flex` at all breakpoints. The tablet breakpoint (`@media (max-width: 1024px)`) turns on `.mobile-filter-toggle` but never hides the desktop collapse button, so they render together.
- **Fix:** Added `.sidebar-collapse-btn, .sidebar-toggle-tab { display: none !important; }` inside the `max-width: 1024px` media query in `wwwroot/css/certificatetileview.css`. The mobile toggle is the single filter control on tablet/mobile; the desktop collapse controls are irrelevant because the sidebar always renders full-width at tablet.
- **Files changed:**
  - `wwwroot/css/certificatetileview.css` (tablet media block around line 2834)
- **Relates to:** SIT item #163 (filter collapse fix). #163 addressed collapsing the individual filter panels via the down-chevron — different control from this one.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-100 — Tile-view list/table mode: lazy-load wipes earlier pages, only latest page visible
- **Date:** 2026-04-26
- **Severity:** High
- **Status:** Fixed — pending deploy
- **Reporter:** Shawn (live observation on `https://gccwebapp.com/Certificate/CertificateTileViewInternal`)
- **Symptom:** On the certificate tile page in **list/table view**, scrolling to trigger the next page replaces the previously rendered rows with only the new page. User sees "the remainder, not the whole of the dataset". Tile (card) view was unaffected. Reproduced on both internal and external tile pages.
- **Root cause:** In `renderCertificates(append=true)` of both `certificate-tile-view-internal.js` (line ~309) and `certificate-tile-view-external.js` (line ~302), the list-view branch called `renderTableRows(newCerts)`. `renderTableRows` is a full-render helper that does `gridBody.innerHTML = certificates.map(...).join('')`, so passing only the new page's rows wiped every previously rendered row. The tile branch correctly used `grid.insertAdjacentHTML('beforeend', html)` and was fine. Public `certificate-tile-view.js` passes the full `state.certificates` to `renderTableRows` after card append, so it re-rendered every row each scroll — slower but visually correct.
- **Fix:** In the list-view append branch of both internal and external scripts, build HTML for the new certs only and call `gridBody.insertAdjacentHTML('beforeend', html)`. After insert: attach `change` listeners to the new `.row-checkbox` elements (so bulk selection still works on later pages), call `applyColumnVisibility()` so column-toggle settings apply to new rows, and `requestAnimationFrame(syncScrollSizes)` to keep the sticky-header scroll bar in sync.
- **Files changed:**
  - `wwwroot/js/certificate-tile-view-internal.js` (`renderCertificates` list-view append branch)
  - `wwwroot/js/certificate-tile-view-external.js` (`renderCertificates` list-view append branch)
- **Verification needed (post-deploy):** Open table view on internal and external tile pages, scroll past page boundary, confirm row count grows and earlier rows persist. Confirm bulk-selection checkboxes on rows past the first page still toggle the bulk-actions bar.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-101 — Import/Export icons reversed across admin views (arrow direction violates UX convention)
- **Date:** 2026-04-26
- **Severity:** Low
- **Status:** Fixed — pending deploy
- **Reporter:** Shawn (live observation on `http://localhost:5101/Company/Index`)
- **Symptom:** Across most admin pages, the **Import** button used `fa-upload` (up arrow) and the **Export** button used `fa-download` (down arrow). Per UX convention (and Shawn's reference image): Import means data flowing INTO the system → arrow should point DOWN; Export means data flowing OUT → arrow should point UP. The mapping was inverted everywhere except Tags and User Management (which already used `fa-file-arrow-down`/`fa-file-arrow-up` correctly).
- **Root cause:** Inconsistent FontAwesome icon convention. The `fa-upload`/`fa-download` icon names describe a literal action (uploading-to-server / downloading-to-browser) — when applied to Import/Export labels, the arrow direction reads backwards because users associate Import with "inbound/down" and Export with "outbound/up" (matches CSV file-arrow icon convention).
- **Fix:** Swapped icons on all affected views so Import → `fa-download` (or `fa-file-arrow-down` where the page already used the file-arrow family) and Export → `fa-upload`. Modal headers for "Import Preview" updated to match. Buttons that label literal actions ("Upload Document", "Download Selected", "Download Template") were left alone — those names match their icon.
- **Files changed:**
  - `Views/Company/Index.cshtml` (Import + Export buttons + Import Preview modal header)
  - `Views/Company/Management.cshtml` (Import Preview modal header — buttons were already correct using `fa-file-arrow-*`)
  - `Views/Product/Management.cshtml` (Import + Export buttons + Import Preview modal header)
  - `Views/Product/ManagementV2.cshtml` (Import + Export All)
  - `Views/Product/ManagementAlt.cshtml` (Import + Export Selected + Export All)
  - `Views/Product/Categories.cshtml` (Import + Export)
  - `Views/Admin/ProductManagement.cshtml` (Import + Export buttons + Import Preview modal header)
  - `Views/Admin/CSFAttributes.cshtml` (Export to CSV)
  - `Views/Admin/CertificateTypes.cshtml` (Export)
  - `Views/Admin/Pathways.cshtml` (Export)
  - `Views/Admin/ProductTypes.cshtml` (Export)
  - `Views/Admin/CertificateFields.cshtml` (Export)
  - `Views/Admin/RoleManagement.cshtml` (Export)
  - `Views/Admin/Partners.cshtml` (Export)
  - `Views/Shared/_ProductIdManager.cshtml` (Import + Export buttons + Import Preview modal header)
- **Pages already correct (left untouched):** `Views/Admin/Tags.cshtml`, `Views/Admin/UserManagement.cshtml` (both use `fa-file-arrow-down`/`fa-file-arrow-up` consistently across buttons + modal headers).
- **Verification needed (post-deploy):** Visit Company/Index, Product/Management, Admin/* pages and confirm Import button shows down-arrow, Export button shows up-arrow. Open the Import Preview modal on Company/Index, Product/Management, Admin/ProductManagement, _ProductIdManager — header icon should be down-arrow.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-099 — SIT #163 follow-up: external cert listing filter panels still don't collapse on desktop
- **Date:** 2026-04-24
- **Severity:** Low
- **Status:** Fixed — pending deploy
- **Reporter:** Carly (SIT re-review of #163, 2026-04-22)
- **Symptom:** On the logged-in cert listing (external/partner tile view), clicking the chevron next to a filter name (e.g. "Certification Type") did not collapse the panel. Carly reported it also appeared broken on the public view and wondered if that was by design.
- **Root cause:** In `wwwroot/js/certificate-tile-view-external.js`, the filter-header click handler lives inside `attachFilterEventListeners()` and was missing the `toggleBound` guard. That function is invoked at three points (init, after data load, on rerender), so the click listener was bound 2–3 times per panel. Each click fired multiple toggles, and an even count canceled out — the panel visually never changed. Same bug previously fixed in the internal view under QC R168; the internal file has the guard, external did not. Public view is structurally different (filter-header handler is attached once during init, not inside `attachFilterEventListeners`) so public does in fact work — Carly's "public doesn't work on desktop" observation likely a stale browser cache.
- **Fix:** Added the same `toggleBound === '1'` guard used by internal view. Each filter-header element now has exactly one click listener regardless of how many times `attachFilterEventListeners()` is called.
- **Files changed:**
  - `wwwroot/js/certificate-tile-view-external.js` (around line 684, `attachFilterEventListeners`)
- **Relates to:** Original SIT #163 fix (which added `toggleFilter()` to internal + external) and DEF-098 (tablet duplicate Hide Filters bar). This closes the third and last tail of #163.
- **Deploy:** `GCCWebApp-test-2026-04-26-2312.zip` (bundled with DEF-100, DEF-101)

### DEF-102 — GS1 endpoint missing per-ASIN product name in `additionalTradeItemId` entries
- **Date:** 2026-04-29
- **Severity:** Low
- **Status:** Closed 2026-04-30. Deployed via `GCCWebApp-test-2026-04-29-2136.zip` (60.3 MB; staged 2026-04-29, deploy held one day by a Plesk/Cloudflare upload error). Verified live on 2026-04-30: `GET /api/v1/certificates/gs1/26-0146` returned both ASINs with per-identifier `name` populated (John Frieda PROfiller+ Thickening Conditioner / Dream Curls Conditioner 250ml — matches Carly's `ASIN Example for Amazon_4.17.26.xlsx`). List endpoint `GET /api/v1/certificates/gs1?pageSize=100` returned 12/12 ASIN entries carrying `name` across KAO, Amazon Devices, and Sonos certs. Standard endpoint shape unchanged. Receipts saved at `docs/tracking/api-samples/2026-04-30_amazon_gs1_single.json` and `docs/tracking/api-samples/2026-04-30_amazon_gs1.json`.
- **Reporter:** Carly (UAT review of GS1 response shape, 2026-04-28)
- **Symptom:** GS1 response (`/api/v1/certificates/gs1` and `/api/v1/certificates/gs1/{n}`) listed each ASIN as `{ "type": "ASIN", "value": "B0..." }` with no name attached. Standard Partner API response surfaces per-identifier `productName` under `productIds[]`, so the two partner-facing responses were inconsistent.
- **Root cause:** Not a build error — the GS1 endpoint was modeled to the GS1 Certification Data Model schema (https://gs1us-technology.github.io/certification-data-model), where `additionalTradeItemId` is a `{type, value}` pair and product naming lives at the Product level. The Standard Partner API has freedom to add per-identifier metadata; GS1 conformance does not. Per-ASIN names are already captured (`CertificateProductId.ProductName`) via both the cert-edit UI's identifier modal and the bulk-import CSV `ProductName` column.
- **Fix:** Extended the GS1 schema with an optional `name` property on `GS1AdditionalTradeItemId`. `MapToGS1Format()` now populates `Name = pi.ProductName` for each ASIN in the `additionalTradeItemId` list. Backward compatible — additive only, omitted when null. No data model, UI, or Standard API changes.
- **Files changed:**
  - `Models/Api/GS1CertificationReport.cs` (added `Name` property with `[JsonIgnore(WhenWritingNull)]`)
  - `Controllers/Api/v1/CertificatesController.cs:~1359` (`MapToGS1Format` projection — added `Name = pi.ProductName`)
  - `Controllers/Api/v1/CertificatesController.cs:~1137` (XML doc example updated to show new field shape; flows through to Swagger)
- **Partner-facing docs updated to mirror the new shape (2026-04-29):**
  - `docs2/exports/AMAZON_API_DEVELOPER_GUIDE.html` — §5.2 GS1 example + Appendix A added a `productIds[].productName` ↔ `additionalTradeItemId[].name` row
  - `docs2/exports/AMAZON_API_DATA_EXAMPLES.html` — both single-ASIN and multi-ASIN GS1 examples
  - `docs2/exports/AMAZON_API_QUICKSTART.html` — GS1 example
  - `docs2/api/AMAZON-API-INTEGRATION-GUIDE.html` — GS1 example (ASIN + SKU pair both carry name)
  - `docs2/api/AMAZON-API-QUICKSTART.html` and `.md` — GS1 example
  - `docs2/api/AMAZON-API-SCOPE.html` — GS1 example
  - `docs2/api/PARTNER-API-TESTING-GUIDE.md` — GS1 example
- **Caveat for Carly:** Only ASINs where the import CSV (or UI edit) had a `ProductName` filled in will return a name. Older imports that left it blank will return no `name` field on those ASIN entries until backfilled.
- **Build:** 0 errors, pre-existing warnings only.
- **Verification (post-deploy):** Hit `GET /api/v1/certificates/gs1/26-0146` with the partner API key and confirm each `additionalTradeItemId` entry includes `name` populated from `productIds[].productName`. Verify Standard endpoint shape unchanged.

### DEF-103 — Partner API by-number and by-id endpoints leak raw certificate entity (status, country, internal IDs)

- **Date:** 2026-04-30
- **Severity:** **High**
- **Status:** **Fixed locally — pending deploy.** Found during pre-UAT self-test (Section A run); fix verified locally same day.
- **Reporter:** Anuki internal QA pass
- **Symptom:** Partner API endpoints `/api/v1/certificates/number/{n}` and `/api/v1/certificates/{id}` return the raw `Certificate` entity instead of a partner-projected DTO. Concretely, when the Amazon Partner API key calls `GET /api/v1/certificates/number/26-0001`, the response includes:
  - `"status": "Client Approved - Paid"` — should be `"Active"` per Amazon contract §4 / §8 (DEF-097c)
  - `"country": "United States"` — should be `"All"` per Amazon contract (DEF-097)
  - Legacy flat fields `asin`, `upc`, `gtin`, `ean` at the top level — contract uses `productIds[]` array
  - Internal IDs leaking: `companyId`, `productId`, `pathwayId`, `productCategoryId`, `certificationTypeId`, `createdBy`, `updatedBy`, `id` (DB primary key)
  - Shape does NOT match the LIST endpoint shape returned for the same cert
- **Contract impact:** Amazon currently cannot reliably use the by-number or by-id endpoints. The list endpoint and GS1 endpoints map status correctly; only these two single-cert flat endpoints regressed. Memory note 2026-04-21 claimed "Fixed on all 6 Partner-facing endpoints (list, list-with-productids, search by product ID, GS1 list, GS1 single, by-id, by-number, PDF download)" — only some were actually fixed.
- **Root cause:** `Controllers/Api/v1/CertificatesController.cs:383` (in `GetCertificateByNumber`) and the parallel by-id action both end with `return SuccessResponse(certificate);` where `certificate` is the raw EF entity loaded via `_context.Certificates.Include(...).FirstOrDefaultAsync(...)`. There is no projection to a Partner DTO and no `isPartnerCaller` branch to remap `status → "Active"` or `country → "All"`. The role-check filter at line 380 only blocks non-public statuses; once a cert passes the filter, the raw entity is serialized.
- **Verification done locally (`localhost:5101`, dev DB `GCCDatabase_Dev`):**
  - `GET /api/v1/certificates` (LIST): returns `"status": "Active"`, `"certificationCountries": "All"` — CORRECT
  - `GET /api/v1/certificates/number/26-0001`: returns `"status": "Client Approved - Paid"`, `"country": "United States"` — INCORRECT
  - `GET /api/v1/certificates/2`: same INCORRECT shape as by-number
- **Fix applied:**
  1. Added shared `BuildPartnerCertificateDto(Certificate c)` private helper at the bottom of `Controllers/Api/v1/CertificatesController.cs`. Returns the contract shape: certificateNumber / status="Active" / issueDate / expirationDate / certificationCountries="All" / partner / certificationResults (percentage-formatted) / pathway / company / productName / certificationType / amazonCode / issuer / productIds[] / pdfDownloadUrl. No internal IDs.
  2. `GetCertificate` (by-id) and `GetCertificateByNumber` now branch on `isPartnerCaller = GetCurrentUserRole() == "Partner"`. Partner callers get the projected DTO; Admin/Analyst keep the raw entity (their existing behavior).
  3. Both endpoints now `Include(c => c.Pathway)` and `Include(c => c.ProductIds)` so the projection has the data it needs.
- **Verification (`localhost:5101`, dev DB):**
  - `GET /api/v1/certificates/number/26-0001`: now returns `status: "Active"`, `certificationCountries: "All"`, `productIds: []` array shape, no internal IDs. ✓
  - `GET /api/v1/certificates/2`: same shape as by-number. ✓
  - Regressions: LIST endpoint still returns 4 certs all `Active`/`All`. GS1 list/single still work. 404 still returned for Draft (`EXT-AMZ-DRAFT`) and legacy Active (`26-0146`).
- **Build:** 0 errors, 47 warnings (unchanged from baseline).
- **Pre-deploy check on prod:** likely had the same defect prior — partner integration would have been getting `status: "Client Approved - Paid"` and `country: "United States"` on these endpoints. Confirm with a call against `gccwebapp.com` once the fix deploys. If Amazon was relying on the by-number endpoint, they would have been ignoring the wrong status (or seeing inconsistent shape between list and single). Worth a heads-up email when this ships.
- **Logged from:** TC-DEF-097/097c partner-side execution, captured in `docs/tracking/test-execution-log.md`. **Found before client UAT — exactly what the self-test pass was for.**

### DEF-104 — Product ID per-row Status drifted from parent certificate (CR-015 build)

- **Date:** 2026-04-30
- **Severity:** Medium (data hygiene; Amazon badging via Partner API was already filtered at the cert level so no live leak — but underlying state was wrong).
- **Status:** **Fixed locally + dev DB backfilled + PROD DB backfilled 2026-05-01 — app deploy pending.**
- **QC mapping:** **QC #164 (4/10/26 — Carly, "Product Identifiers / Amazon Integration")** maps to this fix. Re-verified against current code on 2026-05-06: cascade wired into all 8 mutation paths, Status surface removed from list/filter/modal/import-template/JS-export, prod backfill confirmed 0-mismatch by post-flight audit. Test env (`gccwebapp.com`) is the same prod DB per `project_plesk_deploy_env.md`, so backfill coverage is implicit. No additional work required for QC #164.
- **Reporter:** Shawn (raised on the 2026-04-30 status conversation; client confirmed scope).
- **Symptom:** `CertificateProductId.Status` defaulted to `"Active"` on insert and was never auto-flipped when the parent cert moved out of `PublicStatuses` (Draft / Hold / Expired / Confidential / Not Paid / Republished->Expired / Expired - No Annual Verification). Analysts could also pick any per-row status from the UI / CSV, drifting from the cert's actual eligibility.
- **Why it matters:** Amazon badging integrity. The Partner API already filters cert lists to `PublicStatuses` (DEF-097 / 2026-04-21 fix), so live data going to Amazon was correct, but the underlying per-row state was stale and any future direct query / partner-side data hygiene check would surface the drift.
- **Fix scope (CR-015):**
  - `CertificateProductId.DeriveStatusFromCert(certStatus)` helper — single source of truth: `Active` iff cert is in `PublicStatuses`, else `Inactive`.
  - `IProductIdService.SyncProductIdStatusesForCertificateAsync(certId)` — cascade method; only updates rows where stored status drifts from derived (cheap no-op when in sync).
  - Cascade wired into 6 cert-status-change paths: `CertificateController.Edit` (POST), `CertificateController.Create` (POST), `CertificateController.ImportCertificates` (bulk POST), `CertificateService.PerformBulkActionAsync.update_status`, `CertificateService.UpdateCertificateAsync`, `CertificateWorkflowService.TransitionCertificateStatusAsync`, `CertificateExpirationService.ExpireOverdueCertificatesAsync`, `CertificateImportService.UpdateCertificateAsync` + `.CreateProductIdEntriesAsync`.
  - `ProductIdService.CreateProductIdAsync` / `UpdateProductIdAsync` overwrite incoming Status with the derived value (user input ignored).
  - Surface removal: Status column dropped from `_ProductIdManager.cshtml` table, filter dropdown, and Add/Edit modal; dropped from JS render / filter / form payload (`product-id-manager.js`); CSV import template, sample-CSV API endpoint, and in-memory export now omit the Status column. Status header in CSV import is silently ignored if present (back-compat for any saved templates).
- **Backfill:** `Scripts/database/CR015_backfill_product_id_status.sql` — idempotent. Ran against dev DB (`ANUKI01.GCCDatabase_Dev`) on 2026-04-30 — 12 product IDs flipped to Inactive (parent cert in legacy `Active` status, not in `PublicStatuses`); 0 needed flip to Active. Post-backfill audit query returned 0 mismatches. **Ran against PROD DB (`gccwebap_certificates` @ `68.71.129.120,1533`) on 2026-05-01 — 4 product IDs on Expired certs flipped to Inactive; 0 needed flip to Active; 17 rows on `Client Approved - Paid` certs were already correct. Post-flight audit returned 0 mismatches. Final prod state: 17 Active on Paid, 4 Inactive on Expired.**
- **Build:** 0 errors. `dotnet test` 143/144 (the 1 remaining failure is `CertificateWorkflowServiceTests.CheckAndUpdateExpiringCertificatesAsync_PendingRenewalExpired_TransitionsToExpired` — pre-existing dead-code failure on master, not introduced by this CR; verified via `git stash` baseline).
- **Verification (`localhost:5101`, dev DB):**
  - `/api/productids/sample-csv` returns `Type,Product ID,Partner,ProductName,ProductSku,Description` (Status header gone).
  - Product ID Manager modal on Create / Edit cert no longer shows the Status field.
  - List table no longer renders a Status column.
  - Status filter dropdown removed from filter row.
- **Partner API impact:** None. Contract is unchanged; this only cleans up underlying state. `feedback_amazon_api_immutable.md` rule preserved.
- **Pre-deploy on prod:** Run `CR015_backfill_product_id_status.sql` once after the app deploys. Counts the prod data ahead of time with the pre-flight query in the script header so we know the scale before flipping.

### DEF-104 — VOIDED 2026-04-30 (not a defect)

Originally logged as a regression of QC #31 (download icon position at desktop). On 2026-04-30 the user confirmed: **right-aligned is the intended behavior**, not centered. The 3/10 dev note that said "Changed to justify-content: center" was a mid-cycle fix that was later superseded; the 4/3 reopen ("buttons are not aligned right") meant *should be right-aligned*, and the current implementation correctly does that. My test card had the wrong expected outcome — TC-QC-031 PASSes at all viewports. No code change needed.

### DEF-106 — Partner API single-cert endpoints leak across partners (cross-tenant data exposure)

- **Date:** 2026-05-01
- **Severity:** **High** (security / partner data isolation)
- **Status:** **Fixed locally — pending deploy.** Found during TC-QC-143 (CR-008 Partner Role) execution; fix verified locally same day.
- **Reporter:** Anuki internal QA pass

**Symptom:** A Partner-role caller (API key OR Partner-role app user) could fetch certificates belonging to a DIFFERENT partner via the single-cert endpoints. Empirically confirmed on `localhost:5101` 2026-05-01:
- `GET /api/v1/certificates/number/EXT-NOVI-001` with the Amazon partner key → HTTP 200, returned the Novi cert in full (including `"partner": "Novi"` in the response).
- `GET /api/v1/certificates/42` (Novi cert id) with the Amazon partner key → HTTP 200, same leak.
- `GET /api/v1/certificates/gs1/EXT-NOVI-001` with the Amazon partner key → HTTP 200, GS1-formatted Novi cert exposed.
- `GET /api/v1/certificates/42/download` with the Amazon partner key → HTTP 404 in this case (the cert had no PDF), but the partner-scope check would have approved access if a file existed.

**Root cause:** The single-cert endpoints checked only role and `PublicStatuses`, NOT the caller's assigned partner. The LIST endpoint had been correctly applying `userPartnerNames.Contains(c.Partner)` since 2026-04-21; the same scoping was never applied to the four single-cert endpoints.

**Fix applied (`Controllers/Api/v1/CertificatesController.cs`):**
- `GetCertificate` (by-id, line ~340): added `var userPartnerNames = await GetCurrentUserPartnerNamesAsync(); if (!userPartnerNames.Any() || !userPartnerNames.Contains(certificate.Partner ?? "None")) return NotFoundResponse(...)` after the existing PublicStatuses check.
- `GetCertificateByNumber` (by-number, line ~390): same partner-scope check.
- `GetCertificateGS1` (gs1/{n}, line ~1352): same.
- `DownloadCertificate` ({id}/download, line ~1088): same.
- All four endpoints now: (1) check PublicStatuses for Partner, (2) check userPartnerNames.Contains(cert.Partner), (3) on either fail return generic 404 (no leak of "exists but you can't see it"). Admin/Analyst paths unchanged.

**Verification (`localhost:5101`, dev DB):**
- Positive: Amazon key → Amazon cert (by-number, by-id, GS1) all 200 ✓
- Negative: Amazon key → Novi cert (by-number, by-id, GS1, download) all 404 ✓
- Regression: Amazon LIST endpoint still returns 4 Amazon certs ✓
- Same fix protects Partner-role app users (cookie auth) — they share the same `GetCurrentUserPartnerNamesAsync()` and `GetCurrentUserRole() == "Partner"` paths.

**Pre-deploy implications for prod:**
- This is a real cross-partner data exposure that's been in prod since the partner-API endpoints were built. Severity is High because it's a partner-isolation breach, not a UI bug.
- Coordinate deploy with DEF-103 (which fixed the response shape) — both fixes touch the same four endpoints.
- Client should be informed before deploy. Consider whether to flag to the partner contact (Amazon side); the leak was inbound (Amazon could read others' data), not outbound (Amazon's data wasn't exposed to others — they're only in `c.Partner = 'Amazon'` rows).

**Found by:** TC-QC-143 (CR-008 Partner Role) execution in `docs/tracking/test-execution-log.md`.

### DEF-105 — CLOSED 2026-05-01 (Won't Fix — dev-only, not worth cycles)

Original framing: "prod has 12 duplicate cert types" — wrong, those were L2 subcategories (see `feedback_certtypes_hierarchy.md`). Re-scoped to "dev DB on pre-migration cert-type spec." Shawn confirmed 2026-05-01 he doesn't care about dev data hygiene; closing this entry. Prod is correct, no UAT impact.

(Original analysis preserved below for reference.)

- **Date:** 2026-04-30 (revised 2026-05-01)
- **Severity:** Low (dev environment only)
- **Status:** **Closed — Won't Fix**
- **Reporter:** Anuki internal QA pass

**Original draft of this defect claimed prod had 12 duplicate-name zombie rows. That was wrong.** Schema check showed `CertificationTypes` is self-referencing via `ParentId` with a `Level` column. The 12 "duplicates" I saw in prod are L2 subcategories under parent "Certified Sustainability Facts" (Id=6). They share Names with L1 top-level types, but they're referenced through a separate FK (`Certificates.CertificationTypeSubcategoryId`) and the L1 row carries the `AmazonCode`. By design, not a defect. Cleanup SQL has been removed from this entry — it would have wrongly deactivated valid hierarchy rows.

**Actual finding (dev only):**

The dev DB (`GCCDatabase_Dev` on `ANUKI01`) is on the pre-QC-#45 cert-types spec. The L1 top-level list (which is what the analyst Cert Create dropdown shows) has these wrong values:

| L1 cert type | Dev | Spec |
|---|---|---|
| Carbon Footprint Reduction | Inactive ✗ | Active |
| Sustainable Energy Practices | Inactive ✗ | Active |
| Sustainable Manufacturing Practices | Inactive ✗ | Active |
| Material Ingredient Reporting | MISSING ✗ | Active (renamed from MIV) |
| Product Optimization: Carbon Footprint Reduction | MISSING ✗ | Active |
| Renewable Resource Content | Active ✗ | Inactive |
| Biobased | Active ✗ | Inactive |
| Manufacturer Inventory Verification | Active ✗ | Should be renamed/gone |

Prod (`gccwebap_certificates`) has all of these correct. Migration `19_20260320_UpdateCertificationTypes.sql` ran cleanly on prod; never ran on dev.

**Hierarchy structure (for reference):**
- Dev: 21 L1 (16 active, 5 inactive) + 41 L2 (31 active)
- Prod: 18 L1 (all active) + 14 L2 (all active, mostly under Id=6 "Certified Sustainability Facts")
- The two environments have different L2 trees — that's likely fine, but note for future comparisons.

**Action:** re-seed or migrate the dev DB so future Section B testing isn't tripped by stale cert-type data. Low priority; doesn't block client UAT (prod is already correct). If we re-seed: also dedupe the L1 `Sustainable Energy Practices` row in dev (currently exists twice, both Inactive — appears to be a one-off seed glitch).

**Lessons learned for future DB inspection:**
- `CertificationTypes` is hierarchical via `ParentId` + `Level`. Always include those columns when comparing.
- Same Name at different Levels is by design, not a duplicate.
- See `feedback_certtypes_hierarchy.md`.

**Found by:** TC-QC-045 in `docs/tracking/test-execution-log.md`. Originally flagged with wrong "prod duplicates" framing; corrected after schema check.

---

### DEF-109 — Product ID "Created By" column shows "System" instead of the user who added it

- **Date:** 2026-05-05
- **Severity:** Medium (audit data correctness; visible to client on Cert Edit page)
- **Status:** Fixed (pending UAT)
- **Reporter:** Shawn (continuing QC #80 testing)

**Symptom:** On the Certificate Edit page → Product ID Manager table, the "Created By" column shows "System" for product IDs added through the certificate Create or Edit form, even when a logged-in analyst added them.

**Root cause:** `CertificateController.Create` (POST) and `CertificateController.Edit` (POST) accept the `model.ProductIds` collection from the form and write each row directly to the DB without setting `CreatedBy`. The JS payload from `ProductIdManager.getProductIdsForSubmission()` only carries Type/Value/Partner/Name/SKU/Description — no audit fields — so `CreatedBy` defaults to `null`. On re-fetch, the `CreatedByUser` navigation comes back null, and `product-id-manager.js` line 244–245 falls back to the literal string `'System'`.

This is distinct from `/api/productids` POST (the modal flow on Cert Edit *outside* the form save), which already sets `CreatedBy` correctly via `ProductIdApiController.CreateProductId`. The bug only fires on the Create/Edit page form-save path.

**Fix:** `Controllers/CertificateController.cs`
- Create flow (around line 481): set `CreatedBy = GetCurrentUserId()`, `CreatedAt = now`, `UpdatedAt = now` on each `productId` before adding to the certificate's collection.
- Edit flow (around line 868): same, plus `UpdatedBy = currentUserId`. Note: the Edit flow's `certificate.ProductIds.Clear()` + re-add pattern means existing rows are deleted and re-inserted, so all rows get the new audit stamp on edit — consistent with how `Tags` and `CertificationTypeSubcategories` are handled in the same method.

**Side effect / known limitation:** Existing rows already in DB with `CreatedBy = NULL` will continue to render as "System" until they are touched by a save. A backfill SQL update (`UPDATE CertificateProductIds SET CreatedBy = (SELECT CreatedBy FROM Certificates c WHERE c.Id = CertificateId) WHERE CreatedBy IS NULL`) would clear the historical rows but is a data change — per the project rule (`feedback_data_changes_require_cr.md`), that needs a formal CR before running on prod, so it is NOT included here.

**Test plan:**
1. Log in as `analyst` on `localhost:5101`.
2. Navigate to `/Certificate/Create`. Add a product ID (any type/value) via the modal. Save the certificate.
3. Open the saved certificate via `/Certificate/Edit/{id}`. Confirm the Product ID Manager row shows `Created By: analyst` (not `System`).
4. Add another product ID directly on the Edit page. Save. Reload. Confirm new row also shows `Created By: analyst`.
5. Cross-role check: log in as `admin`, add a product ID, confirm row shows `Created By: admin`.
6. Negative: existing pre-fix product IDs (created before this commit) should still render `System` — confirms the limitation note.

**Found by:** Continuing TC-QC-080 manual UAT. Spotted by Shawn; not in original 4/27 client QC log.

---

### DEF-110 — Admin `/api/certificates` POST/PUT path silently dropped CreatedBy / UpdatedBy

- **Date:** 2026-05-05
- **Severity:** Low (Admin-only API, no client UAT impact, but identical audit-data-loss pattern as DEF-109)
- **Status:** Fixed (pending UAT)
- **Reporter:** Anuki internal QA pass — DEF-109 follow-up sweep

**Symptom:** Certificates created or updated through `POST /api/certificates` and `PUT /api/certificates/{id}` were saved with `CreatedBy = NULL` (and updates with `UpdatedBy = NULL`). The Cert Internal View hides the "Created By" line entirely when `CreatedByUser` is null (`CertificateInternalView.cshtml:187` guards the block), so no visible "System" string appears here — but the audit data is still missing.

**Root cause:** `CertificatesApiController.MapToCertificate` builds a `Certificate` from the request DTO without setting CreatedBy. The controller then routes through `CertificateService.CreateCertificateAsync` / `UpdateCertificateAsync`, neither of which had any way to know the calling user — the methods only accepted a `userRole` string. The wiring lost the user identity between `User.FindFirst(NameIdentifier)` (available on the controller) and the entity (saved by the service).

**Fix:**
- `Services/IServices.cs` — added optional `int? userId = null` parameter to `CreateCertificateAsync` and `UpdateCertificateAsync`.
- `Services/CertificateService.cs` — both methods now stamp `CreatedBy` (and `UpdatedBy` on update) from the new param.
- `Controllers/Api/CertificatesApiController.cs` — added a `GetCurrentUserId()` helper (parses `ClaimTypes.NameIdentifier`) and passes the result into both service calls.

The optional parameter default keeps any future caller that doesn't supply userId from breaking. The web `/Certificate/Create` and `/Certificate/Edit` paths don't use these service methods (they hit the repository directly with their own user-stamp logic), so they are unaffected.

**Test plan:**
1. Log in as `admin`. Use Postman or `curl` against `https://localhost:5101/api/certificates` with `Content-Type: application/json` and a valid CreateCertificateRequest body. Submit POST.
2. Open the new cert via `/Certificate/CertificateInternalView/{id}`. Confirm the "Created By: <admin email>" line appears — previously this row was silently hidden.
3. PUT to `/api/certificates/{id}` with an UpdateCertificateRequest body. Reload the cert detail. Confirm UpdatedBy was set (DB inspection or future audit-trail UI).

**Found by:** Code-survey sweep after DEF-109 fix. Shawn asked "do we have this issue elsewhere in the views?" — this surfaced as the only remaining write path that produced null audit fields.

---

### DEF-111 — Product ID import-template column order didn't match the on-screen Product ID Manager table

- **Date:** 2026-05-06
- **Severity:** Low (cosmetic/UX consistency, no data loss)
- **Status:** Fixed (pending UAT)
- **Reporter:** Shawn — spotted on `/Certificate/Edit/26-0106`

**Symptom:** Downloaded import template (`product-ids-sample.csv`) had columns in the order `Type, Product ID, Partner, ProductName, ProductSku, Description`. The on-screen Product ID Manager table and the in-memory export both use `Type, Product ID, Product Name, SKU, Partner, ...`. Users editing the template in Excel saw the columns in a different order than what they were used to in the UI, which made bulk-edit error-prone.

**Root cause:** `wwwroot/js/product-id-manager.js:1283` had a hard-coded template string that predated the table redesign and was never re-aligned. The in-memory export at line 1253 was updated; the template was missed.

**Fix:** `wwwroot/js/product-id-manager.js`
- `downloadSample()` template header now: `Type,Product ID,Product Name,SKU,Partner,Description` (matches table + export order).
- `parseAndPreviewImport()` productName lookup is now tolerant of both `productname` (legacy template, no space) and `product name` (new template, with space). Older templates users may already have on disk continue to parse correctly.

**Test plan:**
1. Open any cert with Product IDs at `/Certificate/Edit/{id}`. Click **Template** in the Product ID Manager toolbar.
2. Open the downloaded CSV. Confirm header order: `Type, Product ID, Product Name, SKU, Partner, Description` — the same order as the table on the page.
3. Add a row matching the new order; click **Import**, upload. Confirm the preview shows the row correctly mapped (Product Name and SKU not swapped with Partner).
4. Take a previously-downloaded older template (`Type, Product ID, Partner, ProductName, ProductSku, Description`), populate one row, import. Confirm it still parses correctly — backward compatibility verified.
5. Bulk test: import `C:\Users\swest\Downloads\product-ids-sample-1000-asins.csv` (1000 ASINs generated 2026-05-06) against an empty cert. Confirm all 1000 rows preview as **New**, no errors.

**Found by:** Shawn during cert-edit walkthrough. Filed alongside CR-016 prep work — the column-order mismatch is a defect; the broader UX redesign of the import-preview modal is the CR.

---

### DEF-112 — Bulk Product ID import capped at 100 rows due to per-row HTTP requests hitting AspNetCoreRateLimit

- **Date:** 2026-05-06
- **Severity:** High (blocks the use case CR-016 was scoped around — bulk-import of partner-onboarding ASIN lists)
- **Status:** Fixed (pending UAT — requires dev-server restart to load the C# changes)
- **Reporter:** Shawn — `localhost:5101/Certificate/Edit/26-0106` import of 1000-row test CSV. First 100 rows imported, next 900 returned `"Unknown error"`.

**Symptom:** Importing >100 product IDs into a certificate failed every row past the 100th. UI reported `Imported 100 product ID(s). 900 failed: Row 101 ... Unknown error; Row 102 ...`. Exact 100/900 split was the giveaway — that's a per-minute rate-limit cap being hit.

**Root cause:** Two compounding bugs.

1. **Frontend was firing one HTTP request per row.** `wwwroot/js/product-id-manager.js#applyImport()` looped over every selected row and did a separate `fetch('/api/productids', POST)` for each. For 1000 rows that's 1000 separate API calls.
2. **AspNetCoreRateLimit caps `*/api/*` at 100 req/min per IP** (configured in `appsettings.json:96-99`). Request 101 onward returned 429. The fetch wrapper read `await response.text()` for the body but the actual error string came back empty for 429s, so the user-facing toast said `"Unknown error"` instead of `"429 Too Many Requests"`.

**Fix:** Three coordinated changes.

1. **New JSON-body bulk endpoint.** `Controllers/Api/ProductIdApiController.cs` adds `POST /api/productids/certificate/{certificateId}/bulk` accepting `BulkProductIdImportRequest` (`{ items: ProductIdImportItem[], overwriteExisting: bool }`). Replaces 1000 individual POSTs with one request — dodges the rate limit by design.
2. **Frontend rewritten to use bulk.** `wwwroot/js/product-id-manager.js#applyImport()` now splits selected rows into `creates` and `updates`. Creates go in one bulk POST; modifications still PUT individually (small count, no rate-limit risk). The replace-all toggle now passes `overwriteExisting: true` to the bulk endpoint instead of looping individual DELETEs from the browser.
3. **Service-layer pre-fetch optimization.** `Services/ProductIdService.cs#ImportProductIdsAsync` no longer calls `CreateProductIdAsync` per row (which itself fired 3 DB queries + a SaveChanges). Now: pre-fetches existing rows, parent cert status, and max display order in 3 queries total; does duplicate detection against an in-memory dictionary; queues all entities; commits a single batched SaveChanges. Improvement for 1000-row import: ~4000 DB roundtrips → ~3 + 1 batched insert. Should complete in 1-2 seconds instead of timing out.

**Header tolerance (bonus, related to DEF-111):** The file-based `/api/productids/certificate/{id}/import` endpoint now accepts both legacy template headers (`Value`, `ProductName`, `ProductSku`) and new-template headers (`Product ID`, `Product Name`, `SKU`). Backend `DownloadSampleCsv` aligned to the new column order.

**Files touched:**
- `Controllers/Api/ProductIdApiController.cs` — added bulk endpoint and `BulkProductIdImportRequest` DTO; updated DownloadSampleCsv header.
- `Services/ProductIdService.cs` — rewrote `ImportProductIdsAsync` to pre-fetch + batch insert; made `ImportFromCsvAsync` header lookup tolerant.
- `wwwroot/js/product-id-manager.js` — rewrote `applyImport()` to use the bulk endpoint.

**Test plan:**
1. Stop the dev server (it's holding `bin/Debug/net8.0/GCCWebApp.exe` — current PID 37876). Run `dotnet build` then `dotnet run --urls "http://localhost:5101"`.
2. Navigate to `/Certificate/Edit/26-0106`. Hard-refresh (Ctrl+F5) so the browser picks up the new JS.
3. Click **Import** in the Product ID Manager toolbar; pick `C:\Users\swest\Downloads\product-ids-sample-1000-asins.csv`.
4. Preview should show `1000 New, 0 Modified, 0 Errors`.
5. Click **Apply Import**. Expect: success toast `Imported 1000 product ID(s)` (no failures, no rate-limit error). Table reloads showing 1000 rows.
6. Inspect browser DevTools Network tab — only **one** request to `/api/productids/certificate/26-0106/bulk`, not 1000 to `/api/productids`.
7. Inspect server log — should show `Bulk import for certificate {id}: 1000 succeeded, 0 failed`.
8. Edge case: re-import the same file. Default mode (Add) should preview all 1000 as duplicates and report 1000 errors `already exists on this certificate`. Replace mode should silently keep them (no errors).

**Found by:** Shawn during DEF-111 retest with the 1000-ASIN test file generated 2026-05-06.

---

### DEF-113 — Same per-row HTTP-POST anti-pattern across 4 admin imports (Companies, Tags, Products, Users)

- **Date:** 2026-05-06
- **Severity:** High (blocks bulk-onboarding workflows on every admin import surface in the app, identical root cause to DEF-112)
- **Status:** Fixed (pending UAT — requires dev-server restart to load the C# changes)
- **Reporter:** Shawn — proactive sweep after DEF-112 fix

**Symptom:** After fixing DEF-112 on the Product ID import, an inventory of the rest of the app surfaced four more import flows using the same broken pattern: one HTTP POST per row in a `for await` loop. Affected surfaces:

| Surface | URL | Old endpoint per row | Frontend |
|---|---|---|---|
| Companies | `/Company/Management` | `POST /Company/SaveCompany` | `companyManagement.js#applySelectedImport` |
| Tags | `/Admin/Tags` | `POST /Admin/CreateTag` | `tags.js#applySelectedImport` |
| Products | `/Admin/ProductManagement` | `POST /Admin/CreateProduct` | `productManagement.js#applySelectedImport` |
| Users | `/Admin/UserManagement` | `POST /Admin/CreateUser` | `userManagement.js#importCSV` |

In dev, all four were exposed to the localhost IP rate cap of 200/min (`appsettings.json:112-124`), so any import >200 rows would fail rows past 201 with empty `"Unknown error"` strings (same as DEF-112's diagnostic). Per-row pattern was also slow (~10s per 100 rows) regardless of rate-limit hits.

**Root cause:** The frontend pattern of `for (const row of rows) { await fetch('/...', {body: row}) }`. Identical to DEF-112.

**Fix (universal pattern):** For each surface, three coordinated changes:
1. JSON-body bulk endpoint that accepts pre-parsed rows + replace flag (where applicable).
2. Service-layer optimization: pre-fetch existing rows + reference data once, in-memory dup detection, batched `SaveChangesAsync`. Dropped from N×4 DB roundtrips to ~3 + 1 batched insert.
3. Frontend `applyImport()` rewrite: collapse N requests into 1 bulk POST for creates; per-row PUT for the small "modified" set.

**Per-surface specifics:**
- **Companies** — new `POST /Company/BulkImport`, dup key `Name` (case-insensitive). Replace-all supported via `overwriteExisting` flag. Single bulk audit entry. Files: `Services/ICompanyService.cs`, `Services/CompanyService.cs`, `Controllers/CompanyController.cs`, `wwwroot/js/admin/companyManagement.js`.
- **Tags** — new `POST /Admin/BulkImportTags`, dup key `Name + TagType` (case-insensitive — Internal/External tags are independent namespaces). Tightens contract: existing `CreateTagAsync` had no dup check. TagType normalized to lowercase to match prod data. Files: `Services/IAdminService.cs`, `Services/AdminService.cs`, `Controllers/AdminController.cs`, `wwwroot/js/tags.js`.
- **Products** — new `POST /Admin/BulkImportProducts`, dup key `Name + CompanyId`. Pre-fetches Companies + ProductTypes as dictionaries for FK resolution. Replace-all NOT supported (mass-deactivation risk on typo). Bonus fix: the previous JS payload dropped `companyName` and `productTypeName` entirely — products imported as orphans. New JS sends them. Files: `Services/IAdminService.cs`, `Services/AdminService.cs`, `Controllers/AdminController.cs`, `wwwroot/js/admin/productManagement.js`.
- **Users** — new `POST /Admin/BulkImportUsers`, dup key `Email` (case-insensitive). Pre-fetches Users + Companies + Roles. BCrypt password hashing kept inline (CPU-bound, ~50ms/row). Replace-all NOT supported (locked-out-admin risk). New `BulkImportUserItem` DTO uses string-based references (`CompanyNames: List<string>`) which the service resolves to FK IDs in memory. Bonus fix: `userManagement.js#importCSV` was parsing a 9-column legacy format that no longer matched the 7-column template — every column was silently mis-mapped. Aligned to template format. Files: `Services/IAdminService.cs`, `Services/AdminService.cs`, `Models/AdminModels.cs`, `Controllers/AdminController.cs`, `wwwroot/js/admin/userManagement.js`.

**Tracking decision:** all 4 surfaces logged under one DEF-113 since they share root cause + fix pattern.

**Test plan:**
1. Stop the dev server; `dotnet build` then `dotnet run --urls "http://localhost:5101"`.
2. For each test file in `C:\Users\swest\Downloads\`:
   - `test-companies-250.csv` → `/Company/Management` → Import. Expect 250 created, 0 errors. One request to `/Company/BulkImport` in DevTools Network tab.
   - `test-tags-250.csv` → `/Admin/Tags` → Import. Expect 250 created, 0 errors. One request to `/Admin/BulkImportTags`.
   - `test-products-250.csv` → `/Admin/ProductManagement` → Import. Pre-req: `Amazon` company and `Personal Care` product type must exist; otherwise rows fail validation cleanly with name-resolution errors.
   - `test-users-250.csv` → `/Admin/UserManagement` → Import. BCrypt hashing 250 passwords takes ~12s server-side. One request to `/Admin/BulkImportUsers`.
3. Re-run any of the above. Expect all 250 to fail with "already exists" — confirms within-batch and cross-batch dup detection works.

**Found by:** Shawn — "let identify them and we need to test all of them" — proactive request after DEF-112 fix.

---

### DEF-114 — Main nav bar misaligned with page content (flush left edge instead of inside `.page-container`)

- **Date:** 2026-05-07
- **Severity:** Medium (cosmetic, but global — affects every authenticated page across all roles)
- **Status:** Fixed
- **Reporter:** Shawn — Cert single view screenshot, `https://gccwebapp.com/Certificate/CertificateSingleView/26-0157`

**Symptom:** The main navigation bar (`Certificates`, `Products`, etc.) renders flush against the left viewport edge instead of aligning with the `<h1 class="page-title">` and the rest of `.page-container` content. Visible on every page that uses `_Layout.cshtml`.

**Root cause:** `Views/Shared/_Layout.cshtml:155` was missing the closing `>` on the `<div class="nav-wrapper …">` opening tag:

```cshtml
<div class="nav-wrapper @(User.Identity?.IsAuthenticated != true ? "nav-anon" : "")"
    <nav class="main-nav" aria-label="Main navigation">
```

Browser parsers treat `<nav`, `class="main-nav"`, `aria-label="Main navigation"` as junk attributes on the div until they hit the first `>`, then drop the `<nav>` entirely. The `.nav-links <ul>` ends up directly inside `.nav-wrapper`, which has no `max-width` or horizontal padding — so links extend to viewport edge while `.page-container` (max-width 1800px, padding 0 40px) and `.main-nav` (same) sit further inset.

**Fix:** Closed the `nav-wrapper` div tag. One-character change. With the `<nav class="main-nav">` element actually rendering, its `max-width: 1800px; padding: 0 40px` matches `.page-container` exactly, so the nav links now align with `.page-title` on every page.

**File:** `Views/Shared/_Layout.cshtml:155`.

**Test plan:**
1. Hard-refresh `/Certificate/CertificateSingleView/26-0157` while signed in as `external` (or any authenticated role).
2. Confirm `Certificates` link aligns horizontally with the `Certificate Details` h1.
3. Spot-check a page in each role: `/Certificate/CertificateTileViewExternal` (External), `/Certificate/CertificateTileViewInternal` (Analyst/Admin), `/Admin/UserManagement` (Admin).
4. View source — confirm `<nav class="main-nav">` opens correctly and is a child of `.nav-wrapper`.

**Found by:** Shawn — single view screenshot.

---

## Closed / Historical

*(Entries from the pre-existing register to be merged here if recovered.)*

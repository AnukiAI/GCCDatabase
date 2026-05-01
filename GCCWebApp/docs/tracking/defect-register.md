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

### DEF-111 — `/Product/Management` "Error loading products: SyntaxError: Unexpected token '<'" — fetch parses HTML auth-redirect as JSON

- **Date:** 2026-05-01
- **Severity:** Medium (silent functional failure; the user sees an empty/broken product list with no actionable error)
- **Status:** **Open** — surfaced during DEF-108 verification sweep, not fixed in this cycle
- **Reporter:** Anuki internal full-spectrum QA pass (2026-05-01)
- **Symptom:** Logged in as analyst, navigating to `/Product/Management` produces this console error: `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON`. The page renders but the products table is empty / not populated. No visible error toast or banner — the user has no idea anything went wrong.
- **Suspected root cause:** An AJAX fetch on the Product Management page is hitting an API endpoint that returns HTML (almost certainly the login page or AccessDenied page after a session check) and the JS is calling `response.json()` without first checking `response.ok` or `Content-Type`. Two possible upstream causes:
  1. The endpoint requires Admin role and the analyst silently 403s/302s to a login page.
  2. There's a session/CSRF-token mismatch and auth middleware redirects the AJAX call to login.
- **Why it matters:** Real defect for analysts trying to view products (one of the top-3 critical workflows). User sees "page is broken" with no feedback. Silent failure also masks data-integrity issues if downstream code assumes the product list loaded.
- **Repro:** Login as `analyst` / `TestPass123!`, navigate to `http://localhost:5101/Product/Management`, open DevTools console.
- **Investigation needed (NOT done in this cycle):**
  1. Find the failing fetch URL via Network tab.
  2. Check whether endpoint requires `[Authorize(Roles="Admin")]` or similar — if so, hide the page from analyst nav OR add Admin/Analyst to the policy.
  3. Add `response.ok` / content-type guard to the JS so failures surface visibly via `AdminCore.showNotification('error', ...)` instead of console.
- **Files (pending investigation):** likely `Views/Product/Management.cshtml` JS block + whatever controller endpoint it calls.
- **Found by:** `fullspectrum_out/console-errors-rollup.json` (2026-05-01 verification run for DEF-107/108 fix).
- **Deploy:** Open

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

## Closed / Historical

*(Entries from the pre-existing register to be merged here if recovered.)*

# Shipped — April 2026

Snapshot of delivery for the month of April 2026. Organized by theme so it reads like a stakeholder update, not a git log.

Period: 2026-04-01 → 2026-04-24
Deploys to gccwebapp.com: multiple (see Deployments section)
Commits on master: 24

---

## Amazon Partner API — contract compliance

The Amazon integration work. All items confirmed against the live endpoint before ship.

- **`certificationCountries` now always returns `"All"`** (DEF-097). Partner API projections used to pass the internal `Country` field through, so Amazon was seeing whatever analysts had typed ("Germany", "United States", or `null`). Now hardcoded to `"All"` for partner callers on all six partner-facing endpoints (standard list, search, by-id, by-number, GS1 list, GS1 single). Internal Admin/Analyst views still see the raw `Country` value.
- **Partner API now filters out non-public lifecycle statuses.** Draft, Hold, and Expired certs were leaking into Amazon responses because the query didn't filter by `PublicStatuses`. Fixed.
- **Partner API status field hardcoded to `"Active"`** per contract §4 and §8. Internal lifecycle statuses ("Client Approved — Paid", "Draft", etc.) were leaking through the Partner projection. Now mapped to the contract literal.
- **`CertificationResults` now formats as percentage** for Amazon output. Free-text field kept as-is internally; display formatter applied at six output surfaces (internal detail view, internal tile API, Partner v1 list/search/single/GS1, product-ID search).
- **Sample JSON files refreshed** for the 6 active Amazon certs: `docs/tracking/api-samples/2026-04-24_amazon_standard.json` and `2026-04-24_amazon_gs1.json`.
- **PDF test sample** `docs/tracking/api-samples/2026-04-24_certificate_26-0147.pdf` — captured from live API call for Carly's reference.

## Amazon Developer Guide (client-facing documentation)

- 10 fixes applied to HTML source from Carly's CP2 review (28 comments total, 18 already resolved prior).
- Added Appendix A (GS1 governance).
- `productSku` clarification: note that SKU is not always provided; ASIN is the minimum.
- `type` field clarification: only `ASIN` is referenced for Amazon; UPC/GTIN/EAN/ISBN/MPN removed from Amazon-facing docs.
- Updated production URL reference from subdomain to path-based (`https://greencirclecertified.com/database`).

## DEF-100 — Tile list/table view: lazy-load no longer wipes earlier pages (2026-04-26)

On both the analyst (internal) and partner (external) certificate tile pages, switching to **list/table view** and scrolling to load the next page replaced the previously rendered rows with only the new page — so users only ever saw "the remainder, not the whole of the dataset". Tile/card view was unaffected (it correctly used `insertAdjacentHTML('beforeend', …)`); the table-view code path mistakenly called a full-render helper (`renderTableRows`) that does `gridBody.innerHTML = …`, so passing only the new page wiped every previously rendered row. Fix: the list-view append branch now builds HTML for new rows only and appends them directly to `#gridBody`. Also re-attaches `change` listeners on the new `.row-checkbox` elements so bulk-selection on later pages still drives the bulk-actions bar, re-applies column-toggle visibility so newly-loaded rows respect the user's hidden-columns settings, and re-syncs the sticky-header scroll bar via `requestAnimationFrame(syncScrollSizes)`. Files: `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`. Public tile view (`certificate-tile-view.js`) was already structurally correct (it re-renders the full list). Build: 0 errors, 0 warnings. Logged as DEF-100; verification post-deploy is to scroll past the page boundary on table view and confirm row count grows monotonically.

## DEF-112 — Stop auto-generating placeholder PDFs (2026-04-26)

When a certificate had no uploaded PDF, hitting Download returned a freshly-generated placeholder PDF instead of failing — misleading analysts and partners (it implied a real cert exists when it doesn't). Three download paths fell through to `_pdfService.GenerateCertificatePdfAsync()`: `/Certificate/Download/{n}` (the route every UI download button hits), `/api/certificate-tiles/{id}/download` (always generated, never even checked for a file), and `Services/CertificateService.GetCertificateDownloadAsync` (called by `/api/certificates/{id}/download`). All three now return HTTP 404 when `Certificate.CertificateFile` is null/empty or the file is missing on disk. Partner API `/api/v1/certificates/{id}/download` was already correct (returns 404 with a TODO comment) — left untouched per the immutable-Partner-API rule. Verified: cert WITH file returns 200 + 2.4MB PDF; cert WITHOUT file returns 404. Follow-up flagged: list-view download buttons are still clickable for fileless certs (will open a 404 in a new tab), since neither the public nor internal list API surfaces a `hasPdf` indicator yet.

## DEF-101 — Import/Export icon arrow direction unified across all admin views (2026-04-26)

Live observation by Shawn on `/Company/Index`: the **Import** button used an UP arrow (`fa-upload`) and the **Export** button used a DOWN arrow (`fa-download`) — backwards by every UX convention (Import = inbound = down, Export = outbound = up). Audit found the same inversion on most admin pages: Company/Index, Company/Management (modal header only — buttons were fine), Product/Management, Product/ManagementV2, Product/ManagementAlt, Product/Categories, Admin/ProductManagement, Admin/CSFAttributes, Admin/CertificateTypes, Admin/Pathways, Admin/ProductTypes, Admin/CertificateFields, Admin/RoleManagement, Admin/Partners, Shared/_ProductIdManager partial. Tags and User Management already used the correct `fa-file-arrow-down`/`fa-file-arrow-up` family; left untouched. Import Preview modal headers (Company/Index, Company/Management, Product/Management, Admin/ProductManagement, _ProductIdManager) updated to match. Literal-action buttons ("Upload Document", "Download Selected", "Download Template") left alone — those names match their icons. Files: 15 .cshtml views. Logged as DEF-101.

## CRs delivered

- **CR-005 — Public view Hide/Show Filters parity** (2026-04-24). The internal (analyst) and external (logged-in partner) certificate listings already had a desktop "Hide Filters" / "Show Filters" sidebar collapse control; the public listing was missing it. Identified during SIT screenshot review. Added matching markup + JS to `Views/Certificate/CertificateTileView.cshtml` and `wwwroot/js/certificate-tile-view.js`. All 3 cert listing views now behave the same way at desktop. Validated locally via Playwright (collapse/expand both directions, edge tab visibility).
- **CR-007 — Admin-managed Custom Fields appended as columns on the Internal cert table view** (2026-04-24, refined 2026-04-25). The Internal listing's List/Table mode (analyst/admin) had 24 hardcoded columns; admin-managed Custom Fields (created via `/Admin/CertificateFields`) showed up on the cert detail page but never made it to the table. API now returns each cert's custom-field values plus the field definitions, frontend dynamically appends sticky header cells + column-toggle entries under a single "Custom Fields" divider (sorted by `SortOrder`), and table rows render a cell per field at the end. Defaults to OFF in the column toggle (so 30+ fields don't blow up the table on first visit) using a `gcc-cert-cf-seen-internal` localStorage marker — once the analyst toggles a field on, their preference persists. Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/css/pages/certificate-tile-internal.css`. Out of scope (separate CRs if Carly wants them): `/Certificate/ExportCertificates` Excel/CSV server-side endpoint, sorting/filtering by custom-field columns, **Partner API custom-field exposure (deferred — `/api/v1/certificates*` contract is immutable; any partner-facing change would require Carly→Kyle sign-off).** Validated locally via Playwright at 5 viewports.

  Same-day readability fix on the underlying table: Switched cell rendering from `nowrap`+ellipsis to wrap (`white-space: normal`, `min-height: 36px`), so long values (Cert Type "Certified Environmental Facts", Product Name "Dial Complete Antibacterial Deodorant Bar Soap, ...", Status "Client Approved - Paid") now display in full instead of being clipped to "Certified Environmental Fa", "Dial Complete Antibacteric", "CLIENT APPR". Status badge made wrappable too. Actions column widened 90→110px (3 icons + breathing room) and `align-self: stretch` keeps icons centered when neighboring cells wrap and the row grows taller. CSS only: `wwwroot/css/certificatetileview.css`, `wwwroot/css/pages/certificate-tile-internal.css`.
- **CR-006 — Em dash placeholder on certificate detail views** (2026-04-24, extended 2026-04-25). Client wanted "Data not available" replaced with an em dash (—) when a field on the cert detail page is empty. Swapped the visible text in 18 places across the three cert detail views (public `CertificateSingleView`, partner `CertificateExternalView`, analyst/admin `CertificateInternalView`) — Product Category, Product Type, Product Subcategory, Location, Company Email, Company URL. Kept the `<span class="no-data">` wrapper so the existing dimmed-text styling still applies. Out of scope: tile-card listings, PDF export, partner API, server logs (left as-is — flagged for separate confirmation if Carly wants those swept too).

  Same-day extension (2026-04-25): swept the OTHER missing-data fallbacks on the same 3 cert detail views that CR-006 originally missed — `?? "Unknown Company"`, `?? "Unknown Product"`, `?? "Unknown Location"`, `?? "Unknown Country"`, `?? "Unknown"` (dates, status, created/updated), and `return "N/A"` (certificate number formatter). All swapped to `"—"`. About 18 additional instances. Now every empty field on a cert detail page renders consistently as an em dash regardless of which fallback path produced it.

## UI / UX defects

- **Tablet view** on the logged-in certificate listing no longer shows two stacked "Hide Filters" bars (DEF-098). One CSS rule; visible fix.
- **External cert listing filter panels collapse correctly on desktop** (DEF-099, SIT #163 tail). The click handler inside `attachFilterEventListeners()` was missing the dedup guard that the internal view already had, so it bound 2–3 times and even-numbered toggles canceled out. Single-line guard; behavior now matches internal and public. (Carly's secondary comment that public also doesn't collapse on desktop looks like a stale browser cache — public is structurally different and does work.)
- **Date off-by-one bug fixed** (QC #23). Dates entered as 4/4 were displaying as 4/3 due to timezone coercion.
- **Certificate detail/tile CSS layout fixes** — height, logo, spacing.
- **Import/Export iconography unified** across the entire app: `fa-file-import` and `fa-file-export` consistently.
- **User management fixes** (create user error reporting, template sample data removed, import working end-to-end — QC #106–#109 batch).
- **Product Types admin batch** (QC #115, #116, #117): blue-box description accuracy, help modal examples, default-to-wrong-category bug.
- **Product Management batch** (QC #88–#95): sort arrows, text wrapping, pagination (50/page), Product Type + Subcategory columns, Add/Edit modal fields, cascading dropdowns, export columns, sorting fixes.
- **Breadcrumbs batch** (QC #104): 19 views fixed — dead `href="#"` links across Admin, Company, Product, Certificate areas now either link correctly or are non-clickable.
- **Admin Roles permissions** (QC #105): descriptions corrected for Manage Workflow, View Audit Logs, Manage Tags.

## Mobile / responsive

- 17 real issues fixed from CSS audit after business users reported mobile layout problems.
- 8 font sizes bumped to 16px+ in `auth.css` to prevent iOS zoom on focus.
- Logo sizing fixed across breakpoints (was overriding itself at 480px).
- External certificate table: 11 low-priority columns hidden at mobile width, reducing horizontal scroll from ~2491px to ~1100px.
- **Sidebar search-icon vertical centering** (2026-04-26, post-deploy) — On tablet (≤1024px) and mobile, the magnifier icon inside the certificate listing search field was sitting above the input's visual center because the tablet media query used `padding-top: 8px` on `.search-bar`, which extended the container while leaving the absolutely-positioned icon (`top: 50%`) referenced to the taller box. Switched to `margin-top: 8px` so the icon's containing block matches the input. Verified at 5 viewports (1280/1024/768/414/375): icon-center vs input-center delta is now `0px` everywhere; mobile filter toggle (Show/Hide Filters) still hides and re-shows the search bar correctly. CSS only: `wwwroot/css/main.css`.
- Toast notifications reposition to full-width on mobile.

## Test infrastructure

- **363+ automated tests re-pointed at the live site** (`gccwebapp.com`). Fixed 14 files that were hardcoded to `localhost:5101` — 7 base classes, 4 configs, 3 UI test files.
- **Security test suite rewritten** — found and logged 2 real issues (PUT/DELETE on login returns 200 not 405; session cookie missing Secure flag).
- **Accessibility tests** — AxeCore re-enabled (was a placeholder).
- **Mobile test suite strengthened**: 22 NUnit tests with strict assertions, proper device viewports, orientation change coverage. Old suite was silently passing when elements weren't found.
- **Shared `RemoteTestBase`/`RemotePlaywrightBase` infrastructure** created in `Tests.Common` — all projects now configurable via `TEST_BASE_URL` environment variable.

## Test-environment hardening (new today, 2026-04-24)

- **HTTP Basic Auth gate** (`TestEnvironmentGateMiddleware`) in front of `gccwebapp.com` — since the test URL is publicly routable on the open internet, the whole site now requires a shared test password before anyone can reach it. Config-driven; Partner API (`/api/*`) bypasses so Amazon integration is unaffected.
- **`robots.txt`** with `Disallow: /` so search crawlers don't index the test site.
- **TEST ENVIRONMENT banner** on every page for visual disambiguation from the eventual real production URL.

## Deployment / environment fixes

- **Plesk test-deploy environment regression** fixed (2026-04-23). The publish profile had silently been set to `EnvironmentName=Test`, which loaded placeholder config and broke the deployed DB + AAD B2C login. Reset to `Production` and documented in `project_plesk_deploy_env.md` memory to prevent recurrence.
- **`wwwroot/uploads/` excluded from publish profile** (2026-04-24) so future deploys don't overwrite user-uploaded certificate PDFs on the server. Addresses the 2026-04-23 incident where prod uploads were wiped.

## References / docs

- `docs/tracking/defect-register.md` — canonical DEF-XXX log (DEF-097 onwards; DEF-001–DEF-096 in the prior register pending merge from OneDrive recovery).
- `0_Documentation/docs/AMAZON_API_DEVELOPER_GUIDE.html` — client-facing guide with CP2 review fixes.
- `docs/tracking/api-samples/` — live API JSON captures + PDF sample for Amazon reference.

---

## Deployments this month

Every item above ships via the Plesk test zip. Multiple zips deployed in April: sprint 2 build (2026-04-15), tile format (2026-04-16), three break-fix rounds (2026-04-17), break-fix resolution (2026-04-22), API updates (2026-04-24), DEF-097/DEF-098/access-gate build (2026-04-24), and DEF-100/DEF-101 build (`GCCWebApp-test-2026-04-26-2312.zip`, 61 MB) covering tile-view lazy-load fix and Import/Export icon convention sweep.

## Known items still open

- DEF-001 through DEF-096 registry backfill (pending recovery of the prior OneDrive register).
- Separate test DB from prod DB (current "test" deployment runs against prod). Tracked; bigger lift.
- `.pubxml` files should be committed to git to prevent silent environment regressions (memory flagged).
- Move `wwwroot/uploads/` onto separate storage (Azure Blob / volume) so user uploads don't live in the application folder.

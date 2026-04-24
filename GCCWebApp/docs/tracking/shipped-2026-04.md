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

Every item above ships via the Plesk test zip. Multiple zips deployed in April: sprint 2 build (2026-04-15), tile format (2026-04-16), three break-fix rounds (2026-04-17), break-fix resolution (2026-04-22), API updates (2026-04-24), and today's DEF-097/DEF-098/access-gate build.

## Known items still open

- DEF-001 through DEF-096 registry backfill (pending recovery of the prior OneDrive register).
- Separate test DB from prod DB (current "test" deployment runs against prod). Tracked; bigger lift.
- `.pubxml` files should be committed to git to prevent silent environment regressions (memory flagged).
- Move `wwwroot/uploads/` onto separate storage (Azure Blob / volume) so user uploads don't live in the application folder.

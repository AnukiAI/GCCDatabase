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
- **Deploy:** TBD

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
- **Deploy:** TBD

---

## Closed / Historical

*(Entries from the pre-existing register to be merged here if recovered.)*

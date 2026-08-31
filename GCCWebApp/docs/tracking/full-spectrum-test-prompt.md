c# Full-Spectrum Application Testing Prompt — GCC Database Solution

> Adapted from the generic `full-app-testing-prompt.md` template for the GCC Database Solution. All `{{ALL_CAPS_BRACKETS}}` placeholders have been filled from `CLAUDE.md`, `docs/tracking/defect-register.md`, `docs/tracking/test-execution-log.md`, and `docs/tracking/cutover-2026-05-01.md`. Last updated 2026-05-01.

---

## Your Role

You are a senior QA engineer running an end-to-end, day-in-the-life test pass on a production web application. Your job is not just to follow happy paths — you actively try to break things. You think like an annoyed user on a slow connection, an admin doing bulk operations, a mobile user with one thumb, and a screen reader user. **You are especially ruthless about JavaScript errors and CSS/visual defects, because those are what users see and complain about.**

You do not assume anything works. You verify.

## Application Under Test

- **App name:** GCC Database Solution (a.k.a. GCC Web App)
- **URL(s):**
  - **Production:** `https://database.greencirclecertified.com` — a dedicated subdomain (decided 2026-08-14). **Not live yet**, so it is not a valid test target. The earlier path-based address `greencirclecertified.com/database` was never activated and is retired; the app is not path-mounted, so it could never have served it.
  - **Staging / test env:** `https://gccwebapp.com` — Plesk-hosted, **runs against prod DB**, gated behind HTTP Basic Auth + `robots.txt` Disallow + on-screen TEST banner. `/api/*` paths bypass the basic-auth wall so Partner API calls with `X-API-Key` work normally.
  - **Local dev:** `http://localhost:5101` — default per `CLAUDE.md`. **Default your test target here unless explicitly directed otherwise** — staging writes to prod data.
- **Tech stack:** ASP.NET Core 8.0 MVC + EF Core 9.0 + SQL Server + Azure AD B2C + Bootstrap 5 + Razor views (.cshtml) + vanilla JS (no SPA framework)
- **Primary user roles:**
  - **Public** (anonymous) — `/Certificate/CertificateTileView` only
  - **External** (Partner role for human users) — read-only, **company-scoped data only**, NO Add/Edit cert buttons, NO Actions column on Product Mgmt, cannot view internal tags
  - **Analyst** — cert CRUD, workflow transitions, tags
  - **Admin** — full access including `/Admin/*` pages; admin user/role is undeletable
  - **Partner** (machine — `X-API-Key` header) — `/api/v1/*` only; JSON contract is **immutable** (Amazon Climate Pledge Friendly is the live integration)
- **Critical workflows (the ones that lose money/trust if broken):**
  1. **Certificate lifecycle (14-state workflow)** — Draft → … → Republished/Expired. State machine in `CertificateWorkflowService.cs`. Frozen by contract; do not modify, do verify every transition is reachable from at least one role and unreachable from roles without permission.
  2. **Amazon Partner API contract** (`/api/v1/certificates*`) — list, search by product ID, by-number, by-id, GS1 list, GS1 single, PDF download. Each endpoint must return ONLY public-status certs and ONLY the contract DTO shape (`status: "Active"` literal, `certificationCountries: "All"`, `productIds[]` array, no internal IDs). Contract source: `GCC Database Partner API - Amazon Developer Guide_updates_SW3.pdf` §4 + §8.
  3. **Cert tile views** — public (`/CertificateTileView`), external (`/CertificateTileViewExternal`), internal (`/CertificateTileViewInternal`). Tile mode AND list/table mode. Filters, sort, search, autocomplete, infinite scroll, shareable URLs, column-toggle, bulk selection, bulk download, CSV export.
  4. **External user data isolation** — External role can see ONLY their associated company/partner data. Direct-URL access to another company's cert ID must 403/404. Try API list with their cookie, try column-toggle to expose internal-only columns.
  5. **Admin Import/Export (CSV-only)** — every admin page (`/Admin/*`, `/Company/Index`, `/Product/Management`) has Import + Export + Template buttons. Export is **CSV only** — no Excel/JSON/PDF from admin. Import = down arrow icon, Export = up arrow icon (DEF-101).
- **Recent changes since last test pass:** Last cutover 2026-05-01, build `GCCWebApp-test-2026-05-01-0006.zip`. Items shipped:
  - **CR-015 / DEF-104** — `CertificateProductId.Status` now auto-derives from parent cert (cascade); Status column / field / filter REMOVED from Product ID Manager UI; one-time SQL backfill ran on prod 2026-05-01 (4 rows on Expired certs flipped from `Active` → `Inactive`).
  - **DEF-103** — Partner API `/api/v1/certificates/number/{n}` and `/{id}` were leaking raw entity (`status: "Client Approved - Paid"`, raw `country`, internal IDs, flat `asin/upc/gtin/ean`). Now project the contract DTO via shared `BuildPartnerCertificateDto` helper.
  - **DEF-098** — Tablet 1024px: duplicate "Hide Filters" bars on cert listing — desktop sidebar-collapse button now hidden at ≤1024px.
  - **DEF-100** — Tile-view list/table mode: lazy-load was wiping earlier pages. List-view append now uses `insertAdjacentHTML('beforeend')` instead of full re-render.
  - **DEF-101** — Import/Export icon arrows reversed across 17 admin views — Import now down-arrow, Export now up-arrow.
  - Earlier April: DEF-097 (Partner API `certificationCountries: "All"`), DEF-099 (external filter chevron toggle), DEF-102 (GS1 `additionalTradeItemId.name`), test-env access gate, mobile/tablet responsive fixes, Section A self-test PASS.
- **Test credentials:**
  - **Admin:** username `admin` / password `TestPass123!`
  - **Analyst:** username `analyst` / password `TestPass123!`
  - **External:** username `external` / password `TestPass123!`
  - **Partner (machine):** `X-API-Key` header. Local dev key (active in `GCCDatabase_Dev` on `ANUKI01`): `gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7`. Production Amazon key: ask Shawn — never paste in logs.
  - **Public:** anonymous, no creds, public route `/Certificate/CertificateTileView` only.
  - **Login form selectors** (for automation): `#Email`, `#Password`, `button[type="submit"]`. List-view rows are `<div class="table-body-row">` inside `#gridBody`, NOT `<tr>`.

## Testing Methodology — Run All Phases

### Phase 1: Day-in-the-Life End-to-End Journeys

Walk through the application as each persona for an entire "day" of realistic usage. Don't just click — narrate what the user is trying to accomplish, then attempt it. Note friction, confusion, dead ends, and anything that makes you say "wait, what?"

For each persona, run a full workflow: log in → primary task → secondary tasks → edge case actions → log out → log back in and verify state persisted.

**Personas to run:**
1. **First-time user** — no prior context, follows whatever the UI tells them
2. **Power user / admin** — uses keyboard shortcuts, bulk actions, advanced filters
3. **Returning user with stale session** — token expired, cached data, browser sat open overnight
4. **Mobile-only user** — never touches a desktop, one-thumb operation
5. **Analyst running bulk certificate operations** — imports 50 product IDs across 5 certs via the Product ID Manager CSV import, then moves 3 certs through the 14-state workflow (Draft → Submitted → Client Approved - Paid → Republished), then bulk-downloads the resulting PDFs from the internal tile view, then exports the cert list to CSV. Watch for: row drift on lazy-load (DEF-100 regression), product-ID Status not cascading correctly (CR-015 regression), Partner API showing pre-approval certs (DEF-097 regression), bulk-download building the wrong ZIP, double-submits on the workflow transition button.

### Phase 2: Full Regression

Run every documented feature in **`docs/tracking/test-execution-log.md`** (163 QC items, sections A–E) plus the open items in **`docs/tracking/defect-register.md`** (DEF-097 through DEF-104 currently). The xlsx source-of-truth is `Database QC Log_4.27.26_sw.xlsx` (sheet `GCC DB QC Log_4_17`); the test-execution-log mirrors it 1:1. For each:
- Confirm the feature still does what its spec says
- Confirm related features didn't break (CRUD on Entity A often breaks Entity B's list view — e.g., editing a Cert Type should not break the Cert Edit form's dropdown)
- Confirm permission boundaries hold:
  - External cannot view another company's certs (direct-URL)
  - External cannot see internal tags on certs
  - External cannot see Add/Edit/Delete buttons or the Actions column
  - Analyst cannot reach `/Admin/*` routes
  - Partner (`X-API-Key`) cannot reach any non-`/api/v1/*` route
  - Public can reach only `/Certificate/CertificateTileView`

**High-yield regression areas (failed before — re-test every pass):**
- Partner API DTO projection on every endpoint (list / search / by-id / by-number / GS1 list / GS1 single / PDF download). DEF-103 caught two endpoints that were missed in the 2026-04-21 fix.
- Tile-view list mode lazy-load (DEF-100) — internal AND external AND public.
- Tablet 1024px filter UI (DEF-098/099) — single Hide Filters bar, single-click chevron toggle.
- Import/Export icon arrows (DEF-101) — re-verify on Company/Index, Product/Management, Admin/ProductManagement, Admin/Tags, Admin/UserManagement, _ProductIdManager. (`Product/Categories` removed per DEF-272 — orphan page deleted.)
- `CertificationResults` display formatting (`0.17` → `17%`) on all 6 surfaces: internal detail view, internal tile API, Partner API list/search/single/GS1, product-ID search.
- Product ID Status cascade (CR-015) — Status column/field/filter must be GONE from UI; cascade fires on cert status change in both directions.
- Custom Fields rendering — single "Custom Fields" group everywhere, ignore `CustomField.Section`.

### Phase 3: UI Testing — Visual & Layout Defects

This is where you live. Be obsessive. For every page and every state:

- **Alignment & spacing** — anything off by even a few pixels, inconsistent padding, misaligned form fields, buttons that don't line up
- **Typography** — font fallbacks loading (FOUT/FOIT), mismatched font weights, text that's too small on mobile or too large on desktop, line-height issues, text getting cut off. **GCC rule: 16px minimum font size everywhere — sub-16px inputs cause iOS zoom-on-focus.**
- **Color & contrast** — WCAG AA contrast violations, hover states invisible, disabled states that look enabled (or vice versa), dark mode bugs if applicable
- **Overflow & truncation** — long names breaking layouts, tables with content escaping cells, ellipsis missing where it should be, horizontal scroll appearing where it shouldn't
- **Z-index / stacking** — modals behind sticky headers, dropdowns clipped by parent overflow, tooltips appearing under other elements
- **Loading & empty states** — spinners that never go away, layout shift when content loads (CLS), blank pages with no empty-state messaging, skeleton loaders that don't match the real content shape
- **Error states** — inline validation visible, toast/banner errors readable, form fields highlighted, error text actually helpful (not "An error occurred"). **GCC rule: NEVER use native `alert()` / `confirm()` / `prompt()`. Delete = `AdminCore.showConfirmation()` BEFORE + `AdminCore.showNotification()` AFTER. Success = toast only.**
- **Image handling** — broken image icons, missing alt text, images stretched/squished, lazy-loaded images that never load. Cert detail action icons (zoom + download) are right-aligned with the image, NOT centered.
- **Focus states** — visible keyboard focus rings on every interactive element, focus trapped in modals, focus restored after modal close
- **Print styles** — if the app expects to be printed, does it look sane on paper

### Phase 4: JavaScript Deep Dive

Open DevTools console **on every single page**. Treat any console output as a defect until proven otherwise.

- **Console errors** — log every error: page, action that triggered it, full stack
- **Console warnings** — deprecation warnings, hydration mismatches, jQuery deprecations (the app still uses jQuery in places). All of these are bugs.
- **Uncaught promise rejections** — these often hide silent failures
- **Network tab** — failed requests (4xx/5xx), CORS errors, requests that hang, duplicated requests, requests firing on every keystroke when they shouldn't (autocomplete is a known offender)
- **State bugs** — does the UI actually reflect the data after an action? Is there a race condition where clicking the workflow-transition button too fast double-submits? Test on Save, Submit, Approve, Delete, "Send to Workflow" buttons.
- **Event handler bugs** — buttons that need a double-click, forms that submit on Enter when they shouldn't (or don't when they should), event bubbling issues. The external view filter-chevron handler was bound 2–3× under DEF-099 — re-bind regressions are a known failure mode.
- **Memory leaks** — leave a tab open for 10 minutes navigating between cert tile pages and admin pages; does memory grow unbounded?
- **Source maps & bundles** — are source maps leaking to prod? Bundle size sanity check
- **Third-party script failures** — AAD B2C scripts failing silently is the high-impact case here

### Phase 5: CSS Deep Dive

- **Responsive breakpoints** — test at 320px, 375px, 414px, 768px, 1024px, 1280px, 1440px, 1920px. Note exactly which breakpoints break which components. The 480px breakpoint logo override has overridden the 768px fix before (April 2026 — caught and fixed).
- **Browser-specific CSS bugs** — flexbox quirks in Safari, Grid gaps in older Edge, scrollbar styling differences
- **Animation / transition issues** — janky animations, transitions that don't respect `prefers-reduced-motion`, animations that fire on initial render
- **Stylesheet conflicts** — global styles bleeding into components, specificity wars, `!important` cascading badly. The cert tile view CSS file has aggressive specificity to fight Bootstrap defaults.
- **Vendor prefixes** — anything missing for Safari? `-webkit-` prefixes for sticky, backdrop-filter, etc.
- **Layout shift** — measure CLS on key pages
- **Dynamic content** — does CSS hold up when content is empty, very long, or contains unusual characters/emoji/RTL text?
- **GCC rule: NO inline `max-width` overrides on views. Use `.admin-container` with proper structure. Use `ViewData["Title"]` (no `<h1>` in views).**

### Phase 6: Browser Compatibility

Run the top 3 critical workflows in **each** of these. Don't just open the homepage — actually do something. The top 3 workflows are: (a) public cert tile view + filter, (b) analyst Edit cert + workflow transition, (c) Amazon Partner API list + by-number.

| Browser | Versions | Why it matters |
|---|---|---|
| Chrome | Latest + N-1 | Largest share |
| Safari | Latest macOS + iOS | Quirkiest engine, mandatory for iPhone users |
| Firefox | Latest | Catches standards-compliance issues |
| Edge | Latest | Enterprise users; enterprise GCC client base |
| Samsung Internet | Latest | Default on many Android devices |

For each: log a separate bug if behavior diverges from Chrome.

### Phase 7: Mobile & Responsive

- **Real devices > emulators.** Use BrowserStack/Sauce Labs or actual phones if available. Emulators miss touch behavior, haptic, real keyboard quirks. The Playwright test suite already uses iPhone SE/12, Galaxy S20, iPad Mini/Air viewports.
- **Touch targets** — minimum 44x44px (Apple HIG) or 48x48px (Material). Flag anything smaller.
- **Tap behavior** — no 300ms delay, no double-tap zoom on inputs, no accidental double-fires
- **Virtual keyboard** — does it cover the input being typed in? Does the layout reflow correctly? Are correct keyboards triggered (`type="email"`, `type="tel"`, `inputmode`)?
- **Orientation change** — rotate device mid-task. Does state survive? Does layout reflow?
- **iOS Safari specific** — 100vh bug, safe-area-inset for notch, momentum scrolling, fixed positioning weirdness, **iOS zoom-on-focus when input font-size <16px** (this is THE recurring mobile bug in this codebase).
- **Android Chrome specific** — pull-to-refresh interfering with custom gestures, address bar showing/hiding affecting viewport
- **Slow networks** — throttle to "Slow 3G" in DevTools. Does the app degrade gracefully or just hang?

### Phase 8: UX & Heuristic Evaluation

Apply Nielsen's 10 heuristics. For each violation, note severity:
1. Visibility of system status (loading? saved? error?)
2. Match between system and real world (jargon vs. user language)
3. User control and freedom (undo, cancel, back)
4. Consistency and standards
5. Error prevention
6. Recognition rather than recall
7. Flexibility and efficiency (shortcuts for power users)
8. Aesthetic and minimalist design
9. Help users recognize, diagnose, recover from errors
10. Help and documentation

### Phase 9: Accessibility

- Run **axe DevTools** or **WAVE** on every page; log every violation. The test suite re-enabled AxeCore scanning April 2026 — confirm no regressions vs. the existing report at `ACCESSIBILITY_AUDIT_REPORT.md`.
- Tab through every page using only keyboard — can you complete every workflow?
- Test with VoiceOver (Mac/iOS) or NVDA (Windows) on at least one critical workflow
- Verify ARIA labels, landmarks, live regions for dynamic content
- Color is never the only signal (red/green status, error states)

### Phase 10: Performance

- Lighthouse score on key pages (Performance, Accessibility, Best Practices, SEO)
- Core Web Vitals: LCP, FID/INP, CLS
- Time-to-interactive on slow 3G
- Bundle size; any obvious wins (unused dependencies, unsplit chunks)?

---

## Bug Report Format — Use This Exactly

For every defect found, output a structured entry:

```
BUG-### | [SEVERITY] | [CATEGORY]
Title: <one-line summary that would make sense in a Jira backlog>

Environment:
- Browser/OS/Device:
- Screen size:
- User role / test account:
- URL:

Steps to Reproduce:
1.
2.
3.

Expected:
Actual:

Evidence:
- Screenshot/screen recording: <description or attach>
- Console output: <paste verbatim>
- Network failures: <paste verbatim>

Suspected cause (if obvious):
Suggested fix (if obvious):
```

**GCC integration note:** every BUG-### must also be filed in `docs/tracking/defect-register.md` as `DEF-XXX` (continue numbering from the highest existing entry — currently DEF-104). Both IDs cross-reference each other. Per Shawn's standing rule, every QC fix needs a DEF entry — no exceptions.

## Severity Rubric

- **S1 — Critical:** Data loss, security flaw, payment broken, app unusable for a primary persona. **GCC-specific S1 triggers:** any Partner API contract violation (status leak, country leak, internal IDs in response, flat product-ID fields), any external user reading another company's data, AAD B2C login broken for any role.
- **S2 — Major:** Core workflow broken or degraded, no workaround, or workaround is painful
- **S3 — Moderate:** Feature broken with workaround, or significant visual defect on a high-traffic page
- **S4 — Minor:** Cosmetic, low-traffic page, or affects edge-case persona only
- **S5 — Polish:** Nice-to-have, minor UX friction, copy edits

## Categories

`FUNCTIONAL` · `JS-ERROR` · `CSS-VISUAL` · `RESPONSIVE` · `BROWSER-COMPAT` · `PERFORMANCE` · `A11Y` · `UX` · `SECURITY` · `DATA-INTEGRITY` · `COPY`

---

## Final Deliverables

When you finish the pass, produce:

1. **Executive summary** — 5–10 bullet points covering health of the app, biggest risks, recommended go/no-go for release
2. **Bug list** — all defects in the format above, sorted by severity. Mirror into `docs/tracking/defect-register.md` as DEF-XXX entries.
3. **Heatmap** — which pages/features have the most defects (focus next sprint here)
4. **JS console error log** — every unique error, with frequency count and pages affected
5. **CSS/visual defect catalog** — screenshots grouped by component, with breakpoint annotations. Use Playwright `full_page` screenshots at 5 viewports per the standing convention: desktop 1280, desktop 1920, tablet 1024, mobile 414, mobile 375. Wait for content (not just `networkidle`).
6. **Browser/device matrix** — pass/fail grid for the top 3 workflows × browser/device combinations
7. **Top 10 quick wins** — defects that are low-effort, high-impact to fix this week

**GCC integration notes for deliverables:**
- New test cards land in `docs/tracking/test-execution-log.md` under the appropriate Section (A = recent fixes, B = high-pri Ready-for-UAT, C = high-pri Open, D = regression sweep, E = stub backlog).
- When a fix ships, append a paragraph to `docs/tracking/shipped-2026-05.md` (create if absent — current month). Format matches `shipped-2026-04.md`.
- In any text destined for client-facing comms, refer to the other side as "client", not by name.
- Do NOT run `git commit` — leave changes in the working tree for Shawn.
- Do NOT refactor adjacent code while testing. If you spot something, log it as a defect; don't drive-by-fix it.

---

## Mindset Reminders

- If you find yourself thinking "the user probably won't do that" — that's exactly when you do it
- Resize the window mid-action. Hit Back mid-form. Lose connectivity mid-submit
- Paste 5,000 characters into every text field at least once
- Try emoji, RTL text, SQL-injection-looking strings, leading/trailing whitespace
- Click the same button five times in 200ms
- Open the same workflow in two tabs and act on both
- Sign in as one user, then switch accounts in another tab — does state leak?
- A console warning is a bug. A layout shift is a bug. A janky transition is a bug. **Log everything.**

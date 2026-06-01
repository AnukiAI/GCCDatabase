# E2E Smoke Plan — Per-View Coverage Matrix

**Status:** Scoped — not yet built
**Owner:** Anuki (Shawn West, dev)
**Date scoped:** 2026-06-01
**Target start:** TBD (Shawn's call)
**Estimated effort:** ~1 day infra + ~1 day per-view smoke implementation = **~2 days total** (or ~1 day for Tier-1-only)

## Why this exists

Per Shawn 2026-06-01: *"Do we have all permutations of each view for end-to-end scenarios?"* Answer: **no**. What we have today is 31 one-shot Playwright probes in `Scripts/smoke-prod/` (mostly per-DEF verifications like `def232-template-verify.js`) plus a 172-card test execution log that's largely stubs. There is no view × role × viewport × workflow-state matrix.

This plan documents the **middle path** between "ship and hope" and "build the full enterprise test pyramid." It targets ~70% of the realistic regression risk for ~20% of the effort of a full pass.

## What's in scope

A **reusable per-view smoke template** that loops:
- **4 roles** (Public anonymous, External logged-in, Analyst, Admin)
- **3 viewports** (desktop 1440×900, tablet 768×1024, mobile 375×667)
- **A view-specific checklist** of must-render-correctly assertions (page loads, expected sections present, no console errors, no horizontal scroll on mobile, key actions reachable per role)

Optional **Tier 2** adds a **cert workflow state matrix** for tile views only (the most state-sensitive surfaces).

## What's NOT in scope

- Unit tests, integration tests, API contract tests (separate effort)
- Full keyboard-navigation accessibility audit (separate WCAG pass)
- Performance/load testing
- Cross-browser (Chromium only; Firefox/Safari deferred)
- "Happy-path data CRUD" tests for every admin form (admins are 1-2 known users; covered by UAT)

## Role-to-view access matrix (source of truth)

Derived from `Controllers/*.cs` `[Authorize(Roles=...)]` attributes and route policies. Marked with what each role should see / be denied.

| View | Public | External | Analyst | Admin | Partner (API only) |
|---|---|---|---|---|---|
| `/Home/Index` | ✅ | ✅ | ✅ | ✅ | n/a |
| `/Account/Login` | ✅ | redirect | redirect | redirect | n/a |
| `/Account/ForgotPassword`, `/ResetPassword` | ✅ | ✅ | ✅ | ✅ | n/a |
| `/Account/AccessDenied`, `AccountLocked`, `SessionExpired` | ✅ | ✅ | ✅ | ✅ | n/a |
| `/Certificate/CertificateTileView` (public) | ✅ | ✅ | ✅ | ✅ | n/a |
| `/Certificate/CertificateTileViewExternal` | ❌ → 401 | ✅ | ✅ | ✅ | n/a |
| `/Certificate/CertificateTileViewInternal` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Certificate/CertificateSingleView` (public single) | ✅ if status=Paid/Republished, else 404 | same | full | full | n/a |
| `/Certificate/CertificateExternalView` | scoped | scoped | full | full | n/a |
| `/Certificate/CertificateInternalView` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Certificate/Create` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Certificate/Edit/{id}` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/CertificateImport/Index` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Dashboard/Index` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Company/Management`, `/Index` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Product/Management` | ❌ → 401 | ❌ → 403 | ✅ | ✅ | n/a |
| `/Admin/UserManagement`, `RoleManagement`, `Tags`, `Partners`, `ProductTypes`, `CertificateTypes`, `CertificateFields`, `Pathways`, `GoogleAnalytics`, `CSFAttributes`, `GCCAPI`, `PartnerAPI` | ❌ → 401 | ❌ → 403 | ❌ → 403 | ✅ | n/a |
| `/api/v1/certificates*` | ❌ → 401 | filtered by company | filtered | full | filtered by partner |

**Negative-path coverage matters here:** the External-blocked-from-Internal-view and Public-blocked-from-Admin checks are the most security-relevant assertions, and the smoke template should call them out as PASS-when-denied (DEF-204/206/208 history).

## Workflow state matrix (cert tile views only — optional Tier 2)

Per `Models/CertificateStatus.cs` (memory `feedback_workflow_state_count.md`): 8 states. Public-eligible = `ClientApprovedPaid` + `Republished`.

| State | Public tile view | External tile view (own company) | External tile view (other company) | Internal tile view (Analyst/Admin) |
|---|---|---|---|---|
| Draft | hidden | hidden | hidden | shown |
| ClientApprovedNotPaid | hidden | shown (pre-publish hold for visibility per spec) | hidden | shown |
| ClientApprovedConfidential | hidden | shown | hidden | shown |
| ClientApprovedPaid | **shown** | shown | hidden | shown |
| Hold | hidden | hidden | hidden | shown |
| Expired | hidden | hidden | hidden | shown |
| Republished | **shown** | shown | hidden | shown |
| ExpiredNoAnnualVerification | hidden | hidden | hidden | shown |

**Seeding cost.** 8 states × 4 viewer contexts = 32 cells. Need 8 cert fixtures in seed data (one per state, all tagged to one company). One fixture per state is enough — the matrix asserts visibility, not field-level rendering.

## Reusable infrastructure (Day 1)

One shared file: `Scripts/smoke-prod/_lib/smoke-template.js`. Exports:

```js
runViewSmoke({
  viewId: 'cert-tile-public',
  url: '/Certificate/CertificateTileView',
  roles: ['public', 'external', 'analyst', 'admin'],
  viewports: ['desktop', 'tablet', 'mobile'],
  expectedAccess: { public: 200, external: 200, analyst: 200, admin: 200 },
  assertions: async (page, role, viewport) => {
    // view-specific checks
  },
  screenshotOn: 'fail'  // or 'always' for UAT evidence
})
```

What the template handles once:
- Login helpers per role (using `admin`/`analyst`/`external` test accounts; `public` = no login).
- Viewport setup with the same 3 sizes used in mobile audit (memory `feedback_playwright_screenshots`).
- Console-error capture per page (fail the test if a `[pageerror]` or `[console.error]` fires that isn't an expected one — pageUnloading false-positives suppressed per existing tile-view pattern).
- Network-error capture (4xx/5xx on resources).
- Mobile horizontal-scroll check (`document.documentElement.scrollWidth > window.innerWidth` → fail).
- Screenshot capture on fail (or `always` for UAT receipts).
- JSON results file per view (`Scripts/smoke-prod/_results/{viewId}-{timestamp}.json`).
- Roll-up summary file across all views (`Scripts/smoke-prod/_results/summary-{timestamp}.json`).
- Tolerates `expectedAccess: 403` (PASS when access is correctly denied).

Day 1 cost: ~6-8h. Includes one fully-working view example (`/Home/Index`) as the reference implementation.

## Tier 1 — Critical views (Day 2)

10 views, each ~30 min to write the assertion block (~5h):

| # | viewId | URL | Roles tested | Key assertions |
|---|---|---|---|---|
| 1 | `home` | `/Home/Index` | all 4 + public | logo renders, nav links present per role, no console errors |
| 2 | `login` | `/Account/Login` | public | email/password fields, no double-h1 (DEF-104 history), no horizontal scroll on mobile |
| 3 | `cert-tile-public` | `/Certificate/CertificateTileView` | all 4 + public | tile grid renders, only Paid+Republished certs visible, filter sidebar present, infinite scroll loads page 2 |
| 4 | `cert-tile-external` | `/Certificate/CertificateTileViewExternal` | external/analyst/admin (public → 401) | company-scoped subset, internal tags hidden, no Add/Edit buttons for External |
| 5 | `cert-tile-internal` | `/Certificate/CertificateTileViewInternal` | analyst/admin (public+external → 403) | all 8 states visible, internal tags shown, Add Certificate button present |
| 6 | `cert-create` | `/Certificate/Create` | analyst/admin | form sections present, Partner+Pathway+Cert Type dropdowns populate, Certification Results field exists (CR-001 history) |
| 7 | `cert-edit` | `/Certificate/Edit/{seedId}` | analyst/admin | form pre-populated, Product Name shows saved value (DEF-225 history), Product ID Manager renders |
| 8 | `dashboard` | `/Dashboard/Index` | analyst/admin | 5 stat tiles render, breakdown tables totals match badges (DEF-221 history), mobile stats-grid is single column (DEF-114 history) |
| 9 | `admin-user-mgmt` | `/Admin/UserManagement` | admin | user table loads, Template+Import+Export buttons present, Add User modal opens with Partner column visible (DEF-232) |
| 10 | `admin-tags` | `/Admin/Tags` | admin | tag table loads, Template download produces 3-column CSV (DEF-232), Add Tag modal opens |

Day 2 cost: ~5h. Each view smoke = ~30 min following the template.

## Tier 2 — Cert state matrix (optional, +0.5 day)

If we add the workflow-state coverage on tile views (recommended for the next major UAT):
- Seed script: `Scripts/smoke-prod/_lib/seed-state-fixtures.sql` — creates 8 cert fixtures (one per state) under a dedicated test company `ZZ_SmokeFixture_Co_*`.
- Cert tile smokes 3/4/5 above get extended with state-visibility loops.
- Cleanup script: `Scripts/smoke-prod/_lib/teardown-state-fixtures.sql` — removes all `ZZ_SmokeFixture_*` rows.

Extra cost: ~4h (seed/teardown + state-visibility assertion blocks).

## Acceptance criteria for "done"

The smoke pass is "green" when:
1. All 10 Tier 1 views return 0 FAIL across 4 roles × 3 viewports.
2. No unexpected `[pageerror]` or `[console.error]` lines.
3. No mobile horizontal scroll on any view.
4. Negative-path access (Public→Internal, External→Admin, etc.) returns the expected 401/403 — counted as PASS.
5. Roll-up summary JSON ≤30 lines, written to `Scripts/smoke-prod/_results/summary-{timestamp}.json`.

## What this plan does NOT promise

- **It is not a substitute for client UAT.** Carly will still find things — this catches *regressions*, not undiscovered requirements.
- **It does not test all admin CRUD permutations.** Admin forms are exercised at the "page renders" level; field-level CRUD is left to per-DEF probes.
- **It does not catch logic bugs that produce wrong-but-renderable output** (e.g. the DEF-220 "wrong cert counted as expired" class needs targeted assertions, not page-level smoke). Those still need a per-DEF probe when reported.
- **No cross-browser.** Chromium only. Firefox/Safari deferred until a customer complaint.

## Recommended sequencing

1. **Day 1 (infra + reference impl).** Ship `_lib/smoke-template.js` + working `home` smoke. Get Shawn's review before scaling to 10 views — feedback on output shape, screenshot policy, etc. is cheap to incorporate before duplication.
2. **Day 2 (Tier 1 fan-out).** Add the other 9 views. Run full pass. Roll-up summary becomes a deploy gate ("don't push if any view FAILs").
3. **Day 3 (optional Tier 2).** State-matrix on cert tile views. Recommended before the next major UAT cycle (next client retest).

## Open questions before kicking off

- **Test data isolation.** Live test DB on localhost has real seeded data. Tier 2 state fixtures need a known company name we won't collide with. Convention `ZZ_SmokeFixture_Co_{timestamp}` proposed.
- **Screenshot evidence.** "On fail" minimizes disk; "always" produces UAT receipts. Default: on-fail. Override flag `SMOKE_SCREENSHOTS=always` for evidence captures.
- **Public role login.** The "Public" persona has no credentials — smoke runs as an unauthenticated browser session. Needs the test-env access gate to allow `/` and `/Account/Login` for the smoke runner's IP (already true on localhost; if we ever run this against gccwebapp.com, need a smoke-runner exception).
- **Deploy gate vs. nightly.** Should the roll-up summary block Plesk deploys, or just run nightly with an email digest? Default proposal: nightly + on-demand; gate is too aggressive for a 1-developer project where deploys are infrequent.

## Tracking

- This document → `docs/tracking/e2e-smoke-plan.md`
- Per-view smoke files → `Scripts/smoke-prod/_views/{viewId}.js`
- Shared library → `Scripts/smoke-prod/_lib/smoke-template.js`
- Roll-up results → `Scripts/smoke-prod/_results/`
- Status updates on this plan → bottom of this file as a dated bullet log; entry in `observations_log.md` when the infra ships

## Progress log

- **2026-06-01 — Day 1 SHIPPED.** Infra + home reference impl complete and green on localhost:5101.
  - **Built:** `Scripts/smoke-prod/_lib/smoke-template.js` (~250 lines). Exports `runViewSmoke({viewId, url, roles, viewports, expectedAccess, assertions})`. Handles: login per role (uses CLAUDE.md test creds), viewport sizing, console + page-error + network-error capture (with ignored-pattern allowlist), screenshot policy via `SMOKE_SCREENSHOTS` env (`on-fail` default), access-gate check (expectedAccess: 200/401/403 with login-redirect → 401 normalization), mobile horizontal-scroll guard, JSON results writer to `_results/{viewId}-{stamp}.json`. Refuses to run against prod URLs unless `SMOKE_ALLOW_REMOTE=1`.
  - **Built:** `Scripts/smoke-prod/_views/home.js` (~85 lines). Reference impl for `/Home/Index`. Asserts role-based redirect lands on the right tile view (public→`/`, external→`CertificateTileViewExternal`, analyst/admin→`CertificateTileViewInternal`), logo + main nav present, mobile toggle reachable, auth-state-aware login link / logout form visibility.
  - **Verified:** 104/104 PASS twice consecutively against localhost (4 roles × 3 viewports × ~8-10 assertions each). Output JSON in `Scripts/smoke-prod/_results/`.
  - **Findings during validation:**
    1. **Public landing URL is `/` not `/Certificate/CertificateTileView`** — verified via `curl -I /Home/Index`: server returns `Location: /`. ASP.NET URL generator picks the shorter canonical alias. Not a defect — smoke design issue. Fix: accept both as valid landing paths for public.
    2. **DEF-233 — pageUnloading guard missing on `loadFilterOptions`.** Caught on smoke run 3 (9/104 FAIL). Existing pageUnloading fix only wrapped `loadInitialCertificates` catch; the parallel `loadFilterOptions` fetch in all 3 tile-view scripts logged a nondeterministic "Failed to fetch" on smoke context-teardown. 2-line fix per file, 3 files. Logged as DEF-233 in defect register.
  - **Effort:** ~3.5h (1h infra design + write, 0.5h home.js, 2h iterate + DEF-233 fix + docs).
  - **Decisions taken (from "open questions"):**
    - Test data isolation: `ZZ_SmokeFixture_Co_{timestamp}` convention — not used yet (Tier 1 doesn't need fixtures).
    - Screenshot policy: `on-fail` default, env override `SMOKE_SCREENSHOTS=always` for evidence captures.
    - Public-role login: localhost is open; live-env smoke would need a runner-IP exception (deferred, no current need).
    - Deploy gate vs nightly: deferred — run on-demand for now.
- **Day 2 — not yet started.** Pending Shawn review of the home.js reference. Next 9 views per Tier 1 table.

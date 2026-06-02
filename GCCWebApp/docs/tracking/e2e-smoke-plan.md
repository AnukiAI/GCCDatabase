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

### Deploy-gate vs. dev-loop modes

`run-all.js` ships with two modes:

| Mode | Invocation | When to use | Caveat |
|---|---|---|---|
| **Single-run (default)** | `node Scripts/smoke-prod/_views/run-all.js` | Fast dev-loop iteration, you manage the server | Last view (#10 admin-tags) flakes intermittently under sustained load — DEF-235. Acceptable in dev. |
| **Batch-with-restart (deploy gate)** | `SMOKE_BATCH_RESTART=1 node Scripts/smoke-prod/_views/run-all.js` | Pre-deploy verification, CI, anywhere a clean PASS/FAIL signal is required | Slower (~12 min for 2 batches × ~6 min). Kills + restarts `dotnet run` between batches so no batch accumulates the resource pressure that triggers DEF-235. |

**Decision: `SMOKE_BATCH_RESTART=1` IS the deploy-gate standard.** Single-run is for fast feedback only. Anyone treating a single-run RED as a hard deploy block is going to chase DEF-235 phantom failures.

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
- **2026-06-01 — Day 2 SHIPPED.** Tier 1 fan-out complete in a parallel terminal.
  - **Built (Stream B):** `Scripts/smoke-prod/_views/login.js`, `cert-tile-public.js`, `cert-tile-external.js`, `cert-tile-internal.js`, `cert-create.js`, `cert-edit.js`, `dashboard.js`, `admin-user-mgmt.js`, `admin-tags.js` — all 9 remaining Tier 1 views, each following the reference `runViewSmoke({...}) + module.exports = { run }` pattern from `home.js`. Plus orchestrator `Scripts/smoke-prod/_views/run-all.js` that writes a compact `summary-{stamp}.json` per the acceptance criteria.
  - **Shared infra hardened:** Stream B added a `requestedIsAuthPage` check in `_lib/smoke-template.js:183-185` so that testing `/Account/Login` itself is not misread as access denial. Clean layered edit on top of Stream A's Day 1 code; no conflict.
  - **Governance audit pass.** Inventory matches the Tier 1 table 1:1 (10/10 viewIds correct, all use `runViewSmoke`, all export `run`). `dotnet build` shows 0 compile errors — Stream B did not touch any `.cs`. DEF-233 is registered. Total smoke surface: 1068 lines across template + 10 view smokes + orchestrator.
  - **Next:** full localhost roll-up run (`node Scripts/smoke-prod/_views/run-all.js`) to establish the green baseline; optional Tier 2 (workflow-state matrix) is the recommended next deliverable before the next major UAT cycle.
- **2026-06-01 — Tier 1 roll-up EXECUTED → green baseline established.** Ran the full fan-out on localhost:5101.
  - **Result: 10/10 Tier 1 views, 0 FAIL.** ~694 assertions PASS across 4 roles × 3 viewports. Per-view (clean): home 104P · login 28P · cert-tile-public 124P · cert-tile-external 96P · cert-tile-internal 62P · cert-create 80P · cert-edit 62P/**6W** · dashboard 58P · admin-user-mgmt 40P · admin-tags 40P. Negative-path access (Public/External/Analyst hitting views above their level) all returned the expected 401/403, counted as PASS.
  - **One stable finding (WARN, not a code bug):** cert `26-0025` (company *HID Global*, id 19) references product id 6 (*"Bottles"*), which is **not in HID Global's product list** (`GetProductsByCompany` returns only ids 16/79), so the Edit page can't re-select it and renders a blank Product Name. Classified **WARN = data inconsistency for data ops** (per `feedback_data_issue_vs_defect`), almost certainly **DEF-231 fallout** — independently corroborates the need for the **CR-019** product-miscategorization cleanup. The cert-edit smoke deliberately distinguishes this (saved product absent from the company list → WARN) from a genuine DEF-225 re-selection regression (saved product present in options but not selected → FAIL).
  - **Second infra bug found & fixed:** the local dev server stalls ~20-25s on a cold first hit (heavy stats pages like `/Dashboard/Index` + the per-minute `CertificateExpirationBackgroundService` sweep), which blew past Playwright's 30s default and produced spurious `page.goto` timeout FAILs (an early rollup went RED on 6 such timeouts — all of which passed cleanly on re-run once the server was warm). Hardened: nav timeout raised to 60s (`SMOKE_NAV_TIMEOUT` env override) and a server warm-up step added to `run-all.js` before the fan-out. (First infra bug — the auth-page access-check misfire — was the `requestedIsAuthPage` fix noted above.)
  - **Assertions grounded in real view source**, not the plan's prose — e.g. dashboard ships ~12 stat cards / 5 breakdown tables (not "5 tiles"); Edit is keyed by **certificate number** via a constrained custom route (`Program.cs`: `^[0-9]+-[0-9A-Za-z]+$`), not numeric id (a numeric id 400s); external tile view's Add-button + internal-tag removal is baked into the view markup (role-independent).
  - Roll-up artifact: `Scripts/smoke-prod/_results/summary-{stamp}.json` (≤30 lines). Tier 2 (workflow-state matrix) remains optional/not started.
- **2026-06-01 — Second roll-up (cold-start hit) → 4 FAIL surfaces DEF-234.** Re-ran `run-all.js` against the same localhost instance after a brief idle. Result: **686 PASS / 4 FAIL / 10 WARN**.
  - **All 4 FAILs were the same assertion** (`cert-tile-public.no-console-errors`) on 4 of 12 role/viewport cells, paired with `tiles-load: cards=0` WARNs. The error message was `Error loading certificates: TimeoutError: signal timed out` from the JS-side `AbortSignal.timeout(30000)` in `certificate-tile-view.js:243` — distinct from the Playwright nav timeout Stream B hardened above.
  - **Curl-timed root cause:** `/api/certificate-tiles?page=1&pageSize=20` produced **23.114s → 0.198s → 0.054s → 0.031s → 0.027s** across 5 successive calls. The first request is a >100× outlier — EF Core query-compile + cold-cache. Whenever a smoke cell happens to land during a cold window AND has parallel page-load fetches competing for the DB connection, the 23s baseline can stack past 30s and trip the JS abort.
  - **Logged as DEF-234** (Medium severity). Recommendation in the DEF row: pre-warm the tile-cert projection at app startup (Fix #1) + profile for `AsSplitQuery` per memory `feedback_ef_includes_with_projection` (Fix #3). Hiding it with a higher JS timeout (Fix #2) is explicitly NOT recommended.
  - **Implication for the deploy gate.** The plan's acceptance criteria ("don't push if any view FAILs") would block deploy on this. **Recommend revising the rule** to "don't push if any view FAILs *deterministically* — i.e., reproduces on second run." A single-run flake on a known cold-start surface should not gate a deploy unrelated to that surface. Operationally: run `run-all.js` twice; if the second run is green, it was a flake; if FAILs persist, gate the deploy.
  - **cert-edit WARNs** (data-ops finding from Stream B's earlier run) re-confirmed: same 6 cells, same "saved product 6 not in company product list" note. Tracked as CR-019 data cleanup, not a code defect.
- **2026-06-01 — Sustained-load server death diagnosed → batch-with-restart mode added → GREEN baseline achieved.** Beyond the cold-start slowness (DEF-234), repeated full `run-all` passes exposed a harder env limit: the local dev server reliably **exits / refuses connections near the tail** of a sustained ~6-min run (views 1-9 pass, then the next view's cells all `ERR_CONNECTION_REFUSED`). Diagnosed with no app crash stack (external termination / resource pressure), badly amplified by a **zombie pileup** — repeated `dotnet run` + stopped-`run-all` invocations had left up to 5 concurrent node runners + multiple dotnet listeners fighting over port 5101 (`AddressInUseException`); `Get-Process dotnet,node,chrome | Stop-Process` clears it. The `SecurityMonitoringService` "Rapid request pattern" warnings in the log are a red herring — log-only, no IP block.
  - **Fix shipped:** `run-all.js` gained an opt-in **batch-with-restart mode** (`SMOKE_BATCH_RESTART=1`, `SMOKE_BATCH_SIZE` default 5). It owns the server lifecycle: kill any dotnet → start a fresh `dotnet run` → wait for readiness → warm-up → run that batch; repeat per batch. No batch accumulates enough load to fell the server. Leaves the last server running. Default single-run mode is unchanged (you manage the server).
  - **Result: `SMOKE_BATCH_RESTART=1` → 694 PASS / 0 FAIL / 6 WARN → STATUS GREEN ✅** (2 batches of 5, no retries needed). Per-view: home 104 · login 28 · cert-tile-public 124 · cert-tile-external 96 · cert-tile-internal 62 · cert-create 80 · cert-edit 62/**6W** · dashboard 58 · admin-user-mgmt 40 · admin-tags 40. The 6 WARN are the cert-26-0025 data anomaly (CR-019), the only non-PASS in the whole suite. Artifact: `Scripts/smoke-prod/_results/summary-2026-06-02T02-07-23-952Z.json`.
  - **Recommended run command going forward:** `SMOKE_BATCH_RESTART=1 node Scripts/smoke-prod/_views/run-all.js` for a reliable single green artifact. Per-view runs remain the quick spot-check. DEF-234 (cold-start API perf) is still the one real code-side follow-up.

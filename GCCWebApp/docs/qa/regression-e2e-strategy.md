# GCC Web App — Full Regression & End-to-End Test Strategy

**Author:** QA lead / test architect pass
**Date:** 2026-07-07
**Status:** DRAFT for approval — consolidates the 2026-07-02 Phase 0 discovery (`coverage-matrix.md`, `gap-report.md`) and the shipped `e2e-smoke-plan.md` into one regression + E2E strategy.
**Audience:** QA, business users, product owner, project manager, developers — written to be read by all five.

> **Scope note (read first).** This strategy covers **GCCWebApp as it actually exists**: an ASP.NET Core certificate database with public browse, role-scoped internal/external views, an 8-state certificate workflow, admin management, and a partner API (Amazon). The generic prompt that seeded this request referenced **BarTender label printing, purchasing/receiving, and invoicing/AR** — **none of those exist in this application** (confirmed in `gap-report.md` §3 D6). They are explicitly **out of scope** and excluded from all coverage math below. If those workflows were intended for a different system, flag it before we invest test effort.

---

## 1. Objective & Testing Philosophy

Give every release a **repeatable regression signal** that answers one question with evidence: *"Did this change break anything a client, an external partner, Amazon, or an admin depends on?"*

Priorities, in order:

1. **Data isolation & access control never regress.** The highest-cost failure mode in this app is a scoping leak — an external user or Amazon seeing another company's/partner's data, or a public visitor seeing a non-public certificate. History backs this (QC #171, DEF-103/164/198/204/205/236). This is the #1 regression target.
2. **The certificate lifecycle is correct end-to-end** — create → workflow state → visibility per role → edit → export/API, including PDF handling and product-ID linkage.
3. **Regression coverage over unit isolation.** Per the request, we favor journey/E2E assertions over deep unit coverage — because this app's risk lives in *integration* (RBAC + query scoping + workflow + view rendering), not in isolated algorithms. The one place a pure unit test is right (search normalization) already has one.
4. **Business-readable results.** Every run produces a PASS/FAIL roll-up a non-engineer can act on, and every failure maps to a DEF-XXX in `docs/tracking/defect-register.md`.

---

## 2. Current State — What We Already Have (Baseline)

This is not a greenfield. The strategy **extends** these assets; it does not replace them.

| Layer | Asset | Coverage today | Health |
|---|---|---|---|
| **Unit (.NET)** | `GCCWebApp.Tests` (xUnit) | **1 class** — `SearchNormalizerTests` (~13 methods / ~35 cases), CR-187 search contract only | 11 PASS. **No** service/controller/workflow/validation unit tests. |
| **Per-view E2E smoke** | `Scripts/smoke-prod/_views/` + `_lib/smoke-template.js` | 12 view smokes (home, login, 3 tile views, create, edit, dashboard, product-mgmt, admin user-mgmt, admin-tags) + `cert-state-matrix` (8 states × 3 views × roles). 4 roles × 3 viewports. | **GREEN — ~694 PASS / 0 FAIL / 6 WARN** (last full roll-up). Localhost-only. |
| **Post-deploy prod smoke** | `Scripts/smoke-prod/smoke.js` | 60-sec prod confidence: auth gate, API-key bypass, UI renders, CSS freshness/geometry, role pages non-500 | Runs against `gccwebapp.com`. Deploy sanity, not functional depth. |
| **Read-only prod E2E** | `prod-e2e-readonly-batch.js` | API-level: partner-leak prevention, status filtering, RBAC denial, search, security headers. UAT-tagged. | Read paths only (can't cover writes). |
| **Defect-repro probes** | ~40 `def*.js` / `qc*.js` / `verify-*.js` one-offs | Investigative, per-DEF | **Not a maintained suite** — snapshots of past bugs, not re-run as regression. |
| **Test harness** | `Scripts/testing/run-all-tests.ps1`, `run-tests.cmd` | `dotnet test` → `.trx`/HTML into `TestResults_*` | Manual invocation. |

**Structural gaps in the current setup (the strategy must close these):**

- **No CI.** Nothing runs on push/PR. Every suite is hand-invoked. This is the single biggest process gap — regressions are only caught if someone remembers to run the smokes.
- **No write-path (CRUD) coverage.** Every smoke asserts *"the form loads"* — never that a save persists or a delete removes. The core business flow is untested end-to-end.
- **Server-side logic rides entirely on browser smokes.** Controllers, services, the 8-state workflow machine, and query scoping have **zero** unit/integration tests. If the UI renders, we assume the logic is right.
- **The one-off `def*.js` probes are wasted regression value** — they proved a fix once, then went stale. High-value ones (partner isolation, IDOR, import guards) should be promoted into the maintained suite.

---

## 3. Risk-Based Test Prioritization

We cannot test everything with equal depth. Effort follows risk. (Cost-control is a standing project constraint — see `project_lessons_learned`.)

| Rank | Risk area | Why it's top-ranked | Blast radius if it regresses |
|:--:|---|---|---|
| **P0** | **Partner / company data isolation** (external view, external detail, single-cert download, Partner API v1) | Documented repeat-offender class (DEF-103/164/198/204/205/236). Scoping bugs recur on *adjacent* endpoints (list fixed, detail/download missed). | Amazon or a company sees a competitor's certificates → contract/legal exposure. |
| **P0** | **RBAC negative paths** (Public→Internal, External→Admin, Analyst→Admin all denied) | Security boundary; regressions are silent (a page that should 403 starts 200-ing). | Privilege escalation; unauthorized data/actions. |
| **P0** | **Certificate workflow-state visibility** (8 states × public/external/internal) | Public/External must show only `Paid`+`Republished`+not-expired. Per-minute expiration sweep interacts with this. | Draft/Confidential/Hold certs leak to public; paid certs vanish. |
| **P1** | **Certificate CRUD round-trip** (create→persist→edit→save→delete) incl. PDF upload/replace/remove, product-ID linkage, `Certification Results` field (CR-001) | The core analyst workflow. **Currently zero end-to-end coverage.** | Analysts can't create/edit certs; data loss on save; broken product links. |
| **P1** | **Partner API contract stability** (`/api/v1/certificates*`) | Contract is **immutable** (`feedback_amazon_api_immutable`); Amazon consumes it. Gate 4 already has a mechanical contract-diff. | Silent shape change breaks Amazon's ingestion. |
| **P1** | **Import/Export integrity** (CSV upload→preview→apply→undo; company-collision guard DEF-292; global product-ID export CR-184) | Bulk data path; a bad import corrupts many rows at once. | Mass data corruption; failed migration. |
| **P2** | **Auth flows** (login, 2FA setup/challenge, forgot/reset password, lockout) | Security-critical but low change-frequency. **Note: forgot-password email is blocked in prod** (SMTP unconfigured, DEF-263/CR-009) — journey is untestable end-to-end until fixed. | Users locked out; reset unusable. |
| **P2** | **Dashboard & reporting accuracy** (stat counts, breakdown totals, CSV exports) | Wrong-but-renderable numbers (DEF-220/221 class) slip past page-level smoke. | Client makes decisions on wrong counts. |
| **P3** | **Admin CRUD** (tags, cert types, partners, product types, custom fields, roles) | 1–2 known admin users; largely UAT-covered. Page-render smoke is enough for most. | Admin inconvenience; localized. |
| **P3** | **Static/misc** (Home, Privacy, Error, Document lifecycle) | Low risk, low change. | Cosmetic. |

**Undocumented subsystems flagged for a scope decision, not yet tested:** `ETLController` (11 endpoints, absent from all reqs — `gap-report.md` G7), API v1 Metrics controller, bulk certificate write endpoints, and shipped dev scaffolds routable in prod (`Create2`, `Admin/CreateUser` mock UI, `Account/TestAuth`, `Test/ProductTypes`, `Product/ManagementV2/Alt`). **These need an in/out-of-scope ruling before they get test effort or removal.**

---

## 4. The Test Pyramid for This App (Target Shape)

Deliberately **diamond-shaped, not pyramid-shaped** — thin unit base, thick integration/E2E middle — because the risk is integration, not algorithms.

```
        ┌─────────────────────────┐
        │   Manual / Client UAT   │  Exploratory, new-requirement discovery (Carly)
        ├─────────────────────────┤
        │   E2E journeys (thick)  │  ← primary regression signal: roles×views×states + CRUD round-trips
        ├─────────────────────────┤
        │  API / integration      │  ← partner isolation, contract-diff, workflow via WebApplicationFactory
        ├─────────────────────────┤
        │  Unit (thin, targeted)  │  ← only where logic is pure & branchy (SearchNormalizer, status rules, validators)
        └─────────────────────────┘
```

**Layer responsibilities:**

- **Unit (grow selectively).** Add unit tests only where logic is pure and branch-heavy and cheap to isolate: `CertificateStatus` visibility rules (which statuses are public/dashboard/external), date/expiration logic, cross-field validators, `SearchNormalizer` (done). **Do not** chase controller unit tests — cover those at integration level where they're realistic.
- **API / integration (new — biggest ROI).** A `WebApplicationFactory`-based in-memory host that exercises real controllers + real EF query scoping against a disposable/seeded DB. This is where partner-isolation and RBAC assertions become *fast and deterministic* instead of relying on a live browser. Also the natural home for the Amazon contract-diff as a test, not just a deploy-gate script.
- **E2E journeys (extend existing framework).** Keep `_lib/smoke-template.js` as the engine. Add the missing journey specs (§6). This stays the primary business-readable regression signal.
- **Manual / UAT.** Reserved for new-requirement discovery and things automation can't reach (real email delivery, Azure AD B2C IdP round-trip, Duo Push if built, visual/subjective UX). Documented in `qc-manual-tests-*.md`.

---

## 5. Regression Suite Definition & Tiers

A **regression suite** here = the set that must be GREEN before any Plesk deploy. Tiered so we can run a fast gate on every change and a full pass before major releases.

| Tier | Name | Contents | When it runs | Gate? |
|:--:|---|---|---|---|
| **T0** | **Deploy sanity** | `smoke.js` against prod (auth gate, renders, CSS freshness, no 500s) | Immediately post-deploy | Rollback trigger |
| **T1** | **View regression** | `_views/run-all.js` — 10 views × 4 roles × 3 viewports (`SMOKE_BATCH_RESTART=1`) | Pre-deploy, every batch | **Hard gate** (deterministic FAIL blocks) |
| **T2** | **State & isolation matrix** | `cert-state-matrix.js` + promoted partner-isolation specs (from `cr171-*` / `def236-idor` one-offs) | Pre-deploy when certs/scoping/workflow touched; always before major UAT | **Hard gate** on scoping FAIL |
| **T3** | **CRUD round-trips** ✅ `_crud/run-all.js` | create→edit→delete for cert/user/product/company/tag against local dev DB (fixtures-only, SQL-verified) | Pre-deploy when write paths touched | **Hard gate** (Gate 8) |
| **T4** | **API / contract** | Amazon contract-diff (`amazon-contract-diff.js`, Gate 4) + partner API read/write/bulk + RBAC denial | Pre-deploy when API/models touched | **Hard gate** on contract change |
| **T5** | **Unit + integration** | `dotnet test` (SearchNormalizer + new status/validation units + WebApplicationFactory integration) | Every build / CI | Gate on RED |
| **T6** | **Full pass** | T1–T5 + detail views + import/export + auth + admin + document | Before major release / quarterly | Sign-off |

**One-command deterministic gate:** `node Scripts/smoke-prod/deploy-gate.js` (app on `localhost:5101` + `sqlcmd`) runs the three script suites — **T2/T4 security** (`_security/run-all.js`), **T3 CRUD** (`_crud/run-all.js`), and the **T4 Amazon contract diff** — and exits `0` only if all pass (`2` = preflight not met, `1` = a gate failed). It's the mechanical backbone of pre-deploy Gates 3/4/8 and the intended Phase-5 CI entrypoint. Validated 2026-07-08: security 55P + crud 39P + contract UNCHANGED → GREEN in ~75s.

**Deterministic-failure rule (already learned — keep it):** T1/T2 flakes on known cold-start surfaces (DEF-234 EF query-compile, DEF-235 sustained-load) do **not** gate. Run twice; only a FAIL that **reproduces on the second run** blocks a deploy. Use `SMOKE_BATCH_RESTART=1` for the clean signal.

---

## 6. End-to-End Scenario Catalog (Coverage Targets)

The dimensions that define "full E2E coverage" for this app: **Role × View × Workflow-State × Journey.**

### 6.1 Roles (4 personas + partner)
Public (anonymous) · External (company/partner umbrella) · Analyst · Admin · Partner (API-only, e.g. Amazon). Source of truth: `[Authorize]` attributes, mirrored in `e2e-smoke-plan.md` access matrix.

### 6.2 Priority journeys to add (mapped to gap-report C-items)

| Journey | Persona(s) | Asserts | Gap ref | Priority |
|---|---|---|---|:--:|
| **Cert create round-trip** — fill all sections (incl. Certification Results CR-001, partner multi-select, product IDs), upload PDF, Save → appears in internal tile → correct number shown (not ID) | Analyst/Admin | Persistence, cert# display, PDF stored, product-ID link | C1 | **P1** |
| **Cert edit round-trip** — open by cert#, change fields, replace PDF, Save → changes persist; cert# stays readonly (DEF-225 product re-select) | Analyst/Admin | Update persists, no dup product, PDF replace | C1 | **P1** |
| **Cert delete** — delete (Admin-only) → removed from views; soft-delete behavior per DEF-288 decision | Admin | Removal + gating; External/Analyst denied | C1 | **P1** |
| **Detail-view field scoping** — internal vs external vs public single-cert; internal tags hidden from External; partner co-tenants never shown | Public/External/Analyst/Admin | Field-by-field visibility per role | C2 | **P0** |
| **Partner isolation, full surface** — list + detail + single + download + Partner API, each scoped to requester; multi-partner cert visible only to owning collection | External/Partner | No cross-company/partner leak on **every** endpoint | C9 | **P0** |
| **Filter/sort correctness** — applying a filter actually narrows results; each of 7 sort modes orders correctly | All | Result-set changes, not just control presence | C3 | **P2** |
| **Import→preview→apply→undo** — CSV upload, preview modal, apply, then CR-023 undo; DEF-292 company-collision guard rejects | Analyst/Admin | Rows created/reverted; bad import blocked | C4 | **P1** |
| **Auth — 2FA setup + challenge** — enable TOTP, log out, re-login with code; lockout after N fails | All | Enrollment + challenge + lockout | C5 | **P2** |
| **Auth — forgot/reset** — request → (mock email side-effect asserted, not sent) → reset via token | All | Token flow; **blocked in prod by DEF-263** — test on local mock | C5 | **P2** |
| **Document lifecycle** — upload (type/desc/public flag) → download (role-gated) → delete (Admin) | Analyst/Admin/External | File round-trip + download gating | C6 | **P2** |
| **Admin CRUD (per view)** — tags, cert types (hierarchy Level+ParentId), partners, product types, custom fields, roles: add→edit→delete | Admin | CRUD persists; 12+ admin views | C7 | **P3** |
| **API layer** — partner write/bulk (≤100, 207 MultiStatus), v1 Products, internal certs API, auth/refresh | Partner/Admin | Contract + auth + bulk semantics | C8 | **P1/P3** |

### 6.3 Workflow-state matrix (keep + extend)
8 states (`Models/CertificateStatus.cs`) × 3 tile views × role. Already GREEN in `cert-state-matrix.js` (27 cells). **Extend** to detail views and single-cert view, and add the expired-but-not-yet-swept edge (past `ExpirationDate` hidden from public/external even before the per-minute sweep flips status).

---

## 7. Manual Testing Recommendations

Automation can't or shouldn't reach these — keep them as a documented manual checklist (extend `qc-manual-tests-2026-05-01.md`):

- **Real email/SMTP delivery** — assert the *call* in automation; verify actual receipt manually (and note DEF-263 blocks prod).
- **Azure AD B2C external IdP round-trip** — needs a live tenant; out of automated scope.
- **Duo Push MFA** — not built (CR-010); manual only if/when elected.
- **Visual/subjective UX & design-guide conformance** — mobile feel, spacing, contrast beyond the automated horizontal-scroll/geometry checks (use the `ux-design-compliance` / `accessibility-validator` agents for structured passes).
- **Client UAT / new-requirement discovery** — Carly's exploratory testing. Automation catches *regressions*, not *undiscovered requirements* (`e2e-smoke-plan.md`).
- **Migration dry-runs** — source XLSX/XLSM validation is scripted (`2026-06-22-migration-source-validation.py`) but the cutover decision is human-reviewed.

---

## 8. Automation & CI Recommendations

**The biggest process win available: add CI.** Today nothing runs automatically.

1. **Stand up CI (GitHub Actions or equivalent).** ✅ **SHIPPED 2026-07-08** — **`.github/workflows/regression-ci.yml` at the REPO ROOT** (`AnukiAI/GCCWebApp`; the git root is the parent of the app dir, and the pre-existing `ci-cd-pipeline.yml` is dormant template cruft — targets `main/develop`+Azure, runs the Playwright suite as "unit tests"). Two jobs, targeting `master`: **`build-test`** (every push/PR — builds the solution; **blocks** on the clean `GCCWebApp.Tests` (24/24); runs `GCCWebApp.Api.Tests` **non-blocking** because it has **61 pre-existing failures** to triage; deliberately excludes the Playwright `RoleBasedUI.Tests` which needs a live app — 91 fail / 29 min under plain `dotnet test`) and **`deploy-gate`** (deterministic E2E via `deploy-gate.js` on a throwaway SQL Server service; `DEPLOY_GATE_SKIP_CONTRACT=1` so contract stays local; `workflow_dispatch`). **CI seed:** `Scripts/ci/seed-e2e.sql` provisions the 5 partner users + partners/companies/products/certs the security & cert-detail gates need (SeedData seeds login users only; migrations seed no reference data). Self-sufficient + idempotent; **schema-validated** (applies clean, exact counts, rolls back leaving dev untouched). **To finish:** dispatch `deploy-gate` once to confirm GREEN behaviorally, then flip its `if:` to run on `master` push + mark it a required check. Also **triage the 61 Api.Tests failures** to promote that step to blocking.
2. **Build the integration layer (`WebApplicationFactory`).** Highest-ROI new automation: fast, deterministic partner-isolation + RBAC + workflow assertions without a browser. Retire the flaky-browser dependency for security-critical checks.
3. **Promote high-value one-offs into the maintained suite.** Convert `cr171-*` (isolation), `def236-idor` (IDOR), `def300-import-overwrite`, `def292` (import guard), and the contract-diff into named, re-run regression specs. Stop letting proven bugs go stale.
4. **Add the CRUD round-trip suite (T3)** against a disposable local DB — this requires the environment decision in §9.
5. **Keep the deploy-gate discipline** already codified in `pre-deploy-agent-checklist.md` (Gates 0–7). Wire the tiers above into those gates explicitly.
6. **Consolidate/remove dev scaffolds** shipped to prod (gap-report G11) — every removed mock route is one less thing to test and one less attack surface.

---

## 9. Test Data & Environment Strategy — **Decision Required**

This is the fork that `gap-report.md` §5 already parked on your approval, and it gates §6's write-path journeys.

- **The problem:** You chose **prod read-only** for exploration. But the #1 coverage gap (**C1 CRUD round-trips**) and most of §6's P1 journeys **cannot be tested read-only** — they require writing and cleaning up data.
- **Options:**
  - **(A) Prod read-only only.** Cover read/detail/filter/isolation/API-read gaps. CRUD stays UAT-manual. Lowest setup, biggest blind spot (writes untested).
  - **(B) Local write-path pass** on `localhost:5101` + a disposable/seeded DB (per `feedback_testing_environment` — default localhost, never live). Use the `ZZ_SmokeFixture_*` convention already established for state fixtures; seed→assert→teardown. Covers CRUD, import, auth-mock. **Recommended** — it's the only way to close C1.
  - **(C) Hybrid (recommended target).** Read-only prod for T0/T2-read/isolation confidence + local write-path (B) for T3 CRUD and auth. Best coverage; two environments to maintain.

**Data-change guardrail:** any test that writes to **real business records** still needs the CR process (`feedback_data_changes_require_cr`). Fixtures under `ZZ_SmokeFixture_*` on the pre-launch test env are free to create/destroy (`feedback_prelaunch_test_env`).

---

## 10. Entry / Exit Criteria & Deploy Gate

**Entry (a build is ready for regression):** `dotnet build` clean · migrations applied to test DB · test accounts seeded (admin/analyst/external) · target env reachable.

**Exit (a release may deploy):**
1. T5 unit/integration **GREEN** (0 RED).
2. T1 view regression **0 deterministic FAIL** (flake rule §5).
3. T2 state + isolation matrix **0 FAIL** (any scoping FAIL is a **hard block**).
4. T4 Amazon contract-diff prints **CONTRACT UNCHANGED** (or partner sign-off attached).
5. Every new/changed behavior has a DEF-XXX or CR entry; `shipped-2026-MM.md` updated.
6. Pre-deploy checklist Gates 0–7 satisfied, incl. Gate 7 (prod migration SQL run in same step).
7. T0 prod smoke GREEN **after** deploy, else rollback.

**Deploy-gate posture** (per `e2e-smoke-plan.md` open question): for a 1-developer, infrequent-deploy project — **on-demand + pre-deploy gate**, not a blocking nightly. Scoping/contract FAILs are non-negotiable blocks; cosmetic/flake FAILs are advisory.

---

## 11. Defect Management Linkage

Every failure found by any tier flows into the existing system — do not invent a parallel tracker:

- **Code defect** → `DEF-XXX` in `docs/tracking/defect-register.md`, citing the QC log ID + reporter (`feedback_def_entries_note_qc_id`). Current high-water ~DEF-236/300-range.
- **Correct code + broken historical data** → **data-ops, not a DEF** (`feedback_data_issue_vs_defect`) — e.g. the cert-26-0025 product-list anomaly (CR-019), which the smoke suite correctly classifies as WARN.
- **New requirement surfaced during testing** → CR in `change-request-register.md`, not a defect (`project_qc_log_pattern` — most "QC volume" is new requirements, not bugs).
- **Shipped fix** → append to `docs/tracking/shipped-2026-MM.md` (client receipts).

---

## 12. Phased Rollout Plan (Effort-Aware)

Cost is a standing constraint. Sequenced so each phase delivers standalone value and stops cleanly.

| Phase | Deliverable | Depends on | Rough effort |
|:--:|---|---|---|
| **0** ✅ | Discovery — coverage matrix + gap report | done (2026-07-02) | done |
| **1** ✅ | **SHIPPED 2026-07-07** — maintained security suite `Scripts/smoke-prod/_security/` (partner-isolation, rbac-matrix, idor-download + harness + run-all). Baseline **GREEN 45P / 0F / 2W** on localhost. Surfaced + corrected one wrong test assumption (External *does* see Product/Management read-only per `ExternalAccess` policy — app is correct). | done | ~0.5 day |
| **2** ✅ | **SHIPPED 2026-07-07** — `_security/cert-detail-scoping.js` (10 assertions, runtime-discovered fixtures). Proves external can't reach another company's cert detail (404), internal tags never render externally, anonymous status-gate holds. Suite now **GREEN 55P / 0F / 2W**. No detail-scoping defect found. | Phase 1 | ~1 day |
| **3** ✅ | **SHIPPED 2026-07-08** — full `_crud/` write-path suite: **cert + tag + company + product + user** create→edit→delete, verified in SQL, fixtures-only, self-cleaning. **GREEN 39P / 0F**, zero fixture residue (all tables at baseline). Run: `node Scripts/smoke-prod/_crud/run-all.js`. | Env = C | ~1.5 days |
| **4** | `WebApplicationFactory` integration layer (RBAC/workflow/scoping fast tests) + targeted status/validation units | — | ~1.5 days |
| **5** 🟡 | **2026-07-08** — repo-root `.github/workflows/regression-ci.yml` (`master`): `build-test` (blocks on GCCWebApp.Tests 24/24; Api.Tests non-blocking pending 61-failure triage; excludes Playwright suite) + `deploy-gate` (E2E via `deploy-gate.js` on a SQL Server service, `workflow_dispatch`). Harness CI-portable (`SMOKE_DB_CONN`). CI seed `Scripts/ci/seed-e2e.sql` authored + schema-validated. **Remaining:** dispatch deploy-gate once → confirm behavioral GREEN → make it a required push gate; triage 61 Api.Tests failures. | Phases 3–4 | ~1 day |
| **6** | Import/export, auth (2FA/reset-mock), document, admin CRUD, filter/sort correctness | Phases 2–3 | ~2 days |

**Recommended immediate next step:** approve §9 (env) and Phase 1–2 (close the two P0 gaps: detail-view scoping + full partner isolation). That buys the most risk reduction for ~1.5 days before touching the larger CRUD/CI build-out.

---

## Decisions — RESOLVED 2026-07-07

1. **Environment for write-path coverage (§9)** → **(C) Hybrid.** Read-only prod for isolation/API confidence + local `localhost:5101` with disposable `ZZ_SmokeFixture_*` data for CRUD/auth.
2. **Scope of undocumented subsystems (§3)** → **Decide later.** Focus test effort on documented, in-use features first; ETL/Metrics/bulk-write + dev scaffolds revisited separately (no test code written for them yet).
3. **Domain exclusion** → **Confirmed** — BarTender/purchasing/invoicing dropped; not part of this app.
4. **Execution order** → **P0 gaps first (Phase 1 → 2):** promote isolation/IDOR/import one-offs into a maintained suite, then build detail-view + full partner-isolation E2E (gaps C2, C9).

**Active work:** Phase 1 → 2 per §12.

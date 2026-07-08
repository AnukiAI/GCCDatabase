# GCC Web App — Doc ↔ Code ↔ Test Gap Report (Phase 0)

**Date:** 2026-07-02 · **Author:** QA automation pass (Phase 0 discovery)
**Method:** Static cross-reference of four inventories — controllers/routes, views + client actions,
documentation/requirements, and existing test coverage. **No source code was modified.**
**Not yet done (awaiting approval):** Phase 1 live read-only prod walkthrough, Phase 2 spec authoring.
See `coverage-matrix.md` for the row-by-row basis of every claim below.

---

## 1. Doc vs. Code Gaps

### 1a. Documented / expected but NOT built (or blocked) — 🟥

| # | Feature | Doc source | Reality (code) | Severity | Note |
|---|---------|-----------|----------------|:---:|------|
| G1 | **Forgot-Password email delivery** | `change-request-register.md` CR-009 ("confirmation, not a build"); QC #150 | App-native reset flow is fully built, but **prod falls back to MockEmailService (SMTP unconfigured)** — no email is actually sent in production. Reset only works via manual admin edit. | **Defect** | Aligns with memory note `security-plaintext-gmail-credential` / DEF-263. Blocks any UAT of the forgot-password journey. |
| G2 | **Duo Push MFA** | CR-010 (approved 2026-04-29) | Not built; TOTP app-native 2FA works. Optional per client choice. | Observation | Only build if client elects Duo. |
| G3 | **Custom Field CSV export** | CR-012 (approved) | Not built. Custom fields already render on table view (CR-007). | Enhancement | Straightforward add. |
| G4 | **Custom Field sort/filter** | CR-014 (approved) | Not built; depends on G3 surface. | Enhancement | |
| G5 | **Partner Role creation UI** | CR-008 (approved) | Partner role data model exists; no admin UI/wizard to create partner-role users. | Observation | Confirm roadmap vs. expectation. |
| G6 | **Partner custom-fields exposure** | CR-013 (approved w/ gate) | Not built — **gated on Amazon contract sign-off** (contract immutable). | Observation | Correctly parked. |

### 1b. Built but NOT documented — 🟦

| # | Feature (code) | Where | Severity | Note |
|---|----------------|-------|:---:|------|
| G7 | **ETLController** — 11 endpoints (RunFull/IncrementalETL, jobs, alerts, extract, export, metrics, dashboard, validate-config) | `Controllers/Api/ETLController.cs` | **Observation (elevated)** | An entire ETL subsystem appears in code but in **none** of the reviewed requirement/spec docs. Confirm whether it's in scope, dead code, or an undocumented integration. Impacts audit surface + security review. |
| G8 | **Shareable-link endpoints** on all 3 tile views (`.../share-link`) | 3 tile API controllers | Observation | Feature flagged "complete" in `.claude/CLAUDE.md` ("Shareable URLs") but absent from requirement docs. Doc-side gap only. |
| G9 | **API v1 Metrics controller** (get/summary/reset/record) | `Api/v1/MetricsController.cs` | Observation | Undocumented. |
| G10 | **Bulk certificate write endpoints** (create/update/delete/validate, ≤100, 207 MultiStatus) | `Api/v1/BulkCertificatesController.cs` | Observation | Read side documented; bulk-write side thinly/not documented. Confirm against Amazon contract immutability. |
| G11 | **Dev scaffolds shipped in the app** | see list | **Observation** | `Certificate/Create2` + Create Alt, `Product/ManagementV2` + `ManagementAlt`, `Admin/CreateUser` (pure mock UI, no backend), `Account/TestAuth`, `Test/ProductTypes`. These are routable in the deployed app. Recommend gating to Development-only or removing (reduces attack/confusion surface). Not applied — logged only. |

### 1c. Behavior that contradicts the docs

| # | Contradiction | Doc says | Code says | Severity | Note |
|---|---------------|----------|-----------|:---:|------|
| G12 | **Dashboard "active" statuses** | `requirements_analysis_wireframes_7_28_25.md` counts 3 active statuses (Paid, Republished, **Confidential**) | `Models/CertificateStatus.cs` `DashboardStatuses` = 2 (Paid, Republished); Confidential excluded per DEF-223 | Observation | Code is intended current state; **change was tracked as a DEF, not a CR** — docs never updated. Recommend a doc note. |
| G13 | **Role count** | wireframes doc: "5 roles" (incl. Public User); `user-management.md`/CR register: "4 roles" | Code has 4 credentialed roles; Public = anonymous access mode, not an account | Observation (resolved) | No functional gap; consistent with memory `feedback_external_user_umbrella`. Doc wording only. |
| G14 | **Workflow state count** | Historical docs mention 14; current docs 8 | `CertificateStatus.cs` = 8 | None (resolved) | Matches memory `feedback_workflow_state_count`. Cited only to close the loop. |

---

## 2. Coverage Gaps (views/actions with no automated coverage)

Existing automation = **12 per-view smoke specs** (`_views/`) at *presence/role/viewport* level, **1 state-visibility
matrix**, ~30 defect/CR one-off probes, and **1 .NET unit class** (`SearchNormalizerTests`, 11 tests). What that leaves:

| # | Gap | Why it matters | Suggested Phase 2 target |
|---|-----|----------------|--------------------------|
| C1 | **No CRUD round-trips** — cert/user/product/company/tag create→edit→delete | Specs assert "form loads," never that a save persists or a delete removes. This is the core business flow. | Highest priority. Read-only prod can't cover writes — needs local disposable DB (revisit env decision for Phase 2). |
| C2 | **Detail views untested** (internal/external/public single cert) | Field-visibility-by-role is a top client concern (partner isolation) yet no detail-page spec exists. | `cert-detail-{role}.js` |
| C3 | **Filter application & sort order** | Presence asserted; correctness (does filtering/sorting actually change results?) is not. | Extend tile specs with result assertions |
| C4 | **Import/Export end-to-end** | Only ad-hoc `def-*` one-offs; CSV upload→preview→apply→undo not systematized. | `import-cert.js`, `export-*.js` |
| C5 | **Auth flows** — 2FA setup/challenge, forgot/reset password, signup, lockout | Zero coverage; security-critical. G1 (email) blocks part of this on prod. | `auth-2fa.js`, `auth-reset.js` (mock email side-effect) |
| C6 | **Document upload/download/delete** | No coverage of file lifecycle or role gating on download. | `document.js` |
| C7 | **Admin pages (12+)** — Partners, CertificateTypes, RoleManagement, CertificateFields, ProductTypes, Pathways, CSFAttributes, GoogleAnalytics, GCCAPI, PartnerAPI | Only user-mgmt + tags covered. | One spec per admin view |
| C8 | **API layer** — partner write/bulk, v1 Products, Metrics, ETL, internal certs API | Partner *read* has one-offs; write/bulk/ETL untested. ETL entirely untested + undocumented (G7). | API contract specs (stub partner side) |
| C9 | **Partner data isolation at API level** | Memory flags this as high-risk (QC #171, DEF-103/164/198/204/205). One-offs exist but not a durable gate. | Promote `cr171-*` one-offs into Tier-1 |
| C10 | **No integration tests in .NET** | Only 1 unit class; no controller/DB/workflow integration tests. | Optional: `WebApplicationFactory` integration suite |

**Not testable / manual-only (with reason):**
- **Real email/SMTP delivery** — side-effect must be stubbed; assert the call, don't send (also blocked by G1).
- **BarTender label printing** — *not found in this codebase* (see §3, D-note). The prompt's BarTender/purchasing/receiving/invoicing workflows do not appear to exist here.
- **Azure AD B2C external IdP round-trip** — out of scope per e2e-smoke-plan; needs live tenant.
- **Duo Push** — not built (G2).

---

## 3. Defects & Observations Found (static pass — not yet reproduced live)

> These come from cross-referencing only; Phase 1 live walkthrough (read-only prod) is not yet run, so
> nothing here is confirmed against a running instance except where an existing DEF already tracks it.

| ID | Finding | Severity | Suspected file / source | Repro (to confirm in Phase 1) |
|----|---------|:---:|-------------------------|-------------------------------|
| D1 | Forgot-password sends no email in prod (Mock fallback) | **Defect** | `Services/*Email*`, `appsettings.json` (SMTP) — ties to DEF-263 | Trigger `/Account/ForgotPassword` on prod → no email arrives |
| D2 | Dev/mock routes reachable in deployed app | **Observation** | `Create2`, `CreateUser` (mock), `TestAuth`, `Test/ProductTypes`, `ManagementV2/Alt` | Navigate each route while authenticated; confirm reachable |
| D3 | ETL subsystem undocumented — scope/ownership unknown | **Observation** | `Api/ETLController.cs` (Admin+SuperAdmin) | Confirm intended; security-review the surface |
| D4 | Docs stale on Dashboard active-status definition (G12) | **Observation** | `requirements_analysis_wireframes_7_28_25.md` vs `CertificateStatus.cs` | Doc-only fix |
| D5 | Duplicate/parallel management UIs (Product V2/Alt, Company Index/Management) | **Observation** | `Views/Product/*`, `Views/Company/*` | Consolidation candidate; confirm which is canonical |
| D6 | **Prompt-vs-app domain mismatch** | **Observation** | — | The audit prompt asks to cover *label printing (BarTender), purchasing/receiving, invoicing/AR*. **None of these exist in GCCWebApp** (it's a certificate database, not an inventory/ERP app). Confirm the prompt wasn't intended for a different repo, or drop those workflows from scope. |

*No new code defects are asserted as confirmed — Phase 0 is static. D1/D3 have existing memory/DEF backing.*

---

## 4. Test Results

**No suite was executed in this pass** (Phase 0 = discovery, staged agreement). For reference, the existing
`Scripts/smoke-prod/` Tier-1 baseline was last reported **GREEN (~694 PASS / 0 FAIL / 6 WARN)** per
`docs/tracking/e2e-smoke-plan.md`; `GCCWebApp.Tests` = 11 passing unit tests. Neither was re-run here.

A results summary will be populated when Phase 2 specs are authored and run.

---

## 5. Recommended Next Step (for approval)

Phase 0 is complete. Before writing any test code, two decisions from you:

1. **Env for write-path coverage.** You chose *prod read-only* for exploration — but the biggest coverage gap
   (C1, CRUD round-trips) **cannot** be tested read-only. To cover writes we need `localhost:5101` + a
   disposable DB. Options: (a) keep prod read-only, cover *read/detail/filter* gaps only; (b) add a local
   write-path pass for CRUD. 
2. **Scope of the BarTender/purchasing/invoicing workflows** (D6) — these don't exist here; confirm we drop them.

On approval I'll extend `Scripts/smoke-prod/_views/` with the highest-value specs first (detail views C2,
auth C5, admin pages C7, partner-isolation C9), reusing `_lib/smoke-template.js` — no parallel `/e2e` tree.

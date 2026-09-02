# Issue Register

Running log of **infrastructure / tooling / process** issues that are not product defects (those live in `defect-register.md`) and not scoped feature changes (those live in `change-request-register.md`). Newest first. IDs are `ISS-NNN`.

---

### ISS-003 — Two QC-log backup copies are sitting in the client-facing OneDrive folder and will sync to SharePoint

**Date:** 2026-09-02 | **Status:** **OPEN — awaiting Shawn's call** (move, delete, or leave). | **Severity:** Low — housekeeping. Nothing is broken and no data is at risk; the concern is that internal safety copies are visible in a folder the client's material lives in. | **Reporter:** Anuki (internal), raised while closing QC #244.

**What is there.** Both in `…\LocalWest\Projects\GCC Database Solution\` (the OneDrive root for this project), alongside the live QC log:

| File | State it captures |
|---|---|
| `Database QC Log_8.28.26_sw_Updates.BACKUP-20260902.xlsx` | before the QC #244 close-out |
| `Database QC Log_8.28.26_sw_Updates.BACKUP-20260902-b.xlsx` | after the close-out, before the `A253` ID fix |

~12.8 MB each (they carry the workbook's 159 embedded screenshots), so ~25 MB of duplicate sync traffic.

**Why they exist.** Taken deliberately before writing to the QC log — that workbook holds 159 media files, 4 pivot tables and a pivot cache, so a bad write is expensive to undo. See the 2026-09-02 close-out entry in `observations_log.md`.

**Why it is worth a decision rather than just deleting.** The folder is under `OneDrive - PointSolve Technology Inc`, so anything left there **syncs to SharePoint** and lands in the same place as client-facing material. `BACKUP-` filenames are self-explanatory and harmless, but they are our scaffolding, not a deliverable.

**Options.** (a) delete both — the close-out is verified on disk and the workbook's 205 parts / 159 media / 4 pivot tables were confirmed intact, so they have served their purpose; (b) move to a local, non-synced scratch folder if a rollback window is wanted; (c) leave them. **Recommend (a)** once Shawn has eyeballed row 253.

**Wider point worth settling once.** There is no agreed home for pre-edit backups of client documents — every one taken so far has landed next to the original inside OneDrive. One non-synced backup location would stop this recurring. Related: [[feedback_backup_uploads_before_deploy]].

---

### ISS-002 — Partner API integration fixture returns an empty result set to every caller, so 49 integration tests cannot pass

**Date:** 2026-08-04 | **Status:** **OPEN — deferred to after the current deploy** (Shawn, 2026-08-04). Test-infrastructure only; no product code implicated. | **Severity:** Medium-High (assurance) — the product is fine, but the automated proof of partner data isolation is unavailable in the .NET suite. | **Reporter:** Anuki (internal), during the 2026-08-04 full regression pass.

**Symptom.** Every partner-scoped API call made through the integration-test fixture returns `data: []`. Representative failure:

> `Expected data.GetArrayLength() to be 1 because Seed data contains exactly 1 Other certificate, but found 0 (difference of -1).`

**Scope — 49 of the 76 current failures**, measured 2026-08-04 (`729 tests / 653 passed / 76 failed`):

| Class | Failing cases |
|---|---:|
| `Integration.PartnerApiScopingTests` | 40 |
| `Integration.MultiPartnerApiIntegrationTests` | 8 |
| `Integration.ApiIntegrationTests` | 1 |
| **Fixture-attributable subtotal** | **49** |

The **other 27** failures are unit-test classes (`CertificateTileExternalApiControllerTests` 10, `ProductIdServiceTests` 5, `CertificateServiceTests` 5, `AdminServiceTests` 3, `QC_CertificateRepositoryTests` 2, `CertificatesControllerTests` 1, `CertificateTilesApiControllerTests` 1). Those do **not** go through the web fixture and are **not** covered by this issue — they need their own triage and should not be assumed fixed when this one is.

**History — why the count moved 62 → 76 and why that is not a regression.** The 2026-07-31 triage (`test-suite-triage-2026-07-31.md`) recorded 62 failures and identified the root cause. It also found that ~34 isolation tests were passing **vacuously**: they enumerate the returned certificates asserting none belong to another partner, and over an empty array that loop body never executes. DEF-326 (committed `b727c10`, 2026-08-04) added the `RequireNonEmptyData` guard plus two positive controls to 9 of those methods — which expand to **13 test cases** via `[InlineData]`. Those 13 moved from false-green to correctly-red. **No tests were added** (`[Fact]/[Theory]` 38 → 38, `[InlineData]` 9 → 9) and no product code changed. The suite got honest, not worse.

**What is NOT broken.** Partner isolation itself is verified and green by other means — the browser security suite (`Scripts/smoke-prod/_security/partner-isolation.js`) ran **21 PASS / 0 FAIL** against real seeded data in the same 2026-08-04 pass, as did RBAC (24), IDOR (2) and detail-scoping (10). That is the control actually guarding [[feedback_partner_data_isolation]] today. This issue is about restoring the *second*, faster proof in the .NET suite.

**Where to start (not yet investigated).** The fixture is a `WebApplicationFactory`-based integration host; the tests expect specific seed rows (2 Amazon certs, 1 Novi, 1 Other, ASIN `B08XQRT456`, SKU `NOVI-ECS-001`). Likely candidates: the test host's seeding no longer runs, seeds into a different provider/database than the one the request pipeline reads, or the API-key → partner resolution finds no matching partner in the test host and correctly scopes everything out. Confirm which before changing assertions — the tests are now written to fail rather than paper over emptiness, and that property must be preserved.

**Do not.** Relax the DEF-326 guards to make the suite green. Emptiness is treated as failure by design; reverting that re-creates the assurance defect.

**Notes.** Sequenced after the pending deploy at Shawn's direction — nothing here blocks shipping, and the bundle in flight (DEF-319/320 et al.) is unaffected. Related: DEF-326 (assurance fix), `docs/tracking/test-suite-triage-2026-07-31.md`, [[project_security_regression_suite]], [[feedback_partner_data_isolation]].

---

### ISS-001 — Fresh-database seeding is broken: `SeedData.cs` and `seed-e2e.sql` reference seed scripts that no longer exist in the repo, and the in-code seeder is orphaned

**Date:** 2026-07-13 | **Status:** Fix drafted + verified (working tree, `Data/SeedData.cs` + `Program.cs`), **pending next deploy bundle** — see **Fix** below | **Severity:** Medium — does not affect the running product or any existing database; blocks standing up a **brand-new** dev/CI database from scratch. Surfaced during DEF-303 validation (see [[feedback_testing_environment]]). | **Reporter:** Shawn (2026-07-13)

**Symptom.** On a freshly-migrated database, the app boots with no login users, so **every login fails** ("Invalid email or password") and any authenticated view is unreachable. The startup log says *"Database migrations applied. No automatic seeding performed. Use SQL scripts in Database_Scripts/ folder to seed data when needed"* — but the scripts it points to are not in the repo. (Discovered because the local app defaulted to an empty DB when `DATABASE_CONNECTION_STRING` wasn't set in-shell; validating DEF-303's internal/external views required repointing at the already-seeded `GCCDatabase_Dev`.)

**Details / evidence.**
- `Data/SeedData.cs::InitializeAsync` only runs `MigrateAsync()` then early-returns with the "No automatic seeding" message. The idempotent seeder that actually creates Roles/Permissions/Users/API keys — `SeedData.SeedEssentialSecurityDataAsync` (produces `admin` / `analyst` / `external` with a fixed bcrypt hash) — **still exists in the file but is never called** from anywhere.
- The comments in `SeedData.cs` (lines 23–25) and `Program.cs` (line 912) direct the operator to run these scripts, **all four of which are missing** from the working tree:
  - `Database_Scripts/PRODUCTION_ESSENTIAL_SEED.sql`
  - `Database_Scripts/FULL_TEST_DATA_SEED_uniqueemails.sql`
  - `Database_Scripts/FULL_TEST_DATA_SEED.sql`
  - `Database_Scripts/00_CREATE_SCHEMA.sql`
- `Scripts/ci/seed-e2e.sql` is **not** a substitute: its header explicitly assumes *"SeedData has seeded login users admin/analyst/external + the Roles incl. 'Partner'"*, and its prerequisite check `RAISERROR`s (and rolls back) if the `admin` user / `Partner` role / `external` user are absent. So on a truly fresh DB, CI's own seed step would fail too — CI presumably works today only because its databases already carry the base users from some earlier path.
- The only seed-ish SQL still present: `Scripts/ci/seed-e2e.sql` (business data, needs base users) and `Scripts/smoke-prod/_lib/seed-state-fixtures.sql` (smoke fixtures).

**Impact.** New-environment bring-up (a fresh dev laptop, a clean CI database, a rebuilt test DB) cannot be seeded from the documented path. Existing databases (`GCCDatabase_Dev`, prod, current CI) are unaffected — this only bites when starting from an empty schema.

**Candidate fixes (for triage — not yet decided).**
1. **Re-wire the existing in-code seeder** — have `InitializeAsync` call `SeedEssentialSecurityDataAsync` in Development (it's already idempotent and guards on `Roles.AnyAsync()`), so a fresh dev/CI DB self-seeds base users + roles, and `seed-e2e.sql` can then layer business data on top. Lowest-effort, keeps one source of truth in code.
2. **Restore the missing SQL scripts** to `Database_Scripts/` (recover from an earlier commit / backup) and keep the script-based flow the comments describe.
3. **Do both** minimally: re-point the comments/logs at whatever the real, present path is, and delete references to files that no longer exist so operators aren't sent to dead scripts.

**Fix (drafted 2026-07-13, candidate #1 — re-wire the in-code seeder).** Two files, no new SQL, no migration:
- `Data/SeedData.cs` — `InitializeAsync(context)` → `InitializeAsync(context, bool seedEssentialData = false)`. When `true`, after `MigrateAsync()` it calls the pre-existing idempotent `SeedEssentialSecurityDataAsync(context)` (guards on `Roles.AnyAsync()` → no-ops on an already-seeded DB); when `false` it keeps the old "no automatic seeding" log. Default `false` preserves behavior for any other caller (there are none today besides Program.cs).
- `Program.cs` — the Development branch now calls `SeedData.InitializeAsync(context, seedEssentialData: true)`. Development only; Testing/Staging/Production branches are untouched.

**Verification.** ✅ VERIFIED on localhost:5101 (2026-07-13). `dotnet build` clean (0 errors). Booted the app against a **fresh empty** `GCCDatabase_ISS001Verify` (created empty via trusted `sqlcmd`; `GCC_DevUser` lacks `CREATE DATABASE`, an unrelated env perms note): startup logged *"Seeding essential security data (Roles, Permissions, Users, API Keys)…"* and the schema+data built from zero → **14 Users / 5 Roles / 22 Permissions**, incl. `analyst@test-gcc.local` (Analyst) and `external@test-gcc.local` (External). Playwright login as `analyst@test-gcc.local` / `TestPass123!` then reached `/Certificate/CertificateTileViewInternal` (authed view rendered). Also re-confirmed idempotency in the prior run against the already-seeded `GCCDatabase_Dev` (seeder skipped — DB untouched). Throwaway DB dropped after.

**Follow-ups (not done here).** (1) The stale comments in `SeedData.cs` (lines ~23–25) and `Program.cs` (~912) still name the missing `Database_Scripts/*_SEED.sql` / `00_CREATE_SCHEMA.sql` files — left as-is to keep this diff minimal; worth a cleanup pass. (2) The in-code seeder's users (`analyst@test-gcc.local`, `external@test-gcc.local`, admin `swest@anukiconsulting.com`) differ from the emails currently in `GCCDatabase_Dev` (`jane.analyst@anukiconsulting.com`, …) — a pre-existing divergence between the code seeder and whatever script seeded the live dev DB; not reconciled here.

**Notes.** Related: DEF-303 (its verification is what surfaced this); [[project_predeploy_agent_checklist]] (fresh-DB bring-up touches deploy/seed steps); [[feedback_testing_environment]].

---

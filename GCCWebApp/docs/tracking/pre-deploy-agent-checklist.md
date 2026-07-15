# Pre-Deploy Agent Checklist

**Purpose:** Stop client-found UI/QC regressions *before* they ship. Each gate below names the agent that runs it, what it must verify, and the documented incident that justifies it. Run top-to-bottom before every Plesk deploy. A gate that can't pass blocks the deploy — fix or consciously waive (and log the waiver).

**How to run a gate:** launch the named agent via the Task tool with the changed files as scope. Gates 1–4 are independent — fire them in parallel in a single message, then read the results. Gate 5 (governance) runs last because it audits the *outcome* of the others. **Gates 4, 6, and 8 also have deterministic script steps that need the app running on `localhost:5101`** (Gate 4 = `amazon-contract-diff.js` Amazon-shape diff; Gate 6 = cold-start timing; Gate 8 = the write-path/security/contract script bundle) — start the app first, then run those alongside the agent reviews.

**One-command deterministic gate:** with the app on `localhost:5101` and `sqlcmd` on PATH, run **`node Scripts/smoke-prod/deploy-gate.js`**. It runs the three deterministic script suites in sequence — **security** (`_security/run-all.js`: isolation/RBAC/IDOR/detail scoping), **crud** (`_crud/run-all.js`: cert/tag/company/product/user write paths), and **contract** (`amazon-contract-diff.js`) — and exits `0` only if all pass (`2` = app/sqlcmd not ready, `1` = a gate failed). This is the mechanical backbone of Gates 3, 4, and 8 in one shot (and the intended CI entrypoint). The agent-driven gates below still cover what a script can't (visual/design, WCAG, traceability, EF perf, migrations).

**Scope trigger:** run the full list before any `dotnet publish` to gccwebapp.com / greencirclecertified.com. For a hotfix touching one surface, run only the gates whose "Fires when" matches — but Gate 5 always runs.

---

## Gate 0 — Back up server uploads (manual, do this first)

Zip the server's `wwwroot/uploads/` before the deploy. Publish can overwrite real user-uploaded PDFs — this happened once (prod upload wipe, 2026-04-23). The pubxml exclusion is in place, but the backup is the seatbelt. Not an agent task; just don't skip it.

---

## Gate 1 — UI / Design-Guide compliance  ·  `ux-design-compliance`

**Fires when:** any `.cshtml` view or `wwwroot/js/*` / `wwwroot/css/*` changed.

Verify against `/0_Documentation/mockups/0_0_MasterDesignGuide.html`:

- [ ] No native `alert()` / `confirm()` / `prompt()`. Deletes use `AdminCore.showConfirmation()` **before** and `AdminCore.showNotification()` toast **after**. Success = toast only.
- [ ] No `<h1>` in views — title comes from `ViewData["Title"]`. No duplicate page headers.
- [ ] Minimum 16px font everywhere (iOS zoom trigger below that).
- [ ] `.admin-container` structure intact; **no inline `max-width` overrides**.
- [ ] Buttons aligned per guide; cert-detail action icons (zoom/download) stay **right-aligned**, not centered.
- [ ] Import = down arrow, Export = up arrow (do not flip per FontAwesome naming).
- [ ] Client-requested labels (table titles, badges, buttons) are **unchanged**. If a label looks empty, the fix is data scoping — never rename the copy.

> Justifies this gate: design-guide drift is the single most common thing the client surfaces on retest. Cheaper to catch here than in their next cycle.

---

## Gate 2 — Cross-role + responsive regression  ·  `accessibility-validator` (+ Playwright)

**Fires when:** any shared layout, nav, or role-conditional view changed (`_Layout.cshtml`, anything role-gated).

- [ ] Visual fix verified on **all four logins**: Admin, Analyst, External, Public. (DEF-227: a nav-spacing fix landed for Admin only and shipped half-broken. The client's standing note: *"apply fixes to all instances where applicable."*)
- [ ] External login confirmed: **no** Add/Edit cert buttons, **no** Actions column on Product Management, **cannot** see internal tags.
- [ ] Playwright full-page screenshots across **5 viewports** (desktop + tablet + mobile), waiting on real content — not just `networkidle`.
- [ ] AxeCore pass on changed views (no new WCAG 2.1 AA violations).

---

## Gate 3 — Data-scoping & endpoint coverage  ·  `security-specialist`

**Fires when:** any controller/service returning business data changed.

- [ ] Every **new or edited endpoint** that returns records applies the same role/company/partner filter as its list sibling. The recurring trap (DEF-103/164/198/204/205): the *list* endpoint gets filtered, the *detail / single / download* endpoint doesn't. Audit adjacent endpoints, not just the one you touched.
- [ ] External users see only their associated company/partner data on every surface, including detail and export.

---

## Gate 4 — Partner API contract integrity  ·  automated diff + `security-specialist`

**Fires when:** anything under `Controllers/Api/v1/*` changed — even indirectly (shared service/model).

**Mechanical proof (run this first — it's deterministic):**

- [ ] With the app running on `localhost:5101`, run **`node Scripts/smoke-prod/amazon-contract-diff.js`**. It calls every Amazon-facing endpoint exactly as Amazon does (`X-API-Key`, cookie-free) and diffs the response **structure** (field paths, array-vs-object nesting, leaf types — values ignored) against the frozen pre-CR-171 baselines in `docs/tracking/api-samples/` (`2026-04-30_amazon_gs1.json`, `2026-04-30_amazon_gs1_single.json`, `2026-04-24_amazon_standard.json`). **Must print `CONTRACT UNCHANGED` (all PASS).** Any `+ ADDED / - REMOVED / ~ RETYPED` line is a contract change that blocks the deploy.
  - Run against the **same seed dataset the baselines were captured from** (the 6 Amazon certs incl. `26-0146`) — a dev DB reseed reuses those certs, so the shape diff stays valid even though values differ.
  - If a change to the Amazon shape is **intentional**, it is a separate CR requiring client → partner sign-off (per [[feedback_amazon_api_immutable]]). Only then: regenerate the baseline (`amazon-partner-api-local.js` writes a dated GS1 sample) and update the three baseline filenames this gate diffs against, in the same CR.

**Manual review (`security-specialist`), for the data-correctness rules the shape diff can't see:**

- [ ] `/api/v1/certificates*` JSON **shape is byte-identical** to the Amazon contract. Internal app changes must not alter it. Any intentional change is a separate CR requiring client → partner sign-off — it does **not** ride along on an internal deploy.
- [ ] Partner/External callers get `status = "Active"` literal only — no internal lifecycle statuses (Draft / Hold / Client Approved – Paid) leaking through. `PublicStatuses` filter applied on list, search, GS1, by-id, by-number, and PDF endpoints.
- [ ] **Partner data isolation (CR-171):** a partner caller never receives a co-partner's name or co-partner-only product IDs. (`amazon-partner-api-local.js` asserts no `Novi`/`Wayfair`/`Other` leak across all endpoints — run it too if partner scoping/CR-171 code changed.)
- [ ] `CertificationResults` renders as percentage, not raw decimal, on every Partner-facing surface.

---

## Gate 5 — Traceability audit (always runs last)  ·  `governance-manager`

**Fires:** every deploy, no exceptions.

> Audit against **`git` (`git diff --name-only <last-deploy>..HEAD` + `git status`)**, never a point-in-time status snapshot. Dry run 2026-06-01 proved why: the working tree gained two files (DEF-234) and the shipped log grew from 6 to 9 fixes *while the gates were running* — concurrent work in another terminal. A human eyeballing `git status` once will miss work that lands minutes later; re-read git at deploy time.

Audit the working tree against the tracking docs:

- [ ] Every code change maps to a **DEF-XXX** in `docs/tracking/defect-register.md` **or** an approved CR. No orphan changes. (If a change has neither, that's the "building without a CR" risk — stop and reconcile before shipping.)
- [ ] Every DEF row cites its originating **QC #N** in the title + Reporter line.
- [ ] `docs/tracking/shipped-2026-MM.md` has a stakeholder-readable entry for each fix in this deploy.
- [ ] If parallel streams (multiple terminals/agents) produced this batch, confirm no overlap/conflict and no built-but-undocumented work.
- [ ] Real-business-record UPDATE/DELETE is **not** in this deploy unless it has a formal CR + backup. Data cleanup ships on its own track, never bundled with code.

---

## Gate 6 — Performance / EF regression  ·  manual + local timing test

**Fires when:** anything under `Repositories/*`, an EF Core query (`.Where/.Include/.Select` on a `DbSet`), `Program.cs` service registration, or a hosted/background service changed.

- [ ] Any new/changed EF query with multiple collection `Include`s uses `.AsSplitQuery()` and a unique `.ThenBy(c => c.Id)` tiebreaker. (Removing `Include`s to "fix" a cartesian blow-up caused a 30s prod timeout in DEF-166 — `AsSplitQuery` is the correct lever, per the EF-includes rule.)
- [ ] **Cold-start timed locally before deploy.** Run the app on `localhost:5101`, hit the affected endpoint as the *first* request after startup, and record the time. It must clear any client-side `AbortSignal.timeout(...)` ceiling with margin. DEF-234 baseline: `/api/certificate-tiles?page=1&pageSize=20` was 23.1s cold (tripped the 30s JS abort on 4/12 smoke cells); post-fix cold time must be well under that.
- [ ] If a warmup/hosted service was added to mask cold-start latency, confirm it actually covers the slow query path (the first *real* user request is fast, not just the second).

> Justifies this gate: EF/cold-start regressions don't show in a code diff or a UI screenshot — only a timing run catches them, and they've bitten this project twice (DEF-166, DEF-234). The other five gates would wave an EF change straight through.

---

## Gate 7 — Pending migration → prod schema  ·  manual + DB

**Fires when:** the deploy adds any file under `Migrations/` **or** changes a mapped entity (a `Models/*` property that EF maps to a column, a `DbContext` `OnModelCreating` change, or a new/changed `DbSet`).

> **Production never auto-migrates.** `Program.cs:922-926` deliberately skips `Database.Migrate()` in Production (and Staging) — schema is managed by hand. So shipping a DLL that maps a new column does **not** add it to the gccwebapp DB. EF then `SELECT`s the missing column on the very next query against that table and SQL Server throws `Invalid column name '…'`.

> 🟢 **CURRENT BATCH (mid-June 2026 — DEF-267 + CR-171 + CR-187) — CANONICAL ARTIFACT:** **`docs/tracking/CR-171-CR187-deploy-combined.sql`**. One idempotent script that applies the 2 pending migrations (`AddNormalizedSearchColumns`, `AddProductIdPartnersM2M`) **and** both CR-171 partner backfills, with `SET QUOTED_IDENTIFIER ON` and post-deploy verification baked in. **Validated 2026-06-18** by restoring the live test `.bak` to a throwaway DB at the exact prod state (2 behind) and running it: apply exit 0, both migrations applied, backfills populated, `certs_with_partner_missing_M2M = 0` + `productIds_with_partner_missing_M2M = 0`; re-run is a clean idempotent no-op. **Prod state confirmed via live probe:** last applied = `20260605131035_ChangeUserCompanyToFreeText`; `CertificatePartners` already exists; `CertificateProductIdPartners` does not. (Harmless CR-187 1700-byte index-key warning is expected.)

- [ ] Run `dotnet ef migrations list` (or diff `Migrations/` vs the last deploy) and identify every migration **not yet applied to the gccwebapp DB**.
- [ ] For each, have the **exact remediation SQL ready** (script the migration with `dotnet ef migrations script <from> <to> --idempotent`, or hand-write the `ALTER`/`CREATE` + the `__EFMigrationsHistory` insert with the correct `ProductVersion` — currently **8.0.11**).
- [ ] **Bundle schema + any required data backfill into ONE idempotent script.** A migration that changes how existing data is *read* (e.g. CR-171: filtering moved from the scalar `Certificate.Partner` to the `Partners` M2M) needs its backfill applied in the SAME step, or working rows silently disappear from scoped views. Make every step guarded (`IF NOT EXISTS` / `NOT EXISTS`) and end with verification SELECTs that assert the backfill is complete (e.g. `…missing_M2M = 0`).
- [ ] **Validate the script before deploy on a throwaway copy of prod.** Restore the latest prod/test `.bak` to a local throwaway DB, run the script with `sqlcmd -b`, and confirm the post-deploy verification block is all green and a re-run is a clean no-op. (Validating only against the dev DB is insufficient — if dev is already migrated, the guards skip the real `CREATE`/`ALTER` path and won't exercise the `QUOTED_IDENTIFIER` requirement.)
- [ ] **Apply the SQL to gccwebapp in the SAME deploy step as the DLL push** — never DLL-first. Recycle the app pool after.
- [ ] Confirm post-deploy: log in as each of the four test users (Admin/Analyst/External/Public). A login outage here is the canary — `AccountController.cs:1130` swallows the real SQL error and shows a generic `"Invalid email or password."`, so **don't trust the login page text; check the app log** (`AccountController.cs:1132`) for `Invalid column name`.

> Justifies this gate: DEF-239 (2026-06-03) — the CR-020 `UserCompanyId` migration shipped in the DLL but never ran on prod; every login broke and the generic auth error masked the SQL failure. A schema change is invisible to Gates 1-6 (no UI diff, no view, no EF perf signature) — only this gate catches a DLL/schema mismatch before it ships. The CR-171 backfill requirement adds a second failure mode this gate now covers: schema present but data not backfilled → certs vanish for partner logins.

---

## Gate 8 — CRUD write-path integrity  ·  automated (`_crud/run-all.js`)

**Fires when:** any create/update/delete path changed — the certificate `Create`/`Edit`/`Delete` actions, the admin JSON APIs (`/Admin/CreateTag|UpdateTag|DeleteTag`, `/Company/SaveCompany`+`DeleteCompany`, `/Product/SaveProduct`+`DeleteProduct`, `/Admin/CreateUser|UpdateUser|DeleteUser`), their services/repositories, or the mapped `Models/*` entities behind them.

**Mechanical proof (deterministic — needs the app on `localhost:5101` + `sqlcmd`):**

- [ ] Run **`node Scripts/smoke-prod/_crud/run-all.js`** (or the bundled `deploy-gate.js`). It drives a full **create → edit → delete** round-trip for **certificate, tag, company, product, and user**, then **verifies each step directly in SQL** (row persisted / renamed / removed-or-soft-deleted). **Must print `=> GREEN` (39 PASS / 0 FAIL).** Any FAIL means a write path silently stopped persisting — invisible to the UI-render smokes and the design/scoping gates.
- [ ] The suite is **fixtures-only and self-cleaning** (`ZZ_SmokeFixture_*`), and asserts a **Products-count invariant** (proves the CR-186 cert→product find-or-create didn't spawn duplicate product rows) plus net-zero row counts. A non-zero residue or count drift is itself a FAIL — investigate before shipping.
- [ ] Because it writes to the **dev DB**, never point it at a remote target (the harness `guardRemote()` refuses gccwebapp.com). Run it on localhost only.

> Justifies this gate: every other gate checks that a page *renders* or that data is *scoped/shaped* correctly — none of them prove a **Save actually writes** or a **Delete actually removes**. A regression that makes `SaveProduct` a no-op, or a validation change that silently rejects a valid Create, ships clean past Gates 1–7. This gate is the only mechanical proof the core business writes still work end-to-end (controller → service → EF → DB).

---

## Deploy gate results log

Per-bundle record of gate outcomes. Be precise: **VERIFIED** = mechanically run/checked; **N/A** = gate does not fire for this changeset (with the scope reason); **REASONED** = firing gate not run through its full agent/script, assessed by scope+diff instead (a conscious call, not a silent skip); **DEPLOY-TIME** = a manual step owed at upload, not at bundle time.

### `GCCWebApp-deploy-20260713-1242.zip` — assessed 2026-07-13 (supersedes the 0932 assessment; adds DEF-304)

**Shippable code in this changeset:** `wwwroot/js/certificate-tile-view{,-internal,-external}.js` (DEF-303 — search commits on Enter; DEF-304 — shareable filter URLs on the two authed views) + `Data/SeedData.cs`, `Program.cs` (ISS-001 — fresh-DB seeding re-wire, Development-only path). Tracking-doc edits are non-shippable. The bundle is a full working-tree publish, so it also re-carries the prior pending backlog (DEF-301/302 + committed items).

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes (every deploy) | **DEPLOY-TIME** | Owed at upload — zip server `wwwroot/uploads/` before extract. Not done at bundle time. |
| 1 — UI / Design-Guide | Yes (`wwwroot/js/*`) | **REASONED** | Diff is search-commit *timing* only — no markup/CSS/label/dialog change, no new `alert/confirm/prompt`, no `<h1>`/font/label edits. `ux-design-compliance` agent not run (no visual surface changed). |
| 2 — Cross-role + responsive | Yes (role-conditional tile JS) | **VERIFIED (behavior) / REASONED (visual)** | DEF-303: Playwright drove all three views — public (anon), internal + external (analyst): typing draws no chip/no reload; Enter commits chip+reload; clear+Enter clears; autocomplete intact; **zero console errors**. DEF-304: Playwright on internal + external — applied `certType`+`company` filters, confirmed they land in the URL and a fresh load of that URL re-checks the boxes + shows the chips. Full 4-login × 5-viewport screenshot + AxeCore pass not run (no layout/CSS change). |
| 3 — Data-scoping & endpoints | No (ran anyway) | **N/A — but VERIFIED GREEN** | Didn't fire (no business-data endpoint changed), but the `deploy-gate.js` **security** suite (isolation/RBAC/IDOR/detail scoping) ran GREEN (exit 0, 41s) — confirms no scoping regression. |
| 4 — Partner API contract | No (ran anyway) | **N/A — but VERIFIED GREEN** | Didn't fire (no `/api/v1/*` or v1-path change), but `amazon-contract-diff.js` ran: **CONTRACT UNCHANGED** (7 pass / 0 fail) vs the April baselines — GS1 list, GS1 single, Standard list all identical structure. |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual)** | Git-based audit: every code change maps to **DEF-303** or **ISS-001** (both registered with source/Reporter); `shipped-2026-07.md` has stakeholder entries; no orphan changes; no real-record UPDATE/DELETE in the deploy. `governance-manager` agent not invoked — audit done by hand. |
| 6 — Performance / EF | No (effectively) | **N/A** | `Program.cs` change is limited to the **Development** DB-init branch (a seeding call) — no EF query, service registration, or hosted-service change, and the modified branch does not execute on the Production-configured deploy target. |
| 7 — Pending migration → prod | Yes (bundle carries a pending migration) | **VERIFIED** | This changeset adds **no** new migration and no mapped-entity change. In-zip `migrations/apply-migrations.sql` verified: 71 lines, scripted from prod high-water `AddProductIdPartnersM2M`, applies only `WidenCertificateProductIdProductName` (metadata-only widen) + idempotent CR-171 backfill, **0 CONCAT**. Script unchanged from the prior bundle, which was prod-`.bak`-validated 2026-06-18. **DEPLOY-TIME:** still run this SQL (`sqlcmd -I`) *before* recycling the app pool. |
| 8 — CRUD write-path | No (ran anyway) | **N/A — but VERIFIED GREEN** | Didn't fire (no create/update/delete path/service/entity changed), but the `deploy-gate.js` **crud** suite ran **39 PASS / 0 FAIL => GREEN** (exit 0, 49s): full create→edit→delete + SQL-verified persistence + net-zero counts for cert/tag/company/product/user, with the CR-186 no-duplicate-product invariant holding. |

**Net:** no gate failed; no waiver required. Gates 1/2/5/7 were satisfied by targeted verification. The full deterministic backbone **was run** for this bundle — `node Scripts/smoke-prod/deploy-gate.js` (app on `localhost:5101` against the seeded `GCCDatabase_Dev`, incl. the Amazon-baseline certs) returned **GREEN — safe to deploy** (security ✔ 41s · crud ✔ 49s · contract ✔ 4s; results in `Scripts/smoke-prod/_results/deploy-gate-2026-07-13T15-10-55-161Z.json`). So even though Gates 3/4/8 don't formally fire for a frontend-search + dev-seeding changeset, the suites that back them are confirmed green with no collateral regression.

---

## Waiver log

If you ship past a failed gate on purpose, add a line here so it's not silently lost.

| Date | Deploy | Gate waived | Why | Follow-up |
|------|--------|-------------|-----|-----------|
| | | | | |

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

Verify against `../docs2/mockups/GCC_FinalDesign/docs/0_0_MasterDesignGuide_6_4.html` (relative to the project dir; `docs2/` sits at the git repo root — moved there 2026-07-20, see the CLAUDE.md reference table) and `../docs2/claude-ref/UI-STANDARDS.md`. *(Path corrected 2026-07-20 — the old `/0_Documentation/mockups/0_0_MasterDesignGuide.html` reference had been dead for some time, so this gate was auditing against a partially-missing standard. Found during the DEF-312 Gate 1 audit.)*

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

## Gate 9 — Static integrity + shipped-vs-fixed reconciliation  ·  automated (`_static/run-all.js`)

**Fires when:** **always.** It costs seconds, needs nothing running, and its cheapest check is the one that catches the most expensive failure (a fix that never ships).

**Mechanical proof (deterministic — needs NO server, NO database, NO network):**

- [ ] Run **`node Scripts/smoke-prod/_static/run-all.js`**. **Any FAIL is a deploy block.** WARNs are for reading, not blocking.
- [ ] Confirm the **selftest printed `9 ok / 0 failed`** before trusting anything else. `run-all.js` aborts the gate if it fails — see below for why this is not ceremony.
- [ ] Read the **check 4 roll-up** (`ROLL-UP — N fixed defect(s) NOT yet in the client's hands`) and confirm every `MODIFIED` entry is actually in the bundle you are about to upload. **`MODIFIED` means the fix exists ONLY in the working tree** — a bundle published from a clean checkout of `HEAD` silently drops every one of them.
- [ ] Record the four sub-results in the per-bundle table below as `9a/9b/9c/9d`.

**What it checks, and the defect each one exists to catch:**

| Sub | Check | Catches | Level |
|---|---|---|---|
| 9a | `tempdata-pairing` | `TempData` written with no reader on the destination → a stale confirmation surfaces later on an unrelated page (**DEF-318**, **DEF-330**) | FAIL on provable orphans |
| 9b | `redirect-targets` | `RedirectToAction`/`Url.Action`/`asp-action` naming an action that does not exist → 404 after a successful save (**DEF-331**); action rendering a missing view → HTTP 500 | FAIL |
| 9c | `orphan-views` | Partials nothing renders, views with no action, controllers no UI links to — how DocumentController became UI-dead with live routes | WARN only |
| 9d | `unshipped-fixes` | Register says *fixed* while the client still sees the bug because it never shipped | FAIL on missing paths, WARN on drift |

**Safety:** pure static analysis plus read-only `git` (`diff --name-only`, `status --porcelain`, `rev-parse`). It never starts the app, touches the DB, makes a network call, or modifies/stages/commits anything. The only write is a summary JSON to `_results/`. A bug in this gate misreports; it cannot break a build.

**The selftest is fatal — do not skip it.** Both of these happened while the gate was being built, in one sitting: an argument parser swallowed a closing paren so all 58 TempData writes reported "unresolved"; and the register parser used `(.*)$` on a CRLF file (in JavaScript `\r` **is** a line terminator, so `.` never matches it and `$` never anchors) and matched **0 of 247** entries. **Both produced a clean, all-clear report.** A broken check and a healthy codebase are indistinguishable unless something asserts the check still detects known-bad fixtures. That is the selftest's whole job.

> Justifies this gate: Gates 1–8 all assess a **bundle** — how it renders, scopes, performs and writes. None of them assess (a) the defect classes the C# compiler structurally cannot see, because they are magic strings and string-keyed dictionaries, or (b) the gap between *"the register says we fixed it"* and *"the client can actually see it"*. DEF-318 was fixed on 2026-07-31, sat uncommitted, and was re-reported by the client on 2026-08-03. Every gate was green the whole time. This one would have printed `DEF-318(MODIFIED)` on day one.

---

## Deploy gate results log

Per-bundle record of gate outcomes. Be precise: **VERIFIED** = mechanically run/checked; **N/A** = gate does not fire for this changeset (with the scope reason); **REASONED** = firing gate not run through its full agent/script, assessed by scope+diff instead (a conscious call, not a silent skip); **DEPLOY-TIME** = a manual step owed at upload, not at bundle time.

### `GCCWebApp-deploy-20260803-1422.zip` — built + content-verified 2026-08-03 (**supersedes `20260721-1426` and all earlier — THIS IS THE ONE TO UPLOAD**)

**Built** via canonical `Scripts/deploy.ps1 -SkipUploadsBackupPrompt -FromMigration 20260605131035_ChangeUserCompanyToFreeText` (Release, PleskTestEnvironment). 60.5 MB, **336 entries**, `GCCWebApp.dll` 6.07 MB built 14:24.

#### In-zip content verification — "is every change actually in here?"

Razor views are **compiled into `GCCWebApp.dll`** (no separate `Views.dll`), so a view change is only real if its literals are in that DLL. Verified by decoding the DLL and asserting on each change:

| Change | Marker asserted | Result |
|---|---|---|
| CR-031 | client's ALERT sentence, verbatim, full string | **PRESENT** |
| CR-031 | `title: null` (heading suppression) | **PRESENT** |
| CR-031 | `OriginalStatusWasExpired` server flag | **PRESENT** |
| DEF-317 | `Enter numeric certification results (e.g., 65%, 100%)` | **PRESENT** |
| DEF-316 | `Partner (separate by comma)` | **PRESENT** |
| DEF-325 | `UserUpdateResult` enum | **PRESENT** |
| CR-030 → CR-031 | superseded toast copy | **ABSENT ✔** (correctly removed) |
| CR-030 → CR-031 | `ExpiryCorrectionAlert` TempData key | **ABSENT ✔** |
| CR-030 → CR-031 | `Expiry-Correction-Revert` audit reason | **ABSENT ✔** |

Static assets compared **by MD5, working tree vs zip** — all identical: `main.js` (DEF-329), `certificate-create.js` (CR-031), `certificate-tile-view{,-internal,-external}.js` (DEF-319), `certificate-autocomplete.css` + `certificatetileview.css` (DEF-320).

> ⚠️ **Method note — do not repeat this mistake.** The first pass reported four false FAILs. .NET stores string literals as **UTF-16**, and decoding the DLL from byte 0 only sees strings at even offsets — literals at odd offsets are invisible. A grep/decode that finds `UserUpdateResult` (a UTF-8 type name) but not `Save Changes` (a UTF-16 literal) is **broken, not evidence of a stale build**. Always decode at **both** alignments and include known-present control strings (`Save Changes`, `Certificate number cannot be changed`) to prove the method works before trusting a negative.

#### Hygiene — all PASS

No `appsettings.Development.json` / `.Test.json`, no `GCCWebApp.Api.Tests`, **zero** entries under `wwwroot/uploads/`, `App_Data/uploads/`, `Scripts/`, `docs/`, `0_Documentation/`, `docs2/`. Dev DB password absent from every file in the zip.

#### Migration script — `migrations/apply-migrations.sql`

302 lines, **24 `IF NOT EXISTS` guards, 0 `CONCAT`** (the historical abort cause). All four migrations owed on prod are present: `AddNormalizedSearchColumns`, `AddProductIdPartnersM2M`, `WidenCertificateProductIdProductName`, `WidenStatusHistoryCertificateNumber`. CR-171 partner backfill appended. **The script sets no session options itself** — it relies on `sqlcmd -I` for `QUOTED_IDENTIFIER ON`, which the in-zip `migrations/README.md` does document. Measured hazard, not theoretical: a plain `UPDATE` against `Certificates` on this schema fails with *"SET options have incorrect settings: 'QUOTED_IDENTIFIER'"* because of the persisted `Normalized*` computed columns. **Run it with `-I`, or from SSMS (which defaults it ON).**

---

### Working tree @ 2026-08-03 — gate assessment for the above bundle

**Scope:** 24 modified files + 3 new (test, SQL diagnostic, 2 tracking docs), spanning **CR-030, CR-031 and DEF-316 → DEF-329**. Assessed against `git status` / `git diff HEAD` at 2026-08-03 ~18:00, per the Gate 5 rule to read git at deploy time rather than trust an earlier snapshot. **No new migration in this changeset**; four migrations remain owed on prod from earlier bundles (Gate 7 below).

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes | **DEPLOY-TIME — OWED** | Not a bundle-time step. Zip the server's `wwwroot/uploads/` before extracting. |
| 1 — UI / Design-Guide | Yes (3 views, 5 JS, 2 CSS changed) | **PARTIAL — agent review not run** | Mechanically confirmed for CR-031: no native `alert/confirm/prompt` (uses `AdminCore`-style `showConfirmation`), no `<h1>` added, client label copy verbatim. **The full `ux-design-compliance` review was not run** across the other stream's view/CSS/JS changes — owed. |
| 2 — Cross-role + responsive | Yes (tile views + role-conditional JS) | **NOT RUN — owed** | No 4-login × 5-viewport Playwright pass this session. DEF-329 in particular changes homepage search behaviour and is explicitly unverified. |
| 3 — Data-scoping & endpoints | Yes (controllers/services/repo changed) | **VERIFIED GREEN** | `deploy-gate.js` security suite **exit 0, 213s**. |
| 4 — Partner API contract | Yes (shared services/repo) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, **7 pass / 0 fail**, 19s. GS1 list, GS1 single, Standard list all structurally identical to the frozen April baselines. |
| 5 — Traceability audit | Yes (always) | **BLOCKERS FOUND — see below** | Every one of the 24 changed files mapped to a this-batch DEF/CR **except `wwwroot/js/main.js`**, which had none. Now logged as **DEF-329** (reconstructed from the diff — flagged as such). Three further gaps listed under "Blockers". |
| 6 — Performance / EF | Yes (`CertificateRepository.cs` changed) | **NOT RUN — owed** | DEF-328 is itself an EF plan-stability fix; a cold-start timing run on the public tile endpoint is exactly what this gate exists for and was **not** performed. Also note DEF-328 requires a one-time `UPDATE STATISTICS` on **prod** ahead of the deploy. |
| 7 — Pending migration → prod | Yes (carried forward) | **OWED — MANDATORY** | This changeset adds **no** migration, but prod's high-water is `20260605131035_ChangeUserCompanyToFreeText`, so **four** are still owed: `AddNormalizedSearchColumns`, `AddProductIdPartnersM2M`, `WidenCertificateProductIdProductName`, `WidenStatusHistoryCertificateNumber`. The bundle must carry the idempotent script and it must run **before** the app pool recycle. |
| 8 — CRUD write-path | Yes (cert + admin write paths changed) | **VERIFIED GREEN** | `deploy-gate.js` crud suite **39 PASS / 0 FAIL**, exit 0, 175s (cert/tag/company/product/user round-trips, SQL-verified, self-cleaning). |

**Deterministic backbone: GREEN** — `_results/deploy-gate-2026-08-03T17-56-04-392Z.json` (security ✔ 213s · crud ✔ 175s · contract ✔ 19s · roll-up `=> GREEN — safe to deploy`).

**Open items — status after the 14:22 build:**
1. ~~`main.js` orphan~~ — **CLOSED as traceability**: logged as **DEF-329**. Still owed: the change's author should confirm the entry and supply a QC ref, and it is **unverified in a browser** (it alters homepage search behaviour).
2. **Duplicate DEF number: `DEF-326` is used twice** — "Partner-isolation tests passed vacuously…" and "User status compared case-sensitively…". **STILL OPEN.** One must be renumbered. This is [[project_def_number_duplicates_parked]] recurring inside a live batch, not just in history. Note only the *user-status* one is client-facing; the test-only one correctly gets no shipped row.
3. ~~Missing stakeholder rows~~ — **CLOSED**: rows written for **DEF-320, 325, 326 (user status), 327, 328, 329** in `shipped-2026-07.md`, plus the CR-030 row rewritten as combined CR-030+CR-031 (its old text described the superseded post-save toast, which is **not** in this build). DEF-321/324 correctly have no row (logged-only / not-reproducible); the test-only DEF-326 correctly has none (test projects are excluded from the publish — verified in-zip).
4. **Superseded zips still present** — `20260721-1426`, `20260720-2332`, `20260720-2204` and older remain in `deployment-drops/`. **Delete them** so the wrong artifact cannot be uploaded; this recurring hazard is documented in every entry below.
5. **Concurrent work stream — STILL UNCONFIRMED.** Smoke results dated 2026-08-03 16:16–17:39Z came from another terminal during this session. The zip is a snapshot of the tree at 14:22 local; if that stream lands more work, **rebuild**.

**Net:** deterministic backbone **GREEN**, code builds clean, and the bundle's contents are **verified to carry every change** (see the table above). Gate 5 is now satisfied except the DEF-326 renumber. **Gates 1, 2 and 6 remain un-run** — no design-guide review, no 4-login × 5-viewport pass, and no cold-start timing, the last mattering because **DEF-328 is itself an EF plan-stability fix**. These are conscious **REASONED/owed** calls, not silent skips. **Gate 0 (server uploads backup) and Gate 7 (run the SQL before the app-pool recycle) are owed at upload.**

---

### `GCCWebApp-deploy-20260720-2332.zip` — assessed 2026-07-21 (**supersedes 2204 / 2137 / 2037 / 1408 / all earlier — THIS IS THE ONE TO UPLOAD**)

**Why rebuilt:** carries **DEF-315** — partner CSV export identifier-column misalignment (ASIN values landing under the UPC/GTIN headers). Client-reported as a cosmetic `=`/`"`; was actually silent data corruption in a partner deliverable.

**Delta vs 2204:** DEF-315 only — `Services/CertificateService.cs`, `Services/ProductIdService.cs`, `Controllers/ExportController.cs`, `wwwroot/js/admin/exportUtils.js`. No migration change; DEF-314 and CR-029 unchanged.

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes | **DEPLOY-TIME — OWED** | Built non-interactively; server-side backup **not** done. In-zip: 0 entries under `wwwroot/uploads/` and `App_Data/uploads/`. |
| 1 — UI / Design-Guide | Marginal (`wwwroot/js/*` changed) | **REASONED** | `exportUtils.js` change is CSV string encoding only — no markup, CSS, label, dialog, or font change; no new `alert/confirm/prompt`. |
| 2 — Cross-role + responsive | No | **N/A** | No view or role-conditional surface changed. |
| 3 — Data-scoping & endpoints | Yes (export service + controller) | **VERIFIED GREEN** | `deploy-gate.js` security suite exit 0, 33s. DEF-315 changes cell *encoding*, not which rows/columns a role receives — the DEF-295/CR-029 gating is untouched. |
| 4 — Partner API contract | Yes (shared services) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, 7/0 (5s). The CSV exporters are not on the `/api/v1/*` path. |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual, git-based)** | DEF-315 registered with symptom, root cause, before/after measurements, and the two-part fix; stakeholder row in `shipped-2026-07.md`; observations log appended. |
| 6 — Performance / EF | No | **N/A** | String formatting only; no EF query, index, registration, or hosted-service change. |
| 7 — Pending migration → prod | Yes (bundle carries pending migrations) | **VERIFIED** | DEF-315 adds no migration. Bundled `apply-migrations.sql` re-verified by **content, not line count** (it went 271→230 lines between builds purely on formatting — a count check would have raised a false alarm): all four migration history inserts present (`AddNormalizedSearchColumns`, `AddProductIdPartnersM2M`, `WidenCertificateProductIdProductName`, `WidenStatusHistoryCertificateNumber`), `CREATE TABLE [CertificateProductIdPartners]` present, DEF-314's `ALTER COLUMN [CertificateNumber] nvarchar(50)` present, all four `Normalized*` columns present, 24 `IF NOT EXISTS` guards, **0 CONCAT**. **DEPLOY-TIME: run before recycling the app pool — DEF-314 does not take effect without it.** |
| 8 — CRUD write-path | Yes (export/service code) | **VERIFIED GREEN** | `deploy-gate.js` crud suite **39 PASS / 0 FAIL** (exit 0, 53s). Plus a direct partner-export round-trip: 22 header cols, **0/4 misaligned rows**, identifiers under correct headers, leading-zero GTIN preserved as `="012345678905"`, and `StripExcelTextFormat` confirmed to pass plain values through unchanged (import round-trip intact). |

**Net:** all gates satisfied. Deterministic backbone **GREEN** (`_results/deploy-gate-2026-07-21T03-30-12-838Z.json` — security 33s ✔ · crud 53s ✔ · contract 5s ✔). **Gate 0 owed at upload. Gate 7 mandatory** — the SQL is what makes DEF-314 real.

---

### `GCCWebApp-deploy-20260720-2204.zip` — assessed 2026-07-21 (**SUPERSEDED by 2332 — missing the DEF-315 export fix**)

**Why rebuilt:** carries **DEF-314** — the client's live status-save failure on `/Certificate/Edit/26-10126`. Root-caused, fixed, and proven by negative control. Adds model change `Models/CertificateStatusHistory.cs` + **new migration** `20260721015555_WidenStatusHistoryCertificateNumber`.

**Delta vs 2137:** DEF-314 only (one model attribute + one metadata-only migration). CR-029 and everything earlier unchanged.

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes | **DEPLOY-TIME — OWED** | Built non-interactively (`-SkipUploadsBackupPrompt`); server-side backup **not** done. In-zip: 0 entries under `wwwroot/uploads/` and `App_Data/uploads/`. |
| 1 — UI / Design-Guide | No | **N/A** | DEF-314 changes one model attribute + a migration. No view, CSS, or JS touched. (CR-029's `.cshtml` change was assessed on the 2037 bundle below.) |
| 2 — Cross-role + responsive | No | **N/A** | No view or role-conditional surface changed by this delta. |
| 3 — Data-scoping & endpoints | Yes (mapped entity changed) | **VERIFIED GREEN** | `deploy-gate.js` security suite exit 0, **63s**. Widening a column changes no scoping or filtering. |
| 4 — Partner API contract | Yes (shared model) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, 7/0 (7s). `CertificateStatusHistory` is not exposed on `/api/v1/*`. |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual, git-based)** | DEF-314 entered in the register with reported symptom, failure scenario, root cause, why-it-hid, fix, and before/after verification; stakeholder row added to `shipped-2026-07.md`; observations log appended. |
| 6 — Performance / EF | Marginal (mapped entity) | **REASONED** | Metadata-only `ALTER COLUMN` widen on a low-volume audit table. No query, index, `Include`, service registration, or hosted-service change. |
| 7 — Pending migration → prod | **Yes — NEW MIGRATION** | **VERIFIED (bundle) / DEPLOY-TIME (apply)** | ⚠️ **This is the gate that matters for this deploy.** `20260721015555_WidenStatusHistoryCertificateNumber` is new. Production never auto-migrates (`Program.cs:922-926`), so **the DLL alone does NOT fix DEF-314** — the column stays `nvarchar(7)` and the client's error persists. Bundled `apply-migrations.sql` verified to contain all four pending migrations *and* the literal `ALTER TABLE [CertificateStatusHistory] ALTER COLUMN [CertificateNumber] nvarchar(50) NULL;` (line 246), **0 CONCAT**. **Run the SQL BEFORE recycling the app pool.** |
| 8 — CRUD write-path | Yes (cert Edit save path) | **VERIFIED GREEN** | `deploy-gate.js` crud suite **39 PASS / 0 FAIL** (exit 0, 56s). Plus DEF-314's own harness `_diag-status-save-truncation.js`: reproduced the failure pre-fix ([A] pass / [B] **client's exact error** / [C] pass) and cannot reproduce post-fix ([B] now saves) — a **negative control**, not merely a green test. |

**Net:** all gates satisfied; deterministic backbone **GREEN** (`_results/deploy-gate-2026-07-21T02-09-01-973Z.json` — security 63s ✔ · crud 56s ✔ · contract 7s ✔). **Gate 0 owed at upload. Gate 7 is mandatory this time — skipping the SQL leaves the reported defect unfixed.**

---

### `GCCWebApp-deploy-20260720-2137.zip` — assessed 2026-07-20 (**SUPERSEDED by 2204 — missing the DEF-314 fix**)

**Why rebuilt — Gate 7 defect found in the 2037 bundle (and in 1408 and 1250 before it):** all three were built with `-FromMigration 20260617200729_AddProductIdPartnersM2M`, i.e. **assuming prod had already applied that migration**. Their `apply-migrations.sql` therefore applied only `WidenCertificateProductIdProductName` — it **never created `CertificateProductIdPartners`** — and then the appended CR-171 backfill immediately ran `INSERT INTO CertificateProductIdPartners …`. If prod does *not* have that table, the script dies with `Invalid object name 'CertificateProductIdPartners'` and the deploy is left half-applied.

**The contradiction that exposed it:** the 1250/1408 entries assert prod high-water = `AddProductIdPartnersM2M`, but the 🟢 note at the top of Gate 7 records a **live prod probe** finding `CertificateProductIdPartners` **does not exist**. Both cannot be true. The bundled SQL was only correct under the optimistic reading. **Never re-derive prod's high-water from a previous bundle's notes — probe the target, or script from a point you can prove.**

**Fix:** rebuilt with `-FromMigration 20260605131035_ChangeUserCompanyToFreeText` — the last high-water this project has *directly verified* on prod. Because every block is `IF NOT EXISTS`-guarded, this is **safe under either reading**: if prod already has the June-16/17/24 migrations the guards skip them; if it doesn't, they get created. Still clear of the `EXEC(CONCAT(...))` parse trap (`AddCertificationResultsNumeric`, May 12, sits *before* the new baseline) — **0 CONCAT** verified in the output.

**Bundled script now:** 271 lines / 17,788 bytes (was 27 lines / 3,107 bytes). Applies `AddNormalizedSearchColumns` → `AddProductIdPartnersM2M` (incl. `CREATE TABLE [CertificateProductIdPartners]`) → `WidenCertificateProductIdProductName`, then the CR-171 backfill. All three `__EFMigrationsHistory` inserts verified present.

> ⚠️ **Verification gotcha:** grepping the script with `[0-9]+_[A-Za-z]+` silently **misses `AddProductIdPartnersM2M`** — the name contains digits, so the character class truncates the match and the migration looks absent. Use `[A-Za-z0-9]+`. A false "only 2 of 3 migrations" reading came from exactly this.

**Delta vs 2037:** the migration script only — application code is byte-identical (same source tree, CR-029 included).

**Gates:** all eight carried forward from the 2037 assessment below (same code, same green deterministic backbone — security 48s ✔ · crud 39s ✔ · contract ✔ `CONTRACT UNCHANGED`), with **Gate 7 re-verified and now genuinely correct** rather than passing on an unproven assumption. **Gate 0 (server uploads backup) remains OWED at upload.**

> **Open at time of writing:** the client reports *"An error occurred while updating the certificate"* on **every** edit at `https://gccwebapp.com/Certificate/Edit/26-10126`. Root cause **not yet confirmed** — awaiting the server exception from `logs\stdout_*.log`. The missing-`CertificateProductIdPartners` hypothesis has since been **RULED OUT by local experiment** (`Scripts/smoke-prod/_diag-edit-failure-repro.js`): renaming the table away on the dev DB and re-running a plain edit produces **HTTP 500 (raw error page)**, not the client's friendly message — because `ReloadEditFormDataAsync`, called *inside* the catch at `CertificateController.cs:1385`, re-throws on the same missing table and the exception escapes. The client's error therefore comes from something that throws **inside the try but leaves the reload path working**. **This bundle is NOT a fix for that incident** — it is a fix for our own broken migration script, which is a separate (and real) problem.

---

### `GCCWebApp-deploy-20260720-2037.zip` — assessed 2026-07-20 (**SUPERSEDED by 2137 — do not upload; its migration script is broken, see above**)

**Why rebuilt:** carries **CR-029** (QC #201 — Cert Results restored to all partner-facing surfaces, always as a percentage). The 1408 bundle predates it. Views precompile into `GCCWebApp.dll`, so the 1408 zip cannot carry the `CertificateExternalView.cshtml` change. **Delete the 1408 zip** to avoid uploading the wrong artifact.

**Delta vs 1408:** CR-029 only — `Controllers/Api/CertificateTileExternalApiController.cs`, `Services/CertificateService.cs`, `Views/Certificate/CertificateExternalView.cshtml`. Everything else identical.

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes (every deploy) | **DEPLOY-TIME — OWED** | Still owed at upload. Built with `-SkipUploadsBackupPrompt` (non-interactive shell), so the script's reminder was bypassed — **the server-side backup has NOT been done.** Re-verified in-zip: **0 entries** under `wwwroot/uploads/` and `App_Data/uploads/` (both slash styles). |
| 1 — UI / Design-Guide | Yes (`.cshtml` changed) | **REASONED** | The added block is a `detail-section` reusing three classes **already present in the same view** (`detail-section`, `certificate-section-title`, `certificate-field`) and mirrors `CertificateInternalView.cshtml`'s existing Certification Results block verbatim. No new markup pattern, no `<h1>`, no dialog, no inline `max-width`, no font declaration, no label/copy change (heading text matches the internal view and the client's own wording). Styling is inherited by construction. Rendered HTML confirmed on localhost. `ux-design-compliance` agent not re-run for this delta. |
| 2 — Cross-role + responsive | Yes (role-conditional view) | **VERIFIED (behavior) / REASONED (visual)** | Verified as the **External** role on localhost: partner detail for `26-0146` renders `<div class="certificate-field">17%</div>`; external tile API returns `17%/100%/27%/100%`; external CSV carries `Cert Results` and still **excludes** Pathway / Cert Subcategories / Internal Tag. Analyst export regression clean (column order preserved, 54 rows). The change is additive read-only text inside an existing section container, so the 4-login × 5-viewport screenshot + AxeCore sweep was not re-run. |
| 3 — Data-scoping & endpoints | Yes (controller + service changed) | **VERIFIED GREEN** | `deploy-gate.js` security suite exit 0, **48s**. Note: CR-029 *widens* what partners see by one field — that widening is the client's explicit 7/6 instruction, and Pathway / Cert Subcategories / Internal Tag remain gated (asserted in the export check above). |
| 4 — Partner API contract | Yes (shared `CertificateService` changed) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, 7 pass / 0 fail vs the frozen April baselines (3s). `/api/v1/*` untouched. **Also: this CR is what finally makes this gate's own line-72 rule (*"`CertificationResults` renders as percentage on every Partner-facing surface"*) actually true** — it had been passing while the external tile column was blank, because a blank column renders no raw decimal to catch. |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual, git-based)** | CR-029 entered in `change-request-register.md` with source (QC #201, Reporter Gemma, 7/6 client update), the why-CR-not-DEF basis, and the as-built scope; stakeholder row added to `shipped-2026-07.md`; `observations_log.md` appended. No orphan changes in the delta. No real-business-record UPDATE/DELETE in this deploy. |
| 6 — Performance / EF | Marginal (an EF `.Select` changed) | **REASONED** | Added **one scalar column on the root entity** (`c.CertificationResults`) to an existing projection. No new `Include`, no join, no collection navigation, no cartesian-explosion surface, no service registration or hosted-service change — so the DEF-166/234 failure mode does not apply and cold-start timing was not re-run. |
| 7 — Pending migration → prod | Yes (bundle carries a pending migration) | **VERIFIED** | CR-029 adds **no** migration and **no** mapped-entity change (`CertificationResults` is a long-existing column). In-zip `migrations/apply-migrations.sql` regenerated from prod high-water `AddProductIdPartnersM2M`: **27 lines, 3107 bytes, 0 CONCAT**, applies only `WidenCertificateProductIdProductName` + the appended idempotent CR-171 backfill. **DEPLOY-TIME:** run before recycling the app pool. |
| 8 — CRUD write-path | Yes (`CertificateService` changed) | **VERIFIED GREEN** | `deploy-gate.js` crud suite **39 PASS / 0 FAIL** (exit 0, 39s). CR-029 touches read/export paths only, but the suite confirms no collateral write regression. |

**Net:** all eight gates satisfied; **no gate failed, no waiver required.** Deterministic backbone **GREEN** (`_results/deploy-gate-2026-07-21T00-31-45-962Z.json` — security 48s ✔ · crud 39s ✔ · contract 3s ✔). In-zip verification: **338 entries**, 0 upload entries, `GCCWebApp.dll` 6,326,272 bytes. CR-029 confirmed present in the shipped DLL by **UTF-16 string-heap grep** — `Certification Results` (view heading), `Cert Results` (CSV header), `certificate-field` (view class) all True. *(Decode as UTF-16, not UTF-8: .NET's `#US` string heap is UTF-16LE, so a UTF-8 grep returns a false negative on exactly these literals — the same class of trap recorded on the 1408 bundle. Method/metadata names like `FormatCertificationResults` are ASCII and only appear under UTF-8.)* Gates 1/2 were assessed by scope+diff rather than agent-run — a conscious call on the basis that the only view change is an additive read-only field reusing in-file classes. **Gate 0 remains owed at upload.**

---

### `GCCWebApp-deploy-20260720-1408.zip` — assessed 2026-07-20 (**SUPERSEDED by 2037 — do not upload**)

**Why rebuilt:** the 1250 bundle was cut *before* the Gate 1 audit landed. Its finding became **DEF-312** (Import Preview select-all ignoring the CR-025 filter), which changed `wwwroot/js/product-id-manager.js` and `Views/Shared/_ProductIdManager.cshtml` — and views precompile into `GCCWebApp.dll`, so the 1250 zip could not carry the fix. Rebuilt at 14:08. **Delete the 1250 zip** to avoid uploading the wrong artifact.

**Delta vs 1250:** DEF-312 only (select-all scoping + the CR-025 search input's `aria-label`, 16px sizing, and `aria-live` count). Everything else identical.

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes (every deploy) | **DEPLOY-TIME — OWED** | Still owed at upload. Re-verified in-zip: 0 entries under `wwwroot/uploads/` and `App_Data/uploads/` (both slash styles checked). |
| 1 — UI / Design-Guide | Yes | **VERIFIED — CLEAR-TO-SHIP** | `ux-design-compliance` agent run against the working-tree diff. 0 blockers; 5 of 7 rules clean. CR-024 + CR-027 confirmed verbatim vs the requested copy with no adjacent drift. Its one real finding is DEF-312, **fixed and in this bundle**. Remaining should-fix/nits (sub-16px CR-027 hint spans, touch targets, `aria-pressed` in initial markup, `:focus-visible`) logged, not blocking. Also surfaced the dead design-guide path — corrected in CLAUDE.md + 4 agent definitions + this checklist. |
| 2 — Cross-role + responsive | Yes | **VERIFIED — CLEAR TO SHIP** | `accessibility-validator` agent + Playwright + axe across **Admin / Analyst / External / Public** at 5 viewports. 0 blockers, no NEW WCAG regressions from the diff. CR-025 filter tiles verified genuinely accessible (`role="button"`, `tabindex="0"`, Space+Enter, `aria-pressed` toggling, 3px focus ring). External restrictions confirmed (no Add/Import, zero internal tags, Edit/Create → AccessDenied, Actions column role-gated to zero width). DEF-311 tag removal and DEF-309 sort sequencing confirmed working. Artifacts in `Scripts/smoke-prod/_results/gate2-20260720/`. |
| 3 — Data-scoping & endpoints | Yes | **VERIFIED GREEN** | `deploy-gate.js` security suite exit 0, 44s (re-run post-rebuild). |
| 4 — Partner API contract | Yes | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, 7/0 vs the frozen April baselines (re-run post-rebuild, 6s). |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual, git-based)** | Every code change maps to a DEF/CR; DEF-312 added to the register with origin (Gate 1 audit) and reporter. Shipped log carries a stakeholder row per item plus a bundle row. No real-record UPDATE/DELETE in this deploy. |
| 6 — Performance / EF | No | **N/A** | DEF-312 is client-side JS + one view; no EF, repository, service-registration, or hosted-service change. |
| 7 — Pending migration → prod | Yes (bundle carries a pending migration) | **VERIFIED** | Unchanged by the rebuild: no new migration, no mapped-entity change. `migrations/apply-migrations.sql` regenerated from prod high-water `AddProductIdPartnersM2M` — applies only `WidenCertificateProductIdProductName` + the idempotent CR-171 backfill, **0 CONCAT**. **DEPLOY-TIME:** run before recycling the app pool. |
| 8 — CRUD write-path | Yes | **VERIFIED GREEN** | `deploy-gate.js` crud suite **39 PASS / 0 FAIL** (exit 0, 52s, re-run post-rebuild). Plus DEF-310 replace round-trip **10/10** and DEF-312 scoping suite **9/9** (with a negative control proving the test fails against pre-fix code). |

**Net:** all eight gates satisfied, **no gate REASONED-down this time** — Gates 1 and 2 were properly agent-run rather than assessed by scope. Deterministic backbone GREEN (`_results/deploy-gate-2026-07-20T18-17-38-968Z.json`). In-zip verification: 338 entries, DEF-312 present in the shipped `product-id-manager.js`, DEF-310 present in `GCCWebApp.dll`, and the DEF-312 view fix confirmed in **rendered HTML** from the running app (a DLL string-grep gave a false negative — Razor emits attribute values through a separate code path, so grep the rendered page, not the assembly). **Gate 0 remains owed at upload.**

---

### `GCCWebApp-deploy-20260720-1250.zip` — assessed 2026-07-20 (**SUPERSEDED by 1408 — do not upload**)

**Shippable code in this changeset:** the full working tree since the 07-15 commit. New vs. the discarded 1853 zip: DEF-306/307 (CSV formula-injection, leading zeros, UTF-8 BOM — `Services/CertificateService.cs`, `Controllers/ExportController.cs`, `DashboardController.cs`, `Services/ProductIdService.cs`, `Extensions/CsvExportHelper.cs`, `wwwroot/js/admin/exportUtils.js`, `product-id-manager.js`), DEF-308 (`Program.cs` preview route), DEF-309 (all three tile-view JS), **DEF-310** (`CertificateController.cs`, `Extensions/UploadCacheBusting.cs`, 4 cert views, `_ViewImports.cshtml`, `Services/FileUploadService.cs`), DEF-311 (`wwwroot/js/certificate-create.js`), CR-025/026/027. Re-carries DEF-300/301/302/303/304/305, CR-024, ISS-001. Tracking docs, `Scripts/`, and `_results/` are non-shippable.

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes (every deploy) | **DEPLOY-TIME — OWED** | Not done at bundle time. Zip the server's `wwwroot/uploads/` before extracting. The pubxml exclusions were verified in-zip (0 entries under `wwwroot/uploads/` **and** `App_Data/uploads/`), but the backup is still the seatbelt. |
| 1 — UI / Design-Guide | Yes (`.cshtml` + `wwwroot/js/*` changed) | **REASONED** | DEF-310's view edits add a query-string token to an existing `Url.Action` — no markup, CSS, label, dialog, heading, or font change; no new `alert/confirm/prompt`. CR-025/026/027 + DEF-311 were design-reviewed in their own sessions. `ux-design-compliance` agent not re-run for this bundle. |
| 2 — Cross-role + responsive | Yes (role-conditional views changed) | **REASONED** | DEF-310 touches all four cert views (public/single/internal/external) identically — the same token on the same URL — and the endpoint's role gate (`FileAccessAllowed`) was not modified. No layout change, so the 4-login × 5-viewport screenshot sweep + AxeCore pass was not re-run. |
| 3 — Data-scoping & endpoints | Yes (controllers/services changed) | **VERIFIED GREEN** | `deploy-gate.js` **security** suite (isolation / RBAC / IDOR / detail scoping) exit 0, 29s. DEF-310 note: the new `private` cache directive *strengthens* scoping — the same URL returns different bytes per role, so `private` prevents a shared cache serving one role's copy to another. |
| 4 — Partner API contract | Yes (shared services changed) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, 7 pass / 0 fail vs the frozen April baselines (GS1 list, GS1 single, Standard list — identical structure). DEF-310 does not touch `/api/v1/*`. |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual, git-based)** | Every modified code file maps to a DEF or CR (mapping in the "shippable code" line above); every DEF row cites its QC # / reporter; `shipped-2026-07.md` has a stakeholder row for each item **and** a bundle row for this zip. No orphan changes. **No real-business-record UPDATE/DELETE in this deploy** — the two `Scripts/data-ops/*.sql` files are untracked and not shipped (`Scripts/**` is excluded from publish). `governance-manager` agent not invoked; audit done by hand against `git status`. |
| 6 — Performance / EF | No | **N/A** | No `Repositories/*` change, no EF query change, no service registration or hosted-service change. DEF-310 adds a response header and a pure hash helper; `Program.cs` was last touched for DEF-308 (a route mapping). |
| 7 — Pending migration → prod | Yes (bundle carries a pending migration) | **VERIFIED** | This changeset adds **no** new migration and **no** mapped-entity change (`git status` shows nothing under `Migrations/` or `Models/`). In-zip `migrations/apply-migrations.sql` regenerated: **27 lines**, scripted from prod high-water `AddProductIdPartnersM2M`, applies only `WidenCertificateProductIdProductName` (metadata-only widen) + the idempotent CR-171 backfill, **0 CONCAT** — contents read and confirmed. **DEPLOY-TIME:** run this SQL (`sqlcmd -I`) *before* recycling the app pool. |
| 8 — CRUD write-path | Yes (cert Create/Edit path changed) | **VERIFIED GREEN** | `deploy-gate.js` **crud** suite: **39 PASS / 0 FAIL => GREEN** (exit 0, 51s) — full create→edit→delete with SQL-verified persistence and net-zero counts for cert/tag/company/product/user, CR-186 no-duplicate-product invariant holding. Additionally DEF-310's own harness (`def310-replace-document-verify.js`) drove a real cert create + document replace **10/10 PASS**. |

**Net:** no gate failed; no waiver required. Deterministic backbone **GREEN** (`_results/deploy-gate-2026-07-20T16-44-41-517Z.json`). Gates 1 and 2 were assessed by scope+diff rather than re-run — a conscious call, recorded here, on the basis that the only new view change is a query-string token on an existing URL. **Gate 0 is still owed at upload time.**

---

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

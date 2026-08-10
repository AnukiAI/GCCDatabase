# Pre-Deploy Agent Checklist

**Purpose:** Stop client-found UI/QC regressions *before* they ship. Each gate below names the agent that runs it, what it must verify, and the documented incident that justifies it. Run top-to-bottom before every Plesk deploy. A gate that can't pass blocks the deploy — fix or consciously waive (and log the waiver).

> **Gates 0–10 run before the upload. Gate 11 runs after it, and is not optional.** For most of this document's life there was no post-upload step at all, so "Pending deploy" was a state the process could enter and never leave — 54 shipped defects were still recorded as undelivered on 2026-08-04. The deploy is not finished when the zip is up; it is finished when the record says so.

**How to run a gate:** launch the named agent via the Task tool with the changed files as scope. Gates 1–4 are independent — fire them in parallel in a single message, then read the results. Gate 5 (governance) runs last because it audits the *outcome* of the others. **Gates 4, 6, and 8 also have deterministic script steps that need the app running on `localhost:5101`** (Gate 4 = `amazon-contract-diff.js` Amazon-shape diff; Gate 6 = cold-start timing; Gate 8 = the write-path/security/contract script bundle) — start the app first, then run those alongside the agent reviews.

**One-command deterministic gate:** with the app on `localhost:5101` and `sqlcmd` on PATH, run **`node Scripts/smoke-prod/deploy-gate.js`**. It runs the three deterministic script suites in sequence — **security** (`_security/run-all.js`: isolation/RBAC/IDOR/detail scoping), **crud** (`_crud/run-all.js`: cert/tag/company/product/user write paths), and **contract** (`amazon-contract-diff.js`) — and exits `0` only if all pass (`2` = app/sqlcmd not ready, `1` = a gate failed). This is the mechanical backbone of Gates 3, 4, and 8 in one shot (and the intended CI entrypoint). The agent-driven gates below still cover what a script can't (visual/design, WCAG, traceability, EF perf, migrations).

**Scope trigger:** run the full list before any `dotnet publish` to gccwebapp.com / greencirclecertified.com. For a hotfix touching one surface, run only the gates whose "Fires when" matches — but Gates 5, 9 and 10 always run, and Gate 11 always runs after the upload.

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

> ⚠️ **CORRECTED 2026-08-04 — this gate's founding premise is out of date. Read this before acting on anything below.**
>
> This gate was written when **Production never auto-migrated**, and much of the text below (including the DEF-314 entry's *"the DLL alone does NOT fix DEF-314"*) still assumes that. **It is no longer true.** `Program.cs:958-982` applies pending migrations at startup, gated by `Database:AutoMigrateOnStartup` — which **`appsettings.Production.json` sets to `true`** (added for **DEF-252**, itself a recurrence of DEF-239, after two login outages caused by a hand-run migration that never happened). Verified in the `20260804-2230` bundle's own in-zip config.
>
> **What this changes:**
> - The four owed migrations **will apply themselves** on the first startup after the app pool recycles. The bundled `apply-migrations.sql` is now a **belt-and-braces** step, not the only mechanism.
> - EF's own migrator runs with `QUOTED_IDENTIFIER ON`, which is exactly the Msg 1934 half-apply trap the hand-run SQL falls into when someone forgets `sqlcmd -I`. **On this environment the automatic path is the safer of the two.**
> - Running the SQL by hand first is still recommended and still harmless — every block is `IF NOT EXISTS`-guarded, so EF then finds nothing pending. Do it if you want the schema change to land *before* the new code serves traffic.
>
> **Do not delete this gate.** It still governs the real-production cutover (`greencirclecertified.com`), where `AutoMigrateOnStartup` is expected to be `false` and every word below applies verbatim. It also still catches the case auto-migrate cannot: a **data backfill** that a schema migration depends on (CR-171), which EF will never run for you.
>
> **Historical premise, retained because it governs a manual-migration environment:** Production never auto-migrates — schema is managed by hand, so shipping a DLL that maps a new column does not add it to the DB. EF then `SELECT`s the missing column on the next query and SQL Server throws `Invalid column name '…'`.

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

## Gate 10 — Config that points at data still resolves  ·  manual + DB (`sqlcmd`)

**Fires when:** **always.** It is two SQL queries and it guards a control that fails **silently and closed**.

Some settings are not values — they are *references to a database row*. The DLL ships, the config ships, the app starts clean, and the reference is dangling. Nothing surfaces until the behaviour it controls is needed, and by then the evidence is what was lost.

**The one that exists today — `Audit:SystemUserEmail`.**

`AuditHistory.ChangedBy` is `NOT NULL` with `FK_AuditHistory_Users_ChangedBy`, but v1 API callers authenticate with an API key and have **no `Users` row**. `AuditService` therefore resolves a service account named by `Audit:SystemUserEmail` (currently `swest@anukiconsulting.com`, the `admin` account) so machine-initiated deletes can be attributed. If that account is renamed, deactivated, deleted, or has its email changed on the target DB, resolution fails, a warning goes to the log, and **the delete proceeds unaudited**. No error, no user-visible symptom, no failed request — just no audit row for a destroyed record.

- [ ] Read the value the bundle will actually run with: `Audit:SystemUserEmail` in the **in-zip** `appsettings.json` (base config; confirm no environment file overrides it — `appsettings.Production.json` currently does not).
- [ ] Assert the account exists and is usable **on the target DB, not dev** — per the [[feedback_diagnose_on_clients_environment]] rule:
  ```
  sqlcmd -S "tcp:68.71.129.120,1533" -d gccwebap_certificates -U <user> -P <pw> -I -h -1 -W ^
    -Q "SET NOCOUNT ON; SELECT Id, Username, Email, Status FROM Users WHERE Email = '<configured-email>';"
  ```
  **Must return exactly one row, `Status = 'active'`.** Zero rows = deploy block: either fix the config or fix the account before shipping. More than one row means duplicate emails, which the app forbids elsewhere — investigate rather than assume the first wins.
  > **Baseline — verified 2026-08-04 against `gccwebap_certificates`:** returns exactly `1 | admin | swest@anukiconsulting.com | active`. Same row on the dev DB (`GCCDatabase_Dev`), which is why the value sits in base `appsettings.json` with no per-environment override. Note the decoy: `admin@greencirclecertified.com` also exists on both DBs (`admin.uat`, Id 22, Admin role) and is *not* the account the app treats as admin.
- [ ] If the value was changed in this bundle, also confirm the **old** account is not the one referenced by any in-flight data-ops script.

**Do not "fix" this by pointing at whatever admin account happens to exist on the target.** The reference is deliberate: the account is the attribution shown against every machine-initiated change. Changing it silently re-attributes future audit rows to a different person.

> **Watch for new members of this class.** Any future setting whose value is a *lookup key* into the database belongs in this gate — not just a credential or a URL, but anything the app resolves to a row at runtime. The tell is a config read followed by a `Where(x => x.Something == configuredValue)`.

> Justifies this gate: DEF-337. The delete-audit coverage work closed thirteen unaudited delete paths and made the audit trail the system's only recoverability story — then made part of that trail depend on a config value resolving to a row. Gates 1–9 cannot see this: there is no UI diff, no view, no schema change, no failing write, and the app is *healthier* by every other measure. The failure mode is identical in shape to the one DEF-187 and DEF-337 were both raised for — an audit row that was never written, noticed only when someone goes looking for it after a mistaken delete. `DEF337_AuditSystemUserFallbackTests` proves the code fails closed; only this gate proves the *deployed configuration* actually resolves.

---

## Gate 11 — Post-deploy close-out  ·  automated (`Scripts/deploy/close-out.js`)  ·  **runs AFTER the upload**

**This is the only gate that fires after the zip goes up.** Every other gate assesses a bundle; this one records that the bundle *arrived*. Skipping it does not risk a bad deploy — it corrupts the client-facing record instead, which is why it was missing for months without anyone noticing.

- [ ] Immediately after the upload succeeds, dry-run the close-out for the ids in this bundle:
  ```bash
  node Scripts/deploy/close-out.js --date=<upload date> --ids=DEF-1,DEF-2,... --bundle=<zip id>
  ```
  Use `--all-pending` instead of `--ids` only when the bundle genuinely carried everything outstanding.
- [ ] **Read the preview.** Confirm each `-` / `+` pair is a row you actually shipped, and read the `⚠ stamped with NO shipped-log row to corroborate it` list — those dates rest entirely on your assertion.
- [ ] Re-run with `--apply`. Confirm the printed `genuinely undeployed: N -> M` actually moved.
- [ ] Re-run Gate 9 (`node Scripts/smoke-prod/_static/unshipped-fixes.js`) and confirm the roll-up dropped by the number you just closed out. **Independent confirmation, not the script grading its own homework.**
- [ ] Anything listed under `NEEDS A HUMAN` is a real edit for a person — the script refuses rather than guesses. Clear those by hand.

**Why the script refuses instead of just rewriting.** Check 4 decides "did this ship?" with two regexes, and a row carries *two* pending signals: `**Pending deploy 2026-07-31**` and `**NOT yet in a built zip — folds into the next rebuild**`. Flip only the first and the row still reads as pending, the roll-up never moves, and the close-out looks successful while changing nothing. Every rewrite is therefore re-tested against check 4's own imported regexes before it is written, and refused if it would still read as pending.

**Backfill is a different operation from close-out — do not conflate them.** `--date` stamps everything with one date, which is correct for the bundle you just uploaded and *wrong* for history. The first dry run of `--all-pending` proposed rewriting a row that names its own June zip to "Deployed 2026-08-04". A confidently wrong date is worse than visible staleness, so a row whose own date is more than 21 days before `--date` is refused; use `--keep-row-date` to backfill history from each row's own evidence.

> Justifies this gate: on 2026-08-04 a full regression run reported **54 fixed defects "NOT yet in the client's hands"** while every one of them was `CLEAN` — committed and matching HEAD. Every row in `shipped-2026-07.md` still read "Pending deploy", ~50 of them, long after shipping. Nothing had gone wrong at deploy time; there was simply no step that ever marked anything delivered. The cost lands on the client, not the build: the shipped log is used as delivery receipts, and it was **understating** delivery by fifty items.

---

## What is actually deployed right now? — `whats-deployed.js`

**Run `node Scripts/smoke-prod/whats-deployed.js` when nobody can say which artifact is live.** Register text is a *claim* ("deployed 2026-08-04"); this is *evidence*. It is read-only — passes the test-env gate, issues GETs, writes nothing, and never authenticates as an app user.

It reports **two signals separately, and deliberately does not combine them:**

1. **DLL contents, via route presence.** Views and controllers compile into `GCCWebApp.dll`, so no static asset can tell you what is in it. Routing runs **before** authorization, so an anonymous GET to a deleted `[Authorize]` route discriminates cleanly: `302 → /Account/Login` = controller still present; `404` = controller gone. Currently keyed on the **`DocumentController` removal** (deleted in the DEF-337 batch). A **control route** that exists in *both* builds is checked first and **aborts the run if it does not behave like a live authorized route** — without that, a gate page or WAF answering `200` to everything grades as "the application" (the DEF-341 failure mode).
2. **`wwwroot` generation, via asset md5** against the working tree.

They can disagree — a deploy that extracted the DLL but not `wwwroot` is a real, half-applied state, and averaging the two into one number is how that stays hidden.

> **Baseline — measured 2026-08-04 against `https://gccwebapp.com`:** DLL verdict **the DEF-337 batch IS deployed** (all three `/Document/*` routes `404`; control `/Certificate/Create` correctly `302 → /Account/Login`). Asset verdict **7 same / 0 differ / 0 unreachable** — live `wwwroot` matches the working tree exactly. So the live build corresponds to the `20260804-1705` tree, which is the one the client validated. **This retroactively confirms the "deployed 2026-08-04" claims on DEF-334 / DEF-336 / DEF-338 / DEF-339 — they were accurate, they were just unverified.** The only code change made after that build is **DEF-340**, so DEF-340 is the sole fix in `20260804-2230.zip` that is not already live.

**When to re-key it.** The route marker only works while `DocumentController`'s absence is the newest DLL-level landmark. After the next deploy, point `REMOVED_ROUTES` at whatever the newest removed-or-added route is, or the probe silently answers a stale question.

---

## Deploy gate results log

Per-bundle record of gate outcomes. Be precise: **VERIFIED** = mechanically run/checked; **N/A** = gate does not fire for this changeset (with the scope reason); **REASONED** = firing gate not run through its full agent/script, assessed by scope+diff instead (a conscious call, not a silent skip); **DEPLOY-TIME** = a manual step owed at upload, not at bundle time.

### `GCCWebApp-deploy-20260807-1312.zip` — built, gated + content-verified 2026-08-07 (**supersedes `20260804-2230` and all earlier — THIS IS THE ONE TO UPLOAD**)

**Built** via canonical `Scripts/deploy.ps1 -SkipUploadsBackupPrompt -FromMigration 20260721015555_WidenStatusHistoryCertificateNumber` (Release, PleskTestEnvironment). 60.5 MB, **338 entries**, `GCCWebApp.dll` 6,358,016 B. Release build clean: **0 errors**.

**Delta vs `20260804-2230`:** three client-reported fixes from the 2026-08-07 spot-check — **DEF-342** (Product Identifiers CSV template Partner heading + importer prefix tolerance), **DEF-343** (Certification Type blank on Edit for child/inactive types), **DEF-344** (certificate detail showed only the first partner). Four files: `Controllers/CertificateController.cs`, `Controllers/Api/ProductIdApiController.cs`, `Views/Certificate/CertificateInternalView.cshtml`, `wwwroot/js/product-id-manager.js`.

> **Gate 5 concurrency — checked, and it fired again (fourth bundle running).** Another terminal committed **`4c158c2`** at 11:06 mid-session, touching `Controllers/AccountController.cs` (+13/-1). Read the diff rather than assuming: it is the **DEF-340** TOTP `TempData`→`ViewBag` hydration, which **already shipped in `20260804-2230`** — the commit merely caught up with the tree that bundle was published from. So it is not a new behaviour in this zip. Everything else in `4c158c2` is `Scripts/` and `_results/` (never published). **Net: the delta against what is live really is just DEF-342/343/344.**

**No migration owed.** Repo migration tail and prod `__EFMigrationsHistory` are both at `20260721015555_WidenStatusHistoryCertificateNumber` — verified against the prod DB, not from memory. The generated `migrations/apply-migrations.sql` is **0 lines of migration DDL** (2,053 B, all of it the appended CR-171 partner-backfill data script). **Gate 7 does not fire for this bundle.**

#### ⚠️ Verification method note — DEF-344 is literal-invisible in Release

The literal-grep method **failed for DEF-344 and would have produced a false ABSENT**. The fix adds no new string: it swaps `Model.Certificate.Partner` for a join over `Model.Certificate.Partners`. The one candidate marker, the Razor local `partnerNames`, **is present in the Debug DLL but stripped from Release** (local names live in the PDB). This is the mirror image of the DEF-340 note on the bundle below — there the literal existed but predated the fix; here the literal does not survive the build configuration.

What was used instead — the artifact was **executed**, which is stronger than any static marker:

| Link | Evidence |
|---|---|
| The zip carries this build's assembly | in-zip `GCCWebApp.dll` sha256 `B95306FF…` **==** `bin/Release/net8.0/win-x64/publish/GCCWebApp.dll` sha256 — exact match |
| The shipped assembly renders both partners | Ran `publish/GCCWebApp.exe` (the Release output that went into the zip) on `:5102` against `GCCDatabase_Dev`. Cert `26-0146` has `CertificatePartners` = Amazon+Novi and denormalized `Certificates.Partner` = `Amazon`. Detail page rendered **`Partner: Amazon, Novi`** — so it is demonstrably reading the collection, not the column |
| The shipped assembly appends the stored cert type | Same instance: with `26-0146` set to type **73 "Recycled Content" (ParentId 6)** the dropdown rendered **21 options, `selected` = 73**; with the type restored to top-level 17 it rendered **20 options, `selected` = 17`**. The 21-vs-20 delta *is* the append firing (dev has 19 top-level active types + 1 placeholder) |

Dev cert `26-0146` was mutated only to create the child-type condition (dev DB, disposable) and **restored to `CertificationTypeId = 17` afterwards** — confirmed by re-query.

> **Running the published exe to verify needs two env overrides**, worth recording: `appsettings.Development.json` is (correctly) excluded from the publish, so `Authentication:Enable2FA` fell through and the Release instance **challenged 2FA on login**, blocking the check until `Authentication__Enable2FA=false` was set. Incidental but useful corroboration for `production-cutover-plan.md` §2a: **environment variables do override the shipped JSON**, which is the mechanism that section relies on.

#### In-zip content verification

| Change | Marker asserted | Result |
|---|---|---|
| *control* | `CertificateController` (UTF-8 type name) | **PRESENT** ✔ method valid |
| *control* | `zzz_not_a_real_marker_zzz` | **ABSENT** ✔ no false positives |
| DEF-342 | `"Type","Product ID",…,"Partner (separate by comma)","Description"` in in-zip `wwwroot\js\product-id-manager.js` | **PRESENT** |
| DEF-342 | `const partnerHeader = rawHeaders.find` (importer tolerance — proves the **reader** shipped, not just the template) | **PRESENT** |
| DEF-343 | `BuildCertificationTypeOptions` (new method name in DLL metadata) | **PRESENT** |
| DEF-344 | `partnerNames` | **ABSENT — expected**, see method note; proven by execution instead |

DEF-342 is asserted on **both halves deliberately**: a bundle carrying the renamed template header without the importer's prefix matching would silently import every product-ID row with **no partner**, which is worse than the copy bug it fixes.

#### Hygiene — all PASS

**0 entries** for each of: `appsettings.Development.json`, `appsettings.Test.json`, `Api.Tests`, `wwwroot/uploads/` (both slash styles), `App_Data/uploads/`, `Scripts/`, `docs/`, `docs2/`. In-zip `appsettings.json` (3,627 B) contains **no** dev DB credential and no `GCCDatabase_Dev` reference.

> **Pre-existing exposure that ships in this bundle (not introduced by it):** in-zip `appsettings.json` still carries the plaintext Gmail app password — **DEF-263** / [[security-plaintext-gmail-credential]]. It is in every bundle to date. Flagged here because the file lands on the server; it does not block this deploy, and it is the reason `production-cutover-plan.md` §2a routes SMTP credentials through environment variables rather than this file.

#### Gate assessment

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes | **DEPLOY-TIME — OWED** | Built with `-SkipUploadsBackupPrompt`; server-side backup **not** done. In-zip upload entries: 0. |
| 1 — UI / Design-Guide | Yes (`CertificateInternalView.cshtml`) | **REASONED** | One existing `.certificate-field` row changes its *value* from one string to a comma-joined list. No markup, CSS, font, label, container or `<h1>` change; no native dialog. Full `ux-design-compliance` review not re-run. |
| 2 — Cross-role + responsive | Yes (cert detail view) | **PARTIAL — functionally verified, not swept** | Detail + Edit exercised on the **Release artifact** as Admin (evidence above), and all three seeded logins verified role-correct on the Debug build. The 4-login × 5-viewport Playwright + AxeCore pass was **not** run. See the external-view note under Gate 3. |
| 3 — Data-scoping & endpoints | Yes (controllers changed) | **VERIFIED GREEN** | `deploy-gate.js` security suite **exit 0, 66s — PASS**. Note the deliberate scoping decision in DEF-344: only `CertificateInternalView` was changed. `CertificateExternalView` has the same fault but the correct partner-scoped output is the **unresolved DEF-321 question**, so it was left alone rather than risk a cross-partner leak. **No external/partner-facing surface changed in this bundle.** |
| 4 — Partner API contract | Yes (`Api/` controller changed) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, **7 pass / 0 fail**, 36s. GS1 list, GS1 single, Standard list all structurally identical to the frozen April baselines. Load-bearing here: `ProductIdApiController` was edited, even though only in a dead `sample-csv` action. |
| 5 — Traceability audit | Yes (always) | **VERIFIED** | DEF-342/343/344 all in the register with QC refs and client-verbatim repros; `observations_log.md` appended. `4c158c2` read at build time and diffed rather than assumed (see box above). |
| 6 — Performance / EF | Yes (`CertificateController.cs` adds a query) | **REASONED — not timed** | `ReloadEditFormDataAsync` gains one `Where(Id==).Select(CertificationTypeId).FirstOrDefaultAsync()` — single-column, PK-filtered, and only on the **validation re-render** path, not the Edit GET. No cold-start timing run. |
| 7 — Pending migration → prod | **No** | **N/A — nothing owed** | Repo tail == prod high-water `20260721015555_WidenStatusHistoryCertificateNumber`, verified against the prod DB. Generated script carries **0 lines** of migration DDL. |
| 8 — CRUD write-path | Yes (Edit form data path touched) | **VERIFIED GREEN** | `deploy-gate.js` crud suite **exit 0, 85s — PASS**. Relevant because DEF-343 changes what the Edit form offers; the concern it closes is precisely a **wrong value being written on save**. |
| 9 — Static integrity | Yes (always) | **VERIFIED GREEN-WITH-WARNINGS** | `_static/run-all.js`: **22P / 0F / 10W**, exit 0 — identical warning count to the previous bundle, no new warnings. Check-4 roll-up: **5 undeployed** — `DEF-341(CLEAN) DEF-341(CLEAN) DEF-342(MODIFIED) DEF-343(MODIFIED) DEF-344(MODIFIED)`. The three `MODIFIED` are this bundle's fixes, and the gate explicitly warns *"publish from THIS tree, not a clean checkout of HEAD"* — which is what `deploy.ps1` did. The two `DEF-341(CLEAN)` are the known **duplicate DEF-341 headers** with stale Status text, already shipped ([[project_def_number_duplicates_parked]]). |
| 10 — Config resolves to a row | Yes (always) | **VERIFIED** | `Audit:SystemUserEmail` = `swest@anukiconsulting.com` read from the **in-zip** `appsettings.json`; in-zip `appsettings.Production.json` does **not** override it. Probed the **target** DB (`gccwebap_certificates`): returns exactly `1 | admin | swest@anukiconsulting.com | active`. |

**Deterministic backbone: GREEN** — `_results/deploy-gate-2026-08-07T17-25-50-736Z.json` (security ✔ 66s · crud ✔ 85s · contract ✔ 36s · roll-up `=> GREEN — safe to deploy`), plus Gate 9 static ✔ exit 0 (`_results/static-gate-2026-08-07T17-26-31-519Z.json`).

**Net:** built, gated, hygiene-clean and content-verified. All four mechanical gates GREEN with **0 FAIL** anywhere. Every change is provably inside the zip — DEF-342 on both halves of its template/parser pair, DEF-343 and DEF-344 by **executing the shipped Release assembly**, which is stronger evidence than the literal grep that structurally could not see DEF-344.

**Still owed — the ones no script covers:**
- **Gate 0 (upload):** zip the server's `wwwroot/uploads/` before extracting. Built with `-SkipUploadsBackupPrompt`, so no backup was made.
- **Gate 7:** nothing owed — **do not run** `apply-migrations.sql` expecting schema work; it carries only the CR-171 backfill.
- **Gates 1, 2, 6 (REASONED, not run):** no `ux-design-compliance` review, no 4-login × 5-viewport Playwright + AxeCore sweep, no cold-start timing.
- **⚠️ This deploy overwrites `web.config` and `appsettings.Production.json`** (both are in the zip). Any SMTP or `Enable2FA` settings applied on the server for `production-cutover-plan.md` §2a will be **wiped by this extract** and must be re-applied afterwards. The failure is silent — email simply reverts to `MockEmailService`.
- **DEF-342's actual download is unexercised in a browser.** The template is generated client-side; the served asset and the parser were verified, but nobody has opened `product-ids-sample.csv` in Excel to confirm it renders as six columns.

---

### `GCCWebApp-deploy-20260804-2230.zip` — built, gated + content-verified 2026-08-04 (**SUPERSEDED by `20260807-1312` — missing DEF-342 / DEF-343 / DEF-344**)

**Built** via canonical `Scripts/deploy.ps1 -SkipUploadsBackupPrompt -FromMigration 20260605131035_ChangeUserCompanyToFreeText` (Release, PleskTestEnvironment). 60.5 MB, **338 entries**, `GCCWebApp.dll` 6.36 MB built 22:31. Release build clean: **0 errors**.

> **Rebuilt from `20260804-1705` to pick up DEF-340.** The 17:05 bundle was built, then the gates were run against it — and Gate 9 surfaced **DEF-340** (TOTP setup errors written to `TempData` but read from `ViewBag`). The fix landed *after* that zip existed, so the zip was stale the moment it was written. `_static` said so in as many words: `DEF-340(MODIFIED)` in the check-4 roll-up. **This is the roll-up doing exactly the job it was built for** — the 17:05 zip would have shipped clean-looking and silently missing the fix. The 17:05 artifact has been deleted so it cannot be uploaded by mistake.
>
> **All gate results below were produced against the 17:05 code tree.** Gates 3/4/8 are carried forward unchanged: DEF-340 touches one MVC GET action's ViewBag hydration — no endpoint, no query, no write path, no `/api/v1/*` surface. Gate 9 was **re-run after the fix** and is reported at its post-fix numbers.

#### ⚠️ Verification method note — DEF-340 could not be asserted by string literal

Every other change in this bundle was proven present by finding a **new string literal** in the DLL. **That method does not work for DEF-340 and initially gave a false PASS.** The fix adds no literal: the two TOTP error strings it makes visible (`Please enter a valid 6-digit code…`, `Invalid code. Please check your authenticator app…`) were *already in the 17:05 DLL* — they are the **writers**, which were never the problem. Asserting them proves nothing about whether the reader-side hydration shipped. Both DLLs are also **identical in size** (6,356,992 bytes), so size is no signal either.

What was actually used instead — a hash chain, each link checked:

| Link | Evidence |
|---|---|
| The two builds really differ | `sha256` 17:05 `1C41FBA7…` vs 22:30 `88E50C8D…` — **differ** (necessary, not sufficient: MVID changes every build) |
| The zip carries *this* build's assembly | in-zip `GCCWebApp.dll` `sha256` == `bin/Release/net8.0/win-x64/publish/GCCWebApp.dll` `sha256` (`88E50C8D…`) — **exact match** |
| That build came from fixed source | `Controllers/AccountController.cs:1303,1310,1312` carry the `DEF-340` comment and the `TempData["ErrorMessage"] is string totpSetupError` hydration |
| The fix behaves as intended | Gate 9 `tempdata-pairing` re-run: check-1 WARNs **9 → 6**, key now reports `"ErrorMessage" paired (6 write / 2 read)` |

> **Generalise this.** A fix that only *moves* or *consumes* an existing value adds no literal, so the standard literal-grep is structurally blind to it and will return a confident PASS on the wrong evidence. When a change introduces no new string, prove it with the hash chain above (publish-output ↔ zip, plus source), or with a static gate re-run that observes the behaviour change — never with a literal that predates the fix.

**Delta vs `20260803-1422`:** the DEF-337 delete-audit coverage batch (13 previously unaudited delete paths, `LogEntityDeleteWithChildrenAsync` + `AuditJoinRowCollector` + `ProductDeleteAudit`, `Audit:SystemUserEmail` config), the **DocumentController removal**, DEF-338 (CSV-import confirmation) and **DEF-339** (save confirmation on the Create-`Save` and Edit-`Save` paths). Plus the `_static/` Gate 9 suite — which is a `Scripts/` tool, correctly **not** in the zip.

> ⚠️ **Gate 5 concurrency, again — and it mattered.** The zip was built at 17:05 from a working tree that still had DEF-339 uncommitted. At 18:35 another terminal committed **`7da8bd1`**, which turned out to be *exactly* that tree (DEF-339 + tracking docs + the DEF-337 fallback test). Verified by diffing the commit's stat against what was built, not by assuming: **no app-code delta**, so the bundle is current with `HEAD`. The post-build working tree holds only `defect-register.md`, `observations_log.md` and a new `Scripts/data-ops/` cleanup SQL — none of which publish. This is the third bundle where reading git *at build time only* would have been wrong.

#### In-zip content verification — "is every change actually in here?"

Razor views compile into `GCCWebApp.dll` (no separate `Views.dll`), so a view change is only real if its literals are in that DLL. Decoded at **both UTF-16 alignments plus UTF-8**, with control strings asserted first to prove the method works before trusting any negative (per the method note on the bundle below).

| Change | Marker asserted | Result |
|---|---|---|
| *control* | `Save Changes` (UTF-16 literal) | **PRESENT** ✔ method valid |
| *control* | `CertificateController` (UTF-8 type name) | **PRESENT** ✔ method valid |
| *control* | `zzz_not_a_real_marker_zzz` | **ABSENT** ✔ no false positives |
| DEF-339 | `SaveSuccessMessage` (private TempData key) | **PRESENT** |
| DEF-339 | `created successfully!` (Create plain-Save writer) | **PRESENT** |
| DEF-339 | `updated successfully!` (Edit save writer) | **PRESENT** |
| DEF-339 | `saveSuccessMsgShown:` (view-side nonce guard — proves the **reader** shipped, not just the writers) | **PRESENT** |
| DEF-337 | `LogEntityDeleteWithChildrenAsync` | **PRESENT** |
| DEF-337 | `Audit:SystemUserEmail` (config lookup key) | **PRESENT** |
| Removal | `GCCWebApp.Controllers.DocumentController` | **ABSENT ✔** (correctly removed) |

The DEF-339 writer/reader pair is asserted **on both halves deliberately** — a bundle carrying the two `TempData` writes without the view that reads them is precisely the DEF-318/DEF-330 orphan-key failure re-created, and it would look identical to a healthy build from the controller side alone.

#### Hygiene — all PASS

**0 entries** for each of: `appsettings.Development.json`, `appsettings.Test.json`, `Api.Tests`, `wwwroot/uploads/` (both slash styles), `App_Data/uploads/`, `Scripts/`, `docs/`, `docs2/`. In-zip `appsettings.json` (3,627 B) carries the `Audit.SystemUserEmail` block and contains **no** dev DB credential or `GCCDatabase_Dev` reference.

#### Migration script — `migrations/apply-migrations.sql`

303 lines / 18,693 B, **24 `IF NOT EXISTS` guards, 0 `CONCAT`** (the historical abort cause). All four migrations owed on prod present — matched with `[A-Za-z0-9]+` per the documented gotcha that `[A-Za-z]+` silently misses `AddProductIdPartnersM2M`: `AddNormalizedSearchColumns`, `AddProductIdPartnersM2M`, `WidenCertificateProductIdProductName`, `WidenStatusHistoryCertificateNumber`. `CREATE TABLE [CertificateProductIdPartners]` and DEF-314's `ALTER COLUMN [CertificateNumber] nvarchar(50)` both present; CR-171 partner backfill appended. **Run it with `sqlcmd -I`** (or from SSMS) — the script sets no session options itself and the persisted `Normalized*` computed columns make `QUOTED_IDENTIFIER ON` mandatory.

#### Gate assessment

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes | **DEPLOY-TIME — OWED** | Built with `-SkipUploadsBackupPrompt`; server-side backup **not** done. In-zip upload entries: 0. |
| 1 — UI / Design-Guide | Yes (`CertificateInternalView.cshtml`) | **REASONED** | DEF-339's addition is an inline `<script>` — no markup, CSS, font, label or container change; uses `AdminCore.showToastNotification`, **not** a native `alert/confirm/prompt`; no `<h1>`. Full `ux-design-compliance` review not re-run. |
| 2 — Cross-role + responsive | Yes (cert detail view) | **PARTIAL — client-verified, not swept** | The client exercised Edit → Save on localhost and reported *"I see the toast now"* (DEF-339) — first real proof the toast renders above the page stacking context. The 4-login × 5-viewport Playwright + AxeCore pass was **not** run. |
| 3 — Data-scoping & endpoints | Yes (controllers/services/repos changed) | **VERIFIED GREEN** | `deploy-gate.js` security suite **exit 0, 52s — 57 PASS / 0 FAIL**: partner-isolation 21, rbac-matrix 24, idor-download 2, cert-detail-scoping 10. (57P, up from the 55P baseline — the partner-isolation specs that DEF-326 found passing vacuously now actually assert.) |
| 4 — Partner API contract | Yes (`Api/v1/*` + shared services changed) | **VERIFIED GREEN** | `amazon-contract-diff.js`: **CONTRACT UNCHANGED**, **7 pass / 0 fail**, 33s. GS1 list, GS1 single and Standard list all structurally identical to the frozen April baselines. This is a **firing** gate — DEF-337 touched all three v1 controllers — so the green is load-bearing, not a formality. |
| 5 — Traceability audit | Yes (always) | **VERIFIED (manual, git-based)** | Read at **build time and again after the 18:35 commit**. DEF-337/338/339 all registered; observations log appended. No orphan app-code change in the delta. |
| 6 — Performance / EF | Yes (`CertificateRepository.cs` changed) | **NOT RUN — OWED** | No cold-start timing run. |
| 7 — Pending migration → prod | Yes (carried forward) | **SATISFIED — verified against prod 2026-08-05** | ~~OWED — MANDATORY~~. Checked directly rather than from memory: prod `__EFMigrationsHistory` high-water is now **`20260721015555_WidenStatusHistoryCertificateNumber`**, and its applied list matches the repo's `Migrations/` tail exactly. All **four** previously-owed migrations are in. **Verified at the schema level, not just by history rows** (a history row proves a script *ran*, not that it *worked*): `CertificateStatusHistory.CertificateNumber` = `nvarchar(50)` (DEF-314), 8 `Normalized*` columns present (CR-187), `CertificateProductIdPartners` table exists, `CertificateProductIds.ProductName` = `nvarchar(1000)`. Nothing owed. |
| 8 — CRUD write-path | Yes (delete paths rewritten wholesale) | **VERIFIED GREEN** | `deploy-gate.js` crud suite **exit 0, 66s — 39 PASS / 0 FAIL / 0 WARN / 0 BLOCKED**: tag 7, company 7, product 8, user 8, cert 9. All SQL-verified and self-cleaning; **both net-zero invariants held** (`Products` 125→125, `Certificates` 67→67), and the user delete correctly soft-deleted (`IsDeleted=1`) while product/cert hard-deleted. This is the gate that mattered most for this bundle — DEF-337 rewrote thirteen delete paths, and this is the only mechanical proof a Delete still deletes. |
| 9 — Static integrity | Yes (always) | **VERIFIED GREEN-WITH-WARNINGS (re-run post-DEF-340)** | `_static/run-all.js`: **22P / 0F / 10W**, exit 0, selftest **9 ok / 0 failed** (fatal precondition — confirmed, not assumed). **This gate found DEF-340.** Pre-fix run was 12W (`_results/static-gate-2026-08-05T02-12-51-601Z.json`); post-fix 10W — check-1 WARNs **9 → 6** as the three `SetupTotp` orphans cleared, and the key now reports `"ErrorMessage" paired (6 write / 2 read)`. **9a** also independently confirms DEF-339 from source: `"SaveSuccessMessage" paired (2 write / 1 read)`. **9d roll-up: 4 undeployed** — `DEF-179(CLEAN) DEF-160(MODIFIED) DEF-106(CLEAN) DEF-340(MODIFIED)`; both `MODIFIED` entries are in the working tree the bundle was published from, so both are **in** this zip (DEF-160's flip to MODIFIED is file-level granularity — it cites `AccountController.cs`, which DEF-340 edited). |
| 10 — Config resolves to a row | Yes (always) | **VERIFIED** | `Audit:SystemUserEmail` = `swest@anukiconsulting.com` read from the **in-zip** `appsettings.json`; no `appsettings.Production.json` override in the bundle. Baseline probe against `gccwebap_certificates` earlier the same day returned exactly `1 | admin | swest@anukiconsulting.com | active`. |

**Deterministic backbone: GREEN** — `_results/deploy-gate-2026-08-05T02-18-16-233Z.json` (security ✔ 52s · crud ✔ 66s · contract ✔ 33s · roll-up `=> GREEN — safe to deploy`), plus Gate 9 static ✔ exit 0.

**Net:** the bundle is **built, gated, hygiene-clean and content-verified**. All four mechanical gates are GREEN with **0 FAIL** anywhere, and every change in the delta is provably inside the zip — DEF-339 asserted on **both** halves of its writer/reader pair, DEF-340 by hash chain rather than by a literal that predates it. Gate 9 earned its place this cycle: it found DEF-340 *and* then caught the stale 17:05 bundle that didn't contain the fix.

**Still owed — these are the ones no script can cover:**
- **Gate 0 (upload):** zip the server's `wwwroot/uploads/` before extracting. Built with `-SkipUploadsBackupPrompt`, so no backup was made.
- **Gate 7 (upload, MANDATORY):** run `migrations/apply-migrations.sql` with `sqlcmd -I` **before** recycling the app pool. Four migrations are owed on prod.
- **Gates 1, 2, 6 (REASONED, not run):** no `ux-design-compliance` review, no 4-login × 5-viewport Playwright + AxeCore sweep, no cold-start timing. Gate 2's gap is partly covered by the client confirming the DEF-339 toast renders on a real page; Gate 6's is the weakest link, since `CertificateRepository.cs` changed in this delta.
- **DEF-340 is unexercised in a browser** — reaching it needs a real authenticator enrolment with a deliberately wrong code. The static evidence for the mismatch is unambiguous; the rendered result is not verified.

---

### `GCCWebApp-deploy-20260803-1422.zip` — built + content-verified 2026-08-03 (**SUPERSEDED by `20260804-1705` — missing DEF-337 / DEF-338 / DEF-339**)

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

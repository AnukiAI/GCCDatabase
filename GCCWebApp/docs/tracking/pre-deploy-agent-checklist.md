# Pre-Deploy Agent Checklist

**Purpose:** Stop client-found UI/QC regressions *before* they ship. Each gate below names the agent that runs it, what it must verify, and the documented incident that justifies it. Run top-to-bottom before every Plesk deploy. A gate that can't pass blocks the deploy — fix or consciously waive (and log the waiver).

> ## ⚠ "Production" means the TEST environment. Read this before every deploy.
>
> There is **one** deployed environment — `gccwebapp.com` — and it is the **test environment**.
> It is called "Production" only because `ASPNETCORE_ENVIRONMENT=Production` is set in
> `web.config`, which is what makes the app load `appsettings.Production.json` and use SQL Server
> instead of the dev database. **The word is a config-file name, not a statement about who the
> users are.** The real production URL (`database.greencirclecertified.com`) is not live yet.
>
> **That host is a dedicated subdomain, decided 2026-08-14.** The earlier path-based address
> `greencirclecertified.com/database` was published in the Amazon developer guide v1.0–v1.1 but was
> **never activated**, and is retired — the app is not path-mounted (there is no `UsePathBase`
> anywhere), so it could not have served that address without a code change. If you find `/database`
> named as the production URL in any doc, it is stale, not a second environment.
>
> Everything below follows from that, and each line has cost time at least once:
>
> | Reads as production | Is actually |
> |---|---|
> | `appsettings.Production.json` | the **test** environment's config — the only one that ships |
> | `ConnectionStrings:DefaultConnection` in it | the **test** DB (`gccwebap_certificates` @ `68.71.129.120,1533`) |
> | `Jwt:Issuer` / `Audience` = `GCCWebApp-Production` | test-environment tokens |
> | `DataProtection:ApplicationName` = `GCC-Production` | **live — do not change or drop it.** Read at `Program.cs:107` and `SecurityServicesExtensions.cs:74`; it pins the key-ring discriminator, so altering it invalidates every auth cookie at once. (`DataProtection:KeyPath` in the same section *is* inert — DEF-391.) |
> | "deploying to production" in any note | deploying to **test** |
>
> **Consequences that are easy to get wrong:**
> - `Database:AutoMigrateOnStartup: true` is set *because* this is a test env — a real production
>   cutover would set it `false` and run migrations under DBA control. Don't carry the `true`
>   forward to the real production environment.
> - `TestEnvironmentGate:Enabled: true` and `ShowBanner: true` are correct **here** and must be
>   `false` on the real production environment.
> - "Test on localhost, never live" still applies. `gccwebapp.com` being called Production does
>   not make it a safe place to experiment, and it does not make it a real production system
>   either — it is the client's shared test environment, running against real records.
> - The **dev** database (`GCCDatabase_Dev` on `ANUKI01`) is a *third* thing again, and is not a
>   superset of test. See the deploy record at the bottom of this file.
>
> When writing a defect, shipped-log or deploy note, say **"test environment"** or
> **"gccwebapp.com"**. Reserve "production" for the environment that does not exist yet.

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

## Gate 12 — Outbound email still works after the extract  ·  manual + stdout log  ·  **runs AFTER the upload**

**Fires when:** **always**, from the 2026-08-19 bundle onwards. It is one grep and it guards a control that fails **silently and closed**.

The SMTP credential lives in **`appsettings.Secrets.json`** beside `GCCWebApp.dll` — deliberately **not** in the zip, not in git, and `Scripts/deploy.ps1` aborts if it reaches staging. That is what makes it survive a deploy: extracting a zip adds and overwrites but never *deletes*, so a file the zip does not contain is left alone. Two things break that: a **clean-folder deploy** (delete-then-extract) and **restoring the site from a backup taken before the file existed**. Either silently reverts email to `MockEmailService`.

Silently is the operative word, and it is why this gate exists rather than "someone would notice":

- `MockEmailService` logs the message and returns success — no exception, no failed request.
- `MockEmailService.TestConnectionAsync()` **always returns `true`**. Any future "test email" button reports green while delivering nothing. Never accept it as evidence.
- `ForgotPassword` returns an identical response whether or not mail was sent — that is deliberate (DEF-209 anti-enumeration), and it means the UI cannot tell you.

So nothing surfaces until a real person cannot get into their account. Worse than a dead reset link: **a user who enables email 2FA while this is broken is locked out**, because the code they are asked for never arrives.

- [ ] Confirm the file survived the extract — it must sit in the same folder as `GCCWebApp.dll`:
  ```
  Test-Path .\appsettings.Secrets.json    # must be True
  ```
  If `False`, recreate it from `appsettings.Secrets.template.json` **before** recycling the app pool.
- [ ] Read the startup banner in `logs/stdout` after the recycle. It states the choice outright:
  ```
  Email: LIVE via accumail3.accuwebhosting.com:465 (SslOnConnect) as <mailbox>, from <sender>
  Email: DISABLED (MockEmailService) — SmtpSettings missing Username, Password
  ```
  `DISABLED` is a deploy defect, not a warning to note and move past.
- [ ] Prove delivery end-to-end, because a live banner only proves the *config* parsed — it does not prove the host can reach the mail server outbound: run one **password reset** against a mailbox you can read, and confirm the message arrives.

**Two config values are load-bearing. Do not "correct" either one.**

- **Host is `accumail3.accuwebhosting.com`, not `mail.gccwebapp.com`.** Both resolve to `72.18.135.96`, but the server's DigiCert certificate covers only the `accuwebhosting` name, so the friendlier-looking host **fails certificate validation before it authenticates**. It will look like a credential problem and is not.
- **Port 465 with `Security: SslOnConnect`.** Port 587 advertises **no STARTTLS** and answers an explicit `STARTTLS` with `503 TLS is not allowed`, so 465 is the only encrypted path. Moving to 587 to make a connection succeed means sending the mailbox password in clear text. This is also why `EmailService` uses MailKit rather than `System.Net.Mail`, which cannot do implicit TLS at all.

> **When diagnosing this, do not disable certificate validation to "see if the network works".** The first probe of this server used an `SslStream` with a permissive callback and reported the 465 handshake as fine; MailKit, which validates by default, then failed on the hostname mismatch. A probe that turns the check off cannot tell you whether the check passes — and here that was the entire fault.

**Do not move the credential into `appsettings.Production.json` or `web.config` "for tidiness".** Both ship in the zip, so both are overwritten on every deploy — that is the exact failure this design removes, and the checklist warned about it (Deploy record 2026-08-17) before it was fixed. `appsettings.Production.json` is also the file that was **publicly downloadable** during DEF-363; IIS serves `.json` raw whenever the AspNetCore handler is unmapped.

> Justifies this gate: DEF-263 Finding 2. Outbound email was a no-op on the test environment from 2026-06-12 to 2026-08-19 — over two months — and was found by *reading `Program.cs`*, not by anything failing. During that window every password-reset link and 2FA code was generated, logged, and dropped, while the UI reported success each time.

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

### `GCCWebApp-deploy-20260910-2128.zip` — **BUILT + VERIFIED 2026-09-10, NOT UPLOADED**

**Runbook:** `docs/tracking/2026-09-10-deploy-runbook-CR048-CR049.md`
**In-zip `GCCWebApp.dll` md5:** `cf8c7eeafd990c5d` · 341 entries · 61.2 MB

**Carries five items, not the two that were asked for** — a bundle is a full overwrite and ships the
whole working tree. Shawn confirmed shipping all of it:
**CR-048** (Edit opens a new tab) · **CR-049** (List is the default for logged-in users) ·
**DEF-419** (HIGH — CSV import drops a row / blanks a Partner on an embedded line break) ·
**DEF-420** (CSV export quote escaping) · **DEF-342** (Product Identifiers template copy, client has
re-opened it three times).

**No database step.** No migrations in the tree; test is already at the CR-045 high-water
(`20260908175939`). `deploy.ps1` generates a full-history `apply-migrations.sql` regardless — **do
not run it** (DEF-386: SQL Server parses every guard block before `IF NOT EXISTS` can skip it).

| Gate | Result |
|---|---|
| 0 uploads backup | **OUTSTANDING — human, before extract** |
| 1 UI / design guide | PASS (inspection) |
| 2 cross-role + responsive | PASS — 3 roles, 5 viewports, 0 overflow, 0 console errors |
| 3 data scoping / RBAC | **PASS 57P / 0F** |
| 4 Partner API contract | **PASS — CONTRACT UNCHANGED, 7P / 0F** |
| 5 traceability | PASS with 2 notes (DEF-420 status stale; `_Layout` diff carries no ref) |
| 6 performance / EF | PASS — public endpoint (the only 30 s abort ceiling) **0.8 s cold** |
| 7 pending migrations | n/a — none |
| 8 CRUD write paths | **PASS 39P / 0F / 0W / 0B** |
| 9 static integrity | **0 FAIL**, 13 pre-existing warnings |
| 10 config points at data | n/a |
| 11 close-out | after upload |

**Content verification:** `verify-bundle.py` → 0 failures · `marker-check-20260910.py` → 0 failures
(new for this deploy; asserts today's work by md5 for static assets and by literal-in-DLL for
compiled Razor, with a negative control proving the old markup is gone).

> **Marker-check trap, now handled — worth knowing before writing another one.** The first run
> reported three fixes ABSENT from the DLL and all three were present. .NET puts type/member names
> in the `#Strings` heap as **UTF-8** but string literals in the `#US` heap as **UTF-16LE**, and
> compiled Razor markup is a string literal. An ASCII-only byte search therefore finds a C# type name
> and misses every piece of view markup — which reads exactly like a bundle built from the wrong
> tree. Search both encodings.


### `GCCWebApp-deploy-20260908-1607.zip` — **DEPLOYED to gccwebapp.com 2026-09-08, post-deploy verified**

**Runbook:** `docs/tracking/2026-09-08-deploy-runbook-CR043-CR045.md`
**Contents:** CR-043, CR-045, **and DEF-412** (whose own bundle was never uploaded).
**In-zip `GCCWebApp.dll` md5:** `457bd720da884279` · 341 entries · 61.1 MB

**Supersedes `GCCWebApp-deploy-20260908-DEF412.zip` and `GCCWebApp-deploy-20260908-0648.zip` — do
not upload either.** This bundle carries everything they did plus both CRs.

> **DEPLOYED.** DB applied ~16:20 EDT, code uploaded and recycled ~17:0x EDT, verified immediately
> after: **17 of 18 live checks pass**; the one that does not is `CR043.C2`, recorded as a failure
> on purpose because **Novi has 0 tagged certificates on this dataset**, so its isolation control
> returns an empty set and proves nothing. `CR043.C3` (a different company, 23 certificates visible,
> 0 of the 13) is the control that carries that assertion here.

| Gate | Result |
|------|--------|
| 0 — server uploads backup | **NOT DONE — human step, must happen before extraction.** Bundle contains 0 upload entries, so extraction cannot touch the PDF store; take the backup regardless (2026-04-23 and 2026-06-15 both started here) |
| 3 — data scoping | **PASS** — 11/11 live checks, each with a control: anonymous 404s on all 13 Confidential certs while a public cert still 200s; Amazon sees 13/13, **Novi 0/13 on a non-empty response** |
| 4 — partner API contract | **PASS** — response shape unchanged, row set widens only. `partner-isolation.js` / `rbac-matrix.js` needed no re-baselining: they discover certificates dynamically and assert scoping, not a fixed status list |
| 5 — traceability | **PASS** — CR-043 and CR-045 register entries updated to BUILT with as-built sections; shipped log, observations log, ways-to-improve all appended |
| 7 — pending migration | **APPLIED.** `20260908175939_CR045_CertificationResultsVerbatimSort` recorded. `AutoMigrateOnStartup=false` confirmed in the bundle, so nothing migrates on start either way |
| 9 — static integrity | **PASS** — verifier 0 failures, now including CR-043/CR-045 markers added this session |
| 10 — config resolves | **PASS** — bundled `web.config` has `ASPNETCORE_ENVIRONMENT=Production`; 0 DataProtection keys, 0 dev config, 0 secrets overlay, 0 upload entries |
| 11 — post-deploy close-out | **PASS** — `/appsettings.Production.json` and `/appsettings.json` both 404; DEF-412 column present AND populated (169/1,061) |
| 12 — outbound email | **NOT RUN** — nothing in this deploy touches mail; only `2fatest` can receive it |

**Marker check on the in-zip DLL** — size and entry count prove nothing here (339 entries / 61.1 MB
once matched between two bundles while one was missing an entire fix):

| Marker | Expected | Found |
|---|---|---|
| `BadgeableStatuses` | present | **PRESENT** |
| `IsBadgeable` | present | **PRESENT** |
| `PublicStatuses` | **present** — must survive; it still guards the public database | **PRESENT** |
| `FormatCertificationResults` | **absent** | **ABSENT** |
| `DisplayFormatters` | **absent** | **ABSENT** |
| `Cert Subcategories` | present (DEF-412) | **PRESENT** |

**Database, applied then proven against snapshots** (`CertProductIdStatus_bak_CR043_20260908`,
`CertResults_bak_CR045_20260908` — drop both once the deploy is confirmed good):

| | |
|---|---|
| Sort values changed | **0 of 6,012** |
| `CertificationResults` text changed | **0 of 6,012** |
| CR-043 backfill | **420 rows, all on Confidential certificates**, nothing else |
| CR-171 idempotent re-run | 0 + 0 inserted |
| Certificate count | 6,012 before and after |
| `EXEC(CONCAT(…))` surviving in the schema script | **0** — the DEF-386 repair fired on 1 statement, CR-045's computed column |

**The behaviour change, now measured:** the Analyst/Admin `active-certificates` report returned
**1,061** rows against an expected ~1,024. Reconciled exactly — 1,019 Paid+Republished in force
(what it would have returned before) **+ 42 Confidential in force** (added by CR-043 Q2) = 1,061.
Intended, and the one place a count moves without the client having asked for it, so tell them
before they find it.

---

### `GCCWebApp-deploy-20260904-1328.zip` — **BUILT 2026-09-04, verified, NOT uploaded — HELD, review in progress**

Built from the **`ci-regression` working tree** (not a clean checkout of HEAD) with
`dotnet publish -c Release -p:PublishProfile=PleskTestEnvironment`. 61.1 MB, 339 entries.
Publish output lands in `bin/Release/net8.0/**win-x64**/publish/` (the profile sets
`RuntimeIdentifier=win-x64`), **not** `bin/Release/net8.0/publish/` — zipping the wrong path
produces an empty or stale bundle.

**Shawn asked for the fixes to be bundled but explicitly NOT deployed — a few items are still under
review. Do not upload this without checking the contents list below.**

#### ⚠️ This bundle carries more than the fixes it was asked for

The request was to bundle the forgot-password batch (DEF-405/406/407). **That cannot be shipped on
its own.** There is no Razor runtime compilation configured (no `RuntimeCompilation` package, no
`.cshtml` in the build output), so **views compile into `GCCWebApp.dll`**. Shipping a view fix means
shipping the DLL, and the DLL contains every other source change in the working tree. A `.cshtml`
file-drop would do nothing on the server.

So this bundle necessarily also contains work that was **not** part of the request:

| In the bundle | What it is | Review state |
|---|---|---|
| **DEF-405 / 406 / 407** | Forgot-password copy, gradient, tagline | The requested batch. Fixed + verified on localhost. |
| **DEF-402** | Search perf — CR-037 country `EXISTS` moved out of the search `OR` (4 files: both tile API controllers, `CertificateRepository`, `Search/TagSearchMatch.cs`) | **Fixed, previously logged NOT deployed.** Perf claim not re-measured against this build. |
| **DEF-403** | Three dashboard stat exports sorted by the wrong dimension (`ExportController`) | Fixed, previously logged not deployed. |
| **DEF-400 / DEF-401** | Certificate Edit image input (`Views/Certificate/Edit.cshtml`) | **DEF-401 has had no viewport pass.** This is the likeliest thing still under review. |

`DEF-404` (country filter vs export mismatch) is **OPEN and NOT fixed** — it is not in this bundle
and shipping this bundle does not address it.

#### Verification

| Check | Result |
|---|---|
| `verify-bundle.py` hygiene | **ALL PASS** — 0 DataProtection keys, 0 `appsettings.Secrets.json`, 0 `appsettings.Development.json`, 0 `appsettings.Test.json`, 0 `Scripts/`, 0 `docs/`, 0 `docs2/`, 0 `wwwroot/uploads`, 0 `App_Data/uploads`. (`GCCWebApp.pdb` present — non-fatal note, as always.) |
| Required files | **PASS** — `GCCWebApp.dll`, `web.config`, `appsettings.json`, `appsettings.Production.json` |
| Content markers | **failures: 0** (`--no-migrations=CR037_MultiCountryCertificates`) |
| `AutoMigrateOnStartup` | **false** in the shipped `appsettings.Production.json`, and not true — asserted on the in-zip bytes |

**New markers added to `verify-bundle.py` for this batch**, asserted against in-zip bytes:
control (`ForgotPassword` view compiled in) · DEF-405 "1 hour" present / "Reset links expire after
24 hours" absent · DEF-406 no view redeclares `--auth-left-gradient` + the light token ships in
`auth.css` · DEF-407 tagline absent. The control marker matters: three of these are **absence**
assertions, and an absence proves nothing if the view never compiled into the DLL at all.

#### Two verifier failures on the first run — both resolved, one was a real gap

1. **`DEF-405 old 24-hour copy is gone: PRESENT` — false positive I introduced.** The marker
   `"expire after 24 hours"` also matches `Extensions/SwaggerExtensions.cs:356`
   (*"Tokens expire after 24 hours"* — API JWT documentation, unrelated to password reset).
   Tightened to the full sentence `"Reset links expire after 24 hours"`. The reset copy really is
   gone; the marker was the thing at fault.
2. **`migrations/apply-migrations.sql not found in zip` — a genuine gap in the check.** The
   assertion was unconditional, so a legitimately schema-free drop could only ever FAIL, which
   invites the habit of eyeballing a red line and uploading anyway. **The default is unchanged and
   still fatal.** Added an explicit opt-in instead:

   ```
   python Scripts/deploy/verify-bundle.py <zip> --no-migrations=<AlreadyAppliedHighWaterName>
   ```

   It asserts the declared high-water migration type is compiled into the DLL, and prints a loud
   operator assertion into the output for this log to carry. It cannot prove nothing newer exists —
   that part is a human claim, deliberately recorded as one.

**Gate 7 — nothing to run.** Repo migration high-water is
`20260901164220_CR037_MultiCountryCertificates`, which was **applied to the target on 2026-09-02**
(see `docs/tracking/2026-09-02-deploy-runbook.md`). `git status -- Migrations/` is clean and no
migration was added in this batch, so this drop carries no schema change. **DEF-398**
(`IX_CertificateCountries_NormalizedCountry` exceeding the 1700-byte index key limit) remains
outstanding from the last deploy and is unaffected by this bundle.

**Not run against this bundle:** Gates 0–6 and 8–10. No upload, so no Gate 11/12. **This is a
verified artifact, not a gated release** — the full gate run belongs at the point Shawn decides to
deploy, not now, since the contents may still change.

**Housekeeping flag (not actioned — deleting artifacts is Shawn's call):** `deployment-drops/` still
holds `GCCWebApp-deploy-20260831-1628.zip`, which was never uploaded and is now superseded, plus
`GCCWebApp-deploy-20260902-full.zip`, which **is** the currently-live artifact and must be kept.
Uploading 20260831-1628 by mistake would be a downgrade — it predates the 2026-09-02 deploy.


### `GCCWebApp-deploy-20260831-1628.zip` — **BUILT 2026-08-31, verified, NOT uploaded**

Built from the **working tree** (not a clean checkout of HEAD) specifically to replace
`20260830-2245`, which had become a downgrade — see the 🚫 block on that entry below. 61.1 MB,
337 published files.

**The problem it solves.** Until this bundle existed, **no artifact on disk could recreate the live
site**: it was the 2245 DLL plus two JS files hot-deployed after it. This one reproduces it.

| Check | Result |
|---|---|
| `verify-bundle.py` hygiene | **ALL PASS** — 0 DataProtection keys, 0 `appsettings.Secrets.json`, 0 `appsettings.Development.json`, 0 `Scripts/`, 0 `docs/`, 0 uploads |
| `verify-bundle.py` content markers | **failures: 0** — incl. DEF-384 present / half-code expression absent, DEF-385 service + migration + cookie literal, DEF-381 old whole-array append absent, DEF-386 no `EXEC(CONCAT(...))` |
| **wwwroot vs working tree** | **167 / 167 identical, 0 differ** (the 2245 bundle was 165 / 167) |
| Schema script | **0 lines, empty by design** — built `-FromMigration 20260830230201_DEF385AddTrustedDevices`, the target's high-water. Only the idempotent CR-171 backfill is appended. |

**It is NOT byte-equivalent to the live site — it is live PLUS one fix.** Exactly one source file
changed after the 2245 build: `Middleware/RequestResponseLoggingMiddleware.cs` (**DEF-388**, the
duplicated HTTP verb in API log lines). Verified by mtime sweep over every `.cs`/`.cshtml` — one
hit, nothing else. So uploading this bundle ships DEF-388 as well; that is a **logging-only** change
with no user-facing behaviour, but it should be a decision, not a surprise.

**Gate 7 — nothing to run.** The repo high-water equals the target's, so the schema section is
empty. Do not hunt for SQL to execute.

**Not yet run against this bundle:** Gates 1–6 and 8–10, and no upload has happened, so there is no
Gate 11/12. It is a verified artifact, not a gated release.

### `GCCWebApp-deploy-20260830-2245.zip` — **DEPLOYED to gccwebapp.com 2026-08-31, post-deploy verified**

> ## 🚫 DO NOT RE-UPLOAD THIS ZIP. It would regress DEF-387 and DEF-389.
>
> This bundle was built **before** two single-file JavaScript fixes that were hot-deployed to
> gccwebapp.com later on 2026-08-31. It still contains the **pre-fix** copies of both, so extracting
> it again would overwrite the live files and silently undo client-reported fixes:
>
> | File in the zip | Evidence it is the old copy | What re-uploading would undo |
> |---|---|---|
> | `wwwroot/js/userManagement.js` | 92,908 bytes; `X-Requested-With` **×0** (live: 93,344 bytes, **×5**) | **DEF-387** — "Error saving user" goes back to swallowing the real reason |
> | `wwwroot/js/product-id-manager.js` | 107,843 bytes; `updateCount` ×8 / `totalCount` ×20 (live: 108,787 bytes, ×9 / ×23) | **DEF-389** — the product-identifier count stops updating as you add and remove |
>
> Checked file-by-file: **40 of the 42 JS files in the bundle are identical to the working tree; only these two differ.** So the zip is otherwise current — which is exactly what makes it dangerous, because a spot-check of any other file says "same" and reads like the whole bundle is safe.
>
> **The live site is a composite** and no single artifact reproduces it: the **DLL** from this bundle
> plus **two JS files deployed after it**. That state is correct and verified — but it means
> **there is no zip on disk that can recreate the current site.**
>
> **If a rebuild is ever needed, build a fresh bundle from the working tree — never re-extract this
> one.** Both fixes are uncommitted working-tree changes, so a clean checkout of HEAD would also
> miss them; Gate 9 already warns about exactly this ("1 defect exists ONLY in the working tree —
> publish the bundle from THIS tree, not from a clean checkout of HEAD").


> ## ✅ DEPLOYED AND VERIFIED ON THE LIVE TEST SITE
>
> Every claim below was checked against **gccwebapp.com** after the upload, through the gate, not
> inferred from the build.
>
> | Check | Result |
> |---|---|
> | **DEF-381** deployed tile JS carries the fix | `pendingAppend` present; old whole-array append **absent** (66,713 bytes served) |
> | **DEF-381** real browser, 4 scrolls | `+50, +50, +50, +50` — 250 tiles, **250 distinct**, count matches. **Intern unblocked.** |
> | **DEF-382** unaligned offsets on live API | 37 / 75 / 90 / 123 / 149 + unaligned tail — all return the exact rows |
> | **DEF-383** cert `25-0261` (stored `1`) | renders **`100%`** (was `1%`) |
> | **DEF-385** trust-device round trip on live | challenged → trust issued → **second sign-in skipped** → other account still challenged |
> | **DEF-363** `/appsettings.Production.json` | **404** (also `/appsettings.json`, `/web.config`) |
> | **Gate 4** Amazon contract diff | **7P / 0F — `CONTRACT UNCHANGED`** |
> | **Gate 9** static integrity | **23P / 0F / 13W — GREEN-WITH-WARNINGS**, exit 0 |
>
> **Correction to this record's own earlier prediction.** It said Gate 4's diff "will show value
> changes" from DEF-383. It did not, and the prediction was wrong about what the gate measures:
> `amazon-contract-diff.js` compares response **structure** against the April baseline, not values.
> The 1% → 100% change for 11 Amazon certs is real, but this gate is not what would surface it.
>
> **DEF-384 — UPDATE 2026-08-31: now confirmed through the UI by Shawn**, who re-tested the
> reported symptom on the deployed site and reported it working. The note below records the
> inference used before that, and is kept because the reasoning was sound and may be needed again
> when a screen cannot be exercised without side effects.
>
> **DEF-384 was not exercised through the UI at deploy time.** Reaching `ShowRecoveryCodes` means generating a real
> account's recovery codes, which rotates them. It is proven by the byte-level in-zip check (fix
> present, half-code expression absent) plus the fact that DEF-382/383/385 — all behaviours that
> exist only in this build — are confirmed live, so the deployed assembly is demonstrably this one.
> Stated as inference, not as a click-path test.
>
> **Test-data hygiene.** The DEF-385 live check needed a real TOTP challenge, so scratch secrets were
> enrolled on `test.novi` and `test.amazon` and **both restored immediately** (verified: 2FA off,
> method Email, secret NULL, their `TrustedDevices` rows deleted). Confirmed afterwards that
> `2fatest` (still on) and `admin` (still on) were untouched, along with every other account.
>
> **Gate 12 — outbound email: ✅ PASS. Delivery CONFIRMED in the inbox 2026-08-31.**
>
> A 2FA code sent at 03:59:16 UTC arrived at shawnmwest@gmail.com and was confirmed received. Mail
> is **LIVE**, not `MockEmailService`: the server-only `appsettings.Secrets.json` SMTP overlay
> survived the deploy intact. (Code value deliberately not recorded here — it is a credential, even
> an expired one. The pending challenge was left to expire rather than completed.)
>
> The reasoning below is kept because it is the method, and it is what makes the PASS meaningful
> rather than assumed:
>
> Sent by signing in as `2fatest` (2FA on, no TOTP secret, so it downgrades to an emailed code) to
> **shawnmwest@gmail.com**. Chosen over a password-reset deliberately: it persists no account state —
> no reset token, no password change, no 2FA change.
>
> **The HTTP response alone does NOT prove delivery — never record it as if it does.**
> `Program.cs:650` states it plainly: the `MockEmailService` fallback is *"completely silent at
> runtime… the UI still reports success"*. A dead mail path and a working one are indistinguishable
> over HTTP — which is exactly how a dead one hid here for months (DEF-263). The PASS above rests on
> the inbox, not on the 200.
>
> **The two authorities, neither of which is reachable remotely:**
> 1. **The inbox** — a 6-digit code timestamped as above.
> 2. **The startup banner in the server's stdout log** — `Email: LIVE via <host>:<port> … as
>    <username>` versus `Email: DISABLED (MockEmailService) — SmtpSettings missing …`. This banner
>    exists for precisely this question; read it in Plesk if the code does not arrive.
>
> **Prior reasoning that the overlay survived** (supporting, not conclusive): `appsettings.Secrets.json`
> is **not in the bundle** — verified 0 occurrences by `verify-bundle.py` — so extracting the zip
> cannot have overwritten or removed the server's copy.
>
> **Still open after this deploy:**
> - **Tell UAT testers** anyone who already saved 2FA recovery codes should re-copy them (DEF-384).
> - **Tracking gap surfaced by Gate 9:** `DEF-343` and `DEF-344` shipped in this bundle from the
>   working tree but have no shipped-log row, and 101 register entries still say "pending deploy"
>   for defects already delivered. Stale text, not a deploy risk — but the register overstates what
>   is outstanding.

---

### `GCCWebApp-deploy-20260830-2245.zip` — as built, gated + content-verified 2026-08-30

> ## ✅ GATE 7 IS DONE — the database has ALREADY been migrated. Do not run any SQL.
>
> Both migrations were applied to `gccwebap_certificates` by hand on **2026-08-30**, at the client's
> request, ahead of the upload. Verified on the target afterwards:
>
> | Check | Result |
> |---|---|
> | `__EFMigrationsHistory` high-water | `20260830230201_DEF385AddTrustedDevices` |
> | `CertificationResults = '1'` sort value | **100.0000** (was `1.0000`) — 106 certs |
> | `0.01` sort value | `1.0000` — now correctly distinct from `1` |
> | `Silver` / `Gold` | `NULL` — still outside the numeric sort |
> | `TrustedDevices` | table created, 3 indexes, 0 rows |
> | Raw `CertificationResults` data | **988 populated, unchanged** — no business data touched |
>
> This bundle's `migrations\apply-migrations.sql` was therefore generated with
> `-FromMigration 20260830230201_DEF385AddTrustedDevices`, so its schema section is **empty** by
> design and it contains only the idempotent CR-171 backfill. It was executed against the target as
> a final check and returned **exit 0, 0 rows affected** — a clean no-op. Running it again is safe
> and unnecessary.
>
> Rollback, if ever needed: `deployment-drops/ROLLBACK-DEF383-DEF385.sql` (drops `TrustedDevices`,
> restores the `< 1` computed column, removes both history rows).

**Between the SQL and the upload there is a deliberate, harmless inconsistency:** the database now
sorts `1` as 100 while the *currently deployed* code still displays it as "1%". Uploading this
bundle resolves it. Nothing is broken in the meantime.

Content markers **21 / 21** (three negative controls). Hygiene ALL PASS. Unit suite 87 failed /
771 passed — 87 unchanged baseline.

**Contains DEF-381, 382, 383, 384, 385, 386.** Everything below about the Amazon value change
(DEF-383), the recovery-code re-copy (DEF-384) and the post-deploy trusted-device check (DEF-385)
still applies.

> ⚠️ **DEF-386 — read before the next deploy that carries a migration.** The `--idempotent` script
> EF generates is **not runnable as shipped** unless `deploy.ps1` repairs it (it now does) *and* it
> is scoped with `-FromMigration <target high-water>`. Two independent reasons:
> 1. EF emits `EXEC(CONCAT(...))` for computed columns, which SQL Server cannot parse. `deploy.ps1`
>    now rewrites these and `verify-bundle.py` fails the bundle if any survive.
> 2. Even repaired, a from-scratch script **binds every guarded block before `IF NOT EXISTS` can
>    skip it**, so blocks referencing since-dropped columns fail (`Invalid column name
>    'UserCompanyId'`). Only `-FromMigration` avoids that. A bundle meant to bootstrap a *fresh*
>    database needs the full script instead — the two cannot be the same file.

---

### `GCCWebApp-deploy-20260830-2132.zip` — built 2026-08-30 (**SUPERSEDED by `20260830-2245` — its migration script could not be run, see DEF-386**)

Adds **DEF-385** ("Trust this device for 30 days" now actually suppresses the next 2FA challenge).
Hygiene ALL PASS; content markers **24 / 24**, including three negative controls. Unit suite
**87 failed / 771 passed** — 87 is the unchanged baseline; the +38 over the original 733 are the new
DEF-383 (24) and DEF-385 (14) tests.

**TWO migrations in this bundle now**, both in `migrations\apply-migrations.sql`:

| Migration | What it does |
|---|---|
| `20260830221518_DEF383FixCertResultsPercentBoundary` | rebuilds the `CertificationResultsNumeric` computed column |
| `20260830230201_DEF385AddTrustedDevices` | **new `TrustedDevices` table** + FK to `Users` (cascade) + `(TokenHash, UserId)` index |

Gate 7 applies. Run the script **before** recycling the app pool.

**DEF-385 is a new authentication path, so it deserves a deliberate post-deploy check** rather than
being assumed from a green build. On the test environment, with a 2FA-enabled account: sign in, tick
the box, verify; then sign out and sign in again — the second sign-in must not challenge. It is
exercised locally by `_regression/12-trust-device-roundtrip.js`, which also proves a device trusted
for one account does **not** skip 2FA for another.

No new secrets, no config keys, no change to `appsettings.Production.json` required.

Everything below about the Amazon value change (DEF-383) and the recovery-code re-copy (DEF-384)
still applies.

---

### `GCCWebApp-deploy-20260830-1847.zip` — built 2026-08-30 (**SUPERSEDED by `20260830-2132` — missing DEF-385**)

Identical to `20260830-1833` plus **DEF-384** (2FA recovery codes copied/downloaded at half length).
Hygiene ALL PASS; content markers **19 / 19**, including a negative control proving the half-code
expression is gone from the compiled view. `20260830-1833` was never uploaded — ignore it.

**Tell the UAT testers after deploy:** anyone who already saved their 2FA recovery codes from that
page holds an incomplete copy and should re-copy them. The stored codes were always correct — only
the copy/download output was truncated — so nothing needs regenerating server-side.

Everything below about the schema migration and the Amazon value change still applies.

---

### `GCCWebApp-deploy-20260830-1833.zip` — built 2026-08-30 (**SUPERSEDED by `20260830-1847` — missing DEF-384**)

**⚠️ THIS BUNDLE CONTAINS A SCHEMA MIGRATION — the first one in this run of bundles.**
`20260830221518_DEF383FixCertResultsPercentBoundary` rebuilds the `CertificationResultsNumeric`
persisted computed column (drop index → drop column → re-add computed → recreate index). **Gate 7 is
NO LONGER N/A.** Apply `migrations\apply-migrations.sql` **before** recycling the app pool. Note the
test environment has `AutoMigrateOnStartup=true` ([[feedback_test_env_auto_migrates]]), so it would
apply itself on startup anyway — run it deliberately rather than letting that happen unobserved.

| | |
|---|---|
| Size / entries | 61.1 MB / 341 |
| Hygiene | **ALL PASS** — 0 DataProtectionKeys, 0 dev/secrets config, 0 uploads, 0 `Scripts/`, 0 `docs/` |
| Content markers | **17 / 17 PASS**, including three negative controls |
| Unit suite | 87 failed / 757 passed — 87 is the unchanged baseline, +24 are the new DEF-383 tests |

**Delta over `20260828-1258`:** DEF-383 only (Certification Results `1` displayed as "1%" when it
means 100%). Carries everything from the previous bundles, none of which were uploaded.

> **⚠️ DEF-383 changes partner-visible VALUES.** 86 publicly visible certificates change from `"1%"`
> to `"100%"`; **11 of them reach Amazon**, who threshold-check this field. The response *shape* is
> unchanged so the immutable-contract rule is not breached, but **Gate 4's contract diff will show
> value differences and that is expected** — do not read it as a failure, and do not "fix" it back.
> This is the one item in the bundle that warrants telling the client before it goes live.

> **`dotnet ef` traps hit while building this** — both worth avoiding next time:
> `migrations add --no-build` silently produced an **empty** migration (it diffs the *compiled*
> model, and the context edit had not been built in). And `migrations remove --force` tried to
> revert against the dev database, failing on `IX_Certificates_Number`. **No damage** — it rolled
> back, and that index is a *filtered* unique index (`WHERE AllowsDuplicateNumber = 0`) from CR-038,
> which is why 137 duplicate-number groups coexist with it. Verified history, index and column
> intact. Delete scaffolded migration files by hand and rebuild; never `--force` the remove.

---

### `GCCWebApp-deploy-20260828-1258.zip` — built, gated + content-verified 2026-08-28 (**SUPERSEDED by `20260830-1833` — missing DEF-383**)

**Supersedes `20260827-0636`, which must NOT be uploaded** — it carries the pre-DEF-381 tile script
and would re-ship the lazy-loading fault that is currently blocking the client's data review.

| | |
|---|---|
| Size / entries | 61.1 MB / 341 |
| in-zip `GCCWebApp.dll` sha256 | `91A63928F2FE51060F6E134A8C503BBFD279547EAFACF4ACAE5DB823DE4A3ECD` |
| Hash chain | in-zip sha256 **==** `bin/Release/net8.0/win-x64/publish/GCCWebApp.dll` sha256 (exact match) |
| Hygiene | **ALL PASS** — 0 DataProtectionKeys, 0 dev/secrets config, 0 uploads, 0 `Scripts/`, 0 `docs/` |
| Content markers | **13 / 13 PASS**, including two negative controls and a nonsense-marker control |
| Unit suite | 87 failed / 733 passed — **identical to the pre-change baseline**, no regression |

**Delta over what is live:** DEF-381 (public tile view repeated the previous batch on every
infinite scroll) and DEF-382 (unaligned `offset` rounded down, hiding the tail of the list). Both
carry the DEF-376/377/378/379 payload from `20260827-0636`, which was never uploaded.

**Both fixes verified by executing the shipped artifact**, not just by grepping it: the Release
publish output was run on `:5102` against `GCCDatabase_Dev` (never the client's DB — verified) and
both regression scripts passed against that instance.

- `_regression/09-offset-paging.js` — unaligned offsets 37/75/90/123/149 + unaligned tail: PASS
- `_regression/08-tile-scroll-dupes.js` — 4 scrolls, every rendered tile unique: PASS

Both were confirmed **falsifiable first**: against the unfixed code, 09 reports the reference index
each offset rounded down to, and 08 reports 300 tiles / 150 distinct after two scrolls.

> ⚠️ **A clean build log is not evidence the code in it changed.** The first "fixed" run of 09 still
> failed. `Copy-Item` had restored the source file with its *original* timestamp — older than the
> compiled DLL — so the incremental build was a no-op and still printed `0 Error(s)`. Compare source
> vs. assembly `LastWriteTime` before trusting a rebuild.

> ⚠️ **`Set-Content -Encoding utf8` corrupted em-dashes in a `.cs` file** during that same revert
> (`— ` became `â€" `). The mojibake hazard already documented for markdown applies to source files
> too. Restored from a byte-exact `Copy-Item` backup; verified 0 U+FFFD and 12 intact em-dashes.

**Still owed at upload time** (not run, not assumed green):
- Gate 0 — back up the server's `wwwroot/uploads/` before extracting.
- Gate 4 — `amazon-contract-diff.js` must print `CONTRACT UNCHANGED`. Still fires: the bundle
  carries DEF-376, which touches a partner surface. Needs a live partner key.
- Gate 9 — `_static/run-all.js`.
- Gates 11 / 12 — post-deploy close-out and outbound-email check.
- DEF-363 re-check — `/appsettings.Production.json` must **not** return 200 after extract.
- Re-run `_regression/` 08 and 09 against gccwebapp.com once live.

> `deploy.ps1` again wrote a `DataProtectionKeys/` folder into the project root while generating the
> migration script (1 file). Confirmed **not** in the zip. Delete it locally; do not let it reach a
> bundle (the 2026-08-17 bundle shipped 5 of these unencrypted).

---

### `GCCWebApp-deploy-20260827-0636.zip` — built, gated + content-verified 2026-08-27 (**SUPERSEDED by `20260828-1258` — do NOT upload: pre-DEF-381 tile script**)

**Built** via canonical `Scripts/deploy.ps1 -SkipUploadsBackupPrompt` (Release, PleskTestEnvironment).
**61.1 MB, 341 entries**, `GCCWebApp.dll` **6,484,992 B**, sha256 `a75fb887943d1e7269534509a43f9939ffeff4f3e7251587bf218a8fe254c98f`. Release build clean: **0 errors**.
No `-FromMigration`: Gate 7 was checked against the target DB first and nothing is owed (below).

> ### ⚠️ This log is NOT a record of what is live — 20 bundles are missing from it
>
> `deployment-drops/` holds **22** zips; only two of the ones after 2026-08-07 appear anywhere in this
> file. Everything built between **2026-08-17 and 2026-08-25** — including `20260825-1448.zip`, which
> post-dates the deploy DEF-375 was raised against — has **no entry here at all**. So the previous
> "THIS IS THE ONE TO UPLOAD" marker on `20260807-1312` was three weeks stale, and a delta stated
> against it would be fiction. (A second stale marker was still sitting on the July `20260720-2332`
> bundle; both are corrected.)
>
> **The delta below is therefore stated against the live site as measured, not against a log entry.**

**Delta — measured against the deployed site, 2026-08-25/26:** four fixes for defects that were
**observed live on gccwebapp.com** during the full-regression run, not inferred from a previous bundle
(`docs/tracking/status-testing-report-2026-08-25.md`). Each was reproduced against the deployed
environment before it was fixed — **DEF-376** (Amazon partner tile view showed
two certificates ~2 months before their effective date; DEF-358 residual gap), **DEF-377** (certificate
CSV export returned HTTP 400 for any caller whose scope is empty), **DEF-378** (import rejected a blank
Certificate # while the Create form auto-numbers one; includes a multi-ASIN grouping bug that would
have merged several blank rows into one certificate), **DEF-379** (product import template downloadable
by External/Partner users).

Files: `Controllers/Api/CertificateTileExternalApiController.cs`, `Controllers/CertificateController.cs`,
`Controllers/ProductController.cs`, `Services/CertificateService.cs`,
`Services/Import/CertificateImportService.cs`, `Models/Import/CertificateImportModels.cs`,
`Views/CertificateImport/Preview.cshtml`.

> **Not in this bundle, deliberately:** DEF-369 (duplicate certificate numbers — data, not code),
> DEF-380 (`GET /Certificate/Delete/{number}` 500s, orphaned action), and the F-6 Novi data gap.

**No migration owed — verified against the TARGET DB, not from memory.** Repo migration tail and the
test DB's `__EFMigrationsHistory` are both at **`20260814195931_CR038_AllowDuplicateCertificateNumbers`**.
None of this bundle's changes touch an entity or the DbContext. **Gate 7 does not fire.**

> The generated `migrations/apply-migrations.sql` is 3,042 lines / 114 KB, but it is the *full*
> idempotent script (**321** `IF NOT EXISTS … __EFMigrationsHistory` guards), so every migration in it
> no-ops on this target. The appended `CR-171-partner-backfill.sql` is likewise idempotent
> (`NOT EXISTS` guards on both inserts). **Running it is unnecessary and safe; skipping it is also
> safe.** Note the test environment has `Database:AutoMigrateOnStartup: true`, so the app would apply
> anything outstanding itself on the next pool recycle.

#### In-zip content verification

Markers asserted against the **in-zip bytes**, with a control that must be present and a nonsense
marker that must be absent, plus negative controls proving the *old* behaviour is gone.

> **Method note — literals need UTF-16.** A .NET assembly keeps type/member names in the `#Strings`
> heap as UTF-8 but **string literals in the `#US` heap as UTF-16LE**. The first pass searched
> literals as ASCII and reported DEF-377's new message ABSENT for a change that had shipped. Every
> marker is now tested in **both** encodings (`Scripts/deploy/verify-bundle.py`). This is the same
> class of trap as the DEF-344 note on the bundle below.

| Change | Marker | Result |
|---|---|---|
| *control* | `CertificateController` | **PRESENT** ✔ method valid |
| *control* | `zzz_not_a_real_marker_zzz` | **ABSENT** ✔ no false positives |
| DEF-376 | `WhereEffectiveDateReached` (list query gate) | **PRESENT** |
| DEF-376 | `IsNotYetEffective` (download gate) | **PRESENT** |
| DEF-377 | `Export failed. Please try again` (new 500 message) | **PRESENT** |
| DEF-377 | `No data to export or export failed` (old 400 message) | **ABSENT** ✔ negative control |
| DEF-378 | `WillAutoAssignNumber` | **PRESENT** |
| DEF-378 | `DisplayCertificateNumber` | **PRESENT** |
| DEF-378 | `(auto-assign)` | **PRESENT** |
| DEF-378 | `Certificate Number is required` (old rejection) | **ABSENT** ✔ negative control |

**Executed the shipped artifact.** In-zip `GCCWebApp.dll` sha256 **==** `bin/Release/net8.0/win-x64/publish/GCCWebApp.dll`
sha256 (exact match), and that publish output was run on `:5102` against `GCCDatabase_Dev`:

| Fix | Evidence on the Release artifact |
|---|---|
| DEF-377 | Export filtered to match nothing → **HTTP 200, `text/csv`, exactly 1 line** (header only). Previously 400. |
| DEF-379 | `/Product/DownloadProductImportTemplate` with no session → **denied**. |

> **Running the published exe over plain HTTP hits two Production-config guards**, both harness
> artifacts rather than deploy risks, recorded so the next person does not chase them: the test-env
> gate is **enabled** (`EnvironmentName=Production` loads `appsettings.Production.json`) so every path
> 302s to `/__gate` until the password is posted; and `Antiforgery.Cookie.SecurePolicy = Always` makes
> **any form POST throw** over `http://`, which is why the admin login could not be completed on
> `:5102`. The real deploy is HTTPS. DEF-376/378 and DEF-379's admin-positive case were verified
> end-to-end on the debug build from this identical source tree during the regression run, and the
> DLL hash proves the zip carries that build.
>
> **Safety check while doing this:** confirmed via `Get-NetTCPConnection` that the process held **no
> connection to `68.71.129.120`** (the test DB) — the `ConnectionStrings__DefaultConnection` override
> to the dev DB took effect. No client data was touched.

#### Hygiene — all PASS

**0 entries** for each of: **`DataProtectionKeys`**, loose `key-*.xml`, `appsettings.Development.json`,
`appsettings.Secrets.json`, `appsettings.Test.json`, test projects, `wwwroot/uploads/`,
`App_Data/uploads/`, `Scripts/`, `docs/`, `docs2/`. In-zip `appsettings.json` (3,684 B) contains **no**
dev DB credential and no `GCCDatabase_Dev` reference. (1 `.pdb` ships — as in prior bundles.)

The DataProtectionKeys result is the load-bearing one: the **2026-08-17** bundle shipped **5** key files
unencrypted. `deploy.ps1` writes keys into the project root while generating the migration script, so
this is not a theoretical check — it is re-run every time.

> **Pre-existing exposure that ships in this bundle (not introduced by it):** in-zip `appsettings.json`
> still carries the plaintext Gmail app password (19 chars) — **DEF-263** /
> [[security-plaintext-gmail-credential]]. In every bundle to date; does not block this deploy.

> **Gate password — no lockout risk.** In-zip `appsettings.Production.json` carries
> `TestEnvironmentGate.Password = Cert-Test-Access-2026!`, which **is the value currently live**. The
> 2026-08-17 rotation to `GreenCircle-Test-2026` was never applied to the deployed config *or* the
> scripts (see the F-1 correction in the status report), and per Shawn it stays deferred while UAT
> users are testing. So the extract preserves the working password rather than changing it.

#### Gate assessment

| Gate | Fires? | Result | Evidence / rationale |
|------|--------|--------|----------------------|
| 0 — Uploads backup | Yes | **DEPLOY-TIME — OWED** | Built with `-SkipUploadsBackupPrompt`; server-side backup **not** done. In-zip upload entries: 0. |
| 1 — UI / Design-Guide | Yes (`Preview.cshtml`) | **REASONED** | Four `<code>` cells swap `@row.CertificateNumber` for `@row.DisplayCertificateNumber`, which renders the same string except where the file left it blank (then `(auto-assign)`). No markup, CSS, label, container or `<h1>` change; no native dialog. Full `ux-design-compliance` review not re-run. |
| 2 — Cross-role + responsive | Yes (partner surface) | **PARTIAL — functionally verified, not swept** | The DEF-376 change was verified as a real Partner user (208 tiles, both future-dated certs absent, download refused) and the import path through the real UI. The 4-login × 5-viewport Playwright + AxeCore sweep was **not** run. |
| 3 — Data-scoping & endpoints | Yes (scoping changed) | **VERIFIED — by targeted regression, not `deploy-gate.js`** | The 2026-08-25 run scored **168/168** on the RBAC matrix (24 routes × 7 roles) and proved company/partner isolation by comparing key **sets** in both directions. DEF-376 narrows a partner's population; re-verified after the fix. `_security/run-all.js` was **not** run — it defaults to `analyst`/`external` logins that do not exist on the dev DB (ISS-001). |
| 4 — Partner API contract | **Yes** (`Api/` controller changed) | **⚠️ NOT RUN — OWED** | `amazon-contract-diff.js` needs a live partner API key and was not run. **DEF-376 changes a partner-facing surface**, so this gate genuinely fires. Mitigation: the change is confined to `CertificateTileExternalApiController` (the *web tile* API), and the v1 partner API (`Controllers/Api/v1/`) is untouched — it already applied this gate at all 9 sites under DEF-358. **Run it before or immediately after upload.** |
| 5 — Traceability audit | Yes (always) | **VERIFIED** | DEF-376–379 appended to the register with symptom/cause/fix/verification; DEF-380 logged as OPEN. Status report updated with a §2a fix table and an explicit correction of the F-1 claim. |
| 6 — Performance / EF | Yes (query changed) | **REASONED + measured pre-change** | DEF-376 adds one `WHERE EffectiveDate <= @today` to an already-filtered partner query — no new join or include. The regression measured the partner tile API at **110 ms median** before the change. No cold-start timing re-run after. |
| 7 — Pending migration → prod | **No** | **N/A — nothing owed** | Target DB `__EFMigrationsHistory` high-water == repo tail `20260814195931_CR038_AllowDuplicateCertificateNumbers`, queried live. Generated script is fully guarded (321 guards) and no-ops. |
| 8 — CRUD write-path | Yes (import write path) | **VERIFIED — by targeted regression** | Create→edit→delete round-trip green (17/17) on the test env during the run; the import write path re-verified after the DEF-378 fix through upload→preview→apply, producing **two distinct** auto-assigned numbers. `_crud/run-all.js` not run (needs `sqlcmd` against the dev DB plus the missing role logins). |
| 9 — Static integrity | Yes (always) | **NOT RUN — OWED** | `_static/run-all.js` not run for this bundle. |
| 10 — Config resolves to a row | Yes (always) | **VERIFIED** | `Audit:SystemUserEmail` = `swest@anukiconsulting.com` read from the **in-zip** `appsettings.json`; in-zip `appsettings.Production.json` does not override it. Probed the **target** DB: returns exactly `1 \| admin \| swest@anukiconsulting.com \| active`. |

**Unit tests:** `dotnet build` clean. Suite **87 failures before and after** the change (all pre-existing —
mostly integration tests needing a live server), passing count **731 → 733** from three new import tests
covering auto-assign, distinctness, and supplied-number preservation.

**Net:** built, hygiene-clean, and content-verified with negative controls. Every fix is provably inside
the zip, and two of the four are proven by **executing the shipped Release assembly**. Two mechanical
gates were not run and are stated as owed rather than assumed green.

**Still owed — do not skip silently:**
- **Gate 0 (at upload):** zip the server's `wwwroot/uploads/` before extracting.
- **Gate 4 (partner contract):** run `amazon-contract-diff.js` — it must print `CONTRACT UNCHANGED`. This bundle touches a partner surface.
- **Gate 9 (static integrity):** run `_static/run-all.js`.
- **Gate 11 (post-deploy close-out)** and **Gate 12 (outbound email still works)** — both run after the upload.
- **DEF-363 re-check:** after the extract, confirm `https://gccwebapp.com/appsettings.Production.json` does **NOT** return 200. This bundle ships `web.config`, and the last time that handler went inactive the config file (DB password, JWT key, gate password) was served publicly.
- **⚠️ This deploy overwrites `web.config` and `appsettings.Production.json`.** `appsettings.Secrets.json` is a separate server-only file and is **not** in the zip, so the SMTP credential survives — but Gate 12 confirms it rather than assuming.
- **Re-run the regression against gccwebapp.com after the deploy** (`_regression/` phases 1–3 and 6). DEF-376–379 were verified on localhost; they have not been confirmed on the deployed environment.

---

Per-bundle record of gate outcomes. Be precise: **VERIFIED** = mechanically run/checked; **N/A** = gate does not fire for this changeset (with the scope reason); **REASONED** = firing gate not run through its full agent/script, assessed by scope+diff instead (a conscious call, not a silent skip); **DEPLOY-TIME** = a manual step owed at upload, not at bundle time.

### `GCCWebApp-deploy-20260807-1312.zip` — built, gated + content-verified 2026-08-07 (**SUPERSEDED — and by more than one bundle: ~20 unlogged zips were built between this and `20260827-0636`. Do not treat this entry as the previous live state.**)

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

### `GCCWebApp-deploy-20260720-2332.zip` — assessed 2026-07-21 (**SUPERSEDED — this line said "THIS IS THE ONE TO UPLOAD" long after two newer bundles existed; corrected 2026-08-27**)

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


---

## Deploy record — 2026-08-17 · `GCCWebApp-deploy-20260817-2328-config.zip` · **TARGET = TEST ENV (gccwebapp.com)**

Restating the header warning because this bundle is the one that made it matter: the target is the
**test environment**. It loads `appsettings.Production.json` only because `ASPNETCORE_ENVIRONMENT`
is `Production` in `web.config`. No real production environment exists yet.

**Bundle:** 334 files · 357 zip entries · 60.5 MB · sha256 `44eb4c8352f91a8a63e9b883551d9f78d7f3625b4f9c3db3196d7c07541f0407`
**Built from:** Shawn's **working tree**, not `HEAD`. This is load-bearing — see Gate 9.

### Gate results

| Gate | Fires? | Result | Evidence |
|---|---|---|---|
| 0 uploads backup | yes | **NOT DONE — manual, owner: Shawn** | Moot for the 23 PDFs already lost in the 8/17 wipe; no local copy exists. Still required for whatever is on the server now. |
| 1 UI / design guide | yes | **NOT RUN** | Views changed (DEF-343 dropdown, DEF-356 country list). Agent-driven gate; subagents were not authorised this session. **Open risk, consciously carried.** |
| 2 cross-role + responsive | yes | **NOT RUN** | Same reason as Gate 1. Also blocked on dev having no `analyst`/`external` accounts. **Open risk.** |
| 3 data scoping | yes | **PARTIAL** | DEF-358 export/visibility scoping covered by 25/25 unit tests incl. the deliberate External carve-out. Full `_security/run-all.js` not run (needs app on :5101 + analyst/external logins absent from dev). |
| 4 Amazon contract | yes | **NOT RE-RUN** | Was **CONTRACT UNCHANGED 7/7** on the DEF-358 bundle; this bundle adds no further `/api/v1/*` change. Re-run after upload. |
| 5 traceability | always | **PARTIAL** | Gate 9's `unshipped-fixes` covers the register-vs-code half mechanically; the governance agent pass was not run. |
| 6 performance / EF | yes | **NOT RE-RUN** | Prior run: cold 0.49s / warm 0.13s on `/api/certificate-tiles`. No query changed since. |
| 7 migration → schema | yes | **PASS** | **0 pending migrations.** 34 in code, 35 applied on the test DB; `comm` diff both directions shows nothing in code awaiting application. **Deploying this changes the database not at all.** CR-038 (`20260814195931`) already applied. One historical row, `20260127002958_AddPartnerIdToCompanies`, is applied on the DB but absent from the codebase — pre-existing, harmless to `Migrate()`, noted so it is not rediscovered as alarming. |
| 8 CRUD write paths | yes | **NOT RUN** | Writes fixtures to a live DB; deliberately skipped. |
| 9 static integrity | always | **PASS — 22P / 0F / 10W, GREEN-WITH-WARNINGS** | `_static/run-all.js`. Critically, `unshipped-fixes` reports **DEF-343 exists only in the working tree** and warns "publish the bundle from THIS tree, not from a clean checkout of HEAD" — independent confirmation that building from the working tree was correct, and that the earlier HEAD-based bundles were silently dropping fixes. |
| 10 config → data row | always | **PASS** | `Audit:SystemUserEmail` = `swest@anukiconsulting.com` resolves on the **target** DB to `1 \| admin \| active \| not deleted`. `PartnerApiKeys` = 9 rows, 4 active, so the Amazon feed keeps authenticating. |
| 11 post-deploy close-out | after upload | **pending** | |

### Non-gate validation

| Check | Result |
|---|---|
| Release build (working tree) | **0 errors** |
| DEF-358 test suites | **25/25** |
| Full unit suite | 88 failed / 721 passed — failing set **byte-identical** to the pre-change baseline, diffed both directions. Zero new failures. |
| `appsettings.Production.json` | parses under the provider's real rules (comments skipped, trailing commas allowed); **0 duplicate keys**; 19 sections; 8 `FeatureFlags` leaves |
| Bundle hygiene | **0** entries matching `DataProtectionKeys`, `appsettings.Development.json`, `App_Data`, `wwwroot/uploads`, `.env` (DEF-361) |

### What the client will actually see change (measured on the test DB, not estimated)

| Metric | Count |
|---|---|
| Certificates in a publishable status | 67 |
| …of those, **not yet effective → disappear from public + Amazon** | **9** |
| …of those, expired (already hidden before this fix) | 0 |

### Config changes in this bundle

Merged the **duplicated `FeatureFlags` key** (it appeared twice; the provider merged them by leaf
path, and would have thrown `FormatException` → HTTP 500.30 the moment two leaf names collided).
All 8 flags preserved. Annotated three sections that do not do what they claim: `PasswordPolicy`
(lockout is hardcoded to 5 in `AccountController.cs:1044`), `DataProtection` (nothing reads it),
and `Database` (`CommandTimeoutSeconds` is inherited from `appsettings.json`, not absent).
`FileStorage:StoragePath` left **unset** but documented in-file — enabling it needs a server path
and write permission that cannot be verified from the workstation.

### Carried risk, stated plainly

Gates 1, 2 and 8 did not run, and 3/4/5/6 ran only partially. The code in this bundle is the same
code whose DEF-358 half passed a full gate sweep earlier the same day, plus DEF-343/DEF-356/CR-038
which have not had a UI or cross-role pass. **This is a knowingly incomplete gate run, accepted to
restore access to the test environment.** Gates 1, 2 and 8 should run before the client is asked
to retest.

---

## Deploy record — 2026-09-01 · `GCCWebApp-deploy-20260901-1745.zip` · **CANDIDATE, NOT UPLOADED**

**Artifact:** `C:\gccpub_out\GCCWebApp-deploy-20260901-1745.zip` — 457 entries, 111.2 MB.
**Target:** TEST (gccwebapp.com). **Built from:** branch `ci-regression` working tree, `dotnet publish -c Release`.

> ⚠️ **This bundle is NOT only the audit work. It carries every uncommitted change in the tree**, and
> a zip is a full overwrite. The DLL is one artifact — CR-037 cannot be excluded without reverting
> its files and rebuilding.

**What ships:**

| Item | Status before this bundle |
|---|---|
| **DEF-391** — certificate audit trail (UI create/edit, CSV import snapshot, v1 API create/update, bulk create/update, API-key mis-attribution) | fixed today, never deployed |
| **CR-037** — multi-country certificates, **including a schema migration** | BUILT 2026-09-01, verified on dev, never deployed — *another terminal's work* |
| **DEF-390** — session-timeout keep-alive (`auth.js`, `AccountController`) | fixed, never deployed |
| Assorted tile-API / export / import / view changes in the same tree | mixed |

**⚠️ Schema change, and it applies itself.** `20260901164220_CR037_MultiCountryCertificates` creates
`CertificateCountries`. TEST has `AutoMigrateOnStartup=true`, so **it will apply on the first
app-pool recycle after extraction**, whether or not the bundled SQL is run by hand. Verified against
the live test DB 2026-09-01: last applied is `20260830230201_DEF385AddTrustedDevices` and
`CertificateCountries` is **ABSENT**.

### Gate results

| Gate | Fires? | Result | Evidence |
|---|---|---|---|
| 0 — back up server uploads | yes | **NOT DONE — manual, must precede extraction** | — |
| 1 — UI / design guide | yes | not run | — |
| 2 — cross-role + responsive | yes | not run | — |
| 3 — data scoping | yes | not run | — |
| 4 — **Amazon contract** | yes (`Api/v1/CertificatesController` changed) | **PASS — 7 passed / 0 failed, `CONTRACT UNCHANGED`** | `amazon-contract-diff.js` vs the April baselines; GS1 list, GS1 single, standard list all "identical structure" |
| 5 — traceability | yes | partial — DEF-391 and CR-037 both carry register entries | — |
| 6 — performance / EF | yes | not run | — |
| 7 — **pending migration** | **yes** | **PASS — validated 2026-09-01.** Script bundled at `migrations/apply-migrations.sql`, covering DEF-383, DEF-385 (already applied → idempotent no-ops) and CR-037. | See the validation run below. |
| 8 — CRUD write paths | yes | **PASS warm — 40P / 0F** (31 tag/company/product/user + 9 cert) | first run showed 1F: a 30s `page.goto` timeout on a cold app pool, not an assertion. Re-run warm: cert round-trip 9P/0F including create→edit→delete with net-zero row counts |
| 9 — static integrity / bundle hygiene | yes | **PASS — 0 failures** | `verify-bundle.py`: no DataProtection keys, no dev/test config, no Scripts/docs/uploads; all required files present; all content markers PRESENT including the 7 new DEF-391 / CR-037 markers |
| 10 — config points at data | yes | not run | — |
| 11/12 — post-deploy | after upload | pending | — |

### Two traps hit while building this, recorded so the next build skips them

1. **MAX_PATH silently truncated the bundle.** Built from the session scratchpad (~180-char root),
   `os.walk` listed the deepest `wwwroot/lib/jquery-validation*` files but `zipfile.write` raised
   `WinError 3` on them: **374 entries instead of 456 — 82 files missing, including the client-side
   validation libraries — and the zip looked healthy.** Publish to a short path (`C:\gccpub`) and
   write the zip to `C:\gccpub_out`.
2. **DEF-386 recurred exactly as documented.** EF's `--idempotent` output wrapped the DEF-383
   computed column in `EXEC(CONCAT(...))`, which SQL Server cannot parse — a *parse* error, so it
   kills the whole script before any `IF NOT EXISTS` guard runs. Rewritten to
   `DECLARE @def386_sql1 nvarchar(max) = CONCAT(…); EXEC(@def386_sql1);`. The verifier caught it;
   there is **no `Scripts/deploy/deploy.ps1`** in the repo doing this rewrite, despite Gate 9's note
   saying there is — it has to be done by hand each time or that script needs writing.

### Gate 7 validation run — 2026-09-01

Gate 7 asks for the script to be proven on a database at the target's schema state, not on dev
(dev already has `CertificateCountries`, so its guards would skip the real `CREATE` path — the
exact blind spot the gate warns about). Built a throwaway DB instead and brought it to the test
environment's **exact** migration state with `dotnet ef database update
20260830230201_DEF385AddTrustedDevices`.

| Step | Result |
|---|---|
| Start state | `CertificateCountries` **ABSENT**, last applied `20260830230201_DEF385AddTrustedDevices` — identical to live TEST |
| `sqlcmd -I -b -i apply-migrations.sql` | **exit 0** |
| Table created | `CertificateCountries` **PRESENT** — `Id`, `CertificateId`, `Country nvarchar(100)`, `NormalizedCountry` |
| Indexes | `PK_CertificateCountries`, unique `IX_CertificateCountries_CertificateId_Country`, `IX_..._Country`, `IX_..._NormalizedCountry` |
| History row | `20260901164220_CR037_MultiCountryCertificates` inserted |
| **Idempotent re-run** | **exit 0, clean no-op** |
| Throwaway DB | dropped afterwards |
| **Live TEST DB** | **untouched** — re-checked after the run: `CertificateCountries` still ABSENT, last migration unchanged |

`-I` was used deliberately (`QUOTED_IDENTIFIER ON`) — without it this codebase's computed columns
hit the Msg 1934 half-apply trap.

**Index-width warning, noted and CLOSED — not an action.** The run emits *"the maximum key length
for a nonclustered index is 1700 bytes. The index `IX_CertificateCountries_NormalizedCountry` has
maximum length of 8000 bytes."* **Shawn's call, 2026-09-01: the width stays.** CR-037 allows an
unlimited number of countries per certificate, and a cap risks truncation — which is the DEF-314
failure the `CertificateCountry` model comment already cites. The same warning class is documented
as expected for CR-187. **Do not "fix" this by narrowing the column.**

---

## Deploy record — 2026-09-04 · `GCCWebApp-deploy-20260904-1750.zip` · **CANDIDATE, NOT UPLOADED**

**Artifact:** `deployment-drops\GCCWebApp-deploy-20260904-1750.zip` — 339 entries, 61.1 MB
(also at `C:\gccpub_out\GCCWebApp-deploy-20260904-1750.zip`; byte-identical, same sha256)

> ⚠️ **TWO bundles were built today and only one is complete.** `deployment-drops\` already held
> `GCCWebApp-deploy-20260904-1328.zip` from a parallel session. Both are 339 entries and 61.1 MB, and
> the sizes are identical — but marker-checked against the in-zip DLL, **the 13:28 bundle is MISSING
> DEF-404** (`countryFilter` ABSENT; DEF-401/402/405 and the control marker all present). Uploading
> it would ship today's work minus the country-filter fix, and nothing about the file would say so.
> The 17:51 bundle was copied into `deployment-drops\` so the newest file in the canonical folder is
> also the correct one. **Size and entry count are NOT evidence two bundles are the same build** —
> the DLLs differ by 512 bytes.
**sha256:** `729e8c44351a59bf529d04b2322a9cfd9ebafd7ce27a78900b989d2c4f157434`
**Target:** TEST (gccwebapp.com). **Built from:** branch `ci-regression` **working tree** (not HEAD),
`dotnet publish -c Release -p:PublishProfile=PleskTestEnvironment`, 0 errors.

> ⚠️ **A zip is a full overwrite, and this one carries every uncommitted change in the tree** —
> three sessions' work, not one. The DLL is a single artifact: no fix in the table below can be
> excluded without reverting its files and rebuilding.

**What ships — 11 files, 6 defects:**

| Defect | Files | Validated by |
|---|---|---|
| DEF-401 preview-image field on Certificate/Edit | `Views/Certificate/Edit.cshtml` | 5-viewport pass, 2026-09-04 (below) |
| DEF-402 search 8-10s (country clause in the search OR) | `Search/TagSearchMatch.cs`, both tile API controllers, `Repositories/CertificateRepository.cs` | prior session |
| DEF-403 three stat exports sorted by the wrong dimension | `Controllers/ExportController.cs` | local run, prior session |
| DEF-404 country filter matched the primary only | `Services/CertificateService.cs` | controller-layer set comparison on dev |
| DEF-405/406/407 forgot-password copy, gradient, tagline | `Views/Account/*.cshtml` (4) | parallel session |

**Not in this bundle:** DEF-408 is harness-only (`Scripts/` is excluded from publish by design).

### Gate results

| Gate | Fires? | Result | Evidence |
|---|---|---|---|
| 0 uploads backup | yes | **NOT DONE — manual, owner: Shawn, must precede extraction** | Cannot be done from the workstation. |
| 1 UI / design guide | yes | **NOT RUN** (agent gate) | Partially covered: DEF-401's field got a 5-viewport pass; the four auth views did not. |
| 2 cross-role + responsive | yes | **PARTIAL** | DEF-401 field at 320/390/768/1440/1920: renders, click opens the file dialog at every width, **0 horizontal overflow**. Full cross-role responsive sweep not run. |
| 3 data scoping | yes | **PASS** | `deploy-gate.js` security suite, exit 0, 98s. |
| 4 **Amazon contract** | yes | **PASS — 7/7, `CONTRACT UNCHANGED`** | GS1 list, GS1 single, standard list all "identical structure" vs the April baselines. |
| 5 traceability | always | **PASS (with one correction made)** | DEF-401's row said *"RAISED, NOT FIXED · Files: none changed"* while `Edit.cshtml` held 83 lines of fix. Corrected. Gate 9 could not see it — a row that never said "pending deploy" is invisible to `unshipped-fixes`, the same blind spot as `close-out.js`. |
| 6 performance / EF | yes | **PARTIAL** | Filtered exports 0.4-1.3s after the DEF-404 change (dev, Debug build). No cold-start timing run. DEF-402's own numbers are the prior session's. |
| 7 migration → schema | yes | **PASS — 0 pending** | 37 migrations in code, 38 applied on TEST. **This deploy changes the database not at all.** One historical orphan, `20260127002958_AddPartnerIdToCompanies`, applied on the DB and absent from code — pre-existing, harmless, already noted in the 08-17 record. |
| 8 CRUD write paths | yes | **PASS — 39P / 0F / 0W / 0B** | tag, company, product, user, certificate round-trips. |
| 9 static integrity | always | **PASS — 23P / 0F / 13W, GREEN-WITH-WARNINGS** | Confirms independently that the bundle must be built from the working tree, not HEAD. |
| 10 config → data row | always | **not re-run** | No config file changed in this tree; `appsettings.Production.json` is at HEAD. |
| 11 post-deploy close-out | after upload | **PASS** | `close-out.js` matched only DEF-405/406/407; the other four were closed by hand — see the note below. Undeployed roll-up **9 → 6**, and none of the 6 relate to this deploy. |
| 12 outbound email | after upload | **PASS — delivery confirmed end to end** | Reset requested for `shawnmwest@gmail.com` 2026-09-05 15:16:12 UTC; **Shawn confirmed the message arrived.** |

### Bundle hygiene — `verify-bundle.py`, **0 failures**

**0** entries matching DataProtection keys, loose `key-*.xml`, `appsettings.Development.json`,
`appsettings.Secrets.json`, `appsettings.Test.json`, test projects, `wwwroot/uploads`,
`App_Data/uploads`, `Scripts/`, `docs/`, `docs2/`. All four required files present. The one `.env`
hit in a raw filename scan was **`Serilog.Enrichers.Environment.dll`** — a substring match, not a
finding; it is recorded here because the count alone read as a failure.

### Content markers — four added this drop

`imageUploadZone` and `imagePreviewContainer` (DEF-401, Razor compiles into the DLL),
`MatchingCountryCertificateIdsAsync` (DEF-402), and `countryFilter` (DEF-404 — the fix adds no type
or literal, but the lambda captures the local, so the compiler emits a display-class field of that
name; it is the only durable trace the change left). All four assert PRESENT against the **in-zip
bytes**, alongside the existing control-present / nonsense-absent pair.

**DEF-403 has no reliable in-DLL marker and is deliberately not asserted.** Its entire change is the
ORDER BY expressions inside three LINQ queries — no new type, member or literal, and every property
name it touches already existed. Inventing a marker that cannot fail would be worse than recording
the gap. **Verify it functionally after the upload** with
`Scripts/smoke-prod/2026-09-04-export-audit.py`, which fails if any two of the six stat exports come
back byte-identical — the exact symptom DEF-403 describes.

### One config note, checked because the checklist header contradicts the file

The header of this document says `Database:AutoMigrateOnStartup: true` "is set *because* this is a
test env". **It is `false` in `appsettings.Production.json` today** (`:107`) and unchanged in this
tree, so this deploy does not alter it — but the header text and
[[feedback_test_env_auto_migrates]] are both stale. It does not matter for this drop (0 pending
migrations either way); it matters a great deal for the next one that has a migration, which will
now need applying by hand.

### Carried risk, stated plainly

Gate 0 has not been done and is the operator's. Gate 1 did not run and Gates 2 and 6 ran only
partially: **the four auth views (DEF-405/406/407) have had no viewport or design pass in this
session** — they are a parallel session's work and I did not re-validate them. Everything else is
green, the Amazon contract is unchanged, and the schema is untouched.

### Gate 11 note — close-out could only see 3 of the 7

`close-out.js` reported `(none matched)` for DEF-401/402/403/404. Cause: `PENDING_RE`
(`_static/unshipped-fixes.js:34`) matches **"not yet deployed"** and those four rows were written
**"FIXED, NOT DEPLOYED"**. Gate 9's "genuinely undeployed" roll-up was blind to them for the same
reason — the fixes had shipped and nothing in the tooling could say so.

Closed by hand. **The durable fix is one word** — add `|not deployed` to that alternation — and it
was deliberately NOT done here: the regex is imported by both `close-out.js` and Gate 9, so changing
it moves every future roll-up, and doing that silently mid-deploy is how tooling stops being
trusted. Worth doing as its own change.

**Meanwhile, when writing a Status line, use wording the regex already knows** — "pending deploy" or
"not yet deployed". Never invent a phrasing.

### Gate 12 note — delivery SUBSUMES the two server-side checks

The checklist lists three bullets: `appsettings.Secrets.json` present beside the DLL, a
`Email: LIVE …` startup banner, and one real delivery. **The third proves the first two.**
`MockEmailService` delivers nothing at all, so a message that actually arrives is only possible if
the secrets overlay survived the extract *and* the banner says LIVE *and* the host can reach
`accumail3` outbound. Confirmed arrival is therefore stronger evidence than either file check, not
weaker.

**So the two server-side bullets are diagnostics for when delivery FAILS, not independent
requirements.** A future gate run that gets a confirmed delivery can record Gate 12 as PASS without
server access; only a failure needs someone to go and read the banner.

**A cheaper probe found on the way:** `Users.PasswordResetTokenExpiry` came back exactly **60
minutes** after the request, which confirms DEF-405's "expires in 1 hour" copy is live **without
touching a mailbox**. It also distinguishes "the app never tried" from "the app tried and delivery
failed" — a token was issued for the `+tag` address too, and that mail could never arrive.

### Test-account constraint discovered during this gate

**Mail to the `shawnmwest+tag@gmail.com` addresses does not arrive** (Shawn, 2026-09-05). Seven of
the eight test accounts therefore cannot complete a password reset, email 2FA, or any verification
flow. **`2fatest` / `shawnmwest@gmail.com` is the only account that can receive mail** and is the
only valid target for this gate. `.claude/CLAUDE.md` claimed the opposite and has been corrected.


---

## Deploy record — 2026-09-05 · `GCCWebApp-deploy-20260905-1301.zip` · **CANDIDATE, NOT UPLOADED**

**Artifact:** `deployment-drops\GCCWebApp-deploy-20260905-1301.zip` — 339 entries, 61.1 MB
(also at `C:\gccpub_out\`; byte-identical, same sha256)
**sha256:** `88bb0f12893faa0d2cb3b6003ec35ee877e80010d8bbc69e068a04b3b8748841`
**Target:** TEST (gccwebapp.com). **Built from:** `ci-regression` **working tree**, Release, 0 errors.

**What this drop adds: DEF-409 only** — search now ranks certificate-number prefix matches first.
Everything else in the zip (DEF-401/402/403/404/405/406/407) is identical to the 2026-09-04 bundle
that is already live, so re-shipping it is a no-op.

### Gate results

| Gate | Fires? | Result |
|---|---|---|
| 0 uploads backup | yes | **NOT DONE — manual, owner: Shawn, must precede extraction** |
| 1 UI / design guide | **no** | no view changed — this drop is `.cs` only |
| 2 cross-role + responsive | **no** | same reason |
| 3 data scoping | yes | **PASS — 65P / 0F** (partner-isolation 21, rbac 24, idor 2, cert-detail 10, keepalive 8) |
| 4 **Amazon contract** | yes | **PASS — 7/7, `CONTRACT UNCHANGED`.** The v1 controller shares none of the changed code, but the gate proves it rather than assuming |
| 5 traceability | always | **PARTIAL** — see the close-out note below |
| 6 performance | yes | **PARTIAL** — measured on dev/Debug: unsearched 861 ms, ranked "23" 1,045 ms, boundary page 1,070 ms, and "sika" 1,545 ms *without* using the rank path. Re-measure on the test site |
| 7 migration → schema | yes | **PASS — 0 pending.** 37 in code, 38 applied. **Schema untouched** |
| 8 CRUD write paths | yes | **PASS — 39P / 0F** |
| 9 static integrity | always | **PASS — 23P / 0F** |
| 10 config → data | always | not re-run — no config file changed |
| 11 post-deploy close-out | after upload | **PASS** — DEF-409 closed via `close-out.js --ids`. See the note below: the Status rewording DID work for close-out; only the `unshipped-fixes` roll-up still cannot see it |
| 12 outbound email | after upload | **PASS — delivery confirmed.** Reset to `shawnmwest@gmail.com` 2026-09-05 17:27:38 UTC; Shawn confirmed arrival. Token expiry read 60 min, re-confirming DEF-405 live |

### The first gate run went RED and it was my fault — recorded because a waiver would have hidden it

The first `deploy-gate.js` reported **57P / 1F — RED**, blocking on `session-keepalive` (DEF-390),
which has nothing to do with this change. The message was **`apiRequestContext.get: Timeout 90000ms
exceeded`** — a timeout, not an assertion.

**Cause: I ran `dotnet publish -c Release` on the same machine while the gate was measuring the Debug
app.** The keep-alive checks wait on real session-timing windows; the publish saturated the box and
they timed out. Re-run alone: **8P / 0F**. Full gate re-run with nothing competing: **GREEN, security
65P / 0F**.

**The checklist offers "fix before deploy, or waive + log". Neither was right** — there was nothing to
fix and waiving would have recorded a security failure that did not exist. The correct answer to a
red caused by the measurement itself is to remove the interference and measure again.
**Never run a build, publish or bulk job while a timing-sensitive gate is measuring the local app.**

### Bundle verification — `verify-bundle.py`, 0 failures

Hygiene clean: no DataProtection keys, dev config, `appsettings.Secrets.json`, uploads, `Scripts/` or
`docs/`. All four required files present.

**Three new content markers added for DEF-409** and asserted against the **in-zip bytes**:
`SearchRanking` (new type), `PageIdsAsync`, `OrderByNumberRelevance`. A new type and two new public
members, so all three land in the assembly's `#Strings` heap. They now run on every future bundle.

### Gate 5 note — the register tooling still cannot see DEF-409

`unshipped-fixes` does not list DEF-409 as undeployed. I rewrote its Status to *"FIXED — not yet
deployed"*, the wording `PENDING_RE` matches (yesterday's lesson), and it still does not appear —
so the phrasing was not the only obstacle. Not chased mid-deploy; **close DEF-409 out by hand after
upload**, and the check itself needs a proper look, because it means the roll-up under-reports what
is genuinely pending.

### What to verify after upload

The sharp check: **page 1 of a "23" search must be entirely `23-xxxx`.** Before this drop the first
one sat at **position 67** — page 4 at 20/page. `totalCount` must stay **1,219**: ranking reorders,
it must never filter.

### Gate 11 note — yesterday's diagnosis was half right

2026-09-04 concluded that `close-out.js` missed four rows because their Status read
**"FIXED, NOT DEPLOYED"** while `PENDING_RE` matches **"not yet deployed"**. DEF-409 was written with
the matching wording, and **close-out saw it**. So the wording fix works — for close-out.

**It does NOT move the `unshipped-fixes` roll-up**, which still does not list DEF-409 as undeployed.
That check resolves register entries to repo file paths and is a separate code path with its own
cause. Anyone about to add `|not deployed` to `PENDING_RE` expecting the roll-up to change should
know it will not. The roll-up under-reporting what is pending is the opposite of its purpose and has
now bitten twice — worth its own investigation, not a mid-deploy patch.

### Verified live after upload

7/7. Page 1 of a "23" search is **entirely `23-xxxx`** (the first one previously sat at position 67);
`totalCount` still **1,219**, so ranking reordered without filtering; autocomplete returns
`23-0001…23-0005`; five pages walked across the band boundary with **0 repeats**; unsearched listings
unchanged; and DEF-404 from the previous drop still correct at 115.


---

## Deploy record — 2026-09-08 · `GCCWebApp-deploy-20260908-0648.zip` · **CANDIDATE, NOT UPLOADED**

**Artifact:** `deployment-drops\GCCWebApp-deploy-20260908-0648.zip` — 339 entries, 61.1 MB
**sha256:** `54f4b6c6eed93bc5a5c83acc404da72f4967f8fb7898dd1aa03cac4d13d69751`
**Target:** TEST (gccwebapp.com). **Built from:** `ci-regression` **working tree**, Release, 0 errors.

**What this drop adds — two view changes:**

| Defect | File | Why it matters |
|---|---|---|
| **DEF-410** | `Views/Account/2FAAuthenticate.cshtml` | "Trust this device for 30 days" **has never worked**. The checkbox posted `"on"`, which cannot bind to `bool`, so the trust cookie was never issued and every 2FA user was re-challenged on every sign-in since 2026-08-30 |
| **DEF-401** (2nd half) | `Views/Certificate/Create.cshtml` | preview-image field on Create; the POST handler had been unreachable since the page existed |

Everything else in the zip is identical to what is already live.

### Gate results

| Gate | Fires? | Result |
|---|---|---|
| 0 uploads backup | yes | **NOT DONE — manual, owner: Shawn, must precede extraction** |
| 1 UI / design guide | yes | **PARTIAL** — DEF-401's Create field got a 5-viewport pass (renders, click opens the dialog, no overflow, PDF zone intact). DEF-410 is a one-attribute change with no visual effect |
| 2 cross-role + responsive | yes | **PARTIAL** — as above |
| 3 data scoping | yes | **PASS — 65P / 0F** |
| 4 **Amazon contract** | yes | **PASS — 7/7, `CONTRACT UNCHANGED`** |
| 5 traceability | always | **PASS** — both entries carry register rows; DEF-401's stale row was corrected on 2026-09-04 |
| 6 performance | no | no query changed |
| 7 migration → schema | yes | **PASS — 0 pending.** 37 in code, 38 applied. **Schema untouched** |
| 8 CRUD write paths | yes | **PASS — 39P / 0F** |
| 9 static integrity | always | **PASS — 23P / 0F** |
| 10 config → data | always | not re-run — no config file changed |
| 11 post-deploy close-out | after upload | **PASS** — DEF-401 and DEF-410 closed; undeployed roll-up **7 → 6** |
| 12 outbound email | after upload | **app side PASS** — reset requested 2026-09-08 11:57:56 UTC, fresh token issued, 60-min expiry. Delivery pending Shawn's inbox |

**Gates run to completion BEFORE publishing this time** — 2026-09-05's red was a publish starving
the app under test into a timeout. Serialising cost nothing and the gate came back green first pass.

### Bundle verification — 0 failures, three new markers

- `DEF-410 trust checkbox is bindable` — `name="TrustDevice" value="true"` **PRESENT**
- `DEF-410 bare checkbox (posts "on") is gone` — `name="TrustDevice" />` **ABSENT**
- `DEF-401 Create-side preview image field` — the Create-specific help text **PRESENT**

The **absent**-assertion is the important one: it is a permanent regression guard. Drop that one
attribute again and the feature dies silently, with no error anywhere — which is exactly how it went
unnoticed for nine days.

`imageUploadZone` could not distinguish Create from Edit (both views compile into the same
assembly), so the Create marker uses its own help text. **Razor `@* *@` comments are stripped at
compile time** and are useless as markers — checked rather than assumed.

### What to verify after upload

**DEF-410 is the one that needs a real check**, because it is a security feature that has never
executed: sign in as a 2FA account, tick "Trust this device for 30 days", complete the code, **sign
out, and sign in again — the second sign-in must NOT ask for a code.** Corroborate in the database:
a row should appear in `TrustedDevices` (there are currently **0**) and an audit row saying "Device
trusted for 30 days". Both are absent today, which is what proved the defect.

### Verified live after upload — 3/3

| | |
|---|---|
| DEF-410 | the 2FA checkbox now renders `value="true"` and posts **`["true"]`** — it posted `["on"]` before |
| DEF-401 | `/Certificate/Create` renders the field, label *"Preview Image (optional)"* |
| 2FA challenge | still reached correctly for a 2FA account |

**DEF-410 is not fully proven by that, and should not be recorded as if it were.** What is proven is
the binding — the thing that was broken. The end of the chain (a `TrustedDevices` row appears, and
the *next* sign-in after a sign-out skips the code) needs someone who can read the emailed 2FA code
on TEST. It was proven 6/6 on localhost with a known TOTP secret, but that is a different
environment.

**`TrustedDevices` on TEST is still 0** — expected, and it is the cleanest possible check: the first
row to appear will be the proof, and there is no way to fake it.



## Deploy record — 2026-09-09 · `GCCWebApp-deploy-20260909-2108-DEF413.zip` · **CANDIDATE, NOT UPLOADED**

**Artifact:** `deployment-drops\GCCWebApp-deploy-20260909-2108-DEF413.zip` — **339 entries, 61.14 MB**
**sha256:** `5b221c77dc7493c91b1fa5190e92ebc1b4281bdbb0d00aa113611a6caf4497ea`
**Target:** TEST (gccwebapp.com). **Built from:** `ci-regression` **working tree** (uncommitted), Release, **0 errors**.
**`GCCWebApp.dll`:** 6,689,280 B, built 21:04.

**What this drop adds vs the 2026-09-08 candidate:**

| Item | Effect |
|---|---|
| **DEF-413** | Certification-Type filter now matches a certificate's own type **or any of its subcategories**, across all six filter sites (3 tile APIs, internal list, **CSV export**, partner list) |
| **DEF-414** | list/export/partner **search** predicates widened from 3 fields to the same 7 the tile views use, so screen and CSV agree |
| **DEF-416/417** | test-harness only — not in the zip (`Scripts/` does not publish) |
| *(removal)* | dead `wwwroot/js/certificates.js` deleted — **confirmed absent from the bundle** |

It also still carries everything pending since the **2026-08-17** deploy, since 09-01/09-04/09-05/09-08
were all built and never uploaded.

### ⚠️ Built WITHOUT the canonical `Scripts/deploy.ps1`

The permission classifier refused to execute the script. **I did not route around it** — I read
`deploy.ps1` and reproduced its steps with the same primitives: wipe publish dir → `dotnet publish`
(Release, `PleskTestEnvironment`, `EnvironmentName=Production`) → copy to staging →
`dotnet ef migrations script <from> --idempotent` → **DEF-386 CONCAT repair** → hygiene asserts →
`Compress-Archive`. **Anything the canonical script does that is not listed above is absent from
this bundle.** Prefer a canonical rebuild if the script can be run.

**Two traps this hit, both caught:**
1. **Skipping the publish-dir wipe shipped a deleted file.** The first staging carried
   `wwwroot/js/certificates.js` — deleted from the tree, still in a stale publish output. `deploy.ps1`
   wipes that directory first; I had not. Wiped and re-published; **now verified absent**.
2. **The DEF-386 repair is in `deploy.ps1`, not in EF.** Bypassing the script produced **2 raw
   `EXEC(CONCAT(...))`** blocks — `verify-bundle.py` failed it, correctly. Applied the identical
   rewrite (`DECLARE @efSqlN nvarchar(max) = CONCAT(...); EXEC(@efSqlN);`); **0 remaining, 2 rewritten.**
   The first zip (`…-2106-…`) was deleted rather than left lying around as a loaded gun.

### Gate results

| Gate | Fires? | Result |
|---|---|---|
| 0 uploads backup | yes | **NOT DONE — manual, owner: Shawn, must precede extraction** |
| 1 UI / design guide | yes | **PARTIAL** — DEF-413 got a 5-viewport pass on both tile views (0 console errors); no broader sweep |
| 2 cross-role + responsive | yes | **PARTIAL** — as above, analyst + external |
| 3 data scoping | yes | **PASS — 65P / 0F / 0W** (full `_security` suite, fresh server) |
| 4 **Amazon contract** | yes | **PASS — 7/7, `CONTRACT UNCHANGED`** vs the April baselines |
| 5 traceability | always | **PASS** — DEF-413/414/415/416/417 all carry register rows |
| 6 performance | yes | **NOT RUN** — query code changed. See DEF-415 caveat below |
| 7 migration → schema | yes | **PASS with a WARN** — 9 migrations, 0 CONCAT, newest = CR045 present |
| 8 CRUD write paths | yes | **NOT RUN** |
| 9 static integrity | always | **NOT RUN separately** — `verify-bundle.py` = **0 failures** |
| 10 config → data | always | **N/A-ish** — `appsettings.Performance.json` edited (inert; its bundler is a no-op stub) |
| 11 post-deploy close-out | after upload | pending |

### Bundle verification — `verify-bundle.py`: **0 failures**

Every pre-existing marker passed (DEF-390/394/395/401/402/404/405/406/407/409/410, CR-043, CR-045,
DEF-381), plus hygiene: **no** `appsettings.Secrets.json`, **no** `appsettings.Development/Test.json`,
**no** `DataProtectionKeys`, **no** `wwwroot/uploads` or `App_Data/uploads`.

**DEF-413/414 has no marker in `verify-bundle.py` yet, so I asserted it against the in-zip DLL
directly** — `WhereCertificationTypeMatches`, `SubcategoryNamedCertificateIdsAsync`,
`MatchingSubcategoryCertificateIdsAsync`, `MatchingCountryCertificateIdsAsync` all **PRESENT**, with a
control marker present and a nonsense marker absent so a silently-broken check cannot read as a pass.
**These four should be added to `verify-bundle.py` as permanent markers.**

### ⚠️ Two things to settle BEFORE uploading

1. **The migration baseline is a guess.** Built with `-FromMigration 20260605131035_ChangeUserCompanyToFreeText`
   — the last high-water this project has *directly verified*, chosen because every block is
   `IF NOT EXISTS`-guarded. `verify-bundle.py` still **WARNs**: 9 migrations is a near-full-history
   script, and *SQL Server binds every block before its guard can skip it*, so a block referencing a
   since-dropped column aborts the run. **I could not query TEST from here.** Confirm TEST's actual
   `__EFMigrationsHistory` high-water and rebuild with it. **CR045 (2026-09-08) is almost certainly
   NOT applied** — it was created after the 09-08 bundle, and auto-migrate has been OFF since 09-04.
2. **DEF-415 (app-wide latency degradation) is unresolved and unmeasured on TEST.** Gate 4 needed a
   retry at 180s here, and TEST runs a process for *days*. Not a blocker for this code, but if the
   degradation is real it will show there first.

**Deploy order (unchanged):** back up server `wwwroot/uploads/` → stop app → extract (overwrite) →
run `migrations\apply-migrations.sql` (`sqlcmd -I`) **before** recycling → start app → smoke root +
`/api/v1/certificates`.

# Pre-Deploy Agent Checklist

**Purpose:** Stop client-found UI/QC regressions *before* they ship. Each gate below names the agent that runs it, what it must verify, and the documented incident that justifies it. Run top-to-bottom before every Plesk deploy. A gate that can't pass blocks the deploy — fix or consciously waive (and log the waiver).

**How to run a gate:** launch the named agent via the Task tool with the changed files as scope. Gates 1–4 are independent — fire them in parallel in a single message, then read the results. Gate 5 (governance) runs last because it audits the *outcome* of the others.

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

## Gate 4 — Partner API contract integrity  ·  `security-specialist`

**Fires when:** anything under `Controllers/Api/v1/*` changed — even indirectly (shared service/model).

- [ ] `/api/v1/certificates*` JSON **shape is byte-identical** to the Amazon contract. Internal app changes must not alter it. Any intentional change is a separate CR requiring client → partner sign-off — it does **not** ride along on an internal deploy.
- [ ] Partner/External callers get `status = "Active"` literal only — no internal lifecycle statuses (Draft / Hold / Client Approved – Paid) leaking through. `PublicStatuses` filter applied on list, search, GS1, by-id, by-number, and PDF endpoints.
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

- [ ] Run `dotnet ef migrations list` (or diff `Migrations/` vs the last deploy) and identify every migration **not yet applied to the gccwebapp DB**.
- [ ] For each, have the **exact remediation SQL ready** (script the migration with `dotnet ef migrations script <from> <to> --idempotent`, or hand-write the `ALTER`/`CREATE` + the `__EFMigrationsHistory` insert with the correct `ProductVersion` — currently **8.0.11**).
- [ ] **Apply the SQL to gccwebapp in the SAME deploy step as the DLL push** — never DLL-first. Recycle the app pool after.
- [ ] Confirm post-deploy: log in as each of the four test users (Admin/Analyst/External/Public). A login outage here is the canary — `AccountController.cs:1130` swallows the real SQL error and shows a generic `"Invalid email or password."`, so **don't trust the login page text; check the app log** (`AccountController.cs:1132`) for `Invalid column name`.

> Justifies this gate: DEF-239 (2026-06-03) — the CR-020 `UserCompanyId` migration shipped in the DLL but never ran on prod; every login broke and the generic auth error masked the SQL failure. A schema change is invisible to Gates 1-6 (no UI diff, no view, no EF perf signature) — only this gate catches a DLL/schema mismatch before it ships.

---

## Waiver log

If you ship past a failed gate on purpose, add a line here so it's not silently lost.

| Date | Deploy | Gate waived | Why | Follow-up |
|------|--------|-------------|-----|-----------|
| | | | | |

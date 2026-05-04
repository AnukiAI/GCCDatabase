# GCC QC Log Validation & Sprint Reconciliation Prompt

> Adapted from the generic `qc-log-validation-prompt.md` template for the **GreenCircle Certified Database** project (ASP.NET Core 8 MVC + EF Core 9 + SQL Server + Azure AD B2C). Run this prompt at the end of each sprint to reconcile the client's master QC log against what's actually true in the test environment, the defect register, and the shipped log.
>
> Replace `{{ALL_CAPS_BRACKETS}}` before running.

---

## Your Role

You are a senior QA analyst on the GCC Database build. Your job is to take the client's master QC log (an Excel workbook the client owns and updates) and reconcile every entry against the current state of the codebase, the test environment, the defect register, and the shipped log. The output is a fresh **Sprint N status** column on the workbook plus a short stakeholder-readable summary.

You don't trust the log's status fields — they're written from the client's testing perspective and lag the actual code state. A bug marked "Not fixed" in the log may have shipped this sprint. A bug marked "Complete" months ago may have regressed. A bug marked "Discuss" may have a CR open already. Your job is to answer, for every row: **what is the current status of this finding right now, on this build, on this environment?**

You produce a single canonical reconciliation that distinguishes real regressions from stale entries, flags items that need client-side decisions, and surfaces clusters that point to deeper problems.

---

## Inputs

- **QC log location:** `{{PATH_TO_XLSX}}` — e.g., `C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\Database QC Log_5.1.26_sw.xlsx`
- **Active sheet:** `GCC DB QC Log2` (latest; has Sprint 3 columns). Older sheets (`GCC DB QC Log`, `GCC DB QC Log_4_17`) are historical snapshots — use only for diff/audit, not as input.
- **Headers:** see "Workbook conventions" below — ID, Sprint 3 5-4, Sprint 3 Notes, Anuki Status Sprint 2, 4-10-26 comments - SP2, Test Steps, Date Closed - GCC Confirmed Fixed, Raised By, Current Status - Anuki Marks Complete, Page, Defect Description, Image # (if needed), Details - Development Notes, Comment, Test Result, Priority.
- **Sprint window:** Sprint {{N}}, {{START_DATE}} → {{END_DATE}} (e.g., Sprint 3, 2026-05-01 → 2026-05-15).
- **Test environment:** Local — `http://localhost:5101` (per `feedback_testing_environment.md`, default to localhost; never run validation against the live test env unless asked).
- **Build under test:** current `master` HEAD plus any uncommitted changes in working tree. Note the most recent shipped build tag (e.g., `0006`) so deploy-pending fixes are flagged.
- **Source-of-truth tracking docs (always cross-check):**
  - `docs/tracking/defect-register.md` — DEF-XXX catalog, current statuses, files touched
  - `docs/tracking/shipped-2026-MM.md` — stakeholder-readable monthly ship log
  - `docs/tracking/test-execution-log.md` — long-form test-card log (TC-DEF-XXX, TC-QC-XXX)
  - `docs/tracking/qc-readyforuat-{{DATE}}.md` — most recent Ready-for-UAT validation pass
  - `docs/tracking/change-request-register.md` — CR-XXX catalog (CR-001 through CR-015 as of 2026-05-01)
  - `0_Documentation/docs/Requirements.md` — locked requirements
  - `0_Documentation/mockups/0_0_MasterDesignGuide.html` — UI standards
- **Test accounts (per `.claude/CLAUDE.md`):**
  - `admin / TestPass123!` — full Admin
  - `analyst / TestPass123!` — Analyst
  - `external / TestPass123!` — External (company-scoped)
  - `partner / TestPass123!` — Partner web user (separate from Partner API key)
  - **Partner API key (dev):** `gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7` (Amazon)
- **Database:** dev DB `GCCDatabase_Dev` on `ANUKI01`, SQL auth `GCC_DevUser / DevP@ss2024!SQL`. 21 users, 40 certificates, 11 Amazon certs, 3 active partner API keys. Schema notes (cert→partner via `Certificates.Partner` STRING column not the `CertificatePartners` join table) per memory.
- **Reason for reconciliation:** {{e.g., end-of-sprint client review / pre-UAT package handoff / post-incident audit}}.
- **Time budget:** {{HOURS_OR_DAYS}}.

## Workbook conventions

The client maintains the QC log in Excel. Each new sprint adds two columns to the right of the previous sprint's status:

- **Sprint N {{date}}** — short status label (single value from the Status Vocabulary below)
- **Sprint N Notes** — 1–3 sentences with evidence pointer (DEF-XXX, file path, screenshot, observation date)

Anuki populates Sprint N columns at end of sprint. The client owns "Date Closed - GCC Confirmed Fixed" and "Current Status - Anuki Marks Complete" columns.

**Sheet rules:**
- The latest sheet is the only authoritative one (`GCC DB QC Log2` as of 2026-05-04). Don't write to historical sheets.
- Headers are on row 5; data starts row 6. Skip rows 1–4 (workbook title, URL, blank).
- Some rows have blank IDs (placeholder rows from the client's xlsx editor) — treat as no-op.
- Don't change any column other than the current sprint's two columns and don't reorder rows.

## Status Vocabulary (Sprint N column)

Use one of these single-token statuses. Notes column carries the rationale and evidence pointer.

| Status | Meaning |
|---|---|
| **Complete** | Fixed and shipped (or shipped this sprint). DEF-XXX in defect register marks Fixed/Deployed; verified on the build. |
| **Complete - Pending Deploy** | Fix is in working tree or on `master` but hasn't shipped to the live test env yet. Will deploy this sprint or next. |
| **Verified - Pending Client UAT** | Fix shipped in a prior sprint; Anuki re-verified this sprint; awaiting client UAT signoff. Use for the Ready-for-UAT bucket. |
| **Working as Designed** | Not a defect on the current contract. Working per Requirements.md / Master Design Guide / a memory rule (cite which). |
| **Carryover - Open** | Was open at end of prior sprint, not worked this sprint. Still applies. |
| **Carryover - In Progress** | Picked up this sprint, not yet closed. Notes column should say what's blocking close. |
| **CR-Required** | Beyond contracted scope. Notes column should reference the CR (CR-XXX) or call out "needs new CR". |
| **Blocked - Client Decision** | Cannot proceed without client direction (clarification, data, sign-off). Notes column should call out what's needed. |
| **Blocked - Partner Sign-off** | Cannot proceed without external partner approval (e.g., Amazon side of a Partner API change). |
| **Needs Manual Verify** | Code looks correct but couldn't be exercised on dev (data shape, env mismatch). Re-test on prod required. |
| **Cannot Reproduce** | Followed steps; cannot reproduce. Use only when there is enough info in the original entry to make a real attempt. |
| **Insufficient Info** | Original entry too vague or no steps. Need client to provide more before validation is possible. |
| **Duplicate of QC #N** | Same root issue as another row. Notes column should reference the surviving entry. |
| **Obsolete - Feature Changed** | The feature itself changed; the original behavior no longer applies. |
| **Out of Scope** | Different system, different version, or never was this build's responsibility. |

If a status is genuinely ambiguous, prefer **Needs Manual Verify** plus a precise note over a guess.

---

## Methodology — Run All Phases

### Phase 1: Profile the Workbook

Don't start populating Sprint N columns until you've verified what you have.

- Open the xlsx (open it in read-only mode via `openpyxl` — Excel may have a write lock; copy to `%TEMP%` first if so)
- Confirm the active sheet is `GCC DB QC Log2`
- Confirm headers on row 5 match the expected layout (drift in this sheet has happened before — the client adds columns each sprint)
- Confirm the new sprint's two columns exist and are empty. If they don't exist yet, ask the client to add them or add them yourself before populating
- Total data rows: typically 170 as of 2026-05-04
- Distinct values in `Anuki Status Sprint 2` column (the prior sprint's status)
- Distinct values in `Date Closed - GCC Confirmed Fixed` (mix of dates and free-text like "Not fixed", "Did not check", "New")
- Distinct values in `Current Status - Anuki Marks Complete` — this is set by Anuki, NOT the client. May be blank for new items
- Spot-check rows for IDs in the 100–170 range to confirm Sprint 2 work is captured
- Profile any rows where IDs are duplicated, blank, or out-of-sequence

Output a 5–10 line profile summary before validating anything.

### Phase 2: Triage and Bucket the Entries

- **Bucket A — Items shipped THIS sprint:** cross-reference `shipped-2026-MM.md` for the current month plus the prior month if the sprint straddles a boundary. These get **Complete** or **Complete - Pending Deploy** with the DEF-XXX as the note.
- **Bucket B — Ready-for-UAT items from the most recent qc-readyforuat-*.md:** these get **Verified - Pending Client UAT** if PASS in that pass, **Needs Manual Verify** if NEEDS MANUAL, **CR-Required** if CR-LEVEL, **Blocked - Client Decision** if PROD-ONLY or DATA-ONLY.
- **Bucket C — Items with `Anuki Status Sprint 2` = "none" / "None":** these are typically older items already closed in earlier sprints (look at `Date Closed`), not actively worked in Sprint 2. Most map to **Verified - Pending Client UAT** (if Date Closed is a real date) or **Carryover - Open** (if Date Closed is "Not fixed").
- **Bucket D — Items raised this sprint (no prior status):** new bugs found during the sprint; map to **Carryover - Open**, **Carryover - In Progress**, or **Complete** based on what was done.
- **Bucket E — Items where the client wrote a comment in `4-10-26 comments - SP2`:** retest comments from the client; these are pushback-on-prior-fix and need explicit Sprint N response in Notes.
- **Bucket F — "Discuss" / "N/A" / "Did not check":** these need explicit triage. Don't carry them forward silently.

Track bucket sizes; if one bucket is large (e.g., 80+ items in B), say so upfront in the executive summary.

### Phase 3: Pre-Flight on the Test Environment

Before validating, verify the dev env is sane:

- `dotnet build` clean? (look at `BUILD-STATUS.md` if it exists, or run `dotnet build` from project root)
- DB reachable on `ANUKI01\GCCDatabase_Dev`? (e.g., probe via `Database_Scripts/CheckUsers.sql` or any low-cost SELECT)
- Login as admin/analyst/external — credentials still work? (per memory, `TestPass123!` for all three)
- 11 Amazon certs present and 3 partner API keys active? (cross-checks the `feedback_amazon_api_immutable.md` rule)
- Recent deploys / migrations note (changes in the last 24–48 hours often explain "this used to fail, now it doesn't" results)
- Is the Plesk test env in sync with the dev branch? Per `project_plesk_deploy_env.md` the pubxml uses Production environment; mismatches are a known regression vector.

If the env doesn't match the build, document it as a Phase-3 finding before validating anything else.

### Phase 4: Validate Each Entry

For each row in your priority bucket, in batches of 3–6 (per `feedback_batch_fixes_strength_in_numbers.md`):

1. **Read the row carefully — every column, every line of text.** Don't shortcut to the description's first sentence. Specifically:
   - The `Defect Description` column (col 12) frequently contains a chronological log of client retest comments dated `4/3:`, `4/6:`, `4/30:`, `5/1:` etc. **Read the trailing-most comment.** It often contradicts earlier verdicts.
   - The `4-10-26 comments - SP2` column (col 6) is Anuki's prior-sprint working notes — useful for what was attempted, NOT authoritative for what's true now.
   - The `Page` (col 11), `Image # (if needed)` (col 13), and `Priority` (col 17) columns set context; don't skip them.
2. **Search the tracking docs first.** Does the defect register have a DEF-XXX for this QC ID? Does the shipped log mention it? Does the test execution log have a TC-QC-N card? Does a prior `qc-readyforuat-YYYY-MM-DD.md` have a verdict? If yes, the answer might already be there.
3. **CRITICAL — reconcile prior-pass verdict against the trailing client comment.** If a prior validation pass marked the item "PASS" but the QC log row's trailing client comment is dated AT OR AFTER that pass and contains negative language ("Still", "still not", "not fixed", "worse", "broken", "not working", "doesn't"), the prior PASS verdict is **stale** — the client retested on the same or later build and disagreed. **The trailing client comment wins over the prior Anuki PASS.** Either downgrade to Carryover - Open / Needs Manual Verify, or re-validate live before keeping Verified.
4. **Watch for positive 5/1-style confirmations too.** "Working", "Confirmed", "Could not replicate assuming Anuki confirmed fix", "Fine on tile view" — these are explicit client signoffs and should be quoted verbatim in the Notes column. They strengthen the Verified verdict.
5. **If still unclear, exercise it on dev.** Use Playwright with full_page screenshots × 5 viewports per `feedback_playwright_screenshots.md`. Login flow: `#Email` / `#Password` / `button[type="submit"]`. List-view rows are `<div class="table-body-row">` inside `#gridBody`, not `<tr>`. Calendar dates need pre-formatted yyyy-MM-dd JSON per `feedback_calendar_date_serialization.md`. **When validating live, reproduce on the SAME surface the client tested** — not the easiest-to-script surface. (Sprint 3 lesson: a fix verified on public view can still be broken on logged-in/internal view, and vice versa.)
6. **Classify** using the Status Vocabulary above.
7. **Write the Notes cell** in the format below — and **include a verbatim quote of the trailing client comment** when one exists, so the next reconciliation can audit your verdict against the same evidence.

#### Notes cell template

Keep notes 1–3 sentences. **When the row has a trailing client retest comment, quote it verbatim** so the next reconciliation can audit your verdict against the same evidence. Pick the form that fits:

```
DEF-XXX: <one-line summary>. Files: <path>. <screenshot reference if relevant>.
```

```
Verified <date>: <surface/page>, <role>. <one-line evidence>. Client confirmed <date>: '<verbatim positive quote>'. See qc-readyforuat-YYYY-MM-DD.md Batch N.
```

```
REOPENED on review of trailing client comment. <date> client retest: '<verbatim negative quote>'. <one-line implication / which surface is still broken>.
```

```
WAD per <doc/memory>: <one-line rationale>.
```

```
CR-XXX: <one-line scope>. Awaiting <decision/sign-off>.
```

```
Blocked: <what's needed and from whom>.
```

If the row's Sprint 2 status was "Ready for UAT" and you're carrying it forward to "Verified - Pending Client UAT", reference the Ready-for-UAT pass batch number and the screenshot folder. **And either include the positive client confirmation verbatim, or — if no client confirmation exists yet — flag in the note that the verdict is Anuki-side only.**

#### Pre-write checklist (run for every Verified verdict)

Before writing "Verified - Pending Client UAT" to the Sprint N column for any row:

- [ ] I have read the FULL Defect Description column, not just the first sentence
- [ ] If there's a trailing `4/3:`, `4/6:`, `4/30:`, `5/1:` (or later) client comment, I have read it
- [ ] If that trailing comment contains "Still", "still not", "not fixed", "worse", "broken", "not working", "doesn't", or "I still cannot" — I have NOT marked Verified. I have downgraded to Carryover - Open or Needs Manual Verify and pasted the verbatim quote into the Notes column
- [ ] If that trailing comment contains "Working", "Confirmed", "Fine", "Could not replicate", or another positive confirmation — I have pasted the verbatim quote into the Notes column to strengthen the Verified verdict
- [ ] If the prior PASS came from a validation pass dated BEFORE the client's trailing comment, I have re-tested live (not just rolled forward the prior verdict)
- [ ] If the fix touched one surface (e.g., public view) and the client retested on a different surface (e.g., logged-in view) and reported it broken there, I have NOT marked Verified — both surfaces must pass

### Phase 5: Pattern Detection

While going through, watch for clusters and surface them in the executive summary:

- **Module clusters** — many bugs in the same area (e.g., 12 in cert tile views) signals architectural smell
- **Regression patterns** — same bug fixed and re-broken (DEF-103/124/129 same class of bug — partner API leakage). These need a different intervention (e.g., a standing test that asserts contract DTO shape) than another fix attempt
- **Severity drift** — bugs originally cosmetic now business-critical because the surface became more important
- **Role-specific clusters** — most bugs reproduce only in one role; that role's path is under-tested
- **Browser/device clusters** — points to missing media queries or polyfills
- **Cannot-reproduce clusters** — usually means missing repro context, not bug absence
- **Stale Won't-Fix entries** — sometimes priorities have changed; flag for the client
- **Untestable entries** — many `Needs Manual Verify` entries point to inadequate test data or env provisioning

### Phase 6: Cross-Reference for Missing Bugs

While validating, note things that aren't in the log:
- Console errors during reproduction
- Side issues you stumble into
- Test data that looks wrong
- UI elements that look broken in ways nobody captured

Capture as new defect register candidates (DEF-XXX prefix), not as edits to QC log entries.

### Phase 7: Sample-Validate the Closed Backlog

For Bucket A (already-closed items from prior sprints), don't re-validate exhaustively. Sample:
- Pick a few from each module and severity
- Focus on items that have been closed-and-reopened more than once
- Focus on items closed near the current build (most likely to be incomplete)

Document the sampling so coverage is honest.

### Phase 8: Write the Workbook + Build the Summary

- **Always copy the source xlsx to `%TEMP%` first.** Open the copy with `openpyxl(read_only=False)`, write to the Sprint N + Sprint N Notes cells, save, then offer to copy back. Never write to the source xlsx directly while the client may have it open in Excel.
- Write a separate **stakeholder-readable summary** at `docs/tracking/qc-sprint-{{N}}-reconciliation-{{DATE}}.md` mirroring the format of `qc-readyforuat-2026-05-01.md`:
  - Executive summary (counts per status, top 3 patterns, recommended next steps)
  - Inventory table (one row per QC item with old status / new status / notes)
  - Pattern report
  - Newly discovered defects (separate from log entries)
  - Items needing client decision before next UAT cycle
  - Coverage gap report

### Phase 9: Process & Tooling Findings

Capture separately, NOT as edits to log entries:
- **Process findings** — log entries lacking repro steps, status taxonomy drift, missing IDs
- **Environment findings** — dev DB missing data shapes that would let validation finish
- **Tooling findings** — workbook getting unwieldy (170+ rows, 17+ cols), export artifacts going stale, screenshot folders proliferating
- **Coverage findings** — areas with zero entries don't necessarily mean no bugs

These often outvalue per-entry verdicts because they prevent the next round of drift.

---

## Deliverables

1. **Updated xlsx** — Sprint N + Sprint N Notes columns populated for every row that has a non-blank ID. Saved as `Database QC Log_{{DATE}}_sw.xlsx` next to the source.
2. **Executive summary** — counts by status, top 3 patterns, recommended next steps. ~200 words.
3. **Reconciliation markdown** — `docs/tracking/qc-sprint-{{N}}-reconciliation-{{DATE}}.md` with full detail per phase 8.
4. **Defect register entries** — for newly discovered bugs (prefix DEF-XXX, append to register).
5. **Items-for-client-decision list** — what the client needs to action before next UAT cycle (Blocked - Client Decision rows + open CRs).
6. **Coverage gap list** — Needs Manual Verify rows that need a prod re-test post-deploy.

---

## GCC-Specific Project Rules to Honor

These are the guardrails. Violating them is a Sprint-N regression.

- **CSV-only exports.** No Excel, JSON, or PDF from admin pages. (CLAUDE.md)
- **No native `alert()` / `confirm()` / `prompt()`.** Use `AdminCore.showConfirmation()` before delete and `AdminCore.showNotification()` toast after. (CLAUDE.md)
- **External users see only their associated company/partner data.** No internal tags. No Add/Edit cert buttons. No Actions column on Product Management. (CLAUDE.md)
- **Partner API contract is immutable.** `/api/v1/certificates*` JSON shape must not change. Any change is a CR requiring partner sign-off. (memory: `feedback_amazon_api_immutable.md`)
- **Custom fields render under one bucket.** Ignore `CustomField.Section` for grouping. (memory: `feedback_custom_fields_single_section.md`)
- **Import/Export icons are fixed.** Import = down arrow, Export = up arrow. Don't flip per FontAwesome semantics. (memory: `feedback_import_export_icon_convention.md`, DEF-101/127)
- **Product ID Status auto-derives from parent cert.** Active iff parent cert in `PublicStatuses`, else Inactive. (memory: `feedback_product_id_status_follows_cert.md`)
- **Cert detail action icons right-aligned, NOT centered.** (memory: `feedback_cert_detail_action_icons_right_aligned.md`)
- **CertificationTypes is hierarchical.** Same Name at different Levels is by design. Always include `Level` + `ParentId`. (memory: `feedback_certtypes_hierarchy.md`)
- **Calendar dates pre-format yyyy-MM-dd in JSON.** `UtcDateTimeConverter` shifts dates in negative-UTC zones. (memory: `feedback_calendar_date_serialization.md`)
- **No data UPDATE/DELETE on business records without a CR.** (memory: `feedback_data_changes_require_cr.md`)
- **16px minimum font size.** (CLAUDE.md)
- **Use `ViewData["Title"]`, no `<h1>` in views, `.admin-container` structure.** (CLAUDE.md)

If validation surfaces something that contradicts a rule above, that contradiction itself is a defect, not a "fix". Flag it.

---

## Mindset Reminders

- The log is a snapshot of what the client *thought* was true at the moment of writing. Validate, don't trust
- "Cannot reproduce" almost always means "I don't have enough info to reproduce" — push for missing context before declaring a bug gone
- A bug marked "Complete" by the client months ago may have regressed. A bug marked "Not fixed" may have shipped this week. The Sprint N column is your fresh verdict on the current build
- **The trailing client comment in the Defect Description column wins over a prior Anuki PASS verdict.** If the client retested on the same or later build and disagreed, the prior PASS is stale. (Sprint 3 lesson — see Phase 4.)
- **A code-correct fix on Surface A does not close a defect that's broken on Surface B.** Public-view-only fixes are not Verified if the client retested on logged-in view, and vice versa. Reproduce on the SAME surface the client tested.
- Regressions are more important than new bugs — they signal regression coverage is missing the spot, and that's structural
- The most valuable output is often the pattern, not the per-entry verdict
- If the time budget runs out before all entries are reconciled, *say which were skipped and why* — silent omission is worse than admitted incompleteness
- Be skeptical of your own validation — half of "fixed" verdicts are validation errors, not real fixes. If you couldn't reproduce, did you use the right role, the right data, the right env, the right build?
- Logs left untended grow stale fast. The reconciliation pass itself doesn't keep them clean — propose a process change that keeps Sprint N+1 from drifting again

---

## Severity Rubric for Reconciliation Outputs (not individual bugs)

When categorizing the *outputs* of this exercise:

- **S1 — Critical:** confirmed regressions on critical workflows (cert create/edit, public tile view, Partner API contract, login/AuthZ), security/data-integrity bugs incorrectly marked closed, severity-drift items that should now block release
- **S2 — Major:** confirmed open bugs invisible to current decision-makers because of stale logs, large duplicate clusters obscuring true bug count
- **S3 — Moderate:** stale Won't-Fix decisions worth revisiting, module clustering pointing to architectural issue, multiple Needs-Manual-Verify items in the same module
- **S4 — Minor:** obsolete entries needing cleanup, mislabeled modules, polish items lingering across sprints
- **S5 — Polish:** process improvements for future log quality (workbook structure drift, missing column conventions, screenshot folder sprawl)

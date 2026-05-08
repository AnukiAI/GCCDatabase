# Defect Tracker Triage Audit — 2026-05-07

**Scope of audit:** `docs/tracking/defect-register.md` (DEF-XXX register, the Anuki-side audit trail) cross-checked against `docs/tracking/qc-sprint-3-reconciliation-2026-05-04.md` (which carries the client QC log retest comments) and `docs/tracking/qc-log-validation-prompt-gcc.md` (the canonical status taxonomy).

**Author:** Triage hygiene pass, no tracker edits made.

**Closure recommendation legend** (used in tables below):

| Code | Meaning |
|---|---|
| **Close** | Verified Fixed/Deployed; ready to mark Closed once the deploy + retest event has been recorded. |
| **Reopen** | Trailing evidence (client retest comment, surface mismatch, or scope gap) shows the ticket is not actually resolved. |
| **Defer** | Out of current sprint scope: Won't Fix, CR-Required, Out of Scope, or Blocked - Configuration/Decision. Move to a holding bucket — don't close silently. |
| **Needs PO decision** | Genuinely ambiguous from the artifacts on hand. The audit will not classify; flag and explain what the PO has to decide. |

---

## Executive summary of findings

Three structural issues dominate the register; per-ticket ambiguity is mostly downstream of these.

1. **Six pairs of colliding defect IDs.** DEF-109, DEF-110, DEF-111, DEF-112, DEF-113, and DEF-114 each have **two different entries** describing different defects. Looking at the file: lines 21-1308 carry one numbering sequence (newest at top: DEF-160 down to DEF-107), and lines 1503-1812 carry an earlier sequence (DEF-104 through DEF-114) that was apparently merged in without renumbering. Status, severity, and reporter all differ between the two entries sharing each ID — i.e. these are *real* duplicates, not two sides of the same fix. Until renumbered, half the audit trail can't be cited unambiguously.
2. **Status taxonomy drift.** The register's column header defines five values: `Open / In Progress / Fixed / Deployed / Closed`. Actual usage has at least **14 distinct status strings**, with deploy state and verification state collapsed into the same field (`Fixed locally (pending deploy)`, `Fixed (pending UAT / deploy)`, `Fixed (stopgap + DTO refactor)`, `Fixed (source + dev DB) — pending prod SQL run`, `Open — defer; not worth a fix unless touched anyway`, `VOIDED`, `Closed — Won't Fix`, etc.). The canonical `qc-log-validation-prompt-gcc.md` taxonomy already exists for the QC log — the defect register has not adopted it.
3. **No retest history.** A ticket is written once and updated in place. There is no append-only retest log per ticket, so when the same defect is "fixed" multiple times (DEF-103 → DEF-124 → DEF-129 are the same Partner-API entity-leak class, each fixed in turn after the prior fix's "all endpoints" claim turned out to be wrong), the audit trail has to be reconstructed from prose. The column spec doesn't capture deploy date, retest-by-whom-on-what-date, or the trailing client retest comment that (per the validation prompt) is supposed to outweigh a prior PASS verdict.

The combined effect: **closure decisions on individual tickets cannot be made cleanly** until the tracker structure problems are fixed. Recommendations below should therefore be read as "what to do if the structural fix were already in place"; some tickets are flagged "Needs PO decision" specifically because the structural ambiguity makes a clean call impossible from the artifacts on hand.

---

## Flagged tickets — closure ambiguity

### Group 1 — ID collisions (structural, blocks clean closure)

These six IDs each cover two different defects. Until renumbered, neither half can be cleanly closed because "DEF-NNN" no longer uniquely identifies one finding.

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-05-01 | DEF-109 (entry A) — Tile listings render Effective/Expiration date one day earlier than the DB value | Display bug | "**Closed** — deployed and validated by Shawn on 2026-05-01" |
| 2026-05-05 | DEF-109 (entry B) — Product ID "Created By" column shows "System" instead of the user who added it | Audit-data bug | Fixed (pending UAT) |
| 2026-05-03 | DEF-110 (entry A) — CSV export headers don't match UI table column labels (12 export surfaces) | Polish/consistency | Fixed (local) |
| 2026-05-05 | DEF-110 (entry B) — Admin `/api/certificates` POST/PUT path silently dropped CreatedBy / UpdatedBy | Audit-data bug | Fixed (pending UAT) |
| 2026-05-04 | DEF-111 (entry A) — `/Product/Management` "SyntaxError: Unexpected token '<'"; fetch parses HTML auth-redirect as JSON | Functional bug | Deployed 2026-05-04 |
| 2026-05-06 | DEF-111 (entry B) — Product ID import-template column order didn't match the on-screen Product ID Manager table | UX consistency | Fixed (pending UAT) |
| 2026-05-04 | DEF-112 (entry A) — Partner role: web-user "Partner" vs Partner API "Partner" share the same code path with different identities | Architectural | Fixed (source + dev DB) — pending prod SQL run; **Path 4 selected** |
| 2026-05-06 | DEF-112 (entry B) — Bulk Product ID import capped at 100 rows due to per-row HTTP requests hitting AspNetCoreRateLimit | Performance/blocker | Fixed (pending UAT) |
| 2026-05-07 | DEF-113 (entry A) — Remove `/Admin/Index` admin dashboard page (client doesn't want it) | Feature removal | Fixed — pending deploy |
| 2026-05-06 | DEF-113 (entry B) — Same per-row HTTP-POST anti-pattern across 4 admin imports (Companies, Tags, Products, Users) | Performance/blocker | Fixed (pending UAT) |
| 2026-05-07 | DEF-114 (entry A) — Dashboard mobile portrait: column 2 of stats tables clipped, headers/values truncated | Mobile layout | Fixed — pending deploy |
| 2026-05-07 | DEF-114 (entry B) — Main nav bar misaligned with page content (flush left edge instead of inside `.page-container`) | Global layout | Fixed |

**Closure recommendation: Needs PO decision** for the entire group.
**Reasoning:** Renumbering published defect IDs is risky because external artifacts cite them — the May ship log, the test execution log, CR docs, and the QC log Notes column all carry "DEF-109", "DEF-112" etc. as references. Two reasonable resolutions exist and only Shawn can pick:
- (a) Renumber the older block by adding a constant offset (e.g., entry A in each pair stays as DEF-NNN; entry B gets DEF-200+). Update all citations.
- (b) Suffix with a letter (DEF-109a, DEF-109b). Cheaper to retrofit but breaks the "monotonic ID" promise stated in the register's preamble (line 9: *"`DEF-XXX`, monotonically increasing"*).

**Blockers:**
1. PO decision on renumber strategy.
2. Inventory of external citations (`grep "DEF-10[9]\b\|DEF-11[01234]\b" docs/`) before renumbering, so all references move atomically.
3. Decision on whether the architectural Path 4 in DEF-112 (entry A) was reversed by DEF-142, which states *"reverses DEF-112's resolution: DEF-112 (resolved 2026-05-04) chose Path 4 (eliminate web-user Partner) on the assumption no human Partner users were needed... Shawn confirmed 2026-05-05 that the actual requirement is the opposite"* — i.e. DEF-112 (entry A) is not actually still resolved; it was undone within 24 hours. Status as written is misleading.

---

### Group 2 — DEF-103 / DEF-124 / DEF-129 (Partner API entity-leak cluster)

Three separate entries, same root cause class, each one shipped after a prior entry's "all endpoints fixed" claim turned out to be incomplete.

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-04-21 | DEF-103 — Partner API by-number and by-id endpoints leak raw certificate entity (status, country, internal IDs) | Security/contract | Fixed locally — pending deploy |
| 2026-05-03 | DEF-124 — Partner API `/api/v1/certificates/expiring` leaks raw entity | Security/contract (S1) | Fixed (pending deploy) |
| 2026-05-03 | DEF-129 — Partner API `/api/v1/certificates/search` leaks raw entity (same bug class as DEF-103/DEF-124) | Security/contract (S1) | Fixed (pending deploy) |

**Quoted reasoning from the register itself (DEF-124, line 936) — captures why "Closed" on DEF-103 was premature:**

> *"Why this got through 2026-04-21 fix: DEF-103's commit message claimed 'all 6 partner endpoints fixed' but the actual fix only touched list, GS1 list, GS1 single, and missed `/expiring`, `/{id}`, `/number/{n}`. The 2026-04-30 self-test caught `/{id}` + `/number/{n}` and fixed them. `/expiring` wasn't in scope of either pass — slipped through."*

**Closure recommendation per ticket:**
- **DEF-103: Close** once deploy is recorded and a regression test asserts all 6 partner endpoints return the contract DTO shape (currently no such standing test exists; it's an explicit gap called out in DEF-124's "Audit needed").
- **DEF-124: Close** alongside DEF-103, conditional on the same regression test landing.
- **DEF-129: Close** alongside DEF-103/124 (same conditions).

**Blockers (apply to all three):**
1. Deploy event not recorded in the register — current Status field is still "pending deploy" on all three despite same-day fixes 2026-05-03. If they shipped, update to `Deployed YYYY-MM-DD`. If not, the cluster is sitting on an undeployed S1 critical security finding that's been "fixed locally" since 2026-05-03, which by itself is a triage-hygiene flag.
2. No standing contract-shape test exists. Until one does, the "fixed all 6 endpoints" claim is unverifiable and the cluster is structurally vulnerable to a fourth iteration.
3. Confirmation needed on whether prod was ever serving the leaky shape to Amazon between 2026-04-21 and the deploy date. DEF-103 (line 1500) says *"likely had the same defect prior — partner integration would have been getting `status: 'Client Approved - Paid'`... Worth a heads-up email when this ships."* No record that the heads-up email was sent.

---

### Group 3 — DEF-104 (VOIDED + CR-015 collision)

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-04-30 | DEF-104 (entry A) — Product ID per-row Status drifted from parent certificate (CR-015 build) | Data hygiene + UI scope | Fixed locally + dev DB backfilled + PROD DB backfilled 2026-05-01 — app deploy pending |
| 2026-04-30 | DEF-104 (entry B) — VOIDED 2026-04-30 (not a defect) | Withdrawn | n/a |

**Quoted reasoning for the VOID (line 1530):**

> *"Originally logged as a regression of QC #31 (download icon position at desktop). On 2026-04-30 the user confirmed: right-aligned is the intended behavior, not centered. ... My test card had the wrong expected outcome — TC-QC-031 PASSes at all viewports. No code change needed."*

**Closure recommendation: Defer (re-ID required) — Needs PO decision** on which ID stays.
**Reasoning:** Two different findings sharing DEF-104. Entry A (CR-015 work) is the substantive one; entry B is a tester-side mistake that should not have consumed an ID. The VOID convention is also outside the register's official `Open / In Progress / Fixed / Deployed / Closed` taxonomy.
**Blockers:**
1. PO decision on whether the VOID convention is permitted, or whether withdrawn-on-review entries should instead be deleted with a note in a separate "Withdrawn" log.
2. Per the [Cert detail action icons are right-aligned](file:///C/Users/swest/.claude/projects/...feedback_cert_detail_action_icons_right_aligned.md) memory, the right-aligned behavior is now codified — entry B should be removed from triage entirely, not preserved as VOID.

---

### Group 4 — DEF-105 (Won't Fix that should never have been a defect)

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-04-30 (revised 2026-05-01) | DEF-105 — "prod has 12 duplicate cert types" (later re-scoped to dev-data hygiene) | Originally logged wrong; corrected | **Closed — Won't Fix** |

**Quoted self-correction in the entry itself (line 1578):**

> *"Original draft of this defect claimed prod had 12 duplicate-name zombie rows. That was wrong. ... The 12 'duplicates' I saw in prod are L2 subcategories under parent 'Certified Sustainability Facts' (Id=6). ... By design, not a defect."*

**Closure recommendation: Close** as documented (Won't Fix is a legitimate terminal state), but **flag as a process finding** — the original defect was based on an incorrect schema read. This pattern is already covered by the [CertificationTypes is hierarchical](file:///C/Users/swest/.claude/projects/...feedback_certtypes_hierarchy.md) memory, which post-dates DEF-105 and was created precisely to prevent the repeat.
**Blockers:** None for closure. The structural finding is a recommendation, not a blocker.

---

### Group 5 — DEF-121 (in-place status churn, no retest log)

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-05-01 (filed) → 2026-05-03 stopgap → 2026-05-03 DTO refactor | DEF-121 — `/api/productids/certificate/{id}` leaks password hashes and 2FA secrets in CreatedByUser / UpdatedByUser | Security/info disclosure (HIGH) | **Fixed (stopgap + DTO refactor)** — pending deploy |

**Closure recommendation: Close** once deploy is recorded.
**Reasoning:** This entry is functionally complete — both layers of defense in place, JS frontend impact verified, audit-needed scope explicitly enumerated. The triage hygiene problem isn't the resolution; it's that **the ticket carries three updates in one Status field** (filed → stopgap → DTO refactor), and the "Followup ticket needed: DTO refactor (option 1)" promise from the stopgap was implicitly fulfilled in-place rather than via a separate ticket. A future tracker reader will not be able to count "how many fix passes did this take" without parsing prose.
**Blockers:**
1. Deploy event not recorded.
2. The "Audit needed: re-grep all `[HttpGet]` actions in `CertificatesController` and confirm every one returns a projected DTO when `userRole == 'Partner'`. Also audit all other API v1 controllers (`ProductsController`, `CompaniesController`) for the same pattern" requirement (line 937) is open. DEF-130 covered ProductsController; CompaniesController and the remaining HttpGet sweep have no DEF entry as of writing.

---

### Group 6 — DEF-125 (zombie "open — defer")

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-05-03 | DEF-125 — 404 vs 302 on Admin routes leaks which controller actions exist | Polish/S5 (security recon) | **Open** — defer; not worth a fix unless touched anyway |

**Quoted from the entry itself (line 883):**

> *"Recommendation: Leave open; revisit only if a security review specifically flags it."*

**Closure recommendation: Defer** — but **status string should be normalized**.
**Reasoning:** "Open — defer; not worth a fix unless touched anyway" is functionally a "Won't Fix until triggered" state, not actually Open. Leaving it as Open inflates the open-defect count and obscures Sprint 3 priorities. The qc-log validation prompt has a clean equivalent: `Working as Designed` (close enough — it's a known posture, intentionally accepted) or a new `Accepted Risk` status if the team wants to keep these visible without inflating the open count.
**Blockers:**
1. PO decision on whether `Accepted Risk` joins the canonical taxonomy. Until then, the team has no clean place to land deferred-low-severity items, which is why DEF-125 is parked in the wrong bucket.

---

### Group 7 — DEF-128 / DEF-130 / DEF-131 (S1 critical, fixed locally, deploy state unclear)

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-05-03 | DEF-128 — `/Certificate/ExportCertificates` is `[AllowAnonymous]`, supports Excel/PDF (rule says CSV-only), and PDF format returns broken file | Security/data exposure (HIGH/S2) | Fixed (pending deploy) |
| 2026-05-03 (filed) → 2026-05-04 (fixed + deployed) | DEF-130 — `/api/v1/products` was Partner-API-key authorized at the class level — exposing GET data leak AND POST/PUT/DELETE mutation methods to partners | Security/privilege escalation (CRITICAL/S1) | **Deployed 2026-05-04** |
| 2026-05-03 | DEF-131 — Partner API keys could POST/PUT/DELETE certificates via `/api/v1/certificates/bulk/*` | Security/privilege escalation (CRITICAL/S1) | Fixed (pending deploy) |

**Closure recommendation:**
- **DEF-128: Close** once deploy is recorded — fix is verified live on `localhost:5101` (line 816); a `Deployed` event hasn't been written into the entry. Per `feedback_track_every_fix_in_defect_register.md` memory, every fix needs a DEF; the symmetric rule should be that every Deploy needs a register update.
- **DEF-130: Close** — already states "Deployed 2026-05-04" with verification quote. This is the *good* version of a closure entry; the others should follow this format.
- **DEF-131: Close** once deploy is recorded — same shape as DEF-128.

**Blockers:**
1. **Aging concern:** DEF-128 and DEF-131 are S1/HIGH security defects fixed locally on 2026-05-03 and (per the May ship log) deployed by 2026-05-04, but the register still says "pending deploy" on 2026-05-07. Either the deploy didn't happen and the cluster is sitting on an undeployed critical, or the deploy did happen and the register isn't being updated post-deploy. Either way, S1/S2 security defects should not have ambiguous deploy state for 96+ hours.
2. The DEF-130 entry shape ("**Deployed 2026-05-04** — verified live on gccwebapp.com: ...") is the template that every other fix-then-deploy ticket should follow.

---

### Group 8 — DEF-138 (client retest pushback, awaiting client retest signoff)

| Date | Item | Type | Status (as written) |
|---|---|---|---|
| 2026-05-05 | DEF-138 — QC #91: Product Mgmt save silently nulled Sku and Model fields on every update | Data integrity (Medium-High) | Fixed (pending deploy) — verified live on `localhost:5101` |

**Quoted client retest comment that triggered this defect (line 546, register's own quote of the QC log):**

> *"5/1 on QC #91: 'There was no product type and subcat filled in from list. So I added to 25-8760 and 25-8777. But they then are not showing in list at all. What is going on. Also, another issue: When I went back into 25-8760 it keeps clearing out Product Name and Product Type even after saving edit (but it kept prod cat and subcat). What is going on? **This is a big issue!**'"*

**Closure recommendation: Needs PO decision.**
**Reasoning:** The register entry diagnoses two distinct symptoms in the client comment and resolves only one in code. The *first* symptom ("not showing in list at all") is explained as the same SKU-display-N/A symptom misread, with no code change. The *second* symptom (Product Name and Product Type clearing) is fixed in code. Whether this counts as "QC #91 closed" depends on whether the client accepts the misread-explanation for symptom 1 — that is a client-facing UX call, not a developer call. The Sprint 3 reconciliation file (line 83) carries `#91 | Product Management | 5/1: BIG regression — products w/ added Type/Subcat not showing in list; re-edit clears Product Name/Type after save` and assigns it `Carryover - Open` — so the QC-log side of the audit trail says NOT closed.
**Blockers:**
1. PO decision on whether to (a) accept the "client misread N/A as cleared field" diagnosis and bring this to client UAT as Verified, or (b) treat the SKU-display-N/A behavior as a separate UX defect (DEF-XXX-N/A) and not declare QC #91 closed until both are addressed. The register entry itself acknowledges (line 559): *"Whether to drop the `SKU: ...` line from the product list cell entirely so future SKU values can't visually disappear and confuse the user. This is a polish question, not a defect."*

---

## Summary of flagged tickets

| Closure recommendation | Count |
|---|---:|
| Close (verified, deploy + retest event must be recorded) | 6 — DEF-103, DEF-124, DEF-129, DEF-105, DEF-121, DEF-130 |
| Close (S1/S2 security, deploy state must be confirmed) | 2 — DEF-128, DEF-131 |
| Defer (status normalization required) | 1 — DEF-125 |
| Needs PO decision | 7 — entire DEF-109/110/111/112/113/114 collision group + DEF-104 (VOIDED) + DEF-138 (QC #91 client retest acceptance) |

**Total flagged:** 16 tickets across 8 groups. The 6 collision-group pairs count as one "Needs PO decision" because the resolution is identical for all six.

---

## Tracker structure recommendations

The current register format leans on free-prose entries with a single `Status` line that conflates fix state, deploy state, and verification state. Per-entry hygiene rises if the structural shape is fixed first. Recommended changes, in priority order:

1. **Resolve ID collisions before any further closure decisions.** Six pairs of duplicate DEF-NNN IDs makes citation unsafe and turns every cross-doc reference (May ship log, test execution log, CR docs, QC log notes) into a "which one?" lookup. The cheapest fix is to suffix the older block with `a` (DEF-104a, DEF-109a, DEF-110a, DEF-111a, DEF-112a, DEF-113a, DEF-114a) and the newer block with `b`. The register's "monotonically increasing" promise is already broken; suffixing acknowledges it explicitly.
2. **Split the `Status` column into three independent fields:**
   - **Fix State:** `Open / In Progress / Fixed / Won't Fix / Withdrawn` (one of, mutually exclusive)
   - **Deploy State:** `Not Deployed / Pending Deploy / Deployed YYYY-MM-DD` (paired with a build/zip reference)
   - **Verification State:** `Anuki-Verified / Pending Client UAT / Client-Verified / Client-Reopened` (mirrors the qc-log validation prompt taxonomy already in use one folder over)

   This eliminates the `Fixed (pending UAT / deploy)` / `Fixed locally + dev DB backfilled + PROD DB backfilled — app deploy pending` / `Fixed (stopgap + DTO refactor)` ambiguity at structural cost zero.
3. **Add a Retest History block per entry.** Append-only, dated. Each entry adds a single line: `2026-05-04: Anuki-side localhost smoke — PASS (script: ...)` or `2026-05-01: Client retest comment on QC #91: 'this is a big issue' — REOPEN`. The DEF-103 → DEF-124 → DEF-129 cluster only exists because the "all 6 endpoints fixed" claim on DEF-103 went unchallenged; an explicit retest history would have surfaced the gap on the day DEF-103 was supposedly closed, not 12 days later.
4. **Adopt the `qc-log-validation-prompt-gcc.md` Status Vocabulary as the canonical taxonomy** for both the QC log and the defect register. The two artifacts already cross-reference each other; using the same vocabulary removes the per-document translation step. Add `Withdrawn` (replacing the ad-hoc `VOIDED`) and `Accepted Risk` (replacing the ad-hoc `Open — defer; not worth a fix unless touched anyway`) so the register doesn't need the prose escape hatches it currently uses.
5. **Add a Reporter taxonomy field (single-value):** `Client / Anuki Internal QA / Anuki Self-Test / Cross-reference (QC #N)`. Today the register conflates Shawn (Anuki PO + internal tester) with the client (whose comments live in the QC log under `Carly`, `client`, etc.). Per memory `feedback_say_client_not_names.md`, internal docs should refer to "client" — but the register currently has 50+ entries that read "Reporter: Shawn" and four that read "Reporter: Anuki internal QA pass," with no clean way to filter "what came from the client side."
6. **Move CR-Required and feature-removal items out of the defect register.** DEF-113 (entry A) is "Remove `/Admin/Index` admin dashboard page (client doesn't want it)" — that's a feature removal CR, not a defect. DEF-150/151 reference CR-017 (form recovery rework) as the Phase 2 ticket. CRs already live in `change-request-register.md`; the defect register should not also carry them. This matters for triage protocol (item below) — a CR doesn't go through "open → fix → deploy → verify"; it goes through scope approval → CR contract → implementation milestones.
7. **Cap "Pending Deploy" age.** Any entry that's been `Pending Deploy` for >7 days is by definition a triage event: either the deploy is genuinely blocked (in which case escalate), or the entry isn't being maintained post-deploy (in which case the entry is wrong). DEF-128, DEF-131, DEF-103, DEF-124, DEF-129 all fail this check today.

---

## Suggested triage protocol

At intake (any new defect): record the canonical fields atomically and resist consolidating them — `ID`, `Date Opened`, `Reporter (Client / Anuki Internal QA / Anuki Self-Test / Cross-reference)`, `Severity`, `Page/Surface`, `QC log row reference if applicable`, `Repro steps`, `Expected vs actual`, and an empty `Retest History` block. Set `Fix State = Open`, `Deploy State = Not Deployed`, `Verification State = Pending Anuki-Side`. Do not commingle severity with status, do not embed deploy intent in the title, and do not back-fill test cards retroactively into the entry — append them to the Retest History block dated. On every retest (Anuki-side OR client-side), append one line to Retest History with `YYYY-MM-DD | who | surface | result (PASS/FAIL/REOPEN)` and a verbatim quote of any negative client comment per the validation prompt's Phase 4 rule; if the line says REOPEN, flip `Fix State` back to `In Progress` and clear `Verification State` — never let a Verified state survive a documented client REOPEN. On deploy, update `Deploy State = Deployed YYYY-MM-DD` with the build/zip reference and add a Retest History line; do not assume verification carries over from the local Anuki-side smoke. Closure requires three things in this order: `Fix State = Fixed`, `Deploy State = Deployed`, and `Verification State = Client-Verified` OR `Verification State = Anuki-Verified` with an explicit PO note that client UAT is not gating closure for this severity tier — that PO note is the only path to close without a client retest line in the history. Anything that doesn't fit (CR-Required, Won't Fix, Out of Scope, Accepted Risk, Withdrawn) takes a terminal `Fix State` value and exits the register's open-count. When in doubt at any of the four stages — flag with `Needs PO decision` and stop; do not guess.

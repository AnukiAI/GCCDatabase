# Sprint 3 QC Log Reconciliation — 2026-05-04

**Source:** `Database QC Log_5.1.26_sw.xlsx`, sheet **`GCC DB QC Log2`**
**Output:** `Database QC Log_5.4.26_sw_sprint3.xlsx` (next to source) — Sprint 3 5-4 + Sprint 3 Notes columns populated for all 170 items
**Method:** desk reconciliation against the defect register, May ship log, and the 2026-05-01 Ready-for-UAT validation pass. No live re-validation today; this carries forward Sprint 2's verified verdicts and applies fresh classification to items that had no Sprint 2 status.
**Validation prompt template:** `docs/tracking/qc-log-validation-prompt-gcc.md`

**Revision history:**
- **2026-05-04 12:41** — initial pass (130 Verified / 10 Needs Manual / 7 Carryover-Open / 7 Carryover-In Progress / 5 CR-Required / 4 WAD / 3 OOS / 4 other)
- **2026-05-04 ~16:00** — per-row audit of trailing 4/3, 4/30, 5/1 client retest comments. Found 39 rows initially marked "Verified - Pending Client UAT" with pushback patterns. Hand-curated review downgraded 14 (one was already #156): #23, #78, #91, #104, #106→CR-Required, #107, #109, #118, #124→Needs Manual Verify, #127, #131, #141, #155. The remaining 19 had positive 5/1 confirmations or were resolved-with-known-followup; their notes were clarified to reference the specific client comment.

---

## Executive summary

170 QC items reconciled in a single pass + post-audit corrections. Status taxonomy from `qc-log-validation-prompt-gcc.md` (no-deviation).

| Sprint 3 status | Count | % | Action owner |
|---|---:|---:|---|
| Verified - Pending Client UAT | 116 | 68% | **Client** — UAT signoff |
| Carryover - Open | 19 | 11% | Anuki — Sprint 3 fix |
| Needs Manual Verify | 11 | 6% | Anuki — re-test on prod after deploy |
| Carryover - In Progress | 7 | 4% | Anuki — finish Sprint 3 |
| CR-Required | 6 | 4% | **Client** — CR scope decision |
| Working as Designed | 4 | 2% | (closed) |
| Out of Scope | 3 | 2% | (closed) |
| Blocked - Client Decision | 1 | 1% | **Client** — decision |
| Blocked - Configuration | 1 | 1% | Infra — SMTP setup |
| Insufficient Info | 1 | 1% | **Client** — clarify |
| Duplicate | 1 | 1% | (closed) |

**Top 4 patterns:**

1. **68% of the backlog is awaiting client UAT signoff.** The bottleneck for closing the launch gap is no longer Anuki development — it's client-side acceptance testing on the post-deploy build. Recommend scheduling a focused UAT block with Carly to walk the 116 items.
2. **The 5/1 retest comments revealed 13 hidden Sprint-2 misses.** The 2026-05-01 Ready-for-UAT pass verified the *code* changes for 55 items but didn't surface the client's same-day retest comments. After hand-review, 13 items the client claimed "Still not fixed" or "It's worse now" at 5/1 had to be downgraded from Verified — most concerning regressions: **#91 Product Mgmt edit/save (clearing fields), #141/#155 Dashboard exports "worse now where no fields are showing", #107 RBAC + password-reset flow not actually scoping data**.
3. **Most-recent items #164–#170 still untriaged.** New defects raised at Sprint 2/3 boundary; none has been worked.
4. **The "Needs Manual Verify" cluster maps to specific data shapes missing from dev DB.** #124/#128/#149 (cert-type ordering, LEED, L2-L3 filter), #123 (cert #23-0375 special chars), #136 (timezone display), #157 (sort with data variance). Dev DB doesn't have the data to exercise these — they need a prod re-test post-deploy. **Structural test-data gap, not a string of independent bugs.**

**Recommended next steps (in priority order):**

1. **Sprint 3 fix sweep on the 13 reopened items.** Highest-priority: #91 (Product Mgmt edit clearing fields — "big issue" per client), #107 (Company Access scoping not limiting data), #141/#155 (Dashboard export regression). Then: #23, #78, #104, #118, #124, #127, #131. #109 unblocks once #106/#108 land.
2. **Schedule client UAT pass on the 116 Verified items.** Use the inventory below as the test plan.
3. **Triage #164–#170** — fresh defects raised at Sprint 2/3 boundary, none yet worked.
4. **Run the 11 Needs Manual Verify items on prod immediately after the next deploy** (current pending: build `0006` + DEF-127–133 batch).
5. **Convert the 6 CR-Required items into formal CRs** before scoping more development. Per memory `feedback_data_changes_require_cr.md` and `2026-04-07 retrospective` — no more building without a CR. New CR-Required: #106 (free-text Employer field separate from Company Access).
6. **Schedule a 30-minute call** to clear the 1 Insufficient Info and 1 Blocked - Client Decision item (#151 Duo MFA option, #158 hundreds-of-ASINs scaling).

---

## Items by status

### Verified - Pending Client UAT (116)

These passed Anuki validation in prior sprints and/or in the 2026-05-01 Ready-for-UAT pass. Awaiting client UAT signoff. Notes column references the evidence including any positive 5/1 client confirmation.

```
1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24,
26, 27, 28, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
48, 49, 51, 52, 53, 54, 55, 56, 57, 61, 62, 63, 64, 66, 67, 68, 69, 70, 71, 72, 73,
74, 75, 76, 77, 79, 81, 82, 83, 85, 87, 88, 89, 90, 92, 93, 94, 95, 97, 98, 99,
100, 101, 102, 110, 111, 112, 113, 114, 115, 116, 117, 119, 120, 121, 129, 133,
134, 135, 137, 139, 140, 142, 144, 145, 146, 147, 152, 153, 154, 161, 163
```

**Of these, 39 + 4 PROD-ONLY = 43 went through the explicit 2026-05-01 ReadyForUAT pass.** The other 87 were closed in earlier sprints (Sprint 1 / early Sprint 2) and have a Date Closed of 4/3, 4/6, or 4/30 — the client previously confirmed fixed.

**Subset that needs prod-data re-test even though code is verified** (already noted in their respective Notes cells):
- **#45** — cert types dropdown completeness (SQL migration `19_20260320_UpdateCertificationTypes.sql` may not have run on prod)
- **#51** — diacritics rendering in real client data
- **#94** — Product Mgmt Subcategory hygiene (dev has empty data; Carly saw populated-but-wrong)
- **#124** — cert type tag list completeness
- **#135 / #136** — timezone date display (BusinessToday helper applied; verify with a specific eff/exp date)
- **#145** — partner cleanup ("test", "Carly", "cARLY tEST" rows)

### Carryover - Open (19) — Anuki Sprint 3 fix

13 of these are post-audit reopens from misclassified Sprint 2 items where the client claimed "still not fixed" or "worse" at 5/1. The remaining 6 (#164–#170 minus one) are fresh defects raised at Sprint 2/3 boundary.

| QC | Page | Why open in Sprint 3 |
|---|---|---|
| #23 | Logged-in cert listing | 4/30: 'Still not fixed on tile, off a day' — public-view fix didn't cover internal tile-view JS |
| #78 | Create cert page | 5/1: new asks — remove Status from Import template, clarify Description column purpose |
| #91 | Product Management | 5/1: BIG regression — products w/ added Type/Subcat not showing in list; re-edit clears Product Name/Type after save |
| #104 | Breadcrumbs | 5/1: 'Administration breadcrumb links to Admin Dashboard page only accessible through breadcrumb' — needs proper nav restructure |
| #107 | Admin - User | 5/1: Multiple — RBAC scoping not limiting data; password field clears; new user PW rejected; email-already-exists on retry |
| #109 | Admin - User | 5/1: 'Test once 108 and 106 done' — blocked behind those |
| #118 | Cert Field Mgmt | 5/1: Visibility flag set to public but cert detail page doesn't render it (Balcan 26-0159 example) |
| #127 | Logged-in cert listing | 5/1: 'login still shows titles (Type, etc.) and Company Numbers' — public fixed, internal not |
| #131 | Public + Logged-in tiles | 5/1: Company name truncation works for Product but not Company; missing ellipsis |
| #141 | Dashboard | 5/1: REGRESSION 'It's worse now where no fields are showing' (image 65) |
| #155 | Dashboard | 5/1: REGRESSION same as #141 — product cat/type/sub aggregate exports lost columns |
| #156 | Dashboard | Tile (3 statuses incl Confidential) vs export (2 statuses) mismatch + DB-side mojibake in cert 26-0158 (Company/Product/Location) |
| #164 | Product Identifiers / Amazon | NEW — auto-flip Product Status to Inactive when cert expires |
| #165 | Cert Creation | NEW — not all cert types in dropdown (similar to #45) |
| #166 | Functionality Confirmation | NEW — additional Pathway dropdown options for Recycled Content |
| #167 | Functionality Confirmation | NEW — updated 25-0246 expiration to past but status still Approved |
| #168 | Public Cert Detail | NEW — error message after default cert removed |
| #169 | Filtering Public vs Login | NEW — Cert Type filter login view scope |
| #170 | Filtering / Product Type | NEW — product type cleanup behavior |

### Needs Manual Verify (11) — Anuki action

Code looks correct on dev but couldn't be exercised cleanly. Re-test on prod after the next deploy.

| QC | Page | Action |
|---|---|---|
| #60 | Create cert page (post-creation) | Create new cert with PDF, navigate to detail view, verify image renders fully + zoom/download icons right-aligned |
| #65 | Analyst cert listing | Pagination/lazy-load count UX |
| #80 | Create cert page | Open cert with existing IDs, click Edit pencil, verify modal opens + fields not grayed |
| #96 | Login / nav | Verify search suggestions don't render on top of top-nav links |
| #123 | Public view | Cert #23-0375 special-char rendering on prod |
| #124 | Public view | 5/1 client question: ordering of Product Opt + RM at top of filter; admin-side sort capability |
| #128 | Public view | LEED filter with real LEED-tagged certs |
| #130 | Public view | Confirm Product Type A-Z removed from sort menu |
| #136 | Public view | Set cert eff/exp = specific date, verify display matches |
| #149 | Public view | L2/L3 Product Category checkboxes filter results |
| #157 | Logged-in cert list | Sort Status / Location / Partner / ASIN / UPC with data variance |

### Carryover - In Progress (7) — Anuki action

Sprint 2 status was Dev / SIT Testing. Work in flight; finish Sprint 3.

| QC | Page | Note |
|---|---|---|
| #50 | Create cert page | Mobile cert creation — low priority but pending |
| #103 | Product Management Template | Template must include all fields from Add New Product popup |
| #105 | Admin - Role | "Manage Workflow" / "Audit Logs" — placeholder permissions in seed; need decision |
| #108 | Admin - User | Template export sample data + RequirePasswordReset header |
| #132 | Public | Not all countries showing under filter (e.g., Germany) |
| #138 | Public tile view mobile | Spacing below external tag |
| #148 | Global - Excel Templates | Standard-field option hints in template headers |

### CR-Required (6) — Client decision

Beyond contracted scope. Need formal CR before development.

| QC | CR scope | Status |
|---|---|---|
| #59 | Select2/Tom-Select autocomplete on Company / Cert-Type / Product Type selects | Awaiting client decision |
| #84 | Rename/merge "Company Contact Email/URL" + "Contact URL" cols on logged-in cert listing | Awaiting client decision |
| #86 | Mouse-drag horizontal scroll (Wrike-style) | Awaiting client decision |
| #106 | Free-text Employer field on user record (separate from Company Access permission scoping) | Awaiting client decision — added in 5/4 audit |
| #143 | Partner Role creation flow (web user vs Partner API key — architectural decision; see DEF-112) | Awaiting client decision |
| #159 | Bulk-replace flow for thousands of Product IDs | Awaiting client decision |

### Working as Designed (4)

| QC | Reason |
|---|---|
| #29 | Pagination 20-per-page is the design |
| #58 | Cert creation timeout — operational, not a code defect |
| #160 | DB listing URLs depend on prod URL config (no behavior change required) |
| #162 | ASIN/UPC export shows configured values per spec |

### Out of Scope (3)

| QC | Reason |
|---|---|
| #122 | "DO NOT EDIT - CORRECT AS IS PER CP" — sort behavior |
| #125 | GPS under "additional" dropdown — CP NOTE: yes |
| #126 | Grid vs list POV — CP NOTE explicit decision |

### Blocked - Client Decision (1)

- **#158** — What happens with hundreds/thousands of ASINs tied to a cert? Need client direction on UI scaling approach.

### Blocked - Configuration (1)

- **#150** — Forgot password flow needs individual logins (SMTP/B2C config).

### Insufficient Info (1)

- **#151** — Duo MFA option ask. Needs client confirmation before scoping.

### Duplicate (1)

- **#25** — duplicate sort options in public view (see related — open text suggests merge with prior fix).

---

## Newly discovered defects (NOT in this QC log)

Items found while reconciling that should be tracked separately in `defect-register.md`, not added to the QC log:

| DEF | Source | Note |
|---|---|---|
| DEF-127 (shipped 5/4) | Anuki internal QA | Import/Export icon polish on Product/Company pages |
| DEF-128 (shipped 5/3) | Anuki internal QA | Public CSV export was AllowAnonymous + accepted excel/pdf format + leaked Internal Tag column |
| DEF-129 (shipped 5/3) | Anuki internal QA | Partner API `/search` was returning raw entity (same class as DEF-103/124) |
| DEF-130 (open) | Anuki internal QA | `/api/v1/products` leaks raw Product entity to Partner API keys |
| DEF-131 (shipped 5/3) | Anuki internal QA | **CRITICAL** — Partner API keys could POST/PUT/DELETE certs via bulk routes (privilege escalation) |
| DEF-132 (shipped 5/3) | Anuki internal QA | DEF-121 stopgap regression caught + fixed same cycle |
| DEF-133 (shipped 5/4) | Anuki internal QA | Mojibake (`â–¶`, `â€¢`) baked into 4 Razor views |

These are Anuki-discovered, not from the client's QC log. They show up in `shipped-2026-05.md` for stakeholder visibility.

---

## Coverage gap report

The 10 Needs Manual Verify items all share a common cause: **dev DB doesn't have the data to exercise the test condition**. This is the highest-leverage process finding from this reconciliation.

Concrete dev-data gaps:
- No LEED-tagged cert (blocks #128)
- No L2/L3 Product Category data variance (blocks #149)
- No cert numbered like the special-char one Carly saw (#23-0375) (blocks #123)
- All Product Mgmt rows are in "Chemicals and Raw Materials" with empty Product Type/Subcategory (blocks #94 hygiene check, #157 sort variance)
- Cert types post-migration not seeded on dev (blocks #45 dropdown completeness)

**Recommended process change:** before each ReadyForUAT pass, generate a small "data variance" seed batch that covers each known QC repro condition. One LEED cert, one cert with diacritics in the company name, one cert with a specific eff/exp date, three Product Mgmt rows in different categories. ~30 minutes of seed work would close 6 of 10 manual-verify items.

---

## Process & tooling findings

### Trailing-comment blind spot (caught and corrected this pass)

The first-pass classifier at 12:41 rolled up qc-readyforuat-2026-05-01.md verdicts as authoritative for the 55 Sprint-2 items, but **didn't read each row's `Defect Description` column for trailing 4/30 / 5/1 client retest comments**. A regex sweep for pushback language ("Still", "still not", "not fixed", "worse", "5/1:") flagged 39 candidates, of which 13 needed real downgrade after hand review (the other 19 had positive 5/1 confirmations like "Working", "Confirmed", "Could not replicate assuming Anuki confirmed fix" or were resolved-with-known-followup).

**The pattern:** the qc-readyforuat doc verified **code correctness** for its Sprint 2 items, but the client's QC log row often contains a same-day or next-day retest comment that contradicts the code-correctness verdict (e.g., "It's worse now", "Still not fixed on logged-in view"). The Anuki test passed on the surface tested but missed an adjacent surface.

**Process change for Sprint 4 onwards:** at end of each Sprint, before declaring an item "Verified - Pending Client UAT," re-read the FULL Defect Description for client comments dated AT OR AFTER the validation pass. If the trailing comment contains negative language, downgrade to Carryover - Open or Needs Manual Verify with the specific comment captured in the Notes column. **The validation prompt template (`qc-log-validation-prompt-gcc.md` Phase 4) already says this — the Sprint 3 first pass didn't follow it.** Add an explicit checklist item to the prompt: "for every Verified verdict, paste the trailing client comment verbatim into the Notes column."

### Workbook structure drift

The QC log workbook has accumulated 8 sheets and 5.7MB. `Images` tab has 92 embedded images. Each sprint adds 2 columns to `GCC DB QC Log2` — already at 17 cols and will keep growing. **Recommendation:** at end of Sprint 4, archive the column for the closed sprint to a separate sheet and keep only the latest 2 sprint columns visible.

### Status vocabulary drift

Sprint 2's "Anuki Status Sprint 2" column had 15 distinct values, several near-duplicates (`none` vs `None`, "Working As Designed " vs "no change, working as designed."). The Sprint 3 5-4 column should converge on the canonical taxonomy in `qc-log-validation-prompt-gcc.md`. Going forward, keep the vocabulary closed to that list.

### Date Closed column has mixed types

The "Date Closed - GCC Confirmed Fixed" column mixes real dates with free-text ("Not fixed", "Did not check", "Discuss", "N/A"). This makes filtering/sorting brittle. Recommend splitting into two columns next sprint: `Date Closed (date only)` and `Closure Note (text)`.

### Fragility of Sprint columns

Two-column-per-sprint pattern is fine short-term but doesn't scale. By Sprint 6 we'll have 12 sprint-related columns. **Recommendation:** at Sprint 5 boundary, rotate to a single "Latest Status" + "Latest Notes" pair plus a separate sheet that archives historical sprint columns.

---

## How to read the output xlsx

- File: `Database QC Log_5.4.26_sw_sprint3.xlsx`, same folder as source
- Sheet: `GCC DB QC Log2`
- Headers on row 5; data on rows 6–175
- Sprint 3 columns: `Sprint 3 5-4` (status, col C) and `Sprint 3 Notes` (rationale + evidence pointer, col D)
- All other columns unchanged
- Embedded images on the `Images` tab preserved (92 verified)
- Original file untouched. Open the new file alongside the source to diff if desired.

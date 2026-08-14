# Migration decisions — answer tracker

**Author:** Anuki Consulting · **Purpose:** single place to record the client's answers to the open
migration decisions as they arrive, so no answer lives only in an email thread.
**Created:** 2026-07-07 · **Last updated:** 2026-07-07

**How to use:** when the client answers an item, fill its **Answer**, **Answer date**, **Answered by**,
and **Resulting action** cells, then flip **Status** to Answered (or Applied once the action is done).
Do not delete rows — a superseded answer stays with a note. Update the *Last updated* date above.

**Source of these items:**
- Items 1–3 = `migration-client-note-2026-06-11.md` (first client note)
- Items 4–7 = `migration-client-note-2026-06-22.md` / `EMAIL-READY-to-send.md` (validation-gate follow-up)
- Full analysis: `migration-remediation-plan-2026-06-22.md`, `migration-import-transform-plan-2026-06-22.md`

**Status legend:** `Open` = no answer on file · `Answered` = client decided, not yet applied ·
`Applied` = decision carried into staging/config/DB · `Superseded` = replaced by a later answer.

---

## Snapshot

| Answered | Open | Total |
|---:|---:|---:|
| 0 | 7 | 7 |

> As of 2026-07-07, **no client answer is recorded for any item.** Items 5 and 7 cannot be closed by
> our recommended defaults — they require the client specifically.

### Re-check against the 2026-08-12 go-live files (done 2026-08-13)

All seven were re-tested against `GCC Database Migration_Certs_8.12.26V2.xlsx` /
`GCC ASIN Master List_8.12.26.xlsm` before re-asking. **Four no longer need a client answer:**

| # | Was | Now | Evidence |
|---|---|---|---|
| 1 | Open | **Still open** — but it's an *Amazon-side* confirm, not a data question | Client uses `greencircle_cef` consistently, 8,562 rows |
| 2 | Open | **Still open** (low risk — default already built) | 96 names >200 chars, longest **951**; field is `nvarchar(1000)`, so it fits |
| 3 | Open | **Overtaken by the client's own 8/12 email** | They asked for the retired types to be *added and shown, hidden from the dropdown* — which **replaces** the June plan to display "Unspecified". Confirm back; don't re-ask |
| 4 | Open | **RESOLVED by the new data** | 25-0437 / 25-0438 are both present (Expired List 2); orphan ASIN cert numbers **51 → 0** |
| 5 | Open (no default) | **Still open, but now HAS a default** | ASIN list: `0.63` ×513 vs `0.23` ×13, **and the cert listing itself says 0.63** → recommend 0.63 |
| 6 | Open | **RESOLVED by the new data** | Both 25-0479 and 25-0482 read `Block Inc.` in *both* files; "Square" appears nowhere. Use the client's spelling (no comma) |
| 7 | Open (Critical) | **Superseded** — restated with current numbers | Now 137 duplicated numbers / 175 surplus rows across all three sheets (was 114, expired only). Folded into the 8/12 note's item 3, Group 1 — do not ask twice |

Net: the 8/12 client note asks only **A (item 1), B (item 2), C (item 5)**, plus a confirm-back on
item 3. Reproduce with
`scratchpad/recheck_june_decisions.py`. **Do not mark anything Answered on the strength of this
table — these are our findings, not the client's words.** Items 4 and 6 are resolved by *data*,
which is different from a decision on file.

---

## Decision log

### 1. Amazon code for "Certified Environmental Facts" — Priority: High
- **Required decision:** Confirm whether `greencircle_cef` is the correct Amazon long-form code for CEF.
- **Our recommendation / default:** Set `greencircle_cef` (inferred from the client's own ASIN export; needs Amazon-side confirm). No default — external string must be confirmed.
- **Tracked as:** CR-021 (data operation on one live reference row `CertificationTypes.Id = 5`).
- **Status:** **Open** — CR-021 = "Proposed 2026-06-11, awaiting GCC approval, nothing run against live."
- **Answer:** _—_  · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** run the guarded UPDATE (`sql-fixes/2026-06-11_cef-amazon-code.sql`) in an approved maintenance step (Gate 7), then verify partner-API output shows `"amazonCode":"greencircle_cef"`.
- **Refs:** `CR-021-cef-amazon-code.md`, change-request-register §CR-021.

### 2. Long product names — Priority: High
- **Required decision:** Approve storing full product names by widening the field (vs. trimming). 88 names exceed 200 chars (longest 951).
- **Our recommendation / default:** Widen, don't trim (product name is passed to Amazon; trimming changes what Amazon sees). No API-shape change.
- **Status:** **Open** (client confirmation of "keep full names" not on file). Engineering note: the widen migration `WidenCertificateProductIdProductName` is **already built, dev-applied, and bundled into the 2026-07-01 deploy** (Shawn approved the migration to ride along) — i.e. engineering has enabled the default ahead of a recorded client "yes."
- **Answer:** _—_ · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** if "keep full" → nothing further (migration already carries it); if "trim/display-only" → change transform to truncate ~200 and reassess the migration.
- **Refs:** remediation plan §3, import plan P3, `shipped-2026-07.md`.

### 3. Retired certification types — Priority: Medium
- **Required decision:** Confirm the ~86 retired-type expired certs may display as **"Unspecified."**
- **Our recommendation / default:** Bring in as historical on the expired records; show "Unspecified"; keep the type deactivated (off the list for new certs; not passed to Amazon).
- **Status:** **Open.** Engineering note: the deactivated "Unspecified" placeholder mechanism is **built and tested on dev**.
- **Answer:** _—_ · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** if confirmed → proceed as built; if the client wants a specific remap → update the cert-type map (avoids "Unspecified").
- **Refs:** 06-11 note item 2, consolidated email item 3.
- **Affected types:** Biodegradable, Biobased, Renewable Resource Content, Verified Health Product Declaration.

### 4. Missing parent certificates / 51 orphan ASINs — Priority: High
- **Required decision:** Provide certs **25-0437** and **25-0438**, or approve dropping/holding the 51 orphan ASINs that reference them (both X-Sense smoke/CO alarm products).
- **Our recommendation / default:** Hold the 51 ASINs aside until the client supplies the two cert records (likely newer than the 4.9 snapshot).
- **Status:** **Open.**
- **Answer:** _—_ · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** if certs supplied → load them, then attach the 51 ASINs; if drop approved → exclude the 51 and log the exclusion.
- **Refs:** 06-11 note item 3, email item 4, `migration-attachments/item4-orphan-asins.csv`.

### 5. Conflicting result for cert 24-1634 — Priority: High
- **Required decision:** Confirm whether the correct certification result is **0.23** or **0.63** (Philips; ASIN list shows both — 0.63 on 2024-dated entries, 0.23 on 2023-dated).
- **Our recommendation / default:** **None — must be answered by the client** (no single listing value to defer to).
- **Status:** **Open (hard blocker for this cert — no default).**
- **Answer:** _—_ · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** set the confirmed value on cert 24-1634's `Cert Results`; `CertificationResultsNumeric` recomputes.
- **Refs:** remediation §4.2, 06-22 note item 2.

### 6. Company canonical name — Priority: High
- **Required decision:** Use **"Block, Inc."** or **"Square"** for certs **25-0479 / 25-0482** (same company, old vs. new name).
- **Our recommendation / default:** Use "Block, Inc." for both (avoids creating two company records).
- **Status:** **Open.**
- **Answer:** _—_ · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** set the confirmed canonical name in the transform config so both certs resolve to one company.
- **Refs:** remediation §4.3, 06-22 note item 3.

### 7. Duplicate expired certificate records — Priority: Critical
- **Required decision:** Approve keeping the most-complete record for each of the **114** duplicate cert numbers and setting the rest aside (nothing deleted without sign-off).
- **Our recommendation / default:** Keep most-complete row per number; hold the others aside. (Exact-duplicate rows already collapsed mechanically; these 114 differ in content.)
- **Status:** **Open (blocks unique-cert# import for the affected expired records).**
- **Answer:** _—_ · **Answer date:** _—_ · **Answered by:** _—_
- **Resulting action (on answer):** if approved → apply keep-most-complete collapse and re-run the validation gate to 0 blockers; if "review first" → send the list and await per-number direction.
- **Refs:** remediation §4.4, `_remediation_output/20260622-170846/residual-issues.csv`, `migration-attachments/item7-duplicate-expired-certs.csv`.

---

## Notes / reconciliation flags

- **Wording inconsistency to resolve before sending the 06-22 note:** its draft opens with *"Thanks again for
  the answers on the first three items,"* which implies items 1–3 were already answered. No such answer is
  recorded, and CR-021 (item 1) still reads "awaiting GCC approval." Either items 1–3 were answered and the
  CR-021 / migration docs are stale (update them), or that line is draft boilerplate and should be removed.
- **No load has run.** The import-transform plan is DRAFT, blocked on these decisions (§8). Dev test-import can
  proceed with defaults parked; **production remains No-Go** until items answered (esp. 5 and 7) + the
  `ProductName` widen migration runs on prod (Gate 7).
- Policy basis: these are migration-mapping decisions on pre-import staging data, not live UPDATE/DELETE, so
  they do not each need a separate CR — **except item 1 (CR-021)**, which touches a live reference row.

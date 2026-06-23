# Migration source-data remediation plan (2026-06-22)

**Author:** Anuki Consulting · **Scope:** customer source workbooks in `TestData/DBSampleLists/`, **pre-import only**
**Driven by:** `Scripts/data-ops/2026-06-22-migration-source-validation.py` (run result: **7 blockers, 5 warnings**)
**Status:** DRAFT — not yet executed. No live records touched.

---

## 1. Principles

- **Operates on migration staging copies, not originals.** All mechanical fixes are applied to a working copy of the source files (or a staging table), never the client's master export. Originals are preserved for audit.
- **Authoritative-source rule (the key tie-break):** the **certificate listing (Active/Expired) is the source of truth for cert-level fields** (company, certification type, dates, results, status). The **ASIN file is authoritative only for product-level fields** (product-id value, product name, SKU). On collapse, cert-level values are taken from the matching cert row and ASIN-row cert-level values are ignored. This resolves most conflicts deterministically; only conflicts with no listing value escalate to the client.
- **CR policy:** these are migration-mapping decisions on pre-import data, **not** live UPDATE/DELETE on business records, so they do not each need a separate GCC CR (per [[feedback_data_changes_require_cr]] and `migration-client-note-2026-06-11.md`). Every disposition is logged here regardless.
- **Definition of done:** validation gate re-runs to **0 blockers**; warnings are either fixed or explicitly accepted with a logged reason; client decisions are recorded.

---

## 2. Already covered by the 2026-06-11 client note

These three were raised to the client in `migration-client-note-2026-06-11.md` with recommended defaults; remediation just applies whatever the client confirms:

| Validation code | Issue | Issued default (awaiting client) |
|---|---|---|
| `EXP-CTYPE` (86) | Retired cert types (Biodegradable, Biobased, Renewable Resource Content, Verified Health Product Declaration) | Load **as-is** on expired records; keep deactivated; not passed to Amazon |
| `ASIN-ORPHAN` (51) | ASINs ref cert `25-0437`/`25-0438` with no parent cert | **Hold out of load** until client supplies the two cert records |
| (CR-021) | Amazon code `greencircle_cef` for Certified Environmental Facts | Confirm external string (no default) |

**Action:** no new client message needed for these — fold the new decisions in §4 into a follow-up on the same thread.

---

## 3. Blockers — disposition

| Code | Count | Type | Disposition |
|---|---|---|---|
| `ASIN-PNAME-LEN` | 88 | **Decision** | `productName` is passed to Amazon, so truncating alters partner data. **Recommend widening** `CertificateProductId.ProductName` from 200 → 1000 chars (model + migration; does not change API shape, so safe per [[feedback_amazon_api_immutable]]). Fallback only if client confirms display-only: truncate at 197 chars + "…". |
| `ASIN-CONFLICT` | 13 | **Mechanical + Decision** | Apply the authoritative-source rule (§1): pull company/certType/dates from the cert listing, discard ASIN-row cert-level values. Resolves the certType typo (`Certified Environmenal Facts` on 23-0812), company rebrand (Block Inc. vs Square), and date conflicts (24-1634). **Escalate only** the `results` conflict on 24-1634 (`0.23` vs `0.63`) — no single listing value to defer to. |
| `EXP-CERT-DUP` | 124 keys / 278 rows | **Mechanical + Decision** | First pass: collapse **exact-duplicate** rows (keep one). Residual rows that share a cert# but differ in content are genuine conflicts → list for client confirmation (which record is correct). |
| `EXP-CERT-BLANK` | 1 | **Decision** | Single Expired row with no cert#. Inspect; client supplies the number or row is dropped. |
| `ACT-CAT` | 5 | **Mechanical** | Clear prefix typos (label is canonical). Re-prefix deterministically: `4 Construction…`→3, `7 Food and Beverage`→6, `9 Packaging`→8, `2 Chemicals…`→1, `4`-dup→3. Low risk; apply in script. |

---

## 4. Open decisions still needed from client (new since 06-11)

Add these to the existing migration-decisions thread:

1. **`productName` length (`ASIN-PNAME-LEN`)** — 88 product names exceed 200 chars (longest 951). *Recommend* widening the field so Amazon receives the full string; confirm the field is partner-facing (not display-only). **OK to store the full name?**
2. **Certification result for cert `24-1634` (`ASIN-CONFLICT`)** — the ASIN list shows two different results (`0.23` and `0.63`) for the same certificate. **Which is correct?**
3. **Company name normalization (`ASIN-CONFLICT`)** — certs `25-0479`/`25-0482` appear as both "Block Inc." and "Square" (same company). **Confirm the canonical company name** so we don't create two company records.
4. **Duplicate expired cert numbers (`EXP-CERT-DUP` residual)** — after removing exact duplicates, any cert# still attached to two differing records needs the client to say which is authoritative (list provided after the mechanical pass).

---

## 5. Warnings — disposition

| Code | Count | Disposition |
|---|---|---|
| `ASIN-DUP` | 2,190 | **Mechanical** — drop exact-duplicate rows (keep first). Safe, deterministic. |
| `ASIN-JUNK` | 14 | **Mechanical** — placeholders (`NOT LAUNCHED`, `SHAVING`, `To be Checked`) are not valid product IDs; **exclude from load**, report excluded rows. |
| `EXP-DATES` | 55 | **Mechanical + accept** — coerce Excel serials (`45536`→date) to real dates; blank dates on already-expired certs are acceptable (log, don't block); inspect the 1 `expiration<effective` row. |
| `EXP-CERT-FMT` | 3 | **Decision (minor)** — `6H3-####` non-standard cert numbers. Cert# is a string field so they import fine; confirm the prefix is a legitimate legacy format, then keep as-is. |
| `ACT-CAT-MISSING` | 85 | **Analyst pass** — blank/`N/A` product category. Backfill via product-type lookup where possible, else SME assignment. Overlaps [[project_cr186_product_type_subcat_blank]]; not a hard blocker for import but needed for the hierarchy. |

---

## 6. Sequence

1. **Issue §4 decisions** to the client on the 06-11 thread; capture answers.
2. **Build remediation script** (`Scripts/data-ops/2026-06-22-migration-source-remediation.py`) operating on staging copies: applies all §3/§5 *mechanical* fixes, emits a cleaned workbook + a change log CSV (every row changed/dropped, with before/after).
3. **Apply confirmed decisions** (field widen, results value, canonical company, residual dup resolution) into the script's config.
4. **Re-run the validation gate** against the cleaned files → iterate until **0 blockers**.
5. **Analyst category backfill** (`ACT-CAT-MISSING`, 85 rows).
6. **Final gate run + sign-off**, then proceed to the actual import (separate plan; respects pre-deploy [[project_predeploy_agent_checklist]] and DB-change gating).

## 7. Ownership

- **Anuki:** remediation script, validation gate, change log, field-widen migration.
- **Client/GCC:** §4 decisions, residual dup resolution, category SME input.
- **Analyst:** category backfill (step 5).

## 8. Risks

- **Widening `ProductName`** touches the model — must run the matching SQL on prod at deploy (prod never auto-migrates; [[project_predeploy_agent_checklist]] Gate 7).
- **Authoritative-source rule** assumes the cert listing is more trustworthy than the ASIN export for cert-level fields. Validated by the 23-0812 typo case (listing is clean), but call out the assumption to the client.
- **Snapshot drift:** the 4.9.26 cert listing and 6.1.26 ASIN list are different vintages — the 2 orphan certs are likely just newer. Confirm both exports are from the same cutover snapshot before the real import.

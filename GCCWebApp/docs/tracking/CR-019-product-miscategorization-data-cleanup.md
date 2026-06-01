# CR-019 — Data-correction pass: product mis-categorization cleanup (DEF-231 fallout)

**Status:** Proposed — awaiting GCC approval
**Date drafted:** 2026-06-01
**Type:** Data operation (one-time UPDATE/DELETE on live records) — *not* a code change or scope change
**Requested approval from:** GCC
**Related:** DEF-231 (the code fix, already done and verified locally), QC #186, QC #170 (test-data removal)
**Environment:** the live/test database behind gccwebapp.com (same instance the client tests against)

---

## 1. Why this needs your approval

Per our standing agreement, any operation that **updates or deletes existing business records** in the live database requires written approval from GCC before it runs — it is not something we action on internal say-so. This request covers a one-time data-correction pass and the test-data removal you asked for. The underlying code defect that *created* the bad data is already fixed (see §2); this CR is purely about cleaning up the rows that were written before that fix.

---

## 2. Background — what went wrong (and what's already fixed)

On the certificate form, using the **"Add New"** option on the Product Name field to create a product on the fly was filing that product under the **wrong category** — the first category in the list — with **no product type or subcategory, ignoring the category/type/subcategory the analyst had selected on the form**. Selecting that newly-created product then copied its (wrong) category back onto the certificate. Net effect, as reported on 5/29 (QC #186): *"I selected Chemicals and it shows [a test category]… Product Type/Subcategory are not appearing… it changed/removed some of my selections."*

**The code is fixed (DEF-231):** products added this way now keep exactly the category, type, and subcategory chosen on the form. Verified end-to-end on localhost. This fix is queued for the next deploy and **stops any new bad data**.

**What this CR addresses:** certificates and products created *before* that fix may still carry the wrong category and be missing their type/subcategory. Those existing rows do not self-correct.

---

## 3. The hard constraint you should know about

**The analyst's originally-intended category was not stored anywhere — the bug overwrote it.** There is no hidden "correct value" we can programmatically restore. That means this cleanup **cannot be a blind automated fix**. It has to be: identify the suspect rows → a human confirms the correct category/type/subcategory for each genuine record → apply only those confirmed values. For most records, the simplest and safest correction is to **re-open the certificate/product in the now-fixed screen and re-save it** with the right values.

---

## 4. What we propose to do

A three-step pass, fully reversible until the final commit:

1. **Identify (read-only).** Run a read-only query that lists: (a) products sitting under the default category with no type/subcategory, (b) the high-confidence subset where the linked certificate shows the same signature, and (c) the client-created test data (the "Carly Test" category/types/products you asked be removed). No data is changed. Safe to run anytime. Produces a review worklist.

2. **Triage (human).** Anuki + GCC review the worklist and split it into:
   - **Test junk to delete** — the "Carly Test" entries and any throwaway test products. (You explicitly requested removal in QC #170.)
   - **Real records to correct** — genuine certificates/products, each tagged with the correct category/type/subcategory by someone who knows the intended value.

3. **Remediate (guarded write).** Run the correction script, which:
   - Deletes the confirmed test data — but **refuses to delete anything still referenced by a real certificate** (it lists those for a manual decision instead).
   - Applies only the human-confirmed corrections to the real records, with built-in integrity checks (it rejects any category/type/subcategory combination that isn't internally consistent), and updates the linked certificates that still show the corrupted signature.
   - Runs inside a single transaction that **rolls back by default** — we run it as a dry run first, confirm the before/after counts match the worklist, and only then re-run with commit enabled.

---

## 5. Safeguards

- **Full database backup taken immediately before any write.** (No file/upload changes are involved, so the uploads folder is not affected.)
- **Dry-run first:** the remediation script defaults to ROLLBACK; we verify the printed row counts against the approved worklist before committing.
- **Integrity guards** in the script block any inconsistent category/type/subcategory assignment and any deletion that would orphan a real certificate.
- **Scripts are committed and reviewable** before execution:
  - `Scripts/data-ops/2026-06-01-product-miscategorization-IDENTIFY.sql` (read-only)
  - `Scripts/data-ops/2026-06-01-product-miscategorization-REMEDIATE.sql` (guarded, ROLLBACK-by-default)

---

## 6. What GCC is approving

Please confirm approval for:
1. Running the read-only identification query on the live database (no risk).
2. Deleting the client-created test data ("Carly Test" category/types and associated test products) once confirmed not tied to any real certificate.
3. Applying a **reviewed, agreed list** of category/type/subcategory corrections to genuine certificate/product records — with GCC sign-off on the actual worklist before the final commit.

We recommend GCC review the worklist (output of step 1) and confirm the correction values before we commit step 3.

---

## 7. Acceptance criteria

- After the pass, the suspect-product count from the IDENTIFY query is **0** (or any residual rows are documented and intentionally left).
- Spot-checked corrected certificates show the **correct category** in the form and detail view, with type/subcategory present.
- Corrected products show their type and subcategory in **Product Management**.
- The "Carly Test" entries no longer appear in the Product Types hierarchy or product dropdowns.
- No real certificate was deleted or orphaned.

---

## 8. Rollback plan

- **Before commit:** the transaction rolls back automatically — nothing persists, just re-run after adjusting.
- **After commit:** restore from the pre-run database backup taken in §5. Because corrections are applied per-row from a reviewed list, a targeted re-correction is also possible without a full restore.

---

## 9. Out of scope

- **The code fix (DEF-231)** — already done; ships on the next deploy regardless of this CR.
- **Changing how product categories are sorted/ordered** — not part of this; the default-category behavior only mattered because the old quick-add ignored the form, which the fix corrects.
- **Re-categorizing records that are already correct** — only bug-affected rows are touched.

---

## 10. Partner API impact

No JSON **contract/shape** change — `/api/v1/certificates*` is untouched per the contract-immutability agreement. Note, however, that for any **partner-visible** certificate whose category is corrected, the *category value* the partner sees becomes the accurate one. This is a data-accuracy improvement, not a contract change, but flagging it for awareness in case any affected cert is live to a partner.

---

## 11. Effort

- Identify + triage: ~1–2 hrs (plus GCC worklist review).
- Remediate + verify: ~1–2 hrs.
- Volume depends on how many pre-fix rows the IDENTIFY query returns on the live data — to be reported with the worklist.

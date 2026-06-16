# CR-021 — Set Amazon code for "Certified Environmental Facts" (`greencircle_cef`)

**Status:** **Proposed 2026-06-11 — awaiting GCC approval.** Nothing run against live.
**Type:** Data operation (one-time UPDATE on one live reference-data row) — *not* a code change or scope change.
**Requested approval from:** GCC (and one confirmation point — see §6).
**Related:** Migration analysis 2026-06-10 (`docs2/.../MIGRATION_ANALYSIS_2026-06-10.md`), lookup tables 2026-06-11, prior code-set `docs/tracking/sql-fixes/2026-04-22_client-data-corrections.sql`.
**Environment:** the live/test database behind gccwebapp.com (the instance the client tests against).
**Script (for review):** `docs/tracking/sql-fixes/2026-06-11_cef-amazon-code.sql`

---

## 1. Why this needs your approval

Per our standing agreement, any operation that **updates an existing record** in the live database requires written GCC approval before it runs. This is a single-row update to a **reference table** (`CertificationTypes`), not a business record, but it changes a **partner-facing value sent to Amazon**, so we are tracking it as a CR rather than actioning it silently.

---

## 2. Background — what we found

While analyzing the client's sample **ASIN list** (`SAMPLE ASIN List for Anuki Testing_6.1.26.xlsm`) for the data migration, we found that **7,904 of its 14,183** product-ID rows carry:

> `amazonCode = greencircle_cef`  (certification type = **Certified Environmental Facts**)

The application database, however, still holds the **seeded short code `'CEF'`** on that certification type (`CertificationTypes.Id = 5`). When the Amazon long-form codes were set on **2026-04-22**, only three types were confirmed and converted — Sustainable Energy Practices, Recycled Content, Certified Energy Savings — and that script explicitly noted the rest were *"awaiting client mapping."* Certified Environmental Facts was never set.

**Net effect:** any **Certified Environmental Facts** certificate passed to Amazon currently sends `CEF`, which is **not** a code Amazon recognizes. Amazon will not apply the badge for those products until the long-form value is set.

This is the single biggest cert-type in the sample after Recycled Content (61 active + 392 expired certs; 7,904 ASIN rows), so it is not an edge case.

---

## 3. The change

One guarded, idempotent UPDATE (full script in §script above):

```sql
UPDATE CertificationTypes
SET    AmazonCode = 'greencircle_cef'
WHERE  Id = 5
  AND  Name = 'Certified Environmental Facts'
  AND  ISNULL(AmazonCode, '') <> 'greencircle_cef';
```

Run **after** a read-only pre-check (`SELECT Id, Name, AmazonCode FROM CertificationTypes WHERE Id = 5`) confirms the row identity and current value. Mirrors the exact pattern used for the three codes set on 2026-04-22.

---

## 4. What does NOT change

- **No schema change, no migration, no code change.** One data value on one reference row.
- **No Partner API contract/shape change** — the `amazonCode` field already exists in the JSON. Only its *value* for Certified Environmental Facts certs changes (`CEF` → `greencircle_cef`), per `feedback_amazon_api_immutable.md` (value, not shape).
- **No certificate, company, or product records touched.**
- The other 11 active cert types that have no Amazon code are **out of scope** — they are not passed to Amazon and the sample confirms only four codes are in use.

---

## 5. Safeguards

- **Read-only pre-check first** — confirm `Id=5` / `Name` / current `AmazonCode` before any write.
- **Guarded WHERE** keyed on both `Id` and `Name`; **idempotent** (`<> 'greencircle_cef'` → no-op if already set).
- **Transaction-wrapped** with `XACT_ABORT ON` and a verification SELECT.
- **Trivial rollback:** `UPDATE … SET AmazonCode = 'CEF' WHERE Id = 5` (the prior value). No backup restore needed for a single reference value, though the routine pre-deploy DB backup still applies.

---

## 6. The one confirmation we need

We are inferring `greencircle_cef` from the **client's own ASIN export** — strong evidence, since it is the exact string the client expects to pass to Amazon. Before we run it, please **confirm `greencircle_cef` is the correct/official Amazon long-form code** for Certified Environmental Facts (the same way SEP/RC/CES were confirmed on 2026-04-22). If Amazon's contract specifies a different string, we use that instead — the rest of the CR is unchanged.

---

## 7. What GCC is approving

1. Running the read-only pre-check on the live database (no risk).
2. Setting `CertificationTypes.Id = 5` (`Certified Environmental Facts`) `AmazonCode = 'greencircle_cef'` on the live database, once §6 is confirmed.

---

## 8. Acceptance criteria

- Pre-check shows `Id=5, Name='Certified Environmental Facts'` before the run.
- After the run, `AmazonCode = 'greencircle_cef'` on Id 5.
- A Certified Environmental Facts certificate's partner-API output shows `"amazonCode": "greencircle_cef"`.
- No other `CertificationTypes` row changed.

---

## 9. Sequencing (Gate 7)

Prod does not auto-migrate. This is a **direct data UPDATE** and must be run **on prod in the same deploy/maintenance step** it is approved for (per the pre-deploy checklist Gate 7), not left to a migration. It is independent of any code deploy and can run on its own.

---

## 10. Effort

~15 minutes (pre-check + UPDATE + verify). No build, no deploy required.

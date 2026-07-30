# GCC Migration — Lookup Tables

**Date:** 2026-06-11 · **Anuki Consulting** · Reference only — no migration scripts run.

Two transform lookups for the data migration, derived from **codebase source-of-truth**, not guessed:
- Status → 8-state workflow → `lookup_status_to_state.csv`
- Certification type → Amazon code → `lookup_certtype_to_amazoncode.csv`

**Sources of truth:**
- States: `Models/CertificateStatus.cs` (enum + `IsPubliclyVisible`/DEF-223).
- Amazon codes: `Scripts/UpdateCertificationTypes.sql` (type roster, 2026-04-13) + `docs/tracking/sql-fixes/2026-04-22_client-data-corrections.sql` (confirmed codes) + the client ASIN sample (`amazonCode` column).

---

## 1. Status → 8-State

The client's `STATUS` strings **already match the system display names exactly**, so this is a clean 1:1 map — no fuzzy matching needed. Only 3 source values appear in the sample; the other 5 canonical states are listed for completeness.

| Source status (client) | → Target state | Enum | Public? | Sample rows |
|---|---|---|---|---|
| `Client Approved - Paid` | Client Approved - Paid | `ClientApprovedPaid` (3) | **Yes** | 920 |
| `Client Approved - Confidential` | Client Approved - Confidential | `ClientApprovedConfidential` (2) | **No** | 39 |
| `Expired` | Expired | `Expired` (5) | No | 4,374 |
| *(not in sample)* `Draft` | Draft | `Draft` (0) | No | — |
| *(not in sample)* `Client Approved - Not Paid` | Client Approved - Not Paid | `ClientApprovedNotPaid` (1) | No | — |
| *(not in sample)* `Hold` | Hold | `Hold` (4) | No | — |
| *(not in sample)* `Republished` | Republished | `Republished` (6) | **Yes** | — |
| *(not in sample)* `Expired - No Annual Verification` | Expired - No Annual Verification | `ExpiredNoAnnualVerification` (7) | No | — |

**Critical rule:** only `Client Approved - Paid` and `Republished` are public (and count as "Active" on dashboard/API per DEF-223). The **39 Confidential certs must load but stay non-public** — they are paid-but-embargoed.

**Transform notes:**
- Trim whitespace and normalize the hyphen spacing (`Approved - Paid`) before matching — exports sometimes vary the spaces around `-`.
- Any source status not in this table = **reject to an exception queue**, do not default-load.

---

## 2. Certification Type → Amazon Code

Only certificate types Amazon recognizes carry an `amazonCode`; all others load normally but are **not passed to Amazon** (per the client mapping sheet: "only those marked Amazon will be passed"). The ASIN sample uses exactly **4** codes — and they are the only 4 types that need a code.

| Source cert type | Amazon code | Pass to Amazon? | Status |
|---|---|---|---|
| Recycled Content | `greencircle_recycled_content` | **Yes** | confirmed in DB |
| Certified Energy Savings | `greencircle_energy_savings` | **Yes** | confirmed in DB |
| Sustainable Energy Practices | `greencircle_sustainable_energy_practices` | **Yes** | confirmed in DB (Pathway applies) |
| **Certified Environmental Facts** | `greencircle_cef` | **Yes** | ⚠️ **live in client data, NOT yet set in DB** |
| All 11 other active types* | *(none)* | No | not passed |

\* Material Ingredient Reporting, Certified Sustainability Facts, Waste Diversion from Landfill, LCA Optimized, GreenCircle Audited, Carbon Footprint Reduction, Closed Loop Product, Recyclable Material, Certified Water Savings, Sustainable Manufacturing Practices, Dematerialization.

### ⚠️ Action item — `greencircle_cef` not in the database
The DB only had SEP/RC/CES codes set (SQL 2026-04-22, which explicitly noted "Other 15 ... awaiting client mapping"). The client's **live ASIN export uses `greencircle_cef` on 7,904 rows** (Certified Environmental Facts). Before the Amazon feed will badge those products, the DB needs:

> `Certified Environmental Facts` → `AmazonCode = 'greencircle_cef'`

This is a **client-confirmation + a one-line data update** (formal CR, not part of this analysis). Until set, CEF product IDs will migrate but won't be passed to Amazon.

### Retired / unknown types (Expired sheet only)
| Source type | Disposition | Action |
|---|---|---|
| `Biobased` (17 rows) | Deactivated in DB (2026-04-13) | Load as historical, or remap — client decision |
| `Renewable Resource Content` (8) | Deactivated in DB (2026-04-13) | Load as historical, or remap — client decision |
| `Biodegradable` (58) | Not in current vocab | **Client decision** — remap or load historical |
| `Verified Health Product Declaration` (3) | Not in current vocab | **Client decision** — remap or load historical |

None of these carry an Amazon code regardless of disposition. They appear only on expired certs.

---

## Coverage check against the sample
- **Status:** 100% of the 5,333 cert-listing rows (959 Active + 4,374 Expired) map via the 3 in-sample source values. 0 unmapped.
- **Cert type (Amazon):** 100% of the 14,183 ASIN rows carry one of the 4 mapped codes (cef 7,904 / rc 3,545 / energy_savings 2,208 / sep 526). 0 unmapped.
- **Cert type (all):** 86 Expired rows use 4 retired/unknown types pending a client disposition decision (does not block the Amazon feed).

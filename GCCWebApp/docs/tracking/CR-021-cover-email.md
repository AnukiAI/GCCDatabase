# CR-021 — Confirmation Email

> **⚠️ Superseded for sending by the bundle.** The CR-021 confirm is now item 1 of `migration-client-note-2026-06-11.md` (bundled with the two open migration decisions). Send the bundle, not this standalone — kept here for the CR's own paper trail.

## Internal context (not sent — for our records)

**Purpose.** Short confirmation request that accompanies CR-021 (`CR-021-cef-amazon-code.md`). Unlike CR-019, this is not a "please approve a cleanup" note — it asks the client to **confirm one fact** (the Amazon long-form code string for Certified Environmental Facts) before we run a one-line reference-data update.

**Why we're asking rather than just setting it.** We can see `greencircle_cef` in the client's own ASIN export (7,904 of 14,183 rows), which is strong evidence — but the three codes we set on 2026-04-22 (SEP/RC/CES) were each confirmed against Amazon's contract first. We hold CEF to the same bar so we don't push an unrecognized string to a partner. If Amazon's spec says something different, we use that; the CR is otherwise unchanged.

**Key facts the email is built on (full detail in CR-021):**
- The DB holds the seeded **short code `'CEF'`** on the Certified Environmental Facts type (`CertificationTypes.Id = 5`); only SEP/RC/CES were converted to long-form on 2026-04-22 ("others awaiting client mapping").
- Effect: CEF certs passed to Amazon currently send `CEF`, which **Amazon does not recognize** → no badge until corrected.
- Fix is a single guarded UPDATE — `Scripts/sql-fixes/2026-06-11_cef-amazon-code.sql` — ~15 min, no code/deploy.
- Policy basis: [[feedback_data_changes_require_cr]] (live record change needs sign-off). Partner API unchanged in shape, only the value ([[feedback_amazon_api_immutable]]).

**Placeholders to fill before sending:** `[name]` (GCC recipient). Sender filled as Shawn. Keep it short — it's a yes/confirm, not a decision.

**Note:** Keep internal identifiers (Id 5, table names) out of the client email — they don't need them to confirm the code string.

---

## Client-facing email (this is what gets sent)

**Subject:** Quick confirm — Amazon code for "Certified Environmental Facts"

Hi [name],

While prepping the certificate data for migration, I noticed one thing I want to confirm with you before we set it.

In the ASIN list you shared, the **Certified Environmental Facts** certificates use this code for Amazon:

> **`greencircle_cef`**

Our system currently has an older placeholder code for that certificate type, so right now those products wouldn't badge correctly on Amazon. I'd like to update it to match — but since this is the value that actually gets sent to Amazon, I wanted your confirmation first (same as we did for the Recycled Content, Energy Savings, and Sustainable Energy Practices codes earlier).

**Can you confirm `greencircle_cef` is the correct code Amazon expects for Certified Environmental Facts?**

If yes, it's a quick one-line update on our side and those certificates will badge properly. If Amazon's documentation lists a different string, just send that and we'll use it instead.

I've logged this as CR-021 for the record — nothing else you need to do beyond the confirm.

Thanks!
Shawn

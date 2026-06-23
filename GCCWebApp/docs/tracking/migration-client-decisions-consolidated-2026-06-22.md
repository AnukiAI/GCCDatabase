# Migration — consolidated client decisions email (2026-06-22)

## Internal context (not sent — for our records)

**Purpose.** One message covering every open decision the client still needs to make before we
finalize the migration mapping. Consolidates the two prior notes so the client isn't chasing
threads:
- Items 1–3 were first raised in `migration-client-note-2026-06-11.md`. **If any were already
  answered, delete them before sending.**
- Items 4–7 are the new ones surfaced by the validation gate (`migration-remediation-plan-2026-06-22.md` §4).

**Not in this email (handled on our side, no client input needed):** the Pathway and Internal-Tag
import fields — both were importer code gaps; now implemented and tested on localhost.

**Defaults.** Every item except 3 and 5 carries a recommended default, so the client can reply
"go with your recommendations" to unblock the batch in one pass.

**Policy basis:** migration-mapping decisions on pre-import staging data, not live UPDATE/DELETE,
so no separate CR each (per [[feedback_data_changes_require_cr]]). Item 4 may carry a small field-
width change (pre-deploy migration gate, [[project_predeploy_agent_checklist]] Gate 7).

**Placeholders:** `[name]` (client recipient). Sender = Shawn. Cert numbers are the client's own
data — fine to keep. Attach the duplicate-cert list (item 7).

---

## Client-facing email (this is what gets sent)

**Subject:** Certificate data migration — the remaining decisions we need from you

Hi [name],

We're close on prepping your certificate data for migration. A handful of items need your call
before we load everything. I've put my recommendation next to each — if you're happy with them
all, just reply **"go with your recommendations"** and we'll run with it. The first two affect the
Amazon badge, so those are the priority if you're short on time.

**1. Amazon code for "Certified Environmental Facts"**
Your ASIN list uses the code **`greencircle_cef`** for these certificates; our system has an older
placeholder, so right now those products wouldn't badge correctly on Amazon.
→ *Can you confirm `greencircle_cef` is the code Amazon expects?* (Same confirmation we did for
Recycled Content, Energy Savings, and Sustainable Energy Practices.)

**2. A few very long product names**
About 88 of the product names in the ASIN list are longer than our current limit (the longest is
~950 characters). Since the product name is passed to Amazon, I don't want to trim them and change
what Amazon sees.
→ *My recommendation:* widen the field to keep the full names as you provided them. **OK to store
the full names?** (If they're only for internal display and a shorter version is fine, say so and
we'll trim instead.)

**3. A few older certificate types**
Some expired certificates use types that aren't in the current system: **Biodegradable, Biobased,
Renewable Resource Content,** and **Verified Health Product Declaration** (about 86 certificates,
all expired).
→ *My recommendation:* bring them in **as-is on those expired records** so the history stays
accurate, keep them off the list of options for new certificates, and don't send them to Amazon.
**OK to keep them as historical?**

**4. Two ASINs without a matching certificate**
In the ASIN list, certificates **25-0437** and **25-0438** are referenced, but neither appears in
the Active or Expired certificate listings you sent — so there's nothing for those ASINs to attach
to.
→ *My recommendation:* hold those two ASINs aside until you can send the two certificate records
(they may just be newer than the 4/9 export). **Can you send those two, or leave them out?**

**5. Two different results on one certificate**
Certificate **24-1634** shows two different certification results in the ASIN list — **0.23** and
**0.63**. We can only carry one.
→ *Which is the correct result for this certificate?*

**6. "Block Inc." vs "Square"**
Certificates **25-0479** and **25-0482** are listed under both "Block Inc." and "Square" — the same
company under its old and new names. Loading both as-is would create two separate company records.
→ *My recommendation:* use the current name **"Block, Inc."** for both. **Good with that, or would
you prefer "Square"?**

**7. Some older certificates share a certificate number**
On the expired list, 114 certificate numbers appear on more than one record (with slightly
different details). These are all expired/historical, and our new system needs each number to be
unique.
→ *My recommendation:* for each, we keep the most complete record and set the duplicate aside —
nothing is deleted without your sign-off. I've attached the full list so you can spot-check.
**OK to proceed that way, or would you rather review them first?**

Thanks!
Shawn

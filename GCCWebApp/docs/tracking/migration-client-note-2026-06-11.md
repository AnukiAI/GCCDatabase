# Migration prep — client decisions note (2026-06-11)

## Internal context (not sent — for our records)

**Purpose.** Single client-facing note that bundles the three items needing a client answer before we can finalize the data-migration mapping. Replaces sending the standalone CR-021 email separately — folds that confirm in as item 1 so the client gets one message, not three.

**Bundled items:**
1. **CR-021** — confirm the Amazon long-form code `greencircle_cef` for Certified Environmental Facts. Detail: `CR-021-cef-amazon-code.md`. Blocks the Amazon badge for CEF certs.
2. **Retired/unknown cert types** — `Biodegradable` (58 rows), `Biobased` (17), `Renewable Resource Content` (8), `Verified Health Product Declaration` (3). All appear **only on expired certs** in the sample. `Biobased` + `Renewable Resource Content` are already deactivated in the system (2026-04-13); `Biodegradable` and `Verified Health Product Declaration` aren't in the current vocab at all.
3. **Orphan ASIN certs** — `25-0437`, `25-0438` appear in the ASIN list but in **neither** the Active nor Expired certificate listing → no parent cert to attach those product IDs to.

**Each item carries a recommended default** so the client can reply "go with your recommendations" and unblock everything in one pass. Defaults:
1. (no default — must confirm the external Amazon string)
2. **Load as historical on the expired certs as-is; keep deactivated so they're not options for new certs; not passed to Amazon.** Lowest-risk — preserves history, doesn't rewrite past records. Alternative is remap to a current type (rewrites history — not recommended).
3. **Hold those two ASINs out of the load** until the client supplies the two cert records (likely newer than the 4.9.26 listing snapshot). Avoids creating product IDs with no parent.

**Policy basis:** CR-021 = [[feedback_data_changes_require_cr]]; partner value not shape per [[feedback_amazon_api_immutable]]. Items 2–3 are migration-mapping decisions, not live-data changes.

**Placeholders:** `[name]` (GCC recipient). Sender = Shawn. Cert numbers and type names are the client's own data — fine to keep in the email; internal table/Id references are kept out.

---

## Client-facing email (this is what gets sent)

**Subject:** Three quick decisions on the certificate data migration

Hi [name],

As we prep your certificate data for migration, three things came up that need your call. I've put my recommendation next to each, so if you're happy with all three you can just reply *"go with your recommendations"* and we'll run with it.

**1. Amazon code for "Certified Environmental Facts"**
Your ASIN list uses the code **`greencircle_cef`** for these certificates. Our system currently has an older placeholder, so right now those products wouldn't badge correctly on Amazon. I'd like to set it to match.
→ *Can you confirm `greencircle_cef` is the code Amazon expects?* (Same confirmation we did for the Recycled Content, Energy Savings, and Sustainable Energy Practices codes.) If Amazon's docs list a different string, just send that.

**2. A few older certificate types**
Some expired certificates use types that aren't in the current system: **Biodegradable**, **Biobased**, **Renewable Resource Content**, and **Verified Health Product Declaration** (about 86 certificates, all expired).
→ *My recommendation:* bring them in **as-is on those expired records** so the history stays accurate, but keep them off the list of options for new certificates (and they won't be sent to Amazon). The alternative is to relabel them as a current type, which I'd avoid since it changes what those past certificates actually said. **OK to keep them as historical?**

**3. Two ASINs without a matching certificate**
In the ASIN list, certificates **25-0437** and **25-0438** are referenced, but neither appears in the Active or Expired certificate listings you sent — so there's nothing for those ASINs to attach to.
→ *My recommendation:* hold those two ASINs aside for now and load them once you can send the two certificate records (they may just be newer than the 4/9 export). **Can you send those two, or should we leave them out?**

None of this is urgent-urgent, but item 1 is the one that affects the Amazon badge, so that's the priority if you can only get to one.

Thanks!
Shawn

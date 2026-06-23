# Migration prep — client decisions follow-up (2026-06-22)

## Internal context (not sent — for our records)

**Purpose.** Second client-facing note on the data-migration thread. The 06-11 note
(`migration-client-note-2026-06-11.md`) covered the first three items (CR-021 CEF code,
retired cert types, orphan ASINs 25-0437/25-0438). Running the validation gate over the
cleaned source files surfaced four more items that need a client call before the import
mapping can be finalized. Source: `migration-remediation-plan-2026-06-22.md` §4.

**Bundled items (all carry a recommended default so the client can reply "go with your recommendations"):**
1. **Long product names (`ASIN-PNAME-LEN`)** — 88 ASIN product names exceed the current
   200-character store limit (longest 951). Because product name is passed to Amazon,
   truncating would change what Amazon receives. *Default:* widen the field to keep the full
   name (no API-shape change, safe per [[feedback_amazon_api_immutable]]). Fallback only if
   client says display-only: truncate at ~200.
2. **Conflicting result for cert 24-1634 (`ASIN-CONFLICT`)** — the ASIN list shows two
   different certification results (0.23 and 0.63) for the same cert. *No default — must
   confirm which is correct.*
3. **Company name "Block Inc." vs "Square" (`ASIN-CONFLICT`)** — certs 25-0479 / 25-0482 list
   both names for what is the same company (Square rebranded to Block, Inc. in 2021). *Default:*
   use the current legal name "Block, Inc." for both so we don't create two company records.
4. **Duplicate expired certificate numbers (`EXP-CERT-DUP` residual)** — after removing exact
   duplicates, 114 certificate numbers still appear on two or more differing expired-record
   rows. All are expired/historical. *Default:* keep the most complete row per number, set the
   others aside (full list attached as `residual-issues.csv`), nothing deleted without their say.

**Policy basis:** migration-mapping decisions on pre-import staging data, not live UPDATE/DELETE,
so no separate CR each (consistent with the 06-11 note and [[feedback_data_changes_require_cr]]).
Item 1 is the only one that may carry a small schema change (field width) — flagged for the
pre-deploy migration gate ([[project_predeploy_agent_checklist]] Gate 7).

**Placeholders:** `[name]` (client recipient). Sender = Shawn. Cert numbers are the client's own
data — fine to keep. Attach `residual-issues.csv` (filtered to the EXP-CERT-DUP-RESIDUAL rows).

---

## Client-facing email (this is what gets sent)

**Subject:** Re: certificate data migration — four more quick decisions

Hi [name],

Thanks again for the answers on the first three items. As we cleaned up the certificate and
ASIN files, four more things came up that need your call. Same as last time — I've put my
recommendation next to each, so if you're happy with all of them you can just reply
*"go with your recommendations"* and we'll run with it.

**1. A few very long product names**
About 88 of the product names in the ASIN list are longer than our current limit (the longest
is ~950 characters). Since the product name is one of the fields we pass to Amazon, I don't want
to cut them off and change what Amazon sees.
→ *My recommendation:* widen the field so we keep the full product name as you provided it. **OK
to store the full names?** (If these are only meant for internal display and a shorter version is
fine, let me know and we'll trim instead.)

**2. Two different results on one certificate**
Certificate **24-1634** shows two different certification results in the ASIN list — **0.23** and
**0.63**. We can only carry one.
→ *Which is the correct result for this certificate?*

**3. "Block Inc." vs "Square"**
Certificates **25-0479** and **25-0482** are listed under both "Block Inc." and "Square" — same
company, just the old and new names. If we load both as-is we'd end up with two separate company
records.
→ *My recommendation:* use the current name **"Block, Inc."** for both. **Good with that, or would
you prefer "Square"?**

**4. Some older certificates share a certificate number**
On the expired list, 114 certificate numbers show up on more than one record (with slightly
different details). These are all expired/historical. Our new system needs each certificate number
to be unique.
→ *My recommendation:* for each, we keep the most complete record and set the duplicate aside —
nothing gets deleted without your sign-off. I've attached the full list so you can spot-check.
**OK to proceed that way, or do you want to review them first?**

As before, none of this is urgent-urgent, but item 1 is the one tied to the Amazon product names,
so that's the priority if you can only get to one.

Thanks!
Shawn

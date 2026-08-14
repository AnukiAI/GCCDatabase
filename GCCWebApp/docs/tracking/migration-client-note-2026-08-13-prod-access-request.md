# Client Note — Production Access Request (PDF Pull)

**Date:** 2026-08-13
**Purpose:** Request Admin/Analyst read access to the client's current (old) certificate
application so the migration PDF-attach step can pull documents for certificates that are
not publicly consented.

**Status:** PARKED — **not sent.** Shawn confirmed 2026-08-13 that she is logged in to the
client's system, so the access ask is moot for now. Draft kept on file in case the session
turns out to be the wrong role (can't see non-consented certificates), expires, or the client
later needs the request in writing for their own records.

**Basis for the numbers:**
- `docs/tracking/migration-import-transform-plan-2026-06-22.md` §11 (source profile), §10 (PDF link step)
- Public mirror manifest: `C:\Users\swest\GCCPublicMirror\manifest.csv` (1,021 records / 1,021 PDFs)
- Client workbook `Plan` tab, row 13 — records Anuki's 4/15 commitment to script the old→new pull

**Open item flagged in the note:** whether a PDF sourced from the client's *public* database is
acceptable as the stored document of record, or whether every certificate must carry the client's
own copy. If the latter, the 1,021 already held are a test corpus only.

---

## Email draft

**Subject:** Access request — pulling certificate PDFs from the current GreenCircle database

Hi [Name],

As we get ready for the 8/18 load, I need to request access to the current certificate database
so we can pull the certificate PDFs across to the new system.

**What I'm asking for:** a named **Admin or Analyst** account on the production certificate
application. Read access is all we need — we won't create, edit, or delete anything. I checked
the hosting side and we have no credentials there either, so this has to come from your team.

**Why we need it:** the public product database only exposes certificates where the customer has
consented to publish. We've pulled everything available there — 1,021 certificates with PDFs —
but measured against the load set you sent on 8/12, that only covers:

- **954 of 1,014** certificates on the Active List (94%)
- **3 of 344** on Expired List 2
- **0 of 4,198** on Expired List 1

That leaves **4,599 certificates with no document attached** after the migration. An analyst- or
admin-level login is the only way to reach the rest — the public API returns "not found" for
anything not publicly consented, which is correct behavior and not something we'd try to work
around.

This is the step referenced on row 13 of your migration plan — *"Anuki confirmed 4/15 they can
run a script to pull and map these from old DB to new DB."* The script is designed and ready to
build; it just needs credentials to run against.

**If issuing an application account is a problem,** either of these works just as well and may be
simpler on your side:

1. A database export plus the certificate file store (the S3 bucket the downloads redirect to), or
2. A bulk export of the certificate PDFs with the certificate number in the filename.

**Timing:** to keep the 9/14 launch date, I'd like access in hand **by 8/18** so the PDF attach
step can run right after the main data load and be complete before the Amazon review on 8/28. The
account can be disabled the moment the pull finishes, and I'm happy to have it time-limited or
IP-restricted if that helps.

One related question while we're here: is a PDF downloaded from your **public** database acceptable
as the stored document of record in the new system, or should every certificate carry your own file
copy? If it's the latter, the 1,021 we already have are a test corpus only and the credentialed
pull becomes the whole job rather than the remainder.

Thanks —
Shawn West
Anuki Consulting

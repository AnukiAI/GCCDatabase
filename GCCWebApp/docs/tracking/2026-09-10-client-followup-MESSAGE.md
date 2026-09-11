# Paste-ready follow-up message — Amazon payload sent 2026-09-10

**Send from Shawn's work address, as a reply on the existing thread.** Attach
`Scripts/data-ops/_amazon_payload/GCC-Amazon-Payload-2026-09-10-CORRECTED.zip` (1.0 MB, the full
corrected bundle — swap for the individual file if that is what went the first time).

---

**Subject:** RE: DB Feedback — corrected Amazon export (one column)

---

Hi Carly,

Quick correction on the Amazon export I sent earlier — and it's good news rather than bad.

That file was generated at 2:43pm, and the `greencircle_cef` change went in at 4:06pm. So the export
you have still shows the old `CEF` value in the certification code column. The export is a snapshot,
and I took it before the change rather than after.

**Everything you asked to review in it is correct.** I've compared it against a fresh export field by
field: the same certificates, the same product identifiers, and every value matching except that one
column. Your changes from today are all accurately reflected — the 11 certificates that came out of
the feed, and the 7 that moved from Carbon Footprint Reduction to Certified Environmental Facts.

**Nothing needs to happen with Amazon.** They don't receive this file — they call our API directly,
and that has been returning `greencircle_cef` since 4:06pm. The live feed is correct; only my
snapshot was taken too early.

I've attached the corrected export. If you'd already started reviewing the first one, there's no need
to start over — the only difference is that the certification code now reads `greencircle_cef`
instead of `CEF` on the 78 Certified Environmental Facts certificates.

Apologies for the extra email.

Thanks,
Shawn

---

## If you'd rather send two lines

> Hi Carly — the Amazon export I sent earlier was generated at 2:43pm, just before the
> `greencircle_cef` change went in at 4:06pm, so it still shows the old `CEF` code. Everything else in
> it is correct and your changes from today are all reflected properly. Nothing to do on Amazon's side
> — they pull from the API directly and that's been correct since 4:06pm. Corrected export attached.

---

## Facts behind the message, if she asks

| Claim | Basis |
|---|---|
| Generated 2:43pm, change at 4:06pm | File mtimes: payload `14:43:14`, CR-021 applied `16:06` |
| Only the code column differs | Column-aware comparison of both exports: certificate set identical (212); `amazonCode` the only differing field; product identifiers 19,147 rows, same keys and order, only that column differs; both reconciliation CSVs byte-identical |
| 78 certificates affected | 78 certificates, 78 GS1 documents, 7,482 of 19,147 identifier rows |
| Amazon unaffected | Amazon calls `/api/v1/certificates` with their key; the DB value changed at 16:06 and the fresh pull at 16:46 returns `greencircle_cef` on all 78 |
| Their edits correctly reflected | 9/9 → today: 11 dropped (6 WDL, 4 CLP, 1 SMP), 0 added, 7 re-typed CFR → CEF; tagged 295→285, Draft 31→32 accounts for all 11 |

**Deliberately not said:** that other certification types still hold short codes internally. It is
latent (nothing reaches Amazon today), it is already raised in the QC #264 cell, and putting it in an
apology email muddies a simple correction.

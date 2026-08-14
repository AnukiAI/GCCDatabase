# SEND-READY — go-live data received, receipt + decisions (2026-08-13)

> **Before sending:** replace `[name]`. Sender: Shawn.
>
> **Attach (both in this folder):**
> - `2026-08-12-duplicate-cert-numbers.xlsx`
> - `2026-08-13-certs-missing-from-load-set.csv`
>
> **Source of truth:** `../migration-client-note-2026-08-12.md` (this file is a send-ready snapshot
> of its client-facing half — if you edit one, edit both, and prefer editing the source).
> Full analysis: `../migration-import-transform-plan-2026-06-22.md` §11–§15.
> Decision status: `../migration-decisions-answer-tracker.md`.

---

**To:** [name]
**Subject:** Re: GCC database migration — files received, and one item that blocks the upload
**Attachments:** 2026-08-12-duplicate-cert-numbers.xlsx, 2026-08-13-certs-missing-from-load-set.csv

---

Hi [name],

Confirming receipt of both files — the certification list (`GCC Database Migration_Certs_8.12.26V2`)
and the active ASIN list (`GCC ASIN Master List_8.12.26`). We've profiled both. Answers to your
three questions first, then the one item that stops us starting.

**1. Product Category / Type / Subcategory numeric prefixes — yes, handled.**
We'll strip the grouping numbers, so "4.3 Cleaning Supplies" loads as "Cleaning Supplies".
"N/A" will be carried through and shown as you asked, in the 116 rows where you've set it.

Where Product Type or Subcategory is blank we'll leave it empty rather than filling anything in —
the certificate page shows a dash for those, and you can populate them in the system once the load
is done. That affects 100 rows for Product Type and 942 for Subcategory on the active list.

**2. Retired certification types — yes, that works.**
Biobased, Biodegradable, Renewable Resource Content and Verified Health Product Declaration will be
loaded and will display correctly on the historical certificates (83 rows in total), while being
hidden from the dropdown so no one can pick them for new work. The system already supports exactly
this.

**3. Multiple listings sharing one certificate number — this one we can't do as-is, and it's
blocking the upload.**
The new database enforces one record per certificate number, and each certificate carries one
product. The pattern in these files is one certificate number covering **several products** — for
example 26-0182 appears four times for four different Regupol products. It affects **175 rows**
across 137 certificate numbers: 12 rows on the active list, 12 on Expired List 2, and 151 on
Expired List 1.

Looking at them closely, they aren't all the same situation — the full list is attached
(`2026-08-12-duplicate-cert-numbers.xlsx`, one tab per group, with the row number in your own
workbook so you can find each one) — and they fall into three groups needing different calls:

**Group 1 — 119 numbers on Expired List 1 (268 rows). We can clean these.**
These are mostly the same product restated rather than different products: 159 of the rows are Sika
Canada Inc. / Sika Corporation, where a product is listed under both entities and again as a French
"(FR)" variant. We'd keep the most complete record per number and set the rest aside for your review
— nothing deleted. This is the group we raised with you on 22 June. **No action needed if you're
happy with that approach.**

**Group 2 — 16 numbers covering genuinely different products. This is the real decision.**
Eight on the active list (26-0166, 26-0168, 26-0169, 26-0170, 26-0174, 26-0175, 26-0182, 26-0184)
and their prior-year equivalents on Expired List 2 (25-0077, 25-0079, 25-0080, 25-0081, 25-0085,
25-0086, 25-0094, 25-0096) — all Regupol America. Certificate 26-0182, for instance, covers four
distinct products: REGUPOL sonus core, REGUPOL 6510S, Impacta ProBase and Thunder Flat.

Here collapsing to one record would genuinely drop products, so we'd rather you choose:
- **Load one product per certificate** — you tell us which product to keep for each of the 16, and
  we'll list the others separately so nothing is lost from your records, or
- **Change the system to support several products under one certificate number.** This touches the
  Amazon API integration, so we wouldn't recommend attempting it in go-live week — but we can scope
  it as a change request for straight after launch.

**Group 3 — 2 numbers that look like errors in the source.** Each has one certificate number
against two unrelated companies:
- **21-0179** — Deceuninck North America (PVC Window Lineals 261, Recycled Content) and DuPont™
  Performance Building Solutions (Great Stuff Pro Gasket US, Material Ingredient Reporting)
- **21-0180** — Deceuninck North America (PVC Window Lineals 202, Recycled Content) and 4ocean, PBC
  (iPhone 6–11, Recycled Content)

We can't pick between these — could you confirm which record is correct for each number?

**One more thing we found, and it's the one we'd most like you to look at.**
Cross-checking the lists against your Full List tab, **51 certificates with the status "Client
Approved - Paid" are not in any of the three lists we've been asked to load** — and we've confirmed
**50 of them are live on your public database right now**. They're mostly ASSA ABLOY group brands:
Corbin Russwin (14), SARGENT (11), Malarkey Roofing (11), Pioneer Industries (10), plus McKinney,
ACCENTRA and Greenfield Natural Meat. A further 26 expired certificates are missing on the same
basis. Full list attached as `2026-08-13-certs-missing-from-load-set.csv`, with a column showing
which are publicly visible today.

If we load exactly what was sent, those 51 live certificates simply wouldn't exist in the new system
at launch — and because they're on your public site today, that's the kind of gap a customer or an
auditor notices straight away. We suspect it's a filter or a date cut-off in how the lists were
pulled rather than anything deliberate. **Could you confirm whether they should be included?** If so,
the 17 August pull is the natural place to pick them up.

**On certificate PDFs.** We can supply PDFs for **954 of the 1,014 active certificates (94%)** from
the public GreenCircle database, and we've built the mirror already. Expired certificates are the
problem: the public database doesn't publish them, so **0 of the 4,198** on Expired List 1 and only
3 of the 344 on Expired List 2 are available to us.

That means **4,599 certificates can't have their documents attached until we have the credentials
you mentioned** — this is now the long pole for the PDF side, and the script needs building once we
know what the login gives access to. **Could you send them this week?** If they're going to take
longer, that's fine, but tell us now and we'll load the expired records without documents and attach
them in a later pass rather than holding anything up.

**Two smaller items on the ASIN list.**
It contains 21,034 rows, which resolve to 16,237 unique certificate-and-ASIN pairings — some ASINs
are repeated under the same certificate (one appears 14 times). We'll load each pairing once unless
you tell us the repeats are meaningful. Also, six certificate numbers on the *active* ASIN list map
to certificates that appear on the expired lists (23-0814, 25-0484, 25-0485, 25-0486, 25-0487,
25-0492) — could you confirm how those should be treated?

**On removing the existing test data** — agreed, and we'll do that as a distinct, reviewed step
before the load rather than as part of it, so there's a clear record of exactly what was removed.
We've noted your instruction to leave the Certification Types, Product Category/Type/Subcategory,
Tags, Pathways and Partners options in place — those stay. One thing we need from you: your list of
the **key test accounts** to keep, so we don't remove a login someone is still using.

**Certificate numbering — two things to sort out before 18 August.**
Your plan asks us to make sure new certificate numbers pick up from where your team left off. The
system does continue automatically from the highest number it can see — but it can only see what we
load, and that turns out not to be the whole picture.

**First, the 110 numbers we'd otherwise re-use.** The lists you sent stop at **26-0481**, but your
database has issued numbers up to **26-0591**. That leaves 110 numbers (26-0482 to 26-0591) that are
already in use but invisible to us — **107 are internal drafts, and 3 are live "Client Approved -
Paid" certificates**. Because none of them are in the lists we're loading, the new system has no way
to know those numbers are taken, so the first certificate created after go-live would be issued
26-0482 — which one of your drafts already holds, and it would keep going from there. It wouldn't
show as an error at the time; it would surface later, when those certificates are eventually loaded
and can't be, because their numbers now belong to something else.

**The simplest fix: include your draft certificates in the 17 August pull.** We'd load them as
Drafts, exactly as they are now — they stay internal and don't appear anywhere public — and the
numbering then continues correctly on its own, with each number still attached to the certificate
that owns it. Your plan already anticipates re-pulling this group closer to import, so this may just
be a case of including them when you do. If you'd rather not send the drafts, tell us your true last
issued number and we'll set the starting point manually instead.

(The 3 live ones in that range are a separate concern — they're part of the missing-certificates
point above, so they need to arrive in the load either way.)

**Second, the period from 18 August through launch**, when your plan has the team adding
certificates to *both* the current database and the new one. Both systems would be choosing the next
number independently, so both could hand out the same one to different certificates. Two ways to
avoid that, whichever suits your team:
- **Reserve a block** — you keep issuing numbers as normal in the current database, and we start the
  new one at an agreed distance above your last number (your plan mentions leaving a buffer, which
  would do exactly this). This is the one we'd suggest, as it works even if someone forgets.
- **Single source** — from 18 August, new certificate numbers are only issued in one of the two
  systems, and the other is updated from it.

Could you let us know which you'd prefer, and if it's the buffer, roughly how many numbers per month
your team issues so we can size it sensibly?

---

**Finally — the items we raised on 22 June.** We had seven open decisions from the earlier data
review. Rather than resend them, we re-checked all seven against the files you've just sent, and
most have resolved themselves:

- **Missing certificates 25-0437 / 25-0438** — resolved. Both are in the new expired list, and there
  are now **no orphan ASINs at all** (there were 51). Nothing needed.
- **Block, Inc. vs Square** (certs 25-0479 / 25-0482) — resolved. Both files now say "Block Inc."
  consistently, so there's no ambiguity left. We'll use your spelling. Nothing needed.
- **Retired certification types** — answered by your email below; we'll show the real type name and
  hide it from the dropdown, rather than the "Unspecified" label we'd previously proposed. Nothing
  needed unless you'd prefer the old approach.
- **Duplicate expired certificates** — this is the same issue as item 3 above, now covering 137
  numbers rather than the 114 we saw in April. Covered there; no separate answer needed.

That leaves **three** that still need you:

**A. Amazon code for Certified Environmental Facts.** Your ASIN list uses `greencircle_cef` (8,562
rows), and it's consistent throughout, so we're happy to go with it. The reason we keep raising it is
that it's the one code never formally confirmed *with Amazon* — the developer guide only ever
documented Recycled Content and Sustainable Energy Practices. Worth confirming on their side before
9/4, since it affects the badge.

**B. Long product names.** 96 of your ASIN product names are longer than 200 characters (the longest
is 951). We've already widened the field so they're stored in full and passed to Amazon complete,
rather than being cut short — we'd just like your confirmation that full names are what you want,
since it changes what Amazon receives.

**C. Certificate 24-1634 (Philips) — conflicting certification result.** Your ASIN list shows two
different values for this certificate: **0.63** on 513 rows and **0.23** on 13. We couldn't call this
one in April, but the new data points one way — your certificate listing itself now shows **0.63**.
**Unless you tell us otherwise we'll go with 0.63** and treat the 13 rows as stale. A quick yes is
all we need.

If A, B and C all look right, replying **"go with your recommendations"** covers them.

Once we have your call on item 3, we can start. Everything else is ready to go.

Best,
Shawn

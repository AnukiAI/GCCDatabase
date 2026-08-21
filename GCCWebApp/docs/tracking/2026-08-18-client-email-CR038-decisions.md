# Client email — CR-038 duplicate certificate numbers, five decisions

**Status:** Draft for Shawn to send
**Date drafted:** 2026-08-18
**Covers:** CR-038 only. CR-039 (the test-data clear-down) is **not** in this email — it has already
been carried out, so it needs a factual note rather than an approval request. See the note at the
bottom of this file.
**Attach:** `CR-038-client-report-duplicate-certificate-numbers.md` (as PDF) — it holds the full list
of the 137 numbers, the 9 merge candidates and all 29 incomplete records.

> **Internal notes — not for sending.**
>
> 1. **This email asks them to confirm a decision we have already acted on.** The data went into the
>    test site on 2026-08-18 under Option A on Shawn's authority; no client sign-off is on file for
>    either CR-038 or CR-039. The wording below is deliberately straight about that — they will see
>    5,702 certificates when they log in, and discovering the duplicates unannounced would be worse
>    than us naming it first. If Option A is rejected we reload; that is a real cost, so decision 1
>    is not a formality.
> 2. **The public figure changed and both versions were right.** The attached report says "8 currently
>    public". Measured on the loaded test data: **8 duplicated numbers**, made up of **20 certificate
>    records**, every one of them Regupol America. The report counted numbers; 20 counts records. The
>    email gives both so the two documents cannot look like they disagree.
> 3. Two urgent items are **not** in this email — the 1,960 untagged ASINs and cert `24-1634`'s
>    result value. Both are already visible outside the company. See the bottom of this file.

---

**Subject:** Certificate data is loaded on the test site — five decisions on duplicate certificate numbers

Hi [NAME],

Your certificate data is now loaded on the test site and ready for you to review — **5,702
certificates, 16,234 ASINs, and 5,474 certificate documents**, all reconciled against your own
spreadsheets in both directions with nothing unexplained.

Before you start reviewing, there is one thing you should know up front, because you will see it
straight away.

## What we did, and why we are telling you now

**137 of your certificate numbers are each used by more than one certificate — 312 records in
total.** These are not filing errors. They are genuinely different certificates that happen to share
a number: different products, different companies, different dates, and **310 of the 312 have their
own distinct signed document.**

Our system was originally built to enforce one certificate per number, so these records could not be
loaded at all. We recommended **Option A — load them as separate certificates**, keeping every record
and every document exactly as issued, and that is how they are loaded now, so that you could review
the complete dataset rather than a filtered version of it.

**We would like that confirmed rather than assumed.** If you would prefer Option B (one record per
number, with the extra documents attached to it), we can change it — it means reloading, so it is
better decided now than after you have worked through the data.

**This has no effect on Amazon.** We checked the partner feed specifically after loading: the response
format and values are unchanged, so there is nothing for Amazon to re-test.

## The five decisions

| # | What we need from you | Why it matters |
|---|---|---|
| **1** | **Confirm Option A** — keep these as separate certificates. Or tell us to switch to Option B. | Everything else below assumes Option A. Changing it means a reload. |
| **2** | **`21-0179` and `21-0180` each carry two unrelated companies.** `21-0179` is filed under both **Deceuninck North America** and **DuPont™ Performance Building Solutions**; `21-0180` under both **Deceuninck North America** and **4ocean, PBC**. Which is correct in each case? | These two look like genuine source errors rather than the same pattern as the rest, and we will not guess at whose certificate it is. Both are expired records. |
| **3** | **9 numbers could safely be merged** — 8 where the identical document was filed twice, and 1 where the two records match on every single field. Merge them, or keep them separate for consistency with the other 128? | Small, but it changes your final certificate count. |
| **4** | **8 of these duplicated numbers are currently public** — 20 certificate records in total, all **Regupol America**. `26-0182` alone has four certificates sharing that one number. Should the public site show any indication that a number is shared? | Today a visitor searching `26-0182` sees four separate certificates with the same number and nothing explaining why. |
| **5** | **29 records could not be loaded at all** because required information is missing — 13 have no expiration date, 11 no product name, 3 no product name and no dates, and 2 no effective date. Can you supply the missing values, or should they be left out permanently? | They are the only records from your spreadsheets not on the test site. We will not invent the values. **Two of the 29 are currently approved and public** (`23-0358` and `25-0351`, both Sofidel America), so these are live records, not just history. |

## Where this sits

The remaining 128 numbers we are treating as settled unless you tell us otherwise: different
certificates, same issued number, all kept with their own documents. Your own system agrees with that
reading — we checked, and it holds 140 numbers with more than one record against our 137.

Everything above is in the attached report, including the full list of numbers, the 9 merge
candidates, and all 29 incomplete records with the specific field missing from each.

Happy to walk through it on a call if that is quicker than email — decisions 2 and 5 in particular
may be faster to resolve live.

Best,
[SIGNATURE]

---
---

# Not part of the email above — for Shawn

## 1. CR-039 still needs a note, not an approval

The clear-down ran on 2026-08-18. What the client has not been told:

- **229 test certificates, 173 companies, 312 products and 930 activity-history entries were removed**
  before loading, per their written instruction of 2026-08-12.
- **83 test user accounts were deleted.** Five were kept: `swest@anukiconsulting.com` (required — the
  system records changes against it), `admin@`, **`carly@`**, `analyst@` (all
  `greencirclecertified.com`) and `partner@amazon.com`. Every other account looked like our own
  scaffolding. **If anyone on their side had a login we did not recognise, it is gone and only we can
  recreate it** — worth asking explicitly.
- **The migrated certificates arrive with no change history.** The data was loaded directly into the
  database rather than typed through the application, so there is no "imported by / imported on"
  entry per record. History accumulates from their first edit onward.
- Everything they asked us to keep survived and was re-verified after the load: certification types,
  product categories and types, tags, pathways, partners, **all API keys including Amazon's**, roles
  and permissions, and the Google Analytics settings.

## 2. Two items I would put in front of them BEFORE the CR-038 decisions

Both are already visible outside the company, which the CR-038 items mostly are not:

- **1,960 untagged ASINs (DEF-352)** — 12% of the load, across 4 certificates (23-0814 P&G; Kohler
  25-8535 / 26-0053 / 26-0101). They carry no partner tag, and under CR-171 Model A an untagged
  product identifier is **returned to every partner that calls the API**, not hidden. This is
  fail-open, and it is live now.
- **Cert `24-1634` loaded with a certification result of `0.63`** — the source gives both 0.63 and
  0.23 and our own tracker records this as having no safe default and needing the client
  specifically. It went in on our recommendation and **the certificate is publicly visible.** Either
  they confirm 0.63 or we are correcting a public record.

Say the word and I will fold both into the email above, or draft them as a separate shorter note.

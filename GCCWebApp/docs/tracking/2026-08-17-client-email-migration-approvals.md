# Message to client — two approvals needed before the migration loads into test

**Status:** Draft for Shawn to send
**Date drafted:** 2026-08-17
**Covers:** CR-038 (duplicate certificate numbers) + CR-039 (test-data purge authorization)
**Attach:** `CR-038-client-report-duplicate-certificate-numbers.md` (as PDF) — it holds the full
list of the 137 numbers and the 29 incomplete records referenced below.

> **Internal note, not for sending.** The keep-list is **fixed in the script as of 2026-08-17** — all
> five accounts below are now kept, verified against the live test database (0 unmatched, users
> deleted 86 → 83). The question in *"Before we run it"* is therefore a genuine check, not a
> disguised repair: we want to know about any client-side login we can't recognise as theirs. Every
> remaining account looks like our own test scaffolding, but only they can confirm that.

---

**Subject:** Ready to load the certificate data into the test site — two approvals needed

Hi [NAME],

The certificate migration is built and fully rehearsed. We've loaded your complete dataset onto our
development environment end to end, and it's now verified against your own spreadsheets rather than
against our own working files — **every certificate and every ASIN in the workbooks is accounted for,
in both directions, with nothing unexplained.**

| Loaded and verified | |
|---|---|
| Certificates | **5,702** across **5,527** certificate numbers |
| Product identifiers (ASINs) | **16,234** |
| Certificate PDFs attached | **5,474** |
| Records in your spreadsheets that didn't load | **29** — the incomplete ones, listed in the attached report |
| Anything else unaccounted for | **none** |

The next step is loading it into your **test site** so you can review it properly, with real data, in
the real application. **We need two approvals before we can do that.**

---

## 1. Duplicate certificate numbers — please confirm Option A

This is the item we wrote to you about on 14 August. In short: **137 certificate numbers in your data
are each used by more than one certificate — 312 records in total.** These aren't filing errors. They
are genuinely different certificates that happen to share a number: different products, different
companies, different dates, and **310 of the 312 have their own distinct signed PDF.**

The system was built to enforce one certificate per number, so today these records can't load at all.

**Our recommendation is Option A — load them as separate certificates**, keeping every record and
every document exactly as issued. It has already been built and proven on our development
environment, so it's ready the moment you confirm it. The alternative (Option B, one record per
number with the extras attached as additional documents) remains available if you'd prefer no
database change during go-live.

**Five decisions in total**, all covered in the attached report:

| # | What we need |
|---|---|
| 1 | **Confirm Option A** (or B). Nothing loads for these 137 numbers until this is settled. |
| 2 | **`21-0179` and `21-0180` each carry two unrelated companies** (Deceuninck North America vs DuPont Performance Building Solutions; Deceuninck vs 4ocean). These look like source errors rather than the same pattern as the rest — which is correct? We don't want to guess. |
| 3 | **9 numbers could safely be merged** — 8 where the same document was filed twice, 1 identical on every field. Merge them, or keep them separate for consistency? |
| 4 | **8 of these certificates are currently public.** Should the public site show any indication that several certificates share a number? |
| 5 | **The 29 incomplete records** — can you supply the missing product names and dates, or should they be left out? They can't load as they stand and we won't invent the values. |

Worth saying plainly: **this has no effect on Amazon.** We checked the partner feed specifically —
the response format and values are unchanged either way, so there's nothing for Amazon to re-test.

---

## 2. Clearing the test site before we load

Your note of 12 August asked us to remove the test certificates and ASINs before uploading, ready for
go-live. We'd like your sign-off on exactly what that removes, because **it can't be undone from
inside the application** — recovery means restoring the database from a backup we take immediately
beforehand.

**What gets removed** (measured on your test site today):

| | |
|---|---|
| Certificates | 229 |
| Companies | 173 |
| Products | 312 |
| Test user accounts | 83 |
| Activity/audit history | 930 entries |

**What stays, untouched:** certification types, product categories and types, tags, pathways,
partners, **all API keys including Amazon's**, roles and permissions, and your Google Analytics
settings. The clear-down checks each of these before it commits — if any of them were affected it
stops and reverses itself rather than continuing.

### Before we run it, three things to confirm

**a) Which user accounts should survive?** This is the one we'd most like a clear answer on. The
clear-down removes test accounts, and we want to be certain none of yours are caught in that. We've
set it to keep:

- `swest@anukiconsulting.com` (required — the system uses it to record changes)
- `admin@greencirclecertified.com`
- **`carly@greencirclecertified.com`**
- **`analyst@greencirclecertified.com`**
- **`partner@amazon.com`**

Everything else we can see is test data we created — seeded demo users and accounts built for our own
QA runs. **If anyone else on your side has a login to the test site, tell us now** and we'll add them.
An account that's removed has to be recreated by us with a new password; it isn't something you can
recover yourselves.

**b) Is the activity history worth keeping?** The 930 audit entries are a record of test activity
only. We assume they have no value once real data is loaded — please say if that's wrong.

One related point, so it isn't a surprise later: **the migrated certificates will arrive with no
change history.** The data is loaded directly into the database rather than typed in through the
application, so there's no "imported by / imported on" entry against each record. That's normal for a
migration of this size, and history starts accumulating from your first edit onwards — but if you
were expecting to see it, better to know now.

**c) When can we do it?** Your test site is in daily use, and the clear-down plus load isn't instant.
We'd like an agreed window so nobody is working in there while it runs. We'll take a full backup
immediately beforehand in the same session.

**We'd suggest an evening slot, shortly after 22:30.** Your host runs an automatic backup of the test
database at about 22:05 each night, and running just after it gives us both the freshest restore
point and the most room to work in.

**One thing to check with whoever manages the hosting:** that automatic backup ran every night from
8 to 15 August and then appears to have stopped — there's nothing recorded for the 16th or 17th. It's
very likely just a job that needs restarting, but we'd rather have it confirmed running again before
we do this. Nothing is at risk in the meantime; we hold our own separate backup as well.

---

## What happens once you confirm

We load your full dataset into the test site and hand it back for review — all 5,702 certificates,
their PDFs and their ASINs, in the live application. That review is the point of this step: it's much
easier to spot something wrong looking at real records on screen than in a spreadsheet.

Happy to walk through any of this on a call if that's quicker than email.

Best,
[SIGNATURE]

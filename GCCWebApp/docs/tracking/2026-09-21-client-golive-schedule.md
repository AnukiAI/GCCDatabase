# GCC Certificate Database — Go-Live Plan and Schedule

**Prepared by:** Anuki Consulting · **Date:** 21 September 2026
**Proposed go-live:** **Tuesday 13 October 2026** (fallback: Tuesday 20 October)

---

## The short version

The new certificate database will live at **`db.greencirclecertified.com`** — the same web address
your certificates use today. `database.greencirclecertified.com` will forward to it automatically, so
either address gets people to the right place.

Two things matter most, and both are covered:

- **Nothing changes before go-live day.** The current database keeps running exactly as it does now,
  right up to the moment we switch. Every step before that happens on our side, invisibly.
- **Existing links keep working after.** Links your team has emailed, printed or bookmarked — including
  links to individual certificates — will be forwarded automatically to the matching certificate in
  the new system. We have built and tested that forwarding for **5,999 certificate records** and will
  check a sample of it before we switch anything.

If anything goes wrong on the day, we reverse a single setting and the current system comes straight
back. Recovery is minutes, not hours, and we never modify the existing site — so there is nothing to
undo.

---

## What is left to do

| Area | What remains | Who |
|---|---|---|
| **Software** | Final build with the last round of fixes, including a performance safeguard for public traffic and a fix to certificate editing by spreadsheet | Anuki |
| **Web addresses** | Forwarding rules so today's links keep working, and search-engine settings switched from "private" to "public" | Anuki |
| **Test environment** | A permanent separate test site, so your team keeps a safe place to try things after go-live | Anuki |
| **Data** | Certificate numbering aligned with your current system, a final catch-up load, and a two-way reconciliation | Anuki, with your input |
| **Your review** | User acceptance testing against the three user guides, and written sign-off | **GCC** |
| **Amazon** | Advance notice that the address in the data feed changes | GCC + Anuki |
| **Email** | Moving system emails onto the greencirclecertified.com domain — **deliberately scheduled after go-live** so an email issue cannot delay the launch | Anuki |

---

## The schedule

| Week | What happens | Who |
|---|---|---|
| **22 – 26 Sep** | Final software fixes. Build and test the link-forwarding map. Stand up the permanent test site. | Anuki |
| **By Fri 26 Sep** | **Decisions needed from GCC** — see the list below | **GCC** |
| **29 Sep – 3 Oct** | Final build released to the test environment. Full testing. Certificate numbering aligned, final data catch-up, reconciliation both directions. | Anuki |
| **6 – 10 Oct** | **Your review week.** GCC tests the system in the three roles — Administrator, Analyst, and Company user — using the guides we provide. Amazon is given notice of the address change. | **GCC** |
| **Fri 10 Oct** | Written sign-off. Preparatory address change (no effect on the live site). | GCC + Anuki |
| **🚀 Tue 13 Oct, 9:00 am** | **Go-live.** Freeze changes in the current system, run the final data check, switch the web address, verify, and open the site to the public. Expect the switch itself to take about two hours, with the address change reaching everyone within a few minutes. | Both |
| **13 – 17 Oct** | Close support. We monitor for any link that did not forward correctly and fix it the same day. | Anuki |
| **Late Oct – Nov** | **Phase 2:** move system emails onto your domain, and retire the old database platform, including handing over its data and ownership. | Both |

We have chosen a **Tuesday** on purpose: it gives three full business days of support before a weekend.
If your review slips past 10 October, we move to **Tuesday 20 October** rather than compress the
testing.

---

## What we need from you

> The sendable version of this list, with the reasoning and deadlines spelled out, is
> `2026-09-21-client-email-golive-decisions.md`. **Keep the two in step** — they are the same
> **six** items, and item 2 there carries the 12 August follow-up framing plus the unnumbered
> Interface certificate.
>
> **Revised 22 September: the old item 1 — who changes DNS — is answered.** We hold full DNS access,
> so it is no longer a client decision and everything below is renumbered. The letter replaces it
> with a short *"A note on the domain setup"* section that asks nothing but flags one contingent
> decision: if HostMonster cannot clear the fault currently blocking saves in that control panel, we
> may need to recommend moving the domain's records to another provider, which is their call.

These are the items that hold up the schedule. The first two are the ones that matter most.

**Nothing is needed from you on the domain setup.** We have the access required, and the changes on
go-live morning are ours to make.

1. **What is the last certificate number issued in the current system?** We need this in writing.
   The new system generates the next number automatically, and without your current figure it would
   start from a number that has already been used. The cleanest fix is to include your in-progress
   drafts in the final data pull, which makes the numbering correct by construction.

2. **The 174 certificates that were not in the migration files — follow-up to 12 August.** They
   appear in your Full List and in none of the three lists we were asked to load: **51 Client
   Approved – Paid** (we confirmed 50 are publicly visible today), 26 Expired, 96 Draft – Internal,
   1 Draft – Client. The **51** are the ones that matter — mostly ASSA ABLOY brands, and they would
   simply not exist in the new system. Raised 12 August; we do not believe we have had an answer.
   One related case needs a decision too: an Interface certificate that has **no certificate number
   at all**, in either system, and so cannot be loaded or linked as things stand.

3. **Change freeze.** Please confirm a cut-off after which no new certificates are entered into the old
   system, and the go-live hour that suits your team.

4. **Review dates and sign-off.** Who tests, and who signs off, for each of the three roles.

5. **Amazon's notice period.** How much advance warning Amazon requires for the address change in the
   data feed.

6. **Please keep the existing database platform running for 30 days** after go-live. We do not need it
   to serve traffic — it is a safety net, and we would rather have it and not use it.

---

## A note on the current web addresses

| Address | Today | After go-live |
|---|---|---|
| `db.greencirclecertified.com` | The current certificate database | **The new system.** The primary address. |
| `database.greencirclecertified.com` | Forwards to the above | Forwards to the above — unchanged in behaviour |
| `dbtest.greencirclecertified.com` | The address Amazon tests against | Unchanged — stays available for partner testing |

Amazon's integration reads a certificate's web address out of the data feed. That address changes at
go-live, which is why they need notice — the change itself is routine, but it should not be a surprise.

---

*Questions on any of the above are welcome before you circulate this internally. The dates are a
proposal shaped around your review week, and we would rather adjust them now than compress testing
later.*

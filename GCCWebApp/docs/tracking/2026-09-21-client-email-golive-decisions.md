# Client email — go-live decisions required

**Date:** 21 September 2026 · **Revised:** 22 September 2026 · **From:** Anuki Consulting
**Status:** DRAFT, not sent
**Companion:** `2026-09-21-client-golive-schedule.md` (the schedule this list unblocks)

> **Internal note — do not send this box.**
>
> **Revision 22 September: seven decisions became six.** The original item 1 asked who makes the DNS
> changes at HostMonster. **That question is answered — we hold full DNS access**, so it is no longer
> a client decision and has been replaced by a short *"A note on the domain setup"* section that asks
> nothing. Everything after it is renumbered: old 2→1, 3→2, 4→3, 5→4, 6→5, 7→6.
>
> ✅ **`2026-09-21-client-golive-schedule.md` has been brought into step (22 September)** — its
> "What we need from you" list is renumbered to the same six items and carries the same
> "nothing needed from you on the domain setup" line. The two documents agree; if you edit one,
> edit the other.
>
> Item 2 (the 174 certificates) was first raised in `migration-client-note-2026-08-12.md` and has
> never been answered. It is phrased as a follow-up, not a fresh question, so it reads as a reminder
> rather than something we forgot to ask.
>
> **On the domain-setup note:** we mention the control-panel fault because the fallback — moving DNS
> management to another provider — is *their* zone and therefore their decision, and it needs lead
> time. It is deliberately framed as "no action needed now" so it informs without alarming. Do not
> expand it into a technical explanation; if they ask, that conversation is better had on a call.
>
> **On the dates:** 13 October is still the proposal. Two items could move it and neither is the
> client's fault — the domain control-panel fault (§ note) and one internal verification item. If
> either is still open in the week of 29 September, raise the fallback date with them **before** they
> commit their review week, not after.

---

**Subject:** GCC Database go-live — six decisions we need by Friday 26 September

Hello,

We have a go-live plan ready and are proposing **Tuesday 13 October** for the switchover, with
Tuesday 20 October as the fallback. The full schedule is attached.

Six things need a decision from your side before we can commit to those dates. They are listed in
order of how much of the schedule each one holds up. Most are quick; two are not, and I have
flagged those.

None of them require technical knowledge to answer — where a decision has a trade-off, I have set
out the options and our recommendation.

---

## A note on the domain setup — no action needed from you

Your domain records are managed at **HostMonster**, and we have the access we need, so the changes
on go-live morning are ours to make. Nothing is required from you here.

One thing worth mentioning so it is not a surprise later: the control panel at HostMonster is
currently refusing to save changes to this domain, because of a fault in its own validation rather
than anything wrong with your records. **Nothing is broken and nothing is at risk** — the live site
and your email are entirely unaffected. We are working with the host to clear it.

If they cannot, the alternative is to move the domain's records to a different management provider —
a routine change, but it is your domain, so it would be your decision. **We are not asking for that
decision today.** If it becomes necessary we will come to you with a clear recommendation and
whatever time it needs.

---

## 1. What is the last certificate number issued in the current system?

**What we need:** the most recent certificate number you have issued, in writing — including any
drafts in progress.

The new system generates the next certificate number automatically, by continuing from the highest
number it can see. The records we were given to load stop at **26-0481**, so left alone the new
system would issue **26-0482** next. Our reading of your live system is that you have already
issued well past that, into the **26-05xx** range.

If that is right, the new system would reissue roughly a hundred numbers that already belong to
other certificates. The problem would not show up on day one — it would surface weeks later as
certificates that cannot be saved because their number is already taken.

**The cleanest fix** is to include your in-progress drafts in the final data pull. Loading them as
drafts makes the numbering correct automatically, with nothing for anyone to remember.

**Why it matters:** this is the one item where being wrong is expensive and the damage is delayed.

## 2. The 174 certificates that were not in the migration files — follow-up to 12 August

**What we need:** confirmation of which of these should be loaded.

We raised this on **12 August** and I do not believe we have had an answer, so I am repeating it
here rather than assuming.

When we compared your **Full List** tab against the three lists we were asked to load, **174
certificates appear in the Full List and in none of the load lists**:

| | Count | Note |
|---|---|---|
| **Client Approved – Paid** | **51** | **We confirmed 50 of these are publicly visible on your current database today** |
| Expired | 26 | |
| Draft – Internal | 96 | |
| Draft – Client | 1 | |

The **51** are the ones that matter. They are live, paid certificates — mostly ASSA ABLOY brands
(Corbin Russwin, SARGENT, Malarkey, Pioneer, McKinney, ACCENTRA, Greenfield). Eleven of them also
appear on your Hold List marked "Keep".

**If we load exactly what was sent, those 51 certificates will not exist in the new system**, and
they will disappear from public view at go-live.

**One related detail, also for your decision.** One certificate in your records — Interface, "Carpet
tile on CQuest GB Backing", Closed Loop Product, expired December 2025 — **has no certificate number
at all**, in your current system or in the spreadsheet. Because the new system identifies every
certificate by its number, this one cannot be loaded or linked as things stand. Would you like a
number issued for it, or should it be left behind?

**Why it matters:** this is the only item on the list where doing nothing means live certificates
quietly vanish.

## 3. Change freeze and the go-live hour

**What we need:** a cut-off date and time after which no new certificates are entered into the
current system, and confirmation of what hour on 13 October suits your team.

Since the last data load on 3 September, certificates have been entered into both systems. We need
a clean stopping point so the final catch-up load is complete and nothing is entered twice.

We would suggest starting at **9:00 am** on the day. The switch itself takes about two hours, and
the change reaches everyone within a few minutes of the record being updated.

## 4. Review dates and sign-off

**What we need:** who tests, and who signs off, for each of the three roles — Administrator,
Analyst, and Company user.

We have reserved **6–10 October** for your review, with written sign-off on **Friday 10 October**.
We will provide a user guide for each role beforehand.

**Why it matters:** if sign-off slips past 10 October we move to the 20th rather than compress
testing. We would rather shift the date than shorten the review.

## 5. How much notice does Amazon require?

**What we need:** Amazon's required notice period for a change to the web address in the data feed.

Amazon's integration reads each certificate's web address out of the feed we provide. That address
changes at go-live. The change itself is routine — it should not be a surprise to them.

## 6. Please keep the existing database platform running for 30 days

**What we need:** your agreement to leave the current platform in place — not serving visitors, just
not deleted — for 30 days after go-live, and confirmation of who owns its data export and eventual
shutdown.

**Why it matters:** it is a safety net. We do not expect to need it, and we would much rather have
it and not use it.

---

## What happens to existing links

Worth stating plainly, because it is the question most people ask:

- **Nothing changes before go-live day.** Every step until then happens on our side, invisibly. The
  current database keeps running exactly as it does now.
- **Existing links keep working afterwards.** Links your team has emailed, printed or bookmarked —
  including links to individual certificates — are forwarded automatically to the matching
  certificate in the new system. We have built and tested that forwarding for **5,999 certificate
  records**, and we will check a sample of it before anything is switched.

If something is wrong on the day, we reverse a single setting and the current system comes straight
back. Recovery is minutes, and we never modify the existing site, so there is nothing to undo.

---

**Our ask:** answers to items **1 and 2** by **Friday 26 September**, and the rest by **Friday 3
October**. If items 1 or 2 are still open on 3 October, we would move to the 20 October date rather
than run the switchover with them unresolved.

Happy to walk through any of this on a call — several of these are faster discussed than written.

Best regards,

**Anuki Consulting**

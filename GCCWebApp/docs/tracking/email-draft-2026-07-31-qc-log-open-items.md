# Client Email — QC Log Open Items (7.30.26 log) — send-ready draft

*Fill in [Name] and the recipient address. Reflects the reconciliation of
`Database QC Log_7.30.26.xlsx` completed 2026-07-31. Full internal detail:
`docs/tracking/qc-log-open-items-2026-07-31.md`.*

---

**To:** [client contact]
**From:** Shawn West, Anuki Consulting
**Subject:** QC log — where the open items actually stand, and what we're fixing this week

---

Hi [Name],

We've been through the 7/30 QC log line by line and reconciled it against what's actually built. Short version: **the open list is much smaller than it looks, we've started on the quick wins today, and there are two decisions we need from you.**

## The open count is misleading — and that's on our side to fix

The log currently shows **58 items marked "Open."** When we went through them individually, **46 of those already have a completion date and your own confirmation that they're fixed** — the status column simply never got flipped to "Complete."

That leaves **12 genuinely open**, and of those:

- **8 need work from us**
- **3 are waiting on something outside the application** (more below)
- **1 just needs its status confirmed and closed**

We're not raising this as a bookkeeping complaint — it matters because "58 open" is the number that shapes every conversation we have about where the project stands, and it's roughly four times the real figure. We'd suggest we do one pass together to sync the status column so the log reflects reality going forward.

## The 8 real items — what you see today vs. what you'll get

| # | What's happening now | What it will do |
|---|---|---|
| **171** | The certificate import template's Partner column header just says "Partner" — nothing tells you comma is the separator for multiple partners. | Header reads **"Partner (separate by comma)"**. |
| **240** | The guidance text for Certification Results sits *below* the box. | The same wording appears as grey placeholder text **inside** the box, which is what you asked for. |
| **193** | Edit a certificate → Save → go to Certificates → Add Certificate, and a success message for the *previous* action pops up on the blank form. | The confirmation appears once, for the action that caused it, and doesn't resurface. |
| **197** | In search, if you type a word and then move your mouse over the dropdown suggestions, pressing Enter picks the item you *hovered over* instead of what you typed. | Hovering only highlights. Enter keeps what you typed. A suggestion is only chosen by arrow keys or an actual click. |
| **188** | When you correct an expired certificate's date to a future date, the system silently puts the status back to what it was before. | **Status stays "Expired"** and you get an alert telling you to update it. *(This one needs your sign-off — see below.)* |
| **218** | New certificate numbers are coming out as 7 characters (26-10095) rather than 6. | `YY-XXXX`, widening only after 9,999 certificates in a year — plus we'll demonstrate the 2027 rollover for you. |
| **242** | Product Type "Carly Test" won't delete — it reports 2 certificates using it, but those 2 can't be found anywhere to reassign. | The count and the list you can actually act on will agree, so the block can be cleared. |
| **243** | Some Product Types exist with no parent Product Category. | We'll clear the existing ones **and** close the path that created them, so they can't come back. |

**On 171 and 240 specifically:** both of these we told you were fixed, and they weren't — we addressed the request as we'd understood it rather than what you actually asked for. That's our miss and we've logged them as defects against ourselves. Both are quick, and both are in the batch we started today.

## What we're doing this week

**Done today (items 171, 240, 193, 197).** All four are built and verified on our test environment, and will go out in the next deployment. Two of them turned out to have a cause worth mentioning:

- **Item 193** (the stray success message) was reported fixed on 6/23. That fix was real, but it was checked against a different sequence of steps than the one you gave us on 7/30 — which is exactly why your repro steps were what finally pinned it down. Saving an *edit* was leaving behind a confirmation message that no page ever displayed, and an undisplayed message isn't discarded — it waits for the next page willing to show it. That page was the blank Add Certificate form. It's gone.
- **Item 171** wasn't just missing text. The template had been telling people to separate partners with a `|` character, not a comma. So the instruction and your expectation genuinely disagreed. The heading now says "separate by comma", and we confirmed the import accepts both — so **any file built from the older template still imports correctly.**

While fixing item 197 we also found that the keyboard highlight in the search suggestion list had never been visible at all — so arrow-key navigation now shows you where you are. That's fixed in the same batch.

**Next, once we hear back from you (188, 218).** Both need a decision — see below.

**Then 242 and 243 together.** These two look like the same underlying cause in the product hierarchy, so we want to diagnose them as a pair rather than patch each separately. We'll come back with an estimate once we know what we're dealing with, rather than guess at one now.

## Two decisions we need from you

**1. Item 188 — expired certificates.** You've asked that when someone corrects an expiration date to a future date, the status stays "Expired" and the person updates it themselves. We can do that. We want to confirm you're happy with the tradeoff: it means **every** date correction now takes a second, manual step to set the status. The alternative is what's built today, where the system restores the previous status automatically. Your call — just confirm and we'll build it.

Also, so it's on the record: you'd previously asked us for the automatic version, which is why it works that way now. No issue with changing it, we just want to make sure the new behavior is what you want before we swap it.

**2. Item 218 — certificate number length.** You noted you're fine with 7 characters *only* if a year's numbers run out. To build to that we'd be committing the system to a hard ceiling of **9,999 certificates per calendar year**. That's almost certainly comfortable, but it is a real limit, so we'd rather you confirm it explicitly than have us assume it. We'll also send you evidence that the year rollover works (2027 producing 27-XXXX), which you asked for.

## One thing we found ourselves

While testing the Partner template fix, we checked that a certificate imported with two partners really does end up linked to both. It does. But we then found that **the CSV export only reports the first one** — a certificate tied to Amazon and Novi exports as "Amazon". The certificate itself is correct, and the tile views are correct; it's specifically the export column that under-reports.

We haven't fixed it yet, on purpose. The export is scoped per partner for confidentiality reasons, so before we change what that column shows we need to agree what it *should* show: an internal user should presumably see every partner on a certificate, but a partner logging in should still only see their own. We'd rather confirm that with you than guess. Flagging it now because it affects the multi-partner capability you asked for, and any partner analysis you've run off that export.

## Three items that aren't blocked on us

- **Item 150 — password reset emails.** The functionality is built and verified on our side. The test site has no email credentials configured, so reset emails can't physically leave the server. We need mail settings from your IT before this can be tested end to end.
- **Item 151 — Duo as a two-factor option.** Open since April. We can scope it, but we need to know which Duo method you want and whose tenant it runs under.
- **Item 173 — removing the "Partner" column from the partner login view.** You asked us to keep this visible during testing and strip it before go-live. It's still there deliberately. We're moving it onto the pre-launch checklist so it can't be missed at cutover.

## One housekeeping note

There are three item numbers used twice in the log (197, 238, 242) — in each case one row is a "no change needed" note and the other is live work. Worth renumbering so we're never talking about two different things under one number. Similarly, everything from item 187 onward has the classification and severity columns blank, which is a good part of why the open list is hard to read at a glance.

Happy to walk through any of this on a call — particularly the two decisions, if that's faster than email.

Best,
Shawn West
Anuki Consulting

---

*Internal notes, not for sending:*
- *Items 171/240/193/197 = DEF-316/317/318/319. Item 188 = CR-030. Items 218/242/243 deliberately not
  numbered yet — root cause unconfirmed, and per our data-vs-defect rule the classification has to
  follow the repro.*
- *Deliberately not itemized in this email: that 197 has had three partial fixes, and that 193's
  6/23 "resolved" was verified against the wrong path. Both are in the internal report if it comes up.*
- *The 46 stale rows are framed as a shared sync task rather than a log-maintenance failure — the
  point is to get the number corrected, not to assign blame for it.*

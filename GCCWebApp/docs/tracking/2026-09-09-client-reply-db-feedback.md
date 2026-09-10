# Draft reply — "DB Feedback" (Carly, 2026-09-09 10:52 AM)

Status: **DRAFT for Shawn's review. Not sent. No code changed, no register rows added.**
Source: `CR_9_9_26_edit.pdf`. Intake analysis in `observations_log.md` (2026-09-09).

Proposed tracking IDs referenced below are **reserved, not yet logged**: CR-048, CR-049, DEF-416.

---

Hi Carly,

Thanks — this is exactly the kind of feedback that's easiest to act on. I went through all four
items against the code before answering. Two are straightforward changes, and two are questions I
can answer for you right now without touching anything.

## 1. Edit button opening in a new tab — yes, and it's a small change

You're right that this is the low-risk version of the fix. All three Edit buttons you're seeing —
the one on the tile card, the one in the list's Actions column, and the one on the certificate
detail page — run through a single shared piece of code. Changing that one place changes all three
at once and leaves everything else alone.

Two things I'd like you to confirm before I build it:

- **Should the Edit button behave exactly like Preview does today** — new tab, original tab
  untouched with your search results still on screen? That's my assumption.
- **Any objection to it applying on the certificate detail page too?** Editing from there would
  also open a new tab. It's consistent, but it's a slightly different moment than the one your team
  described, so tell me if you'd rather I leave that one alone.

Proposed as **CR-048**.

## 2. List view as the default — yes, but I want to check one thing with you first

Easy change, low risk. Before I make it, though: your team's actual complaint is *"having to toggle
each time the page loads."* There are two ways to solve that and they behave differently:

- **(a) Hard default to List.** Every page load starts in List view, for every logged-in user. This
  is literally what you asked for. The catch is that it just moves the problem — anyone who prefers
  the tile view now has to toggle every time instead.
- **(b) Remember each person's last choice, starting everyone on List.** First visit lands on List;
  after that the page opens in whichever view that person used last. Nobody has to toggle
  repeatedly, whichever view they prefer. It's a bit more work than (a), but not much.

My recommendation is **(b)** — it solves the complaint permanently rather than reassigning it. One
caveat either way: the preference is remembered per browser, so someone switching computers would
start on List again.

Also worth confirming: **does this apply to your internal team only, or to external/partner logins
too?** "All logged in users" could mean either. My assumption is your team's internal view only.

Proposed as **CR-049**.

## 3. The Certificate # column in the template — yes, your read is exactly right

That column is there for precisely the case you described. The import works as an update-or-create:

- **Leave Certificate # blank** → the system assigns the next number and creates a new certificate.
- **Enter an existing certificate's number** → the import updates *that* certificate rather than
  creating a duplicate. Before anything is written you get a preview screen showing a
  field-by-field comparison of what will change, so you can back out if it isn't what you expected.

No change needed here — it's already doing what you'd want.

**One thing I do want to flag, because it affects how safely your team can use this.** Certificate
numbers in the current data are not all unique — there are certificate numbers that appear on more
than one record. If someone imports a row using one of those numbers, the system updates the first
matching record it finds, which may not be the one they meant.

That's a data condition, not a bug in the import, and it's rare enough that it may never come up.
But if your team is going to use the template routinely for edits, I'd suggest we add a check that
stops and warns when an imported number matches more than one certificate, instead of quietly
picking one. Let me know if you'd like that scoped — it would be a separate small change request.

## 4. The "Certificate Number is required" text — you're right, and I'll fix it

Confirmed: that instruction is wrong. Certificate # is not required when adding new entries, exactly
as you found. Worth knowing that the other import screen in the application already describes the
behavior correctly, so right now the two screens contradict each other — I'll bring the modal in
line and make the text spell out both cases (blank = new, filled = update).

I'm logging this one as a **defect (DEF-416)** rather than a change request, since it's incorrect
instructions rather than new functionality.

## On testing

Completely understood on not wanting to test the import against live data. When you're ready, I can
walk your team through it on a copy so they can see the preview-and-confirm step for themselves
before anyone runs it for real — the preview means nothing is written until someone approves the
diff, but it's much easier to trust once you've watched it happen.

Short version: **#3 needs nothing from us**, **#4 I'll fix**, and **#1 and #2 I'll build as soon as
you confirm the two questions above.**

Thanks,
Shawn

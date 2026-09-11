# Draft reply — "DB Feedback" (Carly, 2026-09-09 10:52 AM)

Status: **DRAFT for Shawn's review. Not sent. No code changed, no register rows added.**
Source: `CR_9_9_26_edit.pdf`. Intake analysis in `observations_log.md` (2026-09-09).

Proposed tracking IDs referenced below are **reserved, not yet logged**: CR-048, CR-049, DEF-418.

> **Corrected 2026-09-10** — three fixes, all in `docs/tracking/qc-triage-2026-09-10-QC261-264.md`:
> 1. **Item 1 no longer claims the Edit buttons share one piece of code.** They do not — `editCertificate`
>    is defined separately in three JS files and set inline in two detail views. **Five sites, not one.**
>    The original wording would have promised a one-line change and delivered a five-site one.
> 2. **Item 4 no longer claims another import screen gets the text right.** There is no other import
>    screen and no modal; `Views/CertificateImport/Index.cshtml` contradicts *itself*, and it is wrong
>    in four places rather than the one the client spotted.
> 3. **DEF-416 → DEF-418.** DEF-416 and DEF-417 were both taken on 2026-09-09 by test-harness defects.
>    (`^### DEF-` undercounts: 414-417 are `####` headings. Grep `^#{2,5} DEF-[0-9]+`.)
>
> **This reply covers only the three items in that email.** **QC #264** (the CEF `amazonCode` →
> `greencircle_cef` request, Image 121) arrived in a *separate* 9/9 message and is deliberately not
> answered here — it is **CR-021**, proposed 2026-06-11 and awaiting GCC approval ever since, with the
> script already written. Their email is that approval. Log it against CR-021 and reply separately;
> do not open a new CR number for it.

---

Hi Carly,

Thanks — this is exactly the kind of feedback that's easiest to act on. I went through all four
items against the code before answering. Two are straightforward changes, one is a question I can
answer for you right now without touching anything, and one is a genuine mistake on our side that
I'll correct.

## 1. Edit button opening in a new tab — yes, and it's a small change

You're right that this is the low-risk version of the fix, and it's a small one. Preview and Edit
already sit side by side and differ only in *how* they open the page — Preview opens a new tab, Edit
replaces the one you're on. Making Edit behave like Preview is a small, well-understood change.

One thing worth being straight with you about: the Edit button appears in five places across the
tile view, the list view and the certificate detail page, and each one is wired up separately rather
than sharing a single switch. So this is five small changes rather than one. That doesn't make it
risky — they're all the same change — but it does mean the work is to go through all five together,
because fixing four and missing one would leave you with an Edit button that still behaves the old
way and no obvious reason why.

**My assumption, unless you tell me otherwise:** Edit behaves exactly like Preview does today — new
tab, and the tab you were on is left alone with your search results still on screen. That includes
the Edit button on the certificate detail page, so the button behaves the same way everywhere rather
than differently depending on where you clicked it. Say the word if you'd rather I leave the detail
page as it is.

**One consequence worth knowing before you decide.** Once Edit opens in a new tab, the original list
tab is showing you a snapshot from before your edit — save in the new tab and the list behind it will
still show the old values until it's refreshed. Preview already works this way and it hasn't caused
anyone trouble, but it's the trade-off that comes with not losing your search.

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

## 4. The "Certificate Number is required" text — you're right, and it's fixed

Confirmed: that instruction is wrong. Certificate # is not required when adding new entries, exactly
as you found.

Having gone looking, it's wrong in a few more places than the one you spotted, and the page manages
to contradict itself:

- The instructions at the top say Certificate # is required. It isn't.
- They also say the company, product and certification type must already exist in the database.
  **None of the three has to.** Companies, products and product types are all created automatically
  if the import doesn't recognise them, and an unfamiliar certification type doesn't stop the
  import either — the certificate comes in marked *Unspecified* so an analyst can set it
  afterwards. The one field that genuinely must already exist is **Product Category**, which is
  optional but will reject a row if the value isn't a category we already hold (deliberately, so a
  typo can't create a new category).
- Further down, the same page correctly says companies and products are auto-created. So the two
  halves of the page disagree, and the half most people read first is the wrong one.
- The column names listed on the page (`CERTIFICATE_NUMBER`, `COMPANY_NAME`, and so on) do work —
  the import accepts them — but they aren't the headings the downloadable template actually uses
  (`Certificate #`, `Company`, `Product Name`, `Certification Type`). So the page is describing a
  slightly different file from the one the Download Template button gives you.

None of that changes how the import behaves — it's all instructions rather than functionality — but
it's exactly the kind of thing that makes a team hesitant to use a feature, which is what you're
describing. Both sections have now been rewritten against what the import actually does, the column
names line up with the template, and the two cases you asked about are spelled out on the page
(blank = new, filled = update, with the confirmation step in between). It will be on the test site
with the next release.

I'm logging this one as a **defect (DEF-418)** rather than a change request, since it's incorrect
instructions rather than new functionality.

## On testing

Completely understood on not wanting to test the import against live data. When you're ready, I can
walk your team through it on a copy so they can see the preview-and-confirm step for themselves
before anyone runs it for real — the preview means nothing is written until someone approves the
diff, but it's much easier to trust once you've watched it happen.

Short version: **#3 needs nothing from us** and **#4 I'll fix**. **#1 I can start on your nod** —
I've stated my assumption rather than asking, so it only needs a reply if you disagree. **#2 needs
two answers from you**: whether you want the hard default or the remembered-choice version, and
whether it applies to your internal team only or to external and partner logins as well.

Thanks,
Shawn

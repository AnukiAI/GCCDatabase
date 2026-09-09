# Client update — 2026-09-08 — every open item on the QC log

**Paste-ready.** One message covering all seven genuinely open QC items. The per-cell versions for
the spreadsheet itself are in `qc-log-update-2026-09-08-open-items.md`; this is the narrative
version for an email or a status call.

> **Everything in this message is live on gccwebapp.com as of the evening of 2026-09-08**, including
> CR-043 and CR-045, which were built and deployed the same day. Nothing here is pending a release.

---

## The message

Hi —

We went through the QC log end to end, reconciled every row against what is actually built, and
closed out the last two open change requests. Everything below is live on the test site now.

**Two quick notes on the log itself.** Seventy-four rows still read "Open" in the status column, but
sixty-seven of them already have a close date and your confirmation — they were simply never flipped
to Complete. The genuine list is the seven below. We can sweep the other sixty-seven for you in one
pass if that would help.

### 1. Confidential certificates now reach the export and Amazon (#252)

Your two answers from 9/3 are built exactly as given.

Client Approved - Paid, Client Approved - Confidential and Republished now all appear in the product
identifier export and are returned by the partner API, as long as today is inside the certificate's
effective/expiration window and the certificate is tagged to that partner. A company signed in to its
own account now sees its own Confidential certificates, and Confidential counts as Active in the
internal dashboard counts and their exports.

**The public database is unchanged, and we tested that specifically rather than assuming it.** After
the deploy, every Confidential certificate still returns "not found" to an anonymous visitor, while
ordinary public certificates still load normally. Confidential now means hidden from the public
database — and only that.

Verified live after the deploy: 13 Confidential certificates carry product identifiers — the 11 you
listed plus two that arrived in a later load (26-0473 and 26-0474). Between them they add 420 product
identifier rows to the export. All 13 are tagged to Amazon, so all 13 are now visible to Amazon. We
also confirmed the reverse, that another company's account sees its own certificates and none of
these 13 — widening a status rule must never widen who can see what.

We also fixed something you had not seen yet: the product identifiers on those certificates were
individually marked "Inactive", because that status is copied from the parent certificate when it is
saved. They would have appeared in a file called "Active Product Identifiers" while reading
"Inactive" on the Product Identifiers screen. The stored values have been corrected.

**Please read this before you re-test: 23-0758 is not Confidential.** It is Expired, expiration
8/31/26. It left the export when it expired, which is correct, and it is still absent now. If First
Quality has renewed it, it needs a renewal certificate or corrected dates. We are saying it plainly
because it has come up twice.

Also worth knowing: the 20 Confidential certificates with no partner tag carry no product identifiers
either, so they reach no partner and add nothing to the export. If any of those should be badged,
that is a separate data task — tell us which.

**One number will look different, and we would rather you heard it from us.** The internal "active
certificates" report now returns **1,061** certificates instead of 1,019. The difference is exactly
the **42** Confidential certificates that are inside their date window — the ones your "yes" on the
second question brought into the internal Active count. Nothing was added or changed in the data;
the report is counting what you asked it to count.

### 2. Certification Results is now a true free text field (#251)

Whatever is typed is what publishes — on screen, in the exports, and to Amazon — with no
interpretation at all. The old behaviour converted bare numbers on the way out (0.5 became 50%, 85
became 85%); that conversion has been removed outright rather than switched off, so it cannot
reappear on one screen and not another.

**This changes nothing about your existing data, and we measured that rather than assuming it.**
1,103 certificates carry a value — 1,072 already in `XX%` form and 31 non-numeric ("Pass", "Silver"
and similar). None of them were being converted. We compared every certificate before and after the
change: **zero** displayed values changed, and **zero** sort positions changed. This is only true
because the data correction on 9/2 came first, which normalised 803 certificates to the `XX%` form
you had used on the Amazon certificates.

One trade-off to be aware of: from now on, a mistyped `0.23` will publish as `0.23` rather than being
quietly corrected to `23%`. That is the intent of "whatever is inputted is the value we want" — we
are flagging it so it is not a surprise later. If you would like a gentle warning when someone enters
a bare decimal (a prompt, not a block), say so and we will scope it separately.

### 3. Forgot password (#150)

All three items from your 9/3 note are fixed and live:

- The page said links expire in 24 hours while the email said 1 hour. Both now say **1 hour**, which
  is what the system actually enforces.
- The Forgot Password and Reset Password screens now use the same lighter green as Sign In, so the
  logo no longer blends into the background.
- "Access your sustainability certification database." has been removed from the Forgot Password
  screens.

To your questions: the reset link is built from whatever address the site is served on, so it will
point at the production URL automatically at go-live — nothing to remember. The sender address
(currently admin@gccwebapp.com) is configuration rather than code and will change at go-live, but the
new mailbox or alias has to exist on the production domain first, so we are tracking that as a
go-live task on the email side. On the message landing in spam: noted, and that should improve once
mail sends from the production domain with its own sender records.

### 4. Two-factor authentication (#257)

You were right on both counts.

- Copy All and Download as Text were dropping half of every recovery code. The codes on screen were
  always correct, which is why it went unnoticed. Fixed. Anyone who saved a downloaded copy before
  8/31 has an unusable file and should regenerate their codes.
- "Trust this device for 30 days" genuinely did nothing — the checkbox was not sending a value the
  server could read, so ticking it recorded nothing and you were challenged every time. Fixed.

When you re-test, note the trust box only takes effect from the *next* sign-in after ticking it, and
it needs an account that can receive the emailed code. If resetting your own setup is inconvenient we
can walk it through on a fresh test account.

### 5. Search taking 10–15 seconds (#259)

Fixed, and you were right that it started with the previous release. The multi-country change added a
country lookup that was being re-run once for every row the search scanned rather than once per
search — which is why it was worst on terms that matched *few* certificates, since those scan the
most rows before finishing. It is now a single lookup, and both public and internal search are back
under a second.

Related and also live: searching a year prefix such as "23" now puts the 23-xxxx certificates first
instead of burying them several pages in.

### 6. Product types and the "Carly Test" entries (#170)

Closed on both halves. The Product Types page now shows product *and* certificate usage in one
column, so a type referenced by certificates no longer reads 0, and the delete check names the real
blocker and its count instead of a generic error. Separately, the "Carly Test" test records were
removed on 8/4 — the category, the "Test Type" product type and four test products — with nothing
left attached to any of them.

One deliberate exception: there is also a **company** named "Carly Test" still in the system. It was
not part of your request, so we left it. Say the word and we will remove it.

### 7. Duo as a two-factor option (#151)

No change, and not open work. Duo would be a configuration change in the identity tenant rather than
application code, and before it can be scoped we need to know which factor you want (push, passcode,
or both) and whether it replaces Microsoft Authenticator or sits alongside it. It is not blocking
go-live — Microsoft Authenticator is supported and working today. We suggest moving this to the Phase
2 list so it stops reading as an open defect.

### What we need from you

1. **Re-test all six live items** — everything above except #151.
2. **#151** — a decision on Duo whenever you want it scoped, or agreement to park it.
3. **The "Carly Test" company** (#170) — remove it or leave it?
4. Optional: say the word and we will sweep the 67 rows that are complete but still read "Open".

One heads-up on the Amazon side: Amazon now begins receiving 19 Confidential certificates it has not
seen before. The format of what we send them is unchanged — same fields, more records — so nothing
needs to change on their end, but you may want to give their team notice.

---

## Internal notes (do NOT paste)

- **Deployed 2026-09-08.** Bundle `GCCWebApp-deploy-20260908-1607.zip` (CR-043, CR-045, **DEF-412**).
  DB applied ~16:20 EDT, code ~17:0x EDT. Runbook + full results:
  `docs/tracking/2026-09-08-deploy-runbook-CR043-CR045.md`.
- **17 of 18 live checks pass.** The failure is `CR043.C2`, recorded as INCONCLUSIVE on purpose:
  Novi has **0 tagged certificates** on test, so its isolation control returns an empty set and
  proves nothing. `CR043.C3` (Sika: 23 visible, 0 of the 13) carries that assertion instead.
- **Client-facing numbers used above, all verified live, not estimated:** 13 certs / 420 identifier
  rows / 19 Amazon-tagged Confidential certs / 1,103 populated Certification Results
  (1,072 `%` + 31 non-numeric) / 0 changed values / 1,019 → 1,061 on the active-certificates report
  (1,019 + 42 = 1,061, reconciled against the database).
- **Do not volunteer DEF-412** (the `Cert Subcategories` export column) — it shipped in the same
  bundle but was not on this QC log and has its own thread.
- **Still open on our side:** drop the two rollback tables when satisfied
  (`CertProductIdStatus_bak_CR043_20260908`, `CertResults_bak_CR045_20260908`); close
  `docs/qa/gap-report.md` G12.

# Deployment summary — 25 August 2026

**Bundle:** `GCCWebApp-deploy-20260825-1448.zip` (61.1 MB)
**Target:** the test environment (gccwebapp.com)
**Database changes:** none. This release contains no schema migration.

---

## 1. The headline fix — certificates that share a number

**What was wrong.** Several certificates are allowed to share one certificate number — 137 numbers
are shared across 312 certificates in the current data. The application addressed a certificate by
its number alone, so where a number was shared it could only ever reach **one** of them. The other
**175 certificates had no working address at all**.

This was not confined to one screen. Seven separate places behaved this way, and three of them
matter a great deal:

| Where | What actually happened |
|---|---|
| Downloading a certificate | **You received a different certificate's PDF.** This is the symptom that was reported. |
| Opening a certificate to edit it | **The wrong record opened**, so a correction typed against one product was saved onto another. |
| Deleting a certificate | **The wrong certificate would have been permanently destroyed.** There is no undo for a deletion, and the audit trail would have recorded the wrong record. |

The deletion case had never been reported, because nobody had yet tried to delete one of the 137
shared numbers. It was the most serious of the three.

**What was changed.** Every one of those seven places now accepts the specific certificate alongside
the number. Three points are worth stating plainly, because they were deliberate decisions:

- **Existing links keep working.** The address is still the certificate number. Anything already
  shared, bookmarked or published continues to resolve exactly as before, and for the ~5,390
  certificates whose number is unique nothing about the address changes at all.
- **A mismatch is refused, not guessed.** If the certificate identifier does not match the number,
  the page reports "not found" rather than quietly falling back to a different certificate. Falling
  back is precisely the fault being fixed, so it is now impossible by design.
- **The refusal cannot be used to reach data.** The number must still match, so the identifier
  cannot be used on its own to open a certificate that was not asked for.

**A second fault was found during verification, after the fix was thought to be complete.** One of
the three download routes accepted the certificate identifier and then discarded it at the final
step, so it continued to return the same PDF for every certificate sharing a number — while
returning a perfectly valid response every time. Nothing about the outward behaviour indicated a
problem: the correct status, a real PDF, no error. It was caught only by downloading all four
certificates that share one number and comparing the files to each other. That has been fixed and
re-tested the same way.

---

## 2. Also carried in this bundle

These were fixed earlier and are recorded as not yet delivered to the client. They are included
here. Where a fix's delivery state could not be confirmed mechanically, that is noted rather than
assumed.

| Item | What it fixes |
|---|---|
| DEF-341 | The expired-certificate alert used a different dialog style from every other popup in the application. |
| DEF-342 | The separator hint was added to the certificate import template, but the screen in use was the *Product Identifiers* template, which still lacked it. |
| DEF-343 | Certification Type appeared blank when editing a certificate whose type is a sub-type — and the field then silently rewrote itself on save. |
| DEF-344 | The certificate detail and preview screens showed only the first partner when a certificate had two. |
| DEF-345 | Partner API rate limiting never took effect: the limit was applied before the partner key was recognised, so partner traffic was throttled as anonymous traffic. |
| DEF-346 | The Product ID export separated partners with a pipe character under a heading instructing the reader to use commas. |
| DEF-358 | Certificates could appear publicly before their effective date; the public and partner checks tested status and expiration but never the effective date. |
| DEF-368 | The certificate PDF size limit was 20 MB on one upload route and 10 MB on all the others. |

> **Caveat, stated deliberately.** DEF-341, DEF-345, DEF-346, DEF-358 and DEF-368 are listed as
> undelivered based on the tracking records, not on an inspection of the live site. DEF-358 in
> particular appears in a recent commit message and may already be live. The bundle contains the
> current state of all of them either way, so shipping it is correct regardless — but this table
> should not be read to the client as "eight new fixes" without confirming which were already
> delivered.

---

## 3. Known issue found this cycle and deliberately **not** fixed

**DEF-372 — the delete confirmation page returns an error for every certificate.** The page it tries
to display does not exist. This is long-standing and unrelated to the work above.

It has no effect on anyone using the application normally: all three delete buttons use the
confirmation dialog and never open that page. It is reachable only by typing the address directly or
following an old bookmark, and only by an administrator. It was left alone rather than fixed inside
an unrelated change; the fix is small and needs a decision on whether to restore the page or remove
the unused route.

---

## 4. Why the confidence in this release is higher than usual

Three faults were found **in the release-checking tools themselves**, each of which was reporting
something untrue about the application. None changes what the software does; all three change
whether we can trust a green result.

- The Amazon partner-contract check reported **"CONTRACT CHANGED"** after a slow response, having
  compared nothing at all. That is the alarm that would stop a release and prompt a partner
  conversation, produced by a request that never completed. A check that cannot reach its subject
  now reports *inconclusive* instead of voting.
- The write-path suite reported **five broken save/delete paths**. It had failed while reading a
  configuration file and never ran a single test. The write paths were never broken — and this had
  been the case, unnoticed, for twelve days.
- The static release check **blocked this release** over a value it claimed was never read. It is
  read; the check could not see the way it was read.

All three are fixed and re-run clean. The pattern is the same in each: a tool with no way to say
"I could not run" turned its own breakage into a confident, specific finding about the code.

---

## 5. Verification performed

| Check | Result |
|---|---|
| Release build | 0 errors |
| Certificate-sharing fix, end-to-end on a live instance | **55 checks, 0 failures** |
| Unit tests for the sharing fix | 9 of 9 pass |
| Amazon partner API contract — local build **and** live test site | **Unchanged** on both (7 of 7) |
| Save/update/delete paths (certificate, tag, company, product, user) | **39 of 39 pass** |
| Access control — roles, direct-link access, detail-screen scoping | **36 pass, 0 failures** |
| Bundle contents | No development configuration, encryption keys, uploads, scripts or documentation included |

Each check on the certificate-sharing fix names both the certificate it expects **and** the ones it
must not see. That distinction is the point: "the page loaded" and "the download succeeded" were
both true throughout the period when the wrong certificate was being served.

---

## 6. Outstanding before and after upload

1. **Back up the server's uploaded files before extracting the bundle.** Extraction can overwrite
   stored certificate PDFs; this has happened before.
2. **Five partner-isolation checks could not run locally** — the partner test accounts do not exist
   in the development database. Zero failures is not the same as verified; these need either the
   test data restored or an explicit, recorded waiver.
3. **After upload:** confirm outbound email is live and send one real password reset. Email failure
   is silent — the system reports success while delivering nothing, and its own connection test
   always reports healthy.
4. **After upload:** run the delivery close-out so the tracking records reflect what actually
   shipped, and fold the items above into `shipped-2026-08.md`.

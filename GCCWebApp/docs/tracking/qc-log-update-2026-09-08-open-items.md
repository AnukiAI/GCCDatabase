# QC Log update — 2026-09-08 (in-cell, paste-ready) — all genuinely open items

**Source workbook:** `Database QC Log_9.3.26_sw_updates.xlsx`, sheet **`GCC DB QC Log2`**, header row 5.

**Column map for this workbook** (it has shifted since the July version — check before pasting):

| Col | Header | Who fills it |
|-----|--------|--------------|
| **J** | `Sprint 3 6/23 Notes` | **Anuki** — this is where the cells below go |
| **O** | `Date Closed - GCC Confirmed Fixed` | **Client** — do not fill this in for them |
| **Q** | `Current Status - Anuki Marks Complete` | **Anuki** |

---

## Triage — 263 data rows, 7 genuinely open

Column Q reads `Open` on 74 rows, but **67 of those already carry a real close date in column O** —
stale bookkeeping, no work. Filtering on Q alone overstates the backlog by roughly ten to one.

The **7 real ones**, identified by column O reading as text rather than a date:

| QC # | Row | Page | Verdict | Backing record |
|------|-----|------|---------|----------------|
| **150** | 155 | Sign In / Forgot Password | ✅ **Fixed + deployed 9/4** — all 3 sub-items | DEF-405, DEF-406, DEF-407 |
| **151** | 156 | 2FA — Duo | ⏸ **Phase 2 by agreement** — no work planned | CR (unscoped) |
| **170** | 175 | Product Type classification | ✅ **Fixed (code) + data removed 8/4** | DEF-196, DEF-197, CR-019 |
| **251** | 260 | Cert Results field | ✅ **DEPLOYED 9/8** — verified live | CR-042 (data), CR-045 (code) |
| **252** | 261 | Product Identifier export | ✅ **DEPLOYED 9/8** — verified live | CR-043 |
| **257** | 266 | MS Auth setup | ✅ **Both parts fixed + deployed** (8/31 and 9/8) | DEF-384, DEF-410 |
| **259** | 268 | Search loading time | ✅ **Fixed + deployed 9/4** | DEF-402 (+ DEF-409, 9/5) |

**All 7 are now resolved or deliberately parked, and everything is LIVE on gccwebapp.com.** The two
approved change requests (CR-043, CR-045) were built *and deployed* on 2026-09-08 and verified on the
live site. Nothing on this log is waiting on us.

**Six items are ready for the client to re-test.** #251 and #252 stay `Open` in column Q only until
they confirm — the work is done. #151 (Duo) needs a scoping decision whenever they want it.

---

## QC #150 — Forgot password (row 155)

**Column Q:** `Complete` · **Column J:**

```
9/8: All three items from your 9/3 note are fixed and live on gccwebapp.com.
1. Expiry mismatch - the page said 24 hours, the email said 1 hour. Both now say 1 hour, which is what the system actually enforces.
2. Reset screen background - Forgot Password and Reset Password now use the same lighter green as Sign In, so the logo no longer blends into it.
3. "Access your sustainability certification database." removed from the Forgot Password screens.

ANSWERS TO YOUR QUESTIONS
- Reset links: the link is built from whatever domain the site is served on, so it will point at the production URL automatically at go-live. No code change and nothing to remember.
- Sender address: currently admin@gccwebapp.com. This is configuration, not code, and will be changed at go-live - but the new mailbox or alias has to exist on the production domain first. Flagging it as a go-live task on the email side.
- Spam folder: noted, and expected to improve once mail sends from the production domain with its own SPF/DKIM records rather than the test domain.

Ready for retest.
```

---

## QC #151 — Duo as a 2FA option (row 156)

**Column Q:** `Phase 2` · **Column J:**

```
9/8: No change - parked as Phase 2 by agreement, not open work.

Duo would be a configuration change in the identity tenant rather than application code. Before it can be scoped or priced we need a decision on which factor you want (push, passcode, both) and whether it replaces or sits alongside MS Authenticator. Not blocking go-live; MS Authenticator is supported and working today.

Suggest moving this to the Phase 2 list so it stops reading as an open defect on this log.
```

---

## QC #170 — Product type classification / "Carly Test" (row 175)

**Column Q:** `Complete` · **Column J:**

```
9/8: Closed on both halves.

CODE: the Product Types page now shows both product and certificate usage in one column ("Usage (Products / Certs)"), so a type that is referenced by certificates no longer reads 0. The delete pre-check queries the certificate references too and names the real blocker and its count instead of returning a generic error. Deployed.

DATA: the "Carly Test" test records were removed from the test environment on 8/4/26 - the product category "Carly Test", the product type "Test Type" and 4 test products. 0 products and 0 certificates remain attached to any of them.

ONE THING DELIBERATELY LEFT: there is also a COMPANY named "Carly Test" still in the system. It was not part of your request, so we did not touch it. Tell us if you want it removed as well.

The count-accuracy follow-up you moved to ID 186 was closed 6/17/26.

Ready to mark complete.
```

---

## QC #251 — Cert Results field (row 260) — **DEPLOYED, ready for retest**

**Column Q:** `Open` (until the client confirms) · **Column J:**

```
9/8: Both halves are now done and live on gccwebapp.com. Ready for you to re-test.

THE DATA (9/2): every existing certificate's Certification Results value was normalised to the explicit XX% form you corrected the Amazon certs to. 803 certificates; no bare decimals left anywhere in the database. What is stored is now exactly what is published, so the records we imported are consistent with your own corrections.

THE FIELD (9/8): Certification Results is now a true free text field. Whatever is typed is what publishes - on screen, in the CSV exports, and to Amazon - with no interpretation at all. The old behaviour converted a bare number on the way out (0.5 became 50%, 85 became 85%); that conversion has been removed entirely rather than switched off, so it cannot creep back in on one screen and not another.

WHAT THIS CHANGED FOR YOUR EXISTING DATA: nothing, and we measured it rather than assuming. 1,103 certificates carry a value - 1,072 already in XX% form and 31 non-numeric ("Pass", "Silver" and similar). We compared every certificate before and after: zero displayed values changed and zero sort positions changed. That is only true because the data pass came first; doing these in the other order would have rewritten 803 published values.

We also moved the hidden column that drives sorting in the same release. If we had changed what you see without changing what the system sorts by, a future value would display one way and sort another.

ONE TRADE-OFF TO BE AWARE OF: from now on a mistyped 0.23 will publish as "0.23" rather than being quietly corrected to 23%. That is the intent of "whatever is inputted is the value we want" - flagging it so it is not a surprise. If you would like a gentle warning when someone types a bare decimal (a prompt, not a block), say so and we will scope it separately.

FOR FUTURE LOADS: your source spreadsheets format these cells as 0%, so the cell displays 23% while holding 0.23. Every load from here reads the displayed value.
```

## QC #252 — Confidential certs in PI export / Amazon API (row 261) — **DEPLOYED, ready for retest**

**Column Q:** `Open` (until the client confirms) · **Column J:**

```
9/8: Built and deployed. Ready for you to re-test.

WHAT IS NOW TRUE: Client Approved - Paid, Client Approved - Confidential and Republished all appear in the PI export and are returned by the partner API, provided today is inside the certificate's effective/expiration window and the certificate is tagged to that partner. Your two answers were built exactly as given - a company signed in to its own account now sees its own Confidential certificates, and Confidential counts as Active in the internal dashboard counts and their exports while it is inside the window.

THE PUBLIC DATABASE IS UNCHANGED, and we tested that specifically after the deploy rather than assuming it: every Confidential certificate still returns "not found" to an anonymous visitor, while a normal public certificate still loads. Confidential now means hidden from the public database - and only that.

VERIFIED LIVE: 13 Confidential certificates carry product identifiers - the 11 you listed plus two that came in with a later load (26-0473, 26-0474). Between them they add 420 product identifier rows to the export, and all 13 are tagged to Amazon, so all 13 are now visible to Amazon. We also checked the reverse - that another company's account sees its own certificates and none of these 13 - because widening a status rule must never widen who can see what.

ONE THING WE FIXED THAT YOU HAD NOT SEEN YET: the product identifiers on those certificates were individually marked "Inactive", because that status is copied from the parent certificate at the time it is saved. They would have appeared in a file titled "Active Product Identifiers" while reading Inactive on the Product Identifiers screen. We corrected the stored values as part of this change.

ONE NUMBER WILL LOOK DIFFERENT, AND YOU SHOULD HEAR IT FROM US: the internal "active certificates" report now returns 1,061 certificates instead of 1,019. The difference is exactly the 42 Confidential certificates inside their date window - the ones your "yes" on the second question brought into the internal Active count. Nothing was added or changed in the data; the report is counting what you asked it to count.

PLEASE READ THIS ONE BEFORE YOU RE-TEST: 23-0758 is NOT Confidential. It is Expired, expiration 8/31/26. It left the export when it expired, which is correct, and it is still absent now. If it has been renewed it needs a renewal certificate or corrected dates. This is the second time it has come up, so we want it said plainly rather than referred back to.

ALSO WORTH KNOWING: the 20 Confidential certificates with no partner tag carry no product identifiers either, so they reach no partner and add nothing to the export. If any of those should be badged, that is a separate data task - tell us which ones.

HEADS-UP FOR AMAZON: they now begin receiving 19 Confidential certificates they have not seen before. The format of what we send is unchanged - same fields, more records - so nothing needs to change on their end, but their team may want notice.
```

## QC #257 — MS Auth setup (row 266)

**Column Q:** `Complete` · **Column J:**

```
9/8: Both parts fixed and live.

1. COPY / DOWNLOAD RECOVERY CODES - you were right, they were dropping half of every code (producing "A1B2-" instead of "A1B2-C3D4"). The on-screen codes were always correct, which is why it went unnoticed. Fixed and deployed 8/31/26. Anyone who saved a downloaded copy before that date has an unusable file and should regenerate their codes.

2. "TRUST THIS DEVICE FOR 30 DAYS" - also right. The checkbox was not sending a value the server could read, so ticking it recorded nothing and you were challenged every time. Fixed and deployed 9/8/26.

FOR RETEST: the trust box only takes effect from the NEXT sign-in after you tick it, and retesting needs an account that can receive the emailed code. If resetting your own setup is inconvenient we can walk it through on a fresh test account instead.
```

---

## QC #259 — Search loading time (row 268)

**Column Q:** `Complete` · **Column J:**

```
9/8: Fixed and deployed 9/4 - and you were right that it started with the last push.

CAUSE: the multi-country change in that release added a country lookup that was being re-run once for every row the search scanned, rather than once for the search. That is why it was worst on terms matching few certificates - the fewer the matches, the more rows it scanned first. Rewritten as a single join.

Internal and public search are both back to well under a second.

ALSO DEPLOYED 9/5: searching a year prefix such as "23" now shows 23-xxxx certificates first instead of burying them several pages in.

Ready for retest.
```

---

## Housekeeping (optional, one pass)

**67 rows read `Open` in column Q but hold a real close date in column O.** Flipping those to
`Complete` would make the log's own status column trustworthy again. IDs:

```
50, 104, 118, 148, 159, 167, 171-184, 187-190, 192-203, 217, 218, 220-224, 226, 229, 230,
232, 235, 237-250, 253-256, 258
```

Two rows (185, 186) have a close date and a **blank** column Q, and IDs **197, 238 and 242** each
appear on two unrelated rows — do not key any sweep on ID alone.

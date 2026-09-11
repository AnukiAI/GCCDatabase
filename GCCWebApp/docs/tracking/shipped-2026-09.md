# Shipped — September 2026

Stakeholder-readable log of what reached the test environment (gccwebapp.com) and when.

---

## 2026-09-02 — session timeout, query performance, deploy tooling, multi-country certificates

Deployed to gccwebapp.com. Bundle `GCCWebApp-deploy-20260902-full.zip`
(md5 `de72254e3fd9c42f4f56c0d035314675`), built from a full clean rebuild.

### Fixed

**Sessions were timing out early, and the warning was unreliable (DEF-390).**
The reported symptom was "the 30 minute timeout is not always followed, tends to be shorter, and
sometimes I get the alert and sometimes not." Both halves were real. The application held two
independent 30-minute clocks — one counting from the last page request, one from the last mouse
movement — and they never agreed. Because the session cookie is only renewed once more than half
its life has passed, a user could be signed out **anywhere between 15 and 30 minutes**, not 30.
The warning was driven by the second clock, so it often never appeared: it stayed silent while
someone read a page without clicking, and browsers suspend it entirely in a background tab.

Real activity now keeps the session alive on the server, and the countdown is measured against the
clock rather than a timer the browser can suspend — so a backgrounded tab reaches the right answer
the moment you return to it. "Remember me" is unaffected.

**Certificate pages could return far more data than needed (DEF-394).** A preventive change: one
detail-page query was fetching the combination of six related lists at once. Harmless at today's
data volumes, but the cost multiplies rather than adds as certificates gain more tags, countries and
product IDs — and multi-country support has just added another. Split into separate queries.
No visible change.

**A deployment setting silently did nothing (DEF-395).** The option controlling where the
application stores its encryption keys had been overridden internally, so setting it could only ever
crash startup and never actually move the keys — the cause of the 2026-08-18 outage. The setting now
works, and an unusable value falls back safely with a clear error instead of taking the site down.

### Delivered by the parallel work stream

**Multi-country certificates (CR-037).** Certificates can now hold more than one country. 1,327
existing certificates were migrated to the new structure.

**Certificate edits now write an audit trail (DEF-391).**

Also included: certification-results normalisation (CR-042), the Sofidel and "not-loaded 29"
certificate loads, and DEF-396/397.

### Testing

The maintained security suite — partner isolation, role permissions, cross-company access, download
protection, and a new check for the session fix — passes **65 of 65** with no failures.

### Known follow-ups

- Compound-country certificates (about 15) still hold their countries as a single combined string
  and need a separate, deliberate data change; it alters what the CSV and Excel exports show, so it
  is being scheduled on its own rather than bundled here.
- ~~End-to-end browser confirmation of the session-timeout fix.~~ **Done 2026-09-02** — a full
  36-minute cycle was run against the live test environment, covering four situations at once:
  sitting idle, working without changing page, leaving the tab in the background, and staying signed
  in with "remember me". All eight checks passed. The warning now appears at 25 minutes and sign-out
  follows at 30, in that order; and someone reading a page without clicking through to another one
  now stays signed in, which is the situation that previously logged people out with no warning.
- An index created by the multi-country migration is wider than SQL Server can safely guarantee
  (DEF-398). No effect on current data; queued for a follow-up.

### Incident during deploy

The application went out before its database migration, and certificate pages returned errors until
the migration was applied — the new code expects a table the migration creates. Detected and
corrected the same session. **No data was lost or altered:** certificate count was 5,731 before and
after, and a full backup was taken beforehand. The deploy runbook now states the required order
explicitly.

---

## 2026-09-03 — September certificate and ASIN load

Loaded to gccwebapp.com. A full backup of the database was taken first, and the whole load was
rehearsed against a separate copy before it touched the test environment.

### Loaded

**193 certificates and 1,870 Amazon ASINs.** From the 3 September files: 191 certificates from the
upload sheet, 1,870 product identifiers mapped onto 12 certificates, 9 new companies and 125 new
products. Certificate totals moved from 5,731 to 5,924.

**92 of the new certificates are Drafts**, as expected — the team has stopped using the old database,
so drafts came across too. Drafts are not visible to the public or to partners. **96 of the new
certificates are publicly visible today**; the rest are Draft, Confidential, or not yet within their
effective-to-expiration window.

**Certificates with more than one country now carry all of them.** This was the reported problem
with the previous load, and it is fixed for everything in this one: 10 certificates in this batch
list several countries (for example "United States, China, Poland"), and each country is now held
separately so it works in filters and search rather than being one combined string.

**Product category, type and subcategory no longer carry the spreadsheet's outline numbers.**
"4.3 Cleaning Supplies" loads as "Cleaning Supplies". "N/A" is preserved as a real option and is used
by 18 certificates that are not product-level.

**Product subcategory is populated for the first time** — 20 certificates now carry one (HDPE, PET,
PP, PS, PVC, TPO). Previous loads left this blank.

### Two items that needed a decision

**Three GAF certificates were renewals, not new records.** 25-0497, 25-8163 and 25-8164 already
existed and had expired. The sheet carried them with new dates and an approved status. Confirmed
against your own production database — which shows all three with the renewed dates — and updated in
place. Only the status, dates and location changed; company, product, certification type and the
attached certificate PDF are untouched.

**Two Philips certificates (26-0438, 26-0439) were missing from the sheet.** 1,500 of the 1,909 ASIN
rows — 79% of the file — were assigned to them, so without those two records most of the ASIN file
could not be loaded. Both certificates are live on your production database, and their records were
rebuilt from it field by field. Everything came from your own systems; nothing was invented.

### Checked after loading

Every certificate was compared back to the spreadsheet field by field — company, product,
certification type, status, dates, location, country, results, category, type, subcategory and
pathway — with no differences. All 1,870 ASINs are present, each attached to the right certificate
and correctly scoped to Amazon.

The Amazon API was re-checked against its April baseline and returns **exactly the same structure**.
Amazon now receives 1,870 more product identifiers; nothing about the format they consume changed.

### Known follow-ups

- **Search is slow, and we can reproduce what you reported.** Typing a certificate number into the
  internal search takes about 8 seconds, and a term matching nothing takes about 10 — matching your
  "10-12 seconds" exactly. The pattern is backwards from what you would expect: the *fewer* results a
  term has, the *longer* it takes. Autocomplete is unaffected (under 0.2s) and pages load in about
  half a second, so this is one specific query rather than the site being generally slow. It is a
  regression against 31 August. Two straightforward explanations were tested the same day and both
  ruled out, so a code change is needed rather than a configuration one. The most valuable single fix
  is to search certificate numbers from the start of the number rather than anywhere inside it, which
  is both the slowest case today and the one where matching from the start is the natural behaviour —
  it would mean typing `0636` no longer finds `26-0636`, so we would like your confirmation before
  making that change.
- ~~None of the 193 new certificates have a certificate PDF attached.~~ **Done the same day** —
  see below.
- **Two certificates (26-0532, 26-0631) arrived with no country** and will not appear under any
  country filter until one is supplied.
- ~~15 older certificates from the August load still hold their countries as one combined string.~~
  **Fixed the same day** — see below.

### Certificate PDFs attached — all 96

Every one of the 193 new certificates arrived without a certificate document, because none came with
the files. 96 of them — the publicly visible ones — were published on your own production database,
so we fetched those and attached them the same day.

All 96 are now on the test site with a working preview image and a working download. Each document
was checked to be a real PDF rather than an error page, and each one served back from the test site
is **byte-for-byte identical** to what your production database served. No two certificates ended up
sharing a document or a preview — checked across all 5,924 certificates, not just the new ones.

The two rebuilt Philips certificates are confirmed by their own documents: 26-0438's certificate
reads "Philips, Drachten, The Netherlands — Grooming — Sustainable Energy Practices — 100% Renewable
Electricity Usage — Certification Number 26-0438 — July 1, 2026 to July 7, 2027", matching every
detail we reconstructed from your production site.

The remaining 97 certificates have no document because they are Drafts, Confidential, or not yet in
their date window, and so are not published anywhere we can retrieve them from. If you want
documents on those, they will need to come from the team.

### Multi-country certificates from the August load — 15 still need fixing

You asked whether we had checked the earlier load against the August spreadsheet. We had not, so we
did. Reading every country in that file and comparing it to what the database holds:

- **17 certificates in the August file list more than one country.**
- **2 are correct** — 23-0925 and 23-0926, the two you fixed by hand.
- **15 are still wrong.** They hold the whole thing as a single value, e.g. "United States; China;
  Poland" stored as if it were one country's name. It matches no filter, no country picker and no
  search, and shows up as a country of its own.

  They are 25-8246 through 25-8253 (eight, expired), 25-8767/8768/8769/8770/8772/8774 (six, live and
  approved), and 25-0433 (one, expired).

**Fixed on your approval, same day.** All 15 were split so that each country is held separately:
38 country entries added, 15 combined entries removed. Re-checked afterwards against the August
spreadsheet from scratch — **all 17 multi-country certificates are now correct**, none lost, none
duplicated, and no combined values remain anywhere in the database. On the page, 25-8767 now reads
"United States, Canada" and 26-0636 reads "United States, China, Poland".

**On the semicolons you spotted:** those were never a formatting choice. A semicolon in a country
field only ever meant that record was one of the 15 unfixed ones — the August spreadsheet used
semicolons in some rows and commas in others, and the earlier load stored the text exactly as
typed. The site has always joined multiple countries with commas; now that each country is stored
separately, that is what you see. Semicolons in the **Location** field are correct and were left
alone: 301 certificates list several sites there, and each site already contains a comma
("Athens, GA; Chowchilla, CA").

**One consequence to be aware of**, as flagged before we ran it: the CSV and Excel exports read the
single "primary" country field, so those 15 now export as "United States" rather than the combined
string. Making the exports list every country is a code change that is still outstanding.

### The one missing certificate preview

You spotted that 5,570 certificates have a document but only 5,569 have a preview image. It is a
single certificate, **23-0971**, and it is not from this load — it came across in August.

Its PDF is password-protected. The preview is made by rendering the first page of the document,
which cannot be done on an encrypted file, so no preview image was produced. The document itself is
completely fine and downloads normally; the page simply shows a generic certificate graphic where
the thumbnail would be. Every one of the 96 documents attached today produced a proper preview.

If you would like a real preview on it, the simplest route is an unencrypted copy of that
certificate from the team and we will re-attach it. Alternatively we can make the preview step try
to open permission-protected PDFs, which would likely cover this one and any future ones — a small
code change.

---

## 2026-09-04 — search performance, exports, certificate preview, password-reset screens

Deployed to the test environment on 2026-09-04 (`GCCWebApp-deploy-20260904-1750.zip`). **No database
change** — the schema was already current, so nothing was migrated or altered.

### Fixed

| # | What you will see |
|---|---|
| DEF-402 | **Certificate search is fast again.** It had slowed to 8-10 seconds after the September load; the country lookup added by multi-country certificates was being re-run for every row. |
| DEF-404 | **Filtering by country and exporting now returns the same certificates the list showed you.** Filtering by Canada listed 115 certificates but exported 107 — the eight missing ones were those where Canada is a secondary country. Confirmed after deploy: 115 and 115. |
| DEF-403 | **Three dashboard exports were sorted by the wrong column**, and two of them ("Product Types" and "Products") produced identical files apart from the title. Each now sorts by its own subject. |
| DEF-401 | **Analysts can now replace a certificate's preview image from the Edit screen.** Previously, if a PDF could not be rendered — an encrypted or corrupt file — the preview could only be fixed by a developer. |
| DEF-405 | The password-reset page said links expire in 24 hours while the email said 1 hour. Both now say **1 hour**, which is what the system actually does. |
| DEF-406 | The Forgot Password and Reset Password screens used a darker green than the sign-in screen. All auth screens now match. |
| DEF-407 | Removed "Access your sustainability certification database." from the Forgot Password screens, as requested. |

### Checked after deploying

- **Amazon data feed unchanged** — the API contract was compared against the April baseline: identical structure, 7 of 7 checks passed. Nothing changes for Amazon.
- Certificate, tag, company, product and user create/edit/delete all verified working (39 checks).
- Role separation re-verified: each account type still sees only its own certificates.
- The DEF-404 fix was confirmed on the live site, not just in testing.
- The new preview-image field was checked at five screen sizes, from phone to desktop.

### Known follow-ups

- ~~**DEF-409 — searching a year prefix shows the wrong certificates first.**~~ **Fixed and deployed
  2026-09-05** — see the entry below.
- **DEF-403 has no automated post-deploy check**, unlike the others. Its fix changes only sort order
  inside a query, which leaves no trace we can assert against the shipped file. Verified by running
  the six exports and confirming no two come back identical.
- The Create Certificate screen still has no preview-image field; DEF-401 fixed Edit only.

---

## 2026-09-05 — search results now put the certificate you asked for first

Deployed to the test environment on 2026-09-05 (`GCCWebApp-deploy-20260905-1301.zip`). **No database
change.** One fix.

### Fixed

**DEF-409 — searching a certificate number now shows matching certificates first.**

Searching `23` used to return 1,219 results with the **first 23-xxxx certificate on page 4**. The
results were never wrong — 1,075 of them genuinely were 2023 certificates — but newer certificates
whose *sequence* happens to contain "23" (26-0623, 26-0523) sorted above them, so the first three
pages looked like nonsense.

Certificates whose **number begins with what you typed** now come first. Your chosen sort still
applies within that group, and **nothing has stopped being findable** — 26-0623 is still returned by
a search for "23", just below the 2023 certificates. Typing the dash (`23-`) now works too; it
previously made no difference at all.

The same ordering was applied everywhere search runs: the internal, external and public certificate
views, both search-suggestion dropdowns, and the CSV export — so a filtered list and its export now
come out in the same order.

### Checked after deploying

- Page 1 of a "23" search is now entirely 23-xxxx; the result **count is unchanged at 1,219**, which
  is the important part — the change reorders results, it never hides any.
- Five pages walked with no certificate repeated or skipped.
- Search suggestions for "23" now offer 23-0001 to 23-0005, where they previously offered whichever
  five rows the database happened to return.
- Unsearched lists, and searches by company or product name, are unaffected — verified explicitly.
- **Amazon data feed unchanged** (7 of 7 checks, identical structure to the April baseline).
- Role separation, certificate/company/product/user editing, and outbound email all re-verified.

### Also improved, quietly

Two places previously returned rows in **no defined order at all** — an internal search method and
the CSV export — meaning the same export could come out in a different order twice. Both are now
deterministic.

### Known follow-ups

- Two smaller search questions were deliberately left as they are, pending a decision: whether
  company/product names should also rank by prefix, and whether searching only the sequence part of
  a number (`0971`) should rank.
- The Create Certificate screen still has no preview-image field; DEF-401 fixed Edit only.



---

## 2026-09-08 — September 4th certificate load (88 missing records)

The client identified 88 certificates that were missing from the previous loads and sent them in
`GCC Database Certs for Upload_9.4.26.xlsx`. All 88 are now in the test environment, with their
certificate PDFs attached where the client has published them.

### What was loaded

| | |
|---|---|
| Certificates | **88** — all new, none were already present |
| Companies created | **2** — McKinney (ASSA ABLOY) and Sylvamo North America |
| Products created | 41 |
| Country records | 88 |
| Tag links | 48 (LEED) |
| Amazon associations | 26 |
| Certificate PDFs attached | **51** |
| Database total | 5,924 → **6,012** certificates |

By status: 51 Client Approved - Paid, 36 Draft, 1 Client Approved - Confidential. The 51 paid
certificates are publicly visible today; the other 37 are not, which is correct for their status.

### Checked after loading

Every field of all 88 records was compared back against the client's spreadsheet — company,
product, certification type, status, both dates, location, country, results, category, type,
subcategory and pathway — with **no mismatches**. On top of that:

- **Both new company names came through with their accented and trademark characters intact**, and
  each was created exactly once rather than duplicating an existing record. The sheet's 11 SARGENT
  certificates correctly attached to the existing "Sargent Manufacturing Company" record instead of
  creating a second one that differs only in capitals.
- **The 14 certificates carrying a certification result read 100%, not 1%.** In the client's
  spreadsheet these cells hold the value 1 formatted as a percentage — they display as 100% and
  must be stored that way. This is the same trap that CR-042/045 spent a migration correcting, and
  it is checked explicitly.
- **All 88 certificates are reachable by country filter** (a certificate without its country record
  is invisible to that filter — the problem the client reported in August).
- **All 51 PDFs were verified byte-for-byte** against the copies on the client's own public site
  after upload, previews generated, and no two certificates share a document.

### Known follow-ups

- **The 26 Amazon certificates have no ASINs yet.** No ASIN list covering these 88 was included in
  the drop — the ASIN file in the folder is the previous one and shares no certificate numbers with
  this batch. The certificates reach the Amazon feed; they carry no product-level data until the
  client sends a covering list. **This needs a request to the client.**
- **37 certificates have no PDF**, because 36 are Draft and 1 is Confidential and therefore are not
  published on the client's public site. They can be attached when the client supplies the files or
  publishes the certificates.
- **Certificate subcategories were not loaded** — the sheet supplies 140 of them, but that field is
  empty for all 6,012 certificates and filling it for only the newest 88 would look like a fault
  rather than a fix. Logged as DEF-411 to be fixed in one pass across the whole database.


---

## 2026-09-08 — Certificate subcategories now populated (they never were)

The **Cert Subcategories** column was blank for every certificate in the database — not for some,
for all 6,012 — because no data load had ever filled it, going back to the first migration. The
client had been supplying the values in the upload workbooks the whole time. They are now loaded.

### What changed

| | |
|---|---|
| Certificate–subcategory entries loaded | **456** |
| Certificates now showing subcategories | **227** |
| New subcategory options added to the system | **6** |
| Previously | **0 across the whole database** |

Loaded on the dev environment first, verified, then applied to the test environment and verified
again.

### Checked after loading

- **Every entry matched back to the client's spreadsheets**, compared in both directions — nothing
  in the database that isn't in their files, nothing in their files that didn't load. No
  duplicates.
- **Confirmed through the application, not just the database**: the certificate list now returns
  the subcategories for the certificates that have them, so the column renders and sorts.
- **Every entry was checked for durability.** This is the part worth knowing about: the database
  would have accepted all 456 regardless, but 75 of them would have been **silently erased the first
  time an analyst saved that certificate**, because the certificate's type didn't offer that
  subcategory as a valid choice. Those 75 are exactly the ones that needed the six new options
  created first. All 456 are now verified as durable.

### What the six new options are

Five of them already existed in the system — they were simply unavailable under the certificate
type the client was using them with. Only one, **"Zero Waste to Landfill"**, is genuinely new.
They appear in the subcategory picker when creating or editing certificates of the affected types.

### Known follow-ups

- **The admin exports still won't show this field.** That is a separate code gap — those exports
  never had the column, populated or not. The certificate list exports do have it and now carry the
  data. Logged as DEF-412.
- **About 4,350 older expired certificates cannot be filled in.** None of the source files we hold
  carry subcategory values for them. Closing that gap needs the client to supply the data.
- **One question for the client.** Thirty of the entries name a subcategory identical to the
  certificate's own type (e.g. a Waste Diversion certificate with "Waste Diversion from Landfill" as
  its subcategory). They were loaded because that is what the spreadsheet says, but it is worth
  confirming they were meant as subcategories rather than shorthand — they are cleanly reversible if
  not.

### Also on 2026-09-08 — the admin exports now include the subcategory column

Loading the data was only half of it. The **admin exports never had a Certificate Subcategories
column at all** — they referenced the *product* subcategory, a different field that happens to share
a word — so they would have stayed blank no matter how good the data was.

All four export downloads now carry it: the certificate reports and the dashboard statistic exports,
in both CSV and Excel. Multiple subcategories appear in one cell separated by `|`, the same way
multiple countries already do.

Verified by actually downloading and parsing all four files, not just by compiling: the column is
present and populated in every one.

**This part is not live yet** — it is a code change, so it goes out with the next deploy. The data
itself is already on the test environment.


---

## 2026-09-08 — Confidential certificates reach Amazon, and Certification Results became true free text

**Built, not yet deployed.** Both are approved change requests that had been sitting specified but
unbuilt — CR-043 since 2026-09-02 (client answers received 9/3) and CR-045 since 9/4.

### Changed

| Ref | What changed for the client |
|-----|------------------------------|
| CR-043 | **Confidential certificates now reach the product-identifier export and the Amazon feed.** Paid, Confidential and Republished all count as in force, provided the certificate is inside its effective/expiration window and tagged to that partner. A company signed in to its own account now sees its own Confidential certificates, and Confidential counts as Active in the internal dashboard counts and their exports. **The public database is unchanged** — Confidential stays off it. |
| CR-045 | **Certification Results is a true free-text field.** Whatever an analyst types is what publishes — screen, exports, Amazon — with no interpretation. The old rule silently converted bare numbers on the way out (0.5 → 50%, 85 → 85%). |

### What it moves, in numbers

- **13** in-window Confidential certificates carry product identifiers (the 11 the client listed plus
  `26-0473`/`26-0474` from a later load); **420** identifier rows added to the PI export; all 13 are
  Amazon-tagged, so all 13 become visible to Amazon.
- **CR-045 changes nothing about existing data, and that is measured rather than assumed:** of 1,102
  populated Certification Results values, 1,071 are already `XX%` and 31 are non-numeric — **0** were
  being converted. No displayed value, export, sort order or Amazon payload changes.

### Checked before shipping

Eleven live checks on dev, all passing, each with a control that would catch a false pass:

- Anonymous callers get **404** on all 13 Confidential certificates — while a normal public
  certificate still returns 200, so the 404s mean "embargoed", not "the site is broken".
- The PI export contains all 13, labelled `Client Approved - Confidential`.
- **Amazon sees 13/13; Novi sees 0/13** — and Novi's response was non-empty, so that zero is a real
  denial rather than an empty page. Widening a status rule must never widen a partner's view.
- Expired `23-0758` is **still absent**, which is correct — it is expired, not Confidential.
- Stored `23%` publishes as `23%` everywhere it appears.

Re-runnable against any environment: `Scripts/data-ops/2026-09-08-CR043-CR045-verify.py <base-url>`.

### Deploy requirements — both must go with the code

1. **Migration** `20260908175939_CR045_CertificationResultsVerbatimSort` — rewrites the persisted
   computed column that drives Certification Results sorting. If the code ships without it, a future
   value displays one way and sorts another.
2. **Data script** `Scripts/data-ops/2026-09-08-CR043-productid-status-backfill.sql` — 420 product
   identifier rows on Confidential certificates still read "Inactive"; they are copied from the parent
   certificate at save time and do not recompute themselves. Dry-run by default; set `@Commit = 1`.

**Migration first, then code** — the 2026-09-02 incident is the standing reason.

### Known follow-ups

- **Amazon needs telling.** They begin receiving certificates they have never seen. The response
  *shape* is unchanged, so it is a heads-up, not a contract re-baseline.
- **`23-0758` must be said plainly to the client before delivery**, or it will still be missing
  afterwards and read as a failed fix. It has now been reported missing twice.
- **The optional CR-045 guard was not built** — a soft warning on import preview / Create / Edit when
  someone types a bare decimal. Without it the safety net is fully off, which is the client's stated
  intent.
- **The .NET test suite has 89 pre-existing failures** unrelated to this work (mostly the partner API
  integration tests, which return empty for every partner). This change adds none, established by
  diffing against a pre-change baseline. Worth a session of its own before the suite can be used as a
  gate.


### DEPLOYED 2026-09-08 — verified live

Uploaded and recycled the same evening. **17 of 18 live checks pass.**

- Anonymous callers get **404** on all 13 Confidential certificates, while a normal public
  certificate still returns **200** — the embargo held, and the control proves the check is real.
- The product-identifier export carries all **13** certificates and **420** identifier rows,
  labelled `Client Approved - Confidential`. Expired `23-0758` is still absent, correctly.
- Amazon sees **13/13**. A different company's account sees its own **23** certificates and
  **none** of the 13.
- `Certification Results` publishes verbatim on the tile API and the detail view.
- `/appsettings.Production.json` returns **404** — the config handler survived the extract.
- DEF-412's `Cert Subcategories` column is present **and populated** (169 of 1,061 rows).

**One count moved, and the client should hear it from us first.** The Analyst/Admin
"active-certificates" report now returns **1,061** rows instead of 1,019. The difference is exactly
the **42** in-window Confidential certificates that CR-043 Q2 added to the internal definition of
"active" — the client answered Yes to that question on 9/3. Reconciled against the database, not
estimated.

**The one check that did not pass is a fixture gap, not a defect.** The partner-isolation control
signs in as Novi and asserts it cannot see Amazon's certificates. Novi has **0 tagged certificates
on this environment**, so it sees nothing at all and the check proves nothing — a control that
returns an empty set cannot tell "correctly denied" from "broken". It is reported as a failure
rather than quietly counted as a pass, and a second control using a real company account carries
the assertion instead.

---

## 2026-09-09 — Certificate subcategories are now searchable and filterable (QC #260)

**The problem, in one line.** The certificate subcategory field was being saved, displayed, sorted and
exported — but it was never used for *matching*. Neither the filter checkboxes nor the search box had
ever looked at it, on any view. It was not a recent regression; the field has behaved this way since
it was built.

### What changed

Selecting a Certification Type filter option now returns a certificate when **either** its own
certification type **or** any of its certification-type subcategories carries that name. Typing a
subcategory name into the search box does the same. This applies to the public view, the external
(login) view, the internal analyst/admin views, the internal list, and the CSV export.

**The client's own example, confirmed on the live site.** Certificate `25-8071` (JW Aluminum,
Certified Sustainability Facts, subcategories Recyclable Material / Recycled Content / Waste
Diversion from Landfill) is now returned by all three of "Certified Sustainability Facts",
"Recycled Content" and "Waste Diversion from Landfill".

### What it moves, in numbers

| Public filter option | Before | After |
|---|---|---|
| Recycled Content | 458 | **529** |
| Waste Diversion from Landfill | 46 | **151** |
| Certified Sustainability Facts | 103 | 103 (unchanged) |

**No filter option lost results.** The change is purely additive, so there are still no dead clicks.

The Export button agrees with the screen: exporting a "Recycled Content" filter returns **2,403 rows**,
the post-fix figure exactly (it was 2,314 before).

### Checked before shipping

- Six of the thirteen top-level filters on the live public API returned certificates whose *own* type
  differs from the filter value — impossible under the old exact-name match, so the fix is provably live.
- The export was checked independently of the screen, because a filtered screen and its CSV disagreeing
  is a failure mode this project has already paid for once (DEF-404).
- **Scoping does not widen.** An external user scoped to one company still sees only its own
  certificates: the unscoped match for "Carbon Footprint Reduction" spans 39 certificates across other
  companies, and that user gets exactly its own 1. Anonymous visitors get the 151 that pass the public
  visibility gate out of an unscoped 399.
- Performance: the matching is resolved as a separate set lookup rather than a per-row test, the shape
  this project settled on after two measured slowdowns (DEF-366, DEF-402). No return to multi-second
  searches.

### Known follow-ups

- **Two subcategory names match certificates but cannot be clicked yet** — *Recyclable Material*
  (9 public certs) and *Zero Waste to Landfill* (17). Both are findable by typing them into the search
  box, so nothing is unreachable. Making them clickable means putting subcategory names back into the
  Certification Type filter box, which is the opposite of what was asked for on 4/30 (issue #169), so
  it needs the client's explicit go-ahead first. **Question asked, not assumed.**
- **One confirmation wanted.** "Recycled Content" now means two things at once: certificates whose type
  *is* Recycled Content, plus CSF certificates carrying it as a subcategory. That is what was asked for
  and it is why the counts above rose — but it widens a control the client had already signed off, so
  it is worth an explicit yes.


---

## 2026-09-10 — Amazon now receives the correct code for Certified Environmental Facts (QC #264 / CR-021)

**One value, on one row, that had been wrong since the beginning — and it was blocking Amazon badges
for 476 certificates.**

Certificates of type **Certified Environmental Facts** were being sent to Amazon with the code `CEF`.
Amazon does not recognise that string, so those products would not be badged. They now go across as
**`greencircle_cef`**, which is the value Amazon actually expects.

### Why this took until now

We raised this ourselves on **11 June** as CR-021, having found it while analysing the client's own
sample ASIN list during the migration work: **7,904 of the 14,183 rows** in that file already carried
`greencircle_cef`, while our database still held the short seeded code. When the long-form codes were
set on 22 April, only three certification types were confirmed and Certified Environmental Facts was
left awaiting the client's mapping.

Because this is a value that goes to a partner, we would not change it without written approval — so
it sat, approved-pending, for three months. **The client's 9/9 note is that approval**, and it is
stronger than what we asked for: they named the exact string themselves rather than us inferring it
from their export.

### What changed

One value on one reference row. **No code change, and no change to the shape of what Amazon
receives** — the `amazonCode` field was always in the payload; it was carrying the wrong value for
this one certification type.

Applied to the development database and the test environment, each one checked before and after:

| | Before | After |
|---|---|---|
| Certified Environmental Facts | `CEF` | **`greencircle_cef`** |

### Checked before and after

- **Read-only pre-check first**, on both databases, confirming the row's identity and its current
  value before anything was written. It was still `CEF` on both — a real change, not a no-op.
- The update was **guarded on both the row id and the name**, run inside a transaction, with a check
  afterwards that would roll it back if the resulting value were anything other than expected.
- **It cannot be undone by a future release.** Nothing in the application writes this field — it is
  not seeded, not set by any migration, and the import's similarly-named field belongs to a different
  record type entirely. Checked rather than assumed.

### Known follow-up — a question back to the client

The client's reason for the change was that `greencircle_cef` *"matches more of format as others"*.
Only **four of seventeen** certification types use that long format; the other thirteen still carry
short codes, and ten of those have certificates behind them — the largest being Material Ingredient
Reporting with **1,557**.

If Amazon only receives the four that are already correct, there is nothing to do. If it receives any
of the others, they have exactly the problem this item just fixed, and it would be **silent** — the
certificates cross over with a code Amazon does not recognise and simply are not badged.

**Nothing else was changed.** We would be guessing at the correct strings, and a wrong partner-facing
value is worse than an outdated one. We have asked which types Amazon expects and for the official
code for each, and will handle them as one batch.


### Proven through the live Amazon feed, not just the database

We pulled the actual Amazon payload from the test site afterwards, using the Amazon API key, and
checked every certificate in it. **All 212 certificates now carry a long-form code, and none carries
a short one.**

| Code | Certificates |
|---|---|
| `greencircle_cef` | 78 |
| `greencircle_sustainable_energy_practices` | 67 |
| `greencircle_recycled_content` | 48 |
| `greencircle_energy_savings` | 19 |

### The client's own edits the same day, reconciled

The client updated records on 2026-09-10 and asked for the Amazon payload to be regenerated so they
could review the effect. We compared it against the 9/9 payload certificate by certificate. **Every
difference is accounted for; nothing is unexplained.**

- **11 certificates left the feed** — 6 Waste Diversion from Landfill, 4 Closed Loop Product, 1
  Sustainable Manufacturing Practices. Ten were untagged from Amazon and one moved to Draft.
- **7 certificates were re-typed** from Carbon Footprint Reduction to Certified Environmental Facts
  and stayed in the feed.
- **Nothing was added**, and no other certificate changed. The feed goes from 223 to 212.

**Taken together with this fix, that closes the gap completely.** Before today, four certification
types in the feed were still sending short codes and would have failed exactly as CEF did — sent
across with a value Amazon does not recognise, and silently not badged. The client's edits removed
all four; this fix corrected the fifth.

The remaining short codes elsewhere in the system now reach nobody. They matter only if a certificate
of one of those types is tagged to Amazon later, so we have offered to set them on request rather
than leave a trap.

---

## 2026-09-10 — Edit opens in a new tab; the list view opens by default

**Pending deploy** — built and verified on 2026-09-10, awaiting upload to gccwebapp.com.

### Delivered

**Edit now opens in a new tab, so you no longer lose your search (CR-048).** Reported by the team:
editing a run of certificates meant going back and re-running the same search every single time,
because Edit navigated the page away from the results. Edit now behaves the way Preview already did
— it opens in a new tab, the results list stays exactly where it was, and closing the tab puts you
straight back into the search you were working through. Applied everywhere Edit appears, not just the
one screen it was reported on.

**Logged-in users now land on the List view (CR-049).** Reported by the team: the list is what they
actually work in, and every page load started on tiles, so the first action of every visit was
toggling the view. Logged-in users now open straight into the List view. If you prefer tiles, simply
switch once — your choice is remembered for next time, so nobody has to keep toggling in either
direction. The public certificate database is unchanged.

> The remembered choice is per browser, so signing in on a different machine starts on List again.

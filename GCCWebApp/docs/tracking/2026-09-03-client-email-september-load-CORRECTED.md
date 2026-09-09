# Client email — 2026-09-03 — September certificate + ASIN load: what is in, what we need

**Status:** DRAFT for Shawn's review before sending
**Attach:**
  - `2026-09-03-audit-certificates-loaded.csv` (193 rows — every certificate loaded)
  - `2026-09-03-audit-multi-country-check.csv` (1,305 rows — the country check against their
    August spreadsheet, regenerated AFTER the split so it shows the corrected state)

> INTERNAL — read before sending.
>
> 1. **CR-044 has no client sign-off.** Two things in this load were done on your authority, not
>    theirs: the 3 GAF renewals (an UPDATE to live records) and the 2 rebuilt Philips certificates.
>    Sections 3 and 4 put both in front of them explicitly and ask them to confirm. Do not soften
>    that — it is the only record of authorisation we will have.
> 2. **Say "your live database" for db.greencirclecertified.com and "the new database" for
>    gccwebapp.com.** Never call gccwebapp.com "production" to the client; it is the test
>    environment. Their production is the old system.
> 3. **The search section changed on 2026-09-04.** It used to ask the client to accept a
>    behaviour trade-off on certificate-number matching. That was based on a wrong diagnosis
>    (DEF-399) — the real cause is our own CR-037 country clause, now fixed (DEF-402). The section
>    now says "fixed, pending deployment" and asks for nothing. It admits the cause is ours, which
>    is correct and better than leaving them thinking their data is too big. **Do not promise a
>    deployment date** — the fix builds and is proven at the query level but has not been measured
>    end to end on the live site.
> 3b. **This file supersedes `2026-09-03-client-email-september-load.md`.** That one was open in an
>    editor and could not be rewritten. Delete it and drop the `-CORRECTED` suffix once it is closed,
>    so there is only one version of a client letter in the folder.
> 3c. **The export item was FALSE and is now a correction, not an open item.** It came from the
>    2026-09-01 split script's doc comment, written before `CertificateService.BuildCountryList`
>    existed, and was repeated into this letter without checking the deployed site. Verified
>    2026-09-04 by downloading the real export: multi-country certificates list every country.
>    Owning the mistake in the letter is deliberate — they were told an untruth and would otherwise
>    keep tracking it.
> 4. If you want to attach evidence, the per-certificate audit is
>    `Scripts/data-ops/_batch2_output/<stamp>/certs-loaded.csv` and the country audit is
>    `Scripts/data-ops/_country_audit/august-country-audit-test-*.csv`. Both are clean enough to
>    send as-is. I have not attached them by default — 193 rows may be more than they want.
> 5. Everything above the Subject line and from "# Not part of the email" onward is dropped by
>    `md-to-client-letter.py`.

**Subject:** September load is in — 193 certificates and 1,870 ASINs, and the search slowdown is fixed

The 3 September files are loaded and verified on the new database. Everything you flagged in your
note is addressed, and the multi-country problem is fixed both for this batch and retrospectively
for the earlier one. Below: what went in, the two decisions we made for you and need confirmed, and
the open items.

**Two files are attached so you can check our working rather than take our word for it:**

- **Certificates loaded** — all 193, one row each, with every field as it now stands in the
  database. The two rebuilt Philips certificates are flagged in the last column.
- **Multi-country check** — all 1,305 certificates carrying a country in your August spreadsheet,
  each compared against what the database holds now. Every row reads "match". The 17 that list
  more than one country are in there, so you can see the fix rather than just be told about it.

## 1. What is loaded

| | |
|---|---:|
| Certificates added | **193** |
| Amazon ASINs added | **1,870** |
| New companies | 9 |
| New products | 125 |
| Certificate total, before → after | 5,731 → **5,924** |

Of the 193: **99** are Client Approved - Paid, **92** are Drafts, and **2** are Confidential. The
Drafts came across as you asked, since the team has stopped using the old database. Drafts are not
visible to the public or to partners — that is by design, and they will appear the moment their
status changes.

A full backup of the database was taken before anything was written, and the whole load was
rehearsed against a separate copy first.

## 2. The four things you raised

**"Certs with multiple countries did not carry over."** Fixed, and it went further than the batch.
Ten certificates in this load list several countries and now hold each one separately.

We then checked the **earlier** load against your August spreadsheet, which we had not done before.
That file contains **17** certificates listing more than one country. You had found and fixed
**2**. The other **15** were still storing the whole thing as a single value — "United States;
China; Poland" was being treated as if that were one country's name, which is why it matched no
filter and no search. All 15 are now split. **17 of 17 are correct**, and no combined values remain
anywhere in the database.

That also answers the semicolons you spotted: they were never a formatting choice. A semicolon in a
country field only ever meant that record was one of the unfixed ones — your August sheet used
semicolons in some rows and commas in others, and the earlier load stored the text as typed. The
site has always joined multiple countries with commas.

*Semicolons in the Location field are correct and were left alone.* 301 certificates list several
sites there, and each site already contains a comma — "Athens, GA; Chowchilla, CA".

**"Product Cat/Type/Subcat should not pull the numbers in front of the name."** Done. "4.3 Cleaning
Supplies" loads as "Cleaning Supplies".

**"N/A is an option and should show."** It does — 18 certificates use it.

**"Search is taking 10-12 seconds."** Reproduced exactly. See section 5; we need a decision from you
before we can fix it properly.

## 3. Two certificates we rebuilt — please confirm

**1,500 of the 1,909 ASIN rows — 79% of that file — were assigned to Philips certificates 26-0438
and 26-0439, which were not in the certificate sheet.** Without those two records, most of the ASIN
file could not be loaded at all.

Both certificates are live on your own database, so rather than hold the file we rebuilt the two
records from it, field by field. Every value came from your systems; nothing was invented. The
certificate documents themselves, retrieved afterwards, confirm the result — 26-0438's PDF reads
"Philips, Drachten, The Netherlands — Grooming — Sustainable Energy Practices — 100% Renewable
Electricity Usage — Certification Number 26-0438 — July 1, 2026 to July 7, 2027", matching every
detail we had reconstructed.

**Please confirm these two records are correct.** If anything is off we will correct it; they are
carrying 1,500 ASINs between them.

## 4. Three renewals and two company names — please confirm

**Three GAF certificates were renewals, not new records.** 25-0497, 25-8163 and 25-8164 already
existed and had expired. The sheet carried them with new dates and an approved status, and your live
database shows all three with those renewed dates — so we updated them in place. Only the status and
dates changed; company, product, certification type and the attached document are untouched.
**Please confirm the renewals are correct**, as this changed existing records rather than adding new
ones.

**Two company names in the sheet did not match anything in the database**, and we judged them to be
the same companies rather than new ones:

| In the sheet | Attached to existing record | Why |
|---|---|---|
| J&J MedTech Orthopedics | J&J MedTech Orthop**ae**dics | One-letter spelling variant |
| GAF Materials LLC | GAF | Six other rows in the same sheet spell it "GAF" |

Creating them as new companies would have split one client's certificates across two records. One
certificate each — easily reversed if you would rather they were separate.

## 5. Where the remaining items stand

**a. Search speed — found and fixed, waiting on a deployment.** Typing a certificate number into the
internal search was taking about **8 seconds**, and a term matching nothing about **10** — exactly
the 10-12 seconds you reported.

The cause is ours. The multi-country feature we added on 2 September checks each certificate's
countries in a way that re-reads the country list once per certificate, which is why search slowed
down that week. Measured on your data, that one check accounts for roughly **fifteen times** the
cost of the entire rest of the search.

It is rewritten to look the countries up once instead. On the same searches the query drops from
**about 3 seconds to about 0.2 seconds**, and returns **identical results** — we checked the row
counts match exactly, so nothing about what search finds changes. Country search still works on
every country a certificate lists, primary or secondary.

This is a code change, so it reaches the site at the next deployment rather than immediately, and we
will re-measure on the live site afterwards to confirm the improvement end to end. **No decision
needed from you** — an earlier draft of this note asked you to accept a trade-off on how certificate
numbers are matched; that turned out to be unnecessary and we are not doing it.

**b. 97 of the 193 have no certificate document.** None came with the files. We retrieved and
attached **96** — the ones published on your live database — so those now have a working preview and
download. The remaining 97 are Drafts, Confidential, or not yet within their date window, so they
are not published anywhere we can retrieve them from. **If you want documents on those, they need to
come from the team.**

**c. One certificate's document is password-protected — 23-0971, HP EvoCycle Toner Cartridge.** It
has no preview image, and more importantly **nobody who downloads it can open it**, including you.
The file came from your own system, so the password is on HP's side, not ours. We checked every one
of the 5,671 documents we hold: **this is the only one**. Could you ask HP for an unlocked copy?

**d. Two certificates arrived with no country** — 26-0532 and 26-0631. They will not appear under
any country filter until one is supplied. What should they be?

**e. Exports list every country — this one is already done.** We told you the exports showed only
one country per certificate. **That was wrong, and we are correcting it rather than leaving it in
your notes as an open item.** We checked by downloading the real export from the site: a certificate
covering three countries exports "United States, China, Poland", and every one of the 15 certificates
we corrected yesterday exports its full list. Nothing is outstanding here.

## 6. Amazon

The Amazon feed was re-checked against its April baseline and returns **exactly the same structure**.
Amazon now receives 1,870 additional product identifiers across 12 certificates; nothing about the
format they consume has changed.

---

# Not part of the email — internal record

**Session evidence.** Load and verification: `Scripts/data-ops/2026-09-03-batch2-load-BUILD.py`,
`2026-09-03-batch2-VERIFY.py` (25 checks, green on dev and test). PDFs:
`2026-09-03-batch2-pull-pdfs.py`, `2026-09-03-batch2-upload-pdfs.py`. Country audit:
`2026-09-03-august-country-audit.py`. Search measurement:
`Scripts/smoke-prod/2026-09-03-search-latency-probe.js`.

**Register entries raised this session:** CR-044 (the renewals + rebuilt certificates + company
folding), DEF-399 (search), DEF-400 (encrypted PDF, no preview), DEF-401 (Edit page has no image
input, so an analyst cannot set a preview by hand).

**Deliberately NOT in the email:**
- DEF-401 — internal; it explains why we cannot fix 23-0971's preview ourselves, but the client does
  not need to know our Edit form is missing a field until we have fixed it.
- The permissions/tooling work — entirely internal.
- The `GAR(Z)-GPXX-25` / `GPxx` capitalisation difference between the August and September sheets.
  Two certificates reuse the existing product record; the app cannot distinguish the two spellings
  anyway. Mention only if they ask why the product name looks different from their sheet.

**Backup taken before the load:** `db-backups/gccwebap_certificates-2026-09-03-213053`
(45 tables, 42,595 rows). Undo scripts: `_batch2_output/<stamp>/batch2-UNDO.sql` and
`_batch2_pdfs/undo-<stamp>.sql`.

# Client email — response to the ASIN validation findings

**Status:** Draft for Shawn to send
**Date drafted:** 2026-08-21
**Responds to:** client ASIN validation, 2026-08-21 (Philips 2870/2019, Estée 3252/1847,
Clover 3179/67+1202, Kohler 1959/1051, P&G 2264/1240)
**Attach:** `Scripts/data-ops/_reconciliation/asin-per-company-2026-08-21.csv`
**Attach:** `Scripts/data-ops/_reconciliation/_asin_record_log/asin-record-log.csv` (16,237 rows —
every certificate/ASIN pair) and `asin-percert-summary.csv` (per certificate). The client asked for
"a log of every record and its ASINs compared to the import sheets"; these are it.
**Attach:** `Scripts/data-ops/_reconciliation/_asin_record_log/migration-row-mapping.xlsx` — the
row-by-row map of BOTH import sheets. Tabs: All Rows (21,034) · Duplicates (4,791) · Not Loaded (9) ·
Certificates (5,731) · Certificates Not Loaded (29). **This is the one to send if only one goes.**
**Updated 2026-08-23:** section 2 now carries the record-level result. The client replied that they
are "still concerned the ASINs are not correct", so counts alone will not settle this — the
value-level evidence is the point of the email now, not the arithmetic.
**⚠️ Section 4c is NEW and is the most consequential thing in this email.** Mapping the certificate
workbook row by row surfaced **29 certificates that never loaded**, all quarantined by the transform
for empty required fields. Twenty-seven are Expired; **two (23-0358, 25-0351, Sofidel) are
`Client Approved - Paid`** — live, paid certificates the client will expect to see and which are not
there. This was recorded in `quarantine-incomplete-rows.csv` on 2026-08-18 but was **never surfaced
to the client, and is not in the migration-complete email that went out that day.** Do not drop 4c
to keep the email short; it is the part with a real consequence.

> ## Internal notes — not part of the email
>
> **The finding is unqualified.** The client confirmed they are validating the 2026-08-18 load and
> nothing has been loaded since, so the 08-18 test dump this analysis runs against is exactly the
> data they are looking at. Four of their own five numbers (Philips 2019, P&G 1240, CES 67, Clover
> Imaging 1202) reproduce from that dump to the row, which independently confirms the basis.
>
> **Evidence:** `Scripts/data-ops/2026-08-21-asin-per-company-reconciliation.py` closes
> `master rows = DB rows + collapsed + invalid` for all 36 companies, 21,034 of 21,034 rows,
> exit 0. It complements `2026-08-18-asin-reconciliation.py`, which proves the same thing per
> certificate in both directions (0 missing, 0 extra) but could not answer a per-company question.
>
> **No DEF row.** Nothing is wrong with the code and no data was lost. The nine rows that did not
> load are source-data defects on the client's side and the loader rejected them correctly.
>
> **Worth raising separately as a CR, deliberately kept out of this email:** the Product Management
> page's ASIN count is genuinely confusing. It dedupes by type+value *within each product line item*
> (`wwwroot/js/admin/productManagement.js`), so the total a person arrives at by searching a company
> name is neither the number of ASIN records nor the number of distinct ASINs — it depends on how
> that company's certificates happen to be spread across line items. That is precisely why the
> client's five numbers came out in two different units. A careful reviewer got two different
> answers off one screen; that is a UX problem, not a misread. Suggested fix is a plain
> "distinct ASINs" figure per company. Do not fold this into this email — answer the validation
> question first, raise the CR on its own.
>
> **Decision needed from the client is real**, section 4. We picked which of two product names to
> keep for 1,564 ASINs by spreadsheet row order, because we had to pick something. They should
> ratify it.

---

**Subject:** ASIN validation — every ASIN accounted for, and 29 certificates we need fields for

Hi [NAME],

Thank you for sending these through before you headed out — this was exactly the right thing to
check, and the detail you gave made it quick to trace.

**The short answer: no ASINs are missing.** Every one of the 21,034 rows in your master list is
accounted for. What you found is real, but it is a difference in *what is being counted*, not a
difference in *what was loaded*.

## 1. Why the two numbers differ

Your master list and the application count in different units.

**Your master list is a flat row list.** When the same ASIN appears on a certificate under more than
one marketing name, it takes up more than one row. That is by design and it is useful for your
purposes — but it means the list holds more rows than it holds ASINs.

**The database stores each ASIN once per certificate.** If the same ASIN is listed three times on
one certificate under three different product names, it is one entry in the database, not three.

So for any company where your list repeats ASINs, the master row count will be higher than the
database figure. Neither number is wrong; they are answers to two different questions.

There is a third wrinkle, and it is ours rather than yours: the Product Management page removes
duplicates again within each product line, so the total you reach by searching a company name can
land on either unit depending on how that company's certificates are arranged. That is why your five
results were not consistent with each other. We are looking at making that screen state plainly
which figure it is showing.

## 2. The arithmetic, company by company

For every company, the master row count equals the database entries, plus the rows where the same
ASIN was repeated on the same certificate, plus anything the system could not read.

| Company | Your master list | In the database | Repeated on same certificate | Could not be read | Total accounted for |
|---|---|---|---|---|---|
| Estée Lauder | 3,525 | 1,854 | 1,671 | 0 | **3,525** |
| Clover Environmental | 3,179 | 2,176 | 1,003 | 0 | **3,179** |
| Philips | 2,870 | 2,019 | 842 | 9 | **2,870** |
| Procter & Gamble | 2,264 | 1,240 | 1,024 | 0 | **2,264** |
| Kohler | 1,959 | 1,959 | 0 | 0 | **1,959** |

This holds for all 36 companies in your list — **21,034 of 21,034 rows**. The attached file shows
every company, not just these five, so you can check any of them.

**We have also now checked every ASIN individually, not just the totals.** Because totals can agree
while the underlying values differ, we compared every single ASIN on every certificate against your
master list, in both directions — each value present in your list confirmed present in the database,
and each value in the database confirmed present in your list. Against a live extract taken today:

| Check | Result |
|---|---|
| ASINs matched on both sides | 16,234 |
| In your list but missing from the database | **0** |
| In the database but not in your list | **0** |
| Certificates whose ASIN set differs | **0** of 158 |
| Product names differing in content | **0** (309 differ only by spacing) |

The only values on one side are the nine rows in section 4a below, which are not ASINs and could not
be loaded. We have attached the full record — every certificate, every ASIN, one row each — so this
is checkable rather than something you have to take on trust.

**Kohler is the clearest proof.** Kohler's list never lists the same ASIN twice on the *same*
certificate, so there is nothing to combine — and the master and the database agree exactly,
**1,959 against 1,959**. The 1,051 you saw is that same data with the repeats *across* certificates
removed for display. If anything had gone wrong in the load, Kohler could not have matched to the
row.

One small thing on Estée Lauder: we read 3,525 rows in the master list where your note said 3,252.
Worth a second look in case a figure was transposed somewhere.

## 3. Clover — you were right, and there are two more

Your instinct here was correct. Your master list files everything under Clover Environmental
Solutions, but the certificates themselves carry the individual private-label company names, and the
database follows the certificates. Searching "Clover" finds two of them. There are **four**:

| Company on the certificate | ASINs |
|---|---|
| Clover Imaging Group | 1,202 |
| INNOVERA | 883 |
| Clover Environmental Solutions | 67 |
| Dataproducts | 24 |
| **Total** | **2,176** |

That 2,176, plus the 1,003 rows where an ASIN repeats on the same certificate, is your 3,179.

The open question is which is correct for your purposes — the grouping in your master list, or the
company recorded on each certificate. We have followed the certificates. If you would rather these
were grouped under a parent company, that is a change we can make, but we would want your
confirmation on how the grouping should work before touching it.

## 4. Two things we need from you

**a. Nine rows could not be loaded — all on Philips certificates.** These are the only rows in the
entire migration that did not come across, and it is because the values are not ASINs:

| Certificate | Value in the list | Number of rows |
|---|---|---|
| 24-1634 | TO BE CHECKED | 6 |
| 24-1633 | TO BE CHECKED | 2 |
| 23-1046 | 08FDWPMP6 (nine characters — an ASIN is ten) | 1 |

Send us the correct values and we will load them.

**b. 1,564 ASINs had two different product names, and we had to choose one.** Where the same ASIN
appears twice on one certificate under two different marketing names, only one name can be stored.
We kept the one that appeared first in your spreadsheet. For example, on certificate 23-0753, ASIN
B00L1GHHV2 kept the full Amazon listing title and we set aside "DUDE Wipes 48ct Fragrance Free".

Please confirm that keeping the first name is what you want. If you would rather have the shorter
internal names, or a specific one of the two, we can reload them — the discarded names are all
recorded, nothing has been lost.

**c. Separately — 29 certificates could not be loaded, because required fields are empty in the
certificate spreadsheet.** These are not ASINs and are not related to the counting question above;
we are raising them here because we have now mapped that file row by row as well.

Twenty-seven are Expired. **Two are not, and those are the ones worth your attention:**

| Certificate | Company | Status | What is missing |
|---|---|---|---|
| 23-0358 | Sofidel America Corp. | Client Approved - Paid | Expiration Date |
| 25-0351 | Sofidel America Corp. | Client Approved - Paid | Expiration Date |

Both are live, paid certificates that are **not currently in the database and therefore not visible
in the application.** The other 27 are listed in full on the "Certificates Not Loaded" tab of the
attached workbook, with the missing field named for each — the gaps are Product Name, Effective Date
and Expiration Date.

Send us the missing values and we will load all 29. Nothing was discarded; they are held back
because loading a certificate without an expiration date would make it impossible to determine
whether it is valid.

## 5. One housekeeping note

Your master list has a few company names that differ only by capitalisation or a trailing space —
"X-SENSE" and "X-Sense", "Block Inc." and "Block Inc. " — which splits one company into two entries
when totalling. It does not affect what was loaded, but it will keep making counts look wrong.

---

Please do keep sending findings like this. Checking the totals independently is the right way to
validate a migration, and the two real issues above surfaced because you did.

Best regards,
Shawn

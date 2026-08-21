# Duplicate certificate numbers — findings, options and decisions required

**Prepared by:** Anuki Consulting · **Date:** 2026-08-14
**For:** GreenCircle review ahead of the go-live data migration
**Companion files:** `duplicate-certificates-FULL-LIST.csv` (all 312 rows) ·
`duplicate-certificates-ROLLUP.csv` (summary) · `quarantine-incomplete-rows.csv` (the 29 in §7)

---

## 1. The issue in one paragraph

Some certificate numbers appear more than once in the migration data. The new database was
designed on the assumption that a certificate number identifies exactly one certificate, so as
things stand these records cannot all be loaded. **These are issued certificate numbers that
cannot be changed, and the certificates need to remain available** — so the question is not whether
to keep them, but which way of representing them we adopt.

---

## 2. What we found — measured, not estimated

| | |
|---|---|
| Certificate numbers affected | **137** |
| Certificate rows affected | **312** |
| Extra rows beyond one per number | **175** |
| Numbers where the duplicates have **genuinely different certificate documents** | **128** |
| Numbers where the same document was simply filed twice | 8 |
| Numbers identical on every field we compared | 1 |

Two or three rows usually share a number (104 numbers have 2 rows, 28 have 3, 5 have 4).

### 2.1 The finding that decides the answer

We cross-checked every affected number against the certificate PDFs pulled from your own system.
**128 of the 137 numbers have siblings with genuinely different documents** — different files, not
copies of one file.

This matters because it corrects our earlier working assumption. From the spreadsheets alone, most
of these looked like the same certificate listed twice. **The documents show otherwise: they are
different certificates that happen to share a number.** Merging them would not tidy up duplicate
rows — it would discard 128 real, separately issued certificates.

Your description — *"they are different certificates"* — is borne out by the evidence, and it holds
well beyond the one client we first attributed it to.

### 2.2 What actually differs between them

| Numbers | What differs |
|---|---|
| 92 | Product only |
| 20 | Company **and** product |
| 12 | Nothing on the compared fields |
| 11 | Dates, or a combination of fields |
| 2 | Everything — see §5, these look like source errors |

### 2.3 Who is affected

| Company | Numbers |
|---|---|
| Sika Canada Inc. | 65 |
| Regupol America | 37 |
| Sika Corporation | 16 |
| MAPEI | 10 |
| Deceuninck North America · Corbin Russwin · Sargent · ASSA ABLOY · DuPont · Greenfield | 1–2 each |

### 2.4 Exposure — smaller than we expected, and this is good news

| | |
|---|---|
| Status | **129 Expired**, 8 currently approved and public |
| Listed against a partner (e.g. Amazon) | **0 of 137** |
| Carrying ASINs | **0 of 137** |

**None of these certificates is visible to Amazon or any other partner, and none carries an ASIN.**
That removes the most sensitive risk from this decision: whatever we choose, the partner data feed
is unaffected. Only 8 of the 137 are currently public; the rest are historical records.

---

## 3. The options

### Option A — Load them as separate certificates *(recommended)*

Each of the 312 rows becomes its own certificate, keeping its own product, company, dates and
certificate document.

- ✅ **Nothing is lost.** All 128 distinct documents, all product and company differences preserved.
- ✅ Matches how the records exist in your current system.
- ✅ New certificates would **still be prevented from reusing a number** — enforced by the database
  itself, not merely by a screen validation. The exception applies only to the migrated records,
  exactly as you described it.
- ⚠️ One narrow case moves from the database to the application: a *new* certificate being given a
  number that one of these historical records already uses. That is caught by a check we would add.
- ⚠️ Requires a small database change and a controlled release; it is not a same-day change.

### Option B — One certificate per number, extra documents attached

Keep one certificate per number and attach the sibling PDFs to it as additional documents.

- ✅ Preserves all 128 documents.
- ✅ No change to the uniqueness rule at all.
- ❌ **Loses the product distinction on 92 numbers** — only one product can be shown.
- ❌ **Cannot represent the 22 numbers where the company differs** (e.g. Sika Canada vs Sika
  Corporation). One company must be chosen, which makes the record *wrong* rather than merely
  reduced.
- ❌ Needs a new screen area to display multiple documents, which does not exist today.

### Option C — Merge duplicates into one record

- ❌ **Not viable.** It would discard 128 separately issued certificates. We include it only to
  record that it was considered and rejected on the evidence.

### Option D — Add a suffix to make numbers unique (e.g. `26-0182-A`)

- ❌ **Rejected.** It changes issued certificate numbers, which are the public identifier and cannot
  be altered.

---

## 4. Recommendation

**Option A.** It is the only option that keeps every certificate, every document and every product
and company distinction, and it preserves your rule that new certificates cannot duplicate a
number. The risk that would normally make us cautious — partner-facing data — is measurably zero
here, because none of these certificates is exposed to Amazon or carries an ASIN.

Option B remains a legitimate fallback if a database change is unacceptable in the go-live window,
but it should be chosen knowing that 22 records would carry the wrong company and 92 would show
only one of their products.

---

## 5. Decisions we need from you

| # | Decision | Why it matters |
|---|---|---|
| **1** | **Confirm Option A** (or B). | Nothing loads for these 137 numbers until this is settled. |
| **2** | **Two numbers appear to be source errors** — `21-0179` and `21-0180` each carry **two unrelated companies** with different products, types and dates (Deceuninck North America vs DuPont Performance Building Solutions; Deceuninck vs 4ocean). Which is correct? | These are not the same pattern as the rest and we do not want to guess. |
| **3** | **9 numbers can safely be merged** — 8 where the identical document was filed twice, 1 identical on every field. Merge them, or keep them as separate records for consistency? | Small, but it changes the final counts. |
| **4** | Should the 8 **currently-public** duplicated certificates display any indication that several certificates share a number? | Affects what a visitor sees on the public site. |
| **5** | **The 29 incomplete records in §7** — can you supply the missing product names and dates, or should they be left out of the migration? | They cannot load as they stand, and we will not invent the missing values. |

---

## 7. A second, smaller item — 29 records we could not load

Separate from the duplicate numbers above, **29 rows in the source files are missing information the
database requires**, so they did not load. They are listed in full below and in
`quarantine-incomplete-rows.csv`.

| | |
|---|---|
| Rows affected | **29** of 5,731 |
| Missing a product name | 14 |
| Missing one or both dates | 15 |
| From Expired List 1 | 27 · from Active List | 2 |

**Why we did not fill these in.** A certificate cannot exist in the new database without a product
and without both dates — those fields are required. We could have substituted something (today's
date, a placeholder product), and an earlier version of the import did exactly that: it gave an
expired 2013 certificate an expiry date of 2027, and gave two certificates an effective date
*after* their expiration date. **A plausible-looking invented value is worse than a visible gap**,
because nothing downstream ever flags it. We stopped doing it and these rows are held instead.

**Two of the 29 are currently approved and public** (`23-0358` and `25-0351`, both Sofidel America)
— worth prioritising over the 27 expired historical records.

### The full 29

| Certificate | Company | Status | What is missing |
|---|---|---|---|
| `20-0397` | g2 revolution® | Expired | Product Name |
| `20-0462` | Best Diamond Packaging | Expired | Product Name |
| `20-0464` | Best Diamond Packaging | Expired | Product Name |
| `21-0537` | Maple Leaf Foods - New Hamburg Hatchery | Expired | Product Name |
| `21-0588` | Climate Action Now | Expired | Product Name |
| `21-0704` | Best Diamond Packaging | Expired | Product Name |
| `22-0348` | Malarkey Roofing Products® | Expired | Product Name |
| `23-0376` | Maple Leaf Foods - New Hamburg Hatchery | Expired | Product Name |
| `23-0920` | Cox Enterprises, Inc. | Expired | Product Name |
| `24-0143` | Cox Enterprises, Inc. | Expired | Product Name |
| `25-0271` | Kenvue Brands LLC | Expired | Product Name |
| `24-0353` | Copper Development Association | Expired | Product Name, Effective Date, Expiration Date |
| `24-0354` | Mueller Brass | Expired | Product Name, Effective Date, Expiration Date |
| `24-0355` | Wieland Chase | Expired | Product Name, Effective Date, Expiration Date |
| `13-0157` | Wyndham Worldwide | Expired | Expiration Date |
| `14-0278` | Stanley Security Solutions, Inc. | Expired | Expiration Date |
| `15-0067` | Covanta | Expired | Expiration Date |
| `16-0262` | The Procter & Gamble Company | Expired | Expiration Date |
| `18-0172` | Perkiomen Watershed Conservancy | Expired | Expiration Date |
| `19-0397` | The Procter & Gamble Company | Expired | Expiration Date |
| `21-0613` | The Procter & Gamble Company | Expired | Expiration Date |
| `21-0746` | Hyster-Yale Group, Inc. | Expired | Expiration Date |
| `21-0749` | Nuvera Fuel Cells, LLC. | Expired | Expiration Date |
| `21-0750` | Hyster Company | Expired | Expiration Date |
| `21-0751` | Yale Materials Handling Corporation | Expired | Expiration Date |
| `23-0358` | Sofidel America Corp. | Client Approved - Paid | Expiration Date |
| `25-0351` | Sofidel America Corp. | Client Approved - Paid | Expiration Date |
| `23-0092` | Helen of Troy Limited | Expired | Effective Date |
| `24-0401` | Sika Canada Inc. | Expired | Effective Date |

**What we need:** the missing values, or your confirmation that these records should be left out of
the migration. Supply the values and they load with everything else — no rework.

---

## 8. What happens once decided

**Option A has been built and proven on our development environment, so the change is ready the
moment you confirm it.** We have loaded the complete dataset *including* all 312 duplicate records:

| | |
|---|---|
| Certificates loaded | **5,702** across **5,527** certificate numbers |
| …of which share a number | **312** (the records in this report) |
| Product identifiers | **16,234** |
| Certificate PDFs attached | **5,474** |
| …of the 312 duplicates, with their own PDF | **310** |

Every one of the 312 keeps its own product, company, dates **and its own certificate document** —
`23-0041`, for example, holds four records with four separate PDFs. Reconciled field by field
against your source files: fourteen of the fifteen compared fields match exactly. The fifteenth is
**nine records** where *"The Netherlands"* is stored as *"Netherlands"*, the standard name.

**Your rule for new certificates is preserved and enforced by the database itself.** We tested it
three ways: a new certificate reusing an ordinary number is rejected; a new certificate reusing one
of these historical numbers is rejected; and the migrated records themselves load and remain
editable.

**228 certificates have no PDF** — these are records with no document in your source system, so
there is nothing to retrieve. One further certificate (`23-0971`) has its PDF but no preview image;
the document downloads correctly and we are treating the preview as a minor follow-up.

**Nothing here is applied to your test or live environment yet** — this is our development
rehearsal, awaiting the decisions in §5.

**Attached for review:** the full list of all 312 affected rows, showing for each one the company,
product, certification type, dates, status, whether its siblings have different documents, and our
suggested disposition — so the findings above can be checked line by line.

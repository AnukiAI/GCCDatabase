# Client email — ASIN master review, twelve questions

**Status:** Draft for Shawn to review and send
**Date drafted:** 2026-09-01
**Covers:** `GCC ASIN Master List_8.12.26.xlsm` re-audit, checked against the 2026-08-18 migration
verification workbook and the live test site.
**Attach or link:** the findings report (artifact) —
https://claude.ai/code/artifact/64d4a33c-096e-4547-af4d-85271142afce
**Supporting file:** `docs/tracking/duplicate-certificate-numbers-TEST-2026-09-01.csv` (only if the
duplicate-number thread comes up; it is not referenced in this email).

---

> ## Internal notes — NOT for sending
>
> 1. **Question 5 is RESOLVED — the client made those changes, not us. Verified 2026-09-01 against
>    the test database.** `Certificates.UpdatedBy` = user 99, carly@greencirclecertified.com, for all
>    five, on 26 August between 10:59 and 20:57. She edited **153 certificates in total** in that
>    window (138 on 8/26, 13 on 8/27, 1 on 8/28, 1 on 9/1); `admin.uat` edited 5 more on 8/28. The
>    question below has been rewritten to acknowledge her work rather than ask who did it. **Only
>    `23-0927` still needs an answer** — 52% at load, 60% in the ASIN master, 76% now.
>    Diffing the load-day dump against today, only **10** certificates show any field change; the
>    other ~143 saves changed nothing we can see, which reads as an open-and-save review pass.
>    ⚠️ **This took a database query and a load-day spreadsheet to establish, because certificate
>    edits write no audit record — now raised as DEF-391.** Do not promise the client an edit history
>    until that is fixed.
> 2. **Question 1 (`26-0053` / `26-0101`) is the highest-value item.** If it is one certificate
>    entered twice, deleting one also removes 908 of the 1,960 untagged ASINs in question 6, and
>    the certificate count drops by one. Worth leading with.
> 3. **Nothing the CLIENT is being asked needs a code change** — every question is a data decision or
>    a source-file correction. One code defect came out of the investigation, though, and it is ours,
>    not theirs: **DEF-391, certificate edits write no audit record.** Not mentioned in the email.
> 4. **Still unanswered from 18 August:** CR-038 decision #4 — should the public site indicate that
>    a certificate number is shared? Consider folding a one-line reminder into this email or a
>    separate chase.
> 6. **`B0GBZFMS1L` on `26-0028` is intentional — Shawn confirmed 2026-09-01.** It stays on both
>    certificates; the Excel row was the error, not the record. Item 11 rewritten accordingly, and
>    the "should it be removed" question withdrawn.
> 7. Amazon-facing items (6, 7, 8) may be worth a separate, shorter note to whoever owns the Amazon
>    relationship rather than burying them in a twelve-point email.

---

**Subject:** Questions from a full re-check of the ASIN master list

Hi [NAME],

We have re-checked the ASIN master list (`GCC ASIN Master List_8.12.26.xlsm`) line by line against
what was loaded on 18 August and against what the test site shows today.

**The load itself is sound.** All 21,034 rows are accounted for, 154 of the 158 certificates match
the site exactly, and every figure in the verification workbook we sent you on 18 August still holds.

What the re-check did surface is a set of questions about the source data itself. None of them is a
fault in the system, and none can be answered by us without guessing — so we are putting them to you
rather than deciding on your behalf.

---

## First, the one that explains a question you may already have

A number of certificate numbers appear more than once in the file with what looks like the same
product listed several times. **That is a formatting characteristic of the sheet, not missing data.**
The file lists one row per product *listing*, so the same ASIN appears again whenever it carries a
different product name — a localised name, a gift-edition name, a retail name.

Certificate `23-0925` is the clearest example: **345 rows, but 318 distinct ASINs.** Fourteen ASINs
are listed two, three or four times. The site holds 318, and the two lists contain exactly the same
ASINs with nothing missing in either direction. This affects 47 of the 158 certificates, so any count
taken from a row total will read high.

---

## The certified percentage

This is the number a visitor reads as the claim, and the number Amazon receives, so we have looked at
it hardest.

**1. `26-0053` and `26-0101` look like the same certificate recorded twice.**
Same company (Kohler), same product (Enameled Cast Iron Bathtubs), same certification type, same
dates, and **an identical list of 908 ASINs**. They differ in exactly one value: the result reads
**88%** on one and **87%** on the other. Both are approved and publicly visible today.
*Is this one certificate or two? If one, which result is correct?*

**2. Seven certificates carry more than one result value in the ASIN master.**

| Certificate | Values in the file | The site shows |
|---|---|---|
| 24-1634 | 0.63 and 0.23 | 63% |
| 25-0420 | 0.25, 0.23 and 0.48 | 23% |
| 25-8515 | 0.34 and 0.48 | 34% |
| 25-8516 | 0.43 and 0.48 | 43% |
| 25-8518 | 0.51 and 0.48 | 51% |
| 26-0028 | 0.84 and 1 | 84% |
| 26-0105 | 0.49 and 0.43 | 49% |

We traced where the second value comes from in each case, and there are three separate explanations:

- **A row filed under the wrong certificate number.** On `26-0028` (Kärcher, 84%), one row reads `1`.
  Every other field on that row — certification type, Amazon code, pathway — belongs to Kärcher's
  *Sustainable Energy Practices* certificate `26-0080`. Only the certificate number column says
  26-0028. The product is *K 4 Comfort Premium \*EU*. You have already corrected this on the site by
  adding the product to `26-0080` on 28 August; **the correction we would ask for is to the
  spreadsheet**, so the next export does not reintroduce it.
- **Rows carrying a neighbouring certificate's percentage.** On `24-1634` (Philips, 63%), thirteen
  rows read 0.23 — and nine of those products also sit on `23-1048`, **whose result is 23%**. The
  same pattern appears on `26-0105` (the 43% rows are X-SENSE products certified at 43% under
  `26-0441`) and on `25-0420` (the 0.25 rows match its sister certificate `25-0419`).
  *Please confirm the certificate's own value is the correct one in each case.*
- **A value that matches no certificate at all.** `0.48` appears on **64 rows across four Kao
  certificates** and nowhere else in the file. No Kao certificate has a 48% result — they are 23,
  25, 34, 34, 43, 51, 52, 57 and 100%.
  ***What is 48% for Kao?*** This is the one we cannot resolve from the files at all.

**3. 165 certificates store their result as `1`, and the site shows them as 100%.**
Both your files and the database agree the value is `1`. 141 are public and 71 are passed to Amazon.
*Please confirm `1` always means 100%, and is never shorthand for "not measured".* We ask because it
also appears as an outlier: on `26-0028` every product row reads 0.84 except one, which reads `1`.

**4. Which file is authoritative for a certificate's result — the certificate list or the ASIN
master?** Where the two disagree we have had to decide case by case, and on `26-0270` the two files
differ by 11 points (0.87 against 0.76). We would rather follow a rule than a judgement call.

**5. We can see the review you did on 26 August — thank you. Five result values changed, and one of
them we cannot tie back to either file.**

| Certificate | At load, 18 August | Now |
|---|---|---|
| 23-0753 | 3% | **21%** |
| 23-0812 | 6% | 7% |
| 23-0814 | (blank) | 18% |
| 25-8078 | 64% | 68% |
| 23-0927 | 52% | **76%** |

Four of the five now match the ASIN master exactly, so those look deliberate and we have left them
alone. **`23-0927` is the one to check**: the load had 52%, the ASIN master says 60%, and the
certificate now reads 76%. It is publicly visible.
*Please confirm 76% is correct for `23-0927`.*

You also changed the certification type on `23-0926` and `23-0927` from Carbon Footprint Reduction to
Certified Environmental Facts, cleared the country ("Hungary; Germany") on `23-0925` and `23-0926`,
and adjusted the dates on `24-5712` and `26-0326`. We have not touched any of it — flagging it only
so you know it is reflected on the live site.

---

## Amazon-facing

**6. 1,960 product identifiers carry no partner tag.**
908 on `26-0053`, 908 on `26-0101` and 143 on `25-8535` — all Kohler, all public — plus one on
`23-0814`. An untagged identifier is currently returned to **every** partner that calls the API
rather than being withheld from all of them.
*Which partner should these carry — or should untagged identifiers be withheld instead of shared?*

**7. 1,924 ASINs sit on more than one certificate, and 1,923 of those are on two certificates that
are both currently approved.** In 737 cases the two are different certification types, which we
assume is intended; the rest are the same type in consecutive years, which looks like renewal.
*When one product holds two live certificates, should the partner feed return both, the most recent,
or the one inside its validity window?*

**8. The two Amazon code columns disagree on 5,163 rows** — column D reads `greencircle_sep` where
column E reads `greencircle_sustainable_energy_practices`. The other three certification types match
across both columns.
*Which code should Amazon receive for Sustainable Energy Practices?*

---

## Small corrections to the source file

**9. Eight rows have the text `To be Checked` where an ASIN should be** — two on `24-1633` and six on
`24-1634`. They were not loaded.
*Please supply the ASINs, or confirm those products have none.*

**10. One ASIN is mistyped.** `08FDWPMP6` on `23-1046` is nine characters; the correct
`B08FDWPMP6` is also on the sheet and did load, so nothing is missing — the malformed copy was simply
ignored.

**11. `B0GBZFMS1L` sits on two certificates, and that is settled.** You added it to `26-0080` on
28 August. It remains on `26-0028` as well, which is correct — the row in the ASIN master was the
thing that was wrong, not the certificate. Nothing to do; noted only so the two records are not
mistaken for a duplication fault later.

**12. Sheet2 of the workbook flags `23-0758` as "Missing cert".** The certificate is present, with its
seven ASINs, and expired on 31 August.
*Was that note about a missing document rather than a missing record, and is a renewal in progress?*

---

Everything above is data rather than software — the system is handling all of it correctly, and
nothing needs to change on our side to answer these. If it is quicker, we are happy to walk through
the percentage questions on a call; sections one and two in particular may be faster to settle live.

Best,
[SIGNATURE]

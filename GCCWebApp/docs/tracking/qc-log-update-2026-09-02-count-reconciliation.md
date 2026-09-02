# QC Log update — 2026-09-02 (in-cell, paste-ready)

QC item: "When data was reviewed/exported on 8/27 counts did not align. Anuki needs to confirm what
is missing and why." Related: Issue 250 (Sofidel, no expiration date).

Backing analysis: `docs/tracking/2026-09-01-client-email-count-reconciliation.md`.

> **REVISED 2026-09-02 ~13:30 UTC.** An earlier draft of this file said the 29 records were still
> withheld and the 2 Sofidel certificates were staged pending a decision. **That is no longer true.**
> All 29 were loaded at 13:21 UTC on 2026-09-02 using the per-certificate values the client supplied
> in the green "Missing Field Entry" column (screenshot, 2026-09-02), via
> `Scripts/data-ops/2026-09-02-not-loaded-29-BUILD.py`. **The gap is closed: 5,731 loaded = 5,731
> sent.** Do not paste the earlier version.

Live figures verified against the test database **2026-09-02 after the load**: 5,731 total /
874 active / 871 public / 3 not-yet-effective.

---

## Status cell

```
Closed - reconciled and all 29 records now loaded (9/2/26). 5,731 loaded = 5,731 sent.
```

## Bucket / Root Cause cells

```
Question
```
```
Not a defect - Active and Public measure different things (effective-date window); the 29 withheld records were incomplete at source and are now loaded with the values you supplied 9/2
```

## Notes cell (paste as one cell; line breaks = Alt+Enter)

```
RECONCILED AND CLOSED. Both gaps are explained, and as of 9/2/26 the total gap is closed.

ACTIVE 982 vs PUBLIC 964 on 8/27 = 18.
- 2 = the Sofidel certificates with no expiration date in the source file (23-0358, 25-0351). Same two as Issue 250. They were PART OF the 29 below, not additional.
- 16 = certificates approved and paid, but whose EFFECTIVE DATE had not yet arrived on 8/27. "Active" (internal) = approved and not expired. "Public" = approved AND today falls between the effective and expiration dates. A certificate staged early is deliberately held back from the public database and the Amazon feed until its start date - the behavior agreed 8/17. Breakdown: Fleming Door Products 26-0413 through 26-0420 (8), Aqua Mizer 26-0479/0480/0481, and Charter Next Generation 26-0224/0225, all effective 9/1; DuPont 26-0330 effective 9/11; X-SENSE 26-0441/0442 effective 10/21.
- 13 of those 16 went public on their own on 9/1 with no action from anyone - the system re-checks the window daily. The remaining 3 publish 9/11 and 10/21.

DB EXPORT 980 vs PUBLIC 964 = the same 16 not-yet-effective certificates.
ACTIVE 982 vs DB EXPORT 980 = the 2 Sofidel certificates.

TOTAL SENT 5,731 vs CERT EXPORT 5,702 = 29. These were the incomplete source records we listed on 8/18 and withheld rather than invent a value: 13 with no expiration date, 11 with no product name, 3 with no product name and no effective or expiration date, 2 with no effective date. 27 came from "Expired List 1 - 4.9.26" and 2 from "Active List 7.31.26" (the Sofidel pair).

RESOLVED 9/2/26: you supplied the missing values in the green "Missing Field Entry" column, and ALL 29 records were loaded that day using exactly those values. The database now holds 5,731 certificates - equal to the 5,731 you sent. Nothing from your source files is outside the system any longer.

The 2 Sofidel certificates (23-0358, 25-0351) loaded with the expiration dates you gave (6/1/2040 and 5/1/2040) and are LIVE on the public database now. The other 27 loaded as Expired history, as expected.

ONE ITEM TO CONFIRM: your entry sheet lists the Climate Action Now record as 20-0588. That number appears nowhere in the source workbook or the verification workbook - both carry this certificate as 21-0588 (Climate Action Now, Expired, missing Product Name). We loaded it as 21-0588. Please confirm 21-0588 is correct, or tell us the right number.

VERIFICATION: we rebuilt the database as it stood on 8/27 and reproduced your numbers to the row - Public 964, Active/Export 980.

WHY THE NUMBERS KEEP MOVING: certificates cross into and out of the public window every day. Between 8/27 and 9/2, 108 certificates reached their expiration date and 13 reached their effective date. Every change is recorded with a reason in Audit History. Live counts as of 9/2/26 after the load: 5,731 total, 874 active, 871 public, 3 waiting on an effective date. A count taken 8/27 will never equal a count taken today and neither is wrong - to compare two sets, compare them as of the same date. We can produce a dated snapshot on request.
```

---

## Internal only — do NOT paste

- The **`20-0588` vs `21-0588`** discrepancy is the one genuine open question and is already flagged
  in `2026-09-02-not-loaded-29-BUILD.py`. It is in the client-facing cell above as a confirmation
  request, not as a problem.
- **11 of the 29 took `2026-08-31` as their expiration date** and three more took `2040-05-17`
  (16-year terms). These are the client's supplied values, not ours — DEF-353 is satisfied because we
  did not invent them — but they are the same class of value the 2040 Sofidel dates were questioned
  over on 9/1 (see CR-041). The basis is on file; no further action unless the client revisits.
- `CertificationResults` was normalised database-wide to percent style the same day
  (`2026-09-02-normalize-certification-results-to-percent.sql`, full backup at
  `_backups/20260902-pre-certresults-normalize/`): **961 percent-style, 29 non-numeric, 0 raw
  decimals**. This closes the parked "25% vs 0.25" question — percent won.

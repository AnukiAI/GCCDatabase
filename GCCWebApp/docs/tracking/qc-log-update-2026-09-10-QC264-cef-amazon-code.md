# QC Log update — QC #264, CEF Amazon code (in-cell, paste-ready)

**Source workbook:** `Database QC Log_9.9.26_sw_updates.xlsx`, sheet **`GCC DB QC Log2`**, header row 5.
**Item:** ID **264** · **row 273** · Page `API` · Raised by Carly · Opened 2026-09-09 · col O currently reads `New`.

| Col | Header | Who fills it |
|-----|--------|--------------|
| **J** | `Sprint 3 6/23 Notes` | **Anuki** — the cell below goes here |
| **O** | `Date Closed - GCC Confirmed Fixed` | **Client** — do not fill this in for them |
| **Q** | `Current Status - Anuki Marks Complete` | **Anuki** |

**Backing record:** **CR-021** — proposed 2026-06-11, approved 2026-09-09, **executed 2026-09-10** on
dev and the test environment. Script: `docs/tracking/sql-fixes/2026-06-11_cef-amazon-code.sql`.

---

## Column Q

```
Complete - ready for retest
```

## Column J — paste this

```
9/10: Done. Certified Environmental Facts now sends greencircle_cef instead of CEF. Live on the test environment.

This one has a bit of history worth knowing. We raised it ourselves back on 11 June as CR-021, after going through your sample ASIN list during the migration analysis - 7,904 of the 14,183 rows in that file already carried greencircle_cef, while the database still held the short code CEF. When the long-form codes were set on 22 April only three types were confirmed (Sustainable Energy Practices, Recycled Content, Certified Energy Savings) and Certified Environmental Facts was left pending your mapping. Because it's a value that goes to Amazon, we wouldn't change it without your written approval - so it has been sitting approved-pending since June. Your 9/9 note is that approval, and it's better than what we asked for, because you named the exact string rather than us inferring it from the export.

WHAT CHANGED
One value on one row. No code change, no change to the structure of what Amazon receives - the amazonCode field was always there, it was just carrying the wrong value for this one certification type. 476 certificates are of this type and all of them now report the corrected code.

Applied to both our development database and the test environment, each confirmed before and after.

CONFIRMED THROUGH THE LIVE AMAZON FEED, NOT JUST THE DATABASE
We pulled the actual Amazon payload from the test site afterwards, using the Amazon API key, and checked every certificate in it. All 212 certificates now carry a long-form code, and none carries a short one:

  greencircle_cef                              78
  greencircle_sustainable_energy_practices     67
  greencircle_recycled_content                 48
  greencircle_energy_savings                   19

YOUR EDITS TODAY - WHAT THEY DID TO THE FEED
We compared today's payload against the one from 9/9 so you can see exactly what your team's changes moved. Nothing was unexplained.

- 11 certificates left the feed: 6 Waste Diversion from Landfill, 4 Closed Loop Product, 1 Sustainable Manufacturing Practices. These are 25-8435, 25-8436, 25-8484, 25-8485, 25-9304, 26-0007, 26-0008, 26-0009, 26-0010, 26-0108, 26-0283. Ten were untagged from Amazon and one moved to Draft.
- 7 certificates were re-typed from Carbon Footprint Reduction to Certified Environmental Facts and stayed in the feed: 24-0001, 24-0002, 24-0003, 24-0004, 24-0005, 24-0324, 24-1635.
- Nothing was added, and no other certificate changed.

That takes the feed from 223 certificates to 212.

A NOTE ON WHAT THIS MEANS TOGETHER
Between your edits and this fix, every certificate Amazon now receives carries a code in the long format. Before today, four certification types in the feed were still sending short codes - Carbon Footprint Reduction, Waste Diversion from Landfill, Closed Loop Product and Sustainable Manufacturing Practices - and those would have had the same problem this item just fixed: sent across with a code Amazon doesn't recognise, and silently not badged. Your changes removed all four from the feed, and this fix corrected the fifth.

One thing to keep in mind going forward rather than act on now: the other certification types in the system still hold short codes internally. They cause no problem while Amazon isn't receiving them. But if a certificate of one of those types is ever tagged to Amazon and published, it would go across with a short code and quietly not get badged. If you'd like, tell us which types Amazon should ever receive and the official code for each, and we'll set them now so it can't catch anyone out later.

WHEN YOU RETEST
The API caches responses for five minutes. If you check straight after reading this and still see CEF, give it five minutes - that's the cache expiring, not the change failing.

Ready for retest.
```

---

## Backing evidence (for us, not for the cell)

| Claim in the cell | Where it was proven |
|---|---|
| Was `CEF`, not already set | Read-only pre-check before any write: `Id=5`, `Name='Certified Environmental Facts'`, `AmazonCode='CEF'` — identical on dev and test |
| Now `greencircle_cef` | Guarded UPDATE in a transaction with a post-update assertion that rolls back on mismatch. Both targets: `before=CEF \| after=greencircle_cef \| rows=1`. Re-selected afterwards on test |
| 476 certificates of this type | `SELECT COUNT(*) FROM Certificates WHERE CertificationTypeId = 5` on test |
| No code / no shape change | `amazonCode` is projected straight from `CertificationType.AmazonCode` (`Controllers/Api/v1/CertificatesController.cs:282, 348, 1169, 1776`). Value only — the contract is immutable |
| Five-minute cache | `[ApiCache(CacheProfiles.Medium)]` on the endpoint (`CertificatesController.cs:126`); `Medium = 300` (`Attributes/ApiCacheAttribute.cs:144`) |
| Won't be reverted by a deploy | Nothing in the app writes `CertificationTypes.AmazonCode` — `SeedData.cs` never sets it, no migration seeds it, and the import's `AmazonCode` writes are on `ProductIdEntry`, a different entity |
| 13 types still short-form | Enumerated, not sampled — full list with cert counts in the CR-021 execution note |

**Deliberately not done:** the other thirteen codes were left alone. We would be inventing the
strings, and a wrong partner-facing value is worse than an outdated one. The cell offers to set them
once the client supplies the mapping.

---

## End-to-end proof through the live partner API (added after the re-pull)

The 14:43 payload in `Scripts/data-ops/_amazon_payload/20260910-144252/` **predates the CR-021 update
by ~83 minutes** and still shows `CEF`. **Do not send that one to the client** — it would read as the
fix not having landed. Re-pulled at 16:46 →
`Scripts/data-ops/_amazon_payload/20260910-164647/`.

| Pull | `CEF` | `greencircle_cef` | Short codes present |
|---|---|---|---|
| 9/9 09:47 | 71 | 0 | CFR 7 · WDL 6 · CLP 4 · SMP 1 |
| 9/10 14:43 (pre-CR-021) | 78 | 0 | none |
| **9/10 16:46 (post-CR-021)** | **0** | **78** | **none** |

Membership is identical between the 14:43 and 16:46 pulls (285 tagged / 212 delivered / 73 withheld,
0 findings), so the **only** difference is the code value — the change is isolated and proven.

**The client's edits today, reconciled exactly** (9/9 → today: 223 → 212 delivered, 11 dropped,
0 added, 212 in both):

- **11 left the feed** — 6 Waste Diversion from Landfill, 4 Closed Loop Product, 1 Sustainable
  Manufacturing Practices (`25-8435, 25-8436, 25-8484, 25-8485, 25-9304, 26-0007, 26-0008, 26-0009,
  26-0010, 26-0108, 26-0283`). Tagged count fell 295 → 285 and Draft rose 31 → 32, which accounts for
  all 11.
- **7 re-typed** Carbon Footprint Reduction → Certified Environmental Facts, staying in the feed
  (`24-0001` – `24-0005`, `24-0324`, `24-1635`). These are the `CFR → greencircle_cef` rows.
- Nothing added; no other certificate changed.

**Consequence worth recording:** the client's edits removed the remaining four short-coded types from
the feed and CR-021 fixed the fifth, so **the payload is now 100% long-form**. The thirteen internal
short codes are therefore **latent, not live** — they reach nobody today, and only become a problem if
a certificate of one of those types is Amazon-tagged and published later. That is a weaker finding
than the register's original framing, and the honest one.

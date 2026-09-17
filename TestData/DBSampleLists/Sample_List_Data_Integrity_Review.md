# GCC Sample List Data Integrity Review

**Scope:** Sample list records feeding the Amazon GreenCircle upload template export
**Stack:** ASP.NET Core 8 / MySQL
**Data reviewed (read-only):**

- `SAMPLE ASIN List for Anuki Testing_6.1.26.xlsm` → sheet **GreenCircle Upload Template** — 14,183 product-identifier rows (this is the export-shaped payload).
- `GCC Database SAMPLE Cert Listing for Anuki Tesing_6.2.26.xlsx` → sheets **Active List 4.9.26** (959 certs), **Expired List 4.9.26** (4,374 rows), **Field Options** (allowed values), **Field Mapping** (old→new DB / API).

**Method:** Programmatic profiling of the export files against the allowed-value lists and the field-mapping "Pass to Amazon = Yes" contract. No data was modified. Because these are the Excel exports the DB emits for the Amazon GreenCircle upload, the SQL in the final section is provided so the same checks can be run directly against MySQL for verification.

> Ticket context confirmed: product identifiers (#184, CR-171), product type/subcategory (#148), and multi-partner/cert data (CR-186) are exactly where the defects cluster.

---

## Schema & relationships (as reconstructed from the field mapping)

The sample-list data is a three-level structure. Inferred MySQL tables and the export/API field they map to:

| Entity (inferred table) | Key | Feeds export field(s) |
|---|---|---|
| `certificates` | PK `certificate_number` | `certificateNumber, company, certificationType, issueDate, expirationDate, status, certificationResults, pathway, certificationCountries, issuer` |
| `product_identifiers` | PK `id`, FK `certificate_number` | `type` (ASIN), `value` (ASIN code), `productName`, `productSku`, product `status`, `partner` |
| `certificate_subcategories` | FK `certificate_number` | multi-value subcategories (CSF/WD certs only) |
| lookups: `product_category`, `product_type`, `product_subcategory`, `certification_type` | code PK | validate category/type/subcategory/cert-type |

The upload template is effectively `product_identifiers ⋈ certificates ⋈ backend fields`. **Relationship integrity to verify: every `product_identifiers.certificate_number` must resolve to a `certificates` row, and only Amazon-partner identifiers should be exported.**

**Constraint gaps to confirm in DDL (recommend, do not assume present):** unique key on `certificates.certificate_number`; FK `product_identifiers.certificate_number → certificates.certificate_number`; `NOT NULL` on `value, type, certificate_number, product_category, product_type`; unique composite `(certificate_number, value, partner)` on `product_identifiers` to stop duplicate badge rows (see D1).

---

## Findings by severity

### 🔴 BLOCKERS — will break or corrupt the export / badge assignment

**B1 — Misspelled certification type on 55.7% of export rows**
`certificationType = "Certified Environmenal Facts"` (missing the "t" in *Environmental*) on **7,904 / 14,183** rows. It does not match the allowed value `Certified Environmental Facts` in Field Options. The backend `amazonCode` is the correct `greencircle_cef`, so the badge may still map, but every human-facing/report field and any validation keyed on the display name is wrong.
*Table/col:* `certificates.certification_type` (export `certificationType`). *Example:* rows across the file, e.g. cert `25-8483` family. *Fix:* correct the single lookup/source value `Certified Environmenal Facts → Certified Environmental Facts`; re-export. One-row source fix cascades to all 7,904.

**B2 — Invalid product identifiers (`value`) that are not ASINs — 14 rows**
Placeholder/garbage in the field Amazon matches on:

| Excel row | value | cert# | company |
|---|---|---|---|
| 2109, 2111 | `NOT LAUNCHED` | 25-8161 | Procter & Gamble |
| 4154, 4157 | `SHAVING` | 25-8161 | Procter & Gamble |
| 8346, 8392–8692 (8 rows) | `To be Checked` | 24-1633 / 24-1634 | Philips |
| 12756 | `08FDWPMP6` (missing `B0` prefix) | 23-1046 | Philips |
| 12767 | `?B0937DT67K` (stray `?`) | 23-1046 | Philips |

*Table/col:* `product_identifiers.value`. *Fix:* reject/repair before export — `08FDWPMP6`→ likely `B08FDWPMP6`, `?B0937DT67K`→`B0937DT67K`; the `NOT LAUNCHED` / `SHAVING` / `To be Checked` rows must be resolved with the client or excluded from the export.

**B3 — Orphaned certificate references — 2 certs, 51 export rows**
`certificateNumber` **25-0437** and **25-0438** appear in the ASIN export but exist in neither the Active nor the Expired master cert listing (e.g. rows 9166–9172, company *X-Sense*). These badge rows reference a certificate the DB cannot resolve.
*Table/col:* `product_identifiers.certificate_number` with no `certificates` parent. *Fix:* add the missing certificate records or remove the 51 identifier rows; add the FK constraint to prevent recurrence.

**B4 — `pdfDownloadUrl` empty on 100% of rows (14,183)**
This field is flagged **Pass to Amazon = Yes** in the mapping but is blank for every row. If the export contract requires it, the payload is incomplete.
*Table/col:* backend `pdfDownloadUrl`. *Fix:* confirm whether this is backend-populated post-export; if it is required at export time, wire up the source. Flagged Blocker pending that confirmation — see Q1.

---

### 🟠 DATA ISSUES — wrong or redundant data, export may still run

**D1 — Duplicate badge rows — 2,178 exact-duplicate rows; 3,280 redundant `(value + certificateNumber)` rows**
2,287 ASIN+certificate pairs repeat, some up to 14× (e.g. `B0BWKBKD9S` + `23-1046` ×14). Even if the target dedupes, this inflates the file and risks conflicting attributes per pair.
*Table/col:* `product_identifiers (value, certificate_number)`. *Fix:* de-duplicate; add unique composite key `(certificate_number, value, partner)`.

**D2 — Active status on expired certificates — 47 rows**
`status = Active` but `expirationDate` is in the past (as of 2026-07-06), e.g. rows 9618–9623, certs 25-0484/85/86, expired 2026-06-15. Expired products would keep a live badge.
*Table/col:* `certificates.status` vs `expiration_date`. *Fix:* recompute status from expiration date (job or generated column); exclude expired from export.

**D3 — Internal identifier format inconsistency (`certificationName` vs `amazonCode`) — 526 rows**
For Sustainable Energy Practices, `certificationName = greencircle_sep` while `amazonCode = greencircle_sustainable_energy_practices`. The two should be the same backend slug. Ties directly to the CR-171/#184 identifier problems.
*Table/col:* export `certificationName`. *Fix:* standardize the slug; `certificationName` should equal `amazonCode`.

**D4 — Whitespace / case in `value` — 28 whitespace, 8 lowercase**
Includes **non-breaking spaces** (`\xa0`) that are invisible in Excel — rows 1926–1928, 2149–2151 (`B08P4JYCQM\xa0`), a trailing regular space at row 4447 (`B099KL2C84 `). Exact-match on ASIN will silently fail.
*Table/col:* `product_identifiers.value`. *Fix:* `TRIM` + strip `\xa0`, uppercase-normalize on ingest and pre-export.

**D5 — Missing product classification in Active cert list**
`Product Category` empty on **68/959**, `Product Type` empty on **78/959**, plus 17 rows literally `"N/A"` in both. Product Name (`PRODUCT`) empty on 17; `EXPIRATION_DATE` empty on 2.
*Table/col:* `certificates.product_category / product_type / product_name / expiration_date`. *Fix:* backfill from the client cert; make category/type `NOT NULL` for export-eligible certs.

**D6 — Mis-numbered product-category codes — 5 rows (drives 6 hierarchy mismatches)**
Category label carries the wrong Level-1 number, so the type no longer nests under it:

| cert# | Product Category (as stored) | Product Type | Correct category |
|---|---|---|---|
| 25-8378 | `4 Construction / Building Products` | 3.10 Insulation | `3 Construction / Building Products` |
| 25-0354 | `7 Food and Beverage` | 6.3 Food | `6 Food and Beverage` |
| 26-0225 | `9 Packaging` | 8.2 Plastic | `8 Packaging` |
| 26-0211 | `3 Construction / Building Products` | 1.5 Wood | category or type wrong — review |
| 25-9287 | `4 Construction / Building Products` | 3.4 Concrete | `3 Construction / Building Products` |
| 25-8319 | `2 Chemicals and Raw Materials` | 1.4 Plastics | `1 Chemicals and Raw Materials` |

*Table/col:* `certificates.product_category`. *Fix:* correct the numeric prefix to match the canonical list in Field Options; enforce that `product_type` prefix begins with `product_category` prefix + ".".

---

### 🟡 OBSERVATIONS — expected or low-risk, note for confidence

- **O1 — `pathway` empty on 96.3%** (13,657 rows). Expected: pathway applies only to Sustainable Energy Practices certs. The 526 SEP rows should carry it — worth confirming they do.
- **O2 — Active cert list is clean where it matters:** `certificate_number` is unique (no PK collisions), all 149 certificate-subcategory values validate against the allowed CSF/WD list, and all certification types are valid. Multi-value cert-subcategory columns resolve correctly.
- **O3 — Expired List uses the legacy free-text schema** (`PRODUCT_TYPE` with no category hierarchy) and contains ~155 repeated certificate numbers. It is not part of the export scope; flag only so it isn't migrated as-is.
- **O4 — Constant columns** in the export (`type=ASIN`, `issuer=GreenCircle Certified`, `certificationCountries=All`, `status=Active`) are internally consistent; confirm the all-Active constant is intended given D2.

---

## Export-readiness verdict

**Not ready.** Blockers B1–B3 must be fixed before this export goes to the Amazon GreenCircle upload: 7,904 rows carry a misspelled cert type, 14 rows have non-ASIN identifiers, and 51 rows reference non-existent certificates. B4 (empty `pdfDownloadUrl`) is a blocker pending confirmation of the export contract. After those, D1 (dedupe) and D2 (expired-as-active) should be cleared to avoid a bloated file and stale badges.

---

## SQL for re-running against MySQL

> Adjust table/column names to the actual schema. These assume `certificates`, `product_identifiers`, and a `certification_type` lookup. They reproduce every check above.

```sql
-- B1: certification type not in the allowed lookup (catches the misspelling)
SELECT c.certification_type, COUNT(*) AS rows_affected
FROM certificates c
LEFT JOIN certification_type ct ON ct.name = c.certification_type
WHERE ct.name IS NULL
GROUP BY c.certification_type;

-- B2 + D4: product identifier not a well-formed ASIN, or containing whitespace/lowercase/nbsp
SELECT pi.id, pi.certificate_number, pi.value
FROM product_identifiers pi
WHERE pi.type = 'ASIN'
  AND ( pi.value NOT REGEXP '^B0[0-9A-Z]{8}$'
        OR pi.value <> TRIM(pi.value)
        OR pi.value LIKE '% %'          -- non-breaking space
        OR BINARY pi.value <> UPPER(pi.value) );

-- B3: orphaned certificate references (no parent certificate)
SELECT DISTINCT pi.certificate_number
FROM product_identifiers pi
LEFT JOIN certificates c ON c.certificate_number = pi.certificate_number
WHERE c.certificate_number IS NULL;

-- B4: export field required but empty
SELECT COUNT(*) FROM product_identifiers WHERE pdf_download_url IS NULL OR pdf_download_url = '';

-- D1: duplicate badge rows
SELECT certificate_number, value, partner, COUNT(*) AS copies
FROM product_identifiers
GROUP BY certificate_number, value, partner
HAVING COUNT(*) > 1
ORDER BY copies DESC;

-- D2: status Active but expired
SELECT certificate_number, status, expiration_date
FROM certificates
WHERE status = 'Active' AND expiration_date < CURDATE();

-- D3: certificationName (slug) not matching amazonCode slug
SELECT certification_name, amazon_code, COUNT(*)
FROM product_identifiers pi
JOIN certificates c ON c.certificate_number = pi.certificate_number
WHERE pi.certification_name <> c.amazon_code
GROUP BY certification_name, amazon_code;

-- D5: missing required classification / names / dates
SELECT certificate_number,
       (product_category IS NULL OR product_category IN ('','N/A')) AS cat_missing,
       (product_type     IS NULL OR product_type     IN ('','N/A')) AS type_missing,
       (product_name IS NULL OR product_name = '')                  AS name_missing,
       (expiration_date IS NULL)                                    AS exp_missing
FROM certificates
HAVING cat_missing OR type_missing OR name_missing OR exp_missing;

-- D6: product_type prefix must nest under product_category prefix
SELECT certificate_number, product_category, product_type
FROM certificates
WHERE product_category REGEXP '^[0-9]'
  AND product_type     REGEXP '^[0-9]'
  AND SUBSTRING_INDEX(product_type,' ',1)
      NOT LIKE CONCAT(SUBSTRING_INDEX(product_category,' ',1), '.%');
```

## Recommended fix sequence (each as a separate reviewed change — no in-place edits made here)

1. Correct the `Certified Environmenal Facts` lookup value (B1) — highest impact, single source row.
2. Add the missing certificates 25-0437 / 25-0438 or drop the 51 orphan identifier rows (B3), then add the FK.
3. Repair/exclude the 14 non-ASIN identifiers (B2) and normalize whitespace/case (D4).
4. Confirm the `pdfDownloadUrl` contract (B4).
5. Dedupe identifiers + add unique `(certificate_number, value, partner)` (D1).
6. Recompute status from expiration (D2); backfill classification (D5); fix category numbers (D6).

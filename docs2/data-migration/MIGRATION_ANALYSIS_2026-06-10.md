# GCC Data Migration — Sample Data Analysis

**Date:** 2026-06-10
**Analyst:** Anuki Consulting
**Stage:** Analysis & reporting only — **no INSERT/UPDATE/migration scripts run.**
**Source files (client samples):**
- `TestSampleData/GCC Database SAMPLE Cert Listing for Anuki Tesing_6.2.26.xlsx` — certificate master records
- `TestSampleData/SAMPLE ASIN List for Anuki Testing_6.1.26.xlsm` — certificate → product-ID (ASIN) map

> These are **samples representative of the client's typical export format**, not the full dataset. Numbers below describe the samples; the production export will be larger and may surface additional value variants.

---

## 1. What the two files are (and how they join)

| File / sheet | Data rows | Role in target system |
|---|---|---|
| Cert Listing → `Active List 4.9.26` | **959** | Live certificates (Companies, Products, Certificates, hierarchy, tags) |
| Cert Listing → `Expired List 4.9.26` | **4,374** | Expired certificates (older/flatter schema) |
| Cert Listing → `Field Options` | (lookup) | Client's controlled vocabularies (categories, cert types, tags, subcats) |
| Cert Listing → `New DB & Old DB Field Mapping` | (lookup) | **Client's own old→new field crosswalk** — authoritative for mapping |
| ASIN List → `GreenCircle Upload Template` | **14,183** | Product identifiers → maps to **Certificate Product IDs** (table 08), joined on `certificateNumber` |

**Join key:** `CERTIFICATE_NUMBER` (cert listing) = `certificateNumber` (ASIN list).
The ASIN list is already shaped to the **Amazon partner API field names** (`type`, `value`, `amazonCode`, `pathway`, `issuer`, `certificationCountries`, `pdfDownloadUrl`, …), so it maps almost 1:1 to table **08 Certificate Product IDs**.

**Target import order** (per `DATA_MIGRATION_GUIDE.md`): Certification Types → Categories → Types → Subcategories → Companies → Products → Tags → **Certificates** → **Certificate Product IDs**.

---

## 2. Volumes & distinct entities (samples)

| Metric | Active | Expired | ASIN list |
|---|---|---|---|
| Data rows | 959 | 4,374 | 14,183 |
| Distinct companies | 179 | 273 | 31 |
| Distinct cert numbers | 959 | 4,250 (124 cert#s appear on >1 row) | 100 |
| Distinct cert types | 15 | 17 (incl. retired types) | — |

- **ASINs per certificate:** min 1, max **2,440** (cert `24-0300`), mean ~142. Top 5 certs carry ~7,200 of the 14,183 rows.
- Active vs Expired cert-number overlap: **0** (clean split).

---

## 3. Field mapping (from the client's own "Field Mapping" sheet)

| Old DB / source | New cert-page field | API field | Pass to Amazon | Migration note |
|---|---|---|---|---|
| CERTIFICATE_NUMBER | Certification # | `certificateNumber` | Yes | direct |
| COMPANY | Company | `company` | Yes | direct |
| PRODUCT | Product Name | — | — | direct |
| PRODUCT_TYPE | Product Category | — | — | **Reassign to new 3-level hierarchy** |
| CERTIFICATE_TYPE / Type | Certification Type | `certificationType` + `amazonCode` | Yes | maps to backend code, e.g. `Sustainable Energy Practices` → `greencircle_sustainable_energy_practices` |
| EFFECTIVE_DATE | Effective Date | `issueDate` | Yes | direct |
| EXPIRATION_DATE | Expiration Date | `expirationDate` | Yes | direct |
| LOCATION (city,state,country) | Location | — | — | **Strip country into Country field** |
| STATUS | Status | — | — | map to 8-state workflow |
| Contact | Company Email / URL | — | — | mostly **must be manually sourced** |
| *(new)* | Certificate Subcategories | — | — | CSF & WD only |
| *(new)* | Product Type / Subcategory | — | — | add per new hierarchy |
| *(new)* | Partner | `partner` | — | flag Amazon; only Amazon-flagged are passed |
| *(new)* | Certification Results | `certificationResults` | Yes | optional |
| *(new)* | Pathway | `pathway` | Yes | **SEP / Amazon CPF only** |
| *(new, backend)* | — | `issuer`, `certificationCountries`, `pdfDownloadUrl` | Yes | system-generated, not client-input |

ID sub-fields (under Product Identifiers): `type` (e.g. ASIN), `value` (e.g. B08…), `productName`, `productSku`, `status`, `partner`.

---

## 4. Data-quality findings (by severity)

### 🔴 Blockers — resolve before load

| # | Finding | Detail | Recommended action |
|---|---|---|---|
| B1 | **Orphan product IDs** | ASIN certs `25-0437`, `25-0438` exist in the ASIN list but in **neither** Active nor Expired listing → no parent certificate | Obtain the 2 missing cert records, or hold their ASINs out of load |
| B2 | **Invalid cert# format** | Expired sheet: `6H3-5692`, `6H3-7120`, `6H3-6301` (not `YY-NNNN`); **1 blank** cert# row | Correct or exclude; confirm `6H3-` is not a legacy numbering scheme |
| B3 | **Junk ASIN values** | 14 rows where `value` is not an ASIN: `NOT LAUNCHED`, `SHAVING`, `To be Checked` | Drop or hold — these will fail ASIN validation / Amazon |
| B4 | **Two different schemas** | Active sheet has Country, 3-level hierarchy, tags, partner, contacts; **Expired sheet is the old flat layout** (freetext `PRODUCT_TYPE`, country baked into `LOCATION`, no tags/partner) | Normalize Expired to the new schema before load |

### 🟠 Major — decisions needed

| # | Finding | Detail | Recommended action |
|---|---|---|---|
| M1 | **Status → 8-state mapping** | Active statuses: `Client Approved - Paid` (920), `Client Approved - Confidential` (39); Expired: `Expired` (4,374) | Confirm: Paid → public state; **Confidential (39) must NOT be public**; Expired → Expired. See `Models/CertificateStatus.cs` |
| M2 | **Product hierarchy reassignment** | Active uses new numbered taxonomy (`3 Construction / Building Products` → `3.5 Doors…`); **Expired uses old freetext types** ("Door Closers", "Printer", "Emulsion"…). Active still has gaps: 68 missing Category, 78 missing Product Type, only 56 Subcategory | Build a freetext→hierarchy crosswalk for Expired; fill Active gaps |
| M3 | **Retired cert types in Expired** | Not in current `Field Options` vocab: `Biodegradable` (58), `Biobased` (17), `Renewable Resource Content` (8), `Verified Health Product Declaration` (3) | Decide: map to a current type, or load as historical/retired |
| M4 | **ASINs spanning multiple certs** | 276 ASINs appear on **more than one** certificate number (badge-conflict risk for Amazon) | Define precedence (which cert badges the ASIN) before passing to Amazon |
| M5 | **Country normalization** | `Country` contains: `-` placeholder (39), multi-country (`United States, Canada`; `Hungary; Germany`; `United States; China; Poland`), `The Netherlands` (vs Netherlands), `Wales` (vs United Kingdom), 1 blank | Standardize to single canonical country names per guide |

### 🟡 Minor — clean during transform

| # | Finding | Detail |
|---|---|---|
| N1 | **ASIN row redundancy** | 2,088 ASINs repeat **within the same cert** (e.g. `B0BWKBKD9S` ×14 on `23-1046`) — dedupe per (cert, ASIN) |
| N2 | **Expired duplicate cert# rows** | 124 cert#s span >1 row: 87 are legitimately multi-product; **37 carry a duplicated product** incl. case variants (`MAPEI Ultrabond ECO 995` vs `…Eco 995`) — dedupe |
| N3 | **`productName` has embedded pipes** | Variant text stored as `Product | Black | High Yield`. Harmless as data, but **escape if `|` is ever a delimiter** in import |
| N4 | **Redundant columns (ASIN file)** | `certificationName` duplicates `amazonCode` (both = backend code); `type`/`status`/`issuer`/`certificationCountries` are constant (`ASIN`/`Active`/`GreenCircle Certified`/`All`) |
| N5 | **Multi-location strings** | Active `Location` has 125 rows with `;` (e.g. `Memphis, TN; Ventura, CA`) and 10 still containing a country token — confirm target supports multi-location or pick primary |
| N6 | **Date edge cases** | Active: 2 blank expiration (→ perpetual `2099-12-31`). Expired: 5 blank effective, 14 blank expiration, **1 expiration < effective** |
| N7 | **`certificationResults` format** | Stored as ratio (e.g. `0.86`) in ASIN file; free text in Active sheet (561/959 filled). Confirm display format (ratio vs %) |
| N8 | **Sparse optional fields (Active)** | Partner 92/959, Pathway 27/959, External Tags 234/959, Internal Tags 4/959, Contact Email 117/959, Contact URL 13/959 — contacts largely absent, as the client flagged |

### ✅ Verified clean (no action)
- **Encoding is fine** — `®` and other symbols are correctly stored (UTF-8, `0xAE`); **no mojibake**. Just import as UTF-8. *(The `�` seen in raw terminal dumps was console rendering, not corrupted data.)*
- Active cert numbers: 100% valid `YY-NNNN`, no blanks, no in-sheet duplicates.
- ASIN list internally consistent: all `type=ASIN`, all `status=Active`, all `issuer=GreenCircle Certified`.

---

## 5. Open questions for the client

1. **Confidential certs (39):** confirm they load but stay **non-public** (which of the 8 states?).
2. **`6H3-####` cert numbers (3):** legacy scheme to keep, or data error to fix?
3. **Orphan ASIN certs `25-0437` / `25-0438`:** provide the cert records, or hold the ASINs?
4. **Retired cert types** (Biodegradable, Biobased, Renewable Resource Content, Verified HPD): remap or load as historical?
5. **ASIN-on-multiple-certs (276):** precedence rule for which cert badges the product on Amazon?
6. **Multi-country / multi-location:** does the target model support multiple, or do we pick a primary?
7. **`certificationResults`:** required display format (ratio `0.86` vs `86%`)?
8. **Contacts (email/URL):** confirmed to be sourced manually post-load (mostly empty in sample)?

---

## 6. Recommended next steps (still pre-load)

1. Send §5 open questions to the client.
2. Draft an **Expired-sheet → new-schema** normalization map (M2) and a **freetext-type → hierarchy** crosswalk.
3. Build a **status → 8-state** lookup (M1) and a **cert-type → `amazonCode`** lookup from the Field Mapping sheet.
4. Stage a **dry-run validation pass** (format/required/FK checks) against the import templates — report only, no writes — before any load is authorized.

> Per project rules: any UPDATE/DELETE on real business records requires a formal GCC Change Request. This document is analysis only.

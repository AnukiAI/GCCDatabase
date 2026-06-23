# Migration import-transform plan (2026-06-22)

**Author:** Anuki Consulting · **Scope:** load cleaned + decided source data into the GCC database
**Inputs:** cleaned files from `migration-remediation-plan-2026-06-22.md` + the seven client decisions
**Environment:** **dev/localhost first** ([[feedback_testing_environment]]); prod only after sign-off, via the pre-deploy gate ([[project_predeploy_agent_checklist]])
**Status:** DRAFT — blocked on the open decisions in §8. No load executed.

---

## 1. Strategy — reuse the existing import pipeline

The app already has a proven importer; we transform the source data into **its** contracts rather than writing a new loader.

- **Certificate records** → existing CSV import: `CertificateImportController` POST `/CertificateImport/Upload` → `CertificateImportService` (`ParseCsvFileAsync` :711, `CreateCertificateAsync` :1147). It already does case-insensitive **find-or-create** for Company (:900), Product w/ CR-186 full-identity resolution (`ResolveProductForImportAsync` :785), ProductType (:917), Tags (:955); **partner sync** (`SyncCertificatePartnersAsync` :1218); and accepts 3 header conventions.
- **Product IDs (ASINs)** → product-id bulk import: `ProductIdService.ImportProductIdsAsync` (:524) / `CreateProductIdEntriesAsync` (:1341), per-cert Type+Value dedup, status auto-derived from parent cert (CR-015).

Two load channels because the data shape demands it: ~98 in-scope certs carry ~11,984 product IDs — far more than the single ASIN/UPC columns on the cert CSV can hold, so product IDs go through the per-certificate bulk channel keyed by cert#.

**Importer columns (cert CSV):** `Certificate #, Certification Type, Cert Subcategories, Status, Company, Company Email, Company URL, Product Name, Product Category, Product Type, Subcategory, Partner, Pathway, Cert Results, Effective Date, Expiration Date, Location, Country, External Tag, Internal Tag, ASIN, UPC, GTIN, EAN`

---

## 2. Prerequisites (before any load)

| # | Prereq | Why |
|---|---|---|
| P1 | **Seed CertificationTypes** for the full controlled vocab **+ the 4 retired types** (Biodegradable, Biobased, Renewable Resource Content, Verified Health Product Declaration), the retired ones marked **deactivated**. | Importer does **NOT** auto-create cert types (`ProcessRowAsync` :947 throws if missing). The expired-cert rows using retired types would fail otherwise. Matches the 06-11 client default. |
| P2 | **Confirm Partner "Amazon" exists** (core entity — it does) and any others referenced. | Partner sync does **not** auto-create (:1218); unmatched names are silently dropped. |
| P3 | **If client approves widening productName (decision 1):** run the model+SQL migration to enlarge `CertificateProductId.ProductName` (200 → 1000) and deploy it to the target DB **first**. | 88 product names exceed 200; otherwise truncated/rejected. Prod never auto-migrates — Gate 7. |
| P4 | **Pathway ingestion — VERIFIED 2026-06-22: the importer does NOT ingest Pathway.** No `PathwayColumns` mapping in `CertificateImportService` (cols :58–90), zero `Pathway` references anywhere in `Services/Import/`, and `CreateCertificateAsync` never sets `PathwayId` → it always imports as NULL. The Pathway lookup rows **already exist** (seeded by migration `20260213000922_AddPathwayTable`: "Energy usage reduction", "Onsite fossil fuel usage reduction", "Renewable electricity usage"), and the source uses exactly two of them ("Renewable electricity usage" ×20, "Energy usage reduction" ×7), so match-by-name needs no extra seeding. | 27 active certs carry a pathway (field-map marks it Pass-to-Amazon=Yes). **Fix required — two options below.** Note: pathway is not strictly SEP/Amazon-only in the data (7 non-SEP, 10 non-Amazon rows carry one). |

**P4 fix options:**
- **(Recommended — IMPLEMENTED 2026-06-22)** Extended the importer: `PathwayColumns` mapping, parsed in `ParseCsvFileAsync`, resolved case-insensitively against the seeded `Pathways` table in `ProcessRowAsync` (non-blocking `_logger` warning if unmatched — does not fail the row), set on `Certificate.PathwayId` in `CreateCertificateAsync`, conditionally applied in `UpdateCertificateAsync` (a blank/absent column never clears an existing pathway), audit-snapshotted, and added to `DetectChanges` (guarded). Models updated in `CertificateImportModels.cs`. Build clean, 0 errors. **Runtime test on localhost PASSED 2026-06-22:** 3-row CSV imported via `/api/CertificateImport` Preview+Apply (admin) → `Renewable electricity usage`→PathwayId 3, `Energy usage reduction`→PathwayId 1, and a deliberately bogus value imported with PathwayId NULL + the expected non-blocking log warning (row not failed). Verified by direct DB query; test data cleaned up afterward.
- **(No code) Post-load SQL** — after import, `UPDATE Certificates SET PathwayId = (SELECT Id FROM Pathways WHERE Name = @v) WHERE Number IN (...)` for the 27 certs. Fast but non-durable (every future import re-drops pathway).

**Related gap (same verification) — FIXED 2026-06-22:** the importer previously **silently ignored the Internal Tag column** (external tags only). Now extended: `InternalTagsColumns` mapping → parsed → matched/created with `TagType="internal"` (parallel pipeline to external), attached to the same `Certificate.Tags` M2M. Key schema constraint discovered during testing: **`Tags.Name` is globally unique (`IX_Tags_Name`)** — a name maps to exactly one tag regardless of type. So internal matching is by name across all types (reuse + cross-type warning), brand-new names colliding across both columns are created once (external precedence) with an attach-time fallback, preventing a duplicate-key crash. **Runtime test on localhost PASSED:** existing internal tag matched, new internal tag auto-created as `internal`, and an internal value colliding with an existing external tag reused it (no crash) — all verified by DB query; test data cleaned. (Conversely, the importer **does** map `pdfDownloadUrl` → PDF path at :90 — useful for a future PDF load.)
| P5 | **Start from a known dev DB state** (clean or snapshot). | Importer can update-by-cert# (:1325); a known baseline makes reconciliation counts exact. |

---

## 3. Load order (dependency graph)

1. Prereqs P1–P3 (lookups + schema).
2. **Active certs** → cert CSV import.
3. **Expired certs** → cert CSV import.
4. **Product IDs** → per-cert bulk import (requires certs from steps 2–3 to resolve cert# → CertificateId).
5. Post-load: Pathway backfill (P4 if needed), category backfill (analyst), reconciliation (§6).

---

## 4. Transform A & B — cert listings → cert CSV

### A. Active List (NEW schema, 959 rows) — near 1:1

| Source column | → Import CSV column | Notes |
|---|---|---|
| CERTIFICATE_NUMBER | Certificate # | natural key |
| COMPANY | Company | find-or-create; apply decision 3 (Block, Inc.) |
| PRODUCT | Product Name | CR-186 full-identity resolve |
| Product Category / Type / Subcategory | Product Category / Product Type / Subcategory | already cleaned (prefixes fixed); 85 blank → analyst backfill, not blocking |
| CERTIFICATE_TYPE | Certification Type | must exist (P1) |
| EFFECTIVE_DATE / EXPIRATION_DATE | Effective Date / Expiration Date | pre-format `yyyy-MM-dd` ([[feedback_calendar_date_serialization]]) |
| Location (no country) / Country | Location / Country | already split; cap Location 175 / Country 100 |
| STATUS | Status | "Client Approved - Paid" / "- Confidential" map verbatim to the 8-state enum |
| Partner (Amazon) | Partner | only "Amazon" rows; blank → none |
| Certification Results | Cert Results | string; `CertificationResultsNumeric` auto-computes |
| Pathway | Pathway | see P4 |
| External Tags / Internal Tags | External Tag / Internal Tag | external auto-creates; internal per [[feedback_custom_fields_single_section]] rules |
| Contact Email / URL | Company Email / Company URL | used when creating the Company |

### B. Expired List (OLD schema, 4,363 rows post-dedup) — lossy transform

| Source column | → Import CSV column | Notes |
|---|---|---|
| CERTIFICATE_NUMBER | Certificate # | **114 residual shared numbers blocked on decision 4** |
| COMPANY | Company | find-or-create |
| PRODUCT | Product Name | |
| **PRODUCT_TYPE** (single free-text) | *(unmapped)* | Does **not** map to the new category hierarchy ("reassign per new hierarchy" per the field-map sheet). **Leave Product Category/Type/Subcategory blank** for expired/historical; optional analyst reassignment later. Do **not** force-fit. |
| CERTIFICATE_TYPE | Certification Type | incl. retired types (P1, deactivated) |
| EFFECTIVE_DATE / EXPIRATION_DATE | Effective Date / Expiration Date | serials already coerced; blanks allowed for expired |
| **LOCATION** (combined) | Location + Country | **split required**: strip trailing country into Country, remainder → Location. Where only "City, ST" present, Country stays blank (acceptable). |
| STATUS = "Expired" | Status | → `Expired` |

**Expired-specific risks:** no Partner/CertResults/Pathway/Tags columns (all blank — fine); `6H3-####` cert numbers import as-is (string field); the single `expiration<effective` row needs a date sanity check before load.

---

## 5. Transform C — ASIN list → product IDs

- **Group cleaned ASIN rows by Certificate #** (98 in-scope; orphans 25-0437/25-0438 **held** per 06-11 decision).
- For each group: resolve cert# → CertificateId, then bulk-import that cert's product IDs (`type`→ProductIdType, `value`→ProductIdValue, `productName`→ProductName, `productSku`→ProductSku).
- **Authoritative-source rule (remediation plan §1):** cert-level fields (company, cert type, dates, results) come from the **cert record already loaded in steps 2–3**, NOT from the ASIN row. ASIN-row cert-level values are ignored — this is what resolves the 13 `ASIN-CONFLICT` cases at load time (except the 24-1634 result, decision 2).
- **Status:** ignore the file's "Active" — `DeriveStatusFromCert` sets it from the parent (CR-015). Run `ProductIdService.SyncProductIdStatusesForCertificateAsync` after.
- **Partner on product IDs:** apply CR-171 model — Amazon-tagged where the file marks Amazon; untagged = visible to all the cert's partners ([[feedback_partner_data_isolation]]).
- `certificationResults` per ASIN row is cert-level; carried on the Certificate (from listing), not per product ID.

---

## 6. Reconciliation & acceptance (dev)

- **Counts:** Active 959 + Expired (4,363 − resolved dups) certs; product IDs = 11,984 − (51 orphan rows held) = **11,933** across ~98 certs. Assert post-load counts match the transform output.
- **Spot checks:** 24-1634 result = the decided value; Block/Square certs under one company; a coerced-date expired cert; an Amazon SEP cert has its pathway; retired-type expired cert loaded + type deactivated.
- **Amazon contract diff:** run `Scripts/smoke-prod/amazon-contract-diff.js` — must print **CONTRACT UNCHANGED** ([[feedback_amazon_api_immutable]], Gate 4).
- **Re-run the validation gate** on the transform's CSV outputs → expect 0 blockers.
- **Status history:** importer does **not** create an initial history row (only on change). **Decision:** seed one baseline "migrated" entry per cert, or accept no history for migrated records. *Recommend* a single baseline entry for audit continuity.

## 7. Execution mechanism & rollback

- **Mechanism:** a transform script (emits import-ready CSV + per-cert product-id batches) → load via the existing import UI/endpoints on **localhost**. No bespoke DB writes.
- **PDFs are out of scope here** — source carries no PDF refs (`pdfDownloadUrl` blank); cert documents load separately and relate to the missing-PDF incident, not this data load.
- **Rollback (dev):** restore the pre-load DB snapshot (P5). **Prod:** full backup incl. `wwwroot/uploads/` before any run ([[feedback_backup_uploads_before_deploy]]); DB-change gating per Gate 7.

## 8. Open dependencies (must close before load)

- **Client decisions 1–4** (`migration-client-note-2026-06-22.md` §4) + the **three from 06-11** (CEF code, retired types, orphan ASINs).
- **P4 Pathway-ingestion FIX** (engineering, not client) — gap **confirmed**; pick importer-extension (recommended) vs post-load SQL. Plus the **Internal Tag** ingestion gap (same verification).
- **Tie-break rule for residual duplicate expired cert numbers** (decision 4) — needed before those expired certs can load uniquely.
- **Status-history baseline** decision (§6).

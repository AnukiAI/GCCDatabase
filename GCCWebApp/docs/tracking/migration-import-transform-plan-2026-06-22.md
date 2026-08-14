# Migration import-transform plan (2026-06-22)

**Author:** Anuki Consulting · **Scope:** load cleaned + decided source data into the GCC database
**Inputs:** cleaned files from `migration-remediation-plan-2026-06-22.md` + the seven client decisions
**Environment:** **dev/localhost first** ([[feedback_testing_environment]]); prod only after sign-off, via the pre-deploy gate ([[project_predeploy_agent_checklist]])
**Status:** DRAFT — data load blocked on the open decisions in §8. No load executed.
**Updated 2026-08-12:** added **§9 Certificate PDF corpus** — a public 1021-PDF mirror now exists,
covering **705 of the 959 active certs (73.5%)** and **0 of the expired**. PDF-path testing can
start now; the credentialed pull for the rest is prepped in §9.4. Added **§10 PDF link step** —
the post-load script that attaches those PDFs to the migrated certificates. §10 is **not blocked
by §8** and can be built and rehearsed now.

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
| P3 | **DONE (dev) 2026-06-24 — client confirmed "widen, don't truncate."** `CertificateProductId.ProductName` widened 200 → 1000 (model + migration `20260624142128_WidenCertificateProductIdProductName`, applied to dev DB, verified `nvarchar(1000)`). **Still must run this migration on prod at deploy** (prod never auto-migrates — Gate 7). | 88 product names exceed 200 (longest 951). |
| P4 | **Pathway ingestion — VERIFIED 2026-06-22: the importer does NOT ingest Pathway.** No `PathwayColumns` mapping in `CertificateImportService` (cols :58–90), zero `Pathway` references anywhere in `Services/Import/`, and `CreateCertificateAsync` never sets `PathwayId` → it always imports as NULL. The Pathway lookup rows **already exist** (seeded by migration `20260213000922_AddPathwayTable`: "Energy usage reduction", "Onsite fossil fuel usage reduction", "Renewable electricity usage"), and the source uses exactly two of them ("Renewable electricity usage" ×20, "Energy usage reduction" ×7), so match-by-name needs no extra seeding. | 27 active certs carry a pathway (field-map marks it Pass-to-Amazon=Yes). **Fix required — two options below.** Note: pathway is not strictly SEP/Amazon-only in the data (7 non-SEP, 10 non-Amazon rows carry one). |

**P4 fix options:**
- **(Recommended — IMPLEMENTED 2026-06-22)** Extended the importer: `PathwayColumns` mapping, parsed in `ParseCsvFileAsync`, resolved case-insensitively against the seeded `Pathways` table in `ProcessRowAsync` (non-blocking `_logger` warning if unmatched — does not fail the row), set on `Certificate.PathwayId` in `CreateCertificateAsync`, conditionally applied in `UpdateCertificateAsync` (a blank/absent column never clears an existing pathway), audit-snapshotted, and added to `DetectChanges` (guarded). Models updated in `CertificateImportModels.cs`. Build clean, 0 errors. **Runtime test on localhost PASSED 2026-06-22:** 3-row CSV imported via `/api/CertificateImport` Preview+Apply (admin) → `Renewable electricity usage`→PathwayId 3, `Energy usage reduction`→PathwayId 1, and a deliberately bogus value imported with PathwayId NULL + the expected non-blocking log warning (row not failed). Verified by direct DB query; test data cleaned up afterward.
- **(No code) Post-load SQL** — after import, `UPDATE Certificates SET PathwayId = (SELECT Id FROM Pathways WHERE Name = @v) WHERE Number IN (...)` for the 27 certs. Fast but non-durable (every future import re-drops pathway).

**Related gap (same verification) — FIXED 2026-06-22:** the importer previously **silently ignored the Internal Tag column** (external tags only). Now extended: `InternalTagsColumns` mapping → parsed → matched/created with `TagType="internal"` (parallel pipeline to external), attached to the same `Certificate.Tags` M2M. Key schema constraint discovered during testing: **`Tags.Name` is globally unique (`IX_Tags_Name`)** — a name maps to exactly one tag regardless of type. So internal matching is by name across all types (reuse + cross-type warning), brand-new names colliding across both columns are created once (external precedence) with an attach-time fallback, preventing a duplicate-key crash. **Runtime test on localhost PASSED:** existing internal tag matched, new internal tag auto-created as `internal`, and an internal value colliding with an existing external tag reused it (no crash) — all verified by DB query; test data cleaned. (Conversely, the importer **does** map `pdfDownloadUrl` → PDF path at :90 — useful for a future PDF load.)
| P5 | **Start from a known dev DB state** (clean or snapshot). | Importer can update-by-cert# (:1325); a known baseline makes reconciliation counts exact. |

---

## 3. Load order (dependency graph)

0. **Test-data purge (§12)** — destructive, runs first, and has an explicit *do-not-remove* list.
1. Prereqs P1–P3 (lookups + schema).
2. **Active certs** → cert CSV import.
3. **Expired certs** → cert CSV import.
4. **Product IDs** → per-cert bulk import (requires certs from steps 2–3 to resolve cert# → CertificateId).
5. Post-load: Pathway backfill (P4 if needed), category backfill (analyst), reconciliation (§6).
6. **PDF link step (§10)** — attach certificate PDFs. Runs **last**: it matches on `Certificate.Number`, so every cert must already exist with its final number. Independent of steps 4–5 and safely re-runnable.

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
| **LOCATION** (combined) | Location + Country | **split required — see the multi-site rule below.** `Location` keeps the **full original string** (cap 175); `Country` takes the country of the **FIRST-listed site**. Where only "City, ST" present, Country stays blank (acceptable). |
| STATUS = "Expired" | Status | → `Expired` |

#### Multi-site LOCATION rule (revised 2026-08-13 — the previous rule was wrong)

**The original rule here said "strip trailing country into Country." That silently corrupts multi-site
rows and must not be used.** `LOCATION` is a free-text field in the source and routinely lists several
manufacturing sites, semicolon- or "and"-separated. "Strip trailing" takes the **last** country:

```
25-8767  Athens, GA; Chowchilla, CA; Kansas City, KS; Ottawa, Ontario, Canada; Redcliff, Alberta, Canada
         └─ old rule → Country = "Canada", and the three US sites vanish from Country entirely
```

A US-manufactured product filed as Canadian, with no error raised. Confirmed against the public
GreenCircle mirror: **19 of 1,021 records span more than one country** (~2%), across 35 distinct
countries; one spans US + China + Poland.

**Revised rule — three parts, all required:**

1. **`Location` keeps the complete original string, unmodified** (truncate only at the 175 cap, and
   log any row that truncates). Location is free text by design and is *meant* to list every site —
   nothing is dropped or summarised.
2. **`Country` = the country of the FIRST-listed site.** Deterministic, and it never discards the
   primary plant the way "last wins" does. Where the first site carries no country ("City, ST" only),
   infer US **only** if the token matches a US state abbreviation; otherwise leave `Country` blank.
3. **Every row whose sites span more than one country is written to a review CSV**
   (`migration-attachments/`, cert # + full LOCATION + assigned Country) and handed to GCC for analyst
   correction after the load. **Do not guess during the load** — an assigned country that is merely
   plausible is worse than one flagged for review.

**Client decision pending:** the first-listed-site rule is proposed to GCC in
`docs/tracking/migration-client-note-2026-08-13-location-country.md` as a no-action-needed default.
If **[[CR-037]]** (multi-select countries) is approved *before* the load, this rule is superseded —
the transform then populates the `CertificateCountries` join table with every country found and no
review CSV is needed. That is an argument for settling CR-037 before loading, not after.

**Also applies to Transform A.** §A treats Location/Country as "already split", which is true of that
source's *columns* but does not guarantee the Country column holds a single country — validate it the
same way and route multi-country values into the same review CSV.

**Expired-specific risks:** no Partner/CertResults/Pathway/Tags columns (all blank — fine); `6H3-####` cert numbers import as-is (string field); the single `expiration<effective` row needs a date sanity check before load. **Country values are imported unvalidated (DEF-347)** — the importer writes whatever string it is given, so a bad split lands in the database silently; fix DEF-347 before this load runs.

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

> ⚠️ **COUNTS ARE NOT SUFFICIENT — ADDED 2026-08-13 AFTER THIS SECTION FAILED TO CATCH A REAL FAULT.**
> DEF-351 truncated **50.7% of product names** at the first comma while **every count matched
> exactly** (16,234 in, 16,234 out), because the corruption was *inside* fields rather than row
> loss. A count check is structurally incapable of seeing that.
>
> **A value-level source-vs-DB reconciliation is now mandatory before acceptance:**
> `Scripts/data-ops/2026-08-13-migration-reconciliation.ps1` (UTF-8 dump of the loaded rows)
> then `…-reconciliation.py` (per-field compare against the transform output).
> On its first run it found **three faults counts had hidden** — DEF-351, DEF-353 (invented
> dates) and DEF-354 (`Certificate.ProductTypeId` never set).
>
> **Dump with `System.Data.SqlClient`, never `sqlcmd -o`** — the console codepage mangles
> en dashes and `®`, which produced 3,086 *false* mismatches on the first attempt. Do not diff
> text across a transport that can alter it.

- **Counts:** Active 959 + Expired (4,363 − resolved dups) certs; product IDs = 11,984 − (51 orphan rows held) = **11,933** across ~98 certs. Assert post-load counts match the transform output — as a *floor*, not the acceptance test.
- **Spot checks:** 24-1634 result = the decided value; Block/Square certs under one company; a coerced-date expired cert; an Amazon SEP cert has its pathway; retired-type expired cert loaded + type deactivated.
- **Amazon contract diff:** run `Scripts/smoke-prod/amazon-contract-diff.js` — must print **CONTRACT UNCHANGED** ([[feedback_amazon_api_immutable]], Gate 4).
- **Re-run the validation gate** on the transform's CSV outputs → expect 0 blockers.
- **Status history:** importer does **not** create an initial history row (only on change). **Decision:** seed one baseline "migrated" entry per cert, or accept no history for migrated records. *Recommend* a single baseline entry for audit continuity.

## 7. Execution mechanism & rollback

- **Mechanism:** a transform script (emits import-ready CSV + per-cert product-id batches) → load via the existing import UI/endpoints on **localhost**. No bespoke DB writes.
- **PDFs are handled separately — see §9.** The source listings carry no PDF refs (`pdfDownloadUrl` blank), so cert documents do not travel with this data load. As of 2026-08-12 a **public PDF corpus now exists** and covers 705 of the 959 active certs; §9 is the plan for using it.
- **Rollback (dev):** restore the pre-load DB snapshot (P5). **Prod:** full backup incl. `wwwroot/uploads/` before any run ([[feedback_backup_uploads_before_deploy]]); DB-change gating per Gate 7.

## 8. Open dependencies (must close before load)

- **Client decisions 1–4** (`migration-client-note-2026-06-22.md` §4) + the **three from 06-11** (CEF code, retired types, orphan ASINs).
- **P4 Pathway-ingestion FIX** (engineering, not client) — gap **confirmed**; pick importer-extension (recommended) vs post-load SQL. Plus the **Internal Tag** ingestion gap (same verification).
- **Tie-break rule for residual duplicate expired cert numbers** (decision 4) — needed before those expired certs can load uniquely.
- **Status-history baseline** decision (§6).
- **Credentialed PDF pull** (§9.4) — client to supply credentials w/c 2026-08-12; script needs an auth mode built before it can run.
- *Not blockers for the data load, but needed before the PDF link step runs for real:* the three
  decisions in **§10.7** — chiefly whether a third-party public PDF is acceptable as the stored
  document of record.

---

## 9. Certificate PDF corpus (added 2026-08-12)

The PDF side of the migration was previously "out of scope, loads separately" with no source
identified. There is now a real corpus to work with, and enough of it to start testing the PDF
load path **today** without waiting on the client.

### 9.1 What exists now

**Public GreenCircle database mirror** — complete as of 2026-08-12.

| | |
|---|---|
| Location | `C:\Users\swest\GCCPublicMirror\` (outside the repo; auto-`.gitignore`d — never commit it) |
| Contents | **1021 certificate PDFs, 753 MB**, one per public record |
| Index | `manifest.csv` (21 cols, utf-8-sig/Excel-safe) + `manifest.json` (adds the nested raw API record) |
| Audit trail | `api-certificates-raw.json` — untouched API response |
| Producer | `Scripts/data-ops/2026-08-12-greencircle-public-db-scrape.py` (read-only, public endpoints, no sign-in) |
| Integrity | 1021/1021 valid `%PDF-` headers, 0 zero-byte, 0 failures, 0 retries |

Manifest fields usable as transform input: `certificate_number, company, product, product_type,
certification, effective_date, expiration_date, location, contact, record_id, download_url,
local_path, file_size_bytes, sha256`.

### 9.2 Coverage against our source listings — **measured, not estimated**

> **SUPERSEDED 2026-08-12 (same day) — see §11.** The client sent new source files hours after
> this was written. Against the **new** Active List 7.31.26 the mirror covers **954 of 1014
> (94.1%)**, not 73.5% — the old 4.9.26 list simply predated most of what the mirror holds.
> The table below is kept because it is what the 4.9.26 files measured. **Use §11.4 for planning.**

Cert-number join, public mirror × `_remediation_output/20260622-170846/cleaned_cert_listing.xlsx`:

| Source set | Rows | Have a public PDF | Coverage |
|---|---|---|---|
| **Active List 4.9.26** | 959 | **705** | **73.5%** |
| **Expired List 4.9.26** | 4,219 | **0** | **0%** |
| Public-only (not in either source list) | — | 304 | certs issued after the 4.9.26 snapshot, or other GCC clients |

**The two numbers that drive planning:** 705 real cert+PDF pairs are available now, and **expired
certificates have zero public PDF coverage** — GreenCircle does not publish them. That is a
property of the source, not a gap we can close by scraping harder.

### 9.3 What we can test today (no credentials needed)

Use the 705 overlapping records as the PDF-load test corpus:

- **PDF attach path end-to-end** — the importer maps `pdfDownloadUrl` → PDF path (`:90`), never
  exercised with real files. 705 records is enough to prove the mapping, the
  `IUploadPathResolver` single-resolver contract ([[project_upload_storage_resolver]]), and
  storage outside the deploy folder (the June missing-PDF incident's root cause).
- **Volume realism** — 753 MB / 1021 files averages 0.74 MB. Sizing for uploads storage, backup
  windows, and publish-time risk should use this, not a guess.
- **Metadata cross-check** — an *independent* reading of company, product, certification type,
  and both dates for 705 certs. Disagreements between the mirror and the client's spreadsheet are
  findings worth raising **before** load, and this is the only second source we have.
- **Controlled-vocab reality check** — mirror `certification` and `product_type` values against
  the P1 CertificationTypes seed list and the Field Options sheet.
- **Filename/identity hazards** — 1021 files but **1018 unique sha256**: three pairs are
  byte-identical PDFs (Regupol 26-0201/0202, SARGENT 26-0129/0131, Corbin Russwin 26-0130/0132),
  one certificate covering several products. Also **1009 unique cert numbers across 1021 records**
  — 12 collide. **Cert number is not a unique key for a PDF, and file identity is not record
  identity.** Any PDF loader keyed on cert number alone will mis-attach here.

### 9.4 Credentialed pull — client supplying credentials w/c 2026-08-12

Closes the 4,473 source certs (mostly expired) with no public PDF.

**Prep item, not yet built:** the scrape script has **no authentication mode**. It uses an
unauthenticated session against public endpoints only. A credentialed pull needs a login/session
step and possibly a different endpoint set — a code change to scope once we see what the
credentials grant. Everything else (resumability, manifest, retry/backoff, integrity checks)
carries over unchanged, and a re-run skips the 1021 already held.

**Confirm when credentials arrive:** what they authorise (all certs, or this client's only);
whether the authenticated endpoint is the same `/api/certificates` with a wider result set;
and whether re-distribution of non-public PDFs into our environment is agreed in writing — these
are other companies' certificates.

### 9.5 Boundaries

- Read-only against a third-party public site; no business records touched, so **no CR required**
  ([[feedback_data_changes_require_cr]]).
- Non-public certificates return 404 individually and are **not** enumerable without credentials.
  Nothing was bypassed and no sign-in was attempted.
- Completeness of the public set is **verified**: seven pagination styles each return exactly
  1021 with identical first/last id, and the response carries no `total` field implying more.
- Mirror lives outside the repo and must stay there — 753 MB, and it is third-party content.

---

## 10. PDF link step — post-migration (planned 2026-08-12, not built)

Runs **after** the data load (§3 step 6). Takes the PDFs in
`C:\Users\swest\GCCPublicMirror\certificates\` and attaches each to its migrated certificate,
following the naming convention the application already uses.

**Status: PLAN ONLY — no code written.** Not blocked by §8; can be built and rehearsed against a
loaded dev DB whenever we choose.

### 10.1 What "linking" means in this system — the naming convention is not ours to invent

Verified in code, and both writers already agree:

| | |
|---|---|
| Physical dir | `IUploadPathResolver.CertificatesDir` = `{FileStorage:StoragePath or App_Data/uploads}\certificates` |
| **Filename** | **`{Certificate.Number}_{yyyyMMdd_HHmmss}.pdf`** — `FileUploadService.UploadCertificateAsync:93`, `CertificateImportService.UploadPdfFromPathAsync:1920` |
| DB field | `Certificate.CertificateFile` = logical `uploads/certificates/{fileName}` (`:111`, `:1925`) |
| Thumbnail | `Certificate.CertificateImagePath` = `uploads/images/{Number}_thumb_{yyyyMMdd_HHmmss}.png` |

Two consequences:

- **Mirror filenames must be discarded, not copied.** The mirror uses
  `{CertNo}_{Company}_{sourceId}.pdf`, which was correct for the scrape (it needed the source id
  to keep 12 colliding records apart) but is **not** the system convention. The link step renames
  on copy.
- **The `{CertNo}_` prefix is load-bearing in operations, not just cosmetic.**
  `Scripts/data-ops/2026-06-24-recover-missing-cert-pdfs.ps1` finds files by globbing
  `26-0174_*.pdf`. A PDF written under any other name is unrecoverable by our own recovery tooling.

### 10.2 The decisive constraint: this must be C#, not Python or SQL

`CertificateImagePath` has to be populated in the same pass. The thumbnail is rendered by
`FileUploadService.GeneratePdfThumbnailAsync` using **Docnet + ImageSharp**, with a two-pass
aspect-ratio detection that is not trivially reproducible. A cert with a PDF but no thumbnail
renders a **blank preview/zoom** — indistinguishable to a user from the pre-save blank that is
[[feedback_cert_preview_after_save_by_design]], and it would land as a defect during UAT.

A Python/SQL linker would therefore have to either skip thumbnails (700 blank previews) or
reimplement the renderer. Both are worse than running in-process.

**Recommended shape:** an argv branch in `Program.cs` — `dotnet run -- link-pdfs --manifest <path>
[--dry-run] [--force] [--limit N]` — that builds the host, resolves `IUploadPathResolver` and
`IFileUploadService` from DI, and reuses the existing copy + thumbnail logic. This is the same
"no bespoke DB writes" principle as §7: the link step calls the app's own services rather than
reimplementing them.

*Alternatives considered:* an admin-only batch endpoint (adds a permanent UI/authz surface for a
one-off), or a separate console project (duplicates DI wiring). Neither earns its cost.

### 10.3 Matching rule

- **Join:** `manifest.certificate_number` → `Certificate.Number`, exact first.
- **Fallback pass:** match on the existing persisted `NormalizedNumber` column (CR-187). Do **not**
  add another normalize helper — [[feedback_search_normalizer_single_source]].
- **Ambiguity is a stop, not a guess.** If more than one mirror row maps to one certificate, log
  it and attach nothing for that cert.
- **Skip if `CertificateFile` is already set**, unless `--force`. This is what makes re-runs safe.

**Collision risk is real but currently zero in the load set — measured, not assumed.** Twelve of
the 1021 mirror records share a certificate number with another record. They fall under just **8
numbers, all Regupol America** (26-0166/0168/0169/0170/0174/0175/0182/0184), each being several
*products* documented under one certificate number, with **genuinely different PDFs** — not
duplicates. `Certificate.Number` carries a **UNIQUE index** (`ApplicationDbContext.cs:68`) and
`Certificate` holds a single `ProductId` and a single `CertificateFile`, so the new system cannot
represent "one number, several product PDFs" at all. **All 8 appear in neither the Active nor the
Expired source list** — they are post-4.9.26 certs — so within the 705 records we will actually
load, the cert-number join is a clean **705 rows → 705 distinct numbers, 1:1**. This becomes a
live problem only when the credentialed pull (§9.4) or a refreshed source list brings them in;
`CertificateDocument` exists as a multi-document table but has **no UI surface anywhere**, so it
is not an answer today.

> **CORRECTION 2026-08-12 (same day):** the paragraph above concluded the 8 collisions were
> **outside** the load set. That was true of the 4.9.26 source files and is **false** of the
> 7.31.26 files the client sent hours later — all 8 Regupol numbers are on the new **Active List
> 7.31.26**, and the new expired sheets add more. The load set now contains **175 rows that
> cannot each get their own `Certificate` row**. This moved from a deferred hazard to the
> **top blocker**; see §11.2.

*(This corrects §9.3's framing: the three byte-identical PDF pairs are certs with **different**
numbers sharing one document — harmless, each gets its own copy. The collisions that matter are
these 8, and all of them differ in content.)*

### 10.4 Idempotency, resumability, rollback

- **Idempotent** by the `CertificateFile`-already-set skip; a re-run after a partial failure
  resumes with no duplicate copies.
- **Copy-then-verify:** compare the sha256 of the written file against `manifest.sha256` before
  writing the DB field. A copy that does not match its manifest hash is a failure, not a warning.
- **Rollback** is cheap and does not need a DB restore: `CertificateFile`/`CertificateImagePath`
  back to NULL for the affected ids + delete the written files. Emit an undo CSV (cert id, old
  value, new value, written path) each run so this is mechanical.
- **Audit:** these are updates to business records. Set the audit actor explicitly — the
  `Audit:SystemUserEmail` row must exist **on the target DB** (Gate 10,
  [[project_predeploy_agent_checklist]]).

### 10.5 Acceptance checks

Dry run first; it should print the same numbers the real run does.

| Check | Expected |
|---|---|
| Matched certs | **705** against the Active list |
| Unmatched mirror rows | 316 (304 public-only + 12 collision rows), each with a reason |
| Certs with no PDF after the run | 254 active + all expired — **expected**, see §9.2 |
| Files written | 705, every one resolving through `IUploadPathResolver.TryResolveExisting` |
| sha256 of each copy | equal to `manifest.sha256` |
| Thumbnails generated | 705; any shortfall listed by cert number |
| Ambiguous / already-linked / failed | 0 / 0 / 0 |

Then spot-check in the UI on localhost: open three linked certs and confirm the PDF downloads and
the preview thumbnail renders — verify through the click path, not just the DB
([[feedback_verify_through_client_click_path]]).

### 10.6 Sizing and operational notes

- **510 MB** across the 705 files (avg 0.72 MB, max 9.89 MB, none over the app's 20 MB cert cap),
  plus ~705 thumbnail PNGs. The full 1021-file mirror is 753 MB if we ever load all of it.
- Point `FileStorage:StoragePath` **outside the deploy folder** before running anything on a
  server — this is exactly the store a publish wiped on 2026-06-15
  ([[project_upload_storage_resolver]], [[feedback_backup_uploads_before_deploy]]).
- No schema change, so **Gate 7 does not apply**; the uploads backup step does.
- Estimated build: ~half a day for the argv branch + dry-run reporting, given both the copy and
  thumbnail paths already exist.

### 10.7 Open decisions (for the client / Shawn)

1. **Is a third-party public PDF acceptable as the stored certificate document,** or must the
   attached file be the client's own copy of record? This is the one that changes everything: if
   the answer is "client's copy only," the 705 are a **test corpus only** and the real load waits
   entirely on §9.4.
2. **The 254 active certs with no public PDF** — load them with no document and backfill later, or
   hold the whole PDF step until the credentialed pull is complete?
3. **Regupol-style multi-product certificates** (§10.3) — decide before the credentialed pull, not
   during it. Likely a CR if the answer is "keep all the PDFs."

---

## 11. NEW SOURCE FILES — go-live data received 2026-08-12

The client sent replacement source files and asked us to load them, describing them as **REAL data
for migration** ahead of go-live, with test certs/ASINs removed first. These **supersede** the
4.9.26 cleaned files that §4/§5 were built on. Profiled read-only; nothing loaded.

| | |
|---|---|
| Certs | `DataMigrationTestDev\GCC Database Migration_Certs_8.12.26V2.xlsx` (10 sheets) |
| ASINs | `DataMigrationTestDev\GCC ASIN Master List_8.12.26.xlsm` (1 sheet) |
| Client note | `DataMigrationTestDev\ReadMe.txt` (the covering email) |

### 11.1 The load set

Only three of the ten cert sheets are for loading. `Full List`, `Hold List`, and the 4.9.26
variants are context, **not** load input — worth confirming, since `Full List 7.31.26` (5,909 rows)
looks loadable and is not.

| Sheet | Rows | Distinct cert # | Dup #s | Surplus rows | Status value |
|---|---|---|---|---|---|
| Active List 7.31.26 | 1,026 | 1,014 | 8 | 12 | Client Approved - Paid (986) / Confidential (40) |
| Expired List 2 - 8.11.26 | 356 | 344 | 8 | 12 | Expired |
| Expired List 1 - 4.9.26 | 4,349 | 4,198 | 121 | 151 | Expired |
| **Total** | **5,731** | **5,556** | **137** | **175** | |

**Zero cert-number overlap between the three sheets** — they can be transformed and loaded
independently, in any order.

All three STATUS values resolve cleanly against `CertificateStatusHelper.TryResolveStatus`
(they are canonical strings already). No status mapping work needed — a genuine improvement on
the 4.9.26 files.

### 11.2 BLOCKER — 175 rows cannot each become a certificate

The client asks: *"there are in some cases multiple listings with the same cert number – can they
be uploaded this way since they already exist?"*

**As-is, no.** `Certificate.Number` carries a **UNIQUE index** (`ApplicationDbContext.cs:68`), and
a `Certificate` holds exactly one `ProductId` and one `CertificateFile`. The pattern in the data is
one certificate number covering **several products** (e.g. 26-0182 ×4, all Regupol America), which
the current model cannot represent. 175 rows across 137 numbers are affected — 1.5% of Active,
3.4% of Expired 2, 3.5% of Expired 1.

This is **the same issue as open client decision 7** (114 duplicate expired cert numbers, raised
2026-06-22, still `Open` in `migration-decisions-answer-tracker.md`), now larger and now also
affecting the **Active** list. It needs a decision, not a workaround:

- **(a) Collapse** to one certificate per number, keeping the most complete row and holding the
  rest aside — the June recommendation. Lossy: the other products' rows are not loaded.
- **(b) Model change** — allow multiple products per certificate. Drops the unique index and
  touches every cert-number lookup, **including the Amazon partner API**
  ([[feedback_amazon_api_immutable]]). A CR and a schema migration; not a go-live-week change.
- **(c) Suffix** the numbers to force uniqueness — **rejected**: it corrupts the client's real
  certificate numbers, which are the public identifier.

Recommendation: **(a) for go-live**, with (b) raised as a CR to consider after, so the answer
isn't rushed under a go-live deadline.

**Which numbers — full list at `migration-attachments/2026-08-12-duplicate-cert-numbers.csv`**
(312 rows: every duplicate row with sheet, Excel row, company, product, type, dates, and a flag).
They are **not one population — they are three, and they need different answers:**

| Group | Numbers | Rows | What it actually is | Safe to collapse? |
|---|---|---|---|---|
| **Regupol, Active** | 8 | 20 | 26-0166, 26-0168, 26-0169, 26-0170, 26-0174, 26-0175, 26-0182, 26-0184 — one certificate covering **genuinely different products** (26-0182 = sonus core / 6510S / Impacta ProBase / Thunder Flat). Same company, type and dates throughout. | **No — lossy.** Collapsing drops 12 real products. |
| **Regupol, Expired 2** | 8 | 20 | 25-0077, 25-0079, 25-0080, 25-0081, 25-0085, 25-0086, 25-0094, 25-0096 — the **prior-year edition of the same eight certificates**, same product sets. | Same as above, but historical. |
| **Expired 1** | 121 | 272 | Mostly **restatements of one product**, not several: 159 of the rows are Sika Canada/Sika Corporation, where the same product is listed under both entities and again as a French `(FR)` variant. 75 numbers differ by product name only; 19 repeat the product outright. | **Yes** — this is the June decision-7 population; keep-most-complete loses nothing real. |

**Two are neither, and cannot be collapsed under any rule** — unrelated companies sharing one
certificate number, with different products, types and dates. These look like errors in the source
and need the client to say which is correct:

- **21-0179** — Deceuninck North America (PVC Window Lineals 261, Recycled Content, 2021-05-01)
  vs DuPont™ Performance Building Solutions (Great Stuff Pro Gasket US, Material Ingredient
  Reporting, 2021-04-01)
- **21-0180** — Deceuninck North America (PVC Window Lineals 202, Recycled Content) vs 4ocean, PBC
  (iPhone 6…11, Recycled Content)

So the honest framing for the client is: the 121 on Expired 1 are cleanable, the 16 Regupol ones
are a real modelling gap, and 2 are data errors.

### 11.3 The client's other two questions — both answerable now

**Numeric prefixes** ("4.3 Cleaning Supplies" → "Cleaning Supplies"). Confirmed and quantified:
Product Category 7 of 8 distinct values prefixed, **Product Type 32 of 32**, **Product Subcategory
9 of 9**. Strip on transform — split once on the first space. The client's `N/A` option appears in
**Product Category only** (90 Active + 26 Expired-2 rows); Product Type and Subcategory use a
**blank** cell instead (100 and 942 blank rows on Active).

> **DECIDED 2026-08-13 (Shawn): blank stays blank — load NULL, do not substitute N/A or anything
> else.** The client deliberately cleaned these up before the move and will populate Category /
> Subcategory **in the system, after the load**. The cert views already render an em-dash for a null
> Product Type / Subcategory (`CertificateInternalView.cshtml:110-111`, and the External/Single
> equivalents), so an empty value displays correctly with no transform work.
> Consequence for §6 reconciliation: **do not treat blank Product Type/Subcategory as a load
> defect** — ~100 and ~942 Active rows are expected to land empty by design, and the §4A note about
> "85 blank → analyst backfill" is superseded by this (it's the client's backfill, not ours).

**Retired certification types** — *"Confirm we can still have these added but not in our dropdown
options for selection?"* **Yes.** `CertificationType.IsActive` already exists for exactly this.
All four are on expired sheets only: Biodegradable (56 rows), Biobased (16), Renewable Resource
Content (8), Verified Health Product Declaration (3) — 83 rows total.

> ⚠️ **CORRECTED 2026-08-13 — "all four named types are present" was WRONG.** That claim was
> measured against the local dev DB (34 `CertificationTypes`). Re-measured against the **test
> environment** (32 types, now cloned to dev — §12): **none of the four exist there.** All 32
> existing types are `IsActive = 1`; there are zero inactive types. So this is not a flag-flip on
> existing rows — the four types must be **created** with `IsActive = 0`. See
> [[feedback_diagnose_on_clients_environment]]: the same class of error as DEF-323.

> **VERIFIED 2026-08-13 — the whole path already works. No new build needed.** An earlier draft of
> this section flagged DEF-343 as a blocker to check. It is already fixed, and the fix was written
> for *this* case — `BuildCertificationTypeOptions` (`CertificateController.cs:1542`) grafts the
> stored type back into the dropdown when it is a child type **"or at a since-deactivated type"**
> (its own words), and it is applied on both the Edit GET (`:877`) and the validation re-render
> (`ReloadEditFormDataAsync :1577`). Four checks, all pass:
>
> 1. **Loads** — the importer reads `_context.CertificationTypes.ToListAsync()` **unfiltered**
>    (`CertificateImportService.cs:230`), so a deactivated type matches by name on import.
> 2. **Precedent already in production** — the importer itself creates an `Unspecified` placeholder
>    with `IsActive = false` (`:243`, client directive 2026-06-24) for migrated certs with no known
>    type. A deactivated type carrying real certificates is an established pattern here.
> 3. **Displays** — **no query anywhere filters certificates by `CertificationType.IsActive`**
>    (grep across `**/*.cs` returns nothing), so the 83 certs list, filter and render normally.
> 4. **Hidden from selection** — Create's dropdowns filter `ct.IsActive` (`:327–328`, `:781–782`),
>    so retired types can't be picked for new work.
>
> **One seeding constraint** (P1): seed the four as **top-level, `ParentId = null`, `IsActive =
> false`**. The graft protects the certification-type field; the *subcategory* cascade list
> (`AllCertificationTypes`, `:878`) is still `IsActive`-filtered with no grafting. The four are
> top-level CERTIFICATE_TYPE values so this is the natural seeding anyway — but don't seed them as
> children.

### 11.4 PDF coverage against the new lists — much better for Active

| Sheet | Certs | Have a public PDF | Coverage | Bytes |
|---|---|---|---|---|
| Active List 7.31.26 | 1,014 | **954** | **94.1%** | 709 MB |
| Expired List 2 - 8.11.26 | 344 | 3 | 0.9% | 22 MB |
| Expired List 1 - 4.9.26 | 4,198 | 0 | 0.0% | — |
| **Union** | **5,556** | **957** | **17.2%** | |

The client's instruction — *"Make sure for all the Active and Expired you are pulling associated
cert PDFs from current DB"* — is achievable for **Active** (94.1% from the mirror we already hold)
and **not achievable for Expired** from any public source: GreenCircle does not publish expired
certificates. **4,599 certificates have no public PDF**, essentially all expired. Those depend
entirely on the credentialed pull (§9.4), which is still unbuilt and awaiting credentials.

### 11.5 ASIN master list

21,034 rows → **158 distinct certificate numbers**, 14,301 distinct ASIN values.

- **Zero orphans** — every ASIN cert number exists in the load set. (The 4.9.26 file had 51 orphan
  rows; that problem is gone.)
- **4,797 surplus duplicate `(cert #, ASIN)` pairs** — 21,034 rows collapse to 16,237 distinct
  pairs, with one ASIN appearing up to 14× under a single certificate. The DB enforces
  per-certificate product-ID uniqueness (`FixProductIdUniqueIndex_PerCertificate`), so these must
  be de-duplicated on transform. Mechanical, but it should be **stated** rather than done quietly —
  the client will count rows.
- **Case inconsistency in `pathway`**: "Renewable electricity usage" (4,689) vs "Renewable
  Electricity Usage" (474). Normalize to one before load.
- **6 cert numbers map only to an EXPIRED certificate** (23-0814, 25-0484/5/6/7, 25-0492) despite
  the sheet being titled *active* ASINs. Query with the client.
- Backend constants are uniform as described: `issuer` = GreenCircle Certified, `status` = Active,
  `certificationCountries` = All, `pdfDownloadUrl` **entirely empty** (as intended — we supply PDFs).
- Largest certificate carries **2,440 ASINs**; median 20. Worth a look at import batching.

### 11.6 What still blocks the upload

1. **§11.2 duplicate cert numbers** — 175 rows. No default is safe without the client's word.
2. **All seven June client decisions remain `Open`** in `migration-decisions-answer-tracker.md`,
   including the hard blocker with no default (cert 24-1634's conflicting result). The new files
   do not answer them.
3. **"Remove all test certs/ASINs first"** — see **§12**, now a defined step with the client's
   do-not-remove list. Blocked on: a reviewed deletion manifest, and a written definition of
   "key test accounts."
4. **DEF-343 verification** for the certification-type dropdown (§11.3).
5. **Expired PDFs** — 4,599 certs, credentialed pull not built.

**Not blocked:** the transform work for §11.3's prefix-stripping, the ASIN dedupe, and the §10 PDF
link step for the 954 Active certs can all proceed now.

### 11.7 The client's own `Plan` tab — commitments and constraints we hadn't captured

Sheet 1 of the certs workbook is the client's migration plan. Three things in it bind us:

- **Test-data removal has an explicit exclusion list** (row 26, scheduled 8/10–8/14): *"Anuki removes
  all test data (certs, companies, products, fake users [except key test accounts], etc. — **DO NOT
  remove the Cert Types, Product Cat/Type/Subcat, Tags, Pathways, Partners options**)."* The
  reference/lookup tables must survive the purge. Combine with
  [[feedback_delete_audit_capture_joins]] when scoping that step.
- **A second upload is already scheduled** — row 29: *8/17 GreenCircle provides latest cert and ASIN
  list*; row 30: *8/18 Anuki uploads data*. This is the "another upload with differences" from their
  email; it is 5 days after the first, not a vague future. Launch is **9/14**, Amazon final check
  8/28, Amazon approval due 9/4.
- **The PDF commitment they're referring to is recorded and dated** — row 13: *"How are we pulling
  and uploading cert PDFs? >>> **Anuki confirmed 4/15** they can run a script to pull and map these
  from old DB to new DB."* That is the prior conversation cited in their 8/12 email, and §9/§10 are
  how we honour it. Note it says **old DB**, i.e. credentialed — not the public mirror.

Also worth knowing: row 1 of that tab makes *"check for any dups"* part of **their** pre-send
checklist. The 137 duplicated numbers arrived anyway, so the check either didn't run or didn't cover
the active list — relevant when asking them to resolve §11.2, and a reason to re-verify on the 8/17
file rather than assume it arrives clean.

---

## 12. Test-data purge — the destructive step that runs FIRST

Client instruction (workbook `Plan` row 26, scheduled **8/10–8/14**), quoted exactly:

> *"Anuki removes all test data (certs, companies, products, fake users [except key test accounts],
> etc. --- **DO NOT remove the Cert Types, Product Cat/Type/Subcat, Tags, Pathways, Partners
> options**]"*

### 12.1 In scope / out of scope

| Remove | Keep — explicitly excluded by the client |
|---|---|
| Test **certificates** (+ their product IDs, documents, status history, tag links) | **CertificationTypes** |
| Test **companies** | **ProductCategories / ProductTypes / Product Subcategories** |
| Test **products** | **Tags** (internal + external) |
| Fake **users**, *except key test accounts* | **Pathways** |
| | **Partners** |

The exclusion list is the reference/lookup vocabulary — the same rows P1 seeds. Deleting them would
break the load that follows. Note the tension with P1: the retired cert types must be **seeded**
(§11.3) while the purge must **not** delete cert types — these are consistent, but the purge script
must not be written as "delete everything not referenced by real data."

### 12.2 Rules this step has to satisfy

- **A reviewed manifest before anything is deleted.** Produce an IDENTIFY pass listing every row
  proposed for deletion (id, number/name, why it's classified as test) and get sign-off. This is
  live business data by the time it runs — [[feedback_data_changes_require_cr]] applies even though
  the environment is pre-launch ([[feedback_prelaunch_test_env]]).
- **"Key test accounts" is undefined and must be pinned down before running.** At minimum the three
  seeded role accounts stay. Get the list in writing.
- **Audit must capture join tables, not just the parent rows** — FK children, M2M skip navs, and
  anything removed via `ExecuteDeleteAsync`, audited **before** `SaveChanges`
  ([[feedback_delete_audit_capture_joins]], DEF-337).
- **Full backup first**, DB *and* the uploads store ([[feedback_backup_uploads_before_deploy]]).
- Follow the existing IDENTIFY / REMEDIATE script pairing in `Scripts/data-ops/`; prior art for
  scoped test cleanup: `2026-08-04-qc242-carly-test-cleanup.sql`,
  `2026-08-04-def338-import-toast-test-cleanup.sql`.
- **`sqlcmd` needs `-I`** on these DBs (CR-187 computed columns), and Products→Categories FK is
  `ON DELETE CASCADE` — [[reference_testenv_sql_delete_gotchas]].

### 12.3 Why this step also fixes certificate numbering

`GenerateNextCertificateNumberAsync` (`CertificateRepository.cs:854`) derives the next number from
rows already in the DB — max 4-digit sequence for the current year prefix, +1. So **no manual
seeding is needed** (the client's Plan row 14 asks us to "start counting" from where GCC left off;
this is automatic once real data is loaded). Measured against the go-live files:

- Highest 4-digit `26-` sequence across all three sheets = **481** → after the load the next
  generated number is **`26-0482`**.
- The real data contains **zero** 5-digit sequences. The 5-digit numbers currently in the DB
  (`26-10090`, `26-10120`) are **test/QC records** — removed by this purge. DEF-322 already ignores
  them when picking the next number, so numbering is correct either way, but the purge makes the DB
  consistent with what the client sees.
- 3 numbers are non-standard `6H3-####` (`6H3-5692`, `6H3-6301`, `6H3-7120`) — the known
  `EXP-CERT-FMT` warning, still dispositioned "legacy format, keep as-is." They don't affect
  numbering (the generator only matches a `NN-` prefix).

---

## 13. Second upload — 8/17 delta (client-scheduled)

Their `Plan` rows 29–32: **8/17 GreenCircle provides latest cert and ASIN list · 8/18 Anuki uploads
data.** This is the "another upload with differences" from their 8/12 email — five days out, not
indefinite. Downstream: Amazon final check **8/28**, Amazon approval **9/4**, launch **9/14**.

- **Mechanism:** the importer already updates by certificate number
  (`CertificateImportService` :1325), so re-importing the full 8/17 list is an upsert, not a
  duplicate load. **Prefer a full re-import over hand-computing a delta** — fewer ways to be wrong,
  and it self-heals anything the first load got wrong. Confirm the update path covers every column
  we care about before relying on it.
- **Re-run the §11 profile on the 8/17 file before loading it.** Do not assume the duplicate,
  prefix, and vocabulary findings were fixed — their own dup check missed 137 numbers this time
  (§11.7). This is cheap: the profiling scripts already exist.
- **The PDF link step is safe to re-run** — idempotent by design (§10.4), so it picks up only the
  newly-added certificates.
- ⚠ **Dual-entry window is the real risk.** Plan rows 31–32: *"Anuki needs to make sure the cert
  number picks up with next available based on where GCC left off but **this needs coordination with
  other DB!**"* and *"GreenCircle is adding to **both DBs** at this time."* From 8/18 to launch, new
  certificates are being created in the legacy DB **and** ours, and both allocate from the same
  `26-XXXX` sequence. `Certificate.Number` is UNIQUE, so a number assigned independently on each
  side is a collision that surfaces as a failed row on the next import — or worse, two different
  certificates believing they are `26-0482`. **Agree a rule before 8/18**: either GCC stops
  allocating in one system, or we reserve a block (their Plan row 31 offers "we can leave some
  buffer if needed"). Recommend reserving a buffer — it is the option that survives them forgetting.

---

## 12. DRY RUN ON DEV — go-live sequence confirmation (2026-08-13)

Dev was overwritten with a **row-for-row clone of the test environment** (43 tables / 8,823 rows,
verified against manifest, FKs re-validated, identity seeds checked). Every number below is
measured against that clone, i.e. against **what the purge and load will actually meet on test** —
not against the old local dev seed set.

Nothing was deleted and nothing was loaded. All checks are read-only.

### 12.1 The three-step sequence, as stated by the client

1. Anuki removes all test data (certs, companies, products, fake users except key test accounts)
   — **do not** remove Cert Types, Product Cat/Type/Subcat, Tags, Pathways, Partners
2. Anuki uploads the first batch of real cert data (expired/current), including cert PDFs pulled
   from the existing DB
3. GreenCircle creates team users

**Steps 1 and 3 are confirmed workable. Step 2 is not yet runnable** — see §12.5.

### 12.2 Step 1 — purge scope, measured

| Purge | Rows | | Preserve (client explicit) | Rows |
|---|---|---|---|---|
| Certificates | 228 | | CertificationTypes | 32 |
| CertificateProductIds | 3,202 | | ProductCategories | 9 |
| CertificateProductIdPartners | 2,644 | | ProductTypes | 54 |
| CertificatePartners | 144 | | **ProductSubcategories** | **0 — see §12.3** |
| CertificateStatusHistory | 53 | | Tags | 4 |
| CertificateTag (join) | 14 | | Pathways | 6 |
| CertCertTypeSubcats (join) | 16 | | Partners | 4 (Amazon, Novi, Other, Wayfair) |
| Products | 312 | | | |
| Companies | 173 | | | |
| CompanyUser (join) | 684 | | | |
| AuditHistory | 930 | | | |
| ImportBatches | 6 | | | |
| Users | 88 → keep key accounts | | | |

**Delete order is forced by the FK graph — it is not free choice.** `Certificates` cascades
cleanly to its seven children (ProductIds, Partners, StatusHistory, Tag, CertTypeSubcats,
Documents, CustomFieldValues). But **`AuditHistory`, `Certificates.CreatedBy/UpdatedBy`,
`CertificateStatusHistory`, `CertificateProductIds` and `GoogleAnalyticsSettings` all reference
`Users` with `NO_ACTION`.** Deleting users first fails. Required order:

    Certificates (cascades) → Products → Companies → AuditHistory → ImportBatches → Users

**`Audit:SystemUserEmail` = `swest@anukiconsulting.com` = `Users.Id 1`. That row MUST survive the
purge** or every audited write fails on the target — this is exactly the Gate 10 / DEF-337 failure
mode. Any "key test accounts" list that omits it is wrong.

**Unstated by the client, needs a call before we delete:** `PartnerApiKeys` (9), `ApiKeys` (3),
`GoogleAnalyticsSettings` (1), `CustomFields` (0), and the two CR-019 backup tables
(`Certificates_bak_CR019_20260608` 4, `Products_bak_CR019_20260608` 2). **PartnerApiKeys is the
risky one** — purging it revokes Amazon's live integration key
([[feedback_amazon_api_immutable]]). Recommend: preserve all of these, delete none.

### 12.3 Step 1 — the preserve list has a hole: ProductSubcategories is EMPTY

The client says *"DO NOT remove … Product Cat/Type/Subcat"*. **`ProductSubcategories` has 0 rows in
the test environment.** There is nothing to preserve. Meanwhile the source data supplies 12
distinct subcategory values across 104 rows (PP 40, HDPE 24, LLDPE 10, LDPE 9, PET 7, PVC 9,
TPE/TPV 2, TPO 2, PS 1). So subcategories must be **created**, not preserved — the instruction as
written cannot be executed, and if we follow it literally those 104 rows load with a blank
subcategory. `Certificates.ProductSubcategoryId` is nullable, so **this fails silently, not loudly.**

### 12.4 Step 1+2 interaction — lookup resolution gate (the actual dry run)

New script: `Scripts/data-ops/2026-08-13-migration-dryrun-lookup-resolution.py`. It asks whether
the 5,731 source rows resolve against the reference tables we are told to keep, after stripping the
numeric grouping prefixes (§11.3). Output at `_dryrun_output/20260813-113545/`.

| Sheet | Field | Result |
|---|---|---|
| Active 7.31.26 (1,026) | CERTIFICATE_TYPE | **100%** — 15 values / 1,026 rows |
| | Product Category | **100%** — 7 values / 936 rows + 90 `N/A` |
| | Product Type | **100%** — 32 values / 926 rows + 100 blank |
| | **Product Subcategory** | **0%** — 9 values / 84 rows unresolved (table empty) |
| | Pathway / Partner / Tags | **100%** |
| Expired 2 (356) | CERTIFICATE_TYPE | **100%** — 7 values |
| | **Product Subcategory** | **0%** — 3 values / 20 rows unresolved |
| Expired 1 (4,349) | **CERTIFICATE_TYPE** | **4 values / 83 rows unresolved** |

**Only two failures, and both were already known as questions — now they are quantified:**

1. **Product Subcategory, 104 rows** — §12.3, the empty table.
2. **The four retired cert types, 83 rows** — Biodegradable 56, Biobased 16, Renewable Resource
   Content 8, Verified Health Product Declaration 3. **§11.3's claim that all four already exist
   is corrected above: they do not exist on the test environment.**

Everything else — certification types on both current sheets, product categories, product types,
pathways, partner, external and internal tags — resolves **100%** against the preserved tables.
The prefix-stripping transform is confirmed correct by that 100%: without it, Product Type would
have resolved 0 of 32.

### 12.5 Step 2 is NOT runnable yet — and this is the honest blocker

The end-to-end load dry run (transform → importer Preview) **cannot be run today**, for reasons
that are not about the dev database:

- **No transform script exists for the 8.12 files.** §4/§5 describe Transforms A/B/C as a design
  against the superseded 4.9.26 files. What is built is the *validation* gate
  (`2026-06-22-migration-source-validation.py`) and the *mechanical remediation*
  (`…-remediation.py`, which emits cleaned XLSX — **not** a load-ready importer CSV). The step
  that turns a cleaned sheet into importer input is unwritten.
- **The §11.2 duplicate-cert-number decision is still open** — 175 rows, no safe default. A
  transform cannot be written before it is answered, because the answer determines what the
  transform emits.
- **`Certificates.CompanyId` and `ProductId` are `NOT NULL`.** Every one of the 5,556 certs needs a
  Company and a Product row created — after the purge removes all 173 companies and 312 products.
  That is find-or-create work inside the transform, not a lookup.
- **Expired PDFs** — 4,599 certs have no public PDF; the credentialed pull (§9.4) is unbuilt.

### 12.6 Step 3 — GreenCircle creates team users

No obstacle. `Roles` (5), `Permissions` (22) and `PermissionRole` (41) are untouched by the purge,
so the roles GCC assigns exist. Note that the purge removes the 5 existing
`@greencirclecertified.com` accounts (incl. `admin.uat`, `analyst.uat`) — correct per the
client's plan, since GCC creates the real ones, but it should be **stated to them** rather than
discovered. Confirm the go-live account-creation path works before the last Anuki admin is removed.

---

> ⚠ **Doc housekeeping (2026-08-13):** there are now **two sections numbered 12** — "Test-data purge"
> and "DRY RUN ON DEV", written by concurrent streams. Cross-references below point at the *purge*
> section. Renumber in one pass rather than piecemeal, to avoid breaking the other stream's refs.

---

## 15. GAP — 51 live certificates are missing from the load set (found 2026-08-13)

Cross-check of `Full List 7.31.26` (5,908 rows) against the three load sheets (5,556 numbers):
**174 certificates appear in the Full List but in none of the sheets we were told to load.**

| Status in the client's DB | Missing from load set | Live on the public site |
|---|---|---|
| **Client Approved - Paid** | **51** | **50** |
| Expired | 26 | — |
| DRAFT - Internal | 96 | — (expected; see §14) |
| Draft - Client | 1 | — |

The 51 are concentrated in ASSA ABLOY group brands — Corbin Russwin ×14, SARGENT ×11, Malarkey
Roofing ×11, Pioneer Industries ×10, plus McKinney, ACCENTRA and Greenfield Natural Meat. **50 of
the 51 were verified live against the public mirror** (e.g. 26-0059 Corbin Russwin "800PT EcoFlex®
Premium Exit Trim", expiring 2027-08-31), so these are not stale rows — they are current, published
certificates.

**Consequence if we load exactly what was sent:** 51 certificates that are publicly visible today
would not exist in the new system at go-live, and 50 of them would disappear from the public site
at cutover. This is a data-completeness failure the client would notice immediately.

**Attachment for the client:** `migration-attachments/2026-08-13-certs-missing-from-load-set.csv`
(77 rows — the 51 + the 26 expired; drafts excluded as they're handled by §14), with
`live_on_public_site` and `row_in_Full_List_7.31.26` columns.

**How it probably happened:** the three load sheets look like status/date-filtered pulls; a filter
or cut-off appears to have dropped these. Not a judgement — worth asking rather than assuming.
Note 11 of them also appear on `Hold List 4.9.25` marked **"Keep"** with a 7.31 DB status of
`CLIENT APPROVED - PAID`, i.e. the client's own notes say to retain them.

**Also visible in that CSV:** two rows with company `Test` and one with cert type `BLANK` — check
these are excluded rather than loaded, and that `Test` isn't caught by the §12 purge *after* being
loaded.

**Action:** asked in the 2026-08-12 client note. Recommend they arrive via the 8/17 pull rather than
as a patch, so there is one authoritative source.

---

## 14. Seed the certificate numbering from the last load, before production cutover

**Shawn, 2026-08-13.** After the final load and **before cutover**, the new system's numbering must
be seeded from what was actually loaded, so the first certificate GCC creates in production
continues from their real last-issued number instead of re-using one.

### 14.1 This is a demonstrated gap, not a precaution

Auto-derivation alone is **not** sufficient, and the go-live files prove it:

> **UPDATED 2026-08-13 — the gap is 110, not 19.** This section first used `Full List 7.31.26`,
> which tops out at 26-0500. A later pull from the client's **live** system shows they have issued
> to **26-0591**. The spreadsheet understated the gap by 91. Use the live figure; re-measure again
> before cutover, because it keeps moving.

| | |
|---|---|
| Highest `26-` number in the **load set** | **26-0481** (Aqua Mizer, Inc., Active List) |
| Highest `26-` number GCC has **actually issued** (live system) | **26-0591** |
| Gap | **110 numbers — 26-0482 … 26-0591** |

Of those 110: **107 are `DRAFT - Internal`** and **3 are `Client Approved - Paid`**. The drafts sit
in a status the client told us to skip (`Plan` row 6: *"Draft - Client / DRAFT - INTERNAL — Ignore
for now"*). **The 3 live ones are worse** — they are missing from the load set entirely (the §15
problem) *and* their numbers would be handed to different certificates. Two independent failures on
the same records.

So `GenerateNextCertificateNumberAsync` would derive **26-0482** from the loaded rows and hand it to
a new certificate — a number one of their drafts already holds — then keep going through the range
for 110 certificates. Because `Certificate.Number` is UNIQUE the collision does **not** surface at
issue time; it surfaces later as a **failed import row** when those certificates are finally loaded,
by which point the number belongs to someone else's. Silent at the moment of damage.

### 14.2 How to close it — preferred option first

1. **Load the drafts (recommended).** Ask for the 8/17 pull to include Draft records and load them
   with status `Draft`. Derivation is then correct *by construction* — no new mechanism, no floor to
   maintain, and each number stays attached to the certificate that owns it. `Draft` is one of the 8
   states and is **not** in `PublicStatuses`, so nothing becomes publicly visible. The client already
   anticipates this: *"Will need to repull closer to import to find those still in this status or
   that are now in an active status."*
2. **Explicit floor**, only if drafts can't be supplied. `GenerateNextCertificateNumberAsync`
   (`CertificateRepository.cs:854`) has **no floor parameter** — it derives purely from existing
   rows, so this is a **code change**, not configuration. Scope it only if option 1 is refused.
3. **Reserved block** — the buffer already proposed to the client (§13). Complements either option
   for the dual-entry window; it does not by itself fix the draft gap.

### 14.3 Cutover check — make it falsifiable

Don't assert "numbering is correct" — assert the boundary
([[feedback_confirm_means_falsifiable]]):

- Take GCC's **last issued number** at cutover, in writing, from the legacy system.
- Call `GenerateNextCertificateNumberAsync()` against the production DB; record what it returns.
- **Pass condition: the returned sequence is strictly greater than that last-issued number.** Log
  both values in the cutover record.
- Run it **after** the final delta load, not before — the load moves the answer.
- The method takes an `asOf` clock seam (added for the DEF-322 year-rollover tests), so
  year-boundary behaviour can be asserted without waiting for January.

**Gate:** this belongs on the pre-deploy checklist as a go-live blocker
([[project_predeploy_agent_checklist]]) — a wrong number here is discovered by the client, in
production, on their first new certificate.

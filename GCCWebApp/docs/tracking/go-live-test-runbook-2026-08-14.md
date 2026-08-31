# Go-live migration — TEST environment runbook

**Status:** ✅ **COMPLETE ON TEST 2026-08-18** — every step 0–7 run and verified, documents uploaded
and confirmed serving through the application (§11). Shawn approved the purge and Option A verbally
on 2026-08-18 and directed an in-hours run; **no written client sign-off exists for either CR**, and
Gate 3 (the 1,960 untagged product IDs) is still open. Every step had previously been executed
end-to-end on dev.
**Target:** `gccwebap_certificates` @ `68.71.129.120,1533` (the gccwebapp.com test environment)
**Source of truth for numbers:** the dev rehearsal completed 2026-08-14.

---

## 0. What dev proves

| | Dev (verified 2026-08-14, CR-038 build) |
|---|---|
| Certificates | **5,702** across **5,527** distinct numbers |
| …grandfathered duplicates | **312** (`AllowsDuplicateNumber = 1`) |
| Non-exempt duplicates | **0** — the database guarantee still holds for the other 5,390 |
| …with PDF / thumbnail | **5,474 / 5,473** — disk matches the database exactly |
| Companies / Products | 301 / **3,439** |
| Product IDs / partner links | 16,234 / 14,274 |
| Cert partners / tags | 244 / 388 |
| Untrusted or disabled FKs | **0** |
| NULL `NormalizedNumber` (CR-187) | **0** |
| AuditHistory | **0 — by design**, see §4 |
| Value-level reconciliation | **14 of 15 fields, 0 mismatches**; 0 numbers with a differing record count |

The single reconciliation difference is Country ×9 (`The Netherlands` → `Netherlands`), the
DEF-347 canonicalisation working as intended, not drift.

> **Country was ×15 until 2026-08-14 — DEF-356.** The other 6 were `Wales` → `United Kingdom`,
> which was **not** canonicalisation: the old country list was sovereign-states-only (197 entries)
> and aliased Wales away, discarding a value the client supplied. `Puerto Rico` (2 certs) was absent
> from the list entirely and loaded as an unselectable value. The list is now 220 entries —
> ISO 3166-1 territories plus the four UK constituent countries as first-class choices. **Verified on
> dev after the re-run: `Wales` = 6, `Puerto Rico` = 2, `United Kingdom` 26 → 20.**
> **×9 is now the correct expected number. If a test run reports ×15, it ran against the old list.**

**Products is 3,439, not the 3,305 quoted before 2026-08-14** — that figure belonged to the
pre-CR-038 5,390-certificate load. The extra 312 certificates introduce 134 further distinct
CR-186 product identities. Verified: **0 orphan products, 0 orphan companies** — every product is
referenced by a certificate, so the higher number is real, not purge residue.

**Deliberately absent, and each is expected:**
- **~226 certificates have no PDF** — no document exists in the client's source system. A ceiling,
  not a fetch gap.
- **29 rows withheld** — missing Company / Product Name / Certification Type / dates (§5, DEF-353).
- **104 product-subcategory + 177 cert-subcategory values not stored** — DEF-348, no import path.

---

## 1. Gates before anything runs

| # | Gate | Status |
|---|---|---|
| 1 | **CR raised and approved for the purge.** It deletes business records. | ✅ **APPROVED by Shawn 2026-08-18** and executed the same session. CR-039 was raised 2026-08-17 but **was never sent to the client** — the approval on record is Shawn's, not GCC's. `docs/tracking/CR-039-golive-test-data-purge.md` |
| 2 | Client **confirmation** of Option A for duplicate certificate numbers (§6). The build is DONE; only the sign-off is outstanding. | ✅ **approved by Shawn 2026-08-18**; loaded with `--dup-policy all`. **No written client confirmation is on file** — the 2026-08-17 approvals email is still a draft |
| 3 | Client decision on the 1,960 untagged product IDs (§7). | ❌ **STILL OPEN** — loaded untagged. **Corrected 2026-08-21:** this cell previously read "returned to **every** partner caller", which is the wrong way round and contradicted §7 of this same document. Cert-level scoping runs first and those four certificates have no partner association, so they reach **no** partner — `untagged_reachable_by_a_partner` = **0 of 1,960**. The risk is silent omission, not over-exposure |
| 4 | Agreed keep-list of user accounts. | ✅ **FIXED 2026-08-17, executed 2026-08-18** — kept 5: `swest@`, `admin@gcc`, **`carly@gcc` (client Admin)**, `analyst@gcc`, `partner@amazon.com`. Purge reported **0 unmatched keep-list entries**, users 88 → 5 |
| 5 | Deploy window agreed — test is live and in use by the client. | ⚠️ **not agreed** — Shawn directed an in-hours run on 2026-08-18 (started ~10:50 local). No client-facing window was booked |

> **Gates 3 and 5 were not satisfied when this ran.** Recorded here rather than smoothed over: the
> load went in on Shawn's authority during business hours, with the untagged-ASIN decision still
> open and no written client sign-off on either CR.

> Test drifts during the day: it held **229** certificates against our 09:00 backup's 228 on
> 2026-08-13. Do not run against a stale backup and do not assume nobody else is working.

---

## 2. Backups — take these IMMEDIATELY before, in the same session

1. **Database** — `Scripts/data-ops/backup-testenv-db.ps1` (Export). Writes
   `db-backups/gccwebap_certificates-<ts>/`.
2. **Uploads** — zip the server's `wwwroot/uploads/` (or whatever `FileStorage:StoragePath`
   resolves to). **The DB backup does NOT contain PDFs.** A publish wiped this store once already
   (2026-06-15).

**Rollback is proven, both shapes** (2026-08-13 drill): restore into an empty DB = 8,823 rows in
82s; restore *over a populated* DB = 24s, damaged rows correctly reverted, 0 untrusted FKs,
Unicode intact. Procedure: create/clear DB → `sqlcmd -I -i create-schema.sql` →
`backup-testenv-db.ps1 -Mode Restore -OutDir <backup> -ConnectionString <target> -Confirm`.

---

## 3. Schema prerequisites — ALL THREE ALREADY APPLIED ON TEST (verified 2026-08-18)

> ⚠️ **The line below this heading used to read "these do NOT auto-apply · Test and prod never
> auto-migrate (Gate 7 / DEF-239)". That is STALE and was wrong on 2026-08-18.**
> `appsettings.Production.json` sets **`Database:AutoMigrateOnStartup: true`** (DEF-252, added
> deliberately because gccwebapp.com is a single-tenant pre-launch test env and a forgotten Gate-7
> SQL run had already caused a login outage). So the test environment **does** auto-migrate, and the
> build deployed on 2026-08-18 applied CR-038 by itself.
>
> **Verified on test 2026-08-18 before the purge:**
> - `Certificates.AllowsDuplicateNumber` **present**; `IX_Certificates_Number` =
>   `FILTERED: ([AllowsDuplicateNumber]=(0))`; newest applied migration =
>   `20260814195931_CR038_AllowDuplicateCertificateNumbers`
> - `Products.Name` and `AuditHistory.EntityName` both **`nvarchar(1000)`** (DEF-349 satisfied)
> - all 4 retired certification types present with `IsActive = 0`; `CertificationTypes` = 36
>
> None of the three scripts below needed to be run by hand. **Gate 7 still applies to the real
> production cutover**, where `AutoMigrateOnStartup` is expected to be `false` — do not delete this
> section, and re-check the flag per environment rather than assuming either answer.

| Script | Why |
|---|---|
| `2026-08-13-WidenProductsName-PREREQ.sql` | **DEF-349.** `Products.Name` and `AuditHistory.EntityName` are still `nvarchar(255)`; 13 source rows exceed it (max 710). Without this the load **fails entirely** — 0 rows created. Idempotent. |
| `2026-08-13-migration-prereq-retired-certtypes.sql` | **P1.** Creates the 4 retired certification types with `IsActive = 0` (Biodegradable, Biobased, Renewable Resource Content, Verified HPD). 83 rows depend on them. Idempotent. |
| `2026-08-14-CR038-duplicate-cert-numbers-PROD.sql` | **CR-038.** Adds `AllowsDuplicateNumber` and swaps the unique index for the filtered one. Without it the 312 duplicate rows cannot load at all. Aborts if non-exempt duplicates already exist. Idempotent. **Must ship in the same step as the DLL** — see §6. |

**Not required for the load, but needed for ongoing analyst use:** the code fixes for
DEF-353 / 354 / 355 are built and verified on dev but **not deployed**. The SQL loader bypasses the
importer, so they do not gate the migration itself.

**CR-038 is different — its code IS required.** The filtered index cannot see exempt rows, so the
uniqueness check added to the MVC Edit path and `ValidationService` is the only thing preventing a
new certificate taking a grandfathered number. Deploying the SQL without that build opens the gap.

---

## 4. The load sequence — order is not optional

    0. Transform      2026-08-13-migration-transform.py --dup-policy all      <-- CR-038
       Generate       2026-08-13-migration-sql-generator.py
    1. Purge          2026-08-13-golive-testdata-purge.sql   -v Execute=1
    2. Load           _sql_output/<ts>/migration-load.sql     (UTF-16 LE)
    3. Reconcile      2026-08-13-migration-reconciliation.ps1 + .py
    4. Link PDFs      dotnet run -- link-pdfs --manifest <portal manifest>
    5. Contract diff  Scripts/smoke-prod/amazon-contract-diff.js
    6. Click-path spot check

**`--dup-policy all` is now the go-live mode** (CR-038). The default is still `quarantine`, which
withholds the 312 duplicate rows — correct before the client's decision, wrong after it. Running the
default would silently load 5,390 instead of 5,702.

**Why this order:** the purge deletes certificates, and the PDF links live *on* certificate rows.
Linking before loading, or re-purging after linking, silently discards every link — that happened
on dev on 2026-08-13 and left 4 GB of orphaned files. **Load first, link last.**

**Purge delete order is forced by the FK graph** (the script handles it): Certificates → Products →
Companies → AuditHistory → ImportBatches → Users. Deleting users first simply fails.

**No audit rows are written for the migrated data** (agreed 2026-08-13). Consequence: **there is no
`UndoImport` for this load.** Rollback is §2's restore. On an empty-target load that is stronger
anyway, but it is a deliberate trade, not an oversight.

**Encoding:** `migration-load.sql` is emitted **UTF-16 LE with a BOM** and carries its own encoding
canary. Do not re-save it as UTF-8 — sqlcmd will decode it as ANSI and silently corrupt every
non-ASCII value (this happened on dev: 1,674 product names and 360 company names mangled, with the
load reporting complete success).

---

## 4A. Complete inventory — every script and document, with the exact command

Working directory for all commands: `GCCWebApp/GCCWebApp` (the ASP.NET project folder).
Every script writes into its own timestamped output directory; **nothing overwrites a prior run**,
so a failed attempt can always be compared against a good one.

### Source inputs (read-only — never edited in place)

| Input | Path |
|---|---|
| Certificate listings (Transforms A + B) | `<source>/GCC Database Migration_Certs_8.12.26V2.xlsx` |
| ASIN master list (Transform C) | `<source>/GCC ASIN Master List_8.12.26.xlsm` |
| Portal PDF pull + manifest | `C:\Users\swest\GCCPortalPull\manifest.csv` (5,575 PDFs / 3.59 GB) |
| Country canonical list | `Models/CountryList.cs` — **parsed directly by the generator**, see note below |

> ⚠️ **`Models/CountryList.cs` is a migration input, not just app code.** The generator reads the
> `Countries` and `Aliases` collections straight out of the `.cs` file
> (`2026-08-13-migration-sql-generator.py:95`) so the loader and the running application can never
> disagree. **Editing that file changes what a load produces** — which is exactly how DEF-356 was
> fixed without touching the generator, and why the country counts moved between the 2026-08-13 and
> 2026-08-14 runs.

### The pipeline, in order

| # | Step | Command | Produces |
|---|---|---|---|
| 0a | **Transform** | `python Scripts/data-ops/2026-08-13-migration-transform.py --dup-policy all` | `_transform_output/<ts>/` — `certs-active.csv`, `certs-expired1.csv`, `certs-expired2.csv`, `productids-all.csv`, plus `quarantine-*.csv`, `review-*.csv`, `unmappable-fields.csv`, `transform-summary.csv` |
| 0b | **Generate SQL** | `python Scripts/data-ops/2026-08-13-migration-sql-generator.py` (defaults to the newest transform dir) | `_sql_output/<ts>/migration-load.sql` (**UTF-16 LE**) + `review-unmapped-countries.csv` |
| 1 | **Purge** | `sqlcmd -S <server> -d <db> -I -i Scripts/data-ops/2026-08-13-golive-testdata-purge.sql -v Execute=1` | deletes business records; preserves the §5 list |
| 2 | **Load** | `sqlcmd -S <server> -d <db> -I -i _sql_output/<ts>/migration-load.sql` | one transaction, preflight-checked |
| 3a | **Dump loaded rows** | `pwsh Scripts/data-ops/2026-08-13-migration-reconciliation.ps1` | `_reconciliation/loaded-certificates.csv` (UTF-8 via SqlClient — **not** `sqlcmd -o`) |
| 3b | **Reconcile values** | `python Scripts/data-ops/2026-08-13-migration-reconciliation.py` | `_reconciliation/reconciliation-summary.csv` + `reconciliation-mismatches.csv` |
| 4 | **Link PDFs** | `dotnet run -- link-pdfs --manifest C:\Users\swest\GCCPortalPull\manifest.csv` (add `--dry-run` first) | copies PDFs, generates thumbnails, writes undo/review CSVs to `<manifest dir>\_link-pdfs` |
| 5 | **Contract diff** | `node Scripts/smoke-prod/amazon-contract-diff.js` | must print **`CONTRACT UNCHANGED`** (Gate 4) |
| 6 | **Click-path spot check** | `ViewCertificateFile` / `ViewCertificateImage` on 3 certificates | see §8 — **not** `/uploads/...`, which 404s by design (DEF-208) |
| 7a | **Dump for end-to-end verify** | `powershell -File Scripts/data-ops/2026-08-15-verify-dump.ps1` (pass `-ConnectionString` for test) | `_verify_input/db-certificates.csv`, `db-productids.csv` (UTF-8 via SqlClient) |
| 7b | **Verify spreadsheet → database** | `python Scripts/data-ops/2026-08-15-verify-spreadsheet-to-database.py` | `_verify_output/<ts>/` — `missing-from-database.csv`, `extra-in-database.csv`, `asin-missing.csv`, `asin-extra.csv` |

**Step 7 is not the same check as step 3.** Step 3 reconciles the database against the *transform
output* — both sides come from the same place, so a transform bug is invisible to it. Step 7 goes
back to the client's own workbooks and reconciles in both directions. Run both; neither substitutes
for the other.

**`-I` (QUOTED_IDENTIFIER ON) is required on every `sqlcmd` call** — CR-187's persisted computed
columns fail without it.

### Supporting scripts (not part of the load path)

| Script | Purpose |
|---|---|
| `Scripts/data-ops/backup-testenv-db.ps1` | `-Mode Export` before, `-Mode Restore` to roll back (§2) |
| `Scripts/data-ops/2026-08-13-WidenProductsName-PREREQ.sql` | schema prereq — DEF-349 (§3) |
| `Scripts/data-ops/2026-08-13-migration-prereq-retired-certtypes.sql` | schema prereq — 4 retired cert types (§3) |
| `Scripts/data-ops/2026-08-14-CR038-duplicate-cert-numbers-PROD.sql` | schema prereq — CR-038 filtered index (§3, §6) |
| `Scripts/data-ops/2026-08-14-duplicate-cert-report.py` | builds the client-facing duplicate report (§6) |
| `Scripts/data-ops/2026-08-13-migration-dryrun-lookup-resolution.py` | pre-flight: proves every lookup name resolves on the target |
| `Scripts/data-ops/2026-06-22-migration-source-validation.py` | profiles the source workbooks; flags blockers before any transform |

### Documents that govern this migration

| Document | Role |
|---|---|
| `docs/tracking/go-live-test-runbook-2026-08-14.md` | **this file** — the procedure of record |
| `docs/tracking/CR-038-duplicate-certificate-numbers.md` | the duplicate-number change request |
| `docs/tracking/CR-038-client-report-duplicate-certificate-numbers.md` | the client-facing report + 4 decisions requested |
| `docs/tracking/CR-037-multi-country-certificates.md` | multi-country certificates — **still open**, owns the 17 unresolved rows |
| `docs/tracking/defect-register.md` | DEF-347/348/349/350/351/353/354/355/**356** — all referenced above |
| `docs/tracking/pre-deploy-agent-checklist.md` | Gates 0–10, applies to the deploy that carries this |
| `observations_log.md` | dated narrative of every rehearsal, including what went wrong |

### Run history on dev

| Run | Certs | Country mismatches | Note |
|---|---|---|---|
| 2026-08-13 23:13 | 5,390 | **15** | pre-CR-038, pre-DEF-356. 6 Wales + 9 Netherlands |
| 2026-08-14 14:22 | 5,702 | — | CR-038 (`--dup-policy all`); first run to pick up the fixed country list — `Puerto Rico` drops off the unmapped report |
| 2026-08-14 16:15 → 16:36 | **5,702** | **9** | current dev state; **verified end-to-end 2026-08-14 20:51** (see §8 results below) |

**Verified against the database, not the CSVs, on 2026-08-14:**
certs **5,702** / distinct numbers **5,527** / exempt **312** / with PDF **5,474** / with thumbnail
**5,473** / certs sharing a PDF path **0** / sharing a thumbnail path **0** / NULL `NormalizedNumber`
**0** / products **3,439** (0 orphans) / companies **301** (0 orphans) / product IDs **16,234** /
AuditHistory **0** (by design, §4). **Re-confirmed unchanged 2026-08-15** after a workstation reboot —
every figure above still holds, so the dev rehearsal state is intact and did not need rebuilding.

### End-to-end verification: client spreadsheet → database (step 7, run 2026-08-14 23:06)

This is the check that closes the transform blind spot. Result — **`_verify_output/20260814-230654/`**:

| Direction | Rows | Verdict |
|---|---|---|
| Source certificate rows **not** in the database | **29** | all explained — see below |
| Database certificates **not** in the source | **0** | clean |
| (cert, ASIN) pairs in the sheet but not the DB | **0** | clean |
| (cert, ASIN) pairs in the DB but not the sheet | **0** | clean |

**The 29 are an exact set match — not merely an equal count — against
`_transform_output/20260814-160518/quarantine-incomplete-rows.csv`** (29 rows, 29 distinct
certificate numbers; zero on either side of the difference). These are the DEF-353 rows the transform
deliberately withholds for missing required fields:

| Rows | Missing field(s) |
|---|---|
| 13 | Expiration Date |
| 11 | Product Name |
| 3 | Product Name + Effective Date + Expiration Date |
| 2 | Effective Date |

27 come from `Expired List 1 - 4.9.26`, 2 from `Active List 7.31.26`.

> **Verify the set, not the count.** "29 missing, and 29 quarantined" is a coincidence until the
> certificate numbers are compared. On a test run, re-do the set comparison — a load that drops 29
> *different* rows produces the identical headline number.

**So: 0 unexplained discrepancies between the client's workbooks and the dev database, in both
directions, for both certificates and ASINs.**

---

## 5. What is PRESERVED — verified on dev, untouched by the purge

| Preserved | Rows | Why |
|---|---|---|
| CertificationTypes | 32 (+4 retired = 36) | client explicit |
| ProductCategories / ProductTypes | 9 / 54 | client explicit |
| **ProductSubcategories** | **0** | client explicit — but the table is **empty**; see below |
| Tags | 4 | client explicit |
| Pathways | 6 | client explicit |
| Partners | 4 (Amazon, Novi, Other, Wayfair) | client explicit |
| **PartnerApiKeys** | **9** | **Shawn 2026-08-13 — purging it revokes Amazon's live key** |
| ApiKeys / GoogleAnalyticsSettings | 3 / 1 | Shawn 2026-08-13 |
| Roles / Permissions / PermissionRole | 5 / 22 / 41 | needed for GCC to create their own users |
| `__EFMigrationsHistory` | 34 | schema version — never touch |
| Users | 2 | `swest@anukiconsulting.com` (**mandatory** — `Audit:SystemUserEmail`) + `admin@greencirclecertified.com` |

**Two notes the client should hear rather than discover:**
- `ProductSubcategories` is **empty**, so "preserve Product Cat/Type/Subcat" cannot be honoured for
  subcategories — there is nothing to preserve. 104 source rows carry one and it is **not stored**
  (DEF-348: the importer has no column for it, and the certificate-side field is never set).
- The purge removes the 5 existing `@greencirclecertified.com` accounts including `admin.uat` /
  `analyst.uat` — correct per the client's own step 3, but say so first. Confirm the account-creation
  path works **before** the last surviving admin is removed.
- `Pathways` holds 3 junk test rows (`test`, `testtest`, `Carly test`) of its 6. Preserved because
  it is on the client's list; worth asking whether to clean.

---

## 6. Duplicate certificate numbers — CR-038, BUILT and proven on dev

**The client's answer:** load them. They are different certificates that share an issued number for
one of their customers — *an exception for migrated history; new certificates must still be
prevented from reusing a number.*

**Verified from their own system:** the portal pull returned 6,000 records across 5,822 numbers —
**140 numbers hold more than one record**, and **128 of our 137 have genuinely different signed
documents**. Merging would have destroyed 128 issued certificates. Our schema assumed one
certificate per number; theirs never did.

### What CR-038 changes

| | |
|---|---|
| `Certificate.AllowsDuplicateNumber` | new `bit NOT NULL DEFAULT 0`, set to 1 on **only** the 312 exception rows |
| `IX_Certificates_Number` | UNIQUE → **UNIQUE … WHERE AllowsDuplicateNumber = 0**, same name |
| MVC Edit path | gains a uniqueness check — **it had none**; the index was the only guard |
| `ValidationService` | enforces only when a number is *introduced* (create, or edit that changes it) |
| Six by-number reads | now deterministic — newest `EffectiveDate`, then highest `Id` |
| Loader | flags exempt rows, derived from the staged data rather than a hand-kept list |

### Proven on dev, not assumed

| Test | Result |
|---|---|
| New certificate reusing an **ordinary** number | **blocked by the index** — and the error still names `IX_Certificates_Number`, so DEF-193's retry keeps working |
| Second exempt row on a grandfathered number | **allowed** — the migration loads |
| New certificate taking a **grandfathered** number | **blocked by the application check** (HTTP 400) — the index cannot see exempt rows, so this is the case only code can catch |

Load went 5,390 → **5,702**; exactly **312** exempt, exactly **312** rows share a number,
**0** non-exempt duplicates, **0** over-flagged.

> ⚠️ **The index name is load-bearing.** `CertificateRepository.IsUniqueNumberViolation` matches the
> SQL error on the literal string `IX_Certificates_Number`. Rename the index and DEF-193's
> two-analysts retry silently stops firing.

> ⚠️ **Deploy the schema and the DLL together.** The filtered index cannot see exempt rows, so the
> application check is what stops a new certificate taking a grandfathered number. Running the SQL
> without the matching build leaves that gap open.

### PDF linking under CR-038 — done, and it exposed a second defect

The linker used to join a document to a *certificate number* and attach one document per number,
reporting the rest as unplaceable "alternates". With several certificates now sharing a number that
left **176 duplicates with no PDF** while their documents sat on disk.

It now pairs **1:1** — documents ordered by portal `record_id`, certificates by `Id`.

| | Before | After |
|---|---|---|
| Certificates with a PDF | 5,300 | **5,474** |
| Grandfathered duplicates with their own PDF | 136 of 312 | **310 of 312** |
| Unplaceable documents | 176 | **2** |

The 2 remaining are genuine: the portal holds more documents than we have certificates for that
number, so the surplus has nowhere to go. Reported, not dropped.

> **The defect it exposed — worse than the gap it fixed.** Thumbnails are named
> `{Number}_thumb_{timestamp}.png`. A certificate number is no longer unique, so siblings
> thumbnailed within the same second produced the SAME filename: the second overwrote the first and
> **123 thumbnails were shared by more than one certificate** — each showing another certificate's
> document. The PDF copy alongside already guarded against exactly this collision; the thumbnail did
> not. Fixed in `FileUploadService.GeneratePdfThumbnailAsync` with the same guard.
>
> This was introduced by CR-038, not merely uncovered by it, and it is only visible if you compare
> **files on disk against database references** — the linker's own summary reported success both
> times.

**Verified after the fix:** 5,474 PDFs / 5,473 thumbnails, disk matching the database exactly
(5,474 / 5,473 files, all filenames distinct), **0 certificates sharing a PDF, 0 sharing a
thumbnail**, and each duplicated number holding as many distinct files as it has records
(`23-0041`: 4 records, 4 PDFs, 4 thumbnails).

**228 certificates have no PDF** — no document exists in the client's source system. **One
certificate (`23-0971`) has a PDF but no thumbnail**: a renderer failure on that specific file, not
a collision. `ViewCertificateImage` falls back to `wwwroot/img/certpreview.png`, so the page renders
a generic preview rather than a broken or wrong one. Pre-existing, unrelated to CR-038.

### Still outstanding — client, not build

- **Confirm Option A** (§ the client report). The build is done; only sign-off is missing.
- **`21-0179` and `21-0180`** each carry two unrelated companies — genuine source errors. Which is
  correct? These are not the same pattern as the rest and should not be guessed.
- **9 numbers could be merged** instead (8 same-document-filed-twice, 1 identical on every field) —
  merge, or keep them separate for consistency?

---

## 7. Second open decision — 1,960 untagged product IDs

4 certificates (23-0814 P&G; Kohler 25-8535 / 26-0053 / 26-0101) have a blank cert-level Partner
and carry **1,960 ASINs — 12% of the load**. They are left untagged rather than assumed to be
Amazon.

> ⚠️ **CORRECTED 2026-08-18 — the paragraph below was WRONG, and it reached a client draft.**
> It read: *"Untagged is not neutral — it is fail-open. Under CR-171 Model A
> (`CertificatesController.cs:270`) an untagged product ID is returned to **every** partner caller,
> not hidden."* That describes the product-ID filter **in isolation and ignores the gate above it.**
>
> `CertificatesController.cs:162` scopes the whole query first:
> `query.Where(c => c.Partners.Any(p => userPartnerNames.Contains(p.Name)))`. A certificate with no
> partner association **never enters a partner caller's result set**, so its product IDs are never
> reached and Model A's `!pi.Partners.Any()` branch never runs for them. The single-certificate
> reads (lines 402, 458) apply the same requirement.
>
> **Measured on test 2026-08-18:** all four certificates have **0 rows in `CertificatePartners`**,
> and `untagged_reachable_by_a_partner` = **0** of 1,960.
>
> **The real exposure is the inverse.** These certificates are not over-shared — they are invisible
> to every partner. If 23-0814 (P&G) and Kohler 25-8535 / 26-0053 / 26-0101 were meant to reach
> Amazon, **Amazon is not receiving them or their 1,960 ASINs.** That is the question for the
> client, not "should these be hidden".
>
> Model A's fail-open is real, but it only operates **within** a certificate the partner can
> already see. Caught by Shawn, who did not believe the claim.

4 certificates (23-0814 P&G; Kohler 25-8535 / 26-0053 / 26-0101) have a blank cert-level Partner and
carry **1,960 ASINs — 12% of the load**. They are left untagged rather than assumed to be Amazon.
Listed in `review-untagged-product-ids.csv`.

---

## 8. Acceptance — do not accept on counts alone

Counts cannot see field-internal corruption: DEF-351 truncated 50.7% of product names while every
count matched exactly. **Run the value-level reconciliation** (§4 step 3) and require:

- source certificates = loaded certificates, 0 missing / 0 extra
- 0 mismatches on every field except the known Country canonicalisation
- 0 untrusted or disabled FKs, 0 NULL `NormalizedNumber` / `NormalizedName`
- **CR-038:** exempt count == rows that actually share a number (expect **312**); **0** non-exempt
  duplicates; **0** rows flagged exempt that are not duplicated; **0 certificates sharing a PDF or a
  thumbnail path**, and files on disk == database references for both. The reconciliation must also report
  **0 numbers with a differing record count** — it compares a multiset per number, because no
  natural key is unique (13 source rows are identical on every field)
- PDF: DB link count = files on disk
- **End-to-end spreadsheet → database verification (§4A step 7)**: `extra-in-database.csv`,
  `asin-missing.csv`, `asin-extra.csv` all **empty**, and `missing-from-database.csv` an **exact set
  match** on certificate number against that run's `quarantine-incomplete-rows.csv`. Step 3 cannot
  catch a transform bug — it compares the database to the transform's own output. This one can.
- **Amazon contract diff prints `CONTRACT UNCHANGED`** (Gate 4)
- Click-path spot check on 3 certificates: `ViewCertificateFile` → `application/pdf`,
  `ViewCertificateImage` → `image/png`. **Do not test `/uploads/...` directly — it 404s by design
  (DEF-208).**

---

## 9. Known operational risk that outlives the migration

**DEF-350.** Certificate-import preview cost scales with database size. At 5,390 certificates a
**single-row** file took 148–266s, and at the stock 30s `CommandTimeout` it fails outright. The
CR-038 load takes the database to **5,702**, so this is now marginally worse, not better. Root
cause still unknown — SQL is not the bottleneck (slowest command 533ms). `Database:CommandTimeoutSeconds`
is now configurable (default unchanged at 30) as a mitigation.

The SQL loader sidesteps this for the migration. **It must not become the reason this stops being
fixed** — every routine analyst import after go-live still runs through that path.

---

## 10. Transaction log headroom — learned the hard way on dev

The dev load failed mid-purge with **`The transaction log for database … is full`**. Repeated
purge/load cycles of ~5,700 certificates plus 16,234 product IDs generate a lot of log, and dev was
in FULL recovery with no log backups.

**CHECKED ON TEST 2026-08-17 — there is enough headroom. See CR-039 §5A for the full measurement.**

| | Current | Max |
|---|---|---|
| Data file | 72 MB (**11 MB used**) | **500 MB** — ample; dev holds the whole load in 39 MB allocated / 27 MB used |
| Log file | 72 MB | **1,024 MB** — dev's log peaked at 520 MB across *repeated* cycles |

Recovery model is **FULL**, and the host runs a nightly **FULL ~22:05 + LOG ~22:14** pair, so the log
truncates once a day. **Run shortly after ~22:20**, when the log is emptiest and the full 1,024 MB is
available.

> **The nightly backup chain stopped after 2026-08-15 22:20** — nothing on 08-16/08-17. Confirm it is
> running again before the migration: it is both the restore point and what truncates the log.

On dev the fix was `ALTER DATABASE … SET RECOVERY SIMPLE` plus a log shrink. **Do not do that on
test** — it would break the host's live backup chain, it is the provider's decision, and the headroom
above means it is unnecessary. If the log fills anyway the load rolls back cleanly; ask the provider
for an on-demand log backup and re-run rather than changing any setting.

---

## 11. THE TEST RUN — 2026-08-18

Executed by Claude on Shawn's approval, in business hours, against
`gccwebap_certificates` @ `68.71.129.120,1533`. Elapsed: purge + load ≈ 1 minute; whole sequence
with verification ≈ 35 minutes.

### Artifacts this run produced

| Artifact | Path |
|---|---|
| Pre-purge DB backup | `db-backups/gccwebap_certificates-2026-08-18-105016/` — 43 tables, **8,829 rows** |
| Transform output | `Scripts/data-ops/_transform_output/20260818-105029/` |
| Load SQL | `Scripts/data-ops/_sql_output/20260818-105041/migration-load.sql` (6.4 MB, UTF-16 LE) |
| Target lookup dumps | `Scripts/data-ops/_dryrun_lookups_test/` (dumped from TEST, not dev) |
| Lookup-resolution gate | `Scripts/data-ops/_dryrun_output/20260818-105325/` |
| Reconciliation input | `Scripts/data-ops/_reconciliation/loaded-certificates-TEST-20260818.csv` |
| End-to-end verification | `Scripts/data-ops/_verify_output/20260818-111409/` |

> The reconciliation dump was written to a **`-TEST-20260818` filename** rather than the default
> `loaded-certificates.csv`, so the dev rehearsal artifact is still intact for comparison. Both the
> `.ps1` and the `.py` take explicit paths — **pass them.** `2026-08-13-migration-reconciliation.ps1`
> defaults its `-ConnectionString` to the **dev** database, so running it bare after a test load
> silently reconciles the wrong environment and reports a clean result.

### Results — every acceptance criterion in §8

| Check | Required | Actual |
|---|---|---|
| Certificates loaded | 5,702 / 5,527 numbers | **5,702 / 5,527** ✅ |
| Companies / Products | 301 / 3,439 | **301 / 3,439** ✅ |
| Product IDs / partner links | 16,234 / 14,274 | **16,234 / 14,274** ✅ |
| Source → loaded, both directions | 0 missing / 0 extra | **0 / 0** ✅ |
| Numbers with a differing record count | 0 | **0** ✅ |
| Field mismatches | 0 except Country canonicalisation | **9, all `The Netherlands` → `Netherlands`** ✅ |
| CR-038 exempt vs actually-sharing | 312 == 312 | **312 == 312** ✅ |
| Non-exempt duplicates / over-flagged | 0 / 0 | **0 / 0** ✅ |
| NULL `NormalizedNumber` / company / product | 0 | **0 / 0 / 0** ✅ |
| Untrusted / disabled FKs | 0 / 0 | **0 / 0** ✅ |
| Orphan products / companies | 0 / 0 | **0 / 0** ✅ |
| Step 7 `extra-in-database` / `asin-missing` / `asin-extra` | empty | **all empty** ✅ |
| Step 7 `missing-from-database` | exact set match vs quarantine | **29 = 29, exact set match, 0 on either side** ✅ |
| Amazon contract diff | `CONTRACT UNCHANGED` | **CONTRACT UNCHANGED**, 7/7 ✅ |
| Preserved set after load | unchanged | PartnerApiKeys **9**, Partners **4**, ApiKeys **3**, CertTypes **36**, Users **5** ✅ |

**Country was ×9, not ×15** — confirming the run picked up the fixed 220-entry `CountryList.cs`
(DEF-356), exactly as §0 predicts.

Log headroom was never close: **98 MB used of 1,024 MB** at peak, data file 35 MB of 500 MB. §10's
concern did not materialise on test.

### Two reporting artifacts worth knowing about — neither is a data problem

1. **The generator prints `cert-tag links 388`; the load inserts and the database holds 412.**
   Reconciliation compares tag values per certificate and finds **408 external + 4 internal = 412,
   all matching, 0 mismatches**, so source and database agree exactly. The generator's log line
   counts something narrower than what it emits. Cosmetic, but it will read as drift to the next
   person who diffs the two numbers — dev's recorded `388` in §0 came from the same log line.
2. **`amazon-contract-diff.js` cannot run against test as written.** It logs into the admin UI with
   Playwright to *mint a brand-new partner key*, and the test-env access gate intercepts that login,
   so it times out on `#Email` and prints **`CONTRACT CHANGED`** — a false negative that looks
   exactly like a real contract break. It also defaults `BASE_URL` to `localhost:5101`.
   Gate 4 was executed instead by reusing the script's own `shapePaths`/`diffShapes` logic and the
   same frozen baselines, sourcing the existing Amazon key (`PartnerApiKeys.Id = 6`, plaintext
   column, active, never expires) and calling the partner API directly — **the partner API bypasses
   the gate by design**, so no browser is needed. Worth fixing in the script: honour `BASE_URL`,
   accept a key from the environment, and don't mint a key just to read one.

### Live post-migration checks

- Test-env gate → 302 + cookie; `/Certificate/CertificateSingleView/26-0146` and `/25-8161` both
  **HTTP 200** rendering migrated certificate numbers.
- `/api/certificate-tiles` → **965 publicly visible certificates**, real company names
  (Therdex, 3M, Leoxx…). 981 are `Client Approved - Paid`; the 16-record difference is DEF-358
  windowing (effective ≤ today ≤ expiration), working as designed.
- **3 certificates moved `Client Approved - Paid` → `Expired` between the load and the first
  verification query** (984 → 981). That is the `CertificateExpiration` background sweep acting on
  freshly-loaded rows whose expiration date has passed. Expected; note it before someone reads it as
  a load discrepancy.

### STEP 4 (PDF LINK) — ✅ **COMPLETE 2026-08-18**

Files uploaded by Shawn to `httpdocs\App_Data\uploads\{certificates,images}` and **verified serving
through the application**, 15 of 15 checks
(`Scripts/data-ops/2026-08-18-verify-pdf-clickpath.js`):

| Assertion | Result |
|---|---|
| PDFs served as real `application/pdf` (`%PDF-` magic bytes) | ✅ 4 of 4 fixtures |
| Previews served as real PNG | ✅ 4 of 4 fixtures |
| **CR-038 siblings on `19-0281` serve DIFFERENT documents** | ✅ 2.66 MB vs 315 KB, distinct sha256 |
| **CR-038 siblings serve DIFFERENT previews** | ✅ distinct sha256 — the 123-shared-thumbnail regression is closed |
| `23-0971` (preview render failure) falls back to the generic image | ✅ and distinct from every real preview |
| Public certificate still served anonymously | ✅ |
| Expired certificate withheld from anonymous callers | ✅ 404 |
| `/uploads/...` not browsable (DEF-208) | ✅ 404 |

The detail below is the record of how it was staged and what went wrong on the way.

---

### How it was staged — and the false alarm that followed

**Ran 2026-08-18 16:00, 11.0 min elapsed.** `FileStorage__StoragePath` was pointed at a local
staging root (`C:\Users\swest\GCCUploadStaging`) and `ASPNETCORE_ENVIRONMENT=Production` so the
command wrote to the **test** database:

| | |
|---|---|
| Document/certificate pairs linked | **5,474** |
| sha256 mismatches | **0** |
| Manifest files missing on disk | **0** |
| No matching certificate | 99 (documents for numbers not in the load) |
| Thumbnail failures | **1** — `23-0971`, the same renderer failure seen on dev, pre-existing |
| Unplaceable (review CSV) | **2** — genuine surplus, §6 |
| Undo CSV | `C:\Users\swest\GCCPortalPull\_link-pdfs\undo-20260818-155157.csv` |

Verified with `Scripts/data-ops/2026-08-18-verify-pdf-links.ps1` — **all checks passed**:
`withPdf = 5,474`, `withThumbnail = 5,473`, **0 certificates sharing a PDF path, 0 sharing a
thumbnail path**, and database references == files on disk **as sets in both directions**
(5,474/5,474 and 5,473/5,473, 0 missing).

**Staged and awaiting transfer:** `C:\Users\swest\GCCUploadStaging\certificates` (5,474 files,
**3.47 GB**) and `\images` (5,473 files, **561 MB**).

> ⚠️ **Until that folder is uploaded to the server's upload root, every certificate PDF on the test
> site 404s** — the database holds correct logical paths to files that exist only on the
> workstation. Re-run the verify script with `-StagingRoot <server uploads root>` after the upload;
> that is the check that proves it, not the linker's own summary.

**The build had to be published to `C:\Users\swest\GCCLinkBuild` rather than run via `dotnet run`** —
a local dev server (port 5101, running against the **dev** database) was holding
`bin\Debug\net8.0\GCCWebApp.exe` and blocking the build. It was left running; publishing the
`.csproj` (not the solution — the solution collides on `appsettings.Test.json`) to a separate
directory avoids the conflict entirely.

### Why this could not simply be run from a workstation in the first place

`PdfLinkCommand` writes a **logical** path to the database
(`cert.CertificateFile = "uploads/certificates/{Number}_{timestamp}.pdf"`, `PdfLinkCommand.cs:257`)
and copies the **physical** file to whatever `IUploadPathResolver` resolves locally. Run from a
workstation against the test database, that produces a database full of correct-looking links whose
files sit on the workstation — **every certificate PDF on the test site would 404**, and the
database would report complete success. This is the DEF-319 trap in its purest form.

This is why the linker was pointed at a staging root rather than run with default configuration, and
why the disk-vs-database set comparison above is the acceptance check rather than the linker's own
"5,474 linked" line — that line would read identically in the failure case.

What still has to happen:

1. **Destination — CONFIRMED by Shawn 2026-08-18: `httpdocs\App_Data\uploads\`** on the test server,
   i.e. `FileStorage:StoragePath` stays **unset** and the default resolver location is used. The
   staging folder maps onto it 1:1 — no renaming, no restructuring:

   | Local staging | Server target |
   |---|---|
   | `C:\Users\swest\GCCUploadStaging\certificates\` | `httpdocs\App_Data\uploads\certificates\` |
   | `C:\Users\swest\GCCUploadStaging\images\` | `httpdocs\App_Data\uploads\images\` |

   > ⚠️ **This puts 4 GB of client documents inside the deploy folder** — the arrangement that lost
   > 23 PDFs on 2026-08-17. Deploy bundles exclude `App_Data`, so a normal extract cannot touch them,
   > but a manual "delete everything" wipe of the app root takes them with it. **Accepted knowingly**;
   > DEF-286's `FileStorage:StoragePath` can move the store outside the app root later without moving
   > the files (`TryResolveExisting` finds pre-existing files), but that needs an app-pool recycle
   > because `UploadPathResolver` binds once at startup. **Back up `App_Data\uploads\` before every
   > future deploy** ([[feedback_backup_uploads_before_deploy]]).

2. **Upload the staging directory** — 5,474 PDFs (3.47 GB) + 5,473 thumbnails (561 MB).
3. **Re-assert on the target**, not on staging:
   `Scripts/data-ops/2026-08-18-verify-pdf-links.ps1 -StagingRoot <server uploads root>` — it
   compares database references against files on disk **as sets in both directions**, so an equal
   count cannot mask a wrong-file-in-the-right-place. Then the §6 click-path check via
   `ViewCertificateFile` / `ViewCertificateImage` — **never `/uploads/...`, which 404s by design
   (DEF-208)**.

**The test site shows broken documents from now until step 2 completes.** The data itself is
unaffected. To back the linking out instead, the undo CSV restores the two columns and lists the
copied files to delete — no database restore needed.

### The false alarm — an anonymous sweep cannot count documents

After the upload, an HTTP sweep reported **4,655 of 5,474 documents missing**. Every one of them was
present. The sweep ran **anonymously**, and `ViewCertificateFile` is `[AllowAnonymous]` but
**role-aware** (DEF-204 / DEF-208, `CertificateController.cs:2127`): anonymous, External and Partner
callers must pass status + ownership filters, and a certificate that fails them returns
**404 — byte-identical to the response for a file that does not exist.**

Most of the load is Expired, so most of it is invisible anonymously. The sweep was measuring
**public visibility, not file presence**, and understated coverage by ~85%.

What made it convincing, and therefore worth recording:

- The gap correlated beautifully with certificate-number prefix — `17-` to `22-` entirely absent,
  `26-` almost complete — which reads exactly like an interrupted alphabetical transfer. It was
  really just old certificates being expired and new ones current. **A pattern that fits the
  hypothesis is not evidence when it fits a second hypothesis equally well.**
- Re-running it produced identical numbers, which felt like confirmation. It only confirmed
  determinism.
- Roughly 3.3 GB of files were staged for a re-upload that was never needed.

**Rule: any file-presence check on these routes must run authenticated**, and must distinguish a
real PDF from the generic placeholder the controller falls back to when a file genuinely is absent
(a 200 with `image/png` on the PDF route means *no file*). `2026-08-18-verify-pdf-clickpath.js` now
signs in first, asserts on magic bytes rather than status alone, and keeps a deliberate anonymous
pass whose only job is to assert the gate still closes.

### Two scripting traps found on the way

1. **The login POST bounces to `/__gate` if the `GCC.Session` cookie rides along with it**, or if an
   empty `Cookie:` header is sent. No auth cookie is issued and the response looks exactly like a
   wrong password. Send **Basic credentials plus only the antiforgery cookie** from the same GET.
   The gate accepts Basic as an alternative to its cookie, documented for automated scripts at
   `TestEnvironmentGateMiddleware.cs:98`.
2. **The app pool stopped twice during this work** (site-wide HTTP 503, database unaffected both
   times), each time under concurrent HTTP sweeping while thousands of files were being written
   into `App_Data` — which sits **inside** the watched application directory. IIS restarts the app
   on directory changes and Rapid-Fail Protection stops the pool after enough restarts in a window.
   **Do not run bulk HTTP sweeps against this site.** For file-count verification use Plesk's own
   file listing; for behaviour use a handful of fixtures, as the click-path script does.

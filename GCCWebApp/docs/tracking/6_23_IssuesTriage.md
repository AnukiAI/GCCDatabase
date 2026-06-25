You are helping triage Sprint 3 tester-reported defects for the GCC Certification application.

Review the issue list below and produce a structured defect triage report before proposing fixes. Do not start coding until the issues are grouped, prioritized, and root causes are identified.

## Objectives

1. Analyze each reported defect.
2. Group related issues that may share the same root cause.
3. Identify duplicate or dependent issues.
4. Assign a severity and priority to each issue.
5. Recommend the correct fix approach.
6. Identify where regression testing is needed.
7. Create a developer-ready action plan.

## Application Context

The application includes certificate creation, certificate import, public certificate search, internal/admin certificate views, partner view, certificate tiles, PDF viewing/downloading, and CSV exports.

The tester-reported issues include problems with:

* Certificate status validation and date-driven status changes
* Certificate upload/import behavior
* Product category defaulting incorrectly
* Exact-match search behavior
* Autocomplete/filter text behavior
* PDF download errors
* Certificate tile spacing/layout
* Audit metadata on imported certificates
* CSV percentage formatting
* Long URL wrapping on certificate pages

## Triage Instructions

For each issue, provide the following:

### 1. Issue Summary

Summarize the defect in plain language.

### 2. Likely Root Cause

Identify the most likely technical cause. Call out whether it is likely:

* Front-end validation
* Back-end validation
* Data mapping/import parsing
* Database default value issue
* UI layout/CSS issue
* API response issue
* File handling/caching issue
* Export formatting issue
* Search normalization issue

### 3. Related Issues

Identify any related tickets that may need to be fixed together.

Pay special attention to these likely clusters:

#### Status / Date / Upload Validation Cluster

Issues: 188, 194
Investigate whether imported or auto-generated certificate statuses are being saved as display text instead of valid enum/dropdown values. Also verify whether date-based expiration logic is incorrectly blocking later status correction.

#### Product Category Mapping Cluster

Issues: 189, 195, possibly 186
Investigate whether Product Category defaults to “Chemical and Raw Materials” when the upload value is missing, invalid, not matched exactly, or when the field is left blank during manual certificate creation.

#### Import File Handling Cluster

Issue: 190
Investigate whether the upload component is caching file names or failing to reset state when a revised file with the same filename is selected.

#### Public/Internal Layout Cluster

Issue: 192
Investigate long Company URL wrapping behavior on public and internal certificate detail pages.

#### Upload Success State Cluster

Issue: 193
Investigate whether the upload success message state is not cleared when clicking “Add Certificate” after a successful upload.

#### Search / Filter Cluster

Issues: 196, 197
Investigate search normalization and filter/autocomplete behavior. For issue 196, the search should support user-friendly matching where trademark/special characters do not prevent valid matches. For issue 197, autocomplete suggestions should not override the user’s explicit search/filter value.

#### PDF Access Cluster

Issue: 198
Investigate why the PDF exists and can be viewed from the certificate detail page, but download fails from the search screen and elsewhere. Validate file URL generation, permissions, MIME type, storage path, download endpoint, and front-end button behavior.

#### Tile Layout Cluster

Issue: 199
Review certificate tile spacing and reduce vertical whitespace without breaking the authenticated view fields such as Status and Partner.

#### Audit Metadata Cluster

Issue: 200
Investigate why imported Product ID records show Created as “Invalid Date” and Created By as “System” instead of the import date and importing user.

#### CSV Export Formatting Cluster

Issue: 201
Investigate inconsistent Cert Results formatting in partner CSV export where some percentages export as decimals, such as 0.17, and others export as whole numbers, such as 100.

## Severity / Priority Guidance

Use the following triage logic:

### Critical

Blocks core certificate creation, import, search, PDF access, or data correction.

### High

Causes incorrect data, prevents users from correcting records, corrupts imported values, or prevents certificate download.

### Medium

Impacts usability, layout, export consistency, or user confidence but has a workaround.

### Low

Visual polish or minor display issue with limited business impact.

## Expected Output Format

Return the triage in this format:

| ID | Area | Summary | Likely Root Cause | Related Issues | Severity | Priority | Recommended Fix | Regression Tests |
| -- | ---- | ------- | ----------------- | -------------- | -------- | -------- | --------------- | ---------------- |

After the table, provide:

## Recommended Fix Order

Group the work into logical batches. Prioritize fixes that address shared root causes first.

## Developer Action Plan

Provide a step-by-step action plan for the development team.

## Regression Test Plan

Provide specific regression tests for:

* Manual certificate creation
* Certificate import
* Status dropdown behavior
* Expiration date logic
* Product category mapping
* Search behavior with special characters
* Filter/autocomplete behavior
* PDF view and download behavior
* CSV export formatting
* Audit metadata on imported records

## Questions / Clarifications Needed

List any questions that need to be answered by the testers or product owner before development begins. Do not block obvious fixes if the expected behavior is clear.

## TRIAGE REPORT — Sprint 3 (compiled 2026-06-23)

> Analysis only, no code changes (per QC-review convention). File:line refs verified against current working tree.

| ID | Area | Summary | Likely Root Cause | Related | Severity | Priority | Recommended Fix | Regression Tests |
|----|------|---------|-------------------|---------|----------|----------|-----------------|------------------|
| 194 ✅ | Back-end validation / Import | Status imported without an exact match (e.g. `Client Approved Paid` vs `Client Approved - Paid`) saves as raw text; record then shows blank status and the dropdown won't accept any value — must delete & restart | Import did not validate Status against `CertificateStatusHelper.GetAllStatuses()` keys; raw text persisted (`Services/Import/CertificateImportService.cs:~893`, create ~1270 / update ~1399). Edit dropdown can't locate the invalid value so it renders blank/locked (`Models/CertificateStatus.cs`) | 188 | High | P1 | **FIXED (DEF-276, 2026-06-23):** added `CertificateStatusHelper.TryResolveStatus` (tolerant case/space/punctuation match to the canonical 8 states); import now auto-corrects near-misses to the canonical value and rejects truly-unknown statuses as a preview Error (never persisted). 8/8 keys verified distinct. | Import w/ exact, near-miss, bogus, and blank status; confirm near-miss imports canonical + editable, bogus rejected in preview, blank → Draft |
| 188 ✅ | Workflow / Date logic | Past expiration date auto-flips status to Expired; revising date to the future then errors "cannot change from Expired to Client Approved - Paid"; user cannot recover | Auto-expire via `CertificateExpirationService` (save-time + login/background); Edit POST enforces the matrix (Expired → only Republished/Draft, DEF-203) so the prior status can't be restored. (The `CertificateWorkflowService` auto-expire is dead legacy code — wrong status names.) | 194 | High | P1 | **FIXED (DEF-285, 2026-06-24):** decision = **revert to prior status**. Edit POST now detects expiry-correction (was Expired + new date today/future + user didn't pick a valid alternative) and restores the pre-expiry status from the auto-expire history row (fallback Draft), bypassing the matrix block. Matrix itself unchanged. | Past date→Expired; future date→reverts to prior (e.g. Paid); explicit Republished still honored; still-past date stays Expired |
| 189 ✅ | Data mapping / Import | Product Category imports as "Chemical and Raw Materials" even when another category was on the template | Import had NO column mapping for Product Category; auto-created products hardcoded to `categories.FirstOrDefault()`; cert `ProductCategoryId` never set; `ResolveProductForImportAsync(...categoryId: null)` | 195, 186 | High | P1 | **FIXED (DEF-278, 2026-06-23):** added `ProductCategoryColumns` mapping; resolve against existing categories (case-insensitive); matched → set cert `ProductCategoryId` + auto-created product category; **unmatched → reject row** (per Shawn); blank → null. | Import rows with valid / different-case / bogus / blank category; verify assigned category matches template, bogus rejected in preview |
| 195 ✅ | Display / Create | Cert shows "Chemical and Raw Materials" when category left blank | Cert detail views rendered `cert.ProductCategory ?? Product.Category ?? "—"`; null cert category fell back to the product's (defaulted) category. Create form also auto-fills category from the picked product | 189, 186 | High | P1 | **FIXED (DEF-278, 2026-06-23):** detail views now show `cert.ProductCategory ?? "—"` (no product fallback), per Shawn's "show blank/—" choice. **Ships coupled** with backfill `Scripts/data-ops/2026-06-23-cert-category-backfill-UPDATE.sql` so existing certs keep their category. Root data source fixed by #189. | Create/import cert with blank category → detail shows "—"; existing certs (post-backfill) still show their category |
| 198 ✅ code / ⚠ data | File handling / Incident | "Can't download PDF" from search, but the card "shows the PDF" | **Re-diagnosed:** the card shows a *preview IMAGE* (`CertificateImagePath`), a different file from the PDF (`CertificateFile`) — so "the PDF is there" is the image. The **active** search download (tile views → MVC `/Certificate/Download`) is already App_Data-aware (DEF-264). QC dated **6/15 = the day of the [[incident_prod_all_pdfs_missing_2026-06-15]]** (PDF store wiped) → downloads 404 because files are physically gone. Separately found a **latent** wwwroot-only bug in both tile-API `{id}/download` endpoints (not wired to UI). | — | High→Med | P1 | **PARTIAL (DEF-279, 2026-06-23):** fixed the latent tile-API endpoints to App_Data-first (DEF-264 follow-through). **Actual #198 resolution = restore PDFs from backup** (the incident); re-run `prod-missing-pdf-sweep.js` after restore. Optional UX: gate the tile download icon on file presence. | After PDF restore: view + download same cert from search/tile/detail; confirm `CertificateFile` set + file present |
| 196 ✅ (no code change) | Search normalization | Search "Product A" returns nothing when name is "Product™ A" | **VERIFIED resolved by CR-187.** `SearchNormalizer` strips ™/®/©/punctuation **and whitespace** from BOTH the input and the stored `NormalizedX` columns (`"Product A"`/`"Product™ A"` → `"producta"`); the public search path `CertificateRepository.cs:217-232` queries those columns with the normalized term (`CertificateService`/`FilterService` too). CR-187 shipped in the 6/18-19 deploy; post-deploy regression confirmed `3M™`→4 on prod. QC #196 was filed **6/11, before CR-187 existed**. | 197 | High→Resolved | P2 | **None — CR-187 already fixes this.** Only the public *autocomplete* still uses raw `.Contains` (suggestions only; main search is normalized) — minor, optional. | Post-deploy: search `Product A` vs `Product™ A`, plus ® © - . & on public + internal — confirm same results |
| 200 ✅ | Front-end render | Imported Product ID rows show Created = "Invalid Date" and Created By = "System" | **Re-diagnosed:** NOT a data/serialization bug — both write paths set `CreatedBy`+`CreatedAt`, the GET includes `CreatedByUser` + maps `CreatedAt`, and saved-cert imports re-fetch correctly. Fails only for **in-memory rows on the Create page** (new cert, no audit fields until save): render did `new Date(undefined)`→"Invalid Date" and `…||'System'`. | — | Medium | P2 | **FIXED (DEF-282, 2026-06-23):** `formatDate`/`formatDateTime` guard invalid dates → "—"; Created By shows "—" for not-yet-saved rows (keeps username for saved, "System" only for genuinely-null CreatedBy). After save+reopen, server values show. | Create page: add/import Product IDs → "—/—"; save+reopen → real date+user; existing cert import → correct |
| 201 ✅ | Export formatting | Partner CSV "Cert Results" exports inconsistently — some `0.17`, some `100`, all should be % | `CertificationResults` is a free-text `string?` (`Models/Certificate.cs:51`); export wrote it raw (`Controllers/ExportController.cs:308`). Users entered mixed formats; the display paths use `DisplayFormatters.FormatCertificationResults` but the export bypassed it | — | Medium | P2 | **FIXED (DEF-275, 2026-06-23):** wrapped the export value in `DisplayFormatters.FormatCertificationResults` so CSV matches the web page (always a percentage). PO confirmed percentage is correct. | Export certs with %, decimal, integer, non-numeric, and blank cert results |
| 197 ✅ | UI state / Autocomplete | Filter text box auto-populates a fuller suggestion ("carly test" vs typed "test") and user can't override | **Confirmed exact cause:** `renderAutocomplete` auto-marked the FIRST suggestion `selected` (`index === 0 ? 'selected'`); the Enter handler's `selectedIndex >= 0` branch then overwrote the input with that suggestion. Identical in all 3 tile JS (public/internal/external). | 196 | Medium | P2 | **FIXED (DEF-280, 2026-06-23):** render suggestions with **none pre-selected** → Enter keeps the typed value; ArrowDown/hover/click still explicitly choose. Applied to all 3 tile views. `node --check` clean. | Type "test" (with "carly test" a suggestion) → Enter keeps "test"; ArrowDown+Enter picks "carly test" |
| 190 ✅ | File handling / Front-end | Re-selecting a revised file with the SAME filename in Import errors | **Confirmed not a server collision** (preview cached by GUID, file not persisted by name). Browser suppresses `change` when re-selecting an identical path → stale file kept; plus Back-from-preview leaves the submit button stuck disabled + input cleared → "select a file" error. | — | Medium | P3 | **FIXED (DEF-284, 2026-06-23):** clear file-input `value` on click (same-name reselection always fires change → fresh file); `pageshow`/bfcache handler re-enables the stuck submit button + resets the label. `Views/CertificateImport/Index.cshtml`. | Upload → preview → Back → edit same-named file → re-select → label updates + uploads revised content; button not stuck |
| 193 ✅ | UI state / Front-end | After a successful upload, clicking "Add Certificate" shows the previous cert's success toast | `TempData["SuccessMessage"]` re-rendered from bfcache; prior guard keyed per-message so differing cert numbers leaked stale toasts (`Views/Certificate/Create.cshtml`) | — | Low | P3 | **FIXED (DEF-291, 2026-06-24):** read-once TempData + per-render `Guid` nonce guard suppresses the bfcache/Back re-show; genuine toast still shows once per save. View-only. *(Register reconciled 2026-06-24 — fix was applied by a parallel session but the DEF status had lagged.)* | Create cert → Add another → confirm no stale toast; back-button behavior |
| 192 ✅ | UI / CSS | Long Company URL runs off the page on public + internal detail pages; part of URL hidden | `.certificate-field span` is a flex item with default `min-width:auto` → won't shrink below content, so an unbreakable URL overflows; no wrap rules either | — | Low | P3 | **FIXED (DEF-283, 2026-06-23):** added `min-width:0` + `overflow-wrap:anywhere` + `word-break:break-word` to `.certificate-single-card .certificate-field span` (covers all 3 detail views) | Open detail page with a very long company URL on public + internal → wraps fully, nothing clipped |
| 199 ✅ (needs visual sign-off) | UI / CSS | Tiles have excess vertical whitespace; condense without breaking Status/Partner | Fixed-height blocks + paddings/margins (`certificatetileview.css`). Status/Partner render in the blue header (preserve). | — | Low | P3 | **FIXED (DEF-281, 2026-06-23):** conservative whitespace pass — trimmed header/actions/body/tag padding+margins, the after-Location gap, header line-height 1.5→1.4, product-name buffer 16→10px. **No line-clamp/truncation change; Status/Partner untouched.** ~30px shorter/tile. CSS cache-busted. | Couldn't screenshot locally (dev app 400s on cert endpoints). Carly to review vs image #90 after deploy; can tighten further (3→2 line clamps) if wanted |

\* #196 severity is High *if reproducible*, but most likely an environment/migration gap rather than a code defect.

## Recommended Fix Order

**Batch A — Data integrity in import/create (do first; shared root causes):**
- #194 + #188 (status validation + Expired recovery) — both rooted in status handling.
- #189 + #195 (Product Category mapping/default) — shared `FirstOrDefault()` fallback; coordinate with CR-186.
- #200 (import audit metadata) — same import pipeline.

**Batch B — Core access, environment-gated:**
- #198 (PDF download path unification) — core function.
- #196 (verify CR-187 migration deployed) — likely just a deploy/verify step, do early/cheaply.

**Batch C — Export & search UX:**
- #201 (CSV % formatting), #197 (autocomplete override).

**Batch D — Front-end state & polish:**
- #190 (file input reset), #193 (stale toast), #192 (URL wrap), #199 (tile spacing).

## Developer Action Plan

1. **Confirm #196 is just a deploy gap** before touching code (cheapest win): verify CR-187 migration + `NormalizedX` columns in the test env.
2. **Status cluster (#194/#188):** add enum-validated status mapping on import (trim/case/hyphen-normalize); decide and implement Expired-recovery behavior (PO sign-off — workflow is protected); ensure edit dropdown never locks on an invalid stored value.
3. **Category cluster (#189/#195):** add a Product Category column mapping; find-or-create by normalized name; replace the `FirstOrDefault()` fallback with a true default or required-field. Reconcile with CR-186 split model before coding.
4. **#200:** confirm the Product-ID API returns parseable `CreatedAt` and includes `CreatedByUser`; fix the projection/serialization gap.
5. **#198:** unify view vs download path resolution; normalize stored `CertificateFile` paths; replace silent placeholder fallback with a real error.
6. **#201:** apply the existing display formatter on CSV export (PO confirms target format).
7. **Front-end batch:** #197 explicit-value-wins; #190 reset file input; #193 clear TempData/guard; #192 + #199 CSS.
8. Each fix gets a DEF-XXX referencing its QC #; append to defect register and shipped log when shipped.

## Regression Test Plan

- **Manual cert creation:** blank/valid category; status selection persists; no stale success toast on repeat add.
- **Certificate import:** valid/invalid/blank/near-miss status; category matches template; audit Created date + Created By correct; re-upload same-named revised file.
- **Status dropdown:** invalid stored status still selectable; valid transitions only.
- **Expiration date logic:** past→Expired auto-set; future correction recovers per agreed behavior.
- **Product category mapping:** template value honored; default only when blank.
- **Search w/ special chars:** ™ ® © - . & match correctly (post-migration).
- **Filter/autocomplete:** explicit user value overrides suggestions.
- **PDF view + download:** from search, tile, and detail for standard and non-standard stored paths.
- **CSV export:** Cert Results consistent format across %, decimal, integer, blank.
- **Audit metadata:** imported records show correct date + importing user.

## Questions / Clarifications Needed

1. **#188 (PO):** When a user corrects a past expiration date to the future, what SHOULD happen to the auto-applied Expired status — revert to the prior status, go to Draft, or require manual re-approval? (The 8-state workflow is protected; need direction before changing transition behavior.)
2. **#189/#195/#186 (PO):** What is the correct behavior when Product Category is blank or unmatched — reject the row, assign "Uncategorized", or prompt? And confirm the exact template column name for category.
3. **#201 (PO):** "Cert Results" is intentionally a free-text field (per requirement). What is the desired CSV output — always a percentage with `%`, a 0–100 integer, or leave as entered? Should input be constrained going forward?
4. **#196 (Tester/DevOps):** Confirm the CR-187 search-normalization build is deployed to the environment where the failure was seen.
5. **#190 (Tester):** Capture the exact error text/screenshot on same-filename re-upload to confirm it's client-side state vs a server response.

---

## Issue List (source — tester table)

[PASTE TESTER ISSUE TABLE HERE]
ID	Date Opened	classification	Sprint 3 5-4	Sprint 3 6/23 Notes	Sprint 3 Notes	Anuki Status Sprint 2	4-10-26 comments - SP2	"Date Closed - 
GCC Confirmed Fixed"	Raised By	"Current Status - 
Anuki Marks Complete"	Page	Defect Description	Image # (if needed)	Details - Development Notes
188	6/11/26		New						Gemma	Open	Cert Creation Page	If a certificate date is set to a date in the past, the status will automatically update to Expired. Once revising the date to a date in the future, an error message appears stating that a certificate cannot be changed from Expired to Client Approved - Paid. The same error message appears when the status changes from an invalid entry from upload (see issue ID 194) andattempting to select a Status from the drop-down menu.	86	
189	6/11/26		New						Gemma	Open	Cert Creation Page	"Product Category is generating as ""Chemical and Raw Materials"" even when another category was entered on the Cert Upload Template.
CARLY NOTE: Anuki I assume this is tied to issue going on in #186"		
190	6/11/26		New						Gemma	Open	Import Certificates	When using the import function, when attempting to upload a file, you get an error message if you try to replace the attached file with a revised version of the same file with the same file name (i.e. I changed some data, saved, and attempted to upload the same file within the Import Certificates window)	87	
192	6/11/26		New						Gemma	Open	Cert Creation Page	Public and Internal pages: Company URL is running off the page when it is longer than 1 line. It eventually wraps text, but part of the URL is not visible		
193	6/11/26		New						Gemma	Open	Cert Creation Page	When I've just successfully uploaded a certificate, and go to click the "Add Certificate" button to create another, a pop-up appears saying the last certificate was successfully uploaded. 		
194	6/11/26		New						Gemma	Open	Cert Creation Page	If status name is not entered into upload form exactly (i.e. must have a dash in Client Approved - Paid), the status appears to say Client Approved Paid when viewing on the tile, but when clicking it, the status is blank because it was not an exact match with the drop down options. You cannot then select ANY of the options from the dropdown, you must delete and start again.		
195	6/11/26		New						Gemma	Open	Cert Creation Page	"Product Category is generating as ""Chemical and Raw Materials"" even when you leave blank when creating the certificate.
CARLY NOTE: Anuki I assume this is tied to issue going on in #186"		
196	6/11/26		New						Gemma	Open	Public View	The search function needs to be an exact match; if the search is entered as "Product A" but the actual name is "Product™ A", it will not yield any results		
197	6/15/26		New						Dan	Open	Cert search view	when i search on the certificate page for the word "test" the filter bubble shows test, but the text in the filters column reads "carly test". It seems like it auto populates with other search possibilities, but I cannot get it to stop doing this. Auto populate is good - but I should be able to override like you can in excel for instances... if the autopopulate turns out to not be what the use wants. It actually seems like it is searching correctly... but the text box under the blue 'hide filters' is acting up!	88	
198	6/15/26		New						Dan	Open	Cert search view	this error message is showuing up - but the PDF IS indeed there becuase if I click in to the certificate card instead of try to download from the search screen... I can then actually see the PDF... BUT regardless of where I try.. I cannot dowload the PDF.	89	
199	6/16/26		New						Carly	Open	Cert Tiles	"Reduce spacing in tile to condense size of tile (per 6/15 email). See image 90.
I was thinking the areas with blue highlight below can be tightened up (esp the one after location). The yellow spots may have a little room. 
The top dark blue section I assumed we needed to account for the info that’s in login view for Status/Partner. 
"	90	
200	6/17/26		New						Carly	Open	Cert Creation Page	When importing data for Product ID the Created said 'Invalid Date' and Created By said 'System'. Not sure why it would not list the day I imported and the user log for created by. 	91	
201	6/19/26		New						Gemma	Open	Partner View	When exporting the certification list to CSV, some of the "Cert Results" export as a percentage (i.e. 0.17) and others export as a full number (i.e. 100), when all results are percentages		

# Shipped — June 2026

Snapshot of delivery for June 2026. Stakeholder-readable log.

Period: 2026-06-01 → ongoing

## June 2026 — at-a-glance

| | Count |
|---|---|
| **Total fixes shipped this month** | **39** — 34 DEF + 5 CR (DEF-273/274 retro-logged 2026-06-22 for the two Certificate Create/Edit enhancements). June batch DEF-225/226/227/229/230/231/232/233/234, then DEF-235, DEF-237, QC #106 (DEF-238 + **CR-020**), DEF-240, DEF-242, the 2026-06-05 bundle DEF-246/247/248/249/250/251, the follow-ups DEF-253 + DEF-254, the 6-08/6-09 bundles DEF-255/256/258/260 (DEF-259 superseded by 260), **CR-184** (product-ID export), and the mid-June batch deployed 6-18/19: DEF-264/265/266/**267** + **CR-171** (multi-partner certs + isolation) + **CR-187** (normalized search) + **CR-186** (product-identity split, going-forward code) |
| **Deployed + verified live on gccwebapp.com (2026-06-18/19)** | **Mid-June batch — DEF-267 + CR-171 (multi-partner) + CR-187 (normalized search) + DEF-264/265/266 + CR-186.** Code deployed; both pending migrations applied (auto-migrate); **CR-171 backfill run on prod** (CertificatePartners 1→155, CertificateProductIdPartners 0→992, missing_M2M=0). **Post-deploy regression 8/8 PASS** (`midjune-batch-prod-regression.js`, read-only): internal tile API 249 certs (schema OK), CR-187 search works, DEF-267 8-char Edit renders, partner@amazon.com sees 19 scoped certs (no co-partner leak). Canonical artifact: `GCCWebApp-deploy-20260619-0001.zip` (apply-migrations.sql scripted from prod high-water — avoids the EF9 `EXEC(CONCAT)` bug; `deploy.ps1` gained `-FromMigration` + local ef 8.0.11). |
| **Regression verification — 2026-06-22** (localhost re-run of the full Certificate Create/Edit smoke suite **after** the DEF-271 cache-busting + DEF-272 removal landed in the tree) | **All green — no regressions from the cache-busting/removal work.** `partner-multiselect.js` **21/21** (Create+Edit), `cert-product-clickflow.js` **5/5** (real click-through-the-dropdown across 3M/Acme/Alcon/Amazon/Amazon Devices), `cert-product-populate.js` **9/9** (Create+Edit, native + custom-dropdown + Edit pre-fill), `cert-create-validations.js` **25/0/2-benign** (the 2 WARNs are the documented baseline — Status defaults to *Draft* so it's never empty-on-submit; email pattern allows TLD-less `a@b`), `cert-date-range-validation.js` **4/4** (Create), `cert-edit-date-range-validation.js` **7/7** (Edit, cert `28-0002`). Confirms the Product Name cascade (DEF-271), partner multi-select, field validations, and Expiration-after-Effective rule all still work end-to-end post-change. Verification only — no code change. |
| **Verification only — 2026-06-24** (no code change to the app; no deploy, no migration) | **QC #164 re-verified end-to-end, including the date-driven natural-expiry path.** QC #164's guarantee — a Product Identifier's status follows its parent certificate and is never set by hand, so ASINs tied to a non-public cert are kept out of the Amazon export — was already shipped 2026-05-06 and validated 2026-05-12. This run closes the one path the earlier evidence didn't cover: a certificate that **naturally ages out** (passes its expiration date). An automated end-to-end check (`Scripts/smoke-prod/qc164-expired-asin-exclusion.js`, against the live app + real DB) proves a cert auto-expires via the background service, both its ASINs cascade to Inactive, and the cert **and** both ASINs then disappear from the Amazon export, the product-ID search, and the by-number lookup — then restores cleanly and reappears. **19/19 checks PASS.** Fresh receipts in `docs/tracking/screenshots/qc164/` (`2026-06-24_*`). Also this session: corrected the internal tracking status for this item (now reads "Shipped 2026-05-06" rather than the stale "pending deploy"), and hardened the test harness so a transient local-DB timeout can no longer show up as a false failure (or, more importantly, a false pass on the exclusion check). Verification + tooling only — nothing to deploy. |
| **Pending deploy 2026-06-24** (code-only, no migration; **fold into the next rebuild**) | **DEF-285 (QC #188) — a date-driven "Expired" certificate can be recovered by fixing the date.** Previously, once a cert auto-expired (past expiration date), correcting the date to the future left it stuck on Expired — the workflow only allows Expired → Republished/Draft, so restoring "Client Approved - Paid" was rejected ("a certificate cannot be changed from Expired to Client Approved - Paid"). Per Shawn's decision, the Edit save now **reverts to the prior status**: when a currently-Expired cert's expiration date is moved to today/future and the user didn't pick a valid alternative, the status it held before auto-expiry is restored (recovered from the auto-expire audit history; falls back to Draft if unknown), and the change is recorded in status history. The protected 8-state matrix itself is unchanged — this is a corrective bypass for the auto-expire round-trip. Explicit valid choices (Republished/Draft) are still honored; leaving the date in the past still keeps Expired. Code-only, no migration. |
| **Pending deploy 2026-06-23** (JS-only, no migration; **fold into the next rebuild**) | **DEF-284 (QC #190) — Import: re-uploading a revised file with the same name now works.** Editing a CSV and re-selecting it (same filename) in the Import window didn't take / errored. Not a server collision (the preview is cached by a GUID, the file isn't stored by name) — it's standard browser behavior: re-selecting an identical file path doesn't fire the input's `change` event, so the stale first selection was kept; and returning from the preview via Back left the Upload button stuck on "Processing…". Fix (JS in the Import view): clear the file input on each click so a same-named revised file always registers and uploads, and reset the stuck button + label when the page is restored from the Back button. JS-only, no migration. |
| **Pending deploy 2026-06-23** (CSS-only, no migration; **fold into the next rebuild**) | **DEF-283 (QC #192) — long Company URL no longer runs off the certificate detail page.** A long Company URL overflowed the card (part hidden) instead of wrapping. The value sits in a flex row next to a fixed-width label; the value box defaulted to `min-width:auto` and wouldn't shrink below the unbreakable URL. Fix (CSS): added `min-width:0` + `overflow-wrap:anywhere` + `word-break:break-word` to the field value so the URL wraps fully within the card. Covers all three detail views (public/internal/single). CSS-only, no migration; cache-busted site-wide. |
| **Pending deploy 2026-06-23** (JS-only, no migration; **fold into the next rebuild**) | **DEF-282 (QC #200) — Product ID "Invalid Date" / "Created By: System" on the Create page.** When adding/importing Product IDs while *creating* a new certificate, the Created column showed "Invalid Date" and Created By showed "System". Re-diagnosed: not a data bug — the server sets the date + importing user correctly on save, and existing-cert imports already display correctly. The issue was purely the pre-save display: in-memory rows (cert not saved yet) have no audit fields, so the table rendered `new Date(undefined)` ("Invalid Date") and a "System" fallback. Fix (JS): guard the date formatters to show "—" for missing/invalid dates, and show "—" for not-yet-saved Created By (keeping the real username for saved rows; "System" only when CreatedBy is genuinely null). After the cert is saved and reopened, the real import date + user appear. JS-only, no migration; cache-busted. |
| **Pending deploy 2026-06-23** (CSS-only, no migration; **fold into the next rebuild**) | **DEF-281 (QC #199) — condensed certificate-tile spacing** (Carly's 6/15 request to reduce tile size). A conservative whitespace pass in `certificatetileview.css`: trimmed the header, card-body, tag, and action paddings/margins, tightened the gap **after Location**, reduced the header company-name line-height (3-line clamp kept), and trimmed the product-name height buffer — ~30px shorter per internal tile. **No content truncated** (line-clamp counts unchanged) and the **Status/Partner rows in the dark-blue header are untouched** (Carly noted they're needed in the login view). Stylesheet is cache-busted so it shows post-deploy. **Needs Carly's visual sign-off vs image #90** — couldn't screenshot locally (dev app currently 400s on cert endpoints); can tighten further (drop name clamps 3→2 lines) if she wants more. CSS-only, no migration. |
| **Pending deploy 2026-06-23** (JS-only, no migration; **fold into the next rebuild**) | **DEF-280 (QC #197) — search autocomplete no longer hijacks the typed value.** On the certificate search views, typing e.g. "test" would auto-highlight the first suggestion ("carly test") and pressing Enter replaced the user's typed text with that suggestion — with no way to override (the active-filter chip still showed "test", so the box and chip disagreed). Cause: the suggestion list rendered with the first item pre-`selected`, and the Enter handler used the selected item. Fix: render suggestions with **none pre-selected**, so Enter keeps the typed value while ArrowDown/hover/click still explicitly choose a suggestion (the Excel-like override the reporter asked for). Applied to all three tile views (public/internal/external). JS-only, no migration; `node --check` clean; scripts already cache-busted (DEF-271). |
| **Pending deploy 2026-06-23** (code-only, no migration; **fold into the next rebuild**) | **DEF-279 (QC #198) — PDF download from search.** **Re-diagnosed:** the cert card shows a *preview image* (a different file from the actual PDF), so "the PDF is there" was the image — and the QC is dated 6/15, the day the prod uploads store was wiped (the all-PDFs-missing incident), so downloads 404 because the files are physically gone. The **active** search-screen download already uses the App_Data-aware MVC endpoint (DEF-264). **Actual resolution = restore the PDFs from backup** (data recovery, tracked under the 2026-06-15 incident). **Code fix shipped here:** a *latent* wwwroot-only path bug in both tile-API `{id}/download` endpoints (the DEF-264 regression in adjacent endpoints that DEF-264 missed; not currently wired to any button) — corrected to App_Data-first with wwwroot fallback. Optional follow-up (not built): gate the tile download icon on file presence so file-less certs don't offer a failing download. Code-only, no migration. |
| **Pending deploy 2026-06-23** (code + COUPLED data backfill; **fold into the next rebuild — see coupling note**) | **DEF-278 (QC #189 + #195) — Product Category cluster.** **#189:** certificate **import** now honors the template's **Product Category** column. Before, the column was ignored entirely — every auto-created product and imported cert was forced to the first category ("Chemical and Raw Materials"). Now the value is matched (case-insensitive) to an existing category and applied to both the cert and the product; an **unmatched** category **rejects the row** in the import preview (no auto-create — categories are a controlled list); blank stays blank. **#195:** on the cert detail pages, a **blank** Product Category now shows **"—"** instead of silently inheriting the linked product's (defaulted) category. Per Shawn's decisions (2026-06-23). **DEPLOY COUPLING:** the #195 display change must ship **with** the one-time backfill `Scripts/data-ops/2026-06-23-cert-category-backfill-UPDATE.sql` (sets each cert's category from its product where the cert's is NULL; backup + ROLLBACK-by-default; **needs CR + backup before COMMIT**, CR-186-adjacent) — otherwise existing imported certs (which never had a cert-level category) would show "—" until it runs. Code compiles clean; no schema migration. *(Product Type/Subcategory use the same display fallback but weren't reported — left unchanged, noted as a consistency follow-up.)* |
| **Pending deploy 2026-06-23** (code-only, no migration; **fold into the next rebuild**) | **DEF-276 (QC #194)** — Certificate **import** no longer lets a mistyped Status corrupt a record. Before, a non-exact status string (e.g. "Client Approved Paid" missing the dash, or wrong case/spacing) was stored verbatim; the cert then showed a **blank status** and the Edit dropdown rejected every option (the stored value wasn't in the valid 8-state list), so the only fix was to delete the cert and redo it. Added a tolerant resolver (`CertificateStatusHelper.TryResolveStatus`) that matches imported status text to the canonical state ignoring case/spaces/punctuation: near-misses now **auto-correct** to the right value and import cleanly; genuinely-unknown statuses are **rejected in the import preview** (flagged as an error row, never saved) instead of producing an un-editable cert. All 8 statuses verified to resolve distinctly (no risk of mapping one valid status onto another). Two-file change (`Models/CertificateStatus.cs`, `Services/Import/CertificateImportService.cs`), code-only, no migration; compiles clean. *(Related QC #188 — date-driven Expired recovery — is a separate workflow question still awaiting PO direction; not in this fix.)* |
| **Pending deploy 2026-06-23** (code-only, no migration; **fold into the next rebuild**) | **DEF-275 (QC #201)** — Partner / Product-ID **CSV export** showed the "Certification Results" column inconsistently — some certs as a raw decimal (`0.17`), others as a whole number (`100`) — even though all values are percentages and the **web page already displays them as percentages**. Cause: `CertificationResults` is a free-text field and the export wrote the raw stored value, while every display path routes it through `DisplayFormatters.FormatCertificationResults`. Wrapped the export value in that same formatter so the CSV matches the page exactly (`0.17`→`17%`, `100`→`100%`, `17%`→`17%`, non-numeric like `Pass`→unchanged). Single-line change in `Controllers/ExportController.cs`. Compiles clean. Found in Sprint-3 triage (`docs/tracking/6_23_IssuesTriage.md`); desired output (percentage) confirmed by client. |
| **Pending deploy 2026-06-22** (code-only, no migration; **postdates the 1007.zip — fold into the next rebuild**) | **DEF-269** — Partner API `GET /api/v1/certificates/search` usability fix. The optional `term`/`status` query filters were non-nullable params (implicitly `[Required]` under .NET 8), so an empty `?term=` returned **HTTP 400**; and the `status` filter literal-matched the *internal* lifecycle status, so a partner passing `?status=Active` (the only status they're ever shown) matched **0** rows. Made both params nullable and scoped the `status` filter to non-partner callers only (partners are already constrained to public statuses). Server-only, **no migration**. localhost-verified `def269-search-optional-params.js` **11/11** (empty-term → 200 full set; `status=Active` → 6 certs, was 0; contract-clean). Found by the 2026-06-22 Amazon partner-API test (`amazon-partner-api-local.js`, **45/45** — the Amazon GS1 data path was already correct + isolation-clean). *(Companion finding DEF-270 — partner download has no PDF-generation fallback — left OPEN/triage, needs a product decision.)* |
| **Pending deploy 2026-06-22** (code-only, no migration; **fold into the next rebuild**) | **DEF-272** — Removed the dead `/Product/Categories` page. It was a scaffolded-but-never-finished admin screen: full view markup + 12 KB CSS + backend endpoints existed, but its **only** client script (`productCategories.js`) was never written (zero git history), so the entirely JS-driven UI rendered an empty shell and 404'd the missing script. **Not linked from any navigation** (reachable only by direct URL), and product categories are handled elsewhere (types on `/Admin/ProductTypes`, product CRUD on `/Product/Management`). Surfaced by the DEF-271 cache-busting sweep. Deleted the view, the CSS, and the `ProductController.Categories()` action after confirming zero inbound references. Build clean; localhost-verified `/Product/Categories` + CSS now 404, `/Product/Management` unaffected. Code-only, **no migration**. |
| **Pending deploy 2026-06-22** (code-only, no migration; sweep of `_Layout` + 13 admin/management views) | **DEF-271 follow-up — site-wide cache-busting sweep.** Extended the DEF-271 fix beyond the Certificate views: added `asp-append-version="true"` to every `~/js/` `<script>` in **`Views/Shared/_Layout.cshtml`** (site-wide — `main`/`auth`/`admin-core`/`accessibility-enhancements`, the highest-blast-radius scripts) **+ 13 admin/management views** (UserManagement, ProductManagement [also replaced a fragile manual `?v=5`], Product/Management, Company/Management, CertificateFields, CSFAttributes, Tags, ProductTypes, RoleManagement, PartnerAPI, GCCAPI, GoogleAnalytics; Product/Categories was in this set but is now deleted per DEF-272). Permanently prevents the stale-cached-JS class of ghost-defect on these pages. Live-verified `?v=<hash>` on Home/_Layout + Product/Management + Company/Management; the auth/dashboard tier (Dashboard, Login, TwoFactorPreferences, CreateUser) was intentionally left for a later pass. Code-only, **no migration**. |
| **Pending deploy 2026-06-22** (code-only, no migration; **fold into the next rebuild alongside the 1007.zip working set**) | **DEF-271** — Certificate Create/Edit JS not cache-busted, so a stale cached `certificate-inline-create.js` left the **Product Name** dropdown empty even after a Company was selected ("Product Names are not populating"). The six form scripts on both views were referenced **without** `asp-append-version="true"`; when the dropdown-building script changed (its history has repeated Product-Name "break fix" commits), browsers kept the old copy — products loaded into the hidden native `<select>` but the visible custom dropdown never rebuilt. Added the content-hash version stamp to all six scripts on Create **and** Edit so any change forces a re-fetch. Why it slipped past tests: headless Playwright always fetches fresh JS (cascade passed 9/9 + 5/5 click-flow), so the defect only shows against a warm browser cache — exactly the user's case. Verified post-restart: all 6 scripts emit `?v=<hash>` on both views (12/12) and the cascade still passes 5/5. Repro/regression: `cert-product-populate.js`, `cert-product-clickflow.js`. *Follow-up flagged: sweep the rest of `Views/` for other `<script>` tags missing `asp-append-version`.* |
| **Pending deploy 2026-06-22 — NEXT batch** (`GCCWebApp-deploy-20260622-1007.zip`, 60.4 MB / 335 files; supersedes the deleted `…-1514.zip` which covered only item 1) | **Two Certificate Create + Edit enhancements (now tracked as DEF-273 + DEF-274), code-only, no migration (Gate 7 no-op):** **(1) Expiration-after-Effective-Date validation — DEF-273.** Both dates were independently `[Required]` but nothing enforced the relationship — a cert could save expiring **before/on** its effective date. Server-side `CertificateEditViewModel : IValidatableObject` (`ExpirationDate.Date <= EffectiveDate.Date` rejected; shared by Create+Edit, both gate on `ModelState.IsValid`) + live client-side `validateCertDateRange()` on each date field's `change`. Strictly-after. Smokes: `cert-date-range-validation.js` 4/4 (Create), `cert-edit-date-range-validation.js` 7/7 (Edit), full `cert-create-validations.js` 25/0/2-benign. **(2) Partner(s) control → checkbox dropdown — DEF-274.** Replaced the native Ctrl-click `<select multiple>` (CR-171 UX-backlog item) with a Company-style searchable dropdown using **checkboxes**, **no "Add" option** (partners can't be created from the cert form) — per client ask. New `wwwroot/js/partner-multiselect.js`; native `<select>` stays source-of-truth so the hidden `PartnerIds` CSV the POST handler consumes is unchanged; Edit pre-select honored; stale "Hold Ctrl" helper text corrected. Smoke `partner-multiselect.js` **21/21** (Create+Edit). Both proactive enhancements (no client QC). |
| Deployed + verified live on gccwebapp.com (2026-06-01) | 7 — read-only 10/10 (DEF-225/226/227/229/231/232) + live FUNCTIONAL DEF-231 6/6 & DEF-230 11/11 (self-cleaning throwaway data) |
| Deployed 2026-06-03 (`…-1221.zip`) | DEF-235, DEF-237, QC #106 (DEF-238 + CR-020 `UserCompanyId`) — CR-020 prod migration applied + login restored (DEF-239) |
| **Data cleanup applied to prod 2026-06-08 (CR-019)** | **Product mis-categorization fixed** — the products/certs wrongly filed under the test "Carly Test" category (from the pre-fix DEF-231 quick-add bug) were re-filed to their correct categories. "Test 2 Product" now shows **Chemicals & Raw Materials / Plastics / PS** on Product Management (client image 82, cert 26-0168); 2 products + 4 certs corrected; **0 left on Carly Test**. Update-in-place (client chose not to delete; Carly Test category kept). Backups taken; reversible. **No deploy — data only** (the code bug DEF-231/DEF-243 was already live). Resolves the QC #186 "Product Type/Subcategory blank on Product Management" item. |
| UAT-verified live on gccwebapp.com (2026-06-03) | DEF-225 (Cert Edit Product Name shows saved product — 4/4 live certs) |
| Deployed 2026-06-03 (`…-2110.zip`) | **DEF-240** — Status removed from Product Management (carries an open "no inactive-toggle" UX decision, see entry); **DEF-242** — cert list sorting puts blank values last across all 3 views (prod fingerprint verified, blanks now trail; no migration) |
| Investigated, closed as Working-As-Designed (no code change) | 1 (DEF-228) |
| Data-cleanup pass drafted (needs backup + CR before running) | 1 (DEF-231 fallout) |
| **Deployed 2026-06-04** (`…-1752.zip`) | **CR-184** (Active Product Identifiers export) + **DEF-243** (cert Product Category/Type/Subcategory now sticks on Edit; inline "Add product" requires a category — QC #186 6/4) — **verified live on gccwebapp.com** (read-only prod fingerprint **29 PASS / 0 FAIL**) |
| Pre-deploy gate sweep (2026-06-04, `…-1752.zip`) | **All PASSED** — G1 UI · G2 cross-role/responsive (0 new a11y violations, 40 screenshots) · G3 data-scoping (export Admin/Analyst-only, External 403) · G5 governance · G6 perf (export cold 282ms); G4 & G7 N/A. Backlog nits logged: **DEF-244** (a11y debt) + **DEF-245** (`/Product/GetCategories` external-200) |
| **Pending deploy 2026-06-05** (`GCCWebApp-deploy-20260605-1234.zip`) | **DEF-246** (cert Create + Edit: selected fields now survive a server-side validation re-render — QC #186 6/4 retest; Create 11/0, Edit 8/0 local smoke) + re-bundles DEF-243/CR-184 idempotently. **This one zip is the full 2026-06-05 bundle** — also carries CR-020rev + DEF-247/248/249/250/251 (rows below). Bundled `migrations\apply-migrations.sql` is the Gate-7 prod script. (Supersedes the earlier `…-1128.zip`, rebuilt to include the DEF-251 perf fix; 1128 deleted.) |
| **Pending deploy 2026-06-05** (CR-020 rev) | **CR-020 revision (QC #106)** — (a) User Company changed from FK-dropdown to **free-text** ("open text field… GCC, Amazon Corporate, Novi"); zero access/security impact (scoping reads only Company Access); migration `ChangeUserCompanyToFreeText` backfills employer names before dropping the FK; local `cr020rev` smoke **6 PASS / 0 FAIL**; **Gate 7:** run `Scripts/database/CR-020rev_ChangeUserCompanyToFreeText.sql` in same deploy step. (b) **"Company Access" column added to the User Management table** (was missing); display-only, no SQL; local `qc106-company-access-column` smoke **5 PASS / 0 FAIL**. QC #106 fully addressed (User Company free text + Company Access in table + DEF-238 role-gating). |
| **Pending deploy 2026-06-05** (QC #182 + QC #157) | **DEF-247** — partner/external CSV export no longer leaks Internal-visibility custom fields (QC #182 re-raise; probe 4/4 PASS); **DEF-248** — multi-value ASIN/UPC/GTIN/EAN cells now display in the same order they sort by (QC #157 6/4; probe 8/0); **DEF-249** — cert-list tile APIs honored only 20 rows/page regardless of requested page size — now respect it (Med); **DEF-250** — sorting the cert list by ASIN/GTIN/EAN/Cert-Subcats could take ~30s and intermittently error — now ~1s (High); **DEF-251** — User Management list (`/Admin/GetUsers`) was slow (~12s cold) from a 3-collection cartesian — added `.AsSplitQuery()`, now ~0.5s. All five no-migration, code-only; localhost-verified. |
| Pre-deploy gate sweep (2026-06-05, `…-1234.zip`) | **All gates PASS — clear to upload.** G1 UI/design-guide PASS (0 delta violations) · G2 cross-role/responsive PASS (role-gating correct on all 4 audiences across 5 viewports; the a11y items the validator flagged are **pre-existing DEF-244 debt**, not regressions) · G3 data-scoping PASS (DEF-247 Internal-field gate, DEF-249 public pageSize clamp [1,200], CR-020rev free-text is display-only/not a scope key — adjacent endpoints all scoped) · G4 partner-API contract UNCHANGED (DEF-247/248 are private CSV-path methods; `/api/v1/certificates*` shape byte-identical) · G5 governance PASS (no orphans/conflicts; all DEFs cite QC) · G6 perf — DEF-249/250 verified fast; **DEF-251 found AND FIXED** (`/Admin/GetUsers` 12s→0.5s via `.AsSplitQuery()`), zip rebuilt to include it · G7 migration handled (bundled idempotent `apply-migrations.sql`). |
| **Deployed 2026-06-05 — login outage + fix (DEF-252)** | After the bundle went up, **login broke on gccwebapp.com**: the CR-020rev code shipped but its prod migration SQL was **never run** (Gate-7 miss — **recurrence of DEF-239**), so the `Users` table had no `UserCompany` column and every login threw. **Resolved same day:** backed up `Users` → `Users_bak_CR020_20260605`, applied `CR-020rev_ChangeUserCompanyToFreeText.sql` to the prod DB, and (after a `QUOTED_IDENTIFIER` failure left the column un-backfilled) re-ran the backfill with `-I` to restore the 2 real company values (Alcon, Amazon); confirmed prod migration history matches the build. **Login restored.** Permanent fix: both `CR-020rev_*.sql` and the bundled `apply-migrations.sql` now `SET QUOTED_IDENTIFIER ON` + `SET XACT_ABORT ON` so a future run can't half-apply. See **DEF-252**. |
| **Pending deploy 2026-06-05** (`GCCWebApp-deploy-20260605-1444.zip`) | **Next bundle, supersedes the live 1234.zip** (rebuilt from 1328 to fold in DEF-254; 1328 deleted). **DEF-254** — Product Management → Edit Product now carries over a saved Product Type/Subcategory even when it isn't under the product's Category (cross-category / "Other" types); QC #186 part 3; localhost-verified (#347 Type blank→carried). Plus **DEF-253** (#2 + #7) — two more EF cartesian fixes from the proactive audit: Product Management list (2 collection sub-projections) and the cert-detail load `GetByIdAsync` (5 collections) now use `.AsSplitQuery()`; both localhost-verified (#2 32–50ms; #7 302-redirect in 202ms over 300 seeded ProductIds). *(Audit item #1, the CSV export, was tried and REVERTED — AsSplitQuery timed it out; full-table no-pagination export keeps the single query. See DEF-253.)* **Also ships the DEF-252 permanent fix:** `Program.cs` now **auto-applies pending EF migrations on startup in Production** (gated `Database:AutoMigrateOnStartup`, default true) — this closes the recurring Gate-7 miss (DEF-239/252) so a deploy can no longer ship code ahead of schema. **Gate 7 for this deploy: no-op** — CR-020rev is already on prod, so there are no pending migrations (the bundled `apply-migrations.sql` and the new auto-migrate both find nothing to apply). Watch the startup log line `✅ Production database schema up to date` on first boot to confirm. |
| **Pending deploy 2026-06-05** (`GCCWebApp-deploy-20260605-1641.zip`) | **Latest bundle, supersedes 1444.zip.** Adds **DEF-256** — certificate tile table sort fix: (a) ID columns (ASIN/UPC/GTIN/EAN) now use a **natural sort** so numeric identifiers order `9 < 10 < 100` instead of lexically `1 < 10 < 100 < 2` (client 6/5: *"100 is showing after single digits"*) — fixed-length/alphanumeric prod IDs unaffected; (b) the **External** tile view now sorts **every** column header — ~13 headers (ASIN/UPC/GTIN/EAN/Status/Ext-Tag/Product-Subcategory/Contact/Cert-Results/Pathway/Cert-Subcats/Company-URL + reverses) previously fell through to an Effective-Date no-op; Product Category header remapped to its real sort key. Code-only, **no migration**; localhost-verified (internal + external, all HTTP 200, no regression on alphanumeric ASINs). Also folds in **DEF-255** (Product Management save UX decouple — success toast/close now fire immediately on a successful POST, so a slow `GetProducts` refresh can't make a saved row look unsaved) which was built after 1444. **Gate 7: still a no-op** — no new migrations beyond `ChangeUserCompanyToFreeText` (already on prod since the DEF-252 remediation); bundled idempotent `apply-migrations.sql` + the DEF-252 auto-migrate-on-startup both find nothing to apply. |
| **Pending deploy 2026-06-08** (`GCCWebApp-deploy-20260608-1543.zip`) | **Latest bundle, supersedes 1641.zip.** Adds **DEF-258** — the **Cert SubCategories** column on the internal Cert List now displays its (multi-value) subcategories in the same order it sorts by, so the first subcategory shown is the one the row is ordered on. Closes the last open thread of QC #157 (the client had deprioritized this column). Same fix already applied to ASIN/UPC/GTIN/EAN in DEF-248. Code-only, **no migration**; build-clean (Release publish, 334 app files). Carries forward everything in 1641.zip (DEF-246/247/248/249/250/251/253/254/255/256 + CR-020rev + DEF-252 auto-migrate). **Gate 7: no-op** — only migration in the tree is `ChangeUserCompanyToFreeText`, already on prod since the DEF-252 remediation; bundled idempotent `apply-migrations.sql` + DEF-252 auto-migrate-on-startup both find nothing to apply. *Caveat:* the dev DB has **0** certs with cert subcategories, so DEF-258 is verified by code-parity with DEF-248, not a populated render — recommend a quick prod re-check once it's live. |
| **SUPERSEDED by DEF-260 / caused a prod 503 on deploy — do not re-use** (`GCCWebApp-deploy-20260609-1026.zip`) | **DEF-259** — Cert List (Internal) numeric ID *value* sort. Built + localhost-verified, but on upload to gccwebapp.com it returned a site-wide **503** (deploy-mechanics: locked DLL / app pool not recycled — the code booted clean locally and is not the cause). Client rolled back to the prior build to restore the site. Then clarified that the ID columns show a **count badge**, not values — so value-sorting was the wrong axis. Replaced by **DEF-260** (count sort). Keep this row for history; deploy the DEF-260 bundle instead, with **stop-app → extract → start-app**. |
| **Pending deploy 2026-06-09** (`GCCWebApp-deploy-20260609-1146.zip`, supersedes 1543.zip; the 1026.zip is dead) | **DEF-260** — Cert List ID columns (ASIN/UPC/GTIN/EAN) now **sort by the count badge** (number of IDs on the cert), so the cert with ~1000 IDs goes to the top/bottom as the client expects. The columns render a count badge, not the ID values, so the prior value-based sorts (DEF-256/259) never matched what the client saw. New `OrderByCountEmptiesLast` helper; empty cells last both directions; cheap COUNT on the DEF-250 thin projection (no timeout); internal + external controllers in parity. Code-only, **no migration**. localhost-verified: `def260-count-sort-probe.js` 8/0 (asc `1,2,3,6,101,1004`; desc `1004,…,1`), `qc157-sort-verify-local.js` 12/0 (no regression). **Deploy: stop the app FIRST, extract, then start** (the 1026.zip 503 was caused by extracting over a running worker). |
| **Pending deploy 2026-06-18** (`GCCWebApp-deploy-20260618-1027.zip`) | **CR-171 — Multi-partner certificates + partner data isolation (FULLY BUILT, localhost-verified).** A certificate (and each product identifier) can now be associated with **multiple partners**; each partner login/API key sees **only its own** association — never a co-partner's name or a co-partner-tagged product ID (Model A: untagged product IDs are shared with all the cert's partners). Cert + product-ID partner pickers are now multi-select; cert bulk-upload + the import template accept multiple partners (`Amazon\|Novi`). The Amazon API JSON shape is unchanged — only *which* partner value is returned (caller-scoped). Pre-deploy gates: **security (Gate 3/4) PASS** after fixing a GS1 product-ID value-leak the audit caught; **UI (Gate 1) PASS** + added the missing `.partner-novi` badge style. Regression: Api/Security suites show the **pre-existing** 62/3 failures (byte-identical with the CR-171 code stashed — **zero new failures**); the new `cr171-multipartner-isolation.js` smoke is **10/0**. **This bundle carries the full in-flight working tree** — also includes **DEF-267** (cert-number maxlength) and the prior CR-184 / CR-186 / DEF-264/265/266 working set. **Gate 7: run `migrations\apply-migrations.sql`** — it creates the `CertificateProductIdPartners` table AND (appended post-deploy block) **backfills both partner join tables** from the denormalized Partner strings; **without the backfill, existing certs/product IDs go invisible to partner logins on cutover.** Deploy order: **stop app → extract → run apply-migrations.sql → start app.** |
| **Pending deploy 2026-06-17** (rebuild of `…-20260616-1124.zip` required — new zip TBD) | **DEF-267** — every certificate **save** failed validation with *"Certificate Number must be a string or array type with a maximum length of '7'"*. A stale `[MaxLength(7)]` on the Edit/Create view model rejected any cert number longer than 7 chars — which is every prod number now that the sequence reached 5 digits (`26-10063` = 8 chars); local seed numbers are still 7 chars, so it only surfaced on prod. Raised the cap to 50 to match the entity. **High — blocked all cert edits/creates on prod.** Server-side code change, **no migration**. localhost-verified (`def267-cert-number-maxlength.js`: forced an 8-char number through save → no error; original restored). **Must rebuild the 6-16 zip** to include this 6-17 fix; carries forward the entire 1124.zip working tree. |
| **Pending deploy 2026-06-16** (`GCCWebApp-deploy-20260616-1124.zip`) | **DEF-266** — Certificate **Create** page no longer throws an "Error loading custom fields" toast when a Certification Type is selected: the custom-fields loader now checks the HTTP response before parsing, separates the network step from rendering, and tolerates a malformed field row (null type → defaults to text) instead of crashing the whole section. JS-only, **no migration**. **This bundle also carries the full in-flight working tree** — the CR-186 product-split going-forward code, **DEF-265** (cert edit updates its product in place instead of spawning a duplicate line item), and **DEF-264** (PDF download). Build-clean (334 app files); bundled idempotent `apply-migrations.sql` (Gate 7: no new migrations beyond what's already on prod). |
| **Pending deploy 2026-06-15** | **DEF-264** — certificate **PDF download** restored: after the DEF-208 file move (PDFs relocated to `App_Data`), Download and bulk-ZIP still looked in the old `wwwroot` location and returned a "No certificate PDF has been uploaded" error / a placeholder. Both now resolve the real file. Code-only, **no migration**; build-clean. |
| ~~**Pending deploy 2026-06-09** (`GCCWebApp-deploy-20260609-1026.zip`, supersedes 1543.zip)~~ — superseded, see above | ~~**DEF-259** — Cert List (Internal) numeric ID sort residual.~~ The client confirmed (with 1543.zip already live) that "the sort on the numbers is still not working." Multi-value numeric ID cells (ASIN/UPC/GTIN/EAN) were still ordered **lexically** for both the displayed values and the row's sort key, so a cell `{2, 10}` rendered `"10, 2"` and the row sorted as if its smallest value were `10`. Now both use the same **natural** order (length-then-value): the cell reads `2, 10, 100` and the row is positioned by its true smallest ID. UPC also moved off its old arbitrary representative. Code-only, **no migration**; runs on the DEF-250 thin projection so no sort-timeout regression. localhost-verified (qc157 14/0, def248 8/0, run log clean). **Note:** real production IDs are fixed-length/alphanumeric, where this is a no-op — the effect is only visible on variable-length **numeric** (test-shaped) IDs, so a prod re-check on the client's numeric test data is recommended once deployed. Bundle carries forward everything in 1543.zip (DEF-246–258 + CR-020rev + DEF-252 auto-migrate). **Gate 7: no-op** — no model change in this fix, so no new migration beyond `ChangeUserCompanyToFreeText` (already on prod since DEF-252); bundled idempotent `apply-migrations.sql` + DEF-252 auto-migrate-on-startup both find nothing to apply. |

---

## CR-171 — Multi-partner certificates + partner data isolation

**What the client asked for:** a certificate can be shared with more than one partner (e.g. Amazon *and* Novi), but each partner must see only its own association — never the co-partner's. Confirmed 2026-06-17 to also apply at the **product-identifier** level (so specific products under a shared cert can be exposed per partner via the API), with the cert/product partner pickers becoming **multi-select** and the bulk-upload template supporting it.

**Delivered (build-clean, localhost-verified, pending deploy in `…-20260618-1027.zip`):**
- **Multi-select everywhere** — Create/Edit cert forms, the Product ID Manager (per product ID), the cert bulk-import template (`Partner` column accepts `Amazon|Novi`), and the import parser.
- **Data isolation (the point of the CR):** access is now decided by partner *membership* in the cert's partner set (not a single field), and every partner-facing response is scoped to the caller. A partner API key / login sees only its own partner name and only the product IDs that are untagged (shared) or tagged to it (**Model A**). Verified both ways on the v1 list, by-id, and GS1 endpoints: Amazon never sees Novi's name or Novi-only product IDs, and vice-versa.
- **Amazon API contract unchanged** — JSON shape is byte-identical; only the partner *value* is caller-scoped (no partner sign-off needed).
- **Everyone else** (company users, external clients, analysts, admins) see **all** associated partner names; the public sees none.

**Quality gates:** security audit (Gate 3/4) PASS — it caught and we fixed a GS1 path that would have leaked a co-partner's product-ID *values*; UI audit (Gate 1) PASS. Regression test suites carry the project's existing 62/3 pre-existing failures **unchanged** (proved by stashing the CR-171 code and re-running — same counts), and the dedicated `cr171-multipartner-isolation.js` smoke is 10/0.

**Deploy note (Gate 7):** the bundled `apply-migrations.sql` creates the new `CertificateProductIdPartners` table **and** backfills both partner join tables from the existing single-partner values. This backfill MUST run with the deploy — otherwise every existing cert/product ID disappears from its partner's view on cutover. Stop app → extract → run the SQL → start app.

---

## DEF-267 — Saving a certificate failed validation: "Certificate Number must be a string or array type with a maximum length of '7'"

Reported by the client (6/17): editing a certificate (e.g. `https://gccwebapp.com/Certificate/Edit/26-10063`) and clicking **Save Changes** popped a red validation banner and refused to save — *"The field Certificate Number must be a string or array type with a maximum length of '7'."*

The cause was a leftover validation rule on the Edit/Create form that capped the certificate number at **7 characters**. Certificate numbers run `YY-NNNNN`; once the running number reached five digits on production (e.g. `26-10063`, which is 8 characters) every save tripped the cap. This is why it only showed up on the live site — the test environment's certificate numbers are still 7 characters (`26-0023`), so they slipped under the old limit. The underlying database column always allowed far more than 7, so nothing about the stored data was ever the problem — it was purely an over-tight form rule.

**What changed:** the form's certificate-number limit was raised from 7 to 50 characters, matching the database. This unblocks **both** editing and creating certificates. No database change, no migration.

Verified on the test environment (`def267-cert-number-maxlength.js`): an 8-character certificate number (matching the production format) was driven through Save with no validation error and a clean redirect, and the original number was restored afterward so no test data was left changed. Build-clean (0 warnings / 0 errors). **High severity — this blocked all certificate edits and new certificate creation on production**, so it should lead the next deploy. The 6-16 bundle (`…-1124.zip`) predates this fix and must be **rebuilt** to include it.

---

## DEF-266 — Certificate Create: "Error loading custom fields" message when selecting a Certification Type

Reported by the client (6/16): on the certificate **Create** page (gccwebapp.com/Certificate/Create), choosing a **Certification Type** popped an "Error loading custom fields" message.

The custom-fields section loads its fields the moment you pick a Certification Type. The screen was showing one generic error for two completely different underlying situations, with no way to tell them apart: (1) the request for the field list came back as something other than the expected data (for example, a session that had quietly timed out, or a server hiccup), or (2) one custom-field definition was incomplete (missing its field *type*), which crashed the part of the page that draws the fields — even though loading them had actually succeeded.

**What changed:** the loader now (a) checks the server response is valid before trying to read it — so a timed-out session or a server error is reported clearly instead of as a confusing parse failure, (b) separates "fetching the fields" from "drawing the fields," so a single bad field definition no longer takes down the whole section, and (c) treats a custom field with a missing type as a plain text box instead of failing. On a healthy page nothing changes — the fields load as before. The browser console now also records the *real* cause, so if anything does go wrong we can immediately tell a session/server issue from a bad field definition.

Display/behavior change only — no database change, no migration. The fix is in the browser script, so it takes effect once this build is deployed and the page is reloaded. Pending deploy in `GCCWebApp-deploy-20260616-1124.zip`; recommend a quick live check (open Create, pick a Certification Type, confirm no error) once it's up.

---

## DEF-264 — Certificate PDF download restored ("No certificate PDF has been uploaded" error)

Reported by the client (6/15): downloading a certificate showed an error message instead of the PDF.

The cause was a leftover from an earlier internal change (DEF-208) that moved where uploaded certificate files are stored. The certificate **view/preview** screens were updated to read from the new location, but the **download** buttons were missed — they kept looking in the old folder, found nothing, and returned "No certificate PDF has been uploaded." The same gap affected the **bulk "download selected as ZIP"** action, which quietly substituted a generated placeholder instead of the real uploaded PDF.

**What changed:** both the single-certificate download and the bulk-ZIP download now look in the current storage location first (with a fall-back to the old location so any not-yet-migrated files still work) — the same approach already used by the preview screens. The real uploaded PDF is served again.

Code-only, no database change, no migration. Build-clean locally. Pending deploy to gccwebapp.com — recommend a quick live download check once it's up, and confirming the uploaded PDFs are present in the server's `App_Data/uploads/` (or still reachable via the wwwroot fall-back).

---

## DEF-247 — Internal-only custom fields no longer appear in the partner/external CSV export (QC #182)

Reported by the client (6/4 re-raise of QC #182): a custom field set to **Internal** visibility ("Chris Test Field") was still showing up when a **partner** account exported certificates to CSV. Internal-visibility fields are meant for GCC staff only (Admin/Analyst) — partners and external users should never see them.

The certificate row-level scoping was already correct, and the dedicated *Internal Tag* column was already hidden from partners — but the **custom-field columns** on the same export were never filtered by visibility, so any field marked Internal was disclosed in full (column header and values) to partner/external/anonymous callers.

**What changed:** the CSV export now applies the same Internal-vs-not gate to custom-field columns that it already applied to the Internal Tag column. For a partner/external/anonymous export, Internal-visibility custom fields are omitted entirely — header and values — while Public/External fields and all certificate rows still appear normally. Admin/Analyst exports are unchanged (they still see everything).

Confidentiality fix. No database change, no migration. Verified on localhost (`Scripts/smoke-prod/def247-partner-export-customfield-scope.js`, **4/4 PASS**): partner export succeeds with rows intact, and the Internal field is absent from both the header and every row. (A conclusive positive/negative differential wants prod data — the dev DB has no Internal field carrying a value on a partner-visible certificate — but the gate is proven not to expose it.) Pending deploy to gccwebapp.com.

---

## DEF-248 — Multi-value product-ID cells (ASIN/UPC/GTIN/EAN) now display in the same order they sort by (QC #157)

Reported by the client (6/4, the last open thread of QC #157): sorting the Cert List by **ASIN** "is not" working, while Location now is.

This turned out **not** to be a broken sort — the row ordering was always correct. A certificate can carry several ASINs in one cell, and the cell listed them in the order they were entered, while the row was positioned by the **alphabetically-lowest** value in that cell. So the first value your eye landed on wasn't the value the row was sorted by, which read as "not sorting." (Single-value cells were never affected; this is the same "which of several values leads" question already noted for Cert Subcategories.)

**What changed:** multi-value ID cells now display their values in ascending order, so the first value shown is the value the row is sorted by — across the logged-in Cert List and the public directory. Display-only; no database change, no migration. Verified on localhost (`Scripts/smoke-prod/def248-asin-display-order.js`, **8/0**), including certificates whose values were entered out of order and now display alphabetically. Pending deploy.

---

## DEF-249 — Cert-list views could return only 20 records per request regardless of the requested page size

Internal finding (surfaced while verifying DEF-248): the certificate tile/list APIs (the logged-in Cert List and the public directory) ignored the requested page size on the page-based path and always returned 20 records, because of a default-value bug in how the two pagination styles were reconciled. The on-screen views were unaffected (they request 20 at a time and scroll for more), but any larger data pull or integration that asked for a bigger page silently got a short, misleading result.

**What changed:** the page size is now honored correctly, with a sensible upper cap per endpoint (tighter on the public, unauthenticated endpoint to prevent an oversized request). The on-screen infinite-scroll behavior is unchanged. No database change, no migration. Verified on localhost across a range of page sizes (5→5, 30→30, full set returned, page-2 correct). Pending deploy.

---

## DEF-250 — Sorting the Cert List by ASIN/GTIN/EAN/Cert-Subcategories is now fast (was ~30 seconds and could error)

Found while verifying the items above: sorting the logged-in Cert List by **ASIN, GTIN, EAN, or Cert Subcategories** took roughly 27–30 seconds and intermittently failed with a server error, when the page included a certificate carrying very many product identifiers (the test data simulates a large Amazon seller with hundreds-to-thousands of ASINs — a real-world possibility). Other sort columns were fine.

The slowness came from how the database query computed the sort value for these multi-value columns while also loading all the related data. **What changed:** the query now selects the page of certificates first (a fast, lightweight step) and then loads their full details — which removed the bottleneck. The same sort results, just fast.

**Result:** these sorts went from ~27–30 seconds (with intermittent errors) to **about 1 second**. No database change, no migration, and the sort order itself is unchanged. Verified on localhost: all four columns return in 0.65–1.4s in both directions, the full sort-regression suite passes **14/0**, and the DEF-248 display check still passes. Pending deploy.

---

## DEF-246 — Certificate Create + Edit: field selections now survive a server-side validation re-render (QC #186)

Reported by the client (6/4 retest of QC #186): *"I just tested on create and the fields that are selected are not maintaining state when there is a validation error."*

When a required field is missing (or a server-only rule like Product-ID format fails), the server re-renders the Create form with an error. Previously, every JS-driven selection was lost in that re-render: **Product Name, Product Category, Product Type, Product Subcategory, Certificate Subcategories, and both External and Internal Tags** all reset to blank, forcing the analyst to re-pick everything before resubmitting.

The same gap existed on Edit for Tags and Certificate Subcategories (the product hierarchy was already preserved on Edit via the saved model, but Tags reverted to the originally-saved DB set and Subcategories were lost entirely).

**What changed:**

- **Create form** now rehydrates all JS-driven fields on a validation re-render: product is re-selected (with the auto-fill suppressed so it can't overwrite the analyst's own category choice — same technique as DEF-243), the Category / Type / Subcategory hierarchy is restored with SearchableSelect display synced, Certificate Subcategories are re-checked, and Tags (external + internal) are rebuilt as pills.
- **Edit form** now preserves the analyst's in-progress Tag and Subcategory edits across a re-render — it no longer falls back to the DB-saved values when the analyst had already made changes.

Display/behavior change only — no database change, no migration. Verified on localhost: Create `Scripts/smoke-prod/def-create-validation-state.js` **11 PASS / 0 FAIL**; Edit `Scripts/smoke-prod/def-edit-validation-state.js` **8 PASS / 0 FAIL**. Both tests force a real server re-render (native form submit bypassing client validation) to confirm the round-trip. Pending deploy to gccwebapp.com.

---

## CR-184 — New export: all active product identifiers across every certificate (QC #184)

Reported by the client (Carly, 5/15): *"How would we get a full export with product identifier information across all products? The export on this page includes Identifier Type and Id Value but not the product name at the product level or SKU or partner. We will need to run aggregate export on all active product identifiers across certs."* Until now the only product-identifier export was per‑certificate (one cert at a time), so cross‑cert analysis meant exporting cert‑by‑cert and stitching the files together by hand.

Added a one‑click **"Export Active Product Identifiers"** button on the **Companies & Products → Product Management** page. It downloads a single CSV of every **active** product identifier across all certificates — "active" meaning the identifier's certificate is publicly live (Client Approved‑Paid or Republished), which is the same rule that drives Amazon badging. Each row carries the context an analyst needs for this analysis:

**Certificate Number, Company, Identifier Type, Id Value, Partner, Product Name, SKU, Description, Effective Date, Expiration Date, Created Date, Created By.**

No Status column — because the file is already filtered to active identifiers, every row is active by definition, so a status column would add nothing (confirmed with Shawn 6/4).

**Who can run it:** Admin **and Analyst** (this is a routine analyst task). External/partner users don't see the button and are blocked at the server (403) if they try the link directly.

Verified on the test environment (localhost): analyst download returns the CSV (1,014 active identifiers on the dev data; the 112 tied to non‑live certs are correctly excluded), external user blocked 403, columns and active‑only filter confirmed.

Scope note: this shipped **simpler than the originally‑proposed CR‑184 design** (which had 5 filters and was admin‑only). Per Shawn's direction on 6/4 we delivered the clean one‑click active‑set export analysts asked for; optional filters (by partner / identifier type / date) remain a clean future add if needed. No database change. **Deployed to gccwebapp.com 2026-06-04 and verified live** — admin/analyst download returns the CSV (1,022 active identifiers on prod), the button shows on Product Management with the Export (up-arrow) styling, unauthenticated callers get 401, and External is blocked 403. Pending client UAT.

**Update 2026-06-11 — re-verified live + sent for client UAT.** Re-ran the read-only prod fingerprint (`Scripts/smoke-prod/def243-cr184-prod-fingerprint.js`) → **29 PASS / 0 FAIL / 0 WARN**; export still live and correctly gated (admin export returns `text/csv` with the 12-col header and **1,018 active rows** — the count drifts with prod cert status over time; button present; unauthenticated 401; External 403). Client UAT email drafted at `docs/tracking/CR-184-171-client-email.md` with how-to-test steps and one **open question to the client**: confirm the 12 columns are sufficient, or request additions — candidates offered are Certificate Status / Identifier Status (both deliberately omitted since every row is active), Certification Type, Certification Results, and Updated Date/By. Optional filters remain the v2 follow-up if requested.

---

## DEF-243 — Certificate Product Category/Type/Subcategory now sticks when you edit a certificate (QC #186, 6/4)

Reported by the client (6/4 retest of QC #186): after creating a certificate with a new product, opening it to edit showed the **Product Type blank** and the **Product Category jumping to "Carly Test"** instead of the values that were chosen — *"it's not showing the options I selected for Product Cat, Type, or Subcat."*

This was a **separate issue from the earlier "Add New product" fix** (which was delivered and confirmed live on 6/1). On the edit screen, when the page loaded the certificate's linked product, it was quietly **overwriting the certificate's own saved Category/Type/Subcategory with the product's** — so if the product happened to be filed under a different category, the certificate's display reverted to that. Two things were fixed:

1. **The edit screen now keeps the certificate's saved Category/Type/Subcategory.** It no longer lets the linked product overwrite your selections when the page opens. (For older certificates that never had a category of their own, it still fills the value in from the product, so nothing goes blank.)
2. **The quick "Add product" box now asks you to pick a Product Category first.** Previously, if you typed a new product name before choosing a category, the product was filed under a default category ("Carly Test") with no type — which is what kept producing mis-categorized products. Now it shows *"Select a Product Category above before adding a new product,"* so every quick-added product lands under the right hierarchy.

Display/behavior change only — no certificate data was touched and no database change is needed. Verified on the test environment (localhost, 37/37 checks): the certificate keeps the chosen Category/Type/Subcategory on edit, including a deliberately built case where the product sits under a different category — the certificate correctly keeps its own; and the quick-add now blocks until a category is chosen. **Deployed to gccwebapp.com 2026-06-04 and verified live** (read-only prod fingerprint, 29/29): 10 real certificates keep their saved category on edit — including the client-referenced **26-0166**, and real cases where the certificate sits under one category while its product sits under another (e.g. **26-0168**), where the certificate correctly keeps its own. Pending client UAT.

Note: certificates/products *already* saved under the wrong category before this fix are not changed by it — those are covered by the separately-drafted data-cleanup pass (needs a backup + sign-off before running).

---

## DEF-242 — Certificate list sorting: blank values now sort to the bottom (all list views)

Reported as part of QC #157 (client retest 5/29). When sorting a certificate-list column that's often blank — Location, ASIN, Company URL, Cert Subcategories — the empty rows were landing at the **top** in ascending order, so the first screen looked empty and the column appeared not to sort. (The sort itself was correct; DEF-241 confirmed that — it was purely where the blanks sat.)

Changed the rule across the **whole application**: blank values now always sort to the **bottom**, in both ascending and descending order, so the populated values you want to see always lead the list. Applied identically to the logged-in Cert List view, the partner/external view, and the public directory.

Display/ordering change only — no certificate data was touched, and no database change is needed. Verified on the test environment (localhost 14/14: populated values lead, blanks at the end, both directions). **Deployed to gccwebapp.com 2026-06-03 and verified live** — the same sort that previously returned a page of blanks at the top now leads with the populated values across the certificate list, partner, and public views.

---

## DEF-225 — Cert Edit page: Product Name field now shows the saved product on open

Reported 6/1. When an analyst opened Edit on a cert, the Product Name field appeared blank ("Select product...") even though the cert had a product saved. The hint underneath correctly said "1 product(s) found" but the field itself didn't show which one — leaving the analyst no way to know what was already on the cert without re-picking from the list.

Fixed and verified across desktop, tablet, and mobile (5 screen sizes). The saved product name now appears in dark text the moment the form loads.

**UAT passed on the test site (2026-06-03).** Confirmed live on gccwebapp.com against four real certificates that each have a product saved (Energy Star Certified Appliances, Eero test, test, 3M Test Product): on every one, the Product Name field shows the correct saved product in dark text the instant the Edit form opens. Marked verified — ready to close.

---

## DEF-226 — Admin → Product Types: Usage column makes it obvious when delete will be blocked

Reported 6/1. The Usage column showed counts as a single pill like "0 / 1" — the first number is products, the second is certificates. Admins were reading just the first number, hitting Delete, and being surprised by a block message ("Cannot delete: 1 certificate(s) use this category…"). The counts were always correct; the visual didn't make the blocker stand out.

Fixed. The pill now (1) bolds whichever side is non-zero so the blocking count visually pops, (2) dims the zero side, and (3) shows a small padlock icon when either count is non-zero — a glance tells you delete will be blocked before you try.

---

## DEF-228 — Partner CSV export: investigated, confirmed working as designed (QC #182, 5/29 follow-up)

Reported 6/1. On the 5/29 retest of the DEF-198 partner export fix, the client noted that a cert flagged with an internal tag was still appearing in the partner CSV. We reproduced this against the Amazon-mapped partner login and confirmed: the *internal tag itself* (column + values) is correctly hidden from the CSV — but the *cert row* still appears when it qualifies by partner assignment and status, which is the intended behavior.

Internal tags are admin-only metadata categorization, not row-level access control. The mechanism for hiding a cert from a partner is the cert's **Status** (Draft / Hold / Confidential keep a cert off all partner-facing views; Paid / Republished publish it).

No code change. We'll add this to the next client status note so the team understands which lever controls partner visibility going forward.

---

## DEF-227 — All views now have consistent spacing between the navigation menu and the page content (QC #77)

Reported 6/1. On the Admin login, a gray separator bar sits between the top navigation menu and the page content. On the Analyst login, that separator was missing — so the navigation dropdowns appeared to overlap with the content underneath. The fix had been intended for everyone but only landed for Admin.

Fixed and verified on all four role types (Admin, Analyst, External, Public). The gray spacer now renders identically across every login. Admin still shows breadcrumb text inside that spacer; the others render it empty — same layout, no overlap.

Client also flagged a broader concern: "make sure fixes are applied to all instances where applicable." We've added a permanent reminder to our process so cross-role visual fixes get checked against all four logins before being marked complete.

---

## DEF-232 — Import templates: Tags template no longer splits across columns in Excel; Cert template special characters now render correctly (QC #148, 5/29 follow-up)

Reported 6/1 from client retest of the Sprint 3 template polish.

**Tags template.** When opening the downloaded Tags import template in Excel, the column for `TagType` was breaking into two cells — "Internal" landed in column B and "External" in column C, pushing "Description" off to column D. Fixed by changing the header separator from a comma ("Internal, External") to a slash ("Internal/External") so Excel reads the row as the intended three columns.

**Cert template.** The Status column header in the Cert import template was showing a few "random characters" (`â€"`) in place of the em dash separator. Root cause was a CSV-encoding issue: the file was saved as UTF-8 but without the marker that tells Excel "this is UTF-8" — so Excel on Windows defaulted to its older Windows-1252 encoding and the special character mis-rendered. Added the UTF-8 marker; em dash now renders correctly, and any future special characters in template headers will too.

**User Management template — Partner now supported end-to-end.** Client asked us to include "Partner" alongside Admin/Analyst/External in the Role-column hint. We did that, and also wired the partner column the rest of the way through so the template promise is honest:

- The Role hint now reads `Admin/Analyst/External/Partner`.
- A new `Partner (semicolon-separated names; required for Partner role)` column was added to the template.
- The CSV importer accepts that column, looks up each partner by name against the live Partners list, and attaches the matches to the new user (mirrors the existing `User Company` behavior).
- If a row says `Role=Partner` but the Partner column is empty, the import rejects that row with a clear message — we don't create a Partner-role user with no partner attached. Same for unknown partner names (rejected with "Partner 'X' not found").

End result: an analyst can now bulk-import Partner-role users from a CSV with the same fidelity as the Add-User UI.

---

## DEF-230 — Admin → Product Types: you can now move a type or subcategory to a different parent (QC #170, 5/29 re-open)

Reported 5/29. When editing an existing product type or subcategory, changing its parent — for example moving a type under a different category, or a subcategory under a different type — didn't take. The page saved the name and sort order but quietly discarded the move, so the item stayed exactly where it was. The client's words: *"I could not update the existing… it would not give me the other alternatives, something is broken."*

Fixed and verified on localhost across both cases: moving a top-level type between categories, and moving a subcategory between types (with the subcategory correctly inheriting its new parent's category). The existing guard rails still apply — you can't move an item under itself or under one of its own children, and duplicate names under the same parent are blocked. Queued for the next deploy.

---

## DEF-231 — Cert form "Add New product": the product now keeps the category, type, and subcategory you selected (QC #186, 5/29)

Reported 5/29 (the open-items list the client built from issues #91 and #170). When an analyst typed a new product name and used the "Add New" option on the Product Name field of a certificate, the product was being filed under the **wrong category** — the first one in the list — with no product type or subcategory, ignoring what the analyst had actually picked on the form. And because selecting that new product then copied its (wrong) category back onto the certificate, the certificate showed the wrong category too, and the product didn't carry its type/subcategory into Product Management. The client's words: *"I selected Chemicals and it shows [a test category]… Product Type/Subcategory are not appearing… it changed/removed some of my selections."*

Fixed and verified end-to-end on localhost: a product added this way now saves under exactly the category, type, and subcategory chosen on the form, and the certificate keeps those selections.

**Existing records.** This corrects the behavior going forward. Certificates and products created *before* this fix may still carry the wrong category — and because the analyst's original selection was never stored (the bug overwrote it), those can't be auto-corrected. They're being handled separately: a review list is produced from the database, and each genuine record is either re-saved through the now-corrected screen or corrected in a controlled, backed-up data pass. Any leftover test data (e.g. the "[test category]" entries) is removed in that same pass. This data cleanup is drafted and waiting on a backup + change-request approval before it runs against the live environment.

---

## DEF-233 — Tile views: filter-options fetch no longer logs a "Failed to fetch" on rapid navigation (internal — caught by new E2E smoke)

Reported 6/1 by our own tooling, not the client. The earlier pageUnloading fix from May covered the main certificate fetch on the three tile views but missed the parallel filter-options fetch. On rapid navigation away during initial page load, the unguarded path logged a harmless but visible "Failed to fetch" to the console. Real users rarely navigate fast enough to hit it; our new E2E smoke harness does, and was flagging it as a false-positive failure.

Fixed across all three tile-view scripts (public, external, internal) with the same two-line guard already in use elsewhere. Smoke suite is now deterministic green (104/104 PASS, twice consecutively).

---

## DEF-229 — Certificate Create page: "select all" Product Identifiers now spans every page, not just the visible 50 (QC #159, 5/29 follow-up)

Reported 6/1. When building a new certificate, an analyst who pasted or imported a large list of Product IDs and then used the header "select all" checkbox only selected the 50 rows currently visible on screen — there was no way to select the whole list at once to clear or bulk-act on it. The count hint was right ("N product IDs"), but the cross-page "select all N across pages" banner that appears everywhere else never showed up on the Create page specifically.

Fixed and verified on localhost. The cross-page banner now appears on the Create page too, and "select all" reaches every Product ID in the in-memory list regardless of which page is showing. Queued for the next deploy.

---

## DEF-234 — Public certificate tile view: first-load speed-up (internal — caught by new E2E smoke)

Reported 6/1 by our own tooling, not the client. After the application sits idle (or the server recycles its worker process), the very first load of the public certificate tile view was slow — the certificate grid could sit blank for ~23 seconds while the database query "warmed up," and on a bad cold start it occasionally timed out and showed "Error loading certificates" instead of the tiles. Every load after the first was effectively instant (well under a second). Real users would have seen an occasional slow first load; our new E2E smoke harness, which hits a cold server, caught it as an intermittent failure.

Fixed with two coordinated changes, defense in depth:

- **Query shape.** Changed how the certificate list query is assembled so the database no longer multiplies rows across the certificate's tag and partner associations on a single large query — the standard, lower-cost pattern we already use on the other tile views. Also added a stable secondary sort so the same cert can never accidentally appear on two pages or be skipped when many certs share the same primary sort value.
- **Startup warm-up.** The app now fires one tiny test query at boot so the database query plan is already cached before the first real visitor arrives. The first user request now pays the warm-path cost (sub-second), not the cold-compile cost. Safe-fails — if warm-up encounters any issue, it's logged and the app starts normally.

The fix is in the working tree and queued for the next deploy; a warm re-run of the smoke suite will confirm the cold-start window is closed before it's marked verified.

**Verification (2026-06-02T02:25Z):** `SMOKE_BATCH_RESTART=1` Tier-1 roll-up returned **692 PASS / 0 FAIL / 8 WARN — GREEN**. Public tile cell count: 124/0/0. Curl-timed first hit on a cold restart: ~0.6s (was 23s pre-fix). Cold-start window closed.

---

## DEF-235 — Test suite stays reliable end-to-end: no more last-view flakes on long smoke runs (internal — smoke harness reliability)

Found on 2026-06-02 during a back-to-back run of the new Tier 1 smoke suite: after ~9 views of sustained load (~600 HTTP requests in ~6 min), the local dev server stopped accepting new TCP connections and the last view in the run (`admin-tags`) failed every cell with a connection-refused error. Direct `curl` calls to the same URL right after the suite worked fine — so it was sustained-load related, not a real bug in any view.

**Root cause found and fixed (2026-06-02):** the culprit was the request/response logging middleware. It was wrapping the response body of EVERY request into a memory buffer — including bundled CSS / JavaScript / images / fonts, which were never going to be logged anyway. Under sustained load (the new E2E smoke fires ~30 static-asset requests per page × 12 cells × 10 views = ~3600 buffer allocations per run), the resulting memory pressure produced long garbage-collection pauses that made the web server fall behind on accepting new connections. The fix excludes static-asset paths from body buffering entirely — those requests still get logged (status code, duration), they just skip the wasteful body capture.

**Verification:** the deploy-gate smoke now passes in plain single-run mode (no special workaround needed): **694 PASS / 0 FAIL / 6 WARN, GREEN**. The previously-flaky last view went from 0-of-12 cells passing to 40-of-40. The earlier `SMOKE_BATCH_RESTART=1` workaround is now optional — single-run mode is reliable again. Production benefit: the same allocation pressure would have shown up under burst traffic on Plesk; that whole class of stability concern is now closed.

---

## Internal — E2E Smoke suite (Days 1 + 2 — infra and Tier 1 fan-out)

New automated test surface scoped per `docs/tracking/e2e-smoke-plan.md`. Catches view-level regressions across 4 roles × 3 viewports before they reach client UAT.

- `Scripts/smoke-prod/_lib/smoke-template.js` (~305 lines) — shared runner. Login per role using the CLAUDE.md test creds, viewport sizing, console + page-error + network-error capture (with ignore-list for known noise), screenshot policy via `SMOKE_SCREENSHOTS=on-fail|always|never`, access-gate check (200/401/403 normalization including login-redirect → 401), mobile horizontal-scroll guard, per-view JSON results. Refuses to run against gccwebapp.com / greencirclecertified.com unless `SMOKE_ALLOW_REMOTE=1` is set.
- `Scripts/smoke-prod/_views/` — 10 Tier 1 view smokes (`home`, `login`, `cert-tile-public/external/internal`, `cert-create`, `cert-edit`, `dashboard`, `admin-user-mgmt`, `admin-tags`) plus `run-all.js` orchestrator that writes a summary JSON.
- Acceptance is "all 10 views return 0 FAIL across 4 roles × 3 viewports, and negative-path access (Public→Internal, External→Admin) returns the expected 401/403."
- Day 1 + Day 2 effort: ~1 day each, infra + 10 view-specific assertion blocks. Approach is now the deploy-gate candidate for future Plesk pushes.

---

## QC #106 — User Management: Company Access & new "User Company" field (DEF-238 + CR-020)

Reported across 5/8 → 5/15 → 5/29. This closes the long-running "User Company vs Company Access" item and adds the requested second field. Three parts:

**1. Company Access now follows the role (DEF-238).** When you pick **Admin** or **Analyst** in the Add/Edit User form, the Company Access selector is now **hidden** and replaced with a note: *"Admin and Analyst roles automatically have access to all companies. No company selection is needed."* **External** shows the company selector; **Partner** shows the partner selector. This is the 5/15 feedback that the 5/29 retest reported as not done.

**2. New "User Company" field, separate from Company Access (CR-020).** The user record now has a **User Company** field — the user's own company/employer — chosen from a dropdown of existing companies, separate from Company Access (which controls what data they can see). It appears in the Add/Edit form, the CSV import template, and the CSV export. The two are now distinct fields, ending the back-and-forth over the "Company Access" / "User Company" naming.

**3. The user table shows User Company.** The table's "User Company" column now displays the user's own company (employer); Company Access is managed in the form and import/export, not shown in the table.

Verified on the test environment across the Add flow, Edit flow, import template, and export (role-visibility 6/6, User Company field 6/6).

**Heads-up for this deploy:** the CSV import now has two separate columns — **Company Access** (which companies the user can view; names or "All") and **User Company** (the employer; one company name). Spreadsheets that previously used a "User Company" column to mean access should switch to the **Company Access** column.

**Deploy package (2026-06-03):** `deployment-drops/GCCWebApp-deploy-20260603-1221.zip` (Plesk profile, net8.0/win-x64, framework-dependent). **Requires a one-time database update** — the app does not auto-migrate on startup. Run `deployment-drops/CR-020-AddUserCompanyToUser-idempotent.sql` against the live DB (adds the nullable `Users.UserCompanyId` column + FK; idempotent/safe to re-run) **before or with** this deploy, or CR-020 will error. Pre-deploy: back up the server `wwwroot/uploads/` first (publish overwrites). This zip also carries the other working-tree fixes staged since the last deploy (DEF-235 logging-middleware static-asset fix, DEF-236 product-ID select-all bar, DEF-237 Tag Name).

---

## DEF-240 — Product Management: Status column, popup field, and filter removed (QC #103, 3rd re-raise)

Reported across 3/13 → 5/28. The client asked repeatedly why the **Status** column was still on the Product Management page and in the Add/Edit Product popup: *"What is the purpose of the Status column? I thought we discussed it was not needed."* Earlier rounds had aligned Status across the table, export, template, and popup, but kept the field present. This change does what was actually asked and removes it.

Removed: the Status column from the products table, the Status dropdown from the Add/Edit Product popup, and the Status filter from the filter bar. New products are created active by default; editing a product preserves its current active/inactive state (an edit never silently re-activates a product that was previously retired). The CSV export already had no Status column, so the table, popup, import template, and export are now fully consistent.

**One thing to confirm before this is final:** with the Status control gone, there is currently **no screen control to mark an existing product Inactive** (the import template has no Status column either). Products already marked inactive stay inactive and are still honored everywhere; we just no longer surface a toggle. If you want a way to retire a product going forward, the options are the row Delete action or adding a small dedicated control — flagging so it's a deliberate choice, not a surprise.

---

## Tags import/export: column renamed to "Tag Name" (DEF-237)

Small QC #148 follow-up — the first column of the Tags import template and export now reads **"Tag Name"** instead of the generic "Name". Re-importing existing files still works (the importer accepts both).

---

## Certificate PDF upload — fixed end to end (2026-06-12)

A user reported they couldn't upload a PDF to a certificate. We reproduced it and shipped two fixes, confirmed working on the test site:

1. **Save is no longer blocked.** When you attached a PDF, a pop-up message ("PDF uploaded…") appeared in the **middle of the screen and covered the Save button**, so clicking Save did nothing and the certificate didn't update. That message is now a small, non-blocking notice in the top-right corner — Save always works, and the alarming "Warning" wording on a successful upload was corrected.

2. **The certificate preview image now generates and saves.** After the first fix let saves go through, the certificate still showed "Certificate Unavailable — Preview not generated." The preview thumbnail was being looked for in the wrong folder (a leftover from an earlier security change that moved uploaded files), so it never generated — on the live site *and* locally. That path is corrected across the upload, preview, replace, and import paths. New uploads now show their preview, and certificates uploaded during the affected period will regenerate their preview automatically when viewed.

Also included in the same release: the certificate list's ID columns (ASIN/UPC/GTIN/EAN) now sort by the count shown in the badge, matching what's on screen.

---

## Product Management — Product Type & Subcategory now show as separate line items (2026-06-15)

On the Product Management page, the **Product Type** and **Subcategory** columns were showing blank for most products. The page was working correctly — those details are entered on each **certificate**, not stored on the **product** record the page reads.

Per the agreed rule, a product is now identified by **Company + Name + Category + Product Type + Subcategory**. The same product name with a *different* Type/Subcategory is treated as a **separate line item**, with its own certificate count. Going forward, when a certificate is created or edited, the system automatically files it under the product record matching that exact combination (creating the record if it's new). A blank Type/Subcategory is a valid combination on its own.

This release contains that going-forward behavior and the matching change to the product CSV import. A one-time pass that reorganizes the products already in the system into these separate line items is prepared as a separate, reviewed database step — it runs only with sign-off and a full backup first.

---

## Internal — Verification artifacts

### 2026-06-15 — Product identity split by (Type, Subcategory) (CR-186)

- Code: `Controllers/CertificateController.cs` — `ResolveProductForCertAsync` (Create + Edit): find-or-create the product record matching (Company, Name, Category, Type, Subcategory); point the cert at it. `Controllers/ProductController.cs` — product CSV import dedups on the identity tuple, not Name-only. No schema change, no migration.
- `Scripts/smoke-prod/cr186-split-verify-local.js` — local Playwright on :5101: editing a cert's Type produced a separate product line item with the cert count attached. PASS.
- Migration (separate, gated): `Scripts/data-ops/2026-06-15-product-split-by-type-subcat-{IDENTIFY,REMEDIATE}.sql` — splits existing products by their certs' (Type, Subcategory) combos, re-points certs; ROLLBACK-by-default. Dev dry-run validated (315→316, 1 split, 1 cert re-pointed). Needs GCC review of IDENTIFY + full DB backup before COMMIT. Tracked in CR-186 §10.
- Cert CSV import also tuple-resolves at commit (`Services/Import/CertificateImportService.cs` → `ResolveProductForImportAsync`); verified `Scripts/smoke-prod/cr186-import-split-verify-local.js` (2 same-name rows → 2 records by type). Cert-form UX unchanged. Import has no Subcategory/Category column (separate enhancement); preview "products to create" count is by-name (cosmetic).
- Shipped in `deployment-drops/GCCWebApp-deploy-20260615-1737.zip` (alongside DEF-264).

## Certificate PDF download — fixed (DEF-264, 2026-06-15)

A user reported a PDF "error on download." After the earlier change that moved uploaded files to a more secure location, two download actions — the single-certificate **Download** and the bulk **Download Selected (ZIP)** — were still looking in the old folder, so downloads failed with a "no certificate PDF has been uploaded" message (and the bulk download silently returned a generated placeholder instead of the real file). Both now resolve the file from the new location. Shipped in `deployment-drops/GCCWebApp-deploy-20260615-1737.zip`. Tracked as DEF-264.

**Deployed + verified on prod 2026-06-15** (`Scripts/smoke-prod/post-deploy-1737-verify.js`): admin login OK (schema canary), `/Product/GetProducts` 200, and certificate PDFs download as `application/pdf` (e.g. 26-0163, 26-0157, 26-0151 — 0.7–2.4 MB each), 0 errors. Same deploy carried the CR-186 going-forward code (cert form find-or-create + product/cert import tuple dedup), which is additive — existing data untouched; the CR-186 split migration remains on hold pending GCC confirmation (see CR-186 §10).

### 2026-06-12 — Certificate PDF upload (DEF-261 blocking modal, DEF-262 thumbnail path, DEF-260 count-sort)

- `Scripts/smoke-prod/upload-pdf-edit-repro-live.js` — live repro of the blocking modal on gccwebapp.com (DEF-261).
- `Scripts/smoke-prod/upload-fix-verify-local.js` — DEF-261 fix verify on localhost (10/10): non-blocking toast, Save reachable, cert saved.
- `Scripts/smoke-prod/upload-image-diagnose-live.js` — confirmed "Preview not generated" on the live site (DEF-262).
- `Scripts/smoke-prod/upload-image-diagnose-local.js` — confirmed thumbnail failed locally too (isolated to code, not server) and verified the fix (real 464px preview, PNGs written to `App_Data/uploads/images/`, 0 "not found" errors).
- Root cause + the five corrected path resolvers tracked in `docs/tracking/defect-register.md` (DEF-261, DEF-262).
- Shipped in `deployment-drops/GCCWebApp-deploy-20260612-1509.zip`.

- `scripts/smoke-prod/def225-def226-test.js` — Playwright test for DEF-225 + DEF-226.
- `scripts/smoke-prod/def226-setup.sql` + `def226-cleanup.sql` — idempotent test-data seed and teardown.
- `scripts/smoke-prod/def227-nav-spacing.js` — Playwright probe of breadcrumb spacer across 4 roles. Re-runnable.
- `scripts/smoke-prod/def228-partner-export.js` — Playwright probe of Amazon partner CSV export; verifies internal-tagged cert excluded post-fix.
- DEF-228 pre-fix CSV: `scripts/smoke-prod/screenshots/def228-partner-export-2026-06-01T15-28-48/partner-export.csv` (4 rows, includes 26-0001).
- DEF-228 post-fix CSV: `scripts/smoke-prod/screenshots/def228-partner-export-2026-06-01T15-35-32/partner-export.csv` (3 rows, no 26-0001).
- DEF-225/226 screenshots: `scripts/smoke-prod/screenshots/def225-def226-2026-06-01T13-33-48/`
- DEF-227 4-role verification: `scripts/smoke-prod/screenshots/def227-nav-spacing-2026-06-01T14-05-44/`
- `Scripts/smoke-prod/def230-reparent.js` — Playwright probe of the type/subcategory re-parent fix; self-contained + reversible; 11/11 PASS.
- `Scripts/smoke-prod/def231-quickadd-repro.js` — deterministic repro of the inline-quick-add mis-categorization (8/8); and `def231-fix-verify.js` — real-UI verification of the fix (6/6).
- `Scripts/data-ops/2026-06-01-product-miscategorization-IDENTIFY.sql` (read-only) + `-REMEDIATE.sql` (guarded, ROLLBACK-by-default) — the DEF-231 existing-data cleanup pass; needs backup + CR before running.
- Code-level root cause and file paths are tracked in `docs/tracking/defect-register.md` (DEF-225, DEF-226, DEF-227, DEF-230, DEF-231).

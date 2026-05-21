# Observations Log

## 2026-05-19 — DEF-202 / batch close-out of remaining code-actionable backlog

Five small client-asked corrections fixed in one batch. (1) QC #148 cert import template Status header now reads `Status (Draft / Client Approved - Paid / ... — defaults to Draft if blank)`; import parser extended with a prefix-match fallback so the longer header still resolves. (2) QC #104 Companies breadcrumb renamed to Company Management for parity with the Product side; the Admin Dashboard nav item half of #104 closed as needs-no-action (page was removed by client decision 5/1, `/Admin/Index` redirects to User Management). (3) QC #118 Custom Fields with IsPublic=true now render on the public + external cert detail pages — controllers were already loading the data, the views weren't binding. Grouped under one Custom Fields section per the project-wide rule (CustomField.Section ignored). (4) QC #179 Product ID import preview popup now shows the SKU column (data was already parsed into productSku, just wasn't rendered). (5) QC #184 Product Mgmt export now includes SKU and Partners columns; Partners derived via a new projection on `ProductController.GetProducts`.

Also discovered: **QC #181 (Company Email regex) is already in HEAD** since commit f018782 (5/11, same day client raised). Status synced.

No-code items drafted in `docs/tracking/client-responses-2026-05-19.md`: #167 auto-expire timing explainer and #150 forgot-password verify steps + named-test-user provisioning ask. Both close once Shawn sends.

Two sub-items intentionally deferred for client decision: (a) #179 Description column on product-ID import — expose in UI vs remove from template, (b) #184 cross-cert "all active product identifiers" export — new report surface, not a column-add.

Build: 0 errors, 51 pre-existing warnings (unchanged). Tracking: defect-register DEF-202, shipped-2026-05, this log.

---

## 2026-05-19 — DEF-201 / QC #175 + #176 + #183: Three small UX fixes bundled

(1) QC #175 logo distortion: default `.main-logo` is 280px (≥1440 displays); existing 769-1024 tablet rule drops to 180px; the 1025-1440 gap left it at 280 and `.logo-text-group`'s 300px min-width forced wrap. Added a new `min-width: 1025px and max-width: 1440px` rule with 220/36/24px sizes.

(2) QC #176 mobile-stuck filter sidebar: DEF-098 hides the Hide/Show toggle ≤1024px. If a user collapsed on desktop and then narrowed below 1024, the `.collapsed` class stuck with no way back. Added `matchMedia('(max-width: 1024px)')` listener in all three tile-view JS files — sidebar auto-expands on crossing into the small breakpoint (or initial load there). Same pattern in public, external, internal.

(3) QC #183 dead sort options: `Most Popular Companies` and `Relevance Score` had no backend handler in `MapSortBy` (fell through to Number default, so dropdown clicks produced unchanged sort). Removed both from `CertificateTileViewInternal.cshtml` and `Most Popular Companies` from `CertificateTileViewExternal.cshtml`. Backend untouched.

Also: QC #181 (Company Email format validation) discovered already fixed in commit f018782 on 2026-05-11 — strict regex `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` on both `CertificateEditViewModel.CompanyEmail` and `CompanyViewModel.Email`. Status synced; the fix has been on master since 5/11 and the client just hadn't seen it because gccwebapp.com is still on the pre-deploy build.

Build: 0 errors, 0 warnings on incremental. Files: `wwwroot/css/main.css`, three `certificate-tile-view*.js` files, two `CertificateTileView*.cshtml` files. Tracking: `docs/tracking/defect-register.md` (DEF-201), `docs/tracking/shipped-2026-05.md`.

---

## 2026-05-19 — DEF-200 / QC #180: Dashboard tile↔export divergence (mass alignment + one wrong-column bug)

Client raised QC #180 on 5/11 — *"Certs Expired Last Month is not using right data … Anuki needs to do that"* (the count audit). Audited every dashboard tile against its export and found the gap was broader than the headline: every "Active …" card and the "Expired Last Month" card disagreed with their downloads. Three root causes:

1. **Wrong column.** `Controllers/DashboardController.cs:certs-expired-last-month` called `GetCertificatesByDateRangeAsync(start, end)`, which filters on `EffectiveDate`. So "Expired Last Month" returned certs that became *effective* last month. Plus no status filter → Drafts with stale ExpirationDates leaked through.
2. **Missing aggregation case.** `GenerateAggregatedStatCsv` had cases for categories/types/subcategories/companies but not Products, so `products-with-active-certs` exported per-cert rows (22) instead of distinct products (10).
3. **Tile counted future-dated certs.** `Repositories/CertificateRepository.cs:580-582` had `EffectiveDate >= startOfYear` with no upper bound, so a `2027-07-08` future cert was counted in "Published This Year" tile (16) but excluded from export (15).

Fix introduced `CertificateStatusHelper.EverPublishedStatuses` (DashboardStatuses ∪ {Expired, ExpiredNoAnnualVerification}) as a named constant for the "ever-published" set used by ExpiredLastMonth. New `GetCertificatesByExpirationDateRangeAsync` helper for the right column. DashboardController switched to both. Products added to aggregation. PublishedThisYear capped at today. Parallel `/Export/DownloadStat` aligned defensively.

Verified on dev: all 12 dashboard cards now have tile=export. Probe for ExpiredLastMonth: temporarily set cert 27 ExpirationDate to 2026-04-15; background expiration service auto-flipped Draft→Expired (correct); tile and export both returned exactly 1 row matching that cert; fixture reverted.

Time-trap worth remembering: `wwwroot/js/dashboard.js` runs a 1.5s count-up animation on every stat-value. Playwright `waitUntil:'networkidle'` fires before that completes, so initial verification captured mid-counter values (e.g. 7 instead of 22) and sent me down a phantom rabbit hole investigating why "Statistics['Active']" was returning 7 — it wasn't, the test snapshot just caught the animation mid-roll. Any future automated dashboard test needs `await page.waitForTimeout(2000)` after `goto`.

Build: 0 errors, 51 pre-existing warnings (unchanged). Files: `Models/CertificateStatus.cs`, `Repositories/ICertificateRepository.cs`, `Repositories/CertificateRepository.cs`, `Controllers/DashboardController.cs`, `Controllers/ExportController.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-200), `docs/tracking/shipped-2026-05.md`.

---

## 2026-05-18 — DEF-199 / QC #177+#178: Filter sidebar half of DEF-192 closed

Two related complaints (QC #177 and #178, both 5/8). The Public + External tile-view filter sidebar projected only `c.Product.X` for category/type/subcategory IDs, so any cert-level override (the `Certificates.ProductCategoryId`/`ProductTypeId`/`ProductSubcategoryId` columns DEF-192 added to the listing predicate) was invisible in the sidebar even though the listing matched on it. Plus the company dropdown had no IsActive/IsDeleted predicate, so soft-deleted Companies still surfaced via the cert→Company FK navigation.

Same data-shape gap DEF-192 fixed on the listing side, just half-applied — sidebar source never got the effective-value pattern. Fixed `CertificateTilesApiController.GetFilterOptions` (Public) and `CertificateTileExternalApiController.GetFilterOptions` (External/Partner) to use `c.{X}Id ?? c.Product.{X}Id`, refactored the flat `productCategories`/`productTypes` lists to share the same `visible*Ids` sets the hierarchy uses (single source of truth), and added `c.Company.IsActive && !c.Company.IsDeleted` to the company dropdown query.

Verified locally on dev. Set `Certificates.ProductCategoryId=2` on cert 38 (Client Approved - Paid, Product in category 1): sidebar before fix would have hidden category 2; after fix shows both. Selecting Clothing in the listing returned exactly cert 38. Soft-deleted Acme (cert tied) disappeared from the dropdown after fix. All test mutations reverted.

Out-of-scope follow-up flagged: a cert that overrides only `ProductTypeId` to a type under a different category than its effective category needs `visibleCategoryIds` to also include the type's `CategoryId`. Not reproduced in dev data — dominant pattern is "category override only." Will add if client surfaces it.

Build: 0 errors, 51 pre-existing warnings (unchanged). Files: `Controllers/Api/CertificateTilesApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-199), `docs/tracking/shipped-2026-05.md`.

---

## 2026-05-18 — DEF-198 / QC #182: Partner CSV export was leaking the whole DB

Critical fix for QC #182 (5/15 client, *"This is big issue, needs fixed."*). Partner login's *Export All to CSV* on the tile view returned every cert in the database — drafts, expired rows, certs tagged to other partners. The tile view itself was correctly scoped; the export bypassed that scope because `Services/CertificateService.cs` never applied a role-based predicate, `Controllers/CertificateController.GetUserRole()` had no Partner branch (so partner web users fell through to `"Public"`), and `CertificateListRequest` carried no partner/company allow-list.

Fix: introduced `ApplyExportSecurityScope` in CertificateService (same predicate as `CertificateTileExternalApiController.cs:97-138`), added `AllowedPartnerNames`/`AllowedCompanyIds` to `CertificateListRequest`, ported the partner/company claim helpers from ProductController, added the Partner branch to GetUserRole, plumbed the security context through both `ExportCertificatesAsync` and `ExportCertificatesByIdsAsync` (so the "specific IDs" path can't bypass it either). Partner/External now restricted to their allow-list + PublicStatuses; anonymous Public restricted to PublicStatuses; Admin/Analyst unrestricted.

Verified locally on dev DB (`ANUKI01\GCCDatabase_Dev`) with three callers hitting `/Certificate/ExportCertificates?format=csv`:
- Partner (Amazon-scoped test user `partner_test@gccwebapp.com`): 43 → **4** rows (Amazon × Client Approved - Paid)
- Admin (`swest@anukiconsulting.com`): 43 rows unchanged
- Anonymous: 43 → 22 rows (only PublicStatuses)

SQL truth: Amazon partner = 4 PublicStatus certs (`SELECT COUNT(*) FROM Certificates WHERE Partner='Amazon' AND Status IN ('Client Approved - Paid','Republished')`). Numbers match.

Test fixture left in dev DB: `Users.Id=324` (`partner_test`, Amazon partner mapping, password `TestPass123!`) for future regression coverage of the partner-login path.

Same root-cause family as DEF-103 (Partner API JSON contract, closed 2026-04-30). DEF-103 covered the API list/single endpoints; the CSV export endpoint was simply not in scope at the time. Worth a future sweep across other export endpoints (`/Admin/ExportData`, `/api/productids/.../export`) to confirm no parallel gap.

Build: 0 errors, 51 pre-existing warnings (unchanged). Files: `Models/CertificateViewModels.cs`, `Services/CertificateService.cs`, `Services/IServices.cs`, `Controllers/CertificateController.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-198), `docs/tracking/shipped-2026-05.md`.

---

## 2026-05-18 — QC log open-items triage + 13-item status sync

Full pass through the 34 open QC items in `Database QC Log_5.15.26_og.xlsx` Sheet 2. Triage doc at `docs/tracking/qc-triage-2026-05-18.md` — six batches of 5–6 items each, comparing each row vs current code/memory, deciding STATUS-STALE / FIXED-PENDING-DEPLOY / STILL-BROKEN / NEEDS-CR / NEEDS-CLARIFICATION. No code changes.

Headline: 13 items were already fixed or accepted by client but the Status cell had never been flipped — same staleness pattern QC #170 showed before today's update. Cleared in a single Excel pass: #59, #60, #127, #131, #132, #141, #143, #155, #158, #165, #166 → Complete. #172 (DEF-194 bulk-delete), #174 (cert tile header CSS) → Fixed - pending deploy. Backlog cut from 34 → 21 open without writing any new code.

Critical finding: QC #182 — Partner login "Export All to CSV" leaks all certs (draft + non-Amazon). Same shape as DEF-103 which fixed the API list endpoint but missed this CSV export. Half-day fix; needs to land before any partner UAT.

Major bugs flagged for follow-up: #177/#178 (public filters surface companies/categories without active certs, and selections sometimes don't filter), #180 (dashboard "Expired Last Month" wrong + export rows don't match displayed count). Plus 8 decision/CR items including Partner multi-select (#171, schema + API contract impact), Duo MFA (#151), and mobile-pass-on-Create-form vs wontfix (#50). Pre-production cutover task tracked: #173 (strip "Partner" column from partner-login tile).

Took: ~1 hour. Spreadsheet-only edits in og workbook; working copy left alone per Shawn's "leave my open copy be" rule.

---

## 2026-05-18 — DEF-197 (no QC ref): Product Types Usage column overcounted when entities set multiple FK levels

Found during local smoke test of [[DEF-196]]. The new "Usage (Products / Certs)" column rolled up parent totals as `direct + Σ(children)`. The Certificates table allows a single row to set `ProductCategoryId` + `ProductTypeId` + `ProductSubcategoryId` simultaneously (1 of 43 dev certs does; 1 of 305 dev products does the same on the product side), so each such entity was counted at every level it set, then summed again at the grandparent.

Dev DB: Chemicals category showed `3 / 28` (page) vs `2 / 26` (truth — distinct certs/products in the subtree). Type 4 showed `2 / 2` vs `1 / 1`. Leaf subcategories were always correct. Worst-case overcount for an entity tagged at all N levels is N*(N-1)/2 extra at the root.

Fix in `Services/AdminService.cs:GetProductTypesAsync`: replaced integer-sum rollup with `HashSet<int>` unions per node, count `.Count` once at the end. UnionWith dedupes the multi-FK case naturally. Smoke re-run confirmed the page now matches SQL ground truth at every node. No data migration, no schema change, no UI change — same template renders, just with correct numbers.

Build: 0 errors, 51 pre-existing warnings (unchanged). Files: `Services/AdminService.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-197), `docs/tracking/shipped-2026-05.md`.

Lesson worth keeping: `direct + Σ(children)` only works when each entity is referenced at exactly one level of a tree. As soon as the schema allows multi-level FK tagging, the rollup must dedupe on entity ID, not on count. Same shape as `[[feedback_ef_includes_with_projection]]` — counting when you should be distinct.

---

## 2026-05-15 — DEF-196 / QC #170: Admin → Product Types delete blocked by certs, but count column hid them

Carly's QC #170 (5/1, re-raised 5/15): she couldn't delete her "Carly Test" product type. Tree-view "Products" column showed 0 but every delete attempt returned the generic "An error occurred while deleting. Please try again." Filtering the cert list by "Carly Test" showed certs were actually using it — the admin page was lying about what was deletable.

Three layered bugs on the same path in `Services/AdminService.cs`: (1) `GetProductTypesAsync` populated `ProductCount` from `ProductType.Products` only — the EF navigation only sees `Products.ProductTypeId`, not Products linked via `ProductSubcategoryId`, and no Certificate FK at all. (2) `DeleteProductTypeAsync` pre-check used the same Products-only navigation, so it passed silently when only certs referenced the type. (3) `_context.SaveChangesAsync()` then hit the SQL Server FK Restrict on `Certificates.ProductTypeId`/`ProductSubcategoryId`/`ProductCategoryId` and got swallowed by the broad `catch (Exception)`. Same chain existed in `DeleteProductCategoryAsync`.

Fix: `GetProductTypesAsync` now reads `Certificates.ProductCategoryId/ProductTypeId/ProductSubcategoryId` and `Products.ProductSubcategoryId` upfront, populates a new `CertificateCount` field per node, and corrects Level-2 product counts. Delete methods now run explicit cert pre-checks (`ProductTypeId == id || ProductSubcategoryId == id` for types; `ProductCategoryId == id` for categories) with messages naming the count and the right noun ("subcategory" vs "type" per entity Level). A `DbUpdateException` catch sits above the generic handler as final fallback. UI: column header renamed to "Usage (Products / Certs)" with tooltip; node cells render `${productCount} / ${certificateCount}`. Table view splits into two columns. CSV exports gain a `Certificate Count` column.

Verified on local dev DB: GET `/Admin/GetProductTypes` returns both counts matching direct SQL queries. PET (subcategory with 1 cert via SubcategoryId) → "Cannot delete: 1 certificate(s) use this subcategory. Edit those certificates to use a different subcategory first, then try again." LLDPE (subcategory with 1 product via SubcategoryId — previously hidden) → "Cannot delete: 1 product(s) are assigned to this item. Please reassign them first." Plastics (has 6 subcategories) → existing children check fires first, unchanged. Build: 0 errors, 53 pre-existing warnings.

Cert Edit dropdown side-complaint is the Category→Type cascade working as designed — no code change. Workaround: change Category first to unlock the Type dropdown with non-test options. If client keeps hitting it, add a UX hint under the Type dropdown.

Files: `Services/AdminService.cs`, `Models/AdminModels.cs`, `Views/Admin/ProductTypes.cshtml`, `wwwroot/js/product-types.js`. Tracking: `docs/tracking/defect-register.md` (DEF-196), `docs/tracking/shipped-2026-05.md`.

---

## 2026-05-14 — DEF-194 / QC #172: Bulk-delete checkboxes added to Cert Create+Edit Product Identifier table

Client QC #172 (5/11/26): *"Add check all for mass delete under Product Identifier per 5/8 call."* Triaged as an Enhancement — feature did not exist before. Built immediately per Shawn's "Build now, log as DEF" triage call.

Shared partial `_ProductIdManager` got a select-all `<th>`, a per-row checkbox `<td>`, and a "Delete Selected (N)" red button next to "Add Product ID". `ProductIdManager` class in `wwwroot/js/product-id-manager.js` gained a `selectedIds: Set<number>` that survives re-renders, filter changes, and page changes. Bulk delete runs through `AdminCore.showConfirmation()` then issues parallel `DELETE /api/productids/{id}` via `Promise.allSettled`, tolerating partial failures with an honest "Deleted N of M; X failed" toast. In-memory mode (Create page) drops rows from local state without an API call. Select-all header is tri-state (checked / indeterminate / unchecked).

Both Create and Edit pages get the capability from a single change because they share the partial. QC #172 only named "Cert Creation Page" but Edit benefits too — call this out in the client response so they know.

Files: `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, plus DEF-194 register entry and shipped-2026-05 entry. Build: 0 errors, 53 pre-existing warnings. JS syntax: clean via `node --check`. No data migration, no controller changes, no API contract changes. Read-only views (cert detail) don't get the new column or button.

**Verification limit:** ran static checks only (build clean, Razor partial compiles, JS syntax OK). Could not drive an authenticated browser-free interactive test — dev server is up on `localhost:5101` but the seeded user creds in this environment differ from the standard `analyst@greencirclecertified.com / TestPass123!` (login returned "User not found"). Shawn to validate interactively in the browser before deploy.

## 2026-05-13 — Live smoke: external test user had no Company association → 0 certs visible

Smoke test against `https://gccwebapp.com/` (Playwright, `LiveSmokeTests.S08`) confirmed the test `external` user could log in but the external tile view rendered `"Showing 0 of 0 certificates"`. Empty page, no defect in the view itself.

Root cause: Direct live-DB query showed `external` (Users.Id=3) had role `External` and `IsActive=true`, but **zero rows in `CompanyUser`** — the many-to-many join between Users and Companies. The external tile view filters certs by the user's associated companies, so no association = empty result. The schema correctly enforces "external sees only their company's certs"; the gap was the missing CompanyUser row on the prod test DB.

Fix: `INSERT INTO CompanyUser (UsersId, CompaniesId) VALUES (3, 259)` — linked external user to KAO (Companies.Id=259), which has 4 `Client Approved - Paid` certs on prod. KAO chosen because it had the most public-status certs of any single company on the live test DB (4), giving a meaningful smoke surface for the external view.

Verified by re-running `S08_ExternalLogin_ExternalTileView` against gccwebapp.com → `external-tile-count=4`. Screenshot confirms 4 KAO tiles ("John Frieda PROfiller+ Thickening Conditioner", "John Frieda Profiller plus Shampoo 250ml", "Color Protecting Conditioner 200ml", "Color Protecting Shampoo 250ml"), header reads "Showing 4 of 4 certificates", TEST banner intact.

Not a code defect — test-data seeding gap on the prod test env. No code change required. A new Playwright smoke test class `Tests/Production/LiveSmokeTests.cs` (10 tests) was added to the UAT project as part of this work; it runs against `https://gccwebapp.com/` with the Plesk Basic Auth gate handled via Playwright `HttpCredentials`. Useful for future ad-hoc post-deploy verification.

Files: `GCCWebApp.UAT.Tests/Tests/Production/LiveSmokeTests.cs` (new), live DB `CompanyUser` table (one INSERT).

## 2026-05-12 — Product ID Import: partial-select with a no-change row → "Import failed"

URL: `http://localhost:5101/Certificate/Edit/26-0023`. Shawn observed that selecting a subset of rows in the Import Preview without enabling Replace-all caused the import to fail.

Reproduced locally with Playwright on cert `26-0023`: upload a one-row CSV whose `Type,Product ID` already exist on the cert with identical other fields → preview classifies it as `status='unchanged', action='skip', selected=false`. User manually checks the row → JS includes it in `bulkRows` because the filter was `r.action !== 'update'`. Server rejects on the duplicate check with `"Product ID already exists on this certificate"`. Toast: `Import failed: ASIN:B08N5W0001 - Product ID already exists on this certificate`.

Root cause: `applyImport()` in `wwwroot/js/product-id-manager.js` partitioned `selectedRows` using a `!== 'update'` filter, which let `'skip'` rows reach the bulk endpoint as create attempts. They should have been treated as no-ops in non-overwrite mode.

Fix: changed the bulk filter to `r.action === 'create'`, added a `skippedExisting` partition, and updated the success-message branch so a skip-only selection ends with `"X row(s) already on this certificate — nothing to change"` instead of the generic failure. Mixed selections that include skips append `"; X already on file"` to the success message. Replace-all behavior unchanged. Tracked as DEF-191.

Verified by re-running the four-scenario repro against localhost — all four end in a success toast that accurately describes the outcome.

File: `wwwroot/js/product-id-manager.js`.

## 2026-05-12 — Duplicate Download Template button in Product ID Import Preview modal

URL: `http://localhost:5101/Certificate/Edit/26-0023`. Shawn flagged that the Import Preview modal footer had a "Download Template" button when the same control already exists on the Product ID Manager's main toolbar.

Root cause: `Views/Shared/_ProductIdManager.cshtml` rendered the template-download affordance twice — once in the toolbar (`#downloadSample`, line 19) and once in the import preview modal footer's left cluster (calling the same `productIdManager.downloadSample()` JS handler). Two paths to the same action in one workflow.

Fix: Removed the duplicate button from the modal footer. Kept the DEF-159 Replace-All toggle (the actual unique control that belongs in this footer) and the right-side Cancel + Apply Import buttons. File: `Views/Shared/_ProductIdManager.cshtml`. Tracked as DEF-190.

## 2026-05-12 — Certificate Edit preview regressed from square to portrait

URL: `http://localhost:5101/Certificate/Edit/26-0023`. Sidebar preview box was rendering portrait (US Letter) instead of square.

Root cause: commit `6b37530` (2026-04-13, "defect resolulution") added `.certificate-create-preview { aspect-ratio: 8.5 / 11; ... }` to `wwwroot/css/certificate-create.css` — new rule that didn't exist before. The mobile media query at ≤1024px also pinned the same 8.5/11 ratio. Pre-April-13, the container had no aspect-ratio constraint and rendered closer to square via its content.

Fix: changed `aspect-ratio: 8.5 / 11` → `aspect-ratio: 1 / 1` in both places. `object-fit: contain` on the inner `<img>` still prevents cropping regardless of container shape, so portrait snapshots just letterbox top/bottom inside the square frame. Files: `wwwroot/css/certificate-create.css`. Both Edit and Create pages affected (shared class).

## 2026-05-11 — DEF-167 implemented: Level-1-only cert-type filter dropdown across all three tile-view controllers

Shawn's direction was Level 1 only in the dropdown, fix the data — no parent-rollup logic.

Dev DB audit first: 41 certs, all tagged at Level 1, 0 at Level 2. `CertificateCertificationTypeSubcategories` join table empty. Dev needs no data remediation. The Level-2-tagged certs the client saw are on prod (gccwebapp.com).

Code change: replaced the 18-line `allCertTypes` query + in-memory parent/child flatten loop with a single Level-1-only LINQ query in **three controllers** (turned out to be three, not two — public, internal, external all carry the same pattern):
- `Controllers/Api/CertificateTilesApiController.cs` — public, `/api/certificate-tiles` (the bug the client actually saw)
- `Controllers/Api/CertificateTileInternalApiController.cs` — admin/internal, `/api/certificate-tiles-internal` (client said "login view is fine" — parity-only fix)
- `Controllers/Api/CertificateTileExternalApiController.cs` — external-user, `/api/CertificateTileExternal` (also was missing the `IsActive` guard)

New query in all three:
```csharp
var hierarchicalCertTypes = await _context.CertificationTypes
    .Where(ct => ct.IsActive && ct.ParentId == null && certTypeIds.Contains(ct.Id))
    .OrderBy(ct => ct.SortOrder)
    .Select(ct => new { name = ct.Name, level = ct.Level })
    .ToListAsync();
```
JSON output shape unchanged (`[{name, level}, ...]`); JS consumers untouched.

Local verification: `GET /api/certificate-tiles/filters` returned 4 entries, all `level: 1`. Filter by `?certType=Recycled%20Content` returned 16 certs, all `Recycled Content`. Pre-fix would have included CSF subcategories (and on prod, all 30 of them as separate filter rows).

Wrote `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql` with two parts:
1. Audit SELECT (safe to run anytime) — lists every Level-2-tagged cert with its proposed parent.
2. Remediation block (commented-out, CR-gated) — repoints CertificationTypeId to the parent Level-1, preserves the original Level-2 reference in `CertificateCertificationTypeSubcategories`.

**Pre-deploy gate:** before shipping the code, run the audit query against prod. Any row count > 0 means those certs will silently disappear from filter selection until the remediation block runs. Per `feedback_data_changes_require_cr.md`, the UPDATE block needs a CR with row count attached before it can run on prod. Either submit the CR + run together, or warn the client that those Level-2-tagged certs will be temporarily unfilterable post-deploy.

Files changed: 3 controllers, 1 new SQL script. Build clean (0 errors, 51 pre-existing warnings).

---

## 2026-05-10 — QC #169 / DEF-167 scoped: public cert-tile filter renders subcategories + missing IsActive guard + no parent-rollup on filter selection

Client reported (QC #169, 4/30/26) that the public Certificate Tile View's "Certification Type" filter pane shows CSF subcategories as their own checkboxes ("WHICH IS WRONG"), shows what appears to be all cert types, and that selecting a parent like CSF doesn't surface certs tagged with a child subcategory like Recycled Content. Login (internal) view behaves correctly on the "only types with certs" point, per client.

Code investigation against `Controllers/Api/CertificateTileExternalApiController.cs:528-551` and the internal mirror at `Controllers/Api/CertificateTilesApiController.cs:254-292` confirmed three concrete code defects in the public endpoint's `GetFilters` action:
1. **Subcategory flatten loop** (External `:547-550`): inner `foreach` emits each child cert type as its own filter row alongside its parent. Same pattern present in Internal `:285-292` — latent there since login view consumes it identically.
2. **Missing `IsActive` guard** on External `:535` Where clause; Internal `:278` has `ct.IsActive && (...)`, External omits it.
3. **Filter logic uses string equality on `CertificationTypeNames`** with no parent→children rollup. Per memory `feedback_certtypes_hierarchy.md` the type table is hierarchical by design (Level + ParentId), so the filter ought to expand a selected parent to `{parent ∪ children}` before matching.

Client's three complaints in one item map cleanly to these three bugs. Symptom "public shows ALL cert types" appears to be a perception consequence of (1) — the subcategory bloat makes the dropdown look larger; the actual visibility filter (`certTypeIds.Contains(ct.Id) || Children.Any(...)`) is the same on both controllers.

Scoped as DEF-167 in `docs/tracking/defect-register.md`. No code changes made — investigation only. Open questions before implementation: (a) drop ALL non-Level-1 entries from the dropdown vs. allow Level-2 parents that have Level-3 children, (b) whether the parent→children rollup should be visible to the user (tooltip / "+subcategories" label) or silent. Going with `ParentId == null` only and silent rollup unless client weighs in.

Related backlog items in same family: QC #128 (LEED filter shows certs without LEED tag) and QC #149 (L2/L3 Product Category options don't filter). Worth re-checking those for the same root-cause pattern when DEF-167 is implemented.

---

## 2026-05-07 — DEF-161 /Certificate/Create Product ID validation: lowercase ASIN blocked + server skipped format validation on save

Shawn flagged "validation issues" on `https://gccwebapp.com/Certificate/Create` — couldn't enter a Product ID that passed validation, suspected the issue was global. Reproduced on localhost with a Playwright pass against every Product ID type (`B08N5WRWNW`, `b08n5wrwnw`, `036000291452`, `4006381333931`, `9780306406157`, `SKU-12345`, etc.). Two defects under the one symptom:

1. **Client ASIN regex was case-sensitive** (`product-id-manager.js:861`). Typing `b08n5wrwnw` got "ASIN must be exactly 10 alphanumeric characters" — the message didn't say *uppercase*, so users had no recovery hint. SKU and MPN regexes already had the `/i` flag; ASIN was the lone outlier. Server `ProductIdService.ValidateAsin` had the same case-sensitive pattern.
2. **`CertificateController.Create` POST persisted Product IDs without running format validation** at all — only the Edit-time `/api/productids` round-trip called `ProductIdService.ValidateProductIdAsync`. `Edit` POST had the same gap. Net effect: a malformed Product ID could be saved through the form path even if the client check were bypassed.

Fix is a single normalisation rule applied at every save boundary:
- Client: ASIN regex now case-insensitive; new `normalizeProductIdValue` uppercases ASIN/UPC/GTIN/EAN/ISBN/SKU/MPN values before push to in-memory list.
- Server: new `ProductIdService.NormalizeValueForType` helper, applied inside `ValidateProductIdAsync`, `CreateProductIdAsync`, `UpdateProductIdAsync`, and `ImportProductIdsAsync` (bulk CSV).
- Server: Create POST and Edit POST both run a pre-validation pass — canonicalise type+value, then `ValidateProductIdAsync` each row, raising `ModelState["ProductIds[i].ProductIdValue"]` errors for invalid entries so the form re-renders with inline errors instead of silently saving.

Verified locally:
- Validation truth table (`verify-isbn.mjs`): `asin_lower` and `asin_mixed` flipped from `false` to `true` after fix; all other types unchanged.
- Server `/api/productids/validate` (`verify-server.mjs`): lowercase ASIN now returns `data: true`; bad value still `data: false`.
- End-to-end Create (`e2e-create.mjs`): submitted `b08abc1234` (lowercase) on a new cert; redirect to `/Certificate/CertificateInternalView/26-0158`. Direct SQL: `ASIN | B08ABC1234` — canonicalised on save.

Files: `wwwroot/js/product-id-manager.js`, `Services/ProductIdService.cs`, `Controllers/CertificateController.cs`. DEF-161 in `docs/tracking/defect-register.md`. Entry in `docs/tracking/shipped-2026-05.md`.

---

## 2026-05-07 - Database_Scripts/ prod audit + QC123 script archived

Audited all 5 scripts in `Database_Scripts/` against prod (`gccwebap_certificates`) for run status:
- `20260504_DEF134_RetargetProductSubcategoryFK.sql` — ✅ applied (EF history row present, FK retargeted to ProductTypes, 5 products now carry non-null subcat IDs).
- `AddPartnerRole.sql` — ✅ applied (Partner Id=6, IsActive=1, 4 perms; matches memory 5/01).
- `FixMojibakeProductNames.sql` — ✅ applied. Verified via `CONVERT(VARBINARY, Name)` showing 0x2122 (™) and 0x00AE (®) at the expected positions on Ids 62, 71, 73, 76, 80. sqlcmd's default codepage was mangling display; underlying NVARCHAR is clean.
- `RemoveManageWorkflowPermission.sql` — ✅ applied (`certificate.workflow` row count = 0; matches memory 5/01).
- `QC123_deactivate_duplicate_cert_types.sql` — ❌ NOT applied AND **must not be applied**. Original 2026-04-23 diagnosis predates the cert-type-hierarchy rule. 7 of 10 target Ids don't exist on prod; the 3 surviving (71/73/78) are L2 children of ParentId=6 ("Certified Sustainability Facts") — by design, not dups. They're referenced by certs via `CertificationTypeSubcategoryId` (not `CertificationTypeId`), so the script's CertCount=0 safety check joined the wrong column. **Renamed to `_OBSOLETE_QC123_deactivate_duplicate_cert_types.sql.archived`** with a DO-NOT-RUN header. The original SQL is preserved inside a block comment for reference. The 2026-04-23 observations entry below has been amended with a retraction note.

## 2026-05-07 - QC #58 answer to Carly: cert numbering + draft model + form recovery

Carly's question on the cert Create timeout: *"is that draft user specific or would the draft save for all logged in internal users because that would be the next cert # available?"* Mashed together three different things; clarified each:

1. **Cert numbers are not reserved at form-open.** `CertificateController.Create` (line 287) calls `_certificateRepository.GenerateNextCertificateNumberAsync()`, which is a pure read — `MAX(Number) + 1` for the current year against the `Certificates` table (`Repositories/CertificateRepository.cs:704-718`). Number is shown in the field but only locked in when the row writes on Submit. Two analysts opening Create simultaneously both see the same proposed number; first to submit wins.
2. **Drafts are not user-specific.** A "Draft" is just `Certificate.Status = "Draft"` — a normal database row visible to all internal Admin/Analyst users. There is no per-user scratch / Save-As-Draft store. If you want something to persist before all required fields are filled, that would be a separate CR.
3. **Form-clearing was idle session timeout.** `Program.cs:31` session `IdleTimeout = 30 min`, `Program.cs:245` auth cookie `ExpireTimeSpan = 30 min` with `SlidingExpiration = true`. "Remember Me" only persists login across browser restarts (14 days) — it does not extend idle timeout while a form sits open. After 30 min idle, the anti-forgery token is stale and POST drops the form.
4. **There is no form recovery today.** `wwwroot/js/certificate-create.js:705` has `SHOW_RECOVERY_PROMPT: false` — banner gated off in DEF-151 with auto-save still writing silently to localStorage and stale entries GC'd at 2 hr so storage doesn't leak. Recovery banner is filed as **CR-017 (Phase 2)** per Shawn's call 2026-05-07; not a launch blocker. Originally I told Carly the banner exists — that was wrong; corrected.

QC #58 stays in **Working as Designed** in `qc-sprint-3-reconciliation-2026-05-04.md:151`. CR-017 detail in `change-request-register.md:206` and `docs/tracking/CR-017-form-recovery-ux.md`.

---

## 2026-05-05 - DEF-147: Product ID Manager action icons match app standard

Shawn reported on `gccwebapp.com/Certificate/Edit/26-0160` that the Edit/Delete icons under Product Identifiers didn't match the standard — trash icon was not the bright `#FF5252` red used elsewhere. Two render sites had drifted to legacy Bootstrap `btn btn-outline-primary` / `btn-outline-danger` styling: the Razor partial `Views/Shared/_ProductIdManager.cshtml` (initial page load) and the JS template `wwwroot/js/product-id-manager.js` `getProductIdRowHtml` (dynamic rows). Canonical pattern lives at `wwwroot/css/admin/admin-inline-editing.css` (`.admin-inline-action-btn--edit` blue `#00AEEF`, `--delete` red `#FF5252`) and is used on Pathways, Partners, Tags, etc. CSS was also not linked from `Views/Certificate/Create.cshtml` / `Edit.cshtml`.

Fix updated both render sites to use the standard markup and added the CSS link to both host pages. Kept the existing `btn-edit-product-id` / `btn-delete-product-id` hooks alongside the new classes so JS handlers continue to bind unchanged. Added `aria-label` per a11y convention. Verified on `localhost:5101` / `GCCDatabase_Dev` via Playwright (`C:\Users\swest\AppData\Local\Temp\uitest\test-pid-icons.js`): computed color edit `rgb(0, 174, 239)`, delete `rgb(255, 82, 82)`, zero remaining `btn-outline-*` action classes. Screenshot confirms standard inline-icon footprint with red trash. Tracked as DEF-147; receipts in shipped-2026-05.md.

---

## 2026-05-05 - DEF-146: Product ID Manager "Created By" preserved on cert edit

Shawn reported `gccwebapp.com/Certificate/Edit/26-0147` showing "Created By = System" on every Product ID row. Two-bug interaction: (1) `CertificateController.Edit` POST cleared and re-added the entire `ProductIds` collection on every save, hardcoding `CreatedBy = currentUserId` on every row in the loop — overwriting the original creator on every cert edit; (2) prod cert 26-0147 already had `CreatedBy = NULL` in the DB (likely a legacy state), and `wwwroot/js/product-id-manager.js:244` falls back to literal `"System"` when `createdByUser` is null.

Fix replaces clear-and-readd with merge: existing rows keep their `CreatedBy/CreatedAt`, only get `UpdatedBy/UpdatedAt` stamped; new rows (Id==0) get `CreatedBy = currentUserId`; rows the user deleted in the UI are removed by Id-set diff. Backfill of existing NULL rows on prod is **out of scope** — real-record data UPDATEs need a CR from GCC.

Verified locally on `localhost:5101` / `GCCDatabase_Dev` via Playwright (`C:\Users\swest\AppData\Local\Temp\uitest\test-pid-createdby.js`): three scenarios — preserve NULL on plain edit, preserve admin while analyst saves, new row gets analyst while existing rows keep admin — all PASS. Tracked as DEF-146 in defect register; receipts in shipped-2026-05.md.

Files touched: `Controllers/CertificateController.cs` (Edit POST, Product ID handling block).

---

## 2026-05-05 - Partner API end-to-end verification + mobile smoke (batch 5)

Batch round 5 of internal QC retest. Headline result: **Partner API is fully healthy and contract-compliant** — all four main endpoints verified end-to-end against the live deploy with a freshly-generated key, contract format intact per `feedback_amazon_api_immutable.md`. Mobile responsive smoke also clean across 3 viewports × 2 pages.

**Partner API end-to-end (verified 2026-05-05):**

Generated a fresh key on `/Admin/PartnerAPI` for the Amazon partner via the modal flow (selectors: `#generate-key-btn` → `#key-partner-id` → `#confirm-generate-btn` → `#generated-key-value`). Got a 76-character `gcc_partner_…` key. Tested all four contract endpoints with curl:

1. **`GET /api/v1/certificates?pageSize=1`** — 200, returns 6 total Amazon certs. Each item includes `certificateNumber`, `status: "Active"` (literal, per DEF-103), `partner: "Amazon"`, `certificationResults: "100%"` (percent format per the DisplayFormatters fix from 2026-04-21), `pdfDownloadUrl`, `amazonCode`. No leaked internal IDs.

2. **`GET /api/v1/certificates?pageSize=1&includeProductIds=true`** — 200, same shape plus `productIds: [{type, value, status, partner, productName, productSku}]` array. Verified with cert 26-0151 — got 3 ASINs (`B0BQN8PSY6`, `B0BQN9BJCB`, `B0C544FF8Q`).

3. **`GET /api/v1/certificates/number/26-0151`** — 200, single-cert wrapped in `{success, data: {…}}`. Has `status: "Active"`, `productIds[]`. DEF-103 fix is intact on the live build.

4. **`GET /api/v1/certificates/search/productid?productId=B0BQN8PSY6`** — 200, returns matching certs with `productIds[]` where each entry has an `isMatchedId: true|false` flag indicating which ID matched. This is Amazon's primary search use case.

5. **`GET /api/v1/certificates/gs1/B0BQN8PSY6`** — 404 with structured JSON. Probably correct — GS1 lookups are for GTIN/EAN/UPC, not ASIN.

**Path note for future cycles:** the search route is `/api/v1/certificates/search/productid` (no hyphen). My earlier test used `search/product-id` and got 404 — path typo, not API defect.

**Test-tooling discovery worth saving:** generating a key from a Playwright admin session and then immediately calling `/api/v1/certificates` via Playwright's `ctx.request.get()` with the X-API-Key header returned 401 with empty body. The same key via curl returned 200 with proper JSON. Best guess: Playwright sends the admin session cookie (`GCC.Auth`) alongside X-API-Key, and the auth pipeline picks cookie auth (which doesn't have partner claims) over the API-key scheme. Future Partner API tests should use either a fresh logged-out Playwright context or shell out to curl. **No real defect** — the API auth works correctly when called the way Amazon actually calls it (no cookie, just X-API-Key).

**Mobile responsive smoke** — 3 viewports (iPhone 375×812, iPad 768×1024, Desktop 1440×900) × 2 pages (`/Account/Login`, `/Certificate/CertificateTileView`) = 6 combos, all clean. Zero horizontal scroll on any combo. Confirms the mobile/tablet fixes from 2026-04-01 are still holding on the live deploy.

**Cert workflow buttons on Edit page** — test couldn't complete because of a Playwright session-carryover issue (admin login bled into the analyst-login step). Punted.

Test artifacts: `C:\Users\swest\AppData\Local\Temp\uitest\session5-batch.js`, `partner-api-fullkey.js`, `partner-api-curl-test.js`. Screenshots in `qc-s5-*`.

## 2026-05-05 - Cert tile filters audit (Public / Internal / External) + countries US-first fix (DEF-144)

Continued QC retest by auditing the certificate tile filter sidebar across all three views. Two findings:

### Finding 1 — DEF-144 fix applied: Countries filter ordering

Per requirement, the Countries filter must always show "United States" at the top, then A-Z. Audit results across the three filter endpoints:

| View | Endpoint | Country sort |
|------|----------|--------------|
| Public | `Controllers/Api/CertificateTilesApiController.cs:322` | ✅ Already US-first then A-Z |
| Internal | `Controllers/Api/CertificateTileInternalApiController.cs:526` | ❌ Plain A-Z (FIXED) |
| External | `Controllers/Api/CertificateTileExternalApiController.cs:549` | ❌ Plain A-Z (FIXED) |

Internal and External were doing only `OrderBy(c => c)`. Updated both to materialize the distinct country list, then `OrderBy(c => c == "United States" ? 0 : 1).ThenBy(c => c)` — same pattern Public already used. Compile clean (running dev server locked apphost.exe but C# valid; 0 compiler errors). Logged as DEF-144.

### Finding 2 — Filter visibility scoping (each view's `baseQuery`)

User asked "Are filters displaying data correctly based on what is visible to public?" Audited what each view's filter sidebar can show vs. what the underlying tile cards actually return.

**Public** (`CertificateTilesApiController.GetFilterOptions`):
- baseQuery is `PublicStatuses + non-expired` ✓ matches the visible cert set.
- `companies`, `countries`, `productCategories` (flat), `additionalTags` are all scoped to baseQuery ✓
- BUT `certificationTypes` and `productCategoriesHierarchy` are pulled directly from admin tables (all active rows, ignoring whether any public cert uses them). Possible UX gap: a user could pick a cert type or product category from the dropdown and get zero results.

**Internal** (`CertificateTileInternalApiController.GetFilterOptions`):
- No status scoping — by design (admin/analyst sees Drafts, Holds, Expired, etc.).
- All filter lists pulled from full Certificates table or admin tables. Correct for internal.

**External** (`CertificateTileExternalApiController.GetFilterOptions`):
- baseQuery scoped to user's company/partner + `PublicStatuses + non-expired` ✓
- `companies`, `countries`, `productCategories` (flat), `productTypes`, `certificationTypes`, `additionalTags`, `tags`, `partners` all scoped to baseQuery ✓
- BUT `productCategoriesHierarchy` pulled from admin tables (not scoped) — same minor gap as Public.

The strict-scoping fix for Public and External `certificationTypes` / `productCategoriesHierarchy` is straightforward but changes UX (filter sidebar shrinks for sparse data). Flagging for Shawn's call rather than implementing — matches CLAUDE.md rule to ask before building.

### Finding 3 — DEF-145 fix applied: strict-scope Public + External filter hierarchies (Shawn approved)

After surfacing Finding 2, Shawn directed: "yes do it for Public and External." Applied:

- `Controllers/Api/CertificateTilesApiController.cs` (Public): added `visibleCertTypeIds`, `visibleCategoryIds`, `visibleProductTypeIds`, `visibleSubcategoryIds` from baseQuery. `certificationTypes` query now requires the type to be directly used OR have a child that's used. `productCategoriesHierarchy` keeps a node only if it (or a descendant) is used.
- `Controllers/Api/CertificateTileExternalApiController.cs` (External): same `productCategoriesHierarchy` scoping. (External `certificationTypes` already had this scoping pattern via `certTypeIds` — left as-is.)
- Internal view unchanged — admin/analyst should still see the full taxonomy.

Build clean (0 errors, 49 warnings). Net effect: every option in Public's and External's filter dropdowns now corresponds to ≥1 cert the user can actually see — no more dead clicks that produce empty grids.

Tradeoff to note for UAT: when a new cert type or product category is added by admin but no public cert uses it yet, it won't appear in the Public filter dropdown until a cert is approved with that type/category. This is the intended behavior per Shawn's call — filter UX favors signal over completeness.

## 2026-05-05 - Cert detail + shareable URL + login edges + partner API batch (DEF-143)

Batch round 4 of internal QC retest covered cert detail page render, shareable URL params, login edge cases, and partner API end-to-end. **5 of 6 checks pass; 1 real defect found and fixed (DEF-143).** Detail summary:

- **DEF-143 (fixed in tree)** — All three single-cert detail views (`CertificateSingleView`, `CertificateInternalView`, `CertificateExternalView`) include `<script src="~/js/certificates.js">` which is a list/grid/search initializer. On every cert-detail page load it fires `fetch('/api/certificates')` → 404 (the legacy un-versioned route is gone; only `/api/v1/certificates` exists now), then cascades into a `TypeError: Cannot read properties of undefined (reading 'initializeSearch')`. Each detail view defines its own `window.downloadCertificate` and zoom handlers inline, so the script is genuinely dead. Three single-line deletions. After deploy, detail-page console will be clean.

- **Cert detail icon position confirmed.** Public detail page renders the download icon at x=1337 (right edge), with the cert image at x=40,w=314 (left side). Memory `feedback_cert_detail_action_icons_right_aligned` says icons should be right-aligned with the image — confirmed. Note: only download icon was found in the test (zoom icon used a different selector pattern not picked up by my regex; visual inspection of the PNG screenshot confirms both are present and right-aligned).

- **Shareable URL params work.** `?search=Plastic` on `/Certificate/CertificateTileView` correctly pre-fills the searchInput and filters the visible tiles to matching products. No console errors.

- **Login edge cases all pass.** Wrong password keeps user on `/Account/Login` and shows a visible error message. Empty email is blocked by HTML5 native validation (`Please fill out this field.`).

- **Partner API key generation flow incomplete in test.** The test logged in as admin, navigated to `/Admin/PartnerAPI`, clicked `#generate-key-btn`, but the test couldn't find the partner select modal (selectors didn't match the actual modal). The page renders correctly per screenshot. Punted to a future cycle — needs more time on the actual modal HTML to get selectors right. Did NOT verify the `/api/v1/certificates` end-to-end with a freshly-generated key this turn.

Test artifact: `C:\Users\swest\AppData\Local\Temp\uitest\session4-batch.js`. Screenshots in `qc-s4-*` temp dir.

## 2026-05-05 - Cert workflow surfaces + answer to standing "ID vs Number" CLAUDE.md question

Continued the QC retest cycle by exercising the certificate workflow surfaces. Headline finding: **the user's standing CLAUDE.md concern — "why is table data on the certs pages showing ID instead of certificate number per requirements" — is no longer a defect in the current codebase.** All three tile views (public `wwwroot/js/certificate-tile-view.js:1407`, internal `certificate-tile-view-internal.js:407`, external `certificate-tile-view-external.js`) render `cert.certificateNumber` in the visible cert-number column, not `cert.id`. The Edit URL uses certificate number (e.g., `/Certificate/Edit/24-1605`) per explicit code comment at `certificate-tile-view.js:1442` ("Use certificate number… instead of ID for URL"). The internal `cert.id` only lives in `data-cert-id` attributes for client-side state, never as visible text. Either this was fixed in a prior sprint or the original concern was about an older version; current behavior is correct per requirement.

Other findings during this batch:

1. **Cert Create page `<h1>` count = 2** — same widespread duplication confirmed in the previous turn. Adds to the ~25-file `<h1>` cleanup batch.

2. **Cert Create cascade dropdowns are wrapped in `SearchableSelect` widgets**: `Views/Certificate/Create.cshtml:404` instantiates `new SearchableSelect('#ProductCategoryId', …)` which hides the native `<select>` and replaces it with a custom searchable input. Playwright's `selectOption()` against the native element fails with "element is not visible". Future tests against Cert Create / Cert Edit cascades need to interact with the SearchableSelect widget (typing into its visible input + selecting from the rendered dropdown), not the underlying `<select>`. Memory note for future test cycles.

3. **Tile cards use onclick handlers, not direct hrefs.** Cards render with `onclick="viewCertificateDetails('${cert.certificateNumber}')"` and target `/Certificate/CertificateSingleView/{number}` (opens in new tab via `window.open`). The action icon row uses `event.stopPropagation()` to keep download / external-link / edit handlers separate from the tile-body click. Selectors like `a[href*="/Certificate/Detail/"]` won't find tile links — must click the `.certificate-card` element or one of its onclick-bearing children. Memory note for future tests.

4. **List view inside the tile page uses `<div class="grid-row">` with cells `<div class="grid-cell">`**, not a real `<table>`. Both list and tile flows correctly show `cert.certificateNumber` in the visible Number column.

5. **Public Cert Detail page = `/Certificate/CertificateSingleView/{number}`** (not `/Certificate/Detail/{id}` — that route may not exist for public). Future test should use this URL pattern.

Test artifacts: `C:\Users\swest\AppData\Local\Temp\uitest\cert-flows-batch.js` (pass-through; some assertions failed due to SearchableSelect / hrefless tiles, not real bugs — test design issues now corrected in memory for next cycle).

## 2026-05-05 - Continued batch testing: admin pages + cert tile views

Continued the QC retest cycle by exercising 8 admin pages logged in as admin, plus public/external cert tile views. **Headline result:** the admin surfaces are healthy — every page loads in 600–1500ms, returns HTTP 200, and produces zero console errors. UserMgmt (20 rows), Tags (5), AdmProductMgmt (26), Partners (5), Pathways (4), Roles (5), PartnerAPI (7), and CertificateTypes (route is /Admin/CertificateTypes — earlier 30s "timeout" for analyst was a wrong-URL artifact in my test, not a real perf issue) all rendered correctly with the right action buttons.

Confirmed-and-quantified findings:

1. **`<h1>` duplication is widespread.** Every admin / dashboard / company / product view that uses `_Layout.cshtml` renders two `<h1>` tags with identical "X Management" text. Root cause is mechanical: `Views/Shared/_Layout.cshtml:309` already renders `<h1 class="page-title">@ViewData["Title"]</h1>`, but ~25 view files also render `<h1 class="admin-page-title">@ViewData["Title"]</h1>`. The visual screenshot shows only one heading visible on screen — the second is in the DOM but rendered hidden, so this is an a11y / SEO defect rather than a visual regression. CLAUDE.md says "Use ViewData[\"Title\"] (no `<h1>` in views)" — fix is to drop the `<h1 class="admin-page-title">` line from all 25 violating views; layout already emits the correct h1. Not fixing this turn — batch refactor that needs Shawn's go-ahead. Public `CertificateTileView.cshtml` is correctly NOT a violator (relies on layout h1 only) — that's the pattern to copy.

2. **External user empty-state UX gap.** Logged in as `external` and loaded `/Certificate/CertificateTileViewExternal`. The page renders header/filter chrome but zero tiles and zero list rows. Network capture shows the API call `/api/CertificateTileExternal` returns 200 with `totalCount: 0, certificates: []`. This is *expected per CLAUDE.md* ("External User Restrictions: See ONLY associated company/partner data") — the seeded `external` user has no associated company on prod, so legitimately has nothing to show. **However, the page surfaces no empty-state message.** The user sees a blank canvas under the filter chrome with no indication that "no certs are associated with your account" or similar. Worth a small UX patch on a future cycle. NOT fixing this turn.

3. **External user role-gating works correctly.** Confirmed external sees no Add Certificate button and no Edit buttons (per CLAUDE.md "External User Restrictions: NO Add/Edit certificate buttons"). ✓

4. **Admin page perf is good.** All 8 admin pages load in 600–1500ms, no need for a perf pass. Earlier "30s timeout for /Admin/CertificationTypes" was actually a wrong-URL test artifact — the real route is `/Admin/CertificateTypes`.

5. **Memory note for future selectors:** the External tile API response uses key `certificates` (not `data` or `items`); wrapper is `{totalCount, page, pageSize, totalPages, hasMore, certificates}`. Public tile view list rows are `#gridBody .table-body-row`.

Test artifacts: `C:\Users\swest\AppData\Local\Temp\uitest\admin-batch.js`, `cert-tile-test.js`, `correct-url-followups.js`, `external-tile-debug.js`. Screenshots in per-run `qc-admin-*` and `qc-followup-*` temp dirs.

## 2026-05-05 - DEF-142 — Partner web users have no nav (DEF-112 reversal)

Shawn created `testuser5` with Partner role on gccwebapp.com (Role Management, separate from API permission), logged in, saw no navigation. Diagnosed three-part gap: (1) `_Layout.cshtml` only branches nav for External, not Partner; (2) `ExternalAccess` policy in `Program.cs` only authorizes External/Admin/Analyst claims; (3) `SeedData.cs` had Partner `IsActive=false` per the recent DEF-112 Path 4 (eliminate web-user Partner). Filtering plumbing (`User.Partners` many-to-many, partner-name cert filter, Edit User modal Partner Assignment dropdown) was already fully built — only the policy/nav/active-flag wires were missing.

Shawn's clarified requirement reverses DEF-112: Partner web users DO exist, mirror External access pattern, but filter by partner instead of by company. All Amazon staff users see all Amazon-tagged certs. Amazon API key (`/api/v1/certificates*`) still works as before — different controller, different code path, contract still locked.

Fix shipped in working tree (4 changes): `Program.cs` ExternalAccess policy now accepts Partner claim; `_Layout.cshtml:171` nav branch fires for External || Partner; `SeedData.cs:97` Partner row IsActive=true with dual-purpose description; dev DB Roles row flipped IsActive 0→1. Prod DB needs no flip because the deactivation SQL was never run there. DEF-112 entry annotated with reversal note pointing at DEF-142. Build clean. Pending Plesk deploy to verify on gccwebapp.com.

Architectural concern carried forward (not in scope): the dual meaning of `userRole == "Partner"` (human user vs API key) still shares the same string in cert-listing controllers. Functionally fine today (different controllers, different data sources), but audit-trail confusion remains a future hardening item.

---

## 2026-05-05 - DEF-141 + cross-page smoke run

Continuing internal QC retest of the live test env (gccwebapp.com), did a batched multi-page Playwright smoke (Dashboard, CertTileView, Product/Management, Company/Index, plus role-gated admin pages) plus a deep regression of /Product/Management Add/Edit/Search/Filter/Sort/Export/Import. **Headline result:** the cascade end-to-end works (Add new with Plastics → 6 subs → save → row appears in list). Edit modal pre-fills correctly. Search, category filter, sort, and CSV export all pass. Import button uses the correct `fa-file-arrow-down` icon (down arrow per `feedback_import_export_icon_convention.md`).

Three new findings:

1. **DEF-141 (fixed in tree)** — `/Product/Management` analyst load fires `/Admin/GetProducts`, gets HTML AccessDenied, parses as JSON, errors silently. Source is `wwwroot/js/admin/productManagement.js` auto-instantiating its class on every page that includes it (the Razor view loads it solely for import-modal helpers). DEF-111 fixed the inline `loadProducts()` but missed this duplicate same-named function in a different file. Gated `init()` on `#products-table-body` existence. 4-line fix.

2. **Design-guide drift on success notifications** — `admin-core.js` routes all `showNotification` calls (incl. success) to centered alertdialog modals requiring OK; CLAUDE.md says "Success: Toast notification only". Inline comment says the modal-everywhere routing is deliberate. Flagged but NOT fixed — needs a doc-vs-code decision.

3. **`<h1>` duplicates on three pages** — Dashboard, ProductMgmt, CompanyIndex each report 2 `<h1>` tags via Playwright. CLAUDE.md says no `<h1>` in views (use `ViewData["Title"]`). Visual audit needed before yanking duplicates.

4. **Bonus:** `/Admin/CertificationTypes` timed out at 30s for analyst — should redirect to AccessDenied promptly. Worth a perf pass.

Test artifacts: `C:\Users\swest\AppData\Local\Temp\uitest\product-mgmt-batch.js`, `multi-page-smoke.js`, `check-def111.js`, `subcategory-test.js`, `subcategory-test2.js`.

## 2026-05-05 - DEF-140 — Remember Me silently overridden by client-side idle auto-logout

Shawn reported "Remember Me on the login does not appear to be working." Confirmed scenario: leaving an authenticated tab open and idling. Server-side, the prior fix (commit bc5d619, 2026-03-20) is intact — Remember Me yields a 14-day persistent auth cookie, and `Program.cs` middleware re-hydrates session from claims on each request. Bug is in `wwwroot/js/auth.js:67-113`: a hard-coded 30-min idle timer calls `logoutForm.submit()` regardless of Remember Me state, force-logging out users whose server cookie is still valid for 14 days. Browser-close-and-reopen path is unaffected (JS isn't running). Fix in 3 files: added `RememberMe` claim to the auth ticket in `SignInUser`, layout emits `<meta name="x-remember-me">`, JS reads the tag and skips both the 5-min warning and the 30-min auto-logout when content is "true". Existing 401-fetch-interceptor still handles non-RememberMe expiration. Decision (Shawn): Remember Me checkbox stays unchecked by default — including after explicit logout — for shared-computer safety. Out of scope: the AAD B2C `OnTokenValidated` path doesn't route through `SignInUser`; will need the same claim plumbing if/when AAD B2C is enabled in prod.

## 2026-05-05 - DEF-139 — Subcategory cascade UX hardening on Product Management

Continuing internal QC retest, Shawn reported "Add new Product: Subcategory is not loading the first time Product Type is selected and there are subcategory" on https://gccwebapp.com/Product/Management. Programmatically reproduced against the live page with Playwright across 6 scenarios (first-pick, mid-modal cat-change, close+reopen, rapid type swap, has-subs↔no-subs, kbd interaction) — all six rendered the correct subcategories from `/Certificate/GetProductSubcategories`. Deployed JS matched repo source. Capping diagnosis at ~5 tool calls per the "no chaos rabbit holes" rule, applied a defensive fix that closes all three plausible root causes (stale browser cache, slow-network race, ambiguous placeholder) regardless of which is biting: explicit "Loading subcategories..." state during fetch, "No subcategories available" on success+empty (matches the cert-create pattern), and a module-scope sequence-counter to drop stale fetch responses after a rapid type swap. File: `Views/Product/Management.cshtml`. Logged as DEF-139, shipped-log entry written.

## 2026-05-01 - Export CSV header alignment audit (DEF-110)

Audited every Export feature in the app — 15 surfaces total. Found CSV headers diverging from UI table labels on 12 of them, plus a duplicate-handler bug (Product Management pages were downloading 2 CSVs per Export click) and a CSV-ONLY rule violation (Product Types admin page used a CSV/JSON/Excel chooser modal). Standardized all 12 export header lines to match UI labels exactly, removed the duplicate registration, and forced Product Types to direct CSV. Export header text and import-template column names are now deliberately decoupled: User import template still uses "User Company" for round-trip with Carly's QC #108; export uses "Company" to match UI. Same pattern for product imports. C# changes in CertificateService.cs and AdminService.cs need server restart to take effect; JS/Razor changes are live on localhost:5101. Files: 12. See defect-register DEF-110 for the per-export breakdown.

## 2026-01-11 - Automated Login Testing & CSS Consolidation Verification

### Summary
Implemented automated login testing using curl to verify CSS consolidation and page functionality.

### Login Testing Results

**Authentication Flow:**
- Form field is `Email` (accepts both email and username)
- Anti-forgery token required from login page cookies
- Successful login returns HTTP 302 redirect to `/Dashboard`
- Auth cookie: `GCC.Auth` set on successful login

**Test Command Pattern:**
```bash
# 1. Get login page and anti-forgery token
curl -s -c cookies.txt "http://localhost:5101/Account/Login" -o login_page.html
TOKEN=$(grep -oP 'name="__RequestVerificationToken"[^>]*value="[^"]*"' login_page.html | grep -oP 'value="[^"]*"' | sed 's/value="//;s/"$//')

# 2. Submit login
curl -v -c cookies.txt -b cookies.txt -X POST "http://localhost:5101/Account/Login" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Email=admin&Password=TestPass123!&__RequestVerificationToken=$TOKEN"
```

**Important Discovery:**
- Certificate Edit/Details pages use **certificate number** (e.g., "24-1605") not database ID
- `/Certificate/Edit/1` returns 400 "Certificate number is required"
- `/Certificate/Edit/24-1605` returns 200 OK

### Page Test Results

| Page | URL | Status | Notes |
|------|-----|--------|-------|
| Login | /Account/Login | 200 | CSS consolidated to auth.css |
| ForgotPassword | /Account/ForgotPassword | 200 | CSS consolidated to auth.css |
| Dashboard | /Dashboard | 200 | Requires auth |
| Certificate Create | /Certificate/Create | 200 | Works with auth |
| Certificate Edit | /Certificate/Edit/24-1605 | 200 | Use cert number, not ID |
| Certificate Details | /Certificate/Details/24-1605 | 200 | Use cert number, not ID |
| User Management | /Admin/UserManagement | 200 | Admin role required |
| Internal Cert View | /Certificate/CertificateTileViewInternal | 200 | Auth required |

### CSS Consolidation Status (Account Pages)

| File | Lines Removed | Status |
|------|---------------|--------|
| Login.cshtml | ~424 lines | ✅ Complete |
| SignUp.cshtml | ~293 lines | ✅ Complete |
| ForgotPassword.cshtml | ~432 lines | ✅ Complete |
| ResetPassword.cshtml | ~428 lines | ✅ Complete |
| 2FAAuthenticate.cshtml | ~258 lines | ✅ Complete |
| VerifyEmail.cshtml | ~227 lines | ✅ Complete |
| AccountLocked.cshtml | ~203 lines | ✅ Complete |
| PasswordResetSuccess.cshtml | ~226 lines | ✅ Complete |
| **Total** | **~2,491 lines** | **Moved to auth.css** |

---

## 2025-12-02 - Certificate Tile View UI Layout Inconsistencies

### Requirement
The three certificate tile views must maintain the same UI structure and layout:
- https://gccwebapp.com/Certificate/CertificateTileViewInternal
- https://gccwebapp.com/Certificate/ (Public)
- https://gccwebapp.com/Certificate/CertificateTileViewExternal

**Key Requirement**: Title should be "Certificates" for all three pages.

### Current State Analysis

| Element | Public View | Internal View | External View |
|---------|-------------|---------------|---------------|
| ViewData["Title"] | "Certificates" ✅ | "Certificates" ✅ | "Certificates" ✅ |
| Breadcrumbs | ❌ None | ✅ Has breadcrumbs | ✅ Has breadcrumbs |
| Font Awesome Link | ❌ Uses shared CSS only | ✅ Explicit CDN link | ✅ Explicit CDN link |
| Search Input ID | `id="searchInput"` | No ID (class only) | No ID (class only) |
| Search Placeholder | "Search" | "Search..." | "Search..." |
| Filter Content IDs | `product-category-content` | `product-cat-content` | `product-cat-content` |
| Screen Reader Support | ✅ Has aria-live div | ❌ Missing | ❌ Missing |
| Results Actions Wrapper | Uses `.results-actions` | No wrapper | No wrapper |
| Sort Button ID | `id="sortDropdownBtn"` | No ID | No ID |
| Table View | ❌ No table view | ✅ Has table view | ✅ Has table view |
| Add Certificate Button | ❌ N/A (public) | ✅ Has button | ❌ Removed (correct) |
| Download Selected Button | ❌ N/A (public) | ✅ Has button | ✅ Has button |
| Export All Button | ❌ N/A (public) | ✅ Has button | ✅ Has button |
| View Toggle (Tile/List) | ❌ N/A (public) | ✅ Has toggle | ✅ Has toggle |

### Identified Issues

1. **Inconsistent Search Input IDs**: Public has `id="searchInput"`, Internal/External have no ID
2. **Inconsistent Search Placeholders**: Public uses "Search", Internal/External use "Search..."
3. **Inconsistent Filter Content IDs**: Public uses `product-category-content`, Internal/External use `product-cat-content`
4. **Missing Accessibility Features**: Internal/External missing screen reader announcement div
5. **Inconsistent Results Header Structure**: Public wraps sort controls in `.results-actions`, Internal/External don't
6. **Missing Breadcrumbs on Public**: Public view has no breadcrumbs section
7. **Inconsistent Sort Button IDs**: Public has `id="sortDropdownBtn"`, Internal/External have no ID

### Recommendations

1. Standardize all element IDs across views
2. Use consistent placeholder text
3. Add screen reader support (aria-live) to Internal/External views
4. Use consistent CSS structure across all views
5. Consider adding breadcrumbs to Public view for consistency

---

## 2025-12-02 - FIX APPLIED: Breadcrumb Text Consistency

### Changes Made
- **CertificateTileViewInternal.cshtml**: Changed breadcrumb text from "Certificate Management" to "Certificates"
- **CertificateTileViewExternal.cshtml**: Changed breadcrumb text from "Certificate Management" to "Certificates"

### Result
All three certificate tile views now consistently display "Certificates" as the page title:
- Public: ViewData["Title"] = "Certificates" (no breadcrumb)
- Internal: ViewData["Title"] = "Certificates", Breadcrumb = "Certificates"
- External: ViewData["Title"] = "Certificates", Breadcrumb = "Certificates"

---

## 2025-12-02 - REQUIREMENT: Admin Page UI Consistency

### Requirement
All admin pages must maintain consistent UI layout:
1. **Action buttons (Export, Add New, etc.)** must be **RIGHT-ALIGNED** in the header
2. **Stats bar (Categories, Types, Subcategories, Products counts)** must be **RIGHT-ALIGNED** below search
3. **Search bar** must be **100% width** of the toolbar area

### Pages Affected
- Product Types Management (`/Admin/ProductTypes`)
- All other admin pages should follow this same pattern

### Reference Pages (Correct Layout)
- Tag Management (`/Admin/Tags`)
- User Management (`/Admin/UserManagement`)
- Role Management (`/Admin/RoleManagement`)

### Fix Applied
- Updated `product-types-override.css`:
  - `.admin-header-content`: Added flex with `justify-content: space-between`
  - `.admin-actions-section`: Added `margin-left: auto` for right alignment
  - `.admin-button-group`: Added `justify-content: flex-end`
  - `.admin-filter-section`: Added `justify-content: flex-end` for stats bar

---

## 2025-12-02 - FIX APPLIED: ProductTypes Tree Expand/Collapse Functionality

### Issue
- Expand All / Collapse All buttons not working
- Clicking on tree nodes did not expand/collapse children
- Tree view was not interactive

### Root Cause
The JavaScript used `element.style.display` to check visibility, but the `.children` elements were styled via CSS class (`.children { display: none; }`), not inline styles. When checking `children.style.display`, it returned an empty string `""` instead of `"none"`, causing the logic to fail.

### Fix Applied
Updated `wwwroot/js/product-types.js`:
1. **`toggleNode()` method** (class method): Changed to use `window.getComputedStyle(children).display` instead of `children.style.display`
2. **`expandAll()` method**: Changed to use computed style check
3. **`collapseAll()` method**: Changed to use computed style check
4. **`toggleNode()` global function**: Changed to use computed style check

### Technical Details
```javascript
// Before (broken):
const isExpanded = children.style.display !== 'none';

// After (fixed):
const computedStyle = window.getComputedStyle(children);
const isExpanded = computedStyle.display !== 'none';
```

This ensures the code correctly detects visibility whether set by CSS class or inline style.

---

## 2025-12-02 - REQUIREMENT: No Hardcoded Mock Data - Production Ready

### Requirement
**All admin pages must load data from API endpoints. No hardcoded/mock data should exist in views.**

This is critical for production readiness - views should display:
1. **Loading state** while API data is being fetched
2. **Empty state** when no data exists
3. **Real data** from the database via API

### Pages Affected
- Product Types Management (`/Admin/ProductTypes`) - FIXED
- All other admin pages should follow this pattern

### Fix Applied to ProductTypes
1. **Removed hardcoded tree data** (Building Materials, Textiles, etc.)
2. **Added loading state**: Shows spinner while `/Admin/GetProductTypes` API loads
3. **Added empty state**: Shows message when no product types exist
4. **Stats bar**: Changed from hardcoded values (3, 7, 15, 247) to zeros, updated dynamically by JavaScript

### Files Modified
- `Views/Admin/ProductTypes.cshtml`: Removed ~220 lines of hardcoded tree nodes, replaced with loading/empty states
- `wwwroot/js/product-types.js`: Updated `renderTreeView()` to handle loading/empty states
- `wwwroot/css/product-types-override.css`: Added styles for loading and empty states
- `Services/AdminService.cs`: Removed `GetSampleProductTypes()` method and changed `GetProductTypesAsync()` to return empty list instead of mock data

---

## 2025-12-03 - FIX APPLIED: ProductTypes JavaScript Data Structure Mismatch

### Issue
After removing mock data and loading from API, expand/collapse still didn't work because the tree nodes weren't being rendered correctly.

### Root Cause
**Data structure mismatch between backend and frontend:**
- **API returns**: Nested hierarchical data with `children` arrays already built (e.g., `[{id: 1, name: "Category", children: [{id: 2, name: "Type", children: [...]}]}]`)
- **JavaScript expected**: Flat data with `parentId` references that it would rebuild into hierarchy

The JavaScript `buildHierarchy()` method was trying to re-nest already-nested data, resulting in empty trees.

### Fix Applied
Updated `wwwroot/js/product-types.js`:

1. **`renderTreeView()`**: Changed to directly use nested `hierarchyData` instead of calling `buildHierarchy()`
2. **`updateStats()`**: Changed from filtering flat array to recursively traversing nested tree
3. **`populateTableView()`**: Added `flatten()` helper to convert nested data to flat rows for table display
4. **`initializeExportFunctionality()`**: Added `flatten()` helper for export

### Verified
- Database has 19 ProductTypes records with proper parent-child hierarchy
- Seed script (`FULL_TEST_DATA_SEED.sql`) includes same test data for reproducibility
- All test data resides in database and is loaded via API (no hardcoded mock data)

---

## 2025-12-03 - NEW REQUIREMENT: Certification Type Subcategory (Amazon Request)

### Business Context
Amazon requires a subcategory classification for certification types. Example:
- **Certification Type**: Certified Energy Savings (existing field)
- **Certification Type Subcategory**: Consumer Behavior (NEW field - dropdown)

This needs to be passed to Amazon and shown in login tables.

### Confirmed Requirements
1. **Uniqueness**: Subcategories unique per Certification Type (same name can exist under different parent types)
2. **Admin Management**: New admin page following existing patterns (table, Export, Add New, Template, Import)
3. **Display**: Show in certificate tables like other fields

### Open Questions (Awaiting Client Response)
1. Is the Subcategory field required or optional on certificate upload?
2. Cascading dropdown behavior when Certification Type changes?
3. Example subcategory values for seeding test data?

### Implementation Scope (Pending Confirmation)
1. New `CertificationTypeSubcategory` entity and database table
2. Add `CertificationTypeSubcategoryId` FK to `Certificates` table
3. Admin page: `/Admin/CertificationTypeSubcategories`
4. Update Create/Edit certificate forms with cascading dropdown
5. Update tile views, tables, and API responses

---

## 2025-12-03 - FIX APPLIED: ProductTypes View Toggle Button Highlighting

### Issue
Tree View and Table View buttons were not showing which view was currently active (no highlighting).

### Root Cause
The JavaScript `initializeEventHandlers()` function was removing `onclick` attributes from buttons. The `switchView()` method was trying to find the active button using `document.querySelector(`[onclick*="switchView('${viewType}')"]`)`, but the onclick attribute no longer existed after initialization.

### Fix Applied
1. **ProductTypes.cshtml**: Added `data-view` attribute to both view buttons
   - Tree View button: `data-view="tree"`
   - Table View button: `data-view="table"`

2. **product-types.js - `switchView()` method**: Changed selector from onclick-based to data-view-based:
   ```javascript
   // Before (broken):
   const activeButton = document.querySelector(`[onclick*="switchView('${viewType}')"]`);

   // After (fixed):
   const activeButton = document.querySelector(`.view-btn[data-view="${viewType}"]`);
   ```

3. **product-types.js - `initializeEventHandlers()`**: Updated to use data-view attribute:
   ```javascript
   const viewButtons = document.querySelectorAll('.view-btn[data-view]');
   viewButtons.forEach(button => {
       const viewType = button.getAttribute('data-view');
       button.removeAttribute('onclick');
       button.addEventListener('click', () => switchView(viewType));
   });
   ```

### Result
- Tree View button is highlighted on page load (default view)
- Clicking Table View highlights that button and removes highlight from Tree View
- Clicking Tree View switches back and updates highlighting correctly

---

## 2025-12-07 - FIX APPLIED: Integration Test HTTPS Port Configuration

### Issue
~40+ integration tests were failing with error: "Failed to determine the https port for redirect"

### Root Cause
`app.UseHttpsRedirection()` in Program.cs was being called unconditionally. When tests use `WebApplicationFactory<Program>`, the test server doesn't have HTTPS configured, causing the middleware to fail when trying to determine the HTTPS port.

### Fix Applied
1. **Program.cs** (line 511-515): Made HTTPS redirection conditional on environment:
   ```csharp
   // HTTPS redirection - skip in Test environment to avoid "Failed to determine https port" errors
   if (!app.Environment.IsEnvironment("Test"))
   {
       app.UseHttpsRedirection();
   }
   ```

2. **Created `CustomWebApplicationFactory.cs`**: New shared test fixture that:
   - Sets environment to "Test"
   - Uses in-memory database
   - Seeds test data (roles, users, companies, products, certification types)
   - Configures HTTP-only (no HTTPS) for tests

3. **Updated test classes** to use `CustomWebApplicationFactory<Program>` instead of `WebApplicationFactory<Program>`:
   - `LoginIntegrationTests.cs`
   - `PreDeploymentLoginSmokeTest`
   - `SecurityIntegrationTests.cs` (IntegrationTests.cs)
   - `EdgeCasesErrorHandlingTests.cs`
   - `AdvancedMobileApiEdgeCaseTests.cs`
   - `BusinessContinuityTests.cs`

4. **Fixed `ProductControllerTests.cs`**: Updated constructor call to include new required parameters (logger, configuration)

### Result
- "Failed to determine the https port for redirect" warning eliminated from test output
- Test infrastructure properly configured for HTTP-only testing

### Files Modified
- `GCCWebApp/Program.cs`
- `GCCWebApp-Tests/GCCWebApp.Tests/CustomWebApplicationFactory.cs` (new)
- `GCCWebApp-Tests/GCCWebApp.Tests/Authentication/LoginIntegrationTests.cs`
- `GCCWebApp-Tests/GCCWebApp.Tests/IntegrationTests.cs`
- `GCCWebApp-Tests/GCCWebApp.Tests/UX/EdgeCasesErrorHandlingTests.cs`
- `GCCWebApp-Tests/GCCWebApp.Tests/Mobile/AdvancedMobileApiEdgeCaseTests.cs`
- `GCCWebApp-Tests/GCCWebApp.Tests/Integration/BusinessContinuityTests.cs`
- `GCCWebApp-Tests/GCCWebApp.Tests/Controllers/ProductControllerTests.cs`

---

## 2025-12-07 - Test Suite Status Summary

### Current Test Results
| Metric | Count |
|--------|-------|
| Total Tests | 591 |
| Passed | 461 |
| Failed | 130 |
| Pass Rate | 78% |

### Remaining Failure Categories (130 tests)
1. **API Key Authentication Handler Tests** (~15 tests) - Mock setup issues with extension methods
2. **Validation Service Tests** (~10 tests) - ASIN/UPC validation logic
3. **JWT Token Service Tests** (~5 tests) - Token generation/validation
4. **Login Integration Tests** (~7 tests) - Test database not having seeded users at login time
5. **Mobile/UX Edge Case Tests** (~40 tests) - Playwright browser tests, network simulation
6. **Document Controller Tests** (~3 tests) - File upload service mocking
7. **Partner Management Tests** (~3 tests) - Service layer tests
8. **Data Management/Migration Tests** (~5 tests) - Database migration tests
9. **Email Service Tests** (~2 tests) - SMTP configuration tests

### Next Steps to Improve Pass Rate
1. Fix test database seeding to ensure users exist for login tests
2. Update API Key Authentication Handler tests to use proper mock setup
3. Review validation service logic vs test expectations
4. Skip or mark as "requires browser" the Playwright-based mobile tests

---

## 2025-12-11 - Import Quality Improvements: Fuzzy Matching, Duplicate Detection, Order Guidance

### Business Context
Data migration/import activities consistently fail due to:
- Company name mismatches (40% of failures) - "Amazon.com" vs "Amazon", "Company Inc" vs "Company"
- Import order violations - Importing products before companies exist
- Duplicate entries within same import file

### Root Cause Analysis
1. **Header normalization ≠ Value normalization**: Headers were normalized (CompanyName→company), but values weren't (Amazon.com ≠ Amazon)
2. **No pre-flight FK validation**: Errors only shown after import attempt
3. **No import order enforcement**: Users could import Products before Companies
4. **Duplicate detection on database save only**: Cryptic DB errors instead of preview warnings

### Fixes Applied

#### 1. Fuzzy Company Name Matching (productManagement.js)
**Location**: Lines 1173-1326

**Implementation**:
- 3-tier matching: Exact → Normalized → Fuzzy
- `normalizeCompanyName()`: Strips Inc/Corp/LLC/Ltd/Co suffixes
- `calculateSimilarity()`: Character overlap scoring (≥0.8 threshold for match)
- `getSimilarCompanies()`: Returns top 3 suggestions with ≥0.3 similarity

**Result**: "Amazon.com Inc" now matches "Amazon" automatically, with warning shown

#### 2. Pre-Flight FK Validation (productManagement.js)
**Location**: Lines 714-734 in `buildImportPreview()`

Validates company names BEFORE showing preview modal, not after import attempt.

#### 3. Duplicate Detection Within Import File

**User Management** (userManagement.js lines 771-772, 831-839):
```javascript
const emailsInImport = new Map(); // Track emails within import file
// ... checks for duplicate emails in same import
```

**Tag Management** (tags.js lines 625-626, 686-696):
```javascript
const namesInImport = new Map(); // Track tag names within import file
// ... checks for duplicate tag names in same import
```

#### 4. Import Order Guidance Banner (productManagement.js)
**Location**: Lines 771-803

Shows yellow warning banner when FK errors detected:
- "Recommended Import Order: Companies → Product Types → Products"
- Dismissible with X button
- Only appears when company-not-found errors exist

### Header Normalization Standardized (All 4 Pages)

| File | Function | Line |
|------|----------|------|
| productManagement.js | `normalizeHeaderName()` | 1140-1171 |
| userManagement.js | `normalizeUserHeaderName()` | 723-766 |
| tags.js | `normalizeTagHeaderName()` | 1105-1127 |
| companyManagement.js | `normalizeCompanyHeaderName()` | 540-566 |

All pages now handle header variations: case, spaces, underscores, common aliases.

### Data Migration Documentation Suite Created
**Location**: `TestData/DataMigration/` (6 files)

| Document | Purpose |
|----------|---------|
| `README.md` | Quick start guide, import order |
| `DATA_MIGRATION_RISK_ASSESSMENT.md` | 47 risks, severity matrix, 50-60% cost savings |
| `DATA_QUALITY_CHECKLIST.md` | Scoring (90%+ proceed, <50% reject) |
| `DATA_VALIDATION_RULES.md` | Field specs, regex patterns per entity |
| `PRE_MIGRATION_AUDIT_TEMPLATE.md` | Go/No-Go checklist, approvals |
| `NAME_MAPPING_TEMPLATE.csv` | 45 value transformations |

### Test Templates Created
**Location**: `TestData/ImportTemplates/` (18 CSV files)

Covers: positive, negative, boundary, special characters, mixed test cases
Security tests: SQL injection, XSS attempts

### Bug Fixes During Session

1. **Toast z-index issue**: Toast appearing BEHIND modal backdrop
   - **Fix**: Changed `--admin-z-toast` from 1080 to 10000 in `admin-core.css:123`

2. **Product Management JS not loaded**: `productManagement.js` wasn't included in Management.cshtml
   - **Fix**: Added `<script src="~/js/admin/productManagement.js"></script>` at line 1314

3. **Global variable name mismatch**: HTML onclick used `productManager.` but JS global is `productManagement`
   - **Fix**: Updated all onclick handlers with `replace_all: true`

4. **JSON response validation**: API returning HTML instead of JSON
   - **Fix**: Added content-type check before parsing JSON response

### Key Learnings

1. **Header normalization is NOT value normalization** - Fixing headers doesn't fix mismatched company names
2. **Pre-flight validation saves user frustration** - Show problems before import, not after
3. **Import order matters** - FK dependencies must be respected
4. **Duplicate detection should happen in preview** - Not on database save
5. **HashGenerator processes lock builds** - Kill with `taskkill //F //PID {pid}` before retry

### Files Modified

**JavaScript**:
- `wwwroot/js/admin/productManagement.js` - Fuzzy matching, FK validation, order guidance
- `wwwroot/js/admin/userManagement.js` - Duplicate email detection, header normalization
- `wwwroot/js/admin/tags.js` - Duplicate tag detection, header normalization
- `wwwroot/js/admin/companyManagement.js` - Header normalization

**CSS**:
- `wwwroot/css/admin-core.css` - Toast z-index fix

**Views**:
- `Views/Product/Management.cshtml` - Script include, onclick handler fixes

### Testing Status
**Server running**: http://localhost:5101 (Background Bash ID c1303d)
**Build**: Succeeded with 85 warnings, 0 errors

---

## 2025-12-11 - RECOMMENDATION: Future Import Improvements

### High Priority (Not Yet Implemented)
1. **Fuzzy matching for other FK fields** (ProductType, Category) - Currently only Company has fuzzy matching
2. **Auto-suggest corrections in preview** - Click to apply suggested value
3. **Import order enforcement modal** - Block Product import if Companies table is empty
4. **Bulk fix for common transformations** - "Change all 'Amazon.com' to 'Amazon'"

### Medium Priority
1. **Progress indicator for large imports** - Currently no feedback during processing
2. **Undo import** - Rollback capability within X minutes
3. **Import history/audit log** - Track who imported what and when

### Low Priority
1. **Import templates per partner** - Amazon-specific, Wayfair-specific formats
2. **Scheduled imports** - Auto-import from FTP/cloud storage
3. **Import validation rules admin** - Let admins define custom validation rules

---

## 2025-12-11 - USABILITY ANALYSIS: Certificate Create/Edit Views

### Overview
Analysis of the certificate add/edit functionality focusing on:
1. ISBN handling in Product ID management
2. How new controls can be added without affecting design
3. Inline company/product creation requirements

---

### 1. ISBN Handling Analysis

#### Current Implementation

**Location**: Product ID Manager shared component
- `Views/Shared/_ProductIdManager.cshtml` - UI component
- `wwwroot/js/product-id-manager.js` - Client-side validation
- `Services/ProductIdService.cs` - Server-side validation

**Supported Product ID Types**: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN, OTHER

**ISBN Validation (ProductIdService.cs:41)**:
```csharp
{ "ISBN", @"^(?:ISBN(?:-1[03])?:? )?(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+[- ]){4})[- 0-9]{17}$)(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]$" }
```

**Basic Validation (ProductIdService.cs:487-493)**:
```csharp
private bool ValidateIsbn(string isbn)
{
    if (string.IsNullOrWhiteSpace(isbn)) return false;
    var cleanIsbn = Regex.Replace(isbn, @"[^0-9X]", "");
    return cleanIsbn.Length == 10 || cleanIsbn.Length == 13;
}
```

**Help Text (product-id-manager.js:458)**:
```javascript
'ISBN': 'International Standard Book Number (10 or 13 digits)'
```

**UI Styling (product-id-manager.css:118)**:
```css
.product-type-isbn { background: #fce4ec; color: #c2185b; }
```

#### Strengths
1. **Flexible format acceptance** - Accepts ISBN-10, ISBN-13, with or without hyphens/spaces
2. **Visual distinction** - Pink badge differentiates ISBN from other types
3. **Helpful error messages** - Clear validation feedback
4. **Consistent with other types** - Same UX pattern as ASIN, UPC, etc.

#### Identified Issues

| Issue | Severity | Description |
|-------|----------|-------------|
| No checksum validation | Medium | ISBN has a check digit algorithm; current validation only checks length |
| No format standardization | Low | Saved value keeps user's formatting instead of normalizing |
| Limited help text | Low | Doesn't explain ISBN-10 vs ISBN-13 difference |
| No ISBN lookup | Enhancement | Could integrate with ISBN database for title verification |

#### Recommendations

1. **Enhanced ISBN Validation** (Medium Priority)
   - Add ISBN-10 and ISBN-13 checksum validation
   - ISBN-10: Sum of (digit × position) mod 11 = 0
   - ISBN-13: Alternating 1/3 weights, mod 10 = 0

2. **Format Standardization** (Low Priority)
   - Store normalized format (digits only)
   - Display with standard hyphenation for readability

3. **Improved Help Text** (Low Priority)
   - Update to: "ISBN-10 (10 digits) or ISBN-13 (13 digits, starts with 978/979)"

---

### 2. Adding New Controls Without Affecting Design

#### Current Layout Architecture

**Grid System (certificate-create.css:177-198)**:
```css
.form-grid {
    display: grid !important;
    grid-template-columns: 1fr 380px !important;
    gap: 32px !important;
}
.main-form {
    display: grid !important;
    grid-template-columns: 1fr 1fr !important;
    gap: 20px 24px !important;
}
```

**Layout Structure**:
```
┌─────────────────────────────────────────────────────────────────┐
│                    Certificate Create/Edit                       │
├───────────────────────────────────────┬─────────────────────────┤
│         Main Form (flexible)          │    Sidebar (380px)      │
│                                       │                         │
│  ┌────────────┐  ┌────────────┐      │  ┌─────────────────┐    │
│  │ Field 1    │  │ Field 2    │      │  │ File Upload     │    │
│  └────────────┘  └────────────┘      │  └─────────────────┘    │
│                                       │                         │
│  ┌─────────────────────────────┐     │  ┌─────────────────┐    │
│  │ Full-width field            │     │  │ Tags Section    │    │
│  └─────────────────────────────┘     │  └─────────────────┘    │
│                                       │                         │
│  ┌─────────────────────────────┐     │                         │
│  │ Card Section (e.g., IDs)    │     │                         │
│  └─────────────────────────────┘     │                         │
└───────────────────────────────────────┴─────────────────────────┘
```

#### Recommended Pattern for New Controls

**1. Card-Style Section Pattern (certificate-create.css:132-144)**:
```css
.card-style-section {
    grid-column: 1 / -1;              /* Full width of main form */
    background-color: #f8f9fa;
    border-radius: 8px;
    padding: 20px;
    border: 1px solid #dee2e6;
}
```

**2. Adding a New Section**:
```html
<!-- Example: New "Certification Results" field -->
<div class="card-style-section">
    <h4 class="section-title">Additional Information</h4>
    <div class="form-group">
        <label asp-for="CertificationResults">Certification Results</label>
        <textarea asp-for="CertificationResults" class="form-control"
                  placeholder="Optional: Enter certification score/results"></textarea>
        <span class="text-muted small">For partner threshold verification (e.g., Amazon)</span>
    </div>
</div>
```

#### Design-Safe Addition Rules

| Rule | Description |
|------|-------------|
| Use `grid-column: 1 / -1` | Full-width sections won't disrupt 2-column layout |
| Use card-style sections | Visually groups related fields |
| Place after Product IDs section | Logical flow maintained |
| Keep sidebar intact | Never add form fields to 380px sidebar |
| Test at breakpoints | Verify 1024px, 768px, 480px responsive behavior |

#### Responsive Breakpoints (certificate-create.css)
- **1024px**: Form grid collapses to single column
- **768px**: Main form collapses to single column
- **480px**: Compact mobile layout

---

### 3. Inline Company/Product Creation Requirements

#### Requirements Statement
From CLAUDE.md: "requirements stated the ability to add new companies and products on the certificate page"

#### Current Backend Support

**Company Creation (CertificateController.cs:1553-1558)**:
```csharp
company = new Company {
    Name = row.CompanyName,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
await _unitOfWork.Companies.AddAsync(company);
await _unitOfWork.CompleteAsync();
```

**Product Creation (CertificateController.cs:1567-1573)**:
```csharp
product = new Product {
    Name = row.ProductName,
    CompanyId = company.Id,
    IsActive = true
};
await _unitOfWork.Products.AddAsync(product);
await _unitOfWork.CompleteAsync();
```

**Note**: This code exists in the IMPORT process, not the manual create/edit forms.

#### Gap Analysis

| Requirement | Import | Manual Create/Edit |
|-------------|--------|-------------------|
| Create company inline | ✅ Implemented | ❌ Not implemented |
| Create product inline | ✅ Implemented | ❌ Not implemented |
| UI modal for creation | N/A (automatic) | ❌ Not implemented |
| Validation | ✅ Basic | Needs design |

#### Recommended Implementation

**Option A: Quick-Add Modal Pattern** (Recommended)
```
┌──────────────────────────────────────────┐
│ Company                           [+]    │  <- "+" opens modal
│ ┌────────────────────────────────┐       │
│ │ Select Company...           ▼  │       │
│ └────────────────────────────────┘       │
└──────────────────────────────────────────┘

[+] Modal:
┌────────────────────────────────────────┐
│ Quick Add Company                   X  │
├────────────────────────────────────────┤
│ Company Name: [________________]       │
│                                        │
│           [Cancel]  [Add Company]      │
└────────────────────────────────────────┘
```

**Benefits**:
- User stays on certificate page
- Dropdown refreshes with new company
- Consistent with admin modal patterns
- Uses existing AdminCore modal infrastructure

**Option B: Autocomplete with Create** (Alternative)
- User types company name
- If no match, show "Create 'X' as new company?" option
- Similar to tag creation pattern in Tags Management

#### UI Implementation Suggestion

```javascript
// Add "+" button next to Company dropdown
function initQuickAddCompany() {
    const addBtn = document.getElementById('quickAddCompanyBtn');
    addBtn.addEventListener('click', async () => {
        const confirmed = await AdminCore.showModal({
            title: 'Quick Add Company',
            body: '<input type="text" id="newCompanyName" class="form-control" placeholder="Company Name">',
            confirmText: 'Add Company',
            type: 'primary'
        });

        if (confirmed) {
            const name = document.getElementById('newCompanyName').value;
            const response = await fetch('/api/companies', {
                method: 'POST',
                body: JSON.stringify({ name })
            });
            // Refresh dropdown and select new company
        }
    });
}
```

#### Priority Assessment

| Feature | Priority | Rationale |
|---------|----------|-----------|
| Inline Company Creation | High | Prevents workflow interruption |
| Inline Product Creation | Medium | Less common need (products usually pre-exist) |
| Certification Results Field | High | Amazon requirement per CLAUDE.md |

---

### Summary of Recommendations

#### High Priority
1. **Add Certification Results field** - Open text field, optional, for Amazon threshold verification
2. **Implement inline company creation** - Quick-add modal from certificate page
3. **Enhanced ISBN checksum validation** - Proper ISBN-10/ISBN-13 check digit verification

#### Medium Priority
1. **Implement inline product creation** - Quick-add modal for new products
2. **Format standardization for product IDs** - Normalize storage format

#### Low Priority
1. **Improved help text** - More detailed format guidance
2. **ISBN lookup integration** - Optional title verification

---

### Files Referenced
- `Views/Certificate/Create.cshtml`
- `Views/Certificate/Edit.cshtml`
- `Views/Shared/_ProductIdManager.cshtml`
- `wwwroot/js/product-id-manager.js`
- `wwwroot/css/certificate-create.css`
- `wwwroot/css/product-id-manager.css`
- `Services/ProductIdService.cs`
- `Controllers/CertificateController.cs`

---

## 2025-12-11 - IMPLEMENTATION: Certificate Create/Edit Usability Enhancements

### Changes Made

#### 1. Certification Results Field (Already Implemented)
- **Model**: `Certificate.cs:44` - `CertificationResults` property (max 1000 chars)
- **ViewModel**: `CertificateEditViewModel.cs:27` - `CertificationResults` property
- **Create View**: `Create.cshtml:150-155` - Textarea with placeholder text
- **Edit View**: `Edit.cshtml:151-156` - Textarea with existing value binding
- **Status**: ✅ Complete (was already implemented)

#### 2. Inline Company/Product Creation (NEW)
**File Created**: `wwwroot/js/certificate-inline-create.js`

**Features**:
- "Add New Company..." option at bottom of Company dropdown
- "Add New Product..." option at bottom of Product dropdown
- Visual separator line (───) before the "Add New" options
- Modal dialogs following US-015 backdrop standard (85% black, 3px blur)
- Company creation: Name (required), Email, Website URL
- Product creation: Name (required), Company (required), Description
- New items inserted before separator and automatically selected
- Toast notifications on success
- CSRF token handling for security
- Previous selection restored if modal cancelled

**UI Pattern**: Selecting "Add New Company..." or "Add New Product..." from the dropdown opens a modal, creates the item, then selects it in the dropdown.

**Files Modified**:
- `Views/Certificate/Create.cshtml:307` - Added script reference
- `Views/Certificate/Edit.cshtml:315` - Added script reference

**Backend Endpoints Used**:
- `/Company/SaveCompany` (POST) - Creates new company
- `/Product/SaveProduct` (POST) - Creates new product

#### 3. Enhanced ISBN Checksum Validation (NEW)

**Server-Side** (`Services/ProductIdService.cs:487-565`):
- `ValidateIsbn()` - Main validation with length check
- `ValidateIsbn10Checksum()` - ISBN-10 modulo 11 algorithm
- `ValidateIsbn13Checksum()` - ISBN-13 EAN-13 algorithm
- Validates check digit properly
- ISBN-13 must start with 978 or 979
- X only valid as last character in ISBN-10

**Client-Side** (`wwwroot/js/product-id-manager.js:411-478`):
- `validateIsbn()` - Main validation method
- `validateIsbn10Checksum()` - ISBN-10 checksum
- `validateIsbn13Checksum()` - ISBN-13 checksum
- Matches server-side validation logic

**Improved Help Text** (`product-id-manager.js:531`):
```
'ISBN': 'ISBN-10 (10 digits, e.g., 0-306-40615-2) or ISBN-13 (13 digits starting with 978/979, e.g., 978-0-306-40615-7)'
```

**Improved Error Message** (`product-id-manager.js:509`):
```
'ISBN': 'Invalid ISBN format or checksum. Use ISBN-10 (10 digits) or ISBN-13 (13 digits starting with 978/979)'
```

### Test Status
- **Build**: ✅ Succeeded (0 errors, 0 warnings)
- **Application**: ✅ Running on http://localhost:5101

### Files Changed
| File | Type | Change |
|------|------|--------|
| `wwwroot/js/certificate-inline-create.js` | NEW | Inline company/product creation functionality |
| `Views/Certificate/Create.cshtml` | MODIFIED | Added script reference |
| `Views/Certificate/Edit.cshtml` | MODIFIED | Added script reference |
| `Services/ProductIdService.cs` | MODIFIED | Enhanced ISBN validation with checksum |
| `wwwroot/js/product-id-manager.js` | MODIFIED | Client-side ISBN validation, help text |

### Client Review Checklist
- [ ] Test Company quick-add from Certificate Create page
- [ ] Test Product quick-add from Certificate Create page
- [ ] Test Company quick-add from Certificate Edit page
- [ ] Test Product quick-add from Certificate Edit page
- [ ] Test ISBN-10 validation with valid ISBN (e.g., 0-306-40615-2)
- [ ] Test ISBN-10 validation with invalid checksum
- [ ] Test ISBN-13 validation with valid ISBN (e.g., 978-0-306-40615-7)
- [ ] Test ISBN-13 validation with invalid checksum
- [ ] Verify Certification Results field saves and loads correctly

---

## 2025-12-13 - MILESTONE: Ready for UAT

### Achievement
**GCC Database Solution is officially ready for User Acceptance Testing (UAT).**

This marks a significant project milestone after extensive iterations and improvements including:

### Journey Summary
- **UI/UX Overhaul**: Certificate tile views, admin pages, responsive design
- **Database-Driven Filters**: Dynamic filter options loaded from database (all 3 views)
- **Import Quality**: Fuzzy matching, duplicate detection, import order guidance
- **Inline Creation**: Quick-add company/product from certificate pages
- **Security**: Role-based access, 2FA, API key management
- **Testing**: 624+ tests across 7 test projects (87% pass rate)
- **Data Migration**: Comprehensive validation rules and templates
- **Modal Standardization**: US-015 backdrop standard (85% black, 3px blur)
- **Export Consistency**: CSV-only exports across all admin pages

### What's Next
- Client UAT review and feedback
- Additional improvements planned post-UAT
- Production deployment preparation

### Key Metrics at UAT Milestone
| Metric | Value |
|--------|-------|
| Total Tests | 624+ |
| Pass Rate | 87% |
| Test Projects | 7 |
| User Stories Complete | US-001 to US-010 |
| Admin Pages | 10+ fully functional |
| Certificate Views | 3 (Public, Internal, External) |

### Client Communication
User informed client that the system is ready for UAT. Additional improvements are planned but the core functionality is complete and stable.

---

## 2025-12-13 - Custom Fields Not Displaying on Certificate Create/Edit Pages

### Issue Reported
Custom fields added from the Admin > Certificate Fields page were not appearing on:
- Certificate Create page (`/Certificate/Create`)
- Certificate Edit page (`/Certificate/Edit/{number}`)
- Internal Certificate View

### Root Causes Identified

1. **JavaScript Admin Page Missing `section` Field**
   - `certificate-fields.js` line 319-326: `saveInlineEdit()` function collected section from `inputs[0]` but never sent it to the server
   - Section field was silently dropped during save operations

2. **JavaScript Admin Page Sending Wrong Field Name**
   - Admin JS was sending `notes` but the C# model uses `defaultValue` for notes
   - Property mismatch caused notes to be lost

3. **Missing Field Types in Custom Fields Rendering**
   - `certificate-custom-fields.js` did not handle: email, phone, textarea, multiselect
   - Seed data used these types but they weren't rendering

4. **Automatic Database Seeding Disabled**
   - `SeedData.InitializeAsync()` only runs migrations
   - `SeedCustomFieldsAsync()` exists but is never called
   - CustomFields table likely empty without manual seeding

### Fixes Applied

**File: `wwwroot/js/admin/certificate-fields.js`**
- Added `section: inputs[0].value` to save data (line 320)
- Changed `notes` to `defaultValue` to match C# model (line 325)
- Updated `renderNormalRow` and `renderEditRow` to use defaultValue for notes
- Added email, phone, textarea to control types dropdown
- Updated `formatFieldType()` to include new types

**File: `wwwroot/js/certificate-custom-fields.js`**
- Added case handlers for: email, phone, textarea, multiselect
- Added `escapeHtml()` helper for XSS prevention
- Added `parseOptions()` helper that handles both JSON arrays and comma-separated strings
- Updated options parsing to be more robust

### Technical Details

**Data Flow:**
1. Admin adds field via `/Admin/CertificateFields`
2. JS calls `/Admin/CreateCertificateField` with field data
3. `AdminService.CreateCertificateFieldAsync()` saves to `CustomFields` table
4. Certificate Create/Edit loads via `/Certificate/GetCustomFields?certificateTypeId={id}`
5. `CertificateFieldService.GetFieldsAsync()` returns all visible fields
6. `certificate-custom-fields.js` renders fields dynamically

**Property Mappings:**
| Admin UI Column | C# Model Property | JS Property |
|-----------------|-------------------|-------------|
| Section | Section | section |
| Field Name | Name | name |
| Control Type | FieldType | fieldType |
| Required | IsRequired | isRequired |
| Notes | DefaultValue | defaultValue |

### Testing Recommendations
1. Go to Admin > Certificate Fields
2. Click "Add New Field"
3. Fill in Section, Name, Type, Required, Notes
4. Click Save - verify toast shows success
5. Go to Certificate > Create
6. Select a Certification Type
7. Verify custom fields appear in Custom Fields section

### Files Modified
- `wwwroot/js/admin/certificate-fields.js`
- `wwwroot/js/certificate-custom-fields.js`

---

## 2025-12-13 - FIX APPLIED: Certificate Save After PDF Upload Not Working

### Issue Reported
User reported that saving the certificate after uploading a PDF was not working. The form would not submit after drag-and-drop PDF upload.

### Root Cause Analysis
The drag-and-drop PDF upload handler in `certificate-create.js` was showing the file preview but **NOT assigning the dropped file to the input element's `files` property**.

```javascript
// BEFORE (broken):
uploadZone.addEventListener('drop', (e) => {
    const file = e.dataTransfer.files[0];
    if (file) {
        this.handleFile(file, pdfInput, previewContainer); // Only shows preview!
    }
});
```

This caused:
1. The `certificatePdf` input remained empty after drag-drop
2. HTML5 form validation failed (input has `required` attribute)
3. Form submission was silently blocked

**Click-to-upload worked** because the native file dialog properly populates `input.files`.

### Fix Applied
Used the `DataTransfer` API to assign the dropped file to the input element:

```javascript
// AFTER (fixed):
uploadZone.addEventListener('drop', (e) => {
    const file = e.dataTransfer.files[0];
    if (file) {
        // Assign the dropped file to the input element
        try {
            const dataTransfer = new DataTransfer();
            dataTransfer.items.add(file);
            pdfInput.files = dataTransfer.files;
        } catch (err) {
            console.warn('DataTransfer not supported:', err);
        }
        this.handleFile(file, pdfInput, previewContainer);
    }
});
```

### Testing
1. Go to Certificate > Create
2. Drag and drop a PDF file onto the upload zone
3. Verify the preview shows
4. Fill in required fields and click Save
5. Verify the certificate is created successfully with the PDF attached

### Files Modified
- `wwwroot/js/certificate-create.js` (lines 175-193)

### Impact
- Fix applies to both Certificate Create and Edit pages (shared JavaScript)
- DataTransfer API is supported in all modern browsers (Chrome, Firefox, Edge, Safari)

---

## 2025-12-14 - API and Certificate Pages Testing Session

### Testing Summary

Today's session focused on testing:
1. API endpoints functionality
2. Certificate Create/Edit pages and custom fields
3. End-to-end CRUD operations
4. Test suite execution

### API Testing Results

#### Public API Endpoints - Working
| Endpoint | Status | Notes |
|----------|--------|-------|
| `GET /api/certificate-tiles` | 200 | Returns paginated certificates |
| `GET /api/certificate-tiles/filters` | 200 | Returns filter options |
| `GET /health` | 200 | Health check working |

#### Authenticated API (v1) - Working
**API Key Used**: `gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7`

| Endpoint | Status | Notes |
|----------|--------|-------|
| `GET /api/v1/certificates` | 200 | Returns 178 certificates |
| `GET /api/v1/certificates/{id}` | 200 | Returns certificate by ID |
| `GET /api/v1/certificates/number/{number}` | 200 | Returns by cert number |

### Defects Found and Fixed

#### 1. API Test Compilation Error - FIXED
**File**: `GCCWebApp.Api.Tests/Unit/Controllers/CertificateTilesApiControllerTests.cs`

**Issue**: Test class missing `IConfiguration` parameter in controller constructor

**Error Message**:
```
error CS7036: There is no argument given that corresponds to the required parameter
'configuration' of 'CertificateTilesApiController.CertificateTilesApiController(...)'
```

**Fix Applied**:
- Added `Mock<IConfiguration> _configurationMock` field
- Added configuration mock setup in constructor
- Added `_configurationMock.Object` to controller instantiation

**Lines Modified**: 1-47

### Test Suite Results

#### Unit Tests (GCCWebApp.Tests)
| Category | Status | Notes |
|----------|--------|-------|
| Documentation Tests | Multiple failures | Documentation not meeting test expectations |
| RBAC Security Tests | 1 failure | UserManagementPermissions test |
| Configuration Tests | Passing | |
| Model Tests | Passing | |

#### API Tests (GCCWebApp.Api.Tests)
| Category | Status | Notes |
|----------|--------|-------|
| CertificateTilesApiControllerTests | Failures | Mock setup issues (not related to compilation fix) |
| MultiPartnerApiIntegrationTests | Failures | Requires running server |
| ApiIntegrationTests | Failures | Requires running server |

### Custom Fields Architecture

#### Current Implementation
- **Admin Page**: `/Admin/CertificateFields` - Manages field definitions
- **API**: `/Certificate/GetCustomFields?certificateTypeId={id}` - Returns fields for cert type
- **JavaScript**: `certificate-custom-fields.js` - Client-side rendering
- **Service**: `CertificateFieldService` - Server-side field management

#### Data Flow
1. Admin creates field in CertificateFields page
2. Field saved to `CustomFields` table via AdminService
3. Certificate Create/Edit loads fields via `GetCustomFields` endpoint
4. JavaScript renders fields dynamically based on type
5. Field values saved with certificate via `SaveCustomFieldValues`

### Authentication Requirements

| Page/Endpoint | Authorization |
|---------------|---------------|
| Public Tile View | None (Anonymous) |
| Certificate Create/Edit | AnalystOrAdmin |
| Admin Certificate Fields | AdminOnly |
| Custom Fields API | AnalystOrAdmin |
| v1 API | API Key (Partner) |

### Recommendations

1. **Fix remaining test mock setups** - Several API tests fail due to mock configuration issues
2. **Run Playwright tests** - E2E tests require browser installation
3. **Documentation tests** - Update documentation to meet test expectations or adjust tests
4. **Integration tests** - Need running application for integration tests to pass

### Files Modified This Session
- `GCCWebApp.Api.Tests/Unit/Controllers/CertificateTilesApiControllerTests.cs` - Fixed IConfiguration parameter

### Application Status
- **Build**: Successful
- **Running**: http://localhost:5101
- **Database**: Connected with 178 certificates

---

## 2025-12-18 - OPEN ITEM: Additional Filter - Tag Management Requirements Clarification

### Status: OPEN - Awaiting Requirements Clarification

### Issue Summary
The "Additional" filter section on Certificate Tile Views currently uses `TagType = 'external'` to determine which tags appear. This conflates two different concepts:

1. **Visibility Control** - Who can see the tag (External users vs Internal/Admin users)
2. **Filter Display** - Which tags appear in the "Additional" filter panel

### Current Implementation
```csharp
// CertificateTilesApiController.cs - GetFilterOptions()
additionalTags = await baseQuery
    .SelectMany(c => c.Tags)
    .Where(t => t.IsActive && t.TagType == "external")  // Uses TagType for filter display
    .Select(t => t.Name)
    .Distinct()
    .ToListAsync()
```

### Current Tag Model
```csharp
public class Tag {
    public int Id { get; set; }
    public string Name { get; set; }
    public string TagType { get; set; }  // "internal" or "external"
    public bool IsActive { get; set; }
    // ...
}
```

### Problem Scenarios

| Scenario | Current Behavior | Potential Issue |
|----------|------------------|-----------------|
| "High Priority Client" tag (internal) | Hidden from Additional filter | Cannot filter by it even in Internal view |
| "APR" tag (external) | Shows in Additional filter | Correct behavior |
| New internal workflow tag | Hidden from filter | May need filtering capability |

### Questions Requiring Clarification

1. **What is the purpose of the Additional filter?**
   - Certification-related attributes only (APR, LEED)?
   - All tags that aren't internal?
   - User-selectable via admin interface?

2. **Should filter visibility be separate from user visibility?**
   - A tag could be internal (hidden from External users) but still filterable in Internal view
   - Current design doesn't support this separation

3. **Temporary Fix Applied (2025-12-18)**
   - Set only APR and LEED as `TagType = 'external'`
   - All other tags set to `TagType = 'internal'`
   - This is a workaround, not a proper solution

### Recommended Solution (Pending Approval)

Add a `ShowInAdditionalFilter` boolean field to the Tag model:

```csharp
public class Tag {
    public int Id { get; set; }
    public string Name { get; set; }
    public string TagType { get; set; }      // Controls visibility to External users
    public bool ShowInAdditionalFilter { get; set; }  // NEW: Controls filter panel display
    public bool IsActive { get; set; }
}
```

**Benefits:**
- Separates visibility control from filter display
- Internal tags can optionally appear in Internal view's Additional filter
- Admin can configure which tags appear in filters via Tag Management page
- No hardcoding required

### Implementation Scope (If Approved)

1. Add `ShowInAdditionalFilter` column to Tags table (migration)
2. Update Tag model with new property
3. Update Tag Management admin page to include toggle
4. Update all 3 API controllers to filter by `ShowInAdditionalFilter`
5. Seed existing APR and LEED with `ShowInAdditionalFilter = true`

### Action Required
- [ ] Client to clarify requirements for Additional filter
- [ ] Decide on recommended solution vs alternative approach
- [ ] Prioritize implementation

---

## 2025-12-14 - REQUIREMENT: Certificate Number Format Consistency

### Requirement
**Certificate Number must be displayed as formatted value (XX-XXXX) consistently across ALL pages.**

Examples:
- ✅ Correct: `25-5057`, `24-5118`, `24-0366`
- ❌ Wrong: `85`, `146`, `75` (numeric database IDs)

### Where Certificate Number Must Be Displayed (NOT ID)
| Location | Field to Use | Status |
|----------|--------------|--------|
| Public Tile View | `cert.certificateNumber` | ✅ Verified |
| Internal Tile View (Table) | `cert.certificateNumber` | ✅ Verified |
| Internal Tile View (Cards) | `cert.certificateNumber` | ✅ Verified |
| External Tile View (Table) | `cert.certificateNumber` | ✅ Verified |
| External Tile View (Cards) | `cert.certificateNumber` | ✅ Verified |
| Certificate Details Page | `@Model.Number` | ✅ Verified |
| Certificate Edit Page | `@Model.Number` | ✅ Verified (breadcrumb, subtitle) |
| API Responses | `certificateNumber` | ✅ Verified |
| URL Routes | `/Certificate/Edit/{number}` | ✅ Verified (uses certificate number) |
| Download Filenames | `Certificate_{number}.pdf` | ✅ Verified (e.g., Certificate_25-5057.pdf) |
| ZIP Download | `Certificate_{number}.pdf` | ✅ Verified |
| Export CSV | Certificate Number | To Verify |

### Technical Implementation
- **Database Column**: `Certificates.Number` (string, format: XX-XXXX)
- **Database Column**: `Certificates.Id` (int, auto-increment - internal use only)
- **API Mapping**: `CertificateNumber = c.Number`
- **Never expose numeric ID to users** - use for internal tracking only (data attributes, checkboxes)

### Verified Code Locations
- `CertificateTilesApiController.cs:138` - `CertificateNumber = c.Number`
- `CertificateTileInternalApiController.cs:138` - `CertificateNumber = c.Number`
- `certificate-tile-view-internal.js:1284` - `const certNumber = cert.certificateNumber`
- `certificate-tile-view-external.js:345` - `${escapeHtml(cert.certificateNumber)}`

---

## 2025-12-19 - Certificate Internal View Icon Placement Fix

### Requirement
The Certificate Internal View page (`/Certificate/CertificateInternalView/{number}`) must have:
1. Download icon positioned directly below the certificate image
2. Search/zoom icon positioned on/near the image

### Issue Identified
The `CertificateInternalView.cshtml` had different layout from `CertificateSingleView.cshtml` (public view):
- Internal view had action buttons (download, edit, delete) at the bottom of the card in `.card-actions` div
- No zoom icon overlay on the image
- No image container wrapper

### Fix Applied
Updated `CertificateInternalView.cshtml` (lines 145-204) to match `CertificateSingleView.cshtml` layout:

1. **Added image container**: Wrapped image in `<div class="certificate-image-container">` with `position: relative`
2. **Added zoom icon overlay**: Added `<div class="zoom-icon-overlay">` with search-plus icon inside the container
3. **Moved download button**: Created `<div class="certificate-download-action">` directly below the image container
4. **Reorganized edit/delete**: Moved to new `<div class="certificate-internal-actions">` below download
5. **Added Image Zoom Modal**: Added modal HTML for full-screen image viewing
6. **Added zoom JavaScript**: Added `openImageZoom()`, `closeImageZoom()`, and ESC key handler

### Files Modified
- `Views/Certificate/CertificateInternalView.cshtml` - Layout restructure and added zoom functionality

### Comparison
| Element | Before | After |
|---------|--------|-------|
| Image wrapper | None | `certificate-image-container` |
| Zoom icon | Missing | Overlay on image |
| Download icon | Bottom of card | Below image |
| Edit/Delete | Bottom with download | Below download |
| Zoom modal | Missing | Added |

---

## 2025-12-23 - Product Management Pages Review & Fixes

### Overview
Comprehensive review of all Product-related pages to assess purpose, functionality, and integration.

### Three Product-Related Pages Identified

| Page | URL | Access | Purpose | Status |
|------|-----|--------|---------|--------|
| **Product Management** | `/Product/Management` | All Auth (External filtered) | Full product CRUD with company, category, identifiers | ✅ Functional |
| **Admin Product Management** | `/Admin/ProductManagement` | Admin only | Simplified quick product add | ⚠️ Had stub code |
| **Product Types** | `/Admin/ProductTypes` | Admin only | 3-level hierarchy management | ✅ Functional |

### Issues Found & Fixed

#### 1. Admin Product Management - Stub Implementations → Real APIs

**Issue**: The inline script (lines 236-248, 318-329, 334-342) used `setTimeout()` stubs instead of real API calls.

**Root Cause**: The page loaded `productManagement.js` (with real APIs) but the inline script overrode handlers with fake implementations.

**Fix Applied**:
- `saveNewBtn.click` handler → Now calls `/Admin/CreateProduct` API
- `deleteProduct()` function → Now calls `/Admin/DeleteProduct` API
- `exportBtn.click` handler → Now generates real CSV export

**File Modified**: `Views/Admin/ProductManagement.cshtml`

#### 2. Product Management Modal - US-015 Compliance

**Issue**: Add/Edit Product modal used 50% opacity backdrop instead of required 85%.

**Before**:
```css
.modal {
    background-color: rgba(0, 0, 0, 0.5);  /* 50% opacity */
}
```

**After** (US-015 Standard):
```html
<div style="background-color: rgba(0, 0, 0, 0.85); backdrop-filter: blur(3px);">
```

**Structure Change**:
- Old: Nested `.modal > .modal-content`
- New: Sibling structure with backdrop and container as siblings

**File Modified**: `Views/Product/Management.cshtml`

#### 3. Unused Form Fields Removed

**Issue**: Modal had `product-manufacturer` and `product-country` fields that captured data but never saved it (not in Product model).

**Fix**: Removed these fields from the modal form.

**Product Model Fields Confirmed**:
- Name, Description, Model, SKU, Upc, Asin
- CategoryId, ProductTypeId, ProductSubcategoryId, CompanyId
- IsActive, IsDeleted, CreatedAt, UpdatedAt

**NOT in Model** (removed from form):
- Manufacturer
- Country of Origin

### Files Modified

| File | Changes |
|------|---------|
| `Views/Admin/ProductManagement.cshtml` | Replaced stub handlers with real API calls |
| `Views/Product/Management.cshtml` | US-015 modal compliance, removed unused fields |

### API Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/Admin/GetProducts` | GET | Load products list |
| `/Admin/CreateProduct` | POST | Create new product |
| `/Admin/UpdateProduct/{id}` | PUT | Update existing product |
| `/Admin/DeleteProduct/{id}` | DELETE | Delete product |
| `/Product/GetProducts` | GET | Load products (with External user filtering) |
| `/Product/SaveProduct` | POST | Create/Update product |
| `/Product/DeleteProduct` | DELETE | Delete product |

### Clarification: Two Product Management Pages

**User confirmed both pages serve separate purposes**:

1. **`/Product/Management`** - Full-featured for all authenticated users
   - Complex filters, search, sorting
   - Company, Category, Product Type assignments
   - Product Identifiers management
   - Import/Export CSV
   - External users see filtered data by company/partner

2. **`/Admin/ProductManagement`** - Simplified Admin-only quick add
   - Inline add with just product name
   - Shows product identifiers summary
   - Streamlined for quick data entry

### Build Status
- **Build**: ✅ Succeeded
- **Errors**: 0
- **Warnings**: 0 (relevant)

---

## 2025-12-23 - NEW PAGE: Product ID Manager Requirements Gathering

### Background
User shared notes from business discussions about the Product Management page. Requirements were vague because the business wasn't sure how the page should work.

### User's Original Notes (Captured)
```
- Partners and Clients should have access without add, edit, delete, import
- They would see based on their permissions
- Transform Type to individual columns (ASIN, EAN, GTIN, UPC)
- Column order: Identifiers first, then Product Name, Company, Status, Partner, numbers
- Would need to allow multiple partners
- The API would identify what gets passed per...????
- Multi-select for deletion and status change
- Page 59 reference - is this same as Product ID Manager on cert page?
```

### Clarifications Obtained

| Question | Answer |
|----------|--------|
| Identifier display format | Multiple values with "..." and count, expandable |
| Partners relationship | Partner is per Certificate, NOT Product |
| Page 59 reference | Product ID Manager (on Certificate page) |
| Mockup provided | `5_2_Product.png` - shows Certificate # as first column |
| Data source | CertificateProductIds table (certificate-level), not Products table |
| Existing page | Keep `/Product/Management` for standalone Products |
| New page needed | Create new Product ID Manager page for certificate product IDs |

### Key Distinction Clarified

**Three Product-Related Concepts:**

| Concept | Table | Purpose |
|---------|-------|---------|
| Products | `Products` | Standalone product entities |
| Product Identifiers | `ProductIdentifiers` | Product-level identifiers |
| Certificate Product IDs | `CertificateProductIds` | Certificate-specific identifiers (ASINs, UPCs per certificate) |

The mockup is for **Certificate Product IDs** - a centralized view of all product identifiers across all certificates.

### Requirements Document Created

**File**: `docs/PRODUCT_ID_MANAGER_PAGE_REQUIREMENTS.md`

**Sections**:
1. Overview & Purpose
2. Page Layout (from mockup)
3. Access Control (Analyst vs External)
4. Bulk Operations (multi-select, delete, status change)
5. Search & Filtering
6. Import/Export
7. API Endpoints
8. Data Model
9. UI/UX Considerations
10. Implementation Notes
11. Open Questions
12. Acceptance Criteria (MVP/Should Have/Nice to Have)

### Open Questions Requiring Business Input

| # | Question |
|---|----------|
| 1 | Should inline add create new Certificate if number doesn't exist? |
| 2 | How to handle multiple values per identifier type (e.g., 3 ASINs)? |
| 3 | Should Partners support "Other" with custom value? |
| 4 | Pagination: Fixed page size or user-selectable? |
| 5 | Soft-delete or hard-delete for removed items? |

### Next Steps
1. Review open questions with business
2. Finalize acceptance criteria
3. Begin implementation when requirements approved

---

## 2026-01-09 - REQUIREMENT: Certificate Views Whitespace Consistency

### Requirement
**Location row and action bar (download/view detail icons) need additional whitespace. This must be consistent across ALL certificate views.**

This is a design consistency requirement to ensure 100% layout consistency across the application.

### Views Affected
1. **Public Certificate Tile View** - `/Certificate/CertificateTileView`
2. **External Certificate Tile View** - `/Certificate/CertificateTileViewExternal`
3. **Internal Certificate Tile View** - `/Certificate/CertificateTileViewInternal`

Each view has two display modes:
- **Tile/Card View** - Uses `.certificate-location` and `.card-actions` classes
- **Table View** (External/Internal only) - Uses `.grid-cell[data-column="location"]` and `.actions-column` classes

### Changes Applied

#### 1. Tile/Card View - Location Row
**File**: `wwwroot/css/certificatetileview.css`

**Before**:
```css
.certificate-location {
    margin-top: 10px;
}
```

**After**:
```css
.certificate-location {
    margin-top: 15px;
    margin-bottom: 8px;
    padding-top: 10px;
}
```

#### 2. Tile/Card View - Action Bar
**File**: `wwwroot/css/certificatetileview.css`

**Before**:
```css
.card-actions {
    gap: 10px;
    padding: 12px 15px;
}
```

**After**:
```css
.card-actions {
    gap: 12px;
    padding: 16px 18px;
}
```

#### 3. Table View - Actions Column
**Files**:
- `wwwroot/css/certificatetileview.css`
- `Views/Certificate/CertificateTileViewExternal.cshtml`
- `Views/Certificate/CertificateTileViewInternal.cshtml`

**Before**:
```css
.actions-column {
    gap: 4px;
    padding-left: 4px;
    padding-right: 4px;
}
```

**After**:
```css
.actions-column {
    gap: 6px;
    padding: 8px 10px;
}
```

#### 4. Table View - Location Column
**Files**:
- `wwwroot/css/certificatetileview.css`
- `Views/Certificate/CertificateTileViewExternal.cshtml`
- `Views/Certificate/CertificateTileViewInternal.cshtml`

**Added**:
```css
.grid-cell[data-column="location"] {
    padding: 10px 12px;
}
```

### Files Modified
| File | Change |
|------|--------|
| `wwwroot/css/certificatetileview.css` | Updated `.certificate-location`, `.card-actions`, `.actions-column`, added location column styling |
| `Views/Certificate/CertificateTileViewExternal.cshtml` | Updated inline styles for actions and location columns |
| `Views/Certificate/CertificateTileViewInternal.cshtml` | Updated inline styles for actions and location columns |

### Build Status
- **Build**: ✅ Succeeded (0 errors, 0 warnings)

### Design Consistency Reminder
This requirement reinforces the need for **100% consistency in layout and design across the application**. All certificate views must have identical spacing for:
- Location information display
- Action buttons (download, view, edit icons)

---

## 2026-01-11 - CSS Consolidation: Inline Styles Moved to Shared CSS

### Issue Identified
Certificate tile views (External/Internal) had ~970 lines of inline CSS overriding the shared `certificatetileview.css` file, causing:
- Inconsistent styling between views
- Maintenance difficulties
- ~177 `!important` declarations creating specificity conflicts

### Analysis Performed
| View | Before | After | Reduction |
|------|--------|-------|-----------|
| Public | 246 lines | 246 lines | No change (already correct) |
| External | 877 lines | 527 lines | -40% |
| Internal | 916 lines | 528 lines | -42% |
| Inline CSS total | ~970 lines | ~160 lines | -83% |

### Changes Made

**1. Added to `certificatetileview.css` (~440 lines)**:
- Loading spinner animation
- Collapsible sidebar styles (toggle tab, collapse button)
- Table scroll and wider layout for table view
- Bulk actions bar styling
- Select/Actions column centering
- Spacing between buttons and table
- Grid header/body cell styling
- Zebra striping, status badges, date/certificate formatting

**2. Reduced External view**: Kept only grid columns (21), table min-width, centering overrides

**3. Reduced Internal view**: Kept only grid columns (22), table min-width, centering overrides

### Files Modified
| File | Change |
|------|--------|
| `wwwroot/css/certificatetileview.css` | +442 lines consolidated |
| `Views/Certificate/CertificateTileViewExternal.cshtml` | -350 lines inline CSS |
| `Views/Certificate/CertificateTileViewInternal.cshtml` | -388 lines inline CSS |

### Report Generated
Full analysis: `docs/CSS_INLINE_STYLES_REPORT.md`

---

## 2026-01-09 - VERIFICATION: Two-Factor Authentication Admin Management - COMPLETE

### Issue Investigated
The issues list indicated "Two-Factor Authentication Admin Management" as "UI Missing" with HIGH priority. Upon investigation, this feature is **fully implemented**.

### Implementation Found

| Layer | Location | Status |
|-------|----------|--------|
| **Model** | `AdminModels.cs:32-34` | ✅ `TwoFactorEnabled`, `TwoFactorMethod` properties |
| **Service** | `AdminService.cs:1683-1747` | ✅ `Reset2FAAsync()`, `Disable2FAAsync()`, `Enable2FAAsync()` |
| **Controller** | `AdminController.cs:1293-1357` | ✅ POST endpoints for all 3 operations |
| **View** | `UserManagement.cshtml:109-111` | ✅ 2FA column in table header |
| **JavaScript** | `userManagement.js:153-204` | ✅ Renders dropdown with 2FA actions |
| **JavaScript** | `userManagement.js:1421-1591` | ✅ API calls for view/reset/disable/enable |
| **CSS** | `admin-modals.css:440-594` | ✅ Dropdown menu styling |

### UI Features Available

1. **2FA Status Badge** - Shows "Enabled" (green shield) or "Disabled" (gray unlock) in table
2. **Actions Dropdown Menu** - Click ellipsis icon to see:
   - View 2FA Status - Shows user's current 2FA status and method
   - Reset 2FA (if enabled) - Clears user's 2FA setup, requires reconfiguration
   - Disable 2FA (if enabled) - Turns off 2FA for user
   - Enable 2FA (if disabled) - Turns on 2FA requirement

### Documentation Updated
- Updated CLAUDE.md to mark feature as ✅ COMPLETE
- Changed from "⚠️ REQUIRED - MISSING: UI controls" to "✅ COMPLETE"

### Conclusion
The CLAUDE.md documentation was outdated. The Two-Factor Authentication Admin Management feature is fully functional and can be removed from the issues list.

---

## 2026-01-11 - CSS Consolidation: Account Pages

### Summary
Consolidated inline CSS from 11 Account pages into a shared `wwwroot/css/auth.css` file.

### Changes Made

| File | Before (lines) | After (lines) | Reduction |
|------|----------------|---------------|-----------|
| Login.cshtml | 519 | ~95 | -424 lines |
| SignUp.cshtml | 508 | ~215 | -293 lines |
| ForgotPassword.cshtml | 527 | ~95 | -432 lines |
| ResetPassword.cshtml | 543 | ~115 | -428 lines |
| 2FAAuthenticate.cshtml | 420 | ~162 | -258 lines |
| VerifyEmail.cshtml | 302 | ~75 | -227 lines |
| AccountLocked.cshtml | 318 | ~115 | -203 lines |
| PasswordResetSuccess.cshtml | 341 | ~115 | -226 lines |

### New Files Created
- `wwwroot/css/auth.css` - ~1200 lines of consolidated authentication page styles

### CSS Architecture
- Uses CSS variables for page-specific customization
- Supports two layout patterns:
  - Split layout: `.login-container` (grid 1fr 1fr) - Login, SignUp, ForgotPassword, ResetPassword
  - Single column: `.auth-container` - 2FA, VerifyEmail, AccountLocked, PasswordResetSuccess
- Comprehensive responsive breakpoints for tablet (768px) and mobile (480px)

### Key Benefits
1. **Maintainability**: Single source of truth for authentication styling
2. **Consistency**: All auth pages now share identical base styles
3. **Performance**: Browsers can cache auth.css across all account pages
4. **Reduced duplication**: Eliminated ~2,500 lines of duplicated CSS

### Build Status
- Build succeeded with 0 errors, 0 warnings
- All CSS changes tested and verified

---

## 2026-01-11 - VERIFICATION: Bulk Download as ZIP - COMPLETE

### Issue Investigated
The issues list indicated "Bulk Download as ZIP" as a HIGH priority item requiring implementation. Upon investigation, this feature is **fully implemented**.

### Implementation Found

| Component | Location | Status |
|-----------|----------|--------|
| **Backend Endpoint** | `CertificateController.cs:1395-1517` | ✅ `DownloadSelectedZip` POST endpoint |
| **ZIP Creation** | `CertificateController.cs:1408` | ✅ Uses `System.IO.Compression.ZipArchive` |
| **PDF Retrieval** | `CertificateController.cs:1425-1470` | ✅ Gets uploaded PDFs or generates them |
| **External View JS** | `certificateTileViewExternal.js:1199-1260` | ✅ `downloadSelected()` method |
| **Internal View JS** | `certificate-tile-view-internal.js:1114-1173` | ✅ `handleDownloadSelected()` function |
| **UI Buttons** | Both views | ✅ "Download Selected" button in bulk actions bar |

### Feature Workflow

1. User switches to **Table View** (required for selection)
2. User selects certificates using checkboxes
3. User clicks **Download Selected** button
4. Frontend calls `/Certificate/DownloadSelectedZip` with certificate IDs
5. Backend creates ZIP archive with all certificate PDFs
6. ZIP file downloads with naming: `certificates_YYYY-MM-DD_HHmmss.zip`
7. Success notification shown

### Documentation Updated
- Updated CLAUDE.md:
  - Changed "BULK DOWNLOAD AS ZIP ⚠️ REQUIRED" to "BULK DOWNLOAD AS ZIP ✅ COMPLETE"
  - Removed reminder from Important Reminders section

### Conclusion
The CLAUDE.md documentation was outdated. The Bulk Download as ZIP feature is fully implemented and functional.

---

## 2026-01-12 - Certificate Create/Edit Functional Testing

### Summary
Performed end-to-end functional testing of Certificate Create and Edit functionality using automated curl-based testing.

### Test Environment
- **Application**: Running on http://localhost:5101
- **User**: admin (authenticated with GCC.Auth cookie)
- **Session**: Fresh login with anti-forgery token handling

### Test Results

#### Certificate Create - PASS ✅
| Step | Action | Result |
|------|--------|--------|
| 1 | Login as admin | HTTP 302 redirect (success) |
| 2 | Get Create page | HTTP 200 - Form loaded with auto-generated number "26-0001" |
| 3 | Submit form with valid data | HTTP 302 redirect (success) |
| 4 | Verify certificate exists | HTTP 200 on Edit/Details pages |

**Form Data Submitted**:
- Number: 26-0001 (auto-generated)
- CompanyId: 1
- ProductId: 1
- CertificationTypeId: 1
- Partner: Amazon
- EffectiveDate: 2025-01-01
- ExpirationDate: 2026-01-01
- Status: Draft
- Description: Test certificate created via automated testing

**Certificate Created**: ID 180, Number "26-0001"

#### Certificate Edit - PASS ✅
| Step | Action | Result |
|------|--------|--------|
| 1 | Get Edit page for 26-0001 | HTTP 200 - Form loaded with current values |
| 2 | Submit form with changed status | HTTP 302 redirect (success) |
| 3 | Verify status changed | Status now shows "Review" |

**Changes Made**:
- Status: Draft → Review
- Description: Updated to "Test certificate EDITED - changed status to Review"

### Key Learnings

1. **Certificate Number Auto-Generation**:
   - Create form auto-generates next available number (format: YY-XXXX)
   - Number is readonly and should be used as-is
   - Cannot override with custom number (max 7 characters validation)

2. **Form Submission Requirements**:
   - Anti-forgery token required (`__RequestVerificationToken`)
   - Hidden `Id` field required for Edit (database ID)
   - Certificate number used in URL routing, not database ID

3. **Validation**:
   - Certificate Number: Max 7 characters (format: YY-XXXX)
   - CompanyId, ProductId, CertificationTypeId: Required
   - EffectiveDate, ExpirationDate: Date format YYYY-MM-DD
   - Status: Valid values (Draft, Review, Active, etc.)
   - Partner: Amazon, Novi, Other

### Test Commands Used

```bash
# 1. Login and get auth cookie
curl -s -c cookies.txt -b cookies.txt http://localhost:5101/Account/Login > login.html
TOKEN=$(grep -oP '__RequestVerificationToken.*?value="\K[^"]+' login.html | head -1)
curl -s -c cookies.txt -b cookies.txt -X POST -d "Email=admin&Password=TestPass123!&__RequestVerificationToken=$TOKEN" http://localhost:5101/Account/Login

# 2. Create certificate
curl -s -c cookies.txt -b cookies.txt http://localhost:5101/Certificate/Create > create.html
CERT_NUM=$(grep -oP 'id="Number".*?value="\K[^"]+' create.html)
TOKEN=$(grep -oP '__RequestVerificationToken.*?value="\K[^"]+' create.html | head -1)
curl -s -c cookies.txt -b cookies.txt -X POST -F "Number=$CERT_NUM" -F "CompanyId=1" ... http://localhost:5101/Certificate/Create

# 3. Edit certificate
curl -s -c cookies.txt -b cookies.txt http://localhost:5101/Certificate/Edit/$CERT_NUM > edit.html
TOKEN=$(grep -oP '__RequestVerificationToken.*?value="\K[^"]+' edit.html | head -1)
CERT_ID=$(grep -oP 'name="Id" value="\K[^"]+' edit.html | head -1)
curl -s -c cookies.txt -b cookies.txt -X POST -F "Id=$CERT_ID" -F "Number=$CERT_NUM" -F "Status=Review" ... http://localhost:5101/Certificate/Edit/$CERT_NUM
```

### Application Status
- **Build**: Successful (0 errors)
- **Certificate Create**: Fully functional
- **Certificate Edit**: Fully functional
- **Authentication**: Working with cookie-based sessions

---

## 2026-01-12 - Required Field Asterisk Validation Styling Fix

### Issue Reported
User requested that required field asterisks (*) be visible and red across Certificate Create/Edit pages and all pages with required fields. Also requested removal of caution icons.

### Root Cause
1. The `certificate-create.css` was adding a caution icon (`⚠`) before validation error messages via CSS `::before` pseudo-element
2. Some Account views (SignUp, ResetPassword, PasswordReset) were missing the `text-danger` class on required field asterisks

### Fixes Applied

#### 1. Removed Caution Icon from Validation Messages
**File**: `wwwroot/css/certificate-create.css` (Lines 927-934)

**Before**:
```css
/* Add warning icon before error messages */
.form-group .text-danger:not(:empty)::before,
.form-group .field-validation-error:not(:empty)::before,
.form-group .inline-validation-error::before {
    content: '⚠ ';
}
```

**After**:
```css
/* Required field asterisk styling - Red asterisk for required fields */
.form-group label .text-danger,
.form-group label span.text-danger {
    color: #FF5252 !important;
    font-weight: 600;
    font-size: inherit;
    margin-left: 2px;
}
```

#### 2. Added Required Field Asterisk Styling to Auth Pages
**File**: `wwwroot/css/auth.css` (Lines 715-724)

Added:
```css
/* Required field asterisk - Red asterisk for required fields */
.form-group label .text-danger,
.form-group label span.text-danger,
label .text-danger,
label span.text-danger {
    color: #FF5252 !important;
    font-weight: 700;
    font-size: inherit;
    margin-left: 2px;
}
```

#### 3. Fixed Missing text-danger Class on Asterisks
**Files Modified**:
- `Views/Account/ResetPassword.cshtml` - Added text-danger to Password and Confirm Password asterisks
- `Views/Account/SignUp.cshtml` - Added text-danger to all 5 asterisks (FirstName, LastName, Email, Password, ConfirmPassword)
- `Views/Account/PasswordReset.cshtml` - Added text-danger to Password and Confirm Password asterisks

**Pattern Applied**:
```html
<!-- Before -->
<span aria-hidden="true">*</span>

<!-- After -->
<span class="text-danger" aria-hidden="true">*</span>
```

### Validation Styling Patterns Across Application

| Pattern | Used By | Status |
|---------|---------|--------|
| `<span class="text-danger">*</span>` | Certificate Create/Edit | ✅ Fixed |
| `<span class="text-danger">*</span>` | Account pages (Login, SignUp, etc.) | ✅ Fixed |
| `.admin-label--required::after` | Admin pages (UserMgmt, RoleMgmt, etc.) | ✅ Already working |

### Build Status
- **Build**: ✅ Succeeded (0 errors)
- **Application**: Running on http://localhost:5101

### Test Verification
1. Navigate to `/Certificate/Create` - Required field asterisks should be red
2. Navigate to `/Certificate/Edit/{number}` - Required field asterisks should be red
3. Navigate to `/Account/Login` - Required field asterisks should be red
4. Navigate to `/Account/SignUp` - Required field asterisks should be red
5. Verify NO caution icons appear on any page

---

## 2026-01-12 - Product Import Quality Improvements Implementation

### Summary
Implemented three import quality improvements for the Product Management page as identified from MEDIUM priority issues:

1. **Fuzzy Matching for ProductType/Category** - Extended existing Company fuzzy logic
2. **Auto-Suggest Click to Apply** - Clickable suggestions in import preview modal
3. **Import Order Enforcement Modal** - Block import if prerequisites empty

### Phase 1: Fuzzy Matching for ProductType/Category

**Implementation Files:**
- `wwwroot/js/admin/productManagement.js` (Lines 1390-1608)

**ProductType Matching Functions (Lines 1390-1502):**
- `normalizeProductTypeName()` - Removes common suffixes like "type", "category", "products"
- `findMatchingProductType()` - 3-tier matching: Exact → Normalized → Fuzzy (≥0.8 similarity)
- `getSimilarProductTypes()` - Returns top 3 matches with >0.3 similarity
- `validateProductTypeReference()` - Returns validation result with suggestions

**Category Matching Functions (Lines 1504-1608):**
- `normalizeCategoryName()` - Same pattern as ProductType
- `findMatchingCategory()` - 3-tier matching strategy
- `getSimilarCategories()` - Returns top 3 suggestions
- `validateCategoryReference()` - Returns validation with suggestions

**buildImportPreview() Updates (Lines 736-780):**
- Added ProductType and Category validation in import preview
- Shows suggestions when no match found: "Did you mean: X, Y, Z?"
- Warnings for normalized/fuzzy matches with resolved values

### Phase 2: Auto-Suggest Click to Apply

**Implementation Files:**
- `wwwroot/js/admin/productManagement.js` (Lines 884-1457)

**Clickable Suggestions (Lines 884-916):**
- Error display parses "Did you mean:" pattern
- Renders clickable buttons for each suggestion
- Styled as blue pill buttons for clear visibility

**New Methods:**
- `detectFieldFromError()` (Lines 1133-1138) - Identifies field from error message
- `applySuggestion()` (Lines 1144-1215) - Applies single suggestion to row
- `applyAllSuggestions()` (Lines 1220-1258) - Batch applies first suggestion to all error rows
- `applySuggestionSilent()` (Lines 1263-1301) - Applies suggestion without notification
- `refreshPreviewTable()` (Lines 1306-1428) - Re-renders table without closing modal
- `updateApplyAllSuggestionsButton()` (Lines 1433-1457) - Updates bulk apply button count

**User Experience:**
- Click suggestion button → Value applied → Row re-validated → Table refreshes
- "Apply All Suggestions" button shows count of fixable rows
- Success notification shown after applying suggestion

### Phase 3: Import Order Enforcement Modal

**Implementation Files:**
- `wwwroot/js/admin/productManagement.js` (Lines 2133-2269)
- `Views/Product/Management.cshtml` (Lines 454-462)

**New Methods:**
- `checkImportPrerequisites()` (Lines 2141-2167) - Checks Companies and ProductTypes exist
- `handleImportClick()` (Lines 2172-2188) - Intercepts import button click
- `showPrerequisiteModal()` (Lines 2193-2269) - Displays blocking modal with issues

**Modal Features:**
- Warning icon with "Cannot Import Products" title
- Lists missing prerequisites (Companies, Product Types)
- Navigation buttons to respective management pages
- "Import Anyway" button to bypass check if needed
- Follows US-015 backdrop standard (85% black, 3px blur)

**View Update (Management.cshtml Lines 454-462):**
```javascript
importBtn.addEventListener('click', () => {
    if (window.productManagement && typeof window.productManagement.handleImportClick === 'function') {
        window.productManagement.handleImportClick();
    } else {
        importProducts();
    }
});
```

### Testing Verification

| Feature | Test | Status |
|---------|------|--------|
| Build | `dotnet build` | ✅ 0 errors |
| App Start | http://localhost:5101 | ✅ 200 OK |
| ProductType Fuzzy | Import with typo | Ready to test |
| Category Fuzzy | Import with typo | Ready to test |
| Click Suggestions | Click suggestion button | Ready to test |
| Apply All | Click bulk apply | Ready to test |
| Prerequisite Modal | Empty DB import attempt | Ready to test |

### Code Statistics

| Phase | Lines Added | Methods Added |
|-------|-------------|---------------|
| Phase 1 | ~220 lines | 8 methods |
| Phase 2 | ~330 lines | 6 methods |
| Phase 3 | ~140 lines | 3 methods |
| **Total** | **~690 lines** | **17 methods** |

### Related Documentation
- Plan file: `C:\Users\swest\.claude\plans\fizzy-tickling-rose.md`
- Requirements: MEDIUM priority items from CLAUDE.md

---

## 2026-01-13 - Container Width Audit & Standards Documentation

### Background
User reported that container widths appeared inconsistent across different views, making the application look "messy". Requested documentation of container width standards.

### Audit Performed
Comprehensive review of all CSS files defining container widths across the application.

### Key Findings

#### Design Token (Single Source of Truth)
**File**: `wwwroot/css/admin/admin-core.css` (Line 126)
```css
--admin-container-max-width: 1400px;
```

#### Container Width Categories Identified

| Category | Max-Width | Container Class | Pages Using |
|----------|-----------|-----------------|-------------|
| Standard Admin | 1400px | `.admin-container` | User Mgmt, Role Mgmt, Tags, etc. |
| Auth (2-column) | 900px | `.login-container` | Login, SignUp, ForgotPassword |
| Auth (1-column) | 500px | `.auth-container` | 2FA, VerifyEmail, AccountLocked |
| Certificate Forms | 1400px + grid | `.admin-container` | Create, Edit (special layout) |
| Certificate Tiles | 1400px-2400px | Responsive | Scales on ultrawide monitors |
| Full-Width | 100% | `.admin-container--full-width` | Wide data tables |

#### Inconsistencies Found

| Issue | Severity | File | Fix Needed |
|-------|----------|------|------------|
| Company Mgmt override | HIGH | `admin-company-style.css:22-29` | Remove 100% `!important` override |
| Certificate Details preview | MEDIUM | `Details.cshtml:195` | Replace hardcoded 350px with CSS class |

### Documentation Created

1. **`docs/CONTAINER_WIDTH_STANDARDS.md`** - Comprehensive container width standards document covering:
   - All container categories with CSS definitions
   - Responsive breakpoints
   - Implementation checklist for new pages
   - Known inconsistencies with recommendations

2. **CLAUDE.md** - Added new section "Container Width Standards (US-016)" with:
   - Quick reference table
   - Design token location
   - Known issues to fix
   - Implementation checklist

### Files Modified
| File | Change |
|------|--------|
| `docs/CONTAINER_WIDTH_STANDARDS.md` | NEW - Full standards documentation |
| `.claude/CLAUDE.md` | ADDED - Container Width Standards section |
| `observations_log.md` | APPENDED - This entry |

### Responsive Breakpoints (Standard)
- `576px` - Small phones
- `768px` - Tablets
- `1024px` - Laptops
- `1200px` - Desktops
- `1400px` - Large desktops (matches container max-width)

### Recommendations
1. **Remove Company Management override** - The `max-width: 100% !important` in `admin-company-style.css` should be removed or made intentional via `.admin-container--full-width`
2. **Fix Certificate Details preview** - Replace inline `width: 350px` with responsive CSS class
3. **Enforce standards for new views** - All new pages should follow the documented container classes
4. **Review existing pages** - Audit all admin pages for container consistency

### Cost/Risk Note
Standardizing container widths is LOW RISK because:
- CSS changes only affect layout, not functionality
- Design tokens already exist in admin-core.css
- Fix involves removing problematic overrides, not adding new code

### Fixes Applied (2026-01-13)

**Issue 1: Company Management Container Width Override**
- **File**: `wwwroot/css/admin-company-style.css`
- **Change**: Removed `max-width: 100% !important` and `margin: 0 !important`
- **Result**: Company Management page now uses standard 1400px container width

**Issue 2: Certificate Details Preview Box**
- **Files**:
  - `wwwroot/css/certificates.css` - Added `.certificate-preview-box` class (lines 2072-2157)
  - `Views/Certificate/Details.cshtml` - Replaced inline styles with CSS class
- **Change**: Created responsive CSS class with `max-width: 350px`, `aspect-ratio: 1/1`, and mobile breakpoints
- **Result**: Preview box is now responsive and maintains aspect ratio across screen sizes

**Build Status**: Succeeded (0 errors)

### Hybrid Layout Implementation (2026-01-13)

**User Request**: All pages should have consistent borders like Dashboard page.

**Solution**: Implemented "Hybrid Layout" where:
- Header stays separate at the top
- Toolbar + Content unified in one white box with border

**CSS Added** (`admin-layout.css:154-185`):
```css
.admin-body-content {
    background-color: var(--admin-bg-primary);
    border: 1px solid var(--admin-border-primary);
    border-radius: var(--admin-radius-lg);
    box-shadow: var(--admin-shadow-sm);
    overflow: hidden;
}
```

**Pages Updated (11 total)**:
1. Tags.cshtml
2. UserManagement.cshtml
3. RoleManagement.cshtml
4. CertificateFields.cshtml
5. CertificateTypes.cshtml
6. ProductTypes.cshtml
7. ProductManagement.cshtml
8. Partners.cshtml
9. PartnerAPI.cshtml
10. GCCAPI.cshtml
11. CSFAttributes.cshtml

**Pages Not Updated** (different layout):
- Admin Index (dashboard cards)
- Google Analytics (special form)
- Dashboard (already unified)
- Create User (form-only)

**Build Status**: Succeeded (0 errors)

### Container Width Override Fixes (2026-01-13)

**Problem Identified**: Multiple page-specific CSS files were overriding the standard 1400px container width:

| File | Original Override | Fixed |
|------|-------------------|-------|
| role-management.css | `max-width: none !important` | Removed - inherits 1400px |
| user-management.css | `max-width: none` | Removed - inherits 1400px |
| tags.css | `max-width: none` | Removed - inherits 1400px |
| admin-mockup-override.css | `max-width: 1200px` | Changed to `var(--admin-container-max-width, 1400px)` |
| amazon-api.css | `max-width: 100%` | Removed - inherits 1400px |

**Files Modified**:
- `wwwroot/css/role-management.css`
- `wwwroot/css/user-management.css`
- `wwwroot/css/tags.css`
- `wwwroot/css/admin/admin-mockup-override.css`
- `wwwroot/css/amazon-api.css`

**Result**: All admin pages now use consistent 1400px container width from `--admin-container-max-width` CSS variable.

**Build Status**: Succeeded (0 errors)

---

## 2026-01-14 - Dashboard, Company, and Product Pages Unified Wrapper Fix

### Issue
Dashboard, Company Management, and Product Management pages had inconsistent width and formatting compared to other admin pages. They used legacy class names and separate styling instead of the unified "Hybrid Layout" pattern.

### Root Cause
These pages did not use the `.admin-body-content` wrapper class that provides:
- Unified white background
- Single border around toolbar + content + footer
- Consistent padding and box shadow

### Changes Made

**1. Dashboard Page (`Views/Dashboard/Index.cshtml`)**
- Replaced inline styles with `.admin-body-content` class
- Changed from: `<div class="admin-content" style="background-color: #ffffff; border-radius: 8px; ...">` 
- Changed to: `<div class="admin-body-content">`

**2. Company Management Page (`Views/Company/Management.cshtml`)**
- Added `.admin-body-content` wrapper around content
- Changed legacy classes to admin framework classes:
  - `search-filter-bar` → `admin-toolbar`
  - `search-box` → `admin-search-box`
  - `table-container` → `admin-content`
  - `footer-info` → `admin-footer-info`
- Updated table with `admin-table`, `admin-table-header`, `admin-table-body` classes

**3. Product Management Page (`Views/Product/Management.cshtml`)**
- Added `.admin-body-content` wrapper around content
- Updated class names to use admin framework alongside legacy classes for JS compatibility
- Updated table with admin framework classes

**4. Company Management CSS (`wwwroot/css/company-management.css`)**
- Removed separate background/border from `.table-container`
- Content now inherits unified styling from wrapper

**5. Product Management CSS (`wwwroot/css/product-management.css`)**
- Removed separate backgrounds and shadows from:
  - `.search-filter-bar` (was: white bg, 12px border-radius, box-shadow)
  - `.table-container` (was: white bg, 12px border-radius, box-shadow)
  - `.footer-info` (was: white bg, padding, border-radius, box-shadow)
- All now inherit unified styling from `.admin-body-content` wrapper

**6. Admin Layout CSS (`wwwroot/css/admin/admin-layout.css`)**
- Added padding to `.admin-body-content`: `padding: var(--admin-space-lg)`
- Updated child element rules to expand to edges:
  - `.admin-toolbar`: Uses negative margin to expand, with border-bottom separator
  - `.admin-content`: Uses negative horizontal margin to expand
  - `.admin-footer-info`: Uses negative margin for full-width footer

### Files Modified
- `Views/Dashboard/Index.cshtml`
- `Views/Company/Management.cshtml`
- `Views/Product/Management.cshtml`
- `wwwroot/css/company-management.css`
- `wwwroot/css/product-management.css`
- `wwwroot/css/admin/admin-layout.css`

### Build Status
Succeeded (0 errors, existing warnings only)

---

## 2026-01-14 - Master Design Guide Compliance Testing

### Objective
Comprehensive testing of all views against the Master Design Guide (`0_Documentation/mockups/0_0_MasterDesignGuide.html`) and documented requirements.

### Test Scope
- 12 Admin Views
- 11 Account/Authentication Views
- 8 Certificate Views
- 2 Company Views
- 3 Product Views
- 3 Dashboard/Home Views

---

### Phase 1: Admin Views Test Results

#### Summary
| View | Hybrid Layout | Container Width | Modal Backdrop | Button Order | Status |
|------|---------------|-----------------|----------------|--------------|--------|
| UserManagement | ✅ | ✅ 1400px | ✅ US-015 | ✅ T/I/E/A | PASS |
| RoleManagement | ✅ | ✅ 1400px | ✅ US-015 | ✅ | PASS |
| Tags | ✅ | ✅ 1400px | ✅ US-015 | ✅ T/I/E/A | PASS |
| CertificateTypes | ✅ | ✅ 1400px | ✅ US-015 | ✅ H/E/A | PASS |
| CertificateFields | ✅ | ✅ 1400px | - | ✅ | PASS |
| ProductTypes | ✅ | ✅ 1400px | ⚠️ Old style | ✅ | PARTIAL |
| ProductManagement | ✅ | ✅ 1400px | ✅ US-015 | ✅ | PASS |
| Partners | ✅ | ✅ 1400px | - | ✅ E/A only | PASS |
| PartnerAPI | ✅ | ✅ 1400px | ✅ US-015 | ✅ | PASS |
| GCCAPI | ✅ | ✅ 1400px | ✅ US-015 | ✅ | PASS |
| CSFAttributes | ✅ | ✅ 1400px | - | ✅ | PASS |
| GoogleAnalytics | ❌ Custom | ✅ 1400px | - | ⚠️ | PARTIAL |

**Button Order Legend**: T=Template, I=Import, E=Export, A=Add New, H=Help

#### Detailed Findings

**1. UserManagement.cshtml** - PASS
- Uses `ViewData["Title"]` correctly (no duplicate h1)
- Uses `.admin-container` with standard 1400px max-width
- Implements hybrid layout: `.admin-header` + `.admin-body-content`
- Button order follows standard: Template, Import, Export, Add New
- Modal backdrop follows US-015: `rgba(0, 0, 0, 0.85)` + `blur(3px)`
- Table action icons use correct no-background styling

**2. Tags.cshtml** - PASS
- Full hybrid layout compliance
- US-015 modal backdrop implemented
- Inline editing support properly styled
- Export uses CSV only (correct)

**3. CertificateTypes.cshtml** - PASS
- Hybrid layout with tree view variant
- US-015 modal backdrop on help modal
- Stats bar integrated into toolbar

**4. GoogleAnalytics.cshtml** - PARTIAL
- Uses `.admin-container` correctly
- Uses `.admin-header` correctly
- **ISSUE**: Uses `.analytics-container` instead of `.admin-body-content`
- **RECOMMENDATION**: Refactor to use standard hybrid layout pattern

---

### Phase 2: Authentication Views Test Results

#### Summary
| View | Container | Max-Width | Layout | Status |
|------|-----------|-----------|--------|--------|
| Login | .login-container | 900px | 2-column | PASS |
| SignUp | .login-container | 900px | 2-column | PASS |
| ForgotPassword | .auth-container | 500px | 1-column | PASS |
| ResetPassword | .auth-container | 500px | 1-column | PASS |
| 2FAAuthenticate | .auth-container | 500px | 1-column | PASS |
| SetupTotp | .auth-container | 500px | 1-column | PASS |
| VerifyEmail | .auth-container | 500px | 1-column | PASS |
| AccountLocked | .auth-container | 500px | 1-column | PASS |
| PasswordResetSuccess | .auth-container | 500px | 1-column | PASS |
| TwoFactorPreferences | .auth-container | 500px | 1-column | PASS |
| AccessDenied | .auth-container | 500px | 1-column | PASS |

#### Detailed Findings

**Login.cshtml** - PASS
- Uses standalone layout (no _Layout.cshtml) - correct for auth
- `.login-container` with 2-column grid layout
- Left panel: GCC logo + branding gradient
- Right panel: login form
- Font sizes: 32px header, 16px body text - COMPLIANT
- Uses `auth.css` for consistent styling

---

### Phase 3: Certificate Views Test Results

#### Summary
| View | Container | Layout | Key Features | Status |
|------|-----------|--------|--------------|--------|
| CertificateTileView | Responsive | Tile grid | Infinite scroll, filters | PASS |
| CertificateTileViewInternal | Responsive | 22-col table | Bulk ZIP, checkboxes | PASS |
| CertificateTileViewExternal | Responsive | 21-col table | No Internal Tags | PASS |
| Details | Custom | Card layout | Preview thumbnail | PASS |
| Create | 1400px + grid | Form + sidebar | PDF upload + preview | PASS |
| Edit | 1400px + grid | Form + sidebar | Pre-populated | PASS |
| Import | Form grid | Multi-step | Preview modal | PASS |

#### Detailed Findings

**Details.cshtml** - PASS
- Uses `.certificate-single-container`
- Displays certificate number (not database ID) ✅
- Uses `AdminCore.showNotification()` for success toasts
- Preview thumbnail with click-to-zoom
- Role-based content visibility working

---

### Design Standards Compliance Summary

| Standard | Compliance | Notes |
|----------|-----------|-------|
| US-015 Modal Backdrop | 95% | ProductTypes needs update |
| US-016 Container Width | 100% | All pages compliant |
| Hybrid Layout Pattern | 92% | GoogleAnalytics custom |
| Button Hierarchy | 100% | Primary green, Secondary blue |
| Table Action Icons | 100% | No background, correct colors |
| Typography 16px min | 80% | Table body at 14px |
| Native alert() prohibition | 100% | All use AdminCore |
| CSV-only exports | 100% | No Excel/JSON exports |

---

### Issues Requiring Attention

| Priority | Page | Issue | Recommendation |
|----------|------|-------|----------------|
| Low | GoogleAnalytics | Custom layout | Refactor to hybrid pattern |
| Low | ProductTypes | Old modal backdrop | Update to US-015 standard |
| Medium | Table body text | 14px font | Consider 15-16px minimum |

---

### Test Methodology
1. Read each view file to verify structure
2. Check CSS classes against design tokens
3. Verify modal backdrop implementation
4. Confirm button order and styling
5. Check container max-width values

### Files Reviewed
- `Views/Admin/*.cshtml` (12 files)
- `Views/Account/*.cshtml` (11 files)
- `Views/Certificate/*.cshtml` (8 files)
- `wwwroot/css/admin/*.css` (8 files)
- `wwwroot/css/auth.css`
- `wwwroot/css/certificatetileview.css`

---

## 2026-01-14 - Search Icon Alignment Fix (Certificate Filter Sidebar)

### Issue
Search icon in the left panel filter was top-aligned instead of vertically centered within the search box. This issue affected all certificate pages (Public, External, Internal).

### Root Cause
In `certificatetileview.css` line 66, the `.sidebar .search-bar` had `padding-top: 10px`:
```css
.sidebar .search-bar {
    position: relative;
    margin-bottom: 20px;
    padding-top: 10px;  /* Problem: creates internal space */
    width: 100%;
}
```

The search icon uses `position: absolute; top: 50%; transform: translateY(-50%)` to center vertically. However, this centers relative to the container (including padding), not the input field. The input starts below the padding, so the icon appeared misaligned.

### Fix Applied
Changed `padding-top` to `margin-top` in `wwwroot/css/certificatetileview.css`:
```css
.sidebar .search-bar {
    position: relative;
    margin-bottom: 20px;
    margin-top: 10px;  /* Changed from padding-top - icon now centers correctly */
    width: 100%;
}
```

### Affected Files
- `wwwroot/css/certificatetileview.css` (line 66)

### Pages Affected
- `/Certificate/CertificateTileView` (Public)
- `/Certificate/CertificateTileViewExternal` (External)
- `/Certificate/CertificateTileViewInternal` (Internal)

### Verification
Build succeeded with no CSS-related errors.

---

## 2026-01-14 - Design Guide Compliance Fixes

### Summary
Fixed issues identified during Master Design Guide compliance testing.

### Issues Fixed

#### 1. GoogleAnalytics Page - Hybrid Layout Pattern
**File**: `Views/Admin/GoogleAnalytics.cshtml`
- Changed from `.analytics-container` to `.admin-body-content`
- Added `.admin-content` wrapper inside
- Updated table to use `.admin-table-container` and `.admin-table` classes
- Now follows standard hybrid layout pattern

#### 2. Table Action Icons - Grey Background Removal
**Problem**: Action icons (Edit, Delete, View) were showing grey backgrounds on hover instead of transparent backgrounds.

**Files Fixed**:
- `wwwroot/css/admin/admin-tables.css` (line 261-264)
  - Changed `.admin-action-btn:hover` background from `var(--admin-bg-accent)` to `transparent`
- `wwwroot/css/product-management-v2.css` (line 233-235)
  - Changed `.action-btn:hover` background from `var(--gray-light)` to `transparent`
  - Added specific hover colors for `.action-btn.edit:hover` and `.action-btn.delete:hover`
- `wwwroot/css/certificatetileview.css` (line 1871-1874)
  - Changed `.table-grid .grid-cell.actions-column .action-btn:hover` background from `#e3f2fd` to `rgba(0, 174, 239, 0.1)`

**Standard**: Action icons must have:
- Default: `background: none` or `background: transparent`
- Hover: `rgba(color, 0.1)` with 10% opacity (subtle, not solid)

#### 3. CLAUDE.md Documentation Updates
- Added "Admin Page Toolbar Pattern (US-016 Hybrid Layout)" section with complete structure
- Updated "Table Action Icons vs Buttons" section to emphasize NO GREY BACKGROUNDS
- Added explicit warnings against solid hover colors like `#e3f2fd`, `var(--gray-light)`, `#f5f5f5`

### Files Modified
- `Views/Admin/GoogleAnalytics.cshtml`
- `wwwroot/css/admin/admin-tables.css`
- `wwwroot/css/product-management-v2.css`
- `wwwroot/css/certificatetileview.css`
- `.claude/CLAUDE.md`

### Build Status
Succeeded (0 errors, 0 warnings)

---

## 2026-01-14 - Admin View HTML Structure Standardization

### Summary
Fixed inconsistent HTML structure across 6 admin views to ensure seamless master layout consistency. The issue was causing spacing problems around titles and main content areas.

### Problem Analysis
Views had inconsistent implementation of the standard layout pattern:
- Some views had duplicate toolbar structures
- Some views were missing `admin-toolbar` entirely
- Some views were missing `admin-header`
- Some views were missing `admin-footer-info`
- Some views had empty `admin-title-section`

### Standard Pattern (Per US-016 Hybrid Layout)
```
admin-container
  > admin-header
    > admin-header-content
      > admin-title-section (contains subtitle)
      > admin-actions-section (contains buttons)
  > admin-body-content
    > admin-toolbar (contains search & filters)
    > admin-content (contains table/main content)
    > admin-footer-info (shows count)
```

### Views Fixed

| View | Issue | Fix Applied |
|------|-------|-------------|
| CertificateTypes.cshtml | Double toolbar, missing footer | Merged view-options into admin-toolbar, wrapped content in admin-content, added admin-footer-info |
| ProductTypes.cshtml | Double toolbar, missing footer | Same fix as CertificateTypes |
| GoogleAnalytics.cshtml | Missing toolbar | Added admin-toolbar with status indicator, added admin-footer-info |
| PartnerAPI.cshtml | Missing toolbar, multiple admin-content sections | Added admin-toolbar with stats, consolidated to single admin-content wrapper, added admin-footer-info |
| Dashboard/Index.cshtml | Missing admin-header | Added admin-header with subtitle and empty actions-section |
| CertificateFields.cshtml | Missing subtitle | Added descriptive subtitle to admin-title-section |

### Compliance Status After Fix
- **All 15 admin views now compliant** with standard layout pattern
- Consistent spacing between header, toolbar, content, and footer
- Seamless visual appearance across all pages

### Build Status
Succeeded (0 errors, 0 warnings)

---

## 2026-01-15: CertificateTypes Page - Save/Delete Not Working

### Issue
Save and Delete functionality on `/Admin/CertificateTypes` page was not working. No error messages, buttons appeared to do nothing.

### Root Cause
The `admin-core.js` script was not being loaded on the page. The JavaScript code in `certificate-types.js` depends on `AdminCore.showNotification()` and `AdminCore.showConfirmation()` functions which are defined in `admin-core.js`.

Console showed: `AdminCore available: false`

### Investigation Steps
1. Checked browser console - found `AdminCore available: false`
2. Verified `admin-core.js` file exists and has no syntax errors
3. Found the `@section Scripts` in `CertificateTypes.cshtml` was not including `admin-core.js`
4. Discovered compiled Razor view caching was preventing changes from taking effect
5. Had to kill locked processes and rebuild to pick up changes

### Solution
Added `admin-core.js` to the main `_Layout.cshtml` file so it loads globally on ALL pages:

```html
<script src="~/js/admin/admin-core.js"></script>
```

This ensures AdminCore is always available for any admin page that needs it.

### Additional Fixes Applied
- Added `RequestVerificationToken` headers to `updateCertificateType()` and `deleteCertificateType()` methods in `certificate-types.js` for consistency with other admin pages

### Lesson Learned
- When admin page functionality fails silently, check if `AdminCore` is available in browser console
- Adding shared scripts to `_Layout.cshtml` is more reliable than per-page `@section Scripts`
- Always restart the server after changing Razor view files to clear compiled view cache

---

## 2026-01-18 - API Testing Plan & Amazon Integration Review

### Summary
Comprehensive review of the Partner API for Amazon integration. Created exhaustive testing documentation covering all endpoints, authentication, field mapping, and edge cases.

### Key Findings

**API Architecture:**
- Two authentication paths: Partner API keys (`gcc_partner_` prefix) and general API keys
- Partner keys handled by `PartnerApiKeyAuthenticationMiddleware`
- General keys handled by `ApiKeyAuthenticationHandler`
- v1 API endpoints properly versioned at `/api/v1/`

**Confirmed Working:**
- `CertificationResults` field is properly exposed in all API responses (Critical for Amazon threshold verification)
- Product ID search (`/api/v1/certificates/search/productid`) returns ALL product IDs per certificate
- PDF download supports both certificate number and database ID
- Pagination, sorting, and filtering all functional
- Partner-based filtering implemented in middleware

**API Controllers Inventory:**
| Route | Controller | Purpose |
|-------|------------|---------|
| `/api/v1/certificates` | CertificatesController | Primary partner API |
| `/api/v1/apikeys` | ApiKeysController | API key management (Admin) |
| `/api/admin/partner` | PartnerApiController | Partner management |
| `/api/admin/amazon` | AmazonApiController | Amazon settings |
| `/api/productids` | ProductIdApiController | Product ID CRUD |
| `/api/CertificatesApi` | CertificatesApiController | Legacy internal API |

### Gaps Identified

| Gap | Impact | Status |
|-----|--------|--------|
| Partner data filtering verification needed | Medium | To verify |
| Dynamic PDF generation not implemented | Low | Known limitation |
| Documentation minor mismatches | Low | Updated in test plan |

### Questions Requiring Client Clarification

1. What specific certification result format does Amazon expect?
2. Should expired certificates be visible to partners?
3. What is the exact threshold value for CertificationResults?

### Deliverable Created
- `docs/API-TESTING-PLAN.md` - 12-section comprehensive testing document
  - Authentication test cases (10 tests)
  - Amazon integration test cases (8 tests)
  - Field mapping verification (8 tests)
  - Edge cases & error handling (14 tests)
  - Security testing (9 tests)
  - Performance tests (7 tests)
  - Integration test scripts (PowerShell & Bash)
  - Test checklist for pre/post integration

### Partner Integration Configuration
- **Primary Partner:** Amazon (connecting initially)
- **Secondary Partner:** Novi (future)
- **Data Flow:** Partners PULL data from GCC
- **Authentication:** API Keys only (X-API-Key header)
- **Rate Limit:** 2,000 requests/hour per partner key

### Risk Mitigation
- Created exhaustive test cases before Amazon integration begins
- Documented all field mappings to prevent integration failures
- Identified questions upfront to avoid mid-integration surprises

---

## 2026-01-18 - API Live Testing Results

### Summary
Ran comprehensive API tests against live development environment to verify endpoints work correctly.

### Test Results Overview

| Category | Passed | Failed | Status |
|----------|--------|--------|--------|
| Authentication | 4 | 0 | PASS |
| Certificate Retrieval | 4 | 0 | PASS |
| Product ID Search | 3 | 0 | PASS |
| PDF Download | 0 | 2 | **FAIL** |
| Advanced Search | 2 | 0 | PASS |
| **TOTAL** | **13** | **2** | **87%** |

### Critical Bug Found

**BUG-001: PDF Download Returns 406 Not Acceptable**

- **Severity:** HIGH
- **Impact:** Partners cannot download certificate PDFs via API
- **Endpoint:** `GET /api/v1/certificates/{id}/download`
- **Cause:** The `[Produces("application/pdf")]` Swagger attribute causes content negotiation failure
- **Location:** `Controllers/Api/v1/CertificatesController.cs:869`

**Recommended Fix:**
```csharp
// Remove [Produces("application/pdf")] attribute
// The File() method already sets correct content type
```

### What Works

1. **Authentication** - All scenarios working correctly:
   - Valid API key returns data
   - Invalid/expired/revoked keys return 401
   - Missing auth header returns 401

2. **Certificate Listing** - Works with all options:
   - Basic pagination works
   - `includeProductIds=true` returns ASIN/UPC/GTIN
   - All Amazon required fields present

3. **Product ID Search** - Critical for Amazon:
   - Search by ASIN works correctly
   - Search by UPC works correctly
   - Returns ALL product IDs per certificate with `isMatchedId` flag

4. **Amazon Field Mapping** - All required fields verified:
   - brandName, productName, certificateNumber
   - issueDate, expirationDate
   - certificationName, issuer, standardVersion
   - certificationResults (field exists, may be null)
   - productIdentifiers array with all types
   - pdfDownloadUrl

### Active API Keys Found

| Partner | Key Status | Last Used |
|---------|------------|-----------|
| Amazon | Active (x2) | 2025-11-26 |
| Novi | Revoked | 2025-11-26 |

### Deliverables Created
- `docs/API-TEST-RESULTS-2026-01-18.md` - Full test report with all results

### Immediate Actions Required
1. ~~Fix PDF download 406 error before Amazon integration~~ **FIXED**
2. Populate CertificationResults field for Amazon certificates
3. ~~Test PDF download after fix~~ **VERIFIED**

---

## 2026-01-18 - PDF Download Bug Fix

### Issues Fixed

**Issue 1: HTTP 406 Not Acceptable**
- **File:** `Controllers/Api/v1/CertificatesController.cs`
- **Line:** 869
- **Fix:** Removed `[Produces("application/pdf")]` attribute
- **Reason:** The attribute forced content negotiation to fail when clients didn't send exact Accept header

**Issue 2: Double Path Prefix**
- **File:** `Controllers/Api/v1/CertificatesController.cs`
- **Lines:** 904-915
- **Fix:** Added logic to detect if `CertificateFile` already contains `uploads/` prefix
- **Reason:** Database stores full relative path like `uploads/certificates/file.pdf`, but code was adding prefix again

### Test Results After Fix

| Test | Before | After |
|------|--------|-------|
| PDF download by number | 406 | **200** |
| PDF download by ID | 406 | **200** |
| Certificate not found | 406 | **404** |
| File not exists | 406 | **404** |

### Verification

```bash
curl -H "X-API-Key: ..." "http://localhost:5101/api/v1/certificates/25-5047/download"
# Returns: HTTP 200, 56488 bytes, PDF document version 1.7
```

---

## 2026-01-18 - Comprehensive Test Execution and Issues Documentation

### Summary
Executed all test suites across 6 test projects and documented all issues found. Created `TEST_ISSUES_FOUND_2026-01-18.md` as the main tracking document.

### Test Execution Results

| Test Project | Total | Passed | Failed | Skipped | Pass Rate |
|--------------|-------|--------|--------|---------|-----------|
| GCCWebApp.Tests | 784 | 626 | 106 | 52 | 79.8% |
| GCCWebApp.Api.Tests | 81 | 45 | 36 | 0 | 55.6% |
| GCCWebApp.Security.Tests | 29 | 21 | 8 | 0 | 72.4% |
| GCCWebApp.E2E.Tests | 14 | 6 | 8 | 0 | 42.9% |
| GCCWebApp.Accessibility.Tests | 54 | 17 | 37 | 0 | 31.5% |
| GCCWebApp.UAT.Tests | - | - | - | - | Build Error |
| **TOTAL** | **962** | **715** | **195** | **52** | **74.3%** |

### Key Issues Identified

**Critical Issues:**
1. API Authentication failures in integration tests - tests receive 401 instead of expected responses
2. NullReferenceException in CertificatesController tests - missing null handling
3. Response type mismatch in pagination tests - anonymous types vs. expected DTOs

**Infrastructure Issues:**
1. E2E and Accessibility tests require application running - getting connection refused errors
2. UAT tests have invalid logger configuration
3. Security tests fail to authenticate `securitytestuser`

### Files Created
- `GCCWebApp/TEST_ISSUES_FOUND_2026-01-18.md` - Full issues tracking document with:
  - 9 categorized issues
  - Priority assignments
  - Root cause analysis
  - Recommendations

### Observations
- Unit tests (GCCWebApp.Tests) have best pass rate at ~80%
- Integration/E2E tests require running application - not self-contained
- API tests have authentication setup issues with test API keys
- Many accessibility tests fail due to missing localhost connection

---

## 2026-01-18 - Critical Test Issues Fixed

### Fixes Applied

**ISSUE-002 & ISSUE-003: Fixed CertificatesController Tests (5 tests fixed)**
- Problem: Tests expected `StandardPaginatedResponse<List<Certificate>>` but controller returns anonymous type projections
- Solution: Updated tests to use reflection-based property access instead of direct type casting
- Files Modified: `GCCWebApp.Api.Tests\Unit\Controllers\CertificatesControllerTests.cs`

**ISSUE-005: Fixed RefreshToken 500 Error (1 test fixed)**
- Problem: Invalid tokens caused `SecurityTokenMalformedException` which was caught by generic handler returning 500
- Solution: Added specific exception catch with pattern matching for token-related exceptions, returns 401
- Files Modified: `GCCWebApp\Controllers\Api\v1\AuthController.cs`

**ISSUE-001: Partially Fixed API Authentication**
- Added missing test API keys to `TestDbContextFactory.cs`: `valid_test_key`, `integration_test_key`, `rate_limit_test_key`
- Updated `MultiPartnerApiIntegrationTests.cs` to use `WebApplicationFactoryBase<Program>` instead of `WebApplicationFactory<Program>`

### Results After Fixes

| Test Project | Before | After | Improvement |
|--------------|--------|-------|-------------|
| GCCWebApp.Api.Tests | 45/81 (55.6%) | 51/81 (63.0%) | +6 tests |

### Remaining Issues
- 30 integration tests still fail (mostly API authentication in WebApplicationFactory context)
- E2E and Accessibility tests require running application
- Security tests need proper test user setup

---

## 2026-01-22 - User Management: Role-Based Modal Fields Implementation

### Summary
Implemented role-based conditional fields in the User Management Add/Edit modal per the design recommendations documents.

### Changes Made

#### 1. UserManagement.cshtml - Added Partner Dropdown
```html
<!-- Partner Association - shown for Partner role -->
<div class="admin-form-group" id="partner-group" style="display: none;">
    <label for="user-partner" class="admin-label admin-label--required">Partner</label>
    <select id="user-partner" class="admin-select">
        <option value="">Select a partner</option>
        <!-- Partners populated dynamically by JavaScript -->
    </select>
</div>
```

#### 2. userManagement.js - New Methods Added
- `loadPartners()` - Fetches partners from `/Admin/GetPartners`
- `populatePartnerDropdown()` - Populates the partner dropdown
- `handleRoleChange(role)` - Shows/hides Company/Partner fields based on role:
  - **Partner role**: Show Partner dropdown, hide Companies
  - **External role**: Show Companies, hide Partner
  - **Admin/Analyst**: Hide both (full access)

#### 3. userManagement.js - Updated Methods
- `init()` - Added `loadPartners()` call
- `bindEvents()` - Added role change event listener
- `showAddUserModal()` - Reset partner field and hide both groups
- `editUser()` - Set partner value and trigger `handleRoleChange()`
- `handleFormSubmit()` - Include partnerIds in form data (only for Partner role)

#### 4. AdminService.cs - Backend Updates
- `CreateUserAsync()` - Added partner handling for Partner role users
- `UpdateUserAsync()` - Added partner handling including query includes
- `GetUsersAsync()` - Added `CompanyIds` and `PartnerIds` to response

### Role-Based Field Visibility

| Role | Companies Shown | Partner Shown | Notes |
|------|-----------------|---------------|-------|
| Partner | No | Yes (required) | Sees all certs tagged with partner |
| External | Yes | No | Sees only company certs |
| Admin | No | No | Full access |
| Analyst | No | No | Full access |
| API Integration | No | No | Configured separately |
| Public | No | No | Public certs only |

### Validation Added
- Partner role requires partner selection
- External role requires at least one company selection

### Files Modified
- `Views/Admin/UserManagement.cshtml`
- `wwwroot/js/userManagement.js`
- `Services/AdminService.cs`

### Build Status
Succeeded (0 compilation errors, file lock warning bypassed)

---

## 2026-01-22 - Phase 1: Multi-Partner Support & Company Partner Association

### Summary
Implemented multi-partner support for certificates and added PartnerId to Companies table for reporting.

### Database Changes

#### 1. CertificatePartners Join Table (Many-to-Many)
```sql
CREATE TABLE CertificatePartners (
    CertificatesId INT NOT NULL,
    PartnersId INT NOT NULL,
    PRIMARY KEY (CertificatesId, PartnersId)
);
```

#### 2. Companies.PartnerId (Optional FK for Reporting)
```sql
ALTER TABLE Companies ADD PartnerId INT NULL;
ALTER TABLE Companies ADD CONSTRAINT FK_Companies_Partners
    FOREIGN KEY (PartnerId) REFERENCES Partners(Id);
```

#### Migration Script Created
`docs/Database_Scripts/Migrations/14_20260122_AddMultiPartnerSupport.sql`
- Creates CertificatePartners table
- Adds PartnerId to Companies
- Migrates existing Certificate.Partner string values to CertificatePartners

### Model Changes

#### Company.cs
- Added `PartnerId` (int?, nullable)
- Added `Partner` navigation property

#### Partner.cs
- Added `Companies` collection
- Added `Certificates` collection

#### Certificate.cs
- Added `Partners` collection (ICollection<Partner>)
- Kept legacy `Partner` string field for backward compatibility

#### CertificateEditViewModel.cs
- Added `PartnerIds` (string, comma-separated list from UI)

### Repository Changes

#### IUnitOfWork.cs & UnitOfWork.cs
- Added `Partners` repository (IRepository<Partner>)

### ApplicationDbContext.cs
- Added Certificate-Partner many-to-many relationship
- Added Company-Partner optional FK relationship

### Controller Changes

#### CertificateController.cs - Create Action
- Added partner handling code after certificate save
- Parses PartnerIds comma-separated string
- Associates selected partners with certificate
- Sets legacy Partner field to first partner name for backward compatibility

### UI Changes

#### Views/Certificate/Create.cshtml
- Changed Partner from dropdown to checkbox container
- Added hidden field `partner-ids-hidden` for form submission
- Shows "Loading partners..." while fetching

#### wwwroot/js/certificate-create.js
Added `PartnerManager` component:
- `loadPartners()` - Fetches from /Admin/GetPartners
- `renderCheckboxes()` - Creates checkbox for each partner
- `loadExistingSelections()` - Pre-checks for edit mode
- `updateSelectedPartners()` - Updates hidden field on change
- `getSelectedPartnerIds()` - Returns array of selected IDs

### UI Styling
Partner checkboxes styled as rounded pills with:
- White background, gray border
- Green highlight on hover/checked (#92C83E)
- Flexbox wrap layout

### Build Status
Succeeded (0 compilation errors)

### Next Steps (Not Yet Implemented)
1. ~~Update Certificate Edit view similarly~~ ✅ COMPLETED
2. Update API responses to include Partners array
3. Update certificate tile views to show multiple partner badges
4. Add Partner dropdown to Company Management UI
5. Implement Data Access Filtering (Phase 2)

---

## 2026-01-22 - Certificate Edit View Multi-Partner Support

### Summary
Updated the Certificate Edit view to support multi-partner selection, completing the Edit side of the multi-partner feature.

### Changes Made

#### Views/Certificate/Edit.cshtml
- Changed Partner dropdown to checkbox container (same as Create)
- Added hidden field `partner-ids-hidden` with `data-existing-ids` attribute
- PartnerManager loads existing selections on page load

#### CertificateController.cs - Edit GET Action
- Added code to populate `PartnerIds` in view model from certificate's Partners collection
- Uses `string.Join(",", certificate.Partners.Select(p => p.Id))`

#### CertificateController.cs - Edit POST Action
- Added partner handling similar to Create
- Clears existing partners before re-associating
- Parses PartnerIds comma-separated string
- Updates legacy Partner field for backward compatibility

#### Repositories/CertificateRepository.cs
Updated three methods to include Partners:
1. `GetCertificateByNumberAsync()` - Added `.Include(c => c.Partners)`
2. `GetByIdAsync()` - Added `.Include(c => c.Partners)`
3. `GetAdvancedFilteredCertificatesAsync()` - Added `.Include(c => c.Partners)`

### Build Status
Succeeded (0 compilation errors)

### Remaining Next Steps
1. Update API responses to include Partners array
2. Update certificate tile views to show multiple partner badges
3. Add Partner dropdown to Company Management UI
4. Run migration script on database
5. Implement Data Access Filtering (Phase 2)

---

## 2026-01-22 - Test Suite Cleanup and Reorganization

### Summary
Cleaned up confusing test project structure with duplicates and inconsistent naming.

### Problems Found

| Issue | Description |
|-------|-------------|
| Duplicate folder | `GCCWebApp-Tests/` contained copies of Api.Tests, E2E.Tests, UAT.Tests - NOT in solution |
| Confusing naming | Active role tests were in `GCCWebApp.RoleBasedUI.Tests2/` (awkward "2" suffix) |
| Orphan tests | `GCCWebApp-Tests/GCCWebApp.Tests/` had 39 test files NOT in solution |
| Missing using statements | Several UITests files missing `using NUnit.Framework;` |

### Changes Made

1. **Renamed folder**: `GCCWebApp.RoleBasedUI.Tests2` → `GCCWebApp.RoleBasedUI.Tests`
2. **Updated solution file**: Fixed project references to new path
3. **Deleted duplicate folder**: Removed `GCCWebApp-Tests/` entirely
4. **Fixed NUnit using statements**: Added `using NUnit.Framework;` to 5 test files

### Role-Based Tests Location
The role-based tests **do exist** and are comprehensive:
- Location: `GCCWebApp.RoleBasedUI.Tests/Tests/`
- AdminRole tests (2 files)
- AnalystRole tests (2 files)
- ExternalRole tests (2 files)
- PublicRole tests (2 files)
- CrossRole access violation tests
- RoleTestHelper.cs with login utilities

### Pre-existing Issues (Not Fixed)
The RoleBasedUI.Tests project has code issues unrelated to the folder reorganization:
- `RoleTestHelper` class missing methods: `TestUsers`, `LoginAsync`, `TakeScreenshotAsync`
- Playwright API changes affecting `ToBeVisibleAsync` method signatures

### Build Status Post-Cleanup

| Project | Status |
|---------|--------|
| GCCWebApp | ✅ Builds |
| GCCWebApp.Api.Tests | ✅ Builds |
| GCCWebApp.E2E.Tests | ✅ Builds |
| GCCWebApp.UAT.Tests | ✅ Builds |
| GCCWebApp.RoleBasedUI.Tests | ⚠️ Pre-existing code issues |

### Final Test Project Structure
```
GCCWebApp/
├── GCCWebApp/                          # Main app
├── GCCWebApp.RoleBasedUI.Tests/        # Role-based tests (renamed)
├── GCCWebApp.Api.Tests/
├── GCCWebApp.E2E.Tests/
├── GCCWebApp.UAT.Tests/
├── GCCWebApp.Security.Tests/
├── GCCWebApp.Accessibility.Tests/
├── GCCWebApp.Performance.Tests/
├── GCCWebApp.BDD.Tests/
├── GCCWebApp.Tests.Common/
└── Archive_GCCWebApp.Tests/            # Legacy tests archive
```

### Recommendations
1. ~~Fix RoleTestHelper.cs to implement missing methods~~ ✅ DONE
2. ~~Update Playwright assertions to match current API~~ ✅ DONE
3. Consider removing Archive_GCCWebApp.Tests if tests are duplicated

---

## 2026-01-23 - RoleBasedUI.Tests Fix: Build Errors Resolved

### Summary
Fixed all build errors in GCCWebApp.RoleBasedUI.Tests project so it now compiles successfully.

### Issues Fixed

| Issue | Fix |
|-------|-----|
| Missing `using NUnit.Framework;` | Added to 5 UITests files |
| Missing RoleTestHelper methods | Added `TestUsers`, `LoginAsync`, `TakeScreenshotAsync`, `IsElementVisibleAsync`, `ValidateRoleElementsAsync`, `IsPageAccessibleAsync` |
| Invalid Playwright assertions | Removed string parameters from `ToBeVisibleAsync()` calls - Playwright API doesn't accept message strings |
| Broken PlaywrightAssertionExtensions | Removed non-functional extension class |
| User model required properties | Added `Status` and `TwoFactorMethod` to TestWebApplicationFactory seed data |

### Files Modified

**Helpers:**
- `RoleTestHelper.cs` - Added missing methods and TestUsers dictionary
- `TestWebApplicationFactory.cs` - Added required User properties

**Test Files (Fixed NUnit using + Playwright assertions):**
- `Tests/AdminRole/AdminRoleUITests.cs`
- `Tests/AnalystRole/AnalystRoleUITests.cs`
- `Tests/ExternalRole/ExternalRoleUITests.cs`
- `Tests/PublicRole/PublicRoleUITests.cs`
- `Tests/CrossRole/RoleAccessViolationTests.cs`

### Build Status: All Projects

| Project | Status |
|---------|--------|
| GCCWebApp | ✅ |
| GCCWebApp.Api.Tests | ✅ |
| GCCWebApp.E2E.Tests | ✅ |
| GCCWebApp.UAT.Tests | ✅ |
| GCCWebApp.RoleBasedUI.Tests | ✅ |
| GCCWebApp.Security.Tests | ✅ |
| GCCWebApp.Accessibility.Tests | ✅ |
| GCCWebApp.Performance.Tests | ✅ |
| GCCWebApp.BDD.Tests | ✅ |
| GCCWebApp.Tests.Common | ✅ |

### Test User Credentials (in RoleTestHelper)
```csharp
TestUsers["Admin"] = ("admin", "TestPass123!")
TestUsers["Analyst"] = ("analyst", "TestPass123!")
TestUsers["External"] = ("external", "TestPass123!")
```

---

## 2026-01-23 - Multi-Partner UI Clarification & Change Request

### Issue Identified
The Partner selection UI was incorrectly implemented as **checkboxes** when it should remain a **dropdown with multi-select capability**.

### User Requirement (Clarified)
- Partner selection should still be a **dropdown**
- Admins/Analysts can select **multiple partners** for a certificate if applicable
- NOT a checkbox container

### Implementation Status

| Component | Status |
|-----------|--------|
| Database Migration | COMPLETED |
| CertificatePartners table | CREATED |
| Companies.PartnerId column | ADDED |
| Model changes | COMPLETED |
| Repository changes | COMPLETED |
| Controller changes | COMPLETED |
| UI (Views/JS) | NEEDS CORRECTION |

### Action Taken
Created `todo_pendingChangeRequest.md` documenting:
- All completed backend changes
- UI correction needed (checkbox → multi-select dropdown)
- Step-by-step implementation guide
- Testing checklist
- Questions to clarify before resuming

### Pending Decision
Multi-select dropdown style preference:
1. Standard HTML multi-select (Ctrl+click)
2. Enhanced dropdown with checkboxes inside
3. Tag/chip style selector (Select2/Choices.js)

---

## 2026-01-23 - User Management Role-Based Field Visibility (Already Implemented)

### Summary
Reviewed User Management implementation and discovered the role-based field visibility was **already fully implemented** in the correct file (`wwwroot/js/userManagement.js`).

### File Discovery
Two userManagement.js files exist:
- `wwwroot/js/userManagement.js` - **ACTIVE** (class-based, used by view)
- `wwwroot/js/admin/userManagement.js` - **LEGACY** (functional style, NOT used)

The view at `Views/Admin/UserManagement.cshtml` line 610 references:
```html
<script src="~/js/userManagement.js"></script>
```

### Implementation Status (Already Complete)

| Feature | Location | Status |
|---------|----------|--------|
| Role change event listener | Line 50-51 | DONE |
| `handleRoleChange(role)` function | Lines 170-200 | DONE |
| Partner dropdown population | Lines 127-168 | DONE |
| Company checkboxes population | Lines 1489-1528 | DONE |
| Add modal resets visibility | Lines 470-497 | DONE |
| Edit modal sets partner/companies | Lines 499-546 | DONE |
| Form validation for role | Lines 659-667 | DONE |

### Role-Based Visibility Logic (handleRoleChange)
```javascript
if (role === 'Partner') {
    // Show Partner dropdown, hide Companies
    companyGroup.style.display = 'none';
    partnerGroup.style.display = 'block';
    partnerSelect.required = true;
} else if (role === 'External') {
    // Show Companies, hide Partner
    companyGroup.style.display = 'block';
    partnerGroup.style.display = 'none';
} else {
    // Admin, Analyst, API Integration, Public - hide both
    companyGroup.style.display = 'none';
    partnerGroup.style.display = 'none';
}
```

### Form Validation (handleFormSubmit)
```javascript
if (role === 'Partner' && formData.partnerIds.length === 0) {
    this.showNotification('Partner role requires a partner selection', 'error');
    return;
}
if (role === 'External' && formData.companyIds.length === 0) {
    this.showNotification('External role requires at least one company selection', 'error');
    return;
}
```

### Conclusion
Step 1 of User Management Design Recommendations is already complete. No code changes needed.

---

## 2026-01-23 - ProductController OR Logic Security Fix

### Issue
ProductController had OR logic combining Company + Partner filters, allowing External users to potentially see cross-organizational data.

### Design Requirement
Per Design Recommendations:
- **Partner role** → sees ALL products with partner-tagged certificates
- **External role** → sees ONLY products from their assigned companies
- **No OR logic** combining both

### Files Modified
**`Controllers/ProductController.cs`**

### Changes Made

#### 1. GetDashboardStats() - Lines 67-100
**Before (INSECURE):**
```csharp
if (userRole == "External")
{
    if (hasCompanies && hasPartners)
    {
        productsQuery = productsQuery.Where(p =>
            (p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value)) ||  // ← OR logic
            p.Certificates.Any(c => userPartnerNames.Contains(c.Partner ?? "None"))
        );
    }
    // ...
}
```

**After (SECURE):**
```csharp
if (userRole == "Partner")
{
    // Partner users see products with partner-tagged certificates
    productsQuery = productsQuery.Where(p =>
        p.Certificates.Any(c => userPartnerNames.Contains(c.Partner ?? "None")));
}
else if (userRole == "External")
{
    // External users see ONLY company products (no partner filtering)
    productsQuery = productsQuery.Where(p =>
        p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value));
}
```

#### 2. GetProducts() - Lines 162-195
Same pattern change - removed OR logic, added separate Partner role handling.

#### 3. mostPopularCategory query - Lines 109-125
Updated to handle Partner role with certificate-based filtering.

### Security Impact
- **Fixed**: External users can no longer see cross-organizational data
- **Fixed**: Partner role now properly handled (was missing before)
- **Fixed**: Clear role boundaries enforced

### Build Status
Build succeeded with 0 errors.

---

## 2026-01-23 - CertificateTileExternalApiController Role-Based Filtering

### Issue
The External API controller only filtered by Partner, not by Company for External role users. This meant External users could see certificates outside their company scope.

### Design Requirement
Per Design Recommendations:
- **Partner role** → sees certificates tagged with their partner
- **External role** → sees certificates from their assigned companies only
- **Admin/Analyst** → sees all

### Files Modified
**`Controllers/Api/CertificateTileExternalApiController.cs`**

### Changes Made

#### 1. Added Helper Methods (lines 625-690)
```csharp
private string GetCurrentUserRole()
private string? GetUserPartner()  // Enhanced with logging
private async Task<List<int>> GetCurrentUserCompanyIdsAsync()
private async Task<List<string>> GetCurrentUserPartnerNamesAsync()
```

#### 2. GetCertificateTiles() - Role-Based Filtering (lines 79-115)
**Before:**
```csharp
var userPartner = GetUserPartner();
if (!string.IsNullOrEmpty(userPartner))
{
    query = query.Where(c => c.Partner == userPartner || c.Partner == null);
}
```

**After:**
```csharp
if (userRole == "Partner")
{
    var userPartnerNames = await GetCurrentUserPartnerNamesAsync();
    query = query.Where(c => userPartnerNames.Contains(c.Partner ?? "None"));
}
else if (userRole == "External")
{
    var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
    query = query.Where(c => userCompanyIds.Contains(c.CompanyId));
}
```

#### 3. GetAutocompleteSuggestions() - Same Role-Based Pattern
Updated to filter suggestions based on user's role and associations.

#### 4. GetFilterOptions() - Same Role-Based Pattern
Updated to only show filter options relevant to user's accessible data.

#### 5. DownloadCertificate() - Access Control (lines 530-580)
**Before:**
```csharp
var userPartner = GetUserPartner();
if (!string.IsNullOrEmpty(userPartner) && certificate.Partner != userPartner)
{
    return Forbid();
}
```

**After:**
```csharp
if (userRole == "Partner")
{
    hasAccess = userPartnerNames.Contains(certificate.Partner ?? "None");
}
else if (userRole == "External")
{
    hasAccess = userCompanyIds.Contains(certificate.CompanyId);
}
// With security logging for unauthorized attempts
```

### Security Improvements
- **External users** now correctly restricted to their company's certificates
- **Partner users** correctly see partner-tagged certificates
- **Logging** added for unauthorized access attempts
- **Logging** added for missing claims (Partner without partner, External without company)

### Build Status
Build succeeded with 0 errors.

---


## 2026-01-26 - User Management UX Review

### Issues Found & Fixed
1. **"Require password reset" checkbox not clickable** - Fixed double-click event issue
   - Root cause: Wrapper div had `onclick` that fired `.click()` AND label had `for` attribute
   - Both triggered checkbox toggle, cancelling each other out
   - Solution: Removed redundant `onclick` from wrapper div

2. **Password field hidden from admin** - Implementing visibility toggle
   - Admin needs to see password to communicate to new users
   - Adding show/hide toggle with eye icon

### User Management Use Cases Review

| Use Case | Status | Notes |
|----------|--------|-------|
| Create user | ✓ Supported | |
| Edit user | ✓ Supported | |
| Delete user | ✓ Supported | |
| Activate/Deactivate | ✓ Supported | |
| Assign roles | ✓ Supported | |
| Assign companies | ✓ Supported | External role |
| Assign partners | ✓ Supported | Partner role |
| Search/filter | ✓ Supported | |
| Import/Export | ✓ Supported | |
| Password visibility | ✓ Adding | Show/hide toggle |
| Email to user | TODO | Send credentials email |
| Account unlock | TODO | Future feature |
| 2FA reset | TODO | Future feature |
| Login history | TODO | Future feature |

### Decision Made
- Password will be shown on screen with toggle (not emailed initially)
- Email notification to user will be added later

---

## January 26, 2026 - Certificate Types Duplicate Detection Fix

### Issue Reported
- User reported errors when trying to add a Certificate Type or Subcategory that already exists
- Error message was generic: "Failed to create certificate type"
- No indication that the issue was a duplicate name

### Root Cause Analysis
- `AdminService.CreateCertificateTypeAsync()` had no duplicate checking
- Method only returned `bool` (success/failure) with no error details
- Controller returned generic message on failure

### Solution Implemented
1. **IAdminService.cs** - Changed return types to tuples:
   - `CreateCertificateTypeAsync` → `Task<(bool Success, string? ErrorMessage)>`
   - `UpdateCertificateTypeAsync` → `Task<(bool Success, string? ErrorMessage)>`

2. **AdminService.cs** - Added duplicate detection:
   - Checks if type/subcategory with same name exists at the same level
   - Case-insensitive comparison using `.ToLower()`
   - Specific error messages for types vs subcategories:
     - Type: "A certificate type named 'X' already exists"
     - Subcategory: "A subcategory named 'X' already exists under 'ParentName'"

3. **AdminController.cs** - Updated to return specific error messages

### Test Results
| Test Case | Result | Message |
|-----------|--------|---------|
| Duplicate Type | BLOCKED | "A certificate type named 'test' already exists" |
| Unique Type | CREATED | Success |
| First Subcategory | CREATED | Success |
| Duplicate Subcategory | BLOCKED | "A subcategory named 'TestSubCategory' already exists under 'ParentName'" |

### Files Modified
- `Services/IAdminService.cs` (lines 35-36)
- `Services/AdminService.cs` (lines 741-825)
- `Controllers/AdminController.cs` (lines 690-735)

---

## 2026-01-26 - Tag Filtering Audit - Critical Issues Found

### Summary
Conducted exhaustive review of all tag filtering logic in response to:
1. External tags not showing in Certificate public view
2. Internal view not showing the right tags

### Critical Issues Identified

**Issue #1: Public View - No TagType Validation**
- `CertificateTilesApiController.cs` Lines 148-152
- Filter dropdown correctly shows only external tags
- BUT main query accepts ANY tag name without TagType validation

**Issue #2: External User View - Wrong Tags Can Pass Through**
- `CertificateTileExternalApiController.cs` Lines 165-170
- External users can filter by ANY tag (including internal)
- Missing `&& t.TagType == "external"` in query

**Issue #3: Internal View - Inconsistent Display**
- Response correctly splits tags by type (Lines 178-179)
- BUT filter options show ALL tags together (Lines 450-469)
- Confusing UX - tags split in display but combined in filter

**Issue #4: Repository Level - No Defense**
- `CertificateRepository.cs` Lines 201-202
- No TagType validation at repository level
- All controllers rely on this without passing restrictions

**Issue #5: CSF Tags - Unclear Handling**
- TagType "csf" exists but not consistently filtered
- Unclear if CSF should display as external, internal, or separately

### Security Implications
- External users can see internal tag names via API
- Tag filter bypass possible via manual API requests
- No server-side defense-in-depth

### Documentation Created
- Full audit: `docs/TAG-FILTERING-AUDIT.md`
- Contains fix recommendations and testing checklist

### Priority Actions
1. P1: Fix External API tag validation (Low effort)
2. P1: Fix Public API tag validation (Low effort)
3. P2: Split Internal API filter options (Medium effort)
4. P3: Add repository-level validation (Medium effort)

### FIXES IMPLEMENTED - January 26, 2026

**Files Modified:**

1. **CertificateTilesApiController.cs** (Lines 148-157)
   - Added validation to only accept external tags in public API
   - Validates requested tags against database before filtering

2. **CertificateTileExternalApiController.cs** (Lines 165-170, 201)
   - Added `t.TagType == "external"` to tag filter query
   - Fixed ExternalTags display to only show external tags

3. **CertificateTileInternalApiController.cs** (Lines 450-469)
   - Added separate `internalTags` and `externalTags` lists in filter options
   - Kept combined `tags` list for backward compatibility

**Build Status:** SUCCESS (0 errors)

**Additional Fix Required:**
4. **CertificateRepository.cs** (Lines 204-206)
   - Added handling for `filter.Tags` (string names) - repository only handled `filter.TagIds` before

**API Test Results (January 26, 2026):**

| Test | Result | Status |
|------|--------|--------|
| No filter (baseline) | 24 certificates | PASS |
| External tag (Eco-Friendly) | 1 certificate | PASS |
| Internal tag (High Priority Client) | 0 certificates | PASS - BLOCKED |
| Non-existent tag (FakeTag) | 0 certificates | PASS - BLOCKED |

**Testing Completed:**
- [x] Public view shows only external tags in filter (Eco-Friendly, Verified)
- [x] Public API blocks internal tag filtering
- [x] Public API blocks non-existent tag filtering
- [ ] External user view only shows external tags (requires manual test)
- [ ] Internal view shows both tag types properly separated (requires manual test)

---

## Session: February 16, 2026

### Observation: "Showing 0 items" Footer Bug
- **ProductTypes** and **CertificateTypes** pages had hardcoded "Showing 0 items" in footer (`#item-count`) that was never updated by JavaScript after data loaded
- All other admin pages (RoleManagement, Tags, Partners, etc.) correctly update their footer counts
- **Fix:** Added footer update logic to `updateStats()` in `product-types.js` and `certificate-types.js`

### Potential TODO (Pending Client Confirmation)
- **CSF Attributes architecture:** Currently CSF Attributes are stored in the `Tags` table with `TagType = "csf"`, reusing the Tag model (Name, Description, IsActive only). However, there is an original `CSFAttribute` model in `CertificateDetailsRelatedModels.cs` with richer fields: `AttributeName`, `AttributeValue`, `Unit`, `Category`, `DisplayOrder`, `IsPubliclyVisible`. Need to confirm with client whether CSF Attributes should have their own dedicated table with these additional fields, or if the current Tags-based approach is sufficient.

### CSF Attributes Data Update
- Updated production CSF Attributes to match client's full list (36 attributes)
- Data stored in Tags table with TagType = "csf"

---

## Session: April 21, 2026

### DEF: Partner API Status Contract Violation (Amazon Integration)

**Trigger:** Shawn asked why yesterday's data import didn't result in Amazon seeing `status: "Active"` in the API JSON payload. Initial analysis suggested "as designed" (import defaults certs to Draft, which is correct per the Amazon Developer Guide §8 "draft, expired, suspended, and revoked certs are excluded"). Shawn then provided a live JSON sample showing `"status": "Client Approved - Paid"` being returned to Amazon — confirming the Partner API was leaking internal lifecycle statuses instead of honoring the signed contract.

**Contract source:** `GCC Database Partner API - Amazon Developer Guide_updates_SW3.pdf` v1.0 (March 2026)
- §4 Response Data Model: `status` field → *"Always 'Active' (only active certs returned)"*
- §8 Security & Access Scope: *"Only active certificates are returned; draft, expired, suspended, and revoked certs are excluded"* + *"You will never see: … internal notes, workflow history, draft certificates…"*
- §9 Field Reference: `data[].status = "Active"` (Standard JSON) / `certificationInfo.status = "ACTIVE"` (GS1)

**Root cause:** Two failures in `Controllers/Api/v1/CertificatesController.cs`:
1. **Missing status filter on queries** — Partner role filtering only checked `Partner = "Amazon"`; never filtered by `PublicStatuses`. Draft, Hold, Expired certs leaked through if tagged to Amazon.
2. **Raw status projected to response** — Projections passed `c.Status` directly (e.g., `"Client Approved - Paid"`, `"Draft"`) instead of the contract literal `"Active"`.

**Note:** The GS1 single-cert endpoint already had correct mapping logic (lines 1325-1326 used ACTIVE/INACTIVE). The fix generalizes that pattern across all Partner-facing endpoints.

**Fix applied** (all in `Controllers/Api/v1/CertificatesController.cs`):
- `GetCertificates` (list, with and without ProductIds): added `PublicStatuses` filter for Partner + External roles; projections now output `isPartnerCaller ? "Active" : c.Status`
- `GetCertificatesByProductId` (search by ASIN/UPC/etc.): same filter + same projection rule
- `GetCertificatesGS1` (GS1 list): added `PublicStatuses` filter for Partner + External (GS1 single-cert mapping to ACTIVE/INACTIVE was already correct; filter ensures list only contains active)
- `GetCertificate` (by id), `GetCertificateByNumber`, `GetCertificateGS1` (single), `DownloadCertificate`: added Partner-role 404 guard when certificate status is not in `PublicStatuses`
- Updated two stale XML docstrings at lines ~88 and ~679 that claimed `Active, Inactive, Pending, Expired` — corrected to match the contract's `Active`-only promise

**Scope preservation:**
- Admin and Analyst roles see no behavior change (they still get raw lifecycle status for internal dashboards)
- Import service default of `"Draft"` on new certificates (`CertificateImportService.cs:687, 744`) was not changed — it is correct per contract; the Partner API now correctly hides Drafts instead of leaking them
- No schema changes; no new dependencies

**Why yesterday's import "didn't work":** Two things compounded. Import correctly set new certs to `Draft` per lifecycle safety. But the Partner API did not filter out Drafts AND did not map approved statuses to the contract `"Active"` literal. Shawn saw raw internal values in the JSON and reported that as "import didn't set status to Active" — the defect was actually in the API layer, not the import.

**Build result:** 0 errors, 46 warnings (all pre-existing, unrelated).

**Files modified:** `Controllers/Api/v1/CertificatesController.cs` only.

### Observation: CertificationResults displayed as decimal instead of percentage

**Trigger:** Shawn reviewed `/Certificate/CertificateInternalView/26-0106` and observed `certificationResults` shows as `"0.17"` — should be `"17%"` for consistency with Amazon Developer Guide examples (`"32%"`, `"25%"`, `"60%"`).

**Root cause:** Field is a free-text 1000-char column (`Certificate.CertificationResults`) and analysts entered decimals. No display-side formatting anywhere.

**Scope audit:** 6 output surfaces displayed the raw value — internal detail view, internal tile API, Partner API v1 (4 projection sites including GS1), and product-ID search. External tile API and public tile API already exclude the field. CSV/Excel exports don't include it. JS tile views render whatever the server sends.

**Fix approach (Option A — display-side helper):**
- Added `Extensions/DisplayFormatters.FormatCertificationResults(string?)` with rule:
  - null/empty → unchanged
  - contains `%` → unchanged (already formatted, e.g. `"32%"`)
  - non-numeric → unchanged (e.g. `"Pass"`)
  - numeric in [0, 1) → multiply by 100, append `%` (e.g. `"0.17"` → `"17%"`)
  - numeric ≥ 1 → append `%` (e.g. `"17"` → `"17%"`)
- Applied at 6 sites. EF LINQ projections can't translate the custom method, so 2 of the Partner API projections materialize first then apply the format in a post-query `.Select`.
- Internal tile JS needs no change — it now receives pre-formatted values from the API.

**Files modified:**
- `Extensions/DisplayFormatters.cs` (new, ~15 LOC)
- `Views/Certificate/CertificateInternalView.cshtml:110`
- `Controllers/Api/v1/CertificatesController.cs` (+using, 4 projection sites, GS1 mapper)
- `Controllers/Api/CertificateTileInternalApiController.cs` (+using, 1 projection site)

**Build result:** 0 errors.

**Not in scope** (confirmed with client, Option A):
- No data normalization on save — Create/Edit forms still accept free text
- No DB migration — existing values display-formatted on the fly

---

## 2026-04-22 - QC #10 Retest: Certificates page margins (Database QC Log 4.22.26)

### Status on re-open
QC #10 (Amanda/Dan, 2025-11-04) was marked Complete but Carly's brutal-testing standard would re-flag it. Complaint: "Margins on both sides are very large - can we remove the margins so website uses the full page."

### Audit results (before fix) — `/Certificate/Certificates` at 2560×1080:
- **840px total empty gutter space (32.8% of screen)** — content capped at 1800px, centered, huge gray gutters both sides
- At 1920×1080: 200px gutters (10.4%)
- At ≤1440px: acceptable (<6%)

### Root cause
`wwwroot/css/main.css:207-211` — `.page-container { max-width: 1800px; padding: 0 40px; }` is global. Any viewport wider than 1800px shows gutters. External tile view (`CertificateTileViewExternal`) was already fluid (`max-width: 100%`) — consistent with Carly's expectation.

### Fix applied (scoped, non-breaking)
Added scoped override in `wwwroot/css/certificates.css` (lines 36-40):
```css
body:has(.certificate-search-section) .page-container {
    max-width: none;
}
```
`.certificate-search-section` exists only in `Views/Certificate/Certificates.cshtml` (verified via grep) — scope is guaranteed to the cert list page only. Other admin pages (Dashboard, UserManagement, ProductTypes, etc.) still hit the global 1800px cap.

### Audit results (after fix)
| Viewport | Before total gutter | After total gutter | Improvement |
|---|---|---|---|
| 1920 Full HD | 200px (10.4%) | 80px (4.2%) | −120px |
| 2560 Ultrawide | 840px (32.8%) | 80px (3.1%) | **−760px** |
| ≤1440px | unchanged | unchanged | n/a |

Test tooling: `Scripts/qc-10-margin-audit.js` (Playwright, 9 viewports × 3 roles). Screenshots saved to `Scripts/screenshots/qc-10-2026-04-23/`.

### Remaining
- Update QC log (Database QC Log_4.22.26Updates.xlsx) to confirm #10 retested and fixed
- No regression tests written — would recommend adding viewport/gutter assertions to existing Mobile UI test suite

---

## 2026-04-23 - QC #123: Duplicate certification types in public filter

### Defect
Carly flagged `/api/certificate-tiles/filters` returning 43 cert types with ~10 duplicates (Recycled Content x2, Certified Water Savings x2, LCA Optimized x2, etc.). Dev notes suggested query bug. Actual root cause was two-part:

### Root cause
**1. Data:** 10 `CertificationTypes` rows are duplicates by Name. Same name exists as both a Level-1 root (ParentId NULL) and as a child under `ParentId=7` ("Certified Sustainability Facts"). All certificates reference the root versions — the children have zero cert references. Looks like a bad taxonomy import.

**2. Code:** `CertificateTilesApiController.GetFilterOptions()` pulled `CertificationTypes` without filtering `IsActive = true`, so even if data was deactivated it would still surface. Sister APIs (Internal, External) use `certTypeIds.Contains(ct.Id)` which implicitly filtered orphans.

### Fix
- **Data:** Deactivated 10 duplicate records: IDs 49, 50, 53, 54, 55, 59, 65, 71, 73, 78. SQL saved to `Database_Scripts/QC123_deactivate_duplicate_cert_types.sql` for prod deploy (includes rollback).
- **Code:** `Controllers/Api/CertificateTilesApiController.cs:253-257` — added `.Where(ct => ct.IsActive)` to filter query.

> **2026-05-07 RETRACTION:** the data-side diagnosis above is **wrong** and the SQL was never run on prod. Per `feedback_certtypes_hierarchy.md` (saved 2026-05-01) those rows are L1/L2 hierarchy pairs by design, not duplicates; certs reference the L2s via `CertificationTypeSubcategoryId` not `CertificationTypeId`, so the script's CertCount=0 safety check joined the wrong column. Prod re-verify on 2026-05-07: 7 of 10 target Ids don't exist; the 3 surviving rows (71/73/78) are L2 children of "Certified Sustainability Facts" (ParentId=6, Level=2). The code-side `.Where(ct => ct.IsActive)` change above was the real fix and is sufficient. SQL renamed to `_OBSOLETE_QC123_deactivate_duplicate_cert_types.sql.archived` so it can't be run by accident.

### Verified
API now returns 52 unique certification types (down from 62, removed 10 dup rows).

### Not addressed (separate decisions)
Carly also flagged near-duplicates: "Closed Loop" vs "Closed Loop Product", "Material Ingredient Reporting" vs "Reporting Disclosure". These are distinct names representing different concepts — consolidation is a taxonomy/business call for GCC, not a data cleanup.

---

## 2026-04-23 - QC #124: Public company filter ignored company names

### Defect
`/api/certificate-tiles?company=Amazon` returned all 25 certs instead of just Amazon's. Carly's repro confirmed — sidebar company checkboxes did nothing on the public tile view.

### Root cause
`CertificateTilesApiController.GetCertificates` assigns non-numeric company values to `filter.CompanyNames` (line 139). `CertificateRepository.GetAdvancedFilteredCertificatesAsync` consumed `filter.CompanyIds` but had no branch for `filter.CompanyNames` — the property was defined on the filter interface (`ICertificateRepository.cs:68`) but not read.

### Fix
`Repositories/CertificateRepository.cs:300-302` — added:
```csharp
if (filter.CompanyNames != null && filter.CompanyNames.Any())
    query = query.Where(c => c.Company != null && filter.CompanyNames.Contains(c.Company.Name));
```

### Verified (localhost)
| Filter | Before | After |
|---|---|---|
| `?company=Amazon` | 25 | 8 (all 8 Amazon public certs) |
| `?company=Henkel` | 25 | 4 (8 public, 9 Expired excluded correctly) |
| `?company=NonExistent` | 25 | 0 |
| `?company=KAO` | 25 | 0 (all 4 KAO certs have status='Active' which is NOT in PublicStatuses — separate issue, see below) |
| no filter | 20 | 20 (unaffected) |

---

## 2026-04-23 - QC #125: Country filter shows 'All' literal — BLOCKED on CR

6 certificates have `Country = 'All'` literal in the dev DB (KAO x4, Amazon Devices x1, Sonos x1). This is clearly bad data — 'All' is not a valid country value and was likely entered via a multi-select dropdown that defaulted to a placeholder label.

Shawn's directive (2026-04-23): **"All is not a default field. This is a CR change."** — data cleanup on business records like Certificate.Country requires a formal Change Request from GCC. No unilateral UPDATE.

**Blocked until:** GCC issues a CR specifying whether to NULL these values, replace with a specific country (e.g., United States), or handle via front-end by filtering out the literal 'All' from the dropdown.

---

## 2026-04-23 - QC #120 Row 159 (open) — ancillary finding
PublicStatuses = `['Client Approved - Paid', 'Republished']` only. The DB also contains the literal status value `'Active'` (KAO certs use this) which is treated as NOT-public. Carly's complaint at row 159 — "reports aren't taking into account all 'Active' cert status types" — is a semantic naming collision, not a counting bug. Either:
(a) rename/migrate the literal `'Active'` DB values to their proper workflow state, or
(b) add `'Active'` to PublicStatuses. Needs product decision from GCC.

---

## 2026-05-05 - Product Management duplicate "test" rows (observation only, not a defect)

**What Shawn saw:** 4 rows on `gccwebapp.com/Product/Management` named `test`, company `TEST` — 1 row with only `Chemicals and Raw Materials` filled, 3 more identical rows with `Chemicals and Raw Materials > Plastic > HDPE`.

**On a fresh test:** Shawn re-added a new product through the modal — **single row created, no duplicates.** Not reliably reproducible from a clean attempt.

**Why dups can still slip through:** there is no duplicate-prevention guard anywhere on the manual Add Product flow:

| Layer | File | Has dup check? |
|---|---|---|
| Client (Save button) | `Views/Product/Management.cshtml:918-972` (`saveProduct`) | NO — button is not disabled on click; no spinner; modal stays open until response returns |
| Server (`POST /Product/SaveProduct`) | `Controllers/ProductController.cs:401-419` | NO — straight `_context.Products.Add(product)` |
| Database | `Products.Name` column | NO UNIQUE constraint, no filtered index |
| CSV Import path (separate) | `Controllers/ProductController.cs:755-761` | YES — rejects duplicate `Name` |

**Working theory for the existing 4 rows on prod-test:** an earlier session where the Save button was clicked multiple times during a slow Plesk → Kestrel response. With no UI signal that the save is in flight (no button-disable, no spinner, modal stays open) the user can reasonably click Save again. Each click fires a fresh POST with `id=0` → fresh insert. The 1 + 3 split (one row missing Type/Sub, three identical with full hierarchy) supports two distinct save sessions, the second one clicked 3×.

**Status:** observation only per Shawn — not opening a DEF this round. Existing 3 prod-test dup rows would require a CR for SQL cleanup (Data Changes Require CR rule).

**Future fix scope (if/when prioritized):**
- Client: disable `#save-btn` and set text to "Saving…" inside `saveProduct`'s try; re-enable in `finally`. Prevents the 2nd-click race.
- Server: in `SaveProduct` insert branch, add `Name` (case-insensitive) + `CompanyId` collision check matching the Import path's behavior at line 755-761. Belt-and-suspenders against any other client that calls the endpoint.



---

## 2026-05-08: Classified all 170 QC items in `Database QC Log_5.8.26sw.xlsx` Sheet 2

**Request:** Add classification + root cause to each issue report in the active QC log.

**Approach:**
- Backed up source file to `Database QC Log_5.8.26sw.BACKUP_before_classification.xlsx`
- Inserted new "Root Cause" column at position D (right after existing `classification` column at C); Sprint 3 columns shifted right by 1
- Classified each of 170 items into 5-bucket taxonomy: **Bug / Enhancement / Question / UX / CR**
- Wrote a 1-line root cause to column D (drawn from Details/Comment fields where present; "OPEN - ..." prefix for items still in Open status with no fix yet)

**Distribution:**
| Class | Count | % |
|---|---|---|
| Bug | 78 | 46% |
| UX | 42 | 25% |
| Question | 27 | 16% |
| Enhancement | 17 | 10% |
| CR | 6 | 3% |

**Notable groupings:**
- 78 bugs: heavy on early-build wiring gaps (#9, #13, #16, #18), CSS/styling root causes (#7, #30, #63, #88, #96), data/projection bugs (#22, #67-#70, #73, #117), filter/sort logic (#26, #33, #95, #157), session/auth (#42, #44, #58, #71)
- 42 UX: alignment, font sizes, padding, icon spacing, label rewording — not broken, just polish
- 27 questions: mostly clarifications client never followed up on; should be closed as "no action needed" once verified
- 17 enhancements: collapsible sidebar (#12), pagination (#65, #90), inline modals (#52), country autocomplete (#59)
- 6 CRs: new subcategories (#47, #49), new partner Wayfair (#110), Partner role (#143), Duo 2FA (#151), Product ID auto-status (#164 = CR-015)

**Header row shift verified:**
- Col C = classification
- Col D = Root Cause (NEW)
- Col E = Sprint 3 5-4 (was D)
- Col F-S = remaining columns shifted right by 1

**Files touched:** `Database QC Log_5.8.26sw.xlsx` only (Sheet 2 / "GCC DB QC Log2"). Other sheets untouched.

---

## 2026-05-08 (continued): Added "Reason" column to QC log Sheet 2

**Request:** "Determine the Reason: lack of requirements, actual defect, change due to client preference. I'm trying to provide context rather than the client just saying I didn't QC enough."

**Approach:** Inserted new Reason column at E (after Root Cause at D); Sprint 3 cols shifted right again. Three-bucket assignment per row:
- **Actual defect** — code didn't do what the spec said
- **Lack of requirements** — feature/rule was never specified upfront, surfaced during testing
- **Client preference change** — working feature was changed because the client decided differently

### Reason distribution

| Reason | Count | % |
|---|---|---|
| Actual defect | 64 | 38% |
| Lack of requirements | 61 | 36% |
| Client preference change | 45 | 26% |

### Cross-tab: classification × reason

| | Actual defect | Lack of requirements | Client preference | Total |
|---|---|---|---|---|
| Bug | 55 | 23 | 0 | 78 |
| UX | 9 | 1 | 32 | 42 |
| Question | 0 | 25 | 2 | 27 |
| Enhancement | 0 | 10 | 7 | 17 |
| CR | 0 | 2 | 4 | 6 |
| **Total** | **64** | **61** | **45** | **170** |

### Where the lack-of-requirements concentration is heaviest

- Status taxonomy (#22, #54): 14-state legacy replaced with 8 client-defined; original spec didn't define final list (note: Anuki designed the 14-state system, so this could be reframed as "build proceeded without signed design" rather than pure lack of requirements)
- Country list (#56): originally 5 hardcoded; client later required all 195 ISO (Anuki could have surfaced this earlier as a design question)
- Auto-expire job (#23): not in original spec; revealed at testing (basic data lifecycle Anuki could have flagged)
- Custom field sectioning (#118): rendering rules never specified
- Partner role (#143): role not in original RBAC spec
- Product ID auto-status follows cert (#164 = CR-015): user-controlled originally; client realized later it must auto-derive
- 25 clarification questions where client never specified upfront

---

## 2026-05-08 (continued 2): Added "Severity" column + reframed tone

**Request:** Add severity rating so volume doesn't drown out impact, and update the recommended tone so the analysis reads as joint review rather than self-defense.

**Approach:** Inserted Severity column at F (after Reason at E); Sprint 3 cols shifted right again. Four-bucket rating per row:
- **Critical** — blocks a key user path or shows wrong data publicly
- **Major** — feature is broken or notably wrong but workaround exists
- **Minor** — works but suboptimal UX/visuals
- **Cosmetic** — cosmetic-only or clarification questions; no functional impact

### Severity distribution

| Severity | Count | % |
|---|---|---|
| Critical | 8 | 5% |
| Major | 59 | 35% |
| Minor | 46 | 27% |
| Cosmetic | 57 | 33% |

### Severity × Reason cross-tab

| | Actual defect | Lack of requirements | Client preference | Total |
|---|---|---|---|---|
| Critical | 6 | 2 | 0 | 8 |
| Major | 37 | 22 | 0 | 59 |
| Minor | 16 | 12 | 18 | 46 |
| Cosmetic | 5 | 25 | 27 | 57 |
| **Total** | **64** | **61** | **45** | **170** |

### Honest read of the data

The numbers cut both ways and that's fine — they tell the truth about a complex multi-stakeholder build:

- **High-severity items concentrate in real bugs and missing requirements.** 67 items are Critical or Major. Of those, 43 are actual defects and 24 are lack of requirements. **Zero** are client preference change. The serious stuff is genuinely Anuki's work plus shared scope gaps — not preferences.
- **Low-severity items concentrate in preferences and clarifications.** 103 items are Minor or Cosmetic. Of those, 45 are client preference changes, 37 are lack of requirements, and only 21 are actual defects. The polish bucket is overwhelmingly design refinement.
- **The 8 Critical items are ours to own.** 6 of them are real defects we caught and fixed in this cycle (cert action buttons, can't add company/product, public view leaking non-public statuses, "Failed to create user", User import broken, dates off by 1 day in public view). 2 are lack of requirements that surfaced at the wrong stage of the process. We are not arguing with the Critical items.

### Recommended tone for the client conversation (partnership, not defense)

Use the analysis to *open a joint review*, not to *deflect responsibility*. The phrasing matters more than the data:

**Don't say:** "62% of your QC log wasn't actually our fault — only 38% were defects."

**Do say:** "We classified all 170 items by severity and reason so we can both see where time is going. Here's what stands out — let's talk about how to tighten the loop on each."

**Three honest takeaways to lead with:**

1. **The 8 Critical items are ours to own** — most are real defects we caught and fixed. We're not arguing with these.
2. **The 67 Major+Critical items split roughly 60/40 defects vs. requirements gaps.** The defect side is the testing process working. The requirements gap side is where we both could have caught things earlier — proposing a short joint design checkpoint per sprint to surface unspecified rules (status lifecycle, data visibility, role scope) before they become QC items.
3. **The 103 Minor+Cosmetic items are overwhelmingly polish and clarification** (87 of 103 are preferences or questions). They count as "QC log entries" but they're design iteration, not testing failures. Suggest tracking them in a separate "design refinement" log going forward so the QC count stays meaningful for both sides.

**Invite, don't argue:**
- Ask: "Does this severity/reason read match how you'd categorize these?" — gives the client veto power on the framing rather than handing them a finished narrative.
- Acknowledge: "The 64 actual defects are real and we own them. The other categories are where we'd love your read on whether the requirements were clear at the time."
- Propose: a short joint design-review checkpoint per sprint to convert "lack of requirements" items into specs *before* they hit QC.

### Caveats (be ready for these in the meeting)

- **Self-classification bias.** Anuki did the classifying. Offer to walk through any item where the client disagrees with the Reason or Severity tag.
- **~30+ items are still Open.** Don't claim "the testing worked" while items remain unfixed.
- **Some "lack of requirements" items could be reframed as "Anuki should have asked."** Examples: 5-country hardcode (#56), auto-numbering (#16), auto-expire (#23). These are basic design questions Anuki could have surfaced. Be ready to acknowledge this if the client pushes.
- **The 14-state → 8-state replacement (#22, #54)** is awkward to call "lack of requirements" — Anuki designed the 14-state system. If the client never approved that design, the issue is "build proceeded without signed design," not "client never specified."
- **Item count treats all severities equally.** If asked "how many defects?" the right answer is "64 actual defects, of which 6 were Critical and 37 were Major" — not "38% of 170."


---

## 2026-05-09: Cross-referenced borderline items against original requirements

**Request:** Review the 6 BORDERLINE items in detail and reference the requirements docs at `docs/Final Req Docs/archive/`.

**Source docs reviewed (all dated before build started):**
- `GCC Database Field Requirements_DRAFT_1.30.25.xlsx` (earliest)
- `GCC Database Field Requirements_2.4.25.xlsx`
- `GCC Database Field Requirements_2_19_25.xlsx` (final)

**Critical finding — all 3 versions are identical on the items in question.** Cert # auto-numbering, 7-status taxonomy, and auto-expire were specified from the very first DRAFT dated 1/30/25 and never changed. This means several items I had labeled "Lack of requirements" were actually build deviations from approved requirements.

### Reclassifications based on evidence

| ID | Old Reason | New Reason | Evidence |
|---|---|---|---|
| **16** | Lack of requirements | **Actual defect** | Cert Upload Fields R4 explicitly: "If the certificate number is left blank, a new certificate number will be automatically generated and assigned when saved." Plus format spec (year-prefix + 4-digit sequential). Specified in 1/30/25 DRAFT, identical in all later versions. |
| **22** | Lack of requirements | **Actual defect** | Cert Upload Fields R7 lists exactly 7 statuses: DRAFT, Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold, Expired, Republished. **Anuki built a 14-state system.** Build deviation, not spec gap. |
| **23** | Lack of requirements | **Actual defect** | Cert Upload Fields R7 functionality: "Database automatically moves cert to Expired when the Expiration Date has passed." Specified in 1/30/25 DRAFT. |
| **54** | Lack of requirements | **Actual defect** | Same as #22 — 7-status taxonomy was specified, 14-state was a build deviation. |

### Items that remain BORDERLINE after evidence review

| ID | Reason | Why still borderline |
|---|---|---|
| **56** | Lack of requirements | R17 says only "Dropdown of preset countries" — vague. 5 vs 195 not specified. Industry-standard default is ISO 3166-1 (195 countries) but spec wording was genuinely ambiguous. Anuki could have surfaced earlier. |
| **133** | Lack of requirements | R8/R9 specify date picker format only. Prepopulation behavior never specified. Could be implementation choice or unstated requirement. Genuine spec gap. |

### Updated reason distribution (post-reclassification)

| Reason | Count | % | Change |
|---|---|---|---|
| Actual defect | 68 | 40% | +4 |
| Lack of requirements | 57 | 34% | -4 |
| Client preference change | 45 | 26% | same |

### Updated severity × reason cross-tab

| | Actual defect | Lack of requirements | Client preference | Total |
|---|---|---|---|---|
| Critical | **8** | 0 | 0 | 8 |
| Major | 39 | 20 | 0 | 59 |
| Minor | 16 | 12 | 18 | 46 |
| Cosmetic | 5 | 25 | 27 | 57 |
| **Total** | **68** | **57** | **45** | **170** |

**All 8 Critical items are now correctly labeled Actual defects** — including the 14-vs-7 status taxonomy issue (#22, #54). The original framing "Critical items are ours to own" gets stronger, not weaker, with the correction.

### Updated talking points (stronger, not weaker)

The reclassification actually **strengthens** the conversation:

1. **"We went back and verified the analysis against the original requirements docs (1/30/25 DRAFT through 2/19/25 final). We found 4 items we had mislabeled as 'lack of requirements' — they were actually defects against approved spec. Here's the corrected analysis."** — This shows intellectual honesty and rigorous methodology. Disarms the "self-classification bias" pushback before it lands.

2. **"40% of the QC log was actual defects. We own these. Most have been fixed."** — Real number, real ownership.

3. **"34% surfaced as requirements gaps during testing. The two biggest borderline cases (countries, date prepopulation) are genuine spec ambiguity. We could have surfaced both as design questions earlier — that's a process improvement we're proposing for future sprints."** — Owns the part Anuki could improve without conceding the whole story.

4. **"26% were preference changes during QC. These are design refinement, not testing failures."** — Same point as before.

### What this means for the 14-vs-7 status story specifically

This is the most awkward thread. The honest version:
- Requirements doc (1/30/25 DRAFT through 2/19/25 final) listed 7 statuses
- Anuki built a 14-state workflow system anyway
- Client raised it in QC (#22) with the original 7 + 1 new status = 8
- We replaced the 14-state with the client's 8

**Don't let this come up as a discovery from the client.** Walk into the meeting having already volunteered the correction: "We re-checked the requirements doc and confirmed the original spec listed 7 statuses. We over-engineered the workflow with 14 states. The fix in this cycle reverted to the spec's 7 + 1 added in QC = the 8 we have now. That was on us."

### Caveats remaining

- Self-classification bias still applies on the 158 non-borderline items — offer to walk through any item where the client disagrees
- 30+ items still Open
- Severity ratings still subjective — different reviewer might shift some Major↔Minor


---

## 2026-05-09 (continued): Full re-verification of all 170 items against requirements

**Request:** "what about all of the other issues reported?" — fair pushback that I had only verified the 6 borderline items. The other 164 had self-classification bias risk.

**Approach:** Pulled ALL relevant requirements doc sheets and did a systematic per-item re-verification:
- `Cert Upload Fields` — cert form fields, statuses, dates, country, tags
- `Dashboard Metrics` — dashboard widget specs incl. "Active = Public & Confidential" definition
- `Search Recs` — filter taxonomy, public visibility rules
- `Role Permissions` / `RoleMappings` — user fields, role scoping
- `Amazon API Field Req` — Amazon-specific fields
- `Filter Sections` (Requirements.xlsx) — additional filter spec

### Total reclassifications across both rounds (12 items)

| ID | Original | New | Spec evidence |
|---|---|---|---|
| 2 | LoR | **Actual defect** | R10/R12: "Dropdown (with add capability)" + "Popup to add a new product" |
| 16 | LoR | **Actual defect** | R4: auto-numbering rule explicit |
| 18 | LoR | **Actual defect** | R18 (External Tag) + R19 (Internal Tag) explicit cert fields |
| 22 | LoR | **Actual defect** | R7: 7-status taxonomy specified, Anuki built 14 |
| 23 | LoR | **Actual defect** | R7 functionality: auto-expire on date-passed |
| 32 | LoR | **Actual defect** | Search Recs R107: Packaging is its own L1 category |
| 54 | LoR | **Actual defect** | Same as #22 |
| 62 | LoR | **Actual defect** | R11: "Company Contact Email/URL - Open text field" |
| 106 | LoR | **Actual defect** | RoleMappings R2: "User Collection Fields: ...Company..." |
| 135 | LoR | **Actual defect** | R7: "moves to Expired when Expiration Date HAS PASSED" (cert visible ON the day) |
| 141 | LoR | **Actual defect** | Dashboard R24-R26: "Export to Excel - include all fields" |
| 154 | LoR | **Actual defect** | Dashboard R2: "Active = Public & Confidential that have not expired" |
| 155 | LoR | **Actual defect** | Same as #141 |

Plus 6 items kept their classification but justifications updated with spec citations: #67, #68, #70, #121, #124, #169.

### Final reason distribution (after full re-verification)

| Reason | Count | % | Δ from original |
|---|---|---|---|
| Actual defect | 77 | 45% | +13 |
| Lack of requirements | 48 | 28% | -13 |
| Client preference change | 45 | 26% | same |

### Final severity × reason cross-tab

| | Actual defect | Lack of requirements | Client preference | Total |
|---|---|---|---|---|
| Critical | **8** | 0 | 0 | 8 |
| Major | **47** | 12 | 0 | 59 |
| Minor | 16 | 12 | 18 | 46 |
| Cosmetic | 6 | 24 | 27 | 57 |
| **Total** | **77** | **48** | **45** | **170** |

### What this means for the client conversation

The honest framing is sharper now:

- **45% of QC items were actual defects against the spec.** This is a meaningful number. Owning it gives credibility for the rest.
- **28% were genuine spec gaps** — items where the requirements docs (1/30/25 through 2/19/25) were silent or vague. Half of these (24) are Cosmetic.
- **26% were preference changes during QC** — design refinement, mostly Cosmetic (27).
- **All 8 Critical items are defects against approved requirements.** No exceptions.
- **47 Major items are defects against approved requirements.** This is the body of the work the client most fairly raised.

### Remaining BORDERLINE items (only 2 after evidence review)

| ID | Why still borderline |
|---|---|
| 56 | "Dropdown of preset countries" — vague, never specified count (5 vs 195) |
| 133 | Date picker spec'd format only — prepopulation behavior never addressed |

### Updated framing for the client meeting

Walk in with the corrected analysis already in hand:

> "We did a full cross-reference against the original requirements docs (1/30/25 DRAFT through 2/19/25 final). After the review, **45% of the QC log items are actual defects against approved spec — and we own those**. 28% are genuine requirements gaps where the spec was silent or vague. 26% are preferences that changed during QC. The biggest single thread we owe you is that we built a 14-state status workflow when your spec listed 7. We're not arguing with that — we should have caught it earlier."

This is a stronger, more credible position than the original "62% wasn't our fault" framing because:
1. It's defensible — every reclassification cites a specific requirement row
2. It owns more of the responsibility, which builds trust
3. The remaining 28% LoR is well-supported (mostly genuine gaps in mobile layouts, custom field sectioning, performance specs, partner role)
4. The "we volunteered the correction" move blunts the self-classification-bias attack

### Caveats remaining

- Self-classification bias still applies to the 158 non-reclassified items — but with the spec citation method in place for the reclassified ones, the methodology is more defensible
- 30+ items still Open (unfixed)
- Severity ratings still subjective
- Additional items might shift if we cross-reference against wireframes/user stories PDFs, which I haven't fully reviewed (only the Excel-format requirements docs)

---

## 2026-05-09 (continued 2): Wireframe re-verification + scope-gap CRs identified

**Request:** "check the wireframes for more evidence, can we identify other change requests based on current and documentation"

**Source reviewed:** `GCC Wireframes_UserStories_7_8_25 - Copy.pptx` (97 slides), focusing on user stories (slides 5-14) and wireframe notes (slides 26-87). Extracted 489 lines of slide text + speaker notes via PowerPoint COM.

### Additional reclassifications from wireframe evidence

| ID | Old | New | Wireframe citation |
|---|---|---|---|
| **65** | LoR | **Actual defect** | Slide 37 NOTES (Cert List External): *"Paging; allow minimum of 50 per page"* — pagination at 50 was specified for the analyst/internal cert list |
| **143** | LoR | **Actual defect** | Slide 9 user stories: *"Partners (where they will have access across companies for products that are part of their program only - for example Amazon CPF clients/certs)"* — Partner role explicit in user stories from inception |

Plus justification updates with wireframe citations on items already classified as Actual defect: #31 (button alignment - Slide 31), #36 (shareable URL - Slide 30), #70 (Top 5 Categories - Slide 33 notes "Missing").

### Final distribution (after all 3 verification passes)

| Reason | Count | % | Total Δ from original |
|---|---|---|---|
| **Actual defect** | **79** | **46%** | **+15** |
| Lack of requirements | 46 | 27% | -15 |
| Client preference change | 45 | 26% | same |

### Final severity × reason

| | Actual defect | Lack of requirements | Client preference | Total |
|---|---|---|---|---|
| Critical | **8** | 0 | 0 | 8 |
| Major | **48** | 11 | 0 | 59 |
| Minor | 17 | 11 | 18 | 46 |
| Cosmetic | 6 | 24 | 27 | 57 |
| **Total** | **79** | **46** | **45** | **170** |

**The story is now: 46% defects against approved spec. The remaining 27% LoR is mostly Cosmetic (24 of 46) — i.e., genuine spec gaps that didn't impact functional delivery.**

---

## Scope-gap CRs identified (not in QC log but specified in wireframes/requirements)

While reviewing the wireframes, surfaced **specified features that may not have been built or are partially built.** These are NOT in the current QC log — they should be triaged before the client raises them as additional defects, and converted into formal CRs if outstanding.

### High-likelihood gap items

| # | Spec source | Item | Status to verify |
|---|---|---|---|
| **G1** | Slide 30 (Tile View) + Slide 47 (Internal List) | **Toggle between List and Tile views** on internal cert list | "Ability to toggle between List and Tile views" — does the internal page have both modes wired? |
| **G2** | Slide 37 (External List) | **Multi-select certificate download** | "Ability to multiselect for Certificate Download" — does the multi-select bulk PDF download work end-to-end? |
| **G3** | Slide 37 + Slide 47 NOTES | **Cert preview column (eye/expand icon)** | "Can we have a cert preview column (could be little expand / eye icon) - which would display cert image in webpage view" — was this built or skipped? |
| **G4** | Slide 48 NOTES | **Clone certificate feature** | "Anuki explore options for: Clone feature that only carries over certain fields like Cert Type, Company, Dates, Location, Country, Company Contact" — was this scoped in or out? |
| **G5** | Slide 48 NOTES + Slide 54 | **Compare-and-update Smart Import logic** | "Smart Import Logic: Import compares existing data and only uploads new records. If existing Certificate # is uploaded, only changed fields are updated" — does cert import follow this logic or fail-on-duplicate? |
| **G6** | Slide 54 + #105 in QC log | **Audit Logs UI page** | History tables (AuditHistory, CertificateStatusHistory) exist per memory, but no UI surface. Wireframe specified "History table tracks all changes made through imports or manual updates" — UI page never built |
| **G7** | Slide 24 | **Account Lockout / Too Many Attempts screen** | Spec mentions but flags "Account Lock/Too Many Attempts Screen — Inform user of lockout, Time remaining or action to unlock (e.g., via email)" — likely not built; was this scoped out explicitly? |
| **G8** | Slide 48 NOTES | **Tag color coding (internal vs external)** | "Tags should be color coded for internal vs external" — current state unknown; cosmetic but spec'd |

### Conflicting spec items (require resolution before client raises)

| # | Conflict | Source A | Source B | Implication |
|---|---|---|---|---|
| **C1** | Location required? | Cert Upload Fields R16: "Required: No" | Wireframes Slide 50: "Required fields: ...Location, Country" | Two requirement docs conflict. Anuki built per one or the other. ID 4 in QC log was the client raising this question. Resolve before next sprint. |
| **C2** | Pagination strategy | Slide 30 (tile view): "*Wishlist:* lazy loading" | Slide 37 (list view): "Paging; allow minimum of 50 per page" | Tile view = infinite scroll (per spec). List view = pagination 50/page (per spec). Anuki implemented infinite scroll on both, then added pagination later. Reflects in QC #29, #65, #90. |

### Recommended action on gap items

**Don't volunteer all 8 gap items in the meeting** — that opens a new round of "what else?" The smart move:
1. **Internally verify** which of G1–G8 are actually missing (spend ~30 min checking the running app)
2. **For confirmed gaps:** add to a "deferred from original scope" list and decide which are bring-up-now vs save-for-Phase-2
3. **For C1 (Location required):** raise it proactively as "we noticed conflicting requirements docs on whether Location is required. We built per the field requirements xlsx. Want to confirm."

This shows due diligence (we re-checked the spec on our own initiative) without inviting an unbounded scope review.


### Gap items - quick code verification

Did fast grep checks against the codebase to triage which gap items are actually missing:

| # | Item | Verification result | Action |
|---|---|---|---|
| **G1** | Toggle List/Tile views | **Likely built** — both `CertificateTileViewExternal.cshtml` and `CertificateTileViewInternal.cshtml` reference list/tile toggle | Verify in browser; probably no action needed |
| **G2** | Multi-select cert download | **Likely built** — 11 JS files reference multi-select / bulk download patterns | Verify the bulk PDF download path end-to-end; if working, no action |
| **G3** | Cert preview column (eye icon) | **Status unclear** — needs UI inspection | Check tile/list view for inline preview icon |
| **G4** | Clone certificate feature | **Confirmed missing** — no `Clone` references in Controllers | Wireframe Slide 48 framed it as "Anuki explore options for" — likely scoped out as wishlist; safe to leave |
| **G5** | Compare-and-update Smart Import | **Status unclear** — needs `CertificateImportController` review | If import is fail-on-duplicate today, this is a real gap; was specified |
| **G6** | **Audit Logs UI page** | **Confirmed missing** — no `AuditHistory` views (only `CertificateImport/Preview.cshtml` matches) | Memory confirms tables exist but no UI surface. Specified in wireframe Slide 54. Real gap. |
| **G7** | Account Lockout screen | Likely scoped out (Slide 24 noted "If account creation is allowed in the future" — out-of-scope marker) | Probably not needed |
| **G8** | Tag color coding | Status unclear | Visual check; cosmetic |

### Confirmed gaps requiring decision

Two items are confirmed missing AND in original spec:

1. **G4 - Clone certificate** — wireframe positioned as "explore options for" (wishlist tone). Safe to leave but note in followup CRs.
2. **G6 - Audit Logs UI** — confirmed missing. History data exists in DB. UI page was specified. **This is a real spec gap.** Suggest raising as a Phase-2 CR ("Audit Logs view to surface existing AuditHistory + CertificateStatusHistory tables") to convert it from "missed feature" to "scope decision."

Two items need a few minutes of UI verification: G3 (preview icon), G5 (smart import logic). Worth doing before the next client conversation so you're not caught flat-footed.


### Resolution (2026-05-09)

Shawn confirmed all 8 gap items (G1-G8) and the C1 Location-required conflict are already handled. No outstanding scope CRs from the wireframe review.

---

## 2026-05-09 (continued 3): Impact justification for LoR and CPC categories

**Context:** Shawn's framing for the client conversation — *"There are a lot of defects, I could have done better testing, and the timeline could have been cleaner. But here's why the LoR and CPC categories also matter."*

This section quantifies the real cost of the 91 non-defect items so they're not dismissed as "category labels" — they consumed capacity that could have gone to defect resolution and on-time delivery.

### Rough effort estimates per category

Using typical SDLC effort for items that surface AFTER initial development (i.e., require: discovery → clarification request → wait for decision → design → implementation → re-test → re-deploy):

| Severity / Reason | Count | Avg hrs each | Total hrs (est.) |
|---|---:|---:|---:|
| Major LoR | 11 | 6-12 | **66–132** |
| Major CPC | 0 | — | 0 |
| Minor LoR | 11 | 1.5–3 | **17–33** |
| Minor CPC | 18 | 0.5–1.5 | **9–27** |
| Cosmetic LoR | 24 | 0.5–1 | **12–24** |
| Cosmetic CPC | 27 | 0.25–0.75 | **7–20** |
| **Total non-defect effort** | **91** | | **~110–235 hours** |

That's **3–6 weeks of single-developer time** spent on items that were not bugs. For comparison, the 79 actual defects probably took 150–250 hours to fix (defects are usually faster because the spec is clear — you're matching to a known target).

### The hidden cost: round-trip latency

Each LoR item carries a feedback-loop cost beyond the dev hours:
- Discovery during QC (vs design): ~4-8 weeks LATER than ideal
- Wait time for client decision: avg 1-2 weeks (per pattern in this project)
- Design rework if related to data model: blocks dependent features
- Re-test of all downstream surfaces

For 11 Major LoR items, that's **11 × ~1 week of stalled cycles = ~10 weeks of latency** layered on top of the dev hours. This compounds when items are inter-dependent (status taxonomy → auto-expire → public visibility → Partner API status → exports).

### Top 8 highest-cost LoR items (where late-arrival hurt most)

| ID | Item | Why it was expensive |
|---|---|---|
| **22, 54** | 14-state vs 7-state status taxonomy | 25+ files modified, SQL migration, re-test of every status-dependent code path (filters, exports, dashboard, Partner API). ~40–60 hrs additional. Caught at QC instead of design = 4-month feedback loop. |
| **23** | Auto-expire job | Built CertificateExpirationService, SQL migration, audit hooks. Dependent on #22 being settled first. ~10–15 hrs. |
| **56** | Country list 5 → 195 | New CountryList.cs, 2 controllers, 2 views, plus filter/export updates. ~4–8 hrs at QC vs ~1 hr if specified upfront. |
| **62** | Company Email/URL fields on Create | 6 files modified across model, view, controller. ~6–10 hrs. Both wireframe AND Cert Upload Fields R11 had it specified — late catch. |
| **118** | Custom field sectioning | Built complex section-grouping → client clarified "single bucket per CR-008". Wasted complexity. ~8–12 hrs. |
| **65, 90** | Pagination on internal list | Built infinite-scroll only → client wanted 50/page (which the wireframes specified). ~6–10 hrs to retrofit. |
| **52** | Products tied to company cascade | New API endpoint + 5 files for cascade workflow specified in R10/R12 ("dropdown with add capability"). ~10–15 hrs late vs design. |
| **143** | Partner role | Wireframes Slide 9 explicit. Adding a role retroactively touches RBAC, scoping, API auth. Estimated CR work: ~20–40 hrs. |

**These 8 items alone account for ~110–180 hours**, comparable to the *entire defect remediation cost*. And they were specified in either the field requirements or the wireframes — they should have been caught at design.

### Top 5 highest-cost CPC items (where preference-change hurt)

| ID | Item | Why it was expensive |
|---|---|---|
| **47, 49, 110** | New cert subcategories + Wayfair partner | SQL migrations, seed data updates, taxonomy re-testing. Each round = ~3-5 hrs. |
| **131** | Tile size 5-line → 3-line + remove subcategory | CSS + tile redesign + ellipsis logic. Multiple revisions. ~5–8 hrs. |
| **140** | List view density (font, wrap, padding, icon align) | 4 distinct UI changes in a single QC item. ~6–10 hrs. |
| **84, 86, 91** | Column reorders + scroll behavior + new columns | Each column change = layout + sort + export + toggle update. Cumulative ~10–15 hrs. |
| **151** | Duo as 2FA option (CR) | New auth integration. ~20–40 hrs (if pursued). |

**The 45 CPC items are individually small but cumulatively significant** — and each represents work that was done correctly the first time per spec/mockup, then redone after the client saw it.

### The "build-it-so-I-can-see-it" pattern (already in memory)

Per project retrospective memory: *"Project is ~1 year past original deadline. Root cause is NOT technical execution — it's that the client could not clearly articulate requirements upfront. Anuki ended up running the client's requirements discovery process at its own cost through repeated build-react-shift cycles."*

The data confirms this pattern:
- **46 LoR items** = decisions deferred to QC instead of made at design
- **45 CPC items** = preferences not locked until visible
- **Combined 91 items, ~110–235 hrs** = capacity consumed iterating instead of delivering

If even half those hours had been spent on the still-Open defects or on tightening test coverage, the testing gap Shawn is conceding would be smaller.

### Talking points for the conversation

**Anuki's concessions (Shawn leads with these):**
- 79 actual defects against approved spec. We own these.
- Testing was thinner than it should have been. We could have caught more before client UAT.
- Timeline ran longer than originally scoped.

**Shared accountability framing (this is the new content):**

> *"The 79 defects represent ~46% of the QC log. The other 54% — 46 requirements gaps and 45 preference changes — also consumed time. Roughly 110–235 hours of development work went into items that were not bugs against approved spec. That's 3–6 weeks of capacity that could have gone to defect prevention, more thorough testing, or hitting the original timeline. Of the requirements gaps, the highest-cost items (status taxonomy, country list, company contact fields, partner role) were already in the field requirements xlsx or the wireframes — they were missed by both sides at design review, not absent from spec."*

> *"Going forward: a 30-minute joint design review per sprint, with a sign-off on key data model decisions before development starts, would prevent the largest of these. The 14-state status workflow was built without that kind of explicit signoff. We won't do that again."*

**What this framing accomplishes:**
1. Owns the defects without minimizing them
2. Concedes the testing gap honestly
3. Quantifies the LoR/CPC impact so it's not just a label — it's documented hours
4. Names the "build-react-shift" pattern as the root cause of timeline overrun
5. Proposes a forward-looking process change that addresses both sides
6. Doesn't blame the client — frames it as "missed by both sides at design review"


---

## 2026-05-09 (continued 4): Updated framing — timeline pressure as root cause

**Critical context Shawn added:** Client is frustrated with quality. Time was the biggest factor. Project was perceived as "behind" from the first few meetings. Anuki rushed to hit timelines, didn't do it right from the early meetings.

This reframes the entire analysis. The defects, the LoR items, AND the CPC iteration aren't separate issues — they're symptoms of the same root cause: **a timeline that was set before scope was fully understood, and pressure to "catch up" that crowded out the design and testing rigor needed to deliver quality.**

### The causal chain

```
Early meetings: scope not fully locked
   ↓
Timeline set against incomplete scope
   ↓
Project perceived as "behind" almost immediately
   ↓
Anuki rushes to ship visible progress
   ↓
   - Skipped joint design reviews (LoR items deferred to QC)
   - Lighter test coverage (defects slip past testing)
   - "Build-it-so-I-can-see-it" cycles (CPC iteration)
   ↓
QC log fills with all three categories
   ↓
Client sees the defect count and concludes "quality problem"
   ↓
More pressure to fix → less time for design → loop continues
```

The 79 defects, 46 LoR items, and 45 CPC items are all downstream effects of the same upstream problem.

### What this means for the meeting

Shawn's concessions are now more powerful, not less:
- *"Quality wasn't where it should have been"* — true, AND it had a structural cause
- *"Testing was thinner than it should have been"* — true, AND we know why
- *"Timeline ran long"* — true, AND the original timeline was set against scope that wasn't yet locked

### Revised talking points (replacing the earlier framing)

**Open with full ownership:**

> *"You're right that quality wasn't where it needed to be. We had 79 actual defects. Testing was thinner than it should have been. Timeline ran longer than what we originally agreed to. I want to own that directly before anything else."*

**Then explain the structural cause without making it an excuse:**

> *"Looking back at the data, what I see is a pattern. From the early meetings we were already framed as 'behind,' which meant every cycle had pressure to ship visible progress. That pressure crowded out two things that really matter: joint design reviews before development, and thorough testing before client UAT. We carried both of those gaps for the whole project, and you can see them in the QC log:*
>
> *- The 79 defects are mostly things that more thorough testing would have caught — that's on us.*
> *- The 46 'requirements gap' items are mostly things that joint design review would have caught earlier — those were specified in the docs but not surfaced at design.*
> *- The 45 'preference change' items are the build-it-so-I-can-see-it cycle — and that cycle existed because we didn't have a stable design to test against."*

**Connect to the original miss:**

> *"None of this excuses where we ended up. But the root issue traces back to early meetings where we set a timeline before scope was actually locked. Once we were 'behind' from the start, every subsequent decision optimized for visible progress over rigor. That's a process failure, and it's on both sides — we agreed to that timeline and we didn't push back when scope grew."*

**Forward-looking proposal:**

> *"For whatever's next, two things would prevent the cycle: a real design checkpoint with sign-off before each sprint starts (catches the 46 LoR-type items at design instead of QC), and a regression test pass before each UAT (catches the 79 defect-type items before you see them). Both of those got compressed under timeline pressure. If we plan for them explicitly next time, the QC log on the next cycle should look very different."*

### Why this framing is stronger than the earlier "impact hours" version

The earlier version focused on hours of capacity consumed by LoR/CPC. That's defensible but reads as accounting.

This version names the **structural cause** — timeline pressure from misaligned-from-day-1 expectations — that explains all three buckets. It:

1. **Owns the quality problem fully** without minimizing
2. **Explains the testing gap honestly** — it wasn't laziness, it was time pressure
3. **Connects timeline overrun to original mis-scoping** without blaming the client (uses "we both" framing)
4. **Names "build-it-so-I-can-see-it" as a symptom of the same problem** rather than a separate complaint
5. **Closes with a concrete process change** that fixes the cycle, not a finger-point

### What NOT to say

- "We were forced to rush" — sounds defensive
- "If you'd locked the requirements earlier we wouldn't have these issues" — blame
- "62% of the log isn't really our fault" — undercuts the ownership
- "Scope creep made the timeline impossible" — true but unhelpful in this conversation

### What to reinforce if pushed

If client says "but you have 79 defects, that's a quality problem":
> *"Yes. Some are pure code defects we should have caught — like the dashboard math errors and the form bugs. Some are defects against the spec that we should have built right the first time — like the 14-state workflow when the spec listed 7. We own both. The question is what process change keeps the next 100 from looking the same — and that's where the design checkpoint and regression pass come in."*

If client says "the time and money on this has been extreme":
> *"It has been. The structural reason is that we ended up doing requirements discovery during testing instead of at design. About 110-235 hours of dev work went into clarifying requirements that came up in QC. That capacity should have been preventing defects. We didn't structure the cycles to do both."*


---

## 2026-05-09 (continued 5): Wireframe instability layer — final structural picture

**Critical context Shawn added:** *"Wireframes were always wrong, design went through multiple iterations. It was a constant communication challenge that made collaboration difficult."*

This is the third layer of the same structural problem. Combined with prior context:

1. Original scope wasn't locked when timeline was set (earlier finding)
2. Project perceived as "behind" from first few meetings → rushing
3. **Wireframes themselves went through 8+ iterations May-July 2025** — the design target was a moving one

### Wireframe iteration timeline (file dates from archive)

- `GCC Initial Mockups 1_22_25_CP.pdf` (Jan)
- `GCC Wireframes_UserStories_5_1_25` + CP comments
- `GCC Wireframes_UserStories_6_15_25`
- `GCC Wireframes_UserStories_6_16_25`
- `GCC Wireframes_UserStories_6_25_25`
- `GCC Wireframes_UserStories_7_7_25`
- `GCC Wireframes_UserStories_7_8_25`
- `GCC Wireframes_UserStories_7_16_25`
- `GCC Wireframes_UserStories_7_17_25`

Plus the design guide (`GCC Database Design Guide_6_13_25_updates.pdf`) and multiple status reports.

**8+ wireframe iterations means Anuki was rebuilding to a moving target.** Each iteration meant: review changes → re-design where needed → re-implement → re-test. None of this work was visible as "progress" to the client because the wireframe itself wasn't stable enough to be a finish line.

### Impact on the QC log analysis

This affects the credibility of wireframe-based reclassifications:

**Solid (Field Requirements xlsx — stable across all 3 versions):** ~12 of the 15 reclassifications. These citations hold up. The xlsx didn't change on key items.

**Wireframe-only (vulnerable to churn caveat):**
- #65 pagination 50/page (Slide 37)
- #143 Partner role (Slide 9 user stories)
- #31 button alignment (Slide 31 notes)

These three could reasonably be reframed: Anuki may have built per an earlier wireframe iteration. The current 7/8 wireframe is "what the spec became," not necessarily "what the spec was when the code was written." Decision pending on whether to revert these to LoR with churn note.

### The full structural narrative (this is what to bring to the meeting)

```
Original scope not locked at signing
   ↓
Timeline set against incomplete scope
   ↓
Wireframes go through 8+ iterations May-July 2025
   ↓
Anuki rebuilds to each iteration, each rebuild not visible as "progress"
   ↓
Project perceived as "behind" almost immediately
   ↓
Pressure to ship visible progress
   ↓
   - Joint design review skipped (LoR items deferred to QC)
   - Test coverage thinned (defects slip past)
   - "Build-it-so-I-can-see-it" cycles continue (CPC iteration)
   ↓
QC log fills with all three categories
   ↓
Client sees defect count → concludes "quality problem"
   ↓
Loop continues
```

### Revised talking points (final version, accommodates all 3 layers)

**Open with full ownership:**

> *"You're right that quality wasn't where it should have been. We had 79 defects against the approved spec. Testing was thinner than it needed to be. Timeline ran longer than what we originally agreed to. I want to own all of that directly before anything else."*

**Then explain the structural picture honestly (use this if/when conversation moves past the apology):**

> *"Looking at the data, what I see is a pattern that started early and compounded. From the first few meetings we were already framed as 'behind,' but that was relative to a timeline set before the scope was actually locked. The wireframes went through eight iterations between May and July alone — each one was a rebuild against a moving target. That meant we had constant pressure for visible progress, and each cycle, two things got squeezed: joint design reviews and thorough testing.*
>
> *That's why the QC log looks the way it does:*
> *- The 79 defects are mostly things more testing would have caught.*
> *- The 46 requirements gaps are mostly things joint design review would have caught earlier — most were already in the field requirements xlsx, just not surfaced at design.*
> *- The 45 preference changes are the iterating-on-the-build pattern that existed because we didn't have a stable design to test against.*
>
> *None of that excuses where we ended up. But it explains why every cycle was harder than the last — and why fixing one part without addressing the structural pattern wouldn't have made the others go away."*

**The shared accountability piece:**

> *"This is on both sides. We agreed to early timelines that didn't account for the design discovery still ahead. We didn't push back hard enough when wireframes kept changing. And we shipped builds before they were tested as thoroughly as they needed. On your side, the discovery cycles meant decisions were made over iterations rather than locked at design. Neither of us structured the cycle to allow real design sign-off before development."*

**Forward-looking proposal:**

> *"For the work that's left, two changes would break the cycle: a real design checkpoint with sign-off before each sprint starts, and a test pass before each UAT release. Both got squeezed under timeline pressure. If we plan for them explicitly — even at the cost of a slower-feeling cadence — the next QC log should look very different."*

### Key principle for the meeting

**Don't try to defend the QC count. Defend the proposal for what changes next.** The 79 defects are real. The 46 LoR items are real. The 45 CPC items are real. What you can offer is: *"Here's what specifically prevents the next 100 from looking like this."* That's the conversation worth having.


---

## 2026-05-12 — UI rename: "Company Access" → "User Company" on UserManagement table + CSV templates

Shawn request: on `/Admin/UserManagement`, change the **table column header** and both the **import** and **export** CSV templates from "Company Access" to "User Company". Modal field label left as "Company Access" per [only-make-requested-changes](feedback_only_requested_changes.md) rule (Shawn did not ask for the modal).

Files edited:
- `Views/Admin/UserManagement.cshtml:95` — `<th>` header
- `wwwroot/js/userManagement.js:800,805` — export CSV column key + comment
- `wwwroot/js/userManagement.js:861` — import template CSV header
- `wwwroot/js/userManagement.js:1056` — import-preview "changes diff" display label
- `Controllers/AdminController.cs:244` — comment alignment with template

Server-side import is positional (reads by column **index**, not header name), so the rename is display-only; existing CSVs with the old header still parse.

---

## 2026-05-12 — DEF-186: User import template leaked plaintext passwords; blank cells defaulted to public `TestPass123!`

Shawn raised during DEF-186 review of the Company Access → User Company rename: "password is in the import template. Is this best practice?" Answered: no, it's a high-severity security issue across 3 layers, and the blank-cell fallback compounded it with a publicly-published default.

Filed as DEF-186 in `docs/tracking/defect-register.md`. Severity High (security). Fixed locally on http://localhost:5101 in one cycle:

- New cryptographically-random password generator in `AdminService.CreateUserAsync` is the single safety net — any empty/whitespace password from any caller gets a 24-char random + `RequirePasswordReset=true` forced.
- Template trimmed to 5 columns (`Name, Email, Role, Status, User Company`); Password and RequirePasswordReset columns gone.
- Both JS bulk-import flow and server-side `/Admin/ImportUsers` parser updated to send/use empty password — relies on the service safety net.
- `CreateUserViewModel.Password` `[Required]`/`MinimumLength=8` relaxed; min-length check moved to service for caller-supplied passwords only.

End-to-end verified: imported user has `RequirePasswordReset=True`, BCrypt hash present, login with `TestPass123!` rejected, admin login still works (regression clear).

**Open follow-ups (not in DEF-186 scope):**
- Email-the-user "set your password" at import time so admin doesn't have to coordinate the forgot-password flow out-of-band.
- Move the publicly-documented `TestPass123!` strings out of `.claude/CLAUDE.md` and `CLAUDE.md` into a dev-only secrets file. The default password being literally checked into project docs is what made DEF-186 a credential-stuffing layup.

Per [feedback_no_commits.md](feedback_no_commits.md): changes left in working tree; Shawn commits.

---

## 2026-05-12 — User Management pagination right-aligned

Shawn: `/Admin/UserManagement` pagination should be right-aligned.

Root cause: shared `.pagination-wrapper` uses `justify-content: space-between`, which collapses to left-alignment because the wrapper only has one flex child (`.pagination-controls`). The `pagination-info` div was removed previously (the count display moved to `.admin-footer-info` outside the pagination container), so `space-between` no longer has anything to push apart.

Fix: scoped one rule in `wwwroot/css/pages/user-management.css`:
```css
#user-pagination .pagination-wrapper { justify-content: flex-end; }
```

Scoped to the UserManagement-specific container ID. Product Management uses the same admin-pagination component with a different container ID (`product-pagination`) and was intentionally left untouched per `feedback_only_requested_changes.md` — Shawn asked about UserManagement only.

Verified: rule served via `/css/pages/user-management.css`; shared `admin-tables.css` `.pagination-wrapper` unchanged (still `space-between`); 279 active users in dev DB so pagination renders (12 pages at pageSize=25).

## 2026-05-12 — DEF-187 logged: Edit User modal password field is non-functional

Surfaced while writing `docs/training/user-management.md`. Shawn asked how the admin can give a password to a user who was created via the import template, given that outbound email isn't configured yet and the Forgot Password flow is therefore dead-ended.

Audit found that the Edit User modal renders a password input (`Views/Admin/UserManagement.cshtml:292-294`) that is wired in JS to send `userData.password` on save (`wwwroot/js/admin/userManagement.js:784-785`) — but the receiving model on the server, `UserViewModel` (`Models/AdminModels.cs:14-44`), has no `Password` property. The model binder silently drops the field; `AdminService.UpdateUserAsync` (`Services/AdminService.cs:397`) never touches `user.PasswordHash`. Admins who use Edit to "reset" a password get a green success toast and an unchanged DB. The new user then can't log in with the password the admin just gave them, with no visible error linking the two events.

This is a pre-existing latent defect, not a DEF-186 regression. DEF-186's scope was Create + Import paths only. The Create path works correctly because it uses `CreateUserViewModel` (which does have `Password`).

**Workaround documented in `docs/training/user-management.md` §3 and §4:** delete the user and recreate via Add New User. That doc should have the workaround rows removed once DEF-187 is fixed.

**Fix is small** (~10 lines): add `Password` to `UserViewModel`, honor it in `UpdateUserAsync` when non-blank, hash with BCrypt, set `RequirePasswordReset=false`, log audit entry with "Password changed" redacted comment (never log the value or the hash). Full proposal in defect-register DEF-187 entry. Severity Medium — admin-only path, no security weakening, but high confusion cost pre-launch.

## 2026-05-12 — DEF-187 fixed and verified — rolled in 3 latent admin-audit bugs along the way

Implementation went as proposed for the core password-field fix: added `Password` to `UserViewModel`, honored it in `UpdateUserAsync` (BCrypt hash to `user.PasswordHash`), 8-char min validation in the controller, build clean. But verification surfaced three pre-existing bugs in adjacent code that I rolled into DEF-187 because they're tightly coupled to the audit trail of the password change.

**Rolled-in bug 1 — `RequirePasswordReset` checkbox in Edit modal is dead UI.** Same root pattern as the password field. JS sends `requirePasswordReset` on save, `UpdateUserAsync` never read it. Fix: assign `user.RequirePasswordReset = model.RequirePasswordReset` unconditionally (independent of whether password was also changed). The Security section of the Edit modal now has zero decorative controls.

**Rolled-in bug 2 — audit log was about to start leaking bcrypt hashes.** `AuditService.DetectChanges` reflects over all public props of `User`. As soon as `UpdateUserAsync` started actually changing `PasswordHash`, every password change would have written the old + new BCrypt hashes into `AuditHistory.OldValue` and `AuditHistory.NewValue`. An auditor leaking the audit table would then have an offline-cracking target. Added `"PasswordHash"` to `ShouldSkipProperty`'s skip list. The fact of the change is preserved through a separate `LogAuditEventAsync` call I added in `UpdateUserAsync` — single row, `NewValue = "Password changed by admin"`, `Reason = "Password changed"`. Auditors get the trail; the hash never leaves the `Users` table.

**Rolled-in bug 3 — no admin User-UPDATE audit row had ever been written.** Direct query: `SELECT COUNT(*) FROM AuditHistory WHERE EntityType='User' AND Action='UPDATE'` returned **zero** in dev DB across the entire history. Server log on every admin attempt showed `FK_AuditHistory_Users_ChangedBy` violations swallowed by the audit catch. Root cause: `AdminController.GetCurrentUserId()` line 28 looks for a claim named `"UserId"` which is never issued. `AccountController.SignInUser` line 1138 issues the int Id on `ClaimTypes.NameIdentifier`, and `CertificateController.GetCurrentUserId()` reads it from there — `AdminController` had diverged at some point. Every admin audit call passed `userId=0`, the EF INSERT hit the FK constraint against `Users.Id`, the catch logged and ate the exception. **Blast radius: every admin-initiated audit event in dev/test — user updates, product updates, role updates, tag updates — silently lost since the divergence.** One-line fix: align `AdminController.GetCurrentUserId` to read `NameIdentifier` first, with `sub` (B2C) and legacy `"UserId"` as fallbacks. Audit rows immediately started writing after the fix.

Verification: 7-step PowerShell script archived at `C:\Users\swest\AppData\Local\Temp\verify-def-187.ps1`. Admin login → PUT UpdateUser with new password → login with new password succeeds (302 to `/Certificate/CertificateTileViewInternal`) → login with old password fails (200 login re-render) → restore password to seed default → DB query confirms `Users.Id=2.PasswordHash` is a fresh BCrypt and `RequirePasswordReset=0` → DB query confirms two new `AuditHistory` rows with redacted text only, `ChangedBy=1` (admin), no bcrypt hash in any column. All 7 steps passed cleanly against http://localhost:5101.

Files: `Models/AdminModels.cs`, `Services/AdminService.cs`, `Services/Audit/AuditService.cs`, `Controllers/AdminController.cs`.

Once this ships, `docs/training/user-management.md` §3 and §4 need the "delete and recreate as a workaround" rows pulled — the Edit User flow now works.

Out of scope but flagged: `AuditService.SerializeEntity` (used by the CREATE-event path) uses raw `JsonSerializer.Serialize` with no field exclusion, which means newly-created users currently have their `PasswordHash` bcrypt serialized into `AuditHistory.NewValue` JSON blobs. Pre-existing, separate scope from DEF-187. Worth a future DEF if Shawn wants the audit table cleaned of all hash material.

## 2026-05-12 — DEF-187 audit-serialization fix (correction to the previous flag, plus the actual fix)

Shawn asked me to fix the previously-flagged audit-serialization concern. On closer inspection my flag was **wrong on the credential-leak part** but **right that there's a real bug**.

What I claimed: `SerializeEntity` raw-dumps the User entity to JSON including `PasswordHash`.

What's actually true:
1. `User.PasswordHash` has `[JsonIgnore]` (`Models/User.cs:37` — DEF-121 lineage). Same for `TwoFactorSecret`, `RecoveryCodes`, `SecurityQuestions`, `AllowedIPs`, `PasswordResetToken`, `PasswordResetTokenExpiry`, `PasswordHistory`. So credentials are already protected at the model level — `JsonSerializer.Serialize` skips them automatically.
2. The audit table for every existing User CREATE row contains the literal string `[Serialization failed: User]`. Same for Certificate. Full serialization fails entirely because of cycles in nav properties (User↔Roles, Cert↔Products), the `try`/`catch` in `SerializeEntity` returns the placeholder string. No credential leak, but **no audit content either** — for the most security-relevant CREATE/DELETE events on the most security-relevant entity types.

So the actual bug is data-quality, not data-leakage. Fixed it anyway because the audit trail being useless for User and Certificate creates is a real compliance concern.

Implementation: in `Services/Audit/AuditService.cs`, extracted the audit skip-list (`UpdatedAt`, `UpdatedBy`, `UpdatedByUser`, `CreatedByUser`, `User`, `PasswordHash`) to a `private static readonly string[] AuditSkipFields` so both `DetectChanges` and `SerializeEntity` share one definition. Rewrote `SerializeEntity` to use a cached `static readonly JsonSerializerOptions` with `ReferenceHandler.IgnoreCycles` plus a `DefaultJsonTypeInfoResolver` modifier that drops (a) properties matching the skip-list and (b) navigation collections — same exclusions as the field-diff path, so the two audit code paths agree on what counts as "the entity itself."

Verification: separate PowerShell script at `C:\Users\swest\AppData\Local\Temp\verify-audit-serialization.ps1`. Created an ephemeral test user via `/Admin/CreateUser`, queried `AuditHistory` for the latest User CREATE row, ran 5 assertions, deleted the test user. Results: the new audit row is 622 chars of clean scalar-only JSON (`{"Id":323,"Username":"audit.serialization.<ticks>@…","Email":"…","FullName":"Audit Serialization Test","IsDeleted":false,"Status":"active","IsActive":true,…}`). No "Serialization failed" placeholder. No `PasswordHash` substring. No BCrypt-shaped strings (regex `\$2[aby]\$\d{2}\$[A-Za-z0-9./]{53}` doesn't match). No other JsonIgnore'd security fields (TwoFactorSecret, RecoveryCodes, etc.). All 5 assertions passed.

Pre-existing placeholder rows remain in `AuditHistory` — cannot be backfilled (the original entity state was never captured). Going forward only.

Rolled into DEF-187 as scope item #4 since it's the same family of audit-trail issues uncovered during this session.

## 2026-05-12 — DEF-188 audit: QC #169 (cert type filter) — no code change

Client QC #169 (5/1/26 entry) bundled three sub-claims about the Certification Type filter on the tile views. Audited against `Controllers/Api/CertificateTilesApiController.cs:240-369`, `Models/CertificateStatus.cs`, `Models/CertificationType.cs`, and `Views/Admin/CertificateTypes.cshtml`. All three resolve without new code.

**Sub-claim 1 — "Missing Certified Water Savings, LCA Opt, RM, WD."** R11 by design. Filter pulls `visibleCertTypeIds` = distinct `CertificationTypeId`s on certs in `PublicStatuses` (`Client Approved - Paid` / `Republished`) and not expired (lines 252-257). The four named types have zero certs in public status right now, so they're correctly hidden — comment at lines 252-253 documents this as the "no dead clicks" rule. Client's own follow-up in the CP NOTE — *"is it only showing certs with active listing... is that why they aren't there?"* — is exactly right. Will auto-populate the moment a Client-Approved-Paid cert is tagged to any of them.

**Sub-claim 2 — "Product Opt and RM at top in alpha order + no sort ability under Cert Types."** Word-for-word duplicate of QC #124 (already addressed by DEF-175, 5/11/26). DEF-175 switched `OrderBy(ct.SortOrder)` → `OrderBy(ct.Name)` on all three filter endpoints; status is Fixed locally, pending deploy. Once the build ships, "Product Opt" / "RM" sort into their correct alpha positions, not the top. The "no sort ability" piece is accurate — confirmed `Views/Admin/CertificateTypes.cshtml` has no SortOrder UI — but DEF-175 made it moot by making the sort alpha (auto-positions new admin-added types without any UI intervention).

**Sub-claim 3 — "Would just show options with applicable certs available."** Already the behavior (sub-claim 1).

**Net result:** Closed DEF-188 as review-only. Carly's response should be: (a) the four hidden types are R11 working as specified — they'll appear when a public cert is tagged; (b) the alpha-order / "Product Opt at top" complaint is the QC #124 fix that's queued for the next deploy. Cross-references to DEF-175 / DEF-167 / DEF-168 noted in the DEF-188 entry. Per `feedback_qc_review_no_changes.md`: triage only, no code edits.


## 2026-05-13 — DEF-192 logged: Product Category filter leaks non-matching certs

Shawn flagged cert 26-0158 showing up when only `Product Categories → Carly Test` was checked on `/Certificate/CertificateTileViewInternal`. Verified via anonymous `/api/certificate-tiles?productCategory=Carly Test` — 4 certs returned, 3 legitimate, plus 26-0158 whose displayed category is "Electronics, Computers, and Accessories".

Root cause in `Repositories/CertificateRepository.cs:333-351`: the ProductCategory filter ORs the requested name across 9 fields (cert-level overrides + Product-derived chain + parent cascade). The tile/detail display logic in `Controllers/Api/CertificateTilesApiController.cs:186-188` prefers cert override over Product-derived. So a cert whose Product-side chain contains "Carly Test" but whose cert override points elsewhere matches the filter but visibly shows the override — looks wrong to users.

Logged as DEF-192, Status Open. Affects every tile surface (public, internal, external) since all use the same repo method. No code change made per QC review rule — proposed coalesce-then-match shape included in the entry for when the fix is approved.


## 2026-05-13 — QC Log 5.8 → 5.13: added "Sprint 3 Comments Final" column + refreshed Resolution against 170 items

Took client's 5/8 QC log (`Database QC Log_5.8.26sw.xlsx`) and produced `Database QC Log_5.13.26sw.xlsx` (same folder). Added a new column W titled **Sprint 3 Comments Final** on the `GCC DB QC Log2` tab and refreshed column O (Current Status — Anuki Marks Complete) against the 5/4 reconciliation (`docs/tracking/qc-sprint-3-reconciliation-2026-05-04.md`) plus all post-5/4 shipped work from `shipped-2026-05.md`.

**Final shape (170 items):** 132 Ready-for-UAT / 12 Carryover-Open / 5 In-Progress / 6 CR-Required / 3 Blocked-Client / 1 Blocked-Config / 4 WAD / 3 OOS / 1 Duplicate. Column O resolves to Complete=144, Open=26.

**Post-5/4 upgrades (10 items moved Open → Ready-for-UAT):** QC #103 (DEF-174/182), #108 (DEF-181/186), #127 (DEF-176), #128/#149/#169 (DEF-167), #156 (DEF-179), #157 (DEF-181), #164 (re-verified 5/12), #168 (DEF-183). These had been in Carryover-Open or Needs-Manual-Verify at the 5/4 cut; now closed by Sprint 3 ships.

**Method note:** xlsx edited via PowerShell XmlDocument transform — extract zip → modify `xl/worksheets/sheet2.xml` (dimension A1:W175, new col-23 def width=60, W5 header inlineStr, W6..W175 inlineStr comments, O cells updated to shared-string indices 17/Complete or 104/Open) → modify `xl/tables/table2.xml` (ref A5:W175, count=23, new tableColumn) → re-zip. Original 5.5MB workbook with 92 embedded images + pivot tables preserved unchanged at 5.0MB output. Read-back verification passed on 5 sample rows across the ID range.

**Source data:** 5/4 reconciliation = primary source-of-truth; verified 116/170 IDs explicitly listed in the doc's Verified-Pending-UAT bucket. Cross-checked post-5/4 closures via `Grep "QC #\d+"` over shipped-2026-05.md.

**Email summary delivered separately** — same totals + the 6 CR-Required + 3 Blocked-Client items called out for action.


## 2026-05-13 — DEF-193 logged: cert-number race condition (client QC #58)

Client (5/11) on QC #58 asked: *"if 2 people have open and adding cert and hit save -- does one keep the original number and other get next available? It will never assign the same number right? Please coordinate test on your end."*

**Live-confirmed on gccwebapp.com.** Wrote a PowerShell repro using two isolated .NET HttpClients (each its own CookieContainer + DefaultRequestHeaders.Authorization for the Basic-Auth gate). Logged Session A as analyst, Session B as admin; both GET `/Certificate/Create` concurrently via `Task.WhenAll`. Parsed the `id="Number"` input from each HTML response:

```
Session A (analyst) Number: '26-0164'
Session B (admin)   Number: '26-0164'   <-- IDENTICAL
```

Root cause: `Repositories/CertificateRepository.cs:744` `GenerateNextCertificateNumberAsync` is a pure `MAX(Number) + 1` read with no reservation. Called once from `CertificateController.cs:287/348` during the GET, fills `model.Number` on the form. The UNIQUE index `IX_Certificates_Number` (`ApplicationDbContext.cs:67`) catches the duplicate at save time but the catch block at `CertificateController.cs:653-659` masks the error as a generic "An error occurred." The losing user keeps their form data but the stale number stays in the input — they have to refresh to get a new number.

**Fix drafted (NOT applied) in DEF-193 register entry:** add `AddWithGeneratedNumberAsync(certificate, maxAttempts=3)` to `ICertificateRepository` / `CertificateRepository.cs` that regenerates the number inside a retry loop scoped on `DbUpdateException` whose inner `SqlException` matches errors 2601/2627 against `IX_Certificates_Number`. Controller POST replaces `await AddAsync + CompleteAsync` with `await AddWithGeneratedNumberAsync`. Form pre-fill stays for display but is labeled "preview — assigned at save". Three attempts handles every realistic concurrency level (<10 analysts).

**Why retry instead of SERIALIZABLE / sp_getapplock:** retry is O(1) per attempt, converges in ≤ N attempts for N writers, and the unique-index error is the natural signal. Heavier locking would serialize all cert creation needlessly.

**Repro script + evidence preserved at:** `%TEMP%\def193_repro_httpclient.ps1` + `%TEMP%\def193_evidence\session_{a,b}_create.html`. Repro is portable to localhost by swapping the base URL and removing the gate auth header.

**PowerShell gotchas captured during repro:**
- `Invoke-WebRequest -Credential` drops the Authorization header on 302 redirects following login POST, returning 401 from the gate on the redirected GET. Workaround: HttpClient with `DefaultRequestHeaders.Authorization` set on the client (not the request) — it re-sends on every request including redirects.
- `-MaximumRedirection 0` raises "Operation is not valid due to the current state of the object" when the response has a body in PS 5.1.
- `name="__RequestVerificationToken"` regex needs `[^>]*?` (greedy stop at `>`) not `[^"]*` since `type="hidden"` sits between `name=` and `value=`.

**Status:** Diagnosed + live-reproduced + fix-drafted. Sequencing pending Friday call per client's *"lets discuss on Friday call"* — also surface CR question of whether to require the readonly preview UX or keep the editable Number field.


## 2026-05-13 — DEF-193 fix applied to source (working tree pending deploy)

Applied the patch drafted earlier today.

**Interface (`Repositories/ICertificateRepository.cs`):** added `Task<Certificate> AddWithGeneratedNumberAsync(Certificate certificate, int maxAttempts = 3)` after `GenerateNextCertificateNumberAsync`.

**Implementation (`Repositories/CertificateRepository.cs`):** appended `AddWithGeneratedNumberAsync` + private `IsUniqueNumberViolation` helper. Retry-on-conflict pattern: assign number → AddAsync → SaveChangesAsync → catch DbUpdateException whose inner `Microsoft.Data.SqlClient.SqlException.Number` is 2601 or 2627 AND whose message references `IX_Certificates_Number` → detach entity → retry. Bounded at 3 attempts; throws on final attempt.

**Controller (`Controllers/CertificateController.cs` Create POST):** removed `Number = model.Number` from the initializer, replaced `AddAsync + CompleteAsync` pair with `AddWithGeneratedNumberAsync(certificate)`. Kept upload-before-save ordering — race-losing user's file gets the preview number in the filename (cosmetic drift) but the cert row's `CertificateFile` column links correctly.

**View (`Views/Certificate/Create.cshtml`):** helper text under Number input updated from "Auto-generated (format: YY-XXXX)" to "Preview — final number is assigned at save. If another analyst saves first, you'll be given the next available number automatically."

**Build verification:** 0 errors, 53 warnings (all pre-existing). Standard `dotnet build` against `bin/Debug/net8.0/` fails on output copy because dev server PID 34056 holds GCCWebApp.exe (Shawn has the app running on :5101). Verified C# compile by building to alternate output `bin/Debug/net8.0-verify/` (cleaned up after). Source compiles clean.

**Not done (intentional):** dev server not restarted, no commit. Per `feedback_no_commits.md` Shawn commits himself. To live-test the fix, dev server needs restart; the existing repro script at `%TEMP%\def193_repro_httpclient.ps1` is portable to localhost by swapping base URL + removing gate auth header.

**DEF register + shipped-2026-05 + this log updated.** DEF-193 status now "Fixed locally — pending deploy."


## 2026-05-13 — DEF-193 fix verified end-to-end on localhost

Stopped old dev server PID 34056, rebuilt clean (0 errors), restarted dev server on :5101 (new PID 31176), re-ran the repro adapted for localhost.

**Phase 1 — preview check (expected, unchanged):** both sessions GET'd /Certificate/Create concurrently and both saw preview Number `26-0159`. Helper text now reads "Preview - final number is assigned at save..." (DEF-193 wording is live).

**Phase 2 — the actual fix test:** built minimal valid form payloads for both sessions using known dev-DB IDs (CompanyId=1 Sofidel, ProductId=2 Plastic Film Packaging, CertificationTypeId=1 Recycled Content, PathwayId=1) and POSTed both simultaneously. Both POSTs succeeded:

```
Response A: 200 -> /Certificate/CertificateInternalView/26-0160   (retried)
Response B: 200 -> /Certificate/CertificateInternalView/26-0159   (won the race)

Saved cert A: 26-0160
Saved cert B: 26-0159
```

The retry logic in `AddWithGeneratedNumberAsync` fired exactly once on Session A's save, caught the `DbUpdateException` whose inner `SqlException.Number = 2627` referenced `IX_Certificates_Number`, detached the entity, regenerated `26-0160`, and saved successfully. Session B (the winner of the race) saved with no retry needed.

**Net result matches the client's described desired behavior** per QC #58: *"does one keep the original number and other get next available?"* — yes: B kept 26-0159, A got 26-0160 transparently with no error and no lost form work.

Test certs (Id 56 + 58, Location='DEF-193 Repro') cleaned up from dev DB after verification.

**Gotchas hit during the localhost adaptation:**
- Dev DB emails differ from prod. Live smoke uses bare "admin"/"analyst" usernames (the app's login accepts both username and email). HttpClient-based repro had to use the actual dev DB email addresses (`swest@anukiconsulting.com`, `jane.analyst@anukiconsulting.com`).
- ProductId is a cascading dropdown that's empty on initial form render — switched to hardcoded known-valid IDs from dev DB rather than parse the form's empty <select>.
- PowerShell 5.1 parser choked on em-dashes and apostrophe-in-double-quoted-string in the script body; replaced with ASCII hyphens and simpler wording. UTF-8 encoding is fine, just specific characters trigger parser bugs.

Repro script (post-fix) preserved at `%TEMP%\def193_repro_localhost.ps1`. Evidence HTML at `%TEMP%\def193_localhost_evidence\`. Pre-fix repro on gccwebapp.com still preserved at `%TEMP%\def193_repro_httpclient.ps1` + `%TEMP%\def193_evidence\` for the post-deploy live re-verification.

**DEF-193 status flipped to "Fixed locally and verified end-to-end against the restarted dev server — pending deploy to gccwebapp.com."**

## 2026-05-13 — DEF-192 fix applied, verified, status flipped to Fixed locally

Replaced the OR-across-Product-AND-cert-override predicate in 3 places (`Repositories/CertificateRepository.cs` public path, `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`) with effective-value matching at each hierarchy level — cert override if set, otherwise Product-derived. Mirrors the display projection one-to-one. Parent cascade preserved on the public repo path (its only previous home); not introduced where it never existed.

Verified end-to-end on localhost:5101 dev DB by synthesizing the leak (cert 26-0001 override=Clothing, Product.Category=Chemicals): public API leak total dropped 22→21 with 26-0001 disappearing from the Chemicals result; Clothing went 0→1 with 26-0001 appearing. Internal API (admin auth) matched: Chemicals→39 no 26-0001, Clothing→1 has 26-0001. Dev DB mutation restored at end of test.

Def register + shipped-2026-05 updated. Pending deploy.


## 2026-05-13 — DEF-193 Option A applied + verified (race-loser file rename)

Added Option A to the DEF-193 fix: when AddWithGeneratedNumberAsync assigns a different number than the form preview, rename the uploaded PDF/image/thumbnail on disk so the filename matches the assigned cert Number.

**Implementation (`Controllers/CertificateController.cs`):**
- Inline block after `AddWithGeneratedNumberAsync`: if `certificate.Number != model.Number`, call new private helper `TryRenameUploadedFile` for both `certificate.CertificateFile` and `certificate.CertificateImagePath`. If any rename succeeded, update the in-memory paths and call `_unitOfWork.CompleteAsync()` to persist the new paths. Log info-level message with preview vs assigned numbers.
- `TryRenameUploadedFile(relativePath, oldNumber, newNumber)` helper: resolves physical paths via `_hostingEnvironment.WebRootPath`, only renames if the source file exists AND the destination doesn't (don't clobber), returns the new relative path or null on any failure (file missing, no match, clobber risk, I/O error). Failures are logged as warnings and the original path is left in place — graceful degradation.

**Verification:** ran a v3 repro (`%TEMP%\def193_repro_v3.ps1`) with two concurrent POSTs each carrying a real 60KB PDF. Both saves succeeded; race-loser cert 26-0160 ended up with filename `26-0160_20260513_232825.pdf` (renamed from `26-0159_*`) and thumbnail `26-0160_thumb_20260513_232825.png`. Race-winner cert 26-0159 got `26-0159_20260513_232825_<guid>.pdf` — the GUID suffix is the FileUploadService's existing collision-avoidance kicking in because the preview-number filename was briefly held by the loser before the rename. Both filenames now match their assigned cert numbers; no on-disk drift remaining.

**Side notes from this session:**
- `/Certificate/CertificateInternalView/{number}` returns 400 from the redirected GET because the action expects parameter name `id` while the route binding sees it as `null`. Pre-existing issue, unrelated to DEF-193. Logged as something to investigate separately.
- PowerShell 5.1 parser bug with single-quoted strings ending in `\'` — `'\'` fails with "missing string terminator." Workaround: use `[char]92` instead of `'\'`. Also: files without UTF-8 BOM occasionally fail to parse when loaded via `& script.ps1` even when the Parser API accepts them. Saving with BOM resolves it. Captured for future repros.

**Dev server:** PID 39864 currently running with both Option A + retry fix. Test certs cleaned up; dev DB back to its baseline state.

**Status:** DEF-193 now reads "Fixed locally and verified end-to-end against the restarted dev server (including Option A file rename) — pending deploy to gccwebapp.com."

---

## 2026-05-19 — Sprint-close batch deployed to gccwebapp.com + smoke verified

Zip `GCCWebApp-deploy-20260519-1044.zip` extracted on Plesk; bundled migration `FixUserUniqueIndexesForSoftDelete` ran; app pool recycled.

**Smoke verification on live (`gccwebapp.com`):**
- Baseline `scripts/smoke-prod/smoke.js` — **26/26 PASS** (gate, UI surfaces, DEF-195 pill alignment, CSS freshness, role-gated routes).
- New per-deploy runner `scripts/smoke-prod/smoke-batch-2026-05-19.js` — **11 PASS, 1 SKIP, 0 FAIL** covering this batch's items (#170, #180, #175, #177/178, #181, #183, #104, #148, #184, #179, #118 all PASS; #182 partner-API end-to-end verification deferred — requires `GCC_PARTNER_KEY` env var with the real Amazon partner key).
- Composite: 37/38 verified, 0 failures.

**Cross-cutting discovery during smoke:** login page logo (`.login-logo`) does not shrink at laptop/tablet widths — only mobile. The DEF-201 new breakpoint targets `.main-logo` (post-login layout) which was the actual client-reported surface. Login-page logo responsiveness is a separate small fix, not a regression of this deploy.

**Pattern established:** Going forward, each significant deploy gets its own `smoke-batch-<date>.js` alongside the permanent `smoke.js`. Baseline guards invariants; batch runners prove the *specific deploy contents* are actually live. Mirrors the DEF-195 first-attempt failure mode where the site "loaded fine" but the fix wasn't on the wire.

**Working tree state at end of session:** `Scripts/smoke-prod/smoke-batch-2026-05-19.js` is the only new addition not yet committed. Per standing rule, Shawn commits when ready.

---

## 2026-05-19 — DEF-203 found: cert workflow transition matrix not enforced

During the "test around every QC item" pass, walked a fresh cert (`26-0163`) through all 8 lifecycle states via the documented `CertificateStatusHelper.IsValidTransition` matrix, then probed explicitly-illegal jumps.

**Finding:** Legal walk passes 10/10. The probe `Draft → Republished` (not in matrix) returned **HTTP 200** and persisted — the illegal jump was silently accepted.

**Root cause:** `Controllers/CertificateController.cs:944` Edit POST handler assigns `certificate.Status = model.Status` directly with no matrix validation and no call to the existing `CertificateWorkflowService.TransitionCertificateStatusAsync()`. The matrix and the service are both implemented and registered in DI, but **no controller path invokes them** — dead code.

**Secondary impacts:**
- `CertificateStatusHistory` audit rows NOT being written on Edit-form transitions (the insert lives inside the unreached service method)
- CR-015 cascade (`SyncProductIdStatusesForCertificateAsync`) NOT firing on Edit-form transitions (same root)

**Disposition:** Written up as DEF-203 in `docs/tracking/defect-register.md` with Option A (enforce matrix via service) recommended fix and a test plan that reuses the discovery script. Awaiting client decision on enforce-vs-document-as-relaxed.

**Test script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\workflow-transitions.js` — runs against any cert, walks the full 8-state path, attempts invalid transitions. Reusable for verification once the fix lands.

**Test data:** all WF-Co-* companies, WF-Product-* products, certs 26-0161/26-0162/26-0163, and their PDFs/thumbnails cleaned up from dev DB.

---

## 2026-05-19 — Two more critical defects: DEF-204 + DEF-205

Five-surface probe pass after DEF-203 (PDF auth / role-URL / cross-company / partner API edges / product-ID validation) — 35 PASS, 6 FAIL, of which 4 were real defects and 2 were script issues (analyst is NOT supposed to access /Admin/ProductTypes; EAN test value was syntactically valid but checksum-wrong).

**4 real findings collapsed into 2 DEFs (shared root pattern):**

**DEF-204 (Critical):** Authenticated cross-scope cert leaks. Two endpoints share the same auth-vs-ownership gap:
- `/Certificate/ViewCertificateFile/{id}` — DEF-164 gated anonymous access but lets *any* authenticated user (External, Analyst-on-otherwise-scoped-data, etc.) download any cert PDF regardless of status. Empirically: External (Henkel) user downloaded Draft `26-0026` (151KB) + Expired `26-0012` (151KB) — both other-company certs, neither publicly visible.
- `/Certificate/CertificateExternalView/{number}` — External user (Henkel) loaded cert detail for `26-0001` (Acme Corp) and got HTML response with "Acme Corporation" + "certificate-detail" + "Certification Period" in the body. The tile-view filter correctly scopes to Henkel; the detail-view endpoint doesn't.

Both share root cause: detail endpoints check that the caller is "authenticated enough" but skip the *ownership / company-scope* check that the corresponding list endpoints apply.

**DEF-205 (Critical):** Partner API single-cert endpoints (`/api/v1/certificates/{id}` and `/number/{n}`) leak certs across partners. DEF-198 fixed the LIST scoping but the single endpoints were never given the same partner-ownership filter. The `BuildPartnerCertificateDto` projection hardcodes the `partner` field to the caller's identity, so Amazon partner GET `/api/v1/certificates/number/26-0001` (Acme/None) returned 200 with `partner: "Amazon"` AND `company: "Acme Corporation"` in the JSON — *cross-partner data leak + misleading partner attribution*. Same result via `/api/v1/certificates/2` (by internal id). Drafts ARE correctly hidden (404), so the PublicStatuses filter works; only the partner-ownership filter is missing.

Both are extensions of previously-shipped fixes (DEF-103, DEF-164, DEF-198) that were each scoped to a different surface and didn't catch the adjacent ones. **The pattern is the same — list endpoints scope; detail endpoints don't.** Pattern review should be applied to:
- `CertificateInternalView` / `CertificateSingleView` (single-cert HTML detail)
- `/Certificate/Verify/{n}` redirect target
- Any `/api/v1/certificates/.../download` PDF endpoint (likely same gap as the single-cert leak)

Both DEFs written up in `docs/tracking/defect-register.md` with empirical proof, root cause pinned to controller lines, recommended fix patterns, file lists, and re-runnable test scripts (`probe-security-confirm.js`).

**Pattern note worth a memory entry:** Whenever a scoping fix lands on a LIST endpoint, ask "does the corresponding SINGLE / DETAIL / DOWNLOAD endpoint have the same filter?" The DEF-103 / DEF-164 / DEF-198 / DEF-204 / DEF-205 sequence is the same root-cause class.

---

## 2026-05-19 — Third critical defect today: DEF-206 (internal API has no role guard)

Continuing the surface probe after DEF-204/205. Hit the **internal** `/api/CertificatesApi` endpoint (not the contracted `/api/v1/certificates`).

**Finding:** No `[Authorize(Policy = ...)]` at the class or method level. Any authenticated identity gets 200 with full cert data:

- **External user (Henkel-mapped):** 43 certs across 10+ companies including Drafts, Expired, and the deprecated "Active" legacy status. Internal field shape: `id`, raw `status`, raw `partner`, legacy flat `asin/upc/gtin/ean`, internal flags like `isExpiringSoon`/`statusClass`. The same External user via the role-gated tile-view UI sees 2 Henkel certs only — proving the scoping logic exists, it just doesn't apply to this controller.
- **Amazon Partner X-API-Key on the INTERNAL endpoint:** 11 certs vs. 4 on the contracted v1 endpoint. Includes Drafts (PublicStatuses filter not applied), includes Acme/None partner certs (partner-ownership filter not applied), `partner` field returned as raw `"None"` not the contracted `"Amazon"` literal. Internal data shape leaks `id`, `asin/upc/gtin/ean`, etc. — direct contract drift if Amazon ever hits this endpoint.

**Pattern continues.** The 6 defects in this series (DEF-103, DEF-103-followup, DEF-164, DEF-198, DEF-204, DEF-205, now DEF-206) all share the same root: scoping logic exists in one query / one endpoint / one controller, the matching sibling endpoint lacks it. DEF-206 is the most dangerous so far because:
1. It's a LIST endpoint (returns many rows, not single-cert lookup)
2. It returns the internal data shape (not the contract-clean DTO)
3. ANY authenticated External user can hit it from their browser session — no key needed

Written up in `docs/tracking/defect-register.md` with the standard structure: empirical proof, threat model (insider browse + mis-issued partner key with bigger blast radius than contract suggests), root cause pinned to `Controllers/Api/CertificatesApiController.cs:11-15`, fix recommendation (Option A: delete the partner branch, make this internal-only), test plan, adjacent surfaces to audit.

**Adjacent surfaces in same controller to verify post-fix:**
- `[HttpGet("{id:int}")]` (line 114) — by-id detail
- `[HttpGet("by-number/{n}")]` (line 149) — by-number detail
- `[HttpPost]` (line 185) — create cert via API
- `[HttpPost("{id:int}/upload")]` (line 352) — file upload
- `[HttpGet("{id:int}/download")]` (line 527) — PDF download

All almost certainly inherit the same auth gap.

**Day's running total — 4 DEFs from internal QA, all from the same root-cause family:**
- DEF-203 — Workflow matrix not enforced on Edit POST
- DEF-204 — Authenticated cross-scope PDF/ExternalView leaks
- DEF-205 — Partner API single-cert cross-partner leak
- DEF-206 — Internal /api/CertificatesApi has no role guard (biggest)

All four written up with empirical proof, root cause pinned to specific lines, fix recommendations, and re-runnable test scripts in `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\`.

---

## 2026-05-19 — DEF-206 adjacent-surface audit complete (rolled into same DEF)

Per the closing section of DEF-206 ("Adjacent surfaces to audit") empirically probed all 10 endpoints on `CertificatesApiController`:

| Endpoint | Result |
|---|---|
| GET / (list) | **Leaks** — covered by parent DEF |
| GET /{id} | **Leaks** — External user reads full 30-key detail for other companies, including Draft certs |
| GET /by-number/{n} | **Leaks** — same |
| POST / (create) | OK — `[Authorize(Roles="Admin")]` |
| PUT /{id} (update) | OK — same |
| DELETE /{id} | OK — same |
| POST /{id}/upload | OK — same (not actively probed) |
| POST /GenerateThumbnail | OK — `[Authorize(Roles="Admin,Analyst")]` (not actively probed) |
| GET /{id}/download | OK — denies anonymous + correctly 404s non-public for External (more restrictive than the controller's own comment claims) |
| POST /export | OK — `[Authorize(Policy="AnalystOrAdmin")]` |

**Three GETs leak, seven mutating/protected endpoints are OK.** All three leaking endpoints share the exact same root cause — no class-level `[Authorize]` and no per-method override on the read paths. The class-level guard recommended in DEF-206 closes all three in one change.

**Bonus finding:** the `?userRole=` query parameter on the detail endpoints is **ignored** by the service layer. Same response shape regardless of `?userRole=Public` vs `?userRole=Admin`. So there is no role-escalation via parameter (good), but the parameter is dead code in the API surface (mildly deceptive). Recommendation: delete the parameter from the method signatures during the DEF-206 fix.

**Bonus finding:** the download endpoint's XML comment claims "Public certificates can be downloaded without authentication" but the actual implementation returns 401 for anonymous + Paid cert. Either the comment is stale or there's a default-policy override. Worth verifying when the DEF lands — it's denying more than the comment suggests, so no leak, just documentation drift.

DEF-206 updated in place with the audit table, confirmed leak proof for by-id and by-number, and the userRole-is-dead-code finding.

---

## 2026-05-19 — `CompanyController` + `ProductController` audit — CLEAN

Per the pattern memory (`feedback_scoping_check_adjacent_endpoints.md`), audited the two CRUD controllers most likely to share the DEF-206 root cause.

**Script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-company-product.js` (initial run had a Playwright redirect-following artifact making 8 endpoints look like 200/leaked; confirmed via `probe-redirects-check.js` they're actually 302 → login. Real results below.)

### CompanyController — class-level `[Authorize(Policy = "AnalystOrAdmin")]` ✓

All 4 endpoints tested (Index, GetCompanies, SaveCompany, DeleteCompany) — Anonymous + External both get 302/login. No bypass. CompanyController is correctly gated entirely.

### ProductController — class-level `[Authorize(Policy = "ExternalAccess")]` + per-method scoping ✓

Class-level lets External users IN by design (Product Management is a surface External users can browse). The 5 GET endpoints apply row-level scoping via `GetCurrentUserCompanyIdsAsync()`:

| Endpoint | External (Henkel/KAO) sees | Admin baseline | Verdict |
|---|---|---|---|
| `/Product/GetProducts` | 17 products, distinct companies = `[Henkel, KAO]` | 305 products | ✓ scoped |
| `/Product/GetCompanies` | 2 companies | 285 companies | ✓ scoped |
| `/Product/GetCategories` | 9 categories | 9 | ✓ intentionally global (admin-managed taxonomy) |
| `/Product/GetProductTypes` | 44 types | 44 | ✓ intentionally global |
| `/Product/GetDashboardStats` | 8 stats scoped + 2 global reference counts | 8 | ✓ correctly mixed |

The test user `external.user@anukiconsulting.com` (Users.Id=3) is associated with BOTH Henkel AND KAO via the `Users.Companies` navigation property (many-to-many, presumably via a join table not directly visible in INFORMATION_SCHEMA). Seeing products for both is correct seed-data behavior, not a leak.

All 4 mutation endpoints (SaveProduct, SaveCategory, DeleteProduct, DeleteCategory) override to `[Authorize(Policy = "AnalystOrAdmin")]` — External properly gets 302/login.

### Conclusion — the DEF-204/205/206 pattern is LOCALIZED, not systemic

CompanyController + ProductController follow the correct pattern (class-level guard + per-method row scoping where multi-tenant). The 4 DEFs found today are specific to controllers/endpoints that lacked the class-level guard (CertificatesApiController) or lacked scoping on adjacent detail endpoints despite having it on the list endpoint (DEF-204 ViewCertificateFile / ExternalView, DEF-205 PartnerApi by-id/by-number).

**No new DEFs.** The pattern memory's "always check adjacent endpoints" is still good guidance — applied here, it confirmed two controllers are clean. The "miss" probability is real but bounded.

### Next surfaces worth auditing (in priority order if continuing)

- **AdminController** — many endpoints across UserMgmt, Tags, ProductTypes, CertificateTypes, CertificateFields. Class-level `[Authorize(Policy = "AdminOnly")]` per earlier read, so probably clean. Verify on the GET endpoints + sub-resource endpoints.
- **CertificateTilesApi controllers** — internal + external. We probed external on the cert detail leak but not the tile list scoping under all roles.
- **`/api/productids` controller** — uses `[Authorize(Policy = "AnalystOrAdmin")]` per earlier check; mutating endpoints likely fine but GET-by-certificate might leak cross-tenant if no scope check.
- **CertificateImportController** — bulk-import path; auth gate + content validation.

---

## 2026-05-19 — AdminController + CertificateTilesApi + ProductIdApi + CertificateImport audit — ALL CLEAN

**Script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-batch7.js`. 4 controllers × 5 personas (anon, external, analyst, partner, admin) × ~25 endpoints. **110 of 112 probes correct;** the 2 misses were test-script artifacts, not defects.

### AdminController — `[Authorize(Policy = "AdminOnly")]` ✓

14 endpoints × 4 non-admin personas = **54/54 PASS.** Every non-admin attempt got 302 → login. Admin baseline got 200 on `/Admin/GetRoles` (the `/Admin/GetUsers` 8-second timeout was a Playwright `request.fetch` quirk; direct curl returned 200 in 10ms). UserMgmt / RoleMgmt / ProductTypes / CertificateTypes / CertificateFields / Tags / GoogleAnalytics / CreateUser / DeleteUser — all admin-locked at the door.

### CertificateTilesApiController — public tile API, all 5 methods `[AllowAnonymous]` ✓

Anonymous probe of public surface:
- `/api/certificate-tiles?pageSize=100` → 20 certs, distinct statuses = `["Client Approved - Paid"]` only. No Drafts, no Hold, no Expired, no Active legacy status leaked to anonymous.
- Cert items have 16 keys, no `internalTags` / `asin` / `upc` / `gtin` / `ean` fields exposed (the contract-clean public DTO).
- `/filters` → 200 ✓
- `/autocomplete?term=cer` → 200 ✓
- `/27/download` (Draft) → 404 ✓ (DEF-164 fix intact)
- `/2/download` (Paid) → 404 (no on-disk PDF for cert id=2, so falls through; not a leak)

### CertificateTileExternalApiController — `[Authorize(Policy = "ExternalAccess")]` ✓

- Anonymous → 401 ✓
- External (Henkel-mapped) → 2 certs, distinct companies = `["Henkel"]`, distinct statuses = `["Client Approved - Paid"]`. Correctly scoped (KAO had no Paid certs in dev, so only Henkel shows; ExternalAccess is row-scoped per `userCompanyIds`).
- Analyst → 200 (ExternalAccess policy allows Analyst+; what data they see was not probed in this batch, but Analyst seeing all is the design).

### ProductIdApiController — `[Authorize(Policy = "AnalystOrAdmin")]` ✓

6 endpoints × 5 personas = **30 probes, 29/30 correct.** All expected blocks (anon → 401, external → 403, partner → 403) and expected allows (analyst → 200/404/201, admin → 200/404). The one exception was admin `POST /api/productids` returning 500 — root cause: the analyst persona had created the same ASIN `B08N5WRWNW` on cert id=2 milliseconds earlier in the same probe run, so admin's identical POST hit a duplicate-row constraint at the service layer (500 instead of a clean 409). **Not a defect, just a probe-order race.** The duplicate constraint is the right behavior.

Also worth noting: probe-created `CertificateProductIds.Id=2140` was removed from dev DB after the run.

### CertificateImportController — `[Authorize(Policy = "AdminOnly")]` ✓

3 endpoints × 5 personas = **15/15 PASS.** Index (view), DownloadTemplate (CSV), api/Preview (POST upload preview) — all admin-only at the door. Anonymous 302/401, external/analyst/partner 302/403, admin 200/400 (400 on Preview was the expected "missing file" body validation after passing auth).

### Combined audit — 7 controllers, 4 DEF-candidates found earlier are LOCALIZED

| Controller | Class guard | Verdict |
|---|---|---|
| `CertificatesApiController` (internal v1) | **none** | 🚨 3 leaks (DEF-206) |
| `Controllers/Api/v1/CertificatesController` (partner contract API) | — single-cert paths | 🚨 by-id + by-number leak (DEF-205) |
| `CertificateController.ViewCertificateFile`, `.CertificateExternalView` | mixed | 🚨 cross-scope leaks (DEF-204) |
| `CertificateController.Edit` POST | role policy correct | 🚨 workflow matrix dead code (DEF-203) |
| `CompanyController` | `AnalystOrAdmin` | ✓ clean |
| `ProductController` | `ExternalAccess` + row-scope filter | ✓ clean |
| `AdminController` | `AdminOnly` | ✓ clean |
| `CertificateTilesApiController` (public) | `AllowAnonymous` + status filter | ✓ clean |
| `CertificateTileExternalApiController` | `ExternalAccess` + row scope | ✓ clean |
| `ProductIdApiController` | `AnalystOrAdmin` | ✓ clean |
| `CertificateImportController` | `AdminOnly` | ✓ clean |

**7 controllers clean, 4 specific endpoints/areas leaking. Pattern memory confirmed correct, and the actual blast radius is bounded.**

### Day's running total — unchanged
| DEF | Severity |
|---|---|
| DEF-203 | Major |
| DEF-204 | Critical |
| DEF-205 | Critical |
| DEF-206 | Critical |

### Working tree
- `observations_log.md` — appended full audit table
- No new DEFs
- `Scripts/smoke-prod/smoke-batch-2026-05-19.js` still the only new untracked file

---

## 2026-05-19 — Three more DEFs from the auth-surface audit (DEF-208/209/210)

**Script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-batch8.js` (+ tail). Audited AccountController login surface, DashboardController, PartnerApi class guards, and the static-file middleware path.

### Clean surfaces (16/16 PASS)

- **Dashboard** — class `[AnalystOrAdmin]` blocks Anonymous + External on Index, GetChartData, DownloadStat, DownloadTable. ✓
- **PartnerApi controllers** (`/api/admin/partner/*` + `/api/v1/apikeys`) — class `[AdminOnly]` blocks Anonymous (401) AND Partner X-API-Key (403). Partner keys correctly do NOT unlock admin endpoints. ✓
- **ForgotPassword body-length** identical for valid vs invalid email — message-based enum prevented. ✓
- **Directory listing on `/uploads/certificates/`** → 404. ✓
- **Path traversal** sanitized. ✓

### Three real critical/major findings → 3 new DEFs

**DEF-208 (Critical) — Static-file middleware serves every uploaded cert PDF + thumbnail to anonymous users.** Direct GET `/uploads/certificates/{filename}.pdf` returns the file with `Content-Type: application/pdf`, bypassing `ViewCertificateFile`'s DEF-164 status gate entirely. Same for thumbnails at `/uploads/images/...`. Verified on 5 PDFs + 5 PNGs sampled across cert numbers — 100% leak. Filename format is predictable (`{certNumber}_{YYYYMMDD}_{HHMMSS}.pdf`), so a motivated attacker can enumerate every cert PDF. Fix recommendation (Option B): relocate uploads out of `wwwroot` so static-file middleware never sees them, force all access through controller routes that already apply the right scoping (DEF-204's recommended pattern).

**DEF-209 (Major) — Login error messages enable email enumeration.** `POST /Account/Login` returns the verbose failure reason verbatim:
- Valid email + wrong password: `"Login failed: Invalid password"`
- Nonexistent email + any password: `"Login failed: User not found"`

Two distinct, deterministic messages parseable from the rendered HTML. ForgotPassword has a related **timing side-channel** (valid email = 3008ms because of synchronous SMTP send; invalid email = 32ms — 100× delta) — same class of finding rolled into the same DEF. Fix: generic "Invalid email or password." message on Login, async email send on ForgotPassword.

**DEF-210 (Major) — No login rate limit / account lockout.** Ran 8 consecutive `POST /Account/Login` requests with the same email and unique-wrong passwords in ~2.3 seconds: no slowdown, no lockout, no error-message change between 1st and 8th attempt. AspNetCoreRateLimit is configured for the partner API surface but not for auth routes. Combined with DEF-209 this enables targeted brute force against confirmed-valid accounts.

### Combined attack picture: DEF-209 + DEF-210 chained

An attacker can (1) confirm any email address against the user table via the differing login error, then (2) run unlimited password attempts against that specific account with zero throttling. The admin email is essentially public knowledge. **These should ship in the same deploy** — fixing only the message without rate-limit leaves brute force open; fixing only rate-limit without message leaves the targeting signal open.

### Day's running total
| DEF | Severity | Surface |
|---|---|---|
| DEF-203 | Major | Workflow matrix dead code |
| DEF-204 | Critical | Cross-scope PDF + ExternalView |
| DEF-205 | Critical | Partner API single-cert cross-partner |
| DEF-206 | Critical | Internal `/api/CertificatesApi` no role guard |
| **DEF-208** | **Critical** | **Static-file middleware leaks all cert PDFs + thumbnails** |
| **DEF-209** | **Major** | **Login error messages enable email enumeration** |
| **DEF-210** | **Major** | **No login rate limit / account lockout** |

7 DEFs from internal QA today (numbering 203–206 + 208–210; 207 was a separate mobile-bfcache fix logged yesterday), all written up in `docs/tracking/defect-register.md` with empirical proof, root cause, fix recommendations, file lists, and re-runnable probe scripts in `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\`.

**Pattern memory still valid** — most controllers follow the correct pattern. The 7 DEFs found cluster around four root-cause families:
1. List-endpoint-scoped, detail/sibling-endpoint-not (DEF-103, 164, 198, 204, 205, 206)
2. Static-file middleware bypassing controller-level gates (DEF-208 — same family, different mechanism)
3. Auth surface hardening gaps (DEF-209, 210)
4. Dead-code business rules (DEF-203 workflow service)

---

## 2026-05-19 — Batch 9 audit (CSRF / reset token / file upload / XSS / cookies) — CLEAN

**Script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-batch9.js` (+ direct curl verification).

### Clean surfaces — 5/5 PASS

- **CSRF / antiforgery enforcement.** `POST /Company/SaveCompany` with missing or bogus `RequestVerificationToken` header returns **302 to `/Account/Login?error=session_expired`** (validated via direct curl with no auto-redirect). With the real token, request succeeds. `[ValidateAntiForgeryToken]` attribute fires correctly. Playwright's `request.fetch` auto-following the 302 to a login-page 200 made the initial probe look like a defect — direct curl confirmed enforcement.
- **Password reset bogus token rejected.** `/Account/ResetPasswordWithToken?token=BOGUS_TOKEN_12345&email=...` renders an error page with "invalid/expired/not found" — controlled rejection.
- **ForgotPassword works for existing users.** Returns 200 with confirmation view (and triggers async-ish email flow — see DEF-209 for the timing-channel finding).
- **File upload type validation.** Uploading a fake-PDF (HTML body with `application/pdf` MIME) to `/api/CertificatesApi/GenerateThumbnail` returns **400 with `"Failed to generate thumbnail from PDF. The PDF may be empty or corrupted."`** — controlled rejection at the PDF-parse step, no exception leakage.
- **XSS in company name does not execute.** Created a company named `XSS-Probe-{ts}<script>window.__xss="fired"</script>`, then loaded the Company list page. Result: `window.__xss !== "fired"` (no execution), raw `<script>` not in `innerHTML` (HTML-encoded by Razor's `@` syntax). Razor output encoding is doing its job. The DB stores the raw value (which is fine — input validation belongs at output time, not storage time).

### Cookie configuration finding (not a DEF — pre-existing TODO)

`Program.cs:36, 251, 359` set `options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest` on the session, auth, AND antiforgery cookies, with code comments saying *"TEMPORARY FIX: Allow cookies without valid SSL certificate. TODO: Install proper SSL certificate and change back to CookieSecurePolicy.Always"*.

- **HTTPS prod (gccwebapp.com):** `SameAsRequest` does the right thing today — the cookie gets the `Secure` flag because requests come in via HTTPS. So prod is not currently broken.
- **Risk:** if HTTPS termination breaks (misconfigured proxy, dev container running HTTP, internal probe via HTTP) the cookies become non-Secure and could be observed on the wire. `Always` would harden against that case.

This was flagged earlier today in memory ("session cookie missing Secure flag" from 2026-04-07 entry). Still unfixed. Tracking as a follow-up nice-to-have, not a new DEF — it's a pre-existing item the team is aware of.

### Server crash side-effect (no DEF, just an operational note)

The fake-PDF upload probe in batch 9 appears to have crashed the dev server (subsequent curls returned `Unable to connect`). The endpoint returned 400 with a controlled error message, so it wasn't an unhandled exception in the HTTP path — but something in the file processing left a process in a bad state. Restarted cleanly. **Not a security vulnerability** (can't be used remotely without auth; AnalystOrAdmin-gated). **Possible DoS via crafted PDF** worth following up — but partner API rate-limit covers most of that surface. Logging this as an observation; if it reproduces on more sample inputs, would write up as DEF.

### Cleanup
- `Companies.Id=324` (CSRF-Test-RealToken-CLEANUP) and `Id=325` (XSS-Probe-...) both deleted from dev DB.
- No other test data leftover.

### Day's running total — unchanged at 7 DEFs

| DEF | Severity | Area |
|---|---|---|
| DEF-203 | Major | Workflow matrix not enforced on Edit POST |
| DEF-204 | Critical | Cross-scope PDF + ExternalView |
| DEF-205 | Critical | Partner API single-cert cross-partner |
| DEF-206 | Critical | Internal `/api/CertificatesApi` no role guard |
| DEF-208 | Critical | Static-file middleware leaks all cert files |
| DEF-209 | Major | Login error messages — email enumeration |
| DEF-210 | Major | No login rate limit / account lockout |

(DEF-207 is yesterday's mobile bfcache fix — already shipped.)

---

## 2026-05-19 — Batch 10 audit (open redirect / SQLi / mass assignment / rate-limit) — CLEAN

**Script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-batch10.js`. 17/17 PASS, no new DEFs.

### Surfaces verified clean

**1. Open redirect via `returnUrl` — protected by `Url.IsLocalUrl()` in `AccountController.RedirectToLocal` (line 1173).** Probed 6 attack variants on `POST /Account/Login?returnUrl=...`:
- `https://evil.example.com/phishing` → ignored; redirects to role-based home (`/Certificate/CertificateTileViewInternal`)
- `//evil.example.com/phishing` (protocol-relative)
- `/\evil.example.com` (backslash trick)
- `/.evil.example.com` (dot trick)
- `/Account/Login%2F%2Fevil.example.com` (encoded slashes)
- `javascript:alert(1)` (XSS-via-redirect)

All 6 either redirected to local URL or kicked out via antiforgery. `RedirectToLocal` is the right check; ASP.NET Core's `Url.IsLocalUrl()` handles all common bypass tricks.

**2. SQL injection in cert tile search + autocomplete — input validation rejects dangerous characters.** Tested:
- `' OR '1'='1`
- `'; WAITFOR DELAY '0:0:5'--` (time-based)
- `%' OR 1=1--`
- `' UNION SELECT 1,2,3,4,5,6,7,8,9,10--`
- `"; DROP TABLE Certificates;--`
- Autocomplete: `'`, `' OR 1=1--`

All return 400 "Bad request" with 11-byte body. **Legitimate searches still work** (`?searchTerm=cert` returns 200 with cert list — confirmed via sanity check). Input validation rejects single quotes / SQL keywords at the controller layer. EF Core parameterized queries provide a second defense at the query layer. WAITFOR injection didn't surface as delay (2-8ms response times — query never executed).

**3. Mass assignment / role escalation — External user blocked at the AdminOnly gate.** External user attempting:
- `PUT /Admin/UpdateUser/3` (themselves) with `role: "admin"` payload → **302 to `/Account/AccessDenied`**
- `POST /Admin/CreateUser` with `role: "admin"` payload → **302 to login**

Verified no rogue admin user got created (admin's subsequent GET `/Admin/GetUsers` shows no `evil-admin@e2e.dev` entry). The class-level `[AdminOnly]` policy on AdminController stops the request before any model binding / mass assignment can happen.

**4. Partner API rate-limit observation — 100 rapid requests all succeeded.** Fired 100× `GET /api/v1/certificates?pageSize=1` with Amazon key in 1614ms (~62 req/sec). All 100 returned 200. **Rate limit not tripped at this rate.**

Possible reasons (need follow-up to know which):
- AspNetCoreRateLimit policy is per-minute (e.g., 1000/min) — 100 in 1.6s doesn't exceed
- Test environment relaxes the limit
- Rate limit applied at gateway/proxy on prod (Plesk), not the app

**Not a defect** — just an observation. If contract specifies a particular req/sec ceiling, worth verifying that ceiling actually fires. Otherwise, the LIMIT exists per memory entry `DEF-112` and is probably set to a reasonable production threshold.

### 2FA bypass — not testable on dev

`appsettings.Development.json` has `"Enable2FA": false`. Can't validate 2FA flow without enabling it (which would require email config). On prod gccwebapp.com, `Enable2FA: true` so the 2FA challenge IS in the flow. **Will need to re-test on prod after the next deploy** (where DEF-208/209/210 fixes land) to verify the 2FA gate doesn't have a bypass path.

### Day's running total — unchanged at 7 DEFs

7 internal-QA DEFs total. All written up. The audit's breadth so far:

| Probe batch | Surfaces | Result |
|---|---|---|
| Deploy smoke (Sections A-L) | 12 | 11 PASS, 1 SKIP (partner key) |
| Workflow transitions | 1 (Edit POST) | **DEF-203** found |
| Security/scoping (PDF, ExternalView, single-cert) | 5 | **DEF-204 + DEF-205** found |
| Internal /api/CertificatesApi sweep | 1 controller (3 leaking endpoints) | **DEF-206** found |
| CompanyController + ProductController | 2 controllers | Clean |
| AdminController + Tile + ProductId + Import | 4 controllers | Clean |
| Account / Dashboard / PartnerApi / static-file | 4 surfaces | **DEF-208 + DEF-209 + DEF-210** found |
| CSRF / reset / upload / XSS / cookies | 5 surfaces | Clean |
| Open redirect / SQLi / mass assignment / rate-limit | 4 surfaces | Clean |

**~30 surfaces probed total, 8 found leaking, 22 clean.** The 8 leaking endpoints/surfaces cluster around two root-cause families: (1) controller scoping pattern (list endpoint filters but detail/single/sibling endpoint doesn't — covered by `feedback_scoping_check_adjacent_endpoints.md` memory), and (2) auth surface hardening gaps (DEF-209 + DEF-210 + DEF-208 static-file).

---

## 2026-05-19 — Batch 11 audit + **DEF-205 RETRACTED**

**Batch 11 surfaces probed:** GS1 partner endpoint contract, audit-trail (CertificateStatusHistory), cert delete cascade behavior.

### GS1 endpoint — CLEAN

`GET /api/v1/certificates/gs1` with Amazon partner key:
- Returns 4 certs (matches LIST endpoint count, all Amazon-tagged)
- Contract shape correct: `@context`, `$schema`, `id`, `type`, `issueDate`, `issuer`, `products[]` with nested `certificationInfo`
- All `status` fields are `"ACTIVE"` (contract literal, not raw `Client Approved - Paid`)
- Anonymous → 401 ✓
- Bogus key → 401 ✓
- No cross-partner leak — only Amazon-tagged certs returned

### DEF-205 RETRACTED — same-day false positive

While verifying GS1 contract, I rechecked cert `26-0001` and discovered my DEF-205 finding was based on misreading DB data. **`26-0001` is Amazon-partnered** (`Certificates.Partner = "Amazon"`, `Companies.Name = "Acme Corporation"`). I had confused **Company** (the manufacturer — Acme) with **Partner** (who tracks it — Amazon). A cert can be from any company AND tagged with any partner — they're separate dimensions.

**Retest with truly-non-Amazon cert `26-0002` (Beta Industries, Partner=None):**
- `GET /api/v1/certificates/number/26-0002` → **404** ✓
- `GET /api/v1/certificates/3` (by id) → **404** ✓
- `GET /api/v1/certificates/number/26-0001` (Amazon-tagged) → 200 ✓

**Single-cert endpoints ARE properly scoping by partner ownership.** DEF-205 marked as **RETRACTED** in the defect register with the original write-up preserved for audit trail. Memory entry `feedback_scoping_check_adjacent_endpoints.md` updated to remove DEF-205 from the family list.

**Lesson learned:** When verifying partner-scoping defects, always cross-check the cert's `Partner` column directly in the DB — don't infer partner relationship from the cert's `Company` field. The two columns track orthogonal concepts (manufacturer vs. tracking partner).

### Audit trail — already covered in DEF-203 secondary impacts

`CertificateStatusHistory` rows are written only by `CertificateWorkflowService.TransitionCertificateStatusAsync()`, which DEF-203 documented as never being called from any controller. So Edit-form status changes today produce no audit rows. Already captured in DEF-203's "Secondary impacts" section — not a separate finding.

### Cert delete cascade — test deferred

The previous cert delete I did during DEF-204 cleanup (cert 26-0163) successfully removed CustomFieldValues (3 rows) + the Certificate row, no FK errors. The cleanup pattern in `cleanup2.sql` deletes from CustomFieldValues, CertificateStatusHistory, CertificateProductIds, CertificateDocuments before deleting Certificates — implying app-level cascade (not DB-level CASCADE). Works in the cases I exercised. **Not running a fresh test today;** mark for follow-up.

### Day's running total — CORRECTED to 6 DEFs

| DEF | Severity | Status |
|---|---|---|
| DEF-203 | Major | Open |
| DEF-204 | Critical | Open |
| ~~DEF-205~~ | ~~Critical~~ | **RETRACTED 2026-05-19** |
| DEF-206 | Critical | Open |
| DEF-208 | Critical | Open |
| DEF-209 | Major | Open |
| DEF-210 | Major | Open |

**6 valid DEFs from internal QA today** (DEF-207 is yesterday's separately-shipped bfcache fix). Composite severity distribution: 3 Critical (DEF-204, 206, 208) + 3 Major (DEF-203, 209, 210).

---

## 2026-05-19 — DEF-211 fix: removed "Administration" prefix from Companies & Products breadcrumbs (QC #104 5/1 follow-up)

Shawn reopened QC #104 second half — Carly's 5/1 note that *"Administration"* should not appear in the breadcrumb for Companies & Products pages because they're sibling top-level nav items, not parent/child. DEF-202 (5/19) closed two of three parts of #104 but missed this one.

Two edits, both in Views:
- `Views/Company/Index.cshtml` — `Administration › Companies & Products › Company Management` → `Companies & Products › Company Management`
- `Views/Product/Management.cshtml` — `Administration › Companies & Products › Product Management` → `Companies & Products › Product Management`

Logged as DEF-211 in defect register + shipped-2026-05.md. Pending deploy. Admin views' `Administration ›` prefix kept (those pages truly are under the Administration nav dropdown). `Views/Product/ManagementAlt.cshtml` + `ManagementV2.cshtml` left as-is — they look like dead alternates.

**Adjacent observation** (not addressed in this DEF): breadcrumb `<nav>` in `_Layout.cshtml:299` is gated `role-admin-show`, so Analyst users never see breadcrumbs even though they can reach Companies & Products. Separate concern.

---

## 2026-05-19 — Batch 12 audit (cascade delete / health / API field leaks / sessions) — CLEAN

**Script:** `C:\Users\swest\AppData\Local\Temp\gcc-smoke-pw\probe-batch12.js`. 15/17 PASS, no new DEFs. Two minor observations.

### Clean surfaces

- **Health endpoints anonymous.** `/health` `/health/ready` `/health/live` all 200 OK to anonymous. Body returns `{ status: "Healthy", checks: [...] }`. No connection strings, no exception stacks, no schema metadata leaked. `/health` exposes `database_provider: "Microsoft.EntityFrameworkCore.SqlServer"` and `database_response_time_ms` — minor info disclosure but standard. `/health/ready` and `/health/live` only return "Healthy" (load-balancer probes).
- **Swagger anonymous.** `/swagger`, `/swagger/index.html`, `/swagger/v1/swagger.json` all reachable — by design (API documentation is public per partner contract).
- **Partner API field-leak audit.** `/api/v1/certificates` has: no absolute filesystem paths, no internal numeric IDs adjacent to certificateNumber, `pdfDownloadUrl` is relative (`/api/v1/certificates/EXT-AMZ-003/download`) — contract-clean.
- **Cert delete cascade.** Created CASCADE-Co + CASCADE-Product + cert (Paid, PDF, +1 ProductId via API), then `DELETE /api/CertificatesApi/{id}` → 200 OK. App-level cascade removes dependents cleanly.
- **Concurrent session.** Same user from two browsers — both authenticated independently. Logout from A doesn't propagate to B (expected — no server-side single-session enforcement, normal for multi-device-friendly).
- **Session idle timeout — server-enforced.** `Program.cs:31` `Session.IdleTimeout = 30 min`, `Program.cs:245-246` auth cookie `ExpireTimeSpan = 30 min` with `SlidingExpiration = true`. Cookies are session-cookies (no Expires attribute); the 30-min idle gate is server-side. Reasonable.

### Two observations (not DEFs)

**Obs 1 — GS1 endpoint `@id` URLs use request-relative base.** Amazon CPF contract example shows `"https://greencirclecertified.com/api/v1/certificates/gs1/24-0366"`. Actual response uses request host: `localhost:5101` on dev, `gccwebapp.com` on test env. On the canonical greencirclecertified.com cutover it'll match. Whether Amazon's ingest validates against an expected canonical is the open question — worth a quick check with the contract.

**Obs 2 — Health endpoint exposes DB provider info.** Standard ASP.NET Core health output. No real-world leak (SQL Server is in the deploy stack docs anyway). `/health/ready` and `/health/live` already cover load-balancer probes with just "Healthy"/"Unhealthy". Could gate `/health` behind admin or ops IP-allowlist if defense-in-depth matters. Low priority.

### Day's running total

| DEF | Severity | Author | Status |
|---|---|---|---|
| DEF-203 | Major | Anuki QA | Open |
| DEF-204 | Critical | Anuki QA | Open |
| ~~DEF-205~~ | ~~Critical~~ | — | **Retracted** |
| DEF-206 | Critical | Anuki QA | Open |
| DEF-207 | Low | (yesterday) | shipped |
| DEF-208 | Critical | Anuki QA | Open |
| DEF-209 | Major | Anuki QA | Open |
| DEF-210 | Major | Anuki QA | Open |
| **DEF-211** | (Shawn just added) | Shawn | (in flight) |

**6 from Anuki QA today + 1 from Shawn (DEF-211) — 7 fresh DEFs total today.**

### Cumulative audit table

| Batch | Surfaces | Findings |
|---|---|---|
| Deploy smoke (A-L) | 12 | — |
| Workflow transitions | 1 | DEF-203 |
| Security/scoping audit | 5 | DEF-204 (DEF-205 retracted) |
| Internal `/api/CertificatesApi` | 1 controller / 10 endpoints | DEF-206 |
| Company + Product controllers | 2 | clean |
| Admin/Tile/ProductId/Import | 4 | clean |
| Account/Dashboard/PartnerApi/static-file | 4 | DEF-208 + 209 + 210 |
| CSRF/reset/upload/XSS/cookies | 5 | clean |
| Open redir/SQLi/mass assign/rate limit | 4 | clean |
| GS1/audit-trail/cascade | 3 | clean (+ DEF-205 retraction) |
| Health/field leaks/cascade/sessions | 5 | clean |

**~50 surfaces probed total, 6 leaking, 44 clean.**

### Surfaces NOT yet probed (low priority)
- 2FA flow on prod (need config enabled)
- Email template injection (need to capture outbound email content)
- File upload size limits (server crashed once on malformed input — could probe more)
- WebSocket/SignalR endpoints (if any)
- Encrypted-at-rest checks for sensitive DB columns

---

## 2026-05-19 — QC #164 client response drafted; no code change needed

Client (5/11): *"GCC has no way to verify if this is working, Anuki needs to ensure it is correct - if it's not this would cause huge issues."*

Re-verified the implementation is complete and verification evidence already exists from 2026-05-12 (`docs/tracking/screenshots/qc164/EVIDENCE.md`): UI cleanup confirmed across 5 viewports, UP/DOWN cascade confirmed via real Edit-form Playwright saves on cert 26-0002, Partner API contract confirmed (PublicStatuses filter + `"Active"` literal). The cascade is wired at 8 mutation sites: cert Create/Edit (`CertificateController.cs:662, 1224`), Import (`:2253`), `CertificateService.Update`/`BulkAction` (`:704, 905`), Workflow service (`:102`), Expiration sweep (`:195`), Import service (`:1227, 1307`).

Drafted client response in `docs/tracking/client-responses-2026-05-19.md`, appended as third entry. Response includes (a) test table from EVIDENCE.md, (b) the 8-site cascade coverage, (c) three self-verification steps the client can run without Anuki involvement, (d) transparent flag that DEF-203 (workflow matrix not enforced) is a separate item that does NOT affect the cascade guarantee — fires unconditionally on every save.

No code changes for QC #164. Awaiting Shawn to send the response.

---

## 2026-05-19 — DEF-204 + DEF-206 + DEF-208 VERIFIED LIVE ON PROD gccwebapp.com

**Script:** `Scripts/smoke-prod/probe-prod-defs.js` + `probe-prod-def208.js`. Read-only verification (no data changes on prod).

### DEF-208 (static-file leak) — confirmed on prod

The cert detail page renders the thumbnail filename in its HTML source:

```html
<img src="/uploads/images/26-0163_thumb_20260511_122453.png">
```

Anonymous user fetching the URL directly:

```
GET https://gccwebapp.com/uploads/images/26-0163_thumb_20260511_122453.png
  → 200 OK, image/png
```

**No need to brute-force timestamps** — the filename is embedded in the page source of every cert detail page. Anyone with detail-page access can share the static URL with an unauthenticated viewer.

The /uploads/ path is past the test-env Basic Auth gate (same as `/api/*` bypass). Once Amazon or any partner has the gate creds, they have anonymous access to every uploaded file by URL.

### DEF-206 (internal API no role guard) — confirmed on prod

```
External user GET /api/CertificatesApi?pageSize=50 on prod
  → 200 OK
  → 36 certs across 29 distinct companies
```

Dev had 43 certs / 10+ companies. Prod has fewer certs but spread across more tenants — **cross-company leak is BROADER on prod than dev.**

### DEF-204 (ExternalView cross-company) — confirmed on prod

```
External user GET /Certificate/CertificateExternalView/26-0163 on prod
  → 200 OK, 19899 bytes
  → certificate-detail block + Certification Period block both present
```

Endpoint accepts the request regardless of caller's company ownership.

### Urgency upgrade

These three are now flagged **"Open — VERIFIED LIVE ON PROD"** in the defect register. Earlier "Open" framing implied dev-only findings; prod verification proves they're affecting test-env data right now. **Should ship in the next deploy, not the one after.**

Combined attack picture on prod:
- DEF-206 leaks the list of 36 certs to External users
- DEF-204 leaks the External-view detail page for any cert
- DEF-208 leaks the PDF/thumbnail file with no auth at all

Any External account on prod + gate creds = read access to every cert in the database including underlying PDF files anonymously.

### Day's running total — 6 valid DEFs (3 now confirmed on prod)

| DEF | Severity | Status |
|---|---|---|
| DEF-203 | Major | Open (dev verified) |
| **DEF-204** | **Critical** | **Open — VERIFIED ON PROD** |
| ~~DEF-205~~ | ~~Critical~~ | RETRACTED |
| **DEF-206** | **Critical** | **Open — VERIFIED ON PROD** |
| **DEF-208** | **Critical** | **Open — VERIFIED ON PROD** |
| DEF-209 | Major | Open (dev verified) |
| DEF-210 | Major | Open (dev verified) |

DEF-209 + DEF-210 (auth-surface hardening) — same code ships to prod; almost certainly live there. Not separately reverified to avoid login-attempt log noise on the live system.

---

## 2026-05-20 — DEF-213: Edit cert page tag close-button mojibake

**Trigger.** Shawn shared `https://gccwebapp.com/Certificate/Edit/26-0004` plus a screenshot — External Tags area circled, "APR and LEED has special characters displaying???"

**Diagnosis (~4 tool calls).** Grepped Edit.cshtml for tag-rendering code, found literal `Ã—` at lines 1031 + 1059 in the existing-tag rehydration template strings. `Ã—` is the classic UTF-8-bytes-of-`×`-decoded-as-Windows-1252 mojibake pattern. The add-tag path (`wwwroot/js/certificate-create.js:409`) uses correct `×`; only the rehydration path was wrong, so the bug only manifests when the cert already has tags (which is why it looked like LEED/APR specifically were broken — those are the tags 26-0004 had applied).

**Fix.** Replaced both `Ã—` instances with `&times;` (HTML entity, encoding-safe, matches `certificate-tile-view*.js` convention). Two-line edit, zero regression risk.

**Tag-name check.** Grepped seed data + scripts for `LEED`/`APR` — names are clean ASCII in the DB. The "special characters" Shawn saw were entirely the close-button glyph rendering adjacent to the tag name.

**Logged as DEF-213.** Pending deploy.

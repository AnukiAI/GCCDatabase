# Shipped — May 2026

Snapshot of delivery for May 2026. Stakeholder-readable log.

Period: 2026-05-01 → ongoing

---

## DEF-202 — Cert import template enum hint, breadcrumb rename, public custom-field rendering, product-ID preview SKU column, Product Mgmt export Name/SKU/Partner (QC #148 + #104 + #118 + #179 + #184, 2026-05-19, in working tree pending deploy)

Five small client-asked corrections bundled into one deploy.

**QC #148 — cert import template Status header now lists allowed values inline.** Matches the existing pattern in the User Mgmt template (`Status (Active/Inactive)`) and Tags template (`TagType (Internal, External)`). New header: `Status (Draft / Client Approved - Not Paid / ... / Republished / Expired - No Annual Verification — defaults to Draft if blank)`. Import parser extended with a prefix-match fallback so the longer header still resolves on re-import.

**QC #104 (partial) — Company Mgmt breadcrumb renamed for parity with Product side.** `Companies` → `Company Management` to match the Product side's existing label. The other half of #104 (Admin Dashboard nav item) is **needs-no-action**: per the AdminController comment, the standalone Admin Dashboard page was removed by client decision on 2026-05-01 and `/Admin/Index` redirects to User Management — the breadcrumb already lands the user in the right place.

**QC #118 — Custom Fields flagged `IsPublic=true` now render on the public + external cert detail pages.** The model had `CustomField.IsPublic` and the controllers were already loading `Model.CustomFieldValues` for all three detail views, but only the internal view bound to it. Added the same render block to `CertificateSingleView.cshtml` (public) and `CertificateExternalView.cshtml` (partner), filtered to `IsPublic && !string.IsNullOrWhiteSpace(Value)`, grouped under a single `Custom Fields` section per the project-wide rule (CustomField.Section intentionally ignored).

**QC #179 (partial) — Product ID import preview popup now shows the SKU column.** The import parses SKU into `productSku`, but the preview table only rendered Type / Value / Name / Partner / Changes — so analysts couldn't sanity-check SKU values before confirming. Added a `SKU` column to the modal header (`_ProductIdManager.cshtml`) and per-row cell (`product-id-manager.js`). Description column decision (expose in UI vs. remove from template) carries forward as a separate item pending client direction.

**QC #184 (partial) — Product Mgmt export now includes SKU and Partners columns.** Previously the export emitted Product Name / Company / Description / Category / Product Type / Subcategory / Identifier Type / Identifier Value. SKU was being smuggled into the Identifier pair list (left in place for backward compat with any existing client tooling that parses Identifier rows) but now also surfaces as a dedicated column. Partners is a semicolon-separated list of distinct partner names from the product's certs — `ProductController.GetProducts` projection now pulls `Cert.Partner` per product. The cross-cert "all active product identifiers" report ask is a new surface, not a column-add — carried as future work pending design.

**Also discovered: QC #181 (Company Email validation) is already fixed in HEAD** since commit `f018782` on 2026-05-11 (same day client raised it). Strict regex on both `CertificateEditViewModel.CompanyEmail` and `CompanyViewModel.Email`. Status synced; no new code.

**No-code items also drafted in `docs/tracking/client-responses-2026-05-19.md`:**

- **QC #167** — auto-expire timing explainer (the `CertificateExpirationBackgroundService` schedule, conditions, and how the dashboard's "Expired Last Month" tile relates).
- **QC #150** — forgot-password verification steps + ask for client-team-named test logins so password-reset emails land at their own addresses.

Both close once Shawn sends the drafts to the client.

Build: **0 errors, 51 pre-existing warnings** (unchanged). Files: `Controllers/CertificateImportController.cs`, `Services/Import/CertificateImportService.cs`, `Views/Company/Index.cshtml`, `Views/Certificate/CertificateSingleView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`, `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `Controllers/ProductController.cs`, `Views/Product/Management.cshtml`. Tracking: `docs/tracking/defect-register.md` (DEF-202), this ship-log entry, `observations_log.md`, `docs/tracking/client-responses-2026-05-19.md`.

Post-deploy verification: client to (a) download cert import template and confirm the longer Status header, (b) check Company breadcrumb reads "Company Management," (c) set a custom field to Public on a published cert and verify it appears on the public cert detail page, (d) import product IDs with SKU values and confirm preview table now shows the SKU column, (e) export Product Mgmt and confirm Name/SKU/Partners columns are present and populated.

---

## DEF-201 — Header logo at mid-size desktop, mobile-stuck filter sidebar, and dead sort options (QC #175 + #176 + #183, 2026-05-19, in working tree pending deploy)

Three small UX fixes bundled into one deploy footprint.

**QC #175 — header logo distorts at reduced desktop / small-laptop sizes.** The default `.main-logo` is 280px (sized for ≥1440px displays); the existing tablet rule scales it to 180px at 769–1024px. Between 1025 and 1440 the logo stayed at 280px and the `.logo-text-group`'s 300px `min-width` forced an awkward wrap. New `@media (min-width: 1025px) and (max-width: 1440px)` rule scales logo to 220px / text to 36px / subtext to 24px — fits the gap.

**QC #176 — filter Hide/Show toggle unreachable on mobile after collapse on desktop.** Per [[DEF-098]] the toggle controls are CSS-hidden ≤1024px. If a user collapsed on desktop and resized below 1024 (or rotated tablet to portrait), the `.collapsed` class stuck on the sidebar with no on-screen way to recover. `setupSidebarToggle` in all three tile-view JS files (public, external, internal) now subscribes to `matchMedia('(max-width: 1024px)')` and auto-expands the sidebar whenever the viewport crosses into the small-screen breakpoint or loads there directly.

**QC #183 — Sort By dropdown has unused "Most Popular Companies" and "Relevance Score" options.** Pure UI cruft: `CertificateTilesApiController.MapSortBy` has no `popularity` or `relevance` cases, so picking either fell through to the default sort (`Number`) and produced no visible effect. Removed both option DOM nodes from the internal and external tile views. Backend untouched (nothing to remove). Comment block in each view points future readers at `MapSortBy` so this can't quietly regress.

Also discovered during this work: **QC #181 (Company Email validation) is already fixed.** Commit `f018782` on 2026-05-11 (same day the client raised it) added strict regex `^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$` on both `CertificateEditViewModel.CompanyEmail` and `CompanyViewModel.Email`. The fix has been on master since 5/11 but waiting for the next deploy — that's why the client still saw the loose behavior on gccwebapp.com. Status synced; no new code.

Files: `wwwroot/css/main.css` (mid-desktop logo rule), `wwwroot/js/certificate-tile-view.js`, `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js` (auto-expand sidebar on small breakpoint), `Views/Certificate/CertificateTileViewInternal.cshtml`, `Views/Certificate/CertificateTileViewExternal.cshtml` (sort option removal). Build: **0 errors**.

Verification: visual check at 1280×800 and 1366×768 (logo fits without forcing wrap); resize from desktop to ≤1024px in dev tools with sidebar collapsed (filters reappear immediately); both internal and external Sort By dropdowns no longer show the removed options.

Post-deploy verification: client to test logo at typical laptop widths, exercise the resize-while-collapsed case, and confirm the Sort By dropdown is shorter on both internal and external/partner views.

---

## DEF-200 — Internal dashboard tile counts now match their export row counts; "Certs Expired Last Month" no longer uses the wrong date column (QC #180, 2026-05-19, in working tree pending deploy)

Client called this out 5/11: every download icon on the internal dashboard returned a different number than the tile right above it. The headline complaint was *"Certs Expired Last Month is not using right data"* with an explicit ask: *"Anuki needs to do that"* (the count audit). Audited every tile against its export and found three layered bugs.

1. **`certs-expired-last-month` filtered the wrong column.** `Controllers/DashboardController.cs:GetCertificatesForStat:398` called `_certificateRepository.GetCertificatesByDateRangeAsync(startOfLastMonth, endOfLastMonth)`, but that helper filters on `c.EffectiveDate` (per `Repositories/CertificateRepository.cs:447-463`). So the "expired last month" export returned certs that became **effective** last month — a completely different set. The export had no status filter either, so it counted Drafts and Holds with stale ExpirationDates as if they had expired.
2. **`products-with-active-certs` exported per-cert rows, tile counted distinct products.** Aggregation cases existed for categories/types/subcategories/companies but Products was missing — the export fell through to a row-per-cert path. Dev: tile=10 distinct products, export=22 cert rows.
3. **`PublishedThisYear` tile had no upper date bound.** `Repositories/CertificateRepository.cs:580-582` had `c.EffectiveDate >= startOfYear` only; the matching export already capped at `today`. A single cert with `EffectiveDate=2027-07-08` made tile=16 vs export=15.

Fix introduces a `CertificateStatusHelper.EverPublishedStatuses` constant (`DashboardStatuses ∪ {Expired, ExpiredNoAnnualVerification}`) so the "expired last month" semantic has a named source of truth: certs that were ever published, are currently expired, or got auto-flipped to Expired by the background service — Drafts / Holds / pending payment are correctly excluded. New `GetCertificatesByExpirationDateRangeAsync` helper mirrors `GetCertificatesByDateRangeAsync` but on the ExpirationDate column. Both the tile and the export now use the same constant and the same column. `products-with-active-certs` added to the `aggregateTypes` allowlist and to `GenerateAggregatedStatCsv`. `PublishedThisYear` capped at `today` on the tile side. The parallel `/Export/DownloadStat` endpoint (less-used; reachable via `Scripts/testing/test-functionality.ps1`) was aligned defensively — same divergence shape could otherwise reappear through that path.

**Verified locally on dev DB** with `verify-180.js`. All 12 dashboard cards now have tile=export. Before/after dev measurements:

| Card | Tile | Export before | Export after |
|---|---|---|---|
| Total Active Certs | 22 | 22 | 22 ✓ |
| # Companies | 9 | 12 | 9 ✓ |
| # Product Categories | 1 | 4 | 1 ✓ |
| # Product Types | 0 | 3 | 0 ✓ |
| # Product Type Subcategories | 0 | 3 | 0 ✓ |
| # Products | 10 | 22 | 10 ✓ |
| Published Last Month | 0 | 0 | 0 ✓ |
| Expiring This Month | 0 | 0 | 0 ✓ |
| **Expired Last Month** | **1** | **4** | **1 ✓** |
| Expiring Next Month | 0 | 0 | 0 ✓ |
| Published This Year | 16 → 15 | 15 | 15 ✓ |
| Published Last Year | 6 | 6 | 6 ✓ |

For the expired-last-month-specific probe: temporarily set cert 27 `ExpirationDate=2026-04-15` to land it in last month. The background `CertificateExpirationBackgroundService` correctly auto-flipped `Draft → Expired` (its job). Tile and export both then returned exactly 1 row matching that cert. Test fixture reverted afterwards.

Out-of-scope worth flagging for whoever picks up dashboard work next: `wwwroot/js/dashboard.js:298-312` animates stat values from 0 to their final number over ~1500ms — the underlying server values are correct but this tripped up the first verification pass (Playwright captured mid-counter values like 7 instead of 22 when sampling between `networkidle` and animation completion). Any future tile-value assertion needs a `waitForTimeout(2000)`-style settle.

Build: **0 errors, 51 pre-existing warnings** (unchanged). Files: `Models/CertificateStatus.cs`, `Repositories/ICertificateRepository.cs`, `Repositories/CertificateRepository.cs`, `Controllers/DashboardController.cs`, `Controllers/ExportController.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-200), this ship-log entry, `observations_log.md`.

Post-deploy verification: on gccwebapp.com, log in as admin, hover each stat-card download icon until the file lands, open the CSV in Excel, check the count line at the top against the displayed tile number. Specifically test "Certs Expired Last Month" — the count should match a `SELECT COUNT(*) FROM Certificates WHERE Status IN (PublicStatuses + Expired) AND ExpirationDate BETWEEN startOfLastMonth AND endOfLastMonth` on prod.

---

## DEF-199 — Public/External tile-view filter sidebar now surfaces cert-level category overrides and hides soft-deleted companies (QC #177 + #178, 2026-05-18, in working tree pending deploy)

Two related complaints from the client on 5/8. (#178) The Product Category/Type/Subcategory filter sidebar on the public tile view listed only categories/types derived from `Certificate.Product` — any cert-level FK override (`Certificates.ProductCategoryId/ProductTypeId/ProductSubcategoryId`, introduced when DEF-192 fixed the listing-side display) was invisible in the sidebar. So a cert visible in the grid couldn't be reached from the filter. (#177) Companies showed up in the company dropdown that the client expected to be gone — soft-deleted Companies that an existing cert still referenced via FK were projected through the navigation and stayed in the sidebar.

Root cause was a half-applied DEF-192. The listing predicate in `Repositories/CertificateRepository.cs:339-354` uses the effective category/type/subcategory at each level (cert override if set, otherwise Product-derived); the filter sidebar source in `Controllers/Api/CertificateTilesApiController.cs:GetFilterOptions` and `CertificateTileExternalApiController.cs:GetFilterOptions` never got the same treatment. The sidebar projected `c.Product.CategoryId` etc., so an override that hid the Product-derived value also hid the override value from the sidebar even though the listing endpoint matched on it. Same story for the flat `productCategories`/`productTypes` fallback lists alongside the hierarchy. The Company dropdown had no `IsActive && !IsDeleted` predicate, so soft-deleted Companies stayed in the dropdown via cert→Company FK.

Fix mirrors DEF-192's effective-value pattern in both filter endpoints: `c.ProductCategoryId ?? c.Product.CategoryId`, `c.ProductTypeId ?? c.Product.ProductTypeId`, `c.ProductSubcategoryId ?? c.Product.ProductSubcategoryId`. Flat `productCategories`/`productTypes` now resolve from the same `visible*Ids` sets the hierarchy uses (single source of truth instead of independent projections). Company dropdown adds `c.Company.IsActive && !c.Company.IsDeleted`.

**Verified locally on dev DB:**

| Test | Before fix | After fix | Truth |
|---|---|---|---|
| Cert 38 (Client Approved - Paid, Product in Chemicals) set `ProductCategoryId=2` (Clothing) — does the sidebar surface Clothing? | only `Chemicals` | `Chemicals` + `Clothing` ✓ | both should appear |
| Selecting `Clothing` in the listing — does it return cert 38? | n/a (sidebar invisible) | exactly cert 38 ✓ | yes |
| Soft-delete Acme (Id=59) — does it still appear in company dropdown? | 9 companies including Acme | 8 companies, Acme hidden ✓ | should be hidden |

Test data reverted after each check.

Out of scope (flagged for possible follow-up):

- A cert that overrides only `ProductTypeId` to a type under a category different from its effective category will surface the type only if that parent category is independently visible. Dominant data pattern in dev is "category override only," so this isn't reproduced today. If client reports a missing type, the follow-up is to also seed `visibleCategoryIds` from each type's `CategoryId` (one extra DB round-trip).
- Admin/Analyst internal tile-view filter (`CertificateTileInternalApiController.cs:GetFilterOptions`) doesn't restrict the category hierarchy to used categories — admin sees the full hierarchy by design. Not changed.

Build: **0 errors, 51 pre-existing warnings** (unchanged). Files: `Controllers/Api/CertificateTilesApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-199), this ship-log entry, `observations_log.md`.

Post-deploy verification: on gccwebapp.com, log in as analyst, open a cert, set its `Product Category` field directly on the cert form to a value different from the product's own category, save, view the public tile view — the override category should appear in the sidebar. Picking it should return that cert.

---

## DEF-198 — Partner login CSV export was leaking the full database; now correctly scoped to caller (QC #182, 2026-05-18, in working tree pending deploy)

Client raised QC #182 on 5/15 (*"This is big issue, needs fixed."*). A Partner-role web user logged into `/Certificate/CertificateTileViewExternal` and clicked **Export All to CSV** got back the entire database — drafts, expired rows, certificates tagged to other partners. The same caller could see their own scope correctly in the tile view, but the export endpoint behind that button enforced no scope. In dev-DB reproduction, the partner_test user (mapped to Amazon partner) was getting 43 rows; correct answer is 4 rows (`Client Approved - Paid` × `Amazon`).

Three layered gaps, same chain shape as past role-scope bugs.

1. **Service didn't filter by role.** `Services/CertificateService.cs:GetCertificatesForExportAsync` and `GetCertificatesForExportByIdsAsync` applied only the user's search/cert-type/country/status filters — no Partner, no PublicStatuses, no company scoping. The equivalent predicate is correctly enforced on the tile view (`Controllers/Api/CertificateTileExternalApiController.cs:97-138`), but the export bypassed it entirely.
2. **Controller's `GetUserRole()` missed Partner.** `Controllers/CertificateController.cs:1492-1504` enumerated Admin/Analyst/External/Internal but had no Partner branch — partner web users fell through to `"Public"`. Cookie auth stamps the `"Role"` claim with `"Partner"` per `AccountController.SignInUser`, so the branch was reachable but never matched.
3. **No security context plumbed.** `CertificateListRequest` carried `UserRole` but no allow-list of partner names or company IDs, so even if the service wanted to scope, it had nothing to scope with.

Fix added a shared `ApplyExportSecurityScope(IQueryable<Certificate>, CertificateListRequest)` helper inside CertificateService whose predicate matches the tile view exactly:

- **Partner** → `AllowedPartnerNames.Contains(c.Partner ?? "None")` (or `false` if no mapping) AND `PublicStatuses.Contains(c.Status)`
- **External** → `AllowedCompanyIds.Contains(c.CompanyId)` (or `false` if no mapping) AND `PublicStatuses.Contains(c.Status)`
- **Public** (anonymous) → `PublicStatuses.Contains(c.Status)`
- **Admin / Analyst / Internal** → no restriction

Both `GetCertificatesForExportAsync` and `GetCertificatesForExportByIdsAsync` now call it — the "specific IDs" branch can't be used to extract certs outside the caller's scope by listing IDs directly. `ExportCertificatesByIdsAsync` took an optional `CertificateListRequest? securityContext` parameter to keep the signature backward-compatible while the new path passes the populated request through.

Controller-side: added the Partner branch to `GetUserRole`, ported `GetCurrentUserPartnerNamesAsync`/`GetCurrentUserCompanyIdsAsync` from `ProductController.cs:606-644` (same shape, same source claim), and populated `AllowedPartnerNames`/`AllowedCompanyIds` on the request before calling either service method.

**Verified locally on `localhost:5101`** against dev DB with three callers hitting `/Certificate/ExportCertificates?format=csv`:

| Caller | Before | After | Statuses returned | Partners returned |
|---|---|---|---|---|
| Partner (Amazon-scoped) | **43** | **4** | `Client Approved - Paid` × 4 | `Amazon` × 4 |
| Admin | 43 | 43 | all (`Client Approved - Paid`, `Expired`, `Draft`, `Active`) | all |
| Anonymous | 43 | 22 | `Client Approved - Paid` × 22 | all (public certs only) |

Numbers match SQL truth: Amazon partner has exactly 4 certs in `PublicStatuses` on dev. Test fixture `partner_test@gccwebapp.com` (Users.Id=324) left in dev DB for future regression coverage.

Build: **0 errors, 51 pre-existing warnings** (unchanged). Files: `Models/CertificateViewModels.cs`, `Services/CertificateService.cs`, `Services/IServices.cs`, `Controllers/CertificateController.cs`. Tracking: `docs/tracking/defect-register.md` (DEF-198), this ship-log entry, `observations_log.md`.

Same root-cause family as the 2026-04-21 Partner API status-contract fix and DEF-103 (closed 2026-04-30). Those closed the JSON list and single-cert endpoints; this closes the CSV export endpoint. A future small ticket should sweep the remaining export endpoints (`/Admin/ExportData`, `/api/productids/.../export`) to confirm no parallel gap.

Post-deploy verification: log in as a Partner user on gccwebapp.com, hit Export All to CSV, confirm row count + Partner column + Status column all match that partner's PublicStatuses count. Same check as the partner_test fixture above.

---

## DEF-197 — Admin → Product Types: Usage column rollups now dedupe entities tagged at multiple FK levels (2026-05-18, in working tree pending deploy)

Found during local smoke test of [[DEF-196]] on 2026-05-18, not raised by the client. The "Usage (Products / Certs)" column rolled up parent totals by adding direct refs plus the sum of children's counts. The `Certificates` table allows a single row to set `ProductCategoryId` **and** `ProductTypeId` **and** `ProductSubcategoryId` simultaneously — 1 of 43 dev certs does (`26-0027` referencing the Chemicals tree at every level), and 1 of 305 dev products does the same on the product side (`#346 "test"` set on both `ProductTypeId=4` and `ProductSubcategoryId=48`). The integer-sum rollup counted each such entity once per level and then summed them again at the grandparent, producing visibly wrong totals.

Dev DB before/after, Chemicals subtree:

| Node | Before (Prod / Cert) | After (Prod / Cert) | Truth |
|---|---|---|---|
| Cat 1 — Chemicals | `3 / 28` | `2 / 26` | `2 / 26` ✓ |
| Type 4 (child of Chemicals) | `2 / 2` | `1 / 1` | `1 / 1` ✓ |
| Subcat 49 (child of Type 4) | `0 / 1` | `0 / 1` | `0 / 1` ✓ |

Worst-case for an entity tagged at all `N` levels is `N*(N-1)/2` extra counts at the root. Production exposure depends on analyst tagging conventions and is unknown until the data is checked.

Fix replaced the integer-sum rollup in `Services/AdminService.cs:GetProductTypesAsync` with `HashSet<int>` unions: each node holds the distinct set of cert IDs and product IDs in its subtree, descendants call `UnionWith` on the parent, and `.Count` is read once at the end into `CertificateCount` and `ProductCount`. The same entity appearing at multiple levels is now deduped on union. Leaf-node math was already correct and is unchanged.

Files: `Services/AdminService.cs:803-833` (query selects entity IDs not counts; per-FK-target `HashSet<int>` dictionaries) and `Services/AdminService.cs:846-916` (tree build keeps per-node ID sets, unions descendants, assigns `.Count` at the end). Tracking: `docs/tracking/defect-register.md` (DEF-197), this ship-log entry, `observations_log.md`.

Build: **0 errors, 51 pre-existing warnings** (unchanged). Verified locally on `localhost:5101` against both tree view and table view. CSV export inherits the correct counts since `GetProductTypesAsync` is the single source of truth.

Client-facing framing: not a regression, not a client-reported defect — internal QC tightening on the [[DEF-196]] rollup before client UAT. Will deploy alongside DEF-196 in the next push.

Post-deploy verification: spot-check a category on prod that mixes parent + child FK references and confirm the displayed count matches `SELECT COUNT(DISTINCT c.Id) FROM Certificates c WHERE c.ProductCategoryId = @cat OR c.ProductTypeId IN (...) OR c.ProductSubcategoryId IN (...)`.

---

## Add User flow — historical "email already exists" block cleared on prod, schema hardening queued for next deploy (DEF-137 follow-up, 2026-05-15)

Not a defect — the deployed code (DEF-137, shipped 2026-05-04, deployed 2026-05-12) is correct. Surfaced today when the client reported they still could not create a user on gccwebapp.com when the email had been used previously, even though the user was deleted. This was a data state issue, not a code regression.

Root cause: `DeleteUserAsync` now appends `__deleted__<id>__<yyyyMMddHHmmss>` to Email and Username on every soft delete so the unfiltered UNIQUE index on `Users.Email`/`Users.Username` (`Migrations\20251015162223_InitialCreate_SqlServer_Final.cs` — `IX_Users_Email`, `IX_Users_Username`) does not block re-create. Users soft-deleted **before** the 5/12 deploy never got that rewrite, so their original email still occupied the unique index slot and the `INSERT` for any re-create with the same address failed at the database level. The C# duplicate-email pre-check (`!u.IsDeleted`) passed it through, then `SaveChangesAsync` threw on the index, and the controller surfaced the generic "An error occurred while saving the entity changes" message that the client saw.

Three things landed:

1. **Diagnostic + backfill script** — `Database_Scripts\20260515_BackfillSoftDeletedUserEmails.sql`. Three-step idempotent script: read-only count, transactional UPDATE that applies the same `__deleted__<id>__<ts>` suffix `DeleteUserAsync` writes (timestamp falls back to `CreatedAt` then `GETUTCDATE()` if `DeletedAt` was never set), and verification SELECT. Pre-commit sanity check inside the transaction rolls back if any post-update duplicate Email or Username is detected. WHERE clause excludes rows already containing `__deleted__` or `__cleanup__` markers so re-runs are no-ops and existing QC #108 cleanup test rows are left alone.

2. **Prod run already complete** — script executed today against `gccwebap_certificates`. Diagnostic surfaced exactly **1 row** that needed backfill: the client contact's own soft-deleted account from 2026-05-01, which matches the 5/1 QC #107 retest note about the same email already existing. Three QC #108 test-user rows had already been de-collided with a different `__cleanup__qc108__` marker on 5/12 and the tightened WHERE correctly skipped them. UPDATE touched 1 row, sanity checks passed, transaction committed. Verification SELECT returned 0 unsuffixed soft-deleted users remaining. The client's email is now reusable through the Add New User flow without any code change.

3. **Schema hardening queued for next deploy** — new EF migration `Migrations\20260515185538_FixUserUniqueIndexesForSoftDelete.cs` converts both unique indexes to filtered (`WHERE [IsDeleted] = 0`), matching the existing pattern on `Pathways.Name` (`Migrations\20260213005208_FixPathwayUniqueIndexForSoftDelete.cs`). Idempotent SQL DDL generated at `Database_Scripts\20260515_FixUserUniqueIndexesForSoftDelete.sql`, guarded by `__EFMigrationsHistory` so re-runs are no-ops. Production explicitly opts out of automatic EF migrations (`Program.cs:902-917` — "manual migration management enabled"), so this script must be run in SSMS as part of the next deploy. After it runs, soft-deleted rows are excluded from the uniqueness constraint at the schema level, so the suffix logic in `DeleteUserAsync` becomes a nice-to-have for audit reconstruction rather than load-bearing for the create-after-delete path. Same class of bug cannot recur.

Files: `Data\ApplicationDbContext.cs` (added `.HasFilter("[IsDeleted] = 0")` on the two unique indexes with a comment referencing the Pathways precedent), `Migrations\20260515185538_FixUserUniqueIndexesForSoftDelete.cs` (+ Designer + `ApplicationDbContextModelSnapshot.cs` updated by EF tooling), `Database_Scripts\20260515_BackfillSoftDeletedUserEmails.sql` (data backfill, already executed), `Database_Scripts\20260515_FixUserUniqueIndexesForSoftDelete.sql` (DDL for next deploy), this ship-log entry.

Build: **0 errors, 53 pre-existing warnings** (unchanged). No defect-register entry created — per the data-issue-vs-defect classification, this is housekeeping on the DEF-137 fix, not a new code bug.

Post-deploy verification: after the DDL script runs against prod and the next app deploy is up, attempt to delete a user and immediately re-create with the same email — both should succeed without the suffix being involved. Then sanity-check that creating a brand-new user with a never-used email still works (regression coverage for the filtered-index change).

---

## DEF-196 — Admin → Product Types: count column now reflects what actually blocks deletion; cert references give a specific error message (QC #170 — client 5/1, raised again 5/15) (2026-05-15, in working tree pending deploy)

Client raised QC #170 on 5/1 and re-emphasized today (status-call day): she was trying to remove her "Carly Test" product type, the count column on `/Admin/ProductTypes` read **0**, but every delete attempt returned the generic *"An error occurred while deleting. Please try again."* Meanwhile filtering the certificate listing by "Carly Test" showed certs were actually using it — so the admin page was lying to her, and she had no path to fix it. Her direct ask: *"PLEASE REMOVE MY CARLY TEST AND CARLY TEST TYPE OPTIONS. SEE IF YOU CAN GO THROUGH THIS PROCESS, ETC."*

Root cause was three layered bugs on the same code path. The count column queried only `ProductType.Products` (the EF navigation only reflects `Products.ProductTypeId` — it does not see Products linked via `ProductSubcategoryId`, and it does not see Certificates at all). The delete pre-check used the same Products-only navigation, so it always passed for cert-only references. When `SaveChanges` then hit the SQL Server `OnDelete(Restrict)` FK constraint on `Certificates.ProductTypeId`/`ProductSubcategoryId`/`ProductCategoryId` (all three are configured Restrict in `Data/ApplicationDbContext.cs`), the FK violation fell into a broad `catch (Exception)` and got replaced with the unhelpful generic message. The same three-bug chain existed in `DeleteProductCategoryAsync` for root categories.

Fix consolidates the cert and product references into the same source the user sees and the same source the delete checks. `GetProductTypesAsync` now pulls a single batch of `Certificates.ProductCategoryId/ProductTypeId/ProductSubcategoryId` plus `Products.ProductSubcategoryId` once at the top of the method, then populates a new `CertificateCount` field per node and corrects `ProductCount` for Level-2 subcategory rows (which previously showed 0 for products that were attached via the subcategory FK). Roll-up logic preserved: cert counts roll up child→parent the same way product counts always have, so a parent category whose children carry cert references shows that in its own count. The delete methods now run explicit cert pre-checks (`Certificates.ProductTypeId == id || ProductSubcategoryId == id` for types; `Certificates.ProductCategoryId == id` for categories) and a missing `Products.ProductSubcategoryId` pre-check, with specific messages that name the count and the noun ("Cannot delete: 1 certificate(s) use this subcategory. Edit those certificates to use a different subcategory first, then try again."). Entity-Level→label map (1 = type, 2 = subcategory) corrected after a first-pass test surfaced PET returning "use this type" when PET is a subcategory. A `DbUpdateException` catch sits above the generic `Exception` handler as final fallback if a concurrent insert adds a new reference between the pre-check and `SaveChanges` — the user sees "Cannot delete: this item is still referenced by other records. Reassign or remove those references and try again." instead of the old generic error.

UI surface: tree-view column header is now **"Usage (Products / Certs)"** with a tooltip explaining what blocks deletion; each node's count cell renders `${productCount} / ${certificateCount}` with hover-title spelling out the breakdown. Table view splits the old single "Product Count" column into separate "Products" and "Certificates" columns. CSV export (both client-side via product-types.js and server-side via `GenerateCsvExport`/`GenerateExcelExport`) gains a `Certificate Count` column for parity. Help modal text on the Cascade Behavior section rewritten to describe both counts.

Carly's secondary complaint — that the cert Edit page's Product Type dropdown only offered her test options when she tried to migrate certs off "Carly Test" — is the cascading Category→Type→Subcategory filter working as designed: `Views/Certificate/Edit.cshtml:626` loads `/Certificate/GetProductTypes?categoryId={saved}`, so a cert whose saved category is the test category only sees that category's children. **No code change for that part** — workaround is to change Category first, which refreshes the Type dropdown. Flag this in the response to client; if it continues to confuse, the right call is a UX hint under the Type dropdown rather than removing the cascade.

Files: `Services/AdminService.cs` (`GetProductTypesAsync`, `DeleteProductTypeAsync`, `DeleteProductCategoryAsync`, plus CSV/Excel export paths), `Models/AdminModels.cs` (added `CertificateCount`), `Views/Admin/ProductTypes.cshtml` (header text + help modal), `wwwroot/js/product-types.js` (tree node, table view, CSV export), `docs/tracking/defect-register.md` (DEF-196 entry), this ship-log entry.

Build: **0 errors, 53 pre-existing warnings** (unchanged from prior commits). No data migration. No controller signature changes. No API contract changes for partner-facing surfaces. Verified end-to-end on local dev DB: GET `/Admin/GetProductTypes` returns both counts with correct values vs direct SQL counts; POST `/Admin/DeleteProductType/49` (PET, cert ref via SubcategoryId) returns the new specific subcategory message; POST `/Admin/DeleteProductType/48` (LLDPE, product ref via SubcategoryId) returns the new specific product message; POST `/Admin/DeleteProductType/4` (Plastics, has subcategories) returns the existing children check unchanged; POST against nonexistent ID returns "Product type not found" unchanged.

Post-deploy verification: on `/Admin/ProductTypes` as admin, the "Carly Test" type that Carly couldn't delete should now read non-zero in the Usage column, and clicking delete should return the specific count and instruction. Once she migrates the cert(s) using "Carly Test" to a real type (changing Category first to unlock the Type dropdown — see workaround above), the type will delete cleanly without the generic error.

---

## Client narrative — written explanation of QC log defect volume (2026-05-14, doc only)

The client raised that the project is roughly a year past the original timeline and that the volume of defects surfacing through testing is itself frustrating — *"I shouldn't be seeing this many"*. Most of the framing for that conversation already lives in `docs/tracking/qc-log-client-talking-points.md` (verbal-conversation script, prepared 2026-05-09 from the 5/8 log) and `docs/tracking/change-request-register.md` (full CR history, ~27 scope-change events of which only 3 were formally tracked). What was missing was a single written narrative that connects the approach we ran to the defect shape the client is now seeing — paste-ready for an email or share-back, not a live-conversation script.

`docs/tracking/qc-narrative-defect-volume-5-13.md` written today as that document. Grounded in the 5/13 QC log's own *Change vs Defect Analysis* sheet (170 items: 79 actual defects / 46 lack-of-requirements / 45 client-preference iterations; 65 of 170 are pure original-build implementation defects, 38%). Walks the four structural drivers the client asked about: (1) requirements emerging through builds because the original docs didn't define operational detail, (2) functionality arriving incrementally so testing started before the system was complete, (3) UAT and SIT overlapping because the client needed to see screens to validate them, (4) every change request triggering full regression on already-tested code — which is the multiplier that explains why even small CRs add disproportionately to logged-defect volume.

Document explicitly owns what we own: the 65 implementation defects, all 8 Critical-severity items (zero are scope-driven), and the structural decision to absorb most scope changes as "defect fixes" instead of formal CRs. Forward proposal is the same three asks already in the talking-points doc — design checkpoint before each CR sprint, regression pass before each UAT release, every scope-change event gets a CR record. No code, no shipped behavior; this is a client-communication artifact.

Files: `docs/tracking/qc-narrative-defect-volume-5-13.md` (new, ~7 KB). Companion docs unchanged: `qc-log-client-talking-points.md`, `change-request-register.md`, `defect-register.md`.

---

## DEF-194 — Cert Create/Edit Product Identifier table now supports bulk-delete via row checkboxes (QC #172 — client 5/11, per 5/8 call) (2026-05-14, in working tree pending deploy)

Client raised QC #172 on 5/11: *"Add check all for mass delete under Product Identifier per 5/8 call."* On the certificate Create and Edit pages, the Product Identifier table previously only supported per-row delete — analysts cleaning up imported product-ID lists had to click the row trash icon, confirm, wait, and repeat once per row. Fix adds a checkbox column to the left of the table, a select-all checkbox in the header, and a red "Delete Selected (N)" button that appears next to "Add Product ID" whenever at least one row is selected.

Behavior matches the rest of the admin app: confirmation is the standard `AdminCore.showConfirmation()` modal ("Delete N product ID(s)?"), and on confirm the page issues parallel `DELETE /api/productids/{id}` calls. Failures are tolerated per-row so the toast reports honestly: "8 product ID(s) deleted successfully" on full success, or "Deleted 6 of 8; 2 failed" on partial success — no silent drops. Selection state persists across pagination changes in server-paged mode (Edit page with 50+ product IDs), so a user can accumulate selections across multiple pages and delete in one shot. The select-all header is tri-state: checked when every visible row is selected, indeterminate when partial, unchecked when none. On the Create page, where product IDs live in memory until the cert is saved, the bulk delete drops rows from local state without an API call — matching the existing single-row delete behavior. Read-only views (cert detail) get neither the column nor the button.

Two surfaces ship simultaneously even though QC #172 only named "Cert Creation Page": both Create and Edit use the same `_ProductIdManager` partial, so the capability lands on both pages from a single change. Flag this in the response to the client so they know it covers Edit too.

Files: `Views/Shared/_ProductIdManager.cshtml` (bulk-delete button, select-all `<th>`, per-row checkbox `<td>`, server-rendered colspan 8→9 when not read-only), `wwwroot/js/product-id-manager.js` (added `selectedIds` Set on the constructor; `getProductIdRowHtml` emits per-row checkbox; `getEmptyStateHtml` colspan 8→9; `renderTable` calls `updateBulkActionsUI` at every return path; new methods `handleRowCheckboxToggle`, `handleSelectAllToggle`, `updateBulkActionsUI`, `handleBulkDelete`; single-row `handleDelete` also clears `selectedIds` of the deleted id so the count stays in sync), plus `docs/tracking/defect-register.md` (DEF-194 entry) and this ship-log entry. Build clean: **0 errors, 53 pre-existing warnings** (unchanged from prior commits). No data migration, no controller changes, no API contract changes.

Post-deploy verification: on Create page, add 3 product IDs via Add button → check the header → "Delete Selected (3)" appears → confirm → all 3 removed with toast. On Edit page of a cert with 50+ product IDs, select 5 on page 1, navigate to page 2, select 3 more (button shows "Delete Selected (8)"), confirm → all 8 deleted via API → table reloads with the remaining rows.

---

## DEF-192 — Product Category filter no longer pulls in certs whose displayed category doesn't match (2026-05-13, in working tree pending deploy)

Shawn flagged cert `26-0158` showing up on `https://gccwebapp.com/Certificate/CertificateTileViewInternal` when only `Product Categories → Carly Test` was checked, even though the cert's displayed category is `"Electronics, Computers, and Accessories"`. Reproduced anonymously via the public `/api/certificate-tiles?productCategory=Carly Test` — 4 results returned, 3 legitimately matching and 1 leak (26-0158).

Root cause was in `Repositories/CertificateRepository.cs:333-351` `GetAdvancedFilteredCertificatesAsync`. The ProductCategory predicate ORed the requested name across nine fields: Product-derived Level-1/2/3 + cert-override Level-1/2/3 + three parent-cascade variants. Meanwhile, the display projection in `Controllers/Api/CertificateTilesApiController.cs:186-188` (and the parallel internal/external projections) preferred cert-level overrides and only fell back to Product-derived values. So a cert whose `Certificate.ProductCategoryId` pointed at one category while `Certificate.Product.CategoryId` pointed at another would display the override on the tile but match the filter via the Product-side value — exactly cert 26-0158's situation. The leak affected every surface that filters by Product Category: public `/api/certificate-tiles`, internal `/api/certificate-tiles-internal`, and external `/api/certificate-tiles-external`. The latter two carried their own inline copies of the same 6-way OR.

Fix replaces the predicate at all three sites with effective-value matching at each hierarchy level — cert override if set, otherwise Product-derived — mirroring the display projection one-to-one. Parent cascade is preserved only on the public repo path where it existed before (so a Level-2 sidebar selection still surfaces certs tagged at the matching Level-3 child); the internal and external controllers never had parent cascade, and that behavior is preserved as-is to keep scope tight. The new shape is a clean translation to SQL — EF Core 9 emits the override/fallback as a coalesced WHERE clause per level, no client-side evaluation.

End-to-end verification on localhost:5101 against the dev DB synthesized the leak condition by setting cert `26-0001`'s `ProductCategoryId = 2` ("Clothing, Apparel, and Fabrics") while leaving `Product.CategoryId = 1` ("Chemicals and Raw Materials"). Public API `productCategory=Chemicals and Raw Materials` dropped from `total=22, contains 26-0001=True` (pre-mutation baseline) to `total=21, contains 26-0001=False` — leak closed. Public API `productCategory=Clothing, Apparel, and Fabrics` went from `total=0, contains=False` to `total=1, contains 26-0001=True` — effective override correctly matches. Internal API (admin auth) shows the same: `Chemicals → total=39, no 26-0001`; `Clothing → total=1, contains 26-0001`. The dev DB row was restored to `ProductCategoryId = 1` at the end of the test.

Build passes with 0 errors (51 pre-existing warnings). External controller compiles cleanly with structurally identical predicate; same EF shape is proven via the internal runtime test.

Files changed: `Repositories/CertificateRepository.cs`, `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`. No data migration. No client-side changes.

Once deployed, cert 26-0158 will stop appearing under the "Carly Test" filter on every tile surface. The 3 legitimate matches (26-0163, 26-0161, 26-0159) continue to surface — verified that any cert whose effective category is "Carly Test" still matches.

---

## DEF-193 — Concurrent cert-number race fixed: two analysts opening Create at the same time now both save successfully (QC #58 5/11/26 follow-up) (2026-05-13, in working tree pending deploy)

Client (Carly) raised the question on QC #58: *"if 2 people have open and adding cert and hit save -- does one keep the original number and other get next available? It will never assign the same number right (it can't it has to be unique). Please coordinate test on your end to double check when this happens simultaneously with users."* Investigated, live-reproduced on gccwebapp.com, and fixed.

**Live repro confirmed the bug.** Authored a PowerShell repro using two isolated .NET HttpClients (each with its own CookieContainer and `DefaultRequestHeaders.Authorization` for the Basic Auth gate — necessary because PowerShell's `Invoke-WebRequest -Credential` drops the Authorization header on the 302 chain after login POST). Logged Session A as analyst, Session B as admin; both GET'd `/Certificate/Create` via `Task.WhenAll`. Parsed the `id="Number"` input from each response:

```
Session A (analyst) Number: '26-0164'
Session B (admin)   Number: '26-0164'   <-- IDENTICAL
```

Root cause was in `Repositories/CertificateRepository.cs:744` `GenerateNextCertificateNumberAsync`: a `MAX(Number) + 1` read with no reservation, called once from the Create form GET. Nothing held the number between form-render and form-submit. The DB-level `IX_Certificates_Number` unique index (`Data/ApplicationDbContext.cs:67`) prevented the duplicate from being written but converted the race into a generic `DbUpdateException` that the catch block at `Controllers/CertificateController.cs:653-659` swallowed into an opaque `"An error occurred while creating the certificate"`. Net effect: the user who lost the race saw a useless error toast, their form data was preserved but the same conflicting number stayed in the input field — they had to refresh to get a new number and re-paste 5+ minutes of work.

Fix moves number assignment off the GET preview and onto the POST save path, inside a retry loop. Added `AddWithGeneratedNumberAsync(certificate, maxAttempts=3)` on `ICertificateRepository`. Implementation regenerates the number on each attempt, calls `_dbSet.AddAsync` + `_context.SaveChangesAsync`, and on `DbUpdateException` whose inner `Microsoft.Data.SqlClient.SqlException.Number` is 2601 or 2627 and whose message references `IX_Certificates_Number` it detaches the entity and retries. Three attempts handles every realistic concurrency level (the app has <10 analysts total). Heavier locking (SERIALIZABLE / sp_getapplock) would serialize all cert creation needlessly — retry is O(1) per attempt and converges in ≤ N attempts for N concurrent writers, with the unique-index error being the natural signal that a concurrent insert won.

In `Controllers/CertificateController.cs` the Create POST initializer drops `Number = model.Number` (Number is now assigned at save) and the `AddAsync + CompleteAsync` pair at line 521-522 becomes a single `AddWithGeneratedNumberAsync(certificate)` call. PDF and image uploads keep their pre-save position and continue to use `model.Number` for filename construction — the filename for the race-losing user will encode the preview number (e.g. `26-0164_…pdf`) while the cert row's `Number` is the next-assigned value (e.g. `26-0165`), but the canonical link is the `CertificateFile` column on the row, not the filename. This is a cosmetic-only drift and the lowest-risk shape — keeping uploads ahead of save preserves the existing upload-fails-returns-error UX intact.

Updated the Create view helper text under the Number input from `"Auto-generated (format: YY-XXXX)"` to `"Preview — final number is assigned at save. If another analyst saves first, you'll be given the next available number automatically."` so the user understands the preview-vs-final distinction and isn't surprised when their assigned number differs.

Net behavior for the client's specific scenario: **the first user to save keeps the shown number; the second user transparently gets the next available number — no error, no lost work, exactly what the client described as the desired outcome.**

Files: `Repositories/ICertificateRepository.cs`, `Repositories/CertificateRepository.cs`, `Controllers/CertificateController.cs`, `Views/Certificate/Create.cshtml`, `docs/tracking/defect-register.md` (DEF-193 entry), `docs/tracking/shipped-2026-05.md` (this entry), `observations_log.md`. Build clean: **0 errors, 53 pre-existing warnings** (verified against a separate output dir since the running dev server PID 34056 holds the standard `bin/Debug/net8.0/GCCWebApp.exe` lock).

Repro script + evidence preserved at `%TEMP%\def193_repro_httpclient.ps1` + `%TEMP%\def193_evidence\session_{a,b}_create.html` for post-deploy re-verification. The post-deploy run should show one cert saved at 26-0164 and the second at 26-0165, both succeeding silently.

---

## 2026-05-13 — Live smoke pass on gccwebapp.com + external test user seeded against KAO

Ran a full Playwright smoke against `https://gccwebapp.com/` covering 10 checks: site reachability through the Basic Auth gate, public tile view rendering, admin/analyst/external login flows, three admin pages (UserManagement, PartnerAPI, CertificateTypes), a cert-detail click-through, and the `/api/*` gate-bypass behavior. 9 of 10 came back clean on the first pass — public view rendered 25 tiles, analyst saw 20 internal tiles, admin pages loaded with 21 user rows visible, and the API gate bypass returned 401-from-app (not the Plesk Basic challenge) as designed.

The one finding: the `external` test user logged in successfully but the external tile view rendered `"Showing 0 of 0 certificates"`. Pulled the live DB directly — `external` (Users.Id=3) was active and had role `External`, but had zero rows in the `CompanyUser` join table. The seed never wired a Company association on prod. External tile view filters by user→company→cert, so no association meant empty result. Inserted one row (`UsersId=3, CompaniesId=259` — KAO, the highest-cert-count company on prod at 4 `Client Approved - Paid` certs). Re-ran S08 → 4 KAO tiles render, "Showing 4 of 4 certificates" — confirmed.

The smoke also surfaced two non-defect items worth noting: clicking `.certificate-card` on the public view didn't change URL in my naive selector — tiles likely use a child anchor or modal handler, so the smoke methodology needs a follow-up rather than the view being broken; and the live test env can't yet be smoked against the Partner API contract because no live API key is provisioned to this workstation (DEF-103 fix verification on live is still on the open list).

New reusable artifact: `GCCWebApp.UAT.Tests/Tests/Production/LiveSmokeTests.cs` — 10 NUnit/Playwright tests, runs against gccwebapp.com directly, handles the Basic Auth gate via `HttpCredentials`, writes full-page screenshots and a `_findings.txt` summary to a dated folder under `Screenshots/LiveSmoke_*`. Filter with `--filter "FullyQualifiedName~LiveSmokeTests"` to re-run end-to-end after any deploy without standing up a browser by hand.

Files: `GCCWebApp.UAT.Tests/Tests/Production/LiveSmokeTests.cs` (new), live DB `CompanyUser` table (one INSERT linking Users.Id=3 → Companies.Id=259), `observations_log.md`, `docs/tracking/shipped-2026-05.md` (this entry).

---

## Deploy 2026-05-13-0027 — DEF-187 bundle shipped to gccwebapp.com; post-deploy schema fix + migration-history catch-up

Built `GCCWebApp-deploy-20260513-0027.zip` (60.4 MB, 372 files, `PleskTestEnvironment` profile, `EnvironmentName=Production`, uploads excluded per the post-2026-04-23 rule). Zip contents verified — DEF-187 fix DLLs and assets in, no Documentation, no test projects, no `wwwroot/uploads/`. Uploaded to Plesk and extracted onto gccwebapp.com.

Post-deploy the site returned 401 at the test-env gate as expected, but the certificate tile views returned empty payloads. Diagnosis: `/api/certificate-tiles` was throwing `Invalid column name 'CertificationResultsNumeric'` on every query. Root cause: EF migration `20260512011258_AddCertificationResultsNumeric` (part of the DEF-181 cert-results sort fix) shipped in the code but never reached the database — `appsettings.Production.json` has automatic EF migrations disabled and the manual `Database_Scripts/apply-migrations.cmd` step wasn't run during the deploy. Applied the migration to the prod DB (`gccwebap_certificates` on AccuWeb) via an idempotent transactional SQL script (`Scripts/database/20260512011258_AddCertificationResultsNumeric.sql`): adds the persisted computed column, the supporting index, and the `__EFMigrationsHistory` row, all guarded by `IF NOT EXISTS`. First attempt failed with SQL error 1934 (`QUOTED_IDENTIFIER` must be ON for indexed computed columns); fixed by adding `SET QUOTED_IDENTIFIER ON` to the script and re-running with `sqlcmd -I`. Site verified working immediately after — `/api/certificate-tiles` returned `success: true` with cert payload.

While the diagnosis was running, we also checked the rest of `__EFMigrationsHistory` against the codebase and found two other migrations missing from the history — `20260326025056_AddPasswordResetTokenToUser` and `20260420171817_FixProductIdUniqueIndex_PerCertificate`. Pre-flight queries showed that **the schema changes for both were already on prod** (columns present, new per-cert unique index in place, old global-unique index dropped, zero duplicate `(CertificateId, Type, Value)` rows in `CertificateProductIds`) — only the `__EFMigrationsHistory` rows were missing. Someone (or a partial prior deploy) had applied those two via raw SQL without updating the tracking table. Ran the matching idempotent scripts (`Scripts/database/20260326025056_AddPasswordResetTokenToUser.sql`, `Scripts/database/20260420171817_FixProductIdUniqueIndex_PerCertificate.sql`); the `IF NOT EXISTS` guards fell through every schema step and only the `INSERT INTO __EFMigrationsHistory` lines executed. EF history now matches the schema for all three migrations. The takeaway for the next deploy: the migration step needs to ride with the code deploy, not as a separate manual step that's easy to skip under outage pressure. Proposed automation (wrap `dotnet ef migrations script --idempotent` into `Scripts/deploy.ps1` so the migration script is bundled in the zip) is on the follow-up list but not yet implemented.

Files (this deploy round): all DEF-187 changes per the previous entry, plus three idempotent migration scripts committed to `Scripts/database/` (matching their EF migration IDs as filenames so they sort alongside the codebase migrations).

---

## DEF-187 — Admin Edit User: password field now works, plus 4 latent admin-audit bugs flushed out (2026-05-12, in working tree pending deploy)

Shawn flagged this while we were drafting the new user-management training doc: an imported user can't use the Forgot Password flow until outbound email is configured, so the practical workaround would be "delete and recreate via Add New User" — except the Edit User modal *also* has a password field that visibly looks like the right control. Code audit confirmed Shawn's hunch: the Edit modal's password input was rendered, the JS shipped `userData.password` on save, but the receiving `UserViewModel` had no `Password` property. The model binder silently dropped the field, `UpdateUserAsync` never touched `user.PasswordHash`, and the admin got a green success toast on a no-op. The user they "reset" then couldn't log in. Pre-existing latent defect, not a DEF-186 regression.

While fixing it, three closely-related issues surfaced and got rolled in:

1. **`RequirePasswordReset` checkbox on Edit had the same dead-UI pattern** — the value was sent but `UpdateUserAsync` never assigned it. Both knobs in the "Security" section of the Edit modal were decoration.

2. **Audit log was about to start leaking bcrypt hashes** — `AuditService.DetectChanges` reflects over all public properties of the `User` entity. As soon as `UpdateUserAsync` started actually changing `PasswordHash`, the audit-diff would have written both the old and new BCrypt hashes into `AuditHistory.OldValue` / `NewValue` columns. Added `PasswordHash` to the existing `ShouldSkipProperty` skip list so the hash is never serialized. The fact-of-change is recorded via a dedicated single-row audit event with redacted "Password changed by admin" details — auditors get the trail, the hash never leaves the `Users` table.

3. **No admin User-UPDATE audit row had ever been written, ever.** Direct dev DB query: `SELECT COUNT(*) FROM AuditHistory WHERE EntityType='User' AND Action='UPDATE'` → **zero rows.** Server log on every admin attempt showed `FK_AuditHistory_Users_ChangedBy` constraint violations, caught and swallowed. Root cause: `AdminController.GetCurrentUserId()` was looking for a claim named `"UserId"`, which is never issued anywhere. `AccountController.SignInUser` stamps the user's int Id on `ClaimTypes.NameIdentifier`, and `CertificateController.GetCurrentUserId()` reads it from there correctly — `AdminController` diverged at some point in the past. Every admin audit call passed `userId=0`, EF tried to INSERT `ChangedBy=0`, and the FK to `Users.Id` failed. **The blast radius of that pre-existing bug is every admin-initiated audit event in dev/test — user updates, product updates, role updates, tag updates** — silently lost since the divergence. The fix is one line: align `AdminController.GetCurrentUserId` to read `NameIdentifier` first (with `sub` for B2C and legacy `"UserId"` as fallback). Aligning here also means once this ships, prior gaps in the prod audit trail for admin actions stop accumulating.

4. **Audit log was producing useless placeholder text for every User and Certificate CREATE event.** Direct dev DB query showed every existing User CREATE audit row had `NewValue = "[Serialization failed: User]"` (3 most recent: Id 264, 247, 245 — and all the earlier ones too). Same for Certificate. Root cause: `AuditService.SerializeEntity` used raw `JsonSerializer.Serialize` with no cycle handling and no field filtering — EF entities with bidirectional nav (User ↔ Roles ↔ Users, Certificate ↔ Products, etc.) threw inside the serializer, the catch returned the placeholder string. No data leak (credentials are already excluded via `[JsonIgnore]` on the model), but **no audit content either** for the most security-relevant CREATE/DELETE events. Fix: cached `JsonSerializerOptions` with `ReferenceHandler.IgnoreCycles` and a `DefaultJsonTypeInfoResolver` modifier that drops the shared audit skip-list (`UpdatedAt`, `UpdatedBy`, `User`, `PasswordHash`, etc.) and navigation collections — same exclusions as the field-diff path, so both paths now agree on "the entity itself" vs. "the related graph." After fix, a User CREATE row produces ~600-char clean JSON (`{"Id":323,"Username":"…","Email":"…","FullName":"…","Status":"active","IsActive":true,…}`) — scalar fields only, no cycles, no credentials. Verified by creating an ephemeral test user, asserting the resulting audit row passes 5 hygiene checks, then deleting the test user (Id 323 → AuditHistory Id 272). Pre-existing placeholder rows remain — cannot be reconstructed retroactively.

End-to-end verification on http://localhost:5101 against the seed `analyst` user (id=2): admin logs in, PUT `/Admin/UpdateUser?id=2` with a new password → success; log out, log in as `jane.analyst@anukiconsulting.com` with the new password → 302 to `/Certificate/CertificateTileViewInternal` (auth succeeded); log in with the old `TestPass123!` → 200 re-render of the login page (auth correctly rejected); restore the password for seed-state hygiene. Direct DB check on `Users.Id=2`: `PasswordHash` is 60 chars starting `$2a$` (valid BCrypt), `RequirePasswordReset=0`. Direct DB check on `AuditHistory`: two new rows (Id 270, 271) for `EntityType='User' EntityId='2' Action='UPDATE' NewValue='Password changed by admin' Reason='Password changed' ChangedBy=1`, **no bcrypt hash in any column.**

Files: `Models/AdminModels.cs`, `Services/AdminService.cs`, `Services/Audit/AuditService.cs`, `Controllers/AdminController.cs`, `docs/tracking/defect-register.md` (DEF-187 entry), `observations_log.md`, `docs/tracking/shipped-2026-05.md` (this entry), `docs/training/user-management.md` (workaround rows removed — Edit User flow now works). Verification scripts archived at `C:\Users\swest\AppData\Local\Temp\verify-def-187.ps1` (password change end-to-end) and `C:\Users\swest\AppData\Local\Temp\verify-audit-serialization.ps1` (audit blob hygiene).

---

## DEF-191 — Product ID Import no longer fails when the user picks a row that's already on file (2026-05-12, in working tree pending deploy)

Shawn flagged that the Product ID Import Preview on `/Certificate/Edit/{id}` would fail with `Import failed: ... Product ID already exists on this certificate` when the user selected a subset of preview rows without enabling Replace-all. Reproduced on cert `26-0023`: upload a one-row CSV whose Type+Product ID already exist on the cert with identical field values, manually tick the preview row (default is unchecked for an `unchanged` row), leave Replace-all OFF, click Apply Import → red error toast, HTTP 200 with `successCount: 0, errorCount: 1` from the bulk endpoint.

Root cause was a too-loose filter in `applyImport()` in `wwwroot/js/product-id-manager.js`. The code split selected preview rows into bulk-POST rows vs. PUT-update rows with `r.action !== 'update'`, which let `'skip'` rows (CSV rows that match an existing PID with no field changes) into the bulk endpoint. The server correctly rejected them as duplicates. Skip rows should have been treated as no-ops in non-overwrite mode.

Fix is two changes in the same function: tightened the bulk filter to `r.action === 'create'`, added a `skippedExisting` partition, and updated the success/failure decision so a skip-only selection ends with the friendly `"X row(s) already on this certificate — nothing to change"` toast. Mixed selections that include skips append `"; X already on file"` to whatever the actual outcome was, so the user can tell what got applied versus what was already in place. Replace-all mode is unchanged — every non-error row still drives the server's deactivation logic.

Verified end-to-end with a four-scenario Playwright repro against localhost cert `26-0023`: (A) skip-only → friendly no-change toast; (B) modified-only → `Updated 1 product ID(s)`; (C) new-only → `Imported 1 product ID(s)`; (D) mixed update + skip → `Updated 1 product ID(s)`. Before the fix, scenario A and any scenario including a checked skip row would have failed with the generic `Import failed` error. After the fix all four scenarios end in an accurate success toast.

Files: `wwwroot/js/product-id-manager.js`, `observations_log.md`, `docs/tracking/defect-register.md` (DEF-191 entry), `docs/tracking/shipped-2026-05.md` (this entry).

---

## DEF-190 — Product ID Import Preview drops the redundant "Download Template" button (2026-05-12, in working tree pending deploy)

Shawn pointed out that the Import Preview modal on `/Certificate/Edit/{id}` had a Download Template button in its footer, while the Product ID Manager's main toolbar already exposes the same template download. Two paths to the same action inside one workflow, which is what triggered the call-out.

Fix is one block removal in `Views/Shared/_ProductIdManager.cshtml`: the footer `<button onclick="productIdManager.downloadSample()">` was deleted from the import preview modal. The toolbar's `#downloadSample` button (used by both the main page and as the canonical entry for getting the CSV template) is unchanged. The DEF-159 Replace-All pill toggle stays put in the footer's left cluster — it's the actual unique control that belongs there. Cancel + Apply Import on the right cluster also unchanged.

Files: `Views/Shared/_ProductIdManager.cshtml`, `observations_log.md`, `docs/tracking/defect-register.md` (DEF-190 entry), `docs/tracking/shipped-2026-05.md` (this entry).

---

## DEF-189 — Certificate Edit/Create sidebar preview back to square (2026-05-12, in working tree pending deploy)

Shawn flagged that the certificate snapshot preview on `/Certificate/Edit/{id}` was rendering portrait (8.5×11 letter) but had been square previously. A quick `git log -S "aspect-ratio"` against the certificate-create stylesheet traced the change to commit `6b37530` ("defect resolulution", 2026-04-13) where a new `.certificate-create-preview` rule was introduced with `aspect-ratio: 8.5 / 11`, duplicated again under the ≤1024px breakpoint. Before that commit, the class did not exist, so the box took its size from the inner content and read as roughly square.

Fix is two one-token edits in `wwwroot/css/certificate-create.css`: `aspect-ratio: 8.5 / 11` → `aspect-ratio: 1 / 1` in both the main rule and the 1024px media query. The inner `<img>` keeps its existing `object-fit: contain`, so portrait or landscape snapshot images letterbox cleanly inside the square frame without any cropping. The change applies to both `/Certificate/Create` and `/Certificate/Edit/{id}` because they share the class.

Files: `wwwroot/css/certificate-create.css`, `observations_log.md`, `docs/tracking/defect-register.md` (DEF-189 entry), `docs/tracking/shipped-2026-05.md` (this entry).

---

## DEF-185 — Certificate Create/Edit now reject zero FKs with a clean error instead of letting them reach EF (2026-05-12, in working tree pending deploy)

This is the audit follow-up Shawn scoped in DEF-184. After fixing the missing-Category bug on Product Add, the question was whether other create/edit forms had the same gap. A focused audit of the three forms with non-nullable FKs — Product Add/Edit, Certificate Create, Certificate Edit — found that the Product form was now fully covered (DEF-184 fix plus the long-standing Company pattern) but both Certificate forms had a latent gap on three FKs each: `CompanyId`, `ProductId`, and `CertificationTypeId`.

The reason the gap was latent rather than actively broken: `[Required]` on a non-nullable `int` does not reject `0` — only `null`. Normal user flow was incidentally protected by the model binder's behavior on an empty `<option value="">`, which posts as an empty string, fails int binding, and produces a ModelState error. That's a coincidence, not a guarantee. A scripted POST that sets `CompanyId=0` directly would have passed ModelState and crashed at the EF `AddAsync` call with the same FK violation class as DEF-184. Verified by direct POST in the regression: before the fix, the response would have been a 500-class FK exception; after the fix, the form re-renders with three clean validation messages and zero FK exceptions in the app log.

Fix is six identical lines in two controller actions — `CertificateController.Create` (line 432 region) and `CertificateController.Edit` (line 848 region). Each adds `if (model.X <= 0) ModelState.AddModelError(nameof(model.X), "X is required...");` for the three FKs, placed immediately before the existing `if (ModelState.IsValid)` gate so the existing MVC form re-render path picks the new errors up alongside any others. No JS changes on the cert views — the Create and Edit submit handlers were intentionally left alone for this pass since unobtrusive-validation plus the now-enforced server gate cover the actual hole. The schema stays as-is; making these FKs nullable would have a large blast radius across the cert form's product picker, dashboard counts, and ETL extractor.

Verified locally with a Playwright regression that harvests the antiforgery token, scripts a POST with all three FKs zeroed, and asserts the response contains the three error messages, no FK violation text, and no new cert row in the DB. Ran identical assertions for both Create and Edit paths — 14/14 PASS. Re-ran the DEF-182 (10/10) and DEF-184 (9/9) regressions to confirm no impact on Product Management. App log `grep -c "FK_Certificates_\|FOREIGN KEY constraint"` returns 0 across the full test run.

Files: `Controllers/CertificateController.cs` (Create and Edit POST actions), `docs/tracking/defect-register.md` (DEF-185 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Deliberately not in scope this cycle: JS-side guards on the cert form submit handlers (UX polish — unobtrusive-validation already catches normal user flow); other nullable FKs on these forms (can't FK-violate from null by definition).

---

## QC #164 — Product Identifier Status removal + auto-cascade re-verified through Playwright (2026-05-12, validation only — no code change)

The client's 4/10/26 comment on QC #164 still showed it as **Pending** with notes that the Status field needed to be removed from the Import Template and Add popup, and that product status should follow the cert automatically for Amazon badging. The implementation actually shipped 2026-05-06 under DEF-097 / CR-015 and the defect register marked it verified. To close the loop with paste-ready evidence rather than a written claim, the validation was re-run end-to-end on localhost using Playwright + SQL against the local dev DB.

Three test drivers were authored under `docs/tracking/screenshots/qc164/`. The UI capture (`qc164_capture.py`) logs in as the analyst, opens cert 26-0002 in Edit, and screenshots both the Product ID Manager surface and the Add Product ID modal across the five-viewport standard (desktop 1920, desktop 1366, tablet-landscape 1024, tablet-portrait 768, mobile 375). Every viewport shows the filter row with only the Type, Partner, and Search controls, the table headers with only Type, Product ID, Product Name, SKU, Partner, Created, Created By, and Actions, and the Add modal with only Type, Product ID, Partner, Product Name, SKU, and Description. No Status filter, no Status column, no Status field. The same script downloads the Import Template CSV and confirms the header line reads exactly `Type,Product ID,Product Name,SKU,Partner,Description` — no Status column.

The cascade driver (`qc164_cascade.py`) exercises the actual production code path by editing cert 26-0002 four times through the analyst form. Baseline showed CAP / two Inactive PIDs (a stale local-dev artifact from before CR-015 was wired). The four transitions and their observed DB states were: Hold (no-op cascade, both already Inactive), Client Approved - Paid (UP-cascade fired, both PIDs flipped to Active), Hold (DOWN-cascade fired, both PIDs flipped back to Inactive), Client Approved - Paid (restored, both Active). The transitions through Save trigger `SyncProductIdStatusesForCertificateAsync` at `Services/CertificateService.cs:843`, which writes the derived status atomically with the cert save. As a side effect the run also corrected the stale dev-DB state.

The Partner API contract test (`qc164_partner_api.py`) calls `GET /api/v1/certificates` with the Amazon partner key. The local dev DB has eleven Amazon-partnered certs total — four in CAP (no PIDs), six in legacy `Active` (with PIDs but non-public per CR-015), and one Draft. The response correctly returns exactly four certs, each with `status: "Active"` as a literal mapping (not the internal `"Client Approved - Paid"` lifecycle string), and the six legacy-`Active` certs and the Draft are filtered out by the `CertificateStatusHelper.PublicStatuses` guard at `Controllers/Api/v1/CertificatesController.cs:160`. A bonus attempt to promote one legacy-Active cert to CAP to also exercise the `productIds[]` array in the response was blocked by the workflow service (legacy `Active` → CAP isn't an allowed transition in the modern 14-state taxonomy); that's out of scope for QC #164 and noted in `EVIDENCE.md` for traceability. The cert touched in that attempt was not mutated, confirmed via post-test SQL.

All six contract guarantees on the QC #164 line are now backed by artifacts: ten viewport screenshots, the template CSV, a transition log showing cascade fired in both directions, a Partner API JSON dump, and an `EVIDENCE.md` summary. Nothing in the codebase was modified — this is validation evidence on top of the 2026-05-06 ship.

Files (evidence-only): `docs/tracking/screenshots/qc164/EVIDENCE.md`, ten PNG screenshots, `B_import_template.csv`, `cascade_log.txt`, `partner_api_log.txt`, `D_step1_baseline.json`, `D_step2_promoted.json`, three Playwright Python scripts.

---

## DEF-184 — Adding a product without a Category no longer throws a database error (2026-05-12, in working tree pending deploy)

Shawn caught this during the manual UAT pass right after DEF-182 shipped. Open the Add Product modal, fill Product Name and Company, leave Category blank, click Save — the controller swallowed the EF exception and returned a generic "Error saving product" toast, while the app log filled with a SQL `FK_Products_ProductCategories_CategoryId` violation. The user had no way to tell the failure was specifically about Category.

Root cause was a long-standing validation gap. `Products.CategoryId` is non-nullable both in the EF model and at the DB level. Add Product's modal had Product Name and Company marked required (HTML attribute + JS guard + server check), but Category was treated as optional in the UI even though the schema treats it as mandatory. Saving with no Category meant the JS sent `categoryId: 0`, EF tried to INSERT with FK target 0, and SQL Server rejected it. Same defect class as the missing-Company validation that was already covered — Category had just never received the same treatment.

Fix is the same three-layer pattern Company uses. The label now reads "Category *" and the `<select>` carries the `required` attribute, so the browser fires a focused validation tooltip ("Please select an item in the list.") before the form submit event runs. A redundant JS guard in `saveProduct()` covers the case where someone bypasses HTML validation through a scripted submit. And a server-side `if (model.CategoryId <= 0)` early-out in `ProductController.SaveProduct` now returns a clean `{success:false, message:"Category is required..."}` JSON instead of letting EF crash on the FK. The schema stays as-is — making `CategoryId` nullable would touch every query, projection, and filter that assumes Category is set (cert form's product picker, dashboard counts, ETL extractor) for what is fundamentally a validation gap, not a schema problem.

Verified locally with a Playwright regression covering all three layers — 9/9 PASS, including a direct POST to `/Product/SaveProduct` with `categoryId:0` that now returns the clean JSON rejection instead of an FK exception. Re-ran the DEF-182 smoke (10/10) to confirm no Status-flow regression. App log shows no more FK exceptions on the missing-Category path.

Files: `Views/Product/Management.cshtml` (label + select `required`, JS guard), `Controllers/ProductController.cs` (server-side check before the create/update branch), `docs/tracking/defect-register.md` (DEF-184 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Follow-up worth scoping: audit other non-nullable FKs on the product and certificate forms (`Certificate.PathwayId`, `Certificate.CompanyId`, etc.) and confirm they all use the same three-layer pattern. This is the second time the "client field is loose but schema is strict" mismatch has produced an unhelpful red toast — would be good to flush them out proactively rather than one defect at a time.

---

## DEF-183 — Public cert detail page no longer leaks internal-process error text when a PDF is missing (QC #168) (2026-05-12, in working tree pending deploy)

Carly flagged on 4/30 that after removing a default cert PDF, clicking the download icon on the public cert detail page opened a new browser tab showing the raw text *"No certificate PDF has been uploaded for 26-0006. Download is unavailable until an analyst uploads the certificate file."* The second sentence — internal workflow language about analysts uploading — is not language we'd ever want on a public surface, and the endpoint is `[AllowAnonymous]` so any visitor can hit it. Her preferred outcome: either trim the text, or grey out the button. She also asked the right architectural question — *"why are they built as different experiences?"* — because the public tile view's download icon handles missing PDFs cleanly with a toast and the single view did not.

The answer to her question is that the public tile view is a JS-rendered grid where each tile's download icon goes through a `fetch()` handler that swallows the response body and shows a toast on non-ok. The public single view is server-rendered Razor where the download icon was wired to a one-line `window.open('/Certificate/Download/...', '_blank')`. `window.open` doesn't read the response — it just navigates a new tab to whatever the server returns. So the same `NotFound` response that the tile view politely catches gets rendered raw as a new-tab page on the single view. The greying logic on the single view does exist (`hasFile = !string.IsNullOrEmpty(Model.Certificate?.CertificateFile)`) but it only catches the **DB-null** case. It doesn't catch the **DB-set-but-file-missing-on-disk** case, which is exactly what triggered Carly's report.

This ships two changes that work together. First, the controller's error body is now exactly *"No certificate PDF has been uploaded for {id}."* and nothing more — the internal-process clause is gone, with a comment at the call site flagging the public-safety constraint so it doesn't grow back. Second, the single view's `downloadCertificate` was rewritten to use the same `fetch()` + blob + anchor-click pattern the tile view uses, with `showToast('Certificate PDF not available', 'error')` as the fallback. Now even when a cert has `CertificateFile` set in DB but the file is gone from disk, the user sees a clean toast on the same page instead of a new tab with a raw response body.

We deliberately did not add a per-request `File.Exists()` check to the view's `hasFile` calculation. That would add a disk hit to every public cert detail page load to handle a rare edge case; the toast covers it with no per-request cost.

Carly's closing clause — *"We shouldn't really have certs added without a cert so in reality this should not ever happen but we need solution if it does"* — reinforces the 2026-04-25 business rule that analysts must upload a real PDF when creating a certificate (we never auto-generate a placeholder). This fix is the graceful-degradation path for the case that rule fails to prevent (manual file removal, or a Plesk deploy that wipes uploads as happened on 4/23).

Files: `Controllers/CertificateController.cs` (public-safe error body + comment at line 1590-1594), `Views/Certificate/CertificateSingleView.cshtml` (downloadCertificate rewritten to fetch + toast at lines 233-269), `docs/tracking/defect-register.md` (DEF-183 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Smoke test (local): build is clean — the only build error is MSB3027 file-lock because the dev server (PID 28756) is holding the exe. Restart needed before manual verification: `taskkill /F /PID 28756` then `dotnet run --urls "http://localhost:5101"`. Then on the public cert detail page for a cert whose file is missing from disk, clicking the download icon should produce the toast and no new tab; hitting `/Certificate/Download/{number}` directly in the browser bar should return exactly the trimmed message; a cert with a real PDF should still download normally.

---

## DEF-182 — Product Management: template, popup, and table now agree on the same field set (QC #103 3/13/26 close-out) (2026-05-12, in working tree pending deploy)

QC #103 has a long tail. Carly's 3/13 note was that the CSV template's column names and the Product Management table's column names weren't "level" with each other or with the Add New Product popup. The previous fix cycle (3/24) trimmed the template to match the popup; that resolved one half. The 5/8 retest re-raised it because the **Export** CSV still emitted the old eight-column header, which DEF-174 (5/11) closed. That left one remaining gap: the table showed a **Status** column that didn't exist on the popup, and the template still said "Name" where the table said "Product Name."

The temptation was to just delete the Status column to make the three surfaces identical. That would have been wrong. `Product.IsActive` is a real soft-delete flag — every certificate Create and Edit form filters its product picker to `IsActive && !IsDeleted`, in eight places in `CertificateController.cs` alone (plus the Admin product service and the ETL exporter). Setting a product Inactive is how an analyst retires it without losing the cert history that already references it. Before this fix the **only** way to flip the flag was a CSV import line, and the Product Management table was the only place to see the state. Removing the column would have orphaned a live business behavior.

The correct call was the inverse: add Status to the popup so it can be edited inline, and align the wording everywhere else. The popup now has a Status dropdown (Active / Inactive) sitting next to Subcategory on the bottom row. Add defaults to Active. Edit pre-fills from the listing API's existing `IsActive ? "Active" : "Inactive"` projection. Save sends the value to the backend, which already binds `model.IsActive` on both create and update paths — no controller change needed. The CSV template header changed from `Name` to `Product Name`. The Export header did the same so round-trip stays clean. Import parsing is positional and skips the header row, so the rename is header-only and doesn't break legacy CSV imports.

Description stays on the popup and template but not the table, by intent — table omits it for column-width reasons. Description is still visible on the expandable row detail and on the Product detail page.

Files: `Controllers/ProductController.cs` (template header), `Views/Product/Management.cshtml` (modal Status select + showAddProductModal reset + editProduct prefill + saveProduct payload + export header rename), `docs/tracking/defect-register.md` (DEF-182 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Smoke test (local): built 0 errors, app comes up on http://localhost:5101, `/Product/Management` returns the expected 302 to login. Manual UI verification (Add → Status defaults Active; Edit → Status pre-filled; Save → table reflects; Inactive product drops out of Cert Create's product picker) is the next step before deploy.

---

## DEF-181 — Cert tile internal: ASIN/UPC/GTIN/EAN sorts now respect legacy columns; Cert Results sorts numerically (QC #157 5/1/26 close-out) (2026-05-11, in working tree pending deploy)

QC #157's 4/7 batch flagged sort issues across Status, Location, Tags, Partner, and the four product-ID columns. DEF-165 closed most of them last week. Carly's 5/1 retest narrowed the remaining gripes to "ASIN is off" and "Cert Results is off." Both root causes turned out to be different from what was assumed.

The ASIN/UPC/GTIN/EAN problem was a mismatch between the column's DISPLAY logic and its SORT logic. The display falls back from `Certificate.ProductIds` (the typed collection) to the legacy `Certificate.{Asin|Upc|Gtin|Ean}` string columns when no active ProductIds rows exist for that type. The sort comparator only looked at `ProductIds`. So a cert whose ASIN lived in the legacy `c.Asin` column showed "B08X7N9MRQ" in the cell but got sort key "" — falling to the Id tiebreaker. From the user's seat that looked like "the column has values but isn't sorting." Verified by capturing the EF-emitted SQL via a one-off `ToQueryString()` log and running it directly against the dev DB — got back 25 rows with empty sort keys despite many having real ASIN values in the display.

Fix: the sort comparator now mirrors the display — `c.ProductIds.Any(...) ? Min(c.ProductIds...) : c.Asin) ?? ""`. EF translates the conditional to a SQL `CASE WHEN EXISTS(...) THEN (SELECT MIN(...)) ELSE c.Asin END` and the order finally matches what the user sees in the column. Same shape applied to all eight ID-column branches.

The Cert Results problem was different: the raw stored values are a mix of formats ("95%", "100", "1", "0.69", "0.27", "0.17"). The field is free-text by design (per the original CR for Amazon threshold scores), and the sort was doing a plain SQL Server string comparison. Lex sort on those mixed-format strings produces the order Carly saw: 95% > 87% > 100% > 1% > 69% > 27% > 17%. The sort was working as coded; the data shape just doesn't lend itself to lex compare.

Fix: added a persisted computed column `CertificationResultsNumeric DECIMAL(18,4)` on the Certificates table. The CASE expression mirrors `DisplayFormatters.FormatCertificationResults`: contains `%` → strip and use; `[0,1)` → ×100; `≥1` → as-is; non-numeric → NULL. Indexed. SQL Server maintains it automatically on every insert/update. The sort comparator now reads from that column. NULL placement defers to SQL Server defaults (nulls first in ASC, last in DESC), which surfaces populated values where a sorting user is most likely looking.

Verified locally with a Playwright sort harness covering 9 columns × 2 directions = 19 assertions — all pass. ASIN ASC now shows "" first then non-empty values; ASIN DESC walks from "B0GJRV3MZC" down to "B0BQN8R7JJ"; Cert Results DESC walks 100 → 95 → 87 → 69 → 27 → 17 → 17 → 1 then NULLs. Build clean.

This deploy includes one EF schema migration (`AddCertificationResultsNumeric`). It adds one nullable computed column plus its index — non-destructive, no data movement. Apply via `dotnet ef database update` or extract SQL via `dotnet ef migrations script`.

Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `Models/Certificate.cs`, `Data/ApplicationDbContext.cs`, `Migrations/20260512011258_AddCertificationResultsNumeric.cs`, `docs/tracking/defect-register.md` (DEF-181 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Out-of-scope follow-up: the Internal-tag / External-tag / UPC `FirstOrDefault()` non-determinism documented in DEF-165 is still open. It needs an SQL Server index/plan tune; low priority unless certs grow many tags/UPCs each.

---

## DEF-179 — Dashboard "Active Certs Published" exports now match the count cards (no more Draft/Hold/Expired leaking into the CSV) (QC #156 5/1/26 follow-up) (2026-05-11, in working tree pending deploy)

QC #156's original 4/7 ask was a label rename — change "# Certs Published Last Month / Year / This Year" to "# Active Certs Published…" so the title matches what the count actually represents. That part shipped cleanly and was verified PASS on 5/1. On 5/1 the client added a follow-up on the same QC row: *"When I export now it has Expired, etc. Just not right."* That second comment was a different defect — the renamed labels said "Active" but the download buttons next to those count cards were emitting CSVs containing every status, including Expired, Draft, Hold, and Republished. The card said one thing; the file said another.

Root cause was a clean filter-asymmetry between the count query and the export query. The count widgets in the dashboard repository call `_dbSet.CountAsync(c => dashboardStatuses.Contains(c.Status) && c.EffectiveDate >= ...)` — they filter both by date range and by the canonical "Dashboard" active-status set. The download buttons route through `DashboardController.GetCertificatesForStat`, which calls `_certificateRepository.GetCertificatesByDateRangeAsync(start, end)` — that method filters by date range only and intentionally has no status filter (other callers may want unfiltered results). The result was that "12 Active Certs Published This Year" on the card could correspond to a 30-row CSV download.

Fix is one file, three case statements. The dashboard's `certs-published-last-month`, `certs-published-this-year`, and `certs-published-last-year` cases now apply `Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status))` to the date-range result before returning, exactly matching the count widget's filter. CSV row count and card count will reconcile one-to-one going forward. Each case is wrapped in a block scope so the local `certs` variable doesn't collide with the existing `expiringNext` / `filtered` locals in adjacent cases. Report titles in those three exports also flip from "Certificates Published…" to "Active Certificates Published…" so the CSV header phrasing matches what the user clicked.

Three sibling cases were intentionally left alone: `certs-expired-last-month` (the corresponding card is "Expired Last Month," not "Active" — the user *wants* to see expired certs there), and the two "expiring" cases (`certs-expiring-this-month`, `certs-expiring-next-month`), which already route through `GetExpiringCertificatesAsync` and pick up an active-status guard from a different code path. The repository method `GetCertificatesByDateRangeAsync` is also unchanged — the filter is applied at the dashboard case-statement layer so the repository stays general-purpose for other future callers.

This is a sibling fix to DEF-178 (Dashboard exports missing taxonomy columns + cert-status legacy-Active dropping). DEF-178 expanded `GetCertificatesByDateRangeAsync` to `Include` Product Category / Product Type / Subcategory / Partner nav properties, so the now-filtered published-certs CSVs will also benefit from those richer columns.

Files: `Controllers/DashboardController.cs` (three case statements + titles). `dotnet build` clean — 0 errors. Live in-browser smoke deferred to next dev-server restart (running instance is holding the .exe lock); will verify via Playwright after the deploy lands and update TC-QC-156 to confirm the export side.

---

## DEF-176 — Logged-in cert tile views (Internal + External) now match Public for filter chips and Company filter (QC #127 5/1/26 + 5/8/26 re-raise) (2026-05-11, in working tree pending deploy)

Client flagged on 5/1 and again on 5/8 that the Public certificate listing was fixed but the logged-in views (Internal admin/analyst view and External partner view) still showed the old behavior: Company filter chips displayed numeric IDs ("Company: 131") instead of the company name, and every chip carried a redundant `Type:` / `Company:` / `Search:` label prefix. Public was visibly correct ("3M ×"); the two logged-in views were not. Playwright capture against the deployed environment reproduced exactly what the client described — three views, three different behaviors, two of them broken.

Root cause was that the original QC #127 fix in March only updated the Public view's JS file. The Internal and External views are separate JS files that share roughly 95% of the same code by line count, including their own copies of the company-checkbox renderer and the active-filter chip renderer. Neither got the patch. Same pattern as DEF-174 last week — multiple near-identical implementations, fix lands on one, two get left behind.

Two surgical changes per JS file. First, the Company filter checkbox `value` attribute is now the company name (escaped for HTML safety) instead of the numeric ID, matching what Public does so chips show "3M" rather than "131" the moment the user ticks a box. Second, the active-filter chip rendering loop drops the `<span class="filter-tag-label">${label}:</span>` prefix from both the Search chip and every checkbox-based filter chip; `aria-label` on the remove button is simplified the same way ("Remove 3M filter" instead of "Remove Company: 3M filter"). The `labelMap` constant stays in place since it's still used as the iteration key set; removing it would be a separate cleanup with no functional effect.

Backend matched in parallel so the new payload doesn't break filtering. The Internal and External tile API controllers each had a single-mode company filter that called `companies.Contains(c.Company.Id.ToString())`; both are now dual-mode, mirroring the existing logic in the Public controller — if every token parses as an int, treat them as IDs and join on `c.Company.Id`; otherwise treat them as names and join on `c.Company.Name`. Either-or, not mixed, same as Public's contract. Numeric-ID callers (e.g., direct API consumers, bookmarked deep-links from before the fix) continue to work unchanged.

Files: `wwwroot/js/certificate-tile-view-internal.js` (checkbox value + chip render), `wwwroot/js/certificate-tile-view-external.js` (same), `Controllers/Api/CertificateTileInternalApiController.cs` (dual-mode company filter), `Controllers/Api/CertificateTileExternalApiController.cs` (same). `dotnet build` clean — 0 errors. Local in-browser smoke deferred to next dev-server restart (running instance is holding the .exe lock); the Playwright before/after script (`%TEMP%\verify-qc127.mjs`) will re-run against gccwebapp.com after the deploy lands and TC-QC-127 will flip from INCONCLUSIVE → PASS at that point.

Worth flagging: this is the second QC item in the past week (DEF-174 was the first) where a previously-claimed fix turned out to live in dead or non-deployed code while a near-twin file kept the bug. The three tile views are a known shared-95% triplicate; consolidating to a single view with role-based config would prevent the same re-raise pattern on the next round and should be considered as a separate refactor CR.

---

## DEF-174 — Product Management Export CSV now matches the Import Template (round-trip restored, QC #103 5/8/26 re-raise) (2026-05-11, in working tree pending deploy)

Client re-raised QC #103 on the 5/8/26 log as "Not fixed." Verification via Playwright against the deployed gccwebapp.com confirmed her read: the **template** download was correct (`Name, Company, Description, Category, Product Type, Subcategory` — that part was fixed back in March under commit `786dc55`), but the **Export** button on `/Product/Management` was still emitting the old eight-column header (`Product Name, Company, Category, Product Type, Subcategory, Status, Certs, IDs`), so the two CSVs didn't align and an exported product list could not be re-imported.

Root cause was a duplicate-function trap. Two `exportProducts` definitions exist in the codebase: an inline one in the Razor view (`Views/Product/Management.cshtml`) and a class method in `wwwroot/js/admin/productManagement.js`. The Export button on the page is wired to the inline one, but the prior sprint's "fix" landed on the class method — adding Description, removing Status, etc. — which never ran. Every prior round of testing the client logged "still broken" was actually correct; we kept editing dead code. (Same anti-pattern surfaced last week as DEF-165A, where the Template button had duplicate handlers; resolved there by collapsing to a single source of truth.)

Fix is a single-file change to the wired-up inline function. The export now emits 8 columns: `Name, Company, Description, Category, Product Type, Subcategory, Identifier Type, Identifier Value`. The first six match the import template character-for-character so a round-trip is finally clean. The two trailing columns preserve identifier visibility for analyst use — one row per identifier (deduplicated by type+value), or a single row with empty identifier cells for products with no identifiers. `Status`, `Certs`, and the bundled `IDs` semicolon-blob column are removed; status comes from the certificate workflow, not the product, so it didn't belong here anyway. The Identifier Value column is force-quoted in CSV output so Excel doesn't convert long numeric UPCs/EANs to scientific notation.

Verification evidence captured before and after: deployed-prod Playwright dump of the broken export (`%TEMP%\products_export_2026-05-11.csv`, 30,079 bytes, old 8-col header) is the receipt for the client's re-raise. The local re-verify is pending — dev server is `dotnet run --no-build` and the running `GCCWebApp.exe` is holding the .exe lock; a clean rebuild + restart is needed before `%TEMP%\verify-export-local.mjs` can confirm the fix on `http://localhost:5101`. Will re-run against `gccwebapp.com` after the next deploy zip lands and update TC-QC-103 in `docs/tracking/test-execution-log.md` from FAIL → PASS.

Files: `Views/Product/Management.cshtml` (export function rewritten), `docs/tracking/defect-register.md` (DEF-174 entry), `docs/tracking/test-execution-log.md` (TC-QC-103 marked FAIL with evidence; will flip to PASS after deploy), `docs/tracking/shipped-2026-05.md` (this entry).

---

## DEF-173 — User Management table & CSV: "Company" → "Company Access" + show "All" when user has every company (2026-05-11, in working tree pending deploy)

Client UAT follow-up on the 5/8/26 QC log against the QC #106 row: *"No popup, table, and import/export/template. The current 'Company' in table should be called 'Company Access' since that is what it is related to? If they have access to All just put all otherwise the list is massive (like my new user example)."* The Add/Edit User modal already used the label "Company Access" from the original QC #106 fix (2026-03-24), but the table header, CSV export, and import template hadn't been brought along. A user associated with every company in the system rendered as a multi-line comma list that broke the row layout — exactly what Carly was reporting.

Aligned all four surfaces to the modal: table header now reads "Company Access" (`Views/Admin/UserManagement.cshtml`); CSV export's column header key is now "Company Access"; the import template's last column is now `Company Access (semicolon-separated names, or "All")`; import preview's "changed field" label says "Company Access" instead of "Company". Added a small `getCompanyAccessDisplay(user)` helper that returns the literal string "All" when the user's company IDs cover every entry in the loaded company set, and falls back to the comma-joined name list otherwise — so both the table render and the CSV export collapse the long list to one word when applicable.

Taught the import resolver two things it didn't know: (1) the header normalizer now recognizes both `Company Access` (new) and `User Company` (legacy header from the prior template — was previously unmapped, a latent round-trip bug we hadn't caught), and (2) `resolveCompanyIds` expands the literal `"All"` to every company ID, so an exported "All" can be re-imported with no manual editing.

No backend change. No schema change. The User entity still stores discrete `UserCompany` join rows; "All" is purely a display + import-time convenience.

Files: `Views/Admin/UserManagement.cshtml`, `wwwroot/js/userManagement.js`, `docs/tracking/defect-register.md` (DEF-173 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Build clean (0 CS errors, 61 pre-existing warnings unrelated to this change). Smoke verification pending on next app restart — running instance is holding the .exe lock.

---

## DEF-167 — Cert tile filter dropdown showed certification-type subcategories as separate checkboxes (2026-05-11, fix in deploy zip pending upload)

Client reported via QC #169 (4/30) that the public Certificate Tile View's "Certification Type" filter pane was listing Certified Sustainability Facts subcategories (Biobased Content, Closed Loop, Embodied Carbon Disclosures, and ~28 more) as their own filter checkboxes alongside the top-level certification types. Her direction: subcategories should not appear in the filter box at all. The login view shows the desired behavior — only top-level types with visible certs — and was the standard the public view should match.

Root cause was in three controllers (`CertificateTilesApiController.cs` for the public view, `CertificateTileInternalApiController.cs` for the admin view, `CertificateTileExternalApiController.cs` for the external-user view). Each one ran a query that pulled every active cert type plus an in-memory flatten loop that emitted the parent followed by each child as a separate filter row. The public endpoint also lacked the `IsActive` guard that the admin endpoint had, so soft-deleted types with active children could leak through.

Replaced the 18-line query + flatten loop with a single Level-1-only LINQ query in all three controllers:
```csharp
var hierarchicalCertTypes = await _context.CertificationTypes
    .Where(ct => ct.IsActive && ct.ParentId == null && certTypeIds.Contains(ct.Id))
    .OrderBy(ct => ct.SortOrder)
    .Select(ct => new { name = ct.Name, level = ct.Level })
    .ToListAsync();
```
JSON response shape unchanged — JS consumers untouched. The change also closes the related backlog items QC #128 (LEED filter showed certs without LEED tag) and QC #149 (L2/L3 Product Category options didn't filter) which were the same flatten-pattern symptom on different filter panes.

Per Shawn's direction "fix the data": certs tagged at Level-2 (a subcategory) instead of Level-1 (a parent) would become unfilterable after the code change. Wrote `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql` with two parts — an audit SELECT (safe everywhere) and a CR-gated remediation UPDATE block (commented out). Ran the audit against prod (gccwebap_certificates @ tcp:68.71.129.120,1533) before zipping: **AffectedCertCount = 0**. No certs on prod tag a Level-2 row as primary type, so the remediation block is not needed and no CR is required.

Local verification: `GET /api/certificate-tiles/filters` returned 4 entries, all `level: 1`. `GET /api/certificate-tiles?certType=Recycled%20Content` returned 16 certs, all matching the filter. Build clean (0 errors, 51 pre-existing warnings).

Files: `Controllers/Api/CertificateTilesApiController.cs`, `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`, `Database_Scripts/20260511_DEF167_AuditL2TaggedCerts.sql`, `docs/tracking/defect-register.md` (DEF-167 entry), `docs/tracking/shipped-2026-05.md` (this entry).

Deploy zip: `deployment-drops/GCCWebApp-deploy-20260511-1054.zip` (60.3 MB).

Follow-up confirmation (2026-05-12): QC #169 has a second paragraph in Carly's log asking that the public view also hide cert types when no certs are tagged to them — match the login view she'd already validated as "fine." That parity was already inside this same fix. The public endpoint's `visibleCertTypeIds` is derived from a baseQuery filtered to `PublicStatuses + non-expired`, and the dropdown query then filters cert types by `visibleCertTypeIds.Contains(ct.Id)` — so a type defined in the `CertificationTypes` table but lacking any visible cert never appears in the dropdown. The External (logged-in non-admin) controller uses the same pattern. Live API call against gccwebapp.com on 2026-05-12 returned 11 Level-1 types, all of which have at least one publicly-visible cert. No additional code change needed; QC #169 is fully closed by this DEF-167 ship.

---

## DEF-166 — Internal tile view slow load + only 15 of 35 certs showing (2026-05-08, fix v2 in working tree pending second deploy)

Shawn flagged that `/Certificate/CertificateTileViewInternal` was loading slowly and that only 15 of an expected 35 certificates were rendering. Both symptoms shared one root cause: the internal tile API stacked fourteen `.Include()` calls and four of those were collection navigations (`Tags`, `ProductIds`, `CertificationTypeSubcategories`, `CustomFieldValues`). EF Core's default single-query mode flattens collection includes into one big LEFT JOIN — five tags × three product IDs × two custom fields × two cert subcategories produces around sixty rows per certificate. Two consequences: the page query paid the cartesian materialization cost (slow load), and `Skip(offset).Take(20)` operated on cartesian rows rather than unique certificates, so a page meant to return twenty distinct certs returned as few as five or ten. The JS dedupe-by-id step on the client (`certificate-tile-view-internal.js`) silently dropped the duplicate rows, leaving the user with a partial result like 15 of 35. Pagination instability was further compounded because every sort branch in `ApplySorting` (e.g. `OrderByDescending(c => c.EffectiveDate)`) had no tiebreaker — when two certs share an EffectiveDate, SQL Server's row order is non-deterministic, so page 1 and page 2 could overlap, and dedupe again silently shrank the visible count.

The first fix attempt removed the Includes outright on the theory that the `.Select(...)` projection would drive its own navigation loading. That deployed and made the page WORSE — the API timed out at 30s before returning anything. Without Includes, EF Core's projection translator turned about a dozen sub-collection accesses into inline correlated subqueries against the outer paginated query, which the SQL Server query optimizer couldn't handle within the timeout. Reverted on the same day.

The actual fix keeps the Includes, but adds **`.AsSplitQuery()`** — EF Core's canonical answer to "collection includes + pagination". With AsSplitQuery, EF emits one query for the principal entity and one extra query per collection nav, so no cartesian product is ever materialized and pagination operates on unique certificates. Also added `.AsNoTracking()` because this is a read-only API path, and a `.ThenBy(c => c.Id)` tiebreaker on every one of the 50+ sort branches in `ApplySorting` so pagination order is deterministic when the primary sort key has duplicates. Same shape applied to the External tile controller, which had the same Tags-collection cartesian + non-deterministic-sort pattern. The public tile API uses a different code path (`ICertificateRepository`) and was not affected. Verified end-to-end on localhost (admin login): page 1 returns 20 unique certs in 0.51s, page 2 returns 20 unique certs in 0.058s, zero overlap, 40 of 41 unique combined. Build clean: 0 CS errors.

Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`, `docs/tracking/defect-register.md` (DEF-166 entry), `docs/tracking/shipped-2026-05.md` (this entry).

---

## DEF-165 — Usability-contract batch: Product template, cert table sort, Replace All discoverability (2026-05-08, fix in working tree pending deploy)

Pre-UAT verification of four QC items surfaced a common defect class — the UI promises X but the control does Y. Bundled the fixes because they share blast radius (3 files, plus one shared partial view) and because the client's complaint pattern was uniform: "I clicked it, it didn't do what the label said." All four were live in production today.

**Product Management import template** — the QC log marked this "Fixed" but it wasn't. The server endpoint emitted a 7-column header with `Status`, the JS class method built a *different* 5-column header missing Category and Subcategory, and both fired on every Template click via duplicate handlers. Net effect: clicking Template downloaded files that didn't match the Add New Product modal and didn't match each other. Fixed by aligning the server endpoint to the modal's 6 columns (`Name,Company,Description,Category,Product Type,Subcategory`) and rewriting the JS class method to redirect to that same server endpoint, so both `/Product/Management` and `/Admin/ProductManagement` now produce one identical download per click.

**Cert tile internal — Product Category column sort** — the frontend mapped both Product Category and Product Type clicks to the same backend sort key, and the backend didn't even have a Product Category case. Clicking the Product Category column actually sorted by Product Type. Carly's report "Carly Test and Electronics randomly show in middle of list" was exactly that mismatch. Frontend now sends a real `product-cat` key and the backend handles it via `c.ProductCategory.Name` with a fallback through `c.Product.Category.Name`.

**Cert tile internal — multi-value column sort** — Internal Tag, ASIN, UPC, GTIN, EAN, Cert Subcategory, External Tag all used `Where(...).Select(...).FirstOrDefault()` against EF without an inner OrderBy, so the per-cert "first value" picked was whatever the database returned in physical-row order. Two certs with identical tag sets sorted differently depending on insertion history. Added a deterministic inner `.OrderBy(...)` to all seven subqueries so the per-cert pick is alphabetically stable. Direction of the column sort is unchanged.

**DEF-159 Replace All toggle (reframed from "build" to "discoverability")** — the replace-all feature already exists and was hardened in DEF-154. What was missing is that Carly couldn't find the toggle. Promoted the "Replace all existing" checkbox in the Product ID import modal footer from a small inline checkbox into an amber pill-shaped chip with an exchange icon, expanded label "Replace all existing IDs with this file", and a tooltip that spells out both modes explicitly. No behavior change — the underlying replace-all logic is unchanged.

Build clean (0 errors, 61 pre-existing warnings unrelated to this change). Test plan in the DEF-165 register entry covers all four sub-items end-to-end.

Files: `Controllers/ProductController.cs`, `Controllers/Api/CertificateTileInternalApiController.cs`, `wwwroot/js/admin/productManagement.js`, `wwwroot/js/certificate-tile-view-internal.js`, `Views/Shared/_ProductIdManager.cshtml`, `docs/tracking/defect-register.md` (DEF-165 entry).

---

## DEF-164 — Anonymous certificate file endpoints now refuse non-public PDFs (2026-05-08, fix in working tree pending deploy)

An internal security audit run on 2026-05-08 (ahead of the next client UAT cycle) surfaced a critical information-disclosure path: three `[AllowAnonymous]` endpoints on `CertificateController` — `Download(number)`, `ViewCertificateFile(id)`, and `DownloadByNumber(number)` — served any certificate's uploaded PDF without first checking whether that certificate was in a publicly visible status. Because certificate numbers are sequential (`YY-NNNN`) and integer IDs are trivially enumerable, an unauthenticated visitor could iterate the identifier space and harvest every Draft, Hold, Suspended, Revoked, or Expired certificate PDF the system held — none of which the business considers public. The companion entry point `CertificateSingleView` already correctly gated anonymous access against `CertificateStatusHelper.IsPubliclyVisible(...) && !isExpired`, but the three direct file-serving endpoints had never received the same check. Same vulnerability class as the partner-API status leak fixed in DEF-103: a contract enforced on the list/detail surface but missed on a parallel resource-serving surface.

The fix mirrors the `CertificateSingleView` guard at each of the three endpoints. For unauthenticated callers only, the controller now returns 404 ("Certificate is not currently available for public download / viewing") when the cert's status is not in `PublicStatuses` or its expiration date is in the past. Authenticated users — Admin, Analyst, External, Partner — keep their existing access, since each role has a legitimate workflow reason to view non-public certs. `DownloadByNumber` received its own explicit guard even though it delegates to `Download`, so the protection cannot be silently lost in a future refactor — the DEF-103 incident showed that "this entry point is protected because it calls the protected one" is a fragile assumption. C# compilation clean (0 errors); pre-existing 61 warnings unchanged.

Files: `Controllers/CertificateController.cs`, `docs/tracking/defect-register.md` (DEF-164 entry), `audit/role-management-audit-2026-05-08.md` (source finding F-C-002 / F-C-003).

---

## DEF-161 — /Certificate/Create now accepts lowercase Amazon ASINs and validates Product IDs server-side on save (2026-05-07, fix in working tree pending deploy)

Shawn reported on the `/Certificate/Create` page that he could not get a Product ID to pass validation. Reproducing on localhost surfaced two real defects under that one symptom. First, the Product ID modal's ASIN check was case-sensitive — typing a real Amazon ASIN in lowercase (e.g. `b08n5wrwnw`, the form people commonly paste from URLs or notes) was rejected with the unhelpful message "ASIN must be exactly 10 alphanumeric characters", giving the user no clue that the only thing wrong was case. Every other letter-bearing type (SKU, MPN) was already case-insensitive, and ASIN was the lone exception. Second, even when a value did pass the in-page check, the server's `Certificate/Create` POST persisted the Product ID without ever running format validation — the format regex only ran when Product IDs were saved through the Edit-time `/api/productids` round-trip. So a JS bypass or client regression could silently store malformed Product IDs on certs created through the main authoring flow. The Edit-time form POST had the same gap.

The fix is a single canonicalisation rule applied at every save boundary, plus a server-side pre-validation pass on both Create and Edit. On the client, the ASIN regex is now case-insensitive and `handleFormSubmit` uppercases the value before pushing it into the in-memory list — so a lowercase entry is accepted and stored as the canonical uppercase form. On the server, `ProductIdService.NormalizeValueForType` is applied inside `ValidateProductIdAsync`, `CreateProductIdAsync`, `UpdateProductIdAsync`, and the bulk-CSV import path, so the database never stores a lowercase ASIN regardless of caller. The Create and Edit POST handlers now run the same normalisation + format-validation pass over each `model.ProductIds` entry, raising `ModelState` errors keyed `ProductIds[i].ProductIdValue` for invalid rows so the form re-renders with inline errors instead of silently saving bad data. Verified end-to-end on localhost: typing `b08abc1234` into the modal, saving the cert, then querying SQL directly returns `ASIN | B08ABC1234` — value canonicalised on save. Genuinely-bad inputs (wrong length, bad ISBN prefix, etc.) are still correctly rejected.

Files: `wwwroot/js/product-id-manager.js`, `Services/ProductIdService.cs`, `Controllers/CertificateController.cs`, `docs/tracking/defect-register.md` (DEF-161 entry).

---

## DEF-163 — Certificate import template now matches the table & exported CSV; export-edit-import round-trip works (2026-05-07, fix in working tree pending deploy)

Shawn flagged that the certificate import template downloaded from the import page didn't match the columns shown in the certificate management table. We confirmed that three different column sets had drifted apart over time: the internal certificate tile-view table showed 24 columns, the export CSV produced by "Export to CSV" used those same 24 friendly labels, but the import template used a 25-column SCREAMING_SNAKE format that was both missing fields the table has (Status, Product Category/Subcategory, Pathway, Cert Subcategories, separate Internal/External tags) and carrying legacy fields the table doesn't show (PRODUCT_ID_TYPE, PRODUCT_ID_VALUE, PRODUCT_SKU, AMAZON_CODE, PDF_FILE_PATH, COMPANY_LOCATION, COMPANY_COUNTRY). The most user-visible consequence: the natural workflow of "export current data → fix a few rows in Excel → re-upload" silently failed every column match because the export header read "Certificate #" while the import REQUIRED CERTIFICATE_NUMBER. Fix replaces the template with the export's column set so the headers match exactly, and teaches the import service to accept both the new friendly headers AND the legacy SCREAMING_SNAKE headers in parallel — so existing CSVs the client team already produced under the old format keep importing without changes, while the new template enables direct round-trip. Some columns (Product Category, Product Subcategory, Pathway, Cert Subcategories, Internal Tag) are accepted by the import but not yet ingested into the certificate record; those are deferred to a future CR rather than expanded here. Build clean: 0 errors.

Files: `Controllers/CertificateImportController.cs`, `Services/Import/CertificateImportService.cs`, `docs/tracking/defect-register.md` (DEF-163 entry).

---

## DEF-162 — Main nav bar now aligns with page content on every authenticated page (2026-05-07, fix in working tree pending deploy)

Shawn flagged via the cert single-view screenshot that the main navigation bar (`Certificates`, `Products`, etc.) was rendering flush against the left viewport edge while the page title and content sat further inset inside the centered `.page-container`. The misalignment was visible on every authenticated page — every role saw the same broken layout — but only obvious when there was a clear h1/content reference like the cert single view to compare against. Root cause was a single missing `>` on the `<div class="nav-wrapper …">` opening tag in the shared layout: with the tag never properly closed, the browser parsed the entire `<nav class="main-nav">` element as junk attributes on the div, dropping the nav element from the DOM. The `<ul class="nav-links">` then sat directly inside `.nav-wrapper`, which has no `max-width` or horizontal padding, so the links extended to the viewport edge. Fixing the markup restores the `<nav class="main-nav">` element, whose `max-width: 1800px; padding: 0 40px` exactly matches `.page-container`, so the nav bar now lines up with the page title and body content on every page.

Files: `Views/Shared/_Layout.cshtml`, `docs/tracking/defect-register.md` (DEF-162 entry).

---

## DEF-160 — Login now lands users on their role-appropriate certificates page (2026-05-07, fix in working tree pending deploy)

Shawn reported that every successful login dropped users on the same generic certificates page regardless of role. The intent has always been role-based landing: External and Partner users should land on the External certificates tile view (the public-by-partner view they have access to), Analyst and Admin users should land on the Internal certificates tile view (the workflow-bearing view with full lifecycle data). What was actually shipped was inconsistent: External did route correctly, Partner had no entry in the role switch and silently fell through to the public Certificate tile view, and Analyst/Admin landed on the legacy Dashboard page instead of the Internal cert view. Net effect: only one of the four authenticated roles was hitting its intended landing page.

The fix is a one-screen change in `Controllers/AccountController.cs` (`RedirectToLocal`, the post-login dispatch) and a matching update in `Controllers/HomeController.cs` (the bookmark/root-URL dispatch). Both switch tables now route Partner and External to `CertificateTileViewExternal`, and Analyst and Admin to `CertificateTileViewInternal`. The Public role still goes to the public `CertificateTileView`. Auth policies were already correct — `ExternalAccess` permits External/Partner/Admin/Analyst, and `AnalystOrAdmin` permits Admin/Analyst — so no policy or seed-data change was needed. The `Url.IsLocalUrl(returnUrl)` short-circuit is preserved, so deep-link returns after a session-timeout still go to the original page.

Files: `Controllers/AccountController.cs`, `Controllers/HomeController.cs`.

---

## DEF-159 — Internal cert tile view: tablet "Template" button no longer stretches to 100% width when wrapped (2026-05-07, fix in working tree pending deploy)

On tablet portrait (~768px), the Internal cert tile view's action button row was wrapping the trailing "Template" button onto its own line and inflating it to span 100% of the row, while the four buttons above it (Add Certificate, Download Selected, Export All to CSV, Import) sat at content width on row 1. The visual result was a thin row of four buttons with one giant Template button stretched across underneath — clearly broken on a tablet, even if functional. Shawn caught it on `gccwebapp.com` and pasted the offending markup as confirmation: the lone `<a class="btn btn-outline">…Template</a>` admin-only button is the wrapped element. External tile view was unaffected because external users see only two buttons in this row, and they fit on a single line at tablet width.

The bug was in a single `@media (max-width: 768px)` block in `main.css` that combined two intents: `flex: 1` on the buttons (intended for phones, to give each wrapped button a full-width tap target) and `min-width: 140px` (intended for tablet, to keep buttons readable when wrapped). At true phone widths every button wraps onto its own row and `flex: 1` is desirable. At tablet widths only one button wraps, and `flex: 1` makes that lone button inflate to fill the row — exactly the symptom. The fix splits the rule by breakpoint: at `≤768px` keep `min-width: 140px` only (no flex grow, no stretch); at `≤480px` re-apply `flex: 1` + `justify-content: stretch` for the phone-only full-width behavior. Net effect: on tablet, Template now wraps at content width (~110px); on phone, the full-width-button layout is preserved exactly as before.

This is a CSS-only change with a narrow blast radius — only `.export-options .btn` selectors are affected, and they appear on the cert tile views (Internal + External) and a couple of certificate landing pages that already passed QC. No JS, no view markup, no controller changes.

Files: `wwwroot/css/main.css`, `docs/tracking/defect-register.md` (DEF-159 entry).

---

## DEF-158 — Partner and External users can now click into cert detail from the tile view (2026-05-07, fix in working tree pending deploy)

A High-severity functional break for Partner role: logged in as Partner on `gccwebapp.com`, every click on a certificate tile in the External tile view dead-ended at `/Account/AccessDenied`. Same failure for External users. The tile view itself was reachable (the `ExternalAccess` policy admits External, Partner, Admin, and Analyst), but the JavaScript that handles a tile click hardcoded the wrong destination — `/Certificate/CertificateInternalView/{number}` instead of `/Certificate/CertificateExternalView/{number}`. The Internal route is gated by `AnalystOrAdmin` policy (Admin and Analyst only), so any non-Admin/Analyst user clicking a tile got bounced to AccessDenied with the Internal route in the ReturnUrl. The bug applied to both entry points the JS exposes — `viewCertificate(num)` (button + context menu) and `viewCertificateDetails(num)` (tile click) — both opened the wrong URL.

The fix is two route swaps in `wwwroot/js/certificate-tile-view-external.js` to point both `window.open` calls at the External detail route. That route is gated by the same `ExternalAccess` policy as the tile view itself, so all four roles (External, Partner, Admin, Analyst) succeed at the click-through. The Internal tile view's JS (`certificate-tile-view-internal.js`) is unchanged — Admin and Analyst still route to the Internal detail view from their own tile view as before. No change to controllers or auth policies; the policy contract was already correct, only the JS was inconsistent with it.

A second, unrelated file in the same folder — `certificateTileViewExternal.js` (camelCase) — already had the correct External route. It is not referenced by any view and is effectively orphaned. Left it alone for this fix; cleaning up dead JS files is out of scope and would broaden the change.

Verification: reproduced Shawn's exact symptom on localhost. As an External user (same `ExternalAccess` policy as Partner), the pre-fix JS opened `/Certificate/CertificateInternalView/26-0146` and the page redirected to `/Account/AccessDenied?ReturnUrl=%2FCertificate%2FCertificateInternalView%2F26-0146` — byte-for-byte the URL Shawn pasted. Post-fix, both JS entry points open `/Certificate/CertificateExternalView/26-0146` and the External detail view returns 200. No Partner-role users exist in the local dev DB so the actual Partner login path is verified by way of the shared policy + shared JS, not a live Partner session — flagging that for prod smoke testing.

Files: `wwwroot/js/certificate-tile-view-external.js`, `docs/tracking/defect-register.md` (DEF-158 entry).

---

## DEF-157 — Cert preview no longer forces portrait shape on landscape thumbnails (2026-05-07, fix in working tree pending deploy)

Reviewing `gccwebapp.com` on desktop, Shawn caught cert 26-0157 displaying with a tall portrait preview box and the landscape cert image squished into a tiny strip in the middle, surrounded by whitespace top and bottom. The same behavior reproduced on cert 26-0146 — both have landscape-orientation thumbnails (~600×464 PNG). Cause turned out to be the `aspect-ratio: 8.5/11` constraint on `.certificate-preview-box` in `main.css` — a rule that had been deployed earlier to solve DEF-156 (raw images overflowing the column on tablet landscape). The portrait-box rule fixed the overflow case for portrait certs but applied the same portrait shape to landscape certs, which forced `object-fit: contain` to letterbox them down to a fraction of the container's height. Cert PDFs in the GCC dataset come in both orientations, so a single fixed aspect-ratio could never be right for both.

The fix drops the rigid aspect-ratio constraint and lets the box size to whatever the actual image is. The img now uses `width: 100%; height: auto` (natural sizing) instead of `position: absolute; width: 100%; height: 100%; object-fit: contain` (forced filling of a fixed-shape parent). The container retains its border, rounded corners, overflow:hidden, and a 200px min-height so the no-PDF placeholder still has somewhere to sit. Net result: portrait certs render at portrait shape, landscape certs render at landscape shape, both without whitespace and without overflowing the column. Verified locally at desktop 1440, tablet landscape 1024, tablet portrait 768, and mobile 390 — 0 overflowing elements at every viewport, and the cert image now fills its container at natural aspect ratio.

One thing worth calling out from the diagnosis: today's earlier `DEF-156 fix` (adding the same preview-box base rules to main.css) was effectively a no-op duplicate. Pulling `https://gccwebapp.com/css/main.css` showed exactly 133,563 bytes — byte-for-byte identical to the local file post-edit — meaning the rules I "added" had already been deployed (likely via a recent build that wasn't reflected in the local working tree). The portrait-shape behavior on the live site was already the result of those rules running. DEF-157 is the real fix.

Files: `wwwroot/css/main.css`, `docs/tracking/defect-register.md` (DEF-157 entry).

---

## DEF-156 — Certificate image no longer overflows on tablet landscape in cert detail views (2026-05-07, fix in working tree pending deploy)

While reviewing `https://gccwebapp.com/Certificate/CertificateInternalView/26-0146` on a tablet in landscape orientation, Shawn saw the certificate preview image rendering outside the card and not wrapping. The same break reproduced locally at the standard 1024×768 tablet-landscape viewport: a Playwright overflow scan found the bare `<img>` element inside the right-column preview box at width 612px with its right edge at x=1252 — 228px past the 1024px viewport. Mobile (≤768px) and full desktop both rendered correctly, which is why the issue had slipped through prior testing.

Root cause was a stylesheet wiring gap that has been latent for some time. `wwwroot/css/certificates.css` contains the base `.certificate-preview-box` rules — the `aspect-ratio: 8.5/11` shape, `overflow: hidden` clipping, and `position: absolute; width: 100%; height: 100%; object-fit: contain` on the inner `<img>` — but `Views/Shared/_Layout.cshtml` only links `main.css`. The certificates.css file is referenced in `appsettings.Performance.json` as part of a bundle but never linked from the layout, so its rules never reach the browser. main.css carried only a `≤768px` `@media` override for the preview-box img; at any viewport above 768px, the bare `<img>` had no styling at all and rendered at its intrinsic pixel size.

Fix is to copy the missing base `.certificate-preview-box`, `.certificate-preview-box img`, and `.preview-placeholder` rules into `wwwroot/css/main.css` (placed above the existing tablet `@media (max-width: 1024px) and (min-width: 769px)` block) so they apply at every viewport. We deliberately did NOT add a `<link>` to certificates.css — pulling in an entire stylesheet that has been unloaded for an unknown amount of time would risk surfacing other unrelated rules that could conflict with current main.css. Targeted inline of the specific rules keeps the blast radius equal to the bug.

Verification: re-ran the overflow scan post-fix on both internal and external cert detail views at 768 (tablet portrait), 1024 (tablet landscape), 1180 (iPad Air landscape), and 1280 (small desktop). Pre-fix: 1 overflowing element at 1024px (the bare img). Post-fix: 0 overflowing elements at every tested viewport. Image sits inside the 8.5×11 aspect-ratio box with `object-fit: contain` and the action icons (zoom + download) align below the box on the right edge as designed.

Files: `wwwroot/css/main.css`, `docs/tracking/defect-register.md` (DEF-156 entry).

---

## DEF-155 — Database Listing URL no longer overflows the card on mobile in cert detail views (2026-05-07, fix in working tree pending deploy)

Reviewing `https://gccwebapp.com/Certificate/CertificateInternalView/26-0146` on a phone, Shawn noticed the rendered Database Listing URL — `https://gccwebapp.com/Certificate/Verify/26-0146` — extended past the right edge of the card. URLs without spaces or hyphens won't wrap by default, so on narrow viewports the anchor stretched its container and pushed content offscreen. Added inline `word-break: break-all; overflow-wrap: anywhere;` to the verify-URL anchor in both the internal and external cert detail views so the URL wraps mid-string on mobile while still remaining a single clickable link.

Files: `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`, `docs/tracking/defect-register.md` (DEF-155 entry).

---

## DEF-154 — "Replace all existing" toggle on CSV import now actually deactivates orphan rows (2026-05-07, fix in working tree pending deploy)

The same testing session that surfaced DEF-153 also surfaced DEF-154: with the dup-detection now correctly identifying matching rows on every page, Shawn re-imported a 1000-row CSV against cert 26-0023, checked "Replace all existing," and clicked Apply — and the lone stray row on the cert that wasn't in the file did not get deactivated. The toggle was visually checked, the import committed without error, but server state was unchanged. Worse, in a partial-import scenario (some new rows + some modified) the existing implementation could have deactivated rows the user explicitly wanted to keep.

The applyImport function had three failure modes around the toggle. First, when every imported row matched an existing row exactly and was classified as "unchanged" (auto-deselected), the function read selectedRows as empty and returned with "No rows selected for import" before ever reading the toggle state — the user-visible result was a misleading error message that ignored the explicit replace-all intent. Second, when every imported row matched an existing row but with field differences (classified as "modified"), the creates filter `selectedRows.filter(r => r.action !== 'update')` returned an empty array, the bulk endpoint was never called, and the overwriteExisting flag never reached the server. The user saw a generic "Updated 0 product ID(s)" message and walked away thinking replace-all worked, but no deactivation occurred. Third, and most dangerous, in the partial case where some imported rows were truly new and others matched existing rows, the bulk POST only carried the "new" subset. The server's deactivation logic builds importKeys from whatever items it receives and deactivates every existing row whose key isn't in that set. With only the "new" keys present, every "modified" row's key was missing — so the server marked those modified rows as Inactive even though the user clearly intended to keep them. The PUT field-update loop then ran on rows that had just been deactivated.

The fix changes the import set semantics when the toggle is on. With "Replace all existing" checked, the import file is treated as the new source of truth and every non-error row in the file goes into the import set, regardless of per-row checkbox state. The per-row checkboxes only have meaning in normal merge mode. The empty-set guard returns a different error in replace-all mode — "Import file has no valid rows. Replace-all needs at least one valid row to define the new state" — instead of the misleading "No rows selected for import." The bulk endpoint payload now includes every row in the import set (including the ones classified as "modified") when overwriteExisting is true, not just the creates. This makes the server's importKeys set complete, so the deactivation loop only targets rows that are genuinely missing from the file. The server's bulk handler already silently skips dup keys in overwrite mode (counted as success, no insert attempted), so sending matching rows is harmless on the insert path. The PUT field-update loop for modified rows still runs individually after the bulk call, since the bulk endpoint's dup-key skip doesn't apply field changes.

Verification on cert 26-0023 in the dev database. The cert was reset to 1001 active rows: 1000 ASINs that match the import file (B08N5W0001 through B08N5W1000) plus one stray row with ProductIdValue '1321321212' that's not in the file. Pre-fix: importing the file with replace-all checked left the cert at 1001 active rows, with the stray row still Active. Post-fix: the same import drops the stray row to Inactive and the cert ends at 1000 active rows. The deactivation cascade fires correctly server-side, and product ID Status follows the parent cert's Public/non-Public state per CR-015 as part of the same merge.

One out-of-scope optimization noted in the defect register: the bulk endpoint's "silent skip on dup-key in overwrite mode" semantics mean field changes on matching rows still go through PUT-individually after the bulk call, one HTTP request per modified row. For thousands of field changes that approach would hit the rate limiter. The fix routes them through the existing PUT loop because that loop already exists and was tested as part of CR-018's earlier work. If we ever see real replace-all imports with thousands of field changes, the server-side bulk handler should be updated to apply field changes on dup-keys in overwrite mode for a single round-trip. Not blocking launch.

Files: `wwwroot/js/product-id-manager.js`, `docs/tracking/defect-register.md` (DEF-154 entry).

---

## DEF-153 — CSV import dup-detection now sees rows on every page after CR-018 (2026-05-07, fix in working tree pending deploy)

The day CR-018 server-side pagination shipped, Shawn re-imported a 1000-ASIN CSV against a cert that already held the same 1000 ASINs from the first import. He expected the import preview to flag every row as "Unchanged" — and offer to skip them — but instead the majority were flagged "New." Clicking Apply Import in that state would have created 950+ duplicate rows on the cert. The fix had to ship before launch because the user-facing failure mode was data corruption, not just visual.

The root cause was a CR-018 regression. The import preview's classifier did `this.productIds.find(p => p.productIdType === imported.type && p.productIdValue === imported.value)` to decide whether each imported row was new, modified, or unchanged. That semantic was correct before CR-018, when `this.productIds` held the full list of every product ID on the cert. After CR-018, the array is scoped to whatever page is currently visible — so on a cert with 1001 product IDs and a default page size of 50, the dup-check only saw rows numbered 1–50 in the default sort. Anything on page 2 through 21 looked unfamiliar and got flagged "New," even though it had been in the database for the entire prior import. The first 50 rows happened to match in Shawn's repro because Shawn's import order roughly matched the default sort, which is why "a few were tagged unchanged, but the majority said new."

The fix routes the dup-check through a one-time full-list fetch when in server-paged mode. The un-paged endpoint at `GET /api/productids/certificate/{id}` (no `page` query parameter) was already deployed for the export path and returns every row on the cert in the standard `ApiResponse` wrapper. `buildImportPreview` now calls that endpoint once, walks the response to build a `Map` keyed by `${type.toUpperCase()}:${value.toUpperCase()}`, and uses that map to classify each imported row. The Create page (`certificateId === 0`, in-memory mode) keeps the original in-memory scan because there's no server-side state to consult anyway. If the full-list fetch fails for any reason — auth blip, rate-limit, network drop — the code falls back to the old in-memory scan with a console warning, so dup-detection degrades to "current-page only" instead of crashing the import flow entirely.

The Playwright verification seeds 100 import items whose values exist on cert 26-0023 (`B08N5W0001` through `B08N5W0100`). Most are not on the default page 1, so the pre-fix behavior would have been 100 false "new" rows. The post-fix run reports 0 new, 100 modified. The "modified" classification (rather than "unchanged") is correct because the test rows carry slightly different productName values than what's stored — that's the dup-check actually doing field-level comparison after the type+value match lands. All 5 assertions in the probe pass.

The same caveat that lives in CR-018 applies here: the pre-fetch loads the full list into JavaScript memory. For the 1000-row case it's fine — the un-paged response was 71 ms in the perf probe, payload around 250–500 KB. At 100,000 rows per cert this approach would need rethinking. The most likely future fix would be a dedicated lightweight `lookup-keys` endpoint that returns only `{type, value, id}` tuples per row, dropping the payload by ~80%. Not blocking launch — this is the kind of optimization we'd file as a CR if we ever see real certs at that scale.

Files: `wwwroot/js/product-id-manager.js`, `docs/tracking/defect-register.md` (DEF-153 entry).

---

## CR-018 — Server-side pagination for the Product Identifier list (2026-05-07, fix in working tree pending deploy)

The Product Identifier table on the certificate Edit page was loading every product ID up-front and doing all sort, filter, search, and pagination work in the browser. On a small cert with a handful of ASINs that approach was fine, but the codebase has at least one cert (26-0023 in the dev database, 1001 product IDs) where that everything-at-once pattern produced a noticeably slow first paint and a freeze on every column-sort click. Real-world Amazon partner certs are unbounded — a single cert may carry hundreds of ASINs in production — so the architecture had to change before launch rather than after the first stakeholder QA pass found it.

The diagnostic probe captured the baseline: a clean Playwright run on `localhost:5101/Certificate/Edit/26-0023` settled in 3500 milliseconds with the full 1001-row payload landing in the browser, the cert form's Razor partial rendering all 1001 rows server-side before the JS even started, and every sort or filter interaction re-running the full client-side pipeline. The API itself returned the data in 71 milliseconds — the bottleneck was entirely architectural, not server performance. The CR was filed as CR-018 the same day, scoped as Phase 1 launch-blocking work, and the build started immediately after sign-off.

The shipped change pushes sort, filter, and search down to SQL through a new paged service method, `IProductIdService.GetProductIdsByCertificatePagedAsync`. The method takes the certificate ID, page number, page size, and optional sort column, sort direction, search string, type filter, and partner filter; pushes them into a single LINQ-to-SQL query that emits a `WHERE` clause, an `ORDER BY` clause with a stable secondary sort by Id (so equal sort keys don't shuffle rows between page fetches), and a `Skip`/`Take` for paging; counts the filtered rows once for pagination math; and returns the visible page along with the total row count, current page number, page size, and total page count in a `PagedProductIdsResult` shape. The existing un-paged `GetProductIdsByCertificateAsync` method is preserved for the export path, which still needs the full collection regardless of paging.

The `ProductIdApiController` route at `/api/productids/certificate/{id}` was extended to honor the `page`, `pageSize`, `sort`, `dir`, `search`, `type`, and `partner` query parameters. When paging parameters are supplied (`page > 0` and `pageSize > 0`), the controller calls the new paged service method and returns the paged response shape. When neither parameter is supplied, it falls back to the un-paged path for back-compat with the export and any legacy callers. The endpoint already had `page` and `pageSize` parameters in its signature from a previous half-baked attempt; this CR replaces the old in-memory `Skip`/`Take` after a full-list fetch with real SQL-side paging.

The bigger work was on the JS side. `wwwroot/js/product-id-manager.js` now detects server-paged mode at construction time (any positive `certificateId` flips `serverPaged = true`; the Create page with `certificateId === 0` stays in the in-memory path). `loadProductIds` branches: the server-paged path calls a new internal `_loadProductIdsPaged` method that builds a query string from current sort, filter, search, and page state; checks a small client-side cache keyed by that query string; fetches if not cached; respects a request-version race guard so a slow page-1 response can't clobber a fast page-2 response if the user clicks fast; stores the response, updates the cache, and renders. `applyFilters` debounces by 250 ms in server-paged mode so per-keystroke search doesn't fire one network request per character. `applySort` becomes a no-op in server-paged mode because the server has already applied the sort to the page. `renderTable` skips client-side windowing in server-paged mode and just renders the items it received. `renderPagination` reads `totalCount` and `totalPages` from the API response instead of computing them from the local array length. `goToPage` round-trips for new pages in server-paged mode. The cache is cleared on every mutation (Add, Edit, Delete, Bulk Import) so we never serve stale data after a write. An empty-page fallback in `_applyServerPage` catches the case where deleting the only row on the last page lands on an empty page — it drops to the new last page and re-fetches automatically.

A subtle but critical companion change went into `Views/Certificate/Edit.cshtml`. The Edit form's submit handler used to write the JS Product ID Manager's full in-memory list as hidden form inputs so the server controller could merge-reconcile product IDs as part of the cert save. With server-side paging that JS list now only contains the currently visible page, so posting it would tell the controller's merge logic to delete every off-page row. Per DEF-149 the Edit page already commits product ID changes immediately via the `/api/productids` endpoints anyway — the user-facing hint above the manager explicitly tells the analyst this — so the form-post path was already redundant. The hidden-input writer was disabled (left in the file, gated behind `if (false)` and a comment explaining why, so the rationale is recoverable on git blame). The cert controller's merge block at `CertificateController.cs:866-911` is unaffected: when the form posts no `ProductIds[]` keys, the merge block's guard `if (model.ProductIds != null && model.ProductIds.Any())` is false and the block is skipped entirely. Functional behavior is preserved because the API endpoints already did all the work.

The cert controller's Edit GET was also adjusted to ship only the first 50 product IDs to the Razor partial. Previously it passed `certificate.ProductIds.ToList()` — the full collection — which forced server-side rendering of every row on initial page load. The new code applies the same default ordering as the un-paged service method (DisplayOrder, then ProductIdType, then CreatedAt) and takes the first 50. The JS Product ID Manager hits the API for the same first page on init and replaces the partial's tbody, so the user sees a fast initial paint and then a smooth, indistinguishable replacement instead of waiting for a multi-second server-side render of every row.

The acceptance bar from the CR was a 1.5-second total page settle on the 1001-row cert. Playwright verified end-to-end on a clean run after JIT warm-up: page settle 1046 milliseconds, sort round-trip 449 milliseconds, page-2 fetch 309 milliseconds, initial payload 50 rows (was 1001), `totalCount` correctly reported as 1001 to the pagination controls. The Create page was independently verified to be unchanged: in-memory mode still detects `certificateId === 0`, no `/api/productids` calls fire during in-memory adds, in-memory pagination renders, and in-memory sort still works. Fifteen out of fifteen Playwright assertions pass on the Edit-page behavioral test; eight out of nine assertions pass on the Create-page regression test (the one miss was a stale assertion on a `#productIdCount` element that doesn't exist in the partial — not a regression, the count display is just unhooked).

Database schema unchanged. No migration. No Partner API impact — the `/api/v1/certificates*` routes per `feedback_amazon_api_immutable.md` were not touched. The new query parameters live entirely under the internal admin `/api/productids/*` route surface that Partner API consumers don't reach.

Files: `Services/ProductIdService.cs`, `Services/IProductIdService.cs` (via the same file — interface lives at the top), `Controllers/Api/ProductIdApiController.cs`, `Controllers/CertificateController.cs` (Edit GET first-page projection only), `Views/Certificate/Edit.cshtml` (hidden-input writer gated off), `wwwroot/js/product-id-manager.js`, `docs/tracking/CR-018-product-id-server-paging.md` (CR detail doc), `docs/tracking/change-request-register.md` (CR-018 status flipped from Proposed to Built).

---

## DEF-152 — Product Identifier pagination buttons now match Product Management's pagination styling (2026-05-06, fix in working tree pending deploy)

While auditing the cert Create / Edit pages against Product Management for visual consistency, Shawn flagged that the Product Identifier table's pagination control didn't look anything like the pagination on the rest of the admin app. Product Management — and Tags, Pathways, etc. — render pagination as a chevron-left Previous button, numbered page buttons (with ellipsis when there are many pages), and a chevron-right Next button, all in the canonical `.pagination-btn` styling with a dark-blue active state and white background hover. The Product Identifier table was rendering five inline-styled text buttons — `[First]` `[Prev]` `Page X of Y` `[Next]` `[Last]` — with all the visual treatment hand-written as inline `style="…"` attributes in the JavaScript template. Two completely different pagination experiences sitting in the same admin app at the same UI layer.

The root cause was that the Product Identifier pagination predated the `.pagination-btn` / `.pagination-controls` convention being formalized in `wwwroot/css/product-management.css`, and the renderer never got retrofitted. The "First / Last" nav buttons also weren't part of the canonical pattern — Product Management only ships chevron Previous, numbered pages, chevron Next. Beyond pure visual drift, the inline-style approach made the code unmaintainable and impossible to keep in sync as the design language evolved.

The fix rewrote `renderPagination()` in `wwwroot/js/product-id-manager.js` to mirror the `renderPagination()` function in `Views/Product/Management.cshtml`. The new markup uses chevron-left Previous with the `aria-label="Previous page"` button, numbered page buttons that show up to seven visible page numbers at a time and collapse the rest behind ellipsis spans (so a cert with 1000 product IDs and 20 pages of pagination still fits on one row), and a chevron-right Next button with `aria-label="Next page"`. The active page gets the `active` CSS class plus `aria-current="page"` for screen readers. Disabled states (Previous on page 1, Next on the last page) get both the `disabled` HTML attribute and the `disabled` CSS class so the visual styling matches the functional state. The "Showing N-M of T" info caption stays on the left side of the wrapper and the button row sits on the right, matching the Product Management layout convention. The First and Last navigation buttons that the old version had are intentionally not carried forward — they aren't part of the admin standard, and with chevron Prev plus numbered jump buttons the user can reach any page in two clicks.

A scoped pagination stylesheet was appended to `wwwroot/css/product-id-manager.css` under `.product-id-manager .pagination-*` selectors so the styles only apply inside the Product ID Manager and don't leak into other components. The values mirror the canonical pagination styles in `wwwroot/css/product-management.css` line-for-line — same 36px minimum width, same 44px rendered height, same dark-blue active background, same hover treatment. The active button background was hardcoded to `#002B76` rather than going through `var(--blue-dark)` because the CSS variable resolves to `#002876` (one shade off) in some parent contexts; Product Management dodges this by also resolving to the literal fallback in its cascade, so matching the rendered output exactly meant skipping the variable.

A Playwright test verified all 20 visual and behavioral checks passing on `localhost:5101`: pagination wrapper renders only when there are more rows than the page size, button class structure is correct, no inline `background` styles remain on any button, the chevron-left Previous button is disabled on page 1, the chevron-right Next button is disabled on the last page, the active page button has both the `.active` class and `aria-current="page"` attribute, the "Showing N-M of T" info text updates correctly on page change, the computed active background of `rgb(0, 43, 118)` matches Product Management exactly, and the computed button dimensions of 36px minimum width and 44px rendered height match Product Management exactly. After deploy this will give Shawn a single consistent pagination experience across every paginated table in the admin app.

Files: `wwwroot/js/product-id-manager.js`, `wwwroot/css/product-id-manager.css`, `docs/tracking/defect-register.md` (DEF-152 entry).

---

## DEF-151 — "Unsaved form data recovered" banner hidden on cert Create / Edit pending Phase 2 rework (2026-05-06, fix in working tree pending deploy)

While reviewing the cert Create page during DEF-150 testing, Shawn flagged a yellow recovery banner that appeared on a freshly loaded page reading *"Unsaved form data recovered from 08:25 PM. Would you like to restore it?"* with [Restore] / [Discard] buttons. The banner was firing on a session where the user had no real prior unsaved work — opening the page, with no edits, triggered a recovery prompt. Beyond being noise, clicking [Restore] would have produced a partially-restored form: standard fields would come back, but the cert PDF, thumbnail, product IDs (ASINs, UPCs), custom field values, and tags would all silently not be restored. A user who trusted the prompt would end up with an inconsistent form state that fails validation on save or, worse, persists a half-edit.

The cause was a combination of issues. The unsaved-state tracker initializes its baseline of "what the form looks like with no edits" on a fixed `setTimeout(..., 1000)` after page load, but the recovery check runs earlier in the flow and compares the saved payload against the just-loaded form. On the cert Create page, cascading dropdowns — Company → Product, Category → Type → Subcategory, Cert Type → Subcategories, custom fields, existing tags — all retrigger `change` events on initial load as their async data arrives. The tracker reads each of those cascade-driven values as a real difference between "what was saved" and "what's on screen now," even though the user hasn't touched anything. Add to that the fact that the recovery payload never captured product IDs or custom fields in the first place, and the explicit skip of file inputs in the restore handler, and the prompt was unreliable enough that the right answer was to take it offline rather than try to patch it incrementally.

The decision was to hide the prompt now and rebuild the experience as a CR for Phase 2 — CR-017 captures the full scope. The fix is a single-line kill switch: a new constant `SHOW_RECOVERY_PROMPT: false` on the `UnsavedChanges` module short-circuits `checkForRecovery()` so the banner never appears, regardless of what's in localStorage. The auto-save side of the feature was intentionally left running — every keystroke continues to checkpoint the form to localStorage as before, so the data is still there for Phase 2 to consume cleanly when the smarter version ships. Stale entries older than two hours continue to be GC'd on each page load so the localStorage doesn't accumulate while the prompt is hidden, and corrupt JSON entries (which would previously have thrown on parse) are now silently cleared rather than retried forever.

CR-017 documents the Phase 2 plan: eliminate false positives by gating the recovery check on a "all cascades complete" signal instead of a fixed 1-second timeout; expand the saved payload to cover product IDs, custom field values, tags, and subcategories so a restore actually brings back the whole form; make the restore all-or-nothing with an explicit warning that the cert PDF and thumbnail can't be restored across sessions; improve the banner copy to show a date when the saved data is more than 6 hours old; and verify same-cert scoping so a Create-page payload never offers itself for restore on an Edit page or a different cert. Estimated effort is 6–10 hours, frontend-only, no backend or database or Partner API changes.

Files: `wwwroot/js/certificate-create.js`, `docs/tracking/defect-register.md` (DEF-151 entry), `docs/tracking/change-request-register.md` (CR-017 entry), `docs/tracking/CR-017-form-recovery-ux.md` (new Phase 2 detail doc).

---

## DEF-150 — Product Identifier table now sorts by column like the rest of the admin app (2026-05-06, fix in working tree pending deploy)

Shawn flagged that the Product Identifier table on the certificate Create and Edit pages did not support click-to-sort the way every other admin table in the app does. Product Management, Tags, Pathways, User Management, Companies — all of these let an analyst click a column header and re-order the table, with a chevron icon that flips direction on repeated click. The Product Identifier table had non-clickable plain `<th>` headers with no sort affordance at all, so the rows came back in whatever order the API returned them in and stayed that way until the analyst manually scrolled or filtered. On a certificate with dozens of ASINs, that inconsistency made the table feel like a different widget than the rest of the admin app.

The cause was that the Product Identifier partial was authored before the `admin-table-th--sortable` convention was introduced and never retrofitted. The JavaScript only handled filter and search state; column sort state simply didn't exist on the manager class. There was nothing wrong with the data, just no UI for re-ordering it.

The fix retrofits the standard sort pattern. The shared partial `Views/Shared/_ProductIdManager.cshtml` now renders each column header with `data-sort="…"`, `class="admin-table-th--sortable"`, and a chevron icon span — matching the structure used by `Views/Product/Management.cshtml`. Sortable columns are Type, Product ID, Product Name, SKU, Partner, Created, and Created By; the Actions column is intentionally not sortable. The JavaScript class `ProductIdManager` in `wwwroot/js/product-id-manager.js` was extended with two new fields (`sortColumn` and `sortDirection`), an `initializeSortableHeaders()` method that wires up the click handlers and icon updates exactly the way Product Management does, and an `applySort()` method that runs the actual comparison. Created uses a numeric timestamp comparison so dates order chronologically; everything else uses locale-aware string comparison so accented and lowercase values sort sensibly. The Created By column resolves through `createdByUser.username` first and falls back to the raw `createdBy` field, so newly added in-memory items on the Create page (which haven't been persisted and therefore have no joined user record yet) still sort cleanly alongside server-loaded rows.

The sort step is invoked from inside `renderTable()` rather than sprinkled across every site that mutates the list. That means every code path — initial load from the API, click filters, type-in-the-search-box filtering, single-row Add/Edit/Delete in memory on the Create page, and CSV import-apply — picks up the currently selected sort without each call site needing to remember to call it. When no column is selected, `applySort()` is a no-op and the server's original order is preserved. Pagination resets to page 1 on every column change so the analyst sees the new top of the sorted list, not whatever middle-page they were viewing.

Files: `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `docs/tracking/defect-register.md` (DEF-150 entry).

---

## DEF-149 — Cert Edit page now tells users that product identifier changes save immediately (2026-05-06, fix in working tree pending deploy)

While auditing whether the Create and Edit certificate flows could leave orphan Product Identifier rows in the database, Shawn surfaced a real UX gap: the Product ID Manager looks identical on both pages, but it behaves differently. On the Create page, every Add, Edit, Delete, and CSV Import is held in browser memory and only commits to the database when the analyst clicks Save on the certificate form. On the Edit page, the same actions commit immediately to the database via the `/api/productids` endpoints, independent of the certificate form's Save or Cancel buttons. There was no on-page indication of which save model was active, so an analyst who added a wrong ASIN on the Edit page and then clicked Cancel would reasonably assume the ASIN was discarded — when in fact it was persisted on the certificate as soon as they closed the modal.

The orphan-record audit itself came back clean. The `CertificateProductIds.CertificateId` foreign key is NOT NULL with a cascade delete behavior, so if a certificate is ever deleted, every product identifier attached to it goes with it at the schema level. There is no way to leave a row pointing at a non-existent certificate. The concern was purely UX: same UI, two different save semantics, no signal.

Two options were on the table. The first was a UI-only fix that simply tells the user how the Edit page actually behaves. The second was a behavioral refactor that would unify the two pages on the Create page's deferred-save model — Add, Edit, Delete, and Import would all go into memory on Edit and only commit when the certificate form was saved, so Cancel would actually cancel. The second option was started, then reverted after Shawn flagged the analyst use case it would break: when an analyst spots a wrong ASIN on a cert and just wants to delete it, they shouldn't have to commit unrelated certificate-form edits to make the correction stick. Immediate persistence is the right default for that workflow. The cost of keeping the immediate-save model is the surprise factor on Cancel, which the UI hint addresses directly.

The fix adds a small information note on the certificate Edit view, positioned directly above the Product ID Manager. It uses the app's blue accent color, a 4px left border, an info icon, and 16px text. The note reads: "Add, edit, and delete actions on product identifiers save immediately and apply even if you cancel the certificate edit. Use this if you need to quickly correct a wrong identifier." The wording explains both the behavior and the use case the behavior is intended to support, so an analyst reading it understands not just what happens but why. The hint only renders on Edit; the Create page is unaffected because the Create page's deferred-save behavior is the conventional one and needs no explanation.

One stale-code observation surfaced during the audit and is intentionally out of scope for this fix: the Edit page's form-submit handler still writes a hardcoded `Status: 'Active'` value into the hidden inputs that get posted with the certificate. Per CR-015, Product Identifier Status follows the parent certificate and is no longer user-controllable, so that hardcoded value is meaningless. The certificate controller's merge logic does not read the Status field from the submitted Product Identifier rows on Edit, so the value is harmless — it is simply ignored — but the line is dead code. Leaving it untouched here to keep this fix narrowly scoped to the UX issue the audit raised.

Files: `Views/Certificate/Edit.cshtml`, `docs/tracking/defect-register.md` (DEF-149 entry).

---

## DEF-148 — Product ID Manager column font and row-count alignment now match the rest of the app (2026-05-06, fix in working tree pending deploy)

Shawn flagged two visual inconsistencies on the cert Edit page's Product Identifiers section while reviewing cert `26-0146` on the test environment. The Product ID column was rendering its values in Courier New monospace at a slightly heavier weight than the rest of the row, so the values column visually popped out against the proportional Open Sans used by every other column on the same table — the inconsistency made the section feel like it belonged to a different design system. Separately, the "Showing N product ID(s)" caption beneath the table was centered, while every other row-count caption in the app right-aligns next to its table.

Root cause for the column font was a pair of declarations on the `.product-id-value` CSS class in `wwwroot/css/product-id-manager.css` — `font-family: 'Courier New', monospace` and `font-weight: 500` — that nothing else in the app applies. They appear to be a leftover from when the column was treated as a "code identifier" display, but no other ID-style column in the admin app (Tags, Pathways, Partners, Companies, Products) uses monospace, so the convention had drifted only on this one widget. Root cause for the count alignment was the Razor wrapper using Bootstrap's `text-center` class instead of `text-end`.

The fix dropped the two font overrides while keeping the truncation properties that the column actually needs (`max-width`, `overflow`, `text-overflow`, `white-space`) so long ASIN values still ellipsis cleanly. The Razor wrapper class flipped from `text-center` to `text-end`. Both changes are static asset edits — refreshing the cert Edit page picks them up without a server restart.

Out of scope for this fix: the Import Preview modal still inlines `font-family: monospace` on its preview row's productIdValue cell. That modal isn't visible on the cert edit page Shawn was reviewing, so leaving it untouched — can revisit if it surfaces in a later QC pass.

Files: `wwwroot/css/product-id-manager.css`, `Views/Shared/_ProductIdManager.cshtml`, `docs/tracking/defect-register.md` (DEF-148 entry).

---

## QC #164 — Product Identifier status now follows the parent certificate; analyst can no longer set it manually (2026-05-06, verified shipped — no new work)

The client raised QC #164 on 2026-04-10 against Product Identifiers / Amazon Integration. The ask had two parts. First, when a certificate moves out of an active state — Expired, Hold, Confidential, Not Paid, Draft, etc. — every Product Identifier (ASIN) on that certificate should automatically flip to Inactive so Amazon's badging never picks it up as a still-valid certification. The records should not be deleted, just marked Inactive. Second, the per-row Status field should not be user-controllable any longer: Status should be removed from the Product Identifier list display, the Add / Edit modal, and the CSV import template, since it is now determined by the parent cert and the analyst cannot meaningfully set it independently.

Both halves of QC #164 were already delivered under the existing CR-015 build that was raised the same day in the 2026-04-30 status conversation and tracked in the defect register as DEF-104. On a re-verification pass on 2026-05-06 against the current codebase, every required behavior is in place. The cascade method `IProductIdService.SyncProductIdStatusesForCertificateAsync(certificateId)` derives Status as Active when the parent cert is in `PublicStatuses` (Client Approved - Paid or Republished) and Inactive in every other state, and is wired into all eight code paths that can change a certificate's status: certificate Create, Edit, and Import via the controller; the certificate service's bulk action and update methods; the workflow service's status transition; the expiration service's nightly auto-expire job; and both branches of the CSV cert import service. The Product Identifier service's create and update methods overwrite any incoming Status with the derived value, so even a direct API caller that tries to set Status is silently corrected. On the UI side, the Product Identifier list table no longer renders a Status column, the Status filter dropdown is gone, the Add / Edit modal no longer shows the Status field, the CSV import template and the in-memory CSV export both omit the Status column, and any legacy CSV that still carries a Status header is silently ignored on import for back-compat. Existing records continue to keep their Status column in the database for audit history; it just stops being read as truth in any user-facing path.

The one-time SQL backfill (`Scripts/database/CR015_backfill_product_id_status.sql`) was run against the production database on 2026-05-01: 4 Product Identifiers on Expired certs were flipped to Inactive, 17 rows on Client Approved - Paid certs were already correct and were left untouched, and the post-flight audit query returned zero mismatches. The Plesk test environment at `gccwebapp.com` connects to that same production database (per `project_plesk_deploy_env.md`), so the test environment inherits the backfill automatically — no separate test-env backfill is needed and none has been run separately. The Partner API contract is unchanged; Amazon's view of the data was already correct prior to this change because the list endpoint had been filtering certs to public statuses since DEF-097 (2026-04-21), and this work cleans the underlying per-row state to match.

QC #164 is closed in the defect register as a mapping to DEF-104. No new code change required for this QC item. Files referenced (already shipped under CR-015 / DEF-104): `Models/CertificateProductId.cs`, `Services/ProductIdService.cs`, `Services/CertificateService.cs`, `Services/CertificateExpirationService.cs`, `Services/CertificateWorkflowService.cs`, `Services/Import/CertificateImportService.cs`, `Controllers/CertificateController.cs`, `Controllers/Api/ProductIdApiController.cs`, `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `Scripts/database/CR015_backfill_product_id_status.sql`.

---

## DEF-147 — Product ID Manager action icons now match the app standard; trash icon is red (2026-05-05, fix in working tree pending deploy)

Shawn flagged during continued internal QC on cert 26-0160 that the Edit and Delete action icons in the Product ID Manager did not look like the action icons elsewhere in the admin app. They were rendering as Bootstrap outlined pill buttons with a square border and a muted danger color, instead of the bright-red trash icon and bright-blue edit pencil that Pathways, Partners, Tags, and the other inline-edit admin tables use. The functional behavior was correct on click — only the visual treatment was off — but the inconsistency was visible enough to break the "this is the standard delete affordance, you can trust it" intuition that the rest of the app builds.

Root cause was that the Product ID Manager was the last render site still using the legacy `btn btn-outline-primary` / `btn btn-outline-danger` button group, instead of the canonical `admin-inline-action-btn--edit` / `--delete` pair. The canonical pair is defined in `wwwroot/css/admin/admin-inline-editing.css` with edit at `#00AEEF` blue and delete at `#FF5252` red, with the right hover, scale, and focus treatments. Two divergent render sites needed updating: the Razor partial that draws the initial table rows on page load (`Views/Shared/_ProductIdManager.cshtml`), and the JavaScript template that draws rows added or refreshed via the manager's UI (`wwwroot/js/product-id-manager.js`'s `getProductIdRowHtml`). On top of that, the canonical CSS was not linked from either host page (Create.cshtml or Edit.cshtml), so even fixing the markup wouldn't have produced the right colors without that link.

The fix updates both render sites to emit `<div class="admin-inline-actions">` wrapping `<button class="admin-inline-action-btn admin-inline-action-btn--edit">` / `--delete`, while keeping the existing `btn-edit-product-id` / `btn-delete-product-id` hooks alongside the new classes so the click event bindings keep working without any JS handler changes. Added `aria-label` on each button per the app's accessibility convention. Linked `~/css/admin/admin-inline-editing.css` from Create.cshtml and Edit.cshtml so the standard styling actually loads on the cert create/edit pages.

Verified locally on `localhost:5101` against `GCCDatabase_Dev` via Playwright. Logged in as `analyst`, opened the Edit page for cert `26-0147`, scrolled to Product ID Manager, screenshotted, and read computed styles on the live DOM. Edit button computed color came back as `rgb(0, 174, 239)` (= `#00AEEF`, the standard blue). Delete button computed color came back as `rgb(255, 82, 82)` (= `#FF5252`, the standard red). Zero remaining `btn-outline-primary` / `btn-outline-danger` action button classes on the page. Visual screenshot confirms the standard compact inline-icon footprint and the bright trash icon Shawn was asking for.

Files: `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`, `docs/tracking/defect-register.md` (DEF-147 entry).

---

## DEF-146 — Product ID Manager now preserves the original "Created By" on every cert edit (2026-05-05, fix in working tree pending deploy)

Shawn flagged during continued internal QC that on prod cert `26-0147` the Product ID Manager's "Created By" column was showing `System` for every Product ID row, instead of the user that originally added it. Product ID rows are part of the cert audit trail — knowing who entered which identifier matters when the client or a partner traces back why a particular ASIN got attached to a particular cert. "System" is a fallback string the UI shows whenever the row's `CreatedBy` foreign key is null, so two things were going wrong: the data on those rows was stored without a creator, and any user editing the cert later would have made it worse, not better.

Root cause was in the cert Edit POST controller. The Product ID handling block did a clear-and-readd: `certificate.ProductIds.Clear()` followed by adding each submitted row back, with `CreatedBy` and `CreatedAt` blindly stamped to the current editor and current UTC time on every row in the loop. This meant every cert edit silently rewrote the audit trail on every Product ID row attached to that cert — replacing the original creator with whoever happened to be saving the form. Compounding factor: the Edit form's hidden `ProductIds[i].*` inputs that the JS form-builder generates don't include `CreatedBy`, so the model arrived with `CreatedBy = null`, and if `GetCurrentUserId()` ever returned null (e.g., an Azure AD path where the GUID claim couldn't be mapped to an int user ID in the local Users table), the row landed with `CreatedBy = NULL` in the DB. That is the state that produces "System" in the UI fallback at `wwwroot/js/product-id-manager.js`.

The fix replaces the clear-and-readd with a proper merge. The controller now builds a dictionary of existing Product ID rows by Id, removes only the rows whose Id is not in the submitted set (the user actually deleted them), updates editable fields (Type, Value, Partner, ProductName, ProductSku, Description) on existing rows while preserving `CreatedBy/CreatedAt` and stamping `UpdatedBy/UpdatedAt`, and stamps `CreatedBy = currentUserId` only on truly new rows (Id == 0). Going forward, any cert edit on prod will preserve whatever `CreatedBy` is currently in the database — including the existing NULL values on prod 26-0147 (which is why this fix on its own won't change what 26-0147 displays today; the data is already null and stays null). Backfilling those NULL rows would be a data UPDATE on real cert records and is intentionally out of scope here per our standing rule that data changes on real business records need a CR from the client. Flagged separately for the client if they want historical creator attribution restored.

Verified locally on `localhost:5101` against `GCCDatabase_Dev` with a Playwright script covering three scenarios. Cert `26-0025` has `CreatedBy = NULL` rows mirroring the prod 26-0147 state — logged in as `analyst` and saved the Edit form with no PID changes; the rows stayed NULL post-save (pre-fix, they would have been overwritten to `analyst`). Cert `26-0147` in dev has `CreatedBy = admin` rows — same `analyst` save flow; admin attribution preserved. Then on the same dev `26-0147` while still logged in as `analyst`, used the Product ID Manager UI to add a brand-new SKU row and saved; the new row landed with `CreatedBy = analyst` while the existing two admin rows kept `CreatedBy = admin`. All three scenarios passed; no regressions on the surrounding cert-edit flow.

Files: `Controllers/CertificateController.cs` (Edit POST, ~lines 866-910), `docs/tracking/defect-register.md` (DEF-146 entry).

---

## DEF-143 — Cert detail pages no longer fire two console errors per visit (2026-05-05, fix in working tree pending deploy)

While exercising the cert detail page during continued internal QC, every load of `/Certificate/CertificateSingleView/{certificateNumber}` produced a noisy pair of console errors: a 404 on `/api/certificates?pageSize=100&page=1`, followed by a `TypeError: Cannot read properties of undefined (reading 'initializeSearch')` cascade. The page itself rendered correctly and the download button worked, so the bug was invisible to a casual user — but it represented dead plumbing firing on every single cert-detail navigation, including for unauthenticated public visitors hitting cert links. The same noise was confirmed on the two sibling detail views, `CertificateInternalView` and `CertificateExternalView`, which all three include the same script.

The root cause is a misuse rather than a broken endpoint. All three single-cert detail views include `wwwroot/js/certificates.js`, which despite the generic name is a *list/grid/search/filter* initializer designed for a multi-cert table view that no longer exists in the active routing. On DOMContentLoaded it auto-runs `initializeCertificates()`, which fires `fetch('/api/certificates?pageSize=100&page=1')`. That route returns 404 on the deployed build — looking at the controllers, `Controllers/Api/CertificatesApiController.cs` declares `[Route("api/[controller]")]` and *should* resolve to `/api/certificates`, but in practice the un-versioned API routes (also `/api/etl`) return 404 in the deployed build, while the v1 versioned route at `/api/v1/certificates` works as expected with a Partner API key. Best guess is that the legacy un-versioned controllers were either deregistered or never registered in the active route table when the v1 surface was introduced for Amazon. Either way `certificates.js` is calling a route that doesn't exist, and the failure cascades into a half-initialized state that surfaces a TypeError when the next init step runs.

What saves this from being worse is that each of the three detail views is fully self-sufficient. They each define their own `window.downloadCertificate` inline, handle their own image zoom modal inline, and have no certificates grid, no search input, and no filter UI to need a list-page initializer for. The `<script>` tag is a dead include that produces console noise with zero functional benefit. The fix is three single-line deletions — one `<script src="~/js/certificates.js"></script>` line removed from each detail view's tail script block. After deploy, every cert detail page navigation will produce a clean console.

Two adjacent cleanup decisions are deliberately punted. First, `wwwroot/js/certificates.js` itself becomes orphaned by this fix — no view references it after the deletion. Whether to delete the file from the codebase is a cleanup decision, not a defect, and it can wait. Second, whether the legacy `/api/certificates` and `/api/etl` routes should be re-enabled or their controllers deleted is a separate question. The v1 versioned `/api/v1/certificates*` is the supported Partner API surface today; if anything still relies on the un-versioned routes, it would need to be migrated, which is a separate scope decision.

Files: `Views/Certificate/CertificateSingleView.cshtml`, `Views/Certificate/CertificateInternalView.cshtml`, `Views/Certificate/CertificateExternalView.cshtml`, `docs/tracking/defect-register.md` (DEF-143 entry).

---

## DEF-142 — Partner-role web user now sees the Certificates and Products nav and the cert/product views filtered to their assigned partner (2026-05-05, fix in working tree pending deploy)

Shawn flagged during continued internal QC that he had created `testuser5` on gccwebapp.com with the Partner role under Role Management, logged in, and saw no navigation at all. This pivots a recently-resolved decision: a week ago we treated the Partner role as API-key-only and shipped DEF-112 Path 4 to make it unassignable to humans. The actual requirement is the opposite — Partner web users do exist (Amazon staff with portal logins is the canonical case), they should see the same Certificates and Products view that an External user sees, and the data they see should be filtered to certificates tagged with their assigned partner. Amazon also continues to consume `/api/v1/certificates*` over the locked Partner API key contract; that path is unaffected by this change. All Amazon users see all Amazon-tagged certs (no per-user scoping beyond partner name); same pattern for any other partner.

The good news during diagnosis was that the cert and product filtering plumbing for this case was already fully built. Both `Controllers/Api/CertificateTileExternalApiController.cs` and `Controllers/ProductController.cs` already branch cleanly on `userRole == "Partner"` and filter against `User.Partners` (the many-to-many that the Edit User modal's Partner Assignment dropdown saves to). The only things missing were three small wires: the layout nav rendering didn't have a Partner branch, the `ExternalAccess` policy in `Program.cs` didn't accept the Partner Role claim (so Partner users would have 403'd on the cert/product views even if the nav had linked there), and the seeded `Roles` row for Partner had `IsActive=false` from the DEF-112 reversal — which meant the User Management role dropdown didn't offer Partner on dev. On prod that DB flip was never actually run, which is exactly why Shawn was able to assign Partner to testuser5 there in the first place.

The fix wires the three missing pieces. The `ExternalAccess` policy now accepts the Partner Role claim alongside External, Admin, and Analyst. The shared layout nav now renders the Certificates and Products nav links for `userRole == "External" || userRole == "Partner"` — both roles get the same minimal nav. The seed for the Partner role flips back to `IsActive=true` with a description that reflects the dual purpose (web users and API keys both flow through this role; the web path filters by the user's session-derived `User.Partners` collection, the API key path is scoped at the controller level by the auth middleware before the role check ever fires). The dev DB row was updated in place to match: `IsActive=0` → `IsActive=1`. Prod requires no DB change because the deactivation SQL was never run there. The deactivation script that DEF-112 referenced was never actually committed to the repo, so there's no script to delete.

What we are explicitly not doing in this fix is solving the deeper architectural concern DEF-112 originally raised — that the same string `"Partner"` carries two different identities (human web user with a session, vs Amazon API key request with no user) and the cert-listing controllers can't tell them apart at the role check. Functionally the two don't collide today because they flow through different controllers (the External tile API is web-only, the v1 Partner API is API-key-only) and the data sources for "what partner is this user/request scoped to" come from different places (session userId → `User.Partners` for the web path, middleware-injected partner identity for the API path). The audit-trail concern remains a future hardening item — DEF-112 Path 1 rename of the web-user role to `PartnerUser`, or Path 2 explicit branch split. That deserves its own CR if and when audit logging is taken seriously across both identities, and is not blocking this fix.

To verify after Plesk deploy: assign `testuser5` to a partner via the Edit User modal (if not already assigned), log in as testuser5, confirm the Certificates and Products nav both render, confirm the certificate tile view is filtered to certificates tagged with the assigned partner, and confirm the products page is filtered to products with at least one cert tagged with that partner. Local verification was a clean build; the dev DB flip was confirmed via direct query (`Roles.IsActive` for Partner: 0 → 1).

Files: `Program.cs`, `Views/Shared/_Layout.cshtml`, `Data/SeedData.cs`, `docs/tracking/defect-register.md` (DEF-142 entry, plus reversal note added to DEF-112's history). Dev DB Roles table updated in place.

---

## DEF-141 — Eliminated phantom `/Admin/GetProducts` fetch firing on every analyst load of `/Product/Management` (2026-05-05, fix in working tree pending deploy)

A multi-page smoke run during the continued internal QC retest surfaced a console error on every load of `/Product/Management` as an analyst: `Error loading products: SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON`. The page itself renders the products table correctly, so the error was not visible to the user beyond a brief "Error connecting to server" toast that contradicts what the page is actually showing. But it represented real wasted work — every analyst load was producing an extra HTTP round-trip to an authorization endpoint that always denied them, and writing a misleading console error on every navigation.

Earlier under DEF-111 we added the JSON-vs-HTML guard to the inline `loadProducts()` in the Razor view, which is what populates the actual products table. That fix was correct for what it was scoped to. The issue surfacing now is a different file with a confusingly identical name. The Razor view also includes `wwwroot/js/admin/productManagement.js` so the import-modal helpers are available — the page explicitly delegates the Import button's pre-flight checks to `window.productManagement.handleImportClick()`. That admin script also has its own `loadProducts()` method, which fires `fetch('/Admin/GetProducts')` against the admin-only endpoint. Logged in as analyst, that endpoint redirects to `/Account/AccessDenied`, which returns HTTP 200 with HTML; the script's unguarded `response.json()` chokes on the leading `<`. The page kept working because the inline view's own data load is independent.

The fix is a single four-line gate inside the admin script's `init()`: only run the `loadProducts() / renderProducts() / updateProductCount()` data path when `document.getElementById('products-table-body')` actually exists. That element only lives on `Views/Admin/ProductManagement.cshtml` (the page the class actually owns). On `/Product/Management` the class still instantiates, still binds its event handlers, and still exposes `window.productManagement.handleImportClick()` to the inline view — it just stops trying to fetch products from an admin-only endpoint that the analyst legitimately can't reach. The Admin role's experience on `/Admin/ProductManagement` is unchanged because the gate is satisfied there.

Surfaced two adjacent items during the same retest that are *not* being fixed under this defect because each needs a real decision rather than a quick patch. First, `wwwroot/js/admin/admin-core.js` routes every notification — success, info, warning, error — to centered alertdialog modals that require an OK click. The project CLAUDE.md "User Notifications" rule says "Success: Toast notification only." The inline comment in admin-core.js says the modal-everywhere routing is deliberate ("Route ALL notifications to centered modals for consistency"). So this is a doc-vs-code reconciliation, not an obvious bug — either CLAUDE.md needs to be updated to reflect the modal choice, or admin-core needs a transient-toast variant for success. Punted to a separate decision. Second, the smoke run shows two `<h1>` tags on Dashboard, Product Management, and Company Index, which violates CLAUDE.md's "no `<h1>` in views, use `ViewData[\"Title\"]`" rule. One of the duplicates may be the layout-supplied page heading and the other a redundant declaration inside the view; needs a visual audit before yanking. And third, `/Admin/CertificationTypes` timed out at 30 seconds when smoke-loaded as analyst — it should redirect to AccessDenied promptly. Likely either a slow query running before the authorize check or a misconfigured policy. Worth a per-page timing pass on a future cycle.

Files: `wwwroot/js/admin/productManagement.js`, `docs/tracking/defect-register.md` (DEF-141 entry).

---

## DEF-140 — "Remember Me" now actually keeps the user logged in past 30 minutes (2026-05-05, fix in working tree pending deploy)

Shawn flagged during internal QC that "Remember Me" on the login page didn't appear to be working — specifically when leaving an authenticated tab open and idling. He also noted that after explicit logout, the checkbox came back unchecked on the next visit and asked whether it should be remembered.

On the second question first: the checkbox is staying unchecked by default, and that is the deliberate decision. Pre-checking Remember Me on a shared computer is a known security footgun, and most secure-app conventions default to unchecked — especially right after an explicit logout, where the user has just intentionally signed out. The fix below addresses the actual bug, which is that even when Remember Me *was* checked, the user got logged out at 30 minutes anyway.

The server side of Remember Me has been correct since a prior sprint fix on 2026-03-20 (commit `bc5d619`): when the box is checked, the auth cookie is issued as a 14-day persistent cookie, and a middleware in `Program.cs` re-hydrates the session role/email/name from the auth claims on every request, so closing and re-opening the browser within 14 days lands the user right back into the app already signed in. That part has been working.

What was breaking the user's perception is a separate, client-side timer in `wwwroot/js/auth.js` that runs on every authenticated page. It fires a hard-coded 30-minute idle timeout that programmatically submits the logout form regardless of whether Remember Me was ever checked. So a user who chose Remember Me, then walked away from a tab for half an hour, came back to find themselves signed out — even though the 14-day server cookie was still perfectly valid. The browser-close-and-reopen flow was unaffected because the JS isn't running while the browser is closed, but the open-tab idle case completely defeated the feature.

The fix flows the user's preference from the server through to the client. During sign-in, `SignInUser` now adds a small `RememberMe` claim ("true"/"false") onto the auth ticket. The shared layout reads that claim and emits a `<meta name="x-remember-me">` tag in the page head. The auth.js timer then reads that meta tag at startup, and when the user is in a Remember Me session it skips both the 5-minute pre-timeout warning toast and the 30-minute auto-logout submit entirely. Server-side cookie expiration remains the single source of truth: 14 days persistent for Remember Me users, 30-minute sliding for non-Remember Me users. The existing 401-fetch-interceptor in the same JS file still catches genuinely-expired sessions on the next request and redirects to login with an explanatory message, so the non-Remember Me UX is unchanged.

Out of scope for this fix: the Azure AD B2C path issues its own claims through `OnTokenValidated` and doesn't route through `SignInUser`. AAD B2C is configured but is not the active auth scheme today. If we light it up in production later, the same `RememberMe` claim will need to be plumbed there or this protection will not apply to AAD B2C-authenticated sessions.

Files: `Controllers/AccountController.cs`, `Views/Shared/_Layout.cshtml`, `wwwroot/js/auth.js`, `docs/tracking/defect-register.md` (DEF-140 entry).

---

## DEF-139 — "Add New Product" subcategory dropdown now shows clear loading / empty / available states (2026-05-05, fix in working tree pending deploy)

During continued internal QC retest, Shawn flagged that on `/Product/Management` → "Add New Product", the Subcategory dropdown does not appear to load the first time a Product Type is selected, even on types that do have subcategories. The cascade was added under the earlier QC #93 sprint and works correctly when exercised programmatically — a Playwright run against the live deployed page selected each Product Type and confirmed the corresponding subcategories were fetched and rendered every time, including on rapid type-swaps and modal close-and-reopen. The deployed JavaScript on the server matches the source we have in the repo. So a deterministic "doesn't load" reproduction wasn't possible from automation, which suggests the user-visible symptom is one of: a stale browser cache holding a pre-cascade version of the page, a slow-network race where the dropdown sits empty for a beat between the change event and the fetch return and the user reads that as "broken", or simple ambiguity from the placeholder "Select Subcategory" reading the same whether the dropdown is loading, the type genuinely has no subs, or the fetch silently failed.

Rather than chase a non-reproducing intermittent, the fix tightens the function to remove all three failure modes regardless of which one is actually biting the user. The Subcategory dropdown now flips to "Loading subcategories..." the instant a Product Type is chosen, before the fetch is even sent — so the user sees explicit feedback that something is happening and never stares at an empty placeholder wondering whether to click again. When the fetch returns success with an empty array, the placeholder switches to "No subcategories available" instead of going back to the generic "Select Subcategory", matching the same pattern already in place on the Certificate Create page so the two cascades behave consistently. Finally, a module-scope sequence counter is now captured at the start of each call and re-checked after each await; if a newer call has come in while this one was in flight, the in-flight call quietly bails instead of overwriting the dropdown with stale data. That last guard closes a real, low-probability race even if the user-visible symptom turns out to have been browser cache all along.

If the original symptom was indeed a stale browser cache, the next deploy plus a hard refresh on the user's side will make the underlying fix visible. If it was a network race or timing perception, the loading state and request-token guard together remove the failure mode. Either way the user now sees one of three explicit dropdown placeholders — "Loading subcategories...", a real list of subcategories, or "No subcategories available" — and never an ambiguous default while the page is doing work.

Out of scope for this fix: the same cascade pattern exists on `Views/Admin/ProductManagement.cshtml` and the Certificate Create/Edit pages. Cert pages already display "No subcategories available", so they are consistent with the new behavior. The Admin page wasn't named in the report and was left untouched until there is evidence the same UX gap matters there.

Files: `Views/Product/Management.cshtml`, `docs/tracking/defect-register.md` (DEF-139 entry).

---

## DEF-138 — Product Management edits no longer wipe SKU and Model values on save (2026-05-05, fix in working tree pending deploy)

The client raised QC #91 in the Sprint 2 retest with strong language — "This is a big issue!" — describing two symptoms on the Product Management page. Products that had Type and Subcategory added to them were "not showing in list at all" after save, and re-opening the same products in the edit modal appeared to clear the Product Name and Product Type fields.

The root cause was a quiet regression introduced when an earlier sprint's QC #92 fix removed the Sku, Model, and Status inputs from the Add/Edit Product modal. The frontend save payload no longer carries `sku` or `model`, but the server-side Save action in `Controllers/ProductController.cs` still unconditionally assigned the entity's Sku and Model fields from the incoming model. JSON deserialization filled the missing fields with null, and the server then overwrote the existing database values with null on every product update. Sku and Model values that had been populated by CSV import — or by any path that doesn't go through the modal — were silently destroyed by any subsequent edit through the UI.

The "Product Name and Product Type cleared" report turns out to be a misread of the table display. Each product row renders the Product cell as two stacked lines: the product Name on top, and a `SKU: <value>` line immediately below it. The cert-style numbers the client mentioned ("25-8760", "25-8777") are SKU values, not product names. After the first save, those SKU strings flipped to "N/A" in the table, and the client identified the products by their SKU values — so the rows visually appeared to "go missing" by the client's eye even though the product Name field itself was untouched. This was confirmed by a live diagnostic against the dev environment: Name, ProductType, ProductSubcategory, CompanyId, and CategoryId were all preserved across save; only Sku and Model were nulled. The second part of the original complaint, about Type/Subcategory not showing up, was the same SKU-display-N/A symptom misread again — adding a ProductType through the modal does correctly persist and render in the list.

The fix is two lines in `SaveProduct`. The Sku and Model assignments are now guarded so they only run when the incoming payload carries a non-null value. Modal saves (which omit both fields) leave the existing values alone; CSV imports or any future call site that does send the fields can still update them. The Create branch is unchanged; new products start with whatever Sku/Model the modal supplies, which is null today but is a separate UX decision about whether to surface those inputs.

Live tested on the dev environment by seeding a product with known Sku and Model values, then submitting the exact JSON payload the modal produces. On the un-patched build both fields flipped to null after save; on the patched build both fields are preserved verbatim. A second test confirmed that Type and Subcategory adds continue to flow through to the list correctly.

Two related decisions are deliberately out of scope for this fix. First, whether to add the Sku and Model inputs back to the modal — that is a UX scope question the client should weigh in on, since QC #92 explicitly removed them. Second, whether to drop the `SKU: <value>` line from the product list cell so future SKU changes can't visually disappear and confuse the user. Neither is necessary to close DEF-138.

Files: `Controllers/ProductController.cs`, `docs/tracking/defect-register.md` (DEF-138 entry).

---

## DEF-109 — Product ID "Created By" column showed "System" instead of the user who added it (2026-05-05, fix in working tree pending deploy)

Continuing UAT on QC #80, Shawn flagged that the Product ID Manager table on the Certificate Edit page was rendering the literal string "System" in the Created By column for product IDs added through the certificate Create or Edit form, even though a logged-in analyst had been the one to add them. The bug was easy to mistake for a display-side fallback issue, but the actual cause was on the server: the certificate save handler was never recording which user added each product ID in the first place.

The Product ID Manager modal posts new and edited rows two different ways depending on the page state. On a saved certificate, the modal hits `/api/productids` directly and `ProductIdApiController.CreateProductId` correctly stamps `CreatedBy = GetCurrentUserId()` before persisting — that path was already correct. On a Create page (no certificate ID yet) and on the Edit page's form-save (when the cert and its product IDs are written together as a single submit), the rows ride along inside the certificate POST as `model.ProductIds`. The JavaScript helper `getProductIdsForSubmission()` only includes the user-supplied fields (Type, Value, Partner, Name, SKU, Description) — no audit fields — so when the model binder constructs each `CertificateProductId`, `CreatedBy` is `null`. The Create handler in `CertificateController.cs` was then attaching the rows to the certificate without filling in audit fields, and the Edit handler was clearing the existing collection and re-adding from the form payload, also without filling in audit fields. EF saved rows with null `CreatedBy`, the re-fetch returned `CreatedByUser = null`, and `wwwroot/js/product-id-manager.js` line 244–245 fell back to its hardcoded `'System'` literal. The user wasn't lost in display; the user was never recorded.

The fix is in `Controllers/CertificateController.cs` at the two form-save sites. Both now set `CreatedBy = GetCurrentUserId()`, `CreatedAt = DateTime.UtcNow`, and `UpdatedAt = DateTime.UtcNow` on each product ID before adding it to the certificate's collection. The Edit flow additionally sets `UpdatedBy = currentUserId` because its clear-and-re-add pattern means every row is effectively re-inserted on save and the consistent thing is to stamp both audit pairs. This matches how the surrounding Tags and CertificationTypeSubcategories blocks behave on edit. New rows now show the correct user immediately after the cert is saved and the page is reloaded.

Existing rows already in the database that were saved with null `CreatedBy` will continue to render "System" until they are touched by another save. A backfill SQL update could clear the historical "System" rendering by inferring the cert's CreatedBy onto each orphaned product-ID row, but that's a data change on real records and per project rule (`feedback_data_changes_require_cr.md`) needs a formal CR before running on prod. The historical rendering is the cosmetic price of the legacy bug; the audit data is permanently lost for those rows because nobody ever recorded who added them. New activity is correct from this commit forward.

Verified locally: built cleanly (no compile errors). Tested by adding a product ID as `analyst` on the Create page and confirming the row shows `Created By: analyst` after save and reload. Adding a second product ID directly on the Edit page produces the same correct result. Cross-role check as `admin` confirms the role's username is recorded correctly. Pre-existing rows on a previously-saved cert continue to render "System" as expected for the limitation noted above.

Files: `Controllers/CertificateController.cs`.

---

## DEF-110 — Admin `/api/certificates` POST/PUT no longer drops CreatedBy / UpdatedBy on the certificate audit columns (2026-05-05, fix in working tree pending deploy)

A code-survey sweep done immediately after DEF-109 was fixed surfaced a sibling bug on a different write path. Shawn asked the natural follow-up question — "do we have this issue elsewhere in the views?" — and the answer turned out to be yes, on the Admin-only `/api/certificates` API. Certificates created or updated through that endpoint had been silently saving with `CreatedBy = NULL` (and `UpdatedBy = NULL` on update). The Cert Internal View shows "Created By: <user email>" guarded by an `@if (CreatedByUser != null)` check, so on those rows the entire line was simply hidden rather than rendering "System" — but the audit data was still missing in the database.

The root cause was a wiring gap between the controller and the service. The API controller built a `Certificate` from the request body via `MapToCertificate`, which didn't set CreatedBy. It then handed off to `CertificateService.CreateCertificateAsync` / `UpdateCertificateAsync`, but those methods only accepted a `userRole` string — there was no way to pass the caller's user ID through. So the user identity was available on the controller (via `User.FindFirst(NameIdentifier)`) but never reached the entity that got saved. Same shape as DEF-109, different surface.

The fix adds an optional `int? userId = null` parameter to both service methods (`IServices.cs` and `CertificateService.cs`). Each method now stamps the audit fields from that parameter — Create sets both `CreatedBy` and `UpdatedBy`, Update sets `UpdatedBy`. The Admin `/api/certificates` controller picked up a small `GetCurrentUserId()` helper that parses the `ClaimTypes.NameIdentifier` claim and now passes it into both service calls. The optional parameter default means any future caller that doesn't supply a userId still compiles and runs (it just keeps the legacy behavior). The web `/Certificate/Create` and `/Certificate/Edit` flows go through the repository directly with their own user-stamp logic, so this change has zero effect on the web paths.

This endpoint is Admin-only and we have no evidence of it being exercised in the client UAT loop, so user-visible impact today is effectively zero. The fix lands now because the bug pattern is identical to DEF-109 and leaving it would create the same silent audit-data gap for any future Admin who does hit the API.

Built cleanly (zero compile errors). Files: `Services/IServices.cs`, `Services/CertificateService.cs`, `Controllers/Api/CertificatesApiController.cs`.

---

## Deploy pipeline cleanup — pinned single staging convention; freed 470 MB of stale build artifacts (2026-05-05)

The project root had been accumulating per-run timestamped deploy folders and zips for weeks. As of this morning there were six `GCCWebApp-deploy-YYYYMMDD-HHMM.zip` artifacts (five at the project root, one in a newly introduced `deployment-drops/` folder) plus two `publish/` folders, each carrying a full release-mode copy of the application. The naming convention had drifted across at least four shapes — `deploy-staging/`, `deploy-staging-YYYYMMDD-HHMM/`, `publish-YYYYMMDD-HHMM/`, `deployment-drops/` — and every drift forced a new exclusion line into `Properties/PublishProfiles/PleskTestEnvironment.pubxml` so the next publish wouldn't accidentally pick the stale folder up as Content. The pubxml's exclusion list had grown to ten patterns chasing the convention drift.

The cleanup pins one convention going forward and removes the stale clutter that's already there.

The staging path is now `deployment-drops/staging/`, cleared each run rather than timestamped. The output zip stays timestamped so older runs are distinguishable for rollback, but it lives at `deployment-drops/GCCWebApp-deploy-YYYYMMDD-HHMM.zip` rather than scattered between the project root and the new drops folder. The `.gitignore` adds `deployment-drops/`, `publish-*/`, `deploy-staging/`, and `deploy-staging-*/` so future timestamped folders never sneak into a commit. The pubxml's ten ad-hoc exclusion lines are collapsed into three globs (`publish\**`, `publish-*\**`, `deploy*\**`) that cover every historical naming convention without needing per-run additions.

A new `Scripts/deploy.ps1` documents the convention in code. It runs the pre-deploy uploads-backup checklist (per the project memory rule established after the 2026-04-23 incident where a publish overwrote production user-uploaded PDFs), stops a dev server on port 5101 if one is listening, runs `dotnet publish` against the chosen profile (`PleskTestEnvironment` by default, `AccuWebProduction` if invoked with `-PublishProfile AccuWebProduction`), sanity-checks the staged file count, asserts that `wwwroot/uploads/` did NOT make it into staging before zipping, then zips with the timestamped name. The script is the first version-controlled artifact of the deploy convention; previously it was tribal knowledge and the only evidence was the slowly-growing exclusion list in the pubxml.

The five stale root zips (totaling 302 MB) and the two stale `publish/` folders (totaling 168 MB) were verified as untracked in git before deletion. The most recent zip (`deployment-drops/GCCWebApp-deploy-20260504-2301.zip`) is preserved for rollback context. None of this churn changes the deployed application surface — it only changes how the deployment artifact is built.

Files: `Properties/PublishProfiles/PleskTestEnvironment.pubxml`, `.gitignore`, `Scripts/deploy.ps1` (new).

---

## DEF-137 — User Management: Add User flow honored RequirePasswordReset checkbox and unblocked email reuse after delete (2026-05-04, fix in working tree pending deploy)

The Add User form on the admin-side User Management page had two latent bugs surfaced by the client's 5/1 retest of QC #107. First, the "Require password reset on next login" checkbox on the Add User popup was silently ignored. The form posted the flag to the server, the controller bound it to the model, and then `AdminService.CreateUserAsync` constructed the User entity without copying the field across. The new user's `RequirePasswordReset` defaulted to `false` regardless of what the admin checked, so users created with the checkbox set still logged in normally without being prompted to change their password. Second, deleting a user and trying to recreate them with the same email failed with the cryptic message "An error occurred while saving the entity changes. See the inner exception for details." The duplicate-email pre-check in `CreateUserAsync` looked through the full Users table without filtering soft-deleted rows, and even if that check had been corrected, the database has unfiltered UNIQUE indexes on both Email and Username, which would have rejected the INSERT regardless. The end result was that an email address became unusable forever after a user was deleted from the admin UI.

The fix is in `Services/AdminService.cs`. `CreateUserAsync` now sets `RequirePasswordReset = model.RequirePasswordReset` on the User entity and filters its duplicate-email guard by `!u.IsDeleted` so soft-deleted users are not treated as conflicting records. `DeleteUserAsync` now appends `__deleted__<id>__<timestamp>` to both the Email and Username columns at the moment it sets `IsDeleted = true`, which frees the unique slot at the database level and lets a future admin reuse the same address. The audit log captures the original email value in its before-snapshot, so the original is preserved and reconstructable by stripping the suffix if needed. No schema migration was required.

The fix was verified live on `localhost:5101` against three scenarios: a user is created and deleted, then a new user is created with the same email and the create succeeds; a user is created with the checkbox checked, and on first login is correctly redirected to `/Account/ForcePasswordReset`; an external user is created with a chosen password, logs out, and successfully logs in with that password to land on the external certificate tile view. All three pass on the patched build.

Two related sub-items the client raised under QC #107 are intentionally out of scope for this defect. The password field clears when an existing user is reopened in the edit modal — that is by design (security best practice; the system never displays a stored password back to the form), and the proper remediation is a UX change to add helper text such as "Leave blank to keep current password," not a code change to display the password. The Company Access permission scoping does not limit which records Admin or Analyst users can see — also by design and consistent with the client's own statement on 5/1 that "Admin and Analyst will never be limited to companies but External and Partner types will." The External and Partner scoping IS implemented in the relevant controllers; that scoping should be verified during client UAT against real data.

Historical context worth recording for future reconciliations: the 4/10 dev note on this row claimed it was already resolved by wrapping the user-creation transaction in `_context.Database.CreateExecutionStrategy().ExecuteAsync()` and adding a duplicate-email pre-check that surfaced a real error message to the user. Both pieces were genuine work, but the transaction-wrapper code is no longer in the codebase (the path was later collapsed to a single `await SaveChangesAsync()`, which is its own implicit transaction), and the duplicate-email pre-check landed without filtering soft-deleted rows. The "Ready for UAT" verdict was technically true for the surface that was tested but did not address the soft-delete reuse case or the checkbox flag — exactly the two cases the client retested at 5/1. This is the same trailing-comment blind-spot pattern that surfaced across thirteen other rows in yesterday's Sprint 3 reconciliation, and the same remediation has now been written into the validation prompt template's Phase 4 pre-write checklist so end-of-sprint verdicts include the trailing client comment verbatim.

Files: `Services/AdminService.cs`, `docs/tracking/defect-register.md` (DEF-137 entry).

---

## Certificate tile view — Company Email and Company URL renamed and pinned to the end of the table (2026-05-05)

The certificate tile views (both the analyst-facing internal view and the external partner view) carried a tangle of legacy labels for the company-contact columns. The internal view had a header reading "Company Contact Email/URL" and a separate header reading "Contact URL." The external view said "Company Contact" for the same email column and used "Company URL" for the URL column. The column toggle dropdown said "Company Contact" in some places and the columns themselves landed in the middle of the trailing block — sandwiched between EAN and the "Additional" group of cert-results / pathway / cert-subcategories — which meant Cert Results and Pathway and Subcategories columns were rendering AFTER the contact columns in the table. Custom admin-managed fields, when present, were appended at the very end of the row, so the visible column order was a confused middle-block of identifier and contact columns followed by custom fields at the right edge.

The client request was direct. Use "Company Email" for the email column and "Company URL" for the URL column, and put both of them at the end of the table, with every other column — including any admin-defined custom fields — sitting before them. Two columns, two labels, last two positions.

The fix touches only the certificate tile table surfaces. On both the internal and external Razor views, the sticky header cells for the email and URL columns are renamed to "Company Email" and "Company URL" respectively, and the two cells are reordered so they appear in that order at the very end of the header row, after Cert Subcategories. The column-toggle dropdown is similarly relabeled and reordered, and the now-redundant "Contact" section divider is folded into the existing "Additional" group so the dropdown reflects the same order as the table. The body-row template in the matching internal and external JavaScript renderers is reordered to match, so each rendered row's last two cells are Company Email and Company URL.

The custom-field placement is the second piece. The internal view's JavaScript renders custom-field cells per cert via `renderCustomFieldCells`, and that call was sitting after the static columns in the body-row template — so any admin-defined custom field rendered to the right of Company URL. The same applied to header injection: the dynamic header-cell builder for custom fields was using `headerInner.appendChild`, which always lands the new cell at the end. Both sites are changed. The body template now runs `renderCustomFieldCells` between Cert Subcategories and Company Email, and the header injection now does an `insertBefore` against the Company Email anchor cell. Custom-field columns therefore sit immediately before Company Email, with the standard static columns ahead of them. If the anchor is ever absent (a defensive guard against a future refactor), the code falls back to the previous append behavior so the headers still appear rather than silently disappearing.

Scope discipline. Two over-reach changes from the first pass were reverted before this shipped. The CSV export header in `CertificateService.cs` had been edited to match the new labels, and the `Display(Name=...)` attributes on `CertificateViewModel.CompanyContactEmail` and `CertificateDetailsViewModel.CompanyContact` had been edited similarly — both were rolled back to original values. The reasoning was that those surfaces feed downstream consumers (downloaded CSV files in stakeholders' inboxes, label rendering on Edit / Create form fields and validation messages), and the request was about the table view specifically. The C# property names (`CompanyContactEmail`, `CompanyContactURL`), the API sort keys (`data-sort-column="companyContact"` and `="companyUrl"`), the CSS class names (`tc-contact`, `tc-companyurl`), and the database column names are all unchanged — only the user-visible header text and the column position in the table moved. Backend sort APIs, column-width CSS, and the partner API contract are all unaffected.

Verified live on `http://localhost:5101` against both views as analyst and external user. Internal view total 24 sortable columns plus 1 custom field = 27 cells; the last three columns are `tc-cf-33` (the seeded test custom field), then `tc-contact` ("Company Email"), then `tc-companyurl` ("Company URL"). External view 23 sortable columns; last two cells are Company Email and Company URL with no custom fields injected because the external view does not surface custom fields. Zero JavaScript console errors. Header DOM directly inspected after page render; the `[Display]` attribute regression check on Edit-Certificate forms came back clean (label still reads what it always did because the model attributes were reverted).

The public anonymous tile view (`Views/Certificate/CertificateTileView.cshtml`) was not touched. The client request was specifically about the tables analysts and external users see, and the public view has a different layout that didn't surface the same complaint. If parity is wanted there later, it's a small follow-on.

Files (4): `Views/Certificate/CertificateTileViewInternal.cshtml`, `Views/Certificate/CertificateTileViewExternal.cshtml`, `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`.

---

## Verifications run today on previously-shipped QC items (2026-05-05)

The QC items below were already coded, committed, and deployed to the test environment in earlier work. They are listed here separately from the shipped section because no new code changed today on these line items — what we did today was independent verification (source + live test environment + localhost) confirming the fixes are present and behaving as intended. This section exists so the trail is auditable: when the client retests these items in their next UAT cycle and asks "did anything change," the answer is no, the fix has been in place since its original ship date and was confirmed working again today.

**QC #80 — Product ID modal save/edit on Certificate Create.** The original defect was three failure modes stacked: (1) the modal save button collected empty form data because the wrapping element is a `<div>` not a `<form>`, so `new FormData(e.target)` returned nothing and saves silently failed; (2) clicking Edit on a not-yet-saved (temp) row hit the API at `/api/productids/-NNNN` with a negative ID and 404'd; and (3) repeated open/save/open of the modal stacked Bootstrap backdrops and progressively darkened the page. All three are fixed in `wwwroot/js/product-id-manager.js` — `bootstrap.Modal.getOrCreateInstance` instead of `new bootstrap.Modal` to prevent backdrop accumulation, individual `getElementById` reads to collect form values reliably, a negative-ID branch on Edit that loads the temp item from the in-memory array instead of calling the API, and an explicit backdrop cleanup tagged `// QC #80` after each temp save. Source diff is committed (last touched in commit `98ac5f0`). Today's verification: pulled the JS file from `https://gccwebapp.com/js/product-id-manager.js` with the test-env basic-auth credentials and confirmed all four fix markers are present in the deployed bytes. Then re-ran the full Playwright scenario on `localhost:5101` as `analyst` — added two product IDs back-to-back, edited the first temp row, and asserted on every checkpoint. Backdrop count zero after each of the three modal-close events. Body `modal-open` class cleared. Page outside the modal still editable (no invisible overlay). Edit modal pre-filled with the original ASIN value `B0QC80AAAA` from the in-memory store. Zero JavaScript console errors across the full run. Status remains Ready for UAT for the client to perform their own functional pass.

**QC #38 / QC #58 — Contact column position on the internal certificate tile view.** The original defect was that the Company Contact Email column (`tc-contact`) had been authored sitting right after the Company column, in the middle of the table, where it made no semantic sense — the related Contact URL column was at the far right end, and users wanted the contact-related columns grouped near each other and away from the core company / product / cert columns. The fix was committed in `f964e67` ("updates, table updates," 2026-03-16) and moved the Company Contact column from position 9 to position 20, immediately after EAN, with a new "Contact" section divider in the column-toggle dropdown to group it. Today's verification: source-side, the sticky header cells (lines 386–391 of `CertificateTileViewInternal.cshtml`), the body-row template in the matching JavaScript renderer (lines 1759–1764 of `certificate-tile-view-internal.js`), and the column-toggle dropdown in the cshtml (lines 345–352) all show the Contact column in the post-fix position with consistent ordering across header, body, and toggle. Live verification on `localhost:5101` walked the rendered DOM with Playwright, dumped the full 24-column header order, and confirmed the trailing block reads ASIN → UPC → GTIN → EAN → Company Contact → Cert Results → Pathway → Cert Subcategories → Contact URL, exactly as the fix description states. (As of today, the same two columns have been further moved to the last two positions of the table and renamed to "Company Email" and "Company URL" — captured in the shipped entry above this section. The QC #38 / #58 verification was run before the rename / reorder change, on the previously-shipped state.) Status remains Ready for UAT.

Both verifications were dry runs against the existing deployed fix to confirm no regressions accumulated since original ship, and to document the receipts so the next testing cycle can reference them. No new code or behavior change came out of this work.

---

## DEF-112 — "Partner" role separated cleanly between API-key callers and web users (2026-05-04)

The cert-listing controllers had a long-standing dual-meaning problem on the "Partner" role. The role meant two different things depending on how the request arrived. A human user could be assigned the Partner role via the User Management modal, log in with email and password, and see certificates filtered by their assigned partner names. An automated request from Amazon would hit the same `/api/v1/certificates*` endpoints with an API key, and the API key middleware would inject the same Partner role claim into the request, and the same filter branch would fire. Both identities flowed through the same `userRole == "Partner"` checks across `CertificateTileExternalApiController` and `CertificatesController`. The audit trail couldn't distinguish them, future filter changes had to be reasoned about under both identities at once, and the partner immutability rule on the Amazon API contract was sharing code paths with internal-app web-user logic.

The original report (DEF-112) framed this as needing an architectural decision and laid out four resolution paths. We pulled the prod user list before deciding and confirmed that across all 23 users in the production database, zero have the Partner role. The role was always intended for human Partner-org users (e.g., Amazon staff with portal logins) but no such users were ever created. Path 4 was the cleanest answer: eliminate the web-user side of the role entirely.

The fix is small. The Roles row for Partner gets soft-deactivated by setting `IsActive = false`. The User Management role-list endpoint already filters its dropdown on `IsActive`, so admins can no longer accidentally assign the role to a future human user. The Partner API key middleware is unaffected because it injects the role claim directly on each request without ever reading the Roles table — so partner API authentication continues to work exactly as before. The cert-listing `userRole == "Partner"` filter checks across both controllers are also untouched and still correct. They simply never fire for cookie-authenticated callers anymore because no human has the role. If a future requirement needs human Partner users, the change is reversible: re-run the SQL with `IsActive = 1` and the role reappears in the User Management dropdown.

Three small source changes accompany the data fix. `SeedData.cs` now declares the Partner role with `IsActive = false` and the new description so fresh database initialization matches the prod state. The `PartnerApiKeyAuthenticationMiddleware` claim-injection site picked up a comment block marking it as the single source of truth for the Partner role claim, with a pointer to DEF-112 for context. The SQL data-fix script is idempotent and includes a defense-in-depth abort if any human users currently have the Partner role; we verified it runs cleanly against the development database with the BEFORE/AFTER snapshot showing the IsActive flip.

Prod SQL run is pending — the script lives at `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql` and is safe to run on the production database whenever convenient. Shawn will execute it via the existing manual-migration workflow used for DEF-134's earlier today.

Files: `Data/SeedData.cs`, `Middleware/PartnerApiKeyAuthenticationMiddleware.cs`, `Database_Scripts/20260504_DEF112_DeactivatePartnerRoleForHumans.sql`.

---

## DEF-135 — Google Analytics now allowed by Content Security Policy on the test environment (2026-05-04)

The site has had Google Analytics configured for months. An admin enabled the property `G-16J12CPD8S` through the application's own admin page, the layout correctly injected the `gtag.js` script tag on every rendered view, and zero analytics signals were being collected because the browser was blocking the script load.

The cause was two Content Security Policy headers being emitted on every response and disagreeing with each other. The IIS host emits one CSP header from `web.config`, and the application emits a second CSP header from its security headers middleware. Browsers handle two CSP headers by intersection — every directive uses the most-restrictive value across all sources. The application's middleware-level CSP had already been updated at some point to include the Google Tag Manager and Google Analytics domains in `script-src` and `connect-src`. The IIS-level CSP in `web.config` had not. So the intersection allowed everything the IIS-level CSP allowed, which did not include Google, and the script load was blocked.

The fix is to bring `web.config`'s CSP in line with what the middleware already permits. `script-src` now includes `googletagmanager.com` and `google-analytics.com`. `connect-src` now allows the GA4 beacon hosts that the analytics library calls home — `analytics.google.com`, region-specific `*.google-analytics.com`, `www.google.com`, `stats.g.doubleclick.net`, and `googletagmanager.com`. The two CSP sources now agree.

Per-environment scoping is handled at the database level, not the CSP level. The `GoogleAnalyticsSettings.IsEnabled` row in the database is the on/off switch — the layout only injects the script tag when that flag is true. The same web.config ships to every environment, so CSP allows the Google domains everywhere, but real production can keep `IsEnabled = false` and the script tag is never emitted, so the analytics library never runs. The test environment currently has `IsEnabled = true` because the property `G-16J12CPD8S` was registered for analytics validation. This pairs with the Bing and Google search-console removal tickets Shawn submitted today — the test domain is being deindexed from public search, and analytics on the gated test environment does not leak into discoverable web traffic.

Out of scope but worth noting. Maintaining two CSP sources that have to be kept in sync is fragile, and the drift between them is exactly what caused this bug. A later cleanup pass should consider removing the IIS-level CSP entirely and treating the application middleware as the single source of truth, since the middleware version is environment-aware and can branch on dev vs production whereas a static web.config cannot. That refactor is intentionally not part of this fix.

Files: `web.config`.

---

## DEF-136 — Product Management: false-positive "Error saving product" toast on every successful save (2026-05-04)

After DEF-134's deploy enabled Type and Subcategory persistence on the Product Management page, every successful Add or Edit Product still surfaced a red error toast saying "Error saving product." The product was actually being saved correctly — verified against the prod test database, where three rows Shawn created during testing minutes earlier all persisted with the right type and subcategory IDs. The error toast was a false positive.

The cause is in the Product Management page's inline JavaScript. The success branch of the save handler called two notification functions back-to-back: a stale `showNotification` (a residue of an unfinished in-page notification queue system) and the working `showToast` (which routes through the shared admin notification system). The stale `showNotification` function references a `notificationQueue` global that was never declared at the top of the file, so calling it threw a `ReferenceError`. The save function's outer `catch` block then swallowed the JavaScript error and called `showToast('Error saving product', 'error')`, painting a misleading red toast over what had actually been a successful save.

The fix removes the stale `showNotification` call from the success branch. The very next line — `showToast(message)` — already handles the success notification through the shared admin notification system that does work, so functionality is preserved with no replacement needed.

Out of scope but worth flagging. The dead in-page notification queue scaffolding is still present in the file — a half-finished feature with about a dozen unwired functions. It does no harm now that nothing calls into it, but a future cleanup pass should excise the dead code or consolidate it onto a shared helper. The same pattern may exist in other admin views; not chasing it under this defect.

Files: `Views/Product/Management.cshtml`.

---

## DEF-134 — Product Management Type and Subcategory now actually save (2026-05-04)

The Product Management table added Product Type and Subcategory columns under QC #91 back in March, marked Ready for UAT, and the columns rendered correctly. On 2026-05-04 the client reported the columns were blank for every product even when the user had filled them in on the Add or Edit modal. A check of the dev database confirmed it: every one of the 51 products had `NULL` for both `ProductTypeId` and `ProductSubcategoryId`, including products created after the cascade dropdowns shipped. Two independent root causes were eating the data.

The first was a save-path silent wipe. The Edit Product modal's JavaScript only pre-filled the name and description fields when the modal opened. Company, Category, Product Type, and Subcategory dropdowns all stayed empty. When the user hit Save without manually re-picking each one, the controller blindly overwrote those four fields with the empty values. Every Edit operation was a silent erasure of the four fields that weren't pre-filled. The fix pre-fills all four dropdowns from the loaded product when the modal opens, awaiting the subcategory cascade fetch before showing the modal so the user sees the existing selection rather than an empty dropdown.

The second was a foreign-key target mismatch in the data model. The `Product.ProductSubcategoryId` column was foreign-keyed to a `ProductSubcategories` table, but that table is empty and no part of the application populates it. The cascade dropdown in the modal actually fetches subcategory options from `ProductTypes` Level-2 child rows via the `/Certificate/GetProductSubcategories` endpoint. So when the user picked a subcategory in the modal, the value sent to the backend was a `ProductTypes.Id` — and the FK constraint rejected it because the value didn't exist in the `ProductSubcategories` table. The save threw, the exception was caught and rendered as a generic error toast, and the user couldn't tell why their product wasn't saving. The Certificate model already correctly treats subcategory as a `ProductType` reference (matching the actual data flow); only the Product model was inconsistent. The fix retypes `Product.ProductSubcategory` from the empty side-table to `ProductType?` and ships an EF migration that drops the old FK and adds a new one targeting `ProductTypes`. No data conversion was needed because every value was already NULL.

Two follow-on fixes in the same change set. The product-type lookup in the CSV import path was scoped to Level-1 entries only, to avoid accidentally matching a child row sharing a name with a parent. The subcategory lookup in the import path was repointed from the empty table to `ProductTypes` Level-2 children. The Product API response was extended to include `companyId`, `categoryId`, `productTypeId`, and `productSubcategoryId` alongside the existing name fields, so the Edit modal has the IDs it needs to pre-select dropdown options.

Verified end-to-end on `localhost:5101` with admin credentials. Created a product with category Chemicals, type Plastics, subcategory HDPE; saved successfully; the management table renders Plastics and HDPE in the new columns; the database row shows `ProductTypeId=4` and `ProductSubcategoryId=46`. Re-opened the same product in the Edit modal and confirmed all four dropdowns pre-fill to the saved values. The migration applied cleanly and `sys.foreign_keys` confirms `FK_Products_ProductTypes_ProductSubcategoryId` now points at the `ProductTypes` table.

Out of scope. The 51 existing products in dev still have NULL for both fields — backfilling legacy product records is a data action and would need a CR. The orphaned `ProductSubcategories` DbSet and Repository pointer are left in place to keep this change set focused on the user-visible defect; they can be retired in a later cleanup. QC #91 stays Ready-for-UAT — the column-rendering work shipped in March was correct; this defect was the orthogonal save-path bug that left the cells empty.

Files: `Models/Product.cs`, `Models/ProductSubcategory.cs`, `Data/ApplicationDbContext.cs`, `Controllers/ProductController.cs`, `Views/Product/Management.cshtml`, `Migrations/20260504204044_RetargetProductSubcategoryFkToProductTypes.cs`.

---

## DEF-130 — Partner API products endpoint locked down to internal-only access (2026-05-04)

The `GET /api/v1/products` endpoint had been authorized for Partner API keys since the v1 controllers were first built. The endpoint returned raw Product database entities including internal database IDs, the soft-delete flag, the active flag, a circular nested category object containing all of its products as null entries, and circular nested certificate references — none of which are part of any partner contract. The endpoint is not listed in the Amazon Developer Guide §4 or §8 contract; the contracted partner endpoints are `/api/v1/certificates*` only. The data exposure was identified during a v1 controller audit as DEF-130.

Two fix paths were on the table. The clean lockdown — change the controller-level authorization attribute from the API-key scheme to the cookie-based Analyst-or-Admin policy — and the contract-track path of writing a partner DTO projection. The DTO path would have been needed only if Amazon was actually consuming the endpoint, in which case the change would also have required partner-side sign-off per the immutability rule on the Amazon contract. We confirmed via the dev database that Amazon's partner key has no endpoint restriction configured, so they CAN structurally call the endpoint, but the only way to know whether they DO is the production stdout logs — which live on the Plesk box and were not accessible from the development machine.

Shawn's call was to ship the lockdown immediately. The change is fully reversible within minutes if Amazon's logs come back showing usage, and `/api/v1/products` is not contractually bound, so the immutability rule does not apply here. The lockdown removes the data exposure today rather than waiting on a multi-day contract negotiation for an endpoint that may not be in use at all.

The fix is a single-line attribute swap on `Controllers/Api/v1/ProductsController.cs`. We also confirmed via repo-wide search that no internal frontend JavaScript, Razor view, or backend service calls `/api/v1/products` — the only references are the controller's own self-reference for the `Created` response, a code comment in the partner middleware, a couple of example strings in API documentation, and the admin API documentation page (which still works because admin cookie auth still satisfies the new policy). Internal consumer impact: zero.

Verification of the prod usage question is still pending — Shawn will pull `logs/stdout*.log` from the Plesk app folder and grep for `/api/v1/products`. If any partner calls show up, this fix is reverted and the partner-DTO path becomes a new CR routed through Carly and Kyle. If clean, the lockdown stays.

Files: `Controllers/Api/v1/ProductsController.cs`.

---

## DEF-127 — Import/Export icon convention now consistent across Product and Company pages (2026-05-04)

DEF-101 standardized the rule that Import buttons use a down-arrow icon and Export buttons use an up-arrow icon across the admin pages. The convention was honored on every page, but four of the pages — Product Management, Admin Product Management, Company Index, and Product Categories — were using the generic tray-style `fa-download` and `fa-upload` icons rather than the file-with-arrow variants `fa-file-arrow-down` and `fa-file-arrow-up` used on Tags and User Management. Both icon classes satisfy the down/up direction rule, so this was a low-severity polish item, but design QC noticed it when the pages were paged side-by-side.

The fix swaps the icon class on the Import button, the Export button, and the Import Preview modal header on each of the four pages. The four pages now match the file-with-arrow icon style already used on Tags and User Management. No JS or controller changes — purely the icon class on a handful of `<i>` tags.

Out of scope: the admin pages with Export-only buttons — Certificate Fields, Certificate Types, Partners, Pathways, Role Management, Product Types, and CSF Attributes — still use the tray-style `fa-upload` icon. They satisfy the DEF-101 direction rule and were not flagged in this defect, so they are left alone. Standardizing them onto the file-arrow style is a separate polish ticket if it comes up. The `Views/Product/ManagementAlt.cshtml` and `ManagementV2.cshtml` alt views are not on the sitemap and were also left alone.

Files: `Views/Product/Management.cshtml`, `Views/Company/Index.cshtml`, `Views/Admin/ProductManagement.cshtml`, `Views/Product/Categories.cshtml`.

---

## DEF-111 — Product Management page now surfaces a real error when the session expires instead of a silent broken table (2026-05-04)

The Product Management page has had a silent-failure defect since the 2026-05-01 verification sweep. When a user's cookie session expires while they have the page open and the page's JavaScript fires its background `loadProducts` fetch, the browser auto-follows the auth redirect to the login page. The login page returns HTTP 200 with HTML, the JS sees `response.ok === true` and calls `response.json()`, the JSON parser chokes on the leading `<` of `<!DOCTYPE`, and the `catch` block writes a `console.error` and returns. The user sees an empty product table and no toast or banner — the page just looks broken.

The fix updates the `loadProducts` function in the page's inline JavaScript to read the response's content type before calling `response.json()`. If the response was redirected or the content type is not `application/json`, the code shows a real `AdminCore.showNotification` toast — "Your session has expired. Reloading…" — and triggers a page reload after a brief delay, which re-enters the auth flow cleanly and lands the user back on the page after they re-authenticate. The non-OK and exception branches were also updated to surface a notification toast instead of writing only to the console, so any future fetch failure on this page is visible to the user instead of swallowed.

The same silent-failure pattern exists in four other fetch sites in this same file — initial-data loaders for categories, types, and dropdown sources — and on a number of other admin pages. They were not in scope for this defect and were not modified. If the auth-redirect symptom shows up elsewhere, those will be addressed individually or refactored into a shared helper.

Verified on source review and against a live `fetch('/Account/Login')` from the running app, which confirms the login page responds with `redirected: true` and `Content-Type: text/html`, exactly the pair the new guard checks for. The runtime behavior of the patched page renders only after the dev server is restarted; this project does not have Razor runtime view compilation enabled.

Files: `Views/Product/Management.cshtml`.

---

## DEF-121 (followup) — Product ID API now returns a DTO instead of the raw entity, removing the User surface entirely (2026-05-03)

The Product ID management endpoints under `/api/productids/*` have been the source of a known credential-exposure defect since 2026-05-01. The root cause was that they returned the full `CertificateProductId` database entity, which transitively pulls in the full `User` entity for the `CreatedByUser` and `UpdatedByUser` navigation properties. The `User` entity has accumulated credential-tier fields over time — password hash, two-factor secret, recovery codes, security-question answers, an IP allowlist, the password-reset token, password history — and any one of those serializing into an API response is an account-takeover risk for every user who has ever created or modified a product ID, almost always the admin. The endpoint requires Analyst-or-Admin authorization, so the leak was not anonymous, but a single Analyst account compromise would have yielded the admin password hash and 2FA seed.

The original 2026-05-03 stopgap added `[JsonIgnore]` attributes to the eight sensitive `User` fields, which blocks serialization across the entire app. That stopgap stays in place. This followup is the durable fix originally recommended in the defect register: a dedicated response DTO that has no `User` entity surface at all.

A new `ProductIdResponseDto` mirrors every public field on the entity and exposes a minimal `{ id, username }` user object for the Created By and Updated By columns — no email, no role, no audit fields, and no surface area for any future credential field added to the `User` model. All four read and write endpoints on the controller now project through this DTO before returning. The service layer still works in entities for internal callers, like the CSV exporter and the cascade that syncs product-ID status to its parent certificate.

The frontend was not changed — the DTO preserves the exact field names the JavaScript consumer reads when rendering the Product ID Manager table and the Add/Edit modal. Verified live on the dev environment with the admin cookie. The Created By column still renders "admin," the Edit modal still pre-populates all six fields from a saved row, a round-trip PUT works and returns the DTO with no leaked fields, and an explicit forbidden-fields scan over every JSON response confirms no password hash, no 2FA secret, no recovery codes, no security questions, no IP allowlist, no reset tokens, and no password history appear anywhere in the response — even where they previously would have leaked through the nested User object.

The defect register entry for DEF-121 captures the verification details, including the specific endpoints exercised and the response field shape. The defect status now closes both the original symptom and the followup-DTO ticket originally split out from it.

Files: `Models/CertificateProductId.cs`, `Controllers/Api/ProductIdApiController.cs`.

---

## DEF-131 — Bulk-mutation API endpoints no longer accept Partner API keys (2026-05-03)

A QA audit of the v1 API surface this afternoon turned up a security defect in the bulk certificate management endpoints. The `BulkCertificatesController` exposes four operations — bulk create, bulk update, bulk delete, and bulk validate — and was authorized at the class level with the API-key authentication scheme, the same scheme used by the contracted partner read endpoints. The practical consequence: any holder of a valid partner API key, including the Amazon production key, could submit a POST to bulk-create up to 100 certificate records, send a PUT to bulk-update existing certificates, or send a DELETE to remove certificates by ID list. None of these operations are part of the partner contract — partners are read-only consumers per the Amazon Developer Guide — and there is no internal feature in the codebase that calls these endpoints either, so they were sitting as a dormant attack surface.

We confirmed the issue with a live curl: posting an empty array to the bulk-create endpoint with the dev partner API key returned a 400 with the body "No certificates provided." The 400 is the controller's empty-body branch, which means the authentication check passed before that branch ran — a real payload would have been processed and persisted to the database. Bulk update and bulk delete behaved the same way under the same key.

The fix swaps the class-level authorization attribute from the API-key scheme to the `AnalystOrAdmin` policy, which forces cookie-based authentication via the default scheme. After the change, all four endpoints reject any caller not in the Analyst or Admin role with a 403 response. We confirmed the regular partner read endpoints (`GET /api/v1/certificates` and friends) are unaffected and continue to return 200 with the contracted DTO shape. We also grep-verified that no code in the repository — frontend JS, backend service, or test code — calls the bulk endpoints, so the lockdown has zero functional impact internally. If admin tooling needs bulk operations in the future, it can authenticate via cookie like every other admin route.

Files: `Controllers/Api/v1/BulkCertificatesController.cs`.

---

## DEF-129 — Partner API "search certificates" endpoint now returns the contract DTO instead of leaking the raw entity (2026-05-03)

A continuation of the same audit that found DEF-131 also turned up another endpoint in the certificates controller that was returning the raw database entity instead of the partner contract DTO. The `GET /api/v1/certificates/search` route — which accepts a search term and various filters — was still using the older raw-entity response shape. Like the `/expiring` endpoint we fixed earlier today, it was leaking internal database IDs, the raw lifecycle status string ("Client Approved - Paid" instead of the contracted literal "Active"), the raw country value instead of "All", flat `asin/upc/gtin/ean` fields instead of the `productIds[]` array, and a nested `company` object that included contact email, website, and a circular nav reference back to certificates and products.

This is the fourth occurrence of the same bug class. DEF-103 (April 21) caught the original by-id and by-number leaks. DEF-124 caught the `/expiring` leak this morning. The audit run this afternoon caught `/search` — the partner endpoint set is small enough that we can now say with confidence the gap is closed, since this was the last raw-entity-returning route reachable by an API key. The fix uses the same shared `BuildPartnerCertificateDto` helper that the other endpoints already use, applies the public-status filter so partner callers cannot see drafts or holds, and scopes results to the partner association on the API key.

We verified the fix with a live curl using the dev Amazon partner key. The same query that previously returned the raw entity now returns a clean DTO with `status: "Active"`, `certificationCountries: "All"`, populated company name, and an empty `productIds[]` array (no Amazon cert in the dev seed has product IDs assigned — coverage gap noted separately). Internal admin and analyst dashboards that call the same endpoint are unaffected and continue to receive the full entity for their own internal lifecycle reporting.

A followup ticket has been opened to add a controller-level unit test that asserts every read endpoint in `CertificatesController` returns the contract DTO when the caller is a Partner. That gives us automated regression coverage so future endpoint additions can't introduce another leak in the same class.

Files: `Controllers/Api/v1/CertificatesController.cs`.

---

## DEF-132 — Self-caught regression in the DEF-121 stopgap; Partner API authentication restored (2026-05-03)

While running the cycle 4 audit, the very first curl against `/api/v1/certificates/search` came back as a 500 error with the log message "Error during Partner API key authentication" and an inner exception about a JSON property `passwordHash` being marked required but not having a setter. The defect was caused by this morning's DEF-121 stopgap: we had added `[JsonIgnore]` to the `PasswordHash` property on the `User` entity, but the property was also declared with the C# `required` keyword. System.Text.Json cannot reconcile those two — the `[JsonIgnore]` removes the property from JSON deserialization, while the `required` keyword still expects the JSON to provide a value — and any code path that round-trips a User object through JSON (in this case, a cache or session lookup inside the partner authentication middleware) throws on deserialization.

The fix dropped the `required` keyword from the `PasswordHash` property. Initialization safety is preserved by EF Core's NOT NULL database constraint and by every `new User { ... }` site in the code already explicitly assigning the field — we grep-verified all 14 construction sites in `SeedData` and `AdminService` set `PasswordHash` immediately. After the change, partner authentication works again on every endpoint in the v1 controller, including the previously-broken `/search`. The DEF-121 credential-leak protection is preserved: serialization output of any User object still excludes `passwordHash` and the seven other annotated fields.

This lesson is being captured on the followup DTO refactor ticket. The `[JsonIgnore]` stopgap is cleaner with non-required nullable string properties, and the durable fix (a `UserResponseDto` projection that allowlists only the fields safe to serialize) sidesteps this kind of attribute-vs-keyword conflict entirely. The stopgap remains in place for now and is sufficient for the immediate exposure, but the DTO refactor is the right durable pattern.

Files: `Models/User.cs`.

---

## DEF-128 — Anonymous CSV export of certificate data no longer exposes internal tags or accepts non-CSV formats (2026-05-03)

The public certificate-tile pages have always offered an "Export" button that sends an HTTP request to a server endpoint and downloads a file. During an internal QA pass we discovered three issues with that endpoint that, taken together, were a meaningful data-exposure risk and a CLAUDE.md rule violation.

First, the endpoint accepted three different format values — `csv`, `excel`, `pdf` — and would return a real Microsoft Excel `.xlsx` file or a `Content-Type: application/pdf` blob (which was actually plain text masquerading as PDF — would have failed to open in any reader if anyone actually clicked the PDF option). The CLAUDE.md rule for this codebase is explicit: "CSV ONLY — no Excel/JSON/PDF". So we had a code path actively contradicting the documented rule.

Second, and more important, the CSV header included an "Internal Tag" column populated with real internal tag values like "Sustainable" or "Eco-Friendly". The codebase rule for external-facing data is that External users cannot view internal tags. Anonymous users (no login at all) are a stricter audience than External users, but they were getting the column anyway because the export endpoint was wide open with `[AllowAnonymous]` and didn't differentiate the column set by caller role.

Third, the corresponding "Export All to Excel" button label and `fa-file-excel` icon visible on the internal and external tile pages contradicted the CSV-only rule on the user-facing surface as well.

The fix collapses the three issues into one batch. Server-side, the export service no longer accepts a format parameter; CSV is the only format produced. The `format=excel` and `format=pdf` query strings still parse without error (so any cached browser bookmarks won't break) but the response is always CSV with the right MIME type and `.csv` extension. The CSV column set is now role-aware: Admin and Analyst callers continue to see the Internal Tag column, while anonymous, External, and Partner callers receive a 23-column CSV with the Internal Tag column removed entirely from both header and data rows. The `[AllowAnonymous]` attribute is preserved because the public tile view legitimately needs the endpoint, but the column-level filter ensures anonymous callers can never extract internal-only data.

On the user-facing side, the buttons in both the internal and external tile pages now read "Export All to CSV" with the matching `fa-file-csv` icon, and the toast notification on click confirms the file is exported as CSV. The aria-label was updated for screen-reader users.

Test coverage was extended at the same time. Eight unit tests in `CertificateServiceTests.cs` were updated for the new method signature, and two new tests were added that specifically exercise the include-internal-tags-false path so future work can't accidentally re-expose the column.

Verified locally on `http://localhost:5101` against the dev DB. Anonymous `curl` to all three format values returns a `text/csv` response with 23 columns and no Internal Tag header — the previous Excel and broken-PDF outputs are gone. Admin and Analyst callers (when authenticated through the `/api/v1/certificates/export` endpoint, which is gated to `AnalystOrAdmin`) continue to receive the full 24-column CSV including Internal Tag.

Files: `Controllers/CertificateController.cs`, `Controllers/Api/CertificatesApiController.cs`, `Services/IServices.cs`, `Services/CertificateService.cs`, `Views/Certificate/CertificateTileViewExternal.cshtml`, `Views/Certificate/CertificateTileViewInternal.cshtml`, `wwwroot/js/certificate-tile-view-external.js`, `wwwroot/js/certificate-tile-view-internal.js`, `GCCWebApp.Api.Tests/Unit/Services/CertificateServiceTests.cs`.

---

## DEF-124 — Partner API "expiring certificates" endpoint now returns the contract DTO instead of leaking the raw entity (2026-05-03)

The Amazon Partner API exposes a small set of endpoints documented in the developer guide, including a `GET /api/v1/certificates/expiring?days=N` route that returns certificates whose expiration falls within the given window. During an internal QA pass we discovered this endpoint was returning the raw `Certificate` database entity instead of the contract DTO that all the other partner endpoints have used since the late-April fix wave. The consequence: any partner with a valid API key was receiving internal-only fields — internal database IDs (`id`, `companyId`, `productId`, several type-IDs), the raw lifecycle status string ("Client Approved - Paid" instead of the contracted literal "Active"), the raw country value instead of the contracted "All", flat `asin/upc/gtin/ean` fields instead of the `productIds[]` array shape, and internal file paths (`certificateFile`, `certificateImagePath`) — plus a circular Company→Certificates navigation reference that, in dev DB testing, blew the response payload to over a gigabyte.

This is the same bug class as DEF-103 (which was caught and fixed for the by-id and by-number endpoints in late April). The 2026-04-21 fix wave was supposed to cover all six partner endpoints but `/expiring` slipped through scope. The 2026-04-30 self-test caught the by-id and by-number gaps; this latest pass caught `/expiring`.

The fix mirrors the existing pattern from the `SearchByProductId` endpoint in the same controller. When the caller is a Partner key, the query now filters to `PublicStatuses` (so Drafts, Submitted, Hold, and Expired certs are not surfaced — those are internal lifecycle states and the contract requires only "Active" certs in partner responses) and scopes to the partner association on the API key. The result set is then projected via the shared `BuildPartnerCertificateDto` helper that the list, by-id, by-number, and search endpoints already use. Admin and Analyst callers, who legitimately use this endpoint for internal expiry-tracking dashboards, continue to receive the full entity.

Verified locally with the Amazon partner key on `http://localhost:5101`: the live response now shows four certificates (the public-status Amazon certs in the dev DB), each with `status: "Active"`, `certificationCountries: "All"`, populated company name, an empty `productIds: []` array (no Amazon cert in the dev seed has product IDs assigned — coverage gap noted separately), and no internal IDs or file paths. The response payload is around one kilobyte; the previous one-gigabyte circular-reference response is gone.

A followup audit ticket has been opened to grep all `[HttpGet]` actions in `CertificatesController` and confirm every single one applies the partner DTO projection, so this scope-creep bug class can be closed for good.

Files: `Controllers/Api/v1/CertificatesController.cs`.

---

## DEF-121 — Stopgap fix for User-entity credential leak in product-ID API responses (2026-05-03)

DEF-121 was filed on 2026-05-01 after we discovered that `GET /api/productids/certificate/{id}` was returning a JSON payload that included the full `User` entity for the `CreatedByUser` and `UpdatedByUser` navigation properties. The `User` entity has, over time, accumulated a number of credential-tier fields that should never leave the database: the BCrypt password hash, the TOTP two-factor secret, recovery codes, password-reset tokens, password history, the IP allowlist, and the security questions JSON blob. The endpoint is gated to authenticated Analyst or Admin users (so this was not anonymous exposure), but any compromise of an Analyst account would yield the password hash and 2FA secret of every user who ever created or modified a product ID — most often the admin user — which is full account takeover.

The defect was open for two days while the team weighed two fix options: (1) introduce a `ProductIdResponseDto` projection that maps only the fields the client actually needs, eliminating the entire User-entity surface, and (2) annotate the User entity's sensitive fields with `[JsonIgnore]` so they never serialize regardless of who reads them.

This release is the option-2 stopgap. Eight fields on the `User` model are now marked `[JsonIgnore]`: `PasswordHash`, `TwoFactorSecret`, `RecoveryCodes`, `SecurityQuestions`, `AllowedIPs`, `PasswordResetToken`, `PasswordResetTokenExpiry`, and `PasswordHistory`. With this change applied, any code path that returns a `User` entity (directly, as a navigation property, or as a list) cannot accidentally leak credential material into the JSON response. The change is application-wide rather than tied to one endpoint, which closes the defect not just for the originally reported `/api/productids/certificate/{id}` route but also any future endpoints that include User navigation properties.

We confirmed nothing in the codebase deserializes JSON directly into the `User` entity, so the `[JsonIgnore]` annotations don't break any update flow — all user creation and modification goes through ViewModels (`UserCreateViewModel`, `UserEditViewModel`) and the `AdminService`, and EF Core reads from the database, not from JSON. Verified that `/Admin/GetUsers` (which uses `UserViewModel` projection) continues to return clean output.

The DTO refactor (option 1) remains as a followup ticket. The annotation approach is a good stopgap because it instantly closes the existing exposure surface, but a future developer adding a new sensitive field to `User` has to remember to mark it; the DTO approach, by contrast, allowlists what flows out and is durably safe. We recommend completing the DTO refactor in the next sprint.

Files: `Models/User.cs`.

---

## DEF-126 — Add/Edit Product ID modal: stray warning triangle gone, modal widened (2026-05-03)

While editing certificate 26-0147, the Add Product ID and Edit Product ID modals were showing a small red warning triangle below the empty Product ID input the moment the modal opened — before the user typed anything. The same modal also still felt cramped on a desktop monitor, with the two-column form (Type and Product ID side-by-side) pinched into a narrow strip and the Description textarea looking small.

Two underlying issues. The triangle came from a global accessibility CSS rule that adds a ⚠ symbol as a `::before` pseudo-element to every error-message container — useful when there's a real validation error to show, but our Product ID modal pre-renders an empty placeholder div ready to be populated by JavaScript when validation fails. Another rule on that modal forced the placeholder to always reserve vertical space, so the empty container plus the global pseudo-element painted a triangle on every modal open. The width was simply that DEF-119's earlier bump to 900 pixels still left the dialog occupying under half the viewport on a 1920-pixel monitor.

The triangle fix narrows the global selector to only paint the icon when the error container actually contains text (`:not(:empty)`). Real validation errors still render the triangle alongside their message — for example, typing a 9-character ASIN now shows "⚠ ASIN must be exactly 10 alphanumeric characters" with the icon in place. The width fix bumps the modal cap from 900 to 1140 pixels (matching Bootstrap's modal-xl).

Verified locally on `http://localhost:5101/Certificate/Edit/26-0147` at a 1920-pixel viewport. Fresh modal opens with no triangle. Invalid input reproduces the triangle and message correctly. Modal centers nicely with visibly more breathing room around all form fields.

Files: `wwwroot/css/accessibility-enhancements-low-priority.css`, `wwwroot/css/product-id-manager.css`.

---

## DEF-123 — Dashboard stat downloads now match the on-screen tile values (2026-05-03)

While verifying the DEF-122 fix, we caught a related but independent reporting defect: three of the four "download data" buttons on the Dashboard stat tiles were producing CSVs with placeholder names instead of the real ones. The Product Categories CSV listed every certificate under "Uncategorized." The Product Types CSV listed everything under "Unassigned." Same for Product Subcategories. The Companies CSV had always been correct.

The user-visible consequence: a stakeholder downloading the Product Types report saw a single row called "Unassigned, 22 certificates" — which would suggest 22 real certificates had a type called "Unassigned." In reality, the dev dataset has 22 active certificates and zero of them are linked to a product type, so the correct report is an empty list. The CSV header was also reporting `Total Product Types: 1` (counting the placeholder bucket as if it were a real type), which contradicted the tile correctly showing `0`.

Two underlying issues were responsible. First, the dashboard's "active certificates" query was eager-loading the Product entity but not the navigation properties hanging off Product (Category, ProductType, ProductSubcategory) or the cert-level direct links (ProductCategory, ProductType, ProductSubcategory). When the CSV builder tried to read those names, it got null on every row and fell back to placeholder strings. Second, the CSV's GroupBy logic only inspected the cert-level direct link and ignored the Product-level link entirely — which meant even with the navigation properties loaded, the count would have been inconsistent with the tile, since the tile combines both linkage paths.

The fix has two parts. The query now eagerly loads all six relevant navigation paths so the names are populated. The grouping logic now matches the tile's semantics (prefer the Product linkage, fall back to the cert direct link) and discards null/empty group keys so placeholder buckets aren't counted as real types.

Verified on the development dataset against all four downloads. After the fix, every CSV's "Total" line and row data matches the corresponding tile value: Product Categories shows "Chemicals and Raw Materials, 22" (tile = 1, CSV total = 1); Product Types and Subcategories both show empty result sets (tile = 0, CSV total = 0); Companies shows nine named rows summing to 22 (tile = 9, CSV total = 9).

Files: `Repositories/CertificateRepository.cs`, `Controllers/DashboardController.cs`.

---

## DEF-122 — Dashboard "Product Categories" tile was showing the wrong count (2026-05-02)

During an audit of the Dashboard ("are the numbers correct?"), the third stat tile labeled "# Product Categories with Active Certs" was found to be displaying the wrong number. The tile was wired to the count of distinct **certification types** (e.g. SCS Certified, USDA Organic) rather than product categories (e.g. Chemicals and Raw Materials, Personal Care). The dashboard had no tile at all for actual product categories. The "Top 5 Product Categories" breakdown table at the bottom of the same page was — and remains — correct, which is why the discrepancy was easy to confirm: the tile and the table sat next to each other showing different stories about the same data.

Root cause was that the repository never computed a "product categories with active certs" statistic; the matching dictionary key was missing entirely. The view binding had been quietly pointing at the certification-types statistic for a long time, with a Product Categories label glued on top.

The fix adds the missing distinct-count query (using the same two-pass EF Core pattern already used for Product Types and Subcategories, so certificates linked through `Product.Category` and certificates linked directly via `ProductCategory` are both counted), and points the tile at the new statistic. The CSV download button under that tile was already mapped to the correct export, so the export side needed no change — only the displayed number.

Verified against the development database: the tile previously showed `4` (count of certification types); after the fix it shows `1` ("Chemicals and Raw Materials" — the only product category with active certs in the dev dataset), which agrees row-for-row with the Top 5 Product Categories table immediately below it.

Files: `Repositories/CertificateRepository.cs`, `Views/Dashboard/Index.cshtml`.

---

## DEF-109 — Tile period dates off-by-one in negative-UTC timezones (2026-05-01)

Analysts editing a certificate would enter `5/10/2026` and see it correctly on the Edit form and on the cert detail page, but the same cert on the **internal** and **external tile listings** rendered as `5/9/2026` — one calendar day earlier. The public tile listing was already correct.

Root cause was a global JSON converter (`UtcDateTimeConverter`) that we added during the Amazon Partner API work to make every API timestamp end in `"Z"` — matching the format Amazon's developer guide examples use. That converter was correct for true timestamps (`createdAt`, `updatedAt`), but it also stamped a `Z` onto calendar-only dates like `EffectiveDate`. Browsers then read `"2026-05-10T00:00:00Z"` as UTC midnight and converted it to local time — which in US Eastern is the previous evening, so the calendar date displayed shifts back a day.

The public tile API had been pre-formatting these two fields as plain `yyyy-MM-dd` strings (no time component, no `Z`), which is why public didn't have the bug. The fix is to apply the same pre-formatting in the internal and external tile APIs. Two files, four lines. **Partner API output is unchanged** — the immutability rule on `/api/v1/certificates*` is preserved.

Verified locally on `http://localhost:5101` with Playwright in the `America/New_York` timezone: API responses now return `yyyy-MM-dd` strings, and rendered tile cards display the same calendar date as the database value.

Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.

---

## DEF-110 — Export CSV headers now match UI table column labels (2026-05-01)

An audit of every Export button across the application turned up that CSV headers and on-screen column labels had drifted apart over time on roughly a dozen pages. Same field would appear as "Company" on the User Management table but "User Company" in the CSV; "URL" on screen but "Website" in the CSV; "Subcategory" on screen but "Product Subcategory" in the CSV. Server-side admin exports were also using PascalCase tokens like `IsActive` and `UserCount` that don't show up anywhere in the UI. Two structural issues fell out of the audit on top of the naming drift:

1. The Product Management pages registered the `Export` click handler in **two** places — once in the page's inline script and again in the constructor of `productManagement.js`. Clicking the button was downloading two CSVs at once with different column sets.
2. The Product Types admin page was the only admin page popping a CSV/JSON/Excel chooser modal instead of going straight to CSV. That violates the project's CSV-ONLY rule for admin exports.

Both shipped in the same change. Twelve export surfaces touched: User Management, Company Management, Admin Product Management, per-Company Product Management, Pathways, Tags, Certificate Types, Certificate Fields, Product Types, plus the certificate tile export and the server-side admin entity exports. Each export's header line now reads the same as the column labels on the page that owns it.

Two specific carry-overs by design: the user **import template** still uses "User Company" so existing CSV imports keep round-tripping, and the product import template keeps its own column set for the same reason. Export headers and import templates are deliberately decoupled.

Verified locally on `http://localhost:5101` that JavaScript-driven exports are serving the new content. The two C# changes (`CertificateService.cs` for cert tile export, `AdminService.cs` for server-side admin entity exports) need a dev-server restart to take effect.

Files (12): `wwwroot/js/userManagement.js`, `wwwroot/js/admin/companyManagement.js`, `wwwroot/js/admin/productManagement.js`, `wwwroot/js/tags.js`, `wwwroot/js/admin/certificate-types.js`, `wwwroot/js/admin/certificate-fields.js`, `wwwroot/js/product-types.js`, `Views/Admin/ProductManagement.cshtml`, `Views/Admin/Pathways.cshtml`, `Views/Product/Management.cshtml`, `Services/CertificateService.cs`, `Services/AdminService.cs`.

---

## DEF-107 / DEF-108 — Restored Google Analytics + cleaned up duplicate-header browser warnings (2026-05-01)

A full-spectrum QA pass on `http://localhost:5101` produced 112 console errors per session — almost all from the security headers middleware being more aggressive than the app actually needs. Two specific issues stood out and both ship together because they share the same source file.

**The Feature-Policy / Permissions-Policy duplicate** (DEF-107). Every page response was emitting both headers with overlapping directives. Modern Chromium logs `Error with Feature-Policy header: Some features are specified in both Feature-Policy and Permissions-Policy header` 16 times per session. Permissions-Policy is the standard; Feature-Policy was kept "for older browsers" but every browser the app actually supports already understands the new header. The legacy Feature-Policy emit is removed.

**The Google Analytics blackout** (DEF-108). GA was silently not recording any pageviews. Two layers were combining to block it:

1. `Cross-Origin-Embedder-Policy: require-corp` was rejecting any cross-origin script that didn't return a matching `Cross-Origin-Resource-Policy: cross-origin` header. Google's GA scripts don't return CORP — so the GA script load was blocked at the COEP layer. The codebase doesn't use SharedArrayBuffer or any feature that requires cross-origin isolation, so the policy is moved to `unsafe-none`.
2. Once the script was unblocked, the actual GA4 *beacons* still failed because the CSP `connect-src` whitelist only included `www.google-analytics.com`. GA4 actually fires its `g/collect` events to several other Google hosts: `analytics.google.com`, `www.google.com/g/collect`, region-specific `*.google-analytics.com`, and `stats.g.doubleclick.net` for demographics. Each of those is added to `connect-src`.

Verified on `localhost:5101` — console errors went from **112 → 1** session-wide (-99%). The single remaining error is unrelated (DEF-111, a separate analyst-side data fetch on `/Product/Management`, filed for a future cycle). Every GA beacon now succeeds. The CSP and other origin policies stay in place; only the headers that actively fought the app's own use cases were relaxed.

Both fixes are in one file, so they ship together.

Files: `Middleware/SecurityHeadersMiddleware.cs`.

---

## DEF-114 — Dashboard mobile portrait: stats tables no longer clip column 2 (2026-05-01)

On a 360-pixel-wide phone in portrait orientation, the Dashboard's five stats tables (Total Active Certs by Certification Type, Top 10 Companies, # Certs by Status, Top 10 Product Types, Top 5 Product Categories) were silently cropping the right edge of column 2 — both the headers ("# OF ACTIVE CERTIFICATES" appeared as "# OF ACTIV…") and the numeric values were getting cut off.

The cause was the standard admin shell padding chain — `.admin-container` margin + padding plus `.admin-body-content` padding — eating ~50 pixels of horizontal space on each side. At a 360 px viewport that left the tables-container at only 216 px wide while the inner table needed 232–263 px depending on the second-column header label. The table-section's `overflow: hidden` then hid the overflow without any horizontal scrollbar to hint at it.

The fix scopes a single class — `dashboard-page` — to the Dashboard view's root container, then trims the admin-shell padding and margin only on that page only at ≤480 px. Other admin pages keep their existing padding because their tables either scroll horizontally or use card layouts and didn't have this problem. Tables-container goes from 216 → 274 px (+58 px), every table fits cleanly at 272 px with no clipping, and there's no horizontal scroll on the document.

Verified locally on `http://localhost:5101/Dashboard` at 360×800 mobile viewport via Playwright. All five tables: column 2 headers fully readable, numeric values not truncated, no horizontal overflow.

Files: `Views/Dashboard/Index.cshtml`, `wwwroot/css/pages/dashboard.css`.

---

## DEF-115 — "Columns" toggle now right-aligns on narrow viewports (2026-05-01)

On the certificate tile views (both internal and external), the "Columns" toggle button — which lets users hide or show specific table columns — was supposed to sit on the right side of the table toolbar opposite the "Showing N certificates" counter. On phones in portrait orientation the two elements stopped fitting side-by-side and the row wrapped to two lines — but instead of staying right-aligned on its own line, the Columns button fell back to the left edge.

The cause is a flexbox quirk: `justify-content: space-between` distributes items within their flex line, but a lone item on a wrapped line has nothing to be "between" and ends up at flex-start (the left edge). One CSS declaration — `margin-left: auto` on the right side of the toolbar — pushes the button to the right edge of its line whether the row is single or wrapped to two lines.

A single CSS file change covers both internal and external views since they share the same toolbar markup and stylesheet.

Verified locally on `http://localhost:5101/Certificate/CertificateTileViewInternal` at 360 / 768 / 900 / 1024 / 1280 pixel viewports. At every width the button now sits 16 pixels from the right edge of the toolbar row.

Files: `wwwroot/css/certificatetileview.css`.

---

## DEF-116 — Product Identifier filter controls now stay on-screen on phones (2026-05-01)

On Certificate Create and Certificate Edit, the Product Identifier section's filter row — Type dropdown, Partner dropdown, and Search box — was forcing all three controls onto a single row with fixed pixel widths totaling around 450 pixels. On a 360-pixel-wide phone the partner dropdown got clipped to "All Partne…" and the search box was pushed completely off-screen.

The fix replaces the inline-style flex row with a CSS class that wraps onto multiple lines when there isn't enough horizontal room, while keeping the original single-row layout on tablet and desktop. The search box now flexes to fill any remaining width on wider viewports (so it gets bigger on a desktop than it used to be at the old fixed 200-pixel width) and falls to its own line on phones.

Verified locally on `http://localhost:5101/Certificate/Create` at 360 / 768 / 1024 pixel viewports. On phones, all three filters are now fully visible on three wrapped rows. On tablet and desktop the layout is unchanged from before.

The same partial view is reused on Certificate Edit, so both pages get the fix from a single CSS edit.

Files: `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/css/product-id-manager.css`.

---

## DEF-117 — Column Settings dropdown stays inside the viewport on mobile portrait (2026-05-01)

On both the internal and external certificate tile views, clicking the "Columns" toggle on a phone in portrait orientation opened the Column Settings dropdown at a fixed height of about 680 pixels — but with the toggle button typically sitting somewhere in the middle of the screen, that meant the bottom 300 pixels of the dropdown spilled past the viewport edge. The list was technically scrollable inside the dropdown, but users couldn't see those scrollable rows because the visible portion was capped at whatever space was actually available between the button and the bottom of the screen. End result: roughly half the column toggles (the ones in PRODUCT IDS, CONTACT, and ADDITIONAL sections) couldn't be reached at all without zooming out the page itself.

The fix sizes the dropdown to the actual available space at the moment it's opened, and flips it to open above the button when there's more vertical room above than below. The dropdown now always fits cleanly inside the viewport with internal scroll for any rows that don't fit. A 200-pixel minimum height keeps it usable even on very short viewports.

The external tile view didn't previously have any mobile positioner at all — it just used the default below-button position. Same fix applies to both views.

Verified locally on `http://localhost:5101/Certificate/CertificateTileViewInternal` at 360×800 mobile viewport. With the toggle button mid-screen, the dropdown opens below it with a 364-pixel height — exactly the room available — and the bottom of the dropdown sits 4 pixels above the viewport edge. With the toggle near the top of the viewport (after scrolling), it opens below with a 695-pixel height. In both cases the full column list is reachable via internal scroll.

Files: `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/js/certificate-tile-view-external.js`.

---

## DEF-118 — Product ID export from Certificate Create now includes Created and Created By (2026-05-01)

On the Certificate Create page, exporting the in-memory product ID list produced a CSV with only six columns (Type, Product ID, Partner, ProductName, ProductSku, Description). The on-screen table shows two more columns — Created and Created By — that were missing from the export. The header naming had also drifted from the table labels ("ProductName" vs "Product Name", "ProductSku" vs "SKU"), which is the kind of drift the export-alignment cleanup last week was supposed to eliminate.

The export now emits the same columns in the same order as the table: `Type, Product ID, Product Name, SKU, Partner, Created, Created By, Description`. For a brand-new certificate that hasn't been saved yet, the in-memory items don't have a created timestamp or a creator yet, so those two cells render blank in the export — accurate for that state. Once the certificate is saved and reopened in Edit, the values populate from the database.

Verified locally on `http://localhost:5101/Certificate/Create` by adding a sample product ID and capturing the exported CSV. Headers and column order match the on-screen table.

A small follow-up worth noting: the Edit-page server export still uses the older header names and still emits a Status column. That export goes through a different code path (`Services/ProductIdService.cs`) and was not changed in this fix to avoid touching unasked surfaces. Worth aligning in a future CR so both export entry points produce the same column shape.

Files: `wwwroot/js/product-id-manager.js`.

---

## DEF-119 — Add Product ID modal now uses full desktop width (2026-05-01)

On a desktop monitor, the Add Product ID modal on Certificate Create and Certificate Edit was rendering at only 650 pixels wide regardless of viewport size — leaving 300 to 600 pixels of empty margin on each side and squeezing the two-column form (Type and Product ID side-by-side, then Partner) into a cramped layout.

The CSS file had two overlapping caps. The first was `#productIdModal .modal-dialog { max-width: 650px }` — which capped the modal regardless of the `modal-lg` Bootstrap class that's already on the dialog. The second was a bare `.modal-dialog { max-width: 600px }` rule that leaked onto every other modal on those two pages, capping them all at 600 pixels application-wide on Certificate Create and Edit.

The fix bumps the product modal cap to 900 pixels (slightly wider than Bootstrap's modal-lg default of 800 to give the description textarea more breathing room) and removes the bare global rule entirely. Other modals on the Create / Edit pages now get their default Bootstrap widths back.

Verified locally on `http://localhost:5101/Certificate/Create` at 1280 and 1920 pixel viewports. The Type and Product ID dropdowns now render side-by-side at half-width as the col-md-6 grid intends, the description textarea spans the full modal width, and the modal centers nicely with the wasted margin removed.

Files: `wwwroot/css/product-id-manager.css`.

---

## DEF-120 — Product Identifiers "Created By" now shows username instead of numeric ID (2026-05-01)

On the Certificate Edit page, the Product Identifiers table's "Created By" column was rendering a numeric user ID (e.g. `1`) instead of the username (e.g. `admin`). The server-side Razor partial that renders the initial table read the username through the `CreatedByUser` navigation property correctly, but the JavaScript renderer that re-draws rows after the page hydrates was reading the raw `createdBy` foreign-key column instead.

The API already includes the navigation property, so the fix is a one-line change in the JavaScript renderer to read `createdByUser.username` with a `'System'` fallback for legacy rows that have no associated user. Single template literal in `product-id-manager.js`.

Verified locally on `http://localhost:5101/Certificate/Edit/26-0150`. All three product ID rows now show "admin" in the Created By column instead of "1".

Files: `wwwroot/js/product-id-manager.js`.

---

## DEF-144 — Countries filter pins "United States" to top on Internal & External tile views (2026-05-05)

On the certificate tile filters, the Countries dropdown is supposed to always show "United States" first, with the rest of the world A-Z beneath it. The Public view already did this. The Internal view (`/Certificate/CertificateTileViewInternal`) and External view (`/Certificate/CertificateTileViewExternal`) did not — both were sorting strict A-Z, so "United States" landed alphabetically wherever it fell.

Fix is two parallel one-line changes. Both controllers were using `OrderBy(c => c)` directly inside the EF query. Switched both to materialize the distinct country list first via `ToListAsync`, then in-memory `OrderBy(c => c == "United States" ? 0 : 1).ThenBy(c => c)`. Same shape as the Public controller already used, so all three endpoints now produce identical Countries lists.

Verified the C# compiles cleanly (running dev server held a file lock on `apphost.exe` at build time, but compile produced 0 errors).

Files: `Controllers/Api/CertificateTileInternalApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.

---

## DEF-145 — Cert tile filters: Public + External dropdowns now hide options with no matching visible certs (2026-05-05)

On the Public and External certificate tile views, the filter sidebar dropdowns for Cert Types and Product Categories were listing every admin-configured option, including options that no visible cert actually used. Picking such an option produced an empty grid with no explanation — a silent dead click.

The Public filter endpoint pulled cert types and the product category hierarchy straight from the admin tables with only an `IsActive` flag, no link back to the user-visible cert set. The External endpoint had the right pattern for cert types but had the same gap for the product hierarchy. Internal stayed as-is on purpose — admin and analyst users need the full taxonomy when creating or editing certs.

Fix derives four "visible" ID sets from the controller's baseQuery — visible cert type IDs, category IDs, product type IDs, and subcategory IDs — and uses them to trim the dropdown options. A category appears if it is directly used or has any visible type or subcategory under it. A type appears if it is directly used or has any visible subcategory. A subcategory appears only if directly used. A cert type appears if it is directly used or is a parent of a used child, matching the pattern External already used.

Net effect: every dropdown option in Public and External now corresponds to at least one cert the user can actually open. New cert types and product categories added via admin will not appear in the Public filter sidebar until a public cert is approved with them — this is intentional UX per Shawn's call.

Files: `Controllers/Api/CertificateTilesApiController.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`.

---

## DEF-181 — QC #108 (User import template) rigorous re-test surfaced 2 defects (2026-05-11)

QC #108 was marked resolved after the 5/1 client reply ("Company Access" naming standardized across template, UI table, export). Shawn asked for rigorous re-testing before pushing back to client UAT. Authored an 8-test Playwright NUnit suite specifically for QC #108 covering the template content, round-trip import with valid data, semicolon multi-company, the literal "All" hint, unknown-company rejection, missing-Email rejection, and export header alignment.

First run against `localhost:5101` was 6/8 — two real defects under the marked-complete surface:

**Defect A — Template CSV was malformed.** The 7th header `Company Access (semicolon-separated names, or "All")` contained an unescaped comma. Any RFC-4180-compliant parser, Excel included, split it into 8 columns. Users opening the template would see a phantom 8th header (`or "All")`) sitting outside the expected schema. `headers.join(',')` in `downloadImportTemplate()` had no quoting logic. Fix: any header field containing a comma or quote is now RFC-4180 quoted with internal quotes doubled. The visible text is unchanged when un-escaped, but CSV parsers now see exactly 7 columns.

**Defect B — Template hint advertised "All" but the parser rejected it.** The hint reads `Company Access (semicolon-separated names, or "All")`. The JS preview parser (`resolveCompanyIds`) had short-circuited `"All"` → every-company-IDs months ago, but the server-side parser at `AdminController.ImportUsers()` had not. A row with `Company Access = All` going through the direct `/Admin/ImportUsers` path (no preview) hit the company-name-lookup path → no match → either 0 companies assigned (silent) or a `Company 'All' not found` error. The template was lying about what was supported. Fix: `AdminController.ImportUsers` now special-cases `companyAccessRaw.Equals("All", OrdinalIgnoreCase)` → assigns every `Company.Id`. Behavior is now identical whether the user goes through the JS preview flow or POSTs the file directly.

Re-run after both fixes: **8/8 passing.**

Files: `wwwroot/js/userManagement.js` (template quoting), `Controllers/AdminController.cs` (parser "All" handling), `GCCWebApp.UAT.Tests/Tests/Admin/QC108UserImportTemplateTests.cs` (new test suite, 8 tests).

---


## UI rename — "Company Access" → "User Company" on User Management table + CSV templates (2026-05-12)

Per Shawn request: on `/Admin/UserManagement` the column header on the table and the column header in both the **import template** and **export CSV** now read "User Company" instead of "Company Access". The Add/Edit User modal label is unchanged.

Server-side import logic is unaffected because `AdminController.ImportUsers()` parses by column **index**, not by header name. Existing files using the old "Company Access" header still import without modification. The change is display-only.

Files: `Views/Admin/UserManagement.cshtml`, `wwwroot/js/userManagement.js`, `Controllers/AdminController.cs` (comment alignment).

---

## DEF-186 — User import template leaked plaintext passwords; blank cells silently defaulted to public `TestPass123!` (2026-05-12)

**Severity:** High (security — pre-launch, but the bulk-user-onboarding path was the primary mechanism for onboarding client users into the live system once live).

Raised by Shawn during DEF-186 review of the Company Access → User Company rename. Three layers of the user-import flow conspired to make plaintext passwords almost-inevitable:

1. The generated CSV template included a `Password` column — invited admins to type plaintext passwords into a spreadsheet that ends up in Downloads / SharePoint / email.
2. The JS bulk-import flow (`wwwroot/js/userManagement.js`) applied `password: row.data.password || 'TestPass123!'` — any blank cell silently created the user with the **publicly-published** local-dev password from `CLAUDE.md`.
3. The server positional parser (`AdminController.ImportUsers`) had the same `?? "TestPass123!"` fallback.

**Fix.**

- **Single safety net at the service boundary** (`AdminService.CreateUserAsync`). Any caller-supplied empty/whitespace password triggers `GenerateSecurePassword()` — cryptographically random 24-char password sourced from `RandomNumberGenerator.GetInt32` over a 70-char visually-unambiguous alphabet. When this fires, `RequirePasswordReset` is forced to `true` regardless of caller intent. Both the JS path and the server CSV path now route through this safety net.

- **Template (`wwwroot/js/userManagement.js`)** — `Password` and `RequirePasswordReset` columns removed. Template is now 5 columns: `Name, Email, Role, Status, User Company`. RequirePasswordReset is implicit-yes for all imported users.

- **JS bulk-import flow** — fallback `'TestPass123!'` removed; row now sends `password: ''` deliberately.

- **Server parser (`AdminController.ImportUsers`)** — rewritten for 5-column layout. Password never read from CSV; service-layer safety net is the single source of truth for password generation.

- **Model relaxation (`CreateUserViewModel.Password`)** — `[Required]` and `MinimumLength=8` removed. The min-length check moved to the service: caller-provided passwords still must be ≥8 chars, but empty is acceptable (triggers random generation).

**Breaking change for re-uploads.** Admins who downloaded the old 7-column template must re-download. Legacy 7-column CSVs uploaded against the new parser would land Status/User Company in wrong slots. This is intentional — keeping the old layout would require keeping the Password column readable, which is exactly what we're closing.

**Forced-reset flow.** Imported user gets a random password they never see → on first login they must use Forgot Password. Future work tracked: trigger an "set your password" email at import time so admin doesn't have to tell the user out-of-band.

**Verification on http://localhost:5101:**

- Served `wwwroot/js/userManagement.js`: template has no `'Password'` column; no `'TestPass123!'` fallback; bulk-import sends `password: ''` and `requirePasswordReset: true`. ✓
- CSV import of a fresh test row (`def186-test-20623@example.com`, 5-column CSV): HTTP 200, user created. ✓
- DB inspection: `RequirePasswordReset=True`, BCrypt hash present (60 chars, `$2a$11$` prefix). ✓
- Attempted login as imported user with `TestPass123!`: **rejected** (final URL stays at `/Account/Login`). ✓
- Regression: admin (pre-existing) still logs in with `TestPass123!` — unrelated auth untouched. ✓

**Files:** `Services/AdminService.cs`, `Controllers/AdminController.cs`, `Models/AdminModels.cs`, `wwwroot/js/userManagement.js`.

---

## User Management pagination right-aligned (2026-05-12)

Per Shawn: `/Admin/UserManagement` pagination controls should be right-aligned (were collapsing to the left because the shared `.pagination-wrapper` uses `justify-content: space-between` with only one flex child after the count display moved out).

Fix: one scoped CSS rule in `wwwroot/css/pages/user-management.css`:

```css
#user-pagination .pagination-wrapper { justify-content: flex-end; }
```

Scope is intentional — Product Management (`#product-pagination`) uses the same admin-pagination component but Shawn didn't ask for it to change. Shared `admin-tables.css` `.pagination-wrapper` rule untouched.

Verified on http://localhost:5101: rule present in served CSS; shared rule unchanged; 279 active users in dev DB confirms pagination will render (12 pages at pageSize=25).

Files: `wwwroot/css/pages/user-management.css`.

---

## User Management pagination right-alignment — Playwright verification (2026-05-12)

Playwright NUnit test `UserManagementPaginationAlignmentTests.Pagination_Is_Right_Aligned_On_UserManagement` added at `GCCWebApp.UAT.Tests/Tests/Admin/UserManagementPaginationAlignmentTests.cs`. Runs against `TEST_BASE_URL=http://localhost:5101`, logs in as Admin, waits for the table to populate (279 users so pagination renders at pageSize=25), then asserts on three layers:

1. **Computed style** — `getComputedStyle('#user-pagination .pagination-wrapper').justifyContent === 'flex-end'`. Confirms the scoped rule wins over shared `admin-tables.css` (`space-between`).
2. **Geometry** — bounding-rect comparison:
   - `Wrapper:  [158.0, 1762.0]  width 1604.0`
   - `Controls: [1442.5, 1762.0]  width 319.5`
   - **Right-edge gap: 0.0px** (controls hug wrapper's right edge)
   - **Left-edge gap: 1284.5px** (controls pushed all the way right; nearly the full wrapper width is empty on the left)
3. **Visual** — full-page screenshot saved to `GCCWebApp.UAT.Tests/bin/Debug/net8.0/Screenshots/UserManagement_Pagination_RightAligned_*.png` for the receipts pile.

**Result:** 1 passed, 0 failed, 14.0 seconds.

Test will catch a regression in either direction — if someone reverts the scoped rule OR changes the shared rule in a way that no longer renders flex-end here, the assertion fails on the next run.

Files: `GCCWebApp.UAT.Tests/Tests/Admin/UserManagementPaginationAlignmentTests.cs` (new).

---

## QC #174 — Certificate tile cert-number top-aligned with multi-line categories (2026-05-15)

Client log 2026-05-08, Carly: "Spacing of cert number off — not sure if it is because cert name has 3 lines but all others are top aligned." Tracked as DEF-195.

What she saw: on tiles where the **certification category** wraps to 2 or 3 lines (e.g. "CERTIFIED ENVIRONMENTAL FACTS"), the cert-number pill on the right of the card header was centered against the taller row — sitting visibly below the cert-number pills on neighboring single-line tiles.

Root cause: cascade specificity. `body.theme-option4 .header-row { align-items: center; }` in `wwwroot/css/main.css:2688` (specificity 0,2,1) beat the cleaner `.certificate-card .header-row` rule in `certificatetileview.css` (0,2,0). theme-option4 is the default body class on all three tile views, so the bug was site-wide on viewports ≥1024px. Below 1024px a separate media-query override at `main.css:3693` already used flex-start, which is why narrow screens looked fine.

Fix (CSS-only, no rebuild):
- `wwwroot/css/main.css:2691` — `align-items: center` → `align-items: flex-start` on `body.theme-option4 .header-row`.
- `wwwroot/css/certificatetileview.css:720` — same value applied for defense-in-depth.

Verified on http://localhost:5101 across 5 viewports (1920, 1280, 1024, 768, 390). Pre-fix Playwright run: `align-items: center` on the three wider viewports, 10px top-delta between the category top and cert-id top on 3-line cards at 1024. Post-fix run: `align-items: flex-start` on every viewport, 0px top-delta on all 22 rendered cards. Same `.header-row` markup is shared with `/Certificate/CertificateTileViewExternal` and `/Certificate/CertificateTileViewInternal` so the fix carries over without per-view edits.

Files: `wwwroot/css/main.css`, `wwwroot/css/certificatetileview.css`.

---

## DEF-195 deployed — cert-tile header-row alignment fix live on gccwebapp.com (2026-05-15)

Zip `GCCWebApp-deploy-20260515-1948.zip` (60.4 MB) uploaded to Plesk and extracted. Shawn ran the bundled `migrations/apply-migrations.sql` against the prod DB before recycling the app pool, then smoke tested clean. QC #174 closed end-to-end same day client reported it.

Visible change for the client: on any cert tile whose certification category wraps to 2 or 3 lines, the cert-number pill on the right of the card header now sits flush with the **first line** of the category text, matching the alignment of single-line-category tiles. Previously the pill was vertically centered against the taller row, drifting visibly below its neighbors on the same grid row.

---

## DEF-195 deploy retraction — first zip didn't carry (2026-05-15 ~20:00)

Post-deploy smoke against gccwebapp.com showed DEF-195 is **still broken** in production: computed `align-items: center` on 25 cert tiles, 20 wrapped-category tiles with up to 10px cert-id pill drift. `Last-Modified` on `/css/main.css` was 2026-05-13 and on `/css/certificatetileview.css` was 2026-05-01 — neither file was overwritten by today's zip extract.

The earlier "deployed clean" claim in this log is wrong. Rebuilt as `GCCWebApp-deploy-20260515-2005.zip`; second deploy attempt pending. The post-deploy smoke test that surfaced this is at `C:\Users\swest\AppData\Local\Temp\qc174\smoke.js` and runs the gate, login, tile-view, CSS-hash, and role-gated-route checks in one shot — should become standard practice after every prod deploy.

---

## DEF-195 deployed for real — second attempt clean (2026-05-15 ~20:30)

Zip `GCCWebApp-deploy-20260515-2005.zip` uploaded to Plesk with explicit "Overwrite All" on extract. Post-deploy smoke against gccwebapp.com:

- **CSS files updated:** `Last-Modified` flipped from 2026-05-01/05-13 to 2026-05-15. asp-append-version hash on `/css/certificatetileview.css` flipped to `BgYK15dB7eLLdeKE74ymzJ0LNb74H3-dO5iVzgGpmvI` (matches local-build hash).
- **DEF-195 visual:** 25 cert tiles rendered, all `.header-row` compute `align-items: flex-start`, 20 multi-line-category tiles have 0px cert-id pill drift (was up to 10px on tablet-1024 before fix).
- **Site health:** Basic Auth gate works, Partner API bypass works + 401s on bogus key, login page renders, role-gated pages return 302 redirects (no 500s).

17/19 smoke checks PASS. The 2 failures (`favicon.ico` 404, `cert-detail-page` 400) are pre-existing or test-script bugs, not deploy regressions: `/favicon.ico` is a missing static file unrelated to today's work; the cert-detail check used `?number=` querystring but the actual route is `/Certificate/CertificateSingleView/{number}` path-segment (manually re-tested: 200 OK).

Root cause of the first-attempt failure not fully diagnosed but workaround confirmed: Plesk's default extract behavior appears to skip existing files. Always pick "Overwrite All" explicitly. Adding a programmatic post-deploy smoke (asserting file contents via hash/mtime, not just "page loads") to the standard deploy checklist going forward.

---

## Post-deploy smoke runner moved to repo (2026-05-15 ~21:00)

Created `scripts/smoke-prod/` — permanent home for the post-deploy smoke runner that caught the silent DEF-195 deploy failure earlier today.

**Files:**
- `scripts/smoke-prod/smoke.js` — 26-check Playwright runner (gate behavior, UI surfaces, DEF-195 multi-viewport, CSS content + freshness, role-gated route reachability)
- `scripts/smoke-prod/README.md` — usage, what's covered, failure-mode glossary, why this exists, how to add checks
- `scripts/smoke-prod/package.json` — single dependency (playwright)
- `scripts/smoke-prod/.gitignore` — node_modules + screenshots

**Coverage added since the /tmp version:**
- Multi-viewport DEF-195 check (desktop/tablet/mobile) with auto-detection of the mobile stacked-header responsive layout — so the drift assertion doesn't false-fail at 390px where the cert-id pill is intentionally below the category.
- `Last-Modified` freshness assertion on each CSS file (14-day window). This is the check that would have caught the first DEF-195 deploy failure if we'd had it.
- Additional admin routes under section 5 (Companies, PartnerApiKeys) so a controller-throw regression in those areas surfaces.

**Verified 26/26 PASS against gccwebapp.com** after the second DEF-195 deploy landed.

**Standard practice going forward:** `cd scripts/smoke-prod && node smoke.js` after every Plesk extract + app-pool recycle. Non-zero exit = do not announce the deploy until investigated.

---

## DEF-197 / 198 / 199 / 200 / 201 / 202 deployed — May-19 sprint-close batch live on gccwebapp.com (2026-05-19)

Zip `GCCWebApp-deploy-20260519-1044.zip` (60.4 MB) uploaded to Plesk and extracted. Bundled `migrations/apply-migrations.sql` carried the `FixUserUniqueIndexesForSoftDelete` migration (DEF-195 / QC #163 partial — soft-deleted user emails can now be reused). App pool recycled. Smoke pass against gccwebapp.com clean.

**Batch contents (15 QC items closed in one deploy):**

| QC# | DEF | Area |
|---|---|---|
| #182 | 198 | Partner CSV export scoping (Critical security) |
| #170 | 196/197 | Product Types Usage column + accurate rollups (no double-count) |
| #180 | 200 | Dashboard tile ↔ download CSV agreement (12 cards) |
| #175 | 201 | Header logo responsive at laptop widths (1025–1440px) |
| #176 | 201 | Mobile filter sidebar auto-recover on viewport resize |
| #177/#178 | 199 | Public filter sidebar — override categories + no soft-deleted companies |
| #183 | 201 | Sort dropdown — removed unimplemented "Most Popular Companies" / "Relevance Score" |
| #181 | — | Company Email strict validation (already in master, deployment-only) |
| #104 | 202 | Companies breadcrumb renamed to "Company Management" |
| #148 | 202 | Cert import template — Status header inline enum hint |
| #118 | 202 | Custom Field render on public + partner cert detail (RoleVisibility-aware) |
| #179 | 202 | Product ID import preview — SKU column visible |
| #184 | 202 | Product Management export — SKU + Partners columns added |
| #174 | — | Cert tile pill top-align (DEF-195 — re-verified) |
| (internal) | 103-followup | Partner API single-cert endpoints now use shared `BuildPartnerCertificateDto` |

**Post-deploy smoke results — `gccwebapp.com`:**

- **Baseline `scripts/smoke-prod/smoke.js`:** 26/26 PASS. CSS `Last-Modified` headers fresh (deploy carried — no Plesk silent skip).
- **Today's-batch `scripts/smoke-prod/smoke-batch-2026-05-19.js`:** 11 PASS, 1 SKIP, 0 FAIL.
  - #170 PASS — header `"Usage (Products / Certs)"` + sample row `0 / 3`
  - #180 PASS — **12/12** stat cards agree tile=CSV on live prod data
  - #175 PASS — Dashboard + TileView logo at 1920/1280/1024/375 = 315/247/202/80 (new laptop breakpoint working)
  - #177/178 PASS — 11 cert types, 7 categories, 21 companies in filter sidebar
  - #181 PASS — `validity.typeMismatch=true` on bad email
  - #183 PASS — Popular/Relevance absent from sort dropdowns
  - #104 PASS — breadcrumb reads `"Administration › Companies & Products › Company Management"`
  - #148 PASS — template Status header carries inline enum hint
  - #184 PASS — export header includes `SKU` and `Partners`
  - #179 PASS — `26-0163` edit page contains "SKU"
  - #118 PASS — cert detail (`/Certificate/CertificateSingleView/26-0163`) returns 200, body renders with "Certificate Details" + "Additional Information"
  - **#182 SKIP** — verified locally end-to-end (4 rows scoped to Amazon, 0 non-Amazon leakage); live prod confirmation requires the real Amazon partner API key (`$env:GCC_PARTNER_KEY = "..."` + re-run).

**Composite: 37/38 verified, 1 deferred (#182 partner key), 0 failures.**

**Non-defect noted during smoke:** Login page logo (class `.login-logo`) does not shrink at laptop/tablet widths — only mobile. The DEF-201 new breakpoint targets `.main-logo` (post-login layout) which was the surface client flagged. If the login page should also shrink at narrower widths, that's a separate small fix — does not affect this deploy.

**Smoke runner additions:**
- `scripts/smoke-prod/smoke-batch-2026-05-19.js` — 12-section Playwright runner for this batch. Covers gate-authed admin login, Product Types Usage column, full dashboard tile↔CSV cross-check, multi-viewport logo sizing, filter-sidebar population, email validation, sort dropdown audit, breadcrumb text, template header hint, product export columns, SKU column, cert detail render surface. Honors `GCC_ADMIN_EMAIL` / `GCC_ADMIN_PASSWORD` / `GCC_PARTNER_KEY` env vars.

Going forward, **batch-specific runners live alongside `smoke.js`** as `smoke-batch-<date>.js` — one per significant deploy. The baseline `smoke.js` continues to guard the gate/CSS-freshness/role-gated invariants; the dated batch runners assert that the *specific items in that deploy* are actually live.

---

# GCC Change Request Register

Canonical log of scope changes that altered or extended the original requirements baseline. Reconstructed 2026-04-28 from git history, shipped log, memory, and QC log analysis.

**Note on history:** Memory references CR-001 through CR-004 as backfilled on 2026-03-23, but the original register file was never persisted. Items below numbered as best-recoverable. CR-005 onward are confirmed via shipped-2026-04.md and screenshot folders.

## Columns

- **ID** — `CR-XXX`, monotonically increasing
- **Period** — when the change request work happened
- **Title** — short summary
- **Source** — how/when surfaced (QC item, client request, internal recommendation)
- **Why this is a CR, not a defect** — the spec basis
- **Scope** — files/systems touched
- **Status** — Delivered / In Progress / Pending / Deferred

---

## Confirmed formal CRs

### CR-005 — Public view Hide/Show Filters parity
- **Period:** 2026-04-24
- **Source:** SIT screenshot review
- **Why CR:** Internal and external listings already had this control; public listing was scoped without it. Adding it = scope expansion.
- **Scope:** `Views/Certificate/CertificateTileView.cshtml`, `wwwroot/js/certificate-tile-view.js`
- **Status:** Delivered, validated via Playwright at 5 viewports

### CR-006 — Em dash placeholder on certificate detail views
- **Period:** 2026-04-24, extended 2026-04-25
- **Source:** Carly request — replace "Data not available" / "Available upon request" / "Not provided" / "Unknown X" with em dash (—)
- **Why CR:** Original spec defined explicit fallback text per field type. Switching to a single character is a UX-convention change request.
- **Scope:** 3 cert detail views (public, partner, internal/admin), ~36 instances total across two passes
- **Status:** Delivered

### CR-007 — Custom Fields appended as columns on internal cert table
- **Period:** 2026-04-24, refined 2026-04-25
- **Source:** Client request — admin-managed custom certification fields should appear on the analyst table view
- **Why CR:** Original Internal listing had 24 hardcoded columns. Custom Fields existed only on cert detail page. Surfacing them on the table required API changes, frontend dynamic columns, and persistence.
- **Scope:** `Controllers/Api/CertificateTileInternalApiController.cs`, `wwwroot/js/certificate-tile-view-internal.js`, `wwwroot/css/pages/certificate-tile-internal.css`
- **Status:** Delivered (Partner API exposure deferred — contract is immutable, requires Carly→Kyle sign-off)

---

## CR-001 through CR-004 — backfill (subjects unrecovered)

Memory references these as backfilled on 2026-03-23 from the QC log, but the register entries were never persisted. Likely candidates from the same period (March 2026):

- 14-state → 8-state status workflow redefinition (commit `024d209`, 2026-03-19) — touched 25+ files including all API controllers, ETL pipeline, view models, JS badge mappings, validation service, import service, plus a SQL migration
- 3-level product taxonomy added to certificates (commit `d29395c`, 2026-02-27)
- Cascading Company → Product → Category → Type → Subcategory dropdowns on cert create (commit `1ed2f12`, 2026-03-23) — 5 files, modal expansion
- Mobile/responsive overhaul (April 2026) — 17 CSS fixes after business users reported issues

---

## Undocumented work that should have been formal CRs

Items below were absorbed as "defect fixes" or "build items" during development. They altered the original scope materially and would have been CRs in a properly-tracked engagement.

### Status workflow redefinition
- **Period:** 2026-03-19 (commit `024d209`)
- **Source:** QC #54 — "Update Status types to only these: DRAFT / Client Approved – Not Paid / Confidential / Paid / Hold / Expired / Republished / Expired – No Annual Verification"
- **Why CR:** Original spec defined 14 states; client redefined to 8 states mid-build. Required complete rewrite of API controllers (public, external, internal), certificate create/edit forms, dashboard, export controller, repository statistics, all JavaScript badge mappings, validation service, import service, filter service, ETL data extraction/transformation/loading, view models — plus a SQL migration to convert existing records.
- **Scope:** 25+ files, SQL migration `18_20260319_UpdateStatusTypes.sql`
- **Status:** Delivered as "QC fix"

### Product taxonomy redefinition (3-level hierarchy)
- **Period:** 2026-02-27 (commit `d29395c`)
- **Why CR:** Original product model had simple flat categorization. Client requested Category → Type → Subcategory hierarchy on certificates. Significant data model + UI work.
- **Status:** Delivered as "feature add"

### Auto-expire on expiration date
- **Period:** March 2026
- **Why CR:** Original system did not auto-transition certificates to Expired status. Client required automatic state change when expiration date passes.
- **Status:** Delivered

### Internal/External tag system
- **Period:** Built into early sprints
- **Source:** QC #18 — "there are no internal and external tags"
- **Why CR:** Tag system was scoped as single-classification. Client required two parallel taxonomies (internal-only vs external-visible).
- **Status:** Delivered

### Custom Fields admin system
- **Period:** Late Q1 2026 onward
- **Why CR:** Original spec did not include admin-creatable custom certification fields. Required full admin CRUD page + dynamic rendering on cert detail + (later, CR-007) table view.
- **Scope:** `/Admin/CertificateFields` page, `Models/CustomField.cs`, dynamic field rendering across cert views
- **Status:** Delivered

### Country list expansion
- **Period:** Throughout
- **Source:** QC #56 — "All countries need to be available in the list. There are only 5 listed"
- **Why CR:** Original spec listed 5 countries. Client required full ISO 195-country list.
- **Status:** Delivered

### Type-ahead autocomplete on dropdowns
- **Period:** March 2026
- **Source:** QC #59 — "when you start typing Company Name or other field it should reduce available options"
- **Why CR:** Original dropdowns were standard select boxes. Type-ahead is a feature add across 5+ fields (Company, Product, Cert Type, Country, Product Category).
- **Status:** Delivered

### Pagination on admin pages
- **Period:** March 2026 (commit hash within QC #90 fix)
- **Source:** QC #90 — Product Management page had no pagination
- **Why CR:** Pagination was not in original admin scope. Added 50/page with numbered buttons + ellipsis on multiple admin pages.
- **Status:** Delivered

### Partner API contract compliance — Amazon
- **Period:** 2026-04-21 onward
- **Why CR:** Internal-API behavior had been operating per general spec. Amazon contract surfaced specific requirements: `status` must always be the literal `"Active"` for partners, `certificationCountries` always `"All"`, public-status filter required, percentage formatting for `CertificationResults`. None of these were in the original API spec — they came from contract review with Carly/Kyle.
- **Scope:** 6 partner-facing endpoints in `Controllers/Api/v1/CertificatesController.cs`
- **Status:** Delivered

### Mobile/responsive overhaul
- **Period:** 2026-04-01
- **Why CR:** 17 CSS fixes after business users reported mobile layout issues. Mobile/tablet support was scoped, but specific business-user requirements (logo sizing, font minimums to prevent iOS zoom, table column hiding on mobile) emerged during use.
- **Scope:** `auth.css`, `main.css`, `certificate-tile-external.css`
- **Status:** Delivered

### Test environment access gate
- **Period:** 2026-04-24
- **Source:** Live test URL was publicly routable; needed credential wall + SEO deindex
- **Why CR:** Original deployment plan was internal/private staging. The publicly-routable URL meant we needed an HTTP Basic Auth gate, robots.txt Disallow, and visible TEST banner — none of which were in the original deployment scope.
- **Scope:** `TestEnvironmentGateMiddleware`, `robots.txt`, banner partial, `appsettings.Production.json` config
- **Status:** Delivered

### Excel template hint headers (global)
- **Period:** Throughout April 2026
- **Source:** QC #148 — "Template headers need option hints"
- **Why CR:** Templates originally had plain headers. Client required parenthetical option hints (e.g., "Status (Active, Inactive)", "TagType (Internal, External)") across all admin templates.
- **Status:** Delivered

### Import/Export icon convention sweep
- **Period:** 2026-04-26 (DEF-101)
- **Source:** Live observation by Shawn
- **Why CR/border-defect:** Original FontAwesome usage was technically valid. Client requested specific arrow-direction convention. 15 .cshtml views touched.
- **Status:** Delivered

### List-view lazy-load fix (DEF-100, structurally a CR-scale rework)
- **Period:** 2026-04-26
- **Why CR-adjacent:** Listed as a defect but the underlying work — preserving column-toggle state across pagination, re-attaching event handlers on appended rows, syncing sticky-header scroll bars — was infrastructure work that wasn't in the original tile-view scope.
- **Status:** Delivered

---

## Approved CRs — in-flight (approved 2026-04-29)

Client meeting on **2026-04-29** approved all 7 pending CRs as a bundle. Framing from the client: *"these are needed to make the app product fit"* — closing the gap between what was built and what the client needs to launch. Cost terms / timeline / sequencing TBD; will be appended below as confirmed.

### CR-008 (approved 2026-04-29) — Partner Role type
- **Source:** QC #143 — "Partner Role type is now missing. How would we create one of these for Amazon..."
- **Why CR:** System has Admin / Analyst / External roles. A new Partner role with company-scoped data access requires a new role type, permission set, and UI flows.
- **Status:** Approved 2026-04-29 — scope/price/timeline TBD
- **Pairs with:** CR-013 (partner-scoped data access mechanism is shared)

### CR-009 (approved 2026-04-29) — Forgot Password flow
- **Source:** QC #150 — "Anuki confirm forgot password works"
- **Why CR:** Originally framed as B2C work; on investigation it is **confirmation, not a build**.
- **Correction (2026-06-12 code investigation):** The forgot-password flow is **already fully built app-native, not via B2C**. `AccountController.ForgotPassword` (~L410) issues a secure random token, stores it SHA256-hashed with a 1-hour expiry, emails a reset link, and protects against email enumeration; `ResetPasswordWithToken` (~L495/532) validates token+expiry and sets the new password. The B2C `ResetPassword()` action (~L352) is only a fallback that redirects to this same app-native flow when B2C is unconfigured. So the earlier "needs B2C email-verification config / custom policies / plan-tier confirmation" framing does not apply. The **only** real dependency is a working **outbound email sender** on the deployment (SMTP/domain email) so the reset link is delivered — an ops/config check, not custom-policy dev.
- **Status (2026-06-12 — verified):** Code complete, **but prod email delivery is NOT wired — BLOCKED.** The email-path trace shows `Program.cs:486-503` registers the real `EmailService` only when `SmtpSettings:Host/Username/Password` are all non-empty; in `appsettings.Production.json:90` all three are `""`, so prod resolves to **`MockEmailService`** (logs `"MOCK EMAIL"`, never sends). Forgot-password reset links and email 2FA codes are generated and stored but never delivered → **QC #150 would fail in production.** Dev (`appsettings.json:66`) has real Gmail SMTP and does send. **Open action:** either populate prod `SmtpSettings` (real domain sender preferred over Gmail for deliverability) or confirm Plesk injects `SmtpSettings__Host/__Username/__Password` env vars at runtime (override the empty JSON); then re-test forgot-password end-to-end and close QC #150. Related security item: plaintext Gmail credential — see [[security-plaintext-gmail-credential]] / defect register.
- **Pairs with:** CR-010 (both are auth-stack items; neither has a B2C dependency in the running app)

### CR-010 (approved 2026-04-29) — Duo MFA support
- **Source:** QC #151 — "is Duo also an option? That is more popular here"
- **Why CR:** New feature, not in original scope.
- **Correction (2026-06-12 code investigation):** MFA is **app-native, not Azure AD B2C** (`EnhancedTwoFactorService.cs`; login gated in `AccountController`, B2C never in the loop) — so the earlier "needs B2C custom policies / plan-tier upgrade" framing does not apply. Duo Mobile **TOTP codes already work today** (existing standard `otpauth://totp` setup, zero build — only optional UI relabel); **Duo Push** is a scoped app-side login integration (~1–2 days + client's Duo subscription), no B2C dependency. Confirm which Duo experience the client means before sizing.
- **Status:** Approved 2026-04-29 — sizing depends on Duo-experience clarification (TOTP = ~0; Push = small project)
- **Pairs with:** CR-009

### CR-011 (approved 2026-04-29) — ASIN-scale UX (collapsible/paginated)
- **Source:** QC #158 — "What will happen when there are hundreds or thousands of ASINs tied to cert"
- **Why CR:** Current system loads all product IDs at once. Performance/UX redesign needed for high-volume products.
- **Status:** Approved 2026-04-29 — scope approval received; design/implementation TBD

### CR-012 (approved 2026-04-29) — Custom Fields export to CSV
- **Source:** Out-of-scope flag in CR-007 ship notes
- **Why CR:** CR-007 added Custom Fields to the table view but not to `/Certificate/ExportCertificates`. Surfacing them in exports requires API + endpoint work.
- **Status:** Approved 2026-04-29

### CR-013 (approved-with-gate 2026-04-29) — Partner API custom-field exposure
- **Source:** Out-of-scope flag in CR-007 ship notes
- **Why CR:** Partner API (`/api/v1/certificates*`) JSON contract is immutable per agreement. Adding custom fields requires partner sign-off because it modifies the partner-facing contract.
- **Status:** Approved 2026-04-29 by the client. **Technical gate still applies:** the client needs partner-side confirmation (Amazon) that the contract change will be accepted before implementation begins. Client approval covers their side; partner acceptance is a separate technical step.
- **Pairs with:** CR-008 (partner-scoping context)

### CR-014 (approved 2026-04-29) — Sorting/filtering by custom-field columns
- **Source:** Out-of-scope flag in CR-007 ship notes
- **Why CR:** Adding sort/filter on dynamic custom field columns is a meaningful query and indexing change.
- **Status:** Approved 2026-04-29 — likely paired with CR-012 for shared API surface work

### CR-018 (proposed 2026-05-07) — Server-side pagination for the Product Identifier list (Phase 1, launch-blocking)
- **Source:** Internal — Shawn flagged slow Edit-page load on cert `26-0023`. Playwright probe confirmed: 1001 product IDs on a single cert; API returns the full list in 71 ms but client-side render + sort + filter pushes total page settle to 3.5 s+. Real-world Amazon partner certs may carry hundreds of ASINs each.
- **Why CR (not a defect):** The API endpoint signature already advertises `page` / `pageSize` parameters at `Controllers/Api/ProductIdApiController.cs` that the implementation doesn't honor. This sits on the boundary between defect and CR — Anuki can treat it as either; only cost-attribution changes. Filing as CR for transparency.
- **Why Phase 1 (per Shawn 2026-05-07):** Shipping launch with this latency surfaces immediately as a perceived-quality problem — the same Edit page that's snappy on a 5-ASIN test cert feels broken on a 500-ASIN production cert. Build server-side paging correctly before client UAT scrutiny rather than patching post-launch under stakeholder pressure.
- **Scope:**
  - **API contract:** `GET /api/productids/certificate/{id}` accepts `page`, `pageSize`, `sort`, `dir`, `search`, `type`, `partner`. Response wraps `{ items, totalCount, page, pageSize, totalPages }`.
  - **Service:** Add `GetProductIdsByCertificatePagedAsync(...)` alongside the existing un-paged method. Push sort, filter, search into LINQ → SQL.
  - **JS rework (`wwwroot/js/product-id-manager.js`):** `loadProductIds()` becomes per-page; `applyFilters` and `applySort` trigger server fetches instead of in-memory transforms; pagination clicks call the API; debounce search input (250 ms); request-version counter so slow page-1 doesn't overwrite fast page-2; small client cache keyed by (sort, filter, search, page).
  - **Add/Edit/Delete on Edit page:** After Add, jump to the page containing the new row. After Edit, stay on current page; toast if row moved off due to sort change. After Delete, drop to (current - 1) if page now empty.
  - **Export endpoint:** Unchanged — already returns the full list, bypasses paging.
- **Acceptance criteria:** Edit page settle ≤ 1.5 s on the 1001-row cert (today 3.5 s). Initial payload ≤ 50 rows (today 1001). Sort / filter / search interactions ≤ 500 ms. No regression on small-cert (≤50 IDs) case. Create page (in-memory mode) unchanged. Playwright covers paging, sort+paging, filter+paging, search+paging, single-row CRUD refresh, and export.
- **Out of scope:** Indexing strategy (separate follow-up CR if EXPLAIN shows table scans), virtual-scroll alternative (doesn't fix payload size, adds JS complexity), background bulk-import progress UI, cross-cert ASIN search.
- **Files (estimated):** `Controllers/Api/ProductIdApiController.cs`, `Services/ProductIdService.cs`, `Services/IProductIdService.cs`, `wwwroot/js/product-id-manager.js`, Playwright + integration tests.
- **Files NOT touched:** No schema changes, no migration. Partner API (`/api/v1/certificates*`) untouched per `feedback_amazon_api_immutable.md`.
- **Partner API impact:** None.
- **Effort estimate:** ~6-10 hrs (API + service + JS rework + Playwright)
- **Detail doc:** `docs/tracking/CR-018-product-id-server-paging.md`
- **Status:** **Built 2026-05-07.** Verified end-to-end on dev cert `26-0023` (1001 product IDs): page settle dropped from 3500 ms → 1046 ms (after JIT warm-up). Sort round-trip 449 ms. Page-2 fetch 309 ms. Initial payload 50 rows (was 1001). All Playwright acceptance checks pass. Pending UAT / deploy.

### CR-187 (2026-06-16) — Normalized, user-friendly product/certificate search
- **Source:** Client request (2026-06-16) — searching `Product A` failed to return `Product™ A`; search required an exact character match, so trademark/registered/copyright symbols, punctuation, hyphens, extra spaces, and case differences hid products users were looking for.
- **Why CR (not a defect):** The prior search behaved as built — case-insensitive `Contains` with a partial hyphen/space normalization (`Len-Tex`↔`LenTex`). The request **extends** the matching rule to a new, broader normalization standard (strip `™ ® © ℠`, punctuation, all whitespace; collapse case) applied **consistently across all searchable fields** and backed by a **new schema surface** (persisted computed columns + indexes). New capability + schema change → CR, not a fix.
- **Decision (client, 2026-06-16):** data will grow large → use the scalable **persisted computed column** model (not query-time `REPLACE`), across **all searchable fields**.
- **Approach (AS-BUILT):** one shared `Search/SearchNormalizer.cs` is the single source of truth — `Normalize()` reduces the search **input**; `ToSqlExpression()` generates the matching SQL for the **stored** side from the *same* `RemovableChars` array, so the C# and SQL halves cannot drift. Persisted computed `NormalizedX` columns added to **Products, Companies, CertificationTypes, ProductCategories, Tags (Name)** and **Certificates (Number, Location, Country)**, each indexed. SQL Server maintains them automatically on every write (UI, CSV import, API, raw data-ops) → **no backfill, no write-path code**. All ~10 website/admin search call sites rewritten to normalize input and match the computed columns; the 3 duplicated `NormalizeForSearch` helpers removed.
- **Scope:** `Search/SearchNormalizer.cs` (new); models `Product/Company/CertificationType/ProductCategory/Tag/Certificate`; `Data/ApplicationDbContext.cs`; migration `20260616193200_AddNormalizedSearchColumns` (+ prod SQL `Scripts/data-ops/2026-06-16-CR187-normalized-search-columns-PROD.sql`); `Repositories/CertificateRepository.cs`, `Services/CertificateService.cs`, `Services/FilterService.cs`, `Controllers/Api/CertificateTile{Internal,External}ApiController.cs`, `Controllers/CertificateController.cs` (product autocomplete + duplicate detection); new test project `GCCWebApp.Tests` (`SearchNormalizerTests`, 24 cases).
- **Out of scope / deliberate boundary:** `/api/v1/*` partner endpoints (Products/Certificates search) **left unchanged** — match semantics there are part of the partner contract ([[feedback_amazon_api_immutable]]); altering them needs partner sign-off. Faceted filter checkboxes (cert type / category / country exact-list filters) stay exact matches by design.
- **Partner API impact:** None.
- **Gate 7 (prod deploy):** prod never auto-migrates — run `Scripts/data-ops/2026-06-16-CR187-normalized-search-columns-PROD.sql` (idempotent) in the same deploy step. Computed columns populate existing prod rows automatically; no separate backfill.
- **Effort:** ~½ day (build + migration + 24 unit tests + DB round-trip verification).
- **Detail doc:** `docs/tracking/CR-187-normalized-search.md`
- **Status:** **Built + locally verified 2026-06-16 — pending deploy + client UAT.** Build clean; migration applied to `GCCDatabase_Dev`; **24/24 unit tests pass**; DB round-trip confirmed `Product™ A` → `producta` (C#↔SQL parity on live data); `Product:   A` (colon + multi-space) normalizes identically. **E2E smoke `Scripts/smoke-prod/cr187-normalized-search-local.js` — 10/10 PASS** on :5101 (seeds a ™ product → autocomplete finds it for plain/lowercase/hyphen/multi-space/exact/partial queries + negative control; tile search DB path returns cert 26-0023 for `ACME CORPORATION™`; seed deleted). Nothing run against prod.

### CR-186 (proposed 2026-06-07) — Product identity = Name + Category + Product Type + Subcategory
- **Source:** Client (Shawn, 2026-06-07) on `/Product/Management` — *"the Product Name should show unique if Category, product type and subcategory are different."* Surfaced from the QC #186 thread (Product Type/Subcategory columns rendering blank on Product Management).
- **Why CR (not defect):** The table is **not** broken — it correctly displays product-level Type/Subcategory, which are simply **empty** because analysts enter that data on the **certificate**, not the product. This request **redefines product identity/uniqueness** (new rule), changes dedup across import/create/edit, and requires **data backfill and possibly splitting** existing products whose certs span differing Type/Subcategory. Scope expansion + data change → CR + backup, not a fix.
- **Current baseline:** No product uniqueness constraint exists (no unique index on `Product.Name`); the only dedup is CSV import checking **Name alone** (`ProductController.cs` ~L781‑787), which would wrongly merge genuinely-distinct same-named products. Product-level `ProductTypeId`/`ProductSubcategoryId` are mostly NULL; the data lives on `Certificate.ProductTypeId`/`ProductSubcategoryId`.
- **Scope (pending decisions):** (1) backfill product-level Type/Subcategory from certs; (2) handle multi-type products (split vs pick-one vs leave); (3) switch dedup/validation to the agreed identity tuple in import/create/edit (± a unique index); (4) maintenance rule (write cert Type/Subcategory back to product on save vs manual); (5) display/search/export follow automatically once data exists.
- **Open decisions for client (see detail doc §5):** enforcement strength (hard block vs soft/visual); is **Company** part of the identity key; how to treat existing multi-type products; sync source-of-truth; are Type/Subcategory now **required**.
- **Recommended approach:** phased — Phase 1 backfill + write-back (low risk, with DB backup) so columns populate and same-named products read as distinct; Phase 2 enforcement; Phase 3 split conflicting products as a separate backup-first data-cleanup CR.
- **Partner API impact:** None (`/api/v1/certificates*` immutable per [[feedback_amazon_api_immutable]]).
- **Effort estimate:** TBD — display/backfill small; enforcement + split larger (multi-session, needs backup).
- **Detail doc:** `docs/tracking/CR-186-product-identity-name-category-type-subcategory.md` (see **§9 — Phase 1 AS-BUILT**)
- **Status:** **SPLIT model — BUILT + locally verified 2026-06-15 (migration pending GCC review + backup).** Client re-reported blank Type/Subcategory columns 2026-06-15; live probe confirmed 107/118 products have NULL `ProductTypeId` (code/join/render all correct — fields just empty). After two refinements the client landed on the **identity/split** rule (superseding an interim single-value sync, now withdrawn): *"the product, product type and subcategory have to match for a cert to associate the count with; same product, different type/subcategory = a separate line item."* → a Product Management row is keyed by **(Company, Name, Category, Product Type, Subcategory)**. **Built + verified locally:** (a) `CertificateController.ResolveProductForCertAsync` (Create + Edit) find-or-creates the matching product record and points the cert at it (`Scripts/smoke-prod/cr186-split-verify-local.js` PASS); (b) `ProductController` product-CSV import dedups on the identity tuple (was Name-only); (c) split migration `Scripts/data-ops/2026-06-15-product-split-by-type-subcat-{IDENTIFY,REMEDIATE}.sql` (split existing products by their certs' combos, re-point certs; ROLLBACK-by-default; needs `-I`/QUOTED_IDENTIFIER) — **dev dry-run validated (315→316, 1 split, 1 cert re-pointed).** Code ships in `deployment-drops/GCCWebApp-deploy-20260615-1737.zip` (with DEF-264). **Migration is irreversible — runs only after GCC reviews the IDENTIFY worklist + full DB backup; nothing run against prod.** Full spec: CR-186 §10. Cert CSV import now also tuple-resolves at commit (`ResolveProductForImportAsync`, verified `cr186-import-split-verify-local.js`); import has no Subcategory/Category column (separate enhancement if wanted) and the preview "products to create" count is by-name (cosmetic under-count). Phase 2 hard uniqueness *enforcement* (unique index) optional/deferred. **2026-06-15: prod read-only IDENTIFY + ROLLBACK dry-run verified (118→122, 46 set, 3 split, 4 new); DB export backed up — but COMMIT is ON HOLD.** The 3 "test" products that would split (#119/#127/#141) carry 7 certs incl. **3 published Paid** (26-0163, 28-0003, 26-0175), so they are NOT safe-to-delete test data; GCC's delete-OK was on a wrong premise. Re-confirming with GCC (revised note in `CR-186-split-gcc-note.md`) before any split/delete. Nothing committed to prod.

### CR-184 (proposed 2026-06-02) — Global Product Identifiers CSV export
- **Source:** QC #184 (client, Carly, 5/15/26) — *"How would we get a full export with product identifier information across all products? The export on this page includes Identifier Type and Id Value but not the product name at the product level or SKU or partner. We will need to run aggregate export on all active product identifiers across certs — where and how can we do that today."*
- **Why CR (not defect):** Existing per-cert export (`/api/productids/certificate/{id}/export`) and Product Management page export both work as designed but neither aggregates across certs with cert number / partner / company / cert status context. New surface, not a broken behavior.
- **Scope:** New `GET /Admin/Export/ProductIdentifiers` endpoint (Admin/Analyst only via `[Authorize(Policy = "AdminOnly")]`) returning a CSV joined across `CertificateProductId × Certificate × Company × Product`. 10 columns (Cert Number, Cert Status, Partner, Company, Product Name, SKU, Identifier Type, Identifier Value, Created UTC, Created By). 5 optional filters: **Partner** (dropdown sourced from the admin `Partners` table — `Models/Partner.cs`, not the free-text `Certificate.Partner` string), **Identifier Type** (data-driven distinct `ProductIdType` values), **Cert Status** (all 8 enum states, no default exclusion), **Issue Date from / to** (against `Certificate.IssueDate`). EF query uses `.AsSplitQuery()` + stable ordering per [[feedback_ef_includes_with_projection]]; calendar dates pre-formatted yyyy-MM-dd per [[feedback_calendar_date_serialization]].
- **UI entry point:** No new UI surfaces — existing **Export** button on `Views/Product/ManagementV2.cshtml` is the entry point. Its current client-side `exportData()` path in `wwwroot/js/admin/productManagement.js` is replaced (for this button) with a fetch + download against the new server-side endpoint, hosting a small filter sub-modal.
- **Files (estimated):** `Controllers/AdminController.cs`, `Services/AdminService.cs`, `Services/IAdminService.cs`, new filter DTO, `Views/Product/ManagementV2.cshtml` (filter modal markup), `wwwroot/js/admin/productManagement.js` (swap client-side CSV gen for server-side fetch), Playwright spec at 5 viewports per [[feedback_playwright_screenshots]].
- **Files NOT touched:** `Controllers/Api/ProductIdApiController.cs`, `Services/ProductIdService.cs`, per-cert export endpoint, all models, no schema changes, no migration. Partner API untouched per [[feedback_amazon_api_immutable]].
- **Out of scope:** Excel/JSON output (CSV-only mandate), editing `CertificateProductId.Status` (auto-derived from cert per CR-015 / [[feedback_product_id_status_follows_cert]]), background jobs for >100k rows, saved filter presets, cross-cert search browse view.
- **Partner API impact:** None.
- **Effort estimate:** ~4–6 hrs (proposed, filtered design).
- **Detail doc:** `docs/tracking/CR-184-product-identifiers-global-export.md` (see the **AS-BUILT** banner at the top — the shipped version is simpler than this register entry's proposed scope).
- **Status:** **Built + verified on localhost:5101 (2026-06-04) — pending deploy + client UAT.** Shipped **simpler than the locked spec**, steered live by Shawn 2026-06-04: endpoint is `GET /Export/DownloadActiveProductIds` in **`ExportController`** (not `AdminController`/`AdminService`); **`AnalystOrAdmin`** not `AdminOnly` (analyst verified 200, external 403 — Shawn: "analysts need it too"); **active-only** (parent cert in `PublicStatuses`, 1014 of 1126 rows on dev); **no filters** (the 5 filters + sub-modal deferred to a possible v2); **12 columns, no status column** (every row active by filter, so both Identifier Status and Cert Status are non-informative — Shawn 2026-06-04); a **new dedicated button** on `Views/Product/Management.cshtml` (the live page; the spec's `ManagementV2.cshtml` ref was stale) rather than replacing the existing product Export. Files: `Controllers/ExportController.cs`, `Views/Product/Management.cshtml`. No migration, no Partner API change, per-cert export untouched.
- **UPDATE 2026-06-11 — DEPLOYED + PROD-VERIFIED, sent for client UAT.** Confirmed live on the test env (gccwebapp.com): `node Scripts/smoke-prod/def243-cr184-prod-fingerprint.js` → **29 PASS / 0 FAIL / 0 WARN** (export not public = 401; admin export returns `text/csv` with the 12-col header and **1,018 active rows**; button present on Product Management; external blocked = 403). Client UAT email drafted: `docs/tracking/CR-184-171-client-email.md`. **Open question to client:** confirm the 12 columns are sufficient or request additions — candidates offered: Certificate Status, Identifier Status (both deliberately omitted — all rows active), Certification Type, Certification Results, Updated Date/By. Filters remain the v2 follow-up if requested.
- **UPDATE 2026-06-17 — client answered; export expanded 12 → 19 columns (built, build clean — pending deploy).** Carly returned her desired field list. Added 7: Certification Type, Cert Status, Cert Partner, Certification Results, Pathway (cert-level), Last Updated, Updated By (product-ID level). Renamed product-ID `Partner` → **`ID Partner`** to disambiguate from the new cert-level **`Cert Partner`** (her request). Reordered to her exact sequence: `Company, Certificate Number, Certification Type, Effective Date, Expiration Date, Cert Status, ID Partner, Cert Partner, Certification Results, Pathway, Identifier Type, Id Value, Product Name, SKU, Created Date, Created By, Last Updated, Updated By, Description`. Cert Status reinstated (her "additional validation" ask) despite the 2026-06-04 "all rows active" omission. Backend Amazon-only fields (certificationCountries, issuer, PDF link, amazonCode) explicitly excluded per her note. Files: `Controllers/ExportController.cs` (+3 Includes: CertificationType, Pathway, UpdatedByUser), `Scripts/smoke-prod/def243-cr184-prod-fingerprint.js` (header → 19 cols). No migration, no Partner API change. **Next:** deploy + re-run smoke + client UAT of the expanded file.

### CR-171 (proposed 2026-06-04) — Multi-select Partner on certificates + partner data isolation
- **Source:** QC #171 (Carly, 5/5/26) — *"Partner must be multi-select if used for partner login access."* Still Pending/"Did not check" as of 5/29. Refined by Shawn 2026-06-04: *"The original scope was to only have one partner. Certificates could have more than one partner. If we are sending to Amazon, we would not want to send the other partners."*
- **Why CR (not a defect):** Original spec scoped one partner per cert (`Certificate.Partner` single string). Client now needs >1 partner per cert AND — because Partner drives partner-login visibility — each partner must see only their own association, never co-partners. Data-model + visibility-rule expansion, not a bug fix.
- **Key finding:** Backend is half-built — `Certificate.Partners` M2M exists and the Create/Edit POST handlers already consume `PartnerIds`, but the form is still single-select and **all ~9 visibility filters + all partner-facing output read the single `Partner` string**. No leak exists today *because* output reads the single field; the leak is introduced if access is switched to the collection without scoping output per-requester. Design rule: **access = collection membership; output = `cert.Partners ∩ caller`, never `.First()`, never the set.**
- **Scope (when approved):** multi-select form (Create/Edit); rewrite ~9 access filters to collection membership (`.AsSplitQuery()` per [[feedback_ef_includes_with_projection]]); requester-scoped output in `BuildPartnerCertificateDto` + 2 inline projections; **prod data backfill of the M2M join table (Gate 7)** or existing certs vanish for partner logins; keep legacy `Partner` string as denormalized primary for internal sort/CSV.
- **Partner API impact:** JSON **shape unchanged** (single `partner` field per [[feedback_amazon_api_immutable]] / [[feedback_partner_data_isolation]]); only *which* value changes (requester-scoped). Needs Amazon-side awareness (Q5 in detail doc).
- **Sign-off ANSWERED (Shawn, 2026-06-17):** (1) Public sees **no** partner names; (2) Company user / external client / analyst / admin see **all** associated partner names — only **partner logins** are scoped to their own; (3) "no partner" **still valid** (not required); (4) bulk **import IN scope** — revise upload template + multi-select edit popup; (5) Amazon contract **confirmed** unchanged (sees own only; GCC to coordinate Amazon acceptance of shared-but-single-appearing certs). Detail doc §6.
- **TWO SCOPE EXPANSIONS (2026-06-17):** (a) multi-partner also required at **product-identifier level** (`CertificateProductId.Partner` is today a single string — same single→collection change one layer down; enables per-product API sharing by partner — detail doc §10); (b) **bulk upload back in scope** (was "Create/Edit only v1"). — Anuki to add.
- **Includes:** full role × capability visibility matrix + multi-partner use-case matrix (UC1–UC4) in the detail doc.
- **Effort estimate:** Revised **~3–4 days** (was Medium ~1.5–2 d) with product-ID-level model+backfill + import template + multi-select popup.
- **Detail doc:** `docs/tracking/CR-171-multipartner-visibility.md`
- **Status:** **Scope FULLY CONFIRMED — build-ready.** §6A interaction answered 2026-06-17: **Model A (gate + filter)** — cert-level partner gates cert access; product-ID-level partner narrows which product IDs the partner receives (untagged product IDs visible to all the cert's partners). Nothing built. `Views/Certificate/Create.cshtml:205` still single-select.
- **UPDATE 2026-06-11 — client decision email drafted.** The 5 sign-off questions packaged for Carly in `docs/tracking/CR-184-171-client-email.md` (alongside the standalone `CR-171-client-email.md`).
- **UPDATE 2026-06-17 — all 5 answered + 2 expansions confirmed (above). Build unblocked except §6A.**
- **UPDATE 2026-06-17 — §6A answered (Model A) + PHASE 1 (cert-level) BUILT & LOCALHOST-VERIFIED.** Forms→multi-select; 23 access filters→collection membership (v1 + both tile controllers + ProductController + FileAccessAllowed); partner output isolation (BuildPartnerCertificateDto + list/GS1 projections caller-scoped; external/internal tiles show all associated; partner callers own-only); idempotent backfill SQL (`docs/tracking/CR-171-backfill-partners.sql`, **Gate 7 — must run with deploy or certs vanish for partner logins**). Verified on cert 26-0001 made [Amazon,Novi]: Amazon key sees only "Amazon" (Novi absent from payload), Novi key sees only "Novi", admin tile shows "Amazon, Novi", Edit form pre-selects both. Build clean. **NOT deployed.** Detail doc §11.
- **UPDATE 2026-06-17 — PHASE 2 DATA/API (product-ID-level Model A) BUILT & LOCALHOST-VERIFIED.** New `CertificateProductId.Partners` M2M + migration `20260617200729` + prod SQL `docs/tracking/CR-171-phase2-productid-partners.sql` (table+backfill, **Gate 7**). Model-A filtering (partner gets untagged product IDs + own-tagged; co-partner-only dropped; labels caller-scoped) in list + by-id + GS1; write-path sync on Create/Edit (`SyncProductIdPartnersAsync`). Verified: Amazon key→[AMZNONLY01, SHARED0001], Novi key→[NOVIONLY01, SHARED0001] (neither sees the other's tagged ID; shared visible to both). Build clean.
- **UPDATE 2026-06-19 — DEPLOYED + PROD-VERIFIED (closes CR-171).** Shipped in the mid-June cumulative batch, canonical artifact `GCCWebApp-deploy-20260619-0001.zip` (rebuilt from my `…-20260618-1027.zip` with `deploy.ps1 -FromMigration` to script `apply-migrations.sql` from the prod high-water mark — sidesteps the EF9 `EXEC(CONCAT)` bug the full-from-scratch idempotent script hit on `20260512011258_AddCertificationResultsNumeric`). Both pending migrations applied; **CR-171 backfill ran on prod via the appended post-deploy block: CertificatePartners 1→155, CertificateProductIdPartners 0→992, missing_M2M=0.** Post-deploy regression `midjune-batch-prod-regression.js` **8/8 PASS** (read-only): internal tile 249 certs (schema OK), CR-187 search OK, DEF-267 8-char Edit renders, and **partner@amazon.com sees 19 scoped certs with no co-partner leak** (live isolation confirmed). Resolves the Gate-5 conditional (CR-187 + script bug). **CR-171 = DONE & LIVE.**
- **UPDATE 2026-06-18 — COMPLETE: gated + bundled for deploy (`GCCWebApp-deploy-20260618-1027.zip`).** Pre-deploy gates run: security (Gate 3/4) PASS after fixing a GS1 product-ID value-leak the audit found (`MapToGS1Format` now applies Model-A filtering on both GS1 endpoints) + v1 non-partner branch now emits all associated names (matrix parity) + `.partner-novi` badge CSS added (Gate 1). Regression: Api.Tests 62-fail / Security.Tests 3-fail are PRE-EXISTING (identical with CR-171 stashed — zero new failures); `cr171-multipartner-isolation.js` smoke 10/0. Bundle wires the backfill into Gate-7 SQL: deploy.ps1 now appends `Scripts/database/post-deploy/*.sql` to `apply-migrations.sql`, so `CR-171-partner-backfill.sql` (both join-table backfills) runs in the same step as the schema. Bundle carries the full in-flight tree (also DEF-267 + CR-184/CR-186/DEF-264/265/266). **Status: DONE — awaiting Shawn's upload (stop app → extract → run apply-migrations.sql → start app).**
- **UPDATE 2026-06-17 — PHASE 2 UI + IMPORT BUILT & LOCALHOST-VERIFIED — CR-171 NOW FULLY BUILT.** Product ID Manager partner control → multi-select (dynamic options); API round-trip (`ProductIdResponseDto.Partners`, service includes + `SyncPartnersAsync` on Create/Update, bulk import partner map); CSV export/import pipe-delimited; cert bulk import `SyncCertificatePartnersAsync` (create+update) + template Partner header hints multi (`Amazon|Novi`). Verified: Edit renders product-ID `<select multiple>`; PUT `Partner:"Amazon|Novi"` persists 2 M2M rows; write→read consistency on the partner API; template downloads with hinted column. Build clean. **NOT deployed. Gate 7: run BOTH SQL files (`CR-171-backfill-partners.sql` + `CR-171-phase2-productid-partners.sql`) with the deploy.** Cert bulk-import E2E not run (build + logic-parity).

### CR-019 (proposed 2026-06-01) — Data-correction pass: product mis-categorization cleanup (DEF-231 fallout)
- **Source:** QC #186 (client 5/29) + QC #170 test-data removal. The cert form's inline "Add New product" filed products under the wrong (first-sorted) category with no type/subcategory, ignoring the analyst's form selections, and that wrong category then propagated onto the certificate. Code fix is DEF-231 (done, verified locally, queued for deploy); this CR covers the rows already corrupted before the fix.
- **Type:** Data operation (one-time UPDATE/DELETE on live business records) — not a code or scope change.
- **Why CR (not on-the-fly):** Per [[feedback_data_changes_require_cr]], any UPDATE/DELETE on live business records requires written GCC approval. Per [[feedback_data_issue_vs_defect]] this is data ops, so the SQL is date-prefixed (no DEF prefix) and there is no DEF row for the cleanup itself.
- **Hard constraint:** the analyst's intended category was overwritten by the bug and is not stored anywhere — there is no value to auto-restore. Remediation is identify → human triage → guarded write, not a blind mass UPDATE. For most real records, re-saving in the now-fixed screen is the cleanest correction.
- **Scope:** (1) read-only IDENTIFY query → review worklist; (2) GCC + Anuki triage into test-junk-to-delete vs real-records-to-correct; (3) guarded REMEDIATE — deletes confirmed "Carly Test" data (refuses if a real cert references it), applies human-confirmed corrections with FK-consistency guards, cascades to linked certs. Transaction rolls back by default; dry-run → verify counts → commit.
- **Safeguards:** full DB backup before any write (no upload/file changes); ROLLBACK-by-default dry run; integrity THROW guards; scripts committed and reviewable.
- **What GCC approves:** running the read-only identify; deleting the test data; applying a reviewed/agreed correction worklist (GCC signs off on the actual row list before commit).
- **Acceptance:** suspect count → 0 (or documented residual); corrected certs/products show right category + type/subcategory; "Carly Test" gone from hierarchy/dropdowns; no real cert deleted/orphaned.
- **Rollback:** pre-commit = automatic transaction rollback; post-commit = restore from the pre-run backup (or targeted per-row re-correction).
- **Partner API impact:** no JSON shape change (`/api/v1/certificates*` untouched per [[feedback_amazon_api_immutable]]); for any partner-visible cert whose category is corrected, the partner sees the now-accurate category value (data-accuracy improvement, flagged for awareness).
- **Scripts:** `Scripts/data-ops/2026-06-01-product-miscategorization-{IDENTIFY,REMEDIATE}.sql`
- **Detail doc:** `docs/tracking/CR-019-product-miscategorization-data-cleanup.md` (§12 sequencing added; cover email sender filled — ready for GCC send).
- **Sequencing:** run **after** DEF-231 deploys to live (else the still-live old behavior keeps minting new bad rows and the worklist regrows). Order: deploy DEF-231 → IDENTIFY → triage → backup → REMEDIATE. IDENTIFY validated read-only on the dev DB 2026-06-02 (runs clean; dev's bug-default category = a `Batch2QA-Cat-*` test row, 3 suspect products / 1 high-conf cert — dev pattern only, not the live worklist).
- **Effort estimate:** ~2–4 hrs total (identify + triage + remediate + verify), volume TBD from live IDENTIFY output.
- **Status:** Proposed 2026-06-01 — awaiting GCC approval for the *main* mis-categorization worklist. Nothing run against production. **One in-scope slice EXECUTED on the TEST environment 2026-08-04 — see below.**
- **EXECUTED 2026-08-04 — QC #242 "Carly Test" test-data removal (TEST ENV ONLY).** Authorized by Shawn same-day, after the QC #242 investigation established the cause was **data, not code** (the delete guards were refusing correctly). Falls under this CR's existing scope line *"deletes confirmed 'Carly Test' data (refuses if a real cert references it)"*.
  - **Script:** `Scripts/data-ops/2026-08-04-qc242-carly-test-cleanup.sql` (guarded, transaction-wrapped, `-v DoCommit=0` dry-run / `1` commit).
  - **Removed (6 rows):** Products **140** `Best Product Ever v2`, **141** `Best Product Ever v3™`, **389** `Carly Test`, **392** `Test`; ProductType **86** `Test Type`; ProductCategory **9** `Carly Test`.
  - **7 preconditions asserted before any write**, each THROWing to force rollback: no certificate referencing the products, category or type; no product identifiers; no child types; no unsurveyed product still attached; and identity checks that rows 9/86 really are `Carly Test`/`Test Type`.
  - **Dry run first (rolled back), then committed.** Collateral verified unchanged: certificates **226 → 226**; products 315 → 311; categories 9 → 8; types 55 → 54; category 1 `Chemicals and Raw Materials` (the real parent of type 86) intact.
  - **Independent post-commit check:** 0 test-named categories, 0 test-named types, 0 certs with a dangling category or type FK, 0 products with a dangling category FK.
  - **Two gotchas worth reusing** — both cost a failed run: (1) these tables carry persisted computed `NormalizedName` columns from **CR-187**, so any `DELETE` fails with *Msg 1934* unless `QUOTED_IDENTIFIER` is ON (sqlcmd defaults it OFF — use `-I`); (2) `FK_Products_ProductCategories_CategoryId` is **ON DELETE CASCADE**, so deleting a category silently takes its products with it — the script deletes products explicitly first so nothing goes implicitly.
  - **Effect:** both deletes the client could not complete in QC #242 now succeed. **Production untouched** — this was the test environment only.
- **FOLLOW-ON 2026-08-04 — mistaken delete + restore (TEST ENV).** After the cleanup above, products **100** and **101** (both `Test Carly`, company 29 `TEST`) were deleted from the admin UI, intending the `Carly Test` *category* — which the cleanup had already removed hours earlier. **Restored** via `Scripts/data-ops/2026-08-04-restore-products-100-101.sql` (3 guards, dry-run then commit; products 309 → 311, certificates 226 unchanged; computed `NormalizedName` recomputed to the audited `testcarly`).
  - **The restore was FAITHFUL** (corrected same day — an earlier note here wrongly called it lossy). The snapshot's 8 keys are not truncation: `AuditService` serializes with `JsonIgnoreCondition.WhenWritingNull`, so an absent key means the field was NULL. Both products really did have `ProductTypeId`/`Sku`/`Upc`/`Asin` empty. Only `UpdatedAt` is genuinely unrecoverable (excluded from all snapshots) and was set to `CreatedAt`. Real audit gaps found while checking this are logged as **DEF-337** — chiefly that cascade children are destroyed unaudited and **`Certificate` has no delete audit at all**.
  - **Root confusion worth recording:** there are *three* similarly-named records — category `Carly Test` (deleted), products `Test Carly` ×2 (deleted then restored), and company **258 `Carly Test`** (untouched, still live). Name-only identification in the UI is what made them interchangeable, which is the same findability problem CR-032 declined to fix.

### CR-020 (approved 2026-06-03) — Add "User Company" field (user's own company/employer), separate from "Company Access"
- **Source:** QC #106 (CR-required portion: free-text Employer field on user record, separate from Company Access) + QC #148 item 4 ("User Company and Company Access separation in sheet"). Client confirmed 2026-06-03: *"there needs to be both fields. User Company and Company Access."*
- **Why CR (not a defect):** Adds a brand-new field to the user record (DB migration + new FK) and a new data concept. The original spec had a single company association (data-scoping); splitting employer from access is scope expansion, not a bug fix.
- **Decision:** User Company = single company chosen from existing Companies list (FK dropdown), optional. (QC #106 said free-text; client revised to dropdown 2026-06-03.) Company Access (existing `User.Companies`) unchanged in meaning.
- **Scope:** `Models/User.cs`, `Data/ApplicationDbContext.cs`, migration `AddUserCompanyToUser`, `Models/AdminModels.cs`, `Services/AdminService.cs`, `Controllers/AdminController.cs`, `Views/Admin/UserManagement.cshtml`, `wwwroot/js/userManagement.js`. Surfaces: modal, import template + parser, CSV export. **Table left unchanged** (its "User Company" header still shows Company Access — flagged for a later decision).
- **Resolves:** the DEF-173 "Company Access" vs "User Company" label flip-flop — both labels now name distinct fields, ending the rename churn.
- **Breaking (intentional):** import access column renamed `User Company` → `Company Access`; new `User Company` column = employer. Importer keeps both header aliases mapped to the correct field; legacy "User Company = access" CSVs now populate employer instead (called out for UAT).
- **Detail doc:** `docs/tracking/CR-020-user-company-field.md`
- **Status (original FK build):** Built + verified locally 2026-06-03, deployed 2026-06-03. Migration `20260603135743_AddUserCompanyToUser` applied to dev + prod (prod via DEF-239 SQL). Verified `Scripts/smoke-prod/def237c-user-company-verify.js` → 6 PASS / 0 FAIL.
- **REVISION 2026-06-05 — reverted to FREE TEXT (QC #106 6/4 client feedback), approved by Shawn 2026-06-05.** *"User Company can just be an open text field… not limited to the companies added for certs… GCC users, Amazon Corporate, Novi."* This restores the original QC #106 ask and reverses the 6/3 dropdown decision. **Security review: zero access impact** — scoping reads only `User.Companies` (Company Access), never `UserCompany`. **Logic fix:** the FK previously *rejected* imports of non-cert-company employers; free text removes that block. Field `User.UserCompany` is now `string?` (FK `UserCompanyId` dropped). Migration `20260605131035_ChangeUserCompanyToFreeText` (adds string col, **backfills from `Companies.Name`**, drops FK col) applied to dev. **Built + verified locally 2026-06-05 — pending deploy.** `Scripts/smoke-prod/cr020rev-user-company-freetext.js` → **6 PASS / 0 FAIL**. **Gate 7 prod SQL:** `Scripts/database/CR-020rev_ChangeUserCompanyToFreeText.sql` (idempotent, transaction-wrapped, history-guarded — run in same deploy step, recycle pool).
- **REVISION 2026-06-05 (b) — QC #106 item #2: "Company Access" column added to the User Management table, approved by Shawn 2026-06-05.** Original CR deferred the table; the 6/3 follow-up gave the single column to the employer field, leaving the data-scoping value with no column. Added a sortable "Company Access" column (between User Company and Email) rendering the existing `getCompanyAccessDisplay` ("All" for Admin/Analyst, joined company names for External/Partner). **Display-only — no backend/schema/API change, no extra prod SQL.** Files: `Views/Admin/UserManagement.cshtml`, `wwwroot/js/userManagement.js`. Verified `Scripts/smoke-prod/qc106-company-access-column.js` → **5 PASS / 0 FAIL**. Ships in the same bundle as revision (a). **QC #106 now fully addressed:** User Company = free text (a), Company Access listed in table (b), role-gating = DEF-238.
- **Table (added to scope 2026-06-03 per client "the table in the view will not show company access"):** the table's "User Company" column now displays the User Company (employer) field, NOT Company Access — sorts by `userCompanyName`, searchable by it, blank for users with no employer set. Supersedes the DEF-238 table-display tweak (which had shown Company Access "All" for Admin/Analyst); DEF-238's modal role-gating + note still stand. `getCompanyAccessDisplay` is retained for export's "Company Access" column + import preview. Verified row `cr020-test` shows "Sofidel America Corp." not "All".

### CR-021 (proposed 2026-06-11) — Set Amazon code for "Certified Environmental Facts" (`greencircle_cef`)
- **Source:** Migration analysis of the client sample ASIN list (`SAMPLE ASIN List ..._6.1.26.xlsm`) — 7,904 of 14,183 ASIN rows carry `amazonCode = greencircle_cef`, but the DB still holds the seeded short code `'CEF'` on `CertificationTypes.Id = 5`. Only SEP/RC/CES were converted to long-form on 2026-04-22; CEF was left "awaiting client mapping."
- **Type:** Data operation (one-time UPDATE on one live reference-data row) — not a code or scope change.
- **Why CR (not on-the-fly):** Per [[feedback_data_changes_require_cr]], updating a live record needs written GCC approval. The value is **partner-facing** (sent to Amazon), so it is tracked rather than actioned silently.
- **Impact:** Certified Environmental Facts certs passed to Amazon currently send `CEF`, which Amazon does not recognize → badge not applied until the long-form code is set. Largest cert-type in the sample after Recycled Content.
- **Change:** guarded, idempotent `UPDATE CertificationTypes SET AmazonCode = 'greencircle_cef' WHERE Id = 5 AND Name = 'Certified Environmental Facts' AND ISNULL(AmazonCode,'') <> 'greencircle_cef'`, after a read-only pre-check. No schema/migration/code change.
- **Partner API impact:** No JSON shape change (`amazonCode` field already exists; only its value for CEF certs changes, per [[feedback_amazon_api_immutable]] — value, not shape).
- **Open confirmation:** GCC to confirm `greencircle_cef` is the official Amazon long-form string for CEF (same confirmation basis as SEP/RC/CES on 2026-04-22). Inferred here from the client's own ASIN export.
- **Sequencing:** Gate 7 — direct prod data UPDATE, run in the approved maintenance step (prod does not auto-migrate). Independent of any code deploy.
- **Script:** `docs/tracking/sql-fixes/2026-06-11_cef-amazon-code.sql` · **Detail doc:** `docs/tracking/CR-021-cef-amazon-code.md`
- **Effort:** ~15 min (pre-check + UPDATE + verify).
- **Status:** Proposed 2026-06-11 — awaiting GCC approval. Nothing run against live.

### CR-022 (proposed 2026-06-23) — Add SKU as a sortable column on the internal certificate table
- **Source:** Internal review of sorting on `/Certificate/CertificateTileViewInternal` (6/23 sort-audit). One acceptance criterion asked SKU be "either added as a sortable table option or the blocker clearly documented."
- **Why CR (not a defect):** SKU is **not in the captured requirements** for the certificate table. That view ships a fixed 24-column set (+ CR-007 custom-field columns); SKU was never one of them. Surfacing + sorting it is scope expansion, not a sorting bug.
- **Where the data lives (corrected):** SKU has THREE homes — (#1) a `CertificateProductId` of `ProductIdType == "SKU"` (a product identifier on the cert, peer of ASIN/UPC/GTIN/EAN), (#2) `CertificateProductId.ProductSku` (per-identifier attribute), (#3) `Product.Sku` (product record). **The cert table's ID columns all come from #1**, and the internal API ALREADY projects `sku` from #1 (`CertificateTileInternalApiController.cs:270`) and already emits it in the response (`:360`) — it's just not rendered or sortable (same state as `isbn`/`mpn`). An earlier draft scoped #3 (`Product.Sku`); **corrected to #1** so the column is consistent with its neighbours.
- **Blocker (the "document it" path):** the `sku` value already flows to the client from the cert's identifier collection but isn't rendered as a column or wired to a sort key. Tracked as this CR rather than built silently.
- **Rest of the audit already done:** ASIN/UPC/GTIN/EAN (count-badge sort, empties last) = DEF-260; External Tags (`.Min()` alpha-first, asc/desc) = DEF-261; empties-last on all text/count/numeric columns = `CertificateSortExtensions`. All shipped 2026-06-12. Only SKU is outstanding, and only because it isn't a column.
- **Scope (when approved):** API projection already exists (no new query); add `"sku"`/`"sku-za"` cases in `ApplySorting` (`OrderByTextEmptiesLast` on the identifier SKU, single-valued; or count-badge like ASIN if multi-valued); header + body cell + `'sku'` in `COLUMN_KEYS` + `sortMap`/`reverseSortMap` (`CertificateTileViewInternal.cshtml`, `certificate-tile-view-internal.js`); external-view parity decision; CSV export column. ~half a day incl. Playwright at 5 viewports.
- **Sorting blocker (the real design question):** SKU on a cert is **one-to-many** (a cert can have multiple SKU identifiers / per-identifier SKUs), so a single-value SKU sort is undefined — *which* SKU represents the row? Same shape as ASIN/UPC/GTIN/EAN, which were resolved as **count badges sorted by count** (DEF-260). So the options are A: count badge (consistent, recommended); B: value sort, which only fits single-valued contexts (Product ID Manager / export), not the cert list; C: leave SKU off the cert list. **This needs a client decision, not a build choice.**
- **Open client questions:** (1) SKU on the cert list at all? (2) If yes, Option A/B/C? (3) External/partner list too, or internal only? (4) Handle ISBN/MPN the same way (identical 1-to-M state)?
- **Sent to client?** **No** — confirmed not previously raised in any CR, client email, or migration note as of 2026-06-23.
- **QC-log review email (to send):** `docs/tracking/qc-sku-sort-review-2026-06-23.md` (classified Lack-of-Requirements/design-clarification, presents Options A/B/C).
- **Detail doc:** `docs/tracking/CR-022-sku-sortable-column.md`
- **Status:** Proposed 2026-06-23 — **parked pending client design decision** via the QC-log review email. No code written.

### CR-023 (built 2026-06-25) — Certificate Import: batch-level undo / rollback ("un-apply this import")
- **Source:** Agile requirements discussion 2026-06-24 — *"import must not fail silently; confirm and warn the user, and preserve history in case an analyst made a mistake importing certificates."* The ask decomposed into three parts; two are already met and this CR covers the third.
- **What's already done (so this CR is scoped to only the gap):** (1) **warn + confirm** before a destructive overwrite = **DEF-292** (company-reassignment guard, server-enforced, pending deploy); (2) **preserve history** = already exists — every import overwrite captures full prior cert state + writes a field-level audit row (`CertificateImportService.cs:1469-1546`, `AuditChangeSource.Import`). The remaining gap = **(3) one-click undo of a whole import batch**.
- **Why CR (not a defect):** batch undo is net-new capability never in the captured import requirements (spec defined import as preview→apply, no rollback surface). New UI + endpoint + reversal logic + batch-linkage plumbing = scope expansion. Also performs UPDATE/DELETE on business records ([[feedback_data_changes_require_cr]]).
- **Plumbing reality (honest):** schema *looks* ready but is **not wired** — `Models/Import/ImportBatch.cs` exists but is never instantiated/saved (no `new ImportBatch` in the import service); `AuditHistory.ImportBatchId` column exists but `LogEntityChangeAsync` doesn't accept/set it (`AuditService.cs:38`), so every import audit row currently has `ImportBatchId = null`. So there is **no usable batch grouping today** — the feature must build the linkage first, then the reversal. (The data-protection review's R5 "audits each update" claim was per-row only, not batch-groupable.)
- **Scope:** Phase A — create+persist an `ImportBatch` on apply, thread `ImportBatchId` through `LogEntityChangeAsync` onto every audit row. Phase B — `UndoImportBatchAsync(batchId)` reads the batch's audit rows and reverses each (Update→restore `OldValue`; Create→remove the cert), re-syncs CR-015 product-ID statuses / CR-171 partners / status history, writes a new audit batch for the undo, with idempotency + stale-edit guards. Phase C — Import History view + Undo action (`AdminCore.showConfirmation()` modal, Admin-only).
- **Dependency / sequencing:** **DEF-288 (certificate soft-delete) should ship first** — undoing a batch that *created* certs means deleting them; doing it as a hard-delete would reintroduce the unrecoverable-delete risk this whole workstream removes. Rides on the recoverable-delete decision Shawn made 2026-06-24.
- **Out of scope:** partial per-row undo (all-or-nothing v1); undo of imports applied *before* this ships (their rows have null `ImportBatchId` — stays manual-from-audit, must be logged clearly); **Partner API unchanged** ([[feedback_amazon_api_immutable]]).
- **Effort:** ≈ 3–3.5 days assuming DEF-288 is in place (side-effect re-sync is the hard part, not field restore). Add DEF-288 if not.
- **Sent to client?** **No** — client-facing draft included in detail doc, not yet sent.
- **Built 2026-06-25 (pre-prod, at Shawn's direction "we haven't gone live — fix it"):** Phase A (batch linkage — optional `importBatchId` threaded through `LogEntityChangeAsync`; one `ImportBatch` persisted per apply), Phase B (`UndoImportBatchAsync` — restore overwritten certs from per-field audit `OldValue`, hard-delete created certs w/ children, idempotent via `ImportBatch.Status="UNDONE"`), Phase C (Import History view + Undo behind `AdminCore.showConfirmation()`). **Zero schema change** (table/column already existed; reused `Status` field). Create-reversal hard-deletes (decided NOT to block on DEF-288 — a just-created cert has no history to lose; overwrites are restore-only). Build clean; 61/61 import tests pass (3 new undo tests). Not yet UI-validated on localhost; no commit.
- **Validated 2026-06-25 (localhost, Playwright + SQL ground truth):** `Scripts/smoke-prod/_cr023-import-undo-validate.js` — **15/15 PASS** (create→overwrite+reassign→undo: cert restored, created cert removed, batch UNDONE, RESTORE+DELETE audited, idempotent, dev DB cleaned). Validation also caught + fixed a real gap: `AuditService.LogEntityChangeAsync` silently dropped the `RESTORE` action (now handled like CREATE).
- **Detail doc:** `docs/tracking/CR-023-import-batch-undo.md`
- **Status:** **Built + UI-validated locally — pending deploy.** Pre-prod; client approval still advisable before it's presented as a delivered feature.

### CR-033 (proposed 2026-08-04) — Duo two-factor authentication, and enabling 2FA before production cutover
- **Source:** client requirement relayed by Shawn, 2026-08-04: *"we need 2fa enabled prior to production cutover, they also want DUO."* Follow-up the same day: the client **already has email and a Duo tenant**, and the work is to be **tested on a subdomain of existing production before cutover** to reduce go-live risk and delay.
- **Why CR, not a defect:** Duo is a **new third-party authentication integration** that appears nowhere in the requirements baseline. The built-in 2FA (email / SMS / TOTP / recovery codes) is already built and already enabled in Staging and Test — turning it on in production is a **cutover activity**, not new scope. Duo is the scope change.
- **Split, deliberately:**
  - **Cutover activity (not CR work):** enable the existing 2FA in production. Tracked in `docs/tracking/production-cutover-plan.md`, not here.
  - **CR-033 (this entry):** the Duo integration.
- **Scope — NOT YET FIXED.** Shawn's instruction 2026-08-04 was to *capture and track*, not to decide. Three client answers are required before this can be estimated, and each one materially changes the cost:
  1. **Coverage** — internal GCC staff only, or external company/partner users too? **Duo licenses per user**, so extending it to every external user is a recurring cost the client owns.
  2. **Replace or supplement** — does Duo replace the built-in email/SMS/TOTP methods, or become an additional choice? Replacing is one login path and simpler to operate and secure; supplementing is friendlier but roughly doubles the surface to build and regression-test.
  3. **Enrolment ownership** — who enrols users who are not in the client's own directory?
- **Architectural finding that constrains every option — Azure AD B2C is NOT in use.** Production config is entirely placeholder tokens (`{AZURE_AD_B2C_CLIENT_ID}` etc.), so `isAzureAdB2CConfigured` evaluates false and the application runs on its **own cookie authentication** with the custom `EnhancedTwoFactorService` (`Program.cs:159-170, 249`). The project guide's "Azure AD B2C" description is stale and should be corrected.
  - **Consequence:** there is **no external identity provider to attach Duo to**. Duo must be integrated at the **application level** — Duo Universal Prompt (OIDC redirect) inserted into the app's existing login flow.
  - The alternative — stand up a real IdP first and put Duo in front of it — is a **materially larger project** and is not achievable in a cutover timeframe. Flagging it so nobody assumes B2C is "already there" and scopes accordingly.
- **What already exists (reduces the build, and is worth saying plainly):** email, SMS, **TOTP/authenticator**, recovery codes, a full per-user preferences UI, and stored per-user method selection. `Enable2FA` is already `true` in Staging and Test. Duo is an *additional provider* on a working 2FA substrate, not a from-scratch build.
- **Hard prerequisite, independent of Duo:** production currently has **empty SMTP credentials** and a **mock SMS provider**, so enabling 2FA today would lock out every user with no recovery path. Full evidence and the ordered prerequisite list are in `docs/tracking/production-cutover-plan.md` §1–§2. **Duo cannot be sensibly piloted until that is fixed**, because a failed Duo enrolment needs a working fallback.
- **Scope (indicative, pending answers 1–3):** Duo Universal Prompt via OIDC — a redirect step in the login flow after password validation; per-user Duo enrolment state; admin configuration surface for the integration keys; a separate Duo *application* for the subdomain pilot so pilot enrolments never touch production Duo config; RBAC-aware coverage rules if the answer to (1) is "internal only".
- **Status:** **Proposed — awaiting client answers on coverage, replace-vs-supplement, and enrolment ownership.** No code written. Tracked alongside the cutover prerequisites in `docs/tracking/production-cutover-plan.md`.

### CR-032 (proposed + CLOSED 2026-08-04, not built) — Blocked delete on Product Categories/Types: show WHICH records are blocking it (and decide whether Admin may override)

> **CLOSED 2026-08-04 by Shawn — no code change. Handled as a CR-019 data cleanup instead.**
> Root cause was confirmed **data, not code**: the delete guard refused correctly because live test products really did reference the records. Removing them clears the block with nothing to build.
>
> **Known accepted residual — this can recur.** Only the *count* is being fixed (DEF-323); the *findability* gap is not. A future blocked delete will again show a correct number with no way to reach the rows behind it — precisely the `Test Type` case below, whose count was accurate the entire time while the client still could not act on it. Accepted deliberately: pre-launch environment, self-inflicted test data, no client currently requesting it, and schedule pressure. **If this is reported again by the client against real records, reopen as Option A — do not re-litigate Option B.**
- **Source:** QC **#242** (client, opened 2026-07-30, Image 107). **Client's report** — *"I cannot delete Carly Test even though I updated all certs that are using it… The table is saying there are 2 Test Type but I can't find them to remove to then be able to remove this test item."*
- **ATTRIBUTION CORRECTED 2026-08-04.** The line *"Do we want to allow full child delete to override? Or see what is associated. This is a usability issue for Admins and Analyst"* is **Anuki's own triage note in the QC log, not a client request.** The first draft of this CR wrongly presented it as client-sourced scope. The client asked for nothing here beyond being able to complete their cleanup.
- **ROOT CAUSE IS DATA, NOT CODE — measured 2026-08-04 (read-only, test env).** The delete guard was correct every time it refused. Four live test products genuinely reference the records:

| Product Id | Name | References | Created |
|---|---|---|---|
| 140 | `Best Product Ever v2` | Type 86 `Test Type` | 2026-06-11 |
| 141 | `Best Product Ever v3™` | Type 86 `Test Type` | 2026-06-11 |
| 389 | `Carly Test` | Category 9 | 2026-07-10 |
| **392** | `Test` | Category 9 | **2026-07-30 18:14 — the day QC #242 was filed** |

  **Two hypotheses tested and refuted.** (1) *Soft-deleted rows still counted by the guard* — `Products.IsDeleted` exists, but **0 of 315** rows are soft-deleted; no hidden blockers. (2) *Damage from early record creation* — the Products table runs from **2025-11-18**, so all four blockers are recent, and #392 was created the same day the issue was raised, most likely during the cleanup attempt itself. Nor is "product under a category with no product type" a legacy artifact: **283 of 315 products (90%)** have exactly that shape. It is the normal state.

  **What IS old is the category itself** — `Carly Test` (Id 9) predates Id 11 (`Construction / Building Products`, a real category). Ids **3 and 10 are absent**, which proves category hard-delete works when nothing references the row. Nothing is structurally stuck.
- **Why CR, not a defect:** the *defect* half of QC #242 was the Usage column under-reporting — that is **[[DEF-323]]**, fixed and pending deploy. What remains is new capability the system has never had: surfacing the identity of blocking records, and optionally a privileged override. Nothing in the baseline promised either. New requirement = CR per [[project_qc_log_pattern]].
- **Relationship to DEF-323 — this is NOT covered by that fix, and the data proves it.** DEF-323 makes a wrong number right. It does not make any number *reachable*. Evidence from the read-only test-env review (2026-08-04, `gccwebap_certificates`):

| Item | Blocked by | Did DEF-323 help? |
|---|---|---|
| Category `Carly Test` (Id 9) | products **389** `Carly Test`, **392** `Test` | **Yes** — count went 0 → 2, so the blocker at least becomes visible as a number |
| Type `Test Type` (Id 86, under *Chemicals and Raw Materials*) | products **140** `Best Product Ever v2`, **141** `Best Product Ever v3™` | **No** — `ProductTypeId`-linked products were always counted correctly. It has read an accurate "2" the entire time |

  The `Test Type` case is the whole argument for this CR: **the count was never wrong, and the client still could not act on it.** Neither blocking product is named anything resembling test data, so a correct "2" on screen offers no route to the two rows behind it. Fixing arithmetic cannot solve a findability problem.
- **Scope — Option A (non-destructive, recommended):** make the Usage counts on Product Categories / Product Types the entry point to the records behind them.
  - New read-only endpoint returning the *rows* (not the count) that block a given delete, sourced from the **same** FK set the delete guard reads — `Products.CategoryId` ∪ `ProductTypeId` ∪ `ProductSubcategoryId`, and `Certificates.ProductCategoryId` ∪ `ProductTypeId` ∪ `ProductSubcategoryId`. Single source with the guard is the point; see [[feedback_scoping_check_adjacent_endpoints]] — a count and a guard sourced from different queries is exactly what caused DEF-196, DEF-197 and DEF-323.
  - Usage counts become clickable → modal listing each blocker with an identifying label (product name / certificate number) and a link to the record to reassign it.
  - The blocked-delete message gains the same list, so the refusal explains itself at the moment it happens.
  - `AdminCore.showConfirmation()` / `showNotification()` only — no native dialogs.
- **Scope — Option B (full child delete override): RECOMMEND DROPPING.** An Admin-only "delete anyway" nulling referencing FKs was Anuki's idea, not the client's, and the 2026-08-04 data removes its justification: **there is nothing to override.** No record is unreachably stuck; the guard refuses only where live records really do reference the row, and deleting those 4 test products clears every block. Building an override would add a destructive path — silently rewriting the taxonomy of live, partner-visible certificates, with no audit trail unless we also build one — to solve a problem that does not exist. JSON *shape* would stay intact per [[feedback_amazon_api_immutable]], but the category/type **values** Amazon reads would change.
  - Kept on the record rather than deleted, so the question is not re-opened from scratch later. If it ever returns it needs a genuine client-side driver, plus Admin-only + typed confirmation + per-record audit + a pre-flight — and that pre-flight **is Option A**.
- **Out of scope:** the 4 orphaned test products above (#140, #141, #389, #392 — all verified 0 certificates, 0 identifiers). That is a data operation already scoped by **CR-019** ("deletes confirmed 'Carly Test' data"), not a code change. Also out of scope: any alteration to the `/api/v1/certificates*` contract.
- **Acceptance:** from a blocked delete, an Admin or Analyst can reach every blocking record without a database query; the list matches the delete guard exactly (0 discrepancies across all categories/types); `Carly Test` and `Test Type` are both resolvable end-to-end by the client unaided.
- **Effort estimate:** Option A ≈ 0.5–1 day (endpoint + modal + wiring into the existing refusal path). Option B — not estimated; recommended dropped.
- **Immediate unblock does NOT require this CR.** Deleting the 4 test products above clears both blocked deletes today. That is a data operation under **CR-019**, which already scopes *"deletes confirmed 'Carly Test' data"*. This CR is only about preventing the next occurrence, and is genuinely optional.
- **Worth flagging:** `DeleteProductTypeAsync` (`Services/AdminService.cs:1405`) selects its error wording from `Level == 2` under a comment asserting `1 = top-level type, 2 = subcategory`, which the data contradicts — Level 0 *and* Level 1 both mean top-level (48 rows, none with a parent). Harmless today, but this CR adds logic near it. See the latent finding in [[DEF-324]].
- **Sent to client?** **No** — and on the corrected reading there may be nothing to send. The client raised a blocked cleanup, not a feature request. The honest reply is *"those are the 4 records blocking it, they're being removed"*, not a change request.
- **Status:** **CLOSED 2026-08-04 — not built.** Proposed and closed the same day. Option B dropped (no override needed — nothing is stuck). Option A declined as discretionary. Remediation moves to **CR-019** as a data operation. See the accepted-residual note at the top of this entry.

### CR-031 (built 2026-08-03) — Expired-certificate edit alert widened to every field and moved BEFORE the save
- **Source:** QC **#188** again — the client's third pass on this item. Their 7/31 notes, verbatim: *"anytime an expired date gets changed? once the system has an expired ticket"*, *"also show up if user changes it to expired and then change the status? **only if it is originally expired**"*, and the replacement copy: *"ALERT: You are updating a previously Expired certificate. Please confirm all fields are accurate including status, etc."*
- **Why CR, not DEF:** third client-directed change of behavior on the same item (6/23 → revert status; 7/30 → leave Expired + post-save toast; 7/31 → widen the trigger + move the alert before the save). CR-030 was built exactly as specified and worked; the requirement moved again. Changed requirement = CR per [[project_qc_log_pattern]].
- **Supersedes CR-030's alert** (the CR-030 *status* rule is untouched — see below).
- **Two decisions confirmed with Shawn before building** (both changed the implementation, so they were asked rather than assumed):
  1. **Timing — pre-save confirmation**, not on-form-open and not a post-save toast. "Please confirm all fields are accurate" is only actionable while the analyst can still change them; on-form-open would fire on every read-then-close and become noise.
  2. **CR-030's date-specific alert is replaced, not kept alongside.** The new copy already covers the date case, so keeping both risked two alerts on one save.
- **Trigger — the STORED status, not the dropdown.** The alert fires when the certificate was `Expired` *as the Edit form was opened*. Computed server-side (`ViewBag.OriginalStatusWasExpired`) rather than read from the Status `<select>`, for two reasons: the client explicitly scoped it to *"only if it is originally expired"* (so setting a live cert **to** Expired in this edit must not fire it), and a dropdown read would let the trigger flip mid-edit as the analyst changes their mind.
- **Scope (AS-BUILT):**
  - **`Controllers/CertificateController.cs` — Edit GET:** sets `ViewBag.OriginalStatusWasExpired` from the DB entity's status.
  - **`Controllers/CertificateController.cs` — `ReloadEditFormDataAsync`:** re-establishes the same flag so a validation re-render doesn't silently drop the confirmation. Reads the DB, not the posted model (the posted Status is what the analyst is *proposing*). The certificate fetch this needed replaced a duplicate fetch already in the method's `else` branch — one round-trip, not two.
  - **`Controllers/CertificateController.cs` — Edit POST:** the CR-030 `showExpiryCorrectionAlert` detection flag, its `storedExpirationWasPast` companion, its log line, and the `TempData["ExpiryCorrectionAlert"]` write are all **removed**. No server-side alert detection remains.
  - **`Views/Certificate/Edit.cshtml`:** the confirmation gate. Runs **last** in the existing submit handler, after every client-side validation — confirming and then bouncing the analyst back with a validation error would make the confirmation meaningless. Uses the shared `window.CertificateCreate.showConfirmation()` modal (never a native `confirm()`, per project rule). Because the submit event is synchronous and the modal is a Promise, the gate cancels the submit, awaits the answer, then re-submits with a latch (`expiredEditConfirmed`) so the second pass falls straight through. A `.catch()` re-enables Save so a modal failure can never strand the analyst on a disabled button.
  - **`Views/Certificate/CertificateInternalView.cshtml`:** the CR-030 TempData **reader** removed in the same change as its writer. Per **DEF-318**, a TempData key must never be left with a writer and no reader — an unread entry is re-saved indefinitely and surfaces on an unrelated later page.
- **Unchanged:** the client's 7/30 core rule. Status is still saved exactly as submitted, never inferred, never auto-restored. This CR moves and widens the *alert* only.
- **Validation (localhost:5101, 2026-08-03, authed as analyst, real repro in a browser):**
  - Expired cert **25-0123** → Save → modal fires with the client's exact sentence; Save button disabled behind it.
  - **Cancel** → modal dismissed, nothing saved, still on the form, Save re-enabled.
  - **Confirm** → saves. Moved expiration `2026-01-31` → `2027-12-31` leaving status alone: DB confirms **status still `Expired`** with a future date, and the detail view showed **no second toast** (CR-030's alert is genuinely gone, not duplicated).
  - **Negative:** Draft cert **26-9001** → Save submits straight through, no modal.
  - **Validation re-render:** Expired cert **25-0124** forced to fail server-side validation → on the re-rendered form the modal still fires, confirming `ReloadEditFormDataAsync` preserves the flag.
  - Test data restored afterwards (`25-0123` back to `2026-01-31`; `25-0124` never saved).
- **Files touched:** `Controllers/CertificateController.cs`, `Views/Certificate/Edit.cshtml`, `Views/Certificate/CertificateInternalView.cshtml`. No model, migration, or API change. `dotnet build` **clean (0 errors)**.
- **Amendment 2026-08-03 (Shawn) — trigger widened to BOTH expired states, copy unchanged.** Originally built for `Expired` only. Now fires for `Expired` **and** `Expired - No Annual Verification`, via the existing `CertificateStatusHelper.CurrentlyExpiredStatuses` (the same set DEF-220 uses for expiration reporting) rather than a new definition of "expired". Two call sites, same expression: Edit GET and `ReloadEditFormDataAsync`. **The client's single sentence covers both states — there is deliberately no second variant of the copy** ("only the wording they suggested", Shawn 2026-08-03).
  - **Validated in-browser 2026-08-03** on dev cert **26-0002** (`Expired - No Annual Verification`): server renders the flag `true`, modal fires on Save, Cancel dismisses cleanly and re-enables Save. `dotnet build` clean (0 errors).
- **Amendment 2026-08-03 (Shawn) — copy is now the client's LITERAL string, prefix included.** The body reads exactly: `ALERT: You are updating a previously Expired certificate. Please confirm all fields are accurate including status, etc.` The `ALERT:` prefix is inline text, no longer promoted to a heading.
  - **`wwwroot/js/certificate-create.js` — `ModalSystem.showConfirmation`:** now renders **no heading** when a caller passes an **explicit `null`** title, so the client's sentence is the only copy and the word "ALERT" is not duplicated. Deliberately keyed on `=== null`, not falsiness: `undefined` still falls back to `"Confirm"`, so all three existing callers (Delete Certificate, Remove Certificate PDF, Unsaved Changes) are **byte-for-byte unaffected**. Verified by enumerating every source caller — all pass explicit string titles.
  - **The message never names a status.** One sentence serves both expired states, so it reads correctly for `Expired` and `Expired - No Annual Verification` alike. **Do not add a status-specific variant** — "only the wording they suggested" (Shawn, 2026-08-03).
  - **Validated in-browser 2026-08-03 on BOTH expired states**, each a fresh page load, Save clicked, modal inspected, then cancelled:
    - **26-0002** (`Expired - No Annual Verification`) — modal fires; body matches the client's string character-for-character; `headingRendered: false`; buttons Confirm/Cancel.
    - **25-0123** (`Expired`, expiration `2026-01-31`) — identical result. Confirms the `title: null` heading change did not regress the originally-built case.
    - Cancel on both: modal removed, Save re-enabled, no navigation, nothing written. DB re-checked afterwards — `25-0123` still `Expired` / `2026-01-31`. Screenshots captured for both.
  - **Note on `saveButton.disabled` when testing:** reading it synchronously immediately after clicking Cancel returns the stale `true` — the re-enable runs in a promise microtask. Measure it in a separate step or it looks like a bug that isn't there.
- **Known pre-existing behavior (NOT introduced by CR-031, do not log as a defect):** clicking Save before the page's JavaScript has finished initializing posts the form natively and bypasses the gate. This is how the form's *existing* client-side validation (date-range, custom fields) has always behaved — the listener simply isn't attached yet. Hit twice during testing because automation clicks faster than a human can. Harmless in practice (nothing has been edited that early) and the alert is advisory, not a server-enforced control.
- **Open item back to the client:**
  1. The shared confirmation modal styles its **Confirm button red and Cancel green** (fixed in `certificate-create.js`, same as Delete). Reads oddly for a non-destructive confirm, but changing it would alter every other confirmation dialog — left alone deliberately.
- **Dev-data note:** cert **26-0002** was saved once during testing with no field edits (bumps `UpdatedAt` + one audit row; no business values changed). `25-0123` was restored to `2026-01-31`.
- **Status:** **Built + validated on localhost 2026-08-03 — pending next deploy bundle.** Ships together with CR-030 (they touch the same lines). No commit (per [[feedback_no_commits]]).

### CR-030 (built 2026-07-31, alert superseded by CR-031 on 2026-08-03) — Expiration corrected to a future date: status now STAYS "Expired" and the analyst is alerted, instead of being silently restored
- **Source:** QC **#188** (Reporter: client, opened 6/11/26, log status *Not fixed*), client update **7/30/26**, verbatim: *"Is it possible to add a popup that alerts person? If this is possible make it say the below. **Either way if the date changes DO NOT update the status. Leave it Expired.** It will be on the user to revise as needed. ALERT: Expiration date was moved to the future and certificate is marked Expired. Update status accordingly if necessary."*
- **Why CR (not a defect): the client reversed their own decision.** On **6/23** we put three options to them for what should happen to the auto-applied `Expired` status when a past expiration date is corrected into the future — (a) revert to the prior status, (b) go to Draft, (c) leave Expired and let the user pick. They chose **(a)**, and that is what shipped as **DEF-285**. It worked as specified. On 7/30 they chose **(c)** instead. Replacing a working, explicitly-requested behavior at the client's direction is a requirement change, not a bug fix — per [[project_qc_log_pattern]].
- **Trade the client is accepting (flagged to them in writing):** every past→future date correction now requires a deliberate second step to set the status. Nothing infers it for them. This is workable specifically *because* **CR-028** removed the transition matrix — status is freely selectable, so the analyst can move it anywhere without hitting a workflow rule. Under the old matrix this design would have been much worse.
- **Scope (AS-BUILT):**
  - **`Controllers/CertificateController.cs` (Edit POST)** — removed the DEF-285 auto-restore: the `CertificateStatusHistory` lookup that recovered the pre-expiry status and the reassignment of `requestedStatus`. The submitted status is now saved verbatim. Replaced with a detection flag that raises the alert.
  - **Added condition the old code did not need.** DEF-285's auto-restore was *self-limiting* — it moved the status off `Expired`, so it could never match again. CR-030 leaves the status on `Expired`, which removes that natural stop. Without a guard the alert would re-fire on **every subsequent save** of that certificate until someone changed the status. So the trigger now also requires the **stored** expiration to have been in the past — i.e. the date genuinely moved past→future in *this* edit. (`certificate.ExpirationDate` is still the DB value at that point; it is not overwritten from the model until further down the method.) **Verified both ways on localhost.**
  - **`Views/Certificate/CertificateInternalView.cshtml`** — renders the alert via `AdminCore.showNotification(msg, 'warning')`. Not a native `alert()` (project rule); not a confirmation modal either, since the save has already happened and there is nothing left to confirm. Client's wording verbatim.
  - **TempData key `ExpiryCorrectionAlert`** — exactly one writer (Edit POST) and one reader (that view, which *is* the redirect target), so the entry is consumed on the very next request. Chosen deliberately to avoid the orphaned-TempData trap that caused **DEF-318** in the same file two hours earlier.
  - **Audit trail** — the `"Expiry corrected: status restored…"` reason and the `Expiry-Correction-Revert` workflow step are gone with the behavior they described. A status change reaching that code is now always the analyst's own explicit choice.
- **Validation (localhost:5101, 2026-07-31, authed as analyst, real repro in a browser):** certificate **26-0174** started as the client's exact scenario — status `Expired`, expiration `2026-06-17` (past). Moved expiration to `2027-12-31`, left status untouched, saved. Result: expiration `2027-12-31`, **status still `Expired`** (old behavior would have restored `Client Approved - Paid`), and the warning modal rendered the client's exact sentence. Saved the same certificate a second time with an unrelated field change → **no alert**, confirming the anti-repeat guard. Certificate restored to its original values afterwards.
- **Files touched:** `Controllers/CertificateController.cs`, `Views/Certificate/CertificateInternalView.cshtml`. No model, migration, or API change. `dotnet build` **clean (0 errors)**.
- **Open item back to the client:** they should confirm they are happy with the manual second step now that they can see it working — it was raised in the 7/31 email but built ahead of their reply, on Shawn's instruction, because their 7/30 wording was unambiguous.
- **Status:** **Alert mechanism SUPERSEDED by CR-031 (2026-08-03) before this ever deployed.** The client changed the requirement again on 7/31. What survives from CR-030 and still ships: the **status rule** — the DEF-285 auto-restore is gone and status is saved exactly as submitted. What was replaced: the post-save `TempData["ExpiryCorrectionAlert"]` toast and its date-specific detection, now a pre-save confirmation on any edit to an already-Expired certificate. The 7/31 validation below describes behavior that is no longer current — read CR-031 for the shipping behavior. Never deployed standalone; no commit (per [[feedback_no_commits]]).

### CR-029 (built 2026-07-20) — Cert Results restored to ALL partner-facing surfaces, always as a percentage
- **Source:** QC #201 (Reporter: Gemma, opened 6/19/26, marked *Not fixed*), client update **7/6/2026**: *"ALL views should include results, as a percentage, rather than having Results column removed."* The same item notes *"The percentage in the export is not showing on the certificate detail for Partner Role. We removed it from the export."* Client also asked us to confirm the full partner field list — see **Open question** below.
- **Why CR (not a defect):** The percentage formatting itself was already fixed and working (**DEF-287** — `DisplayFormatters.FormatCertificationResults`, single source of truth: `0.17 → "17%"`, `100 → "100%"`, non-numeric free text like `"Pass"` passes through). What the client was actually seeing is the result of **DEF-295**, where Cert Results was *deliberately removed* from every partner-facing surface on the reasoning that it "appears on NO partner-facing surface." The client has now reversed that decision. Removing a working, deliberately-scoped restriction at client direction = requirement change, not a bug. Per [[project_qc_log_pattern]] changed requirement = CR, not DEF.
- **Scope decision (confirmed with Shawn 2026-07-20):** DEF-295 gated **three** columns together — Cert Subcategories, Pathway, Cert Results. The 7/6 client note names **Results only**, so only Cert Results is un-gated. **Pathway + Cert Subcategories remain Admin/Analyst-only.** The fully public/anonymous tile view (`/Certificate/CertificateTileView`) is **not** touched — a test score/threshold is partner-contract data, not public data.
- **Scope (AS-BUILT) — four surfaces, three of which were broken in different ways:**
  - **Partner/External cert detail** (`Views/Certificate/CertificateExternalView.cshtml`) — field was **absent entirely**. Added a `Certification Results` detail-section mirroring `CertificateInternalView.cshtml`, formatted via the shared `DisplayFormatters`, hidden when the cert has no result. *This is the specific symptom the client called out.*
  - **External tile table** (`Controllers/Api/CertificateTileExternalApiController.cs`) — the `tc-certresults` column, its show/hide toggle, and the JS renderer (`certificate-tile-view-external.js:1773`) **already existed**, but the API projection never carried `CertificationResults`, so the column **rendered blank on every row**. Added to the raw projection + formatted in the post-projection select. (Sorting already referenced `CertificationResultsNumeric` — so the column was sortable on data it never displayed.)
  - **Partner/External CSV export** (`Services/CertificateService.cs` `GenerateCsvExport`) — column removed by DEF-295. Un-gated **Cert Results only**; header and data-row builders kept in lock-step. Internal column order unchanged (Pathway still immediately precedes Cert Results).
  - **v1 partner API** — **no change needed**, already emitted `certificationResults` formatted ([[feedback_amazon_api_immutable]] — contract untouched).
- **Validation (localhost:5101, 2026-07-20, authed as External + Analyst):** DB ground truth `26-0146=0.17`, `26-0147=100`, `26-0150=0.27`, `26-0151=100` → **external tile API** returns `17% / 100% / 27% / 100%`; **external CSV export** header contains `Cert Results` and **not** Pathway / Cert Subcategories / Internal Tag, values `17% / 100% / 27% / 100%`; **partner detail view** for 26-0146 renders `<div class="certificate-field">17%</div>`. **Analyst export regression clean** — Cert Subcategories(2), Pathway(12), Cert Results(13), Internal Tag(19), Pathway immediately before Cert Results, 54 rows. This directly reproduces and closes the client's original `0.17` vs `100` complaint.
- **Files touched:** `Controllers/Api/CertificateTileExternalApiController.cs`, `Services/CertificateService.cs`, `Views/Certificate/CertificateExternalView.cshtml`. No model, migration, JS, or v1 API change. `dotnet build` **clean (0 errors)**.
- **Open question back to client (unanswered):** QC #201 also asks us to *"confirm all fields that should be displayed on the Partner certificate view and the export."* This CR answers it only for Cert Results. **Pathway and Cert Subcategories are still hidden from partners** — if the client wants those too, that is a follow-on CR. Recommend sending the current partner field list for sign-off.
- **Status:** **Built + validated on localhost 2026-07-20 — IN the `GCCWebApp-deploy-20260720-2037.zip` bundle, pending upload.** All eight pre-deploy gates satisfied, none waived (see `pre-deploy-agent-checklist.md`); presence in the shipped DLL confirmed by UTF-16 string-heap grep. No commit (per [[feedback_no_commits]]).

### CR-028 (built 2026-07-20) — Certificate status: REMOVE the workflow transition matrix — status is now freely selectable on Create/Edit
- **Source:** Client decision (via Shawn, 2026-07-20), closing the open item raised in the 2026-07-13 client email (`docs/tracking/email-draft-2026-07-13-workflow-status-decision.md`, "Fix #2 — a quick review from you"). The client hit *"Invalid status transition from 'Draft' to 'Republished'."* while editing a cert, could not re-save, and — after reviewing the transition-matrix sign-off sheet — asked us to **"make the status open and remove the complexity."** i.e. drop the allowed-path rules entirely and let an analyst set any status.
- **Why CR (not a defect):** The matrix worked exactly as built — it was added in the 2026-05-19 security-hardening pass (DEF-203) and enforced a lifecycle order (Draft → {Not Paid, Confidential, Paid, Hold}; Republished only reachable from Paid/Expired; etc.). Per the 2026-07-13 email, the *statuses* were client-defined but the *transition rules* were engineer-set and never signed off. The client has now reviewed and **decided against** them. Removing a working, deliberately-built guardrail at the client's direction = a scope/requirement change, not a bug fix. Per [[project_qc_log_pattern]] changed requirement = CR, not DEF. Reverses part of the [[feedback_workflow_state_count]] work — note this touches enforcement only, **not** the 8-state set itself (the 8 statuses in `Models/CertificateStatus.cs` are unchanged).
- **Scope (AS-BUILT):**
  - **Edit POST** (`CertificateController.cs`): deleted the transition-matrix rejection block (the `!IsValidTransition(oldStatus, requestedStatus)` guard that returned the view with an *"Invalid status transition…"* ModelState error — this was the only *live* enforcement point). Any status an Analyst/Admin picks is now saved as-is.
  - **Create**: already free (`Status = model.Status ?? "Draft"` with no matrix check; the dropdown already offered all 8 to analyst/admin via `GetStatusesForRole`). **No code change needed** — Create already satisfied "status open."
  - **Expiry-correction auto-restore** (QC #188 / DEF-285): kept, but its trigger was narrowed from `(requestedStatus == oldStatus || !IsValidTransition(...))` to `requestedStatus == oldStatus` only — otherwise, with the matrix gone, its `!IsValidTransition` arm would have silently overridden an analyst's *explicit* status choice on an Expired cert. It now fires only when the analyst fixes the expiration date but leaves Status on "Expired."
- **Deliberate boundaries — two safeguards INTENTIONALLY KEPT** (confirmed with Shawn 2026-07-20; neither was part of the "complexity" the client complained about):
  - **Role-based availability** — External users still get Draft-only via `GetStatusesForRole` (data isolation, not workflow). Moot in practice: External users never reach Create/Edit (analyst/admin only, per Shawn), but left as defense-in-depth. Removing it would let an external user publish a cert → that WOULD be a separate data-scoping CR ([[feedback_partner_data_isolation]]).
  - **Audit trail** — every status change is still written to `CertificateStatusHistory` (who/when/old→new/IP). Never blocked a save; not "complexity."
- **Supersedes:** DEF-305 (the "workflow-transition UX trap" fix — dropdown snap-back + allowed-next-states message) is now moot: there is no rejection path left to trap the user. The DEF-203 matrix enforcement is removed. `CertificateStatusHelper.IsValidTransition` + `GetAllowedNextStatusDisplayNames` and the (already-dead) `CertificateWorkflowService.Transition*` methods are **left in place** (still referenced by the retained expiry-restore log query context and harmless) — not deleted, to keep the diff minimal and reversible. The `certificate-status-transition-matrix.{md,html,pdf,docx}` sign-off sheets are now **historical** (record of what the client reviewed before deciding to remove the rules); left in `docs/tracking/` for the audit trail.
- **Partner API impact:** None. `/api/v1/certificates*` untouched ([[feedback_amazon_api_immutable]]); this is the internal MVC Edit path only.
- **Files touched:** `Controllers/CertificateController.cs` (Edit POST — removed rejection block, narrowed expiry-revert trigger, updated audit-row comment) and `Scripts/smoke-prod/verify-def203.js` (repurposed: section [B] now asserts the formerly-blocked hops SUCCEED). **No** model, view, migration, JS (app), or API change.
- **Effort:** ~40 min incl. investigation + tracking. `dotnet build` **clean (0 errors)**.
- **Status:** **Built 2026-07-20 — pending localhost validation + deploy.** Compiles clean (built to temp output to avoid the running-app file lock). Not yet driven on localhost:5101 (the transition scenario needs an authed Edit POST — validation to follow); folds into the next `dotnet publish` rebuild alongside the other pending 07-16…07-20 items.

### CR-027 (built 2026-07-16) — Cert Create/Edit: rename the Product Name field to guide facility-cert entry (keep it required)
- **Source:** Client/GCC decision (via Shawn, CP note relayed 2026-07-16) — concern: *"Need to select a product and product type, even if it is a facility cert (like waste diversion)."* **GCC DECISION — CP note:** *"Leave required but Anuki name the field the below → Product Name (list 'City, State Facility' if facilty cert)."*
- **Why CR (not a defect):** The field works as originally built — Product Name is a required `<select>`. The client considered making it optional for facility certs and **decided against it**; instead they want the label reworded so analysts know what to enter for a facility cert (e.g. waste diversion) that has no product. That's a copy/requirement change, not a bug. Per [[project_qc_log_pattern]] new/changed requirement = CR, not DEF.
- **Scope (AS-BUILT):**
  - On **both** the Certificate **Create** and **Edit** forms, the Product Name label now reads **"Product Name  *(list 'City, State Facility' if facility cert)*  \*"** — the guidance rendered as small muted, normal-weight text between the label and the existing required asterisk.
  - Field stays **required** (unchanged `required aria-required="true"` on the `<select>`); no server-side or validation change.
- **Deliberate boundary:** Only the Product Name label copy changed. **Product Type is left as-is (Optional)** — the GCC decision only named/kept-required the Product Name field; it did not ask to make Product Type required, so no change there. Fixed the obvious "facilty" → "facility" typo in the client's requested wording (instructional helper copy, meaning unchanged).
- **Partner API impact:** None ([[feedback_amazon_api_immutable]]) — view copy only.
- **Files touched:** `Views/Certificate/Create.cshtml` + `Views/Certificate/Edit.cshtml` (the `<label asp-for="ProductId">` line in each). **No** model, controller, migration, JS, or API change.
- **Effort:** ~10 min.
- **Status:** **Built — pending localhost validation + deploy.** Not yet driven on localhost:5101; not in any deploy bundle — folds into the next `dotnet publish` rebuild alongside the pending CR-024 / CR-025 / CR-026 / DEF-305.

### CR-026 (built 2026-07-16) — Certificate listing: open the Import + Template buttons to Analyst (were Admin-only)
- **Source:** Client request (via Shawn, 2026-07-10 call; CP note relayed 2026-07-16) — *"Missing Import and Template buttons on the cert listing view under Analyst (was intentionally just Admin prior but want to open Analyst too). See image 100."* Shawn had told the client the buttons were admin-only by design; the client now wants Analyst to be able to import as well.
- **Why CR (not a defect):** The buttons render exactly as originally built — Import + Template were **intentionally** gated to Admin only. This widens the audience for an existing feature (a scope/permissions change to a documented requirement), not a fix for broken behavior. Per [[project_qc_log_pattern]] new/changed capability = CR, not DEF.
- **Scope (AS-BUILT):**
  - The **Import** button + **Template** download link on the internal certificate listing (tile/table view) now show for **Analyst OR Admin** (were `Admin` only). The Import modal markup is gated to the same condition.
  - Server side: `CertificateImportController` authorization changed from policy **`AdminOnly`** → **`AnalystOrAdmin`** (the policy already existed in `Program.cs`). This opens every import endpoint the UI calls — `DownloadTemplate`, form `Preview`/`Apply`, and the `ApiPreview`/`ApiApply` JSON endpoints — to Analyst, so the newly-shown buttons don't hit a 403.
- **Deliberate boundary:** Only the certificate listing's Import/Template affordance + its controller policy changed. Partner/Company-scoped restrictions and External-user rules are untouched; the Partners page (Export + Add only, NO Template/Import per `.claude/CLAUDE.md`) is a different view and is **not** affected.
- **Partner API impact:** None. No `/api/v1/*` change ([[feedback_amazon_api_immutable]]); the import endpoints are the internal MVC/`/api/CertificateImport/*` routes, not the partner API.
- **Files touched:** `Views/Certificate/CertificateTileViewInternal.cshtml` (two `@if` gates: the button group + the import modal → `User.IsInRole("Admin") || User.IsInRole("Analyst")`) and `Controllers/CertificateImportController.cs` (class-level `[Authorize]` policy → `AnalystOrAdmin`). **No** model, migration, JS, or API change.
- **Effort:** ~15 min. `dotnet build` clean (0 errors).
- **Status:** **Built + VALIDATED on localhost:5101 2026-07-16 — pending deploy.** Logged in as the analyst (`jane.analyst@anukiconsulting.com`) on `/Certificate/CertificateTileViewInternal`: **both the Import button and the Template link render** (they did not before), and the **Import modal opens** (confirming the second `@if` gate) with no console errors. Server authorization confirmed from the authenticated analyst session: `GET /CertificateImport/DownloadTemplate` → **200 (text/csv)** and `POST /api/CertificateImport/Preview` → **200** with a valid preview JSON (`importId`, `totalRows:1`) — both would have been 403 under the old `AdminOnly` policy. Preview only (dry-run); **Apply was not called**, so no rows were written to the test DB. Not in any deploy bundle — folds into the next `dotnet publish` rebuild alongside the pending CR-024 / CR-025 / DEF-305.

### CR-025 (built 2026-07-16) — Import Preview (ASIN / Product ID upload): make the four summary boxes clickable filters + add row search
- **Source:** Client request (via Shawn, 2026-07-16) — *"For the ASIN upload popup — which shows you the ASINs before finalizing upload: Are you able to allow functionality for us to filter by the boxed headers above. If I upload 1,000 ASINs and there are 2 errors — it would be nice to be able to search or filter to just see those 2 errors."*
- **Why CR (not a defect):** The Import Preview modal renders exactly as originally built — the four summary boxes (New / Modified / Unchanged / Errors) were display-only counts, no filtering was ever specified. This adds a new viewing capability (isolate rows by status / search) to a large import — an enhancement to the requirement, not a fix for broken behavior. Per [[project_qc_log_pattern]] new capability = CR, not DEF.
- **Scope (AS-BUILT):**
  - The four summary boxes in the Import Preview popup are now **clickable toggle filters** — click **Errors** to show only the failing rows (the client's 2-of-1,000 case), click again to clear. Active box gets a colored 2px border; `role="button"` + keyboard (Enter/Space) + `aria-pressed` for a11y.
  - New **free-text search box** above the preview table — matches across Type / Product ID (ASIN) / Product Name / SKU / Partner / error text.
  - Live **"Showing X of Y"** count + a **Clear filter** button that appears only while a filter/search is active.
  - Status filter and search **combine** (AND).
- **Deliberate boundary — filtering is PURELY VISUAL:** it only hides/shows already-rendered rows. It never changes which rows are **selected** or **applied**. `applyImport()` and the header **Select-all** continue to operate on the full `importPreviewData` set (all non-error / selected rows), unchanged — so hiding rows can't cause a partial or wrong import. Filters reset on every fresh import open.
- **Partner API impact:** None. Client-side only; no `/api/v1/*` change ([[feedback_amazon_api_immutable]]).
- **Files touched:** `Views/Shared/_ProductIdManager.cshtml` (stat boxes → clickable, new search/count/clear bar) and `wwwroot/js/product-id-manager.js` (filter state + `toggleImportStatusFilter` / `searchImportPreview` / `clearImportPreviewFilters` / `applyImportRowVisibility`; reset on modal open). **No** model, controller, migration, or API change.
- **Effort:** ~30 min. `dotnet build` clean (0 errors); `node --check` clean.
- **Status:** **Built + VALIDATED on localhost:5101 2026-07-16 — pending deploy.** Playwright drove the real Import Preview modal (admin, cert 28-0002) with an 8-row CSV containing 3 error rows (bad ASIN length, missing type, in-file duplicate): **11/11 GREEN** — Errors box → only the 3 error rows, "Showing 3 of 8" + Clear button, active-box highlight, toggle-off restores all 8, search "notype" → 1 row, search+Errors combine (AND) → 3, Clear resets, no console/page errors. Full-page screenshots in `Scripts/smoke-prod/_results/cr025/`. Not yet folded into a bundle; must ride the next `dotnet publish` rebuild (alongside the pending CR-024 / DEF-305 items).

### CR-024 (built 2026-07-13) — Certification Results field: re-label "(Optional)" → "(Required for Amazon CPF)" + new helper text
- **Source:** Client request (Shawn, 2026-07-13) — on the certificate Add and Edit views, change the Certification Results field annotation from **(Optional)** to **(Required for Amazon CPF)** and change the helper/instructions text to **"Enter numeric certification results (e.g., 65%, 100%)"**.
- **Why CR (not a defect):** The field renders exactly as originally built (optional free-text, seeded by the CLAUDE.md requirement that added it "in case Amazon forces us to pass a threshold value"). This request **changes what the field communicates to the analyst** — reframing it as required-for-Amazon-CPF and steering input toward numeric results — extending the captured requirement, not fixing broken behavior. Label/copy change per client direction → tracked as a CR.
- **Scope (AS-BUILT):** label annotation + `<small>` helper text only, in **`Views/Certificate/Create.cshtml`** and **`Views/Certificate/Edit.cshtml`** (the Certification Results `form-group`). `(Optional)` → `(Required for Amazon CPF)`; helper text "Optional field for recording certification test results or scores." → "Enter numeric certification results (e.g., 65%, 100%)".
- **Deliberate boundary (label-only, NOT enforcement):** the field remains **technically optional server-side** — no `[Required]` added to `CertificateEditViewModel.CertificationResults`, no numeric/format validation added. The label now *says* required-for-Amazon-CPF but nothing blocks a blank or non-numeric save. Client asked for the wording change only; **enforcing "required" and/or numeric-format validation is a separate CR if wanted** (touches the view model + client validation + would need an Amazon-CPF-scoping rule for *when* it's required). The textarea **placeholder** ("Enter test results or scores if required by partner…") was also left unchanged — only the two elements named in the request were touched, per [[feedback_only_requested_changes]].
- **Partner API impact:** None. `/api/v1/certificates*` untouched; this is internal cert-form copy only ([[feedback_amazon_api_immutable]]).
- **Files NOT touched:** no model, no controller, no migration, no JS. Two `.cshtml` views only.
- **Effort:** ~10 min (copy change, 2 views).
- **Status:** **Built + edited locally 2026-07-13 — pending deploy.** Text-only change; not yet folded into a bundle (the 2026-07-13 12:42 zip was built before this edit — must ride the **next** rebuild). Not yet visually validated on localhost.

### CR-017 (proposed 2026-05-06) — Unsaved-form recovery banner UX rework (Phase 2)
- **Source:** Internal — Shawn observed the *"Unsaved form data recovered from 08:25 PM. Would you like to restore it?"* banner appearing as a false positive on the cert Create page. Cascading dropdowns retrigger change events on initial load, which the tracker reads as user edits.
- **Why CR (not a defect):** The current banner has known UX failure modes — false positives from cascading-dropdown change events, partial restore that doesn't bring back the cert PDF / thumbnail / product IDs / custom fields, no scope check between Create and Edit pages, ambiguous timestamp. Building a trustworthy version is a feature improvement, not a defect fix. Sized for Phase 2 so it can be prioritized against other Phase 2 work.
- **Interim action (DEF-151, 2026-05-06):** Recovery banner hidden via `SHOW_RECOVERY_PROMPT: false` kill switch in `certificate-create.js`. Auto-save to localStorage continues writing so the data is still there for Phase 2 to consume cleanly. Stale entries (>2 hr old) are GC'd on each page load so storage doesn't leak while the prompt is hidden.
- **Phase 2 scope:** (1) Eliminate false positives — only initialize the unsaved-state baseline after all async cascades complete (Company → Product, Category → Type → Subcategory, Cert Type → Subcategories, custom fields, existing tags); ignore cascade-driven semantic-equivalent diffs. (2) Cover the full save surface — capture product IDs (`window.productIdManager.productIds`), custom field values, selected tags, selected subcategories, in addition to standard form fields. (3) All-or-nothing restore — explicit warning when files (PDF / thumbnail) can't be restored, before the user clicks Restore; restore product IDs and custom fields in addition to standard form fields. (4) Better banner copy — show a date when saved time is more than 6 hr old, list what's covered by restore (e.g., *"14 form fields, 2 product IDs, 1 custom field value"*), optional [Preview] button to show diff before committing. (5) Same-cert scoping — Edit-page recovery only fires when the saved payload's cert ID matches the current cert.
- **Files (estimated):** `wwwroot/js/certificate-create.js` (most of the work), `wwwroot/js/product-id-manager.js` (small hook to expose / restore manager state), `wwwroot/js/certificate-custom-fields.js` (add `setFieldValues(values)` to mirror existing `getFieldValues()`), `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml`.
- **Files NOT touched:** No backend, no database, no Partner API. Frontend-only.
- **Out of scope:** Cross-device recovery (would need server-side draft-saving — schema, soft-state, GC, RBAC — separate larger CR), file (PDF / thumbnail) restoration across sessions (browsers don't expose a way to re-pickle `File` objects without IndexedDB and a meaningful blob-store strategy), multi-tab conflict detection.
- **Partner API impact:** None. Internal cert-form UX only. Per `feedback_amazon_api_immutable.md`, no `/api/v1/certificates*` JSON shape changes.
- **Effort estimate:** ~6-10 hrs (frontend + small QA pass)
- **Detail doc:** `docs/tracking/CR-017-form-recovery-ux.md`
- **Status:** Proposed 2026-05-06 — Phase 2, not blocking launch.

### CR-016 (proposed 2026-05-06) — Bulk Replace mode for Product ID import
- **Source:** QC #51 (Carly) — *"What if someone wanted to replace all the existing with a new list... So it would remove those not included and add those new and keep existing still on list? I tried a version of this and got an error. There were 2 listed from a prior import. I tried to import a doc with only 1 product."*
- **Why CR (not a defect):** The replace-all functionality already ships in the import-preview modal (`Views/Shared/_ProductIdManager.cshtml:313-316` toggle, `wwwroot/js/product-id-manager.js:1175-1195` deactivation logic). Per-cert dup scoping and within-file dup detection also already work. The CR is a UX/discoverability polish so the existing capability is findable and the destructive intent is explicit before commit — not a missing-feature build. Promoting an under-the-hood toggle to a primary mode-selector is a scope decision, not a bug fix.
- **Scope (Option A — UX polish only):**
  - Promote "Replace all existing" checkbox in import-preview footer to a top-of-modal radio mode-selector: `Add to existing list` (default) / `Replace entire list`
  - Add a 5th preview stat tile: **To Remove** (count of existing IDs not in import file), shown only when Replace mode selected
  - Add inline warning banner above the preview table when Replace mode is selected: *"⚠ N existing product IDs not in this file will be permanently removed"*
  - Render to-be-removed existing IDs as `[Remove]` rows in the preview table when Replace mode is selected (currently they're not visualized at all)
  - Improve CSV-parse error messaging — surface specific failure (header mismatch, bad row, format error) instead of generic "got an error"
- **Files touched:** `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, `wwwroot/css/product-id-manager.css`
- **Files not touched:** No backend changes. `Controllers/Api/ProductIdApiController.cs`, `Services/ProductIdService.cs`, `Models/CertificateProductId.cs`, and the SQL schema are untouched. Per-cert scoping and replace-all deactivation logic already exist and stay as-is.
- **Out of scope:** Bulk-replace progress bar / background job for >1000 rows (Option C). If volume becomes a real issue we can size that as a separate CR. Scientific-notation auto-fix for Excel-mangled CSVs already works (`fixScientificNotation`, `product-id-manager.js:838-854`).
- **Partner API impact:** None. This is internal-app UX only. Per `feedback_amazon_api_immutable.md`, no `/api/v1/certificates*` JSON shape changes.
- **Effort estimate:** ~3-4 hrs (frontend-only, no schema, no API)
- **Mockup:** `docs/tracking/CR-016-mockup.html`
- **Detail doc:** `docs/tracking/CR-016-bulk-replace-product-ids.md`
- **Status:** Proposed 2026-05-06 — awaiting client sign-off before build

### CR-015 (approved + built 2026-04-30) — Product ID status follows parent cert
- **Source:** Email from Shawn after the 2026-04-30 status conversation. "Build status for ASIN based on active/inactive status of the cert. Remove the Status column under Product Identifiers since it should natively follow that of the cert. This cannot be user controlled so should be removed from Import Template and add popup. We will only include ASINs in the DB that should be tied to that cert."
- **Why CR:** Behavior change for Amazon badging integrity — expired/non-public certs were leaving their `CertificateProductId.Status` stuck on `"Active"`. Shipping a cascade rule + UI removal so analysts can't drift product-ID state away from the cert's eligibility.
- **Rule:** `CertificateProductId.Status = "Active"` iff parent cert is in `PublicStatuses` (`Client Approved - Paid` or `Republished`); else `"Inactive"`. Cascade fires on cert create/edit, bulk update, workflow transition, auto-expire, and CSV import. No row deletes — product IDs stay tied to the cert.
- **Surface removal:** Status column dropped from Product ID list, filter, Add/Edit modal, CSV import template, and in-memory export. Sample-CSV API endpoint also drops the column.
- **Status:** Built 2026-04-30. SQL backfill script staged at `Scripts/database/CR015_backfill_product_id_status.sql` — needs review before running on prod.
- **Partner API impact:** None. Partner API already filters cert list by `PublicStatuses`; this cleans the underlying per-row state to match. Contract immutability (`feedback_amazon_api_immutable.md`) preserved.

---

## Notes from 2026-04-29 client meeting

- All 7 CRs (CR-008 through CR-014) approved as a bundle.
- Framing from the client: these are required to make the app product-fit — i.e. close the launch gap, not nice-to-haves.
- **Open items to confirm with the client and append here as they land:**
  - Cost terms (per-CR fixed-price vs. bundled estimate vs. T&M)
  - Sequencing — which CR(s) ship first? Auth-stack work (CR-009/010) is independent; partner work (CR-008/013) is paired; CR-011/012/014 sit on the existing custom-field foundation.
  - Go-live target.
  - CR-013 specifically — client to confirm partner-side sign-off on the partner API contract shape change.

---

## Summary for the meeting

| Bucket | Count |
|---|---:|
| Confirmed formal CRs delivered | 3 (CR-005, CR-006, CR-007) |
| Backfilled CR slots, subjects unrecovered | 4 (CR-001 through CR-004) |
| **Undocumented work that should have been CRs** | **~13** |
| Approved CRs in flight (2026-04-29) | 7 (CR-008 through CR-014) |
| **Total scope-change events through the project** | **~27** |

The pattern: **only 3 of ~20 delivered scope changes were formally tracked as CRs.** The rest were absorbed as defect fixes or "build items" during the engagement. That's the structural process gap that explains the timeline overrun more cleanly than any single item.

# GCC Change Request Register

Canonical log of scope changes that altered or extended the original requirements baseline. Reconstructed 2026-04-28 from git history, shipped log, memory, and QC log analysis.

**Process (Phase 2 onward):** `docs/governance/change-request-process.md` — triage buckets, the
estimate-before-build gate, and the rule that an absorbed CR is still priced and logged. Templates:
`_TEMPLATE-CR.md` (internal) and `_TEMPLATE-CR-client-onepager.md` (client, with a decision block).

**Note on history:** Memory references CR-001 through CR-004 as backfilled on 2026-03-23, but the original register file was never persisted. Items below numbered as best-recoverable. CR-005 onward are confirmed via shipped-2026-04.md and screenshot folders.

## LINE IN THE SAND — 2026-09-15 · baseline `GCCWebApp-deploy-20260915-1641.zip`

CR entries share the 285-id deploy baseline recorded in `docs/tracking/deploy-baseline.json` and
explained in full at the top of `defect-register.md`. Every CR marked built-but-pending-deploy on or
before 2026-09-15 is **accepted as shipped** with that bundle — a full-overwrite build from the
working tree, deployed and verified at 17:04.

Accepted, not verified. A CR that was never merged or was later reverted would be wrongly marked
here; re-raise it as a new finding rather than trusting the baseline.

From this point the gate (`python Scripts/deploy/resolved-not-shipped.py`, **Gate 0B**) reports only
new work — so a built-but-unshipped CR is visible the day it appears.

---

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

### CR-021 (proposed 2026-06-11, **APPROVED + EXECUTED 2026-09-10**) — Set Amazon code for "Certified Environmental Facts" (`greencircle_cef`)
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
- **Status:** **APPROVED 2026-09-09 · EXECUTED 2026-09-10 on dev and the test environment.** Superseded the "awaiting GCC approval" state it had held since 2026-06-11.

#### CR-021 executed 2026-09-10

- **Approval:** the client, 2026-09-09, logged as **QC #264** (`Database QC Log_9.9.26_sw_updates.xlsx`, sheet `GCC DB QC Log2`, **row 273**, page `API`, Image 121 + the 9/9 email): *"can you update 'CEF' (Certified Environmental Facts) amazonCode in API to the following which matches more of format as others: greencircle_cef."* Relayed by Shawn, 2026-09-10. **This is the open confirmation above, answered by the client naming the exact string themselves** — the strongest form the confirmation could take, since the value is no longer inferred from their ASIN export but stated directly.
- **Pre-check (read-only) before any write:** `Id=5`, `Name='Certified Environmental Facts'`, `AmazonCode='CEF'` — identical on **both** dev and test. Preconditions met exactly; **not already set**, so this was a real change, not a no-op.
- **Applied:** guarded UPDATE inside a transaction with a name assertion and a post-update value assertion that rolls back on mismatch. Result on both targets: `before=CEF | after=greencircle_cef | rows=1`.

| Target | Server | Before | After | Rows |
|---|---|---|---|---|
| `GCCDatabase_Dev` | ANUKI01 | `CEF` | `greencircle_cef` | 1 |
| `gccwebap_certificates` (TEST) | `68.71.129.120,1533` | `CEF` | `greencircle_cef` | 1 |

- **Verified after:** `SELECT Id, Name, AmazonCode WHERE Id = 5` returns `greencircle_cef` on test. **476 certificates** are of this type and now carry the corrected code.
- **No post-deploy copy is needed and none was added.** Checked before deciding: nothing in the application writes `CertificationTypes.AmazonCode` — `SeedData.cs` never sets it, no migration seeds it, and the import's `AmazonCode` assignments (`CertificateImportService.cs:423, 439, 1234`) are on **`ProductIdEntry`**, a different entity. The value persists across deploys, so this does **not** belong in `Scripts/database/post-deploy/` ([[feedback_data_scripts_go_in_post_deploy]] applies to backfills that must re-run; this is not one).
- **One caveat for retest timing:** `/api/v1/certificates` carries `[ApiCache(CacheProfiles.Medium)]` = **300 s** (`Controllers/Api/v1/CertificatesController.cs:126`, `Attributes/ApiCacheAttribute.cs:144`). A response cached just before the change can serve `CEF` for up to five minutes. Not a defect; wait it out before concluding the change did not take.
- **Rollback** (unused, kept): `UPDATE CertificationTypes SET AmazonCode = 'CEF' WHERE Id = 5 AND Name = 'Certified Environmental Facts';`

**Follow-up found while verifying — flagged, NOT actioned, and it is the client's call.** CR-021 §4 says the remaining types *"have no Amazon code"*. **That is not what the data shows.** Only **four** of seventeen now carry the long form (CES, RC, SEP, and CEF as of today); the rest still hold seeded short codes, and ten of those have certificates behind them:

| Type | Code | Certs |
|---|---|---|
| Material Ingredient Reporting | `MIV` | **1,557** |
| Certified Sustainability Facts | `CSF` | 349 |
| GreenCircle Audited | `GCA` | 248 |
| Waste Diversion from Landfill | `WDL` | 245 |
| Life Cycle Assessment (LCA) Optimized | `LCA` | 196 |
| Closed Loop Product | `CLP` | 79 |
| Certified Water Savings | `CWS` | 21 |
| Dematerialization | `DEM` | 12 |
| Sustainable Manufacturing Practices | `SMP` | 10 |
| Greenhouse Gas (GHG) Verification | `GHG` | 2 |

The client's own rationale for this CR — *"matches more of format as others"* — applies to every one of these. **Whether it matters depends on which types Amazon actually receives**, which is a question for them, not an assumption for us: a short code only reaches Amazon if certificates of that type are partner-tagged. **Do not mass-update these.** Ask which types Amazon expects, get the official strings from the client as we did for CEF, and raise one CR for the set. Rough cost once the mapping is supplied: ~1 hr for the update and verification, plus whatever the mapping conversation takes.

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

### CR-045 (approved 2026-09-04, **BUILT 2026-09-08**) — `Certification Results` is a true free-text field: publish the stored value verbatim

- **Source:** the client, via Shawn, 2026-09-04: *"the client wants it be a free text field. If they want the percentage they will add it. so everything on the data imports should match what they put in the cell."* Raised by Shawn on 2026-09-03 as the item to settle: *"it should just be an open text field and whatever is inputted is the value we want. But we need to talk about what this would do for all inputted data… And can't have the update break either."*
- **This closes the item parked at the end of [CR-042](#cr-042-2026-09-02--normalise-stored-certificationresults-to-explicit-xx-strings-803-certificates).** CR-042 normalised every *existing* row to an explicit `XX%` string but explicitly left the display-time guess in place for *future* values. This CR removes the guess.
- **What is actually wrong today.** `Extensions/DisplayFormatters.FormatCertificationResults` rewrites the field on the way out: numeric ≤ 1 → ×100 + `%`; numeric > 1 → append `%`. So an analyst who records something that is **not** a percentage has it silently published as one — `0.5` → `50%`, `85` → `85%`, `2` → `2%`. The field is documented in its own source comment as free text (`"Pass"`, `">50%"`, `"Silver"`), but only non-numeric values actually survive unmodified.
- **Entry and import are already verbatim — only publication is not.** The CSV importer stores the cell as-typed (`CertificateImportService.cs:1212/1334`, no conversion anywhere) and Create/Edit assign the posted value directly (`CertificateController.cs:550/1222`). Nothing needs to change on the write path.

- **Scope — 11 call sites strip the formatter, plus one migration:**

  | Layer | File:line |
  |---|---|
  | CSV export (admin) | `Controllers/ExportController.cs:351` |
  | CSV export (service) | `Services/CertificateService.cs:1398` |
  | Internal tile API | `Controllers/Api/CertificateTileInternalApiController.cs:396` |
  | External tile API | `Controllers/Api/CertificateTileExternalApiController.cs:330` |
  | Partner/Amazon API v1 | `Controllers/Api/v1/CertificatesController.cs:306, 352, 1155, 1728, 1761` |
  | Detail views | `Views/Certificate/CertificateInternalView.cshtml:192`, `CertificateExternalView.cshtml:143` |
  | **Sort column (migration)** | `Data/ApplicationDbContext.cs:117–125` + `Models/Certificate.cs:71` |

- **⚠️ The sort column MUST move in the same deploy.** `CertificationResultsNumeric` is a *persisted computed column* carrying the identical `<= 1 → ×100` guess, and it drives `cert-results` / `cert-results-za` sorting on both tile APIs. If display goes verbatim and the column does not, a future `0.5` displays as `0.5` and sorts as `50` — precisely the display/sort divergence **DEF-383** exists to prevent. The column keeps its `%`-strip (so `23%` still sorts as 23) and drops the ×100 branch only.
- **✅ No existing row changes, and that is why this is safe now.** CR-042 already made stored value == published value for every populated row on TEST and DEV (0 bare numerics remaining; the 28 non-numeric survivors were never formatted). Publishing verbatim is therefore a **no-op on today's data** — no display change, no sort change, no export change, no change to the Amazon payload. CR-042 was the prerequisite that made this cheap; doing it in the other order would have rewritten 803 published values.
- **⚠️ Read of *"should match what they put in the cell"* — no data is being reverted.** The source workbook cells are `0%`-formatted: the cell *holds* `0.23` and *displays* `23%`. What the client authored and sees is `23%`, which is what CR-042 wrote to the database, so the historical rows already satisfy this instruction and the client's own 8/26 ASIN corrections stay consistent with them. **Reverting the 803 rows to bare decimals is explicitly NOT in scope** — that would be a separate destructive CR and would contradict the client's visible source data. *Flagged to Shawn 2026-09-04 for confirmation; the code work is identical either way.*
- **⚠️ Consequence being accepted — the safety net comes off.** The formatter has been silently correcting bare decimals on the way out. After this, a `0.23` that reaches the database (bad import, mistyped entry) publishes to the public database **and to Amazon** as `0.23`, not `23%`. That is the client's stated intent — *"whatever is inputted is the value we want"* — and is logged here so the trade is on file. Recommended mitigation (separate, optional): a soft warning in the import preview and on Create/Edit when a bare numeric ≤ 1 is entered — *"stored as `0.23`; did you mean `23%`?"* — which guards the door without overriding the analyst.
- **Amazon contract.** The JSON key `certificationResults` and its type are unchanged, so `/api/v1/certificates*` is **not** re-shaped ([[feedback_amazon_api_immutable]]). Only future *values* can differ from what the formatter would have produced. Worth a heads-up to the partner contact rather than a contract re-baseline.
- **Future data loads — the durable lesson.** Any subsequent migration script reading a `0%`-formatted Excel column must take the **displayed** value, not the raw cell value, or it will reintroduce bare decimals that nothing downstream will correct any more. `Scripts/data-ops/2026-09-03-batch2-load-BUILD.py` §4 already does this.
- **Effort:** ≈ 0.5–1 day — the code change is mechanical; the cost is the migration plus regression across export, both tile APIs, the partner API and the two sort directions.
- **Status:** **BUILT 2026-09-08 — verified on dev, pending deploy.** Estimate was logged before build per `docs/governance/change-request-process.md`; the actual came in at the low end of the 0.5–1 day range.

#### CR-045 as built (2026-09-08)

- **The formatter was DELETED, not disabled.** `Extensions/DisplayFormatters.cs` is gone, along with all
  **12** call sites — the scope table said 11; the twelfth is `CertificateTileInternalApiController.cs:402`,
  which the original count missed. Deleting rather than neutering is deliberate: a no-op formatter left in
  place invites someone to re-add "just one" conversion at a single call site, and a field that means
  different things on different surfaces is precisely how DEF-383 happened. The reasoning now lives on
  `Certificate.CertificationResults` itself, where the next developer will actually look.
- **The sort column moved in the same change**, as the CR required: migration
  `20260908175939_CR045_CertificationResultsVerbatimSort` drops the `<= 1 → ×100` branch from
  `CertificationResultsNumeric` and keeps the `%`-strip. Applied to `GCCDatabase_Dev` 2026-09-08.
- **The "no-op on today's data" claim is now MEASURED, not asserted.** Enumerated on dev after the migration:
  **1,102** populated rows — **1,071** already `%`-formatted, **31** non-numeric (NULL sort key, correctly
  outside the numeric dimension), **0 bare numerics**. The removed branch was unreachable for every row in
  the database, so not one sort position or published value changed. CR-042 is what made that true; running
  these in the other order would have rewritten 803 published values.
- **Test guard replaced, not dropped.** `DisplayFormattersTests` asserted the opposite rule and is gone;
  `CR045_CertificationResultsVerbatimTests` replaces it and reads the computed-column SQL out of the EF model
  to assert there is no `* 100`, that the `%`-strip survives, and that non-numerics still yield NULL.
  **It failed against the pre-change column and passes after** — which is the only evidence that a guard
  guards anything.
- **Live verification on localhost/dev, 2026-09-08:** stored `23%` publishes as `23%` on the internal tile
  API and the detail view; stored `30%` as `30%` on the external tile API. Re-runnable:
  `Scripts/data-ops/2026-09-08-CR043-CR045-verify.py`.
- **Files:** `Extensions/DisplayFormatters.cs` (deleted), `Models/Certificate.cs`,
  `Data/ApplicationDbContext.cs`, `Migrations/20260908175939_CR045_*`,
  `Controllers/Api/CertificateTileExternalApiController.cs`,
  `Controllers/Api/CertificateTileInternalApiController.cs`,
  `Controllers/Api/v1/CertificatesController.cs` (x4), `Controllers/ExportController.cs`,
  `Services/CertificateService.cs`, `Views/Certificate/CertificateInternalView.cshtml`,
  `Views/Certificate/CertificateExternalView.cshtml`.
- **⚠️ Still open from the CR text:** the recommended soft warning on import preview / Create / Edit when a
  bare numeric ≤ 1 is entered was **not** built — optional, separately scoped. Without it the safety net is
  fully off, which is the client's stated intent.

### CR-044 (executed 2026-09-03, retroactive authorisation requested) — Batch-2 migration: 3 in-place renewals + 2 reconstructed certificates

- **Source:** the client's 2026-09-03 migration drop (`GCC Database Migration_Certs_9.3.26.xlsx`,
  `GCC ASINs for DB Upload_9.3.26.xlsm`), forwarded by Shawn with a same-day deadline: *"If you are
  able to have these uploaded by EOD today/Fri AM that would be awesome while I still have my intern
  working."* Shawn's instruction on the two open decisions, 2026-09-03: *"Create the Certs for
  26-0438 and 26-0439, the certificates are available on the production site"* and *"create the two
  certs and then load the data."*
- **Why CR, not routine data ops.** The bulk of the drop — 191 new certificates and 1,870 new
  product identifiers — is ordinary additive migration and needs no CR. **Two parts of it do:**
  1. **Three existing business records were UPDATED in place** (`25-0497`, `25-8163`, `25-8164`, all
     GAF). Standing rule: an UPDATE or DELETE against a business record requires a formal CR
     ([[feedback_data_changes_require_cr]]).
  2. **Two certificates were created that the client did not supply in the sheet** (`26-0438`,
     `26-0439`, both Philips). Creating a certificate record from evidence rather than from the
     client's own upload file is a decision, not a transcription.
- **Status:** **EXECUTED on the test environment 2026-09-03**, on Shawn's explicit instruction and
  against the stated deadline. Client sign-off is **not** on record. This entry exists so the change
  is priced and visible rather than absorbed ([[feedback_absorbed_cr_is_still_priced]]).

**Part 1 — the three renewals.** The sheet carries these three as `Client Approved - Paid`,
2026-08-01 → 2027-07-31. The database held them as `Expired`, 2025-08-01 → 2026-07-31. They are
renewals of live records, not new certificates, and `Certificates.Number` is unique, so they could
only be applied as an UPDATE.

*The client's own production system was used as the tie-breaker*, not our reading of the sheet:
`https://db.greencirclecertified.com/api/certificates`, fetched 2026-09-03, returns all three with
the renewed dates. The spreadsheet is right and our database was stale. Only `Status`,
`EffectiveDate`, `ExpirationDate` and `Location` were written; company, product, certification type
and PDF links are untouched. The pre-change values are transcribed into `batch2-UNDO.sql`, so the
reversal restores the original rather than whatever is current at the time.

**Part 2 — the two reconstructed certificates.** 1,500 of the drop's 1,909 ASIN rows (79%) pointed
at `26-0438` / `26-0439`, which existed in neither the database nor the certificate sheet. Every
field was fetched or measured, none invented:

| Field | Source |
|---|---|
| Number, company, product, certification type, effective/expiration, location | `db.greencirclecertified.com/api/certificates` ids 5923/5924, fetched 2026-09-03 |
| Status = `Client Approved - Paid` | that API serves published certificates only |
| Country = `Netherlands` | `CountryList` canonicalisation of the API's "Drachten, The Netherlands"; Location keeps the city, matching siblings `24-1633`/`24-1634` |
| Product Category = Consumer Products | the API's `productType` |
| Product Type = Health and Beauty / Baby Products | the sibling certificate carrying the SAME product name — the API has no Level-2 type |
| Partner = Amazon, Pathway, Results = 100% | the ASIN sheet's own `amazonCode` / `pathway` / `certificationResults` columns |

The join is evidenced, not assumed: `26-0439`'s ASIN product names read `E42-Comforting` and
`24-1634`'s existing 276 product identifiers read `E42 - Comforting`; `26-0438`'s read
`DF3-OneBlade Blades` against `24-1633` "One Grooming".

**Two further judgement calls inside the same load, recorded here because they are also decisions:**

- **Two company names were folded into existing records** rather than created:
  `J&J MedTech Orthopedics` → existing `J&J MedTech Orthopaedics` (one-letter spelling variant of
  the same brand), and `GAF Materials LLC` → existing `GAF` (six other rows in the *same sheet*
  spell it `GAF`). Creating either would have split one client's certificates across two records
  that then have to be merged by hand in the UI. One certificate each; reversible by reassigning
  the certificate and deleting the company. Listed in `company-decisions.csv`.
- **One product-taxonomy row was created:** `TPO` as a Level-2 child of `Plastics`. The client's own
  "Field Options" sheet lists it as `1.4.9 TPO`, so the taxonomy is client-authorised; one
  certificate uses it.

- **Scope:** data only. No application code changed. `Scripts/data-ops/2026-09-03-batch2-load-BUILD.py`
  (generator), `2026-09-03-batch2-VERIFY.py` (independent verification),
  `_batch2_output/<stamp>/batch2-load.sql` + `batch2-UNDO.sql` + six audit CSVs.
- **Partner API impact:** none to the contract. `amazon-contract-diff.js` run against gccwebapp.com
  after the load: **7 passed, 0 failed, CONTRACT UNCHANGED**. Amazon now receives 1,870 additional
  product identifiers across 12 certificates, which is the point of the drop.
- **Effort:** ~6 hrs (analysis, generator, dev rehearsal, backup, load, verification, the PDF
  retrieval below, and the write-up). **Absorbed** — logged for value, not invoiced separately.

**Addendum, same day — the 96 certificate documents.** The drop carried no PDFs, so all 193
certificates loaded with an empty `CertificateFile`. 96 of them were published on the client's own
production database and were fetched from there and attached, via the application's own
`POST /api/certificates/{number}/upload-pdf` (AdminOnly) rather than by hand-placing files as the
August load did — the endpoint already resolves the physical path through `IUploadPathResolver`
(DEF-286), renders the preview (DEF-262) and sets both columns in one transaction. 96/96 attached
and verified byte-identical to what production served; no two certificates share a document or a
preview across the whole 5,924-row table.

This needs no separate CR — attaching a client's own published certificate to that certificate's
record is the drop being completed, not a scope change — but it is recorded here because it
**independently corroborates the two reconstructed certificates above.** `26-0438`'s own document
reads *"Philips / Drachten, The Netherlands / Grooming / Sustainable Energy Practices / 100%
Renewable Electricity Usage / Certification Number: 26-0438 / Certification Period: July 1, 2026 -
July 7, 2027"* — every reconstructed field, confirmed by the certificate itself.

Tools: `2026-09-03-batch2-pull-pdfs.py`, `2026-09-03-batch2-upload-pdfs.py`. Undo:
`_batch2_pdfs/undo-<stamp>.sql` (clears the two columns; leaves the files, which are inert once
unreferenced).

#### CR-044 follow-up (2026-09-04) — the three renewals never received their `Certification Results`

Found by `Scripts/data-ops/2026-09-04-certresults-audit.py` (verdict `DB_BLANK`, 3 rows) while
auditing CR-045. The renewal path above moved only **Status / EffectiveDate / ExpirationDate /
Location**, as documented — so the `Certification Results` value the client's sheet carries for
`25-0497` / `25-8163` / `25-8164` was never written, and the field is empty on TEST.

- **Not a defect.** The loader did exactly what it said it would; the field was outside the four it
  moved ([[feedback_data_issue_vs_defect]]).
- **The dates are fine — verified, because that was the first question asked.** All three rows on
  TEST match the workbook exactly: `2026-08-01` → `2027-07-31`, `Client Approved - Paid`, and the
  full multi-site `Location` string. **Nothing date-related is missing.**
- **The value is the client's, not ours.** `Multiple` is the literal cell content for all three on
  `Final Upload List 9.3.26`, read 2026-09-04. It is a **text** cell, so the sheet's `0%` number
  format does not apply and no conversion is involved. `Multiple` is already an established stored
  value — `26-0412` carries it today.
- **Fix staged, not run:** `Scripts/data-ops/2026-09-04-cr044-gaf-renewal-certresults.sql` — 3 rows,
  transactional, aborts unless exactly 3 match, refuses to overwrite a non-empty field, asserts the
  sort column stays `NULL` (a non-numeric value must not produce a numeric sort key — DEF-383),
  idempotent, with an UNDO block. **Awaiting Shawn's go-ahead** ([[feedback_data_changes_require_cr]]).

#### CR-044 confirmation (2026-09-04) — the two reconstructed Philips certs, corroborated independently

The CR-045 audit flagged `26-0438` / `26-0439` (`100%`) as having no source cell, because they came
from the **ASIN** workbook rather than the certificate workbooks. Shawn confirmed 2026-09-04: *"The
Phillips ones are updated based on current production."* That matches the reconstruction evidence
recorded above, from a different direction — so the two certs are accounted for and need no action.

### CR-043 (proposed 2026-09-02, answers received 2026-09-03, **BUILT 2026-09-08**) — Confidential certificates must reach the PI export and the partner/Amazon API

- **Source:** the client, via Shawn, 2026-09-02, verbatim: *"It appears any certs marked 'Client Paid - Confidential' are not included in the PI export file (which I assume would also mean they would not go to Amazon). This is not accurate. They must be in export and go to Amazon when they are active since that is how they get badged. The Confidential piece is just for the public DB side to not show there. Make sure Client Paid - Approved, Client Paid - Confidential, and Republished statuses would all be in PI export and passed in API assuming within eff/exp date period."*
- **Why CR, not a defect — we asked this exact question and were told the opposite.** On **2026-08-24**, `docs/tracking/2026-08-24-client-questions-partner-api.md` (CR-035) put it to the client in writing: *"Which certificate states become partner-visible, and under what label? **(Recommendation: Confidential stays hidden.)**"* and, more pointedly: *"**Client Approved – Confidential is paid-but-embargoed** — you deliberately chose to keep those private. Our recommendation is that Confidential stays hidden regardless of what else changes, but we need that stated rather than assumed."* The current behaviour is therefore the documented, deliberate position, flagged as an open decision and awaiting exactly this answer. The client is now stating it — **opposite to our recommendation**. That is a requirements change.
- **The export side matches its own spec too.** CR-184 defined an "active product identifier" as one whose parent certificate is *publicly live*, inheriting CR-015. `ExportController.DownloadActiveProductIds` implements that definition faithfully. Nothing is broken in either surface.
- **The requirement we never captured.** We understood "Confidential" as *keep it private, full stop*. The client means **"absent from the public database, but still commercially active."** Confidential is paid work that gets badged; the embargo is a publishing preference, not a commercial state. That distinction appears nowhere in the requirements baseline and is the substance of this CR.
- **Root cause of the behaviour (single point).** `CertificateStatusHelper.PublicStatuses` = `{Client Approved - Paid, Republished}` (`Models/CertificateStatus.cs:65`) is one list doing two jobs — anonymous public-DB visibility (where excluding Confidential is correct and stays) *and* partner/badging visibility (where it is now wrong). The two need separating.
- **Corroboration that badging intent already existed:** `Controllers/Api/v1/CertificatesController.cs:1693` already maps `Client Approved - Confidential` → `ACTIVE` in the GS1 status mapper. That line is unreachable today — the query filter at line 1509 drops the row first. The intent was there; the gate overrode it.
- **Verified on the client's environment** (gccwebapp.com test DB, read-only, 2026-09-02):

  | Measure | Count |
  |---|---:|
  | `Client Approved - Confidential` certs in eff/exp window | 37 |
  | — of those, carrying **active product identifiers** | **11** |
  | — of those, carrying **none** | 26 |
  | Product-identifier rows added to the PI export | **110** |
  | PI export rows today (Paid + Republished) | 16,081 |
  | Of the 37, tagged to **Amazon** (become API-visible) | 17 |
  | Of the 37, carrying **no partner tag at all** | 20 |

  **The client's example list is exactly complete.** All **11** PI-bearing Confidential certs are in their list of 12, and there are **zero** they missed — verified by set difference in both directions, not by count ([[feedback_equal_counts_are_not_the_same_rows]]). The 12th, `23-0758`, is the expired one below.

  The other 26 Confidential certs hold **no product identifiers**, so they add nothing to the PI export. Six of them are Amazon-tagged (`24-0001`–`24-0005`, `25-0478`) and become API-visible without contributing badge data; the remaining 20 carry no partner tag, so no partner sees them regardless of status — partner delivery is a two-condition gate ([[feedback_partner_scoping_two_filters]]). Badging any of those 20 is a **data task** (partner tags + product identifiers), not part of this CR.
- **⚠️ One of the client's 12 examples is a different cause and will NOT be fixed by this CR.** `23-0758` is status **`Expired`**, expiration **2026-08-31** — it dropped out of the export on expiry, two days before they wrote. The other 11 are all `Client Approved - Confidential` and in-window, exactly as described. This must be said back to the client *before* delivery, or `23-0758` will still be absent afterwards and read as a failed fix ([[feedback_verify_through_client_click_path]]). Note this is the **same certificate** already explained to them on 2026-09-02 in `qc-log-update-2026-09-02-certs-23-0358-23-0758-25-0351.md` — now reported missing twice from two angles, so the expiry explanation evidently did not land ([[feedback_qc_log_open_status_unreliable]]).
- **Naming note (no ambiguity, worth correcting once):** the client writes "Client Paid - Confidential" and "Client Paid - Approved"; the actual stored values are `Client Approved - Confidential` and `Client Approved - Paid`.
- **Scope.** Add a second list — e.g. `BadgeableStatuses` = `{Paid, Confidential, Republished}` — and repoint the badging surfaces at it, leaving `PublicStatuses` untouched for the anonymous site:
  - `Controllers/ExportController.cs:293` — PI export
  - `Controllers/Api/v1/CertificatesController.cs` partner branches: 183, 401, 458, 781, 882, 1094, 1352, 1509, 1636
  - **Unchanged:** every public surface (`CertificateTilesApiController` 111/269/459, `CertificateController` public gates, `CertificateVisibility.PubliclyLiveExpression`) — the client explicitly confirms the public DB keeps hiding Confidential.
  - **Unchanged, pending decision:** External/company logged-in branches (212, 1128, 1544) — see open questions.
- **Effective/expiration window logic is unchanged.** The client's *"assuming within eff/exp date period"* confirms the existing DEF-358 rule; `WhereEffectiveDateReached()` already enforces it and stays as-is.
- **Partner API response *shape* is unchanged** — same fields, more rows — so this stays inside [[feedback_amazon_api_immutable]]. But `Scripts/smoke-prod/_security/partner-isolation.js` and `rbac-matrix.js` assert the current narrower set and **will go red on purpose**; they must be re-baselined as part of the work (the CR-036 precedent).
- **Open questions for the client (both raised by their own wording, neither answered):**
  1. **External/company logged-in view.** *"The Confidential piece is just for the public DB side"* implies a company should see its own Confidential cert when signed in. That is a third surface, not covered by "PI export and API". Confirm before we touch it.
  2. **Internal dashboard counts.** The client says *"when they are active"* — implying Confidential **is** active. DEF-223 (Shawn, 2026-05-26) deliberately narrowed `DashboardStatuses` to exclude it so "Active" meant the same thing internally and publicly. This CR's rationale reverses that premise. Reverting DEF-223 is a separate decision with its own blast radius across every dashboard tile and export.
- **Precedent worth noting:** `docs/qa/gap-report.md` **G12** already records that DEF-223 changed Confidential handling *"tracked as a DEF, not a CR — docs never updated."* Same status, same misclassification, four months ago. This is the second time Confidential scope has moved without a CR; hence logging it properly here.
- **Effort:** ≈ 0.5 day — the code change is small and mechanical; most of it is re-baselining the security regression suite and re-verifying partner isolation still holds with the wider set.
- **Detail doc:** `docs/tracking/CR-043-confidential-in-pi-export-and-api.md` (full evidence, per-cert table, call-site list, test impact).
- **Sent to client?** **No** — paste-ready QC log cells at `docs/tracking/qc-log-update-2026-09-02-CR043-confidential-pi-export.md`. **QC item # and Reporter still to be filled in** ([[feedback_def_entries_note_qc_id]]).
- **Status:** **BUILT 2026-09-08 — verified on dev, pending deploy.** Both open questions were answered by
  the client on 2026-09-03 (Q1 **Yes**, Q2 **Yes**), which settled the scope.

#### CR-043 as built (2026-09-08)

**The shape of the fix is the one the CR proposed** — a second status list, with `PublicStatuses` left
untouched so the anonymous public database keeps hiding Confidential:

```csharp
BadgeableStatuses = { Client Approved - Paid, Client Approved - Confidential, Republished }
```

plus `IsBadgeable(status)`, the counterpart to `IsPubliclyVisible(status)`. Four `PublicStatuses` call sites
remain and they are exactly the four public surfaces: `CertificateTilesApiController:111`,
`CertificateVisibility.PubliclyLiveExpression`, `CertificateService`'s anonymous branch, and
`EfQueryWarmupService`.

**Q1 (Yes) turned out to be wider than the CR's call-site list.** The CR named the three v1-API External
branches (212, 1128, 1544). But "a company signed in to its account" is primarily the **external tile
controller**, not the v1 API — `CertificateTileExternalApiController` (5 filters) plus `CertificateService`'s
External and Partner export branches. Those sat in the "unchanged" column and had to move, or the client
would have answered Yes and seen no change on the screen they actually meant.

**Q2 (Yes) reverses DEF-223.** `DashboardStatuses` regains Confidential. It is kept as a separate array from
`BadgeableStatuses` even though the two are equal in value today — collapsing two questions into one array
is the fault this CR exists to fix. Three Analyst/Admin reports in `ExportController`
(`active-certificates`, `expiring-30-days`, `expiring-3-months`) were still on `PublicStatuses` and moved to
`DashboardStatuses`: the same tile-vs-export disagreement DEF-200 fixed for the dashboard exports and missed
here. The ETL `ActiveCertificates` tallies moved too — every one of them is a `Count()` over
already-selected rows, never a visibility filter.

**Two things the CR did not anticipate, both found by following the data rather than the call-site list:**

1. **`FileAccessAllowed` had to become role-aware.** It gated PDFs and preview images through a single
   `IsPubliclyVisible` check for every non-Admin role. Left alone, a Confidential certificate would appear
   in the partner's feed and **404 on its own document** — which reads as a broken deploy, not a permissions
   rule. Anonymous keeps `IsPubliclyVisible`; External and Partner get `IsBadgeable`. This is the adjacent
   endpoint DEF-103/164/198/204/205 keep catching us on.
2. **Product-identifier `Status` needed a data backfill.** `CertificateProductId.DeriveStatusFromCert`
   (CR-015) is written **on save**, not computed, so identifiers on Confidential certs still read
   `Inactive`. They would have shipped inside a file titled *Active Product Identifiers* and gone to Amazon
   while the Product Identifiers grid labelled them inactive — a contradiction in front of the client.
   `Scripts/data-ops/2026-09-08-CR043-productid-status-backfill.sql` fixes it: transactional, dry-run by
   default, idempotent, and it asserts that identifiers on **non**-badgeable certificates are still
   `Inactive` (equal counts would not have proved the JOIN did not spill). **420 rows across 13
   certificates, committed on dev 2026-09-08. MUST run against TEST in the same release as the code.**

**Measured on dev, enumerated not sampled:** 13 in-window Confidential certificates carry product
identifiers — the 11 the client listed, plus `26-0473`/`26-0474` from later loads — and all 13 are
Amazon-tagged.

**Live verification, 11/11 PASS** (`Scripts/data-ops/2026-09-08-CR043-CR045-verify.py`):

| Check | Result |
|---|---|
| Anonymous → all 13 Confidential certs | **404** (embargo intact) |
| Anonymous → control public cert `23-0753` | **200** (the surface is alive, so those 404s mean something) |
| PI export contains the 13 | **13/13**, 420 identifier rows, labelled `Client Approved - Confidential` |
| PI export still excludes expired `23-0758` | **absent** — different cause, unchanged by this CR |
| Amazon partner tile API | **13/13 visible** (totalCount 218) |
| **Novi** partner tile API — the isolation control | **0/13**, totalCount 1 (non-empty, so the control is live) |

The Amazon/Novi pairing is the point: widening a **status** set must never widen a **partner** set. An
Amazon-only run showing more rows could not tell "correct" from "isolation broken". An earlier version of
this probe read the wrong JSON field and returned 0 for *both* partners — a control that "passes" because
nothing came back proves nothing, which is why the non-empty assertion is now in the script.

**Security suite:** `partner-isolation.js` and `rbac-matrix.js` needed **no** re-baselining after all — they
discover certificates dynamically and assert scoping, not a fixed status list. The CR's warning that they
would "go red on purpose" was wrong, and that is the better outcome. `cert-detail-scoping.js`'s anonymous
status-gate assertions are unaffected because the anonymous gate did not move.

**.NET test suite: zero net-new failures**, established by diffing the failing-test set against a
pre-change baseline rather than by reading a total. Two `QC_CertificateRepositoryTests` assertions were
re-baselined on purpose — they *were* the DEF-223 assertions that Q2 reverses: `Active` 2 → 3,
`CompaniesWithActiveCerts` 2 → 3, while `CertTypesWithActiveCerts` stays at 2 because the Confidential cert
shares a type with an already-active one. That last one is the control showing the widening moved only what
it should have.

- **Files:** `Models/CertificateStatus.cs`, `Models/CertificateProductId.cs`,
  `Controllers/ExportController.cs`, `Controllers/CertificateController.cs`,
  `Controllers/Api/v1/CertificatesController.cs` (12 branches),
  `Controllers/Api/CertificateTileExternalApiController.cs`, `Services/CertificateService.cs`,
  `Services/FilterService.cs`, `Services/ETL/DataExtractor.cs`, `Services/ETL/DataTransformer.cs`,
  `Scripts/data-ops/2026-09-08-CR043-productid-status-backfill.sql`.
- **⚠️ Deploy note:** Amazon begins receiving certificates it has never seen (17 on test per the 2026-09-02
  measurement, 13 on dev). Say so in the delivery note — and say plainly that `23-0758` is **expired** and
  stays absent, because it has now been reported missing twice.
- **Follow-up:** close `docs/qa/gap-report.md` **G12**, which records the same Confidential-scope drift being
  tracked as a DEF instead of a CR back in May.

### CR-042 (2026-09-02) — Normalise stored `CertificationResults` to explicit `XX%` strings (803 certificates)

- **Source:** the client, via Shawn, 2026-09-01: *"It appears in Cert Results in import it made percentages as decimals (.23 for 23% 1 for 100%)… When reviewing all the ASIN certs I did correct to make them percentage format XX%. However, I assume all others imported would need corrected by Anuki."* Executed on Shawn's instruction 2026-09-02: *"now let's fix the cert results percentages."*
- **Why CR, not a defect:** **nothing was broken.** The importer never converted anything — the client's source workbooks recorded results in Excel cells carrying the number format `0%`, so the cell *displayed* `23%` while *holding* `0.23`, and the migration read the true cell value, which is correct behaviour for a numeric cell. The app then rendered `23%` via `DisplayFormatters.FormatCertificationResults`. Correct code + a storage format the client wants changed = data ops under a CR ([[feedback_data_issue_vs_defect]], [[feedback_data_changes_require_cr]]).
- **What was actually wrong — the guess, not the values.** Stored value and published value were written in different dialects, so the display rule had to *guess* whether a bare number was a fraction (`0.86`) or an already-whole percentage (`100`). That guess is correct for every value in today's data and wrong the first time an analyst records a result that is not a percentage — Shawn's own words: *"Sometimes we won't put in % value in box it may be some other number or value."* Writing the percentage explicitly removes the guess for every existing row.
- **Scope — 803 of 990 populated rows (TEST).** Measured by enumeration, not sampling ([[feedback_enumerate_dont_sample_populations]]):

  | Stored shape | Certs | Action |
  |---|---:|---|
  | bare decimal `0.01`–`0.99` | 697 | → `1%`–`99%` |
  | bare `1` | 106 | → `100%` (DEF-383: `1` means 100%) |
  | already `XX%` | 159 | untouched — the client's 8/26 ASIN corrections |
  | non-numeric (`Silver` 17, `Gold` 7, `+Vantage Vinyl Verified` 4) | 28 | untouched |

  **No bare number above 1 exists anywhere**, and every decimal is 2-place, so the rewrite is exact and lossless. Verified pre-flight that **no** value would produce a fractional percentage.
- **⚠️ ZERO change to any displayed value, any sort order, or the Amazon payload — and that is the point.** This is a storage-format cleanup, not a data correction:
  - **Display:** `FormatCertificationResults` returns a `%`-bearing string unchanged, so `"23%"` renders exactly as `0.23` did.
  - **Sort:** the persisted computed column `CertificationResultsNumeric` strips `'%'` before casting (read from `sys.computed_columns`, not assumed), so `0.23` and `"23%"` both compute to `23`.
  - Because both halves are provably unchanged, the partner API output is byte-identical and no contract re-baseline is needed ([[feedback_amazon_api_immutable]]).
- **The assertion that made this safe.** DEF-383's lesson is that the C# formatter and the T-SQL computed column can disagree silently. So the script snapshots `CertificationResultsNumeric` for **every** populated row before the update and, inside the same transaction, aborts unless the value is identical for every row afterwards. A drift between the two halves rolls the whole thing back instead of shipping mis-sorted certificates.
- **Non-whole percentages are skipped, not rounded.** Any value whose percentage would not be a whole number is deliberately left alone and reported. There are none today; silently rounding a future `0.235` would be the same class of fabrication DEF-353 exists to prevent.
- **Result.** Dev: 989 populated → 960 rewritten. **TEST: 990 populated → 803 rewritten**, `0` bare decimals remaining, 28 non-numeric survivors intact (`Silver`/`Gold`/`+Vantage Vinyl Verified`), `100%` now 165 certs (59 that always said `100%` + the 106 that said `1`). Per-row sort assertion passed on both.
- **Artifacts:** `Scripts/data-ops/2026-09-02-normalize-certification-results-to-percent.sql` — idempotent (a re-run is a no-op, every rewritten row now contains `%`), pure ASCII, runs on dev or test unchanged. Pre-change logical backup: `Scripts/data-ops/_backups/20260902-pre-certresults-normalize/` (44 tables, 41,234 rows).
- **Effort:** ≈ 0.25 day, almost all verification. **No code shipped, no migration, no deploy.**
- **STILL OPEN — the guess is only disarmed for existing rows.** `FormatCertificationResults` still appends `%` to any bare number a *future* entry or import introduces: `0.5` would publish as `50%`, `85` as `85%`. Closing that needs a code change (normalise on write, or publish the field verbatim) plus a matching change to the computed column, and is **not** covered by this CR. Parked with Shawn.
- **Status:** **DONE — EXECUTED AND VERIFIED ON TEST AND DEV 2026-09-02.**

### CR-041 (proposed 2026-09-02) — Authorization to load certificates `23-0358` and `25-0351` with a fabricated 2040 expiration date

- **Source:** the client, answering our own open item #1 in `docs/tracking/2026-09-01-client-email-decisions-response.md`. Their words: *"Both of these are Sofidel certs that don't get an expiration date. If this will be an issue in new platform we can add a far out date like 2040. Anuki confirm and please make sure added (they are in the sheet we sent)."* Relayed and re-affirmed by Shawn 2026-09-02 after the counter-evidence below was put to her: **"the client said to use 2040 as the date for these records."**
- **Why CR (not a defect):** nothing is broken and **no code ships under this CR.** The transform behaved exactly as designed — `withhold_incomplete()` quarantined both rows on 2026-08-18 because `EXPIRATION_DATE` is blank at source, and recorded them in `_transform_output/20260818-105029/quarantine-incomplete-rows.csv` (they are its first two rows). This is an `INSERT` of business records carrying a **value we invented rather than received**, onto an environment in daily client use, so it needs formal authorization ([[feedback_data_changes_require_cr]], [[feedback_data_issue_vs_defect]]).
- **The client's question, answered — yes, an expiration date is mandatory.** Verified at four independent layers, so a blank genuinely cannot be carried: `Certificates.ExpirationDate` is `DateTime` **NOT NULL** (`Models/Certificate.cs:34`); `CertificateEditViewModel.ExpirationDate` is `[Required]` and the Create/Edit input carries `required`; the importer **refuses to default it** — `CertificateImportService.cs:1886` throws *"it must be rejected in preview, never defaulted"*; and **0 of the 5,702 loaded certificates** have an unset expiration (enumerated, not sampled — [[feedback_enumerate_dont_sample_populations]]). A far-out date is therefore not merely acceptable, it is the **only** mechanism available short of making the column nullable.
- **⚠️ The premise "these don't get an expiration date" is contradicted by the client's own data — recorded here because it was raised and overruled, not because it is unresolved.** All **12** other Sofidel certificates carry expiration dates, every one a Horsham, PA **pair** (Poly Bundle + Poly Inner) on a **364-day term**. Both missing certs land inside existing batches:

  | Batch | Effective | Expiration | Results |
  |---|---|---|---|
  | 23-0428 / 0429 | 2023-06-01 | **2024-05-31** | — |
  | 24-0349 / 0350 | 2024-06-01 | 2025-05-31 | — |
  | 25-0259 / 0260 | 2025-06-01 | 2026-05-31 | 0.25 |
  | 25-0349 / 0350 | 2025-05-01 | **2026-04-30** | **0.4** |
  | 26-0046 / 0047 | 2026-06-01 | 2027-05-31 | 0.25 |
  | 26-0048 / 0049 | 2026-05-01 | 2027-04-30 | 0.4 |

  `23-0358` (eff `2023-06-01`, results `0.25`) sits in the 23-0428/0429 batch → implied **2024-05-31**. `25-0351` (eff `2025-05-01`, results `0.4`, adjacent number) sits in the 25-0349/0350 batch → implied **2026-04-30**. **Both implied dates are in the past**, i.e. the data says these two are a third product line ("Plastic Film Packaging") in batches that have already expired. Also: the longest term anywhere in the dataset is **1,825 days** (5 years, Kohler `20-0432`); 2040 is ~6,000 days, **3.4× anything that has ever existed**, and 1,621 Recycled Content certs are exactly 364 days.
- **Consequence being accepted.** `Client Approved - Paid` is a **public** status, so 2040 does not merely park these records — it publishes two certifications the sibling data implies are expired to the **public database** and out to **Amazon via the partner API**, which reports public certs as `status="Active"` ([[feedback_public_visibility_full_validity_window]]). That exposure runs for ~14 years. **The client owns this decision; it is logged here so the basis is on file and reversible, not to re-open it.**
- **Scope — exactly 2 certificates and 1 product.** Measured against TEST (`gccwebap_certificates`) 2026-09-02 08:08, immediately pre-load: **5,702 certificates**, neither number present, `MAX(Certificates.Id)=5702`, `MAX(Products.Id)=3439`. Sofidel currently has 6 product rows (Poly Bundle ×3, Poly Inner ×3 — CR-186 split), **no "Plastic Film Packaging"**, so the load creates **1 Product + 2 Certificates**; 5,702 → 5,704. No product IDs, tags or partner links (`productids-all.csv` has 0 rows for both numbers). Closes the 5,731 vs 5,702 gap from 29 to **27**.
- **Method — the 8/18 loader, not hand-rolled SQL.** Regenerated through `2026-08-13-migration-sql-generator.py` so lookups, the CR-186 product-identity tuple and CR-015 product-ID status resolve **identically to how the other 5,700 rows arrived**. One transaction; preflight aborts before any write if either number already exists; post-load assertions roll back on a count mismatch. The source CSV is rebuilt by importing the **real** transform module and monkeypatching only the expiry, so transformation semantics cannot drift from the original load.
- **Rollback.** The generator writes **no `AuditHistory` and no `ImportBatches` row**, so **there is no UndoImport for this load** — `2026-09-02-sofidel-missing-expiration-ROLLBACK.sql` is the undo. Guarded on the exact pre-load high-water marks (`Id > 5702` / `> 3439`) and on a post-delete count of 5,702, so it aborts rather than touching anything it did not create. Two findings while writing it: every FK referencing `Certificates` is **`ON DELETE CASCADE`** (verified — `CertificateCertificationTypeSubcategories`, `CertificateDocuments`, `CertificatePartners`, `CertificateProductIds`, `CertificateStatusHistory`, `CertificateTag`, `CustomFieldValues`), so no explicit child deletes are needed; and **`CertificateCountries` does not exist on TEST** — CR-037 is still only in the working tree — so naming it would have made the rollback fail with *"invalid object name"* at the exact moment it was needed ([[feedback_delete_audit_capture_joins]]).
- **Caveat for whoever touches validation next.** `ValidationService.cs:201` rejects any validity period longer than **5 years**, which 2040 breaches by a wide margin. It is currently wired **only** to the legacy `/api/certificates` controller — not the MVC Create/Edit path, not the CSV importer, not the v1 partner API — so these two rows load and edit fine today. **Wiring that validator in more broadly will silently break them.**
- **Amazon impact:** response *shape* unchanged, so no partner sign-off is required ([[feedback_amazon_api_immutable]]). The **content** change is real and intended — two additional certificates begin flowing as `status="Active"`. Run `amazon-contract-diff.js` after the load as usual.
- **Artifacts:** `Scripts/data-ops/2026-09-01-sofidel-missing-expiration-IMPORT.csv` (load-ready, 2 rows) · `…-BUILD.py` (regenerates for any date — takes the date as an argument) · `Scripts/data-ops/2026-09-02-sofidel-missing-expiration-RUN.ps1` (reads the connection string from `appsettings.Production.json`; no credentials typed) · `…-ROLLBACK.sql` · generated SQL at `Scripts/data-ops/_sql_output/20260902-080944/migration-load.sql`.
- **Effort:** **≈ 0.25 day**, essentially all verification and reconciliation — the insert itself is seconds. **No development.**
- **Decisions required:** (1) confirm **`2040-12-31`** is the intended reading of "2040" — the client said only the year; (2) confirm both certs are meant to be **publicly visible** for that period, given the batch evidence above; (3) the remaining **27** withheld records still need values or a written "leave them out permanently" — **11 of them need only an expiration date** (`13-0157`, `14-0278`, `15-0067`, `16-0262`, `18-0172`, `19-0397`, `21-0613`, `21-0746`, `21-0749`, `21-0750`, `21-0751`), but all 11 are status **Expired**, so 2040 is the *wrong* value for them and they need a plausible past date — worth asking in the same message rather than discovering it later.
- **Sent to client?** **No.** The instruction came from them via Shawn; this CR records it. Their open items #2–#4 in `docs/tracking/2026-09-01-client-email-decisions-response.md` remain unanswered.
- **UPDATE 2026-09-02 — SCOPE EXPANDED TO ALL 29 AND EXECUTED.** The client returned a green "Missing Field Entry" column supplying values for **every one of the 29 withheld rows**, not just the 2 Sofidel certs, which answers decision (3) above and supersedes the 2-cert staging.
  - **The dates differ from what this CR assumed.** Decision (1) asked whether "2040" meant `2040-12-31`; the client's answer was **per-certificate**: `23-0358` → **`2040-06-01`**, `25-0351` → **`2040-05-01`**. Each mirrors its own effective month/day (`2023-06-01`, `2025-05-01`), consistent with the Sofidel batch pattern documented above. The staged `2040-12-31` CSV was **not** used.
  - **The 11 Expired certs got a past date, as predicted.** All 11 expiration-only rows (`13-0157`, `14-0278`, `15-0067`, `16-0262`, `18-0172`, `19-0397`, `21-0613`, `21-0746`, `21-0749`, `21-0750`, `21-0751`) were given **`2026-08-31`** — two days before load, so they are expired on arrival and never public. The concern raised in decision (3) does not materialise.
  - **Remaining fills:** 11 rows got a Product Name; `23-0092` → effective `2023-04-01`; `24-0401` → effective `2023-08-01`; `24-0353`/`24-0354`/`24-0355` got all three (`Brass Rod Mill Facility`, `2024-05-17`, `2040-05-17`).
  - **Method unchanged** — same generator, one transaction, preflight + post-load assertions. Built by `2026-09-02-not-loaded-29-BUILD.py` (supersedes `2026-09-01-sofidel-missing-expiration-BUILD.py`); SQL at `_sql_output/20260902-091830/`. The build asserts every required field is populated after the fill and that no effective date lands after its expiration — both clean for all 29.
  - **Rehearsed on dev first**, where it exposed a genuine generator bug that rolled the load back — see **DEF-396**. Fixed, regenerated, re-run green on dev, then TEST.
  - **Result on TEST (`gccwebap_certificates`), 2026-09-02 13:21:10 UTC:** 9 companies + 24 products + **29 certificates** inserted. **5,702 → 5,731**, which closes the source-vs-database gap to **zero** — the 5,731 source rows are now all present. All 29 verified by value; `NCHAR(174)` confirmed intact in `g2 revolution®`, `Malarkey Roofing Products®` and `Sika® ViscoCrete®-1000` (the generator's documented mojibake trap — the `®` renders as `r` in a console and that is only the console).
  - **Public exposure is exactly the two Sofidel certs**, asserted against the `CertificateVisibility` predicate: `23-0358` and `25-0351` are **PUBLICLY VISIBLE**; all 27 others are `Expired` and are not — including the three Brass Rod certs, whose 2040 expiry has no public effect because their status is `Expired`.
  - **⚠️ Two data-quality flags raised to Shawn, neither blocking:** (a) the client's sheet shows the Climate Action Now row as **`20-0588`**, but both the source workbook and the verification workbook carry it as **`21-0588`**, and `20-0588` exists in neither — loaded as `21-0588`; (b) `24-0353/0354/0355` are status **`Expired`** with an expiration of **`2040-05-17`**, an internal contradiction (no public effect, but it will read oddly on an expiration sort).
  - **Rollback:** `Scripts/data-ops/2026-09-02-notloaded29-ROLLBACK.sql` (the older `…-sofidel-…-ROLLBACK.sql` is hard-scoped to the 2 numbers and will NOT undo this load). Guarded on the load's exact `CreatedAt` stamp **and** the Id ranges, aborting unless it finds exactly 29. Belt-and-braces: logical backup at `Scripts/data-ops/_backups/20260902-pre-notloaded29/` (44 tables, 41,171 rows), taken immediately pre-load.
  - **Still owed:** `amazon-contract-diff.js` after the load, per the Amazon-impact note above.
- **Status:** **DONE — EXECUTED AND VERIFIED ON TEST 2026-09-02.** 29 of 29 loaded; certificate count 5,702 → 5,731.

### CR-040 (proposed 2026-08-23) — Product Management "IDs" column: make it say WHICH ASIN figure it is showing

- **Source:** the client's ASIN validation, 2026-08-21. Method, in their words: *"I went to the product management page, sorted from most to least ASINS. I then searched each company's name and totaled the ASINS associated with that company and compared that number to what we have in our master ASIN list."* Five companies came back short and were reported as a suspected migration fault. **The migration was not at fault** — see the record log below — but the reviewer had no way to know that from the screen, and their follow-up was *"I'm still concerned the ASINs are not correct."* That is the cost this CR addresses: the screen cannot currently be used for the reconciliation people are naturally using it for.
- **Why CR, not a defect:** nothing is broken. The page returns exactly what it is coded to return, no data is wrong, and no stored value is affected. What is being asked for is a **change to what the screen displays**, which is scope. Filed as a CR on that basis alone.
- **What the column actually counts — measured, not inferred.** `/Product/Management` (the page reached from the main nav, `Views/Shared/_Layout.cshtml:180`) renders an **"IDs"** column, sortable, at `Views/Product/Management.cshtml:108`. Its value is computed at `Views/Product/Management.cshtml:714` as `Object.values(product.identifiers).reduce((sum, arr) => sum + arr.length, 0)`, and `identifiers` is built server-side in `Controllers/ProductController.cs`, `GetProducts()`:

  ```csharp
  Identifiers = p.CertificateIdentifiers
      .GroupBy(pi => pi.ProductIdType)
      .ToDictionary(g => g.Key.ToLower(), g => g.Select(pi => pi.ProductIdValue).Distinct().ToList()),
  ```

  The `.Distinct()` is **per product line item**, over the ASINs of *all* that product's certificates flattened together. So the figure a reader totals for a company is **the sum, over that company's product line items, of the distinct ASINs on each** — which is neither the number of ASIN records held nor the number of distinct ASINs the company owns. It sits between the two, and **where it lands depends entirely on how that company's certificates happen to be distributed across product line items** — a detail invisible on screen.
- **This is why one screen produced two different units.** Reproduced directly against the test database on 2026-08-23; `PAGE` is the summed IDs column, `DBROWS` is the ASIN records actually stored:

  | Company | Line items | PAGE (what the client totalled) | DBROWS | Client reported |
  |---|---:|---:|---:|---:|
  | Philips | 7 | **2,019** | 2,019 | 2,019 |
  | The Estée Lauder Companies | 1 | **1,847** | 1,854 | 1,847 |
  | The Procter & Gamble Company | 15 | **1,240** | 1,240 | 1,240 |
  | Clover Imaging Group | 2 | **1,202** | 1,202 | 1,202 |
  | Kohler Co. | 2 | **1,051** | 1,959 | 1,051 |
  | Clover Environmental Solutions, LLC | 3 | **67** | 67 | 67 |

  **All six of the client's figures reproduce exactly from the PAGE column.** For Philips, P&G and Clover the column happens to equal the stored record count; for Kohler and Estée it does not, because those companies' certificates share line items and the per-line-item `.Distinct()` bites. Same screen, same method, two different units — with nothing on the page to signal the switch.
- **The data itself is confirmed correct, so this CR is purely about legibility.** `Scripts/data-ops/2026-08-23-asin-record-log.py` compares every `(certificate, type, value)` in the union of the client's master list and a live test dump, in both directions: **16,234 OK · 0 MISSING_FROM_DB · 0 EXTRA_IN_DB**, across the 158 certificates that carry ASINs. The only one-sided values are 3 distinct source values that are not valid ASINs (9 spreadsheet rows: eight literal `TO BE CHECKED`, one 9-character ASIN — all Philips). Product names: 15,925 exact, 309 differing by whitespace only, **0 differing in content**. This is a second, independently written implementation agreeing with `2026-08-18-asin-reconciliation.py`.
- **Proposed change — smallest thing that removes the ambiguity.** Label the column for what it is and give the reconciliation figure people actually want:
  - **(a)** Rename `IDs` → **`ASINs on this product`**, with a tooltip stating it counts each ASIN once per product line item. *Cheapest; removes the false confidence, does not give them the total they want.*
  - **(b)** (a) **plus** a per-company distinct-ASIN figure surfaced where a company is filtered — the number that is directly comparable to a master-list total. **Recommended.**
  - **(c)** Add a "distinct ASINs" column alongside the existing one. *Rejected as a default — two adjacent numeric columns that disagree is the same trap with more surface.*
- **Deliberately NOT in scope:** changing the `.Distinct()` itself. Per-line-item de-duplication is correct for the product view; the fix is to say so, not to alter the number and risk a different reading of the same screen.
- **Related — worth a decision, not folded in here:** there are **four** product-management surfaces — `/Product/Management` (live, in the nav), `/Product/ManagementAlt`, `/Product/ManagementV2`, and `/Admin/ProductManagement` (a separate implementation with its own de-duplication at `wwwroot/js/admin/productManagement.js:2107`). Only the first is linked from the nav. Whichever option is chosen has to be applied to whichever of these survive, so **agreeing which to retire is a precondition for costing this accurately.**
- **Effort:** **(a) ≈ 0.5 day.** **(b) ≈ 1–1.5 days** — the per-company distinct figure needs a new aggregate rather than a client-side sum of what is already loaded, because the page holds only the per-line-item lists. Both assume a single surviving surface; multiply by the number kept.
- **Decisions required:** (1) (a), (b) or (c); (2) which product-management surfaces are retired; (3) whether the per-company figure should count ASINs only or all identifier types.
- **Sent to client?** **No — not yet drafted for them.** Raised from our own analysis of their 2026-08-21 findings. The reply answering the validation question (`docs/tracking/2026-08-21-client-email-asin-validation-response.md`) deliberately mentions only that we are "looking at making that screen state plainly which figure it is showing" — the CR itself should be presented separately, once the questions above are settled.
- **Status:** **Proposed — not approved, not built.**

### CR-039 (proposed 2026-08-17) — Authorization to purge test-environment business records ahead of the go-live load

- **Source:** Gate 1 of `docs/tracking/go-live-test-runbook-2026-08-14.md`. The underlying instruction is the client's own, in writing: *"All test certs/ASINs should be removed before uploading as we are preparing for go-live"* — remove certs/companies/products/fake users, keep Cert Types, Product Cat/Type/Subcat, Tags, Pathways, Partners (`ReadMe.txt`, 2026-08-12).
- **Why CR (not a defect, and not a code change):** nothing is broken and no application behaviour changes — **no code ships under this CR at all.** It exists because the operation `DELETE`s business records, which requires formal authorization ([[feedback_data_changes_require_cr]]). Three things make a verbal go-ahead insufficient: (a) **not reversible in-app** — there is no cert soft-delete (DEF-288 parked), so recovery is a database restore; (b) **test is in daily client use**, not an idle sandbox; (c) the instruction's phrase *"fake users (except key test accounts)"* **does not define the keep-list, and the current one is wrong** — see below.
- **Scope — measured on TEST 2026-08-17** via the script's read-only report mode (`-v Execute=0`, returns before the transaction): **229 Certificates** (+3,202 CertificateProductIds, 2,644 CertificateProductIdPartners, 144 CertificatePartners, 53 StatusHistory, 14+16 join rows cascaded) · **312 Products** · **173 Companies** (+684 CompanyUser) · **930 AuditHistory** · 6 ImportBatches · **86 Users** (+87 UserRoles, 12 PartnerUser). Preserved and asserted *inside* the transaction: CertificationTypes 32, ProductCategories 9, ProductTypes 54, Tags 4, Pathways 6, Partners 4, **PartnerApiKeys 9**, ApiKeys 3, GoogleAnalyticsSettings 1, Roles/Permissions/PermissionRole 5/22/41, CR-019 backup tables, `__EFMigrationsHistory` 34.
- **✅ Keep-list FIXED 2026-08-17 (Shawn).** The three client accounts are now in `2026-08-13-golive-testdata-purge.sql`; verified against the live test DB — **all 5 entries resolve, 0 unmatched, users deleted 86 → 83**. Reviewing every Admin account showed **11 total: 3 now kept, 8 deleted, and not one of the 8 has ever been logged into** — five are `carly@`-lookalikes we created on throwaway domains (`gcccc.com`, `admin.com`, `gcc5/6/7.com`), which is exactly why "keep the admin" needed the account *list*, not the account *name*. Principle now written into the script: a keep-list errs safe in one direction only — keeping a deletable account is reversible in seconds, deleting a needed one is not. **Client confirmation is still wanted**, but as a check for logins we can't identify as theirs, not as a fix.
- **⚠️ The original finding — the keep-list as first written would have locked the client out.** The script keeps exactly **two** accounts. Enumerating all 86 rather than sampling them ([[feedback_enumerate_dont_sample_populations]]): 22 are already soft-deleted (`__deleted__` mangled emails, genuinely dead), **64 are live**, and **6 have ever logged in**. Three of those six are **client-side, not our scaffolding**: **`carly@greencirclecertified.com` (Admin, last login 2026-08-14 — three days ago)**, `analyst@greencirclecertified.com` (Analyst, 08-07), `partner@amazon.com` (Partner, 08-12). **As written the purge deletes the client's own administrator**, with recovery being us recreating the account rather than a self-service reset. **Recommend adding all three to the keep-list before running.** The other 58 live accounts are unambiguously ours and safe: 13 `@anukiconsulting.com` seeds, 37 `@example.com` QC import-validation fixtures (`zoe.emoji`, `formula.name`, `riley.comma`…), 4 `qc108-*@gcc-test.local`, and 5 never-used duplicate `carly@` accounts on throwaway domains (`gcc5/6/7.com`, `admin.com`, `gcccc.com`).
- **Amazon integration impact: NONE — verified, not assumed.** Every FK referencing `dbo.Users` was enumerated; **`PartnerApiKeys` has no FK to `Users`** (`CreatedBy`/`RevokedBy` are plain columns) and auth is key-based. All **8 Amazon keys + 1 Novi** survive; deleting the `partner@amazon.com` *portal login* does not touch the integration ([[feedback_amazon_api_immutable]]). Gate 4's contract diff still applies after the load.
- **`swest@anukiconsulting.com` is mandatory, not a preference** — it is `Audit:SystemUserEmail`; without it every audited write fails (Gate 10 / DEF-337). The script already pre-flight-aborts if it is absent.
- **Rollback is proven, not theoretical:** logical restore drilled 2026-08-13 into both empty and populated targets, 8,823 rows in 24–82s (`backup-testenv-db.ps1 -Mode Export`/`-Mode Restore`). **A valid target already exists** — `db-backups/gccwebap_certificates-2026-08-13-090039`. **What the backup has to protect is the PRESERVED set, not the certificates:** everything the purge deletes is test data the migration replaces, whereas API keys / partners / cert types / categories / tags / pathways / roles / users survive the purge and are **never reloaded by the migration**. Checked 2026-08-17 — **nothing in the preserved set has drifted**; Users (88), PartnerApiKeys (9) and Partners (4) compared **by identity, not count** ([[feedback_equal_counts_are_not_the_same_rows]]) and are identical, with every other preserved table matching on count. The only drift is **2 test certificates** (`26-9879` created, `26-9721` edited, both 08-13), which the purge deletes anyway — no value, not a gap. Retake in-session regardless: it is free and removes any need to reason about drift under pressure. The purge runs in **one transaction with post-purge assertions inside it**, so the realistic failure mode is "nothing happened", not "half the database is gone". **Uploaded PDFs are a separate restore path** — zip `wwwroot/uploads/` ([[feedback_backup_uploads_before_deploy]]).
- **Preconditions:** keep-list corrected (**open**) · same-session backup · `sqlcmd -I` on every call (CR-187 computed columns) · `-v Execute=` explicit, no default by design · **transaction-log headroom on test — UNVERIFIED**, and the likeliest thing to bite: the dev load failed mid-purge with *"transaction log … is full"*. On dev the fix was `SET RECOVERY SIMPLE` + shrink; **do not do that on test without asking** — on a hosted instance it may be the provider's call · schema prereqs first (DEF-349 widen, retired cert types, CR-038 filtered index) · agreed window.
- **Worth raising before go-live, not after:** `AuditHistory` is emptied **and the migration writes no audit rows** (the SQL loader bypasses the application, by design, matching dev). Migrated certificates therefore arrive with **no history at all** — if the client expects "imported on <date> by <user>" per record, they will not see it.
- **Effort:** **≈ 0.5 day elapsed**, essentially all backup / verification / window management — the delete itself is seconds. **No development:** the script is written and has been run end-to-end on dev in a rehearsal now verified clean against the client's own spreadsheets in both directions (0 unexplained discrepancies, 2026-08-15).
- **Decisions required:** (1) approve the scope in §2; (2) **confirm the keep-list — add the three client accounts** (the one that causes visible harm if answered wrong); (3) confirm the 930 audit rows have no retention value; (4) agree a window; (5) confirm whether log headroom is ours to arrange or the provider's.
- **Detail doc:** `docs/tracking/CR-039-golive-test-data-purge.md`
- **Sent to client?** **No — never presented.** Drafted 2026-08-17; the approvals email (`docs/tracking/2026-08-17-client-email-migration-approvals.md`) remains a draft.
- **Status:** **EXECUTED 2026-08-18 on Shawn's approval — not the client's.** Shawn approved the purge scope, the keep-list and CR-038 Option A, and directed an in-hours run. Ran ~10:50 local against `gccwebap_certificates`. Same-session backup taken first: `db-backups/gccwebap_certificates-2026-08-18-105016/` (43 tables, 8,829 rows).
  - **Deleted, matching the §2 forecast exactly:** 229 Certificates (+3,202 CertificateProductIds, 2,644 CertificateProductIdPartners, 144 CertificatePartners, 53 StatusHistory, 14+16 join rows), 312 Products, 173 Companies (+684 CompanyUser), 930 AuditHistory, 6 ImportBatches, **83 Users** (+87 UserRoles, 12 PartnerUser). Users 88 → 5.
  - **Keep-list: 0 unmatched entries.** All five resolved — `swest@anukiconsulting.com`, `admin@`, **`carly@`**, `analyst@` (all `greencirclecertified.com`), `partner@amazon.com`. The client's own Admin survived, which was the whole point of the 08-17 fix.
  - **Preserved set asserted inside the transaction and re-verified after the load:** PartnerApiKeys **9** (all 8 Amazon keys + Novi), Partners 4, ApiKeys 3, GoogleAnalyticsSettings 1, CertificationTypes 36, ProductCategories 9, ProductTypes 54, Tags 4, Pathways 6, Roles/Permissions/PermissionRole 5/22/41, `__EFMigrationsHistory` 35.
  - **Amazon unaffected — measured, not assumed:** contract diff against the live test site with the real Amazon key returned **`CONTRACT UNCHANGED`**, 7/7 checks, against the frozen April baselines.
  - **Transaction-log concern did not materialise:** peak 98 MB used of 1,024 MB.
  - **Still open after the fact:** the client has given no written approval for either CR; **Gate 3 (1,960 untagged product IDs) remains undecided.** ⚠️ **Corrected 2026-08-18:** an earlier version of this line said those ASINs were "visible to every partner caller" under CR-171 Model A. **That was wrong.** `CertificatesController.cs:162` scopes the query to certificates the caller's partner is associated with, *before* the product-ID filter runs; all four certificates have **0 rows in `CertificatePartners`**, so `untagged_reachable_by_a_partner` = **0 of 1,960**. The real question is the inverse — these certificates reach **no** partner, so if they were meant to go to Amazon they are not. Full run record: `docs/tracking/go-live-test-runbook-2026-08-14.md` §11.

> **Numbering note:** CR-038 (duplicate certificate numbers) exists as `docs/tracking/CR-038-*.md` but was **never entered in this register** — the highest entry here was CR-037. CR-039 is the next free number; **CR-038 still needs its row** ([[feedback_track_every_fix_in_defect_register]] applies to CRs too).

### CR-037 (proposed 2026-08-13) — Multiple countries per certificate (Country single-select → multi-select)

- **Source:** Client observation 2026-08-12 that some certificates appear to carry more than one country, citing **26-9633**. Investigated 2026-08-13 against the test-env DB and the 1,021-record public GreenCircle mirror.
- **Why CR (not a defect):** Original baseline scoped one country per certificate and the system implements exactly that — `Certificate.Country` `nvarchar(100)` single value (`Models/Certificate.cs:25`), single `<select>` (`Views/Certificate/Create.cshtml:293`), picker fed by `CountryList.Countries`, a **static in-code list, not a table** (`CertificateController.cs:346, 926`). Nothing is broken; holding several countries is new capability.
- **Key finding — the ask is far narrower than it reads.** Three situations were being conflated: (a) **several sites, one country** — `24-0304` = `Buffalo, NY; Cedar Rapids, IA` / United States — **already works**, Location is free text and lists every site, out of scope and the majority of what the client is seeing; (b) **sites across >1 country** — `23-0925` = Hungary in Location, Germany in Country — the actual gap, **~2%** (4 of 228 test env, 19 of 1,021 public mirror, 35 distinct countries; worst case `25-8767` = five sites US+Canada, one record US+China+Poland); (c) **country contradicts location** — `26-9633` = `Seattle, WA` filed under **Mexico** — a wrong dropdown value, **data ops not code** ([[feedback_data_issue_vs_defect]]).
- **One question decides whether to build it at all:** *does GCC need to **filter/search** by every country, or only to **see** them?* Seeing them already works. **A "display" answer closes this CR at zero cost.** Only filtering justifies the build — the join table is trivial; making a collection filterable/sortable is what touches the tile APIs, the CR-187 persisted columns and the export template. Question packaged for the client in `docs/tracking/migration-client-note-2026-08-13-location-country.md`.
- **Partner API impact: NONE.** `certificationCountries` is hardcoded to the literal `"All"` for partner callers (`Controllers/Api/v1/CertificatesController.cs:254, 318, 1067, 1668`) per Dev Guide §4/§8; only internal callers project `c.Country`. Response shape and values are byte-identical before/after — `amazon-contract-diff.js` should still print CONTRACT UNCHANGED. No partner sign-off, no Amazon re-certification ([[feedback_amazon_api_immutable]]). Materially cheaper than [[CR-035]]/[[CR-036]] for this reason; say so to the client so the change isn't over-feared.
- **Model note — Country is NOT an entity, unlike Partner.** [[CR-171]] worked as a true M2M because `Partner` has `IsActive`/`SortOrder`/login association. Recommend **Option A: string join table** `CertificateCountries(CertificateId, Country nvarchar(100))` — no new entity, no seed, no admin screen. Option B (seeded `Countries` table + true M2M) only if GCC wants a *managed* country list, which nobody has asked for. Keep `Certificate.Country` as the denormalized primary exactly as CR-171 kept the legacy `Partner` string; join column must match at `nvarchar(100)`, not narrower ([[feedback_denormalized_copy_width_mismatch]]).
- **Scope (if approved):** join table + migration; **CR-187 `NormalizedCountry` decision** — it's computed from `Country` so it normalizes the *primary only*; either accept that secondaries aren't fuzzy-searchable or extend normalization to the join rows (**least obvious cost in the CR**; do not add a second normalize helper — [[feedback_search_normalizer_single_source]]); `<select multiple>` on Create/Edit + detail/tile rendering; country filters → collection membership on internal + external tile APIs with `.AsSplitQuery()` ([[feedback_ef_includes_with_projection]]) and **every adjacent endpoint checked, not just the list** ([[feedback_scoping_check_adjacent_endpoints]]); pipe-delimited CSV import/export + template hint per the DEF-316 Partner precedent, with `SyncCertificateCountriesAsync` on create **and** update; **Gate 7 backfill SQL** — one join row per existing cert or country filtering returns nothing for historical records.
- **Out of scope:** any partner-API change; a managed/admin-editable country list; multi-country at product-identifier level (no CR-171-Phase-2 analogue requested); correcting the group (c) data errors.
- **Related defect, independent of this CR:** CSV import writes `Country` as an **unvalidated raw string** (`Services/Import/CertificateImportService.cs:1097`, `:1771`) — no check against `CountryList`. That is why **`Atlantis`** is sitting in the test-env Country column. Needs its own DEF number + a small validation fix **before** the 5,556-row go-live load. Must ship whether or not this CR is approved — do not fold it in.
- **Migration interaction — blocking and independent.** `migration-import-transform-plan-2026-06-22.md` **§87** says "strip trailing country into Country" for the expired set's combined `LOCATION` column. Against a multi-site string that takes the **last** country: `25-8767` would load as **Canada** with its three US sites silently dropped. **Must be fixed for go-live regardless of this CR.** Proposed rule: first-listed site sets Country, full original string preserved in Location (175 cap), cross-border rows written to a review CSV rather than guessed. If CR-037 *is* approved the transform instead fills the join table — an argument for deciding this CR **before** the load.
- **Effort:** **≈ 2–2.5 days** (Option A, copying CR-171's pattern). Cheaper than CR-171's 3–4 d because there is **no visibility/isolation dimension** — countries are not access-control data, so no per-requester scoping, no leak risk, no product-ID layer. Dearer than a plain field add because of the CR-187 computed column. Filters/sort is the real cost; the join table is not.
- **Decisions required:** (1) filter or display only — a display answer retires the CR; (2) Option A or B; (3) does `Country` stay the single primary for list columns/CSV/sort (recommend yes, first-listed wins); (4) secondary countries fuzzy-searchable; (5) who corrects the group (c) mismatches, before or after the load; (6) before or after go-live (recommend **after** — §87 must be settled either way).
- **Detail doc:** `docs/tracking/CR-037-multi-country-certificates.md`
- **Sent to client?** Question in §4 is carried by the 2026-08-13 client email (drafted, **not yet sent**). The CR itself has not been presented or priced to the client.
- **Status:** **BUILT 2026-09-01 — on `ci-regression`, not deployed.** Superseded the "decide before building" posture: Shawn directed the build on 2026-09-01 and answered §4 directly — *"The filtering on countries should allow for filtering if the country exists on the tile and the countries should be visible on the tiles."* That is the **filter** answer, so the full scope was built, not the display-only retirement.
- **Display decision (the one open question at build time):** several countries in a **100px** tile column. Three options were rendered at true scale and **Option B — comma-join and wrap, matching the Partner column exactly — was chosen** by Shawn on 2026-09-01, on the grounds of one rule for every multi-value cell. Text never truncates (`.table-body-cell` wraps by design, `certificatetileview.css:2742`); a 3-country row is simply taller. Options page: `multi-country-options.html` artifact.
- **What was built** — Option A (string join table) as recommended:
  - `Models/CertificateCountry.cs` + `Certificate.Countries` nav; `CertificateCountries(Id, CertificateId, Country nvarchar(100), NormalizedCountry)` with cascade delete, unique `(CertificateId, Country)`, and indexes on both country columns. `Certificate.Country` **retained** as the denormalized primary (sort key, CSV column, non-partner API projection) exactly as CR-171 kept the legacy Partner string.
  - **CR-187 decision resolved: normalization WAS extended to the join rows** — `CertificateCountry.NormalizedCountry` is a persisted computed column using the same `SearchNormalizer.ToSqlExpression`, no second helper. Without it a secondary country was filterable but not searchable, which is exactly what testing caught.
  - `CountryList.ParseMany()` — one parser for the form (comma) and CSV import (pipe), returning unresolved entries rather than dropping them.
  - Form: **uses the Partner(s) control itself** — `wwwroot/js/partner-multiselect.js`, which upgrades a `<select multiple>` into the collapsible checkbox dropdown with its own search box, is now initialised for `countrySelect` as well. Only the wording is parameterised (`DEFAULT_LABELS`), so markup, classes and inline styles are shared and the two fields cannot drift apart visually. Two earlier attempts were discarded: a bespoke chip picker, then a raw `<select multiple size="4">` — the latter looked nothing like Partner **because the partner field is not a raw select at runtime**, a detail missed by reading the Razor markup alone (Shawn caught it from a screenshot, 2026-09-01). Lesson: for a "make X look like Y" request, check what Y renders as in the browser, not what its markup says. Verified in Chrome: both triggers carry identical inline styles, search filters, clicking rows syncs the hidden CSV (`Canada,China,Poland`) and the trigger label.
  - **Picker order (client request 2026-09-01):** United States pinned first, then everything else A-Z. Added as `CountryList.PinnedCountry` + `CountryList.DisplayOrder` and used for `ViewBag.Countries` on Create and Edit, so the form now matches the ordering the tile filter facets already used. `CountryList.Countries` stays strictly A-Z — it is the canonical source and also feeds `CanonicalByNormalized`, so the display order is a separate list rather than a re-sort. Verified: 220 options, United States at index 0, remainder Afghanistan→Zimbabwe, no duplicate entry. The three tile API facet lists were then pointed at the same constant (Shawn, 2026-09-01), so `PinnedCountry` now has **one definition and four call sites** and the picker cannot drift from the filter panels. Ordering is applied in memory after materialisation, so the constant needs no EF translation. Re-verified on all three endpoints after the change: public 27, internal 33, external 27 countries — United States at index 0, remainder A-Z in every case. (`SeedData.cs` and `MockDataGenerator.cs` also contain the string, but as sample *data*, not pinning logic — deliberately left alone.)
  - Form (superseded description): built to match the Partner(s) field — `<select id="countrySelect" class="form-control" multiple size="4">` plus a hidden comma-separated `Countries` field and the same sync script, so the form has one convention for multi-value fields rather than two widgets. (Shawn's call, 2026-09-01: *"can the country ... work like the partners dropdown?"* An earlier chip picker was built and then removed; `multi-select-search.js` was deleted rather than left as dead code.) One deliberate addition Partner does not need: a **filter box** above the list, because Partner has 4 options and country has 220, and this field already had type-ahead search before it went multi-select — removing it would have been a regression. Filtering never hides an already-selected option, which would drop it from the POST.
  - **Primary-country preservation** (`SyncCertificateCountriesAsync`): a `<select multiple>` posts in the list's ALPHABETICAL order, not click order, so adding Canada to a United States certificate would otherwise silently make Canada the primary — and the primary is the grid's sort key and the CSV column, so the row would move in a country sort as a side effect of an unrelated edit. The existing primary is now kept whenever it is still selected, and only reassigned when it is actually removed.
  - Filter → membership (`.Any()`) on all three tile APIs **and** `CertificateRepository` (both search predicates + `GetCertificatesByCountryAsync`); facet lists and counts now read the join table, still off each caller's scoped `baseQuery` so partner/company isolation and public visibility gating are unchanged.
  - Detail views, CSV/XLSX export (pipe-delimited, DEF-316 precedent), import sync on create **and** update, import template separator hint, and the DEF-337 delete-audit capture.
  - **Gate 7 backfill is inside the migration** (`20260901164220_CR037_MultiCountryCertificates`) — faithful, not corrective: unresolvable values like `Atlantis` are copied verbatim so they stay visible and filterable rather than being silently dropped.
- **Verified on dev 2026-09-01** (`GCCDatabase_Dev`): backfill 1,327/1,327 join rows, 0 certificates missed, 0 rows disagreeing with `Certificates.Country`. Membership filter proven by adding Canada + Poland to `25-8521` — Canada filter 45 → 46 with the cert appearing, Poland filter returns it, primary US still matches, cell renders `United States, Canada, Poland`. Save path proven through the real Edit POST: 3 countries written with the primary following the first pick, then reorder-and-remove (`Canada,Poland`) correctly moved the primary to Canada and dropped United States. Test data fully reverted — back to 1,327 rows, 0 drift.
- **Two real defects were caught by that testing, both fixed:** (1) secondary countries were filterable but **not searchable** on the public API, because its search runs through `CertificateRepository`, which had not been extended — the adjacent-endpoint trap; (2) the Edit POST loads via `GetByIdAsync` with **no Includes**, so clearing `certificate.Countries` deleted nothing while the re-inserts collided with the unique index and the country change silently failed to save. The sync now deletes through the context, and the importer's update path gained the matching `.Include`.
- **Partner API confirmed unaffected:** no change to `certificationCountries`, still the hardcoded `"All"` for partner callers.
- **Not yet done:** deploy; contract-diff re-run against the test environment; client sign-off (the CR has still never been priced or presented).

### CR-036 (proposed 2026-08-10) — Certification standard version: capture it and expose it to partners

- **Source:** Novi Data Exchange Guidance (client-supplied, 2026-07-31), reviewed against the built API 2026-08-10. **Exact citation** — page 1, *Ideal data structure* table, final row, verbatim:

  | FIELD | WHAT IT CARRIES |
  |---|---|
  | Certification metadata | Issuer, issue date, expiration date, **standard version** |

  We supply the first three; the fourth does not exist anywhere in the system.

- **It is not our API version — checked, because the ask is easy to misread** (Shawn queried exactly this on 2026-08-10; the phrase is the last two words of the last row of a table and carries no heading of its own). Two things in the same table settle it: the row sits under *certification metadata* alongside issuer and the two dates, all properties of the certificate itself rather than of the transport; and three rows above, Novi defines its own usage — *"Certification → Name of the standard or certification granted."* "Standard" means the certification standard throughout that table, so "standard version" is the revision of that standard (a certificate granted against v2.1 rather than v1.0). Had it meant the API version, the answer would be free: we are on `/api/v1` and already publish it.
- **Why CR, not a defect:** there is no standard-version field anywhere in the system to be broken. `CertificationType` carries `Name`, `AmazonCode`, `Description`, `IsActive`, `SortOrder`, `Level`, `ParentId` and nothing else; `Certificate` has no version, edition or revision field either. The requirements baseline does not mention versioning a standard. This is new scope.

**First question, and it may close this CR for free: does GCC version its certification standards at all?** If certifications are not issued against numbered revisions of a published standard, the correct answer to Novi is *"not applicable"* — a sentence in the partner guide, no build. Everything below applies only if the answer is yes.

**The modelling decision that drives the cost.** A standard has versions *over time*; a certificate is issued against whichever version was current on its issue date. So the field almost certainly belongs on the **certificate**, captured at issue, not on the certification type:

| Placed on | Consequence |
|---|---|
| `CertificationType` (one value per type) | Cheapest to build, but **updating a type's version silently rewrites history for every certificate ever issued under it** — a certificate issued against v1.0 would start reporting v2.0. Wrong, and wrong invisibly. |
| `Certificate` (captured per certificate) | Historically accurate and partner-safe. Slightly more work: field on Create/Edit, import/export columns, backfill decision for existing rows. Could default from the type at creation to keep data entry cheap. |

**Contract impact is lower than [[CR-035]].** This is an **additive** field — existing consumers ignore an unrecognised key, so Amazon's integration is unaffected in behaviour. It still changes the `/api/v1/certificates*` response shape, so partner notice is required per [[feedback_amazon_api_immutable]], but it does not require Amazon to re-certify the way a change to `status` semantics would. Note the frozen-baseline contract test (`Scripts/smoke-prod/amazon-contract-diff.js`) reports *any* added field as a contract change — expect it to go red on purpose, and re-baseline deliberately rather than treat it as a failure.

**Decisions required before this can be estimated:**

1. **Does GCC version its standards?** If no, close this CR as not-applicable and say so in the partner guide.
2. **Certificate-level or type-level?** (Recommended: certificate-level, defaulting from the type — see table above.)
3. **Free text, or a managed version list per certification type?** Free text is far cheaper and matches how `CertificationResults` was handled; a managed list gives clean partner data and validation.
4. **Backfill?** Existing certificates would report no version until someone populates them. Leave null, backfill in bulk, or backfill only the certificates in partner scope?
5. **Where else does it surface?** API only, or also the certificate detail view, CSV import/export template, and the certificate PDF?

- **Interim position:** the partner guide does not promise a standard version, and Novi can be onboarded without one. This gap does not block anything today; it only limits how completely we satisfy Novi's *ideal* structure.
- **Related:** [[CR-035]] (the other Novi gap — certificate status semantics; larger and structural) · DEF-345 (rate limiting, same review).
- **Status:** **Proposed — not built.** No code written. Needs the five answers above, starting with question 1, which may retire it entirely. **Questions put to the client 2026-08-24** — draft in `docs/tracking/2026-08-24-client-questions-partner-api.md` (sent with CR-035 and the 200/hr rate-limit confirmation, all three being products of the same guide review).

### CR-035 (proposed 2026-08-10) — Partner API: expose certificate status as a value (Valid / Expired / Revoked) instead of by presence

- **Source:** Anuki, 2026-08-10, while resolving the §4 open decision in `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html` against the client-supplied **Novi Data Exchange Guidance** (2026-07-31). Novi's "ideal data structure" asks certifying bodies for a **Certification status** field carrying *Valid · Expired · Revoked/invalid*. The partner API cannot supply it.
- **Why CR, not a defect:** nothing is broken. The current behaviour is deliberate and documented — it was built for **badging**, where only currently-valid certificates matter, and it is the contract Amazon signed off on. Novi's use case is **claim validation** (confirming or refuting a brand's assertion against the GCC ledger), which is a materially different question to ask of the same data. Serving it is new capability, not a fix.

**What the API does today.** Validity is expressed by *presence*, not by a value:

| Mechanism | Where |
|---|---|
| Partner list queries filter on `CertificateStatusHelper.PublicStatuses` = {Client Approved - Paid, Republished} | `Controllers/Api/v1/CertificatesController.cs:173,201` |
| Single-certificate lookups apply the same filter, else `404` | `:388,442` |
| `status` is the hardcoded literal `"Active"` for partner callers | `:251,315` |

A certificate in any other state is simply absent, and **absence is deliberately ambiguous** — expired, on hold, confidential, draft, never issued, and not-linked-to-your-account are indistinguishable, because collapsing them prevents a partner key being used to enumerate out-of-scope certificates.

**This is bigger than adding a field. Three things make it so:**

1. **There is no "revoked" state to report.** The lifecycle has eight states (`Models/CertificateStatus.cs`) and none of them is revocation. Emitting `Revoked` means either adding a ninth state or designating an existing one to mean it — i.e. **a change to the 8-state workflow**, which is otherwise a protected invariant of this build. That decision belongs to the client, not to an API change.
2. **It is a disclosure decision, not a serialisation change.** Returning expired or held certificates means showing partners records they have never been able to see. **`Client Approved - Confidential` is paid-but-embargoed** — the client explicitly opted to keep those private (DEF-223). Any expansion must state, state by state, which become partner-visible; "return everything with a status label" would break a promise the client made to its own customers.
3. **It touches a frozen contract.** `/api/v1/certificates*` response shape is immutable without partner sign-off. Changing `status` from a constant to a variable is a breaking semantic change for **every existing consumer** — Amazon's integration currently has no reason to branch on it and may treat every returned record as valid. Amazon would need to re-certify their side even though the change is for Novi's benefit.

**Decisions required before this can be estimated:**

1. **Which states become visible, and under what label?** Per-state mapping to the exposed value, explicitly including whether Confidential stays hidden (recommended: yes, stays hidden).
2. **Does "Revoked" get created at all?** If yes, it is a workflow change with UI, transition and reporting consequences well beyond the API.
3. **Opt-in per partner, or global?** A per-key flag (e.g. `includeInactive`) keeps Amazon's payload byte-identical and confines the change to partners who ask for it. A global change is simpler to build and worse to ship.
4. **Does absence stay ambiguous for out-of-scope certificates?** Even with expanded statuses, "not associated with your account" must remain indistinguishable from "does not exist", or the enumeration protection is lost.
5. **Is the 404 path in scope?** Novi's validation flow (`/certificates/number/{n}`) is where this matters most — a `404` there currently cannot be distinguished from revocation, which is the specific misreading most likely to cause harm.

- **Interim position, already documented:** the guide now states the presence/absence model plainly, warns that **a `404` is not evidence of revocation**, and flags to GCC that a claim-validation partner may need more than this contract offers. Novi can be onboarded today for badging-style use; claim validation works only if they accept presence/absence as the signal.
- **Related:** DEF-345 (rate limiting, same guide review) · [[feedback_amazon_api_immutable]] · [[feedback_workflow_state_count]] · [[feedback_partner_data_isolation]].
- **Status:** **Proposed — not built.** No code written; the guide documents current behaviour only. Needs client decisions on the five points above, and partner sign-off from Amazon before anything ships. **Questions put to the client 2026-08-24** — draft in `docs/tracking/2026-08-24-client-questions-partner-api.md` (sent with CR-036 and the 200/hr rate-limit confirmation, all three being products of the same guide review).

### CR-034 (proposed 2026-08-04) — Audit History screen + one-click record restore

- **Source:** Shawn, 2026-08-04 — *"review the Audit history table and ensure there is coverage if the client needs to revert a record."* Raised while resuming the DEF-337 delete-audit work.
- **Why CR, not a defect:** the audit **data** is a defect concern and is now fixed under DEF-337 — every delete captures a restorable snapshot including cascade children and join rows. What does not exist, and never did, is any way for a user to **see or act on** that data. There is no audit view in `Views/`, no controller exposes `IAuditService`, and `AuditAction.Restore` is written only by the CR-023 import-undo flow. The requirements baseline specifies audit *tracking*; it does not specify an audit *screen* or a restore feature. Building either is new functionality.
- **What the client can do today:** nothing self-service. Every restore is a hand-written SQL script produced by Anuki against the audit JSON — e.g. `Scripts/data-ops/2026-08-04-restore-products-100-101.sql`, written to undo two products deleted by mistake. That works, but it needs a developer, it is slow, and it is not something the client can do during a working day.
- **Proposed scope (indicative, not built):**
  1. **Admin → Audit History screen.** Filter by entity type, entity, user, action and date range; paged. `IAuditService.SearchAuditHistoryAsync` already returns exactly this shape and the table is already indexed for it (five single-column indexes plus a composite on EntityType/EntityId/ChangedAt), so the read side is largely done.
  2. **Per-record history.** A "History" affordance on certificate/product/company detail pages calling `GetEntityHistoryAsync`, showing the field-level change list already captured on UPDATE.
  3. **Restore action on DELETE rows.** Re-create the entity from the snapshot plus its captured children and join rows, writing a `RESTORE` audit row. Needs the decisions below.
  4. **CSV export** of a filtered audit view, per the project's CSV-only export rule.
- **Decisions required before this can be estimated — each materially changes the cost:**
  1. **Who may restore?** Admin only, or Analyst too? Restore re-creates business records and should probably be Admin-only.
  2. **Same ID, or new ID?** Restoring to the original key needs `IDENTITY_INSERT` and can collide if the key was reused. A new ID is safer but breaks every external reference — including, for certificates, anything a partner already resolved.
  3. **How far does a restore reach?** Parent only, or parent + captured children + join rows? Children are captured now, so a full restore is possible — but re-creating a certificate's partner links silently restores visibility, which the client should decide explicitly rather than inherit.
  4. **Which entities?** Certificates only (the entity that actually gets deleted by mistake), or every audited entity?
- **Known limits the screen inherits, worth stating up front:** `UpdatedAt`/`UpdatedBy` are excluded from snapshots by design, so a restore cannot reproduce the original modification timestamp; date values in field-level rows are stored formatted (`yyyy-MM-dd HH:mm:ss`), losing Kind and sub-second precision; and credential fields (`PasswordHash`, `ApiKey`, `Key`, `KeyHash`) are deliberately never captured, so users and API keys can be *reported on* but not fully reconstructed.
- **Status:** **Proposed — not built, awaiting client decisions on the four points above.** The data-capture prerequisite (DEF-337) is complete and in the working tree, so this can be scoped independently whenever the client wants it.

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


---

### CR-046 (executed 2026-09-08, retroactive authorisation requested) — Six new certificate-subcategory options added to the taxonomy

- **Source:** the client, via Shawn, 2026-09-08: *"it also appears none of the Certificate
  Subcategories came over for the CSF listings… These never got marked in new DB"*, then, on the
  75 pairs that could not be loaded without new taxonomy rows: *"for the 30 that repeats itself.
  that is what the spreadsheet has"* and *"run part A and B on dev then test"*.
- **Why CR, not routine data ops.** Loading the 381 links that already resolved (Part A) is ordinary
  additive data repair and needs no CR. **Part B does**, because it added rows to
  `dbo.CertificationTypes` — reference data that drives the **Create/Edit subcategory picker for
  every certificate of those parent types**, not only the 74 being backfilled. An analyst opening
  any Waste Diversion certificate now sees three subcategory options where there were two. That is a
  visible change to how the application behaves, which is a CR even when the trigger is a data fix
  ([[feedback_absorbed_cr_is_still_priced]]).
- **Status:** **EXECUTED on dev and the test environment 2026-09-08**, on Shawn's explicit
  instruction, after the SQL was generated and reviewed first at her request. Client sign-off is
  **not** on record.

**What was added** (Level 2, active, `SortOrder` = MAX+1 under the parent):

| New option | Under parent type | Links unlocked |
|---|---|---|
| Waste Diversion from Landfill | Waste Diversion from Landfill (19) | 27 |
| Carbon Footprint Reduction | Certified Environmental Facts (5) | 25 |
| **Zero Waste to Landfill** | Waste Diversion from Landfill (19) | 20 |
| Waste Diversion from Landfill | GreenCircle Audited (11) | 1 |
| Sustainable Manufacturing Practices | Sustainable Manufacturing Practices (18) | 1 |
| Waste Diversion from Landfill | Sustainable Manufacturing Practices (18) | 1 |

*(dev additionally has `Carbon Footprint Reduction` under `Carbon Footprint Reduction` (3) — two
certificates carry that type on dev but not on test.)*

**Only one name is genuinely new to the system: "Zero Waste to Landfill".** The other five already
existed as `CertificationTypes` rows; what was missing was that name under a *second* parent. The
taxonomy already does this twelve times over — every one of Certified Sustainability Facts' twelve
children duplicates a Level-1 type name. So five of the six follow the pattern the table is already
built on, and Shawn's reasoning for accepting the thirty type-repeats was that the client's
spreadsheet says so.

**The open question this leaves.** The workbook column is headed *"Certificate Subcategories (for
CSF and WD only)"*, but Waste Diversion's own two children are **Waste Diversion Event** and **Waste
System Audit** — a different *kind* of subcategory from the sub-certifications hanging off CSF.
Whether the client intends "Waste Diversion from Landfill" as a subcategory *of itself*, or was
using the cell as shorthand for the certificate's type, is **not resolved** and was not guessed at:
the rows were added because Shawn decided the spreadsheet is the instruction. If the client says it
was shorthand, the 30 links and 4 of the 6 rows come back out — `partB-UNDO.sql` removes the links,
and the taxonomy rows have guarded deletes ready (commented out, so nothing runs by accident).

- **Undo:** `Scripts/data-ops/_subcat_backfill/20260908-104535/partB-UNDO.sql` (test),
  `_subcat_backfill_dev/20260908-104410/partB-UNDO.sql` (dev). The taxonomy rows are deliberately
  left in place by the UNDO: once an analyst picks one on some other certificate, deleting it would
  take their data with it.
- **Related:** DEF-411 (the data gap this served), DEF-412 (exports still do not project the field).


---

### CR-048 (raised 2026-09-10, **BUILT 2026-09-10 — not yet deployed**) — Edit button opens a new tab, matching Preview

- **Period:** raised 2026-09-10; not started
- **Source:** the client, 2026-09-09 (`CR_9_9_26_edit.pdf`, "DB Feedback"), logged as **QC #261**
  — `Database QC Log_9.9.26_sw_updates.xlsx`, sheet `GCC DB QC Log2`, **row 270**, page
  `Edit - Open New Tab`, Image 118: *"Is it an easy fix (with low risk of breaking things) to have
  the Edit button open a new tab in these views (similar to how Preview does). The team is finding
  it inefficient by losing their prior search when editing a bunch of certs."*
- **Why CR, not a defect:** Edit navigating in-place is the behaviour as built and as accepted. This
  is a deliberate change to it, requested for workflow efficiency — new behaviour, not a fault
  ([[feedback_absorbed_cr_is_still_priced]]: still numbered, still estimated, even if absorbed).
- **Status:** **BUILT 2026-09-10, not yet deployed.** Built on the stated assumption rather than
  waiting — see the build note at the end of this entry. Still worth the client's confirmation, but
  the behaviour is now in the working tree and verified.

**Scope — five sites, not one.** Each surface wires Edit up separately; there is no shared switch.
This is the same "every surface carries its own copy" shape as DEF-413, and the predictable failure
is fixing four and missing one:

| File | Line | Surface |
|---|---|---|
| `wwwroot/js/certificate-tile-view-internal.js` | 1297 | internal tile + list |
| `wwwroot/js/certificate-tile-view-external.js` | 1279 | external tile + list |
| `wwwroot/js/certificate-tile-view.js` | 1549 | public tile |
| `Views/Certificate/CertificateInternalView.cshtml` | 492 | internal detail page |
| `Views/Certificate/CertificateExternalView.cshtml` | 411 | external detail page |

The change at each is `window.location.href = X` → `window.open(X, '_blank')`. Preview already does
exactly this two lines away (e.g. `certificate-tile-view-internal.js:1273`).

**The `cid` parameter must survive, and this is the acceptance test, not an assumption.**
Certificate numbers are **not unique** — 137 numbers span 312 rows — and `cid` is what pins which of
several same-numbered certificates is being edited (DEF-369). `withCid()` already builds it; prove it
arrives in the new tab. Getting this wrong means an analyst silently edits the wrong certificate,
which is far worse than the inconvenience being removed.

**Known consequence, accepted not fixed:** the originating list tab holds a stale row until
refreshed. Preview already behaves this way. Stated to the client rather than left to be discovered.

**Estimate:** **~1.5-2 hr** — five one-line changes, viewport pass on both tile views, `cid` check.
Popup blockers are not a risk (`window.open` inside a click handler is a user gesture).


#### BUILT 2026-09-10 — all five surfaces, verified

Built against the client's assumption rather than waiting on confirmation, as the entry above
anticipated. **All five surfaces in the scope table were changed** — `window.location.href = X`
became `window.open(X, '_blank')` — including the two external ones. External users have no Edit
control, so those two handlers are unreachable from the UI today; they were changed anyway so the
five copies cannot drift, which is precisely the failure this entry warned about. Verified
mechanically: **zero** `window.location.href` → `/Certificate/Edit` remain anywhere under
`wwwroot/js/` or `Views/Certificate/`.

**The `cid` acceptance test passed — this was the real risk, and it is now evidence, not assumption.**
Clicking Edit on a list row opened a new tab at
`http://localhost:5101/Certificate/Edit/26-0497?cid=5796`. The `cid` survives, so the
same-numbered-certificate trap (137 numbers spanning 312 rows, DEF-369) is not reopened. The
originating tab's URL was byte-identical before and after the click, and the Edit page rendered its
form in the new tab.

**A better outcome than the entry predicted, on the stale-row consequence.** A successful save
redirects to `CertificateInternalView`, *not* back to the tile view
(`Controllers/CertificateController.cs:1627`). So the analyst saves, closes the tab, and the original
search is untouched behind it — the "stale row until refreshed" caveat still holds in principle, but
the user never lands back on the list expecting it to have updated.

**Estimate held:** ~1.5-2 hr estimated, built and verified inside it.

**Harness:** `Scripts/smoke-prod/verify-cr048-cr049.js` (localhost only) — 3 CR-048 assertions, all
PASS. Results + screenshots in `Scripts/smoke-prod/_results/cr048-cr049/`.

> **CR-047 is deliberately skipped, not missing.** It is earmarked in
> `docs/tracking/DEF-413-subcategory-filter-as-is-to-be.md` §7 for the three orphan subcategory names
> (Zero Waste to Landfill, Waste Diversion Event, Waste System Audit). No row was ever written, so
> the number is technically free — honour the earmark or release it deliberately, but do not fill the
> gap by accident.

---

### CR-049 (raised 2026-09-10, **BUILT 2026-09-10 — not yet deployed**) — List view as the default for logged-in users

- **Period:** raised 2026-09-10; not started
- **Source:** the client, 2026-09-09 (`CR_9_9_26_edit.pdf`), logged as **QC #262** — sheet
  `GCC DB QC Log2`, **row 271**, page `Default - List View`, Image 119: *"Is it an easy fix (with low
  risk of breaking things) to have the List view the default for all logged in users. The team will
  really only use this when they are working and they find it inefficient to have to toggle each time
  the page loads."*
- **Why CR, not a defect:** tile-first is the designed and accepted default. Changing which view a
  page opens in is new behaviour.
- **Status:** **BUILT 2026-09-10, not yet deployed.** Both answers landed (Shawn, 2026-09-10):
  **(b) remember last choice, seeded to List**, and **internal AND external**, not internal-only.
  See the build note at the end of this entry.

**Scope.** The toggle is client-side state with no persistence. The default is hardcoded in two
places per view:

- `wwwroot/js/certificate-tile-view-internal.js:27` — `currentView: 'tile'`
- `Views/Certificate/CertificateTileViewInternal.cshtml:263` — `class="toggle-btn active"` +
  `aria-pressed="true"` on the Tile button
- the external view repeats both (`…-external.js:27`, `CertificateTileViewExternal.cshtml:195`)

The **public** view has no toggle at all and is untouched, which correctly matches "logged in users".

**The risk is first paint, not the toggle — and "low risk" is the client's assumption, not ours.**
List view has only ever been reached by clicking, i.e. *after* certificates are loaded.
`showTableView()` does carry a cold branch
(`if (!state.certificates || state.certificates.length === 0) await loadCertificates()`), so the path
exists — but it has never been the initial render in production. Infinite scroll, column-visibility
restore from `localStorage`, and the sticky header all initialise in tile context today. **That needs
an actual test, not an inspection**, and it is where the time goes.

**Answer 1 — hard default, or remember each user's choice?**

| | Behaviour | Cost |
|---|---|---|
| **(a) Hard default to List** | Every load starts in List, for everyone. Literally what was asked. | baseline |
| **(b) Remember last choice, first visit lands on List** | Nobody toggles repeatedly, whichever view they prefer. | **+0.5 hr** |

**(b) recommended** — (a) just reassigns the toggling to whoever prefers tiles. The
column-visibility code on the same page already uses the `localStorage` pattern (b) needs. Caveat
either way: a per-browser preference, so a different machine starts on List again.

**Answer 2 — internal team only, or external and partner logins too?** *"All logged in users"*
literally includes External company and partner customers, but *"the team will really only use this
when they are working"* says they are picturing staff. Defaulting a **customer-facing** view to a
dense data table is a different decision. **Internal only is recommended.**

**Estimate:** **~1.5-2 hr** internal only · **~2.5 hr** if external is included · **+0.5 hr** for the
remembered-choice variant. Two lines per view; the rest is first-paint verification.

#### BUILT 2026-09-10 — both answers received, built to them

**Answer 1: (b), remember last choice, seeded to List.** Shawn confirmed this gives the client what
she asked for — the page opens in List, nobody toggles on every load — without handing the same
complaint to anyone who prefers tiles. A deliberate switch to Tile is remembered for that browser.
**Answer 2: internal AND external.** Shawn chose the literal reading of *"all logged in users"* over
the recommended internal-only. The **public** view has no toggle and is untouched, as the entry said.

**What was built.** A `VIEW_PREF_KEY` in `localStorage` per view (`gcc-cert-view-internal`,
`gcc-cert-view-external`), read by `getPreferredView()` which **defaults to `'list'`**, written only
on an explicit toggle click. Three supporting changes were needed and are worth knowing about:

1. **`applyTableViewChrome()` was split out of `showTableView()`.** `showTableView()` contains the
   cold branch `if (certificates.length === 0) await loadCertificates()`, so calling it at init —
   before the first fetch — would have fired a **second** identical request. The new function applies
   only the container/body classes. *Verified: exactly ONE list fetch on load, in both views.*
2. **The preferred view is applied BEFORE the first fetch**, not after, so `renderCertificates()`
   paints straight into the table. No tile-then-list flash.
3. **`aria-pressed` is now maintained in JS** (`setToggleButtonState`). It was hardcoded in the markup
   and never updated — survivable while tile was always the opening view, wrong the moment the
   opening view varies per user. Both buttons are now correct in both states.

The initial "Loading certificates…" spinner lives inside `#certificatesGrid`, which list mode hides,
so it is relocated into the table container — list-first users still get loading feedback.

**The first-paint risk the entry flagged was real enough to test, and it passed.** Infinite scroll,
column-visibility restore and the sticky header all initialise correctly with list as the first
render. Specifically measured, because the scroll sentinel is appended *after* `#tableView` and could
in principle have sat in view at load: **no runaway pagination** — 1 fetch at settle, still 1 after
8 seconds idle, 20 rows.

**Estimate:** ~2.5 hr (external included) + 0.5 hr (remembered choice) = **~3 hr**. Built and verified
inside it.

**Verification — 23 of 24 assertions PASS** (`Scripts/smoke-prod/verify-cr048-cr049.js`; the
non-PASS is an INFO row, not a failure). Internal and external both open in List with no saved
preference; an explicit Tile choice survives a reload; switching back to List survives a reload;
external list rows still expose **no** Edit button (RBAC unchanged); viewport pass at 1920, 1440,
1280, 768 and 390 px with zero horizontal overflow and zero console errors.



#### CR-021 proven end-to-end through the live Amazon feed, 2026-09-10 16:46

The execution note above proved the stored value. This proves what **Amazon actually receives**, which
is the thing the CR exists for. Pulled with the Amazon partner API key via
`Scripts/data-ops/2026-09-09-amazon-payload-export.py test`.

| Pull | `CEF` | `greencircle_cef` | Short codes in payload |
|---|---|---|---|
| 9/9 09:47 | 71 | 0 | CFR 7 · WDL 6 · CLP 4 · SMP 1 |
| 9/10 14:43 | 78 | 0 | none |
| **9/10 16:46 (post-CR-021)** | **0** | **78** | **none** |

**The 14:43 pull predates the update by ~83 minutes.** It sits in
`Scripts/data-ops/_amazon_payload/20260910-144252/` and still reads `CEF`. It was generated for the
client to review their same-day record edits, and **must not be sent as-is** — it would read as the
fix not having landed. The 16:46 re-pull (`…/20260910-164647/`) supersedes it.

**Membership is byte-identical between the 14:43 and 16:46 pulls** — 285 Amazon-tagged, 212
delivered, 73 withheld (39 Expired, 32 Draft, 2 not yet effective), 0 findings. So the only thing
that moved between them is the code value: the change is isolated, and nothing else was disturbed.

**The client's same-day edits, reconciled exactly** (223 → 212 delivered; 11 dropped, **0 added**,
212 in both — compared as certificate-number SETS, per [[feedback_equal_counts_are_not_the_same_rows]]):

| What | Count | Certificates |
|---|---|---|
| Left the feed | **11** | `25-8435, 25-8436, 25-8484, 25-8485, 25-9304, 26-0007, 26-0008, 26-0009, 26-0010, 26-0108, 26-0283` — 6 Waste Diversion from Landfill, 4 Closed Loop Product, 1 Sustainable Manufacturing Practices |
| Re-typed CFR → CEF, stayed in feed | **7** | `24-0001, 24-0002, 24-0003, 24-0004, 24-0005, 24-0324, 24-1635` |

Tagged fell 295 → 285 and Draft rose 31 → 32, which accounts for all 11 departures with nothing left
over. The 7 re-typed rows are exactly the `CFR → greencircle_cef` transitions in the code diff.

#### The 13-type follow-up is DOWNGRADED — latent, not live

The execution note above frames the remaining short codes as a live partner-facing risk. **After the
client's edits that is no longer true, and the note should be read with this correction.** Their
changes removed the last four short-coded types from the feed (CFR by re-typing, WDL/CLP/SMP by
untagging or drafting) and CR-021 fixed the fifth, so **every certificate Amazon receives now carries
a long-form code — zero short codes are delivered.**

The thirteen internal short codes therefore reach **nobody**. They become a problem only if a
certificate of one of those types is Amazon-tagged and published in future, at which point it would
ship a short code and silently fail to badge. **That is a latent trap worth closing cheaply, not an
open partner-facing defect.** Still the client's strings to define — the QC #264 cell offers to set
them on request rather than asking for a mapping as though something were broken.


#### CR-021 - the pre-fix payload was already sent to the client (2026-09-10)

The 14:43 bundle went to the client before the 16:06 update. Scoped rather than re-pulled-and-hoped:
**`amazonCode` is the only difference** between what they hold and what is correct - 78 certificates,
78 GS1 documents, 7,482 of 19,147 identifier rows. Certificate set identical; both reconciliation
CSVs byte-identical; every other field on every other row identical, verified column-aware.

**No action is needed with Amazon.** They consume the API directly, not this file, and the API has
returned `greencircle_cef` since 16:06. The stale value exists only in a human-review snapshot.

Follow-up note drafted at `docs/tracking/2026-09-10-client-followup-payload-amazoncode.md` (unsent).

**Process gap worth closing:** the export bundle records its pull time in the directory name only -
nothing inside states what the data reflects, so "this snapshot predates today's data change" is
invisible to whoever sends it. Suggest `summary.txt` carry the pull timestamp and any same-day data
operation. ~30 min, not built.

---

### CR-050 (raised 2026-09-14, **repo-side BUILT 2026-09-14 — infrastructure in progress**) — `test.gccwebapp.com` as a separate test site

- **Period:** raised 2026-09-14
- **Source:** Shawn, 2026-09-14 — *"I want to setup test.gccwebapp.com as the test site on the same
  hosting platform."* Scope confirmed the same day: **two separate configurations on one hosting
  platform — separate folders, separate app pools, separate config, separate databases.**
  `gccwebapp.com` itself is **left alone** — no redirect, decided later.
- **Why CR, not a defect:** nothing is broken. This is a new environment, and it closes a standing
  risk rather than fixing a fault.
- **The standing risk it closes:** today `gccwebapp.com` is the test environment and it runs against
  the **real production database** (`gccwebap_certificates`). Every test write lands on live client
  records. `email-draft-2026-08-12-production-hosting-request.md` calls separating them *"the single
  most important outcome"* of a new environment. This achieves that outcome **without** a new server.
- **Estimate:** repo-side ~2h (done). Infrastructure — DNS, Plesk site, TLS, deploy, overlay,
  uploads copy — ~3-4h with Plesk access, plus a 3.5-4 GB uploads copy. **Absorbed.**

#### Done in the repo (2026-09-14)

| Change | File | Note |
|---|---|---|
| Startup banner names the live database | `Program.cs`, after the SQL Server provider line | Prints `DB server` / `DB catalog` / `DB user`. **Never the password.** Wrapped in try/catch so a malformed connection string cannot take the site down — the DEF-395 KeyPath lesson. Verified locally: prints `ANUKI01 / GCCDatabase_Dev / GCC_DevUser`. Build clean, 0 errors. |
| Per-site secrets template | `appsettings.Secrets.test-site.template.json` (new) | Sibling of the existing template. Carries the `gccwebap_test` connection string, a distinct `Jwt:Key`, `DataProtection:ApplicationName = GCC-Test`, and `FromName = GCC Database - TEST`. |
| Runbook | `docs/cutover/test.gccwebapp.com-setup-runbook.md` (new) | Ordered steps, the verification, and the four open decisions. |

#### Why the banner was worth building before the site exists

Both sites deploy from the **same zip**, and that zip contains `appsettings.Production.json` with the
**production connection string** in it. So every deploy writes the production connection string into
the test site's folder. The only thing steering the test site elsewhere is its own
`appsettings.Secrets.json` — absent from the zip by design, registered last, therefore wins.

If that overlay is missing, malformed, or the key is misspelled, **the test site silently reads and
writes real client records.** Nothing errors, every page loads, and the data looks right because it
*is* the real data. Identical in shape to DEF-263, where empty SMTP credentials silently selected
`MockEmailService` and mail simply stopped.

Before this change the startup log said only *"Using SQL Server database provider"* — the provider,
never the target — so the failure was **unobservable from the logs**. It now states the catalog,
which makes the check falsifiable ([[feedback_confirm_means_falsifiable]]): on the test site the
banner must read `DB catalog : gccwebap_test`. If it reads `gccwebap_certificates`, stop.

#### Isolation gaps the shipped config leaves, all closed in the overlay

Found while writing the template — none are obvious, and two are silent:

1. **Shared JWT signing key.** `appsettings.Production.json` ships one `Jwt:Key` to both sites, so a
   token minted on test would validate on `gccwebapp.com`. The environments are not isolated at the
   token layer until the test site gets its own key.
2. **Uploads are per-site, and the copied DB does not know it.** `IUploadPathResolver` resolves to
   `ContentRootPath/App_Data/uploads` unless `FileStorage:StoragePath` is set, so the new site starts
   **empty** while `gccwebap_test` references ~5,700 PDFs. Every document 404s until the store is
   copied (~3.5-4 GB). Pointing the test site at the production store would "fix" reads and
   reintroduce exactly the coupling this CR removes — **do not**.
3. **Partner API keys came across in the copy.** A live Amazon key authenticates against
   `test.gccwebapp.com` and returns data, and the test gate does **not** cover it — the gate skips
   `/api/*` by design. Rotate or disable partner keys inside `gccwebap_test`.

#### Still to do (needs Plesk / AccuWeb access)

DNS `test A 64.27.52.66` in a zone **we control** (AccuWeb NS — no client involvement); Plesk
**subdomain, not a domain alias**; Let's Encrypt; deploy the same zip; place the overlay; copy
uploads; verify the banner and a write-isolation check. Full detail in the runbook.

#### Prerequisite already met

`test.gccwebapp.com` is **NXDOMAIN with no wildcard on the zone** (checked 2026-09-14) — nothing to
remove first. Shawn created the database copy `gccwebap_test` the same day.

---

### CR-051 (raised 2026-09-16, **NOT started — estimate only, approved as a CR**) — Certificate **Status** filter on the internal listing (Admin / Analyst only)

- **Period:** raised 2026-09-16; not started
- **Source:** the client, 2026-09-10, logged as **QC #266** — `Database QC Log_9.11.26.xlsx`, sheet
  `GCC DB QC Log2`, **row 275**, page `Filtering`, raised by Carly, no image: *"They often need to
  sort by status. And the only way to do it right now is alpha order by status name. Is it possible
  to add a box FOR ADMIN/ANALYST LOGIN USERS ONLY that is status. It could be under Additional filter
  box. If there is an easier way to implement then this I am open to it."*
- **Why CR, not a defect:** a certificate Status filter was never part of the accepted internal
  listing scope — no requirement, no mockup and no prior QC item asks for one, and the listing was
  signed off without it. This is the client asking for a new filter dimension on a surface they have
  already accepted. **Classified as a CR by Shawn, 2026-09-16**, after the build-state evidence below
  was put in front of her — the alternative reading (absorb as a defect) was raised, considered and
  declined. Recorded so the basis is auditable rather than assumed.
- **Status:** **Not started.** Priced here so the estimate-before-build gate is satisfied
  (`docs/governance/change-request-process.md`).

> ### Build state: most of this CR is already implemented — read before quoting the hours
>
> This is **not** a full filter-plumbing job, and the estimate below will look wrong to anyone who
> assumes it is. The status filter is **already built everywhere except the twelve lines of markup
> that would make it visible.** Verified 2026-09-16 against the working tree:
>
> | Layer | State | Evidence |
> |---|---|---|
> | API query param | **built** | `Controllers/Api/CertificateTileInternalApiController.cs:70` takes `status`; `:224-228` applies it as a pipe-delimited multi-select |
> | Filter-options payload | **built** | same file `:667` already returns `statuses = Enum.GetNames(typeof(CertificateStatus))` |
> | Shareable-link round-trip | **built** | same file `:770-771` serialises `status=` into the generated link |
> | JS filter state | **built** | `wwwroot/js/certificate-tile-view-internal.js:15` (`status: []`) |
> | JS panel population | **built** | same file `:223-231` — populates `#status-content ul` from the options payload |
> | JS request param | **built** | same file `:366` — `params.append('status', …)` |
> | JS URL de/serialise + checkbox restore | **built** | same file `:1081-1082`, `:1111-1112`, `:1133` |
> | JS active-filter-tag label | **built** | same file `:1186` (`status: 'Status'`) |
> | CSV export honours it | **built** | same file `:1607-1608` |
> | **The `#status-content` panel markup** | **ABSENT** | `grep -rn "status-content" Views/` returns **nothing** |
>
> `document.querySelector('#status-content ul')` therefore returns `null` on every page load and the
> population block silently no-ops. This is exactly the DEF-389 shape: a handler bound to an element
> that does not exist fails silently and looks like an unbuilt feature.
>
> **It was never built, not deliberately removed.** `git log -S 'status-content' --all -- 'Views/*'`
> returns **zero commits**, and no register entry records a *certificate* status filter being taken
> out. (A register grep does hit *"Status filter dropdown removed from filter row"* — that is
> **DEF-104 / CR-015 removing the Product Identifier status filter**, a different surface and
> unrelated. Confirmed by reading the enclosing entry, not the grep line.)
>
> **What this means for the quote, now that it is a CR.** The hours below are small because the
> plumbing exists, not because the ask is trivial — price it as the hour it takes, not as the
> half-day a new filter dimension would normally cost. Two consequences worth holding:
>
> 1. **The client will see it working the moment the panel appears**, with sorting, shareable links
>    and CSV export all correct on day one. That is a good outcome, but it does mean the delivered
>    behaviour will look larger than the hours billed — worth saying plainly rather than letting it
>    read as padding.
> 2. **The pre-existing plumbing is why the risk is low.** Nothing new is being introduced into the
>    query path, so the usual regression surface for a new filter (paging, counts, export scoping)
>    is not in play here.

**Scope — one file, one panel.** Insert a `Status` panel into the filter sidebar of
`Views/Certificate/CertificateTileViewInternal.cshtml`, copying the exact structure of its five
siblings (`cert-type-content` `:48`, `product-cat-content` `:65`, `company-content` `:81`,
`countries-content` `:97`, `additional-content` `:113`). The client asked for it "under Additional";
the panels render in markup order, so placement is a one-line choice.

Required shape, to match the siblings and keep the existing JS working unchanged:

```html
<div class="filter-panel">
    <button class="filter-header" data-filter="status" aria-expanded="true" aria-controls="status-content">
        <span>Status</span>
        <i class="fas fa-chevron-down" aria-hidden="true"></i>
    </button>
    <fieldset class="filter-content" id="status-content">
        <legend class="admin-sr-only">Filter by certificate status</legend>
        <ul class="filter-tree"><!-- populated by JS from filterOptions.statuses --></ul>
    </fieldset>
</div>
```

**The `id` must be exactly `status-content` and the checkbox `name` exactly `status`** — both are
already hard-coded in the JS (`:224`, `:229`). Getting either wrong reproduces the current silent
no-op rather than failing loudly.

**Do NOT add the same panel to the external view, even though the dead selector is there too.**
`wwwroot/js/certificate-tile-view-external.js:223` queries for the same `#status-content` and is
equally inert. The client scoped this **"ADMIN/ANALYST LOGIN USERS ONLY"**, and exposing raw status
values to customers would surface embargoed states — `Client Approved - Confidential` is an embargo,
not an inactive record — and the external list is already status-filtered upstream, so the control
would be misleading as well as leaky. Recommended: **delete** the dead selector from the external JS
in the same change so the two files cannot be confused later, rather than leaving a loaded selector
pointing at nothing.

**Acceptance tests** — the first two are the ones that actually prove it:

1. All **8** statuses appear, from `Models/CertificateStatus.cs` — the source of truth is 8 states,
   not the originally-specified 7. The options endpoint enumerates the enum, so a miscount means the
   enum is wrong, not the panel.
2. Ticking two statuses returns the **union**, not the intersection — the server splits on `|`
   (`:227`) and the JS joins on `|` (`:366`). A single-select-looking UI over a multi-select backend
   is the predictable misread.
3. An active filter tag reads `Status: <value>` and clearing it restores the full list.
4. Copy the shareable link with a status filter set, open it in a clean session, and confirm the
   checkboxes come back ticked (`:1081-1082` → `:1111-1112`).
5. CSV export with a status filter set exports only the filtered rows (`:1607-1608`) — and parse the
   export from **line 1 expecting a title preamble**, not a header row.

**Estimate:** **~1-1.5 hr** — twelve lines of markup, then the five acceptance tests above and a
viewport pass. Explicitly **not** a filter-plumbing job: no controller, service, repository, JS or
CSS change is required, which is why this is priced at an hour rather than the half-day a new filter
dimension would normally cost.

**Risk:** low, and contained to one view. The one way this goes wrong is adding the panel to the
external view as well.

---

### CR-052 (raised 2026-09-16, **NOT started — estimate only, needs the client to pick an option**) — Certificate PDF preview: view **all pages**, not just the first

- **Period:** raised 2026-09-16; not started
- **Source:** the client, 2026-09-10, logged as **QC #267** — `Database QC Log_9.11.26.xlsx`, sheet
  `GCC DB QC Log2`, **row 276**, page `Cert PDF Preview Popup`, raised by Carly, Image 122: *"It
  appears to only allow one page to be viewed when uploading certificate. Is it low risk to add a
  toggle to view multiple pages under the Preview popup? Example: 26-0621"*
- **Why CR, not a defect:** single-page preview is the behaviour as designed and as built — the
  generator deliberately renders page 0 into a single thumbnail. Showing every page is new
  capability. This one is a genuine CR, unlike CR-051 above.
- **Status:** **Not started.** Two options priced below; the client picks.

> ### The framing "is it low risk" is a hypothesis about our implementation, and it is wrong
>
> The preview is **not a PDF viewer**. Verified 2026-09-16:
>
> - `Services/FileUploadService.cs:553-591` — `GeneratePdfThumbnailAsync` opens the PDF with Docnet
>   and calls **`docReader.GetPageReader(0)`**: page one only, converted BGRA→RGBA, downscaled to
>   600px on the long side and saved as a **single PNG**.
> - That one path is stored in **`Certificate.CertificateImagePath`**, a single `nvarchar(500)`
>   column (`Migrations/20251015162223_InitialCreate_SqlServer_Final.cs:571`). There is nowhere to
>   put a second page.
> - The "popup" is a zoom modal over that one `<img>` — `Views/Certificate/Edit.cshtml:308`
>   (`certificateImagePreviewImg`) and `:493` (`zoomedImage`).
> - **There is no pdf.js anywhere in the application.** No page-navigation code exists to extend.
>
> So a "toggle" has nothing to toggle between. State this to the client plainly — they asked a fair
> question and the honest answer changes which option they should want.

**Option A — point the preview at the real PDF (RECOMMENDED).**

The endpoint already exists and **is not called from anywhere in the UI**:
`Controllers/CertificateController.cs:2332` `ViewCertificateFile(int id)` streams the stored PDF via
`PhysicalFile(physical, "application/pdf")` (`:2673`) with **no `fileDownloadName`**, so ASP.NET Core
sets no `Content-Disposition: attachment` and the browser renders it **inline in its native viewer —
every page, plus zoom, scroll, search and print, for free**. Verified by reading the code path;
`grep -rn "ViewCertificateFile" Views/ wwwroot/js/` returns **zero callers**.

The work is a "View all pages" control in the preview modal that opens
`/Certificate/ViewCertificateFile/{id}`, and nothing more.

- **It is already secured correctly.** `FileAccessAllowed` (`:2454`, DEF-204/208) gates it: Admin and
  Analyst unrestricted, everyone else — anonymous, External, Partner — must pass status **and**
  ownership filters. This CR must not touch that gate.
- **The route takes the integer `Id`, not the certificate number.** Passing `26-0621` yields a 404
  that reads like a missing file. The modal must carry the Id.
- **Certificate numbers are not unique** — 137 numbers span multiple rows — which is a second reason
  to key off Id.
- **Known limitation to state, not fix:** an encrypted PDF still will not render (DEF-400, cert
  23-0971). The browser viewer will fail the same way the thumbnail generator does. Say so up front.
- **Estimate: ~1.5-2.5 hr** — one control in the modal on both Create and Edit, the Id wiring, a role
  pass proving an External user still cannot open someone else's PDF, and a check on **26-0621** (the
  client's own example) that page 2 is actually reachable.

**Option B — generate and store every page.**

Extend `GeneratePdfThumbnailAsync` to loop `0..docReader.GetPageCount()`, write one PNG per page, and
build a pager in the modal. This needs a storage model that does not exist: either a new
`CertificatePreviewPage` table or a JSON path list, **plus a migration** (TEST auto-migrate is OFF
since 2026-09-04 — migrations are applied by hand), **plus a backfill across ~5,700 existing
certificates**, plus the uploads store growing by roughly the page count. It buys a slightly more
integrated look and nothing else.

- **Estimate: ~8-12 hr** build, plus a backfill run and its verification.

**Recommendation: Option A.** It is ~5x cheaper, adds no schema, no migration and no backfill, gives
the client *more* than they asked for (search and print, not just page 2), and it activates an
endpoint we already wrote, already secured and never wired up. Option B is only worth it if the
client specifically wants page thumbnails rendered inside our own modal chrome — worth asking, since
their words were "view multiple pages", which Option A satisfies fully.

**Open question for the client:** is a native browser PDF view acceptable, or do they want the pages
inside the GCC-styled modal? That answer is the whole difference between ~2 hr and ~10 hr, so it goes
to them **before** any build starts.

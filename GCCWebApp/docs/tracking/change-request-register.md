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

### CR-171 (proposed 2026-06-04) — Multi-select Partner on certificates + partner data isolation
- **Source:** QC #171 (Carly, 5/5/26) — *"Partner must be multi-select if used for partner login access."* Still Pending/"Did not check" as of 5/29. Refined by Shawn 2026-06-04: *"The original scope was to only have one partner. Certificates could have more than one partner. If we are sending to Amazon, we would not want to send the other partners."*
- **Why CR (not a defect):** Original spec scoped one partner per cert (`Certificate.Partner` single string). Client now needs >1 partner per cert AND — because Partner drives partner-login visibility — each partner must see only their own association, never co-partners. Data-model + visibility-rule expansion, not a bug fix.
- **Key finding:** Backend is half-built — `Certificate.Partners` M2M exists and the Create/Edit POST handlers already consume `PartnerIds`, but the form is still single-select and **all ~9 visibility filters + all partner-facing output read the single `Partner` string**. No leak exists today *because* output reads the single field; the leak is introduced if access is switched to the collection without scoping output per-requester. Design rule: **access = collection membership; output = `cert.Partners ∩ caller`, never `.First()`, never the set.**
- **Scope (when approved):** multi-select form (Create/Edit); rewrite ~9 access filters to collection membership (`.AsSplitQuery()` per [[feedback_ef_includes_with_projection]]); requester-scoped output in `BuildPartnerCertificateDto` + 2 inline projections; **prod data backfill of the M2M join table (Gate 7)** or existing certs vanish for partner logins; keep legacy `Partner` string as denormalized primary for internal sort/CSV.
- **Partner API impact:** JSON **shape unchanged** (single `partner` field per [[feedback_amazon_api_immutable]] / [[feedback_partner_data_isolation]]); only *which* value changes (requester-scoped). Needs Amazon-side awareness (Q5 in detail doc).
- **Open questions for sign-off:** (1) does Public see a partner value at all; (2) does a Company user see co-partners on their own cert; (3) is "no partner" still valid; (4) multi-partner via CSV import in/out of scope; (5) Amazon contract confirmation. See detail doc §6.
- **Includes:** full role × capability visibility matrix + multi-partner use-case matrix (UC1–UC4) in the detail doc.
- **Effort estimate:** Medium (~1.5–2 days incl. backfill + Playwright).
- **Detail doc:** `docs/tracking/CR-171-multipartner-visibility.md`
- **Status:** **Pending — awaiting client sign-off.** Nothing built. Visibility matrix documented for client review.
- **UPDATE 2026-06-11 — client decision email drafted.** The 5 sign-off questions packaged for Carly in `docs/tracking/CR-184-171-client-email.md` (alongside the standalone `CR-171-client-email.md`). Still nothing built — `Views/Certificate/Create.cshtml:205` remains single-select; build stays on hold pending answers.

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
- **Status:** Proposed 2026-06-01 — awaiting GCC approval. Nothing run against live yet.

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

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
- **Why CR:** Requires Azure AD B2C email verification configuration + real domain email setup. Infrastructure + custom policy work, not a code defect.
- **Status:** Approved 2026-04-29 — needs B2C plan tier confirmation before sizing
- **Pairs with:** CR-010 (B2C custom-policy work overlaps)

### CR-010 (approved 2026-04-29) — Duo MFA support
- **Source:** QC #151 — "is Duo also an option? That is more popular here"
- **Why CR:** Requires Azure AD B2C custom policies + potentially plan tier upgrade. New feature, not in original scope.
- **Status:** Approved 2026-04-29 — needs B2C config investigation before sizing
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

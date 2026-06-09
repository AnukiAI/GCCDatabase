# CR-184 — Global Product Identifiers CSV export

> ## ⚠️ AS-BUILT (reconciled 2026-06-04) — read this first
>
> **Status:** **Built + verified on localhost:5101 (2026-06-04) — pending deploy + client UAT.**
> The shipped implementation is **simpler than the proposed spec below** and was steered live by Shawn across the 2026-06-04 session. The original §1–§9 spec is **retained unchanged for audit** (planned-vs-built trail), but where it conflicts, **this AS-BUILT section governs.**
>
> **What shipped:**
> - **Endpoint:** `GET /Export/DownloadActiveProductIds` in **`ExportController`** (not `AdminController` / `AdminService`). Inline CSV build reusing the controller's existing pattern + a new `CsvField()` RFC-4180 escape helper.
> - **RBAC:** `[Authorize(Policy = "AnalystOrAdmin")]` — **Admin + Analyst** (verified: analyst 200, external **403**). *This intentionally overrides the spec's `AdminOnly`, which would have locked analysts out — Shawn confirmed 2026-06-04 "analysts need it too."*
> - **Scope:** **Active only** — parent cert in `PublicStatuses` (Client Approved - Paid or Republished), i.e. CR-015's definition of an active product identifier. Matches Carly's literal "all **active** product identifiers across certs." (Verified: 1014 active rows vs 1126 all-status on the dev DB.)
> - **No filters.** The spec's 5 filters (Partner / Identifier Type / Cert Status / Issue date from-to) and filter sub-modal were **not built** — deferred as a possible v2. One-click full export of the active set.
> - **Columns (12, no status column):** Certificate Number, Company, Identifier Type, Id Value, Partner, Product Name, SKU, Description, Effective Date, Expiration Date, Created Date, Created By. *Status columns omitted entirely — every row is active by filter, so both the derived Identifier Status (CR-015) and Certificate Status are non-informative (Shawn 2026-06-04).*
> - **UI:** a **new dedicated button** "Export Active Product Identifiers" on **`Views/Product/Management.cshtml`** (the actually-rendered product page — the spec's `ManagementV2.cshtml` reference was stale), role-gated `role-element role-analyst-show role-admin-show`. The existing client-side product Export button was **left untouched** (not replaced).
>
> **Files actually touched:** `Controllers/ExportController.cs` (new action + `CsvField` helper), `Views/Product/Management.cshtml` (button + inline fetch handler). No service-layer, no DTO, no migration, no Partner API change. Per-cert export untouched.
>
> **Why simpler than spec:** Shawn's live direction this session prioritized a one-click analyst-accessible active-set export over the filtered admin-only design. Filters remain a clean future add (see §2.4) if analysts later need to slice by partner/type/date.

---

**Status (original proposal):** Proposed — scope locked 2026-06-03 (Shawn answered the open questions); ready to build pending sign-off. *(Superseded by AS-BUILT above, 2026-06-04.)*
**Date drafted:** 2026-06-02 (open questions resolved 2026-06-03)
**Source:** QC #184 (client, 5/15/26) — Carly: *"How would we get a full export with product identifier information across all products? The export on this page includes Identifier Type and Id Value but not the product name at the product level or SKU or partner. We will need to run aggregate export on all active product identifiers across certs — where and how can we do that today. This is a common point of analysis and export we have to run."*
**Estimated effort:** ~4–6 hours (service + controller + UI filter form + Playwright at 5 viewports)

---

## 1. Problem

There is no way today to pull a single CSV of product identifiers spanning all certificates with the context an analyst needs (cert number, partner, company, cert status). Existing surfaces only solve a subset:

- **Per-certificate export** (`wwwroot/js/product-id-manager.js:1961` → `GET /api/productids/certificate/{id}/export`) returns identifiers for one cert. Columns: Type, Product ID, Product Name, SKU, Partner, Created, Created By, Description. Useful at the row level, useless for cross-cert analysis.
- **Product Management page export** (`Views/Product/ManagementV2.cshtml` → `wwwroot/js/admin/productManagement.js:648`) emits product rows with Identifier Type + Value, but no cert number, no partner, no cert status, and no date filters.

So an analyst running a "show me every active ASIN tied to a paid Amazon cert in May" question has no path other than per-cert export × N certs + manual stitching.

---

## 2. Proposed change

A new admin-only CSV export endpoint that joins `CertificateProductId` × `Certificate` × `Company` × `Product` and returns one row per identifier, with filters.

### 2.1 API contract

`GET /Admin/Export/ProductIdentifiers?partner=&identifierType=&certStatus=&createdFrom=&createdTo=`

Returns `File(bytes, "text/csv", "product-identifiers-YYYY-MM-DD.csv")`. Direct file download, no JSON wrapper.

### 2.2 RBAC

`[Authorize(Policy = "AdminOnly")]` — already class-level on `AdminController` (`Controllers/AdminController.cs:12`). Admin + Analyst can run it. External (Company + Partner) blocked.

### 2.3 CSV columns (one row per `CertificateProductId`)

| # | Column | Source |
|---|---|---|
| 1 | Cert Number | `Certificate.CertNumber` *(never DB Id — per CLAUDE.md)* |
| 2 | Cert Status | `Certificate.Status` (one of the 8 enum values) |
| 3 | Partner | `CertificateProductId.Partner` ?? `Certificate.Partner` |
| 4 | Company | `Certificate.Company.Name` |
| 5 | Product Name | `CertificateProductId.ProductName` ?? `Certificate.Product.Name` |
| 6 | SKU | `CertificateProductId.ProductSku` ?? `Certificate.Product.Sku` |
| 7 | Identifier Type | `CertificateProductId.ProductIdType` |
| 8 | Identifier Value | `CertificateProductId.ProductIdValue` |
| 9 | Created (UTC) | `CertificateProductId.CreatedAt` |
| 10 | Created By | `CertificateProductId.CreatedByUser.UserName` |

Status column intentionally **not** included — `CertificateProductId.Status` is auto-derived from the parent cert (CR-015, `feedback_product_id_status_follows_cert`), so it's redundant with column 2.

### 2.4 Filters (all optional, query-string)

| Filter | Query param | Values | Source of truth |
|---|---|---|---|
| Partner | `partner` | Active rows from the admin **Partners** table | `Models/Partner.cs` (admin-managed via `Views/Admin/Partners.cshtml`); filter dropdown populated from `Partners` table, not from the `Certificate.Partner` string field |
| Identifier Type | `identifierType` | Distinct `ProductIdType` values currently in `CertificateProductIds` | data-driven (`SELECT DISTINCT ProductIdType ...`) |
| Cert Status | `certStatus` | Any of the 8 enum values | `CertificateStatusHelper.GetAllStatuses()` — no default exclusion, all 8 selectable |
| Issue date from | `issuedFrom` | `YYYY-MM-DD` | filter on `Certificate.IssueDate`; pre-format yyyy-MM-dd per [[feedback_calendar_date_serialization]] |
| Issue date to | `issuedTo` | `YYYY-MM-DD` | same — `Certificate.IssueDate` |

### 2.5 Service layer

```csharp
// Services/IAdminService.cs
Task<byte[]> ExportProductIdentifiersAsync(ProductIdExportFilters filters);
```

EF query (per `feedback_ef_includes_with_projection`, DEF-166/234 pattern):

```csharp
_db.CertificateProductIds
    .Include(cpi => cpi.Certificate).ThenInclude(c => c.Company)
    .Include(cpi => cpi.Certificate).ThenInclude(c => c.Product)
    .Include(cpi => cpi.CreatedByUser)
    .Where(/* filters */)
    .OrderBy(cpi => cpi.Certificate.CertNumber)
    .ThenBy(cpi => cpi.Id)
    .AsSplitQuery()
    .ToListAsync();
```

Server-side CSV generation (not client) — needed for the cross-cert JOIN and to avoid shipping the full dataset to the browser.

### 2.6 UI entry point

**No new UI elements.** Per Shawn 2026-06-03 — Carly's QC referenced "the export on this page" meaning the Product Management page (`Views/Product/ManagementV2.cshtml`). The existing **Export** button on Product Management is the entry point; no new tile, no new page, no new button. The button opens its existing modal (or a small filter sub-modal hosting the 5 filters), submits to the new server-side `/Admin/Export/ProductIdentifiers` endpoint, and triggers the download.

Implication: the existing client-side `exportData()` / `generateCSV()` path in `wwwroot/js/admin/productManagement.js:620-707` is **replaced** (for this button) with a server-side fetch + download flow. The current product-level CSV (Product Name, Company, Category, Status, Identifier Type, Identifier Value, SKU, Model) is superseded by the new identifier-level CSV (the 10 columns in §2.3), which is the superset Carly asked for.

---

## 3. Acceptance criteria

- Admin or Analyst user can pull a CSV that includes all 10 columns above, joined across every certificate.
- External user (Company or Partner) gets `403`.
- All 5 filters apply correctly, can be combined, and `issuedFrom`/`issuedTo` (against `Certificate.IssueDate`) use inclusive boundaries on the server's local date interpretation (pre-formatted yyyy-MM-dd, not UTC-serialized — per [[feedback_calendar_date_serialization]]).
- Partner filter dropdown is populated from the admin `Partners` table (`Models/Partner.cs`), not from the free-text `Certificate.Partner` string field.
- Identifier Type filter dropdown is populated by distinct `ProductIdType` values currently present in `CertificateProductIds`.
- Sort order is stable: by `CertNumber` ascending then `CertificateProductId.Id` ascending.
- Empty result set returns an empty CSV (header row only), not a 404.
- Existing per-cert export endpoint (`/api/productids/certificate/{id}/export`) is untouched.
- Existing Product Management page export is untouched.
- Playwright validation at 5 viewports per `feedback_playwright_screenshots`: filter form renders, submit triggers download, RBAC denies External users.

---

## 4. Files (estimated)

- `Controllers/AdminController.cs` — new `ExportProductIdentifiers()` action.
- `Services/AdminService.cs` + `Services/IAdminService.cs` — new `ExportProductIdentifiersAsync()` method + `ProductIdExportFilters` DTO.
- `Models/Admin/ProductIdExportFilters.cs` (or inline in `AdminModels.cs`) — filter parameter object.
- `Views/Product/ManagementV2.cshtml` — small filter sub-modal markup attached to the existing Export button.
- `wwwroot/js/admin/productManagement.js` — replace the current client-side `exportData()` / `generateCSV()` path for this button with a fetch-and-download call to `/Admin/Export/ProductIdentifiers`. Keep the rest of the file untouched.
- Playwright spec covering filter form, RBAC, download, edge cases.

---

## 5. Files NOT touched

- `Controllers/Api/ProductIdApiController.cs` — existing per-cert API and export endpoints stay as-is.
- `Services/ProductIdService.cs` — no changes; new logic lives in `AdminService`.
- `Models/CertificateProductId.cs`, `Models/ProductIdentifier.cs`, `Models/Product.cs` — no schema changes.
- No SQL migration.
- Partner API (`/api/v1/certificates*`) — untouched per [[feedback_amazon_api_immutable]].

---

## 6. Out of scope

- **Excel / JSON / PDF output** — CLAUDE.md mandates CSV only from admin pages.
- **Editing `CertificateProductId.Status`** — auto-derived from parent cert per CR-015, [[feedback_product_id_status_follows_cert]]. The status column is excluded from this export accordingly.
- **Background job / async download** for very large exports. If a future client pushes 100k+ identifiers and the synchronous download times out, that's a separate CR.
- **Saved filter presets** — not requested.
- **Cross-cert search UI** — this CR is an export, not a searchable browse view.

---

## 7. Partner API impact

None. This is an internal admin export endpoint. Per [[feedback_amazon_api_immutable]], no `/api/v1/certificates*` JSON shape changes.

---

## 8. Open questions — resolved 2026-06-03 (Shawn)

1. **UI home** — *No new UI elements.* Use the existing Export button on the Product Management page; the same button now triggers the new server-side export. Rationale: Carly's QC said *"the export on this page"* — that page is Product Management.
2. **Partner filter values** — *Populated from the admin Partners table* (`Models/Partner.cs`, managed at `Views/Admin/Partners.cshtml`). Not free-text, not the `Certificate.Partner` string.
3. **Identifier Type filter values** — *Data-driven.* `SELECT DISTINCT ProductIdType` from `CertificateProductIds`.
4. **Cert Status default** — *No default exclusion.* All 8 enum states selectable; nothing pre-filtered.
5. **Date range field** — *Filter on `Certificate.IssueDate`.* Not `CertificateProductId.CreatedAt`.

---

## 9. Why CR (not defect)

Per Carly's note ("Did not check, not ready for UAT") — this is a new aggregate export surface the system has never had, not a broken behavior. Existing per-cert export works as designed. Adding a cross-cert export with new filter scaffolding is scope expansion, not a fix.

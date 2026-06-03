# CR-184 — Global Product Identifiers CSV export

**Status:** Proposed — awaiting client sign-off on 5 open questions (see §8)
**Date drafted:** 2026-06-02
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
| Partner | `partner` | `Amazon` \| `Novi` \| `None` | `Certificate.Partner` string field |
| Identifier Type | `identifierType` | distinct values from `ProductIdType` | populated from data |
| Cert Status | `certStatus` | one of 8 enum values | `CertificateStatusHelper.GetAllStatuses()` |
| Date range from | `createdFrom` | `YYYY-MM-DD` | pre-format yyyy-MM-dd per `feedback_calendar_date_serialization` |
| Date range to | `createdTo` | `YYYY-MM-DD` | same |

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

Two reasonable homes (decide in §8):

- **A.** New "Exports" tile on Admin dashboard → opens filter modal → submit triggers download.
- **B.** Button on Product Management page header next to existing export → opens filter modal → triggers download.

---

## 3. Acceptance criteria

- Admin or Analyst user can pull a CSV that includes all 10 columns above, joined across every certificate.
- External user (Company or Partner) gets `403`.
- All 5 filters apply correctly, can be combined, and `createdFrom`/`createdTo` use inclusive boundaries on the server's local date interpretation (pre-formatted yyyy-MM-dd, not UTC-serialized — per [[feedback_calendar_date_serialization]]).
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
- **UI host** (depending on §8 decision):
  - Option A: `Views/Admin/Index.cshtml` (tile) + new `Views/Admin/ExportProductIdentifiers.cshtml` (filter form / modal).
  - Option B: `Views/Product/ManagementV2.cshtml` (button) + `wwwroot/js/admin/productManagement.js` (modal launcher).
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

## 8. Open questions (need client/Shawn sign-off before build)

1. **UI home** — Admin dashboard "Exports" section (A) or Product Management page header button (B)?
2. **Partner filter values** — restrict to fixed list `{Amazon, Novi, None}` or free-text match? (Today `Certificate.Partner` is a string field, not a FK.)
3. **Identifier Type filter values** — distinct-from-data dropdown, or fixed list (UPC, ASIN, EAN, Model #, etc.)?
4. **Cert Status default** — exclude Draft / Hold by default (since Carly's framing was "active product identifiers"), or include all 8 states until filtered?
5. **Date range field** — filter on `CertificateProductId.CreatedAt` (when the identifier row was added) or `Certificate.IssueDate` (when the cert was issued)?

---

## 9. Why CR (not defect)

Per Carly's note ("Did not check, not ready for UAT") — this is a new aggregate export surface the system has never had, not a broken behavior. Existing per-cert export works as designed. Adding a cross-cert export with new filter scaffolding is scope expansion, not a fix.

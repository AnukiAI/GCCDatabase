# CR-018 — Server-side pagination for the Product Identifier list (Phase 1 / launch-blocking)

**Status:** Proposed — awaiting client sign-off (or Anuki internal approval if treated as a defect-class fix)
**Date drafted:** 2026-05-07
**Source:** Internal — Shawn observed slow Edit-page load on cert `26-0023` (1001 product IDs). Confirmed via Playwright probe: API returns in 71ms, but client-side render of 1001 rows pushes total page settle to 3.5s+.
**Estimated effort:** 6–10 hours (API + service + JS rework + Playwright update)
**Phase:** **1 — required before launch.** Real-world Amazon partner certs may carry hundreds of ASINs; current client-side everything-at-once approach won't scale.

---

## 1. Problem

The Product Identifier list on cert Edit pages fetches every product ID up-front via `GET /api/productids/certificate/{id}` and renders the whole list client-side, doing all sort, filter, search, and pagination in JavaScript. With ~50 product IDs this is fine. With 1000+, the user sees a noticeable freeze on initial render, on every column-sort click, and on every filter change.

**Measured on cert 26-0023 (1001 ASINs, dev environment, localhost:5101, headless Chromium):**

| Phase | Time |
|---|---|
| Server returns 1001 product IDs | 71 ms |
| Edit page navigation + networkidle | 1486 ms |
| Total page settle (load + JS sort + filter + render of 1001 rows) | ~3500 ms |

The client-side time is dominated by sorting 1001 objects, building 1001 DOM rows, and applying any filter to all 1001 in-memory. The server isn't the bottleneck; the architecture is.

---

## 2. Root cause

`wwwroot/js/product-id-manager.js`:
- `loadProductIds()` calls `/api/productids/certificate/{id}` once and pulls the full list into `this.productIds[]`.
- `applyFilters()` filters the full array against type / partner / search, then sets `filteredProductIds`.
- `applySort()` sorts the filtered array by the active column.
- `renderTable()` paginates the sorted/filtered array client-side at 50 per page.

Every user interaction (sort click, filter change, search keystroke) re-runs the full client-side pipeline against all 1001 rows.

`Controllers/Api/ProductIdApiController.cs` `GetProductIdsByCertificate(int certificateId, int page = 1, int pageSize = 50)` — already has `page` and `pageSize` parameters in the signature, but the implementation ignores them and returns everything.

`Services/ProductIdService.cs` `GetProductIdsByCertificateAsync(int certificateId)` — no paging parameters; loads the entire collection.

So the stub is partly there — the controller signature accepts paging args — but the service method is the wrong shape to support them and the JS doesn't pass them.

---

## 3. Proposed change

### 3.1 API contract (revised)

`GET /api/productids/certificate/{certificateId}?page=1&pageSize=50&sort=type&dir=asc&search=&type=&partner=`

Response shape:
```json
{
  "data": {
    "items": [ /* product ID rows for this page */ ],
    "totalCount": 1001,
    "page": 1,
    "pageSize": 50,
    "totalPages": 21
  },
  "success": true
}
```

Existing list endpoints that don't need this (the per-id GETs, the bulk POST, the export) stay unchanged.

### 3.2 Service layer

Add a new method, don't change the existing one (keep back-compat for export and other callers):

```csharp
Task<PagedResult<CertificateProductId>> GetProductIdsByCertificatePagedAsync(
    int certificateId,
    int page,
    int pageSize,
    string? sortColumn,
    string? sortDirection,
    string? search,
    string? typeFilter,
    string? partnerFilter);
```

Pushes the sort, filter, and search into LINQ → SQL so the database does the work and we transmit only the visible page.

### 3.3 JS rework

`product-id-manager.js`:
- Remove the "load all once" assumption. `loadProductIds()` becomes a per-page fetch that includes the active sort, filter, search params.
- `applyFilters()` and `applySort()` become trigger functions that call `loadProductIds()` with new params, not in-memory transforms.
- Pagination clicks call `loadProductIds()` with the requested page.
- Add request debouncing on the search input (250 ms) so typing doesn't fire one request per keystroke.
- Add a tiny request-version counter so a slow response from page 1 doesn't overwrite a fast response from page 2 if the user clicks fast.
- Maintain a small client-side cache keyed by (sort, filter, search, page) so going back to page 1 after visiting page 2 doesn't refetch.

The Create page stays exactly as it is — `certificateId === 0` means no API call, all in-memory. That path doesn't change.

### 3.4 Add/Edit/Delete behavior on Edit page

Currently single-row mutations (add / edit / delete) hit the API and then call `loadProductIds()` to refresh the entire list. With server-side paging, that now means refetching the current page, which is fine and fast — but the new row may not appear on the user's current page if sort lands it elsewhere. UX consideration:

- After Add: jump to whichever page contains the new row (server returns the row's position; client computes the page).
- After Edit: stay on current page; if the edited row moved off (because the sort key changed), show a brief toast: *"Updated — moved to page N due to sort"*.
- After Delete: stay on current page. If the page is now empty, drop back to page (current - 1).

### 3.5 Export endpoint

`GET /api/productids/certificate/{id}/export` already returns the full CSV via `ExportToCsvAsync` — it bypasses paging because export should always be the full list. No change.

### 3.6 Internal contract preserved

Keep `IProductIdService.GetProductIdsByCertificateAsync(int)` (the un-paged method) for the export path and any internal callers that need the full collection. Add the paged variant alongside.

---

## 4. Acceptance criteria

- Open `/Certificate/Edit/26-0023` (1001 product IDs in dev DB). Page settle time ≤ 1.5 s end-to-end (today: 3.5 s).
- API request payload size for the initial page load: ≤ 50 rows (today: 1001 rows).
- Sort by any column: response time ≤ 300 ms (server query) + ≤ 200 ms render. No visible freeze.
- Filter change (type, partner): same target — ≤ 500 ms total.
- Search keystroke: debounced to 250 ms; smooth typing experience.
- Add / Edit / Delete: refresh feels instant (≤ 500 ms total).
- Export: full CSV with all 1001 rows (unchanged behavior).
- Cert with 50 product IDs: behavior visually identical to today (no regression on the small-cert case).
- Create page (cert ID 0, in-memory mode): completely unchanged.
- Playwright tests cover: paging across pages, sort with paging, filter with paging, search with paging, Add/Edit/Delete refresh, export still pulls full list.

---

## 5. Out of scope

- **Indexing strategy.** If `CertificateProductIds` lacks the right indexes for the new sortable / filterable columns at scale, that's a separate DB-side optimization. Spot-check after this CR ships; address as a follow-up CR if EXPLAIN shows table scans on common queries.
- **Virtual-scroll alternative.** A client-side virtualized list (`react-window`-style) would fix the render-time half of the problem without a backend change, but doesn't fix the network-payload half (still ships 1001 rows to the browser) and adds complexity to the JS that the team doesn't currently maintain. Going server-side is the right architectural choice.
- **Background bulk-import progress UI.** If client-onboarding pushes 5000+ ASINs per cert, we may need a progress bar / async job. Not blocking launch — defer until we see the load profile.
- **Cross-cert search.** Searching all product IDs across all certs (e.g., "find which cert owns ASIN B08N5WRWNW") is its own surface — not what this CR addresses.

---

## 6. Files (estimated)

- `Controllers/Api/ProductIdApiController.cs` — adopt the new paged service method, validate query params, shape the response.
- `Services/ProductIdService.cs` — add `GetProductIdsByCertificatePagedAsync`. Keep existing un-paged method for export / internal use.
- `Services/IProductIdService.cs` — interface signature.
- `wwwroot/js/product-id-manager.js` — the bulk of the work.
- `Tests` — Playwright + integration tests (new + updated).

No database schema changes. No migration. No Partner API impact (per `feedback_amazon_api_immutable.md`, `/api/v1/certificates*` is untouched — this is the internal admin API only).

---

## 7. Why Phase 1

Real-world Amazon partner cert data is unbounded. We have evidence today that a single cert can carry 1000+ product IDs, and the current architecture buckles at that scale. Shipping the launch with this latency is going to surface immediately as a perceived-quality problem — the same Edit page that's snappy on a 5-ASIN test cert will feel broken on a 500-ASIN production cert. Better to build the server-side paging once, correctly, before client UAT scrutiny than to patch it post-launch with stakeholder pressure.

---

## 8. Why this is also discussable as a defect

The endpoint signature already advertises `page` and `pageSize` parameters that don't actually work — that's arguably a bug in the contract, not a missing feature. If the client treats this as a defect rather than a CR, the work is the same; only the cost-attribution changes.

# CR-022 — Add SKU as a sortable column on the internal certificate table

- **Period:** Surfaced 2026-06-23
- **Status:** **Proposed — pending client approval** (not yet sent at time of drafting)
- **Source:** Internal review of sorting behavior on `/Certificate/CertificateTileViewInternal` (the 6/23 sort-audit task). One acceptance criterion asked that **SKU** be "either added as a sortable table option or the blocker clearly documented."

## Why this is a CR, not a defect

SKU is **not in the captured requirements** for the internal certificate tile/table view. The view ships with a fixed set of 24 columns (Certificate #, Type, Status, dates, Company, Product fields, Location/Country, External/Internal Tag, Partner, ASIN/UPC/GTIN/EAN, Cert Results, Pathway, Cert Subcategories, Company Email, Company URL) plus admin-managed custom-field columns (CR-007). **SKU was never one of them.**

**Where does SKU data live? Three distinct homes — pick deliberately.** This is the crux of the CR:

1. **`CertificateProductId` rows with `ProductIdType == "SKU"`** — SKU as a *product identifier on the certificate*, the SAME collection (and a direct peer) of ASIN/UPC/GTIN/EAN. **This is already projected and sent to the browser** by the internal API: `CertificateTileInternalApiController.cs:270` reads it and `:360` emits it as `sku` in the response JSON (alongside `isbn`/`mpn`, which are in the same not-yet-displayed state). The cell value just isn't rendered or sortable.
2. **`CertificateProductId.ProductSku`** — a per-identifier attribute ("the SKU paired with *this* ASIN/UPC row"). Surfaced in the Product ID Manager grid.
3. **`Product.Sku`** — a SKU on the product record. Surfaced on Product Management.

> **Correct source for a cert-table column = #1**, because the table's ID columns (ASIN/UPC/GTIN/EAN) all come from the certificate's product-identifier collection. Sourcing a cert-table SKU column from `Product.Sku` (#3) would be inconsistent with every neighbouring column and can show a *different* value. **An earlier draft of this CR mistakenly scoped #3 — corrected to #1.**

> **Blocker statement (satisfies the "document the blocker" path of the AC):** the `sku` value already flows to the client from the cert's product-identifier collection, but is not rendered as a column or wired to a sort key. Surfacing + sorting it is the build below and is tracked as this CR.

## Current state — the rest of the sort audit is already done

For the record, the other sort columns called out in the same review are already correct in the shipped build (DEF-256/258/259/260/261, shipped 2026-06-12):
- **ASIN / UPC / GTIN / EAN** sort by the **count badge** the cell displays, empties last in both directions (DEF-260).
- **External Tags** sort by the alphabetically-first tag (`.Min()`), matching the alphabetical comma-joined display, asc and desc (DEF-261 controller change).
- **Empty/null** values sort **last in both directions** across all text/count/numeric columns (`CertificateSortExtensions`).

Only SKU is outstanding, and only because it isn't a column.

## Scope (when approved) — sourcing SKU from the product-identifier collection (#1)

1. **API projection** — *already done.* `sku` is projected from `ProductIdType == "SKU"` (`CertificateTileInternalApiController.cs:270`) and emitted in the response (`:360`). No new query needed. (It's currently `.FirstOrDefault()` = single-valued; if SKU should be multi-valued like ASIN, switch to a list + count badge — see open question below.)
2. **Sort case** — add `"sku"` / `"sku-za"` to `ApplySorting`. As single-valued text: `OrderByTextEmptiesLast(c => c.ProductIds.Where(p => p.ProductIdType == "SKU" && p.IsActive).Select(p => p.ProductIdValue).FirstOrDefault(), …)` (empties last per QC #157). If multi-valued, mirror ASIN's `OrderByCountEmptiesLast` count approach instead.
3. **Header + row cell + column toggle** — add a `tc-sku` header (`Views/Certificate/CertificateTileViewInternal.cshtml`), a body cell rendering `cert.sku`, the `'sku'` key in `COLUMN_KEYS`, and the `sku` → `sku` entry in `sortMap`/`reverseSortMap` (`wwwroot/js/certificate-tile-view-internal.js`).
4. **External-view parity** — decide whether the external table (`CertificateTileViewExternal`) gets the column too (it mirrors the internal column set).
5. **CSV export** — add SKU to `/Certificate/ExportCertificates` so export ↔ table stay aligned.

> **Note:** `isbn` and `mpn` are in the identical already-projected-but-not-displayed state. If the client wants SKU surfaced, ask whether ISBN/MPN should be too, to avoid a second one-off CR.

**Estimate:** ~half a day incl. Playwright validation at the standard 5 viewports (less than first thought — the data plumbing already exists).

## Acceptance criteria (when built)

- SKU column appears in the internal table, behind the column-toggle like the other optional columns.
- Clicking the SKU header sorts ascending, second click descending; empty SKUs sort last in both directions.
- No regression to the existing 24 columns or to count/tag sorting.

---

## Client-facing draft (review before sending)

**Subject:** Quick decision — do you want a SKU column on the certificate list?

Hi [name],

While reviewing the sorting on the internal certificate list (the spreadsheet-style table view), one item came up that I want to check with you before building.

The acceptance notes asked for **SKU** to be a sortable column on that certificate list. Right now SKU isn't shown there as its own column, so there's nothing to sort. SKU was never part of the original certificate-list design, so adding it is a small change rather than a fix — and the plumbing is mostly already in place, so it's quick on our side (about half a day).

One thing worth clarifying so we surface the *right* SKU: the system records SKU in a couple of ways — the SKU tied to a product's marketplace identifiers (the same place ASIN/UPC/GTIN/EAN come from on a certificate) versus a SKU on the product record itself. For a column sitting alongside the ASIN/UPC/GTIN/EAN columns, we'd use the certificate's identifier SKU so it's consistent with its neighbours.

If approved, we'd:
- add a **SKU column** to the certificate list (hidden/shown via the same column picker as the other optional columns),
- make it **sortable** (A–Z and Z–A, with blanks sorting to the bottom, consistent with the rest of the table), and
- include SKU in the **CSV export** so the export matches the table.

**A few quick questions:**
1. Do you want the SKU column added to the certificate list, or is SKU fine where it is today (on the product/identifier screens)?
2. Which SKU do you mean — the one tied to a product's marketplace identifiers, or the product-record SKU? (We'd recommend the former for this table.)
3. Should it appear on the **external/partner** certificate list too, or **internal only**?
4. While we're in there: we also already capture **ISBN** and **MPN** the same way and could add those columns in the same pass if useful — want them, or SKU only?

If you'd rather not add it, no problem — we'll note that SKU stays on the product screens and close this out. Everything else in the sorting review is already done and working.

I've logged this as CR-022 for the record.

Thanks!
Shawn

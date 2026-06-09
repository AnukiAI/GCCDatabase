# CR-186 — Product identity = Name + Category + Product Type + Subcategory

**Status:** **ON HOLD — likely unnecessary; superseded by DEF-243 (deployed) + CR-019 (data cleanup).** Raised after the client's 6/6–6/7 Product Management feedback, but the 2026-06-08 review (client image 82, cert 26-0168) showed the actual symptom is **already-fixed code (DEF-243, live 2026-06-04) + stale pre-fix product data**, not a missing product-identity rule. The blank/"Carly Test" product columns are historical mis-filed rows that the existing **CR-019 / DEF-231 cleanup** (`Scripts/data-ops/2026-06-01-product-miscategorization-*.sql`) corrects. **Do not build this CR** unless the client explicitly asks for a *uniqueness constraint* on products (hard block on duplicate Name+Category+Type+Subcategory) — the display/data problem is solved by DEF-243 + CR-019. The cert-key question is settled: certificates stay keyed on certificate number; the cascade is product-descriptive, never a certificate key.
**Date drafted:** 2026-06-07
**Source:** Client (Shawn), 2026-06-07, on `https://gccwebapp.com/Product/Management`: *"the Product Name should show unique if Category, product type and subcategory are different."* Surfaced while investigating QC #186 ("not seeing all options carried over under Product Management" — Product Type / Subcategory columns rendering blank).
**Estimated effort:** TBD pending the decisions in §5 (display-only ≈ small; uniqueness enforcement + data backfill/split ≈ larger, multi-session, needs DB backup).

---

## 1. Problem / context

The client reported that **Product Type and Subcategory render blank** on the Product Management table for (nearly) all products. Investigation established:

- **Not a rendering bug.** The table column works (e.g. "EcoFlex Sustainable Packaging" correctly shows Subcategory "Cleaning Supplies"), and locally the projection returns Type/Subcategory names whenever the product's IDs are set. Verified: code resolves `Product.ProductType.Name` / `Product.ProductSubcategory.Name` correctly.
- **The product-level fields are simply empty.** Analysts enter Category/Type/Subcategory on the **certificate** (the cert form), not on the **product**. The two are independent fields; nothing syncs them. So the product's own `ProductTypeId` / `ProductSubcategoryId` are NULL for most products, and the table (which reads product-level) shows blank.

Out of that, the client stated the underlying expectation: **a product's identity should be the combination of Name + Category + Product Type + Subcategory** — two products that share a Name but differ on any of those are *distinct* products and should be shown (and treated) as such. Today the blank Type/Subcategory columns make same-named products indistinguishable on the page.

## 2. Requirement (as understood)

> Product uniqueness key = **(Name, Category, Product Type, Subcategory)**. Same Name with a different Category/Type/Subcategory = a different product. The Product Management table must show Type/Subcategory so these are distinguishable, and the system should treat them as distinct (not collapse them as duplicate "Name").

*(Whether "Company" is also part of the identity key is an open question — see §5.)*

## 3. Current state (baseline this CR changes)

- **No product uniqueness constraint exists.** There is no unique index on `Product.Name` (only non-unique performance indexes). The only dedup logic is the **CSV import**, which checks **Name alone** (`ProductController.cs` ~L781‑787: `p.Name == productName` → "Product already exists"). So today the system would *wrongly merge/reject* two genuinely-distinct products that share a Name but differ by Category/Type/Subcategory.
- **Product-level Type/Subcategory are mostly NULL** — the data lives on certificates. (`Product` has its own `ProductTypeId`/`ProductSubcategoryId`, set only via the product Create/Edit form + import.)
- **Type/Subcategory ARE captured on certificates** (`Certificate.ProductTypeId`/`ProductSubcategoryId`), entered on the cert form, and shown on the cert detail view — which is why the cert page shows them while Product Management doesn't.

## 4. What this change implies (scope)

Adopting the new identity rule touches several areas:

1. **Data — backfill.** Populate each product's `ProductTypeId`/`ProductSubcategoryId` (today blank) so the table can display them. Natural source = the product's certificate(s).
2. **Data — the hard case (split).** If a single existing product is linked to certificates with **different** Type/Subcategory values, then under the new rule that one product actually represents **multiple** distinct products. Backfilling a single value loses information; doing it "right" means **splitting** the product and re-pointing each certificate to the correct product. This is a non-trivial, irreversible-without-backup data migration. **Needs client decision (§5).**
3. **Uniqueness enforcement.** Change dedup/validation from "Name" to the 4-tuple in: CSV import, product Create, product Edit. Optionally add a DB unique index on the tuple.
4. **Maintenance going forward.** Decide how product-level Type/Subcategory stay correct: set on the product form (manual), and/or auto-synced from the certificate when a cert is saved.
5. **Display.** Once populated, the existing table columns (and search + CSV export, which read the same fields) will show them automatically — no display code change needed beyond having the data.

## 5. Decisions needed from the client (the point of this review)

1. **Enforcement strength.** Hard rule (block saving / importing a duplicate (Name, Category, Type, Subcategory)) — or soft (just make them visually distinct on the page, no blocking)?
2. **Is Company part of the identity?** Should two *different companies* be allowed the same (Name, Category, Type, Subcategory), or is the company also a distinguishing field? (Recommend: include Company — products belong to a company.)
3. **Existing multi-type products (the split question).** When one product's certificates span different Type/Subcategory values, do we (a) **split** into multiple products (most faithful, highest effort + risk, needs backup), (b) **pick one** value per product (e.g. from the most recent cert) and accept some flattening, or (c) leave existing data as-is and only enforce going forward?
4. **Maintenance source of truth.** Should saving a certificate **write its Type/Subcategory back to the product** (keep them in sync automatically), or is product-level Type/Subcategory entered/maintained separately on the product form?
5. **Are Type/Subcategory now required** when creating a product/cert (since identity depends on them), or still optional?

## 6. Recommended approach (proposed, pending answers above)

A phased path that delivers the visible fix fast and quarantines the risky data work:

- **Phase 1 — make products distinguishable (low risk).** One-time backfill of product-level Type/Subcategory from each product's primary (most-recent) certificate where the product is currently blank **and** all its certs agree; plus write-back on cert save so they stay in sync. Result: the table, search, and export show Type/Subcategory and same-named products read as distinct. Run with a DB backup ([[feedback_data_changes_require_cr]]).
- **Phase 2 — enforcement (after Phase 1 validated).** Switch import/create/edit dedup to the agreed identity tuple; optionally add the unique index.
- **Phase 3 — split the conflicting products (separate data-cleanup CR).** Handle the multi-type products that Phase 1 flagged (couldn't safely auto-fill). Backup-first, reviewed list, its own track — never bundled with code per the pre-deploy rules.

This keeps the client-visible win (populated columns) in a small, reversible step and isolates the irreversible data restructuring for explicit sign-off.

## 7. Why this is a CR, not a defect

The table itself is not broken — it correctly displays product-level data that is simply empty. This request **redefines product identity / uniqueness** (new rule), changes dedup behavior across import/create/edit, and requires **data backfill and possibly data splitting**. That is scope expansion beyond the original product model, and the data changes require a CR + backup per project rules — not a code bug fix.

## 8. Out of scope / notes

- **Partner API** (`/api/v1/certificates*`) — unaffected; contract immutable ([[feedback_amazon_api_immutable]]).
- The interim "derive the table display from the certificate" experiment was **reverted** — it papered over the blank columns without addressing identity, and conflicts with the product-level-as-truth direction the client chose.
- Cert-level Type/Subcategory entry is unchanged.
- Linked: [[feedback_data_changes_require_cr]], QC #186 (DEF-243 cert category sticks, DEF-246 validation state, DEF-254 product edit carry-over — all the same client thread).

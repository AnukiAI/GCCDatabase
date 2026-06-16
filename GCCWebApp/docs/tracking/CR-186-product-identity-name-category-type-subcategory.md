# CR-186 — Product identity = Name + Category + Product Type + Subcategory

**Status:** **DIRECTION CHANGED 2026-06-15 (Shawn) — PHASE 1 SUPERSEDED, MOVING TO THE SPLIT (Phase 2/3).** Shawn clarified the actual requirement: *"the product, product type and subcategory have to match for a certificate to associate the count with. If a cert has the same product but different product type and subcategory it should be a separate line item on the product management page."* This is the **identity/split** model, not the single-value sync. Product Management must show **one line item per (Product, Product Type, Subcategory)**, each its own editable record with its own cert count. **Phase 1 artifacts were WITHDRAWN:** the interim single-value sync, its deploy zip, and the most-recent-wins backfill SQL (all deleted) implemented the superseded "one value per product" rule. The shipping build is the SPLIT model — see §10 and the `...1737.zip`. The split build needs a signed-off plan + DB backup before any migration (see **§10**). Open blocking decisions in §10/§5.

*(Prior Phase 1 status, retained for history:)* **PHASE 1 — DECIDED + BUILT 2026-06-15 (pending GCC backfill approval).** The client re-reported on 2026-06-15 that Product Type & Subcategory are still blank on `/Product/Management`. A read-only live probe (`Scripts/smoke-prod/product-type-subcat-probe.js`) confirmed **107 of 118 products have NULL `ProductTypeId`/`ProductSubcategoryId`** — the endpoint, EF join, and table render are all correct; the product-level fields are simply empty because the data lives on the certificate. **Client decisions (2026-06-15):** *(1)* *"for each product, the product type and subcategory will be unique; the fields should be populated when a certificate is created or edited"* and *(2)* *"if there is not a value for product type and subcategory then it should be blank."* → single value per product, certificate is the source of truth, product **mirrors the saved cert** (overwritten on every create/edit, **blanked when the cert has no value**), most-recently-saved cert wins. **Built:** the code sync + the one-time backfill SQL (most-recent typed cert rule; products with no typed cert stay blank). The live backfill is gated on GCC approval + DB backup (nothing run against prod). Phase 2 (hard *enforcement* of uniqueness) stays on hold; Phase 3 (splitting multi-type products) is **no longer needed** for the columns fix (resolved via most-recent-wins). See **§9 — Phase 1 AS-BUILT** below.

*(Prior status, retained for history: ON HOLD — likely unnecessary; superseded by DEF-243 (deployed) + CR-019 (data cleanup). The 2026-06-08 review (client image 82, cert 26-0168) showed the symptom was partly already-fixed code (DEF-243, live 2026-06-04) + stale pre-fix data. The cert-key question is settled: certificates stay keyed on certificate number; the cascade is product-descriptive, never a certificate key. An interim "derive the table display from the certificate" experiment was reverted — Phase 1 instead **persists** the value onto the product, which is the §6 recommended path.)*
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

---

## 9. Phase 1 — AS-BUILT (2026-06-15)

**Client decisions (2026-06-15):** *(1)* *"for each product, the product type and subcategory will be unique; the fields should be populated when a certificate is created or edited."* *(2)* *"if there is not a value for product type and subcategory then it should be blank."* This settles the §5 open questions for Phase 1: each product holds a **single** Product Type + Subcategory; the **certificate is the source of truth**; the product is a **mirror of the cert being saved** — synced on every create/edit, and **blanked when the cert has no value** (no stale value retained). On conflict the most-recently-saved certificate wins. No Phase 3 split required.

Phase 1 = populate the product-level Type/Subcategory. Two parts: a **code sync** (going forward) and a **one-time data backfill** (existing rows), both applying the same most-recent-cert rule.

### 9a. Code — sync on certificate save (going forward)
- **File:** `Controllers/CertificateController.cs` — private helper `PropagateProductClassificationFromCertAsync(productId, certProductTypeId, certProductSubcategoryId)`, called after save in **both** the Create and Edit POST paths (after `SyncProductIdStatusesForCertificateAsync`).
- **Behavior (mirror the cert, per the decisions):** when a cert is saved, the linked product's `ProductTypeId` + `ProductSubcategoryId` are set to the cert's values (taken from the same cert to keep the parent/child pair consistent). Because the cert is the source of truth and each product holds one value, this **overwrites** the product (last save wins). Decision (2) — *if the cert has no value, the product is blanked* (null is copied through; no stale value retained). A no-op write is skipped to avoid `UpdatedAt` churn.
- **Build:** compiles clean (`dotnet build`, 0 errors). No schema change, no migration. Partner API untouched ([[feedback_amazon_api_immutable]]).
- **Known follow-up:** bulk **CSV certificate import** (`Services/Import/CertificateImportService.cs`) does not yet call this sync, so future imported certs won't auto-update the product. Existing imported certs are covered by the one-time backfill below. Flagged for a small follow-up if the client imports certs regularly.

### 9b. Data — one-time backfill of existing rows (needs GCC approval + backup)
- **Scripts:**
  - `Scripts/data-ops/2026-06-15-product-type-subcat-backfill-IDENTIFY.sql` — **read-only.** Headline counts (total / already-set / blank / fillable / no-typed-cert), the exact fill worklist, and an informational list of products whose certs disagree (most-recent chosen). Run first; this is what GCC reviews.
  - `Scripts/data-ops/2026-06-15-product-type-subcat-backfill-REMEDIATE.sql` — **guarded, ROLLBACK-by-default.** For each blank product, copies Type + Subcategory from its **most-recently-updated certificate that has a type**. Prints before/after counts; flip `ROLLBACK TRAN` → `COMMIT TRAN` after verifying against IDENTIFY.
- **Rule mirrors the code:** only `ProductTypeId IS NULL` rows are touched (already-set products re-sync on their next cert save); both fields come from the same most-recent typed cert.
- **Gate:** live business-data change — requires written GCC go-ahead on the reviewed IDENTIFY worklist + a full DB backup immediately before REMEDIATE ([[feedback_data_changes_require_cr]]). **Nothing has been run against live.**

### 9c. Verification
- **LOCAL — DONE 2026-06-15 on `http://localhost:5101` (dev DB `GCCDatabase_Dev`):**
  - Going-forward sync (`Scripts/smoke-prod/cr186-sync-verify-local.js`): edited cert `26-0023` (product #77) — set Product Type "Additives" → product showed "Additives" (was blank) ✅; cleared the cert's Product Type → product blanked, no stale value ✅. **Both decisions PASS.**
  - Backfill IDENTIFY ran clean (T-SQL valid): dev had 314 products, 6 already set, 308 blank, 1 fillable (#6 Bottles → Plastics/PET), 307 with no typed cert → correctly stay blank. No conflicting-type products on dev.
  - Backfill REMEDIATE **dry-run** (ROLLBACK-by-default): filled 1, 308→307 blank, rolled back — nothing committed. Script validated end-to-end.
- **PROD — pending:** pre = live probe 107/118 blank (2026-06-15). After GCC-approved backfill: re-run `Scripts/smoke-prod/product-type-subcat-probe.js`, expect blanks to drop to only products with no typed certificate. Code deploy: create/edit a cert with a Type → linked product mirrors it immediately (clearing the type blanks the product), no SQL re-run.

### 9d. Still on hold (not Phase 1)
- **Phase 2** — uniqueness *enforcement* (hard-block duplicate identity on import/create/edit, optional unique index). The decision establishes uniqueness as the data shape; whether to *enforce* it with a blocking constraint is still a separate, larger change — needs §5 decisions 1, 2, 5. Not built.
- **Phase 3** — no longer required for the blank-columns fix: the decision resolves conflicting-cert products via most-recent-wins rather than splitting. A split-into-distinct-products effort would only be revisited if the client later wants genuine multi-type products as separate records.

---

## 10. SPLIT model — locked spec (2026-06-15, supersedes Phase 1)

Shawn changed direction: a Product Management line item is keyed by **(Company, Name, Category, Product Type, Subcategory)**. Same product Name with a different Type/Subcategory = a **separate product record** = separate line item, with its own certificate count. Phase 1's single-value sync is withdrawn (held, not shipped).

### 10.1 Locked decisions
1. **Identity key** = Company + Name + Category + Product Type + Subcategory. (Category is implied by Type, kept explicit for the record.)
2. **Cert form UX unchanged** — analyst picks a product by name and sets Type/Subcategory as today; on save the backend **finds-or-creates** the product record matching the full identity and points the cert at it.
3. **CSV import dedup** changes from Name-only to the full identity tuple (today it would merge distinct combos).
4. **Type/Subcategory stay OPTIONAL** — a (blank, blank) combo is a valid line item (its own record).
5. **No hard unique constraint this phase** — the split itself makes records distinct; a unique index is an optional small follow-up if enforcement is wanted.

### 10.2 Going-forward code (reversible — ships first)
- `CertificateController` Create + Edit: replace the held `PropagateProductClassificationFromCertAsync` with `ResolveProductForCertAsync(basePickedProductId, categoryId, typeId, subcatId)` → returns the resolved/created product id, assigned to `certificate.ProductId`.
- Resolution: load the picked base product (for Company/Name/SKU/Description); category for the record = cert's `ProductCategoryId` ?? base product's `CategoryId`; match an existing non-deleted product with the same Company+Name+Category+Type+Subcategory; if none, create one cloning the base product's identity-neutral fields. Null Type/Subcategory match handled in-memory (avoids EF null-equality pitfalls).
- New certs/combos therefore produce their own product records immediately; existing data unchanged until the migration runs.

### 10.3 CSV import (reversible)
- **Product import — DONE.** `ProductController` product-import dedup changed from **Name-only** to the full identity tuple (Company + Name + Category + Type + Subcategory). Same Name with a different Type/Subcategory now imports as a distinct record instead of being rejected as a duplicate. Null Type/Subcategory matched in-memory.
- **Certificate import — DONE.** `Services/Import/CertificateImportService.cs` now re-resolves each row's product through `ResolveProductForImportAsync` at commit (after the legacy name-keyed step), so same-name/different-type imported certs land on **separate** product records — the import equivalent of the cert form's find-or-create. Verified by `Scripts/smoke-prod/cr186-import-split-verify-local.js`: a 2-row CSV (same name, Additives vs Metal Alloys) created 2 product records (#652/#653), one cert each. **Caveat (cosmetic):** the *preview* still counts "products to create" by name (reported 1), while the commit correctly makes 2 — preview count under-reports splits. The import has **no Subcategory/Category column**, so Subcategory resolves null and Category falls back to the base product's; adding those columns is a separate import-feature enhancement if needed.

### 10.4 One-time migration (IRREVERSIBLE — backup + CR-gated, runs last)
- Scripts: `Scripts/data-ops/2026-06-15-product-split-by-type-subcat-{IDENTIFY,REMEDIATE}.sql` (supersede the withdrawn single-value backfill, which is deleted).
- For each existing product whose certs span K distinct (Type, Subcategory) combos: keep the product for its **primary** combo (most certs), create K−1 new records (cloning Name/Company/Category/Description/SKU/Model), re-point each cert to its matching record. Certs with no Type/Subcategory → a (blank) combo record. Single-combo / no-cert products: set their Type/Subcategory, no split.
- **Default (flagged):** product-level Identifiers/Tags stay on the primary (most-certs) record, not duplicated.
- Safety: ROLLBACK-by-default; `QUOTED_IDENTIFIER/ANSI_NULLS ON` (Products has a computed/indexed column → 1934 otherwise). IDENTIFY (read-only worklist) → GCC review → full DB backup → REMEDIATE.
- **Validated on dev DB 2026-06-15 (dry-run):** 315→316 products, 24 primaries set, 1 split (product #6 "Bottles" → kept Plastics/PET + new #651 blank combo), 1 cert re-pointed, rolled back.
- **PROD read-only IDENTIFY + ROLLBACK dry-run run 2026-06-15:** 118→122, 46 primaries set, 3 split, 4 new records, 4 certs re-pointed. Script behaves correctly on live data. **DB export/backup taken.**
- **⚠ HOLD — do NOT commit (2026-06-15).** Investigation of the 3 splitting products (#119 "test", #127 "3M Test Product", #141 "Best Product Ever v3T") found they carry **7 certificates, 3 of them published `Client Approved - Paid`** (26-0163, 28-0003, 26-0175) — so they are NOT disposable test data, and GCC's "OK to delete" was given on a wrong premise. Per Shawn 2026-06-15: **re-confirm with GCC before split OR delete.** Revised GCC note drafted (`CR-186-split-gcc-note.md`, "SEND THIS — REVISED" section); the delete-recommending versions are marked superseded. Commit blocked pending GCC's real/test confirmation on those 3 products + their Paid certs.

### 10.5 Build order
1. `ResolveProductForCertAsync` (code) + local test — ✅ **DONE.** `Controllers/CertificateController.cs` (Create + Edit resolve via find-or-create; held single-value sync removed). Verified by `Scripts/smoke-prod/cr186-split-verify-local.js`: editing cert 26-0023's Type → new product line item, count followed.
2. Import dedup → tuple — ✅ **DONE.** `Controllers/ProductController.cs` product import dedups on the identity tuple (was Name-only). `Services/Import/CertificateImportService.cs` re-resolves each row to the tuple record at commit (`ResolveProductForImportAsync`) — verified by `cr186-import-split-verify-local.js` (2 same-name rows → 2 records by type). §10.3.
3. Split migration IDENTIFY + REMEDIATE — ✅ **DRAFTED + dev dry-run validated** (§10.4). Irreversible; runs only after GCC review of IDENTIFY + backup.
4. New deploy zip (replaces the held `...1630.zip`) — **next.** Bundles CR-186 code + DEF-264 PDF-download fix.
- Effort estimate: ~1.5–3 days. Nothing irreversible runs without the IDENTIFY review + backup.

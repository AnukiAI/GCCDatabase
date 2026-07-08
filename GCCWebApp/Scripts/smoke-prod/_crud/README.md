# CRUD Round-Trip Suite (`_crud/`)

**Write-path** regression — the biggest coverage gap (C1 in `docs/qa/regression-e2e-strategy.md`):
the read-only smokes only assert *"the form loads"*, never that a **save persists** or a **delete
removes**. These specs exercise the real write path (controller → service → EF → DB) and verify
persistence directly in SQL.

## ⚠ This suite WRITES to the dev DB — safety model

Non-negotiable rules baked into `_lib/write-harness.js` and every spec:

1. **Fixtures only.** Every written row is a self-created fixture marked `ZZ_SmokeFixture_*` (same
   convention as the `_views/` state-matrix). Real business rows are never touched.
2. **No product pollution.** Certificate create does CR-186 find-or-create on products. Specs clone
   an **existing product's own dims** (Name+Category+Company+Type+Subcategory) so resolution *finds*
   rather than *creates*, and assert a **Products-count invariant** to prove it.
3. **Guaranteed teardown.** Happy path deletes via the app; a `finally` SQL backstop removes any
   fixture left by a mid-flow failure. Post-run counts must return to baseline.
4. **Local only.** `guardRemote()` refuses gccwebapp.com. Runs against `localhost:5101` per the
   hybrid-env decision (`feedback_testing_environment`).

## Specs

All five entity round-trips build; full suite **GREEN 39P / 0F** and leaves zero fixture residue.

| Spec | Entity | Endpoints | Delete | Asserts |
|---|---|---|---|---|
| `tag-crud-roundtrip.js` | Tag | `POST/PUT/DELETE /Admin/*Tag` (JSON) | hard | create→persisted, rename→persisted, delete→gone, count net-zero. |
| `company-crud-roundtrip.js` | Company | `POST /Company/SaveCompany`, `DELETE /Company/DeleteCompany` | soft | create/rename/soft-delete verified in SQL, count net-zero after teardown. |
| `product-crud-roundtrip.js` | Product | `POST /Product/SaveProduct`, `DELETE /Product/DeleteProduct` | hard | create/rename/delete; SaveProduct has no find-or-create → no pollution. FKs (Category+Company) discovered from DB. |
| `user-crud-roundtrip.js` | User | `POST /Admin/CreateUser`, `PUT /Admin/UpdateUser`, `DELETE /Admin/DeleteUser` | soft | create/rename/soft-delete; NON-Admin role (deletable). Name lives in `FullName` (the `Name` column is unused/NULL). |
| `cert-crud-roundtrip.js` | Certificate | MVC form `Create`/`Edit`/`Delete` (multipart + antiforgery) | hard | create→302→persisted, edit→persisted, delete→removed, **Products-count invariant** (CR-186 no pollution). |

**Antiforgery:** the four admin JSON APIs validate the `RequestVerificationToken` **header** (sourced from the hidden input); the cert form uses the classic `__RequestVerificationToken` form field. Both handled by the harness (`jsonReq` / `postForm` + `antiforgeryToken`).

## Requirements & run

- Dev server on `localhost:5101` **and** `sqlcmd` (connection parsed from `appsettings.Development.json`).
- CR-187 note: statements touching `Certificates` need `SET QUOTED_IDENTIFIER ON` (handled by the harness).

```bash
node Scripts/smoke-prod/_crud/run-all.js              # all CRUD specs
node Scripts/smoke-prod/_crud/cert-crud-roundtrip.js  # single spec
```

Any FAIL/BLOCKED exits non-zero. Result JSON lands in `../_results/`.

## Result semantics

- **PASS** — the write persisted / removed as expected.
- **FAIL** — a broken write path (save didn't persist, delete didn't remove) **or a safety breach**
  (product pollution, non-zero fixture residue). Investigate before deploy.
- **BLOCKED** — couldn't run (no clonable product, login/sqlcmd failure). Not green until resolved.

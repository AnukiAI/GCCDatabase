# CR-187 — Normalized, user-friendly product/certificate search

**Status:** Built + locally verified 2026-06-16. Pending deploy (Gate 7 prod SQL) + client UAT.
**Register:** see `change-request-register.md` → CR-187.

## Problem (client, 2026-06-16)
Search required an exact character match. `Product A` did not return `Product™ A`. Trademark/
registered/copyright symbols, punctuation, hyphens, extra spaces, and case differences hid
products from users who didn't type the exact stored formatting.

## Decision
Data will grow large → implement the scalable **persisted computed column** model (normalize once
on write, search a precomputed indexed column) rather than recomputing `REPLACE` per row per query.
Apply consistently across **all** searchable fields.

## Design — one rule, two halves that can't drift
`Search/SearchNormalizer.cs` is the single source of truth:
- `Normalize(string)` — reduces the **search input** (lowercase; strip `™ ® © ℠ °`, punctuation,
  dashes, quotes, brackets, separators, and **all** whitespace; `%`/`_` LIKE wildcards stripped too).
  Removing all whitespace makes "Product   A" and "Product A" collapse to the same value, so
  multi-space handling is automatic.
- `ToSqlExpression(column)` — generates the SQL `LOWER(REPLACE(REPLACE(...)))` for the **stored**
  side from the *same* `RemovableChars` array. Because both halves derive from one array, the C#
  and SQL normalization are structurally identical, not just "kept in sync by hand."

Persisted computed columns (SQL Server maintains them on every insert/update — UI, CSV import, API,
raw data-ops alike → **no backfill, no write-path code**), each indexed:

| Entity | Column(s) |
|--------|-----------|
| Product | `NormalizedName` |
| Company | `NormalizedName` |
| CertificationType | `NormalizedName` |
| ProductCategory | `NormalizedName` |
| Tag | `NormalizedName` |
| Certificate | `NormalizedNumber`, `NormalizedLocation`, `NormalizedCountry` |

Pattern mirrors the existing `Certificate.CertificationResultsNumeric` computed column (DEF-181).

## Call sites updated (website + admin)
- `Repositories/CertificateRepository.cs` — `SearchCertificatesAsync`, `GetPagedCertificatesAsync`,
  `GetAdvancedFilteredCertificatesAsync` (main tile/list search), `GetCertificatesByLocationAsync`,
  `GetCertificatesByCountryAsync`. Removed `NormalizeForSearch` + `GetSearchTermVariations`.
- `Services/CertificateService.cs` — list, by-partner, and export search blocks.
- `Services/FilterService.cs` — cert-type / company / country filter-option search + filter counts.
- `Controllers/Api/CertificateTileInternalApiController.cs` — main search + autocomplete; removed local `NormalizeForSearch`.
- `Controllers/Api/CertificateTileExternalApiController.cs` — main search + autocomplete (role scoping preserved); removed local `NormalizeForSearch`.
- `Controllers/CertificateController.cs` — `SearchProducts` (create-form autocomplete) + `FindSimilarProducts` (duplicate detection) now use the shared normalizer.
- `Controllers/Api/CertificateTilesApiController.cs` (public) — unchanged; delegates to `GetAdvancedFilteredCertificatesAsync`.

### Deliberate scope boundary
`/api/v1/*` partner endpoints (`ProductsController`, `CertificatesController` search) **not changed** —
match semantics there are part of the partner contract; altering them needs partner sign-off
(`feedback_amazon_api_immutable`). Faceted filter checkboxes remain exact-list matches by design.

## Acceptance criteria → evidence
| AC | Result |
|----|--------|
| `Product A` returns `Product™ A` | ✅ DB round-trip: stored `NormalizedName`=`producta`; normalized search returns the row |
| Without ™/® returns symboled products | ✅ same; `®`/`©` covered by unit tests |
| Case-insensitive | ✅ `product a` ≡ `Product A` (unit test) |
| Extra spaces / punctuation | ✅ `Product   A`, `Product: A` → `producta` (unit + DB) |
| Existing exact-match still works | ✅ exact term is a substring of its own normalized form |
| No records hidden by special chars | ✅ stored side strips the same chars; symboled names become findable |
| Results ordered logically | ✅ existing sort/ranking preserved (autocomplete still ranks exact→starts-with→contains) |
| Automated tests added | ✅ `GCCWebApp.Tests/SearchNormalizerTests` — 24 cases (™ ® ©, case, hyphen, multi-space, colon, leading/trailing, partial, parity, SQL-gen guards) |

## Verification performed (local, 2026-06-16)
- `dotnet build` — clean (0 errors).
- `dotnet ef database update` — migration applied to `GCCDatabase_Dev`; computed columns auto-populated.
- `dotnet test` — **24/24 pass**.
- DB round-trip (rolled-back transaction): `Product™ A` → `producta`; normalized `producta` search
  returns it; `Product:   A` → `producta`. Confirms C#↔SQL parity on real data.
- **E2E smoke (live app on :5101): `Scripts/smoke-prod/cr187-normalized-search-local.js` — 10/10 PASS.**
  Seeds a `ZZ CR187 Smoke Product™ A` product, then the product autocomplete returns it for
  plain (no ™) / lowercase / hyphenated / extra-spaces / exact-with-™ / partial queries (plus a
  negative control); the internal tile search (DB computed-column path) returns cert `26-0023` for
  the symbol+case-mangled company query `ACME CORPORATION™`; seeded product deleted at the end.
  Results JSON under `Scripts/smoke-prod/_results/`.

## Deploy — Gate 7 (prod never auto-migrates)
Run in the same deploy step as the code drop:
```
Scripts/data-ops/2026-06-16-CR187-normalized-search-columns-PROD.sql   (idempotent)
```
Computed columns populate existing prod rows automatically — no separate backfill. Verify after:
`SELECT TOP 5 Name, NormalizedName FROM Products;` should show lowercased, symbol-stripped values.

## Follow-ups (optional)
- ~~E2E Playwright smoke~~ — **done**: `Scripts/smoke-prod/cr187-normalized-search-local.js` (10/10 PASS).
- If the catalog ever needs substring search at very large scale, consider SQL Server Full-Text on
  the `NormalizedX` columns (leading-wildcard `LIKE` can't seek a B-tree index).

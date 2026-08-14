# CR-037 — Multiple countries per certificate (Country single-select → multi-select)

**Status:** Proposed — not built. No code written.
**Raised:** 2026-08-13 (Anuki, from a client observation 2026-08-12)
**Precedent:** [[CR-171]] (multi-select Partner) — same single→collection shape, one field over.

---

## 1. Source

Client observed that some certificates appear to carry more than one country and asked whether the
design supports it, citing **26-9633**. Investigated 2026-08-13 against the test-env database and
the 1,021-record public GreenCircle mirror.

## 2. Why this is a CR, not a defect

The original requirements baseline scoped **one country per certificate**. The system implements
exactly that:

| Layer | Today |
|---|---|
| Model | `Certificate.Country`, `nvarchar(100)`, single value (`Models/Certificate.cs:25`) |
| UI | Single `<select>` (`Views/Certificate/Create.cshtml:293`, Edit equivalent) |
| Picker source | `CountryList.Countries` — a **static in-code list**, not a database table (`CertificateController.cs:346, 926`) |
| Search | `NormalizedCountry`, a persisted computed column derived from `Country` (CR-187) |

Nothing is broken. Holding several countries is new capability, so it is scope expansion.

## 3. Key finding — the ask is narrower than it first appears

Three different situations were being read as "multiple countries." Separating them is what makes
this CR optional rather than urgent:

| Group | Rate | Example | Verdict |
|---|---|---|---|
| Several sites, **one** country | majority | `24-0304` — `Buffalo, NY; Cedar Rapids, IA` / United States | **Already works.** Location is free text and lists every site. Out of scope. |
| Sites across **>1** country | ~2% | `23-0925` — `Gyongyos, Hungary and Marktheidenfeld` / Germany | **This CR.** The second country exists only as free text — not filterable, not searchable. |
| Country **contradicts** Location | a handful | `26-9633` — `Seattle, WA` / **Mexico** | **Data ops, not code.** Wrong dropdown value. Needs analyst correction ([[feedback_data_issue_vs_defect]]). |

Cross-border rate is consistent across both data sets: **4 of 228** on the test env, **19 of 1,021**
in the public mirror, spanning 35 distinct countries. Worst case is `25-8767` — five sites across
the US and Canada; one record spans US + China + Poland.

## 4. The question that decides whether to build this at all

> **Does GCC need to search and filter by every country involved, or only to see them?**

**Seeing them already works** — Location is free text and carries every site. If display is
sufficient, this CR closes for free and no code is written. **Only filtering justifies the build**,
and that is the expensive half: the join table is trivial, but making a collection filterable and
sortable is what touches the tile APIs, the persisted search columns, and the export template.

Ask this before anything below is estimated.

## 5. Partner API impact — none

`certificationCountries` is **hardcoded to the literal `"All"`** for partner callers
(`Controllers/Api/v1/CertificatesController.cs:254, 318, 1067, 1668`) per Dev Guide §4/§8. Only
internal/non-partner callers project `c.Country`.

So the `/api/v1/certificates*` response shape and values are **byte-identical** before and after
this change, and `Scripts/smoke-prod/amazon-contract-diff.js` should still print CONTRACT UNCHANGED.
No partner sign-off, no Amazon re-certification ([[feedback_amazon_api_immutable]]). This is the
single biggest cost saving versus [[CR-035]] / [[CR-036]], and it should be stated to the client so
the change isn't over-feared.

## 6. Model decision — countries are not an entity, unlike Partner

[[CR-171]] worked as a true M2M because `Partner` is a managed entity with `IsActive`, `SortOrder`
and login association. **Country is not** — it's a static string list. Two options:

| Option | Shape | Assessment |
|---|---|---|
| **A — string join table (recommended)** | `CertificateCountries(CertificateId, Country nvarchar(100))` | No new entity, no seed data, no admin screen. Matches how the picker already works. Cheapest path to the same result. |
| B — `Country` entity + true M2M | Seed a `Countries` table from `CountryList`, join like `CertificatePartners` | Only worth it if GCC wants to *manage* the country list (add/retire/sort) — nobody has asked. Adds a table, a seed, a migration and an admin surface. |

**Recommend A** unless the client wants a managed country list, which would be its own CR.

**Keep `Certificate.Country` as the denormalized primary**, exactly as CR-171 kept the legacy
`Partner` string: it feeds internal sort, CSV, and the `NormalizedCountry` computed column, and
dropping it would multiply the blast radius for no benefit ([[feedback_denormalized_copy_width_mismatch]]
— if kept, the join column must be `nvarchar(100)`, matching the source, not narrower).

## 7. Scope (if approved)

1. **Schema** — `CertificateCountries` join table + migration; `Certificate.Country` retained as the
   primary/first-listed value.
2. **Search (CR-187)** — `NormalizedCountry` is computed from `Country` and therefore normalizes the
   **primary only**. Either accept that secondary countries aren't fuzzy-searchable, or extend the
   normalization to the join rows. This is the least obvious cost in the CR
   ([[feedback_search_normalizer_single_source]] — do **not** add a second normalize helper).
3. **UI** — Create/Edit `<select multiple>` (mirror the CR-171 partner control); certificate detail
   and tile views render the full set.
4. **Filters/sort** — country filter on the internal + external tile APIs moves from equality to
   collection membership; add `.AsSplitQuery()` per [[feedback_ef_includes_with_projection]].
   **Check every adjacent endpoint, not just the list** ([[feedback_scoping_check_adjacent_endpoints]]).
5. **CSV import/export** — pipe-delimited multi-value column + template header hint, exactly as
   DEF-316 did for Partner; `SyncCertificateCountriesAsync` on both create and update paths.
6. **Migration transform** — supersedes plan §87 (see §9).
7. **Gate 7 backfill SQL** — every existing certificate gets one join row from its current `Country`,
   or country filtering returns nothing for historical records. Prod never auto-migrates
   ([[project_predeploy_agent_checklist]]).

**Out of scope:** any change to the partner API; a managed/admin-editable country list; multi-country
at product-identifier level (no analogue to CR-171 Phase 2 has been asked for); correcting the group 3
data errors (analyst task).

## 8. Related defect — DEF-347, import writes Country unvalidated

**Independent of this CR and worth fixing regardless.** CSV import assigns `Country` as a raw string
with no check against `CountryList` (`Services/Import/CertificateImportService.cs:1097`, `:1206`,
`:1771`). That is how the value **`Atlantis`** is currently sitting in the test-env `Country` column.
Logged as **DEF-347** (2026-08-13, Open) and must be fixed **before** the 5,556-row go-live load, or
junk enters at scale and silently.

**Deliberately not folded into this CR.** CR-037 may be retired for free if the client answers
"display is enough" (§4); DEF-347 must ship either way, so tying it here would make a mandatory
correction hostage to an optional feature.

## 9. Migration interaction — this part is blocking and independent

`migration-import-transform-plan-2026-06-22.md` **§87** currently says, for the expired/historical
set whose `LOCATION` column is combined: *"strip trailing country into Country."*

Against a multi-site string that takes the **last** country. `25-8767` would load as **Canada** with
its three US sites silently dropped from the Country field.

**This must be fixed for the go-live load whether or not CR-037 is ever built.** Proposed rule:
**first-listed site sets Country**, full original string preserved in Location (175 cap), and every
cross-border row written to a review CSV rather than guessed at. If CR-037 *is* approved, the same
transform instead populates all countries into the join table and the rule becomes moot — which is
an argument for deciding this CR before the load, not after.

## 10. Effort

**≈ 2–2.5 days**, assuming Option A and that CR-171's pattern is copied rather than re-derived.

| Item | Notes |
|---|---|
| Join table + migration + backfill SQL | Small — mechanical, CR-171 has working precedent |
| Multi-select UI (Create/Edit/detail/tiles) | Small — reuse the partner control |
| Filters/sort to collection membership | **Medium — the real cost.** Every country filter + adjacent endpoints |
| CR-187 normalization decision | Small if primary-only; medium if extended to the collection |
| CSV import/export + template | Small — DEF-316 precedent |

Cheaper than CR-171's 3–4 days because there is **no visibility/isolation dimension** — countries are
not access-control data, so there is no per-requester scoping, no leak risk, and no product-identifier
layer. More expensive than a plain field add because of the CR-187 computed column.

## 11. Decisions required

1. **Filter, or display only?** (§4 — a "display" answer closes this CR at zero cost.)
2. **Option A or B** — string join table, or a managed country list? (Recommend A.)
3. **Does `Country` remain the single "primary"** shown in list columns, CSV and sort? (Recommend
   yes — first-listed wins.)
4. **Secondary countries fuzzy-searchable?** (§7.2 — drives whether CR-187 normalization is extended.)
5. **Who corrects the group 3 mismatches** like 26-9633, and when — before or after the go-live load?
6. **Is this before or after go-live?** Recommend **after**; §9 must be settled either way.

## 12. Related

[[CR-171]] (precedent — multi-select Partner) · [[CR-187]] (normalized search columns) ·
[[feedback_amazon_api_immutable]] · [[feedback_scoping_check_adjacent_endpoints]] ·
`docs/tracking/migration-client-note-2026-08-13-location-country.md` (client email carrying §4's
question) · `migration-import-transform-plan-2026-06-22.md` §87.

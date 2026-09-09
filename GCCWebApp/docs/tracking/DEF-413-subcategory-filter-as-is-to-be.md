# DEF-413 — Certificate subcategory filtering: As-Is / To-Be

**Status:** specification only. **No code has been written.**
**Date:** 2026-09-09
**Register entry:** `docs/tracking/defect-register.md` → DEF-413 (+ addendum)
**Decision still open:** DEF-413 (defect) vs CR-047 (change) — see §8.

---

## 1. The request

Client, 2026-09-09:

> The search functionality is not working for these. The intention for this additional field was to
> have the certs with these show up when filtering. For example, if a client has a CSF cert and it
> has Recycled Content and Waste Diversion cert subcategories selected —
> user selects 'Certified Sustainability Facts' → CSF shows in filtered list;
> user selects 'Recycled Content' → CSF shows;
> user selects 'Waste Diversion from Landfill' → CSF shows.
> This functionality is the same for public and login views.

**One-line diagnosis:** the certificate subcategory field is written, displayed, sorted and exported,
but it is matched by **nothing**. `Certificate.CertificationTypeSubcategories` appears in no `Where`
clause anywhere in the solution. It has been capture-and-display since it was built (M2M shipped
2026-01-27, migration `20260127183149_AddMultipleSubcategoriesToCertificate`).

**A real certificate that reproduces the client's example exactly:**

> **`25-8071`** — JW Aluminum · type **Certified Sustainability Facts** · status Client Approved -
> Paid · publicly visible today · subcategories **Recyclable Material, Recycled Content, Waste
> Diversion from Landfill**.

Today it is returned by the "Certified Sustainability Facts" checkbox and by nothing else.

---

## 2. Pages in scope

Three pages, all three showing the same certificate-tile UI with the same left-hand filter pane. Each
has its **own** API controller and its **own** copy of the filter predicate — this is why the fix has
three sites rather than one.

| # | Page | Route | Audience | API endpoint | Front-end |
|---|---|---|---|---|---|
| **P1** | Public certificate tile view | `/Certificate/CertificateTileView` | Anonymous (`[AllowAnonymous]`) | `GET /api/certificate-tiles` → `CertificateRepository.GetAdvancedFilteredCertificatesAsync` | `wwwroot/js/certificate-tile-view.js` |
| **P2** | External certificate tile view | `/Certificate/CertificateTileViewExternal` | External (company) + Partner | `GET /api/CertificateTileExternal` | `wwwroot/js/certificate-tile-view-external.js` |
| **P3** | Internal certificate tile view | `/Certificate/CertificateTileViewInternal` | Analyst + Admin | `GET /api/certificate-tiles-internal` | `wwwroot/js/certificate-tile-view-internal.js` |

On each page, three separate controls are affected:

- **(a)** the **Certification Type filter checkboxes** in the left pane — the client's worked example
- **(b)** the **free-text search box** — "the search functionality is not working"
- **(c)** the **autocomplete suggestions** under the search box — P2 and P3 only, see §4.3

Not in scope: `/Certificate/CertificateSingleView/{number}`, the Admin pages, `/api/v1/certificates*`
(Amazon), and all exports. See §7.

---

## 3. AS-IS

### 3.1 (a) The filter checkbox — matches the parent type name only

All three surfaces run string equality against **`Certificate.CertificationType.Name`** — the
certificate's single parent type. The subcategory collection is never consulted.

**P1 — `Repositories/CertificateRepository.cs:467-468`** (inside `GetAdvancedFilteredCertificatesAsync`):

```csharp
// New filter properties for API
if (filter.CertificationTypeNames != null && filter.CertificationTypeNames.Any())
    query = query.Where(c => c.CertificationType != null && EF.Constant(filter.CertificationTypeNames).Contains(c.CertificationType.Name));
```

**P2 — `Controllers/Api/CertificateTileExternalApiController.cs:189-194`:**

```csharp
// Apply certification type filter
if (!string.IsNullOrWhiteSpace(certType))
{
    var types = certType.Split('|', StringSplitOptions.RemoveEmptyEntries);
    query = query.Where(c => types.Contains(c.CertificationType.Name));
}
```

**P3 — `Controllers/Api/CertificateTileInternalApiController.cs:154-159`:** byte-identical to P2.

### 3.2 (b) The free-text search box — same omission

The search predicate is a big OR across the searchable fields. Subcategories are not one of them.

**P3 — `CertificateTileInternalApiController.cs:169-176`** (P2 at `:177-185`, P1 at
`CertificateRepository.cs:365-378` — the field lists differ slightly by design):

```csharp
query = query.WhereMatchesSearch(c =>
    c.NormalizedNumber!.Contains(nq) ||
    c.CertificationType.NormalizedName!.Contains(nq) ||   // <- parent type only
    c.Company.NormalizedName!.Contains(nq) ||
    c.Product.NormalizedName!.Contains(nq) ||
    c.Product.Category.NormalizedName!.Contains(nq) ||
    (c.NormalizedLocation != null && c.NormalizedLocation.Contains(nq)) ||
    (c.NormalizedCountry != null && c.NormalizedCountry.Contains(nq)),
    TagSearchMatch.Union(tagIds, countryIds));
```

### 3.3 (c) Autocomplete on P2/P3 suggests terms that return nothing

`CertificateTileInternalApiController.cs:489-491` and
`CertificateTileExternalApiController.cs:477-479` are identical:

```csharp
var certTypes = await _context.CertificationTypes
    .Where(ct => ct.NormalizedName!.Contains(nq))
    .Select(ct => new { value = ct.Name, label = ct.Name, type = "Certification Type" })
```

No `ParentId` guard and no `IsActive` guard. So the box **will suggest Level-2 subcategory names**
(and deactivated types), and accepting the suggestion runs the §3.2 search, which matches parent
names only. Typing `zero` on the internal view suggests **"Zero Waste to Landfill"** and then returns
**0 results** — there is no Level-1 type of that name. P1's autocomplete is derived from live
certificates' parent type names, so it has no dead suggestions.

### 3.4 Why this reads as "broken" rather than "missing"

**Every CSF child name also exists as its own Level-1 type** — 18 such name pairs on
`GCCDatabase_Dev`. Both names in the client's example are among them. So "Recycled Content" **is** an
available checkbox (it is offered as the Level-1 row of that name), and clicking it returns a healthy
458 public certificates. What it never returns is the 68 CSF certificates that carry Recycled Content
as a subcategory. Nothing looks empty or missing; the results are simply incomplete.

### 3.5 What the filter dropdown currently offers — and why that is correct

Since **DEF-167** (shipped 2026-05-11), all three `/filters` endpoints build the list as Level-1 only:

```csharp
var hierarchicalCertTypes = await _context.CertificationTypes
    .Where(ct => ct.IsActive && ct.ParentId == null && visibleCertTypeIds.Contains(ct.Id))
    .OrderBy(ct => ct.Name)
    .Select(ct => new { name = ct.Name, level = ct.Level })
    .ToListAsync();
```

`CertificateTilesApiController.cs:316`, `CertificateTileInternalApiController.cs:585`,
`CertificateTileExternalApiController.cs:644`.

**This matches the original requirement and must not change.** `docs/Final Req Docs/archive/Requirements.xlsx`,
sheet **"Filter Sections"**, column A specifies the Certification Type filter as a **flat list of 15
names with no hierarchy** — in which Certified Sustainability Facts sits as a peer alongside ten of
what are today its own children. All 15 exist as Level-1 rows now; the two inactive ones (Biobased,
Renewable Resource Content) are correctly excluded by the `IsActive` guard.

**So the dropdown already conforms to spec. Only the matching does not.**

### 3.6 Data currently unreachable

`GCCDatabase_Dev`, 2026-09-09 — `CertificateCertificationTypeSubcategories`: **456 link rows across
227 distinct certificates**, of which **150 are publicly visible today**. Most arrived on 2026-09-08
via the DEF-411 backfill and CR-046, which is why the gap surfaced now.

| Subcategory | Certs (all) | Publicly visible certs by parent type |
|---|---|---|
| Waste Diversion from Landfill | 181 | Certified Sustainability Facts — 103 |
| Recycled Content | 89 | Waste Diversion from Landfill — 25 |
| Sustainable Energy Practices | 67 | Certified Environmental Facts — 18 |
| Life Cycle Assessment (LCA) Optimized | 31 | Carbon Footprint Reduction — 2 |
| Carbon Footprint Reduction | 25 | GreenCircle Audited — 1 |
| Sustainable Manufacturing Practices | 22 | Sustainable Manufacturing Practices — 1 |
| Zero Waste to Landfill | 20 | **150 total** |
| Certified Energy Savings | 17 | |
| Recyclable Material | 4 | |

---

## 4. TO-BE

### 4.1 (a) The filter checkbox — matches type **or** any subcategory

> Selecting a Certification Type filter option returns a certificate when **either** the
> certificate's certification type **or** any of its certification-type subcategories carries that
> name. Identical on all three pages. The list of available options does not change.

**Client's example, after the change** — `25-8071` (JW Aluminum, CSF) is returned by:
"Certified Sustainability Facts" ✔ · "Recycled Content" ✔ · "Waste Diversion from Landfill" ✔ ·
"Recyclable Material" ✔.

**Measured effect on the public view** (dev data, publicly-visible certificates only — these are the
exact numbers to assert against in test):

| Filter option | As-Is | To-Be | Δ |
|---|---|---|---|
| Certified Sustainability Facts | 103 | 103 | — |
| Recycled Content | 458 | **526** | +68 |
| Waste Diversion from Landfill | 46 | **151** | +105 |
| Sustainable Energy Practices | 69 | **112** | +43 |
| Certified Energy Savings | 52 | **66** | +14 |
| Life Cycle Assessment (LCA) Optimized | 43 | **63** | +20 |
| Carbon Footprint Reduction | 10 | **28** | +18 |
| Recyclable Material | 5 | **9** | +4 |
| Sustainable Manufacturing Practices | 2 | **23** | +21 |

Every other option is unchanged. **No option loses results** — the change is purely additive, so the
"every option returns ≥ 1 result / no dead clicks" guarantee is strengthened, not weakened.

**The one semantic consequence to be aware of.** "Recycled Content" comes to mean two things at once:
*certificates whose type is Recycled Content* **and** *CSF certificates carrying Recycled Content as
a subcategory*. That is what the client asked for and what the flat requirement list implies, but it
is a widening of an existing, client-validated control. Worth one sentence of confirmation — see §8.

### 4.2 (b) The search box — subcategory names become searchable

> Typing a subcategory name into search returns certificates carrying it, in addition to
> certificates whose type or other searchable fields match.

After this, `25-8071` is found by searching `recycled content` as well as `certified sustainability
facts`, on all three pages.

### 4.3 (c) Autocomplete — **decision required, not assumed**

Once §4.2 ships, a suggested subcategory name that has links returns results, so most of the
dead-suggestion problem disappears on its own. Two residual cases remain on P2/P3:

- names with zero links (**Waste System Audit**, **Waste Diversion Event**) — still suggest, still return 0
- **deactivated** types (Biobased, Biodegradable, Renewable Resource Content, Unspecified, Verified
  Health Product Declaration) — suggested despite being excluded from the dropdown

**Recommendation:** add `ct.IsActive` to both autocomplete queries. Leave the zero-link case alone —
guarding it costs an extra query per keystroke for two names. **Flagged, not bundled:** this is a
separate one-line change and should be an explicit yes/no, not something absorbed silently.

### 4.4 What deliberately does NOT change

| Thing | Why it stays |
|---|---|
| **Filter dropdown contents** (Level-1 only, `IsActive`, visible-cert guard) | DEF-167, and it matches the "Filter Sections" spec (§3.5) |
| **The three orphan Level-2 names** (Waste Diversion Event, Waste System Audit, Zero Waste to Landfill) | Adding them puts Level-2 rows back in the box the client rejected in April → **CR-047**, §7 |
| **`/api/v1/certificates*`** (Amazon partner API) | Contract is immutable |
| **All exports** | DEF-412 already projects `Cert Subcategories`; nothing here touches export |
| **Role scoping / visibility** | Untouched — see the security note in §5.1 |
| **The 8-state workflow** | Untouched |
| **The deprecated single `CertificationTypeSubcategoryId`** | Not read by this change; the M2M collection is the source of truth |

---

## 5. The exact change

**Five files. Four if autocomplete (§4.3) is declined.**

### 5.0 The performance constraint that dictates the shape

The obvious implementation is wrong here, and expensively so:

```csharp
// DO NOT WRITE THIS
query = query.Where(c => types.Contains(c.CertificationType.Name)
                      || c.CertificationTypeSubcategories.Any(s => types.Contains(s.Name)));
```

An `.Any()` over a collection navigation sitting **inside an OR** cannot be collapsed into a
semi-join; SQL Server re-evaluates it once per row of the already-joined result. This project has
paid for that lesson twice, both documented in `Search/TagSearchMatch.cs`:

- **DEF-366** (tags): 3,200 ms → 212 ms with the clause removed; 17,106 logical reads against a
  **four-row** table
- **DEF-402** (CR-037 countries): the country leg cost 2,648–3,297 ms of a ~195 ms query — 15× the
  whole rest of it

Both were fixed the same way: resolve the matching certificate ids **up front as their own query**,
where it *is* a plain semi-join and costs ~0 ms, then OR the id set back in as set membership. The
same shape is required here. The rejected alternatives are recorded in `TagSearchMatch.cs:42-45` —
uncorrelated `IN (SELECT …)` and `OPTION (RECOMPILE, ...)` were both measured and neither helped.

### 5.1 `Search/TagSearchMatch.cs` — add two resolvers and one predicate helper

Extend the existing class rather than adding a new file: DEF-402 set that precedent by putting the
country resolver here, and `MaxInlineIds`, `Union` and `ParameterRebinder` are all reused.

**Add, after `MatchingCountryCertificateIdsAsync` (currently ends line 119):**

```csharp
/// <summary>
/// DEF-413. Certificate ids whose certification-type SUBCATEGORIES match the already-normalized
/// search term. Same semi-join shape, and for the same reason, as the tag and country resolvers
/// above — see the class remarks. Returns an empty list for a blank term.
/// </summary>
public static async Task<IReadOnlyList<int>> MatchingSubcategoryCertificateIdsAsync(
    IQueryable<Certificate> certificates,
    string nq,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrEmpty(nq)) return Array.Empty<int>();

    return await certificates
        .Where(c => c.CertificationTypeSubcategories.Any(
            s => s.NormalizedName != null && s.NormalizedName.Contains(nq)))
        .Select(c => c.Id)
        // Stable order for the same plan-cache reason as the tag and country lists.
        .OrderBy(id => id)
        .Take(MaxInlineIds + 1)
        .ToListAsync(cancellationToken);
}

/// <summary>
/// DEF-413. Certificate ids carrying any subcategory whose EXACT name is in <paramref name="names"/>.
/// This is the filter-checkbox counterpart of the search resolver above: the sidebar posts exact
/// names, not a search term, so this matches on equality rather than Contains.
/// </summary>
public static async Task<IReadOnlyList<int>> SubcategoryNamedCertificateIdsAsync(
    IQueryable<Certificate> certificates,
    List<string> names,
    CancellationToken cancellationToken = default)
{
    if (names == null || names.Count == 0) return Array.Empty<int>();

    return await certificates
        .Where(c => c.CertificationTypeSubcategories.Any(s => EF.Constant(names).Contains(s.Name)))
        .Select(c => c.Id)
        .OrderBy(id => id)
        .Take(MaxInlineIds + 1)
        .ToListAsync(cancellationToken);
}

/// <summary>
/// DEF-413. Applies the Certification Type filter: a certificate matches when its own type name is
/// in <paramref name="typeNames"/>, OR it is in <paramref name="subcategoryMatchedIds"/> (resolved
/// by <see cref="SubcategoryNamedCertificateIdsAsync"/>).
///
/// Shares the id-list guard with <see cref="WhereMatchesSearch"/>: a literal EF.Constant IN list up
/// to MaxInlineIds so the optimizer gets real histogram values, a parameterized Contains above it so
/// we never emit a pathological literal list. Kept in one place because all three tile surfaces
/// carry their own copy of this filter and had drifted before.
/// </summary>
public static IQueryable<Certificate> WhereCertificationTypeMatches(
    this IQueryable<Certificate> query,
    List<string> typeNames,
    IReadOnlyList<int> subcategoryMatchedIds)
{
    // Nothing matched on subcategories → the extra clause cannot change the result set. This is
    // the fast path and keeps the emitted SQL byte-identical to the pre-DEF-413 query.
    if (subcategoryMatchedIds.Count == 0)
        return query.Where(c => c.CertificationType != null
                                && EF.Constant(typeNames).Contains(c.CertificationType.Name));

    if (subcategoryMatchedIds.Count <= MaxInlineIds)
        return query.Where(c => (c.CertificationType != null
                                 && EF.Constant(typeNames).Contains(c.CertificationType.Name))
                                || EF.Constant(subcategoryMatchedIds).Contains(c.Id));

    var ids = subcategoryMatchedIds.ToList();
    return query.Where(c => (c.CertificationType != null
                             && EF.Constant(typeNames).Contains(c.CertificationType.Name))
                            || ids.Contains(c.Id));
}
```

> **Security note — this is the part to read twice.** The resolvers run against the **unscoped**
> `Certificates` set, exactly as the tag and country resolvers already do. That is safe for the same
> documented reason (`CertificateTileExternalApiController.cs:167-170`): role scoping and public
> visibility are applied as **separate AND'd `Where` clauses earlier in the chain**, so OR-ing an
> unscoped id set into a later AND'd clause can only ever *narrow* within what the caller may already
> see. It can never widen it. **This must be re-verified in review, and it is assertion #7 in §6.**

### 5.2 `Repositories/CertificateRepository.cs` — P1 (public), 2 edits

**Edit 1 — the filter, lines 467-468.** Replace:

```csharp
// New filter properties for API
if (filter.CertificationTypeNames != null && filter.CertificationTypeNames.Any())
    query = query.Where(c => c.CertificationType != null && EF.Constant(filter.CertificationTypeNames).Contains(c.CertificationType.Name));
```

with:

```csharp
// New filter properties for API
if (filter.CertificationTypeNames != null && filter.CertificationTypeNames.Any())
{
    // DEF-413: a filter option matches the certificate's TYPE or any of its certification-type
    // SUBCATEGORIES. Resolved as its own semi-join, never as an .Any() inside the OR — see
    // Search/TagSearchMatch.cs (DEF-366, DEF-402).
    var subcatIds = await TagSearchMatch.SubcategoryNamedCertificateIdsAsync(
        _dbSet, filter.CertificationTypeNames);
    query = query.WhereCertificationTypeMatches(filter.CertificationTypeNames, subcatIds);
}
```

`GetAdvancedFilteredCertificatesAsync` is already `async` and already awaits the tag/country
resolvers at lines 367-369, so the added `await` needs no signature change.

**Edit 2 — the search, lines 365-378.** Add the resolver alongside the existing two and fold it into
the union:

```csharp
var tagIds = await TagSearchMatch.MatchingCertificateIdsAsync(_dbSet, nq);
var countryIds = await TagSearchMatch.MatchingCountryCertificateIdsAsync(_dbSet, nq);
// DEF-413: subcategory names are searchable, resolved the same way.
var subcatIds = await TagSearchMatch.MatchingSubcategoryCertificateIdsAsync(_dbSet, nq);
query = query.WhereMatchesSearch(c =>
    /* …unchanged field list… */,
    TagSearchMatch.Union(TagSearchMatch.Union(tagIds, countryIds), subcatIds));
```

`Union` is associative and already sorts, de-duplicates and re-applies the cap, so nesting is correct
and needs no new overload.

### 5.3 `Controllers/Api/CertificateTileExternalApiController.cs` — P2, 2 edits

**Edit 1 — the filter, lines 189-194.** Replace:

```csharp
// Apply certification type filter
if (!string.IsNullOrWhiteSpace(certType))
{
    var types = certType.Split('|', StringSplitOptions.RemoveEmptyEntries);
    query = query.Where(c => types.Contains(c.CertificationType.Name));
}
```

with:

```csharp
// Apply certification type filter
if (!string.IsNullOrWhiteSpace(certType))
{
    // DEF-413: match the certificate's TYPE or any of its certification-type SUBCATEGORIES.
    // The role scoping applied above this point is a separate AND'd Where, so OR-ing an
    // unscoped subcategory id set in here cannot widen what an External or Partner caller sees —
    // the same argument as the tag/country search legs above.
    var types = certType.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
    var subcatIds = await TagSearchMatch.SubcategoryNamedCertificateIdsAsync(_context.Certificates, types);
    query = query.WhereCertificationTypeMatches(types, subcatIds);
}
```

Note `.ToList()` — `EF.Constant` needs a materialized list, and `Split` returns an array.

**Edit 2 — the search, lines 177-185.** Same three-line shape as §5.2 Edit 2, against
`_context.Certificates`.

### 5.4 `Controllers/Api/CertificateTileInternalApiController.cs` — P3, 2 edits

Byte-identical to §5.3. **Filter at lines 154-159; search at lines 169-176.**

### 5.5 Autocomplete — only if §4.3 is approved

One word added in each of two files:

- `CertificateTileInternalApiController.cs:490` — `.Where(ct => ct.IsActive && ct.NormalizedName!.Contains(nq))`
- `CertificateTileExternalApiController.cs:478` — same

### 5.6 Explicitly not needed

- **No migration.** The join table, the FKs and `CertificationTypes.NormalizedName` all exist. All 21
  Level-2 rows have `NormalizedName` populated (verified) — the search leg works without a backfill.
- **No new `.Include()`.** The semi-join returns ids; it loads no navigation. Do not add a seventh
  collection Include to the internal listing query — it already runs six under `.AsSplitQuery()`
  (DEF-393, DEF-166).
- **No front-end change.** The JS already posts `certType=A|B|C`; only the server's interpretation
  changes.
- **No `/filters` endpoint change.** §3.5.

---

## 6. Acceptance tests

Fixture: **`25-8071`** (JW Aluminum · CSF · Client Approved - Paid · publicly visible · subcategories
Recyclable Material, Recycled Content, Waste Diversion from Landfill).

| # | Page | Action | Expected |
|---|---|---|---|
| 1 | P1 anonymous | filter "Certified Sustainability Facts" | `25-8071` present · **103** results (unchanged) |
| 2 | P1 anonymous | filter "Recycled Content" | `25-8071` present · **526** results (was 458) |
| 3 | P1 anonymous | filter "Waste Diversion from Landfill" | `25-8071` present · **151** results (was 46) |
| 4 | P1 anonymous | search `recycled content` | `25-8071` present |
| 5 | P2 as `test.external` | repeat 1-4 within that company's scope | same behaviour, scoped |
| 6 | P3 as `test.analyst` | repeat 1-4 | same behaviour |
| 7 | **P2 as `test.external` vs `test.external2`** | filter "Recycled Content" on both | **neither returns the other's certificates** — the security assertion for §5.1 |
| 8 | P1 anonymous | filter "Certified Environmental Facts" then "Carbon Footprint Reduction" | `23-0753` (DUDE Wipes) returned by both |
| 9 | All three | every other filter option | result counts unchanged vs a pre-change baseline |
| 10 | P1 anonymous | a not-yet-effective / expired cert with subcategories | **still hidden** — DEF-358 visibility unaffected |
| 11 | P3 | search timing, 5,702-row set | within DEF-402's post-fix band; no return to seconds |
| 12 | P3 | autocomplete `zero` | *(only if §4.3 approved)* suggestion now returns results |

Capture a **full baseline of per-option counts before the change** for test 9. Per
`feedback_equal_counts_are_not_the_same_rows`, compare the certificate-number **sets**, not just the
totals — an unchanged count is not proof of an unchanged result.

The numbers above are from `GCCDatabase_Dev`. Re-derive them on the test environment before asserting
there; do not port these values across.


---

## 6a. Live reproduction of the client-reported sequence (2026-09-09)

Shawn, testing: *"I clicked on the 3 checkboxes and it filtered data. When I searched for Recycled
content it gave me a different number and then when I closed one the tags, nothing came back."*

**Reproduced exactly against `/api/certificate-tiles` on localhost:5101** (anonymous, dev data). The
SQL model of the same predicates returns the identical as-is figures, so the to-be column is
trustworthy:

| Step | Action | As-Is | To-Be | Client saw |
|---|---|---|---|---|
| A | 3 checkboxes: CSF + Recycled Content + Waste Diversion | **607** | 609 | "it filtered data" |
| B | A + search `recycled content` | **458** | **526** | "a different number" |
| C | **remove the Recycled Content checkbox**, search still active | **0** | **68** | "nothing came back" |
| D | C without the search term | 149 | 151 | — |
| E | search `recycled content` alone, no checkboxes | 458 | 526 | — |

**Which chip she removed is deducible, and it was Recycled Content.** Removing either of the other
two leaves the result at 458:

| search `recycled content` + | result |
|---|---|
| CSF \| Waste Diversion *(Recycled Content removed)* | **0** |
| Recycled Content \| Waste Diversion *(CSF removed)* | 458 |
| CSF \| Recycled Content *(Waste Diversion removed)* | 458 |

**Mechanism — it is DEF-413, in compound form, not a second bug.** Search and the type filter are
independent `.Where()` calls, so they AND. The measurements prove the search leg resolves to exactly
the set of certificates whose *type* is Recycled Content: `search ∩ {RC,WD} = 458` and
`search ∩ {CSF,RC} = 458` force `search ⊆ RC-typed`, and `|search| = 458 = |RC-typed|` makes them the
same set. Remove the Recycled Content checkbox and the surviving filter (CSF + Waste Diversion)
excludes every member of that set — the intersection is necessarily empty.

The 68 certificates that *should* have been there are the publicly-visible CSF certificates carrying
Recycled Content as a **subcategory** — including the client's own example, `25-8071`. They are
invisible to the search leg (§3.2) and to the filter leg (§3.1) alike.

**No front-end defect is involved.** The chip-removal handler, the query-string builder and the
infinite-scroll offset all behave correctly; the empty page is the honest result of an empty
intersection. Nothing in the JS needs to change.

### Consequence for decision #3 — the search leg is no longer optional

§8 listed "search leg in or out" as an open question worth ~1.5-2 hr. **This reproduction answers it.**
Fixing only the filter predicate would change step C from 0 to 68 *(the filter leg alone recovers
those rows)* — but step B would stay wrong, and any user who types a subcategory name into the search
box on any of the three pages would still get results that silently omit every certificate carrying
that name as a subcategory. Worse, the filter-only fix produces a **new inconsistency**: the checkbox
would find the CSF certificates while typing the same words into the search box would not.

**Recommendation: build both legs together.** Decision #3 should close as "in".


---

## 7. Out of scope → CR-047

Three subcategory names have **no Level-1 twin**, so they are absent from the dropdown and this change
leaves them unreachable by checkbox (though §4.2 makes them findable by *search*):

| Name | Parent | Certs |
|---|---|---|
| Zero Waste to Landfill | Waste Diversion from Landfill | 20 |
| Waste Diversion Event | Waste Diversion from Landfill | 0 |
| Waste System Audit | Waste Diversion from Landfill | 0 |

Surfacing these means adding Level-2 rows to the filter dropdown, which reverses DEF-167. None appears
in the "Filter Sections" requirement. **Raise as CR-047 with client confirmation — do not fold into
DEF-413.**

---

## 8. Open decisions

| # | Decision | Owner | Blocking? |
|---|---|---|---|
| 1 | **DEF-413 or CR-047** for the predicate work | Shawn | No — the code is the same either way |
| 2 | **Confirm the widened checkbox semantics with the client** — "Recycled Content" will return both certs *of* that type and CSF certs carrying it (§4.1) | Client | Recommended before build |
| 3 | ~~Search leg in or out~~ — **ANSWERED by the §6a reproduction: IN.** Filter-only leaves step B wrong and creates a checkbox-vs-search inconsistency | — | Closed |
| 4 | **Autocomplete `IsActive` guard** (§4.3) | Shawn | No |
| 5 | **CR-047** for the three orphan names (§7) | Shawn → client | No |

---

## 9. Effort

| Item | Est. |
|---|---|
| §5.1 resolvers + predicate helper, with the DEF-366/402 comment trail | 1.0 hr |
| §5.2–5.4 six call-site edits (three surfaces × filter + search) | 1.0 hr |
| §5.5 autocomplete guard *(if approved)* | 0.25 hr |
| Baseline capture + §6 tests 1-12 across three surfaces and four accounts | 2.0 hr |
| Register/shipped-log updates | 0.5 hr |
| **Total** | **≈ 4.5-5 hr**, plus a deploy |

~~Filter only, dropping the search leg: ≈ 3-3.5 hr.~~ **Withdrawn** — §6a shows the filter-only
variant leaves the reported symptom half-fixed and introduces a checkbox-vs-search inconsistency.

---

## 10. Evidence

- Code sites read at the line numbers cited, working tree, branch `ci-regression`, 2026-09-09.
- `git log -S "CertificationTypeSubcategories.Any" --all` → 4 commits, all display/projection; no
  filter predicate has ever existed.
- Data from `GCCDatabase_Dev` on ANUKI01, 2026-09-09: link counts, the 18 duplicate L1/L2 name pairs,
  the 3 orphan names, the as-is/to-be count table, and fixture `25-8071`.
- Requirement from `docs/Final Req Docs/archive/Requirements.xlsx`, sheet "Filter Sections".
- QC #169 from `Database QC Log_9.3.26_sw_updates.xlsx`, sheet `GCC DB QC Log2`, row 174 — see the
  DEF-413 addendum in the register for the misattributed-quote correction at DEF-167.

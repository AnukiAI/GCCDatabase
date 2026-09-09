# DEF-409 — Search result ranking: design and edge cases

**Status:** **BUILT 2026-09-05** — Option A, all applicable surfaces. See the defect register for
results. This document is kept as the design record; the count of surfaces here says "nine" and the
true figure is **eleven** (ten changed, one — `FilterService:250` — returns only counts and cannot be
ordered). Raised 2026-09-04 from *"I just searched for 23 and 26
came up. that is wrong."*

Every number below was measured against the TEST database and the live site on 2026-09-04, not
estimated.

---

## 1. What is actually wrong

**The filter is right. The order is wrong.**

| | |
|---|---|
| `search=23` returns | **1,219** rows |
| …that genuinely are `23-xxxx` | **1,075 (88%)** |
| …position of the first one | **67 of 1,219** — page 4 at the UI's 20/page |
| the rows above it | `26-0623`, `26-0523`, `26-0323`, `26-0123` … |

Those really do contain "23" — in the **sequence** half, not the year half. The default sort is
`newest`, so 2026 certificates outrank 2023 ones, and the user reads three pages of apparent
nonsense before the first correct row.

**Reconciliation, so the numbers are trustworthy:** 1,185 of the hits match on the number
(`NormalizedNumber LIKE '%23%'`) and 34 on product name — 1,185 + 34 = 1,219. The prefix band on
the number is 1,075, which is exactly the count of `23-xxxx` found by walking the live result set.
Two independent routes to the same figure.

**Typing the dash does not help.** `search=23-` returns the identical 1,219, because
`SearchNormalizer` strips `-` from **both** sides (`Search/SearchNormalizer.cs:37`). There is
currently no way for a user to say "the year part".

**Not caused by the 2026-09-04 deploy.** That deploy carried DEF-402, which rewrote part of search,
but `git diff HEAD` shows `NormalizedNumber.Contains(nq)` untouched. Long-standing behaviour, newly
visible at 5,924 certificates.

### The normalization fact the whole design rests on

`-` is stripped, so `23-0456` is stored as `230456` and `26-0623` as `260623`.

**A prefix match on the normalized number is therefore exactly a year-prefix match**, for free, with
no parsing of the number format. `LIKE '23%'` returns 1,075 rows — precisely the 2023 certificates.

`NormalizedNumber` is also **already indexed**, and the comment on it reads *"Indexed for
prefix/autocomplete"* (`Data/ApplicationDbContext.cs:132`). The design below is what that index was
put there for.

---

## 2. Options

### Option A — rank, don't filter *(recommended)*

Keep the result set identical. Order it in three bands, then apply the user's chosen sort **within**
each band:

| Band | Rule | `search=23` |
|---|---|---|
| 0 | `NormalizedNumber == nq` (exact) | 0 |
| 1 | `NormalizedNumber` starts with `nq` | 1,075 |
| 2 | everything else — substring in number, or a company/product/tag match | 144 |

`23` opens on 2023 certificates; `26-0623` is still findable, further down. **Nothing stops being
discoverable, so nobody's existing workflow breaks.** That property is why this is the
recommendation.

### Option B — anchor a bare 2-digit query to the year

Treat `23` as `23-%` and drop the rest.

Sharper, and **wrong in a way that is hard to detect**: it silently stops finding things. Concretely,
`6H3-5692` exists (see §3), and someone searching `35` finds it today; under Option B a 2-digit query
becomes a year filter and that row disappears. A search that quietly *stops* returning a real record
is a worse defect than one that orders badly, because nothing on screen indicates it happened.

### Option C — leave it, document it

Defensible only if searching by number is rare. Certificate number is the primary identifier in this
system, so it is not.

**Recommendation: Option A.** It is also the pattern the codebase already uses — the Amazon v1 API
does `.OrderByDescending(pi => pi.isMatchedId) // Put the matched ID first`
(`Controllers/Api/V1/CertificatesController.cs:1177`). This is not a new idea here, just an unapplied
one.

---

## 3. Edge cases

### Query shape

| # | Query | Prefix band | Behaviour under Option A | Risk |
|---|---|---|---|---|
| 1 | empty / whitespace | — | **no banding at all**; order must be byte-identical to today | invariant to test |
| 2 | `2` | 5,873 of 5,906 | near no-op — almost every number starts with 2 | none |
| 3 | `9` | **0** | pure no-op; existing sort governs | none |
| 4 | `23` | 1,075 | the target case | — |
| 5 | `23-` | 1,075 | normalizes to `23`; **incidentally fixes the "dash doesn't help" complaint** | — |
| 6 | `23-0971` | exact | band 0 | — |
| 7 | a duplicated number | exact, several rows | 137 numbers are shared by >1 certificate — tie broken by `Id` | see #16 |
| 8 | `0971` (sequence only) | 0 | stays band 2 — **known limitation, accepted** | documented |
| 9 | `Sika` | 0 | no row is in band 0/1, so all land in band 2 and today's order is preserved exactly | verify |
| 10 | `6H3` | 3 | works — banding never parses the format | — |
| 11 | longer than any number | 0 | 0 results, as today | — |
| 12 | contains `%` or `_` | — | **already safe**: the normalizer strips both, explicitly so they "can never act as a wildcard" (`SearchNormalizer.cs:45`) | none |
| 13 | `Product™` / unicode | — | stripped both sides | none |
| 14 | `6h3` vs `6H3` | — | both sides lowercased | none |

### Data shape

15. **Three certificates are not `YY-NNNN`:** `6H3-5692`, `6H3-6301`, `6H3-7120` (5,921 of 5,924 are
    `YY-NNNN`; 0 are `YY-` anything else). Option A is format-agnostic and handles them. **Option B
    is the option these three break.**
16. **137 numbers are shared by more than one certificate** (CR-038, 175 extra rows). Ranking creates
    ties; `.ThenBy(c => c.Id)` is **already on every branch** of `ApplySorting` and must stay.
17. **`NormalizedNumber` is nullable.** `NULL LIKE '23%'` is `NULL`, so the `CASE` falls to `ELSE` and
    the row lands in band 2. Safe by construction, but assert it.
18. **Rows matched only by tag** (`TagSearchMatch` OR-s ids in) have numbers unrelated to the query →
    band 2. Correct.
19. **Rows matched only by company/product** (34 of the 1,219) → band 2. Correct.

### Ordering and pagination — where this is most likely to break

20. Banding must be applied **before** `Skip`/`Take`, on the ID-only projection. The DEF-250 pattern
    pages IDs first and hydrates second (`CertificateTileInternalApiController.cs:240`).
21. The hydrate step deliberately drops the ORDER BY and **restores the order in memory from
    `pagedIds`** (`:246-248`). Banding rides along for free — but only if it was applied to the ID
    query. Applying it to the hydrate query instead would do nothing at all and look like it worked.
22. **Page 2 must not repeat or skip rows.** This is the classic failure and the reason the `.ThenBy(
    c => c.Id)` tiebreaker exists (the "15 of 35" symptom, DEF-166/234). Test explicitly across a
    band boundary — for `23` that boundary is at position 67, i.e. between pages 3 and 4.
23. Both paging paths must agree: `offset`/`limit` **and** `page`/`pageSize` (DEF-249).
24. **`recently-issued` adds a `.Where()` inside the sort switch** (`:792`) — it is a filter wearing a
    sort's clothing. Any restructuring must not apply it twice or drop it.

### Sort interaction — the one real judgement call

25. **With a search term and an explicit sort**, the band goes first and the user's sort applies
    within each band. Rationale: typing a query is a stronger statement of intent than the ambient
    sort, and their sort still fully governs inside each band. **This is a decision to confirm, not a
    fact** — the alternative (honour the sort strictly, rank only when sort is default) is defensible
    and cheaper to explain.
26. **With no search term, ordering must be identical to today.** Strongest single regression test.
27. Descending sorts do not invert the band. Band is relevance (0,1,2), not a data column.

### Surfaces — nine call sites, five files

28. `NormalizedNumber.Contains(nq)` appears at: internal tile `:138` (list) and `:444`
    (**autocomplete**); external tile `:174` and `:401` (**autocomplete**); `CertificateRepository`
    `:209`, `:230`, `:351`; `CertificateService` `:57`, `:193`, `:489`; `FilterService:250`.
    **Fixing only the internal list leaves the same complaint reproducible on the public and
    external views** — the DEF-103/164/198 pattern of a scoping-style fix applied to one endpoint.
29. **The two autocomplete endpoints deserve it most and cost least.** They are number-only
    (`.Where(c => c.NormalizedNumber.Contains(nq))`), so a suggestion list offering `26-0623` when
    you type `23` is the sharpest version of this bug, and prefix-first is the whole fix.
30. Partner/external scoping is applied **before** ordering, so ranking cannot widen a scoped set.

### Non-functional

31. **`totalCount` does not change.** This is pure ordering: no row enters or leaves any result set,
    so there is **no scoping or security consequence**. (Run the security gate anyway — policy — but
    expect it unchanged.)
32. **The Amazon contract is insulated.** `Controllers/Api/V1/CertificatesController.cs` has its own
    `OrderBy` chain and calls neither `ApplySorting` nor `WhereMatchesSearch`. Verified by grep. The
    partner feed cannot be reordered by this change.
33. **Performance.** The band is a `CASE` in `ORDER BY`, over the already-filtered set — at worst
    ~5,900 rows. It does **not** go anywhere near the search `OR`: DEF-402 is the standing warning
    that a clause added there cost 15x. Measure before/after regardless
    ([[feedback_measure_dont_reason_about_perf]]).
34. The filter itself stays `LIKE '%23%'` — non-sargable, unchanged. This design does not make search
    slower; it also does not make the filter faster.

---

## 4. Implementation shape

`ApplySorting(query, sortBy)` is the single ordering hook — but its `switch` has **33 branches**,
several using custom helpers (`OrderByTextEmptiesLast`, `OrderByNumericNullsLast`, …) that return
`IOrderedQueryable`. A leading sort key **cannot be prepended** to an already-ordered query, so there
are two honest routes:

**A1 — partition (recommended).** Leave all 33 branches untouched. Select the page of IDs from the
banded partitions in order, reusing the existing `ApplySorting` within each:

```
top  = ApplySorting(query.Where(inBand01), sortBy)     // exact + prefix
rest = ApplySorting(query.Where(!inBand01), sortBy)    // everything else
```
then slice the page across the two by count. Costs one extra `COUNT` and at most two thin ID
queries per page. **Zero churn in the sort switch**, which is where the risk actually lives.

**A2 — prepend the key.** Change `ApplySorting` to take `nq`, seed the chain with the band, and turn
all 33 branches into `ThenBy`, adding `ThenBy…` siblings for the four helpers in
`Extensions/CertificateSortExtensions.cs`. One clean SQL statement; materially more code touched,
including the `recently-issued` landmine at #24.

**Take A1 first.** If the extra query ever shows up in a measurement, A2 is the optimisation — and by
then there will be tests.

---

## 5. Test plan — every assertion falsifiable

| # | Assertion | Expected |
|---|---|---|
| T1 | `search=23`, first page | every row is `23-xxxx` |
| T2 | `search=23` total | **still 1,219** — ranking must not filter |
| T3 | `search=23-` | identical result set **and identical order** to `search=23` |
| T4 | no search term, each `sortBy` | order **byte-identical to pre-change** (capture both, diff) |
| T5 | `search=Sika` | order byte-identical to pre-change — proves non-numeric queries are untouched |
| T6 | pages 3→4 at the band boundary (position 67) | no row repeated, none skipped |
| T7 | `offset/limit` vs `page/pageSize` for the same window | identical rows |
| T8 | `search=9` | band empty; order byte-identical to pre-change |
| T9 | a duplicated number (one of the 137) | all its rows adjacent, ordered by `Id` |
| T10 | `search=6H3` | the three `6H3-` certificates first |
| T11 | internal, external, public, and both autocompletes | all four surfaces agree on ranking |
| T12 | external / partner accounts | `totalCount` unchanged from today, per role |
| T13 | Amazon `/api/v1/*` | contract diff still `CONTRACT UNCHANGED` |
| T14 | search latency, before vs after | no regression; compare against DEF-402's numbers |

T4, T5 and T8 are the ones that catch an over-broad change — they assert what must **not** move.

---

## 6. Open questions for Shawn

1. **Does the band outrank an explicitly chosen sort?** (#25) I have assumed yes.
2. **Should a company/product *prefix* also rank above a mid-word match** — should searching `Sika`
   put companies starting with "Sika" above products merely containing it? Same mechanism, wider
   scope. Not included above.
3. **Sequence-only search** (`0971`, #8) stays unranked. Acceptable, or should the part after the
   year rank too?
4. **All nine surfaces, or the four user-facing ones first?** The autocompletes are the cheapest win.

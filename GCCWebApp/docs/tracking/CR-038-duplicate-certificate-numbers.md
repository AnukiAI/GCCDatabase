# CR-038 — Allow duplicate certificate numbers for migrated history, keep uniqueness for new certificates

**Raised:** 2026-08-14 · **Origin:** client decision relayed by Shawn · **Status:** PROPOSED — not built
**Blocks:** the go-live load of 312 rows / 137 certificate numbers

---

## The requirement, as stated

> *"We need to load these duplicates in the system, because they have the same cert number, but
> they are different certificates for one of their clients. This is an exception to the rule.
> Currently functionality for any new certificates created would not be allowed to have duplicate
> certificate numbers."*

So: **duplicates are legal for existing/migrated records, and illegal for anything created from
now on.** This is a grandfather rule, not a relaxation.

**The client's premise is correct and now independently verified.** Their live system holds
**6,000 certificate records across 5,822 distinct numbers — 140 numbers carry more than one
record** (measured from the credentialed portal pull). Our schema was designed with a unique
certificate number; theirs was not. This is a design gap on our side, not bad data on theirs.

---

## The dataset — measured 2026-08-14

**312 rows across 137 certificate numbers.** Group sizes: 104 numbers × 2 rows, 28 × 3, 5 × 4.

**What differs inside a number:**

| Numbers | Differs by |
|---|---|
| 92 | **Product name only** |
| 20 | Company **and** product name |
| 12 | nothing on the compared fields |
| 7 | expiration date only |
| 3 | product name + expiration date |
| 3 | broader (incl. `21-0179` / `21-0180`, the two genuine data errors) |

**By company** — this is *not* one client: Sika Canada 80 numbers / 159 rows · Regupol America
37 / 93 · Sika Corporation 21 / 22 · MAPEI 10 / 20 · then a tail of 7 more. (Numbers exceed 137
because a number can appear under two companies — the Sika Canada / Sika Corporation cross-listing.)

### ⚠️ The decisive fact — and it corrects plan §11.2

Plan §11.2 judged the 121 Expired-1 numbers "restatements of one product… keep-most-complete loses
nothing real", and this CR's first draft repeated that. **That judgement was made from spreadsheet
fields only. The documents say otherwise.**

Cross-checking the 137 against the client's own portal pull:

- **136 of 137 have a PDF, and all 136 carry more than one document record.**
- **128 have genuinely DIFFERENT documents** (distinct sha256). Only 8 are byte-identical.
- Spread: Sika Canada 63 · Regupol 37 · Sika Corporation 15 · MAPEI 10 · Deceuninck 2 ·
  Greenfield 1.

**So collapsing would destroy 128 distinct signed certificates**, not tidy up duplicate rows. The
client's "they are different certificates" is literally true, and it is true well beyond Regupol.
Any option that keeps one row per number must still preserve those 128 documents.

---

## What blocks it today

`ApplicationDbContext.cs:68` — `entity.HasIndex(e => e.Number).IsUnique();`

That single line is the hard blocker. Everything else below is consequence.

---

## Proposed design — REVISED 2026-08-14 after review

> **The first draft of this section was wrong in two ways.** Both are recorded rather than quietly
> replaced, because the first version would have shipped a requirement violation.
>
> **Error 1 — "the application guard already exists" was half true.**
> `ValidationService.ValidateCertificateNumberUniquenessAsync` exists, but it is called **only from
> `CertificatesApiController`** (:195, :260). It is **not wired into the MVC Create/Edit path** that
> analysts actually use, and `CertificateService` never calls it. The only other explicit check
> lives in `CertificateController.Import` (:2778), a JSON endpoint.
> **So on the primary certificate-creation path, uniqueness today is enforced by the database index
> and nothing else.** Dropping that index as originally drafted would have left the main path with
> **zero** enforcement — the exact opposite of what the client asked for.
>
> **Error 2 — dropping uniqueness was the wrong instrument.** A **filtered unique index** keeps the
> database guarantee for new certificates while exempting the grandfathered rows.

### 1. Add a discriminator scoped to the exception

`Certificate.AllowsDuplicateNumber` (bit, NOT NULL, default 0). Set to **1 only on the 312 rows
that participate in a duplicated number** — not on the whole migration. The client called this "an
exception to the rule", so the exemption should mark exactly the exception and nothing else.

### 2. Filtered unique index — the database keeps guaranteeing the rule

```sql
DROP INDEX IX_Certificates_Number ON dbo.Certificates;
CREATE UNIQUE INDEX IX_Certificates_Number ON dbo.Certificates(Number)
    WHERE AllowsDuplicateNumber = 0;
```

**Proven on SQL Server 2026-08-14** against a scratch database:

| Case | Result |
|---|---|
| 3 grandfathered rows sharing `26-0182` | **inserted** ✔ the migration loads |
| two NEW certificates with the same number | **blocked by the index** ✔ requirement enforced *in the database* |
| a NEW certificate colliding with a *grandfathered* number | **allowed** — the index cannot see exempt rows |

So the index covers new-vs-new (the common case) at the storage layer, and only **new-vs-migrated**
needs application help.

### 3. Close the application gap — required, not optional

- Add an explicit uniqueness check to the **MVC Create/Edit path**, which has none today. This is a
  latent gap **independent of this CR** and should arguably be fixed regardless.
- The check must consider **all** certificates including exempt ones, so a new certificate cannot
  take a grandfathered number.
- Skip the check when an existing certificate is saved **without changing its number** — otherwise
  every one of the 137 duplicated certificates fails validation the first time an analyst opens and
  saves it, which would make the migration self-defeating.

---

## Second review pass — 2026-08-14 — what it changed

Four things the first draft missed. Two remove risk; two add a constraint.

**1. Amazon exposure is ZERO — the decision I flagged as needing partner sign-off is moot.**
Measured across all 312 rows: **none has a Partner and none carries an ASIN.** Amazon (and every
other partner) cannot see any of these certificates, because the v1 API filters certificates by
partner before any by-number lookup happens. Blast-radius items 1–3 below are therefore **not
reachable for this dataset**. They remain a latent hazard only if a duplicated number is ever given
a partner in future, so the deterministic-pick helper is still worth building — but it is no longer
a go-live blocker or a contract conversation.

**2. Only 8 of 137 are currently public** (`Client Approved - Paid`); 129 are `Expired`. The
public-site blast radius is 8 certificates, not 137.

**3. The filtered index MUST keep the name `IX_Certificates_Number`.**
`CertificateRepository.IsUniqueNumberViolation` (:939) matches the SQL error on **the index name
string**:

```csharp
return (sqlEx.Number == 2601 || sqlEx.Number == 2627)
    && sqlEx.Message.IndexOf("IX_Certificates_Number", StringComparison.OrdinalIgnoreCase) >= 0;
```

That guard powers **DEF-193**'s retry, which fixes two analysts opening the Create form
back-to-back and being handed the same number. Rename the index and the catch stops matching —
DEF-193 silently regresses with no error anywhere. Keeping the name preserves it, because the
filtered index still throws on new-vs-new collisions.

**4. Auto-numbering is safe.** `GenerateNextCertificateNumberAsync` takes `MAX(sequence)` for the
year prefix across **all** certificates, so a generated number cannot land on a grandfathered one.
No change needed.

---

## Blast radius — every place that assumes one certificate per number

Measured, not estimated. `FirstOrDefaultAsync(c => c.Number == …)` silently returns **one arbitrary
row of N** once duplicates exist — the plan decides which, so it is not even stable between runs.

| # | Site | Impact | Needs |
|---|---|---|---|
| **1** | `Api/v1/CertificatesController.cs:432` — GET certificate by number | **Amazon partner API** returns one arbitrary certificate of N | **decision** |
| **2** | `Api/v1/CertificatesController.cs:1258` — download by number-or-id | **Amazon** gets one arbitrary PDF of N | **decision** |
| **3** | `Api/v1/CertificatesController.cs:1536` — by-number lookup | same | **decision** |
| 4 | `Services/CertificateService.cs:832`, `:1088` | internal by-number lookups return one of N | rule |
| 5 | `Controllers/CertificateController.cs:3289` — PDF upload by number | could attach a PDF to the wrong sibling | rule |
| 6 | `Services/Import/CertificateImportService.cs:1572` — update-by-cert-number | an import row matching a duplicated number updates an **arbitrary** sibling | rule — or refuse to update duplicated numbers and require Id |
| 7 | `Scripts/data-ops/2026-06-24-recover-missing-cert-pdfs.ps1` | globs `{Number}_*.pdf`; several files now share a prefix | tolerate |
| 8 | PDF link step (`PdfLinkCommand`) | joins manifest → `Certificate.Number` | **improves** — see below |

### The Amazon API is the decision that cannot be made unilaterally

Items 1–3 are partner-facing and the contract is treated as immutable
([[feedback_amazon_api_immutable]], Gate 4 has a frozen-baseline diff). Three options:

| | Option | Contract impact |
|---|---|---|
| **a** | **Deterministic pick** — newest effective date, tie-break on Id | Response *shape* unchanged; the value returned becomes stable instead of arbitrary. **Recommended** — arguably a bug fix, since today's behaviour is plan-dependent |
| **b** | **Return all matches** | Shape change — array where an object was. Requires partner sign-off |
| **c** | Leave as-is | Amazon silently receives a random sibling. **Not acceptable** |

**Recommend (a) now, and tell Amazon about (b) as a future option** — the duplicates are one
client's certificates, and Amazon may legitimately need all of them eventually.

### One thing that gets *better*

The PDF link step currently attaches the lowest-`record_id` document per number and reports 176
alternates it cannot place. The portal manifest carries a **distinct document per record**, so once
duplicates exist as separate certificates those alternates map **1:1** instead of being discarded —
provided the linker is keyed on `record_id`, not just number.

---

## Work items

| # | Item | Notes |
|---|---|---|
| 1 | EF migration: add `AllowsDuplicateNumber` + swap to the **filtered** unique index | + SQL for test/prod (Gate 7) |
| 2 | **Wire a uniqueness check into the MVC Create/Edit path** — it has none today | the gap found in review; needed even without this CR |
| 2b | Carve-out: skip the check when an existing cert's number is unchanged | otherwise the 137 become uneditable |
| 3 | Deterministic selection for by-number reads (items 1–5) | shared helper, not N ad-hoc `OrderBy`s |
| 4 | Importer: decide update-by-number behaviour for duplicated numbers | recommend: refuse, require Id |
| 5 | Transform: `--dup-policy` gains an `emit-all` mode that is now *valid* | it exists as `all`, currently documented as "never run" |
| 6 | Re-run dev rehearsal with duplicates loaded, reconcile | proves 5,390 → ~5,702 |
| 7 | Gate 4 contract diff **must** be re-run | behaviour of items 1–3 changes |

**Not in scope:** `CertificateDocument` (multi-document per certificate). It exists but has no UI
surface anywhere; loading duplicates as separate certificates is the cheaper answer to the same
problem.

---

## Risks

- **Silent wrong-record reads** are the main hazard, and they fail quietly. Every by-number read
  must become deterministic in the same change — a half-done version is worse than today, because
  duplicates would exist while the reads still assume they cannot.
- **Uniqueness is NOT reduced to an application-only rule** — that was the first draft's flaw. The
  filtered index keeps a database guarantee for every non-exempt certificate. The single case the
  database cannot cover is a **new certificate taking a grandfathered number**, which the
  application check must catch.
- **The exemption must be set correctly by the loader.** If `AllowsDuplicateNumber` were set on
  every migrated row instead of only the 312, the guarantee would silently disappear for 5,078
  certificates. The loader should set it from the duplicate set explicitly and the count should be
  asserted after the load (expect **312**).
- Test/prod need the migration applied explicitly (Gate 7).

---

## The alternative that does NOT break the current design

Asked directly: what can be done without dropping the unique index?

### Option A — one certificate per number, siblings' PDFs into `CertificateDocuments`

`dbo.CertificateDocuments` already exists with exactly the right shape (`CertificateId`,
`FileName`, `FilePath`, `FileHash`, `DocumentType`, `Version`, `IsPublic`) and is **more built than
plan §10.3 implied**: `FileUploadService.UploadCertificateDocumentAsync` is a working write path,
and DEF-337's delete-audit already captures the table. What is missing is **read/list surface** —
no controller or view exposes it.

| | |
|---|---|
| **Preserves** | all **128 distinct documents** — the thing collapsing would destroy |
| **Keeps intact** | the UNIQUE index, all 8 by-number read paths, and the **Amazon contract untouched** (still one certificate per number) |
| **Build** | a documents list on the certificate detail page + loader change. Write path and audit already exist |
| **Still loses** | the differing **product name** on 92 numbers — one row, one `ProductId` |
| **Cannot represent at all** | the **22 numbers whose Company differs** (Sika Canada vs Sika Corporation). A certificate belongs to one company; picking one is *wrong data*, not merely reduced data |

### Honest comparison

| | Option A (no design break) | Option B (this CR) |
|---|---|---|
| 128 distinct documents | ✅ preserved | ✅ preserved |
| Differing product names (92) | ❌ flattened | ✅ |
| Differing companies (22) | ❌ **mis-attributed** | ✅ |
| UNIQUE index / DB guarantee | ✅ kept | ❌ becomes app-layer |
| Amazon API by-number reads | ✅ unambiguous | ⚠️ needs deterministic pick |
| Build size | small–medium | medium–large |

**Recommendation.** Option A is the only thing that fits "without breaking the current design", and
it covers **115 of 137** numbers respectably. But it **cannot** represent the 22 cross-company
numbers at all, and it flattens 92 product distinctions the client can see in their own system.

If the client's requirement is taken literally — *these are different certificates, load them* —
**Option B is the faithful model and Option A is a workaround that will read as wrong on 22
records.** A defensible middle path is **A for go-live** (nothing is destroyed, nothing breaks) with
**B as a scheduled CR**, provided the client accepts the 22 cross-company records being held back
rather than mis-attributed.

---

## Effect on the go-live load

With this CR built, the 312 quarantined rows load as **312 certificates across 137 numbers**,
taking the load from 5,390 to roughly **5,702**. The three populations identified in plan §11.2 no
longer need different answers — they all simply load — **except the 2 genuine data errors**
(`21-0179`, `21-0180`: unrelated companies sharing a number), which still need the client to say
which is correct.

**Until this is built, those 137 numbers load nothing.**

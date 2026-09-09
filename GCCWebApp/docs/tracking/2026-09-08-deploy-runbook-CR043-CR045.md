# Deploy runbook — 2026-09-08 → gccwebapp.com (test) — CR-043, CR-045, DEF-412

**Bundle:** `deployment-drops/GCCWebApp-deploy-20260908-1607.zip` — 341 entries, 61.1 MB
**In-zip `GCCWebApp.dll` md5:** `457bd720da884279`
**Verifier:** `python Scripts/deploy/verify-bundle.py deployment-drops/GCCWebApp-deploy-20260908-1607.zip`
→ **0 failures**

> ## ✅ STATUS: DEPLOYED AND VERIFIED — 2026-09-08
>
> Database applied ~16:20 EDT; code uploaded and app pool recycled by Shawn ~17:0x EDT; verified
> live immediately after. **17 of 18 live checks pass and the one that does not is a fixture
> limitation, not a defect** — see "Live verification results" below, which is the record of what
> was actually observed rather than what was expected.
>
> The steps below are kept as written for the next deploy. **One correction:** step 4 said the
> test-environment gate takes HTTP Basic. **It does not** — `TestEnvironmentGate.Mode` is `Cookie`,
> so a script that sends `httpCredentials` and follows redirects gets HTTP 200 and a ~2.5 KB gate
> page for every URL and grades that page as the application. That is DEF-341, and it produces a
> false PASS on exactly the anonymous checks this deploy depended on. The verify script now passes
> the cookie gate, including on its anonymous session.

---

## What is in this drop

Three pieces of work, all previously built and gated but never uploaded:

| Ref | What the client sees |
|-----|----------------------|
| **CR-043** | Confidential certificates now reach the product-identifier export and the Amazon partner feed. A company signed in sees its own Confidential certificates. Confidential counts as Active in internal dashboard counts and their exports. **The public database is unchanged.** |
| **CR-045** | `Certification Results` publishes verbatim — whatever is typed is what ships, with no percentage conversion. |
| **DEF-412** | A `Cert Subcategories` column on all four server-side certificate exports. Built 2026-09-08 morning; its own bundle (`…-DEF412.zip`) was never uploaded and **this bundle supersedes it.** |

`GCCWebApp-deploy-20260908-DEF412.zip` and `…-0648.zip` are **superseded — do not upload either.**
This bundle contains everything they did plus both CRs.

---

## Step 1 — Back up the server's `wwwroot/uploads/` · **HUMAN, DO THIS FIRST**

Zip the server's `wwwroot/uploads/` before extracting anything.

This bundle contains **zero** upload entries (verified: `wwwroot/uploads` 0, `App_Data/uploads` 0), so
extraction will not touch the PDF store. **Take the backup anyway** — both the 2026-04-23 upload wipe
and the 2026-06-15 all-PDFs-missing incident started at exactly this step, and neither was expected
to touch uploads either.

A database backup is *not* required at this point: the DB work is finished, and the pre-change state
is already snapshotted in two tables (see Step 5).

## Step 2 — Upload and extract · **HUMAN**

Upload `GCCWebApp-deploy-20260908-1607.zip` to the Plesk site root and extract, overwriting.

`appsettings.Secrets.json` is a **server-only overlay and is deliberately not in the bundle**.
Confirm it survived the extract — if it did not, outbound email silently falls back to the mock
service and nothing errors.

## Step 3 — Recycle the IIS app pool · **HUMAN**

The schema is already current, so nothing has to be run between extract and recycle.

## Step 4 — Verify live · **not optional**

```
python Scripts/data-ops/2026-09-08-CR043-CR045-verify.py https://gccwebapp.com
```

The script takes a base URL and asserts eleven things with controls attached. **Before running it,
change the `CONFIDENTIAL` list to the 13 numbers below** — it currently carries the dev list, which
happens to be identical, but confirm rather than assume:

```
24-5426  24-5427  24-5428  24-5712  24-5713  24-5714  24-5773
25-0479  25-0480  25-0481  25-0482  26-0473  26-0474
```

It needs an admin sign-in; `admin.uat` / `TestPass123!` works (**not** `admin` — its password was
changed on 2026-08-19 and only Shawn knows it). Partner/company accounts are `test.amazon`,
`test.novi` and `test.external` on `GccTest2026!`.

The test-environment gate is a **COOKIE** gate, not Basic auth: GET `/__gate?returnUrl=%2F`, take the
`formToken`, POST it with the password. The script does this for every session **including the
anonymous one** — without it, every URL answers 200 with the gate page and the anonymous 404
assertions pass for the wrong reason (DEF-341).

Expected, and what each one is actually proving:

| Check | Expected | Why it is there |
|---|---|---|
| Anonymous → each of the 13 | **404** | the embargo survived |
| Anonymous → `23-0753` | **200** | control: the public surface is alive, so those 404s mean something |
| PI export contains the 13 | **13/13**, 420 identifier rows | the fix landed |
| PI export excludes `23-0758` | **absent** | it is Expired, not Confidential — different cause |
| Amazon partner sees the 13 | **13/13** | badging works |
| **Novi** sees any of the 13 | **0/13, on a non-empty response** | isolation held. A zero on an EMPTY response proves nothing — and on this dataset Novi has 0 tagged certs, so it is empty. Reported as INCONCLUSIVE |
| **Another company** (Sika) sees any of the 13 | **0 of 13, out of 23 visible** | the control that actually carries the isolation assertion here |
| `23-0758` Certification Results | published as **`23%`** | verbatim |

Also run the standing post-deploy checks:

- `/appsettings.Production.json` must return **404**, not 200 (the handler is active).
- DEF-412: sign in and download `/Export/DownloadReport?type=active-certificates&format=csv` — the
  header must end `…,Country,Cert Subcategories`. **Note this report's row set changes with this
  deploy**: CR-043 Q2 moved it from `PublicStatuses` to `DashboardStatuses`, so in-window
  Confidential certificates now appear in it. That is intended, and it is the one place the client
  may notice a count move without having asked for it.

## Step 5 — Rollback, if it comes to that

**Code:** re-upload the previously live bundle. **Ask what that would undo** — it would remove
DEF-412 and both CRs, and nothing else; the live `wwwroot` already matches the working tree exactly
(9/9 assets identical, checked 2026-09-08), so this deploy changes the DLL and nothing else.

**Schema:** `dotnet ef database update 20260901164220_CR037_MultiCountryCertificates` restores the
old computed column. Safe — it changed no data (proven below).

**Data:** the pre-change state is on the server in two tables:

| Table | Rows | Holds |
|---|---|---|
| `CertProductIdStatus_bak_CR043_20260908` | 19,047 | every product-identifier `Status` + `UpdatedAt` before the backfill |
| `CertResults_bak_CR045_20260908` | 6,012 | every `CertificationResults` + its sort value before the migration |

Drop them once the deploy is confirmed good.

---

## What was checked before any of this ran

### Bundle

| Check | Result |
|---|---|
| Verifier | **0 failures** (now includes CR-043 and CR-045 markers, added this session) |
| `ASPNETCORE_ENVIRONMENT` in bundled `web.config` | **Production** |
| DataProtection keys / dev config / secrets overlay | **0 / 0 / 0** |
| `wwwroot/uploads` / `App_Data/uploads` | **0 / 0** |
| `EXEC(CONCAT(…))` in the schema script | **0** — the DEF-386 repair fired on exactly 1 statement, which is CR-045's computed column |
| Schema script carries | exactly 1 migration: `20260908175939_CR045_…` |

**Marker check on the in-zip DLL itself** (size and entry count prove nothing — 339 entries / 61.1 MB
once matched between two bundles while one was missing a whole fix):

| Marker | | |
|---|---|---|
| `BadgeableStatuses` | **PRESENT** | CR-043 |
| `IsBadgeable` | **PRESENT** | CR-043 |
| `PublicStatuses` | **PRESENT** | CR-043 — it must *survive*; it still guards the public database |
| `FormatCertificationResults` | **ABSENT** | CR-045, asserted by absence |
| `DisplayFormatters` | **ABSENT** | CR-045, asserted by absence |
| `Cert Subcategories` | **PRESENT** | DEF-412 |

### Database, applied and then proven against the snapshots

| | |
|---|---|
| Migration recorded | `20260908175939_CR045_CertificationResultsVerbatimSort` |
| Computed column | no `* 100`, `%`-strip kept, `is_persisted = 1` |
| Index | `IX_Certificates_CertificationResultsNumeric` re-created |
| **Sort values that changed** | **0 of 6,012** |
| **`CertificationResults` text that changed** | **0 of 6,012** |
| CR-043 backfill | **420 rows, every one on a Confidential certificate**, nothing else touched |
| CR-171 idempotent re-run | 0 + 0 rows inserted, as expected |
| Certificate count | 6,012 before and after |

The "CR-045 changes nothing" claim is **measured against the before/after snapshot**, not argued from
the fact that 0 bare numerics exist. Both are true; only one is evidence.

---

## What the gap means while it lasts

Between now and the upload, the schema is new and the code is old.

- **CR-045 — no effect.** The old code still applies the ×100 rule at display time, but there are
  **0 bare numeric values** in the database for it to act on, so display and sort still agree.
- **CR-043 — cosmetic only.** 420 product identifiers now read `Active` on the Product Identifiers
  grid, while the export and the Amazon feed still exclude their certificates (old code). Nothing is
  published that should not be.
- **The one thing to avoid:** if an analyst edits a Confidential certificate's product identifiers
  before the code lands, the old `DeriveStatusFromCert` will flip those rows back to `Inactive`.
  Harmless and re-runnable — `Scripts/database/post-deploy/CR-043-productid-status-backfill.sql` is
  idempotent — but it is the reason not to leave this sitting for days.

---

## Tell the client

`docs/tracking/2026-09-08-client-update-qc-open-items.md` is written and covers all seven open QC
items. Two things in it must be said **before** they re-test, or they will read as failures:

1. **`23-0758` is Expired, not Confidential.** It stays out of the export. This is the second time it
   has been reported missing.
2. **Amazon begins receiving certificates it has never seen** — 19 Confidential certificates are
   Amazon-tagged on the target. The response *shape* is unchanged, so this is a heads-up rather than
   a contract change.


---

# Live verification results — 2026-09-08, after the upload

## CR-043 / CR-045 · `Scripts/data-ops/2026-09-08-CR043-CR045-verify.py https://gccwebapp.com`

| Check | Result |
|---|---|
| A1 anonymous → all 13 Confidential certs | **404** — embargo intact |
| A2 control: anonymous → public cert `23-0753` | **200** — the public surface is alive, so those 404s mean something |
| B1 PI export contains the 13 | **13/13** |
| B2 identifier rows | **420 across 13 certificates**, in a file of 18,935 rows |
| B3 labelled correctly | `Client Approved - Confidential` |
| B4 control: expired `23-0758` | **absent**, as it should be |
| C1 Amazon partner sees the 13 | **13/13** (totalCount 223) |
| **C2 co-partner control (Novi)** | **INCONCLUSIVE — reported as a failure on purpose.** See below. |
| C3 control: another company (Sika) | **23 certificates visible, 0 of the 13** |
| D1 internal tile API | `certificationResults = "23%"` — verbatim |
| D2 detail view | shows `23%` |

### Why C2 is recorded as a failure rather than a pass

`test.novi` returned **0 certificates in total**. The original assertion — "Novi sees none of the
13" — was therefore true, and worthless: a control that returns nothing cannot distinguish
*correctly denied* from *completely broken*. Checked in the database rather than assumed:

| Partner | Tagged certificates |
|---|---:|
| Amazon | 295 |
| **Novi** | **0** |
| Other | 0 |
| Wayfair | 0 |

**Novi has no tagged certificates on this dataset at all**, so the Amazon/Novi pair documented in
`.claude/CLAUDE.md` cannot prove partner isolation *here*. It works on dev, where the CI seed gives
Novi its own certificate. This is a fixture gap, not a product defect, and the script now says so
instead of scoring it green.

**C3 is the control that does the job**: `test.external` (Sika Corporation) sees **23** certificates
and **none of the 13**. That figure reconciles exactly — Sika holds 574 certificates, of which 551
are `Expired` and 23 are `Client Approved - Paid` and in force. Expected 23, observed 23.

## Gate 11 — standing post-deploy checks

| Check | Result |
|---|---|
| `/appsettings.Production.json` | **404** — the handler removal survived the extract |
| `/appsettings.json` | **404** |
| DEF-412 export header | ends `…,Country,Cert Subcategories` |
| DEF-412 column populated | **169 of 1,061 rows** carry subcategories |
| CR-043 Q2 effect on the same report | **42 Confidential rows** now included |

### The row-count move, reconciled rather than waved through

The `active-certificates` report returned **1,061** rows where the DEF-412 runbook expected ~1,024.
Queried against the database:

* Paid + Republished, in force = **1,019** ← what the report would have returned before CR-043
* Confidential, in force = **42** ← added by CR-043 Q2
* **1,019 + 42 = 1,061**, which is exactly what the report returned.

Nothing unexplained. **This is the one number the client may notice moving without having asked**,
so it is worth saying to them before they find it.

## Still to do

* **Drop the two rollback tables once you are satisfied** — `CertProductIdStatus_bak_CR043_20260908`
  (19,047 rows) and `CertResults_bak_CR045_20260908` (6,012 rows). Left in place deliberately for now.
* **Gate 12 (outbound email) not re-run.** Nothing in this deploy touches mail, and only `2fatest`
  can receive it.
* Close `docs/qa/gap-report.md` **G12**.

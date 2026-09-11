# Deploy runbook — 2026-09-10 → gccwebapp.com (test) — CR-048, CR-049 (+ DEF-419, DEF-420, DEF-342)

**Bundle:** `deployment-drops/GCCWebApp-deploy-20260910-2128.zip` — 341 entries, 61.2 MB
**In-zip `GCCWebApp.dll` md5:** `cf8c7eeafd990c5d`
**Verifiers:** `python Scripts/deploy/verify-bundle.py …` → **0 failures** ·
`python Scripts/deploy/marker-check-20260910.py …` → **0 failures**

> ## STATUS: BUILT AND VERIFIED — **NOT UPLOADED**
>
> The upload is a human step through Plesk. Everything below the line marked **HUMAN** is yours.
> Nothing has been pushed to gccwebapp.com by this session.

---

## ⚠️ This bundle carries more than the two CRs that were asked for

A Plesk bundle is a **full application overwrite**, so it ships the entire working tree — not a
selected subset. Shawn confirmed shipping all of it (2026-09-10). What is actually in it:

| Ref | Stream | What the client sees | State |
|-----|--------|----------------------|-------|
| **CR-048** | this session | Edit opens in a new tab, so the results list survives | Built + verified today |
| **CR-049** | this session | Logged-in users open in List view; an explicit Tile choice is remembered | Built + verified today |
| **DEF-419** | parallel terminal | CSV import no longer drops a row / blanks a Partner when a cell contains a line break | **HIGH** — built, tested green on localhost, never deployed |
| **DEF-420** | parallel terminal | CSV **export** escapes quotes, so an exported file re-imports intact | Built — see the register note below |
| **DEF-342** | earlier stream | Product Identifiers template carries the final separator copy | Fixed 2026-08-07, never deployed; client has re-opened this **three times** |

**Two register-hygiene notes, raised not silently fixed** — both belong to the parallel stream and
were left for its owner rather than edited underneath it:

1. **DEF-420's register status still reads `RAISED — not fixed. No code changed.`** That is stale:
   four files carry `DEF-420` fixes (`GccCsv.escapeField`) and they are in this bundle. The status
   line needs updating or the shipped log will understate what was delivered — the exact failure
   Gate 11 exists to prevent.
2. **`Views/Shared/_Layout.cshtml` carries no DEF/CR reference in its diff.** It is not an orphan —
   the change is the `<script src="~/js/csv-parse.js">` include that DEF-419/420 depend on — but a
   reader auditing the diff cannot tell that from the diff alone.

---

## Do you need the database step? **No.**

**There are no pending migrations.** The working tree adds none, and the newest migration in the
generated script (`20260908175939_CR045_CertificationResultsVerbatimSort`) is the one already applied
to test on 2026-09-08. Every change in this bundle is code and static assets.

`deploy.ps1` still generates `migrations/apply-migrations.sql` (full history, 38 migrations) because
it always does. **Do not run it.** The verifier warns about full-history scripts for good reason:
SQL Server parses every guard block before the `IF NOT EXISTS` can skip it, so a block referencing a
since-dropped column aborts the whole run (DEF-386). Running a script you do not need is pure
downside here.

> **Remember: TEST no longer auto-migrates.** `AutoMigrateOnStartup` is `false` (verified present and
> false in this bundle). That is the correct setting and it is why the absence of pending migrations
> matters — nothing will be applied for you, and nothing needs to be.

---

## Step 0 — Back up the server's `wwwroot/uploads/` · **HUMAN, DO THIS FIRST**

Zip the server's `wwwroot/uploads/` before extracting anything. Publish has overwritten real
user-uploaded PDFs before (prod upload wipe, 2026-04-23). The pubxml exclusion is in place; the
backup is the seatbelt, not the belt.

## Step 1 — Upload and extract · **HUMAN**

Upload `GCCWebApp-deploy-20260910-2128.zip` via the Plesk file manager and extract over the existing
files.

## Step 2 — Recycle the IIS app pool · **HUMAN**

No database step comes first this time (see above) — recycle straight after the extract.

## Step 3 — Verify the handler is live · **HUMAN, 30 seconds, do not skip**

Request `https://gccwebapp.com/appsettings.Production.json` in a browser.

- **404 or 403 → correct.**
- **200 → STOP.** The ASP.NET Core handler is inactive and IIS is serving raw config — DB password,
  JWT key and gate password in plaintext. That is DEF-363, it has happened on this exact server, and
  it is the single highest-severity thing that can go wrong at this step.

---

## Step 4 — Post-deploy verification

Run against the live site once the pool is back:

```
BASE_URL=https://gccwebapp.com SMOKE_ALLOW_REMOTE=1 node Scripts/smoke-prod/verify-cr048-cr049.js
```

The harness is localhost-guarded by default; the two environment variables above are what let it
point at test. It signs in, so it also needs the test-env cookie gate — note that
`TestEnvironmentGate.Mode` is **Cookie**, not HTTP Basic (DEF-341). A script that sends
`httpCredentials` gets HTTP 200 and a ~2.5 KB gate page for every URL and grades *that* as the
application, which produces a false PASS on exactly the checks you care about.

**What to confirm by eye, in under two minutes:**

| Check | Expected |
|---|---|
| Sign in as an analyst | Lands on the **List** view, not tiles |
| Click Tile, reload | Still Tile — the choice was remembered |
| Click List, reload | Still List |
| Click Edit on any row | Opens a **new tab**; the results list behind it is untouched |
| The new tab's URL | Carries `?cid=…` — this is the one that matters (see below) |
| Sign in as an external customer | Also opens in List; **no** Edit button anywhere |
| Public database, signed out | Unchanged — still tiles, no toggle |

**Why the `cid` check is the one that matters.** Certificate numbers are **not unique** — 137 numbers
span 312 rows. `cid` is what pins which of several same-numbered certificates is being edited
(DEF-369). If it were dropped, an analyst would silently edit the wrong certificate, which is far
worse than the inconvenience CR-048 removes. Verified locally
(`/Certificate/Edit/26-0497?cid=5796`); confirm it survived the deploy.

**Also worth one import and one export** (DEF-419/420 ride along in this bundle): import a CSV whose
description contains an Alt+Enter line break, and export a page whose data contains a double-quote,
then re-import it. Both are the parallel stream's fixes; neither has ever run on test.

---

## Step 5 — Close out · `Scripts/deploy/close-out.js`

```
node Scripts/deploy/close-out.js --date=<upload date> --ids=CR-048,CR-049,DEF-419,DEF-420,DEF-342 \
  --bundle=20260910-2128            # dry run first; add --apply to write
```

Then re-run Gate 9 (`node Scripts/smoke-prod/_static/run-all.js`) to confirm independently that the
roll-up moved. Close-out is the only gate that runs **after** the upload, and it exists because
"Pending deploy" was previously a state the process entered and never left — at one point
understating delivery by ~50 items.

---

## Pre-deploy gate results

| Gate | Fires? | Result |
|---|---|---|
| 0 · uploads backup | yes | **OUTSTANDING — human step, do before extract** |
| 1 · UI / design guide | yes (.cshtml + js) | PASS by inspection — no native dialogs, no `<h1>`, no `.admin-container` or inline `max-width` change, no client-facing label altered. CR-049 *adds* correct `aria-pressed` maintenance where the markup had it hardcoded |
| 2 · cross-role + responsive | yes (`_Layout`) | PASS — internal, external and public all exercised; 5 viewports (1920/1440/1280/768/390), zero horizontal overflow, zero console errors |
| 3 · data scoping / RBAC | yes (controllers) | **PASS — 57P / 0F** (`rbac-matrix` 24P, `cert-detail-scoping` 10P, `idor-download` 2P, `partner-isolation` 21P). External still has no Edit control |
| 4 · Partner API contract | yes (shared service) | **PASS — `CONTRACT UNCHANGED`**, 7P / 0F vs the April baselines |
| 5 · traceability | always | PASS with 2 notes — DEF-420 stale status, `_Layout` unreferenced in its own diff (both above) |
| 6 · performance / EF | yes (`ProductIdService`) | PASS — the only client-side abort ceiling is 30 s on the **public** view; that endpoint measured **0.8 s** cold. Internal measured 1.7 s. (An earlier 38–42 s reading on the internal endpoint did **not** reproduce — see `ways-to-improve.md`; it has no abort ceiling behind it either way) |
| 7 · pending migrations | **no** | No migrations in this tree; test is already at the CR-045 high-water |
| 8 · CRUD write paths | yes (importers) | **PASS — 39P / 0F / 0W / 0B, GREEN** |
| 9 · static integrity | always | **0 FAIL**, GREEN-WITH-WARNINGS — all 13 warnings pre-existing (stale register status text, two known orphan views) |
| 10 · config points at data | no | Nothing in this bundle changes data-pointing config |
| 11 · close-out | after upload | Step 5 above |
| 12 · outbound email | after upload | Nothing here touches mail; skip unless you want the routine check |

### Bundle content verification

```
python Scripts/deploy/verify-bundle.py deployment-drops/GCCWebApp-deploy-20260910-2128.zip  → 0 failures
python Scripts/deploy/marker-check-20260910.py deployment-drops/GCCWebApp-deploy-20260910-2128.zip → 0 failures
```

`marker-check-20260910.py` was written for this deploy because `verify-bundle.py` checks a curated
list of *older* markers and cannot know about work added after it was last edited. Two bundles have
previously matched on entry count **and** size while differing by a whole fix, so each item is
asserted against the bundle's actual bytes: static JS md5-for-md5 against the working tree, and
compiled Razor markup as a literal inside `GCCWebApp.dll`.

It carries a **negative control** — `data-view="tile" aria-pressed="true"` must be **absent** — so a
pass means the old markup is genuinely gone rather than the search being vacuous.

> **One trap worth keeping.** The first run of the marker check reported three fixes ABSENT from the
> DLL. They were all present. .NET stores type and member names in the `#Strings` heap as UTF-8 but
> **string literals in the `#US` heap as UTF-16LE**, and compiled Razor markup is a string literal.
> An ASCII-only byte search finds the C# type name and misses every piece of view markup — which
> reads exactly like a bundle built from the wrong tree. The check now searches both encodings.

### Hygiene

- No `DataProtectionKeys` in the bundle (dev keys would ship **unencrypted**).
- No `appsettings.Development.json` (points at the dev DB).
- `AutoMigrateOnStartup` present and **false**.

---

## Rollback

The previous live bundle is `GCCWebApp-deploy-20260908-1607.zip` (deployed and verified 2026-09-08).
Re-extracting it reverts the code — but read it as a **downgrade**, not a neutral undo: it would
remove CR-048, CR-049, DEF-419, DEF-420 and DEF-342. Since this deploy has no database step, a
rollback is code-only and needs no schema work.

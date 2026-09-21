# Pre-deploy record — `GCCWebApp-deploy-20260921-0805.zip`

**Status: BUILT AND VERIFIED. NOT UPLOADED, NOT DEPLOYED.**
Target: TEST environment (gccwebapp.com). Built from branch `ci-regression`, working tree
(uncommitted — two parallel sessions' changes, see *What rides along*).

| | |
|---|---|
| **Artifact** | `deployment-drops/GCCWebApp-deploy-20260921-0805.zip` |
| **Size / entries** | 61.2 MB / **340** |
| **In-zip `GCCWebApp.dll`** | **md5 `0d2203208598b959`**, 6,713,856 bytes |
| **Built with** | `Scripts/deploy.ps1 -SkipUploadsBackupPrompt` (the canonical tool) |
| **Schema** | **No new migration.** High-water `20260908175939_CR045_CertificationResultsVerbatimSort`, unchanged since 2026-09-08 |
| **Bundle verifier** | `Scripts/deploy/verify-bundle.py` → **failures: 0** |

> **Identify this bundle by the DLL md5, not by size.** Six other ~61–62 MB zips sit in
> `deployment-drops/`, and on 2026-09-04 two same-size bundles differed by a whole fix.

## What it carries — all 11 resolved-but-unshipped items

Reconciled with `Scripts/deploy/resolved-not-shipped.py` (Gate 0B) **before** the cut: it reported
exactly 11 RESOLVED-AND-WAITING, 0 UNCLASSIFIED, 0 NO-STATUS. All 11 are in this build.

| Id | What |
|---|---|
| CR-051 | Built 2026-09-16 |
| CR-052 | Built 2026-09-16 (Option A, all five surfaces) |
| CR-053 | Built 2026-09-17 (Option C) — certificate preview pager |
| DEF-430 | Tile views stuck on "Loading…" after a failed certificate load |
| DEF-431 | Tile hydrate re-applied filters the page's ids had already resolved |
| DEF-432 | `CertificatePageCount` role-dependent JSON with no cache headers |
| DEF-434 | Preview pager swallowed Left/Right arrow keys in form fields |
| DEF-436 | Page 2 appeared to zoom in (thumbnail vs on-demand render size) |
| **DEF-437** | **CSV edit-import stripped ALL tags — fixed and verified 2026-09-20, 23/23** |
| DEF-438 | Preview popup consistent but unreadable (620px cap); pager below the fold |
| DEF-439 | No stylesheet cache-busted — a deployed CSS fix invisible for up to 7 days |

## Content markers — verified against the IN-ZIP bytes, not the build folder

| Marker | Result |
|---|---|
| DEF-437 — partitioned tag provenance key (`InternalTags`) in DLL | PASS |
| DEF-439 — `AppendVersion` tag-helper call compiled into DLL | PASS |
| DEF-438 — flat `max-height: min(85vh, 620px)` gone from active CSS | PASS |
| DEF-434 — pager arrow-key guard present in `certificate-preview-pager.js` | PASS |

Plus the verifier's own ~30 historical markers (DEF-381/383/385/390/394/395/401/402/404/405/406/407/409/410,
CR-043/045, AutoMigrateOnStartup=false, …) — all PASS.

## Hygiene

| Check | Result |
|---|---|
| `wwwroot/uploads` excluded | PASS |
| `App_Data/uploads` excluded | PASS |
| `appsettings.Development.json` / dev config excluded | PASS |
| `appsettings.*.template.json` excluded | PASS |
| `EnvironmentName` is `Production` | PASS |
| **`DataProtectionKeys/` absent** | PASS (explicitly scanned) |
| `.bak` / `.orig` / editor leftovers absent | PASS |
| `Scripts/`, `docs/` absent | PASS |

## Migration script

- 38 migrations, `InitialCreate_SqlServer_Final` … `CR045_CertificationResultsVerbatimSort`.
- **DEF-386 repair applied: 3 `EXEC(CONCAT(...))` rewritten** to
  `DECLARE @efSqlN nvarchar(max) = CONCAT(...); EXEC(@efSqlN);` — verified in the shipped file:
  3 DECLAREs, 3 matching EXECs, **0 unrepaired**. Without this the script aborts at statement one.
- **Post-deploy DATA scripts appended** (Gate 7): `CR-043-productid-status-backfill.sql` and
  `CR-171-partner-backfill.sql`.
- ⚠️ **Outstanding WARN, not a failure:** the verifier prefers `-FromMigration <high-water>` over a
  full-history script, because SQL Server compiles every block before its guard can skip it. This
  bundle ships full history, as every prior bundle has. Left as-is deliberately — switching would
  have changed the shipped script's shape on a drop that carries no schema change at all — but it is
  the right thing to fix on the next bundle that *does* carry one.

## What rides along that is not on the list above

The working tree is uncommitted and spans two parallel sessions. Tracked code changes in the cut:
`Controllers/CertificateController.cs`, `Services/Import/CertificateImportService.cs`,
`Views/Shared/_Layout.cshtml`, `wwwroot/css/main.css`, `wwwroot/js/certificate-preview-pager.js`.
All five map to the 11 items above. Test-harness files (`Scripts/`, `docs/`) are excluded by the
publish profile and are not in the zip.

## What this bundle would UNDO

A bundle is a **full overwrite** of the application folder. Diffed against the previous bundle
(`20260918-1302`): **no entry present there is missing here** — the earlier 2-entry gap was an
artifact of my first, hand-rolled cut and does not exist in this one. Uploads and user data are
excluded, so extracting cannot clobber the PDF store.

It does **not** contain anything newer that may have been changed on the server since the last
deploy. That has not been checked — see *Not checked* below.

## Still owed at deploy time — NOT done by this build

1. **Back up the SERVER `wwwroot/uploads/` before extracting.** The build's own prompt was skipped
   (`-SkipUploadsBackupPrompt`) because this was a build, not a deploy. Standing rule, and the
   2026-06-15 all-PDFs-missing incident is why.
2. **Run `migrations\apply-migrations.sql` BEFORE recycling the app pool.** No-op if the target DB is
   current, but it also carries the two post-deploy data backfills.
3. **Recycle the IIS app pool.**
4. **Verify through the client's click path**, not just a route probe — and confirm
   `/appsettings.Production.json` does **not** return 200.
5. **DEF-439 specifically:** confirm the emitted HTML carries `?v=` on the stylesheet links. A
   CSS-only fix cannot be verified by probing the server for a file.
6. **Close the loop:** update the 11 register statuses to deployed, append to
   `docs/tracking/shipped-2026-09.md`.

## Not checked — stated so it is not mistaken for a cleared gate

- **What is currently live on gccwebapp.com was not probed.** No `whats-deployed.js` run, no md5
  comparison against the server. So "what this bundle would undo" is answered only against the
  previous *bundle*, not against the *server*.
- **No smoke suite was run against this build.** DEF-437's own verification (23/23) ran against the
  same code on localhost, but the other ten items were verified by their own sessions, not re-run here.
- **Nothing was deployed.** The zip has not left this machine.

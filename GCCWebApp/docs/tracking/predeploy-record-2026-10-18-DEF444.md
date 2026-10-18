# Pre-deploy record — `GCCWebApp-deploy-20261018-1923.zip`

**Status: BUILT + GATED 2026-10-18, NOT UPLOADED.**
Target: gccwebapp.com (serves `db.greencirclecertified.com`; db. is an alias on the same site and app
pool). Built from branch `ci-regression` at commit `1bf8bb9e` with
`Scripts/deploy.ps1 -SkipUploadsBackupPrompt`. Carries **DEF-444 only**.

| | |
|---|---|
| **Artifact to upload** | `deployment-drops/GCCWebApp-deploy-20261018-1923.zip` (64,192,946 bytes, 341 entries, `testzip()` clean) |
| **In-zip `GCCWebApp.dll`** | **6,725,120 bytes, md5 `17e6085984ff8533`**. Live (9/29-1502) is 6,728,704 bytes, md5 `020c8cd1a1462049` |
| **In-zip `GCCWebApp.runtimeconfig.json`** | **`"System.GC.Server": false`**. Live is `true` |
| **Schema** | **No new migration.** High-water still `20260908175939_CR045_CertificationResultsVerbatimSort`. `apply-migrations.sql` differs from 9/29 only by regenerated seed timestamps (6 lines). **Do not run it** |
| **Server-owned files** | `wwwroot/robots.txt`, `appsettings.Production.json` and `web.config` are **byte-identical to the 9/29-1502 bundle** that is live. Extracting does not change them |

## Root cause (DEF-444)

The app pool for gccwebapp.com / db. stopped on 2026-10-15 and 2026-10-16 and stayed stopped until it
was started by hand in Plesk.

- **Host confirmed (AccuWeb ticket, 2026-10-18):** the pool's **512 MB private-memory limit** was being
  exceeded and triggered the recycles. **Rapid-Fail Protection is on** (threshold 5 failures). No
  maintenance or host-side action in the window.
- **Our logs (stdout, 2026-10-16 19:43–20:00):** four process starts. Each started cleanly, served all
  requests with 200, then stopped in an orderly way, right after Amazon `GET /api/v1/certificates`
  bursts. No application exception.
- **Why memory was high (our side):**
  1. **Server GC** (Web SDK default) sizes heaps per CPU core and holds memory longer. Wrong mode for a
     512 MB cap on a shared multi-core host.
  2. **`RequestResponseLoggingMiddleware` copied every response body** into a MemoryStream. An Amazon
     page is up to 2.2–2.4 MB. The copy only fed an object that was never logged.
- Rate limits (60/min per IP, 200/hr per key) cap the number of requests, not the memory per request.

**Fixes in this bundle:**
1. `GCCWebApp.csproj` → `<ServerGarbageCollection>false</ServerGarbageCollection>` (Workstation GC).
2. `RequestResponseLoggingMiddleware.cs` → response bodies are no longer buffered. Log output is
   unchanged (status and duration only, as before).

**Not in this bundle:** Fix 3 (bound the `[ApiCache]` response cache on the partner list). Proposed in
DEF-444, waiting on the 10/16 IIS log.

## Gates

| Gate | Result |
|---|---|
| 0 — back up uploads | **Shawn, before extract** |
| What is live | Probed 2026-10-18: `/` 200, `/health/live` 200, `/health/ready` 200, `/Home/NotFound` 404, `/api/v1/certificates` 401 (no key), `/appsettings.Production.json` 404, `robots.txt` 200 public. Consistent with the 9/29-1502 build (live DLL not checked on the server) |
| Bundle vs live bundle (CRC, all 341 entries) | Changed: `GCCWebApp.dll`, `.exe`, `.pdb`, `GCCWebApp.runtimeconfig.json`, `migrations/README.md`, `migrations/apply-migrations.sql`. None added, none removed |
| Source changed since live build | `GCCWebApp.csproj`, `Middleware/RequestResponseLoggingMiddleware.cs` (DEF-444). `Program.cs` also differs from commit `8d511661`, but the diff is the DEF-442 retry, which the live 9/29 build already carries (it was built from the uncommitted change) |
| `verify-bundle.py --no-migrations=…CR045…` | **0 failures** (1 standing WARN: full-history migration script — not being run) |
| DLL markers | `[Response too large to log]` 1→0 (Fix 2 in); `StartupConnectBudgetSeconds` 1→1 and `DEF-442 startup retry` 1→1 (DEF-442 kept); controls `Browse all certificates` 1→1, `Certificate Preview` 10→10 |
| Local verification (Release build, dev DB) | Health, home, public tile view, tile filters → 200. Fresh dev Amazon key: no key → 401; Amazon pages 1–3 (`includeProductIds=true&pageSize=100`) all 200 with complete JSON (1,683,512 / 2,367,208 / 524,802 bytes), 217/217 items; repeat of page 1 (cache path) complete. Private memory after the run 98 MB |
| 1–4B, 5, 6, 8, 9, 10 | **Not run.** Change is a GC runtime setting plus removal of an unused response copy in logging middleware; no view, query, scoping or API-contract change. Stated, not waived silently |
| 7 — migration | None needed (see Schema) |

**Not verified:** memory behaviour on the production host under a real Amazon burst. That is the
48-hour watch below, not something a local run can prove.

## Deploy steps (Shawn)

1. **Gate 0:** back up the server's `wwwroot/uploads/`.
2. Stop the app, upload and extract the zip over the site, start the app (DEF-259 lesson: don't
   extract over a running worker).
3. No migration.
4. **Optional, same window:** in Plesk, set the pool's Managed pipeline mode to **Integrated** and
   .NET CLR version to **No Managed Code** (Microsoft guidance for ASP.NET Core; not the cause).
5. Recycle / start the app pool.
6. Tell Claude — post-deploy checks follow.

## Post-deploy checks

- `/` → 200; `robots.txt` still public; `/appsettings.Production.json` → 404; `/health/ready` 200.
- Public tile view + a certificate detail page 200; `/api/v1/certificates` → 401 without a key, 200 with
  the Amazon key.
- **On the server:** `GCCWebApp.runtimeconfig.json` reads `"System.GC.Server": false`, and
  `GCCWebApp.dll` = **6,725,120 bytes**, dated 2026-10-18. This proves the zip extracted.
- stdout log after the restart: normal startup, no `crit` lines.
- **48-hour watch:** no pool stops, including during Amazon bursts. If possible, note the worker's
  private memory during a burst (Plesk or AccuWeb); it should stay well under 512 MB.
- Close-out: DEF-444 → deployed; append to the shipped log.

## Rollback

- **Whole bundle:** redeploy `deployment-drops/GCCWebApp-deploy-20260929-1502.zip`.
- **Fix 1 only, no rebuild:** set `"System.GC.Server": true` in the server's
  `GCCWebApp.runtimeconfig.json` and restart the pool.

## Host-side (not in the bundle)

- AccuWeb offers +512 MB private memory for $10/month. Decide after the 48-hour watch.
- Still wanted from AccuWeb: the WAS event entries (their screenshot) for 2026-10-15 and 2026-10-16,
  to confirm which events counted toward Rapid-Fail.

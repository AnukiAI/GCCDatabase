# Pre-deploy record — `GCCWebApp-deploy-20260929-1502.zip`

**Status: UPLOADED to test.gccwebapp.com 2026-09-29 (per Shawn) — NOT the main site. Build on test NOT
yet confirmed** (see "Post-deploy, test.gccwebapp.com" at the end). Main site (db.greencirclecertified.com)
still on 9/23-2211.

**Original status: BUILT + GATED 2026-09-29, NOT UPLOADED.**
Target: gccwebapp.com (serves `db.greencirclecertified.com`). Built from branch `ci-regression` with
`Scripts/deploy.ps1 -SkipUploadsBackupPrompt`. Carries **DEF-442 only**.

| | |
|---|---|
| **Artifact to upload** | `deployment-drops/GCCWebApp-deploy-20260929-1502.zip` (61.2 MB, 339 entries, `testzip()` clean) |
| **In-zip `GCCWebApp.dll`** | **md5 `020c8cd1a1462049`**. Live (9/23-2211) is `3aa8acaf2c8d6127` |
| **Schema** | **No new migration.** High-water still `20260908175939_CR045_CertificationResultsVerbatimSort`. `apply-migrations.sql` differs from 9/23 only by regenerated timestamps. **Do not run it** |
| **Server-owned files** | `wwwroot/robots.txt` and `appsettings.Production.json` are **byte-identical to the 9/23-2211 bundle** that is live. Live probe 2026-09-29: robots `Allow: /`, `/` → 200 (gate off). Extracting does not revert either |

## Root cause (DEF-442)

The stdout log from the outage (`stdout_20260929171640_19112.log`) shows the app starting normally,
then **failing its one-time database check** against SQL at `68.71.129.120,1533` and throwing
(`Program.cs:1229`). The process exits. IIS restarts it into the same failure, and after repeated
crashes IIS Rapid-Fail Protection **stops the app pool** — every URL then returns 503 until someone
restarts the pool by hand, even after SQL is back.

- **Trigger (hosting side):** SQL Server unreachable at the moment of startup. Why is for the host
  ticket.
- **Amplifier (our code):** the check was a single `CanConnectAsync()`, which swallows the error and
  returns false, so the configured `EnableRetryOnFailure` never ran. One blip at a cold start = outage.
- **Frequency (hosting config):** the app pool is not set to AlwaysRunning, so idle shutdowns create
  more cold starts, each one exposed to the check.
- **Seen before, misattributed:** the 503s of 2026-06-09 and 2026-09-08 match this signature and were
  put down to deploy mechanics / "stopped pool" without reading the stdout log.

**Fix in this bundle:** bounded retry at startup (5/10/15/20/30s backoff, 90s total budget, under
IIS's 120s startup limit). Verified locally: gives up cleanly when the DB never returns; comes up on
its own when the DB returns mid-retry. Details in DEF-442.

## Gates

| Gate | Result |
|---|---|
| 0 — back up uploads | **Shawn, before extract** |
| 0B — resolved-not-shipped | 1 pending (DEF-442) — **in this build**. 0 unclassified, 0 no-status |
| What is live | Probed: `/Home/NotFound` → 404 with the DEF-441 page, so the 9/23 build is live |
| Bundle vs live bundle (CRC, all 339 entries) | Changed: `GCCWebApp.dll`, `.pdb`, `migrations/README.md`, `migrations/apply-migrations.sql`. None added, none removed |
| Source changed since live build | `Program.cs` only (mtime scan of app source, excl. bin/obj/tests/scripts/docs) |
| `verify-bundle.py` | **0 failures** (1 standing WARN: full-history migration script — not being run) |
| DLL markers | `StartupConnectBudgetSeconds` 0→1, `DEF-442 startup retry` 0→1; controls `Browse all certificates` 1→1, `Certificate Preview` 10→10 |
| 1–4B, 5, 6, 8, 9, 10 | **Not run.** Change is confined to the startup DB check in `Program.cs`; no view, endpoint, query, scoping, API or config change. Stated, not waived silently |
| 7 — migration | None needed (see Schema) |

**Not verified:** the Production startup branch at runtime (same helper as Staging, which was
tested); behaviour against a *network* outage as opposed to a refused login.

## Deploy steps (Shawn)

1. **Gate 0:** back up the server's `wwwroot/uploads/`.
2. Stop the app, upload and extract the zip over the site, start the app (DEF-259 lesson: don't
   extract over a running worker).
3. No migration.
4. Recycle the app pool.
5. Tell Claude — post-deploy checks follow.

## Post-deploy checks (Gates 11 + 12)

- `/` → 200, no banner; `robots.txt` still `Allow: /`; `/appsettings.Production.json` → 404.
- Public tile view + a certificate detail page 200; `/api/v1/certificates` → 401 without a key.
- stdout log after the recycle: normal startup, **no** `DEF-442` warnings expected when SQL is healthy.
- Gate 12: password reset to `2fatest` arrives.
- Close-out: DEF-442 → deployed; append to `shipped-2026-09.md`.

## Host-side (not in the bundle)

Ask the host for: app pool Start Mode **AlwaysRunning**, Idle Time-out **0**, Rapid-Fail Protection
relaxed; and whether SQL at `68.71.129.120:1533` was reachable at 2026-09-29 17:16:40 server time.

## Post-deploy, test.gccwebapp.com — 2026-09-29 (Shawn reported "deployed" with the test URL)

Probed from outside, unauthenticated:

| Check | test.gccwebapp.com | db.greencirclecertified.com (control) |
|---|---|---|
| `/` | 302 → `/__gate` → "GCC Test Environment" password page — **gate ON** ✅ | 200, no banner ✅ |
| Every other page (`/Home/NotFound`, `/health/live`, `/appsettings.Production.json`) | 302 → gate (cannot see past it) | 404 / 200 / 404 ✅ |
| `/api/v1/certificates` | 401 ✅ | 401 ✅ |
| `robots.txt` | **`Allow: /`** ⚠️ | `Allow: /` ✅ |
| Gate page | `<meta name="robots" content="noindex, nofollow">`, `no-store` | — |

**Not confirmed from outside: that the new DLL is running on test.** DEF-442 has no visible marker
(its log lines only appear when SQL is unreachable), and everything else on test sits behind the gate.
Confirm on the server: `GCCWebApp.dll` = **6,728,704 bytes**, 2026-09-29 (the 9/23 DLL is 6,725,632),
and the newest `logs\stdout_*.log` shows a normal startup with no `crit` lines.

**Finding — robots.txt on the gated test site says `Allow: /`.** With the gate ON the app falls through
to the static `wwwroot/robots.txt` (`Program.cs:~898`), and that file was changed to the public version
on 2026-09-23 to stop full bundles reverting the main site. The `Program.cs` comment still says the file
"ships as `Disallow: /`". **Exposure is low:** every URL on test redirects to the gate page, which is
`noindex, nofollow`. **Cannot tell** whether this arrived today or with an earlier bundle to test — the
file is byte-identical to the 9/23 bundle. Not fixed; raised for triage.
## Post-deploy, PRODUCTION (db.greencirclecertified.com) — 2026-09-29 (Shawn: "I just deployed the zip to prod")

A 503 seen on db. just before this was the deploy's restart, not a fault.

| Check | Result |
|---|---|
| `/` | 200, no banner / gate ✅ |
| `robots.txt` | 200, `Allow: /` (public, unchanged) ✅ |
| `/appsettings.Production.json` | 404 — handler active ✅ |
| `/Home/NotFound` | 404 with the DEF-441 page ✅ |
| `/Certificate/CertificateTileView` | 200 ✅ |
| `/Certificate/CertificateSingleView/26-0330` | 200 ✅ |
| `/api/v1/certificates` (no key) | 401 ✅ |
| `/health/live` | 200 ✅ |
| `/health/ready` (includes DB) | 200 `Healthy` ✅ |
| `gccwebapp.com/some/path?x=1` | 301 → `db.…/some/path?x=1` ✅ |

**Still open before close-out:** confirm on the server that `GCCWebApp.dll` = **6,728,704 bytes**, dated
2026-09-29, and that the newest `logs\stdout_*.log` has no `crit` lines. Gate 12 (password-reset email
to `2fatest`) not yet run. The DEF-442 retry itself is only observable during a DB outage.- **Bundle identity confirmed (2026-09-29):** Shawn uploaded "the one from 3:06 today". `GCCWebApp-deploy-20260929-1502.zip` is the ONLY zip built today, and its file time is **15:06:09** (the name is the build START; the zip finished writing at 15:06). Same file, 64,198,580 bytes. Extraction on the server (DLL 6,728,704 bytes) is still unconfirmed.
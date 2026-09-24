# Pre-deploy record — `GCCWebApp-deploy-20260923-1456-HOTFIX-keep-server-config.zip`

**Status: SUPERSEDED — NOT UPLOADED.** Shawn asked for a full rebuild instead. What shipped: **`GCCWebApp-deploy-20260923-2211.zip`**, a full bundle built AFTER the repo's `wwwroot/robots.txt` (public version) and `appsettings.Production.json` (`TestEnvironmentGate` Enabled/ShowBanner **false**) were changed to match the live site, so the full zip no longer reverts them. Same DLL (md5 `3aa8acaf2c8d6127`). **DEPLOYED to gccwebapp.com 2026-09-23, post-deploy verified** (gate off, robots public, DEF-441 pages live, 1,966/1,966 legacy links, API 401, Gate 12 email arrived). Gates 1–10 other than 0B were skipped at Shawn's direction.

**Original status:** built + gated 2026-09-23, not uploaded. Hotfix for DEF-441, chosen by Shawn over holding
for the next build.
Target: gccwebapp.com, which serves `db.greencirclecertified.com` (now the public site).
Built from branch `ci-regression`, uncommitted working tree.

| | |
|---|---|
| **Artifact to upload** | `deployment-drops/GCCWebApp-deploy-20260923-1456-HOTFIX-keep-server-config.zip` |
| **Entries** | 339 (the built zip's 341 minus two server-owned files, see below). `testzip()` clean |
| **In-zip `GCCWebApp.dll`** | **md5 `3aa8acaf2c8d6127`**. The live 9/22 DLL is `ae50f49df5e7e622` |
| **Built with** | `Scripts/deploy.ps1 -SkipUploadsBackupPrompt`, then the two files removed with Python (fresh zip headers, CRC-tested) |
| **Schema** | **No new migration.** High-water still `20260908175939_CR045_CertificationResultsVerbatimSort`. `apply-migrations.sql` differs from 9/22 only by regenerated seed timestamps (6 lines). **Do not run it**: nothing to apply |
| **DO NOT UPLOAD** | `GCCWebApp-deploy-20260923-1456-DO-NOT-UPLOAD-overwrites-gate-and-robots.zip`, the unmodified build. See the gate failure below |

## Gate 0B: 1 resolved-and-waiting (DEF-441), 0 unclassified, 0 no-status

DEF-441 is in the build.

## ⛔ Gate failure caught: the repo still describes the TEST environment

The built zip carried two files that would have **reverted the public site to test mode**:

| File in zip | Zip says | Live right now | Effect if extracted |
|---|---|---|---|
| `wwwroot/robots.txt` | `Disallow: /` (test) | public directory version, `Allow: /` | **search engines told to drop the whole public site** |
| `appsettings.Production.json` | `TestEnvironmentGate: Enabled: true, ShowBanner: true` | gate off (`/` → 200) | **password gate + red banner back on the public site**, if the server's override lives in this file |

Both were changed **by hand on the server** when the gate came off on 2026-09-22 (the 9/22 record
notes it: the 9/22 zip also had `Enabled: true` / `Disallow: /`). Nothing in the repo records the
live values, so **every full bundle built from this repo reverts them**.

**Resolution for this hotfix:** the two files are removed from the zip, so the server keeps its own
copies. The only change to `appsettings.Production.json` since 9/22 is `SmtpSettings.FromEmail` →
`noreply@db.greencirclecertified.com`, which `web.config` already sets as an environment variable
(and which is already the live sender, as the client confirmed).

**Waiver, logged:** `verify-bundle.py` reports `failures: 3`, all from the deliberately absent
`appsettings.Production.json` (the file check plus the two `AutoMigrateOnStartup` checks that read
it). Everything else passes. `verify-redirect-map.py --bundle`: ALL CHECKS PASSED.

**Follow-up, required before the next full bundle:** bring `appsettings.Production.json` and
`wwwroot/robots.txt` in the repo into line with production (and give `test.gccwebapp.com` its own
override), or keep excluding them. Until then, **every** bundle must be checked for these two files.

## What it carries vs the live 9/22 bundle (entry-level CRC diff, 339 files each before removal)

| Changed | Why |
|---|---|
| `GCCWebApp.dll` / `.pdb` / `.xml` | DEF-441 (Program.cs, HomeController, CertificateController, `Views/Home/NotFound.cshtml`) |
| `web.config` | the 10 legacy-link / gccwebapp.com redirect rule changes from 2026-09-23, **already hand-applied on the server**, plus the `SmtpSettings__FromEmail` env var (already live). Parsed-XML comparison: zip == repo |
| `rewritemaps.config` | 6,018 entries (+19), **already uploaded to the server** 2026-09-23 |
| `migrations/*` | regenerated timestamps only |

No file added, none removed apart from the two server-owned ones. **All 89 site js/css/txt files in
the zip are byte-identical to what is live now**, so nothing else hand-edited on the server gets overwritten.

**Riding along, already live:** the other uncommitted code in `Program.cs`, `CertificateController.cs`
and `FileUploadService.cs` (DEF-208/400/432/433/438, CR-053) was in the 9/22 build (per its record,
built from this same working tree). Source files modified since the 9/22 build: only the three
DEF-441 files plus `web.config`, `rewritemaps.config` and `appsettings.Production.json`.

**DLL markers (raw byte search, both encodings):**

| Literal | 9/22 DLL | new DLL |
|---|---|---|
| `Browse all certificates` (DEF-441 view) | 0 | 1 |
| `Certificate not available` (DEF-441 controller) | 0 | 1 |
| `View PDF` (CR-054, control) | 1 | 1 |
| `Certificate Preview` (positive control) | 10 | 10 |

## Deploy steps (Shawn)

1. **Gate 0:** back up the server's `wwwroot/uploads/`.
2. Upload the **HOTFIX-keep-server-config** zip and extract it over the site.
3. No migration.
4. **Restart the site / recycle the app pool** (the DLL changed).
5. Tell Claude. Post-deploy checks follow.

## Post-deploy checks (Gates 11 + 12)

- The not-found page appears for a missing page, `/certificate/99999999` and a non-public
  certificate. Status stays 404.
- The gate is **still off** (`/` → 200), there's no banner, and `robots.txt` is still the public version.
- Legacy links: re-run `Scripts/cutover/verify-legacy-links.py`, expect 1,966 / 1,966.
- Amazon API `/api/v1/certificates` → 401 without a key.
- Gate 12: a password reset to `2fatest` (the only account that receives mail) arrives from
  `noreply@db.greencirclecertified.com`.
- Close-out: DEF-441 → deployed.

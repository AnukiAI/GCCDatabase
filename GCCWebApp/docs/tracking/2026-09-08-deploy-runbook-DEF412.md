> ## ⚠ SUPERSEDED — do not deploy this bundle
>
> `GCCWebApp-deploy-20260908-DEF412.zip` (11:28) was **overtaken the same afternoon** by
> `GCCWebApp-deploy-20260908-1607.zip`, which contains this DEF-412 fix **plus** further code and
> the `20260908175939_CR045_CertificationResultsVerbatimSort` migration. The 16:07 bundle was
> deployed and **DEF-412 is live** — verified 2026-09-09, see the outcome at the foot of this file.
>
> **Uploading the 11:28 zip now would be a downgrade**: a zip is a full overwrite, so it would roll
> back everything added between 11:28 and 16:07 and would leave the code behind an already-applied
> schema. Keep it only as a build artifact ([[feedback_bundle_ask_what_it_would_undo]]).
>
> **The process lesson:** I handed over a bundle path without re-checking the drops folder for
> anything newer. That folder is shared and other sessions build into it — between building at 11:28
> and quoting the path the next day, a better bundle had appeared and shipped. **Re-check the drops
> folder immediately before naming a zip, not when you build it.**


# Deploy runbook — 2026-09-08 → gccwebapp.com (test) — DEF-412 export column

**Bundle:** `deployment-drops/GCCWebApp-deploy-20260908-DEF412.zip` — 337 entries, 61.1 MB
**Verified:** `python Scripts/deploy/verify-bundle.py <zip> --no-migrations=20260901164220_CR037_MultiCountryCertificates` → **0 failures, exit 0**

Built with `dotnet publish GCCWebApp.csproj -c Release -p:PublishProfile=PleskTestEnvironment`.
Note the explicit `.csproj`: running it against the folder publishes the **solution**, which builds
the test projects and leaves the web project's publish directory empty. Output lands in
`bin/Release/net8.0/win-x64/publish/` (the `win-x64` segment comes from the profile's
RuntimeIdentifier), not `bin/Release/net8.0/publish/`.

## What is in this drop

**One code change: DEF-412.** A `Cert Subcategories` column added to all four server-side
certificate export paths (2 CSV generators, 2 Excel generators) in `Controllers/ExportController.cs`.
Nothing else was written for this deploy.

**Proven, not assumed, against the bundle already sitting in `deployment-drops` from 06:48 today:**

| | 06:48 bundle | this bundle |
|---|---|---|
| entries | 339 | 337 |
| `GCCWebApp.dll` md5 | `66ff7b8f4b4b1335` | `faea68b6a74a55d7` |
| DEF-412 header in the DLL | **ABSENT** | **PRESENT** |

**The only files that differ between the two bundles are `GCCWebApp.dll`, `.pdb` and `.xml`.**
Every other entry is byte-identical (the 2-entry count difference is two empty directory records).
So this drop changes the application binary and nothing else — no wwwroot, no config, no
dependencies. **What it would UNDO relative to the 06:48 bundle: nothing.**

**Marker check on the DLL itself** (memory: `feedback_two_bundles_same_size_differ` — 339 entries /
61.1 MB once matched while a whole fix was missing, so size and count prove nothing):

* the full new CSV header is present as a UTF-16 literal
* occurrences of the old header **not** followed by `,Cert Subcategories`: **0**
* `.NET` stores literals UTF-16 — an ASCII byte search reports ABSENT and is the wrong test

## No schema change

Repo high-water is `20260901164220_CR037_MultiCountryCertificates`, and it is **already applied** on
the target (read from `__EFMigrationsHistory`, 2026-09-08). `AutoMigrateOnStartup` is `false` in
`appsettings.Production.json` — confirmed in the bundle — so nothing migrates on start either way.
The verifier was told this explicitly with `--no-migrations=`; it refuses a bundle with neither a
migrations script nor that declaration, which is why the first run failed.

## Pre-flight already done

| Check | Result |
|---|---|
| `ASPNETCORE_ENVIRONMENT` in bundled web.config | **Production** (`hostingModel=inprocess`) |
| DataProtection keys in bundle | 0 |
| `appsettings.Development.json` / `appsettings.Secrets.json` | 0 / 0 |
| `wwwroot/uploads` / `App_Data/uploads` | 0 / 0 |
| DEF-401/402/404/405/406/407/409/410 markers | all PRESENT, matching what is already live |
| Release build | succeeded, warnings only |
| Behaviour verified | all four exports downloaded and parsed against dev — column present and populated |

## Steps — REMAINING, and they need a human

### 1. Back up first
- Database backup of `gccwebap_certificates`.
- Zip the server's `wwwroot/uploads/`. This bundle contains no uploads directory (verified 0), so
  extraction will not touch the PDF store — take the backup anyway. Both the 2026-04-23 upload wipe
  and the 2026-06-15 all-PDFs-missing incident started here.

### 2. Upload the code
Upload and extract `GCCWebApp-deploy-20260908-DEF412.zip` over the Plesk site root, then recycle the
app pool. **This step is manual — there is no automated upload path; `Scripts/deploy.ps1` only
builds the zip.**

`appsettings.Secrets.json` is a server-only overlay and is deliberately not in the bundle. Confirm it
survived the extract or outbound email silently falls back to the mock service.

### 3. Verify live (Gate 11 — not optional)
- `/appsettings.Production.json` must return **404**, not 200.
- Sign in as `admin.uat` and download
  `/Export/DownloadReport?type=active-certificates&format=csv` — the header must end
  `...,Country,Cert Subcategories`, and rows for certificates that have subcategories must carry
  them pipe-delimited.
- Spot-check the Excel form of the same report: column 10, header `Cert Subcategories`.

Expected on test after deploy: 150 of the ~1,024 rows in the active-certificates report carry
subcategories (the other 77 subcategory-bearing certificates are Expired or Confidential and are
correctly outside that report's scope).


---

## OUTCOME — DEF-412 is live, verified 2026-09-09

Delivered by `GCCWebApp-deploy-20260908-1607.zip`, not by this bundle.

The site was returning **HTTP 503 on every URL** from `Microsoft-HTTPAPI/2.0` — IIS answering while
the app never started, i.e. a stopped app pool rather than a code fault. Shawn restarted the pool
and it came back on the first probe.

**Gate 11, all pass:**

| Check | Result |
|---|---|
| `/appsettings.Production.json` | **404** — handler active |
| Sign in (`admin.uat`) | OK |
| Certificate detail page | HTTP 200, renders — code and schema agree |
| **`Cert Subcategories` in the CSV export** | **PRESENT**, index 9 of 10 |
| Rows populated | **169 of 1,063** |

Checked independently of the app first: `__EFMigrationsHistory` already holds
`20260908175939_CR045_CertificationResultsVerbatimSort`, so the schema was ahead of the outage, not
broken by it. The 456 subcategory links were intact throughout.

The export also confirms **CR-046 end to end**: `26-0249` exports `Zero Waste to Landfill`, one of
the six taxonomy rows created yesterday — a value that did not exist in the system two days ago now
reaching a client-facing export.

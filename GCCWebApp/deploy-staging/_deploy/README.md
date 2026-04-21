# Deployment Package — 2026-04-21

**Contents of the ZIP at the root = full published ASP.NET Core 8.0 web app.**
Extract the zip contents directly into the site's `httpdocs` (or equivalent) folder. The `_deploy/` folder is supporting material only — do **not** copy it into the live site.

## Zip layout

```
<zip root>/
├── GCCWebApp.dll           ← ASP.NET app (extract to httpdocs)
├── GCCWebApp.exe
├── web.config              ← IIS config
├── appsettings.*.json
├── wwwroot/                ← static assets
├── runtimes/
├── ...(~145 files at root)
└── _deploy/                ← NOT part of the live site
    ├── README.md           ← this file
    ├── certificates/       ← correctly-named PDF for server filesystem
    └── sql/                ← SQL scripts (already executed — audit trail)
```

## What this release includes

### 1. Defect fix — Partner API status contract violation (HIGH)
**Problem:** Amazon was receiving internal status values (`"Client Approved - Paid"`, `"Draft"`) and Draft certs were leaking into the feed.
**Contract:** Amazon Developer Guide v1.0 §4 / §8 — status must be `"Active"`, only active certs returned.
**Fix:** `Controllers/Api/v1/CertificatesController.cs` — all Partner-facing endpoints now filter by `PublicStatuses` and project `status = "Active"` for Partner callers. Admin / Analyst callers unchanged.

### 2. Data correction — cert 26-0126 → 26-0106
Dev and Test/Prod DBs were both updated on 2026-04-21. The SQL in `_deploy/sql/` is **already executed** and kept only as audit trail.

## Deploy steps

1. Stop IIS app pool on gccwebapp.com.
2. Back up current `httpdocs` (or equivalent).
3. Extract the zip root contents into `httpdocs`, **excluding** `_deploy/` and **preserving** `wwwroot/uploads/` (don't overwrite existing uploads).
4. **Server filesystem cleanup** (see next section).
5. Start IIS app pool.
6. **Smoke test:**
   ```
   curl -s -H "X-API-Key: <amazon-key>" https://gccwebapp.com/api/v1/certificates/gs1 | jq '.data[0].certificationInfo.status'
   ```
   Expected: `"ACTIVE"`. No values like `"Client Approved - Paid"` or `"Draft"` anywhere in the response.

## Server filesystem cleanup (Test/Prod — `wwwroot/uploads/`)

The Test/Prod DB now references these canonical files. Rename/delete on the server filesystem to match:

| Cert | Keep | Delete (examples) |
|---|---|---|
| 26-0106 | `26-0106_20260420_173717.pdf` *(rename from `26-0126_...`)* | old `26-0126_*.pdf` |
| 26-0146 | `26-0146_20260420_173720.pdf` | earlier-timestamped duplicates |
| 26-0147 | `26-0147_20260420_173727.pdf` | earlier-timestamped duplicates |
| 26-0150 | `26-0150_20260420_173730.pdf` | earlier-timestamped duplicates |
| 26-0151 | `26-0151_20260420_173732.pdf` | earlier-timestamped duplicates |
| 26-0157 | `26-0157_20260420_173734.pdf` | earlier-timestamped duplicates |

Thumbnails in `wwwroot/uploads/images/` follow the same pattern (`..._thumb_...`).

The PDF in `_deploy/certificates/` is pre-named correctly for 26-0106 — you can upload it instead of renaming, if easier.

## Rollback

- **Code:** `git revert` the 2026-04-21 Partner API commit.
- **Data (Test/Prod):**
  ```sql
  UPDATE Certificates
  SET Number = '26-0126',
      CertificateFile = 'uploads/certificates/26-0126_20260420_173717.pdf',
      CertificateImagePath = 'uploads/images/26-0126_thumb_20260420_173717.png'
  WHERE Id = 68;
  ```

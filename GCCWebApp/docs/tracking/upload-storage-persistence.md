# Upload Storage Persistence (DEF-286 / QC #198)

**Purpose:** keep uploaded certificate PDFs and generated thumbnails alive across deployments.
This is the durable fix for the recurring "PDF not found after deployment" symptom (QC #198)
and the [2026-06-15 incident](../../observations_log.md) where the prod upload store was wiped.

## How upload storage works now

- **Logical path (DB):** `Certificate.CertificateFile` / `Certificate.CertificateImagePath`
  store a stable logical path like `uploads/certificates/{file}.pdf`. This never changes and
  is environment-independent.
- **Physical root:** resolved at runtime by `Services/UploadPathResolver.cs`
  (`IUploadPathResolver`) — the single source of truth for every reader and writer
  (controllers, file-upload service, import service). Config key:
  `FileStorage:StoragePath`.
  - **Empty / unset (default):** `ContentRootPath/App_Data/uploads` — the DEF-208 home.
  - **Set:** the uploads root (the folder that directly contains `certificates/` and
    `images/`). Absolute path used as-is; relative resolved under `ContentRootPath`.
- **Read fallback order** (so legacy files stay reachable during a migration):
  configured root → `App_Data/uploads` → `wwwroot/uploads` (pre-DEF-208).
- **Missing-file logging:** when the DB references a file that is not on disk, every download
  path logs a structured warning naming the cert number, the logical path, and the resolved
  physical path, with a pointer to this incident/backup procedure.

## Why this matters

`App_Data/uploads` lives **inside** the deployment folder. A Plesk publish that overwrites the
folder can wipe it — exactly the failure mode behind QC #198 and the 2026-06-15 incident.
Pointing `FileStorage:StoragePath` at a path **outside** the deploy folder makes the store
survive deployments without relying on a manual pre-deploy backup.

## Production setup (Plesk / Windows) — do this ONCE

1. Pick a persistent directory **outside** the site's deploy folder, e.g. `C:\GCCData\uploads`.
2. Create it and its two subfolders:
   - `C:\GCCData\uploads\certificates`
   - `C:\GCCData\uploads\images`
3. Grant the application pool identity (IIS AppPool user) **Modify** permission on `C:\GCCData\uploads`.
4. In the server's `appsettings.Production.json`, set:
   ```json
   "FileStorage": { "StoragePath": "C:\\GCCData\\uploads" }
   ```
   (Use double backslashes in JSON. An absolute path is used verbatim.)
5. **Migrate existing files once:** copy the current contents of
   `<deployfolder>\App_Data\uploads\certificates` and `...\images` into the new
   `C:\GCCData\uploads\...` folders. The resolver's App_Data fallback keeps old files
   reachable until this copy is done, so there is no downtime window.
6. Recycle the app pool and verify a known certificate downloads and previews.

## Deployment rules (every publish) — DO NOT BREAK THESE

- **Never** delete, overwrite, or recreate the persistent upload directory
  (`FileStorage:StoragePath`) during a deploy. It is intentionally outside the deploy folder.
- If `FileStorage:StoragePath` is **empty** (files still under `App_Data/uploads`),
  follow the existing rule: **zip `App_Data/uploads/` before publishing** — a publish can
  overwrite it. (Tracked in the pre-deploy checklist; this is the manual safety net until the
  persistent path above is configured.)
- Confirm `appsettings.Production.json` on the server still has the correct `StoragePath`
  after deploy (publishing can replace `appsettings.*.json` if not excluded).
- Smoke after deploy: open a certificate that has a PDF → preview renders and download succeeds.

## If files go missing again

1. Check the app logs for the `Certificate PDF missing on disk: cert {Number} ...` warning —
   it names every affected cert and the path it looked for.
2. Restore the upload directory from backup.
3. Re-run `Scripts/smoke-prod/prod-missing-pdf-sweep.js` to confirm 0 missing files.

## Related

- DEF-208 — moved uploads out of `wwwroot` (security).
- DEF-262 / DEF-264 / DEF-279 — fixed individual resolvers that drifted; DEF-286 centralised
  them into `IUploadPathResolver` so that bug class can't recur.
- `docs/tracking/defect-register.md` → DEF-286.
- 2026-06-15 incident — prod upload store wiped; recovery from backup.

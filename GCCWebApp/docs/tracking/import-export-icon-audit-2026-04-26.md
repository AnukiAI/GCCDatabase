# Import/Export Icon Audit — 2026-04-26

## Convention (per Shawn)

| Action | Direction | Recommended FA6 icon |
|--------|-----------|----------------------|
| **Import** (data into database) | Arrow **down** | `fa-file-arrow-down` (or `fa-download`) |
| **Export** (data out of database) | Arrow **up** | `fa-file-arrow-up` (or `fa-upload`) |

The currently-used `fa-file-import` and `fa-file-export` icons render with **right-pointing** arrows (file with arrow going *into* / *out of* it horizontally). That's what Shawn flagged on `/Certificate/CertificateTileViewInternal`.

---

## Severity Summary

- **Violations (right-arrow icons)**: 36 occurrences across 18 files — every Import/Export button in the app
- **Compliant**: 25+ occurrences using `fa-download` (down) for download actions and `fa-upload` (up) for upload actions
- **Out of scope** (brand/non-directional, no fix needed): `fa-file-excel`, `fa-file-csv`, `fa-cloud-upload-alt` on import drop-zones (user IS uploading the file → up makes sense)

---

## Violations — `fa-file-import` (should be `fa-file-arrow-down`)

| # | File | Line | Context |
|---|------|------|---------|
| 1 | `Views/Certificate/CertificateTileViewInternal.cshtml` | 254 | "Import" toolbar button — **the one Shawn flagged** |
| 2 | `Views/Certificate/CertificateTileViewInternal.cshtml` | 419 | Import modal title bar |
| 3 | `Views/Company/Management.cshtml` | 25 | "Import" toolbar button |
| 4 | `Views/Company/Management.cshtml` | 112 | Import preview modal title |
| 5 | `Views/Company/Index.cshtml` | 38 | "Import" toolbar button |
| 6 | `Views/Company/Index.cshtml` | 96 | Import preview modal title |
| 7 | `Views/Product/Management.cshtml` | 39 | "Import" toolbar button |
| 8 | `Views/Product/Management.cshtml` | 141 | Import preview modal title |
| 9 | `Views/Product/ManagementV2.cshtml` | 28 | "Import" toolbar button |
| 10 | `Views/Product/ManagementAlt.cshtml` | 28 | "Import" toolbar button |
| 11 | `Views/Product/Categories.cshtml` | 18 | "Import" toolbar button |
| 12 | `Views/Shared/_ProductIdManager.cshtml` | 23 | "Import" Product IDs button |
| 13 | `Views/Shared/_ProductIdManager.cshtml` | 276 | Import preview modal title |
| 14 | `Views/Admin/ProductManagement.cshtml` | 37 | "Import" toolbar button |
| 15 | `Views/Admin/ProductManagement.cshtml` | 116 | Import preview modal title |
| 16 | `Views/Admin/UserManagement.cshtml` | 37 | "Import" toolbar button |
| 17 | `Views/Admin/UserManagement.cshtml` | 137 | Import preview modal title |
| 18 | `Views/Admin/Tags.cshtml` | 36 | "Import" toolbar button |
| 19 | `Views/Admin/Tags.cshtml` | 138 | Import preview modal title |

**Total: 19 occurrences**

---

## Violations — `fa-file-export` (should be `fa-file-arrow-up`)

| # | File | Line | Context |
|---|------|------|---------|
| 1 | `Views/Company/Management.cshtml` | 28 | "Export" toolbar button |
| 2 | `Views/Company/Index.cshtml` | 41 | "Export" toolbar button |
| 3 | `Views/Product/Management.cshtml` | 42 | "Export" toolbar button |
| 4 | `Views/Product/ManagementV2.cshtml` | 31 | "Export All" button |
| 5 | `Views/Product/ManagementAlt.cshtml` | 31 | "Export Selected" button |
| 6 | `Views/Product/ManagementAlt.cshtml` | 34 | "Export All" button |
| 7 | `Views/Product/Categories.cshtml` | 21 | "Export" button |
| 8 | `Views/Shared/_ProductIdManager.cshtml` | 26 | "Export" Product IDs button |
| 9 | `Views/Admin/ProductManagement.cshtml` | 40 | "Export" button |
| 10 | `Views/Admin/UserManagement.cshtml` | 40 | "Export" button |
| 11 | `Views/Admin/Tags.cshtml` | 39 | "Export" button |
| 12 | `Views/Admin/Partners.cshtml` | 35 | "Export" button |
| 13 | `Views/Admin/CertificateTypes.cshtml` | 63 | "Export" button |
| 14 | `Views/Admin/CertificateFields.cshtml` | 34 | "Export" button |
| 15 | `Views/Admin/CSFAttributes.cshtml` | 32 | "Export to CSV" button |
| 16 | `Views/Admin/Pathways.cshtml` | 33 | "Export" button |
| 17 | `Views/Admin/ProductTypes.cshtml` | 64 | "Export" button |
| 18 | `Views/Admin/RoleManagement.cshtml` | 33 | "Export" button |

**Total: 18 occurrences**

---

## Compliant Icons (no change needed)

| Icon | Direction | Used for |
|------|-----------|----------|
| `fa-download` (down) | ↓ | Certificate PDF download buttons, "Download Selected", template download links, dashboard quick-stat downloads |
| `fa-upload` (up) | ↑ | "Upload Document" buttons (analyst uploads certificate PDF), "Upload & Preview" import-step button |
| `fa-cloud-upload-alt` (up cloud) | ↑ | Drag-and-drop upload zones inside import modals |
| `fa-file-download` (down) | ↓ | Template download links (e.g., CertificateTileViewInternal line 257) |
| `fa-file-excel` | brand | "Export All to Excel" — Excel logo, not directional |

---

## Recommended Fix

Global find/replace, two pairs:

```
fa-file-import  →  fa-file-arrow-down
fa-file-export  →  fa-file-arrow-up
```

Both replacement icons exist in FontAwesome 6 Free Solid (the `fas` family already in use).

**Verification needed**: confirm `fa-file-arrow-down` and `fa-file-arrow-up` are bundled in the project's FA build before bulk-replacing — older FA5-style projects may need `fa-file-download` / `fa-file-upload` instead (those exist in both FA5 and FA6 Free).

---

## Notes

- Drop zones inside import modals (`fa-cloud-upload-alt`) are intentionally "up" because the user is uploading a CSV. Don't change those.
- "Download Template" links use `fa-file-download` (correct — down arrow for download).
- The bulk fix is mechanical and low-risk (icon swap only, no markup or behavior change). Recommend doing it as one commit so the convention lands consistently.

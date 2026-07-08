# GCC Web App — Coverage Matrix (Phase 0)

> **Scope of this pass:** Phase 0 discovery only (per staged agreement). This matrix cross-references
> **Views/Actions × Docs × Code × Existing Tests**. No new test code has been written yet — the
> `Test?` column reflects the *existing* `Scripts/smoke-prod/` framework and `GCCWebApp.Tests/`.
> Phase 1 (live read-only prod walkthrough) and Phase 2 (spec authoring) await approval.
>
> **Legend** — `✔` present · `✘` absent · `~` partial/indirect · `n/a` not applicable
> **Test source:** JS specs live in `Scripts/smoke-prod/_views/`; .NET in `GCCWebApp.Tests/`.
> **Flags:** 🟥 = **Docs ✔ / Code ✘ or blocked** (documented-not-built) · 🟦 = **Code ✔ / Docs ✘** (built-not-documented)
>
> Generated: 2026-07-02 · Inventories: 25+ controllers, ~45 views, ~130 server actions, 12 per-view smoke specs.

---

## 1. Certificate — Tile / Browse Views

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| CertificateTileView (Public) | Browse grid / infinite scroll | ✔ | ✔ | ✔ `cert-tile-public.js` | Public, PublicStatuses only |
| CertificateTileView (Public) | Search + autocomplete | ✔ | ✔ | ~ | `/api/certificate-tiles/autocomplete`; search normalized (CR-187, unit-tested) — no filter-interaction E2E |
| CertificateTileView (Public) | Filter (type/category/company/country/tags) | ✔ | ✔ | ~ | Presence asserted; filter *application* not E2E-tested |
| CertificateTileView (Public) | Sort dropdown (7 modes) | ✔ | ✔ | ✘ | No sort-order assertion |
| CertificateTileView (Public) | Clear filters | ✔ | ✔ | ✘ | |
| CertificateTileView (Public) | Generate shareable link | 🟦 ✘ | ✔ | ✘ | `POST /api/certificate-tiles/share-link` — **built, not documented** |
| CertificateTileView (Public) | Download cert PDF | ✔ | ✔ | ✘ | `/api/certificate-tiles/{id}/download` |
| CertificateTileViewExternal | Browse grid (company/partner-scoped) | ✔ | ✔ | ✔ `cert-tile-external.js` | Internal tags hidden, no Add btn asserted |
| CertificateTileViewExternal | Search / filter / sort / share-link / download | ~ | ✔ | ~ | Same sub-actions as public, external-scoped; share-link undocumented 🟦 |
| CertificateTileViewInternal | Browse grid (all data) | ✔ | ✔ | ✔ `cert-tile-internal.js` | Add btn + internal-tag col + admin import gating asserted |
| CertificateTileViewInternal | Status & partner filters (admin-only) | ✔ | ✔ | ~ | `/api/certificate-tiles-internal/filters` |
| CertificateTileViewInternal | Search / sort / share-link / download | ~ | ✔ | ~ | share-link undocumented 🟦 |
| All tile views | Workflow-state visibility per role | ✔ | ✔ | ✔ `cert-state-matrix.js` | 8 states × 3 roles seeded/asserted |

## 2. Certificate — Detail Views

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| CertificateInternalView/{number} | View details (full metadata) | ✔ | ✔ | ✘ | No per-view detail spec (access-matrix only) |
| CertificateInternalView | Zoom image / download PDF / toggle product IDs | ~ | ✔ | ✘ | JS: `openImageZoom`, `downloadCertificate`, `togglePidShowAll` |
| CertificateInternalView | Edit / Delete (gated by CanEdit/CanDelete) | ✔ | ✔ | ✘ | Delete → `POST /Certificate/Delete/{number}` |
| CertificateExternalView/{number} | View details (external-visible fields) | ✔ | ✔ | ✘ | No detail spec |
| CertificateSingleView/{number} | Public single-cert view (status-gated) | ✔ | ✔ | ✘ | No detail spec |
| Verify/{number} | Public verification redirect | ~ | ✔ | ✘ | 🟦 route lightly documented |

## 3. Certificate — Create / Edit / Import

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| Create (GET/POST) | Full create form loads (sections/dropdowns) | ✔ | ✔ | ✔ `cert-create.js` | Presence-level only |
| Create | Company→Product→Category→Type→Subcat cascade | ✔ | ✔ | ~ | `cert-product-*.js` one-offs; not in Tier 1 |
| Create | Certification Results field (CR-001) | ✔ | ✔ | ✔ | Field presence asserted |
| Create | Partner multi-select | ✔ | ✔ | ~ | `cr171-*.js` one-offs |
| Create | Product ID Manager (add/edit/delete rows) | ✔ | ✔ | ~ | `POST/PUT/DELETE /api/productids`; presence only |
| Create | Upload PDF (drag-drop, 10MB) | ✔ | ✔ | ✘ | Not exercised in smoke |
| Create | Internal/External tag pills | ✔ | ✔ | ✘ | |
| Create | Save / Save & Add Another / Reset / Cancel | ✔ | ✔ | ✘ | No CRUD round-trip test |
| Create | Date cross-field validation | ✔ | ✔ | ~ | `cert-date-range-validation.js` one-off |
| Edit (GET/POST) | Edit form pre-populated | ✔ | ✔ | ✔ `cert-edit.js` | cert# readonly, product re-select (DEF-225) |
| Edit | View / Remove / Replace PDF | ✔ | ✔ | ✘ | `DELETE /api/certificates/{id}/pdf` |
| Edit | Delete (Admin-only) | ✔ | ✔ | ✘ | |
| Create2 / Create Alt | Cache-bypass test form | 🟦 ✘ | ✔ | ✘ | 🟦 dev scaffold, undocumented — candidate for removal |
| Import (CSV) | Upload → preview → confirm | ✔ | ✔ | ~ | `def109-import-e2e-verify.js` one-off; not Tier 1 |
| Import | Download template / history / undo | ✔ | ✔ | ✘ | Undo = CR-023 |

## 4. Dashboard

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| Dashboard/Index | Stat cards + breakdown tables render | ✔ | ✔ | ✔ `dashboard.js` | Role-gated internal-only breakdowns |
| Dashboard | GetChartData (role-based) | ✔ | ✔ | ~ | Charts asserted present |
| Dashboard | Download stat / table / reports (CSV) | ✔ | ✔ | ✘ | `/Dashboard/DownloadStat`, `DownloadTable`; also `ExportController` |

## 5. Product / Company Management

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| Product/Management | Table + role-gated Add/Actions | ✔ | ✔ | ✔ `product-mgmt.js` | External hidden asserted |
| Product/Management | Filters (category/company/certs) + clear | ✔ | ✔ | ✘ | |
| Product/Management | Import / Export / Export active product IDs | ✔ | ✔ | ✘ | CR-184 global export |
| Product/Management | Save / Delete product | ✔ | ✔ | ✘ | No CRUD round-trip |
| Product/ManagementV2, ManagementAlt | Alternate UIs | 🟦 ✘ | ✔ | ✘ | 🟦 undocumented duplicates — consolidation candidate |
| Company/Index, Management | List / search / add / edit / delete | ✔ | ✔ | ✘ | No spec |
| Company/Index | CSV import (preview modal) / export / template | ✔ | ✔ | ✘ | `POST /api/companies/import` |
| Company/Create | Create company form | ✔ | ✔ | ✘ | |

## 6. Admin Pages

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| Admin/UserManagement | Table + Template/Import/Export/Add (DEF-232) | ✔ | ✔ | ✔ `admin-user-mgmt.js` | |
| Admin/UserManagement | Edit / Delete / Activate / Resend invite | ✔ | ✔ | ✘ | CRUD not round-tripped |
| Admin/EditUser | Edit user (role/company/2FA/password) | ✔ | ✔ | ✘ | |
| Admin/Tags | Table + Template/Import/Export/Add + type filter | ✔ | ✔ | ✔ `admin-tags.js` | |
| Admin/Tags | Edit / Delete tag | ✔ | ✔ | ✘ | |
| Admin/Partners | Export + Add (no Template/Import) | ✔ | ✔ | ✘ | Add/edit/delete inline; `/api/partners` |
| Admin/CertificateTypes | Tree/table, add/edit/delete, expand, export | ✔ | ✔ | ✘ | Hierarchical (Level+ParentId) |
| Admin/ProductManagement | Product list + bulk import | ✔ | ✔ | ~ | Overlaps Product/Management |
| Admin/RoleManagement | Role/permission CRUD | ✔ | ✔ | ✘ | Fine-grained perms under-documented |
| Admin/CertificateFields | Custom field CRUD | ✔ | ✔ | ✘ | |
| Admin/CSFAttributes | CSF attribute mgmt | ~ | ✔ | ✘ | 🟦 thinly documented |
| Admin/ProductTypes | Product type hierarchy | ✔ | ✔ | ✘ | |
| Admin/Pathways | Pathway mgmt (Amazon CPF) | ~ | ✔ | ✘ | |
| Admin/GoogleAnalytics | GA config | ✔ | ✔ | ✘ | |
| Admin/GCCAPI, PartnerAPI | API key/docs surfaces | ✔ | ✔ | ✘ | |
| Admin/CreateUser | **Mock UI only (no backend)** | 🟦 ✘ | ✔ | ✘ | 🟥/🟦 UI mockup shipped in app — remove or gate |

## 7. Account / Auth

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| Account/SignIn + Login | Login form + submit | ✔ | ✔ | ✔ `login.js` | Single-h1 (DEF-104) asserted |
| Account/ForgotPassword | Request reset email | ✔ | ✔ | ✘ | 🟥 **email blocked in prod (SMTP unconfigured, CR-009)** |
| Account/ResetPasswordWithToken | Reset via token | ✔ | ✔ | ✘ | Token flow |
| Account/SetupTotp + EnableTotp/DisableTotp | TOTP 2FA setup | ✔ | ✔ | ✘ | |
| Account/TwoFactorAuthentication + Verify2FA/Resend2FA | 2FA challenge | ✔ | ✔ | ✘ | |
| Account/TwoFactorPreferences | Recovery codes / regenerate | ✔ | ✔ | ✘ | |
| Account/EditProfile | Edit own profile | ✔ | ✔ | ✘ | |
| Account/SignUp, VerifyEmail, ForcePasswordReset | Registration/verify | ✔ | ✔ | ✘ | |
| Account/SessionExpired, AccountLocked, AccessDenied | Error/state pages | ✔ | ✔ | ✘ | |
| Account/TestAuth | Dev-only auth diagnostic | 🟦 ✘ | ✔ | ✘ | 🟦 denied in prod; undocumented |
| Duo Push MFA | Duo Push challenge | 🟥 ✔ (CR-010) | ✘ | ✘ | 🟥 **documented (approved), not built** — TOTP only |

## 8. Home / Misc

| View | Action | In Docs? | In Code? | Test? | Status / Notes |
|------|--------|:---:|:---:|:---:|---|
| Home/Index | Landing + role redirect | ✔ | ✔ | ✔ `home.js` | |
| Home/Privacy, Error | Static pages | ~ | ✔ | ✘ | |
| Document/Upload | Upload doc (type/desc/public flag) | ✔ | ✔ | ✘ | `POST /Document/Upload` |
| Document/Download, Preview, Delete | Doc lifecycle | ✔ | ✔ | ✘ | Delete = AdminOnly |
| Test/ProductTypes | Dev test view | 🟦 ✘ | ✔ | ✘ | 🟦 dev scaffold |

## 9. API Endpoints (server-to-server / AJAX)

| Area | Endpoints | In Docs? | In Code? | Test? | Status / Notes |
|------|-----------|:---:|:---:|:---:|---|
| Partner API v1 (certificates) | GET list/{id}/number/search/expiring/productid/download | ✔ | ✔ | ~ | `amazon-partner-api-local.js` + contract-diff one-offs; **contract immutable** |
| Partner API v1 (certificates) | POST/PUT/DELETE, bulk (create/update/delete/validate ≤100) | ~ | ✔ | ✘ | 🟦 write/bulk endpoints thinly documented |
| API v1 Auth | login / refresh / validate (Bearer) | ✔ | ✔ | ✘ | |
| API v1 ApiKeys | list/create/revoke | ✔ | ✔ | ✘ | |
| API v1 Products | CRUD + search | ✔ | ✔ | ✘ | |
| API v1 Metrics | get/summary/reset/record | 🟦 ✘ | ✔ | ✘ | 🟦 undocumented |
| Internal certs API | CRUD, upload, thumbnail, download, export, remove-pdf | ✔ | ✔ | ~ | |
| ProductId API | CRUD, bulk, import, export, validate, sample-csv | ✔ | ✔ | ~ | CR-018 paging tested via one-offs |
| Partner admin API | partner apikeys, usage stats | ✔ | ✔ | ✘ | |
| **ETLController** | RunFull/Incremental, jobs, alerts, extract, export, dashboard | 🟦 ✘ | ✔ | ✘ | 🟥/🟦 **large subsystem, not in reviewed docs** — confirm in/out of scope |

---

## 10. Flagged Gap Summary (feeds gap-report.md)

### 🟥 Documented / expected but NOT built (or blocked)
- **Forgot-Password email** — built but **blocked in prod** (SMTP unconfigured, CR-009 / QC #150). Highest-impact.
- **Duo Push MFA** (CR-010) — approved, not built (TOTP works).
- **Custom Field CSV export** (CR-012) & **sort/filter** (CR-014) — approved, no build.
- **Partner Role creation UI** (CR-008) — data model exists, UI not built.
- **Partner custom-fields exposure** (CR-013) — gated on Amazon sign-off.

### 🟦 Built but NOT documented
- Shareable-link endpoints on all 3 tile views (`share-link`).
- **ETLController** (11 endpoints) — entire subsystem absent from reviewed docs.
- API v1 **Metrics** controller; **bulk** certificate write endpoints.
- Dev scaffolds shipped in app: `Create2`/Create Alt, `Product/ManagementV2` & `ManagementAlt`, `Admin/CreateUser` (mock UI), `Account/TestAuth`, `Test/ProductTypes`.

### ⬜ Coverage gaps (built + documented, but NO automated test)
- All **detail views** (internal/external/public single cert).
- **CRUD round-trips** (cert/user/product/company/tag create→edit→delete) — only "form loads" is asserted.
- **Import/Export** end-to-end (only `def-*` one-offs, not systematized).
- **Filter application & sort order** (presence asserted, behavior not).
- **Auth flows** (2FA, forgot/reset password, signup, lockout).
- **Document** upload/download/delete.
- Most **Admin pages** (only user-mgmt + tags have specs; 12+ admin views uncovered).
- **API layer** broadly (partner API read has one-offs; write/bulk/ETL/metrics untested).

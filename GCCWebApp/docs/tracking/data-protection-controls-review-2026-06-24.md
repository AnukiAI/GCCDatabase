# Data Protection Controls Review — 2026-06-24

**Type:** Internal audit (read-only; no code changed).
**Scope:** Where users can delete / overwrite / corrupt the wrong data, and where audit trail, soft-delete, or recovery is missing.
**Stack confirmed:** ASP.NET Core 8 MVC + EF Core 9 + SQL Server.
**Decision (Shawn, 2026-06-24):** deleted **certificates must be recoverable** (soft-delete, not permanent).

**Registered as:** DEF-288 (certificate deletion), DEF-289 (document/PDF deletion), DEF-290 (admin-entity delete consistency). Lower-priority items (R5/R7) captured here as notes.

---

## What already exists (good)

- **Audit infrastructure is strong** — `AuditHistory` table + `Services/Audit/AuditService.cs`: field-level diff on UPDATE, full-entity JSON snapshot on CREATE/DELETE, IP / user-agent / session capture, `ChangeSource` (Web UI / API / Import / Migration), `PasswordHash` redaction. Supports DELETE with a recoverable JSON snapshot in `OldValue`.
- **Status history** — `CertificateStatusHistory` (separate table).
- **Import history** — `ImportBatch` + `AuditHistory.ImportBatchId`.
- **Partial soft-delete** — `IsDeleted` on **User** and **Pathway** only (migrations `FixUserUniqueIndexesForSoftDelete`, `FixPathwayUniqueIndexForSoftDelete`).
- **FK delete behavior** — most relationships `OnDelete(Restrict/NoAction)` in `ApplicationDbContext.cs` (blocks orphaning).

**The core gap:** the audit *infrastructure* exists but is **not wired into the destructive paths**, and the core business entity (Certificate) and uploaded PDFs have **no recovery path**.

---

## Risk-ranked inventory

| # | Risk | Surface (file) | Safeguards present | Missing | Severity | DEF |
|---|------|----------------|--------------------|---------|----------|-----|
| R1 | Bulk certificate **hard-delete**, no audit, no confirm | `Controllers/Api/v1/BulkCertificatesController.cs:236` (`BulkDelete`) | AnalystOrAdmin auth (DEF-131 blocks Partner keys); 100-cap | `Remove()` hard-delete; no audit row (no `IAuditService` injected); no soft-delete; no typed confirm; cascades `CertificateStatusHistory` | 🔴 Critical | DEF-288 |
| R2 | Single certificate **hard-delete**, no audit | `Controllers/CertificateController.cs:1591` (`DeleteConfirmed`) | AdminOnly, antiforgery, GET confirm page | `RemoveAsync` hard-delete; only `_logger.LogInformation` — no `AuditHistory` row; no recovery | 🔴 Critical | DEF-288 |
| R3 | Document delete **physically destroys the PDF** | `Controllers/DocumentController.cs:231` → `Services/FileUploadService.cs:220` (`File.Delete`) | Row soft-deletes (`IsActive=false`); AdminOnly | PDF hard-deleted from disk (irreversible); `GetCurrentUserId()` returns `null` → no attribution, no audit | 🔴 Critical | DEF-289 |
| R4 | Certificate has **no soft-delete** at all | `Models/Certificate.cs` | — | No `IsDeleted` / query filter on the core business entity; every delete path is permanent | 🔴 Critical | DEF-288 |
| R5 | Import **overwrites** existing certs in bulk | `Services/Import/CertificateImportService.cs:1449` (`UpdateCertificateAsync`) | ✅ Preview/confirm flow; ✅ audits each update (`:1539`); ✅ blank cell = no-op (`:1237`) | No batch-level snapshot/rollback; wrong file matched on cert# overwrites many live certs (mitigated by audit + preview) | 🟠 High | note only |
| R6 | Admin entity deletes: **inconsistent** soft-delete + audit | `Controllers/AdminController.cs` (10 delete actions) + `Services/AdminService.cs` | Most FKs `Restrict/NoAction`; `AdminService` injects `IAuditService`; User/Pathway soft-delete | Soft-delete entity-by-entity (only User/Pathway); per-action audit coverage not uniformly verified | 🟠 High | DEF-290 |
| R7 | Physical file **overwrite** on import attach | `Services/Import/CertificateImportService.cs:1651` (`File.Copy(..., overwrite: true)`) | — | Silently overwrites a stored file on name collision | 🟡 Medium | note only |

---

## Recommended fixes

### Certificate recovery (R1/R2/R4 → DEF-288)
1. **Soft-delete the Certificate** — add `IsDeleted` / `DeletedAt` / `DeletedBy` + a global query filter, matching the existing User/Pathway pattern. Converts both delete paths from irreversible to recoverable.
2. **Wire audit into both delete paths** — `await _auditService.LogEntityChangeAsync(..., AuditAction.Delete, oldEntity, null, ...)` before `SaveChanges`. The existing `AuditHistory` table already snapshots the full entity JSON — **no new table needed.**
3. **Bulk confirm** — `BulkDelete` should require a typed count confirmation in the UI; also confirm whether the endpoint should be **disabled entirely** (DEF-131 notes no repo code calls it).
4. Re-point the `CertificateStatusHistory` cascade so soft-deleting a cert doesn't lose its history (it currently hard-cascades on real delete — `ApplicationDbContext.cs:753`).

### PDF protection (R3 → DEF-289)
1. **Stop physically deleting the PDF** — the row already soft-deletes (`IsActive=false`); drop the `DeleteFileAsync` call, or move the file to a `_deleted/` quarantine folder so a mis-click can't destroy the artifact. Directly relevant to [[incident_prod_all_pdfs_missing_2026-06-15]].
2. **Fix attribution** — `DocumentController.GetCurrentUserId()` returns `null`; wire it to the `NameIdentifier` claim and write an audit row.

### Admin-entity consistency (R6 → DEF-290)
Audit each of the 10 `AdminController` delete actions: confirm (a) an `AuditHistory` DELETE row is written, and (b) a soft-delete vs. hard-delete decision is made deliberately per entity (currently only User/Pathway recover).

---

## Test cases (currently absent — see [[issue_testing_gaps]])

1. Delete a cert → assert an `AuditHistory` DELETE row exists with the JSON snapshot, and the cert is hidden from lists but recoverable.
2. Bulk-delete 3 certs → assert 3 audit rows + all 3 soft-deleted (none physically gone).
3. Document delete → assert the PDF is still retrievable (quarantine), not `File.Delete`d, and an attributed audit row exists.
4. Import a wrong-cert# file → assert overwrite is audited and blank cells did not null existing data.
5. Delete a referenced `ProductType` → assert `Restrict` blocks it with a friendly error, not a 500.

## Open questions

1. **Bulk API** — disable `BulkDelete` entirely (no repo caller per DEF-131) or harden + keep?
2. **Retention** — how long should soft-deleted certs / quarantined PDFs be retained before a purge job hard-deletes them?
3. **Restore UI** — does Admin need a "Recover deleted" screen, or is DB-level restore (set `IsDeleted=false`) acceptable for now?

## Linked memory
[[incident_prod_all_pdfs_missing_2026-06-15]], [[feedback_backup_uploads_before_deploy]], [[feedback_scoping_check_adjacent_endpoints]], [[issue_testing_gaps]], [[feedback_track_every_fix_in_defect_register]].

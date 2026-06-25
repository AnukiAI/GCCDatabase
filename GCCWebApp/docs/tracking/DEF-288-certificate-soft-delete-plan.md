# DEF-288 — Implementation Plan: Certificate soft-delete + delete auditing

**Status:** Plan only — no code written. Awaiting sign-off on the two design decisions below.
**Decision already made (Shawn, 2026-06-24):** deleted certificates must be **recoverable**.
**Source:** `docs/tracking/data-protection-controls-review-2026-06-24.md` (R1/R2/R4).

## Objective
Make every certificate-delete path **recoverable** and **audited**:
1. `CertificateController.DeleteConfirmed` (single, AdminOnly) — `:1591`
2. `BulkCertificatesController.BulkDelete` (up to 100, AnalystOrAdmin) — `:236`

Currently both hard-delete via EF `Remove`, write no `AuditHistory` row, and cascade-wipe `CertificateStatusHistory` (`ApplicationDbContext.cs:753`). The `Certificate` entity has no soft-delete field.

---

## Design decision #1 — how to hide soft-deleted certs (NEEDS SIGN-OFF)

The certificate read surface is large: certificate repository (list/paged/by-number/advanced-filter), 3 tile APIs (public/internal/external), the v1 Partner API, `ExportController`, dashboard counts. Two options:

| | **Option A — EF global query filter** (recommended) | **Option B — manual `.Where(!IsDeleted)`** (convention-consistent) |
|---|---|---|
| How | `modelBuilder.Entity<Certificate>().HasQueryFilter(c => !c.IsDeleted)` — one line hides deleted certs from **every** query automatically | Add `&& !c.IsDeleted` at each of the ~10+ read sites, matching how User/Company/Partner are filtered today |
| Pro | Can't miss an endpoint — kills the [[feedback_scoping_check_adjacent_endpoints]] miss-class that caused DEF-103/164/198/204/205 | Matches existing house pattern exactly (no new EF concept) |
| Con | **Diverges from convention** (project uses zero global query filters today); restore/admin paths must call `.IgnoreQueryFilters()`; EF may warn on required-nav includes | Easy to miss a read path — exactly the recurring defect class; every future cert query must remember the filter |

**Recommendation: Option A.** The certificate read surface is too wide and too partner-facing to rely on remembering a manual filter at every call site; a leaked-but-deleted cert in the Partner API or export is a contract risk. The convention divergence is worth it and should be documented in `.claude/CLAUDE.md` as "Certificate uses a global soft-delete query filter; other entities filter manually." If you'd rather stay strictly consistent, Option B is viable but raises long-term miss risk.

> The rest of this plan assumes **Option A**. Switch the "read-path" step for Option B's checklist if chosen.

## Design decision #2 — bulk endpoint (NEEDS SIGN-OFF)
`BulkCertificatesController.BulkDelete` has **no repo caller** (per DEF-131). Choose:
- **(a) Harden** — convert to soft-delete + audit + typed-count confirm (more work, keeps capability).
- **(b) Disable** — return 410/remove the action; soft-delete only via the single-cert UI path (less surface, less work).

Recommend **(b) disable** unless there's a known need for bulk delete — it removes a 100-record blast-radius endpoint that nothing uses. If kept, harden per below.

---

## Schema changes

`Models/Certificate.cs` — add (mirrors `User`/`Company`/`Partner`):
```csharp
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAt { get; set; }
public int? DeletedBy { get; set; }          // FK to Users — attribution (User only has DeletedAt; we add DeletedBy)
```

`Data/ApplicationDbContext.cs`:
- **Filtered unique index** — change `entity.HasIndex(e => e.Number).IsUnique();` (`:68`) to `.IsUnique().HasFilter("[IsDeleted] = 0");` so a soft-deleted cert number can be re-used (same fix as `FixUserUniqueIndexesForSoftDelete`).
- **(Option A) Global query filter** — `entity.HasQueryFilter(c => !c.IsDeleted);` in the `Certificate` config block (`:65`).
- **Cascade** — no change needed. With soft-delete the cert row is never physically removed, so the `CertificateStatusHistory` Cascade (`:753`) never fires and history is preserved automatically. (A future hard-purge job would still cascade-clean correctly.)

## Migration
- `dotnet ef migrations add AddCertificateSoftDelete` → adds the 3 columns + rebuilds `IX_Certificates_Number` as a filtered unique index. Defaults: `IsDeleted = 0`, so existing rows are unaffected.
- **Gate-7 prod SQL** (prod never auto-migrates — [[project_predeploy_agent_checklist]]): hand-author `Scripts/data-ops/2026-06-DD-AddCertificateSoftDelete-PROD.sql` (idempotent: `ALTER TABLE` add columns if not exists, drop+recreate the filtered index, stamp `__EFMigrationsHistory`). Run in the same deploy step as the code.

## Code changes

**`Controllers/CertificateController.cs` — `DeleteConfirmed` (`:1591`)**
Replace the `RemoveAsync` + log with soft-delete + audit (the controller already injects `_auditService` `:30/57` and has `GetCurrentUserId()` `:1843` / `GetCurrentUsername()` `:1880`):
```csharp
var before = /* snapshot for audit */;
certificate.IsDeleted = true;
certificate.DeletedAt = DateTime.UtcNow;
certificate.DeletedBy = GetCurrentUserId();
await _unitOfWork.CompleteAsync();
await _auditService.LogEntityChangeAsync(
    certificate.Id.ToString(), certificate.Number, AuditAction.Delete,
    before, null, GetCurrentUserId() ?? 0, GetCurrentUsername(),
    reason: null, changeSource: AuditChangeSource.WebUI);
```
Do **not** call `_certificateRepository.RemoveAsync` (it does `_dbSet.Remove` — hard delete, `Repository.cs:58`).

**`Controllers/Api/v1/BulkCertificatesController.cs` — `BulkDelete` (`:236`)** — if hardened (decision #2a):
- Inject `IAuditService` + a way to resolve the acting user (it currently injects only `_context`/`_logger`; check `BaseApiController` for an existing user helper, else add `IHttpContextAccessor`).
- Replace `_context.Certificates.Remove(cert)` with the three soft-delete assignments; write one `AuditHistory` DELETE row per cert (loop) before `SaveChanges`.
- Keep the 100-cap; require the client to send a confirmation flag / typed count.
- If **disabled** (decision #2b): return `410 Gone` (or remove the action) and note in the controller header comment.

**Restore path (recoverability):**
- Minimum: an Admin action `RestoreCertificate(number)` that loads the row with `.IgnoreQueryFilters()` (Option A), sets `IsDeleted=false`/clears `DeletedAt`/`DeletedBy`, writes an `AuditAction.Restore` row. Wire a button on a "Deleted certificates" admin list (or defer the UI and document DB-level restore for now — see review Open Q3).

## Read-path audit (Option A = mostly automatic)
With the global filter, all LINQ queries over `Certificates` exclude deleted rows automatically. **Verify** the few paths that could bypass it:
- Raw SQL / `FromSqlRaw` over Certificates (grep — none expected).
- `IgnoreQueryFilters()` usages (should be only restore/admin).
- The duplicate-number check in `BulkCertificatesController.ValidateBulk` (`:312`) — with the filtered unique index, a soft-deleted number is re-usable; confirm validation reflects that.
- `CertificateImportService` cert-number match (`:1184`) — decide whether importing a number that matches a *soft-deleted* cert should resurrect it or create new (recommend: treat as new; flag in preview).

## Tests (add — see [[issue_testing_gaps]])
1. Delete a cert → `AuditHistory` DELETE row exists w/ JSON snapshot; cert hidden from list/by-number/tile/partner queries; row still in DB with `IsDeleted=1`.
2. Restore → cert reappears; `Restore` audit row written.
3. Re-create a cert with a soft-deleted cert's number → succeeds (filtered unique index).
4. `CertificateStatusHistory` survives a soft-delete (not cascade-wiped).
5. (If kept) `BulkDelete` of 3 → 3 audit rows, all 3 soft-deleted, none physically gone; >100 rejected; missing confirm flag rejected.
6. Partner v1 API + export do **not** return soft-deleted certs.

## Rollout / deploy
- Standard: build + targeted tests on `localhost:5101`, then bundle for Plesk.
- New migration ⇒ **Gate-7** SQL must ship in the same step (prod won't auto-migrate).
- Append to `shipped-2026-06.md` when it ships; update DEF-288 status.

## Risks / notes
- **Global-filter side effects (Option A):** EF can emit warnings when a required navigation points at a filtered entity from an unfiltered one; validate `Include` chains (Company→Certificates etc.) build cleanly. Acceptable and well-trodden, but test the dashboard/tile includes.
- **CR rule:** this changes behavior on live business records (delete becomes reversible) and alters schema — likely warrants a **CR** alongside DEF-288 per [[feedback_data_changes_require_cr]]. Confirm with client before deploy.
- DEF-289 (PDF physical-delete) is **independent** — a deleted cert's PDF is untouched by this change; that's handled separately.

## Effort (rough)
- Schema + migration + Gate-7 SQL: ~0.5 day
- Controller soft-delete + audit wiring (single + bulk): ~0.5 day
- Restore action (no UI) + read-path verification: ~0.5 day
- Tests: ~0.5 day
- **~2 days**, +0.5 day if a full "Deleted certificates" admin recovery UI is wanted.

## Open questions (carry from review)
1. Decision #1: global query filter vs. manual (recommend global).
2. Decision #2: disable vs. harden `BulkDelete` (recommend disable).
3. Retention: should a purge job ever hard-delete soft-deleted certs, and after how long?
4. Restore UI now, or DB-level restore acceptable initially?

## Linked memory
[[feedback_data_changes_require_cr]], [[project_predeploy_agent_checklist]], [[feedback_scoping_check_adjacent_endpoints]], [[issue_testing_gaps]], [[feedback_track_every_fix_in_defect_register]].

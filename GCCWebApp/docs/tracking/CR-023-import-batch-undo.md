# CR-023 — Certificate Import: batch-level undo / rollback ("un-apply this import")

- **Period:** Surfaced 2026-06-24 · Built + UI-validated 2026-06-25
- **Status:** **Built + validated locally (pre-production) — pending deploy.** Implemented at Shawn's direction ("this is backend and we haven't gone live yet — write the CR and fix it"). Build clean; **61/61 unit tests pass**; **15/15 end-to-end UI checks pass** on localhost:5101 (Playwright + SQL ground truth — see Validation below). No commit (Shawn commits). Client email below not sent.

> **Build note — two scope decisions made to keep this self-contained:**
> 1. **Zero schema change.** `ImportBatch` table + `AuditHistory.ImportBatchId` column already exist (migration `20251208051905`), and the undone-marker reuses the existing `ImportBatch.Status` field (new `"UNDONE"` constant). No migration, so **no Gate-7 prod SQL** is required.
> 2. **Create-reversal hard-deletes** the certs the import created (children removed explicitly: product IDs, status history, documents, custom-field values, tag/partner M2M), rather than waiting on DEF-288 soft-delete. Rationale: a cert created seconds ago by a mistaken import has no history to lose — a different risk class than deleting an established cert. Overwritten certs are **restored in place** from the per-field audit, never deleted. Every undo action is itself audited (`AuditAction.Restore` / `AuditAction.Delete`, source `System`).
- **Source:** Agile requirements discussion (2026-06-24) on making certificate import safe. The ask, in the client's framing: *"the import must not fail silently — confirm and warn the user, and preserve history in case an analyst made a mistake importing certificates."* That requirement decomposed into three parts; two are already satisfied and this CR covers the third.

## Background — the three-part requirement and what's already done

| # | Ask | State | Where |
|---|-----|-------|-------|
| 1 | Warn + confirm before a destructive overwrite | ✅ **Built (DEF-292), pending deploy** | Company-reassignment guard: red warning, required acknowledgement checkbox, server-enforced refusal. `CertificateImportService.cs`, `CertificateImportController.cs`, `Preview.cshtml` |
| 2 | Preserve history so a mistake is recoverable | ✅ **Already exists** | Every import overwrite captures the full prior cert state and writes a field-level audit record (`UpdateCertificateAsync` → `_auditService.LogEntityChangeAsync(... AuditAction.Update, oldCertificate, certificate ... AuditChangeSource.Import)`, `CertificateImportService.cs:1469-1546`). Creates and deletes snapshot the whole entity. |
| 3 | **One-click undo of a whole import batch** | ❌ **Not built — this CR** | — |

Today, if an analyst applies the wrong file, the data **is** recoverable — but only by a developer reading `AuditHistory` row-by-row and hand-reversing each change. There is no "undo this import" action for an analyst, and no batch-level rollback. That is the gap CR-023 closes.

## Why this is a CR, not a defect

Per-row audit-on-import and the DEF-292 reassignment guard are defects/fixes against the existing import behavior. A **batch undo feature** is net-new capability that was never in the captured import requirements — the original spec defined import as preview → apply, with no rollback surface. Building an analyst-facing "revert this import" action (new UI, new endpoint, new reversal logic, and the batch-linkage plumbing to support it) is scope expansion, so it is tracked as a CR rather than absorbed as a fix. ([[feedback_data_changes_require_cr]] also applies — a rollback performs UPDATE/DELETE on business records.)

## Current state of the plumbing — honest assessment

The schema *looks* ready but is **not wired**, which materially affects scope:

- ✅ **Per-row audit on import is real.** `AuditService` writes a field-level diff on UPDATE (old→new per field), a full-entity JSON snapshot on CREATE and DELETE, with `ChangeSource = Import`, user/IP/session, and `PasswordHash` redaction. The data needed to reverse a single row exists.
- ⚠️ **`ImportBatch` table exists but is never populated.** `Models/Import/ImportBatch.cs` is marked *"Phase 2"*; there is **no `new ImportBatch(...)`** and no save of it anywhere in `CertificateImportService.cs`. No batch row is created on apply.
- ⚠️ **`AuditHistory.ImportBatchId` column exists but is never set.** `LogEntityChangeAsync` does not even accept an `importBatchId` parameter (`AuditService.cs:38-48`), so every import-sourced audit row currently has `ImportBatchId = null`. There is **no reliable way today to group "all the certs touched by this one import"** without timestamp heuristics.

So the batch-undo feature must first **build the batch linkage**, then **build the reversal**. The "plumbing exists" half-claim in the 2026-06-24 data-protection review (R5) referred to per-row audit only — not to a usable batch grouping.

## Scope (when approved)

### Phase A — establish the batch linkage (prerequisite)
1. **Create + persist one `ImportBatch`** at the start of `ApplyImportAsync` (filename, entity type = Certificate, counts, applying user, timestamp). Populate its result counts at the end.
2. **Thread `ImportBatchId` through audit.** Add an optional `Guid? importBatchId` to `LogEntityChangeAsync` and set it on every audit row written during that apply (`CreateAuditRecord`). Now every cert created/updated by an import is tagged with its batch.
3. Migration: none for the columns (both already exist); a **Gate-7 prod SQL is NOT needed for schema**, but confirm `ImportBatch` table is present on prod (it may have been added by an earlier migration without ever being used) — see open questions.

### Phase B — the reversal
4. **`UndoImportBatchAsync(Guid batchId)`** on the import service. Reads all `AuditHistory` rows for the batch and reverses each, newest-first:
   - **Update rows** → restore the captured `OldValue` per field back onto the certificate.
   - **Create rows** → the cert did not exist before the import; remove it. **Must respect DEF-288** — once certificate soft-delete ships, "remove" = soft-delete, not hard-delete. Until then, this is the strongest argument to sequence CR-023 *after* DEF-288.
   - Re-sync downstream side-effects the import touched: product-ID statuses (CR-015 cascade), cert-level partners (CR-171), `CertificateProductId` rows, and status history.
   - Write a **new audit batch** for the undo itself (so the rollback is itself auditable and, ideally, re-undoable).
5. **Idempotency + guards:** an already-undone batch can't be undone twice; refuse undo if any cert in the batch was edited *after* the import (the captured `OldValue` is stale — warn and require explicit override, mirroring the DEF-292 acknowledgement pattern).

### Phase C — analyst surface
6. **Import history view** — list recent `ImportBatch` rows (file, when, who, counts) with an **Undo** action behind `AdminCore.showConfirmation()` (per [[feedback_data_changes_require_cr]] / UI standards — modal before, toast after; never a native `confirm()`).
7. RBAC: Admin-only initially (import is already Admin-only); decide whether Analyst can undo their own batch.

## Out of scope
- Partial / per-row undo within a batch (all-or-nothing batch reversal only in v1).
- Undo of imports applied *before* this feature ships — those rows have `ImportBatchId = null` and can't be grouped. Recovery for historical mistakes stays manual-from-audit. **Log this limit clearly so it isn't read as "all imports are undoable."**
- Partner API: **no change.** Per [[feedback_amazon_api_immutable]], no `/api/v1/certificates*` shape change. Internal/admin surface only.

## Dependencies / sequencing
- **DEF-288 (certificate soft-delete) should ship first.** Undoing a batch that *created* certs means deleting them; doing that as a hard-delete would reintroduce exactly the unrecoverable-delete risk this whole workstream is trying to remove. Shawn already decided (2026-06-24) certs must be recoverable — CR-023's create-reversal should ride on that soft-delete.
- Builds on DEF-292 (warn/confirm) and the existing `AuditService` import audit.

## Effort estimate
- Phase A (batch linkage): ~0.5 day.
- Phase B (reversal logic + side-effect re-sync + idempotency guards): ~1.5–2 days — the side-effect re-sync (product IDs, partners, status history) is the hard part, not the field restore.
- Phase C (import history view + undo UI + Playwright at 5 viewports): ~1 day.
- **Total ≈ 3–3.5 days**, assuming DEF-288 soft-delete is already in place. Add DEF-288 if it isn't.

## Acceptance criteria (when built)
- Applying an import creates exactly one `ImportBatch`; every cert it created/updated has that `ImportBatchId` on its audit rows.
- An Import History view lists batches with file, timestamp, user, and counts.
- Undoing a batch restores every updated cert to its pre-import field values and removes (soft-deletes) every cert the batch created, plus correctly re-syncs product-ID statuses, partners, and status history.
- The undo is itself audited; a batch cannot be undone twice; a batch with post-import edits warns and requires explicit override before undo.
- No Partner API contract change; no regression to preview/apply or the DEF-292 guard.

## As built (2026-06-25)

- **Batch linkage (Phase A).** `LogEntityChangeAsync` gained an optional `Guid? importBatchId`, stamped on every audit row (`AuditService` + `IAuditService`, `CreateAuditRecord`). `ApplyImportAsync` now creates one `ImportBatch` per apply (Phase 0, inside the transaction so a rollback leaves none), threads its id into the cert create/update audit, and finalizes it with counts + `COMPLETED`/`PARTIAL_SUCCESS` before commit. `CreateCertificateAsync`/`UpdateCertificateAsync` take the batch id. `ApplyImportResult.ImportBatchId` exposes it.
- **Reversal (Phase B).** `UndoImportBatchAsync(batchId, userId, username)` on the import service: loads the batch (refuses if not found / already `UNDONE` / not a completed import); reads the batch's `Certificate` audit rows; restores each overwritten cert's scalar fields from the `UPDATE` rows' `OldValue` (typed switch in `ApplyOldValueToCertificate`), re-syncs CR-015 product-ID statuses, and audits a `Restore`; removes each created cert (from the `CREATE` rows) with children, and audits a `Delete`; marks the batch `UNDONE` with a note. All in one transaction.
- **Surface (Phase C).** `GetImportHistoryAsync` + controller `History` (GET) and `UndoImport` (POST, antiforgery, AdminOnly). New `Views/CertificateImport/History.cshtml` lists batches with an Undo button guarded by `AdminCore.showConfirmation()` (no native `confirm()`); "Import History" link added to the Import page header.
- **Tests.** `CertificateImportServiceTests`: 61/61 pass, including `UndoImportBatchAsync_RestoresOverwritten_AndDeletesCreated`, `_AlreadyUndone_IsRefused`, `_UnknownBatch_ReturnsNotFound`. (Also repaired the pre-existing test-project compile break: the ctor was missing the CR-023 R5 `certificateService` arg at 3 sites, and the CR-023 R5 backup step needed `ImportBackupsDir` on the path stub.)
- **Known limitation (carried from the plan):** the per-field audit captures scalar cert fields only, so a restore reverts Company/Status/dates/identifiers but **not** tag/partner/product-id *collection* changes on an overwritten cert. The CR-023 R5 pre-import backup CSV remains the full-fidelity manual fallback. Surfaced to the user in the History page info banner.

## Validation (2026-06-25, localhost:5101, Playwright as admin + SQL ground truth)

Script: `Scripts/smoke-prod/_cr023-import-undo-validate.js` (screenshots + `results.json` in `Scripts/smoke-prod/_results/cr023/`). **15/15 PASS.**

1. **Arrange** — Import #1 (UI: upload → preview → apply) creates `ZZ-9001` under "CR023 Undo Co A". DB confirms cert + company. ✅
2. **Act** — Import #2 contains `ZZ-9001` → "CR023 Undo Co B" (overwrite + reassignment) **and** new `ZZ-9002`. Preview shows **both** the overwrite and the reassignment acknowledgement boxes; apply reassigns `ZZ-9001`→Co B and creates `ZZ-9002` (DB-confirmed). ✅
3. **Undo** — Import History lists the batch with an Undo control; posting the undo: `ZZ-9001` **restored to Co A**, `ZZ-9002` **removed**, batch `Status=UNDONE`, audit has **1 RESTORE + 1 DELETE** row tagged with the batch id, History shows the **Undone** badge, and the undone batch is **no longer undoable** (idempotent). ✅
4. **Cleanup** — all `ZZ-90xx` fixtures + test companies/products/batches removed; dev DB restored.

**Bug found & fixed during validation:** the initial run showed `RESTORE rows=0` — the data restore worked but wasn't audited, because `AuditService.LogEntityChangeAsync` only handled `Create`/`Update`/`Delete` and silently dropped the `RESTORE` action. Fixed by treating `RESTORE` like `CREATE` (full-entity snapshot in `NewValue`); re-validated → `RESTORE rows=1`. This makes `AuditAction.Restore` a first-class audited action project-wide.

---

## Client-facing draft (review before sending)

**Subject:** One more safety step for certificate imports — "undo an import"

Hi [name],

Following our conversation about making certificate imports safe, I want to confirm one piece before we build it.

We've already done two of the three things you asked for:
1. **The import now warns and makes the analyst confirm** before it would reassign a certificate to a different company (the most dangerous kind of typo) — it can't be applied silently.
2. **Every import already keeps a full before-and-after history** of each certificate it changes, so a mistake can be traced and reversed.

The third piece — a one-click **"undo this whole import"** button for the analyst — isn't built yet. Right now, reversing a bad import is possible but it's a manual job on our side from the history log. The feature would give you an **Import History** screen listing each import (file, who ran it, when, how many certificates), with an **Undo** button that rolls the whole batch back: restoring changed certificates to how they were, and removing the ones that import created.

Two things worth flagging so we sequence it right:
- We'd want this to ride on the **recoverable-delete** work you already approved (so "undo" never permanently destroys anything).
- It would cover imports going forward; imports already run before this ships wouldn't be one-click-undoable (those stay a manual recovery, same as today).

Estimated effort is roughly **3 days** on top of the recoverable-delete work.

Is this worth building now, or would you rather rely on the warning + history we already have and revisit undo later? Either way, I've logged it as **CR-023** so it's tracked.

Thanks!
Shawn

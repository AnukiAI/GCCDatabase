# GCC Project Tracking — Index

**Project:** GCC Database Solution · **Prepared by:** Anuki Consulting
**Role:** Master index of the `docs/tracking/` framework — the entry point to every register, log, plan, and gate.
**Last updated:** 2026-08-17

> This is an **index**, not a metrics snapshot. Counts and statuses live in the linked files (their
> own headers are the source of truth) so this page doesn't go stale. A prior metrics-style dashboard
> (dated 2026-03-25) was retired in a file-cleanup commit; its content remains recoverable in git
> history (`1efa546^:GCCWebApp/docs/tracking/TRACKING-DASHBOARD.md`). Nothing was lost — this replaces
> it with a lower-maintenance index.

---

## Registers (canonical item logs)

| Register | File | Holds |
|---|---|---|
| Defect Register | [defect-register.md](defect-register.md) | DEF-XXX — canonical defect log (current high-water in file header) |
| Change Request Register | [change-request-register.md](change-request-register.md) | CR-XXX — scope/data-op change requests (incl. CR-021 migration CEF code) |
| Issue Register | [issue-register.md](issue-register.md) | ISS-XXX — infrastructure / tooling / test-harness issues that are neither product defects nor scoped changes |

*(`progress-log.md` and `effort-log.md` were consolidated away in earlier cleanups; effort context now
lives in the QA and delivery docs below. `issue-register.md` was **revived 2026-07-13** with ISS-001 and is
current — the note that previously listed it as consolidated away was stale.)*

### Per-CR plan & correspondence docs

Detail behind rows in the [Change Request Register](change-request-register.md). Each links its plan/spec
doc and any paired client/cover email. *(CR-021 is listed under **Migration** below — it's the CEF Amazon-code CR.)*

| CR | Plan / spec doc | Paired correspondence |
|---|---|---|
| CR-016 | [CR-016-bulk-replace-product-ids.md](CR-016-bulk-replace-product-ids.md) | — |
| CR-017 | [CR-017-form-recovery-ux.md](CR-017-form-recovery-ux.md) | — |
| CR-018 | [CR-018-product-id-server-paging.md](CR-018-product-id-server-paging.md) | — |
| CR-019 | [CR-019-product-miscategorization-data-cleanup.md](CR-019-product-miscategorization-data-cleanup.md) | [CR-019-cover-email.md](CR-019-cover-email.md) |
| CR-020 | [CR-020-user-company-field.md](CR-020-user-company-field.md) | — |
| CR-022 | [CR-022-sku-sortable-column.md](CR-022-sku-sortable-column.md) | — |
| CR-023 | [CR-023-import-batch-undo.md](CR-023-import-batch-undo.md) | — |
| CR-171 | [CR-171-multipartner-visibility.md](CR-171-multipartner-visibility.md) | [CR-171-client-email.md](CR-171-client-email.md) |
| CR-184 | [CR-184-product-identifiers-global-export.md](CR-184-product-identifiers-global-export.md) | [CR-184-171-client-email.md](CR-184-171-client-email.md) |
| CR-186 | [CR-186-product-identity-name-category-type-subcategory.md](CR-186-product-identity-name-category-type-subcategory.md) · [CR-186-split-gcc-note.md](CR-186-split-gcc-note.md) | [CR-186-client-email.md](CR-186-client-email.md) |
| CR-187 | [CR-187-normalized-search.md](CR-187-normalized-search.md) | — |

### Per-DEF plan docs

Detail behind rows in the [Defect Register](defect-register.md). Only defects that warranted a
standalone plan/design doc are listed; all others live inline in the register.

| DEF | Plan doc |
|---|---|
| DEF-288 | [DEF-288-certificate-soft-delete-plan.md](DEF-288-certificate-soft-delete-plan.md) — cert soft-delete design (parked; likely a CR) |

## Migration (source-data → GCC database)

| Doc | File | Purpose |
|---|---|---|
| **Decisions answer tracker** | [migration-decisions-answer-tracker.md](migration-decisions-answer-tracker.md) | **Single place client answers to the 7 open decisions land as they arrive** (0/7 answered as of 2026-07-07) |
| Remediation plan | [migration-remediation-plan-2026-06-22.md](migration-remediation-plan-2026-06-22.md) | Validation-gate blockers/warnings + disposition (7 blockers, 5 warnings) |
| Import/transform plan | [migration-import-transform-plan-2026-06-22.md](migration-import-transform-plan-2026-06-22.md) | Load strategy, prereqs, sequence, reconciliation (DRAFT — blocked on decisions) |
| Client note (first 3 items) | [migration-client-note-2026-06-11.md](migration-client-note-2026-06-11.md) | CEF code, retired types, orphan ASINs |
| Client note (items 4–7) | [migration-client-note-2026-06-22.md](migration-client-note-2026-06-22.md) | Long names, 24-1634 result, Block/Square, duplicate cert# |
| Consolidated / send-ready email | [migration-client-decisions-consolidated-2026-06-22.md](migration-client-decisions-consolidated-2026-06-22.md) · [migration-attachments/EMAIL-READY-to-send.md](migration-attachments/EMAIL-READY-to-send.md) | Full 7-item client email + attachments |
| CR-021 (CEF Amazon code) | [CR-021-cef-amazon-code.md](CR-021-cef-amazon-code.md) | The one migration item tracked as a formal CR (live reference-row UPDATE) |

## Delivery / shipped logs (stakeholder-readable)

| Month | File |
|---|---|
| August 2026 | [shipped-2026-08.md](shipped-2026-08.md) |
| July 2026 | [shipped-2026-07.md](shipped-2026-07.md) |
| June 2026 | [shipped-2026-06.md](shipped-2026-06.md) |
| May 2026 | [shipped-2026-05.md](shipped-2026-05.md) |
| April 2026 | [shipped-2026-04.md](shipped-2026-04.md) |

## Deploy & QA gates

| Doc | File | Purpose |
|---|---|---|
| Pre-deploy agent checklist | [pre-deploy-agent-checklist.md](pre-deploy-agent-checklist.md) | Gates 0–7 before every Plesk deploy |
| Regression / E2E strategy | [../qa/regression-e2e-strategy.md](../qa/regression-e2e-strategy.md) | Security P0 gate + E2E strategy |
| E2E smoke plan | [e2e-smoke-plan.md](e2e-smoke-plan.md) | View/journey smoke coverage |
| Pre-deploy records | [predeploy-record-2026-06-june-batch.md](predeploy-record-2026-06-june-batch.md) · [predeploy-record-2026-06-mid-batch.md](predeploy-record-2026-06-mid-batch.md) | Per-deploy gate evidence |
| Test execution log | [test-execution-log.md](test-execution-log.md) | Manual/automated test runs |

## Process / improvement

| Doc | File |
|---|---|
| Ways to improve | [ways-to-improve.md](ways-to-improve.md) |
| Data-protection controls review | [data-protection-controls-review-2026-06-24.md](data-protection-controls-review-2026-06-24.md) |

---

## Maintenance

- **When work ships:** append to the current `shipped-YYYY-MM.md` and update the relevant register (DEF/CR status).
- **When a migration decision is answered:** update [migration-decisions-answer-tracker.md](migration-decisions-answer-tracker.md), not this index.
- **When a new standing tracking doc is created:** add one row here under the right section and bump *Last updated*.
- **When a new CR/DEF gets its own plan or correspondence doc:** add a row to the Per-CR or Per-DEF table above (main doc + any paired email).
- Keep this page a **pointer list** — resist re-adding live counts; they belong in the linked files.

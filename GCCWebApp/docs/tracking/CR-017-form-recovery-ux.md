# CR-017 — Unsaved-form recovery banner UX rework (Phase 2)

**Status:** Proposed — Phase 2 / future
**Date drafted:** 2026-05-06
**Source:** Internal — Shawn observed false-positive recovery prompts on the cert Create page (e.g., *"Unsaved form data recovered from 08:25 PM. Would you like to restore it?"*). Decision: hide the prompt now, rebuild the UX in a later phase.
**Estimated effort:** 6–10 hrs (frontend + a small QA pass)
**Phase:** 2 — not blocking launch

---

## 1. Current state (as of 2026-05-06)

The cert Create / Edit pages run a localStorage-backed unsaved-changes tracker (`wwwroot/js/certificate-create.js`, `UnsavedChanges` module). It auto-saves the form to `localStorage` periodically while the user is editing, and on next page load it checks for prior data and offers a recovery banner:

> *"Unsaved form data recovered from HH:MM. Would you like to restore it?"*  →  [Restore] [Discard]

The auto-save itself works. The recovery prompt has known UX problems that surfaced during internal QA:

1. **False positives on cascading dropdowns.** Loading the Edit page fires `change` on cascading selects (Company → Product, Category → Type → Subcategory, Cert Type → Subcategories). Each fire rebaselines after a `setTimeout`, but the banner appears before the rebaseline, so a freshly opened Edit page shows a recovery prompt for "data" that's just the same form mid-cascade.
2. **Partial-restore fidelity.** The restore handler skips `__RequestVerificationToken` and file inputs (`CertificateFile`, `ThumbnailFile`). That means a recovered session does not bring back the uploaded PDF, the cert image preview, or the auth token — so the user sees text fields restored but the cert document is missing. They'll click Save and either get a validation error (PDF required on Create) or quietly persist a half-edit.
3. **Doesn't cover Product IDs.** The Product ID Manager has its own state (in-memory on Create, immediate API on Edit, per DEF-149). The recovery banner only restores standard form fields, not the product ID list. After a "restore" the user sees their cert fields back but no ASINs.
4. **Doesn't cover custom fields.** Custom field values are written via hidden inputs on submit (`form.querySelectorAll('input[name^="CustomFieldValues"]')`) but only built at submit time, not present in the live form for the auto-save to capture.
5. **Time format ambiguity.** Banner says *"recovered from 08:25 PM"* with no date. If the user came back the next day, it's not obviously stale. (Existing 2-hour TTL handles this in code, but not in the UI.)

The prompt is currently **hidden** (DEF-151, 2026-05-06) via a `SHOW_RECOVERY_PROMPT: false` kill switch in `certificate-create.js`. Auto-save continues writing to localStorage; stale entries (older than 2 hours) are GC'd on each page load so we don't leak storage. The data is preserved so Phase 2 can consume it cleanly.

---

## 2. Phase 2 goal

Re-enable a recovery experience that is **trustworthy** — when the banner appears, the user can act on it without ending up in a half-restored state.

---

## 3. Proposed scope

### 3.1 Eliminate false positives
- Don't initialize the unsaved-changes baseline until **after** all async cascading completes (Company products load, product type cascade, cert subcategories, custom fields, existing tags). The current code calls `rebaselineUnsavedChanges()` on a `setTimeout(..., 1000)` — replace with explicit promise-chained signals from the cascade loaders so the banner only ever evaluates a fully-loaded form.
- Don't show the recovery banner for diffs that consist solely of values that match what the cascade just set (semantic equivalence — if the saved value for a hidden tag-IDs field is the same JSON as the just-rendered value, it's not a difference).

### 3.2 Cover the full save surface
- Capture **product IDs** (`window.productIdManager.productIds`) into the localStorage payload alongside form fields.
- Capture **custom field values** (`certificateCustomFields.getFieldValues()`) into the payload.
- Capture **selected tags** (external + internal hidden inputs) into the payload.
- Capture **selected subcategories** (the `SubcategoryIds` hidden input) into the payload.

### 3.3 Restore handler must be all-or-nothing
- If the saved payload references files the browser can't restore (cert PDF, thumbnail), the banner should call out that limitation: *"Your text fields can be restored, but your previously uploaded PDF will need to be re-uploaded."* Make the limitation explicit before the user clicks Restore, not after.
- Restore product IDs into the manager (push into `productIdManager.productIds`, then call `renderTable()`).
- Restore custom field values into the field controls (call `certificateCustomFields.setFieldValues(values)` — method may need to be added).
- Re-run validation after restore so any errors are visible immediately.

### 3.4 Banner copy
- Show a date when the saved time is more than 6 hours old: *"Unsaved form data recovered from yesterday at 08:25 PM. Would you like to restore it?"*
- Show what's covered by the restore: *"Restoring will recover: 14 form fields, 2 product IDs, 1 custom field value. The certificate PDF will need to be re-uploaded."*
- Keep [Restore] and [Discard]. Add a third option [Preview] that expands a list of which fields will be changed before committing — for users who don't want to gamble.

### 3.5 Edit-page applicability
- On the Edit page, the recovery banner should only appear if the saved data is for the **same certificate** (compare `Model.Id`). Currently the storage key is presumably keyed by URL path, but verify — a recovered Create-page payload should never apply to an Edit page or vice versa.

---

## 4. Out of scope

- **Cross-device recovery.** localStorage is per-device. If the user saves on their laptop and then opens the cert on their phone, no recovery. Server-side draft-saving is a much bigger CR (schema, soft-state, garbage collection, RBAC) and not in this CR's scope.
- **Recovery for cert PDF / thumbnail uploads.** Browsers don't expose a way to re-pickle a `File` object across sessions without IndexedDB and a meaningful blob-store strategy. Out of scope; the Phase 2 banner explicitly tells the user this.
- **Conflict detection.** If the user has the same cert open in two tabs and saves in both, last-write-wins on the server. Multi-tab merge logic is a separate, bigger problem.

---

## 5. Files (estimated)

- `wwwroot/js/certificate-create.js` — `UnsavedChanges` module rework (the bulk of the work).
- `wwwroot/js/product-id-manager.js` — small hook so the manager can be asked for / told to restore its state.
- `wwwroot/js/certificate-custom-fields.js` — add `setFieldValues(values)` to mirror the existing `getFieldValues()`.
- `Views/Certificate/Create.cshtml`, `Views/Certificate/Edit.cshtml` — wire the new lifecycle hook for "all-cascades-done", flip `SHOW_RECOVERY_PROMPT` back on once the banner is trustworthy.

No backend changes. No database changes. No Partner API impact.

---

## 6. Why this is a CR, not a defect

The current behavior is that **no prompt appears**. That's intentional and stable. Building a smarter, more trustworthy prompt is a feature improvement, not a fix for a regression. It needs to be sized, prioritized against other Phase 2 work, and signed off — not slipped into a hotfix.

---

## 7. Acceptance criteria (when Phase 2 is built)

- Opening a freshly-loaded Edit page never shows the recovery banner unless the user actually had unsaved changes from a prior session (false-positive rate validated against 10 representative cert Edit loads in QA).
- Restore brings back all of: standard fields, product IDs, custom field values, tags, subcategories. PDF / thumbnail are explicitly called out as not restorable.
- Same-cert scoping verified: a saved Create-page payload never offers itself for restore on an Edit page; an Edit-page payload for cert A never offers itself on cert B.
- Banner displays a date when the saved time is more than 6 hours old.
- All work covered by Playwright tests in the existing test suite.

# CR-016 — Bulk Replace mode for Product ID import

**Status:** Proposed — awaiting client sign-off
**Date drafted:** 2026-05-06
**Source:** Client UAT QC log row 51 (Carly)
**Estimated effort:** 3–4 hours (frontend-only)

---

## 1. The ask (verbatim from QC log)

> **DISCUSS:** I see how someone would edit or add to the Product list individually.
>
> What if someone wanted to replace all the existing with a new list that could have a thousand records tied to it? So it would remove those not included and add those new and keep existing still on list?
>
> I tried a version of this and got an error. There were 2 listed from a prior import. I tried to import a doc with only 1 product.

## 2. Discovery — what already exists

The replace-all capability **already ships** in the Product ID Manager. During investigation we confirmed:

| Capability | Status | Reference |
|---|---|---|
| Replace-all toggle | **Built** | `_ProductIdManager.cshtml:313-316` |
| Deactivation logic (remove existing not in import) | **Built** | `product-id-manager.js:1175-1195` |
| Per-certificate scoping (not global) | **Built** | `product-id-manager.js:884` — matches against `this.productIds` only |
| Within-file duplicate detection | **Built** | `product-id-manager.js:940-946` |
| Diff preview (New / Modified / Unchanged / Errors) | **Built** | Import preview modal |
| Per-row select/deselect before commit | **Built** | Import preview modal |
| Excel scientific-notation auto-fix | **Built** | `product-id-manager.js:838-854` |

**Conclusion:** The functionality is in place. The reported error is most likely a discoverability problem — the "Replace all existing" toggle is tucked into the modal footer between the Download Template button and the Cancel/Apply buttons. A first-time user importing a 1-row file against 2 existing rows would see the preview show `1 New, 0 Modified, 2 Unchanged` (the 2 existing rows are not in the file, so they look "unchanged from the database's perspective") and would not know that the toggle controls whether the missing rows get removed.

This CR is a **UX polish to surface the existing capability**, not a build of new functionality.

## 3. The change

### Before (current)
- Replace-all is a single checkbox tucked in the modal footer, labeled `☐ Replace all existing` with a hover tooltip
- Existing IDs that would be removed by the toggle are not shown anywhere in the preview
- Stats show: New / Modified / Unchanged / Errors
- No warning before destructive action — toggle + Apply commits the deletes silently

### After (proposed)
- Top of modal: **mode selector** as radio buttons
  - ◉ **Add to existing list** (default — current append behavior)
  - ○ **Replace entire list** (remove IDs not in this file)
- Stats expanded to 5 tiles. The **To Remove** tile only appears when Replace mode is selected.
- Inline warning banner above the preview table when Replace mode is selected:
  > ⚠ N existing product IDs not in this file will be permanently removed
- Existing rows that would be removed appear in the preview table as **`[Remove]`** rows so the user sees exactly what's leaving
- CSV-parse failures surface specific reason (header mismatch, malformed row, bad format) instead of a generic error

### Out of scope (and why)
- **Background processing / progress bar for >1000 rows.** Current implementation handles list scale inline. If we hit a real performance ceiling at the partner-onboarding stage, we'll size that as a separate CR. Building it speculatively burns budget.
- **Backend / schema / API changes.** Per-cert scoping and replace-all deactivation already work correctly. No changes to `Controllers/Api/ProductIdApiController.cs`, `Services/ProductIdService.cs`, or `Models/CertificateProductId.cs`.
- **Partner API contract.** No changes to `/api/v1/certificates*` — this is internal-app UX only. Per `feedback_amazon_api_immutable.md`, partner-facing JSON shape stays frozen.

## 4. Why this is a CR, not a defect

A defect would mean the system fails to do what was specified. Here the system does what was specified — it removes existing IDs not in the import when the toggle is on. The QC item is a **scope decision**: should the Replace-all path be a primary, prominent mode (vs. a tucked-away toggle), and should we render the destructive impact explicitly before commit? Both questions are UX scope choices that go beyond the original requirement and warrant a formal CR.

## 5. Files affected

| File | Change |
|---|---|
| `Views/Shared/_ProductIdManager.cshtml` | Add mode-selector radios at top of import-preview modal; add "To Remove" stat tile (conditional render); add warning banner; restructure footer (drop the old toggle) |
| `wwwroot/js/product-id-manager.js` | Read radio mode in `applyImport()`; render `[Remove]` preview rows when in Replace mode; expand error messaging in `parseAndPreviewImport()` |
| `wwwroot/css/product-id-manager.css` | Styles for mode selector, warning banner, "To Remove" tile, `[Remove]` row variant |

No SQL migration. No backend code touched. No test data changes.

## 6. Success criteria (how the client verifies)

1. Open any cert with existing Product IDs → click Bulk Import → upload a CSV with only some of those IDs
2. Default mode (`Add to existing list`) → preview shows `New / Modified / Unchanged` exactly as today; no destructive behavior
3. Switch to `Replace entire list` mode →
   - "To Remove" stat tile appears with correct count
   - Warning banner shows correct count of IDs to be removed
   - Preview table shows the to-be-removed rows tagged `[Remove]`
4. Apply → only the rows in the import file remain on the cert; the prior IDs not in the file are gone
5. CSV with malformed rows → error messages identify the specific row + reason, not a generic "import failed"
6. Per-cert scope holds: importing the same identifier on Cert A and Cert B does not flag a global duplicate

## 7. Effort & timing

- **Build:** ~3-4 hrs (one front-end pass)
- **Test:** ~1 hr (Playwright scenarios for each mode, plus manual on real cert)
- **Total:** ~4-5 hrs end to end

Can ship the same day it's approved. No deploy dependencies, no Partner API coordination.

## 8. Alternatives considered

| Option | Effort | Recommendation |
|---|---|---|
| **A — UX polish (this CR)** | 3-4 hrs | ✅ Recommended |
| B — Mode-based redesign (3 modes: Append / Replace / Sync, picked before file upload) | 8-10 hrs | Heavier mental model than the client asked for; defer unless they want a third "merge" semantic |
| C — Dedicated "Replace List" wizard with progress bar + background job | 16-20 hrs | Speculative — solves a scale problem we don't yet have |

## 9. Decision needed from client

1. **Approve Option A?** (Yes/No/want B or C instead)
2. **Default mode**: stay on `Add to existing list` (safest, current behavior preserved)?
3. **Naming**: are `Add to existing list` and `Replace entire list` the right labels, or do you prefer different terms (e.g., `Append` / `Replace`)?

---

## Appendix — Mockup

See `docs/tracking/CR-016-mockup.html` for the visual mockup of the proposed import-preview modal.

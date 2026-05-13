# DEF-191 — Product ID Import Preview, partial selection with Replace-all OFF

## Bug

On `/Certificate/Edit/{id}`, when the user uploaded a CSV, opened the Import Preview, manually checked a row whose Type+Product ID already matched an existing PID on the cert with identical other fields (preview classified as `status: unchanged, action: skip`), and clicked **Apply Import** without enabling **Replace all existing**, the import failed with:

> Import failed: ASIN:B08N5W0001 - Product ID already exists on this certificate

The server returned HTTP 200 with `successCount: 0, errorCount: 1`.

## Root cause

`applyImport()` in `wwwroot/js/product-id-manager.js` partitioned selected preview rows into the bulk-POST payload with the filter `r.action !== 'update'`. That filter let `'skip'` rows through into the bulk endpoint, where the server (correctly) rejected them as duplicates. Skip rows are no-ops by definition and should not have been sent.

## Fix

Two changes in the same `applyImport()` function:

1. Tightened the bulk filter from `r.action !== 'update'` to `r.action === 'create'`. Skip rows now partition into a new `skippedExisting` list.
2. Added a skip-aware branch to the success-message decision so a skip-only selection ends with the friendly toast `"X row(s) already on this certificate — nothing to change"` instead of the generic `Import failed: Unknown error` fallback. Mixed selections that include skips append `"; X already on file"` to the success message.

Replace-all mode is unchanged — every non-error row still drives the server's deactivation logic.

## Verification

Run from this directory:

```bash
py -3 verify_def191.py
```

Requires: dev server on `http://localhost:5101`, seeded analyst user (`jane.analyst@anukiconsulting.com` / `TestPass123!`), and cert `26-0023` with at least 11 product IDs.

The script exercises four scenarios on cert `26-0023`, all with **Replace-all OFF**:

| Scenario | What the CSV contains | What the user checks | Expected toast |
|---|---|---|---|
| A | one row whose Type+Value+all-other-fields match an existing PID | the one row | `1 row(s) already on this certificate — nothing to change` |
| B | one row whose Type+Value match an existing PID but a field is changed | the one row | `Updated 1 product ID(s)` |
| C | one row with a new Type+Value | the one row | `Imported 1 product ID(s)` |
| D | one update + one skip (different existing PIDs) | both | `Updated 1 product ID(s); 1 already on file` |

Before fix: A failed with `Import failed: ... Product ID already exists`, and D would similarly fail because the skip row reached the bulk endpoint.

After fix: all four scenarios end in a success toast that accurately describes the outcome.

## Captured run (2026-05-12)

```
================================================================
DEF-191 verification — Product ID Import Preview, Replace-all OFF
================================================================
Cert: 26-0023 (id=24)

  [PASS] A skip-only: 1 row(s) already on this certificate — nothing to change
  [PASS] B modified-only: Updated 1 product ID(s)
  [PASS] C new-only: Imported 1 product ID(s)
  [PASS] D mixed update+skip: Updated 1 product ID(s); 1 already on file

RESULT: PASS
```

## Artifacts in this folder

- `verify_def191.py` — Playwright driver covering all four scenarios.
- `A_skip_only_toast.png` — proof screenshot of scenario A's new success modal. The previously-broken case now renders the friendly no-change toast.

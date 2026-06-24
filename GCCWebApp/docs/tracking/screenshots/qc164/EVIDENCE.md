# QC #164 — Validation Evidence

> **Companion (2026-06-23):** the **date-expiry** case — a cert that ages past its expiration date,
> proving its ASINs drop from the Amazon export automatically — is verified separately in
> [`EVIDENCE-date-expiry-2026-06-23.md`](EVIDENCE-date-expiry-2026-06-23.md) (19/19 PASS, with
> before/after export JSON samples). The document below covers the status-change (Hold) case and
> the UI/import surfaces.

**Test date:** 2026-05-12
**Environment:** localhost:5101, GCCDatabase_Dev
**QC item:** Product Identifier status follows cert; Status removed from UI, Import Template, Add popup
**Scope per defect register (DEF-097):** Verified shipped 2026-05-06 — this is the manual re-verification through Playwright per `feedback_playwright_screenshots.md`

## Summary

| # | Test | Result | Artifact |
|---|------|--------|----------|
| A1 | Product ID Manager filter row + table — no Status column/filter, 5 viewports | PASS | `A1_pid_manager__*.png` |
| A2 | Add Product ID modal — no Status field, 5 viewports | PASS | `A2_add_modal__*.png` |
| B  | Import template CSV — no Status header | PASS | `B_import_template.csv` |
| C1 | UP-cascade: cert Hold → CAP flips PIDs Inactive → Active | PASS | `cascade_log.txt` T2 |
| C2 | DOWN-cascade: cert CAP → Hold flips PIDs Active → Inactive | PASS | `cascade_log.txt` T3 |
| D  | Partner API list — only PublicStatuses certs, `status` literal `"Active"` | PASS | `D_step1_baseline.json`, `partner_api_log.txt` |

## A1 — Product ID Manager UI (5 viewports)

Cert under test: **26-0002** (Beta Industries, CAP, 2 product IDs).

Captured on each of: desktop 1920×1080, desktop 1366×768, tablet-landscape 1024×768, tablet-portrait 768×1024, mobile 375×667.

Filter row across all viewports contains only: **All Types | All Partners | Search**. No Status dropdown.

Table headers across all viewports: **Type | Product ID | Product Name | SKU | Partner | Created | Created By | Actions**. No Status column.

## A2 — Add Product ID modal (5 viewports)

Fields rendered: **Type, Product ID, Partner, Product Name, SKU, Description**. No Status field. Same across all 5 viewports.

## B — Import template

File `B_import_template.csv` content (one line, no trailing newline):

```
Type,Product ID,Product Name,SKU,Partner,Description
```

No Status column.

## C — Auto-cascade (the Amazon-badging guarantee)

Driven through the analyst Edit form via Playwright (`qc164_cascade.py`). Each transition is a real Edit/Save through the controller, exercising the `SyncProductIdStatusesForCertificateAsync` cascade in `CertificateService.cs:843`.

| Time | Cert Status set via UI | Cert.Status in DB | PID Statuses in DB | Verdict |
|------|------------------------|-------------------|---------------------|---------|
| T0   | (baseline)             | Client Approved - Paid | Inactive, Inactive (stale local dev data) | — |
| T1   | Hold                   | Hold | Inactive, Inactive | no-op cascade — both already Inactive |
| T2   | Client Approved - Paid | Client Approved - Paid | **Active, Active** | **UP-cascade fired** |
| T3   | Hold                   | Hold | **Inactive, Inactive** | **DOWN-cascade fired** |
| T4   | Client Approved - Paid | Client Approved - Paid | Active, Active | restored |

Final dev DB state for cert 26-0002: CAP / PIDs Active (also incidentally fixed the stale local dev-DB mismatch that existed at T0).

Full log: `cascade_log.txt`.

## D — Partner API contract

Endpoint: `GET /api/v1/certificates`
Auth: `X-API-Key: gcc_partner_959...` (Amazon)
Dev DB has 11 Amazon-partnered certs total: 4 CAP (no PIDs), 6 legacy `Active` (with PIDs but non-public per CR-015), 1 Draft.

```
totalCount: 4
  EXT-AMZ-003: status='Active', productIds=0
  26-0001:    status='Active', productIds=0
  EXT-AMZ-002: status='Active', productIds=0
  EXT-AMZ-001: status='Active', productIds=0
```

Contract checks (all PASS):
- **Filtering:** 6 legacy-`Active` certs and 1 Draft cert are NOT in the response (filtered by `CertificateStatusHelper.PublicStatuses` per `Controllers/Api/v1/CertificatesController.cs:160`)
- **Status literal:** every response item has `status: "Active"` even though the internal cert.Status is `"Client Approved - Paid"` (mapping in `BuildPartnerCertificateDto`)
- **Cert 26-0146** (legacy `Active`) is correctly absent — its child PIDs would not be promoted to `Active` either since the parent isn't public.

Bonus test (promote cert 26-0146 → CAP to demonstrate `productIds[]` in response) was attempted in `qc164_partner_api.py` but the workflow gate blocked the legacy `Active` → CAP transition on save; this is unrelated to QC #164 and noted as out-of-scope here. Cert 26-0146 was **NOT mutated** (confirmed via post-test SQL snapshot).

## Files

```
qc164_capture.py       — Playwright UI capture (A1, A2, B)
qc164_cascade.py       — Playwright cascade driver (C1, C2)
qc164_partner_api.py   — Partner API contract driver (D)
A1_pid_manager__*.png  — 5 viewport screenshots of Edit page (UI surfaces)
A2_add_modal__*.png    — 5 viewport screenshots of Add Product ID modal
B_import_template.csv  — downloaded template, headers only
cascade_log.txt        — T0..T4 transition log
partner_api_log.txt    — Partner API call log
D_step1_baseline.json  — full Partner API list response
D_step2_promoted.json  — attempted promote (no change, transition blocked)
EVIDENCE.md            — this file
```

## E — Export column audit (2026-06-24, code-level — the 6/23 "confirm export matches CR" note)

Shawn's 6/23 SIT note asked to *"confirm export matches CR details in email."* Static audit of the export/template column sets (no code change):

| Export path | Source | Status column? |
|---|---|---|
| Per-cert in-memory export | `product-id-manager.js:2046` — `Type, Product ID, Product Name, SKU, Partner, Created, Created By, Description` | **None** (CR-015 comment `:2040`) |
| Import template (sample) | `product-id-manager.js:2081` — `Type, Product ID, Product Name, SKU, Partner, Description` | **None** (CR-015 comment `:2077`) |
| Global CR-184 export | `ExportController.DownloadActiveProductIds` (`ExportController.cs:293`) | **Cert-level** `Certificate Status` only — derived, added 6/17 per client "for additional validation"; no product-ID-controlled status column; set pre-filtered to active (parent cert Paid/Republished) |

Remaining `<th>Status</th>` at `_ProductIdManager.cshtml:345` is the **import-preview row state** (New/Modified/Unchanged/Error), not the cert status — correct to keep.

**Verdict: PASS** — exports match the CR. Status is never a user-controlled product-ID field anywhere; where status appears it is the cert's derived status.

## Conclusion

QC #164 is **fully verified** through Playwright + SQL + Partner API, plus a code-level export-column audit (§E). All UI surfaces are clean across 5 viewports, the auto-cascade fires correctly in both directions on real Edit-form saves, the Partner API correctly filters non-public certs and emits the contract-literal `"Active"` status, and no export or template surfaces a user-controllable product-ID status.

No code changes were made. One side effect: stale local dev-DB data for cert 26-0002 was incidentally corrected by exercising the cascade.

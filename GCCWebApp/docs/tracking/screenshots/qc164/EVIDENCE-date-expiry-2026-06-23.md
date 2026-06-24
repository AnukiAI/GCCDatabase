# QC #164 — Date-Expiry Exclusion Evidence (Amazon Integration)

**Test date:** 2026-06-23 (run completed early 2026-06-24)
**Environment:** localhost:5101, live app, real DB `GCCDatabase_Dev`
**QC item:** Product Identifiers / Amazon Integration (Change Request, SIT)
**Question being verified:** *Are ASINs tied to expired/inactive certificates excluded from the Amazon export/badging?*
**Harness:** `Scripts/smoke-prod/qc164-expired-asin-exclusion.js` (re-runnable)
**Result:** **19/19 checks PASS** — see `2026-06-23_run_log.txt`

> This document covers the **date-expiry** case specifically (a certificate that *ages past its
> expiration date*). The earlier `EVIDENCE.md` (2026-05-12) proved the **status-change** case
> (Hold) and the UI/import surfaces. Together they cover both ways a cert can become non-active.

---

## Why GCC can trust this (the mechanism)

An ASIN is excluded from the Amazon export by **two independent filters**, and a date-expired
cert hits **both**:

1. **Automated expiry** — when a cert passes its `ExpirationDate`, the background service
   `CertificateExpirationBackgroundService` (runs on a timer; daily 00:00 UTC in production)
   flips its status to **Expired** and cascades its product IDs to **Inactive** (CR-015).
   *(A save-time guard, `TryApplySaveTimeAutoExpire`, does the same instantly on any manual edit.)*
2. **Cert-level export filter** — every partner endpoint returns only
   `CertificateStatusHelper.PublicStatuses` = `{ Client Approved - Paid, Republished }`.
   `Expired` is not in that set, so the whole certificate drops out.
   (`Controllers/Api/v1/CertificatesController.cs:168`, `:690`, `:788`, `:998`, `:383`, `:437`)
3. **Product-ID-level export filter** — even on a still-active cert, only `Status == "Active"`
   product IDs are emitted (`:264`, `:1067`). The cascade in step 1 sets them to `Inactive`.

So the ASIN is dropped **twice over** — defense in depth.

---

## Live test — what was actually executed

**Target:** cert **26-0147** (Amazon partner, `Client Approved - Paid`, 2 ASINs:
`B01DSSNB5G`, `B0BRNPBZJ8`). The Amazon partner API was driven exactly as Amazon calls it:
a freshly-minted `gcc_partner_…` key, cookie-free, `X-API-Key` header.

| Step | Action | Observed | Verdict |
|------|--------|----------|---------|
| 1 | **BASELINE** — call Amazon export | 26-0147 present, `status:"Active"`, both ASINs returned. Export = **6 certs / 13 ASINs** | active cert exports its ASINs ✓ |
| 2 | **AGE OUT** — backdate `ExpirationDate` to 2026-06-01 (past) via SQL, simulating time passing | cert still `Client Approved - Paid`, ASINs still `Active` (not yet swept) | — |
| 3 | **WAIT** — let the **real background service** run | After ~55s: `Status → Expired`, both ASINs `→ Inactive`. `CertificateStatusHistory` row: `Client Approved - Paid → Expired`, `ChangedByUsername=System`, `WorkflowStep=Auto-Expiration` | automated expiry fired ✓ |
| 4 | **PROVE** — call Amazon export again | 26-0147 **absent**; neither ASIN appears anywhere in the payload; `search/productid?productId=B01DSSNB5G` returns **0 certs**; `GET /certificates/number/26-0147` returns **404**. Export = **5 certs / 11 ASINs** | ASINs excluded ✓ |
| 5 | **RESTORE** — return cert to original | `Client Approved - Paid`, `2028-05-31`, ASINs `Active`; reappears in export | dev data left clean ✓ |

**Net effect on what Amazon receives:** the export went from **6 certs / 13 ASINs** to
**5 certs / 11 ASINs** — exactly cert 26-0147 and its two ASINs removed, nothing else touched.

---

## Artifacts (in this folder)

```
2026-06-23_export_1_baseline.json     — full Amazon export BEFORE expiry (26-0147 + ASINs present)
2026-06-23_export_2_after_expiry.json — full Amazon export AFTER expiry (26-0147 + ASINs absent)
2026-06-23_db_original.txt            — DB snapshot: cert CAP, ASINs Active
2026-06-23_db_after_expiry.txt        — DB snapshot: cert Expired, ASINs Inactive
2026-06-23_run_log.txt                — full 19/19 PASS run log incl. CertificateStatusHistory proof
qc164-expired-asin-exclusion.js       — the harness (Scripts/smoke-prod/), re-runnable
```

## Note for GCC

GCC said they have no direct way to verify this backend behavior. The two export JSON files are
the verification: open both, search for `26-0147` and `B01DSSNB5G`. They are present in
`_baseline.json` and gone from `_after_expiry.json`, with no other rows changing. The only thing
done to the cert between the two files was letting its expiration date pass — the system did the
rest automatically.

**No code changes were made.** The dev cert was restored to its original state at the end of the run.

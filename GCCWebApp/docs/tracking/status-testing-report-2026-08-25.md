# Status Testing Report — Full Regression, TEST Environment

**Target:** `https://gccwebapp.com` (test environment, running the migrated client dataset loaded 2026-08-18)
**Date:** 2026-08-25 → 2026-08-26
**Run by:** Anuki (internal), automated regression + browser-driven visual verification
**Scope requested:** all roles, exports, imports, downloads, data matching, create, edit, performance

> **UPDATE 2026-08-26 — all five findings actioned.** F-1 corrected (see below: the premise was
> wrong), F-2 through F-5 fixed, built clean, and **verified end-to-end on localhost through the
> real HTTP path**, not just at the unit layer. Tracked as **DEF-376 to DEF-379**; a sixth issue
> found during cleanup is logged as **DEF-380** (open). Nothing is deployed yet.
> See §2a for fix status.

---

## 1. Headline

| | |
|---|---|
| **Total checks** | **324** |
| Passed | **308** |
| Failed | **9** |
| Warnings | 1 |
| Informational | 6 |
| **Verdict** | **The application is functionally sound.** Access control, the write path, and performance are clean. Every failure is a data-scoping or data-quality issue, not a broken feature. |

**Nothing found here blocks continued UAT.** Two items should be resolved before the client sees the
Amazon partner surface, and one is an environment/ops issue that will silently break the existing
script suite.

### Results by phase

| Phase | What it covers | Pass | Fail | Warn |
|---|---|---:|---:|---:|
| 0 — Preflight | Gate mode, 6 role logins, landing pages | 13 | 0 | 0 |
| 1 — RBAC matrix | 24 routes × 7 roles | **168** | **0** | 0 |
| 2 — Data & scoping | Populations, isolation pairs, cert-number integrity | 31 | 2 | 0 |
| 3 — Exports & downloads | CSV exports + templates per role, scoping | 47 | 6 | 0 |
| 4 — Create / edit / delete | Full write round-trip through the UI | **17** | **0** | 0 |
| 5 — Performance | 11 pages + 8 APIs, 5 samples each | **19** | **0** | 0 |
| 6 — Import | Template → upload → preview → apply → verify | 13 | 1 | 1 |

---

## 2a. Fix status (added 2026-08-26)

| # | Finding | Outcome | DEF | Verified how |
|---|---|---|---|---|
| F-1 | Gate password rotation | **Premise was wrong — corrected, no code change needed** | — | See correction below |
| F-2 | Amazon tile view leaked 2 not-yet-effective certs | **FIXED** | DEF-376 | Real Partner user on localhost: 208 tiles (was 210), both certs gone, download returns 400 |
| F-3 | Empty-scope export returned HTTP 400 | **FIXED** | DEF-377 | Empty-filter export returns 200 `text/csv`, header row only |
| F-4 | Import rejected a blank Certificate # | **FIXED** | DEF-378 | Real upload → preview → apply: 2 blank rows → `26-0482` + `26-0483`, distinct |
| F-5 | Product import template reachable by External | **FIXED** | DEF-379 | Admin 200, anonymous 302 to login |
| F-6 | Novi has no cert associations | Unchanged — data gap, needs seeding | — | — |
| F-7 | Duplicate cert numbers | Unchanged — already DEF-369 | DEF-369 | — |
| — | `GET /Certificate/Delete/{number}` 500s (no view) | **Found during cleanup, OPEN** | DEF-380 | — |

**Correction to F-1.** The report originally said the 2026-08-17 rotation had been applied to the
scripts and that all 46 would fail at the gate. **That was wrong.** The scripts were never rotated:
92 files under `Scripts/` already carry the working `Cert-Test-Access-2026!`, and the only file with
the non-working value was `_regression/_lib/regression-lib.js`, written during this run. The
existing suite was never broken. Per Shawn (2026-08-26) the rotation is deliberately **not**
happening while UAT users are mid-test, so the regression lib was pinned to the live value instead.

**Not deployed.** All four code fixes are in the working tree only. The regression phases that found
F-2 through F-5 should be re-run against gccwebapp.com after the next deploy to confirm them there —
localhost verification used the dev clone, which carries the same `26-0441`/`26-0442` rows.

**Build and tests:** `dotnet build` clean (0 errors). Full unit suite **87 failures before and after**
(all pre-existing, mostly integration tests needing a live server), passing count **731 → 733** from
three new import tests.

---

## 2. What passed, and what that actually proves

**Access control — 168/168.** Every one of 24 routes was requested by all seven callers
(anonymous, External ×2, Partner ×2, Analyst, Admin). Public routes served anonymously; every
External-, Analyst-, and Admin-gated route bounced everyone below its policy to
`/Account/Login` or `/Account/AccessDenied` and served 200 to those at or above it. No route
leaked, and no route over-blocked.

**Company and partner isolation — proven by pairing, not by counting.** `test.external`
(Sika Corporation) and `test.external2` (MAPEI) were each walked to exhaustion and their
certificate-number **sets** compared in both directions: 30 vs 21, **0 shared**, and each sees
exactly one distinct company name. Same for the Amazon/Novi partner pair. A matching count would
have survived a scoping bug; a set comparison does not.

**Analyst and Admin see the identical internal population** — 5,702 rows, 5,527 distinct numbers,
zero rows exclusive to either role.

**Public visibility holds.** The anonymous population (964) is a strict **subset** of the internal
population, with no cert visible publicly that is absent internally.

**The write path works end to end.** As Analyst, through the real form: create → auto-numbered
`26-0482` → PDF uploaded and linked → read back through the API → Draft correctly **not** publicly
visible → Edit form prefilled with the stored values → edit persisted → no duplicate row created →
Delete correctly refused to the Analyst (302 to AccessDenied) → deleted as Admin → fixture removed.
The optional **Certification Results** field round-tripped correctly on both create and edit.

**Performance is comfortable — 19/19 within budget, most by an order of magnitude.**

| Surface | Median | Budget |
|---|---:|---:|
| Public tile view | 149 ms | 3,000 ms |
| Internal tile view (Analyst) | 159 ms | 4,000 ms |
| Dashboard | 475 ms | 6,000 ms |
| Certificate Create form | 379 ms | 5,000 ms |
| Internal tile API, deep page (page 50) | 144 ms | 3,000 ms |
| Search API (`search=sika`) | 356 ms | 3,000 ms |
| **Full CSV export, all 5,702 rows / 1.5 MB** | **3,759 ms** | 15,000 ms |

Deep pagination costs nothing over page 1, which means the `AsSplitQuery()` work from DEF-166/234
is holding. Cold-start on an idle app pool adds roughly 0.3–0.9 s on first hit, then settles.

**Visual verification in Chrome** confirmed the role contracts on screen: internal tiles show
**certificate numbers (26-0441, 26-0330…), not database IDs**; the External user's certificate page
has **no Add Certificate / Import / Template buttons**; the External user's Product Management table
**has no Actions column** and ends at the IDs column.

---

## 3. Findings

### F-1 — Test-environment gate password rotation never took effect *(ops — fix first, it is cheap)*

The 2026-08-17 rotation to `GreenCircle-Test-2026` is **not live**. That value returns **401**;
the pre-rotation `Cert-Test-Access-2026!` still opens the gate.

> **CORRECTED 2026-08-26 — the impact claim below was wrong.** This section originally said all 46
> scripts had been rotated and were therefore failing at the gate. They had not been: 92 files under
> `Scripts/` already carry the working `Cert-Test-Access-2026!`, and the only file with the
> non-working value was the `regression-lib.js` written during this run. **The existing suite was
> never broken.** The rotation exists in intent only — it reached neither the deployed config nor
> the scripts. Per Shawn, it stays that way while UAT users are testing; the regression lib is now
> pinned to the live value.

A deliberately-wrong password returns a clean `401 "Incorrect password"`, which proves CSRF, data
protection, and the compare path are all healthy — the configured value simply is not the one
running. `TestEnvironmentGateMiddleware` binds its options once in the constructor and is a
singleton, so **an app-pool recycle is required**; editing the JSON alone does nothing. The DEF-363
recovery may also have restored the older `appsettings.Production.json`.

**Recommendation:** decide which password is intended, set it, recycle the app pool, and re-verify.

---

### F-2 — Amazon partner tile view shows 2 certificates ~2 months before their effective date *(DEF-358 residual gap)*

**This is the finding most worth acting on before the client demos the partner surface.**

For `test.amazon`, the tile view returns **210** certificates but the CSV export returns **208**.
The two extra rows are:

| Cert # | Company | Status | Effective | Expires |
|---|---|---|---|---|
| `26-0441` | X-SENSE | Client Approved - Paid | **2026-10-21** | 2027-10-20 |
| `26-0442` | X-SENSE | Client Approved - Paid | **2026-10-21** | 2027-10-20 |

Both are **not yet effective** — today is 2026-08-25.

DEF-358 established that the not-yet-effective gate applies to *"anonymous public + the
partner/Amazon surface"*. It was wired into the v1 partner API (all 9 gate sites) and, by DEF-359,
into the Active Product Identifiers export. `Models/CertificateVisibility.cs` records that
`CertificateTileExternalApiController` was excluded **deliberately** — correct for the *External /
owning-company* role, which should see its own upcoming certs, but that controller **also serves the
Partner role**, which should not.

The evidence lines up exactly: DEF-359's own note records the Amazon partner API's clean set as
**208 rows** — the same 208 the export returns today. The tile view is the outlier.

- Query filter: `CertificateTileExternalApiController` line ~222 tests `PublicStatuses` + expiration, no effective date.
- Download gate: same controller, line 717, same omission.
- By contrast `CertificateController.cs:2392` **does** gate `Partner` on `IsNotYetEffective`.

**Recommendation:** apply the DEF-358 lower bound in `CertificateTileExternalApiController` for the
Partner role only, leaving External/company behaviour unchanged. Suggested new **DEF-376**
(register high-water is currently **DEF-375**).

---

### F-3 — Novi partner's certificate export returns HTTP 400 *(minor, but client-visible)*

`test.novi` requesting `/Certificate/ExportCertificates?format=csv` gets
**HTTP 400, `text/plain`, "No data to export or export failed"**.

A partner with zero associated certificates should get an empty CSV (header row only) or a friendly
empty-state — not a client-error status with an ambiguous message that conflates "nothing to export"
with "the export failed". This is the same broad-catch anti-pattern already called out on
`ExportController.cs:227` under DEF-359/DEF-360.

Root cause is **data, not code** (see F-6) — but the error handling is a genuine defect independent
of the data.

---

### F-4 — Import rejects a blank Certificate #, while the Create form auto-numbers one *(inconsistency)*

Two write paths disagree about the same field:

| Path | Blank Certificate # |
|---|---|
| `/Certificate/Create` | **Auto-numbers.** Verified: generated `26-0482`. |
| CSV import | **Rejects.** `"Certificate Number is required"`, whole import blocked (`Cannot Apply Import`). |

The import template annotates the Status column with *"defaults to Draft if blank"* but marks
nothing on Certificate #, so there is no way for an analyst to know it is mandatory until the import
is refused. Auto-numbering is an original requirement.

**Recommendation:** either auto-number on import for parity, or mark the column required in the
template header. The second is a ten-minute fix; the first is the requirement-faithful one.
Worth a CR rather than a DEF if the client wants the behaviour changed.

---

### F-5 — Product import template is downloadable by External and Partner users *(low)*

`/Product/DownloadProductImportTemplate` returns **HTTP 200 / text/csv** to `test.external`,
`test.external2`, `test.amazon`, and `test.novi`. `ProductController` carries a class-level
`[Authorize(Policy = "ExternalAccess")]` and this action adds no further attribute, while the
matching `ImportProducts` action **is** `AnalystOrAdmin`.

No data leaks — it is an empty column-header template. But it hands an import affordance to roles
that cannot import, which cuts against the "External users get no Add/Edit affordances" rule. The
certificate import template is correctly gated; only the product one is not.

**Recommendation:** add `[Authorize(Policy = "AnalystOrAdmin")]` to the action. One line.

---

### F-6 — Novi has zero certificate associations anywhere in the database *(data / coverage gap)*

Across all 5,702 certificates the partner field holds only **`Amazon` (244)** and **blank (5,458)**.
No certificate is associated with Novi, Wayfair, or Other.

This is why `test.novi` sees nothing, and it is **correct behaviour given the data** — partner
scoping runs the cert-level association first, so a partner with no associations reaches nobody. It
is a data gap, not a code defect.

The consequence is a **test-coverage hole**: the Novi partner path is entirely unexercised on TEST,
so the amazon/novi pair currently proves only "Amazon sees Amazon" and "Novi sees nothing" — it
cannot prove Novi is correctly denied *Amazon's* rows. Seeding a handful of Novi associations would
close that.

---

### F-7 — Duplicate certificate numbers are live on TEST — **already tracked as DEF-369**

137 certificate numbers are shared by 312 rows (5,702 rows → 5,527 distinct). **8 of those numbers,
covering 20 rows, are publicly visible.** Examples: `26-0182` ×4, `26-0166` ×3, `26-0168` ×3.

312 − 137 = **175 surplus rows**, which matches DEF-369's "175 certificates have no URL at all"
exactly. This is the known migration blocker, confirmed live rather than a new defect.

Recorded here as regression evidence and to confirm it survived the migration. No new DEF needed.

---

## 4. Open question for the client (not a defect)

**Should an External (owning-company) user be able to see their own expired certificates?**

Today they cannot. Sika Corporation has **574** certificates in the database; `test.external` sees
**30**. MAPEI has **313**; `test.external2` sees **21**.

That is as-coded and deliberate — `CertificateTileExternalApiController` filters External users to
publishable statuses **and** unexpired. Given that **4,682 of 5,702 certificates (82%) are Expired**,
the effect is that a company sees roughly 5% of its own record.

| Status | Count |
|---|---:|
| Expired | 4,682 |
| Client Approved - Paid | 980 |
| Client Approved - Confidential | 40 |

DEF-358's scope note says External users keep seeing *upcoming* certs because "a company needs to
know its cert is queued". The same reasoning arguably applies to its expired history. This is a
**requirements decision, not a bug** — flagging it because the numbers are dramatic enough that the
client will ask, and the answer should be decided rather than discovered.

---

## 5. Coverage gaps — what this run did *not* test

Stated plainly so the pass rate is not read as more than it is.

1. **Import reassignment / overwrite acknowledgement paths.** These rewrite existing client rows;
   exercising them needs a CR, not a smoke run.
2. **Import Undo.** The batch id is not scrapeable from the History page markup, so undo was not
   fired. Cleanup fell through to the Admin-delete backstop, which succeeded.
3. **Partner API v1 contract diff.** Requires a live partner API key; not run here. Gate 4 of the
   pre-deploy checklist covers it via `amazon-contract-diff.js`.
4. **The certification-type picker widget.** Its native `<select>` is `display:none`, so the field
   was set programmatically. The picker's own click behaviour was not exercised.
5. **2FA.** All six role accounts have 2FA off by design; the `2fatest` fixture was not used.
6. **Bulk PDF download / ZIP, and per-certificate PDF download at scale.** Deliberately skipped —
   bulk sweeps have killed the app pool before.
7. **Chrome DOM assertions.** The Claude-in-Chrome extension lacks read permission for
   `gccwebapp.com`, so browser-side verification was screenshot-based. All DOM-level assertions came
   from the headless run.

---

## 6. Suggested triage

| # | Finding | Severity | Status (2026-08-26) |
|---|---|---|---|
| F-1 | Gate password rotation | — | **Premise corrected**; rotation deliberately deferred during UAT |
| F-2 | Amazon tile view leaks 2 not-yet-effective certs | **High** (partner-facing) | **FIXED — DEF-376**, verified locally |
| F-3 | Empty-scope export returns HTTP 400 | Medium | **FIXED — DEF-377**, verified locally |
| F-4 | Import vs Create auto-number inconsistency | Medium | **FIXED — DEF-378**, verified locally |
| F-5 | Product import template reachable by External | Low | **FIXED — DEF-379**, verified locally |
| F-6 | Novi has no cert associations | Low (data) | Open — seed a few rows on TEST |
| F-7 | Duplicate cert numbers | — | Open — **already DEF-369** |
| — | `GET /Certificate/Delete/{number}` 500s | Low | **Open — DEF-380** (found during cleanup) |

Register high-water was **DEF-375** at the time of the run; it is **DEF-380** after logging these.
All four fixes are in the working tree and **not deployed** — re-run phases 1–3 and 6 against
gccwebapp.com after the next deploy to confirm them there.

---

## 7. How to re-run

```bash
cd Scripts/smoke-prod
node _regression/00-preflight.js        # gate + 6 role logins  (run this first)
node _regression/01-rbac-matrix.js      # 24 routes x 7 roles
node _regression/02-data-scoping.js     # populations + isolation pairs
node _regression/03-exports-downloads.js
node _regression/04-create-edit.js      # WRITES a fixture cert, deletes it in finally
node _regression/06-import.js           # WRITES one imported row, deletes it in finally
node _regression/05-performance.js
```

Results land as JSON in `Scripts/smoke-prod/_regression/_results/`, artifacts (CSV exports, the
fixture PDF, failure screenshots) in `_regression/_artifacts/`.

Environment overrides: `BASE_URL`, `GATE_PASS`, `REG_PASSWORD`, `REG_HEADLESS=0` to watch it run.

Both write phases create only `ZZ_RegressionFixture_*` / `ZZ_RegressionImport_*` rows and remove
them in a `finally` block; if teardown ever fails, the run prints
**`MANUAL CLEANUP REQUIRED`** with the certificate number. Both fixtures were confirmed removed on
this run.

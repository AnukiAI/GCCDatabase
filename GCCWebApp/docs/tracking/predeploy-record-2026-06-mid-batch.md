# Pre-Deploy Gate Record — June 2026 (mid-month batch)

**Instance of** `docs/tracking/pre-deploy-agent-checklist.md` (the 7-gate standard).
**Created:** 2026-06-18. **Status:** ✅ Gates 1–7 PASS (with fixes folded in) — ⬜ Gate 0 (manual upload backup) outstanding; clear to deploy once the bundle is rebuilt and Gate 0 done.
**Deploy target:** gccwebapp.com / greencirclecertified.com (Plesk) — DB `gccwebap_certificates` @ `tcp:68.71.129.120,1533`.

> Purpose: the checklist defines *how* to gate; this file records *whether each gate ran* for this batch, with evidence. Don't deploy until every row is ✅ or has a logged waiver. Archive after the batch ships.

---

## What's in this batch

Source of truth: `docs/tracking/shipped-2026-06.md` (pending-deploy rows) + `defect-register.md`. The deploy is a **rebuild of `…-20260616-1124.zip`** (that zip predates the 6-17 fixes and must be rebuilt to include them).

| Item | Surface | Files | Risk class |
|---|---|---|---|
| **DEF-267** | Cert **save** validation (Edit + Create) | `Models/CertificateEditViewModel.cs` (`[MaxLength(7)]`→`50`) | **HIGH — blocks ALL cert saves on prod** |
| **CR-171** | Multi-partner (cert-level + product-ID-level), data isolation, output scoping | `Controllers/Api/v1/CertificatesController.cs`, `Controllers/Api/CertificateTile{External,Internal}ApiController.cs`, `Controllers/ProductController.cs`, `Controllers/CertificateController.cs`, `Controllers/CertificateImportController.cs`, `Services/ProductIdService.cs`, `Services/Import/CertificateImportService.cs`, `Services/CertificateService.cs`, `Data/ApplicationDbContext.cs`, `Models/CertificateProductId.cs`, `Views/Certificate/{Create,Edit}.cshtml`, `Views/Shared/_ProductIdManager.cshtml`, `wwwroot/js/product-id-manager.js`, **migration `AddProductIdPartnersM2M`** | **HIGH — partner API + data isolation + schema migration + mandatory backfill** |
| **CR-187** | Normalized search (symbols/case/spacing) | **migration `AddNormalizedSearchColumns`** (PERSISTED computed cols + indexes), `SearchNormalizer`, repo search | **schema migration** |
| **DEF-266** | Cert Create custom-fields error toast | `wwwroot/js/certificate-custom-fields.js` | UI / JS |
| **DEF-265** | Cert edit product-dup | `Controllers/CertificateController.cs` (`ResolveProductForCertAsync`) | data-write |
| **DEF-264** | PDF download (App_Data path) | `Controllers/CertificateController.cs` (`Download`/`DownloadSelectedZip`) | file path |
| **CR-186** | Product split (find-or-create per tuple) | cert form + `ProductController` | data-write |

**Risk read:** Gate 1/2 fire (views + JS + shared partial). Gate 3 fires (every business-data controller changed for CR-171). Gate 4 fires (**`/api/v1` changed ~130 lines** for CR-171). Gate 6 fires (`CertificateRepository` + `DbContext` + EF collection projections). Gate 7 fires (**2 new migrations + 2 mandatory backfills**). All gates active for this batch.

---

## Gate status

| Gate | Owner | Fires? | Status | Evidence |
|---|---|---|---|---|
| **0 — Back up server uploads** | manual | Always | ⬜ **TODO (Shawn)** | Zip server `wwwroot/uploads/` (or `App_Data/uploads/`) before publish. Not yet done. |
| **1 — UI / Design-Guide** | `ux-design-compliance` | ✅ yes | ✅ **PASS-with-nits** (2026-06-17) | No native dialogs, no `max-width`, container intact, labels unchanged, multi-select partner control compliant. Pre-existing-only nits: `<h1>` in Create/Edit (existed in HEAD); missing `.partner-novi` badge color (cosmetic, falls back to grey). No new violations. |
| **2 — Cross-role + responsive** | `accessibility-validator` + Playwright | ✅ yes | ✅ **PASS-with-nits** (2026-06-18) | External restrictions **intact** (no Add/Edit, Actions col hidden, no internal tags); Public blocked; multi-select renders across 5 viewports, 0 horizontal scroll; **zero NEW axe violations** (all flagged items pre-date this batch — DEF-244 debt). Screenshots in `Scripts/smoke-prod/_results/screenshots/gate2-def267-cr171/`. |
| **3 — Data-scoping** | `security-specialist` + runtime smoke | ✅ yes | ✅ **PASS (after fix)** (2026-06-18) | Audit: 23 partner filters correctly scalar→M2M; isolation core sound. **One miss found + FIXED:** `Services/CertificateService.cs:509` partner CSV export still used scalar `c.Partner` → co-partner dropped from export; changed to `c.Partners.Any(...)`. Build clean. **Runtime proof on real data:** `cr171-partner-isolation-local.js` **5/5 partner users clean, 0 co-partner leaks** (incl. `pru.multi` Amazon+Novi); `qc182` (fixed) → SCOPED OK 6/6; `def247` field-scope 5/5. |
| **4 — Partner API contract** | `security-specialist` | ✅ yes | ✅ **PASS** (2026-06-17) | `/api/v1/certificates*` JSON shape **byte-identical** (partner stays a scalar string, not an array); Amazon caller sees only its own partner (stricter than before); `status="Active"` + `PublicStatuses` enforced on list/search/GS1/by-id/by-number/PDF. **Advisory (non-blocking):** product-ID `partner` sub-label *value* can change for untagged-shared IDs (Model A) — shape unaffected; flag to Amazon CP2 if they string-match it. |
| **5 — Traceability** | `governance-manager` (inline) | Always | ✅ **PASS** (2026-06-18) | Every changed code file maps to DEF-267 / CR-171 / CR-187 / DEF-264/265/266 / CR-186. No orphans. Gate-3 fix recorded under CR-171. shipped-2026-06.md + defect-register.md + observations_log.md current. |
| **6 — Performance / EF** | manual review | ✅ yes | ✅ **PASS** (2026-06-18) | New `.ThenInclude(pi => pi.Partners)` in `CertificateRepository.GetByIdAsync` rides the existing `.AsSplitQuery()` (DEF-253) — no cartesian. M2M config in `DbContext` is standard. CR-171 collection projections/subqueries confirmed translating at runtime (app boots + serves on restored data). |
| **7 — Pending migration → prod schema** | manual + DB | ✅ yes | ✅ **PASS — artifact ready + validated** (2026-06-18) | Canonical artifact **`docs/tracking/CR-171-CR187-deploy-combined.sql`** (2 migrations + both CR-171 backfills, idempotent, `SET QUOTED_IDENTIFIER ON`, post-deploy verification). **Validated** on a throwaway restore of the live `.bak` at exact prod state (2 behind): apply exit 0, both migrations applied, `certs_with_partner_missing_M2M=0` + `productIds_with_partner_missing_M2M=0`; re-run = clean no-op. **Prod state (live probe 2026-06-17):** last applied `20260605131035_ChangeUserCompanyToFreeText`; `CertificatePartners` exists; `CertificateProductIdPartners` absent. |

---

## Deploy runbook (ORDER MATTERS)

> ⚠️ **Prod config has `AutoMigrateOnStartup=true`** (DEF-252). If the new DLL boots *before* the backfill runs, its startup auto-migrate creates the M2M schema and there's a window where partner logins see **zero certs**. Avoid it by running the SQL first.

1. **Gate 0** — back up server uploads (`wwwroot/uploads/` ↔ `App_Data/uploads/`). Don't skip.
2. **Back up the prod DB** (`gccwebap_certificates`) — full backup before any schema change.
3. **Rebuild the bundle** — Release publish of the *current* working tree (must include DEF-267 + the Gate-3 export fix; the 6-16 `…-1124.zip` is stale). Confirm `dotnet build` 0/0.
4. **Apply the combined SQL FIRST**, while the old app is still up (the old DLL ignores the new columns/tables, so this is safe):
   ```
   sqlcmd -S tcp:68.71.129.120,1533 -U <user> -P <pwd> -d gccwebap_certificates -b \
          -i docs/tracking/CR-171-CR187-deploy-combined.sql
   ```
   Confirm the Section D verification block is all green (both migrations applied, `…missing_M2M = 0`). Harmless CR-187 1700-byte index-key warning is expected.
5. **Swap the DLL: stop app → extract → start** (never extract over a running worker — that caused the DEF-259/260 prod 503). On boot, auto-migrate finds nothing pending (already applied) → no-op; watch the log for `✅ Production database schema up to date`.
6. **Post-deploy verification** (below).

---

## Post-deploy verification

- [ ] Log in as all four test users (Admin / Analyst / External / Public) — no login outage (the DEF-239 canary; check app log for `Invalid column name`, not just the page text).
- [ ] **DEF-267:** edit a real cert with an 8-char number (e.g. `26-10063`) → Save → no `MaxLength(7)` banner.
- [ ] **CR-171 isolation:** log in as a partner principal → sees only own-partner certs; co-partner names absent. (Re-run `cr171-partner-isolation-local.js` with `BASE_URL=https://gccwebapp.com` + a known partner login.)
- [ ] **CR-187:** search a cert by a symbol/case-mangled term (e.g. `3M™`) → found.
- [ ] **DEF-264:** download a cert PDF → real file served.
- [ ] Spot-check the **live 249-cert dataset** (validation here used the 6-15 snapshot = 50 certs; prod has 249 — re-confirm counts/scoping on the full set).
- [ ] `curl -sI https://gccwebapp.com/` security-header sanity (carried-forward DEF-235/237).

---

## Blockers / watch items

1. **⬜ Gate 0 not done** — upload backup is the one outstanding gate.
2. **Bundle must be rebuilt** — `…-1124.zip` predates DEF-267 + the Gate-3 fix.
3. **Exposed prod DB password** — surfaced in this session's transcript (`appsettings.Production.json`); rotate at convenience.
4. **Amazon CP2 advisory** (Gate 4) — product-ID partner sub-label *value* can change (Model A); shape unchanged. Inform Amazon if they string-match it; not a contract break, no sign-off required to deploy.
5. **Data staleness** — gates/smokes validated on the 6-15 backup (50 certs); live is 249. Re-check scoping/counts on prod after deploy.
6. **CR-187 index warning** — `IX_*_Normalized*` can exceed the 1700-byte key limit "for large values"; harmless for real cert#/company names (would only fail on ~850+ char values). Inherent to the CR-187 migration.

---

## Waiver log (for this batch)

| Date | Deploy | Gate waived | Why | Follow-up |
|------|--------|-------------|-----|-----------|
| | | | | |

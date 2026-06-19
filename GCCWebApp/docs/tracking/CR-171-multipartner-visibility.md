# CR-171 — Multi-select Partner on certificates + partner data isolation

- **Status:** **DEPLOYED + PROD-VERIFIED 2026-06-19 (CLOSED).** Live in `GCCWebApp-deploy-20260619-0001.zip`; prod backfill ran (CertificatePartners 1→155, CertificateProductIdPartners 0→992, missing_M2M=0); post-deploy regression 8/8 incl. live partner-isolation check (Amazon login sees 19 scoped certs, no co-partner leak). Phase 1 (cert-level) + Phase 2 (product-ID Model-A, multi-select UI, bulk import), gated (security/UI/governance) with a GS1 value-leak found+fixed. See §11 (Phase 1), §12 (Phase 2). Scope confirmed Model A (§6A).
- **Source:** QC #171 (Carly, 5/5/26) — *"Partner must be multi-select if used for partner login access."* Still "Pending / Did not check" as of 5/29.
- **Refined by:** Shawn, 2026-06-04 — *"The original scope was to only have one partner. Certificates could have more than one partner. If we are sending to Amazon, we would not want to send the other partners."*
- **CONFIRMED by client (Shawn), 2026-06-17** — all 5 sign-off questions answered (§6) **and two scope expansions added**: (1) multi-partner must also exist at the **product-identifier level** (used for API sharing of specific products under a cert by associated partner); (2) **bulk upload is back in scope** — upload template to be revised and the edit popup must offer multi-select. See §6 (answers) and §10 (product-ID scope).
- **Related memory:** [[feedback_partner_data_isolation]], [[feedback_amazon_api_immutable]], [[feedback_external_user_umbrella]], [[feedback_workflow_state_count]], [[feedback_ef_includes_with_projection]].

---

## 1. Why this is a CR, not a defect

Original spec scoped **one partner per certificate** (`Certificate.Partner`, a single string: Amazon / Novi / None / Other). The client now needs a certificate to carry **more than one** partner, *and* — because Partner drives partner-login visibility — each partner must see only **their own** association, never the co-partners. That is a scope expansion of the data model and the visibility rules, not a bug fix.

## 2. Current state (what's already half-built)

| Piece | State today |
|---|---|
| `Certificate.Partner` (single string) | ✅ In use everywhere — drives **all** visibility filters and **all** partner-facing output |
| `Certificate.Partners` (many-to-many) | ⚠️ Exists (join table, repo `.Include`), but **unused by the UI** |
| Create / Edit POST handlers | ⚠️ Already read `model.PartnerIds` (CSV) → populate `Certificate.Partners`, then **collapse** to `Partner = partnerList.First().Name` "for backward compatibility" (`CertificateController.cs:606, 1142`) |
| Create / Edit **form** | ❌ Still a single `<select asp-for="Partner">` (`Create.cshtml:205`) — never posts `PartnerIds`, so the multi-partner branch is dead code from the UI |
| Visibility filters (~9 sites) | ❌ All read the single `c.Partner` string, not the collection |
| Amazon API v1 output | ✅ Returns `partner = c.Partner` (single) — **does not leak today**, because it reads the single field, never the collection |

**Net:** today the app is effectively single-partner. No leak exists *because* nothing reads the collection on the way out. The leak risk is **introduced** the moment access is switched to the collection without also scoping output per-requester.

## 3. The design rule this CR must enforce

> **Access** = "is the caller one of the cert's partners?" → read the `Partners` **collection**.
> **Output** = "show the caller **their own** partner name only" = `cert.Partners ∩ caller's partner names`. **Never** `.First()`, never the full set.

The legacy single `Partner` string stays as a denormalized "primary partner" for internal sort/display/CSV — it is NOT the partner-facing output anymore (output is computed per requester).

---

## 4. Visibility matrix — A. Role baseline (authoritative, current behaviour)

Source of truth: `CertificateController.cs:1929-1931` + `CertificateTileExternalApiController.cs` + `Api/v1/CertificatesController.cs`.

| Capability | Public (anon) | External — Company user | External — Partner user | Partner API key (machine) | Analyst | Admin |
|---|---|---|---|---|---|---|
| **Which certs** | all certs | certs of their **Company**(ies) | certs tagged to **their partner** | certs tagged to **their partner** | all | all |
| **Status filter** | PublicStatuses¹ only, non-expired | PublicStatuses¹ + own company, non-expired | PublicStatuses¹ + own partner, non-expired | PublicStatuses¹ (reported as "Active") + own partner | all 8 states | all 8 states |
| **Partner field shown** | **none** (confirmed) | **all associated** (confirmed) | **their own partner only** | **their own partner only** | all partners | all partners |
| **Internal tags** | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Custom fields** | external-visible only | external-visible only | external-visible only | per contract (CR-013, deferred) | all | all |
| **Add / Edit certificate** | ❌ | ❌ | ❌ | ❌ (read; some write endpoints by contract) | ✅ | ✅ |
| **Actions column (Product Mgmt)** | ❌ | ❌ | ❌ | n/a | ✅ | ✅ |

¹ **PublicStatuses** = `Client Approved - Paid` + `Republished` (`Models/CertificateStatus.cs:64`). All three external-facing roles are capped at these two states + non-expired. Internal roles see all 8.

**What CR-171 changes in this table:** only the **"Which certs"** rule for *Partner user* and *Partner API key* (single string → collection membership) and the **"Partner field shown"** rule (must become requester-scoped). Everything else is unchanged.

## 5. Visibility matrix — B. Multi-partner use cases (intended post-CR behaviour)

Assume the cert is in a PublicStatus + non-expired (so it's eligible for external view at all). "✓ sees" / "✗ blocked" = does the cert appear; "Partner shown" = what value that viewer sees in the partner field.

"Partner shown" values below reflect the 2026-06-17 confirmation: **public = none; company user / external client / analyst / admin = all associated; partner login = own only.**

| # | Cert's partners | Amazon login | Novi login | Other-partner login | Company user (cert's company) | Public | Analyst/Admin |
|---|---|---|---|---|---|---|---|
| UC1 | **[Amazon]** | ✓ sees · "Amazon" | ✗ blocked | ✗ blocked | ✓ · "Amazon" | ✓ · none | ✓ · "Amazon" |
| UC2 | **[Amazon, Novi]** | ✓ sees · **"Amazon"** (not Novi) | ✓ sees · **"Novi"** (not Amazon) | ✗ blocked | ✓ · "Amazon, Novi" | ✓ · none | ✓ · "Amazon, Novi" |
| UC3 | **[None]** | ✗ blocked | ✗ blocked | ✗ blocked | ✓ · none | ✓ · none | ✓ · "None" |
| UC4 | **[Amazon]**, company = Sofidel | ✓ · "Amazon" | ✗ blocked | ✗ blocked | Sofidel user ✓ · "Amazon" | ✓ · none | ✓ |

**The headline cell is UC2.** Today (single string collapsed to first) only **one** of Amazon/Novi sees the cert, and the partner field could show the *wrong* partner. After CR-171 both see it, each labelled with **their own** partner only. That is the whole point of the CR.

## 6. Sign-off questions — ANSWERED by client 2026-06-17

| # | Question | **Client answer (2026-06-17)** |
|---|---|---|
| 1 | Does **Public** (anon) see a Partner value? | **No.** Certificate visible; **no partner names shown** to public. (= option (a)) |
| 2 | Does a **Company user / External client** see co-partners on their own cert? | **Yes — all associated partner names shown.** Same for GCC analyst/admin. Only *partner logins* are scoped to their own name. |
| 3 | Is **"no partner" still valid** (not required)? | **Yes.** A partner association is **not required**; certs may exist with no partner. No min-1 validation. |
| 4 | **Bulk upload** multi-partner — in or out of scope? | **IN scope** (reversed from the earlier "v1 Create/Edit only" recommendation). Driven by the product-ID-level expansion. **Revise the upload template** and ensure the **edit popup offers multi-select**. — *Anuki to add.* |
| 5 | **Amazon API contract** — shape stays single, value requester-scoped? | **Confirmed.** Format to Amazon **unchanged**; Amazon sees **only its own** association. GCC to **coordinate with Amazon** to confirm acceptance of certs that may be shared but appear single-associated on their end. |

**Net effect on output isolation (simpler than first feared):** the *only* requester-scoped output path is the **partner login (UI) + partner API key**. Everyone else is binary — **internal (analyst/admin), company users, and external clients see ALL associated partner names; public sees NONE.** No per-requester intersection is needed for company/external/internal — only for partner principals.

## 6A. Cert-level ↔ product-ID-level interaction — CONFIRMED: **Model A** (Shawn, 2026-06-17)

> **Model A — gate + filter (chosen).** Cert-level partner membership decides *whether* the partner sees the cert at all; product-ID-level membership then decides *which* product IDs within it they receive. A product ID with **no** partner tag = visible to **all** of that cert's partners. Cert-access semantics are unchanged from today; product-ID partner is an optional narrowing refinement.

Build implication: the API access gate stays on `cert.Partners` membership (§7.2); the product-ID projection additionally filters the returned product-ID list to `pid.Partners ∩ caller` **∪ untagged product IDs**. (Model B — product-ID authoritative — was rejected.)

## 7. Build scope (for when/if approved — NOT started)

1. **Forms** — Create + Edit: single-select → multi-select posting `PartnerIds`; pre-select existing partners on Edit. (`Views/Certificate/Create.cshtml`, `Edit.cshtml`; backend handlers already consume `PartnerIds`.)
2. **Access filters (~9 sites)** — `userPartnerNames.Contains(c.Partner)` → `c.Partners.Any(p => userPartnerNames.Contains(p.Name))`, with `.Include(c => c.Partners)` and `.AsSplitQuery()` per [[feedback_ef_includes_with_projection]]:
   - `Api/CertificateTileExternalApiController.cs` (110, 138, 328, 371, 388, 452, 671)
   - `Api/v1/CertificatesController.cs` (157, 188, 363, 414, 650, 743, 938, 973, 1203, 1335, 1371, 1475)
   - `CertificateController.cs:1966`
   - `ProductController.cs` (78, 118, 124, 190)
3. **Output isolation (linchpin)** — partner-facing projections emit requester-scoped partner name: `BuildPartnerCertificateDto` + the 2 inline projections in `Api/v1/CertificatesController.cs` need the caller's partner names passed in.
4. **Data backfill (Gate 7, prod SQL)** — populate the M2M join table from existing single `Partner` values, or every existing cert goes invisible to partner logins on cutover. Per [[project_predeploy_agent_checklist]] this is a prod SQL step run with the deploy (prod does not auto-migrate).
5. **Keep legacy `Partner` string** as denormalized primary-partner for internal sort/display/CSV (`ExportController.cs:289`, sort at `…:874`).
6. **Import — IN scope (confirmed 2026-06-17).** Revise the cert upload template to accept multiple partners per row (and product-ID-level partners — see §10); update the import parser; ensure the **edit popup offers multi-select**. Keep single-partner rows backward-compatible.
7. **Product-ID-level multi-partner** — see §10.
8. **Tests** — Playwright external/partner visibility per [[feedback_playwright_screenshots]]; explicit UC2 cross-partner isolation assertion (Amazon key must NOT see "Novi" anywhere in the payload); product-ID-level isolation test (Amazon API receives only Amazon-tagged product IDs).

## 8. Risk register

| Risk | Mitigation |
|---|---|
| Backfill missed → certs vanish for partner logins on cutover | Gate 7 prod SQL run in same deploy step; verify count parity pre/post |
| Output not scoped → Amazon sees "Novi" (the exact leak Shawn flagged) | §7.3 requester-scoped projection + UC2 negative test |
| Amazon contract shape change | Shape stays single; value scoped — confirm with partner (Q5) |
| EF `.Any()` on M2M under pagination | `.AsSplitQuery()` + stable `.ThenBy(c => c.Id)` per [[feedback_ef_includes_with_projection]] |

## 9. Effort (rough, for approved build)

Forms ~1–2 h · access-filter rewrite + includes ~½ day · output isolation ~2–3 h · backfill script + verify ~2 h · regression/Playwright ~½ day. **Medium.**

**Updated 2026-06-17 with the two expansions:** + product-ID-level multi-partner (data model mirror of cert level: ~½ day model+migration, ~2–3 h API filter/output, ~2 h backfill of existing `CertificateProductId.Partner`) + import template revision & multi-select edit popup (~½ day). **Revised total: ~3–4 days.** Confirm Model A vs B (§6A) before estimating output-isolation precisely.

---

## 10. Product-ID-level multi-partner (added 2026-06-17)

**Driver:** the client needs API sharing of *specific products* under a cert by associated partner — so a single cert tagged [Amazon, Novi] can expose ASIN-1 to Amazon and UPC-2 to Novi.

**Current state:** `CertificateProductId.Partner` is already a **single nullable string** (`Models/CertificateProductId.cs:32`), and `ProductIdImportItem.Partner` (`:183`) + `BulkProductIdViewModel.DefaultPartner` (`:172`) are single. So this is the **same single-string → collection** change as the cert level, one layer down.

**Scope:**
1. **Model** — add a product-ID ↔ partner association (M2M, mirroring `Certificate.Partners`), or keep `Partner` denormalized + add `Partners` collection. Migration + matching prod SQL (Gate 7).
2. **API output isolation** — partner-scoped product-ID filtering: a partner principal receives only the product IDs tagged to them (interaction with cert-level access = §6A Model A vs B — **needs direction**).
3. **UI** — product-ID add/edit row gains multi-select partner (replacing the single dropdown).
4. **Import** — upload template gains per-product-ID partner column(s); parser maps to the collection.
5. **Backfill** — populate the new product-ID partner association from existing `CertificateProductId.Partner` values, else product IDs vanish for partner logins on cutover (same risk as cert-level backfill).
6. **Status interplay** — product-ID `Status` already auto-derives from the parent cert (CR-015, [[feedback_product_id_status_follows_cert]]); partner association is independent of status — confirm no coupling.

---

## 11. AS-BUILT — Phase 1 (cert-level), 2026-06-17

**Built (build clean, localhost-verified):**
1. **Forms** — `Views/Certificate/Create.cshtml` + `Edit.cshtml`: single `<select asp-for="Partner">` → `<select id="partnerSelect" multiple>` posting the hidden CSV `PartnerIds` (a tiny sync script keeps it current). Edit pre-selects existing partners. Backend POST handlers already consumed `PartnerIds` (unchanged).
2. **Access filters → collection membership** (`userPartnerNames.Contains(c.Partner ?? "None")` → `c.Partners.Any(p => userPartnerNames.Contains(p.Name))`), 23 sites across `Api/v1/CertificatesController.cs`, `Api/CertificateTileExternalApiController.cs`, `Api/CertificateTileInternalApiController.cs`, plus `ProductController.cs` (4) and `CertificateController.cs` `FileAccessAllowed` (now reads the M2M from the DB, not the denorm primary). Single-cert in-memory checks got `.Include(c => c.Partners)`.
3. **Output isolation (leak fix):**
   - `BuildPartnerCertificateDto(Certificate, callerPartnerNames)` — emits only the caller's own partner; co-partner product-ID labels suppressed.
   - v1 list endpoints + GS1/by-product-ID: partner callers get caller-scoped partner (`ownPartner` correlated subquery / in-memory intersection); non-partner callers keep the primary.
   - External tile: partner caller → own; company/internal → all associated (joined). Filter dropdown reads the M2M and scopes partner callers to own.
   - Internal tile: shows ALL associated names (joined).
4. **Backfill** — `docs/tracking/CR-171-backfill-partners.sql` (idempotent). **Gate 7: must run with the deploy** (prod has no join rows yet → certs would vanish for partner logins). Verified locally: 8 rows inserted, 0 unmatched, re-run = 0.

**Localhost verification (cert 26-0001 made multi-partner [Amazon, Novi]):**
- Amazon partner key → list + by-id: `partner = "Amazon"`, **"Novi" absent from entire payload**.
- Novi partner key → `partner = "Novi"`, only Novi in `partners seen`. (Reciprocal.)
- Admin internal tile → `Partner = "Amazon, Novi"` (see-all).
- Edit form → both Amazon + Novi pre-selected in the multi-select.

**Known minor follow-up:** partner-column SORT still keys on the denormalized primary `Certificate.Partner` (the displayed joined list may not lead with the sort key). Low priority; not one of the QC-flagged sort columns.

## 12. Phase 2 — product-ID level + import

### 12a. Data + API layer — BUILT + LOCALHOST-VERIFIED 2026-06-17
- **Model**: `CertificateProductId.Partners` (M2M) + join `CertificateProductIdPartners`; denorm `Partner` kept as primary. Migration `20260617200729_AddProductIdPartnersM2M` (applied to dev).
- **Model-A filtering** (partner caller receives untagged product IDs + those tagged to them; co-partner-only IDs dropped; label = caller's own tag, blank for untagged). Helpers `ProductIdVisibleToCaller` / `ScopedProductIdPartner` in v1; wired into the list projection (EF inline), GS1/by-product-ID, and `BuildPartnerCertificateDto`. ProductIds includes got `.ThenInclude(pi => pi.Partners)` + `.AsSplitQuery()` on paged paths per [[feedback_ef_includes_with_projection]].
- **Write path**: `CertificateController.SyncProductIdPartnersAsync` keeps `pid.Partners` in step with the denorm `Partner` on Create + Edit save (parses comma/pipe — forward-compatible with a multi-select UI). `CertificateRepository.GetByIdAsync` now `.ThenInclude(pi => pi.Partners)`.
- **Backfill / prod SQL**: `docs/tracking/CR-171-phase2-productid-partners.sql` (idempotent table create + history row + backfill from `CertificateProductId.Partner`). **Gate 7 — run with deploy.**
- **Verified** (cert 26-0001, product IDs: AMZNONLY01→Amazon, NOVIONLY01→Novi, SHARED0001→untagged): Amazon key sees `[AMZNONLY01, SHARED0001]` (NOT the Novi UPC); Novi sees `[NOVIONLY01, SHARED0001]` (NOT the Amazon ASIN); labels caller-scoped; on both list and by-id. Build clean; EF translation OK at runtime.

### 12b. Product-ID UI multi-select + import — BUILT + LOCALHOST-VERIFIED 2026-06-17
- **Product ID Manager** (`wwwroot/js/product-id-manager.js`): partner control → `<select multiple>` (dynamic options from `ViewBag.Partners`, `_ProductIdManager.cshtml`). Helpers `getPartnerList`/`setSelectedPartners`/`getSelectedPartners`; populate, save (posts pipe-delimited `Partner`), badge (multiple badges), filter (membership), reset (clears multi-select), CSV export (pipe-joined) all updated.
- **Product ID API round-trip**: `ProductIdResponseDto.Partners` (list) + GET includes (`ProductIdService` GetByCertificate/paged/GetById `.Include(Partners)`); `SyncPartnersAsync` on Create + Update (Update now loads `.Include(Partners)`); bulk import resolves the delimited Partner cell via a preloaded partner map.
- **Cert bulk import** (`Services/Import/CertificateImportService.cs`): `SyncCertificatePartnersAsync` on create + update paths (update loads `.Include(c => c.Partners)`); template (`CertificateImportController.DownloadTemplate`) Partner header now hints the pipe-delimited multi format. Header prefix-matching already resolves the hinted column.
- **Verified**: Edit page renders the product-ID `<select multiple>` with Amazon/Novi/Other/Wayfair; GET `/api/productids/certificate/2` returns `partners[]`; PUT `Partner:"Amazon|Novi"` persists both rows to `CertificateProductIdPartners` (denorm primary = first); after tagging, the Novi key's API response gains that product ID (write→read consistency); cert import template downloads with the hinted Partner column. Build clean. (Full cert bulk-import E2E not run — covered by build + logic parity with the verified product-ID sync.)

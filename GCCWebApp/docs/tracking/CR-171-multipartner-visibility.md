# CR-171 — Multi-select Partner on certificates + partner data isolation

- **Status:** **Pending — awaiting client sign-off.** Nothing built. Scope + visibility matrix documented for review.
- **Source:** QC #171 (Carly, 5/5/26) — *"Partner must be multi-select if used for partner login access."* Still "Pending / Did not check" as of 5/29.
- **Refined by:** Shawn, 2026-06-04 — *"The original scope was to only have one partner. Certificates could have more than one partner. If we are sending to Amazon, we would not want to send the other partners."*
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
| **Partner field shown** | ⚠️ open Q (§6) | ⚠️ open Q (§6) | **their own partner only** | **their own partner only** | all partners | all partners |
| **Internal tags** | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Custom fields** | external-visible only | external-visible only | external-visible only | per contract (CR-013, deferred) | all | all |
| **Add / Edit certificate** | ❌ | ❌ | ❌ | ❌ (read; some write endpoints by contract) | ✅ | ✅ |
| **Actions column (Product Mgmt)** | ❌ | ❌ | ❌ | n/a | ✅ | ✅ |

¹ **PublicStatuses** = `Client Approved - Paid` + `Republished` (`Models/CertificateStatus.cs:64`). All three external-facing roles are capped at these two states + non-expired. Internal roles see all 8.

**What CR-171 changes in this table:** only the **"Which certs"** rule for *Partner user* and *Partner API key* (single string → collection membership) and the **"Partner field shown"** rule (must become requester-scoped). Everything else is unchanged.

## 5. Visibility matrix — B. Multi-partner use cases (intended post-CR behaviour)

Assume the cert is in a PublicStatus + non-expired (so it's eligible for external view at all). "✓ sees" / "✗ blocked" = does the cert appear; "Partner shown" = what value that viewer sees in the partner field.

| # | Cert's partners | Amazon login | Novi login | Other-partner login | Company user (cert's company) | Public | Analyst/Admin |
|---|---|---|---|---|---|---|---|
| UC1 | **[Amazon]** | ✓ sees · "Amazon" | ✗ blocked | ✗ blocked | ✓ · §6 | ✓ · §6 | ✓ · "Amazon" |
| UC2 | **[Amazon, Novi]** | ✓ sees · **"Amazon"** (not Novi) | ✓ sees · **"Novi"** (not Amazon) | ✗ blocked | ✓ · §6 | ✓ · §6 | ✓ · "Amazon, Novi" |
| UC3 | **[None]** | ✗ blocked | ✗ blocked | ✗ blocked | ✓ · §6 | ✓ · §6 | ✓ · "None" |
| UC4 | **[Amazon]**, company = Sofidel | ✓ · "Amazon" | ✗ blocked | ✗ blocked | Sofidel user ✓ · §6 | ✓ · §6 | ✓ |

**The headline cell is UC2.** Today (single string collapsed to first) only **one** of Amazon/Novi sees the cert, and the partner field could show the *wrong* partner. After CR-171 both see it, each labelled with **their own** partner only. That is the whole point of the CR.

## 6. Open questions for client sign-off

1. **Does Public see a Partner value at all?** For an anonymous viewer there is no "requester" to scope to. Options: (a) public sees **no** partner field, (b) public sees the **full** list "Amazon; Novi", (c) public sees nothing. Isolation principle leans to **(a)/(c)**. *Client to confirm.*
2. **Does a Company user see co-partners on their own cert?** A Sofidel user viewing their company's cert tagged [Amazon, Novi] — do they see both partner names, or none? They own the cert but the values may still be commercially sensitive. *Client to confirm.*
3. **Does the cert require ≥1 partner, or is "no partner" still valid?** (Affects form validation + whether `Partner = None` stays a first-class option.)
4. **CSV import multi-partner** — should the import template accept multiple partners per cert row, or is multi-partner Create/Edit-only for v1? (Recommend: Create/Edit only for v1.)
5. **Amazon API contract** — confirmed the JSON **shape stays single** `partner` field (per [[feedback_amazon_api_immutable]]); the only change is *which* single value (requester-scoped, not `.First()`). Needs Amazon-side awareness that a cert may now be shared but they still receive only their own label. *Carly → Kyle to confirm no contract objection.*

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
6. **Import** — out of scope for v1 unless §6 Q4 says otherwise.
7. **Tests** — Playwright external/partner visibility per [[feedback_playwright_screenshots]]; explicit UC2 cross-partner isolation assertion (Amazon key must NOT see "Novi" anywhere in the payload).

## 8. Risk register

| Risk | Mitigation |
|---|---|
| Backfill missed → certs vanish for partner logins on cutover | Gate 7 prod SQL run in same deploy step; verify count parity pre/post |
| Output not scoped → Amazon sees "Novi" (the exact leak Shawn flagged) | §7.3 requester-scoped projection + UC2 negative test |
| Amazon contract shape change | Shape stays single; value scoped — confirm with partner (Q5) |
| EF `.Any()` on M2M under pagination | `.AsSplitQuery()` + stable `.ThenBy(c => c.Id)` per [[feedback_ef_includes_with_projection]] |

## 9. Effort (rough, for approved build)

Forms ~1–2 h · access-filter rewrite + includes ~½ day · output isolation ~2–3 h · backfill script + verify ~2 h · regression/Playwright ~½ day. **Medium.**

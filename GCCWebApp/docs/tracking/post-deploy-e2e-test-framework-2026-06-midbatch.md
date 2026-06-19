# Post-Deployment E2E Test Framework — Mid-June 2026 Release

**Release under test:** DEF-267 (cert-save validation) · CR-171 (multi-partner: cert + product-ID level, data isolation) · CR-187 (normalized search) · DEF-264 (PDF download) · DEF-265 (cert-edit product de-dup) · DEF-266 (custom-fields toast) · CR-186 (product split)
**Build artifact:** `GCCWebApp-deploy-20260619-0001.zip`
**Target env:** https://gccwebapp.com (pre-launch test/UAT) · Local mirror: http://localhost:5101
**Prod DB:** `gccwebap_certificates` — schema migrated (`AddNormalizedSearchColumns`, `AddProductIdPartnersM2M`); CR-171 backfill applied (CertificatePartners=155, CertificateProductIdPartners=992).
**Owner:** QA Lead · **Created:** 2026-06-19

---

## 0. How to use this document

- Each **test case** uses the standard 12-column table. Fill **Actual Result / Status / Defect ID** during execution. Leave the rest as the spec.
- **Status** values: `Not Run` · `Pass` · `Fail` · `Blocked` · `N/A` · `Deferred`.
- **Severity** (for failures): `S1-Critical` (data loss / security / core flow down / no workaround) · `S2-High` (major function broken, workaround exists) · `S3-Medium` (minor function / cosmetic-with-impact) · `S4-Low` (cosmetic / nit).
- Log every `Fail` in the **Defect Log** (§12) with a DEF-XXX id and cross-reference it in the test row.
- **Read-only vs write:** gccwebapp.com is pre-launch UAT. Prefer read-only verification on prod; perform destructive/write E2E on the **local mirror** or on **clearly-marked throwaway test records** (prefix `ZZTEST-`). Never bulk-mutate real UAT records without a CR + backup.

### Test accounts & access
| Role | Login | Password | Scope |
|---|---|---|---|
| Admin | `admin` | `TestPass123!` | Full |
| Analyst | `analyst` | `TestPass123!` | Create/Edit certs, no admin |
| External (company) | `external` | `TestPass123!` | Own company data only |
| Partner (Amazon) | `partner@amazon.com` | `TestPass123!` | Amazon-tagged data only |
| Partner (Novi) | `perry.novi@example.com` | `TestPass123!` | Novi only |
| Partner (multi) | `pru.multi@example.com` | `TestPass123!` | Amazon + Novi |
| Site gate (test env) | `gcc` | `Cert-Test-Access-2026!` | HTTP basic-auth wall in front of gccwebapp.com |
| Partner API | `X-Api-Key` header | (per-partner key) | `/api/v1/certificates*` |

### Entry criteria
- [ ] Build `…-0619-0001.zip` deployed; app pool recycled; site reachable behind the gate.
- [ ] Schema migrations applied + CR-171 backfill confirmed (`missing_M2M = 0`).
- [ ] All four standard logins authenticate (DEF-239 login canary).
- [ ] This framework reviewed; testers assigned per area.

### Exit criteria
- [ ] 100% of S1/S2 test cases executed; **zero open S1, zero open S2** (or each waived with sign-off).
- [ ] All matrices (field/business/integration/security) executed.
- [ ] Regression checklist green.
- [ ] Go/No-Go (§15) signed.

---

## 1. Deployment-Success Smoke (run FIRST — gate to full E2E)

| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| DS-01 | Deploy | Site reachable | Deploy done | — | GET `/` behind gate | HTTP 200, GreenCircle home renders | | Not Run | S1 | | Auto: `midjune-batch-prod-regression.js` |
| DS-02 | Deploy | All 4 logins work (DEF-239 canary) | — | 4 accounts | Log in as admin/analyst/external/partner | Each lands authenticated; **check app log for `Invalid column name`** if any fail | | Not Run | S1 | | Generic "invalid password" can mask a schema break |
| DS-03 | Deploy | Schema present | — | — | Internal tile API `/api/certificate-tiles-internal?page=1&pageSize=10` as admin | HTTP 200, `totalCount=249`, no 500 | | Not Run | S1 | | Proves CR-187 `Normalized*` + CR-171 `Partners` columns exist |
| DS-04 | Deploy | Backfill applied | — | — | DB: `SELECT COUNT(*) FROM CertificatePartners` / `CertificateProductIdPartners` | 155 / 992; `missing_M2M = 0` | | Not Run | S1 | | If 0 → partner certs invisible |
| DS-05 | Deploy | Static assets current | — | — | Load `/js/product-id-manager.js`, `/js/certificate-custom-fields.js` | New build served (no stale cache); 200 | | Not Run | S2 | | Hard-refresh; check fix markers |
| DS-06 | Deploy | No console errors on key pages | — | — | Open home, tile view, cert detail; check browser console | No uncaught JS errors | | Not Run | S3 | | |

---

## 2. End-to-End Test Scenarios

### 2.1 Authentication & RBAC (AU)
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| AU-01 | Auth | Valid admin login | — | admin/TestPass123! | Login | Lands on dashboard/internal tile | | Not Run | S1 | | |
| AU-02 | Auth | Invalid password rejected | — | admin/wrong | Login | "Invalid email or password"; no lockout bypass | | Not Run | S2 | | |
| AU-03 | RBAC | Analyst cannot reach `/Admin/*` | analyst in | — | Navigate `/Admin/UserManagement` | 302/403 AccessDenied | | Not Run | S1 | | |
| AU-04 | RBAC | External sees no Add/Edit cert buttons | external in | — | Open external tile view | No Add/Edit affordance; no Actions column on Product Mgmt; no internal tags | | Not Run | S1 | | Core external restriction |
| AU-05 | RBAC | External scoped to own company | external in | — | Open cert list | Only own company's certs visible (list + detail + download + export) | | Not Run | S1 | | Audit adjacent endpoints, not just list |
| AU-06 | RBAC | Partner scoped to own partner | partner@amazon.com in | — | Open external tile view | Only Amazon-tagged certs; no co-partner names anywhere | | Not Run | S1 | | CR-171 core |
| AU-07 | Auth | Logout invalidates session | logged in | — | Logout → back button | Cannot access protected pages | | Not Run | S2 | | |
| AU-08 | RBAC | Admin/admin-role cannot be deleted | admin in | — | Try delete admin user/role | Blocked with message | | Not Run | S2 | | |

### 2.2 Certificate CRUD & DEF-267 (CC)
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| CC-01 | Cert/Create | Create cert (happy path) | analyst in | Company, CertType, Product, dates, status Draft | Create → fill required → Save | Cert created; auto-numbered `YY-NNNNN`; toast success; lands on detail | | Not Run | S1 | | |
| CC-02 | Cert/Edit · **DEF-267** | Save cert with 8-char number | analyst in | cert `26-10063` (8 chars) | Edit → Save Changes (no field change) | **Saves; NO "maximum length of '7'" banner**; redirect to view | | Not Run | S1 | | The exact DEF-267 regression |
| CC-03 | Cert/Edit · DEF-267 | Save cert with 5-digit sequence | analyst in | any `26-1xxxx` | Edit → change a field → Save | Saves successfully | | Not Run | S1 | | |
| CC-04 | Cert/Edit | Required-field validation | analyst in | blank Company | Save with Company empty | Inline validation; not saved | | Not Run | S2 | | See Field Matrix §3 |
| CC-05 | Cert/Detail | Cert list shows **certificate number**, not Id | any | — | Open cert list/tiles | Number column shows `YY-NNNNN`, never the DB Id | | Not Run | S2 | | Long-standing requirement |
| CC-06 | Cert/Delete | Delete cert uses modal + toast | admin in | ZZTEST cert | Delete | `AdminCore.showConfirmation()` modal BEFORE, toast AFTER; **no native confirm()** | | Not Run | S2 | | Local/throwaway only |
| CC-07 | Cert/Create | Certification Results optional field | analyst in | results "92%" | Create with Certification Results | Saved; renders as entered (percentage on partner surfaces) | | Not Run | S3 | | New optional field |
| CC-08 | Cert/Edit | Pathway (Amazon CPF) selectable | analyst in | Pathway value | Edit → set Pathway → Save | Persists | | Not Run | S3 | | |

### 2.3 Workflow / Status (WF) — 8 states
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| WF-01 | Workflow | Valid status transition | analyst in | Draft cert | Move Draft→…→Client Approved – Paid | Allowed per `CertificateStatus.cs`; history recorded | | Not Run | S2 | | 8 states; do NOT alter workflow |
| WF-02 | Workflow | Public visibility = ClientApprovedPaid + Republished | — | certs in each status | Open public tile view | Only `Client Approved – Paid` + `Republished` appear publicly | | Not Run | S1 | | Status leak = security |
| WF-03 | Workflow | Internal statuses hidden from partner/external | partner in | Draft/Hold cert | Partner API + tile | Only `status="Active"` literal; no Draft/Hold/internal | | Not Run | S1 | | PublicStatuses filter |
| WF-04 | Workflow | Expiration auto-status | — | cert past Expiration Date | Wait for sweep / trigger | Status → Expired | | Not Run | S3 | | Background service |
| WF-05 | Workflow | Product ID status follows parent cert (CR-015) | analyst in | cert + product IDs | Change cert status | Product ID status auto-derives; not user-set | | Not Run | S3 | | |

### 2.4 Product Management & Product IDs (PM) — CR-186, DEF-265
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| PM-01 | Product · **DEF-265** | Edit cert's Product Type does NOT spawn duplicate | analyst in | sole-owner cert | Edit cert → change Product Type/Subcat → Save | Product line item **updated in place**; product count unchanged; no 0-cert orphan | | Not Run | S2 | | DEF-265 regression |
| PM-02 | Product · CR-186 | Split per (Company+Name+Category+Type+Subcat) | analyst in | shared product | Edit one cert's tuple | New line item only when tuple truly differs / shared by others | | Not Run | S2 | | CR-186 design |
| PM-03 | ProductID | Add ASIN/UPC/GTIN/EAN | analyst in | valid ASIN | Add Product ID | Saved; appears on cert + Product ID Manager | | Not Run | S2 | | |
| PM-04 | ProductID | Multi-value cell display order matches sort | — | cert w/ multiple ASINs | View cert list | Displayed order == sort order; leading value == sort key | | Not Run | S3 | | DEF-248 regression |
| PM-05 | ProductID | Product ID immediate save independent of cert edit | analyst in | — | Add/edit/delete a Product ID, then Cancel cert edit | Product ID change persists (saves immediately) | | Not Run | S3 | | Per UI hint |
| PM-06 | Product | External has NO Actions column | external in | — | Open `/Product/Management` | No Actions column; no edit affordance | | Not Run | S1 | | |

### 2.5 Multi-Partner — CR-171 (MP)  ← primary release feature
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| MP-01 | CR-171/UI | Cert Create partner multi-select | analyst in | Amazon + Novi | Create cert → select 2 partners → Save | Both partners saved (M2M); detail shows both | | Not Run | S2 | | `#partnerSelect` multiple |
| MP-02 | CR-171/UI | Cert Edit pre-selects existing partners | analyst in | multi-partner cert | Open Edit | Existing partners pre-checked in multi-select | | Not Run | S2 | | |
| MP-03 | CR-171/Isolation | Amazon partner sees only Amazon certs | partner@amazon.com in | shared Amazon+Novi cert | View tiles + API | Sees the cert; **"Novi" appears nowhere** in payload/UI | | Not Run | S1 | | The leak risk |
| MP-04 | CR-171/Isolation | Novi partner sees only Novi | perry.novi in | same shared cert | View tiles + API | Sees cert as Novi; Amazon absent | | Not Run | S1 | | |
| MP-05 | CR-171/Isolation | Multi-partner login sees union (own only) | pru.multi in | — | View tiles | Sees Amazon + Novi certs; **never Wayfair/Other** | | Not Run | S1 | | Dedup: shared cert once |
| MP-06 | CR-171/Internal | Internal/Company/Analyst/Admin see ALL partner names | admin in | multi-partner cert | View internal tile/detail | All associated partner names shown | | Not Run | S2 | | |
| MP-07 | CR-171/Public | Public sees NO partner names | anon | — | Public tile view | No partner names rendered | | Not Run | S1 | | |
| MP-08 | CR-171/ProductID | Product-ID-level partner tag scopes API (Model A) | partner@amazon.com (API) | product ID tagged Novi on an Amazon cert | Call `/api/v1/certificates` | Amazon sees the cert; Novi-tagged product ID suppressed; untagged shared | | Not Run | S1 | | Phase 2 isolation |
| MP-09 | CR-171/Data | No cert lost its partner post-backfill | — | — | Compare partner cert counts pre/post deploy | Every cert with a `Partner` value visible to that partner | | Not Run | S1 | | `missing_M2M=0` |
| MP-10 | CR-171/Export | Partner CSV export scoped via M2M | partner in | — | Export from external view | Only own-partner certs; co-partner certs excluded; no co-partner names | | Not Run | S1 | | DEF gate-3 fix (`CertificateService:509`) |

### 2.6 Search — CR-187 (SR)
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| SR-01 | Search | Symbol-insensitive | any | "3M™" | Search certs by company | Finds "3M" cert(s) | | Not Run | S2 | | |
| SR-02 | Search | Case-insensitive | any | "amazon" / "AMAZON" | Search | Same results regardless of case | | Not Run | S3 | | |
| SR-03 | Search | Spacing/punctuation-insensitive | any | "ZZ-CR187", "ZZ CR187" | Search product | Both find the same product | | Not Run | S3 | | |
| SR-04 | Search | Cert number search | any | `26-0173` | Search | Finds the cert | | Not Run | S2 | | NormalizedNumber |
| SR-05 | Search | Autocomplete (product) | analyst in | partial name | Type in product autocomplete | Matches surface | | Not Run | S3 | | |
| SR-06 | Search | No false matches | any | unrelated term | Search | Returns nothing (no over-match) | | Not Run | S3 | | |

### 2.7 PDF & Files — DEF-264 (PDF)
| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| PDF-01 | PDF · **DEF-264** | Single cert PDF download | any w/ access | cert w/ PDF | Click download | Real uploaded PDF served (from App_Data) — not "No PDF uploaded" | | Not Run | S1 | | DEF-264 regression |
| PDF-02 | PDF · DEF-264 | Bulk ZIP download | admin in | multiple certs | Select → Download ZIP | ZIP contains real PDFs (not placeholders) | | Not Run | S2 | | |
| PDF-03 | PDF/Security | Bulk ZIP respects scope (IDOR) | external in | request range incl. other-company ids | Bulk download | Only authorized certs in ZIP | | Not Run | S1 | | DEF-236 guard |
| PDF-04 | PDF | Upload new PDF on Edit | analyst in | new PDF | Edit → upload → Save | Replaces; thumbnail regenerated | | Not Run | S3 | | |
| PDF-05 | PDF | Cert preview thumbnail renders | any | cert w/ PDF | Open detail | Image/preview shows | | Not Run | S3 | | |

---

## 3. Field Validation Matrix (FV)

| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| FV-01 | Cert # | Max length now 50 (DEF-267) | Edit form | 8–50 char number | Save | Accepts up to 50 | | Not Run | S1 | | Was 7 |
| FV-02 | Cert # | Required | Create | blank | Save | Rejected, "required" | | Not Run | S2 | | |
| FV-03 | Cert # | Uniqueness | Create | existing number | Save | Rejected (unique index) | | Not Run | S2 | | |
| FV-04 | Company | Required | Create | blank | Save | Rejected | | Not Run | S2 | | |
| FV-05 | Effective Date | Required + valid date | Create | blank / invalid | Save | Rejected | | Not Run | S2 | | |
| FV-06 | Expiration Date | After Effective Date | Create | exp < eff | Save | Rejected or flagged | | Not Run | S2 | | Confirm rule |
| FV-07 | Status | Required, valid enum | Create | — | Save | Only the 8 valid states | | Not Run | S2 | | |
| FV-08 | Certification Results | Optional, free text, max len | Create | 1000+ chars | Save | Honors max length; optional | | Not Run | S3 | | MaxLength(1000) |
| FV-09 | Partner(s) | Optional, multi-select | Create | none / many | Save | "no partner" allowed; multiple allowed | | Not Run | S2 | | CR-171 |
| FV-10 | Company URL | Optional, URL format | Create | bad url | Save | Validated or accepted-as-text per spec | | Not Run | S3 | | |
| FV-11 | Company Email | Optional, email format | Create | bad email | Save | Validation per spec | | Not Run | S3 | | |
| FV-12 | Product ID (ASIN) | Format/length | Add product ID | invalid ASIN | Save | Validation per type | | Not Run | S3 | | ASIN/UPC/GTIN/EAN differ |
| FV-13 | Custom field (required) | Enforced | cert type w/ required custom field | blank | Save | Rejected | | Not Run | S3 | | |
| FV-14 | Custom field (null type) | Renders safely (DEF-266) | cert type w/ malformed field | — | Select cert type on Create | Defaults to text; no "Error loading custom fields" toast | | Not Run | S2 | | DEF-266 regression |
| FV-15 | Min font 16px / mobile | No iOS zoom | mobile viewport | — | Focus inputs | Inputs ≥16px | | Not Run | S4 | | Design guide |

---

## 4. Business Rule Validation Matrix (BR)

| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| BR-01 | Visibility | Public = ClientApprovedPaid + Republished only | certs across states | — | Public tile | Other 6 states never public | | Not Run | S1 | | |
| BR-02 | Partner scope | Partner sees only own-partner certs | multi-partner data | — | Partner login/API | Co-partner data never exposed | | Not Run | S1 | | CR-171 |
| BR-03 | External scope | External sees only own company | — | — | External login | Cross-company never exposed | | Not Run | S1 | | |
| BR-04 | Product ID status | Derives from parent cert (CR-015) | — | — | Change cert status | Product ID status follows; not editable | | Not Run | S3 | | |
| BR-05 | Product split | One line item per Company+Name+Category+Type+Subcat | — | — | Create/edit products | Matches CR-186 model | | Not Run | S2 | | |
| BR-06 | Cert numbering | Auto-generated `YY-NNNNN`, unique, immutable on edit | — | — | Create; then edit | Number generated; "cannot be changed" on edit | | Not Run | S2 | | |
| BR-07 | Amazon API contract | Response shape immutable | — | — | Compare `/api/v1` JSON to baseline | Byte-identical shape; `status="Active"`; results as % | | Not Run | S1 | | Needs partner sign-off to change |
| BR-08 | Custom field visibility | Internal-only fields hidden from partner/external export | Internal custom field | — | Partner CSV export | Internal field absent | | Not Run | S1 | | DEF-247 |
| BR-09 | Tags | Internal tags hidden from external | internal tag on cert | — | External detail | Internal tag not shown | | Not Run | S2 | | |
| BR-10 | Partners set | Core entities Amazon/Novi/Other; "no partner" valid | — | — | Create cert w/o partner | Allowed | | Not Run | S3 | | |
| BR-11 | Client-requested labels immutable | — | — | — | Inspect labels/badges | Copy unchanged from spec | | Not Run | S3 | | Fix scope, not labels |

---

## 5. Integration Test Matrix (IN)

| Test ID | Area | Scenario | Preconditions | Test Data | Steps | Expected Result | Actual Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| IN-01 | Partner API (Amazon) | List certs by API key | valid Amazon key | — | GET `/api/v1/certificates` w/ `X-Api-Key` | 200; only Amazon certs; `status="Active"`; correct shape | | Not Run | S1 | | |
| IN-02 | Partner API | By GTIN / by product-id | Amazon key | known GTIN | GET GS1/by-product-id endpoints | Scoped result; co-partner product IDs suppressed | | Not Run | S1 | | CR-171 Phase 2 |
| IN-03 | Partner API | By certificate number | Amazon key | `26-xxxxx` | GET by-number | Scoped; PublicStatuses only | | Not Run | S1 | | |
| IN-04 | Partner API | PDF endpoint scoped | Amazon key | other-partner cert | GET pdf | 404/denied for non-owned | | Not Run | S1 | | |
| IN-05 | Partner API | Invalid/expired key | bad key | — | GET `/api/v1/certificates` | 401 | | Not Run | S2 | | |
| IN-06 | Partner API | CertificationResults as % | Amazon key | cert w/ results | GET | Percentage, not raw decimal | | Not Run | S2 | | |
| IN-07 | Tile API (internal) | Pagination honored | admin in | pageSize=200 | GET internal tiles | Returns requested page size (not capped at 20) | | Not Run | S2 | | DEF-249 |
| IN-08 | Tile API (external) | Auth-gated | anon | — | GET `/api/CertificateTileExternal` | 401 (no anon data) | | Not Run | S1 | | |
| IN-09 | Email | Notification email send | email configured | — | Trigger an email event | Sent (or MockEmailService no-op on test) — confirm config | | Not Run | S2 | | DEF-263: prod falls back to mock |
| IN-10 | Azure AD B2C | External SSO login (if enabled) | B2C configured | — | Login via B2C | Authenticates | | Not Run | S2 | | Confirm if in scope |
| IN-11 | CSV Import | Cert/Tags/Product ID import | admin in | template file | Import | Rows created; UTF-8 BOM intact (no Excel mojibake) | | Not Run | S2 | | DEF-232 |
| IN-12 | Google Analytics | Tracking fires (if enabled) | GA configured | — | Load public page | GA event sent | | Not Run | S4 | | |

---

## 6. Regression Test Checklist (RG)

Run after primary E2E; confirms the release did not break shipped functionality.

| Test ID | Area | Scenario | Expected Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|
| RG-01 | Cert list sort | Blank values sort last (DEF-242) | Blanks trail in both directions | Not Run | S3 | | |
| RG-02 | ID column sort | ASIN/UPC/GTIN/EAN sort by count badge (DEF-260) | Count-based order; empties last | Not Run | S3 | | |
| RG-03 | Cert subcats sort | Display order == sort (DEF-258) | First shown == ordered-on | Not Run | S3 | | |
| RG-04 | Product Type re-parent | Move type/subcat to new parent (DEF-230) | Works | Not Run | S3 | | |
| RG-05 | Quick-add product categorization | Requires category (DEF-231/243) | Blocks until category chosen; sticks on edit | Not Run | S2 | | |
| RG-06 | User Management | List loads fast, Company Access incl. "All" (DEF-251/QC106) | <1s; "All" assigns every company | Not Run | S3 | | |
| RG-07 | Security headers | Single Permissions-Policy etc. (DEF-107/235) | One of each header | Not Run | S3 | | `curl -sI` |
| RG-08 | Nav spacing all roles | 32px spacer on all 4 logins (DEF-227) | Consistent | Not Run | S4 | | |
| RG-09 | Infinite scroll / tiles | Loads more on scroll | Works | Not Run | S3 | | |
| RG-10 | Shareable cert URLs | Direct cert link resolves | Works for public certs | Not Run | S3 | | |
| RG-11 | CR-184 export | Active product identifiers CSV | Admin/Analyst download; External 403 | Not Run | S2 | | |
| RG-12 | Existing single-partner certs | Pre-CR-171 certs still display partner | Partner shown correctly post-backfill | Not Run | S1 | | |
| RG-13 | Responsive | 5 viewports, no horizontal scroll | Clean desktop→mobile | Not Run | S3 | | |
| RG-14 | No native dialogs anywhere | alert/confirm/prompt absent | Modal + toast pattern | Not Run | S2 | | |

---

## 7. Security & Permissions Checklist (SE)

| Test ID | Area | Scenario | Expected Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|
| SE-01 | Partner isolation | Partner cannot see co-partner data on ANY surface (list/detail/download/export/API) | Fully scoped | Not Run | S1 | | Audit adjacent endpoints |
| SE-02 | External isolation | External cannot see other companies | Fully scoped | Not Run | S1 | | |
| SE-03 | IDOR — bulk ZIP | Range/id tampering | Only authorized certs returned | Not Run | S1 | | DEF-236 |
| SE-04 | IDOR — detail/PDF | Direct id access to unauthorized cert | Denied | Not Run | S1 | | |
| SE-05 | Privilege escalation | Analyst→admin route/API | Denied | Not Run | S1 | | |
| SE-06 | API key auth | Missing/invalid/expired key | 401 | Not Run | S2 | | |
| SE-07 | Internal status leak | Draft/Hold via partner/public | Never exposed | Not Run | S1 | | |
| SE-08 | Internal fields/tags leak | Via export/API to external | Never exposed | Not Run | S1 | | DEF-247 |
| SE-09 | Anonymous endpoints | External/internal tile APIs anon | 401/redirect | Not Run | S1 | | |
| SE-10 | Secrets | No secrets in client/source/responses | None exposed | Not Run | S2 | | **Rotate prod DB pwd surfaced this cycle** |
| SE-11 | Session | Logout + timeout | Session invalidated | Not Run | S2 | | |
| SE-12 | Headers/HTTPS | HSTS/CSP/X-Frame single + HTTPS enforced | Correct | Not Run | S3 | | |

---

## 8. Reporting & Export Checklist (RP)

| Test ID | Area | Scenario | Expected Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|
| RP-01 | Export format | All admin exports are CSV only | No Excel/JSON/PDF from admin pages | Not Run | S2 | | |
| RP-02 | Cert export (admin) | Full data export | All accessible certs; correct columns | Not Run | S2 | | |
| RP-03 | Partner export scope | Partner CSV = own scope (M2M) | Co-partner certs excluded; count == tile total | Not Run | S1 | | CertificateService:509 fix; qc182 |
| RP-04 | External export scope | External CSV = own company | Cross-company excluded | Not Run | S1 | | |
| RP-05 | Internal-field gating | Internal custom fields excluded for partner/external | Absent in header + rows | Not Run | S1 | | DEF-247 |
| RP-06 | CR-184 export | Active product identifiers | Correct active set; Admin/Analyst only | Not Run | S2 | | 1,022 active on prod baseline |
| RP-07 | Encoding | UTF-8 BOM; special chars | No mojibake in Excel | Not Run | S3 | | DEF-232 |
| RP-08 | Embedded newlines | Multi-line fields in CSV | Rows parse correctly (no row inflation) | Not Run | S3 | | qc182 parser lesson |
| RP-09 | Export permissions | External blocked from admin exports | 401/403 | Not Run | S2 | | |
| RP-10 | Large export perf | Export big set | Completes < timeout | Not Run | S3 | | |

---

## 9. Notification Checklist (NT)

| Test ID | Area | Scenario | Expected Result | Status | Severity | Defect ID | Notes |
|---|---|---|---|---|---|---|---|
| NT-01 | Toast | Save success | Toast only (no modal/redirect interrupt) | Not Run | S3 | | |
| NT-02 | Delete confirm | Delete action | `AdminCore.showConfirmation()` modal BEFORE | Not Run | S2 | | |
| NT-03 | Delete result | After delete | `AdminCore.showNotification()` toast AFTER | Not Run | S2 | | |
| NT-04 | No native dialogs | Any action | No `alert()/confirm()/prompt()` | Not Run | S2 | | |
| NT-05 | Validation errors | Failed save | Inline field errors + summary banner | Not Run | S3 | | |
| NT-06 | Custom fields error (DEF-266) | Select cert type | No spurious "Error loading custom fields" | Not Run | S2 | | Console distinguishes real cause |
| NT-07 | Email notifications | Triggered events | Sent per config (or mock on test) | Not Run | S2 | | DEF-263 |
| NT-08 | Error pages | 404/500 | Friendly page; no stack trace to user | Not Run | S3 | | |

---

## 10. Cross-Browser / Device Matrix (optional but recommended)

| Test ID | Browser/Device | Key flows (login, cert list, create/edit, search, tile) | Expected | Status | Notes |
|---|---|---|---|---|---|
| XB-01 | Chrome desktop | All | Pass | Not Run | |
| XB-02 | Edge desktop | All | Pass | Not Run | |
| XB-03 | Safari/iOS | All + 16px no-zoom | Pass | Not Run | |
| XB-04 | Android Chrome | All | Pass | Not Run | |
| XB-05 | Tablet 768 | Layout | No overflow | Not Run | |

---

## 11. Automated Coverage Map (run alongside manual)

| Script (Scripts/smoke-prod/) | Covers | Target | Notes |
|---|---|---|---|
| `midjune-batch-prod-regression.js` | DS-01..03, SR-01, CC-02(page), MP-03/10, IN-08 | gccwebapp.com (read-only) | 8/8 green 2026-06-19 |
| `cr171-partner-isolation-local.js` | MP-03/04/05, SE-01 | localhost mirror | 5/5 green |
| `def267-cert-number-maxlength.js` | CC-02/03, FV-01 (write) | localhost mirror | Pass |
| `cr187-normalized-search-local.js` | SR-01..06 | localhost mirror | 10/10 |
| `def247-partner-export-customfield-scope.js` | BR-08, RP-05 | localhost mirror | 5/5 |
| `qc182-partner-export-scope.js` (fixed) | RP-03 | configurable | SCOPED OK |
| `def265-cert-edit-no-dup-product.js` | PM-01 | localhost mirror | Pass |

> Manual UAT still required for business-judgement, cross-browser, and exploratory scenarios; automation is the regression backbone.

---

## 12. Defect Log Template

| Defect ID | Test ID | Title | Area | Severity | Steps to Reproduce | Expected | Actual | Environment | Build | Reported By | Date | Status | Assigned To | Resolution / Root Cause | Verified By | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| DEF-XXX | | | | | | | | gccwebapp.com | …-0619-0001 | | 2026-06-__ | New | | | | |

**Status flow:** New → Triaged → In Progress → Fixed → Ready for Retest → Verified Closed / Reopened.
**Rule (project):** every code-fix defect gets a DEF-XXX in `defect-register.md` and cites its QC/test id. Data-only issues (correct code, bad historical rows) are data-ops, not DEF rows.

---

## 13. UAT Execution Tracker

| Area | Total Cases | Executed | Pass | Fail | Blocked | N/A | % Complete | Open S1 | Open S2 | Tester | Sign-off |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Deployment Smoke (DS) | 6 | | | | | | | | | | |
| Auth & RBAC (AU) | 8 | | | | | | | | | | |
| Certificate CRUD (CC) | 8 | | | | | | | | | | |
| Workflow (WF) | 5 | | | | | | | | | | |
| Product Mgmt (PM) | 6 | | | | | | | | | | |
| Multi-Partner (MP) | 10 | | | | | | | | | | |
| Search (SR) | 6 | | | | | | | | | | |
| PDF/Files (PDF) | 5 | | | | | | | | | | |
| Field Validation (FV) | 15 | | | | | | | | | | |
| Business Rules (BR) | 11 | | | | | | | | | | |
| Integration (IN) | 12 | | | | | | | | | | |
| Regression (RG) | 14 | | | | | | | | | | |
| Security (SE) | 12 | | | | | | | | | | |
| Reporting/Export (RP) | 10 | | | | | | | | | | |
| Notifications (NT) | 8 | | | | | | | | | | |
| Cross-Browser (XB) | 5 | | | | | | | | | | |
| **TOTAL** | **141** | | | | | | | | | | |

**Daily standup fields:** cases run today · new defects (by severity) · blockers · burn-down vs plan · ETA to exit criteria.

---

## 14. Production Readiness Checklist

> "Production" = the AccuWeb production site (greencirclecertified.com), if/when this batch is promoted beyond the gccwebapp.com test env.

| # | Item | Owner | Status | Evidence / Notes |
|---|---|---|---|---|
| 1 | All S1/S2 test cases Pass; zero open S1/S2 (or waived w/ sign-off) | QA Lead | ☐ | UAT tracker §13 |
| 2 | Server uploads backed up before publish (Gate 0) | Release Mgr | ☐ | zip `wwwroot`/`App_Data/uploads` |
| 3 | Prod DB backed up before schema/data change | Release Mgr | ☐ | |
| 4 | Migrations applied in SAME step as DLL; combined SQL run with `-I` | Release Mgr | ☐ | `apply-migrations.sql` from prod high-water (no CONCAT) |
| 5 | CR-171 backfill applied; `missing_M2M = 0` verified | Release Mgr | ☐ | CertificatePartners / CertificateProductIdPartners |
| 6 | Post-deploy regression suite green | QA | ☐ | `midjune-batch-prod-regression.js` |
| 7 | 4-role login canary passes post-deploy | QA | ☐ | DEF-239 |
| 8 | Partner API contract unchanged (Amazon sign-off if shape changed) | BA | ☐ | BR-07 |
| 9 | Rollback plan ready (prior zip + DB restore point) | Release Mgr | ☐ | keep last good zip |
| 10 | Deploy sequence: SQL-first (AutoMigrate caveat), stop→extract→start | Release Mgr | ☐ | predeploy record runbook |
| 11 | Secrets rotated (prod DB password exposed this cycle) | Security | ☐ | **action item** |
| 12 | Monitoring/log review post-deploy (first 24–48h) | Ops | ☐ | watch for `Invalid column name`, 500s |
| 13 | Stakeholder/shipped log updated; client comms drafted | BA | ☐ | `shipped-2026-06.md` |
| 14 | Known-issues / deferred defects documented | QA Lead | ☐ | |

---

## 15. Go / No-Go Recommendation Template

**Release:** Mid-June 2026 batch · **Build:** `GCCWebApp-deploy-20260619-0001.zip` · **Decision date:** ______ · **Decision meeting attendees:** ______

### Test execution summary
| Metric | Value |
|---|---|
| Total test cases | 141 |
| Executed | ___ / 141 ( __% ) |
| Pass | ___ |
| Fail | ___ |
| Blocked / Deferred | ___ |
| Open defects — S1 | ___ |
| Open defects — S2 | ___ |
| Open defects — S3/S4 | ___ |

### Quality gates
| Gate | Threshold | Met? |
|---|---|---|
| Critical flows (login, cert CRUD, partner isolation, PDF, search) all Pass | 100% | ☐ |
| Open S1 defects | 0 | ☐ |
| Open S2 defects | 0 (or waived) | ☐ |
| Partner data isolation verified (CR-171) | 100% | ☐ |
| Partner API contract intact | Yes | ☐ |
| Regression checklist | Green | ☐ |
| Production readiness checklist | Complete | ☐ |

### Recommendation
> ☐ **GO** — all gates met; release approved for production promotion.
> ☐ **GO WITH CONDITIONS** — release approved; the following must be completed/monitored: ____________ (e.g., rotate DB password, monitor logs 48h, fix S3s in next patch).
> ☐ **NO-GO** — blocking issues: ____________ . Re-test after fix; next decision date ____________ .

### Risk & rationale
- Top residual risks: ______________________________
- Rollback trigger & plan: ______________________________
- Deferred items (with justification): ______________________________

### Sign-off
| Role | Name | Decision | Signature | Date |
|---|---|---|---|---|
| QA Lead | | | | |
| Business Analyst | | | | |
| Release Manager | | | | |
| Product Owner / Client | | | | |

---

### Appendix — Severity & Priority quick guide
- **S1-Critical:** data loss, security/isolation breach, core flow down, no workaround → **No-Go** if open.
- **S2-High:** major function broken, workaround exists → **No-Go** unless explicitly waived.
- **S3-Medium:** minor function/UX, limited impact → may ship with documented plan.
- **S4-Low:** cosmetic/nit → backlog.

---

## 16. Execution Log — Round 1 (2026-06-19, READ-ONLY on gccwebapp.com)

Runner: `Scripts/smoke-prod/prod-e2e-readonly-batch.js` (+ `midjune-batch-prod-regression.js`). **17 PASS / 0 FAIL / 2 BLOCKED.**

| Test ID | Status | Actual Result |
|---|---|---|
| DS-01/02/03/04 (smoke, 4-role login, schema, backfill) | Pass | site 200; admin/analyst/external/partner@amazon login; internal API total=249; CertificatePartners=155 / CertProductIdPartners=992 |
| MP-07 / WF-02 (public visibility) | Pass | public API 74 certs; **0 expose partner**; statuses = Client Approved - Paid + Republished only |
| IN-08 / SE-09 (external API anon) | Pass | HTTP 401 |
| RG-07 / SE-12 (security headers) | Pass | x-frame-options, CSP, x-content-type-options, HSTS present |
| IN-07 (pagination) | Pass | internal pageSize=300 → 249 rows (not capped 20) |
| SR-01 / SR-04 (search) | Pass | "3M™"→4; "26-0173"→1 |
| AU-03 (analyst→admin) | Pass | 302 redirect (denied) |
| AU-05 (external company scope) | Pass | external sees 4 of 249 |
| AU-04 (external no Actions col) | Pass | Actions column not visible |
| MP-03 (Amazon isolation) | Pass | 19 scoped certs; no Novi/Wayfair leak |
| RP-03 (Amazon export scope) | Pass (S4 note) | export 20 distinct vs tile 19 — all Amazon-scoped (no leak); tile applies an extra active/valid filter → +1 in export. Verify-only. |
| MP-04 (Novi isolation) | **Blocked** | `perry.novi@example.com`/TestPass123! login failed on prod — needs prod creds (passed 5/5 on dev mirror) |
| MP-05 (multi-partner isolation) | **Blocked** | `pru.multi@example.com`/TestPass123! login failed on prod — needs prod creds (passed on dev mirror) |

**Round-1 verdict:** deploy healthy; no S1/S2/S3 defects found. Outstanding to fully close UAT:
1. **Partner-login coverage on prod** (MP-04/05) — provide prod passwords for perry.novi / pru.multi (or a partner API key) so isolation is verified live for Novi + multi-partner, not just Amazon.
2. **Write-path E2E** (CC create/edit/delete, DEF-267 save, DEF-265 product de-dup, PM, custom fields) — run on the **local mirror** (validated pre-deploy: def267, def265, cr187 10/10, def247 5/5, cr171 5/5) or on throwaway `ZZTEST-` records.
3. **Manual/exploratory** UAT for business judgement + cross-browser (§10).

> No S1/S2 open. Go/No-Go (§15) gate "critical flows pass" is GREEN for everything testable read-only on prod; MP-04/05 marked Blocked pending creds (low risk — same code path as the passing Amazon case + dev-mirror 5/5).

## 17. Execution Log — Round 2 (2026-06-19, WRITE-PATH on local mirror)

Local mirror = restored copy of prod data (`GCCDatabase_Dev`), running the deployed working-tree code. Writes confined to the throwaway mirror. **All green.**

| Test ID | Status | Actual Result |
|---|---|---|
| CC-02 / CC-03 / FV-01 (DEF-267 save) | Pass | 8-char number `26-01730` saved, no MaxLength(7) banner, redirected (`def267-cert-number-maxlength.js`) |
| PM-01 (DEF-265 product de-dup) | Pass | edit cert product type → line item updated in place, count 118→118, no orphan (`def265-cert-edit-no-dup-product.js`) |
| SR-01..06 (CR-187 search) | Pass | 10/10 — symbol/case/spacing/punctuation/partial + negative control (`cr187-normalized-search-local.js`) |
| BR-08 / RP-05 (DEF-247 internal-field export gate) | Pass | 5/5 — internal custom field absent from partner export header + rows |
| MP-03/04/05 / SE-01 (CR-171 isolation, ALL partners) | Pass | **5/5 users clean, 0 co-partner leaks** — Amazon 6, Novi 1, Wayfair 0, Other 0, **multi (Amazon+Novi) 6 (shared cert deduped, no Wayfair/Other)** |

> Note: a first pass hit local-app-lifecycle flakiness (background `dotnet run` dropped mid-suite → login-click timeouts on the last 2 scripts); re-run after restart was fully green. Not a product issue.

**Combined status (Round 1 read-only prod + Round 2 write-path mirror):** **27 automated checks PASS, 0 FAIL, 2 BLOCKED** (Novi/multi partner *login* on prod — isolation itself verified on the mirror 5/5; prod-creds needed to also confirm live). No S1/S2/S3 defects across both rounds.

**Still owed for full UAT sign-off (human/creds-gated):**
- MP-04/05 *live on prod* — needs prod passwords for perry.novi / pru.multi (or a partner API key).
- Manual/exploratory: CC-01 create happy-path, CC-06 delete modal+toast, DEF-266 custom-fields toast on Create, MP-01/02 multi-select on Create/Edit, WF status transitions, full FV field-validation matrix, cross-browser (§10).

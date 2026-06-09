# CR-020 — Add "User Company" field (user's own company/employer), separate from "Company Access"

- **Proposed:** 2026-06-03
- **Status:** Approved by client (Shawn, 2026-06-03) — "there needs to be both fields. User Company and Company Access." Build authorized same session ("draft CR, then build now").
- **Origin:** QC #106 (CR-required portion: *"free-text Employer field on user record, separate from Company Access"*) and QC #148 item 4 (*"User Management User Company and Company Access separation in sheet"*).
- **Related:** [[DEF-238]] (role-gated Company Access visibility — the non-CR half of QC #106/#148, already built). DEF-173 conflict-history (the "Company Access" vs "User Company" label saga) is **resolved by this CR** — both labels now name distinct fields, so the rename ping-pong ends.

## Problem

The user record has exactly one company concept today — `User.Companies` (many-to-many) — which controls **which companies' data a user can view** ("Company Access"). Over time this single field was relabeled "Company" → "Company Access" → "User Company" across surfaces, conflating two ideas the client actually wants separate:

1. **Company Access** — which companies' data the user may view (data-scoping). *(Exists today as `User.Companies`.)*
2. **User Company** — the user's **own** company / employer. *(Does not exist — this CR adds it.)*

## Decision (client-confirmed 2026-06-03)

- **User Company** is a **single company chosen from the existing Companies list** (FK, dropdown) — *not* free text. (QC #106 said "free-text"; client revised to a dropdown of existing companies on 2026-06-03.)
- Both fields exist independently. User Company is **optional**.
- **Surfaces in scope:** Add/Edit User **modal**, **import template + parser**, **CSV export**.
- **Out of scope (this CR):** the user **table** columns — left unchanged for now. *Flag:* the table's existing column header reads "User Company" but currently displays Company Access; that mislabel remains until a separate decision.

## Implementation

| Layer | Change |
|------|--------|
| Model | `User.UserCompanyId` (`int?`) + `User.UserCompany` (nav) — `Models/User.cs` |
| DbContext | `HasOne(u => u.UserCompany).WithMany().HasForeignKey(u => u.UserCompanyId).OnDelete(SetNull)` |
| Migration | `AddUserCompanyToUser` — nullable `UserCompanyId` column + FK |
| ViewModels | `UserViewModel.UserCompanyId` + `UserCompanyName`; `CreateUserViewModel.UserCompanyId`; bulk-import model `UserCompanyName` — `Models/AdminModels.cs` |
| Service | `AdminService`: project User Company in `GetUsersAsync`/`GetUserByIdAsync`; persist in `CreateUserAsync`/`UpdateUserAsync`; resolve name→id in `BulkImportUsersAsync` |
| Controller | `AdminController.ImportUsers` positional parse + CSV export header/value |
| Modal | `UserManagement.cshtml` — new "User Company" single-select dropdown (shown for all roles; optional) |
| Client JS | `userManagement.js` — populate dropdown, save/load `UserCompanyId`, add "User Company" template column, **rename access column to "Company Access"**, add export column, teach import parser to read both columns |

## Import/export column change (breaking, intentional)

The import template/export gains a clear two-column split:

| Column | Maps to | Format |
|--------|---------|--------|
| `Company Access (semicolon-separated names, or "All")` | `User.Companies` (access scope) | names joined by `;`, or literal `All` |
| `User Company` | `User.UserCompanyId` (employer) | single company name |

The access column was previously labeled **`User Company`** (per the 2026-05-21 client confirmation under DEF-173). This CR **reassigns** that label to the new employer field and renames the access column to **`Company Access`**. The importer keeps `User Company`/`Company Access` header aliases mapped to the correct field; legacy CSVs that used "User Company" to mean *access* will now populate the employer field instead — called out so it isn't a surprise during UAT.

## Verification

Localhost `:5101` Playwright + manual: create/edit a user with a User Company set; confirm it persists and round-trips through template → import → export. (Script: `Scripts/smoke-prod/def237c-user-company-verify.js`.)

---

## Revision 2026-06-05 — User Company reverted to FREE TEXT (QC #106 client feedback)

- **Origin:** QC #106 follow-up (6/4): *"User Company can just be an open text field. Not limited to the companies added for certs. This dropdown would not work in cases like GCC users, Amazon Corporate, Novi, etc."* This reverses the 2026-06-03 dropdown decision (line 17) and restores the **original** QC #106 ask ("free-text Employer field").
- **Approved by Shawn 2026-06-05** ("Free-text string column").

### Security / logic review (done before building)
- **Security impact: none.** Data access is driven solely by the **Company Access** collection (`User.Companies`) via `GetCurrentUserCompanyIdsAsync()` (`CertificateController.cs:1636` + identical copies in Product/Tile/v1 API controllers). That path does `u.Companies.Select(c => c.Id)` — it **never reads `UserCompany`/`UserCompanyId`**. User Company is label/display metadata only; converting it cannot widen or break any user's access.
- **Logic fix (not just a field-type change):** the old FK *rejected* import rows whose employer wasn't an existing cert company (`AdminService.cs` + `AdminController.ImportUsers`), so "GCC"/"Amazon Corporate"/"Novi" users were un-importable. Free text removes that block and stops the importer from auto-creating junk Company rows.

### Implementation delta (vs the original FK build)
| Layer | Change |
|------|--------|
| Model | `User.UserCompany` is now `string?` `[MaxLength(255)]` — FK `UserCompanyId` + `Company` nav removed (`Models/User.cs`) |
| DbContext | `HasOne/WithMany/HasForeignKey(UserCompanyId)` config removed (`Data/ApplicationDbContext.cs`) |
| Migration | `20260605131035_ChangeUserCompanyToFreeText` — adds `UserCompany nvarchar(255)`, **backfills from `Companies.Name`**, then drops the FK/index/`UserCompanyId` column |
| ViewModels | `UserViewModel`/`CreateUserViewModel`: `UserCompanyId` removed; `UserCompanyName` (string) is the single carrier (`Models/AdminModels.cs`) |
| Service | `AdminService`: project/persist `UserCompany` as string; bulk-import sets it verbatim (no name→id resolve, no fail-fast) |
| Controller | `AdminController.ImportUsers`: column 5 passed through as free text |
| Modal | `UserManagement.cshtml` — `<select>` → `<input type="text" maxlength="255">` |
| Client JS | `userManagement.js` — load/save `userCompanyName` string, removed dropdown-populate block, import passes free text, template hint → "free-text employer" |

### Prod deploy (Gate 7 — prod never auto-migrates; DEF-239 lesson)
Run **`Scripts/database/CR-020rev_ChangeUserCompanyToFreeText.sql`** (idempotent, transaction-wrapped, history-guarded) against the gccwebapp DB in the SAME deploy step as the DLL, then recycle the app pool. The script preserves any existing employer value by backfilling from `Companies.Name` before dropping `UserCompanyId`.

### Verification (2026-06-05)
`Scripts/smoke-prod/cr020rev-user-company-freetext.js` → **6 PASS / 0 FAIL** on `:5101`: input is text (not select), free-text "GCC Corporate …" (a non-cert company) persists on create, reflects on edit, independent of Company Access, template + export both retain the two columns.

---

## Revision 2026-06-05 (b) — QC #106 item #2: add "Company Access" COLUMN to the User Management table

- **Origin:** QC #106 (6/4) open item #2 — *"Company Access listing is missing in table on User Management page per prior feedback."* The original CR-020 deferred the table (line 20: "Out of scope… the user table columns"); the 6/3 follow-up remapped the single table column to the **employer** field. That left the data-scoping **Company Access** value with no column. This revision adds it as its own column, so the table now shows BOTH fields the client asked to separate.
- **Approved by Shawn 2026-06-05** ("yes do item #2 as well").
- **Change (display-only — no backend, no schema, no API):**
  - `Views/Admin/UserManagement.cshtml` — new sortable `<th data-sort="companyAccess">Company Access</th>` between "User Company" and "Email"; column widths rebalanced.
  - `wwwroot/js/userManagement.js` — `createUserRow` renders a `.user-company-access` cell via the existing **`getCompanyAccessDisplay(user)`** (the same "All"/joined-names logic the CSV export + import preview already use); `handleSort` special-cases the computed `companyAccess` key; empty-state `colspan` bumped 8 → 9.
- **Display rule (unchanged from export):** Admin/Analyst → "All" (per DEF-238 they implicitly have all access); External/Partner → their associated company names joined by ", " (or "All" if every company is selected); none set → blank.
- **Verification (`:5101`):** `Scripts/smoke-prod/qc106-company-access-column.js` → **5 PASS / 0 FAIL** — Company Access header present and separate from User Company, all 25 rows render the cell, Admin shows "All", header-click sort renders without error. (Dev DB has only Admin/Analyst users, so the live non-"All" value wasn't exercised — but it's the identical function + `companyNames` data as the already-verified export column.)
- **Deploy:** front-end only — ships in the same bundle as Revision (a); **no extra prod SQL** beyond the `CR-020rev` migration script.

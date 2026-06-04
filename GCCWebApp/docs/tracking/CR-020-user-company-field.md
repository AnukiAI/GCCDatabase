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

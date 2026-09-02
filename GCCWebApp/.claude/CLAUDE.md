# GCC Database Solution - AI Assistant Guide

ASP.NET Core 8.0 MVC + EF Core 9.0 + SQL Server + Azure AD B2C + Bootstrap 5

## Quick Start
```bash
dotnet restore && dotnet build && dotnet run --urls "http://localhost:5101"
```

## Reference Docs (Read when working on specific areas)

> 📁 `docs2/` now lives **inside the git repo**, at the repo root — moved there 2026-07-20
> (412 files, 18.8 MB) because it previously sat one level above the repo in OneDrive only, so a
> fresh clone got none of it. It is deliberately placed at the **repo root, not inside the
> ASP.NET project folder**, so it can never be swept into a `dotnet publish`.
>
> Paths below are relative to this project directory (`GCCWebApp/GCCWebApp`, the normal working
> dir), hence the single `../`. All paths verified to resolve 2026-07-20.

| Area | File | When to Read |
|------|------|--------------|
| Database | `../docs2/claude-ref/DATABASE.md` | DB connections, schema, migrations, troubleshooting |
| UI Standards | `../docs2/claude-ref/UI-STANDARDS.md` | Modals, buttons, notifications, colors, containers |
| View Patterns | `../docs2/claude-ref/VIEW-PATTERNS.md` | Admin pages, templates, tile views, forms |
| API | `../docs2/claude-ref/API-REFERENCE.md` | REST endpoints, Novi integration, authentication |
| Testing | `../docs2/claude-ref/TESTING.md` | Credentials, coverage, running tests |
| Deployment | `../docs2/claude-ref/DEPLOYMENT.md` | Production, web.config, troubleshooting |
| UI Audit | `../docs2/claude-ref/UI-CONSISTENCY-AUDIT.md` | Prior UI consistency findings |

**In-repo docs** (these DO travel with the code): `docs/tracking/` (defect + CR registers,
shipped log, pre-deploy checklist), `docs/qa/`, `docs/governance/`.

## Critical Rules (Always Apply)

### Data Export
- **CSV ONLY** - No Excel/JSON/PDF from admin pages
- Use `ExportUtils.exportToCSV()` directly

### User Notifications
**NEVER use native `alert()`, `confirm()`, `prompt()`**
- Delete: `AdminCore.showConfirmation()` BEFORE, `AdminCore.showNotification()` AFTER
- Success: Toast notification only

### External User Restrictions
- See ONLY associated company/partner data
- CANNOT view/edit internal tags
- NO Add/Edit certificate buttons
- NO Actions column on Product Management

### Partners
- Core entities: Amazon, Novi, Other
- Partners page: Export + Add only (NO Template/Import)
- 8-state certificate workflow (source of truth: `Models/CertificateStatus.cs` — original spec was 7, Republished added making 8)

### Views
- Use `ViewData["Title"]` (no `<h1>` in views)
- Use `.admin-container` with proper structure
- NO inline `max-width` overrides

## Key Files
| Purpose | File |
|---------|------|
| Certificate CRUD | `CertificateController.cs`, `CertificateService.cs` |
| Workflow | `CertificateWorkflowService.cs` (8 states — see `Models/CertificateStatus.cs`) |
| Product IDs | `ProductIdService.cs`, `CertificateProductId.cs` |
| Tile APIs | `CertificateTilesApiController.cs`, `CertificateTileExternalApiController.cs` |
| Design Guide | `../docs2/mockups/GCC_FinalDesign/docs/0_0_MasterDesignGuide_6_4.html` (newest; `_5_22` is the prior rev) |
| Requirements | `docs/Final Req Docs/` + `docs/Final Req Docs/archive/` (PDFs/notes — there is no `Requirements.md`) |

## Routes Quick Reference

**Certificate routes are keyed by CERTIFICATE NUMBER, not integer Id** — `/Certificate/Edit/26-0224`
works, `/Certificate/Edit/1308` returns HTTP 400 *"Certificate number is required"*. Same for
`CertificateSingleView`. The one exception is the **POST** half of Edit, which does take `int id`
(`Edit(string number)` for GET, `Edit(int id, CertificateEditViewModel model, …)` for POST) — so an
integer "works" on submit and fails on load, which reads like a broken page rather than a wrong key.

| Route | Key | Access | Attribute |
|-------|-----|--------|-----------|
| `/Certificate/CertificateTileView` | — | Public | `[AllowAnonymous]` |
| `/Certificate/CertificateSingleView/{number}` | number | Public | `[AllowAnonymous]` |
| `/Certificate/CertificateTileViewExternal` | — | External+ | `[Authorize(Policy = "ExternalAccess")]` |
| `/Certificate/CertificateTileViewInternal` | — | Analyst+ | `[Authorize(Policy = "AnalystOrAdmin")]` |
| `/Certificate/Create` | — | Analyst+ | `[Authorize(Policy = "AnalystOrAdmin")]` |
| `/Certificate/Edit/{number}` | **number** (GET) / `int id` (POST) | Analyst+ | `[Authorize(Policy = "AnalystOrAdmin")]` |
| `/Admin/*` | — | Admin only | — |
| `/api/v1/certificates` | — | API Key | `[Authorize(AuthenticationSchemes = "ApiKey")]` |

Public routes are `[AllowAnonymous]` at the action, but the **data** is still gated — publishable
status AND inside the effective-to-expiration window (DEF-358, `Models/CertificateVisibility.cs`).

**Every row verified live on localhost:5101, 2026-08-17**, requested both anonymously and as `admin`:
the two Public rows return 200 anonymously; External+/Analyst+/Admin rows all bounce anonymous
callers to `/Account/Login` and return 200 for admin; `Edit/1308` returns 400 for admin, confirming
the number-vs-Id note above. `/api/v1/certificates` verified with an active Amazon API key.

## Development Rules

**DO**: Read before edit, CSV exports, Modal→Delete→Toast, follow patterns, clean rebuild

**DON'T**: New auth systems, modify 8-state workflow, push to master, native JS dialogs, unrelated changes

**ALWAYS**: Check requirements, ask clarifying questions, review mockups, test changes

## Quick Credentials

### Role-matrix test accounts — TEST environment (gccwebapp.com) only

Created 2026-08-20 through `/Admin/CreateUser` (not SQL), so each went through the same validation,
hashing and audit path as a real account. **All share the password `GccTest2026!`** and have 2FA
**off**, so they are usable immediately; switch 2FA on per account to test it.

| Username | Email | Role | Scoped to | Lands on |
|----------|-------|------|-----------|----------|
| `test.admin` | shawnmwest+admin@gmail.com | Admin | — | TileViewInternal |
| `test.analyst` | shawnmwest+analyst@gmail.com | Analyst | — | TileViewInternal |
| `test.external` | shawnmwest+external@gmail.com | External | **Sika Corporation** (574 certs) | TileViewExternal |
| `test.external2` | shawnmwest+external2@gmail.com | External | **MAPEI** (313 certs) | TileViewExternal |
| `test.amazon` | shawnmwest+amazon@gmail.com | Partner | **Amazon** | TileViewExternal |
| `test.novi` | shawnmwest+novi@gmail.com | Partner | **Novi** | TileViewExternal |
| *(email login only)* | shawnmwest+xsense@gmail.com | External | **X-SENSE** (10 certs, **2 not yet effective**) | TileViewExternal |

All six verified to sign in and land on the correct role page, 2026-08-20.

The **X-SENSE** row was added 2026-09-02 for the DEF-376 carve-out check and is the odd one out: it has no short username (sign in with the email) and it exists for one reason — it is the only External account that owns a **future-dated** certificate (`26-0441`/`26-0442`, effective 2026-10-21), which is what makes it possible to prove a company still sees its own *upcoming* certs while Amazon cannot. **It stops being able to prove that on 2026-10-21**, when those certs come into force — see the review note in `observations_log.md`.

**The pairs are the point.** `test.external` / `test.external2` sit on different companies and
`test.amazon` / `test.novi` on different partners, because a single scoped account can only ever
show that it *sees* something — never that it is correctly *denied* someone else's data. Test them
against each other.

**Addresses use Gmail subaddressing** (`+tag`) so each account has the unique email the app requires
while all mail lands in one inbox. Deliberately **not** on `gccwebapp.com`: that domain's only
mailbox is the address the app *sends from*, and giving a test account the sender's address caused
the credential collision on 2026-08-19 (see DEF-263). Note `mail.gccwebapp.com` does **not** support
subaddressing — Gmail does; don't generalise.

Also present: `2fatest` / `GccTest2026!` (Analyst, email `shawnmwest@gmail.com` — no `+` tag) — the
2FA fixture, deliberately left with 2FA **on**. Because it is the only account with 2FA enabled, it
is the `WITH2FA_USER` in `Scripts/data-ops/2026-08-19-verify-2fa-per-user.js`; changing its 2FA
state breaks that check.

### Dev database

**`GCCDatabase_Dev` currently has exactly TWO users — both Admin.** The database was overwritten
with the test environment's dataset on 2026-08-13 to rehearse the migration, and that dataset never
contained the old seeded analyst/external accounts. Verified by enumerating `Users` 2026-08-17.

| Username | Email | Password | Role | Exists in dev? |
|----------|-------|----------|------|----------------|
| `admin` | swest@anukiconsulting.com | **CHANGED — see below** | Admin | **yes** |
| `admin.uat` | admin@greencirclecertified.com | TestPass123! | Admin | **yes** — login verified 2026-08-17 |
| ~~`analyst`~~ | ~~jane.analyst@anukiconsulting.com~~ | — | Analyst | **NO — user does not exist** |
| ~~`external`~~ | ~~external.user@anukiconsulting.com~~ | — | External | **NO — user does not exist** |

> **⚠️ `admin` no longer uses `TestPass123!` on the TEST environment (gccwebapp.com).** Its password
> was changed on **2026-08-19 at 18:00 UTC** by a real password-reset, run to verify DEF-263 email
> delivery end-to-end. Confirmed in the DB: the reset token was consumed and `PasswordHash` moved
> from `$2a$11$PLqXq…` to `$2a$11$DZxYtoR…`. **Only Shawn knows the new value.** The symptom if you
> forget this is *"Invalid email or password"*, which reads exactly like a broken login — it is not.
> **Use `admin.uat` instead** (same Admin role, still `TestPass123!`, verified 2026-08-19), which is
> why `Scripts/data-ops/2026-08-19-verify-2fa-per-user.js` defaults to it.
> The dev database (`GCCDatabase_Dev`) is unaffected — the reset was against test only.

The password is unchanged — `TestPass123!` still works. An analyst/external login against dev fails
with *"Invalid email or password"* because **the account is absent**, not because the hash rotated.
Don't burn time re-trying passwords; check `Users` first.

**Consequence for the test suites — PARTLY RESOLVED 2026-09-02.** The claim above that dev holds
only two users is **out of date**. `analyst` and `external` do authenticate against dev today
(`cert-detail-scoping` signs in as both), and the five CR-171 **partner** fixtures were restored on
2026-09-02 by running `Scripts/ci/seed-e2e.sql` against `GCCDatabase_Dev`. Dev now has **10 users**.

That seed is idempotent, transactional and additive: its only DELETEs are scoped to its own fixture
markers (`Certificates.Number LIKE '98-9%'`, `Products`/`Companies` named `ZZ_CI_%`, and its five
fixture emails), all of which matched **zero** rows beforehand. Real certificate count was 5,702
before and 5,702 after — 7 synthetic `98-900x` certs were added alongside. Re-run it any time the
partner specs report BLOCKED:

```
sqlcmd -C -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P <pw> -I -b -i Scripts/ci/seed-e2e.sql
```

Fixture partner users (password `TestPass123!`, copied from `admin`'s hash by the seed):
`partner@amazon.com` → Amazon · `perry.novi@example.com` → Novi ·
`pam.wayfair@example.com` → Wayfair · `pip.other@example.com` → Other ·
`pru.multi@example.com` → **Amazon + Novi** (the multi-partner case — the one that can prove a
co-partner leak).

With those present `partner-isolation` runs its full 21 assertions instead of reporting 5 BLOCKED.

**Posting the login form programmatically** (curl / `Invoke-WebRequest` / smoke scripts): the field is
named **`Email`**, not `Username` — but it accepts **either** a username or an email
(`LoginViewModel.Email` is labelled "Email or Username"; the lookup is
`u.Email == email || u.Username == email`, `AccountController.cs:1159`). So `Email=admin` and
`Email=swest@anukiconsulting.com` both work; posting `Username=admin` silently fails and re-renders
the login page as HTTP 200, which reads like a bad password rather than a wrong field name.
An `__RequestVerificationToken` from a prior GET is also required. Blocking redirects to detect the
302 is unreliable here — POST, follow the redirect, then confirm by requesting `/Dashboard` and
checking you were not bounced back to `/Account/Login`.

## Feature Status
All complete: Certificate Tiles, Infinite Scroll, Autocomplete, Shareable URLs, Product IDs, CSV Import/Export, 8-State Workflow, RBAC, Partner API, Database Filters

## Notes
- Tech: ASP.NET Core MVC + SQL Server (not Django/PostgreSQL)
- External ≠ Analyst/Admin permissions
- Admin user/role cannot be deleted
- Emails must be unique per user
- 16px minimum font size

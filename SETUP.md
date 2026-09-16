# GCC Database Solution — fresh-machine setup

Everything below assumes a clean clone of this repo and nothing else. Written 2026-09-16 after a
setup attempt on a second machine appeared to be missing files; it was not — the repo was complete,
but the committed configuration is machine-specific. That is the part this document exists to fix.

ASP.NET Core 8.0 MVC · EF Core 9.0 · SQL Server · Bootstrap 5

---

## 1. Prerequisites

| Requirement | Notes |
|---|---|
| .NET SDK 8.0 | `dotnet --list-sdks` must show an `8.x`. The app targets `net8.0`. |
| SQL Server 2019+ or Express | LocalDB also works. Note the instance name — you will need it in step 3. |
| `sqlcmd` | Ships with SQL Server client tools. Used to run the test-data seed. |
| Node.js (optional) | Only for the smoke/E2E scripts under `GCCWebApp/Scripts/`. Not needed to run the app. |

## 2. Clone and build

```bash
git clone https://github.com/AnukiAI/GCCWebApp.git
cd GCCWebApp
dotnet restore
dotnet build
```

The solution at the repo root (`GCCWebApp.sln`) includes the test projects. The web app itself is
`GCCWebApp/GCCWebApp.csproj`.

## 3. Create the database and point the app at it

**This is the step that fails on a new machine.** `GCCWebApp/appsettings.Development.json` is
committed with a connection string hardcoded to the original dev box:

```
Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=...
```

`ANUKI01` is a machine name and `GCC_DevUser` is a SQL login local to it. Neither exists on your
machine, so EF cannot connect, migrations never apply, and the seeder never runs — which looks
exactly like "the seed data is missing from the repo."

Create the database and a login:

```sql
CREATE DATABASE GCCDatabase_Dev;
GO
CREATE LOGIN GCC_DevUser WITH PASSWORD = 'DevP@ss2024!SQL';
GO
USE GCCDatabase_Dev;
CREATE USER GCC_DevUser FOR LOGIN GCC_DevUser;
ALTER ROLE db_owner ADD MEMBER GCC_DevUser;
GO
```

Then override the connection string **without editing the committed file** — an env var wins over
`appsettings.Development.json`, so your machine name never lands in a commit:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=<YOUR-INSTANCE>;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
```

`dotnet user-secrets` is the durable alternative if you would rather not re-set the variable per
shell. Do **not** commit a changed `appsettings.Development.json`; it would break the other machine.

Also create the uploads root, which the app expects to exist and which is deliberately outside the
repo (DEF-286 — uploads must never be OneDrive-synced or swept into a publish):

```powershell
New-Item -ItemType Directory -Force C:\GCCData\uploads
```

## 4. Run — schema and login users seed themselves

```bash
dotnet run --project GCCWebApp --urls "http://localhost:5101"
```

On a **Development** environment against an empty database, startup applies all EF migrations and
then runs `SeedData.InitializeAsync(context, seedEssentialData: true)` (`Program.cs:1107`). That
creates Roles, Permissions, the API keys, and the login users. It is idempotent — it no-ops the
moment `Roles` has any row, so it never touches an already-populated database.

Seeded logins, **all with password `TestPass123!`**: `admin`, `analyst`, `external`, `testuser`,
plus a set of named analyst/external accounts. The login form field is named `Email` but accepts a
username or an email.

If the app starts but you cannot log in, the seeder did not run — check the startup log for the
database-initialization line before assuming a bad password.

## 5. Seed the business test data

`SeedData` creates **no** Partners, Companies, Products, Certificates or CertificationTypes. Those
come from a separate, tracked seed:

```bash
sqlcmd -C -S <YOUR-INSTANCE> -d GCCDatabase_Dev -U GCC_DevUser -P 'DevP@ss2024!SQL' -I -b \
  -i GCCWebApp/Scripts/ci/seed-e2e.sql
```

`-I` (QUOTED_IDENTIFIER ON) is **required** — the schema has filtered indexes and computed columns
and the script fails without it. The seed is idempotent, transactional and additive: its only
DELETEs are scoped to its own fixture markers (`Certificates.Number LIKE '98-9%'`, `Products` and
`Companies` named `ZZ_CI_%`, and its five fixture emails), so it is safe to re-run.

It creates Partners (Amazon/Novi/Other/Wayfair, resolved by name, never by assumed id), the
`ZZ_CI_*` companies and products, certificates `98-9001`–`98-9104`, and five Partner-role users
(`partner@amazon.com`, `perry.novi@example.com`, `pam.wayfair@example.com`, `pip.other@example.com`,
`pru.multi@example.com` — the last is the multi-partner case that can prove a co-partner leak).
Those users reuse `admin`'s password hash, so they are also `TestPass123!`.

This seed is synthetic fixture data. It is **not** the client dataset — that is loaded separately
from the migration workbooks and is not in the repo.

## 6. Verify

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5101/Certificate/CertificateTileView
```

Expect `200` anonymously. Then sign in as `admin` / `TestPass123!` and confirm you land on the
internal tile view.

---

## Where the documentation lives

All of this travels with the repo.

| Path | Contents |
|---|---|
| `.claude/CLAUDE.md` (in `GCCWebApp/`) | The authoritative working guide: routes, credentials, rules. Start here. |
| `GCCWebApp/docs/Final Req Docs/` | Current requirements. `archive/` holds the superseded revisions. |
| `GCCWebApp/docs/tracking/` | Defect + change-request registers, shipped log, pre-deploy checklist. |
| `GCCWebApp/docs/qa/`, `docs/governance/` | Test strategy and governance. |
| `docs2/claude-ref/` | Reference notes on database, UI standards, view patterns, API, deployment. **Stale — 6-8 months old; `.claude/CLAUDE.md` is fresher and wins on any conflict.** |
| `docs2/mockups/` | Design guides and mockups. |

The `archive/` folders under `docs/Final Req Docs/` and `docs2/` were excluded from git until
2026-09-16 by an over-broad `**/Archive/` rule meant for old *code*. They are now tracked — see the
comment on that rule in `.gitignore` before changing it.

## Known setup gotchas

- **`docs2/claude-ref/DATABASE.md` and `TESTING.md` contain stale and wrong credentials.** They also
  contain plaintext production credentials that should not be there. Do not set up against them.
- **Certificate routes are keyed by certificate number, not integer id.** `/Certificate/Edit/26-0224`
  works; `/Certificate/Edit/1308` returns HTTP 400. The POST half of Edit does take an int, so an
  integer "works" on submit and fails on load.
- **Seeded certificates have no PDFs on disk.** The uploads store and the seeded rows are disjoint,
  so preview/download of seeded certs will 404. Upload flows on newly created certs work fine.
- Secrets are intentionally absent from the repo: `appsettings.Secrets.json`,
  `appsettings.Production.json`, `.env*` and `DataProtectionKeys/` are all ignored. Templates for
  the first two are committed as `*.template.json`.

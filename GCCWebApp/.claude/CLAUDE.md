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
| Route | Access |
|-------|--------|
| `/Certificate/CertificateTileView` | Public |
| `/Certificate/CertificateTileViewExternal` | External+ |
| `/Certificate/Create`, `/Edit/{id}` | Analyst+ |
| `/Admin/*` | Admin only |
| `/api/v1/certificates` | API Key |

## Development Rules

**DO**: Read before edit, CSV exports, Modal→Delete→Toast, follow patterns, clean rebuild

**DON'T**: New auth systems, modify 8-state workflow, push to master, native JS dialogs, unrelated changes

**ALWAYS**: Check requirements, ask clarifying questions, review mockups, test changes

## Quick Credentials
| User | Password | Role |
|------|----------|------|
| admin | TestPass123! | Admin |
| analyst | TestPass123! | Analyst |
| external | TestPass123! | External |

## Feature Status
All complete: Certificate Tiles, Infinite Scroll, Autocomplete, Shareable URLs, Product IDs, CSV Import/Export, 8-State Workflow, RBAC, Partner API, Database Filters

## Notes
- Tech: ASP.NET Core MVC + SQL Server (not Django/PostgreSQL)
- External ≠ Analyst/Admin permissions
- Admin user/role cannot be deleted
- Emails must be unique per user
- 16px minimum font size

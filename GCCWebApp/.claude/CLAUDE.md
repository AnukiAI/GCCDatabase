# GCC Database Solution - AI Assistant Guide

ASP.NET Core 8.0 MVC + EF Core 9.0 + SQL Server + Azure AD B2C + Bootstrap 5

## Quick Start
```bash
dotnet restore && dotnet build && dotnet run --urls "http://localhost:5101"
```

## Reference Docs (Read when working on specific areas)
| Area | File | When to Read |
|------|------|--------------|
| Database | `docs/claude-ref/DATABASE.md` | DB connections, schema, migrations, troubleshooting |
| UI Standards | `docs/claude-ref/UI-STANDARDS.md` | Modals, buttons, notifications, colors, containers |
| View Patterns | `docs/claude-ref/VIEW-PATTERNS.md` | Admin pages, templates, tile views, forms |
| API | `docs/claude-ref/API-REFERENCE.md` | REST endpoints, Novi integration, authentication |
| Testing | `docs/claude-ref/TESTING.md` | Credentials, coverage, running tests |
| Deployment | `docs/claude-ref/DEPLOYMENT.md` | Production, web.config, troubleshooting |

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
| Design Guide | `/0_Documentation/mockups/0_0_MasterDesignGuide.html` |
| Requirements | `0_Documentation/docs/Requirements.md` |

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

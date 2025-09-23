# GCC Database Solution - AI Assistant Guide

Quick reference for Claude Code when working with this ASP.NET Core MVC application.

## Quick Start

```bash
dotnet restore && dotnet build
dotnet run --urls "http://localhost:5101"
dotnet ef database update
dotnet test
```

## Tech Stack
- **ASP.NET Core 8.0 MVC** with Entity Framework Core
- **SQL Server** (LocalDB dev / SQL Server prod)
- **Azure AD B2C** + Cookie Auth fallback
- **QuestPDF** for PDF generation

## Critical Requirements

### Certificate Tile View (Landing Page)
- **NO PAGINATION** - Infinite scroll implemented ✅
- Autocomplete search with categorized suggestions ✅
- Unique/shareable URLs for filtered searches ✅
- Download certificates directly from tiles

### Product ID Management ✅
- Multiple ASINs per certificate (100s supported)
- Product types: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
- Bulk CSV import/export with validation
- Migration: `20250918212035_AddCertificateProductIds`

### Partners & Workflow
- Partners: Amazon, Wayfair, None (no branding in Phase 1)
- 14-state certificate lifecycle (`CertificateWorkflowService`)
- Role-based permissions enforced

## Implementation Status ✅

**All 3 phases complete (2025-09-18)**:
- Database schema for 100s of ASINs per certificate
- Product ID Manager UI with bulk import/export
- Autocomplete search with categories
- Infinite scroll (no pagination)
- Shareable URLs for filtered searches
- Professional Excel export (ClosedXML)


## Key Files

**Core**: `CertificateController.cs`, `CertificateService.cs`, `CertificateWorkflowService.cs`
**Product IDs**: `ProductIdService.cs`, `CertificateProductId.cs`, `_ProductIdManager.cshtml`
**Search/UI**: `CertificateTilesApiController.cs`, `certificate-tile-view.js`
**Design**: `/0_Documentation/mockups/0_0_MasterDesignGuide.html`

## Documentation

**Requirements**: `0_Documentation/docs/Requirements.md` (US-001 to US-010)
**Wireframes**: `0_Documentation/00 GCC_ImplementationDocuments_8_22_25/GCC Wireframes_UserStories_8_25_25.pptx`
**Test Cases**: `0_Documentation/docs/MANUAL_UAT_TEST_CASES.md`

## Routes

**Public**: `/Certificate/CertificateTileView` (main), `/Certificate/Details/{id}`, `/Account/Login`
**Admin/Analyst**: `/Certificate/Create`, `/Certificate/Edit/{id}`, `/Dashboard`
**Admin Only**: `/Admin/UserManagement`, `/Admin/RoleManagement`

## API

**Public**: `GET /api/v1/certificates`, `/api/CertificateTileExternal/tiles`
**Authenticated**: `POST/PUT/DELETE /api/v1/certificates/{id}`
**Amazon**: `/api/CertificateTileExternal/*` (partner filtering)

## Test Credentials
Admin: `admin@greencirclecertified.com` / `Admin123!`
Analyst: `analyst@greencirclecertified.com` / `Analyst123!`
External: `partner@amazon.com` / `Partner123!`

## Database

**Dev**: SQLite (`Data Source=gcc_database.db`)
**Prod**: SQL Server
**Tables**: Certificates, Companies, Products, Users, ApiKeys, CertificateProductIds
**Key Migrations**: `20250918212035_AddCertificateProductIds`, `20250827182202_AddPartnerFieldToCertificate`

## Configuration

**Required**: `ASPNETCORE_ENVIRONMENT`, `GCC_JWT_SECRET` (64+ chars), DB connection string
**Optional**: Azure AD B2C, Application Insights, SMTP
**Feature Flags**: Check `appsettings.json`


## Amazon API Integration ✅

**Auth**: API Key (`X-API-Key` header) or JWT Bearer token
**Rate Limits**: 1000/hour for partners, configurable
**Swagger**: http://localhost:5101/swagger
**Partner endpoints**: `/api/CertificateTileExternal/*` with automatic filtering

## Docker Deployment

```bash
# Quick start
./docker-run-production.sh  # SQLite on port 5105
docker-compose up -d        # Full stack with PostgreSQL

# Build options
./docker-build.cmd          # Interactive build menu
```


**Docker compose configurations**: `docker-compose.yml` (PostgreSQL), `docker-compose.mariadb.yml`
**Health endpoint**: `/health`


## Development Guidelines

**Always check**: Mockups (`/0_Documentation/pages/`), Master Design Guide, existing patterns
**Common services**: `CertificateWorkflowService` (14-state), `ProductIdService`, `FileUploadService`
**Git**: Feature branches only, no direct master push


## Important Reminders

✅ **DO**: Infinite scroll (no pagination), autocomplete search, shareable URLs, minimal edits
❌ **DON'T**: Create new auth systems, modify 14-state workflow, push to master
📋 **ALWAYS**: Read before edit, test changes, follow patterns, update tests


## Production Status

**✅ ALL PHASES COMPLETE** - Ready for production deployment
*Last Updated: September 18, 2025*
## Special Notes

- **Tech**: ASP.NET Core MVC + SQL Server (not Django/PostgreSQL)
- **Design**: Follow Master Design Guide, 16px min font, no nav search bar
- **Testing**: Update tests for new features, verify JS rendering
- **Constraints**: Admin user/role cannot be deleted, Phase 1 has no partner branding
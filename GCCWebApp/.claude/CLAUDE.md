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

- **ASP.NET Core 8.0 MVC** + Entity Framework Core 9.0.9
- **SQL Server** (ANUKI01 dev / AccuWebHosting prod)
- **Azure AD B2C** + Cookie Auth fallback
- **QuestPDF** (PDF), **ClosedXML** (Excel), **Bootstrap 5**, **jQuery**
- **Testing**: xUnit 2.9.3, Moq 4.20.72, Playwright 1.55.0

## System Requirements

**Required**: .NET SDK 8.0.400+, SQL Server 2019+, VS 2022/VS Code + C#, Git
**Optional**: SSMS, Postman, Playwright browsers (`pwsh playwright.ps1 install`)
**System**: Windows 10/11, 8GB RAM (16GB recommended), Port 5101 (HTTP), Port 1533 (SQL prod)

## Setup & Installation

```bash
# 1. Clone & navigate
git clone [repository-url]
cd "GCC Database Solution\GCCWebApp"

# 2. Set environment
$env:ASPNETCORE_ENVIRONMENT = "Development"

# 3. Restore, migrate, build
cd GCCWebApp
dotnet restore
dotnet ef database update
dotnet build

# 4. Run application (auto-seeds data)
dotnet run --urls "http://localhost:5101"

# 5. Run tests
cd ../GCCWebApp-Tests/GCCWebApp.Tests
dotnet test
```

## Database Configuration

### Quick Reference

**Dev (ANUKI01)**:
```
Server: ANUKI01
Database: GCCDatabase_Dev
Auth: SQL (User: GCC_DevUser, Pass: DevP@ss2024!SQL)
Connection: Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false
```

**Prod (AccuWebHosting)**:
```
Server: 68.71.129.120,1533
Database: gccwebap_certificates
Username: GCC_adminxyz123
Password: Q3Ez4e!gE_47KNi
```

### Complete Database Documentation

**📚 All database documentation:** `docs/database/`

**Quick Links:**
- [Quick Start Guide](../docs/database/deployment/QUICK_START.md) - 5-minute database setup
- [Sync Dev to Prod](../docs/database/deployment/SYNC_DEV_TO_PROD.md) - Update production data
- [Critical Schema Requirement](../docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md) - **MUST READ**
- [Connection Strings](../docs/database/reference/CONNECTION_STRINGS.md) - Full connection details
- [Common Issues](../docs/database/troubleshooting/COMMON_ISSUES.md) - Troubleshooting guide
- [Database README](../docs/database/README.md) - Complete documentation index

### CRITICAL DATABASE REQUIREMENT - Schema Ownership

**⚠️ REQUIRED: All database tables MUST be in 'dbo' schema**

```
- Entity Framework requires dbo.TableName (not username.TableName)
- Production user MUST have:
  1. db_owner role membership
  2. DEFAULT_SCHEMA = dbo
- Common error: Tables created as GCC_adminxyz123.TableName instead of dbo.TableName
- Symptom: Certificates not displaying despite data in database
- Fix: See docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md
- Verify: SELECT SCHEMA_NAME(schema_id), name FROM sys.tables
  Should show: dbo | Certificates (not GCC_adminxyz123 | Certificates)
```

**See:** [Critical Schema Requirement](../docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md) for complete details.

**Key Tables**: Certificates, Companies, Products, Users, ApiKeys, CertificateProductIds, Roles, UserRoles
**Key Migrations**: `20250918212035_AddCertificateProductIds`, `20250827182202_AddPartnerFieldToCertificate`

## Environment Variables

```bash
# Required
ASPNETCORE_ENVIRONMENT=Development  # or Production, Staging

# Optional (if not in appsettings)
GCC_JWT_SECRET=[64+ character secret]
GCC_DB_CONNECTION=[connection string]

# Azure AD B2C (optional)
AzureAdB2C__Instance=[instance]
AzureAdB2C__Domain=[domain]
AzureAdB2C__ClientId=[client-id]
```

## Critical Requirements

### Certificate Tile View (Landing Page)
- **NO PAGINATION** - Infinite scroll ✅
- Autocomplete search with categorized suggestions ✅
- Unique/shareable URLs for filtered searches ✅
- Download certificates directly from tiles ✅
- **DATABASE-DRIVEN FILTERS** ✅ - All filter options dynamically loaded from database
  - Applies to ALL 3 views: Public, Internal, External
  - Filters only show items with active certificates (no "0 results")
  - Auto-updates when database changes
  - **Exception**: Product Categories in External view (~370 lines, deferred)
  - API: `CertificateTilesApiController.cs:180-232`, `CertificateTileInternalApiController.cs:324-333`, `CertificateTileExternalApiController.cs:278-395`
- **DESIGN CONSISTENCY** ✅ - External and Internal views visually identical (2025-10-30)
  - Filter panel checkboxes properly aligned with labels (10px spacing)
  - Custom styled thin scrollbars (8px width) in filter content areas
  - No visual barriers between checkboxes and text
  - Table grid layout with professional spacing (14px/10px padding)
  - Zebra striping for table rows (alternating #fff/#fafafa)
  - Typography: Monospace for certificate numbers and dates
  - Sticky table headers during vertical scrolling
  - Status badges with color coding (Active: green, Draft: yellow, Expired: red)
  - 52px minimum row height for comfortable reading
  - Smooth hover effects (0.2s transition) with proper color contrast
  - CSS: `wwwroot/css/certificatetileview.css` (unified styling)
- **BULK DOWNLOAD AS ZIP** ⚠️ REQUIRED
  - **PRIORITY: HIGH** - Download multiple certificates as single ZIP
  - Applies to External & Internal views
  - "Download Selected" button creates ZIP archive of PDFs
  - ZIP naming: `certificates_YYYY-MM-DD_HHmmss.zip`
  - Shows progress modal, provides feedback
  - Backend: `/Certificate/DownloadSelectedZip` using `System.IO.Compression.ZipArchive`
  - Frontend: Update `certificateTileViewExternal.js:1125`, `certificate-tile-view-internal.js`

### Product ID Management ✅
- Multiple ASINs per certificate (100s supported)
- Product types: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
- Bulk CSV import/export with validation

### Data Export Requirements
- **CSV ONLY** - All exports must use CSV format exclusively
- No Excel/XLSX, JSON, or PDF exports from admin pages
- Apply to: Tags, Users, Products, Companies, Certificate Types, Certificate Fields
- Use `ExportUtils.exportToCSV()` directly, NOT `ExportUtils.showExportModal()`

### External User Data Filtering ✅ **NEW**
- **REQUIREMENT**: External role users see ONLY their associated data
- **Product Management Page** (`/Product/Management`):
  - External users with **Companies**: Show products where `Product.CompanyId` matches user's company IDs
  - External users with **Partners**: Show products with certificates where `Certificate.Partner` matches user's partner names
  - External users with **BOTH**: Show products matching EITHER company OR partner (OR logic)
  - External users with **NEITHER**: Show no products (empty result)
- **Implementation**: `ProductController.cs` lines 426-444 (helper method), lines 114-157 (GetProducts filtering)
- **User Association**: Many-to-many relationships
  - `User.Companies` collection (ICollection<Company>)
  - `User.Partners` collection (ICollection<Partner>)
- **Partner Matching**: Certificate Partner field values: "Amazon", "Wayfair", "None"
- **Admin/Analyst**: No filtering - see all products regardless of associations
- **Dropdown Filtering**: See US-013 in requirements.md
  - **Companies**: FILTERED - External users see only their associated companies
  - **Categories**: UNFILTERED - All users see all active categories
  - **Product Types**: UNFILTERED - All users see all active product types
  - **Rationale**: Companies filtered for security; Categories/Types unfiltered for usability (External can't create/edit anyway)

### External User Access Restrictions ✅
- **REQUIREMENT**: External role users have restricted access to sensitive fields and operations
- **Internal Tags**:
  - External users **CANNOT view** internal tags on certificates
  - External users **CANNOT add or edit** internal tags
  - Internal Tag column hidden in External Certificate Tile View table
  - Admin/Analyst users have full access to internal tags
- **Add/Edit Operations**:
  - "Add Certificate" button hidden for External users (CSS: `display: none`)
  - Edit action buttons hidden in certificate views for External users
  - External users can only view and download certificates they have access to
- **Implementation**: Role-based UI hiding via CSS classes and backend authorization checks

### Two-Factor Authentication Admin Management ⚠️ REQUIRED
- **PRIORITY: HIGH** - Admins help users locked out by 2FA
- Admin can view 2FA status, reset 2FA, disable 2FA
- Backend endpoints exist: `/Admin/Reset2FA`, `/Admin/Disable2FA`
- JavaScript methods in `userManagement.js:712-838`
- **MISSING**: UI controls in User Management view

### Partners & Workflow
- Partners: Amazon, Wayfair, Other (core entities, NOT bulk manageable)
- **Partners Page**: ONLY Export + Add New Partner buttons (NO Template/Import)
- 14-state certificate lifecycle (`CertificateWorkflowService`)
- Role-based permissions enforced

### Partner API Access ⚠️ **REQUIRED - NEW**
- **REQUIREMENT**: Partners (Amazon, Wayfair, etc.) access GCC data via dedicated API keys
- **Architecture**: Partners pull data from GCC API (not push to partner APIs)
- **Scalability**: System must support multiple partners with individual API keys
- **Implementation**:
  - Each partner gets a unique API key to access GCC REST API endpoints
  - Partner API keys managed separately from general API keys
  - Track usage statistics per partner (requests, endpoints accessed, data volume)
  - Configure access permissions per partner (which endpoints they can access)
  - Support partner-specific rate limits and quotas
- **Admin Interface**: `/Admin/PartnerAPI` (formerly `/Admin/AmazonAPI`)
  - List all partners (Amazon, Wayfair, etc.)
  - Generate/revoke API keys for each partner
  - View usage statistics and access logs
  - Configure endpoint permissions and rate limits
- **Database**: `PartnerApiKey` table with partner association, key, permissions, usage stats
- **Security**: Partner API keys use same authentication as general API keys but with partner tracking
- **Future**: Add webhook support for partners to receive real-time updates

### User Feedback & Alert System

**CRITICAL**: All feedback must use custom components. Native `alert()`, `confirm()`, `prompt()` STRICTLY PROHIBITED.

#### Toast Notifications (Non-Blocking)
- **USE**: `AdminCore.showNotification(message, type)`
- **POSITIONING**: Top-right (desktop), full-width top (mobile)
- **COLORS**: Success (#92C83E), Error (#FF5252), Warning (#FFC107), Info (#002B76)
- **AUTO-DISMISS**: Success/Info: 5s, Error/Warning: 8s, Critical: Manual
- **WHEN**: Form submissions, save confirmations, background completion, non-critical warnings

#### Modal Confirmations (Blocking)
- **USE**: `AdminCore.showConfirmation(options)` for confirmations, `AdminCore.showProgress()` for uploads
- **REQUIRED FOR**: Delete actions, destructive operations, security changes, sign out
- **DELETE PATTERN**: BEFORE delete → Modal confirmation, AFTER delete → Toast notification

**Modal Types**:
1. **Delete Confirmation**: Title + message + Delete (red) + Cancel (green) buttons
2. **Success**: Title + message + OK (green) button, optional 3s auto-close
3. **Progress**: Title + message + progress bar + Cancel (green) button

**Example - Correct Delete Pattern**:
```javascript
// BEFORE: Modal confirmation
const confirmed = await AdminCore.showConfirmation({
    title: 'Delete Confirmation',
    message: 'This action cannot be undone. Delete this item?',
    confirmText: 'Delete',
    type: 'danger'
});
if (!confirmed) return;

// Execute deletion
await deleteItem(id);

// AFTER: Toast notification
AdminCore.showNotification('Item deleted successfully', 'success');
```

**WRONG**:
```javascript
// ❌ NEVER use native dialogs
if (confirm('Delete?')) { ... }

// ❌ NEVER use toast for confirmations
AdminCore.showNotification('Delete this item?', 'warning');

// ❌ NEVER use modal for success messages
await AdminCore.showConfirmation({ message: 'Deleted!' });
```

### Modal Backdrop Standard (US-015) - MANDATORY FOR ALL MODALS

**CRITICAL**: **ALL modals and popups** must follow this backdrop standard - no exceptions.

**Requirements** (see `docs/core/requirements.md` US-015 for full details):
- **85% black opacity backdrop**: `rgba(0, 0, 0, 0.85)`
- **3px blur effect**: `backdrop-filter: blur(3px)`
- **Full viewport coverage**: `position: fixed; top: 0; left: 0; right: 0; bottom: 0;`
- **Z-index layering**: Backdrop (9998), Modal container (9999)
- **Sibling structure**: Backdrop and container as siblings (not nested)
- **High contrast**: White background, dark text, visible borders

**Reference Implementation**: `Views/Admin/PartnerAPI.cshtml` (lines 183-272)

**Modal Structure Pattern**:
```html
<div id="modal-id" class="admin-modal" style="display: none;">
    <!-- Backdrop (sibling) -->
    <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0;
         background-color: rgba(0, 0, 0, 0.85); z-index: 9998;
         backdrop-filter: blur(3px);"></div>

    <!-- Modal Container (sibling) -->
    <div class="admin-modal-container" style="position: relative; z-index: 9999;
         max-width: 650px; background-color: white;
         box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);">
        <div class="admin-modal-header" style="background-color: #f8f9fa;
             border-bottom: 2px solid #e9ecef;">
            <h3 class="admin-modal-title" style="color: #002B76; font-weight: 600;">Title</h3>
            <button class="admin-modal-close"><i class="fas fa-times"></i></button>
        </div>
        <div class="admin-modal-body" style="background-color: white;">
            <!-- Modal content with high-contrast form elements -->
        </div>
        <div class="admin-modal-footer" style="background-color: #f8f9fa;
             border-top: 2px solid #e9ecef;">
            <!-- Action buttons -->
        </div>
    </div>
</div>
```

**Pages with Updated Modals**:
- ✅ Partner API Management (`Views/Admin/PartnerAPI.cshtml`)
- ✅ User Management (`Views/Admin/UserManagement.cshtml`)
- ✅ Role Management (`Views/Admin/RoleManagement.cshtml`)
- ⚠️ **All other modals must be updated to this standard**

## Import Template Download Feature

**Pattern**: All admin pages with import have "Template" button downloading CSV template.
**EXCEPTION**: Partners page has NO Template/Import buttons (core entities only).

**Button HTML**:
```html
<button id="template-btn" class="admin-btn admin-btn--secondary" title="Download CSV template for bulk import">
    <i class="fas fa-file-download"></i> Template
</button>
```

**Implementation** (condensed):
```javascript
downloadImportTemplate() {
    const templateData = [{ Field1: 'Example 1', Field2: 'Example 2' }];
    const headers = ['Field1', 'Field2'];
    const csvContent = [headers.join(','), ...templateData.map(row =>
        headers.map(h => (row[h] || '').includes(',') ? `"${row[h]}"` : row[h]).join(',')
    )].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `entity_import_template_${new Date().toISOString().split('T')[0]}.csv`;
    link.click();

    AdminCore.showNotification('Template downloaded', 'success');
}
```

**Implemented Pages**: User Management ✅, Tag Management ✅, Product Management ✅, Company Management ✅
**Exception**: Partners Management ❌ (NO Template/Import)

## View Development Standards

### Page Title Handling
- ✅ **DO**: Use `ViewData["Title"]` (rendered by `_Layout.cshtml`)
- ❌ **DON'T**: Add `<h1 class="admin-page-title">` in views (creates duplicate)

### Admin Page Header Structure
- ✅ **DO**: Use `.admin-header-content` → `.admin-title-section` + `.admin-actions-section`
- ❌ **DON'T**: Use deprecated `.buttons-area` or place buttons directly under `.admin-header`

**Example - CORRECT**:
```cshtml
@{ ViewData["Title"] = "Tag Management"; }
<div class="admin-container">
    <header class="admin-header">
        <div class="admin-header-content">
            <div class="admin-title-section">
                <p class="admin-page-subtitle">Organize content with tags</p>
            </div>
            <div class="admin-actions-section">
                <div class="admin-button-group">
                    <button class="admin-btn admin-btn--secondary">Export</button>
                    <button class="admin-btn admin-btn--primary">Add New</button>
                </div>
            </div>
        </div>
    </header>
</div>
```

**INCORRECT**: Adding `<h1 class="admin-page-title">Tag Management</h1>` inside view, or using `.buttons-area` class.

## Key Files & Routes

**Core**: `CertificateController.cs`, `CertificateService.cs`, `CertificateWorkflowService.cs`
**Product IDs**: `ProductIdService.cs`, `CertificateProductId.cs`, `_ProductIdManager.cshtml`
**Search/UI**: `CertificateTilesApiController.cs`, `certificate-tile-view.js`
**Design**: `/0_Documentation/mockups/0_0_MasterDesignGuide.html`
**Requirements**: `0_Documentation/docs/Requirements.md` (US-001 to US-010)

**Routes**:
- **Public**: `/Certificate/CertificateTileView`, `/Certificate/Details/{id}`, `/Account/Login`
- **External/Analyst/Admin**: `/Certificate/CertificateTileViewExternal` (requires ExternalAccess)
- **Admin/Analyst**: `/Certificate/Create`, `/Certificate/Edit/{id}`, `/Dashboard`
- **Admin Only**: `/Admin/UserManagement`, `/Admin/RoleManagement`

**API**:
- **Public**: `GET /api/v1/certificates`
- **External/Analyst/Admin**: `GET /api/CertificateTileExternal/*` (cookie or API key auth)
- **Authenticated**: `POST/PUT/DELETE /api/v1/certificates/{id}`
- **Swagger**: http://localhost:5101/swagger

**Test Credentials** (Login with USERNAME, not email):
- Admin: Username: `admin` / Password: `TestPass123!` (Email: swest@anukiconsulting.com)
- Analyst: Username: `analyst` / Password: `TestPass123!` (Email: swest@anukiconsulting.com)
- External: Username: `external` / Password: `TestPass123!` (Email: swest@anukiconsulting.com)

**All Test Accounts** (14 total, all use Password: `TestPass123!` and Email: `swest@anukiconsulting.com`):
| Username | Role | Name |
|----------|------|------|
| admin | Admin | Admin User |
| analyst | Analyst | Jane Analyst |
| external | External | External User |
| testuser | Public | Test User |
| sarah.johnson | Analyst | Sarah Johnson |
| mike.chen | Analyst | Mike Chen |
| lisa.rodriguez | External | Lisa Rodriguez |
| david.kim | External | David Kim |
| emma.wilson | External | Emma Wilson |
| alex.martinez | External | Alex Martinez |
| jennifer.lee | Analyst | Jennifer Lee |
| robert.taylor | Analyst | Robert Taylor |
| maria.garcia | External | Maria Garcia |
| public.user | Public | Public User |

**Note**: All 2FA emails will be sent to swest@anukiconsulting.com

## Feature Implementation Status

| User Story | Implementation | Status |
|------------|----------------|--------|
| US-001 | Certificate Tile View (Public) | ✅ Complete |
| US-002 | Infinite Scroll (No Pagination) | ✅ Complete |
| US-003 | Autocomplete Search | ✅ Complete |
| US-004 | Shareable URLs | ✅ Complete |
| US-005 | Product ID Management | ✅ Complete |
| US-006 | Bulk CSV Import/Export | ✅ Complete |
| US-007 | Certificate Workflow (14 states) | ✅ Complete |
| US-008 | Role-Based Access Control | ✅ Complete |
| US-009 | Partner API Access (Amazon, Wayfair, etc.) | ⚠️ Redesign Required |
| US-010 | Excel Export | ✅ Complete |
| FILTER-001 | Database-Driven Filters (All Views) | ✅ Complete (2025-10-27) |

## Test Coverage

| Component | Test Project | Tests | Pass Rate |
|-----------|--------------|-------|-----------|
| Configuration | GCCWebApp.Tests | 12 | 100% |
| Authentication | GCCWebApp.Tests | 15 | 100% |
| Models | GCCWebApp.Tests | 24 | 100% |
| Services | GCCWebApp.Tests | 70 | 97% |
| Controllers | GCCWebApp.Tests | 45 | 93% |
| API Endpoints | GCCWebApp.Api.Tests | 20 | 85% |
| Security | GCCWebApp.Security.Tests | 30 | 90% |
| E2E/UI | GCCWebApp.UAT.Tests | 80 | 75% |
| Performance | GCCWebApp.Performance.Tests | 10 | 80% |
| Accessibility | GCCWebApp.Accessibility.Tests | 8 | 100% |
| **TOTAL** | **7 projects** | **624+** | **87%** |

## Component Dependencies

```
CertificateController → CertificateService, CertificateWorkflowService, ProductIdService, FileUploadService → ApplicationDbContext
AdminController → UserService, RoleService → ApplicationDbContext
AccountController → SignInManager, UserManager, JwtTokenService
DashboardController → CertificateService → ApplicationDbContext
```

**Key Interfaces**: `ICertificateService`, `ICertificateWorkflowService`, `IProductIdService`, `IFileUploadService`, `IEmailService`

## Entity Relationships

```
Certificate (1) → (n) CertificateProductId (ASINs, UPCs, etc.)
Certificate (n) → (1) Company
Certificate (n) → (1) Product
Certificate (1) → (1) Partner (enum: Amazon, Wayfair, None)
User (n) → (n) Role (via UserRoles)
ApiKey (n) → (1) User
```

## Docker Deployment

```bash
./docker-run-production.sh  # SQLite on port 5105
docker-compose up -d        # Full stack with PostgreSQL
./docker-build.cmd          # Interactive build menu
```

**Configs**: `docker-compose.yml` (PostgreSQL), `docker-compose.mariadb.yml`
**Health**: `/health`

## Production Deployment (AccuWebHosting / Plesk)

### ⚠️ CRITICAL: web.config Must Be Plesk-Compatible

**Known Issue:** HTTP 500.19 error - "Duplicate collection entry" when deploying to Plesk/AccuWebHosting.

**Root Cause:** Plesk has server-level IIS configuration that conflicts with these web.config sections:
- `<fileExtensions>` - Blocking .db, .config, .log, .bak files
- `<hiddenSegments>` - Hiding bin, App_code, App_Data, logs directories

**Fix:** These sections MUST be removed from web.config before deployment to Plesk.

**Correct web.config for Plesk:**
```xml
<!-- Request Filtering - REMOVED fileExtensions and hiddenSegments to avoid Plesk conflicts -->
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="104857600" />
  </requestFiltering>
</security>
```

**WRONG - DO NOT USE ON PLESK:**
```xml
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="104857600" />
    <fileExtensions>  <!-- ❌ REMOVE THIS - Causes HTTP 500.19 -->
      <add fileExtension=".db" allowed="false" />
      <add fileExtension=".config" allowed="false" />
    </fileExtensions>
    <hiddenSegments>  <!-- ❌ REMOVE THIS - Causes HTTP 500.19 -->
      <add segment="bin" />
      <add segment="logs" />
    </hiddenSegments>
  </requestFiltering>
</security>
```

### Production Deployment Checklist

**⚠️ CRITICAL: ALWAYS check and fix SOURCE web.config FIRST before publishing!**

The web.config in the project root (`GCCWebApp/web.config`) is copied during publish. If it has the wrong sections, they will regenerate every time you publish.

Before publishing for production:
```bash
# 0. FIRST: Check SOURCE web.config in project root
# Verify GCCWebApp/web.config does NOT have <fileExtensions> or <hiddenSegments> sections
# These sections MUST be removed from the SOURCE, not just the published output

# 1. Clean publish
dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o publish-production

# 2. Verify web.config is correct (should already be correct if step 0 was done)
# Double-check publish-production/web.config has the fix

# 3. Create logs folder
mkdir -p publish-production/logs
touch publish-production/logs/.gitkeep

# 4. Create ZIP
powershell -Command "Compress-Archive -Path './publish-production/*' -DestinationPath './GCCWebApp-Production-Deploy.zip' -CompressionLevel Optimal -Force"
```

**Files to check/fix:**
- ✅ **SOURCE**: `GCCWebApp/web.config` (project root) - FIX THIS FIRST
- ✅ **OUTPUT**: `publish-production/web.config` (verify after publish)

### Other Common Deployment Issues

**Data Protection Error (HTTP 500):**
- Check `appsettings.Production.json` has `"PersistKeysToAzureBlobStorage": false`
- Azure Blob placeholders cause app crash on startup

**Missing Logs Folder:**
- web.config requires `logs/` folder to exist
- IIS needs write permissions: `icacls logs /grant "IIS_IUSRS:(OI)(CI)F"`

**Full Documentation:** See `docs/deployment/HTTP_500_TROUBLESHOOTING.md` and `docs/deployment/WEB_CONFIG_FIX_DEPLOYMENT_20251010.md`

## Troubleshooting

**Database Connection Issues**:
```bash
# Cannot connect to ANUKI01
Get-Service MSSQLSERVER
sqlcmd -S ANUKI01 -E -Q "SELECT @@VERSION"

# Login failed
# Verify credentials in appsettings.Development.json, reset password in SSMS

# Database doesn't exist
sqlcmd -S ANUKI01 -U GCC_DevUser -P DevP@ss2024!SQL -Q "CREATE DATABASE GCCDatabase_Dev"
dotnet ef database update
```

**Build Issues**:
```bash
# File locked
taskkill /F /IM dotnet.exe && taskkill /F /IM GCCWebApp.exe
dotnet clean

# Package restore failed
dotnet nuget locals all --clear
dotnet restore --verbosity detailed
```

**Runtime Issues**:
```bash
# Data not loading
# Check logs, verify migrations (dotnet ef migrations list), verify data exists, check browser console (F12)

# 401 Unauthorized
# Verify login cookie "GCCAuth", verify role in DB, clear cookies and re-login

# JWT expired
# Verify JWT config (64+ char key), check jwt.io, check system time sync
```

**Test Issues**:
```bash
# Playwright browser not found
cd GCCWebApp-Tests/GCCWebApp.Tests/bin/Debug/net8.0
pwsh playwright.ps1 install
```

**Performance Issues**:
```bash
# Slow page loads: Enable detailed logging, check SQL Profiler, verify indexes, check network latency
# High memory: Check EF tracking (use AsNoTracking()), verify disposal, check circular references, profile with dotMemory
```

## Known Issues

| Issue | Impact | Fix | Status |
|-------|--------|-----|--------|
| SQL Server LocalDB Logon Trigger | Error 17892 | Switched to ANUKI01 | ✅ RESOLVED |
| Playwright Browsers Not Installed | 86 E2E tests fail | `pwsh playwright.ps1 install` | Documented |
| API Key Handler Mock Setup | 12 auth tests fail | Replace extension method mocks | Documented |
| Nullable Reference Warnings | 58+ warnings (CS8618) | Can ignore (EF Core initializes) | Low priority |

## Development Guidelines

**Always check**: Mockups (`/0_Documentation/pages/`), Master Design Guide, existing patterns
**Common services**: `CertificateWorkflowService` (14-state), `ProductIdService`, `FileUploadService`
**Git**: Feature branches only, no direct master push

## Important Reminders

✅ **DO**: Infinite scroll, autocomplete search, shareable URLs, minimal edits, CSV-only exports, use `AdminCore.showConfirmation()` BEFORE delete, use `AdminCore.showNotification()` AFTER delete
❌ **DON'T**: Create new auth systems, modify 14-state workflow, push to master, use Excel/JSON exports, use native `alert()`/`confirm()`/`prompt()`, use toast for delete confirmations
📋 **ALWAYS**: Read before edit, test changes, follow patterns, update tests, use CSV exports, show MODAL before deletes, show TOAST after deletes, clean rebuild after changes

## Special Notes

- **Tech**: ASP.NET Core MVC + SQL Server (not Django/PostgreSQL)
- **Design**: Follow Master Design Guide, 16px min font, no nav search bar
- **Testing**: Update tests for new features, verify JS rendering
- **Constraints**: Admin user/role cannot be deleted, Phase 1 has no partner branding
- Do not include icons in text
- External Users ≠ Analyst/Admin permissions. Admin has CRUD Roles. Analyst has read/write but NO Admin page access.
- **Product Management Actions Column**: Actions column (Edit/Delete buttons) only visible to Admin and Analyst roles. External users see NO Actions column to avoid confusing empty column.
- Stop making random changes unrelated to the defect
- Ask followup questions for clarity and focus on the issue
- Review requirements and current design, determine exact change, include what changes will be made for quality check
- Always clean rebuild and run after improvements. If build fails, kill process and resolve errors/warnings
- Download Selected on External/Internal views needs ZIP download capability for all selected certificates

## Production Status

**✅ ALL PHASES COMPLETE** - Ready for production deployment
*Last Updated: October 15, 2025*

**Testing Status**: 624+ tests, 87% pass rate (expected 95% after Playwright install)
**Database**: ANUKI01 configured, migrations pending
**Documentation**: Comprehensive setup, requirements, and traceability complete

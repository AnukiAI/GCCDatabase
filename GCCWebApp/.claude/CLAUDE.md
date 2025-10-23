# GCC Database Solution - AI Assistant Guide

Quick reference for Claude Code when working with this ASP.NET Core MVC application.

## Quick Start

```bash
dotnet restore && dotnet build
dotnet run --urls "http://localhost:5101"
dotnet ef database update
dotnet test
```

## Prerequisites & System Requirements

### Required Software
- **.NET SDK**: 8.0.400 or higher (compatible with 9.0.x)
  - Verify: `dotnet --version`
- **SQL Server**: SQL Server 2019+ or SQL Server 2022
  - Development: Local instance (ANUKI01) with Windows Authentication
  - Production: SQL Server on AccuWebHosting (68.71.129.120:1533)
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** for version control

### Optional Tools
- **SQL Server Management Studio (SSMS)** for database management
- **Postman** or **Insomnia** for API testing
- **Playwright** browsers for E2E testing: `pwsh playwright.ps1 install`

### System Requirements
- **OS**: Windows 10/11 or Windows Server 2019+
- **RAM**: 8GB minimum, 16GB recommended
- **Disk Space**: 2GB for application + dependencies
- **Network**: Port 5101 (HTTP), Port 1533 (SQL Server production)

### .NET Dependencies (Managed by NuGet)
- **Entity Framework Core**: 9.0.9
- **Microsoft Identity Web**: 3.10.0
- **QuestPDF**: Latest stable (PDF generation)
- **ClosedXML**: Latest stable (Excel export)
- **xUnit**: 2.9.3 (testing)
- **FluentAssertions**: 8.7.1 (testing)
- **Moq**: 4.20.72 (mocking)
- **Microsoft.Playwright**: 1.55.0 (E2E testing)

## Tech Stack
- **ASP.NET Core 8.0 MVC** with Entity Framework Core
- **SQL Server** (ANUKI01 dev / AccuWebHosting prod)
- **Azure AD B2C** + Cookie Auth fallback
- **QuestPDF** for PDF generation
- **Bootstrap 5** for UI framework
- **jQuery** for client-side interactions

## Detailed Setup & Installation

### 1. Clone Repository
```bash
git clone [repository-url]
cd "GCC Database Solution\GCCWebApp"
```

### 2. Configure Database (Development)

**Option A: Local SQL Server (ANUKI01) - Recommended**
```bash
# Verify SQL Server is running
# Ensure Windows Authentication is enabled
# User must have db_owner permissions

# Connection string is already configured in:
# - appsettings.Development.json
# - Data/ApplicationDbContextFactory.cs

# Connection details:
# Server: ANUKI01
# Database: GCCDatabase_Dev
# Authentication: SQL Authentication
# User: GCC_DevUser
# Password: DevP@ss2024!SQL
```

**Option B: Production SQL Server (AccuWebHosting)**
```bash
# Update appsettings.Production.json with:
# Server: 68.71.129.120,1533
# Database: gccwebap_certificates
# Username: GCC_adminxyz123
# Password: [from secure vault]
```

### 3. Restore Dependencies
```bash
cd GCCWebApp
dotnet restore
```

### 4. Apply Database Migrations
```bash
# Ensure ASPNETCORE_ENVIRONMENT is set
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run migrations to create/update database schema
dotnet ef database update

# Verify database creation
# You should see tables: Certificates, Companies, Products, Users,
# ApiKeys, CertificateProductIds, Roles, UserRoles, etc.
```

### 5. Build Application
```bash
dotnet build

# Expected output: Build succeeded with 0 errors
# Note: 58+ nullable reference warnings are known and can be ignored
```

### 6. Seed Initial Data (First Run)
```bash
# Run application - data seeding happens automatically
dotnet run --urls "http://localhost:5101"

# Initial seed data includes:
# - Admin user: admin@greencirclecertified.com
# - Analyst user: analyst@greencirclecertified.com
# - External user: partner@amazon.com
# - Sample certificates for testing
# - Product types (ASIN, UPC, GTIN, etc.)
# - Certificate types (FSC, PEFC, SFI, etc.)
```

### 7. Verify Installation
Open browser to: http://localhost:5101

**Verification Checklist:**
- [ ] Certificate Tile View loads without errors
- [ ] Sample certificates are displayed (if seeded)
- [ ] Login page accessible at /Account/Login
- [ ] Can login with test credentials
- [ ] Admin can access /Admin/UserManagement
- [ ] Database queries execute successfully (check logs)

### 8. Run Tests
```bash
# Run main test suite
cd ../GCCWebApp-Tests/GCCWebApp.Tests
dotnet test

# Expected results (after database setup):
# Total: 508 tests
# Pass Rate: 90-95% (after Playwright installation)

# Install Playwright browsers (fixes 86 test failures)
cd bin/Debug/net8.0
pwsh playwright.ps1 install

# Run all test projects
cd ../../..
dotnet test GCCWebApp.sln
```

## Environment Configuration

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;MultipleActiveResultSets=true;TrustServerCertificate=true;Encrypt=false"
  },
  "Authentication": {
    "EnableTestMode": true,
    "Enable2FA": false
  },
  "Jwt": {
    "Key": "[64+ character secret key]",
    "Issuer": "GCCWebApp",
    "Audience": "GCCWebApp",
    "ExpiryMinutes": 60
  },
  "Features": {
    "TwoFactorAuthentication": false,
    "RequireEmailConfirmation": false,
    "AllowGuestAccess": true
  }
}
```

### Environment Variables
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
- **NO PAGINATION** - Infinite scroll implemented ✅
- Autocomplete search with categorized suggestions ✅
- Unique/shareable URLs for filtered searches ✅
- Download certificates directly from tiles

### Product ID Management ✅
- Multiple ASINs per certificate (100s supported)
- Product types: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
- Bulk CSV import/export with validation
- Migration: `20250918212035_AddCertificateProductIds`

### Data Export Requirements
- **CSV ONLY** - All data exports must use CSV format exclusively
- No Excel/XLSX, JSON, or PDF exports from admin pages
- Apply to: Tags, Users, Products, Companies, Certificate Types, Certificate Fields
- Implementation: Use `ExportUtils.exportToCSV()` directly, NOT `ExportUtils.showExportModal()`
- Template downloads also use CSV format

### Two-Factor Authentication Admin Management ⚠️ REQUIRED
- **PRIORITY: HIGH** - Admins must be able to help users locked out by 2FA issues
- Admin can view user's 2FA status (enabled/disabled, method, date configured)
- Admin can reset user's 2FA (removes setup, forces reconfiguration)
- Admin can disable user's 2FA (allows login without 2FA)
- 2FA dropdown menu in User Management table Actions column
- Backend endpoints already exist (`/Admin/Reset2FA`, `/Admin/Disable2FA`)
- JavaScript methods already implemented (`userManagement.js` lines 712-838)
- **MISSING**: UI controls in User Management view
- **Full Requirements**: `/0_Documentation/2FA_ADMIN_MANAGEMENT_REQUIREMENTS.md`
- Use cases: lost device, phone number change, authenticator app issues, time sync problems

### Partners & Workflow
- Partners: Amazon, Wayfair, Other (core entities, NOT bulk manageable)
- **Partners Page Actions**: ONLY Export and Add New Partner buttons
  - ❌ **NO Template button** - Partners are core entities, not bulk imported
  - ❌ **NO Import button** - Partners are managed individually (Amazon, Wayfair, Other)
  - ✅ Export button - For reporting/backup purposes
  - ✅ Add New Partner button - For individual partner creation
- 14-state certificate lifecycle (`CertificateWorkflowService`)
- Role-based permissions enforced

### User Feedback & Alert System Standardization

**CRITICAL REQUIREMENT**: All user feedback must use standardized custom components. Native browser alerts (`alert()`, `confirm()`, `prompt()`) are STRICTLY PROHIBITED throughout the entire application.

#### Toast Notifications (Non-Blocking Messages)
- **SINGLE SYSTEM ONLY** - All toast notifications must use `AdminCore.showNotification()`
- **POSITIONING** - Fixed top-right (desktop), full-width top (mobile)
- **COLOR-CODED** - Success: Green (#92C83E), Error: Red (#FF5252), Warning: Yellow (#FFC107), Info: Navy Blue (#002B76)
- **AUTO-DISMISS** - Success/Info: 5s, Error/Warning: 8s, Critical: Manual (`duration: 0`)
- **ACCESSIBILITY** - ARIA labels, screen reader support, high contrast mode, reduced motion support

**When to use Toast Notifications:**
- Form submission success/error
- Data save confirmations
- Background process completion
- Non-critical warnings
- General information messages

#### Modal Confirmations (Blocking User Actions)
- **CRITICAL REQUIREMENT** - ALL delete confirmations MUST use modal popups, NOT toast notifications
- **REQUIRED FOR** - Delete actions, destructive operations, irreversible changes, data loss warnings
- **NO NATIVE DIALOGS** - NEVER use `window.confirm()` or `window.alert()`
- **CUSTOM MODALS ONLY** - Use `AdminCore.showConfirmation()` for all confirmation dialogs
- **DELETE PATTERN** - Before deletion → Show modal confirmation, After deletion → Show toast notification for success/error

**Modal Design Requirements:**

1. **Delete Confirmation Modals**
   - Title: Bold, clear action description (e.g., "Delete Confirmation")
   - Message: Explain consequences ("This action is irreversible. Are you sure you want to delete this item?")
   - Buttons:
     - Delete: Red background (#FF5252), white text, left position
     - Cancel: Green background (#92C83E), white text, right position
   - Modal styling: White background, rounded corners, centered on screen, overlay backdrop

2. **Success Modals** (after completing actions)
   - Title: Bold success message (e.g., "Delete Successful", "Update Successful")
   - Message: Confirm what happened ("The item was successfully deleted.")
   - Button: Single green OK button (#92C83E), full width, centered
   - Auto-close option: Optional 3-second auto-dismiss after display

3. **Progress Modals** (file uploads, long operations)
   - Title: Action in progress (e.g., "Uploading File")
   - Message: Status text ("Your file is being uploaded. Please wait...")
   - Progress bar: Visual indicator of completion (green fill #92C83E on gray background)
   - Cancel button: Green background (#92C83E), full width, positioned below progress bar
   - Must prevent user from navigating away during operation

**Modal Component Structure:**
```html
<!-- Delete Confirmation Example -->
<div class="modal-overlay">
    <div class="modal-dialog">
        <h2 class="modal-title">Delete Confirmation</h2>
        <p class="modal-message">This action is irreversible. Are you sure you want to delete this item?</p>
        <div class="modal-actions">
            <button class="modal-btn modal-btn--danger">Delete</button>
            <button class="modal-btn modal-btn--success">Cancel</button>
        </div>
    </div>
</div>

<!-- Progress Modal Example -->
<div class="modal-overlay">
    <div class="modal-dialog">
        <h2 class="modal-title">Uploading File</h2>
        <p class="modal-message">Your file is being uploaded. Please wait...</p>
        <div class="progress-bar">
            <div class="progress-fill" style="width: 60%"></div>
        </div>
        <button class="modal-btn modal-btn--success modal-btn--full">Cancel Upload</button>
    </div>
</div>

<!-- Success Modal Example -->
<div class="modal-overlay">
    <div class="modal-dialog">
        <h2 class="modal-title">Update Successful</h2>
        <p class="modal-message">Your changes have been saved successfully.</p>
        <button class="modal-btn modal-btn--success modal-btn--full">OK</button>
    </div>
</div>
```

**CSS Requirements:**
- `.modal-overlay`: Semi-transparent dark backdrop (rgba(0, 0, 0, 0.5))
- `.modal-dialog`: White background, centered, max-width 500px, padding 24px, border-radius 8px
- `.modal-title`: Bold, 20px font size, navy blue (#002B76)
- `.modal-message`: Regular text, 16px font size, gray (#666)
- `.modal-btn--danger`: Red background (#FF5252), white text
- `.modal-btn--success`: Green background (#92C83E), white text
- `.modal-btn--full`: Full width button
- `.progress-bar`: Gray background (#E0E0E0), height 20px, rounded corners
- `.progress-fill`: Green background (#92C83E), animated transition

**CRITICAL: When to use Modals vs Toast Notifications:**

**ALWAYS Use Modals (AdminCore.showConfirmation()) for:**
- ✅ Delete confirmations (BEFORE deleting) - User must explicitly confirm
- ✅ Destructive actions that cannot be undone
- ✅ Critical warnings requiring user decision
- ✅ Security changes (disable 2FA, reset password, etc.)
- ✅ Sign out confirmations
- ✅ Any action requiring explicit user consent

**ALWAYS Use Toast Notifications (AdminCore.showNotification()) for:**
- ✅ Success messages AFTER actions complete (e.g., "Item deleted successfully")
- ✅ Form validation errors (e.g., "Email is required")
- ✅ Background task completion
- ✅ Informational messages
- ✅ Error messages from failed operations

**Use Progress Modals (AdminCore.showProgress()) for:**
- ✅ File uploads with percentage tracking
- ✅ Batch operations with progress updates
- ✅ Long-running tasks requiring user awareness

**Example - Correct Delete Pattern:**
```javascript
// STEP 1: Show modal confirmation BEFORE delete
const confirmed = await AdminCore.showConfirmation({
    title: 'Delete Confirmation',
    message: 'This action cannot be undone. Delete this item?',
    confirmText: 'Delete',
    type: 'danger'
});

if (!confirmed) return;

// STEP 2: Perform deletion
await deleteItem(id);

// STEP 3: Show toast notification AFTER delete
AdminCore.showNotification('Item deleted successfully', 'success');
```

**WRONG - Do NOT do this:**
```javascript
// ❌ WRONG: Using toast for delete confirmation
AdminCore.showNotification('Delete this item?', 'warning'); // NO!

// ❌ WRONG: Using native confirm
if (confirm('Delete?')) { ... } // NO!

// ❌ WRONG: Using modal for success message after delete
await AdminCore.showConfirmation({ message: 'Deleted!' }); // NO!
```

**Implementation Checklist:**
- [ ] Identify all instances of `alert()`, `confirm()`, `prompt()` in JavaScript files
- [ ] Replace with custom modal components
- [ ] Ensure consistent styling across all modals
- [ ] Add keyboard navigation support (Enter, Escape)
- [ ] Implement focus trapping within modals
- [ ] Add ARIA labels for screen readers
- [ ] Test on mobile devices for responsive behavior

**DOCUMENTATION** - Full audit and requirements: `0_Documentation/NOTIFICATION_SYSTEM_AUDIT.md`

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
**External/Analyst/Admin**: `/Certificate/CertificateTileViewExternal` (requires ExternalAccess policy)
**Admin/Analyst**: `/Certificate/Create`, `/Certificate/Edit/{id}`, `/Dashboard`
**Admin Only**: `/Admin/UserManagement`, `/Admin/RoleManagement`

## API

**Public**: `GET /api/v1/certificates`
**External/Analyst/Admin**: `GET /api/CertificateTileExternal/*` (requires ExternalAccess policy, cookie-based auth after 2FA)
**Authenticated**: `POST/PUT/DELETE /api/v1/certificates/{id}`
**Partner API**: `/api/CertificateTileExternal/*` can also accept API Key authentication for partners

## Test Credentials
Admin: `admin@greencirclecertified.com` / `Admin123!`
Analyst: `analyst@greencirclecertified.com` / `Analyst123!`
External: `partner@amazon.com` / `Partner123!`

## Database

**Dev**: SQL Server on ANUKI01
  - Server: `ANUKI01`
  - Database: `GCCDatabase_Dev`
  - Authentication: Windows Integrated Security
  - Connection String: `Server=ANUKI01;Database=GCCDatabase_Dev;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true`
**Prod**: SQL Server on AccuWebHosting
  - Server: `68.71.129.120,1533` (port 1533)
  - Database: `gccwebap_certificates`
  - Username: `GCC_adminxyz123`
  - Connection: IP-based (domain name not configured)
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

✅ **DO**: Infinite scroll (no pagination), autocomplete search, shareable URLs, minimal edits, CSV-only exports, use `AdminCore.showConfirmation()` for delete confirmations (BEFORE action), use `AdminCore.showNotification()` for success/error messages (AFTER action)
❌ **DON'T**: Create new auth systems, modify 14-state workflow, push to master, use Excel/JSON exports, use native `alert()`/`confirm()`/`prompt()` anywhere, use toast notifications for delete confirmations
📋 **ALWAYS**: Read before edit, test changes, follow patterns, update tests, use CSV for all data exports, show MODAL confirmation before deletes, show TOAST notification after deletes

## Import Template Download Feature

### Overview
All admin pages with data import functionality include a "Template" button that downloads a properly formatted CSV template file to help admins prepare import data correctly.

### Implementation Pattern

**Button Placement**: Template button appears to the left of the Import button in the `admin-actions-section`.

**IMPORTANT EXCEPTION - Partners Page**:
- ❌ Partners Management page does NOT have Template or Import buttons
- Partners are core entities (Amazon, Wayfair, Other) managed individually, NOT bulk imported
- Only Export and Add New Partner buttons are allowed on `/Admin/Partners`

**Button HTML**:
```html
<button id="template-btn" class="admin-btn admin-btn--secondary" title="Download CSV template for bulk import">
    <i class="fas fa-file-download"></i> Template
</button>
```

**JavaScript Implementation** (Class-based approach):
```javascript
downloadImportTemplate() {
    try {
        // Create template data with example row
        const templateData = [
            { Field1: 'Example Value 1', Field2: 'Example Value 2', ... }
        ];

        // Create CSV content with headers and example row
        const headers = ['Field1', 'Field2', ...];
        const csvContent = [
            headers.join(','),
            ...templateData.map(row =>
                headers.map(header => {
                    const value = row[header] || '';
                    return value.includes(',') || value.includes('"')
                        ? `"${value.replace(/"/g, '""')}"`
                        : value;
                }).join(',')
            )
        ].join('\n');

        // Download file
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', `entity_import_template_${new Date().toISOString().split('T')[0]}.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        this.showNotification('Import template downloaded successfully', 'success');
    } catch (error) {
        console.error('Error downloading template:', error);
        this.showNotification('Error downloading template. Please try again.', 'error');
    }
}
```

**JavaScript Implementation** (Function-based approach for non-class modules):
```javascript
function downloadImportTemplate() {
    try {
        // ... same implementation as above, use AdminCore.showNotification() instead
        AdminCore.showNotification('Import template downloaded successfully', 'success');
    } catch (error) {
        console.error('Error downloading template:', error);
        AdminCore.showNotification('Error downloading template. Please try again.', 'error');
    }
}
```

**Event Binding**:
- Class-based: `document.getElementById('template-btn')?.addEventListener('click', () => this.downloadImportTemplate());`
- Function-based: `document.getElementById('template-btn')?.addEventListener('click', downloadImportTemplate);`

### Implemented Pages

| Page | View | JavaScript | Template Format | Notes |
|------|------|------------|-----------------|-------|
| User Management | `Views/Admin/UserManagement.cshtml` | `wwwroot/js/userManagement.js` (class) | Name, Email, Company, Role, Status, Password, RequirePasswordReset | ✅ Template + Import + Export |
| Tag Management | `Views/Admin/Tags.cshtml` | `wwwroot/js/tags.js` (function) | Name, TagType (external/internal), Description | ✅ Template + Import + Export |
| Product Management | `Views/Admin/ProductManagement.cshtml` | `wwwroot/js/admin/productManagement.js` (class) | Name, Description, ProductType, Company, Status | ✅ Template + Import + Export |
| Company Management | `Views/Company/Index.cshtml` | Inline script in view (function) | Name, Email, Website | ✅ Template + Import + Export |
| **Partners Management** | `Views/Admin/Partners.cshtml` | Inline script in view (function) | **N/A - No Template/Import** | ❌ NO Template/Import. ONLY Export + Add New |

### Template File Naming Convention
- Format: `{entity}_import_template_{YYYY-MM-DD}.csv`
- Examples:
  - `users_import_template_2025-10-18.csv`
  - `tags_import_template_2025-10-18.csv`
  - `products_import_template_2025-10-18.csv`
  - `companies_import_template_2025-10-18.csv`

## View Development Standards

### Page Title Handling
**CRITICAL REQUIREMENT**: Views must NOT include duplicate `<h1>` page titles.

- ✅ **DO**: Use `ViewData["Title"]` to set the page title (handled by shared layout)
- ❌ **DON'T**: Add `<h1 class="admin-page-title">` or similar title elements in views
- The shared layout (`_Layout.cshtml`) automatically renders the page title
- Views should only contain subtitles, content, and action buttons

### Admin Page Header Structure
**CRITICAL REQUIREMENT**: All admin pages must use the standard header structure for proper action button alignment.

- ✅ **DO**: Use `.admin-header-content` wrapper with `.admin-title-section` and `.admin-actions-section`
- ❌ **DON'T**: Use deprecated `.buttons-area` class or place buttons directly under `.admin-header`
- Action buttons must be right-aligned with the page content area
- Button groups should use `.admin-button-group` class with appropriate button styles (`admin-btn--primary`, `admin-btn--secondary`)

**Example - CORRECT:**
```cshtml
@{
    ViewData["Title"] = "Tag Management";
}

<div class="admin-container">
    <header class="admin-header">
        <div class="admin-header-content">
            <div class="admin-title-section">
                <p class="admin-page-subtitle">Organize content with tags and categories</p>
            </div>
            <div class="admin-actions-section">
                <div class="admin-button-group">
                    <button class="admin-btn admin-btn--secondary">
                        <i class="fas fa-download"></i> Export
                    </button>
                    <button class="admin-btn admin-btn--primary">
                        <i class="fas fa-plus"></i> Add New
                    </button>
                </div>
            </div>
        </div>
    </header>
</div>
```

**Example - INCORRECT (Duplicate Title):**
```cshtml
@{
    ViewData["Title"] = "Tag Management";
}

<div class="admin-container">
    <header class="admin-header">
        <div class="admin-header-content">
            <div class="admin-title-section">
                <h1 class="admin-page-title">Tag Management</h1> <!-- ❌ REMOVE THIS -->
                <p class="admin-page-subtitle">Organize content with tags and categories</p>
            </div>
        </div>
    </header>
</div>
```

**Example - INCORRECT (Old Header Structure):**
```cshtml
<div class="admin-container">
    <header class="admin-header">
        <div class="buttons-area"> <!-- ❌ DEPRECATED - Use admin-header-content instead -->
            <button class="btn">Export</button>
            <button class="btn btn-add">Add New</button>
        </div>
    </header>
</div>
```

**Applies to all views:**
- Admin views (`/Views/Admin/*.cshtml`)
- Certificate views (`/Views/Certificate/*.cshtml`)
- Account views (`/Views/Account/*.cshtml`)
- All other Razor views


## Traceability & Requirements Mapping

### Feature Implementation Status

| User Story | Requirement | Implementation | Tests | Status |
|------------|-------------|----------------|-------|--------|
| US-001 | Certificate Tile View (Public) | CertificateController.cs:105, certificate-tile-view.js | GCCWebApp.UAT.Tests/Tests/Integration/CertificateTileViewTests.cs | ✅ Complete |
| US-002 | Infinite Scroll (No Pagination) | certificate-tile-view.js:250-320 | Integration tests | ✅ Complete |
| US-003 | Autocomplete Search | certificate-tile-view.js:150-200, CertificateTilesApiController.cs:45 | SearchTests.cs | ✅ Complete |
| US-004 | Shareable URLs | certificate-tile-view.js:80-120, query string params | URLTests.cs | ✅ Complete |
| US-005 | Product ID Management | ProductIdService.cs, _ProductIdManager.cshtml | GCCWebApp.Tests/Services/ProductIdServiceTests.cs | ✅ Complete |
| US-006 | Bulk CSV Import/Export | ProductIdService.cs:150-250, CertificateController.cs:300 | ProductIdImportTests.cs | ✅ Complete |
| US-007 | Certificate Workflow (14 states) | CertificateWorkflowService.cs | WorkflowTests.cs | ✅ Complete |
| US-008 | Role-Based Access Control | Authorization policies in Program.cs:80-95 | GCCWebApp.Tests/Authentication/AuthenticationTests.cs | ✅ Complete |
| US-009 | Amazon API Integration | CertificateTilesApiController.cs, ApiKeyAuthenticationHandler.cs | GCCWebApp.Api.Tests/Unit/Controllers/CertificateApiTests.cs | ✅ Complete |
| US-010 | Excel Export | CertificateController.cs:250, ClosedXML integration | ExportTests.cs | ✅ Complete |

### Database Schema Traceability

**Migration History:**
1. `20250827182202_AddPartnerFieldToCertificate` - Added Partner field for Amazon/Wayfair filtering
2. `20250918212035_AddCertificateProductIds` - Added many-to-many relationship for Product IDs (ASINs, UPCs, etc.)
3. Previous migrations - Core schema (Certificates, Companies, Products, Users, ApiKeys)

**Entity Relationships:**
```
Certificate (1) ──> (n) CertificateProductId (Product IDs: ASIN, UPC, etc.)
Certificate (n) ──> (1) Company
Certificate (n) ──> (1) Product
User (n) ──> (n) Role (via UserRoles join table)
ApiKey (n) ──> (1) User
Certificate (1) ──> (1) Partner (enum: Amazon, Wayfair, None)
```

### Test Coverage by Component

| Component | Test Project | Test Count | Pass Rate | Location |
|-----------|--------------|------------|-----------|----------|
| Configuration | GCCWebApp.Tests | 12 | 100% | Configuration/ConfigurationValidationTests.cs |
| Authentication | GCCWebApp.Tests | 15 | 100% | Authentication/AuthenticationTests.cs |
| Models | GCCWebApp.Tests | 24 | 100% | Models/*.cs |
| Services | GCCWebApp.Tests | 70 | 97% | Services/*.cs |
| Controllers | GCCWebApp.Tests | 45 | 93% | Controllers/*.cs |
| API Endpoints | GCCWebApp.Api.Tests | 20 | 85% | Unit/Controllers/*.cs |
| Security | GCCWebApp.Security.Tests | 30 | 90% | Tests/*.cs |
| E2E/UI | GCCWebApp.UAT.Tests | 80 | 75% | Tests/Integration/*.cs |
| Performance | GCCWebApp.Performance.Tests | 10 | 80% | Tests/*.cs |
| Accessibility | GCCWebApp.Accessibility.Tests | 8 | 100% | Tests/*.cs |
| **Total** | **7 test projects** | **624+** | **87%** | - |

### Component Dependencies

**Controllers → Services → Data Access:**
```
CertificateController
  ├─> CertificateService (business logic)
  ├─> CertificateWorkflowService (state machine)
  ├─> ProductIdService (ASIN/UPC management)
  ├─> FileUploadService (PDF uploads)
  └─> ApplicationDbContext (EF Core)

AdminController
  ├─> UserService (user management)
  ├─> RoleService (role management)
  └─> ApplicationDbContext

AccountController
  ├─> SignInManager (authentication)
  ├─> UserManager (identity)
  └─> JwtTokenService (API auth)

DashboardController
  ├─> CertificateService (statistics)
  └─> ApplicationDbContext (reporting)
```

**Key Service Interfaces:**
- `ICertificateService` - Certificate CRUD operations
- `ICertificateWorkflowService` - State machine (14 states)
- `IProductIdService` - Product ID management (ASINs, UPCs)
- `IFileUploadService` - File handling and validation
- `IEmailService` - Email notifications (optional)

### API Endpoints Traceability

| Endpoint | Controller | Action | Auth Required | Role | Purpose |
|----------|------------|--------|---------------|------|---------|
| GET /api/v1/certificates | CertificatesApiController | GetAll | No | Public | List all certificates |
| GET /api/v1/certificates/{id} | CertificatesApiController | GetById | No | Public | Get certificate details |
| POST /api/v1/certificates | CertificatesApiController | Create | Yes | Admin, Analyst | Create certificate |
| PUT /api/v1/certificates/{id} | CertificatesApiController | Update | Yes | Admin, Analyst | Update certificate |
| DELETE /api/v1/certificates/{id} | CertificatesApiController | Delete | Yes | Admin | Delete certificate |
| GET /api/CertificateTileExternal/tiles | CertificateTileExternalController | GetTiles | API Key | Partner | Amazon/Wayfair filtered view |
| GET /swagger | Swagger UI | - | No | Public | API documentation |

### Configuration Traceability

**appsettings.json sections:**
- `ConnectionStrings` - Database configuration (ANUKI01 dev, AccuWebHosting prod)
- `Authentication` - Test mode, 2FA settings
- `Jwt` - Token generation settings (64+ char key required)
- `Features` - Feature flags (2FA, email confirmation, guest access)
- `IpRateLimiting` - API rate limiting (1000/hour default)
- `AzureAdB2C` - Azure AD configuration (optional)
- `Logging` - Log levels by namespace
- `Kestrel` - Server configuration (port 5101 dev)

**Environment-specific settings:**
- `appsettings.Development.json` - ANUKI01 SQL Server, test mode enabled, debug logging
- `appsettings.Production.json` - AccuWebHosting SQL Server, test mode disabled, production logging
- `appsettings.Staging.json` - Staging environment (if exists)

### Known Issues & Workarounds

**Issue 1: SQL Server LocalDB Logon Trigger (RESOLVED)**
- **Error**: Error 17892 - Logon trigger blocking access
- **Resolution**: Switched to ANUKI01 local SQL Server instance
- **Status**: No longer applicable

**Issue 2: Playwright Browsers Not Installed**
- **Impact**: 86 E2E/mobile tests fail
- **Fix**: `cd GCCWebApp-Tests/GCCWebApp.Tests/bin/Debug/net8.0 && pwsh playwright.ps1 install`
- **Status**: Documented, awaiting execution

**Issue 3: API Key Handler Mock Setup**
- **Impact**: 12 authentication tests fail
- **Fix**: Replace extension method mocks in ApiKeyAuthenticationHandlerTests.cs
- **Status**: Documented, awaiting implementation

**Issue 4: Nullable Reference Warnings**
- **Impact**: 58+ build warnings (CS8618)
- **Cause**: Properties not initialized in model constructors
- **Workaround**: Warnings can be safely ignored; models are initialized by EF Core
- **Status**: Low priority, cosmetic issue

## Troubleshooting Guide

### Database Connection Issues

**Problem:** "Cannot connect to ANUKI01"
```bash
# Verify SQL Server is running
Get-Service MSSQLSERVER

# Test connection
sqlcmd -S ANUKI01 -E -Q "SELECT @@VERSION"

# Check firewall (if remote)
Test-NetConnection -ComputerName ANUKI01 -Port 1433
```

**Problem:** "Login failed for user 'GCC_DevUser'"
```bash
# Verify credentials in appsettings.Development.json
# Reset password in SQL Server Management Studio
# Ensure user has db_owner role on GCCDatabase_Dev
```

**Problem:** "Database 'GCCDatabase_Dev' does not exist"
```bash
# Create database manually
sqlcmd -S ANUKI01 -U GCC_DevUser -P DevP@ss2024!SQL -Q "CREATE DATABASE GCCDatabase_Dev"

# Then run migrations
dotnet ef database update
```

### Build Issues

**Problem:** "Could not copy GCCWebApp.exe - file is locked"
```bash
# Kill locking processes
taskkill /F /IM dotnet.exe
taskkill /F /IM GCCWebApp.exe

# Clean build artifacts
dotnet clean
```

**Problem:** "Package restore failed"
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with diagnostic logging
dotnet restore --verbosity detailed
```

### Runtime Issues

**Problem:** "Data not loading in Certificate Tile View"
```bash
# Check database connection in logs
# Verify migrations applied: dotnet ef migrations list
# Verify data exists: sqlcmd -S ANUKI01 -d GCCDatabase_Dev -Q "SELECT COUNT(*) FROM Certificates"
# Check browser console for JavaScript errors (F12)
```

**Problem:** "401 Unauthorized accessing Admin pages"
```bash
# Verify user is logged in: check cookie "GCCAuth"
# Verify user has Admin role in database:
# sqlcmd -Q "SELECT u.Email, r.Name FROM Users u JOIN UserRoles ur ON u.Id = ur.UserId JOIN Roles r ON ur.RoleId = r.Id"
# Clear browser cookies and re-login
```

**Problem:** "JWT token expired or invalid"
```bash
# Verify JWT configuration in appsettings.json
# Key must be 64+ characters
# Verify token in browser: jwt.io
# Check clock skew (system time sync)
```

### Test Issues

**Problem:** "Tests failing with database errors"
```bash
# Tests use in-memory database by default
# Check test output for specific errors
# Ensure test project references EF Core InMemory provider
```

**Problem:** "Playwright tests failing - browser not found"
```bash
cd GCCWebApp-Tests/GCCWebApp.Tests/bin/Debug/net8.0
pwsh playwright.ps1 install
```

### Performance Issues

**Problem:** "Slow page load times"
```bash
# Enable detailed logging to identify bottlenecks
# Check database query performance (SQL Profiler)
# Verify indexes exist on frequently queried columns
# Check network latency to database server
```

**Problem:** "High memory usage"
```bash
# Check for EF Core tracking issues (use AsNoTracking() for read-only queries)
# Verify IDisposable resources are properly disposed
# Check for circular references in models
# Profile with dotMemory or PerfView
```

## Production Status

**✅ ALL PHASES COMPLETE** - Ready for production deployment
*Last Updated: October 15, 2025*

### Current Testing Status
- **Total Tests**: 624+ across 7 test projects
- **Pass Rate**: 87% (expected 95% after Playwright install)
- **Database**: ANUKI01 configured, migrations pending
- **Documentation**: Comprehensive setup, requirements, and traceability complete

## Special Notes

- **Tech**: ASP.NET Core MVC + SQL Server (not Django/PostgreSQL)
- **Design**: Follow Master Design Guide, 16px min font, no nav search bar
- **Testing**: Update tests for new features, verify JS rendering
- **Constraints**: Admin user/role cannot be deleted, Phase 1 has no partner branding
- do not include icons in the text
- External Users do not have the same permissions as Analyst and Admin. Admin has CRUD Roles. Analyst has read write and does not have access to the Admin pages.
- stop making random changes that aren't relevant to the defect
- ask followup questions for clarity and focus on the issue or defect
- review requirments and current design and determine the exact change. Include what the changes will be prior to making them to make sure we have an additional quality check
# GCC WebApp Deployment Log

Track all deployments to production/test servers including code changes and database migrations.

---

## Deployment 2026-03-25 (v2.0.1) — QC Validation Fixes

**Package**: `GCCWebApp-Deployment-20260325.zip` (~115 MB)
**Target**: AccuWebHosting / Plesk (https://gccwebapp.com)

### Database Changes Required: NO

No new SQL scripts. This is a code-only deployment (CSS, JS, and Controller fixes found during QC validation testing).

### Code Changes

| Issue | Area | Fix |
|-------|------|-----|
| NEW-001 (Critical) | Public view - direct URL | Anonymous users could access non-public certs via direct URL (`/Certificate/CertificateSingleView/26-0003`). Added status + expiration guard. Returns 404 for non-public certs. File: `CertificateController.cs` |
| NEW-002 | Public view - UI | Blue focus border on checkboxes/logo on mouse click. Changed `:focus` to `:focus-visible` (keyboard only). Files: `main.css`, `certificatetileview.css` |
| NEW-003 (High) | Public view - mobile/tablet | "Show Filters & Search" not displaying. Root cause: `main.css` used `.sidebar.expanded` but JS toggles `.sidebar.mobile-open`. Fixed class mismatch + removed `max-height: 60px` constraint. File: `main.css` |
| NEW-004 | Public view - mobile/tablet | Empty nav bar showing for anonymous users on mobile. Added `nav-anon` class, hidden at <=1024px. Files: `_Layout.cshtml`, `main.css` |
| QC #50 | Create cert - mobile | Form layout distorted on mobile. Base grid styles used `!important` blocking responsive overrides. Added proper responsive breakpoints for 1024px, 768px, 480px. File: `certificate-create.css` |

### Deployment Steps

1. **Stop application** on Plesk
2. **Backup current files** on server
3. **Extract zip** to application root (overwrite all files)
4. **Verify `appsettings.Production.json`** — ensure connection string and settings preserved
5. **Start application** on Plesk
6. **Verify** — test direct URL access to Draft cert while not logged in (should 404), test mobile filter toggle, test Create cert page on mobile

---

## Deployment 2026-03-24 (v2.0.0) — QC Sprint 1

**Package**: `GCCWebApp-Deployment-20260324.zip` (~64 MB)
**Target**: AccuWebHosting / Plesk (https://gccwebapp.com)

### Database Changes Required: YES

**Run these scripts IN ORDER before deploying code:**

| # | Script | Description | QC |
|---|--------|-------------|----|
| 15 | `15_20260223_CleanupDuplicateCustomFields.sql` | Remove custom fields that duplicate hard-coded model fields | — |
| 16 | `16_20260225_SeedProductCategories.sql` | Seed product category hierarchy | — |
| 17 | `17_20260225_SeedCSFSubcategories.sql` | Seed CSF subcategories | — |
| 18 | `18_20260319_UpdateStatusTypes.sql` | **CRITICAL** — Migrate 14-state → 8 client-defined statuses | QC log |
| 19 | `19_20260320_UpdateCertificationTypes.sql` | Add/rename/deactivate certification types + subcategories | QC log |
| 20 | `20_20260324_UpdatePartnerDescriptions.sql` | Update Novi description, add Wayfair partner | #110 |
| 21 | `21_20260324_UpdatePermissionDescriptions.sql` | Fix permission descriptions (Manage Workflow, Audit Logs, Tags) | #105 |

**EF Core Migrations (schema changes — run via `dotnet ef` or apply SQL manually):**

| Migration | Description |
|-----------|-------------|
| `20260130180056_ProductTypesPreventDuplicatesUnderSameParent` | Unique constraint on product types |
| `20260213000922_AddPathwayTable` | New Pathways table |
| `20260213005208_FixPathwayUniqueIndexForSoftDelete` | Fix pathway unique index |
| `20260227190743_AddProductCategoryTypeToCertificate` | Add ProductCategoryId/TypeId/SubcategoryId to Certificates |

**To apply EF migrations against production:**
```bash
dotnet ef database update --connection "Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

**To generate SQL script instead (safer):**
```bash
dotnet ef migrations script 20251208051905_AddImportBatchAndAuditImportBatchId --idempotent -o ef_migrations.sql
```

### Code Changes (QC Defect Fixes)

| QC # | Area | Fix |
|------|------|-----|
| #20 | Certificate views | Company Email/URL show dash instead of placeholder text |
| #78 | Product ID Manager | Template CSV already headers-only (confirmed) |
| #79 | Product ID Manager | Add/Save form data collection fixed (was using button as form target) |
| #80 | Product ID Manager | Edit on Create page fixed (temp items loaded from memory, not API) |
| #81 | Product ID Manager | CSV import handles Excel scientific notation (1.23E+12 → full number) |
| #96 | Navigation | Nav z-index fix — dropdowns always render above page content |
| #99 | Product ID Manager | Import: duplicate check scoped to certificate + detailed error messages |
| #104 | Breadcrumbs | Broken links fixed across 19 views |
| #105 | Admin - Roles | Permission descriptions updated (+ SQL script #21) |
| #106 | Admin - Users | Company field shown for all roles except Partner |
| #107 | Admin - Users | CreateUser error handling — actual error messages returned |
| #108 | Admin - Users | Template export — removed fake sample data row |
| #109 | Admin - Users | Import fixed (same root cause as #107 + #108) |
| #110 | Admin - Partners | Novi description updated, Wayfair added (+ SQL script #20) |
| #115 | Admin - Product Types | Blue box description text fixed |
| #116 | Admin - Product Types | Help modal fake examples removed |
| #117 | Admin - Product Types | New items no longer default to first category |
| — | Auth | Remember Me cookie: 14-day expiration when checked |
| — | Statuses | 14-state → 8 client-defined statuses (+ SQL script #18) |
| — | Cert Types | Certification type hierarchy updates (+ SQL script #19) |
| — | Products | Company → Product cascading dropdowns on Create/Edit |
| — | Tile view | Certificate tag bottom spacing |

### Deployment Steps

1. **Backup database** (full backup before any changes)
2. **Run EF migrations** (4 schema changes since v1.4.0)
3. **Run data scripts #15–#21** in order (see table above)
4. **Verify database** — spot-check certificate statuses, cert types, partners, permissions
5. **Stop application** on Plesk
6. **Backup current files** on server
7. **Upload and extract ZIP** to web root
8. **Verify `appsettings.Production.json`** connection string unchanged
9. **Verify `logs/` folder** has write permissions
10. **Start application**

### Post-Deployment Verification

- [ ] Login works (admin, analyst, external)
- [ ] Remember Me persists across browser restart
- [ ] Nav dropdowns appear above search bars on all pages
- [ ] Certificate statuses show new 8-state values
- [ ] Create cert → Product ID Manager: Add, Edit, Delete work
- [ ] Create cert → Product ID Manager: Import 2+ row CSV works
- [ ] Create cert → Company → Product cascading dropdowns work
- [ ] Admin → Users: Create user shows Company field
- [ ] Admin → Users: Import works with error feedback
- [ ] Admin → Product Types: new items get correct category
- [ ] Admin → Roles: permission descriptions updated
- [ ] Admin → Partners: Wayfair visible, Novi description updated
- [ ] Breadcrumb links navigate correctly
- [ ] Public tile view shows only "Client Approved - Paid" and "Republished"

---

## Deployment 2025-12-13 (v1.4.0)

**Package**: `GCCWebApp-Production-Deploy.zip` (61 MB)
**Target**: AccuWebHosting / Plesk (https://gccwebapp.com)

### Database Changes Required: NO (Already Applied)

**Verified 2025-12-13**: Production database already has all 14 migrations applied, including the latest `20251208051905_AddImportBatchAndAuditImportBatchId`.

**Migration Already Applied (for reference):**
```sql
-- Add ImportBatchId column to AuditHistory
ALTER TABLE AuditHistory ADD ImportBatchId uniqueidentifier NULL;

-- Create ImportBatches table
CREATE TABLE ImportBatches (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    EntityType nvarchar(100) NOT NULL,
    FileName nvarchar(255) NULL,
    TotalRows int NOT NULL,
    NewCount int NOT NULL,
    ModifiedCount int NOT NULL,
    SkippedCount int NOT NULL,
    UnchangedCount int NOT NULL,
    ErrorCount int NOT NULL,
    Status nvarchar(20) NOT NULL,
    ImportedBy int NOT NULL,
    ImportedByUsername nvarchar(255) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    CompletedAt datetime2 NULL,
    IpAddress nvarchar(50) NULL,
    ErrorMessage nvarchar(max) NULL,
    CONSTRAINT FK_ImportBatches_Users_ImportedBy FOREIGN KEY (ImportedBy) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_ImportBatches_ImportedBy ON ImportBatches(ImportedBy);

-- Update EF migration history
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251208051905_AddImportBatchAndAuditImportBatchId', '9.0.0');
```

### Code Changes

1. **PDF Upload Fix** - Certificate Create/Edit drag-and-drop now works
   - File: `wwwroot/js/certificate-create.js`
   - Issue: Drag-drop showed preview but didn't attach file to form
   - Fix: Use DataTransfer API to assign dropped files to input element

2. **Bootstrap 5 Integration** - Modals now display correctly
   - File: `Views/Shared/_Layout.cshtml`
   - Added Bootstrap 5 CSS and JS CDN links

3. **Custom Fields JavaScript Fixes**
   - File: `wwwroot/js/admin/certificate-fields.js`
   - Fixed: Section field not being saved
   - Fixed: Notes field mapped to wrong property (defaultValue)

4. **Product ID Manager Modal Fixes**
   - File: `Views/Shared/_ProductIdManager.cshtml`
   - File: `wwwroot/css/product-id-manager.css`
   - Fixed: Modals displaying inline instead of as overlays

### Deployment Steps

1. **Backup current production files**
2. **Run SQL migrations** (see above)
3. **Upload and extract ZIP** to web root
4. **Verify logs folder** has write permissions
5. **Restart application pool**
6. **Test**:
   - [ ] Login works
   - [ ] Certificate Create with PDF drag-drop
   - [ ] Certificate Edit with PDF drag-drop
   - [ ] Product ID Manager modals open correctly
   - [ ] Custom fields display on certificate forms

---

## Deployment 2025-12-11 (v1.3.0)

**Package**: Previous deployment
**Database Changes**: None (code-only)

### Code Changes
- Import quality improvements (fuzzy matching, duplicate detection)
- Certificate Create/Edit usability enhancements
- UAT ready milestone

---

## Deployment 2025-11-26 (v1.2.0)

**Database Migration**: `20251126212053_AddCertificationResultsToCertificate`

**SQL:**
```sql
ALTER TABLE Certificates ADD CertificationResults nvarchar(1000) NULL;

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251126212053_AddCertificationResultsToCertificate', '9.0.0');
```

### Code Changes
- Added CertificationResults field to Certificate model
- Amazon threshold tracking support

---

## Deployment 2025-11-23 (v1.1.0)

**Database Migration**: `20251123161550_AddAuditHistoryTable`

**SQL:**
```sql
CREATE TABLE AuditHistory (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    EntityType nvarchar(100) NOT NULL,
    EntityId nvarchar(100) NOT NULL,
    Action nvarchar(50) NOT NULL,
    OldValues nvarchar(max) NULL,
    NewValues nvarchar(max) NULL,
    UserId int NULL,
    Username nvarchar(255) NULL,
    IpAddress nvarchar(50) NULL,
    Timestamp datetime2 NOT NULL,
    AdditionalInfo nvarchar(max) NULL
);

CREATE INDEX IX_AuditHistory_EntityType_EntityId ON AuditHistory(EntityType, EntityId);
CREATE INDEX IX_AuditHistory_Timestamp ON AuditHistory(Timestamp);
CREATE INDEX IX_AuditHistory_UserId ON AuditHistory(UserId);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251123161550_AddAuditHistoryTable', '9.0.0');
```

---

## Migration Reference

### EF Core Migrations (Schema)

| Migration ID | Date | Description |
|--------------|------|-------------|
| 20251015162223_InitialCreate_SqlServer_Final | 2025-10-15 | Initial schema |
| 20251029003704_AddLevelToCertificationType | 2025-10-29 | Cert type levels |
| 20251029005907_DistributeCertificatesAcrossTypes | 2025-10-29 | Data migration |
| 20251029011232_AddParentIdToCertificationType | 2025-10-29 | Hierarchical types |
| 20251029163311_FixWasteSystemAuditLevel | 2025-10-29 | Level fix |
| 20251111164931_AddAmazonApiIntegration | 2025-11-11 | Amazon API tables |
| 20251112144629_AddPartnerApiKeys | 2025-11-12 | Partner API keys |
| 20251113161816_AddGoogleAnalyticsSettings | 2025-11-13 | GA settings |
| 20251113171055_AddSectionToCustomFields | 2025-11-13 | Custom field sections |
| 20251117211135_AddCertificateStatusHistory | 2025-11-17 | Status history |
| 20251118214329_RenameRoleUserToUserRoles | 2025-11-18 | Table rename |
| 20251123161550_AddAuditHistoryTable | 2025-11-23 | Audit logging |
| 20251126212053_AddCertificationResultsToCertificate | 2025-11-26 | Cert results |
| 20251208051905_AddImportBatchAndAuditImportBatchId | 2025-12-08 | Import batches |
| 20260130180056_ProductTypesPreventDuplicatesUnderSameParent | 2026-01-30 | Unique constraint |
| 20260213000922_AddPathwayTable | 2026-02-13 | Pathways table |
| 20260213005208_FixPathwayUniqueIndexForSoftDelete | 2026-02-13 | Pathway index fix |
| 20260227190743_AddProductCategoryTypeToCertificate | 2026-02-27 | Product category on certs |

### Data Migration Scripts (Run manually)

| # | Script | Date | Description | Deployment |
|---|--------|------|-------------|------------|
| 15 | CleanupDuplicateCustomFields | 2026-02-23 | Remove duplicate custom fields | v2.0.0 |
| 16 | SeedProductCategories | 2026-02-25 | Product category hierarchy | v2.0.0 |
| 17 | SeedCSFSubcategories | 2026-02-25 | CSF subcategory seed | v2.0.0 |
| 18 | UpdateStatusTypes | 2026-03-19 | 14-state → 8-state statuses | v2.0.0 |
| 19 | UpdateCertificationTypes | 2026-03-20 | Cert type hierarchy updates | v2.0.0 |
| 20 | UpdatePartnerDescriptions | 2026-03-24 | Novi desc + Wayfair partner | v2.0.0 |
| 21 | UpdatePermissionDescriptions | 2026-03-24 | QC #105 permission descs | v2.0.0 |

---

## Notes

- Always backup database before running migrations
- Test on staging/dev first if possible
- web.config is Plesk-compatible (no fileExtensions/hiddenSegments)
- Logs folder must exist with write permissions

# GCC WebApp Deployment Log

Track all deployments to production/test servers including code changes and database migrations.

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

---

## Notes

- Always backup database before running migrations
- Test on staging/dev first if possible
- web.config is Plesk-compatible (no fileExtensions/hiddenSegments)
- Logs folder must exist with write permissions

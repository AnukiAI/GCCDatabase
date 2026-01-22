# GCC Database Migration Scripts

This folder contains idempotent SQL scripts for all Entity Framework Core migrations. These scripts can be safely run multiple times - they check if each migration has already been applied before executing.

## Quick Start

### Fresh Database (New Installation)
Run the complete script that includes all migrations:
```sql
-- Run this on a new/empty database
sqlcmd -S [SERVER] -d [DATABASE] -U [USER] -P [PASSWORD] -i 00_COMPLETE_ALL_MIGRATIONS.sql
```

### Incremental Updates (Existing Database)
Run only the specific migration scripts needed. Check `__EFMigrationsHistory` table to see which migrations have been applied:
```sql
SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;
```

## Migration Scripts

| # | File | Date | Description |
|---|------|------|-------------|
| 00 | `00_COMPLETE_ALL_MIGRATIONS.sql` | - | **Complete schema** - All migrations in one file |
| 01 | `01_20251015_InitialCreate_SqlServer_Final.sql` | 2025-10-15 | Initial database schema with all tables |
| 02 | `02_20251029_AddLevelToCertificationType.sql` | 2025-10-29 | Add Level column to CertificationType |
| 03 | `03_20251029_DistributeCertificatesAcrossTypes.sql` | 2025-10-29 | Data migration for certificate types |
| 04 | `04_20251029_AddParentIdToCertificationType.sql` | 2025-10-29 | Add ParentId for hierarchical cert types |
| 05 | `05_20251029_FixWasteSystemAuditLevel.sql` | 2025-10-29 | Fix Waste System Audit level assignment |
| 06 | `06_20251111_AddAmazonApiIntegration.sql` | 2025-11-11 | Amazon API integration settings table |
| 07 | `07_20251112_AddPartnerApiKeys.sql` | 2025-11-12 | Partner API keys table for multi-partner support |
| 08 | `08_20251113_AddGoogleAnalyticsSettings.sql` | 2025-11-13 | Google Analytics configuration table |
| 09 | `09_20251113_AddSectionToCustomFields.sql` | 2025-11-13 | Add Section column to CustomFields |
| 10 | `10_20251117_AddCertificateStatusHistory.sql` | 2025-11-17 | Certificate status history audit trail |
| 11 | `11_20251118_RenameRoleUserToUserRoles.sql` | 2025-11-18 | Rename RoleUser junction table to UserRoles |
| 12 | `12_20251123_AddAuditHistoryTable.sql` | 2025-11-23 | General audit history table |
| 13 | `13_20251126_AddCertificationResultsToCertificate.sql` | 2025-11-26 | Add CertificationResults field for partner requirements |

## Connection Strings

### Development (ANUKI01)
```
Server=ANUKI01;Database=GCCDatabase_Dev;Integrated Security=true;TrustServerCertificate=true
```

### Production (AccuWebHosting)
```
Server=68.71.129.120,1533;Database=gccwebap_certificates;User Id=GCC_adminxyz123;Password=Q3Ez4e!gE_47KNi;TrustServerCertificate=true
```

## Usage Examples

### Apply specific migration to Production
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i 13_20251126_AddCertificationResultsToCertificate.sql
```

### Apply all migrations to Dev (Windows Auth)
```bash
sqlcmd -S ANUKI01 -E -d GCCDatabase_Dev -i 00_COMPLETE_ALL_MIGRATIONS.sql
```

### Check migration status
```sql
SELECT
    MigrationId,
    ProductVersion,
    'Applied' as Status
FROM __EFMigrationsHistory
ORDER BY MigrationId;
```

## Important Notes

1. **Idempotent Scripts**: All scripts check `__EFMigrationsHistory` before applying changes. Safe to run multiple times.

2. **Order Matters**: If running individual scripts, apply them in numerical order (01, 02, 03...).

3. **Backup First**: Always backup the database before applying migrations to production.

4. **Schema Ownership**: Ensure database user has `dbo` as default schema. See `docs/database/reference/CRITICAL_SCHEMA_REQUIREMENT.md`.

5. **Transaction Wrapped**: Each script runs within a transaction and will rollback on error.

## Generating New Migration Scripts

When adding new migrations via EF Core:
```bash
# Create migration
dotnet ef migrations add MigrationName

# Generate SQL script
dotnet ef migrations script PreviousMigration NewMigration --idempotent -o Database_Scripts/Migrations/XX_YYYYMMDD_MigrationName.sql
```

## Related Documentation

- `/docs/database/README.md` - Complete database documentation
- `/docs/database/deployment/SYNC_DEV_TO_PROD.md` - Production sync guide
- `/Database_Scripts/PRODUCTION_REBUILD_GUIDE.md` - Full rebuild instructions

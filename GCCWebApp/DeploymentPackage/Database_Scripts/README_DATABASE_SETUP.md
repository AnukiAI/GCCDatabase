# Database Setup Instructions

This guide explains how to set up your production/test database with the complete schema and test data.

## Overview

You need to run **THREE SQL scripts in order**:

1. **00_CREATE_SCHEMA.sql** - Creates all database tables and indexes
2. **FULL_TEST_DATA_SEED.sql** - Populates the database with test data
3. **DASHBOARD_DATA_SEED.sql** - Configures certificate dates and dashboard statistics

## Prerequisites

- SQL Server 2019+ installed and running
- Access to your production database:
  - **Server**: 68.71.129.120,1533
  - **Database**: gccwebap_certificates
  - **Username**: GCC_adminxyz123
  - **Password**: [from secure vault]
- SQL Server Management Studio (SSMS) or sqlcmd tool

## Step 1: Create Database Schema

This script creates all 28 tables and 100+ indexes required by the GCC application.

### Option A: Using SQL Server Management Studio (SSMS)

1. Open SQL Server Management Studio
2. Connect to your server:
   - Server: `68.71.129.120,1533`
   - Authentication: SQL Server Authentication
   - Login: `GCC_adminxyz123`
   - Password: [your password]
3. Select database `gccwebap_certificates`
4. Open the file: `Database_Scripts/00_CREATE_SCHEMA.sql`
5. Click **Execute** (or press F5)
6. Verify output shows "Schema creation complete!"

### Option B: Using sqlcmd

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [YOUR_PASSWORD] -d gccwebap_certificates -i "Database_Scripts\00_CREATE_SCHEMA.sql"
```

### Expected Output

```
============================================
GCC Database Schema Creation
Started at: 2025-10-16 14:15:00
============================================

1. Creating independent tables...
  - ApiKeys table created
  - CertificateAggregates table created
  - CertificationTypes table created
  ...

Tables created: 28
Indexes created: 100+ performance indexes

NEXT STEP: Run FULL_TEST_DATA_SEED.sql to populate data
============================================
```

### Tables Created

**Independent Tables (13):**
- ApiKeys
- CertificateAggregates
- CertificationTypes
- Companies
- ETLAlerts, ETLConfigurations, ETLDataQualityIssues, ETLLogEntries
- Partners
- Permissions
- ProductCategories
- Roles
- Tags
- Users

**Dependent Tables (15):**
- CustomFields, CustomFieldValues
- ProductTypes, ProductSubcategories, Products, ProductIdentifiers
- Certificates, CertificateDocuments, CertificateProductIds
- Join tables: PermissionRole, CompanyUser, PartnerUser, RoleUser, CertificateTag, ProductTag

## Step 2: Seed Test Data

After the schema is created, populate it with test data including 175+ certificates, 28 companies, 43 products, and 15 test users.

### Option A: Using SQL Server Management Studio (SSMS)

1. In SSMS, still connected to `gccwebap_certificates`
2. Open the file: `Database_Scripts/FULL_TEST_DATA_SEED.sql`
3. Click **Execute** (or press F5)
4. Verify output shows "Data seeding complete!"

### Option B: Using sqlcmd

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P [YOUR_PASSWORD] -d gccwebap_certificates -i "Database_Scripts\FULL_TEST_DATA_SEED.sql"
```

### Expected Output

```
============================================
GCC Complete Test Data Seeding
Started at: 2025-10-16 14:20:00
============================================

1. Seeding Product Categories...
   Inserted: 6 categories
2. Seeding Certification Types...
   Inserted: 5 certification types
3. Seeding Partners...
   Inserted: 3 partners
...
15. Seeding API Keys...
   Inserted: 3 API keys

============================================
DATA SEEDING COMPLETE
============================================
Total records inserted:
  - Product Categories: 6
  - Certification Types: 5
  - Partners: 3
  - Companies: 28
  - Product Types: 19
  - Products: 43
  - Certificates: 175
  - Tags: 10
  - Roles: 4
  - Permissions: 6
  - Users: 15
  - User-Role Relationships: 15
  - API Keys: 3
============================================
```

## Step 3: Verify Database Setup

Run this validation query to confirm everything is set up correctly:

```sql
-- Verify table counts
SELECT
    'ProductCategories' AS TableName, COUNT(*) AS RecordCount FROM ProductCategories
UNION ALL
SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes
UNION ALL
SELECT 'Partners', COUNT(*) FROM Partners
UNION ALL
SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL
SELECT 'ProductTypes', COUNT(*) FROM ProductTypes
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Certificates', COUNT(*) FROM Certificates
UNION ALL
SELECT 'Tags', COUNT(*) FROM Tags
UNION ALL
SELECT 'Roles', COUNT(*) FROM Roles
UNION ALL
SELECT 'Permissions', COUNT(*) FROM Permissions
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'ApiKeys', COUNT(*) FROM ApiKeys
ORDER BY TableName;
```

### Expected Results

| TableName | RecordCount |
|-----------|-------------|
| ApiKeys | 3 |
| Certificates | 175 |
| CertificationTypes | 5 |
| Companies | 28 |
| Partners | 3 |
| Permissions | 6 |
| ProductCategories | 6 |
| ProductTypes | 19 |
| Products | 43 |
| Roles | 4 |
| Tags | 10 |
| Users | 15 |

## Test User Credentials

After seeding, you can log into the application with these test accounts:

### Admin User
- **Email**: admin@greencirclecertified.com
- **Password**: TestPass123!
- **Permissions**: Full CRUD access to everything including user/role management

### Analyst User
- **Email**: analyst@greencirclecertified.com
- **Password**: TestPass123!
- **Permissions**: Read/Write certificates, no admin access

### External/Partner User
- **Email**: partner@amazon.com
- **Password**: TestPass123!
- **Permissions**: Read-only access to partner-filtered certificates

### Additional Test Users (12 more)
All use password: **TestPass123!**
- john.analyst@greencircle.com (Analyst)
- sarah.admin@greencircle.com (Admin)
- mike.extern@wayfair.com (External)
- emily.analyst@greencircle.com (Analyst)
- etc.

## Troubleshooting

### Error: "Invalid object name 'ProductCategories'"
**Cause**: Schema script (Step 1) was not run successfully.
**Solution**: Run `00_CREATE_SCHEMA.sql` first before `FULL_TEST_DATA_SEED.sql`

### Error: "Violation of UNIQUE KEY constraint"
**Cause**: Data already exists in the database from a previous seed.
**Solution**: Either:
- Clear existing data: `EXEC sp_MSforeachtable 'DELETE FROM ?'`
- Or drop and recreate the database

### Error: "Cannot insert duplicate key"
**Cause**: Seed script was run multiple times.
**Solution**: The seed script is idempotent and checks for existing data. If this error occurs, manually delete conflicting records or recreate the database.

### Connection Timeout
**Cause**: Network issues or server unavailable.
**Solution**:
- Verify server is accessible: `ping 68.71.129.120`
- Check firewall allows port 1533
- Verify credentials are correct

## Script Details

### 00_CREATE_SCHEMA.sql
- **Purpose**: Creates all database tables, foreign keys, and indexes
- **Source**: Generated from Entity Framework migration `20251015162223_InitialCreate_SqlServer_Final`
- **Idempotent**: Yes - checks if tables exist before creating
- **Execution Time**: ~10-30 seconds
- **Size**: ~1200 lines of SQL

### FULL_TEST_DATA_SEED.sql
- **Purpose**: Populates database with comprehensive test data
- **Data Includes**:
  - 175 certificates (25 real + 150 generated)
  - 28 companies (real company data)
  - 43 products across 6 categories
  - 15 test users across 4 roles
  - Reference data (certification types, partners, tags)
- **Idempotent**: Partial - uses `IF NOT EXISTS` checks
- **Execution Time**: ~20-60 seconds
- **Size**: ~1400 lines of SQL

## Alternative: Use Entity Framework Migrations

If you prefer to use Entity Framework migrations instead of SQL scripts:

```bash
# Set environment to Production
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Update connection string in appsettings.Production.json

# Run migrations
dotnet ef database update --project GCCWebApp

# Run application (automatic seeding via SeedData.cs)
dotnet run --urls "http://localhost:5101"
```

**Note**: The C# seeding in `SeedData.cs` contains the same data as `FULL_TEST_DATA_SEED.sql`.

## On-Demand Migration Management (Recommended)

**IMPORTANT**: Automatic migrations are now **disabled** for Production/Staging environments. The application will NOT automatically create or update database tables when it starts.

To apply schema changes or refresh the database, use the **on-demand migration scripts**:

### Quick Start

```bash
# Apply migrations to Production database
cd Database_Scripts
.\apply-migrations.cmd Production

# Or use PowerShell directly
.\apply-migrations.ps1 -Environment Production
```

### Available Commands

**Windows Batch File (Recommended for simplicity)**:
```bash
.\apply-migrations.cmd Production   # Apply to production database
.\apply-migrations.cmd Staging      # Apply to staging database
.\apply-migrations.cmd Development  # Apply to development database
```

**PowerShell (Advanced options)**:
```powershell
# Standard migration
.\apply-migrations.ps1 -Environment Production

# Preview migrations without applying (generates SQL script)
.\apply-migrations.ps1 -Environment Production -WhatIf

# Force migration without confirmation prompt
.\apply-migrations.ps1 -Environment Production -Force
```

### Safety Features

1. **Production Confirmation**: Requires typing "YES" before applying to production
2. **Preview Mode**: Use `-WhatIf` to generate SQL script without applying changes
3. **Verbose Output**: Shows all pending migrations before applying
4. **Error Handling**: Detailed error messages if migration fails
5. **Schema Validation**: Automatic cleanup of wrong-schema tables

### When to Run Migrations

Run the migration script when:
- You need to update the database schema after code changes
- New Entity Framework migrations have been created
- You're setting up a new environment
- Tables need to be recreated or refreshed

**Do NOT run migrations**:
- During application startup (automatic migrations are disabled)
- Without reviewing pending migrations first
- Without backing up production data first

### Workflow Example

```bash
# 1. Create a new migration (development)
cd GCCWebApp
dotnet ef migrations add AddNewFeature

# 2. Review the generated migration code
# Check: Migrations/[timestamp]_AddNewFeature.cs

# 3. Test migration locally
cd Database_Scripts
.\apply-migrations.ps1 -Environment Development

# 4. Preview production migration (generates SQL)
.\apply-migrations.ps1 -Environment Production -WhatIf

# 5. Review generated SQL script
# Check: Database_Scripts/migration_preview_*.sql

# 6. Apply to production
.\apply-migrations.ps1 -Environment Production
# Type "YES" when prompted

# 7. Verify application connectivity
cd ..
dotnet run --urls "http://localhost:5101"
```

### Troubleshooting Migration Script

**Error: "Entity Framework tools not found"**
```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Or update if already installed
dotnet tool update --global dotnet-ef
```

**Error: "Cannot connect to database"**
- Verify connection string in `appsettings.[Environment].json`
- Check SQL Server is running and accessible
- Verify firewall allows port 1533 for remote connections
- Test connection manually: `sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123`

**Error: "Migration already applied"**
- This is safe - EF Core tracks applied migrations
- Script will skip already-applied migrations
- Use `-WhatIf` to see what would be applied

### Environment Configuration

The migration script reads connection strings from:
- `appsettings.Production.json` - Production database (68.71.129.120:1533)
- `appsettings.Staging.json` - Staging database (if configured)
- `appsettings.Development.json` - Local development database (ANUKI01)

Ensure these files have correct connection strings before running migrations.

## Next Steps

After successful database setup:

1. **Test the Application**
   ```bash
   cd GCCWebApp
   dotnet run --urls "http://localhost:5101"
   ```

2. **Verify Certificate Tile View**
   - Navigate to: http://localhost:5101/Certificate/CertificateTileView
   - Should see 175 certificates displayed

3. **Test Authentication**
   - Navigate to: http://localhost:5101/Account/Login
   - Log in with admin@greencirclecertified.com / TestPass123!
   - Verify admin dashboard access

4. **Run Validation Script**
   - Execute: `Database_Scripts/PRODUCTION_VALIDATION.sql`
   - Verifies data integrity and relationships

## Support

For issues or questions:
- Check troubleshooting section above
- Review migration file: `Migrations/20251015162223_InitialCreate_SqlServer_Final.cs`
- Review seeding code: `Data/SeedData.cs`
- Consult documentation: `0_Documentation/docs/`

## File Locations

```
GCCWebApp/
├── Database_Scripts/
│   ├── README_DATABASE_SETUP.md (this file)
│   ├── DATABASE_SCHEMA_REFERENCE.md (⭐ SCHEMA REFERENCE - check this first!)
│   ├── 00_CREATE_SCHEMA.sql (Step 1 - Schema creation)
│   ├── FULL_TEST_DATA_SEED.sql (Step 2 - Data seeding)
│   ├── DASHBOARD_DATA_SEED.sql (Step 3 - Dashboard statistics)
│   ├── apply-migrations.ps1 (PowerShell on-demand migration script)
│   ├── apply-migrations.cmd (Windows batch wrapper for migrations)
│   ├── PRODUCTION_ESSENTIAL_SEED.sql (Production-safe reference data only)
│   └── PRODUCTION_VALIDATION.sql (Validation queries)
├── Migrations/
│   └── 20251015162223_InitialCreate_SqlServer_Final.cs (EF Core migration)
└── Data/
    └── SeedData.cs (C# automatic seeding)
```

**IMPORTANT:** Before writing any SQL scripts, always check `DATABASE_SCHEMA_REFERENCE.md` for correct table and column names!

---
**Last Updated**: October 16, 2025
**Database Version**: Initial Schema (Migration: 20251015162223)
**Compatible With**: GCC Web Application v1.0

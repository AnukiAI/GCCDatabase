# Database Migration Instructions

Due to complexity with automated scripts, here's the manual migration process:

## Option 1: Use Entity Framework to Seed Production

The easiest way is to let the application seed the production database on first run.

1. Update `appsettings.Production.json` to point to production database (already configured)
2. Run the application in Production mode:
   ```bash
   set ASPNETCORE_ENVIRONMENT=Production
   dotnet run
   ```
3. The application will automatically:
   - Apply all migrations
   - Seed default data (roles, admin user, etc.)

## Option 2: Manual SQL Export/Import (RECOMMENDED FOR DATA)

Since you have existing data in development that needs to go to production:

### Step 1: Backup Development Database
```bash
sqlcmd -S ANUKI01 -E -Q "BACKUP DATABASE GCCDatabase_Dev TO DISK='C:\Temp\GCC_Dev_Backup.bak' WITH FORMAT;"
```

### Step 2: Generate Scripts in SSMS

1. Open SQL Server Management Studio
2. Connect to ANUKI01
3. Right-click `GCCDatabase_Dev` → Tasks → Generate Scripts
4. Choose "Select specific database objects"
5. Select tables: Roles, Companies, Partners, CertificationTypes, Products, Users, Tags, Certificates, ApiKeys
6. Click "Advanced" → Set "Types of data to script" to **"Data only"**
7. Save to file: `C:\Temp\data-export.sql`
8. Execute this file against production database

### Step 3: Execute on Production

```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i C:\Temp\data-export.sql
```

## Option 3: Use the Generated SQL (If Available)

If `DatabaseScripts/00_Complete_Schema.sql` was created during deployment package generation:

```bash
# 1. Deploy schema
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i DatabaseScripts/00_Complete_Schema.sql

# 2. Let application seed on first run
set ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

## Verification

After migration, verify data:

```sql
SELECT 'Certificates' as TableName, COUNT(*) as RowCount FROM Certificates
UNION ALL
SELECT 'Companies', COUNT(*) FROM Companies
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Roles', COUNT(*) FROM Roles;
```

Expected counts from development:
- Certificates: 178
- Companies: 28
- Products: 39
- Users: 14
- Roles: 4

## Current Status

- Production database: **EMPTY** (0 rows in all tables)
- Development database: **POPULATED** (178 certificates, 28 companies, 39 products)
- Schema: **EXISTS** in both (all migrations applied)

The application code is working correctly. Once data is migrated, certificates will display properly.

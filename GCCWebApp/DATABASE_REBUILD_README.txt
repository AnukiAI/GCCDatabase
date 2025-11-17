============================================
PRODUCTION DATABASE COMPLETE REBUILD
============================================

This folder contains scripts for a complete production database rebuild.

============================================
QUICK START
============================================

Run this single command:
    REBUILD_PRODUCTION_DATABASE.cmd

This will execute all 4 steps automatically:
  1. Drop all tables
  2. Create schema (from EF migrations)
  3. Seed lookup tables
  4. Load actual data

============================================
MANUAL STEPS (if needed)
============================================

Step 1: Drop All Tables
------------------------
File: Database_Scripts\DROP_ALL_TABLES.sql
What: Drops all existing tables in production
Run:  sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Database_Scripts\DROP_ALL_TABLES.sql"

Step 2: Create Schema
----------------------
File: Database_Scripts\SCHEMA_COMPLETE.sql
What: Creates all tables, indexes, constraints (1,888 lines)
Run:  sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Database_Scripts\SCHEMA_COMPLETE.sql"

Step 3: Seed Lookup Tables
---------------------------
File: Database_Scripts\PRODUCTION_ESSENTIAL_SEED.sql
What: Inserts ProductCategories, ProductTypes, CertificationTypes, Partners, Roles
Run:  sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Database_Scripts\PRODUCTION_ESSENTIAL_SEED.sql"

Step 4: Load Actual Data
-------------------------
File: PRODUCTION_DATA_CLEAN.sql
What: Inserts Companies (28), Products (39), Certificates (80)
Run:  sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_DATA_CLEAN.sql"

============================================
FILES CREATED
============================================

Main Scripts:
  REBUILD_PRODUCTION_DATABASE.cmd        - Master rebuild script (runs all steps)

Database Scripts:
  Database_Scripts\DROP_ALL_TABLES.sql   - Drops all tables
  Database_Scripts\SCHEMA_COMPLETE.sql   - Creates all tables (from EF migrations)
  Database_Scripts\PRODUCTION_ESSENTIAL_SEED.sql  - Lookup tables

Data Scripts:
  PRODUCTION_DATA_CLEAN.sql              - Actual data (companies, products, certs)
  export_dev_to_production_scripts_v2.sql - Export script (for future use)

Helper Scripts:
  COMPLETE_REFRESH_NOW.cmd               - Alternative rebuild approach
  REBUILD_PRODUCTION_DBO.cmd             - Simplified rebuild
  export_from_dev.cmd                    - Export helper
  import_to_production.cmd               - Import helper

============================================
EXPECTED RESULTS
============================================

After successful rebuild:

Lookup Tables:
  - ProductCategories: 6
  - ProductTypes: 5
  - CertificationTypes: 5
  - Partners: 3
  - Roles: 4
  - Permissions: 6

Actual Data:
  - Companies: 28
  - Products: 39
  - Certificates: 80 (Active only)

============================================
POST-REBUILD CHECKLIST
============================================

1. Verify Counts
   Run: sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Companies', COUNT(*) FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates;"

2. Restart IIS (CRITICAL!)
   - Contact AccuWebHosting support, OR
   - RDP: iisreset /noforce, OR
   - IIS Manager: Recycle GCCWebApp app pool

3. Test Production Site
   - Public: https://greencirclecertified.com/Certificate/CertificateTileView
   - Admin: https://greencirclecertified.com/Account/Login (admin / TestPass123!)
   - Verify certificates display
   - Test search and filters

4. Check Admin Pages
   - /Admin/UserManagement
   - /Admin/ProductManagement
   - /Admin/Partners
   - All should load without errors

============================================
TROUBLESHOOTING
============================================

Issue: "Login failed for user 'GCC_adminxyz123'"
Fix: Verify production database credentials with AccuWebHosting

Issue: "Cannot drop table because it is referenced by a FOREIGN KEY constraint"
Fix: The DROP script handles this with sp_MSforeachtable. If it still fails, run DROP script twice.

Issue: "Invalid column name" during schema creation
Fix: Schema was generated from latest EF migrations. If error occurs, check Database_Scripts\SCHEMA_COMPLETE.sql for syntax errors.

Issue: "Foreign key constraint" during data load
Fix: Some products/certificates reference lookup values that don't exist. This is expected - partial import is OK. Verify lookup tables were seeded first.

Issue: Data imported but not showing on website
Fix: RESTART IIS! The application caches database connections and needs restart to see new data.

============================================
MAINTENANCE
============================================

To regenerate schema SQL from latest migrations:
  dotnet ef migrations script --idempotent -o "Database_Scripts/SCHEMA_COMPLETE.sql"

To export fresh data from dev:
  Run: export_dev_to_production_scripts_v2.sql on ANUKI01 / GCCDatabase_Dev
  Then clean output and save as PRODUCTION_DATA_CLEAN.sql

============================================
SUPPORT
============================================

Questions: swest@anukiconsulting.com
Production Host: AccuWebHosting
Database: SQL Server 2019 (68.71.129.120,1533)

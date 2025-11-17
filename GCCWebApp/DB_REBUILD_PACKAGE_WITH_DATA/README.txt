================================================================================
GCC DATABASE REBUILD PACKAGE - WITH TEST CERTIFICATE DATA
================================================================================
Created: 2025-11-14
Purpose: Complete database rebuild including test certificates for public viewing

ISSUE ADDRESSED:
----------------
Production site loads successfully but shows NO certificate data because
PRODUCTION_ESSENTIAL_SEED.sql only seeds lookup tables and structure.
It does NOT include Companies, Products, or Certificates.

THIS PACKAGE FIXES THE ISSUE by including test certificate data.

================================================================================
WHAT'S INCLUDED:
================================================================================

01_DROP_ALL_TABLES.sql
    - Drops all existing tables and foreign key constraints
    - Provides clean slate for database rebuild
    - Safe to run - uses IF EXISTS checks

02_CREATE_SCHEMA.sql
    - Creates all 35+ database tables
    - Generated from Entity Framework migrations
    - Creates complete database schema structure

03_SEED_DATA_WITH_CERTIFICATES.sql
    - Seeds Product Categories (6)
    - Seeds Certification Types (5)
    - Seeds Partners (3)
    - Seeds Roles & Permissions (4 roles, 6 permissions)
    - Seeds Product Types (multiple)
    - *** Seeds 28 Companies ***
    - *** Seeds 43 Products ***
    - *** Seeds 175+ Certificates ***
    - Seeds 15 test users with various roles

================================================================================
EXECUTION INSTRUCTIONS:
================================================================================

1. CONNECT TO SQL SERVER
   -------------------------
   Server: 68.71.129.120,1533
   Database: gccwebap_certificates
   Username: GCC_adminxyz123
   Password: [your password]

2. BACKUP EXISTING DATA (If needed)
   ---------------------------------
   If you have production data you want to preserve, back it up first:

   USE gccwebap_certificates;
   GO

   -- Export your certificate data to CSV or create backup
   SELECT * INTO backup_Certificates FROM Certificates;
   SELECT * INTO backup_Companies FROM Companies;
   SELECT * INTO backup_Products FROM Products;

3. RUN SCRIPTS IN ORDER
   ----------------------
   Execute these scripts in EXACT order:

   Step 1: DROP ALL TABLES
   -----------------------
   Open and execute: 01_DROP_ALL_TABLES.sql

   Expected output: "All tables dropped successfully"

   Step 2: CREATE SCHEMA
   ---------------------
   Open and execute: 02_CREATE_SCHEMA.sql

   Expected output: "35+ tables created"

   Step 3: SEED DATA WITH CERTIFICATES
   -----------------------------------
   Open and execute: 03_SEED_DATA_WITH_CERTIFICATES.sql

   Expected output:
   ✓ 6 Product Categories created
   ✓ 5 Certification Types created
   ✓ 3 Partners created
   ✓ 28 Companies created
   ✓ 43 Products created
   ✓ 175+ Certificates created
   ✓ 15 Users created

4. VERIFY DATA
   -----------
   Run these queries to confirm data exists:

   SELECT COUNT(*) FROM Certificates;      -- Should show 175+
   SELECT COUNT(*) FROM Companies;         -- Should show 28
   SELECT COUNT(*) FROM Products;          -- Should show 43
   SELECT COUNT(*) FROM Users;             -- Should show 15

   SELECT TOP 10 Number, Status, EffectiveDate
   FROM Certificates
   WHERE Status = 'Active'
   ORDER BY CreatedAt DESC;

5. RESTART APPLICATION
   -------------------
   After seeding data, restart the GCC Web Application:

   - In IIS: Recycle application pool or restart site
   - Or: Touch web.config to force application restart
   - Or: Wait for app to auto-restart (may take a few minutes)

6. VERIFY PUBLIC PAGE
   ------------------
   Visit: https://gccwebapp.com/

   You should now see:
   ✓ Certificate tiles displaying
   ✓ Company names visible
   ✓ Product information shown
   ✓ Filters populated with data
   ✓ Search autocomplete working

================================================================================
TEST USER CREDENTIALS:
================================================================================

After seeding, you can login with these test accounts:
(All users have password: TestPass123!)
(All users have email: swest@anukiconsulting.com)

ADMIN ACCESS:
   Username: admin
   Role: Admin (full system access)

ANALYST ACCESS:
   Username: analyst
   Role: Analyst (certificate management)

EXTERNAL ACCESS:
   Username: external
   Role: External (limited access)

For complete list of all 15 test users, see the seed script.

================================================================================
DATABASE SCHEMA OWNERSHIP CRITICAL NOTE:
================================================================================

⚠️ IMPORTANT: All tables MUST be in 'dbo' schema (not GCC_adminxyz123 schema)

Entity Framework requires tables as dbo.TableName
Production user MUST have:
  1. db_owner role membership
  2. DEFAULT_SCHEMA = dbo

To verify schema ownership:
   SELECT SCHEMA_NAME(schema_id) as SchemaName, name as TableName
   FROM sys.tables
   ORDER BY SchemaName, TableName;

All tables should show schema: dbo
If tables show GCC_adminxyz123, they need to be transferred to dbo schema.

================================================================================
TROUBLESHOOTING:
================================================================================

ISSUE: Script fails with "Cannot drop table, it doesn't exist"
FIX: This is normal if running on fresh database. Continue to next script.

ISSUE: Script fails with "PRIMARY KEY constraint violation"
FIX: Tables already have data. Run 01_DROP_ALL_TABLES.sql first.

ISSUE: Certificate page still shows no data after seeding
FIX:
   1. Verify data in database: SELECT COUNT(*) FROM Certificates;
   2. Check table schema ownership (must be dbo, not username)
   3. Restart application (IIS recycle or touch web.config)
   4. Clear browser cache and refresh page
   5. Check logs folder for application errors

ISSUE: Login fails after seeding
FIX:
   1. Verify Users table has data: SELECT * FROM Users;
   2. Use test credentials: admin / TestPass123!
   3. Ensure 2FA is disabled in appsettings.Production.json

ISSUE: "Invalid object name 'dbo.Certificates'" error
FIX: Tables created in wrong schema (GCC_adminxyz123 instead of dbo)
   Solution: Run DatabaseScripts/REBUILD_PRODUCTION_DBO.cmd

================================================================================
WHAT DATA IS INCLUDED:
================================================================================

COMPANIES (28):
- Building Materials: CertainTeed, Owens Corning, Johns Manville, etc.
- Textiles: Interface, Shaw Industries, Mohawk Industries, etc.
- Consumer Products: Procter & Gamble, Unilever, etc.
- Electronics: Philips, Siemens, Samsung, etc.

PRODUCTS (43):
- Insulation products
- Flooring products
- Lighting products
- Carpeting products
- Furniture products
- Various sustainable materials

CERTIFICATES (175+):
- Multiple certificate types (CEF, CSF, RC, ES, MIR)
- Various statuses (Active, Draft, Expired)
- Real certificate numbers (format: YY-NNNN)
- Dates spanning multiple years
- Product IDs (ASINs, UPCs, GTINs)
- Complete metadata and fields

================================================================================
NEXT STEPS AFTER SEEDING:
================================================================================

1. Verify public certificate page displays data
2. Test search and filter functionality
3. Login as admin user to access admin dashboard
4. Review and customize test data as needed
5. Begin adding your real production certificates

================================================================================
PRODUCTION USE:
================================================================================

This seed data includes TEST CERTIFICATES for demonstration purposes.

For production use, you should:
1. Run these scripts to get a working system
2. Login as admin (username: admin, password: TestPass123!)
3. Navigate to Admin > User Management
4. Create your real admin account
5. Change the default admin password
6. Begin replacing test data with real production data
   OR
   Delete test certificates and add real ones through the UI

================================================================================
SUPPORT:
================================================================================

If you encounter issues:
1. Check application logs in /logs folder
2. Verify SQL Server connectivity
3. Confirm database user has db_owner role
4. Ensure ASPNETCORE_ENVIRONMENT=Production in web.config
5. Check that DatabaseProvider=SqlServer in web.config

For database schema issues:
- See: DatabaseScripts/README_DATABASE_SETUP.md
- See: DatabaseScripts/DATABASE_SCHEMA_OVERVIEW.md

================================================================================

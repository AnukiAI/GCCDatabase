============================================
SYNC DEV DATA TO PRODUCTION - COMPLETE GUIDE
============================================

OVERVIEW:
This guide walks through syncing all Companies, Products, and Active Certificates
from dev database (ANUKI01) to production (AccuWebHosting).

PREREQUISITE:
Production database should be cleared of test data (already done - see clear_results.log)

============================================
METHOD: Direct Database Export/Import
============================================

STEP 1: Export from Dev Database
---------------------------------
Run: export_from_dev.cmd

What it does:
- Connects to ANUKI01 / GCCDatabase_Dev
- Runs export_dev_to_production_scripts.sql
- Generates production-compatible INSERT statements
- Saves output to PRODUCTION_DATA_FROM_DEV.sql

Expected output: File created with ~28 companies, ~39 products, ~178 certificates


STEP 2: Import to Production Database
--------------------------------------
Run: import_to_production.cmd

What it does:
- Connects to 68.71.129.120,1533 / gccwebap_certificates
- Runs PRODUCTION_DATA_FROM_DEV.sql
- Imports all companies, products, certificates
- Verifies data counts
- Saves log to import_results.log

Expected counts:
- Companies: ~28
- Products: ~39
- Certificates: ~178 (all Active status)


STEP 3: Restart Production Application
---------------------------------------
Option A: IIS Restart (if you have IIS Manager access)
1. Open IIS Manager
2. Find GCCWebApp application pool
3. Right-click → Recycle

Option B: Remote Desktop (if you have RDP access)
1. Connect to production server
2. Run: iisreset /noforce

Option C: Contact hosting provider
If you don't have direct access, contact AccuWebHosting support


STEP 4: Verify Production Site
-------------------------------
1. Open browser: https://greencirclecertified.com/Certificate/CertificateTileView
2. Verify certificates are displaying
3. Check counts match expected numbers
4. Test search and filtering


============================================
ALTERNATIVE: ONE-CLICK SYNC
============================================

Run: sync_dev_to_production_complete.cmd

This runs both export and import in sequence. Only use if you want
to automate the entire process without reviewing export output first.


============================================
FILES CREATED
============================================

Scripts:
- export_dev_to_production_scripts.sql  (SQL query to generate INSERT statements)
- export_from_dev.cmd                   (Run on dev to export)
- import_to_production.cmd              (Run to import to production)
- sync_dev_to_production_complete.cmd   (One-click full sync)

Output Files:
- PRODUCTION_DATA_FROM_DEV.sql          (Generated INSERT statements)
- import_results.log                    (Import execution log)

Previous Files (no longer needed):
- PRODUCTION_SEED_CERTIFICATES.sql      (Old 22-certificate test seed)
- FULL_TEST_DATA_SEED_PRODUCTION.sql    (Failed PowerShell transformation)
- fix_seed_script.ps1                   (Old PowerShell approach)


============================================
TEST CREDENTIALS
============================================

All test accounts use Password: TestPass123!
All emails: swest@anukiconsulting.com

Primary Test Accounts:
- Username: admin     | Role: Admin    | Name: Admin User
- Username: analyst   | Role: Analyst  | Name: Jane Analyst
- Username: external  | Role: External | Name: External User

Additional Test Accounts (11 more):
- sarah.johnson, mike.chen, lisa.rodriguez, david.kim, emma.wilson,
  alex.martinez, jennifer.lee, robert.taylor, maria.garcia,
  public.user, testuser


============================================
TROUBLESHOOTING
============================================

Issue: "Cannot connect to ANUKI01"
Fix: Verify dev database is accessible, check VPN if required

Issue: "Cannot connect to 68.71.129.120,1533"
Fix: Verify production credentials, check firewall (port 1533)

Issue: "Column count mismatch"
Fix: This shouldn't happen with direct export method. If it does,
     check that export_dev_to_production_scripts.sql ran without errors.

Issue: "IDENTITY_INSERT is already ON"
Fix: Run this on production:
     SET IDENTITY_INSERT dbo.Companies OFF;
     SET IDENTITY_INSERT dbo.Products OFF;
     SET IDENTITY_INSERT dbo.Certificates OFF;

Issue: "Violation of PRIMARY KEY constraint"
Fix: Production database not cleared. Run clear_and_reseed_production.sql first.

Issue: "Data imported but not showing on website"
Fix: Restart IIS application pool (see Step 3 above)


============================================
SUPPORT
============================================

If you encounter issues:
1. Check the log files (import_results.log)
2. Verify database connection strings
3. Confirm production database schema matches dev
4. Contact: swest@anukiconsulting.com

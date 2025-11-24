===========================================================
GCC Database REBUILD Package - Clean Database Reset
===========================================================

⚠️  WARNING: These scripts will DELETE ALL existing data!
Only run if you want a completely fresh database.

===========================================================
RUN THESE SCRIPTS IN ORDER:
===========================================================

Connection Info:
----------------
Server: 68.71.129.120,1533
Database: gccwebap_certificates  
User: GCC_adminxyz123
Password: Q3Ez4e!gE_47KNi


STEP 1: Drop All Tables
------------------------
File: 01_DROP_ALL_TABLES.sql

⚠️  This DELETES all tables and data!
Only run if you're sure you want to start fresh.


STEP 2: Create Database Schema
-------------------------------
File: 02_CREATE_SCHEMA.sql

Creates all tables:
- Users, Roles, UserRoles
- Certificates, Companies, Products
- CertificateProductIds (ASINs, UPCs, etc.)
- GoogleAnalyticsSettings
- And 30+ other tables


STEP 3: Seed Essential Data
----------------------------
File: 03_SEED_DATA.sql

Seeds production-ready data:
- Product Categories (6 types)
- Certification Types (CEF, CSF, RC, etc.)
- Certificate Fields (50+ fields)
- Default Admin User
  Username: admin
  Password: TestPass123!
- Roles (Admin, Analyst, External, Public)
- Google Analytics Settings (empty, ready for config)


STEP 4: Restart Your Application
---------------------------------
After running all scripts:
1. Go to your web application folder
2. Edit web.config (add a space anywhere)
3. Save the file
4. IIS will automatically restart the app
5. Visit https://gccwebapp.com/
6. Login with: admin / TestPass123!


===========================================================
How to Run These Scripts
===========================================================

Option A: Using SQL Server Management Studio (SSMS)
----------------------------------------------------
1. Connect to: 68.71.129.120,1533
2. Select database: gccwebap_certificates
3. Open each .sql file
4. Click "Execute" (F5)
5. Wait for "Command completed successfully"


Option B: Using AccuWebHosting phpMyAdmin/Database Tool
--------------------------------------------------------
1. Login to AccuWebHosting control panel
2. Open Database Manager
3. Select gccwebap_certificates database
4. Copy/paste each script into the SQL query box
5. Click "Run" or "Execute"


Option C: Using sqlcmd Command Line
-----------------------------------
sqlcmd -S 68.71.129.120,1533 -d gccwebap_certificates ^
       -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" ^
       -i 01_DROP_ALL_TABLES.sql

sqlcmd -S 68.71.129.120,1533 -d gccwebap_certificates ^
       -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" ^
       -i 02_CREATE_SCHEMA.sql

sqlcmd -S 68.71.129.120,1533 -d gccwebap_certificates ^
       -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" ^
       -i 03_SEED_DATA.sql


===========================================================
Expected Results
===========================================================

After Step 1 (Drop): 0 tables
After Step 2 (Create): 35+ tables
After Step 3 (Seed): Tables with data, 1 admin user

Website should load without errors!

===========================================================

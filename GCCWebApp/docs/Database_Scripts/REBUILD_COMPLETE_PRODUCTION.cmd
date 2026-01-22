@echo off
REM ============================================
REM Complete Production Database Rebuild
REM ============================================
REM This script performs ALL 4 steps:
REM 1. Generate fresh schema from EF migrations
REM 2. Drop all tables
REM 3. Recreate schema
REM 4. Seed test data
REM
REM ⚠️ WARNING: This is DESTRUCTIVE!
REM ============================================

echo ============================================
echo GCC DATABASE COMPLETE REBUILD
echo ============================================
echo.
echo ⚠️  WARNING: DESTRUCTIVE OPERATION ⚠️
echo.
echo This will:
echo   1. Generate fresh schema from migrations
echo   2. DROP all tables (complete wipe)
echo   3. CREATE schema from scratch
echo   4. SEED test data
echo.
echo Target: gccwebap_certificates (PRODUCTION TEST)
echo Server: 68.71.129.120,1533
echo.
echo Press Ctrl+C to CANCEL, or
pause

REM Database connection details
SET SERVER=68.71.129.120,1533
SET DATABASE=gccwebap_certificates
SET USERNAME=GCC_adminxyz123
SET PASSWORD=Q3Ez4e!gE_47KNi

echo.
echo ============================================
echo Step 1/4: Generating fresh schema script...
echo ============================================
cd ..
dotnet ef migrations script --idempotent > schema_migrations_temp.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Schema generation failed!
    echo Make sure you're in the GCCWebApp project folder.
    pause
    exit /b 1
)

REM Create final script with SET statements
echo -- Required SET options for SQL Server > schema_migrations.sql
echo SET ANSI_NULLS ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo SET QUOTED_IDENTIFIER ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo SET ANSI_PADDING ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo SET ANSI_WARNINGS ON; >> schema_migrations.sql
echo GO >> schema_migrations.sql
echo. >> schema_migrations.sql

REM Skip first 3 lines (build output) and append rest
powershell -Command "Get-Content schema_migrations_temp.sql | Select-Object -Skip 3 | Add-Content schema_migrations.sql"
del schema_migrations_temp.sql

echo ✓ Schema script generated successfully
echo.

echo ============================================
echo Step 2/4: Dropping all tables...
echo ============================================
echo This will delete all existing tables and data...
timeout /t 3
cd Database_Scripts
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i DROP_ALL_TABLES_SAFE.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Drop tables failed!
    echo Check the error messages above.
    pause
    exit /b 1
)

echo.
echo ============================================
echo Step 3/4: Recreating schema from migrations...
echo ============================================
cd ..
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i schema_migrations.sql 2>&1 | findstr /C:"Msg" /C:"Error"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo WARNING: Possible errors during schema creation!
    echo Check output above. Press Ctrl+C to stop, or
    pause
)

REM Verify tables were created
echo.
echo Verifying schema creation...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT COUNT(*) as TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'" -h -1 > table_count.txt
set /p TABLE_COUNT=<table_count.txt
del table_count.txt

echo Tables created: %TABLE_COUNT%
if "%TABLE_COUNT%"=="35" (
    echo ✓ Schema created successfully - all 35 tables present
) else (
    echo.
    echo WARNING: Expected 35 tables, found %TABLE_COUNT%
    echo Schema creation may have failed!
    echo Press Ctrl+C to stop, or
    pause
)

echo.
echo ============================================
echo Step 4/4: Seeding test data...
echo ============================================
cd Database_Scripts
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i FULL_TEST_DATA_SEED.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Data seeding failed!
    echo Check the error messages above.
    pause
    exit /b 1
)

echo.
echo ============================================
echo ✅ REBUILD COMPLETE!
echo ============================================
echo.
echo Database is now ready with:
echo   ✓ Complete schema (35 tables)
echo   ✓ 15 test users
echo   ✓ 28 companies
echo   ✓ 43 products
echo   ✓ 25 certificates
echo   ✓ Roles, permissions, tags, API keys
echo.
echo Next steps:
echo.
echo   1. Test login: https://gccwebapp.com/Account/Login
echo      Username: admin
echo      Password: TestPass123!
echo.
echo   2. Verify users: https://gccwebapp.com/Admin/UserManagement
echo      Should show 15 users
echo.
echo   3. Check products: https://gccwebapp.com/Product/Management
echo      Should show 43 products
echo.
echo   4. View certificates: https://gccwebapp.com/Certificate/CertificateTileView
echo      Should show 25 certificates
echo.
pause

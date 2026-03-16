@echo off
REM ============================================
REM COMPLETE DATABASE REBUILD - Production Test
REM ============================================
REM This script performs a FULL rebuild:
REM 1. DROP all tables (complete wipe)
REM 2. CREATE schema from scratch
REM 3. SEED with test data
REM
REM ⚠️ WARNING: This is DESTRUCTIVE!
REM Use this ONLY for test/development environments
REM ============================================

echo ============================================
echo GCC DATABASE COMPLETE REBUILD
echo ============================================
echo.
echo ⚠️  WARNING: DESTRUCTIVE OPERATION ⚠️
echo.
echo This will:
echo   1. DROP all tables (schema + data)
echo   2. CREATE fresh schema
echo   3. SEED test data
echo.
echo Current Target: gccwebap_certificates (PRODUCTION TEST)
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
echo Step 1/3: DROPPING all tables...
echo ============================================
echo This will delete everything (schema + data)
timeout /t 3
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i DROP_ALL_TABLES.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Table drop failed!
    echo Check the error messages above.
    pause
    exit /b 1
)

echo.
echo ============================================
echo Step 2/3: CREATING schema from scratch...
echo ============================================
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i SCHEMA_COMPLETE.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Schema creation failed!
    echo Check the error messages above.
    pause
    exit /b 1
)

echo.
echo ============================================
echo Step 3/3: SEEDING test data...
echo ============================================
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
echo ✅ SUCCESS: Database rebuilt completely!
echo ============================================
echo.
echo Database is now fresh and ready with:
echo   ✓ Complete schema (all tables, indexes, constraints)
echo   ✓ 15 test users (password: TestPass123!)
echo   ✓ 28 companies
echo   ✓ 43 products
echo   ✓ 25 certificates
echo   ✓ All reference data
echo.
echo Next steps:
echo   1. Test login: https://gccwebapp.com/Account/Login
echo      Username: admin
echo      Password: TestPass123!
echo.
echo   2. Verify users: https://gccwebapp.com/Admin/UserManagement
echo      Should show 15 users
echo.
echo   3. Check certificates: https://gccwebapp.com/Certificate/CertificateTileView
echo      Should show 25 certificates
echo.
pause

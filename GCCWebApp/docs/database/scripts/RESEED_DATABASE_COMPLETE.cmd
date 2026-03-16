@echo off
REM ============================================
REM Complete Database Reseed Script
REM ============================================
REM This script:
REM 1. Cleans all data (keeps schema)
REM 2. Reseeds with fresh test data
REM ============================================

echo ============================================
echo GCC Database Complete Reseed
echo ============================================
echo.
echo This will:
echo   1. CLEAN all data from the database
echo   2. RESEED with fresh test data
echo.
echo Press Ctrl+C to cancel, or
pause

REM Database connection details
SET SERVER=68.71.129.120,1533
SET DATABASE=gccwebap_certificates
SET USERNAME=GCC_adminxyz123
SET PASSWORD=Q3Ez4e!gE_47KNi

echo.
echo ============================================
echo Step 1: Cleaning all data...
echo ============================================
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i CLEAN_ALL_DATA.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Data cleanup failed!
    echo Check the error messages above.
    pause
    exit /b 1
)

echo.
echo ============================================
echo Step 2: Reseeding with test data...
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
echo SUCCESS: Database reseeded completely!
echo ============================================
echo.
echo You can now:
echo   - Login to https://gccwebapp.com/Account/Login
echo   - Username: admin
echo   - Password: TestPass123!
echo.
echo Or check User Management:
echo   https://gccwebapp.com/Admin/UserManagement
echo.
pause

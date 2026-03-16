@echo off
REM ============================================
REM Company Data Seeding Script Runner
REM ============================================
REM Run this script to seed company data for Company Management page
REM Location: Database_Scripts\seed-companies.cmd
REM ============================================

echo ============================================
echo GCC Company Data Seeding
echo ============================================
echo.

:MENU
echo Select environment:
echo.
echo 1. Development (ANUKI01)
echo 2. Production (AccuWebHosting)
echo 3. Exit
echo.
set /p choice="Enter your choice (1-3): "

if "%choice%"=="1" goto DEV
if "%choice%"=="2" goto PROD
if "%choice%"=="3" goto END
echo Invalid choice. Please try again.
echo.
goto MENU

:DEV
echo.
echo Running on DEVELOPMENT (ANUKI01)...
echo Database: GCCDatabase_Dev
echo.
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -E -i "COMPANY_DATA_SEED.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo SUCCESS: Company data seeded successfully!
    echo ============================================
    echo.
    echo Next steps:
    echo 1. Navigate to http://localhost:5101/Company/Index
    echo 2. Verify companies are displayed
    echo 3. Test search, sort, and CRUD operations
    echo.
) else (
    echo.
    echo ============================================
    echo ERROR: Failed to seed company data
    echo ============================================
    echo.
    echo Troubleshooting:
    echo 1. Verify SQL Server is running
    echo 2. Check database exists: GCCDatabase_Dev
    echo 3. Ensure you have permissions
    echo 4. Review error messages above
    echo.
)

pause
goto MENU

:PROD
echo.
echo ============================================
echo WARNING: PRODUCTION DEPLOYMENT
echo ============================================
echo.
echo This will seed data to PRODUCTION database!
echo Server: 68.71.129.120:1533
echo Database: gccwebap_certificates
echo.
set /p confirm="Are you sure you want to continue? (yes/no): "

if /i not "%confirm%"=="yes" (
    echo.
    echo Operation cancelled.
    echo.
    pause
    goto MENU
)

echo.
set /p username="Enter SQL username (GCC_adminxyz123): "
if "%username%"=="" set username=GCC_adminxyz123

echo.
echo Running on PRODUCTION...
echo.
sqlcmd -S 68.71.129.120,1533 -U %username% -d gccwebap_certificates -i "COMPANY_DATA_SEED.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo SUCCESS: Production data seeded successfully!
    echo ============================================
    echo.
    echo Next steps:
    echo 1. Navigate to https://gccwebapp.com/Company/Index
    echo 2. Verify companies are displayed
    echo 3. Test production environment
    echo.
) else (
    echo.
    echo ============================================
    echo ERROR: Failed to seed production data
    echo ============================================
    echo.
    echo Troubleshooting:
    echo 1. Verify VPN/network connection
    echo 2. Check credentials are correct
    echo 3. Verify firewall allows port 1533
    echo 4. Review error messages above
    echo.
)

pause
goto MENU

:END
echo.
echo Exiting...
echo.
exit /b 0

@echo off
REM =========================================
REM GCC Production Database Quick Fix
REM =========================================
REM This script will:
REM 1. Test connection to production database
REM 2. Run diagnostic to see current state
REM 3. Deploy minimal data to get site working
REM 4. Verify deployment success
REM =========================================

setlocal

set SERVER=68.71.129.120,1533
set DATABASE=gccwebap_certificates
set USERNAME=GCC_adminxyz123
set PASSWORD=Q3Ez4e!gE_47KNi

echo =========================================
echo GCC PRODUCTION DATABASE QUICK FIX
echo =========================================
echo.
echo Server: %SERVER%
echo Database: %DATABASE%
echo.

REM Test connection
echo Testing connection to production database...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -Q "SELECT @@VERSION" -d master -b >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Cannot connect to production database
    echo Please check:
    echo   1. VPN connection is active
    echo   2. Server %SERVER% is accessible
    echo   3. Firewall allows port 1533
    echo   4. Credentials are correct
    pause
    exit /b 1
)
echo [OK] Connection successful
echo.

REM Check database exists
echo Checking if database exists...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -Q "SELECT name FROM sys.databases WHERE name = '%DATABASE%'" -d master -h -1 | findstr %DATABASE% >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Database '%DATABASE%' does not exist
    pause
    exit /b 1
)
echo [OK] Database exists
echo.

REM Run diagnostic
echo =========================================
echo STEP 1: Running diagnostic (current state)
echo =========================================
echo.
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 01_Diagnose_Production.sql
echo.
pause
echo.

REM Confirm deployment
echo =========================================
echo STEP 2: Deploy minimal data
echo =========================================
echo.
echo This will:
echo   - Delete any existing data in production
echo   - Import 4 roles, 1 admin user, 3 partners
echo   - Import 5 companies, 5 cert types, 5 products
echo   - Import 10 sample certificates
echo.
set /p CONTINUE="Continue with deployment? (Y/N): "
if /i not "%CONTINUE%"=="Y" (
    echo Deployment cancelled
    pause
    exit /b 0
)
echo.

echo Deploying data...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 04_Quick_Fix_Minimal_Data.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Deployment failed!
    echo Check the error messages above
    pause
    exit /b 1
)
echo.

REM Verify deployment
echo =========================================
echo STEP 3: Verifying deployment
echo =========================================
echo.
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT 'Certificates' as TableName, COUNT(*) as RowCount FROM Certificates UNION ALL SELECT 'Companies', COUNT(*) FROM Companies UNION ALL SELECT 'Roles', COUNT(*) FROM Roles ORDER BY TableName"
echo.

REM Success message
echo =========================================
echo DEPLOYMENT COMPLETE!
echo =========================================
echo.
echo Production database now has minimal data
echo.
echo NEXT STEPS:
echo   1. Visit your production website
echo   2. Go to /Certificate/CertificateTileView
echo   3. You should see 10 certificates
echo   4. Login: admin@greencirclecertified.com / Admin123!
echo.
echo To import FULL dataset (178 certificates):
echo   Run: ..\DataMigration\migrate-to-production.ps1
echo.
echo =========================================
pause

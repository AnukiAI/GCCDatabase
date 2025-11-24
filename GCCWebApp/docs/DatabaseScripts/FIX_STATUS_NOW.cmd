@echo off
REM =========================================
REM FIX: Certificate Status Values
REM =========================================
REM This fixes certificates not displaying due to wrong status values
REM =========================================

setlocal

set SERVER=68.71.129.120,1533
set DATABASE=gccwebap_certificates
set USERNAME=GCC_adminxyz123
set PASSWORD=Q3Ez4e!gE_47KNi

echo =========================================
echo CERTIFICATE STATUS FIX
echo =========================================
echo.
echo Problem: Database has 80 certificates but none display
echo Cause: Certificates have wrong status values
echo.
echo The public tile view ONLY shows certificates with status:
echo   - active
echo   - expiring_soon
echo   - renewal_in_progress
echo.
echo This script will fix the status values.
echo.
pause
echo.

REM Test connection
echo Testing connection...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -Q "SELECT @@VERSION" -d master -b >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Cannot connect to production database
    pause
    exit /b 1
)
echo [OK] Connected
echo.

REM Check current status
echo =========================================
echo STEP 1: Check current certificate status
echo =========================================
echo.
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 05_Check_Certificate_Status.sql
echo.
pause
echo.

REM Fix status
echo =========================================
echo STEP 2: Fix certificate status values
echo =========================================
echo.
set /p CONTINUE="Update certificate status to make them visible? (Y/N): "
if /i not "%CONTINUE%"=="Y" (
    echo Fix cancelled
    pause
    exit /b 0
)
echo.

echo Fixing status values...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 06_Fix_Certificate_Status.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Fix failed!
    pause
    exit /b 1
)
echo.

REM Verify
echo =========================================
echo STEP 3: Verify Fix
echo =========================================
echo.
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT Status, COUNT(*) as Count FROM Certificates WHERE Status IN ('active', 'expiring_soon', 'renewal_in_progress') GROUP BY Status"
echo.

echo =========================================
echo FIX COMPLETE!
echo =========================================
echo.
echo Next steps:
echo   1. Refresh your production website
echo   2. Go to /Certificate/CertificateTileView
echo   3. Certificates should now display!
echo.
echo If still not showing, check:
echo   1. Browser cache - do hard refresh (Ctrl+F5)
echo   2. Application is using Production environment
echo   3. Application has been restarted to use new data
echo.
echo =========================================
pause

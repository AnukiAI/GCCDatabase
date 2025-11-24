@echo off
echo ============================================
echo COMPLETE DEV TO PRODUCTION SYNC
echo ============================================
echo.
echo This will:
echo 1. Export data from ANUKI01 / GCCDatabase_Dev
echo 2. Import data to 68.71.129.120,1533 / gccwebap_certificates
echo.
echo Press Ctrl+C to cancel, or
pause

echo.
echo ============================================
echo STEP 1: Exporting from Dev Database
echo ============================================
call export_from_dev.cmd

if %errorlevel% neq 0 (
    echo.
    echo Export failed. Stopping sync process.
    pause
    exit /b 1
)

echo.
echo ============================================
echo STEP 2: Importing to Production Database
echo ============================================
call import_to_production.cmd

if %errorlevel% neq 0 (
    echo.
    echo Import failed. Check import_results.log
    pause
    exit /b 1
)

echo.
echo ============================================
echo SYNC COMPLETE!
echo ============================================
echo.
echo Next step: Restart production IIS to see data on website
echo.
echo Production URL: https://greencirclecertified.com/Certificate/CertificateTileView
echo.
pause

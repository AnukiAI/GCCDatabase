@echo off
REM Quick script to seed production database
REM Run this to populate certificate data

echo ============================================
echo GCC Production Database Seeding
echo ============================================
echo.
echo Connecting to: 68.71.129.120,1533
echo Database: gccwebap_certificates
echo.

sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_SEED_CERTIFICATES.sql" -o "seed_results.log"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo SUCCESS: Seed script executed successfully
    echo ============================================
    echo.
    echo Check seed_results.log for details
    echo.
    echo Next step: Restart your IIS application
    echo Then visit: https://gccwebapp.com/
    echo.
) else (
    echo.
    echo ============================================
    echo ERROR: Seed script failed
    echo ============================================
    echo.
    echo Check seed_results.log for error details
    echo Error code: %ERRORLEVEL%
    echo.
)

pause

@echo off
REM =========================================
REM GCC Production - Load Large Test Dataset
REM =========================================
REM This script loads 250 certificates and supporting test data
REM into the production database for testing/staging purposes.
REM =========================================

echo.
echo =========================================
echo GCC - Load Large Test Dataset
echo =========================================
echo.
echo This will load comprehensive test data:
echo   - 4 Roles
echo   - 15 Users
echo   - 3 Partners
echo   - 50 Companies
echo   - 10 Product Categories
echo   - 15 Product Types
echo   - 10 Certification Types
echo   - 30 Tags
echo   - 150 Products
echo   - 250 Certificates
echo.
echo WARNING: This will CLEAR existing data!
echo.
set /p CONFIRM="Type YES to continue: "

if /i NOT "%CONFIRM%"=="YES" (
    echo.
    echo Operation cancelled.
    pause
    exit /b
)

echo.
echo Connecting to production database...
echo.

REM Change to script directory
cd /d "%~dp0"

sqlcmd -S 68.71.129.120,1533 ^
       -U GCC_adminxyz123 ^
       -P "Q3Ez4e!gE_47KNi" ^
       -d gccwebap_certificates ^
       -i "SEED_LARGE_TEST_DATA.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =========================================
    echo SUCCESS! Large test dataset loaded.
    echo =========================================
    echo.
    echo Next steps:
    echo   1. Visit your production website
    echo   2. Navigate to /Certificate/CertificateTileView
    echo   3. You should see ~175 active certificates
    echo   4. Login with admin@greencirclecertified.com / Admin123!
    echo.
) else (
    echo.
    echo =========================================
    echo ERROR! Script failed with error code %ERRORLEVEL%
    echo =========================================
    echo.
    echo Please check the error messages above.
    echo.
)

pause

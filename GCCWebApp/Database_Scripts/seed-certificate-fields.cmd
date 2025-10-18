@echo off
REM ============================================
REM Certificate Fields Data Seeding Script
REM ============================================

echo.
echo ============================================
echo Certificate Fields Data Seeding
echo ============================================
echo.

REM Check if SQL script exists
if not exist "CERTIFICATE_FIELDS_DATA_SEED.sql" (
    echo ERROR: CERTIFICATE_FIELDS_DATA_SEED.sql not found
    echo Please run this script from the Database_Scripts directory
    pause
    exit /b 1
)

echo Select target environment:
echo.
echo 1. Development (ANUKI01 - GCCDatabase_Dev)
echo 2. Production (AccuWebHosting - gccwebap_certificates)
echo 3. Cancel
echo.
set /p choice="Enter your choice (1-3): "

if "%choice%"=="1" goto development
if "%choice%"=="2" goto production
if "%choice%"=="3" goto cancel
echo Invalid choice
goto end

:development
echo.
echo Running on Development database (ANUKI01)...
echo.
sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P DevP@ss2024!SQL -i "CERTIFICATE_FIELDS_DATA_SEED.sql"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Certificate Fields seeding completed successfully
) else (
    echo.
    echo ❌ ERROR: Seeding failed with error code %ERRORLEVEL%
)
goto end

:production
echo.
echo WARNING: You are about to seed Production database!
set /p confirm="Are you sure? (yes/no): "
if /i not "%confirm%"=="yes" goto cancel

echo.
echo Running on Production database (AccuWebHosting)...
echo.
set /p password="Enter password for GCC_adminxyz123: "
sqlcmd -S 68.71.129.120,1533 -d gccwebap_certificates -U GCC_adminxyz123 -P %password% -i "CERTIFICATE_FIELDS_DATA_SEED.sql"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Certificate Fields seeding completed successfully
) else (
    echo.
    echo ❌ ERROR: Seeding failed with error code %ERRORLEVEL%
)
goto end

:cancel
echo.
echo Operation cancelled
goto end

:end
echo.
pause

@echo off
REM ============================================
REM Export COMPLETE Dev Data to Production
REM ============================================
REM Exports ALL business data for 100% dev/prod match
REM Includes: CertificateProductIds, Tags, CustomFields, etc.
REM ============================================

echo ============================================
echo GCC Database - Complete Export from Dev
echo ============================================
echo.
echo Source: ANUKI01 / GCCDatabase_Dev
echo Output: PRODUCTION_DATA_COMPLETE.sql
echo.
echo This exports ALL data including:
echo   - All lookup tables
echo   - Companies, Products, Certificates
echo   - CertificateProductIds (ASINs/UPCs)
echo   - Certificate Tags associations
echo   - Custom Field Values
echo.
echo EXCLUDES: Users, Roles, Permissions (security)
echo.
echo ============================================
echo.

set SOURCE_SERVER=ANUKI01
set SOURCE_DATABASE=GCCDatabase_Dev
set SOURCE_USER=GCC_DevUser
set SOURCE_PASSWORD=DevP@ss2024!SQL
set EXPORT_SCRIPT=export_dev_to_production_COMPLETE.sql
set OUTPUT_FILE=PRODUCTION_DATA_COMPLETE.sql

echo Running export script...
echo.

sqlcmd -S %SOURCE_SERVER% -U %SOURCE_USER% -P "%SOURCE_PASSWORD%" -d %SOURCE_DATABASE% -i "%EXPORT_SCRIPT%" -o "%OUTPUT_FILE%" -W

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo EXPORT SUCCESSFUL
    echo ============================================
    echo.
    echo Output file: %OUTPUT_FILE%
    echo.
    echo Next steps:
    echo   1. Review %OUTPUT_FILE% to verify data
    echo   2. Run import_to_production_COMPLETE.cmd to import
    echo.
    echo ============================================
) else (
    echo.
    echo ============================================
    echo EXPORT FAILED - Error code: %ERRORLEVEL%
    echo ============================================
    echo.
    echo Check:
    echo   - SQL Server is running
    echo   - Database credentials are correct
    echo   - You have read access to GCCDatabase_Dev
    echo.
)

pause

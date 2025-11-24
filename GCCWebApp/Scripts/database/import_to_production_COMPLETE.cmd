@echo off
REM ============================================
REM Import COMPLETE Data to Production
REM ============================================
REM Imports ALL business data for 100% dev/prod match
REM ============================================

echo ============================================
echo GCC Database - Complete Import to Production
echo ============================================
echo.
echo Target: 68.71.129.120,1533 / gccwebap_certificates
echo Source File: PRODUCTION_DATA_COMPLETE.sql
echo.
echo WARNING: This will import ALL dev data to production
echo ============================================
echo.

set PROD_SERVER=68.71.129.120,1533
set PROD_DATABASE=gccwebap_certificates
set PROD_USER=GCC_adminxyz123
set PROD_PASSWORD=Q3Ez4e!gE_47KNi
set INPUT_FILE=PRODUCTION_DATA_COMPLETE.sql
set LOG_FILE=import_results_COMPLETE.log

REM Check if input file exists
if not exist "%INPUT_FILE%" (
    echo ERROR: %INPUT_FILE% not found!
    echo.
    echo Run export_from_dev_COMPLETE.cmd first to generate the export file.
    echo.
    pause
    exit /b 1
)

echo File found: %INPUT_FILE%
echo.
echo IMPORTANT: Clear existing data first? (Recommended)
echo.
choice /C YN /M "Clear existing certificate data before import"

if %ERRORLEVEL% EQU 1 (
    echo.
    echo Clearing existing data...
    echo.

    REM Create temp script to clear data
    echo DELETE FROM CertificateProductIds; > clear_data.sql
    echo DELETE FROM CertificateTags; >> clear_data.sql
    echo DELETE FROM CustomFieldValues; >> clear_data.sql
    echo DELETE FROM Certificates; >> clear_data.sql
    echo DELETE FROM Products; >> clear_data.sql
    echo DELETE FROM ProductIdentifiers; >> clear_data.sql
    echo DELETE FROM Companies; >> clear_data.sql
    echo DELETE FROM Tags; >> clear_data.sql
    echo DELETE FROM CustomFields; >> clear_data.sql
    echo PRINT 'Existing data cleared successfully'; >> clear_data.sql

    sqlcmd -S %PROD_SERVER% -U %PROD_USER% -P "%PROD_PASSWORD%" -d %PROD_DATABASE% -i "clear_data.sql" -W

    if %ERRORLEVEL% NEQ 0 (
        echo ERROR: Failed to clear existing data
        echo.
        pause
        exit /b 1
    )

    del clear_data.sql
    echo Data cleared successfully.
    echo.
)

echo Importing data to production...
echo This may take several minutes...
echo.

sqlcmd -S %PROD_SERVER% -U %PROD_USER% -P "%PROD_PASSWORD%" -d %PROD_DATABASE% -i "%INPUT_FILE%" -o "%LOG_FILE%" -W -t 600

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo IMPORT SUCCESSFUL
    echo ============================================
    echo.
    echo Log file: %LOG_FILE%
    echo.
    echo Verifying imported data...
    echo.

    REM Create verification script
    echo SELECT 'Companies' as Entity, COUNT(*) as Count FROM Companies > verify.sql
    echo UNION ALL SELECT 'Products', COUNT(*) FROM Products >> verify.sql
    echo UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates >> verify.sql
    echo UNION ALL SELECT 'CertificateProductIds', COUNT(*) FROM CertificateProductIds >> verify.sql
    echo UNION ALL SELECT 'Tags', COUNT(*) FROM Tags >> verify.sql
    echo ORDER BY Entity; >> verify.sql

    sqlcmd -S %PROD_SERVER% -U %PROD_USER% -P "%PROD_PASSWORD%" -d %PROD_DATABASE% -i "verify.sql" -W

    del verify.sql

    echo.
    echo ============================================
    echo Next Steps:
    echo ============================================
    echo.
    echo 1. RESTART APPLICATION (IIS recycle or touch web.config)
    echo 2. Test production site: https://greencirclecertified.com
    echo 3. Verify certificates display correctly
    echo 4. Check product IDs loaded (ASINs/UPCs)
    echo 5. Verify certificate tags work
    echo.
    echo ============================================
) else (
    echo.
    echo ============================================
    echo IMPORT FAILED - Error code: %ERRORLEVEL%
    echo ============================================
    echo.
    echo Check log file: %LOG_FILE%
    echo.
    echo Common issues:
    echo   - Connection timeout (increase with -t parameter)
    echo   - Foreign key constraint violations
    echo   - Duplicate key violations (data wasn't cleared)
    echo   - Schema mismatch between dev and prod
    echo.
)

pause

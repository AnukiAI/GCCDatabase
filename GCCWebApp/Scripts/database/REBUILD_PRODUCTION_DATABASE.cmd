@echo off
echo ============================================
echo COMPLETE PRODUCTION DATABASE REBUILD
echo ============================================
echo.
echo This will:
echo   1. DROP all existing tables
echo   2. CREATE schema from EF migrations
echo   3. SEED lookup tables (essential data)
echo   4. LOAD actual data (companies, products, certificates)
echo.
echo WARNING: This will DELETE ALL existing data!
echo.
set /p confirm="Type 'YES' to continue: "
if not "%confirm%"=="YES" (
    echo Cancelled.
    pause
    exit /b 0
)

set SERVER=68.71.129.120,1533
set USER=GCC_adminxyz123
set PASS=Q3Ez4e!gE_47KNi
set DB=gccwebap_certificates

echo.
echo ============================================
echo STEP 1: Dropping all tables...
echo ============================================
sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DB% -i "Database_Scripts\DROP_ALL_TABLES.sql" -o "rebuild_step1_drop.log"

if %errorlevel% neq 0 (
    echo ERROR: Drop tables failed
    type rebuild_step1_drop.log
    pause
    exit /b 1
)

echo.
echo ============================================
echo STEP 2: Creating schema from EF migrations...
echo ============================================
sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DB% -i "Database_Scripts\SCHEMA_COMPLETE.sql" -o "rebuild_step2_schema.log"

if %errorlevel% neq 0 (
    echo ERROR: Schema creation failed
    type rebuild_step2_schema.log | findstr /C:"Msg" /C:"Error"
    pause
    exit /b 1
)

echo.
echo ============================================
echo STEP 3: Seeding lookup tables...
echo ============================================
sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DB% -i "Database_Scripts\PRODUCTION_ESSENTIAL_SEED.sql" -o "rebuild_step3_essential.log"

if %errorlevel% neq 0 (
    echo ERROR: Essential seed failed
    type rebuild_step3_essential.log | findstr /C:"Msg" /C:"Error"
    pause
    exit /b 1
)

echo.
echo ============================================
echo STEP 4: Loading actual data...
echo ============================================
if exist "PRODUCTION_DATA_CLEAN.sql" (
    sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DB% -i "PRODUCTION_DATA_CLEAN.sql" -o "rebuild_step4_data.log"

    if %errorlevel% neq 0 (
        echo WARNING: Some data may have failed to import
        echo Check rebuild_step4_data.log for details
    )
) else (
    echo WARNING: PRODUCTION_DATA_CLEAN.sql not found
    echo Run export_all_tables_from_dev.sql first to generate data
)

echo.
echo ============================================
echo STEP 5: Verifying database...
echo ============================================
sqlcmd -S %SERVER% -U %USER% -P "%PASS%" -d %DB% -Q "SELECT 'ProductCategories' AS Entity, COUNT(*) AS Count FROM ProductCategories UNION ALL SELECT 'ProductTypes', COUNT(*) FROM ProductTypes UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes UNION ALL SELECT 'Partners', COUNT(*) FROM Partners UNION ALL SELECT 'Companies', COUNT(*) FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates;" -W

echo.
echo ============================================
echo DATABASE REBUILD COMPLETE!
echo ============================================
echo.
echo Expected counts:
echo   - ProductCategories: 6
echo   - ProductTypes: 5+
echo   - CertificationTypes: 5+
echo   - Partners: 3
echo   - Companies: 28
echo   - Products: 39
echo   - Certificates: 80
echo.
echo CRITICAL: You MUST restart IIS for changes to take effect!
echo   1. Contact AccuWebHosting support, OR
echo   2. RDP: iisreset /noforce, OR
echo   3. IIS Manager: Recycle GCCWebApp app pool
echo.
echo Then test: https://greencirclecertified.com/Certificate/CertificateTileView
echo.
pause

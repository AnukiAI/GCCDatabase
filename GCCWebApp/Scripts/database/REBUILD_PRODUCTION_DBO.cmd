@echo off
echo ============================================
echo SIMPLE SOLUTION: Copy PRODUCTION_ESSENTIAL_SEED.sql
echo from dev database to production
echo ============================================
echo.
echo This approach:
echo   1. Uses existing PRODUCTION_ESSENTIAL_SEED.sql from Database_Scripts
echo   2. Runs it on production to create lookup tables
echo   3. Then imports the data we already exported
echo.
echo This is faster and more reliable than cursor-based export.
echo.
pause

echo.
echo Step 1: Running PRODUCTION_ESSENTIAL_SEED.sql on production...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Database_Scripts\PRODUCTION_ESSENTIAL_SEED.sql" -o "essential_seed_prod.log"

if %errorlevel% neq 0 (
    echo ERROR: Essential seed failed
    type essential_seed_prod.log
    pause
    exit /b 1
)

echo.
echo Step 2: Importing data (using previously exported PRODUCTION_DATA_CLEAN.sql)...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_DATA_CLEAN.sql" -o "rebuild_import.log"

if %errorlevel% neq 0 (
    echo ERROR: Data import failed
    type rebuild_import.log | findstr /C:"Msg" /C:"Error"
    pause
    exit /b 1
)

echo.
echo Step 3: Verifying counts...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Companies' AS Entity, COUNT(*) AS Count FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates;" -W

echo.
echo DONE! Now restart IIS and check the production site.
pause

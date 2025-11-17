@echo off
echo ============================================
echo COMPLETE DATABASE REFRESH - Dev to Production
echo ============================================
echo.
echo This will:
echo   1. Export ALL tables from dev (ANUKI01)
echo   2. Clear existing partial data from production
echo   3. Import complete dataset to production
echo.
echo Press Ctrl+C to cancel, or
pause

echo.
echo ============================================
echo STEP 1: Exporting ALL tables from dev...
echo ============================================
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev -i "export_all_tables_from_dev.sql" -o "COMPLETE_EXPORT.sql"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Export failed with error code %errorlevel%
    echo Check connection to ANUKI01
    pause
    exit /b 1
)

echo.
echo Cleaning export file (removing sqlcmd messages)...
powershell -Command "Get-Content 'COMPLETE_EXPORT.sql' | Select-Object -Skip 1 | Set-Content 'COMPLETE_EXPORT_CLEAN.sql'"

echo.
echo ============================================
echo STEP 2: Clearing partial data from production...
echo ============================================
echo.
echo WARNING: This will delete existing data from production!
echo Press Ctrl+C to cancel, or
pause

sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "DELETE FROM Certificates; DELETE FROM Products; DELETE FROM Companies; DELETE FROM Tags; DELETE FROM Partners; DELETE FROM CertificateFields; DELETE FROM CertificationTypes; DELETE FROM ProductTypes; DELETE FROM ProductCategories;" -o "clear_production.log"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Clear failed with error code %errorlevel%
    pause
    exit /b 1
)

echo.
echo ============================================
echo STEP 3: Importing complete dataset to production...
echo ============================================
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "COMPLETE_EXPORT_CLEAN.sql" -o "complete_import.log"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Import failed with error code %errorlevel%
    echo Check complete_import.log for details
    pause
    exit /b 1
)

echo.
echo ============================================
echo STEP 4: Verifying import...
echo ============================================
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'ProductCategories' AS Entity, COUNT(*) AS Count FROM ProductCategories UNION ALL SELECT 'ProductTypes', COUNT(*) FROM ProductTypes UNION ALL SELECT 'CertificationTypes', COUNT(*) FROM CertificationTypes UNION ALL SELECT 'Companies', COUNT(*) FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates;" -W

echo.
echo ============================================
echo IMPORT COMPLETE!
echo ============================================
echo.
echo Expected counts:
echo   - ProductCategories: ~6
echo   - ProductTypes: ~8
echo   - CertificationTypes: ~4
echo   - Companies: 28
echo   - Products: 39
echo   - Certificates: 80
echo.
echo IMPORTANT: Restart IIS to see data on website!
echo   - Contact AccuWebHosting support, OR
echo   - Use IIS Manager to recycle app pool
echo.
pause

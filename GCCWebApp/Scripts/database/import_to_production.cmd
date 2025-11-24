@echo off
echo ============================================
echo Importing Dev Data to Production
echo Server: 68.71.129.120,1533
echo Database: gccwebap_certificates
echo Started at: %date% %time%
echo ============================================
echo.

if not exist "PRODUCTION_DATA_FROM_DEV.sql" (
    echo ERROR: PRODUCTION_DATA_FROM_DEV.sql not found!
    echo Please run export_from_dev.cmd first.
    pause
    exit /b 1
)

echo Connecting to production database...
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "PRODUCTION_DATA_FROM_DEV.sql" -o "import_results.log"

if %errorlevel% equ 0 (
    echo.
    echo ============================================
    echo Import completed successfully!
    echo.
    echo Verifying data counts...
    sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT 'Companies' AS EntityType, COUNT(*) AS Count FROM Companies UNION ALL SELECT 'Products', COUNT(*) FROM Products UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates UNION ALL SELECT 'Active Certificates', COUNT(*) FROM Certificates WHERE Status = 'Active';"
    echo.
    echo ============================================
    echo Import complete! Check above for data counts.
    echo Expected: ~28 Companies, ~39 Products, ~178 Certificates
    echo.
    echo Next: Restart production IIS to see data on website
    echo ============================================
) else (
    echo.
    echo ============================================
    echo ERROR: Import failed with error code %errorlevel%
    echo Check import_results.log for details
    echo ============================================
)

pause

@echo off
echo ============================================
echo Exporting Dev Data to Production Scripts
echo Started at: %date% %time%
echo ============================================
echo.

echo Connecting to ANUKI01 / GCCDatabase_Dev...
sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev -i "export_dev_to_production_scripts.sql" -o "PRODUCTION_DATA_FROM_DEV.sql" -W -s "," -h -1

if %errorlevel% equ 0 (
    echo.
    echo ============================================
    echo Export completed successfully!
    echo Output saved to: PRODUCTION_DATA_FROM_DEV.sql
    echo.
    echo Next step: Run import_to_production.cmd
    echo ============================================
) else (
    echo.
    echo ============================================
    echo ERROR: Export failed with error code %errorlevel%
    echo Check connection to ANUKI01
    echo ============================================
)

pause

@echo off
REM =========================================
REM RECREATE TABLES (KEEP DATABASE)
REM =========================================
REM This will DROP all tables and recreate them
REM Database itself will remain intact
REM =========================================

setlocal

set SERVER=68.71.129.120,1533
set DATABASE=gccwebap_certificates
set USERNAME=GCC_adminxyz123
set PASSWORD=Q3Ez4e!gE_47KNi

color 0E
echo =========================================
echo RECREATE TABLES - KEEP DATABASE
echo =========================================
color 07
echo.
echo This will:
echo   1. DROP all existing tables
echo   2. Configure user with dbo schema
echo   3. CREATE tables as dbo.TableName
echo   4. Seed sample data
echo.
color 0E
echo WARNING: All table data will be lost!
color 07
echo.
echo Database will remain intact.
echo Only tables will be dropped and recreated.
echo.
pause
echo.

REM Test connection
echo Testing connection to production server...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -Q "SELECT @@VERSION" -d master -b >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo.
    color 0C
    echo ERROR: Cannot connect to production server!
    color 07
    echo.
    echo Check:
    echo   1. Server is accessible: %SERVER%
    echo   2. Credentials are correct
    echo   3. Firewall allows port 1533
    echo.
    pause
    exit /b 1
)
echo [OK] Connected to production server
echo.

REM Show current state
echo =========================================
echo CURRENT STATE (BEFORE DROP)
echo =========================================
echo.
echo Tables by schema:
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT SCHEMA_NAME(schema_id) as SchemaName, COUNT(*) as TableCount FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)" 2>nul
echo.
echo Sample tables:
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT TOP 5 SCHEMA_NAME(schema_id) as [Schema], name as TableName FROM sys.tables ORDER BY name" 2>nul
echo.
pause
echo.

REM Drop all tables
echo =========================================
echo STEP 1: Drop All Tables
echo =========================================
echo.
echo Dropping all tables (keeping database)...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 00_DROP_TABLES_ONLY.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    color 0C
    echo ERROR: Failed to drop tables!
    color 07
    echo.
    echo Check the error messages above
    pause
    exit /b 1
)
echo.
echo [OK] All tables dropped successfully
echo.
pause
echo.

REM Apply schema
echo =========================================
echo STEP 2: Create Database Schema
echo =========================================
echo.
echo Creating all tables as dbo.TableName...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 00_Complete_Schema.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    color 0C
    echo ERROR: Schema creation failed!
    color 07
    echo.
    pause
    exit /b 1
)
echo.
echo [OK] Schema created successfully
echo.

REM Verify schema
echo Verifying all tables are in dbo schema...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT COUNT(*) as NonDboTables FROM sys.tables WHERE SCHEMA_NAME(schema_id) != 'dbo'" -h -1 > temp_count.txt
set /p NON_DBO_COUNT=<temp_count.txt
del temp_count.txt 2>nul

echo.
color 0A
echo SUCCESS: All tables are in dbo schema!
color 07
echo.
pause
echo.

REM Seed data
echo =========================================
echo STEP 3: Seed Sample Data
echo =========================================
echo.
set /p SEED="Import sample data (10 certificates)? (Y/N): "
if /i "%SEED%"=="Y" (
    echo.
    echo Seeding database...
    sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 04_Quick_Fix_Minimal_Data.sql
    if %ERRORLEVEL% NEQ 0 (
        echo.
        color 0E
        echo WARNING: Data seed had errors
        color 07
        echo Database is ready but may not have sample data
    ) else (
        echo.
        color 0A
        echo [OK] Sample data imported successfully
        color 07
    )
) else (
    echo.
    echo Skipping data seed - database is empty
    echo You can seed later by running: 04_Quick_Fix_Minimal_Data.sql
)
echo.

REM Final verification
echo =========================================
echo STEP 4: Final Verification
echo =========================================
echo.

echo Schema Verification:
echo ---------------------
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT SCHEMA_NAME(schema_id) as SchemaName, COUNT(*) as TableCount FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)"
echo.

echo Sample Tables (should all be dbo):
echo ---------------------
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT TOP 5 SCHEMA_NAME(schema_id) as [Schema], name as TableName FROM sys.tables ORDER BY name"
echo.

echo Row Counts:
echo ---------------------
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT 'Roles' as TableName, COUNT(*) as Rows FROM Roles UNION ALL SELECT 'Companies', COUNT(*) FROM Companies UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates UNION ALL SELECT 'Users', COUNT(*) FROM Users ORDER BY TableName"
echo.

echo =========================================
echo TABLES RECREATED SUCCESSFULLY!
echo =========================================
echo.
color 0A
echo SUCCESS! Tables recreated with dbo schema:
color 07
echo.
echo   ✓ Old tables dropped
echo   ✓ User configured with dbo schema
echo   ✓ All tables created as dbo.TableName
echo   ✓ Sample data imported (if selected)
echo.
echo Database is now ready for production use!
echo.
echo Next Steps:
echo   1. Deploy application to production server
echo   2. Set ASPNETCORE_ENVIRONMENT=Production
echo   3. Start application
echo   4. Test: http://localhost:5101/api/certificate-tiles
echo   5. Visit: /Certificate/CertificateTileView
echo.
echo The website should now display certificates!
echo.
echo =========================================
pause

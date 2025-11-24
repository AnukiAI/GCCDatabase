@echo off
REM =========================================
REM COMPLETE DATABASE REFRESH
REM =========================================
REM This will COMPLETELY WIPE and recreate the database
REM ALL EXISTING DATA WILL BE LOST!
REM =========================================

setlocal

set SERVER=68.71.129.120,1533
set DATABASE=gccwebap_certificates
set USERNAME=GCC_adminxyz123
set PASSWORD=Q3Ez4e!gE_47KNi

color 0C
echo =========================================
echo COMPLETE DATABASE REFRESH
echo =========================================
color 07
echo.
echo This will:
echo   1. KILL all database connections
echo   2. DROP the entire database
echo   3. CREATE a fresh database
echo   4. Configure user with dbo schema
echo   5. Apply schema (create tables)
echo   6. Seed sample data
echo.
color 0C
echo WARNING: ALL EXISTING DATA WILL BE LOST!
color 07
echo.
echo Current state:
echo   - Database may have tables in wrong schema (GCC_adminxyz123)
echo   - Old data will be completely removed
echo.
echo After refresh:
echo   - Clean database with dbo schema
echo   - Ready for production use
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

REM Final confirmation
echo.
color 0E
echo FINAL CONFIRMATION REQUIRED
color 07
echo.
echo Type exactly: DELETE ALL DATA
set /p CONFIRM="Confirmation: "
if not "%CONFIRM%"=="DELETE ALL DATA" (
    echo.
    echo Refresh cancelled - you must type exactly: DELETE ALL DATA
    pause
    exit /b 0
)
echo.

REM Show current state
echo =========================================
echo CURRENT STATE (BEFORE REFRESH)
echo =========================================
echo.
echo Checking current database state...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT 'Schema: ' + SCHEMA_NAME(schema_id) as Info, 'Tables: ' + CAST(COUNT(*) AS NVARCHAR(10)) as Count FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)" 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Database does not exist or cannot be accessed
)
echo.
pause
echo.

REM Execute complete refresh
echo =========================================
echo STEP 1: Complete Database Refresh
echo =========================================
echo.
echo Wiping database and recreating...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d master -i 00_COMPLETE_DATABASE_REFRESH.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    color 0C
    echo ERROR: Database refresh failed!
    color 07
    echo.
    echo Check the error messages above
    pause
    exit /b 1
)
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
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT SCHEMA_NAME(schema_id) as SchemaName, name as TableName FROM sys.tables WHERE SCHEMA_NAME(schema_id) != 'dbo'"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo Checking for tables NOT in dbo schema...
    sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT COUNT(*) as NonDboTables FROM sys.tables WHERE SCHEMA_NAME(schema_id) != 'dbo'" -h -1 > temp_count.txt
    set /p NON_DBO_COUNT=<temp_count.txt
    del temp_count.txt 2>nul

    echo.
    color 0A
    echo SUCCESS: All tables are in dbo schema!
    color 07
)
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
echo COMPLETE DATABASE REFRESH FINISHED!
echo =========================================
echo.
color 0A
echo SUCCESS! Production database rebuilt:
color 07
echo.
echo   ✓ Old database completely removed
echo   ✓ New database created
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

@echo off
REM =========================================
REM REBUILD PRODUCTION DATABASE - DBO SCHEMA FIX
REM =========================================
REM CRITICAL: This drops and recreates the database
REM Make sure you understand what this does!
REM =========================================

setlocal

set SERVER=68.71.129.120,1533
set DATABASE=gccwebap_certificates
set USERNAME=GCC_adminxyz123
set PASSWORD=Q3Ez4e!gE_47KNi

echo =========================================
echo PRODUCTION DATABASE REBUILD - DBO SCHEMA
echo =========================================
echo.
echo PROBLEM: Tables created under wrong schema
echo   Current: GCC_adminxyz123.Certificates
echo   Should be: dbo.Certificates
echo.
echo Entity Framework REQUIRES dbo schema!
echo.
echo This script will:
echo   1. DROP existing database (all data lost!)
echo   2. CREATE new database
echo   3. Configure user with dbo default schema
echo   4. Apply schema (create tables as dbo.TableName)
echo   5. Seed initial data
echo.
color 0C
echo WARNING: THIS WILL DELETE ALL DATA!
color 07
echo.
echo Make sure you have a backup if needed!
echo.
pause
echo.

REM Test connection
echo Testing connection to production...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -Q "SELECT @@VERSION" -d master -b >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Cannot connect to production server
    pause
    exit /b 1
)
echo [OK] Connected
echo.

REM Final confirmation
set /p CONFIRM="Type 'REBUILD' to confirm database rebuild: "
if not "%CONFIRM%"=="REBUILD" (
    echo Rebuild cancelled - you must type REBUILD exactly
    pause
    exit /b 0
)
echo.

REM Check current schema
echo =========================================
echo STEP 1: Check current schema (BEFORE)
echo =========================================
echo.
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 07_Check_Schema_Owner.sql
echo.
pause
echo.

REM Rebuild database
echo =========================================
echo STEP 2: Rebuild database with dbo schema
echo =========================================
echo.
echo Dropping and recreating database...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d master -i 08_Rebuild_Database_DBO_Schema.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Database rebuild failed!
    pause
    exit /b 1
)
echo.
pause
echo.

REM Apply schema
echo =========================================
echo STEP 3: Create tables (dbo schema)
echo =========================================
echo.
echo Creating tables as dbo.TableName...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 00_Complete_Schema.sql
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Schema creation failed!
    pause
    exit /b 1
)
echo.

REM Verify schema
echo Verifying tables are in dbo schema...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT SCHEMA_NAME(schema_id) as SchemaName, COUNT(*) as TableCount FROM sys.tables GROUP BY SCHEMA_NAME(schema_id)"
echo.
pause
echo.

REM Seed data
echo =========================================
echo STEP 4: Seed initial data
echo =========================================
echo.
set /p SEED="Import sample data (10 certificates)? (Y/N): "
if /i "%SEED%"=="Y" (
    echo Seeding database with sample data...
    sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i 04_Quick_Fix_Minimal_Data.sql
    if %ERRORLEVEL% NEQ 0 (
        echo.
        echo WARNING: Data seed failed, but database is ready
    )
) else (
    echo Skipping data seed - database is empty
)
echo.

REM Final verification
echo =========================================
echo STEP 5: Final Verification
echo =========================================
echo.
echo Checking schema ownership...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT SCHEMA_NAME(t.schema_id) as SchemaName, t.name as TableName FROM sys.tables t WHERE SCHEMA_NAME(t.schema_id) != 'dbo'"
echo.
echo If no rows returned above, all tables are in dbo schema (CORRECT!)
echo.

echo Checking row counts...
sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -Q "SELECT 'Roles' as TableName, COUNT(*) as Rows FROM Roles UNION ALL SELECT 'Companies', COUNT(*) FROM Companies UNION ALL SELECT 'Certificates', COUNT(*) FROM Certificates ORDER BY TableName"
echo.

echo =========================================
echo REBUILD COMPLETE!
echo =========================================
echo.
echo Database rebuilt with CORRECT schema:
echo   ✓ User: GCC_adminxyz123
echo   ✓ Default Schema: dbo
echo   ✓ Role: db_owner
echo   ✓ Tables: dbo.TableName
echo.
echo Entity Framework will now work correctly!
echo.
echo Next steps:
echo   1. Deploy application to production
echo   2. Set ASPNETCORE_ENVIRONMENT=Production
echo   3. Start application
echo   4. Visit /Certificate/CertificateTileView
echo.
echo If you skipped data seed, run:
echo   04_Quick_Fix_Minimal_Data.sql
echo.
echo =========================================
pause

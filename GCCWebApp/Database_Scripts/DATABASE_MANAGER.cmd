@echo off
REM ============================================
REM GCC Database Management Menu
REM ============================================

:MENU
cls
echo ============================================
echo GCC DATABASE MANAGEMENT MENU
echo ============================================
echo.
echo Target: gccwebap_certificates (Production Test)
echo Server: 68.71.129.120,1533
echo.
echo Choose an option:
echo.
echo 1. QUICK RESEED (Clean data + Reseed)
echo    - Keeps schema intact
echo    - Deletes all data
echo    - Reseeds fresh data
echo    - Safe, fast, reversible
echo.
echo 2. FULL REBUILD (Drop + Schema + Seed)
echo    - ⚠️  DESTRUCTIVE - Drops everything
echo    - Recreates schema from scratch
echo    - Reseeds fresh data
echo    - Use for testing schema changes
echo.
echo 3. CLEAN DATA ONLY (No reseed)
echo    - Keeps schema intact
echo    - Deletes all data
echo    - Resets identity seeds
echo    - Empty database ready for manual data
echo.
echo 4. SEED DATA ONLY (No clean)
echo    - Keeps existing data
echo    - Adds test data
echo    - May cause duplicate errors
echo.
echo 5. EXIT
echo.
echo ============================================

set /p choice="Enter choice (1-5): "

if "%choice%"=="1" goto QUICK_RESEED
if "%choice%"=="2" goto FULL_REBUILD
if "%choice%"=="3" goto CLEAN_ONLY
if "%choice%"=="4" goto SEED_ONLY
if "%choice%"=="5" goto EXIT
echo Invalid choice. Please try again.
timeout /t 2
goto MENU

:QUICK_RESEED
cls
echo ============================================
echo QUICK RESEED - Clean Data + Reseed
echo ============================================
echo.
call RESEED_DATABASE_COMPLETE.cmd
pause
goto MENU

:FULL_REBUILD
cls
echo ============================================
echo FULL REBUILD - Drop + Schema + Seed
echo ============================================
echo.
call REBUILD_DATABASE_COMPLETE.cmd
pause
goto MENU

:CLEAN_ONLY
cls
echo ============================================
echo CLEAN DATA ONLY
echo ============================================
echo.
SET SERVER=68.71.129.120,1533
SET DATABASE=gccwebap_certificates
SET USERNAME=GCC_adminxyz123
SET PASSWORD=Q3Ez4e!gE_47KNi

echo This will delete all data but keep the schema.
echo Press Ctrl+C to cancel, or
pause

sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i CLEAN_ALL_DATA.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Data cleanup failed!
    pause
) else (
    echo.
    echo ✅ SUCCESS: All data cleaned!
    echo Database is now empty and ready.
    pause
)
goto MENU

:SEED_ONLY
cls
echo ============================================
echo SEED DATA ONLY
echo ============================================
echo.
SET SERVER=68.71.129.120,1533
SET DATABASE=gccwebap_certificates
SET USERNAME=GCC_adminxyz123
SET PASSWORD=Q3Ez4e!gE_47KNi

echo This will add test data without cleaning.
echo May cause duplicate key errors if data exists.
echo Press Ctrl+C to cancel, or
pause

sqlcmd -S %SERVER% -U %USERNAME% -P %PASSWORD% -d %DATABASE% -i FULL_TEST_DATA_SEED.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Data seeding failed!
    echo This is normal if data already exists.
    pause
) else (
    echo.
    echo ✅ SUCCESS: Test data seeded!
    pause
)
goto MENU

:EXIT
echo.
echo Exiting...
exit /b 0

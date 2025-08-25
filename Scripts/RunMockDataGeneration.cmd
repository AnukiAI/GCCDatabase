@echo off
REM ================================================================
REM GCC Database Solution - Mock Data Generation Script
REM ================================================================
REM This script generates comprehensive mock data for the GCC Web Application
REM Usage: RunMockDataGeneration.cmd [option]
REM Options:
REM   small    - Generate small dataset (50 companies, 100 products, 300 certificates)
REM   medium   - Generate medium dataset (100 companies, 200 products, 600 certificates)  
REM   large    - Generate large dataset (150 companies, 250 products, 1000 certificates)
REM   reset    - Clear existing data and regenerate large dataset
REM ================================================================

echo.
echo ================================================================
echo    GCC Database Solution - Mock Data Generator
echo ================================================================
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Set default parameters
set COMPANIES=150
set PRODUCTS=250
set CERTIFICATES=1000
set USERS=75
set RESET_DATA=false

REM Parse command line argument
if "%1"=="small" (
    set COMPANIES=50
    set PRODUCTS=100
    set CERTIFICATES=300
    set USERS=25
    echo Selected: SMALL dataset
) else if "%1"=="medium" (
    set COMPANIES=100
    set PRODUCTS=200
    set CERTIFICATES=600
    set USERS=50
    echo Selected: MEDIUM dataset
) else if "%1"=="large" (
    set COMPANIES=150
    set PRODUCTS=250
    set CERTIFICATES=1000
    set USERS=75
    echo Selected: LARGE dataset
) else if "%1"=="reset" (
    set COMPANIES=150
    set PRODUCTS=250
    set CERTIFICATES=1000
    set USERS=75
    set RESET_DATA=true
    echo Selected: RESET and regenerate LARGE dataset
) else (
    set COMPANIES=150
    set PRODUCTS=250
    set CERTIFICATES=1000
    set USERS=75
    echo Selected: DEFAULT (large) dataset
)

echo.
echo Dataset Configuration:
echo   Companies:    %COMPANIES%
echo   Products:     %PRODUCTS%
echo   Certificates: %CERTIFICATES%
echo   Users:        %USERS%
echo   Reset Data:   %RESET_DATA%
echo.

REM Check if application is running
echo Checking if application is currently running...
tasklist | findstr "GCCWebApp.exe" >nul
if not errorlevel 1 (
    echo WARNING: GCCWebApp.exe is currently running
    echo Please stop the application before running data generation
    echo.
    choice /C YN /M "Do you want to continue anyway (may cause database conflicts)"
    if errorlevel 2 goto :cancel
)

REM Backup existing database
echo.
echo Creating database backup...
if exist "gcc_database.db" (
    copy "gcc_database.db" "gcc_database_backup_%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%%time:~6,2%.db" >nul
    if errorlevel 1 (
        echo WARNING: Could not create database backup
    ) else (
        echo Database backup created successfully
    )
)

echo.
echo ================================================================
echo Starting Mock Data Generation...
echo ================================================================
echo.

REM Record start time
set START_TIME=%time%

REM Run the data generation
echo Executing: dotnet run -- generate-mock-data --companies %COMPANIES% --products %PRODUCTS% --certificates %CERTIFICATES% --users %USERS% --reset %RESET_DATA%
echo.

dotnet run -- generate-mock-data --companies %COMPANIES% --products %PRODUCTS% --certificates %CERTIFICATES% --users %USERS% --reset %RESET_DATA%

REM Check exit code
if errorlevel 1 (
    echo.
    echo ================================================================
    echo ERROR: Mock data generation failed!
    echo ================================================================
    echo.
    echo Possible causes:
    echo   1. Database is locked (application still running)
    echo   2. Insufficient disk space
    echo   3. Database corruption
    echo   4. Missing dependencies
    echo.
    echo Solutions:
    echo   1. Stop all instances of GCCWebApp
    echo   2. Check available disk space
    echo   3. Try running with administrator privileges
    echo   4. Check the application logs for detailed error information
    echo.
    goto :error
)

REM Record end time and calculate duration
set END_TIME=%time%

echo.
echo ================================================================
echo Mock Data Generation Completed Successfully!
echo ================================================================
echo.
echo Generation Details:
echo   Start Time: %START_TIME%
echo   End Time:   %END_TIME%
echo   Companies Generated:    %COMPANIES%
echo   Products Generated:     %PRODUCTS%  
echo   Certificates Generated: %CERTIFICATES%
echo   Users Generated:        %USERS%
echo.

REM Show database size
if exist "gcc_database.db" (
    for %%A in ("gcc_database.db") do (
        echo Database Size: %%~zA bytes
    )
)

echo.
echo Next Steps:
echo   1. Start the GCC Web Application: dotnet run
echo   2. Open browser to: http://localhost:5101
echo   3. Login with admin credentials to verify data
echo   4. Test search, filtering, and export functionality
echo.

REM Ask if user wants to start the application
choice /C YN /M "Do you want to start the GCC Web Application now"
if not errorlevel 2 (
    echo.
    echo Starting GCC Web Application...
    start cmd /k "dotnet run"
    timeout /t 3 /nobreak >nul
    echo.
    echo Application starting in new window...
    echo Open browser to: http://localhost:5101
    echo.
)

goto :end

:cancel
echo.
echo Mock data generation cancelled by user.
goto :end

:error
echo.
echo Mock data generation failed. Check logs for details.
pause
exit /b 1

:end
echo.
echo Script completed.
pause
exit /b 0
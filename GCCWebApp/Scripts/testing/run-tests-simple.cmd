@echo off
echo ========================================
echo GCC Web Application - Test Runner
echo ========================================
echo.

REM Kill existing processes
echo Cleaning up processes...
taskkill /IM dotnet.exe /F 2>nul
taskkill /IM testhost.exe /F 2>nul
timeout /t 2 /nobreak >nul

REM Create results directory
set TIMESTAMP=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%
set RESULTS_DIR=TestResults_%TIMESTAMP%
mkdir %RESULTS_DIR% 2>nul

echo Results will be saved to: %RESULTS_DIR%
echo.

REM Run Unit Tests
echo ========================================
echo Running: GCCWebApp.Tests (Unit Tests)
echo ========================================
cd ..\GCCWebApp.Tests
dotnet restore --verbosity quiet
dotnet build --no-restore --verbosity quiet
dotnet test --no-build --logger "trx;LogFileName=..\GCCWebApp\%RESULTS_DIR%\UnitTests.trx" --logger "console;verbosity=normal"
echo.

REM Run API Tests
echo ========================================
echo Running: GCCWebApp.Api.Tests
echo ========================================
cd ..\GCCWebApp.Api.Tests
dotnet restore --verbosity quiet
dotnet build --no-restore --verbosity quiet
dotnet test --no-build --logger "trx;LogFileName=..\GCCWebApp\%RESULTS_DIR%\ApiTests.trx" --logger "console;verbosity=normal"
echo.

REM Run UAT Tests
echo ========================================
echo Running: GCCWebApp.UAT.Tests
echo ========================================
cd ..\GCCWebApp.UAT.Tests
dotnet restore --verbosity quiet
dotnet build --no-restore --verbosity quiet
dotnet test --no-build --logger "trx;LogFileName=..\GCCWebApp\%RESULTS_DIR%\UATTests.trx" --logger "console;verbosity=normal"
echo.

REM Run Security Tests
echo ========================================
echo Running: GCCWebApp.Security.Tests
echo ========================================
cd ..\GCCWebApp.Security.Tests
dotnet restore --verbosity quiet
dotnet build --no-restore --verbosity quiet
dotnet test --no-build --logger "trx;LogFileName=..\GCCWebApp\%RESULTS_DIR%\SecurityTests.trx" --logger "console;verbosity=normal"
echo.

REM Run Performance Tests
echo ========================================
echo Running: GCCWebApp.Performance.Tests
echo ========================================
cd ..\GCCWebApp.Performance.Tests
dotnet restore --verbosity quiet
dotnet build --no-restore --verbosity quiet
dotnet test --no-build --logger "trx;LogFileName=..\GCCWebApp\%RESULTS_DIR%\PerformanceTests.trx" --logger "console;verbosity=normal"
echo.

cd ..\GCCWebApp
echo ========================================
echo Test Execution Complete!
echo ========================================
echo.
echo Results saved to: %RESULTS_DIR%
echo.
pause
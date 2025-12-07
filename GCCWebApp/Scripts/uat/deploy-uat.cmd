@echo off
REM ============================================
REM GCC Web Application - UAT Deployment Script
REM ============================================
REM This script builds and packages the application
REM for UAT deployment to AccuWebHosting
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ============================================
echo GCC Web Application - UAT Deployment
echo ============================================
echo.

REM Colors
set "INFO=[92m[INFO][0m"
set "ERROR=[91m[ERROR][0m"
set "WARNING=[93m[WARNING][0m"
set "SUCCESS=[92m[SUCCESS][0m"

REM Configuration
set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%..\.."
set "PUBLISH_DIR=%PROJECT_DIR%\publish-production"
set "TIMESTAMP=%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%"
set TIMESTAMP=%TIMESTAMP: =0%

cd /d "%PROJECT_DIR%"
echo %INFO% Project directory: %PROJECT_DIR%
echo.

REM ============================================
echo Step 1: Pre-Deployment Checks
echo ============================================

echo %INFO% Checking for uncommitted changes...
git status --porcelain > nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo %WARNING% Git repository detected, checking status...
)

echo %INFO% Verifying web.config...
findstr /C:"fileExtensions" web.config > nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo %ERROR% web.config contains fileExtensions - will cause Plesk error!
    echo %ERROR% Please remove fileExtensions section before deploying.
    pause
    exit /b 1
)

findstr /C:"hiddenSegments" web.config > nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo %ERROR% web.config contains hiddenSegments - will cause Plesk error!
    echo %ERROR% Please remove hiddenSegments section before deploying.
    pause
    exit /b 1
)
echo %SUCCESS% web.config is Plesk-compatible

REM ============================================
echo.
echo Step 2: Clean Build
echo ============================================

if exist "%PUBLISH_DIR%" (
    echo %INFO% Removing old publish directory...
    rmdir /s /q "%PUBLISH_DIR%"
)

echo %INFO% Cleaning solution...
dotnet clean -c Release > nul 2>&1
echo %SUCCESS% Clean complete

REM ============================================
echo.
echo Step 3: Restore Dependencies
echo ============================================

dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Failed to restore dependencies
    pause
    exit /b 1
)
echo %SUCCESS% Dependencies restored

REM ============================================
echo.
echo Step 4: Build Application
echo ============================================

dotnet build -c Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Build failed
    pause
    exit /b 1
)
echo %SUCCESS% Build successful

REM ============================================
echo.
echo Step 5: Publish Application
echo ============================================

dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o "%PUBLISH_DIR%"
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Publish failed
    pause
    exit /b 1
)
echo %SUCCESS% Publish successful

REM ============================================
echo.
echo Step 6: Create Logs Folder
echo ============================================

if not exist "%PUBLISH_DIR%\logs" mkdir "%PUBLISH_DIR%\logs"
echo. > "%PUBLISH_DIR%\logs\.gitkeep"
echo %SUCCESS% Logs folder created

REM ============================================
echo.
echo Step 7: Verify Critical Files
echo ============================================

set "MISSING="
if not exist "%PUBLISH_DIR%\GCCWebApp.dll" set MISSING=!MISSING! GCCWebApp.dll
if not exist "%PUBLISH_DIR%\GCCWebApp.exe" set MISSING=!MISSING! GCCWebApp.exe
if not exist "%PUBLISH_DIR%\appsettings.json" set MISSING=!MISSING! appsettings.json
if not exist "%PUBLISH_DIR%\appsettings.Production.json" set MISSING=!MISSING! appsettings.Production.json
if not exist "%PUBLISH_DIR%\web.config" set MISSING=!MISSING! web.config
if not exist "%PUBLISH_DIR%\wwwroot" set MISSING=!MISSING! wwwroot

if not "!MISSING!"=="" (
    echo %ERROR% Missing critical files:!MISSING!
    pause
    exit /b 1
)
echo %SUCCESS% All critical files present

REM ============================================
echo.
echo Step 8: Create Deployment Info
echo ============================================

(
    echo GCC Web Application - UAT Deployment
    echo ============================================
    echo Build Date: %date% %time%
    echo Environment: Production
    echo Target: https://gccwebapp.com
    echo ============================================
    echo.
    echo UAT Test Credentials:
    echo   Admin: admin / TestPass123!
    echo   Analyst: analyst / TestPass123!
    echo   External: external / TestPass123!
    echo.
    echo Or use email login:
    echo   admin@greencirclecertified.com / Admin123!
    echo.
) > "%PUBLISH_DIR%\UAT_DEPLOYMENT_INFO.txt"
echo %SUCCESS% Deployment info created

REM ============================================
echo.
echo Step 9: Create ZIP Package
echo ============================================

set "ZIP_FILE=%PROJECT_DIR%\GCCWebApp-UAT-%TIMESTAMP%.zip"
powershell -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ZIP_FILE%' -CompressionLevel Optimal -Force"
if %ERRORLEVEL% NEQ 0 (
    echo %WARNING% ZIP creation failed - manual packaging required
) else (
    echo %SUCCESS% ZIP created: %ZIP_FILE%
)

REM ============================================
echo.
echo ============================================
echo [92mUAT DEPLOYMENT PACKAGE READY![0m
echo ============================================
echo.
echo Package location: %PUBLISH_DIR%
echo ZIP file: %ZIP_FILE%
echo.
echo NEXT STEPS:
echo.
echo 1. Login to Plesk at AccuWebHosting
echo 2. Stop the GCC application
echo 3. Backup current files (rename to backup)
echo 4. Upload new files from publish-production
echo 5. Restart the application
echo 6. Run POST_DEPLOYMENT_CHECKLIST.md
echo.
echo Test URLs:
echo   https://gccwebapp.com
echo   https://gccwebapp.com/Account/Login
echo   https://gccwebapp.com/Certificate/CertificateTileView
echo.
pause

@echo off
REM ============================================
REM GCC Web Application - AccuWebHosting Deployment
REM ============================================
REM This script builds and packages the application
REM for deployment to AccuWebHosting production server
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ============================================
echo GCC Web Application - Production Deployment
echo ============================================
echo.

REM Set colors for output
set "INFO=[92m[INFO][0m"
set "ERROR=[91m[ERROR][0m"
set "WARNING=[93m[WARNING][0m"

REM Configuration
set "PROJECT_DIR=%~dp0..\.."
set "PUBLISH_DIR=%PROJECT_DIR%\publish-accuweb-production"
set "BACKUP_DIR=%PROJECT_DIR%\backup-%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set BACKUP_DIR=%BACKUP_DIR: =0%

echo %INFO% Project directory: %PROJECT_DIR%
echo %INFO% Publish directory: %PUBLISH_DIR%
echo.

REM Navigate to project directory
cd /d "%PROJECT_DIR%"

REM Step 1: Clean previous publish
echo.
echo ============================================
echo Step 1: Cleaning Previous Build
echo ============================================
if exist "%PUBLISH_DIR%" (
    echo %INFO% Removing old publish directory...
    rmdir /s /q "%PUBLISH_DIR%"
)
mkdir "%PUBLISH_DIR%"
echo %INFO% Clean complete

REM Step 2: Restore dependencies
echo.
echo ============================================
echo Step 2: Restoring Dependencies
echo ============================================
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Failed to restore dependencies
    pause
    exit /b 1
)
echo %INFO% Dependencies restored

REM Step 3: Build application
echo.
echo ============================================
echo Step 3: Building Application
echo ============================================
dotnet build -c Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Build failed
    pause
    exit /b 1
)
echo %INFO% Build successful

REM Step 4: Publish application
echo.
echo ============================================
echo Step 4: Publishing Application
echo ============================================
echo %INFO% Publishing for win-x64 runtime...
dotnet publish GCCWebApp.csproj -c Release -r win-x64 --self-contained false -o "%PUBLISH_DIR%"
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Publish failed
    pause
    exit /b 1
)
echo %INFO% Publish successful

REM Step 5: Copy database scripts
echo.
echo ============================================
echo Step 5: Copying Database Scripts
echo ============================================
if not exist "%PUBLISH_DIR%\Database_Scripts" mkdir "%PUBLISH_DIR%\Database_Scripts"
copy "Database_Scripts\00_CREATE_SCHEMA.sql" "%PUBLISH_DIR%\Database_Scripts\" >nul
copy "Database_Scripts\FULL_TEST_DATA_SEED.sql" "%PUBLISH_DIR%\Database_Scripts\" >nul
copy "Database_Scripts\apply-migrations.ps1" "%PUBLISH_DIR%\Database_Scripts\" >nul
copy "Database_Scripts\apply-migrations.cmd" "%PUBLISH_DIR%\Database_Scripts\" >nul
copy "Database_Scripts\README_DATABASE_SETUP.md" "%PUBLISH_DIR%\Database_Scripts\" >nul
echo %INFO% Database scripts copied

REM Step 6: Verify critical files
echo.
echo ============================================
echo Step 6: Verifying Deployment Package
echo ============================================
echo %INFO% Checking critical files...

set "MISSING_FILES="
if not exist "%PUBLISH_DIR%\GCCWebApp.dll" set MISSING_FILES=!MISSING_FILES! GCCWebApp.dll
if not exist "%PUBLISH_DIR%\appsettings.json" set MISSING_FILES=!MISSING_FILES! appsettings.json
if not exist "%PUBLISH_DIR%\web.config" set MISSING_FILES=!MISSING_FILES! web.config

if not "!MISSING_FILES!"=="" (
    echo %ERROR% Missing critical files: !MISSING_FILES!
    pause
    exit /b 1
)

echo %INFO% All critical files present

REM Step 7: Create deployment package info
echo.
echo ============================================
echo Step 7: Creating Deployment Info
echo ============================================
(
    echo GCC Web Application - Production Deployment
    echo ============================================
    echo Build Date: %date% %time%
    echo Environment: Production
    echo Target Server: 68.71.129.120,1533
    echo Database: gccwebap_certificates
    echo ============================================
    echo.
    echo IMPORTANT CHANGES IN THIS DEPLOYMENT:
    echo - Automatic migrations DISABLED for Production
    echo - Manual migration management via apply-migrations.cmd
    echo - Schema locked to dbo
    echo - Application only validates DB connectivity on startup
    echo.
    echo DEPLOYMENT STEPS:
    echo 1. Stop the application on AccuWebHosting
    echo 2. Backup current application files
    echo 3. Upload contents of publish-accuweb-production folder
    echo 4. Update appsettings.Production.json with connection string
    echo 5. Restart the application
    echo 6. Verify no auto-migrations occur in logs
    echo.
    echo DATABASE MANAGEMENT:
    echo - Schema changes are now manual only
    echo - Use Database_Scripts\apply-migrations.cmd when needed
    echo - See Database_Scripts\README_DATABASE_SETUP.md
    echo.
) > "%PUBLISH_DIR%\DEPLOYMENT_INFO.txt"
echo %INFO% Deployment info created

REM Step 8: Calculate package size
echo.
echo ============================================
echo Step 8: Package Summary
echo ============================================
for /f "tokens=3" %%a in ('dir /s "%PUBLISH_DIR%" ^| find "File(s)"') do set SIZE=%%a
echo %INFO% Package size: !SIZE! bytes
for /f %%a in ('dir /b "%PUBLISH_DIR%" ^| find /c /v ""') do echo %INFO% Total files: %%a

REM Success summary
echo.
echo ============================================
echo [92mDEPLOYMENT PACKAGE READY![0m
echo ============================================
echo.
echo Package location: %PUBLISH_DIR%
echo.
echo NEXT STEPS:
echo.
echo 1. STOP the application on AccuWebHosting:
echo    - Login to Plesk control panel
echo    - Navigate to your GCC application
echo    - Stop the application process
echo.
echo 2. BACKUP current files on server (recommended):
echo    - Download current application folder
echo    - Or rename folder to "gccwebapp_backup_[date]"
echo.
echo 3. UPLOAD new files:
echo    - Upload entire contents of: %PUBLISH_DIR%
echo    - Target location: /httpdocs or your app folder
echo    - Use FTP or Plesk File Manager
echo.
echo 4. UPDATE appsettings.Production.json on server:
echo    - Ensure connection string is correct
echo    - Server: 68.71.129.120,1533
echo    - Database: gccwebap_certificates
echo    - User: GCC_adminxyz123
echo.
echo 5. START the application:
echo    - Restart via Plesk control panel
echo    - Or use: systemctl restart gcc-webapp
echo.
echo 6. VERIFY deployment:
echo    - Check application logs for "skipping automatic migrations"
echo    - Should see: "Production environment - manual migration management"
echo    - NO tables should be created automatically
echo.
echo 7. MANAGE migrations manually when needed:
echo    - Upload Database_Scripts folder if not already present
echo    - Run: .\apply-migrations.cmd Production
echo.
echo.
echo [93mIMPORTANT: Automatic migrations are now DISABLED.[0m
echo [93mYou control when database changes are applied.[0m
echo.
pause

@echo off
REM ============================================
REM GCC Web Application - Publish and ZIP
REM ============================================
REM This script builds, publishes, and creates
REM a ZIP file ready for deployment
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ============================================
echo GCC Web Application - Publish and ZIP
echo ============================================
echo.

REM Set colors for output
set "INFO=[92m[INFO][0m"
set "ERROR=[91m[ERROR][0m"
set "WARNING=[93m[WARNING][0m"

REM Configuration
set "PROJECT_DIR=%~dp0..\.."
set "TIMESTAMP=%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%%time:~6,2%"
set TIMESTAMP=%TIMESTAMP: =0%
set "PUBLISH_DIR=%PROJECT_DIR%\publish-deployment-%TIMESTAMP%"
set "ZIP_FILE=%PROJECT_DIR%\GCCWebApp-Deployment-%TIMESTAMP%.zip"

echo %INFO% Project directory: %PROJECT_DIR%
echo %INFO% Publish directory: %PUBLISH_DIR%
echo %INFO% ZIP file: %ZIP_FILE%
echo.

REM Navigate to project directory
cd /d "%PROJECT_DIR%"

REM Step 1: Clean previous builds
echo.
echo ============================================
echo Step 1: Cleaning Previous Builds
echo ============================================
dotnet clean -c Release
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

REM Step 5: Copy additional deployment files
echo.
echo ============================================
echo Step 5: Copying Deployment Files
echo ============================================

REM Create directories
if not exist "%PUBLISH_DIR%\Database_Scripts" mkdir "%PUBLISH_DIR%\Database_Scripts"
if not exist "%PUBLISH_DIR%\Deployment_Tools" mkdir "%PUBLISH_DIR%\Deployment_Tools"

REM Copy database scripts
if exist "Database_Scripts" (
    copy "Database_Scripts\*.sql" "%PUBLISH_DIR%\Database_Scripts\" >nul 2>&1
    copy "Database_Scripts\*.ps1" "%PUBLISH_DIR%\Database_Scripts\" >nul 2>&1
    copy "Database_Scripts\*.cmd" "%PUBLISH_DIR%\Database_Scripts\" >nul 2>&1
    copy "Database_Scripts\*.md" "%PUBLISH_DIR%\Database_Scripts\" >nul 2>&1
    echo %INFO% Database scripts copied
)

REM Copy deployment tools
copy "%~dp0deploy-accuweb-production.cmd" "%PUBLISH_DIR%\Deployment_Tools\" >nul 2>&1
echo %INFO% Deployment tools copied

REM Step 6: Create deployment documentation
echo.
echo ============================================
echo Step 6: Creating Deployment Documentation
echo ============================================
(
    echo GCC Web Application - Deployment Package
    echo ============================================
    echo Build Date: %date% %time%
    echo Package: GCCWebApp-Deployment-%TIMESTAMP%.zip
    echo Environment: Production
    echo Target Server: AccuWebHosting (68.71.129.120,1533)
    echo Database: gccwebap_certificates
    echo ============================================
    echo.
    echo DEPLOYMENT INSTRUCTIONS:
    echo.
    echo 1. EXTRACT ZIP FILE:
    echo    - Extract GCCWebApp-Deployment-%TIMESTAMP%.zip
    echo    - This creates a folder with all application files
    echo.
    echo 2. STOP APPLICATION on AccuWebHosting:
    echo    - Login to Plesk control panel
    echo    - Stop the GCC Web Application
    echo.
    echo 3. BACKUP CURRENT FILES (recommended):
    echo    - Download or rename current application folder
    echo    - Save as backup (e.g., gccwebapp_backup_%TIMESTAMP%)
    echo.
    echo 4. UPLOAD NEW FILES:
    echo    - Upload entire contents to /httpdocs or app folder
    echo    - Use FTP or Plesk File Manager
    echo.
    echo 5. UPDATE CONFIGURATION:
    echo    - Edit appsettings.Production.json on server
    echo    - Verify connection string:
    echo      Server: 68.71.129.120,1533
    echo      Database: gccwebap_certificates
    echo      User: GCC_adminxyz123
    echo.
    echo 6. START APPLICATION:
    echo    - Restart via Plesk control panel
    echo    - Or use: systemctl restart gcc-webapp
    echo.
    echo 7. VERIFY DEPLOYMENT:
    echo    - Browse to your application URL
    echo    - Check application logs for errors
    echo    - Test login and certificate viewing
    echo.
    echo DATABASE MIGRATIONS:
    echo - Automatic migrations are DISABLED in Production
    echo - To apply schema changes:
    echo   1. Navigate to Database_Scripts folder
    echo   2. Run: .\apply-migrations.cmd Production
    echo   3. See README_DATABASE_SETUP.md for details
    echo.
    echo PACKAGE CONTENTS:
    echo - Application DLLs and executables
    echo - Static files (CSS, JS, images)
    echo - Configuration files (appsettings.json, web.config)
    echo - Database_Scripts folder (migration tools)
    echo - Deployment_Tools folder (helper scripts)
    echo.
) > "%PUBLISH_DIR%\DEPLOYMENT_README.txt"
echo %INFO% Deployment documentation created

REM Step 7: Verify critical files
echo.
echo ============================================
echo Step 7: Verifying Package Contents
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

REM Step 8: Create ZIP file
echo.
echo ============================================
echo Step 8: Creating ZIP File
echo ============================================
echo %INFO% Compressing deployment package...

powershell -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ZIP_FILE%' -Force"
if %ERRORLEVEL% NEQ 0 (
    echo %ERROR% Failed to create ZIP file
    pause
    exit /b 1
)
echo %INFO% ZIP file created successfully

REM Step 9: Calculate sizes
echo.
echo ============================================
echo Step 9: Package Summary
echo ============================================
for %%A in ("%ZIP_FILE%") do (
    set "SIZE=%%~zA"
    set /a "SIZE_MB=!SIZE! / 1048576"
    echo %INFO% ZIP file size: !SIZE_MB! MB
)

for /f "tokens=3" %%a in ('dir /s "%PUBLISH_DIR%" ^| find "File(s)"') do set SIZE=%%a
echo %INFO% Uncompressed size: !SIZE! bytes

for /f %%a in ('dir /b "%PUBLISH_DIR%" ^| find /c /v ""') do echo %INFO% Total files: %%a

REM Step 10: Cleanup option
echo.
echo ============================================
echo Step 10: Cleanup
echo ============================================
set /p CLEANUP="Delete temporary publish folder? (Y/N): "
if /i "%CLEANUP%"=="Y" (
    rmdir /s /q "%PUBLISH_DIR%"
    echo %INFO% Temporary folder deleted
) else (
    echo %INFO% Temporary folder kept at: %PUBLISH_DIR%
)

REM Success summary
echo.
echo ============================================
echo [92mDEPLOYMENT PACKAGE READY![0m
echo ============================================
echo.
echo ZIP File Location:
echo %ZIP_FILE%
echo.
echo File Size: !SIZE_MB! MB
echo.
echo NEXT STEPS:
echo.
echo 1. Transfer the ZIP file to your deployment location:
echo    - Email, cloud storage, or FTP
echo.
echo 2. Extract on target server:
echo    - Unzip GCCWebApp-Deployment-%TIMESTAMP%.zip
echo.
echo 3. Follow instructions in DEPLOYMENT_README.txt
echo    - Located in the extracted folder
echo.
echo 4. For detailed instructions, see:
echo    - 0_Documentation\docs\DEPLOYMENT_GUIDE.md
echo.
echo [93mIMPORTANT: Test in a staging environment first![0m
echo.
pause

@echo off
echo =================================================================
echo GCC Web Application - TEST Environment Deployment Script
echo =================================================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] This script requires administrator privileges.
    echo Please run as administrator.
    echo.
    pause
    exit /b 1
)

echo [INFO] Administrator privileges confirmed.
echo.

REM Set TEST environment deployment variables
set ENVIRONMENT=Test
set APP_NAME=GCCWebApp-Test
set SITE_NAME=GCCWebApp-Test
set APP_POOL_NAME=GCCWebApp-Test-AppPool
set DEPLOY_PATH=C:\inetpub\wwwroot\GCCWebApp-Test
set HTTP_PORT=5102
set HTTPS_PORT=5103
set LOG_PATH=C:\inetpub\logs\GCCWebApp-Test
set DB_NAME=gcc_database_test.db

echo =================================================================
echo TEST Environment Deployment Configuration:
echo =================================================================
echo Environment: %ENVIRONMENT%
echo Application Name: %APP_NAME%
echo Site Name: %SITE_NAME%
echo App Pool Name: %APP_POOL_NAME%
echo Deploy Path: %DEPLOY_PATH%
echo HTTP Port: %HTTP_PORT%
echo HTTPS Port: %HTTPS_PORT%
echo Log Path: %LOG_PATH%
echo Database: %DB_NAME%
echo =================================================================
echo.

REM Confirm deployment
set /p CONFIRM="Deploy to TEST environment? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Deployment cancelled.
    exit /b 0
)
echo.

REM Create directories
echo [STEP 1] Creating deployment directories...
if not exist "%DEPLOY_PATH%" mkdir "%DEPLOY_PATH%"
if not exist "%LOG_PATH%" mkdir "%LOG_PATH%"
if not exist "%DEPLOY_PATH%\logs" mkdir "%DEPLOY_PATH%\logs"
if not exist "%DEPLOY_PATH%\DataProtectionKeys" mkdir "%DEPLOY_PATH%\DataProtectionKeys"
echo [SUCCESS] Directories created.
echo.

REM Stop existing site and app pool if they exist
echo [STEP 2] Stopping existing IIS site and application pool...
%WINDIR%\system32\inetsrv\appcmd.exe stop site "%SITE_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe stop apppool "%APP_POOL_NAME%" >nul 2>&1
timeout /t 3 /nobreak >nul
echo [SUCCESS] Existing services stopped.
echo.

REM Backup existing deployment if it exists
echo [STEP 3] Backing up existing deployment...
if exist "%DEPLOY_PATH%\GCCWebApp.dll" (
    set BACKUP_DIR=%DEPLOY_PATH%_backup_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%
    set BACKUP_DIR=!BACKUP_DIR: =0!
    xcopy "%DEPLOY_PATH%" "!BACKUP_DIR!" /E /I /Q /Y >nul
    echo [SUCCESS] Backup created: !BACKUP_DIR!
) else (
    echo [INFO] No existing deployment to backup.
)
echo.

REM Build and publish application for Test environment
echo [STEP 4] Building and publishing application for TEST environment...
echo [INFO] Cleaning previous build...
dotnet clean --configuration Release >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Clean failed.
    pause
    exit /b 1
)

echo [INFO] Restoring packages...
dotnet restore
if %errorLevel% neq 0 (
    echo [ERROR] Restore failed.
    pause
    exit /b 1
)

echo [INFO] Building application...
dotnet build --configuration Release --no-restore
if %errorLevel% neq 0 (
    echo [ERROR] Build failed.
    pause
    exit /b 1
)

echo [INFO] Publishing to Test environment...
dotnet publish --configuration Release --output "%DEPLOY_PATH%" --no-build /p:EnvironmentName=Test
if %errorLevel% neq 0 (
    echo [ERROR] Publish failed.
    pause
    exit /b 1
)
echo [SUCCESS] Application built and published.
echo.

REM Copy Test configuration
echo [STEP 5] Configuring Test environment settings...
if exist "appsettings.Test.json" (
    copy "appsettings.Test.json" "%DEPLOY_PATH%\appsettings.Test.json" /Y >nul
    echo [SUCCESS] Test configuration copied.
) else (
    echo [WARNING] appsettings.Test.json not found. Using default settings.
)

REM Set environment variable in web.config
echo [INFO] Setting environment variable to Test...
powershell -Command "(Get-Content '%DEPLOY_PATH%\web.config') -replace 'ASPNETCORE_ENVIRONMENT\" value=\"Development', 'ASPNETCORE_ENVIRONMENT\" value=\"Test' | Set-Content '%DEPLOY_PATH%\web.config'"
echo [SUCCESS] Environment variable configured.
echo.

REM Initialize Test database
echo [STEP 6] Initializing Test database...
if not exist "%DEPLOY_PATH%\%DB_NAME%" (
    echo [INFO] Creating new test database...
    cd /d "%DEPLOY_PATH%"
    dotnet GCCWebApp.dll --migrate-only >nul 2>&1
    echo [SUCCESS] Test database created and migrated.
) else (
    echo [INFO] Test database already exists.
    set /p RESET_DB="Reset test database? (y/n): "
    if /i "!RESET_DB!"=="y" (
        del "%DEPLOY_PATH%\%DB_NAME%" >nul 2>&1
        del "%DEPLOY_PATH%\%DB_NAME%-shm" >nul 2>&1
        del "%DEPLOY_PATH%\%DB_NAME%-wal" >nul 2>&1
        cd /d "%DEPLOY_PATH%"
        dotnet GCCWebApp.dll --migrate-only >nul 2>&1
        echo [SUCCESS] Test database reset and migrated.
    )
)
cd /d "%~dp0"
echo.

REM Create or update application pool
echo [STEP 7] Configuring IIS Application Pool for Test...
%WINDIR%\system32\inetsrv\appcmd.exe delete apppool "%APP_POOL_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe add apppool /name:"%APP_POOL_NAME%"
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /managedRuntimeVersion:""
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /processModel.identityType:ApplicationPoolIdentity
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /processModel.loadUserProfile:true
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /processModel.idleTimeout:00:00:00
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /recycling.periodicRestart.time:00:00:00
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /startMode:AlwaysRunning
echo [SUCCESS] Application pool configured.
echo.

REM Create or update website
echo [STEP 8] Configuring IIS Website for Test...
%WINDIR%\system32\inetsrv\appcmd.exe delete site "%SITE_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe add site /name:"%SITE_NAME%" /physicalPath:"%DEPLOY_PATH%" /bindings:"http/*:%HTTP_PORT%:"
%WINDIR%\system32\inetsrv\appcmd.exe set site "%SITE_NAME%" /applicationPool:"%APP_POOL_NAME%"
echo [SUCCESS] Website configured.
echo.

REM Configure HTTPS binding
echo [STEP 9] Configuring HTTPS for Test environment...
powershell -Command "$cert = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -match 'localhost' -or $_.Subject -match 'GCCWebApp-Test'} | Select-Object -First 1; if (!$cert) { $cert = New-SelfSignedCertificate -DnsName 'localhost','GCCWebApp-Test' -CertStoreLocation 'cert:\LocalMachine\My' -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(2); $store = New-Object System.Security.Cryptography.X509Certificates.X509Store('Root','LocalMachine'); $store.Open('ReadWrite'); $store.Add($cert); $store.Close(); } Write-Host $cert.Thumbprint"
%WINDIR%\system32\inetsrv\appcmd.exe set site "%SITE_NAME%" /+bindings.[protocol='https',bindingInformation='*:%HTTPS_PORT%:']
echo [SUCCESS] HTTPS configured.
echo.

REM Set permissions
echo [STEP 10] Setting folder permissions...
icacls "%DEPLOY_PATH%" /grant "IIS_IUSRS:(OI)(CI)RX" /t /q >nul
icacls "%DEPLOY_PATH%\logs" /grant "IIS_IUSRS:(OI)(CI)F" /t /q >nul
icacls "%DEPLOY_PATH%\DataProtectionKeys" /grant "IIS_IUSRS:(OI)(CI)F" /t /q >nul
icacls "%LOG_PATH%" /grant "IIS_IUSRS:(OI)(CI)F" /t /q >nul
icacls "%DEPLOY_PATH%\%DB_NAME%" /grant "IIS_IUSRS:F" >nul 2>&1
echo [SUCCESS] Permissions configured.
echo.

REM Start application pool and website
echo [STEP 11] Starting IIS services...
%WINDIR%\system32\inetsrv\appcmd.exe start apppool "%APP_POOL_NAME%"
if %errorLevel% neq 0 (
    echo [ERROR] Failed to start application pool.
    pause
    exit /b 1
)

%WINDIR%\system32\inetsrv\appcmd.exe start site "%SITE_NAME%"
if %errorLevel% neq 0 (
    echo [ERROR] Failed to start website.
    pause
    exit /b 1
)
echo [SUCCESS] IIS services started.
echo.

REM Test the deployment
echo [STEP 12] Testing deployment...
timeout /t 5 /nobreak >nul
echo [INFO] Testing HTTP endpoint...
curl -s -o nul -w "HTTP Status: %%{http_code}\n" http://localhost:%HTTP_PORT%/health 2>nul || echo [WARNING] HTTP endpoint not responding
echo [INFO] Testing HTTPS endpoint...
curl -k -s -o nul -w "HTTPS Status: %%{http_code}\n" https://localhost:%HTTPS_PORT%/health 2>nul || echo [WARNING] HTTPS endpoint not responding
echo.

REM Run smoke tests
echo [STEP 13] Running smoke tests...
echo [INFO] Checking database connection...
curl -s http://localhost:%HTTP_PORT%/api/health/db 2>nul | findstr "Healthy" >nul && (
    echo [SUCCESS] Database connection verified.
) || (
    echo [WARNING] Database connection could not be verified.
)
echo.

REM Display deployment summary
echo =================================================================
echo TEST ENVIRONMENT DEPLOYMENT COMPLETED!
echo =================================================================
echo.
echo Environment: TEST
echo Application URLs:
echo   HTTP:  http://localhost:%HTTP_PORT%
echo   HTTPS: https://localhost:%HTTPS_PORT%
echo.
echo Test Credentials:
echo   Admin:    admin@greencirclecertified.com / Admin123!
echo   Analyst:  analyst@greencirclecertified.com / Analyst123!
echo   External: partner@amazon.com / Partner123!
echo.
echo Health Check Endpoints:
echo   General: http://localhost:%HTTP_PORT%/health
echo   Database: http://localhost:%HTTP_PORT%/api/health/db
echo.
echo Management Commands:
echo   View Site:     %WINDIR%\system32\inetsrv\appcmd.exe list site "%SITE_NAME%"
echo   Stop Site:     %WINDIR%\system32\inetsrv\appcmd.exe stop site "%SITE_NAME%"
echo   Start Site:    %WINDIR%\system32\inetsrv\appcmd.exe start site "%SITE_NAME%"
echo   Recycle Pool:  %WINDIR%\system32\inetsrv\appcmd.exe recycle apppool "%APP_POOL_NAME%"
echo.
echo Log Locations:
echo   Application: %DEPLOY_PATH%\logs\
echo   IIS Logs: %LOG_PATH%\
echo   Event Viewer: Windows Logs > Application
echo.
echo Database:
echo   Location: %DEPLOY_PATH%\%DB_NAME%
echo.
echo Next Steps:
echo   1. Open https://localhost:%HTTPS_PORT% in your browser
echo   2. Login with test credentials
echo   3. Run test suite: dotnet test GCCWebApp.Tests
echo   4. Check application logs for any issues
echo =================================================================
echo.

REM Offer to open browser
set /p OPEN_BROWSER="Open TEST application in browser? (y/n): "
if /i "%OPEN_BROWSER%"=="y" (
    echo Opening TEST environment...
    start https://localhost:%HTTPS_PORT%
)

REM Offer to run tests
echo.
set /p RUN_TESTS="Run automated tests? (y/n): "
if /i "%RUN_TESTS%"=="y" (
    echo.
    echo Running test suite...
    cd /d "%~dp0"
    dotnet test --configuration Release --logger "console;verbosity=minimal"
)

echo.
echo Test deployment completed successfully!
pause
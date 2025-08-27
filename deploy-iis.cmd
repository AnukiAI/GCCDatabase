@echo off
echo =================================================================
echo GCC Web Application - IIS Deployment Script
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

REM Set deployment variables
set APP_NAME=GCCWebApp
set SITE_NAME=GCCWebApp-Local
set APP_POOL_NAME=GCCWebApp-AppPool
set DEPLOY_PATH=C:\inetpub\wwwroot\GCCWebApp
set HTTP_PORT=5101
set HTTPS_PORT=5101
set LOG_PATH=C:\inetpub\logs\GCCWebApp

echo =================================================================
echo Deployment Configuration:
echo =================================================================
echo Application Name: %APP_NAME%
echo Site Name: %SITE_NAME%
echo App Pool Name: %APP_POOL_NAME%
echo Deploy Path: %DEPLOY_PATH%
echo HTTP Port: %HTTP_PORT%
echo HTTPS Port: %HTTPS_PORT%
echo Log Path: %LOG_PATH%
echo =================================================================
echo.

REM Create directories
echo [STEP 1] Creating deployment directories...
if not exist "%DEPLOY_PATH%" mkdir "%DEPLOY_PATH%"
if not exist "%LOG_PATH%" mkdir "%LOG_PATH%"
if not exist "%DEPLOY_PATH%\logs" mkdir "%DEPLOY_PATH%\logs"
echo [SUCCESS] Directories created.
echo.

REM Stop existing site and app pool if they exist
echo [STEP 2] Stopping existing IIS site and application pool...
%WINDIR%\system32\inetsrv\appcmd.exe stop site "%SITE_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe stop apppool "%APP_POOL_NAME%" >nul 2>&1
echo [SUCCESS] Existing services stopped.
echo.

REM Build and publish application
echo [STEP 3] Building and publishing application...
dotnet clean --configuration Release
if %errorLevel% neq 0 (
    echo [ERROR] Clean failed.
    pause
    exit /b 1
)

dotnet restore
if %errorLevel% neq 0 (
    echo [ERROR] Restore failed.
    pause
    exit /b 1
)

dotnet build --configuration Release --no-restore
if %errorLevel% neq 0 (
    echo [ERROR] Build failed.
    pause
    exit /b 1
)

dotnet publish --configuration Release --output "%DEPLOY_PATH%" --no-build
if %errorLevel% neq 0 (
    echo [ERROR] Publish failed.
    pause
    exit /b 1
)
echo [SUCCESS] Application built and published.
echo.

REM Create or update application pool
echo [STEP 4] Configuring IIS Application Pool...
%WINDIR%\system32\inetsrv\appcmd.exe add apppool /name:"%APP_POOL_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /managedRuntimeVersion:""
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /processModel.identityType:ApplicationPoolIdentity
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /processModel.loadUserProfile:true
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /processModel.idleTimeout:00:00:00
%WINDIR%\system32\inetsrv\appcmd.exe set apppool "%APP_POOL_NAME%" /recycling.periodicRestart.time:00:00:00
echo [SUCCESS] Application pool configured.
echo.

REM Create or update website
echo [STEP 5] Configuring IIS Website...
%WINDIR%\system32\inetsrv\appcmd.exe add site /name:"%SITE_NAME%" /physicalPath:"%DEPLOY_PATH%" /bindings:"http/*:%HTTP_PORT%:" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe set site "%SITE_NAME%" /applicationPool:"%APP_POOL_NAME%"
echo [SUCCESS] Website configured.
echo.

REM Configure HTTPS binding (requires certificate)
echo [STEP 6] Configuring HTTPS binding...
echo [INFO] Checking for SSL certificate...
for /f "tokens=2" %%i in ('certlm -store my -c^|findstr /i "localhost"') do (
    set CERT_THUMBPRINT=%%i
    goto :cert_found
)

echo [WARNING] No SSL certificate found for localhost.
echo [INFO] Creating self-signed certificate...
powershell -Command "$cert = New-SelfSignedCertificate -DnsName 'localhost' -CertStoreLocation 'cert:\LocalMachine\My' -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(2); $store = New-Object System.Security.Cryptography.X509Certificates.X509Store('Root','LocalMachine'); $store.Open('ReadWrite'); $store.Add($cert); $store.Close(); Write-Host $cert.Thumbprint"

:cert_found
echo [INFO] SSL certificate configured.
%WINDIR%\system32\inetsrv\appcmd.exe set site "%SITE_NAME%" /+bindings.[protocol='https',bindingInformation='*:%HTTPS_PORT%:'] >nul 2>&1
echo [SUCCESS] HTTPS binding configured.
echo.

REM Set permissions
echo [STEP 7] Setting folder permissions...
icacls "%DEPLOY_PATH%" /grant "IIS_IUSRS:(OI)(CI)RX" /t >nul
icacls "%DEPLOY_PATH%\logs" /grant "IIS_IUSRS:(OI)(CI)F" /t >nul
icacls "%LOG_PATH%" /grant "IIS_IUSRS:(OI)(CI)F" /t >nul
echo [SUCCESS] Permissions configured.
echo.

REM Start application pool and website
echo [STEP 8] Starting IIS services...
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
echo [STEP 9] Testing deployment...
timeout /t 5 /nobreak >nul
echo [INFO] Testing HTTP endpoint...
curl -s -o nul -w "HTTP Status: %%{http_code}\n" http://localhost:%HTTP_PORT%
echo [INFO] Testing HTTPS endpoint...
curl -k -s -o nul -w "HTTPS Status: %%{http_code}\n" https://localhost:%HTTPS_PORT%
echo.

REM Display deployment summary
echo =================================================================
echo DEPLOYMENT COMPLETED SUCCESSFULLY!
echo =================================================================
echo.
echo Application URLs:
echo   HTTP:  http://localhost:%HTTP_PORT%
echo   HTTPS: https://localhost:%HTTPS_PORT%
echo.
echo Management Commands:
echo   View Site:     %%WINDIR%%\system32\inetsrv\appcmd.exe list site "%SITE_NAME%"
echo   Stop Site:     %%WINDIR%%\system32\inetsrv\appcmd.exe stop site "%SITE_NAME%"
echo   Start Site:    %%WINDIR%%\system32\inetsrv\appcmd.exe start site "%SITE_NAME%"
echo   View App Pool: %%WINDIR%%\system32\inetsrv\appcmd.exe list apppool "%APP_POOL_NAME%"
echo.
echo Log Locations:
echo   Application Logs: %DEPLOY_PATH%\logs\
echo   IIS Logs: %LOG_PATH%\
echo   Event Viewer: Windows Logs ^> Application
echo.
echo Next Steps:
echo   1. Test the application in your browser
echo   2. Check logs for any startup issues
echo   3. Configure database connection if needed
echo   4. Set up monitoring and health checks
echo =================================================================
echo.

REM Offer to open browser
set /p OPEN_BROWSER="Open application in browser? (y/n): "
if /i "%OPEN_BROWSER%"=="y" (
    echo Opening application...
    start https://localhost:%HTTPS_PORT%
)

echo.
echo Deployment script completed.
pause
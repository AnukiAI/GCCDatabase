@echo off
echo =================================================================
echo GCC Web Application - IIS Management Script
echo =================================================================
echo.

set SITE_NAME=GCCWebApp-Local
set APP_POOL_NAME=GCCWebApp-AppPool
set PORT=5101

:menu
echo Available Commands:
echo   1. Start Application
echo   2. Stop Application
echo   3. Restart Application
echo   4. Check Status
echo   5. View Logs
echo   6. Open Application
echo   7. Redeploy Application
echo   8. Remove Application
echo   9. Exit
echo.
set /p choice="Select option (1-9): "

if "%choice%"=="1" goto start_app
if "%choice%"=="2" goto stop_app
if "%choice%"=="3" goto restart_app
if "%choice%"=="4" goto check_status
if "%choice%"=="5" goto view_logs
if "%choice%"=="6" goto open_app
if "%choice%"=="7" goto redeploy_app
if "%choice%"=="8" goto remove_app
if "%choice%"=="9" goto exit_script
echo Invalid choice. Please try again.
echo.
goto menu

:start_app
echo Starting GCC Web Application...
%WINDIR%\system32\inetsrv\appcmd.exe start apppool "%APP_POOL_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe start site "%SITE_NAME%" >nul 2>&1
timeout /t 3 /nobreak >nul
curl -s -o nul -w "Status: %%{http_code}\n" https://localhost:%PORT%
echo Application started.
echo.
goto menu

:stop_app
echo Stopping GCC Web Application...
%WINDIR%\system32\inetsrv\appcmd.exe stop site "%SITE_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe stop apppool "%APP_POOL_NAME%" >nul 2>&1
echo Application stopped.
echo.
goto menu

:restart_app
echo Restarting GCC Web Application...
%WINDIR%\system32\inetsrv\appcmd.exe stop site "%SITE_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe stop apppool "%APP_POOL_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul
%WINDIR%\system32\inetsrv\appcmd.exe start apppool "%APP_POOL_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe start site "%SITE_NAME%" >nul 2>&1
timeout /t 3 /nobreak >nul
curl -s -o nul -w "Status: %%{http_code}\n" https://localhost:%PORT%
echo Application restarted.
echo.
goto menu

:check_status
echo Checking GCC Web Application Status...
echo.
echo Site Status:
%WINDIR%\system32\inetsrv\appcmd.exe list site "%SITE_NAME%"
echo.
echo App Pool Status:
%WINDIR%\system32\inetsrv\appcmd.exe list apppool "%APP_POOL_NAME%"
echo.
echo HTTP Health Check:
curl -s -o nul -w "HTTP Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:%PORT%
echo HTTPS Health Check:
curl -k -s -o nul -w "HTTPS Status: %%{http_code} | Response Time: %%{time_total}s\n" https://localhost:%PORT%
echo.
goto menu

:view_logs
echo Recent Application Logs:
echo =================================================================
if exist "C:\inetpub\wwwroot\GCCWebApp\logs\*.log" (
    for /f %%i in ('dir /b /od "C:\inetpub\wwwroot\GCCWebApp\logs\*.log"') do (
        echo Latest log file: %%i
        echo -----------------------------------------------------------------
        tail -n 20 "C:\inetpub\wwwroot\GCCWebApp\logs\%%i" 2>nul || (
            powershell -Command "Get-Content 'C:\inetpub\wwwroot\GCCWebApp\logs\%%i' | Select-Object -Last 20"
        )
        goto logs_shown
    )
    :logs_shown
) else (
    echo No log files found.
)
echo =================================================================
echo.
goto menu

:open_app
echo Opening GCC Web Application...
start https://localhost:%PORT%
echo.
goto menu

:redeploy_app
echo Redeploying GCC Web Application...
call deploy-iis.cmd
echo.
goto menu

:remove_app
echo WARNING: This will completely remove the GCC Web Application from IIS.
set /p confirm="Are you sure? (y/N): "
if /i not "%confirm%"=="y" (
    echo Operation cancelled.
    echo.
    goto menu
)
echo Removing GCC Web Application...
%WINDIR%\system32\inetsrv\appcmd.exe delete site "%SITE_NAME%" >nul 2>&1
%WINDIR%\system32\inetsrv\appcmd.exe delete apppool "%APP_POOL_NAME%" >nul 2>&1
echo Application removed from IIS.
echo Note: Files in C:\inetpub\wwwroot\GCCWebApp were not deleted.
echo.
goto menu

:exit_script
echo Exiting...
exit /b 0
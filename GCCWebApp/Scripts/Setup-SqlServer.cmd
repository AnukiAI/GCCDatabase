@echo off
echo =========================================
echo GCC Database SQL Server Setup
echo =========================================
echo.
echo This script will set up SQL Server for local production testing.
echo Please ensure you are running this as Administrator.
echo.
echo Options:
echo 1. Full Setup (Install SQL Server, Create Databases, Set Permissions)
echo 2. Install SQL Server Only
echo 3. Create Databases Only
echo 4. Setup Permissions Only
echo 5. Exit
echo.
set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" goto full
if "%choice%"=="2" goto install
if "%choice%"=="3" goto database
if "%choice%"=="4" goto permissions
if "%choice%"=="5" goto end

:full
echo.
echo Running full setup...
powershell -ExecutionPolicy Bypass -File "%~dp0Setup-SqlServer-Local.ps1" -RunAll
goto done

:install
echo.
echo Installing SQL Server...
powershell -ExecutionPolicy Bypass -File "%~dp0Setup-SqlServer-Local.ps1" -InstallSqlServer
goto done

:database
echo.
echo Creating databases...
powershell -ExecutionPolicy Bypass -File "%~dp0Setup-SqlServer-Local.ps1" -CreateDatabase
goto done

:permissions
echo.
echo Setting up permissions...
powershell -ExecutionPolicy Bypass -File "%~dp0Setup-SqlServer-Local.ps1" -SetupPermissions
goto done

:done
echo.
echo =========================================
echo Setup completed!
echo =========================================
echo.
echo Next steps:
echo 1. Update appsettings.json with the connection string shown above
echo 2. Run: dotnet ef database update
echo 3. Test the application
echo.
pause
goto end

:end
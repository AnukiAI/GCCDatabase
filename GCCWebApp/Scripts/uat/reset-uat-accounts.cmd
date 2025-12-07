@echo off
REM ============================================
REM GCC Web Application - Reset UAT Accounts
REM ============================================
REM Resets all test user passwords in production
REM ============================================

setlocal

echo.
echo ============================================
echo GCC Web Application - Reset UAT Accounts
echo ============================================
echo.

set "INFO=[92m[INFO][0m"
set "ERROR=[91m[ERROR][0m"
set "WARNING=[93m[WARNING][0m"
set "SUCCESS=[92m[SUCCESS][0m"

set "SERVER=68.71.129.120,1533"
set "DATABASE=gccwebap_certificates"
set "USERNAME=GCC_adminxyz123"
set "PASSWORD=Q3Ez4e!gE_47KNi"

set "SCRIPT_DIR=%~dp0"
set "SQL_SCRIPT=%SCRIPT_DIR%..\..\Scripts\RESET_PRODUCTION_PASSWORDS.sql"

echo %INFO% This will reset ALL test account passwords to: TestPass123!
echo.
echo Target: %SERVER% / %DATABASE%
echo.

choice /C YN /M "Are you sure you want to reset passwords"
if %ERRORLEVEL% EQU 2 (
    echo.
    echo %INFO% Operation cancelled.
    pause
    exit /b 0
)

echo.
echo %INFO% Connecting to production database...
echo.

REM Check if SQL script exists
if not exist "%SQL_SCRIPT%" (
    echo %ERROR% SQL script not found: %SQL_SCRIPT%
    echo.
    echo Creating inline reset script...

    REM Run inline SQL
    sqlcmd -S %SERVER% -U %USERNAME% -P "%PASSWORD%" -d %DATABASE% -Q "UPDATE Users SET PasswordHash = '$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa' WHERE Username IN ('admin', 'analyst', 'external', 'testuser', 'sarah.johnson', 'mike.chen', 'lisa.rodriguez', 'david.kim', 'emma.wilson', 'alex.martinez', 'jennifer.lee', 'robert.taylor', 'maria.garcia', 'public.user'); SELECT @@ROWCOUNT AS 'Rows Updated';"
) else (
    REM Run from file
    sqlcmd -S %SERVER% -U %USERNAME% -P "%PASSWORD%" -d %DATABASE% -i "%SQL_SCRIPT%"
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo %ERROR% Failed to reset passwords
    echo.
    echo Possible issues:
    echo   - Cannot connect to database server
    echo   - Invalid credentials
    echo   - Network/firewall issues
    echo.
    pause
    exit /b 1
)

echo.
echo %SUCCESS% Passwords reset successfully!
echo.
echo ============================================
echo Test Account Credentials
echo ============================================
echo.
echo Username Login (use username, not email):
echo   admin / TestPass123!
echo   analyst / TestPass123!
echo   external / TestPass123!
echo.
echo Test URL: https://gccwebapp.com/Account/Login
echo.
pause

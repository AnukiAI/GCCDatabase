@echo off
REM ============================================
REM GCC Database Migration Script (Windows Batch Wrapper)
REM ============================================
REM
REM USAGE:
REM   apply-migrations.cmd [Development|Staging|Production]
REM
REM EXAMPLES:
REM   apply-migrations.cmd Production
REM   apply-migrations.cmd Staging
REM   apply-migrations.cmd Development
REM ============================================

setlocal

REM Default to Production if no argument provided
set ENVIRONMENT=%1
if "%ENVIRONMENT%"=="" set ENVIRONMENT=Production

echo.
echo ============================================
echo GCC Database Migration Tool
echo ============================================
echo.
echo Target Environment: %ENVIRONMENT%
echo.

REM Check if PowerShell is available
where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Using PowerShell Core...
    pwsh -ExecutionPolicy Bypass -File "%~dp0apply-migrations.ps1" -Environment %ENVIRONMENT%
) else (
    echo Using Windows PowerShell...
    powershell -ExecutionPolicy Bypass -File "%~dp0apply-migrations.ps1" -Environment %ENVIRONMENT%
)

echo.
if %ERRORLEVEL% EQU 0 (
    echo Migration completed successfully.
) else (
    echo Migration failed. See error messages above.
)

pause

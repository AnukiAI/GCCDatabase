@echo off
REM GCC Web App E2E Test Runner (Batch Wrapper)
REM This script calls the PowerShell script to run E2E tests

echo Starting GCC Web App E2E Tests...
echo.

REM Check if PowerShell is available
where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    REM Use PowerShell Core if available
    pwsh -ExecutionPolicy Bypass -File "%~dp0run-e2e-tests.ps1" %*
) else (
    REM Fall back to Windows PowerShell
    powershell -ExecutionPolicy Bypass -File "%~dp0run-e2e-tests.ps1" %*
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Tests failed! See output above for details.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Tests completed successfully!
pause

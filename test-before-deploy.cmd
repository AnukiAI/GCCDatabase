@echo off
REM ============================================================================
REM PRE-DEPLOYMENT LOGIN TEST
REM ============================================================================
REM Run this BEFORE every deployment to ensure login works
REM If this test fails, DO NOT deploy!
REM ============================================================================

echo.
echo ============================================================================
echo  PRE-DEPLOYMENT LOGIN SMOKE TEST
echo ============================================================================
echo  This test verifies that admin login works before deployment.
echo  If this test FAILS, deployment is BLOCKED until issue is resolved.
echo ============================================================================
echo.

cd GCCWebApp-Tests\GCCWebApp.Tests

echo Running critical login test...
echo.

dotnet test --filter "Category=SmokeTest&Priority=Critical" --logger "console;verbosity=detailed"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================================================
    echo  ✓ LOGIN TEST PASSED - Safe to deploy!
    echo ============================================================================
    echo.
    exit /b 0
) else (
    echo.
    echo ============================================================================
    echo  ✗ LOGIN TEST FAILED - DO NOT DEPLOY!
    echo ============================================================================
    echo  Fix the login issue before deploying to production.
    echo  Common issues:
    echo    1. Cookie security policy blocking cookies
    echo    2. DataProtection keys not persisting
    echo    3. User/role configuration incorrect
    echo    4. Missing logs folder
    echo.
    echo  Run full login tests for more details:
    echo    dotnet test --filter "Category=Critical&Category=PreDeployment"
    echo ============================================================================
    echo.
    exit /b 1
)

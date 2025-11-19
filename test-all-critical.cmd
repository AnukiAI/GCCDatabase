@echo off
REM ============================================================================
REM RUN ALL CRITICAL TESTS
REM ============================================================================
REM Runs all tests tagged as Critical to verify core functionality
REM ============================================================================

echo.
echo ============================================================================
echo  RUNNING ALL CRITICAL TESTS
echo ============================================================================
echo  These tests verify core functionality including:
echo    - Admin login works
echo    - Different user roles can login
echo    - Authentication cookies are set correctly
echo    - Protected pages are accessible after login
echo    - Invalid credentials are rejected
echo ============================================================================
echo.

cd GCCWebApp-Tests\GCCWebApp.Tests

echo Running all critical login tests...
echo.

dotnet test --filter "Category=Critical&Category=PreDeployment" --logger "console;verbosity=normal"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================================================
    echo  ✓ ALL CRITICAL TESTS PASSED
    echo ============================================================================
    echo.

    echo Now running smoke test...
    dotnet test --filter "Category=SmokeTest" --logger "console;verbosity=detailed"

    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ============================================================================
        echo  ✓✓✓ ALL TESTS PASSED - SAFE TO DEPLOY ✓✓✓
        echo ============================================================================
        echo.
        exit /b 0
    )
)

echo.
echo ============================================================================
echo  ✗ TESTS FAILED - DO NOT DEPLOY
echo ============================================================================
echo  Review the test output above to identify failures.
echo  Fix all issues before deploying to production.
echo ============================================================================
echo.
exit /b 1

@echo off
echo ========================================
echo  GCC Web App - Role-Based UI Tests
echo ========================================
echo.

REM Ensure Playwright browsers are installed
echo Installing Playwright browsers...
pwsh -Command "& {npx playwright install}"
echo.

REM Create Screenshots directory
if not exist "Screenshots" mkdir Screenshots
if not exist "Screenshots\Admin" mkdir Screenshots\Admin
if not exist "Screenshots\Analyst" mkdir Screenshots\Analyst
if not exist "Screenshots\External" mkdir Screenshots\External
if not exist "Screenshots\Public" mkdir Screenshots\Public
if not exist "Screenshots\Security" mkdir Screenshots\Security

echo Starting Role-Based UI Tests...
echo.

REM Run all tests with detailed output
dotnet test --logger "console;verbosity=detailed" --logger "trx;LogFileName=role-based-ui-tests.trx" --results-directory "test-results"

echo.
echo ========================================
echo  Test Execution Complete
echo ========================================
echo.
echo Test results saved to: test-results\role-based-ui-tests.trx
echo Screenshots saved to: Screenshots\
echo.

REM Check if tests passed
if %ERRORLEVEL% EQU 0 (
    echo ✅ All tests PASSED
) else (
    echo ❌ Some tests FAILED - Check output above
)

echo.
echo Press any key to exit...
pause >nul
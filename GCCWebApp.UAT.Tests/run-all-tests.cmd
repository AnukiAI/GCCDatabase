@echo off
echo ========================================
echo GCC Web App - Complete UAT Test Suite
echo ========================================
echo.

REM Set test execution parameters
set TEST_TIMEOUT=600000
set BROWSER_HEADLESS=false

echo Starting comprehensive UAT test execution...
echo Test timeout: %TEST_TIMEOUT%ms
echo Browser headless: %BROWSER_HEADLESS%
echo.

REM Ensure screenshots directory exists
if not exist "Screenshots" mkdir Screenshots

echo Running all UAT tests with detailed output...
dotnet test --logger "console;verbosity=detailed" --logger "trx;LogFileName=UAT_TestResults_%date:~-4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.trx" --results-directory "TestResults" --collect:"XPlat Code Coverage"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo ✅ All UAT tests completed successfully!
    echo ========================================
    echo.
    echo Test artifacts saved in:
    echo - Screenshots: Screenshots\
    echo - Test Results: TestResults\
    echo.
) else (
    echo.
    echo ========================================
    echo ❌ Some tests failed. Check output above.
    echo ========================================
    echo.
    echo Review test artifacts:
    echo - Screenshots: Screenshots\
    echo - Test Results: TestResults\
    echo.
)

pause
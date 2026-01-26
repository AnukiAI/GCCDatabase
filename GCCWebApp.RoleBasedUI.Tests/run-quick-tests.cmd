@echo off
echo ========================================
echo  GCC Web App - Quick Test Run
echo ========================================
echo.

REM Create directories
if not exist "Screenshots" mkdir Screenshots
if not exist "test-results" mkdir test-results

echo Installing Playwright browsers (quick)...
pwsh -Command "& {npx playwright install chromium --with-deps}" >nul 2>&1

echo.
echo Running core functionality tests...
echo.

REM Run basic login and navigation tests
echo [1/3] Testing Login Functionality...
dotnet test --filter "TestName~Login" --logger "console;verbosity=minimal" --results-directory "test-results"

echo [2/3] Testing Admin Access...
dotnet test --filter "TestName~Admin" --logger "console;verbosity=minimal" --results-directory "test-results"

echo [3/3] Testing Security Basics...
dotnet test --filter "TestName~Security" --logger "console;verbosity=minimal" --results-directory "test-results"

echo.
echo ========================================
echo  Quick Test Complete
echo ========================================
echo.
echo For comprehensive testing, run: run-all-tests.cmd
echo.
pause
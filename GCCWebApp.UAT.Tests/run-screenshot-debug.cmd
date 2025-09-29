@echo off
echo ========================================
echo Screenshot Test Debug Script
echo ========================================
echo.

REM Check if application is running
echo Step 1: Checking if GCC Web App is running...
curl -k -s -o nul https://localhost:7248
if %errorlevel% neq 0 (
    echo [ERROR] Application is NOT running at https://localhost:7248
    echo Please start the application first:
    echo   1. Open another terminal
    echo   2. Navigate to GCCWebApp folder  
    echo   3. Run: dotnet run
    pause
    exit /b 1
) else (
    echo [OK] Application is running at https://localhost:7248
)

echo.
echo Step 2: Installing Playwright browsers if needed...
pwsh -Command "npx playwright install chromium"

echo.
echo Step 3: Running a single screenshot test in verbose mode...
dotnet test --filter "FullyQualifiedName~PublicPagesScreenshotTests.CaptureHomePage" --logger "console;verbosity=detailed" -- NUnit.NumberOfTestWorkers=1

echo.
echo ========================================
echo Debug test completed!
echo Check the Screenshots folder for output
echo ========================================
pause
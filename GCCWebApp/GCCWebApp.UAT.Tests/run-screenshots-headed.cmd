@echo off
echo ========================================
echo Running Screenshot Tests in HEADED Mode (Visible Browser)
echo ========================================
echo.

REM Set environment variables for headed mode
set TEST_HEADLESS=false
set TEST_BASE_URL=https://localhost:7248

REM Check if application is running
echo Checking if GCC Web App is running at %TEST_BASE_URL%...
curl -k -s -o nul %TEST_BASE_URL%
if %errorlevel% neq 0 (
    echo [ERROR] Application is NOT running at %TEST_BASE_URL%
    echo Please start the application first.
    pause
    exit /b 1
)

echo [OK] Application is running
echo.
echo Running screenshot tests with visible browser...
echo You will see browser windows open and navigate through pages.
echo.
dotnet test --filter "FullyQualifiedName~ScreenshotTests" --logger "console;verbosity=normal" --results-directory test-results

echo.
echo Tests completed! Screenshots saved to: Screenshots\
pause
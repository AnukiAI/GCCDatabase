@echo off
echo ========================================
echo Comprehensive UAT Test Suite
echo ========================================
echo.

REM Check if application is running
echo Step 1: Checking if GCC Web App is running...
curl -k -s -o nul https://localhost:7248
if %errorlevel% neq 0 (
    echo [ERROR] Application is NOT running at https://localhost:7248
    echo Please start the application first.
    pause
    exit /b 1
)

echo [OK] Application is running
echo.

echo Step 2: Running Basic Functional Tests...
dotnet test --filter "Category!=Validation&Category!=Negative&Category!=UX&Category!=EdgeCases" --logger "console;verbosity=normal" --results-directory test-results/functional

echo.
echo Step 3: Running Form Validation Tests...
dotnet test --filter "Category=Validation" --logger "console;verbosity=normal" --results-directory test-results/validation

echo.
echo Step 4: Running Security ^& Negative Tests...
dotnet test --filter "Category=Negative" --logger "console;verbosity=normal" --results-directory test-results/security

echo.
echo Step 5: Running UX/UI Tests...
dotnet test --filter "Category=UX" --logger "console;verbosity=normal" --results-directory test-results/ux

echo.
echo Step 6: Running Edge Case Tests...
dotnet test --filter "Category=EdgeCases" --logger "console;verbosity=normal" --results-directory test-results/edge-cases

echo.
echo Step 7: Running Screenshot Documentation Tests...
dotnet test --filter "FullyQualifiedName~ScreenshotTests" --logger "console;verbosity=normal" --results-directory test-results/screenshots

echo.
echo ========================================
echo Comprehensive Test Suite Complete!
echo ========================================
echo.
echo Test Results Summary:
echo - Functional Tests: test-results/functional/
echo - Validation Tests: test-results/validation/
echo - Security Tests: test-results/security/
echo - UX/UI Tests: test-results/ux/
echo - Edge Cases: test-results/edge-cases/
echo - Screenshots: test-results/screenshots/
echo.
echo Check individual result directories for detailed reports.
echo.
pause

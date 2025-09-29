@echo off
echo Running GCC Web App Accessibility Tests
echo =====================================

echo.
echo Building project...
dotnet build --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b 1
)

echo.
echo Running accessibility tests...
dotnet test --configuration Release --logger "trx;LogFileName=accessibility_results.trx" --results-directory TestResults

echo.
echo Accessibility test run completed.
echo Check the Reports folder for detailed violation reports.
echo Check TestResults folder for test execution results.

pause
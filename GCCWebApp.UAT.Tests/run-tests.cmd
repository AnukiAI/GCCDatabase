@echo off
echo Running GCC Web App UAT Tests with Screenshots...
echo.

REM Check if application is running
echo Checking if GCC Web App is running at https://localhost:7248...
curl -k -s -o nul https://localhost:7130
if %errorlevel% neq 0 (
    echo.
    echo [WARNING] GCC Web App doesn't appear to be running at https://localhost:7248
    echo Please start the application before running tests:
    echo   1. Open another terminal
    echo   2. Navigate to the GCCWebApp folder
    echo   3. Run: dotnet run
    echo.
    pause
)

echo.
echo Starting UAT Tests...
echo Results will be saved to: test-results/
echo Screenshots will be saved to: Screenshots/
echo.

dotnet test --logger "console;verbosity=detailed" --results-directory test-results

echo.
echo Tests completed! Check the following for results:
echo - Screenshots: Screenshots/ folder
echo - Test Results: test-results/ folder
echo - Console output above
echo.
pause
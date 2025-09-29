@echo off
echo ======================================
echo Running Full System Screenshot Tests
echo ======================================

REM Ensure the application is running
echo Starting application server...
start /B dotnet run --project "..\GCCWebApp.csproj" --urls "https://localhost:7130;http://localhost:7130"

REM Wait for application to start
timeout /t 10

echo.
echo Running screenshot tests...
echo.

REM Install Playwright browsers if needed
echo Installing Playwright browsers...
pwsh bin\Debug\net8.0\playwright.ps1 install

echo.
echo Running all screenshot test suites...
echo.

REM Run all screenshot tests
dotnet test --filter "TestCategory~Screenshot" --logger "console;verbosity=detailed" --logger "html;logfilename=ScreenshotTestResults.html"

REM Also run with specific test classes
dotnet test --filter "ClassName~Screenshot" --logger "console;verbosity=detailed"

echo.
echo ======================================
echo Screenshot Tests Complete
echo ======================================
echo.
echo Screenshots saved in: Screenshots\Full_System_Screenshots_[timestamp]
echo Test results saved in: TestResults\ScreenshotTestResults.html
echo.
echo Press any key to close application server...
pause

REM Stop the application server
taskkill /f /im dotnet.exe 2>nul

echo Done.
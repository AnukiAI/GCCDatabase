@echo off
echo ======================================
echo Running Quick Screenshot Tests (Desktop Only)
echo ======================================

REM Ensure the application is running
echo Starting application server...
start /B dotnet run --project "..\GCCWebApp.csproj" --urls "https://localhost:5001;http://localhost:5000"

REM Wait for application to start
timeout /t 10

echo.
echo Running quick screenshot tests (Desktop viewport only)...
echo.

REM Install Playwright browsers if needed
echo Installing Playwright browsers...
pwsh bin\Debug\net8.0\playwright.ps1 install

echo.
echo Running public pages screenshot tests...
dotnet test --filter "FullyQualifiedName~PublicPagesScreenshotTests" --logger "console;verbosity=detailed"

echo.
echo Running authentication pages screenshot tests...
dotnet test --filter "FullyQualifiedName~AuthenticationPagesScreenshotTests" --logger "console;verbosity=detailed"

echo.
echo ======================================
echo Quick Screenshot Tests Complete
echo ======================================
echo.
echo Screenshots saved in: Screenshots\Full_System_Screenshots_[timestamp]
echo.
echo Press any key to close application server...
pause

REM Stop the application server
taskkill /f /im dotnet.exe 2>nul

echo Done.
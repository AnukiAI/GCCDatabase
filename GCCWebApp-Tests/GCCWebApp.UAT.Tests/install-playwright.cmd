@echo off
echo ========================================
echo Installing Playwright Browsers
echo ========================================
echo.

echo Installing Playwright CLI tool...
dotnet tool install --global Microsoft.Playwright.CLI

echo.
echo Installing browser binaries...
playwright install

echo.
echo Alternatively, using PowerShell...
pwsh -Command "npx playwright install"

echo.
echo ========================================
echo Installation complete!
echo ========================================
pause
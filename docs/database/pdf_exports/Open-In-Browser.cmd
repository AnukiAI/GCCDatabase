@echo off
REM GCC Database - Open HTML files in browser for PDF export
REM Version: 1.0
REM Date: November 21, 2025

echo.
echo ===================================================
echo GCC Database - Open Documentation in Browser
echo ===================================================
echo.
echo This will open all HTML documentation files in your
echo default browser. You can then print to PDF using
echo Ctrl+P and selecting "Save as PDF".
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul

echo.
echo Opening files in browser...
echo.

REM Open Data Migration Strategy
if exist "Data_Migration_Strategy.html" (
    echo Opening: Data_Migration_Strategy.html
    start "" "Data_Migration_Strategy.html"
    timeout /t 2 /nobreak >nul
) else (
    echo Warning: Data_Migration_Strategy.html not found
)

REM Open Field Mapping Reference
if exist "Field_Mapping_Reference.html" (
    echo Opening: Field_Mapping_Reference.html
    start "" "Field_Mapping_Reference.html"
    timeout /t 2 /nobreak >nul
) else (
    echo Note: Field_Mapping_Reference.html not found (may not be created yet)
)

echo.
echo ===================================================
echo Files opened in browser
echo ===================================================
echo.
echo To convert to PDF:
echo   1. Press Ctrl+P in your browser
echo   2. Select "Save as PDF" as the destination
echo   3. Enable "Background graphics" in print settings
echo   4. Click "Save"
echo.
echo For more options, see README.md in this directory.
echo.
pause

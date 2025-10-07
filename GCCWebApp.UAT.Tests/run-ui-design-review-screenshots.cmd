@echo off
REM ==============================================================================
REM UI Design Review Screenshot Capture
REM ==============================================================================
REM Captures screenshots of all pages for UI design consistency review
REM Screenshots saved to: Screenshots/UI_Design_Review_{timestamp}/
REM ==============================================================================

echo.
echo ================================================================================
echo   GCC Web Application - UI Design Review Screenshot Capture
echo ================================================================================
echo.
echo This script captures screenshots of all pages for design consistency review.
echo.
echo IMPORTANT: Make sure the application is running at http://localhost:5101
echo.
echo Press Ctrl+C to cancel, or
pause

echo.
echo ================================================================================
echo Starting UI Design Review Screenshot Capture...
echo ================================================================================
echo.

REM Run the UI Design Review screenshot tests
dotnet test --filter "Category=UIDesignReview" --logger "console;verbosity=detailed"

echo.
echo ================================================================================
echo Screenshot Capture Complete!
echo ================================================================================
echo.
echo Screenshots saved to: Screenshots\UI_Design_Review_{timestamp}\
echo.
echo Next Steps:
echo 1. Open the screenshots folder to review all captured images
echo 2. Open 0_Documentation\Deliverables\UI_DESIGN_CONSISTENCY_REVIEW.html
echo 3. Compare screenshots against design standards
echo 4. Document any inconsistencies found
echo 5. Create prioritized fix list
echo.
echo Press any key to open the Screenshots folder...
pause >nul

REM Open the Screenshots directory
start "" "Screenshots"

echo.
echo Done!
echo.

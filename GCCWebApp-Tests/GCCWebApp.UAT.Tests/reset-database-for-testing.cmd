@echo off
echo ========================================
echo Resetting Database for Testing
echo ========================================
echo.

echo Step 1: Navigating to main application directory...
cd /d "C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"

echo Step 2: Stopping any running application processes...
taskkill /f /im dotnet.exe 2>nul
taskkill /f /im GCCWebApp.exe 2>nul

echo Step 3: Deleting existing database file...
if exist "gcc_database.db" (
    del "gcc_database.db"
    echo   ✓ Database file deleted
) else (
    echo   ℹ No existing database file found
)

if exist "gcc_database.db-shm" del "gcc_database.db-shm"
if exist "gcc_database.db-wal" del "gcc_database.db-wal"

echo Step 4: Building the application...
dotnet build --configuration Release

echo Step 5: Running the application to trigger database seeding...
echo   ℹ The application will start and seed the database with proper password hashes
echo   ℹ After you see "Application started" message, you can close it with Ctrl+C
echo.

dotnet run

echo.
echo ========================================
echo Database reset complete!
echo ========================================
echo.
echo The database now has users with proper password hashes:
echo   admin@gcc.com:TestPass123!
echo   analyst@gcc.com:TestPass123!
echo   external@company.com:TestPass123!
echo.
echo You can now run your screenshot tests!
echo.
pause
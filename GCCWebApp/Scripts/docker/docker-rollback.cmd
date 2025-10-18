@echo off
setlocal enabledelayedexpansion

REM =================================================================
REM GCC Docker Test Environment - Rollback Script
REM =================================================================
echo.
echo ============================================================
echo   GCC Docker Test - Rollback and Recovery
echo ============================================================
echo.

REM Set variables
set PROJECT_NAME=gcc-test
set COMPOSE_FILE=docker-compose.test.yml
set BACKUP_DIR=backups
set LOG_FILE=logs\rollback_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.log
set LOG_FILE=!LOG_FILE: =0!

REM Create log directory
if not exist "logs" mkdir "logs"

REM Start logging
echo ============================================================ >> "%LOG_FILE%"
echo Rollback Script Started: %date% %time% >> "%LOG_FILE%"
echo ============================================================ >> "%LOG_FILE%"

REM Check if Docker is running
docker ps >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker is not running.
    pause
    exit /b 1
)

REM Menu
:menu
cls
echo ============================================================
echo   GCC Docker Test - Rollback Menu
echo ============================================================
echo.
echo Current Status:
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}" 2>nul || echo No containers running
echo.
echo ============================================================
echo.
echo 1. Quick Rollback (Last Known Good)
echo 2. List Available Backups
echo 3. Rollback to Specific Backup
echo 4. Emergency Stop All Services
echo 5. Restore Database Only
echo 6. Rollback Application Only
echo 7. View Rollback History
echo 8. Create Manual Backup
echo 9. Disaster Recovery
echo 0. Exit
echo.
set /p CHOICE="Select option: "

if "%CHOICE%"=="1" goto quick_rollback
if "%CHOICE%"=="2" goto list_backups
if "%CHOICE%"=="3" goto specific_rollback
if "%CHOICE%"=="4" goto emergency_stop
if "%CHOICE%"=="5" goto restore_database
if "%CHOICE%"=="6" goto rollback_app
if "%CHOICE%"=="7" goto view_history
if "%CHOICE%"=="8" goto manual_backup
if "%CHOICE%"=="9" goto disaster_recovery
if "%CHOICE%"=="0" exit /b 0
goto menu

REM ============= Quick Rollback =============
:quick_rollback
cls
echo ============================================================
echo   Quick Rollback - Last Known Good Configuration
echo ============================================================
echo.

REM Find the most recent backup
set LATEST_BACKUP=
for /d %%d in (%BACKUP_DIR%\test_*) do set LATEST_BACKUP=%%d

if "%LATEST_BACKUP%"=="" (
    echo [ERROR] No backups found.
    echo Please create a backup first or use manual recovery.
    pause
    goto menu
)

echo Found latest backup: %LATEST_BACKUP%
echo.
echo This will:
echo   1. Stop current containers
echo   2. Restore from: %LATEST_BACKUP%
echo   3. Restart services with previous configuration
echo.
set /p CONFIRM="Continue with rollback? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Rollback cancelled.
    pause
    goto menu
)

echo.
echo [STEP 1/4] Stopping current services...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down >> "%LOG_FILE%" 2>&1
echo [SUCCESS] Services stopped.

echo [STEP 2/4] Restoring database backup...
if exist "%LATEST_BACKUP%\database_backup.bak" (
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d sqlserver-test >> "%LOG_FILE%" 2>&1
    timeout /t 10 /nobreak >nul
    
    docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "RESTORE DATABASE [GCC_Test] FROM DISK = N'/var/opt/mssql/data/backup_restore.bak' WITH REPLACE" >> "%LOG_FILE%" 2>&1
    if %errorLevel% equ 0 (
        echo [SUCCESS] Database restored.
    ) else (
        echo [WARNING] Database restore failed.
    )
) else (
    echo [WARNING] No database backup found.
)

echo [STEP 3/4] Restoring application image...
if exist "%LATEST_BACKUP%\image_tags.txt" (
    set /p IMAGE_TAG=<"%LATEST_BACKUP%\image_tags.txt"
    docker tag gcc-webapp:!IMAGE_TAG! gcc-webapp:test-latest >> "%LOG_FILE%" 2>&1
    echo [SUCCESS] Application image restored to !IMAGE_TAG!
) else (
    echo [WARNING] No image backup found.
)

echo [STEP 4/4] Starting services with restored configuration...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d >> "%LOG_FILE%" 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Services started.
) else (
    echo [ERROR] Failed to start services.
)

echo.
echo Rollback completed. Checking health...
timeout /t 10 /nobreak >nul
curl -f -s http://localhost:5102/health >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Application is healthy after rollback.
) else (
    echo [WARNING] Application health check failed.
)

echo.
echo Rollback completed at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= List Backups =============
:list_backups
cls
echo ============================================================
echo   Available Backups
echo ============================================================
echo.

if not exist "%BACKUP_DIR%" (
    echo No backup directory found.
    pause
    goto menu
)

echo Backup Name                    Date         Size
echo ------------------------------------------------
for /d %%d in (%BACKUP_DIR%\test_*) do (
    set BACKUP_NAME=%%~nxd
    set BACKUP_DATE=%%~td
    
    REM Calculate backup size
    set SIZE=0
    for /r "%%d" %%f in (*) do set /a SIZE+=%%~zf
    set /a SIZE_MB=!SIZE!/1048576
    
    echo !BACKUP_NAME!    !BACKUP_DATE!    !SIZE_MB! MB
)

echo.
echo Docker Image Backups:
docker images gcc-webapp:test-* --format "table {{.Repository}}:{{.Tag}}\t{{.CreatedAt}}\t{{.Size}}"

echo.
pause
goto menu

REM ============= Specific Rollback =============
:specific_rollback
cls
echo ============================================================
echo   Rollback to Specific Backup
echo ============================================================
echo.

echo Available backups:
set COUNT=0
for /d %%d in (%BACKUP_DIR%\test_*) do (
    set /a COUNT+=1
    echo !COUNT!. %%~nxd
    set BACKUP_!COUNT!=%%d
)

if %COUNT%==0 (
    echo No backups found.
    pause
    goto menu
)

echo.
set /p BACKUP_NUM="Select backup number (1-%COUNT%): "

if not defined BACKUP_%BACKUP_NUM% (
    echo Invalid selection.
    pause
    goto menu
)

set SELECTED_BACKUP=!BACKUP_%BACKUP_NUM%!
echo.
echo Selected: %SELECTED_BACKUP%
echo.
set /p CONFIRM="Rollback to this backup? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Rollback cancelled.
    pause
    goto menu
)

REM Perform rollback to selected backup
echo.
echo Rolling back to: %SELECTED_BACKUP%
echo.

call :perform_rollback "%SELECTED_BACKUP%"

echo.
echo Rollback completed.
pause
goto menu

REM ============= Emergency Stop =============
:emergency_stop
cls
echo ============================================================
echo   Emergency Stop - All Services
echo ============================================================
echo.
echo WARNING: This will immediately stop all Docker containers!
echo.
set /p CONFIRM="Are you sure? (yes/no): "
if /i not "%CONFIRM%"=="yes" (
    echo Emergency stop cancelled.
    pause
    goto menu
)

echo.
echo [EMERGENCY] Stopping all GCC test containers...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% kill >> "%LOG_FILE%" 2>&1
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down -v >> "%LOG_FILE%" 2>&1

echo [EMERGENCY] Force stopping any remaining containers...
for /f "tokens=1" %%c in ('docker ps -q --filter "name=gcc-"') do (
    docker kill %%c >> "%LOG_FILE%" 2>&1
    echo Killed container: %%c
)

echo.
echo [COMPLETE] All services stopped.
echo Emergency stop at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Restore Database Only =============
:restore_database
cls
echo ============================================================
echo   Restore Database Only
echo ============================================================
echo.

echo Available database backups:
set COUNT=0
for /r "%BACKUP_DIR%" %%f in (database_backup.bak) do (
    set /a COUNT+=1
    echo !COUNT!. %%f
    set DB_BACKUP_!COUNT!=%%f
)

if %COUNT%==0 (
    echo No database backups found.
    pause
    goto menu
)

echo.
set /p BACKUP_NUM="Select database backup (1-%COUNT%): "

if not defined DB_BACKUP_%BACKUP_NUM% (
    echo Invalid selection.
    pause
    goto menu
)

set SELECTED_DB_BACKUP=!DB_BACKUP_%BACKUP_NUM%!
echo.
echo Selected: %SELECTED_DB_BACKUP%
echo.
set /p CONFIRM="Restore this database backup? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Database restore cancelled.
    pause
    goto menu
)

echo.
echo [STEP 1/3] Ensuring SQL Server is running...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d sqlserver-test >> "%LOG_FILE%" 2>&1
timeout /t 10 /nobreak >nul

echo [STEP 2/3] Copying backup file to container...
docker cp "%SELECTED_DB_BACKUP%" gcc-sqlserver-test:/var/opt/mssql/data/restore.bak >> "%LOG_FILE%" 2>&1

echo [STEP 3/3] Restoring database...
docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "ALTER DATABASE [GCC_Test] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [GCC_Test] FROM DISK = N'/var/opt/mssql/data/restore.bak' WITH REPLACE; ALTER DATABASE [GCC_Test] SET MULTI_USER;" >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo [SUCCESS] Database restored successfully.
) else (
    echo [ERROR] Database restore failed. Check logs for details.
)

echo.
pause
goto menu

REM ============= Rollback Application Only =============
:rollback_app
cls
echo ============================================================
echo   Rollback Application Only
echo ============================================================
echo.

echo Available application images:
docker images gcc-webapp:test-* --format "table {{.Tag}}\t{{.CreatedAt}}\t{{.Size}}"

echo.
set /p IMAGE_TAG="Enter image tag to rollback to (e.g., test-20250830_1234): "

docker images gcc-webapp:%IMAGE_TAG% --format "{{.ID}}" >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Image gcc-webapp:%IMAGE_TAG% not found.
    pause
    goto menu
)

echo.
echo This will rollback the application to: gcc-webapp:%IMAGE_TAG%
set /p CONFIRM="Continue? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Application rollback cancelled.
    pause
    goto menu
)

echo.
echo [STEP 1/3] Stopping application container...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% stop gccwebapp-test >> "%LOG_FILE%" 2>&1

echo [STEP 2/3] Updating image tag...
docker tag gcc-webapp:%IMAGE_TAG% gcc-webapp:test-latest >> "%LOG_FILE%" 2>&1

echo [STEP 3/3] Starting application with rolled back image...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d gccwebapp-test >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo [SUCCESS] Application rolled back to %IMAGE_TAG%.
) else (
    echo [ERROR] Application rollback failed.
)

echo.
pause
goto menu

REM ============= View History =============
:view_history
cls
echo ============================================================
echo   Rollback History
echo ============================================================
echo.

if exist "%LOG_FILE%" (
    echo Recent rollback operations:
    echo ---------------------------
    findstr /i "rollback started completed failed" logs\rollback_*.log 2>nul | more
) else (
    echo No rollback history found.
)

echo.
echo Deployment History:
echo -------------------
for /f "tokens=*" %%f in ('dir /b /o-d logs\docker-deploy-test_*.log 2^>nul') do (
    echo %%f
    findstr "completed" "logs\%%f" 2>nul
)

echo.
pause
goto menu

REM ============= Manual Backup =============
:manual_backup
cls
echo ============================================================
echo   Create Manual Backup
echo ============================================================
echo.

set BACKUP_NAME=test_manual_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_NAME=!BACKUP_NAME: =0!
set MANUAL_BACKUP_DIR=%BACKUP_DIR%\%BACKUP_NAME%

echo Creating backup: %BACKUP_NAME%
echo.

if not exist "%MANUAL_BACKUP_DIR%" mkdir "%MANUAL_BACKUP_DIR%"

echo [STEP 1/4] Backing up database...
docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "BACKUP DATABASE [GCC_Test] TO DISK = N'/var/opt/mssql/data/manual_backup.bak' WITH FORMAT, INIT" >> "%LOG_FILE%" 2>&1
if %errorLevel% equ 0 (
    docker cp gcc-sqlserver-test:/var/opt/mssql/data/manual_backup.bak "%MANUAL_BACKUP_DIR%\database_backup.bak" >> "%LOG_FILE%" 2>&1
    echo [SUCCESS] Database backed up.
) else (
    echo [WARNING] Database backup failed.
)

echo [STEP 2/4] Saving current image tags...
docker images gcc-webapp:test-latest --format "{{.Tag}}" > "%MANUAL_BACKUP_DIR%\image_tags.txt"
echo [SUCCESS] Image tags saved.

echo [STEP 3/4] Exporting container configurations...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% config > "%MANUAL_BACKUP_DIR%\docker-compose-config.yml" 2>nul
echo [SUCCESS] Configuration exported.

echo [STEP 4/4] Saving environment info...
(
echo Backup Date: %date% %time%
echo.
echo Container Status:
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% ps
echo.
echo Image Info:
docker images gcc-webapp:test-* --format "table {{.Repository}}:{{.Tag}}\t{{.ID}}\t{{.Size}}"
) > "%MANUAL_BACKUP_DIR%\backup_info.txt"
echo [SUCCESS] Environment info saved.

echo.
echo Manual backup completed: %MANUAL_BACKUP_DIR%
echo Backup created at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Disaster Recovery =============
:disaster_recovery
cls
echo ============================================================
echo   Disaster Recovery
echo ============================================================
echo.
echo This will attempt to recover from a complete failure.
echo.
echo Recovery options:
echo 1. Rebuild from source code
echo 2. Pull from Docker registry
echo 3. Restore from file backup
echo 4. Clean slate (remove everything and start fresh)
echo 0. Cancel
echo.
set /p DR_CHOICE="Select recovery option: "

if "%DR_CHOICE%"=="1" (
    echo.
    echo [DR] Rebuilding from source code...
    
    echo [1/4] Cleaning Docker environment...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down -v --remove-orphans >> "%LOG_FILE%" 2>&1
    docker system prune -f >> "%LOG_FILE%" 2>&1
    
    echo [2/4] Rebuilding application image...
    docker build -t gcc-webapp:test-recovery -t gcc-webapp:test-latest . >> "%LOG_FILE%" 2>&1
    
    echo [3/4] Starting infrastructure...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d sqlserver-test redis-test >> "%LOG_FILE%" 2>&1
    timeout /t 15 /nobreak >nul
    
    echo [4/4] Starting application...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d >> "%LOG_FILE%" 2>&1
    
    echo [DR] Recovery complete.
    
) else if "%DR_CHOICE%"=="2" (
    echo.
    set /p REGISTRY="Enter Docker registry URL: "
    set /p IMAGE="Enter image name:tag: "
    
    echo [DR] Pulling from registry...
    docker pull %REGISTRY%/%IMAGE% >> "%LOG_FILE%" 2>&1
    docker tag %REGISTRY%/%IMAGE% gcc-webapp:test-latest >> "%LOG_FILE%" 2>&1
    
    echo [DR] Starting services...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d >> "%LOG_FILE%" 2>&1
    
    echo [DR] Recovery complete.
    
) else if "%DR_CHOICE%"=="3" (
    echo.
    set /p BACKUP_PATH="Enter full path to backup file: "
    
    if exist "%BACKUP_PATH%" (
        echo [DR] Restoring from backup...
        call :perform_rollback "%BACKUP_PATH%"
        echo [DR] Recovery complete.
    ) else (
        echo [ERROR] Backup file not found.
    )
    
) else if "%DR_CHOICE%"=="4" (
    echo.
    echo WARNING: This will remove ALL Docker resources for the test environment!
    set /p CONFIRM="Are you absolutely sure? (type 'yes' to confirm): "
    
    if /i "%CONFIRM%"=="yes" (
        echo [DR] Performing clean slate recovery...
        
        echo [1/5] Removing all containers...
        docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down -v --remove-orphans >> "%LOG_FILE%" 2>&1
        
        echo [2/5] Removing all images...
        for /f "tokens=1" %%i in ('docker images gcc-webapp:test-* -q') do docker rmi -f %%i >> "%LOG_FILE%" 2>&1
        
        echo [3/5] Removing all volumes...
        for /f "tokens=1" %%v in ('docker volume ls -q ^| findstr gcc-test') do docker volume rm %%v >> "%LOG_FILE%" 2>&1
        
        echo [4/5] Removing network...
        docker network rm gcc-test-network >> "%LOG_FILE%" 2>&1
        
        echo [5/5] Clean rebuild...
        call docker-deploy-test.cmd
        
        echo [DR] Clean slate recovery complete.
    ) else (
        echo [DR] Clean slate recovery cancelled.
    )
) else (
    echo [DR] Disaster recovery cancelled.
)

echo.
echo Disaster recovery at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Helper Functions =============

:perform_rollback
set BACKUP_PATH=%~1

echo Performing rollback from: %BACKUP_PATH%

REM Stop current services
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down >> "%LOG_FILE%" 2>&1

REM Restore database if backup exists
if exist "%BACKUP_PATH%\database_backup.bak" (
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d sqlserver-test >> "%LOG_FILE%" 2>&1
    timeout /t 10 /nobreak >nul
    docker cp "%BACKUP_PATH%\database_backup.bak" gcc-sqlserver-test:/var/opt/mssql/data/restore.bak >> "%LOG_FILE%" 2>&1
    docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "RESTORE DATABASE [GCC_Test] FROM DISK = N'/var/opt/mssql/data/restore.bak' WITH REPLACE" >> "%LOG_FILE%" 2>&1
)

REM Restore image tags if available
if exist "%BACKUP_PATH%\image_tags.txt" (
    set /p IMAGE_TAG=<"%BACKUP_PATH%\image_tags.txt"
    docker tag gcc-webapp:!IMAGE_TAG! gcc-webapp:test-latest >> "%LOG_FILE%" 2>&1
)

REM Restart all services
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d >> "%LOG_FILE%" 2>&1

exit /b 0
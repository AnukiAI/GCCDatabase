@echo off
setlocal enabledelayedexpansion

REM =================================================================
REM GCC Docker Test Environment - Cleanup and Maintenance Script
REM =================================================================
echo.
echo ============================================================
echo   GCC Docker Test - Cleanup and Maintenance
echo ============================================================
echo.

REM Set variables
set PROJECT_NAME=gcc-test
set COMPOSE_FILE=docker-compose.test.yml
set LOG_FILE=logs\cleanup_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.log
set LOG_FILE=!LOG_FILE: =0!
set BACKUP_DIR=backups
set RETENTION_DAYS=7

REM Create log directory
if not exist "logs" mkdir "logs"

REM Start logging
echo ============================================================ >> "%LOG_FILE%"
echo Cleanup Script Started: %date% %time% >> "%LOG_FILE%"
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
echo   GCC Docker Test - Cleanup and Maintenance Menu
echo ============================================================
echo.
echo Current Docker Usage:
docker system df
echo.
echo ============================================================
echo.
echo 1. Quick Cleanup (Remove stopped containers and unused images)
echo 2. Deep Clean (Remove all test resources)
echo 3. Clean Logs and Temp Files
echo 4. Prune Old Backups
echo 5. Container Maintenance
echo 6. Volume Cleanup
echo 7. Network Cleanup
echo 8. Full System Prune (CAUTION)
echo 9. Maintenance Report
echo 0. Exit
echo.
set /p CHOICE="Select option: "

if "%CHOICE%"=="1" goto quick_cleanup
if "%CHOICE%"=="2" goto deep_clean
if "%CHOICE%"=="3" goto clean_logs
if "%CHOICE%"=="4" goto prune_backups
if "%CHOICE%"=="5" goto container_maintenance
if "%CHOICE%"=="6" goto volume_cleanup
if "%CHOICE%"=="7" goto network_cleanup
if "%CHOICE%"=="8" goto system_prune
if "%CHOICE%"=="9" goto maintenance_report
if "%CHOICE%"=="0" exit /b 0
goto menu

REM ============= Quick Cleanup =============
:quick_cleanup
cls
echo ============================================================
echo   Quick Cleanup
echo ============================================================
echo.

echo [STEP 1/5] Removing stopped containers...
docker container prune -f >> "%LOG_FILE%" 2>&1
for /f "tokens=1" %%c in ('docker ps -a -q -f status^=exited -f name^=gcc-') do (
    docker rm %%c >> "%LOG_FILE%" 2>&1
    echo Removed container: %%c
)

echo [STEP 2/5] Removing dangling images...
docker image prune -f >> "%LOG_FILE%" 2>&1

echo [STEP 3/5] Removing unused networks...
docker network prune -f >> "%LOG_FILE%" 2>&1

echo [STEP 4/5] Removing dangling volumes...
docker volume prune -f >> "%LOG_FILE%" 2>&1

echo [STEP 5/5] Cleaning build cache...
docker builder prune -f --filter "until=24h" >> "%LOG_FILE%" 2>&1

echo.
echo Space reclaimed:
docker system df
echo.
echo Quick cleanup completed at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Deep Clean =============
:deep_clean
cls
echo ============================================================
echo   Deep Clean - Remove All Test Resources
echo ============================================================
echo.
echo WARNING: This will remove:
echo   - All test containers (running and stopped)
echo   - All test images
echo   - All test volumes
echo   - All test networks
echo.
set /p CONFIRM="Are you sure you want to continue? (yes/no): "
if /i not "%CONFIRM%"=="yes" (
    echo Deep clean cancelled.
    pause
    goto menu
)

echo.
echo [STEP 1/6] Stopping all test containers...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down -v >> "%LOG_FILE%" 2>&1

echo [STEP 2/6] Removing all test containers...
for /f "tokens=1" %%c in ('docker ps -a -q -f name^=gcc-') do (
    docker rm -f %%c >> "%LOG_FILE%" 2>&1
    echo Removed container: %%c
)

echo [STEP 3/6] Removing all test images...
for /f "tokens=1" %%i in ('docker images gcc-webapp:test-* -q') do (
    docker rmi -f %%i >> "%LOG_FILE%" 2>&1
    echo Removed image: %%i
)

echo [STEP 4/6] Removing all test volumes...
for /f "tokens=1" %%v in ('docker volume ls -q ^| findstr gcc-test') do (
    docker volume rm -f %%v >> "%LOG_FILE%" 2>&1
    echo Removed volume: %%v
)

echo [STEP 5/6] Removing test network...
docker network rm gcc-test-network >> "%LOG_FILE%" 2>&1

echo [STEP 6/6] Cleaning Docker builder cache...
docker builder prune -a -f >> "%LOG_FILE%" 2>&1

echo.
echo Deep clean completed. Space reclaimed:
docker system df
echo.
echo Deep clean completed at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Clean Logs =============
:clean_logs
cls
echo ============================================================
echo   Clean Logs and Temp Files
echo ============================================================
echo.

echo Current log directory size:
for /f "tokens=3" %%s in ('dir logs /s /-c 2^>nul ^| findstr "File(s)"') do set LOG_SIZE=%%s
echo Logs: %LOG_SIZE% bytes
echo.

echo [STEP 1/4] Archiving old logs...
set ARCHIVE_NAME=logs_archive_%date:~-4,4%%date:~-10,2%%date:~-7,2%.zip
if exist "logs\*.log" (
    powershell -Command "Compress-Archive -Path 'logs\*.log' -DestinationPath 'logs\archives\%ARCHIVE_NAME%' -Force" 2>nul
    echo Logs archived to: logs\archives\%ARCHIVE_NAME%
)

echo [STEP 2/4] Removing old log files (older than %RETENTION_DAYS% days)...
forfiles /p "logs" /s /m *.log /d -%RETENTION_DAYS% /c "cmd /c del @path" 2>nul

echo [STEP 3/4] Truncating container logs...
for /f "tokens=1" %%c in ('docker ps -q -f name^=gcc-') do (
    docker exec %%c sh -c "truncate -s 0 /app/logs/*.log" 2>nul
    echo Truncated logs in container: %%c
)

echo [STEP 4/4] Cleaning temporary files...
if exist "temp" rd /s /q "temp" 2>nul
if exist "*.tmp" del /q "*.tmp" 2>nul

echo.
echo Log cleanup completed.
pause
goto menu

REM ============= Prune Backups =============
:prune_backups
cls
echo ============================================================
echo   Prune Old Backups
echo ============================================================
echo.

if not exist "%BACKUP_DIR%" (
    echo No backup directory found.
    pause
    goto menu
)

echo Current backup directory size:
for /f "tokens=3" %%s in ('dir %BACKUP_DIR% /s /-c 2^>nul ^| findstr "File(s)"') do set BACKUP_SIZE=%%s
set /a BACKUP_SIZE_MB=%BACKUP_SIZE%/1048576
echo Total size: %BACKUP_SIZE_MB% MB
echo.

echo Backups older than %RETENTION_DAYS% days will be removed:
echo ------------------------------------------------
forfiles /p "%BACKUP_DIR%" /d -%RETENTION_DAYS% /c "cmd /c echo @path" 2>nul

echo.
set /p CONFIRM="Remove old backups? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Backup pruning cancelled.
    pause
    goto menu
)

echo.
echo Removing old backups...
forfiles /p "%BACKUP_DIR%" /d -%RETENTION_DAYS% /c "cmd /c rd /s /q @path" 2>nul

echo Removing old Docker image backups...
for /f "tokens=1,2" %%a in ('docker images gcc-webapp:test-backup-* --format "{{.ID}} {{.CreatedAt}}"') do (
    REM This is simplified - would need more complex date parsing for production
    docker rmi %%a >> "%LOG_FILE%" 2>&1
)

echo.
echo Backup pruning completed.
pause
goto menu

REM ============= Container Maintenance =============
:container_maintenance
cls
echo ============================================================
echo   Container Maintenance
echo ============================================================
echo.

echo Running Containers:
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Size}}"
echo.

echo Container Health:
for /f "tokens=1" %%c in ('docker ps -q -f name^=gcc-') do (
    echo.
    docker inspect %%c --format="{{.Name}}: Health={{.State.Health.Status}}, Restarts={{.RestartCount}}"
    
    REM Check if container needs restart due to unhealthy state
    docker inspect %%c --format="{{.State.Health.Status}}" | findstr "unhealthy" >nul
    if !errorLevel! equ 0 (
        echo   [WARNING] Container is unhealthy. Restarting...
        docker restart %%c >> "%LOG_FILE%" 2>&1
    )
)

echo.
echo Checking for updates...
for %%i in (mcr.microsoft.com/mssql/server:2022-latest redis:7-alpine nginx:alpine) do (
    echo Checking %%i...
    docker pull %%i --quiet >> "%LOG_FILE%" 2>&1
    if !errorLevel! equ 0 (
        echo   [INFO] %%i is up to date or updated
    ) else (
        echo   [WARNING] Could not check %%i
    )
)

echo.
echo Container maintenance completed.
pause
goto menu

REM ============= Volume Cleanup =============
:volume_cleanup
cls
echo ============================================================
echo   Volume Cleanup
echo ============================================================
echo.

echo Docker Volumes:
docker volume ls --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}"
echo.

echo Test Environment Volumes:
docker volume ls | findstr gcc-test
echo.

echo Checking for orphaned volumes...
set ORPHANED_COUNT=0
for /f "tokens=1" %%v in ('docker volume ls -q -f dangling^=true') do (
    set /a ORPHANED_COUNT+=1
    echo Orphaned: %%v
)

if %ORPHANED_COUNT% gtr 0 (
    echo.
    echo Found %ORPHANED_COUNT% orphaned volumes.
    set /p REMOVE_ORPHANED="Remove orphaned volumes? (y/n): "
    if /i "!REMOVE_ORPHANED!"=="y" (
        docker volume prune -f >> "%LOG_FILE%" 2>&1
        echo Orphaned volumes removed.
    )
) else (
    echo No orphaned volumes found.
)

echo.
echo Volume sizes (test volumes):
for /f "tokens=1" %%v in ('docker volume ls -q ^| findstr gcc-test') do (
    docker run --rm -v %%v:/data alpine du -sh /data 2>nul | findstr /v "^$"
)

echo.
pause
goto menu

REM ============= Network Cleanup =============
:network_cleanup
cls
echo ============================================================
echo   Network Cleanup
echo ============================================================
echo.

echo Docker Networks:
docker network ls
echo.

echo Checking for unused networks...
docker network prune --dry-run
echo.

set /p CLEAN_NETWORKS="Remove unused networks? (y/n): "
if /i "%CLEAN_NETWORKS%"=="y" (
    docker network prune -f >> "%LOG_FILE%" 2>&1
    echo Unused networks removed.
)

echo.
echo Inspecting test network...
docker network inspect gcc-test-network --format="Subnet: {{range .IPAM.Config}}{{.Subnet}}{{end}}" 2>nul
docker network inspect gcc-test-network --format="Connected: {{len .Containers}} containers" 2>nul

echo.
pause
goto menu

REM ============= System Prune =============
:system_prune
cls
echo ============================================================
echo   Full System Prune - CAUTION
echo ============================================================
echo.
echo WARNING: This will remove:
echo   - All stopped containers
echo   - All networks not used by containers
echo   - All dangling images
echo   - All dangling build cache
echo   - All unused volumes (with confirmation)
echo.
echo This affects ALL Docker resources, not just test environment!
echo.
set /p CONFIRM="Are you absolutely sure? (type 'CONFIRM' to proceed): "
if not "%CONFIRM%"=="CONFIRM" (
    echo System prune cancelled.
    pause
    goto menu
)

echo.
echo Performing Docker system prune...

echo [1/2] System prune without volumes...
docker system prune -a -f >> "%LOG_FILE%" 2>&1

echo [2/2] Volume prune...
set /p PRUNE_VOLUMES="Also remove unused volumes? (y/n): "
if /i "%PRUNE_VOLUMES%"=="y" (
    docker volume prune -f >> "%LOG_FILE%" 2>&1
)

echo.
echo System prune completed. Space reclaimed:
docker system df
echo.
pause
goto menu

REM ============= Maintenance Report =============
:maintenance_report
cls
echo ============================================================
echo   Maintenance Report
echo ============================================================
echo.

set REPORT_FILE=reports\maintenance_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.txt
set REPORT_FILE=!REPORT_FILE: =0!
if not exist "reports" mkdir "reports"

echo Generating maintenance report...
(
echo ============================================================
echo GCC Docker Test Environment - Maintenance Report
echo Generated: %date% %time%
echo ============================================================
echo.

echo DOCKER SYSTEM INFORMATION
echo -------------------------
docker version
echo.
docker system info --format "Containers: {{.Containers}}"
docker system info --format "Images: {{.Images}}"
docker system info --format "Server Version: {{.ServerVersion}}"
echo.

echo DISK USAGE
echo ----------
docker system df
echo.

echo TEST ENVIRONMENT STATUS
echo -----------------------
echo Containers:
docker ps -a --filter "name=gcc-" --format "table {{.Names}}\t{{.Status}}\t{{.Size}}"
echo.

echo Images:
docker images gcc-webapp:* --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
echo.

echo Volumes:
docker volume ls | findstr gcc-test
echo.

echo Networks:
docker network ls | findstr gcc-test
echo.

echo LOG FILES
echo ---------
echo Directory: logs\
dir logs /o-d 2>nul | findstr ".log"
echo.

echo BACKUP FILES
echo ------------
echo Directory: %BACKUP_DIR%\
dir %BACKUP_DIR% /o-d 2>nul | findstr "test_"
echo.

echo RESOURCE CONSUMPTION
echo --------------------
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
echo.

echo RECOMMENDED ACTIONS
echo -------------------
set CLEANUP_NEEDED=false

REM Check for old backups
for /f %%x in ('forfiles /p "%BACKUP_DIR%" /d -%RETENTION_DAYS% /c "cmd /c echo 1" 2^>nul ^| find /c "1"') do (
    if %%x gtr 0 (
        echo - Remove old backups (%%x backups older than %RETENTION_DAYS% days)
        set CLEANUP_NEEDED=true
    )
)

REM Check for dangling images
for /f %%x in ('docker images -f dangling^=true -q ^| find /c /v ""') do (
    if %%x gtr 0 (
        echo - Remove dangling images (%%x found)
        set CLEANUP_NEEDED=true
    )
)

REM Check for stopped containers
for /f %%x in ('docker ps -a -q -f status^=exited -f name^=gcc- ^| find /c /v ""') do (
    if %%x gtr 0 (
        echo - Remove stopped containers (%%x found)
        set CLEANUP_NEEDED=true
    )
)

if "%CLEANUP_NEEDED%"=="false" (
    echo No maintenance actions required.
)

echo.
echo ============================================================
echo End of Report
echo ============================================================
) > "%REPORT_FILE%"

echo.
echo Report saved to: %REPORT_FILE%
echo.
type "%REPORT_FILE%" | more
echo.
pause
goto menu
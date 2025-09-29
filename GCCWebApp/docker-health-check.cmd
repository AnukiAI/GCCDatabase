@echo off
setlocal enabledelayedexpansion

REM =================================================================
REM GCC Docker Test Environment - Health Check & Monitoring Script
REM =================================================================
echo.
echo ============================================================
echo   GCC Docker Test - Health Check and Monitoring
echo ============================================================
echo.

REM Set variables
set PROJECT_NAME=gcc-test
set COMPOSE_FILE=docker-compose.test.yml
set CHECK_INTERVAL=5
set MAX_CHECKS=12
set LOG_FILE=logs\health-check_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.log
set LOG_FILE=!LOG_FILE: =0!

REM Create log directory
if not exist "logs" mkdir "logs"

REM Start logging
echo ============================================================ >> "%LOG_FILE%"
echo Health Check Started: %date% %time% >> "%LOG_FILE%"
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
echo   GCC Docker Test - Health Check Menu
echo ============================================================
echo.
echo 1. Quick Health Check (All Services)
echo 2. Detailed Health Status
echo 3. Container Resource Usage
echo 4. View Application Logs
echo 5. Database Connectivity Test
echo 6. API Endpoint Tests
echo 7. Performance Metrics
echo 8. Continuous Monitoring Mode
echo 9. Generate Health Report
echo 0. Exit
echo.
set /p CHOICE="Select option: "

if "%CHOICE%"=="1" goto quick_check
if "%CHOICE%"=="2" goto detailed_status
if "%CHOICE%"=="3" goto resource_usage
if "%CHOICE%"=="4" goto view_logs
if "%CHOICE%"=="5" goto db_test
if "%CHOICE%"=="6" goto api_tests
if "%CHOICE%"=="7" goto performance
if "%CHOICE%"=="8" goto monitoring
if "%CHOICE%"=="9" goto report
if "%CHOICE%"=="0" exit /b 0
goto menu

REM ============= Quick Health Check =============
:quick_check
cls
echo ============================================================
echo   Quick Health Check
echo ============================================================
echo.

echo Checking services status...
echo.

REM Check each service
call :check_service gcc-webapp-test "Web Application"
call :check_service gcc-sqlserver-test "SQL Server"
call :check_service gcc-redis-test "Redis Cache"
call :check_service gcc-nginx-test "Nginx Proxy"

echo.
echo Checking endpoints...
echo.

REM Check endpoints
call :check_endpoint "http://localhost:5102/health" "Application Health"
call :check_endpoint "http://localhost:5102" "Application Home"
call :check_endpoint "http://localhost:8090" "Nginx Proxy"

echo.
echo Quick check completed at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Detailed Status =============
:detailed_status
cls
echo ============================================================
echo   Detailed Health Status
echo ============================================================
echo.

echo Container Status:
echo -----------------
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% ps
echo.

echo Health Status:
echo --------------
for %%c in (gcc-webapp-test gcc-sqlserver-test gcc-redis-test) do (
    echo.
    echo %%c:
    docker inspect %%c --format="  Status: {{.State.Status}}" 2>nul
    docker inspect %%c --format="  Health: {{.State.Health.Status}}" 2>nul
    docker inspect %%c --format="  Started: {{.State.StartedAt}}" 2>nul
    docker inspect %%c --format="  Restarts: {{.RestartCount}}" 2>nul
)

echo.
echo Network Status:
echo ---------------
docker network inspect gcc-test-network --format="Connected Containers: {{len .Containers}}" 2>nul
docker network ls | findstr gcc-test-network

echo.
echo Volume Status:
echo --------------
docker volume ls | findstr gcc-test

echo.
pause
goto menu

REM ============= Resource Usage =============
:resource_usage
cls
echo ============================================================
echo   Container Resource Usage
echo ============================================================
echo.
echo Press Ctrl+C to stop monitoring...
echo.

docker stats --format "table {{.Container}}\t{{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}\t{{.BlockIO}}"

pause
goto menu

REM ============= View Logs =============
:view_logs
cls
echo ============================================================
echo   Application Logs
echo ============================================================
echo.
echo 1. GCC Web Application
echo 2. SQL Server
echo 3. Redis Cache
echo 4. Nginx Proxy
echo 5. All Services
echo 0. Back to Menu
echo.
set /p LOG_CHOICE="Select service: "

if "%LOG_CHOICE%"=="1" (
    echo.
    echo Showing last 50 lines of Web Application logs...
    echo Press Ctrl+C to stop...
    docker logs --tail 50 -f gcc-webapp-test
) else if "%LOG_CHOICE%"=="2" (
    echo.
    echo Showing last 50 lines of SQL Server logs...
    docker logs --tail 50 gcc-sqlserver-test
) else if "%LOG_CHOICE%"=="3" (
    echo.
    echo Showing last 50 lines of Redis logs...
    docker logs --tail 50 gcc-redis-test
) else if "%LOG_CHOICE%"=="4" (
    echo.
    echo Showing last 50 lines of Nginx logs...
    docker logs --tail 50 gcc-nginx-test
) else if "%LOG_CHOICE%"=="5" (
    echo.
    echo Showing logs for all services...
    echo Press Ctrl+C to stop...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% logs --tail 50 -f
) else (
    goto menu
)

pause
goto menu

REM ============= Database Test =============
:db_test
cls
echo ============================================================
echo   Database Connectivity Test
echo ============================================================
echo.

echo Testing SQL Server connection...
docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "SELECT @@VERSION" 2>nul
if %errorLevel% equ 0 (
    echo [SUCCESS] SQL Server connection successful.
    echo.
    
    echo Database Information:
    docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "SELECT name, state_desc, size * 8 / 1024 AS 'Size (MB)' FROM sys.master_files WHERE database_id = DB_ID('GCC_Test')" 2>nul
    echo.
    
    echo Table Count:
    docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -d GCC_Test -Q "SELECT COUNT(*) as TableCount FROM sys.tables" 2>nul
    echo.
    
    echo Active Connections:
    docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "SELECT COUNT(*) as ActiveConnections FROM sys.dm_exec_connections WHERE database_id = DB_ID('GCC_Test')" 2>nul
) else (
    echo [ERROR] SQL Server connection failed.
)

echo.
echo Testing Redis connection...
docker exec gcc-redis-test redis-cli -a TestRedisP@ss123 ping 2>nul | findstr "PONG" >nul
if %errorLevel% equ 0 (
    echo [SUCCESS] Redis connection successful.
    
    docker exec gcc-redis-test redis-cli -a TestRedisP@ss123 INFO server 2>nul | findstr "redis_version"
    docker exec gcc-redis-test redis-cli -a TestRedisP@ss123 DBSIZE 2>nul
) else (
    echo [ERROR] Redis connection failed.
)

echo.
pause
goto menu

REM ============= API Tests =============
:api_tests
cls
echo ============================================================
echo   API Endpoint Tests
echo ============================================================
echo.

echo Testing API endpoints...
echo.

REM Health endpoint
echo [1/8] Health Check Endpoint:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/health

REM Home page
echo [2/8] Home Page:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/

REM Login page
echo [3/8] Login Page:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/Account/Login

REM API endpoints
echo [4/8] API Certificates:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/api/v1/certificates

echo [5/8] API Companies:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/api/v1/companies

echo [6/8] API Products:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/api/v1/products

REM Static resources
echo [7/8] CSS Resources:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/css/site.css

echo [8/8] JS Resources:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/js/site.js

echo.
echo API tests completed at %time% >> "%LOG_FILE%"
pause
goto menu

REM ============= Performance Metrics =============
:performance
cls
echo ============================================================
echo   Performance Metrics
echo ============================================================
echo.

echo Collecting performance data...
echo.

REM Response time tests
echo Response Time Tests:
echo --------------------
for /l %%i in (1,1,5) do (
    echo Test %%i:
    curl -s -o nul -w "  Home: %%{time_total}s | " http://localhost:5102/
    curl -s -o nul -w "Health: %%{time_total}s | " http://localhost:5102/health
    curl -s -o nul -w "API: %%{time_total}s\n" http://localhost:5102/api/v1/certificates
    timeout /t 1 /nobreak >nul
)

echo.
echo Container Metrics:
echo ------------------
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"

echo.
echo Database Performance:
echo ---------------------
docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "SELECT TOP 5 total_worker_time/execution_count AS AvgCPU, total_elapsed_time/execution_count AS AvgDuration, execution_count, SUBSTRING(st.text, (qs.statement_start_offset/2)+1, 50) AS QueryText FROM sys.dm_exec_query_stats AS qs CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st ORDER BY total_worker_time/execution_count DESC" 2>nul

echo.
pause
goto menu

REM ============= Continuous Monitoring =============
:monitoring
cls
echo ============================================================
echo   Continuous Monitoring Mode
echo ============================================================
echo.
echo Monitoring will check health every %CHECK_INTERVAL% seconds.
echo Press Ctrl+C to stop monitoring...
echo.

set COUNTER=0
:monitor_loop
cls
echo ============================================================
echo   Monitoring Status - Check #!COUNTER! - %time%
echo ============================================================
echo.

REM Quick status check
echo Service Status:
for %%c in (gcc-webapp-test gcc-sqlserver-test gcc-redis-test gcc-nginx-test) do (
    docker inspect %%c --format="%%c: {{.State.Status}} | Health: {{.State.Health.Status}}" 2>nul || echo %%c: Not Found
)

echo.
echo Endpoint Status:
curl -s -o nul -w "App Health: %%{http_code}\n" http://localhost:5102/health
curl -s -o nul -w "Home Page: %%{http_code}\n" http://localhost:5102/
curl -s -o nul -w "API: %%{http_code}\n" http://localhost:5102/api/v1/certificates

echo.
echo Resource Usage:
docker stats --no-stream --format "{{.Name}}: CPU {{.CPUPerc}} | Mem {{.MemUsage}}"

set /a COUNTER+=1
if %COUNTER% geq %MAX_CHECKS% (
    echo.
    echo Maximum checks reached. Returning to menu...
    pause
    goto menu
)

timeout /t %CHECK_INTERVAL% /nobreak >nul
goto monitor_loop

REM ============= Generate Report =============
:report
cls
echo ============================================================
echo   Generating Health Report
echo ============================================================
echo.

set REPORT_FILE=reports\health-report_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.txt
set REPORT_FILE=!REPORT_FILE: =0!
if not exist "reports" mkdir "reports"

echo Generating comprehensive health report...
(
echo ============================================================
echo GCC Docker Test Environment - Health Report
echo Generated: %date% %time%
echo ============================================================
echo.
echo EXECUTIVE SUMMARY
echo -----------------
echo Environment: TEST
echo Docker Compose Project: %PROJECT_NAME%
echo.

echo CONTAINER STATUS
echo ----------------
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% ps
echo.

echo HEALTH STATUS
echo -------------
for %%c in (gcc-webapp-test gcc-sqlserver-test gcc-redis-test gcc-nginx-test) do (
    echo.
    echo %%c:
    docker inspect %%c --format="  Status: {{.State.Status}}"
    docker inspect %%c --format="  Health: {{.State.Health.Status}}"
    docker inspect %%c --format="  Uptime: {{.State.StartedAt}}"
    docker inspect %%c --format="  Restarts: {{.RestartCount}}"
)
echo.

echo RESOURCE USAGE
echo --------------
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
echo.

echo ENDPOINT TESTS
echo --------------
echo Health Check:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/health
echo Home Page:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/
echo API Endpoint:
curl -s -o nul -w "  Status: %%{http_code} | Response Time: %%{time_total}s\n" http://localhost:5102/api/v1/certificates
echo.

echo DATABASE STATUS
echo ---------------
docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "SELECT 'Database' = name, 'Status' = state_desc, 'Size (MB)' = size * 8 / 1024 FROM sys.master_files WHERE database_id = DB_ID('GCC_Test')"
echo.

echo RECENT LOGS (Last 10 lines)
echo ---------------------------
echo Web Application:
docker logs --tail 10 gcc-webapp-test 2>&1
echo.
echo SQL Server:
docker logs --tail 10 gcc-sqlserver-test 2>&1
echo.

echo ============================================================
echo End of Report
echo ============================================================
) > "%REPORT_FILE%"

echo [SUCCESS] Report generated: %REPORT_FILE%
echo.
type "%REPORT_FILE%" | more
echo.
pause
goto menu

REM ============= Helper Functions =============

:check_service
set CONTAINER=%1
set SERVICE_NAME=%2
docker ps --format "{{.Names}}" | findstr /i "%CONTAINER%" >nul
if %errorLevel% equ 0 (
    docker inspect %CONTAINER% --format="{{.State.Health.Status}}" 2>nul | findstr "healthy" >nul
    if !errorLevel! equ 0 (
        echo [OK]     %SERVICE_NAME% is healthy
    ) else (
        echo [WARN]   %SERVICE_NAME% is running but not healthy
    )
) else (
    echo [ERROR]  %SERVICE_NAME% is not running
)
exit /b 0

:check_endpoint
set URL=%~1
set ENDPOINT_NAME=%2
curl -f -s -o nul %URL%
if %errorLevel% equ 0 (
    echo [OK]     %ENDPOINT_NAME%
) else (
    echo [ERROR]  %ENDPOINT_NAME%
)
exit /b 0
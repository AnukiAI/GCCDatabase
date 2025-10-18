@echo off
setlocal enabledelayedexpansion

REM =================================================================
REM GCC Web Application - Docker Test Environment Deployment
REM =================================================================
echo.
echo ============================================================
echo   GCC Web Application - Docker Test Deployment Script
echo ============================================================
echo.

REM Check if Docker is installed and running
docker --version >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker is not installed or not in PATH.
    echo Please install Docker Desktop from https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

docker ps >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker daemon is not running.
    echo Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo [INFO] Docker is installed and running.
echo.

REM Set deployment variables
set ENVIRONMENT=Test
set COMPOSE_FILE=docker-compose.test.yml
set ENV_FILE=.env.test
set PROJECT_NAME=gcc-test
set BUILD_NUMBER=%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BUILD_NUMBER=!BUILD_NUMBER: =0!
set BACKUP_DIR=backups\test_%BUILD_NUMBER%
set LOG_FILE=logs\docker-deploy-test_%BUILD_NUMBER%.log

REM Create necessary directories
if not exist "logs" mkdir "logs"
if not exist "backups" mkdir "backups"
if not exist "nginx" mkdir "nginx"
if not exist "Scripts" mkdir "Scripts"

REM Start logging
echo ============================================================ >> "%LOG_FILE%"
echo Docker Test Deployment Started: %date% %time% >> "%LOG_FILE%"
echo ============================================================ >> "%LOG_FILE%"

REM Display deployment configuration
echo =================================================================
echo DEPLOYMENT CONFIGURATION
echo =================================================================
echo Environment:      %ENVIRONMENT%
echo Compose File:     %COMPOSE_FILE%
echo Environment File: %ENV_FILE%
echo Project Name:     %PROJECT_NAME%
echo Build Number:     %BUILD_NUMBER%
echo Log File:         %LOG_FILE%
echo =================================================================
echo.

REM Check for environment file
if not exist "%ENV_FILE%" (
    echo [WARNING] Environment file %ENV_FILE% not found.
    echo [INFO] Creating default environment file...
    call :create_default_env_file
    echo [SUCCESS] Created %ENV_FILE% with default values.
    echo [ACTION] Please review and update the environment variables before deployment.
    echo.
)

REM Confirm deployment
echo This will deploy the TEST environment using Docker Compose.
echo.
echo Services to be deployed:
echo   - GCC Web Application (Port 5102/5103)
echo   - SQL Server Test Database (Port 1434)
echo   - Redis Cache (Port 6380)
echo   - Nginx Proxy (Port 8090/8091)
echo.
set /p CONFIRM="Continue with deployment? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Deployment cancelled by user. >> "%LOG_FILE%"
    echo [INFO] Deployment cancelled.
    exit /b 0
)
echo.

REM Step 1: Backup current deployment if exists
echo [STEP 1/10] Checking for existing deployment...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% ps >nul 2>&1
if %errorLevel% equ 0 (
    echo [INFO] Found existing deployment. Creating backup...
    
    REM Export volumes
    echo [INFO] Backing up Docker volumes...
    if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"
    
    REM Backup database
    docker exec gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestP@ssw0rd123! -Q "BACKUP DATABASE [GCC_Test] TO DISK = N'/var/opt/mssql/data/backup_test.bak' WITH FORMAT" >nul 2>&1
    docker cp gcc-sqlserver-test:/var/opt/mssql/data/backup_test.bak "%BACKUP_DIR%\database_backup.bak" >nul 2>&1
    
    REM Save current image tags
    docker images gcc-webapp:test-* --format "{{.Tag}}" > "%BACKUP_DIR%\image_tags.txt"
    
    echo [SUCCESS] Backup created in %BACKUP_DIR% >> "%LOG_FILE%"
    echo [SUCCESS] Backup created.
) else (
    echo [INFO] No existing deployment found. >> "%LOG_FILE%"
    echo [INFO] No existing deployment found.
)
echo.

REM Step 2: Stop existing containers
echo [STEP 2/10] Stopping existing containers...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down --remove-orphans >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Existing containers stopped. >> "%LOG_FILE%"
    echo [SUCCESS] Containers stopped.
) else (
    echo [INFO] No containers to stop. >> "%LOG_FILE%"
    echo [INFO] No containers to stop.
)
echo.

REM Step 3: Build application image
echo [STEP 3/10] Building Docker image...
echo [INFO] This may take several minutes...
docker build -t gcc-webapp:test-%BUILD_NUMBER% -t gcc-webapp:test-latest --build-arg BUILD_CONFIGURATION=Release . >> "%LOG_FILE%" 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker build failed. Check %LOG_FILE% for details. >> "%LOG_FILE%"
    echo [ERROR] Docker build failed. Check the log file for details.
    echo Log file: %LOG_FILE%
    pause
    exit /b 1
)
echo [SUCCESS] Docker image built: gcc-webapp:test-%BUILD_NUMBER% >> "%LOG_FILE%"
echo [SUCCESS] Docker image built successfully.
echo.

REM Step 4: Scan image for vulnerabilities (optional)
echo [STEP 4/10] Scanning image for vulnerabilities...
docker scan gcc-webapp:test-latest --severity high >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] No high severity vulnerabilities found. >> "%LOG_FILE%"
    echo [SUCCESS] Security scan passed.
) else (
    echo [WARNING] Security scan unavailable or vulnerabilities found. >> "%LOG_FILE%"
    echo [WARNING] Security scan skipped or vulnerabilities detected.
    echo [INFO] Run 'docker scan gcc-webapp:test-latest' for details.
)
echo.

REM Step 5: Create/Update network
echo [STEP 5/10] Setting up Docker network...
docker network create gcc-test-network >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Network created. >> "%LOG_FILE%"
    echo [SUCCESS] Network created.
) else (
    echo [INFO] Network already exists. >> "%LOG_FILE%"
    echo [INFO] Using existing network.
)
echo.

REM Step 6: Start infrastructure services
echo [STEP 6/10] Starting infrastructure services...
echo [INFO] Starting SQL Server...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d sqlserver-test >> "%LOG_FILE%" 2>&1
timeout /t 10 /nobreak >nul

echo [INFO] Starting Redis...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d redis-test >> "%LOG_FILE%" 2>&1
timeout /t 5 /nobreak >nul

echo [INFO] Waiting for services to be healthy...
call :wait_for_service sqlserver-test 60
call :wait_for_service redis-test 30

echo [SUCCESS] Infrastructure services started. >> "%LOG_FILE%"
echo [SUCCESS] Infrastructure ready.
echo.

REM Step 7: Run database migrations
echo [STEP 7/10] Running database migrations...
docker run --rm ^
    --network gcc-test-network ^
    -e ASPNETCORE_ENVIRONMENT=Test ^
    -e ConnectionStrings__DefaultConnection="Server=sqlserver-test;Database=GCC_Test;User Id=sa;Password=TestP@ssw0rd123!;TrustServerCertificate=true" ^
    gcc-webapp:test-latest ^
    dotnet ef database update >> "%LOG_FILE%" 2>&1

if %errorLevel% neq 0 (
    echo [WARNING] Migration failed or not configured. >> "%LOG_FILE%"
    echo [WARNING] Database migration skipped.
) else (
    echo [SUCCESS] Database migrated. >> "%LOG_FILE%"
    echo [SUCCESS] Database migrated.
)
echo.

REM Step 8: Start application
echo [STEP 8/10] Starting application services...
docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% up -d gccwebapp-test nginx-test >> "%LOG_FILE%" 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Failed to start application services. >> "%LOG_FILE%"
    echo [ERROR] Application startup failed. Check logs for details.
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% logs --tail=50
    pause
    exit /b 1
)
echo [SUCCESS] Application services started. >> "%LOG_FILE%"
echo [SUCCESS] Application started.
echo.

REM Step 9: Health checks
echo [STEP 9/10] Running health checks...
timeout /t 10 /nobreak >nul

echo [INFO] Checking application health...
curl -f -s http://localhost:5102/health >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Application health check passed. >> "%LOG_FILE%"
    echo [SUCCESS] Application is healthy.
) else (
    echo [WARNING] Application health check failed. >> "%LOG_FILE%"
    echo [WARNING] Application may still be starting...
)

echo [INFO] Checking database connectivity...
docker exec gcc-webapp-test curl -s http://localhost:8080/api/health/db >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Database connectivity verified. >> "%LOG_FILE%"
    echo [SUCCESS] Database connection verified.
) else (
    echo [WARNING] Database connectivity check failed. >> "%LOG_FILE%"
    echo [WARNING] Database connection not verified.
)
echo.

REM Step 10: Seed test data (optional)
echo [STEP 10/10] Seeding test data...
set /p SEED_DATA="Seed test data? (y/n): "
if /i "%SEED_DATA%"=="y" (
    echo [INFO] Seeding test data...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% run --rm test-seeder >> "%LOG_FILE%" 2>&1
    if %errorLevel% equ 0 (
        echo [SUCCESS] Test data seeded. >> "%LOG_FILE%"
        echo [SUCCESS] Test data seeded.
    ) else (
        echo [WARNING] Test data seeding failed. >> "%LOG_FILE%"
        echo [WARNING] Could not seed test data.
    )
) else (
    echo [INFO] Skipping test data seeding. >> "%LOG_FILE%"
    echo [INFO] Test data seeding skipped.
)
echo.

REM Display deployment summary
echo =================================================================
echo DEPLOYMENT COMPLETED SUCCESSFULLY!
echo =================================================================
echo.
echo Environment:        TEST (Docker)
echo Build Number:       %BUILD_NUMBER%
echo.
echo Access URLs:
echo   Application:      http://localhost:5102
echo   Application SSL:  https://localhost:5103
echo   Proxy:           http://localhost:8090
echo   Proxy SSL:       https://localhost:8091
echo.
echo Database:
echo   SQL Server:      localhost,1434
echo   Database Name:   GCC_Test
echo   SA Password:     (check .env.test file)
echo.
echo Redis Cache:
echo   Host:            localhost:6380
echo   Password:        (check .env.test file)
echo.
echo Test Credentials:
echo   Admin:           admin@greencirclecertified.com / Admin123!
echo   Analyst:         analyst@greencirclecertified.com / Analyst123!
echo   External:        partner@amazon.com / Partner123!
echo.
echo Docker Commands:
echo   View logs:       docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% logs -f
echo   Stop all:        docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% down
echo   Restart app:     docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% restart gccwebapp-test
echo   Shell access:    docker exec -it gcc-webapp-test /bin/bash
echo   DB access:       docker exec -it gcc-sqlserver-test /opt/mssql-tools/bin/sqlcmd -S localhost -U sa
echo.
echo Monitoring:
echo   Container status:  docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% ps
echo   Resource usage:    docker stats
echo   Health status:     curl http://localhost:5102/health
echo.
echo Log Files:
echo   Deployment log:    %LOG_FILE%
echo   Application logs:  docker logs gcc-webapp-test
echo   SQL Server logs:   docker logs gcc-sqlserver-test
echo =================================================================
echo.

REM Completion time
echo Deployment completed at: %date% %time% >> "%LOG_FILE%"
echo ============================================================ >> "%LOG_FILE%"

REM Offer to open browser
set /p OPEN_BROWSER="Open application in browser? (y/n): "
if /i "%OPEN_BROWSER%"=="y" (
    echo Opening TEST environment...
    start http://localhost:5102
)

REM Offer to view logs
echo.
set /p VIEW_LOGS="View application logs? (y/n): "
if /i "%VIEW_LOGS%"=="y" (
    echo.
    echo Press Ctrl+C to stop viewing logs...
    docker-compose -f %COMPOSE_FILE% -p %PROJECT_NAME% logs -f --tail=50 gccwebapp-test
)

echo.
echo Deployment script completed!
pause
exit /b 0

REM ============= Helper Functions =============

:wait_for_service
set SERVICE_NAME=%1
set MAX_WAIT=%2
set COUNTER=0

:wait_loop
docker inspect %SERVICE_NAME% --format="{{.State.Health.Status}}" 2>nul | findstr "healthy" >nul
if %errorLevel% equ 0 (
    echo [SUCCESS] %SERVICE_NAME% is healthy.
    exit /b 0
)

set /a COUNTER+=5
if %COUNTER% geq %MAX_WAIT% (
    echo [WARNING] %SERVICE_NAME% health check timeout.
    exit /b 1
)

echo [INFO] Waiting for %SERVICE_NAME%... (%COUNTER%/%MAX_WAIT% seconds)
timeout /t 5 /nobreak >nul
goto wait_loop

:create_default_env_file
(
echo # GCC Test Environment Configuration
echo # Generated: %date% %time%
echo.
echo # Database Configuration
echo DB_SA_PASSWORD=TestP@ssw0rd123!
echo.
echo # JWT Configuration
echo JWT_SECRET_KEY=test-jwt-secret-key-min-64-chars-for-security-requirements-padding-here-1234567890
echo.
echo # Redis Configuration
echo REDIS_PASSWORD=TestRedisP@ss123
echo.
echo # SMTP Configuration (Mailtrap example)
echo SMTP_HOST=smtp.mailtrap.io
echo SMTP_PORT=587
echo SMTP_USERNAME=your_mailtrap_username
echo SMTP_PASSWORD=your_mailtrap_password
echo SMTP_FROM_EMAIL=test@greencirclecertified.com
echo.
echo # Feature Flags
echo ENABLE_2FA=false
echo.
echo # Build Information
echo BUILD_NUMBER=%BUILD_NUMBER%
) > "%ENV_FILE%"
exit /b 0
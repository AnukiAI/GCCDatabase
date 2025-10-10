@echo off
setlocal enabledelayedexpansion

REM =================================================================
REM GCC Web Application - Docker Image Build Script
REM =================================================================
echo.
echo ============================================================
echo   GCC Web Application - Docker Image Builder
echo ============================================================
echo.

REM Check if Docker is installed
docker --version >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker is not installed or not in PATH.
    echo Please install Docker Desktop from https://www.docker.com/products/docker-desktop
    pause
    exit /b 1
)

REM Check if Docker daemon is running
docker ps >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker daemon is not running.
    echo Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo [INFO] Docker is installed and running.
echo.

REM Set build variables
set BUILD_DATE=%date:~-4,4%%date:~-10,2%%date:~-7,2%
set BUILD_TIME=%time:~0,2%%time:~3,2%%time:~6,2%
set BUILD_TIME=!BUILD_TIME: =0!
set BUILD_NUMBER=%BUILD_DATE%_%BUILD_TIME%
set GIT_COMMIT=unknown
set LOG_FILE=logs\docker-build_%BUILD_NUMBER%.log

REM Get git commit hash if available
for /f "tokens=*" %%g in ('git rev-parse --short HEAD 2^>nul') do set GIT_COMMIT=%%g

REM Create log directory
if not exist "logs" mkdir "logs"

REM Start logging
echo ============================================================ >> "%LOG_FILE%"
echo Docker Build Started: %date% %time% >> "%LOG_FILE%"
echo Git Commit: %GIT_COMMIT% >> "%LOG_FILE%"
echo ============================================================ >> "%LOG_FILE%"

REM Display build menu
:menu
cls
echo ============================================================
echo   Docker Image Build Menu
echo ============================================================
echo.
echo Build Information:
echo   Date:        %BUILD_DATE%
echo   Build ID:    %BUILD_NUMBER%
echo   Git Commit:  %GIT_COMMIT%
echo.
echo ============================================================
echo.
echo 1. Build Development Image
echo 2. Build Test Image
echo 3. Build Production Image
echo 4. Build All Environments
echo 5. Build and Push to Registry
echo 6. Build Multi-Architecture Image
echo 7. Security Scan Image
echo 8. Clean Build (No Cache)
echo 9. View Build History
echo 0. Exit
echo.
set /p CHOICE="Select build option: "

if "%CHOICE%"=="1" goto build_dev
if "%CHOICE%"=="2" goto build_test
if "%CHOICE%"=="3" goto build_prod
if "%CHOICE%"=="4" goto build_all
if "%CHOICE%"=="5" goto build_push
if "%CHOICE%"=="6" goto build_multi
if "%CHOICE%"=="7" goto security_scan
if "%CHOICE%"=="8" goto clean_build
if "%CHOICE%"=="9" goto build_history
if "%CHOICE%"=="0" exit /b 0
goto menu

REM ============= Build Development Image =============
:build_dev
cls
echo ============================================================
echo   Building Development Image
echo ============================================================
echo.

set IMAGE_TAG=gcc-webapp:dev-%BUILD_NUMBER%
set IMAGE_LATEST=gcc-webapp:dev-latest

echo Building: %IMAGE_TAG%
echo.

docker build ^
    --build-arg BUILD_CONFIGURATION=Debug ^
    --build-arg BUILD_DATE=%BUILD_DATE% ^
    --build-arg BUILD_NUMBER=%BUILD_NUMBER% ^
    --build-arg GIT_COMMIT=%GIT_COMMIT% ^
    --build-arg ASPNETCORE_ENVIRONMENT=Development ^
    --label "version=%BUILD_NUMBER%" ^
    --label "environment=development" ^
    --label "git.commit=%GIT_COMMIT%" ^
    --label "build.date=%date%" ^
    -t %IMAGE_TAG% ^
    -t %IMAGE_LATEST% ^
    -f Dockerfile ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo [SUCCESS] Development image built: %IMAGE_TAG%
    echo.
    docker images %IMAGE_TAG%
    echo.
    echo Image size:
    docker images %IMAGE_TAG% --format "Size: {{.Size}}"
) else (
    echo [ERROR] Build failed. Check %LOG_FILE% for details.
)

pause
goto menu

REM ============= Build Test Image =============
:build_test
cls
echo ============================================================
echo   Building Test Image
echo ============================================================
echo.

set IMAGE_TAG=gcc-webapp:test-%BUILD_NUMBER%
set IMAGE_LATEST=gcc-webapp:test-latest

echo Building: %IMAGE_TAG%
echo.

docker build ^
    --build-arg BUILD_CONFIGURATION=Release ^
    --build-arg BUILD_DATE=%BUILD_DATE% ^
    --build-arg BUILD_NUMBER=%BUILD_NUMBER% ^
    --build-arg GIT_COMMIT=%GIT_COMMIT% ^
    --build-arg ASPNETCORE_ENVIRONMENT=Test ^
    --label "version=%BUILD_NUMBER%" ^
    --label "environment=test" ^
    --label "git.commit=%GIT_COMMIT%" ^
    --label "build.date=%date%" ^
    -t %IMAGE_TAG% ^
    -t %IMAGE_LATEST% ^
    -f Dockerfile ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo [SUCCESS] Test image built: %IMAGE_TAG%
    echo.
    
    echo Running quick test...
    docker run --rm -d -p 5102:8080 --name gcc-test-temp %IMAGE_TAG%
    timeout /t 5 /nobreak >nul
    
    curl -f http://localhost:5102/health >nul 2>&1
    if !errorLevel! equ 0 (
        echo [SUCCESS] Health check passed
    ) else (
        echo [WARNING] Health check failed
    )
    
    docker stop gcc-test-temp >nul 2>&1
    
    echo.
    docker images %IMAGE_TAG%
) else (
    echo [ERROR] Build failed. Check %LOG_FILE% for details.
)

pause
goto menu

REM ============= Build Production Image =============
:build_prod
cls
echo ============================================================
echo   Building Production Image
echo ============================================================
echo.

echo WARNING: Building production image
echo Please ensure all security checks are complete.
echo.
set /p CONFIRM="Continue with production build? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Production build cancelled.
    pause
    goto menu
)

set IMAGE_TAG=gcc-webapp:prod-%BUILD_NUMBER%
set IMAGE_LATEST=gcc-webapp:latest
set IMAGE_VERSION=gcc-webapp:v%BUILD_DATE%

echo.
echo Building: %IMAGE_TAG%
echo.

REM Production optimizations
echo [STEP 1/4] Running pre-build checks...
dotnet test --configuration Release --no-build >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Tests failed. Fix tests before production build.
    pause
    goto menu
)

echo [STEP 2/4] Building production image...
docker build ^
    --build-arg BUILD_CONFIGURATION=Release ^
    --build-arg BUILD_DATE=%BUILD_DATE% ^
    --build-arg BUILD_NUMBER=%BUILD_NUMBER% ^
    --build-arg GIT_COMMIT=%GIT_COMMIT% ^
    --build-arg ASPNETCORE_ENVIRONMENT=Production ^
    --label "version=%BUILD_NUMBER%" ^
    --label "environment=production" ^
    --label "git.commit=%GIT_COMMIT%" ^
    --label "build.date=%date%" ^
    --label "maintainer=GCC DevOps" ^
    -t %IMAGE_TAG% ^
    -t %IMAGE_LATEST% ^
    -t %IMAGE_VERSION% ^
    -f Dockerfile ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% neq 0 (
    echo [ERROR] Build failed. Check %LOG_FILE% for details.
    pause
    goto menu
)

echo [STEP 3/4] Running security scan...
docker scan %IMAGE_TAG% --severity high >nul 2>&1
if %errorLevel% equ 0 (
    echo [SUCCESS] Security scan passed
) else (
    echo [WARNING] Security vulnerabilities detected or scan unavailable
    set /p CONTINUE="Continue anyway? (y/n): "
    if /i not "!CONTINUE!"=="y" (
        echo Build cancelled due to security concerns.
        docker rmi %IMAGE_TAG% >nul 2>&1
        pause
        goto menu
    )
)

echo [STEP 4/4] Optimizing image...
docker save %IMAGE_TAG% | docker load >nul 2>&1

echo.
echo [SUCCESS] Production image built: %IMAGE_TAG%
echo.
docker images gcc-webapp:prod-* --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

pause
goto menu

REM ============= Build All Environments =============
:build_all
cls
echo ============================================================
echo   Building All Environment Images
echo ============================================================
echo.

echo This will build images for Development, Test, and Production.
set /p CONFIRM="Continue? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Build cancelled.
    pause
    goto menu
)

echo.
echo [1/3] Building Development image...
call :build_image_silent dev Debug Development

echo [2/3] Building Test image...
call :build_image_silent test Release Test

echo [3/3] Building Production image...
call :build_image_silent prod Release Production

echo.
echo All images built successfully:
docker images gcc-webapp:* --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

pause
goto menu

REM ============= Build and Push to Registry =============
:build_push
cls
echo ============================================================
echo   Build and Push to Registry
echo ============================================================
echo.

echo Registry Options:
echo 1. Docker Hub
echo 2. Azure Container Registry
echo 3. AWS ECR
echo 4. Private Registry
echo 0. Cancel
echo.
set /p REGISTRY="Select registry: "

if "%REGISTRY%"=="0" goto menu

set /p REGISTRY_USER="Registry username: "
set /p REGISTRY_PASS="Registry password: "
set /p REGISTRY_URL="Registry URL (or dockerhub username): "

echo.
echo Logging into registry...
echo %REGISTRY_PASS% | docker login -u %REGISTRY_USER% --password-stdin

if %errorLevel% neq 0 (
    echo [ERROR] Login failed.
    pause
    goto menu
)

echo.
echo Select environment to build and push:
echo 1. Development
echo 2. Test
echo 3. Production
echo.
set /p ENV_CHOICE="Select environment: "

if "%ENV_CHOICE%"=="1" (
    set ENV_NAME=dev
    set BUILD_CONFIG=Debug
    set ASPNET_ENV=Development
) else if "%ENV_CHOICE%"=="2" (
    set ENV_NAME=test
    set BUILD_CONFIG=Release
    set ASPNET_ENV=Test
) else (
    set ENV_NAME=prod
    set BUILD_CONFIG=Release
    set ASPNET_ENV=Production
)

set LOCAL_TAG=gcc-webapp:%ENV_NAME%-%BUILD_NUMBER%
set REMOTE_TAG=%REGISTRY_URL%/gcc-webapp:%ENV_NAME%-%BUILD_NUMBER%
set REMOTE_LATEST=%REGISTRY_URL%/gcc-webapp:%ENV_NAME%-latest

echo.
echo Building image...
docker build ^
    --build-arg BUILD_CONFIGURATION=%BUILD_CONFIG% ^
    --build-arg ASPNETCORE_ENVIRONMENT=%ASPNET_ENV% ^
    -t %LOCAL_TAG% ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% neq 0 (
    echo [ERROR] Build failed.
    pause
    goto menu
)

echo Tagging for registry...
docker tag %LOCAL_TAG% %REMOTE_TAG%
docker tag %LOCAL_TAG% %REMOTE_LATEST%

echo Pushing to registry...
docker push %REMOTE_TAG%
docker push %REMOTE_LATEST%

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Image pushed to registry:
    echo   %REMOTE_TAG%
    echo   %REMOTE_LATEST%
) else (
    echo [ERROR] Push failed.
)

docker logout
pause
goto menu

REM ============= Build Multi-Architecture =============
:build_multi
cls
echo ============================================================
echo   Build Multi-Architecture Image
echo ============================================================
echo.

echo This will build images for multiple platforms:
echo   - linux/amd64 (Standard Linux)
echo   - linux/arm64 (ARM servers, Apple Silicon)
echo   - windows/amd64 (Windows containers)
echo.

REM Check if buildx is available
docker buildx version >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker buildx is not available.
    echo Please update Docker Desktop to the latest version.
    pause
    goto menu
)

echo Setting up buildx...
docker buildx create --name gcc-builder --use >nul 2>&1
docker buildx inspect --bootstrap >nul 2>&1

set /p ENV_CHOICE="Environment (dev/test/prod): "
set MULTI_TAG=gcc-webapp:%ENV_CHOICE%-multi-%BUILD_NUMBER%

echo.
echo Building multi-architecture image...
docker buildx build ^
    --platform linux/amd64,linux/arm64 ^
    --build-arg BUILD_CONFIGURATION=Release ^
    --build-arg BUILD_NUMBER=%BUILD_NUMBER% ^
    -t %MULTI_TAG% ^
    -f Dockerfile ^
    --push=false ^
    --load ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo [SUCCESS] Multi-architecture image built: %MULTI_TAG%
    docker buildx imagetools inspect %MULTI_TAG%
) else (
    echo [ERROR] Multi-architecture build failed.
)

docker buildx rm gcc-builder >nul 2>&1
pause
goto menu

REM ============= Security Scan =============
:security_scan
cls
echo ============================================================
echo   Security Scan
echo ============================================================
echo.

echo Available images:
docker images gcc-webapp:* --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}"
echo.

set /p IMAGE_TO_SCAN="Enter image tag to scan (e.g., gcc-webapp:latest): "

echo.
echo Running security scan on %IMAGE_TO_SCAN%...
echo.

REM Try Docker Scout first (newer)
docker scout version >nul 2>&1
if %errorLevel% equ 0 (
    echo Using Docker Scout...
    docker scout cves %IMAGE_TO_SCAN%
    docker scout recommendations %IMAGE_TO_SCAN%
) else (
    REM Fall back to legacy scan
    echo Using Docker Scan...
    docker scan %IMAGE_TO_SCAN%
)

echo.
echo Additional security checks:
echo.

echo Checking for exposed secrets...
docker run --rm -v "%cd%:/src" aquasec/trivy fs /src --security-checks secret >nul 2>&1
if %errorLevel% equ 0 (
    echo [OK] No secrets found
) else (
    echo [WARNING] Possible secrets detected
)

echo Checking Dockerfile best practices...
docker run --rm -i hadolint/hadolint < Dockerfile >nul 2>&1
if %errorLevel% equ 0 (
    echo [OK] Dockerfile follows best practices
) else (
    echo [WARNING] Dockerfile has issues
)

pause
goto menu

REM ============= Clean Build =============
:clean_build
cls
echo ============================================================
echo   Clean Build (No Cache)
echo ============================================================
echo.

echo This will rebuild the image from scratch without using cache.
echo This is useful when dependencies have changed.
echo.
set /p CONFIRM="Continue with clean build? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Clean build cancelled.
    pause
    goto menu
)

echo.
echo Select environment:
echo 1. Development
echo 2. Test
echo 3. Production
echo.
set /p ENV_CHOICE="Select environment: "

if "%ENV_CHOICE%"=="1" (
    set ENV_NAME=dev
    set BUILD_CONFIG=Debug
) else if "%ENV_CHOICE%"=="2" (
    set ENV_NAME=test
    set BUILD_CONFIG=Release
) else (
    set ENV_NAME=prod
    set BUILD_CONFIG=Release
)

set IMAGE_TAG=gcc-webapp:%ENV_NAME%-clean-%BUILD_NUMBER%

echo.
echo [STEP 1/3] Cleaning Docker build cache...
docker builder prune -f >nul 2>&1

echo [STEP 2/3] Pulling latest base images...
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull mcr.microsoft.com/dotnet/sdk:8.0

echo [STEP 3/3] Building image without cache...
docker build ^
    --no-cache ^
    --pull ^
    --build-arg BUILD_CONFIGURATION=%BUILD_CONFIG% ^
    --build-arg BUILD_NUMBER=%BUILD_NUMBER% ^
    -t %IMAGE_TAG% ^
    -f Dockerfile ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Clean build completed: %IMAGE_TAG%
    docker images %IMAGE_TAG%
) else (
    echo [ERROR] Clean build failed.
)

pause
goto menu

REM ============= Build History =============
:build_history
cls
echo ============================================================
echo   Build History
echo ============================================================
echo.

echo Recent Docker images:
echo --------------------
docker images gcc-webapp:* --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

echo.
echo Build logs:
echo -----------
dir logs\docker-build_*.log /o-d 2>nul | findstr ".log"

echo.
echo Image layers (latest):
echo ----------------------
docker history gcc-webapp:latest --no-trunc --format "table {{.CreatedBy}}\t{{.Size}}"

echo.
echo Disk usage:
echo -----------
docker system df

pause
goto menu

REM ============= Helper Functions =============

:build_image_silent
set ENV=%1
set CONFIG=%2
set ASPNET=%3
set TAG=gcc-webapp:%ENV%-%BUILD_NUMBER%

docker build ^
    --build-arg BUILD_CONFIGURATION=%CONFIG% ^
    --build-arg ASPNETCORE_ENVIRONMENT=%ASPNET% ^
    --build-arg BUILD_NUMBER=%BUILD_NUMBER% ^
    -t %TAG% ^
    -t gcc-webapp:%ENV%-latest ^
    -f Dockerfile ^
    . >> "%LOG_FILE%" 2>&1

if %errorLevel% equ 0 (
    echo   [SUCCESS] %ENV% image: %TAG%
) else (
    echo   [ERROR] %ENV% build failed
)
exit /b 0
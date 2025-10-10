@echo off
setlocal enabledelayedexpansion

REM =================================================================
REM GCC Web Application - Docker Registry Push Script
REM =================================================================
echo.
echo ============================================================
echo   GCC Docker Registry Push Manager
echo ============================================================
echo.

REM Check Docker availability
docker --version >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Docker is not installed.
    pause
    exit /b 1
)

REM Set variables
set BUILD_DATE=%date:~-4,4%%date:~-10,2%%date:~-7,2%
set BUILD_TIME=%time:~0,2%%time:~3,2%%time:~6,2%
set BUILD_TIME=!BUILD_TIME: =0!
set BUILD_NUMBER=%BUILD_DATE%_%BUILD_TIME%
set LOG_FILE=logs\docker-push_%BUILD_NUMBER%.log

if not exist "logs" mkdir "logs"

REM Load configuration if exists
if exist ".docker-registry.conf" (
    for /f "tokens=1,2 delims==" %%a in (.docker-registry.conf) do set %%a=%%b
    echo [INFO] Loaded registry configuration
)

:menu
cls
echo ============================================================
echo   Docker Registry Push Manager
echo ============================================================
echo.
echo Available Images:
docker images gcc-webapp:* --format "table {{.Repository}}:{{.Tag}}\t{{.Size}}"
echo.
echo ============================================================
echo.
echo 1. Push to Docker Hub
echo 2. Push to Azure Container Registry
echo 3. Push to AWS ECR
echo 4. Push to Google Container Registry
echo 5. Push to Private Registry
echo 6. Configure Registry Settings
echo 7. Push Multiple Tags
echo 8. Automated CI/CD Push
echo 0. Exit
echo.
set /p CHOICE="Select option: "

if "%CHOICE%"=="1" goto docker_hub
if "%CHOICE%"=="2" goto azure_acr
if "%CHOICE%"=="3" goto aws_ecr
if "%CHOICE%"=="4" goto google_gcr
if "%CHOICE%"=="5" goto private_registry
if "%CHOICE%"=="6" goto configure
if "%CHOICE%"=="7" goto multi_push
if "%CHOICE%"=="8" goto cicd_push
if "%CHOICE%"=="0" exit /b 0
goto menu

REM ============= Docker Hub =============
:docker_hub
cls
echo ============================================================
echo   Push to Docker Hub
echo ============================================================
echo.

if not defined DOCKERHUB_USERNAME (
    set /p DOCKERHUB_USERNAME="Docker Hub username: "
)
if not defined DOCKERHUB_TOKEN (
    set /p DOCKERHUB_TOKEN="Docker Hub access token: "
)

echo.
echo Logging into Docker Hub...
echo %DOCKERHUB_TOKEN% | docker login -u %DOCKERHUB_USERNAME% --password-stdin

if %errorLevel% neq 0 (
    echo [ERROR] Login failed. Check credentials.
    pause
    goto menu
)

echo.
set /p LOCAL_IMAGE="Enter local image tag (e.g., gcc-webapp:prod-latest): "
set /p REMOTE_TAG="Enter Docker Hub repository (e.g., myuser/gcc-webapp:latest): "

echo.
echo Tagging image...
docker tag %LOCAL_IMAGE% %REMOTE_TAG%

echo Pushing to Docker Hub...
docker push %REMOTE_TAG% 2>&1 | tee -a "%LOG_FILE%"

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Image pushed to Docker Hub: %REMOTE_TAG%
    echo URL: https://hub.docker.com/r/%DOCKERHUB_USERNAME%/
) else (
    echo [ERROR] Push failed.
)

docker logout
pause
goto menu

REM ============= Azure ACR =============
:azure_acr
cls
echo ============================================================
echo   Push to Azure Container Registry
echo ============================================================
echo.

if not defined AZURE_REGISTRY (
    set /p AZURE_REGISTRY="ACR name (e.g., myregistry.azurecr.io): "
)
if not defined AZURE_USERNAME (
    set /p AZURE_USERNAME="ACR username: "
)
if not defined AZURE_PASSWORD (
    set /p AZURE_PASSWORD="ACR password: "
)

echo.
echo Logging into Azure Container Registry...
echo %AZURE_PASSWORD% | docker login %AZURE_REGISTRY% -u %AZURE_USERNAME% --password-stdin

if %errorLevel% neq 0 (
    echo [ERROR] ACR login failed.
    echo Try: az acr login --name [registry-name]
    pause
    goto menu
)

echo.
set /p LOCAL_IMAGE="Enter local image tag: "
set REMOTE_TAG=%AZURE_REGISTRY%/%LOCAL_IMAGE%

echo.
echo Tagging for ACR...
docker tag %LOCAL_IMAGE% %REMOTE_TAG%

echo Pushing to ACR...
docker push %REMOTE_TAG%

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Image pushed to ACR: %REMOTE_TAG%
    
    REM Import to Azure App Service (optional)
    set /p IMPORT_APP="Import to App Service? (y/n): "
    if /i "!IMPORT_APP!"=="y" (
        set /p RESOURCE_GROUP="Resource group: "
        set /p APP_NAME="App Service name: "
        
        az webapp config container set ^
            --resource-group !RESOURCE_GROUP! ^
            --name !APP_NAME! ^
            --docker-custom-image-name %REMOTE_TAG% ^
            --docker-registry-server-url https://%AZURE_REGISTRY%
    )
) else (
    echo [ERROR] Push failed.
)

docker logout %AZURE_REGISTRY%
pause
goto menu

REM ============= AWS ECR =============
:aws_ecr
cls
echo ============================================================
echo   Push to AWS ECR
echo ============================================================
echo.

if not defined AWS_REGION (
    set /p AWS_REGION="AWS Region (e.g., us-east-1): "
)
if not defined AWS_ACCOUNT_ID (
    set /p AWS_ACCOUNT_ID="AWS Account ID: "
)

set ECR_URI=%AWS_ACCOUNT_ID%.dkr.ecr.%AWS_REGION%.amazonaws.com

echo.
echo Getting ECR login token...
aws ecr get-login-password --region %AWS_REGION% | docker login --username AWS --password-stdin %ECR_URI%

if %errorLevel% neq 0 (
    echo [ERROR] ECR login failed. Check AWS CLI configuration.
    pause
    goto menu
)

echo.
set /p REPOSITORY_NAME="ECR repository name: "
set /p LOCAL_IMAGE="Enter local image tag: "

REM Create repository if it doesn't exist
aws ecr describe-repositories --repository-names %REPOSITORY_NAME% --region %AWS_REGION% >nul 2>&1
if %errorLevel% neq 0 (
    echo Creating ECR repository...
    aws ecr create-repository --repository-name %REPOSITORY_NAME% --region %AWS_REGION%
)

set REMOTE_TAG=%ECR_URI%/%REPOSITORY_NAME%:latest

echo.
echo Tagging for ECR...
docker tag %LOCAL_IMAGE% %REMOTE_TAG%

echo Pushing to ECR...
docker push %REMOTE_TAG%

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Image pushed to ECR: %REMOTE_TAG%
    
    REM Set lifecycle policy
    echo Setting lifecycle policy...
    echo {
    echo   "rules": [{
    echo     "rulePriority": 1,
    echo     "description": "Keep last 10 images",
    echo     "selection": {
    echo       "tagStatus": "any",
    echo       "countType": "imageCountMoreThan",
    echo       "countNumber": 10
    echo     },
    echo     "action": { "type": "expire" }
    echo   }]
    echo } > ecr-lifecycle.json
    
    aws ecr put-lifecycle-policy ^
        --repository-name %REPOSITORY_NAME% ^
        --lifecycle-policy-text file://ecr-lifecycle.json ^
        --region %AWS_REGION% >nul 2>&1
    
    del ecr-lifecycle.json
) else (
    echo [ERROR] Push failed.
)

docker logout %ECR_URI%
pause
goto menu

REM ============= Google GCR =============
:google_gcr
cls
echo ============================================================
echo   Push to Google Container Registry
echo ============================================================
echo.

if not defined GCP_PROJECT_ID (
    set /p GCP_PROJECT_ID="GCP Project ID: "
)

echo.
echo Configuring Docker for GCR...
gcloud auth configure-docker

if %errorLevel% neq 0 (
    echo [ERROR] GCloud configuration failed.
    echo Install: https://cloud.google.com/sdk/docs/install
    pause
    goto menu
)

echo.
set /p LOCAL_IMAGE="Enter local image tag: "
set /p IMAGE_NAME="Image name for GCR: "
set REMOTE_TAG=gcr.io/%GCP_PROJECT_ID%/%IMAGE_NAME%:latest

echo.
echo Tagging for GCR...
docker tag %LOCAL_IMAGE% %REMOTE_TAG%

echo Pushing to GCR...
docker push %REMOTE_TAG%

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Image pushed to GCR: %REMOTE_TAG%
    
    set /p DEPLOY_CLOUD_RUN="Deploy to Cloud Run? (y/n): "
    if /i "!DEPLOY_CLOUD_RUN!"=="y" (
        set /p SERVICE_NAME="Cloud Run service name: "
        
        gcloud run deploy !SERVICE_NAME! ^
            --image %REMOTE_TAG% ^
            --platform managed ^
            --region us-central1 ^
            --allow-unauthenticated
    )
) else (
    echo [ERROR] Push failed.
)

pause
goto menu

REM ============= Private Registry =============
:private_registry
cls
echo ============================================================
echo   Push to Private Registry
echo ============================================================
echo.

set /p REGISTRY_URL="Registry URL (e.g., registry.company.com:5000): "
set /p REGISTRY_USER="Username (leave empty for anonymous): "

if not "%REGISTRY_USER%"=="" (
    set /p REGISTRY_PASS="Password: "
    echo !REGISTRY_PASS! | docker login %REGISTRY_URL% -u %REGISTRY_USER% --password-stdin
) else (
    echo Using anonymous access...
)

echo.
set /p LOCAL_IMAGE="Enter local image tag: "
set /p REMOTE_PATH="Remote path (e.g., gcc-webapp:latest): "
set REMOTE_TAG=%REGISTRY_URL%/%REMOTE_PATH%

echo.
echo Tagging image...
docker tag %LOCAL_IMAGE% %REMOTE_TAG%

echo Pushing to private registry...
docker push %REMOTE_TAG%

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] Image pushed: %REMOTE_TAG%
) else (
    echo [ERROR] Push failed. Check registry configuration.
)

if not "%REGISTRY_USER%"=="" docker logout %REGISTRY_URL%
pause
goto menu

REM ============= Configure Settings =============
:configure
cls
echo ============================================================
echo   Configure Registry Settings
echo ============================================================
echo.

echo Current settings:
if exist ".docker-registry.conf" type .docker-registry.conf

echo.
echo Configure:
echo 1. Docker Hub credentials
echo 2. Azure ACR settings
echo 3. AWS ECR settings
echo 4. Private registry
echo 5. Clear all settings
echo 0. Back
echo.
set /p CONFIG_CHOICE="Select: "

if "%CONFIG_CHOICE%"=="1" (
    set /p DOCKERHUB_USERNAME="Docker Hub username: "
    set /p DOCKERHUB_TOKEN="Docker Hub token: "
    echo DOCKERHUB_USERNAME=%DOCKERHUB_USERNAME% >> .docker-registry.conf
    echo DOCKERHUB_TOKEN=%DOCKERHUB_TOKEN% >> .docker-registry.conf
    echo [SUCCESS] Docker Hub configured
) else if "%CONFIG_CHOICE%"=="2" (
    set /p AZURE_REGISTRY="ACR URL: "
    set /p AZURE_USERNAME="ACR username: "
    echo AZURE_REGISTRY=%AZURE_REGISTRY% >> .docker-registry.conf
    echo AZURE_USERNAME=%AZURE_USERNAME% >> .docker-registry.conf
    echo [SUCCESS] Azure ACR configured
) else if "%CONFIG_CHOICE%"=="5" (
    del .docker-registry.conf 2>nul
    echo [SUCCESS] Settings cleared
)

pause
goto menu

REM ============= Multi Push =============
:multi_push
cls
echo ============================================================
echo   Push Multiple Tags
echo ============================================================
echo.

set /p SOURCE_IMAGE="Source image tag: "

echo.
echo Enter target registries (empty to finish):
set COUNT=0

:multi_input
set /a COUNT+=1
set /p "TARGET_%COUNT%=Target %COUNT%: "
if not "!TARGET_%COUNT%!"=="" goto multi_input
set /a COUNT-=1

echo.
echo Will push to %COUNT% registries:
for /l %%i in (1,1,%COUNT%) do echo   %%i. !TARGET_%%i!

echo.
set /p CONFIRM="Continue? (y/n): "
if /i not "%CONFIRM%"=="y" goto menu

echo.
for /l %%i in (1,1,%COUNT%) do (
    echo Pushing to !TARGET_%%i!...
    docker tag %SOURCE_IMAGE% !TARGET_%%i!
    docker push !TARGET_%%i!
    if !errorLevel! equ 0 (
        echo   [SUCCESS] Pushed to !TARGET_%%i!
    ) else (
        echo   [ERROR] Failed to push to !TARGET_%%i!
    )
)

pause
goto menu

REM ============= CI/CD Push =============
:cicd_push
cls
echo ============================================================
echo   Automated CI/CD Push
echo ============================================================
echo.

echo This will push images based on branch/tag:
echo   - main/master -> production
echo   - develop -> test
echo   - feature/* -> dev
echo.

REM Get current git branch
for /f "tokens=*" %%g in ('git rev-parse --abbrev-ref HEAD 2^>nul') do set GIT_BRANCH=%%g

if "%GIT_BRANCH%"=="" (
    echo [ERROR] Not in a git repository
    pause
    goto menu
)

echo Current branch: %GIT_BRANCH%

REM Determine environment based on branch
if "%GIT_BRANCH%"=="main" set ENV=prod
if "%GIT_BRANCH%"=="master" set ENV=prod
if "%GIT_BRANCH%"=="develop" set ENV=test
if "%GIT_BRANCH:~0,7%"=="feature" set ENV=dev
if "%GIT_BRANCH:~0,7%"=="hotfix" set ENV=test

if not defined ENV (
    echo [WARNING] Unknown branch pattern, defaulting to dev
    set ENV=dev
)

echo Target environment: %ENV%
echo.

set /p REGISTRY="Registry (dockerhub/acr/ecr/gcr): "
set /p REGISTRY_PATH="Registry path: "

REM Build image
echo Building %ENV% image...
docker build -t gcc-webapp:%ENV%-ci-%BUILD_NUMBER% .

REM Push to registry
echo Pushing to %REGISTRY%...
set REMOTE_TAG=%REGISTRY_PATH%/gcc-webapp:%ENV%-ci-%BUILD_NUMBER%

docker tag gcc-webapp:%ENV%-ci-%BUILD_NUMBER% %REMOTE_TAG%
docker push %REMOTE_TAG%

if %errorLevel% equ 0 (
    echo.
    echo [SUCCESS] CI/CD push completed
    echo Image: %REMOTE_TAG%
    
    REM Create deployment manifest
    echo Creating deployment manifest...
    (
        echo {
        echo   "image": "%REMOTE_TAG%",
        echo   "branch": "%GIT_BRANCH%",
        echo   "environment": "%ENV%",
        echo   "build_number": "%BUILD_NUMBER%",
        echo   "timestamp": "%date% %time%"
        echo }
    ) > deployment-manifest.json
    
    echo Manifest saved: deployment-manifest.json
) else (
    echo [ERROR] CI/CD push failed
)

pause
goto menu
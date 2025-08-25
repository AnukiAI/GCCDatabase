@echo off
REM ============================================
REM GCC WEB APPLICATION - DOCKER DEPLOYMENT SCRIPT (Windows)
REM ============================================

setlocal enabledelayedexpansion

echo 🚀 Starting GCC Web Application Deployment...

REM Check if Docker is installed
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not installed. Please install Docker Desktop first.
    pause
    exit /b 1
)

REM Check if Docker Compose is installed
docker-compose --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker Compose is not installed. Please install Docker Compose first.
    pause
    exit /b 1
)

REM Default values
set DATABASE_TYPE=postgresql
set ENV_FILE=.env
set REBUILD=false
set ACTION=up

REM Parse command line arguments
:parse_args
if "%1"=="" goto :end_parse
if "%1"=="-d" (
    set DATABASE_TYPE=%2
    shift
    shift
    goto :parse_args
)
if "%1"=="--database" (
    set DATABASE_TYPE=%2
    shift
    shift
    goto :parse_args
)
if "%1"=="-e" (
    set ENV_FILE=%2
    shift
    shift
    goto :parse_args
)
if "%1"=="--env-file" (
    set ENV_FILE=%2
    shift
    shift
    goto :parse_args
)
if "%1"=="--rebuild" (
    set REBUILD=true
    shift
    goto :parse_args
)
if "%1"=="--down" (
    set ACTION=down
    shift
    goto :parse_args
)
if "%1"=="--logs" (
    set ACTION=logs
    shift
    goto :parse_args
)
if "%1"=="--status" (
    set ACTION=status
    shift
    goto :parse_args
)
if "%1"=="-h" goto :usage
if "%1"=="--help" goto :usage
echo ❌ Unknown option: %1
goto :usage

:end_parse

REM Validate database type
if not "%DATABASE_TYPE%"=="postgresql" if not "%DATABASE_TYPE%"=="mariadb" (
    echo ❌ Invalid database type: %DATABASE_TYPE%
    echo Valid options: postgresql, mariadb
    pause
    exit /b 1
)

REM Set compose file based on database type
if "%DATABASE_TYPE%"=="mariadb" (
    set COMPOSE_FILE=docker-compose.mariadb.yml
) else (
    set COMPOSE_FILE=docker-compose.yml
)

echo 📋 Configuration:
echo    Database: %DATABASE_TYPE%
echo    Compose file: %COMPOSE_FILE%
echo    Environment file: %ENV_FILE%

REM Check if environment file exists
if not exist "%ENV_FILE%" (
    echo ⚠️  Environment file %ENV_FILE% not found.
    if exist ".env.example" (
        echo 📝 Creating %ENV_FILE% from .env.example...
        copy .env.example "%ENV_FILE%" >nul
        echo ⚠️  Please edit %ENV_FILE% with your actual configuration values!
        echo    Required changes:
        echo    - Update database passwords
        echo    - Set JWT secret key (64+ characters^)
        echo    - Configure SMTP settings for 2FA
        echo.
        echo Press Enter after updating %ENV_FILE%...
        pause >nul
    ) else (
        echo ❌ No .env.example file found. Please create %ENV_FILE% manually.
        pause
        exit /b 1
    )
)

REM Execute the requested action
if "%ACTION%"=="up" goto :action_up
if "%ACTION%"=="down" goto :action_down
if "%ACTION%"=="logs" goto :action_logs
if "%ACTION%"=="status" goto :action_status

:action_up
echo 🔨 Building and starting containers...

if "%REBUILD%"=="true" (
    echo 🔄 Rebuilding images from scratch...
    docker-compose -f "%COMPOSE_FILE%" --env-file "%ENV_FILE%" build --no-cache
)

docker-compose -f "%COMPOSE_FILE%" --env-file "%ENV_FILE%" up -d

echo ⏳ Waiting for services to be ready...
timeout /t 10 /nobreak >nul

REM Check if services are running
echo 📊 Container status:
docker-compose -f "%COMPOSE_FILE%" --env-file "%ENV_FILE%" ps

echo.
echo ✅ Deployment complete!
echo 🌐 Application URL: http://localhost:8080
echo 📊 Database: %DATABASE_TYPE% running on default port
echo.
echo 🔑 Default login credentials:
echo    Email: admin@greencirclecertified.com
echo    Password: admin123
echo.
echo 📝 To view logs: deploy.cmd --logs
echo 🛑 To stop: deploy.cmd --down
goto :end

:action_down
echo 🛑 Stopping and removing containers...
docker-compose -f "%COMPOSE_FILE%" --env-file "%ENV_FILE%" down
echo ✅ Containers stopped.
goto :end

:action_logs
echo 📋 Showing container logs...
docker-compose -f "%COMPOSE_FILE%" --env-file "%ENV_FILE%" logs -f
goto :end

:action_status
echo 📊 Container status:
docker-compose -f "%COMPOSE_FILE%" --env-file "%ENV_FILE%" ps
goto :end

:usage
echo Usage: %0 [OPTIONS]
echo Options:
echo   -d, --database TYPE    Database type (postgresql^|mariadb^) [default: postgresql]
echo   -e, --env-file FILE    Environment file [default: .env]
echo   -h, --help            Show this help message
echo   --rebuild             Rebuild images from scratch
echo   --down               Stop and remove containers
echo   --logs               Show container logs
echo   --status             Show container status
pause
exit /b 1

:end
pause
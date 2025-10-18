#!/bin/bash

# ============================================
# GCC WEB APPLICATION - DOCKER DEPLOYMENT SCRIPT
# ============================================

set -e  # Exit on any error

echo "🚀 Starting GCC Web Application Deployment..."

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

# Function to display usage
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo "Options:"
    echo "  -d, --database TYPE    Database type (postgresql|mariadb) [default: postgresql]"
    echo "  -e, --env-file FILE    Environment file [default: .env]"
    echo "  -h, --help            Show this help message"
    echo "  --rebuild             Rebuild images from scratch"
    echo "  --down               Stop and remove containers"
    echo "  --logs               Show container logs"
    echo "  --status             Show container status"
    exit 1
}

# Default values
DATABASE_TYPE="postgresql"
ENV_FILE=".env"
REBUILD=false
ACTION="up"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--database)
            DATABASE_TYPE="$2"
            shift 2
            ;;
        -e|--env-file)
            ENV_FILE="$2"
            shift 2
            ;;
        --rebuild)
            REBUILD=true
            shift
            ;;
        --down)
            ACTION="down"
            shift
            ;;
        --logs)
            ACTION="logs"
            shift
            ;;
        --status)
            ACTION="status"
            shift
            ;;
        -h|--help)
            usage
            ;;
        *)
            echo "❌ Unknown option: $1"
            usage
            ;;
    esac
done

# Validate database type
if [[ "$DATABASE_TYPE" != "postgresql" && "$DATABASE_TYPE" != "mariadb" ]]; then
    echo "❌ Invalid database type: $DATABASE_TYPE"
    echo "Valid options: postgresql, mariadb"
    exit 1
fi

# Set compose file based on database type
if [[ "$DATABASE_TYPE" == "mariadb" ]]; then
    COMPOSE_FILE="docker-compose.mariadb.yml"
else
    COMPOSE_FILE="docker-compose.yml"
fi

echo "📋 Configuration:"
echo "   Database: $DATABASE_TYPE"
echo "   Compose file: $COMPOSE_FILE"
echo "   Environment file: $ENV_FILE"

# Check if environment file exists
if [[ ! -f "$ENV_FILE" ]]; then
    echo "⚠️  Environment file $ENV_FILE not found."
    if [[ -f ".env.example" ]]; then
        echo "📝 Creating $ENV_FILE from .env.example..."
        cp .env.example "$ENV_FILE"
        echo "⚠️  Please edit $ENV_FILE with your actual configuration values!"
        echo "   Required changes:"
        echo "   - Update database passwords"
        echo "   - Set JWT secret key (64+ characters)"
        echo "   - Configure SMTP settings for 2FA"
        echo ""
        read -p "Press Enter after updating $ENV_FILE or Ctrl+C to cancel..."
    else
        echo "❌ No .env.example file found. Please create $ENV_FILE manually."
        exit 1
    fi
fi

# Execute the requested action
case $ACTION in
    "up")
        echo "🔨 Building and starting containers..."
        
        if [[ "$REBUILD" == true ]]; then
            echo "🔄 Rebuilding images from scratch..."
            docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" build --no-cache
        fi
        
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d
        
        echo "⏳ Waiting for services to be ready..."
        sleep 10
        
        # Check if services are running
        echo "📊 Container status:"
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps
        
        echo ""
        echo "✅ Deployment complete!"
        echo "🌐 Application URL: http://localhost:8080"
        echo "📊 Database: $DATABASE_TYPE running on default port"
        echo ""
        echo "🔑 Default login credentials:"
        echo "   Email: admin@greencirclecertified.com"
        echo "   Password: admin123"
        echo ""
        echo "📝 To view logs: ./deploy.sh --logs"
        echo "🛑 To stop: ./deploy.sh --down"
        ;;
        
    "down")
        echo "🛑 Stopping and removing containers..."
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down
        echo "✅ Containers stopped."
        ;;
        
    "logs")
        echo "📋 Showing container logs..."
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" logs -f
        ;;
        
    "status")
        echo "📊 Container status:"
        docker-compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps
        ;;
esac
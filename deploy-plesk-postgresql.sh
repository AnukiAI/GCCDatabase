#!/bin/bash
# Plesk deployment with PostgreSQL (for enterprise setups)

echo "🚀 Deploying GCC WebApp to Plesk (PostgreSQL Setup)"
echo "====================================================="

# Configuration - EDIT THESE VALUES
DB_HOST="localhost"  # Change to your PostgreSQL host
DB_NAME="gcc_database"
DB_USER="gcc_user"
DB_PASSWORD="${DB_PASSWORD:-your_secure_db_password}"  # Set via environment variable
JWT_SECRET="${JWT_SECRET:-}"  # Set via environment variable

CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:production-ready"
PORT="5101"

# Validate required variables
if [ -z "$DB_PASSWORD" ]; then
    echo "❌ ERROR: DB_PASSWORD environment variable is required"
    echo "Usage: DB_PASSWORD='your_password' JWT_SECRET='your_jwt_secret' ./deploy-plesk-postgresql.sh"
    exit 1
fi

# Generate JWT secret if not provided
if [ -z "$JWT_SECRET" ]; then
    echo "🔑 Generating secure JWT secret..."
    JWT_SECRET=$(openssl rand -base64 48 2>/dev/null || head -c 48 /dev/urandom | base64)
    echo "Generated JWT secret: ${JWT_SECRET:0:20}... (truncated for security)"
fi

# Stop existing container
echo "🛑 Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true

# Test database connection
echo "🔍 Testing database connection..."
if command -v psql >/dev/null 2>&1; then
    PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c "SELECT 1;" >/dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "✅ Database connection successful"
    else
        echo "⚠️  Warning: Could not connect to database. Proceeding anyway..."
    fi
else
    echo "ℹ️  PostgreSQL client not available, skipping connection test"
fi

# Pull latest image
echo "📥 Pulling latest image..."
docker pull $IMAGE_NAME

# Deploy container
echo "🚀 Starting container..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p $PORT:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=$DB_HOST;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD" \
  -e DatabaseProvider=postgresql \
  -e Jwt__Key="$JWT_SECRET" \
  -e DataProtection__PersistKeysToAzureBlobStorage=false \
  -e AZURE_AD_B2C_CLIENT_SECRET="not-using-azure-b2c" \
  -e AZURE_AD_CLIENT_SECRET="not-using-azure-ad" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e SMTP_PASSWORD="not-using-email" \
  $IMAGE_NAME

# Wait for startup
echo "⏳ Waiting for application startup..."
sleep 20

# Test deployment
echo "🔍 Testing deployment..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:$PORT || echo "000")

if [ "$HTTP_STATUS" = "302" ] || [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ SUCCESS! Application is running"
    echo ""
    echo "🌐 Application URL: http://angry-pascal.70-32-82-212.plesk.page:$PORT"
    echo "📊 Container status: docker ps | grep $CONTAINER_NAME"
    echo "📝 View logs: docker logs $CONTAINER_NAME"
    echo ""
    echo "🔑 IMPORTANT: Save these secrets securely:"
    echo "DB_PASSWORD=$DB_PASSWORD"
    echo "JWT_SECRET=$JWT_SECRET"
else
    echo "❌ FAILED! HTTP Status: $HTTP_STATUS"
    echo "📝 Check logs: docker logs $CONTAINER_NAME"
    exit 1
fi
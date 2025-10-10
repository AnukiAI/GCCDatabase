#!/bin/bash
# Simple Plesk deployment with SQLite (recommended for most users)

echo "🚀 Deploying GCC WebApp to Plesk (Simple SQLite Setup)"
echo "=========================================================="

# Configuration
CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:production-ready"
PORT="5101"
DATA_DIR="/var/www/vhosts/angry-pascal.70-32-82-212.plesk.page/httpdocs/data"

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

# Create data directory
echo "📁 Creating data directory..."
mkdir -p $DATA_DIR
chmod 755 $DATA_DIR

# Pull latest image
echo "📥 Pulling latest image..."
docker pull $IMAGE_NAME

# Deploy container
echo "🚀 Starting container..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p $PORT:8080 \
  -v $DATA_DIR:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Data Source=/app/data/gcc_database.db" \
  -e Jwt__Key="$JWT_SECRET" \
  -e DataProtection__PersistKeysToAzureBlobStorage=false \
  -e AZURE_AD_B2C_CLIENT_SECRET="not-using-azure-b2c" \
  -e AZURE_AD_CLIENT_SECRET="not-using-azure-ad" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e SMTP_PASSWORD="not-using-email" \
  $IMAGE_NAME

# Wait for startup
echo "⏳ Waiting for application startup..."
sleep 15

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
    echo "🔑 IMPORTANT: Save your JWT secret securely:"
    echo "JWT_SECRET=$JWT_SECRET"
else
    echo "❌ FAILED! HTTP Status: $HTTP_STATUS"
    echo "📝 Check logs: docker logs $CONTAINER_NAME"
    exit 1
fi
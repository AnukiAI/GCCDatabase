#!/bin/bash

# ============================================================
# GCC Web App - Docker Deployment Script for Plesk Server
# With PostgreSQL Database Support
# ============================================================

# Configuration - UPDATE THESE VALUES
POSTGRES_HOST="localhost"           # PostgreSQL host (localhost if on same server)
POSTGRES_DB="gccdb"                 # Database name
POSTGRES_USER="gccuser"             # Database user
POSTGRES_PASSWORD="YourPassword"    # Database password
POSTGRES_PORT="5432"                # PostgreSQL port

DOMAIN="your-domain.com"            # Your domain
JWT_SECRET="$(openssl rand -base64 64 | tr -d '\n')"  # Generate random JWT secret

# Docker configuration
CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="gcc-webapp:production"
PORT="5102"

echo "======================================"
echo "GCC Web App Docker Deployment"
echo "======================================"

# Stop and remove existing container
echo "Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

# Run container with PostgreSQL configuration
echo "Starting container with PostgreSQL configuration..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p ${PORT}:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e ASPNETCORE_HTTP_PORTS=8080 \
  -e ConnectionStrings__DefaultConnection="Host=${POSTGRES_HOST};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Port=${POSTGRES_PORT}" \
  -e Jwt__Key="${JWT_SECRET}" \
  -e Jwt__Issuer="https://${DOMAIN}" \
  -e Jwt__Audience="https://${DOMAIN}" \
  -e GCC_JWT_SECRET="${JWT_SECRET}" \
  -e DataProtection__AzureBlobStorageConnectionString="DefaultEndpointsProtocol=https;AccountName=dummy;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net" \
  -e DataProtection__UseAzure=false \
  -e GCC_AZURE_AD_B2C_CLIENT_SECRET="not-configured" \
  -e GCC_AZURE_AD_CLIENT_SECRET="not-configured" \
  -e ApplicationInsights__ConnectionString="InstrumentationKey=00000000-0000-0000-0000-000000000000" \
  -e EmailConfiguration__Password="smtp-password" \
  -e FeatureFlags__EnableAzureB2C=false \
  -e FeatureFlags__EnableApplicationInsights=false \
  -e FeatureFlags__EnableAzureBlobStorage=false \
  $IMAGE_NAME

# Check if container started
sleep 5
if docker ps | grep -q $CONTAINER_NAME; then
    echo "✅ Container started successfully!"
    echo ""
    echo "Application is running on port $PORT"
    echo "Configure your Plesk nginx proxy to forward to http://localhost:$PORT"
    echo ""
    echo "View logs: docker logs -f $CONTAINER_NAME"
else
    echo "❌ Container failed to start. Check logs:"
    docker logs $CONTAINER_NAME
    exit 1
fi
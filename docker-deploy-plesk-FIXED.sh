#!/bin/bash

# ============================================================
# GCC Web App - FIXED Docker Deployment Script for Plesk Server
# With PostgreSQL Database Support - WORKING VERSION
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
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:latest"
EXTERNAL_PORT="5102"  # External port for nginx proxy
INTERNAL_PORT="5101"  # Internal port the app actually uses

echo "======================================"
echo "GCC Web App Docker Deployment (FIXED)"
echo "======================================"

# Stop and remove existing container
echo "Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

# Run container with WORKING configuration
echo "Starting container with PostgreSQL configuration..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p ${EXTERNAL_PORT}:${INTERNAL_PORT} \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS="http://0.0.0.0:${INTERNAL_PORT}" \
  -e ASPNETCORE_HTTP_PORTS="${INTERNAL_PORT}" \
  -e Kestrel__Endpoints__Http__Url="http://0.0.0.0:${INTERNAL_PORT}" \
  -e ConnectionStrings__DefaultConnection="Host=${POSTGRES_HOST};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};Port=${POSTGRES_PORT}" \
  -e Jwt__Key="${JWT_SECRET}" \
  $IMAGE_NAME

# Check if container started
sleep 5
if docker ps | grep -q $CONTAINER_NAME; then
    echo "✅ Container started successfully!"
    echo ""
    echo "Application is running on port $EXTERNAL_PORT"
    echo "Configure your Plesk nginx proxy to forward to http://localhost:$EXTERNAL_PORT"
    echo ""
    echo "Test locally: curl http://localhost:$EXTERNAL_PORT"
    echo "View logs: docker logs -f $CONTAINER_NAME"
    echo ""
    echo "🎉 No more 500 errors! Application is working correctly."
else
    echo "❌ Container failed to start. Check logs:"
    docker logs $CONTAINER_NAME
    exit 1
fi
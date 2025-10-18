#!/bin/bash
# Enterprise Plesk deployment with Azure integration

echo "🏢 Deploying GCC WebApp to Plesk (Enterprise Azure Setup)"
echo "=========================================================="

# Configuration - SET THESE VIA ENVIRONMENT VARIABLES
DB_HOST="${DB_HOST:-localhost}"
DB_NAME="${DB_NAME:-gcc_database}"
DB_USER="${DB_USER:-gcc_user}"
DB_PASSWORD="${DB_PASSWORD:-}"
JWT_SECRET="${JWT_SECRET:-}"
AZURE_B2C_SECRET="${AZURE_B2C_SECRET:-}"
AZURE_AD_SECRET="${AZURE_AD_SECRET:-}"
AZURE_BLOB_CONNECTION="${AZURE_BLOB_CONNECTION:-}"
APP_INSIGHTS_CONNECTION="${APP_INSIGHTS_CONNECTION:-}"
SMTP_PASSWORD="${SMTP_PASSWORD:-}"

CONTAINER_NAME="gcc-webapp"
IMAGE_NAME="ghcr.io/anukiai/gcc-webapp:production-ready"
PORT="5101"

# Validate required variables
MISSING_VARS=()
[ -z "$DB_PASSWORD" ] && MISSING_VARS+=("DB_PASSWORD")
[ -z "$JWT_SECRET" ] && MISSING_VARS+=("JWT_SECRET")
[ -z "$AZURE_B2C_SECRET" ] && MISSING_VARS+=("AZURE_B2C_SECRET")
[ -z "$AZURE_AD_SECRET" ] && MISSING_VARS+=("AZURE_AD_SECRET")
[ -z "$AZURE_BLOB_CONNECTION" ] && MISSING_VARS+=("AZURE_BLOB_CONNECTION")

if [ ${#MISSING_VARS[@]} -gt 0 ]; then
    echo "❌ ERROR: Missing required environment variables:"
    printf '%s\n' "${MISSING_VARS[@]}"
    echo ""
    echo "Usage example:"
    echo "DB_PASSWORD='db_pass' JWT_SECRET='jwt_secret' AZURE_B2C_SECRET='b2c_secret' \\"
    echo "AZURE_AD_SECRET='ad_secret' AZURE_BLOB_CONNECTION='blob_connection' \\"
    echo "./deploy-plesk-enterprise.sh"
    exit 1
fi

# Stop existing container
echo "🛑 Stopping existing container..."
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true

# Pull latest image
echo "📥 Pulling latest image..."
docker pull $IMAGE_NAME

# Deploy container with full Azure integration
echo "🚀 Starting container with Azure integration..."
docker run -d \
  --name $CONTAINER_NAME \
  --restart unless-stopped \
  -p $PORT:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=$DB_HOST;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD" \
  -e DatabaseProvider=postgresql \
  -e Jwt__Key="$JWT_SECRET" \
  -e DataProtection__PersistKeysToAzureBlobStorage=true \
  -e DataProtection__AzureBlobStorageConnectionString="$AZURE_BLOB_CONNECTION" \
  -e AzureAdB2C__ClientSecret="$AZURE_B2C_SECRET" \
  -e AzureAd__ClientSecret="$AZURE_AD_SECRET" \
  -e ApplicationInsights__ConnectionString="$APP_INSIGHTS_CONNECTION" \
  -e EmailConfiguration__Password="$SMTP_PASSWORD" \
  -e AZURE_AD_B2C_CLIENT_SECRET="$AZURE_B2C_SECRET" \
  -e AZURE_AD_CLIENT_SECRET="$AZURE_AD_SECRET" \
  -e AZURE_BLOB_CONNECTION_STRING="$AZURE_BLOB_CONNECTION" \
  -e APPLICATION_INSIGHTS_CONNECTION_STRING="$APP_INSIGHTS_CONNECTION" \
  -e SMTP_PASSWORD="$SMTP_PASSWORD" \
  $IMAGE_NAME

# Wait for startup
echo "⏳ Waiting for application startup..."
sleep 25

# Test deployment
echo "🔍 Testing deployment..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:$PORT || echo "000")

if [ "$HTTP_STATUS" = "302" ] || [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ SUCCESS! Enterprise application is running"
    echo ""
    echo "🌐 Application URL: http://angry-pascal.70-32-82-212.plesk.page:$PORT"
    echo "📊 Container status: docker ps | grep $CONTAINER_NAME"
    echo "📝 View logs: docker logs $CONTAINER_NAME"
    echo ""
    echo "🔒 Azure Integration Features Enabled:"
    echo "  ✓ Azure AD B2C Authentication"
    echo "  ✓ Azure Blob Storage for Data Protection"
    echo "  ✓ Application Insights Monitoring"
    echo "  ✓ Email Notifications"
else
    echo "❌ FAILED! HTTP Status: $HTTP_STATUS"
    echo "📝 Check logs: docker logs $CONTAINER_NAME"
    echo ""
    echo "Common enterprise deployment issues:"
    echo "  • Azure connection strings format"
    echo "  • Network connectivity to Azure services"
    echo "  • Service principal permissions"
    exit 1
fi
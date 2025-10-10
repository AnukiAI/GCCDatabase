#!/bin/bash

# GCC Web App - Azure App Service Quick Deployment Script
# Run this script to deploy your app to Azure

set -e  # Exit on any error

echo "╔════════════════════════════════════════════╗"
echo "║   GCC Web App - Azure Deployment Script   ║"
echo "║         Quick & Easy Deployment            ║"
echo "╚════════════════════════════════════════════╝"

# Configuration - Modify these if needed
RESOURCE_GROUP="GCC-Resources"
LOCATION="eastus"  # Change to your preferred region
APP_NAME="gccwebapp-$RANDOM"  # Ensures unique name
PLAN_NAME="GCC-AppPlan"
SKU="B1"  # B1 = Basic ($13/month), F1 = Free (limited), S1 = Standard ($70/month)

echo ""
echo "📋 Deployment Configuration:"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   Location: $LOCATION"
echo "   App Name: $APP_NAME"
echo "   Pricing Tier: $SKU"
echo ""

# Check if Azure CLI is installed
echo "🔍 Checking Azure CLI installation..."
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed!"
    echo "Please install from: https://aka.ms/installazurecli"
    echo "Or run: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash"
    exit 1
fi
echo "✅ Azure CLI is installed"

# Login to Azure
echo ""
echo "🔐 Logging into Azure..."
echo "   A browser window will open for authentication"
az login --only-show-errors > /dev/null
echo "✅ Logged in successfully"

# Check if deployment package exists
PACKAGE="gccwebapp-dotnet8.tar.gz"
if [ ! -f "$PACKAGE" ]; then
    echo "❌ Deployment package not found: $PACKAGE"
    echo "Building deployment package..."

    # Try to build the package
    dotnet publish GCCWebApp.csproj -c Release -r linux-x64 --self-contained false -o ./publish

    if [ -d "./publish" ]; then
        echo "Creating deployment archive..."
        tar -czf $PACKAGE -C publish .
        echo "✅ Package created successfully"
    else
        echo "❌ Failed to create deployment package"
        exit 1
    fi
else
    echo "✅ Deployment package found: $PACKAGE"
fi

# Create Resource Group
echo ""
echo "📁 Creating Resource Group..."
az group create \
    --name $RESOURCE_GROUP \
    --location "$LOCATION" \
    --only-show-errors > /dev/null
echo "✅ Resource group created/verified"

# Create App Service Plan
echo ""
echo "📋 Creating App Service Plan..."
az appservice plan create \
    --name $PLAN_NAME \
    --resource-group $RESOURCE_GROUP \
    --location "$LOCATION" \
    --sku $SKU \
    --is-linux \
    --only-show-errors > /dev/null
echo "✅ App Service Plan created"

# Create Web App
echo ""
echo "🌐 Creating Web App..."
az webapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --plan $PLAN_NAME \
    --runtime "DOTNET:8.0" \
    --only-show-errors > /dev/null
echo "✅ Web App created"

# Deploy the application
echo ""
echo "🚀 Deploying application (this may take 2-3 minutes)..."
az webapp deployment source config-zip \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --src $PACKAGE \
    --only-show-errors 2>&1 > /dev/null || true
echo "✅ Application deployed!"

# Configure Application Settings
echo ""
echo "⚙️  Configuring application settings..."

# Generate a secure JWT secret
JWT_SECRET=$(openssl rand -base64 48 | tr -d '\n')

az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    GCC_JWT_SECRET="$JWT_SECRET" \
    DatabaseProvider="Sqlite" \
    ConnectionStrings__DefaultConnection="Data Source=gcc_database.db" \
    Features__EnableAzureAdB2C="false" \
    Features__EnableEmailNotifications="false" \
    Features__EnableTwoFactorAuth="false" \
    Features__EnableDataExport="true" \
    Features__EnableBulkOperations="true" \
    Features__EnableAuditLogging="true" \
    --only-show-errors > /dev/null

echo "✅ Application settings configured"

# Enable Always On for Basic tier and above
if [ "$SKU" != "F1" ] && [ "$SKU" != "D1" ]; then
    echo ""
    echo "🔄 Enabling Always On..."
    az webapp config set \
        --resource-group $RESOURCE_GROUP \
        --name $APP_NAME \
        --always-on true \
        --only-show-errors > /dev/null
    echo "✅ Always On enabled"
fi

# Get the app URL
APP_URL="https://$APP_NAME.azurewebsites.net"

# Final summary
echo ""
echo "════════════════════════════════════════════════"
echo "       🎉 DEPLOYMENT COMPLETE! 🎉"
echo "════════════════════════════════════════════════"
echo ""
echo "📌 Application Details:"
echo "   URL: $APP_URL"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   App Name: $APP_NAME"
echo "   Region: $LOCATION"
echo "   Pricing Tier: $SKU"
echo ""

# Offer to open in browser
read -p "Would you like to open the app in your browser? (Y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Opening $APP_URL..."
    if command -v xdg-open &> /dev/null; then
        xdg-open "$APP_URL"
    elif command -v open &> /dev/null; then
        open "$APP_URL"
    else
        echo "Please open $APP_URL in your browser"
    fi
fi

echo ""
echo "📝 Next Steps:"
echo "1. Verify the application is working correctly"
echo "2. Set up a custom domain (optional)"
echo "3. Configure SSL certificate (free with custom domain)"
echo "4. Set up monitoring and alerts"
echo "5. Configure database if using SQL Server"
echo ""
echo "🔧 Useful Commands:"
echo "View logs:     az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo "Restart app:   az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo "View settings: az webapp config appsettings list --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo "Delete all:    az group delete --name $RESOURCE_GROUP --yes"
echo ""
echo "💡 Tip: Save these details for future reference!"
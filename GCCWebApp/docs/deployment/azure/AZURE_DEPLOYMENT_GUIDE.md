# Azure App Service Deployment Guide for GCC Web Application

## Prerequisites

1. **Azure Account**: Sign up at https://azure.microsoft.com (free tier available)
2. **Azure CLI**: Install from https://docs.microsoft.com/cli/azure/install-azure-cli
3. **Your compiled app**: `gccwebapp-dotnet8.tar.gz` (already created ✅)

## Step 1: Install Azure CLI (if not installed)

**Windows (PowerShell as Admin):**
```powershell
Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi
Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
```

**Or use winget:**
```bash
winget install Microsoft.AzureCLI
```

## Step 2: Login to Azure

```bash
az login
```
This opens your browser to authenticate.

## Step 3: Quick Deploy (Fastest Option)

Run this single command from your project directory:

```bash
az webapp up --name gccwebapp --runtime "DOTNET:8.0" --location "East US" --sku B1
```

This command:
- Creates a resource group
- Creates an App Service Plan (Basic B1 tier ~$13/month)
- Creates the Web App
- Deploys your application

## Step 4: Detailed Deployment (More Control)

If you want more control, use these commands:

```bash
# Set variables
$RESOURCE_GROUP="GCC-Resources"
$LOCATION="eastus"
$PLAN_NAME="GCC-AppPlan"
$APP_NAME="gccwebapp"
$SQL_SERVER="gcc-sqlserver"
$SQL_DB="GCCDatabase"
$SQL_ADMIN="gccadmin"
$SQL_PASSWORD="YourSecurePassword123!"

# Create Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan (B1 = Basic, ~$13/month)
az appservice plan create \
    --name $PLAN_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku B1 \
    --is-linux

# Create Web App with .NET 8.0
az webapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --plan $PLAN_NAME \
    --runtime "DOTNET:8.0"

# Deploy your application package
az webapp deployment source config-zip \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --src gccwebapp-dotnet8.tar.gz
```

## Step 5: Configure Database (Optional - for SQL Server)

### Option A: Use SQLite (Simplest - No Extra Cost)
Your app already supports SQLite. Just set the connection string:

```bash
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --settings DatabaseProvider="Sqlite" \
    ConnectionStrings__DefaultConnection="Data Source=gcc_database.db"
```

### Option B: Azure SQL Database (Managed SQL Server)
```bash
# Create SQL Server
az sql server create \
    --name $SQL_SERVER \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user $SQL_ADMIN \
    --admin-password $SQL_PASSWORD

# Create Database (Basic tier ~$5/month)
az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER \
    --name $SQL_DB \
    --service-objective Basic

# Allow Azure services to access
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER \
    --name AllowAzure \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

# Get connection string
$CONNECTION_STRING=$(az sql db show-connection-string \
    --server $SQL_SERVER \
    --name $SQL_DB \
    --client ado.net \
    --output tsv)

# Set connection string (replace placeholders)
$CONNECTION_STRING=$CONNECTION_STRING.Replace("<username>", $SQL_ADMIN).Replace("<password>", $SQL_PASSWORD)

az webapp config connection-string set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --connection-string-type SQLServer \
    --settings DefaultConnection=$CONNECTION_STRING
```

## Step 6: Configure Application Settings

```bash
# Set environment variables
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    GCC_JWT_SECRET="your-secure-64-character-jwt-secret-key-replace-this-immediately" \
    DatabaseProvider="SqlServer" \
    Features__EnableAzureAdB2C="false" \
    Features__EnableEmailNotifications="false" \
    Features__EnableTwoFactorAuth="false" \
    Features__EnableDataExport="true" \
    Features__EnableBulkOperations="true" \
    Features__EnableAuditLogging="true" \
    ApiSettings__EnableSwagger="false" \
    ApiSettings__RateLimitRequests="1000" \
    ApiSettings__RateLimitPeriodMinutes="60"
```

## Step 7: Configure Custom Domain (Optional)

```bash
# Add custom domain
az webapp config hostname add \
    --webapp-name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --hostname www.yourdomain.com

# Add SSL certificate (free managed cert)
az webapp config ssl bind \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --certificate-thumbprint $(az webapp config ssl create \
        --name $APP_NAME \
        --resource-group $RESOURCE_GROUP \
        --hostname www.yourdomain.com \
        --query thumbprint -o tsv) \
    --ssl-type SNI
```

## Step 8: Enable Monitoring

```bash
# Enable Application Insights
az monitor app-insights component create \
    --app gccwebapp-insights \
    --location $LOCATION \
    --resource-group $RESOURCE_GROUP \
    --application-type web

# Get Instrumentation Key
$INSTRUMENTATION_KEY=$(az monitor app-insights component show \
    --app gccwebapp-insights \
    --resource-group $RESOURCE_GROUP \
    --query instrumentationKey -o tsv)

# Connect to Web App
az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --settings APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY \
    ApplicationInsights__InstrumentationKey=$INSTRUMENTATION_KEY
```

## Step 9: Set Up Continuous Deployment (Optional)

### From GitHub:
```bash
az webapp deployment source config \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --repo-url https://github.com/yourusername/gccwebapp \
    --branch main \
    --git-token $GITHUB_TOKEN
```

## Step 10: Verify Deployment

```bash
# Get app URL
$APP_URL=$(az webapp show \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query defaultHostName -o tsv)

echo "Your app is deployed at: https://$APP_URL"

# Open in browser
start "https://$APP_URL"

# Check deployment logs
az webapp log tail \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP

# Check app health
curl "https://$APP_URL/health"
```

## Cost Optimization Tips

### Development/Testing (Cheapest):
- **Free Tier (F1)**: 60 min/day compute, perfect for testing
- **Shared (D1)**: ~$10/month, always on

### Production (Recommended):
- **Basic (B1)**: ~$13/month, custom domains, SSL
- **Basic (B2)**: ~$26/month, more resources
- **Standard (S1)**: ~$70/month, auto-scaling, slots

### Change pricing tier:
```bash
az appservice plan update \
    --name $PLAN_NAME \
    --resource-group $RESOURCE_GROUP \
    --sku F1  # Free tier for testing
```

## Troubleshooting

### View Logs:
```bash
az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP
```

### Enable detailed logging:
```bash
az webapp log config \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --application-logging filesystem \
    --detailed-error-messages true \
    --failed-request-tracing true \
    --level information
```

### SSH into container (for debugging):
```bash
az webapp ssh --name $APP_NAME --resource-group $RESOURCE_GROUP
```

### Restart app:
```bash
az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP
```

## Quick Deploy Script

Save this as `deploy-to-azure.ps1`:

```powershell
# Quick Azure Deployment Script for GCC Web App
$ErrorActionPreference = "Stop"

Write-Host "🚀 GCC Web App - Azure Deployment" -ForegroundColor Blue

# Configuration
$RESOURCE_GROUP = "GCC-Resources"
$LOCATION = "eastus"
$APP_NAME = "gccwebapp-$(Get-Random -Maximum 9999)"
$PLAN_NAME = "GCC-AppPlan"

Write-Host "📦 Deploying to Azure App Service..." -ForegroundColor Yellow

# Login to Azure
Write-Host "🔐 Logging into Azure..." -ForegroundColor Cyan
az login

# Create resource group
Write-Host "📁 Creating resource group..." -ForegroundColor Cyan
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy app with single command
Write-Host "🚀 Deploying application..." -ForegroundColor Cyan
az webapp up `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --plan $PLAN_NAME `
    --location $LOCATION `
    --runtime "DOTNET:8.0" `
    --sku B1

# Configure settings
Write-Host "⚙️ Configuring application settings..." -ForegroundColor Cyan
az webapp config appsettings set `
    --resource-group $RESOURCE_GROUP `
    --name $APP_NAME `
    --settings `
    ASPNETCORE_ENVIRONMENT="Production" `
    DatabaseProvider="Sqlite" `
    ConnectionStrings__DefaultConnection="Data Source=gcc_database.db"

# Get URL
$URL = "https://$APP_NAME.azurewebsites.net"
Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host "🌐 Your app is available at: $URL" -ForegroundColor Green
Write-Host "Opening in browser..." -ForegroundColor Yellow
Start-Process $URL
```

## Next Steps After Deployment

1. ✅ Verify the application loads
2. ✅ Test login functionality
3. ✅ Check certificate search works
4. ✅ Test API endpoints (`/api/health`)
5. ⚙️ Configure production database if needed
6. 🔒 Set up custom domain and SSL
7. 📊 Enable monitoring and alerts
8. 🔄 Set up automated backups

## Support Resources

- **Azure Portal**: https://portal.azure.com
- **Azure App Service Docs**: https://docs.microsoft.com/azure/app-service
- **Pricing Calculator**: https://azure.microsoft.com/pricing/calculator/
- **Free Account**: https://azure.microsoft.com/free/

---

**Deployment Time**: ~5-10 minutes
**Monthly Cost**: $13-70 depending on tier
**Free Trial**: $200 credit for 30 days available
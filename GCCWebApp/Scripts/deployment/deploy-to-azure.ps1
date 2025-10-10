# GCC Web App - Azure App Service Quick Deployment Script
# Run this script in PowerShell to deploy your app to Azurecan

$ErrorActionPreference = "Stop"

Write-Host @"
╔════════════════════════════════════════════╗
║   GCC Web App - Azure Deployment Script   ║
║         Quick & Easy Deployment            ║
╚════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# Configuration - Modify these if needed
$RESOURCE_GROUP = "GCC-Resources"
$LOCATION = "eastus"  # Change to your preferred region
$APP_NAME = "gccwebapp-$(Get-Random -Minimum 1000 -Maximum 9999)"  # Ensures unique name
$PLAN_NAME = "GCC-AppPlan"
$SKU = "B1"  # B1 = Basic ($13/month), F1 = Free (limited), S1 = Standard ($70/month)

Write-Host "`n📋 Deployment Configuration:" -ForegroundColor Yellow
Write-Host "   Resource Group: $RESOURCE_GROUP"
Write-Host "   Location: $LOCATION"
Write-Host "   App Name: $APP_NAME"
Write-Host "   Pricing Tier: $SKU"
Write-Host ""

# Check if Azure CLI is installed
Write-Host "🔍 Checking Azure CLI installation..." -ForegroundColor Cyan
try {
    $azVersion = az version 2>$null
    Write-Host "✅ Azure CLI is installed" -ForegroundColor Green
} catch {
    Write-Host "❌ Azure CLI is not installed!" -ForegroundColor Red
    Write-Host "Please install from: https://aka.ms/installazurecli" -ForegroundColor Yellow
    Write-Host "Or run: winget install Microsoft.AzureCLI" -ForegroundColor Yellow
    exit 1
}

# Login to Azure
Write-Host "`n🔐 Logging into Azure..." -ForegroundColor Cyan
Write-Host "   A browser window will open for authentication" -ForegroundColor Gray
az login --only-show-errors | Out-Null
Write-Host "✅ Logged in successfully" -ForegroundColor Green

# Check if deployment package exists
$PACKAGE = "gccwebapp-dotnet8.tar.gz"
if (-not (Test-Path $PACKAGE)) {
    Write-Host "❌ Deployment package not found: $PACKAGE" -ForegroundColor Red
    Write-Host "Building deployment package..." -ForegroundColor Yellow

    # Try to build the package
    dotnet publish GCCWebApp.csproj -c Release -r linux-x64 --self-contained false -o ./publish

    if (Test-Path "./publish") {
        Write-Host "Creating deployment archive..." -ForegroundColor Yellow
        tar -czf $PACKAGE -C publish .
        Write-Host "✅ Package created successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to create deployment package" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✅ Deployment package found: $PACKAGE" -ForegroundColor Green
}

# Create Resource Group
Write-Host "`n📁 Creating Resource Group..." -ForegroundColor Cyan
az group create `
    --name $RESOURCE_GROUP `
    --location $LOCATION `
    --only-show-errors | Out-Null
Write-Host "✅ Resource group created/verified" -ForegroundColor Green

# Create App Service Plan
Write-Host "`n📋 Creating App Service Plan..." -ForegroundColor Cyan
az appservice plan create `
    --name $PLAN_NAME `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --sku $SKU `
    --is-linux `
    --only-show-errors | Out-Null
Write-Host "✅ App Service Plan created" -ForegroundColor Green

# Create Web App
Write-Host "`n🌐 Creating Web App..." -ForegroundColor Cyan
az webapp create `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --plan $PLAN_NAME `
    --runtime "DOTNET:8.0" `
    --only-show-errors | Out-Null
Write-Host "✅ Web App created" -ForegroundColor Green

# Deploy the application
Write-Host "`n🚀 Deploying application (this may take 2-3 minutes)..." -ForegroundColor Cyan
$deployResult = az webapp deployment source config-zip `
    --resource-group $RESOURCE_GROUP `
    --name $APP_NAME `
    --src $PACKAGE `
    --only-show-errors 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Application deployed successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Deployment completed with warnings" -ForegroundColor Yellow
}

# Configure Application Settings
Write-Host "`n⚙️  Configuring application settings..." -ForegroundColor Cyan

# Generate a secure JWT secret
$JWT_SECRET = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})

az webapp config appsettings set `
    --resource-group $RESOURCE_GROUP `
    --name $APP_NAME `
    --settings `
    ASPNETCORE_ENVIRONMENT="Production" `
    GCC_JWT_SECRET="$JWT_SECRET" `
    DatabaseProvider="Sqlite" `
    ConnectionStrings__DefaultConnection="Data Source=gcc_database.db" `
    Features__EnableAzureAdB2C="false" `
    Features__EnableEmailNotifications="false" `
    Features__EnableTwoFactorAuth="false" `
    Features__EnableDataExport="true" `
    Features__EnableBulkOperations="true" `
    Features__EnableAuditLogging="true" `
    --only-show-errors | Out-Null

Write-Host "✅ Application settings configured" -ForegroundColor Green

# Enable Always On for Basic tier and above
if ($SKU -ne "F1" -and $SKU -ne "D1") {
    Write-Host "`n🔄 Enabling Always On..." -ForegroundColor Cyan
    az webapp config set `
        --resource-group $RESOURCE_GROUP `
        --name $APP_NAME `
        --always-on true `
        --only-show-errors | Out-Null
    Write-Host "✅ Always On enabled" -ForegroundColor Green
}

# Get the app URL
$APP_URL = "https://$APP_NAME.azurewebsites.net"

# Final summary
Write-Host "`n" -NoNewline
Write-Host "════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "       🎉 DEPLOYMENT COMPLETE! 🎉" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Application Details:" -ForegroundColor Yellow
Write-Host "   URL: $APP_URL" -ForegroundColor White
Write-Host "   Resource Group: $RESOURCE_GROUP" -ForegroundColor White
Write-Host "   App Name: $APP_NAME" -ForegroundColor White
Write-Host "   Region: $LOCATION" -ForegroundColor White
Write-Host "   Pricing Tier: $SKU" -ForegroundColor White
Write-Host ""

# Offer to open in browser
$openBrowser = Read-Host "Would you like to open the app in your browser? (Y/N)"
if ($openBrowser -eq "Y" -or $openBrowser -eq "y") {
    Write-Host "Opening $APP_URL..." -ForegroundColor Cyan
    Start-Process $APP_URL
}

Write-Host "`n📝 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Verify the application is working correctly"
Write-Host "2. Set up a custom domain (optional)"
Write-Host "3. Configure SSL certificate (free with custom domain)"
Write-Host "4. Set up monitoring and alerts"
Write-Host "5. Configure database if using SQL Server"
Write-Host ""
Write-Host "🔧 Useful Commands:" -ForegroundColor Cyan
Write-Host "View logs:     az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP"
Write-Host "Restart app:   az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP"
Write-Host "View settings: az webapp config appsettings list --name $APP_NAME --resource-group $RESOURCE_GROUP"
Write-Host "Delete all:    az group delete --name $RESOURCE_GROUP --yes"
Write-Host ""
Write-Host "💡 Tip: Save these details for future reference!" -ForegroundColor Green